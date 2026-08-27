using UnityEngine;

public enum RaceTrackTheme
{
    Desert,
    NeonCity,
    Glacier,
    Volcano,
    Moon
}

public sealed class RaceTrackDefinition
{
    public readonly string DisplayName;
    public readonly string ShortName;
    public readonly string Description;
    public readonly Vector2[] Nodes;
    public readonly Color CameraColor;
    public readonly Color GroundColor;
    public readonly Color GroundStripeA;
    public readonly Color GroundStripeB;
    public readonly Color CurbA;
    public readonly Color CurbB;
    public readonly Color AsphaltA;
    public readonly Color AsphaltB;
    public readonly Color AccentColor;
    public readonly RaceTrackTheme Theme;
    public bool IsDesert { get { return Theme == RaceTrackTheme.Desert || Theme == RaceTrackTheme.Volcano; } }
    public bool IsGlacier { get { return Theme == RaceTrackTheme.Glacier; } }
    public bool IsVolcanic { get { return Theme == RaceTrackTheme.Volcano; } }
    public bool IsLunar { get { return Theme == RaceTrackTheme.Moon; } }

    public RaceTrackDefinition(
        string displayName,
        string shortName,
        string description,
        Vector2[] nodes,
        Color cameraColor,
        Color groundColor,
        Color groundStripeA,
        Color groundStripeB,
        Color curbA,
        Color curbB,
        Color asphaltA,
        Color asphaltB,
        Color accentColor,
        RaceTrackTheme theme)
    {
        DisplayName = displayName;
        ShortName = shortName;
        Description = description;
        Nodes = nodes;
        CameraColor = cameraColor;
        GroundColor = groundColor;
        GroundStripeA = groundStripeA;
        GroundStripeB = groundStripeB;
        CurbA = curbA;
        CurbB = curbB;
        AsphaltA = asphaltA;
        AsphaltB = asphaltB;
        AccentColor = accentColor;
        Theme = theme;
    }
}

