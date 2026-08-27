using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed partial class NeonCircuitGame : MonoBehaviour
{
    public const float TrackWidth = 7.2f;
    private const int LapCheckpointCount = 24;
    private const float MinimumValidLapRatio = 0.86f;
    private const int MinimapTrackSamples = 256;
    private const int MinimapTextureWidth = 768;
    private const int MinimapTextureHeight = 432;
    private const float PickupObstacleClearance = 1.05f;
    private const int PickupPlacementLayerMask = ~0;
    private static readonly float[] PickupLaneSearchOffsets = { 0f, -0.9f, 0.9f, -1.8f, 1.8f, -2.7f, 2.7f };
    private static readonly float[] PickupTrackSearchOffsets = { 0f, 0.012f, -0.012f, 0.024f, -0.024f, 0.04f, -0.04f };
    private Vector2[] TrackNodes { get { return ActiveTrack.Nodes; } }
    private RaceTrackDefinition ActiveTrack { get { return RaceTrackCatalog.Get(selectedTrackIndex); } }
    public const int TotalLaps = 3;
    public const float PlayerStartT = -0.045f;
    public const float PlayerStartLane = -1.65f;
    private const int MaxUpgradeLevel = 5;
    private const string CoinsKey = "NeonCircuit.Coins";
    private const string EngineKey = "NeonCircuit.Engine";
    private const string HandlingKey = "NeonCircuit.Handling";
    private const string ArmorKey = "NeonCircuit.Armor";
    private const string WeaponDamageKey = "NeonCircuit.WeaponDamage";
    private const string WeaponAmmoKey = "NeonCircuit.WeaponAmmo";
    private const string WeaponRateKey = "NeonCircuit.WeaponRate";
    private const string SelectedCarKey = "NeonCircuit.SelectedCar";
    private const string PaintKey = "NeonCircuit.Paint";
    private const string NeonKey = "NeonCircuit.Neon";
    private const string TrackKey = "NeonCircuit.SelectedTrack";
    private const string OwnedCarPrefix = "NeonCircuit.OwnedCar.";
    private static readonly string[] CarNames =
    {
        "NEON R", "VOLT S", "DRIFT RX", "TITAN GT",
        "PHANTOM X", "RAPTOR 4X", "BLAZE RS", "NOVA LM", "ZENITH Q"
    };
    private static readonly string[] CarClasses =
    {
        "СБАЛАНСИРОВАННЫЙ", "СКОРОСТЬ", "ДРИФТ", "БРОНЯ",
        "ГИПЕРКАР", "РАЛЛИ", "МОЩНОСТЬ", "ПРОТОТИП", "МАНЁВРЕННОСТЬ"
    };
    private static readonly int[] CarPrices = { 0, 850, 1250, 1700, 2300, 2600, 2900, 3400, 3000 };
    private static readonly float[] CarAcceleration = { 1f, 1.1f, 1.03f, 0.96f, 1.14f, 1.02f, 1.15f, 1.12f, 1.07f };
    private static readonly float[] CarTopSpeed = { 1f, 1.12f, 1.04f, 0.97f, 1.18f, 1.01f, 1.09f, 1.2f, 1.06f };
    private static readonly float[] CarHandling = { 1f, 0.96f, 1.13f, 0.94f, 1.02f, 1.08f, 0.92f, 1.1f, 1.2f };
    private static readonly float[] CarDamage = { 1f, 1.08f, 1f, 0.7f, 1.1f, 0.78f, 0.88f, 1.05f, 0.98f };
    private static readonly Vector3[] CarScales =
    {
        new Vector3(1f, 1f, 1f),
        new Vector3(0.92f, 1.08f, 1f),
        new Vector3(1.08f, 0.96f, 1f),
        new Vector3(1.14f, 1.05f, 1f),
        new Vector3(0.9f, 1.1f, 1f),
        new Vector3(1.12f, 1.08f, 1f),
        new Vector3(1.1f, 1.02f, 1f),
        new Vector3(0.94f, 1.12f, 1f),
        new Vector3(0.96f, 1f, 1f)
    };
    private static readonly Color[] PaintColors =
    {
        new Color(1f, 0.34f, 0.08f),
        new Color(0.04f, 0.74f, 0.86f),
        new Color(0.92f, 0.12f, 0.22f),
        new Color(0.72f, 0.08f, 0.92f),
        new Color(0.96f, 0.78f, 0.08f),
        new Color(0.88f, 0.92f, 0.9f),
        new Color(0.08f, 0.72f, 0.28f),
        new Color(0.12f, 0.34f, 0.96f),
        new Color(1f, 0.16f, 0.56f),
        new Color(0.075f, 0.09f, 0.13f)
    };
    private static readonly Color[] NeonColors =
    {
        new Color(0.12f, 0.94f, 1f),
        new Color(1f, 0.26f, 0.08f),
        new Color(0.35f, 1f, 0.34f),
        new Color(0.95f, 0.2f, 0.78f),
        new Color(1f, 0.84f, 0.18f)
    };
    private static readonly int PaintColorShaderProperty = Shader.PropertyToID("_PaintColor");

    private readonly List<CircuitAI> opponents = new List<CircuitAI>();
    private readonly List<WeaponPickup> weaponPickups = new List<WeaponPickup>();
    private readonly List<RepairPickup> repairPickups = new List<RepairPickup>();
    private readonly List<TrackObstacle> trackObstacles = new List<TrackObstacle>();
    private Sprite pixelSprite;
    private Sprite circleSprite;
    private Sprite[] trackCarSprites;
    private Sprite[] trackBrokenCarSprites;
    private Sprite[] trackBrokenCarVariant2Sprites;
    private Texture2D pixelTexture;
    private Texture2D circleTexture;
    private Texture2D asphaltTexture;
    private Texture2D panelTexture;
    private Texture2D minimapTrackTexture;
    private Material driftTrailMaterial;
    private Material carPaintMaterial;
    private readonly SpriteRenderer[] startLightRenderers = new SpriteRenderer[5];
    private ArcadeCarController player;
    private CarDamage playerDamage;
    private PlayerWeaponSystem playerWeapon;
    private float countdown = 3.8f;
    private float raceTime;
    private float lapStartedAt;
    private float bestLap = float.PositiveInfinity;
    private float finishTime;
    private int completedLaps;
    private int lastCheckpointSector;
    private uint lapCheckpointMask;
    private float lastTrackParameter;
    private float validatedLapProgress;
    private bool trackProgressInitialized;
    private bool raceStarted;
    private bool raceFinished;
    private bool playerWrecked;
    private bool garageOpen;
    private bool mainMenuOpen = true;
    private bool trackLoadPending;
    private int builtTrackIndex;
    private static bool startRaceAfterSceneReload;
    private float menuAnimationStartedAt;
    private int coins;
    private int engineLevel;
    private int handlingLevel;
    private int armorLevel;
    private int weaponDamageLevel;
    private int weaponAmmoLevel;
    private int weaponRateLevel;
    private int garageUpgradeTab;
    private int selectedCarIndex;
    private int garageCarIndex;
    private int paintColorIndex;
    private int neonColorIndex;
    private int selectedTrackIndex;
    private int lastFinishReward;
    private string garageMessage = string.Empty;
    private float garageMessageUntil;
    private GUIStyle hudStyle;
    private GUIStyle labelStyle;
    private GUIStyle bigStyle;
    private GUIStyle smallStyle;
    private GUIStyle menuTitleStyle;
    private GUIStyle menuButtonStyle;
    private GUIStyle centeredStyle;
    private GUIStyle heroTitleStyle;
    private GUIStyle kickerStyle;
    private GUIStyle actionTitleStyle;
    private GUIStyle actionNumberStyle;
    private GUIStyle microStyle;
    private float hitFlashAmount;
    private float nitroFlashAmount;
    private float driftSfxCooldownUntil;
    private float nitroSfxCooldownUntil;
    private float impactSfxCooldownUntil;
    private AudioSource sfxSource;
    private AudioClip driftSfxClip;
    private AudioClip driftBoostSfxClip;
    private AudioClip nitroSfxClip;
    private AudioClip impactSfxClip;
    private Texture2D overlayTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private Texture2D buttonActiveTexture;
    private readonly Vector2[] minimapTrackPoints = new Vector2[MinimapTrackSamples + 1];
    private readonly List<Vector2> minimapObstaclePoints = new List<Vector2>();
    private Vector2 minimapWorldMin;
    private Vector2 minimapWorldMax;

    public bool RaceStarted { get { return raceStarted; } }
    public bool RaceFinished { get { return raceFinished; } }
    public float RaceTime { get { return raceTime; } }
    public float EngineAccelerationMultiplier { get { return (1f + engineLevel * 0.10f) * CarAcceleration[selectedCarIndex] * StoryAccelerationFactor * ArcadeAccelerationFactor; } }
    public float TopSpeedMultiplier { get { return (1f + engineLevel * 0.06f) * CarTopSpeed[selectedCarIndex] * StoryTopSpeedFactor * ArcadeTopSpeedFactor; } }
    public float HandlingMultiplier { get { return (1f + handlingLevel * 0.08f) * CarHandling[selectedCarIndex] * WeatherGripMultiplier * StoryHandlingFactor * ArcadeHandlingFactor; } }
    public float ImpactSpeedRetention { get { return 0.78f + armorLevel * 0.028f; } }
    public float ImpactAngularRetention { get { return 0.65f + armorLevel * 0.04f; } }
    public float PlayerDamageMultiplier { get { return Mathf.Clamp((1f - armorLevel * 0.08f) * CarDamage[selectedCarIndex] * StoryDamageFactor * ArcadeDamageFactor, 0.35f, 1.35f); } }
    public float RivalAccelerationMultiplier { get { return (1.34f + engineLevel * 0.12f) * StoryAccelerationFactor * ArcadeAccelerationFactor; } }
    public float RivalSpeedMultiplier { get { return (1.28f + engineLevel * 0.075f) * StoryTopSpeedFactor * ArcadeTopSpeedFactor; } }
    public float RivalHandlingMultiplier { get { return (1.1f + handlingLevel * 0.09f) * WeatherGripMultiplier * StoryHandlingFactor * ArcadeHandlingFactor; } }
    public float RivalDamageMultiplier { get { return Mathf.Clamp(1f - armorLevel * 0.08f, 0.6f, 1f); } }
    public float PlayerWeaponDamageMultiplier { get { return 1f + weaponDamageLevel * 0.16f; } }
    public int PlayerWeaponMaxAmmo { get { return 9 + weaponAmmoLevel * 2; } }
    public float PlayerWeaponCooldownMultiplier { get { return Mathf.Max(0.58f, 1f - weaponRateLevel * 0.08f); } }
    public Material DriftTrailMaterial { get { return driftTrailMaterial; } }

    private void Awake()
    {
        Application.targetFrameRate = 120;
        QualitySettings.antiAliasing = Mathf.Max(QualitySettings.antiAliasing, 4);
        Time.timeScale = 1f;
        LoadProgress();
        LoadWeatherSetting();
        selectedTrackIndex = Mathf.Clamp(PlayerPrefs.GetInt(TrackKey, 0), 0, RaceTrackCatalog.Count - 1);
        builtTrackIndex = selectedTrackIndex;
        PreparePendingArcadeRaceBeforeWorldBuild();
        PreparePendingStoryRaceBeforeWorldBuild();
        CacheMinimapTrack();
        CreateSharedVisuals();
        SetupRuntimeAudio();
        BuildWorld();
        SpawnRacers();
        SetupCamera();
        ApplySelectedCarVisuals();
        Time.timeScale = 0f;
        menuAnimationStartedAt = Time.unscaledTime;
        if (startRaceAfterSceneReload)
        {
            startRaceAfterSceneReload = false;
            RestorePendingStoryRaceAfterReload();
            RestorePendingArcadeRaceAfterReload();
            StartCoroutine(BeginRaceAfterReload());
        }
    }

    private void CreateSharedVisuals()
    {
        pixelTexture = new Texture2D(1, 1);
        pixelTexture.name = "Runtime Pixel";
        pixelTexture.SetPixel(0, 0, Color.white);
        pixelTexture.Apply();
        pixelSprite = Sprite.Create(pixelTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        pixelSprite.name = "Runtime Pixel Sprite";
        asphaltTexture = CreateAsphaltTexture();
        trackCarSprites = new[]
        {
            Resources.Load<Sprite>("UI/TrackCarSprite"),
            Resources.Load<Sprite>("UI/TrackCarVoltS"),
            Resources.Load<Sprite>("UI/TrackCarDriftRX"),
            Resources.Load<Sprite>("UI/TrackCarTitanGT"),
            Resources.Load<Sprite>("UI/TrackCarPhantomX"),
            Resources.Load<Sprite>("UI/TrackCarRaptor4X"),
            Resources.Load<Sprite>("UI/TrackCarBlazeRS"),
            Resources.Load<Sprite>("UI/TrackCarNovaLM"),
            Resources.Load<Sprite>("UI/TrackCarZenithQ")
        };
        trackBrokenCarSprites = new[]
        {
            Resources.Load<Sprite>("UI/TrackCarBrokenNeonR"),
            Resources.Load<Sprite>("UI/TrackCarBrokenVoltS"),
            Resources.Load<Sprite>("UI/TrackCarBrokenDriftRX"),
            Resources.Load<Sprite>("UI/TrackCarBrokenTitanGT"),
            null,
            null,
            null,
            null,
            null
        };
        trackBrokenCarVariant2Sprites = new[]
        {
            Resources.Load<Sprite>("UI/TrackCarBrokenNeonRVariant2"),
            Resources.Load<Sprite>("UI/TrackCarBrokenVoltSVariant2"),
            Resources.Load<Sprite>("UI/TrackCarBrokenDriftRXVariant2"),
            Resources.Load<Sprite>("UI/TrackCarBrokenTitanGTVariant2"),
            null,
            null,
            null,
            null,
            null
        };
        LoadStoryVehicleSprites();

        CreateCircleSprite();
        CreateMinimapTrackTexture();

        panelTexture = new Texture2D(1, 1);
        panelTexture.SetPixel(0, 0, new Color(0.025f, 0.04f, 0.055f, 0.9f));
        panelTexture.Apply();

        overlayTexture = CreateSolidTexture(new Color(0.005f, 0.02f, 0.024f, 0.9f));
        buttonTexture = CreateSolidTexture(new Color(0.045f, 0.13f, 0.15f, 0.98f));
        buttonHoverTexture = CreateSolidTexture(new Color(0.06f, 0.46f, 0.52f, 0.98f));
        buttonActiveTexture = CreateSolidTexture(new Color(1f, 0.31f, 0.08f, 0.98f));
        driftTrailMaterial = new Material(Shader.Find("Sprites/Default"));
        if (driftTrailMaterial != null)
        {
            driftTrailMaterial.color = new Color(1f, 1f, 1f, 0.42f);
        }

        Shader paintShader = Resources.Load<Shader>("Shaders/CarPaint");
        if (paintShader != null)
        {
            carPaintMaterial = new Material(paintShader);
            carPaintMaterial.name = "Runtime Car Paint Material";
        }
    }

    private void SetupRuntimeAudio()
    {
        if (sfxSource != null)
        {
            return;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 0.9f;

        driftSfxClip = CreateToneClip("DriftLoop", 160f, 0.12f, 0.18f);
        driftBoostSfxClip = CreateToneClip("DriftBoostLoop", 220f, 0.12f, 0.22f);
        nitroSfxClip = CreateToneClip("NitroLoop", 90f, 0.16f, 0.32f);
        impactSfxClip = CreateToneClip("Impact", 40f, 0.09f, 0.28f, true);
        SetupExtendedAudio();
    }

    private AudioClip CreateToneClip(string name, float frequency, float duration, float volume, bool decay = false)
    {
        const int SampleRate = 22050;
        int length = Mathf.Max(1, Mathf.CeilToInt(SampleRate * duration));
        float[] samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float time = i / (float)SampleRate;
            float tone = Mathf.Sin(time * frequency * Mathf.PI * 2f);
            float envelope = decay ? Mathf.Pow(1f - time / duration, 2f) : 1f;
            samples[i] = tone * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(name, length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private Texture2D CreateAsphaltTexture()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Detailed Asphalt";
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Repeat;
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float broadNoise = Mathf.PerlinNoise(x * 0.055f + 3.1f, y * 0.055f + 7.4f);
                float fineNoise = Mathf.Repeat(Mathf.Sin((x + 11f) * 12.9898f + (y + 23f) * 78.233f) * 43758.5453f, 1f);
                float value = 0.7f + broadNoise * 0.18f + (fineNoise - 0.5f) * 0.085f;
                bool aggregate = fineNoise > 0.965f;
                if (aggregate)
                {
                    value += 0.12f;
                }

                pixels[y * size + x] = new Color(value * 0.96f, value * 0.985f, value, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private void CreateCircleSprite()
    {
        const int size = 128;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTexture.name = "Runtime Smooth Circle";
        circleTexture.filterMode = FilterMode.Bilinear;
        circleTexture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f - 1.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius + 0.75f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        circleTexture.SetPixels(pixels);
        circleTexture.Apply();
        circleSprite = Sprite.Create(circleTexture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        circleSprite.name = "Runtime Smooth Circle Sprite";
    }

    private void BuildWorld()
    {
        RaceTrackDefinition track = ActiveTrack;
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.backgroundColor = track.CameraColor;
        }

        CreateVisual("Track Base", Vector2.zero, new Vector2(116f, 92f), track.GroundColor, -30, 0f, transform);

        for (int i = 0; i < 92; i++)
        {
            float x = Mathf.Lerp(-56f, 56f, Mathf.Repeat(i * 0.6180339f + 0.17f, 1f));
            float y = Mathf.Lerp(-44f, 44f, Mathf.Repeat(i * 0.4142136f + 0.31f, 1f));
            float width = 3.8f + Mathf.Repeat(i * 1.731f, 1f) * 7.5f;
            float height = 1.6f + Mathf.Repeat(i * 2.173f, 1f) * 3.8f;
            Color terrainColor = i % 2 == 0 ? track.GroundStripeA : track.GroundStripeB;
            terrainColor.a *= 0.62f;
            CreateVisual("Organic Terrain Variation", new Vector2(x, y), new Vector2(width, height), terrainColor, -29, i * 37f % 180f, transform, false, circleSprite);
        }

        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.PI * 2f / 32f;
            float radiusX = 53f + Mathf.Sin(i * 1.9f) * 2f;
            float radiusY = 42f + Mathf.Cos(i * 1.4f) * 1.5f;
            CreateTree(new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY), i);
        }

        const int trackMeshSegmentCount = 1024;
        float meshCurbOffset = TrackWidth * 0.5f + 0.14f;
        Color looseShoulderA = Color.Lerp(track.GroundColor, track.AsphaltA, ActiveTrack.IsGlacier ? 0.24f : 0.38f);
        Color looseShoulderB = Color.Lerp(track.GroundColor, track.AsphaltB, ActiveTrack.IsLunar ? 0.34f : 0.48f);
        if (ActiveTrack.IsVolcanic)
        {
            looseShoulderA = new Color(0.12f, 0.045f, 0.04f);
            looseShoulderB = new Color(0.075f, 0.028f, 0.035f);
        }
        else if (ActiveTrack.IsGlacier)
        {
            looseShoulderA = new Color(0.24f, 0.39f, 0.52f);
            looseShoulderB = new Color(0.44f, 0.62f, 0.72f);
        }

        CreateTrackStrip("Track Shadow", TrackWidth + 2.75f, new Color(0.005f, 0.012f, 0.016f, 0.78f), new Color(0.005f, 0.012f, 0.016f, 0.78f), 1, -25, trackMeshSegmentCount, 0f, new Vector2(0.28f, -0.26f));
        CreateTrackStrip("Loose Track Shoulder", TrackWidth + 2.08f, looseShoulderA, looseShoulderB, 22, -24, trackMeshSegmentCount, 0f, Vector2.zero, asphaltTexture, 40f);
        CreateTrackStrip("Compacted Track Shoulder", TrackWidth + 1.42f, Color.Lerp(looseShoulderA, track.AsphaltA, 0.34f), Color.Lerp(looseShoulderB, track.AsphaltB, 0.34f), 36, -23, trackMeshSegmentCount, 0f, Vector2.zero, asphaltTexture, 36f);
        CreateTrackStrip("Road Foundation", TrackWidth + 1.08f, Color.Lerp(track.AsphaltA, Color.black, 0.42f), Color.Lerp(track.AsphaltA, Color.black, 0.42f), 1, -22, trackMeshSegmentCount, 0f, Vector2.zero);
        CreateTrackStrip("Asphalt Edge", TrackWidth + 0.24f, new Color(0.045f, 0.052f, 0.056f), new Color(0.045f, 0.052f, 0.056f), 1, -21, trackMeshSegmentCount, 0f, Vector2.zero);
        Color asphaltBlend = Color.Lerp(track.AsphaltA, track.AsphaltB, 0.45f);
        CreateTrackStrip("Detailed Asphalt Surface", TrackWidth - 0.05f, asphaltBlend, Color.Lerp(asphaltBlend, Color.white, 0.025f), 96, -20, trackMeshSegmentCount, 0f, Vector2.zero, asphaltTexture, 32f);
        CreateTrackStrip("Road Crown Highlight", 2.8f, new Color(0.65f, 0.68f, 0.69f, 0.022f), new Color(0.42f, 0.45f, 0.47f, 0.012f), 64, -19, trackMeshSegmentCount, 0f, Vector2.zero);
        CreateTrackStrip("Left Curb Foundation", 0.76f, new Color(0.045f, 0.048f, 0.05f), new Color(0.045f, 0.048f, 0.05f), 1, -19, trackMeshSegmentCount, meshCurbOffset, Vector2.zero);
        CreateTrackStrip("Right Curb Foundation", 0.76f, new Color(0.045f, 0.048f, 0.05f), new Color(0.045f, 0.048f, 0.05f), 1, -19, trackMeshSegmentCount, -meshCurbOffset, Vector2.zero);
        CreateTrackStrip("Left Curb Strip", 0.52f, track.CurbA, track.CurbB, 5, -18, trackMeshSegmentCount, meshCurbOffset, Vector2.zero);
        CreateTrackStrip("Right Curb Strip", 0.52f, track.CurbA, track.CurbB, 5, -18, trackMeshSegmentCount, -meshCurbOffset, Vector2.zero);
        CreateTrackStrip("Left Curb Inner Bevel", 0.075f, new Color(0.96f, 0.98f, 0.94f, 0.72f), new Color(0.68f, 0.72f, 0.7f, 0.62f), 5, -17, trackMeshSegmentCount, meshCurbOffset - 0.23f, Vector2.zero);
        CreateTrackStrip("Right Curb Inner Bevel", 0.075f, new Color(0.96f, 0.98f, 0.94f, 0.72f), new Color(0.68f, 0.72f, 0.7f, 0.62f), 5, -17, trackMeshSegmentCount, -meshCurbOffset + 0.23f, Vector2.zero);
        CreateTrackStrip("Left Curb Outer Shadow", 0.13f, new Color(0.005f, 0.008f, 0.01f, 0.58f), new Color(0.008f, 0.01f, 0.012f, 0.48f), 5, -17, trackMeshSegmentCount, meshCurbOffset + 0.25f, Vector2.zero);
        CreateTrackStrip("Right Curb Outer Shadow", 0.13f, new Color(0.005f, 0.008f, 0.01f, 0.58f), new Color(0.008f, 0.01f, 0.012f, 0.48f), 5, -17, trackMeshSegmentCount, -meshCurbOffset - 0.25f, Vector2.zero);
        CreateTrackStrip("Left Road Edge Line", 0.1f, new Color(0.92f, 0.94f, 0.9f, 0.92f), new Color(0.92f, 0.94f, 0.9f, 0.92f), 1, -17, trackMeshSegmentCount, TrackWidth * 0.5f - 0.22f, Vector2.zero);
        CreateTrackStrip("Right Road Edge Line", 0.1f, new Color(0.92f, 0.94f, 0.9f, 0.92f), new Color(0.92f, 0.94f, 0.9f, 0.92f), 1, -17, trackMeshSegmentCount, -TrackWidth * 0.5f + 0.22f, Vector2.zero);
        Color rubberGroove = new Color(0.004f, 0.007f, 0.009f, 0.17f);
        Color rubberGap = new Color(0.004f, 0.007f, 0.009f, 0f);
        CreateTrackStrip("Left Racing Rubber Groove", 0.2f, rubberGroove, rubberGap, 13, -16, trackMeshSegmentCount, 0.54f, Vector2.zero);
        CreateTrackStrip("Right Racing Rubber Groove", 0.2f, rubberGap, rubberGroove, 17, -16, trackMeshSegmentCount, -0.54f, Vector2.zero);
        CreateTrackStrip("Left Drainage Channel", 0.12f, new Color(0.008f, 0.014f, 0.017f, 0.56f), new Color(0.03f, 0.04f, 0.043f, 0.48f), 18, -16, trackMeshSegmentCount, TrackWidth * 0.5f - 0.48f, Vector2.zero);
        CreateTrackStrip("Right Drainage Channel", 0.12f, new Color(0.008f, 0.014f, 0.017f, 0.56f), new Color(0.03f, 0.04f, 0.043f, 0.48f), 18, -16, trackMeshSegmentCount, -TrackWidth * 0.5f + 0.48f, Vector2.zero);
        CreateRoadSurfaceDetails();
        CreateTracksideRealismDetails();
        CreateEnvironmentalTrackContamination();

        const int segmentCount = 384;
        for (int i = 0; i < segmentCount; i++)
        {
            float t0 = i * Mathf.PI * 2f / segmentCount;
            float t1 = (i + 1) * Mathf.PI * 2f / segmentCount;
            Vector2 a = PathPoint(t0, 0f);
            Vector2 b = PathPoint(t1, 0f);
            Vector2 midpoint = (a + b) * 0.5f;
            Vector2 delta = b - a;
            Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f;
            float length = delta.magnitude + 0.18f;
            if (i % 9 < 4)
            {
                CreateVisual("Lane Dash Shadow", midpoint + new Vector2(0.05f, -0.05f), new Vector2(0.16f, length * 0.74f), new Color(0f, 0f, 0f, 0.38f), -18, angle, transform);
                CreateVisual("Lane Dash", midpoint, new Vector2(0.1f, length * 0.72f), new Color(1f, 0.78f, 0.18f, 0.9f), -17, angle, transform);
            }

            if (i % 17 == 5 || i % 17 == 6)
            {
                float skidOffset = i % 2 == 0 ? 0.85f : -0.85f;
                CreateVisual("Skid Mark", midpoint + normal * skidOffset, new Vector2(0.09f, length * 1.45f), new Color(0.012f, 0.016f, 0.018f, 0.55f), -16, angle + (i % 3 - 1) * 4f, transform);
                CreateVisual("Skid Mark Pair", midpoint + normal * (skidOffset + 0.24f), new Vector2(0.07f, length * 1.34f), new Color(0.012f, 0.016f, 0.018f, 0.42f), -16, angle + (i % 3 - 1) * 4f, transform);
            }

            if (i % 24 == 0)
            {
                float side = (i / 24) % 2 == 0 ? 1f : -1f;
                Vector2 lampPosition = midpoint + normal * side * (TrackWidth * 0.5f + 2.1f);
                CreateVisual("Lamp Cast Shadow", lampPosition + new Vector2(0.42f, -0.38f), new Vector2(0.24f, 1.3f), new Color(0f, 0f, 0f, 0.42f), -16, angle - 28f, transform);
                CreateVisual("Lamp Glow", lampPosition, new Vector2(1.45f, 1.45f), new Color(ActiveTrack.AccentColor.r, ActiveTrack.AccentColor.g, ActiveTrack.AccentColor.b, 0.16f), -15, 45f, transform);
                CreateVisual("Lamp Base", lampPosition, new Vector2(0.42f, 0.42f), new Color(0.018f, 0.035f, 0.045f), -14, 45f, transform);
                CreateVisual("Lamp Core", lampPosition, new Vector2(0.17f, 0.17f), ActiveTrack.AccentColor, -13, 45f, transform);
            }

            if (i % 36 == 18)
            {
                float side = (i / 36) % 2 == 0 ? -1f : 1f;
                Vector2 signPosition = midpoint + normal * side * (TrackWidth * 0.5f + 1.35f);
                CreateVisual("Direction Sign Back", signPosition, new Vector2(1.15f, 0.4f), new Color(0.015f, 0.035f, 0.045f), -14, angle, transform);
                CreateVisual("Direction Sign", signPosition, new Vector2(0.78f, 0.1f), ActiveTrack.AccentColor, -13, angle + 18f, transform);
            }
        }

        CreateTrackObstacles();
        CreateWeaponPickups();
        CreateRepairPickups();
        CreateRainPuddles();
        CreateStartLine();
        CreateInfieldDecor();
    }

    private void CreateTrackStrip(string objectName, float width, Color colorA, Color colorB, int colorSpan, int sortingOrder, int segmentCount, float laneOffset, Vector2 worldOffset, Texture2D surfaceTexture = null, float textureRepeat = 1f)
    {
        Vector2[] pathCenters = new Vector2[segmentCount];
        Vector2[] centers = new Vector2[segmentCount];
        Vector2[] normals = new Vector2[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i * Mathf.PI * 2f / segmentCount;
            pathCenters[i] = TrackCenter(t);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int previous = (i - 1 + segmentCount) % segmentCount;
            int next = (i + 1) % segmentCount;
            Vector2 tangent = (pathCenters[next] - pathCenters[previous]).normalized;
            normals[i] = new Vector2(-tangent.y, tangent.x);
            centers[i] = pathCenters[i] + normals[i] * laneOffset + worldOffset;
        }

        Vector3[] vertices = new Vector3[segmentCount * 8];
        Vector2[] uv = new Vector2[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[segmentCount * 18];
        float halfWidth = width * 0.5f;
        const float edgeFeather = 0.09f;
        int safeColorSpan = Mathf.Max(1, colorSpan);

        for (int i = 0; i < segmentCount; i++)
        {
            int next = (i + 1) % segmentCount;
            int vertex = i * 8;
            int triangle = i * 18;
            Color segmentColor = (i / safeColorSpan) % 2 == 0 ? colorA : colorB;
            Color transparentColor = new Color(segmentColor.r, segmentColor.g, segmentColor.b, 0f);

            vertices[vertex] = centers[i] + normals[i] * (halfWidth + edgeFeather);
            vertices[vertex + 1] = centers[i] + normals[i] * halfWidth;
            vertices[vertex + 2] = centers[i] - normals[i] * halfWidth;
            vertices[vertex + 3] = centers[i] - normals[i] * (halfWidth + edgeFeather);
            vertices[vertex + 4] = centers[next] + normals[next] * (halfWidth + edgeFeather);
            vertices[vertex + 5] = centers[next] + normals[next] * halfWidth;
            vertices[vertex + 6] = centers[next] - normals[next] * halfWidth;
            vertices[vertex + 7] = centers[next] - normals[next] * (halfWidth + edgeFeather);

            float u0 = i / (float)segmentCount * textureRepeat;
            float u1 = (i + 1f) / segmentCount * textureRepeat;
            uv[vertex] = new Vector2(u0, 1f);
            uv[vertex + 1] = new Vector2(u0, 0.96f);
            uv[vertex + 2] = new Vector2(u0, 0.04f);
            uv[vertex + 3] = new Vector2(u0, 0f);
            uv[vertex + 4] = new Vector2(u1, 1f);
            uv[vertex + 5] = new Vector2(u1, 0.96f);
            uv[vertex + 6] = new Vector2(u1, 0.04f);
            uv[vertex + 7] = new Vector2(u1, 0f);

            colors[vertex] = transparentColor;
            colors[vertex + 1] = segmentColor;
            colors[vertex + 2] = segmentColor;
            colors[vertex + 3] = transparentColor;
            colors[vertex + 4] = transparentColor;
            colors[vertex + 5] = segmentColor;
            colors[vertex + 6] = segmentColor;
            colors[vertex + 7] = transparentColor;

            triangles[triangle] = vertex;
            triangles[triangle + 1] = vertex + 4;
            triangles[triangle + 2] = vertex + 1;
            triangles[triangle + 3] = vertex + 4;
            triangles[triangle + 4] = vertex + 5;
            triangles[triangle + 5] = vertex + 1;

            triangles[triangle + 6] = vertex + 1;
            triangles[triangle + 7] = vertex + 5;
            triangles[triangle + 8] = vertex + 2;
            triangles[triangle + 9] = vertex + 5;
            triangles[triangle + 10] = vertex + 6;
            triangles[triangle + 11] = vertex + 2;

            triangles[triangle + 12] = vertex + 2;
            triangles[triangle + 13] = vertex + 6;
            triangles[triangle + 14] = vertex + 3;
            triangles[triangle + 15] = vertex + 6;
            triangles[triangle + 16] = vertex + 7;
            triangles[triangle + 17] = vertex + 3;
        }

        Mesh mesh = new Mesh();
        mesh.name = objectName + " Mesh";
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();

        GameObject strip = new GameObject(objectName);
        strip.transform.SetParent(transform, false);
        strip.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = strip.AddComponent<MeshRenderer>();
        Shader shader = Shader.Find("Sprites/Default");
        Material material = new Material(shader);
        material.name = objectName + " Material";
        material.mainTexture = surfaceTexture != null ? surfaceTexture : pixelTexture;
        renderer.sharedMaterial = material;
        renderer.sortingOrder = sortingOrder;
    }

    private void CreateRoadSurfaceDetails()
    {
        for (int i = 0; i < 58; i++)
        {
            float t = Mathf.Repeat(0.018f + i * 0.061803f, 1f) * Mathf.PI * 2f;
            float lane = Mathf.Lerp(-2.65f, 2.65f, Mathf.Repeat(i * 0.381966f + 0.21f, 1f));
            Vector2 position = PathPoint(t, lane);
            float rotation = PathRotation(t) + Mathf.Lerp(-20f, 20f, Mathf.Repeat(i * 0.713f, 1f));
            float patchWidth = 0.55f + Mathf.Repeat(i * 1.37f, 1f) * 1.5f;
            float patchLength = 0.35f + Mathf.Repeat(i * 2.11f, 1f) * 1.25f;
            Color patchColor = i % 3 == 0
                ? new Color(0.015f, 0.02f, 0.023f, 0.26f)
                : new Color(0.24f, 0.26f, 0.27f, 0.09f);
            CreateVisual("Asphalt Wear Patch", position, new Vector2(patchWidth, patchLength), patchColor, -16, rotation, transform, false, circleSprite);

            if (i % 2 == 0)
            {
                float crackDirection = rotation + (i % 4 < 2 ? 38f : -42f);
                CreateVisual("Road Crack", position, new Vector2(0.035f, patchLength * 1.25f), new Color(0.008f, 0.011f, 0.013f, 0.58f), -15, crackDirection, transform);
                CreateVisual("Road Crack Branch", position + new Vector2(0.08f, -0.04f), new Vector2(0.025f, patchLength * 0.54f), new Color(0.008f, 0.011f, 0.013f, 0.48f), -15, crackDirection + 34f, transform);
            }
        }

        for (int i = 0; i < 28; i++)
        {
            float t = (i + 0.45f) * Mathf.PI * 2f / 28f;
            float side = i % 2 == 0 ? 1f : -1f;
            Vector2 drainPosition = PathPoint(t, side * (TrackWidth * 0.5f - 0.48f));
            float rotation = PathRotation(t);
            CreateVisual("Road Drain", drainPosition, new Vector2(0.28f, 0.52f), new Color(0.012f, 0.018f, 0.02f, 0.88f), -15, rotation, transform);
            CreateVisual("Road Drain Highlight", drainPosition, new Vector2(0.045f, 0.44f), new Color(0.38f, 0.42f, 0.43f, 0.62f), -14, rotation, transform);
        }

        for (int i = 0; i < 24; i++)
        {
            float t = Mathf.Repeat(0.031f + i * 0.0877f, 1f) * Mathf.PI * 2f;
            float lane = Mathf.Lerp(-2.2f, 2.2f, Mathf.Repeat(i * 0.4721f + 0.18f, 1f));
            Vector2 seamPosition = PathPoint(t, lane);
            float seamLength = Mathf.Lerp(0.65f, 1.85f, Mathf.Repeat(i * 0.318f, 1f));
            float seamRotation = PathRotation(t) + 90f + Mathf.Lerp(-16f, 16f, Mathf.Repeat(i * 0.731f, 1f));
            CreateVisual("Asphalt Repair Seam Shadow", seamPosition + new Vector2(0.035f, -0.035f), new Vector2(0.075f, seamLength), new Color(0f, 0f, 0f, 0.42f), -16, seamRotation, transform);
            CreateVisual("Asphalt Repair Seam", seamPosition, new Vector2(0.035f, seamLength * 0.94f), new Color(0.12f, 0.13f, 0.135f, 0.48f), -15, seamRotation, transform);
        }

        for (int i = 0; i < 18; i++)
        {
            float t = Mathf.Repeat(0.056f + i * 0.113f, 1f) * Mathf.PI * 2f;
            float lane = Mathf.Lerp(-2.45f, 2.45f, Mathf.Repeat(i * 0.618f + 0.37f, 1f));
            Vector2 stainPosition = PathPoint(t, lane);
            float stainSize = Mathf.Lerp(0.34f, 0.92f, Mathf.Repeat(i * 0.271f, 1f));
            CreateVisual("Road Fluid Stain", stainPosition, new Vector2(stainSize, stainSize * 0.58f), new Color(0.008f, 0.014f, 0.017f, 0.2f), -15, i * 47f % 180f, transform, false, circleSprite);
            CreateVisual("Road Fluid Stain Center", stainPosition + new Vector2(-0.06f, 0.04f), new Vector2(stainSize * 0.46f, stainSize * 0.27f), new Color(0.005f, 0.009f, 0.012f, 0.22f), -14, i * 47f % 180f, transform, false, circleSprite);
        }
    }

    private void CreateTracksideRealismDetails()
    {
        Color shoulderStone = ActiveTrack.IsGlacier
            ? new Color(0.64f, 0.8f, 0.88f, 0.78f)
            : ActiveTrack.IsLunar
                ? new Color(0.42f, 0.41f, 0.52f, 0.72f)
                : ActiveTrack.IsVolcanic
                    ? new Color(0.16f, 0.075f, 0.065f, 0.84f)
                    : ActiveTrack.IsDesert
                        ? new Color(0.58f, 0.31f, 0.12f, 0.74f)
                        : new Color(0.26f, 0.3f, 0.32f, 0.72f);

        for (int i = 0; i < 118; i++)
        {
            float t = Mathf.Repeat(0.009f + i * 0.061803f, 1f) * Mathf.PI * 2f;
            float side = i % 2 == 0 ? 1f : -1f;
            float distanceFromEdge = 0.43f + Mathf.Repeat(i * 0.417f, 1f) * 0.62f;
            Vector2 position = PathPoint(t, side * (TrackWidth * 0.5f + distanceFromEdge));
            float size = Mathf.Lerp(0.08f, 0.24f, Mathf.Repeat(i * 0.337f, 1f));
            Color stoneColor = Color.Lerp(shoulderStone, ActiveTrack.GroundStripeB, Mathf.Repeat(i * 0.283f, 1f) * 0.42f);
            CreateVisual("Loose Shoulder Aggregate", position, new Vector2(size, size * Mathf.Lerp(0.55f, 1.2f, Mathf.Repeat(i * 0.19f, 1f))), stoneColor, -16, i * 53f % 180f, transform, false, circleSprite);
        }

        const int railSampleCount = 64;
        float railStep = Mathf.PI * 2f / railSampleCount;
        int placedRailCount = 0;
        for (int i = 0; i < railSampleCount; i++)
        {
            float t = (i + 0.5f) * railStep;
            Vector2 previous = PathPoint(t - railStep * 0.34f, 0f);
            Vector2 center = PathPoint(t, 0f);
            Vector2 next = PathPoint(t + railStep * 0.34f, 0f);
            Vector2 incoming = (center - previous).normalized;
            Vector2 outgoing = (next - center).normalized;
            float signedTurn = Vector2.SignedAngle(incoming, outgoing);
            bool dangerousCorner = Mathf.Abs(signedTurn) >= 1.65f;
            if (!dangerousCorner && i % 9 != 3)
            {
                continue;
            }

            float side = dangerousCorner ? (signedTurn > 0f ? -1f : 1f) : ((i / 9) % 2 == 0 ? 1f : -1f);
            float railLane = side * (TrackWidth * 0.5f + 1.05f);
            Vector2 railPosition = PathPoint(t, railLane);
            if (!HasTracksideClearance(railPosition, t))
            {
                continue;
            }

            float railLength = Mathf.Clamp(Vector2.Distance(previous, next) * 0.82f, 2.6f, 4.9f);
            float railRotation = PathRotation(t);
            GameObject railRoot = new GameObject("Trackside Guardrail " + (placedRailCount + 1));
            railRoot.transform.SetParent(transform);
            railRoot.transform.position = railPosition;
            railRoot.transform.rotation = Quaternion.Euler(0f, 0f, railRotation);

            Color railDark = ActiveTrack.IsLunar ? new Color(0.1f, 0.09f, 0.17f) : new Color(0.055f, 0.065f, 0.07f);
            Color railMetal = ActiveTrack.IsDesert ? new Color(0.44f, 0.39f, 0.32f) : new Color(0.38f, 0.43f, 0.45f);
            Color railHighlight = ActiveTrack.IsGlacier ? new Color(0.8f, 0.95f, 1f) : new Color(0.72f, 0.76f, 0.76f);
            Color reflector = placedRailCount % 2 == 0 ? ActiveTrack.AccentColor : ActiveTrack.CurbB;

            CreateVisual("Guardrail Shadow", new Vector2(0.2f, -0.16f), new Vector2(0.62f, railLength + 0.3f), new Color(0f, 0f, 0f, 0.48f), -16, 0f, railRoot.transform, true);
            CreateVisual("Guardrail Back Plate", Vector2.zero, new Vector2(0.38f, railLength), railDark, -15, 0f, railRoot.transform, true);
            CreateVisual("Guardrail Metal Beam", new Vector2(-0.035f, 0f), new Vector2(0.2f, railLength * 0.96f), railMetal, -14, 0f, railRoot.transform, true);
            CreateVisual("Guardrail Edge Highlight", new Vector2(-0.11f, 0f), new Vector2(0.045f, railLength * 0.92f), railHighlight, -13, 0f, railRoot.transform, true);

            for (int post = -1; post <= 1; post++)
            {
                Vector2 postPosition = new Vector2(0.03f, post * railLength * 0.36f);
                CreateVisual("Guardrail Post Shadow", postPosition + new Vector2(0.11f, -0.08f), new Vector2(0.62f, 0.16f), new Color(0f, 0f, 0f, 0.5f), -14, 0f, railRoot.transform, true);
                CreateVisual("Guardrail Post", postPosition, new Vector2(0.52f, 0.12f), railMetal, -12, 0f, railRoot.transform, true);
                CreateVisual("Guardrail Reflector", postPosition + new Vector2(-0.2f, 0f), new Vector2(0.12f, 0.12f), reflector, -11, 45f, railRoot.transform, true);
            }

            if (placedRailCount % 3 == 1)
            {
                Vector2 tirePosition = PathPoint(t + railStep * 0.18f, side * (TrackWidth * 0.5f + 1.72f));
                if (HasTracksideClearance(tirePosition, t))
                {
                    CreateTracksideTireStack(tirePosition, railRotation, placedRailCount);
                }
            }

            placedRailCount++;
        }

        for (int i = 0; i < 26; i++)
        {
            float t = (i + 0.32f) * Mathf.PI * 2f / 26f;
            float side = i % 2 == 0 ? 1f : -1f;
            Vector2 reflectorPosition = PathPoint(t, side * (TrackWidth * 0.5f + 0.72f));
            float rotation = PathRotation(t);
            Color reflectorColor = i % 3 == 0 ? ActiveTrack.AccentColor : new Color(0.92f, 0.94f, 0.86f);
            CreateVisual("Roadside Reflector Shadow", reflectorPosition + new Vector2(0.08f, -0.08f), new Vector2(0.26f, 0.38f), new Color(0f, 0f, 0f, 0.48f), -15, rotation, transform);
            CreateVisual("Roadside Reflector Housing", reflectorPosition, new Vector2(0.2f, 0.32f), new Color(0.04f, 0.055f, 0.06f), -14, rotation, transform);
            CreateVisual("Roadside Reflector Lens", reflectorPosition, new Vector2(0.09f, 0.16f), reflectorColor, -13, rotation, transform);
        }

        CreateCornerRunoffZones();
        CreateCornerBrakeMarkerBoards();
    }

    private void CreateCornerRunoffZones()
    {
        const int sampleCount = 72;
        float step = Mathf.PI * 2f / sampleCount;
        int runoffIndex = 0;

        for (int i = 0; i < sampleCount && runoffIndex < 10; i++)
        {
            float t = (i + 0.5f) * step;
            Vector2 incoming = PathDerivative(t - step * 0.58f).normalized;
            Vector2 outgoing = PathDerivative(t + step * 0.58f).normalized;
            float signedTurn = Vector2.SignedAngle(incoming, outgoing);
            float severity = Mathf.Abs(signedTurn);
            if (severity < 15f)
            {
                continue;
            }

            float outsideSide = signedTurn > 0f ? -1f : 1f;
            float runoffLane = outsideSide * (TrackWidth * 0.5f + 1.25f);
            Vector2 runoffPosition = PathPoint(t, runoffLane);
            if (!HasTracksideClearance(runoffPosition, t))
            {
                continue;
            }

            float runoffLength = Mathf.Lerp(3.7f, 5.8f, Mathf.InverseLerp(15f, 38f, severity));
            GameObject runoff = new GameObject("Corner Runoff Zone " + (runoffIndex + 1));
            runoff.transform.SetParent(transform);
            runoff.transform.position = runoffPosition;
            runoff.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(t));

            CreateVisual("Runoff Shadow", new Vector2(0.16f, -0.14f), new Vector2(1.95f, runoffLength + 0.45f), new Color(0f, 0f, 0f, 0.48f), -18, 0f, runoff.transform, true, circleSprite);
            CreateVisual("Runoff Gravel Bed", Vector2.zero, new Vector2(1.72f, runoffLength), Color.Lerp(ActiveTrack.GroundStripeA, new Color(0.22f, 0.23f, 0.22f), 0.38f), -17, 0f, runoff.transform, true, circleSprite);
            CreateVisual("Runoff Inner Edge", new Vector2(outsideSide * -0.71f, 0f), new Vector2(0.16f, runoffLength * 0.93f), ActiveTrack.CurbB, -15, 0f, runoff.transform, true);

            for (int stripe = -2; stripe <= 2; stripe++)
            {
                Color stripeColor = (stripe + runoffIndex) % 2 == 0 ? ActiveTrack.CurbA : ActiveTrack.CurbB;
                CreateVisual("Runoff Warning Stripe", new Vector2(0f, stripe * runoffLength * 0.18f), new Vector2(1.34f, 0.18f), stripeColor, -14, outsideSide * 22f, runoff.transform, true);
            }

            for (int stone = 0; stone < 8; stone++)
            {
                float stoneX = Mathf.Lerp(-0.58f, 0.58f, Mathf.Repeat(stone * 0.618f + runoffIndex * 0.17f, 1f));
                float stoneY = Mathf.Lerp(-runoffLength * 0.4f, runoffLength * 0.4f, Mathf.Repeat(stone * 0.347f + 0.23f, 1f));
                float stoneSize = Mathf.Lerp(0.06f, 0.14f, Mathf.Repeat(stone * 0.271f, 1f));
                CreateVisual("Runoff Aggregate", new Vector2(stoneX, stoneY), new Vector2(stoneSize, stoneSize), new Color(0.52f, 0.53f, 0.49f, 0.78f), -13, stone * 31f, runoff.transform, true, circleSprite);
            }

            float insideSide = -outsideSide;
            Vector2 apexPosition = PathPoint(t + step * 0.18f, insideSide * (TrackWidth * 0.5f - 0.12f));
            float apexRotation = PathRotation(t + step * 0.18f);
            CreateVisual("Illuminated Apex Pad", apexPosition, new Vector2(0.52f, 1.45f), new Color(0.015f, 0.035f, 0.04f, 0.94f), -15, apexRotation, transform, false, circleSprite);
            CreateVisual("Illuminated Apex Strip", apexPosition, new Vector2(0.18f, 1.08f), ActiveTrack.AccentColor, -13, apexRotation, transform);

            runoffIndex++;
            i += 4;
        }
    }

    private void CreateCornerBrakeMarkerBoards()
    {
        const int sampleCount = 52;
        float step = Mathf.PI * 2f / sampleCount;
        int markerGroup = 0;
        for (int i = 0; i < sampleCount && markerGroup < 9; i++)
        {
            float t = (i + 0.5f) * step;
            Vector2 currentDirection = PathDerivative(t).normalized;
            Vector2 futureDirection = PathDerivative(t + step * 0.72f).normalized;
            float signedTurn = Vector2.SignedAngle(currentDirection, futureDirection);
            if (Mathf.Abs(signedTurn) < 17f)
            {
                continue;
            }

            float side = signedTurn > 0f ? -1f : 1f;
            bool placedAnyBoard = false;
            for (int board = 0; board < 3; board++)
            {
                float boardT = t - step * (0.78f - board * 0.25f);
                Vector2 boardPosition = PathPoint(boardT, side * (TrackWidth * 0.5f + 1.92f));
                if (!HasTracksideClearance(boardPosition, boardT))
                {
                    continue;
                }

                GameObject boardRoot = new GameObject("Brake Marker " + (markerGroup + 1) + "-" + (3 - board));
                boardRoot.transform.SetParent(transform);
                boardRoot.transform.position = boardPosition;
                boardRoot.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(boardT));

                CreateVisual("Brake Marker Shadow", new Vector2(0.15f, -0.13f), new Vector2(0.84f, 1.04f), new Color(0f, 0f, 0f, 0.52f), -15, 0f, boardRoot.transform, true);
                CreateVisual("Brake Marker Frame", Vector2.zero, new Vector2(0.72f, 0.92f), new Color(0.82f, 0.85f, 0.82f), -14, 0f, boardRoot.transform, true);
                CreateVisual("Brake Marker Face", Vector2.zero, new Vector2(0.58f, 0.78f), new Color(0.055f, 0.065f, 0.068f), -13, 0f, boardRoot.transform, true);
                for (int stripe = 0; stripe <= board; stripe++)
                {
                    float y = (stripe - board * 0.5f) * 0.2f;
                    CreateVisual("Brake Distance Stripe", new Vector2(0f, y), new Vector2(0.42f, 0.09f), stripe % 2 == 0 ? ActiveTrack.CurbB : ActiveTrack.AccentColor, -12, 0f, boardRoot.transform, true);
                }

                placedAnyBoard = true;
            }

            if (placedAnyBoard)
            {
                markerGroup++;
                i += 3;
            }
        }
    }

    private bool HasTracksideClearance(Vector2 position, float sourceParameter)
    {
        const int clearanceSamples = 96;
        for (int i = 0; i < clearanceSamples; i++)
        {
            float sampleParameter = i * Mathf.PI * 2f / clearanceSamples;
            float parameterGap = Mathf.Abs(Mathf.DeltaAngle(sourceParameter * Mathf.Rad2Deg, sampleParameter * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
            if (parameterGap < 0.22f)
            {
                continue;
            }

            if (Vector2.Distance(position, PathPoint(sampleParameter, 0f)) < TrackWidth * 0.67f + 0.52f)
            {
                return false;
            }
        }

        return true;
    }

    private void CreateTracksideTireStack(Vector2 position, float rotation, int index)
    {
        GameObject tireRoot = new GameObject("Safety Tire Stack " + (index + 1));
        tireRoot.transform.SetParent(transform);
        tireRoot.transform.position = position;
        tireRoot.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

        for (int tire = -1; tire <= 1; tire++)
        {
            Vector2 tirePosition = new Vector2(0f, tire * 0.43f);
            CreateVisual("Tire Shadow", tirePosition + new Vector2(0.11f, -0.1f), new Vector2(0.72f, 0.72f), new Color(0f, 0f, 0f, 0.52f), -15, 0f, tireRoot.transform, true, circleSprite);
            CreateVisual("Safety Tire", tirePosition, new Vector2(0.62f, 0.62f), new Color(0.018f, 0.022f, 0.024f), -13, 0f, tireRoot.transform, true, circleSprite);
            CreateVisual("Tire Sidewall", tirePosition, new Vector2(0.39f, 0.39f), new Color(0.095f, 0.105f, 0.11f), -12, 0f, tireRoot.transform, true, circleSprite);
            CreateVisual("Tire Opening", tirePosition, new Vector2(0.2f, 0.2f), new Color(0.008f, 0.01f, 0.012f), -11, 0f, tireRoot.transform, true, circleSprite);
        }
    }

    private void CreateEnvironmentalTrackContamination()
    {
        Color edgeDeposit;
        Color edgeDepositHighlight;
        if (ActiveTrack.IsGlacier)
        {
            edgeDeposit = new Color(0.68f, 0.86f, 0.93f, 0.23f);
            edgeDepositHighlight = new Color(0.88f, 0.97f, 1f, 0.27f);
        }
        else if (ActiveTrack.IsVolcanic)
        {
            edgeDeposit = new Color(0.055f, 0.025f, 0.028f, 0.46f);
            edgeDepositHighlight = new Color(0.3f, 0.08f, 0.025f, 0.25f);
        }
        else if (ActiveTrack.IsLunar)
        {
            edgeDeposit = new Color(0.36f, 0.34f, 0.46f, 0.31f);
            edgeDepositHighlight = new Color(0.63f, 0.62f, 0.74f, 0.2f);
        }
        else if (ActiveTrack.IsDesert)
        {
            edgeDeposit = new Color(0.55f, 0.28f, 0.09f, 0.31f);
            edgeDepositHighlight = new Color(0.86f, 0.53f, 0.17f, 0.21f);
        }
        else
        {
            edgeDeposit = new Color(0.025f, 0.04f, 0.048f, 0.34f);
            edgeDepositHighlight = new Color(0.2f, 0.3f, 0.34f, 0.16f);
        }

        for (int i = 0; i < 44; i++)
        {
            float t = Mathf.Repeat(0.014f + i * 0.07721f, 1f) * Mathf.PI * 2f;
            float side = i % 2 == 0 ? 1f : -1f;
            float lane = side * Mathf.Lerp(TrackWidth * 0.5f - 0.38f, TrackWidth * 0.5f + 0.12f, Mathf.Repeat(i * 0.419f, 1f));
            Vector2 position = PathPoint(t, lane);
            float rotation = PathRotation(t) + Mathf.Lerp(-14f, 14f, Mathf.Repeat(i * 0.673f, 1f));
            float length = Mathf.Lerp(0.55f, 1.85f, Mathf.Repeat(i * 0.293f, 1f));
            float width = Mathf.Lerp(0.16f, 0.52f, Mathf.Repeat(i * 0.517f, 1f));

            CreateVisual("Environmental Edge Deposit", position, new Vector2(width, length), edgeDeposit, -15, rotation, transform, false, circleSprite);
            if (i % 3 == 0)
            {
                Vector2 highlightPosition = position + (Vector2)(Quaternion.Euler(0f, 0f, rotation) * new Vector3(0.08f, 0.04f, 0f));
                CreateVisual("Environmental Deposit Highlight", highlightPosition, new Vector2(width * 0.34f, length * 0.72f), edgeDepositHighlight, -14, rotation, transform, false, circleSprite);
            }
        }

        const int brakingSamples = 72;
        for (int i = 0; i < brakingSamples; i++)
        {
            float t = i * Mathf.PI * 2f / brakingSamples;
            Vector2 currentDirection = PathDerivative(t).normalized;
            Vector2 futureDirection = PathDerivative(t + 0.055f).normalized;
            float cornerSeverity = Mathf.Abs(Vector2.SignedAngle(currentDirection, futureDirection));
            if (cornerSeverity < 12f || i % 3 != 0)
            {
                continue;
            }

            float rotation = PathRotation(t);
            float markLength = Mathf.Lerp(1.6f, 3.6f, Mathf.InverseLerp(12f, 34f, cornerSeverity));
            for (int wheel = -1; wheel <= 1; wheel += 2)
            {
                Vector2 position = PathPoint(t, wheel * 0.46f);
                CreateVisual("Braking Zone Rubber", position, new Vector2(0.11f, markLength), new Color(0.004f, 0.006f, 0.008f, 0.34f), -14, rotation, transform);
                CreateVisual("Braking Zone Rubber Fade", position + (Vector2)(Quaternion.Euler(0f, 0f, rotation) * new Vector3(0.04f, -markLength * 0.14f, 0f)), new Vector2(0.19f, markLength * 0.56f), new Color(0.01f, 0.012f, 0.014f, 0.13f), -15, rotation, transform, false, circleSprite);
            }
        }

        for (int i = 0; i < 18; i++)
        {
            float t = (i + 0.42f) * Mathf.PI * 2f / 18f;
            float side = i % 2 == 0 ? 1f : -1f;
            Vector2 curbScuff = PathPoint(t, side * (TrackWidth * 0.5f + 0.08f));
            float rotation = PathRotation(t) + Mathf.Lerp(-8f, 8f, Mathf.Repeat(i * 0.37f, 1f));
            CreateVisual("Curb Tire Scuff", curbScuff, new Vector2(0.18f, Mathf.Lerp(0.7f, 1.45f, Mathf.Repeat(i * 0.61f, 1f))), new Color(0.01f, 0.012f, 0.014f, 0.36f), -14, rotation, transform, false, circleSprite);
        }
    }

    private void CreateTrackObstacles()
    {
        float[] fractions = ActiveTrack.IsDesert
            ? new[] { 0.11f, 0.23f, 0.36f, 0.49f, 0.63f, 0.76f, 0.88f }
            : new[] { 0.08f, 0.15f, 0.22f, 0.30f, 0.37f, 0.45f, 0.53f, 0.61f, 0.69f, 0.77f, 0.85f, 0.93f };
        float[] lanes = ActiveTrack.IsDesert
            ? new[] { -1.75f, 1.7f, -1.45f, 1.8f, -1.8f, 1.5f, -1.55f }
            : new[] { 1.75f, -1.7f, 0.35f, 1.8f, -1.8f, -0.35f, 1.75f, -1.75f, 0.4f, 1.8f, -1.8f, -0.3f };
        minimapObstaclePoints.Clear();
        trackObstacles.Clear();

        for (int i = 0; i < fractions.Length; i++)
        {
            float t = fractions[i] * Mathf.PI * 2f;
            minimapObstaclePoints.Add(PathPoint(t, lanes[i]));
            if (!ActiveTrack.IsDesert && (i % 4 == 1 || i % 4 == 2))
            {
                CreateConeCluster(t, lanes[i], i);
            }
            else if (i % 3 == 2)
            {
                CreateConeCluster(t, lanes[i], i);
            }
            else
            {
                CreateRoadBarrier(t, lanes[i], i);
            }
        }
    }

    private void CreateRoadBarrier(float t, float lane, int index)
    {
        GameObject root = new GameObject("Road Barrier " + (index + 1));
        root.transform.SetParent(transform);
        root.transform.position = PathPoint(t, lane);
        root.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(t));

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(2.55f, 0.62f);
        collider.edgeRadius = 0.08f;

        Color mainColor = ActiveTrack.IsDesert ? new Color(0.96f, 0.31f, 0.045f) : ActiveTrack.CurbB;
        Color stripeColor = ActiveTrack.IsDesert ? new Color(1f, 0.91f, 0.62f) : ActiveTrack.CurbA;
        Color bodyColor = ActiveTrack.IsDesert ? new Color(0.16f, 0.09f, 0.055f) : new Color(0.025f, 0.055f, 0.085f);
        Color edgeColor = ActiveTrack.IsDesert ? new Color(0.055f, 0.035f, 0.025f) : new Color(0.008f, 0.018f, 0.03f);
        Color glowColor = new Color(stripeColor.r, stripeColor.g, stripeColor.b, ActiveTrack.IsDesert ? 0.13f : 0.22f);
        TrackObstacle obstacle = root.AddComponent<TrackObstacle>();
        obstacle.Initialize(this, 1f, 0.62f, mainColor, 15f);
        trackObstacles.Add(obstacle);

        CreateVisual("Barrier Shadow", new Vector2(0.16f, -0.16f), new Vector2(3.08f, 0.96f), new Color(0f, 0f, 0f, 0.58f), -15, 0f, root.transform, true);
        CreateVisual("Barrier Underglow", new Vector2(0f, -0.04f), new Vector2(2.96f, 0.84f), glowColor, -14, 0f, root.transform, true, circleSprite);
        CreateVisual("Left Barrier Foot", new Vector2(-1.02f, -0.42f), new Vector2(0.48f, 0.3f), edgeColor, -13, 0f, root.transform, true);
        CreateVisual("Right Barrier Foot", new Vector2(1.02f, -0.42f), new Vector2(0.48f, 0.3f), edgeColor, -13, 0f, root.transform, true);
        CreateVisual("Barrier Outer Frame", Vector2.zero, new Vector2(2.82f, 0.72f), edgeColor, -12, 0f, root.transform, true);
        CreateVisual("Barrier Body", new Vector2(0f, 0.015f), new Vector2(2.64f, 0.55f), bodyColor, -11, 0f, root.transform, true);
        CreateVisual("Barrier Top Edge", new Vector2(0f, 0.26f), new Vector2(2.55f, 0.07f), Color.Lerp(bodyColor, Color.white, 0.28f), -10, 0f, root.transform, true);
        CreateVisual("Barrier Bottom Edge", new Vector2(0f, -0.25f), new Vector2(2.55f, 0.07f), Color.Lerp(edgeColor, mainColor, 0.2f), -10, 0f, root.transform, true);

        for (int stripe = 0; stripe < 7; stripe++)
        {
            float x = -1.08f + stripe * 0.36f;
            Color color = stripe % 2 == 0 ? mainColor : stripeColor;
            CreateVisual("Barrier Reflective Stripe", new Vector2(x, 0.015f), new Vector2(0.29f, 0.47f), color, -9, stripe % 2 == 0 ? 22f : -22f, root.transform, true);
        }

        for (int bolt = 0; bolt < 4; bolt++)
        {
            float x = -0.96f + bolt * 0.64f;
            CreateVisual("Barrier Bolt", new Vector2(x, -0.18f), new Vector2(0.09f, 0.09f), new Color(0.72f, 0.78f, 0.78f), -8, 45f, root.transform, true);
        }

        Vector2 leftBeacon = new Vector2(-1.12f, 0.43f);
        Vector2 rightBeacon = new Vector2(1.12f, 0.43f);
        SpriteRenderer leftGlow = CreateVisual("Left Beacon Glow", leftBeacon, new Vector2(0.56f, 0.56f), new Color(stripeColor.r, stripeColor.g, stripeColor.b, 0.24f), -8, 0f, root.transform, true, circleSprite);
        SpriteRenderer rightGlow = CreateVisual("Right Beacon Glow", rightBeacon, new Vector2(0.56f, 0.56f), new Color(stripeColor.r, stripeColor.g, stripeColor.b, 0.24f), -8, 0f, root.transform, true, circleSprite);
        CreateVisual("Left Beacon Housing", leftBeacon, new Vector2(0.32f, 0.32f), edgeColor, -7, 45f, root.transform, true);
        CreateVisual("Right Beacon Housing", rightBeacon, new Vector2(0.32f, 0.32f), edgeColor, -7, 45f, root.transform, true);
        SpriteRenderer leftCore = CreateVisual("Left Beacon Core", leftBeacon, new Vector2(0.17f, 0.17f), stripeColor, -6, 45f, root.transform, true);
        SpriteRenderer rightCore = CreateVisual("Right Beacon Core", rightBeacon, new Vector2(0.17f, 0.17f), stripeColor, -6, 45f, root.transform, true);
        root.AddComponent<ObstacleBeacon>().Initialize(index * 0.83f, new[] { leftGlow, rightGlow, leftCore, rightCore });
    }

    private void CreateConeCluster(float t, float lane, int index)
    {
        GameObject root = new GameObject("Cone Cluster " + (index + 1));
        root.transform.SetParent(transform);
        root.transform.position = PathPoint(t, lane);
        root.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(t));

        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.95f, 0.78f);
        collider.edgeRadius = 0.12f;

        Color coneColor = ActiveTrack.IsDesert ? new Color(1f, 0.4f, 0.035f) : new Color(1f, 0.18f, 0.045f);
        Color coneDark = Color.Lerp(coneColor, new Color(0.08f, 0.025f, 0.015f), 0.38f);
        Color bandColor = ActiveTrack.IsDesert ? new Color(1f, 0.92f, 0.68f) : ActiveTrack.CurbA;
        Color baseColor = ActiveTrack.IsDesert ? new Color(0.12f, 0.07f, 0.04f) : new Color(0.018f, 0.035f, 0.055f);
        TrackObstacle obstacle = root.AddComponent<TrackObstacle>();
        obstacle.Initialize(this, 0.72f, 1.45f, coneColor, 10f);
        trackObstacles.Add(obstacle);
        SpriteRenderer[] reflectors = new SpriteRenderer[3];

        CreateVisual("Cone Cluster Shadow", new Vector2(0.15f, -0.14f), new Vector2(2.35f, 1.04f), new Color(0f, 0f, 0f, 0.5f), -15, 0f, root.transform, true);
        CreateVisual("Cone Cluster Glow", Vector2.zero, new Vector2(2.18f, 0.82f), new Color(bandColor.r, bandColor.g, bandColor.b, ActiveTrack.IsDesert ? 0.06f : 0.12f), -14, 0f, root.transform, true, circleSprite);

        for (int cone = 0; cone < 3; cone++)
        {
            float x = -0.7f + cone * 0.7f;
            float y = cone == 1 ? 0.1f : -0.07f;
            float size = cone == 1 ? 1.08f : 0.92f;
            Vector2 position = new Vector2(x, y);

            CreateVisual("Cone Shadow", position + new Vector2(0.09f, -0.1f), new Vector2(0.72f, 0.64f) * size, new Color(0f, 0f, 0f, 0.48f), -13, 0f, root.transform, true, circleSprite);
            CreateVisual("Cone Base Outer", position, new Vector2(0.66f, 0.66f) * size, baseColor, -12, 45f, root.transform, true);
            CreateVisual("Cone Base Rim", position, new Vector2(0.54f, 0.54f) * size, Color.Lerp(baseColor, bandColor, 0.24f), -11, 45f, root.transform, true);
            CreateVisual("Cone Lower Body", position, new Vector2(0.46f, 0.46f) * size, coneDark, -10, 0f, root.transform, true, circleSprite);
            reflectors[cone] = CreateVisual("Cone Reflector Glow", position, new Vector2(0.45f, 0.45f) * size, new Color(bandColor.r, bandColor.g, bandColor.b, 0.22f), -9, 0f, root.transform, true, circleSprite);
            CreateVisual("Cone Reflective Ring", position, new Vector2(0.35f, 0.35f) * size, bandColor, -8, 0f, root.transform, true, circleSprite);
            CreateVisual("Cone Upper Body", position, new Vector2(0.24f, 0.24f) * size, coneColor, -7, 0f, root.transform, true, circleSprite);
            CreateVisual("Cone Highlight", position + new Vector2(-0.055f, 0.06f), new Vector2(0.075f, 0.075f) * size, new Color(1f, 0.76f, 0.42f, 0.9f), -6, 0f, root.transform, true, circleSprite);
            CreateVisual("Cone Cap", position, new Vector2(0.075f, 0.075f) * size, bandColor, -5, 0f, root.transform, true, circleSprite);
        }

        root.AddComponent<ObstacleBeacon>().Initialize(index * 0.71f, reflectors);
    }

    private void CreateWeaponPickups()
    {
        float[] fractions = { 0.06f, 0.18f, 0.30f, 0.42f, 0.54f, 0.70f, 0.82f, 0.94f };
        float[] lanes = { 1.15f, -1.2f, 0.8f, -1.1f, 1.25f, -0.85f, 1.1f, -1.15f };
        Color pickupColor = ActiveTrack.IsDesert
            ? new Color(0.2f, 1f, 0.48f)
            : new Color(0.16f, 1f, 0.62f);

        Physics2D.SyncTransforms();
        weaponPickups.Clear();
        for (int i = 0; i < fractions.Length; i++)
        {
            float t = fractions[i] * Mathf.PI * 2f;
            float lane = lanes[i];
            if (!TryFindSafePickupPlacement(ref t, ref lane))
            {
                continue;
            }
            GameObject root = new GameObject("Neon Rocket Pickup " + (i + 1));
            root.transform.SetParent(transform);
            root.transform.position = PathPoint(t, lane);
            root.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(t));

            CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
            trigger.radius = 0.68f;
            trigger.isTrigger = true;

            SpriteRenderer glow = CreateVisual("Pickup Glow", Vector2.zero, new Vector2(1.65f, 1.65f), new Color(pickupColor.r, pickupColor.g, pickupColor.b, 0.16f), -4, 0f, root.transform, true, circleSprite);
            SpriteRenderer frame = CreateVisual("Pickup Frame", Vector2.zero, new Vector2(0.92f, 0.92f), new Color(0.008f, 0.04f, 0.045f), -3, 45f, root.transform, true);
            SpriteRenderer rim = CreateVisual("Pickup Neon Rim", Vector2.zero, new Vector2(0.72f, 0.72f), pickupColor, -2, 45f, root.transform, true);
            SpriteRenderer center = CreateVisual("Pickup Center", Vector2.zero, new Vector2(0.52f, 0.52f), new Color(0.015f, 0.075f, 0.085f), -1, 45f, root.transform, true);
            SpriteRenderer rocketBody = CreateVisual("Pickup Rocket Body", new Vector2(0f, 0.02f), new Vector2(0.13f, 0.48f), new Color(1f, 0.48f, 0.08f), 0, 0f, root.transform, true);
            SpriteRenderer rocketWing = CreateVisual("Pickup Rocket Wing", new Vector2(0f, -0.06f), new Vector2(0.38f, 0.09f), new Color(1f, 0.86f, 0.26f), 0, 0f, root.transform, true);
            SpriteRenderer rocketTip = CreateVisual("Pickup Rocket Tip", new Vector2(0f, 0.25f), new Vector2(0.2f, 0.2f), Color.white, 1, 0f, root.transform, true, circleSprite);

            WeaponPickup pickup = root.AddComponent<WeaponPickup>();
            pickup.Initialize(3, 8.5f, t, lane, new[] { glow, frame, rim, center, rocketBody, rocketWing, rocketTip }, i * 0.73f);
            weaponPickups.Add(pickup);
        }
    }

    private bool TryFindSafePickupPlacement(ref float t, ref float lane)
    {
        float originalT = t;
        float originalLane = lane;
        float laneLimit = TrackWidth * 0.5f - PickupObstacleClearance;

        for (int trackIndex = 0; trackIndex < PickupTrackSearchOffsets.Length; trackIndex++)
        {
            float candidateT = Mathf.Repeat(originalT + PickupTrackSearchOffsets[trackIndex] * Mathf.PI * 2f, Mathf.PI * 2f);
            for (int laneIndex = 0; laneIndex < PickupLaneSearchOffsets.Length; laneIndex++)
            {
                float candidateLane = Mathf.Clamp(originalLane + PickupLaneSearchOffsets[laneIndex], -laneLimit, laneLimit);
                Vector2 candidatePosition = PathPoint(candidateT, candidateLane);
                Collider2D[] overlaps = Physics2D.OverlapCircleAll(candidatePosition, PickupObstacleClearance, PickupPlacementLayerMask);
                bool blocked = false;

                for (int hitIndex = 0; hitIndex < overlaps.Length; hitIndex++)
                {
                    Collider2D hit = overlaps[hitIndex];
                    if (hit != null && hit.GetComponentInParent<TrackObstacle>() != null)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    t = candidateT;
                    lane = candidateLane;
                    return true;
                }
            }
        }

        return false;
    }

private void CreateStartLine()
    {
        Vector2 center = PathPoint(0f, 0f);
        Vector2 tangent = PathDerivative(0f).normalized;
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        float rotation = PathRotation(0f);

        for (int i = 0; i < 12; i++)
        {
            float lane = -TrackWidth * 0.5f + (i + 0.5f) * TrackWidth / 12f;
            Color color = i % 2 == 0 ? Color.white : new Color(0.05f, 0.06f, 0.07f);
            CreateVisual("Start Grid", center + normal * lane, new Vector2(TrackWidth / 12f, 0.62f), color, -12, rotation, transform);
        }

        CreateVisual("Start Banner Left", center - normal * TrackWidth * 0.68f, new Vector2(0.24f, 1.7f), new Color(0.05f, 0.75f, 0.83f), -8, rotation + 90f, transform);
        CreateVisual("Start Banner Right", center + normal * TrackWidth * 0.68f, new Vector2(0.24f, 1.7f), new Color(0.05f, 0.75f, 0.83f), -8, rotation + 90f, transform);

        GameObject gantry = new GameObject("Start Finish Light Gantry");
        gantry.transform.SetParent(transform);
        gantry.transform.position = center + tangent * 1.45f;
        gantry.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

        CreateVisual("Gantry Shadow", new Vector2(0.14f, -0.12f), new Vector2(TrackWidth + 2.5f, 0.9f), new Color(0f, 0f, 0f, 0.62f), -11, 0f, gantry.transform, true);
        CreateVisual("Gantry Deck", Vector2.zero, new Vector2(TrackWidth + 2.35f, 0.72f), new Color(0.025f, 0.04f, 0.05f), -10, 0f, gantry.transform, true);
        CreateVisual("Gantry Accent Rail", new Vector2(0f, -0.28f), new Vector2(TrackWidth + 1.95f, 0.08f), ActiveTrack.AccentColor, -9, 0f, gantry.transform, true);

        for (int sideIndex = -1; sideIndex <= 1; sideIndex += 2)
        {
            float supportX = sideIndex * (TrackWidth * 0.5f + 0.82f);
            CreateVisual("Gantry Support Shadow", new Vector2(supportX + 0.1f, 0.17f), new Vector2(0.72f, 1.55f), new Color(0f, 0f, 0f, 0.56f), -11, 0f, gantry.transform, true);
            CreateVisual("Gantry Support", new Vector2(supportX, 0.12f), new Vector2(0.48f, 1.48f), new Color(0.07f, 0.09f, 0.1f), -9, 0f, gantry.transform, true);
            CreateVisual("Gantry Support Marker", new Vector2(supportX, -0.18f), new Vector2(0.5f, 0.16f), sideIndex < 0 ? ActiveTrack.CurbA : ActiveTrack.CurbB, -8, 0f, gantry.transform, true);
        }

        for (int i = 0; i < startLightRenderers.Length; i++)
        {
            Vector2 lightPosition = new Vector2((i - 2) * 0.58f, 0.02f);
            CreateVisual("Start Light Housing", lightPosition, new Vector2(0.46f, 0.46f), new Color(0.005f, 0.008f, 0.01f), -8, 45f, gantry.transform, true, circleSprite);
            startLightRenderers[i] = CreateVisual("Start Sequence Light " + (i + 1), lightPosition, new Vector2(0.27f, 0.27f), new Color(0.12f, 0.025f, 0.025f), -7, 45f, gantry.transform, true, circleSprite);
        }
    }

    private void UpdateStartGantryLights()
    {
        if (startLightRenderers[0] == null)
        {
            return;
        }

        bool showGreen = raceStarted && !raceFinished;
        int redLightCount = Mathf.Clamp(Mathf.FloorToInt((3.8f - countdown) / 0.62f) + 1, 0, startLightRenderers.Length);
        float pulse = 0.86f + Mathf.Sin(Time.unscaledTime * 7f) * 0.14f;

        for (int i = 0; i < startLightRenderers.Length; i++)
        {
            if (showGreen)
            {
                startLightRenderers[i].color = new Color(0.18f, 1f, 0.32f, pulse);
            }
            else if (!raceStarted && i < redLightCount)
            {
                startLightRenderers[i].color = new Color(1f, 0.08f, 0.035f, pulse);
            }
            else if (raceFinished)
            {
                startLightRenderers[i].color = new Color(ActiveTrack.AccentColor.r, ActiveTrack.AccentColor.g, ActiveTrack.AccentColor.b, pulse);
            }
            else
            {
                startLightRenderers[i].color = new Color(0.1f, 0.018f, 0.018f, 0.78f);
            }
        }
    }

    private void CreateInfieldDecor()
    {
        if (ActiveTrack.IsLunar)
        {
            CreateLunarInfieldDecor();
            return;
        }

        if (ActiveTrack.IsGlacier)
        {
            CreateGlacierInfieldDecor();
            return;
        }

        if (ActiveTrack.IsVolcanic)
        {
            CreateVolcanoInfieldDecor();
            return;
        }

        if (ActiveTrack.IsDesert)
        {
            CreateDesertInfieldDecor();
            return;
        }

        CreateVisual("Paddock Shadow", new Vector2(1.8f, -1.95f), new Vector2(14.8f, 5.9f), new Color(0.005f, 0.025f, 0.028f, 0.62f), -27, -8f, transform);
        CreateVisual("Paddock", new Vector2(1.5f, -1.7f), new Vector2(14.5f, 5.6f), new Color(0.055f, 0.085f, 0.095f), -26, -8f, transform);
        CreateVisual("Pit Building Shadow", new Vector2(1.72f, -2.02f), new Vector2(6.9f, 2.45f), new Color(0.005f, 0.015f, 0.02f, 0.65f), -25, -8f, transform);
        CreateVisual("Pit Building", new Vector2(1.5f, -1.8f), new Vector2(6.5f, 2.2f), new Color(0.075f, 0.12f, 0.145f), -24, -8f, transform);
        CreateVisual("Pit Roof Glow", new Vector2(1.5f, -1.35f), new Vector2(7.15f, 0.58f), new Color(0.05f, 0.72f, 0.82f, 0.18f), -23, -8f, transform);
        CreateVisual("Pit Roof", new Vector2(1.5f, -1.35f), new Vector2(6.8f, 0.28f), new Color(0.12f, 0.88f, 0.94f), -22, -8f, transform);

        for (int i = 0; i < 9; i++)
        {
            Color light = i % 3 == 0 ? new Color(1f, 0.31f, 0.12f) : new Color(0.92f, 0.82f, 0.36f);
            Vector2 lightPosition = new Vector2(-1.4f + i * 0.72f, -1.7f);
            CreateVisual("Pit Light Glow", lightPosition, new Vector2(0.52f, 0.42f), new Color(light.r, light.g, light.b, 0.16f), -21, -8f, transform);
            CreateVisual("Pit Light", lightPosition, new Vector2(0.24f, 0.18f), light, -20, -8f, transform);
        }

        CreateVisual("Grandstand Shadow", new Vector2(0f, 2.27f), new Vector2(17.5f, 1.55f), new Color(0.005f, 0.02f, 0.025f, 0.55f), -26, 0f, transform);
        CreateVisual("Grandstand Base", new Vector2(0f, 2.12f), new Vector2(17.2f, 1.4f), new Color(0.045f, 0.065f, 0.075f), -25, 0f, transform);

        for (int i = 0; i < 22; i++)
        {
            float x = -8f + (i % 11) * 1.55f;
            float y = 2.0f + (i / 11) * 0.55f;
            Color seat = i % 2 == 0 ? new Color(0.92f, 0.19f, 0.12f) : new Color(0.08f, 0.65f, 0.76f);
            CreateVisual("Grandstand Seat", new Vector2(x, y), new Vector2(1.2f, 0.32f), seat, -24, 0f, transform);
        }

        CreateVisual("Infield Logo Glow", new Vector2(-0.2f, -8.2f), new Vector2(4.6f, 4.6f), new Color(0.06f, 0.72f, 0.82f, 0.1f), -27, 45f, transform);
        CreateVisual("Infield Logo Outer", new Vector2(-0.2f, -8.2f), new Vector2(3.2f, 3.2f), new Color(0.03f, 0.13f, 0.15f), -26, 45f, transform);
        CreateVisual("Infield Logo Inner", new Vector2(-0.2f, -8.2f), new Vector2(1.8f, 1.8f), new Color(1f, 0.31f, 0.08f), -25, 45f, transform);
    }

    private void CreateDesertInfieldDecor()
    {
        CreateVisual("Canyon Camp Shadow", new Vector2(-8.4f, -3.2f), new Vector2(13.5f, 6.8f), new Color(0.08f, 0.025f, 0.01f, 0.62f), -27, -6f, transform);
        CreateVisual("Canyon Camp", new Vector2(-8f, -2.8f), new Vector2(13f, 6.3f), new Color(0.48f, 0.23f, 0.08f), -26, -6f, transform);
        CreateVisual("Service Tent", new Vector2(-10f, -2.1f), new Vector2(5.2f, 2.4f), new Color(0.78f, 0.39f, 0.09f), -24, -6f, transform);
        CreateVisual("Service Tent Roof", new Vector2(-10f, -1.55f), new Vector2(5.7f, 0.42f), ActiveTrack.AccentColor, -23, -6f, transform);
        CreateVisual("Water Tank", new Vector2(-3.4f, -3.8f), new Vector2(2.1f, 2.1f), new Color(0.19f, 0.34f, 0.34f), -24, 45f, transform);

        Vector2[] mesas =
        {
            new Vector2(-14f, 10f),
            new Vector2(-3f, 12f),
            new Vector2(-15f, -13f),
            new Vector2(1f, -15f)
        };

        for (int i = 0; i < mesas.Length; i++)
        {
            float size = 2.8f + (i % 2) * 1.2f;
            CreateVisual("Mesa Shadow", mesas[i] + new Vector2(0.35f, -0.35f), new Vector2(size + 0.8f, size + 0.8f), new Color(0.08f, 0.02f, 0.01f, 0.58f), -27, 45f, transform);
            CreateVisual("Mesa Base", mesas[i], new Vector2(size, size), new Color(0.42f, 0.16f, 0.045f), -26, 45f, transform);
            CreateVisual("Mesa Top", mesas[i] + new Vector2(-0.18f, 0.18f), new Vector2(size * 0.62f, size * 0.62f), new Color(0.72f, 0.31f, 0.08f), -25, 45f, transform);
        }
    }

    private void CreateRepairPickups()
    {
        float[] fractions = { 0.12f, 0.24f, 0.36f, 0.48f, 0.60f, 0.76f, 0.88f };
        float[] lanes = { -1.35f, 1.2f, -0.85f, 1.35f, -1.15f, 0.9f, -1.3f };
        Color repairGreen = new Color(0.2f, 1f, 0.34f);
        Color repairCyan = new Color(0.08f, 0.94f, 1f);

        repairPickups.Clear();
        for (int i = 0; i < fractions.Length; i++)
        {
            float t = fractions[i] * Mathf.PI * 2f;
            GameObject root = new GameObject("Repair Pickup " + (i + 1));
            root.transform.SetParent(transform);
            root.transform.position = PathPoint(t, lanes[i]);
            root.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(t));

            CircleCollider2D trigger = root.AddComponent<CircleCollider2D>();
            trigger.radius = 0.72f;
            trigger.isTrigger = true;

            SpriteRenderer glow = CreateVisual("Repair Glow", Vector2.zero, new Vector2(1.72f, 1.72f), new Color(repairGreen.r, repairGreen.g, repairGreen.b, 0.18f), -4, 0f, root.transform, true, circleSprite);
            SpriteRenderer shadow = CreateVisual("Repair Shadow", new Vector2(0.09f, -0.09f), new Vector2(1.02f, 1.02f), new Color(0f, 0f, 0f, 0.58f), -3, 45f, root.transform, true);
            SpriteRenderer outerFrame = CreateVisual("Repair Outer Frame", Vector2.zero, new Vector2(0.96f, 0.96f), repairCyan, -2, 45f, root.transform, true);
            SpriteRenderer innerFrame = CreateVisual("Repair Inner Frame", Vector2.zero, new Vector2(0.76f, 0.76f), new Color(0.012f, 0.06f, 0.055f), -1, 45f, root.transform, true);
            SpriteRenderer kit = CreateVisual("Repair Kit", Vector2.zero, new Vector2(0.62f, 0.62f), new Color(0.9f, 1f, 0.92f), 0, 0f, root.transform, true);
            SpriteRenderer crossVertical = CreateVisual("Repair Cross Vertical", Vector2.zero, new Vector2(0.16f, 0.48f), repairGreen, 1, 0f, root.transform, true);
            SpriteRenderer crossHorizontal = CreateVisual("Repair Cross Horizontal", Vector2.zero, new Vector2(0.48f, 0.16f), repairGreen, 1, 0f, root.transform, true);

            RepairPickup pickup = root.AddComponent<RepairPickup>();
            pickup.Initialize(global::CarDamage.MaxHealth, 10f, t, lanes[i], new[] { glow, shadow, outerFrame, innerFrame, kit, crossVertical, crossHorizontal }, i * 0.81f);
            repairPickups.Add(pickup);
        }
    }

    private void CreateGlacierInfieldDecor()
    {
        Color ice = new Color(0.48f, 0.9f, 1f);
        Color deepIce = new Color(0.06f, 0.24f, 0.42f);
        CreateVisual("Frozen Lake Glow", new Vector2(2f, -1f), new Vector2(13.5f, 13.5f), new Color(0.2f, 0.8f, 1f, 0.11f), -28, 0f, transform, false, circleSprite);
        CreateVisual("Frozen Lake", new Vector2(2f, -1f), new Vector2(11.8f, 11.8f), new Color(0.12f, 0.4f, 0.62f, 0.72f), -27, 0f, transform, false, circleSprite);
        CreateVisual("Ice Fault A", new Vector2(0.4f, -0.4f), new Vector2(8.8f, 0.18f), ice, -26, 24f, transform);
        CreateVisual("Ice Fault B", new Vector2(3.6f, -2.2f), new Vector2(6.4f, 0.13f), new Color(0.75f, 0.98f, 1f), -26, -38f, transform);

        CreateVisual("Polar Station Shadow", new Vector2(-10.2f, 5.5f), new Vector2(9.8f, 5.2f), new Color(0.005f, 0.035f, 0.08f, 0.7f), -27, -8f, transform);
        CreateVisual("Polar Station", new Vector2(-10.5f, 5.8f), new Vector2(9.2f, 4.6f), new Color(0.12f, 0.25f, 0.38f), -25, -8f, transform);
        CreateVisual("Polar Station Roof", new Vector2(-10.5f, 6.65f), new Vector2(9.7f, 0.42f), ice, -24, -8f, transform);
        for (int i = 0; i < 6; i++)
        {
            Vector2 window = new Vector2(-13.3f + i * 1.15f, 5.65f);
            CreateVisual("Polar Window Glow", window, new Vector2(0.72f, 0.5f), new Color(0.32f, 0.9f, 1f, 0.18f), -24, -8f, transform);
            CreateVisual("Polar Window", window, new Vector2(0.42f, 0.24f), i % 2 == 0 ? ice : new Color(0.94f, 0.82f, 0.34f), -23, -8f, transform);
        }

        Vector2[] crystals = { new Vector2(12f, 7f), new Vector2(15f, 3f), new Vector2(-3f, -10f), new Vector2(-8f, -8f) };
        for (int i = 0; i < crystals.Length; i++)
        {
            float size = 1.5f + (i % 2) * 0.55f;
            CreateVisual("Ice Crystal Glow", crystals[i], new Vector2(size + 1f, size + 1f), new Color(0.25f, 0.9f, 1f, 0.12f), -27, 45f, transform, false, circleSprite);
            CreateVisual("Ice Crystal", crystals[i], new Vector2(size * 0.38f, size * 1.55f), ice, -25, 28f + i * 17f, transform);
            CreateVisual("Ice Crystal Side", crystals[i] + new Vector2(0.65f, -0.15f), new Vector2(size * 0.26f, size), deepIce, -25, -18f + i * 13f, transform);
        }
    }

    private void CreateVolcanoInfieldDecor()
    {
        Color lava = new Color(1f, 0.2f, 0.015f);
        Color hotLava = new Color(1f, 0.78f, 0.06f);
        Color obsidian = new Color(0.075f, 0.035f, 0.055f);
        CreateVisual("Magma Crater Glow", new Vector2(0f, 0f), new Vector2(12.5f, 12.5f), new Color(1f, 0.08f, 0.01f, 0.16f), -28, 0f, transform, false, circleSprite);
        CreateVisual("Magma Crater Rim", new Vector2(0f, 0f), new Vector2(10.6f, 10.6f), obsidian, -27, 0f, transform, false, circleSprite);
        CreateVisual("Magma Pool", new Vector2(0f, 0f), new Vector2(7.8f, 7.8f), lava, -26, 0f, transform, false, circleSprite);
        CreateVisual("Magma Core", new Vector2(-0.7f, 0.7f), new Vector2(4.9f, 4.9f), hotLava, -25, 0f, transform, false, circleSprite);
        CreateVisual("Lava Split A", new Vector2(6.2f, -3.8f), new Vector2(8.8f, 0.34f), lava, -26, -28f, transform);
        CreateVisual("Lava Split B", new Vector2(-7.1f, 4.8f), new Vector2(7.2f, 0.28f), hotLava, -26, 21f, transform);

        CreateVisual("Magma Relay Shadow", new Vector2(-12.2f, -7.2f), new Vector2(9.8f, 4.8f), new Color(0.02f, 0f, 0.01f, 0.72f), -27, 7f, transform);
        CreateVisual("Magma Relay", new Vector2(-12.5f, -6.8f), new Vector2(9.2f, 4.2f), new Color(0.16f, 0.07f, 0.075f), -25, 7f, transform);
        CreateVisual("Magma Relay Rail", new Vector2(-12.5f, -6.05f), new Vector2(9.5f, 0.38f), hotLava, -24, 7f, transform);
        for (int i = 0; i < 5; i++)
        {
            Vector2 vent = new Vector2(-15.2f + i * 1.35f, -6.85f);
            CreateVisual("Heat Vent Glow", vent, new Vector2(0.7f, 0.7f), new Color(1f, 0.22f, 0.02f, 0.18f), -24, 45f, transform, false, circleSprite);
            CreateVisual("Heat Vent", vent, new Vector2(0.28f, 0.28f), i % 2 == 0 ? hotLava : lava, -23, 45f, transform);
        }
    }

    private void CreateLunarInfieldDecor()
    {
        Color moonDust = new Color(0.18f, 0.18f, 0.28f);
        Color craterShadow = new Color(0.025f, 0.02f, 0.07f, 0.82f);
        Color station = new Color(0.3f, 0.34f, 0.46f);
        Color stationLight = new Color(0.74f, 0.58f, 1f);

        CreateVisual("Lunar Crater Shadow", new Vector2(2.4f, -1.6f), new Vector2(14.2f, 14.2f), craterShadow, -28, 0f, transform, false, circleSprite);
        CreateVisual("Lunar Crater Rim", new Vector2(2f, -1.2f), new Vector2(12.2f, 12.2f), moonDust, -27, 0f, transform, false, circleSprite);
        CreateVisual("Lunar Crater Floor", new Vector2(2f, -1.2f), new Vector2(8.3f, 8.3f), new Color(0.07f, 0.065f, 0.13f), -26, 0f, transform, false, circleSprite);
        CreateVisual("Lunar Crater Highlight", new Vector2(-0.2f, 1f), new Vector2(4.8f, 1.1f), new Color(0.55f, 0.58f, 0.76f, 0.46f), -25, 18f, transform);

        CreateVisual("Moon Base Shadow", new Vector2(-12.1f, 6.2f), new Vector2(10.8f, 5.8f), new Color(0.015f, 0.01f, 0.05f, 0.72f), -27, -7f, transform);
        CreateVisual("Moon Base", new Vector2(-12.5f, 6.6f), new Vector2(10.1f, 5.1f), station, -25, -7f, transform);
        CreateVisual("Moon Base Roof", new Vector2(-12.5f, 7.45f), new Vector2(10.6f, 0.42f), stationLight, -24, -7f, transform);

        for (int i = 0; i < 6; i++)
        {
            Vector2 window = new Vector2(-15.5f + i * 1.2f, 6.45f);
            Color light = i % 2 == 0 ? stationLight : new Color(0.58f, 0.86f, 1f);
            CreateVisual("Moon Base Window Glow", window, new Vector2(0.68f, 0.5f), new Color(light.r, light.g, light.b, 0.18f), -24, -7f, transform, false, circleSprite);
            CreateVisual("Moon Base Window", window, new Vector2(0.38f, 0.2f), light, -23, -7f, transform);
        }

        Vector2[] panelCenters = { new Vector2(-18.3f, 2.1f), new Vector2(-6.6f, 3.6f) };
        for (int panel = 0; panel < panelCenters.Length; panel++)
        {
            float rotation = panel == 0 ? -12f : 8f;
            CreateVisual("Solar Panel Frame", panelCenters[panel], new Vector2(5.3f, 2.2f), new Color(0.08f, 0.07f, 0.15f), -25, rotation, transform);
            for (int cell = 0; cell < 4; cell++)
            {
                Vector2 offset = new Vector2(-1.75f + cell * 1.16f, 0f);
                CreateVisual("Solar Cell", panelCenters[panel] + offset, new Vector2(0.92f, 1.58f), cell % 2 == 0 ? new Color(0.2f, 0.34f, 0.66f) : new Color(0.32f, 0.2f, 0.62f), -24, rotation, transform);
            }
        }

        Vector2[] smallCraters = { new Vector2(-5f, -13f), new Vector2(14f, 9f), new Vector2(18f, -10f), new Vector2(-14f, 16f) };
        for (int i = 0; i < smallCraters.Length; i++)
        {
            float size = 1.9f + (i % 2) * 0.75f;
            CreateVisual("Small Lunar Crater", smallCraters[i], new Vector2(size, size), craterShadow, -27, 0f, transform, false, circleSprite);
            CreateVisual("Small Lunar Crater Rim", smallCraters[i] + new Vector2(-0.16f, 0.16f), new Vector2(size * 0.68f, size * 0.68f), moonDust, -26, 0f, transform, false, circleSprite);
        }
    }

    private void CreateTree(Vector2 position, int index)
    {
        if (ActiveTrack.IsLunar)
        {
            float rockSize = 1.05f + (index % 4) * 0.24f;
            float rockRotation = 9f + index * 29f;
            Color rock = index % 2 == 0 ? new Color(0.24f, 0.24f, 0.35f) : new Color(0.14f, 0.13f, 0.24f);
            CreateVisual("Lunar Rock Shadow", position + new Vector2(0.25f, -0.25f), new Vector2(rockSize + 0.5f, rockSize + 0.5f), new Color(0.015f, 0.01f, 0.05f, 0.68f), -28, rockRotation, transform);
            CreateVisual("Lunar Rock", position, new Vector2(rockSize, rockSize * 0.82f), rock, -27, rockRotation, transform);
            CreateVisual("Lunar Rock Highlight", position + new Vector2(-0.17f, 0.15f), new Vector2(rockSize * 0.44f, rockSize * 0.28f), new Color(0.56f, 0.56f, 0.72f), -26, rockRotation, transform);

            if (index % 5 == 0)
            {
                Vector2 beacon = position + new Vector2(0f, rockSize * 0.9f);
                CreateVisual("Lunar Beacon Glow", beacon, new Vector2(1.1f, 1.1f), new Color(0.72f, 0.48f, 1f, 0.14f), -26, 45f, transform, false, circleSprite);
                CreateVisual("Lunar Beacon", beacon, new Vector2(0.22f, 0.8f), ActiveTrack.AccentColor, -25, 0f, transform);
            }

            return;
        }

        if (ActiveTrack.IsGlacier)
        {
            float iceSize = 1.15f + (index % 4) * 0.22f;
            float iceRotation = 16f + index * 31f;
            Color ice = index % 2 == 0 ? new Color(0.52f, 0.94f, 1f) : new Color(0.18f, 0.64f, 1f);
            CreateVisual("Glacier Crystal Glow", position, new Vector2(iceSize + 1.1f, iceSize + 1.1f), new Color(ice.r, ice.g, ice.b, 0.12f), -29, 45f, transform, false, circleSprite);
            CreateVisual("Glacier Crystal", position, new Vector2(iceSize * 0.42f, iceSize * 1.5f), ice, -27, iceRotation, transform);
            CreateVisual("Glacier Crystal Wing", position + new Vector2(0.45f, -0.18f), new Vector2(iceSize * 0.28f, iceSize), new Color(0.75f, 0.98f, 1f), -26, -iceRotation * 0.55f, transform);
            return;
        }

        if (ActiveTrack.IsVolcanic)
        {
            float rockSize = 1.3f + (index % 4) * 0.25f;
            float rockRotation = 12f + index * 27f;
            CreateVisual("Volcanic Rock Glow", position, new Vector2(rockSize + 0.9f, rockSize + 0.9f), new Color(1f, 0.12f, 0.01f, 0.1f), -29, 45f, transform, false, circleSprite);
            CreateVisual("Volcanic Rock", position, new Vector2(rockSize, rockSize * 0.82f), new Color(0.09f, 0.035f, 0.045f), -27, rockRotation, transform);
            CreateVisual("Volcanic Seam", position + new Vector2(-0.12f, 0.08f), new Vector2(rockSize * 0.72f, 0.16f), index % 2 == 0 ? new Color(1f, 0.18f, 0.01f) : new Color(1f, 0.68f, 0.04f), -26, rockRotation - 24f, transform);
            return;
        }

        if (ActiveTrack.IsDesert)
        {
            float size = 1.25f + (index % 4) * 0.23f;
            float rotation = 18f + index * 29f;
            CreateVisual("Canyon Rock Shadow", position + new Vector2(0.28f, -0.28f), new Vector2(size + 0.55f, size + 0.55f), new Color(0.08f, 0.02f, 0.01f, 0.58f), -28, rotation, transform);
            CreateVisual("Canyon Rock", position, new Vector2(size, size * 0.82f), new Color(0.48f, 0.18f, 0.05f), -27, rotation, transform);
            CreateVisual("Canyon Rock Highlight", position + new Vector2(-0.18f, 0.16f), new Vector2(size * 0.46f, size * 0.34f), new Color(0.82f, 0.37f, 0.09f), -26, rotation, transform);
            return;
        }

        float width = 2.1f + (index % 3) * 0.65f;
        float height = 2.8f + ((index + 1) % 4) * 0.75f;
        float cityRotation = index % 2 == 0 ? 0f : 90f;
        Color neon = index % 2 == 0 ? ActiveTrack.AccentColor : new Color(1f, 0.04f, 0.65f);
        Color window = index % 3 == 0 ? new Color(0.28f, 0.62f, 1f) : neon;
        CreateVisual("City Block Glow", position, new Vector2(width + 0.7f, height + 0.7f), new Color(neon.r, neon.g, neon.b, 0.1f), -29, cityRotation, transform);
        CreateVisual("City Block Shadow", position + new Vector2(0.36f, -0.36f), new Vector2(width + 0.5f, height + 0.5f), new Color(0f, 0f, 0.02f, 0.82f), -28, cityRotation, transform);
        CreateVisual("City Block", position, new Vector2(width, height), new Color(0.014f, 0.02f, 0.065f), -27, cityRotation, transform);
        CreateVisual("City Roof", position + new Vector2(-0.1f, 0.1f), new Vector2(width * 0.74f, height * 0.74f), new Color(0.045f, 0.06f, 0.14f), -26, cityRotation, transform);
        CreateVisual("City Neon Horizontal", position + new Vector2(0f, height * 0.28f), new Vector2(width * 0.74f, 0.14f), neon, -25, cityRotation, transform);
        CreateVisual("City Neon Vertical", position, new Vector2(0.12f, height * 0.62f), window, -25, cityRotation, transform);
    }

    private void SpawnRacers()
    {
        GameObject playerObject = CreateCar("PLAYER", new Color(1f, 0.34f, 0.08f), new Color(1f, 0.86f, 0.28f), 2, selectedCarIndex);
        playerObject.transform.position = PathPoint(PlayerStartT, PlayerStartLane);
        playerObject.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(PlayerStartT));

        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearDamping = 1.25f;
        body.angularDamping = 4f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D collider = playerObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.82f, 1.58f);
        collider.edgeRadius = 0.12f;

        player = playerObject.AddComponent<ArcadeCarController>();
        player.Initialize(this, body);

        playerDamage = playerObject.AddComponent<CarDamage>();
        bool playerUsesStoryVehicleSprite = GetActiveStoryVehicleSprite() != null;
        playerDamage.Initialize(
            this,
            true,
            playerUsesStoryVehicleSprite ? null : GetTrackBrokenCarSprite(selectedCarIndex),
            playerUsesStoryVehicleSprite ? null : GetTrackBrokenCarVariant2Sprite(selectedCarIndex));

        playerWeapon = playerObject.AddComponent<PlayerWeaponSystem>();
        playerWeapon.Initialize(this, body, pixelSprite, circleSprite, true);

        
        Color[] colors =
        {
            new Color(0.06f, 0.78f, 0.86f),
            new Color(0.93f, 0.18f, 0.30f),
            new Color(0.82f, 0.72f, 0.12f),
            new Color(0.55f, 0.28f, 0.92f),
            new Color(0.12f, 0.82f, 0.38f),
            new Color(0.96f, 0.42f, 0.08f)
        };

        for (int i = 0; i < colors.Length; i++)
        {
            int gridSlot = i + 1;
            int rivalCarIndex = (i + 1) % CarNames.Length;
            int row = gridSlot / 2;
            float startT = PlayerStartT - 0.075f * row;
            float lane = gridSlot % 2 == 0 ? -1.65f : 1.65f;
            GameObject aiObject = CreateCar("RIVAL " + (i + 1), colors[i], Color.white, 1, rivalCarIndex);
            aiObject.transform.position = PathPoint(startT, lane);
            aiObject.transform.rotation = Quaternion.Euler(0f, 0f, PathRotation(startT));
            Rigidbody2D aiBody = aiObject.AddComponent<Rigidbody2D>();
            aiBody.bodyType = RigidbodyType2D.Dynamic;
            aiBody.gravityScale = 0f;
            aiBody.mass = 1f;
            aiBody.linearDamping = 0.82f;
            aiBody.angularDamping = 4.2f;
            aiBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            aiBody.interpolation = RigidbodyInterpolation2D.Interpolate;
            BoxCollider2D aiCollider = aiObject.AddComponent<BoxCollider2D>();
            aiCollider.isTrigger = false;
            aiCollider.size = new Vector2(0.82f, 1.58f);
            aiCollider.edgeRadius = 0.12f;

            CircuitAI ai = aiObject.AddComponent<CircuitAI>();
            ai.Initialize(this, aiBody, startT, lane, 14.15f + i * 0.5f, i);
            CarDamage aiDamage = aiObject.AddComponent<CarDamage>();
            bool rivalUsesStoryVehicleSprite = GetActiveStoryVehicleSprite() != null;
            aiDamage.Initialize(
                this,
                false,
                rivalUsesStoryVehicleSprite ? null : GetTrackBrokenCarSprite(rivalCarIndex),
                rivalUsesStoryVehicleSprite ? null : GetTrackBrokenCarVariant2Sprite(rivalCarIndex));
            PlayerWeaponSystem aiWeapon = aiObject.AddComponent<PlayerWeaponSystem>();
            aiWeapon.Initialize(this, aiBody, pixelSprite, circleSprite, false);
            ai.AttachWeapon(aiWeapon);
            
opponents.Add(ai);
        }
    }

    private GameObject CreateCar(string objectName, Color bodyColor, Color stripeColor, int sortingOrder, int carIndex)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(transform);
        int safeCarIndex = Mathf.Clamp(carIndex, 0, CarNames.Length - 1);
        Sprite carSprite = GetActiveStoryVehicleSprite() ?? GetTrackCarSprite(safeCarIndex);
        Color bodyLight = Color.Lerp(bodyColor, Color.white, 0.2f);
        Color bodyDark = Color.Lerp(bodyColor, Color.black, 0.42f);
        Color glass = new Color(0.035f, 0.12f, 0.17f);

        if (carSprite != null)
        {
            CreateVisual("Shadow", new Vector2(0.14f, -0.13f), new Vector2(1.14f, 2.02f), new Color(0f, 0f, 0f, 0.4f), sortingOrder - 3, 0f, root.transform, true, circleSprite);
            CreateVisual("Underglow", Vector2.zero, new Vector2(1.24f, 2.08f), new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.2f), sortingOrder - 2, 0f, root.transform, true, circleSprite);
            Color spriteTint = Color.Lerp(Color.white, bodyColor, 0.72f);
            CreateVisual("Body", Vector2.zero, GetTrackCarVisualScale(carSprite), spriteTint, sortingOrder + 1, 0f, root.transform, true, carSprite);
            root.transform.localScale = CarScales[safeCarIndex];
            return root;
        }

        CreateVisual("Shadow", new Vector2(0.15f, -0.14f), new Vector2(1.16f, 1.98f), new Color(0f, 0f, 0f, 0.42f), sortingOrder - 3, 0f, root.transform, true);
        CreateVisual("Underglow", Vector2.zero, new Vector2(1.28f, 2.08f), new Color(bodyColor.r, bodyColor.g, bodyColor.b, 0.2f), sortingOrder - 2, 0f, root.transform, true);
        CreateVisual("Body Outline", Vector2.zero, new Vector2(1.06f, 1.9f), new Color(0.018f, 0.025f, 0.03f), sortingOrder - 1, 0f, root.transform, true);
        CreateVisual("Body", Vector2.zero, new Vector2(0.94f, 1.76f), bodyColor, sortingOrder, 0f, root.transform, true);
        CreateVisual("Left Side Panel", new Vector2(-0.4f, -0.02f), new Vector2(0.13f, 1.25f), bodyDark, sortingOrder + 1, 0f, root.transform, true);
        CreateVisual("Right Side Panel", new Vector2(0.4f, -0.02f), new Vector2(0.13f, 1.25f), bodyDark, sortingOrder + 1, 0f, root.transform, true);
        CreateVisual("Hood", new Vector2(0f, 0.59f), new Vector2(0.7f, 0.38f), bodyLight, sortingOrder + 1, 0f, root.transform, true);
        CreateVisual("Rear Deck", new Vector2(0f, -0.64f), new Vector2(0.72f, 0.3f), bodyDark, sortingOrder + 1, 0f, root.transform, true);
        CreateVisual("Cabin Outline", new Vector2(0f, 0.12f), new Vector2(0.76f, 0.74f), new Color(0.012f, 0.025f, 0.032f), sortingOrder + 2, 0f, root.transform, true);
        CreateVisual("Cabin", new Vector2(0f, 0.12f), new Vector2(0.66f, 0.62f), glass, sortingOrder + 3, 0f, root.transform, true);
        CreateVisual("Windshield", new Vector2(0f, 0.42f), new Vector2(0.58f, 0.13f), new Color(0.36f, 0.84f, 0.94f), sortingOrder + 4, 0f, root.transform, true);
        CreateVisual("Rear Window", new Vector2(0f, -0.18f), new Vector2(0.56f, 0.12f), new Color(0.14f, 0.42f, 0.52f), sortingOrder + 4, 0f, root.transform, true);
        CreateVisual("Stripe", new Vector2(0f, -0.48f), new Vector2(0.13f, 0.66f), stripeColor, sortingOrder + 3, 0f, root.transform, true);
        CreateVisual("Number Panel", new Vector2(0f, -0.5f), new Vector2(0.34f, 0.22f), new Color(0.94f, 0.95f, 0.9f), sortingOrder + 4, 0f, root.transform, true);
        CreateVisual("Front Bumper", new Vector2(0f, 0.88f), new Vector2(0.82f, 0.08f), new Color(0.02f, 0.03f, 0.035f), sortingOrder + 2, 0f, root.transform, true);
        CreateVisual("Rear Bumper", new Vector2(0f, -0.88f), new Vector2(0.82f, 0.08f), new Color(0.02f, 0.03f, 0.035f), sortingOrder + 2, 0f, root.transform, true);
        CreateVisual("Left Headlight Glow", new Vector2(-0.28f, 0.91f), new Vector2(0.34f, 0.34f), new Color(1f, 0.94f, 0.58f, 0.15f), sortingOrder + 3, 45f, root.transform, true);
        CreateVisual("Right Headlight Glow", new Vector2(0.28f, 0.91f), new Vector2(0.34f, 0.34f), new Color(1f, 0.94f, 0.58f, 0.15f), sortingOrder + 3, 45f, root.transform, true);
        CreateVisual("Left Headlight", new Vector2(-0.28f, 0.84f), new Vector2(0.2f, 0.1f), new Color(1f, 0.92f, 0.5f), sortingOrder + 5, 0f, root.transform, true);
        CreateVisual("Right Headlight", new Vector2(0.28f, 0.84f), new Vector2(0.2f, 0.1f), new Color(1f, 0.92f, 0.5f), sortingOrder + 5, 0f, root.transform, true);
        CreateVisual("Left Tail Light", new Vector2(-0.28f, -0.84f), new Vector2(0.2f, 0.1f), new Color(1f, 0.08f, 0.04f), sortingOrder + 5, 0f, root.transform, true);
        CreateVisual("Right Tail Light", new Vector2(0.28f, -0.84f), new Vector2(0.2f, 0.1f), new Color(1f, 0.08f, 0.04f), sortingOrder + 5, 0f, root.transform, true);

        Color tire = new Color(0.025f, 0.03f, 0.035f);
        Color wheelHub = new Color(0.45f, 0.52f, 0.54f);
        Vector2[] wheelPositions =
        {
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-0.5f, -0.5f),
            new Vector2(0.5f, -0.5f)
        };

        for (int i = 0; i < wheelPositions.Length; i++)
        {
            CreateVisual("Wheel", wheelPositions[i], new Vector2(0.17f, 0.45f), tire, sortingOrder, 0f, root.transform, true);
            CreateVisual("Wheel Hub", wheelPositions[i], new Vector2(0.08f, 0.18f), wheelHub, sortingOrder + 1, 0f, root.transform, true);
        }

        root.transform.localScale = CarScales[safeCarIndex];
        return root;
    }

    private Sprite GetTrackCarSprite(int carIndex)
    {
        if (trackCarSprites == null || trackCarSprites.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Clamp(carIndex, 0, trackCarSprites.Length - 1);
        return trackCarSprites[safeIndex] != null ? trackCarSprites[safeIndex] : trackCarSprites[0];
    }

    private Sprite GetTrackBrokenCarSprite(int carIndex)
    {
        if (trackBrokenCarSprites == null || trackBrokenCarSprites.Length == 0)
        {
            return null;
        }

        int safeIndex = Mathf.Clamp(carIndex, 0, trackBrokenCarSprites.Length - 1);
        return trackBrokenCarSprites[safeIndex];
    }

    private Sprite GetTrackBrokenCarVariant2Sprite(int carIndex)
    {
        if (trackBrokenCarVariant2Sprites == null || trackBrokenCarVariant2Sprites.Length == 0)
        {
            return GetTrackBrokenCarSprite(carIndex);
        }

        int safeIndex = Mathf.Clamp(carIndex, 0, trackBrokenCarVariant2Sprites.Length - 1);
        Sprite variant = trackBrokenCarVariant2Sprites[safeIndex];
        return variant != null ? variant : GetTrackBrokenCarSprite(carIndex);
    }

    private static Vector2 GetTrackCarVisualScale(Sprite sprite)
    {
        if (sprite == null || sprite.pixelsPerUnit <= 0f || sprite.rect.height <= 0f)
        {
            return Vector2.one;
        }

        const float targetHeight = 1.98f;
        float nativeHeight = sprite.rect.height / sprite.pixelsPerUnit;
        float scale = targetHeight / nativeHeight;
        return new Vector2(scale, scale);
    }

    private SpriteRenderer CreateVisual(string objectName, Vector2 position, Vector2 scale, Color color, int sortingOrder, float rotation, Transform parent, bool local = false, Sprite sprite = null)
    {
        GameObject visual = new GameObject(objectName);
        visual.transform.SetParent(parent);
        if (local)
        {
            visual.transform.localPosition = position;
            visual.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }
        else
        {
            visual.transform.position = position;
            visual.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        }

        visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite != null ? sprite : pixelSprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 7.7f;
        camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        if (FindAnyObjectByType<AudioListener>() == null)
        {
            camera.gameObject.AddComponent<AudioListener>();
        }
        camera.gameObject.AddComponent<SmoothRaceCamera>().Initialize(player.transform);
    }

    private void LoadProgress()
    {
        coins = Mathf.Max(0, PlayerPrefs.GetInt(CoinsKey, 500));
        engineLevel = Mathf.Clamp(PlayerPrefs.GetInt(EngineKey, 0), 0, MaxUpgradeLevel);
        handlingLevel = Mathf.Clamp(PlayerPrefs.GetInt(HandlingKey, 0), 0, MaxUpgradeLevel);
        armorLevel = Mathf.Clamp(PlayerPrefs.GetInt(ArmorKey, 0), 0, MaxUpgradeLevel);
        weaponDamageLevel = Mathf.Clamp(PlayerPrefs.GetInt(WeaponDamageKey, 0), 0, MaxUpgradeLevel);
        weaponAmmoLevel = Mathf.Clamp(PlayerPrefs.GetInt(WeaponAmmoKey, 0), 0, MaxUpgradeLevel);
        weaponRateLevel = Mathf.Clamp(PlayerPrefs.GetInt(WeaponRateKey, 0), 0, MaxUpgradeLevel);
        selectedCarIndex = Mathf.Clamp(PlayerPrefs.GetInt(SelectedCarKey, 0), 0, CarNames.Length - 1);
        if (!IsCarOwned(selectedCarIndex))
        {
            selectedCarIndex = 0;
        }

        garageCarIndex = selectedCarIndex;
        paintColorIndex = Mathf.Clamp(PlayerPrefs.GetInt(PaintKey, 0), 0, PaintColors.Length - 1);
        neonColorIndex = Mathf.Clamp(PlayerPrefs.GetInt(NeonKey, 0), 0, NeonColors.Length - 1);
        LoadStoryProgress();
    }

    private void SaveProgress()
    {
        PlayerPrefs.SetInt(CoinsKey, coins);
        PlayerPrefs.SetInt(EngineKey, engineLevel);
        PlayerPrefs.SetInt(HandlingKey, handlingLevel);
        PlayerPrefs.SetInt(ArmorKey, armorLevel);
        PlayerPrefs.SetInt(WeaponDamageKey, weaponDamageLevel);
        PlayerPrefs.SetInt(WeaponAmmoKey, weaponAmmoLevel);
        PlayerPrefs.SetInt(WeaponRateKey, weaponRateLevel);
        PlayerPrefs.SetInt(SelectedCarKey, selectedCarIndex);
        PlayerPrefs.SetInt(PaintKey, paintColorIndex);
        PlayerPrefs.SetInt(NeonKey, neonColorIndex);
        PlayerPrefs.Save();
    }

    private bool IsCarOwned(int carIndex)
    {
        return carIndex == 0 || PlayerPrefs.GetInt(OwnedCarPrefix + carIndex, 0) == 1;
    }

    private void TryBuyOrSelectCar()
    {
        if (IsCarOwned(garageCarIndex))
        {
            selectedCarIndex = garageCarIndex;
            garageMessage = CarNames[selectedCarIndex] + " ВЫБРАНА";
        }
        else
        {
            int price = CarPrices[garageCarIndex];
            if (coins < price)
            {
                garageMessage = "НЕДОСТАТОЧНО МОНЕТ ДЛЯ ПОКУПКИ";
                garageMessageUntil = Time.unscaledTime + 2.5f;
                return;
            }

            coins -= price;
            PlayerPrefs.SetInt(OwnedCarPrefix + garageCarIndex, 1);
            selectedCarIndex = garageCarIndex;
            garageMessage = CarNames[selectedCarIndex] + " КУПЛЕНА И ВЫБРАНА";
        }

        garageMessageUntil = Time.unscaledTime + 2.5f;
        ApplySelectedCarVisuals();
        SaveProgress();
    }

    private void SelectPaint(int index)
    {
        paintColorIndex = Mathf.Clamp(index, 0, PaintColors.Length - 1);
        garageMessage = "ЦВЕТ КУЗОВА ПРИМЕНЁН";
        garageMessageUntil = Time.unscaledTime + 1.5f;
        ApplySelectedCarVisuals();
        SaveProgress();
    }

    private void SelectNeon(int index)
    {
        neonColorIndex = Mathf.Clamp(index, 0, NeonColors.Length - 1);
        garageMessage = "НЕОНОВАЯ ПОДСВЕТКА ПРИМЕНЕНА";
        garageMessageUntil = Time.unscaledTime + 1.5f;
        ApplySelectedCarVisuals();
        SaveProgress();
    }

    private void ApplySelectedCarVisuals()
    {
        if (player == null)
        {
            return;
        }

        Color paint = PaintColors[paintColorIndex];
        Color neon = NeonColors[neonColorIndex];
        Sprite storyVehicleSprite = GetActiveStoryVehicleSprite();
        Sprite selectedSprite = storyVehicleSprite ?? GetTrackCarSprite(selectedCarIndex);
        Transform bodyPart = player.transform.Find("Body");
        SpriteRenderer bodyRenderer = null;
        if (bodyPart != null && selectedSprite != null)
        {
            bodyRenderer = bodyPart.GetComponent<SpriteRenderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = selectedSprite;
                bodyPart.localScale = GetTrackCarVisualScale(selectedSprite);
            }
        }

        if (bodyRenderer != null && selectedSprite != null && carPaintMaterial != null)
        {
            bodyRenderer.sharedMaterial = carPaintMaterial;
            bodyRenderer.color = Color.white;
            MaterialPropertyBlock paintProperties = new MaterialPropertyBlock();
            bodyRenderer.GetPropertyBlock(paintProperties);
            paintProperties.SetColor(PaintColorShaderProperty, paint);
            bodyRenderer.SetPropertyBlock(paintProperties);
        }
        else
        {
            SetPlayerPartColor("Body", selectedSprite != null ? Color.Lerp(Color.white, paint, 0.72f) : paint);
        }

        SetPlayerPartColor("Hood", Color.Lerp(paint, Color.white, 0.2f));
        SetPlayerPartColor("Rear Deck", Color.Lerp(paint, Color.black, 0.42f));
        SetPlayerPartColor("Left Side Panel", Color.Lerp(paint, Color.black, 0.42f));
        SetPlayerPartColor("Right Side Panel", Color.Lerp(paint, Color.black, 0.42f));
        SetPlayerPartColor("Stripe", neon);
        SetPlayerPartColor("Windshield", Color.Lerp(new Color(0.08f, 0.25f, 0.32f), neon, 0.35f));
        SetPlayerPartColor("Underglow", new Color(neon.r, neon.g, neon.b, 0.24f));
        if (playerDamage != null)
        {
            playerDamage.ConfigureSprites(
                selectedSprite,
                storyVehicleSprite != null ? null : GetTrackBrokenCarSprite(selectedCarIndex),
                storyVehicleSprite != null ? null : GetTrackBrokenCarVariant2Sprite(selectedCarIndex));
        }

        player.transform.localScale = CarScales[selectedCarIndex];
        ApplyStoryVehicleProfile();
    }

    private void SetPlayerPartColor(string partName, Color color)
    {
        Transform part = player.transform.Find(partName);
        if (part == null)
        {
            return;
        }

        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = color;
        }
    }

    private void AddCoins(int amount)
    {
        coins = Mathf.Max(0, coins + amount);
        SaveProgress();
    }

    private int GetUpgradeCost(int category)
    {
        int level = GetUpgradeLevel(category);
        int baseCost = category == 0 ? 180
            : category == 1 ? 150
            : category == 2 ? 130
            : category == 3 ? 210
            : category == 4 ? 170
            : 190;
        return baseCost * (level + 1);
    }

    private int GetUpgradeLevel(int category)
    {
        switch (category)
        {
            case 0: return engineLevel;
            case 1: return handlingLevel;
            case 2: return armorLevel;
            case 3: return weaponDamageLevel;
            case 4: return weaponAmmoLevel;
            case 5: return weaponRateLevel;
            default: return MaxUpgradeLevel;
        }
    }

    private void TryBuyUpgrade(int category)
    {
        int level = GetUpgradeLevel(category);
        if (level >= MaxUpgradeLevel)
        {
            garageMessage = "УЛУЧШЕНИЕ УЖЕ МАКСИМАЛЬНОГО УРОВНЯ";
            garageMessageUntil = Time.unscaledTime + 2f;
            return;
        }

        int cost = GetUpgradeCost(category);
        if (coins < cost)
        {
            garageMessage = "НЕДОСТАТОЧНО МОНЕТ";
            garageMessageUntil = Time.unscaledTime + 2f;
            return;
        }

        coins -= cost;
        if (category == 0)
        {
            engineLevel++;
            garageMessage = "ДВИГАТЕЛЬ УЛУЧШЕН / СОПЕРНИКИ УСКОРИЛИСЬ";
        }
        else if (category == 1)
        {
            handlingLevel++;
            garageMessage = "УПРАВЛЕНИЕ УЛУЧШЕНО / СОПЕРНИКИ СТАЛИ ТОЧНЕЕ";
        }
        else if (category == 2)
        {
            armorLevel++;
            garageMessage = "КОРПУС УСИЛЕН / СОПЕРНИКИ СТАЛИ ПРОЧНЕЕ";
        }
        else if (category == 3)
        {
            weaponDamageLevel++;
            garageMessage = "УРОН ОРУЖИЯ УВЕЛИЧЕН НА 16%";
        }
        else if (category == 4)
        {
            weaponAmmoLevel++;
            garageMessage = "МАКСИМАЛЬНЫЙ БОЕЗАПАС УВЕЛИЧЕН НА 2";
        }
        else
        {
            weaponRateLevel++;
            garageMessage = "СКОРОСТРЕЛЬНОСТЬ ОРУЖИЯ УВЕЛИЧЕНА";
        }

        garageMessageUntil = Time.unscaledTime + 2f;
        SaveProgress();
    }

    private void ToggleGarage()
    {
        bool wasGarageOpen = garageOpen;
        garageOpen = !garageOpen;
        garageCarIndex = selectedCarIndex;
        if (garageOpen)
        {
            garageUpgradeTab = 0;
        }
        Time.timeScale = mainMenuOpen || garageOpen ? 0f : 1f;
        if (wasGarageOpen && !garageOpen && mainMenuOpen)
        {
            menuAnimationStartedAt = Time.unscaledTime;
        }
    }

    private void StartRaceFromMenu()
    {
        if (trackLoadPending)
        {
            return;
        }

        mainMenuModeSelectionOpen = false;
        RestoreFreeRaceSelection();
        PlayMenuStartSfx();
        storyModeOpen = false;
        storyRaceActive = false;
        pendingStoryRaceAfterReload = false;
        ResetStoryMissionResult();
        PrepareArcadeRaceStart();
        PlayerPrefs.SetInt(TrackKey, selectedTrackIndex);
        PlayerPrefs.Save();

        if (builtTrackIndex != selectedTrackIndex)
        {
            trackLoadPending = true;
            startRaceAfterSceneReload = true;
            StartCoroutine(LoadSelectedTrackForRace());
            return;
        }

        BeginRaceNow();
    }

    private void BeginRaceNow()
    {
        mainMenuOpen = false;
        garageOpen = false;
        storyModeOpen = false;
        ApplySelectedCarVisuals();
        RestartRace();
    }

    private IEnumerator BeginRaceAfterReload()
    {
        yield return null;
        BeginRaceNow();
    }

    private IEnumerator LoadSelectedTrackForRace()
    {
        // Give IMGUI one frame to show the loading panel before scene construction begins.
        yield return new WaitForEndOfFrame();
        Time.timeScale = 1f;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
            SceneManager.GetActiveScene().buildIndex,
            LoadSceneMode.Single);
        while (loadOperation != null && !loadOperation.isDone)
        {
            yield return null;
        }
    }

    private void OpenMainMenu()
    {
        bool returnToStory = storyRaceActive;
        mainMenuOpen = true;
        garageOpen = false;
        mainMenuModeSelectionOpen = false;
        storyRaceActive = false;
        storyModeOpen = returnToStory;
        ExitArcadeRaceModeToMenu();
        Time.timeScale = 0f;
        menuAnimationStartedAt = Time.unscaledTime;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private bool GaragePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.gKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.G);
#endif
    }

    private int GaragePurchasePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return -1;
        }

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 0;
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 1;
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 2;
        return -1;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
        return -1;
#endif
    }

    private int TrackChoicePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return -1;
        }

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) return 0;
        if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) return 1;
        if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) return 2;
        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) return 3;
        if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) return 4;
        return -1;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) return 3;
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) return 4;
        return -1;
#endif
    }

    private bool ConfirmPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    private bool CancelPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        ReleasePaintedGarageCarPreviews();
        if (carPaintMaterial != null)
        {
            Destroy(carPaintMaterial);
        }
    }

    
    private void Update()
    {
        hitFlashAmount = Mathf.MoveTowards(hitFlashAmount, 0f, Time.unscaledDeltaTime * 1.75f);
        nitroFlashAmount = Mathf.MoveTowards(nitroFlashAmount, 0f, Time.unscaledDeltaTime * 3.8f);
        UpdateExtendedAudio();
        UpdateStartGantryLights();
        if (mainMenuOpen)
        {
            if (storyModeOpen)
            {
                UpdateStoryMenuInput();
            }
            else if (garageOpen)
            {
                int purchase = GaragePurchasePressed();
                if (purchase >= 0)
                {
                    TryBuyUpgrade(garageUpgradeTab == 1 ? purchase + 3 : purchase);
                }

                if (GaragePressed() || CancelPressed())
                {
                    garageOpen = false;
                    menuAnimationStartedAt = Time.unscaledTime;
                }
            }
            else if (mainMenuModeSelectionOpen)
            {
                int modeChoice = TrackChoicePressed();
                if (modeChoice >= 0 && modeChoice <= 4)
                {
                    ActivateMainMenuMode(modeChoice);
                }
                else if (CancelPressed())
                {
                    CloseMainMenuModeSelection();
                }
                else if (StoryPreviousPressed())
                {
                    mainMenuSelectedMode = (mainMenuSelectedMode + 4) % 5;
                    PlayMenuClickSfx();
                }
                else if (StoryNextPressed())
                {
                    mainMenuSelectedMode = (mainMenuSelectedMode + 1) % 5;
                    PlayMenuClickSfx();
                }
                else if (ConfirmPressed())
                {
                    ActivateMainMenuMode(mainMenuSelectedMode);
                }
            }
            else
            {
                int trackChoice = TrackChoicePressed();
                if (trackChoice >= 0 && trackChoice < RaceTrackCatalog.Count)
                {
                    SelectTrack(trackChoice);
                }
                else if (GaragePressed())
                {
                    ToggleGarage();
                }
                else if (ConfirmPressed())
                {
                    OpenMainMenuModeSelection();
                }
            }

            return;
        }

        if (CancelPressed())
        {
            OpenMainMenu();
            return;
        }

        if (GaragePressed())
        {
            ToggleGarage();
        }

        if (garageOpen)
        {
            int purchase = GaragePurchasePressed();
            if (purchase >= 0)
            {
                TryBuyUpgrade(garageUpgradeTab == 1 ? purchase + 3 : purchase);
            }
            return;
        }

        if (!raceStarted)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0f)
            {
                countdown = 0f;
                raceStarted = true;
                lapStartedAt = 0f;
            }
        }
        else if (!raceFinished)
        {
            raceTime += Time.deltaTime;
            UpdateStoryMissionProgress();
            UpdateArcadeRaceProgress();
            UpdateLapProgress();
        }

        if (RestartPressed())
        {
            RestartRace();
        }
    }

    public void AddHitFlash(float amount)
    {
        hitFlashAmount = Mathf.Clamp01(hitFlashAmount + Mathf.Clamp01(amount));
    }

    public void ShakeCamera(float amount, float duration)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        SmoothRaceCamera followCamera = camera.GetComponent<SmoothRaceCamera>();
        if (followCamera == null)
        {
            return;
        }

        followCamera.AddShake(amount, duration);
    }

    public void PlayDriftSfx(float driftAmount)
    {
        if (sfxSource == null || driftSfxClip == null || Time.time < driftSfxCooldownUntil)
        {
            return;
        }

        AudioClip clip = driftAmount >= 0.82f && driftBoostSfxClip != null
            ? driftBoostSfxClip
            : driftSfxClip;
        sfxSource.pitch = Mathf.Lerp(0.86f, 1.3f, driftAmount);
        sfxSource.PlayOneShot(clip, Mathf.Lerp(0.12f, 0.28f, driftAmount));
        driftSfxCooldownUntil = Time.time + Mathf.Lerp(0.16f, 0.07f, driftAmount);
    }

    public void PlayNitroSfx()
    {
        if (sfxSource == null || nitroSfxClip == null)
        {
            return;
        }

        if (Time.time < nitroSfxCooldownUntil)
        {
            return;
        }

        sfxSource.pitch = 1.15f;
        sfxSource.PlayOneShot(nitroSfxClip, 0.22f);
        nitroSfxCooldownUntil = Time.time + 0.09f;
    }

    public void TriggerNitroBurst(Vector2 position, Vector2 forward, bool emphasizePlayer = true, int particleCount = 10)
    {
        if (emphasizePlayer)
        {
            nitroFlashAmount = 1f;
            ShakeCamera(0.32f, 0.2f);
        }

        if (pixelSprite == null)
        {
            return;
        }

        Vector2 backwards = forward.sqrMagnitude > 0.001f ? -forward.normalized : Vector2.down;
        Color neon = NeonColors[Mathf.Clamp(neonColorIndex, 0, NeonColors.Length - 1)];
        int burstParticleCount = Mathf.Clamp(particleCount, 1, 24);
        for (int i = 0; i < burstParticleCount; i++)
        {
            Vector2 spread = Quaternion.Euler(0f, 0f, Random.Range(-28f, 28f)) * backwards;
            GameObject particle = new GameObject("NitroBurst");
            particle.transform.SetParent(transform);
            particle.transform.position = new Vector3(position.x, position.y, 0f);
            particle.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(spread.y, spread.x) * Mathf.Rad2Deg - 90f);
            particle.transform.localScale = new Vector3(Random.Range(0.045f, 0.1f), Random.Range(0.24f, 0.52f), 1f);
            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = pixelSprite;
            renderer.sortingOrder = 19;
            renderer.color = i % 3 == 0
                ? new Color(1f, 0.2f, 0.72f, 0.92f)
                : new Color(neon.r, neon.g, neon.b, 0.88f);
            Vector2 speed = spread * Random.Range(4.5f, 8.5f) + Random.insideUnitCircle * 0.8f;
            StartCoroutine(AnimateAndDestroySpark(particle.transform, renderer, speed, Random.Range(0.18f, 0.34f)));
        }
    }

    public void PlayImpactSound(float impactSpeed)
    {
        if (sfxSource == null || impactSfxClip == null || Time.time < impactSfxCooldownUntil)
        {
            return;
        }

        sfxSource.pitch = Mathf.Lerp(0.82f, 1.22f, Mathf.Clamp01(impactSpeed / 25f));
        sfxSource.PlayOneShot(impactSfxClip, Mathf.Lerp(0.2f, 0.42f, Mathf.Clamp01(impactSpeed / 30f)));
        impactSfxCooldownUntil = Time.time + 0.18f;
    }

    public void SpawnDriftSmoke(Vector2 position, Vector2 velocity, float intensity)
    {
        if (circleSprite == null || pixelTexture == null)
        {
            return;
        }

        GameObject smokeObject = new GameObject("DriftSmoke");
        smokeObject.transform.SetParent(transform);
        Vector2 spawnOffset = Random.insideUnitCircle * 0.08f;
        smokeObject.transform.position = new Vector3(position.x + spawnOffset.x, position.y + spawnOffset.y, 0f);
        smokeObject.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        SpriteRenderer renderer = smokeObject.AddComponent<SpriteRenderer>();
        renderer.sprite = circleSprite;
        renderer.sortingOrder = 15;
        float alpha = Mathf.Lerp(0.13f, 0.38f, Mathf.Clamp01(intensity));
        Color neon = NeonColors[Mathf.Clamp(neonColorIndex, 0, NeonColors.Length - 1)];
        Color smokeColor = Color.Lerp(new Color(0.78f, 0.86f, 0.92f), neon, 0.18f);
        renderer.color = new Color(smokeColor.r, smokeColor.g, smokeColor.b, alpha);
        float scale = Mathf.Lerp(0.18f, 0.52f, Mathf.Clamp01(intensity));
        smokeObject.transform.localScale = new Vector3(scale, scale, 1f);
        Vector2 smokeVelocity = velocity * 0.72f + Random.insideUnitCircle * 0.24f;
        StartCoroutine(AnimateAndDestroySmoke(smokeObject.transform, renderer, smokeVelocity, Mathf.Lerp(0.46f, 0.78f, Mathf.Clamp01(intensity))));

        if (intensity > 0.55f && Random.value < 0.3f)
        {
            GameObject ember = new GameObject("DriftEmber");
            ember.transform.SetParent(transform);
            ember.transform.position = smokeObject.transform.position;
            ember.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            ember.transform.localScale = new Vector3(Random.Range(0.035f, 0.07f), Random.Range(0.12f, 0.24f), 1f);
            SpriteRenderer emberRenderer = ember.AddComponent<SpriteRenderer>();
            emberRenderer.sprite = pixelSprite;
            emberRenderer.sortingOrder = 18;
            emberRenderer.color = new Color(neon.r, neon.g, neon.b, 0.86f);
            Vector2 emberVelocity = velocity * 0.45f + Random.insideUnitCircle * 1.6f;
            StartCoroutine(AnimateAndDestroySpark(ember.transform, emberRenderer, emberVelocity, Random.Range(0.18f, 0.3f)));
        }
    }

    public void SpawnSurfaceSpray(Vector2 position, Vector2 velocity, float intensity)
    {
        if (circleSprite == null)
        {
            return;
        }

        intensity = Mathf.Clamp01(intensity);
        Color sprayColor;
        if (RainPuddlesActive)
        {
            sprayColor = new Color(0.56f, 0.82f, 0.9f);
        }
        else if (ActiveTrack.IsGlacier)
        {
            sprayColor = new Color(0.82f, 0.94f, 1f);
        }
        else if (ActiveTrack.IsVolcanic)
        {
            sprayColor = new Color(0.2f, 0.12f, 0.11f);
        }
        else if (ActiveTrack.IsLunar)
        {
            sprayColor = new Color(0.56f, 0.54f, 0.68f);
        }
        else if (ActiveTrack.IsDesert)
        {
            sprayColor = new Color(0.78f, 0.43f, 0.14f);
        }
        else
        {
            sprayColor = new Color(0.42f, 0.48f, 0.5f);
        }

        int particleCount = intensity > 0.72f ? 2 : 1;
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = new GameObject("Surface Spray");
            particle.transform.SetParent(transform);
            particle.transform.position = position + Random.insideUnitCircle * 0.12f;
            particle.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            SpriteRenderer renderer = particle.AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;
            renderer.sortingOrder = 17;
            float alpha = Mathf.Lerp(0.16f, 0.42f, intensity) * (RainPuddlesActive ? 0.72f : 1f);
            renderer.color = new Color(sprayColor.r, sprayColor.g, sprayColor.b, alpha);

            float width = Random.Range(0.16f, 0.34f) * Mathf.Lerp(0.8f, 1.45f, intensity);
            float length = Random.Range(0.25f, 0.54f) * Mathf.Lerp(0.8f, 1.5f, intensity);
            particle.transform.localScale = new Vector3(width, length, 1f);
            Vector2 particleVelocity = velocity * Random.Range(0.7f, 1.08f) + Random.insideUnitCircle * 0.42f;
            StartCoroutine(AnimateAndDestroySmoke(
                particle.transform,
                renderer,
                particleVelocity,
                Random.Range(0.38f, 0.68f)));
        }
    }

    public void SpawnImpactSparks(Vector2 position, Vector2 direction, float intensity)
    {
        if (pixelTexture == null || circleSprite == null)
        {
            return;
        }

        intensity = Mathf.Clamp01(intensity);
        int count = Mathf.Clamp(Mathf.CeilToInt(5f + intensity * 7f), 3, 12);

        GameObject flashObject = new GameObject("ImpactFlash");
        flashObject.transform.SetParent(transform);
        flashObject.transform.position = new Vector3(position.x, position.y, 0f);
        flashObject.transform.localScale = Vector3.one * Mathf.Lerp(0.18f, 0.46f, intensity);
        SpriteRenderer flashRenderer = flashObject.AddComponent<SpriteRenderer>();
        flashRenderer.sprite = circleSprite;
        flashRenderer.sortingOrder = 23;
        flashRenderer.color = new Color(1f, Mathf.Lerp(0.55f, 0.92f, intensity), 0.16f, 0.82f);
        StartCoroutine(AnimateAndDestroyFlash(flashObject.transform, flashRenderer, Mathf.Lerp(0.12f, 0.24f, intensity)));

        for (int i = 0; i < count; i++)
        {
            float randomAngle = Random.value * 360f * Mathf.Deg2Rad;
            Vector2 directionOffset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
            if (direction.sqrMagnitude > 0.0001f)
            {
                Vector2 forward = direction.normalized;
                Vector3 rotated = Quaternion.AngleAxis(Random.Range(-55f, 55f), Vector3.forward) * new Vector3(forward.x, forward.y, 0f);
                directionOffset = new Vector2(rotated.x, rotated.y);
            }

            GameObject sparkObject = new GameObject("ImpactSpark");
            sparkObject.transform.SetParent(transform);
            sparkObject.transform.position = new Vector3(position.x, position.y, 0f);
            SpriteRenderer renderer = sparkObject.AddComponent<SpriteRenderer>();
            renderer.sprite = pixelSprite;
            renderer.sortingOrder = 21;
            Color sparkColor = new Color(
                1f,
                Mathf.Lerp(0.45f, 0.95f, Random.value),
                Mathf.Lerp(0.08f, 0.45f, Random.value),
                Mathf.Lerp(0.65f, 0.95f, Random.value));
            renderer.color = sparkColor;

            sparkObject.transform.rotation = Quaternion.Euler(0f, 0f, randomAngle * Mathf.Rad2Deg);
            float sparkScale = Mathf.Lerp(0.08f, 0.24f, Mathf.Clamp01(intensity));
            sparkObject.transform.localScale = new Vector3(Random.Range(0.3f, 0.85f), sparkScale, 1f);
            float spread = Random.Range(0.32f, 0.88f);
            Vector2 speed = directionOffset * spread * Mathf.Lerp(3.2f, 7.4f, intensity);
            StartCoroutine(AnimateAndDestroySpark(sparkObject.transform, renderer, speed, Mathf.Lerp(0.22f, 0.42f, Mathf.Clamp01(intensity))));
        }
    }

    public void SpawnObstacleDebris(Vector2 position, Vector2 direction, Color obstacleColor, float intensity)
    {
        if (pixelSprite == null)
        {
            return;
        }

        intensity = Mathf.Clamp01(intensity);
        Vector2 forward = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        int fragmentCount = Mathf.Clamp(Mathf.RoundToInt(3f + intensity * 5f), 3, 8);

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = new GameObject("Obstacle Fragment");
            fragment.transform.SetParent(transform);
            fragment.transform.position = position + Random.insideUnitCircle * 0.16f;
            fragment.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            float width = Random.Range(0.08f, 0.19f) * Mathf.Lerp(0.8f, 1.35f, intensity);
            float height = Random.Range(0.1f, 0.32f) * Mathf.Lerp(0.8f, 1.35f, intensity);
            fragment.transform.localScale = new Vector3(width, height, 1f);

            SpriteRenderer renderer = fragment.AddComponent<SpriteRenderer>();
            renderer.sprite = pixelSprite;
            renderer.sortingOrder = 22;
            renderer.color = Color.Lerp(obstacleColor, new Color(0.06f, 0.07f, 0.075f), Random.Range(0.08f, 0.55f));

            Vector3 rotated = Quaternion.AngleAxis(Random.Range(-78f, 78f), Vector3.forward) * new Vector3(forward.x, forward.y, 0f);
            Vector2 fragmentDirection = new Vector2(rotated.x, rotated.y);
            Vector2 fragmentVelocity = fragmentDirection * Random.Range(2.4f, 5.8f) * Mathf.Lerp(0.65f, 1.2f, intensity);
            float spin = Random.Range(-620f, 620f);
            StartCoroutine(AnimateAndDestroyObstacleDebris(
                fragment.transform,
                renderer,
                fragmentVelocity,
                spin,
                Random.Range(0.46f, 0.82f)));
        }
    }

    private IEnumerator AnimateAndDestroyObstacleDebris(Transform target, SpriteRenderer sprite, Vector2 velocity, float spin, float lifeTime)
    {
        float timer = 0f;
        while (timer < lifeTime)
        {
            float t = timer / Mathf.Max(0.001f, lifeTime);
            if (sprite != null)
            {
                Color color = sprite.color;
                color.a = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0.58f, 1f, t));
                sprite.color = color;
            }

            if (target != null)
            {
                target.position += (Vector3)(velocity * Time.deltaTime);
                target.Rotate(0f, 0f, spin * Time.deltaTime);
                velocity = Vector2.Lerp(velocity, Vector2.zero, 2.3f * Time.deltaTime);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (target != null)
        {
            Destroy(target.gameObject);
        }
    }

    private IEnumerator AnimateAndDestroySmoke(Transform target, SpriteRenderer sprite, Vector2 velocity, float lifeTime)
    {
        float timer = 0f;
        while (timer < lifeTime)
        {
            float t = timer / lifeTime;
            if (sprite != null)
            {
                Color color = sprite.color;
                color.a = Mathf.Lerp(0.52f, 0f, t);
                sprite.color = color;
            }

            if (target != null)
            {
                target.position += (Vector3)(velocity * Time.deltaTime);
                target.localScale *= (1f + 0.72f * Time.deltaTime);
                target.Rotate(0f, 0f, 38f * Time.deltaTime);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (target != null)
        {
            Destroy(target.gameObject);
        }
    }

    private IEnumerator AnimateAndDestroySpark(Transform target, SpriteRenderer sprite, Vector2 speed, float lifeTime)
    {
        float timer = 0f;
        while (timer < lifeTime)
        {
            float t = timer / lifeTime;
            if (sprite != null)
            {
                Color color = sprite.color;
                color.a = Mathf.Lerp(0.86f, 0f, t);
                sprite.color = color;
            }

            if (target != null)
            {
                target.position += (Vector3)(speed * Time.deltaTime);
                target.localScale *= 1f + 1.4f * Time.deltaTime;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (target != null)
        {
            Destroy(target.gameObject);
        }
    }

    private IEnumerator AnimateAndDestroyFlash(Transform target, SpriteRenderer sprite, float lifeTime)
    {
        float timer = 0f;
        Vector3 startScale = target != null ? target.localScale : Vector3.one;
        while (timer < lifeTime)
        {
            float t = timer / Mathf.Max(0.001f, lifeTime);
            if (sprite != null)
            {
                Color color = sprite.color;
                color.a = Mathf.Lerp(0.82f, 0f, t * t);
                sprite.color = color;
            }

            if (target != null)
            {
                target.localScale = startScale * Mathf.Lerp(1f, 3.4f, t);
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (target != null)
        {
            Destroy(target.gameObject);
        }
    }

    private void UpdateLapProgress()
    {
        const float fullLoop = Mathf.PI * 2f;
        Vector2 playerPosition = player.transform.position;
        float checkpointStep = fullLoop / LapCheckpointCount;
        float normalized = trackProgressInitialized
            ? TrackParameterNear(playerPosition, lastTrackParameter)
            : TrackParameter(playerPosition);
        int sector = Mathf.FloorToInt(normalized / checkpointStep) % LapCheckpointCount;

        if (!trackProgressInitialized)
        {
            lastTrackParameter = normalized;
            lastCheckpointSector = sector;
            lapCheckpointMask = 1u << sector;
            trackProgressInitialized = true;
            return;
        }

        float previousParameter = lastTrackParameter;
        float delta = Mathf.DeltaAngle(previousParameter * Mathf.Rad2Deg, normalized * Mathf.Rad2Deg) * Mathf.Deg2Rad;
        lastTrackParameter = normalized;

        bool onRoad = Vector2.Distance(playerPosition, PathPoint(normalized, 0f)) <= TrackWidth * 0.59f;
        bool plausibleStep = Mathf.Abs(delta) <= checkpointStep * 3.25f;
        if (!plausibleStep)
        {
            lastCheckpointSector = sector;
            return;
        }

        if (onRoad)
        {
            validatedLapProgress = Mathf.Clamp(validatedLapProgress + delta, 0f, fullLoop);
            if (delta > 0f)
            {
                RegisterForwardLapSectors(lastCheckpointSector, sector);
            }
        }

        bool crossedFinishForward = onRoad
            && delta > 0f
            && previousParameter > fullLoop - checkpointStep * 1.75f
            && normalized < checkpointStep * 1.75f;

        if (crossedFinishForward)
        {
            int passedSectors = CountPassedLapSectors();
            bool validLap = validatedLapProgress >= fullLoop * MinimumValidLapRatio
                && passedSectors >= LapCheckpointCount - 3;

            if (validLap)
            {
                completedLaps++;
                AddCoins(100);
                float lapTime = raceTime - lapStartedAt;
                bestLap = Mathf.Min(bestLap, lapTime);
                lapStartedAt = raceTime;

                if (completedLaps >= RaceLapTarget)
                {
                    raceFinished = true;
                    finishTime = raceTime;
                    lastFinishReward = Mathf.Max(180, 600 - (RacePosition() - 1) * 90);
                    AddCoins(lastFinishReward);
                    player.SetFinished();
                    ResolveStoryRaceAtFinish();
                    ResolveArcadeRaceAtFinish();
                }
            }

            // The first crossing only arms lap one. Every later crossing starts a
            // fresh validated lap, even if the previous attempt was incomplete.
            validatedLapProgress = 0f;
            lapCheckpointMask = 1u;
        }

        lastCheckpointSector = sector;
    }

    private void RegisterForwardLapSectors(int previousSector, int currentSector)
    {
        int sector = previousSector;
        for (int step = 0; step < LapCheckpointCount; step++)
        {
            if (sector == currentSector)
            {
                break;
            }

            sector = (sector + 1) % LapCheckpointCount;
            lapCheckpointMask |= 1u << sector;
        }
    }

    private int CountPassedLapSectors()
    {
        uint mask = lapCheckpointMask;
        int count = 0;
        while (mask != 0u)
        {
            count += (int)(mask & 1u);
            mask >>= 1;
        }

        return count;
    }


    private bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

public void HandlePlayerBroken()
    {
        if (playerWrecked)
        {
            return;
        }

        playerWrecked = true;
        raceFinished = true;
        finishTime = raceTime;
        lastFinishReward = 0;
        player.SetBroken();
        ResolveStoryRaceAsFailed("МАШИНА УНИЧТОЖЕНА  /  МИССИЯ ПРОВАЛЕНА");
    }

    
private void RestartRace()
    {
        ResetExtendedRaceAudio();
        garageOpen = false;
        Time.timeScale = 1f;
        completedLaps = 0;
        lastCheckpointSector = 0;
        lapCheckpointMask = 0u;
        lastTrackParameter = 0f;
        validatedLapProgress = 0f;
        trackProgressInitialized = false;
        raceTime = 0f;
        lapStartedAt = 0f;
        bestLap = float.PositiveInfinity;
        finishTime = 0f;
        lastFinishReward = 0;
        countdown = 3.8f;
        raceStarted = false;
        raceFinished = false;
        playerWrecked = false;
        ResetStoryMissionResult();
        ResetArcadeRaceProgress();
        player.ResetToStart();
        playerDamage.Repair();
        playerWeapon.ResetWeapon();

        for (int i = 0; i < weaponPickups.Count; i++)
        {
            weaponPickups[i].ResetPickup();
        }

        for (int i = 0; i < repairPickups.Count; i++)
        {
            repairPickups[i].ResetPickup();
        }

        for (int i = 0; i < trackObstacles.Count; i++)
        {
            if (trackObstacles[i] != null)
            {
                trackObstacles[i].ResetObstacle();
            }
        }

        for (int i = 0; i < opponents.Count; i++)
        {
            opponents[i].GetComponent<CarDamage>().Repair();
            
opponents[i].ResetRacer();
        }
    }

    public bool IsOnRoad(Vector2 position)
    {
        float t = TrackParameter(position);
        return Vector2.Distance(position, PathPoint(t, 0f)) <= TrackWidth * 0.56f;
    }

    public float DistanceFromTrackCenter(Vector2 position)
    {
        float t = TrackParameter(position);
        return Vector2.Distance(position, PathPoint(t, 0f));
    }

    public Vector2 NearestTrackPoint(Vector2 position)
    {
        return PathPoint(TrackParameter(position), 0f);
    }

    public float NearestTrackRotation(Vector2 position)
    {
        return PathRotation(TrackParameter(position));
    }

    public float PlayerProgress
    {
        get
        {
            return completedLaps + Mathf.Clamp01(validatedLapProgress / (Mathf.PI * 2f));
        }
    }


    public Vector2 PathPoint(float t, float lane)
    {
        Vector2 center = TrackCenter(t);
        Vector2 tangent = TrackDerivative(t).normalized;
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        return center + normal * lane;
    }

    public float PathRotation(float t)
    {
        Vector2 tangent = TrackDerivative(t);
        return Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg - 90f;
    }

    public Vector2 PathDerivative(float t)
    {
        return TrackDerivative(t);
    }

    private Vector2 TrackCenter(float t)
    {
        float normalized = Mathf.Repeat(t, Mathf.PI * 2f) / (Mathf.PI * 2f);
        float scaled = normalized * TrackNodes.Length;
        int p1 = Mathf.FloorToInt(scaled) % TrackNodes.Length;
        float u = scaled - Mathf.Floor(scaled);
        int p0 = WrapNode(p1 - 1);
        int p2 = WrapNode(p1 + 1);
        int p3 = WrapNode(p1 + 2);
        return CatmullRom(TrackNodes[p0], TrackNodes[p1], TrackNodes[p2], TrackNodes[p3], u);
    }

    private Vector2 TrackDerivative(float t)
    {
        float normalized = Mathf.Repeat(t, Mathf.PI * 2f) / (Mathf.PI * 2f);
        float scaled = normalized * TrackNodes.Length;
        int p1 = Mathf.FloorToInt(scaled) % TrackNodes.Length;
        float u = scaled - Mathf.Floor(scaled);
        int p0 = WrapNode(p1 - 1);
        int p2 = WrapNode(p1 + 1);
        int p3 = WrapNode(p1 + 2);
        float parameterScale = TrackNodes.Length / (Mathf.PI * 2f);
        return CatmullRomDerivative(TrackNodes[p0], TrackNodes[p1], TrackNodes[p2], TrackNodes[p3], u) * parameterScale;
    }

    private static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
    {
        float u2 = u * u;
        float u3 = u2 * u;
        return 0.5f * ((2f * p1) + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
    }

    private static Vector2 CatmullRomDerivative(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float u)
    {
        float u2 = u * u;
        return 0.5f * ((-p0 + p2) + 2f * (2f * p0 - 5f * p1 + 4f * p2 - p3) * u + 3f * (-p0 + 3f * p1 - 3f * p2 + p3) * u2);
    }

    private int WrapNode(int index)
    {
        int count = TrackNodes.Length;
        return (index % count + count) % count;
    }

    private float TrackParameterNear(Vector2 position, float referenceParameter)
    {
        const int localSamples = 32;
        const float searchRadius = 0.52f;
        float fullLoop = Mathf.PI * 2f;
        float bestT = referenceParameter;
        float bestDistance = (TrackCenter(bestT) - position).sqrMagnitude;

        for (int i = 0; i <= localSamples; i++)
        {
            float offset = Mathf.Lerp(-searchRadius, searchRadius, i / (float)localSamples);
            float candidateT = referenceParameter + offset;
            float distance = (TrackCenter(candidateT) - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestT = candidateT;
            }
        }

        float step = searchRadius / localSamples;
        for (int iteration = 0; iteration < 6; iteration++)
        {
            float left = bestT - step;
            float right = bestT + step;
            float leftDistance = (TrackCenter(left) - position).sqrMagnitude;
            float rightDistance = (TrackCenter(right) - position).sqrMagnitude;

            if (leftDistance < bestDistance)
            {
                bestDistance = leftDistance;
                bestT = left;
            }

            if (rightDistance < bestDistance)
            {
                bestDistance = rightDistance;
                bestT = right;
            }

            step *= 0.5f;
        }

        // Keep continuity at crossings, but recover globally after a teleport or
        // reset where the previous parameter is no longer near the vehicle.
        float recoveryDistance = TrackWidth * 1.35f;
        if (bestDistance > recoveryDistance * recoveryDistance)
        {
            return TrackParameter(position);
        }

        return Mathf.Repeat(bestT, fullLoop);
    }

    private float TrackParameter(Vector2 position)
    {
        const int searchSamples = 256;
        float fullLoop = Mathf.PI * 2f;
        float bestT = 0f;
        float bestDistance = float.PositiveInfinity;

        for (int i = 0; i < searchSamples; i++)
        {
            float candidateT = i * fullLoop / searchSamples;
            float distance = (TrackCenter(candidateT) - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestT = candidateT;
            }
        }

        float step = fullLoop / searchSamples;
        for (int iteration = 0; iteration < 6; iteration++)
        {
            float left = bestT - step;
            float right = bestT + step;
            float leftDistance = (TrackCenter(left) - position).sqrMagnitude;
            float rightDistance = (TrackCenter(right) - position).sqrMagnitude;

            if (leftDistance < bestDistance)
            {
                bestDistance = leftDistance;
                bestT = left;
            }

            if (rightDistance < bestDistance)
            {
                bestDistance = rightDistance;
                bestT = right;
            }

            step *= 0.5f;
        }

        return Mathf.Repeat(bestT, fullLoop);
    }

    private int RacePosition()
    {
        int position = 1;
        float progress = PlayerProgress;
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null && opponents[i].gameObject.activeSelf && opponents[i].TotalProgress > progress)
            {
                position++;
            }
        }

        return position;
    }

    private string FormatTime(float value)
    {
        int minutes = Mathf.FloorToInt(value / 60f);
        float seconds = value - minutes * 60f;
        return minutes.ToString("00") + ":" + seconds.ToString("00.000");
    }

    private void EnsureGuiStyles()
    {
        if (hudStyle != null && microStyle != null && heroTitleStyle != null)
        {
            return;
        }

        hudStyle = new GUIStyle(GUI.skin.box);
        hudStyle.normal.background = panelTexture;
        hudStyle.padding = new RectOffset(18, 18, 14, 14);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 22;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.normal.textColor = new Color(0.92f, 0.96f, 0.92f);

        smallStyle = new GUIStyle(labelStyle);
        smallStyle.fontSize = 15;
        smallStyle.fontStyle = FontStyle.Normal;
        smallStyle.normal.textColor = new Color(0.48f, 0.86f, 0.88f);

        bigStyle = new GUIStyle(labelStyle);
        bigStyle.fontSize = 58;
        bigStyle.alignment = TextAnchor.MiddleCenter;
        bigStyle.normal.textColor = new Color(1f, 0.82f, 0.22f);

        menuTitleStyle = new GUIStyle(bigStyle);
        menuTitleStyle.fontSize = 72;
        menuTitleStyle.normal.textColor = new Color(0.92f, 0.98f, 0.96f);

        centeredStyle = new GUIStyle(smallStyle);
        centeredStyle.alignment = TextAnchor.MiddleCenter;

        menuButtonStyle = new GUIStyle(GUI.skin.button);
        menuButtonStyle.fontSize = 22;
        menuButtonStyle.fontStyle = FontStyle.Bold;
        menuButtonStyle.normal.background = buttonTexture;
        menuButtonStyle.hover.background = buttonHoverTexture;
        menuButtonStyle.active.background = buttonActiveTexture;
        menuButtonStyle.normal.textColor = new Color(0.88f, 0.96f, 0.94f);
        menuButtonStyle.hover.textColor = Color.white;
        menuButtonStyle.active.textColor = Color.white;

        heroTitleStyle = new GUIStyle(menuTitleStyle);
        heroTitleStyle.fontSize = 88;
        heroTitleStyle.alignment = TextAnchor.MiddleLeft;

        kickerStyle = new GUIStyle(smallStyle);
        kickerStyle.fontSize = 14;
        kickerStyle.fontStyle = FontStyle.Bold;
        kickerStyle.normal.textColor = new Color(0.2f, 0.92f, 0.96f);

        actionTitleStyle = new GUIStyle(labelStyle);
        actionTitleStyle.fontSize = 25;
        actionTitleStyle.alignment = TextAnchor.MiddleLeft;

        actionNumberStyle = new GUIStyle(bigStyle);
        actionNumberStyle.fontSize = 38;
        actionNumberStyle.alignment = TextAnchor.MiddleCenter;
        actionNumberStyle.normal.textColor = new Color(1f, 0.34f, 0.08f);

        microStyle = new GUIStyle(smallStyle);
        microStyle.fontSize = 12;
        microStyle.normal.textColor = new Color(0.38f, 0.62f, 0.64f);
    }

    private float MenuIntroProgress(float delay, float duration)
    {
        float progress = Mathf.Clamp01((Time.unscaledTime - menuAnimationStartedAt - delay) / duration);
        float inverse = 1f - progress;
        return 1f - inverse * inverse * inverse;
    }

private void DrawMenuBackdrop(float screenWidth, float screenHeight)
    {
        Color accent = NeonColors[neonColorIndex];
        Color magenta = new Color(0.95f, 0.06f, 0.46f);
        Color orange = new Color(1f, 0.25f, 0.055f);
        float time = Time.unscaledTime;

        GUI.DrawTexture(new Rect(0f, 0f, screenWidth, screenHeight), overlayTexture);

        const int gradientSteps = 26;
        for (int i = 0; i < gradientSteps; i++)
        {
            float t = i / (float)(gradientSteps - 1);
            Color bandColor = Color.Lerp(
                new Color(0.004f, 0.018f, 0.03f, 0.98f),
                new Color(0.035f, 0.006f, 0.052f, 0.98f),
                t);
            DrawSolidRect(new Rect(0f, screenHeight * t, screenWidth, screenHeight / gradientSteps + 2f), bandColor);
        }

        Color previousGuiColor = GUI.color;
        float sunSize = Mathf.Min(screenWidth, screenHeight) * 0.62f;
        Vector2 sunCenter = new Vector2(
            screenWidth * 0.69f + Mathf.Sin(time * 0.31f) * 13f,
            screenHeight * 0.43f + Mathf.Sin(time * 0.56f) * 9f);
        for (int i = 7; i >= 0; i--)
        {
            float size = sunSize + i * 58f;
            float alpha = 0.018f + (7 - i) * 0.006f;
            GUI.color = new Color(accent.r, accent.g, accent.b, alpha);
            GUI.DrawTexture(new Rect(sunCenter.x - size * 0.5f, sunCenter.y - size * 0.5f, size, size), circleTexture);
        }

        GUI.color = new Color(magenta.r, magenta.g, magenta.b, 0.035f);
        GUI.DrawTexture(new Rect(screenWidth * 0.48f, screenHeight * 0.11f, sunSize * 0.86f, sunSize * 0.86f), circleTexture);
        GUI.color = previousGuiColor;

        float horizon = screenHeight * 0.635f;
        float buildingSpacing = (screenWidth + 180f) / 34f;
        float skylineOffset = Mathf.Repeat(time * 7f, buildingSpacing);
        for (int building = 0; building < 35; building++)
        {
            float width = 34f + (building % 5) * 13f;
            float x = building * buildingSpacing - 60f - skylineOffset;
            float height = 55f + Mathf.Abs(Mathf.Sin(building * 1.73f)) * 155f;
            Color silhouette = building % 3 == 0
                ? new Color(0.018f, 0.025f, 0.047f, 0.98f)
                : new Color(0.009f, 0.018f, 0.032f, 0.98f);
            DrawSolidRect(new Rect(x, horizon - height, width, height + 8f), silhouette);

            for (int floor = 0; floor < 5; floor++)
            {
                if ((building + floor) % 3 == 0)
                {
                    float windowY = horizon - 20f - floor * 26f;
                    if (windowY > horizon - height + 12f)
                    {
                        Color windowColor = building % 4 == 0
                            ? new Color(orange.r, orange.g, orange.b, 0.34f)
                            : new Color(accent.r, accent.g, accent.b, 0.28f);
                        DrawSolidRect(new Rect(x + 9f, windowY, Mathf.Max(5f, width - 20f), 3f), windowColor);
                    }
                }
            }
        }

        float roadCenter = screenWidth * 0.68f;
        const int roadSteps = 72;
        for (int i = 0; i < roadSteps; i++)
        {
            float t = i / (float)(roadSteps - 1);
            float y = Mathf.Lerp(horizon, screenHeight + 4f, t);
            float roadWidth = Mathf.Lerp(90f, screenWidth * 0.9f, t * t);
            Color roadColor = Color.Lerp(
                new Color(0.008f, 0.014f, 0.022f, 0.92f),
                new Color(0.015f, 0.008f, 0.021f, 0.98f),
                t);
            DrawSolidRect(new Rect(roadCenter - roadWidth * 0.5f, y, roadWidth, (screenHeight - horizon) / roadSteps + 2f), roadColor);
        }

        for (int lane = -3; lane <= 3; lane++)
        {
            float endX = roadCenter + lane * screenWidth * 0.17f;
            float angle = Mathf.Atan2(screenHeight - horizon, endX - roadCenter) * Mathf.Rad2Deg - 90f;
            float length = Vector2.Distance(new Vector2(roadCenter, horizon), new Vector2(endX, screenHeight));
            DrawRotatedRect(
                new Rect(roadCenter - 1f, horizon, 2f, length),
                new Color(accent.r, accent.g, accent.b, lane == 0 ? 0.18f : 0.085f),
                -angle);
        }

        for (int row = 1; row < 11; row++)
        {
            float t = row / 10f;
            float y = Mathf.Lerp(horizon, screenHeight, t * t);
            float width = Mathf.Lerp(100f, screenWidth * 0.86f, t * t);
            DrawSolidRect(new Rect(roadCenter - width * 0.5f, y, width, row == 10 ? 2f : 1f), new Color(accent.r, accent.g, accent.b, 0.11f - t * 0.045f));
        }

        DrawMenuSpeedTrails(screenWidth, screenHeight, horizon, roadCenter, accent, magenta, orange, time);

        DrawRotatedRect(new Rect(-screenWidth * 0.06f, screenHeight * 0.26f, screenWidth * 0.72f, 2f), new Color(magenta.r, magenta.g, magenta.b, 0.28f), -8f);
        DrawRotatedRect(new Rect(screenWidth * 0.42f, screenHeight * 0.72f, screenWidth * 0.68f, 3f), new Color(orange.r, orange.g, orange.b, 0.24f), -8f);

        float sweepY = Mathf.Repeat(time * 52f, screenHeight + 100f) - 50f;
        DrawSolidRect(new Rect(0f, sweepY, screenWidth, 1f), new Color(accent.r, accent.g, accent.b, 0.07f));

        for (int i = 0; i < 9; i++)
        {
            float alpha = 0.028f + i * 0.012f;
            DrawSolidRect(new Rect(i * 10f, 0f, 11f, screenHeight), new Color(0f, 0f, 0f, alpha));
            DrawSolidRect(new Rect(screenWidth - 11f - i * 10f, 0f, 11f, screenHeight), new Color(0f, 0f, 0f, alpha));
        }

        DrawSolidRect(new Rect(0f, 0f, screenWidth, 4f), new Color(accent.r, accent.g, accent.b, 0.7f));
        DrawSolidRect(new Rect(0f, screenHeight - 4f, screenWidth, 4f), new Color(magenta.r, magenta.g, magenta.b, 0.72f));
    }

    private void DrawMenuSpeedTrails(
        float screenWidth,
        float screenHeight,
        float horizon,
        float roadCenter,
        Color accent,
        Color magenta,
        Color orange,
        float time)
    {
        for (int i = 0; i < 14; i++)
        {
            float phase = Mathf.Repeat(time * (0.15f + i % 3 * 0.018f) + i * 0.173f, 1f);
            float depth = phase * phase;
            float y = Mathf.Lerp(horizon + 5f, screenHeight + 34f, depth);
            float lane = (i % 7 - 3f) / 3f;
            float x = roadCenter + lane * screenWidth * 0.29f * depth;
            float length = Mathf.Lerp(5f, 58f, depth);
            float thickness = Mathf.Lerp(1f, 4f, depth);
            Color trailColor = i % 3 == 0 ? orange : i % 3 == 1 ? magenta : accent;
            float alpha = Mathf.Sin(phase * Mathf.PI) * Mathf.Lerp(0.08f, 0.46f, depth);

            DrawRotatedRect(
                new Rect(x - thickness * 0.5f, y - length, thickness, length),
                new Color(trailColor.r, trailColor.g, trailColor.b, alpha * 0.35f),
                -lane * 16f);
            DrawSolidRect(
                new Rect(x - thickness * 0.5f, y - thickness * 0.5f, thickness, thickness),
                new Color(trailColor.r, trailColor.g, trailColor.b, alpha));
        }

        for (int i = 0; i < 8; i++)
        {
            float phase = Mathf.Repeat(time * 0.11f + i * 0.137f, 1f);
            float x = Mathf.Lerp(-30f, screenWidth + 30f, phase);
            float y = horizon - 42f - i * 17f + Mathf.Sin(time * 0.9f + i) * 8f;
            Color signal = i % 2 == 0 ? accent : magenta;
            DrawSolidRect(new Rect(x, y, 18f + i % 3 * 8f, 2f), new Color(signal.r, signal.g, signal.b, 0.11f));
        }
    }

private void DrawMainMenu(float screenWidth, float screenHeight)
    {
        DrawMenuBackdrop(screenWidth, screenHeight);

        float contentWidth = Mathf.Min(1740f, screenWidth - 80f);
        float layoutContentX = (screenWidth - contentWidth) * 0.5f;
        float topY = Mathf.Max(30f, screenHeight * 0.038f);
        float stageY = topY + 112f;
        float stageHeight = screenHeight - stageY - 72f;

        float leftWidth = Mathf.Min(470f, contentWidth * 0.31f);
        float eventWidth = Mathf.Min(398f, contentWidth * 0.275f);
        float leftIntro = MenuIntroProgress(0f, 0.58f);
        float showcaseIntro = MenuIntroProgress(0.12f, 0.66f);
        float eventIntro = MenuIntroProgress(0.24f, 0.7f);
        float contentX = layoutContentX - (1f - leftIntro) * 82f;
        float showcaseX = layoutContentX + leftWidth + 34f + (1f - showcaseIntro) * 96f;
        float showcaseWidth = contentWidth - leftWidth - eventWidth - 68f;
        float eventX = layoutContentX + contentWidth - eventWidth + (1f - eventIntro) * 124f;

        Color accent = NeonColors[neonColorIndex];
        Color magenta = new Color(0.95f, 0.06f, 0.46f);
        Color orange = new Color(1f, 0.25f, 0.055f);
        float menuTime = Time.unscaledTime;

        GUI.Label(new Rect(contentX, topY, 520f, 22f), "NIGHT LEAGUE   /   LIVE CHANNEL 01", kickerStyle);
        DrawSolidRect(new Rect(contentX + 332f, topY + 12f, 88f, 1f), new Color(accent.r, accent.g, accent.b, 0.55f));

        Rect accountRect = new Rect(eventX, topY - 2f, eventWidth, 48f);
        DrawSolidRect(new Rect(accountRect.x + 5f, accountRect.y + 6f, accountRect.width, accountRect.height), new Color(0f, 0f, 0f, 0.42f));
        DrawSolidRect(accountRect, new Color(0.006f, 0.029f, 0.045f, 0.92f));
        DrawSolidRect(new Rect(accountRect.x, accountRect.y, 4f, accountRect.height), accent);
        GUI.Label(new Rect(accountRect.x + 16f, accountRect.y + 5f, 170f, 18f), "DRIVER 01   /   ONLINE", microStyle);
        GUI.Label(new Rect(accountRect.x + 16f, accountRect.y + 23f, 200f, 20f), coins + " РњРћРќР•Рў", kickerStyle);
        float onlinePulse = 0.58f + Mathf.Sin(menuTime * 4.8f) * 0.34f;
        DrawSolidRect(new Rect(accountRect.x + accountRect.width - 32f, accountRect.y + 14f, 17f, 17f), new Color(accent.r, accent.g, accent.b, onlinePulse * 0.13f));
        DrawSolidRect(new Rect(accountRect.x + accountRect.width - 28f, accountRect.y + 18f, 9f, 9f), new Color(accent.r, accent.g, accent.b, onlinePulse));

        Color previousGuiColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.78f);
        GUI.Label(new Rect(contentX + 7f, topY + 48f, 560f, 86f), "NEON", heroTitleStyle);
        GUI.Label(new Rect(contentX + 7f, topY + 121f, 600f, 86f), "CIRCUIT", heroTitleStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(contentX, topY + 40f, 560f, 86f), "NEON", heroTitleStyle);
        GUI.color = accent;
        GUI.Label(new Rect(contentX, topY + 113f, 600f, 86f), "CIRCUIT", heroTitleStyle);
        GUI.color = previousGuiColor;

        float glitchPhase = Mathf.Repeat(menuTime, 5.2f);
        if (glitchPhase < 0.16f)
        {
            float glitch = Mathf.Sin(glitchPhase / 0.16f * Mathf.PI);
            float offset = 3f + glitch * 7f;
            GUI.color = new Color(magenta.r, magenta.g, magenta.b, 0.34f * glitch);
            GUI.Label(new Rect(contentX - offset, topY + 40f, 560f, 86f), "NEON", heroTitleStyle);
            GUI.color = new Color(accent.r, accent.g, accent.b, 0.28f * glitch);
            GUI.Label(new Rect(contentX + offset, topY + 113f, 600f, 86f), "CIRCUIT", heroTitleStyle);
            GUI.color = previousGuiColor;

            float sliceY = topY + 76f + Mathf.Repeat(menuTime * 83f, 96f);
            DrawSolidRect(new Rect(contentX - 8f, sliceY, leftWidth + 26f, 3f), new Color(accent.r, accent.g, accent.b, 0.48f * glitch));
        }

        float logoScanner = Mathf.Repeat(menuTime * 0.24f, 1f);
        float logoScannerX = Mathf.Lerp(contentX + 4f, contentX + leftWidth - 18f, logoScanner);
        DrawSolidRect(new Rect(logoScannerX, topY + 238f, 34f, 4f), new Color(accent.r, accent.g, accent.b, 0.22f));

        GUI.Label(new Rect(contentX + 4f, topY + 207f, leftWidth - 8f, 24f), "РЎРљРћР РћРЎРўР¬. РќР•РћРќ. РќРРљРђРљРРҐ РџР РђР’РР›.", smallStyle);
        DrawSolidRect(new Rect(contentX + 4f, topY + 241f, leftWidth - 16f, 2f), new Color(accent.r, accent.g, accent.b, 0.36f));
        DrawSolidRect(new Rect(contentX + 4f, topY + 241f, leftWidth * 0.26f, 2f), magenta);

        float actionY = stageY + 270f;
        if (DrawMenuAction(new Rect(contentX, actionY, leftWidth - 6f, 96f), "01", "РќРђР§РђРўР¬ Р“РћРќРљРЈ", ActiveTrack.ShortName + "   /   3 РљР РЈР“Рђ", false))
        {
            StartRaceFromMenu();
        }

        if (DrawMenuAction(new Rect(contentX, actionY + 110f, leftWidth - 6f, 82f), "02", "Р“РђР РђР–", "РњРђРЁРРќР«   /   РўР®РќРРќР“   /   РЎРўРР›Р¬", false))
        {
            garageOpen = true;
            garageCarIndex = selectedCarIndex;
        }

        if (DrawMenuAction(new Rect(contentX, actionY + 206f, leftWidth - 6f, 66f), "03", "Р’Р«РҐРћР”", "РџРћРљРРќРЈРўР¬ РР“Р РЈ", true))
        {
            QuitGame();
        }

        GUI.Label(new Rect(contentX + 2f, actionY + 295f, leftWidth - 10f, 20f), "ENTER  SELECT     G  GARAGE     ESC  BACK", microStyle);

        Rect showcaseRect = new Rect(showcaseX, stageY + 25f, showcaseWidth, stageHeight - 78f);
        DrawSolidRect(new Rect(showcaseRect.x + 13f, showcaseRect.y + 15f, showcaseRect.width, showcaseRect.height), new Color(0f, 0f, 0f, 0.35f));
        DrawSolidRect(showcaseRect, new Color(0.004f, 0.016f, 0.027f, 0.46f));
        DrawSolidRect(new Rect(showcaseRect.x, showcaseRect.y, showcaseRect.width, 1f), new Color(accent.r, accent.g, accent.b, 0.32f));
        DrawSolidRect(new Rect(showcaseRect.x, showcaseRect.y, 1f, showcaseRect.height), new Color(accent.r, accent.g, accent.b, 0.18f));
        float showcaseScanY = Mathf.Lerp(showcaseRect.y + 4f, showcaseRect.yMax - 4f, Mathf.Repeat(menuTime * 0.095f, 1f));
        DrawSolidRect(new Rect(showcaseRect.x + 4f, showcaseScanY, showcaseRect.width - 8f, 2f), new Color(accent.r, accent.g, accent.b, 0.07f));

        GUI.Label(new Rect(showcaseRect.x + 22f, showcaseRect.y + 17f, 250f, 20f), "CURRENT VEHICLE   /   LIVE", kickerStyle);
        DrawSolidRect(new Rect(showcaseRect.x + showcaseRect.width - 102f, showcaseRect.y + 24f, 72f, 2f), orange);

        Rect previewRect = new Rect(
            showcaseRect.x + 18f,
            showcaseRect.y + 52f,
            showcaseRect.width - 36f,
            showcaseRect.height - 168f);
        DrawCarPreview(previewRect, selectedCarIndex);

        GUI.Label(new Rect(showcaseRect.x + 20f, showcaseRect.y + showcaseRect.height - 110f, showcaseRect.width - 40f, 48f), CarNames[selectedCarIndex], bigStyle);
        GUI.Label(new Rect(showcaseRect.x + 20f, showcaseRect.y + showcaseRect.height - 69f, showcaseRect.width - 40f, 22f), CarClasses[selectedCarIndex], centeredStyle);

        float chipY = showcaseRect.y + showcaseRect.height - 38f;
        float chipGap = 8f;
        float chipWidth = (showcaseRect.width - 40f - chipGap * 2f) / 3f;
        string[] chipLabels = { "SPEED", "HANDLING", "ARMOR" };
        int[] chipValues =
        {
            Mathf.RoundToInt(CarTopSpeed[selectedCarIndex] * 100f),
            Mathf.RoundToInt(CarHandling[selectedCarIndex] * 100f),
            Mathf.RoundToInt((2f - CarDamage[selectedCarIndex]) * 100f)
        };

        for (int i = 0; i < 3; i++)
        {
            Rect chip = new Rect(showcaseRect.x + 20f + i * (chipWidth + chipGap), chipY, chipWidth, 30f);
            DrawSolidRect(chip, new Color(0.006f, 0.035f, 0.05f, 0.94f));
            DrawSolidRect(new Rect(chip.x, chip.y, 3f, chip.height), i == 2 ? orange : accent);
            GUI.Label(new Rect(chip.x + 10f, chip.y + 4f, chip.width - 54f, 20f), chipLabels[i], microStyle);
            GUI.Label(new Rect(chip.x + chip.width - 46f, chip.y + 3f, 40f, 21f), chipValues[i].ToString(), kickerStyle);
        }

        Rect eventRect = new Rect(eventX, stageY, eventWidth, stageHeight);
        DrawPanelFrame(eventRect, ActiveTrack.AccentColor);
        float eventScanY = Mathf.Lerp(eventRect.y + 8f, eventRect.yMax - 8f, Mathf.Repeat(menuTime * 0.082f + 0.36f, 1f));
        DrawSolidRect(new Rect(eventRect.x + 8f, eventScanY, eventRect.width - 16f, 2f), new Color(ActiveTrack.AccentColor.r, ActiveTrack.AccentColor.g, ActiveTrack.AccentColor.b, 0.09f));
        GUI.Label(new Rect(eventRect.x + 24f, eventRect.y + 21f, eventRect.width - 48f, 20f), "NEXT EVENT   /   NIGHT RUN", kickerStyle);
        GUI.Label(new Rect(eventRect.x + 23f, eventRect.y + 51f, eventRect.width - 46f, 34f), ActiveTrack.ShortName, labelStyle);
        GUI.Label(new Rect(eventRect.x + 25f, eventRect.y + 87f, eventRect.width - 50f, 22f), ActiveTrack.Description, smallStyle);

        DrawSolidRect(new Rect(eventRect.x + 24f, eventRect.y + 122f, eventRect.width - 48f, 1f), new Color(accent.r, accent.g, accent.b, 0.34f));
        GUI.Label(new Rect(eventRect.x + 24f, eventRect.y + 135f, eventRect.width - 48f, 18f), "Р’Р«Р‘РћР  РўР РђРЎРЎР«", microStyle);

        float trackY = eventRect.y + 164f;
        if (DrawTrackChoice(new Rect(eventRect.x + 23f, trackY, eventRect.width - 46f, 72f), 0))
        {
            SelectTrack(0);
        }

        if (DrawTrackChoice(new Rect(eventRect.x + 23f, trackY + 86f, eventRect.width - 46f, 72f), 1))
        {
            SelectTrack(1);
        }

        float raceInfoY = trackY + 183f;
        Rect lapsRect = new Rect(eventRect.x + 23f, raceInfoY, (eventRect.width - 58f) * 0.5f, 68f);
        Rect gridRect = new Rect(lapsRect.xMax + 12f, raceInfoY, lapsRect.width, 68f);
        DrawSolidRect(lapsRect, new Color(0.008f, 0.034f, 0.048f, 0.92f));
        DrawSolidRect(gridRect, new Color(0.008f, 0.034f, 0.048f, 0.92f));
        DrawSolidRect(new Rect(lapsRect.x, lapsRect.y, lapsRect.width, 2f), accent);
        DrawSolidRect(new Rect(gridRect.x, gridRect.y, gridRect.width, 2f), orange);
        GUI.Label(new Rect(lapsRect.x + 12f, lapsRect.y + 10f, lapsRect.width - 24f, 18f), "Р”РРЎРўРђРќР¦РРЇ", microStyle);
        GUI.Label(new Rect(lapsRect.x + 12f, lapsRect.y + 30f, lapsRect.width - 24f, 28f), "3 РљР РЈР“Рђ", labelStyle);
        GUI.Label(new Rect(gridRect.x + 12f, gridRect.y + 10f, gridRect.width - 24f, 18f), "РЎРўРђР РўРћР’РђРЇ РЎР•РўРљРђ", microStyle);
        GUI.Label(new Rect(gridRect.x + 12f, gridRect.y + 30f, gridRect.width - 24f, 28f), (opponents.Count + 1) + " РњРђРЁРРќ", labelStyle);

        float buildY = raceInfoY + 90f;
        GUI.Label(new Rect(eventRect.x + 24f, buildY, eventRect.width - 48f, 18f), "ACTIVE PERFORMANCE KIT", microStyle);
        DrawSolidRect(new Rect(eventRect.x + 24f, buildY + 27f, 38f, 32f), PaintColors[paintColorIndex]);
        DrawSolidRect(new Rect(eventRect.x + 72f, buildY + 27f, 38f, 32f), accent);
        GUI.Label(new Rect(eventRect.x + 124f, buildY + 25f, eventRect.width - 148f, 22f), engineLevel + "." + handlingLevel + "." + armorLevel, labelStyle);
        GUI.Label(new Rect(eventRect.x + 125f, buildY + 49f, eventRect.width - 150f, 18f), "ENGINE / GRIP / ARMOR", microStyle);

        float readyY = eventRect.y + eventRect.height - 72f;
        DrawSolidRect(new Rect(eventRect.x + 23f, readyY, eventRect.width - 46f, 48f), new Color(accent.r * 0.16f, accent.g * 0.16f, accent.b * 0.16f, 0.95f));
        DrawSolidRect(new Rect(eventRect.x + 23f, readyY, 6f, 48f), accent);
        float readySweep = Mathf.Repeat(menuTime * 0.38f, 1f);
        float readySweepX = Mathf.Lerp(eventRect.x + 32f, eventRect.xMax - 36f, readySweep);
        DrawSolidRect(new Rect(readySweepX, readyY + 3f, 3f, 42f), new Color(accent.r, accent.g, accent.b, 0.2f));
        GUI.Label(new Rect(eventRect.x + 42f, readyY + 7f, eventRect.width - 105f, 20f), "GRID READY", kickerStyle);
        GUI.Label(new Rect(eventRect.x + 42f, readyY + 25f, eventRect.width - 105f, 18f), "PRESS ENTER TO DEPLOY", microStyle);
        DrawSolidRect(new Rect(eventRect.x + eventRect.width - 54f, readyY + 19f, 10f, 10f), accent);

        GUI.Label(new Rect(contentX, screenHeight - 49f, contentWidth, 18f), "NEON CIRCUIT   вЂў   " + ActiveTrack.ShortName + "   вЂў   VEHICLE " + (selectedCarIndex + 1).ToString("00") + "   вЂў   SYSTEM NOMINAL", microStyle);
        DrawSolidRect(new Rect(contentX, screenHeight - 25f, contentWidth, 2f), new Color(accent.r, accent.g, accent.b, 0.42f));
        DrawSolidRect(new Rect(contentX, screenHeight - 25f, contentWidth * 0.2f, 2f), magenta);
    }


private bool DrawTrackChoice(Rect rect, int trackIndex)
    {
        RaceTrackDefinition track = RaceTrackCatalog.Get(trackIndex);
        bool selected = trackIndex == selectedTrackIndex;
        bool hovered = rect.Contains(Event.current.mousePosition);
        Color accent = track.AccentColor;

        Color background = selected
            ? new Color(accent.r * 0.18f, accent.g * 0.18f, accent.b * 0.18f, 0.98f)
            : hovered
                ? new Color(accent.r * 0.105f, accent.g * 0.105f, accent.b * 0.105f, 0.97f)
                : new Color(0.008f, 0.027f, 0.045f, 0.94f);

        DrawSolidRect(new Rect(rect.x + 7f, rect.y + 8f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.45f));
        if (selected || hovered)
        {
            DrawSolidRect(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), new Color(accent.r, accent.g, accent.b, selected ? 0.11f : 0.055f));
        }

        DrawSolidRect(rect, background);
        DrawSolidRect(new Rect(rect.x, rect.y, selected ? 7f : 3f, rect.height), accent);
        DrawSolidRect(new Rect(rect.x + 3f, rect.y, rect.width - 3f, 1f), new Color(accent.r, accent.g, accent.b, selected ? 0.78f : 0.22f));

        string indexText = (trackIndex + 1).ToString("00");
        GUI.Label(new Rect(rect.x + 17f, rect.y + 11f, 46f, 26f), indexText, actionNumberStyle);
        GUI.Label(new Rect(rect.x + 68f, rect.y + 10f, rect.width - 112f, 27f), track.ShortName, labelStyle);
        GUI.Label(new Rect(rect.x + 70f, rect.y + 39f, rect.width - 100f, 20f), selected ? "РўР РђРЎРЎРђ Р’Р«Р‘Р РђРќРђ" : track.Description, microStyle);

        if (selected)
        {
            float pulse = 0.72f + Mathf.Sin(Time.unscaledTime * 2.8f) * 0.18f;
            DrawSolidRect(new Rect(rect.x + rect.width - 31f, rect.y + 16f, 10f, 10f), new Color(accent.r, accent.g, accent.b, pulse));
            DrawSolidRect(new Rect(rect.x + rect.width - 48f, rect.y + rect.height - 3f, 48f, 3f), accent);
        }
        else
        {
            GUI.Label(new Rect(rect.x + rect.width - 35f, rect.y + 22f, 22f, 24f), ">", labelStyle);
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private void SelectTrack(int trackIndex)
    {
        int clamped = Mathf.Clamp(trackIndex, 0, RaceTrackCatalog.Count - 1);
        if (clamped == selectedTrackIndex)
        {
            return;
        }

        selectedTrackIndex = clamped;
        PlayerPrefs.SetInt(TrackKey, selectedTrackIndex);
        CacheMinimapTrack();
        CreateMinimapTrackTexture();
        menuAnimationStartedAt = Time.unscaledTime;
    }

private bool DrawMenuAction(Rect rect, string number, string title, string subtitle, bool danger)
    {
        bool hovered = rect.Contains(Event.current.mousePosition);
        bool primary = number == "01";
        Color accent = danger ? new Color(1f, 0.16f, 0.22f) : NeonColors[neonColorIndex];
        Color idle = danger
            ? new Color(0.075f, 0.015f, 0.027f, 0.9f)
            : new Color(0.008f, 0.035f, 0.052f, 0.93f);
        Color active = new Color(accent.r * 0.21f, accent.g * 0.21f, accent.b * 0.21f, 0.985f);
        Color background = primary || hovered ? active : idle;
        float hoverSlide = hovered ? 6f + Mathf.Sin(Time.unscaledTime * 7f) * 1.2f : 0f;
        Rect visualRect = new Rect(rect.x + hoverSlide, rect.y, rect.width, rect.height);

        float glow = primary
            ? 0.085f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.025f
            : hovered ? 0.07f : 0f;

        if (glow > 0f)
        {
            DrawSolidRect(new Rect(visualRect.x - 5f, visualRect.y - 5f, visualRect.width + 10f, visualRect.height + 10f), new Color(accent.r, accent.g, accent.b, glow));
        }

        DrawSolidRect(new Rect(visualRect.x + 10f, visualRect.y + 10f, visualRect.width, visualRect.height), new Color(0f, 0f, 0f, 0.5f));
        DrawSolidRect(visualRect, background);
        DrawSolidRect(new Rect(visualRect.x, visualRect.y, primary || hovered ? 8f : 3f, visualRect.height), accent);
        DrawSolidRect(new Rect(visualRect.x, visualRect.y, visualRect.width, primary ? 2f : 1f), new Color(accent.r, accent.g, accent.b, primary ? 0.88f : hovered ? 0.65f : 0.2f));

        float numberWidth = primary ? 82f : 68f;
        DrawSolidRect(new Rect(visualRect.x + numberWidth, visualRect.y + 16f, 1f, visualRect.height - 32f), new Color(accent.r, accent.g, accent.b, 0.24f));
        GUI.Label(new Rect(visualRect.x + 12f, visualRect.y + (visualRect.height - 58f) * 0.5f, numberWidth - 17f, 58f), number, actionNumberStyle);
        GUI.Label(new Rect(visualRect.x + numberWidth + 22f, visualRect.y + (primary ? 14f : 10f), visualRect.width - numberWidth - 88f, 40f), title, actionTitleStyle);

        if (visualRect.height >= 72f)
        {
            GUI.Label(new Rect(visualRect.x + numberWidth + 24f, visualRect.y + visualRect.height - 28f, visualRect.width - numberWidth - 95f, 19f), subtitle, microStyle);
        }

        if (primary)
        {
            DrawSolidRect(new Rect(visualRect.x + visualRect.width - 89f, visualRect.y + 16f, 62f, 22f), new Color(accent.r, accent.g, accent.b, 0.17f));
            GUI.Label(new Rect(visualRect.x + visualRect.width - 83f, visualRect.y + 16f, 52f, 20f), "ENTER", microStyle);
            DrawSolidRect(new Rect(visualRect.x + 8f, visualRect.y + visualRect.height - 4f, visualRect.width - 8f, 4f), new Color(accent.r, accent.g, accent.b, 0.72f));
        }

        GUI.Label(new Rect(visualRect.x + visualRect.width - 42f, visualRect.y + (visualRect.height - 28f) * 0.5f, 28f, 28f), hovered ? ">>" : ">", labelStyle);

        if (hovered && !primary)
        {
            DrawSolidRect(new Rect(visualRect.x + 4f, visualRect.y + visualRect.height - 3f, visualRect.width - 4f, 3f), new Color(accent.r, accent.g, accent.b, 0.62f));
        }

        if (hovered)
        {
            int buttonIndex = number.Length > 1 ? number[1] - '0' : 0;
            float scan = Mathf.Repeat(Time.unscaledTime * 0.9f + buttonIndex * 0.17f, 1f);
            float scannerX = Mathf.Lerp(visualRect.x + 12f, visualRect.xMax - 12f, scan);
            DrawSolidRect(new Rect(scannerX, visualRect.y + 4f, 2f, visualRect.height - 8f), new Color(accent.r, accent.g, accent.b, 0.2f));
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

private void DrawPanelFrame(Rect rect, Color accent)
    {
        DrawSolidRect(new Rect(rect.x + 14f, rect.y + 16f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.52f));
        DrawSolidRect(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f), new Color(accent.r, accent.g, accent.b, 0.055f));
        DrawSolidRect(rect, new Color(0.006f, 0.027f, 0.034f, 0.965f));

        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(accent.r, accent.g, accent.b, 0.72f));
        DrawSolidRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), new Color(accent.r, accent.g, accent.b, 0.18f));
        DrawSolidRect(new Rect(rect.x, rect.y, 1f, rect.height), new Color(accent.r, accent.g, accent.b, 0.34f));
        DrawSolidRect(new Rect(rect.x + rect.width - 1f, rect.y, 1f, rect.height), new Color(accent.r, accent.g, accent.b, 0.16f));

        DrawSolidRect(new Rect(rect.x, rect.y, 7f, 74f), accent);
        DrawSolidRect(new Rect(rect.x, rect.y, 74f, 4f), accent);
        DrawSolidRect(new Rect(rect.x + rect.width - 58f, rect.y + rect.height - 4f, 58f, 4f), new Color(1f, 0.28f, 0.06f));
        DrawSolidRect(new Rect(rect.x + rect.width - 4f, rect.y + rect.height - 58f, 4f, 58f), new Color(1f, 0.28f, 0.06f));

        for (int i = 0; i < 5; i++)
        {
            DrawRotatedRect(
                new Rect(rect.x + rect.width - 148f + i * 18f, rect.y + 16f, 10f, 2f),
                new Color(accent.r, accent.g, accent.b, 0.26f + i * 0.08f),
                -45f);
        }
    }

    private void DrawStatMeter(Rect rect, string title, float value)
    {
        GUI.Label(new Rect(rect.x, rect.y, 145f, 20f), title, microStyle);
        float normalized = Mathf.InverseLerp(0.82f, 1.16f, value);
        float startX = rect.x + 145f;
        float blockWidth = Mathf.Max(10f, (rect.width - 150f) / 8f - 4f);
        for (int i = 0; i < 8; i++)
        {
            float filled = (i + 1f) / 8f;
            Color color = filled <= normalized
                ? Color.Lerp(new Color(0.08f, 0.62f, 0.68f), new Color(1f, 0.34f, 0.08f), filled)
                : new Color(0.08f, 0.16f, 0.17f);
            DrawSolidRect(new Rect(startX + i * (blockWidth + 4f), rect.y + 3f, blockWidth, 14f), color);
        }
    }

    private void DrawRotatedRect(Rect rect, Color color, float angle)
    {
        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, rect.center);
        DrawSolidRect(rect, color);
        GUI.matrix = previous;
    }

    private void DrawMenuGarage(float screenWidth, float screenHeight)
    {
        DrawMenuBackdrop(screenWidth, screenHeight);
        float panelWidth = Mathf.Min(1040f, screenWidth - 48f);
        float panelHeight = Mathf.Min(760f, screenHeight - 40f);
        float panelX = (screenWidth - panelWidth) * 0.5f;
        float panelY = (screenHeight - panelHeight) * 0.5f;
        GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), GUIContent.none, hudStyle);

        GUI.Label(new Rect(panelX + 28f, panelY + 16f, 380f, 58f), "Р“РђР РђР–", bigStyle);
        GUI.Label(new Rect(panelX + 390f, panelY + 31f, 310f, 30f), "Р‘РђР›РђРќРЎ  " + coins + " РњРћРќР•Рў", labelStyle);
        if (GUI.Button(new Rect(panelX + panelWidth - 190f, panelY + 20f, 160f, 48f), "РќРђР—РђР”", menuButtonStyle))
        {
            garageOpen = false;
            menuAnimationStartedAt = Time.unscaledTime;
        }

        float leftX = panelX + 28f;
        float topY = panelY + 85f;
        float leftWidth = panelWidth * 0.46f;
        GUI.Box(new Rect(leftX, topY, leftWidth, panelHeight - 112f), GUIContent.none, hudStyle);

        if (GUI.Button(new Rect(leftX + 18f, topY + 16f, 52f, 44f), "<", menuButtonStyle))
        {
            garageCarIndex = (garageCarIndex + CarNames.Length - 1) % CarNames.Length;
        }

        if (GUI.Button(new Rect(leftX + leftWidth - 70f, topY + 16f, 52f, 44f), ">", menuButtonStyle))
        {
            garageCarIndex = (garageCarIndex + 1) % CarNames.Length;
        }

        GUI.Label(new Rect(leftX + 75f, topY + 12f, leftWidth - 150f, 34f), CarNames[garageCarIndex], labelStyle);
        GUI.Label(new Rect(leftX + 75f, topY + 43f, leftWidth - 150f, 24f), CarClasses[garageCarIndex], centeredStyle);
        DrawCarPreview(new Rect(leftX + 55f, topY + 76f, leftWidth - 110f, 235f), garageCarIndex);

        float statsY = topY + 316f;
        GUI.Label(new Rect(leftX + 24f, statsY, leftWidth - 48f, 24f), "РЎРљРћР РћРЎРўР¬    " + StatBar(CarTopSpeed[garageCarIndex]), smallStyle);
        GUI.Label(new Rect(leftX + 24f, statsY + 27f, leftWidth - 48f, 24f), "РЈРџР РђР’Р›Р•РќРР•  " + StatBar(CarHandling[garageCarIndex]), smallStyle);
        GUI.Label(new Rect(leftX + 24f, statsY + 54f, leftWidth - 48f, 24f), "РџР РћР§РќРћРЎРўР¬   " + StatBar(2f - CarDamage[garageCarIndex]), smallStyle);

        bool owned = IsCarOwned(garageCarIndex);
        string carButton = selectedCarIndex == garageCarIndex
            ? "Р’Р«Р‘Р РђРќРђ"
            : owned ? "Р’Р«Р‘Р РђРўР¬" : "РљРЈРџРРўР¬  " + CarPrices[garageCarIndex];
        GUI.enabled = selectedCarIndex != garageCarIndex;
        if (GUI.Button(new Rect(leftX + 24f, statsY + 92f, leftWidth - 48f, 52f), carButton, menuButtonStyle))
        {
            TryBuyOrSelectCar();
        }
        GUI.enabled = true;

        GUI.Label(new Rect(leftX + 24f, statsY + 158f, leftWidth - 48f, 28f), "Р¦Р’Р•Рў РљРЈР—РћР’Рђ", labelStyle);
        DrawColorChoices(new Rect(leftX + 24f, statsY + 192f, leftWidth - 48f, 34f), PaintColors, paintColorIndex, true);
        GUI.Label(new Rect(leftX + 24f, statsY + 236f, leftWidth - 48f, 28f), "РќР•РћРќРћР’РђРЇ РџРћР”РЎР’Р•РўРљРђ", labelStyle);
        DrawColorChoices(new Rect(leftX + 24f, statsY + 270f, leftWidth - 48f, 34f), NeonColors, neonColorIndex, false);

        float rightX = leftX + leftWidth + 24f;
        float rightWidth = panelX + panelWidth - 28f - rightX;
        GUI.Box(new Rect(rightX, topY, rightWidth, panelHeight - 112f), GUIContent.none, hudStyle);
        GUI.Label(new Rect(rightX + 24f, topY + 18f, rightWidth - 48f, 34f), "РЈР›РЈР§РЁР•РќРРЇ", labelStyle);
        DrawUpgradeCard(new Rect(rightX + 22f, topY + 68f, rightWidth - 44f, 142f), 0, "Р”Р’РР“РђРўР•Р›Р¬", "+10% СЂР°Р·РіРѕРЅ  /  +6% СЃРєРѕСЂРѕСЃС‚СЊ");
        DrawUpgradeCard(new Rect(rightX + 22f, topY + 226f, rightWidth - 44f, 142f), 1, "РЈРџР РђР’Р›Р•РќРР•", "+8% РїРѕРІРѕСЂРѕС‚ Рё СЃС†РµРїР»РµРЅРёРµ");
        DrawUpgradeCard(new Rect(rightX + 22f, topY + 384f, rightWidth - 44f, 142f), 2, "РљРћР РџРЈРЎ", "РјРµРЅСЊС€Рµ СѓСЂРѕРЅР° РїСЂРё СЃС‚РѕР»РєРЅРѕРІРµРЅРёСЏС…");

        if (Time.unscaledTime < garageMessageUntil)
        {
            GUI.Label(new Rect(rightX + 22f, topY + 545f, rightWidth - 44f, 35f), garageMessage, centeredStyle);
        }

        GUI.Label(new Rect(rightX + 22f, topY + 585f, rightWidth - 44f, 34f), "РџРѕРєСѓРїРєРё Рё С‚СЋРЅРёРЅРі СЃРѕС…СЂР°РЅСЏСЋС‚СЃСЏ Р°РІС‚РѕРјР°С‚РёС‡РµСЃРєРё", centeredStyle);
    }

    private void DrawUpgradeCard(Rect rect, int category, string title, string description)
    {
        DrawSolidRect(rect, new Color(0.025f, 0.075f, 0.085f, 0.95f));
        int level = category == 0 ? engineLevel : category == 1 ? handlingLevel : armorLevel;
        GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, rect.width - 36f, 28f), title + "  " + level + " / " + MaxUpgradeLevel, labelStyle);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 43f, rect.width - 36f, 24f), description, smallStyle);
        string buttonText = level >= MaxUpgradeLevel ? "РњРђРљРЎРРњРЈРњ" : "[ " + (category + 1) + " ]  РЈР›РЈР§РЁРРўР¬  " + GetUpgradeCost(category);
        GUI.enabled = level < MaxUpgradeLevel;
        if (GUI.Button(new Rect(rect.x + 18f, rect.y + 79f, rect.width - 36f, 46f), buttonText, menuButtonStyle))
        {
            TryBuyUpgrade(category);
        }
        GUI.enabled = true;
    }

    private void DrawColorChoices(Rect rect, Color[] colors, int selectedIndex, bool paint)
    {
        float gap = 9f;
        float size = Mathf.Min(rect.height, (rect.width - gap * (colors.Length - 1)) / colors.Length);
        for (int i = 0; i < colors.Length; i++)
        {
            Rect swatch = new Rect(rect.x + i * (size + gap), rect.y, size, size);
            bool hovered = swatch.Contains(Event.current.mousePosition);
            if (i == selectedIndex)
            {
                DrawSolidRect(new Rect(swatch.x - 5f, swatch.y - 5f, swatch.width + 10f, swatch.height + 10f), new Color(0.005f, 0.015f, 0.02f));
                DrawSolidRect(new Rect(swatch.x - 3f, swatch.y - 3f, swatch.width + 6f, swatch.height + 6f), Color.white);
            }
            else if (hovered)
            {
                DrawSolidRect(new Rect(swatch.x - 3f, swatch.y - 3f, swatch.width + 6f, swatch.height + 6f), new Color(0.12f, 0.94f, 1f));
            }

            DrawSolidRect(swatch, colors[i]);
            if (GUI.Button(swatch, GUIContent.none, GUIStyle.none))
            {
                if (paint)
                {
                    SelectPaint(i);
                }
                else
                {
                    SelectNeon(i);
                }
            }
        }
    }

private void DrawCarPreview(Rect rect, int carIndex)
    {
        Color paint = PaintColors[paintColorIndex];
        Color neon = NeonColors[neonColorIndex];
        Vector3 shape = CarScales[carIndex];

        DrawSolidRect(rect, new Color(0.004f, 0.014f, 0.024f, 0.72f));
        DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(neon.r, neon.g, neon.b, 0.3f));
        DrawSolidRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), new Color(neon.r, neon.g, neon.b, 0.16f));

        for (float x = rect.x + 34f; x < rect.xMax; x += 58f)
        {
            DrawSolidRect(new Rect(x, rect.y, 1f, rect.height), new Color(neon.r, neon.g, neon.b, 0.032f));
        }

        for (float y = rect.y + 30f; y < rect.yMax; y += 48f)
        {
            DrawSolidRect(new Rect(rect.x, y, rect.width, 1f), new Color(neon.r, neon.g, neon.b, 0.03f));
        }

        float previewTime = Time.unscaledTime;
        float scannerY = Mathf.Lerp(rect.y + 8f, rect.yMax - 8f, Mathf.Repeat(previewTime * 0.14f, 1f));
        DrawSolidRect(new Rect(rect.x + 2f, scannerY, rect.width - 4f, 2f), new Color(neon.r, neon.g, neon.b, 0.13f));

        float previewScale = Mathf.Clamp(Mathf.Min(rect.width / 350f, rect.height / 270f), 0.84f, 2.15f);
        float bodyWidth = 88f * shape.x * previewScale;
        float bodyHeight = 158f * shape.y * previewScale;
        float centerX = rect.center.x;
        float centerY = rect.center.y + rect.height * 0.02f + Mathf.Sin(previewTime * 1.8f) * 4f;
        float glowPulse = 0.82f + Mathf.Sin(previewTime * 2.4f) * 0.18f;

        for (int markerIndex = 0; markerIndex < 3; markerIndex++)
        {
            float angle = previewTime * (0.62f + markerIndex * 0.08f) + markerIndex * Mathf.PI * 0.68f;
            float orbitX = centerX + Mathf.Cos(angle) * bodyWidth * 0.92f;
            float orbitY = centerY + Mathf.Sin(angle) * bodyHeight * 0.64f;
            float markerSize = (5f + markerIndex * 1.5f) * previewScale;
            Color markerColor = markerIndex == 1
                ? new Color(1f, 0.06f, 0.47f, 0.42f)
                : new Color(0.05f, 0.95f, 1f, 0.38f);
            DrawRotatedRect(
                new Rect(orbitX - markerSize * 0.5f, orbitY - markerSize * 0.5f, markerSize, markerSize),
                markerColor,
                45f);
        }

        Color previousGuiColor = GUI.color;
        for (int i = 5; i >= 0; i--)
        {
            float sizeX = bodyWidth * (1.5f + i * 0.16f);
            float sizeY = bodyHeight * (1.18f + i * 0.11f);
            GUI.color = new Color(neon.r, neon.g, neon.b, (0.02f + (5 - i) * 0.008f) * glowPulse);
            GUI.DrawTexture(new Rect(centerX - sizeX * 0.5f, centerY - sizeY * 0.5f, sizeX, sizeY), circleTexture);
        }
        GUI.color = previousGuiColor;

        DrawSolidRect(new Rect(centerX - bodyWidth * 0.68f, centerY + bodyHeight * 0.48f, bodyWidth * 1.36f, 4f), new Color(neon.r, neon.g, neon.b, 0.68f));
        DrawSolidRect(new Rect(centerX - bodyWidth * 0.56f, centerY - bodyHeight * 0.55f, bodyWidth * 1.12f, bodyHeight * 1.12f), new Color(0f, 0f, 0f, 0.62f));

        float wheelWidth = Mathf.Max(10f, bodyWidth * 0.17f);
        float wheelHeight = bodyHeight * 0.245f;
        Color tire = new Color(0.006f, 0.009f, 0.014f, 1f);
        Color rim = Color.Lerp(neon, Color.white, 0.24f);
        float leftWheelX = centerX - bodyWidth * 0.5f - wheelWidth * 0.54f;
        float rightWheelX = centerX + bodyWidth * 0.5f - wheelWidth * 0.46f;
        float frontWheelY = centerY - bodyHeight * 0.32f;
        float rearWheelY = centerY + bodyHeight * 0.13f;

        DrawSolidRect(new Rect(leftWheelX, frontWheelY, wheelWidth, wheelHeight), tire);
        DrawSolidRect(new Rect(rightWheelX, frontWheelY, wheelWidth, wheelHeight), tire);
        DrawSolidRect(new Rect(leftWheelX, rearWheelY, wheelWidth, wheelHeight), tire);
        DrawSolidRect(new Rect(rightWheelX, rearWheelY, wheelWidth, wheelHeight), tire);
        DrawSolidRect(new Rect(leftWheelX + wheelWidth * 0.3f, frontWheelY + 7f, wheelWidth * 0.4f, wheelHeight - 14f), rim);
        DrawSolidRect(new Rect(rightWheelX + wheelWidth * 0.3f, frontWheelY + 7f, wheelWidth * 0.4f, wheelHeight - 14f), rim);
        DrawSolidRect(new Rect(leftWheelX + wheelWidth * 0.3f, rearWheelY + 7f, wheelWidth * 0.4f, wheelHeight - 14f), rim);
        DrawSolidRect(new Rect(rightWheelX + wheelWidth * 0.3f, rearWheelY + 7f, wheelWidth * 0.4f, wheelHeight - 14f), rim);

        DrawSolidRect(new Rect(centerX - bodyWidth * 0.54f, centerY - bodyHeight * 0.52f, bodyWidth * 1.08f, bodyHeight * 1.05f), Color.Lerp(paint, Color.black, 0.57f));
        DrawSolidRect(new Rect(centerX - bodyWidth * 0.49f, centerY - bodyHeight * 0.5f, bodyWidth * 0.98f, bodyHeight), Color.Lerp(paint, Color.black, 0.12f));

        DrawSolidRect(new Rect(centerX - bodyWidth * 0.42f, centerY - bodyHeight * 0.46f, bodyWidth * 0.84f, bodyHeight * 0.24f), Color.Lerp(paint, Color.white, 0.19f));
        DrawSolidRect(new Rect(centerX - bodyWidth * 0.37f, centerY - bodyHeight * 0.18f, bodyWidth * 0.74f, bodyHeight * 0.39f), new Color(0.01f, 0.075f, 0.125f, 0.98f));
        DrawSolidRect(new Rect(centerX - bodyWidth * 0.31f, centerY - bodyHeight * 0.12f, bodyWidth * 0.62f, bodyHeight * 0.025f), new Color(neon.r, neon.g, neon.b, 0.46f));
        DrawSolidRect(new Rect(centerX - bodyWidth * 0.03f, centerY - bodyHeight * 0.45f, bodyWidth * 0.06f, bodyHeight * 0.88f), neon);

        DrawSolidRect(new Rect(centerX - bodyWidth * 0.38f, centerY - bodyHeight * 0.48f, bodyWidth * 0.25f, Mathf.Max(6f, bodyHeight * 0.035f)), new Color(1f, 0.94f, 0.68f));
        DrawSolidRect(new Rect(centerX + bodyWidth * 0.13f, centerY - bodyHeight * 0.48f, bodyWidth * 0.25f, Mathf.Max(6f, bodyHeight * 0.035f)), new Color(1f, 0.94f, 0.68f));
        DrawSolidRect(new Rect(centerX - bodyWidth * 0.38f, centerY + bodyHeight * 0.44f, bodyWidth * 0.24f, Mathf.Max(6f, bodyHeight * 0.032f)), new Color(1f, 0.08f, 0.16f));
        DrawSolidRect(new Rect(centerX + bodyWidth * 0.14f, centerY + bodyHeight * 0.44f, bodyWidth * 0.24f, Mathf.Max(6f, bodyHeight * 0.032f)), new Color(1f, 0.08f, 0.16f));

        DrawSolidRect(new Rect(centerX - bodyWidth * 0.59f, centerY + bodyHeight * 0.38f, bodyWidth * 1.18f, bodyHeight * 0.07f), Color.Lerp(paint, Color.black, 0.38f));
        DrawSolidRect(new Rect(centerX - bodyWidth * 0.5f, centerY + bodyHeight * 0.385f, bodyWidth, 3f), new Color(neon.r, neon.g, neon.b, 0.68f));

        DrawSolidRect(new Rect(rect.x + 20f, rect.y + 18f, 52f, 3f), neon);
        GUI.Label(new Rect(rect.x + 20f, rect.y + 25f, 180f, 20f), "VEHICLE FEED   /   01", microStyle);
        GUI.Label(new Rect(rect.x + rect.width - 128f, rect.y + rect.height - 30f, 108f, 18f), "SYNC 100%", microStyle);
    }

    private string StatBar(float value)
    {
        int count = Mathf.Clamp(Mathf.RoundToInt(value * 5f), 3, 6);
        return new string((char)0x25A0, count) + new string((char)0x25A1, 6 - count);
    }

    private void CacheMinimapTrack()
    {
        minimapWorldMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        minimapWorldMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        for (int i = 0; i <= MinimapTrackSamples; i++)
        {
            float t = i * Mathf.PI * 2f / MinimapTrackSamples;
            Vector2 point = TrackCenter(t);
            minimapTrackPoints[i] = point;
            minimapWorldMin = Vector2.Min(minimapWorldMin, point);
            minimapWorldMax = Vector2.Max(minimapWorldMax, point);
        }

        Vector2 padding = Vector2.one * (TrackWidth * 0.72f);
        minimapWorldMin -= padding;
        minimapWorldMax += padding;
    }

    private void CreateMinimapTrackTexture()
    {
        if (minimapTrackTexture != null)
        {
            Destroy(minimapTrackTexture);
        }

        minimapTrackTexture = new Texture2D(MinimapTextureWidth, MinimapTextureHeight, TextureFormat.RGBA32, false);
        minimapTrackTexture.name = "Runtime Minimap Track";
        minimapTrackTexture.filterMode = FilterMode.Bilinear;
        minimapTrackTexture.wrapMode = TextureWrapMode.Clamp;
        minimapTrackTexture.hideFlags = HideFlags.DontSave;

        Color[] pixels = new Color[MinimapTextureWidth * MinimapTextureHeight];
        Vector2[] texturePoints = new Vector2[MinimapTrackSamples + 1];
        for (int i = 0; i <= MinimapTrackSamples; i++)
        {
            texturePoints[i] = MinimapTexturePoint(minimapTrackPoints[i]);
        }

        PaintMinimapTrackLayer(pixels, texturePoints, 29f, new Color(0f, 0f, 0f, 0.28f), false);
        PaintMinimapTrackLayer(pixels, texturePoints, 25f, new Color(ActiveTrack.AccentColor.r, ActiveTrack.AccentColor.g, ActiveTrack.AccentColor.b, 0.26f), false);
        PaintMinimapTrackLayer(pixels, texturePoints, 22.5f, Color.white, true);
        PaintMinimapTrackLayer(pixels, texturePoints, 19.5f, new Color(0.055f, 0.065f, 0.07f, 0.99f), false);
        PaintMinimapTrackLayer(pixels, texturePoints, 17.2f, new Color(0.105f, 0.115f, 0.12f, 0.42f), false);
        PaintMinimapDashedCenterLine(pixels, texturePoints);

        minimapTrackTexture.SetPixels(pixels);
        minimapTrackTexture.Apply(false, false);
    }

    private void PaintMinimapDashedCenterLine(Color[] pixels, Vector2[] points)
    {
        for (int i = 0; i < MinimapTrackSamples; i++)
        {
            if (i % 12 >= 6)
            {
                continue;
            }

            PaintMinimapLine(pixels, points[i], points[i + 1], 1.25f, new Color(1f, 0.78f, 0.18f, 0.94f));
        }
    }

    private Vector2 MinimapTexturePoint(Vector2 worldPoint)
    {
        Vector2 worldSize = minimapWorldMax - minimapWorldMin;
        float scale = Mathf.Min(
            MinimapTextureWidth / Mathf.Max(worldSize.x, 0.01f),
            MinimapTextureHeight / Mathf.Max(worldSize.y, 0.01f));
        Vector2 usedSize = worldSize * scale;
        Vector2 offset = new Vector2(
            (MinimapTextureWidth - usedSize.x) * 0.5f,
            (MinimapTextureHeight - usedSize.y) * 0.5f);
        return offset + (worldPoint - minimapWorldMin) * scale;
    }

    private void PaintMinimapTrackLayer(Color[] pixels, Vector2[] points, float radius, Color color, bool useTrackColors)
    {
        for (int i = 0; i < MinimapTrackSamples; i++)
        {
            Color lineColor = color;
            if (useTrackColors)
            {
                Color trackColor = (i / 16) % 2 == 0 ? ActiveTrack.CurbA : ActiveTrack.CurbB;
                lineColor = new Color(trackColor.r, trackColor.g, trackColor.b, color.a);
            }

            PaintMinimapLine(pixels, points[i], points[i + 1], radius, lineColor);
        }
    }

    private void PaintMinimapLine(Color[] pixels, Vector2 start, Vector2 end, float radius, Color color)
    {
        float distance = Vector2.Distance(start, end);
        int steps = Mathf.Max(1, Mathf.CeilToInt(distance * 1.35f));
        for (int step = 0; step <= steps; step++)
        {
            PaintMinimapDisc(pixels, Vector2.Lerp(start, end, step / (float)steps), radius, color);
        }
    }

    private void PaintMinimapDisc(Color[] pixels, Vector2 center, float radius, Color color)
    {
        int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 1f));
        int maxX = Mathf.Min(MinimapTextureWidth - 1, Mathf.CeilToInt(center.x + radius + 1f));
        int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 1f));
        int maxY = Mathf.Min(MinimapTextureHeight - 1, Mathf.CeilToInt(center.y + radius + 1f));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float coverage = Mathf.Clamp01(radius + 0.7f - Vector2.Distance(center, new Vector2(x + 0.5f, y + 0.5f)));
                if (coverage <= 0f)
                {
                    continue;
                }

                int pixelIndex = y * MinimapTextureWidth + x;
                Color destination = pixels[pixelIndex];
                float sourceAlpha = color.a * coverage;
                float outputAlpha = sourceAlpha + destination.a * (1f - sourceAlpha);
                if (outputAlpha <= 0.0001f)
                {
                    continue;
                }

                Vector3 blendedRgb = (
                    new Vector3(color.r, color.g, color.b) * sourceAlpha
                    + new Vector3(destination.r, destination.g, destination.b) * destination.a * (1f - sourceAlpha)) / outputAlpha;
                pixels[pixelIndex] = new Color(blendedRgb.x, blendedRgb.y, blendedRgb.z, outputAlpha);
            }
        }
    }

    private void DrawMinimap(float screenWidth)
    {
        Rect panelRect = new Rect(screenWidth - 319f, 112f, 294f, 212f);
        Rect mapRect = new Rect(panelRect.x + 14f, panelRect.y + 47f, panelRect.width - 28f, 148f);
        Color accent = ActiveTrack.AccentColor;
        Color cyan = ActiveTrack.CurbA;
        Color magenta = ActiveTrack.CurbB;
        Color border = new Color(accent.r, accent.g, accent.b, 0.62f);

        DrawSolidRect(new Rect(panelRect.x + 5f, panelRect.y + 7f, panelRect.width, panelRect.height), new Color(0f, 0f, 0f, 0.48f));
        GUI.Box(panelRect, GUIContent.none, hudStyle);
        DrawSolidRect(new Rect(panelRect.x, panelRect.y, panelRect.width * 0.62f, 3f), cyan);
        DrawSolidRect(new Rect(panelRect.x + panelRect.width * 0.62f, panelRect.y, panelRect.width * 0.38f, 3f), magenta);
        GUI.Label(new Rect(panelRect.x + 17f, panelRect.y + 10f, 140f, 22f), "CIRCUIT MAP", smallStyle);
        GUI.Label(new Rect(panelRect.x + 164f, panelRect.y + 12f, panelRect.width - 181f, 20f), ActiveTrack.ShortName, microStyle);
        DrawSolidRect(new Rect(panelRect.x + 17f, panelRect.y + 36f, panelRect.width - 34f, 1f), new Color(accent.r, accent.g, accent.b, 0.28f));

        DrawSolidRect(new Rect(mapRect.x - 2f, mapRect.y - 2f, mapRect.width + 4f, mapRect.height + 4f), new Color(accent.r, accent.g, accent.b, 0.14f));
        DrawSolidRect(mapRect, new Color(0.002f, 0.008f, 0.018f, 0.98f));
        for (int column = 1; column < 6; column++)
        {
            float x = mapRect.x + mapRect.width * column / 6f;
            DrawSolidRect(new Rect(x, mapRect.y, 1f, mapRect.height), new Color(accent.r, accent.g, accent.b, 0.075f));
        }

        for (int row = 1; row < 4; row++)
        {
            float y = mapRect.y + mapRect.height * row / 4f;
            DrawSolidRect(new Rect(mapRect.x, y, mapRect.width, 1f), new Color(accent.r, accent.g, accent.b, 0.075f));
        }

        for (int scanline = 8; scanline < mapRect.height; scanline += 12)
        {
            DrawSolidRect(new Rect(mapRect.x, mapRect.y + scanline, mapRect.width, 1f), new Color(1f, 1f, 1f, 0.018f));
        }

        if (minimapTrackTexture != null)
        {
            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(mapRect, minimapTrackTexture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }

        float scanX = mapRect.x + Mathf.Repeat(Time.unscaledTime * 24f, mapRect.width);
        DrawSolidRect(new Rect(scanX, mapRect.y + 2f, 1f, mapRect.height - 4f), new Color(accent.r, accent.g, accent.b, 0.11f));

        Vector2 startPoint = MinimapPoint(PathPoint(0f, 0f), mapRect);
        DrawMinimapMarker(startPoint, 12f, new Color(1f, 1f, 1f, 0.13f));
        DrawSolidRect(new Rect(startPoint.x - 5f, startPoint.y - 1f, 10f, 2f), Color.white);
        DrawSolidRect(new Rect(startPoint.x - 1f, startPoint.y - 5f, 2f, 10f), new Color(0.02f, 0.04f, 0.055f));

        for (int i = 0; i < weaponPickups.Count; i++)
        {
            if (!weaponPickups[i].IsAvailable)
            {
                continue;
            }

            Vector2 pickupPoint = MinimapPoint(weaponPickups[i].transform.position, mapRect);
            DrawMinimapMarker(pickupPoint, 10f, new Color(0.12f, 1f, 0.55f, 0.15f));
            DrawMinimapMarker(pickupPoint, 5.5f, new Color(0.005f, 0.035f, 0.03f, 0.98f));
            DrawMinimapMarker(pickupPoint, 3f, new Color(0.18f, 1f, 0.62f));
        }

        for (int i = 0; i < minimapObstaclePoints.Count; i++)
        {
            Vector2 obstaclePoint = MinimapPoint(minimapObstaclePoints[i], mapRect);
            DrawMinimapMarker(obstaclePoint, 10f, new Color(1f, 0.25f, 0.04f, 0.16f));
            DrawMinimapMarker(obstaclePoint, 6f, new Color(0.025f, 0.02f, 0.015f, 0.98f));
            DrawMinimapMarker(obstaclePoint, 3.5f, new Color(1f, 0.48f, 0.06f));
        }

        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] == null || !opponents[i].gameObject.activeSelf)
            {
                continue;
            }
            CarDamage damage = opponents[i].GetComponent<CarDamage>();
            Color markerColor = damage != null && damage.IsBroken
                ? new Color(0.32f, 0.34f, 0.36f)
                : Color.Lerp(new Color(1f, 0.12f, 0.08f), new Color(1f, 0.72f, 0.12f), i / Mathf.Max(1f, opponents.Count - 1f));
            Vector2 rivalPoint = MinimapPoint(opponents[i].transform.position, mapRect);
            DrawMinimapMarker(rivalPoint, 9f, new Color(markerColor.r, markerColor.g, markerColor.b, 0.16f));
            DrawMinimapMarker(rivalPoint, 5f, markerColor);
        }

        if (player != null)
        {
            Vector2 playerPoint = MinimapPoint(player.transform.position, mapRect);
            float pulse = 13f + Mathf.Sin(Time.unscaledTime * 5f) * 1.5f;
            Color playerColor = PaintColors[paintColorIndex];
            DrawMinimapMarker(playerPoint, pulse, new Color(playerColor.r, playerColor.g, playerColor.b, 0.2f));
            DrawMinimapMarker(playerPoint, 9f, Color.white);
            DrawMinimapMarker(playerPoint, 6f, playerColor);

        }

        DrawSolidRect(new Rect(mapRect.x, mapRect.y, 14f, 2f), border);
        DrawSolidRect(new Rect(mapRect.x, mapRect.y, 2f, 14f), border);
        DrawSolidRect(new Rect(mapRect.xMax - 14f, mapRect.y, 14f, 2f), border);
        DrawSolidRect(new Rect(mapRect.xMax - 2f, mapRect.y, 2f, 14f), border);
        DrawSolidRect(new Rect(mapRect.x, mapRect.yMax - 2f, 14f, 2f), border);
        DrawSolidRect(new Rect(mapRect.x, mapRect.yMax - 14f, 2f, 14f), border);
        DrawSolidRect(new Rect(mapRect.xMax - 14f, mapRect.yMax - 2f, 14f, 2f), border);
        DrawSolidRect(new Rect(mapRect.xMax - 2f, mapRect.yMax - 14f, 2f, 14f), border);

    }

    private Vector2 MinimapPoint(Vector2 worldPoint, Rect mapRect)
    {
        Vector2 worldSize = minimapWorldMax - minimapWorldMin;
        float scale = Mathf.Min(mapRect.width / Mathf.Max(worldSize.x, 0.01f), mapRect.height / Mathf.Max(worldSize.y, 0.01f));
        Vector2 usedSize = worldSize * scale;
        Vector2 offset = new Vector2(mapRect.x + (mapRect.width - usedSize.x) * 0.5f, mapRect.y + (mapRect.height - usedSize.y) * 0.5f);
        return new Vector2(
            offset.x + (worldPoint.x - minimapWorldMin.x) * scale,
            offset.y + usedSize.y - (worldPoint.y - minimapWorldMin.y) * scale);
    }

    private void DrawMinimapMarker(Vector2 center, float size, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), circleTexture);
        GUI.color = previous;
    }

    private void DrawMinimapDiamond(Vector2 center, float size, Color color)
    {
        DrawSolidRect(new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size), color);
    }

    private void DrawSolidRect(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, pixelTexture);
        GUI.color = previous;
    }

    private void DrawPlayerDurability(float screenWidth, float screenHeight)
    {
        if (playerDamage == null)
        {
            return;
        }

        float ratio = Mathf.Clamp01(playerDamage.Health / global::CarDamage.MaxHealth);
        const float barWidth = 430f;
        const float barHeight = 16f;
        float x = screenWidth * 0.5f - barWidth * 0.5f;
        float y = screenHeight - 48f;

        GUI.Label(new Rect(x, y - 25f, barWidth, 22f), "ПРОЧНОСТЬ  " + Mathf.CeilToInt(playerDamage.Health) + " / " + Mathf.CeilToInt(global::CarDamage.MaxHealth), smallStyle);
        DrawSolidRect(new Rect(x - 4f, y - 4f, barWidth + 8f, barHeight + 8f), new Color(0.005f, 0.01f, 0.015f, 0.92f));
        DrawSolidRect(new Rect(x, y, barWidth, barHeight), new Color(0.16f, 0.025f, 0.025f, 0.96f));
        DrawSolidRect(new Rect(x, y, barWidth * ratio, barHeight), new Color(0.96f, 0.055f, 0.045f, 1f));
        DrawSolidRect(new Rect(x, y, barWidth * ratio, 3f), new Color(1f, 0.38f, 0.3f, 0.92f));
    }

    private void DrawPlayerWeaponHud(float screenWidth, float screenHeight)
    {
        if (playerWeapon == null)
        {
            return;
        }

        Rect panelRect = new Rect(screenWidth - 327f, screenHeight - 111f, 302f, 87f);
        bool armed = playerWeapon.Ammo > 0;
        Color weaponAccent = playerWeapon.ActiveWeapon == CarWeaponType.PlasmaBlaster
            ? new Color(0.22f, 0.62f, 1f)
            : new Color(0.18f, 1f, 0.62f);
        Color accent = armed ? weaponAccent : new Color(0.42f, 0.48f, 0.5f);
        DrawSolidRect(new Rect(panelRect.x + 6f, panelRect.y + 7f, panelRect.width, panelRect.height), new Color(0f, 0f, 0f, 0.46f));
        GUI.Box(panelRect, GUIContent.none, hudStyle);
        DrawSolidRect(new Rect(panelRect.x, panelRect.y, panelRect.width, 3f), accent);
        GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 11f, 185f, 22f), playerWeapon.ActiveWeaponName, smallStyle);
        GUI.Label(new Rect(panelRect.x + 207f, panelRect.y + 9f, 80f, 28f), playerWeapon.Ammo + " / " + playerWeapon.MaxAmmo, labelStyle);

        string status = playerWeapon.PickupFlashActive
            ? "БОЕЗАПАС ПОЛУЧЕН  +3"
            : armed ? "SPACE - ОГОНЬ    Q - СМЕНИТЬ" : "НАЙДИ КОНТЕЙНЕР    Q - СМЕНИТЬ";
        GUI.Label(new Rect(panelRect.x + 16f, panelRect.y + 48f, panelRect.width - 32f, 24f), status, microStyle);
    }

private void DrawWreckedOverlay(float screenWidth, float screenHeight)
{
    Color danger = new Color(1f, 0.18f, 0.045f);
    Color warning = new Color(1f, 0.68f, 0.08f);
    Color cyan = new Color(0.12f, 0.86f, 0.9f);
    float pulse = 0.62f + Mathf.Sin(Time.unscaledTime * 5.5f) * 0.18f;

    DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(0.002f, 0.006f, 0.01f, 0.86f));
    for (float y = 18f; y < screenHeight; y += 54f)
    {
        DrawSolidRect(new Rect(0f, y, screenWidth, 1f), new Color(1f, 0.12f, 0.025f, 0.035f));
    }

    DrawSolidRect(new Rect(0f, 0f, 10f, screenHeight), new Color(danger.r, danger.g, danger.b, pulse));
    DrawSolidRect(new Rect(screenWidth - 10f, 0f, 10f, screenHeight), new Color(danger.r, danger.g, danger.b, pulse));

    float panelWidth = Mathf.Min(980f, screenWidth - 72f);
    const float panelHeight = 540f;
    float panelX = (screenWidth - panelWidth) * 0.5f;
    float panelY = (screenHeight - panelHeight) * 0.5f;
    Rect panel = new Rect(panelX, panelY, panelWidth, panelHeight);

    DrawSolidRect(new Rect(panel.x + 16f, panel.y + 18f, panel.width, panel.height), new Color(0f, 0f, 0f, 0.62f));
    DrawSolidRect(panel, new Color(0.006f, 0.018f, 0.026f, 0.985f));
    DrawSolidRect(new Rect(panel.x, panel.y, panel.width, 5f), danger);
    DrawSolidRect(new Rect(panel.x, panel.y, 6f, 126f), danger);
    DrawSolidRect(new Rect(panel.x + panel.width - 94f, panel.y + panel.height - 4f, 94f, 4f), warning);

    GUI.Label(new Rect(panel.x + 32f, panel.y + 20f, panel.width - 64f, 22f), "SYSTEM ALERT  //  TERMINAL DAMAGE  //  CODE 00", kickerStyle);
    GUI.Label(new Rect(panel.x + 26f, panel.y + 48f, panel.width - 52f, 76f), "РњРђРЁРРќРђ Р РђР—Р‘РРўРђ", bigStyle);
    GUI.Label(new Rect(panel.x + 34f, panel.y + 122f, panel.width - 68f, 26f), "РљР РРўРР§Р•РЎРљРћР• РџРћР’Р Р•Р–Р”Р•РќРР• вЂ” РЈРџР РђР’Р›Р•РќРР• Р—РђР‘Р›РћРљРР РћР’РђРќРћ", smallStyle);

    float healthY = panel.y + 163f;
    GUI.Label(new Rect(panel.x + 34f, healthY, 250f, 22f), "Р¦Р•Р›РћРЎРўРќРћРЎРўР¬ РљРћР РџРЈРЎРђ", microStyle);
    GUI.Label(new Rect(panel.x + panel.width - 178f, healthY - 4f, 144f, 28f), "0 / " + Mathf.CeilToInt(global::CarDamage.MaxHealth), labelStyle);
    DrawSolidRect(new Rect(panel.x + 34f, healthY + 30f, panel.width - 68f, 13f), new Color(0.14f, 0.018f, 0.015f, 1f));
    DrawSolidRect(new Rect(panel.x + 34f, healthY + 30f, 12f, 13f), new Color(danger.r, danger.g, danger.b, pulse));
    DrawSolidRect(new Rect(panel.x + 34f, healthY + 48f, panel.width - 68f, 1f), new Color(1f, 0.24f, 0.06f, 0.36f));

    float metricsY = panel.y + 234f;
    float metricGap = 12f;
    float metricWidth = (panel.width - 68f - metricGap * 2f) / 3f;
    DrawCrashMetric(new Rect(panel.x + 34f, metricsY, metricWidth, 92f), "Р’Р Р•РњРЇ Р”Рћ РђР’РђР РР", FormatTime(finishTime), danger);
    DrawCrashMetric(new Rect(panel.x + 34f + metricWidth + metricGap, metricsY, metricWidth, 92f), "РџР РћР“Р Р•РЎРЎ Р“РћРќРљР", Mathf.Min(completedLaps + 1, RaceLapTarget) + " / " + RaceLapTarget + " РљР РЈР“", warning);
    DrawCrashMetric(new Rect(panel.x + 34f + (metricWidth + metricGap) * 2f, metricsY, metricWidth, 92f), "РџРћР—РР¦РРЇ", RacePosition() + " / " + (ActiveOpponentCount + 1), cyan);

    float actionsY = panel.y + 358f;
    float innerWidth = panel.width - 68f;
    float actionGap = 12f;
    float retryWidth = innerWidth * 0.42f;
    float secondaryWidth = (innerWidth - retryWidth - actionGap * 2f) * 0.5f;
    if (DrawCrashAction(new Rect(panel.x + 34f, actionsY, retryWidth, 84f), "R", "РџРћР’РўРћР РРўР¬ Р“РћРќРљРЈ", "Р Р•РњРћРќРў Р РќРћР’Р«Р™ РЎРўРђР Рў", danger))
    {
        RestartRace();
    }

    if (DrawCrashAction(new Rect(panel.x + 34f + retryWidth + actionGap, actionsY, secondaryWidth, 84f), "G", "Р“РђР РђР–", "РЈРЎРР›РРўР¬ РљРћР РџРЈРЎ", warning))
    {
        garageOpen = true;
        garageCarIndex = selectedCarIndex;
        Time.timeScale = 0f;
    }

    if (DrawCrashAction(new Rect(panel.x + 34f + retryWidth + actionGap + secondaryWidth + actionGap, actionsY, secondaryWidth, 84f), "ESC", "Р’ РњР•РќР®", "Р’Р«Р‘РћР  РўР РђРЎРЎР«", cyan))
    {
        OpenMainMenu();
    }

    GUI.Label(new Rect(panel.x + 34f, panel.y + 475f, panel.width - 68f, 22f), "R  RETRY     /     G  GARAGE     /     ESC  MAIN MENU", centeredStyle);
    GUI.Label(new Rect(panel.x + 34f, panel.y + 506f, panel.width - 68f, 18f), "NEON CIRCUIT SAFETY SYSTEM  //  VEHICLE OFFLINE", microStyle);
}

    private void OnGUI()
    {
        EnsureGuiStyles();
        float scale = Mathf.Clamp(Screen.height / 900f, 0.72f, 1.15f);
        float screenWidth = Screen.width / scale;
        float screenHeight = Screen.height / scale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        GUI.backgroundColor = Color.white;

        if (DrawArcadeGui(screenWidth, screenHeight))
        {
            return;
        }

        if (hitFlashAmount > 0.01f)
        {
            DrawSolidRect(new Rect(0f, 0f, screenWidth, screenHeight), new Color(1f, 0.2f, 0.13f, hitFlashAmount * 0.46f));
        }

        if (mainMenuOpen)
        {
            if (garageOpen)
            {
                DrawMenuGarage(screenWidth, screenHeight);
            }
            else
            {
                DrawMainMenu(screenWidth, screenHeight);
            }

            return;
        }

        GUI.Box(new Rect(22f, 22f, 310f, 180f), GUIContent.none, hudStyle);
        GUI.Label(new Rect(42f, 34f, 270f, 30f), ActiveTrack.ShortName, labelStyle);
        GUI.Label(new Rect(42f, 67f, 270f, 24f), "РњРћРќР•РўР«  " + coins, smallStyle);
        GUI.Label(new Rect(42f, 91f, 270f, 24f), "РљР РЈР“  " + Mathf.Min(completedLaps + 1, RaceLapTarget) + " / " + RaceLapTarget, smallStyle);
        GUI.Label(new Rect(42f, 115f, 270f, 24f), "РџРћР—РР¦РРЇ  " + RacePosition() + " / " + (ActiveOpponentCount + 1), smallStyle);
        GUI.Label(new Rect(42f, 139f, 270f, 24f), "Р’Р Р•РњРЇ  " + FormatTime(raceFinished ? finishTime : raceTime), smallStyle);
        GUI.Label(new Rect(42f, 163f, 270f, 24f), "РЎРљРћР РћРЎРўР¬  " + Mathf.RoundToInt(player.SpeedKph) + " РєРј/С‡", smallStyle);
        if (player != null)
        {
            GUI.Label(new Rect(42f, 187f, 270f, 24f), "Drift Boost: " + Mathf.RoundToInt(player.DriftCombo * 100f) + "  |  Nitro: " + Mathf.CeilToInt(player.NitroFuel) + " / 100", smallStyle);
        }

if (raceStarted && !float.IsPositiveInfinity(bestLap))
        {
            GUI.Box(new Rect(screenWidth - 245f, 22f, 220f, 78f), GUIContent.none, hudStyle);
            GUI.Label(new Rect(screenWidth - 225f, 35f, 190f, 25f), "Р›РЈР§РЁРР™ РљР РЈР“", smallStyle);
            GUI.Label(new Rect(screenWidth - 225f, 61f, 190f, 28f), FormatTime(bestLap), labelStyle);
        }

        if (!garageOpen)
        {
            DrawMinimap(screenWidth);
        }

        if (!raceStarted && !garageOpen)
        {
            string text = countdown > 3f ? "Р“РћРўРћР’Р¬РЎРЇ" : Mathf.CeilToInt(countdown).ToString();
            GUI.Label(new Rect(0f, screenHeight * 0.32f, screenWidth, 90f), text, bigStyle);
        }
        else if (raceStarted && raceTime < 1.15f && !garageOpen)
        {
            GUI.Label(new Rect(0f, screenHeight * 0.32f, screenWidth, 90f), "РЎРўРђР Рў!", bigStyle);
        }

        if (raceFinished && !playerWrecked && !garageOpen)
        {
            GUI.Box(new Rect(screenWidth * 0.5f - 240f, screenHeight * 0.5f - 120f, 480f, 240f), GUIContent.none, hudStyle);
            GUI.Label(new Rect(screenWidth * 0.5f - 210f, screenHeight * 0.5f - 98f, 420f, 62f), "Р¤РРќРРЁ!", bigStyle);
            GUI.Label(new Rect(screenWidth * 0.5f - 170f, screenHeight * 0.5f - 25f, 340f, 30f), "РРўРћР“  " + FormatTime(finishTime), labelStyle);
            GUI.Label(new Rect(screenWidth * 0.5f - 170f, screenHeight * 0.5f + 13f, 340f, 24f), "РќРђР“Р РђР”Рђ  " + lastFinishReward + " РјРѕРЅРµС‚", smallStyle);
            GUI.Label(new Rect(screenWidth * 0.5f - 170f, screenHeight * 0.5f + 47f, 360f, 24f), "R - СЃРЅРѕРІР°     G - РѕС‚РєСЂС‹С‚СЊ РіР°СЂР°Р¶", smallStyle);
        }

        if (playerWrecked && !garageOpen)
        {
            DrawWreckedOverlay(screenWidth, screenHeight);
            return;
        }

        
        GUI.Box(new Rect(22f, screenHeight - 70f, 975f, 46f), GUIContent.none, hudStyle);
        GUI.Label(new Rect(40f, screenHeight - 60f, 945f, 25f), "WASD - РЈРџР РђР’Р›Р•РќРР•    SHIFT - Р”Р РР¤Рў    X - НИТРО    SPACE - РћР“РћРќР¬    Q - РћР РЈР–РР•    G - Р“РђР РђР–    R - Р Р•РЎРўРђР Рў", smallStyle);

        if (!garageOpen)
        {
            DrawPlayerDurability(screenWidth, screenHeight);
            DrawPlayerWeaponHud(screenWidth, screenHeight);
        }

        if (garageOpen)
        {
            float panelX = screenWidth * 0.5f - 330f;
            float panelY = screenHeight * 0.5f - 220f;
            GUI.Box(new Rect(panelX, panelY, 660f, 440f), GUIContent.none, hudStyle);
            GUI.Label(new Rect(panelX + 20f, panelY + 14f, 620f, 72f), "Р“РђР РђР–", bigStyle);
            GUI.Label(new Rect(panelX + 35f, panelY + 90f, 590f, 30f), "Р‘РђР›РђРќРЎ: " + coins + " РњРћРќР•Рў", labelStyle);

            GUI.Label(new Rect(panelX + 35f, panelY + 140f, 320f, 28f), "Р”Р’РР“РђРўР•Р›Р¬  " + engineLevel + " / " + MaxUpgradeLevel, labelStyle);
            GUI.Label(new Rect(panelX + 35f, panelY + 169f, 330f, 24f), "+10% СЂР°Р·РіРѕРЅ, +6% СЃРєРѕСЂРѕСЃС‚СЊ", smallStyle);
            string engineButton = engineLevel >= MaxUpgradeLevel ? "РњРђРљРЎРРњРЈРњ" : "[1] РљРЈРџРРўР¬ - " + GetUpgradeCost(0);
            if (GUI.Button(new Rect(panelX + 390f, panelY + 143f, 230f, 48f), engineButton)) TryBuyUpgrade(0);

            GUI.Label(new Rect(panelX + 35f, panelY + 215f, 320f, 28f), "РЈРџР РђР’Р›Р•РќРР•  " + handlingLevel + " / " + MaxUpgradeLevel, labelStyle);
            GUI.Label(new Rect(panelX + 35f, panelY + 244f, 330f, 24f), "+8% РїРѕРІРѕСЂРѕС‚ Рё СЃС†РµРїР»РµРЅРёРµ", smallStyle);
            string handlingButton = handlingLevel >= MaxUpgradeLevel ? "РњРђРљРЎРРњРЈРњ" : "[2] РљРЈРџРРўР¬ - " + GetUpgradeCost(1);
            if (GUI.Button(new Rect(panelX + 390f, panelY + 218f, 230f, 48f), handlingButton)) TryBuyUpgrade(1);

            GUI.Label(new Rect(panelX + 35f, panelY + 290f, 320f, 28f), "РљРћР РџРЈРЎ  " + armorLevel + " / " + MaxUpgradeLevel, labelStyle);
            GUI.Label(new Rect(panelX + 35f, panelY + 319f, 340f, 24f), "РјРµРЅСЊС€Рµ СѓСЂРѕРЅР° Рё РїРѕС‚РµСЂСЊ РїСЂРё СѓРґР°СЂР°С…", smallStyle);
            string armorButton = armorLevel >= MaxUpgradeLevel ? "РњРђРљРЎРРњРЈРњ" : "[3] РљРЈРџРРўР¬ - " + GetUpgradeCost(2);
            if (GUI.Button(new Rect(panelX + 390f, panelY + 293f, 230f, 48f), armorButton)) TryBuyUpgrade(2);

            if (Time.unscaledTime < garageMessageUntil)
            {
                GUI.Label(new Rect(panelX + 35f, panelY + 360f, 590f, 28f), garageMessage, labelStyle);
            }

            GUI.Label(new Rect(panelX + 35f, panelY + 402f, 590f, 24f), "РќР°Р¶РјРё G, С‡С‚РѕР±С‹ РІРµСЂРЅСѓС‚СЊСЃСЏ РІ РіРѕРЅРєСѓ", smallStyle);
        }
    }


private bool DrawCrashAction(Rect rect, string hotkey, string title, string subtitle, Color accent)
{
    bool hovered = rect.Contains(Event.current.mousePosition);
    Color background = hovered
        ? new Color(accent.r * 0.19f, accent.g * 0.19f, accent.b * 0.19f, 0.99f)
        : new Color(0.022f, 0.052f, 0.062f, 0.98f);

    DrawSolidRect(new Rect(rect.x + 6f, rect.y + 7f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.48f));
    DrawSolidRect(rect, background);
    DrawSolidRect(new Rect(rect.x, rect.y, hovered ? 8f : 4f, rect.height), accent);
    DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 1f), new Color(accent.r, accent.g, accent.b, hovered ? 0.92f : 0.34f));
    GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, 48f, 22f), hotkey, kickerStyle);
    GUI.Label(new Rect(rect.x + 66f, rect.y + 9f, rect.width - 82f, 32f), title, actionTitleStyle);
    GUI.Label(new Rect(rect.x + 67f, rect.y + 48f, rect.width - 84f, 20f), subtitle, microStyle);
    return GUI.Button(rect, GUIContent.none, GUIStyle.none);
}


private void DrawCrashMetric(Rect rect, string title, string value, Color accent)
{
    DrawSolidRect(new Rect(rect.x + 5f, rect.y + 6f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.42f));
    DrawSolidRect(rect, new Color(0.018f, 0.032f, 0.04f, 0.98f));
    DrawSolidRect(new Rect(rect.x, rect.y, rect.width, 3f), accent);
    DrawSolidRect(new Rect(rect.x, rect.y, 3f, rect.height), new Color(accent.r, accent.g, accent.b, 0.72f));
    GUI.Label(new Rect(rect.x + 16f, rect.y + 15f, rect.width - 32f, 20f), title, microStyle);
    GUI.Label(new Rect(rect.x + 16f, rect.y + 43f, rect.width - 32f, 34f), value, labelStyle);
}
}

public sealed class ArcadeCarController : MonoBehaviour
{
    private NeonCircuitGame game;
    private Rigidbody2D body;
    private CarDamage damageState;
    private float speedKph;
    private const float DriftMinimumSpeed = 4.5f;
    private const float DriftEngageRate = 5.5f;
    private const float DriftReleaseRate = 2.8f;
    private const float DriftSmokeInterval = 0.07f;
    private const float DriftBoostMultiplierMax = 1.85f;
    private const float NitroFuelMax = 100f;
    private const float NitroConsumption = 26f;
    private const float NitroRecharge = 22f;
    private const float NitroSpeedMultiplier = 1.35f;
    private const float NitroAccelerationMultiplier = 1.22f;
    private const float StoryTruckRoadDamping = 0.22f;
    private float driftAmount;
    private float driftCombo;
    private float nitroFuel = NitroFuelMax;
    private float nextDriftSmokeAt;
    private float driftBoostMultiplier;
    private bool nitroActive;
    private bool finished;
    private bool broken;
    private TrailRenderer[] driftTrails = new TrailRenderer[0];
    private TrailRenderer[] tireMarkTrails = new TrailRenderer[0];
    private TrailRenderer[] nitroTrails = new TrailRenderer[0];
    private bool isDrifting;
    private float puddleSkidUntil;
    private float puddleSkidStrength;
    private float puddleSteerBias;
    private float nextSurfaceSprayAt;
    private float nextCurbFeedbackAt;

    public float SpeedKph { get { return speedKph; } }
    public float DriftCombo { get { return driftCombo; } }
    public float NitroFuel { get { return nitroFuel; } }
    public bool IsDrifting { get { return isDrifting; } }
    public bool IsNitroActive { get { return nitroActive; } }

    public void Initialize(NeonCircuitGame owner, Rigidbody2D rigidbody)
    {
        game = owner;
        body = rigidbody;
        CreateDriftTrails();
        CreateTireMarkTrails();
        CreateNitroTrails();
    }

    public void ApplyPuddleSkid(float strength)
    {
        if (body == null || body.linearVelocity.magnitude < 1.2f)
        {
            return;
        }

        puddleSkidStrength = Mathf.Clamp01(strength);
        puddleSkidUntil = Mathf.Max(puddleSkidUntil, Time.time + Mathf.Lerp(0.62f, 1.05f, puddleSkidStrength));
        float lateralSpeed = Vector2.Dot(body.linearVelocity, transform.right);
        float skidDirection = Mathf.Abs(lateralSpeed) > 0.18f
            ? Mathf.Sign(lateralSpeed)
            : (Mathf.Sin(transform.position.x * 1.73f + transform.position.y * 2.31f + Time.time * 3.1f) >= 0f ? 1f : -1f);
        puddleSteerBias = skidDirection * Mathf.Lerp(0.28f, 0.72f, puddleSkidStrength);
        body.AddForce((Vector2)transform.right * skidDirection * Mathf.Lerp(1.4f, 3.7f, puddleSkidStrength), ForceMode2D.Impulse);
        body.angularVelocity += skidDirection * Mathf.Lerp(34f, 88f, puddleSkidStrength);
    }

    private void CreateDriftTrails()
    {
        if (driftTrails != null && driftTrails.Length > 0)
        {
            return;
        }

        driftTrails = new TrailRenderer[2];
        driftTrails[0] = CreateTrailRenderer(new Vector3(-0.34f, -0.76f, 0f));
        driftTrails[1] = CreateTrailRenderer(new Vector3(0.34f, -0.76f, 0f));
    }

    private TrailRenderer CreateTrailRenderer(Vector3 localPosition)
    {
        GameObject trailObject = new GameObject("DriftTrail");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = localPosition;
        trailObject.transform.localRotation = Quaternion.identity;

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 0.36f;
        trail.minVertexDistance = 0.05f;
        trail.startWidth = 0.19f;
        trail.endWidth = 0.025f;
        trail.material = game != null ? game.DriftTrailMaterial : null;
        trail.sortingLayerName = "Default";
        trail.sortingOrder = 16;
        trail.startColor = new Color(0.18f, 0.96f, 1f, 0.55f);
        trail.endColor = new Color(0.84f, 0.28f, 1f, 0f);
        trail.emitting = false;
        return trail;
    }

    private void CreateTireMarkTrails()
    {
        if (tireMarkTrails != null && tireMarkTrails.Length > 0)
        {
            return;
        }

        tireMarkTrails = new TrailRenderer[2];
        tireMarkTrails[0] = CreateTireMarkTrail(new Vector3(-0.34f, -0.74f, 0f));
        tireMarkTrails[1] = CreateTireMarkTrail(new Vector3(0.34f, -0.74f, 0f));
    }

    private TrailRenderer CreateTireMarkTrail(Vector3 localPosition)
    {
        GameObject trailObject = new GameObject("Persistent Tire Mark");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = localPosition;
        trailObject.transform.localRotation = Quaternion.identity;

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 8.5f;
        trail.minVertexDistance = 0.055f;
        trail.startWidth = 0.115f;
        trail.endWidth = 0.075f;
        trail.material = game != null ? game.DriftTrailMaterial : null;
        trail.sortingLayerName = "Default";
        trail.sortingOrder = -13;
        trail.startColor = new Color(0.008f, 0.01f, 0.012f, 0.36f);
        trail.endColor = new Color(0.008f, 0.01f, 0.012f, 0.035f);
        trail.emitting = false;
        return trail;
    }

    private void CreateNitroTrails()
    {
        if (nitroTrails != null && nitroTrails.Length > 0)
        {
            return;
        }

        nitroTrails = new TrailRenderer[2];
        nitroTrails[0] = CreateNitroTrail(new Vector3(-0.24f, -0.82f, 0f));
        nitroTrails[1] = CreateNitroTrail(new Vector3(0.24f, -0.82f, 0f));
    }

    private TrailRenderer CreateNitroTrail(Vector3 localPosition)
    {
        GameObject trailObject = new GameObject("NitroTrail");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = localPosition;
        trailObject.transform.localRotation = Quaternion.identity;

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 0.19f;
        trail.minVertexDistance = 0.025f;
        trail.startWidth = 0.24f;
        trail.endWidth = 0.015f;
        trail.material = game != null ? game.DriftTrailMaterial : null;
        trail.sortingLayerName = "Default";
        trail.sortingOrder = 18;
        trail.startColor = new Color(0.72f, 1f, 1f, 0.95f);
        trail.endColor = new Color(1f, 0.08f, 0.62f, 0f);
        trail.emitting = false;
        return trail;
    }

private void FixedUpdate()
{
    if (game == null || body == null)
    {
        return;
    }

    if (!game.RaceStarted || game.RaceFinished || finished || broken)
    {
        driftAmount = Mathf.MoveTowards(driftAmount, 0f, DriftReleaseRate * Time.fixedDeltaTime);
        isDrifting = false;
        driftBoostMultiplier = 1f;
        nitroActive = false;
        SetDriftTrailEmission(false);
        SetNitroTrailEmission(false);
        body.linearVelocity *= 0.92f;
        speedKph = body.linearVelocity.magnitude * 12f;
        return;
    }

    float throttle;
    float steer;
    bool driftHeld;
    bool nitroPressed;
    ReadInput(out throttle, out steer, out driftHeld, out nitroPressed);

    if (damageState == null)
    {
        damageState = GetComponent<CarDamage>();
    }
    float enginePerformance = damageState != null ? damageState.EnginePerformance : 1f;
    float speedPerformance = damageState != null ? damageState.MaximumSpeedPerformance : 1f;
    float steeringPerformance = damageState != null ? damageState.SteeringPerformance : 1f;
    float gripPerformance = damageState != null ? damageState.GripPerformance : 1f;
    if (damageState != null)
    {
        steer = Mathf.Clamp(steer + damageState.SteeringPull, -1f, 1f);
    }

    float puddleAmount = Time.time < puddleSkidUntil ? puddleSkidStrength : 0f;
    if (puddleAmount > 0f)
    {
        steer = Mathf.Clamp(steer + puddleSteerBias * puddleAmount, -1f, 1f);
    }
    else
    {
        puddleSteerBias = Mathf.MoveTowards(puddleSteerBias, 0f, Time.fixedDeltaTime * 2.4f);
    }

    float forwardSpeed = Vector2.Dot(body.linearVelocity, transform.up);
    bool onRoad = game.IsOnRoad(body.position);
    float trackCenterDistance = game.DistanceFromTrackCenter(body.position);
    bool onCurb = trackCenterDistance >= NeonCircuitGame.TrackWidth * 0.47f
        && trackCenterDistance <= NeonCircuitGame.TrackWidth * 0.61f;
    UpdateSurfaceInteraction(onRoad, onCurb, Mathf.Abs(forwardSpeed));
    bool driftRequested = onRoad
        && driftHeld
        && Mathf.Abs(forwardSpeed) >= DriftMinimumSpeed
        && Mathf.Abs(steer) >= 0.12f;
    float driftRate = driftRequested ? DriftEngageRate : DriftReleaseRate;
    driftAmount = Mathf.MoveTowards(
        driftAmount,
        driftRequested ? 1f : 0f,
        driftRate * Time.fixedDeltaTime);
    isDrifting = onRoad && driftAmount > 0.04f;

    if (isDrifting)
    {
        driftCombo = Mathf.Min(100f, driftCombo + (Mathf.Abs(steer) * 18f + driftAmount * 16f) * Time.fixedDeltaTime);
        driftBoostMultiplier = Mathf.Lerp(1f, DriftBoostMultiplierMax, Mathf.Clamp01(driftCombo / 75f));

        if (game != null)
        {
            game.PlayDriftSfx(driftAmount);
            if (Time.time >= nextDriftSmokeAt)
            {
                Vector3 rearLeft = transform.TransformPoint(new Vector3(-0.34f, -0.75f, 0f));
                Vector3 rearRight = transform.TransformPoint(new Vector3(0.34f, -0.75f, 0f));
                Vector2 driftVelocity = -transform.right * steer * 3f * driftAmount;
                game.SpawnDriftSmoke(new Vector2(rearLeft.x, rearLeft.y), driftVelocity, driftAmount);
                game.SpawnDriftSmoke(new Vector2(rearRight.x, rearRight.y), driftVelocity, driftAmount);
                float interval = Mathf.Lerp(0.16f, 0.05f, driftAmount);
                nextDriftSmokeAt = Time.time + interval;
            }
        }
    }
    else
    {
        driftCombo = Mathf.Max(0f, driftCombo - Time.fixedDeltaTime * 18f);
        driftBoostMultiplier = Mathf.Lerp(driftBoostMultiplier, 1f, 1.8f * Time.fixedDeltaTime);
    }

    bool nitroPreviouslyActive = nitroActive;
    bool canNitro = game != null && game.RaceStarted && Mathf.Abs(throttle) > 0.05f;
    nitroActive = canNitro && nitroPressed && nitroFuel > 0.5f;
    if (nitroActive)
    {
        nitroFuel = Mathf.Max(0f, nitroFuel - NitroConsumption * Time.fixedDeltaTime);
        if (!nitroPreviouslyActive && game != null)
        {
            game.PlayNitroSfx();
            Vector2 burstPosition = (Vector2)transform.position - (Vector2)transform.up * 0.72f;
            game.TriggerNitroBurst(burstPosition, transform.up);
        }
    }
    else
    {
        nitroFuel = Mathf.Min(NitroFuelMax, nitroFuel + NitroRecharge * Time.fixedDeltaTime);
    }
    SetDriftTrailEmission(isDrifting);
    SetNitroTrailEmission(nitroActive);

    float acceleration = (throttle >= 0f ? 16.5f : 9f) * game.EngineAccelerationMultiplier * enginePerformance;
    acceleration *= Mathf.Lerp(1f, 0.86f, driftAmount);
    acceleration *= nitroActive ? NitroAccelerationMultiplier : 1f;
    acceleration *= nitroActive ? 1f : Mathf.Lerp(1f, driftBoostMultiplier, driftAmount);
    body.AddForce((Vector2)transform.up * throttle * acceleration, ForceMode2D.Force);

    float traction = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 2.2f);
    float direction = Mathf.Abs(forwardSpeed) < 0.25f ? 1f : Mathf.Sign(forwardSpeed);
    float steeringRate = Mathf.Lerp(145f, 215f, driftAmount);
    steeringRate *= Mathf.Lerp(1f, 0.48f, puddleAmount);
    steeringRate *= steeringPerformance;
    body.MoveRotation(
        body.rotation
        - steer * steeringRate * game.HandlingMultiplier * traction * direction * Time.fixedDeltaTime);

    Vector2 sideVelocity = Vector2.Dot(body.linearVelocity, transform.right) * (Vector2)transform.right;
    float normalGrip = 5.8f * game.HandlingMultiplier;
    float driftGrip = 1.15f + game.HandlingMultiplier * 0.2f;
    float lateralGrip = Mathf.Lerp(normalGrip, driftGrip, driftAmount);
    lateralGrip *= Mathf.Lerp(1f, 0.15f, puddleAmount);
    lateralGrip *= gripPerformance;
    body.linearVelocity -= sideVelocity * Mathf.Clamp01(lateralGrip * Time.fixedDeltaTime);

    if (driftAmount > 0.01f)
    {
        Vector2 slideForce = -(Vector2)transform.right
            * steer
            * Mathf.Abs(forwardSpeed)
            * 0.75f
            * driftAmount;
        body.AddForce(slideForce, ForceMode2D.Force);
    }

    float storyVehicleMaximumSpeedKph = game.StoryVehicleMaximumSpeedKph;
    body.linearDamping = onRoad
        ? storyVehicleMaximumSpeedKph > 0f
            ? StoryTruckRoadDamping
            : Mathf.Lerp(1.15f, 0.55f, driftAmount) * Mathf.Lerp(1f, 0.58f, puddleAmount)
        : 4.8f;
    float maxSpeed = onRoad
        ? storyVehicleMaximumSpeedKph > 0f
            ? storyVehicleMaximumSpeedKph / 12f
            : 13.2f * game.TopSpeedMultiplier * Mathf.Lerp(1f, 1.03f, driftAmount) * (nitroActive ? NitroSpeedMultiplier : driftBoostMultiplier)
        : 6.3f;
    maxSpeed *= speedPerformance;
    if (body.linearVelocity.magnitude > maxSpeed)
    {
        body.linearVelocity = body.linearVelocity.normalized * maxSpeed;
    }

    if (Vector2.Distance(body.position, game.NearestTrackPoint(body.position)) > 10f)
    {
        RecoverToTrack();
    }

    speedKph = body.linearVelocity.magnitude * 12f;
}

private void UpdateSurfaceInteraction(bool onRoad, bool onCurb, float forwardSpeed)
{
    if (forwardSpeed < 2f)
    {
        return;
    }

    if ((!onRoad || onCurb) && Time.time >= nextSurfaceSprayAt)
    {
        Vector3 rearPosition = transform.TransformPoint(new Vector3(0f, -0.78f, 0f));
        Vector2 sprayVelocity = -body.linearVelocity * 0.24f - (Vector2)transform.up * 0.35f;
        float sprayIntensity = Mathf.Clamp01(forwardSpeed / 10f) * (onRoad ? 0.58f : 1f);
        game.SpawnSurfaceSpray(rearPosition, sprayVelocity, sprayIntensity);
        nextSurfaceSprayAt = Time.time + (onRoad ? 0.12f : 0.075f);
    }

    if (onCurb)
    {
        float vibration = Mathf.Sin(Time.time * 52f + transform.position.x * 0.7f) * Mathf.Clamp01(forwardSpeed / 9f);
        body.AddForce((Vector2)transform.right * vibration * 0.42f, ForceMode2D.Force);
        if (Time.time >= nextCurbFeedbackAt)
        {
            game.ShakeCamera(Mathf.Lerp(0.025f, 0.085f, Mathf.Clamp01(forwardSpeed / 11f)), 0.08f);
            nextCurbFeedbackAt = Time.time + 0.16f;
        }
    }
}

private void ReadInput(out float throttle, out float steer, out bool driftHeld, out bool nitroHeld)
{
#if ENABLE_INPUT_SYSTEM
    Keyboard keyboard = Keyboard.current;
    if (keyboard == null)
    {
        throttle = 0f;
        steer = 0f;
        driftHeld = false;
        nitroHeld = false;
        return;
    }

    throttle = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
             - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
    steer = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
          - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
    driftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
    nitroHeld = keyboard.xKey.isPressed;
#else
    throttle = Input.GetAxisRaw("Vertical");
    steer = Input.GetAxisRaw("Horizontal");
    driftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    nitroHeld = Input.GetKey(KeyCode.X);
#endif
}

private void RecoverToTrack()
{
    driftAmount = 0f;
    puddleSkidUntil = 0f;
    puddleSkidStrength = 0f;
    puddleSteerBias = 0f;
    nitroActive = false;
    SetDriftTrailEmission(false);
    SetNitroTrailEmission(false);
    ClearEffectTrails();
    Vector2 point = game.NearestTrackPoint(body.position);
    body.position = point;
    body.rotation = game.NearestTrackRotation(point);
    body.linearVelocity = Vector2.zero;
    body.angularVelocity = 0f;
}

public void ResetToStart()
{
    finished = false;
    broken = false;
    driftAmount = 0f;
    driftCombo = 0f;
    nitroFuel = NitroFuelMax;
    nitroActive = false;
    isDrifting = false;
    driftBoostMultiplier = 1f;
    puddleSkidUntil = 0f;
    puddleSkidStrength = 0f;
    puddleSteerBias = 0f;
    SetDriftTrailEmission(false);
    SetNitroTrailEmission(false);
    ClearEffectTrails();
    body.position = game.PathPoint(NeonCircuitGame.PlayerStartT, NeonCircuitGame.PlayerStartLane);
    body.rotation = game.PathRotation(NeonCircuitGame.PlayerStartT);
    body.linearVelocity = Vector2.zero;
    body.angularVelocity = 0f;
}

public void SetBroken()
{
    broken = true;
    driftAmount = 0f;
    driftCombo = 0f;
    driftBoostMultiplier = 1f;
    nitroActive = false;
    isDrifting = false;
    SetDriftTrailEmission(false);
    SetNitroTrailEmission(false);
    ClearEffectTrails();
    body.linearVelocity *= 0.15f;
    body.angularVelocity *= 0.15f;
}

private void SetDriftTrailEmission(bool emit)
{
    for (int i = 0; i < driftTrails.Length; i++)
    {
        if (driftTrails[i] == null)
        {
            continue;
        }

        driftTrails[i].emitting = emit;
    }

    for (int i = 0; i < tireMarkTrails.Length; i++)
    {
        if (tireMarkTrails[i] != null)
        {
            tireMarkTrails[i].emitting = emit;
        }
    }
}

private void SetNitroTrailEmission(bool emit)
{
    float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 34f) * 0.5f;
    for (int i = 0; i < nitroTrails.Length; i++)
    {
        TrailRenderer trail = nitroTrails[i];
        if (trail == null)
        {
            continue;
        }

        trail.emitting = emit;
        trail.startWidth = Mathf.Lerp(0.19f, 0.29f, pulse);
        trail.time = Mathf.Lerp(0.15f, 0.23f, pulse);
    }
}

private void ClearEffectTrails()
{
    for (int i = 0; i < driftTrails.Length; i++)
    {
        if (driftTrails[i] != null)
        {
            driftTrails[i].Clear();
        }
    }

    for (int i = 0; i < nitroTrails.Length; i++)
    {
        if (nitroTrails[i] != null)
        {
            nitroTrails[i].Clear();
        }
    }

    for (int i = 0; i < tireMarkTrails.Length; i++)
    {
        if (tireMarkTrails[i] != null)
        {
            tireMarkTrails[i].Clear();
        }
    }
}

    
public void SetFinished()
    {
        finished = true;
    }


private void OnCollisionEnter2D(Collision2D collision)
    {
        if (body == null || game == null)
        {
            return;
        }

        if (collision.collider.GetComponent<CircuitAI>() != null || collision.collider.GetComponent<TrackObstacle>() != null)
        {
            float impactSpeed = collision.relativeVelocity.magnitude;
            body.linearVelocity *= game.ImpactSpeedRetention;
            body.angularVelocity *= game.ImpactAngularRetention;

            driftCombo = 0f;
            SetDriftTrailEmission(false);
            isDrifting = false;
        }
    }
}

public sealed class CircuitAI : MonoBehaviour
{
    private enum RivalTactic
    {
        Racing,
        Overtaking,
        CollectingWeapon,
        CollectingRepair,
        Defending,
        Reversing,
        Avoiding
    }

    private const float AvoidanceCheckInterval = 0.045f;
    private const float AvoidanceLaneOffset = 2.65f;
    private const float AvoidanceProbeRadius = 0.88f;
    private const int AvoidanceProbeCount = 12;
    private const float WeaponPickupLookAhead = 0.62f;
    private const float RepairPickupLookAhead = 0.74f;
    private const float WeaponTargetRange = 17.5f;
    private const float DriftEngageRate = 3.8f;
    private const float DriftReleaseRate = 2.6f;
    private const float DriftMinimumSharpness = 0.2f;
    private const float CornerLinePrioritySharpness = 0.18f;
    private const float AiEngineAcceleration = 20.5f;
    private const float AiBrakeAcceleration = 12f;
    private const float AiNormalGrip = 5.2f;
    private const float AiDriftGrip = 1.45f;
    private const float AiReverseAcceleration = 18f;
    private const float AiReverseMaximumSpeed = 4.8f;
    private const float AiStuckReverseDelay = 1.05f;
    private const float AiRecoveryDelay = 3.2f;
    private const float NitroFuelMax = 100f;
    private const float NitroMinimumActivationFuel = 24f;
    private const float NitroConsumption = 31f;
    private const float NitroRecharge = 14f;
    private const float NitroSpeedMultiplier = 1.2f;
    private const float NitroAccelerationMultiplier = 1.18f;
    private static readonly float[] AvoidanceLanes = { -AvoidanceLaneOffset, -1.3f, 0f, 1.3f, AvoidanceLaneOffset };
    private static readonly float[] PersonalityAggression = { 0.38f, 0.82f, 0.56f, 0.92f, 0.46f, 0.72f };
    private static readonly float[] PersonalityAwareness = { 0.88f, 0.68f, 0.8f, 0.62f, 0.94f, 0.76f };
    private static readonly float[] PersonalityResourceFocus = { 0.68f, 0.34f, 0.58f, 0.26f, 0.84f, 0.46f };
    private static readonly Vector2 ObstacleClearanceSize = new Vector2(0.9f, 1.72f);

    private NeonCircuitGame game;
    private Rigidbody2D body;
    private float startT;
    private float startLane;
    private float t;
    private float lane;
    private float targetLane;
    private float baseSpeed;
    private float speed;
    private float stuckTime;
    private float reversingUntil;
    private float reverseCooldownUntil;
    private float reverseTurnDirection;
    private int personality;
    private PlayerWeaponSystem weapon;
    private CarDamage carDamage;
    private WeaponPickup[] weaponPickups = new WeaponPickup[0];
    private RepairPickup[] repairPickups = new RepairPickup[0];
    private bool broken;
    private readonly Collider2D[] avoidanceHits = new Collider2D[24];
    private readonly RaycastHit2D[] weaponSightHits = new RaycastHit2D[16];
    private float nextAvoidanceCheck;
    private float avoidanceUntil;
    private float avoidanceSpeedMultiplier = 1f;
    private float nextWeaponDecision;
    private float nextWeaponFireTime;
    private float nextRacingLineDecision;
    private float tacticCommittedUntil;
    private float aggression;
    private float awareness;
    private float resourceFocus;
    private float tacticalSpeedMultiplier = 1f;
    private float driftAmount;
    private float driftDirection;
    private float upcomingTurnSharpness;
    private float nextDriftSmokeAt;
    private float nitroFuel = NitroFuelMax;
    private float nitroCommittedUntil;
    private float nextNitroDecision;
    private float nextNitroUseAllowed;
    private float nextNitroSparkAt;
    private bool nitroActive;
    private bool avoiding;
    private bool seekingWeaponPickup;
    private bool isDrifting;
    private float puddleSkidUntil;
    private float puddleSkidStrength;
    private float puddleSteerBias;
    private RivalTactic currentTactic;
    private float nextSurfaceSprayAt;
    private TrailRenderer[] driftTrails = new TrailRenderer[0];
    private TrailRenderer[] tireMarkTrails = new TrailRenderer[0];
    private TrailRenderer[] nitroTrails = new TrailRenderer[0];

    public float TotalProgress { get { return t / (Mathf.PI * 2f); } }
    public int WeaponAmmo { get { return weapon != null ? weapon.Ammo : 0; } }
    public bool IsDrifting { get { return isDrifting; } }
    public bool IsReversing { get { return Time.time < reversingUntil; } }
    public bool IsNitroActive { get { return nitroActive; } }
    public float NitroFuel { get { return nitroFuel; } }
    public float DriftAmount { get { return driftAmount; } }
    public string CurrentTacticName { get { return currentTactic.ToString(); } }

    public void Initialize(NeonCircuitGame owner, Rigidbody2D rigidbody, float initialT, float initialLane, float racerSpeed, int racerPersonality)
    {
        game = owner;
        body = rigidbody;
        startT = initialT;
        startLane = initialLane;
        t = initialT;
        lane = initialLane;
        targetLane = initialLane;
        baseSpeed = racerSpeed;
        personality = racerPersonality;
        int profile = Mathf.Abs(personality) % PersonalityAggression.Length;
        aggression = PersonalityAggression[profile];
        awareness = PersonalityAwareness[profile];
        resourceFocus = PersonalityResourceFocus[profile];
        currentTactic = RivalTactic.Racing;
        CreateDriftTrails();
        CreateTireMarkTrails();
        CreateNitroTrails();
    }

    public void ApplyPuddleSkid(float strength)
    {
        if (body == null || body.linearVelocity.magnitude < 1.2f)
        {
            return;
        }

        puddleSkidStrength = Mathf.Clamp01(strength);
        puddleSkidUntil = Mathf.Max(puddleSkidUntil, Time.time + Mathf.Lerp(0.5f, 0.88f, puddleSkidStrength));
        float lateralSpeed = Vector2.Dot(body.linearVelocity, transform.right);
        float skidDirection = Mathf.Abs(lateralSpeed) > 0.18f
            ? Mathf.Sign(lateralSpeed)
            : ((personality + Mathf.FloorToInt(Time.time * 2f)) % 2 == 0 ? 1f : -1f);
        puddleSteerBias = skidDirection * Mathf.Lerp(0.2f, 0.58f, puddleSkidStrength);
        body.AddForce((Vector2)transform.right * skidDirection * Mathf.Lerp(1.1f, 3f, puddleSkidStrength), ForceMode2D.Impulse);
        body.angularVelocity += skidDirection * Mathf.Lerp(28f, 72f, puddleSkidStrength);
    }

    private void CreateDriftTrails()
    {
        driftTrails = new TrailRenderer[2];
        driftTrails[0] = CreateDriftTrail(new Vector3(-0.34f, -0.76f, 0f));
        driftTrails[1] = CreateDriftTrail(new Vector3(0.34f, -0.76f, 0f));
    }

    private TrailRenderer CreateDriftTrail(Vector3 localPosition)
    {
        GameObject trailObject = new GameObject("Rival Drift Trail");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = localPosition;
        trailObject.transform.localRotation = Quaternion.identity;

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 0.2f;
        trail.minVertexDistance = 0.06f;
        trail.startWidth = 0.13f;
        trail.endWidth = 0.01f;
        trail.material = game != null ? game.DriftTrailMaterial : null;
        trail.sortingLayerName = "Default";
        trail.sortingOrder = 16;
        Color start = personality % 2 == 0
            ? new Color(1f, 0.18f, 0.58f, 0.3f)
            : new Color(0.12f, 0.92f, 1f, 0.3f);
        trail.startColor = start;
        trail.endColor = new Color(start.r, start.g, start.b, 0f);
        trail.emitting = false;
        return trail;
    }

    private void CreateTireMarkTrails()
    {
        tireMarkTrails = new TrailRenderer[2];
        tireMarkTrails[0] = CreateTireMarkTrail(new Vector3(-0.34f, -0.74f, 0f));
        tireMarkTrails[1] = CreateTireMarkTrail(new Vector3(0.34f, -0.74f, 0f));
    }

    private TrailRenderer CreateTireMarkTrail(Vector3 localPosition)
    {
        GameObject trailObject = new GameObject("Rival Tire Mark");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = localPosition;
        trailObject.transform.localRotation = Quaternion.identity;

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        trail.time = 6.5f;
        trail.minVertexDistance = 0.065f;
        trail.startWidth = 0.1f;
        trail.endWidth = 0.06f;
        trail.material = game != null ? game.DriftTrailMaterial : null;
        trail.sortingLayerName = "Default";
        trail.sortingOrder = -13;
        trail.startColor = new Color(0.008f, 0.01f, 0.012f, 0.28f);
        trail.endColor = new Color(0.008f, 0.01f, 0.012f, 0.025f);
        trail.emitting = false;
        return trail;
    }

    private void CreateNitroTrails()
    {
        nitroTrails = new TrailRenderer[3];
        nitroTrails[0] = CreateNitroTrail(new Vector3(-0.24f, -0.82f, 0f));
        nitroTrails[1] = CreateNitroTrail(new Vector3(0.24f, -0.82f, 0f));
        nitroTrails[2] = CreateNitroTrail(new Vector3(0f, -0.88f, 0f));
    }

    private TrailRenderer CreateNitroTrail(Vector3 localPosition)
    {
        GameObject trailObject = new GameObject("Rival Nitro Trail");
        trailObject.transform.SetParent(transform);
        trailObject.transform.localPosition = localPosition;
        trailObject.transform.localRotation = Quaternion.identity;

        TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
        bool glowTrail = Mathf.Abs(localPosition.x) < 0.01f;
        trail.time = glowTrail ? 0.34f : 0.28f;
        trail.minVertexDistance = 0.025f;
        trail.startWidth = glowTrail ? 0.48f : 0.34f;
        trail.endWidth = glowTrail ? 0.045f : 0.02f;
        trail.material = game != null ? game.DriftTrailMaterial : null;
        trail.sortingLayerName = "Default";
        trail.sortingOrder = glowTrail ? 17 : 18;
        Color start = personality % 2 == 0
            ? new Color(1f, 0.74f, 0.16f, 0.95f)
            : new Color(1f, 0.18f, 0.58f, 0.95f);
        trail.startColor = glowTrail
            ? new Color(start.r, start.g, start.b, 0.48f)
            : start;
        trail.endColor = new Color(start.r, 0.05f, 0.18f, 0f);
        trail.emitting = false;
        return trail;
    }

    public void AttachWeapon(PlayerWeaponSystem carWeapon)
    {
        weapon = carWeapon;
        carDamage = GetComponent<CarDamage>();
        weaponPickups = FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
        repairPickups = FindObjectsByType<RepairPickup>(FindObjectsSortMode.None);
    }

    private void FixedUpdate()
    {
        if (game == null || body == null || broken)
        {
            SetDriftTrailEmission(false);
            nitroActive = false;
            SetNitroTrailEmission(false);
            return;
        }

        if (carDamage == null)
        {
            carDamage = GetComponent<CarDamage>();
        }

        TryCollectNearbyWeaponPickup();
        TryCollectNearbyRepairPickup();
        if (!game.RaceStarted || game.RaceFinished)
        {
            driftAmount = Mathf.MoveTowards(driftAmount, 0f, DriftReleaseRate * Time.fixedDeltaTime);
            upcomingTurnSharpness = 0f;
            isDrifting = false;
            nitroActive = false;
            SetDriftTrailEmission(false);
            SetNitroTrailEmission(false);
            return;
        }

        UpdateTrackCoordinates();
        speed = body.linearVelocity.magnitude;
        if (Time.time < reversingUntil)
        {
            UpdateNitroState(false);
            DriveReverse();
            return;
        }

        if (currentTactic == RivalTactic.Reversing)
        {
            FinishReverseManeuver();
        }

        UpdateWeaponDecision();
        UpdateAvoidanceDecision();
        if (!avoiding)
        {
            UpdateRacingLine();
        }

        UpdateDriftState();
        UpdateNitroState(true);

        avoidanceSpeedMultiplier = Mathf.MoveTowards(
            avoidanceSpeedMultiplier,
            avoiding ? avoidanceSpeedMultiplier : 1f,
            1.8f * Time.fixedDeltaTime);

        float pulse = Mathf.Sin(Time.time * (0.7f + personality * 0.09f) + personality * 1.7f) * 0.45f;
        float raceDifficulty = Mathf.Clamp01(game.RaceTime / 95f);
        float progressDifference = game.PlayerProgress - TotalProgress;
        float catchUpBonus = Mathf.Clamp(progressDifference * 0.08f, -0.015f, 0.1f);
        float skillMultiplier = 1.06f + raceDifficulty * 0.07f + personality * 0.004f + catchUpBonus;
        float desiredSpeed = (baseSpeed + pulse) * game.RivalSpeedMultiplier * skillMultiplier * avoidanceSpeedMultiplier;
        desiredSpeed *= tacticalSpeedMultiplier;
        desiredSpeed *= Mathf.Lerp(1f, 0.97f, upcomingTurnSharpness);
        desiredSpeed *= Mathf.Lerp(1f, 0.995f, driftAmount);
        desiredSpeed *= nitroActive ? NitroSpeedMultiplier : 1f;
        DriveCar(desiredSpeed);
        UpdateRecoveryState();
        UpdateDriftEffects();
    }

    private void UpdateTrackCoordinates()
    {
        const int coarseSteps = 10;
        const int refineSteps = 4;
        float bestT = t;
        float bestDistance = ((Vector2)body.position - game.PathPoint(t, 0f)).sqrMagnitude;
        float searchStep = 0.045f;

        for (int step = -coarseSteps; step <= coarseSteps; step++)
        {
            float candidateT = t + step * searchStep;
            float distance = ((Vector2)body.position - game.PathPoint(candidateT, 0f)).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestT = candidateT;
            }
        }

        searchStep *= 0.5f;
        for (int iteration = 0; iteration < refineSteps; iteration++)
        {
            float leftT = bestT - searchStep;
            float rightT = bestT + searchStep;
            float leftDistance = ((Vector2)body.position - game.PathPoint(leftT, 0f)).sqrMagnitude;
            float rightDistance = ((Vector2)body.position - game.PathPoint(rightT, 0f)).sqrMagnitude;
            if (leftDistance < bestDistance)
            {
                bestDistance = leftDistance;
                bestT = leftT;
            }

            if (rightDistance < bestDistance)
            {
                bestDistance = rightDistance;
                bestT = rightT;
            }

            searchStep *= 0.5f;
        }

        t = bestT;
        Vector2 center = game.PathPoint(t, 0f);
        Vector2 tangent = game.PathDerivative(t).normalized;
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        lane = Vector2.Dot(body.position - center, normal);
    }

    private void DriveCar(float desiredSpeed)
    {
        Vector2 derivative = game.PathDerivative(t);
        float derivativeMagnitude = Mathf.Max(derivative.magnitude, 0.1f);
        float speedRatio = Mathf.Clamp01(speed / Mathf.Max(baseSpeed, 0.1f));
        float lookAheadDistance = Mathf.Lerp(4.2f, 7.8f, speedRatio) * Mathf.Lerp(0.9f, 1.08f, awareness);
        lookAheadDistance *= Mathf.Lerp(1f, 0.78f, upcomingTurnSharpness);
        float lookAheadT = lookAheadDistance / derivativeMagnitude;
        Vector2 targetPosition = game.PathPoint(t + lookAheadT, targetLane);
        Vector2 toTarget = targetPosition - body.position;
        Vector2 desiredDirection = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : derivative.normalized;

        float headingError = Vector2.SignedAngle(transform.up, desiredDirection);
        float steeringPrecision = Mathf.Lerp(46f, 30f, awareness);
        float steer = Mathf.Clamp(-headingError / steeringPrecision, -1f, 1f);
        float puddleAmount = Time.time < puddleSkidUntil ? puddleSkidStrength : 0f;
        if (puddleAmount > 0f)
        {
            steer = Mathf.Clamp(steer + puddleSteerBias * puddleAmount, -1f, 1f);
        }
        else
        {
            puddleSteerBias = Mathf.MoveTowards(puddleSteerBias, 0f, Time.fixedDeltaTime * 2.8f);
        }
        if (carDamage != null)
        {
            steer = Mathf.Clamp(steer + carDamage.SteeringPull, -1f, 1f);
        }
        float forwardSpeed = Vector2.Dot(body.linearVelocity, transform.up);
        float throttle;
        if (forwardSpeed < desiredSpeed - 0.35f)
        {
            throttle = 1f;
        }
        else if (forwardSpeed > desiredSpeed + 0.25f)
        {
            throttle = -Mathf.Lerp(0.35f, 0.7f, upcomingTurnSharpness);
        }
        else
        {
            throttle = 0.16f;
        }

        float absoluteHeadingError = Mathf.Abs(headingError);
        if (absoluteHeadingError > 52f)
        {
            throttle = Mathf.Min(throttle, 0.08f);
        }

        bool onRoad = Mathf.Abs(lane) <= NeonCircuitGame.TrackWidth * 0.56f;
        bool onCurb = Mathf.Abs(lane) >= NeonCircuitGame.TrackWidth * 0.47f
            && Mathf.Abs(lane) <= NeonCircuitGame.TrackWidth * 0.61f;
        if ((!onRoad || onCurb) && speed >= 2f && Time.time >= nextSurfaceSprayAt)
        {
            Vector3 rearPosition = transform.TransformPoint(new Vector3(0f, -0.76f, 0f));
            Vector2 sprayVelocity = -body.linearVelocity * 0.2f - (Vector2)transform.up * 0.28f;
            float sprayIntensity = Mathf.Clamp01(speed / 10f) * (onRoad ? 0.48f : 0.82f);
            game.SpawnSurfaceSpray(rearPosition, sprayVelocity, sprayIntensity);
            nextSurfaceSprayAt = Time.time + (onRoad ? 0.16f : 0.1f);
        }
        float acceleration = throttle >= 0f
            ? AiEngineAcceleration * game.RivalAccelerationMultiplier
            : AiBrakeAcceleration;
        if (nitroActive && throttle > 0f)
        {
            acceleration *= NitroAccelerationMultiplier;
        }
        if (carDamage != null)
        {
            acceleration *= carDamage.EnginePerformance;
        }
        if (!onRoad)
        {
            throttle = Mathf.Min(throttle, 0.42f);
        }

        body.AddForce((Vector2)transform.up * throttle * acceleration, ForceMode2D.Force);

        float steeringTraction = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 1.4f);
        float steeringRate = Mathf.Lerp(142f, 194f, awareness) * game.RivalHandlingMultiplier;
        steeringRate *= Mathf.Lerp(1f, 1.14f, driftAmount);
        steeringRate *= Mathf.Lerp(1f, 0.55f, puddleAmount);
        steeringRate *= carDamage != null ? carDamage.SteeringPerformance : 1f;
        body.MoveRotation(body.rotation - steer * steeringRate * steeringTraction * Time.fixedDeltaTime);

        Vector2 sideVelocity = Vector2.Dot(body.linearVelocity, transform.right) * (Vector2)transform.right;
        float lateralGrip = Mathf.Lerp(AiNormalGrip, AiDriftGrip, driftAmount) * game.RivalHandlingMultiplier;
        lateralGrip *= Mathf.Lerp(1f, 0.18f, puddleAmount);
        lateralGrip *= carDamage != null ? carDamage.GripPerformance : 1f;
        body.linearVelocity -= sideVelocity * Mathf.Clamp01(lateralGrip * Time.fixedDeltaTime);
        body.linearDamping = onRoad
            ? Mathf.Lerp(0.82f, 0.48f, driftAmount) * Mathf.Lerp(1f, 0.62f, puddleAmount)
            : 3.5f;

        float maximumSpeed = Mathf.Max(baseSpeed * 0.9f, desiredSpeed + 1.35f);
        maximumSpeed *= carDamage != null ? carDamage.MaximumSpeedPerformance : 1f;
        if (body.linearVelocity.magnitude > maximumSpeed)
        {
            body.linearVelocity = body.linearVelocity.normalized * maximumSpeed;
        }

        speed = body.linearVelocity.magnitude;
    }

    private void UpdateNitroState(bool allowActivation)
    {
        float forwardSpeed = Vector2.Dot(body.linearVelocity, transform.up);
        bool safeToBoost = allowActivation
            && !avoiding
            && driftAmount < 0.22f
            && Mathf.Abs(lane) <= NeonCircuitGame.TrackWidth * 0.5f
            && upcomingTurnSharpness <= 0.3f
            && forwardSpeed >= baseSpeed * 0.45f;

        if (nitroActive && (!safeToBoost || Time.time >= nitroCommittedUntil || nitroFuel <= 0.5f))
        {
            nitroActive = false;
        }

        if (!nitroActive
            && safeToBoost
            && nitroFuel >= NitroMinimumActivationFuel
            && Time.time >= nextNitroDecision
            && Time.time >= nextNitroUseAllowed)
        {
            nextNitroDecision = Time.time + Mathf.Lerp(0.55f, 0.3f, awareness);
            float progressDifference = game.PlayerProgress - TotalProgress;
            bool overtaking = currentTactic == RivalTactic.Overtaking;
            bool catchingPlayer = progressDifference > Mathf.Lerp(0.055f, 0.018f, aggression);
            float opportunityPhase = Mathf.Sin(game.RaceTime * 0.72f + personality * 1.91f);
            bool openStraight = currentTactic == RivalTactic.Racing
                && nitroFuel >= 68f
                && opportunityPhase > Mathf.Lerp(0.68f, 0.22f, aggression);
            bool scheduledBoost = game.RaceTime >= 2.6f + personality * 0.48f;

            if (game.RaceTime > 2.2f && (overtaking || catchingPlayer || openStraight || scheduledBoost))
            {
                nitroActive = true;
                float burstDuration = Mathf.Lerp(1.05f, 1.55f, aggression);
                nitroCommittedUntil = Time.time + burstDuration;
                nextNitroUseAllowed = nitroCommittedUntil + Mathf.Lerp(3.5f, 2.2f, aggression);
                nextNitroSparkAt = Time.time + 0.08f;
                Vector2 burstPosition = (Vector2)transform.position - (Vector2)transform.up * 0.72f;
                game.TriggerNitroBurst(burstPosition, transform.up, false, 12);
            }
        }

        if (nitroActive)
        {
            nitroFuel = Mathf.Max(0f, nitroFuel - NitroConsumption * Time.fixedDeltaTime);
            if (Time.time >= nextNitroSparkAt)
            {
                nextNitroSparkAt = Time.time + 0.14f;
                Vector2 sparkPosition = (Vector2)transform.position - (Vector2)transform.up * 0.82f;
                game.TriggerNitroBurst(sparkPosition, transform.up, false, 3);
            }
        }
        else
        {
            nitroFuel = Mathf.Min(NitroFuelMax, nitroFuel + NitroRecharge * Time.fixedDeltaTime);
        }

        SetNitroTrailEmission(nitroActive);
    }

    private void DriveReverse()
    {
        Vector2 reverseDirection = -(Vector2)transform.up;
        float backwardSpeed = Vector2.Dot(body.linearVelocity, reverseDirection);
        if (backwardSpeed < AiReverseMaximumSpeed)
        {
            body.AddForce(reverseDirection * AiReverseAcceleration, ForceMode2D.Force);
        }

        float steeringTraction = Mathf.Clamp01(Mathf.Abs(backwardSpeed) / 1.4f);
        float steeringRate = Mathf.Lerp(78f, 118f, awareness) * game.RivalHandlingMultiplier;
        body.MoveRotation(
            body.rotation
            + reverseTurnDirection * steeringRate * Mathf.Lerp(0.38f, 1f, steeringTraction) * Time.fixedDeltaTime);

        Vector2 sideVelocity = Vector2.Dot(body.linearVelocity, transform.right) * (Vector2)transform.right;
        body.linearVelocity -= sideVelocity * Mathf.Clamp01(3.1f * Time.fixedDeltaTime);
        body.linearDamping = 0.72f;
        if (body.linearVelocity.magnitude > AiReverseMaximumSpeed)
        {
            body.linearVelocity = body.linearVelocity.normalized * AiReverseMaximumSpeed;
        }

        speed = body.linearVelocity.magnitude;
        stuckTime = 0f;
    }

    private void BeginReverseManeuver(float turnDirection, float duration)
    {
        if (Time.time < reverseCooldownUntil || Time.time < reversingUntil)
        {
            return;
        }

        reverseTurnDirection = Mathf.Abs(turnDirection) > 0.01f
            ? Mathf.Sign(turnDirection)
            : (personality % 2 == 0 ? 1f : -1f);
        reversingUntil = Time.time + Mathf.Max(0.55f, duration);
        reverseCooldownUntil = reversingUntil + 0.75f;
        currentTactic = RivalTactic.Reversing;
        avoiding = true;
        avoidanceUntil = reversingUntil + 0.9f;
        tacticalSpeedMultiplier = 0.88f;
        avoidanceSpeedMultiplier = Mathf.Max(avoidanceSpeedMultiplier, 0.42f);
        driftAmount = 0f;
        upcomingTurnSharpness = 0f;
        isDrifting = false;
        nitroActive = false;
        SetDriftTrailEmission(false);
        SetNitroTrailEmission(false);
        body.linearVelocity *= 0.28f;
        body.angularVelocity *= 0.35f;
        stuckTime = 0f;
    }

    private void FinishReverseManeuver()
    {
        currentTactic = RivalTactic.Avoiding;
        avoiding = true;
        avoidanceUntil = Time.time + 0.9f;
        tacticalSpeedMultiplier = 0.94f;
        avoidanceSpeedMultiplier = Mathf.Max(avoidanceSpeedMultiplier, 0.62f);
        nextAvoidanceCheck = 0f;
        nextRacingLineDecision = 0f;
        body.linearVelocity *= 0.35f;
        body.angularVelocity *= 0.45f;
    }

    private void UpdateRecoveryState()
    {
        bool farFromRoad = Mathf.Abs(lane) > NeonCircuitGame.TrackWidth * 0.82f;
        bool barelyMoving = speed < 0.7f;
        stuckTime = barelyMoving || farFromRoad
            ? stuckTime + Time.fixedDeltaTime
            : Mathf.Max(0f, stuckTime - Time.fixedDeltaTime * 2f);

        if (barelyMoving && stuckTime >= AiStuckReverseDelay && Time.time >= reverseCooldownUntil)
        {
            float derivativeMagnitude = Mathf.Max(game.PathDerivative(t).magnitude, 0.1f);
            float lookAheadT = 11f / derivativeMagnitude;
            float leftRisk = LaneRisk(-AvoidanceLaneOffset, lookAheadT);
            float rightRisk = LaneRisk(AvoidanceLaneOffset, lookAheadT);
            targetLane = leftRisk <= rightRisk ? -AvoidanceLaneOffset : AvoidanceLaneOffset;
            float turnDirection = leftRisk < rightRisk
                ? -1f
                : rightRisk < leftRisk ? 1f : (personality % 2 == 0 ? 1f : -1f);
            BeginReverseManeuver(turnDirection, 0.92f + (1f - awareness) * 0.28f);
            return;
        }

        if ((farFromRoad && stuckTime > 1.35f) || stuckTime >= AiRecoveryDelay)
        {
            RecoverToRacingLine();
        }
    }

    private void RecoverToRacingLine()
    {
        float recoveryLane = Mathf.Clamp(targetLane, -1.25f, 1.25f);
        body.position = game.PathPoint(t, recoveryLane);
        body.rotation = game.PathRotation(t);
        body.linearVelocity = game.PathDerivative(t).normalized * 2.2f;
        body.angularVelocity = 0f;
        lane = recoveryLane;
        targetLane = recoveryLane;
        speed = body.linearVelocity.magnitude;
        stuckTime = 0f;
        reversingUntil = 0f;
        avoiding = false;
        currentTactic = RivalTactic.Racing;
        tacticalSpeedMultiplier = 1f;
        avoidanceSpeedMultiplier = 0.72f;
    }

    private void UpdateDriftState()
    {
        Vector2 currentTangent = game.PathDerivative(t).normalized;
        float derivativeMagnitude = Mathf.Max(game.PathDerivative(t).magnitude, 0.1f);
        float lookAheadT = 8.5f / derivativeMagnitude;
        Vector2 futureTangent = game.PathDerivative(t + lookAheadT).normalized;
        float turnDirection = currentTangent.x * futureTangent.y - currentTangent.y * futureTangent.x;
        float turnSharpness = Mathf.Clamp01((1f - Vector2.Dot(currentTangent, futureTangent)) * 5.2f);
        upcomingTurnSharpness = turnSharpness;
        bool driftRequested = !avoiding
            && speed >= baseSpeed * 0.62f
            && turnSharpness >= DriftMinimumSharpness
            && Mathf.Abs(turnDirection) >= 0.018f;

        if (driftRequested)
        {
            driftDirection = Mathf.Sign(turnDirection);
        }

        driftAmount = Mathf.MoveTowards(
            driftAmount,
            driftRequested ? Mathf.Lerp(0.58f, 1f, turnSharpness) : 0f,
            (driftRequested ? DriftEngageRate : DriftReleaseRate) * Time.fixedDeltaTime);
        isDrifting = driftAmount > 0.12f;
        SetDriftTrailEmission(isDrifting);
    }

    private void UpdateDriftEffects()
    {
        if (!isDrifting || Time.time < nextDriftSmokeAt)
        {
            return;
        }

        Vector3 rearLeft = transform.TransformPoint(new Vector3(-0.34f, -0.74f, 0f));
        Vector3 rearRight = transform.TransformPoint(new Vector3(0.34f, -0.74f, 0f));
        Vector2 smokeVelocity = -transform.right * driftDirection * Mathf.Lerp(1.1f, 2.5f, driftAmount);
        game.SpawnDriftSmoke(new Vector2(rearLeft.x, rearLeft.y), smokeVelocity, driftAmount * 0.8f);
        game.SpawnDriftSmoke(new Vector2(rearRight.x, rearRight.y), smokeVelocity, driftAmount * 0.8f);
        nextDriftSmokeAt = Time.time + Mathf.Lerp(0.24f, 0.12f, driftAmount);
    }

    private void SetDriftTrailEmission(bool emit)
    {
        for (int i = 0; i < driftTrails.Length; i++)
        {
            if (driftTrails[i] != null)
            {
                driftTrails[i].emitting = emit;
            }
        }

        for (int i = 0; i < tireMarkTrails.Length; i++)
        {
            if (tireMarkTrails[i] != null)
            {
                tireMarkTrails[i].emitting = emit;
            }
        }
    }

    private void SetNitroTrailEmission(bool emit)
    {
        float pulse = 0.5f + Mathf.Sin(Time.unscaledTime * 31f + personality) * 0.5f;
        for (int i = 0; i < nitroTrails.Length; i++)
        {
            TrailRenderer trail = nitroTrails[i];
            if (trail == null)
            {
                continue;
            }

            trail.emitting = emit;
            bool glowTrail = i == nitroTrails.Length - 1;
            trail.startWidth = glowTrail
                ? Mathf.Lerp(0.42f, 0.58f, pulse)
                : Mathf.Lerp(0.28f, 0.4f, pulse);
            trail.time = glowTrail
                ? Mathf.Lerp(0.28f, 0.42f, pulse)
                : Mathf.Lerp(0.23f, 0.34f, pulse);
        }
    }

    private void ClearNitroTrails()
    {
        for (int i = 0; i < nitroTrails.Length; i++)
        {
            if (nitroTrails[i] != null)
            {
                nitroTrails[i].Clear();
            }
        }
    }

    private void UpdateRacingLine()
    {
        if (Time.time < nextRacingLineDecision)
        {
            return;
        }

        nextRacingLineDecision = Time.time + Mathf.Lerp(0.22f, 0.12f, awareness) + personality * 0.003f;
        Vector2 currentTangent = game.PathDerivative(t).normalized;
        float derivativeMagnitude = Mathf.Max(game.PathDerivative(t).magnitude, 0.1f);
        float lookAheadT = 9f / derivativeMagnitude;
        Vector2 futureTangent = game.PathDerivative(t + lookAheadT).normalized;
        float turnDirection = currentTangent.x * futureTangent.y - currentTangent.y * futureTangent.x;
        float turnSharpness = Mathf.Clamp01((1f - Vector2.Dot(currentTangent, futureTangent)) * 4.5f);
        float personalityOffset = (personality % 3 - 1) * 0.1f;
        float chosenLane = personality % 2 == 0 ? -0.45f : 0.45f;
        float bestUtility = 0.32f + CommitmentBonus(RivalTactic.Racing);
        float chosenSpeedMultiplier = 1f;
        RivalTactic chosenTactic = RivalTactic.Racing;

        if (Mathf.Abs(turnDirection) > 0.025f)
        {
            float apexLane = turnSharpness >= CornerLinePrioritySharpness
                ? Mathf.Lerp(0.65f, 1.85f, turnSharpness)
                : Mathf.Lerp(0.45f, 1.35f, turnSharpness);
            chosenLane = Mathf.Clamp(Mathf.Sign(turnDirection) * apexLane + personalityOffset, -2.1f, 2.1f);
            bestUtility = 0.48f + turnSharpness * 1.35f + awareness * 0.24f + CommitmentBonus(RivalTactic.Racing);
            chosenSpeedMultiplier = Mathf.Lerp(1.03f, 0.99f, turnSharpness);
        }

        float candidateLane;
        float candidateUtility;
        if (TryChooseRepairPickupLane(out candidateLane, out candidateUtility))
        {
            candidateUtility += CommitmentBonus(RivalTactic.CollectingRepair);
            if (candidateUtility > bestUtility)
            {
                bestUtility = candidateUtility;
                chosenLane = candidateLane;
                chosenSpeedMultiplier = 0.96f;
                chosenTactic = RivalTactic.CollectingRepair;
            }
        }

        if (TryChooseWeaponPickupLane(out candidateLane, out candidateUtility))
        {
            candidateUtility += CommitmentBonus(RivalTactic.CollectingWeapon);
            if (candidateUtility > bestUtility)
            {
                bestUtility = candidateUtility;
                chosenLane = candidateLane;
                chosenSpeedMultiplier = 0.99f;
                chosenTactic = RivalTactic.CollectingWeapon;
            }
        }

        if (TryChooseOvertakeLane(out candidateLane, out candidateUtility))
        {
            candidateUtility *= Mathf.Lerp(1f, 0.46f, turnSharpness);
            candidateUtility += CommitmentBonus(RivalTactic.Overtaking);
            if (candidateUtility > bestUtility)
            {
                bestUtility = candidateUtility;
                chosenLane = candidateLane;
                chosenSpeedMultiplier = 1.03f + GetAdaptiveAggression() * 0.045f;
                chosenTactic = RivalTactic.Overtaking;
            }
        }

        if (TryChooseDefensiveLane(out candidateLane, out candidateUtility))
        {
            candidateUtility *= Mathf.Lerp(1f, 0.55f, turnSharpness);
            candidateUtility += CommitmentBonus(RivalTactic.Defending);
            if (candidateUtility > bestUtility)
            {
                chosenLane = candidateLane;
                chosenSpeedMultiplier = 0.985f;
                chosenTactic = RivalTactic.Defending;
            }
        }

        if (chosenTactic != currentTactic)
        {
            currentTactic = chosenTactic;
            tacticCommittedUntil = Time.time + Mathf.Lerp(0.42f, 0.72f, 1f - awareness);
        }

        targetLane = Mathf.Clamp(chosenLane, -AvoidanceLaneOffset, AvoidanceLaneOffset);
        tacticalSpeedMultiplier = chosenSpeedMultiplier;
        seekingWeaponPickup = chosenTactic == RivalTactic.CollectingWeapon || chosenTactic == RivalTactic.CollectingRepair;
    }

    private bool TryChooseOvertakeLane(out float overtakeLane, out float utility)
    {
        overtakeLane = lane;
        utility = 0f;
        CarDamage[] cars = FindObjectsByType<CarDamage>(FindObjectsSortMode.None);
        float closestForwardDistance = 8.5f;
        float blockingLateralDistance = 0f;

        for (int i = 0; i < cars.Length; i++)
        {
            CarDamage candidate = cars[i];
            if (candidate == null || candidate.IsBroken || candidate.gameObject == gameObject)
            {
                continue;
            }

            Vector2 toCandidate = (Vector2)candidate.transform.position - body.position;
            float forwardDistance = Vector2.Dot(transform.up, toCandidate);
            float lateralDistance = Vector2.Dot(transform.right, toCandidate);
            if (forwardDistance <= 0.8f || forwardDistance >= closestForwardDistance || Mathf.Abs(lateralDistance) > 2.35f)
            {
                continue;
            }

            closestForwardDistance = forwardDistance;
            blockingLateralDistance = lateralDistance;
        }

        if (closestForwardDistance >= 8.5f)
        {
            return false;
        }

        float derivativeMagnitude = Mathf.Max(game.PathDerivative(t).magnitude, 0.1f);
        float lookAheadT = 11f / derivativeMagnitude;
        float preferredDirection = blockingLateralDistance >= 0f ? 1f : -1f;
        float preferredLane = Mathf.Clamp(lane + preferredDirection * 1.4f, -2.4f, 2.4f);
        float alternateLane = Mathf.Clamp(lane - preferredDirection * 1.25f, -2.4f, 2.4f);
        float preferredRisk = LaneRisk(preferredLane, lookAheadT) + Mathf.Abs(preferredLane - lane) * 0.18f;
        float alternateRisk = LaneRisk(alternateLane, lookAheadT) + Mathf.Abs(alternateLane - lane) * 0.18f + 0.32f;
        overtakeLane = preferredRisk <= alternateRisk ? preferredLane : alternateLane;

        float urgency = Mathf.InverseLerp(8.5f, 1.2f, closestForwardDistance);
        utility = 0.62f + urgency * 0.92f + GetAdaptiveAggression() * 0.72f;
        return true;
    }

    private bool TryChooseWeaponPickupLane(out float pickupLane, out float utility)
    {
        pickupLane = lane;
        utility = 0f;
        if (weapon == null || weapon.Ammo > Mathf.CeilToInt(weapon.MaxAmmo * 0.58f))
        {
            return false;
        }

        if (weaponPickups == null || weaponPickups.Length == 0)
        {
            weaponPickups = FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
        }

        float currentT = Mathf.Repeat(t, Mathf.PI * 2f);
        WeaponPickup bestPickup = null;
        float bestDelta = WeaponPickupLookAhead;

        for (int i = 0; i < weaponPickups.Length; i++)
        {
            WeaponPickup pickup = weaponPickups[i];
            if (pickup == null || !pickup.IsAvailable)
            {
                continue;
            }

            float delta = Mathf.Repeat(pickup.TrackT - currentT, Mathf.PI * 2f);
            if (delta < 0.018f || delta >= bestDelta)
            {
                continue;
            }

            bestDelta = delta;
            bestPickup = pickup;
        }

        if (bestPickup == null)
        {
            return false;
        }

        float ammoNeed = 1f - weapon.Ammo / (float)Mathf.Max(1, weapon.MaxAmmo);
        float proximity = 1f - bestDelta / WeaponPickupLookAhead;
        pickupLane = Mathf.Clamp(bestPickup.TrackLane, -AvoidanceLaneOffset, AvoidanceLaneOffset);
        utility = 0.34f + ammoNeed * 1.2f + proximity * 0.38f + resourceFocus * 0.28f + aggression * 0.16f;
        return true;
    }

    private bool TryChooseRepairPickupLane(out float pickupLane, out float utility)
    {
        pickupLane = lane;
        utility = 0f;
        if (carDamage == null)
        {
            carDamage = GetComponent<CarDamage>();
        }

        if (carDamage == null || carDamage.Health >= CarDamage.MaxHealth * 0.88f)
        {
            return false;
        }

        if (repairPickups == null || repairPickups.Length == 0)
        {
            repairPickups = FindObjectsByType<RepairPickup>(FindObjectsSortMode.None);
        }

        float currentT = Mathf.Repeat(t, Mathf.PI * 2f);
        RepairPickup bestPickup = null;
        float bestDelta = RepairPickupLookAhead;
        for (int i = 0; i < repairPickups.Length; i++)
        {
            RepairPickup pickup = repairPickups[i];
            if (pickup == null || !pickup.IsAvailable)
            {
                continue;
            }

            float delta = Mathf.Repeat(pickup.TrackT - currentT, Mathf.PI * 2f);
            if (delta < 0.018f || delta >= bestDelta)
            {
                continue;
            }

            bestDelta = delta;
            bestPickup = pickup;
        }

        if (bestPickup == null)
        {
            return false;
        }

        float damageNeed = 1f - carDamage.Health / CarDamage.MaxHealth;
        float proximity = 1f - bestDelta / RepairPickupLookAhead;
        pickupLane = Mathf.Clamp(bestPickup.TrackLane, -AvoidanceLaneOffset, AvoidanceLaneOffset);
        utility = 0.28f + damageNeed * 2.2f + proximity * 0.52f + resourceFocus * 0.42f;
        return true;
    }

    private bool TryChooseDefensiveLane(out float defensiveLane, out float utility)
    {
        defensiveLane = lane;
        utility = 0f;
        CarDamage[] cars = FindObjectsByType<CarDamage>(FindObjectsSortMode.None);
        float closestRearDistance = 6.8f;
        float threatLateralDistance = 0f;

        for (int i = 0; i < cars.Length; i++)
        {
            CarDamage candidate = cars[i];
            if (candidate == null || candidate.IsBroken || candidate.gameObject == gameObject)
            {
                continue;
            }

            Vector2 toCandidate = (Vector2)candidate.transform.position - body.position;
            float forwardDistance = Vector2.Dot(transform.up, toCandidate);
            float rearDistance = -forwardDistance;
            float lateralDistance = Vector2.Dot(transform.right, toCandidate);
            if (rearDistance <= 0.9f || rearDistance >= closestRearDistance || Mathf.Abs(lateralDistance) > 2.2f)
            {
                continue;
            }

            closestRearDistance = rearDistance;
            threatLateralDistance = lateralDistance;
        }

        if (closestRearDistance >= 6.8f)
        {
            return false;
        }

        float adaptiveAggression = GetAdaptiveAggression();
        float blockAmount = Mathf.Lerp(0.28f, 0.72f, adaptiveAggression);
        defensiveLane = Mathf.Clamp(lane - Mathf.Sign(threatLateralDistance) * blockAmount, -2.25f, 2.25f);
        float urgency = Mathf.InverseLerp(6.8f, 1.2f, closestRearDistance);
        utility = 0.42f + urgency * 0.64f + adaptiveAggression * 0.56f;
        return true;
    }

    private float CommitmentBonus(RivalTactic tactic)
    {
        return currentTactic == tactic && Time.time < tacticCommittedUntil ? 0.28f : 0f;
    }

    private float GetAdaptiveAggression()
    {
        float fallingBehind = Mathf.Clamp01((game.PlayerProgress - TotalProgress) * 0.55f);
        return Mathf.Clamp01(aggression + fallingBehind * 0.24f);
    }

    private void TryCollectNearbyWeaponPickup()
    {
        if (weapon == null || weapon.Ammo >= weapon.MaxAmmo)
        {
            return;
        }

        if (weaponPickups == null || weaponPickups.Length == 0)
        {
            weaponPickups = FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
        }

        for (int i = 0; i < weaponPickups.Length; i++)
        {
            WeaponPickup pickup = weaponPickups[i];
            if (pickup == null || !pickup.IsAvailable)
            {
                continue;
            }

            if (Vector2.Distance(body.position, pickup.transform.position) <= 1.05f && pickup.TryCollect(weapon))
            {
                return;
            }
        }
    }

    private void TryCollectNearbyRepairPickup()
    {
        if (carDamage == null)
        {
            carDamage = GetComponent<CarDamage>();
        }

        if (carDamage == null || carDamage.Health >= CarDamage.MaxHealth)
        {
            return;
        }

        if (repairPickups == null || repairPickups.Length == 0)
        {
            repairPickups = FindObjectsByType<RepairPickup>(FindObjectsSortMode.None);
        }

        for (int i = 0; i < repairPickups.Length; i++)
        {
            RepairPickup pickup = repairPickups[i];
            if (pickup == null || !pickup.IsAvailable)
            {
                continue;
            }

            if (Vector2.Distance(body.position, pickup.transform.position) <= 1.05f && pickup.TryCollect(carDamage))
            {
                return;
            }
        }
    }

    private void UpdateWeaponDecision()
    {
        if (weapon == null || weapon.Ammo <= 0 || Time.time < nextWeaponDecision)
        {
            return;
        }

        nextWeaponDecision = Time.time + Mathf.Lerp(0.24f, 0.11f, awareness) + personality * 0.008f;
        if (Time.time < nextWeaponFireTime)
        {
            return;
        }

        CarDamage[] cars = FindObjectsByType<CarDamage>(FindObjectsSortMode.None);
        Vector2 forward = transform.up;
        Transform bestTarget = null;
        float bestScore = float.PositiveInfinity;

        for (int i = 0; i < cars.Length; i++)
        {
            CarDamage candidate = cars[i];
            if (candidate == null || candidate.IsBroken || candidate.gameObject == gameObject)
            {
                continue;
            }

            Rigidbody2D candidateBody = candidate.GetComponent<Rigidbody2D>();
            Vector2 rawTargetPosition = candidate.transform.position;
            Vector2 rawOffset = rawTargetPosition - body.position;
            float rawDistance = rawOffset.magnitude;
            float predictionTime = Mathf.Clamp(rawDistance / 24f, 0f, 0.52f) * awareness;
            Vector2 predictedTargetPosition = rawTargetPosition;
            if (candidateBody != null)
            {
                predictedTargetPosition += candidateBody.linearVelocity * predictionTime;
            }

            Vector2 toCandidate = predictedTargetPosition - body.position;
            float distance = toCandidate.magnitude;
            if (distance < 1.8f || distance > WeaponTargetRange)
            {
                continue;
            }

            float alignment = Vector2.Dot(forward, toCandidate / distance);
            if (alignment < 0.64f)
            {
                continue;
            }

            float lateralDistance = Mathf.Abs(Vector2.Dot(transform.right, toCandidate));
            if (lateralDistance > 5.1f)
            {
                continue;
            }

            if (!HasClearShot(candidate, toCandidate, distance))
            {
                continue;
            }

            float adaptiveAggression = GetAdaptiveAggression();
            float playerPriority = candidate.IsPlayerCar ? Mathf.Lerp(-2.1f, -4.2f, adaptiveAggression) : 0f;
            float weakenedPriority = -(1f - candidate.Health / CarDamage.MaxHealth) * Mathf.Lerp(1.4f, 3.2f, adaptiveAggression);
            float score = distance + lateralDistance * 1.15f + (1f - alignment) * 4f + playerPriority + weakenedPriority;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = candidate.transform;
            }
        }

        if (bestTarget != null && weapon.TryFire(bestTarget))
        {
            nextWeaponFireTime = Time.time + Mathf.Lerp(0.72f, 0.46f, GetAdaptiveAggression()) + personality * 0.025f;
        }
    }

    private bool HasClearShot(CarDamage target, Vector2 toTarget, float distance)
    {
        if (distance <= 0.01f)
        {
            return false;
        }

        ContactFilter2D sightFilter = ContactFilter2D.noFilter;
        int hitCount = Physics2D.Raycast(body.position, toTarget / distance, sightFilter, weaponSightHits, distance);
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider2D hit = weaponSightHits[hitIndex].collider;
            if (hit == null || hit.attachedRigidbody == body || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            TrackObstacle obstacle = hit.GetComponent<TrackObstacle>();
            if (obstacle != null)
            {
                return false;
            }

            CarDamage hitCar = hit.GetComponent<CarDamage>();
            if (hitCar != null && hitCar != target)
            {
                return false;
            }
        }

        return true;
    }

    private void UpdateAvoidanceDecision()
    {
        if (Time.time < nextAvoidanceCheck)
        {
            return;
        }

        nextAvoidanceCheck = Time.time + AvoidanceCheckInterval + personality * 0.006f;
        float derivativeMagnitude = Mathf.Max(game.PathDerivative(t).magnitude, 0.1f);
        float lookAheadDistance = Mathf.Lerp(13f, 20f, Mathf.Clamp01(speed / Mathf.Max(baseSpeed, 0.1f)));
        float lookAheadT = lookAheadDistance / derivativeMagnitude;
        float currentRisk = LaneRisk(lane, lookAheadT);

        if (currentRisk <= 0f)
        {
            if (avoiding && Time.time >= avoidanceUntil && Mathf.Abs(lane - targetLane) < 0.3f)
            {
                avoiding = false;
                currentTactic = RivalTactic.Racing;
                tacticalSpeedMultiplier = 1f;
                nextRacingLineDecision = 0f;
            }

            return;
        }

        float bestLane = lane;
        float bestRisk = currentRisk + 2f;
        for (int i = 0; i < AvoidanceLanes.Length; i++)
        {
            float candidateLane = AvoidanceLanes[i];
            float risk = LaneRisk(candidateLane, lookAheadT);
            float laneChangeCost = Mathf.Abs(candidateLane - lane) * 0.65f;
            float personalityBias = i == personality % AvoidanceLanes.Length ? -0.08f : 0f;
            float score = risk + laneChangeCost + personalityBias;

            if (score < bestRisk)
            {
                bestRisk = score;
                bestLane = candidateLane;
            }
        }

        targetLane = bestLane;
        avoiding = true;
        currentTactic = RivalTactic.Avoiding;
        tacticalSpeedMultiplier = 0.9f;
        avoidanceUntil = Time.time + 1.2f;
        avoidanceSpeedMultiplier = bestRisk > 6f ? 0.42f : 0.74f;
    }

    private void BeginEmergencyAvoidance(float lookAheadT)
    {
        float bestLane = lane;
        float bestScore = float.PositiveInfinity;
        for (int i = 0; i < AvoidanceLanes.Length; i++)
        {
            float candidateLane = AvoidanceLanes[i];
            float risk = LaneRisk(candidateLane, lookAheadT);
            float laneChangeCost = Mathf.Abs(candidateLane - lane) * 0.8f;
            float personalityBias = i == personality % AvoidanceLanes.Length ? -0.05f : 0f;
            float score = risk + laneChangeCost + personalityBias;
            if (score < bestScore)
            {
                bestScore = score;
                bestLane = candidateLane;
            }
        }

        targetLane = bestLane;

        avoiding = true;
        currentTactic = RivalTactic.Avoiding;
        tacticalSpeedMultiplier = 0.82f;
        avoidanceUntil = Time.time + 1.4f;
        avoidanceSpeedMultiplier = 0.14f;
    }

    private bool ObstacleBlocksPosition(Vector2 position, float rotation)
    {
        int hitCount = Physics2D.OverlapBoxNonAlloc(position, ObstacleClearanceSize, rotation, avoidanceHits);
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            Collider2D hit = avoidanceHits[hitIndex];
            if (hit != null && hit.GetComponent<TrackObstacle>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private float LaneRisk(float candidateLane, float lookAheadT)
    {
        float risk = 0f;
        for (int probe = 1; probe <= AvoidanceProbeCount; probe++)
        {
            float fraction = probe / (float)AvoidanceProbeCount;
            Vector2 probePosition = game.PathPoint(t + lookAheadT * fraction, candidateLane);
            int hitCount = Physics2D.OverlapCircleNonAlloc(probePosition, AvoidanceProbeRadius, avoidanceHits);

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                Collider2D hit = avoidanceHits[hitIndex];
                if (!IsAvoidanceHazard(hit))
                {
                    continue;
                }

                risk += Mathf.Lerp(8f, 2f, fraction);
            }
        }

        return risk;
    }

    private bool IsAvoidanceHazard(Collider2D hit)
    {
        if (hit == null || hit.attachedRigidbody == body || hit.transform.IsChildOf(transform))
        {
            return false;
        }

        return hit.GetComponent<TrackObstacle>() != null || hit.GetComponent<CarDamage>() != null;
    }

    public void SetBroken()
    {
        broken = true;
        speed = 0f;
        stuckTime = 0f;
        reversingUntil = 0f;
        reverseCooldownUntil = 0f;
        driftAmount = 0f;
        upcomingTurnSharpness = 0f;
        isDrifting = false;
        nitroActive = false;
        SetDriftTrailEmission(false);
        SetNitroTrailEmission(false);
        ClearNitroTrails();
        if (body != null)
        {
            body.linearVelocity *= 0.15f;
            body.angularVelocity *= 0.15f;
        }
    }

    public void ApplyWeaponImpact()
    {
        if (broken)
        {
            return;
        }

        speed *= 0.24f;
        body.linearVelocity *= 0.42f;
        body.angularVelocity *= 0.72f;
        avoidanceSpeedMultiplier = Mathf.Min(avoidanceSpeedMultiplier, 0.48f);
    }

    public void ApplyPlasmaImpact()
    {
        if (broken)
        {
            return;
        }

        speed *= 0.7f;
        body.linearVelocity *= 0.72f;
        avoidanceSpeedMultiplier = Mathf.Min(avoidanceSpeedMultiplier, 0.82f);
    }

    public void HitObstacle(Vector2 obstaclePosition)
    {
        speed = body.linearVelocity.magnitude;
        body.linearVelocity *= 0.38f;
        body.angularVelocity *= 0.45f;
        driftAmount = 0f;
        upcomingTurnSharpness = 0f;
        isDrifting = false;
        nitroActive = false;
        SetDriftTrailEmission(false);
        SetNitroTrailEmission(false);
        Vector2 tangent = game.PathDerivative(t).normalized;
        Vector2 normal = new Vector2(-tangent.y, tangent.x);
        float obstacleLane = Vector2.Dot(obstaclePosition - game.PathPoint(t, 0f), normal);
        targetLane = obstacleLane >= lane ? -AvoidanceLaneOffset : AvoidanceLaneOffset;
        float obstacleSide = Vector2.Dot(obstaclePosition - body.position, transform.right);
        float turnAwayFromObstacle = Mathf.Abs(obstacleSide) > 0.12f
            ? (obstacleSide > 0f ? 1f : -1f)
            : (personality % 2 == 0 ? 1f : -1f);
        stuckTime = 0f;
        avoiding = true;
        currentTactic = RivalTactic.Avoiding;
        tacticalSpeedMultiplier = 0.9f;
        avoidanceUntil = Time.time + 1.4f;
        avoidanceSpeedMultiplier = 0.28f;
        BeginReverseManeuver(turnAwayFromObstacle, 0.88f + (1f - awareness) * 0.3f);
    }

    
public void ResetRacer()
    {
        t = startT;
        lane = startLane;
        targetLane = startLane;
        speed = 0f;
        stuckTime = 0f;
        reversingUntil = 0f;
        reverseCooldownUntil = 0f;
        reverseTurnDirection = 0f;
        broken = false;
        avoiding = false;
        seekingWeaponPickup = false;
        currentTactic = RivalTactic.Racing;
        tacticCommittedUntil = 0f;
        tacticalSpeedMultiplier = 1f;
        driftAmount = 0f;
        driftDirection = 0f;
        nitroFuel = NitroFuelMax;
        nitroCommittedUntil = 0f;
        nextNitroDecision = 0f;
        nextNitroUseAllowed = 0f;
        nextNitroSparkAt = 0f;
        nitroActive = false;
        puddleSkidUntil = 0f;
        puddleSkidStrength = 0f;
        puddleSteerBias = 0f;
        upcomingTurnSharpness = 0f;
        isDrifting = false;
        nextDriftSmokeAt = 0f;
        SetDriftTrailEmission(false);
        SetNitroTrailEmission(false);
        ClearNitroTrails();
        for (int i = 0; i < tireMarkTrails.Length; i++)
        {
            if (tireMarkTrails[i] != null)
            {
                tireMarkTrails[i].Clear();
            }
        }
        avoidanceUntil = 0f;
        nextAvoidanceCheck = 0f;
        avoidanceSpeedMultiplier = 1f;
        nextWeaponDecision = 0f;
        nextWeaponFireTime = 0f;
        nextRacingLineDecision = 0f;
        if (weapon != null)
        {
            weapon.ResetWeapon();
        }

        body.position = game.PathPoint(startT, startLane);
        body.rotation = game.PathRotation(startT);
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }
}

public sealed class SmoothRaceCamera : MonoBehaviour
{
    private Transform target;
    private Rigidbody2D targetBody;
    private ArcadeCarController targetController;
    private Camera raceCamera;
    private Vector3 velocity;
    private Vector3 shakeOffset;
    private float shakeTime;
    private float shakeDuration;
    private float shakeIntensity;
    private float shakeSeed;
    private float zoomVelocity;
    private float baseOrthographicSize = 7.7f;

    public void Initialize(Transform followTarget)
    {
        target = followTarget;
        targetBody = target != null ? target.GetComponent<Rigidbody2D>() : null;
        targetController = target != null ? target.GetComponent<ArcadeCarController>() : null;
        raceCamera = GetComponent<Camera>();
        if (raceCamera != null)
        {
            baseOrthographicSize = raceCamera.orthographicSize;
        }

        shakeSeed = Random.Range(0f, 1000f);
    }

    public void AddShake(float amount, float duration)
    {
        shakeIntensity = Mathf.Max(shakeIntensity, amount);
        shakeTime = Mathf.Max(shakeTime, duration);
        shakeDuration = Mathf.Max(shakeDuration, duration);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (shakeTime > 0f)
        {
            shakeTime = Mathf.Max(0f, shakeTime - Time.unscaledDeltaTime);
            float envelope = Mathf.Clamp01(shakeTime / Mathf.Max(0.001f, shakeDuration));
            float noiseTime = Time.unscaledTime * 27f;
            float noiseX = Mathf.PerlinNoise(shakeSeed, noiseTime) * 2f - 1f;
            float noiseY = Mathf.PerlinNoise(shakeSeed + 31.7f, noiseTime) * 2f - 1f;
            float strength = shakeIntensity * envelope * 0.38f;
            shakeOffset = new Vector3(noiseX * strength, noiseY * strength, 0f);
        }
        else
        {
            shakeOffset = Vector3.zero;
            shakeIntensity = 0f;
            shakeDuration = 0f;
        }

        float speedRatio = targetBody != null ? Mathf.Clamp01(targetBody.linearVelocity.magnitude / 17.5f) : 0f;
        bool nitroActive = targetController != null && targetController.IsNitroActive;
        bool drifting = targetController != null && targetController.IsDrifting;
        Vector2 lookAhead = targetBody != null
            ? targetBody.linearVelocity * Mathf.Lerp(0.035f, 0.075f, speedRatio)
            : Vector2.zero;
        Vector3 desired = new Vector3(target.position.x + lookAhead.x, target.position.y + lookAhead.y, -10f);
        float followTime = nitroActive ? 0.14f : drifting ? 0.19f : 0.22f;
        transform.position = Vector3.SmoothDamp(transform.position, desired + shakeOffset, ref velocity, followTime);

        if (raceCamera != null)
        {
            float targetSize = baseOrthographicSize + speedRatio * 0.52f + (nitroActive ? 0.42f : 0f) + (drifting ? 0.1f : 0f);
            raceCamera.orthographicSize = Mathf.SmoothDamp(raceCamera.orthographicSize, targetSize, ref zoomVelocity, nitroActive ? 0.13f : 0.28f, Mathf.Infinity, Time.unscaledDeltaTime);
        }
    }
}

