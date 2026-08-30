using UnityEngine;

public sealed partial class NeonCircuitGame
{
    private GUIStyle storyTitleStyle;
    private GUIStyle storyBodyStyle;
    private GUIStyle storyObjectiveStyle;
    private GUIStyle storyDialogueBodyStyle;
    private Texture2D storyCharacterSheet;

    private void EnsureStoryStyles()
    {
        if (storyTitleStyle != null)
        {
            return;
        }

        storyTitleStyle = new GUIStyle(arcadeHeadingStyle);
        storyTitleStyle.fontSize = 30;
        storyTitleStyle.wordWrap = false;
        storyTitleStyle.alignment = TextAnchor.MiddleLeft;

        storyBodyStyle = new GUIStyle(arcadeSmallStyle);
        storyBodyStyle.fontSize = 16;
        storyBodyStyle.wordWrap = true;
        storyBodyStyle.alignment = TextAnchor.UpperLeft;
        storyBodyStyle.normal.textColor = new Color(0.76f, 0.91f, 0.96f);

        storyObjectiveStyle = new GUIStyle(arcadeHeadingStyle);
        storyObjectiveStyle.fontSize = 19;
        storyObjectiveStyle.wordWrap = true;
        storyObjectiveStyle.alignment = TextAnchor.MiddleLeft;
        storyObjectiveStyle.normal.textColor = Color.white;

        storyDialogueBodyStyle = new GUIStyle(arcadeSmallStyle);
        storyDialogueBodyStyle.fontSize = 23;
        storyDialogueBodyStyle.fontStyle = FontStyle.Bold;
        storyDialogueBodyStyle.wordWrap = true;
        storyDialogueBodyStyle.alignment = TextAnchor.UpperLeft;
        storyDialogueBodyStyle.normal.textColor = new Color(0.88f, 0.96f, 1f);

        storyCharacterSheet = Resources.Load<Texture2D>("UI/Story/StoryCharactersPixel");
        if (storyCharacterSheet != null)
        {
            storyCharacterSheet.filterMode = FilterMode.Point;
            storyCharacterSheet.wrapMode = TextureWrapMode.Clamp;
        }
    }

