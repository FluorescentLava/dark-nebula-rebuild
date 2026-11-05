using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// The original iOS films store their portrait picture sideways in a 480x320
// frame. Its native player rotated the view by +pi/2; keep the files unchanged.
public sealed class StoryMoviePlayer : MonoBehaviour
{
    public const string Intro = "intro.m4v";
    public const string Extro = "extro.m4v";

    public enum Result { Pending, Completed, Skipped, Failed, Cancelled }

    public sealed class Playback
    {
        internal StoryMoviePlayer Player;
        public Result Outcome { get; internal set; }
        public bool HasStarted { get; internal set; }
        public string Error { get; internal set; }
        public bool IsDone { get { return Outcome != Result.Pending; } }
        public bool WasHandled { get { return Outcome == Result.Completed || Outcome == Result.Skipped; } }

        public void Cancel()
        {
            if (Player != null) Player.Finish(Result.Cancelled);
        }
    }

    private static StoryMoviePlayer active;
    private Playback playback;
    private MonoBehaviour owner;
    private VideoPlayer video;
    private AudioSource soundtrack;
    private RawImage picture;
    private bool allowSkip;
    private bool globalsCaptured;
    private bool previousAudioPause;
    private float previousTimeScale;
    private int previousSleepTimeout;
    private bool applicationPaused;
    private bool applicationFocused = true;
    private bool suspended;
    private double startedAt;
    private double lastProgressAt;
    private double firstFrameAt;
    private long lastFrame = -1;

    public Playback CurrentPlayback { get { return playback; } }

    public static Playback Play(MonoBehaviour owner, string filename, bool allowedToSkip = true)
    {
        var request = new Playback();
        if (owner == null || !owner.isActiveAndEnabled || active != null)
        {
            request.Outcome = Result.Failed;
            request.Error = "A movie needs an active owner and exclusive playback.";
            return request;
        }

        // An overlay canvas must be a scene root, independent of the menu's
        // transforms and of any camera viewport or IMGUI clipping state.
        var container = new GameObject("StoryMovie", typeof(RectTransform));
        var player = container.AddComponent<StoryMoviePlayer>();
        active = player;
        request.Player = player;
        player.playback = request;
        player.owner = owner;
        player.allowSkip = allowedToSkip;
        try { player.Begin(filename); }
        catch (Exception exception) { player.Fail(exception.Message); }
        return request;
    }

    private void Begin(string filename)
    {
        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousSleepTimeout = Screen.sleepTimeout;
        globalsCaptured = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        startedAt = lastProgressAt = Time.realtimeSinceStartupAsDouble;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var background = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(transform, false);
        background.GetComponent<Image>().color = Color.black;
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = backgroundRect.offsetMax = Vector2.zero;
        var frame = new GameObject("Picture", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        frame.transform.SetParent(transform, false);
        picture = frame.GetComponent<RawImage>();
        picture.raycastTarget = false;
        picture.enabled = false;
        picture.rectTransform.anchorMin = picture.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        picture.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);

        soundtrack = gameObject.AddComponent<AudioSource>();
        soundtrack.playOnAwake = false;
        soundtrack.ignoreListenerPause = true;
        soundtrack.spatialBlend = 0f;
        // Each original film has one mixed soundtrack. Respect the all-audio-off
        // combination without changing either stored music/effects preference.
        soundtrack.mute = FLD.Audio.IsMusicMuted && FLD.Audio.IsSoundEffectsMuted;

        video = gameObject.AddComponent<VideoPlayer>();
        video.playOnAwake = false;
        video.source = VideoSource.Url;
        video.url = Application.streamingAssetsPath.TrimEnd('/', '\\') + "/Story/" + filename;
        video.renderMode = VideoRenderMode.APIOnly;
        video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        video.waitForFirstFrame = true;
        video.isLooping = false;
        video.skipOnDrop = true;
        video.audioOutputMode = VideoAudioOutputMode.AudioSource;
        video.controlledAudioTrackCount = 1;
        video.EnableAudioTrack(0, true);
        video.SetTargetAudioSource(0, soundtrack);
        video.sendFrameReadyEvents = true;
        video.prepareCompleted += OnPrepared;
        video.frameReady += OnFrameReady;
        video.loopPointReached += OnEnded;
        video.errorReceived += OnError;
        Debug.Log("Story movie preparing: " + filename);
        video.Prepare();
    }

