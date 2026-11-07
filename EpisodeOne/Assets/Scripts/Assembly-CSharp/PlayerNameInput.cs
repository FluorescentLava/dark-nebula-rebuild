using UnityEngine;

// The original input-field art and button route, backed by the native keyboard.
public sealed class PlayerNameInput : MonoBehaviour
{
    const int MaximumLength = 24;
    DynamicFont label;
    SimpleButton button;
    TouchScreenKeyboard keyboard;
    string value, beforeEditing;
    bool desktopEditing;
    Vector3 originalPosition;
    tk2dSprite background;
    Renderer[] textRenderers;
    MaterialPropertyBlock textTint;
    Collider hitArea;
    float revealStarted;
    bool revealing;

    public static PlayerNameInput Create(Transform parent, SimpleButton template, tk2dSpriteCollectionData collection)
    {
        var root = new GameObject("Leaderboard name");
        root.layer = parent.gameObject.layer;
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(0f, -158f, -40f);
        var input = root.AddComponent<PlayerNameInput>();
        input.background = tk2dSprite.AddComponent(root, collection, collection.GetSpriteIdByName("InputField"));
        input.button = root.AddComponent<SimpleButton>();
        input.button.viewCamera = template.viewCamera;
        input.button.buttonUpSprite = input.button.buttonDownSprite = input.button.buttonPressedSprite = "InputField";
        input.button.buttonUpSound = template.buttonUpSound;
        var hit = root.GetComponent<BoxCollider>();
        if (hit == null) hit = root.AddComponent<BoxCollider>();
        hit.center = Vector3.zero;
        hit.size = new Vector3(197f, 26f, 2f);
        input.hitArea = hit;
        input.button.ButtonUpEvent += input.BeginEditing;
        input.label = Instantiate(template.GetComponentInChildren<DynamicFont>(), root.transform);
        input.label.name = "Player nickname";
        input.label.transform.localScale = Vector3.one;
        input.label.SetOriginalPos(new Vector3(0f, 0f, -1f));
        input.label.Anchor = TextAnchor.MiddleCenter;
        input.label.Alignment = TextAlignment.Center;
        input.label.SelectedEntry = 0;
        input.label.FontColor = 3;
        input.label.Localize = false;
        input.label.Wrapping = DynamicFont.WrapType.SHRINK;
        input.label.WrapWidth = 183f;
        input.value = UserSettings.Instance.GetUserName();
        input.label.SetLiteralText(input.value);
        var title = Instantiate(input.label, root.transform);
        title.name = "Nickname title";
        title.FontColor = 1;
        title.SelectedEntry = 4;
        title.SetOriginalPos(new Vector3(0f, 23f, -1f));
        title.Text = "LEADERBOARD NAME";
        LegacyControlTextLayout.Bind(input.label, root.GetComponent<Renderer>());
        input.textRenderers = new[] { input.label.GetComponent<Renderer>(), title.GetComponent<Renderer>() };
        input.textTint = new MaterialPropertyBlock();
        root.SetActive(false);
        return input;
    }

    public void Reveal(bool immediate = false)
    {
        gameObject.SetActive(true);
        revealing = !immediate;
        revealStarted = Time.unscaledTime;
        SetOpacity(immediate ? 1f : 0f);
    }

    void SetOpacity(float opacity)
    {
        Color color = background.color;
        color.a = opacity;
        background.color = color;
        // DynamicFont stores its text color in rich-text tags. Tint the
        // renderer so the same fade also covers those tagged glyphs.
        textTint.SetColor("_Color", new Color(1f, 1f, 1f, opacity));
        foreach (Renderer renderer in textRenderers) renderer.SetPropertyBlock(textTint);
        hitArea.enabled = opacity >= 1f;
    }

    void BeginEditing(SimpleButton source)
    {
        if (revealing || keyboard != null || desktopEditing) return;
        beforeEditing = value;
        if (TouchScreenKeyboard.isSupported)
        {
            keyboard = TouchScreenKeyboard.Open(value, TouchScreenKeyboardType.Default, false, false, false, false, "", MaximumLength);
            originalPosition = transform.parent.position;
            // Raise the result panel while the native keyboard covers its lower half.
            if (keyboard != null) transform.parent.position = originalPosition + Vector3.up * 100f;
        }
        else desktopEditing = true;
    }

    void Update()
    {
        if (revealing)
        {
            float opacity = Mathf.Clamp01((Time.unscaledTime - revealStarted) / 0.3f);
            SetOpacity(opacity);
            revealing = opacity < 1f;
        }
        if (keyboard == null) return;
        label.SetLiteralText(keyboard.text);
        if (keyboard.status == TouchScreenKeyboard.Status.Visible) return;
        bool canceled = keyboard.status == TouchScreenKeyboard.Status.Canceled;
        value = canceled ? beforeEditing : keyboard.text;
        keyboard.active = false;
        keyboard = null;
        transform.parent.position = originalPosition;
        if (canceled) label.SetLiteralText(value);
        else Commit();
    }

    public void Commit()
    {
        if (keyboard != null)
        {
            value = keyboard.text;
            keyboard.active = false;
            keyboard = null;
            transform.parent.position = originalPosition;
        }
        desktopEditing = false;
        UserSettings.Instance.SetUserName(value);
        value = UserSettings.Instance.GetUserName();
        label.SetLiteralText(value);
    }

    // Desktop builds can edit the same nickname with their normal text input/IME.
    void OnGUI()
    {
        if (!desktopEditing) return;
        Camera camera = button.viewCamera;
        if (camera == null) return;
        Bounds bounds = GetComponent<Renderer>().bounds;
        Vector3 min = camera.WorldToScreenPoint(bounds.min), max = camera.WorldToScreenPoint(bounds.max);
        Rect rect = new Rect(min.x, Screen.height - max.y, max.x - min.x, max.y - min.y);
        GUI.SetNextControlName("Player nickname");
        value = GUI.TextField(rect, value, MaximumLength);
        GUI.FocusControl("Player nickname");
        if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)) Commit();
        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            value = beforeEditing;
            desktopEditing = false;
            label.SetLiteralText(value);
        }
    }

    void OnDisable()
    {
        if (keyboard != null)
        {
            keyboard.active = false;
            keyboard = null;
            transform.parent.position = originalPosition;
        }
        desktopEditing = false;
    }
}
