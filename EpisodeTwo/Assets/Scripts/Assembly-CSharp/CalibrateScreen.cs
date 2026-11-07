using UnityEngine;

public class CalibrateScreen : MonoBehaviour
{
	public delegate void HandlerDelegate();

	public GameObject ButtonCalibrate;

	public GameObject ButtonGoBack;

	public GameObject Title;

	private SimpleText _titleText;

	private float _triggerGoBackAtTime;
    private bool closing;
    private float openedAt;
	private readonly Transform[] _instructionLines = new Transform[8];
	private readonly Vector3[] _instructionPositions = new Vector3[8];
    private readonly string[] _instructionIDs = new string[8];

	public event HandlerDelegate EventGoBack;

	private void Start()
	{
		SimpleText[] componentsInChildren = GetComponentsInChildren<SimpleText>();
		foreach (SimpleText simpleText in componentsInChildren)
		{
			for (int i = 0; i < _instructionLines.Length; i++)
			{
				if (simpleText.name != "Text" + i) continue;
				_instructionLines[i] = simpleText.transform;
				_instructionPositions[i] = simpleText.transform.localPosition;
                _instructionIDs[i] = simpleText.GetComponent<tk2dTextMesh>().text;
			}
			simpleText.UseCurrentTextAsStringID();
		}
		RefreshInstructionLayout();
		ButtonCalibrate.GetComponent<SimpleButton>().ButtonUpEvent += delegate
		{
            if (closing || _triggerGoBackAtTime != 0f) return;
			UserSettings.I.Calibrate();
			SimpleText[] componentsInChildren2 = GetComponentsInChildren<SimpleText>();
			foreach (SimpleText simpleText2 in componentsInChildren2)
			{
				simpleText2.GetComponent<Renderer>().enabled = false;
			}
			Title.GetComponent<SimpleText>().SetStringID("CALIBRATED!");
			Title.GetComponent<Renderer>().enabled = true;
			ButtonGoBack.SetActive(false);
			ButtonCalibrate.SetActive(false);
			_triggerGoBackAtTime = Time.realtimeSinceStartup + 2f;
		};
		ButtonGoBack.GetComponent<SimpleButton>().ButtonUpEvent += delegate
		{
			OnGoBack();
		};
		_titleText = Title.GetComponent<SimpleText>();
	}

	private void OnEnable()
	{
        closing = false;
        openedAt = Time.realtimeSinceStartup;
		GamePreferences.Changed += RefreshInstructionLayout;
		_triggerGoBackAtTime = 0f;
		// The previous calibration leaves both button hierarchies inactive.
		// Restore their text renderers as well as the currently active labels.
		SimpleText[] componentsInChildren = GetComponentsInChildren<SimpleText>(true);
		foreach (SimpleText simpleText in componentsInChildren)
		{
			simpleText.GetComponent<Renderer>().enabled = true;
		}
		if (_titleText != null)
		{
			_titleText.SetStringID("CALIBRATE");
		}
		ButtonGoBack.SetActive(true);
		ButtonCalibrate.SetActive(true);
        RefreshInstructionLayout();
	}

	private void OnDisable()
	{
		GamePreferences.Changed -= RefreshInstructionLayout;
	}

	private void RefreshInstructionLayout()
	{
		if (_instructionLines[0] == null) return;
		for (int i = 0; i < _instructionLines.Length; i++)
		{
			if (_instructionLines[i] == null) continue;
			Vector3 position = _instructionPositions[i];
            bool visible = !GamePreferences.Chinese || i == 0 || i == 4;
            _instructionLines[i].gameObject.SetActive(visible);
            if (GamePreferences.Chinese && visible)
            {
                position.y = i == 0 ? 116f : 42f;
                _instructionLines[i].GetComponent<SimpleText>().SetStringID(i == 0 ? "CALIBRATE.INSTRUCTIONS.POSITION" : "LOADING.HINT.CALIBRATE");
            }
            else if (!GamePreferences.Chinese) _instructionLines[i].GetComponent<SimpleText>().SetStringID(_instructionIDs[i]);
			_instructionLines[i].localPosition = position;
		}
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
