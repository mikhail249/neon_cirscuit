using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed partial class NeonCircuitGame
{
    private const string TutorialCompletedKey = "NeonCircuit.TutorialCompleted";
    private enum TutorialPhase { Welcome, Practice, Praise, Help, Complete }

    private sealed class DrivingLesson
    {
        public readonly string Title, Keys, Objective, Dialogue, Hint, Praise;
        public DrivingLesson(string title, string keys, string objective, string dialogue, string hint, string praise)
        {
            Title = title; Keys = keys; Objective = objective;
            Dialogue = dialogue; Hint = hint; Praise = praise;
        }
    }

    private static readonly DrivingLesson[] DrivingLessons =
    {
        new DrivingLesson("ЗНАКОМСТВО С ГАЗОМ", "W / ↑", "Разгонись до 35 км/ч.",
            "Я Рук, механик твоей команды. Держи W — поедем вперёд. Педаль газа справа. Хотя на клавиатуре она почему-то сверху...",
            "Удерживай W или стрелку вверх. Не отпускай, пока скорость не достигнет 35 км/ч. Следи за числом внизу.",
            "Есть движение! Важный этап: раньше машина была мебелью, а теперь — транспортом."),
        new DrivingLesson("РУЛЬ — ЭТО НЕ УКРАШЕНИЕ", "W + A / D", "На ходу немного поверни влево, затем вправо.",
            "Газ держим, руль пробуем. Коротко нажми A, потом D. Машина поворачивает носом, а не телепортируется в соседнюю полосу!",
            "Сначала разгонись с W. На скорости выше 10 км/ч удержи A примерно четверть секунды, затем D. Стрелки тоже работают.",
            "Лево есть. Право есть. Для полноценного водителя осталось научиться не путать их, когда я кричу."),
        new DrivingLesson("ТОРМОЗА ПРИДУМАЛИ УМНЫЕ", "W → S / ↓", "Разгонись до 25 км/ч, затем затормози до 10.",
            "Проверим тормоза до того, как их проверит стена. Набери 25 на W, отпусти газ и зажми S. Чуть постоим красиво.",
            "Набери минимум 25 км/ч. Отпусти W и удерживай S, пока скорость не упадёт ниже 10. Если держать S дальше, машина поедет назад.",
            "Остановились! Мой любимый звук — это звук ремонта, который не понадобился."),
        new DrivingLesson("ПОВОРОТ С ХАРАКТЕРОМ", "W + SHIFT + A / D", "Разгонись до 60 км/ч и подрифтуй полсекунды.",
            "Теперь учимся входить в поворот эффектно. На асфальте набери 60, зажми Shift и слегка поверни. Да, дым от шин сейчас — хороший знак!",
            "Дрифт работает на асфальте. Удерживай W до 60 км/ч, затем добавь Shift и A или D. Хватит короткого заноса; прогресс не теряется. R вернёт на учебную прямую.",
            "Красиво! Следы на асфальте оставим. Подпишем: «Современное искусство, автор — ты»."),
        new DrivingLesson("КНОПКА «УХ ТЫ!»", "W + X", "Удерживай газ и нитро полсекунды.",
            "X — нитро. Нажимай вместе с газом и держи нос прямо. Если улыбка шире лобового стекла — значит, работает.",
            "Продолжай держать W и добавь X на полсекунды. Нитро расходует запас, а после отпускания X постепенно восстанавливается.",
            "Вот это рывок! Кофе я, конечно, разлил. Но ради такого старта готов сварить новый."),
        new DrivingLesson("ЧТО У НАС ПОД КАПОТОМ?", "Q", "Переключи оружие один раз.",
            "Кроме мотора тут есть сюрприз. Нажми Q и посмотри, как сменится название оружия. Обещаю: чайник к этой кнопке не подключён.",
            "Коротко нажми Q. Оружие переключается даже на месте. Его название видно внизу; заблокированные виды автоматически пропускаются.",
            "Переключилось! Это плазма. Не путай с фарами: дальний свет у нас гораздо вежливее."),
        new DrivingLesson("ПРОБНЫЙ ЗАЛП", "SPACE", "Выпусти одну учебную ракету.",
            "Вернул тебе ракеты и выдал боезапас. Нажми пробел — можно прямо с места. По учебной трассе сегодня летят только хорошие намерения.",
            "Коротко нажми пробел. Ракета летит вперёд от носа машины. В обычном заезде боезапас пополняют зелёные предметы на трассе.",
            "Пуск засчитан! Теперь ты умеешь ездить, тормозить, дрифтовать и убедительно просить дорогу.")
    };

    private static bool pendingDrivingTutorial;
    private bool tutorialActive;
    private TutorialPhase tutorialPhase;
    private int tutorialLesson;
    private float tutorialElapsed, tutorialProgress, tutorialActionTime;
    private float tutorialLeftTurn, tutorialRightTurn, tutorialPreviousRotation;
    private float tutorialStartParameter;
    private bool tutorialBrakeReady, tutorialFunnyReply;
    private CarWeaponType tutorialInitialWeapon;
    private int tutorialInitialShots;
    private float tutorialInputReadyAt;
    private GUIStyle tutorialBodyStyle, tutorialTitleStyle, tutorialButtonStyle;

    public bool IsTutorialActive { get { return tutorialActive; } }
    public bool IsTutorialPracticing { get { return tutorialActive && tutorialPhase == TutorialPhase.Practice; } }
    public int TutorialLessonIndex { get { return tutorialLesson; } }

    private bool TutorialShortcutPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.T);
