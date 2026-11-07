using FLD;
using UnityEngine;

public class CalibrateScreen : MonoBehaviour
{
	public delegate void HandlerDelegate();

	public SimpleButton ButtonCalibrate;

	public SimpleButton ButtonGoBack;

	public DynamicFont TitleText;

	public DynamicFont Text;

	private float _triggerGoBackAtTime;
    private bool closing;
    private float openedAt;

	public event HandlerDelegate EventGoBack;

	private void Start()
	{
		base.transform.localPosition = new Vector3(0f, 0f, base.transform.localPosition.z);
		Text.Text = "CALIBRATE TEXT".Localize();
		ButtonCalibrate.Text = "CALIBRATE CONTROLS".Localize();
		ButtonCalibrate.ButtonUpEvent += delegate
		{
            if (closing || _triggerGoBackAtTime != 0f) return;
			UserSettings.Instance.Calibrate();
			Text.GetComponent<Renderer>().enabled = false;
			TitleText.Text = "CALIBRATED!".Localize();
			ButtonGoBack.gameObject.SetActive(false);
			ButtonCalibrate.gameObject.SetActive(false);
			_triggerGoBackAtTime = Time.realtimeSinceStartup + 2f;
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
		_triggerGoBackAtTime = 0f;
		Text.GetComponent<Renderer>().enabled = true;
		TitleText.Text = "CALIBRATE".Localize();
		ButtonGoBack.gameObject.SetActive(true);
		ButtonCalibrate.gameObject.SetActive(true);
	}

	private void OnGoBack()
	{
        if (closing) return;
        closing = true;
        _triggerGoBackAtTime = 0f;
        SceneTransitionInput.SuppressFor(.7f);
		if (EventGoBack != null)
		{
			EventGoBack();
		}
	}

	public virtual void Update()
	{
		if (!closing && _triggerGoBackAtTime == 0f && Time.realtimeSinceStartup > openedAt + .2f && Input.GetKeyUp(KeyCode.Escape))
		{
			OnGoBack();
		}
		if (_triggerGoBackAtTime != 0f && !(_triggerGoBackAtTime > Time.realtimeSinceStartup))
		{
			OnGoBack();
		}
	}
}
