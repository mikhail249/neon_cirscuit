using UnityEngine;

public sealed class NeonPlasmaBolt : MonoBehaviour
{
    private const float FlightSpeed = 30f;
    private const float Lifetime = 1.35f;
    private const float WeaponDamage = 10f;

    private GameObject owner;
    private Rigidbody2D body;
    private Collider2D boltCollider;
    private SpriteRenderer glow;
    private SpriteRenderer core;
    private float launchedAt;
    private float impactStartedAt;
    private float damageAmount = WeaponDamage;
    private bool enemyBolt;
    private bool impacting;

    public void Initialize(GameObject boltOwner, Vector2 launchVelocity, Sprite pixelSprite, Sprite circleSprite, float damageMultiplier = 1f)
    {
        owner = boltOwner;
        body = GetComponent<Rigidbody2D>();
        boltCollider = GetComponent<Collider2D>();
        enemyBolt = owner != null && owner.GetComponent<CircuitAI>() != null;
        damageAmount = WeaponDamage * Mathf.Max(0.1f, damageMultiplier);
        launchedAt = Time.time;
        CreateVisuals(pixelSprite, circleSprite);

        Vector2 direction = launchVelocity.sqrMagnitude > 0.01f
            ? launchVelocity.normalized
            : (Vector2)transform.up;
        body.linearVelocity = direction * FlightSpeed;
        body.MoveRotation(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    public bool IsOwnedBy(GameObject candidate)
    {
        return owner == candidate;
    }

    private void FixedUpdate()
    {
        if (impacting || body == null)
        {
            return;
        }

        if (Time.time - launchedAt >= Lifetime)
        {
            BeginImpact();
            return;
        }

        Vector2 direction = body.linearVelocity.sqrMagnitude > 0.01f
            ? body.linearVelocity.normalized
            : (Vector2)transform.up;
        body.linearVelocity = direction * FlightSpeed;
        body.MoveRotation(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
    }

    private void Update()
    {
        if (impacting)
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - impactStartedAt) / 0.18f);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 3.1f, progress);
            SetVisualAlpha(1f - progress);
            if (progress >= 1f)
            {
                Destroy(gameObject);
            }

            return;
        }

        float pulse = 0.9f + Mathf.Sin(Time.unscaledTime * 28f) * 0.13f;
        if (glow != null)
        {
            glow.transform.localScale = new Vector3(0.72f * pulse, 1.25f * pulse, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (impacting || other.gameObject == owner || (owner != null && other.transform.IsChildOf(owner.transform)))
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
                rival.ApplyPlasmaImpact();
            }
            else if (other.attachedRigidbody != null)
            {
                other.attachedRigidbody.linearVelocity *= 0.72f;
                other.attachedRigidbody.angularVelocity *= 0.82f;
            }

            BeginImpact();
            return;
        }

        if (other.GetComponent<TrackObstacle>() != null)
        {
            BeginImpact();
        }
    }

    private void CreateVisuals(Sprite pixelSprite, Sprite circleSprite)
    {
        Color glowColor = enemyBolt
            ? new Color(1f, 0.05f, 0.42f, 0.3f)
            : new Color(0.22f, 0.52f, 1f, 0.3f);
        Color coreColor = enemyBolt
            ? new Color(1f, 0.16f, 0.48f)
            : new Color(0.18f, 0.96f, 1f);
        Color centerColor = enemyBolt
            ? new Color(1f, 0.82f, 0.92f)
            : new Color(0.88f, 1f, 1f);

        glow = CreateVisual("Plasma Glow", Vector2.zero, new Vector2(0.72f, 1.25f), glowColor, 18, circleSprite);
        CreateVisual("Plasma Trail", new Vector2(0f, -0.42f), new Vector2(0.24f, 0.9f), new Color(coreColor.r, coreColor.g, coreColor.b, 0.38f), 19, pixelSprite);
        core = CreateVisual("Plasma Core", Vector2.zero, new Vector2(0.22f, 0.72f), coreColor, 20, pixelSprite);
        CreateVisual("Plasma Center", new Vector2(0f, 0.14f), new Vector2(0.12f, 0.32f), centerColor, 21, pixelSprite);
    }

    private SpriteRenderer CreateVisual(string visualName, Vector2 localPosition, Vector2 scale, Color color, int sortingOrder, Sprite sprite)
    {
        GameObject visual = new GameObject(visualName);
        visual.transform.SetParent(transform);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void BeginImpact()
    {
        if (impacting)
        {
            return;
        }

        impacting = true;
        impactStartedAt = Time.unscaledTime;
        if (boltCollider != null)
        {
            boltCollider.enabled = false;
        }

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        if (core != null)
        {
            core.color = Color.white;
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
