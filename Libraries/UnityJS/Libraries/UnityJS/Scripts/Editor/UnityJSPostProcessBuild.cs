using System.Collections;
using System.IO;
using UnityEditor.Callbacks;
using UnityEditor;
using UnityEngine;
using UnityEditor.iOS.Xcode;


public class UnityJSPostProcessBuild {

    [PostProcessBuild(100)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
    {
        Debug.Log("PostProcessBuild: OnPostProcessBuild: buildTarget: " + buildTarget + " path: " + path);

        if (buildTarget == BuildTarget.iOS) {

            string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
            PBXProject proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));
            string target = proj.TargetGuidByName("Unity-iPhone");
            proj.AddFrameworkToProject(target, "WebKit.framework", false);
            File.WriteAllText(projPath, proj.WriteToString());

        }

    }

}
