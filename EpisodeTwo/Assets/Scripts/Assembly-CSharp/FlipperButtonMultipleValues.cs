using System.Collections;
using UnityEngine;

public class FlipperButtonMultipleValues : MonoBehaviour
{
	public delegate void HandlerDelegate(int value);

	private const float originalSpacing = -7f;

	private const float zeroSpacing = -19f;

	public GameObject Title;

	public GameObject Value;

	public GameObject ButtonLeft;

	public GameObject ButtonRight;

	public GameObject Overlay;

	private string[] _names;

	private int _currentIndex;

	private bool _enabled = true;

    private bool spacingAnimated;
    private void OnDisable()
    {
        StopCoroutine("AnimateSpacing");
        if (spacingAnimated)
        {
            Value.GetComponent<SimpleText>().SetAnimatedSpacing(originalSpacing, originalSpacing);
            spacingAnimated = false;
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
			Overlay.GetComponent<Renderer>().enabled = !value;
		}
	}

	public event HandlerDelegate EventValueChanged;

	private void Start()
	{
		ButtonLeft.GetComponent<SimpleButton>().ButtonUpEvent += OnLeft;
		ButtonRight.GetComponent<SimpleButton>().ButtonUpEvent += OnRight;
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

	public void Init(int pCurrentKeyValue, string[] pNames, string titleStringID)
	{
		_currentIndex = pCurrentKeyValue;
		_names = pNames;
		Title.GetComponent<SimpleText>().SetStringID(titleStringID);
		Value.GetComponent<SimpleText>().SetText(_names[_currentIndex]);
	}

	private void OnLeft(SimpleButton source)
	{
		_currentIndex--;
		if (_currentIndex < 0)
		{
			_currentIndex = _names.Length - 1;
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
		UpdateValue();
	}

	private void UpdateValue()
	{
		Value.GetComponent<SimpleText>().SetText(_names[_currentIndex]);
		if (EventValueChanged != null)
		{
			EventValueChanged(_currentIndex);
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
