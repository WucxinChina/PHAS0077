//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

// Singleton pattern: http://wiki.unity3d.com/index.php/Singleton
using UnityEngine;

public class RCCP_Singleton<T> : RCCP_GenericComponent where T : RCCP_GenericComponent {

    private static T m_Instance;
    private static readonly object m_Lock = new object();

    public static T Instance {

        get {

            lock (m_Lock) {

                if (m_Instance != null)
                    return m_Instance;

                // Search for existing instance.
#if !UNITY_2022_1_OR_NEWER
                m_Instance = (T)FindObjectOfType(typeof(T));
#else
                m_Instance = (T)FindFirstObjectByType(typeof(T));
#endif
                // Create new instance if one doesn't already exist.
                if (m_Instance != null) return m_Instance;
                // Need to create a new GameObject to attach the singleton to.
                var singletonObject = new GameObject();
                m_Instance = singletonObject.AddComponent<T>();
                singletonObject.name = typeof(T).ToString();

                // Make instance persistent.
                //DontDestroyOnLoad(singletonObject);

                return m_Instance;

            }

        }

    }

}
