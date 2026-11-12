using FLD;
using UnityEngine;

public class CalibrateScreen : MonoBehaviour
{
	public delegate void HandlerDelegate();

	public SimpleButton ButtonCalibrate;

	public SimpleButton ButtonGoBack;

	public DynamicFont TitleText;

	public DynamicFont Text;

	private bool calibrated;
    private bool closing;
    private float openedAt;
    private Renderer backdrop;
    private Vector3 backdropScale, backdropPosition;

	public event HandlerDelegate EventGoBack;

	private void Start()
	{
		base.transform.localPosition = new Vector3(0f, 0f, base.transform.localPosition.z);
		ConfigureTextSize();
		Text.Text = "CALIBRATE TEXT".Localize();
		ButtonCalibrate.Text = "CALIBRATE CONTROLS".Localize();
		ButtonCalibrate.ButtonUpEvent += delegate
		{
            if (closing || calibrated) return;
			UserSettings.Instance.Calibrate();
			calibrated = true;
			Text.GetComponent<Renderer>().enabled = false;
			TitleText.Text = "CALIBRATED!".Localize();
			ButtonGoBack.gameObject.SetActive(true);
			ButtonCalibrate.gameObject.SetActive(false);
		};
		ButtonGoBack.Text = "GO BACK".Localize();
		ButtonGoBack.ButtonUpEvent += delegate
		{
			OnGoBack();
		};
	}

	public void Show()
	{
        closing = false;
        openedAt = Time.realtimeSinceStartup;
		ConfigureTextSize();
		Text.Text = "CALIBRATE TEXT".Localize();
		calibrated = false;
		Text.GetComponent<Renderer>().enabled = true;
		TitleText.Text = "CALIBRATE".Localize();
		ButtonGoBack.gameObject.SetActive(true);
		ButtonCalibrate.gameObject.SetActive(true);
	}

	private void ConfigureTextSize()
	{
		// Use the normal heading size, matching the confirmation screens.
		TitleText.SelectedEntry = 1;
		Text.SelectedEntry = 0;
		Text.Wrapping = GamePreferences.Chinese ? DynamicFont.WrapType.NONE : DynamicFont.WrapType.WORD;
	}

	private void OnGoBack()
	{
        if (closing) return;
        closing = true;
        SceneTransitionInput.SuppressFor(.7f);
		if (EventGoBack != null)
		{
			EventGoBack();
		}
	}

    private void LateUpdate()
    {
        MenuContentFit layout = GetComponent<MenuContentFit>();
        if (layout == null || layout.Camera == null) return;
        if (backdrop == null)
        {
            if (GetComponentInParent<Hud>() == null) return;
            Transform background = transform.Find("Background");
            if (background == null) return;
            backdrop = background.GetComponent<Renderer>();
            backdropScale = background.localScale;
            backdropPosition = background.localPosition;
            background.GetComponent<tk2dSprite>().color = new Color(0f, 0f, 0f, 200f / 255f);
        }
        LegacyScreenFit.CoverBackdrop(layout.Camera, backdrop, backdropScale, backdropPosition);
    }

	public virtual void Update()
	{
		if (!closing && Time.realtimeSinceStartup > openedAt + .2f && Input.GetKeyUp(KeyCode.Escape))
		{
			OnGoBack();
		}
	}
}
