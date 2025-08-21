//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------


using UnityEngine;
using UnityEditor;

public class RCC_UpgradeWindow : EditorWindow {

    private GUISkin skin;

    private float fadeValue = 0f;
    private double startTime;
    private bool isAnimating = true;

    private static readonly string prefsKey = "RCCP_UpgradeReminder_Hide";
    private static bool dontShowAgain = false;

    [InitializeOnLoadMethod]
    private static void InitOnLoad() {
     
        EditorApplication.delayCall += () => {

            if (SessionState.GetBool("RCCP_UpgradeReminder_Showed", false))
                return;

            if (EditorPrefs.GetBool(prefsKey, false))
                return;

            ShowWindow();

            SessionState.SetBool("RCCP_UpgradeReminder_Showed", true);

        };

    }

    public static void ShowWindow() {

        RCC_UpgradeWindow window = GetWindow<RCC_UpgradeWindow>(true, "Upgrade to Realistic Car Controller Pro", true);
        window.minSize = new Vector2(400f, 250f);
        //window.maxSize = new Vector2(400f, 250f);
        window.Show();

    }

    private void OnEnable() {

        startTime = EditorApplication.timeSinceStartup;
        skin = Resources.Load("RCC_WindowSkin") as GUISkin;

        dontShowAgain = EditorPrefs.GetBool(prefsKey, false);

    }

    private void OnGUI() {

        if (skin)
            GUI.skin = skin;

        // Fade in
        if (isAnimating) {
            fadeValue = Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - startTime) * 2f));
            if (fadeValue >= 1f)
                isAnimating = false;

            Repaint();
        }

        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, fadeValue);

        EditorGUILayout.Space(10f);
        EditorGUILayout.BeginVertical(GUI.skin.box);

        EditorGUILayout.Space(5f);
        GUILayout.Label("Upgrading to Realistic Car Controller [Pro]", EditorStyles.boldLabel);
        EditorGUILayout.Space(5f);

        GUILayout.Label(
            "Pro version is the next generation vehicle physics package based on RCC. It includes fully modular vehicle architecture, realistic drivetrain, improved wheelcolliders, attachable components, URP/HDRP support, remap inputs, better AI, and much more.\n\nUpgrading to Realistic Car Controller Pro is %50 off for a limited time!"
        );

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Learn More", GUILayout.Height(30))) {
            Application.OpenURL("https://assetstore.unity.com/packages/tools/physics/realistic-car-controller-pro-178967");
        }

        if (GUILayout.Button("Close", GUILayout.Height(30))) {

            if (dontShowAgain) {
                EditorPrefs.SetBool(prefsKey, true);
            }

            Close();

        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10f);
        EditorGUILayout.BeginHorizontal();
        dontShowAgain = GUILayout.Toggle(dontShowAgain, " Don't show again");
        EditorGUILayout.EndHorizontal();

        GUI.color = oldColor;

    }

}
