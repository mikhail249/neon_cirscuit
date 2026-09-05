using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum CarWeaponType
{
    NeonRocket,
    PlasmaBlaster,
    EchoArc,
    OrbitMine,
    IcarLance,
    PhantomSwarm
}

public sealed class PlayerWeaponSystem : MonoBehaviour
{
    private const int MaximumAmmo = 9;
    private const float RocketFireCooldown = 0.42f;
    private const float PlasmaFireCooldown = 0.2f;
    private const float EchoArcFireCooldown = 0.72f;
    private const float OrbitMineFireCooldown = 1.15f;
    private const float IcarLanceFireCooldown = 0.9f;
    private const float PhantomSwarmFireCooldown = 4.8f;

    private NeonCircuitGame game;
    private Rigidbody2D body;
    private Sprite pixelSprite;
    private Sprite circleSprite;
    private bool playerControlled;
    private int ammo;
    private CarWeaponType activeWeapon;
    private float nextFireTime;
    private float nextPickupTime;
    private float pickupFlashUntil;

    public int Ammo { get { return ammo; } }
    public int ShotsFired { get; private set; }
    public int MaxAmmo { get { return playerControlled && game != null ? game.PlayerWeaponMaxAmmo : MaximumAmmo; } }
    public float DamageMultiplier { get { return playerControlled && game != null ? game.PlayerWeaponDamageMultiplier : 1f; } }
    public CarWeaponType ActiveWeapon { get { return activeWeapon; } }
    public string ActiveWeaponName
    {
        get
        {
            switch (activeWeapon)
            {
                case CarWeaponType.PlasmaBlaster: return "ПЛАЗМА-БЛАСТЕР";
                case CarWeaponType.EchoArc: return "ЭХО-ДУГА";
                case CarWeaponType.OrbitMine: return "ОРБИТАЛЬНАЯ МИНА";
                case CarWeaponType.IcarLance: return "КОПЬЁ ИКАРА";
                case CarWeaponType.PhantomSwarm: return "РОЙ ФАНТОМОВ";
                default: return "НЕОН-РАКЕТЫ";
            }
        }
    }
    public int ActiveWeaponAmmoCost { get { return GetAmmoCost(activeWeapon); } }
    public bool CanFire
    {
        get
        {
            CarDamage damage = GetComponent<CarDamage>();
            return ammo >= ActiveWeaponAmmoCost && Time.time >= nextFireTime && damage != null && !damage.IsBroken;
        }
    }
    public bool IsPlayerControlled { get { return playerControlled; } }
    public bool PickupFlashActive { get { return Time.unscaledTime < pickupFlashUntil; } }

    public void Initialize(
        NeonCircuitGame owner,
        Rigidbody2D rigidbody,
        Sprite projectileSprite,
        Sprite projectileCircleSprite,
        bool usePlayerInput = true)
    {
        game = owner;
        body = rigidbody;
        pixelSprite = projectileSprite;
        circleSprite = projectileCircleSprite;
        playerControlled = usePlayerInput;
        activeWeapon = playerControlled && game != null
            ? game.SelectedWeaponType
            : CarWeaponType.NeonRocket;
    }

    private void Update()
    {
        if (!playerControlled || game == null || !game.RaceStarted || game.RaceFinished || Time.timeScale <= 0f)
        {
            return;
        }

        if (SwitchWeaponPressed())
        {
            activeWeapon = game.GetNextUnlockedWeapon(activeWeapon);
        }

        if (FirePressed())
        {
            TryFire();
        }
    }

    public bool TryAddAmmo(int amount)
    {
        if (amount <= 0 || ammo >= MaxAmmo || Time.time < nextPickupTime)
        {
            return false;
        }

        nextPickupTime = Time.time + 0.12f;
        ammo = Mathf.Min(MaxAmmo, ammo + amount);
        pickupFlashUntil = Time.unscaledTime + 1.25f;
        if (playerControlled && game != null)
        {
            game.PlayPickupSfx(false);
        }
        return true;
    }

