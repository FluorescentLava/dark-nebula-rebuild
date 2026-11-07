using System;
using System.Collections;
using System.Text.RegularExpressions;
using FLD;
using UnityEngine;

public class LevelSelectScreen : MonoBehaviour
{
	[SerializeField]
	private LevelCircle[] _levelCircles;

	[SerializeField]
	private SwipeSelector _swipeSelector;

	[SerializeField]
	private MenuHeader _menuheader;

	public GameObject btnStart;

	public GameObject btnCheat;

	public UnlockAllEffect UnlockEffect;

	public Camera viewCamera;

	public Action<int> LevelSelectedEvent;

	private CameraFade fade;

	private bool _isEnabled = true;

	private void OnDisable()
	{
		fade = null;
	}

	private void Start()
	{
		DarkNebulaAdsManager.Instance.ShowAd("LevelSelect").Wait((string obj) =>
		{
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		_menuheader.SetTitleID("SELECT LEVEL", true);
		btnStart.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onStartLevel;
		_menuheader.BackButton.ButtonUpEvent += onMainMenu;
		if (Debug.isDebugBuild)
		{
			SimpleButton[] componentsInChildren = btnCheat.GetComponentsInChildren<SimpleButton>();
			SimpleButton[] array = componentsInChildren;
			foreach (SimpleButton simpleButton in array)
			{
				simpleButton.ButtonUpEvent += onCheat;
			}
		}
		else
		{
			UnityEngine.Object.Destroy(btnCheat);
		}
		int num = UserProgress.Instance.GetMaxSelectableLevel(UserSettings.Instance.IsExpertMode);
		if (num == 11 || num == 23)
		{
			num--;
		}
		SelectLevel(num);
		if (num > 11)
		{
			_swipeSelector.SelectedIndex = 1;
			if (num > 12)
			{
				_swipeSelector.SnapTo(_swipeSelector.SelectedIndex);
			}
			else
			{
				_swipeSelector.SlowMoveWithDelayTo(_swipeSelector.SelectedIndex, 0.5f);
			}
		}
	}

	private void Update()
	{
		if (_isEnabled && (Input.GetKeyUp(KeyCode.Menu) || Input.GetKeyUp(KeyCode.Escape)))
		{
			MainMenu();
		}
		if (!Input.GetMouseButtonUp(0))
		{
			return;
		}
		Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, 100000000f))
		{
			string s = hitInfo.collider.gameObject.name.Replace("Collider", string.Empty);
			int result = -1;
			if (int.TryParse(s, out result) && result <= UserProgress.Instance.GetMaxSelectableLevel(UserSettings.Instance.IsExpertMode))
			{
				SelectLevel(result);
			}
		}
	}

	private void SelectLevel(int pLevel)
	{
		_menuheader.SetLevel(pLevel);
		LevelCircle[] levelCircles = _levelCircles;
		foreach (LevelCircle levelCircle in levelCircles)
		{
			if (levelCircle.HasLevel(pLevel))
			{
				levelCircle.SelectLevel(pLevel);
			}
			else
			{
				levelCircle.UnselectLevel();
			}
		}
		if (ContentGatingManager.Instance.IsLevelGated(pLevel) && !ContentGatingManager.Instance.IsGateUnlocked(pLevel))
		{
			btnStart.GetComponent<SimpleButton>().Text = "UNLOCK LEVEL".Localize();
		}
		else
		{
			btnStart.GetComponent<SimpleButton>().Text = "START LEVEL".Localize();
		}
		if (LevelSelectedEvent != null)
		{
			LevelSelectedEvent(pLevel);
		}
	}

	private void SetEnabled(bool on)
	{
		_isEnabled = on;
		btnStart.GetComponentInChildren<SimpleButton>().enabled = on;
		_menuheader.BackButton.GetComponentInChildren<SimpleButton>().enabled = on;
	}

	private IEnumerator LoadLevel(string levelName)
	{
        if (!SceneTransitionInput.TryBegin()) yield break;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}

	private void onStartLevel(SimpleButton source)
	{
		LevelCircle[] levelCircles = _levelCircles;
		foreach (LevelCircle levelCircle in levelCircles)
		{
			if (levelCircle.HasSelectedLevel)
			{
				LoadLevelScreen.LevelToLoad = levelCircle.selectedLevel;
				break;
			}
		}
		UserSettings.Instance.SetLastSelectedLevel(LoadLevelScreen.LevelToLoad, UserSettings.Instance.IsExpertMode);
		if (ContentGatingManager.Instance.IsLevelGated(LoadLevelScreen.LevelToLoad) && !ContentGatingManager.Instance.IsGateUnlocked(LoadLevelScreen.LevelToLoad))
		{
			StartCoroutine(LoadLevel("GateLock"));
		}
		else
		{
			StartCoroutine(LoadLevel("BoosterSelection"));
		}
	}

	private void onMainMenu(SimpleButton source)
	{
		MainMenu();
	}

	private void MainMenu()
	{
		StartCoroutine(LoadLevel("MainMenu"));
	}

	private void onCheat(SimpleButton source)
	{
		tk2dTextMesh componentInChildren = source.GetComponentInChildren<tk2dTextMesh>();
		int num = FindIntInString(componentInChildren.text);
		Debug.Log("Cheat goto level " + num);
		if (num != 0)
		{
			LoadLevelScreen.LevelToLoad = num;
		}
		else
		{
			LoadLevelScreen.LevelToLoad = 11;
		}
		StartCoroutine(LoadLevel("LoadLevel"));
	}

	private int FindIntInString(string str)
	{
		string value = Regex.Match(str, "\\d+").Value;
		int result = 0;
		if (int.TryParse(value, out result))
		{
			return result;
		}
		return 0;
	}

	private void HandleDarkNebulaStoreInstanceonPurchaseFinished(string pId)
	{
		CleanupTryPurchase();
		Debug.Log("HandleDarkNebulaStoreInstanceonPurchaseFinished " + pId);
	}

	private void HandleDarkNebulaStoreInstanceonPurchaseFailed(bool pCanceled, string pId, string pMessage)
	{
		CleanupTryPurchase();
		Debug.Log("HandleDarkNebulaStoreInstanceonPurchaseFailed " + pCanceled + ", " + pId + ", " + pMessage);
	}

	private void CleanupTryPurchase()
	{
		SetEnabled(true);
	}

	private void ShowBoughtEffect()
	{
		UnlockAllEffect unlockAllEffect = (UnlockAllEffect)UnityEngine.Object.Instantiate(UnlockEffect);
		unlockAllEffect.transform.position = base.transform.position;
		Invoke("UpdateLevelCircle", unlockAllEffect.UnlockDelay);
	}

	private void UpdateLevelCircle()
	{
		LevelCircle[] levelCircles = _levelCircles;
		foreach (LevelCircle levelCircle in levelCircles)
		{
			if (levelCircle.HasSelectedLevel)
			{
				levelCircle.SelectLevel(levelCircle.selectedLevel);
			}
		}
		SetEnabled(true);
	}
}
