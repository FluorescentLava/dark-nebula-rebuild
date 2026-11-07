using System;
using UnityEngine;

// Append new events so existing serialized/numeric callers keep their values.
public enum HapticEvent { Damage, Jump, Pickup, Spawn, Finish, Score, Star, CriticalPickup, Death, Spring, FinalStar }

public static class GameHaptics
{
    sealed class Pattern
    {
        internal readonly int Priority;
        internal readonly long[] Timing, FallbackTiming;
        internal readonly int[] Strength;
        internal Pattern(int priority, long[] timing, int[] strength, long[] fallbackTiming)
        {
            Priority = priority;
            Timing = timing;
            Strength = strength;
            FallbackTiming = fallbackTiming;
        }
    }

    // Motors without amplitude control need time to start. Their shorter echoes
    // convey decay through pulse duration instead of unsupported amplitude values.
    static readonly Pattern pickup = new Pattern(20,
        new long[] { 0, 18 }, new int[] { 0, 95 }, new long[] { 0, 32 });
    static readonly Pattern criticalPickup = new Pattern(40,
        new long[] { 0, 45 }, new int[] { 0, 180 }, new long[] { 0, 55 });
    static readonly Pattern spring = new Pattern(50,
        new long[] { 0, 32, 24, 22, 30, 15 }, new int[] { 0, 230, 0, 145, 0, 80 },
        new long[] { 0, 42, 28, 28, 34, 20 });
    static readonly Pattern spawn = new Pattern(30,
        new long[] { 0, 25, 40, 40 }, new int[] { 0, 100, 0, 160 },
        new long[] { 0, 35, 45, 45 });
    static readonly Pattern finish = new Pattern(70,
        new long[] { 0, 40, 55, 65 }, new int[] { 0, 160, 0, 230 },
        new long[] { 0, 45, 55, 80 });
    // Adjacent on segments retain one uninterrupted 15 ms pulse. Some OEMs
    // replace a single short segment with the same medium click at every amplitude.
    static readonly Pattern score = new Pattern(10,
        new long[] { 0, 7, 8 }, new int[] { 0, 200, 200 }, new long[] { 0, 15 });
    static readonly Pattern star = new Pattern(60,
        new long[] { 0, 7, 8 }, new int[] { 0, 230, 230 }, new long[] { 0, 15 });
    static readonly Pattern finalStar = new Pattern(70,
        new long[] { 0, 7, 8 }, new int[] { 0, 255, 255 }, new long[] { 0, 15 });
    static readonly Pattern damage = new Pattern(80,
        new long[] { 0, 90 }, new int[] { 0, 230 }, new long[] { 0, 110 });
    static readonly Pattern death = new Pattern(100,
        new long[] { 0, 650 }, new int[] { 0, 255 }, new long[] { 0, 650 });

    static float lastPickup = float.NegativeInfinity;
    static float lastCriticalPickup = float.NegativeInfinity;
    static float lastScore = float.NegativeInfinity;
    static float activeUntil;
    static int activePriority;
    static bool paused;
    static VibrationMode observedMode;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
    static GameHapticsLifecycle lifetime;
#endif

    static GameHaptics()
    {
        observedMode = GamePreferences.Vibration;
        GamePreferences.Changed += PreferencesChanged;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Cancel();
#if UNITY_ANDROID && !UNITY_EDITOR
        ReleaseAndroid();
        warned = 0;
#endif
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        lifetime = null;
#endif
        paused = false;
        GamePreferences.Changed -= PreferencesChanged;
        observedMode = GamePreferences.Vibration;
        GamePreferences.Changed += PreferencesChanged;
    }

    static void PreferencesChanged()
    {
        VibrationMode mode = GamePreferences.Vibration;
        if (mode == observedMode) return;
        observedMode = mode;
        Cancel();
    }

