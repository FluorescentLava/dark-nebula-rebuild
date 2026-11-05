using System.Collections;
using UnityEngine;

public class FlipperButton : MonoBehaviour
{
	public delegate void HandlerDelegate(bool value);

	private const float originalSpacing = -7f;

	private const float zeroSpacing = -19f;

	public GameObject Title;

	public GameObject Value;

	public GameObject ButtonLeft;

	public GameObject ButtonRight;

	public GameObject Overlay;

	private string _on = "ON";

	private string _off = "OFF";

	private bool _value;
    private string _titleId;
    private string[] _choices;
    private int _choiceIndex;
    public event System.Action<int> EventChoiceChanged;
    public int CurrentChoice { get { return _choiceIndex; } }

    private void OnEnable() { GamePreferences.Changed += RefreshLabels; RefreshLabels(); }
    private bool spacingAnimated;
    private void OnDisable()
    {
        GamePreferences.Changed -= RefreshLabels;
        StopCoroutine("AnimateSpacing");
        if (spacingAnimated)
        {
            Value.GetComponent<SimpleText>().SetAnimatedSpacing(originalSpacing, originalSpacing);
            spacingAnimated = false;
        }
    }
    public void InitChoices(int value, string[] names, string title)
    {
        _choices=names;
        _choiceIndex=Mathf.Clamp(value,0,names.Length-1);
        _titleId=title;
        RefreshLabels();
    }
    public void RefreshLabels()
    {
        if (_titleId==null) return;
        Title.SetActive(!string.IsNullOrEmpty(_titleId));
        Title.GetComponent<SimpleText>().SetText(GameLocalization.Text(_titleId));
        string value=_choices!=null ? _choices[_choiceIndex] : (_value ? _on : _off);
        Value.GetComponent<SimpleText>().SetText(GameLocalization.Text(value));
    }


	private bool _enabled = true;

	public bool LastTapWasLeft;

	public bool Enabled
	{
		get
		{
			return _enabled;
		}
		set
		{
			_enabled = value;
			ButtonLeft.GetComponent<SimpleButton>().enabled = value;
			ButtonRight.GetComponent<SimpleButton>().enabled = value;
			Overlay.GetComponent<Renderer>().enabled = !value;
		}
	}

	public event HandlerDelegate EventValueChanged;

	private void Start()
	{
		ButtonLeft.GetComponent<SimpleButton>().ButtonUpEvent += OnToggleLeft;
		ButtonRight.GetComponent<SimpleButton>().ButtonUpEvent += OnToggleRight;
		tk2dSprite[] componentsInChildren = GetComponentsInChildren<tk2dSprite>();
		foreach (tk2dSprite tk2dSprite2 in componentsInChildren)
		{
			Align(tk2dSprite2.gameObject);
		}
		Enabled = _enabled;
	}

	private void Align(GameObject go)
	{
		Vector3 position = go.transform.position;
		position.x = Mathf.Round(position.x * 2f) * 0.5f;
		position.y = Mathf.Round(position.y * 2f) * 0.5f;
		go.transform.position = position;
	}

	public void UseTitleOnly()
	{
		ButtonLeft.SetActive(false);
		ButtonRight.SetActive(false);
		tk2dSprite[] componentsInChildren = GetComponentsInChildren<tk2dSprite>();
		foreach (tk2dSprite tk2dSprite2 in componentsInChildren)
		{
			tk2dSprite2.gameObject.GetComponent<Renderer>().enabled = false;
		}
		Value.SetActive(false);
		Title.GetComponent<Renderer>().enabled = true;
		Title.transform.Translate(Vector3.down * 28f);
	}

    public void Init(bool value, string onStringID, string offStringID, string titleStringID)
    {
        _choices=null;
        _value=value;
        _on=onStringID;
        _off=offStringID;
        _titleId=titleStringID;
        RefreshLabels();
    }

	private void OnToggleLeft(SimpleButton source)
	{
		OnToggle(true);
	}

	private void OnToggleRight(SimpleButton source)
	{
		OnToggle(false);
	}

    private void OnToggle(bool isLeft)
    {
        LastTapWasLeft=isLeft;
        if (_choices!=null)
        {
            _choiceIndex=(_choiceIndex+(isLeft ? -1 : 1)+_choices.Length)%_choices.Length;
            RefreshLabels();
            if(EventChoiceChanged!=null) EventChoiceChanged(_choiceIndex);
        }
        else
        {
            _value=!_value;
            RefreshLabels();
            if(EventValueChanged!=null) EventValueChanged(_value);
        }
        StopCoroutine("AnimateSpacing");
        StartCoroutine("AnimateSpacing");
    }

	private IEnumerator AnimateSpacing()
	{
        spacingAnimated = true;
		float begin = Time.realtimeSinceStartup;
		float duration = 0.2f;
		while (begin + duration > Time.realtimeSinceStartup)
		{
			float elapsed = Time.realtimeSinceStartup - begin;
			float percent = elapsed / duration;
            Value.GetComponent<SimpleText>().SetAnimatedSpacing(zeroSpacing + (originalSpacing - zeroSpacing) * percent, originalSpacing);
			yield return null;
		}
        Value.GetComponent<SimpleText>().SetAnimatedSpacing(originalSpacing, originalSpacing);
        spacingAnimated = false;
	}
}