    public bool TryFire(Transform preferredTarget = null)
    {
        CarDamage damage = GetComponent<CarDamage>();
        if (Time.time < nextFireTime || damage == null || damage.IsBroken)
        {
            return false;
        }

        CarWeaponType weaponToFire = activeWeapon;
        if (!playerControlled && preferredTarget != null)
        {
            float targetDistance = Vector2.Distance(transform.position, preferredTarget.position);
            weaponToFire = targetDistance <= 9f ? CarWeaponType.PlasmaBlaster : CarWeaponType.NeonRocket;
        }

        int ammoCost = GetAmmoCost(weaponToFire);
        if (ammo < ammoCost)
        {
            return false;
        }

        ammo -= ammoCost;
        float cooldownMultiplier = playerControlled && game != null ? game.PlayerWeaponCooldownMultiplier : 1f;
        nextFireTime = Time.time + GetFireCooldown(weaponToFire) * cooldownMultiplier;
        switch (weaponToFire)
        {
            case CarWeaponType.PlasmaBlaster:
                CreatePlasmaBolt();
                break;
            case CarWeaponType.EchoArc:
                CreateEchoArc();
                break;
            case CarWeaponType.OrbitMine:
                CreateOrbitMine();
                break;
            case CarWeaponType.IcarLance:
                CreateIcarLance();
                break;
            case CarWeaponType.PhantomSwarm:
                CreatePhantomSwarm();
                break;
            default:
                CreateRocket();
                break;
        }

        if (playerControlled && game != null)
        {
            game.PlayWeaponSfx(weaponToFire);
        }

        ShotsFired++;
        return true;
    }

    public void ResetWeapon()
    {
        ShotsFired = 0;
        ammo = 0;
        activeWeapon = playerControlled && game != null
            ? game.SelectedWeaponType
            : CarWeaponType.NeonRocket;
        nextFireTime = 0f;
        nextPickupTime = 0f;
        pickupFlashUntil = 0f;

        NeonRocket[] rockets = FindObjectsByType<NeonRocket>();
        for (int i = 0; i < rockets.Length; i++)
        {
            if (rockets[i].IsOwnedBy(gameObject))
            {
                Destroy(rockets[i].gameObject);
            }
        }

        NeonPlasmaBolt[] plasmaBolts = FindObjectsByType<NeonPlasmaBolt>();
        for (int i = 0; i < plasmaBolts.Length; i++)
        {
            if (plasmaBolts[i].IsOwnedBy(gameObject))
            {
                Destroy(plasmaBolts[i].gameObject);
            }
        }

        DestroyOwnedEffects(FindObjectsByType<NeonEchoArc>());
        DestroyOwnedEffects(FindObjectsByType<NeonOrbitMine>());
        DestroyOwnedEffects(FindObjectsByType<NeonIcarLance>());
        DestroyOwnedEffects(FindObjectsByType<NeonPhantomSwarm>());
    }

    public void EquipWeapon(CarWeaponType weaponType)
    {
        if (!playerControlled || game == null || !game.IsWeaponUnlocked(weaponType))
        {
            return;
        }

        activeWeapon = weaponType;
    }

    private void CreateRocket()
    {
        GameObject rocketObject = new GameObject(playerControlled ? "Player Neon Rocket" : "Rival Neon Rocket");
        rocketObject.transform.SetParent(game.transform);
        rocketObject.transform.position = transform.position + transform.up * 1.22f;
        rocketObject.transform.rotation = transform.rotation;

        Rigidbody2D rocketBody = rocketObject.AddComponent<Rigidbody2D>();
        rocketBody.gravityScale = 0f;
        rocketBody.linearDamping = 0f;
        rocketBody.angularDamping = 0f;
        rocketBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rocketBody.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D rocketCollider = rocketObject.AddComponent<CircleCollider2D>();
        rocketCollider.radius = 0.2f;
        rocketCollider.isTrigger = true;

        Collider2D ownerCollider = GetComponent<Collider2D>();
        if (ownerCollider != null)
        {
            Physics2D.IgnoreCollision(rocketCollider, ownerCollider, true);
        }

        NeonRocket rocket = rocketObject.AddComponent<NeonRocket>();
        Vector2 launchVelocity = (Vector2)transform.up * 19f + body.linearVelocity * 0.35f;
        rocket.Initialize(gameObject, launchVelocity, pixelSprite, circleSprite, DamageMultiplier);
    }

