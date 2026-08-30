using UnityEngine;

public sealed partial class NeonCircuitGame
{
    private const string OwnedWeaponPrefix = "NeonCircuit.OwnedWeapon.";
    private const string SelectedWeaponKey = "NeonCircuit.SelectedWeapon";

    private static readonly int[] StoryRewardCarByChapter =
    {
        -1, 9, -1, 10, -1, 11, -1, 12
    };

    private static readonly int[] StoryRewardWeaponByChapter =
    {
        (int)CarWeaponType.EchoArc,
        -1,
        (int)CarWeaponType.OrbitMine,
        -1,
        (int)CarWeaponType.IcarLance,
        -1,
        (int)CarWeaponType.PhantomSwarm,
        -1
    };

    private CarWeaponType selectedWeaponType = CarWeaponType.NeonRocket;
    private Texture2D[] storyWeaponIcons;

    public CarWeaponType SelectedWeaponType { get { return selectedWeaponType; } }

    private void LoadStoryRewardProgress()
    {
        bool changed = false;
        for (int chapterIndex = 0; chapterIndex < StoryRewardCarByChapter.Length; chapterIndex++)
        {
            if (IsStoryChapterCompleted(chapterIndex))
            {
                changed |= GrantStoryChapterUnlock(chapterIndex);
            }
        }

        int savedWeapon = PlayerPrefs.GetInt(SelectedWeaponKey, (int)CarWeaponType.NeonRocket);
        CarWeaponType loadedWeapon = savedWeapon >= (int)CarWeaponType.NeonRocket && savedWeapon <= (int)CarWeaponType.PhantomSwarm
            ? (CarWeaponType)savedWeapon
            : CarWeaponType.NeonRocket;
        selectedWeaponType = IsWeaponUnlocked(loadedWeapon) ? loadedWeapon : CarWeaponType.NeonRocket;
        if (savedWeapon != (int)selectedWeaponType)
        {
            PlayerPrefs.SetInt(SelectedWeaponKey, (int)selectedWeaponType);
            changed = true;
        }

        if (changed)
        {
            PlayerPrefs.Save();
        }
    }

    private bool GrantStoryChapterUnlock(int chapterIndex)
    {
        int safeChapter = Mathf.Clamp(chapterIndex, 0, StoryRewardCarByChapter.Length - 1);
        bool changed = false;
        int rewardCar = StoryRewardCarByChapter[safeChapter];
        if (rewardCar >= 0 && PlayerPrefs.GetInt(OwnedCarPrefix + rewardCar, 0) != 1)
        {
            PlayerPrefs.SetInt(OwnedCarPrefix + rewardCar, 1);
            changed = true;
        }

        int rewardWeapon = StoryRewardWeaponByChapter[safeChapter];
        if (rewardWeapon >= 0 && PlayerPrefs.GetInt(OwnedWeaponPrefix + rewardWeapon, 0) != 1)
        {
            PlayerPrefs.SetInt(OwnedWeaponPrefix + rewardWeapon, 1);
            changed = true;
        }

        return changed;
    }

    public bool IsWeaponUnlocked(CarWeaponType weapon)
    {
        if (weapon == CarWeaponType.NeonRocket || weapon == CarWeaponType.PlasmaBlaster)
        {
            return true;
        }

        return PlayerPrefs.GetInt(OwnedWeaponPrefix + (int)weapon, 0) == 1;
    }

    public CarWeaponType GetNextUnlockedWeapon(CarWeaponType current)
    {
        int weaponCount = (int)CarWeaponType.PhantomSwarm + 1;
        int currentIndex = Mathf.Clamp((int)current, 0, weaponCount - 1);
        for (int offset = 1; offset <= weaponCount; offset++)
        {
            CarWeaponType candidate = (CarWeaponType)((currentIndex + offset) % weaponCount);
            if (IsWeaponUnlocked(candidate))
            {
                return candidate;
            }
        }

        return CarWeaponType.NeonRocket;
    }

