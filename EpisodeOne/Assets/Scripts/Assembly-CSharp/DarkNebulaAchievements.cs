using FLD;
using UnityEngine;

public class DarkNebulaAchievements
{
	private struct GoldStarAchievement
	{
		public int StarsRequired;

		public int IconIndex;

		public string IconId
		{
			get
			{
				return "achievement-icon" + IconIndex;
			}
		}

		public GoldStarAchievement(int pStars, int pIcon)
		{
			StarsRequired = pStars;
			IconIndex = pIcon;
		}
	}

	private const int GoldStarAchievementBaseId = 11;

	private const string SyncedGoldStarCountStoreKey = "syncedGoldStarCount";

	private static readonly GoldStarAchievement[] GoldStarAchievements = new GoldStarAchievement[14]
	{
		new GoldStarAchievement(1, 1),
		new GoldStarAchievement(4, 1),
		new GoldStarAchievement(7, 1),
		new GoldStarAchievement(10, 1),
		new GoldStarAchievement(13, 2),
		new GoldStarAchievement(16, 2),
		new GoldStarAchievement(19, 2),
		new GoldStarAchievement(22, 2),
		new GoldStarAchievement(25, 2),
		new GoldStarAchievement(28, 3),
		new GoldStarAchievement(31, 3),
		new GoldStarAchievement(34, 3),
		new GoldStarAchievement(37, 3),
		new GoldStarAchievement(40, 4)
	};

	private static DarkNebulaAchievements _instance;

	private static PersistentStore Store
	{
		get
		{
			return PersistentStore.GetInstance("achievements");
		}
	}

	private int LastSyncedGoldStarCount
	{
		get
		{
			return Store.GetInt("syncedGoldStarCount");
		}
		set
		{
			Store.Set("syncedGoldStarCount", value);
		}
	}

	public static DarkNebulaAchievements Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = new DarkNebulaAchievements();
			}
			return _instance;
		}
	}

	public void EraseProgress()
	{
		LastSyncedGoldStarCount = 0;
	}

	public int AchievementCount { get { return GoldStarAchievements.Length; } }
	public int GetStarsRequired(int index) { return GoldStarAchievements[index].StarsRequired; }
	public string GetIconId(int index) { return GoldStarAchievements[index].IconId; }

	public int GetGoldStarsCount()
	{
		int num = 23;
		int num2 = 0;
		for (int i = 1; i <= num; i++)
		{
			if (UserProgress.Instance.IsLevelStarGold(i, false))
			{
				num2++;
			}
			if (UserProgress.Instance.IsLevelStarGold(i, true))
			{
				num2++;
			}
		}
		return num2;
	}

	public void SyncAchievements()
	{
		SyncGoldStarAchievements();
	}

	private void SyncGoldStarAchievements()
	{
		int num = -1;
		int goldStarsCount = GetGoldStarsCount();
		for (int i = 0; i < GoldStarAchievements.Length; i++)
		{
			if (goldStarsCount >= GoldStarAchievements[i].StarsRequired)
			{
				// Online achievement submission is disabled in the offline build.
				if (LastSyncedGoldStarCount < GoldStarAchievements[i].StarsRequired)
				{
					num = i;
				}
			}
		}
		LastSyncedGoldStarCount = goldStarsCount;
		if (num >= 0)
		{
			Debug.Log("Would show achievement window for reaching level " + num + " gold star level achievement!");
			AchievementPopup.Open(GoldStarAchievements[num].IconId, ("ACHIEVEMENT.GOLDSTARS." + num).Localize());
		}
		else
		{
			Debug.Log("No new achievements granted this time");
		}
	}
}
