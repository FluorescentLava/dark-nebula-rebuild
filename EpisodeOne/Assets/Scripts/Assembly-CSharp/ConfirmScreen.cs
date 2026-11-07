using FLD;
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
			if (GetComponentInParent<Hud>() != null)
				background.GetComponent<tk2dSprite>().color = new Color(0f, 0f, 0f, 200f / 255f);
		}
		// Fit the original modal backdrop to the complete HUD camera view.
		// MenuContentFit keeps the title and buttons at their authored size.
		LegacyScreenFit.CoverBackdrop(layout.Camera, backdrop, backdropScale, backdropPosition);
	}

	protected const string YesID = "YES";

	protected const string NoID = "NO";

	public event HandlerDelegate EventYES;

	public event HandlerDelegate EventNO;

	private void Start()
	{
		if (GetComponentInParent<Hud>() != null)
		{
			// Match Episode Two's larger confirmation title while retaining button type size.
			DynamicFont title = lblTitle.GetComponentInChildren<DynamicFont>();
			title.SelectedEntry = 1; // Size9, instead of the Size7 button font.
			title.Refresh();
		}
		btn0.GetComponentInChildren<DynamicFont>().Text = "YES".Localize();
		btn0.GetComponentInChildren<SimpleButton>().ButtonUpEvent += delegate
		{
			EventYES();
		};
		btn1.GetComponentInChildren<DynamicFont>().Text = "NO".Localize();
		btn1.GetComponentInChildren<SimpleButton>().ButtonUpEvent += delegate
		{
			EventNO();
		};
	}

	public void SetTitle(string stringID)
	{
		lblTitle.GetComponentInChildren<DynamicFont>().Text = stringID.Localize();
	}

	public void SetTitleText(string str)
	{
		lblTitle.GetComponentInChildren<DynamicFont>().Text = str;
	}

	public void SetYes(string stringID)
	{
		btn0.GetComponentInChildren<DynamicFont>().Text = stringID.Localize();
	}

	public void SetNo(string stringID)
	{
		btn1.GetComponentInChildren<DynamicFont>().Text = stringID.Localize();
	}

	private void Update()
	{
		if (Input.GetKeyUp(KeyCode.Escape))
		{
			EventNO();
		}
	}
}
