using System;
#if GLEY_TRAFFIC_SYSTEM
using Unity.Mathematics;
#endif
using UnityEngine;

namespace Gley.TrafficSystem
{
    public class BehaviourResult
    {
        private float _steerPercent;
        private int _targetGear;

        public TargetSpeedPoint SpeedInPoint { get; set; }
        public string Name { get; set; }
        public int Priority { get; set; }
        public float BrakePercent { get; set; }
        public float MaxAllowedSpeed { get; set; }

        public float SteerPercent
        {
            get => _steerPercent;
            set
            {
                if (value == TrafficSystemConstants.AUTO_STEER)
                {
                    _steerPercent = value;
                }
                else
                {
                    _steerPercent = Mathf.Clamp(value, -1, 1);
                }
            }
        }

        public int TargetGear
        {
            get => _targetGear;
            set => _targetGear = Math.Sign(value);
        }

        public BehaviourResult(int priority = 0)
        {
            Name = "DEFAULT";
            Priority = priority;
            BrakePercent = 0;
            MaxAllowedSpeed = TrafficSystemConstants.DEFAULT_SPEED;
            TargetGear = 1;
            SteerPercent = TrafficSystemConstants.AUTO_STEER;
#if GLEY_TRAFFIC_SYSTEM
            SpeedInPoint = new TargetSpeedPoint(TrafficSystemConstants.DEFAULT_POSITION, TrafficSystemConstants.DEFAULT_SPEED, 0, "DEFAULT");
#endif
        }

#if GLEY_TRAFFIC_SYSTEM
        public void Append(BehaviourResult value, float3 frontTriggerPosition)
        {
            if (value.Priority != 0 || Priority != 0)
            {
                if (Priority < value.Priority)
                {
                    Priority = value.Priority;
                    _targetGear = value.TargetGear;
                    _steerPercent = value.SteerPercent;
                    SpeedInPoint = value.SpeedInPoint;
                    BrakePercent = value.BrakePercent;
                    MaxAllowedSpeed = value.MaxAllowedSpeed;
                    Name = value.Name;
                }
                return;
            }

            //value.Print();
            // min target gear
            if (TargetGear > value.TargetGear)
            {
                _targetGear = value.TargetGear;
            }

            if (SteerPercent == TrafficSystemConstants.AUTO_STEER && value.SteerPercent != TrafficSystemConstants.AUTO_STEER)
            {
                _steerPercent = value.SteerPercent;
            }
            else
            {
                if (value.SteerPercent != TrafficSystemConstants.AUTO_STEER)
                {
                    // max steer percent
                    if (Mathf.Abs(SteerPercent) < Mathf.Abs(value.SteerPercent))
                    {
                        _steerPercent = value.SteerPercent;
                        Debug.Log($"SteerPercent: {SteerPercent}");
                    }
                }
            }

            float sqrDistCurrent = Vector3.SqrMagnitude(frontTriggerPosition - SpeedInPoint.StopPosition) - SpeedInPoint.DistanceToStop * SpeedInPoint.DistanceToStop;
            float sqrDistNew = Vector3.SqrMagnitude(frontTriggerPosition - value.SpeedInPoint.StopPosition) - value.SpeedInPoint.DistanceToStop * value.SpeedInPoint.DistanceToStop;

            if (sqrDistNew < sqrDistCurrent || (Vector3.SqrMagnitude(frontTriggerPosition - value.SpeedInPoint.StopPosition) < 1 && value.SpeedInPoint.TargetSpeed == 0))
            {
                if (!(Vector3.SqrMagnitude(frontTriggerPosition - SpeedInPoint.StopPosition) < 1 && SpeedInPoint.TargetSpeed == 0))
                {
                    SpeedInPoint = value.SpeedInPoint;
                }
            }

            // max brake percent
            if (BrakePercent < value.BrakePercent)
            {
                BrakePercent = value.BrakePercent;
                MaxAllowedSpeed = value.SpeedInPoint.TargetSpeed;
                Name = value.Name;
            }

            // min allowed speed
            if (MaxAllowedSpeed > value.MaxAllowedSpeed)
            {
                MaxAllowedSpeed = value.MaxAllowedSpeed;
                Name = value.Name;
            }
        }
#endif

#if GLEY_TRAFFIC_SYSTEM
        public string Print(string eolCharacter = "\n")
        {
            string text = $"{SpeedInPoint.Name} {eolCharacter}" +
                $"MaxAllowedSpeed {MaxAllowedSpeed} {eolCharacter}" +
                $"BrakePercent {BrakePercent} {eolCharacter}" +
                $"TargetGear {TargetGear} {eolCharacter}" +
                $"SteerPercent {SteerPercent} {eolCharacter}" +
                $"Speed Point: [Name {SpeedInPoint.Name} TargetSpeed {SpeedInPoint.TargetSpeed} StopPosition {SpeedInPoint.StopPosition} DistanceToStop {SpeedInPoint.DistanceToStop}]";
            return text;
        }
#endif
    }
}