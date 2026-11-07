using UnityEngine;
using UnityEngine.SceneManagement;

// Keep a navigation gesture in its original scene until the transition finishes.
[DefaultExecutionOrder(-32000)]
public sealed class SceneTransitionInput : MonoBehaviour
{
    static bool loading;
    static bool blocking;
    static float releaseAfter;
    bool applicationPaused;
    bool focused = true;

    public static bool Blocking { get { return blocking; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { loading = blocking = false; releaseAfter = 0f; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        Input.backButtonLeavesApp = false;
        var root = new GameObject("Scene transition input");
        DontDestroyOnLoad(root);
        root.AddComponent<SceneTransitionInput>();
    }

    public static bool TryBegin()
    {
        if (blocking) return false;
        Hold();
        return true;
    }

    public static void Hold() { loading = blocking = true; }
    public static void Complete()
    {
        loading = false;
        releaseAfter = Time.realtimeSinceStartup + .2f;
    }

    void OnEnable() { SceneManager.sceneLoaded += SceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= SceneLoaded; }
    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Native levels release the lock after their entry fade, not scene activation.
        if (blocking && scene.name != "LoadLevel" && scene.name != "StubLoader" && !scene.path.Contains("/Scenes/Levels/")) Complete();
    }

    void Update()
    {
        if (!blocking) return;
        bool held = Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Menu) || Input.GetMouseButton(0) || Input.touchCount > 0;
        if (held) releaseAfter = Time.realtimeSinceStartup + .2f;
        // Legacy input is shared by all recovered controls. Consume it before their Updates.
        Input.ResetInputAxes();
        if (loading || held || Time.realtimeSinceStartup < releaseAfter) return;
        blocking = false;
#if !UNITY_EDITOR
        // Home/app switching during loading still pauses the completed level normally.
        if ((applicationPaused || !focused) && Hud.Instance != null) Hud.Instance.Pause();
#endif
    }
    void OnApplicationPause(bool paused) { applicationPaused = paused; }
    void OnApplicationFocus(bool hasFocus) { focused = hasFocus; }
}
