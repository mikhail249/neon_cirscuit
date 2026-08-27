using UnityEngine;

public sealed class NeonRocket : MonoBehaviour
{
    private const float FlightSpeed = 19f;
    private const float Lifetime = 2.8f;
    private const float WeaponDamage = 10f;

    private GameObject owner;
    private Rigidbody2D body;
    private Collider2D rocketCollider;
    private Vector2 flightDirection;
    private SpriteRenderer glow;
    private SpriteRenderer core;
    private SpriteRenderer trailGlow;
    private SpriteRenderer trailCore;
    private SpriteRenderer leftEngineFlare;
    private SpriteRenderer rightEngineFlare;
    private SpriteRenderer leftNavigationLight;
    private SpriteRenderer rightNavigationLight;
    private Color glowBaseColor;
    private Color navigationBaseColor;
    private float launchedAt;
    private float explosionStartedAt;
    private float damageAmount = WeaponDamage;
    private bool enemyRocket;
    private bool exploding;

    public void Initialize(
        GameObject rocketOwner,
        Vector2 launchVelocity,
        Sprite pixelSprite,
        Sprite circleSprite,
        float damageMultiplier = 1f)
    {
        owner = rocketOwner;
        body = GetComponent<Rigidbody2D>();
        rocketCollider = GetComponent<Collider2D>();
        enemyRocket = owner != null && owner.GetComponent<CircuitAI>() != null;
        damageAmount = WeaponDamage * Mathf.Max(0.1f, damageMultiplier);
        launchedAt = Time.time;
        CreateVisuals(pixelSprite, circleSprite);
        flightDirection = launchVelocity.sqrMagnitude > 0.01f
            ? launchVelocity.normalized
            : (Vector2)transform.up;
        body.linearVelocity = flightDirection * FlightSpeed;
        body.rotation = Mathf.Atan2(flightDirection.y, flightDirection.x) * Mathf.Rad2Deg - 90f;
    }

    public bool IsOwnedBy(GameObject candidate)
    {
        return owner == candidate;
    }

    private void FixedUpdate()
    {
        if (exploding || body == null)
        {
            return;
        }

        if (Time.time - launchedAt >= Lifetime)
        {
            BeginExplosion();
            return;
        }

        body.linearVelocity = flightDirection * FlightSpeed;
        body.MoveRotation(Mathf.Atan2(flightDirection.y, flightDirection.x) * Mathf.Rad2Deg - 90f);
    }

