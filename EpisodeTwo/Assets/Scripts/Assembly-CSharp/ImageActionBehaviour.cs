using UnityEngine;

public class ImageActionBehaviour : MonoBehaviour
{
	private float showTime;

	private bool isDone;

	private GameObject Image;

	private float duration;

	private CameraFade _fade;

	public SimpleText[] texts;

	public Camera HudCamera;

	private bool isFading;

	private Renderer background;
	private Vector3 backgroundScale;
	private Vector3 backgroundPosition;
	private Vector3 imageScale;
	private float fittedSize = -1f;
	private float fittedAspect = -1f;

	private void Awake()
	{
		SetTextVisibility(false);
		base.gameObject.SetActive(false);
		_fade = base.gameObject.AddComponent<CameraFade>();
	}

	public void Show(string imageName)
	{
		showTime = Time.realtimeSinceStartup;
		isDone = false;
		switch (imageName)
		{
		case "instructions":
		case "calibration":
			SetTextVisibility(true);
			UpdateImage(imageName);
			duration = 5f;
			if (imageName == "calibration")
			{
				tk2dFontData font = texts[3].GetComponent<tk2dTextMesh>().font;
				texts[1].GetComponent<tk2dTextMesh>().font = font;
				texts[2].GetComponent<tk2dTextMesh>().font = font;
				texts[0].SetStringID("CALIBRATE CONTROLS");
				texts[1].SetStringID("CALIBRATE CONTROLS_INSTRUCTIONS1");
				texts[2].SetStringID("CALIBRATE CONTROLS_INSTRUCTIONS2");
				texts[3].SetStringID("CALIBRATE CONTROLS_INSTRUCTIONS3");
				texts[4].SetStringID("CALIBRATE CONTROLS_INSTRUCTIONS4");
				texts[5].SetStringID("CALIBRATE CONTROLS_INSTRUCTIONS5");
				for (int i = 1; i < 6; i++)
				{
					texts[i].transform.position = texts[0].transform.position + new Vector3(0f, i * -20 - 20, 0f);
				}
				if (GamePreferences.Chinese)
				{
					texts[1].SetStringID("CALIBRATE.INSTRUCTIONS.ANGLE");
					texts[1].transform.position = texts[0].transform.position + new Vector3(0f, -48f, 0f);
					for (int i = 2; i < 6; i++) texts[i].gameObject.SetActive(false);
				}
			}
			break;
		default:
			SetTextVisibility(false);
			UpdateImage(imageName);
			duration = 3f;
			break;
		}
		base.gameObject.SetActive(true);
        // Timed tutorial actions must finish before navigation can change HUD state.
        SceneTransitionInput.SuppressFor(duration + .7f);
		isFading = false;
		_fade.FadeIn();
		Hud.ImageActionRunning = true;
	}

	private void SetTextVisibility(bool pVisible)
	{
		for (int i = 0; i < base.gameObject.transform.childCount; i++)
		{
			base.gameObject.transform.GetChild(i).gameObject.SetActive(pVisible);
		}
	}

	protected void UpdateImage(string imageName)
	{
		if (Image != null)
		{
			Object.Destroy(Image);
		}
		Image = Object.Instantiate(LevelAssets.GetAsset(imageName).gameObject) as GameObject;
		if (Image != null)
		{
			Image.transform.parent = base.transform;
			Image.transform.localPosition = Vector3.zero;
			imageScale = Image.transform.localScale;
			Image.SetActive(true);
			World.hud.ShowHudIcons(false);
			Transform backdrop = Image.transform.Find("Background");
			background = backdrop != null ? backdrop.GetComponent<Renderer>() : null;
			if (background != null)
			{
				backgroundScale = background.transform.localScale;
				backgroundPosition = background.transform.localPosition;
			}
			FixAspect();
		}
	}

	private void FixAspect()
	{
		if (Image == null || HudCamera == null) return;
		if (background != null)
		{
			// Fit only scenery in a hierarchy; phone, arrows and labels retain their scale.
			LegacyScreenFit.CoverBackdrop(HudCamera, background, backgroundScale, backgroundPosition);
		}
		else if (Image.GetComponent<Renderer>() != null)
		{
			LegacyScreenFit.CoverBackdrop(HudCamera, Image.GetComponent<Renderer>(), imageScale, Vector3.zero);
		}
		fittedSize = HudCamera.orthographicSize;
		fittedAspect = HudCamera.aspect;
	}

	private void LateUpdate()
	{
		if (Image != null && HudCamera != null &&
			(!Mathf.Approximately(fittedSize, HudCamera.orthographicSize) || !Mathf.Approximately(fittedAspect, HudCamera.aspect))) FixAspect();
	}

	private void Update()
	{
		if (!isFading && Time.realtimeSinceStartup > showTime + duration)
		{
			_fade.FadeOut();
			isFading = true;
		}
		if (!isDone && Time.realtimeSinceStartup > showTime + duration + 0.5f)
		{
			if (Image != null)
			{
				Object.Destroy(Image);
			}
			base.gameObject.SetActive(false);
			isDone = true;
			Hud.ImageActionRunning = false;
			Hud.Instance.ShowHudIcons();
		}
	}

	public bool IsDone()
	{
		return isDone;
	}
}
