using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;
using System;

public class PrintMomoInputs : MonoBehaviour
{
    private Dictionary<InputControl, object> lastValues = new Dictionary<InputControl, object>();

    // 日志文件路径
    private string logFilePath;

    void Start()
    {
        // 保存到项目根目录下的 Logs 文件夹
        string logDir = Path.Combine(Application.dataPath, "../Logs");
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        // 生成带日期时间的文件名
        logFilePath = Path.Combine(logDir, $"MomoInputLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

        // 写文件头
        File.AppendAllText(logFilePath, $"===== Logitech MOMO Racing Input Log Started at {DateTime.Now} =====\n");
    }

    void Update()
    {
        foreach (var device in InputSystem.devices)
        {
            // 过滤掉非 Logitech MOMO Racing 的设备
            if (!device.displayName.Contains("G25 Racing Wheel"))
                continue;

            string deviceInfo = $"🎮 检测到设备: {device.displayName} | 布局: {device.layout}";
            Debug.Log(deviceInfo);
            File.AppendAllText(logFilePath, deviceInfo + "\n");

            foreach (var control in device.allControls)
            {
                var value = control.ReadValueAsObject();
                if (value != null)
                {
                    if (!lastValues.ContainsKey(control) || !Equals(lastValues[control], value))
                    {
                        string logLine = $"{control.name} = {value}";
                        Debug.Log(logLine);
                        File.AppendAllText(logFilePath, logLine + "\n");

                        lastValues[control] = value;
                    }
                }
            }
        }
    }
}