    static void EnsureLifetime()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        if (lifetime != null) return;
        var owner = new GameObject("GameHapticsLifecycle");
        owner.hideFlags = HideFlags.HideInHierarchy;
        UnityEngine.Object.DontDestroyOnLoad(owner);
        lifetime = owner.AddComponent<GameHapticsLifecycle>();
#endif
    }

    public static void Play(HapticEvent kind)
    {
        VibrationMode mode = GamePreferences.Vibration;
        if (paused || mode == VibrationMode.Disabled) return;
        EnsureLifetime();
        if (mode == VibrationMode.Original)
        {
            // Gameplay retains the original damage/death call sites and guards.
            if (kind == HapticEvent.Damage || kind == HapticEvent.Death)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                InitializeAndroid(); // Retain a handle so pause/mode changes can cancel it.
                nativeMayBeRunning = true;
#endif
                Handheld.Vibrate();
            }
            return;
        }

        float now = Time.realtimeSinceStartup;
        Pattern pattern = PatternFor(kind);
        // Android replaces an active effect on every vibrate call. Only a more
        // important event may interrupt; a death always gets its full duration.
        if (now < activeUntil && pattern.Priority <= activePriority) return;
        if (kind == HapticEvent.CriticalPickup && now - lastCriticalPickup < .075f) return;
        if (kind == HapticEvent.Pickup && now - lastPickup < .075f) return;
        if (kind == HapticEvent.Score && now - lastScore < .188f) return;

        float duration = 0f;
#if UNITY_ANDROID && !UNITY_EDITOR
        duration = PlayAndroid(pattern);
#elif UNITY_IOS && !UNITY_EDITOR
        if (kind == HapticEvent.Score)
        {
            DarkNebulaScoreTick();
            duration = .015f;
        }
        else if (kind == HapticEvent.Star || kind == HapticEvent.FinalStar)
        {
            DarkNebulaStarAward(kind == HapticEvent.FinalStar ? 1f : .9f);
            duration = .015f;
        }
        else
        {
            Handheld.Vibrate();
            duration = .65f;
        }
#endif
        // A failed/unsupported backend must not consume an event's cooldown.
        if (duration <= 0f) return;
        activePriority = pattern.Priority;
        activeUntil = now + duration;
        if (kind == HapticEvent.CriticalPickup) lastCriticalPickup = lastPickup = now;
        if (kind == HapticEvent.Pickup) lastPickup = now;
        if (kind == HapticEvent.Score) lastScore = now;
    }

#if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    static extern void DarkNebulaScoreTick();
    [System.Runtime.InteropServices.DllImport("__Internal")]
    static extern void DarkNebulaStarAward(float intensity);
#endif

    // A score ending must not cancel a star, damage or another stronger event.
    public static void StopScore()
    {
        if (activePriority == score.Priority) Cancel();
    }

    static Pattern PatternFor(HapticEvent kind)
    {
        switch (kind)
        {
            case HapticEvent.Pickup: return pickup;
            case HapticEvent.CriticalPickup: return criticalPickup;
            case HapticEvent.Jump:
            case HapticEvent.Spring: return spring;
            case HapticEvent.Spawn: return spawn;
            case HapticEvent.Finish: return finish;
            case HapticEvent.Score: return score;
            case HapticEvent.Star: return star;
            case HapticEvent.FinalStar: return finalStar;
            case HapticEvent.Death: return death;
            default: return damage;
        }
    }

    static float Duration(long[] timing)
    {
        long milliseconds = 0;
        for (int i = 0; i < timing.Length; ++i) milliseconds += timing[i];
        return milliseconds / 1000f;
    }

    public static void Cancel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (vibrator == null && nativeMayBeRunning) InitializeAndroid();
        try { if (vibrator != null) vibrator.Call("cancel"); }
        catch (AndroidJavaException exception)
        {
            WarnOnce(16, "cancel failed; the device service will be reacquired", exception);
            ReleaseAndroid();
        }
        nativeMayBeRunning = false;
