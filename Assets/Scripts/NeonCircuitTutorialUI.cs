using UnityEngine;

public sealed partial class NeonCircuitGame
{
    private void EnsureTutorialStyles()
    {
        EnsureStoryStyles();
        EnsureControlStyles();
        if (tutorialBodyStyle != null) return;
        tutorialBodyStyle = new GUIStyle(storyBodyStyle) { fontSize = 20, wordWrap = true };
        tutorialTitleStyle = new GUIStyle(arcadeHeadingStyle) { fontSize = 27, wordWrap = true, alignment = TextAnchor.MiddleLeft };
        tutorialButtonStyle = new GUIStyle(arcadeSmallStyle) { fontSize = 19, wordWrap = true, alignment = TextAnchor.MiddleCenter };
    }

    private void DrawTutorialPortrait(Rect rect)
    {
        DrawSolidRect(rect, new Color(0.06f, 0.055f, 0.035f, 1f));
        if (storyCharacterSheet != null)
        {
            float bob = Mathf.Sin(Time.unscaledTime * 2.4f) * 2f;
            GUI.DrawTextureWithTexCoords(new Rect(rect.x + 4f, rect.y + 4f + bob, rect.width - 8f, rect.height - 8f), storyCharacterSheet, new Rect(0.5f, 0.5f, 0.5f, 0.5f), true);
        }
        for (int i = 0; i < 8; i++)
        {
            float height = 3f + (Mathf.Sin(Time.unscaledTime * 5f + i * 1.7f) * 0.5f + 0.5f) * 12f;
            DrawSolidRect(new Rect(rect.x + 9f + i * 7f, rect.yMax - height - 5f, 4f, height), ArcadeYellow);
        }
    }

