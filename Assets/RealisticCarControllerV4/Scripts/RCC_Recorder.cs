//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Records and replays vehicle inputs, transforms, and rigidbody states.
/// Optimized version with better memory management and no forced garbage collection.
/// </summary>
public class RCC_Recorder : RCC_Core {

    /// <summary>
    /// Recording mode states.
    /// </summary>
    public enum RecorderMode {

        Neutral,
        Record,
        Play

    }

    /// <summary>
    /// Current recorder mode.
    /// </summary>
    public RecorderMode mode = RecorderMode.Neutral;

    /// <summary>
    /// Maximum recording duration in seconds.
    /// </summary>
    public float maxRecordingDuration = 300f;  // 5 minutes default

    /// <summary>
    /// Maximum number of frames to record (safety limit).
    /// </summary>
    public int maxRecordingFrames = 18000;  // 5 minutes at 60 FPS

    /// <summary>
    /// Current recording time.
    /// </summary>
    private float recordingTime = 0f;

    /// <summary>
    /// Initial list capacity for better memory allocation.
    /// </summary>
    private const int INITIAL_CAPACITY = 3600;  // 1 minute at 60 FPS

    /// <summary>
    /// Lists to store recorded data during recording.
    /// </summary>
    private List<PlayerInput> recordingInputs;
    private List<PlayerTransform> recordingTransforms;
    private List<PlayerRigidBody> recordingRigidbodies;

    /// <summary>
    /// Completed recording clip.
    /// </summary>
    public RecordedClip recordedClip;

    /// <summary>
    /// Playback time for interpolation.
    /// </summary>
    private float playbackTime = 0f;

    /// <summary>
    /// Recorded data structures.
    /// </summary>
    [System.Serializable]
    public class PlayerInput {

        public float throttleInput = 0f;
        public float brakeInput = 0f;
        public float steerInput = 0f;
        public float handbrakeInput = 0f;
        public float clutchInput = 0f;
        public float boostInput = 0f;

    }

    [System.Serializable]
    public class PlayerTransform {

        public Vector3 position;
        public Quaternion rotation;

    }

    [System.Serializable]
    public class PlayerRigidBody {

        public Vector3 velocity;
        public Vector3 angularVelocity;

    }

    [System.Serializable]
    public class RecordedClip {

        public string recordName;
        public PlayerInput[] inputs;
        public PlayerTransform[] transforms;
        public PlayerRigidBody[] rigids;

        public RecordedClip(PlayerInput[] _inputs, PlayerTransform[] _transforms, PlayerRigidBody[] _rigids, string _recordName) {

            inputs = _inputs;
            transforms = _transforms;
            rigids = _rigids;
            recordName = _recordName;

        }

    }

    private void Awake() {

        // Initialize lists with initial capacity to reduce memory allocations
        InitializeLists();

    }

    private void OnEnable() {

        // Clear any existing recording data on enable
        ClearRecordingData();

    }

    private void Update() {

        // Update UI or visual feedback here if needed
        if (mode == RecorderMode.Record) {

            recordingTime += Time.deltaTime;

        }

    }

    private void FixedUpdate() {

        switch (mode) {

            case RecorderMode.Record:
                RecordFrame();
                break;

            case RecorderMode.Play:
                PlayFrame();
                break;

        }

    }

    /// <summary>
    /// Initializes recording lists with initial capacity.
    /// </summary>
    private void InitializeLists() {

        if (recordingInputs == null)
            recordingInputs = new List<PlayerInput>(INITIAL_CAPACITY);

        if (recordingTransforms == null)
            recordingTransforms = new List<PlayerTransform>(INITIAL_CAPACITY);

        if (recordingRigidbodies == null)
            recordingRigidbodies = new List<PlayerRigidBody>(INITIAL_CAPACITY);

    }

