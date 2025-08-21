using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR;
// If you don't have Newtonsoft, remove this using and use JsonUtility in WriteMetaJson
using Newtonsoft.Json;

public class ExperimentLogger : MonoBehaviour
{
    [Header("References")]
    public Gley.TrafficSystem.PlayerComponent player;
    public Camera centerEyeCamera;

    [Header("Experiment Condition")]
    public Condition experimentCondition = Condition.MPC_HMI;
    public enum Condition { MPC_HMI, MPC_NoHMI, CACC_HMI, CACC_NoHMI }
    public string participantId = "P001";
    public string sessionTag = "S1";

    [Header("Sampling")]
    public float sampleHz = 20f;

    [Header("Emergency thresholds")]
    public float accelAbsThreshold = 5.0f; // m/s^2
    public float jerkAbsThreshold = 10.0f; // m/s^3

    [Header("Pedal/Steer statistics")]
    public float statWindowSec = 5f;

    [Header("Files")]
    [Tooltip("Non-Windows fallback dir under persistentDataPath; on Windows writes to C:/Users/Public/Chenxin/Log")]
    public string folderNonWindows = "Logs";

    private const int HEADER_COLS = 49;

    private string _csvPath;
    private string _metaPath;
    private float _dt;
    private float _timer;
    private float _lastFlushTime;

    private StreamWriter _writer;
    private readonly StringBuilder _sb = new(512);

    // kinematics state
    private Vector3 _prevVel;
    private float _prevAccel;
    private bool _hasPrevVel;

    // counters & flags
    private int _emergencyCount = 0;
    private bool _mergeTimingActive = false;
    private float _mergeStartTime = 0f;
    private float _mergeDurationLast = 0f;

    // merge snapshots
    private float _distFrontAtStart = float.NaN;
    private float _distRearAtStart = float.NaN;
    private float _distFrontAfter = float.NaN;
    private float _distRearAfter = float.NaN;

    // input stats (windowed)
    private readonly Queue<(float t, float v)> _gasHist = new();
    private readonly Queue<(float t, float v)> _brakeHist = new();
    private readonly Queue<(float t, float v)> _steerHist = new();
    private float _lastGas, _lastBrake, _lastSteer;

    // XR fallback
    private InputDevice _centerEye;

    void Start()
    {
        // Resolve writable directory
        string baseDir;
        if (Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.WindowsEditor)
        {
            baseDir = @"C:\Users\Public\Chenxin\Log";
        }
        else
        {
            baseDir = Path.Combine(Application.persistentDataPath, folderNonWindows);
        }

        Directory.CreateDirectory(baseDir);

        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var baseName = $"{participantId}_{sessionTag}_{experimentCondition}_{ts}";
        _csvPath = Path.Combine(baseDir, baseName + ".csv");
        _metaPath = Path.Combine(baseDir, baseName + "_meta.json");

        _dt = 1f / Mathf.Max(1f, sampleHz);
        _timer = 0f;
        _lastFlushTime = Time.unscaledTime;

        // Open writer once
        _writer = new StreamWriter(_csvPath, false, new UTF8Encoding(false));

        // 👉 Write participantId and condition in the very first line
        _writer.WriteLine($"participantId={participantId},condition={experimentCondition}");

        // Now write header row
        WriteCsvHeader();
        _writer.Flush();

        WriteMetaJson();

        TryGetCenterEyeDevice();

        if (player != null)
        {
            _lastGas = player.RawGas;
            _lastBrake = player.RawBrake;
            _lastSteer = player.RawSteer;
        }

        if (CountHeaderColumns() != HEADER_COLS)
            Debug.LogWarning($"[ExperimentLogger] HEADER_COLS={HEADER_COLS} does not match header count.");
    }