    private void Update()
    {
        if (exploding)
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - explosionStartedAt) / 0.26f);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 4.2f, progress);
            SetVisualAlpha(1f - progress);
            if (progress >= 1f)
            {
                Destroy(gameObject);
            }

            return;
        }

        float pulse = 0.88f + Mathf.Sin(Time.unscaledTime * 24f) * 0.12f;
        float flarePulse = 0.86f + Mathf.Sin(Time.unscaledTime * 31f + 1.4f) * 0.14f;
        if (trailGlow != null)
        {
            trailGlow.transform.localScale = new Vector3(0.44f * pulse, 0.74f * pulse, 1f);
        }

        if (trailCore != null)
        {
            trailCore.transform.localScale = new Vector3(0.12f * flarePulse, 0.62f * flarePulse, 1f);
        }

        if (leftEngineFlare != null && rightEngineFlare != null)
        {
            Vector3 flareScale = new Vector3(0.07f, 0.4f * flarePulse, 1f);
            leftEngineFlare.transform.localScale = flareScale;
            rightEngineFlare.transform.localScale = flareScale;
        }

        float lightPulse = 0.62f + Mathf.Sin(Time.unscaledTime * 14f) * 0.28f;
        if (glow != null)
        {
            glow.color = new Color(glowBaseColor.r, glowBaseColor.g, glowBaseColor.b, glowBaseColor.a * lightPulse);
        }

        if (leftNavigationLight != null && rightNavigationLight != null)
        {
            Color navigation = new Color(
                navigationBaseColor.r,
                navigationBaseColor.g,
                navigationBaseColor.b,
                lightPulse);
            leftNavigationLight.color = navigation;
            rightNavigationLight.color = navigation;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (exploding || other.gameObject == owner || (owner != null && other.transform.IsChildOf(owner.transform)))
        {
            return;
        }

        CarDamage damage = other.GetComponent<CarDamage>();
        if (damage != null)
        {
            damage.TakeDamageAt(damageAmount, transform.position);
            CircuitAI rival = other.GetComponent<CircuitAI>();
            if (rival != null)
            {
                rival.ApplyWeaponImpact();
            }
            else
            {
                Rigidbody2D targetBody = other.attachedRigidbody;
                if (targetBody != null)
                {
                    targetBody.linearVelocity *= 0.42f;
                    targetBody.angularVelocity *= 0.6f;
                }
            }

            BeginExplosion();
            return;
        }

        if (other.GetComponent<TrackObstacle>() != null)
        {
            BeginExplosion();
        }
    }

    private void CreateVisuals(Sprite pixelSprite, Sprite circleSprite)
    {
        Color hullOutline = enemyRocket ? new Color(0.18f, 0.008f, 0.05f) : new Color(0.015f, 0.08f, 0.12f);
        Color hullColor = enemyRocket ? new Color(0.92f, 0.035f, 0.22f) : new Color(0.12f, 0.78f, 0.92f);
        Color hullHighlight = enemyRocket ? new Color(1f, 0.28f, 0.16f) : new Color(0.72f, 0.98f, 1f);
        Color finColor = enemyRocket ? new Color(0.58f, 0.018f, 0.2f) : new Color(0.025f, 0.38f, 0.58f);
        Color noseColor = enemyRocket ? new Color(1f, 0.72f, 0.2f) : new Color(1f, 0.54f, 0.08f);
        glowBaseColor = enemyRocket ? new Color(1f, 0.02f, 0.26f, 0.32f) : new Color(0.08f, 0.9f, 1f, 0.28f);
        navigationBaseColor = enemyRocket ? new Color(1f, 0.12f, 0.28f) : new Color(0.48f, 1f, 0.34f);

        glow = CreateVisual("Rocket Aura", new Vector2(0f, 0.015f), new Vector2(0.66f, 1f), glowBaseColor, 16, circleSprite);
        CreateVisual("Rocket Shadow", new Vector2(0.045f, -0.045f), new Vector2(0.39f, 0.91f), new Color(0f, 0f, 0.015f, 0.74f), 17, circleSprite);

        trailGlow = CreateVisual("Engine Glow", new Vector2(0f, -0.57f), new Vector2(0.44f, 0.74f), new Color(1f, 0.08f, 0.018f, 0.28f), 17, circleSprite);
        leftEngineFlare = CreateVisual("Left Engine Flare", new Vector2(-0.075f, -0.55f), new Vector2(0.07f, 0.4f), new Color(1f, 0.32f, 0.035f, 0.82f), 18, pixelSprite, -4f);
        rightEngineFlare = CreateVisual("Right Engine Flare", new Vector2(0.075f, -0.55f), new Vector2(0.07f, 0.4f), new Color(1f, 0.32f, 0.035f, 0.82f), 18, pixelSprite, 4f);
        trailCore = CreateVisual("Engine Core", new Vector2(0f, -0.56f), new Vector2(0.12f, 0.62f), new Color(1f, 0.88f, 0.34f), 19, pixelSprite);

        CreateVisual("Left Stabilizer", new Vector2(-0.18f, -0.14f), new Vector2(0.17f, 0.36f), hullOutline, 19, pixelSprite, -17f);
        CreateVisual("Right Stabilizer", new Vector2(0.18f, -0.14f), new Vector2(0.17f, 0.36f), hullOutline, 19, pixelSprite, 17f);
        CreateVisual("Left Fin Panel", new Vector2(-0.175f, -0.13f), new Vector2(0.1f, 0.28f), finColor, 20, pixelSprite, -17f);
        CreateVisual("Right Fin Panel", new Vector2(0.175f, -0.13f), new Vector2(0.1f, 0.28f), finColor, 20, pixelSprite, 17f);

        CreateVisual("Rocket Hull Outline", Vector2.zero, new Vector2(0.34f, 0.82f), hullOutline, 20, circleSprite);
        core = CreateVisual("Rocket Hull", new Vector2(0f, 0.015f), new Vector2(0.25f, 0.7f), hullColor, 21, circleSprite);
        CreateVisual("Rocket Center Panel", new Vector2(0f, -0.015f), new Vector2(0.075f, 0.43f), hullHighlight, 22, pixelSprite);
        CreateVisual("Engine Collar", new Vector2(0f, -0.32f), new Vector2(0.31f, 0.09f), hullOutline, 22, pixelSprite);
        CreateVisual("Engine Collar Light", new Vector2(0f, -0.305f), new Vector2(0.2f, 0.035f), noseColor, 23, pixelSprite);

        CreateVisual("Nose Housing", new Vector2(0f, 0.37f), new Vector2(0.24f, 0.24f), hullOutline, 22, pixelSprite, 45f);
        CreateVisual("Nose Cone", new Vector2(0f, 0.385f), new Vector2(0.17f, 0.17f), noseColor, 23, pixelSprite, 45f);
        CreateVisual("Nose Highlight", new Vector2(-0.032f, 0.43f), new Vector2(0.045f, 0.065f), new Color(1f, 0.98f, 0.75f), 24, circleSprite);

        leftNavigationLight = CreateVisual("Left Navigation Light", new Vector2(-0.22f, -0.03f), new Vector2(0.06f, 0.06f), navigationBaseColor, 23, circleSprite);
        rightNavigationLight = CreateVisual("Right Navigation Light", new Vector2(0.22f, -0.03f), new Vector2(0.06f, 0.06f), navigationBaseColor, 23, circleSprite);
    }

    private SpriteRenderer CreateVisual(
        string visualName,
        Vector2 localPosition,
        Vector2 scale,
        Color color,
        int sortingOrder,
        Sprite sprite,
        float localRotation = 0f)
    {
        GameObject visual = new GameObject(visualName);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, localRotation);
        visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void BeginExplosion()
    {
        if (exploding)
        {
            return;
        }

        exploding = true;
        explosionStartedAt = Time.unscaledTime;
        if (rocketCollider != null)
        {
            rocketCollider.enabled = false;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        if (core != null)
        {
            core.color = new Color(1f, 0.9f, 0.28f);
        }

        if (glow != null)
        {
            glow.color = new Color(1f, 0.18f, 0.04f, 0.72f);
        }
    }

    private void SetVisualAlpha(float alpha)
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            Color color = renderers[i].color;
            color.a = Mathf.Min(color.a, alpha);
            renderers[i].color = color;
        }
    }
}
