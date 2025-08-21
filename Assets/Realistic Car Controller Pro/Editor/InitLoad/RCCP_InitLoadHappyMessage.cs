//----------------------------------------------
//        Realistic Car Controller Pro
//
// Copyright 2014 - 2025 BoneCracker Games
// https://www.bonecrackergames.com
// Ekrem Bugra Ozdoganlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

public class RCCP_InitLoadHappyMessage {

    [InitializeOnLoadMethod]
    public static void InitOnLoad() {

        if (SessionState.GetBool("BCG_HAPPYMESSAGE", false))
            return;

        EditorApplication.delayCall += () => {

            string[] messages = new string[5];

            messages[0] = "BoneCracker Games | Thanks for your support. We’re excited to see what you build with our assets. Good luck and have fun!";
            messages[1] = "BoneCracker Games | Thank you for your purchase. We’re here to support you in making your project a success. Happy coding!";
            messages[2] = "BoneCracker Games | Thanks for choosing our assets. We can’t wait to see what you’ll create. If you need help, we’re here for you!";
            messages[3] = "BoneCracker Games | We’re excited to see our assets in action in your projects. Wishing you the best in your development endeavors!";
            messages[4] = "BoneCracker Games | We appreciate your trust in our assets. Get started and bring your ideas to life. We're here to assist you along the way!";

            string randomMessage = messages[UnityEngine.Random.Range(0, 5)];

            Debug.Log("<color=#00FF00>" + randomMessage + "</color>");
            SessionState.SetBool("BCG_HAPPYMESSAGE", true);

        };

    }

}