#endif
        activePriority = 0;
        activeUntil = 0f;
        lastPickup = lastCriticalPickup = lastScore = float.NegativeInfinity;
    }

    internal static void ApplicationPaused(bool value)
    {
        // Unity may send an initial false callback when the helper is created.
        // It must not cancel the event that caused that creation.
        if (paused == value) return;
        Cancel();
#if UNITY_ANDROID && !UNITY_EDITOR
        ReleaseAndroid();
#endif
        paused = value;
    }

    internal static void ApplicationQuit()
    {
        ApplicationPaused(true);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject vibrator, gameAttributes;
    static int sdk, warned;
    static bool initialized, hasVibrator, amplitudeControl, legacyOnly, nativeMayBeRunning;

    static bool InitializeAndroid()
    {
        if (initialized) return hasVibrator;
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                sdk = version.GetStatic<int>("SDK_INT");
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (activity == null) return false; // Activity can be absent during a lifecycle transition.
                if (sdk >= 31)
                {
                    try
                    {
                        using (var manager = activity.Call<AndroidJavaObject>("getSystemService", "vibrator_manager"))
                            if (manager != null) vibrator = manager.Call<AndroidJavaObject>("getDefaultVibrator");
                    }
                    catch (AndroidJavaException exception)
                    {
                        WarnOnce(1, "VibratorManager unavailable; trying the vibrator service", exception);
                    }
                }
                if (vibrator == null) vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            }
            if (vibrator == null) return false;
            hasVibrator = vibrator.Call<bool>("hasVibrator");
            if (hasVibrator && sdk >= 26)
            {
                try { amplitudeControl = vibrator.Call<bool>("hasAmplitudeControl"); }
                catch (AndroidJavaException exception)
                {
                    WarnOnce(2, "amplitude capability unavailable; using timed pulses", exception);
                    amplitudeControl = false;
                }
            }
            if (hasVibrator && sdk >= 21)
            {
                // Without an explicit usage Android can classify a short pickup
                // as TOUCH and drop it when keyboard/touch feedback is disabled.
                // Gameplay effects belong to GAME (mapped to vibration MEDIA).
                using (var attributes = new AndroidJavaClass("android.media.AudioAttributes"))
                using (var builder = new AndroidJavaObject("android.media.AudioAttributes$Builder"))
                using (var configured = builder.Call<AndroidJavaObject>("setUsage", attributes.GetStatic<int>("USAGE_GAME")))
                    gameAttributes = configured.Call<AndroidJavaObject>("build");
            }
            // A failed acquisition remains retryable on the next event.
            initialized = true;
            return hasVibrator;
        }
        catch (AndroidJavaException exception)
        {
            WarnOnce(4, "device initialization failed; service lookup will be retried", exception);
            ReleaseAndroid();
            return false;
        }
    }

    static float PlayAndroid(Pattern pattern)
    {
        if (!InitializeAndroid())
        {
            if (initialized) return 0f; // Device explicitly reports that it has no motor.
            return PlayNativeFallback(pattern);
        }
        long[] timing = amplitudeControl ? pattern.Timing : pattern.FallbackTiming;
        if (sdk >= 26 && !legacyOnly)
        {
            try
            {
                using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                using (var effect = amplitudeControl
                    ? effectClass.CallStatic<AndroidJavaObject>("createWaveform", timing, pattern.Strength, -1)
                    : effectClass.CallStatic<AndroidJavaObject>("createWaveform", timing, -1))
                    vibrator.Call("vibrate", effect, gameAttributes);
                nativeMayBeRunning = false;
                return Duration(timing);
            }
            catch (AndroidJavaException exception)
            {
                WarnOnce(8, "VibrationEffect failed; using legacy timed pulses", exception);
                legacyOnly = true;
            }
        }
        try
        {
            // Never pass low-amplitude timings to the legacy full-power motor.
            if (gameAttributes != null)
                vibrator.Call("vibrate", pattern.FallbackTiming, -1, gameAttributes);
            else
                vibrator.Call("vibrate", pattern.FallbackTiming, -1);
            nativeMayBeRunning = false;
            return Duration(pattern.FallbackTiming);
        }
        catch (AndroidJavaException exception)
        {
            WarnOnce(32, "timed vibration failed; the device service will be reacquired", exception);
            ReleaseAndroid();
            return PlayNativeFallback(pattern);
        }
    }

    static float PlayNativeFallback(Pattern pattern)
    {
        // The native system pulse has a fixed long duration. Never turn a small
        // pickup or spring into a long buzz when the Java service is unavailable.
        if (pattern != damage && pattern != death) return 0f;
        Handheld.Vibrate();
        nativeMayBeRunning = true;
        return .65f;
    }

    static void ReleaseAndroid()
    {
        if (vibrator != null) vibrator.Dispose();
        if (gameAttributes != null) gameAttributes.Dispose();
        vibrator = null;
        gameAttributes = null;
        initialized = hasVibrator = amplitudeControl = legacyOnly = false;
    }

    static void WarnOnce(int flag, string message, AndroidJavaException exception)
    {
        if ((warned & flag) != 0) return;
        warned |= flag;
        Debug.LogWarning("GameHaptics: " + message + ". " + exception.Message);
    }
#endif
}
