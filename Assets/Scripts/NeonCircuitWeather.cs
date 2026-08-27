using UnityEngine;

public sealed partial class NeonCircuitGame
{
    private const string WeatherEnabledKey = "NeonCircuit.WeatherEnabled";
    private bool weatherEnabled = true;

    private enum CircuitWeather
    {
        Dust,
        Rain,
        Snow,
        Ash,
        IonStorm,
        Thunderstorm,
        Blizzard,
        Sandstorm
    }

    private CircuitWeather ActiveWeather
    {
        get
        {
            switch (selectedTrackIndex)
            {
                case 0: return CircuitWeather.Dust;
                case 1: return CircuitWeather.Rain;
                case 2: return CircuitWeather.Snow;
                case 3: return CircuitWeather.Ash;
                case 4: return CircuitWeather.IonStorm;
                case 5: return CircuitWeather.Thunderstorm;
                case 6: return CircuitWeather.Blizzard;
                default: return CircuitWeather.Sandstorm;
            }
        }
    }

    private string WeatherName
    {
        get
        {
            if (!weatherEnabled)
            {
                return "ОТКЛЮЧЕНА";
            }

            switch (ActiveWeather)
            {
                case CircuitWeather.Dust: return "ПЫЛЬНЫЙ ВЕТЕР";
                case CircuitWeather.Rain: return "НЕОНОВЫЙ ДОЖДЬ";
                case CircuitWeather.Snow: return "СНЕГОПАД";
                case CircuitWeather.Ash: return "ПЕПЛОПАД";
                case CircuitWeather.IonStorm: return "ИОННАЯ БУРЯ";
                case CircuitWeather.Thunderstorm: return "ГРОЗА";
                case CircuitWeather.Blizzard: return "МЕТЕЛЬ";
                default: return "ПЕСЧАНАЯ БУРЯ";
            }
        }
    }

    private Color WeatherColor
    {
        get
        {
            if (!weatherEnabled)
            {
                return new Color(0.42f, 0.5f, 0.58f);
            }

            switch (ActiveWeather)
            {
                case CircuitWeather.Rain:
                case CircuitWeather.Thunderstorm:
                    return new Color(0.18f, 0.84f, 1f);
                case CircuitWeather.Snow:
                case CircuitWeather.Blizzard:
                    return new Color(0.82f, 0.96f, 1f);
                case CircuitWeather.Ash:
                    return new Color(1f, 0.25f, 0.04f);
                case CircuitWeather.IonStorm:
                    return new Color(0.72f, 0.38f, 1f);
                default:
                    return new Color(1f, 0.68f, 0.12f);
            }
        }
    }

    public float WeatherGripMultiplier
    {
        get
        {
            if (!weatherEnabled)
            {
                return 1f;
            }

            switch (ActiveWeather)
            {
                case CircuitWeather.Thunderstorm: return 0.86f;
                case CircuitWeather.Blizzard: return 0.88f;
                case CircuitWeather.Rain: return 0.9f;
                case CircuitWeather.Sandstorm: return 0.91f;
                case CircuitWeather.Snow: return 0.93f;
                case CircuitWeather.Dust: return 0.95f;
                case CircuitWeather.Ash: return 0.96f;
                default: return 0.97f;
            }
        }
    }

    public bool RainPuddlesActive
    {
        get
        {
            return weatherEnabled
                && (ActiveWeather == CircuitWeather.Rain || ActiveWeather == CircuitWeather.Thunderstorm);
        }
    }