    private void SelectStoryWeapon(CarWeaponType weapon)
    {
        if (!IsWeaponUnlocked(weapon))
        {
            int chapter = GetWeaponUnlockChapter(weapon);
            garageMessage = chapter >= 0
                ? "ОРУЖИЕ ОТКРОЕТСЯ В ГЛАВЕ " + (chapter + 1).ToString("00")
                : "ОРУЖИЕ ЕЩЁ НЕ ОТКРЫТО";
            garageMessageUntil = Time.unscaledTime + 2.5f;
            return;
        }

        selectedWeaponType = weapon;
        PlayerPrefs.SetInt(SelectedWeaponKey, (int)selectedWeaponType);
        PlayerPrefs.Save();
        if (playerWeapon != null)
        {
            playerWeapon.EquipWeapon(selectedWeaponType);
        }

        garageMessage = GetWeaponDisplayName(weapon) + " ВЫБРАНО";
        garageMessageUntil = Time.unscaledTime + 1.8f;
    }

    private int GetStoryCarUnlockChapter(int carIndex)
    {
        for (int i = 0; i < StoryRewardCarByChapter.Length; i++)
        {
            if (StoryRewardCarByChapter[i] == carIndex)
            {
                return i;
            }
        }

        return -1;
    }

    private int GetWeaponUnlockChapter(CarWeaponType weapon)
    {
        for (int i = 0; i < StoryRewardWeaponByChapter.Length; i++)
        {
            if (StoryRewardWeaponByChapter[i] == (int)weapon)
            {
                return i;
            }
        }

        return -1;
    }

    public string GetWeaponDisplayName(CarWeaponType weapon)
    {
        switch (weapon)
        {
            case CarWeaponType.PlasmaBlaster: return "ПЛАЗМА";
            case CarWeaponType.EchoArc: return "ЭХО-ДУГА";
            case CarWeaponType.OrbitMine: return "ОРБИТАЛЬНАЯ МИНА";
            case CarWeaponType.IcarLance: return "КОПЬЁ ИКАРА";
            case CarWeaponType.PhantomSwarm: return "РОЙ ФАНТОМОВ";
            default: return "НЕОН-РАКЕТА";
        }
    }

    public string GetStoryRewardTitle(int chapterIndex)
    {
        int safeChapter = Mathf.Clamp(chapterIndex, 0, StoryRewardCarByChapter.Length - 1);
        int rewardCar = StoryRewardCarByChapter[safeChapter];
        if (rewardCar >= 0)
        {
            return "МАШИНА  " + CarNames[rewardCar];
        }

        return "ОРУЖИЕ  " + GetWeaponDisplayName((CarWeaponType)StoryRewardWeaponByChapter[safeChapter]);
    }

    public string GetStoryRewardDetail(int chapterIndex)
    {
        switch (Mathf.Clamp(chapterIndex, 0, StoryRewardCarByChapter.Length - 1))
        {
            case 0: return "ЦЕПНАЯ МОЛНИЯ  /  ДО 3 ЦЕЛЕЙ";
            case 1: return "ЛЁГКИЙ РАЗВЕДЧИК  /  ВЫСОКАЯ МАНЁВРЕННОСТЬ";
            case 2: return "ЛОВУШКА ПОЗАДИ  /  ВЗРЫВ ПО РАДИУСУ";
            case 3: return "ТЯЖЁЛЫЙ ТАРАН  /  УСИЛЕННАЯ БРОНЯ";
            case 4: return "ПРОБИВАЮЩИЙ ЛУЧ  /  НЕСКОЛЬКО ЦЕЛЕЙ";
            case 5: return "ШТУРМОВОЙ WIDEBODY  /  СКОРОСТЬ";
            case 6: return "4 АВТОНОМНЫХ ДРОНА  /  ВРЕМЕННАЯ АТАКА";
            default: return "ФИНАЛЬНЫЙ ПРОТОТИП  /  ЛУЧШИЕ ХАРАКТЕРИСТИКИ";
        }
    }

    public Color GetStoryRewardColor(int chapterIndex)
    {
        Color[] colors = { ArcadeCyan, ArcadeLime, ArcadeOrange, ArcadeYellow, ArcadeYellow, ArcadePink, ArcadePink, ArcadeCyan };
        return colors[Mathf.Clamp(chapterIndex, 0, colors.Length - 1)];
    }

    public bool IsStoryChapterRewardOwned(int chapterIndex)
    {
        int safeChapter = Mathf.Clamp(chapterIndex, 0, StoryRewardCarByChapter.Length - 1);
        int rewardCar = StoryRewardCarByChapter[safeChapter];
        if (rewardCar >= 0)
        {
            return IsCarOwned(rewardCar);
        }

        return IsWeaponUnlocked((CarWeaponType)StoryRewardWeaponByChapter[safeChapter]);
    }

