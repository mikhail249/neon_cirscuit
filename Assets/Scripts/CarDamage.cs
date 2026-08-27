using UnityEngine;

public sealed class CarDamage : MonoBehaviour
{
    public const float MaxHealth = 200f;
    private const float LightTouchThreshold = 0.45f;
    private const float StrongImpactThreshold = 3.2f;
    private const float HitCooldown = 0.24f;
    private const float DamagedVisualThreshold = 0.4f;
    private const float SmokeVisualThreshold = 0.38f;
    private const int MaximumDentMarks = 10;

    private static Sprite dentDecalSprite;
    private static Sprite crackDecalSprite;
    private static Sprite solidPixelSprite;
    private static Sprite smokePlumeSprite;

    private NeonCircuitGame game;
    private Rigidbody2D body;
    private SpriteRenderer bodyRenderer;
    private Transform healthBarRoot;
    private SpriteRenderer healthBack;
    private SpriteRenderer healthFill;
    private SpriteRenderer smoke;
    private SpriteRenderer glassCrack;
    private SpriteRenderer frontLightDamage;
    private SpriteRenderer rearLightDamage;
    private SpriteMask damageClipMask;
    private Sprite normalBodySprite;
    private Sprite brokenBodySprite;
    private Sprite brokenBodySpriteVariant2;
    private Color originalBodyColor;
    private Vector3 originalBodyLocalScale;
    private Vector3 originalBodyLocalPosition;
    private Quaternion originalBodyLocalRotation;
    private readonly SpriteRenderer[] dentMarks = new SpriteRenderer[MaximumDentMarks];
    private float health = MaxHealth;
    private float lastHitTime = -10f;
    private float frontDent;
    private float rearDent;
    private float leftDent;
    private float rightDent;
    private float lastFragmentTime = -10f;
    private int nextDentMark;
    private bool frontPartDetached;
    private bool rearPartDetached;
    private bool leftPartDetached;
    private bool rightPartDetached;
    private bool playerCar;
    private bool broken;

    public float Health { get { return health; } }
    public bool IsBroken { get { return broken; } }
    public bool IsPlayerCar { get { return playerCar; } }
    public float EnginePerformance
    {
        get
        {
            float damage = 1f - Mathf.Clamp01(health / MaxHealth);
            return Mathf.Lerp(1f, 0.68f, Mathf.InverseLerp(0.32f, 1f, damage));
        }
    }
    public float MaximumSpeedPerformance
    {
        get
        {
            float damage = 1f - Mathf.Clamp01(health / MaxHealth);
            return Mathf.Lerp(1f, 0.78f, Mathf.InverseLerp(0.42f, 1f, damage));
        }
    }
    public float SteeringPerformance
    {
        get
        {
            float damage = 1f - Mathf.Clamp01(health / MaxHealth);
            float asymmetricDamage = Mathf.Abs(leftDent - rightDent) + Mathf.Abs(frontDent - rearDent) * 0.45f;
            return Mathf.Clamp(Mathf.Lerp(1f, 0.74f, Mathf.InverseLerp(0.38f, 1f, damage)) - asymmetricDamage * 0.42f, 0.62f, 1f);
        }
    }
    public float GripPerformance
    {
        get
        {
            float damage = 1f - Mathf.Clamp01(health / MaxHealth);
            return Mathf.Lerp(1f, 0.72f, Mathf.InverseLerp(0.48f, 1f, damage));
        }
    }
    public float SteeringPull { get { return Mathf.Clamp((rightDent - leftDent) * 0.58f, -0.09f, 0.09f); } }

    public void Initialize(NeonCircuitGame owner, bool isPlayer, Sprite destroyedSprite = null, Sprite destroyedSpriteVariant2 = null)
    {
        game = owner;
        playerCar = isPlayer;
        body = GetComponent<Rigidbody2D>();

        Transform bodyVisual = transform.Find("Body");
        if (bodyVisual != null)
        {
            bodyRenderer = bodyVisual.GetComponent<SpriteRenderer>();
        }

        if (bodyRenderer != null)
        {
            normalBodySprite = bodyRenderer.sprite;
            brokenBodySprite = destroyedSprite;
            brokenBodySpriteVariant2 = destroyedSpriteVariant2;
            originalBodyColor = bodyRenderer.color;
            originalBodyLocalScale = bodyRenderer.transform.localScale;
            originalBodyLocalPosition = bodyRenderer.transform.localPosition;
            originalBodyLocalRotation = bodyRenderer.transform.localRotation;
            CreateDamageVisuals(bodyRenderer.sprite, bodyRenderer.sortingOrder);
        }

        UpdateVisuals();
    }

