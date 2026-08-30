using System.Collections.Generic;
using UnityEngine;

public sealed class NeonPhantomSwarm : MonoBehaviour
{
    private const int DroneCount = 4;
    private const float Duration = 4.4f;
    private const float AttackRange = 8.5f;
    private const float ShotInterval = 0.48f;
    private const float DroneDamage = 3.6f;
    private const float TracerLifetime = 0.18f;

    private readonly Transform[] droneRoots = new Transform[DroneCount];
    private readonly SpriteRenderer[] droneAuras = new SpriteRenderer[DroneCount];
    private readonly List<SwarmTracer> tracers = new List<SwarmTracer>();
    private GameObject owner;
    private Sprite pixelSprite;
    private Sprite circleSprite;
    private float damageAmount;
    private float createdAt;
    private float nextShotAt;
    private int firingDrone;
    private int shotsFired;

    public void Initialize(GameObject weaponOwner, Sprite pixel, Sprite circle, float damageMultiplier)
    {
        owner = weaponOwner;
        pixelSprite = pixel;
        circleSprite = circle;
        damageAmount = DroneDamage * Mathf.Max(0.1f, damageMultiplier);
        createdAt = Time.time;
        nextShotAt = Time.time + 0.32f;
        transform.position = owner != null ? owner.transform.position : Vector3.zero;
        CreateDrones();
    }

    public bool IsOwnedBy(GameObject candidate)
    {
        return owner == candidate;
    }

    private void Update()
    {
        if (owner == null || Time.time - createdAt >= Duration)
        {
            Destroy(gameObject);
            return;
        }

        CarDamage ownerDamage = owner.GetComponent<CarDamage>();
        if (ownerDamage != null && ownerDamage.IsBroken)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = owner.transform.position;
        UpdateDronePositions();
        UpdateTracers();

        if (Time.time >= nextShotAt)
        {
            nextShotAt = Time.time + ShotInterval;
            FireNextDrone();
        }
    }

    private void CreateDrones()
    {
        for (int i = 0; i < DroneCount; i++)
        {
            GameObject root = new GameObject("Phantom Drone " + (i + 1));
            root.transform.SetParent(transform);
            droneRoots[i] = root.transform;

            Color accent = GetDroneColor(i);
            droneAuras[i] = CreateVisual(root.transform, "Drone Aura", Vector2.zero, new Vector2(0.82f, 0.82f), new Color(accent.r, accent.g, accent.b, 0.2f), 24, circleSprite);
            CreateVisual(root.transform, "Drone Body", Vector2.zero, new Vector2(0.26f, 0.58f), new Color(0.025f, 0.055f, 0.095f), 25, pixelSprite);
            CreateVisual(root.transform, "Drone Core", new Vector2(0f, 0.05f), new Vector2(0.1f, 0.3f), accent, 26, pixelSprite);
            CreateVisual(root.transform, "Drone Left Wing", new Vector2(-0.19f, -0.02f), new Vector2(0.2f, 0.1f), accent, 25, pixelSprite, -24f);
            CreateVisual(root.transform, "Drone Right Wing", new Vector2(0.19f, -0.02f), new Vector2(0.2f, 0.1f), accent, 25, pixelSprite, 24f);
        }
    }

