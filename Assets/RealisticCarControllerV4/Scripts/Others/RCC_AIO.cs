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
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the All-In-One (AIO) playable demo scene for RCC.
/// This script handles level loading, UI toggling, and application quitting.
/// </summary>
public class RCC_AIO : RCC_Core {

    /// <summary>
    /// Singleton instance of the RCC_AIO script.
    /// </summary>
    private static RCC_AIO instance;

    /// <summary>
    /// UI panel for standard levels.
    /// </summary>
    public GameObject levels;

    /// <summary>
    /// UI panel for Photon multiplayer levels.
    /// </summary>
    public GameObject photonLevels;

    /// <summary>
    /// UI panel for enter/exit enabled levels.
    /// </summary>
    public GameObject BCGLevels;

    /// <summary>
    /// Back button in the UI.
    /// </summary>
    public GameObject back;

    /// <summary>
    /// Asynchronous operation for loading scenes.
    /// </summary>
    private AsyncOperation async;

    /// <summary>
    /// Slider used to display the scene loading progress.
    /// </summary>
    public Slider slider;

    private void Start() {

        // Ensure only one instance exists in the scene. If an instance already exists, destroy this one.
        if (instance) {

            Destroy(gameObject);
            return;

        } else {

            instance = this;
            DontDestroyOnLoad(gameObject);

        }

        // Disabling Photon level buttons if RCC_PHOTON is not defined.
#if !RCC_PHOTON
        Toggle[] pbuttons = photonLevels.GetComponentsInChildren<Toggle>();

        foreach (var button in pbuttons)
            button.interactable = false;
#endif

        // Disabling Enter/Exit level buttons if BCG_ENTEREXIT is not defined.
#if !BCG_ENTEREXIT
        Toggle[] bbuttons = BCGLevels.GetComponentsInChildren<Toggle>();

        foreach (var button in bbuttons)
            button.interactable = false;
#endif

    }

    private void Update() {

        // If a level is being loaded asynchronously, update the loading slider.
        if (async != null && !async.isDone) {

            if (!slider.gameObject.activeSelf)
                slider.gameObject.SetActive(true);

            // Update the slider value based on the async progress.
            slider.value = async.progress;

        } else {

            // Hide the slider if loading is complete.
            if (slider.gameObject.activeSelf)
                slider.gameObject.SetActive(false);

        }

    }

    /// <summary>
    /// Loads a specified level asynchronously.
    /// </summary>
    /// <param name="levelName">The name of the level to load.</param>
    public void LoadLevel(string levelName) {

        async = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Single);

    }

    /// <summary>
    /// Toggles between UI menus, enabling the specified menu while disabling others.
    /// </summary>
    /// <param name="menu">The menu GameObject to activate.</param>
    public void ToggleMenu(GameObject menu) {

        // Disable other UI panels before enabling the target menu.
        levels.SetActive(false);
        back.SetActive(false);

        menu.SetActive(true);

    }

    /// <summary>
    /// Closes the application. Has no effect in the Unity Editor.
    /// </summary>
    public void Quit() {

        Application.Quit();

    }

}
