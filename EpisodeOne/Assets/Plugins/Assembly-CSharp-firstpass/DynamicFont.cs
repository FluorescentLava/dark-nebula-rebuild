using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using FLD;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public class DynamicFont : MonoBehaviour
{
	public enum WrapType
	{
		NONE = 0,
		WORD = 1,
		SHRINK = 2
	}

	private TextMesh _textMesh;

    private static Font leaderboardChineseFont;
    private static Font leaderboardBoldFont;
    private static readonly Dictionary<Font, float> ascentFractions = new Dictionary<Font, float>();

    private static Font ChineseFontFor(DynamicFontSettings.Entry entry)
    {
        // The leaderboard's Arial/ArialSmall family uses a normal sans face;
        // BankGothic menu labels keep their accepted geometric Chinese face.
        if (entry.Name.StartsWith("Leaderboard"))
            return leaderboardBoldFont ?? (leaderboardBoldFont = Resources.Load<Font>("Fonts/NotoSansSC-Bold"));
        if (entry.Name == "Arial" || entry.Name == "ArialSmall")
            return leaderboardChineseFont ?? (leaderboardChineseFont = Resources.Load<Font>("Fonts/NotoSansSC-Regular"));
        return GameLocalization.ChineseFont;
    }

	public WrapType Wrapping;

	public Action FontRefreshed;

	public float WrapWidth;

	public int SelectedEntry;

	private float _scale = 1f;

	public int FontColor;

	public bool Localize = true;

	public bool OverrideRightToLeft;

	public TextAnchor Anchor = TextAnchor.MiddleCenter;

	public TextAlignment Alignment = TextAlignment.Center;

	public FontStyle Style;

	private Vector3 _orgLocalPosition;

	public string OriginalText = string.Empty;

    private bool messageGradient;
    private Material messageMaterial;

    public void EnableMessageGradient()
    {
        if (!messageGradient) Font.textureRebuilt += RefreshMessageTexture;
        messageGradient = true;
        FontColor = 0;
        Refresh();
    }

    private void RefreshMessageTexture(Font font)
    {
        if (messageMaterial != null && GetTextMesh().font == font)
            messageMaterial.mainTexture = font.material.mainTexture;
    }

    private void ApplyMessageGradient()
    {
        if (!messageGradient) return;
        if (messageMaterial == null)
        {
            messageMaterial = new Material(CjkFontAtlas.MaterialFor("FontSmall_Tonaddata"));
            messageMaterial.name = "HUD message gradient and shadow";
            messageMaterial.SetFloat("_UseLocalHeight", 1f);
        }
        float low, high;
        LocalInkY(GetTextMesh(), out low, out high);
        messageMaterial.SetVector("_GlyphYBounds", new Vector4(low, high, 0f, 0f));
        messageMaterial.mainTexture = GetTextMesh().font.material.mainTexture;
        GetComponent<Renderer>().sharedMaterial = messageMaterial;
    }

    private void OnDestroy()
    {
        Font.textureRebuilt -= RefreshMessageTexture;
        if (messageMaterial != null) Destroy(messageMaterial);
    }

	private string _currentText = string.Empty;

    public bool IsLiteralText { get; private set; }
    public void SetLiteralText(string value)
    {
        IsLiteralText = true;
        _currentText = value ?? string.Empty;
        Refresh();
    }

	public string Text
	{
		get
		{
			return _currentText;
		}
		set
		{
			IsLiteralText = false;
            _currentText = Application.isPlaying ? GameLocalization.Text(value) : value;
			Refresh();
		}
	}

	public float Width
	{
		get
		{
			return GetTextMesh().GetComponent<Renderer>().bounds.size.x;
		}
	}

	public float Height
	{
		get
		{
			return GetTextMesh().GetComponent<Renderer>().bounds.size.y;
		}
	}

	public static DynamicFontSettings Settings
	{
		get
		{
			return DynamicFontSettings.Instance;
		}
	}

	public TextMesh GetTextMesh()
	{
		if (_textMesh != null)
		{
			return _textMesh;
		}
		_textMesh = GetComponent<TextMesh>();
		return _textMesh;
	}

	public void SetOriginalPos()
	{
		base.transform.localPosition = _orgLocalPosition;
	}

	public void SetOriginalPos(Vector3 position)
	{
		_orgLocalPosition = position;
		base.transform.localPosition = position;
	}

	private void OnEnable() { if (!IsLiteralText && !string.IsNullOrEmpty(_currentText)) Text = GameLocalization.Text(_currentText); }

	private void Awake()
	{
		_orgLocalPosition = base.transform.localPosition;
		if (string.IsNullOrEmpty(_currentText))
		{
			Text = ((!Localize) ? OriginalText : OriginalText.Localize());
		}
	}

	public void Refresh()
	{
		if (Settings == null || Settings.Entries.Length == 0)
		{
			return;
		}
		string pText = LocalizationManager.Instance.ConvertForRendering(TryWrap((!Application.isPlaying) ? OriginalText : _currentText));
		string text = ColorText(pText, Settings.GetColor(FontColor));
		GetTextMesh().text = text;
		GetTextMesh().alignment = Alignment;
		GetTextMesh().anchor = Anchor;
		if (Application.isPlaying)
		{
			base.transform.localPosition = _orgLocalPosition;
			if (LocalizationManager.Instance.IsRightToLeftLanguage && !OverrideRightToLeft)
			{
				if (Alignment != TextAlignment.Center)
				{
					GetTextMesh().alignment = ((Alignment == TextAlignment.Left) ? TextAlignment.Right : TextAlignment.Left);
				}
				switch (Anchor)
				{
				case TextAnchor.UpperLeft:
				case TextAnchor.UpperRight:
					GetTextMesh().anchor = ((Anchor == TextAnchor.UpperLeft) ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
					break;
				case TextAnchor.MiddleLeft:
				case TextAnchor.MiddleRight:
					GetTextMesh().anchor = ((Anchor != TextAnchor.MiddleLeft) ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight);
					break;
				case TextAnchor.LowerLeft:
				case TextAnchor.LowerRight:
					GetTextMesh().anchor = ((Anchor != TextAnchor.LowerLeft) ? TextAnchor.LowerLeft : TextAnchor.LowerRight);
					break;
				}
				switch (GetTextMesh().anchor)
				{
				case TextAnchor.UpperLeft:
				case TextAnchor.MiddleLeft:
				case TextAnchor.LowerLeft:
					base.transform.localPosition += Vector3.right * (0f - WrapWidth);
					break;
				case TextAnchor.UpperRight:
				case TextAnchor.MiddleRight:
				case TextAnchor.LowerRight:
					base.transform.localPosition += Vector3.right * WrapWidth;
					break;
				}
				base.transform.localPosition += new Vector3(0f, -4f, 0f);
			}
			DynamicFontSettings.Entry.FontOverride fontOverride = Settings.Entries[SelectedEntry].GetOverride(LocalizationManager.Instance.PrimaryLanguageId);
			if (fontOverride != null)
			{
				base.transform.localPosition += fontOverride.Offset;
			}
		}
		RefreshTextMesh(GetTextMesh(), Settings.Entries[SelectedEntry], _scale);
        AlignGlyphAnchor(GetTextMesh());
        ApplyMessageGradient();
		if (FontRefreshed != null)
		{
			FontRefreshed();
		}
	}

	private string WordWrap(string pString)
	{
		if (string.IsNullOrEmpty(pString))
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder();
		string[] array = pString.Split('\n');
		foreach (string pLine in array)
		{
			WordWrapLine(pLine, stringBuilder);
			stringBuilder.Append('\n');
		}
		return stringBuilder.ToString().Trim();
	}

	private void WordWrapLine(string pLine, StringBuilder pResult)
	{
		if (string.IsNullOrEmpty(pLine))
		{
			return;
		}
        if (GameLocalization.ContainsChinese(pLine))
        {
            WordWrapChineseLine(pLine, pResult);
            return;
        }
		float num = WrapWidth;
		string[] array = pLine.Split(' ', '\t');
		float unwrappedWidth = GetUnwrappedWidth(" ");
		bool flag = true;
		string[] array2 = array;
		foreach (string text in array2)
		{
			float unwrappedWidth2 = GetUnwrappedWidth(text);
			float num2 = unwrappedWidth2;
			if (!flag)
			{
				num2 += unwrappedWidth;
			}
			if (num2 > num)
			{
				pResult.Append('\n');
				flag = true;
				num = WrapWidth;
				num2 = unwrappedWidth2;
			}
			if (!flag)
			{
				pResult.Append(" ");
			}
			pResult.Append(text);
			num -= num2;
			flag = false;
		}
	}

    private void WordWrapChineseLine(string text, StringBuilder output)
    {
        var tokens = new List<string>();
        foreach (System.Text.RegularExpressions.Match match in Regex.Matches(text, @"[A-Za-z0-9]+|[ \t]+|."))
        {
            string token = match.Value;
            if (tokens.Count > 0 && "，。！？：；、）】》”’…".IndexOf(token[0]) >= 0)
                tokens[tokens.Count - 1] += token;
            else if (tokens.Count > 0 && "（【《“‘".IndexOf(tokens[tokens.Count - 1][tokens[tokens.Count - 1].Length - 1]) >= 0)
                tokens[tokens.Count - 1] += token;
            else tokens.Add(token);
        }
        string line = string.Empty;
        foreach (string token in tokens)
        {
            float width = GetRenderedWidth(token);
            if (line.Length > 0 && GetRenderedWidth(line + token) > WrapWidth)
            {
                output.Append('\n');
                line = string.Empty;
            }
            if (line.Length == 0 && string.IsNullOrWhiteSpace(token)) continue;
            // Very long Latin words in otherwise Chinese hints must also fit.
            if (width > WrapWidth)
            {
                foreach (char c in token)
                {
                    if (line.Length > 0 && GetRenderedWidth(line + c) > WrapWidth) { output.Append('\n'); line = string.Empty; }
                    output.Append(c); line += c;
                }
            }
            else { output.Append(token); line += token; }
        }
    }

    private float GetRenderedWidth(string value)
    {
        TextMesh mesh = GetTextMesh();
        string previous = mesh.text;
        mesh.text = value;
        float width = mesh.GetComponent<Renderer>().localBounds.size.x;
        mesh.text = previous;
        return width;
    }

    // TextMesh bounds describe a typographic line box. Buttons need the center
    // of the visible glyphs, which differs between the recovered and CJK fonts.
    public float InkCenterWorldY
    {
        get
        {
            float low, high;
            LocalInkY(GetTextMesh(), out low, out high);
            return transform.TransformPoint(new Vector3(0f, (low + high) * .5f, 0f)).y;
        }
    }

    private static void LocalInkY(TextMesh text, out float low, out float high)
    {
        Bounds bounds = text.GetComponent<Renderer>().localBounds;
        string plain = Regex.Replace(text.text, "<.*?>", string.Empty).Replace("\r", "");
        string[] lines = plain.Split('\n');
        Font font = text.font;
        float ascentFraction;
        if (!ascentFractions.TryGetValue(font, out ascentFraction))
        {
            ascentFraction = font.lineHeight > 0 ? (float)font.ascent / font.lineHeight : .8f;
            // Font.ascent/lineHeight are rounded at the import size. Use the
            // face's precise metrics at the actual raster size instead.
            if (FontEngine.LoadFontFace(font, 64) == FontEngineError.Success)
            {
                var face = FontEngine.GetFaceInfo();
                float height = face.ascentLine - face.descentLine;
                if (height > 0) ascentFraction = face.ascentLine / height;
            }
            ascentFractions[font] = ascentFraction;
        }
        float cell = bounds.size.y / Mathf.Max(1f, 1f + (lines.Length - 1) * text.lineSpacing);
        float baseline = bounds.max.y - cell * ascentFraction;
        float pixel = text.characterSize * .1f;
        low = float.PositiveInfinity; high = float.NegativeInfinity;
        font.RequestCharactersInTexture(plain, text.fontSize, text.fontStyle);
        for (int row = 0; row < lines.Length; row++)
            foreach (char c in lines[row])
            {
                CharacterInfo info;
                if (char.IsWhiteSpace(c) || !font.GetCharacterInfo(c, out info, text.fontSize, text.fontStyle) || info.maxY <= info.minY) continue;
                low = Mathf.Min(low, baseline - row * cell * text.lineSpacing + info.minY * pixel);
                high = Mathf.Max(high, baseline - row * cell * text.lineSpacing + info.maxY * pixel);
            }
        if (float.IsInfinity(low)) { low = bounds.min.y; high = bounds.max.y; }
    }

    private static void AlignGlyphAnchor(TextMesh text)
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(text.text)) return;
        float low, high;
        LocalInkY(text, out low, out high);
        int vertical = (int)text.anchor / 3;
        // TextMesh anchors its typographic line box, whose padding varies with
        // the font. All UI labels anchor their visible glyphs in the same way.
        float delta = vertical == 0 ? -high : vertical == 2 ? -low : -(low + high) * .5f;
        text.transform.localPosition += text.transform.localRotation * (Vector3.up * delta * text.transform.localScale.y);
    }

	private string TryWrap(string pText)
	{
		if (WrapWidth <= 0f)
		{
			return pText;
		}
		// Measure the incoming script with its own font before wrapping or shrinking.
        GetTextMesh().text = pText;
        RefreshTextMesh(GetTextMesh(), Settings.Entries[SelectedEntry]);
		if (Wrapping == WrapType.WORD)
		{
			_scale = 1f;
			return WordWrap(pText);
		}
		if (Wrapping == WrapType.SHRINK)
		{
			_scale = Mathf.Min(1f, WrapWidth / GetUnwrappedWidth(pText));
			return pText;
		}
		return pText;
	}

	private void OnDrawGizmosSelected()
	{
		if (!(Settings == null) && Wrapping != WrapType.NONE && WrapWidth > 0f)
		{
			TextMesh textMesh = GetTextMesh();
			Vector3 vector = base.transform.position;
			Vector3 vector2 = base.transform.position;
			switch (textMesh.anchor)
			{
			case TextAnchor.UpperLeft:
			case TextAnchor.MiddleLeft:
			case TextAnchor.LowerLeft:
				vector2 = vector + base.transform.right * WrapWidth;
				break;
			case TextAnchor.UpperCenter:
			case TextAnchor.MiddleCenter:
			case TextAnchor.LowerCenter:
				vector = base.transform.position + base.transform.right * (0f - WrapWidth / 2f);
				vector2 = base.transform.position + base.transform.right * (WrapWidth / 2f);
				break;
			case TextAnchor.UpperRight:
			case TextAnchor.MiddleRight:
			case TextAnchor.LowerRight:
				vector2 = vector + base.transform.right * (0f - WrapWidth);
				break;
			}
			float num = Settings.OrthographicSize / 10f;
			Vector3 vector3 = base.transform.up * (0f - num);
			Vector3 vector4 = base.transform.up * num;
			Gizmos.DrawLine(vector + vector4, vector + vector3);
			Gizmos.DrawLine(vector2 + vector4, vector2 + vector3);
			Gizmos.DrawLine(vector, vector2);
		}
	}

	private float GetUnwrappedWidth(string pText)
	{
		pText = Regex.Replace(pText, "<.*?>", string.Empty);
		Dictionary<char, float> dictionary = new Dictionary<char, float>();
		TextMesh textMesh = GetTextMesh();
		string text = textMesh.text;
		textMesh.text = "a";
		float x = textMesh.GetComponent<Renderer>().localBounds.size.x;
		textMesh.text = "a a";
		float value = textMesh.GetComponent<Renderer>().localBounds.size.x - 2f * x;
		dictionary.Add(' ', value);
		dictionary.Add('a', x);
		textMesh.text = text;
		char[] array = pText.ToCharArray();
		float num = 0f;
		foreach (char c in array)
		{
			if (dictionary.ContainsKey(c))
			{
				num += dictionary[c];
				continue;
			}
			textMesh.text = string.Empty + c;
			float x2 = textMesh.GetComponent<Renderer>().localBounds.size.x;
			dictionary.Add(c, x2);
			num += x2;
		}
		textMesh.text = text;
		return num;
	}

	public static string ColorText(string pText, Color32 pColor)
	{
		string text = "#" + pColor.r.ToString("X2") + pColor.g.ToString("X2") + pColor.b.ToString("X2");
		return "<color=" + text + ">" + pText + "</color>";
	}

	public static void RefreshAllFontsInScene()
	{
		RefreshAllFontsInScene(-1);
	}

	public static void RefreshAllFontsInScene(int pEntry)
	{
		DynamicFont[] array = (DynamicFont[])UnityEngine.Object.FindObjectsOfType(typeof(DynamicFont));
		DynamicFont[] array2 = array;
		foreach (DynamicFont dynamicFont in array2)
		{
			if (pEntry == -1 || dynamicFont.SelectedEntry == pEntry)
			{
				dynamicFont.Refresh();
			}
		}
	}

	public static void RefreshTextMesh(TextMesh pTextMesh, DynamicFontSettings.Entry pEntry, float pScale = 1f)
	{
		Font font = pEntry.Font;
		FilterMode filterMode = pEntry.FilterMode;
		int size = pEntry.Size;
		float lineSpacing = pEntry.LineSpacing;
		if (Application.isPlaying)
		{
			DynamicFontSettings.Entry.FontOverride fontOverride = pEntry.GetOverride(LocalizationManager.Instance.PrimaryLanguageId);
			if (fontOverride != null)
			{
				font = fontOverride.Font;
				filterMode = fontOverride.FilterMode;
				size = fontOverride.Size;
				lineSpacing = fontOverride.LineSpacing;
			}
		}
        Font referenceFont = font;
        Font chineseFont = Application.isPlaying && GameLocalization.ContainsChinese(pTextMesh.text) ? ChineseFontFor(pEntry) : null;
		if (chineseFont != null)
        {
            font = chineseFont;
            lineSpacing = 1.15f;
        }
        font.material.mainTexture.filterMode = filterMode;
		if (Application.isPlaying && pEntry.CapitalHeight > 0f)
		{
			const int rasterSize = 64;
			bool translated = font != referenceFont;
			FontStyle style = translated ? FontStyle.Normal : pEntry.Style;
			char reference = translated ? '\u4e2d' : (char)pEntry.ReferenceCharacter;
			font.RequestCharactersInTexture(reference.ToString(), rasterSize, style);
			CharacterInfo glyph;
			if (font.GetCharacterInfo(reference, out glyph, rasterSize, style) && glyph.maxY > glyph.minY)
			{
				float height = pEntry.CapitalHeight * (translated ? 1.5f : 1f);
				pTextMesh.font = font;
				pTextMesh.GetComponent<Renderer>().sharedMaterial = font.material;
				pTextMesh.fontStyle = style;
				pTextMesh.characterSize = height * pScale * 10f / (glyph.maxY - glyph.minY);
				pTextMesh.fontSize = rasterSize;
				pTextMesh.lineSpacing = lineSpacing;
				pTextMesh.richText = true;
				return;
			}
		}
		int num = ((!Application.isPlaying) ? 768 : Screen.height);
		float num2 = 100f / ((float)num / 2f);
		num2 = Mathf.Round(num2 * 1000f) / 1000f;
		int fontSize = Mathf.RoundToInt((float)size * pScale / num2);
        if (font != referenceFont)
        {
            referenceFont.RequestCharactersInTexture("H", fontSize, pEntry.Style);
            font.RequestCharactersInTexture("中", fontSize, pEntry.Style);
            CharacterInfo latin, chinese;
            if (referenceFont.GetCharacterInfo('H', out latin, fontSize, pEntry.Style) && font.GetCharacterInfo('中', out chinese, fontSize, pEntry.Style) && chinese.maxY > chinese.minY)
                fontSize = Mathf.Max(1, Mathf.RoundToInt(fontSize * (float)(latin.maxY - latin.minY) / (chinese.maxY - chinese.minY)));
            fontSize = Mathf.Max(1, Mathf.RoundToInt(fontSize * GameLocalization.ChineseTextScale));
        }
		pTextMesh.font = font;
		pTextMesh.GetComponent<Renderer>().sharedMaterial = font.material;
		pTextMesh.fontStyle = pEntry.Style;
		pTextMesh.characterSize = num2 / 10f * Settings.OrthographicSize;
		pTextMesh.fontSize = fontSize;
		pTextMesh.lineSpacing = lineSpacing;
		pTextMesh.richText = true;
	}

	public static void PreBake(string pCharacters)
	{
		if (Settings == null)
		{
			return;
		}
		GameObject gameObject = GameObject.Find("fgDynamicFont");
		if (!(gameObject != null))
		{
			gameObject = new GameObject("fgDynamicFont");
			UnityEngine.Object.DontDestroyOnLoad(gameObject);
			DynamicFontSettings.Entry[] entries = Settings.Entries;
			foreach (DynamicFontSettings.Entry entry in entries)
			{
				GameObject gameObject2 = new GameObject(entry.Name);
				gameObject2.transform.parent = gameObject.transform;
				UnityEngine.Object.DontDestroyOnLoad(gameObject2);
				TextMesh textMesh = gameObject2.AddComponent<TextMesh>();
				textMesh.font = entry.Font;
				RefreshTextMesh(textMesh, entry);
				textMesh.text = pCharacters;
			}
		}
	}
}
