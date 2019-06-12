////////////////////////////////////////////////////////////////////////
// DeploymentBuilder.cs
// By Don Hopkins.
// Copyright (C) 2019 by Don Hopkins.


////////////////////////////////////////////////////////////////////////
// Notes:


// https://github.com/ludo6577/VrMultiplatform


////////////////////////////////////////////////////////////////////////


#pragma warning disable 0414
#pragma warning disable 0219
#pragma warning disable 0168


////////////////////////////////////////////////////////////////////////


using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;


////////////////////////////////////////////////////////////////////////


namespace UnityJS {


public class DeploymentBuilder : MonoBehaviour {


    ////////////////////////////////////////////////////////////////////////
    // Static Class Variables


    private static string deploymentConfigurationsFileName = "Config/DeploymentConfigurations";
    private static JArray deploymentConfigurations = null;


    ////////////////////////////////////////////////////////////////////////
    // Static Methods


    public static bool GetInDeployment(out string deploymentID, out string deployableApplicationDataPath)
    {
        string deploymentsDir = "/Deployments/";
        string applicationDataPath = Application.dataPath;
        int deploymentsDirStart = applicationDataPath.IndexOf(deploymentsDir);
        bool inDeployment = deploymentsDirStart >= 0;

        if (inDeployment) {

            int deploymentNameStart = deploymentsDirStart + deploymentsDir.Length;
            int deploymentNameEnd = applicationDataPath.IndexOf("/", deploymentNameStart);
            int deploymentNameLength =
                (deploymentNameEnd == -1)
                    ? (applicationDataPath.Length - deploymentNameStart)
                    : (deploymentNameEnd - deploymentNameStart);
            deploymentID =
                applicationDataPath.Substring(
                    deploymentNameStart, 
                    deploymentNameLength);

            deployableApplicationDataPath = applicationDataPath.Substring(0, deploymentNameStart) + "/UnityJS/Assets/";

        } else {

            deploymentID = "";
            deployableApplicationDataPath = applicationDataPath;

        }

        return inDeployment;
    }


    public static JObject FindDeployment(string configurationID)
    {
        LoadDeploymentConfigurations();

        if (deploymentConfigurations == null) {
            Debug.LogError("DeploymentBuilder: FindDeployment: missing deploymentConfigurations!");
            return null;
        }

        JObject config = null;

        foreach (JObject deploymentConfiguration in deploymentConfigurations) {
            string deploymentConfigurationID = (string)deploymentConfiguration["id"];
            if (deploymentConfigurationID == configurationID) {
                config = deploymentConfiguration;
                return config;
            }
        }

        Debug.LogError("DeploymentBuilder: FindDeployment: unknown configurationID: " + configurationID);

        return null;
    }