    private bool DrawTutorialButton(Rect rect, string text)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        DrawControlSurface(rect, hovered);
        GUI.Label(new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, rect.height - 8f), text, tutorialButtonStyle);
        if (!GUI.Button(rect, GUIContent.none, GUIStyle.none)) return false;
        PlayMenuClickSfx();
        return true;
    }

    private void DrawTutorialMenuEntry(Rect rect)
    {
        EnsureTutorialStyles();
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = previous * Matrix4x4.TRS(new Vector3(rect.x, rect.y, 0f), Quaternion.identity, new Vector3(rect.width / 486f, rect.height / 88f, 1f));
        bool completed = PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
        DrawSolidRect(new Rect(0f, 0f, 486f, 88f), new Color(0.024f, 0.048f, 0.045f, 0.98f));
        DrawTutorialPortrait(new Rect(5f, 5f, 78f, 78f));
        GUI.Label(new Rect(96f, 9f, 380f, 33f), "ОБУЧЕНИЕ С РУКОМ", tutorialTitleStyle);
        GUI.Label(new Rect(98f, 46f, 375f, 27f), completed ? "T  /  ПРОЙДЕНО · ПОВТОРИТЬ" : "T  /  7 ПРИЁМОВ · БЕЗ РИСКА", arcadeSmallStyle);
        DrawSolidRect(new Rect(0f, 0f, 486f, 3f), ArcadeLime);
        bool clicked = GUI.Button(new Rect(0f, 0f, 486f, 88f), GUIContent.none, GUIStyle.none);
        GUI.matrix = previous;
        if (clicked) { PlayMenuClickSfx(); StartDrivingTutorial(); }
    }

    private void DrawDrivingTutorialGui(float screenWidth, float screenHeight)
    {
        EnsureTutorialStyles();
        if (tutorialPhase != TutorialPhase.Practice)
        {
            DrawTutorialConversation(screenWidth, screenHeight);
            return;
        }

        DrivingLesson lesson = DrivingLessons[tutorialLesson];
        float width = Mathf.Min(1060f, screenWidth - 40f);
        float textWidth = width - 140f;
        string speech = tutorialElapsed >= 14f ? lesson.Hint : lesson.Dialogue;
        float speechHeight = tutorialBodyStyle.CalcHeight(new GUIContent(speech), textWidth);
        Rect radio = new Rect((screenWidth - width) * 0.5f, 20f, width, Mathf.Max(144f, speechHeight + 78f));
        DrawControlSurface(radio);
        DrawTutorialPortrait(new Rect(radio.x + 15f, radio.y + 20f, 100f, 100f));
        GUI.Label(new Rect(radio.x + 130f, radio.y + 15f, textWidth, 28f), "РУК  //  МЕХАНИК НА СВЯЗИ", arcadeCompactHeadingStyle);
        GUI.Label(new Rect(radio.x + 130f, radio.y + 53f, textWidth, speechHeight), speech, tutorialBodyStyle);

        float footerWidth = Mathf.Min(780f, screenWidth - 40f);
        Rect footer = new Rect((screenWidth - footerWidth) * 0.5f, screenHeight - 84f, footerWidth, 59f);
        string telemetry = "СКОРОСТЬ  " + Mathf.RoundToInt(player.SpeedKph) + " км/ч   ·   НИТРО  " + Mathf.CeilToInt(player.NitroFuel) + "%";
        if (tutorialLesson >= 5) telemetry = playerWeapon.ActiveWeaponName + "   ·   ЗАРЯДЫ  " + playerWeapon.Ammo;
        float buttonWidth = (footer.width - 16f) / 3f;
        if (DrawKeyboardActionButton(new Rect(footer.x, footer.y, buttonWidth, footer.height), KeyCode.H, "Рук, помоги")) PauseTutorialForHelp();
        if (DrawKeyboardActionButton(new Rect(footer.x + buttonWidth + 8f, footer.y, buttonWidth, footer.height), KeyCode.R, "На дорогу")) BeginTutorialLesson();
        if (DrawKeyboardActionButton(new Rect(footer.x + (buttonWidth + 8f) * 2f, footer.y, buttonWidth, footer.height), KeyCode.Escape, "В меню")) OpenMainMenu();

        // Keep the exercise card above the footer; the centre remains a driving view.
        float taskWidth = Mathf.Min(780f, screenWidth - 40f);
        float objectiveHeight = tutorialBodyStyle.CalcHeight(new GUIContent(lesson.Objective), taskWidth - 32f);
        float taskHeight = 172f + objectiveHeight;
        Rect task = new Rect((screenWidth - taskWidth) * 0.5f, footer.y - taskHeight - 18f, taskWidth, taskHeight);
        DrawControlSurface(task);
        GUI.Label(new Rect(task.x + 16f, task.y + 9f, task.width - 32f, 25f), "ПРИЁМ " + (tutorialLesson + 1) + " / 7  ·  " + lesson.Title, arcadeSmallStyle);
        DrawLessonKeys(new Rect(task.x + 16f, task.y + 40f, task.width - 32f, 76f));
        GUI.Label(new Rect(task.x + 16f, task.y + 120f, task.width - 32f, objectiveHeight), lesson.Objective, tutorialBodyStyle);
        GUI.Label(new Rect(task.x + 16f, task.y + 126f + objectiveHeight, task.width - 32f, 25f), telemetry, controlSmallStyle);
        Rect progress = new Rect(task.x + 16f, task.yMax - 16f, task.width - 32f, 6f);
        DrawRoundedRect(progress, new Color(0.12f, 0.2f, 0.24f), 3f);
        DrawRoundedRect(new Rect(progress.x, progress.y, progress.width * tutorialProgress, progress.height), ControlsMint, 3f);
    }

    private void DrawTutorialConversation(float screenWidth, float screenHeight)
    {
        DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.002f, 0.006f, 0.02f, 0.72f));
        const float width = 960f;
        const float height = 610f;
        float scale = Mathf.Min(1f, (screenWidth - 40f) / width, (screenHeight - 40f) / height);
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = previous * Matrix4x4.TRS(new Vector3((screenWidth - width * scale) * 0.5f, (screenHeight - height * scale) * 0.5f, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
        bool welcome = tutorialPhase == TutorialPhase.Welcome;
        bool help = tutorialPhase == TutorialPhase.Help;
        bool complete = tutorialPhase == TutorialPhase.Complete;
        DrawControlSurface(new Rect(0f, 0f, width, height));
        DrawTutorialPortrait(new Rect(24f, 85f, 220f, 220f));
        GUI.Label(new Rect(30f, 316f, 210f, 42f), "РУК", arcadeTitleStyle);
        GUI.Label(new Rect(31f, 369f, 210f, 30f), "МЕХАНИК КОМАНДЫ", arcadeSmallStyle);

        string title = welcome ? "АВТОШКОЛА РУКА" : help ? "СПОКОЙНО, Я РЯДОМ" : complete ? "ЗАЧЁТ! ТЫ ЗА РУЛЁМ" : "ПОЛУЧИЛОСЬ!";
        GUI.Label(new Rect(28f, 20f, 904f, 43f), title, tutorialTitleStyle);
        GUI.Label(new Rect(270f, 83f, 658f, 29f), welcome ? "7 КОРОТКИХ ПРИЁМОВ · В ТВОЁМ ТЕМПЕ" : "ПРИЁМ " + (tutorialLesson + 1) + " / 7  ·  " + DrivingLessons[tutorialLesson].Title, arcadeSmallStyle);
        string text;
        if (welcome)
            text = tutorialFunnyReply
                ? "Точно безопасно! Соперников отправил за булочками, повреждения отключил. Даже моя страховка одобрила. А она обычно отвечает только словом «НЕТ».\n\nБудем учиться по одному приёму. Я объясню — ты попробуешь. Если застрянешь, нажми H или верни машину на дорогу кнопкой R."
                : "Привет, пилот! Я Рук. Чиню машины и иногда — уверенность в себе. Сегодня прокачаем и то, и другое.\n\nГаз, руль, тормоз, дрифт, нитро и оружие: каждый приём попробуешь сам. После успеха остановимся поболтать. Спешить некуда — соперников нет, машина не ломается.";
        else if (help)
            text = DrivingLessons[tutorialLesson].Hint + "\n\nИгра сейчас на паузе. Выдохни, найди нужные клавиши — и продолжим. Учебная машина терпеливая. Я её такой собрал.";
        else if (complete)
            text = DrivingLessons[tutorialLesson].Praise + "\n\nЛика уже ждёт в кампании. Езжай к ней — теперь её фраза «жми газ» не будет загадкой.\n\nЗачёт сохранён. Вернуться сюда можно в любой момент через «Обучение с Руком».";
        else
            text = tutorialFunnyReply
                ? "Секрет простой: пробуешь, ошибаешься, пробуешь ещё. Я так однажды собрал тостер. Получился двигатель.\n\n" + DrivingLessons[tutorialLesson].Praise
                : DrivingLessons[tutorialLesson].Praise + "\n\nСледующий приём начнём на учебной прямой. Готов — скажи, и поедем!";
        GUI.Label(new Rect(270f, 131f, 658f, 274f), text, tutorialBodyStyle);

        int completed = complete ? DrivingLessons.Length : tutorialPhase == TutorialPhase.Praise ? tutorialLesson + 1 : tutorialLesson;
        for (int i = 0; i < DrivingLessons.Length; i++)
        {
            Rect marker = new Rect(270f + i * 94f, 425f, 84f, 8f);
            DrawRoundedRect(marker, i < completed ? ControlsMint : new Color(0.13f, 0.23f, 0.28f), 4f);
        }

        string primary = welcome ? "ПОЕХАЛИ, РУК!" : help ? "ПОНЯЛ, ПРОБУЮ!" : complete ? "К ЛИКЕ В КАМПАНИЮ" : "К СЛЕДУЮЩЕМУ ПРИЁМУ";
        if (DrawKeyboardActionButton(new Rect(24f, 459f, 546f, 64f), KeyCode.Return, primary, true)) AdvanceDrivingTutorial();
        if (DrawTutorialButton(new Rect(588f, 459f, 348f, 64f), welcome ? "А ЭТО БЕЗОПАСНО?" : help ? "ВЕРНИ МЕНЯ НА ДОРОГУ" : complete ? "ПОКАЖУ ЕЩЁ РАЗ!" : "РУК, КАК Я ТЕБЕ?"))
        {
            if (welcome || tutorialPhase == TutorialPhase.Praise) tutorialFunnyReply = true;
            else if (help) BeginTutorialLesson();
            else { tutorialLesson = 0; BeginTutorialLesson(); }
        }
        if (DrawKeyboardActionButton(new Rect(24f, 539f, 912f, 46f), KeyCode.Escape, "Вернуться в меню")) OpenMainMenu();
        GUI.matrix = previous;
    }
}
