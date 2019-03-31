////////////////////////////////////////////////////////////////////////
// DeploymentBuilderWindow.cs
// By Don Hopkins.
// Copyright (C) 2014 by Deployment Corporation.


////////////////////////////////////////////////////////////////////////


#pragma warning disable 0414
#pragma warning disable 0219
#pragma warning disable 0168


////////////////////////////////////////////////////////////////////////
// Notes:


// EditorUserBuildSettings.activeBuildTarget


////////////////////////////////////////////////////////////////////////


using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;


////////////////////////////////////////////////////////////////////////


namespace UnityJS {


public class DeploymentBuilderWindow : EditorWindow {


    ////////////////////////////////////////////////////////////////////////
    // Instance Variables


    private Vector2 scrollPos1 = Vector2.zero;
    private float maxScrollViewHeight1 = 400.0f;
    private Vector2 scrollPos2 = Vector2.zero;
    private float maxScrollViewHeight2 = 400.0f;

    private static readonly GUIStyle titleFontStyle = new GUIStyle();
    private static readonly GUIStyle listFontStyle = new GUIStyle();
    private static readonly GUIStyle listFontStyleSelected = new GUIStyle();
    private static readonly GUIStyle listFontStyleCurrent = new GUIStyle();
    private static readonly GUIStyle listFontStyleCurrentSelected = new GUIStyle();

    ////////////////////////////////////////////////////////////////////////
    // Static Methods


    [MenuItem("Window/UnityJS Deployment Builder Window")]
    public static void ShowWindow()
    {
        var w = GetWindow(typeof(DeploymentBuilderWindow), false, "UnityJS Deployment Builder") as DeploymentBuilderWindow;
        if (w != null) {
            w.Show();
        }
    }


    ////////////////////////////////////////////////////////////////////////
    // Instance Methods


    private void Awake()
    {
        RectOffset margin = new RectOffset(10, 10, 10, 10);

        titleFontStyle.fontSize = 20;
        titleFontStyle.fontStyle = FontStyle.Bold;
        titleFontStyle.margin = margin;
        //titleFontStyle.normal.textColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        //titleFontStyle.normal.background = MakeTex(1, 1, new Color(0.8f, 0.8f, 0.8f, 1.0f));

        listFontStyle.fontSize = 12;
        listFontStyle.fontStyle = FontStyle.Normal;
        listFontStyle.margin = margin;
        listFontStyle.normal.textColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        //listFontStyle.normal.background = MakeTex(1, 1, new Color(0.8f, 0.8f, 0.8f, 1.0f));

        listFontStyleSelected.fontSize = 12;
        listFontStyleSelected.fontStyle = FontStyle.Normal;
        listFontStyleSelected.margin = margin;
        listFontStyleSelected.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        listFontStyleSelected.normal.background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f, 1.0f));

        listFontStyleCurrent.fontSize = 12;
        listFontStyleCurrent.fontStyle = FontStyle.Bold;
        listFontStyleCurrent.margin = margin;
        listFontStyleCurrent.normal.textColor = new Color(0.0f, 0.0f, 1.0f, 1.0f);
        listFontStyleCurrent.normal.background = MakeTex(1, 1, new Color(0.8f, 0.8f, 0.8f, 1.0f));

