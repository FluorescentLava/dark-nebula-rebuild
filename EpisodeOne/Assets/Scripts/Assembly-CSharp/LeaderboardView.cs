using System.Collections.Generic;
using Palringo;
using UnityEngine;

public class LeaderboardView : MonoBehaviour
{
	public delegate void HandlerDelegate(bool value);

	private const float ViewHeight = 180f;

	private const float FirstEntryPosition = 54f;

	private const int VisibleEntries = 4;

	public GameObject GfxHeaderNormal;

	public GameObject GfxHeaderExpert;

	public GameObject GfxContent;

	public GameObject EntryPrefab;

	[SerializeField]
	private GameObject _loadingSpinner;

	[SerializeField]
	private GameObject _palringoAdContainer;

	[SerializeField]
	private SimpleButton _palringoButton;

	private List<LeaderboardEntry> _entries;

	private int _entryCount;

	private bool _expert;

	private float _inputStartY;

	private float _contentYAtMouseDown;

	private bool _isMouseDown;

	private Vector3 _contentOrigin;

	private float _headerOriginY;

	private Camera _viewCamera;

	private ScrollPhysics _scroll;

	private bool _initialied;

	private float origY;

	private bool _isUsingLargeView;

	private float lastMouseDownY;
	private DynamicFont[] _chineseHeaders;
	private tk2dTextMesh[] _englishHeaders;

	public event HandlerDelegate EventDifficultyChanged;

	private void Start()
	{
		_chineseHeaders = new DynamicFont[2];
		_englishHeaders = new tk2dTextMesh[2];
		GameObject[] headers = { GfxHeaderNormal, GfxHeaderExpert };
		var font = Resources.Load<GameObject>("LeaderboardText/FontInputdata").GetComponent<tk2dFontData>();
		for (int index = 0; index < headers.Length; index++)
		{
			GameObject header = headers[index];
			DynamicFont label = header.GetComponentInChildren<DynamicFont>();
			_chineseHeaders[index] = label;
			label.SelectedEntry = 9;
			label.FontColor = 3;
			label.Refresh();
			LegacyControlTextLayout.Bind(label, header.GetComponentInChildren<tk2dBaseSprite>().GetComponent<Renderer>());
			var root = new GameObject("English header");
			root.layer = header.layer;
			root.transform.SetParent(header.transform, false);
			float headerX = index == 0 ? -72.2f : 72.15619f;
			float labelX = (headerX - header.transform.localPosition.x) / header.transform.localScale.x;
			root.transform.localPosition = new Vector3(labelX, 0f, 82.856445f);
			root.transform.localScale = new Vector3(.4994481f / header.transform.localScale.x, .4994481f, .4994481f);
			Vector3 chinesePosition = label.transform.localPosition;
			chinesePosition.x = labelX;
			label.SetOriginalPos(chinesePosition);
			var bitmap = root.AddComponent<tk2dTextMesh>();
			bitmap.font = font;
			bitmap.anchor = TextAnchor.MiddleCenter;
			bitmap.SetLiteralText(index == 0 ? "NORMAL" : "EXPERT");
			bitmap.ForceBuild();
			_englishHeaders[index] = bitmap;
		}
		GamePreferences.Changed += RefreshHeaderLanguage;
		RefreshHeaderLanguage();
		_palringoButton.ButtonUpEvent += HandlePalringoButton;
		_viewCamera = base.transform.parent.gameObject.GetComponentInChildren<Camera>();
		FixAspect();
		Init(UserSettings.Instance.IsExpertMode);
	}

	private void RefreshHeaderLanguage()
	{
		for (int index = 0; index < _chineseHeaders.Length; index++)
		{
			_chineseHeaders[index].gameObject.SetActive(GamePreferences.Chinese);
			_chineseHeaders[index].Text = GameLocalization.Text(index == 0 ? "NORMAL" : "EXPERT");
			_englishHeaders[index].gameObject.SetActive(!GamePreferences.Chinese);
		}
	}

	private void OnDestroy() { GamePreferences.Changed -= RefreshHeaderLanguage; }

	private void HandlePalringoButton(SimpleButton pButton)
	{
		_scroll.velocity = 0f;
		base.gameObject.SetActive(false);
		GenericWaitPopup.Show();
		DarkNebulaPalringo.Connect.Authorize().Wait((AuthorizationResult result) =>
		{
			GenericWaitPopup.HidePopup();
			base.gameObject.SetActive(true);
			if (result == AuthorizationResult.Granted)
			{
				LeaderboardController leaderboardController = Object.FindObjectOfType<LeaderboardController>();
				if (leaderboardController != null)
				{
					leaderboardController.SetupLeaderboard();
					leaderboardController.UpdateLeaderboard();
				}
				_palringoAdContainer.SetActive(false);
			}
		});
	}

