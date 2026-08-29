using UnityEngine;

public sealed partial class NeonCircuitGame
{
    private const float ArcadeDriftTarget = 3000f;
    private const int ArcadeDriftReward = 450;

    private enum ArcadeRaceMode
    {
        Standard,
        DriftChallenge,
        HeavyTruck,
        Motorcycle
    }

    private ArcadeRaceMode arcadeRaceMode;
    private static ArcadeRaceMode pendingArcadeRaceMode;
    private static int arcadeReturnCarIndex = -1;
    private float arcadeDriftScore;
    private float arcadeBestDriftCombo;
    private bool arcadeModeResolved;
    private bool arcadeModeSucceeded;
    private int arcadeModeReward;
    private int arcadeFinishPosition;
    private string arcadeModeResultMessage = string.Empty;

    private bool IsDriftChallenge
    {
        get { return !storyRaceActive && arcadeRaceMode == ArcadeRaceMode.DriftChallenge; }
    }

    private bool IsHeavyTruckRace
    {
        get { return !storyRaceActive && arcadeRaceMode == ArcadeRaceMode.HeavyTruck; }
    }

    private bool IsMotorcycleRace
    {
        get { return !storyRaceActive && arcadeRaceMode == ArcadeRaceMode.Motorcycle; }
    }

    private bool IsSpecialArcadeRace
    {
        get { return IsDriftChallenge || IsHeavyTruckRace || IsMotorcycleRace; }
    }

    private float ArcadeAccelerationFactor
    {
        get
        {
            if (IsDriftChallenge) return 1.06f;
            if (IsHeavyTruckRace) return 0.74f;
            if (IsMotorcycleRace) return 1.2f;
            return 1f;
        }
    }

    private float ArcadeTopSpeedFactor
    {
        get
        {
            if (IsDriftChallenge) return 1.04f;
            if (IsHeavyTruckRace) return 0.86f;
            if (IsMotorcycleRace) return 1.16f;
            return 1f;
        }
    }

    private float ArcadeHandlingFactor
    {
        get
        {
            if (IsDriftChallenge) return 1.2f;
            if (IsHeavyTruckRace) return 0.62f;
            if (IsMotorcycleRace) return 1.3f;
            return 1f;
        }
    }

    private float ArcadeDamageFactor
    {
        get
        {
            if (IsDriftChallenge) return 0.92f;
            if (IsHeavyTruckRace) return 0.55f;
            if (IsMotorcycleRace) return 1.22f;
            return 1f;
        }
    }

    private void LaunchArcadeRaceMode(ArcadeRaceMode mode)
    {
        arcadeRaceMode = mode;
        pendingArcadeRaceMode = mode;
        if (mode != ArcadeRaceMode.Standard && arcadeReturnCarIndex < 0)
        {
            arcadeReturnCarIndex = selectedCarIndex;
        }

        StartRaceFromMenu();
    }

    private void PrepareArcadeRaceStart()
    {
        pendingArcadeRaceMode = arcadeRaceMode;
        if (arcadeRaceMode == ArcadeRaceMode.DriftChallenge)
        {
            selectedCarIndex = 2;
            garageCarIndex = selectedCarIndex;
            if (opponents.Count > 0) ConfigureRaceOpponents(0);
        }
        else if (arcadeRaceMode == ArcadeRaceMode.HeavyTruck)
        {
            selectedCarIndex = 3;
            garageCarIndex = selectedCarIndex;
            if (opponents.Count > 0) ConfigureRaceOpponents(5);
        }
        else if (arcadeRaceMode == ArcadeRaceMode.Motorcycle)
        {
            selectedCarIndex = 1;
            garageCarIndex = selectedCarIndex;
            if (opponents.Count > 0) ConfigureRaceOpponents(6);
        }
        else if (opponents.Count > 0)
        {
            ConfigureRaceOpponents(opponents.Count);
        }
    }

