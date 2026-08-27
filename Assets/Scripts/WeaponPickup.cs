using UnityEngine;

public sealed class WeaponPickup : MonoBehaviour
{
    private int ammoAmount;
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
        int pickupAmmo,
        float pickupRespawnDelay,
        float pickupTrackT,
        float pickupTrackLane,
        SpriteRenderer[] pickupVisuals,
        float animationPhase)
    {
        ammoAmount = Mathf.Max(1, pickupAmmo);
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

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.8f + phase) * 0.09f;
        transform.rotation = Quaternion.Euler(0f, 0f, baseRotation + Time.unscaledTime * 34f);
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
        PlayerWeaponSystem weapon = other.GetComponent<PlayerWeaponSystem>();
        TryCollect(weapon);
    }

    public bool TryCollect(PlayerWeaponSystem weapon)
    {
        if (!available || weapon == null)
        {
            return false;
        }

        SetAvailable(false);
        if (!weapon.TryAddAmmo(ammoAmount))
        {
            SetAvailable(true);
            return false;
        }

        respawnAt = Time.time + respawnDelay;
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