#endif
    }

    private bool TutorialHelpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.H);
#endif
    }

    private void StartDrivingTutorial()
    {
        if (trackLoadPending || tutorialActive) return;
        pendingDrivingTutorial = true;
        LaunchArcadeRaceMode(ArcadeRaceMode.Standard);
    }

    private void BeginDrivingTutorial()
    {
        pendingDrivingTutorial = false;
        tutorialActive = true;
        tutorialLesson = 0;
        tutorialPhase = TutorialPhase.Welcome;
        tutorialFunnyReply = false;
        ConfigureRaceOpponents(0);
        SetTrackObstaclesEnabled(false);
        raceStarted = true;
        countdown = 0f;
        tutorialStartParameter = FindTutorialStraight();
        ResetTutorialCar();
        Time.timeScale = 0f;
        tutorialInputReadyAt = Time.unscaledTime + 0.25f;
    }

    private float FindTutorialStraight()
    {
        float bestParameter = 0f;
        float bestBend = float.PositiveInfinity;
        for (int i = 0; i < 128; i++)
        {
            float t = i * Mathf.PI * 2f / 128f;
            Vector2 direction = PathDerivative(t);
            float bend = Vector2.Angle(direction, PathDerivative(t + 0.12f))
                + Vector2.Angle(direction, PathDerivative(t + 0.24f));
            if (bend < bestBend)
            {
                bestBend = bend;
                bestParameter = t;
            }
        }
        return bestParameter;
    }

    private void ResetTutorialCar()
    {
        player.ResetToStart();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 position = PathPoint(tutorialStartParameter, 0f);
        float rotation = PathRotation(tutorialStartParameter);
        player.transform.SetPositionAndRotation(new Vector3(position.x, position.y, player.transform.position.z), Quaternion.Euler(0f, 0f, rotation));
        body.position = position;
        body.rotation = rotation;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        if (Camera.main != null)
        {
            SmoothRaceCamera followCamera = Camera.main.GetComponent<SmoothRaceCamera>();
            if (followCamera != null) followCamera.SnapToTarget();
        }
        tutorialPreviousRotation = rotation;
        playerWeapon.ResetWeapon();
        playerWeapon.EquipWeapon(CarWeaponType.NeonRocket);
        playerWeapon.TryAddAmmo(playerWeapon.MaxAmmo);
        tutorialInitialWeapon = playerWeapon.ActiveWeapon;
        tutorialInitialShots = playerWeapon.ShotsFired;
    }

    private void BeginTutorialLesson()
    {
        ResetTutorialCar();
        tutorialElapsed = tutorialProgress = tutorialActionTime = 0f;
        tutorialLeftTurn = tutorialRightTurn = 0f;
        tutorialBrakeReady = false;
        tutorialFunnyReply = false;
        tutorialPhase = TutorialPhase.Practice;
        Time.timeScale = 1f;
        tutorialInputReadyAt = Time.unscaledTime + 0.2f;
    }

    private void AdvanceDrivingTutorial()
    {
        if (!tutorialActive || Time.unscaledTime < tutorialInputReadyAt) return;
        if (tutorialPhase == TutorialPhase.Help)
        {
            tutorialPhase = TutorialPhase.Practice;
            Time.timeScale = 1f;
        }
        else if (tutorialPhase == TutorialPhase.Welcome)
        {
            BeginTutorialLesson();
        }
        else if (tutorialPhase == TutorialPhase.Praise)
        {
            tutorialLesson++;
            BeginTutorialLesson();
        }
        else if (tutorialPhase == TutorialPhase.Complete)
        {
            OpenMainMenu();
            OpenStoryMode();
        }
    }

    private void UpdateDrivingTutorial()
    {
        if (CancelPressed()) { OpenMainMenu(); return; }
        if (Time.unscaledTime < tutorialInputReadyAt) return;
        if (ConfirmPressed()) { AdvanceDrivingTutorial(); return; }
        if (tutorialPhase != TutorialPhase.Practice) return;
        if (RestartPressed()) { BeginTutorialLesson(); return; }
        if (TutorialHelpPressed()) { PauseTutorialForHelp(); return; }

        tutorialElapsed += Time.deltaTime;
        float speed = player.SpeedKph;
        switch (tutorialLesson)
        {
            case 0:
                tutorialProgress = Mathf.Clamp01(speed / 35f);
                if (player.ThrottleInput <= 0f) tutorialProgress = Mathf.Min(tutorialProgress, 0.99f);
                break;
            case 1:
                float turn = Mathf.DeltaAngle(tutorialPreviousRotation, player.transform.eulerAngles.z);
                if (speed >= 10f && Mathf.Abs(player.SteeringInput) > 0.1f)
                {
                    if (turn > 0f) tutorialLeftTurn += turn;
                    else tutorialRightTurn -= turn;
                }
                tutorialProgress = (Mathf.Clamp01(tutorialLeftTurn / 12f) + Mathf.Clamp01(tutorialRightTurn / 12f)) * 0.5f;
                break;
            case 2:
                tutorialBrakeReady |= speed >= 25f;
                if (tutorialBrakeReady && player.ThrottleInput < -0.1f) tutorialActionTime += Time.deltaTime;
                tutorialProgress = tutorialBrakeReady ? 0.5f : Mathf.Clamp01(speed / 25f) * 0.5f;
                if (tutorialBrakeReady && tutorialActionTime >= 0.12f && player.ThrottleInput < -0.1f && speed <= 10f) tutorialProgress = 1f;
                break;
            case 3:
                if (player.IsDrifting) tutorialActionTime += Time.deltaTime;
                tutorialProgress = Mathf.Clamp01(tutorialActionTime / 0.5f);
                break;
            case 4:
                if (player.IsNitroActive) tutorialActionTime += Time.deltaTime;
                tutorialProgress = Mathf.Clamp01(tutorialActionTime / 0.5f);
                break;
            case 5:
                tutorialProgress = playerWeapon.ActiveWeapon != tutorialInitialWeapon ? 1f : 0f;
                break;
            case 6:
                tutorialProgress = playerWeapon.ShotsFired > tutorialInitialShots ? 1f : 0f;
                if (playerWeapon.Ammo < playerWeapon.ActiveWeaponAmmoCost) playerWeapon.TryAddAmmo(playerWeapon.MaxAmmo);
                break;
        }
        tutorialPreviousRotation = player.transform.eulerAngles.z;
        if (tutorialProgress >= 1f) CompleteTutorialLesson();
    }

    private void CompleteTutorialLesson()
    {
        Time.timeScale = 0f;
        tutorialFunnyReply = false;
        tutorialPhase = tutorialLesson == DrivingLessons.Length - 1 ? TutorialPhase.Complete : TutorialPhase.Praise;
        tutorialInputReadyAt = Time.unscaledTime + 0.35f;
        PlayPickupSfx(false);
        if (tutorialPhase == TutorialPhase.Complete)
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 1);
            PlayerPrefs.Save();
        }
    }

    private void PauseTutorialForHelp()
    {
        tutorialPhase = TutorialPhase.Help;
        Time.timeScale = 0f;
        tutorialInputReadyAt = Time.unscaledTime + 0.2f;
    }

    private void EndDrivingTutorial()
    {
        tutorialActive = false;
        pendingDrivingTutorial = false;
        playerWeapon.ResetWeapon();
        player.ResetToStart();
        raceStarted = false;
    }
}
