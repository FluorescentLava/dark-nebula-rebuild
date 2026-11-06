using FLD;
using UnityEngine;

public class FlipperButtonMultipleValues : MonoBehaviour
{
	public delegate void HandlerDelegate(int value);

	private const float originalSpacing = -7f;

	private const float zeroSpacing = -19f;

	public DynamicFont Title;

	public DynamicFont Value;

	public GameObject ButtonLeft;

	public GameObject ButtonRight;

	public GameObject Overlay;

	private string[] _names;

	private int _currentIndex;

	public int ForbiddenIndex = -1;

	private bool _enabled = true;

	public int CurrentIndex
	{
		get
		{
			return _currentIndex;
		}
	}

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
		}
	}

	public event HandlerDelegate EventValueChanged;

	private void Start()
	{
		Overlay.gameObject.SetActive(false);
		ButtonLeft.GetComponent<SimpleButton>().ButtonUpEvent += OnLeft;
		ButtonRight.GetComponent<SimpleButton>().ButtonUpEvent += OnRight;
		tk2dSprite[] componentsInChildren = GetComponentsInChildren<tk2dSprite>();
		foreach (tk2dSprite tk2dSprite2 in componentsInChildren)
		{
			Align(tk2dSprite2.gameObject);
		}
		LegacyControlTextLayout.Bind(Value, ButtonLeft.GetComponent<Renderer>());
		Enabled = _enabled;
	}

	private void Align(GameObject go)
	{
		Vector3 position = go.transform.position;
		position.x = Mathf.Round(position.x * 2f) * 0.5f;
		position.y = Mathf.Round(position.y * 2f) * 0.5f;
		go.transform.position = position;
	}

	public void Init(int pCurrentKeyValue, string[] pNames, string titleStringID)
	{
		_currentIndex = pCurrentKeyValue;
		_names = pNames;
		Title.Text = titleStringID.Localize();
		Value.Text = pNames[_currentIndex];
	}

	private void OnLeft(SimpleButton source)
	{
		_currentIndex--;
		if (_currentIndex == ForbiddenIndex)
		{
			_currentIndex--;
		}
		if (_currentIndex < 0)
		{
			_currentIndex = _names.Length - 1;
		}
		if (_currentIndex == ForbiddenIndex)
		{
			_currentIndex--;
		}
		UpdateValue();
	}

	private void OnRight(SimpleButton source)
	{
		_currentIndex++;
		if (_currentIndex >= _names.Length)
		{
			_currentIndex = 0;
		}
		if (_currentIndex == ForbiddenIndex)
		{
			_currentIndex++;
		}
		UpdateValue();
	}

	private void UpdateValue()
	{
		Value.Text = _names[_currentIndex];
		if (EventValueChanged != null)
		{
			EventValueChanged(_currentIndex);
		}
	}
}