    private void OnPrepared(VideoPlayer source)
    {
        if (playback.IsDone) return;
        lastProgressAt = Time.realtimeSinceStartupAsDouble;
        if (!suspended) source.Play();
    }

    private void OnFrameReady(VideoPlayer source, long frame)
    {
        if (playback.IsDone) return;
        lastProgressAt = Time.realtimeSinceStartupAsDouble;
        lastFrame = frame;
        if (!playback.HasStarted)
        {
            playback.HasStarted = true;
            firstFrameAt = lastProgressAt;
            Debug.Log("Story movie started: " + source.url);
        }
    }

    private void OnEnded(VideoPlayer source)
    {
        if (playback.HasStarted) Finish(Result.Completed);
        else Fail("The movie ended without producing a video frame.");
    }

    private void OnError(VideoPlayer source, string message) { Fail(message); }

    private void Fail(string message)
    {
        if (playback == null || playback.IsDone) return;
        playback.Error = message;
        Debug.LogWarning("Story movie unavailable: " + message);
        Finish(Result.Failed);
    }

    public bool RequestSkip()
    {
        if (playback == null || playback.IsDone || !allowSkip || !playback.HasStarted) return false;
        Finish(Result.Skipped);
        return true;
    }

    public static bool SkipInputHeld
    {
        get
        {
            return Input.GetMouseButton(0) || Input.touchCount > 0 ||
                Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return);
        }
    }

    private void Update()
    {
        if (playback == null || playback.IsDone) return;
        if (owner == null || !owner.isActiveAndEnabled) { Finish(Result.Cancelled); return; }
        if (suspended) return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (video != null && video.frame != lastFrame && video.frame >= 0)
        {
            lastFrame = video.frame;
            lastProgressAt = now;
        }
        if ((!playback.HasStarted && now - startedAt > 15d) ||
            (playback.HasStarted && now - lastProgressAt > 10d))
        {
            Fail("Video preparation or playback timed out.");
            return;
        }
        if (playback.HasStarted && now - firstFrameAt > 0.25d &&
            (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Escape) ||
             Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
             (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)))
            RequestSkip();
    }

    private void LateUpdate()
    {
        if (playback == null || playback.IsDone || picture == null) return;
        Texture texture = video != null ? video.texture : null;
        if (texture == null || !playback.HasStarted) return;
        picture.texture = texture;
        picture.enabled = true;
        Rect safe = Screen.safeArea;
        float scale = Mathf.Min(safe.width / texture.height, safe.height / texture.width);
        picture.rectTransform.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
        picture.rectTransform.anchoredPosition = safe.center - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
    }

    private void OnApplicationPause(bool paused) { applicationPaused = paused; UpdateSuspension(); }
    private void OnApplicationFocus(bool focused) { applicationFocused = focused; UpdateSuspension(); }

    private void UpdateSuspension()
    {
        bool shouldSuspend = applicationPaused || !applicationFocused;
        if (suspended == shouldSuspend) return;
        suspended = shouldSuspend;
        if (video == null || playback == null || playback.IsDone) return;
        if (suspended) video.Pause();
        else
        {
            startedAt = lastProgressAt = Time.realtimeSinceStartupAsDouble;
            if (video.isPrepared) video.Play();
        }
    }

    private void Finish(Result outcome)
    {
        if (playback == null || playback.IsDone) return;
        playback.Outcome = outcome;
        playback.Player = null;
        if (video != null)
        {
            video.prepareCompleted -= OnPrepared;
            video.frameReady -= OnFrameReady;
            video.loopPointReached -= OnEnded;
            video.errorReceived -= OnError;
            video.Stop();
        }
        if (globalsCaptured)
        {
            Time.timeScale = previousTimeScale;
            AudioListener.pause = previousAudioPause;
            Screen.sleepTimeout = previousSleepTimeout;
            globalsCaptured = false;
        }
        if (active == this) active = null;
        Debug.Log("Story movie finished: " + outcome);
        Destroy(gameObject);
    }

    private void OnDisable() { Finish(Result.Cancelled); }
    private void OnDestroy() { Finish(Result.Cancelled); }
}
