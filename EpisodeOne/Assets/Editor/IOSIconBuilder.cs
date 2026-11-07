#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class IOSIconBuilder : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return -1000; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.iOS) return;
        PlayerSettings.statusBarHidden = true;
        SetIcons();
    }

    public static void SetIcons()
    {
        const string path = "Assets/Icons/IOSIcon.png";
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        Texture2D artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (artwork == null) throw new BuildFailedException("Missing original iOS icon: " + path);

        int count = 0;
        foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.iOS))
        {
            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.iOS, kind);
            foreach (PlatformIcon icon in icons)
            {
                icon.SetTexture(artwork);
                count++;
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.iOS, kind, icons);
        }
        if (count == 0) throw new BuildFailedException("No iOS application icon slots are available.");
        Debug.Log("iOS icons: assigned original artwork to " + count + " slots.");
    }
}
#endif
