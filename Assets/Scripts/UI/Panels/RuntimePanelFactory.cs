using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// RuntimePanelFactory — Creates UGUI panels at runtime when no prefab is assigned.
/// Builds Profile, WorldTour, ArtistBattle, and DailyChallenge panels programmatically.
/// These can later be replaced with proper prefab assets.
/// </summary>
public static class RuntimePanelFactory
{
    // Theme colors matching UIConfig defaults
    private static readonly Color BG_DARK = new Color(0.08f, 0.08f, 0.14f, 0.97f);
    private static readonly Color BG_CARD = new Color(0.12f, 0.12f, 0.20f, 0.90f);
    private static readonly Color PRIMARY = new Color(0.2f, 0.78f, 1f);
    private static readonly Color ACCENT = new Color(1f, 0.4f, 0.6f);
    private static readonly Color SUCCESS = new Color(0.3f, 1f, 0.4f);
    private static readonly Color WARNING = new Color(1f, 0.8f, 0.2f);
    private static readonly Color TEXT_PRIMARY = Color.white;
    private static readonly Color TEXT_DIM = new Color(0.7f, 0.7f, 0.8f);
    private static readonly Color TEXT_MUTED = new Color(0.5f, 0.5f, 0.6f);
    private static readonly Color BTN_PRIMARY = new Color(0.2f, 0.55f, 0.95f);
    private static readonly Color BTN_SECONDARY = new Color(0.3f, 0.3f, 0.45f);
    private static readonly Color BTN_DANGER = new Color(0.85f, 0.2f, 0.3f);
    private static readonly Color BTN_SUCCESS = new Color(0.15f, 0.7f, 0.35f);

    /// <summary>
    /// Creates a panel for the given state. Returns null if state doesn't need a runtime panel.
    /// </summary>
    public static GameObject CreatePanel(GameState state, Transform parent)
    {
        switch (state)
        {
            case GameState.Profile:       return CreateProfilePanel(parent);
            case GameState.WorldTour:     return CreateWorldTourPanel(parent);
            case GameState.ArtistBattle:  return CreateArtistBattlePanel(parent);
            case GameState.DailyChallenge:return CreateDailyChallengePanel(parent);
            case GameState.SongResult:    return CreateSongResultPanel(parent);
            default: return null;
        }
    }

    // ============================================================
    //  PROFILE PANEL
    // ============================================================
    private static GameObject CreateProfilePanel(Transform parent)
    {
        var panel = CreateBasePanel("ProfilePanel", parent);
        var content = GetContentArea(panel);

        // Title
        CreateTitle(content, "PROFIL", PRIMARY);

        // Player Info Card
        var infoCard = CreateCard(content, 180f);
        CreateLabel(infoCard.transform, "profile-level", "LV.1", 42, PRIMARY, TextAlignmentOptions.Center, true);
        CreateLabel(infoCard.transform, "profile-rank", "Rank: Caylak", 22, WARNING, TextAlignmentOptions.Center);
        CreateLabel(infoCard.transform, "profile-xp", "XP: 0/500 (0%)", 16, TEXT_DIM, TextAlignmentOptions.Center);

        // XP Bar
        var xpBar = CreateProgressBar(infoCard.transform, "profile-xp-bar", PRIMARY, 0f);

        CreateLabel(infoCard.transform, "profile-currency", "Altin: 0", 18, WARNING, TextAlignmentOptions.Center);
        CreateLabel(infoCard.transform, "profile-streak", "Giris Serisi: 0 gun", 14, ACCENT, TextAlignmentOptions.Center);

        // Stats Card
        var statsCard = CreateCard(content, 160f);
        CreateLabel(statsCard.transform, "", "ISTATISTIKLER", 18, PRIMARY, TextAlignmentOptions.Left, true);
        CreateLabel(statsCard.transform, "profile-total-songs", "Calinan Sarki: 0", 15, TEXT_DIM);
        CreateLabel(statsCard.transform, "profile-total-notes", "Vurulan Nota: 0", 15, TEXT_DIM);
        CreateLabel(statsCard.transform, "profile-best-combo", "En Iyi Kombo: 0", 15, TEXT_DIM);
        CreateLabel(statsCard.transform, "profile-avg-accuracy", "En Iyi Dogruluk: %0", 15, TEXT_DIM);

        // Achievements Card
        var achieveCard = CreateCard(content, 120f);
        CreateLabel(achieveCard.transform, "", "BASARIMLAR", 18, PRIMARY, TextAlignmentOptions.Left, true);
        CreateLabel(achieveCard.transform, "profile-achievements", "Acilan: 0 / 50 (%0)", 15, TEXT_DIM);
        CreateLabel(achieveCard.transform, "profile-recent-achieve", "", 14, SUCCESS);

        // Back Button
        CreateButton(content, "BackButton", "< Geri", BTN_SECONDARY, () => { });

        return panel;
    }

