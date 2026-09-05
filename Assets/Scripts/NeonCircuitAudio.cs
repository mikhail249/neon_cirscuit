using UnityEngine;

public sealed partial class NeonCircuitGame
{
    private AudioSource menuMusicSource;
    private AudioSource engineLoopSource;
    private AudioSource weatherLoopSource;
    private AudioSource uiSfxSource;
    private AudioSource raceSfxSource;

    private AudioClip menuMusicClip;
    private AudioClip engineLoopClip;
    private AudioClip weatherLoopClip;
    private AudioClip menuHoverClip;
    private AudioClip menuClickClip;
    private AudioClip menuStartClip;
    private AudioClip countdownClip;
    private AudioClip raceGoClip;
    private AudioClip pickupClip;
    private AudioClip repairClip;
    private AudioClip rocketClip;
    private AudioClip plasmaClip;
    private AudioClip echoArcClip;
    private AudioClip orbitMineClip;
    private AudioClip icarLanceClip;
    private AudioClip phantomSwarmClip;
    private AudioClip finishClip;
    private AudioClip wreckClip;
    private AudioClip puddleClip;

    private int activeMenuHoverId = -1;
    private float nextMenuHoverSoundAt;
    private int lastCountdownCue = 4;
    private bool raceStartedForAudio;
    private bool raceFinishedForAudio;

    private void SetupExtendedAudio()
    {
        if (menuMusicSource != null)
        {
            return;
        }

        menuMusicClip = CreateMenuMusicClip();
        engineLoopClip = CreateEngineLoopClip();
        weatherLoopClip = CreateNoiseLoopClip("Weather Ambience", 2.4f, 0.22f);
        menuHoverClip = CreateSweepClip("Menu Hover", 620f, 910f, 0.055f, 0.24f);
        menuClickClip = CreateSweepClip("Menu Click", 420f, 260f, 0.085f, 0.34f);
        menuStartClip = CreateSweepClip("Race Deploy", 180f, 760f, 0.32f, 0.42f);
        countdownClip = CreateSweepClip("Countdown", 390f, 330f, 0.11f, 0.34f);
        raceGoClip = CreateSweepClip("Race Go", 520f, 1120f, 0.28f, 0.48f);
        pickupClip = CreateSweepClip("Ammo Pickup", 540f, 980f, 0.14f, 0.34f);
        repairClip = CreateSweepClip("Repair Pickup", 310f, 740f, 0.22f, 0.38f);
        rocketClip = CreateSweepClip("Rocket Fire", 150f, 72f, 0.18f, 0.5f);
        plasmaClip = CreateSweepClip("Plasma Fire", 920f, 520f, 0.1f, 0.36f);
        echoArcClip = CreateSweepClip("Echo Arc Fire", 1460f, 410f, 0.2f, 0.4f);
        orbitMineClip = CreateSweepClip("Orbital Mine Deploy", 210f, 96f, 0.28f, 0.46f);
        icarLanceClip = CreateSweepClip("Icar Lance Fire", 360f, 1720f, 0.24f, 0.46f);
        phantomSwarmClip = CreateSweepClip("Phantom Swarm Deploy", 680f, 1380f, 0.38f, 0.38f);
        finishClip = CreateSweepClip("Finish", 440f, 1320f, 0.55f, 0.42f);
        wreckClip = CreateSweepClip("Vehicle Wrecked", 190f, 48f, 0.48f, 0.52f);
        puddleClip = CreateSweepClip("Puddle Splash", 290f, 72f, 0.2f, 0.36f);

        menuMusicSource = CreateLoopSource(menuMusicClip);
        engineLoopSource = CreateLoopSource(engineLoopClip);
        weatherLoopSource = CreateLoopSource(weatherLoopClip);
        uiSfxSource = CreateOneShotSource(0.82f);
        raceSfxSource = CreateOneShotSource(0.9f);
    }

