using UnityEngine;

public class ConfirmScreen : SimpleScreen
{
	Renderer backdrop;
	Vector3 backdropScale, backdropPosition;

	private void LateUpdate()
	{
		MenuContentFit layout = GetComponent<MenuContentFit>();
		if (layout == null || layout.Camera == null) return;
		if (backdrop == null)
		{
			Transform background = transform.Find("Background");
			if (background == null) return;
			backdrop = background.GetComponent<Renderer>();
			if (backdrop == null) return;
			backdropScale = background.localScale;
			backdropPosition = background.localPosition;
		}
		// Fit the original modal backdrop to the complete HUD camera view.
		// MenuContentFit keeps the title and buttons at their authored size.
		LegacyScreenFit.CoverBackdrop(layout.Camera, backdrop, backdropScale, backdropPosition);
	}

	private string _yesID = "YES";

	private string _noID = "NO";

	public event HandlerDelegate EventYES;

	public event HandlerDelegate EventNO;

	private void Start()
	{
		btn0.GetComponentInChildren<SimpleText>().SetStringID(_yesID);
		btn0.GetComponentInChildren<SimpleButton>().ButtonUpEvent += delegate
		{
			EventYES();
		};
		btn1.GetComponentInChildren<SimpleText>().SetStringID(_noID);
		btn1.GetComponentInChildren<SimpleButton>().ButtonUpEvent += delegate
		{
			EventNO();
		};
	}

	public void SetTitle(string stringID)
	{
		lblTitle.GetComponentInChildren<SimpleText>().SetStringID(stringID);
	}

	public void SetTitleText(string str)
	{
		lblTitle.GetComponentInChildren<SimpleText>().SetText(str);
	}

	public void SetYes(string stringID)
	{
		_yesID = stringID;
		btn0.GetComponentInChildren<SimpleText>().SetStringID(_yesID);
	}

	public void SetNo(string stringID)
	{
		_noID = stringID;
		btn1.GetComponentInChildren<SimpleText>().SetStringID(_noID);
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			EventNO();
		}
	}
}
