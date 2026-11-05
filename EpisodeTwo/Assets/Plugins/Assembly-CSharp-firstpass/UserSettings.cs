using System;
using FLD;
using UnityEngine;

public class UserSettings
{
	private static UserSettings _instance;

	private bool _expert;

	public Quaternion centerCalibration = Quaternion.LookRotation(Vector3.back, Vector3.up);

	private StorageUnit settings;

	public static UserSettings I
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

	public bool expert
	{
		get
		{
			return _expert;
		}
	}

	public UserSettings()
	{
		settings = Storage.persistent;
		Load();
	}

	static UserSettings()
	{
		int displayRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        Application.targetFrameRate = displayRate > 0 ? displayRate : 60;
	}

	private static void SetQuality(int level, bool force)
	{
		QualitySettings.SetQualityLevel(level, force);
		Debug.Log("Set quality to " + QualitySettings.names[level]);
	}

	private void Load()
	{
		settings.SetTyped("centerCalibration", Quaternion.LookRotation(Vector3.back, Vector3.up), false);
		centerCalibration = settings.GetQuaternion("centerCalibration");
		_expert = GetExpert();
	}

	public void ResetToDefaults()
	{
		settings.Delete();
		Load();
	}

	public void Calibrate()
	{
		Vector3 forward = LevelMotionCalibration.Gravity;
		forward.Normalize();
		if (forward.sqrMagnitude == 0f)
		{
			forward = new Vector3(0f, 0f, -1f);
		}
		centerCalibration = Quaternion.LookRotation(forward, Vector3.up);
		settings.SetTyped("centerCalibration", centerCalibration);
	}

	public bool GetHasPlayedIntroMovie()
	{
		settings.Set("HasPlayedIntroMovie", false, false);
		return settings.GetBool("HasPlayedIntroMovie");
	}

	public void SetHasPlayedIntroMovie(bool value)
	{
		settings.Set("HasPlayedIntroMovie", value);
	}

	public bool GetEnforceInternet()
	{
		if (GetIsPayingUser())
		{
			if (Debug.isDebugBuild)
			{
				Debug.Log("Is Paying User");
			}
			return false;
		}
		if (!BuildSettings.Current.ChartBoostEnabled && !BuildSettings.Current.VungleEnabled)
		{
			if (Debug.isDebugBuild)
			{
				Debug.Log("No ads enabled");
			}
			return false;
		}
		bool flag = Application.internetReachability == NetworkReachability.NotReachable;
		if (Debug.isDebugBuild)
		{
			if (flag)
			{
				Debug.Log("Isn't connected to the internet");
			}
			else
			{
				Debug.Log("Is connected to the internet");
			}
		}
		return flag;
	}

	public int GetDayAndUpdateDayStats()
	{
		DateTime firstTimePlayed = GetFirstTimePlayed();
		TimeSpan timeSpan = DateTime.Now - firstTimePlayed;
		int lastPlayedDay = GetLastPlayedDay();
		SetLastPlayedDay(timeSpan.Days);
		if (timeSpan.Days != lastPlayedDay)
		{
			if (timeSpan.Days == lastPlayedDay + 1)
			{
				SetConsecutiveDay(GetConsecutiveDay() + 1);
				SetTotalDays(GetTotalDays() + 1);
			}
			else
			{
				SetConsecutiveDay(0);
				SetTotalDays(GetTotalDays() + 1);
			}
		}
		return timeSpan.Days;
	}

	private int GetLastPlayedDay()
	{
		settings.Set("lastPlayedDay", 0, false);
		return settings.GetInt("lastPlayedDay");
	}

	private void SetLastPlayedDay(int day)
	{
		settings.Set("lastPlayedDay", day);
	}

	public int GetConsecutiveDay()
	{
		settings.Set("consecutivePlayedDay", 0, false);
		return settings.GetInt("consecutivePlayedDay");
	}

	private void SetConsecutiveDay(int days)
	{
		settings.Set("consecutivePlayedDay", days);
	}

	public int GetTotalDays()
	{
		settings.Set("totalDays", 1, false);
		return settings.GetInt("totalDays");
	}

	public void SetTotalDays(int days)
	{
		settings.Set("totalDays", days);
	}

	public bool IsFirstTimePlaying()
	{
		bool result = !settings.Has("firstTimePlayed");
		GetFirstTimePlayed();
		return result;
	}

	public DateTime GetFirstTimePlayed()
	{
		settings.Set("firstTimePlayed", DateTime.Now.ToBinary().ToString(), false);
		long dateData = settings.GetLong("firstTimePlayed");
		return DateTime.FromBinary(dateData);
	}

	public int GetTimesPlayed()
	{
		settings.Set("timesPlayed", 0, false);
		return settings.GetInt("timesPlayed");
	}

