using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using FLD.JSON;
using FLD;
using Palringo;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
	public GameObject GfxStar;

	public LeaderboardEntryVO Entry;

	[SerializeField]
	private tk2dSprite _background;

	[SerializeField]
	private DynamicFont _timeText;

	[SerializeField]
	private SysFontText _nameText;

	[SerializeField]
	private DynamicFont _positionText;

	[SerializeField]
	private DynamicFont _userIDText;

	[SerializeField]
	private Transform _profileImage;

	[SerializeField]
	private Material _defaultMaterial;

	[SerializeField]
	private Color _currentPlayerColor;

	private bool _haveProfileImage;
	private DynamicFont _localNameText;
	private DynamicFont _dateText;
	private tk2dTextMesh _bitmapTime;
	private tk2dTextMesh _bitmapDate;
	private static tk2dFontData leaderboardFont;
	private static tk2dSpriteCollectionData leaderboardStars;

	private tk2dTextMesh CreateBitmapLabel(string name, Vector3 position, float scale, TextAnchor anchor)
	{
		if (leaderboardFont == null)
			leaderboardFont = Resources.Load<GameObject>("LeaderboardText/FontInputdata").GetComponent<tk2dFontData>();
		var root = new GameObject(name);
		root.layer = gameObject.layer;
		root.transform.SetParent(transform, false);
		root.transform.localPosition = position;
		root.transform.localScale = Vector3.one * scale;
		var label = root.AddComponent<tk2dTextMesh>();
		label.font = leaderboardFont;
		label.anchor = anchor;
		label.maxChars = 64;
		// Awake runs before a runtime-created label has a font. Build once after
		// assigning it so subsequent text changes use an initialized mesh/queue.
		label.ForceBuild();
		return label;
	}

	public void SetEntry(LeaderboardEntryVO pEntry, int pListposition)
	{
		// The original SysFont renderer depends on a removed Android Java plugin.
		// Keep the portrait, with the same name/time/date hierarchy as Episode Two.
		_nameText.gameObject.SetActive(false);
		if (_localNameText == null)
		{
			_localNameText = UnityEngine.Object.Instantiate(_timeText, _timeText.transform.parent);
			_localNameText.name = "Local score name";
			_localNameText.SetOriginalPos(new Vector3(-110f, 5.3812775f, -1f));
			_localNameText.Anchor = TextAnchor.MiddleLeft;
			_localNameText.Alignment = TextAlignment.Left;
			_localNameText.Localize = false;
			_localNameText.Wrapping = DynamicFont.WrapType.SHRINK;
			_localNameText.SelectedEntry = 6;
			_localNameText.WrapWidth = 148f;
			_dateText = UnityEngine.Object.Instantiate(_timeText, _timeText.transform.parent);
			_dateText.name = "Best time label";
			_dateText.SetOriginalPos(new Vector3(140.49313f, -8.538132f, -1f));
			_dateText.SelectedEntry = 8;
			_dateText.FontColor = 11;
			_dateText.transform.localScale = Vector3.one * .9f;
			_dateText.Localize = false;
			_dateText.Wrapping = DynamicFont.WrapType.SHRINK;
			_dateText.WrapWidth = 138f;
			// Reuse Episode Two's actual bitmap face, scale and authored anchors.
			// Dynamic nicknames keep full Unicode support alongside the portrait.
			_timeText.name = "Legacy time";
			_timeText.gameObject.SetActive(false);
			_bitmapTime = CreateBitmapLabel("Time", new Vector3(58.61621f, 5.5825195f, 0f), .402484f, TextAnchor.MiddleLeft);
			_bitmapDate = CreateBitmapLabel("Score date", new Vector3(140.49313f, -8.373947f, 0f), .32836974f, TextAnchor.MiddleRight);
			_bitmapDate.color = new Color(.46268654f, .46268654f, .46268654f, 1f);
			_bitmapDate.color2 = new Color(.42537314f, .42537314f, .42537314f, 1f);
			_bitmapDate.useGradient = true;
			_bitmapDate.inlineStyling = true;
			_bitmapDate.kerning = true;
			if (leaderboardStars == null)
				leaderboardStars = Resources.Load<GameObject>("LeaderboardText/LeaderboardStars").GetComponent<tk2dSpriteCollectionData>();
			GfxStar.GetComponent<tk2dSprite>().SetSprite(leaderboardStars, "Skillstars/2");
			Vector3 portraitPosition = _profileImage.localPosition;
			portraitPosition.x = -130f;
			_profileImage.localPosition = portraitPosition;
			_localNameText.FontRefreshed += AlignLocalRow;
			_dateText.FontRefreshed += AlignLocalRow;
		}
		Entry = pEntry;
		base.transform.localPosition = new Vector3(0f, -18f + (float)pListposition * -36f, 0f);
		if (pListposition % 2 == 0)
		{
			_background.spriteId = _background.GetSpriteIdByName("LeaderboardEven");
		}
		else
		{
			_background.spriteId = _background.GetSpriteIdByName("LeaderboardOdd");
		}
		bool flag = true;
		if (pEntry.time <= 0f && string.IsNullOrEmpty(pEntry.name) && string.IsNullOrEmpty(pEntry.userID))
		{
			flag = false;
		}
		_userIDText.gameObject.SetActive(flag);
		_localNameText.gameObject.SetActive(flag);
		_dateText.gameObject.SetActive(flag && pEntry.date == default(DateTime));
		_bitmapDate.gameObject.SetActive(flag && pEntry.date != default(DateTime));
		_profileImage.gameObject.SetActive(flag);
		_positionText.gameObject.SetActive(flag);
		_bitmapTime.gameObject.SetActive(flag);
		GfxStar.GetComponent<Renderer>().enabled = flag;
		if (flag)
		{
			if (string.IsNullOrEmpty(pEntry.name))
			{
				pEntry.name = "PLAYER".Localize();
			}
			if (DarkNebulaPalringo.IsAuthorized)
			{
				_userIDText.gameObject.SetActive(true);
				_positionText.Text = pEntry.position + ".";
			}
			else
			{
				_positionText.Text = string.Empty;
				_userIDText.gameObject.SetActive(false);
			}
			_localNameText.SetLiteralText((DarkNebulaPalringo.IsAuthorized ? pEntry.position : pListposition + 1) + ". " + pEntry.name);
			_bitmapTime.text = LevelCompleteSection.ToTimeString(pEntry.time);
			_bitmapTime.Commit();
			_bitmapDate.text = "^5" + pEntry.date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();
			_bitmapDate.Commit();
			_dateText.SetLiteralText("BEST TIME".Localize());
			if (pEntry.star >= 0 && pEntry.expert)
			{
				GfxStar.GetComponentInChildren<tk2dSprite>().spriteId = GfxStar.GetComponentInChildren<tk2dSprite>().GetSpriteIdByName("SkillstarsExpert/" + pEntry.star);
			}
			else if (pEntry.star >= 0)
			{
				GfxStar.GetComponentInChildren<tk2dSprite>().spriteId = GfxStar.GetComponentInChildren<tk2dSprite>().GetSpriteIdByName("Skillstars/" + pEntry.star);
			}
			if (pEntry.star < 0)
			{
				GfxStar.GetComponent<Renderer>().enabled = false;
			}
			if (pEntry.userID != null)
			{
				_userIDText.Text = pEntry.userID;
			}
			if (pEntry.profileImage != null)
			{
				SetProfileImage(pEntry.profileImage);
			}
			else
			{
				_haveProfileImage = false;
			}
			AlignLocalRow();
		}
	}

	private void AlignLocalRow()
	{
		if (_dateText == null) return;
		foreach (DynamicFont label in new[] { _localNameText, _dateText })
		{
			Vector3 position = label.transform.position;
			float y = label == _dateText ? -8.538132f : 5.3812775f;
			position.y += transform.TransformPoint(new Vector3(0f, y, 0f)).y - label.InkCenterWorldY;
			label.transform.position = position;
		}
	}

	public void SetCurrentUserIndicator()
	{
		_background.color = _currentPlayerColor;
	}

	private void Update()
	{
		if (!_haveProfileImage && Entry.palringoEntry != null && !(Entry.palringoEntry.User.Avatar == null))
		{
			SetProfileImage(Entry.palringoEntry.User.Avatar);
		}
	}

	private void SetProfileImage(Texture2D pProfileImage)
	{
		if (pProfileImage != null)
		{
			_haveProfileImage = true;
			_profileImage.GetComponent<Renderer>().material = new Material(_profileImage.GetComponent<Renderer>().material);
			_profileImage.GetComponent<Renderer>().material.mainTexture = pProfileImage;
		}
		else
		{
			_profileImage.GetComponent<Renderer>().material = _defaultMaterial;
		}
	}

	public void Reset()
	{
		_haveProfileImage = false;
		_profileImage.GetComponent<Renderer>().material = _defaultMaterial;
		if (_background != null)
		{
			_background.color = Color.white;
		}
	}

	public static LeaderboardEntryVO Deserialize(Dictionary<string, object> data)
	{
		LeaderboardEntryVO entry;
		LocalLeaderboard.TryDeserialize(data, out entry);
		return entry;
	}

	public static LeaderboardEntryVO GetEntryFromJValue(JValue pJValueEntry)
	{
		return new LeaderboardEntryVO
		{
			date = DateTime.Parse(pJValueEntry["date"].StringValue),
			name = pJValueEntry["name"].StringValue,
			time = float.Parse(pJValueEntry["time"].StringValue),
			star = int.Parse(pJValueEntry["star"].StringValue),
			expert = bool.Parse(pJValueEntry["expert"].StringValue)
		};
	}

	public static JValue GetJValueFromEntry(LeaderboardEntryVO pEntry)
	{
		JValue jValue = new JValue();
		jValue.Add("date", pEntry.date.ToString());
		jValue.Add("name", pEntry.name);
		jValue.Add("time", pEntry.time.ToString());
		jValue.Add("star", pEntry.star.ToString());
		jValue.Add("expert", pEntry.expert.ToString());
		return jValue;
	}

	public static Dictionary<string, object> Serialize(LeaderboardEntryVO entry)
	{
		return LocalLeaderboard.Serialize(entry);
	}

	public static int GetLevelFromFromLeaderboardEntry(Palringo.LeaderboardEntry pEntry)
	{
		if (pEntry.LeaderboardId.Contains("total"))
		{
			return 0;
		}
		string[] array = pEntry.LeaderboardId.Split('/');
		string value = Regex.Match(array[0], "\\d+").Value;
		return int.Parse(value);
	}

	private static bool IsExpertLeaderboardEntry(Palringo.LeaderboardEntry pEntry)
	{
		return pEntry.LeaderboardId.Contains("hard");
	}

	public static LeaderboardEntryVO GetTotalTimeEntryForLocalUser(bool pExpert)
	{
		return new LeaderboardEntryVO
		{
			name = UserSettings.Instance.GetUserName(),
			time = UserProgress.Instance.GetTotalTime(pExpert),
			star = -1,
			expert = pExpert
		};
	}

	public static LeaderboardEntryVO GetEntryFromGlobalEntry(Palringo.LeaderboardEntry pEntry)
	{
		bool flag = IsExpertLeaderboardEntry(pEntry);
		LeaderboardEntryVO result = new LeaderboardEntryVO
		{
			level = GetLevelFromFromLeaderboardEntry(pEntry),
			name = pEntry.User.Nickname,
			time = (float)pEntry.Score / 100000f,
			expert = flag,
			profileImage = pEntry.User.Avatar,
			position = pEntry.Rank,
			userID = pEntry.User.UserId,
			palringoEntry = pEntry
		};
		if (result.level == 0)
		{
			result.star = -1;
		}
		else
		{
			result.star = (int)LevelCompleteScreen.GetStarValue(result.time, result.level, flag);
		}
		return result;
	}

	private static void PrintEntry(LeaderboardEntryVO entry)
	{
		Debug.Log("Entry: \n Name: " + entry.name + "\ntime" + entry.time + "\nstar" + entry.star + "\nexpert" + entry.expert + "\nuserid" + entry.userID);
	}
}
