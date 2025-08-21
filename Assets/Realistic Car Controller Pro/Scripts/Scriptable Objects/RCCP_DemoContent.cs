//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All removable demo content.
/// </summary>
public class RCCP_DemoContent : ScriptableObject {

    public int instanceId = 0;

    #region singleton
    private static RCCP_DemoContent instance;
    public static RCCP_DemoContent Instance { get { if (instance == null) instance = Resources.Load("RCCP_DemoContent") as RCCP_DemoContent; return instance; } }
    #endregion

    public bool dontAskDemoContent = false;
    public Object[] content;

    public Object builtinShadersContent;
    public Object URPShadersContent;
    public Object HDRPShadersContent;

}
