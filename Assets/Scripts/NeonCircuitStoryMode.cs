using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed partial class NeonCircuitGame
{
    private const string StoryUnlockedKey = "NeonCircuit.StoryUnlocked";
    private const string StoryCompletedPrefix = "NeonCircuit.StoryCompleted.";
    private const float TruckMaximumSpeedKph = 180f;

    private enum StoryMissionType { Race, Motorcycle, Drift, Smash, TimeTrial, Truck, Duel, Finale }
    private enum StoryVehicleType { Car, Motorcycle, Truck }

    private sealed class StoryDialogueLine
    {
        public readonly int PortraitIndex;
        public readonly string Speaker;
        public readonly string Channel;
        public readonly string Text;
        public readonly Color Accent;

        public StoryDialogueLine(int portraitIndex, string speaker, string channel, string text, Color accent)
        {
            PortraitIndex = portraitIndex;
            Speaker = speaker;
            Channel = channel;
            Text = text;
            Accent = accent;
        }
    }

    private sealed class StoryChapterDefinition
    {
        public readonly string Title;
        public readonly string CodeName;
        public readonly string Briefing;
        public readonly string Objective;
        public readonly int TrackIndex;
        public readonly int CarIndex;
        public readonly string ModeName;
        public readonly int LapCount;
        public readonly int RivalCount;
        public readonly int Reward;
        public readonly int MaximumPosition;
        public readonly float TimeLimit;
        public readonly StoryMissionType MissionType;
        public readonly StoryVehicleType VehicleType;
        public readonly string VehicleName;
        public readonly float ObjectiveTarget;
        public readonly StoryDialogueLine[] Dialogue;

        public StoryChapterDefinition(
            string title,
            string codeName,
            string briefing,
            string objective,
            int trackIndex,
            int carIndex,
            string modeName,
            int lapCount,
            int rivalCount,
            int reward,
            int maximumPosition,
            float timeLimit,
            StoryMissionType missionType,
            StoryVehicleType vehicleType,
            string vehicleName,
            float objectiveTarget,
            StoryDialogueLine[] dialogue)
        {
            Title = title;
            CodeName = codeName;
            Briefing = briefing;
            Objective = objective;
            TrackIndex = trackIndex;
            CarIndex = carIndex;
            ModeName = modeName;
            LapCount = lapCount;
            RivalCount = rivalCount;
            Reward = reward;
            MaximumPosition = maximumPosition;
            TimeLimit = timeLimit;
            MissionType = missionType;
            VehicleType = vehicleType;
            VehicleName = vehicleName;
            ObjectiveTarget = objectiveTarget;
            Dialogue = dialogue ?? new StoryDialogueLine[0];
        }
    }

    private static readonly StoryChapterDefinition[] StoryChapters =
    {
        new StoryChapterDefinition(
            "ПЕРВЫЙ СИГНАЛ", "ПРОЛОГ",
            "Аварийный сигнал выводит неизвестного пилота на след тайной системы, которая подменяет результаты Ночной Лиги.",
            "ЗАВЕРШИ ГОНКУ", 0, 0, "ОБУЧЕНИЕ", 2, 3, 250, 7, 0f,
            StoryMissionType.Race, StoryVehicleType.Car, "NEON R", 0f,
            new[]
            {
                new StoryDialogueLine(0, "ЛИКА", "ВЗЛОМАННЫЙ КАНАЛ", "Если слышишь меня — не отвечай. Атлас уже ищет твой голос. Просто выведи NEON R на трассу и доберись до финиша. Там я расскажу, почему город решил, что тебя не существует.", new Color(0.04f, 0.92f, 1f)),
                new StoryDialogueLine(1, "РУК", "МЕХАНИК", "Я собрал NEON R из трёх списанных болидов и одной очень плохой идеи. Если услышишь странный стук — прибавь музыку. Если стук станет громче музыки — тормози.", new Color(1f, 0.72f, 0.08f)),
                new StoryDialogueLine(3, "АТЛАС", "ПЕРЕХВАЧЕННЫЙ КАНАЛ", "Пилот без регистрации, остановитесь. У вас нет имени, команды или шанса на победу. Не вынуждайте систему доказывать последнее.", new Color(1f, 0.08f, 0.22f))
            }),
        new StoryChapterDefinition(
            "ГОРОД БЕЗ ПРАВИЛ", "КОНТАКТ",
            "Курьер Атласа уходит через узкие тоннели Неон-Грида. Перехватить его можно только на лёгком мотоцикле.",
            "ФИНИШИРУЙ В ТОП-5 НА МОТОЦИКЛЕ", 1, 1, "МОТО-ПОГОНЯ", 3, 6, 300, 5, 0f,
            StoryMissionType.Motorcycle, StoryVehicleType.Motorcycle, "VOLT BIKE X", 0f,
            new[]
            {
                new StoryDialogueLine(0, "ЛИКА", "ДИСПЕТЧЕР ЛИГИ", "Курьер нырнул в тоннели Неон-Грида. На машине не пройдём. Рук нашёл байк — хотя слово «нашёл» у него обычно означает «не спрашивай, чей он».", new Color(0.04f, 0.92f, 1f)),
                new StoryDialogueLine(2, "НИКС", "НЕИЗВЕСТНЫЙ ГОНЩИК", "Слышишь двигатель впереди? Это я. Догони — получишь ключ. Отстанешь — будешь всю ночь гадать, почему Атлас так боится одного курьера.", new Color(1f, 0.08f, 0.62f)),
                new StoryDialogueLine(3, "АТЛАС", "КОМАНДНЫЙ КАНАЛ", "Никс, маршрут изменён. Устраните свидетеля. За отказ будет удалена не только ваша лицензия.", new Color(1f, 0.08f, 0.22f))
            }),
        new StoryChapterDefinition(
            "ЛЕДЯНОЙ СЛЕД", "ПЕРЕХВАТ",
            "Украденный модуль спрятан в ледяных тоннелях. Сканер откроет его только после серии контролируемых заносов.",
            "НАБЕРИ 1000 ОЧКОВ ДРИФТА И ФИНИШИРУЙ", 2, 2, "ДРИФТ-ИСПЫТАНИЕ", 3, 0, 350, 7, 0f,
            StoryMissionType.Drift, StoryVehicleType.Car, "DRIFT RX", 1000f,
            new[]
            {
                new StoryDialogueLine(1, "РУК", "ГАРАЖНЫЙ КАНАЛ", "Ключ Никс ведёт под ледник. Сканер откроется только на длинном заносе. Да, я тоже считаю это глупостью. Нет, молотком открыть не получилось.", new Color(1f, 0.72f, 0.08f)),
                new StoryDialogueLine(2, "НИКС", "ОТКРЫТЫЙ КАНАЛ", "Атлас называет любой риск ошибкой. Покажи ему тысячу очков ошибки — красивой, громкой и полностью контролируемой.", new Color(1f, 0.08f, 0.62f)),
                new StoryDialogueLine(0, "ЛИКА", "ДИСПЕТЧЕР ЛИГИ", "Держи угол и не касайся стен. Если связь оборвётся, следуй за синими огнями. Красные ведут к Атласу. Или в пропасть. Сегодня это почти одно и то же.", new Color(0.04f, 0.92f, 1f))
            }),
        new StoryChapterDefinition(
            "КРАСНАЯ ЗОНА", "ПРОРЫВ",
            "Атлас перекрыл Магму-9 тяжёлыми барьерами. Разрушь блокаду и освободи маршрут для подпольных пилотов.",
            "СНЕСИ 7 ПРЕПЯТСТВИЙ И ФИНИШИРУЙ", 3, 3, "РАЗРУШЕНИЕ", 3, 6, 425, 7, 0f,
            StoryMissionType.Smash, StoryVehicleType.Car, "TITAN GT", 7f,
            new[]
            {
                new StoryDialogueLine(3, "АТЛАС", "ГОРОДСКАЯ СЕТЬ", "Магма-9 запечатана. За барьерами нет выхода, только последствия. Развернитесь — и система забудет, что вы сюда приехали.", new Color(1f, 0.08f, 0.22f)),
                new StoryDialogueLine(1, "РУК", "ГАРАЖНЫЙ КАНАЛ", "Слышал? Он сам предложил забыть. Значит, мы близко. TITAN выдержит семь ударов. Восьмой тоже, но тогда домой пойдём пешком.", new Color(1f, 0.72f, 0.08f)),
                new StoryDialogueLine(0, "ЛИКА", "ДИСПЕТЧЕР ЛИГИ", "За каждым барьером ждёт пилот, которому запретили гоняться. Разнеси блокаду — и к финишу ты поедешь уже не один.", new Color(0.04f, 0.92f, 1f))
            }),
        new StoryChapterDefinition(
            "ТЁМНАЯ СТОРОНА", "ОРБИТА",
            "Лунный архив хранит доказательства подмены гонок. Передача исчезнет через пять минут.",
            "ТОП-3  /  БЫСТРЕЕ 05:00", 4, 1, "ЗАЕЗД НА ВРЕМЯ", 2, 4, 500, 3, 300f,
            StoryMissionType.TimeTrial, StoryVehicleType.Car, "VOLT S", 0f,
            new[]
            {
                new StoryDialogueLine(0, "ЛИКА", "ЛУННЫЙ РЕТРАНСЛЯТОР", "Архив открыт. Там имена всех, чьи победы украл Атлас — и запись последнего заезда Икара. Через пять минут система сожжёт файл вместе с ретранслятором.", new Color(0.04f, 0.92f, 1f)),
                new StoryDialogueLine(1, "РУК", "ГАРАЖНЫЙ КАНАЛ", "Я снял с VOLT S всё, что мешало скорости: ограничитель, гарантию и здравый смысл. Двигатель переживёт один заезд. Наверное.", new Color(1f, 0.72f, 0.08f)),
                new StoryDialogueLine(2, "НИКС", "ЗАШИФРОВАННЫЙ КАНАЛ", "Достанешь запись — покажу дорогу к ядру. И Лика наконец услышит, что её брат сказал перед исчезновением. Не опоздай.", new Color(1f, 0.08f, 0.62f))
            }),
        new StoryChapterDefinition(
            "ШТОРМОВОЙ КОНВОЙ", "ПОГОНЯ",
            "Ядро Лиги перевозят через порт в бронированной колонне. Внедрись в конвой на тяжёлом грузовике.",
            "ФИНИШИРУЙ В ТОП-2 НА ТЯЖЁЛОМ ГРУЗОВИКЕ", 5, 2, "ГРУЗОВОЙ КОНВОЙ", 3, 5, 575, 2, 0f,
            StoryMissionType.Truck, StoryVehicleType.Truck, "TITAN HAULER", 0f,
            new[]
            {
                new StoryDialogueLine(1, "РУК", "ПОРТОВЫЙ ГАРАЖ", "TITAN HAULER весит как дом и управляется как дом, который столкнули с холма. Хорошая новость: броневики Атласа легче.", new Color(1f, 0.72f, 0.08f)),
                new StoryDialogueLine(2, "НИКС", "КАНАЛ КОНВОЯ", "Я в головной машине. Не стреляй в кабину — там я. Контейнер отмечен розовым маяком. Да, я выбрала розовый. У Атласа с чувством юмора ещё хуже, чем у Рука.", new Color(1f, 0.08f, 0.62f)),
                new StoryDialogueLine(3, "АТЛАС", "КОМАНДНЫЙ КАНАЛ", "Предательство подтверждено. Никс, ваша замена уже назначена. Неизвестный пилот, для вас замена не требуется.", new Color(1f, 0.08f, 0.22f))
            }),
        new StoryChapterDefinition(
            "ХОЛОДНЫЙ РАСЧЁТ", "ДУЭЛЬ",
            "Последний верный Атласу чемпион защищает вход в ядро. Только чистая победа разрушит его легенду.",
            "ЗАЙМИ 1 МЕСТО", 6, 3, "ДУЭЛЬ", 2, 1, 700, 1, 0f,
            StoryMissionType.Duel, StoryVehicleType.Car, "TITAN GT", 0f,
            new[]
            {
                new StoryDialogueLine(2, "НИКС", "ЛИЧНЫЙ КАНАЛ", "Его зовут Вектор. Когда-то он вытащил меня из горящей машины, а потом отдал Атласу. Не знаю, кем он станет сегодня. Победи — и заставь его выбрать.", new Color(1f, 0.08f, 0.62f)),
                new StoryDialogueLine(0, "ЛИКА", "ДИСПЕТЧЕР ЛИГИ", "Город уже видел архив, но правда без победы звучит как оправдание проигравших. Нужен финиш, который Атлас не сможет стереть.", new Color(0.04f, 0.92f, 1f)),
                new StoryDialogueLine(3, "АТЛАС", "ПРЯМОЕ СОЕДИНЕНИЕ", "Вектор знает цену хаоса. А вы знаете только скорость. Ваша легенда закончится там, где начинается расчёт.", new Color(1f, 0.08f, 0.22f))
            }),
        new StoryChapterDefinition(
            "ПОСЛЕДНИЙ РАССВЕТ", "ФИНАЛ",
            "Команда выводит доказательства в эфир. Осталось выиграть последнюю гонку до полного отключения сети.",
            "1 МЕСТО  /  БЫСТРЕЕ 05:00", 7, 0, "ФИНАЛ", 4, 6, 1000, 1, 300f,
            StoryMissionType.Finale, StoryVehicleType.Car, "NEON R", 0f,
            new[]
            {
                new StoryDialogueLine(0, "ЛИКА", "ОБЩИЙ КАНАЛ ЛИГИ", "Весь город на этой частоте. Финишируй первым до отключения сети — и каждый увидит правду. Икар, если его запись ещё слышит нас... это для него.", new Color(0.04f, 0.92f, 1f)),
                new StoryDialogueLine(1, "РУК", "ГАРАЖНЫЙ КАНАЛ", "Я вернул NEON R передатчик Икара. Теперь машина говорит голосами всех, кого пытались стереть. Только, пожалуйста, пусть сегодня она говорит не через дым.", new Color(1f, 0.72f, 0.08f)),
                new StoryDialogueLine(2, "НИКС", "ОТКРЫТЫЙ КАНАЛ", "Охрану беру на себя. Ты бери финиш. Если выживем, первым делом устроим честную гонку. Вторым — поспорим, кто на самом деле спас город.", new Color(1f, 0.08f, 0.62f)),
                new StoryDialogueLine(3, "АТЛАС", "АВАРИЙНЫЙ КАНАЛ", "Свобода — это красивое название для аварии, которую никто не сможет остановить. Последний шанс: покиньте трассу.", new Color(1f, 0.08f, 0.22f))
            })
    };

    private bool storyModeOpen;
    private bool storyRaceActive;
    private bool storyMissionResolved;
    private bool storyMissionSucceeded;
    private int storySelectedChapter;
    private int storyUnlockedChapter;
    private int storyEarnedReward;
    private int storyFinishPosition;
    private string storyResultMessage = string.Empty;
    private bool storyDialogueOpen;
    private int storyDialogueIndex;
    private float storyDriftScore;
    private int storyObstacleSmashes;
    private Texture2D storyVehicleSheet;
    private Sprite storyMotorcycleSprite;
    private Sprite storyTruckSprite;
    private static bool pendingStoryRaceAfterReload;
    private static int pendingStoryChapterAfterReload;
    private static int pendingStoryCarAfterReload = -1;
    private static int storyReturnCarIndex = -1;
    private static int storyReturnTrackIndex = -1;

    private StoryChapterDefinition ActiveStoryChapter
    {
        get { return StoryChapters[Mathf.Clamp(storySelectedChapter, 0, StoryChapters.Length - 1)]; }
    }

    private int RaceLapTarget
    {
        get { return storyRaceActive ? ActiveStoryChapter.LapCount : TotalLaps; }
    }

    private float StoryAccelerationFactor
    {
        get
        {
            if (!storyRaceActive) return 1f;
            if (ActiveStoryChapter.VehicleType == StoryVehicleType.Motorcycle) return 1.2f;
            if (ActiveStoryChapter.VehicleType == StoryVehicleType.Truck) return 0.74f;
            return ActiveStoryChapter.MissionType == StoryMissionType.Drift ? 1.05f : 1f;
        }
    }

    private float StoryTopSpeedFactor
    {
        get
        {
            if (!storyRaceActive) return 1f;
            if (ActiveStoryChapter.VehicleType == StoryVehicleType.Motorcycle) return 1.16f;
            if (ActiveStoryChapter.VehicleType == StoryVehicleType.Truck) return 0.86f;
            return 1f;
        }
    }

    public float StoryVehicleMaximumSpeedKph
    {
        get
        {
            return (storyRaceActive && ActiveStoryChapter.VehicleType == StoryVehicleType.Truck) || IsHeavyTruckRace
                ? TruckMaximumSpeedKph
                : 0f;
        }
    }

    private float StoryHandlingFactor
    {
        get
        {
            if (!storyRaceActive) return 1f;
            if (ActiveStoryChapter.VehicleType == StoryVehicleType.Motorcycle) return 1.3f;
            if (ActiveStoryChapter.VehicleType == StoryVehicleType.Truck) return 0.62f;
            return ActiveStoryChapter.MissionType == StoryMissionType.Drift ? 1.2f : 1f;
        }
    }

    private float StoryDamageFactor
    {
        get
        {
            if (!storyRaceActive) return 1f;
            if (ActiveStoryChapter.VehicleType == StoryVehicleType.Motorcycle) return 1.22f;
            if (ActiveStoryChapter.VehicleType == StoryVehicleType.Truck) return 0.55f;
            return 1f;
        }
    }

    private int ActiveOpponentCount
    {
        get
        {
            int active = 0;
            for (int i = 0; i < opponents.Count; i++)
            {
                if (opponents[i] != null && opponents[i].gameObject.activeSelf)
                {
                    active++;
                }
            }
            return active;
        }
    }

    private void ConfigureRaceOpponents(int rivalCount)
    {
        int clamped = Mathf.Clamp(rivalCount, 0, opponents.Count);
        for (int i = 0; i < opponents.Count; i++)
        {
            if (opponents[i] != null)
            {
                opponents[i].gameObject.SetActive(i < clamped);
            }
        }
    }

    private void LoadStoryVehicleSprites()
    {
        storyVehicleSheet = Resources.Load<Texture2D>("UI/Story/StoryVehiclesPixel");
        if (storyVehicleSheet == null)
        {
            storyVehicleSheet = Resources.Load<Texture2D>("UI/Story/StoryVehicles");
        }
        if (storyVehicleSheet == null)
        {
            return;
        }

        storyVehicleSheet.filterMode = FilterMode.Point;
        storyVehicleSheet.wrapMode = TextureWrapMode.Clamp;
        float width = storyVehicleSheet.width;
        float height = storyVehicleSheet.height;
        storyMotorcycleSprite = Sprite.Create(
            storyVehicleSheet,
            new Rect(width * 0.09f, height * 0.02f, width * 0.23f, height * 0.96f),
            new Vector2(0.5f, 0.5f),
            256f);
        storyMotorcycleSprite.name = "Story VOLT BIKE X";
        storyTruckSprite = Sprite.Create(
            storyVehicleSheet,
            new Rect(width * 0.42f, height * 0.02f, width * 0.57f, height * 0.96f),
            new Vector2(0.5f, 0.5f),
            256f);
        storyTruckSprite.name = "Story TITAN HAULER";
    }

    private Sprite GetActiveStoryVehicleSprite()
    {
        if (IsMotorcycleRace)
        {
            return storyMotorcycleSprite;
        }

        if (IsHeavyTruckRace)
        {
            return storyTruckSprite;
        }

        if (!storyRaceActive)
        {
            return null;
        }

        return ActiveStoryChapter.VehicleType == StoryVehicleType.Motorcycle
            ? storyMotorcycleSprite
            : ActiveStoryChapter.VehicleType == StoryVehicleType.Truck
                ? storyTruckSprite
                : null;
    }

    private void ApplyStoryVehicleProfile()
    {
        if (player == null)
        {
            return;
        }

        StoryVehicleType vehicleType = storyRaceActive
            ? ActiveStoryChapter.VehicleType
            : IsMotorcycleRace ? StoryVehicleType.Motorcycle
            : IsHeavyTruckRace ? StoryVehicleType.Truck
            : StoryVehicleType.Car;
        Vector3 playerScale = CarScales[Mathf.Clamp(selectedCarIndex, 0, CarScales.Length - 1)];
        Vector2 colliderSize = new Vector2(0.82f, 1.58f);
        float mass = 1f;
        float linearDamping = 1.25f;
        float angularDamping = 4f;
        Vector2 damageCrackPosition = new Vector2(0f, 0.17f);
        float damageCrackScale = 1f;

        if (vehicleType == StoryVehicleType.Motorcycle)
        {
            // Keep the root scale uniform so the top-down motorcycle sprite
            // preserves its original proportions on the track.
            playerScale = new Vector3(1.04f, 1.04f, 1f);
            colliderSize = new Vector2(0.55f, 1.55f);
            mass = 0.62f;
            linearDamping = 0.82f;
            angularDamping = 2.8f;
        }
        else if (vehicleType == StoryVehicleType.Truck)
        {
            playerScale = new Vector3(1.18f, 1.18f, 1f);
            colliderSize = new Vector2(0.94f, 1.72f);
            mass = 2.7f;
            linearDamping = 1.65f;
            angularDamping = 6.8f;
            damageCrackPosition = new Vector2(0f, 0.54f);
            damageCrackScale = 0.68f;
        }

        player.transform.localScale = playerScale;
        if (playerDamage != null)
        {
            playerDamage.ConfigureDamageOverlayLayout(damageCrackPosition, damageCrackScale);
        }
        BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
        if (playerCollider != null) playerCollider.size = colliderSize;
        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
        {
            playerBody.mass = mass;
            playerBody.linearDamping = linearDamping;
            playerBody.angularDamping = angularDamping;
        }

        for (int i = 0; i < opponents.Count; i++)
        {
            CircuitAI opponent = opponents[i];
            if (opponent == null) continue;
            int rivalCarIndex = (i + 1) % CarScales.Length;
            Sprite specialVehicleSprite = GetActiveStoryVehicleSprite();
            Sprite rivalSprite = specialVehicleSprite ?? GetTrackCarSprite(rivalCarIndex);
            Transform rivalBodyPart = opponent.transform.Find("Body");
            if (rivalBodyPart != null)
            {
                SpriteRenderer rivalRenderer = rivalBodyPart.GetComponent<SpriteRenderer>();
                if (rivalRenderer != null)
                {
                    rivalRenderer.sprite = rivalSprite;
                    if (specialVehicleSprite != null)
                    {
                        rivalRenderer.color = Color.white;
                    }
                }
                if (rivalSprite != null)
                {
                    rivalBodyPart.localScale = GetTrackCarVisualScale(rivalSprite);
                }
            }
            CarDamage rivalDamage = opponent.GetComponent<CarDamage>();
            if (rivalDamage != null)
            {
                rivalDamage.ConfigureSprites(
                    rivalSprite,
                    specialVehicleSprite != null ? null : GetTrackBrokenCarSprite(rivalCarIndex),
                    specialVehicleSprite != null ? null : GetTrackBrokenCarVariant2Sprite(rivalCarIndex));
                rivalDamage.ConfigureDamageOverlayLayout(damageCrackPosition, damageCrackScale);
            }
            opponent.transform.localScale = vehicleType == StoryVehicleType.Motorcycle
                ? new Vector3(1.04f, 1.04f, 1f)
                : vehicleType == StoryVehicleType.Truck
                    ? new Vector3(1.18f, 1.18f, 1f)
                    : CarScales[rivalCarIndex];

            BoxCollider2D rivalCollider = opponent.GetComponent<BoxCollider2D>();
            if (rivalCollider != null) rivalCollider.size = colliderSize;
            Rigidbody2D rivalBody = opponent.GetComponent<Rigidbody2D>();
            if (rivalBody != null)
            {
                rivalBody.mass = mass;
                rivalBody.linearDamping = vehicleType == StoryVehicleType.Car ? 0.82f : linearDamping;
                rivalBody.angularDamping = vehicleType == StoryVehicleType.Car ? 4.2f : angularDamping;
            }
        }
    }

    private void LoadStoryProgress()
    {
        storyUnlockedChapter = Mathf.Clamp(PlayerPrefs.GetInt(StoryUnlockedKey, 0), 0, StoryChapters.Length - 1);
        storySelectedChapter = storyUnlockedChapter;
    }

    private void OpenStoryMode()
    {
        CaptureFreeRaceSelection();
        mainMenuModeSelectionOpen = false;
        storyModeOpen = true;
        storyDialogueOpen = false;
        storyDialogueIsDebrief = false;
        garageOpen = false;
        storySelectedChapter = Mathf.Clamp(storyUnlockedChapter, 0, StoryChapters.Length - 1);
        SelectTrack(ActiveStoryChapter.TrackIndex);
        menuAnimationStartedAt = Time.unscaledTime;
    }

    private void CloseStoryMode()
    {
        storyModeOpen = false;
        storyDialogueOpen = false;
        storyDialogueIsDebrief = false;
        RestoreFreeRaceSelection();
        menuAnimationStartedAt = Time.unscaledTime;
    }

    private void CaptureFreeRaceSelection()
    {
        if (storyReturnCarIndex < 0)
        {
            storyReturnCarIndex = selectedCarIndex;
        }
        if (storyReturnTrackIndex < 0)
        {
            storyReturnTrackIndex = selectedTrackIndex;
        }
    }

    private void RestoreFreeRaceSelection()
    {
        if (opponents.Count > 0)
        {
            ConfigureRaceOpponents(opponents.Count);
        }

        if (storyReturnCarIndex >= 0)
        {
            selectedCarIndex = Mathf.Clamp(storyReturnCarIndex, 0, CarNames.Length - 1);
            garageCarIndex = selectedCarIndex;
            PlayerPrefs.SetInt(SelectedCarKey, selectedCarIndex);
            ApplySelectedCarVisuals();
        }

        if (storyReturnTrackIndex >= 0)
        {
            SelectTrack(storyReturnTrackIndex);
            PlayerPrefs.SetInt(TrackKey, selectedTrackIndex);
        }

        storyReturnCarIndex = -1;
        storyReturnTrackIndex = -1;
        PlayerPrefs.Save();
    }

    private void PreparePendingStoryRaceBeforeWorldBuild()
    {
        if (!startRaceAfterSceneReload || !pendingStoryRaceAfterReload)
        {
            return;
        }

        storySelectedChapter = Mathf.Clamp(pendingStoryChapterAfterReload, 0, StoryChapters.Length - 1);
        storyRaceActive = true;
        int chapterCar = pendingStoryCarAfterReload >= 0
            ? pendingStoryCarAfterReload
            : ActiveStoryChapter.CarIndex;
        selectedCarIndex = Mathf.Clamp(chapterCar, 0, CarNames.Length - 1);
        garageCarIndex = selectedCarIndex;
    }

    private void SelectStoryChapter(int chapterIndex)
    {
        int clamped = Mathf.Clamp(chapterIndex, 0, StoryChapters.Length - 1);
        if (clamped > storyUnlockedChapter)
        {
            return;
        }

        storySelectedChapter = clamped;
        SelectTrack(ActiveStoryChapter.TrackIndex);
    }

    private void StartSelectedStoryChapter()
    {
        if (trackLoadPending || storySelectedChapter > storyUnlockedChapter)
        {
            return;
        }

        if (ActiveStoryChapter.Dialogue.Length > 0)
        {
            storyDialogueIsDebrief = false;
            storyDialogueOpen = true;
            storyDialogueIndex = 0;
            menuAnimationStartedAt = Time.unscaledTime;
            PlayMenuClickSfx();
            return;
        }

        LaunchSelectedStoryChapter();
    }

    private void LaunchSelectedStoryChapter()
    {
        if (trackLoadPending || storySelectedChapter > storyUnlockedChapter)
        {
            return;
        }

        StoryChapterDefinition chapter = ActiveStoryChapter;
        CaptureFreeRaceSelection();
        SelectTrack(chapter.TrackIndex);
        selectedCarIndex = Mathf.Clamp(chapter.CarIndex, 0, CarNames.Length - 1);
        garageCarIndex = selectedCarIndex;
        if (opponents.Count > 0)
        {
            ConfigureRaceOpponents(chapter.RivalCount);
        }
        PlayMenuStartSfx();
        PlayerPrefs.SetInt(TrackKey, selectedTrackIndex);
        PlayerPrefs.Save();

        storyRaceActive = true;
        storyModeOpen = false;
        storyDialogueOpen = false;
        storyDialogueIsDebrief = false;
        ResetStoryMissionResult();

        if (builtTrackIndex != selectedTrackIndex)
        {
            trackLoadPending = true;
            startRaceAfterSceneReload = true;
            pendingStoryRaceAfterReload = true;
            pendingStoryChapterAfterReload = storySelectedChapter;
            pendingStoryCarAfterReload = selectedCarIndex;
            StartCoroutine(LoadSelectedTrackForRace());
            return;
        }

        pendingStoryCarAfterReload = -1;
        BeginRaceNow();
    }

    private void AdvanceStoryDialogue()
    {
        if (!storyDialogueOpen)
        {
            return;
        }

        StoryDialogueLine[] dialogue = ActiveStoryDialogue;
        if (storyDialogueIndex + 1 < dialogue.Length)
        {
            storyDialogueIndex++;
            PlayMenuClickSfx();
            return;
        }

        storyDialogueOpen = false;
        if (storyDialogueIsDebrief)
        {
            CompleteStoryDebrief();
        }
        else
        {
            LaunchSelectedStoryChapter();
        }
    }

    private void RestorePendingStoryRaceAfterReload()
    {
        storyRaceActive = pendingStoryRaceAfterReload;
        if (pendingStoryRaceAfterReload)
        {
            storySelectedChapter = Mathf.Clamp(pendingStoryChapterAfterReload, 0, StoryChapters.Length - 1);
            int chapterCar = pendingStoryCarAfterReload >= 0
                ? pendingStoryCarAfterReload
                : ActiveStoryChapter.CarIndex;
            selectedCarIndex = Mathf.Clamp(chapterCar, 0, CarNames.Length - 1);
            garageCarIndex = selectedCarIndex;
            ConfigureRaceOpponents(ActiveStoryChapter.RivalCount);
        }

        pendingStoryRaceAfterReload = false;
        pendingStoryChapterAfterReload = 0;
        pendingStoryCarAfterReload = -1;
        ResetStoryMissionResult();
    }

    private void ResetStoryMissionResult()
    {
        storyMissionResolved = false;
        storyMissionSucceeded = false;
        storyEarnedReward = 0;
        storyFinishPosition = 0;
        storyResultMessage = string.Empty;
        storyDriftScore = 0f;
        storyObstacleSmashes = 0;
    }

    private void UpdateStoryMissionProgress()
    {
        if (!storyRaceActive || !raceStarted || raceFinished || player == null)
        {
            return;
        }

        if (ActiveStoryChapter.MissionType == StoryMissionType.Drift && player.IsDrifting)
        {
            storyDriftScore += Time.deltaTime * (48f + player.DriftCombo * 1.35f);
        }
    }

    public void RegisterStoryObstacleSmashed()
    {
        if (storyRaceActive && !storyMissionResolved && ActiveStoryChapter.MissionType == StoryMissionType.Smash)
        {
            storyObstacleSmashes++;
        }
    }

    private void ResolveStoryRaceAtFinish()
    {
        if (!storyRaceActive || storyMissionResolved)
        {
            return;
        }

        StoryChapterDefinition chapter = ActiveStoryChapter;
        storyFinishPosition = RecordedFinishPosition;
        bool positionPassed = storyFinishPosition <= chapter.MaximumPosition;
        bool timePassed = chapter.TimeLimit <= 0f || finishTime <= chapter.TimeLimit;
        bool specialPassed = chapter.MissionType == StoryMissionType.Drift
            ? storyDriftScore >= chapter.ObjectiveTarget
            : chapter.MissionType == StoryMissionType.Smash
                ? storyObstacleSmashes >= Mathf.RoundToInt(chapter.ObjectiveTarget)
                : true;
        storyMissionSucceeded = positionPassed && timePassed && specialPassed && !playerWrecked;
        storyMissionResolved = true;

        if (!specialPassed)
        {
            storyResultMessage = chapter.MissionType == StoryMissionType.Drift
                ? "ЦЕЛЬ НЕ ВЫПОЛНЕНА: ДРИФТ " + Mathf.RoundToInt(storyDriftScore) + " / " + Mathf.RoundToInt(chapter.ObjectiveTarget)
                : "ЦЕЛЬ НЕ ВЫПОЛНЕНА: ПРЕПЯТСТВИЯ " + storyObstacleSmashes + " / " + Mathf.RoundToInt(chapter.ObjectiveTarget);
            return;
        }

        if (!positionPassed)
        {
            storyResultMessage = "ЦЕЛЬ НЕ ВЫПОЛНЕНА: НУЖНА ПОЗИЦИЯ " + chapter.MaximumPosition + " ИЛИ ВЫШЕ";
            return;
        }

        if (!timePassed)
        {
            storyResultMessage = "ЦЕЛЬ НЕ ВЫПОЛНЕНА: ПРЕВЫШЕНО ВРЕМЯ МИССИИ";
            return;
        }

        bool firstCompletion = !IsStoryChapterCompleted(storySelectedChapter);
        PlayerPrefs.SetInt(StoryCompletedPrefix + storySelectedChapter, 1);
        bool receivedUniqueReward = GrantStoryChapterUnlock(storySelectedChapter);
        if (storySelectedChapter < StoryChapters.Length - 1)
        {
            storyUnlockedChapter = Mathf.Max(storyUnlockedChapter, storySelectedChapter + 1);
            PlayerPrefs.SetInt(StoryUnlockedKey, storyUnlockedChapter);
        }

        storyEarnedReward = firstCompletion ? chapter.Reward : 0;
        storyResultMessage = firstCompletion
            ? "ГЛАВА ПРОЙДЕНА  /  " + GetStoryRewardTitle(storySelectedChapter) + " ОТКРЫТО"
            : "ГЛАВА ПРОЙДЕНА ПОВТОРНО";

        if (storyEarnedReward > 0)
        {
            AddCoins(storyEarnedReward);
        }
        else if (receivedUniqueReward || firstCompletion)
        {
            PlayerPrefs.Save();
        }
    }

    private void ResolveStoryRaceAsFailed(string reason)
    {
        if (!storyRaceActive || storyMissionResolved)
        {
            return;
        }

        storyMissionResolved = true;
        storyMissionSucceeded = false;
        storyFinishPosition = RecordedFinishPosition;
        storyResultMessage = reason;
    }

    private bool IsStoryChapterCompleted(int chapterIndex)
    {
        return PlayerPrefs.GetInt(StoryCompletedPrefix + Mathf.Clamp(chapterIndex, 0, StoryChapters.Length - 1), 0) == 1;
    }

    private int CompletedStoryChapterCount()
    {
        int completed = 0;
        for (int i = 0; i < StoryChapters.Length; i++)
        {
            if (IsStoryChapterCompleted(i))
            {
                completed++;
            }
        }

        return completed;
    }

    private void StartNextStoryChapter()
    {
        if (!storyMissionSucceeded)
        {
            OpenMainMenu();
            storyModeOpen = true;
            return;
        }

        OpenStoryDebrief();
    }

    private void UpdateStoryMenuInput()
    {
        if (storyDialogueOpen)
        {
            if (CancelPressed())
            {
                CancelActiveStoryDialogue();
            }
            else if (StoryPreviousPressed())
            {
                storyDialogueIndex = Mathf.Max(0, storyDialogueIndex - 1);
                PlayMenuClickSfx();
            }
            else if (StoryNextPressed() || ConfirmPressed())
            {
                AdvanceStoryDialogue();
            }
            return;
        }

        if (CancelPressed())
        {
            CloseStoryMode();
            return;
        }

        if (StoryPreviousPressed())
        {
            SelectStoryChapter(storySelectedChapter - 1);
        }
        else if (StoryNextPressed())
        {
            SelectStoryChapter(Mathf.Min(storySelectedChapter + 1, storyUnlockedChapter));
        }
        else if (ConfirmPressed())
        {
            StartSelectedStoryChapter();
        }
    }

    private bool StoryPreviousPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
#endif
    }

    private bool StoryNextPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
#endif
    }
}
