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

[AddComponentMenu("BoneCracker Games/Realistic Car Controller Pro/Misc/RCCP Prop")]
public class RCCP_Prop : RCCP_GenericComponent {

    public float destroyAfterCollision = 3f;

    private void Awake() {

#if UNITY_2022_2_OR_NEWER
        IgnoreLayers();
#endif

    }

    private void OnEnable() {

        if (RCCPSettings.setLayers && RCCPSettings.RCCPPropLayer != "")
            gameObject.layer = LayerMask.NameToLayer(RCCPSettings.RCCPPropLayer);

        Rigidbody rigid = GetComponent<Rigidbody>();

        if (rigid)
            rigid.Sleep();

    }

    private void Reset() {

        if (RCCP_Settings.Instance.setLayers && RCCP_Settings.Instance.RCCPPropLayer != "")
            gameObject.layer = LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPPropLayer);

#if UNITY_2022_2_OR_NEWER
        IgnoreLayers();
#endif

    }

#if UNITY_2022_2_OR_NEWER
    private void IgnoreLayers() {

        //  Getting collider.
        Collider[] partColliders = GetComponentsInChildren<Collider>(true);

        LayerMask curLayerMask = -1;

        foreach (Collider collider in partColliders) {

            curLayerMask = collider.excludeLayers;
            curLayerMask |= (1 << LayerMask.NameToLayer(RCCP_Settings.Instance.RCCPWheelColliderLayer));
            collider.excludeLayers = curLayerMask;

        }

    }
#endif

    private void OnCollisionEnter(Collision collision) {

        if (destroyAfterCollision <= 0 || collision.impulse.magnitude < 100)
            return;

        Destroy(gameObject, destroyAfterCollision);

    }

}
