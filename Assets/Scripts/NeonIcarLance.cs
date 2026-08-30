using System.Collections.Generic;
using UnityEngine;

public sealed class NeonIcarLance : MonoBehaviour
{
    private const float BeamLength = 24f;
    private const float BeamHalfWidth = 0.72f;
    private const float WeaponDamage = 22f;
    private const float VisualLifetime = 0.28f;

    private GameObject owner;
    private SpriteRenderer[] visuals;
    private float createdAt;

    public void Initialize(
        GameObject weaponOwner,
        Vector2 origin,
        Vector2 forward,
        Sprite pixelSprite,
        Sprite circleSprite,
        float damageMultiplier)
    {
        owner = weaponOwner;
        transform.position = origin;
        createdAt = Time.unscaledTime;
        Vector2 direction = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector2.up;

        CreateVisuals(origin, direction, pixelSprite, circleSprite);
        StrikeTargets(origin, direction, damageMultiplier);
    }

    public bool IsOwnedBy(GameObject candidate)
    {
        return owner == candidate;
    }

    private void Update()
    {
        float progress = Mathf.Clamp01((Time.unscaledTime - createdAt) / VisualLifetime);
        if (visuals != null)
        {
            for (int i = 0; i < visuals.Length; i++)
            {
                SpriteRenderer renderer = visuals[i];
                if (renderer == null)
                {
                    continue;
                }

                Color color = renderer.color;
                color.a = Mathf.Min(color.a, 1f - progress);
                renderer.color = color;
            }
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void StrikeTargets(Vector2 origin, Vector2 forward, float damageMultiplier)
    {
        CarDamage[] cars = FindObjectsByType<CarDamage>();
        List<TargetHit> hits = new List<TargetHit>();
        Vector2 right = new Vector2(forward.y, -forward.x);

        for (int i = 0; i < cars.Length; i++)
        {
            CarDamage candidate = cars[i];
            if (candidate == null || candidate.IsBroken || candidate.gameObject == owner)
            {
                continue;
            }

            Vector2 offset = (Vector2)candidate.transform.position - origin;
            float forwardDistance = Vector2.Dot(offset, forward);
            float lateralDistance = Mathf.Abs(Vector2.Dot(offset, right));
            if (forwardDistance <= 0f || forwardDistance > BeamLength || lateralDistance > BeamHalfWidth)
            {
                continue;
            }

            hits.Add(new TargetHit(candidate, forwardDistance));
        }

        hits.Sort((left, rightHit) => left.Distance.CompareTo(rightHit.Distance));
        for (int i = 0; i < hits.Count; i++)
        {
            CarDamage target = hits[i].Target;
            float penetrationRetention = Mathf.Pow(0.82f, i);
            target.TakeDamageAt(WeaponDamage * penetrationRetention * Mathf.Max(0.1f, damageMultiplier), target.transform.position);

            CircuitAI rival = target.GetComponent<CircuitAI>();
            if (rival != null)
            {
                rival.ApplyWeaponImpact();
            }
            else
            {
                Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
                if (targetBody != null)
                {
                    targetBody.linearVelocity *= 0.52f;
                    targetBody.angularVelocity *= 0.68f;
                }
            }
        }
    }

    private void CreateVisuals(Vector2 origin, Vector2 forward, Sprite pixelSprite, Sprite circleSprite)
    {
        Vector2 end = origin + forward * BeamLength;
        Vector2 midpoint = (origin + end) * 0.5f;
        float rotation = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg - 90f;
        visuals = new SpriteRenderer[7];
        visuals[0] = CreateBeam("Icar Lance Outer Glow", midpoint, new Vector2(0.82f, BeamLength), rotation, new Color(1f, 0.42f, 0.05f, 0.22f), 26, pixelSprite);
        visuals[1] = CreateBeam("Icar Lance Gold", midpoint, new Vector2(0.34f, BeamLength), rotation, new Color(1f, 0.68f, 0.08f, 0.78f), 27, pixelSprite);
        visuals[2] = CreateBeam("Icar Lance White Core", midpoint, new Vector2(0.1f, BeamLength), rotation, new Color(1f, 0.98f, 0.72f), 28, pixelSprite);

        Vector2 side = new Vector2(-forward.y, forward.x) * 0.31f;
        visuals[3] = CreateBeam("Icar Lance Left Rail", midpoint + side, new Vector2(0.045f, BeamLength * 0.94f), rotation, new Color(0.2f, 0.92f, 1f, 0.72f), 27, pixelSprite);
        visuals[4] = CreateBeam("Icar Lance Right Rail", midpoint - side, new Vector2(0.045f, BeamLength * 0.94f), rotation, new Color(1f, 0.18f, 0.64f, 0.72f), 27, pixelSprite);
        visuals[5] = CreateBeam("Icar Muzzle Flash", origin, new Vector2(1.35f, 1.35f), 0f, new Color(1f, 0.58f, 0.08f, 0.62f), 29, circleSprite);
        visuals[6] = CreateBeam("Icar Beam Tip", end, new Vector2(0.66f, 0.66f), 0f, new Color(1f, 0.92f, 0.52f, 0.5f), 29, circleSprite);
    }

    private SpriteRenderer CreateBeam(
        string visualName,
        Vector2 position,
        Vector2 scale,
        float rotation,
        Color color,
        int sortingOrder,
        Sprite sprite)
    {
        GameObject visual = new GameObject(visualName);
        visual.transform.SetParent(transform);
        visual.transform.position = position;
        visual.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private readonly struct TargetHit
    {
        public readonly CarDamage Target;
        public readonly float Distance;

        public TargetHit(CarDamage target, float distance)
        {
            Target = target;
            Distance = distance;
        }
    }
}