    void Update()
    {
        if (player == null) return;

        _timer += Time.deltaTime;
        while (_timer >= _dt)
        {
            _timer -= _dt;
            SampleAndAppend();
        }

        // Merge timing based on right indicator
        bool rightOn = player.IndicatorRightOn;
        if (!_mergeTimingActive && rightOn)
        {
            _mergeTimingActive = true;
            _mergeStartTime = Time.time;
            (_distFrontAtStart, _distRearAtStart) = ComputeFrontRearDistances();
        }
        else if (_mergeTimingActive && !rightOn)
        {
            _mergeTimingActive = false;
            _mergeDurationLast = Time.time - _mergeStartTime;
            (_distFrontAfter, _distRearAfter) = ComputeFrontRearDistances();

            AppendEventRow("MERGE", "RightIndicatorCycle",
                ("durationSec", _mergeDurationLast.ToString(CultureInfo.InvariantCulture)),
                ("distFrontAtStart", Safe(_distFrontAtStart, "F3")),
                ("distRearAtStart", Safe(_distRearAtStart, "F3")),
                ("distFrontAfter", Safe(_distFrontAfter, "F3")),
                ("distRearAfter", Safe(_distRearAfter, "F3"))
            );
        }

        // Periodic flush (once per second, unscaled)
        if (Time.unscaledTime - _lastFlushTime > 1f && _writer != null)
        {
            _writer.Flush();
            _lastFlushTime = Time.unscaledTime;
        }
    }

