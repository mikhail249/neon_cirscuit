using UnityEngine;

public sealed partial class NeonCircuitGame
{
    private static readonly Color ArcadeInk = new Color(0.006f, 0.01f, 0.038f, 1f);
    private static readonly Color ArcadePanel = new Color(0.01f, 0.035f, 0.075f, 0.97f);
    private static readonly Color ArcadeCyan = new Color(0.04f, 0.92f, 1f, 1f);
    private static readonly Color ArcadePink = new Color(1f, 0.035f, 0.42f, 1f);
    private static readonly Color ArcadeLime = new Color(0.18f, 1f, 0.28f, 1f);
    private static readonly Color ArcadeYellow = new Color(1f, 0.78f, 0.04f, 1f);
    private static readonly Color ArcadeOrange = new Color(1f, 0.25f, 0.035f, 1f);
    private static readonly Color ArcadeViolet = new Color(0.58f, 0.32f, 1f, 1f);

    private GUIStyle arcadeLogoStyle;
    private GUIStyle arcadeTitleStyle;
    private GUIStyle arcadeHeadingStyle;
    private GUIStyle arcadeCompactHeadingStyle;
    private GUIStyle arcadeLabelStyle;
    private GUIStyle arcadeSmallStyle;
    private GUIStyle arcadeMicroStyle;
    private GUIStyle arcadeNumberStyle;
    private GUIStyle arcadeCenteredStyle;
    private Texture2D retroArcadeMenuTexture;
    private Texture2D garageWorkshopTexture;
    private Texture2D[] garageCarSprites;
    private Texture2D[] paintedGarageCarSprites;
    private int[] paintedGarageColorIndices;
    private bool mainMenuModeSelectionOpen;
    private int mainMenuSelectedMode;

    private bool DrawArcadeGui(float screenWidth, float screenHeight)
    {
        EnsureArcadeStyles();

        if (trackLoadPending)
        {
            DrawArcadeTrackLoading(screenWidth, screenHeight);
            return true;
        }

        if (mainMenuOpen)
        {
            if (storyModeOpen)
            {
                if (storyDialogueOpen)
                {
                    DrawArcadeStoryDialogue(screenWidth, screenHeight);
                }
                else
                {
                    DrawArcadeStoryMenu(screenWidth, screenHeight);
                }
            }
            else if (garageOpen)
            {
                DrawArcadeGarage(screenWidth, screenHeight);
            }
            else if (mainMenuModeSelectionOpen)
            {
                DrawArcadeModeSelection(screenWidth, screenHeight);
            }
            else
            {
                DrawArcadeMainMenu(screenWidth, screenHeight);
            }

            return true;
        }

        if (garageOpen)
        {
            DrawArcadeGarage(screenWidth, screenHeight);
            return true;
        }

        if (tutorialActive)
        {
            DrawDrivingTutorialGui(screenWidth, screenHeight);
            return true;
        }

        DrawArcadeWeather(screenWidth, screenHeight);
        DrawArcadeRaceHud(screenWidth, screenHeight);
        DrawArcadeRaceEffects(screenWidth, screenHeight);
        if (playerWrecked)
        {
            DrawArcadeWreckedOverlay(screenWidth, screenHeight);
        }
        else if (raceFinished)
        {
            DrawArcadeFinishOverlay(screenWidth, screenHeight);
        }
        else if (!raceStarted)
        {
            DrawArcadeCountdown(screenWidth, screenHeight, countdown > 3f ? "READY" : Mathf.CeilToInt(countdown).ToString(), ArcadeYellow);
        }
        else if (raceTime < 1.15f)
        {
            DrawArcadeCountdown(screenWidth, screenHeight, "GO!", ArcadeLime);
        }

        return true;
    }

    private void EnsureArcadeStyles()
    {
        if (retroArcadeMenuTexture == null)
        {
            retroArcadeMenuTexture = Resources.Load<Texture2D>("UI/RetroArcadeMenu");
        }

        if (garageWorkshopTexture == null)
        {
            garageWorkshopTexture = Resources.Load<Texture2D>("UI/GarageWorkshopBackground");
        }

        if (garageCarSprites == null || garageCarSprites.Length != CarNames.Length)
        {
            garageCarSprites = new[]
            {
                Resources.Load<Texture2D>("UI/GarageCarSprite"),
                Resources.Load<Texture2D>("UI/GarageCarVoltS"),
                Resources.Load<Texture2D>("UI/GarageCarDriftRX"),
                Resources.Load<Texture2D>("UI/GarageCarTitanGT"),
                Resources.Load<Texture2D>("UI/GarageCarPhantomX"),
                Resources.Load<Texture2D>("UI/GarageCarRaptor4X"),
                Resources.Load<Texture2D>("UI/GarageCarBlazeRS"),
                Resources.Load<Texture2D>("UI/GarageCarNovaLM"),
                Resources.Load<Texture2D>("UI/GarageCarZenithQ"),
                Resources.Load<Texture2D>("UI/GarageCarSignalGhost"),
                Resources.Load<Texture2D>("UI/GarageCarMagmaRam"),
                Resources.Load<Texture2D>("UI/GarageCarTempestXR"),
                Resources.Load<Texture2D>("UI/GarageCarIkarusZero")
            };
        }

        if (arcadeLogoStyle != null && arcadeCompactHeadingStyle != null)
        {
            arcadeCompactHeadingStyle.fontSize = 21;
            arcadeCompactHeadingStyle.normal.textColor = Color.white;
            return;
        }

        arcadeLogoStyle = new GUIStyle(GUI.skin.label);
        arcadeLogoStyle.fontSize = 76;
        arcadeLogoStyle.fontStyle = FontStyle.Bold;
        arcadeLogoStyle.alignment = TextAnchor.MiddleLeft;
        arcadeLogoStyle.normal.textColor = Color.white;

        arcadeTitleStyle = new GUIStyle(arcadeLogoStyle);
        arcadeTitleStyle.fontSize = 56;
        arcadeTitleStyle.alignment = TextAnchor.MiddleCenter;

        arcadeHeadingStyle = new GUIStyle(arcadeLogoStyle);
        arcadeHeadingStyle.fontSize = 29;

        arcadeCompactHeadingStyle = new GUIStyle(arcadeHeadingStyle);
        arcadeCompactHeadingStyle.fontSize = 21;
        arcadeCompactHeadingStyle.normal.textColor = Color.white;

        arcadeLabelStyle = new GUIStyle(GUI.skin.label);
        arcadeLabelStyle.fontSize = 21;
        arcadeLabelStyle.fontStyle = FontStyle.Bold;
        arcadeLabelStyle.normal.textColor = Color.white;

        arcadeSmallStyle = new GUIStyle(arcadeLabelStyle);
        arcadeSmallStyle.fontSize = 15;
        arcadeSmallStyle.normal.textColor = new Color(0.62f, 0.92f, 1f);

        arcadeMicroStyle = new GUIStyle(arcadeSmallStyle);
        arcadeMicroStyle.fontSize = 11;
        arcadeMicroStyle.normal.textColor = new Color(0.42f, 0.74f, 0.84f);

        arcadeNumberStyle = new GUIStyle(arcadeLogoStyle);
        arcadeNumberStyle.fontSize = 36;
        arcadeNumberStyle.alignment = TextAnchor.MiddleCenter;
        arcadeNumberStyle.normal.textColor = Color.white;

        arcadeCenteredStyle = new GUIStyle(arcadeSmallStyle);
        arcadeCenteredStyle.alignment = TextAnchor.MiddleCenter;
    }

    private Rect RetroRect(Rect sourceRect, float screenWidth, float screenHeight)
    {
        const float referenceWidth = 1680f;
        const float referenceHeight = 945f;
        float scale = Mathf.Max(screenWidth / referenceWidth, screenHeight / referenceHeight);
        float offsetX = (screenWidth - referenceWidth * scale) * 0.5f;
        float offsetY = (screenHeight - referenceHeight * scale) * 0.5f;
        return new Rect(
            offsetX + sourceRect.x * scale,
            offsetY + sourceRect.y * scale,
            sourceRect.width * scale,
            sourceRect.height * scale);
    }