    private void EnsureStoryWeaponIcons()
    {
        if (storyWeaponIcons != null && storyWeaponIcons.Length == 6)
        {
            return;
        }

        storyWeaponIcons = new[]
        {
            (Texture2D)null,
            (Texture2D)null,
            Resources.Load<Texture2D>("UI/Weapons/EchoArc"),
            Resources.Load<Texture2D>("UI/Weapons/OrbitMine"),
            Resources.Load<Texture2D>("UI/Weapons/IcarLance"),
            Resources.Load<Texture2D>("UI/Weapons/PhantomSwarm")
        };
    }

    private void DrawStoryWeaponArsenal(Rect rect)
    {
        EnsureStoryWeaponIcons();
        DrawSolidRect(rect, new Color(0.002f, 0.011f, 0.03f, 0.98f));
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 3f), ArcadePink);
        GUI.Label(new Rect(rect.x + 12f, rect.y + 7f, rect.width - 24f, 20f), "STORY ARSENAL  //  ВЫБЕРИТЕ СТАРТОВОЕ ОРУЖИЕ", arcadeMicroStyle);

        CarWeaponType[] weapons =
        {
            CarWeaponType.NeonRocket,
            CarWeaponType.PlasmaBlaster,
            CarWeaponType.EchoArc,
            CarWeaponType.OrbitMine,
            CarWeaponType.IcarLance,
            CarWeaponType.PhantomSwarm
        };
        Color[] accents = { ArcadeOrange, ArcadeCyan, ArcadeCyan, ArcadeOrange, ArcadeYellow, ArcadePink };
        float gap = 7f;
        float itemWidth = (rect.width - 24f - gap * 2f) / 3f;
        float itemHeight = (rect.height - 39f - gap) * 0.5f;

        for (int i = 0; i < weapons.Length; i++)
        {
            int column = i % 3;
            int row = i / 3;
            Rect item = new Rect(rect.x + 12f + column * (itemWidth + gap), rect.y + 30f + row * (itemHeight + gap), itemWidth, itemHeight);
            bool unlocked = IsWeaponUnlocked(weapons[i]);
            bool selected = selectedWeaponType == weapons[i];
            Color accent = accents[i];
            DrawSolidRect(item, selected
                ? new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f, 0.99f)
                : new Color(0.008f, 0.025f, 0.055f, 0.96f));
            DrawSolidRect(new Rect(item.x, item.y, 3f, item.height), unlocked ? accent : new Color(0.25f, 0.32f, 0.38f));
            if (selected)
            {
                DrawSolidRect(new Rect(item.x, item.yMax - 3f, item.width, 3f), accent);
            }

            Rect iconRect = new Rect(item.x + 8f, item.y + 7f, 34f, 34f);
            if (storyWeaponIcons[i] != null)
            {
                Color previous = GUI.color;
                GUI.color = unlocked ? Color.white : new Color(0.32f, 0.39f, 0.44f, 0.72f);
                GUI.DrawTexture(iconRect, storyWeaponIcons[i], ScaleMode.ScaleToFit, true);
                GUI.color = previous;
            }
            else
            {
                DrawSolidRect(iconRect, new Color(accent.r, accent.g, accent.b, unlocked ? 0.38f : 0.12f));
                GUI.Label(iconRect, i == 0 ? "R" : "P", arcadeCenteredStyle);
            }

            string compactName = GetWeaponDisplayName(weapons[i]);
            GUI.Label(new Rect(item.x + 48f, item.y + 6f, item.width - 54f, 20f), compactName, arcadeMicroStyle);
            int unlockChapter = GetWeaponUnlockChapter(weapons[i]);
            string status = selected ? "ВЫБРАНО" : unlocked ? "ДОСТУПНО" : "ГЛАВА " + (unlockChapter + 1).ToString("00");
            GUI.Label(new Rect(item.x + 48f, item.y + 28f, item.width - 54f, 16f), status, arcadeMicroStyle);

            if (GUI.Button(item, GUIContent.none, GUIStyle.none))
            {
                SelectStoryWeapon(weapons[i]);
            }
        }
    }
}