    // ============================================================
    //  WORLD TOUR PANEL
    // ============================================================
    private static GameObject CreateWorldTourPanel(Transform parent)
    {
        var panel = CreateBasePanel("WorldTourPanel", parent);
        var content = GetContentArea(panel);

        // Title
        CreateTitle(content, "DUNYA TURU", PRIMARY);

        // Progress bar
        var progressCard = CreateCard(content, 70f);
        CreateLabel(progressCard.transform, "tour-progress-text", "Tur Ilerlemesi: %0", 16, TEXT_DIM, TextAlignmentOptions.Center);
        CreateProgressBar(progressCard.transform, "tour-progress-bar", SUCCESS, 0f);

        // City List (scrollable)
        var scrollGo = CreateScrollView(content, "tour-city-list", 420f);

        // Scroll content will be populated by TourPanelController
        // Create placeholder cities
        var scrollContent = scrollGo.GetComponent<ScrollRect>().content;
        for (int i = 0; i < 10; i++)
        {
            var cityCard = CreateCard(scrollContent, 90f, addVerticalLayout: false);
            cityCard.name = $"CityCard_{i}";

            var hl = cityCard.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.padding = new RectOffset(10, 10, 5, 5);
            hl.childForceExpandWidth = false;

            // Icon
            CreateLabel(cityCard.transform, $"city-icon-{i}", "#", 28, TEXT_PRIMARY, TextAlignmentOptions.Center, false, 50f);
            // Info group
            var info = new GameObject($"CityInfo_{i}", typeof(RectTransform), typeof(VerticalLayoutGroup));
            info.transform.SetParent(cityCard.transform, false);
            info.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            var infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;
            CreateLabel(info.transform, $"city-name-{i}", "Sehir Adi", 16, TEXT_PRIMARY, TextAlignmentOptions.Left, true);
            CreateLabel(info.transform, $"city-status-{i}", "[Kilitli]", 13, TEXT_MUTED);
            // Stars
            CreateLabel(cityCard.transform, $"city-stars-{i}", "---", 18, WARNING, TextAlignmentOptions.Center, false, 80f);
        }

        // Buttons row
        var btnRow = CreateHorizontalGroup(content, 55f);
        CreateButton(btnRow, "BackButton", "< Geri", BTN_SECONDARY, () => { });
        CreateButton(btnRow, "StartConcertButton", "> Konser Baslat", BTN_PRIMARY, () => { });

        return panel;
    }

    // ============================================================
    //  ARTIST BATTLE PANEL
    // ============================================================
    private static GameObject CreateArtistBattlePanel(Transform parent)
    {
        var panel = CreateBasePanel("ArtistBattlePanel", parent);
        var content = GetContentArea(panel);

        // Title
        CreateTitle(content, "BESTECI DUELLOSU", ACCENT);

        // Battle Stats Card
        var statsCard = CreateCard(content, 60f);
        CreateLabel(statsCard.transform, "battle-stats", "Galibiyet: 0  |  Maglubiyet: 0", 15, TEXT_DIM, TextAlignmentOptions.Center);

        // Artist List (scrollable)
        var scrollGo = CreateScrollView(content, "battle-artist-list", 440f);
        var scrollContent = scrollGo.GetComponent<ScrollRect>().content;

        for (int i = 0; i < 8; i++)
        {
            var artistCard = CreateCard(scrollContent, 110f, addVerticalLayout: false);
            artistCard.name = $"ArtistCard_{i}";

            // Portrait + Info
            var hl = artistCard.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 12;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.padding = new RectOffset(12, 12, 8, 8);
            hl.childForceExpandWidth = false;

            // Portrait icon
            CreateLabel(artistCard.transform, $"artist-icon-{i}", "~", 36, TEXT_PRIMARY, TextAlignmentOptions.Center, false, 55f);

            // Info column
            var info = new GameObject($"ArtistInfo_{i}", typeof(RectTransform), typeof(VerticalLayoutGroup));
            info.transform.SetParent(artistCard.transform, false);
            info.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            info.GetComponent<VerticalLayoutGroup>().spacing = 2;
            var infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;

            CreateLabel(info.transform, $"artist-name-{i}", "Besteci Adi", 16, TEXT_PRIMARY, TextAlignmentOptions.Left, true);
            CreateLabel(info.transform, $"artist-era-{i}", "Donem", 12, TEXT_MUTED);
            CreateLabel(info.transform, $"artist-ability-{i}", "* Ozel Yetenek", 13, PRIMARY);
            CreateLabel(info.transform, $"artist-status-{i}", "[Kilitli] (Seviye 3)", 13, TEXT_MUTED);

            // Fight button
            var fightBtn = CreateButton(artistCard.transform, $"FightButton_{i}", "!!", BTN_DANGER, () => { }, 55f);
        }

        // Bottom
        var btnRow = CreateHorizontalGroup(content, 55f);
        CreateButton(btnRow, "BackButton", "< Geri", BTN_SECONDARY, () => { });

        return panel;
    }