    private void PreparePendingArcadeRaceBeforeWorldBuild()
    {
        arcadeRaceMode = startRaceAfterSceneReload ? pendingArcadeRaceMode : ArcadeRaceMode.Standard;
        if (arcadeRaceMode == ArcadeRaceMode.DriftChallenge)
        {
            selectedCarIndex = 2;
            garageCarIndex = selectedCarIndex;
        }
        else if (arcadeRaceMode == ArcadeRaceMode.HeavyTruck)
        {
            selectedCarIndex = 3;
            garageCarIndex = selectedCarIndex;
        }
        else if (arcadeRaceMode == ArcadeRaceMode.Motorcycle)
        {
            selectedCarIndex = 1;
            garageCarIndex = selectedCarIndex;
        }
    }

    private void RestorePendingArcadeRaceAfterReload()
    {
        arcadeRaceMode = pendingArcadeRaceMode;
        PrepareArcadeRaceStart();
        ResetArcadeRaceProgress();
    }

    private void ExitArcadeRaceModeToMenu()
    {
        bool hadSpecialMode = arcadeRaceMode != ArcadeRaceMode.Standard;
        arcadeRaceMode = ArcadeRaceMode.Standard;
        pendingArcadeRaceMode = ArcadeRaceMode.Standard;
        if (opponents.Count > 0)
        {
            ConfigureRaceOpponents(opponents.Count);
        }

        if (hadSpecialMode && arcadeReturnCarIndex >= 0)
        {
            selectedCarIndex = Mathf.Clamp(arcadeReturnCarIndex, 0, CarNames.Length - 1);
            garageCarIndex = selectedCarIndex;
            arcadeReturnCarIndex = -1;
            ApplySelectedCarVisuals();
        }

        ResetArcadeRaceProgress();
    }

    private void ResetArcadeRaceProgress()
    {
        arcadeDriftScore = 0f;
        arcadeBestDriftCombo = 0f;
        arcadeModeResolved = false;
        arcadeModeSucceeded = false;
        arcadeModeReward = 0;
        arcadeFinishPosition = 0;
        arcadeModeResultMessage = string.Empty;
    }

    private void UpdateArcadeRaceProgress()
    {
        if (!IsDriftChallenge || player == null || !raceStarted || raceFinished)
        {
            return;
        }

        arcadeBestDriftCombo = Mathf.Max(arcadeBestDriftCombo, player.DriftCombo);
        if (player.IsDrifting)
        {
            arcadeDriftScore += Time.deltaTime * (52f + player.DriftCombo * 1.45f);
        }
    }

    private void ResolveArcadeRaceAtFinish()
    {
        if (!IsSpecialArcadeRace || arcadeModeResolved)
        {
            return;
        }

        arcadeModeResolved = true;
        arcadeFinishPosition = RacePosition();
        if (IsDriftChallenge)
        {
            arcadeModeSucceeded = arcadeDriftScore >= ArcadeDriftTarget;
            arcadeModeReward = arcadeModeSucceeded ? ArcadeDriftReward : 0;
            arcadeModeResultMessage = arcadeModeSucceeded
                ? "ЦЕЛЬ ВЫПОЛНЕНА  /  ДРИФТ-СЕРИЯ ЗАСЧИТАНА"
                : "НУЖНО " + Mathf.RoundToInt(ArcadeDriftTarget) + " ОЧКОВ  /  ПОПРОБУЙТЕ ЕЩЁ РАЗ";
        }
        else if (IsHeavyTruckRace)
        {
            arcadeModeSucceeded = true;
            arcadeModeReward = arcadeFinishPosition == 1 ? 450 : arcadeFinishPosition <= 3 ? 250 : 120;
            arcadeModeResultMessage = arcadeFinishPosition == 1
                ? "ПОБЕДА ТЯЖЁЛОЙ ЛИГИ  /  КОЛОННА ПОКОРЕНА"
                : "ТЯЖЁЛАЯ ГОНКА ЗАВЕРШЕНА  /  ПОЗИЦИЯ " + arcadeFinishPosition;
        }
        else
        {
            arcadeModeSucceeded = true;
            arcadeModeReward = arcadeFinishPosition == 1 ? 500 : arcadeFinishPosition <= 3 ? 300 : 140;
            arcadeModeResultMessage = arcadeFinishPosition == 1
                ? "ПОБЕДА МОТО-ЛИГИ  /  VOLT BIKE X НА ФИНИШЕ ПЕРВЫМ"
                : "МОТОГОНКА ЗАВЕРШЕНА  /  ПОЗИЦИЯ " + arcadeFinishPosition;
        }

        if (arcadeModeReward > 0)
        {
            lastFinishReward += arcadeModeReward;
            AddCoins(arcadeModeReward);
        }
    }

