#if UNITY_EDITOR && UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class IOSNativePostprocessor
{
    [PostProcessBuild(100)]
    public static void LinkNativeFrameworks(BuildTarget target, string outputPath)
    {
        if (target != BuildTarget.iOS) return;
        string projectPath = PBXProject.GetPBXProjectPath(outputPath);
        var project = new PBXProject();
        project.ReadFromFile(projectPath);
        // Native plugins compile into UnityFramework, which must link the API
        // directly rather than relying on the application target's frameworks.
        project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "CoreHaptics.framework", false);
        project.WriteToFile(projectPath);
    }
}
#endif