    private AudioSource CreateLoopSource(AudioClip clip)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        return source;
    }

    private AudioSource CreateOneShotSource(float volume)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    private AudioClip CreateMenuMusicClip()
    {
        const int sampleRate = 22050;
        const float duration = 4f;
        int length = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[length];
        float[] notes = { 110f, 130.81f, 164.81f, 196f, 164.81f, 130.81f, 146.83f, 174.61f };

        for (int i = 0; i < length; i++)
        {
            float time = i / (float)sampleRate;
            int noteIndex = Mathf.FloorToInt(time * 2f) % notes.Length;
            float beat = Mathf.Repeat(time * 2f, 1f);
            float envelope = Mathf.Lerp(1f, 0.16f, beat);
            float frequency = notes[noteIndex];
            float bass = Mathf.Sin(time * frequency * Mathf.PI * 2f);
            float shimmer = Mathf.Sin(time * frequency * 2f * Mathf.PI * 2f) * 0.24f;
            float pulse = Mathf.Sign(Mathf.Sin(time * frequency * 0.5f * Mathf.PI * 2f)) * 0.12f;
            samples[i] = (bass * 0.42f + shimmer + pulse) * envelope * 0.24f;
        }

        AudioClip clip = AudioClip.Create("Neon Menu Loop", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateEngineLoopClip()
    {
        const int sampleRate = 22050;
        const float duration = 1f;
        int length = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[length];
        for (int i = 0; i < length; i++)
        {
            float time = i / (float)sampleRate;
            float fundamental = Mathf.Sin(time * 55f * Mathf.PI * 2f);
            float second = Mathf.Sin(time * 110f * Mathf.PI * 2f) * 0.38f;
            float third = Mathf.Sin(time * 165f * Mathf.PI * 2f) * 0.16f;
            float grit = Mathf.Sin(time * 660f * Mathf.PI * 2f) * Mathf.Sin(time * 19f * Mathf.PI * 2f) * 0.07f;
            samples[i] = (fundamental * 0.52f + second + third + grit) * 0.36f;
        }

        AudioClip clip = AudioClip.Create("Arcade Engine Loop", length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateNoiseLoopClip(string clipName, float duration, float volume)
    {
        const int sampleRate = 22050;
        int length = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[length];
        float smoothed = 0f;
        for (int i = 0; i < length; i++)
        {
            float noise = Mathf.Repeat(Mathf.Sin((i + 17f) * 12.9898f) * 43758.5453f, 1f) * 2f - 1f;
            smoothed = Mathf.Lerp(smoothed, noise, 0.08f);
            float wind = Mathf.Sin(i / (float)sampleRate * Mathf.PI * 2f * 0.7f) * 0.2f;
            samples[i] = (smoothed + wind) * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateSweepClip(string clipName, float startFrequency, float endFrequency, float duration, float volume)
    {
        const int sampleRate = 22050;
        int length = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[length];
        float phase = 0f;
        for (int i = 0; i < length; i++)
        {
            float progress = i / (float)Mathf.Max(1, length - 1);
            float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
            phase += frequency / sampleRate * Mathf.PI * 2f;
            float envelope = Mathf.Pow(1f - progress, 1.35f);
            float tone = Mathf.Sin(phase) * 0.78f + Mathf.Sin(phase * 2.02f) * 0.22f;
            samples[i] = tone * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, length, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void UpdateExtendedAudio()
    {
        if (menuMusicSource == null || engineLoopSource == null || weatherLoopSource == null)
        {
            return;
        }

        if (!menuMusicSource.isPlaying) menuMusicSource.Play();
        if (!engineLoopSource.isPlaying) engineLoopSource.Play();
        if (!weatherLoopSource.isPlaying) weatherLoopSource.Play();

        float delta = Time.unscaledDeltaTime;
        bool menuActive = mainMenuOpen || garageOpen;
        menuMusicSource.volume = Mathf.MoveTowards(menuMusicSource.volume, menuActive ? 0.18f : 0.035f, delta * 0.32f);

        bool driving = !mainMenuOpen && !garageOpen && !playerWrecked && !raceFinished && player != null;
        float speedRatio = driving ? Mathf.Clamp01(player.SpeedKph / 180f) : 0f;
        float engineTarget = driving ? Mathf.Lerp(0.12f, 0.38f, speedRatio) : 0f;
        if (driving && player.IsNitroActive)
        {
            engineTarget = Mathf.Min(0.5f, engineTarget + 0.11f);
        }

        engineLoopSource.volume = Mathf.MoveTowards(engineLoopSource.volume, engineTarget, delta * 1.4f);
        float targetPitch = Mathf.Lerp(0.62f, 1.72f, speedRatio) + (driving && player.IsNitroActive ? 0.16f : 0f);
        engineLoopSource.pitch = Mathf.Lerp(engineLoopSource.pitch, targetPitch, delta * 5f);

        float weatherTarget = driving && weatherEnabled ? GetWeatherAudioVolume() : 0f;
        weatherLoopSource.volume = Mathf.MoveTowards(weatherLoopSource.volume, weatherTarget, delta * 0.42f);
        weatherLoopSource.pitch = ActiveWeather == CircuitWeather.Blizzard || ActiveWeather == CircuitWeather.Sandstorm
            ? 0.72f
            : ActiveWeather == CircuitWeather.Rain || ActiveWeather == CircuitWeather.Thunderstorm ? 1.28f : 0.9f;

        UpdateCountdownAudio();
        UpdateFinishAudio();
    }

    private float GetWeatherAudioVolume()
    {
        switch (ActiveWeather)
        {
            case CircuitWeather.Thunderstorm: return 0.2f;
            case CircuitWeather.Rain: return 0.14f;
            case CircuitWeather.Blizzard: return 0.16f;
            case CircuitWeather.Sandstorm: return 0.13f;
            case CircuitWeather.Snow: return 0.075f;
            default: return 0.09f;
        }
    }

    private void UpdateCountdownAudio()
    {
        if (mainMenuOpen || garageOpen || raceFinished)
        {
            raceStartedForAudio = raceStarted;
            return;
        }

        if (!raceStarted)
        {
            int cue = Mathf.CeilToInt(countdown);
            if (cue >= 1 && cue <= 3 && cue != lastCountdownCue)
            {
                lastCountdownCue = cue;
                raceSfxSource.pitch = Mathf.Lerp(0.92f, 1.08f, (3 - cue) / 2f);
                raceSfxSource.PlayOneShot(countdownClip, 0.72f);
            }
        }
        else if (!raceStartedForAudio)
        {
            raceSfxSource.pitch = 1f;
            raceSfxSource.PlayOneShot(raceGoClip, 0.9f);
        }

        raceStartedForAudio = raceStarted;
    }

    private void UpdateFinishAudio()
    {
        if (raceFinished && !raceFinishedForAudio)
        {
            raceSfxSource.pitch = 1f;
            raceSfxSource.PlayOneShot(playerWrecked ? wreckClip : finishClip, playerWrecked ? 0.95f : 0.84f);
        }
        raceFinishedForAudio = raceFinished;
    }

    private void ResetExtendedRaceAudio()
    {
        lastCountdownCue = 4;
        raceStartedForAudio = false;
        raceFinishedForAudio = false;
    }

    public void RegisterMenuHover(int controlId, bool hovered)
    {
        if (!hovered)
        {
            if (activeMenuHoverId == controlId) activeMenuHoverId = -1;
            return;
        }

        if (activeMenuHoverId == controlId || Time.unscaledTime < nextMenuHoverSoundAt)
        {
            return;
        }

        activeMenuHoverId = controlId;
        nextMenuHoverSoundAt = Time.unscaledTime + 0.045f;
        if (uiSfxSource != null && menuHoverClip != null)
        {
            uiSfxSource.pitch = 0.96f + Mathf.Repeat(controlId * 0.013f, 0.12f);
            uiSfxSource.PlayOneShot(menuHoverClip, 0.5f);
        }
    }

    public void PlayMenuClickSfx()
    {
        if (uiSfxSource == null || menuClickClip == null) return;
        uiSfxSource.pitch = 1f;
        uiSfxSource.PlayOneShot(menuClickClip, 0.74f);
    }

    private void PlayMenuStartSfx()
    {
        if (uiSfxSource == null || menuStartClip == null) return;
        uiSfxSource.pitch = 1f;
        uiSfxSource.PlayOneShot(menuStartClip, 0.9f);
    }

    public void PlayPickupSfx(bool repair)
    {
        AudioClip clip = repair ? repairClip : pickupClip;
        if (raceSfxSource == null || clip == null) return;
        raceSfxSource.pitch = 1f;
        raceSfxSource.PlayOneShot(clip, 0.82f);
    }

    public void PlayWeaponSfx(CarWeaponType weaponType)
    {
        if (raceSfxSource == null) return;
        raceSfxSource.pitch = 1f;
        AudioClip clip;
        switch (weaponType)
        {
            case CarWeaponType.PlasmaBlaster:
                clip = plasmaClip;
                break;
            case CarWeaponType.EchoArc:
                clip = echoArcClip;
                break;
            case CarWeaponType.OrbitMine:
                clip = orbitMineClip;
                break;
            case CarWeaponType.IcarLance:
                clip = icarLanceClip;
                break;
            case CarWeaponType.PhantomSwarm:
                clip = phantomSwarmClip;
                break;
            default:
                clip = rocketClip;
                break;
        }
        raceSfxSource.PlayOneShot(clip, 0.86f);
    }

    public void PlayPuddleSfx(float strength)
    {
        if (raceSfxSource == null || puddleClip == null) return;
        raceSfxSource.pitch = Mathf.Lerp(0.82f, 1.08f, Mathf.Clamp01(strength));
        raceSfxSource.PlayOneShot(puddleClip, Mathf.Lerp(0.48f, 0.8f, Mathf.Clamp01(strength)));
    }
}