    public static void ConfigureDeployment(string configurationID, bool deploy=false, bool build=false)
    {
        string deploymentID;
        string deployableApplicationDataPath;
        bool inDeployment = 
            DeploymentBuilder.GetInDeployment(
                out deploymentID,
                out deployableApplicationDataPath);

        JObject config = FindDeployment(configurationID);
        if (config == null) {
            return;
        }

        // Configure scenes, and load initial scene.

        string[] scenes = config["scenes"].ToObject<string[]>();
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: scenes: " + scenes);
        if (scenes == null) {
            Debug.LogError("DeploymentBuilder: ConfigureDeployment: scenes missing from config: "  + config);
            return;
        }

        // Make a list of EditorBuilderSettingsScenes for the EditorBuildingSettings.scenes array.

        List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();
        foreach (string scenePath in scenes)
        {
            EditorBuildSettingsScene editorBuildSettingsScene = new EditorBuildSettingsScene(scenePath, true);
            editorBuildSettingsScenes.Add(editorBuildSettingsScene);
            Debug.Log("editorBuildSettingsScene: " + editorBuildSettingsScene + " scenePath: " + scenePath);
        }

        // Set the EditorBuildingSettings.scenes array.

        EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
        //Debug.Log("EditorBuildSettings.scenes: " + EditorBuildSettings.scenes);

        // Open the initial scene.

        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(scenes[0]);
        if (scene == null) {
            Debug.LogError("DeploymentBuilder: ConfigureDeployment: can't open scene: "  + scenes[0]);
            return;
        }

        // Fish out the Bridge.

        GameObject bridgeObj = GameObject.Find("Bridge");
        if (bridgeObj == null) {
            Debug.LogError("DeploymentBuilder: ConfigureDeployment: Can't find Bridge GameObject in scene: " + scenes[0]);
            return;
        }

        // Configure the Booter, if present.

        Booter booter =
            bridgeObj.GetComponent<Booter>();
        if (booter != null) {

            Undo.RecordObject(bridgeObj, "Configure Bridge");
            EditorUtility.SetDirty(booter);

            string bootConfigurationsKey = (string)config["bootConfigurationsKey"];
            //Debug.Log("DeploymentBuilder: ConfigureDeployment: bootConfigurationsKey: " + bootConfigurationsKey);
            if (bootConfigurationsKey == null) {
                bootConfigurationsKey = "";
            }
            booter.bootConfigurationsKey = bootConfigurationsKey;

        }

        // Configure the Bridge, which must be present.

        Bridge bridge =
            bridgeObj.GetComponent<Bridge>();
        if (bridge == null) {
            Debug.LogError("DeploymentBuilder: ConfigureDeployment: Can't find Bridge component on Bridge GameObject bridgeObj: " + bridgeObj);
            return;
        }

        Undo.RecordObject(bridgeObj, "Configure Bridge");
        EditorUtility.SetDirty(bridge);

        string deployment = (string)config["deployment"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployment: " + deployment);
        bridge.deployment = deployment;

        string title = (string)config["title"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: title: " + title);
        bridge.title = title;

        string gameID = (string)config["gameID"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: gameID: " + gameID);
        bridge.gameID = gameID;

        string url = (string)config["url"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: url: " + url);
        bridge.url = url;

        string spreadsheetID = (string)config["spreadsheetID"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: spreadsheetID: " + spreadsheetID);
        bridge.spreadsheetID = spreadsheetID;

        string configuration = (string)config["configuration"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: configuration: " + configuration);
        bridge.configuration = configuration;

#if USE_SOCKETIO && UNITY_EDITOR

        bool useSocketIO = (bool)config["useSocketIO"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: useSocketIO: " + useSocketIO);
        bridge.useSocketIO = useSocketIO;

        string socketIOAddress = (string)config["socketIOAddress"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: socketIOAddress: " + socketIOAddress);
        bridge.socketIOAddress = socketIOAddress;

#endif

        // Configure the PlayerSettings.

        string productName = (string)config["productName"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: productName: " + productName);
        PlayerSettings.productName = productName;

        BuildTargetGroup buildTargetGroup = Bridge.ToEnum<BuildTargetGroup>(config["buildTargetGroup"]);
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: buildTargetGroup: " + buildTargetGroup);

        string bundleIdentifier = (string)config["bundleIdentifier"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: bundleIdentifier: " + bundleIdentifier);
        PlayerSettings.SetApplicationIdentifier(buildTargetGroup, bundleIdentifier);

        string defineSymbols = (string)config["defineSymbols"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: defineSymbols: " + defineSymbols);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defineSymbols);

        string bundleVersion = (string)config["bundleVersion"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: bundleVersion: " + bundleVersion);
        PlayerSettings.bundleVersion = bundleVersion;

        bool virtualRealitySupported = (bool)config["virtualRealitySupported"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: virtualRealitySupported: " + virtualRealitySupported);
        PlayerSettings.virtualRealitySupported = virtualRealitySupported;

        // Configure the PlayerSettings for the build target.

        BuildTarget buildTarget = Bridge.ToEnum<BuildTarget>(config["buildTarget"]);
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: buildTarget: " + buildTarget);

        switch (buildTarget) {

            case BuildTarget.WebGL: {

                string webGLTemplate = (string)config["webGLTemplate"];
                //Debug.Log("DeploymentBuilder: ConfigureDeployment: webGLTemplate: " + webGLTemplate);
                PlayerSettings.WebGL.template = webGLTemplate;

                break;
            }

            case BuildTarget.iOS: {

                string buildNumber = (string)config["buildNumber"];
                //Debug.Log("DeploymentBuilder: ConfigureDeployment: buildNumber: " + buildNumber);
                PlayerSettings.iOS.buildNumber = buildNumber;

                break;
            }

            case BuildTarget.Android: {

                string sdk = 
                    Environment.GetEnvironmentVariable(
                        //"ANDROID_SDK_ROOT"
                        "ANDROID_HOME"
                    );

                if (!string.IsNullOrEmpty(sdk)) {
                    EditorPrefs.SetString("AndroidSdkRoot", sdk);
                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: Android sdk: " + sdk);
                }

                int bundleVersionCode = (int)config["bundleVersionCode"];
                //Debug.Log("DeploymentBuilder: ConfigureDeployment: bundleVersionCode: " + bundleVersionCode);
                PlayerSettings.Android.bundleVersionCode = bundleVersionCode;

                string keystorePath = (string)config["keystorePath"];
                //Debug.Log("DeploymentBuilder: ConfigureDeployment: keystorePath: " + keystorePath);
                PlayerSettings.Android.keystoreName = keystorePath;

                string keyaliasName = (string)config["keyaliasName"];
                //Debug.Log("DeploymentBuilder: ConfigureDeployment: keyaliasName: " + keyaliasName);
                PlayerSettings.Android.keyaliasName = keyaliasName;

                string keyaliasPass = (string)config["keyaliasPass"];
                //Debug.Log("DeploymentBuilder: ConfigureDeployment: keyaliasPass: " + keyaliasPass);
                PlayerSettings.Android.keyaliasPass = keyaliasPass;

                break;
            }

            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64: {
                break;
            }

            case BuildTarget.StandaloneOSXIntel:
            case BuildTarget.StandaloneOSXIntel64:
            case BuildTarget.StandaloneOSX: {
                break;
            }

            default: {
                Debug.LogError("DeploymentBuilder: ConfigureDeployment: Unknown buildTarget: " + buildTarget);
                return;
            }

        } // switch buildTarget

        EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

        // Make sure all the changes are saved.

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        // Copy files around in the deployable project.

        JArray copyFiles = (JArray)config["copyFiles"];
        //Debug.Log("DeploymentBuilder: ConfigureDeployment: copyFiles: " + copyFiles);

        if (copyFiles != null) {

            foreach (JArray fromToPaths in copyFiles) {

                string sourcePath = 
                    Path.GetFullPath(
                        deployableApplicationDataPath + 
                        "/" + 
                        (string)fromToPaths[0]);

                string destPath = 
                    Path.GetFullPath(
                        deployableApplicationDataPath + 
                        "/" + 
                        (string)fromToPaths[1]);

                CopyTree(sourcePath, destPath, false);

            }

            AssetDatabase.Refresh();

        } // if copyFiles != null

        // Deploy this configuration if deploy is enabled.

        if (deploy) {

            if (deployment == null) {
                Debug.Log("DeploymentBuilder: ConfigureDeployment: missing deployment! config: " + config.ToString());
                return;
            }

            string deploymentsDirectory = (string)config["deploymentsDirectory"];
            //Debug.Log("DeploymentBuilder: ConfigureDeployment: deploymentsDirectory: " + deploymentsDirectory);

            string rootPath =
                Path.GetFullPath(
                    deployableApplicationDataPath + 
                    "/..");

            string deploymentPath = 
                Path.GetFullPath(
                    rootPath + 
                    deploymentsDirectory + 
                    deployment);

            //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployment: " + deployment + " deploymentPath: " + deploymentPath);

            // Clean out the deployment only if we're in the deployable project.

            if (!inDeployment) {

#if false
                if (Directory.Exists(deploymentPath)) {

                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: deleting old directory deploymentPath: " + deploymentPath);
                    Directory.Delete(deploymentPath, true);

                }

                //Debug.Log("DeploymentBuilder: ConfigureDeployment: Creating new directory deploymentPath: " + deploymentPath);
                Directory.CreateDirectory(deploymentPath);

#endif

                JArray deployClean = (JArray)config["deployClean"];
                //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployClean: " + deployClean);

                if (deployClean != null) {

                    foreach (string item in deployClean) {

                        string destPath = deploymentPath + "/" + item;
                        //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployClean: item: " + item);

                        if (File.Exists(destPath)) {

                            //Debug.Log("DeploymentBuilder: ConfigureDeployment: Cleaning file: " + destPath);
                            File.Delete(destPath);

                        } else if (Directory.Exists(destPath)) {

                            //Debug.Log("DeploymentBuilder: ConfigureDeployment: Cleaning directory: " + destPath);
                            Directory.Delete(destPath, true);

                        }

                        string destPathMeta = destPath + ".meta";
                        //Debug.Log("DeploymentBuilder: ConfigureDeployment: destMetaPath: " + destMetaPath);

                        if (File.Exists(destPathMeta)) {

                            //Debug.Log("DeploymentBuilder: ConfigureDeployment: Cleaning file: " + destPathMeta);
                            File.Delete(destPathMeta);

                        } else if (Directory.Exists(destPathMeta)) {

                            //Debug.Log("DeploymentBuilder: ConfigureDeployment: Cleaning directory: " + destPathMeta);
                            Directory.Delete(destPathMeta, true);

                        }

                    }

                }

            }

            // Create directories in the deployment project.

            JArray deployCreateDirectories = (JArray)config["deployCreateDirectories"];
            //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployCreateDirectories: " + deployCreateDirectories);

            if (deployCreateDirectories != null) {

                foreach (string item in deployCreateDirectories) {

                    string sourcePath;
                    string destPath;

                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployCreateDirectories: item: " + item);

                    sourcePath = 
                        rootPath +
                        "/" +
                        item;

                    destPath = 
                        deploymentPath + 
                        "/" + 
                        item;

                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: Creating directory destPath: " + destPath);
                    if (!Directory.Exists(destPath)) {
                        Directory.CreateDirectory(destPath);
                    }

                    string sourceMetaPath = sourcePath + ".meta";
                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: sourceMetaPath: " + sourceMetaPath);
                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: sourceMetaPath exists: " + File.Exists(sourceMetaPath));
                    string destMetaPath = destPath + ".meta";
                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: destMetaPath: " + destMetaPath);

                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployCreateDirectories: sourceMetaPath: " + sourceMetaPath + " destMetaPath: " + destMetaPath + " source file exists: " + File.Exists(sourceMetaPath));

                    if (File.Exists(sourceMetaPath)) {

                        string sourceMetaPathRelative = RelativeLinkPath(sourceMetaPath, destMetaPath);

                        //Debug.Log("DeploymentBuilder: ConfigureDeployment: Copying directory meta file sourceMetaPath: " + sourceMetaPath + " sourceMetaPathRelative: " + sourceMetaPathRelative + " to destMetaPath: " + destMetaPath);

                        MakeSymbolicLink(sourceMetaPathRelative, destMetaPath);
                    }

                }

            }

            // Copy files into the deployment project.

            JArray deployCopyFiles = (JArray)config["deployCopyFiles"];
            //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployCopyFiles: " + deployCopyFiles);

            if (deployCopyFiles != null) {

                foreach (JToken item in deployCopyFiles) {

                    string sourcePath;
                    string destPath;

                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployCopyFiles: item: " + item);

                    string itemString = (string)item;
                    JArray itemArray = (item is JArray) ? (JArray)item : null;

                    if (itemString != null) {

                        sourcePath = 
                            rootPath +
                            "/" + 
                            itemString;

                        destPath = 
                            deploymentPath + 
                            "/" + 
                            itemString;

                    } else if (itemArray != null) {

                        sourcePath = 
                            rootPath +
                            "/" + 
                            (string)itemArray[0];

                        destPath = 
                            deploymentPath + 
                            "/" + 
                            (string)itemArray[1];

                    } else {
                        Debug.LogError("DeploymentBuilder: ConfigureDeployment: deployCopyFiles: invalid item: " + item);
                        return;
                    }

                    //Debug.Log("DeploymentBuilder: deployCopyFile: sourcePath: " + sourcePath + " destPath: " + destPath);
                    CopyTree(sourcePath, destPath, true);

                }

            }

            // Symlink files into the deployment project.

            JArray deployLinkFiles = (JArray)config["deployLinkFiles"];
            //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: " + deployLinkFiles);

            if (deployLinkFiles != null) {

                foreach (JToken item in deployLinkFiles) {

                    string sourcePath;
                    string destPath;

                    JArray itemArray = (item is JArray) ? (JArray)item : null;
                    string itemString = (itemArray == null) ? (string)item : null;

                    //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: item: " + item.Type + " " + item + " itemString: " + itemString + " itemArray: " + itemArray);

                    if (itemString != null) {

                        sourcePath = 
                            rootPath +
                            "/" + 
                            itemString;

                        destPath =
                            deploymentPath + 
                            "/" + 
                            itemString;

                    } else if (itemArray != null) {

                        sourcePath =
                            rootPath +
                            "/" + 
                            (string)itemArray[0];

                        destPath = 
                            deploymentPath + 
                            "/" + 
                            (string)itemArray[1];

                    } else {
                        Debug.LogError("DeploymentBuilder: ConfigureDeployment: invalid item: " + item);
                        return;
                    }

                    if (Directory.Exists(destPath)) {

                        //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: deleting existing directoy destPath: " + destPath);
                        Directory.Delete(destPath, true);

                    } else if (File.Exists(destPath)) {

                        //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: deleting existing file destPath: " + destPath);
                        File.Delete(destPath);

                    }

                    if (Directory.Exists(sourcePath) ||
                        File.Exists(sourcePath))  {

                        string sourcePathRelative = RelativeLinkPath(sourcePath, destPath);

                        string destParentPath = Directory.GetParent(destPath).FullName;
                        bool destParentDirectoryExists = Directory.Exists(destParentPath);

                        //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: destPath: " + destPath + " destParentPath: " + destParentPath + " destParentDirectoryExists: " + destParentDirectoryExists);

                        if (!destParentDirectoryExists) {

                            // TODO: Link .meta files in intermediate created directories.
                            Directory.CreateDirectory(destParentPath);

                        }

                        //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: linking sourcePath: " + sourcePath + " sourcePathRelative: " + sourcePathRelative + " to destPath " + destPath);

                        MakeSymbolicLink(sourcePathRelative, destPath);

                        string sourceMetaPath = sourcePath + ".meta";
                        string sourceMetaPathRelative = sourcePathRelative + ".meta";
                        bool sourceMetaFileExists = File.Exists(sourceMetaPath);

                        //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: sourceMetaPath: " + sourceMetaPath + " sourceMetaPathRelative: " + sourceMetaPathRelative + " sourceMetaFileExists: " + sourceMetaFileExists);

                        if (sourceMetaFileExists) {

                            string destMetaPath = destPath + ".meta";

                            //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: copying meta file from sourceMetaPath: " + sourceMetaPath + " sourceMetaPathRelative: " + sourceMetaPathRelative + " to destMetaPath: " + destMetaPath);

                            if (Directory.Exists(destMetaPath)) {

                                //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: deleting existing meta directoy destMetaPath: " + destMetaPath);
                                Directory.Delete(destMetaPath, true);

                            } else if (File.Exists(destMetaPath)) {

                                //Debug.Log("DeploymentBuilder: ConfigureDeployment: deployLinkFiles: deleting existing meta file destMetaPath: " + destMetaPath);
                                File.Delete(destMetaPath);

                            }

                            MakeSymbolicLink(sourceMetaPathRelative, destMetaPath);

                        }

                    } else {
                        Debug.LogError("DeploymentBuilder: ConfigureDeployment: missing sourcePath: " + sourcePath);
                    }

                }

            }

        } // if deploy

        // Build this configuration if build is enabled.

        if (build) {

            BuildOptions buildOptions = Bridge.ToEnumMask<BuildOptions>(config["buildOptions"]);
            //Debug.Log("DeploymentBuilder: ConfigureDeployment: buildOptions: " + buildOptions);

            string buildLocation = (string)config["buildLocation"];
            //Debug.Log("DeploymentBuilder: ConfigureDeployment: buildLocation: " + buildLocation);

            // Switch to the build target.
            //EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);

            BuildReport buildReport =
                BuildPipeline.BuildPlayer(
                    scenes, 
                    buildLocation, 
                    buildTarget, 
                    buildOptions);

            if (buildReport.summary.result != BuildResult.Succeeded) {
                Debug.Log("DeploymentBuilder: ");
                throw new Exception("Build failed!");
            }

        } // if build

    }
    

    public static void ReloadDeploymentConfigurations()
    {
        deploymentConfigurations = null;
        LoadDeploymentConfigurations();
    }


    private static void LoadDeploymentConfigurations()
    {
        if (deploymentConfigurations != null) {
            return;
        }

        deploymentConfigurations = new JArray();

        string fileName = "Config/DeploymentConfigurations";
        UnityEngine.Object[] resources = Resources.LoadAll(fileName, typeof(TextAsset));
        Debug.Log("DeploymentBuilder: LoadDeploymentConfigurations: Found " + resources.Length + " resources for fileName: " + fileName);

        foreach (TextAsset resource in resources) {

            string text = resource.text;
            Resources.UnloadAsset(resource);

            if (string.IsNullOrEmpty(text)) {
                Debug.LogError("DeploymentBuilder: LoadDeploymentConfigurations: Resource is not TextAsset! fileName: " + fileName);
                continue;
            }

            JToken data = JToken.Parse(text);
            if (data == null) {
                Debug.LogError("DeploymentBuilder: LoadDeploymentConfigurations: Error parsing fileName: " + fileName + " text:\n" + text);
                continue;
            }

            JArray configurations = (JArray)data;
            if (configurations == null) {
                Debug.LogError("DeploymentBuilder: LoadDeploymentConfigurations: Configurations should be an array! text:\n" + text);
                continue;
            }

            foreach (JToken token in data) {
                deploymentConfigurations.Add(token);
            }

        }

    }


    public static JArray GetDeploymentConfigurations()
    {
        LoadDeploymentConfigurations();
        return deploymentConfigurations;
    }


    private static JToken LoadJSONFile(string fileName)
    {
        string text = LoadTextFile(fileName);
        JToken data = JToken.Parse(text);
        //Debug.Log("DeploymentBuilder: LoadJSONFile: fileName: " + fileName + " data: " + data);
        return data;
    }


    public static string LoadTextFile(string fileName)
    {
        string result;

        TextAsset textFile =
            Resources.Load<TextAsset>(fileName);
        result = textFile.text;
        Resources.UnloadAsset(textFile);

        return result;
    }


    public static void CopyTree(string sourcePath, string destPath, bool copyMeta)
    {
        bool sourceDirectoryExists = Directory.Exists(sourcePath);
        bool sourceFileExists = File.Exists(sourcePath);

        //Debug.Log("DeploymentBuilder: CopyTree: sourcePath: " + sourcePath + " destPath: " + destPath + " sourceDirectoryExists: " + sourceDirectoryExists + " sourceFileExists: " + sourceFileExists);

        if (!sourceDirectoryExists && !sourceFileExists) {
            Debug.LogError("DeploymentBuilder: CopyTree: missing file or directory sourcePath: " + sourcePath);
            return;
        }

        bool destDirectoryExists = Directory.Exists(destPath);
        bool destFileExists = File.Exists(destPath);

        //Debug.Log("DeploymentBuilder: CopyTree: destDirectoryExists: " + destDirectoryExists + " destFileExists: " + destFileExists);

        if (destDirectoryExists) {
            //Debug.Log("DeploymentBuilder: CopyTree: deleting old existing directory destPath: " + destPath);
            Directory.Delete(destPath, true);
        }

        if (destFileExists) {
            //Debug.Log("DeploymentBuilder: CopyTree: deleting old existing file destPath: " + destPath);
            File.Delete(destPath);
        }

        string destParentPath = Path.GetDirectoryName(destPath);
        bool destParentDirectoryExists = Directory.Exists(destParentPath);

        //Debug.Log("DeploymentBuilder: CopyTree: destParentPath: " + destParentPath + " destParentDirectoryExists: " + destParentDirectoryExists);

        if (!destParentDirectoryExists) {

            //Debug.Log("DeploymentBuilder: CopyTree: creating new destParentPath: " + destParentPath);

            // TODO: Copy .meta files in intermediate created directories.
            Directory.CreateDirectory(destParentPath);

        }

        //Debug.Log("DeploymentBuilder: CopyTree: CopyFileOrDirectory sourcePath: " + sourcePath + " destPath: " + destPath);
        FileUtil.CopyFileOrDirectory(sourcePath, destPath);

        if (copyMeta) {

            string sourceMetaPath = sourcePath + ".meta";
            bool sourceMetaFileExists = File.Exists(sourceMetaPath);

            //Debug.Log("DeploymentBuilder: CopyTree: sourceMetaPath: " + sourceMetaPath + " sourceMetaFileExists: " + sourceMetaFileExists);

            if (sourceMetaFileExists) {

                string destMetaPath = destPath + ".meta";

                //Debug.Log("DeploymentBuilder: CopyTree: copying meta file from sourceMetaPath: " + sourceMetaPath + " to destMetaPath: " + destMetaPath);

                if (Directory.Exists(destMetaPath)) {

                    //Debug.Log("DeploymentBuilder: CopyTree: deleting existing meta directoy destMetaPath: " + destMetaPath);
                    Directory.Delete(destMetaPath, true);

                } else if (File.Exists(destMetaPath)) {

                    //Debug.Log("DeploymentBuilder: CopyTree: deleting existing meta file destMetaPath: " + destMetaPath);
                    File.Delete(destMetaPath);

                }

#if true
                FileUtil.CopyFileOrDirectory(sourceMetaPath, destMetaPath);
#else

                string sourceMetaPathRelative = RelativeLinkPath(sourceMetaPath, destMetaPath);

                //Debug.Log("DeploymentBuilder: CopyTree: Linking directory meta file sourceMetaPath: " + sourceMetaPath + " sourceMetaPathRelative: " + sourceMetaPathRelative + " to destMetaPath: " + destMetaPath);

                MakeSymbolicLink(sourceMetaPathRelative, destMetaPath);
#endif

            }

        }

    }


    public static string RelativeLinkPath(string sourcePath, string destPath)
    {
        char[] separators = new char[] {
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        };

        string sourceDirPath =
            Directory.Exists(sourcePath)
                ? sourcePath
                : Path.GetDirectoryName(sourcePath);
        string[] sourcePathDirs = 
            sourceDirPath.Split(separators); // , StringSplitOptions.RemoveEmptyEntries

        string destDirPath =
            Directory.Exists(destPath)
                ? destPath
                : Path.GetDirectoryName(destPath);
        string[] destPathDirs = 
            destDirPath.Split(separators); // , StringSplitOptions.RemoveEmptyEntries

        List<string> sameDirs = new List<string>();
        int minDirs = Math.Min(sourcePathDirs.Length, destPathDirs.Length);
        int i;
        for (i = 0; i < minDirs; i++) {
            if (sourcePathDirs[i] != destPathDirs[i]) {
                break;
            }
            sameDirs.Add(sourcePathDirs[i]);
        }

        string commonPath =
            String.Join("/", sameDirs.ToArray());

        string relPath = "";
        int ups = destPathDirs.Length - i;
        for (int up = 0; up < ups; up++) {
            relPath += "../";
        }

        int downs = sourcePathDirs.Length;
        for (; i < downs; i++) {
            relPath += sourcePathDirs[i];
            if (i != (downs - 1)) {
                relPath += "/";
            }
        }

        if (!Directory.Exists(sourcePath)) {
            string[] components = sourcePath.Split(separators);
            if (components.Length > 0) {
                relPath += "/" + components[components.Length - 1];
            }
        }

        return relPath;
    }


    public static void MakeSymbolicLink(string sourcePath, string destPath)
    {
        //Debug.Log("DeploymentBuilder: MakeSymbolicLink: platform: " + Application.platform + " sourcePath: " + sourcePath + " destPath: " + destPath);

        var process = new System.Diagnostics.Process();

        if (Application.platform == RuntimePlatform.OSXEditor) {
            process.StartInfo.FileName = "ln";
            process.StartInfo.Arguments = 
                "-s " +
                QuoteShellPathParam(sourcePath) + " " +
                QuoteShellPathParam(destPath);
        } else {
            process.StartInfo.FileName = "mklink";
            process.StartInfo.Arguments =
                (Directory.Exists(destPath) ? "/d " : "") +
                QuoteShellPathParam(sourcePath) + " " +
                QuoteShellPathParam(destPath);
        }

        //Debug.Log("DeploymentBuilder: MakeSymbolicLink: FileName: " + process.StartInfo.FileName + " Arguments: " + process.StartInfo.Arguments);

        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
        process.Dispose();
    }


    static string QuoteShellPathParam(string pathParam)
    {
        return 
            ("\"" +
             pathParam.Replace("\"", "\\\"") +
             "\"");
    }


}


}


////////////////////////////////////////////////////////////////////////