    // ============================================================
    //  DAILY CHALLENGE PANEL
    // ============================================================
    private static GameObject CreateDailyChallengePanel(Transform parent)
    {
        var panel = CreateBasePanel("DailyChallengePanel", parent);
        var content = GetContentArea(panel);

        // Title
        CreateTitle(content, "GUNLUK GOREVLER", WARNING);

        // Streak Card
        var streakCard = CreateCard(content, 80f);
        CreateLabel(streakCard.transform, "daily-streak", "Ardisik Gun: 0 / 7", 18, ACCENT, TextAlignmentOptions.Center, true);
        CreateProgressBar(streakCard.transform, "daily-streak-bar", ACCENT, 0f);
        CreateLabel(streakCard.transform, "daily-weekly-status", "Haftalik Bonus: Henuz kazanilmadi", 13, TEXT_MUTED, TextAlignmentOptions.Center);

        // Challenge Cards
        for (int i = 0; i < 3; i++)
        {
            var card = CreateCard(content, 100f, addVerticalLayout: false);
            card.name = $"ChallengeCard_{i}";

            var hl = card.AddComponent<HorizontalLayoutGroup>();
            hl.spacing = 10;
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.padding = new RectOffset(12, 12, 8, 8);
            hl.childForceExpandWidth = false;

            // Icon
            CreateLabel(card.transform, $"challenge-icon-{i}", "*", 30, TEXT_PRIMARY, TextAlignmentOptions.Center, false, 45f);

            // Info
            var info = new GameObject($"ChallengeInfo_{i}", typeof(RectTransform), typeof(VerticalLayoutGroup));
            info.transform.SetParent(card.transform, false);
            info.GetComponent<VerticalLayoutGroup>().childForceExpandHeight = false;
            info.GetComponent<VerticalLayoutGroup>().spacing = 3;
            var infoLE = info.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1;

            CreateLabel(info.transform, $"challenge-title-{i}", "Gorev Basligi", 16, TEXT_PRIMARY, TextAlignmentOptions.Left, true);
            CreateLabel(info.transform, $"challenge-desc-{i}", "Gorev aciklamasi", 13, TEXT_DIM);
            CreateLabel(info.transform, $"challenge-progress-{i}", "Ilerleme: 0/3", 13, PRIMARY);
            CreateLabel(info.transform, $"challenge-reward-{i}", "+50 XP  +15 Altin", 12, SUCCESS);

            // Status check
            CreateLabel(card.transform, $"challenge-check-{i}", "[ ]", 26, TEXT_MUTED, TextAlignmentOptions.Center, false, 40f);
        }

        // Summary
        var summaryCard = CreateCard(content, 55f);
        CreateLabel(summaryCard.transform, "daily-completed-count", "Bugun Tamamlanan: 0 / 3", 15, TEXT_DIM, TextAlignmentOptions.Center);
        CreateLabel(summaryCard.transform, "daily-total-completed", "Toplam Tamamlanan Gorev: 0", 13, TEXT_MUTED, TextAlignmentOptions.Center);

        // Buttons
        var btnRow = CreateHorizontalGroup(content, 55f);
        CreateButton(btnRow, "BackButton", "< Geri", BTN_SECONDARY, () => { });
        CreateButton(btnRow, "PlayButton", "> Sarki Cal", BTN_PRIMARY, () => { });

        return panel;
    }

    // ============================================================
    //  BASE PANEL BUILDER HELPERS
    // ============================================================

    private static GameObject CreateBasePanel(string name, Transform parent)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var bg = panel.GetComponent<Image>();
        bg.color = BG_DARK;

