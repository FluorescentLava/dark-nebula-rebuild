using System;
using UnityEngine;

public class Constants
{
	public const int LayerDefault = 0;

	public const int LayerSideWalls = 8;

	public const int LayerVessel = 9;

	public const int LayerBullets = 10;

	public const float MusicFadeTime = 1.5f;

	public const float MusicVolume = 0.6f;

	public const float ScreenFadeInTime = 0.5f;

	public const float ScreenFadeOutTime = 0.5f;

	public const int VirtualScreenWidth = 320;

	public const float PI = (float)Math.PI;

	public const float TargetFPS = 30f;

	public const int LastLevelChapterOne = 11;

	public const int LastLevelChapterTwo = 23;

	public const int TotalLevels = 23;

	public const string TagPlayer = "Player";

	public const string TagAbyss = "Abyss";

	public const string TagBullet = "Bullet";

	public const string TagPuck = "Puck";

	public const string TagTurret = "Turret";

	public const string CollisionTypeAbyss = "abysscollision";

	public const string CollisionTypeBox = "boxcollision";

	public const string CollisionTypeCircle = "circlecollision";

	public const string CollisionTypeRedEnergy = "redenergysourcecollision";

	public const string CollisionTypeGreenEnergy = "greenenergysourcecollision";

	public const string CollisionTypeBlueEnergy = "blueenergysourcecollision";

	public const string CollisionTypeDeath = "deathcollision";

	public const int MaxOfflineLeaderboardEntries = 1;

	public const int MaxOnlineLeaderboardEntries = 50;

	public const int PalringoScoreMultiplier = 100000;

	public const string PalringoLeaderboardLevel = "level";

	public const string PalringoLeaderboardExpert = "hard";

	public const string PalringoLeaderboardNormal = "easy";

	public const string PalringoLeaderboardTotal = "total";


	public const string SceneMainMenu = "MainMenu";

	public const string SceneBoosterShop = "BoosterShop";

	public const string SceneAvatarShop = "AvatarShop";

	public const string SceneLevelSelect = "LevelSelect";

	public const string SceneLoadlevel = "LoadLevel";

	public const string SceneBoosterSelect = "BoosterSelection";

	public const string ScenePostComplete = "PostComplete";

	public const string SceneLeaderboard = "Leaderboard";

	public const string SceneLevelComplete = "LevelComplete";

	public const string SceneGateLock = "GateLock";

	public const string SceneEndOfGame = "EndOfGame";

	public const string SceneEndOfChapterOne = "EndOfChapterOne";

	public const string ChartboostMainMenu = "MainMenu";

	public const string ChartboostLevelSelect = "LevelSelect";

	public const string ChartboostBoosterSelect = "BoosterSelect";

	public const string ChartboostBoosterShop = "BoosterShop";


	public const string ChartboostAvatarSelect = "AvatarSelect";

	public const string ChartboostLoadLevel = "LoadLevel";

	public const string ChartboostInGameMenu = "InGameMenu";

	public const string ChartboostEndOfChapterOne = "EndOfChapterOne";

	public const string ChartboostEndOfGame = "EndOfGame";

	public const string ChartboostLeaderboard = "Leaderboard";

	public const string ChartboostLevelComplete = "LevelComplete";

	public const string ChartboostPostComplete = "PostComplete";

	public const string ChartboostGateLock = "GateLock";

	public static int OriginalScreenHeight;

	public static int VirtualScreenHeight;

	public static int VirtualScreenHeightHalf;

	static Constants()
	{
		OriginalScreenHeight = 480;
		VirtualScreenHeight = 480;
		VirtualScreenHeightHalf = 240;
		VirtualScreenHeight = (int)((float)Screen.height / (float)Screen.width * 320f);
		VirtualScreenHeightHalf = VirtualScreenHeight / 2;
	}
}
