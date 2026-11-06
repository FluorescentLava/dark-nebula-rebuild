using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class LeaderboardEntry : MonoBehaviour
{
	public GameObject GfxBackground;

	public GameObject GfxStar;

	public GameObject GfxName;

	public GameObject GfxTime;

	public GameObject GfxDate;

	public GameObject GfxProfileImage;

	private const float NameWidth = 148f;
	private const float NameCenterY = 5.3812775f;
	private Vector3 _nameScale;
	private bool _nameLayoutInitialized;

	private void SetName(string caption)
	{
		Transform nameTransform = GfxName.transform;
		if (!_nameLayoutInitialized)
		{
			_nameScale = nameTransform.localScale;
			_nameLayoutInitialized = true;
		}
		// A row is reused when switching levels and difficulties. Fit from the
		// authored size each time so a short name restores its normal size.
		nameTransform.localScale = _nameScale;
		tk2dTextMesh label = GfxName.GetComponent<tk2dTextMesh>();
		if (label.maxChars < caption.Length) label.maxChars = caption.Length;
		GfxName.GetComponent<SimpleText>().SetLiteralText(caption);
		Mesh mesh = label.GetComponent<MeshFilter>().sharedMesh;
		// Bitmap commits can retain the preceding name's bounds. Read the mesh
		// after Commit, including the Unicode replacement mesh when one is used.
		mesh.RecalculateBounds();
		Bounds bounds = mesh.bounds;
		float width = bounds.size.x * Mathf.Abs(_nameScale.x);
		float fit = width > NameWidth ? NameWidth / width : 1f;
		nameTransform.localScale = _nameScale * fit;
		Vector3 position = nameTransform.localPosition;
		position.y = NameCenterY - bounds.center.y * nameTransform.localScale.y;
		nameTransform.localPosition = position;
	}

	public void SetEntry(LeaderboardEntryVO entry, int position)
	{
		base.transform.localPosition = new Vector3(0f, -18f + (float)position * -36f, 0f);
		if (position % 2 == 0)
		{
			GfxBackground.GetComponentInChildren<tk2dSprite>().spriteId = GfxBackground.GetComponentInChildren<tk2dSprite>().GetSpriteIdByName("UI_LeaderboardEven");
		}
		else
		{
			GfxBackground.GetComponentInChildren<tk2dSprite>().spriteId = GfxBackground.GetComponentInChildren<tk2dSprite>().GetSpriteIdByName("UI_LeaderboardOdd");
		}
		bool flag = !string.IsNullOrEmpty(entry.name);
		GfxName.GetComponent<Renderer>().enabled = flag;
		GfxTime.GetComponent<Renderer>().enabled = flag;
		GfxDate.GetComponent<Renderer>().enabled = flag;
		GfxStar.GetComponent<Renderer>().enabled = flag && entry.star >= 0;
		GfxProfileImage.SetActive(flag);
		if (flag)
		{
			SetName(string.Format("{0}. {1}", position + 1, entry.name));
			GfxTime.GetComponentInChildren<SimpleText>().SetText(LevelCompleteTimeSection.ToTimeString(entry.time));
			GfxDate.GetComponentInChildren<SimpleText>().SetText("^5" + (entry.date == default(System.DateTime) ? "BEST TIME" : entry.date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()));
			if (entry.star >= 0 && entry.expert)
			{
				GfxStar.GetComponentInChildren<tk2dSprite>().spriteId = GfxStar.GetComponentInChildren<tk2dSprite>().GetSpriteIdByName("SkillstarsExpert/" + entry.star);
			}
			else if (entry.star >= 0)
			{
				GfxStar.GetComponentInChildren<tk2dSprite>().spriteId = GfxStar.GetComponentInChildren<tk2dSprite>().GetSpriteIdByName("Skillstars/" + entry.star);
			}
		}
	}

	public static LeaderboardEntryVO Deserialize(Dictionary<string, object> data)
	{
		LeaderboardEntryVO entry;
		LocalLeaderboard.TryDeserialize(data, out entry);
		return entry;
	}

	public static Dictionary<string, object> Serialize(LeaderboardEntryVO entry)
	{
		return LocalLeaderboard.Serialize(entry);
	}
}
