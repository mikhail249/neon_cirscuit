using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum CarWeaponType
{
    NeonRocket,
    PlasmaBlaster
}

public sealed class PlayerWeaponSystem : MonoBehaviour
{
    private const int MaximumAmmo = 9;
    private const float RocketFireCooldown = 0.42f;
    private const float PlasmaFireCooldown = 0.2f;

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
    public int MaxAmmo { get { return playerControlled && game != null ? game.PlayerWeaponMaxAmmo : MaximumAmmo; } }
    public float DamageMultiplier { get { return playerControlled && game != null ? game.PlayerWeaponDamageMultiplier : 1f; } }
    public CarWeaponType ActiveWeapon { get { return activeWeapon; } }
    public string ActiveWeaponName { get { return activeWeapon == CarWeaponType.PlasmaBlaster ? "ПЛАЗМА-БЛАСТЕР" : "НЕОН-РАКЕТЫ"; } }
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
    }

    private void Update()
    {
        if (!playerControlled || game == null || !game.RaceStarted || game.RaceFinished || Time.timeScale <= 0f)
        {
            return;
        }

        if (SwitchWeaponPressed())
        {
            activeWeapon = activeWeapon == CarWeaponType.NeonRocket
                ? CarWeaponType.PlasmaBlaster
                : CarWeaponType.NeonRocket;
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
        if (ammo <= 0 || Time.time < nextFireTime || damage == null || damage.IsBroken)
        {
            return false;
        }

        CarWeaponType weaponToFire = activeWeapon;
        if (!playerControlled && preferredTarget != null)
        {
            float targetDistance = Vector2.Distance(transform.position, preferredTarget.position);
            weaponToFire = targetDistance <= 9f ? CarWeaponType.PlasmaBlaster : CarWeaponType.NeonRocket;
        }

        ammo--;
        float cooldownMultiplier = playerControlled && game != null ? game.PlayerWeaponCooldownMultiplier : 1f;
        nextFireTime = Time.time + (weaponToFire == CarWeaponType.PlasmaBlaster ? PlasmaFireCooldown : RocketFireCooldown) * cooldownMultiplier;
        if (weaponToFire == CarWeaponType.PlasmaBlaster)
        {
            CreatePlasmaBolt();
        }
        else
        {
            CreateRocket();
        }

        if (playerControlled && game != null)
        {
            game.PlayWeaponSfx(weaponToFire);
        }

        return true;
    }

    public void ResetWeapon()
    {
        ammo = 0;
        activeWeapon = CarWeaponType.NeonRocket;
        nextFireTime = 0f;
        nextPickupTime = 0f;
        pickupFlashUntil = 0f;

        NeonRocket[] rockets = FindObjectsByType<NeonRocket>(FindObjectsSortMode.None);
        for (int i = 0; i < rockets.Length; i++)
        {
            if (rockets[i].IsOwnedBy(gameObject))
            {
                Destroy(rockets[i].gameObject);
            }
        }

        NeonPlasmaBolt[] plasmaBolts = FindObjectsByType<NeonPlasmaBolt>(FindObjectsSortMode.None);
        for (int i = 0; i < plasmaBolts.Length; i++)
        {
            if (plasmaBolts[i].IsOwnedBy(gameObject))
            {
                Destroy(plasmaBolts[i].gameObject);
            }
        }
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
