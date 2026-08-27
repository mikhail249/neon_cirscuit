using UnityEngine;

public sealed class WetPuddle : MonoBehaviour
{
    private NeonCircuitGame game;
    private BoxCollider2D trigger;
    private SpriteRenderer[] visuals = new SpriteRenderer[0];
    private Color[] baseColors = new Color[0];
    private Vector3[] baseScales = new Vector3[0];
    private float skidStrength;
    private float phase;
    private float splashUntil;
    private bool wasActive;

    public void Initialize(
        NeonCircuitGame owner,
        float strength,
        BoxCollider2D puddleTrigger,
        SpriteRenderer[] puddleVisuals,
        float animationPhase)
    {
        game = owner;
        skidStrength = Mathf.Clamp01(strength);
        trigger = puddleTrigger;
        visuals = puddleVisuals ?? new SpriteRenderer[0];
        phase = animationPhase;
        baseColors = new Color[visuals.Length];
        baseScales = new Vector3[visuals.Length];

        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null)
            {
                continue;
            }

            baseColors[i] = visuals[i].color;
            baseScales[i] = visuals[i].transform.localScale;
        }

        RefreshActiveState(true);
    }

    private void Update()
    {
        RefreshActiveState(false);
        if (!wasActive)
        {
            return;
        }

        float pulse = 0.5f + Mathf.Sin(Time.time * 2.1f + phase) * 0.5f;
        float splash = Time.time < splashUntil ? Mathf.Clamp01((splashUntil - Time.time) / 0.26f) : 0f;
        for (int i = 0; i < visuals.Length; i++)
        {
            SpriteRenderer visual = visuals[i];
            if (visual == null)
            {
                continue;
            }

            Color color = baseColors[i];
            if (i >= 4)
            {
                color.a *= Mathf.Lerp(0.62f, 1f, pulse);
            }
            color = Color.Lerp(color, new Color(0.72f, 1f, 1f, color.a), splash * 0.55f);
            visual.color = color;
            visual.transform.localScale = baseScales[i] * (1f + splash * (i < 2 ? 0.035f : 0.11f));
        }
    }

    private void RefreshActiveState(bool force)
    {
        bool active = game != null && game.RainPuddlesActive;
        if (!force && active == wasActive)
        {
            return;
        }

        wasActive = active;
        if (trigger != null)
        {
            trigger.enabled = active;
        }

        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null)
            {
                visuals[i].enabled = active;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!wasActive || other == null)
        {
            return;
        }

        GameObject vehicle = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;
        ArcadeCarController player = vehicle.GetComponent<ArcadeCarController>();
        if (player != null)
        {
            player.ApplyPuddleSkid(skidStrength);
            splashUntil = Time.time + 0.26f;
            game.PlayPuddleSfx(skidStrength);
            return;
        }

        CircuitAI rival = vehicle.GetComponent<CircuitAI>();
        if (rival != null)
        {
            rival.ApplyPuddleSkid(skidStrength * 0.92f);
            splashUntil = Time.time + 0.22f;
        }
    }
}