    private int MainMenuModePreviewCarIndex
    {
        get
        {
            if (mainMenuModeSelectionOpen && mainMenuSelectedMode == 2) return 2;
            if (mainMenuModeSelectionOpen && mainMenuSelectedMode == 3) return 3;
            if (mainMenuModeSelectionOpen && mainMenuSelectedMode == 4) return 1;
            return selectedCarIndex;
        }
    }

    private Sprite MainMenuSpecialVehiclePreviewSprite
    {
        get
        {
            if (!mainMenuModeSelectionOpen) return null;
            if (mainMenuSelectedMode == 3) return storyTruckSprite;
            if (mainMenuSelectedMode == 4) return storyMotorcycleSprite;
            return null;
        }
    }

    private void DrawArcadeModeHud(float screenWidth, float screenHeight)
    {
        if (!IsSpecialArcadeRace)
        {
            return;
        }

        Color accent = IsDriftChallenge ? ArcadeCyan : IsMotorcycleRace ? ArcadeViolet : ArcadeYellow;
        Rect panel = new Rect(screenWidth * 0.5f - 300f, 18f, 600f, 112f);
        DrawArcadePanel(panel, accent, ArcadePink);
        GUI.Label(
            new Rect(panel.x + 18f, panel.y + 9f, panel.width - 36f, 20f),
            IsDriftChallenge ? "ARCADE  /  DRIFT CHALLENGE"
                : IsMotorcycleRace ? "ARCADE  /  MOTORCYCLE LEAGUE"
                : "ARCADE  /  HEAVY TRUCK LEAGUE",
            arcadeMicroStyle);

        Color previous = GUI.color;
        GUI.color = accent;
        GUI.Label(
            new Rect(panel.x + 18f, panel.y + 31f, panel.width - 36f, 38f),
            IsDriftChallenge ? "НАБЕРИ " + Mathf.RoundToInt(ArcadeDriftTarget) + " ОЧКОВ ДРИФТА"
                : IsMotorcycleRace ? "МОТОГОНКА: VOLT BIKE X"
                : "ГОНКА ТЯЖЁЛЫХ ГРУЗОВИКОВ",
            arcadeHeadingStyle);
        GUI.color = previous;

        if (IsDriftChallenge)
        {
            float ratio = arcadeDriftScore / ArcadeDriftTarget;
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 72f, panel.width - 180f, 19f),
                "DRIFT  " + Mathf.RoundToInt(arcadeDriftScore) + " / " + Mathf.RoundToInt(ArcadeDriftTarget)
                    + "    MAX COMBO  " + Mathf.RoundToInt(arcadeBestDriftCombo),
                arcadeMicroStyle);
            DrawArcadeSegments(new Rect(panel.x + 18f, panel.y + 96f, panel.width - 36f, 7f), ratio, accent, 20);
        }
        else if (IsHeavyTruckRace)
        {
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 77f, panel.width - 36f, 19f),
                "POSITION  " + RacePosition() + " / " + (ActiveOpponentCount + 1)
                    + "    MASS  2700 KG    MAX  " + Mathf.RoundToInt(TruckMaximumSpeedKph) + " KM/H",
                arcadeMicroStyle);
        }
        else
        {
            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 77f, panel.width - 36f, 19f),
                "POSITION  " + RacePosition() + " / " + (ActiveOpponentCount + 1)
                    + "    MASS  620 KG    HIGH SPEED / LOW ARMOR",
                arcadeMicroStyle);
        }
    }

    private void DrawArcadeModeFinishOverlay(float screenWidth, float screenHeight)
    {
        Color resultColor = IsDriftChallenge
            ? arcadeModeSucceeded ? ArcadeLime : ArcadePink
            : IsMotorcycleRace ? ArcadeViolet : ArcadeYellow;
        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.002f, 0.004f, 0.018f, 0.9f));
        Rect panel = new Rect(screenWidth * 0.5f - 410f, screenHeight * 0.5f - 270f, 820f, 540f);
        DrawArcadePanel(panel, resultColor, ArcadeCyan);
        GUI.Label(
            new Rect(panel.x + 26f, panel.y + 18f, panel.width - 52f, 26f),
            IsDriftChallenge ? "DRIFT CHALLENGE  //  RESULT"
                : IsMotorcycleRace ? "MOTORCYCLE LEAGUE  //  RESULT"
                : "HEAVY TRUCK LEAGUE  //  RESULT",
            arcadeSmallStyle);

        Color previous = GUI.color;
        GUI.color = resultColor;
        GUI.Label(
            new Rect(panel.x + 26f, panel.y + 50f, panel.width - 52f, 72f),
            IsDriftChallenge
                ? arcadeModeSucceeded ? "ДРИФТ ЗАСЧИТАН!" : "ЦЕЛЬ НЕ ВЫПОЛНЕНА"
                : IsMotorcycleRace
                    ? arcadeFinishPosition == 1 ? "ПОБЕДА МОТОЦИКЛА!" : "ФИНИШ МОТО-ЛИГИ"
                    : arcadeFinishPosition == 1 ? "ПОБЕДА ГРУЗОВИКА!" : "ФИНИШ ТЯЖЁЛОЙ ЛИГИ",
            arcadeTitleStyle);
        GUI.color = previous;
        GUI.Label(new Rect(panel.x + 28f, panel.y + 123f, panel.width - 56f, 26f), arcadeModeResultMessage, arcadeCenteredStyle);

        float metricGap = 12f;
        float metricWidth = (panel.width - 56f - metricGap * 2f) / 3f;
        string firstLabel = IsDriftChallenge ? "DRIFT SCORE" : "POSITION";
        string firstValue = IsDriftChallenge
            ? Mathf.RoundToInt(arcadeDriftScore) + " / " + Mathf.RoundToInt(ArcadeDriftTarget)
            : Mathf.Max(1, arcadeFinishPosition) + " / " + (ActiveOpponentCount + 1);
        DrawArcadeMetric(new Rect(panel.x + 28f, panel.y + 170f, metricWidth, 108f), firstLabel, firstValue, ArcadeCyan);
        DrawArcadeMetric(new Rect(panel.x + 28f + metricWidth + metricGap, panel.y + 170f, metricWidth, 108f), "TOTAL TIME", FormatTime(finishTime), resultColor);
        DrawArcadeMetric(new Rect(panel.x + 28f + (metricWidth + metricGap) * 2f, panel.y + 170f, metricWidth, 108f), "MODE BONUS", arcadeModeReward + " COINS", ArcadeYellow);

        if (DrawArcadeButton(new Rect(panel.x + 28f, panel.y + 322f, 370f, 92f), "R", "ЕЩЁ РАЗ", "RESTART MODE", ArcadePink, true))
        {
            RestartRace();
        }
        if (DrawArcadeButton(new Rect(panel.x + 422f, panel.y + 322f, 370f, 92f), "ESC", "В МЕНЮ", "SELECT ANOTHER MODE", ArcadeCyan, false))
        {
            OpenMainMenu();
        }
        GUI.Label(
            new Rect(panel.x + 28f, panel.y + 452f, panel.width - 56f, 34f),
            IsDriftChallenge
                ? "SHIFT — ДРИФТ  /  ДЕРЖИТЕ ЗАНОС, ЧТОБЫ РАСТИЛ КОМБО"
                : IsMotorcycleRace
                    ? "МОТОЦИКЛ БЫСТРО РАЗГОНЯЕТСЯ, НО ПОЛУЧАЕТ БОЛЬШЕ УРОНА"
                    : "ТЯЖЁЛАЯ МАШИНА МЕДЛЕННЕЕ ПОВОРАЧИВАЕТ, НО ЛУЧШЕ ДЕРЖИТ УДАР",
            arcadeCenteredStyle);
    }
}