    private void CreateRainPuddles()
    {
        if (ActiveWeather != CircuitWeather.Rain && ActiveWeather != CircuitWeather.Thunderstorm)
        {
            return;
        }

        CreateTrackStrip(
            "Wet Asphalt Sheen",
            TrackWidth - 0.24f,
            new Color(0.18f, 0.42f, 0.52f, 0.055f),
            new Color(0.42f, 0.68f, 0.74f, 0.018f),
            54,
            -15,
            768,
            0f,
            Vector2.zero);

        for (int reflectionIndex = 0; reflectionIndex < 34; reflectionIndex++)
        {
            float reflectionT = Mathf.Repeat(0.017f + reflectionIndex * 0.0831f, 1f) * Mathf.PI * 2f;
            float reflectionLane = Mathf.Lerp(-2.75f, 2.75f, Mathf.Repeat(reflectionIndex * 0.618f + 0.23f, 1f));
            Vector2 reflectionPosition = PathPoint(reflectionT, reflectionLane);
            float reflectionLength = Mathf.Lerp(0.65f, 2.1f, Mathf.Repeat(reflectionIndex * 0.347f, 1f));
            float reflectionWidth = Mathf.Lerp(0.045f, 0.12f, Mathf.Repeat(reflectionIndex * 0.271f, 1f));
            CreateVisual(
                "Wet Road Reflection",
                reflectionPosition,
                new Vector2(reflectionWidth, reflectionLength),
                new Color(0.5f, 0.9f, 1f, 0.16f),
                -11,
                PathRotation(reflectionT) + Mathf.Lerp(-8f, 8f, Mathf.Repeat(reflectionIndex * 0.43f, 1f)),
                transform,
                false,
                circleSprite);
        }

        float[] fractions = { 0.035f, 0.145f, 0.265f, 0.385f, 0.515f, 0.635f, 0.755f, 0.875f, 0.955f };
        float[] lanes = { -0.7f, 1.15f, -1.35f, 0.65f, 1.45f, -0.55f, -1.25f, 0.8f, 1.25f };

        for (int i = 0; i < fractions.Length; i++)
        {
            float t = fractions[i] * Mathf.PI * 2f;
            float lane = lanes[i];
            if (!TryFindSafePickupPlacement(ref t, ref lane))
            {
                continue;
            }

            GameObject root = new GameObject("Wet Puddle " + (i + 1));
            root.transform.SetParent(transform);
            root.transform.position = PathPoint(t, lane);
            root.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(t));

            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2.6f + (i % 3) * 0.22f, 1.05f + (i % 2) * 0.18f);

            float width = 2.7f + (i % 3) * 0.24f;
            float height = 1.08f + (i % 2) * 0.16f;
            Color waterDark = new Color(0.015f, 0.12f, 0.19f, 0.76f);
            Color water = new Color(0.04f, 0.34f, 0.48f, 0.58f);
            Color reflection = new Color(0.24f, 0.9f, 1f, 0.48f);

            SpriteRenderer shadow = CreateVisual("Puddle Shadow", new Vector2(0.1f, -0.08f), new Vector2(width + 0.22f, height + 0.16f), new Color(0f, 0.025f, 0.05f, 0.48f), -14, 0f, root.transform, true, circleSprite);
            SpriteRenderer body = CreateVisual("Puddle Water", Vector2.zero, new Vector2(width, height), waterDark, -13, 0f, root.transform, true, circleSprite);
            SpriteRenderer lobeLeft = CreateVisual("Puddle Left Lobe", new Vector2(-width * 0.36f, 0.03f), new Vector2(width * 0.42f, height * 0.7f), water, -12, 0f, root.transform, true, circleSprite);
            SpriteRenderer lobeRight = CreateVisual("Puddle Right Lobe", new Vector2(width * 0.34f, -0.04f), new Vector2(width * 0.38f, height * 0.62f), water, -12, 0f, root.transform, true, circleSprite);
            SpriteRenderer shine = CreateVisual("Puddle Reflection", new Vector2(-width * 0.12f, 0.16f), new Vector2(width * 0.48f, 0.075f), reflection, -11, -7f, root.transform, true);
            SpriteRenderer ripple = CreateVisual("Puddle Ripple", new Vector2(width * 0.18f, -0.15f), new Vector2(width * 0.31f, 0.055f), new Color(0.62f, 0.98f, 1f, 0.42f), -10, 4f, root.transform, true);