	public void ShowLoadScreen(bool pIsExpert, bool pShowLoadscreen = true)
	{
		if (!pShowLoadscreen)
		{
			_scroll.velocity = 0f;
		}
		if (pShowLoadscreen)
		{
			SetList(new List<LeaderboardEntryVO>(), pIsExpert);
		}
		_loadingSpinner.SetActive(pShowLoadscreen);
		_palringoAdContainer.SetActive(false);
	}

	private void Init(bool pExpert)
	{
		if (!_initialied)
		{
			_initialied = true;
			_expert = pExpert;
			_scroll = new ScrollPhysics();
			_scroll.position = 54f;
			_contentOrigin = GfxContent.transform.localPosition;
			_contentOrigin.Scale(new Vector3(1f, 0f, 1f));
			_headerOriginY = GfxHeaderNormal.transform.localPosition.y;
			GfxHeaderNormal.GetComponent<ToggleButton>().ButtonUpEvent += HandleButtonNormalUpEvent;
			GfxHeaderExpert.GetComponent<ToggleButton>().ButtonUpEvent += HandleButtonExpertUpEvent;
			UpdateToggles();
			_entries = new List<LeaderboardEntry>();
			List<LeaderboardEntryVO> pEntries = new List<LeaderboardEntryVO>();
			SetList(pEntries, _expert);
		}
	}

	private void OnDisable()
	{
	}

	private void UpdateToggles()
	{
		GfxHeaderNormal.GetComponent<ToggleButton>().SetToggle(!_expert);
		GfxHeaderExpert.GetComponent<ToggleButton>().SetToggle(_expert);
	}

	private void HandleButtonNormalUpEvent(ToggleButton source)
	{
		_expert = false;
		UpdateToggles();
		if (EventDifficultyChanged != null)
		{
			EventDifficultyChanged(_expert);
		}
	}

	private void HandleButtonExpertUpEvent(ToggleButton source)
	{
		_expert = true;
		UpdateToggles();
		if (EventDifficultyChanged != null)
		{
			EventDifficultyChanged(_expert);
		}
	}

	public void SetList(List<LeaderboardEntryVO> pEntries, bool pExpert)
	{
		pEntries = new List<LeaderboardEntryVO>(pEntries);
		Init(pExpert);
		int num = 0;
		int count = _entries.Count;
		_entryCount = pEntries.Count;
		if (_entryCount < 4)
		{
			for (int i = _entryCount; i < 4; i++)
			{
				pEntries.Add(default(LeaderboardEntryVO));
			}
			_entryCount = pEntries.Count;
		}
		foreach (LeaderboardEntryVO pEntry in pEntries)
		{
			LeaderboardEntry leaderboardEntry;
			if (num < count)
			{
				leaderboardEntry = _entries[num];
				leaderboardEntry.Reset();
			}
			else
			{
				GameObject gameObject = Object.Instantiate(EntryPrefab) as GameObject;
				leaderboardEntry = gameObject.GetComponent<LeaderboardEntry>();
				leaderboardEntry.transform.parent = GfxContent.transform;
				_entries.Add(leaderboardEntry);
			}
			leaderboardEntry.SetEntry(pEntry, num);
			num++;
		}
		for (int num2 = count - 1; num2 >= num; num2--)
		{
			Object.Destroy(_entries[num2].gameObject);
			_entries.RemoveAt(num2);
		}
		_palringoAdContainer.SetActive(false);
	}

	public void GotoEntry(LeaderboardEntryVO pEntry)
	{
		int num = _entries.FindIndex((LeaderboardEntry e) => e.Entry.userID == pEntry.userID);
		if (num < 0 || string.IsNullOrEmpty(pEntry.userID))
		{
			num = 0;
		}
		else
		{
			_entries[num].SetCurrentUserIndicator();
		}
		num = Mathf.Min(num, _entries.Count - 4);
		float position = 54f + (float)num * 36f;
		_scroll.position = position;
		_scroll.velocity = 0f;
	}

