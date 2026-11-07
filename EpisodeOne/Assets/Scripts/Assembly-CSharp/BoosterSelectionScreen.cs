using System;
using System.Collections;
using System.Collections.Generic;
using FLD;
using UnityEngine;

public class BoosterSelectionScreen : MonoBehaviour
{
	[SerializeField]
	private List<BoosterButton> _boosterButtons;

	[SerializeField]
	private SimpleButton _startButton;

	[SerializeField]
	private MenuHeader _menuHeader;

	private CameraFade fade;

	private bool _isEnabled = true;

	private void Start()
	{
		DarkNebulaAdsManager.Instance.ShowAd("BoosterSelect").Wait((string obj) =>
		{
			StartProcedure();
		});
	}

	private void StartProcedure()
	{
		_startButton.Text = "PLAY".Localize();
		_menuHeader.SetTitleID("BOOSTER HEADER", true);
		_menuHeader.UpdateCurrentLevel();
		_isEnabled = true;
		Time.timeScale = 1f;
		fade = base.gameObject.AddComponent<CameraFade>();
		fade.FadeIn();
		foreach (BoosterButton boosterButton in _boosterButtons)
		{
			boosterButton.BuyClicked = (Action<Booster>)Delegate.Combine(boosterButton.BuyClicked, new Action<Booster>(HandleBuyBooster));
		}
		_menuHeader.BackButton.ButtonUpEvent += HandleButtonUpEvent;
		_startButton.ButtonUpEvent += HandleButtonUpEvent;
	}

	private void HandleBuyBooster(Booster pBooster)
	{
		BoosterManager.Instance.BoosterToBuy = pBooster;
		StartCoroutine(LoadScene("BoosterShop"));
	}

	private void HandleButtonUpEvent(SimpleButton pButton)
	{
		if (pButton == _menuHeader.BackButton)
		{
			StartCoroutine(LoadScene("LevelSelect"));
		}
		else if (pButton == _startButton)
		{
			StartCoroutine(LoadScene("LoadLevel"));
		}
		else
		{
			Debug.LogWarning("Something unknown was clicked");
		}
	}

	private void Update()
	{
		if (_isEnabled && Input.GetKeyUp(KeyCode.Escape))
		{
			StartCoroutine(LoadScene("LevelSelect"));
		}
	}

	private IEnumerator LoadScene(string levelName)
	{
        if (!SceneTransitionInput.TryBegin()) yield break;
		_menuHeader.BackButton.ButtonUpEvent -= HandleButtonUpEvent;
		_startButton.ButtonUpEvent -= HandleButtonUpEvent;
		_isEnabled = false;
		fade.FadeOut();
		yield return new WaitForSeconds(0.5f);
		Application.LoadLevel(levelName);
	}
}