    /// <summary>
    /// Records current frame data.
    /// </summary>
    private void RecordFrame() {

        // Check if we've reached recording limits
        if (recordingTime >= maxRecordingDuration || recordingInputs.Count >= maxRecordingFrames) {

            Debug.LogWarning("RCC_Recorder: Maximum recording duration/frames reached. Stopping recording.");
            Stop();
            return;

        }

        if (!CarController) {

            Stop();
            return;

        }

        // Record input data
        PlayerInput currentInput = new PlayerInput {
            throttleInput = CarController.throttleInput,
            brakeInput = CarController.brakeInput,
            steerInput = CarController.steerInput,
            handbrakeInput = CarController.handbrakeInput,
            clutchInput = CarController.clutchInput,
            boostInput = CarController.boostInput
        };

        // Record transform data
        PlayerTransform currentTransform = new PlayerTransform {
            position = CarController.transform.position,
            rotation = CarController.transform.rotation
        };

        // Record rigidbody data
        PlayerRigidBody currentRigid = new PlayerRigidBody {
            velocity = CarController.Rigid.linearVelocity,
            angularVelocity = CarController.Rigid.angularVelocity
        };

        // Add to recording lists
        recordingInputs.Add(currentInput);
        recordingTransforms.Add(currentTransform);
        recordingRigidbodies.Add(currentRigid);

    }

    /// <summary>
    /// Plays back recorded frame data.
    /// </summary>
    private void PlayFrame() {

        if (recordedClip == null || recordedClip.inputs == null || recordedClip.inputs.Length == 0) {

            Stop();
            return;

        }

        // Calculate current playback position
        playbackTime += Time.fixedDeltaTime;
        int frameIndex = Mathf.FloorToInt(playbackTime / Time.fixedDeltaTime);

        // Check if playback is complete - check ALL arrays
        if (frameIndex >= recordedClip.inputs.Length ||
            frameIndex >= recordedClip.transforms.Length ||
            frameIndex >= recordedClip.rigids.Length) {

            Stop();
            return;

        }

        if (!CarController) {

            Stop();
            return;

        }

        // Apply recorded inputs
        CarController.throttleInput = recordedClip.inputs[frameIndex].throttleInput;
        CarController.brakeInput = recordedClip.inputs[frameIndex].brakeInput;
        CarController.steerInput = recordedClip.inputs[frameIndex].steerInput;
        CarController.handbrakeInput = recordedClip.inputs[frameIndex].handbrakeInput;
        CarController.clutchInput = recordedClip.inputs[frameIndex].clutchInput;
        CarController.boostInput = recordedClip.inputs[frameIndex].boostInput;

        // Apply transform and rigidbody data
        CarController.transform.position = recordedClip.transforms[frameIndex].position;
        CarController.transform.rotation = recordedClip.transforms[frameIndex].rotation;
        CarController.Rigid.linearVelocity = recordedClip.rigids[frameIndex].velocity;
        CarController.Rigid.angularVelocity = recordedClip.rigids[frameIndex].angularVelocity;

    }

    /// <summary>
    /// Starts or stops recording.
    /// </summary>
    public void Record() {

        if (mode == RecorderMode.Record) {

            // Stop recording and save clip
            SaveRecording();
            mode = RecorderMode.Neutral;

            if (CarController)
                CarController.SetExternalControl(false);

        } else {

            // Start recording
            ClearRecordingData();
            mode = RecorderMode.Record;

            if (CarController)
                CarController.SetExternalControl(false);

        }

    }

    /// <summary>
    /// Starts or stops playback.
    /// </summary>
    public void Play() {

        if (mode == RecorderMode.Play) {

            Stop();

        } else {

            if (recordedClip != null && recordedClip.inputs != null && recordedClip.inputs.Length > 0) {

                mode = RecorderMode.Play;
                playbackTime = 0f;

                if (CarController)
                    CarController.SetExternalControl(true);

            } else {

                Debug.LogWarning("RCC_Recorder: No recorded clip available to play.");

            }

        }

    }

    /// <summary>
    /// Stops recording or playback.
    /// </summary>
    public void Stop() {

        if (mode == RecorderMode.Record) {

            SaveRecording();

        }

        mode = RecorderMode.Neutral;
        playbackTime = 0f;

        if (CarController)
            CarController.SetExternalControl(false);

    }

