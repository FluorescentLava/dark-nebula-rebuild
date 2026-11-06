#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

// A cached DefaultAsset can retain a WAV's GUID while exposing no AudioClip.
// Repair that import before scenes resolve their music references.
[InitializeOnLoad]
public sealed class AudioClipImportIntegrity : IPreprocessBuildWithReport, IProcessSceneWithReport
{
    public int callbackOrder { get { return -1000; } }

    static AudioClipImportIntegrity()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;
        try { RepairAndValidate(); }
        catch (Exception exception)
        {
            EditorApplication.isPlaying = false;
            Debug.LogException(exception);
        }
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        RepairAndValidate();
    }

    public void OnProcessScene(Scene scene, BuildReport report)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (World world in root.GetComponentsInChildren<World>(true))
        {
            if (world.MusicClips == null)
                throw new BuildFailedException("Missing music array in " + scene.path + ": World.MusicClips");
            for (int index = 0; index < world.MusicClips.Length; index++)
                if (world.MusicClips[index] == null)
                    throw new BuildFailedException("Missing music reference in " + scene.path
                        + ": World.MusicClips[" + index + "]");
            Debug.Log("Audio scene integrity: " + scene.path + ";level=" + world.LevelNum
                + ";musicReferences=" + world.MusicClips.Length);
        }
    }

    public static void RepairAndValidate()
    {
        string directory = Path.Combine(Application.dataPath, "AudioClip");
        int checkedClips = 0;
        int repairedClips = 0;
        foreach (string file in Directory.EnumerateFiles(directory, "*.wav", SearchOption.AllDirectories))
        {
            checkedClips++;
            string path = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) != null) continue;

            // Preserve the original bytes, GUID, importer settings and scene
            // references. Only regenerate the missing imported object.
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
                throw new BuildFailedException("Audio clip import failed: " + path);

            Debug.Log("Reimported missing audio clip: " + path);
            repairedClips++;
        }
        Debug.Log("Audio import integrity: validated=" + checkedClips + ";reimported=" + repairedClips
            + ";target=" + EditorUserBuildSettings.activeBuildTarget);
    }
}
#endif