    private Camera _layoutHudCamera;
    private Rect _lastHudRect;
    private bool _lastLargeView;
	private void FixAspect()
    {
        if(_layoutHudCamera==null)_layoutHudCamera=GameObject.Find("Hud Camera").GetComponent<Camera>();
        Camera hud=_layoutHudCamera;
        Rect frame=LegacyScreenFit.ContentRect();
        float nativeHeight=frame.width/LegacyScreenFit.ReferenceAspect;
        frame=new Rect(frame.xMin,frame.center.y-nativeHeight*0.5f,frame.width,nativeHeight);
        var completed=Object.FindAnyObjectByType<PostCompleteScreen>();
        if(completed==null && frame==_lastHudRect && _lastLargeView==_isUsingLargeView)return;
        _lastHudRect=frame;_lastLargeView=_isUsingLargeView;
        float width=frame.width*0.94f;
        float height=width*178f/320f;
        float bottom=frame.yMin+frame.height*(_isUsingLargeView?0.36f:0.15f);
        if(completed!=null)
        {
            // This is an embedded panel above three result actions. Its slot
            // is different from the stand-alone leaderboard screen.
            Rect buttons=UnifiedHudLayout.ScreenBounds(hud,completed.ButtonNextLevel.gameObject,null,true);
            Rect safe=LegacyScreenFit.ContentRect();
            float gap=Screen.width*.025f;
            var header=Object.FindAnyObjectByType<MenuHeader>();
            float availableTop=header!=null?UnifiedHudLayout.ScreenBounds(hud,header.gameObject,null,true).yMin-gap:safe.center.y+frame.height*.3f;
            float availableBottom=buttons.yMax+gap;
            float availableHeight=Mathf.Max(1f,availableTop-availableBottom);
            height=Mathf.Min(height,availableHeight);
            width=height*320f/178f;
            bottom=availableBottom+(availableHeight-height)*.35f;
        }
        _viewCamera.rect=new Rect((frame.center.x-width*0.5f)/Screen.width,bottom/Screen.height,width/Screen.width,height/Screen.height);
        _viewCamera.orthographicSize=89f;
        _viewCamera.ResetProjectionMatrix();
        _viewCamera.aspect=320f/178f;
    }

    private void LateUpdate()
    {
        if(_viewCamera!=null)FixAspect();
    }

	public void SetUseLargeView()
	{
		_isUsingLargeView = true;
	}

	private void Update()
	{
		if (_scroll == null)
		{
			return;
		}
		Ray ray = _viewCamera.ScreenPointToRay(Input.mousePosition);
		float num = GfxContent.transform.localPosition.y;
		if (Input.GetMouseButton(0))
		{
			RaycastHit hitInfo;
			if (_isMouseDown)
			{
				float num2 = _inputStartY - Input.mousePosition.y;
				num = _contentYAtMouseDown - num2;
			}
			else if (base.GetComponent<Collider>().Raycast(ray, out hitInfo, 100000000f))
			{
				_inputStartY = Input.mousePosition.y;
				_contentYAtMouseDown = GfxContent.transform.localPosition.y;
				_isMouseDown = true;
			}
		}
		else if (_isMouseDown)
		{
			_scroll.velocity += (Input.mousePosition.y - lastMouseDownY) * 10f;
			_isMouseDown = false;
		}
		float num3 = 54f + (float)_entryCount * 36f;
		float num4 = num;
		if (num4 < 54f)
		{
			num4 = 54f;
		}
		else if (num4 > num3 - 180f + 54f)
		{
			num4 = num3 - 180f + 54f;
		}
		if (_isMouseDown)
		{
			if (num4 != num)
			{
				num += (num4 - num) * 0.5f;
			}
			lastMouseDownY = Input.mousePosition.y;
			_scroll.position = num;
		}
		else
		{
			if (num4 != num)
			{
				_scroll.drag = 5f;
				_scroll.SpringToTarget(num4, Time.deltaTime);
			}
			else
			{
				_scroll.drag = 0.5f;
			}
			_scroll.Update(Time.deltaTime);
			num = _scroll.position;
		}
		Vector3 localPosition = GfxHeaderNormal.transform.localPosition;
		localPosition.y = _headerOriginY + Mathf.Min(0f, num - 54f);
		GfxHeaderNormal.transform.localPosition = localPosition;
		localPosition = GfxHeaderExpert.transform.localPosition;
		localPosition.y = _headerOriginY + Mathf.Min(0f, num - 54f);
		GfxHeaderExpert.transform.localPosition = localPosition;
		localPosition = GfxContent.transform.localPosition;
		if (!_isMouseDown && _scroll.velocity < 15f && _scroll.velocity > -15f)
		{
			float num5 = 54f - num;
			if (num5 < 1f && num5 > -1f)
			{
				num = 54f;
				_scroll.position = num;
			}
		}
		localPosition.y = num;
		GfxContent.transform.localPosition = localPosition;
	}
}
