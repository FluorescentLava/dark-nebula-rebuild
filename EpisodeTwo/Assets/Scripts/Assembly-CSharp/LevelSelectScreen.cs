using System.Collections;
using UnityEngine;

public class LevelSelectScreen : MonoBehaviour
{
	public GameObject btnStart;

	public GameObject btnBack;

	public GameObject btnCheat;

	public static bool ForceExtendedPack;

	public FlipperButton LevelPackFlipper;

	public GameObject btnRemoveAds;

	public GameObject title;

	public UnlockAllEffect UnlockEffect;

	public Camera viewCamera;

	private CameraFade fade;

	public LevelCircle levelCircle;

	public LevelCircle levelCircleExtended;

	private LevelCircle currentLevelCircle;

	private int _maxSelectableLevelAtStart;

	private LevelPack CurrentLevelPack;

	private bool _isEnabled = true;

	private void OnDisable()
	{
		fade = null;
	}

	private void Awake()
	{
		levelCircle.gameObject.SetActive(true);
		levelCircleExtended.gameObject.SetActive(true);
	}

	private void Start()
	{
		Time.timeScale = 1f;
		CurrentLevelPack = UserSettings.I.GetCurrentPack();
		if (ForceExtendedPack)
		{
			ForceExtendedPack = false;
			CurrentLevelPack = LevelPack.Extended;
		}
		LevelPackFlipper.Init(CurrentLevelPack == LevelPack.Original, "ORIGINAL", "EXTENDED", string.Empty);
		LevelPackFlipper.EventValueChanged += OnLevelPackChanged;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		title.GetComponentInChildren<SimpleText>().SetStringID("SELECT LEVEL");
		btnStart.GetComponentInChildren<SimpleText>().SetStringID("START LEVEL");
		btnStart.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onStartLevel;
		btnBack.GetComponentInChildren<SimpleButton>().ButtonUpEvent += onMainMenu;
		btnRemoveAds.SetActive(false); // No purchase entry in the offline build.
		Object.Destroy(btnCheat);
		_maxSelectableLevelAtStart = UserSettings.I.GetMaxSelectableLevel(CurrentLevelPack);
		Debug.Log("MaxLevel: " + _maxSelectableLevelAtStart + " lastLevel: " + UserSettings.I.GetLastSelectedLevel(LevelPack.Original) + ":" + UserSettings.I.GetLastSelectedLevel(LevelPack.Extended));
		UpdateRemoveAds();
		levelCircle.SelectLevel(UserSettings.I.GetLastSelectedLevel(LevelPack.Original));
		levelCircleExtended.SelectLevel(UserSettings.I.GetLastSelectedLevel(LevelPack.Extended));
		ChangeLevelPack(CurrentLevelPack, true, true);
		DarkNebulaMetrics.StartScreen(DarkNebulaMetrics.Screens.LevelSelect);
		if (!BuildSettings.Current.ExtendedLevelsEnabled)
		{
			LevelPackFlipper.gameObject.SetActive(false);
		}
		var offlineLayout = gameObject.AddComponent<OfflineLevelSelectLayout>();
		offlineLayout.Camera = viewCamera;
		offlineLayout.StartButton = btnStart;
		offlineLayout.PackSelector = LevelPackFlipper.gameObject;
		offlineLayout.RemovedAdPosition = btnRemoveAds.transform.position;
		Object.Destroy(btnRemoveAds);
		btnRemoveAds = null;
	}

	private void OnLevelPackChanged(bool value)
	{
		if (value)
		{
			ChangeLevelPack(LevelPack.Original, LevelPackFlipper.LastTapWasLeft);
		}
		else
		{
			ChangeLevelPack(LevelPack.Extended, LevelPackFlipper.LastTapWasLeft);
		}
	}

	private void ChangeLevelPack(LevelPack pNewPack, bool pAnimateFromLeft, bool pSnapIn = false)
	{
		CurrentLevelPack = pNewPack;
		if (!BuildSettings.Current.ExtendedLevelsEnabled)
		{
			CurrentLevelPack = LevelPack.Original;
		}
		UserSettings.I.SetCurrentPack(CurrentLevelPack);
		if (CurrentLevelPack == LevelPack.Original)
		{
			currentLevelCircle = levelCircle;
			if (pSnapIn)
			{
				levelCircle.SnapIn();
				levelCircleExtended.SnapOut();
			}
			else
			{
				levelCircle.AnimateIn(pAnimateFromLeft);
				levelCircleExtended.AnimateOut(pAnimateFromLeft);
			}
			levelCircle.SelectLevel(levelCircle.selectedLevel);
		}
		else
		{
			currentLevelCircle = levelCircleExtended;
			if (pSnapIn)
			{
				levelCircle.SnapOut();
				levelCircleExtended.SnapIn();
			}
			else
			{
				levelCircle.AnimateOut(pAnimateFromLeft);
				levelCircleExtended.AnimateIn(pAnimateFromLeft);
			}
			levelCircleExtended.SelectLevel(levelCircleExtended.selectedLevel);
		}
	}

