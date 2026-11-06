using System;
using UnityEngine;

public class BoosterManager
{
	private static BoosterManager _instance;

	public Booster BoosterToBuy;

	public static BoosterManager Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new BoosterManager();
			}
			return _instance;
		}
	}

	public static float MagnetMaxSpeed
	{
		get
		{
			Booster booster = Resources.Load("HardCurrencyItems/BoosterMagnet") as Booster;
			return booster.MagnetMaxSpeed;
		}
	}

	public static float MagnetDistance
	{
		get
		{
			Booster booster = Resources.Load("HardCurrencyItems/BoosterMagnet") as Booster;
			return booster.MagnetDistance;
		}
	}

	public static float SpeedBonus
	{
		get
		{
			Booster booster = Resources.Load("HardCurrencyItems/BoosterSpeed") as Booster;
			return booster.SpeedBonus;
		}
	}

	public static void AwardStartupBoosters()
	{
		UnlockBooster(Booster.GetIdForBooster(Booster.BoosterType.SHIELD, 1, false));
	}

	public static void DeleteAllBoosters()
	{
		for (int i = 0; i <= 23; i++)
		{
			foreach (int value in Enum.GetValues(typeof(Booster.BoosterType)))
			{
				string idForCurrentDifficulty = Booster.GetIdForCurrentDifficulty((Booster.BoosterType)value, i);
			}
		}
	}

	public static void UnlockBooster(string pID, bool pAlsoEnable = true)
	{
		UserPurchases.Instance.GiveProduct(pID);
	}

	public static void UnlockAllBoosters()
	{
		for (int i = 1; i <= 23; i++)
		{
			foreach (int value in Enum.GetValues(typeof(Booster.BoosterType)))
			{
				string idForBooster = Booster.GetIdForBooster((Booster.BoosterType)value, i, false);
				UnlockBooster(idForBooster, false);
				string idForBooster2 = Booster.GetIdForBooster((Booster.BoosterType)value, i, true);
				UnlockBooster(idForBooster2, false);
			}
		}
	}

	public static bool Have(string pBoosterID)
	{
		return UserPurchases.Instance.HasProduct(pBoosterID);
	}

	public static bool Have(Booster pBooster)
	{
		return Have(pBooster.ID);
	}

	public static bool IsEnabled(string pBoosterID)
	{
		return Have(pBoosterID) && UserSettings.Instance.IsBoosterEnabled(pBoosterID);
	}

	public static bool IsEnabled(Booster pBooster, int pLevel, bool pExpert)
	{
		return IsEnabled(Booster.GetIdForBooster(pBooster.Type, pLevel, pExpert));
	}

	public static void EnableBooster(string pBoosterID)
	{
		UserSettings.Instance.SetBoosterEnabled(pBoosterID, true);
	}

	public static void EnableBooster(Booster pBooster, int pLevel, bool pExpert)
	{
		EnableBooster(Booster.GetIdForBooster(pBooster.Type, pLevel, pExpert));
	}

	public static void DisableBooster(string pBoosterID)
	{
		UserSettings.Instance.SetBoosterEnabled(pBoosterID, false);
	}

	public static void DisableBooster(Booster pBooster, int pLevel, bool pExpert)
	{
		DisableBooster(Booster.GetIdForBooster(pBooster.Type, pLevel, pExpert));
	}
}
