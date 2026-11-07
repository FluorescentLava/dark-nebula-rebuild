using UnityEngine;

// A local browser for the recovered gold-star achievements and original badges.
public sealed class AchievementsScreen : MonoBehaviour
{
    const int RowsPerPage = 4;
    readonly GameObject[] rows = new GameObject[RowsPerPage];
    System.Action onClose;
    FlipperButton pages;
    DynamicFont summary;
    SimpleButton back;
    int page;
    bool closing;

    public static void Open(IngameOptionScreen template, System.Action closed)
    {
        var root = new GameObject("Achievements");
        root.layer = template.gameObject.layer;
        root.transform.SetParent(template.transform.parent, false);
        root.transform.localPosition = template.transform.localPosition;
        var screen = root.AddComponent<AchievementsScreen>();
        screen.onClose = closed;
        screen.Initialize(template);
    }

    GameObject Copy(GameObject template, string name, float y)
    {
        var copy = Instantiate(template, transform);
        copy.name = name;
        copy.transform.localPosition = new Vector3(0f, y, template.transform.localPosition.z);
        copy.SetActive(true);
        foreach (var button in copy.GetComponentsInChildren<SimpleButton>(true))
        {
            button.targetObject = null;
            button.messageName = string.Empty;
            button.enabled = true;
        }
        return copy;
    }

    void Initialize(IngameOptionScreen template)
    {
        Transform background = template.transform.Find("Background");
        if (background != null && background.gameObject.activeSelf)
        {
            var copy = Instantiate(background.gameObject, transform);
            copy.name = "Background";
            copy.transform.localPosition = new Vector3(0f, 0f, 10f);
            copy.SetActive(true);
        }
        pages = Copy(template.FlipperMusic, "Achievement pages", 225f).GetComponent<FlipperButton>();
        pages.Title.SetOriginalPos();
        pages.Title.SetOriginalPos(pages.Title.transform.localPosition + Vector3.up * 12f);
        pages.EventChoiceChanged += index => { page = index; Refresh(); };
        summary = Instantiate(pages.Value, transform);
        summary.name = "Achievement progress";
        summary.SetOriginalPos(new Vector3(0f, 171f, 0f));
        summary.SelectedEntry = 4;
        back = Copy(template.BtnGoBack, "Back", -230f).GetComponent<SimpleButton>();
        back.ButtonUpEvent += source => Close();
        GameObject popup = Resources.Load<GameObject>("Prefabs/UI/AchievementPopup");
        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = Instantiate(popup, transform);
            rows[i].name = "Achievement row " + i;
            rows[i].transform.localPosition = new Vector3(0f, 115f - i * 82f, -1f);
            Destroy(rows[i].GetComponent<AchievementPopup>());
        }
        var fit = gameObject.AddComponent<MenuContentFit>();
        fit.Camera = template.BtnGoBack.GetComponent<SimpleButton>().viewCamera;
        if (fit.Camera == null) fit.Camera = Camera.main;
        GamePreferences.Changed += Refresh;
        Refresh();
    }

    void Refresh()
    {
        var achievements = DarkNebulaAchievements.Instance;
        int count = achievements.AchievementCount;
        int stars = achievements.GetGoldStarsCount(), unlocked = 0;
        for (int i = 0; i < count; i++) if (stars >= achievements.GetStarsRequired(i)) unlocked++;
        summary.SetLiteralText(GameLocalization.Format("ACHIEVEMENT SUMMARY", unlocked, count));
        int pageCount = (count + RowsPerPage - 1) / RowsPerPage;
        string[] names = new string[pageCount];
        for (int i = 0; i < names.Length; i++) names[i] = (i + 1) + " / " + pageCount;
        pages.InitChoices(page, names, "ACHIEVEMENTS");
        back.Text = GameLocalization.Text("GO BACK");
        for (int i = 0; i < rows.Length; i++)
        {
            int index = page * RowsPerPage + i;
            rows[i].SetActive(index < count);
            if (index >= count) continue;
            int required = achievements.GetStarsRequired(index);
            bool earned = stars >= required;
            var title = rows[i].transform.Find("Background/Text").GetComponent<DynamicFont>();
            title.Text = GameLocalization.Text("ACHIEVEMENT.GOLDSTARS." + index);
            title.FontColor = earned ? 0 : 1;
            title.Refresh();
            var status = rows[i].transform.Find("Background/Header").GetComponent<DynamicFont>();
            status.FontColor = earned ? 6 : 5;
            status.SetLiteralText(earned ? GameLocalization.Text("UNLOCKED") :
                GameLocalization.Format("ACHIEVEMENT PROGRESS", Mathf.Min(stars, required), required));
            var icon = rows[i].transform.Find("Background/Icon").GetComponent<tk2dSprite>();
            icon.SetSprite(achievements.GetIconId(index));
            icon.color = earned ? Color.white : new Color(.4f, .4f, .4f, 1f);
        }
    }

    public void Close()
    {
        if (closing) return;
        closing = true;
        if (onClose != null) onClose();
        Destroy(gameObject);
    }
    void Update() { if (Input.GetKeyUp(KeyCode.Escape)) Close(); }
    void OnDestroy() { GamePreferences.Changed -= Refresh; }
}