    private void CreatePlasmaBolt()
    {
        GameObject boltObject = new GameObject(playerControlled ? "Player Plasma Bolt" : "Rival Plasma Bolt");
        boltObject.transform.SetParent(game.transform);
        boltObject.transform.position = transform.position + transform.up * 1.18f;
        boltObject.transform.rotation = transform.rotation;

        Rigidbody2D boltBody = boltObject.AddComponent<Rigidbody2D>();
        boltBody.gravityScale = 0f;
        boltBody.linearDamping = 0f;
        boltBody.angularDamping = 0f;
        boltBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        boltBody.interpolation = RigidbodyInterpolation2D.Interpolate;

        CircleCollider2D boltCollider = boltObject.AddComponent<CircleCollider2D>();
        boltCollider.radius = 0.16f;
        boltCollider.isTrigger = true;

        Collider2D ownerCollider = GetComponent<Collider2D>();
        if (ownerCollider != null)
        {
            Physics2D.IgnoreCollision(boltCollider, ownerCollider, true);
        }

        NeonPlasmaBolt bolt = boltObject.AddComponent<NeonPlasmaBolt>();
        Vector2 launchVelocity = (Vector2)transform.up * 30f + body.linearVelocity * 0.18f;
        bolt.Initialize(gameObject, launchVelocity, pixelSprite, circleSprite, DamageMultiplier);
    }

    private void CreateEchoArc()
    {
        GameObject arcObject = new GameObject("Player Echo Arc");
        arcObject.transform.SetParent(game.transform);
        NeonEchoArc arc = arcObject.AddComponent<NeonEchoArc>();
        arc.Initialize(gameObject, transform.position + transform.up * 0.72f, transform.up, pixelSprite, circleSprite, DamageMultiplier);
    }

    private void CreateOrbitMine()
    {
        GameObject mineObject = new GameObject("Player Orbital Mine");
        mineObject.transform.SetParent(game.transform);
        mineObject.transform.position = transform.position - transform.up * 1.38f;
        mineObject.transform.rotation = transform.rotation;
        NeonOrbitMine mine = mineObject.AddComponent<NeonOrbitMine>();
        mine.Initialize(gameObject, body != null ? body.linearVelocity * 0.18f : Vector2.zero, pixelSprite, circleSprite, DamageMultiplier);
    }

    private void CreateIcarLance()
    {
        GameObject lanceObject = new GameObject("Player Icar Lance");
        lanceObject.transform.SetParent(game.transform);
        NeonIcarLance lance = lanceObject.AddComponent<NeonIcarLance>();
        lance.Initialize(gameObject, transform.position + transform.up * 0.82f, transform.up, pixelSprite, circleSprite, DamageMultiplier);
    }

    private void CreatePhantomSwarm()
    {
        GameObject swarmObject = new GameObject("Player Phantom Swarm");
        swarmObject.transform.SetParent(game.transform);
        NeonPhantomSwarm swarm = swarmObject.AddComponent<NeonPhantomSwarm>();
        swarm.Initialize(gameObject, pixelSprite, circleSprite, DamageMultiplier);
    }

    private static int GetAmmoCost(CarWeaponType weaponType)
    {
        switch (weaponType)
        {
            case CarWeaponType.OrbitMine: return 2;
            case CarWeaponType.IcarLance: return 3;
            case CarWeaponType.PhantomSwarm: return 2;
            default: return 1;
        }
    }

    private static float GetFireCooldown(CarWeaponType weaponType)
    {
        switch (weaponType)
        {
            case CarWeaponType.PlasmaBlaster: return PlasmaFireCooldown;
            case CarWeaponType.EchoArc: return EchoArcFireCooldown;
            case CarWeaponType.OrbitMine: return OrbitMineFireCooldown;
            case CarWeaponType.IcarLance: return IcarLanceFireCooldown;
            case CarWeaponType.PhantomSwarm: return PhantomSwarmFireCooldown;
            default: return RocketFireCooldown;
        }
    }

    private void DestroyOwnedEffects<T>(T[] effects) where T : Component
    {
        for (int i = 0; i < effects.Length; i++)
        {
            Component effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            bool owned = false;
            if (effect is NeonEchoArc arc) owned = arc.IsOwnedBy(gameObject);
            else if (effect is NeonOrbitMine mine) owned = mine.IsOwnedBy(gameObject);
            else if (effect is NeonIcarLance lance) owned = lance.IsOwnedBy(gameObject);
            else if (effect is NeonPhantomSwarm swarm) owned = swarm.IsOwnedBy(gameObject);

            if (owned)
            {
                Destroy(effect.gameObject);
            }
        }
    }

    private bool FirePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private bool SwitchWeaponPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.qKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Q);
#endif
    }
}
