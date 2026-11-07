using Random = UnityEngine.Random;
using System;
using FLD;
using UnityEngine;

public class RandomHint : MonoBehaviour
{
	[Serializable]
	public struct Entry
	{
		public string SpriteId;

		public string StringId;
	}

	[SerializeField]
	private tk2dSprite _sprite;

	[SerializeField]
	private DynamicFont _text;

	[SerializeField]
	private Entry[] _entries;

	private void Start()
	{
		ShowRandomEntry();
	}

	public void ShowRandomEntry()
	{
		if (_entries.Length > 0)
		{
			ShowEntry(_entries[Random.Range(0, _entries.Length)]);
		}
	}

	private void ShowEntry(Entry pEntry)
	{
		_sprite.spriteId = _sprite.GetSpriteIdByName(pEntry.SpriteId);
		string message=pEntry.StringId.Localize();
        bool leaderboard = message.ToUpperInvariant().Contains("PALRINGO") || pEntry.StringId == "VIEW YOUR BEST TIMES IN LEADERBOARDS.";
        _sprite.gameObject.SetActive(!leaderboard);
        if(leaderboard)
        {
            _text.Anchor = TextAnchor.MiddleCenter;
            _text.Alignment = TextAlignment.Center;
            _text.WrapWidth = 280f;
            _text.Wrapping = GamePreferences.Chinese ? DynamicFont.WrapType.NONE : DynamicFont.WrapType.WORD;
            _text.SetOriginalPos(new Vector3(-transform.localPosition.x, 2f, _text.transform.localPosition.z));
            _text.Text="VIEW YOUR BEST TIMES IN LEADERBOARDS.";
        }
        else
        {
            if (GamePreferences.Chinese && pEntry.StringId == "LOADING.HINT.CALIBRATE")
            {
                _text.Anchor = TextAnchor.MiddleCenter;
                _text.Alignment = TextAlignment.Center;
                _text.Wrapping = DynamicFont.WrapType.NONE;
                _text.SetOriginalPos(new Vector3(22f, 2f, _text.transform.localPosition.z));
            }
            _text.Text=message;
        }
	}
}
