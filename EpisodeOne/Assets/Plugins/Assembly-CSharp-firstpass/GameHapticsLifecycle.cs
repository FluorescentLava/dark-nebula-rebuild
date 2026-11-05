using UnityEngine;

// Survives scene loads; the OS service is reacquired after application resume.
public sealed class GameHapticsLifecycle : MonoBehaviour
{
    void OnApplicationPause(bool paused) { GameHaptics.ApplicationPaused(paused); }
    void OnApplicationQuit() { GameHaptics.ApplicationQuit(); }
}