public static class RaceTrackCatalog
{
    private static readonly RaceTrackDefinition[] Tracks =
    {
        new RaceTrackDefinition(
            "ПУСТЫННЫЙ КАНЬОН",
            "КАНЬОН",
            "СКАЛЫ  /  СКОРОСТНЫЕ ДУГИ",
            new[]
            {
                new Vector2(40f, -24f),
                new Vector2(20f, -29f),
                new Vector2(-5f, -29f),
                new Vector2(-29f, -27f),
                new Vector2(-43f, -15f),
                new Vector2(-36f, 0f),
                new Vector2(-45f, 14f),
                new Vector2(-34f, 29f),
                new Vector2(-14f, 31f),
                new Vector2(1f, 21f),
                new Vector2(14f, 30f),
                new Vector2(34f, 27f),
                new Vector2(44f, 14f),
                new Vector2(40f, 1f),
                new Vector2(29f, -5f),
                new Vector2(20f, 4f),
                new Vector2(9f, 1f),
                new Vector2(8f, -11f),
                new Vector2(23f, -16f),
                new Vector2(39f, -12f)
            },
            new Color(0.16f, 0.075f, 0.03f),
            new Color(0.40f, 0.19f, 0.07f),
            new Color(0.66f, 0.31f, 0.09f, 0.34f),
            new Color(0.26f, 0.11f, 0.04f, 0.30f),
            new Color(0.96f, 0.38f, 0.08f),
            new Color(0.98f, 0.88f, 0.62f),
            new Color(0.14f, 0.12f, 0.11f),
            new Color(0.11f, 0.09f, 0.085f),
            new Color(1f, 0.43f, 0.08f),
            RaceTrackTheme.Desert),
        new RaceTrackDefinition(
            "НЕОНОВЫЙ ГОРОД",
            "НЕОН-ГРИД",
            "МЕГАПОЛИС  /  ДВОЙНЫЕ ШИКАНЫ И ШПИЛЬКИ",
            new[]
            {
                new Vector2(-6f, -30f),
                new Vector2(-20f, -30f),
                new Vector2(-31f, -33f),
                new Vector2(-40f, -25f),
                new Vector2(-44f, -16f),
                new Vector2(-43f, -7f),
                new Vector2(-47f, 2f),
                new Vector2(-44f, 10f),
                new Vector2(-45f, 18f),
                new Vector2(-34f, 30f),
                new Vector2(-16f, 32f),
                new Vector2(-9f, 24f),
                new Vector2(-20f, 13f),
                new Vector2(-22f, 2f),
                new Vector2(-10f, -4f),
                new Vector2(-3f, 0f),
                new Vector2(2f, 16f),
                new Vector2(17f, 28f),
                new Vector2(34f, 28f),
                new Vector2(45f, 18f),
                new Vector2(46f, 4f),
                new Vector2(40f, -5f),
                new Vector2(30f, 0f),
                new Vector2(28f, 12f),
                new Vector2(18f, 13f),
                new Vector2(13f, -2f),
                new Vector2(20f, -15f),
                new Vector2(28f, -14f),
                new Vector2(34f, -13f),
                new Vector2(39f, -14f),
                new Vector2(43f, -18f),
                new Vector2(44f, -23f),
                new Vector2(43f, -28f),
                new Vector2(39f, -32f),
                new Vector2(34f, -33f),
                new Vector2(28f, -33f),
                new Vector2(20f, -33f),
                new Vector2(10f, -32f)
            },
            new Color(0.003f, 0.006f, 0.025f),
            new Color(0.008f, 0.012f, 0.035f),
            new Color(0.08f, 0.16f, 0.24f, 0.23f),
            new Color(0.24f, 0.02f, 0.28f, 0.18f),
            new Color(0.02f, 0.95f, 1f),
            new Color(1f, 0.04f, 0.65f),
            new Color(0.055f, 0.065f, 0.11f),
            new Color(0.035f, 0.04f, 0.075f),
            new Color(0.12f, 1f, 1f),
            RaceTrackTheme.NeonCity),
        new RaceTrackDefinition(
            "ПОЛЯРНЫЙ РАЗЛОМ",
            "АЙС-ФЬОРД",
            "ЛЕДЯНЫЕ ДУГИ  /  ДЛИННЫЕ СКОЛЬЖЕНИЯ",
            new[]
            {
                new Vector2(52f, -24f),
                new Vector2(42f, -34f),
                new Vector2(20f, -36f),
                new Vector2(-18f, -29f),
                new Vector2(-37f, -21f),
                new Vector2(-46f, -8f),
                new Vector2(-44f, 8f),
                new Vector2(-35f, 22f),
                new Vector2(-18f, 30f),
                new Vector2(2f, 32f),
                new Vector2(21f, 27f),
                new Vector2(38f, 18f),
                new Vector2(46f, 5f),
                new Vector2(44f, -3f),
                new Vector2(33f, -1f),
                new Vector2(22f, -4f),
                new Vector2(12f, 6f),
                new Vector2(0f, 12f),
                new Vector2(-12f, 8f),
                new Vector2(-20f, -2f),
                new Vector2(-12f, -12f),
                new Vector2(5f, -15f),
                new Vector2(23f, -20f),
                new Vector2(43f, -17f)
            },
            new Color(0.015f, 0.045f, 0.11f),
            new Color(0.055f, 0.13f, 0.22f),
            new Color(0.22f, 0.65f, 0.92f, 0.27f),
            new Color(0.7f, 0.93f, 1f, 0.16f),
            new Color(0.72f, 0.96f, 1f),
            new Color(0.12f, 0.62f, 1f),
            new Color(0.12f, 0.17f, 0.25f),
            new Color(0.075f, 0.11f, 0.19f),
            new Color(0.42f, 0.92f, 1f),
            RaceTrackTheme.Glacier),
        new RaceTrackDefinition(
            "МАГМОВОЕ КОЛЬЦО",
            "МАГМА-9",
            "ЛАВА  /  ТЕСНЫЕ ШПИЛЬКИ И РАЗЛОМЫ",
            new[]
            {
                new Vector2(34f, -40f),
                new Vector2(20f, -36f),
                new Vector2(0f, -32f),
                new Vector2(-20f, -30f),
                new Vector2(-39f, -21f),
                new Vector2(-47f, -5f),
                new Vector2(-42f, 14f),
                new Vector2(-30f, 27f),
                new Vector2(-10f, 33f),
                new Vector2(12f, 31f),
                new Vector2(31f, 24f),
                new Vector2(44f, 11f),
                new Vector2(46f, -5f),
                new Vector2(36f, -16f),
                new Vector2(23f, -10f),
                new Vector2(17f, 2f),
                new Vector2(23f, 13f),
                new Vector2(12f, 21f),
                new Vector2(-2f, 19f),
                new Vector2(-11f, 9f),
                new Vector2(-24f, 6f),
                new Vector2(-29f, -7f),
                new Vector2(-19f, -17f),
                new Vector2(0f, -23f),
                new Vector2(18f, -27f),
                new Vector2(34f, -27f),
                new Vector2(44f, -28f),
                new Vector2(50f, -32f),
                new Vector2(49f, -37f),
                new Vector2(43f, -40f)
            },
            new Color(0.075f, 0.008f, 0.012f),
            new Color(0.16f, 0.025f, 0.025f),
            new Color(0.72f, 0.08f, 0.025f, 0.28f),
            new Color(1f, 0.34f, 0.02f, 0.18f),
            new Color(1f, 0.26f, 0.025f),
            new Color(1f, 0.78f, 0.08f),
            new Color(0.105f, 0.07f, 0.085f),
            new Color(0.055f, 0.03f, 0.045f),
            new Color(1f, 0.2f, 0.025f),
            RaceTrackTheme.Volcano),
        new RaceTrackDefinition(
            "ЛУННЫЙ КОСМОДРОМ",
            "ЛУНА-12",
            "КРАТЕРЫ  /  ОРБИТАЛЬНЫЕ ДУГИ И S-СЕКЦИЯ",
            new[]
            {
                new Vector2(34f, -38f),
                new Vector2(20f, -38f),
                new Vector2(4f, -35f),
                new Vector2(-14f, -32f),
                new Vector2(-31f, -27f),
                new Vector2(-44f, -17f),
                new Vector2(-48f, -3f),
                new Vector2(-44f, 11f),
                new Vector2(-36f, 24f),
                new Vector2(-22f, 32f),
                new Vector2(-5f, 35f),
                new Vector2(11f, 31f),
                new Vector2(25f, 36f),
                new Vector2(40f, 29f),
                new Vector2(48f, 17f),
                new Vector2(49f, 4f),
                new Vector2(43f, -6f),
                new Vector2(31f, -9f),
                new Vector2(23f, -1f),
                new Vector2(26f, 10f),
                new Vector2(17f, 18f),
                new Vector2(5f, 16f),
                new Vector2(-2f, 7f),
                new Vector2(-13f, 10f),
                new Vector2(-22f, 4f),
                new Vector2(-20f, -6f),
                new Vector2(-9f, -12f),
                new Vector2(5f, -10f),
                new Vector2(13f, -20f),
                new Vector2(25f, -26f),
                new Vector2(39f, -27f),
                new Vector2(47f, -31f),
                new Vector2(45f, -36f)
            },
            new Color(0.008f, 0.006f, 0.025f),
            new Color(0.075f, 0.07f, 0.12f),
            new Color(0.32f, 0.28f, 0.52f, 0.22f),
            new Color(0.62f, 0.72f, 1f, 0.14f),
            new Color(0.92f, 0.95f, 1f),
            new Color(0.58f, 0.28f, 1f),
            new Color(0.085f, 0.09f, 0.14f),
            new Color(0.05f, 0.055f, 0.1f),
            new Color(0.72f, 0.48f, 1f),
            RaceTrackTheme.Moon),
        new RaceTrackDefinition(
            "ГРОЗОВОЙ ПОРТ",
            "ШТОРМ-ПОРТ",
            "ДОКИ  /  ДЛИННЫЕ ПРЯМЫЕ И РЕЗКИЕ ШИКАНЫ",
            new[]
            {
                new Vector2(50f, -23f),
                new Vector2(47f, -31f),
                new Vector2(39f, -34f),
                new Vector2(28f, -34f),
                new Vector2(10f, -28f),
                new Vector2(-12f, -27f),
                new Vector2(-35f, -25f),
                new Vector2(-50f, -18f),
                new Vector2(-54f, -5f),
                new Vector2(-50f, 8f),
                new Vector2(-38f, 14f),
                new Vector2(-22f, 13f),
                new Vector2(-10f, 20f),
                new Vector2(5f, 28f),
                new Vector2(24f, 27f),
                new Vector2(42f, 20f),
                new Vector2(52f, 10f),
                new Vector2(50f, 0f),
                new Vector2(38f, -4f),
                new Vector2(25f, 2f),
                new Vector2(16f, 11f),
                new Vector2(5f, 10f),
                new Vector2(-2f, 6f),
                new Vector2(-8f, 5f),
                new Vector2(-14f, 4f),
                new Vector2(-20f, 1f),
                new Vector2(-23f, -5f),
                new Vector2(-20f, -11f),
                new Vector2(-14f, -14f),
                new Vector2(-7f, -14f),
                new Vector2(2f, -14f),
                new Vector2(18f, -10f),
                new Vector2(28f, -12f),
                new Vector2(39f, -12f),
                new Vector2(47f, -15f)
            },
            new Color(0.003f, 0.018f, 0.035f),
            new Color(0.012f, 0.055f, 0.075f),
            new Color(0.03f, 0.52f, 0.68f, 0.25f),
            new Color(0.02f, 0.18f, 0.3f, 0.2f),
            new Color(0.05f, 0.9f, 1f),
            new Color(1f, 0.72f, 0.08f),
            new Color(0.045f, 0.075f, 0.105f),
            new Color(0.025f, 0.045f, 0.075f),
            new Color(0.08f, 0.92f, 1f),
            RaceTrackTheme.NeonCity),
        new RaceTrackDefinition(
            "ХРУСТАЛЬНЫЙ ЛАБИРИНТ",
            "КРИО-ЛАБ",
            "ЛЕДЯНЫЕ ТОННЕЛИ  /  ТЕХНИЧНЫЕ ПЕТЛИ И ДВОЙНОЙ АПЕКС",
            new[]
            {
                new Vector2(6f, -46f),
                new Vector2(2f, -35f),
                new Vector2(-20f, -31f),
                new Vector2(-36f, -22f),
                new Vector2(-45f, -8f),
                new Vector2(-42f, 9f),
                new Vector2(-31f, 23f),
                new Vector2(-15f, 31f),
                new Vector2(5f, 34f),
                new Vector2(26f, 30f),
                new Vector2(43f, 20f),
                new Vector2(49f, 5f),
                new Vector2(46f, -8f),
                new Vector2(42f, -12f),
                new Vector2(26f, -12f),
                new Vector2(18f, -2f),
                new Vector2(24f, 10f),
                new Vector2(17f, 20f),
                new Vector2(5f, 18f),
                new Vector2(-4f, 9f),
                new Vector2(-15f, 14f),
                new Vector2(-27f, 9f),
                new Vector2(-30f, -2f),
                new Vector2(-23f, -12f),
                new Vector2(-10f, -17f),
                new Vector2(5f, -14f),
                new Vector2(35f, -26f),
                new Vector2(45f, -33f),
                new Vector2(46f, -42f),
                new Vector2(38f, -51f),
                new Vector2(22f, -53f)
            },
            new Color(0.008f, 0.035f, 0.085f),
            new Color(0.035f, 0.12f, 0.18f),
            new Color(0.22f, 0.78f, 0.92f, 0.28f),
            new Color(0.56f, 0.34f, 0.9f, 0.16f),
            new Color(0.68f, 0.98f, 1f),
            new Color(0.7f, 0.28f, 1f),
            new Color(0.095f, 0.16f, 0.24f),
            new Color(0.055f, 0.095f, 0.17f),
            new Color(0.38f, 0.94f, 1f),
            RaceTrackTheme.Glacier),
        new RaceTrackDefinition(
            "СОЛНЕЧНЫЕ ДЮНЫ",
            "СОЛЯРИС",
            "ПЕСКИ  /  СКОРОСТНЫЕ ГРЕБНИ И ШИРОКИЕ ДУГИ",
            new[]
            {
                new Vector2(48f, -46f),
                new Vector2(28f, -47f),
                new Vector2(5f, -43f),
                new Vector2(-25f, -34f),
                new Vector2(-45f, -22f),
                new Vector2(-52f, -4f),
                new Vector2(-45f, 15f),
                new Vector2(-28f, 31f),
                new Vector2(-5f, 39f),
                new Vector2(18f, 35f),
                new Vector2(38f, 24f),
                new Vector2(50f, 8f),
                new Vector2(46f, -4f),
                new Vector2(33f, -8f),
                new Vector2(22f, 2f),
                new Vector2(28f, 13f),
                new Vector2(17f, 22f),
                new Vector2(5f, 18f),
                new Vector2(-4f, 8f),
                new Vector2(-17f, 14f),
                new Vector2(-30f, 7f),
                new Vector2(-34f, -6f),
                new Vector2(-23f, -16f),
                new Vector2(-5f, -19f),
                new Vector2(12f, -15f),
                new Vector2(24f, -23f),
                new Vector2(40f, -20f),
                new Vector2(54f, -21f),
                new Vector2(63f, -27f),
                new Vector2(64f, -36f),
                new Vector2(57f, -43f)
            },
            new Color(0.14f, 0.055f, 0.008f),
            new Color(0.46f, 0.23f, 0.045f),
            new Color(0.9f, 0.48f, 0.08f, 0.3f),
            new Color(0.36f, 0.12f, 0.025f, 0.24f),
            new Color(1f, 0.62f, 0.06f),
            new Color(1f, 0.94f, 0.58f),
            new Color(0.16f, 0.115f, 0.075f),
            new Color(0.105f, 0.075f, 0.055f),
            new Color(1f, 0.52f, 0.06f),
            RaceTrackTheme.Desert)
    };

    public static int Count { get { return Tracks.Length; } }

    public static RaceTrackDefinition Get(int index)
    {
        return Tracks[Mathf.Clamp(index, 0, Tracks.Length - 1)];
    }
}