            WetPuddle puddle = root.AddComponent<WetPuddle>();
            puddle.Initialize(this, 0.72f + (i % 3) * 0.1f, trigger,
                new[] { shadow, body, lobeLeft, lobeRight, shine, ripple }, i * 0.67f);
        }
    }

    private void DrawArcadeWeather(float screenWidth, float screenHeight)
    {
        if (!weatherEnabled)
        {
            DrawWeatherHud(screenWidth);
            return;
        }

        float time = Time.unscaledTime;
        float intensity = 0.78f + Mathf.Sin(time * 0.31f + selectedTrackIndex) * 0.16f;

        switch (ActiveWeather)
        {
            case CircuitWeather.Rain:
                DrawRain(screenWidth, screenHeight, time, intensity, false);
                break;
            case CircuitWeather.Thunderstorm:
                DrawRain(screenWidth, screenHeight, time, intensity, true);
                break;
            case CircuitWeather.Snow:
                DrawSnow(screenWidth, screenHeight, time, intensity, false);
                break;
            case CircuitWeather.Blizzard:
                DrawSnow(screenWidth, screenHeight, time, intensity, true);
                break;
            case CircuitWeather.Ash:
                DrawAsh(screenWidth, screenHeight, time, intensity);
                break;
            case CircuitWeather.IonStorm:
                DrawIonStorm(screenWidth, screenHeight, time, intensity);
                break;
            case CircuitWeather.Dust:
                DrawSand(screenWidth, screenHeight, time, intensity * 0.62f);
                break;
            default:
                DrawSand(screenWidth, screenHeight, time, intensity);
                break;
        }

        DrawWeatherHud(screenWidth);
    }

    private void DrawRain(float width, float height, float time, float intensity, bool thunder)
    {
        DrawSolidRect(new Rect(0f, 0f, width, height), new Color(0.01f, 0.07f, 0.13f, 0.08f * intensity));
        int dropCount = thunder ? 104 : 76;
        for (int i = 0; i < dropCount; i++)
        {
            float phase = Mathf.Repeat(time * (1.05f + i % 5 * 0.045f) + i * 0.071f, 1f);
            float x = Mathf.Repeat(i * 139.7f + time * (250f + i % 4 * 22f), width + 180f) - 90f;
            float y = phase * (height + 100f) - 50f;
            float length = 18f + i % 6 * 5f;
            float alpha = (0.16f + i % 4 * 0.035f) * intensity;
            DrawRotatedRect(new Rect(x, y, 2f, length), new Color(0.38f, 0.88f, 1f, alpha), -18f);
        }

        for (int band = 0; band < 4; band++)
        {
            float fogX = Mathf.Repeat(time * (18f + band * 3f) + band * width * 0.31f, width * 1.35f) - width * 0.35f;
            float fogY = height * (0.2f + band * 0.19f);
            DrawSolidRect(new Rect(fogX, fogY, width * 0.46f, 18f + band * 6f), new Color(0.24f, 0.58f, 0.7f, 0.024f * intensity));
        }

        if (!thunder)
        {
            return;
        }

        float lightningPhase = Mathf.Repeat(time + selectedTrackIndex * 0.83f, 6.4f);
        if (lightningPhase < 0.18f)
        {
            float flash = Mathf.Sin(lightningPhase / 0.18f * Mathf.PI);
            DrawSolidRect(new Rect(0f, 0f, width, height), new Color(0.62f, 0.88f, 1f, flash * 0.2f));
            Vector2 point = new Vector2(width * 0.72f, -10f);
            for (int segment = 0; segment < 6; segment++)
            {
                Vector2 next = point + new Vector2((segment % 2 == 0 ? -1f : 1f) * (25f + segment * 4f), 56f + segment * 7f);
                DrawWeatherBolt(point, next, new Color(0.84f, 0.96f, 1f, flash));
                point = next;
            }
        }
    }

    private void DrawWeatherBolt(Vector2 start, Vector2 end, Color color)
    {
        Vector2 delta = end - start;
        float length = delta.magnitude;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
        DrawRotatedRect(new Rect(start.x - 2f, start.y, 5f, length), new Color(color.r, color.g, color.b, color.a * 0.28f), -angle);
        DrawRotatedRect(new Rect(start.x - 1f, start.y, 2f, length), color, -angle);
    }

    private void DrawSnow(float width, float height, float time, float intensity, bool blizzard)
    {
        DrawSolidRect(new Rect(0f, 0f, width, height), new Color(0.28f, 0.62f, 0.78f, (blizzard ? 0.1f : 0.055f) * intensity));
        int flakeCount = blizzard ? 96 : 62;
        Color previous = GUI.color;
        for (int i = 0; i < flakeCount; i++)
        {
            float fallSpeed = 44f + i % 7 * 13f;
            float y = Mathf.Repeat(time * fallSpeed + i * 97.3f, height + 40f) - 20f;
            float x = Mathf.Repeat(i * 173.7f + Mathf.Sin(time * (0.7f + i % 4 * 0.12f) + i) * (blizzard ? 95f : 34f), width + 30f) - 15f;
            float size = 3f + i % 5 * 1.35f;
            GUI.color = new Color(0.88f, 0.98f, 1f, (0.22f + i % 4 * 0.06f) * intensity);
            GUI.DrawTexture(new Rect(x, y, size, size), circleTexture);
            if (blizzard && i % 3 == 0)
            {
                DrawRotatedRect(new Rect(x, y, 2f, 24f + i % 4 * 8f), new Color(0.8f, 0.97f, 1f, 0.18f * intensity), -68f);
            }
        }
        GUI.color = previous;
    }

    private void DrawSand(float width, float height, float time, float intensity)
    {
        DrawSolidRect(new Rect(0f, 0f, width, height), new Color(0.48f, 0.2f, 0.025f, 0.09f * intensity));
        for (int i = 0; i < 70; i++)
        {
            float x = Mathf.Repeat(time * (130f + i % 6 * 22f) + i * 89.7f, width + 180f) - 90f;
            float y = Mathf.Repeat(i * 61.7f + Mathf.Sin(time * 0.9f + i) * 35f, height);
            float length = 12f + i % 7 * 9f;
            float alpha = (0.06f + i % 5 * 0.025f) * intensity;
            DrawSolidRect(new Rect(x, y, length, 2f + i % 3), new Color(1f, 0.64f, 0.16f, alpha));
        }

        for (int band = 0; band < 5; band++)
        {
            float x = Mathf.Repeat(time * (24f + band * 5f) + band * width * 0.27f, width * 1.5f) - width * 0.5f;
            DrawSolidRect(new Rect(x, height * (0.18f + band * 0.17f), width * 0.54f, 20f + band * 9f), new Color(0.85f, 0.34f, 0.035f, 0.022f * intensity));
        }
    }

    private void DrawAsh(float width, float height, float time, float intensity)
    {
        DrawSolidRect(new Rect(0f, 0f, width, height), new Color(0.12f, 0.015f, 0.01f, 0.065f * intensity));
        for (int i = 0; i < 68; i++)
        {
            float y = Mathf.Repeat(time * (25f + i % 5 * 9f) + i * 83.2f, height + 30f) - 15f;
            float x = Mathf.Repeat(i * 151.4f + Mathf.Sin(time * 0.63f + i) * 42f, width);
            float size = 2f + i % 5;
            Color ash = i % 8 == 0 ? new Color(1f, 0.22f, 0.02f, 0.48f * intensity) : new Color(0.62f, 0.58f, 0.56f, 0.25f * intensity);
            DrawRotatedRect(new Rect(x, y, size, size * 2.2f), ash, i * 31f + time * 18f);
        }
    }

    private void DrawIonStorm(float width, float height, float time, float intensity)
    {
        float pulse = 0.5f + Mathf.Sin(time * 2.7f) * 0.5f;
        DrawSolidRect(new Rect(0f, 0f, width, height), new Color(0.3f, 0.08f, 0.48f, (0.035f + pulse * 0.025f) * intensity));
        for (int i = 0; i < 54; i++)
        {
            float phase = Mathf.Repeat(time * (0.25f + i % 4 * 0.04f) + i * 0.113f, 1f);
            float x = Mathf.Repeat(i * 191.3f + time * 44f, width);
            float y = phase * height;
            float size = 3f + phase * 7f;
            Color ion = i % 2 == 0 ? new Color(0.72f, 0.36f, 1f, 0.38f * intensity) : new Color(0.18f, 0.92f, 1f, 0.3f * intensity);
            DrawRotatedRect(new Rect(x, y, size, size), ion, 45f);
        }

        float waveY = Mathf.Repeat(time * 78f, height + 100f) - 50f;
        DrawSolidRect(new Rect(0f, waveY - 8f, width, 17f), new Color(0.62f, 0.28f, 1f, 0.025f));
        DrawSolidRect(new Rect(0f, waveY, width, 2f), new Color(0.28f, 0.9f, 1f, 0.15f));
    }

    private void DrawWeatherHud(float screenWidth)
    {
        Color accent = WeatherColor;
        Rect panel = new Rect(screenWidth * 0.5f - 158f, 20f, 316f, 43f);
        DrawSolidRect(new Rect(panel.x + 5f, panel.y + 6f, panel.width, panel.height), new Color(0f, 0f, 0f, 0.55f));
        DrawSolidRect(panel, new Color(0.004f, 0.016f, 0.045f, 0.92f));
        DrawSolidRect(new Rect(panel.x, panel.y, panel.width, 3f), accent);
        DrawSolidRect(new Rect(panel.x, panel.y, 4f, panel.height), accent);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 7f, 78f, 18f), "WEATHER", arcadeMicroStyle);
        GUI.Label(new Rect(panel.x + 90f, panel.y + 5f, panel.width - 110f, 24f), WeatherName, arcadeSmallStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 25f, panel.width - 28f, 15f), "СЦЕПЛЕНИЕ  " + Mathf.RoundToInt(WeatherGripMultiplier * 100f) + "%", arcadeMicroStyle);
    }

    private void DrawWeatherPreview(Rect previewPanel)
    {
        Color accent = WeatherColor;
        float time = Time.unscaledTime;
        if (weatherEnabled)
        {
            for (int i = 0; i < 22; i++)
            {
                float phase = Mathf.Repeat(time * (0.34f + i % 4 * 0.04f) + i * 0.103f, 1f);
                float x = previewPanel.x + Mathf.Repeat(i * 71.3f + time * 36f, previewPanel.width - 18f) + 9f;
                float y = previewPanel.y + 10f + phase * (previewPanel.height - 67f);
                if (ActiveWeather == CircuitWeather.Rain || ActiveWeather == CircuitWeather.Thunderstorm || ActiveWeather == CircuitWeather.Blizzard)
                {
                    DrawRotatedRect(new Rect(x, y, 2f, 12f), new Color(accent.r, accent.g, accent.b, 0.32f), -18f);
                }
                else
                {
                    DrawSolidRect(new Rect(x, y, 4f + i % 3 * 2f, 3f), new Color(accent.r, accent.g, accent.b, 0.3f));
                }
            }
        }

        Rect chip = new Rect(previewPanel.x + 12f, previewPanel.y + 10f, Mathf.Min(188f, previewPanel.width - 24f), 25f);
        DrawSolidRect(chip, new Color(0.002f, 0.01f, 0.035f, 0.88f));
        DrawSolidRect(new Rect(chip.x, chip.y, 4f, chip.height), accent);
        GUI.Label(new Rect(chip.x + 10f, chip.y + 4f, chip.width - 16f, 17f), "WEATHER / " + WeatherName, arcadeMicroStyle);
    }

    private void LoadWeatherSetting()
    {
        weatherEnabled = PlayerPrefs.GetInt(WeatherEnabledKey, 1) != 0;
    }

    private void ToggleWeather()
    {
        PlayMenuClickSfx();
        weatherEnabled = !weatherEnabled;
        PlayerPrefs.SetInt(WeatherEnabledKey, weatherEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