    /// <summary>
    /// Saves the current recording to a clip.
    /// </summary>
    private void SaveRecording() {

        if (recordingInputs != null && recordingInputs.Count > 0) {

            recordedClip = new RecordedClip(
                recordingInputs.ToArray(),
                recordingTransforms.ToArray(),
                recordingRigidbodies.ToArray(),
                "Recording_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss")
            );

            Debug.Log($"RCC_Recorder: Saved recording with {recordingInputs.Count} frames.");

        }

        ClearRecordingData();

    }

    /// <summary>
    /// Clears all recording data to free memory.
    /// </summary>
    private void ClearRecordingData() {

        if (recordingInputs != null) {

            recordingInputs.Clear();

            // Only trim if significantly oversized
            if (recordingInputs.Capacity > INITIAL_CAPACITY * 4)
                recordingInputs.Capacity = INITIAL_CAPACITY;

        }

        if (recordingTransforms != null) {

            recordingTransforms.Clear();

            // Only trim if significantly oversized
            if (recordingTransforms.Capacity > INITIAL_CAPACITY * 4)
                recordingTransforms.Capacity = INITIAL_CAPACITY;

        }

        if (recordingRigidbodies != null) {

            recordingRigidbodies.Clear();

            // Only trim if significantly oversized
            if (recordingRigidbodies.Capacity > INITIAL_CAPACITY * 4)
                recordingRigidbodies.Capacity = INITIAL_CAPACITY;

        }

        recordingTime = 0f;

    }

    /// <summary>
    /// Clears the recorded clip to free memory.
    /// </summary>
    public void ClearRecordedClip() {

        recordedClip = null;
        // Removed System.GC.Collect() - let Unity handle garbage collection naturally

    }

    /// <summary>
    /// Coroutine to clear recorded clip asynchronously to avoid frame drops.
    /// </summary>
    private IEnumerator ClearRecordedClipAsync() {

        recordedClip = null;

        // Wait a frame before suggesting garbage collection
        yield return null;

        // Use incremental GC if available (Unity 2019.1+)
#if UNITY_2019_1_OR_NEWER
        if (UnityEngine.Scripting.GarbageCollector.isIncremental) {

            UnityEngine.Scripting.GarbageCollector.CollectIncremental(1000000); // 1ms time slice

        }
#endif

    }

    private void OnDisable() {

        Stop();
        ClearRecordingData();

    }

    private void OnDestroy() {

        // Simply clear references without forcing GC
        ClearRecordingData();
        recordedClip = null;

        // Clear list references
        recordingInputs = null;
        recordingTransforms = null;
        recordingRigidbodies = null;

    }

    /// <summary>
    /// Draws recording status in the editor.
    /// </summary>
    private void OnDrawGizmos() {

        if (!enabled || !gameObject.activeInHierarchy)
            return;

        if (mode == RecorderMode.Record) {

            // Draw recording indicator
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.5f);

            // Draw recording progress
            if (maxRecordingDuration > 0) {

                float progress = recordingTime / maxRecordingDuration;
                Gizmos.color = Color.Lerp(Color.green, Color.red, progress);
                Gizmos.DrawLine(transform.position + Vector3.up * 2.5f, transform.position + Vector3.up * 2.5f + Vector3.right * (progress * 2f));

            }

        } else if (mode == RecorderMode.Play) {

            // Draw playback indicator
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 3f, 0.5f);

            // Draw playback progress
            if (recordedClip != null && recordedClip.inputs != null && recordedClip.inputs.Length > 0) {

                float progress = playbackTime / (recordedClip.inputs.Length * Time.fixedDeltaTime);
                Gizmos.color = Color.Lerp(Color.green, Color.yellow, progress);
                Gizmos.DrawLine(transform.position + Vector3.up * 2.5f, transform.position + Vector3.up * 2.5f + Vector3.right * (progress * 2f));

            }

        }

    }

}