        listFontStyleCurrentSelected.fontSize = 12;
        listFontStyleCurrentSelected.fontStyle = FontStyle.Bold;
        listFontStyleCurrentSelected.margin = margin;
        listFontStyleCurrentSelected.normal.textColor = new Color(0.0f, 0.0f, 1.0f, 1.0f);
        listFontStyleCurrentSelected.normal.background = MakeTex(1, 1, new Color(0.2f, 0.2f, 0.2f, 1.0f));
    }


    private void OnGUI()
    {
        string applicationDataPath = Application.dataPath;
        JObject currentDeploymentConfiguration = null;
        JObject selectedDeploymentConfiguration = null;

        string deploymentID;
        string deployableApplicationDataPath;
        bool inDeployment = 
            DeploymentBuilder.GetInDeployment(
                out deploymentID,
                out deployableApplicationDataPath);

        string selectedDeploymentConfigurationID =
            EditorPrefs.GetString(
                "Deployment_selectedDeploymentConfigurationID", 
                "");

        string currentDeploymentConfigurationID =
            EditorPrefs.GetString(
                "Deployment_currentDeploymentConfigurationID", 
                "");

        GUILayout.Label(
            "UnityJS Deployment Builder Window");

        GUILayout.Space(5);

        if (GUILayout.Button(
            "\nReload Deployment Configurations\n")) {

            DeploymentBuilder.ReloadDeploymentConfigurations();

        }

        JArray deploymentConfigurations = 
            DeploymentBuilder.GetDeploymentConfigurations();

        GUILayout.Space(5);

        if (inDeployment) {

            selectedDeploymentConfigurationID = currentDeploymentConfigurationID = 
                deploymentID;

        }

        foreach (JObject deploymentConfiguration in deploymentConfigurations) {
            string id = 
                (string)deploymentConfiguration["id"];
            if (id == currentDeploymentConfigurationID) {
                currentDeploymentConfiguration = deploymentConfiguration;
                selectedDeploymentConfiguration = currentDeploymentConfiguration;
                break;
            }
        }

        if (inDeployment) {
            
            GUILayout.Label(
                "This is the deployment project: " + currentDeploymentConfigurationID);

            GUILayout.Space(5);

            if (currentDeploymentConfiguration == null) {

                GUILayout.Label(
                    "A deployment with that id is missing from the DeploymentConfigurations file!");

            } else {

                string[] scenes = currentDeploymentConfiguration["scenes"].ToObject<string[]>();
                string scenePath = (scenes.Length > 0) ? scenes[0] : "";

                UnityEngine.SceneManagement.Scene activeScene = EditorSceneManager.GetActiveScene();

                if ((activeScene == null) ||
                    (activeScene.path != scenePath)) {

                    if (GUILayout.Button(
                        "\nLoad Scene:\n" +
                        scenePath +
                        "\n")) {

                        UnityEngine.SceneManagement.Scene scene = 
                            EditorSceneManager.OpenScene(scenePath);
                        
                        Debug.Log("Opened scenePath: " + scenePath + " scene: " + scene);

                    }
                        
                } else {

                    if (GUILayout.Button(
                        "\nBuild Scene:\n" +
                        scenePath +
                        "\n")) {

                        DeploymentBuilder.ConfigureDeployment(currentDeploymentConfigurationID, false, true);

                    }

                }

            }

            GUILayout.Space(5);

        } else {

            GUILayout.Label(
                "This is the deployable project, not a deployment project.");

            GUILayout.Label(
                "Select a deployment configuration:");

            scrollPos1 = 
                EditorGUILayout.BeginScrollView(
                    scrollPos1, 
                    GUILayout.ExpandHeight(true),
                    GUILayout.MaxHeight(maxScrollViewHeight1));

            foreach (JObject deploymentConfiguration in deploymentConfigurations) {
                string id = 
                    (string)deploymentConfiguration["id"];
                bool current = 
                    id == currentDeploymentConfigurationID;
                bool selected = 
                    id == selectedDeploymentConfigurationID;
                string title = 
                    "    " + 
                    id + 
                    ": " +
                    (string)deploymentConfiguration["title"] +
                    (current
                        ? " (current)"
                        : "");

                if (selected) {
                    selectedDeploymentConfiguration = deploymentConfiguration;
                }

                if (GUILayout.Button(
                    title, 
                    current
                        ? (selected
                            ? listFontStyleCurrentSelected
                            : listFontStyleCurrent)
                        : (selected
                            ? listFontStyleSelected
                            : listFontStyle))) {

                    EditorPrefs.SetString("Deployment_selectedDeploymentConfigurationID", id);

                }

            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(5);

        }

        if (selectedDeploymentConfiguration == null) {

            GUILayout.Label(
                "Select a deployment configuration to see its properties.");

        } else {

            string title = 
                (string)selectedDeploymentConfiguration["title"];
            string applicationName = 
                (string)selectedDeploymentConfiguration["applicationName"];
            string location =
                (string)selectedDeploymentConfiguration["location"];
            string buildTarget =
                (string)selectedDeploymentConfiguration["buildTarget"];
            string buildTargetGroup =
                (string)selectedDeploymentConfiguration["buildTargetGroup"];
            string virtualRealitySupported =
                (string)selectedDeploymentConfiguration["virtualRealitySupported"];

            GUILayout.Label(
                "Selected Deployment Configuration" +
                ((selectedDeploymentConfiguration == currentDeploymentConfiguration)
                    ? " (current):\n"
                    : " (not current):\n") +
                "    ID: " + selectedDeploymentConfigurationID + "\n" +
                "    Title: "  + title + "\n" +
                "    Application Name: "  + applicationName + "\n" +
                "    Location: "  + location + "\n" +
                "    Build Target: "  + buildTarget + "\n" +
                "    Build Target Group: "  + buildTargetGroup + "\n" +
                "    Virtual Reality Supported: "  + virtualRealitySupported + "\n" +
                "");

            GUILayout.Space(5);

            if (selectedDeploymentConfiguration != currentDeploymentConfiguration) {

                if (GUILayout.Button(
                    "\nChange current deployment configuration to:\n\n" +
                    "ID: " + selectedDeploymentConfigurationID +
                    "\n" +
                    "Title: " + (string)selectedDeploymentConfiguration["title"] +
                    "\n")) {
                    EditorPrefs.SetString("Deployment_currentDeploymentConfigurationID", selectedDeploymentConfigurationID);
                }

                GUILayout.Space(5);

            } else {

                if (GUILayout.Button(
                    "\nConfigure and Deploy Current Deployment Configuration\n")) {
                    DeploymentBuilder.ConfigureDeployment(currentDeploymentConfigurationID, true, false);
                }

                GUILayout.Space(5);

            }

        }

        GUILayout.Space(5);

        GUILayout.Label(
            (currentDeploymentConfiguration == null)
                ? "All Deployment Configurations:"
                : "Deployment Configuration for " + currentDeploymentConfigurationID + ":");

        scrollPos2 = 
            EditorGUILayout.BeginScrollView(
                scrollPos2, 
                GUILayout.ExpandHeight(true),
                GUILayout.MaxHeight(maxScrollViewHeight2));

        string json = 
            (currentDeploymentConfiguration != null)
                ? JsonConvert.SerializeObject(
                    currentDeploymentConfiguration, 
                    Formatting.Indented)
                : ((deploymentConfigurations != null)
                    ? JsonConvert.SerializeObject(
                        deploymentConfigurations, 
                        Formatting.Indented)
                    : "undefined");

        GUILayout.Label(
            json);

        EditorGUILayout.EndScrollView();

        GUILayout.FlexibleSpace();
    }


    private static Texture2D MakeTex(int width, int height, Color col)
    {
        var pix = new Color[width*height];

        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        var result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();

        return result;
    }


}


}


////////////////////////////////////////////////////////////////////////