    public void ConfigureSprites(Sprite normalSprite, Sprite destroyedSprite, Sprite destroyedSpriteVariant2 = null)
    {
        if (normalSprite != null)
        {
            normalBodySprite = normalSprite;
        }

        brokenBodySprite = destroyedSprite;
        brokenBodySpriteVariant2 = destroyedSpriteVariant2;

        if (bodyRenderer == null)
        {
            return;
        }

        originalBodyColor = bodyRenderer.color;
        if (!broken && health >= MaxHealth - 0.01f)
        {
            originalBodyLocalScale = bodyRenderer.transform.localScale;
            originalBodyLocalPosition = bodyRenderer.transform.localPosition;
            originalBodyLocalRotation = bodyRenderer.transform.localRotation;
        }
        if (!broken && normalBodySprite != null)
        {
            bodyRenderer.sprite = normalBodySprite;
        }
        if (damageClipMask != null && normalBodySprite != null)
        {
            damageClipMask.sprite = normalBodySprite;
        }
    }

    private void CreateDamageVisuals(Sprite sprite, int sortingOrder)
    {
        CreateDamageClipMask(sprite, sortingOrder);

        if (!playerCar)
        {
            GameObject healthBarObject = new GameObject("Health Bar");
            healthBarObject.transform.SetParent(transform);
            healthBarObject.transform.localPosition = new Vector3(0f, 1.12f, 0f);
            healthBarObject.transform.localRotation = Quaternion.identity;
            healthBarObject.transform.localScale = Vector3.one;
            healthBarRoot = healthBarObject.transform;

            GameObject backObject = new GameObject("Health Bar Back");
            backObject.transform.SetParent(healthBarRoot);
            backObject.transform.localPosition = Vector3.zero;
            backObject.transform.localRotation = Quaternion.identity;
            backObject.transform.localScale = new Vector3(1.08f, 0.12f, 1f);
            healthBack = backObject.AddComponent<SpriteRenderer>();
            healthBack.sprite = GetSolidPixelSprite();
            healthBack.color = new Color(0.025f, 0.03f, 0.035f, 0.92f);
            healthBack.sortingOrder = sortingOrder + 12;

            GameObject fillObject = new GameObject("Health Bar Fill");
            fillObject.transform.SetParent(healthBarRoot);
            fillObject.transform.localPosition = Vector3.zero;
            fillObject.transform.localRotation = Quaternion.identity;
            healthFill = fillObject.AddComponent<SpriteRenderer>();
            healthFill.sprite = GetSolidPixelSprite();
            healthFill.sortingOrder = sortingOrder + 13;
            UpdateHealthBarTransform();
        }

        GameObject smokeObject = new GameObject("Broken Smoke");
        smokeObject.transform.SetParent(transform);
        smokeObject.transform.localPosition = new Vector3(0.25f, 0.25f, 0f);
        smokeObject.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        smokeObject.transform.localScale = new Vector3(0.38f, 0.38f, 1f);
        smoke = smokeObject.AddComponent<SpriteRenderer>();
        smoke.sprite = GetSmokePlumeSprite();
        smoke.color = new Color(0.2f, 0.22f, 0.23f, 0f);
        smoke.sortingOrder = sortingOrder + 14;
        smoke.enabled = false;

        Sprite dentSprite = GetDentDecalSprite();
        for (int i = 0; i < dentMarks.Length; i++)
        {
            GameObject dentObject = new GameObject("Dent Mark " + (i + 1));
            dentObject.transform.SetParent(transform);
            dentObject.transform.localPosition = Vector3.zero;
            dentObject.transform.localRotation = Quaternion.identity;
            dentObject.transform.localScale = Vector3.one * 0.16f;
            SpriteRenderer dent = dentObject.AddComponent<SpriteRenderer>();
            dent.sprite = dentSprite;
            dent.color = new Color(0.035f, 0.018f, 0.02f, 0.88f);
            dent.sortingOrder = sortingOrder + 9;
            dent.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            dent.enabled = false;
            dentMarks[i] = dent;
        }

        GameObject crackObject = new GameObject("Cracked Glass");
        crackObject.transform.SetParent(transform);
        crackObject.transform.localPosition = new Vector3(0f, 0.17f, 0f);
        crackObject.transform.localRotation = Quaternion.identity;
        crackObject.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        glassCrack = crackObject.AddComponent<SpriteRenderer>();
        glassCrack.sprite = GetCrackDecalSprite();
        glassCrack.color = new Color(0.72f, 0.92f, 1f, 0.82f);
        glassCrack.sortingOrder = sortingOrder + 10;
        glassCrack.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        glassCrack.enabled = false;

        frontLightDamage = CreateDamageMask("Broken Front Lights", new Vector2(0f, 0.78f), new Vector2(0.78f, 0.17f), sortingOrder + 11);
        rearLightDamage = CreateDamageMask("Broken Rear Lights", new Vector2(0f, -0.78f), new Vector2(0.78f, 0.17f), sortingOrder + 11);
    }