	public void IncreaseTimesPlayed()
	{
		settings.Set("timesPlayed", GetTimesPlayed() + 1);
	}

	public void SetExpert(bool value)
	{
		_expert = value;
		settings.Set("Expert", _expert);
	}

	public bool GetExpert()
	{
		settings.Set("Expert", false, false);
		return settings.GetBool("Expert");
	}

	public int GetLevelStar(int levelNum, bool expert)
	{
		return settings.GetInt("LevelStar" + levelNum + "-" + expert);
	}

	public void SetLevelStar(int levelNum, int starValue, bool expert)
	{
		settings.Set("LevelStar" + levelNum + "-" + expert, starValue);
	}

	public bool GetIsAllGold(int pFirstLevel, int pLastLevel, bool pExpert)
	{
		for (int i = pFirstLevel; i <= pLastLevel; i++)
		{
			if (GetLevelStar(i, pExpert) != 3)
			{
				return false;
			}
		}
		return true;
	}

	public string GetSegmentedTimesPlayed()
	{
		return SpecialSegmentString(GetTimesPlayed());
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

	public int GetLastSelectedLevel(LevelPack pack)
	{
		string text = pack.ToString();
		settings.Set("LastSelectedLevel" + text, GetPackMinLevel(pack), false);
		return Mathf.Clamp(settings.GetInt("LastSelectedLevel" + text), GetPackMinLevel(pack), GetPackMaxLevel(pack) - 1);
	}

	public void SetLastSelectedLevel(int level)
	{
		LevelPack packFromLevelNum = GetPackFromLevelNum(level);
		settings.Set("LastSelectedLevel" + packFromLevelNum, level);
	}

	public void SetCompletedLevel(int level, bool expert)
	{
		LevelPack packFromLevelNum = GetPackFromLevelNum(level);
		int maxSelectableLevel = GetMaxSelectableLevel(packFromLevelNum);
		string text = packFromLevelNum.ToString();
		if (level + 1 >= maxSelectableLevel)
		{
			settings.Set("MaxSelectableLevel" + text, level + 1);
		}
		SetLastSelectedLevel(level + 1);
		// Commit all score and unlock changes at the completion boundary.
		PlayerPrefs.Save();
	}

	public void ResetHighestPlayedLevel()
	{
		settings.Set("HighestPlayedLevel", 0);
	}

	public int GetMaxSelectableLevel(LevelPack pack)
	{
		// Preserve actual unlock progression even in a development build.
		string text = pack.ToString();
		settings.Set("MaxSelectableLevel" + text, GetPackMinLevel(pack), false);
		return Mathf.Clamp(settings.GetInt("MaxSelectableLevel" + text), GetPackMinLevel(pack), GetPackMaxLevel(pack));
	}

	public void SetMaxSelectableLevel(int pLevel)
	{
		LevelPack packFromLevelNum = GetPackFromLevelNum(pLevel);
		string text = packFromLevelNum.ToString();
		settings.Set("MaxSelectableLevel" + text, pLevel);
	}

	public void SetLastPlayedLevel(int level)
	{
		settings.Set("LastPlayedLevel", level);
	}

	public int GetLastPlayedLevel()
	{
		settings.Set("LastPlayedLevel", 1, false);
		return settings.GetInt("LastPlayedLevel");
	}

	public string GetUserName()
	{
		settings.Set("UserName", Language.Get("Player"), false);
		return settings.GetString("UserName");
	}

	public void SetUserName(string name)
	{
		settings.Set("UserName", name);
		PlayerPrefs.Save();
	}

	public float GetBestTime(int levelNum, bool expert)
	{
		float time = settings.GetFloat("BestTime" + levelNum + "-" + expert);
		return time > 0f && !float.IsNaN(time) && !float.IsInfinity(time) ? time : 0f;
	}

	public void SetBestTime(float time, int levelNum, bool expert)
	{
		if (time > 0f && !float.IsNaN(time) && !float.IsInfinity(time)) settings.Set("BestTime" + levelNum + "-" + expert, time);
	}

	public void RemoveBestTime(int levelNum, bool expert)
	{
		settings.Delete("BestTime" + levelNum + "-" + expert);
	}

	public bool GetPostResults()
	{
		settings.Set("PostResults", true, false);
		return settings.GetBool("PostResults");
	}

	public void SetPostResults(bool value)
	{
		settings.Set("PostResults", value);
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

	public int GetHighestPlayedLevel(LevelPack pack)
	{
		string text = pack.ToString();
		settings.Set("HighestPlayedLevel" + text, GetPackMinLevel(pack) - 1, false);
		return settings.GetInt("HighestPlayedLevel" + text);
	}

	public void SetHighestPlayedLevel(int value)
	{
		LevelPack packFromLevelNum = GetPackFromLevelNum(value);
		string text = packFromLevelNum.ToString();
		settings.Set("HighestPlayedLevel" + text, Math.Max(value, GetHighestPlayedLevel(packFromLevelNum)));
	}

	public int GetSkipableAdsSeen()
	{
		settings.Set("SkipableAdsSeen", 0, false);
		return settings.GetInt("SkipableAdsSeen");
	}

	public void SetSkipableAdsSeen(int value)
	{
		settings.Set("SkipableAdsSeen", value);
	}

	public bool GetFirstTimeInLevelComplete()
	{
		settings.Set("FirstTimeInLevelComplete", true, false);
		return settings.GetBool("FirstTimeInLevelComplete");
	}

	public void SetFirstTimeInLevelComplete(bool value)
	{
		settings.Set("FirstTimeInLevelComplete", value);
	}

	public bool GetHasAnyProgress()
	{
		for (int i = 1; i <= 10; i++)
		{
			if (GetLevelStar(i, false) != 0)
			{
				return true;
			}
			if (GetLevelStar(i, true) != 0)
			{
				return true;
			}
		}
		return false;
	}

	public void SetString(string pName, string value)
	{
		settings.Set(pName, value);
	}

	public string GetString(string pName, string pDefault = null)
	{
		settings.Set(pName, pDefault, false);
		return settings.GetString(pName);
	}

	public bool GetHasShownYouAreGood()
	{
		settings.Set("HasShownYouAreGood", false, false);
		return settings.GetBool("HasShownYouAreGood");
	}

	public void SetHasShownYouAreGood(bool value)
	{
		settings.Set("HasShownYouAreGood", value);
	}

	public bool GetIsPayingUser()
	{
		settings.Set("IsPayingUser", false, false);
		return settings.GetBool("IsPayingUser");
	}

	public void SetIsPayingUser(bool pIsPayingUser)
	{
		settings.Set("IsPayingUser", pIsPayingUser);
	}

	public int GetLevelsCompleted()
	{
		settings.Set("LevelsCompleted", 0, false);
		return settings.GetInt("LevelsCompleted");
	}

	public void IncreaseLevelsCompleted()
	{
		int levelsCompleted = GetLevelsCompleted();
		settings.Set("LevelsCompleted", levelsCompleted + 1);
	}

	public static float GetDifficultValue(float[] pValue)
	{
		if (I.expert)
		{
			return pValue[0] + (pValue[1] - pValue[0]) * 0.75f;
		}
		return pValue[0];
	}

	public LevelPack GetPackFromLevelNum(int level)
	{
		foreach (int value in Enum.GetValues(typeof(LevelPack)))
		{
			if (level >= GetPackMinLevel((LevelPack)value) && level <= GetPackMaxLevel((LevelPack)value))
			{
				return (LevelPack)value;
			}
		}
		return LevelPack.Original;
	}

	public int GetMaxLevel()
	{
		int num = 1;
		foreach (int value in Enum.GetValues(typeof(LevelPack)))
		{
			num = Math.Max(num, GetPackMaxLevel((LevelPack)value));
		}
		return num;
	}

	public int GetPackMinLevel(LevelPack pack)
	{
		if (pack == LevelPack.Original)
		{
			return 1;
		}
		return 21;
	}

	public int GetPackMaxLevel(LevelPack pack)
	{
		if (pack == LevelPack.Original)
		{
			return 20;
		}
		return 32;
	}

	public string GetSceneName(int levelToLoad)
	{
		LevelPack packFromLevelNum = GetPackFromLevelNum(levelToLoad);
		LevelPack levelPack = packFromLevelNum;
		if (levelPack == LevelPack.Extended)
		{
			return "level2b-" + levelToLoad;
		}
		return "level2-" + levelToLoad;
	}

	public LevelPack GetCurrentPack()
	{
		LevelPack levelPack = LevelPack.Original;
		settings.Set("CurrentLevelPack", levelPack.ToString(), false);
		string value = settings.GetString("CurrentLevelPack");
		if (Enum.IsDefined(typeof(LevelPack), value))
		{
			levelPack = (LevelPack)(int)Enum.Parse(typeof(LevelPack), value, true);
		}
		return levelPack;
	}

	public void SetCurrentPack(LevelPack pack)
	{
		settings.Set("CurrentLevelPack", pack.ToString());
	}

	public int GetLevelCount(LevelPack pack)
	{
		return GetPackMaxLevel(pack) - GetPackMinLevel(pack) + 1;
	}

	public int GetRelativeLevel(int level, LevelPack pack)
	{
		return level - (GetPackMinLevel(pack) - 1);
	}
}
