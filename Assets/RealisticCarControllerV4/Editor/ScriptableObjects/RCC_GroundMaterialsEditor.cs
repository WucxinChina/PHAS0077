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
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(RCC_GroundMaterials))]
public class RCC_GroundMaterialsEditor : Editor {

    private RCC_GroundMaterials prop;
    private Vector2 scrollPos;
    private GUISkin skin;

    public void OnEnable() {

        skin = Resources.Load<GUISkin>("RCC_WindowSkin");
        prop = (RCC_GroundMaterials)target;

        if (prop.frictions == null)
            prop.frictions = new RCC_GroundMaterials.GroundMaterialFrictions[0];

    }

    public override void OnInspectorGUI() {

        serializedObject.Update();
        GUI.skin = skin != null ? skin : EditorGUIUtility.GetBuiltinSkin(EditorSkin.Scene);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Wheels Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This editor will update necessary .asset files in your project. Don't move ''Resources/RCC Assets''.", MessageType.Info);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Ground Materials", EditorStyles.boldLabel);

        for (int i = 0; i < prop.frictions.Length; i++) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            if (prop.frictions[i].groundMaterial)
                EditorGUILayout.LabelField(prop.frictions[i].groundMaterial.name + (i == 0 ? " (Default)" : ""), EditorStyles.boldLabel);

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.Width(25f)))
                RemoveGroundMaterial(i);

            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            prop.frictions[i].groundMaterial = (PhysicsMaterial)EditorGUILayout.ObjectField("Physic Material", prop.frictions[i].groundMaterial, typeof(PhysicsMaterial), false);
            prop.frictions[i].forwardStiffness = EditorGUILayout.FloatField("Forward Stiffness", prop.frictions[i].forwardStiffness);
            prop.frictions[i].sidewaysStiffness = EditorGUILayout.FloatField("Sideways Stiffness", prop.frictions[i].sidewaysStiffness);
            prop.frictions[i].slip = EditorGUILayout.FloatField("Slip", prop.frictions[i].slip);
            prop.frictions[i].damp = EditorGUILayout.FloatField("Damp", prop.frictions[i].damp);
            prop.frictions[i].volume = EditorGUILayout.Slider("Volume", prop.frictions[i].volume, 0f, 1f);
            prop.frictions[i].groundSound = (AudioClip)EditorGUILayout.ObjectField("Wheel Sound", prop.frictions[i].groundSound, typeof(AudioClip), false);
            prop.frictions[i].deflate = EditorGUILayout.Toggle("Deflate", prop.frictions[i].deflate);
            prop.frictions[i].groundParticles = (GameObject)EditorGUILayout.ObjectField("Wheel Particles", prop.frictions[i].groundParticles, typeof(GameObject), false);
            prop.frictions[i].skidmark = (RCC_Skidmarks)EditorGUILayout.ObjectField("Wheel Skidmarks", prop.frictions[i].skidmark, typeof(RCC_Skidmarks), false);

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainFrictions"), new GUIContent("Terrain Physic Material"), true);

        if (GUILayout.Button("Create New Ground Material"))
            AddNewWheel();

        if (GUILayout.Button("--< Return To Asset Settings"))
            OpenGeneralSettings();

        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Ekrem Bugra Ozdoganlar\nBoneCracker Games", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

        serializedObject.ApplyModifiedProperties();

    }

    public void AddNewWheel() {

        List<RCC_GroundMaterials.GroundMaterialFrictions> groundMaterials = prop.frictions.ToList();
        groundMaterials.Add(new RCC_GroundMaterials.GroundMaterialFrictions());
        prop.frictions = groundMaterials.ToArray();
        Save();

    }

    public void RemoveGroundMaterial(int index) {

        List<RCC_GroundMaterials.GroundMaterialFrictions> groundMaterials = prop.frictions.ToList();
        groundMaterials.RemoveAt(index);
        prop.frictions = groundMaterials.ToArray();
        Save();

    }

    public void Save() {

        EditorUtility.SetDirty(prop);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

    }

    public void OpenGeneralSettings() {

        Selection.activeObject = RCC_Settings.Instance;

    }

}