        // Scroll area with vertical layout
        var scrollGo = new GameObject("ScrollArea", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(panel.transform, false);
        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0.05f, 0.02f);
        scrollRT.anchorMax = new Vector2(0.95f, 0.98f);
        scrollRT.offsetMin = Vector2.zero;
        scrollRT.offsetMax = Vector2.zero;
        scrollGo.GetComponent<Image>().color = Color.clear;

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        contentGo.transform.SetParent(scrollGo.transform, false);
        var contentRT = contentGo.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.offsetMin = Vector2.zero;
        contentRT.offsetMax = Vector2.zero;

        var csf = contentGo.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 10;
        vlg.padding = new RectOffset(8, 8, 12, 20);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.content = contentRT;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 25f;

        // Mask
        var mask = scrollGo.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        scrollGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f); // Need very faint for Mask

        return panel;
    }

    private static Transform GetContentArea(GameObject panel)
    {
        return panel.transform.Find("ScrollArea/Content");
    }

    private static void CreateTitle(Transform parent, string text, Color color)
    {
        var go = new GameObject("Title", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = 50;
        go.GetComponent<LayoutElement>().flexibleWidth = 1;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 28;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = false;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string goName, string text, int fontSize,
        Color color, TextAlignmentOptions align = TextAlignmentOptions.Left, bool bold = false, float fixedWidth = -1)
    {
        if (string.IsNullOrEmpty(goName)) goName = "Label_" + text.GetHashCode();
        var go = new GameObject(goName, typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = fontSize + 10;
        if (fixedWidth > 0)
        {
            le.preferredWidth = fixedWidth;
            le.flexibleWidth = 0;
        }
        else
        {
            le.flexibleWidth = 1;
        }

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    private static GameObject CreateCard(Transform parent, float height, bool addVerticalLayout = true)
    {
        var card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        card.transform.SetParent(parent, false);

        card.GetComponent<Image>().color = BG_CARD;
        var le = card.GetComponent<LayoutElement>();
        le.preferredHeight = height;
        le.flexibleWidth = 1;

        if (addVerticalLayout)
        {
            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4;
            vlg.padding = new RectOffset(14, 14, 10, 10);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
        }

        return card;
    }

    private static GameObject CreateProgressBar(Transform parent, string goName, Color fillColor, float progress)
    {
        var bar = new GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        bar.transform.SetParent(parent, false);
        bar.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f);
        var barLE = bar.GetComponent<LayoutElement>();
        barLE.preferredHeight = 14;
        barLE.flexibleWidth = 1;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(bar.transform, false);
        fill.GetComponent<Image>().color = fillColor;
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        return bar;
    }

    private static Button CreateButton(Transform parent, string goName, string text, Color bgColor,
        System.Action onClick, float fixedWidth = -1)
    {
        var go = new GameObject(goName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        go.GetComponent<Image>().color = bgColor;
        var le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 45;
        if (fixedWidth > 0)
        {
            le.preferredWidth = fixedWidth;
            le.flexibleWidth = 0;
        }
        else
        {
            le.flexibleWidth = 1;
        }

        // Create child text object (Button GO already has Image, can't have two Graphic components)
        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRT = textGo.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        var tmpChild = textGo.AddComponent<TextMeshProUGUI>();
        tmpChild.text = text;
        tmpChild.fontSize = 16;
        tmpChild.color = TEXT_PRIMARY;
        tmpChild.fontStyle = FontStyles.Bold;
        tmpChild.alignment = TextAlignmentOptions.Center;

        var btn = go.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        return btn;
    }

    private static GameObject CreateScrollView(Transform parent, string goName, float height)
    {
        var scrollGo = new GameObject(goName, typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(LayoutElement), typeof(Mask));
        scrollGo.transform.SetParent(parent, false);
        scrollGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        scrollGo.GetComponent<LayoutElement>().preferredHeight = height;
        scrollGo.GetComponent<LayoutElement>().flexibleWidth = 1;
        scrollGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        contentGo.transform.SetParent(scrollGo.transform, false);
        var crt = contentGo.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1);
        crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        scrollGo.GetComponent<ScrollRect>().content = crt;
        scrollGo.GetComponent<ScrollRect>().horizontal = false;
        scrollGo.GetComponent<ScrollRect>().vertical = true;
        scrollGo.GetComponent<ScrollRect>().scrollSensitivity = 25f;

        return scrollGo;
    }

    private static Transform CreateHorizontalGroup(Transform parent, float height)
    {
        var go = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().preferredHeight = height;
        go.GetComponent<LayoutElement>().flexibleWidth = 1;
        var hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 12;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;
        return go.transform;
    }

    // ============================================================
    //  SONG RESULT PANEL (3-star animated result screen)
    // ============================================================
    private static GameObject CreateSongResultPanel(Transform parent)
    {
        var panel = CreateBasePanel("SongResultPanel", parent);
        var content = GetContentArea(panel);

        // Add a named wrapper so controller can find it
        var resultContent = new GameObject("ResultContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        resultContent.transform.SetParent(content, false);
        var rcRT = resultContent.GetComponent<RectTransform>();
        rcRT.anchorMin = Vector2.zero;
        rcRT.anchorMax = Vector2.one;
        rcRT.offsetMin = Vector2.zero;
        rcRT.offsetMax = Vector2.zero;

        var rcVLG = resultContent.GetComponent<VerticalLayoutGroup>();
        rcVLG.spacing = 6;
        rcVLG.padding = new RectOffset(20, 20, 10, 10);
        rcVLG.childAlignment = TextAnchor.UpperCenter;
        rcVLG.childForceExpandWidth = true;
        rcVLG.childForceExpandHeight = false;
        rcVLG.childControlWidth = true;
        rcVLG.childControlHeight = true;

        resultContent.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var rc = resultContent.transform;

        // --- "SONUC" Title ---
        CreateLabel(rc, "result-header", "SONUC", 28, PRIMARY, TextAlignmentOptions.Center, true);

        // --- Song Title & Artist ---
        CreateLabel(rc, "result-song-title", "...", 22, TEXT_PRIMARY, TextAlignmentOptions.Center, true);
        CreateLabel(rc, "result-artist", "", 16, TEXT_DIM, TextAlignmentOptions.Center);

        // --- Grade Badge ---
        var gradeCard = CreateCard(rc, 70f);
        CreateLabel(gradeCard.transform, "result-grade", "?", 52, WARNING, TextAlignmentOptions.Center, true);

        // --- 3 Stars Row ---
        var starsRow = new GameObject("StarsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        starsRow.transform.SetParent(rc, false);
        starsRow.GetComponent<LayoutElement>().preferredHeight = 80;
        starsRow.GetComponent<LayoutElement>().flexibleWidth = 1;
        var starsHLG = starsRow.GetComponent<HorizontalLayoutGroup>();
        starsHLG.spacing = 24;
        starsHLG.childAlignment = TextAnchor.MiddleCenter;
        starsHLG.childForceExpandWidth = false;
        starsHLG.childForceExpandHeight = false;
        starsHLG.childControlWidth = false;
        starsHLG.childControlHeight = false;

        for (int i = 0; i < 3; i++)
        {
            // Star container with glow
            var starGo = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
            starGo.transform.SetParent(starsRow.transform, false);
            var starRT = starGo.GetComponent<RectTransform>();
            starRT.sizeDelta = new Vector2(60, 60);
            var starImg = starGo.GetComponent<Image>();
            starImg.color = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            // Use a rounded rect or just color — no sprite needed
            starImg.type = Image.Type.Simple;

            // Glow child (larger, behind visually but rendered after due to hierarchy)
            var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(starGo.transform, false);
            glowGo.transform.SetAsFirstSibling(); // Behind the star
            var glowRT = glowGo.GetComponent<RectTransform>();
            glowRT.sizeDelta = new Vector2(80, 80);
            glowRT.anchorMin = new Vector2(0.5f, 0.5f);
            glowRT.anchorMax = new Vector2(0.5f, 0.5f);
            glowRT.anchoredPosition = Vector2.zero;
            var glowImg = glowGo.GetComponent<Image>();
            glowImg.color = new Color(1f, 0.85f, 0.1f, 0f);

            // Star text overlay (the actual star symbol using a supported character)
            var starText = new GameObject("StarText", typeof(RectTransform));
            starText.transform.SetParent(starGo.transform, false);
            var stRT = starText.GetComponent<RectTransform>();
            stRT.anchorMin = Vector2.zero;
            stRT.anchorMax = Vector2.one;
            stRT.offsetMin = Vector2.zero;
            stRT.offsetMax = Vector2.zero;
            var tmp = starText.AddComponent<TextMeshProUGUI>();
            tmp.text = "*";
            tmp.fontSize = 40;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
        }

        // --- Accuracy ---
        var accuracyCard = CreateCard(rc, 50f);
        var accuracyRow = new GameObject("AccuracyRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        accuracyRow.transform.SetParent(accuracyCard.transform, false);
        accuracyRow.GetComponent<LayoutElement>().preferredHeight = 40;
        accuracyRow.GetComponent<LayoutElement>().flexibleWidth = 1;
        var aHLG = accuracyRow.GetComponent<HorizontalLayoutGroup>();
        aHLG.childAlignment = TextAnchor.MiddleCenter;
        aHLG.spacing = 8;
        aHLG.childForceExpandWidth = true;
        aHLG.childForceExpandHeight = false;
        CreateLabel(accuracyRow.transform, "", "Dogruluk:", 18, TEXT_DIM, TextAlignmentOptions.Right, true);
        CreateLabel(accuracyRow.transform, "result-accuracy", "%0.0", 26, SUCCESS, TextAlignmentOptions.Left, true);

        // --- Score & Combo Row ---
        var scoreRow = CreateHorizontalGroup(rc, 45f);
        CreateStatPair(scoreRow, "Skor", "result-score", "0");
        CreateStatPair(scoreRow, "Maks Kombo", "result-combo", "0");

        // --- Hit Breakdown Card ---
        var hitsCard = CreateCard(rc, 130f);
        CreateLabel(hitsCard.transform, "", "ISABET DETAYLARI", 15, PRIMARY, TextAlignmentOptions.Center, true);

        var hitGrid = new GameObject("HitGrid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
        hitGrid.transform.SetParent(hitsCard.transform, false);
        hitGrid.GetComponent<LayoutElement>().preferredHeight = 90;
        hitGrid.GetComponent<LayoutElement>().flexibleWidth = 1;
        var grid = hitGrid.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(130, 38);
        grid.spacing = new Vector2(8, 4);
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 2;

        CreateHitStatCell(hitGrid.transform, "Perfect", "result-perfect", "0", SUCCESS);
        CreateHitStatCell(hitGrid.transform, "Good", "result-good", "0", PRIMARY);
        CreateHitStatCell(hitGrid.transform, "Okay", "result-okay", "0", WARNING);
        CreateHitStatCell(hitGrid.transform, "Miss", "result-miss", "0", new Color(1f, 0.3f, 0.3f));

        // --- XP & Currency Row ---
        var rewardCard = CreateCard(rc, 60f);
        var rewardRow = CreateHorizontalGroup(rewardCard.transform, 50f);
        CreateStatPair(rewardRow, "XP", "result-xp", "+0 XP");
        CreateStatPair(rewardRow, "Altin", "result-currency", "+0");

        // --- Level Info ---
        CreateLabel(rc, "result-level", "Seviye 1", 16, TEXT_DIM, TextAlignmentOptions.Center);

        // --- Action Buttons ---
        var btnRow = CreateHorizontalGroup(rc, 55f);
        CreateButton(btnRow, "RetryButton", "Tekrar Oyna", ACCENT, () =>
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr != null) uiMgr.OnRestartPressed?.Invoke();
        });
        CreateButton(btnRow, "ContinueButton", "Ana Menu", BTN_PRIMARY, () =>
        {
            var uiMgr = UIManager.Instance;
            if (uiMgr != null) uiMgr.OnMainMenuPressed?.Invoke();
        });

        return panel;
    }

    // Helper: stat pair (label + value stacked)
    private static void CreateStatPair(Transform parent, string label, string valueName, string defaultValue)
    {
        var col = new GameObject(valueName + "_col", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        col.transform.SetParent(parent, false);
        col.GetComponent<LayoutElement>().flexibleWidth = 1;
        var vlg = col.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.spacing = 2;

        CreateLabel(col.transform, "", label, 13, TEXT_MUTED, TextAlignmentOptions.Center);
        CreateLabel(col.transform, valueName, defaultValue, 20, TEXT_PRIMARY, TextAlignmentOptions.Center, true);
    }

    // Helper: hit stat cell (colored value + label)
    private static void CreateHitStatCell(Transform parent, string label, string valueName, string defaultValue, Color color)
    {
        var cell = new GameObject(valueName + "_cell", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        cell.transform.SetParent(parent, false);
        var hlg = cell.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 6;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        CreateLabel(cell.transform, "", label + ":", 14, TEXT_DIM, TextAlignmentOptions.Right);
        CreateLabel(cell.transform, valueName, defaultValue, 16, color, TextAlignmentOptions.Left, true);
    }
}