    private bool DrawRetroArtwork(float screenWidth, float screenHeight, float dimAmount)
    {
        if (retroArcadeMenuTexture == null)
        {
            return false;
        }

        float time = Time.unscaledTime;
        float brightness = 0.965f + Mathf.Sin(time * 1.35f) * 0.035f;
        Color previousGuiColor = GUI.color;
        GUI.color = new Color(brightness, brightness, 1f, 1f);
        GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), retroArcadeMenuTexture, ScaleMode.ScaleAndCrop, false);
        GUI.color = previousGuiColor;
        if (dimAmount > 0f)
        {
            DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.001f, 0.004f, 0.02f, Mathf.Clamp01(dimAmount)));
        }

        for (float y = 5f; y < screenHeight; y += 7f)
        {
            DrawSolidRect(new Rect(0f, y, screenWidth, 1f), new Color(1f, 1f, 1f, dimAmount > 0f ? 0.012f : 0.022f));
        }

        float sweepY = Mathf.Repeat(time * 118f, screenHeight + 160f) - 80f;
        float sweepAlpha = dimAmount > 0f ? 0.045f : 0.095f;
        DrawSolidRect(new Rect(0f, sweepY - 6f, screenWidth, 13f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, sweepAlpha * 0.25f));
        DrawSolidRect(new Rect(0f, sweepY, screenWidth, 2f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, sweepAlpha));

        float glitch = Mathf.Pow(Mathf.Clamp01(Mathf.Sin(time * 1.73f)), 20f);
        if (glitch > 0.025f)
        {
            float glitchY = Mathf.Repeat(time * 263f, screenHeight - 30f);
            DrawSolidRect(new Rect(0f, glitchY, screenWidth, 3f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, glitch * 0.12f));
            DrawSolidRect(new Rect(screenWidth * 0.18f, glitchY + 7f, screenWidth * 0.64f, 2f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, glitch * 0.16f));
        }

        return true;
    }

    private void DrawArcadeBackdrop(float screenWidth, float screenHeight, bool showMenuArtwork = true)
    {
        if (showMenuArtwork && DrawRetroArtwork(screenWidth, screenHeight, 0.78f))
        {
            DrawSolidRect(new Rect(0f, 0f, screenWidth, 5f), ArcadePink);
            DrawSolidRect(new Rect(0f, screenHeight - 5f, screenWidth, 5f), ArcadeCyan);
            return;
        }

        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), ArcadeInk);
        DrawSolidRect(new Rect(0f, screenHeight * 0.52f, screenWidth, screenHeight * 0.48f), new Color(0.022f, 0.003f, 0.045f, 1f));

        float horizon = screenHeight * 0.57f;
        float time = Time.unscaledTime;
        float buildingSpacing = (screenWidth + 100f) / 29f;
        float skylineOffset = Mathf.Repeat(time * 5f, buildingSpacing);
        for (int i = 0; i < 30; i++)
        {
            float width = 38f + i % 5 * 10f;
            float x = i * buildingSpacing - 44f - skylineOffset;
            float height = 70f + Mathf.Abs(Mathf.Sin(i * 1.37f)) * 190f;
            Color building = i % 2 == 0 ? new Color(0.012f, 0.025f, 0.07f) : new Color(0.02f, 0.012f, 0.062f);
            DrawSolidRect(new Rect(x, horizon - height, width, height), building);

            for (int floor = 0; floor < 7; floor++)
            {
                float windowY = horizon - 18f - floor * 24f;
                if ((i + floor) % 3 == 0 && windowY > horizon - height + 8f)
                {
                    Color light = i % 3 == 0 ? ArcadePink : i % 3 == 1 ? ArcadeCyan : ArcadeYellow;
                    DrawSolidRect(new Rect(x + 8f, windowY, Mathf.Max(5f, width - 17f), 4f), new Color(light.r, light.g, light.b, 0.64f));
                }
            }
        }

        DrawSolidRect(new Rect(0f, horizon, screenWidth, 4f), ArcadePink);
        for (int row = 1; row < 10; row++)
        {
            float t = row / 9f;
            float y = Mathf.Lerp(horizon, screenHeight, t * t);
            DrawSolidRect(new Rect(0f, y, screenWidth, row == 9 ? 3f : 1f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.18f));
        }

        for (int lane = -8; lane <= 8; lane++)
        {
            float endX = screenWidth * 0.5f + lane * screenWidth * 0.085f;
            float length = Vector2.Distance(new Vector2(screenWidth * 0.5f, horizon), new Vector2(endX, screenHeight));
            float angle = Mathf.Atan2(endX - screenWidth * 0.5f, screenHeight - horizon) * Mathf.Rad2Deg;
            DrawRotatedRect(new Rect(screenWidth * 0.5f, horizon, 1f, length), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.11f), -angle);
        }

        for (float y = 6f; y < screenHeight; y += 9f)
        {
            DrawSolidRect(new Rect(0f, y, screenWidth, 1f), new Color(1f, 1f, 1f, 0.014f));
        }

        float sweepY = Mathf.Repeat(time * 92f, screenHeight + 120f) - 60f;
        DrawSolidRect(new Rect(0f, sweepY - 5f, screenWidth, 11f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.025f));
        DrawSolidRect(new Rect(0f, sweepY, screenWidth, 2f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.09f));

        DrawSolidRect(new Rect(0f, 0f, screenWidth, 5f), ArcadePink);
        DrawSolidRect(new Rect(0f, screenHeight - 5f, screenWidth, 5f), ArcadeCyan);
    }

    private void DrawGarageWorkshopBackdrop(float screenWidth, float screenHeight)
    {
        if (garageWorkshopTexture == null)
        {
            DrawArcadeBackdrop(screenWidth, screenHeight, false);
            return;
        }

        Color previous = GUI.color;
        float pulse = 0.97f + Mathf.Sin(Time.unscaledTime * 1.35f) * 0.025f;
        GUI.color = new Color(pulse, pulse, 1f, 1f);
        GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), garageWorkshopTexture, ScaleMode.ScaleAndCrop, false);
        GUI.color = previous;

        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.001f, 0.004f, 0.018f, 0.26f));
        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight * 0.15f), new Color(0.001f, 0.003f, 0.014f, 0.56f));
        DrawSolidRect(new Rect(0f, screenHeight * 0.69f, screenWidth, screenHeight * 0.31f), new Color(0.001f, 0.003f, 0.014f, 0.2f));

        for (float y = 5f; y < screenHeight; y += 8f)
        {
            DrawSolidRect(new Rect(0f, y, screenWidth, 1f), new Color(1f, 1f, 1f, 0.016f));
        }

        float sweepY = Mathf.Repeat(Time.unscaledTime * 84f, screenHeight + 140f) - 70f;
        DrawSolidRect(new Rect(0f, sweepY - 5f, screenWidth, 11f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.025f));
        DrawSolidRect(new Rect(0f, sweepY, screenWidth, 2f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.09f));
        DrawSolidRect(new Rect(0f, 0f, screenWidth, 4f), ArcadePink);
        DrawSolidRect(new Rect(0f, screenHeight - 4f, screenWidth, 4f), ArcadeCyan);
    }

    private void DrawArcadePanel(Rect rect, Color accent, Color secondary, bool drawTopBorder = true, bool drawInnerTopLine = true)
    {
        DrawSolidRect(new Rect(rect.x + 11f, rect.y + 13f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.72f));
        DrawSolidRect(new Rect(rect.x - 6f, rect.y - 6f, rect.width + 12f, rect.height + 12f), new Color(accent.r, accent.g, accent.b, 0.2f));
        DrawSolidRect(rect, new Color(0.003f, 0.014f, 0.043f, 0.985f));
        DrawSolidRect(new Rect(rect.x + 7f, rect.y + 7f, rect.width - 14f, rect.height - 14f), new Color(0.012f, 0.036f, 0.075f, 0.96f));
        if (drawTopBorder)
        {
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 5f), accent);
        }
        DrawSolidRect(new Rect(rect.x, rect.y + rect.height - 5f, rect.width, 5f), secondary);
        DrawSolidRect(new Rect(rect.x, rect.y, 5f, rect.height), accent);
        DrawSolidRect(new Rect(rect.x + rect.width - 5f, rect.y, 5f, rect.height), secondary);
        if (drawInnerTopLine)
        {
            DrawSolidRect(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, 2f), new Color(accent.r, accent.g, accent.b, 0.35f));
        }
        DrawSolidRect(new Rect(rect.x, rect.y, 18f, 18f), ArcadeInk);
        DrawSolidRect(new Rect(rect.x + rect.width - 18f, rect.y + rect.height - 18f, 18f, 18f), ArcadeInk);
        DrawSolidRect(new Rect(rect.x + 5f, rect.y + 16f, 4f, rect.height - 32f), new Color(accent.r, accent.g, accent.b, 0.34f));
    }

    private bool DrawRetroHotspot(Rect sourceRect, float screenWidth, float screenHeight, Color accent, bool primary)
    {
        Rect rect = RetroRect(sourceRect, screenWidth, screenHeight);
        bool hovered = rect.Contains(Event.current.mousePosition);
        int audioId = Mathf.RoundToInt(sourceRect.x * 13f + sourceRect.y * 31f);
        RegisterMenuHover(audioId, hovered);
        if (hovered || primary)
        {
            float pulse = primary ? 0.12f + Mathf.Sin(Time.unscaledTime * 3.4f) * 0.045f : 0.11f;
            DrawSolidRect(rect, new Color(accent.r, accent.g, accent.b, hovered ? 0.18f : pulse));
            float edge = Mathf.Max(2f, rect.height * 0.018f);
            DrawSolidRect(new Rect(rect.x, rect.y, rect.width, edge), new Color(accent.r, accent.g, accent.b, hovered ? 1f : 0.72f));
            DrawSolidRect(new Rect(rect.x, rect.y + rect.height - edge, rect.width, edge), new Color(accent.r, accent.g, accent.b, hovered ? 1f : 0.72f));
            DrawSolidRect(new Rect(rect.x, rect.y, edge, rect.height), new Color(accent.r, accent.g, accent.b, hovered ? 1f : 0.72f));

            float scan = Mathf.Repeat(Time.unscaledTime * (hovered ? 1.05f : 0.52f), 1f);
            float scanX = Mathf.Lerp(rect.x + edge, rect.xMax - edge, scan);
            DrawSolidRect(new Rect(scanX - 18f, rect.y + edge, 36f, rect.height - edge * 2f), new Color(accent.r, accent.g, accent.b, hovered ? 0.2f : 0.11f));
            DrawSolidRect(new Rect(scanX, rect.y + edge, Mathf.Max(3f, edge * 0.8f), rect.height - edge * 2f), new Color(accent.r, accent.g, accent.b, hovered ? 0.66f : 0.42f));

            if (primary)
            {
                float runner = Mathf.Repeat(Time.unscaledTime * 0.7f, 1f);
                float runnerWidth = Mathf.Max(32f, rect.width * 0.16f);
                float runnerX = Mathf.Lerp(rect.x, rect.xMax - runnerWidth, runner);
                DrawSolidRect(new Rect(runnerX, rect.yMax - edge * 1.5f, runnerWidth, edge * 1.5f), new Color(1f, 1f, 1f, 0.72f));
            }
        }

        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        if (clicked)
        {
            PlayMenuClickSfx();
        }
        return clicked;
    }

    private bool DrawArcadeButton(Rect rect, string number, string title, string subtitle, Color accent, bool selected)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        int audioId = Mathf.RoundToInt(rect.x * 17f + rect.y * 29f + rect.width * 3f);
        RegisterMenuHover(audioId, hovered);
        bool highlighted = selected || hovered;
        bool compact = rect.width < 300f;
        bool compactTitle = compact || (!string.IsNullOrEmpty(title) && title.Length > 14);
        float numberDivider = compact ? 72f : 78f;
        float titleX = compact ? 84f : 98f;
        float arrowReserve = compact ? 38f : 58f;
        Color fill = highlighted ? new Color(accent.r * 0.24f, accent.g * 0.24f, accent.b * 0.24f, 0.98f) : new Color(0.012f, 0.028f, 0.062f, 0.98f);

        DrawSolidRect(new Rect(rect.x + 7f, rect.y + 8f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.62f));
        DrawSolidRect(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), new Color(accent.r, accent.g, accent.b, highlighted ? 0.26f : 0.1f));
        DrawSolidRect(rect, fill);
        DrawSolidRect(new Rect(rect.x, rect.y, highlighted ? 7f : 3f, rect.height), accent);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 3f), accent);
        if (number != "<")
        {
            DrawSolidRect(new Rect(rect.x + numberDivider, rect.y + 12f, 2f, rect.height - 24f), new Color(accent.r, accent.g, accent.b, 0.52f));
        }

        Color previous = GUI.color;
        GUI.color = accent;
        GUI.Label(new Rect(rect.x + 9f, rect.y + 8f, numberDivider - 15f, rect.height - 16f), number, compact ? arcadeCompactHeadingStyle : arcadeNumberStyle);
        GUI.color = Color.white;
        float titleY = rect.y + (number == "<" ? 4f : 10f);
        GUI.Label(new Rect(rect.x + titleX, titleY, rect.width - titleX - arrowReserve, 34f), title, compactTitle ? arcadeCompactHeadingStyle : arcadeHeadingStyle);
        GUI.color = previous;

        if (!string.IsNullOrEmpty(subtitle))
        {
            GUI.Label(new Rect(rect.x + titleX + 2f, rect.y + rect.height - 28f, rect.width - titleX - arrowReserve, 18f), subtitle, arcadeMicroStyle);
        }

        GUI.Label(new Rect(rect.x + rect.width - (compact ? 30f : 48f), rect.y + (rect.height - 30f) * 0.5f, compact ? 24f : 34f, 30f), highlighted ? (compact ? ">" : ">>>") : ">", arcadeLabelStyle);
        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        if (clicked)
        {
            PlayMenuClickSfx();
        }
        return clicked;
    }

    private void DrawArcadeMenuMotionOverlay(float screenWidth, float screenHeight)
    {
        float intro = MenuIntroProgress(0f, 0.9f);
        if (intro < 0.999f)
        {
            DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0f, 0f, 0.012f, (1f - intro) * 0.96f));
        }

        float beamX = Mathf.Repeat(Time.unscaledTime * 145f, screenWidth + 420f) - 210f;
        DrawRotatedRect(
            new Rect(beamX, -screenHeight * 0.18f, 2f, screenHeight * 1.42f),
            new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.055f),
            -13f);

        float pulse = 0.45f + Mathf.Sin(Time.unscaledTime * 2.6f) * 0.25f;
        DrawSolidRect(new Rect(0f, 0f, screenWidth * pulse, 3f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.72f));
        DrawSolidRect(new Rect(screenWidth * (1f - pulse), screenHeight - 3f, screenWidth * pulse, 3f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.72f));
    }

    private void DrawArcadeMenuAmbientMotion(float screenWidth, float screenHeight)
    {
        float time = Time.unscaledTime;

        // Bright traffic trails across the central city are deliberately large enough
        // to remain visible when the Game view is scaled down inside the editor.
        for (int i = 0; i < 9; i++)
        {
            float phase = Mathf.Repeat(time * (0.2f + i % 3 * 0.035f) + i * 0.137f, 1f);
            float sourceX = Mathf.Lerp(500f, 1170f, phase);
            float sourceY = 245f + i % 6 * 54f + Mathf.Sin(time * 1.2f + i) * 8f;
            float sourceWidth = 34f + i % 4 * 18f;
            Color trail = i % 3 == 0 ? ArcadePink : i % 3 == 1 ? ArcadeCyan : ArcadeYellow;
            float alpha = Mathf.Sin(phase * Mathf.PI);
            Rect glowRect = RetroRect(new Rect(sourceX - sourceWidth, sourceY - 3f, sourceWidth, 9f), screenWidth, screenHeight);
            Rect coreRect = RetroRect(new Rect(sourceX - sourceWidth * 0.72f, sourceY, sourceWidth * 0.72f, 3f), screenWidth, screenHeight);
            DrawSolidRect(glowRect, new Color(trail.r, trail.g, trail.b, alpha * 0.12f));
            DrawSolidRect(coreRect, new Color(trail.r, trail.g, trail.b, alpha * 0.62f));
        }

        for (int i = 0; i < 7; i++)
        {
            float phase = Mathf.Repeat(time * (0.34f + i * 0.012f) + i * 0.19f, 1f);
            float sourceX = Mathf.Lerp(530f, 1160f, phase);
            float sourceY = 730f + i * 22f;
            Color spark = i % 2 == 0 ? ArcadeCyan : ArcadePink;
            Rect sparkRect = RetroRect(new Rect(sourceX, sourceY, 10f + phase * 38f, 4f + phase * 3f), screenWidth, screenHeight);
            DrawSolidRect(sparkRect, new Color(spark.r, spark.g, spark.b, Mathf.Sin(phase * Mathf.PI) * 0.54f));
        }

        float logoScan = Mathf.Repeat(time * 0.32f, 1f);
        float logoY = Mathf.Lerp(68f, 268f, logoScan);
        Rect logoGlow = RetroRect(new Rect(78f, logoY - 5f, 512f, 13f), screenWidth, screenHeight);
        Rect logoCore = RetroRect(new Rect(78f, logoY, 512f, 3f), screenWidth, screenHeight);
        DrawSolidRect(logoGlow, new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.08f));
        DrawSolidRect(logoCore, new Color(1f, 1f, 1f, 0.24f));

        float signPulse = 0.45f + Mathf.Sin(time * 5.4f) * 0.34f;
        DrawSolidRect(RetroRect(new Rect(769f, 407f, 70f, 8f), screenWidth, screenHeight), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, signPulse));
        DrawSolidRect(RetroRect(new Rect(905f, 281f, 8f, 52f), screenWidth, screenHeight), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, signPulse * 0.78f));
    }

    private void DrawArcadeMainMenu(float screenWidth, float screenHeight)
    {
        if (!DrawRetroArtwork(screenWidth, screenHeight, 0f))
        {
            DrawArcadeBackdrop(screenWidth, screenHeight);
            float fallbackX = (screenWidth - 500f) * 0.5f;
            if (DrawArcadeButton(new Rect(fallbackX, 220f, 500f, 88f), "01", "START", "ВЫБЕРИТЕ РЕЖИМ", ArcadePink, true)) OpenMainMenuModeSelection();
            if (DrawArcadeButton(new Rect(fallbackX, 326f, 500f, 88f), "02", "ГАРАЖ", "МАШИНЫ / ТЮНИНГ", ArcadeCyan, false)) garageOpen = true;
            if (DrawArcadeButton(new Rect(fallbackX, 432f, 500f, 88f), "03", "ВЫХОД", string.Empty, ArcadeYellow, false)) QuitGame();
            DrawTutorialMenuEntry(new Rect(fallbackX, 538f, 500f, 88f));
            DrawArcadeMenuMotionOverlay(screenWidth, screenHeight);
            return;
        }

        DrawArcadeMenuAmbientMotion(screenWidth, screenHeight);
        DrawMainMenuPlayerCar(screenWidth, screenHeight);

        Rect freeModeTag = RetroRect(new Rect(78f, 279f, 486f, 24f), screenWidth, screenHeight);
        DrawSolidRect(freeModeTag, new Color(0.03f, 0.002f, 0.025f, 0.88f));
        GUI.Label(freeModeTag, "СВОБОДНАЯ ГОНКА  //  ВЫБОР ТРАССЫ И МАШИНЫ", arcadeMicroStyle);

        if (DrawRetroHotspot(new Rect(78f, 308f, 486f, 130f), screenWidth, screenHeight, ArcadePink, true))
        {
            OpenMainMenuModeSelection();
        }

        if (DrawRetroHotspot(new Rect(78f, 459f, 486f, 122f), screenWidth, screenHeight, ArcadeCyan, false))
        {
            garageOpen = true;
            garageCarIndex = selectedCarIndex;
        }

        if (DrawRetroHotspot(new Rect(78f, 605f, 486f, 124f), screenWidth, screenHeight, ArcadeYellow, false))
        {
            QuitGame();
        }

        Rect trackHeader = RetroRect(new Rect(1190f, 137f, 394f, 54f), screenWidth, screenHeight);
        DrawSolidRect(trackHeader, new Color(0.006f, 0.02f, 0.055f, 1f));
        Color previous = GUI.color;
        GUI.color = ActiveTrack.AccentColor;
        GUI.Label(new Rect(trackHeader.x + 10f, trackHeader.y + 4f, trackHeader.width - 20f, trackHeader.height - 8f), "TRACK: " + ActiveTrack.ShortName, arcadeHeadingStyle);
        GUI.color = previous;

        DrawMainMenuTrackPreview(screenWidth, screenHeight);

        Rect trackFooter = RetroRect(new Rect(1190f, 532f, 398f, 73f), screenWidth, screenHeight);
        DrawSolidRect(trackFooter, new Color(0.004f, 0.016f, 0.046f, 1f));
        DrawSolidRect(new Rect(trackFooter.x, trackFooter.y, trackFooter.width, Mathf.Max(2f, trackFooter.height * 0.04f)), ActiveTrack.AccentColor);
        float arrowWidth = trackFooter.width * 0.18f;
        Rect previousTrack = new Rect(trackFooter.x + 5f, trackFooter.y + 6f, arrowWidth, trackFooter.height - 12f);
        Rect nextTrack = new Rect(trackFooter.x + trackFooter.width - arrowWidth - 5f, trackFooter.y + 6f, arrowWidth, trackFooter.height - 12f);
        GUI.Label(previousTrack, "<", arcadeTitleStyle);
        GUI.Label(nextTrack, ">", arcadeTitleStyle);
        GUI.Label(new Rect(previousTrack.xMax, trackFooter.y + 5f, trackFooter.width - arrowWidth * 2f - 10f, trackFooter.height * 0.52f), "3 LAPS", arcadeHeadingStyle);
        GUI.Label(new Rect(previousTrack.xMax, trackFooter.y + trackFooter.height * 0.52f, trackFooter.width - arrowWidth * 2f - 10f, trackFooter.height * 0.38f), (selectedTrackIndex + 1).ToString("00") + " / " + RaceTrackCatalog.Count.ToString("00"), arcadeCenteredStyle);
        if (GUI.Button(previousTrack, GUIContent.none, GUIStyle.none)) SelectTrack((selectedTrackIndex + RaceTrackCatalog.Count - 1) % RaceTrackCatalog.Count);
        if (GUI.Button(nextTrack, GUIContent.none, GUIStyle.none)) SelectTrack((selectedTrackIndex + 1) % RaceTrackCatalog.Count);
        if (GUI.Button(RetroRect(new Rect(1170f, 118f, 446f, 408f), screenWidth, screenHeight), GUIContent.none, GUIStyle.none))
        {
            SelectTrack((selectedTrackIndex + 1) % RaceTrackCatalog.Count);
        }

        DrawMainMenuWeatherToggle(screenWidth, screenHeight);

        DrawTutorialMenuEntry(RetroRect(new Rect(78f, 756f, 486f, 88f), screenWidth, screenHeight));

        Rect coinValue = RetroRect(new Rect(1360f, 710f, 205f, 88f), screenWidth, screenHeight);
        DrawSolidRect(coinValue, new Color(0.045f, 0.035f, 0.002f, 1f));
        GUI.color = ArcadeYellow;
        GUI.Label(coinValue, coins.ToString("000"), arcadeTitleStyle);
        GUI.color = previous;

        float coinPulse = 0.45f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.28f;
        float coinEdge = Mathf.Max(2f, coinValue.height * 0.035f);
        DrawSolidRect(new Rect(coinValue.x, coinValue.y, coinValue.width, coinEdge), new Color(ArcadeYellow.r, ArcadeYellow.g, ArcadeYellow.b, coinPulse));
        DrawSolidRect(new Rect(coinValue.x, coinValue.yMax - coinEdge, coinValue.width, coinEdge), new Color(ArcadeYellow.r, ArcadeYellow.g, ArcadeYellow.b, coinPulse));

        DrawArcadeMenuMotionOverlay(screenWidth, screenHeight);
    }

    private void OpenMainMenuModeSelection()
    {
        mainMenuModeSelectionOpen = true;
        mainMenuSelectedMode = 0;
        garageOpen = false;
        menuAnimationStartedAt = Time.unscaledTime;
    }

    private void CloseMainMenuModeSelection()
    {
        mainMenuModeSelectionOpen = false;
        menuAnimationStartedAt = Time.unscaledTime;
    }

    private void ActivateMainMenuMode(int modeIndex)
    {
        mainMenuSelectedMode = Mathf.Clamp(modeIndex, 0, 4);
        mainMenuModeSelectionOpen = false;
        if (mainMenuSelectedMode == 0)
        {
            OpenStoryMode();
        }
        else if (mainMenuSelectedMode == 1)
        {
            LaunchArcadeRaceMode(ArcadeRaceMode.Standard);
        }
        else if (mainMenuSelectedMode == 2)
        {
            LaunchArcadeRaceMode(ArcadeRaceMode.DriftChallenge);
        }
        else if (mainMenuSelectedMode == 3)
        {
            LaunchArcadeRaceMode(ArcadeRaceMode.HeavyTruck);
        }
        else
        {
            LaunchArcadeRaceMode(ArcadeRaceMode.Motorcycle);
        }
    }

    private void DrawArcadeModeSelection(float screenWidth, float screenHeight)
    {
        bool hasArtwork = DrawRetroArtwork(screenWidth, screenHeight, 0f);
        if (!hasArtwork)
        {
            DrawArcadeBackdrop(screenWidth, screenHeight, false);
            float fallbackX = (screenWidth - 620f) * 0.5f;
            GUI.Label(new Rect(fallbackX, 95f, 620f, 66f), "ВЫБЕРИТЕ РЕЖИМ", arcadeTitleStyle);
            if (DrawArcadeButton(new Rect(fallbackX, 175f, 620f, 72f), "01", "КАМПАНИЯ", "СЮЖЕТ / 8 ГЛАВ", ArcadeLime, mainMenuSelectedMode == 0)) ActivateMainMenuMode(0);
            if (DrawArcadeButton(new Rect(fallbackX, 257f, 620f, 72f), "02", "СВОБОДНАЯ ГОНКА", "ТРАССА / МАШИНА / ПОГОДА", ArcadePink, mainMenuSelectedMode == 1)) ActivateMainMenuMode(1);
            if (DrawArcadeButton(new Rect(fallbackX, 339f, 620f, 72f), "03", "ДРИФТ-ИСПЫТАНИЕ", "3000 ОЧКОВ / DRIFT RX", ArcadeCyan, mainMenuSelectedMode == 2)) ActivateMainMenuMode(2);
            if (DrawArcadeButton(new Rect(fallbackX, 421f, 620f, 72f), "04", "ТЯЖЁЛЫЕ ГРУЗОВИКИ", "TITAN HAULER / ТЯЖЁЛАЯ ФИЗИКА", ArcadeYellow, mainMenuSelectedMode == 3)) ActivateMainMenuMode(3);
            if (DrawArcadeButton(new Rect(fallbackX, 503f, 620f, 72f), "05", "МОТОГОНКА", "VOLT BIKE X / ЛЁГКАЯ ФИЗИКА", ArcadeViolet, mainMenuSelectedMode == 4)) ActivateMainMenuMode(4);
            if (DrawArcadeButton(new Rect(fallbackX, 590f, 620f, 64f), "<", "НАЗАД", "ESC", ArcadeCyan, false)) CloseMainMenuModeSelection();
            DrawArcadeMenuMotionOverlay(screenWidth, screenHeight);
            return;
        }

        DrawArcadeMenuAmbientMotion(screenWidth, screenHeight);
        DrawMainMenuPlayerCar(screenWidth, screenHeight);

        Rect selectorPanel = RetroRect(new Rect(54f, 280f, 536f, 604f), screenWidth, screenHeight);
        DrawSolidRect(new Rect(selectorPanel.x + 10f, selectorPanel.y + 12f, selectorPanel.width, selectorPanel.height), new Color(0f, 0f, 0f, 0.72f));
        DrawSolidRect(selectorPanel, new Color(0.003f, 0.012f, 0.038f, 0.975f));
        DrawSolidRect(new Rect(selectorPanel.x, selectorPanel.y, selectorPanel.width, Mathf.Max(3f, selectorPanel.height * 0.007f)), ArcadePink);
        DrawSolidRect(new Rect(selectorPanel.x, selectorPanel.y, Mathf.Max(4f, selectorPanel.width * 0.009f), selectorPanel.height), ArcadeCyan);
        DrawSolidRect(new Rect(selectorPanel.xMax - Mathf.Max(3f, selectorPanel.width * 0.007f), selectorPanel.y, Mathf.Max(3f, selectorPanel.width * 0.007f), selectorPanel.height), ArcadePink);

        Rect header = RetroRect(new Rect(72f, 296f, 500f, 66f), screenWidth, screenHeight);
        DrawSolidRect(header, new Color(0.025f, 0.003f, 0.038f, 0.98f));
        DrawSolidRect(new Rect(header.x, header.yMax - 3f, header.width, 3f), ArcadePink);
        GUI.Label(new Rect(header.x + 12f, header.y + 3f, header.width - 24f, header.height * 0.62f), "ВЫБЕРИТЕ РЕЖИМ", arcadeHeadingStyle);
        GUI.Label(new Rect(header.x + 14f, header.y + header.height * 0.61f, header.width - 28f, header.height * 0.3f), "START  //  NIGHT LEAGUE", arcadeMicroStyle);

        Rect campaignButton = RetroRect(new Rect(78f, 376f, 486f, 75f), screenWidth, screenHeight);
        Rect freeRaceButton = RetroRect(new Rect(78f, 458f, 486f, 75f), screenWidth, screenHeight);
        Rect driftButton = RetroRect(new Rect(78f, 540f, 486f, 75f), screenWidth, screenHeight);
        Rect truckButton = RetroRect(new Rect(78f, 622f, 486f, 75f), screenWidth, screenHeight);
        Rect motorcycleButton = RetroRect(new Rect(78f, 704f, 486f, 75f), screenWidth, screenHeight);
        Rect backButton = RetroRect(new Rect(78f, 786f, 486f, 52f), screenWidth, screenHeight);
        if (DrawMainMenuModeButton(campaignButton, 0, "01", "КАМПАНИЯ", "СЮЖЕТ  /  8 ГЛАВ  /  НАГРАДЫ", ArcadeLime))
        {
            ActivateMainMenuMode(0);
        }
        else if (DrawMainMenuModeButton(freeRaceButton, 1, "02", "СВОБОДНАЯ ГОНКА", "ТРАССА  /  МАШИНА  /  ПОГОДА", ArcadePink))
        {
            ActivateMainMenuMode(1);
        }
        else if (DrawMainMenuModeButton(driftButton, 2, "03", "ДРИФТ-ИСПЫТАНИЕ", "3000 ОЧКОВ  /  DRIFT RX", ArcadeCyan))
        {
            ActivateMainMenuMode(2);
        }
        else if (DrawMainMenuModeButton(truckButton, 3, "04", "ТЯЖЁЛЫЕ ГРУЗОВИКИ", "TITAN HAULER  /  ТЯЖЁЛАЯ ФИЗИКА", ArcadeYellow))
        {
            ActivateMainMenuMode(3);
        }
        else if (DrawMainMenuModeButton(motorcycleButton, 4, "05", "МОТОГОНКА", "VOLT BIKE X  /  ЛЁГКАЯ ФИЗИКА", ArcadeViolet))
        {
            ActivateMainMenuMode(4);
        }
        else if (DrawMainMenuModeButton(backButton, -1, "<", "НАЗАД", "ESC  /  ГЛАВНОЕ МЕНЮ", ArcadeYellow))
        {
            CloseMainMenuModeSelection();
        }

        Rect selectorStatus = RetroRect(new Rect(78f, 837f, 486f, 30f), screenWidth, screenHeight);
        Color statusAccent = mainMenuSelectedMode == 0 ? ArcadeLime
            : mainMenuSelectedMode == 2 ? ArcadeCyan
            : mainMenuSelectedMode == 3 ? ArcadeYellow
            : mainMenuSelectedMode == 4 ? ArcadeViolet
            : ArcadePink;
        DrawSolidRect(selectorStatus, new Color(0.004f, 0.02f, 0.045f, 0.98f));
        DrawSolidRect(new Rect(selectorStatus.x, selectorStatus.y, 5f, selectorStatus.height), statusAccent);
        string status = mainMenuSelectedMode == 0
            ? "КАМПАНИЯ  " + CompletedStoryChapterCount() + " / " + StoryChapters.Length
            : mainMenuSelectedMode == 2 ? "DRIFT RX  //  ЦЕЛЬ 3000"
            : mainMenuSelectedMode == 3 ? "TITAN HAULER  //  2700 KG"
            : mainMenuSelectedMode == 4 ? "VOLT BIKE X  //  620 KG"
            : ActiveTrack.ShortName + "  //  " + CarNames[selectedCarIndex];
        GUI.Label(new Rect(selectorStatus.x + 14f, selectorStatus.y + 2f, selectorStatus.width - 28f, selectorStatus.height - 4f), status, arcadeMicroStyle);

        DrawModeSelectionTrackPanel(screenWidth, screenHeight);
        DrawMainMenuWeatherToggle(screenWidth, screenHeight);
        DrawModeSelectionCoins(screenWidth, screenHeight);
        DrawArcadeMenuMotionOverlay(screenWidth, screenHeight);
    }

    private bool DrawMainMenuModeButton(Rect rect, int modeIndex, string number, string title, string subtitle, Color accent, bool selectedOverride = false)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        bool selected = selectedOverride || (modeIndex >= 0 && mainMenuSelectedMode == modeIndex);
        bool highlighted = hovered || selected;
        RegisterMenuHover(8300 + modeIndex, hovered);

        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 4.8f + modeIndex) * 0.5f;
        Color fill = highlighted
            ? new Color(accent.r * 0.19f, accent.g * 0.19f, accent.b * 0.19f, 0.99f)
            : new Color(0.007f, 0.018f, 0.045f, 0.99f);
        DrawSolidRect(new Rect(rect.x + 7f, rect.y + 8f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.72f));
        DrawSolidRect(new Rect(rect.x - 4f, rect.y - 4f, rect.width + 8f, rect.height + 8f), new Color(accent.r, accent.g, accent.b, highlighted ? 0.2f + pulse * 0.12f : 0.08f));
        DrawSolidRect(rect, fill);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, Mathf.Max(3f, rect.height * 0.035f)), accent);
        DrawSolidRect(new Rect(rect.x, rect.yMax - Mathf.Max(3f, rect.height * 0.035f), rect.width, Mathf.Max(3f, rect.height * 0.035f)), accent);
        DrawSolidRect(new Rect(rect.x, rect.y, highlighted ? 8f : 4f, rect.height), accent);

        float numberWidth = Mathf.Min(105f, rect.width * 0.22f);
        Rect numberRect = new Rect(rect.x + 8f, rect.y + 8f, numberWidth - 8f, rect.height - 16f);
        DrawSolidRect(numberRect, new Color(accent.r * 0.16f, accent.g * 0.16f, accent.b * 0.16f, 0.92f));
        if (number != "<")
        {
            DrawSolidRect(new Rect(rect.x + numberWidth, rect.y + 12f, 2f, rect.height - 24f), new Color(accent.r, accent.g, accent.b, 0.62f));
        }

        Color previous = GUI.color;
        GUI.color = accent;
        GUI.Label(numberRect, number, arcadeTitleStyle);
        GUI.color = Color.white;
        float titleX = rect.x + numberWidth + 22f;
        float titleY = rect.y + rect.height * 0.16f - (number == "<" ? 6f : 0f);
        GUI.Label(new Rect(titleX, titleY, rect.width - numberWidth - 78f, rect.height * 0.42f), title, arcadeHeadingStyle);
        GUI.color = previous;
        GUI.Label(new Rect(titleX + 2f, rect.y + rect.height * 0.61f, rect.width - numberWidth - 84f, rect.height * 0.2f), subtitle, arcadeMicroStyle);
        GUI.Label(new Rect(rect.xMax - 54f, rect.y + (rect.height - 34f) * 0.5f, 42f, 34f), highlighted ? ">>>" : ">", arcadeLabelStyle);

        float runnerX = Mathf.Lerp(rect.x + 10f, rect.xMax - 70f, Mathf.Repeat(Time.unscaledTime * (highlighted ? 0.72f : 0.34f) + modeIndex * 0.18f, 1f));
        DrawSolidRect(new Rect(runnerX, rect.yMax - 6f, 60f, 4f), new Color(1f, 1f, 1f, highlighted ? 0.82f : 0.32f));

        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        if (clicked)
        {
            PlayMenuClickSfx();
        }
        return clicked;
    }

    private void DrawModeSelectionTrackPanel(float screenWidth, float screenHeight)
    {
        Rect trackHeader = RetroRect(new Rect(1190f, 137f, 394f, 54f), screenWidth, screenHeight);
        DrawSolidRect(trackHeader, new Color(0.006f, 0.02f, 0.055f, 1f));
        Color previous = GUI.color;
        Color accent = mainMenuSelectedMode == 0 ? ArcadeLime
            : mainMenuSelectedMode == 2 ? ArcadeCyan
            : mainMenuSelectedMode == 3 ? ArcadeYellow
            : mainMenuSelectedMode == 4 ? ArcadeViolet
            : ActiveTrack.AccentColor;
        GUI.color = accent;
        string headerText = mainMenuSelectedMode == 0 ? "STORY: 8 ГЛАВ"
            : mainMenuSelectedMode == 2 ? "MODE: DRIFT"
            : mainMenuSelectedMode == 3 ? "MODE: HEAVY"
            : mainMenuSelectedMode == 4 ? "MODE: MOTO"
            : "TRACK: " + ActiveTrack.ShortName;
        GUI.Label(new Rect(trackHeader.x + 10f, trackHeader.y + 4f, trackHeader.width - 20f, trackHeader.height - 8f), headerText, arcadeHeadingStyle);
        GUI.color = previous;

        DrawMainMenuTrackPreview(screenWidth, screenHeight);

        Rect trackFooter = RetroRect(new Rect(1190f, 532f, 398f, 73f), screenWidth, screenHeight);
        DrawSolidRect(trackFooter, new Color(0.004f, 0.016f, 0.046f, 1f));
        DrawSolidRect(new Rect(trackFooter.x, trackFooter.y, trackFooter.width, Mathf.Max(2f, trackFooter.height * 0.04f)), accent);
        string mainText = mainMenuSelectedMode == 0 ? "КАМПАНИЯ"
            : mainMenuSelectedMode == 2 ? "3000 DRIFT"
            : mainMenuSelectedMode == 3 ? "HEAVY LEAGUE"
            : mainMenuSelectedMode == 4 ? "MOTO LEAGUE"
            : "3 LAPS";
        string subText = mainMenuSelectedMode == 0
            ? CompletedStoryChapterCount() + " / " + StoryChapters.Length + " ЗАВЕРШЕНО"
            : mainMenuSelectedMode == 2 ? "DRIFT RX  /  3 LAPS"
            : mainMenuSelectedMode == 3 ? "TITAN HAULER  /  3 LAPS"
            : mainMenuSelectedMode == 4 ? "VOLT BIKE X  /  3 LAPS"
            : (selectedTrackIndex + 1).ToString("00") + " / " + RaceTrackCatalog.Count.ToString("00");
        GUI.Label(new Rect(trackFooter.x + 18f, trackFooter.y + 4f, trackFooter.width - 36f, trackFooter.height * 0.54f), mainText, arcadeHeadingStyle);
        GUI.Label(new Rect(trackFooter.x + 18f, trackFooter.y + trackFooter.height * 0.52f, trackFooter.width - 36f, trackFooter.height * 0.38f), subText, arcadeCenteredStyle);
    }

    private void DrawModeSelectionCoins(float screenWidth, float screenHeight)
    {
        Rect coinValue = RetroRect(new Rect(1360f, 710f, 205f, 88f), screenWidth, screenHeight);
        DrawSolidRect(coinValue, new Color(0.045f, 0.035f, 0.002f, 1f));
        Color previous = GUI.color;
        GUI.color = ArcadeYellow;
        GUI.Label(coinValue, coins.ToString("000"), arcadeTitleStyle);
        GUI.color = previous;

        float pulse = 0.45f + Mathf.Sin(Time.unscaledTime * 4.2f) * 0.28f;
        float edge = Mathf.Max(2f, coinValue.height * 0.035f);
        DrawSolidRect(new Rect(coinValue.x, coinValue.y, coinValue.width, edge), new Color(ArcadeYellow.r, ArcadeYellow.g, ArcadeYellow.b, pulse));
        DrawSolidRect(new Rect(coinValue.x, coinValue.yMax - edge, coinValue.width, edge), new Color(ArcadeYellow.r, ArcadeYellow.g, ArcadeYellow.b, pulse));
    }

    private void DrawMainMenuWeatherToggle(float screenWidth, float screenHeight)
    {
        Rect button = RetroRect(new Rect(1190f, 54f, 398f, 74f), screenWidth, screenHeight);
        Color accent = weatherEnabled ? WeatherColor : ArcadePink;
        string stateCode = weatherEnabled ? "ON" : "OFF";
        string stateText = weatherEnabled ? "ПОГОДА ВКЛ" : "ПОГОДА ВЫКЛ";
        string actionText = weatherEnabled
            ? WeatherName + "  /  ОТКЛЮЧИТЬ"
            : "СУХАЯ ТРАССА  /  ВКЛЮЧИТЬ";

        if (DrawMainMenuModeButton(button, -2, stateCode, stateText, actionText, accent, weatherEnabled))
        {
            ToggleWeather();
        }
    }

    private void DrawMainMenuTrackPreview(float screenWidth, float screenHeight)
    {
        float intro = MenuIntroProgress(0.18f, 0.72f);
        float slideX = (1f - intro) * 95f;
        Rect previewPanel = RetroRect(new Rect(1194f + slideX, 199f, 386f, 315f), screenWidth, screenHeight);
        Color accent = ActiveTrack.AccentColor;
        DrawSolidRect(previewPanel, new Color(0.003f, 0.009f, 0.027f, 0.97f));
        DrawSolidRect(new Rect(previewPanel.x, previewPanel.y, previewPanel.width, Mathf.Max(2f, previewPanel.height * 0.012f)), accent);
        DrawSolidRect(new Rect(previewPanel.x, previewPanel.yMax - Mathf.Max(2f, previewPanel.height * 0.012f), previewPanel.width, Mathf.Max(2f, previewPanel.height * 0.012f)), accent);

        for (int column = 1; column < 8; column++)
        {
            float x = previewPanel.x + previewPanel.width * column / 8f;
            DrawSolidRect(new Rect(x, previewPanel.y + 8f, 1f, previewPanel.height - 16f), new Color(accent.r, accent.g, accent.b, 0.055f));
        }

        for (int row = 1; row < 6; row++)
        {
            float y = previewPanel.y + previewPanel.height * row / 6f;
            DrawSolidRect(new Rect(previewPanel.x + 8f, y, previewPanel.width - 16f, 1f), new Color(accent.r, accent.g, accent.b, 0.055f));
        }

        float scanY = Mathf.Lerp(previewPanel.y + 8f, previewPanel.yMax - 8f, Mathf.Repeat(Time.unscaledTime * 0.22f, 1f));
        DrawSolidRect(new Rect(previewPanel.x + 8f, scanY - 4f, previewPanel.width - 16f, 9f), new Color(accent.r, accent.g, accent.b, 0.035f));
        DrawSolidRect(new Rect(previewPanel.x + 8f, scanY, previewPanel.width - 16f, 2f), new Color(accent.r, accent.g, accent.b, 0.24f));

        Rect mapBounds = new Rect(previewPanel.x + 16f, previewPanel.y + 15f, previewPanel.width - 32f, previewPanel.height - 70f);
        Rect mapRect = mapBounds;
        if (minimapTrackTexture != null)
        {
            mapRect = FitTrackPreviewTexture(mapBounds, minimapTrackTexture);
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(mapRect, minimapTrackTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        Vector2 startPoint = MinimapPoint(PathPoint(0f, 0f), mapRect);
        float pulse = Mathf.Max(9f, mapRect.height * 0.06f) + Mathf.Sin(Time.unscaledTime * 4f) * 1.4f;
        DrawMinimapMarker(startPoint, pulse, new Color(accent.r, accent.g, accent.b, 0.24f));
        DrawMinimapMarker(startPoint, Mathf.Max(5f, pulse * 0.48f), Color.white);
        DrawMinimapMarker(startPoint, Mathf.Max(3f, pulse * 0.28f), accent);

        DrawWeatherPreview(previewPanel);

        Rect description = new Rect(previewPanel.x + 15f, previewPanel.yMax - 47f, previewPanel.width - 30f, 31f);
        DrawSolidRect(description, new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 0.94f));
        DrawSolidRect(new Rect(description.x, description.y, 4f, description.height), accent);
        GUI.Label(new Rect(description.x + 11f, description.y + 6f, description.width - 20f, description.height - 10f), ActiveTrack.Description, arcadeMicroStyle);

        DrawSolidRect(new Rect(previewPanel.x, previewPanel.y, 16f, 2f), Color.white);
        DrawSolidRect(new Rect(previewPanel.x, previewPanel.y, 2f, 16f), Color.white);
        DrawSolidRect(new Rect(previewPanel.xMax - 16f, previewPanel.yMax - 2f, 16f, 2f), Color.white);
        DrawSolidRect(new Rect(previewPanel.xMax - 2f, previewPanel.yMax - 16f, 2f, 16f), Color.white);
    }

    private void DrawArcadeTrackLoading(float screenWidth, float screenHeight)
    {
        DrawArcadeBackdrop(screenWidth, screenHeight);
        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.002f, 0.004f, 0.018f, 0.84f));

        float panelWidth = Mathf.Min(610f, screenWidth - 48f);
        Rect panel = new Rect((screenWidth - panelWidth) * 0.5f, screenHeight * 0.5f - 115f, panelWidth, 230f);
        Color accent = ActiveTrack.AccentColor;
        DrawArcadePanel(panel, accent, ArcadePink);
        GUI.Label(new Rect(panel.x + 28f, panel.y + 24f, panel.width - 56f, 38f), "ЗАГРУЗКА ТРАССЫ", arcadeHeadingStyle);

        Color previous = GUI.color;
        GUI.color = accent;
        GUI.Label(new Rect(panel.x + 28f, panel.y + 66f, panel.width - 56f, 42f), ActiveTrack.ShortName, arcadeTitleStyle);
        GUI.color = previous;

        Rect progress = new Rect(panel.x + 32f, panel.y + 132f, panel.width - 64f, 18f);
        DrawSolidRect(progress, new Color(0f, 0f, 0f, 0.76f));
        float runnerWidth = Mathf.Max(74f, progress.width * 0.22f);
        float runnerX = Mathf.Lerp(progress.x, progress.xMax - runnerWidth, Mathf.PingPong(Time.unscaledTime * 0.8f, 1f));
        DrawSolidRect(new Rect(runnerX, progress.y + 3f, runnerWidth, progress.height - 6f), accent);
        GUI.Label(new Rect(panel.x + 32f, panel.y + 169f, panel.width - 64f, 24f), "BUILDING CIRCUIT  //  PLEASE WAIT", arcadeMicroStyle);
    }

    private static Rect FitTrackPreviewTexture(Rect bounds, Texture2D texture)
    {
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            return bounds;
        }

        float width = bounds.width;
        float height = width * texture.height / texture.width;
        if (height > bounds.height)
        {
            height = bounds.height;
            width = height * texture.width / texture.height;
        }

        return new Rect(bounds.center.x - width * 0.5f, bounds.center.y - height * 0.5f, width, height);
    }

    private void DrawMainMenuPlayerCar(float screenWidth, float screenHeight)
    {
        Sprite specialVehicle = MainMenuSpecialVehiclePreviewSprite;
        Texture2D carSprite = specialVehicle != null
            ? specialVehicle.texture
            : GetPaintedGarageCarSprite(MainMenuModePreviewCarIndex);
        if (carSprite == null)
        {
            return;
        }

        float previewWidth = specialVehicle != null ? specialVehicle.rect.width : carSprite.width;
        float previewHeight = specialVehicle != null ? specialVehicle.rect.height : carSprite.height;
        Rect textureCoordinates = specialVehicle != null
            ? new Rect(
                specialVehicle.rect.x / carSprite.width,
                specialVehicle.rect.y / carSprite.height,
                specialVehicle.rect.width / carSprite.width,
                specialVehicle.rect.height / carSprite.height)
            : GetGarageCarTextureCoordinates(MainMenuModePreviewCarIndex);

        float time = Time.unscaledTime;
        float intro = MenuIntroProgress(0.08f, 0.78f);
        float entranceOffset = (1f - intro) * 150f;
        float bob = Mathf.Sin(time * 1.85f) * 13f;
        float sway = Mathf.Sin(time * 0.72f) * 10f;
        float tilt = Mathf.Sin(time * 1.15f) * 0.85f;
        float scalePulse = (Mathf.Sin(time * 1.85f) + 1f) * 4f;
        float glowPulse = 0.82f + Mathf.Sin(time * 2.5f) * 0.18f;
        Color previous = GUI.color;

        GUI.color = new Color(0.004f, 0.012f + glowPulse * 0.006f, 0.035f + glowPulse * 0.015f, 1f);
        GUI.DrawTexture(RetroRect(new Rect(590f, 555f, 550f, 120f), screenWidth, screenHeight), circleTexture, ScaleMode.StretchToFill, true);

        float platformRunner = Mathf.Repeat(time * 0.48f, 1f);
        DrawSolidRect(
            RetroRect(new Rect(600f + platformRunner * 480f, 646f, 54f, 7f), screenWidth, screenHeight),
            new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.78f));
        DrawSolidRect(
            RetroRect(new Rect(1080f - platformRunner * 480f, 659f, 42f, 5f), screenWidth, screenHeight),
            new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.68f));

        for (int i = 0; i < 5; i++)
        {
            float exhaustPhase = Mathf.Repeat(time * (0.72f + i * 0.045f) + i * 0.17f, 1f);
            float exhaustLength = 28f + exhaustPhase * 85f;
            float exhaustX = 592f + sway - exhaustLength;
            float exhaustY = 542f + bob + i * 7f;
            Color exhaustColor = i % 2 == 0 ? ArcadeCyan : ArcadePink;
            DrawSolidRect(
                RetroRect(new Rect(exhaustX, exhaustY, exhaustLength, 4f + exhaustPhase * 3f), screenWidth, screenHeight),
                new Color(exhaustColor.r, exhaustColor.g, exhaustColor.b, Mathf.Sin(exhaustPhase * Mathf.PI) * 0.52f));
        }

        GUI.color = new Color(0.006f, 0.018f, 0.05f, 1f);
        Rect shadowBounds = RetroRect(new Rect(565f + sway * 0.35f, 435f + entranceOffset * 0.72f + bob * 0.35f, 575f, 200f), screenWidth, screenHeight);
        GUI.DrawTextureWithTexCoords(FitGarageCarTexture(shadowBounds, previewWidth, previewHeight), carSprite, textureCoordinates, true);

        GUI.color = Color.white;
        Rect carBounds = RetroRect(new Rect(580f + sway - scalePulse * 0.5f, 455f + entranceOffset + bob - scalePulse * 0.22f, 540f + scalePulse, 181f + scalePulse * 0.44f), screenWidth, screenHeight);
        Rect fittedCar = FitGarageCarTexture(carBounds, previewWidth, previewHeight);
        Matrix4x4 previousMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(tilt, fittedCar.center);
        GUI.DrawTextureWithTexCoords(fittedCar, carSprite, textureCoordinates, true);
        GUI.matrix = previousMatrix;

        GUI.color = previous;
    }

    private Texture2D GetGarageCarSprite(int carIndex)
    {
        if (garageCarSprites == null || garageCarSprites.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Clamp(carIndex, 0, garageCarSprites.Length - 1);
        return garageCarSprites[safeIndex] != null ? garageCarSprites[safeIndex] : garageCarSprites[0];
    }

    private static Rect GetGarageCarTextureCoordinates(int carIndex)
    {
        return carIndex >= 9
            ? new Rect(1f, 0f, -1f, 1f)
            : new Rect(0f, 0f, 1f, 1f);
    }

    private Texture2D GetPaintedGarageCarSprite(int carIndex)
    {
        Texture2D source = GetGarageCarSprite(carIndex);
        if (source == null || !source.isReadable)
        {
            return source;
        }

        int safeIndex = Mathf.Clamp(carIndex, 0, garageCarSprites.Length - 1);
        if (paintedGarageCarSprites == null || paintedGarageCarSprites.Length != garageCarSprites.Length)
        {
            ReleasePaintedGarageCarPreviews();
            paintedGarageCarSprites = new Texture2D[garageCarSprites.Length];
            paintedGarageColorIndices = new int[garageCarSprites.Length];
            for (int i = 0; i < paintedGarageColorIndices.Length; i++)
            {
                paintedGarageColorIndices[i] = -1;
            }
        }

        if (paintedGarageCarSprites[safeIndex] != null && paintedGarageColorIndices[safeIndex] == paintColorIndex)
        {
            return paintedGarageCarSprites[safeIndex];
        }

        if (paintedGarageCarSprites[safeIndex] != null)
        {
            Destroy(paintedGarageCarSprites[safeIndex]);
        }

        Color paint = PaintColors[paintColorIndex];
        Color32[] pixels = source.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color original = pixels[i];
            if (original.a <= 0.01f)
            {
                continue;
            }

            float brightest = Mathf.Max(original.r, Mathf.Max(original.g, original.b));
            float cyanStrength = Mathf.Min(original.g, original.b) - original.r;
            float hueMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.045f, 0.18f, cyanStrength));
            float brightnessMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.16f, 0.48f, brightest));
            float bodyMask = hueMask * brightnessMask;
            if (bodyMask <= 0.001f)
            {
                continue;
            }

            float shade = Mathf.Lerp(0.5f, 1.08f, brightest);
            Color painted = new Color(paint.r * shade, paint.g * shade, paint.b * shade, original.a);
            float highlight = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.76f, 1f, brightest)) * 0.14f;
            painted = Color.Lerp(painted, new Color(1f, 1f, 1f, original.a), highlight);
            Color result = Color.Lerp(original, painted, bodyMask);
            result.a = original.a;
            pixels[i] = result;
        }

        Texture2D paintedTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
        paintedTexture.name = source.name + " Paint " + paintColorIndex;
        paintedTexture.filterMode = FilterMode.Point;
        paintedTexture.wrapMode = TextureWrapMode.Clamp;
        paintedTexture.SetPixels32(pixels);
        paintedTexture.Apply(false, true);
        paintedGarageCarSprites[safeIndex] = paintedTexture;
        paintedGarageColorIndices[safeIndex] = paintColorIndex;
        return paintedTexture;
    }

    private void ReleasePaintedGarageCarPreviews()
    {
        if (paintedGarageCarSprites != null)
        {
            for (int i = 0; i < paintedGarageCarSprites.Length; i++)
            {
                if (paintedGarageCarSprites[i] != null)
                {
                    Destroy(paintedGarageCarSprites[i]);
                }
            }
        }

        paintedGarageCarSprites = null;
        paintedGarageColorIndices = null;
    }

    private static Rect FitGarageCarTexture(Rect bounds, Texture2D texture)
    {
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            return bounds;
        }

        return FitGarageCarTexture(bounds, texture.width, texture.height);
    }

    private static Rect FitGarageCarTexture(Rect bounds, float textureWidth, float textureHeight)
    {
        if (textureWidth <= 0f || textureHeight <= 0f)
        {
            return bounds;
        }

        float width = bounds.width;
        float height = width * textureHeight / textureWidth;
        if (height > bounds.height)
        {
            height = bounds.height;
            width = height * textureWidth / textureHeight;
        }

        return new Rect(bounds.center.x - width * 0.5f, bounds.center.y - height * 0.5f, width, height);
    }

    private bool DrawArcadeTrackChoice(Rect rect, int trackIndex)
    {
        RaceTrackDefinition track = RaceTrackCatalog.Get(trackIndex);
        bool selected = trackIndex == selectedTrackIndex;
        bool hovered = rect.Contains(Event.current.mousePosition);
        Color accent = track.AccentColor;
        Color fill = selected || hovered ? new Color(accent.r * 0.2f, accent.g * 0.2f, accent.b * 0.2f, 0.98f) : new Color(0.01f, 0.026f, 0.06f, 0.98f);
        DrawSolidRect(rect, fill);
        DrawSolidRect(new Rect(rect.x, rect.y, selected ? 7f : 3f, rect.height), accent);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 2f), accent);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 5f, 42f, 24f), (trackIndex + 1).ToString("00"), arcadeLabelStyle);
        GUI.Label(new Rect(rect.x + 61f, rect.y + 5f, rect.width - 86f, 22f), track.ShortName, arcadeSmallStyle);
        GUI.Label(new Rect(rect.x + 63f, rect.y + 27f, rect.width - 88f, 17f), selected ? "SELECTED" : track.Description, arcadeMicroStyle);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void DrawArcadeCarPreview(Rect rect, int carIndex)
    {
        DrawSolidRect(rect, new Color(0.002f, 0.008f, 0.026f, 0.28f));
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.64f));
        DrawSolidRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.48f));
        for (int x = 1; x < 12; x++) DrawSolidRect(new Rect(rect.x + rect.width * x / 12f, rect.y, 1f, rect.height), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.045f));
        for (int y = 1; y < 7; y++) DrawSolidRect(new Rect(rect.x, rect.y + rect.height * y / 7f, rect.width, 1f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.04f));

        Color neon = NeonColors[neonColorIndex];
        float centerX = rect.center.x;
        float bob = Mathf.Sin(Time.unscaledTime * 1.8f) * Mathf.Min(5f, rect.height * 0.018f);
        float centerY = rect.center.y + rect.height * 0.07f + bob;

        Color previous = GUI.color;
        float glowPulse = 0.13f + Mathf.Sin(Time.unscaledTime * 2.4f) * 0.025f;
        GUI.color = new Color(neon.r, neon.g, neon.b, glowPulse);
        GUI.DrawTexture(new Rect(rect.x + rect.width * 0.07f, rect.y + rect.height * 0.2f, rect.width * 0.86f, rect.height * 0.64f), circleTexture);
        GUI.color = new Color(neon.r, neon.g, neon.b, 0.16f);
        GUI.DrawTexture(new Rect(rect.x + rect.width * 0.16f, rect.y + rect.height * 0.69f, rect.width * 0.68f, rect.height * 0.15f), circleTexture);
        GUI.color = previous;

        Texture2D carSprite = GetPaintedGarageCarSprite(carIndex);
        if (carSprite != null)
        {
            float spriteWidth = rect.width * 0.9f;
            float spriteHeight = spriteWidth * carSprite.height / carSprite.width;
            if (spriteHeight > rect.height * 0.74f)
            {
                spriteHeight = rect.height * 0.74f;
                spriteWidth = spriteHeight * carSprite.width / carSprite.height;
            }

            Rect spriteRect = new Rect(centerX - spriteWidth * 0.5f, centerY - spriteHeight * 0.5f, spriteWidth, spriteHeight);
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(spriteRect, carSprite, GetGarageCarTextureCoordinates(carIndex), true);
            GUI.color = previous;
        }

        GUI.Label(new Rect(rect.x + 14f, rect.y + 10f, 210f, 18f), "WORKSHOP VEHICLE FEED", arcadeMicroStyle);
        GUI.Label(new Rect(rect.x + rect.width - 116f, rect.y + rect.height - 25f, 102f, 18f), "SYNC 100%", arcadeMicroStyle);
    }

    private void DrawArcadeWheel(Vector2 center, float size, Color accent)
    {
        Color previous = GUI.color;
        GUI.color = new Color(0.002f, 0.004f, 0.012f, 1f);
        GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), circleTexture);
        GUI.color = accent;
        GUI.DrawTexture(new Rect(center.x - size * 0.31f, center.y - size * 0.31f, size * 0.62f, size * 0.62f), circleTexture);
        GUI.color = new Color(0.02f, 0.03f, 0.06f, 1f);
        GUI.DrawTexture(new Rect(center.x - size * 0.16f, center.y - size * 0.16f, size * 0.32f, size * 0.32f), circleTexture);
        GUI.color = previous;
    }

    private void DrawArcadeStatStrip(Rect rect, int carIndex)
    {
        float gap = 8f;
        float width = (rect.width - gap * 2f) / 3f;
        string[] labels = { "SPEED", "HANDLING", "ARMOR" };
        float[] values = { CarTopSpeed[carIndex], CarHandling[carIndex], 2f - CarDamage[carIndex] };
        Color[] colors = { ArcadeCyan, ArcadeLime, ArcadePink };
        for (int i = 0; i < 3; i++)
        {
            Rect item = new Rect(rect.x + i * (width + gap), rect.y, width, rect.height);
            DrawSolidRect(item, new Color(0.004f, 0.018f, 0.046f, 0.96f));
            DrawSolidRect(new Rect(item.x, item.y, 3f, item.height), colors[i]);
            GUI.Label(new Rect(item.x + 9f, item.y + 5f, item.width - 62f, 18f), labels[i], arcadeMicroStyle);
            GUI.Label(new Rect(item.x + item.width - 50f, item.y + 4f, 43f, 20f), Mathf.RoundToInt(values[i] * 100f).ToString(), arcadeSmallStyle);
            float statRatio = values[i] / 1.2f;
            DrawArcadeSegments(new Rect(item.x + 9f, item.yMax - 17f, item.width - 18f, 9f), statRatio, colors[i], 8);
        }
    }

    private void DrawGarageCarCarousel(Rect rect)
    {
        DrawSolidRect(rect, new Color(0.002f, 0.01f, 0.031f, 0.92f));
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.5f));

        const float arrowWidth = 48f;
        const float outerGap = 9f;
        const int maxVisibleCars = 5;
        float itemGap = 8f;
        int count = Mathf.Max(1, CarNames.Length);
        int visibleCount = Mathf.Min(maxVisibleCars, count);
        float itemWidth = (rect.width - arrowWidth * 2f - outerGap * 2f - itemGap * (visibleCount - 1)) / visibleCount;
        Rect previousRect = new Rect(rect.x + 4f, rect.y + 8f, arrowWidth - 8f, rect.height - 16f);
        Rect nextRect = new Rect(rect.xMax - arrowWidth + 4f, rect.y + 8f, arrowWidth - 8f, rect.height - 16f);

        GUI.Label(previousRect, "<", arcadeHeadingStyle);
        GUI.Label(nextRect, ">", arcadeHeadingStyle);
        if (GUI.Button(previousRect, GUIContent.none, GUIStyle.none)) garageCarIndex = (garageCarIndex + CarNames.Length - 1) % CarNames.Length;
        if (GUI.Button(nextRect, GUIContent.none, GUIStyle.none)) garageCarIndex = (garageCarIndex + 1) % CarNames.Length;

        int firstVisibleIndex = count <= visibleCount ? 0 : garageCarIndex - visibleCount / 2;
        for (int slot = 0; slot < visibleCount; slot++)
        {
            int i = (firstVisibleIndex + slot + count) % count;
            Rect item = new Rect(rect.x + arrowWidth + outerGap + slot * (itemWidth + itemGap), rect.y + 7f, itemWidth, rect.height - 14f);
            bool selected = i == garageCarIndex;
            Color accent = selected ? ArcadeLime : i % 3 == 0 ? ArcadeCyan : i % 3 == 1 ? ArcadePink : ArcadeYellow;
            DrawSolidRect(new Rect(item.x - 2f, item.y - 2f, item.width + 4f, item.height + 4f), new Color(accent.r, accent.g, accent.b, selected ? 0.85f : 0.22f));
            DrawSolidRect(item, selected ? new Color(accent.r * 0.15f, accent.g * 0.15f, accent.b * 0.15f, 0.98f) : new Color(0.006f, 0.018f, 0.048f, 0.94f));
            if (selected) DrawSolidRect(new Rect(item.x, item.yMax - 5f, item.width, 5f), accent);

            Texture2D sprite = selected ? GetPaintedGarageCarSprite(i) : GetGarageCarSprite(i);
            if (sprite != null)
            {
                Rect imageBounds = new Rect(item.x + 8f, item.y + 4f, item.width - 16f, item.height - 29f);
                Color oldColor = GUI.color;
                GUI.color = selected ? Color.white : new Color(0.62f, 0.75f, 0.82f, 0.82f);
                GUI.DrawTextureWithTexCoords(FitGarageCarTexture(imageBounds, sprite), sprite, GetGarageCarTextureCoordinates(i), true);
                GUI.color = oldColor;
            }

            int storyChapter = GetStoryCarUnlockChapter(i);
            string carStatus = IsCarOwned(i)
                ? CarNames[i]
                : storyChapter >= 0
                    ? "STORY  " + (storyChapter + 1).ToString("00")
                    : "LOCKED  " + CarPrices[i];
            GUI.Label(new Rect(item.x + 4f, item.yMax - 23f, item.width - 8f, 18f), carStatus, arcadeCenteredStyle);
            if (GUI.Button(item, GUIContent.none, GUIStyle.none)) garageCarIndex = i;
        }
    }

    private void DrawArcadeGarage(float screenWidth, float screenHeight)
    {
        const float referenceWidth = 1680f;
        const float referenceHeight = 945f;
        float scale = Mathf.Min(screenWidth / referenceWidth, screenHeight / referenceHeight);
        float offsetX = (screenWidth - referenceWidth * scale) * 0.5f;
        float offsetY = (screenHeight - referenceHeight * scale) * 0.5f;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = previousMatrix * Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        DrawArcadeGarageReference(referenceWidth, referenceHeight);
        GUI.matrix = previousMatrix;
    }

    private void DrawArcadeGarageReference(float screenWidth, float screenHeight)
    {
        DrawGarageWorkshopBackdrop(screenWidth, screenHeight);
        float outerMargin = Mathf.Clamp(screenWidth * 0.022f, 22f, 42f);
        float contentWidth = Mathf.Min(1640f, screenWidth - outerMargin * 2f);
        float contentX = (screenWidth - contentWidth) * 0.5f;
        float topY = Mathf.Clamp(screenHeight * 0.025f, 18f, 28f);
        float headerHeight = 78f;
        float bodyY = topY + headerHeight + 10f;
        float bodyHeight = screenHeight - bodyY - Mathf.Clamp(screenHeight * 0.024f, 18f, 26f);
        float leftWidth = contentWidth * 0.645f;
        float rightX = contentX + leftWidth + 24f;
        float rightWidth = contentWidth - leftWidth - 24f;

        DrawSolidRect(new Rect(contentX, topY + headerHeight - 4f, contentWidth, 2f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.32f));
        GUI.Label(new Rect(contentX, topY - 2f, 300f, 78f), "GARAGE", arcadeTitleStyle);
        GUI.Label(new Rect(contentX + 292f, topY + 24f, 120f, 38f), "// 02", arcadeHeadingStyle);

        float headerActionWidth = Mathf.Clamp(rightWidth * 0.48f, 205f, 252f);
        Rect coins = new Rect(contentX + contentWidth - headerActionWidth, topY, headerActionWidth, 40f);
        DrawArcadePanel(coins, ArcadeYellow, ArcadeOrange, true, false);
        GUI.Label(new Rect(coins.x + 14f, coins.y + 5f, coins.width - 28f, 30f), this.coins + " COINS", arcadeCompactHeadingStyle);
        Rect back = new Rect(contentX + contentWidth - headerActionWidth, topY + 46f, headerActionWidth, 32f);
        if (DrawArcadeButton(back, "<", "НАЗАД", string.Empty, ArcadeCyan, false))
        {
            if (mainMenuOpen) garageOpen = false; else ToggleGarage();
        }

        Rect carPanel = new Rect(contentX, bodyY, leftWidth, bodyHeight);
        DrawSolidRect(new Rect(carPanel.x, carPanel.y, 5f, carPanel.height), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.72f));
        DrawSolidRect(new Rect(carPanel.x, carPanel.y, carPanel.width, 3f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.54f));
        float innerX = carPanel.x + 18f;
        float innerWidth = carPanel.width - 36f;
        float cursorY = carPanel.y + 14f;
        GUI.Label(new Rect(innerX + 10f, cursorY, innerWidth * 0.56f, 35f), CarNames[garageCarIndex], arcadeHeadingStyle);
        GUI.Label(new Rect(innerX + innerWidth * 0.55f, cursorY + 5f, innerWidth * 0.43f, 25f), CarClasses[garageCarIndex].ToUpperInvariant(), arcadeCenteredStyle);
        DrawSolidRect(new Rect(innerX, cursorY + 39f, innerWidth, 2f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.55f));

        float carouselHeight = Mathf.Clamp(bodyHeight * 0.12f, 82f, 104f);
        cursorY += 49f;
        DrawGarageCarCarousel(new Rect(innerX, cursorY, innerWidth, carouselHeight));
        cursorY += carouselHeight + 10f;

        float customizationHeight = Mathf.Clamp(bodyHeight * 0.155f, 108f, 132f);
        const float statHeight = 52f;
        const float actionHeight = 70f;
        float heroHeight = carPanel.yMax - 16f - customizationHeight - 10f - actionHeight - 9f - statHeight - 10f - cursorY;
        heroHeight = Mathf.Max(170f, heroHeight);
        DrawArcadeCarPreview(new Rect(innerX, cursorY, innerWidth, heroHeight), garageCarIndex);
        cursorY += heroHeight + 10f;
        DrawArcadeStatStrip(new Rect(innerX, cursorY, innerWidth, statHeight), garageCarIndex);
        cursorY += statHeight + 9f;

        bool owned = IsCarOwned(garageCarIndex);
        int storyUnlockChapter = GetStoryCarUnlockChapter(garageCarIndex);
        bool storyLocked = storyUnlockChapter >= 0 && !owned;
        string action = selectedCarIndex == garageCarIndex
            ? "ВЫБРАНА"
            : owned
                ? "ВЫБРАТЬ"
                : storyLocked
                    ? "СЮЖЕТНАЯ НАГРАДА"
                    : "КУПИТЬ  " + CarPrices[garageCarIndex];
        bool canAffordCar = owned || (!storyLocked && this.coins >= CarPrices[garageCarIndex]);
        Color vehicleActionAccent = owned ? ArcadeLime : storyLocked ? ArcadeCyan : canAffordCar ? ArcadeYellow : ArcadePink;
        string vehicleActionSubtitle = owned
            ? "READY TO RACE"
            : storyLocked
                ? "ПРОЙДИТЕ ГЛАВУ  " + (storyUnlockChapter + 1).ToString("00")
                : canAffordCar
                    ? "PRICE  " + CarPrices[garageCarIndex] + " COINS"
                    : "НЕ ХВАТАЕТ  " + (CarPrices[garageCarIndex] - this.coins) + " COINS";
        float actionWidth = innerWidth * 0.62f;
        float actionX = innerX + (innerWidth - actionWidth) * 0.5f;
        bool canChooseCar = selectedCarIndex != garageCarIndex;
        if (DrawArcadeButton(new Rect(actionX, cursorY, actionWidth, actionHeight), owned ? "OK" : "CR", action, vehicleActionSubtitle, vehicleActionAccent, true) && canChooseCar)
        {
            TryBuyOrSelectCar();
        }
        cursorY += actionHeight + 10f;

        Rect customization = new Rect(innerX, cursorY, innerWidth, customizationHeight);
        DrawSolidRect(customization, new Color(0.003f, 0.014f, 0.04f, 0.95f));
        DrawSolidRect(new Rect(customization.x, customization.y, customization.width, 3f), ArcadeCyan);
        float customColumnWidth = (customization.width - 34f) * 0.5f;
        GUI.Label(new Rect(customization.x + 14f, customization.y + 10f, customColumnWidth, 22f), "PAINT", arcadeSmallStyle);
        DrawArcadeColorChoices(new Rect(customization.x + 14f, customization.y + 42f, customColumnWidth, Mathf.Min(42f, customization.height - 54f)), PaintColors, paintColorIndex, true);
        DrawSolidRect(new Rect(customization.center.x, customization.y + 12f, 2f, customization.height - 24f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.25f));
        GUI.Label(new Rect(customization.center.x + 16f, customization.y + 10f, customColumnWidth, 22f), "NEON", arcadeSmallStyle);
        DrawArcadeColorChoices(new Rect(customization.center.x + 16f, customization.y + 42f, customColumnWidth, Mathf.Min(42f, customization.height - 54f)), NeonColors, neonColorIndex, false);

        Rect upgrades = new Rect(rightX, bodyY, rightWidth, bodyHeight);
        DrawArcadePanel(upgrades, ArcadeCyan, ArcadeYellow);
        GUI.Label(new Rect(upgrades.x + 20f, upgrades.y + 14f, upgrades.width - 40f, 34f), "PERFORMANCE SHOP", arcadeHeadingStyle);
        float tabGap = 12f;
        float shopInnerX = upgrades.x + 18f;
        float shopInnerWidth = upgrades.width - 36f;
        float tabWidth = (shopInnerWidth - tabGap) * 0.5f;
        if (DrawArcadeShopTab(new Rect(shopInnerX, upgrades.y + 54f, tabWidth, 44f), "МАШИНА", ArcadeCyan, garageUpgradeTab == 0)) garageUpgradeTab = 0;
        if (DrawArcadeShopTab(new Rect(shopInnerX + tabWidth + tabGap, upgrades.y + 54f, tabWidth, 44f), "ОРУЖИЕ", ArcadePink, garageUpgradeTab == 1)) garageUpgradeTab = 1;

        float cardsY = upgrades.y + 112f;
        float cardsBottom = upgrades.yMax - 20f;
        float cardGap = 12f;
        float cardHeight = (cardsBottom - cardsY - cardGap * 2f) / 3f;

        if (garageUpgradeTab == 0)
        {
            DrawArcadeUpgradeCard(new Rect(shopInnerX, cardsY, shopInnerWidth, cardHeight), 0, "01 ENGINE", "+10% ACCEL  /  +6% SPEED", ArcadePink);
            DrawArcadeUpgradeCard(new Rect(shopInnerX, cardsY + cardHeight + cardGap, shopInnerWidth, cardHeight), 1, "02 HANDLING", "+8% TURN  /  GRIP", ArcadeCyan);
            DrawArcadeUpgradeCard(new Rect(shopInnerX, cardsY + (cardHeight + cardGap) * 2f, shopInnerWidth, cardHeight), 2, "03 ARMOR", "LESS COLLISION DAMAGE", ArcadeYellow);
        }
        else
        {
            float arsenalHeight = Mathf.Min(212f, (cardsBottom - cardsY) * 0.36f);
            DrawStoryWeaponArsenal(new Rect(shopInnerX, cardsY, shopInnerWidth, arsenalHeight));
            float weaponCardsY = cardsY + arsenalHeight + cardGap;
            float weaponCardHeight = (cardsBottom - weaponCardsY - cardGap * 2f) / 3f;
            DrawArcadeUpgradeCard(new Rect(shopInnerX, weaponCardsY, shopInnerWidth, weaponCardHeight), 3, "01 WEAPON DAMAGE", "+16% DAMAGE  /  ALL WEAPONS", ArcadePink);
            DrawArcadeUpgradeCard(new Rect(shopInnerX, weaponCardsY + weaponCardHeight + cardGap, shopInnerWidth, weaponCardHeight), 4, "02 MAX AMMO", "+2 MAX AMMO  /  ALL WEAPONS", ArcadeLime);
            DrawArcadeUpgradeCard(new Rect(shopInnerX, weaponCardsY + (weaponCardHeight + cardGap) * 2f, shopInnerWidth, weaponCardHeight), 5, "03 FIRE RATE", "-8% FIRE COOLDOWN", ArcadeYellow);
        }

        if (Time.unscaledTime < garageMessageUntil)
        {
            Rect message = new Rect(upgrades.x + 18f, upgrades.yMax - 48f, upgrades.width - 36f, 30f);
            DrawSolidRect(message, new Color(ArcadeInk.r, ArcadeInk.g, ArcadeInk.b, 0.92f));
            GUI.Label(message, garageMessage, arcadeCenteredStyle);
        }
    }

    private bool DrawArcadeShopTab(Rect rect, string title, Color accent, bool selected)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        Color fill = selected || hovered
            ? new Color(accent.r * 0.19f, accent.g * 0.19f, accent.b * 0.19f, 0.98f)
            : new Color(0.006f, 0.02f, 0.052f, 0.98f);
        DrawSolidRect(new Rect(rect.x + 5f, rect.y + 6f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.52f));
        DrawSolidRect(rect, fill);
        DrawSolidRect(new Rect(rect.x, rect.y, selected ? 6f : 3f, rect.height), accent);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 3f), accent);
        Color previous = GUI.color;
        GUI.color = selected ? accent : Color.white;
        GUI.Label(new Rect(rect.x + 14f, rect.y + 7f, rect.width - 28f, 30f), title, arcadeCenteredStyle);
        GUI.color = previous;
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void DrawArcadeUpgradeCard(Rect rect, int category, string title, string description, Color accent)
    {
        int level = GetUpgradeLevel(category);
        DrawArcadePanel(rect, accent, ArcadeCyan);
        GUIStyle titleStyle = title.Length > 14 ? arcadeCompactHeadingStyle : arcadeHeadingStyle;
        GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 142f, 30f), title, titleStyle);
        GUI.Label(new Rect(rect.x + rect.width - 118f, rect.y + 13f, 100f, 26f), level + " / " + MaxUpgradeLevel, arcadeLabelStyle);
        GUI.Label(new Rect(rect.x + 20f, rect.y + 45f, rect.width - 40f, 20f), description, arcadeMicroStyle);
        float controlY = rect.y + Mathf.Clamp(rect.height * 0.49f, 70f, 92f);
        float controlHeight = Mathf.Clamp(rect.yMax - controlY - 13f, 48f, 66f);
        DrawArcadeSegments(new Rect(rect.x + 20f, controlY + 4f, rect.width * 0.43f, 16f), level / (float)MaxUpgradeLevel, accent, 5);
        if (DrawArcadePurchaseButton(new Rect(rect.x + rect.width * 0.51f, controlY, rect.width * 0.46f - 18f, controlHeight), category, accent)) TryBuyUpgrade(category);
    }

    private bool DrawArcadePurchaseButton(Rect rect, int category, Color accent)
    {
        int level = GetUpgradeLevel(category);
        int cost = GetUpgradeCost(category);
        bool isMax = level >= MaxUpgradeLevel;
        bool canAfford = !isMax && coins >= cost;
        bool hovered = !isMax && rect.Contains(Event.current.mousePosition);
        Color stateAccent = isMax
            ? ArcadeLime
            : canAfford
                ? accent
                : new Color(1f, 0.18f, 0.12f);

        int audioId = Mathf.RoundToInt(rect.x * 23f + rect.y * 31f + category * 101f);
        RegisterMenuHover(audioId, hovered);

        Color fill = hovered
            ? new Color(stateAccent.r * 0.24f, stateAccent.g * 0.24f, stateAccent.b * 0.24f, 0.99f)
            : new Color(0.008f, 0.02f, 0.045f, 0.99f);
        if (!canAfford && !isMax)
        {
            fill = new Color(0.065f, 0.012f, 0.018f, 0.99f);
        }

        DrawSolidRect(new Rect(rect.x + 6f, rect.y + 7f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.66f));
        DrawSolidRect(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), new Color(stateAccent.r, stateAccent.g, stateAccent.b, hovered ? 0.34f : 0.13f));
        DrawSolidRect(rect, fill);
        DrawSolidRect(new Rect(rect.x, rect.y, hovered ? 7f : 4f, rect.height), stateAccent);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 3f), stateAccent);
        DrawSolidRect(new Rect(rect.x, rect.yMax - 3f, rect.width, 3f), new Color(stateAccent.r, stateAccent.g, stateAccent.b, 0.72f));

        float badgeWidth = Mathf.Clamp(rect.width * 0.2f, 42f, 54f);
        Rect badge = new Rect(rect.x + 9f, rect.y + 8f, badgeWidth - 9f, rect.height - 16f);
        DrawSolidRect(badge, new Color(stateAccent.r, stateAccent.g, stateAccent.b, isMax ? 0.28f : 0.16f));
        Color previous = GUI.color;
        GUI.color = stateAccent;
        GUI.Label(badge, isMax ? "OK" : "+", arcadeCenteredStyle);
        GUI.color = previous;

        float priceWidth = Mathf.Clamp(rect.width * 0.29f, 68f, 86f);
        Rect price = new Rect(rect.xMax - priceWidth - 8f, rect.y + 8f, priceWidth, rect.height - 16f);
        DrawSolidRect(price, new Color(stateAccent.r, stateAccent.g, stateAccent.b, isMax ? 0.22f : canAfford ? 0.18f : 0.1f));
        DrawSolidRect(new Rect(price.x, price.y, 2f, price.height), new Color(stateAccent.r, stateAccent.g, stateAccent.b, 0.78f));
        GUI.color = stateAccent;
        GUI.Label(price, isMax ? "MAX" : cost + " C", arcadeCenteredStyle);
        GUI.color = previous;

        float textX = badge.xMax + 10f;
        float textWidth = price.x - textX - 8f;
        string title = isMax ? "ГОТОВО" : canAfford ? "КУПИТЬ" : "НЕТ МОНЕТ";
        string subtitle = isMax ? "MAX LEVEL" : canAfford ? "UPGRADE READY" : "НУЖНО +" + (cost - coins);
        GUIStyle purchaseTitleStyle = title.Length > 7 || textWidth < 92f ? arcadeSmallStyle : arcadeCompactHeadingStyle;
        GUI.Label(new Rect(textX, rect.y + 6f, textWidth, Mathf.Max(20f, rect.height * 0.48f)), title, purchaseTitleStyle);
        GUI.Label(new Rect(textX + 1f, rect.yMax - 25f, textWidth, 18f), subtitle, arcadeMicroStyle);

        if (hovered)
        {
            float scan = Mathf.Repeat(Time.unscaledTime * 1.25f, 1f);
            float scanX = Mathf.Lerp(rect.x + 8f, rect.xMax - 10f, scan);
            DrawSolidRect(new Rect(scanX, rect.y + 4f, 3f, rect.height - 8f), new Color(stateAccent.r, stateAccent.g, stateAccent.b, 0.58f));
        }

        if (isMax)
        {
            return false;
        }

        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);
        if (clicked)
        {
            PlayMenuClickSfx();
        }

        return clicked;
    }

    private void DrawArcadeColorChoices(Rect rect, Color[] colors, int selectedIndex, bool paint)
    {
        float gap = 10f;
        float size = Mathf.Min(rect.height, (rect.width - gap * (colors.Length - 1)) / colors.Length);
        for (int i = 0; i < colors.Length; i++)
        {
            Rect swatch = new Rect(rect.x + i * (size + gap), rect.y, size, size);
            DrawSolidRect(new Rect(swatch.x - 4f, swatch.y - 4f, swatch.width + 8f, swatch.height + 8f), i == selectedIndex ? ArcadeYellow : new Color(0.1f, 0.24f, 0.34f, 1f));
            DrawSolidRect(swatch, colors[i]);
            if (GUI.Button(swatch, GUIContent.none, GUIStyle.none))
            {
                if (paint) SelectPaint(i); else SelectNeon(i);
            }
        }
    }

    private void DrawArcadeRaceHud(float screenWidth, float screenHeight)
    {
        Rect stats = new Rect(20f, 20f, 316f, 186f);
        DrawArcadePanel(stats, ArcadePink, ArcadeCyan);
        GUI.Label(new Rect(stats.x + 17f, stats.y + 13f, stats.width - 34f, 30f), ActiveTrack.ShortName, arcadeHeadingStyle);
        GUI.Label(new Rect(stats.x + 18f, stats.y + 51f, stats.width - 36f, 22f), "COINS  " + coins, arcadeSmallStyle);
        GUI.Label(new Rect(stats.x + 18f, stats.y + 77f, stats.width - 36f, 22f), "LAP  " + Mathf.Min(completedLaps + 1, RaceLapTarget) + " / " + RaceLapTarget, arcadeSmallStyle);
        GUI.Label(new Rect(stats.x + 18f, stats.y + 103f, stats.width - 36f, 22f), "POSITION  " + RacePosition() + " / " + (ActiveOpponentCount + 1), arcadeSmallStyle);
        GUI.Label(new Rect(stats.x + 18f, stats.y + 129f, stats.width - 36f, 22f), "TIME  " + FormatTime(raceFinished ? finishTime : raceTime), arcadeSmallStyle);
        GUI.Label(new Rect(stats.x + 18f, stats.y + 155f, stats.width - 36f, 22f), "SPEED  " + Mathf.RoundToInt(player != null ? player.SpeedKph : 0f) + " KM/H", arcadeSmallStyle);

        if (storyRaceActive)
        {
            DrawArcadeStoryObjectiveHud(screenWidth, screenHeight);
        }
        else
        {
            DrawArcadeModeHud(screenWidth, screenHeight);
        }

        if (raceStarted && !float.IsPositiveInfinity(bestLap))
        {
            Rect best = new Rect(screenWidth - 250f, 20f, 230f, 78f);
            DrawArcadePanel(best, ArcadeYellow, ArcadePink);
            GUI.Label(new Rect(best.x + 14f, best.y + 9f, best.width - 28f, 20f), "BEST LAP", arcadeMicroStyle);
            GUI.Label(new Rect(best.x + 14f, best.y + 31f, best.width - 28f, 34f), FormatTime(bestLap), arcadeHeadingStyle);
        }

        DrawMinimap(screenWidth);
        GetRaceHudLayout(screenWidth, screenHeight, out Rect controls, out Rect durability, out Rect weapon);
        DrawPlayerDurability(durability);
        DrawPlayerWeaponHud(weapon);
        DrawRaceControls(controls);
    }

    private void DrawArcadeRaceEffects(float screenWidth, float screenHeight)
    {
        if (player == null || mainMenuOpen || garageOpen)
        {
            return;
        }

        float time = Time.unscaledTime;
        float speedRatio = Mathf.InverseLerp(72f, 185f, player.SpeedKph);
        if (speedRatio > 0.01f)
        {
            float edgeAlpha = speedRatio * 0.035f;
            DrawSolidRect(new Rect(0f, 0f, 12f, screenHeight), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, edgeAlpha));
            DrawSolidRect(new Rect(screenWidth - 12f, 0f, 12f, screenHeight), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, edgeAlpha));
        }

        if (player.IsDrifting)
        {
            float driftStrength = Mathf.Clamp01(player.DriftCombo / 45f);
            float pulse = 0.55f + Mathf.Sin(time * 8f) * 0.2f;
            DrawSolidRect(new Rect(0f, screenHeight - 8f, screenWidth * driftStrength, 8f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.16f * pulse));
            DrawSolidRect(new Rect(screenWidth * (1f - driftStrength), screenHeight - 4f, screenWidth * driftStrength, 4f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.26f * pulse));
        }

        if (player.IsNitroActive)
        {
            float pulse = 0.78f + Mathf.Sin(time * 24f) * 0.12f;
            float centerX = screenWidth * 0.5f;
            float centerY = screenHeight * 0.53f;
            for (int i = 0; i < 18; i++)
            {
                float angle = i * (360f / 18f) + Mathf.Sin(i * 2.17f) * 5f;
                float radians = angle * Mathf.Deg2Rad;
                float phase = Mathf.Repeat(time * 2.65f + i * 0.137f, 1f);
                float distance = Mathf.Lerp(Mathf.Min(screenWidth, screenHeight) * 0.22f, Mathf.Max(screenWidth, screenHeight) * 0.58f, phase);
                float x = centerX + Mathf.Cos(radians) * distance;
                float y = centerY + Mathf.Sin(radians) * distance * 0.58f;
                float length = Mathf.Lerp(18f, 94f, phase) * pulse;
                Color color = i % 3 == 0 ? ArcadePink : ArcadeCyan;
                DrawRotatedRect(new Rect(x - 1.5f, y - length * 0.5f, 3f, length), new Color(color.r, color.g, color.b, Mathf.Lerp(0.03f, 0.24f, phase)), angle - 90f);
            }

            DrawSolidRect(new Rect(0f, 0f, screenWidth, 5f), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.48f * pulse));
            DrawSolidRect(new Rect(0f, screenHeight - 5f, screenWidth, 5f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.48f * pulse));
        }

        if (nitroFlashAmount > 0.01f)
        {
            float flash = nitroFlashAmount * nitroFlashAmount;
            DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.18f, 0.92f, 1f, flash * 0.12f));
        }

        if (hitFlashAmount > 0.01f)
        {
            float flash = hitFlashAmount * hitFlashAmount;
            DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(1f, 0.12f, 0.055f, flash * 0.38f));
            DrawSolidRect(new Rect(0f, 0f, screenWidth, 7f), new Color(1f, 0.65f, 0.12f, flash * 0.72f));
            DrawSolidRect(new Rect(0f, screenHeight - 7f, screenWidth, 7f), new Color(1f, 0.18f, 0.06f, flash * 0.72f));
        }
    }

    private void DrawArcadeSegments(Rect rect, float ratio, Color accent, int count)
    {
        ratio = Mathf.Clamp01(ratio);
        float gap = 3f;
        float width = (rect.width - gap * (count - 1)) / count;
        for (int i = 0; i < count; i++)
        {
            Color color = (i + 1f) / count <= ratio ? accent : new Color(0.08f, 0.12f, 0.19f, 1f);
            DrawSolidRect(new Rect(rect.x + i * (width + gap), rect.y, width, rect.height), color);
        }
    }

    private void DrawArcadeCountdown(float screenWidth, float screenHeight, string text, Color accent)
    {
        Rect panel = new Rect(screenWidth * 0.5f - 230f, screenHeight * 0.31f, 460f, 132f);
        DrawArcadePanel(panel, accent, ArcadePink);
        Color previous = GUI.color;
        GUI.color = accent;
        GUI.Label(panel, text, arcadeTitleStyle);
        GUI.color = previous;
    }

    private void DrawArcadeFinishOverlay(float screenWidth, float screenHeight)
    {
        if (storyRaceActive)
        {
            DrawArcadeStoryFinishOverlay(screenWidth, screenHeight);
            return;
        }
        if (IsSpecialArcadeRace)
        {
            DrawArcadeModeFinishOverlay(screenWidth, screenHeight);
            return;
        }

        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.002f, 0.004f, 0.018f, 0.86f));
        Rect panel = new Rect(screenWidth * 0.5f - 390f, screenHeight * 0.5f - 260f, 780f, 520f);
        DrawArcadePanel(panel, ArcadeLime, ArcadeCyan);
        GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 32f), "RACE COMPLETE  //  HIGH SCORE", arcadeSmallStyle);
        Color previous = GUI.color;
        GUI.color = ArcadeLime;
        GUI.Label(new Rect(panel.x + 24f, panel.y + 55f, panel.width - 48f, 92f), "ФИНИШ!", arcadeTitleStyle);
        GUI.color = previous;
        DrawArcadeMetric(new Rect(panel.x + 28f, panel.y + 168f, 350f, 112f), "TOTAL TIME", FormatTime(finishTime), ArcadeCyan);
        DrawArcadeMetric(new Rect(panel.x + 402f, panel.y + 168f, 350f, 112f), "REWARD", lastFinishReward + " COINS", ArcadeYellow);
        if (DrawArcadeButton(new Rect(panel.x + 28f, panel.y + 320f, 350f, 92f), "R", "ЕЩЁ РАЗ", "RESTART RACE", ArcadePink, true)) RestartRace();
        if (DrawArcadeButton(new Rect(panel.x + 402f, panel.y + 320f, 350f, 92f), "G", "ГАРАЖ", "UPGRADE CAR", ArcadeCyan, false))
        {
            garageOpen = true;
            garageCarIndex = selectedCarIndex;
            Time.timeScale = 0f;
        }
        if (DrawArcadeButton(new Rect(panel.x + 215f, panel.y + 430f, 350f, 62f), "ESC", "В МЕНЮ", string.Empty, ArcadeYellow, false)) OpenMainMenu();
    }

    private void DrawArcadeWreckedOverlay(float screenWidth, float screenHeight)
    {
        if (!DrawRetroArtwork(screenWidth, screenHeight, 0.82f))
        {
            DrawArcadeBackdrop(screenWidth, screenHeight);
            DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.002f, 0.003f, 0.016f, 0.78f));
        }

        float alarmPulse = 0.08f + (Mathf.Sin(Time.unscaledTime * 4.8f) + 1f) * 0.035f;
        DrawSolidRect(new Rect(0f, screenHeight * 0.18f, screenWidth, screenHeight * 0.64f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, alarmPulse));

        const float panelWidth = 1000f;
        const float panelHeight = 580f;
        Rect panel = new Rect(screenWidth * 0.5f - panelWidth * 0.5f, screenHeight * 0.5f - panelHeight * 0.5f, panelWidth, panelHeight);
        DrawSolidRect(new Rect(panel.x + 13f, panel.y + 15f, panel.width, panel.height), new Color(0f, 0f, 0f, 0.82f));
        DrawSolidRect(new Rect(panel.x - 8f, panel.y - 8f, panel.width + 16f, panel.height + 16f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.22f));
        DrawSolidRect(panel, new Color(0.002f, 0.008f, 0.028f, 0.995f));
        DrawSolidRect(new Rect(panel.x, panel.y, panel.width, 7f), ArcadePink);
        DrawSolidRect(new Rect(panel.x, panel.y, 7f, panel.height), ArcadePink);
        DrawSolidRect(new Rect(panel.x, panel.y + panel.height - 7f, panel.width, 7f), ArcadeCyan);
        DrawSolidRect(new Rect(panel.x + panel.width - 7f, panel.y, 7f, panel.height), ArcadeCyan);
        DrawSolidRect(new Rect(panel.x + 13f, panel.y + 13f, panel.width - 26f, panel.height - 26f), new Color(0.008f, 0.019f, 0.052f, 0.98f));
        DrawSolidRect(new Rect(panel.x + 21f, panel.y + 19f, panel.width - 42f, 35f), new Color(0.13f, 0.004f, 0.04f, 0.98f));
        DrawSolidRect(new Rect(panel.x + 21f, panel.y + 19f, 7f, 35f), ArcadePink);
        GUI.Label(new Rect(panel.x + 42f, panel.y + 27f, 560f, 20f), "NEON CIRCUIT  /  SYSTEM FAILURE  /  VEHICLE OFFLINE", arcadeSmallStyle);
        Color previous = GUI.color;
        GUI.color = ArcadePink;
        GUI.Label(new Rect(panel.x + panel.width - 178f, panel.y + 27f, 142f, 20f), "ERROR 00", arcadeSmallStyle);
        GUI.color = previous;

        GUI.color = ArcadePink;
        GUI.Label(new Rect(panel.x + 30f, panel.y + 57f, panel.width - 60f, 70f), "МАШИНА РАЗБИТА", arcadeTitleStyle);
        GUI.color = previous;
        if (storyRaceActive)
        {
            GUI.Label(new Rect(panel.x + 30f, panel.y + 111f, panel.width - 60f, 20f), "СЮЖЕТ  /  " + ActiveStoryChapter.Title + "  /  МИССИЯ ПРОВАЛЕНА", arcadeCenteredStyle);
        }
        DrawSolidRect(new Rect(panel.x + 31f, panel.y + 128f, panel.width - 62f, 3f), ArcadePink);
        DrawSolidRect(new Rect(panel.x + panel.width * 0.5f, panel.y + 128f, panel.width * 0.47f, 3f), ArcadeCyan);

        Rect actions = new Rect(panel.x + 28f, panel.y + 147f, 392f, 382f);
        Rect report = new Rect(panel.x + 442f, panel.y + 147f, 530f, 382f);
        GUI.Label(new Rect(actions.x + 2f, actions.y, actions.width - 4f, 22f), "CONTINUE?  /  SELECT ACTION", arcadeSmallStyle);

        if (DrawArcadeCrashButton(new Rect(actions.x, actions.y + 32f, actions.width, 98f), "01", "ПОВТОРИТЬ", "R  /  REPAIR + RESTART", ArcadePink, true)) RestartRace();
        if (DrawArcadeCrashButton(new Rect(actions.x, actions.y + 144f, actions.width, 98f), "02", "ГАРАЖ", "G  /  CHANGE VEHICLE", ArcadeCyan, false))
        {
            garageOpen = true;
            garageCarIndex = selectedCarIndex;
            Time.timeScale = 0f;
        }
        if (DrawArcadeCrashButton(new Rect(actions.x, actions.y + 256f, actions.width, 98f), "03", "ГЛАВНОЕ МЕНЮ", "ESC  /  TRACK SELECT", ArcadeYellow, false)) OpenMainMenu();

        float pulseAlpha = 0.42f + (Mathf.Sin(Time.unscaledTime * 5.2f) + 1f) * 0.16f;
        DrawSolidRect(new Rect(actions.x, actions.y + 372f, actions.width, 3f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, pulseAlpha));

        DrawSolidRect(report, new Color(0.002f, 0.009f, 0.032f, 0.99f));
        DrawSolidRect(new Rect(report.x, report.y, report.width, 5f), ArcadeCyan);
        DrawSolidRect(new Rect(report.x, report.y, 5f, report.height), ArcadeCyan);
        DrawSolidRect(new Rect(report.x + report.width - 5f, report.y, 5f, report.height), ArcadePink);
        Rect vehicleFeed = new Rect(report.x + 14f, report.y + 16f, report.width - 28f, 206f);
        DrawArcadeCrashVehicleFeed(vehicleFeed);

        float metricGap = 8f;
        float metricWidth = (report.width - 28f - metricGap * 2f) / 3f;
        float metricY = report.y + 234f;
        DrawArcadeCrashReadout(new Rect(report.x + 14f, metricY, metricWidth, 68f), "ВРЕМЯ", FormatTime(finishTime), ArcadePink);
        DrawArcadeCrashReadout(new Rect(report.x + 14f + metricWidth + metricGap, metricY, metricWidth, 68f), "КРУГ", Mathf.Min(completedLaps + 1, TotalLaps) + " / " + TotalLaps, ArcadeYellow);
        DrawArcadeCrashReadout(new Rect(report.x + 14f + (metricWidth + metricGap) * 2f, metricY, metricWidth, 68f), "ПОЗИЦИЯ", RacePosition() + " / " + (ActiveOpponentCount + 1), ArcadeCyan);

        Rect integrity = new Rect(report.x + 14f, report.y + 315f, report.width - 28f, 52f);
        DrawSolidRect(integrity, new Color(0.025f, 0.008f, 0.03f, 0.99f));
        DrawSolidRect(new Rect(integrity.x, integrity.y, integrity.width, 3f), ArcadePink);
        GUI.Label(new Rect(integrity.x + 11f, integrity.y + 8f, 210f, 18f), "CHASSIS INTEGRITY", arcadeMicroStyle);
        GUI.color = ArcadePink;
        GUI.Label(new Rect(integrity.x + integrity.width - 66f, integrity.y + 5f, 54f, 23f), "00%", arcadeSmallStyle);
        GUI.color = previous;
        DrawArcadeSegments(new Rect(integrity.x + 11f, integrity.y + 32f, integrity.width - 22f, 9f), 0f, ArcadePink, 18);

        GUI.Label(new Rect(panel.x + 30f, panel.y + 544f, panel.width - 60f, 18f), "SYSTEM HALTED  //  AWAITING INPUT  //  PRESS R TO CONTINUE", arcadeCenteredStyle);
    }

    private bool DrawArcadeCrashButton(Rect rect, string number, string title, string subtitle, Color accent, bool selected)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        bool highlighted = selected || hovered;
        Color fill = highlighted
            ? new Color(accent.r * 0.2f, accent.g * 0.1f, accent.b * 0.16f, 0.99f)
            : new Color(0.008f, 0.021f, 0.05f, 0.99f);

        DrawSolidRect(new Rect(rect.x + 9f, rect.y + 10f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.72f));
        DrawSolidRect(new Rect(rect.x - 5f, rect.y - 5f, rect.width + 10f, rect.height + 10f), new Color(accent.r, accent.g, accent.b, highlighted ? 0.24f : 0.1f));
        DrawSolidRect(rect, fill);
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 5f), accent);
        DrawSolidRect(new Rect(rect.x, rect.y + rect.height - 5f, rect.width, 5f), accent);
        DrawSolidRect(new Rect(rect.x, rect.y, 5f, rect.height), accent);
        DrawSolidRect(new Rect(rect.x + rect.width - 5f, rect.y, 5f, rect.height), accent);
        DrawSolidRect(new Rect(rect.x + 82f, rect.y + 5f, 4f, rect.height - 10f), new Color(accent.r, accent.g, accent.b, 0.66f));
        DrawSolidRect(new Rect(rect.x + 9f, rect.y + 9f, 64f, rect.height - 18f), new Color(accent.r * 0.18f, accent.g * 0.18f, accent.b * 0.18f, 0.92f));

        Color previous = GUI.color;
        GUI.color = accent;
        GUI.Label(new Rect(rect.x + 10f, rect.y + 10f, 62f, rect.height - 20f), number, arcadeNumberStyle);
        GUI.color = Color.white;
        GUIStyle titleStyle = title.Length > 11 ? arcadeCompactHeadingStyle : arcadeHeadingStyle;
        GUI.Label(new Rect(rect.x + 101f, rect.y + 12f, rect.width - 154f, 34f), title, titleStyle);
        GUI.color = previous;
        GUI.Label(new Rect(rect.x + 103f, rect.y + 61f, rect.width - 158f, 17f), subtitle, arcadeMicroStyle);
        GUI.color = accent;
        GUI.Label(new Rect(rect.x + rect.width - 50f, rect.y + 32f, 36f, 28f), highlighted ? ">>>" : ">", arcadeLabelStyle);
        GUI.color = previous;
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void DrawArcadeCrashVehicleFeed(Rect rect)
    {
        DrawSolidRect(rect, new Color(0.002f, 0.007f, 0.024f, 1f));
        for (int x = 1; x < 12; x++) DrawSolidRect(new Rect(rect.x + rect.width * x / 12f, rect.y, 1f, rect.height), new Color(ArcadeCyan.r, ArcadeCyan.g, ArcadeCyan.b, 0.045f));
        for (int y = 1; y < 6; y++) DrawSolidRect(new Rect(rect.x, rect.y + rect.height * y / 6f, rect.width, 1f), new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.06f));

        Color previous = GUI.color;
        GUI.color = new Color(ArcadePink.r, ArcadePink.g, ArcadePink.b, 0.12f);
        GUI.DrawTexture(new Rect(rect.x + rect.width * 0.18f, rect.y + rect.height * 0.13f, rect.width * 0.64f, rect.height * 0.76f), circleTexture);

        Texture2D carSprite = GetPaintedGarageCarSprite(selectedCarIndex);
        if (carSprite != null)
        {
            Rect carBounds = new Rect(rect.x + 42f, rect.y + 35f, rect.width - 84f, rect.height - 57f);
            Rect carRect = FitGarageCarTexture(carBounds, carSprite);
            GUI.color = new Color(0.76f, 0.36f, 0.48f, 0.9f);
            GUI.DrawTexture(carRect, carSprite, ScaleMode.StretchToFill, true);
        }

        GUI.color = previous;
        GUI.Label(new Rect(rect.x + 12f, rect.y + 10f, 300f, 18f), "CURRENT VEHICLE  /  " + CarNames[selectedCarIndex], arcadeMicroStyle);
        GUI.color = ArcadePink;
        GUI.Label(new Rect(rect.x + rect.width - 126f, rect.y + rect.height - 27f, 112f, 18f), "SYNC LOST", arcadeMicroStyle);
        GUI.color = previous;
    }

    private void DrawArcadeCrashReadout(Rect rect, string title, string value, Color accent)
    {
        DrawSolidRect(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.48f));
        DrawSolidRect(rect, new Color(0.008f, 0.024f, 0.055f, 0.98f));
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 3f), accent);
        DrawSolidRect(new Rect(rect.x, rect.y, 3f, rect.height), new Color(accent.r, accent.g, accent.b, 0.66f));
        GUI.Label(new Rect(rect.x + 12f, rect.y + 11f, rect.width - 24f, 18f), title, arcadeMicroStyle);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 32f, rect.width - 24f, 29f), value, arcadeCompactHeadingStyle);
    }

    private void DrawArcadeMetric(Rect rect, string title, string value, Color accent)
    {
        DrawArcadePanel(rect, accent, ArcadeCyan);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 12f, rect.width - 28f, 20f), title, arcadeMicroStyle);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 43f, rect.width - 28f, 42f), value, arcadeHeadingStyle);
    }
}