    private void UpdateDronePositions()
    {
        float elapsed = Time.time - createdAt;
        float deploy = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.34f));
        for (int i = 0; i < DroneCount; i++)
        {
            float angle = elapsed * 145f + i * (360f / DroneCount);
            float radians = angle * Mathf.Deg2Rad;
            float radius = (1.48f + Mathf.Sin(elapsed * 3.2f + i) * 0.16f) * deploy;
            droneRoots[i].localPosition = new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0f);
            droneRoots[i].localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

            if (droneAuras[i] != null)
            {
                float pulse = 0.86f + Mathf.Sin(Time.unscaledTime * 17f + i * 1.8f) * 0.14f;
                droneAuras[i].transform.localScale = Vector3.one * (0.82f * pulse);
            }
        }
    }

    private void FireNextDrone()
    {
        CarDamage target = FindNearestTarget();
        if (target == null)
        {
            return;
        }

        int droneIndex = firingDrone % DroneCount;
        Transform drone = droneRoots[droneIndex];
        firingDrone = (firingDrone + 1) % DroneCount;
        shotsFired++;
        Vector2 impactPoint = target.transform.position;
        target.TakeDamageAt(damageAmount, impactPoint);

        Rigidbody2D targetBody = target.GetComponent<Rigidbody2D>();
        if (targetBody != null)
        {
            targetBody.linearVelocity *= 0.94f;
            targetBody.angularVelocity *= 0.9f;
        }
        CircuitAI rival = target.GetComponent<CircuitAI>();
        if (rival != null && shotsFired % 3 == 0)
        {
            rival.ApplyPlasmaImpact();
        }

        CreateTracer(drone.position, impactPoint, droneIndex);
    }

    private CarDamage FindNearestTarget()
    {
        CarDamage[] cars = FindObjectsByType<CarDamage>();
        CarDamage nearest = null;
        float bestDistance = AttackRange;
        for (int i = 0; i < cars.Length; i++)
        {
            CarDamage candidate = cars[i];
            if (candidate == null || candidate.IsBroken || candidate.gameObject == owner)
            {
                continue;
            }

            float distance = Vector2.Distance(transform.position, candidate.transform.position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                nearest = candidate;
            }
        }
        return nearest;
    }

    private void CreateTracer(Vector2 start, Vector2 end, int colorIndex)
    {
        Vector2 delta = end - start;
        if (delta.sqrMagnitude < 0.01f)
        {
            return;
        }

        Color color = GetDroneColor(colorIndex);
        GameObject root = new GameObject("Phantom Drone Tracer");
        root.transform.SetParent(transform);
        root.transform.position = (start + end) * 0.5f;
        root.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg - 90f);

        SpriteRenderer glow = CreateVisual(root.transform, "Tracer Glow", Vector2.zero, new Vector2(0.16f, delta.magnitude), new Color(color.r, color.g, color.b, 0.34f), 27, pixelSprite);
        SpriteRenderer core = CreateVisual(root.transform, "Tracer Core", Vector2.zero, new Vector2(0.045f, delta.magnitude), color, 28, pixelSprite);
        tracers.Add(new SwarmTracer(root, glow, core, Time.unscaledTime));
    }

    private void UpdateTracers()
    {
        for (int i = tracers.Count - 1; i >= 0; i--)
        {
            SwarmTracer tracer = tracers[i];
            float progress = Mathf.Clamp01((Time.unscaledTime - tracer.CreatedAt) / TracerLifetime);
            if (tracer.Glow != null)
            {
                Color color = tracer.Glow.color;
                color.a = 0.34f * (1f - progress);
                tracer.Glow.color = color;
            }
            if (tracer.Core != null)
            {
                Color color = tracer.Core.color;
                color.a = 1f - progress;
                tracer.Core.color = color;
            }
            if (progress >= 1f)
            {
                if (tracer.Root != null) Destroy(tracer.Root);
                tracers.RemoveAt(i);
            }
        }
    }

    private static SpriteRenderer CreateVisual(
        Transform parent,
        string visualName,
        Vector2 localPosition,
        Vector2 scale,
        Color color,
        int sortingOrder,
        Sprite sprite,
        float localRotation = 0f)
    {
        GameObject visual = new GameObject(visualName);
        visual.transform.SetParent(parent);
        visual.transform.localPosition = localPosition;
        visual.transform.localRotation = Quaternion.Euler(0f, 0f, localRotation);
        visual.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static Color GetDroneColor(int index)
    {
        switch (index % DroneCount)
        {
            case 1: return new Color(1f, 0.12f, 0.68f);
            case 2: return new Color(0.6f, 0.32f, 1f);
            case 3: return new Color(1f, 0.64f, 0.08f);
            default: return new Color(0.1f, 0.95f, 1f);
        }
    }

    private readonly struct SwarmTracer
    {
        public readonly GameObject Root;
        public readonly SpriteRenderer Glow;
        public readonly SpriteRenderer Core;
        public readonly float CreatedAt;

        public SwarmTracer(GameObject root, SpriteRenderer glow, SpriteRenderer core, float createdAt)
        {
            Root = root;
            Glow = glow;
            Core = core;
            CreatedAt = createdAt;
        }
    }
}
