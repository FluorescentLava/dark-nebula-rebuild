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
    private RectTransform skipPrompt;
    private Text skipLabel;
    private bool allowSkip;
    private bool waitForPointerRelease = true;
    private bool touchMouseGuard;
    private bool pointerDown;
    private bool skipCandidate;
    private int pointerId;
    private Vector2 pointerStart;
    private double pointerStartedAt;
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

        if (allowSkip)
        {
            var prompt = new GameObject("Skip prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            prompt.transform.SetParent(transform, false);
            prompt.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
            prompt.GetComponent<Image>().raycastTarget = false;
            skipPrompt = prompt.GetComponent<RectTransform>();
            skipPrompt.anchorMin = skipPrompt.anchorMax = new Vector2(0.5f, 0.5f);
            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            label.transform.SetParent(prompt.transform, false);
            skipLabel = label.GetComponent<Text>();
            // Use the same geometric CJK source as ChineseAtlas and the recovered
            // BankGothic face used by the menus, with their normal gray tint.
            skipLabel.font = Resources.Load<Font>(GamePreferences.Chinese
                ? "Fonts/DarkNebulaCJKFullWidth-Regular" : "Fonts/darknebula");
            skipLabel.text = GamePreferences.Chinese ? "单击此处跳过动画" : "Tap here to skip the animation";
            skipLabel.color = new Color(1f, 1f, 1f, 0.4f);
            skipLabel.alignment = TextAnchor.MiddleCenter;
            skipLabel.supportRichText = false;
            skipLabel.raycastTarget = false;
            skipLabel.resizeTextForBestFit = true;
            RectTransform labelRect = skipLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);
            prompt.SetActive(false);
        }

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
        video.playbackSpeed = 1f;
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
        if (allowSkip && playback.HasStarted && now - firstFrameAt > 0.25d) UpdateSkipInput(now);
    }

    private void BlockSkipGesture()
    {
        waitForPointerRelease = true;
        pointerDown = skipCandidate = false;
    }

    private void UpdateSkipInput(double now)
    {
        int touches = Input.touchCount;
        Touch touch = touches > 0 ? Input.GetTouch(0) : default(Touch);
        UpdateSkipInput(now, touches, touch.fingerId, touch.phase, touch.position,
            Input.GetMouseButton(0), Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), Input.mousePosition);
    }

    private void UpdateSkipInput(double now, int touches, int finger, TouchPhase phase, Vector2 touchPosition,
        bool held, bool pressed, bool released, Vector2 mousePosition)
    {
        // Touch is authoritative. Its emulated mouse press/release must drain
        // completely before a subsequent mouse gesture can be considered.
        if (touches > 0)
        {
            touchMouseGuard = true;
            HandleSkipPointer(finger, phase == TouchPhase.Began, phase == TouchPhase.Ended,
                phase == TouchPhase.Canceled || touches != 1, touchPosition, now);
            return;
        }
        if (touchMouseGuard)
        {
            BlockSkipGesture();
            if (!held && !pressed && !released) { touchMouseGuard = false; waitForPointerRelease = false; }
            return;
        }
        if (!held && !pressed && !released) { waitForPointerRelease = false; return; }
        HandleSkipPointer(-1, pressed, released, false, mousePosition, now);
    }

    private bool InsideSkipPrompt(Vector2 position)
    {
        return skipPrompt != null && skipPrompt.gameObject.activeSelf &&
            RectTransformUtility.RectangleContainsScreenPoint(skipPrompt, position, null);
    }

    private void HandleSkipPointer(int id, bool pressed, bool released, bool cancelled, Vector2 position, double now)
    {
        if (cancelled) { BlockSkipGesture(); return; }
        if (waitForPointerRelease) return;
        if (pressed)
        {
            if (pointerDown) { BlockSkipGesture(); return; }
            if (!skipPrompt.gameObject.activeSelf)
            {
                skipPrompt.gameObject.SetActive(true);
                BlockSkipGesture();
                return;
            }
            pointerDown = true;
            pointerId = id;
            pointerStart = position;
            pointerStartedAt = now;
            skipCandidate = InsideSkipPrompt(position);
        }
        if (!pointerDown) { BlockSkipGesture(); return; }
        float slop = Mathf.Max(12f, Screen.width * 0.025f);
        if (id != pointerId || !InsideSkipPrompt(position) || now - pointerStartedAt > 0.75d ||
            (position - pointerStart).sqrMagnitude > slop * slop) skipCandidate = false;
        if (!released) return;
        bool confirmed = skipCandidate;
        BlockSkipGesture();
        if (confirmed) RequestSkip();
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
        if (skipPrompt != null)
        {
            float margin = Mathf.Max(8f, safe.width * 0.03f);
            float height = Mathf.Min(Mathf.Clamp(safe.width * 0.12f, 44f, 128f), Mathf.Max(1f, safe.height - margin * 2f));
            skipPrompt.sizeDelta = new Vector2(Mathf.Max(1f, safe.width - margin * 2f), height);
            float lift = Mathf.Min(safe.width * 0.035f, Mathf.Max(0f, safe.height - margin * 2f - height));
            skipPrompt.anchoredPosition = new Vector2(safe.center.x - Screen.width * 0.5f,
                safe.yMin + margin + lift + height * 0.5f - Screen.height * 0.5f);
            // BankGothic has a lower cap height than the fullwidth CJK face.
            float textSize = (GamePreferences.Chinese ? 0.045f : 0.075f) * 0.85f;
            skipLabel.resizeTextMaxSize = Mathf.Max(12, Mathf.RoundToInt(safe.width * textSize));
            skipLabel.resizeTextMinSize = Mathf.Max(10, skipLabel.resizeTextMaxSize / 2);
            skipLabel.fontSize = skipLabel.resizeTextMaxSize;
        }
    }

    private void OnApplicationPause(bool paused) { applicationPaused = paused; UpdateSuspension(); }
    private void OnApplicationFocus(bool focused) { applicationFocused = focused; UpdateSuspension(); }

    private void UpdateSuspension()
    {
        bool shouldSuspend = applicationPaused || !applicationFocused;
        if (suspended == shouldSuspend) return;
        suspended = shouldSuspend;
        BlockSkipGesture();
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
