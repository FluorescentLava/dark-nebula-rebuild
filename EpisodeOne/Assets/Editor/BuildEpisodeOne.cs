#if UNITY_EDITOR && UNITY_ANDROID
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Android;
using UnityEngine;

public static class BuildEpisodeOne
{
    public static void Build()
    {
        string sdk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk");
        string ndk = Path.Combine(sdk, "ndk", "27.2.12479018");
        string managedJdk = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Toolchains/jdk17"));
        string previousWorkJdk = Path.GetFullPath(Path.Combine(Application.dataPath, "../../jdk17/jdk-17.0.20.1+1"));
        string javaExe = Application.platform == RuntimePlatform.WindowsEditor ? "java.exe" : "java";
        string jdk = new[] { managedJdk, previousWorkJdk, Environment.GetEnvironmentVariable("DARK_NEBULA_JDK17"), EditorPrefs.GetString("Jdk17Path"), Environment.GetEnvironmentVariable("JAVA_HOME") }
            .FirstOrDefault(p => !string.IsNullOrEmpty(p) && File.Exists(Path.Combine(p, "bin", javaExe)));
        if (string.IsNullOrEmpty(jdk)) throw new Exception("JDK 17 not found. Configure DARK_NEBULA_JDK17 or Unity External Tools.");
        var keys = new[] { "AndroidSdkRoot", "AndroidNdkRootR27C", "Jdk17Path" };
        var previous = keys.Select(x => EditorPrefs.GetString(x)).ToArray();
        var boolKeys = new[] { "SdkUseEmbedded", "NdkUseEmbedded", "JdkUseEmbedded" };
        var previousBool = boolKeys.Select(x => EditorPrefs.GetBool(x, true)).ToArray();
        bool previousExport = EditorUserBuildSettings.exportAsGoogleAndroidProject;
        string previousIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        bool exportProject = Array.IndexOf(Environment.GetCommandLineArgs(), "-exportGradle") >= 0;
        bool development = Array.IndexOf(Environment.GetCommandLineArgs(), "-development") >= 0;
        bool validation = Array.IndexOf(Environment.GetCommandLineArgs(), "-validation") >= 0;
        if (validation && !development) throw new Exception("Validation packages must be Development builds.");
        try
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = exportProject;
            UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = sdk;
            UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = ndk;
            UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = jdk;
            PlayerSettings.productName = "Dark Nebula Episode One";
            PlayerSettings.bundleVersion = "0.3.6";
            PlayerSettings.SplashScreen.show = false;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            LauncherIconBuilder.SetIcons();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, validation ? "com.darknebula.validation.one" : "com.codex.darknebula.episode1.test");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = 17;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel35;
            var scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            string fileName = "DarkNebula-Episode1-" + PlayerSettings.bundleVersion
                + (development ? "-Development" : "-Android64") + ".apk";
            var output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Builds", fileName));
            if (exportProject) output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../android-final-exports/episode1"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            Debug.Log("BUILD scenes=" + scenes.Length + " output=" + output);
            var result = BuildPipeline.BuildPlayer(scenes, output, BuildTarget.Android,
                development ? BuildOptions.Development : BuildOptions.None);
            Debug.Log("BUILD_RESULT " + result.summary.result + " errors=" + result.summary.totalErrors + " warnings=" + result.summary.totalWarnings);
            if (result.summary.result != BuildResult.Succeeded) throw new Exception("Android build did not succeed");
        }
        finally
        {
            EditorUserBuildSettings.exportAsGoogleAndroidProject = previousExport;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, previousIdentifier);
            AssetDatabase.SaveAssets();
            for (int i=0; i<keys.Length; i++) EditorPrefs.SetString(keys[i], previous[i]);
            for (int i=0; i<boolKeys.Length; i++) EditorPrefs.SetBool(boolKeys[i], previousBool[i]);
        }
    }

}
#endif
