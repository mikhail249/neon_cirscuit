using UnityEngine;

public sealed class NeonOrbitMine : MonoBehaviour
{
    private const float ArmDelay = 0.55f;
    private const float TriggerRadius = 2.15f;
    private const float DamageRadius = 3.35f;
    private const float Lifetime = 12f;
    private const float WeaponDamage = 18f;

    private GameObject owner;
    private Rigidbody2D body;
    private SpriteRenderer aura;
    private SpriteRenderer outerRing;
    private SpriteRenderer core;
    private SpriteRenderer warningLight;
    private float createdAt;
    private float explodedAt;
    private float damageAmount;
    private bool exploding;

    public void Initialize(
        GameObject weaponOwner,
        Vector2 inheritedVelocity,
        Sprite pixelSprite,
        Sprite circleSprite,
        float damageMultiplier)
    {
        owner = weaponOwner;
        damageAmount = WeaponDamage * Mathf.Max(0.1f, damageMultiplier);
        createdAt = Time.time;

        body = gameObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.linearDamping = 3.2f;
        body.angularDamping = 2.4f;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.linearVelocity = inheritedVelocity;

        CreateVisuals(pixelSprite, circleSprite);
    }

    public bool IsOwnedBy(GameObject candidate)
    {
        return owner == candidate;
    }

    private void Update()
    {
        if (exploding)
        {
            float progress = Mathf.Clamp01((Time.unscaledTime - explodedAt) / 0.34f);
            transform.localScale = Vector3.one * Mathf.Lerp(1f, 4.8f, progress);
            SetAlpha(1f - progress);
            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
            return;
        }

        float age = Time.time - createdAt;
        if (age >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        bool armed = age >= ArmDelay;
        float pulse = armed
            ? 0.84f + Mathf.Sin(Time.unscaledTime * 11f) * 0.16f
            : Mathf.Lerp(0.45f, 0.84f, Mathf.Clamp01(age / ArmDelay));
        if (aura != null)
        {
            aura.transform.localScale = Vector3.one * (1.52f * pulse);
        }
        if (outerRing != null)
        {
            outerRing.transform.Rotate(0f, 0f, Time.unscaledDeltaTime * 84f);
        }
        if (warningLight != null)
        {
            Color color = warningLight.color;
            color.a = armed ? Mathf.Lerp(0.28f, 1f, pulse) : 0.24f;
            warningLight.color = color;
        }

        if (armed && HasTargetInRange())
        {
            Detonate();
        }
    }

    private bool HasTargetInRange()
    {
        CarDamage[] cars = FindObjectsByType<CarDamage>();
        for (int i = 0; i < cars.Length; i++)
        {
            CarDamage target = cars[i];
            if (target == null || target.IsBroken || target.gameObject == owner)
            {
                continue;
            }

            if (Vector2.Distance(transform.position, target.transform.position) <= TriggerRadius)
            {
                return true;
            }
        }
        return false;
    }

    private void Detonate()
    {
        if (exploding)
        {
            return;
        }

        exploding = true;
        explodedAt = Time.unscaledTime;
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }

        CarDamage[] cars = FindObjectsByType<CarDamage>();
        for (int i = 0; i < cars.Length; i++)
        {
            CarDamage target = cars[i];
            if (target == null || target.IsBroken || target.gameObject == owner)
            {
                continue;
            }

            Vector2 offset = (Vector2)target.transform.position - (Vector2)transform.position;
            float distance = offset.magnitude;
            if (distance > DamageRadius)
            {
                continue;
            }

            float falloff = Mathf.Lerp(0.48f, 1f, 1f - distance / DamageRadius);
            target.TakeDamageAt(damageAmount * falloff, transform.position);
            CircuitAI rival = target.GetComponent<CircuitAI>();
            if (rival != null)
            {
                rival.ApplyWeaponImpact();
            }

            Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
            if (targetBody != null)
            {
                Vector2 pushDirection = distance > 0.05f ? offset / distance : Vector2.up;
                targetBody.AddForce(pushDirection * Mathf.Lerp(8f, 19f, falloff), ForceMode2D.Impulse);
                targetBody.angularVelocity += Mathf.Sign(Vector2.Dot(pushDirection, target.transform.right)) * 95f * falloff;
            }
        }

        if (aura != null) aura.color = new Color(1f, 0.12f, 0.38f, 0.78f);
        if (outerRing != null) outerRing.color = new Color(1f, 0.76f, 0.2f, 1f);
        if (core != null) core.color = Color.white;
    }

    private void CreateVisuals(Sprite pixelSprite, Sprite circleSprite)
    {
        aura = CreateVisual("Mine Proximity Aura", Vector2.zero, new Vector2(1.52f, 1.52f), new Color(0.68f, 0.12f, 1f, 0.2f), 17, circleSprite);
        CreateVisual("Mine Shadow", new Vector2(0.08f, -0.08f), new Vector2(0.9f, 0.9f), new Color(0f, 0f, 0.02f, 0.72f), 18, circleSprite);
        outerRing = CreateVisual("Mine Orbital Ring", Vector2.zero, new Vector2(0.92f, 0.18f), new Color(0.2f, 0.94f, 1f), 20, pixelSprite, 45f);
        CreateVisual("Mine Orbital Ring Cross", Vector2.zero, new Vector2(0.92f, 0.18f), new Color(1f, 0.18f, 0.74f), 20, pixelSprite, -45f);
        core = CreateVisual("Mine Core", Vector2.zero, new Vector2(0.48f, 0.48f), new Color(0.05f, 0.08f, 0.18f), 21, circleSprite);
        CreateVisual("Mine Core Rim", Vector2.zero, new Vector2(0.32f, 0.32f), new Color(0.64f, 0.2f, 1f), 22, circleSprite);
        warningLight = CreateVisual("Mine Warning Light", new Vector2(0f, 0.02f), new Vector2(0.13f, 0.13f), new Color(1f, 0.72f, 0.18f), 23, circleSprite);
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

    private void SetAlpha(float alpha)
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
