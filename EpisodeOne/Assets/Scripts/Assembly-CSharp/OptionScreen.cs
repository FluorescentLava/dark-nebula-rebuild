using UnityEngine;
public class OptionScreen : IngameOptionScreen
{
    [SerializeField] SimpleButton _calibrateButton;
    [SerializeField] SimpleButton _restorePurchaseButton;
    [SerializeField] SimpleButton _backButton;
    [SerializeField] Alert _alertScreen;
    public GameObject FlipperDifficulty;
    protected override void Start()
    {
        transform.position=new Vector3(0,0,transform.position.z);
        base.Start();
        FlipperDifficulty.GetComponent<FlipperButton>().EventValueChanged += value=>UserSettings.Instance.IsExpertMode=value;
        RefreshSettings();
    }
    protected override GameObject VesselButton() { return _restorePurchaseButton!=null ? _restorePurchaseButton.gameObject : base.VesselButton(); }
    protected override void ApplyLayout()
    {
        base.ApplyLayout();
        foreach(DynamicFont heading in GetComponentsInChildren<DynamicFont>(true))
        {
            if(heading.transform.parent!=transform || heading.OriginalText!="GAME OPTIONS")continue;
            Vector3 position=heading.transform.localPosition;
            position.y=262f;
            // Store the heading's layout position so language refreshes retain the gap.
            heading.SetOriginalPos(position);
            heading.Refresh();
        }
    }
    protected override void RefreshSettings()
    {
        base.RefreshSettings();
        if(!IsInitialized)return;
        if(FlipperDifficulty!=null)FlipperDifficulty.GetComponent<FlipperButton>().Init(UserSettings.Instance.IsExpertMode,"EXPERT","NORMAL","DIFFICULTY");
    }
}
