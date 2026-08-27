using System.Collections.Generic;
using UnityEngine;

public sealed class TrackObstacle : MonoBehaviour
{
    private const float HitCooldown = 0.45f;
    private readonly Dictionary<GameObject, float> lastHitTimes = new Dictionary<GameObject, float>();
    private NeonCircuitGame game;
    private Rigidbody2D obstacleBody;
    private Vector2 startPosition;
    private float startRotation;
    private Color debrisColor = Color.gray;
    private float damageMultiplier = 1f;
    private float fixedCollisionDamage = -1f;
    private float scatterMultiplier = 1f;
    private float settleAfter;
    private bool scattered;
    private bool countedForStory;

    public void Initialize(NeonCircuitGame owner, float damageScale, float scatterScale, Color fragmentColor, float fixedDamage = -1f)
    {
        game = owner;
        damageMultiplier = Mathf.Max(0f, damageScale);
        fixedCollisionDamage = fixedDamage;
        scatterMultiplier = Mathf.Max(0.15f, scatterScale);
        debrisColor = fragmentColor;
        startPosition = transform.position;
        startRotation = transform.eulerAngles.z;

        obstacleBody = GetComponent<Rigidbody2D>();
        if (obstacleBody == null)
        {
            obstacleBody = gameObject.AddComponent<Rigidbody2D>();
        }

        obstacleBody.bodyType = RigidbodyType2D.Kinematic;
        obstacleBody.gravityScale = 0f;
        float weightFactor = Mathf.Clamp01(damageMultiplier / scatterMultiplier);
        obstacleBody.mass = Mathf.Clamp(damageMultiplier * 2.4f / scatterMultiplier, 0.55f, 3.8f);
        obstacleBody.linearDamping = Mathf.Lerp(0.85f, 2.25f, weightFactor);
        obstacleBody.angularDamping = Mathf.Lerp(0.65f, 1.65f, weightFactor);
        obstacleBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        obstacleBody.interpolation = RigidbodyInterpolation2D.Interpolate;

        Collider2D obstacleCollider = GetComponent<Collider2D>();
        if (obstacleCollider != null)
        {
            PhysicsMaterial2D material = new PhysicsMaterial2D(name + " Impact Material");
            material.friction = Mathf.Lerp(0.26f, 0.52f, weightFactor);
            material.bounciness = Mathf.Lerp(0.48f, 0.18f, weightFactor);
            obstacleCollider.sharedMaterial = material;
        }
    }

