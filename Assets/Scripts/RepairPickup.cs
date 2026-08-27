using UnityEngine;

public sealed class RepairPickup : MonoBehaviour
{
    private float repairAmount;
    private float respawnDelay;
    private SpriteRenderer[] visuals = new SpriteRenderer[0];
    private Vector3[] baseScales = new Vector3[0];
    private float respawnAt;
    private float phase;
    private float baseRotation;
    private float trackT;
    private float trackLane;
    private bool available;

    public bool IsAvailable { get { return available; } }
    public float TrackT { get { return trackT; } }
    public float TrackLane { get { return trackLane; } }

    public void Initialize(
        float pickupRepairAmount,
        float pickupRespawnDelay,
        float pickupTrackT,
        float pickupTrackLane,
        SpriteRenderer[] pickupVisuals,
        float animationPhase)
    {
        repairAmount = Mathf.Max(1f, pickupRepairAmount);
        respawnDelay = Mathf.Max(1f, pickupRespawnDelay);
        trackT = pickupTrackT;
        trackLane = pickupTrackLane;
        visuals = pickupVisuals ?? new SpriteRenderer[0];
        baseScales = new Vector3[visuals.Length];
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null)
            {
                baseScales[i] = visuals[i].transform.localScale;
            }
        }

        phase = animationPhase;
        baseRotation = transform.eulerAngles.z;
        SetAvailable(true);
    }

    private void Update()
    {
        if (!available)
        {
            if (Time.time >= respawnAt)
            {
                SetAvailable(true);
            }

            return;
        }

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 5.4f + phase) * 0.1f;
        transform.rotation = Quaternion.Euler(0f, 0f, baseRotation + Time.unscaledTime * 26f);
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null)
            {
                visuals[i].transform.localScale = baseScales[i] * pulse;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollect(other.GetComponent<CarDamage>());
    }

    public bool TryCollect(CarDamage damage)
    {
        if (!available || damage == null || !damage.TryRepair(repairAmount))
        {
            return false;
        }

        respawnAt = Time.time + respawnDelay;
        SetAvailable(false);
        if (damage.GetComponent<ArcadeCarController>() != null)
        {
            NeonCircuitGame game = FindAnyObjectByType<NeonCircuitGame>();
            if (game != null)
            {
                game.PlayPickupSfx(true);
            }
        }
        return true;
    }

    public void ResetPickup()
    {
        respawnAt = 0f;
        SetAvailable(true);
    }

    private void SetAvailable(bool value)
    {
        available = value;
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] != null)
            {
                visuals[i].enabled = value;
                visuals[i].transform.localScale = baseScales[i];
            }
        }
    }
}
