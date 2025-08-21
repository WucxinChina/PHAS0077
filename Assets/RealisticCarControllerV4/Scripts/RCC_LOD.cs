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
/// A Level of Detail (LOD) system that enables or disables specific components (lights, audio sources, cameras, etc.)
/// in the vehicle based on its distance to the RCC camera. This helps optimize performance in large scenes.
/// </summary>
public class RCC_LOD : RCC_Core {

    /// <summary>
    /// The current distance between this vehicle and the active RCC camera.
    /// </summary>
    private float distanceToCamera = 0f;

    /// <summary>
    /// A multiplier that scales all LOD distances. 
    /// Increasing this value makes LOD transitions occur farther from the camera.
    /// </summary>
    public float lodBias = 2f;

    #region LOD Group

    /// <summary>
    /// Represents a group of GameObjects or components that can be collectively enabled or disabled.
    /// </summary>
    [System.Serializable]
    public class LODGroup {

        /// <summary>
        /// A list of GameObjects that belong to this LOD group. 
        /// When enabled, all these GameObjects become active; when disabled, all become inactive.
        /// </summary>
        public List<GameObject> group = new List<GameObject>();

        /// <summary>
        /// Optional array of wheel colliders associated with this group, 
        /// allowing us to disable certain wheel-specific features (e.g., skidmarks) at lower LOD levels.
        /// </summary>
        internal RCC_WheelCollider[] wheelColliderGroup;

        /// <summary>
        /// Tracks whether this group is currently active.
        /// </summary>
        public bool active = false;

        /// <summary>
        /// Adds a new GameObject to this LOD group if it's not already included.
        /// </summary>
        /// <param name="add">The GameObject to add.</param>
        public void Add(GameObject add) {

            if (!group.Contains(add))
                group.Add(add);

        }

        /// <summary>
        /// Activates all GameObjects in this group and re-enables specific wheel collider behaviors.
        /// </summary>
        public void EnableGroup() {

            // If already active, do nothing.
            if (active)
                return;

            // Enable all GameObjects in the group.
            for (int i = 0; i < group.Count; i++)
                group[i].SetActive(true);

            // If there are wheel colliders in this group, enable aligning wheels and skidmarks.
            if (wheelColliderGroup != null && wheelColliderGroup.Length > 0) {

                for (int i = 0; i < wheelColliderGroup.Length; i++) {

                    wheelColliderGroup[i].alignWheel = true;
                    wheelColliderGroup[i].drawSkid = true;

                }

            }

            active = true;

        }

        /// <summary>
        /// Deactivates all GameObjects in this group and disables certain wheel collider features.
        /// </summary>
        public void DisableGroup() {

            // If already inactive, do nothing.
            if (!active)
                return;

            // Disable all GameObjects in the group.
            for (int i = 0; i < group.Count; i++)
                group[i].SetActive(false);

            // If there are wheel colliders, disable aligning wheels and skidmarks for performance savings.
            if (wheelColliderGroup != null && wheelColliderGroup.Length > 0) {

                for (int i = 0; i < wheelColliderGroup.Length; i++) {

                    wheelColliderGroup[i].alignWheel = false;
                    wheelColliderGroup[i].drawSkid = false;

                }

            }

            active = false;

        }

    }

    #endregion

    /// <summary>
    /// Array of LOD groups that can be enabled or disabled depending on the current LOD level.
    /// </summary>
    private LODGroup[] lodGroup;

    /// <summary>
    /// The currently evaluated LOD level (0, 1, or 2).
    /// </summary>
    private int level = 0;

    /// <summary>
    /// Tracks the previous LOD level, used to detect when a level change occurs.
    /// </summary>
    private int oldLevel = -1;

    private void Awake() {

        // Create three LOD groups (lodGroup[0], lodGroup[1], lodGroup[2]).
        // You can add more groups if needed.
        lodGroup = new LODGroup[3];
        lodGroup[0] = new LODGroup();
        lodGroup[1] = new LODGroup();
        lodGroup[2] = new LODGroup();

    }

    private IEnumerator Start() {

        // Wait for a FixedUpdate to ensure the vehicle and its children are fully instantiated.
        yield return new WaitForFixedUpdate();

        // The second group (lodGroup[1]) will include audio sources, hood camera, wheel camera, and particles.
        if (transform.Find("All Audio Sources"))
            lodGroup[1].Add(transform.Find("All Audio Sources").gameObject);

        // The first group (lodGroup[0]) will include lights and wheel colliders.
        RCC_Light[] allLights = GetComponentsInChildren<RCC_Light>();
        foreach (RCC_Light item in allLights)
            lodGroup[0].Add(item.gameObject);

        // Hood camera goes to lodGroup[1].
        RCC_HoodCamera hoodCamera = GetComponentInChildren<RCC_HoodCamera>();
        if (hoodCamera)
            lodGroup[1].Add(hoodCamera.gameObject);

        // Wheel camera also goes to lodGroup[1].
        RCC_WheelCamera wheelCamera = GetComponentInChildren<RCC_WheelCamera>();
        if (wheelCamera)
            lodGroup[1].Add(wheelCamera.gameObject);

        // Particle systems (exhaust smoke, dust, etc.) also belong to lodGroup[1].
        ParticleSystem[] allParticles = GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem item in allParticles)
            lodGroup[1].Add(item.gameObject);

        // Store references to all wheel colliders in lodGroup[0] for easy enabling/disabling of their advanced features.
        lodGroup[0].wheelColliderGroup = GetComponentsInChildren<RCC_WheelCollider>();

    }

    private void Update() {

        // Get the distance from the vehicle to the active RCC main camera, if present.
        if (RCCSceneManager.activeMainCamera)
            distanceToCamera = Vector3.Distance(transform.position, RCCSceneManager.activeMainCamera.transform.position);

        // Determine LOD level based on distance thresholds, scaled by lodBias.
        if (distanceToCamera < 25f * lodBias)
            level = 2;
        else if (distanceToCamera < 50f * lodBias)
            level = 1;
        else if (distanceToCamera < 100f * lodBias)
            level = 0;

        // If the LOD level has changed since the last frame, apply the new LOD.
        if (level != oldLevel)
            SetLOD();

        // Record the LOD level for next frame.
        oldLevel = level;

    }

    /// <summary>
    /// Activates the current LOD level and deactivates higher ones. 
    /// For example, if level is 1, we enable lodGroup[0..1], and disable groups above that.
    /// </summary>
    private void SetLOD() {

        // Enable all groups down to the current level (e.g., if level=2, enable groups 2,1,0).
        for (int i = level; i >= 0; i--)
            lodGroup[i].EnableGroup();

        // Calculate how many groups remain above the current level (which should be disabled).
        int lev = (lodGroup.Length - 1) - level;
        for (int i = 0; i < lev; i++)
            lodGroup[i].DisableGroup();

    }

}
