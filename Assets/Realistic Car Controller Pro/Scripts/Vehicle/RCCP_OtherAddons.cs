//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A container for miscellaneous vehicle add-ons beyond the core drivetrain. 
/// Provides references to NOS, dashboards, cameras, exhausts, AI, recorder, trailer attachments, limiters, etc.
/// </summary>
[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Other Addons/RCCP Other Addons")]
public class RCCP_OtherAddons : RCCP_Component {

    /// <summary>
    /// NOS component (boosting engine torque).
    /// </summary>
    public RCCP_Nos Nos {
        get {
            if (_nos == null)
                _nos = RCCP_TryGetComponentInChildren.Get<RCCP_Nos>(transform);
            return _nos;
        }
        set {
            _nos = value;
        }
    }

    /// <summary>
    /// Visual dashboard component for instrument panels (speedometer, RPM, etc.).
    /// </summary>
    public RCCP_Visual_Dashboard Dashboard {
        get {
            if (_dashboard == null)
                _dashboard = RCCP_TryGetComponentInChildren.Get<RCCP_Visual_Dashboard>(transform);
            return _dashboard;
        }
        set {
            _dashboard = value;
        }
    }

    /// <summary>
    /// Additional exterior cameras such as hood and wheel viewpoints.
    /// </summary>
    public RCCP_Exterior_Cameras ExteriorCameras {
        get {
            if (_exteriorCameras == null)
                _exteriorCameras = RCCP_TryGetComponentInChildren.Get<RCCP_Exterior_Cameras>(transform);
            return _exteriorCameras;
        }
        set {
            _exteriorCameras = value;
        }
    }

    /// <summary>
    /// Manages multiple exhaust points and their effects (smoke, flames).
    /// </summary>
    public RCCP_Exhausts Exhausts {
        get {
            if (_exhausts == null)
                _exhausts = RCCP_TryGetComponentInChildren.Get<RCCP_Exhausts>(transform);
            return _exhausts;
        }
        set {
            _exhausts = value;
        }
    }

    /// <summary>
    /// AI component that can drive or navigate the vehicle programmatically.
    /// </summary>
    public RCCP_AI AI {
        get {
            if (_AI == null)
                _AI = RCCP_TryGetComponentInChildren.Get<RCCP_AI>(transform);
            return _AI;
        }
        set {
            _AI = value;
        }
    }

    /// <summary>
    /// Recorder component for recording and replaying vehicle movement.
    /// </summary>
    public RCCP_Recorder Recorder {
        get {
            if (_recorder == null)
                _recorder = RCCP_TryGetComponentInChildren.Get<RCCP_Recorder>(transform);
            return _recorder;
        }
        set {
            _recorder = value;
        }
    }

    /// <summary>
    /// Manages trailer attachments, allowing the vehicle to tow trailers.
    /// </summary>
    public RCCP_TrailerAttacher TrailAttacher {
        get {
            if (_trailerAttacher == null)
                _trailerAttacher = RCCP_TryGetComponentInChildren.Get<RCCP_TrailerAttacher>(transform);
            return _trailerAttacher;
        }
        set {
            _trailerAttacher = value;
        }
    }

    /// <summary>
    /// Speed limiter for restricting max speed per gear.
    /// </summary>
    public RCCP_Limiter Limiter {
        get {
            if (_limiter == null)
                _limiter = RCCP_TryGetComponentInChildren.Get<RCCP_Limiter>(transform);
            return _limiter;
        }
        set {
            _limiter = value;
        }
    }

    /// <summary>
    /// Wheel blur component, creating motion-blur effects for rotating wheels.
    /// </summary>
    public RCCP_WheelBlur WheelBlur {
        get {
            if (_wheelBlur == null)
                _wheelBlur = RCCP_TryGetComponentInChildren.Get<RCCP_WheelBlur>(transform);
            return _wheelBlur;
        }
        set {
            _wheelBlur = value;
        }
    }

    /// <summary>
    /// Fuel tank component, tracking fuel capacity/consumption and optionally stopping the engine when empty.
    /// </summary>
    public RCCP_FuelTank FuelTank {
        get {
            if (_fuelTank == null)
                _fuelTank = RCCP_TryGetComponentInChildren.Get<RCCP_FuelTank>(transform);
            return _fuelTank;
        }
        set {
            _fuelTank = value;
        }
    }

    /// <summary>
    /// Simulates body tilting (lean) when accelerating, braking, or cornering.
    /// </summary>
    public RCCP_BodyTilt BodyTilt {
        get {
            if (_bodyTilt == null)
                _bodyTilt = RCCP_TryGetComponentInChildren.Get<RCCP_BodyTilt>(transform);
            return _bodyTilt;
        }
        set {
            _bodyTilt = value;
        }
    }

    // Backing fields for each property
    private RCCP_Nos _nos;
    private RCCP_Visual_Dashboard _dashboard;
    private RCCP_Exterior_Cameras _exteriorCameras;
    private RCCP_Exhausts _exhausts;
    private RCCP_AI _AI;
    private RCCP_Recorder _recorder;
    private RCCP_TrailerAttacher _trailerAttacher;
    private RCCP_Limiter _limiter;
    private RCCP_WheelBlur _wheelBlur;
    private RCCP_FuelTank _fuelTank;
    private RCCP_BodyTilt _bodyTilt;

}
