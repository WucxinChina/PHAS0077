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
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Demo AIO")]
public class RCCP_DemoAIO : RCCP_GenericComponent {

    public static RCCP_DemoAIO Instance;

    public GameObject content;
    public GameObject loading;
    public GameObject credits;
    public GameObject back;

    public GameObject[] photonButtons;
    public GameObject[] sharedAssetsButtons;
    public GameObject[] cityTrafficButtons;

    public GameObject photonInfo;
    public GameObject sharedAssetsInfo;
    public GameObject cityTrafficInfo;

    private void Awake() {

        if (Instance == null) {

            Instance = this;
            DontDestroyOnLoad(gameObject);

        } else {

            Destroy(gameObject);
            return;

        }

    }

    private void Start() {

        SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

#if RCCP_PHOTON

        for (int i = 0; i < photonButtons.Length; i++)
            photonButtons[i].GetComponent<Button>().interactable = true;

        photonInfo.SetActive(false);

#else

        for (int i = 0; i < photonButtons.Length; i++)
            photonButtons[i].GetComponent<Button>().interactable = false;

        photonInfo.SetActive(true);

#endif

#if BCG_ENTEREXIT

        for (int i = 0; i < sharedAssetsButtons.Length; i++)
            sharedAssetsButtons[i].GetComponent<Button>().interactable = true;

        sharedAssetsInfo.SetActive(false);

#else

        for (int i = 0; i < sharedAssetsButtons.Length; i++)
            sharedAssetsButtons[i].GetComponent<Button>().interactable = false;

        sharedAssetsInfo.SetActive(true);

#endif

#if BCG_RTRC

        for (int i = 0; i < cityTrafficButtons.Length; i++)
            cityTrafficButtons[i].GetComponent<Button>().interactable = true;

        cityTrafficInfo.SetActive(false);

#else

        for (int i = 0; i < cityTrafficButtons.Length; i++)
            cityTrafficButtons[i].GetComponent<Button>().interactable = false;

        cityTrafficInfo.SetActive(true);

#endif

    }

    private void SceneManager_activeSceneChanged(Scene arg0, Scene arg1) {

        loading.SetActive(false);

    }

    public void LoadScene(string sceneIndex) {

        loading.SetActive(true);
        SceneManager.LoadSceneAsync(sceneIndex);

        if (sceneIndex == RCCP_DemoScenes.Instance.path_city_AIO) {

            content.SetActive(true);
            back.SetActive(false);
            credits.SetActive(true);

        } else {

            content.SetActive(false);
            back.SetActive(true);
            credits.SetActive(false);

        }

#if RCCP_PHOTON

        if (sceneIndex == RCCP_DemoScenes_Photon.Instance.path_demo_PUN2Lobby || sceneIndex == RCCP_DemoScenes_Photon.Instance.path_demo_PUN2City)
            back.SetActive(false);

#endif

    }

}
