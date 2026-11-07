using FLD;
using UnityEngine;

public class UserSettings
{
	private const string SelectedAvatarKey = "SelectedAvatar";

	private const string LastSelectedLevelNormalKey = "LastSelectedLevelNormal";

	private const string LastSelectedLevelExpertKey = "LastSelectedLevelExpert";

	private static UserSettings _instance;

	private StorageUnit _settings;

	public Quaternion centerCalibration = Quaternion.LookRotation(Vector3.back, Vector3.up);

	public static UserSettings Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new UserSettings();
			}
			return _instance;
		}
	}

	public bool IsExpertMode
	{
		get
		{
			_settings.Set("Expert", false, false);
			return _settings.GetBool("Expert");
		}
		set
		{
			_settings.Set("Expert", value);
		}
	}

	public string ActiveAvatarId
	{
		get
		{
			return _settings.GetString("SelectedAvatar");
		}
		set
		{
			_settings.Set("SelectedAvatar", value);
		}
	}

	public UserSettings()
	{
		_settings = Storage.persistent;
		Load();
	}

	private void Load()
	{
		_settings.SetTyped("centerCalibration", Quaternion.LookRotation(Vector3.back, Vector3.up), false);
		centerCalibration = _settings.GetQuaternion("centerCalibration");
	}

	public void Calibrate()
	{
        LevelMotionCalibration.Cancel();
		Vector3 forward = LevelMotionCalibration.Gravity;
		forward.Normalize();
		if (forward.sqrMagnitude == 0f)
		{
			forward = new Vector3(0f, 0f, -1f);
		}
		centerCalibration = Quaternion.LookRotation(forward, Vector3.up);
		_settings.SetTyped("centerCalibration", centerCalibration);
	}

	public static string SpecialSegmentString(int num)
	{
		if (num < 10)
		{
			return num.ToString();
		}
		if (num < 30)
		{
			int num2 = Mathf.RoundToInt(num / 5) * 5;
			return num2 + "-" + (num2 + 4);
		}
		if (num < 50)
		{
			int num3 = Mathf.RoundToInt(num / 10) * 10;
			return num3 + "-" + (num3 + 9);
		}
		if (num < 100)
		{
			return "50-99";
		}
		if (num < 250)
		{
			return "100-249";
		}
		if (num < 500)
		{
			return "250-499";
		}
		return "500+";
	}

	public int GetLastSelectedLevel(bool pIsExpert)
	{
		string pID = ((!pIsExpert) ? "LastSelectedLevelNormal" : "LastSelectedLevelExpert");
		return Mathf.Max(1, _settings.GetInt(pID));
	}

	public void SetLastSelectedLevel(int pLevel, bool pIsExpert)
	{
		string pID = ((!pIsExpert) ? "LastSelectedLevelNormal" : "LastSelectedLevelExpert");
		int num = Mathf.Min(pLevel, 22);
		_settings.Set(pID, num);
	}

	public string GetUserName()
	{
		string name = _settings.GetString("UserName");
		return string.IsNullOrWhiteSpace(name) ? "PLAYER".Localize() : name;
	}

	public void SetUserName(string name)
	{
		// Store the literal nickname, so changing language never translates it.
		_settings.Set("UserName", string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim());
		PlayerPrefs.Save();
	}

	public bool GetVibration()
	{
        return GamePreferences.Vibration != VibrationMode.Disabled;
	}

	public void SetVibration(bool value)
	{
		if (!value)
		{
			GamePreferences.Vibration = VibrationMode.Disabled;
		}
		else if (GamePreferences.Vibration == VibrationMode.Disabled)
		{
			GamePreferences.Vibration = VibrationMode.Original;
		}
	}

	public static float GetDifficultValue(float[] pValue)
	{
		if (Instance.IsExpertMode)
		{
			return pValue[0] + (pValue[1] - pValue[0]) * 0.75f;
		}
		return pValue[0];
	}

	public string GetSceneName(int levelToLoad)
	{
		string empty = string.Empty;
		if (levelToLoad > 11)
		{
			return "level1b-" + (levelToLoad - 11);
		}
		return "level1-" + levelToLoad;
	}

	public void SetBoosterEnabled(string pID, bool pEnabled)
	{
		_settings.Set("BoosterEnabled." + pID, pEnabled);
	}

	public bool IsBoosterEnabled(string pID)
	{
		string pID2 = "BoosterEnabled." + pID;
		if (!_settings.Has(pID2))
		{
			return UserPurchases.Instance.HasProduct(pID);
		}
		return _settings.GetBool(pID2);
	}
}