    private void CreateDamageClipMask(Sprite sprite, int sortingOrder)
    {
        if (bodyRenderer == null || sprite == null)
        {
            return;
        }

        GameObject maskObject = new GameObject("Damage Body Clip Mask");
        maskObject.transform.SetParent(bodyRenderer.transform);
        maskObject.transform.localPosition = Vector3.zero;
        maskObject.transform.localRotation = Quaternion.identity;
        maskObject.transform.localScale = Vector3.one;
        damageClipMask = maskObject.AddComponent<SpriteMask>();
        damageClipMask.sprite = sprite;
        damageClipMask.alphaCutoff = 0.08f;
        damageClipMask.isCustomRangeActive = true;
        damageClipMask.frontSortingLayerID = bodyRenderer.sortingLayerID;
        damageClipMask.backSortingLayerID = bodyRenderer.sortingLayerID;
        damageClipMask.frontSortingOrder = sortingOrder + 11;
        damageClipMask.backSortingOrder = sortingOrder + 8;
    }

    private SpriteRenderer CreateDamageMask(string objectName, Vector2 localPosition, Vector2 localScale, int sortingOrder)
    {
        GameObject maskObject = new GameObject(objectName);
        maskObject.transform.SetParent(transform);
        maskObject.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
        maskObject.transform.localRotation = Quaternion.identity;
        maskObject.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
        SpriteRenderer mask = maskObject.AddComponent<SpriteRenderer>();
        mask.sprite = GetSolidPixelSprite();
        mask.color = new Color(0.01f, 0.012f, 0.016f, 0.88f);
        mask.sortingOrder = sortingOrder;
        mask.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        mask.enabled = false;
        return mask;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (broken || Time.time - lastHitTime < HitCooldown)
        {
            return;
        }

        CarDamage otherCar = collision.collider.GetComponent<CarDamage>();
        if (otherCar == null)
        {
            return;
        }

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < LightTouchThreshold)
        {
            return;
        }

        Vector2 impactPoint = transform.position;
        if (collision.contactCount > 0)
        {
            impactPoint = collision.GetContact(0).point;
        }

        float damage = impactSpeed >= StrongImpactThreshold ? 10f : 5f;

        lastHitTime = Time.time;
        TakeDamageAt(damage, impactPoint);

