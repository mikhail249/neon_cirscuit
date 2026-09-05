using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed partial class NeonCircuitGame
{
    private const float ControlCardHeight = 88f;
    private const float ControlCardGap = 8f;
    private static readonly float[] ControlCardWidths = { 158f, 108f, 94f, 130f, 114f, 110f, 118f, 104f };
    private static readonly string[] ControlLabels = { "Руль\nи газ", "Дрифт", "Нитро", "Огонь", "Оружие", "Гараж", "Заново", "Меню" };
    private static readonly KeyCode[] ControlKeys = { KeyCode.W, KeyCode.LeftShift, KeyCode.X, KeyCode.Space, KeyCode.Q, KeyCode.G, KeyCode.R, KeyCode.Escape };
    private static readonly Color ControlsMint = new Color(0.42f, 0.95f, 0.8f);
    private static readonly Color ControlsText = new Color(0.92f, 0.96f, 1f);
    private static readonly Color ControlsMuted = new Color(0.6f, 0.69f, 0.79f);
    private GUIStyle controlCaptionStyle, controlSmallStyle, controlKeyStyle, controlValueStyle;

    private void EnsureControlStyles()
    {
        if (controlCaptionStyle != null) return;
        controlCaptionStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter, wordWrap = true, padding = new RectOffset() };
        controlCaptionStyle.normal.textColor = ControlsText;
        controlSmallStyle = new GUIStyle(controlCaptionStyle) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
        controlSmallStyle.normal.textColor = ControlsMuted;
        controlKeyStyle = new GUIStyle(controlCaptionStyle) { fontSize = 17, fontStyle = FontStyle.Bold, wordWrap = false };
        controlValueStyle = new GUIStyle(controlCaptionStyle) { fontSize = 21, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
    }

    private void DrawRoundedRect(Rect rect, Color color, float radius)
    {
        if (rect.width <= 0f || rect.height <= 0f) return;
        GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, Mathf.Min(radius, rect.height * 0.5f, rect.width * 0.5f));
    }

    private void DrawControlSurface(Rect rect, bool highlighted = false)
    {
        DrawRoundedRect(new Rect(rect.x, rect.y + 5f, rect.width, rect.height), new Color(0f, 0.01f, 0.025f, 0.18f), 16f);
        DrawRoundedRect(rect, highlighted ? new Color(0.27f, 0.63f, 0.59f, 0.9f) : new Color(0.27f, 0.34f, 0.43f, 0.75f), 14f);
        DrawRoundedRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), new Color(0.027f, 0.045f, 0.074f, 0.96f), 13f);
    }

    private static string ControlKeyLabel(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.LeftShift: return "Shift";
            case KeyCode.Space: return "Пробел";
            case KeyCode.Escape: return "Esc";
            case KeyCode.Return: return "Enter";
            default: return key.ToString();
        }
    }

    // Physical key codes also work when the operating system's layout is Russian.
    private static bool ControlKeyHeld(KeyCode key)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        switch (key)
        {
            case KeyCode.W: return keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed;
            case KeyCode.A: return keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed;
            case KeyCode.S: return keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed;
            case KeyCode.D: return keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed;
            case KeyCode.LeftShift: return keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            case KeyCode.X: return keyboard.xKey.isPressed;
            case KeyCode.Space: return keyboard.spaceKey.isPressed;
            case KeyCode.Q: return keyboard.qKey.isPressed;
            case KeyCode.G: return keyboard.gKey.isPressed;
            case KeyCode.R: return keyboard.rKey.isPressed;
            case KeyCode.H: return keyboard.hKey.isPressed;
            case KeyCode.Return: return keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed;
            case KeyCode.Escape: return keyboard.escapeKey.isPressed;
            default: return false;
        }
#else
        if (Input.GetKey(key)) return true;
        switch (key)
        {
            case KeyCode.W: return Input.GetKey(KeyCode.UpArrow);
            case KeyCode.A: return Input.GetKey(KeyCode.LeftArrow);
            case KeyCode.S: return Input.GetKey(KeyCode.DownArrow);
            case KeyCode.D: return Input.GetKey(KeyCode.RightArrow);
            case KeyCode.LeftShift: return Input.GetKey(KeyCode.RightShift);
            case KeyCode.Return: return Input.GetKey(KeyCode.KeypadEnter);
            default: return false;
        }
