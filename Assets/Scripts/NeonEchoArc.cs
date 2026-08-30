using System.Collections.Generic;
using UnityEngine;

public sealed class NeonEchoArc : MonoBehaviour
{
    private const float FirstTargetRange = 9.5f;
    private const float ChainRange = 4.6f;
    private const int MaximumTargets = 3;
    private const float VisualLifetime = 0.3f;

    private readonly List<SpriteRenderer> visuals = new List<SpriteRenderer>();
    private GameObject owner;
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
        List<CarDamage> targets = FindChainTargets(origin, direction);
        Vector2 segmentStart = origin;
        for (int i = 0; i < targets.Count; i++)
        {
            CarDamage target = targets[i];
            Vector2 impactPoint = target.transform.position;
            CreateArc(segmentStart, impactPoint, pixelSprite, circleSprite, i);

            float damage = Mathf.Max(4f, 12f - i * 2.5f) * Mathf.Max(0.1f, damageMultiplier);
            target.TakeDamageAt(damage, impactPoint);
            ApplyDisruption(target, Mathf.Lerp(0.58f, 0.78f, i / 2f));
            segmentStart = impactPoint;
        }

        if (targets.Count == 0)
        {
            CreateArc(origin, origin + direction * 3.4f, pixelSprite, circleSprite, 0);
        }
    }

    public bool IsOwnedBy(GameObject candidate)
    {
        return owner == candidate;
    }

    private void Update()
    {
        float progress = Mathf.Clamp01((Time.unscaledTime - createdAt) / VisualLifetime);
        float pulse = 0.82f + Mathf.Sin(Time.unscaledTime * 58f) * 0.18f;
        for (int i = 0; i < visuals.Count; i++)
        {
            SpriteRenderer renderer = visuals[i];
            if (renderer == null)
            {
                continue;
            }

            Color color = renderer.color;
            color.a = Mathf.Min(color.a, (1f - progress) * pulse);
            renderer.color = color;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private List<CarDamage> FindChainTargets(Vector2 origin, Vector2 forward)
    {
        List<CarDamage> result = new List<CarDamage>(MaximumTargets);
        CarDamage[] cars = FindObjectsByType<CarDamage>();
        CarDamage first = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < cars.Length; i++)
        {
            CarDamage candidate = cars[i];
            if (!IsValidTarget(candidate, result))
            {
                continue;
            }

            Vector2 offset = (Vector2)candidate.transform.position - origin;
            float distance = offset.magnitude;
            if (distance > FirstTargetRange || distance < 0.1f)
            {
                continue;
            }

            float forwardAlignment = Vector2.Dot(offset / distance, forward);
            if (forwardAlignment < 0.22f)
            {
                continue;
            }

            float score = distance * Mathf.Lerp(1.55f, 0.72f, Mathf.InverseLerp(0.22f, 1f, forwardAlignment));
            if (score < bestScore)
            {
                bestScore = score;
                first = candidate;
            }
        }

        if (first == null)
        {
            return result;
        }

        result.Add(first);
        while (result.Count < MaximumTargets)
        {
            Vector2 previous = result[result.Count - 1].transform.position;
            CarDamage next = null;
            float closestDistance = ChainRange;
            for (int i = 0; i < cars.Length; i++)
            {
                CarDamage candidate = cars[i];
                if (!IsValidTarget(candidate, result))
                {
                    continue;
                }

                float distance = Vector2.Distance(previous, candidate.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    next = candidate;
                }
            }

            if (next == null)
            {
                break;
            }

            result.Add(next);
        }

        return result;
    }

    private bool IsValidTarget(CarDamage candidate, List<CarDamage> selected)
    {
        return candidate != null &&
               !candidate.IsBroken &&
               candidate.gameObject != owner &&
               !selected.Contains(candidate);
    }

    private static void ApplyDisruption(CarDamage target, float retention)
    {
        CircuitAI rival = target.GetComponent<CircuitAI>();
        if (rival != null)
        {
            rival.ApplyPlasmaImpact();
            return;
        }

        Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
        if (targetBody != null)
        {
            targetBody.linearVelocity *= retention;
            targetBody.angularVelocity *= 0.72f;
        }
    }

    private void CreateArc(Vector2 start, Vector2 end, Sprite pixelSprite, Sprite circleSprite, int chainIndex)
    {
        Vector2 delta = end - start;
        float length = delta.magnitude;
        if (length <= 0.02f)
        {
            return;
        }

        Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
        Vector2 firstKink = Vector2.Lerp(start, end, 0.34f) + normal * (chainIndex % 2 == 0 ? 0.2f : -0.2f);
        Vector2 secondKink = Vector2.Lerp(start, end, 0.67f) - normal * (chainIndex % 2 == 0 ? 0.17f : -0.17f);
        Color glow = new Color(0.15f, 0.88f, 1f, 0.42f);
        Color core = new Color(0.76f, 1f, 1f, 0.96f);

        CreateSegment(start, firstKink, 0.18f, glow, 27, pixelSprite);
        CreateSegment(firstKink, secondKink, 0.18f, glow, 27, pixelSprite);
        CreateSegment(secondKink, end, 0.18f, glow, 27, pixelSprite);
        CreateSegment(start, firstKink, 0.055f, core, 28, pixelSprite);
        CreateSegment(firstKink, secondKink, 0.055f, core, 28, pixelSprite);
        CreateSegment(secondKink, end, 0.055f, core, 28, pixelSprite);
        CreatePoint(end, 0.68f, glow, 26, circleSprite);
        CreatePoint(end, 0.19f, core, 29, circleSprite);
    }

    private void CreateSegment(Vector2 start, Vector2 end, float width, Color color, int order, Sprite sprite)
    {
        Vector2 delta = end - start;
        GameObject segment = new GameObject("Echo Arc Segment");
        segment.transform.SetParent(transform);
        segment.transform.position = (start + end) * 0.5f;
        segment.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);
        segment.transform.localScale = new Vector3(width, delta.magnitude, 1f);
        SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        visuals.Add(renderer);
    }

    private void CreatePoint(Vector2 position, float size, Color color, int order, Sprite sprite)
    {
        GameObject point = new GameObject("Echo Arc Impact");
        point.transform.SetParent(transform);
        point.transform.position = position;
        point.transform.localScale = Vector3.one * size;
        SpriteRenderer renderer = point.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        visuals.Add(renderer);
    }
}