	private void UpdateRemoveAds()
	{
		if (btnRemoveAds != null) btnRemoveAds.SetActive(false);
	}

	private void Update()
	{
#if UNITY_EDITOR
        if(UnityEditor.SessionState.GetBool("WideReview",false))return;
#endif
		if (_isEnabled && (Input.GetKeyUp(KeyCode.Menu) || Input.GetKeyUp(KeyCode.Escape)))
		{
			MainMenu();
		}
		if (!Input.GetMouseButton(0))
		{
			return;
		}
		Ray ray = viewCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo;
		if (Physics.Raycast(ray, out hitInfo, 100000000f))
		{
			string s = hitInfo.collider.gameObject.name.Replace("Collider", string.Empty);
			int result = -1;
			if (int.TryParse(s, out result) && result <= UserSettings.I.GetMaxSelectableLevel(CurrentLevelPack) && currentLevelCircle.selectedLevel != result)
			{
				currentLevelCircle.SelectLevel(result);
			}
		}
	}

	private void SetEnabled(bool on)
	{
		_isEnabled = on;
		btnStart.GetComponentInChildren<SimpleButton>().enabled = on;
		btnBack.GetComponentInChildren<SimpleButton>().enabled = on;
		if (on)
		{
			UpdateRemoveAds();
		}
		else
		{
			if (btnRemoveAds != null) btnRemoveAds.SetActive(false);
		}
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
		SleepTimeoutManager.NeverSleep();
		LoadLevelScreen.LevelToLoad = currentLevelCircle.selectedLevel;
		World.IsRestart = false;
		StartCoroutine(LoadLevel("LoadLevel"));
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
		LoadLevelScreen.LevelToLoad = 11;
		StartCoroutine(LoadLevel("LoadLevel"));
	}

	private void onRemoveAds(SimpleButton source)
	{
		// Purchase entry removed.
	}

	private void HandleDarkNebulaStoreInstanceonPurchaseFinished(string pId)
	{
		DarkNebulaMetrics.BoughtIAP("darknebula.ep2.removeads");
		CleanupTryPurchase();
		Debug.Log("HandleDarkNebulaStoreInstanceonPurchaseFinished " + pId);
	}

	private void HandleDarkNebulaStoreInstanceonPurchaseFailed(bool pCanceled, string pId, string pMessage)
	{
		if (pCanceled)
		{
			DarkNebulaMetrics.BoughtIAPCancelled("darknebula.ep2.removeads");
		}
		else
		{
			DarkNebulaMetrics.BoughtIAPFailed("darknebula.ep2.removeads", pMessage);
		}
		CleanupTryPurchase();
		Debug.Log("HandleDarkNebulaStoreInstanceonPurchaseFailed " + pCanceled + ", " + pId + ", " + pMessage);
	}

	private void CleanupTryPurchase()
	{
		DarkNebulaStore.Instance.onPurchaseFailed -= HandleDarkNebulaStoreInstanceonPurchaseFailed;
		DarkNebulaStore.Instance.onPurchaseFinished -= HandleDarkNebulaStoreInstanceonPurchaseFinished;
		if (DarkNebulaStore.HasRemovedAds())
		{
			Invoke("ShowUnlockEffect", 0.1f);
		}
		else
		{
			SetEnabled(true);
		}
	}

	private void ShowUnlockEffect()
	{
		UnlockAllEffect unlockAllEffect = (UnlockAllEffect)Object.Instantiate(UnlockEffect);
		unlockAllEffect.transform.position = base.transform.position;
		Invoke("UpdateLevelCircle", unlockAllEffect.UnlockDelay);
	}

	private void UpdateLevelCircle()
	{
		currentLevelCircle.SelectLevel(currentLevelCircle.selectedLevel);
		SetEnabled(true);
	}
}
