using UnityEngine;

// Both the main menu and pause menu load the same authored settings page.
public sealed class IngameOptionScreen : MonoBehaviour
{
    public delegate void HandlerDelegate();
    public GameObject BtnCalibrateControls, BtnGoBack, BtnSelectVessel;
    public GameObject FlipperMusic, FlipperSound, FlipperVibration;
    public GameObject FlipperDifficulty, FlipperTimer, FlipperLanguage;

    [SerializeField] Renderer _pauseBackdrop;
    Camera viewCamera;
    Vector3 backdropScale, backdropPosition;
    bool initialized;
    bool isEnabled = true;
    int inputEnabledFrame;

    public bool IsInMainMenu { get; private set; }
    public event HandlerDelegate EventGoBack;
    public event HandlerDelegate EventLaunchCalibrateControls;

    public static IngameOptionScreen Create(Transform owner, bool inMainMenu)
    {
        var root = Instantiate(Resources.Load<GameObject>("GameOptions"), owner, false);
        root.name = "Game options";
        var page = root.GetComponent<IngameOptionScreen>();
        page.IsInMainMenu = inMainMenu;
        page.viewCamera = owner.GetComponentInChildren<Camera>() ?? Camera.main;
        page._pauseBackdrop.gameObject.SetActive(!inMainMenu);
        page.backdropScale = page._pauseBackdrop.transform.localScale;
        page.backdropPosition = page._pauseBackdrop.transform.localPosition;
        foreach (SimpleButton button in root.GetComponentsInChildren<SimpleButton>(true))
            button.viewCamera = page.viewCamera;
        root.AddComponent<MenuContentFit>().Camera = page.viewCamera;
        return page;
    }

    void Start()
    {
        BtnGoBack.GetComponent<SimpleButton>().ButtonUpEvent += b => CloseOptions();
        BtnCalibrateControls.GetComponent<SimpleButton>().ButtonUpEvent += b => OpenCalibration();
        BtnSelectVessel.GetComponent<SimpleButton>().ButtonUpEvent += b => OpenVessels();
        FlipperMusic.GetComponent<FlipperButton>().EventValueChanged += MusicChanged;
        FlipperSound.GetComponent<FlipperButton>().EventValueChanged += value => FLD.Audio.IsSoundEffectsMuted = !value;
        FlipperVibration.GetComponent<FlipperButton>().EventChoiceChanged += value =>
        {
            GameHaptics.Cancel();
            GamePreferences.Vibration = (VibrationMode)value;
            GameHaptics.Play(HapticEvent.Jump);
        };
        FlipperDifficulty.GetComponent<FlipperButton>().EventValueChanged += value =>
        {
            if (IsInMainMenu) UserSettings.Instance.IsExpertMode = value;
        };
        FlipperTimer.GetComponent<FlipperButton>().EventValueChanged += value => GamePreferences.ShowTimer = value;
        FlipperLanguage.GetComponent<FlipperButton>().EventChoiceChanged += value => GamePreferences.Chinese = value == 1;
        initialized = true;
        RefreshSettings();
        SetEnabled(isEnabled);
    }

    void RefreshSettings()
    {
        if (!initialized) return;
        FlipperMusic.GetComponent<FlipperButton>().Init(!FLD.Audio.IsMusicMuted, "ENABLED", "DISABLED", "MUSIC");
        FlipperSound.GetComponent<FlipperButton>().Init(!FLD.Audio.IsSoundEffectsMuted, "ENABLED", "DISABLED", "SOUND EFFECTS");
        FlipperVibration.GetComponent<FlipperButton>().InitChoices((int)GamePreferences.Vibration, new[] { "DISABLED", "ORIGINAL", "ENHANCED" }, "VIBRATION");
        FlipperDifficulty.GetComponent<FlipperButton>().Init(UserSettings.Instance.IsExpertMode, "EXPERT", "NORMAL", "DIFFICULTY");
        FlipperTimer.GetComponent<FlipperButton>().Init(GamePreferences.ShowTimer, "ENABLED", "DISABLED", "TIMER");
        FlipperLanguage.GetComponent<FlipperButton>().InitChoices(GamePreferences.Chinese ? 1 : 0, new[] { "ENGLISH", "简体中文" }, "LANGUAGE");
        BtnCalibrateControls.GetComponent<SimpleButton>().Text = GameLocalization.Text("CALIBRATE CONTROLS");
        BtnGoBack.GetComponent<SimpleButton>().Text = GameLocalization.Text("GO BACK");
        BtnSelectVessel.GetComponent<SimpleButton>().Text = GameLocalization.Text("SELECT VESSEL");
    }

    void OnEnable()
    {
        inputEnabledFrame = Time.frameCount;
        GamePreferences.Changed += RefreshSettings;
        RefreshSettings();
    }
    void OnDisable() { GamePreferences.Changed -= RefreshSettings; }

    void LateUpdate()
    {
        if (!IsInMainMenu && viewCamera != null)
            LegacyScreenFit.CoverBackdrop(viewCamera, _pauseBackdrop, backdropScale, backdropPosition);
    }

    public void SetEnabled(bool on)
    {
        isEnabled = on;
        if (on) inputEnabledFrame = Time.frameCount;
        foreach (SimpleButton button in GetComponentsInChildren<SimpleButton>()) button.enabled = on;
        // Difficulty belongs to the current run; change it before starting a level.
        FlipperDifficulty.GetComponent<FlipperButton>().Enabled = on && IsInMainMenu;
    }
    public void CloseOptions() { if (EventGoBack != null) EventGoBack(); }
    public void OpenCalibration() { if (EventLaunchCalibrateControls != null) EventLaunchCalibrateControls(); }
    public void OpenVessels() { VesselSelectionScreen.Open(this); }

    void Update()
    {
        if (isEnabled && Time.frameCount > inputEnabledFrame && (Input.GetKeyUp(KeyCode.Menu) || Input.GetKeyUp(KeyCode.Escape))) CloseOptions();
    }
    void MusicChanged(bool value)
    {
        FLD.Audio.IsMusicMuted = !value;
        if (value && IsInMainMenu)
        {
            MainMenuScreen menu = GetComponentInParent<MainMenuScreen>();
            if (menu != null) menu.PlayMenuMusic();
        }
    }
}