    private void Update()
    {
        if (!scattered || obstacleBody == null || obstacleBody.bodyType != RigidbodyType2D.Dynamic || Time.time < settleAfter)
        {
            return;
        }

        if (obstacleBody.linearVelocity.sqrMagnitude <= 0.035f && Mathf.Abs(obstacleBody.angularVelocity) <= 7f)
        {
            FreezeWhereItLanded();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject car = collision.collider.attachedRigidbody != null
            ? collision.collider.attachedRigidbody.gameObject
            : collision.collider.gameObject;
        CarDamage carDamage = car.GetComponent<CarDamage>();
        if (carDamage == null)
        {
            return;
        }

        float lastHitTime;
        if (lastHitTimes.TryGetValue(car, out lastHitTime) && Time.time - lastHitTime < HitCooldown)
        {
            return;
        }

        lastHitTimes[car] = Time.time;
        float impactSpeed = collision.relativeVelocity.magnitude;
        float damage = fixedCollisionDamage > 0f
            ? fixedCollisionDamage
            : Mathf.Clamp((impactSpeed - 1.5f) * 5.5f * damageMultiplier, 3f, 32f);

        Vector2 impactPoint = car.transform.position;
        if (collision.contactCount > 0)
        {
            impactPoint = collision.GetContact(0).point;
        }

        carDamage.TakeDamageAt(damage, impactPoint);

        if (impactSpeed >= 2.2f)
        {
            carDamage.TriggerImpactFeedback(impactPoint, collision.relativeVelocity, impactSpeed);
        }

        Rigidbody2D carBody = car.GetComponent<Rigidbody2D>();
        ScatterFromImpact(car, carBody, collision, impactPoint, impactSpeed);

        if (carBody != null && carBody.bodyType == RigidbodyType2D.Dynamic)
        {
            float speedRetention = Mathf.Lerp(0.84f, 0.64f, Mathf.Clamp01(damageMultiplier));
            float angularRetention = Mathf.Lerp(0.82f, 0.64f, Mathf.Clamp01(damageMultiplier));
            carBody.linearVelocity *= speedRetention;
            carBody.angularVelocity *= angularRetention;
        }

        CircuitAI ai = car.GetComponent<CircuitAI>();
        if (ai != null)
        {
            ai.HitObstacle(transform.position);
        }
    }

    private void ScatterFromImpact(GameObject car, Rigidbody2D carBody, Collision2D collision, Vector2 impactPoint, float impactSpeed)
    {
        if (obstacleBody == null || impactSpeed < 0.65f)
        {
            return;
        }

        if (obstacleBody.bodyType != RigidbodyType2D.Dynamic)
        {
            obstacleBody.bodyType = RigidbodyType2D.Dynamic;
        }

        obstacleBody.WakeUp();
        Vector2 awayFromCar = (Vector2)transform.position - (Vector2)car.transform.position;
        if (awayFromCar.sqrMagnitude < 0.001f && collision.contactCount > 0)
        {
            awayFromCar = -collision.GetContact(0).normal;
        }
        awayFromCar = awayFromCar.sqrMagnitude > 0.001f ? awayFromCar.normalized : Vector2.up;

        Vector2 travelDirection = carBody != null && carBody.linearVelocity.sqrMagnitude > 0.04f
            ? carBody.linearVelocity.normalized
            : collision.relativeVelocity.normalized;
        Vector2 launchDirection = awayFromCar * 0.58f + travelDirection * 0.72f;
        if (launchDirection.sqrMagnitude < 0.001f)
        {
            launchDirection = awayFromCar;
        }

        float impulse = Mathf.Clamp(impactSpeed * 0.42f, 0.75f, 7.6f) * scatterMultiplier;
        obstacleBody.AddForce(launchDirection.normalized * impulse, ForceMode2D.Impulse);

        float spinSign = Mathf.Sign(Vector2.SignedAngle(awayFromCar, travelDirection));
        if (Mathf.Abs(spinSign) < 0.1f)
        {
            spinSign = ((GetInstanceID() + Mathf.RoundToInt(Time.time * 10f)) & 1) == 0 ? -1f : 1f;
        }
        obstacleBody.angularVelocity += spinSign * Mathf.Clamp(impactSpeed * 28f * scatterMultiplier, 45f, 520f);

        scattered = true;
        settleAfter = Time.time + Mathf.Lerp(0.8f, 1.35f, Mathf.Clamp01(impactSpeed / 18f));

        if (!countedForStory && game != null && car.GetComponent<ArcadeCarController>() != null)
        {
            countedForStory = true;
            game.RegisterStoryObstacleSmashed();
        }

        if (game != null && impactSpeed >= 1.4f)
        {
            game.SpawnObstacleDebris(
                impactPoint,
                launchDirection,
                debrisColor,
                Mathf.Clamp01(impactSpeed / 16f) * Mathf.Min(scatterMultiplier, 1.35f));
        }
    }

    private void FreezeWhereItLanded()
    {
        obstacleBody.linearVelocity = Vector2.zero;
        obstacleBody.angularVelocity = 0f;
        obstacleBody.bodyType = RigidbodyType2D.Kinematic;
        scattered = false;
    }

    public void ResetObstacle()
    {
        if (obstacleBody == null)
        {
            return;
        }

        obstacleBody.linearVelocity = Vector2.zero;
        obstacleBody.angularVelocity = 0f;
        obstacleBody.bodyType = RigidbodyType2D.Kinematic;
        obstacleBody.position = startPosition;
        obstacleBody.rotation = startRotation;
        transform.SetPositionAndRotation(startPosition, Quaternion.Euler(0f, 0f, startRotation));
        scattered = false;
        countedForStory = false;
        settleAfter = 0f;
        lastHitTimes.Clear();
    }
}

public sealed class ObstacleBeacon : MonoBehaviour
{
    private SpriteRenderer[] lights = new SpriteRenderer[0];
    private Color[] baseColors = new Color[0];
    private Vector3[] baseScales = new Vector3[0];
    private float phase;

    public void Initialize(float animationPhase, SpriteRenderer[] animatedLights)
    {
        phase = animationPhase;
        lights = animatedLights ?? new SpriteRenderer[0];
        baseColors = new Color[lights.Length];
        baseScales = new Vector3[lights.Length];

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
            {
                continue;
            }

            baseColors[i] = lights[i].color;
            baseScales[i] = lights[i].transform.localScale;
        }
    }

    private void Update()
    {
        float pulse = (Mathf.Sin(Time.unscaledTime * 4.2f + phase) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(0.52f, 1f, pulse);
        float scale = Mathf.Lerp(0.92f, 1.1f, pulse);

        for (int i = 0; i < lights.Length; i++)
        {
            SpriteRenderer light = lights[i];
            if (light == null)
            {
                continue;
            }

            Color color = baseColors[i];
            color.a *= alpha;
            light.color = color;
            light.transform.localScale = baseScales[i] * scale;
        }
    }
}
