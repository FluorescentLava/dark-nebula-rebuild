using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

// Interface translations keep immutable UVs. User names use a separately imported
// font asset so adding a name cannot repack any interface or DynamicFont atlas.
public static class CjkFontAtlas
{
    const int LiteralRasterSize = 64;
    [Serializable] public sealed class Glyph
    {
        public int code;
        public float advance, x0, y0, x1, y1, u0, v0, u1, v1;
        // Unity can rotate dynamic glyphs when packing; retain all four UV corners.
        public Vector2 BottomLeft { get; internal set; }
        public Vector2 TopLeft { get; internal set; }
        public Vector2 TopRight { get; internal set; }
        public Vector2 BottomRight { get; internal set; }
    }
    [Serializable] sealed class Catalog { public float capTop, capBottom; public Glyph[] glyphs; public string[] styles; }
    static Catalog catalog;
    static Dictionary<int, Glyph> glyphs;
    static readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static readonly Dictionary<char, Glyph> literalGlyphs = new Dictionary<char, Glyph>();
    static Font literalFont;
    static string literalCharacters;
    static string literalText;
    static bool literalDirty = true;
    static int literalGeneration;
    public static float CapTop { get { Load(); return catalog.capTop; } }
    public static float CapBottom { get { Load(); return catalog.capBottom; } }
    public static bool LiteralReady { get { return literalFont != null && literalGlyphs.ContainsKey('?'); } }
    public static int LiteralGeneration { get { return literalGeneration; } }
    public static bool HasGlyph(char c) { Load(); return glyphs.ContainsKey(c); }
    public static Glyph GetGlyph(char c) { Load(); Glyph glyph; return glyphs.TryGetValue(c, out glyph) ? glyph : glyphs['□']; }
    static void Load()
    {
        if (catalog != null) return;
        catalog = JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("Fonts/ChineseAtlas").text);
        glyphs = new Dictionary<int, Glyph>();
        foreach (Glyph glyph in catalog.glyphs)
        {
            glyph.BottomLeft = new Vector2(glyph.u0, glyph.v0);
            glyph.TopLeft = new Vector2(glyph.u0, glyph.v1);
            glyph.TopRight = new Vector2(glyph.u1, glyph.v1);
            glyph.BottomRight = new Vector2(glyph.u1, glyph.v0);
            glyphs[glyph.code] = glyph;
        }
    }
    public static bool PrepareLiteral(string text)
    {
        Load();
        if (!literalDirty && text == literalText) return LiteralReady;
        if (literalFont == null)
        {
            // Unity does not support cloning a dynamic Font. This dedicated TTF
            // has identical outlines and its own internal font family/atlas.
            literalFont = Resources.Load<Font>("Fonts/DarkNebulaNickname-Regular");
            if (literalFont == null) return false;
            Font.textureRebuilt += LiteralTextureRebuilt;
        }
        // A rebuild can discard older requests. Request all visible names together.
        var requested = new SortedSet<char>();
        requested.Add('?');
        foreach (char c in text)
            if (c != '\n' && c != '\r' && literalFont.HasCharacter(c)) requested.Add(c);
        var characters = new StringBuilder(requested.Count);
        foreach (char c in requested) characters.Append(c);
        string current = characters.ToString();
        if (!literalDirty && current == literalCharacters) { literalText = text; return LiteralReady; }
        literalFont.RequestCharactersInTexture(current, LiteralRasterSize, FontStyle.Normal);
        literalGlyphs.Clear();
        foreach (char c in requested)
        {
            CharacterInfo info;
            if (!literalFont.GetCharacterInfo(c, out info, LiteralRasterSize, FontStyle.Normal)) continue;
            Glyph baked;
            float advance = info.advance;
            // Keep accepted ASCII advances and the fullwidth TTF's exact CJK cells.
            if (glyphs.TryGetValue(c, out baked)) advance = baked.advance;
            else if ((c >= 0x2e80 && c <= 0x9fff) || (c >= 0xff01 && c <= 0xff60)) advance = glyphs['中'].advance;
            literalGlyphs[c] = new Glyph
            {
                code = c, advance = advance,
                x0 = info.minX, y0 = info.minY, x1 = info.maxX, y1 = info.maxY,
                BottomLeft = info.uvBottomLeft, TopLeft = info.uvTopLeft,
                TopRight = info.uvTopRight, BottomRight = info.uvBottomRight
            };
        }
        literalCharacters = current;
        literalText = text;
        literalDirty = false;
        literalGeneration++;
        UpdateLiteralTextures();
        return LiteralReady;
    }
    public static Glyph GetLiteralGlyph(char c)
    {
        Glyph glyph;
        if (literalGlyphs.TryGetValue(c, out glyph)) return glyph;
        if (literalGlyphs.TryGetValue('?', out glyph)) return glyph;
        return GetGlyph(c);
    }
    static void LiteralTextureRebuilt(Font changed)
    {
        if (changed != literalFont) return;
        literalDirty = true;
        literalGeneration++;
        UpdateLiteralTextures();
    }
    static void UpdateLiteralTextures()
    {
        if (literalFont == null) return;
        Texture texture = literalFont.material.mainTexture;
        if (texture != null) texture.filterMode = FilterMode.Bilinear;
        foreach (KeyValuePair<string, Material> pair in materials)
            if (pair.Key.StartsWith("literal/", StringComparison.Ordinal) && pair.Value != null)
                pair.Value.mainTexture = texture;
    }
    public static Material MaterialFor(string style, Texture2D gradient = null, int gradientIndex = 0, int gradientCount = 1, bool literal = false)
    {
        Load(); Material material;
        string key = (literal ? "literal/" : "static/") + style + (gradient != null ? "/" + gradient.GetInstanceID() + "/" + gradientIndex + "/" + gradientCount : "");
        if (materials.TryGetValue(key, out material) && material != null) return material;
        Texture2D ramp = Resources.Load<Texture2D>("Fonts/ChineseTintRamps");
        material = new Material(Resources.Load<Shader>("ChineseText")) { name = (literal ? "Chinese player name / " : "Chinese text / ") + style };
        material.mainTexture = literal && literalFont != null ? literalFont.material.mainTexture : Resources.Load<Texture2D>("Fonts/ChineseAtlas");
        int index = Array.IndexOf(catalog.styles, style);
        material.SetTexture("_TintRamp", index >= 0 ? ramp : Texture2D.whiteTexture);
        material.SetFloat("_StyleRow", index >= 0 ? 1f - (index + .5f) / ramp.height : .5f);
        material.SetTexture("_OriginalGradient", gradient != null ? gradient : Texture2D.whiteTexture);
        material.SetFloat("_UseGradient", gradient != null ? 1f : 0f);
        material.SetFloat("_GradientCount", Mathf.Max(1, gradientCount));
        material.SetFloat("_GradientIndex", gradientIndex);
        material.SetFloat("_MessageStyle", style == "FontSmall_Tonaddata" ? 1f : 0f);
        materials[key] = material; return material;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        Font.textureRebuilt -= LiteralTextureRebuilt;
        foreach (Material material in materials.Values) if (material != null) UnityEngine.Object.Destroy(material);
        // Resources owns the imported font; only generated materials are destroyed.
        materials.Clear(); literalGlyphs.Clear(); catalog = null; glyphs = null;
        literalFont = null; literalCharacters = null; literalText = null; literalDirty = true; literalGeneration++;
    }
}
