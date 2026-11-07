using FLD;
using UnityEngine;

public class Alert : ConfirmScreen
{
	public GameObject lblBody;

	[SerializeField]
	private GameObject _titleEnhancer;

	private void Awake()
	{
		base.transform.position = new Vector3(0f, 0f, base.transform.position.z);
	}

    public override void SetAcknowledgementOnly()
    {
        base.SetAcknowledgementOnly();
        DynamicFont title = lblTitle.GetComponentInChildren<DynamicFont>();
        title.SelectedEntry = 2; // Size11, larger than the body and button.
        title.Anchor = TextAnchor.MiddleCenter;
        title.Alignment = TextAlignment.Center;
        title.SetOriginalPos(new Vector3(0f, 55f, title.transform.localPosition.z));
        title.Refresh();
        DynamicFont body = lblBody.GetComponent<DynamicFont>();
        body.Anchor = TextAnchor.MiddleCenter;
        body.Alignment = TextAlignment.Center;
        body.SetOriginalPos(new Vector3(0f, 5f, body.transform.localPosition.z));
        body.Refresh();
        Vector3 buttonPosition = btn0.transform.localPosition;
        buttonPosition.y = -55f;
        btn0.transform.localPosition = buttonPosition;
    }

	public void SetBody(string stringID)
	{
		lblBody.GetComponent<DynamicFont>().Text = stringID.Localize().ToUpper();
	}

	public void Reset()
	{
		_titleEnhancer.gameObject.SetActive(false);
		btn0.gameObject.SetActive(true);
		btn1.gameObject.SetActive(true);
		btn0.GetComponentInChildren<DynamicFont>().Text = "YES".Localize();
		btn1.GetComponentInChildren<DynamicFont>().Text = "NO".Localize();
	}

	public void ShowTitleEnhancer()
	{
		_titleEnhancer.gameObject.SetActive(true);
	}

	public void HideYes()
	{
		btn0.gameObject.SetActive(false);
	}

	public void HideNo()
	{
		btn1.gameObject.SetActive(false);
	}

	public void SetBodyText(string str)
	{
		lblBody.GetComponent<DynamicFont>().Text = str;
	}
}
