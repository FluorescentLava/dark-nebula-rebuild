using UnityEngine;

// Extend the recovered settings panel using its original sprites, labels and arrows.
public class IngameOptionScreen : MonoBehaviour
{
    public delegate void HandlerDelegate();
    public GameObject BtnCalibrateControls;
    public GameObject BtnGoBack;
    public GameObject FlipperMusic;
    public GameObject FlipperSound;
    public GameObject FlipperVibration;

    [System.NonSerialized] public GameObject FlipperTimer, FlipperLanguage, BtnSelectVessel;
    bool _isEnabled=true;
    int inputEnabledFrame=-1;
    bool initialized;
    protected bool IsInitialized { get { return initialized; } }
    public bool IsInMainMenu { get { return this is OptionScreen; } }
    public event HandlerDelegate EventGoBack;
    public event HandlerDelegate EventLaunchCalibrateControls;
    protected virtual void Awake()
    {
    }
    protected virtual void Start()
    {
        BtnGoBack.GetComponent<SimpleButton>().ButtonUpEvent += b=>CloseOptions();
        BtnCalibrateControls.GetComponent<SimpleButton>().ButtonUpEvent += b=>OpenCalibration();
        FlipperMusic.GetComponent<FlipperButton>().EventValueChanged += MusicChanged;
        FlipperSound.GetComponent<FlipperButton>().EventValueChanged += value=>FLD.Audio.IsSoundEffectsMuted=!value;
        FlipperVibration.GetComponent<FlipperButton>().EventChoiceChanged += value=>
        {
            GameHaptics.Cancel();
            GamePreferences.Vibration=(VibrationMode)value;
            GameHaptics.Play(HapticEvent.Jump);
        };
        FlipperTimer=CloneRow("FlipperButtonTimer");
        FlipperTimer.GetComponent<FlipperButton>().EventValueChanged += value=>GamePreferences.ShowTimer=value;
        FlipperLanguage=CloneRow("FlipperButtonLanguage");
        FlipperLanguage.GetComponent<FlipperButton>().EventChoiceChanged += value=>GamePreferences.Chinese=value==1;
        BtnSelectVessel=VesselButton();
        BtnSelectVessel.SetActive(true);
        BtnSelectVessel.GetComponent<SimpleButton>().ButtonUpEvent += b=>OpenVessels();
        initialized=true;
        ApplyLayout();
        RefreshSettings();
    }
    protected virtual GameObject VesselButton()
    {
        GameObject button=Instantiate(BtnCalibrateControls,BtnCalibrateControls.transform.parent);
        button.name="Select Vessel";
        button.GetComponent<SimpleButton>().targetObject=null;
        button.GetComponent<SimpleButton>().messageName=string.Empty;
        return button;
    }
    GameObject CloneRow(string name)
    {
        GameObject row=Instantiate(FlipperMusic,FlipperMusic.transform.parent);
        row.name=name;
        // Instantiation copies serialized artwork and references, not runtime delegates.
        row.SetActive(true);
        return row;
    }
    protected static void Position(GameObject control,float y)
    {
        if(control==null)return;
        Vector3 position=control.transform.localPosition;
        position.y=y;
        control.transform.localPosition=position;
    }
    protected virtual void ApplyLayout()
    {
        if(IsInMainMenu)
        {
            // Center the tighter rows and action buttons with clear gaps between groups.
            Position(FlipperSound,198); Position(FlipperMusic,139); Position(FlipperVibration,80);
            Position(((OptionScreen)this).FlipperDifficulty,21);
            Position(FlipperTimer,-38); Position(FlipperLanguage,-97);
            Position(BtnCalibrateControls,-131); Position(BtnSelectVessel,-179); Position(BtnGoBack,-233);
            // Preserve the tighter title position when the language refreshes.
            foreach(GameObject row in new[]{FlipperSound,FlipperMusic,FlipperVibration,((OptionScreen)this).FlipperDifficulty,FlipperTimer,FlipperLanguage})
            {
                DynamicFont title=row.GetComponent<FlipperButton>().Title;
                Vector3 position=title.transform.localPosition;position.y=27f;
                title.SetOriginalPos(position);
                title.Refresh();
            }
        }
        else
        {
            // The recovered pause panel has its title and calibration above the toggles.
            Position(BtnCalibrateControls,100); Position(BtnSelectVessel,47);
            Position(FlipperSound,-14); Position(FlipperMusic,-76); Position(FlipperVibration,-138);
            Position(FlipperTimer,-200); Position(FlipperLanguage,-262); Position(BtnGoBack,-324);
        }
    }
    protected virtual void RefreshSettings()
    {
        if(!initialized)return;
        FlipperMusic.GetComponent<FlipperButton>().Init(!FLD.Audio.IsMusicMuted,"ENABLED","DISABLED","MUSIC");
        FlipperSound.GetComponent<FlipperButton>().Init(!FLD.Audio.IsSoundEffectsMuted,"ENABLED","DISABLED","SOUND EFFECTS");
        FlipperVibration.GetComponent<FlipperButton>().InitChoices((int)GamePreferences.Vibration,new[]{"DISABLED","ORIGINAL","ENHANCED"},"VIBRATION");
        FlipperTimer.GetComponent<FlipperButton>().Init(GamePreferences.ShowTimer,"ENABLED","DISABLED","TIMER");
        FlipperLanguage.GetComponent<FlipperButton>().InitChoices(GamePreferences.Chinese?1:0,new[]{"ENGLISH","简体中文"},"LANGUAGE");
        Label(BtnCalibrateControls,"CALIBRATE CONTROLS");
        Label(BtnGoBack,"GO BACK");
        Label(BtnSelectVessel,"SELECT VESSEL");
    }
    protected static void Label(GameObject button,string key)
    {
        button.GetComponentInChildren<SimpleButton>().Text=GameLocalization.Text(key);
    }
    protected virtual void OnEnable()
    {
        inputEnabledFrame=Time.frameCount;
        GamePreferences.Changed += RefreshSettings;
        RefreshSettings();
    }
    protected virtual void OnDisable() { GamePreferences.Changed -= RefreshSettings; }
    public void SetEnabled(bool on)
    {
        _isEnabled=on;
        if(on)inputEnabledFrame=Time.frameCount;
        foreach(SimpleButton button in GetComponentsInChildren<SimpleButton>())button.enabled=on;
    }
    public void CloseOptions() { if(EventGoBack!=null)EventGoBack(); }
    public void OpenCalibration() { if(EventLaunchCalibrateControls!=null)EventLaunchCalibrateControls(); }
    public void OpenVessels() { OriginalVesselScreen.Open(this); }
    public virtual void Update()
    {
        if(_isEnabled && Time.frameCount>inputEnabledFrame && (Input.GetKeyUp(KeyCode.Menu)||Input.GetKeyUp(KeyCode.Escape)))CloseOptions();
    }
    void MusicChanged(bool value)
    {
        FLD.Audio.IsMusicMuted=!value;
        if(value && IsInMainMenu)
        {
            MainMenuScreen menu=GetComponentInParent<MainMenuScreen>();
            if(menu!=null)menu.PlayMenuMusic();
        }
    }
}