    private void DrawArcadeStoryMenu(float screenWidth, float screenHeight)
    {
        EnsureStoryStyles();
        bool hasArtwork = DrawRetroArtwork(screenWidth, screenHeight, 0.46f);
        if (!hasArtwork)
        {
            DrawArcadeBackdrop(screenWidth, screenHeight);
            DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.002f, 0.005f, 0.02f, 0.72f));
        }
        else
        {
            DrawArcadeMenuAmbientMotion(screenWidth, screenHeight);
        }

        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.001f, 0.004f, 0.015f, 0.34f));
        float margin = Mathf.Clamp(screenWidth * 0.022f, 16f, 42f);
        float headerHeight = Mathf.Clamp(screenHeight * 0.115f, 66f, 90f);
        Rect header = new Rect(margin, margin, screenWidth - margin * 2f, headerHeight);
        DrawArcadePanel(header, ArcadePink, ArcadeCyan);
        DrawSolidRect(new Rect(header.x + 9f, header.y + 9f, 9f, header.height - 18f), ArcadePink);
        GUI.Label(new Rect(header.x + 31f, header.y + 7f, header.width * 0.55f, header.height * 0.58f), "СЮЖЕТ", arcadeTitleStyle);
        GUI.Label(new Rect(header.x + 34f, header.y + header.height * 0.62f, header.width * 0.58f, 18f), "NIGHT LEAGUE  //  STORY MODE  //  8 ГЛАВ", arcadeMicroStyle);

        int completed = CompletedStoryChapterCount();
        float progressWidth = Mathf.Clamp(header.width * 0.27f, 210f, 350f);
        Rect progressBlock = new Rect(header.xMax - progressWidth - 18f, header.y + 11f, progressWidth, header.height - 22f);
        DrawSolidRect(progressBlock, new Color(0.002f, 0.012f, 0.034f, 0.94f));
        DrawSolidRect(new Rect(progressBlock.x, progressBlock.y, 5f, progressBlock.height), ArcadeLime);
        GUI.Label(new Rect(progressBlock.x + 15f, progressBlock.y + 5f, progressBlock.width - 30f, 22f), "ПРОГРЕСС  " + completed + " / " + StoryChapters.Length, arcadeSmallStyle);
        Rect progress = new Rect(progressBlock.x + 15f, progressBlock.yMax - 18f, progressBlock.width - 30f, 9f);
        DrawSolidRect(progress, new Color(0f, 0f, 0f, 0.82f));
        DrawSolidRect(new Rect(progress.x + 2f, progress.y + 2f, (progress.width - 4f) * completed / StoryChapters.Length, progress.height - 4f), ArcadeLime);

        float contentY = header.yMax + 14f;
        float contentHeight = screenHeight - contentY - margin;
        float listWidth = Mathf.Clamp(screenWidth * 0.34f, 305f, 500f);
        Rect chapterList = new Rect(margin, contentY, listWidth, contentHeight);
        float contentGap = Mathf.Clamp(screenWidth * 0.014f, 11f, 23f);
        Rect details = new Rect(chapterList.xMax + contentGap, contentY, screenWidth - margin - chapterList.xMax - contentGap, contentHeight);
        DrawArcadePanel(chapterList, ArcadeCyan, ArcadePink);
        DrawArcadePanel(details, ActiveTrack.AccentColor, ArcadeYellow);

        GUI.Label(new Rect(chapterList.x + 18f, chapterList.y + 11f, chapterList.width - 36f, 26f), "ВЫБОР ГЛАВЫ", arcadeHeadingStyle);
        GUI.Label(new Rect(chapterList.x + 20f, chapterList.y + 38f, chapterList.width - 40f, 16f), "MISSION SELECT  //  ПРОГРЕСС СОХРАНЯЕТСЯ", arcadeMicroStyle);

        const float cardGap = 6f;
        float cardsTop = chapterList.y + 62f;
        float cardsHeight = chapterList.yMax - cardsTop - 13f;
        float cardHeight = (cardsHeight - cardGap * (StoryChapters.Length - 1)) / StoryChapters.Length;
        for (int index = 0; index < StoryChapters.Length; index++)
        {
            Rect card = new Rect(
                chapterList.x + 14f,
                cardsTop + index * (cardHeight + cardGap),
                chapterList.width - 28f,
                cardHeight);
            DrawStoryChapterCard(card, index);
        }

        DrawStoryChapterDetails(details);
        DrawArcadeMenuMotionOverlay(screenWidth, screenHeight);
    }

    private void DrawStoryChapterCard(Rect rect, int chapterIndex)
    {
        StoryChapterDefinition chapter = StoryChapters[chapterIndex];
        bool unlocked = chapterIndex <= storyUnlockedChapter;
        bool selected = chapterIndex == storySelectedChapter;
        bool completed = IsStoryChapterCompleted(chapterIndex);
        bool hovered = unlocked && rect.Contains(Event.current.mousePosition);
        Color trackAccent = RaceTrackCatalog.Get(chapter.TrackIndex).AccentColor;
        Color accent = unlocked ? trackAccent : new Color(0.24f, 0.29f, 0.34f);
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 5f + chapterIndex * 0.8f) * 0.5f;
        Color fill = selected
            ? new Color(accent.r * 0.29f, accent.g * 0.22f, accent.b * 0.2f, 0.99f)
            : hovered
                ? new Color(accent.r * 0.18f, accent.g * 0.14f, accent.b * 0.16f, 0.99f)
                : new Color(0.003f, 0.013f, 0.038f, 0.985f);

        DrawSolidRect(new Rect(rect.x + 5f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.64f));
        if (selected || hovered)
        {
            DrawSolidRect(new Rect(rect.x - 3f, rect.y - 2f, rect.width + 6f, rect.height + 4f), new Color(accent.r, accent.g, accent.b, selected ? 0.22f + pulse * 0.12f : 0.13f));
        }
        DrawSolidRect(rect, fill);
        DrawSolidRect(new Rect(rect.x, rect.y, selected ? 8f : 4f, rect.height), accent);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, selected ? 4f : 2f), accent);
        float numberWidth = Mathf.Clamp(rect.width * 0.17f, 48f, 68f);
        DrawSolidRect(new Rect(rect.x + numberWidth, rect.y + 7f, 2f, rect.height - 14f), new Color(accent.r, accent.g, accent.b, 0.58f));

        Color previous = GUI.color;
        GUI.color = accent;
        GUI.Label(new Rect(rect.x + 8f, rect.y + 2f, numberWidth - 14f, rect.height - 4f), (chapterIndex + 1).ToString("00"), arcadeHeadingStyle);
        GUI.color = unlocked ? Color.white : new Color(0.5f, 0.56f, 0.62f);
        float textX = rect.x + numberWidth + 12f;
        GUI.Label(new Rect(textX, rect.y + 3f, rect.xMax - textX - 28f, rect.height * 0.56f), unlocked ? chapter.Title : "ЗАКРЫТО", arcadeSmallStyle);
        GUI.color = previous;

        string status = !unlocked
            ? "ТРЕБУЕТСЯ ПРЕДЫДУЩАЯ ГЛАВА"
            : completed
                ? "OK  //  " + chapter.ModeName
                : chapter.ModeName + "  //  " + chapter.VehicleName;
        GUI.Label(new Rect(textX, rect.y + rect.height * 0.5f, rect.xMax - textX - 28f, rect.height * 0.42f), status, arcadeMicroStyle);
        if (completed)
        {
            GUI.color = ArcadeLime;
            GUI.Label(new Rect(rect.xMax - 31f, rect.y + 5f, 28f, rect.height - 10f), "OK", arcadeMicroStyle);
            GUI.color = previous;
        }
        else if (unlocked)
        {
            GUI.color = accent;
            GUI.Label(new Rect(rect.xMax - 27f, rect.y + 5f, 22f, rect.height - 10f), ">", arcadeSmallStyle);
            GUI.color = previous;
        }

        if (unlocked)
        {
            RegisterMenuHover(7100 + chapterIndex, hovered);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                PlayMenuClickSfx();
                SelectStoryChapter(chapterIndex);
            }
        }
    }

    private void DrawStoryChapterDetails(Rect rect)
    {
        StoryChapterDefinition chapter = ActiveStoryChapter;
        RaceTrackDefinition track = RaceTrackCatalog.Get(chapter.TrackIndex);
        Color accent = track.AccentColor;
        bool completed = IsStoryChapterCompleted(storySelectedChapter);
        Color previous = GUI.color;

        Rect titleBand = new Rect(rect.x + 14f, rect.y + 13f, rect.width - 28f, Mathf.Clamp(rect.height * 0.145f, 58f, 76f));
        DrawSolidRect(titleBand, new Color(0.002f, 0.012f, 0.038f, 0.97f));
        DrawSolidRect(new Rect(titleBand.x, titleBand.y, 7f, titleBand.height), accent);
        DrawSolidRect(new Rect(titleBand.x, titleBand.y, titleBand.width, 3f), ArcadePink);
        GUI.Label(new Rect(titleBand.x + 18f, titleBand.y + 7f, titleBand.width - 36f, 18f), "ГЛАВА " + (storySelectedChapter + 1).ToString("00") + "  //  " + chapter.CodeName + "  //  " + chapter.ModeName, arcadeMicroStyle);
        GUI.color = accent;
        GUI.Label(new Rect(titleBand.x + 16f, titleBand.y + 25f, titleBand.width - 32f, titleBand.height - 28f), chapter.Title, storyTitleStyle);
        GUI.color = previous;

        float actionHeight = Mathf.Clamp(rect.height * 0.135f, 56f, 72f);
        float actionY = rect.yMax - actionHeight - 14f;
        float objectiveHeight = Mathf.Clamp(rect.height * 0.19f, 78f, 100f);
        Rect objective = new Rect(rect.x + 14f, actionY - objectiveHeight - 10f, rect.width - 28f, objectiveHeight);
        float upperTop = titleBand.yMax + 10f;
        float upperHeight = Mathf.Max(130f, objective.y - upperTop - 10f);
        float vehicleWidth = Mathf.Clamp(rect.width * 0.34f, 170f, 310f);
        Rect vehiclePanel = new Rect(rect.x + 14f, upperTop, vehicleWidth, upperHeight);
        Rect informationPanel = new Rect(vehiclePanel.xMax + 10f, upperTop, rect.xMax - 14f - vehiclePanel.xMax - 10f, upperHeight);
        DrawStoryVehicleShowcase(vehiclePanel, chapter, accent);

        float briefingHeight = Mathf.Clamp(informationPanel.height * 0.47f, 70f, 145f);
        Rect briefing = new Rect(informationPanel.x, informationPanel.y, informationPanel.width, briefingHeight);
        Rect mapPanel = new Rect(informationPanel.x, briefing.yMax + 8f, informationPanel.width, informationPanel.yMax - briefing.yMax - 8f);
        DrawSolidRect(briefing, new Color(0.003f, 0.012f, 0.035f, 0.96f));
        DrawSolidRect(new Rect(briefing.x, briefing.y, 5f, briefing.height), ArcadePink);
        DrawSolidRect(new Rect(briefing.x, briefing.y, briefing.width, 3f), ArcadePink);
        GUI.Label(new Rect(briefing.x + 13f, briefing.y + 7f, briefing.width - 26f, 19f), "БРИФИНГ  //  STORY TRANSMISSION", arcadeMicroStyle);
        GUI.Label(new Rect(briefing.x + 13f, briefing.y + 29f, briefing.width - 26f, briefing.height - 35f), chapter.Briefing, storyBodyStyle);

        DrawStoryTrackPreview(mapPanel, track, accent, chapter);

        DrawSolidRect(new Rect(objective.x + 5f, objective.y + 6f, objective.width, objective.height), new Color(0f, 0f, 0f, 0.6f));
        DrawSolidRect(objective, new Color(accent.r * 0.16f, accent.g * 0.11f, accent.b * 0.13f, 0.98f));
        DrawSolidRect(new Rect(objective.x, objective.y, 8f, objective.height), accent);
        DrawSolidRect(new Rect(objective.x, objective.y, objective.width, 3f), ArcadeYellow);
        GUI.Label(new Rect(objective.x + 18f, objective.y + 9f, 170f, 18f), "ЦЕЛЬ МИССИИ", arcadeMicroStyle);
        GUI.color = accent;
        GUI.Label(new Rect(objective.x + 18f, objective.y + 27f, objective.width - 220f, objective.height - 29f), chapter.Objective, storyObjectiveStyle);
        GUI.color = previous;
        Rect reward = new Rect(objective.xMax - 178f, objective.y + 10f, 162f, objective.height - 20f);
        DrawSolidRect(reward, new Color(0.025f, 0.019f, 0.002f, 0.9f));
        DrawSolidRect(new Rect(reward.x, reward.y, 4f, reward.height), ArcadeYellow);
        GUI.Label(new Rect(reward.x + 14f, reward.y + 5f, reward.width - 26f, 18f), completed ? "НАГРАДА ПОЛУЧЕНА" : "НАГРАДА", arcadeMicroStyle);
        GUI.color = completed ? new Color(0.54f, 0.63f, 0.67f) : ArcadeYellow;
        GUI.Label(new Rect(reward.x + 14f, reward.y + 23f, reward.width - 26f, reward.height - 26f), completed ? "REPLAY" : chapter.Reward + " COINS", arcadeCompactHeadingStyle);
        GUI.color = previous;

        float actionWidth = (rect.width - 38f) * 0.64f;
        if (DrawArcadeButton(new Rect(rect.x + 14f, actionY, actionWidth, actionHeight), "GO", "НАЧАТЬ МИССИЮ", track.ShortName + "  //  " + chapter.VehicleName, accent, true))
        {
            StartSelectedStoryChapter();
        }

        if (DrawArcadeButton(new Rect(rect.x + 24f + actionWidth, actionY, rect.width - actionWidth - 38f, actionHeight), "<", "НАЗАД", "ESC", ArcadeCyan, false))
        {
            CloseStoryMode();
        }
    }

    private void DrawStoryVehicleShowcase(Rect rect, StoryChapterDefinition chapter, Color accent)
    {
        DrawSolidRect(new Rect(rect.x + 5f, rect.y + 6f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.62f));
        DrawSolidRect(rect, new Color(0.002f, 0.012f, 0.036f, 0.98f));
        DrawSolidRect(new Rect(rect.x, rect.y, 5f, rect.height), ArcadeCyan);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 3f), accent);
        GUI.Label(new Rect(rect.x + 13f, rect.y + 8f, rect.width - 26f, 19f), "MISSION VEHICLE  //  LIVE", arcadeMicroStyle);

        float footerHeight = Mathf.Clamp(rect.height * 0.27f, 43f, 62f);
        Rect preview = new Rect(rect.x + 10f, rect.y + 31f, rect.width - 20f, Mathf.Max(54f, rect.height - footerHeight - 38f));
        if (chapter.VehicleType == StoryVehicleType.Car || storyVehicleSheet == null)
        {
            DrawStoryCarPreview(preview, chapter.CarIndex);
        }
        else
        {
            DrawStoryVehiclePreview(preview, chapter.VehicleType);
        }
        DrawSolidRect(new Rect(rect.x + 9f, rect.yMax - footerHeight, rect.width - 18f, footerHeight - 8f), new Color(accent.r * 0.14f, accent.g * 0.12f, accent.b * 0.12f, 0.95f));
        GUI.color = accent;
        GUI.Label(new Rect(rect.x + 15f, rect.yMax - footerHeight + 3f, rect.width - 30f, 27f), chapter.VehicleName, arcadeCompactHeadingStyle);
        GUI.color = Color.white;
        string vehicleClass = chapter.VehicleType == StoryVehicleType.Motorcycle
            ? "МОТОЦИКЛ  /  ЛЁГКИЙ  /  МАНЁВРЕННЫЙ"
            : chapter.VehicleType == StoryVehicleType.Truck
                ? "ГРУЗОВИК  /  ТЯЖЁЛЫЙ  /  БРОНИРОВАННЫЙ"
                : CarClasses[chapter.CarIndex];
        GUI.Label(new Rect(rect.x + 15f, rect.yMax - 26f, rect.width - 30f, 17f), vehicleClass, arcadeMicroStyle);
        GUI.color = Color.white;
    }

    private void DrawStoryCarPreview(Rect rect, int carIndex)
    {
        Sprite carSprite = GetTrackCarSprite(carIndex);
        if (carSprite == null || carSprite.texture == null)
        {
            DrawCarPreview(rect, carIndex);
            return;
        }

        Rect source = carSprite.textureRect;
        float sourceAspect = source.width / Mathf.Max(1f, source.height);
        float drawHeight = rect.height * 0.27f;
        float drawWidth = drawHeight * sourceAspect;
        float maxWidth = rect.width * 0.58f;
        if (drawWidth > maxWidth)
        {
            drawWidth = maxWidth;
            drawHeight = drawWidth / Mathf.Max(0.01f, sourceAspect);
        }

        Rect drawRect = new Rect(
            rect.center.x - drawWidth * 0.5f,
            rect.center.y - drawHeight * 0.5f,
            drawWidth,
            drawHeight);
        Rect uv = new Rect(
            source.x / carSprite.texture.width,
            source.y / carSprite.texture.height,
            source.width / carSprite.texture.width,
            source.height / carSprite.texture.height);
        Color previous = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTextureWithTexCoords(drawRect, carSprite.texture, uv, true);
        GUI.color = previous;
    }

    private void DrawStoryVehiclePreview(Rect rect, StoryVehicleType vehicleType)
    {
        Rect uv = vehicleType == StoryVehicleType.Motorcycle
            ? new Rect(0.09f, 0.02f, 0.23f, 0.96f)
            : new Rect(0.42f, 0.02f, 0.57f, 0.96f);
        float sourceAspect = (uv.width * storyVehicleSheet.width) / Mathf.Max(1f, uv.height * storyVehicleSheet.height);
        float maxWidth = rect.width * (vehicleType == StoryVehicleType.Motorcycle ? 0.62f : 0.9f);
        float maxHeight = rect.height * (vehicleType == StoryVehicleType.Motorcycle ? 0.24f : 0.93f);
        float drawHeight = maxHeight;
        float drawWidth = drawHeight * sourceAspect;
        if (drawWidth > maxWidth)
        {
            drawWidth = maxWidth;
            drawHeight = drawWidth / Mathf.Max(0.01f, sourceAspect);
        }

        Rect drawRect = new Rect(
            rect.center.x - drawWidth * 0.5f,
            rect.center.y - drawHeight * 0.5f,
            drawWidth,
            drawHeight);
        Color previous = GUI.color;
        GUI.color = Color.white;
        GUI.DrawTextureWithTexCoords(drawRect, storyVehicleSheet, uv, true);
        GUI.color = previous;
    }

    private void DrawArcadeStoryDialogue(float screenWidth, float screenHeight)
    {
        EnsureStoryStyles();
        bool hasArtwork = DrawRetroArtwork(screenWidth, screenHeight, 0.54f);
        if (!hasArtwork)
        {
            DrawArcadeBackdrop(screenWidth, screenHeight, false);
        }
        else
        {
            DrawArcadeMenuAmbientMotion(screenWidth, screenHeight);
        }

        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.001f, 0.004f, 0.018f, 0.48f));
        StoryChapterDefinition chapter = ActiveStoryChapter;
        StoryDialogueLine[] dialogue = ActiveStoryDialogue;
        int safeIndex = Mathf.Clamp(storyDialogueIndex, 0, Mathf.Max(0, dialogue.Length - 1));
        StoryDialogueLine line = dialogue[safeIndex];

        float margin = Mathf.Clamp(screenWidth * 0.045f, 28f, 82f);
        Rect panel = new Rect(margin, screenHeight * 0.12f, screenWidth - margin * 2f, screenHeight * 0.76f);
        DrawArcadePanel(panel, line.Accent, ArcadeCyan);

        Rect header = new Rect(panel.x + 18f, panel.y + 18f, panel.width - 36f, 72f);
        DrawSolidRect(header, new Color(0.003f, 0.014f, 0.045f, 0.98f));
        DrawSolidRect(new Rect(header.x, header.y, 7f, header.height), line.Accent);
        string transmissionTitle = storyDialogueIsDebrief ? "ПОСЛЕ ГОНКИ  //  РАЗВИТИЕ ИСТОРИИ" : "STORY TRANSMISSION";
        GUI.Label(new Rect(header.x + 22f, header.y + 6f, header.width - 44f, 34f), transmissionTitle + "  //  ГЛАВА " + (storySelectedChapter + 1).ToString("00") + "  //  " + chapter.Title, arcadeCompactHeadingStyle);
        GUI.Label(new Rect(header.x + 24f, header.y + 43f, header.width - 48f, 20f), (storyDialogueIsDebrief ? "ИТОГИ МИССИИ" : "СВЯЗЬ УСТАНОВЛЕНА") + "  //  " + (safeIndex + 1) + " / " + dialogue.Length, arcadeMicroStyle);

        float contentY = header.yMax + 18f;
        float contentHeight = panel.yMax - contentY - 112f;
        float portraitWidth = Mathf.Clamp(panel.width * 0.34f, 250f, 430f);
        Rect portraitPanel = new Rect(panel.x + 20f, contentY, portraitWidth, contentHeight);
        Rect textPanel = new Rect(portraitPanel.xMax + 18f, contentY, panel.xMax - portraitPanel.xMax - 38f, contentHeight);

        DrawSolidRect(portraitPanel, new Color(0.002f, 0.009f, 0.03f, 0.99f));
        DrawSolidRect(new Rect(portraitPanel.x, portraitPanel.y, portraitPanel.width, 4f), line.Accent);
        DrawSolidRect(new Rect(portraitPanel.x, portraitPanel.y, 4f, portraitPanel.height), line.Accent);
        if (storyCharacterSheet != null)
        {
            float portraitSize = Mathf.Min(portraitPanel.width - 20f, portraitPanel.height - 20f);
            Rect portraitRect = new Rect(portraitPanel.center.x - portraitSize * 0.5f, portraitPanel.yMax - portraitSize - 8f, portraitSize, portraitSize);
            float uvX = line.PortraitIndex % 2 == 0 ? 0f : 0.5f;
            float uvY = line.PortraitIndex < 2 ? 0.5f : 0f;
            GUI.DrawTextureWithTexCoords(portraitRect, storyCharacterSheet, new Rect(uvX, uvY, 0.5f, 0.5f), true);
        }

        DrawSolidRect(textPanel, new Color(0.003f, 0.012f, 0.037f, 0.985f));
        DrawSolidRect(new Rect(textPanel.x, textPanel.y, 7f, textPanel.height), line.Accent);
        DrawSolidRect(new Rect(textPanel.x, textPanel.y, textPanel.width, 4f), ArcadePink);
        Color previous = GUI.color;
        GUI.color = line.Accent;
        GUI.Label(new Rect(textPanel.x + 24f, textPanel.y + 22f, textPanel.width - 48f, 48f), line.Speaker, arcadeTitleStyle);
        GUI.color = previous;
        GUI.Label(new Rect(textPanel.x + 28f, textPanel.y + 72f, textPanel.width - 56f, 24f), line.Channel, arcadeSmallStyle);
        DrawSolidRect(new Rect(textPanel.x + 28f, textPanel.y + 106f, textPanel.width - 56f, 2f), new Color(line.Accent.r, line.Accent.g, line.Accent.b, 0.58f));
        GUI.Label(new Rect(textPanel.x + 30f, textPanel.y + 132f, textPanel.width - 60f, textPanel.height - 158f), line.Text, storyDialogueBodyStyle);

        float buttonY = panel.yMax - 82f;
        float backWidth = Mathf.Clamp(panel.width * 0.28f, 230f, 340f);
        string backTitle = storyDialogueIsDebrief ? "ПРОПУСТИТЬ" : "К ГЛАВАМ";
        if (DrawArcadeButton(new Rect(panel.x + 20f, buttonY, backWidth, 62f), "<", backTitle, "ESC", ArcadeYellow, false))
        {
            CancelActiveStoryDialogue();
        }

        string continueTitle = safeIndex + 1 < dialogue.Length
            ? "ДАЛЕЕ"
            : storyDialogueIsDebrief
                ? storySelectedChapter >= StoryChapters.Length - 1 ? "ЗАВЕРШИТЬ ИСТОРИЮ" : "СЛЕДУЮЩАЯ ГЛАВА"
                : "НАЧАТЬ МИССИЮ";
        if (DrawArcadeButton(new Rect(panel.x + 30f + backWidth, buttonY, panel.width - backWidth - 50f, 62f), ">", continueTitle, "ENTER  //  ПРОДОЛЖИТЬ", line.Accent, true))
        {
            AdvanceStoryDialogue();
        }

        for (int i = 0; i < dialogue.Length; i++)
        {
            float dotSize = i == safeIndex ? 15f : 9f;
            float dotX = panel.center.x + (i - (dialogue.Length - 1) * 0.5f) * 25f - dotSize * 0.5f;
            DrawSolidRect(new Rect(dotX, panel.yMax - 101f, dotSize, 5f), i <= safeIndex ? line.Accent : new Color(0.18f, 0.28f, 0.38f));
        }

        DrawArcadeMenuMotionOverlay(screenWidth, screenHeight);
    }

    private void DrawStoryTrackPreview(Rect rect, RaceTrackDefinition track, Color accent, StoryChapterDefinition chapter)
    {
        DrawSolidRect(rect, new Color(0.002f, 0.008f, 0.027f, 0.99f));
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 4f), accent);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 9f, rect.width - 24f, 21f), "TRACK  /  " + track.ShortName, arcadeSmallStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 29f, rect.width - 24f, 18f), "РЕЖИМ  /  " + chapter.ModeName + "   //   " + chapter.LapCount + " КРУГА   //   " + chapter.RivalCount + " СОПЕРНИКОВ", arcadeMicroStyle);
        Rect mapBounds = new Rect(rect.x + 12f, rect.y + 53f, rect.width - 24f, rect.height - 67f);
        if (minimapTrackTexture != null)
        {
            Rect mapRect = FitTrackPreviewTexture(mapBounds, minimapTrackTexture);
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(mapRect, minimapTrackTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;

            Vector2 start = MinimapPoint(PathPoint(0f, 0f), mapRect);
            DrawMinimapMarker(start, 11f, new Color(accent.r, accent.g, accent.b, 0.25f));
            DrawMinimapMarker(start, 5f, accent);
        }
    }

    private void DrawArcadeStoryObjectiveHud(float screenWidth, float screenHeight)
    {
        EnsureStoryStyles();
        StoryChapterDefinition chapter = ActiveStoryChapter;
        Color accent = ActiveTrack.AccentColor;
        Rect panel = new Rect(screenWidth * 0.5f - 315f, 18f, 630f, 112f);
        DrawArcadePanel(panel, accent, ArcadePink);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 10f, panel.width - 36f, 19f), "СЮЖЕТ  /  " + chapter.ModeName + "  /  ГЛАВА " + (storySelectedChapter + 1).ToString("00"), arcadeMicroStyle);
        Color previous = GUI.color;
        GUI.color = accent;
        GUI.Label(new Rect(panel.x + 18f, panel.y + 31f, panel.width - 36f, 43f), chapter.Objective, storyObjectiveStyle);
        GUI.color = previous;
        string status = "ПОЗИЦИЯ " + RacePosition() + " / " + (ActiveOpponentCount + 1) + "     КРУГ " + Mathf.Min(completedLaps + 1, RaceLapTarget) + " / " + RaceLapTarget;
        if (chapter.MissionType == StoryMissionType.Drift)
        {
            status = "ДРИФТ  " + Mathf.RoundToInt(storyDriftScore) + " / " + Mathf.RoundToInt(chapter.ObjectiveTarget) + "     КРУГ " + Mathf.Min(completedLaps + 1, RaceLapTarget) + " / " + RaceLapTarget;
        }
        else if (chapter.MissionType == StoryMissionType.Smash)
        {
            status = "РАЗРУШЕНО  " + storyObstacleSmashes + " / " + Mathf.RoundToInt(chapter.ObjectiveTarget) + "     КРУГ " + Mathf.Min(completedLaps + 1, RaceLapTarget) + " / " + RaceLapTarget;
        }
        if (chapter.TimeLimit > 0f)
        {
            float remaining = Mathf.Max(0f, chapter.TimeLimit - raceTime);
            status += "     ВРЕМЯ " + FormatTime(remaining);
        }
        GUI.Label(new Rect(panel.x + 18f, panel.y + 82f, panel.width - 36f, 18f), status, arcadeMicroStyle);
    }

    private void DrawArcadeStoryFinishOverlay(float screenWidth, float screenHeight)
    {
        EnsureStoryStyles();
        Color resultColor = storyMissionSucceeded ? ArcadeLime : ArcadePink;
        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.002f, 0.004f, 0.018f, 0.9f));
        Rect panel = new Rect(screenWidth * 0.5f - 440f, screenHeight * 0.5f - 285f, 880f, 570f);
        DrawArcadePanel(panel, resultColor, ArcadeCyan);
        GUI.Label(new Rect(panel.x + 28f, panel.y + 20f, panel.width - 56f, 24f), "STORY TRANSMISSION  //  CHAPTER " + (storySelectedChapter + 1).ToString("00"), arcadeSmallStyle);

        Color previous = GUI.color;
        GUI.color = resultColor;
        GUI.Label(new Rect(panel.x + 28f, panel.y + 49f, panel.width - 56f, 75f), storyMissionSucceeded ? "МИССИЯ ВЫПОЛНЕНА" : "МИССИЯ ПРОВАЛЕНА", arcadeTitleStyle);
        GUI.color = previous;
        GUIStyle resultMessageStyle = new GUIStyle(arcadeCenteredStyle)
        {
            wordWrap = true,
            clipping = TextClipping.Clip,
            fontSize = 13
        };
        GUI.Label(new Rect(panel.x + 30f, panel.y + 116f, panel.width - 60f, 45f), ActiveStoryChapter.Title + "  /  " + ActiveStoryChapter.ModeName + "  /  " + storyResultMessage, resultMessageStyle);

        float metricGap = 12f;
        float metricWidth = (panel.width - 56f - metricGap * 2f) / 3f;
        float metricY = panel.y + 170f;
        string firstMetricLabel = ActiveStoryChapter.MissionType == StoryMissionType.Drift
            ? "DRIFT SCORE"
            : ActiveStoryChapter.MissionType == StoryMissionType.Smash ? "DESTROYED" : "POSITION";
        string firstMetricValue = ActiveStoryChapter.MissionType == StoryMissionType.Drift
            ? Mathf.RoundToInt(storyDriftScore).ToString()
            : ActiveStoryChapter.MissionType == StoryMissionType.Smash
                ? storyObstacleSmashes + " / " + Mathf.RoundToInt(ActiveStoryChapter.ObjectiveTarget)
                : Mathf.Max(1, storyFinishPosition) + " / " + (ActiveOpponentCount + 1);
        DrawArcadeMetric(new Rect(panel.x + 28f, metricY, metricWidth, 104f), firstMetricLabel, firstMetricValue, ArcadeCyan);
        DrawArcadeMetric(new Rect(panel.x + 28f + metricWidth + metricGap, metricY, metricWidth, 104f), "TOTAL TIME", FormatTime(finishTime), resultColor);
        DrawArcadeMetric(new Rect(panel.x + 28f + (metricWidth + metricGap) * 2f, metricY, metricWidth, 104f), "STORY BONUS", storyEarnedReward + " COINS", ArcadeYellow);

        string primaryTitle = storyMissionSucceeded
            ? storySelectedChapter < StoryChapters.Length - 1 ? "ПРОДОЛЖИТЬ ИСТОРИЮ" : "ЭПИЛОГ"
            : "ПОВТОРИТЬ";
        string primarySubtitle = storyMissionSucceeded ? "AFTER-RACE TRANSMISSION" : "RESTART MISSION";
        if (DrawArcadeButton(new Rect(panel.x + 28f, panel.y + 308f, 400f, 92f), "01", primaryTitle, primarySubtitle, resultColor, true))
        {
            if (storyMissionSucceeded)
            {
                StartNextStoryChapter();
            }
            else
            {
                RestartRace();
            }
        }

        if (DrawArcadeButton(new Rect(panel.x + 452f, panel.y + 308f, 400f, 92f), "02", "ГАРАЖ", "UPGRADE VEHICLE", ArcadeCyan, false))
        {
            garageOpen = true;
            garageCarIndex = selectedCarIndex;
            Time.timeScale = 0f;
        }

        if (DrawArcadeButton(new Rect(panel.x + 240f, panel.y + 430f, 400f, 76f), "<", "ВЫБОР ГЛАВ", "ESC  /  STORY ARCHIVE", ArcadeYellow, false))
        {
            OpenMainMenu();
        }

        GUI.Label(new Rect(panel.x + 28f, panel.y + 526f, panel.width - 56f, 20f), storyMissionSucceeded ? "PROGRESS SAVED  //  NIGHT LEAGUE SIGNAL RESTORED" : ActiveStoryChapter.Objective, arcadeCenteredStyle);
    }
}
