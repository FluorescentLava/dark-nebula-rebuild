using System;
using UnityEngine;

public class DynamicFontSettings : ScriptableObject
{
	[Serializable]
	public class Entry
	{
		[Serializable]
		public class FontOverride
		{
			public string[] Languages;

			public Font Font;

			public FontStyle Style;

			public int Size;

			public float LineSpacing;

			public FilterMode FilterMode;

			public Vector3 Offset = Vector3.zero;
		}

		public string Name;

		public Font Font;

		public FontStyle Style;

		public int Size;

		// Optional authored glyph height, independent of raster resolution.
		public float CapitalHeight;
		public int ReferenceCharacter = 72;

		public float LineSpacing;

		public FilterMode FilterMode;

		public FontOverride[] Overrides;

		public FontOverride GetOverride(string pLanguageCode)
		{
			if (Overrides == null || Overrides.Length == 0)
			{
				return null;
			}
			FontOverride[] overrides = Overrides;
			foreach (FontOverride fontOverride in overrides)
			{
				if (fontOverride.Languages == null || fontOverride.Languages.Length == 0)
				{
					continue;
				}
				string[] languages = fontOverride.Languages;
				foreach (string text in languages)
				{
					if (pLanguageCode == text)
					{
						return fontOverride;
					}
				}
			}
			return null;
		}
	}

	private static DynamicFontSettings _instance;

	public float OrthographicSize = 1f;

	public Color[] Colors;

	public Entry[] Entries;

	public static DynamicFontSettings Instance
	{
		get
		{
			if (_instance != null)
			{
				return _instance;
			}
			_instance = (DynamicFontSettings)Resources.Load("uFuga2/DynamicFontSettings", typeof(DynamicFontSettings));
			return _instance;
		}
	}

	public Color GetColor(int pColorNum)
	{
		if (Colors.Length == 0)
		{
			return Color.white;
		}
		pColorNum = Mathf.Clamp(pColorNum, 0, Colors.Length - 1);
		return Colors[pColorNum];
	}
}