#endif
    }

    private void DrawControlKey(Rect rect, KeyCode key, bool suggested = false, bool pointerPressed = false)
    {
        EnsureControlStyles();
        bool pressed = ControlKeyHeld(key) || pointerPressed;
        if (suggested || pressed)
        {
            float alpha = pressed ? 0.28f : 0.08f + 0.04f * Mathf.Sin(Time.unscaledTime * 2.5f);
            DrawRoundedRect(new Rect(rect.x - 4f, rect.y - 3f, rect.width + 8f, rect.height + 13f), new Color(ControlsMint.r, ControlsMint.g, ControlsMint.b, alpha), 11f);
        }
        DrawRoundedRect(new Rect(rect.x, rect.y + 6f, rect.width, rect.height), new Color(0f, 0.01f, 0.025f, 0.6f), 8f);
        DrawRoundedRect(new Rect(rect.x, rect.y + 4f, rect.width, rect.height), pressed ? new Color(0.19f, 0.52f, 0.45f) : new Color(0.34f, 0.42f, 0.51f), 7f);
        rect.y += pressed ? 3f : 0f;
        DrawRoundedRect(rect, pressed ? ControlsMint : new Color(0.96f, 0.98f, 1f), 7f);
        DrawRoundedRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width - 4f, rect.height - 5f), pressed ? new Color(0.52f, 1f, 0.86f) : new Color(0.84f, 0.9f, 0.96f), 5f);
        controlKeyStyle.fontSize = rect.height < 31f ? 14 : 17;
        controlKeyStyle.normal.textColor = new Color(0.065f, 0.14f, 0.2f);
        GUI.Label(rect, ControlKeyLabel(key), controlKeyStyle);
        if (key == KeyCode.Space)
            DrawRoundedRect(new Rect(rect.center.x - 17f, rect.yMax - 6f, 34f, 2f), new Color(0.38f, 0.49f, 0.58f), 1f);
    }

    private static float ControlKeyWidth(KeyCode key)
    {
        if (key == KeyCode.Space) return 92f;
        if (key == KeyCode.LeftShift || key == KeyCode.Return) return 68f;
        if (key == KeyCode.Escape) return 52f;
        return 38f;
    }

    private static float RaceControlsNaturalWidth()
    {
        float width = ControlCardGap * (ControlCardWidths.Length - 1);
        for (int i = 0; i < ControlCardWidths.Length; i++) width += ControlCardWidths[i];
        return width;
    }

    private static int ControlRowEnd(int start, float width, out float rowWidth)
    {
        rowWidth = Mathf.Min(width, ControlCardWidths[start]);
        int end = start + 1;
        while (end < ControlCardWidths.Length && rowWidth + ControlCardGap + ControlCardWidths[end] <= width)
        {
            rowWidth += ControlCardGap + ControlCardWidths[end];
            end++;
        }
        return end;
    }

    private static float RaceControlsHeight(float width)
    {
        int rows = 0;
        for (int start = 0; start < ControlCardWidths.Length; rows++)
            start = ControlRowEnd(start, width, out _);
        return rows * ControlCardHeight + (rows - 1) * ControlCardGap + 5f;
    }

    private void DrawRaceControls(Rect controls)
    {
        EnsureControlStyles();
        float y = controls.y;
        for (int start = 0; start < ControlCardWidths.Length;)
        {
            int end = ControlRowEnd(start, controls.width, out float rowWidth);
            float x = controls.center.x - rowWidth * 0.5f;
            for (int i = start; i < end; i++)
            {
                Rect card = new Rect(x, y, Mathf.Min(controls.width, ControlCardWidths[i]), ControlCardHeight);
                bool clickable = i >= 3 && raceStarted && !raceFinished && !playerWrecked && Time.timeScale > 0f;
                bool hover = clickable && card.Contains(Event.current.mousePosition);
                bool pointerPressed = hover && Event.current.type != EventType.Layout && GUIUtility.hotControl != 0;
                bool held = ControlKeyHeld(ControlKeys[i]);
                if (i == 0) held |= ControlKeyHeld(KeyCode.A) || ControlKeyHeld(KeyCode.S) || ControlKeyHeld(KeyCode.D);
                DrawControlSurface(card, hover || held);
                if (i == 0)
                {
                    DrawControlKey(new Rect(x + 43f, y + 9f, 27f, 27f), KeyCode.W);
                    DrawControlKey(new Rect(x + 12f, y + 42f, 27f, 27f), KeyCode.A);
                    DrawControlKey(new Rect(x + 43f, y + 42f, 27f, 27f), KeyCode.S);
                    DrawControlKey(new Rect(x + 74f, y + 42f, 27f, 27f), KeyCode.D);
                    GUI.Label(new Rect(x + 106f, y + 17f, 44f, 55f), ControlLabels[i], controlCaptionStyle);
                }
                else
                {
                    float keyWidth = ControlKeyWidth(ControlKeys[i]);
                    DrawControlKey(new Rect(card.center.x - keyWidth * 0.5f, y + 12f, keyWidth, 34f), ControlKeys[i], false, pointerPressed);
                    GUI.Label(new Rect(x + 5f, y + 58f, card.width - 10f, 22f), ControlLabels[i], controlCaptionStyle);
                }
                if (clickable && GUI.Button(card, GUIContent.none, GUIStyle.none))
                {
                    PlayMenuClickSfx();
                    switch (i)
                    {
                        case 3: if (playerWeapon != null) playerWeapon.TryFire(); break;
                        case 4: if (playerWeapon != null) playerWeapon.EquipWeapon(GetNextUnlockedWeapon(playerWeapon.ActiveWeapon)); break;
                        case 5: ToggleGarage(); break;
                        case 6: RestartRace(); break;
                        case 7: OpenMainMenu(); break;
                    }
                }
                x += card.width + ControlCardGap;
            }
            start = end;
            y += ControlCardHeight + ControlCardGap;
        }
    }

    private bool DrawKeyboardActionButton(Rect rect, KeyCode key, string caption, bool primary = false)
    {
        EnsureControlStyles();
        bool hovered = rect.Contains(Event.current.mousePosition);
        DrawControlSurface(rect, primary || hovered);
        float keyWidth = ControlKeyWidth(key);
        DrawControlKey(new Rect(rect.x + 13f, rect.center.y - 18f, keyWidth, 32f), key, primary, hovered && GUIUtility.hotControl != 0);
        controlCaptionStyle.fontSize = primary ? 18 : 15;
        GUI.Label(new Rect(rect.x + keyWidth + 24f, rect.y + 4f, rect.width - keyWidth - 34f, rect.height - 8f), caption, controlCaptionStyle);
        controlCaptionStyle.fontSize = 15;
        if (!GUI.Button(rect, GUIContent.none, GUIStyle.none)) return false;
        PlayMenuClickSfx();
        return true;
    }

    private void DrawLessonKeys(Rect rect)
    {
        // A/D is a choice; the braking step changes its prompt once enough speed is reached.
        string prompt = tutorialLesson == 2 && tutorialBrakeReady ? "ОТПУСТИ ГАЗ И УДЕРЖИВАЙ" : "УДЕРЖИВАЙ";
        if (tutorialLesson >= 5) prompt = "НАЖМИ ОДИН РАЗ";
        EnsureControlStyles();
        controlSmallStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 20f), prompt, controlSmallStyle);
        controlSmallStyle.alignment = TextAnchor.MiddleLeft;
        KeyCode first = tutorialLesson == 2 && tutorialBrakeReady ? KeyCode.S : tutorialLesson == 5 ? KeyCode.Q : tutorialLesson == 6 ? KeyCode.Space : KeyCode.W;
        float total = ControlKeyWidth(first);
        if (tutorialLesson == 1 || tutorialLesson == 3) total += 28f + 38f + 42f + 38f;
        if (tutorialLesson == 3) total += 28f + ControlKeyWidth(KeyCode.LeftShift);
        if (tutorialLesson == 4) total += 28f + ControlKeyWidth(KeyCode.X);
        float x = rect.center.x - total * 0.5f;
        DrawLessonKeyPart(ref x, rect.y + 26f, first);
        if (tutorialLesson == 3)
        {
            DrawLessonKeyJoin(ref x, rect.y + 26f, "+", 28f);
            DrawLessonKeyPart(ref x, rect.y + 26f, KeyCode.LeftShift);
        }
        if (tutorialLesson == 1 || tutorialLesson == 3)
        {
            DrawLessonKeyJoin(ref x, rect.y + 26f, "+", 28f);
            DrawLessonKeyPart(ref x, rect.y + 26f, KeyCode.A);
            DrawLessonKeyJoin(ref x, rect.y + 26f, "или", 42f);
            DrawLessonKeyPart(ref x, rect.y + 26f, KeyCode.D);
        }
        if (tutorialLesson == 4)
        {
            DrawLessonKeyJoin(ref x, rect.y + 26f, "+", 28f);
            DrawLessonKeyPart(ref x, rect.y + 26f, KeyCode.X);
        }
    }

    private void DrawLessonKeyPart(ref float x, float y, KeyCode key)
    {
        float width = ControlKeyWidth(key);
        DrawControlKey(new Rect(x, y, width, 38f), key, true);
        x += width;
    }

    private void DrawLessonKeyJoin(ref float x, float y, string text, float width)
    {
        GUI.Label(new Rect(x, y, width, 38f), text, controlCaptionStyle);
        x += width;
    }
}