    private void SampleAndAppend()
    {
        var t = Time.time;

        // Player pose/vel
        var tr = player.PlayerTransform;
        var rb = player.PlayerRigidbody;

        Vector3 pos = tr != null ? tr.position : Vector3.zero;
        Vector3 fwd = tr != null ? tr.forward : Vector3.forward;

        Vector3 vel = Vector3.zero;
#if UNITY_6000_0_OR_NEWER
        if (rb != null) vel = rb.linearVelocity;
#else
        if (rb != null) vel = rb.velocity;
#endif

        // Acceleration/Jerk (using _dt)
        float accel = 0f, jerk = 0f;
        if (_hasPrevVel)
        {
            accel = (vel.magnitude - _prevVel.magnitude) / _dt;
            jerk = (accel - _prevAccel) / _dt;

            if (Mathf.Abs(accel) >= accelAbsThreshold || Mathf.Abs(jerk) >= jerkAbsThreshold)
            {
                _emergencyCount++;
                AppendEventRow("EMERGENCY", "KinematicsThreshold",
                    ("absAccel", Mathf.Abs(accel).ToString("F3", CultureInfo.InvariantCulture)),
                    ("absJerk", Mathf.Abs(jerk).ToString("F3", CultureInfo.InvariantCulture)),
                    ("count", _emergencyCount.ToString())
                );
            }
        }
        _prevVel = vel;
        _prevAccel = accel;
        _hasPrevVel = true;

        // HMD world + local
        Vector3 hPosW = Vector3.zero, hPosL = Vector3.zero;
        Quaternion hRotW = Quaternion.identity, hRotL = Quaternion.identity;
        Vector3 hEulerW = Vector3.zero, hEulerL = Vector3.zero;

        if (centerEyeCamera != null)
        {
            hPosW = centerEyeCamera.transform.position;
            hRotW = centerEyeCamera.transform.rotation;
            hEulerW = hRotW.eulerAngles;

            hPosL = centerEyeCamera.transform.localPosition;
            hRotL = centerEyeCamera.transform.localRotation;
            hEulerL = hRotL.eulerAngles;
        }
        else if (_centerEye.isValid &&
                 _centerEye.TryGetFeatureValue(CommonUsages.centerEyePosition, out hPosW) &&
                 _centerEye.TryGetFeatureValue(CommonUsages.centerEyeRotation, out hRotW))
        {
            hEulerW = hRotW.eulerAngles;
            hPosL = Vector3.zero;
            hRotL = Quaternion.identity;
            hEulerL = Vector3.zero;
        }

        // Distances
        var (distFront, distRear) = ComputeFrontRearDistances();

        // Inputs (from PlayerComponent)
        float gas = player.RawGas;
        float brake = player.RawBrake;
        float steer = player.RawSteer;

        UpdateInputStats(_gasHist, ref _lastGas, gas, t);
        UpdateInputStats(_brakeHist, ref _lastBrake, brake, t);
        UpdateInputStats(_steerHist, ref _lastSteer, steer, t);

        var (gasFreq, gasAmp) = ComputeStats(_gasHist);
        var (brakeFreq, brakeAmp) = ComputeStats(_brakeHist);
        var (steerFreq, steerAmp) = ComputeStats(_steerHist);

        // Build CSV row with minimal allocations
        _sb.Clear();

        // type + time
        _sb.Append(',').Append(t.ToString("F3", CultureInfo.InvariantCulture));

        // Player pose/vel
        Append3(pos, "F4"); Append3(fwd, "F4"); Append3(vel, "F4");
        _sb.Append(',').Append(accel.ToString("F4", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(jerk.ToString("F4", CultureInfo.InvariantCulture));

        // HMD WORLD
        Append3(hPosW, "F4");
        AppendQuat(hRotW);
        Append3(hEulerW, "F3");

        // HMD LOCAL
        Append3(hPosL, "F4");
        AppendQuat(hRotL);
        Append3(hEulerL, "F3");

        // merge distances
        _sb.Append(',').Append(Safe(distFront, "F3"));
        _sb.Append(',').Append(Safe(distRear, "F3"));

        // indicators
        _sb.Append(',').Append(player.IndicatorLeftOn ? '1' : '0');
        _sb.Append(',').Append(player.IndicatorRightOn ? '1' : '0');
        _sb.Append(',').Append(player.IndicatorAllOn ? '1' : '0');

        // raw inputs
        _sb.Append(',').Append(gas.ToString("F3", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(brake.ToString("F3", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(steer.ToString("F3", CultureInfo.InvariantCulture));

        // input stats
        _sb.Append(',').Append(gasFreq.ToString("F3", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(gasAmp.ToString("F3", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(brakeFreq.ToString("F3", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(brakeAmp.ToString("F3", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(steerFreq.ToString("F3", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(steerAmp.ToString("F3", CultureInfo.InvariantCulture));

        // emergencies cumulative
        _sb.Append(',').Append(_emergencyCount.ToString());

        // eventKV (empty for data row)
        _sb.Append(',');

        _writer.WriteLine(_sb.ToString());
    }

    private (float distFront, float distRear) ComputeFrontRearDistances()
    {
        float distF = float.NaN, distR = float.NaN;
        var front = player.RightFrontCandidate;
        var rear = player.RightRearCandidate;

        if (front != null && front.BackPosition != null)
            distF = Vector3.Distance(player.PlayerTransform.position, front.BackPosition.position);

        if (rear != null && rear.FrontPosition != null)
            distR = Vector3.Distance(player.PlayerTransform.position, rear.FrontPosition.position);

        return (distF, distR);
    }

    private void UpdateInputStats(Queue<(float t, float v)> q, ref float last, float now, float tNow)
    {
        q.Enqueue((tNow, now));
        while (q.Count > 0 && (tNow - q.Peek().t) > statWindowSec) q.Dequeue();
        last = now;
    }

    private (float freq, float amp) ComputeStats(Queue<(float t, float v)> q)
    {
        if (q.Count < 2) return (0f, 0f);
        var arr = q.ToArray();
        float t0 = arr[0].t;
        float t1 = arr[^1].t;
        float span = Mathf.Max(1e-3f, t1 - t0);

        float vMin = float.MaxValue, vMax = float.MinValue;
        for (int i = 0; i < arr.Length; i++)
        {
            float v = arr[i].v;
            if (v < vMin) vMin = v;
            if (v > vMax) vMax = v;
        }
        float amp = vMax - vMin;
        float freq = (arr.Length - 1) / span;
        return (freq, amp);
    }

    private void AppendEventRow(string evtType, string evtName, params (string key, string value)[] extras)
    {
        var row = new string[HEADER_COLS];
        for (int i = 0; i < HEADER_COLS; i++) row[i] = "";
        row[0] = "EVENT";
        row[1] = Time.time.ToString("F3", CultureInfo.InvariantCulture);

        string kv = $"{evtType}:{evtName}";
        foreach (var (k, v) in extras) kv += $";{k}={v}";
        row[HEADER_COLS - 1] = kv;

        _writer.WriteLine(string.Join(",", row));
    }

    private void TryGetCenterEyeDevice()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);
        foreach (var d in devices)
        {
            if (d.characteristics.HasFlag(InputDeviceCharacteristics.HeadMounted))
            {
                _centerEye = d;
                break;
            }
        }
    }

    private void WriteCsvHeader()
    {
        _writer.WriteLine(string.Join(",",
            "type", "time",
            "p.x", "p.y", "p.z",
            "fwd.x", "fwd.y", "fwd.z",
            "v.x", "v.y", "v.z",
            "accel", "jerk",
            "h.x", "h.y", "h.z",
            "hq.x", "hq.y", "hq.z", "hq.w",
            "he.x", "he.y", "he.z",
            "hLocal.x", "hLocal.y", "hLocal.z",
            "hqLocal.x", "hqLocal.y", "hqLocal.z", "hqLocal.w",
            "heLocal.x", "heLocal.y", "heLocal.z",
            "distFront", "distRear",
            "ind.left", "ind.right", "ind.all",
            "gas", "brake", "steer",
            "gas.freq", "gas.amp",
            "brake.freq", "brake.amp",
            "steer.freq", "steer.amp",
            "emergency.count",
            "eventKV"
        ));
    }

    private int CountHeaderColumns()
    {
        const string header = "type,time,p.x,p.y,p.z,fwd.x,fwd.y,fwd.z,v.x,v.y,v.z,accel,jerk,h.x,h.y,h.z,hq.x,hq.y,hq.z,hq.w,he.x,he.y,he.z,hLocal.x,hLocal.y,hLocal.z,hqLocal.x,hqLocal.y,hqLocal.z,hqLocal.w,heLocal.x,heLocal.y,heLocal.z,distFront,distRear,ind.left,ind.right,ind.all,gas,brake,steer,gas.freq,gas.amp,brake.freq,brake.amp,steer.freq,steer.amp,emergency.count,eventKV";
        return header.Split(',').Length;
    }

    private void WriteMetaJson()
    {
        var meta = new
        {
            participantId,
            sessionTag,
            condition = experimentCondition.ToString(),
            startedAt = DateTime.Now.ToString("O"),
            sampleHz,
            accelAbsThreshold,
            jerkAbsThreshold,
            statWindowSec,
            app = Application.productName,
            version = Application.version,
            platform = Application.platform.ToString(),
            unity = Application.unityVersion,
            logDir = (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
                     ? @"C:\Users\Public\Chenxin\Log"
                     : Path.Combine(Application.persistentDataPath, folderNonWindows)
        };

        // If no Newtonsoft: var json = JsonUtility.ToJson(meta, true);
        var json = JsonConvert.SerializeObject(meta, Formatting.Indented);
        File.WriteAllText(_metaPath, json);
    }

    // Helpers
    private string Safe(float v, string fmt)
    {
        if (float.IsNaN(v) || float.IsInfinity(v)) return "";
        return v.ToString(fmt, CultureInfo.InvariantCulture);
    }
    private void Append3(in Vector3 v, string fmt)
    {
        _sb.Append(',').Append(v.x.ToString(fmt, CultureInfo.InvariantCulture));
        _sb.Append(',').Append(v.y.ToString(fmt, CultureInfo.InvariantCulture));
        _sb.Append(',').Append(v.z.ToString(fmt, CultureInfo.InvariantCulture));
    }
    private void AppendQuat(in Quaternion q)
    {
        _sb.Append(',').Append(q.x.ToString("F6", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(q.y.ToString("F6", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(q.z.ToString("F6", CultureInfo.InvariantCulture));
        _sb.Append(',').Append(q.w.ToString("F6", CultureInfo.InvariantCulture));
    }

    private void OnApplicationQuit()
    {
        try { _writer?.Flush(); _writer?.Close(); } catch { }
    }
    private void OnDestroy()
    {
        try { _writer?.Flush(); _writer?.Close(); } catch { }
    }
}
