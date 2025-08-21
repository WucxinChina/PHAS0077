using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.IO;

public class MomoRCCPInput : MonoBehaviour
{

    // 目标车辆的 RCCP_Input
    public RCCP_Input targetVehicleInput;
    public RCCP_Inputs momoInputs;

    private void Awake()
    {
        momoInputs = new RCCP_Inputs();
    }

    void Update()
    {
        foreach (var device in InputSystem.devices)
        {
            if (!device.displayName.Contains("G25 Racing Wheel"))
                continue;

            foreach (var control in device.allControls)
            {
                var value = control.ReadValueAsObject();
                if (value == null) continue;

                string controlName = control.name;

                // 按名称映射到 RCCP_Inputs
                switch (controlName)
                {
                    case "up":
                        targetVehicleInput.throttleInput = (float)value;
                        break;

                    case "down":
                        targetVehicleInput.brakeInput = (float)value;
                        break;

                    case "x":
                        Debug.Log((float)value);
                        targetVehicleInput.steerInput = (float)value;
                        break;
                }
            }

            // 把输入喂给 RCCP
/*            if (targetVehicleInput != null)
            {
                targetVehicleInput.OverrideInputs(momoInputs);
                Debug.Log("throttleInput: " + momoInputs.throttleInput);
                Debug.Log("steerInput: " + momoInputs.steerInput);
            }*/
        }
    }
}