        TriggerImpactFeedback(impactPoint, collision.relativeVelocity, impactSpeed);
    }

    public void TakeDamage(float amount)
    {
        TakeDamageAt(amount, (Vector2)transform.position + (Vector2)transform.up * 0.76f);
    }

    public void TakeDamageAt(float amount, Vector2 impactPoint)
    {
        if (broken || amount <= 0f)
        {
            return;
        }

        health = Mathf.Max(0f, health - amount);
        AddDent(impactPoint, amount);
        UpdateVisuals();

        if (health <= 0f)
        {
            BreakDown();
        }
    }

    public bool TryRepair(float amount)
    {
        if (broken || amount <= 0f || health >= MaxHealth)
        {
            return false;
        }

        health = Mathf.Min(MaxHealth, health + amount);
        if (health >= MaxHealth - 0.01f)
        {
            ClearDamageVisuals();
        }
        UpdateVisuals();
        return true;
    }

    private void BreakDown()
    {
        broken = true;

        if (bodyRenderer != null && brokenBodySprite != null)
        {
            bool useSecondVariant = brokenBodySpriteVariant2 != null && Random.value >= 0.5f;
            bodyRenderer.sprite = useSecondVariant ? brokenBodySpriteVariant2 : brokenBodySprite;
            bodyRenderer.color = originalBodyColor;
        }

        if (body != null)
        {
            body.linearVelocity *= 0.12f;
            body.angularVelocity *= 0.12f;
        }

        ArcadeCarController playerController = GetComponent<ArcadeCarController>();
        if (playerController != null)
        {
            playerController.SetBroken();
            if (game != null)
            {
                game.HandlePlayerBroken();
            }
        }

        CircuitAI aiController = GetComponent<CircuitAI>();
        if (aiController != null)
        {
            aiController.SetBroken();
        }

        if (smoke != null)
        {
            smoke.enabled = true;
        }
    }

    private void Update()
    {
        if (smoke == null || !smoke.enabled)
        {
            return;
        }

        float damageRatio = 1f - Mathf.Clamp01(health / MaxHealth);
        float pulse = Mathf.Lerp(0.32f, 0.58f, damageRatio) + Mathf.Sin(Time.unscaledTime * 7f) * 0.09f;
        smoke.transform.localScale = new Vector3(pulse, pulse, 1f);
        smoke.transform.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 55f);
        smoke.color = new Color(0.12f, 0.13f, 0.14f, broken ? 0.84f : Mathf.Lerp(0.4f, 0.72f, damageRatio));
    }

    private void LateUpdate()
    {
        UpdateHealthBarTransform();
    }

    private void UpdateHealthBarTransform()
    {
        if (healthBarRoot == null)
        {
            return;
        }

        Vector3 parentScale = transform.lossyScale;
        float inverseScaleX = 1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.x));
        float inverseScaleY = 1f / Mathf.Max(0.001f, Mathf.Abs(parentScale.y));
        healthBarRoot.position = transform.position + Vector3.up * 1.18f;
        healthBarRoot.rotation = Quaternion.identity;
        healthBarRoot.localScale = new Vector3(inverseScaleX, inverseScaleY, 1f);
    }

    private void UpdateVisuals()
    {
        float ratio = Mathf.Clamp01(health / MaxHealth);

        if (healthFill != null)
        {
            healthFill.transform.localScale = new Vector3(1f * ratio, 0.075f, 1f);
            healthFill.transform.localPosition = new Vector3(-0.5f * (1f - ratio), 0f, 0f);
            healthFill.color = Color.Lerp(new Color(0.48f, 0.025f, 0.015f), new Color(1f, 0.12f, 0.045f), ratio);
        }

        if (bodyRenderer != null)
        {
            Sprite targetSprite = ratio <= DamagedVisualThreshold && brokenBodySprite != null
                ? brokenBodySprite
                : normalBodySprite;
            if (targetSprite != null)
            {
                bodyRenderer.sprite = targetSprite;
                if (damageClipMask != null)
                {
                    damageClipMask.sprite = targetSprite;
                }
            }

            float visibleDamage = Mathf.Clamp01(Mathf.InverseLerp(0.06f, 0.82f, 1f - ratio));
            Color batteredColor = new Color(
                originalBodyColor.r * 0.48f + 0.1f,
                originalBodyColor.g * 0.34f,
                originalBodyColor.b * 0.3f,
                originalBodyColor.a);
            bodyRenderer.color = Color.Lerp(originalBodyColor, batteredColor, visibleDamage * 0.78f);
        }

        if (smoke != null)
        {
            smoke.enabled = broken || ratio <= SmokeVisualThreshold;
        }

        UpdateDamageOverlays(ratio);
    }

    public void Repair()
    {
        health = MaxHealth;
        broken = false;
        lastHitTime = -10f;

        if (bodyRenderer != null)
        {
            if (normalBodySprite != null)
            {
                bodyRenderer.sprite = normalBodySprite;
            }

            bodyRenderer.color = originalBodyColor;
        }

        if (smoke != null)
        {
            smoke.enabled = false;
        }

        ClearDamageVisuals();
        UpdateVisuals();
    }

    private void AddDent(Vector2 impactWorldPoint, float damageAmount)
    {
        if (bodyRenderer == null || dentMarks.Length == 0)
        {
            return;
        }

        Vector2 localPoint = transform.InverseTransformPoint(impactWorldPoint);
        if (localPoint.sqrMagnitude < 0.01f)
        {
            localPoint = Vector2.up * 0.72f;
        }

        float strength = Mathf.Clamp(damageAmount / 28f, 0.2f, 1f);

        SpriteRenderer dent = dentMarks[nextDentMark % dentMarks.Length];
        nextDentMark++;
        float size = Mathf.Lerp(0.21f, 0.44f, strength);
        float stretch = Mathf.Lerp(0.64f, 1.42f, Mathf.Repeat(nextDentMark * 0.618f, 1f));
        localPoint = ClampDentPointToBody(localPoint, size, stretch);
        if (dent != null)
        {
            dent.transform.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            dent.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(localPoint.y, localPoint.x) * Mathf.Rad2Deg + nextDentMark * 37f);
            dent.transform.localScale = new Vector3(size * stretch, size / stretch, 1f);
            dent.color = Color.Lerp(
                new Color(0.13f, 0.008f, 0.006f, 0.92f),
                new Color(0.96f, 0.11f, 0.018f, 1f),
                strength * 0.58f);
            dent.enabled = true;
        }

        float deformation = Mathf.Lerp(0.027f, 0.074f, strength);
        if (Mathf.Abs(localPoint.y) >= Mathf.Abs(localPoint.x))
        {
            if (localPoint.y >= 0f)
            {
                frontDent = Mathf.Min(0.22f, frontDent + deformation);
            }
            else
            {
                rearDent = Mathf.Min(0.22f, rearDent + deformation);
            }
        }
        else if (localPoint.x < 0f)
        {
            leftDent = Mathf.Min(0.2f, leftDent + deformation);
        }
        else
        {
            rightDent = Mathf.Min(0.2f, rightDent + deformation);
        }

        ApplyBodyDeformation();
        TryDetachDamagedPart(localPoint, impactWorldPoint, damageAmount);

        if (damageAmount >= 9f && Time.time - lastFragmentTime >= 0.12f)
        {
            SpawnMetalFragments(impactWorldPoint, damageAmount, false);
            lastFragmentTime = Time.time;
        }
    }

    private Vector2 ClampDentPointToBody(Vector2 localPoint, float size, float stretch)
    {
        Sprite hullSprite = normalBodySprite != null ? normalBodySprite : bodyRenderer.sprite;
        if (hullSprite == null)
        {
            return new Vector2(
                Mathf.Clamp(localPoint.x, -0.24f, 0.24f),
                Mathf.Clamp(localPoint.y, -0.62f, 0.62f));
        }

        Vector2 spriteExtents = hullSprite.bounds.extents;
        float halfWidth = spriteExtents.x * Mathf.Abs(originalBodyLocalScale.x);
        float halfLength = spriteExtents.y * Mathf.Abs(originalBodyLocalScale.y);
        float decalWidth = size * stretch;
        float decalLength = size / Mathf.Max(stretch, 0.01f);
        float decalRadius = Mathf.Sqrt(decalWidth * decalWidth + decalLength * decalLength) * 0.5f;

        float safeHalfLength = Mathf.Max(0.18f, halfLength * 0.88f - decalRadius * 0.52f);
        localPoint.y = Mathf.Clamp(localPoint.y, -safeHalfLength, safeHalfLength);

        float normalizedLength = Mathf.Clamp01(Mathf.Abs(localPoint.y) / Mathf.Max(halfLength, 0.01f));
        float endTaper = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.92f, normalizedLength));
        float hullHalfWidth = halfWidth * Mathf.Lerp(0.86f, 0.44f, endTaper);
        float safeHalfWidth = Mathf.Max(0.055f, hullHalfWidth - decalRadius * 0.62f);
        localPoint.x = Mathf.Clamp(localPoint.x, -safeHalfWidth, safeHalfWidth);
        return localPoint;
    }

    private void ApplyBodyDeformation()
    {
        if (bodyRenderer == null)
        {
            return;
        }

        float widthScale = Mathf.Clamp(1f - (leftDent + rightDent) * 0.64f, 0.76f, 1f);
        float lengthScale = Mathf.Clamp(1f - (frontDent + rearDent) * 0.58f, 0.74f, 1f);
        bodyRenderer.transform.localScale = new Vector3(
            originalBodyLocalScale.x * widthScale,
            originalBodyLocalScale.y * lengthScale,
            originalBodyLocalScale.z);
        bodyRenderer.transform.localPosition = originalBodyLocalPosition + new Vector3(
            (leftDent - rightDent) * 0.27f,
            (rearDent - frontDent) * 0.23f,
            0f);
        bodyRenderer.transform.localRotation = originalBodyLocalRotation * Quaternion.Euler(0f, 0f, (rightDent - leftDent) * 25f);
    }

    private void UpdateDamageOverlays(float healthRatio)
    {
        float totalDamage = 1f - Mathf.Clamp01(healthRatio);
        if (glassCrack != null)
        {
            float glassSeverity = Mathf.Clamp01(
                Mathf.InverseLerp(0.12f, 0.68f, totalDamage)
                + (frontDent + leftDent + rightDent) * 1.8f);
            glassCrack.enabled = glassSeverity > 0.025f;
            glassCrack.color = new Color(0.76f, 0.96f, 1f, Mathf.Lerp(0.48f, 1f, glassSeverity));
            float glassScale = Mathf.Lerp(0.46f, 0.78f, glassSeverity);
            glassCrack.transform.localScale = new Vector3(glassScale, glassScale, 1f);
            glassCrack.transform.localRotation = Quaternion.Euler(0f, 0f, (rightDent - leftDent) * 125f);
        }

        if (frontLightDamage != null)
        {
            float frontSeverity = Mathf.Clamp01(frontDent / 0.11f + (frontPartDetached ? 0.7f : 0f));
            frontLightDamage.enabled = frontSeverity > 0.15f;
            frontLightDamage.color = new Color(0.008f, 0.01f, 0.014f, Mathf.Lerp(0.58f, 0.96f, frontSeverity));
        }

        if (rearLightDamage != null)
        {
            float rearSeverity = Mathf.Clamp01(rearDent / 0.11f + (rearPartDetached ? 0.7f : 0f));
            rearLightDamage.enabled = rearSeverity > 0.15f;
            rearLightDamage.color = new Color(0.008f, 0.01f, 0.014f, Mathf.Lerp(0.58f, 0.96f, rearSeverity));
        }
    }

    private void TryDetachDamagedPart(Vector2 localPoint, Vector2 impactWorldPoint, float damageAmount)
    {
        bool frontOrRear = Mathf.Abs(localPoint.y) >= Mathf.Abs(localPoint.x);
        float zoneDamage;
        bool alreadyDetached;

        if (frontOrRear && localPoint.y >= 0f)
        {
            zoneDamage = frontDent;
            alreadyDetached = frontPartDetached;
        }
        else if (frontOrRear)
        {
            zoneDamage = rearDent;
            alreadyDetached = rearPartDetached;
        }
        else if (localPoint.x < 0f)
        {
            zoneDamage = leftDent;
            alreadyDetached = leftPartDetached;
        }
        else
        {
            zoneDamage = rightDent;
            alreadyDetached = rightPartDetached;
        }

        if (alreadyDetached || (zoneDamage < 0.075f && damageAmount < 18f))
        {
            return;
        }

        if (frontOrRear && localPoint.y >= 0f)
        {
            frontPartDetached = true;
        }
        else if (frontOrRear)
        {
            rearPartDetached = true;
        }
        else if (localPoint.x < 0f)
        {
            leftPartDetached = true;
        }
        else
        {
            rightPartDetached = true;
        }

        SpawnMetalFragments(impactWorldPoint, Mathf.Max(damageAmount, 18f), true);
        lastFragmentTime = Time.time;
    }

    private void SpawnMetalFragments(Vector2 impactWorldPoint, float damageAmount, bool includeLargePanel)
    {
        int fragmentCount = includeLargePanel ? 7 : 4;
        float impactStrength = Mathf.Clamp01(damageAmount / 28f);
        Vector2 awayFromCar = impactWorldPoint - (Vector2)transform.position;
        if (awayFromCar.sqrMagnitude < 0.01f)
        {
            awayFromCar = transform.up;
        }
        awayFromCar.Normalize();

        for (int i = 0; i < fragmentCount; i++)
        {
            GameObject fragment = new GameObject(includeLargePanel && i == 0 ? "Detached Body Panel" : "Metal Fragment");
            fragment.transform.position = impactWorldPoint + Random.insideUnitCircle * 0.12f;
            fragment.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            bool largePanel = includeLargePanel && i == 0;
            float fragmentWidth = largePanel ? Random.Range(0.2f, 0.29f) : Random.Range(0.045f, 0.11f);
            float fragmentLength = largePanel ? Random.Range(0.07f, 0.12f) : Random.Range(0.035f, 0.08f);
            fragment.transform.localScale = new Vector3(fragmentWidth, fragmentLength, 1f);

            SpriteRenderer fragmentRenderer = fragment.AddComponent<SpriteRenderer>();
            fragmentRenderer.sprite = GetSolidPixelSprite();
            Color metalColor = Color.Lerp(originalBodyColor, new Color(0.08f, 0.085f, 0.09f, 1f), Random.Range(0.12f, 0.58f));
            metalColor.a = 0.96f;
            fragmentRenderer.color = metalColor;
            fragmentRenderer.sortingOrder = bodyRenderer != null ? bodyRenderer.sortingOrder + 7 : 22;

            Rigidbody2D fragmentBody = fragment.AddComponent<Rigidbody2D>();
            fragmentBody.gravityScale = 0f;
            fragmentBody.mass = largePanel ? 0.11f : 0.035f;
            fragmentBody.linearDamping = largePanel ? 1.25f : 1.8f;
            fragmentBody.angularDamping = 0.55f;
            fragmentBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Vector2 inheritedVelocity = body != null ? body.linearVelocity * 0.48f : Vector2.zero;
            Vector2 spread = Random.insideUnitCircle * Mathf.Lerp(0.9f, 2.6f, impactStrength);
            fragmentBody.linearVelocity = inheritedVelocity + awayFromCar * Mathf.Lerp(1.6f, 4.8f, impactStrength) + spread;
            fragmentBody.angularVelocity = Random.Range(-520f, 520f) * Mathf.Lerp(0.45f, 1f, impactStrength);

            CarDebrisPiece debris = fragment.AddComponent<CarDebrisPiece>();
            debris.Initialize(fragmentRenderer, Random.Range(2.3f, 4.1f));
        }
    }

    private void ClearDamageVisuals()
    {
        frontDent = 0f;
        rearDent = 0f;
        leftDent = 0f;
        rightDent = 0f;
        nextDentMark = 0;
        lastFragmentTime = -10f;
        frontPartDetached = false;
        rearPartDetached = false;
        leftPartDetached = false;
        rightPartDetached = false;

        for (int i = 0; i < dentMarks.Length; i++)
        {
            if (dentMarks[i] != null)
            {
                dentMarks[i].enabled = false;
            }
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.transform.localScale = originalBodyLocalScale;
            bodyRenderer.transform.localPosition = originalBodyLocalPosition;
            bodyRenderer.transform.localRotation = originalBodyLocalRotation;
            if (!broken && normalBodySprite != null)
            {
                bodyRenderer.sprite = normalBodySprite;
            }
        }

        if (glassCrack != null)
        {
            glassCrack.enabled = false;
        }
        if (frontLightDamage != null)
        {
            frontLightDamage.enabled = false;
        }
        if (rearLightDamage != null)
        {
            rearLightDamage.enabled = false;
        }
    }

    private static Sprite GetDentDecalSprite()
    {
        if (dentDecalSprite != null)
        {
            return dentDecalSprite;
        }

        const int size = 12;
        string[] pattern =
        {
            "............",
            "....++......",
            "..++##++....",
            ".+##..##+...",
            "+##....##+..",
            "+#..##..##+.",
            "+##....###+.",
            ".+#######+..",
            "..++###++...",
            "....+++.....",
            "............",
            "............"
        };

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Dent Decal";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            string row = pattern[size - 1 - y];
            for (int x = 0; x < size; x++)
            {
                char symbol = row[x];
                byte alpha = symbol == '#' ? (byte)235 : symbol == '+' ? (byte)118 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        dentDecalSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        dentDecalSprite.name = "Runtime Dent Decal Sprite";
        return dentDecalSprite;
    }

    private static Sprite GetSolidPixelSprite()
    {
        if (solidPixelSprite != null)
        {
            return solidPixelSprite;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.name = "Runtime Solid Pixel";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply(false, true);
        solidPixelSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        solidPixelSprite.name = "Runtime Solid Pixel Sprite";
        return solidPixelSprite;
    }

    private static Sprite GetCrackDecalSprite()
    {
        if (crackDecalSprite != null)
        {
            return crackDecalSprite;
        }

        const int size = 18;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Glass Cracks";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        Vector2[] directions =
        {
            new Vector2(1f, 0.15f), new Vector2(0.62f, 0.8f), new Vector2(-0.18f, 1f),
            new Vector2(-0.8f, 0.48f), new Vector2(-1f, -0.2f), new Vector2(-0.42f, -1f),
            new Vector2(0.25f, -1f), new Vector2(0.9f, -0.55f)
        };
        int center = size / 2;
        for (int ray = 0; ray < directions.Length; ray++)
        {
            Vector2 direction = directions[ray].normalized;
            int steps = 5 + ray % 3;
            for (int step = 0; step <= steps; step++)
            {
                float wobble = Mathf.Sin(ray * 2.17f + step * 1.83f) * 0.45f;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                int x = Mathf.Clamp(Mathf.RoundToInt(center + direction.x * step + perpendicular.x * wobble), 0, size - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(center + direction.y * step + perpendicular.y * wobble), 0, size - 1);
                pixels[y * size + x] = new Color32(255, 255, 255, step < 2 ? (byte)245 : (byte)205);

                if (step == 4 && ray % 2 == 0)
                {
                    int branchX = Mathf.Clamp(x + (direction.y >= 0f ? 1 : -1), 0, size - 1);
                    int branchY = Mathf.Clamp(y + (direction.x >= 0f ? -1 : 1), 0, size - 1);
                    pixels[branchY * size + branchX] = new Color32(255, 255, 255, 155);
                }
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        crackDecalSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        crackDecalSprite.name = "Runtime Glass Cracks Sprite";
        return crackDecalSprite;
    }

    private static Sprite GetSmokePlumeSprite()
    {
        if (smokePlumeSprite != null)
        {
            return smokePlumeSprite;
        }

        const int size = 16;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Runtime Damage Smoke";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 delta = new Vector2(x, y) - center;
                float distance = delta.magnitude / (size * 0.5f);
                float noise = Mathf.Sin(x * 2.31f + y * 1.77f) * 0.1f;
                float density = Mathf.Clamp01(1f - distance + noise);
                byte alpha = (byte)Mathf.RoundToInt(density * density * 235f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        smokePlumeSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        smokePlumeSprite.name = "Runtime Damage Smoke Sprite";
        return smokePlumeSprite;
    }

    public void TriggerImpactFeedback(Vector2 position, Vector2 impactDirection, float impactSpeed)
    {
        if (!playerCar || game == null)
        {
            return;
        }

        game.AddHitFlash(Mathf.Clamp01(impactSpeed * 0.045f));
        game.ShakeCamera(Mathf.Clamp(impactSpeed * 0.12f, 0.22f, 1.5f), Mathf.Clamp(impactSpeed * 0.05f, 0.1f, 0.68f));
        game.PlayImpactSound(impactSpeed);
        game.SpawnImpactSparks(position, impactDirection, Mathf.Clamp01(impactSpeed / 24f));
    }
}

public sealed class CarDebrisPiece : MonoBehaviour
{
    private SpriteRenderer visual;
    private Color originalColor;
    private Vector3 originalScale;
    private float createdAt;
    private float lifetime;

    public void Initialize(SpriteRenderer debrisVisual, float debrisLifetime)
    {
        visual = debrisVisual;
        originalColor = visual != null ? visual.color : Color.white;
        originalScale = transform.localScale;
        createdAt = Time.time;
        lifetime = Mathf.Max(0.25f, debrisLifetime);
    }

    private void Update()
    {
        float progress = Mathf.Clamp01((Time.time - createdAt) / lifetime);
        float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 1f, progress));
        if (visual != null)
        {
            visual.color = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * fade);
        }
        transform.localScale = originalScale * Mathf.Lerp(1f, 0.72f, Mathf.InverseLerp(0.58f, 1f, progress));

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
