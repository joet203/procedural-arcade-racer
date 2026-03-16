using UnityEngine;

public class CarAudio : MonoBehaviour
{
    private CarController car;

    // Audio sources - simplified, bass-focused
    private AudioSource engineSource;
    private AudioSource exhaustSource;
    private AudioSource tireSource;
    private AudioSource windSource;
    private AudioSource oneShotSource;
    private AudioSource turboSource;

    // Engine state
    private float currentRPM;
    private float targetRPM;
    private float rpmVelocity;
    private float throttle;
    private float lastThrottle;

    // Volume smoothing
    private float engineVol, exhaustVol, tireVol, windVol, turboVol;
    private float engineVolVel, exhaustVolVel, tireVolVel, windVolVel, turboVolVel;

    [Header("Master")]
    public float masterVolume = 0.6f;

    [Header("Engine")]
    public float idleRPM = 800f;
    public float maxRPM = 6500f;
    public float engineMaxVol = 0.7f;

    [Header("Exhaust")]
    public float exhaustMaxVol = 0.45f;

    [Header("Tires")]
    public float tireMaxVol = 0.02f; // Almost silent

    [Header("Wind")]
    public float windMaxVol = 0.01f; // Almost silent

    [Header("Turbo/Boost")]
    public float turboMaxVol = 0.55f;

    private int sampleRate;
    private System.Random rand;

    void Start()
    {
        car = GetComponent<CarController>();
        sampleRate = AudioSettings.outputSampleRate;
        rand = new System.Random();
        currentRPM = idleRPM;

        CreateAudioSources();
    }

    void CreateAudioSources()
    {
        // Main engine - deep V8 rumble
        engineSource = CreateSource("Engine", true, 0.3f);
        engineSource.clip = CreateEngineClip();
        engineSource.Play();

        // Exhaust - throaty low burble
        exhaustSource = CreateSource("Exhaust", true, 0.3f);
        exhaustSource.clip = CreateExhaustClip();
        exhaustSource.Play();

        // Tires - low rumble/grumble (not screech)
        tireSource = CreateSource("Tires", true, 0.25f);
        tireSource.clip = CreateTireRumbleClip();
        tireSource.Play();

        // Wind - soft whoosh
        windSource = CreateSource("Wind", true, 0f);
        windSource.clip = CreateWindClip();
        windSource.Play();

        // Turbo - deep whoosh while boosting
        turboSource = CreateSource("Turbo", true, 0.3f);
        turboSource.clip = CreateTurboClip();
        turboSource.Play();

        // One-shots
        oneShotSource = CreateSource("OneShot", false, 0.4f);
    }

    AudioSource CreateSource(string name, bool loop, float spatialBlend)
    {
        GameObject obj = new GameObject(name + "Audio");
        obj.transform.SetParent(transform);
        AudioSource src = obj.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = spatialBlend;
        src.volume = 0f;
        src.dopplerLevel = 0f;
        return src;
    }

    // Deep V8 engine - bass focused, clean idle
    AudioClip CreateEngineClip()
    {
        int samples = sampleRate * 2;
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            // Very low fundamentals (V8 firing at ~30-60Hz)
            float baseFreq = 40f;

            // Primary bass rumble - clean sine waves only
            float sample = Mathf.Sin(t * baseFreq * 2f * Mathf.PI) * 0.5f;
            sample += Mathf.Sin(t * baseFreq * 2f * 2f * Mathf.PI) * 0.3f; // 80Hz
            sample += Mathf.Sin(t * baseFreq * 3f * 2f * Mathf.PI) * 0.12f; // 120Hz

            // Sub-bass thump
            sample += Mathf.Sin(t * 25f * 2f * Mathf.PI) * 0.18f;

            // Gentle variation for organic feel (slower, subtler)
            float variation = Mathf.Sin(t * 3.5f * 2f * Mathf.PI) * 0.05f;
            sample *= (1f + variation);

            // NO noise/grit - keep it clean for idle

            data[i] = sample * 0.5f;
        }

        AudioClip clip = AudioClip.Create("Engine", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Exhaust - low throaty burble, clean
    AudioClip CreateExhaustClip()
    {
        int samples = sampleRate * 2;
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            // Low pulse (exhaust note) - clean tones only
            float pulse = Mathf.Sin(t * 35f * 2f * Mathf.PI) * 0.4f;
            pulse += Mathf.Sin(t * 70f * 2f * Mathf.PI) * 0.22f;
            pulse += Mathf.Sin(t * 52f * 2f * Mathf.PI) * 0.15f; // Slight detuning for richness

            // Shape for more aggressive attack
            pulse = Mathf.Pow(Mathf.Abs(pulse), 0.8f) * Mathf.Sign(pulse);

            // Subtle low-frequency modulation instead of noise
            float mod = Mathf.Sin(t * 4.2f * 2f * Mathf.PI) * 0.08f;
            float sample = pulse * (1f + mod);

            data[i] = sample * 0.45f;
        }

        AudioClip clip = AudioClip.Create("Exhaust", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Tire rumble - low frequency grumble, NOT screech
    AudioClip CreateTireRumbleClip()
    {
        int samples = sampleRate * 2;
        float[] data = new float[samples];

        float[] lpBuffer = new float[16];
        int lpIdx = 0;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            // Low rumble frequencies (rubber on road)
            float rumble = Mathf.Sin(t * 60f * 2f * Mathf.PI) * 0.3f;
            rumble += Mathf.Sin(t * 120f * 2f * Mathf.PI) * 0.2f;
            rumble += Mathf.Sin(t * 180f * 2f * Mathf.PI) * 0.1f;

            // Filtered noise for texture
            float noise = (float)rand.NextDouble() * 2f - 1f;
            lpBuffer[lpIdx] = noise;
            lpIdx = (lpIdx + 1) % lpBuffer.Length;

            float filtered = 0f;
            for (int j = 0; j < lpBuffer.Length; j++)
                filtered += lpBuffer[j];
            filtered /= lpBuffer.Length;

            float sample = rumble + filtered * 0.2f;

            data[i] = sample * 0.5f;
        }

        AudioClip clip = AudioClip.Create("TireRumble", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Wind - very soft, muffled whoosh
    AudioClip CreateWindClip()
    {
        int samples = sampleRate * 3;
        float[] data = new float[samples];

        float[] buffer = new float[80]; // Very heavy smoothing for muffled sound
        int bufIdx = 0;

        for (int i = 0; i < samples; i++)
        {
            float noise = (float)rand.NextDouble() * 2f - 1f;

            buffer[bufIdx] = noise;
            bufIdx = (bufIdx + 1) % buffer.Length;

            float sum = 0f;
            for (int j = 0; j < buffer.Length; j++)
                sum += buffer[j];

            float sample = sum / buffer.Length;

            // Very low frequency base tone
            float t = (float)i / sampleRate;
            float bassTone = Mathf.Sin(t * 30f * 2f * Mathf.PI) * 0.15f;

            sample = sample * 0.3f + bassTone;

            data[i] = sample * 0.4f;
        }

        AudioClip clip = AudioClip.Create("Wind", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void Update()
    {
        if (car == null) return;

        UpdateRPM();
        UpdateEngineSound();
        UpdateExhaustSound();
        UpdateTireSound();
        UpdateWindSound();
        UpdateTurboSound();

        lastThrottle = throttle;
    }

    void UpdateRPM()
    {
        throttle = Mathf.Max(0, Input.GetAxis("Vertical"));

        float speedRPM = Mathf.Lerp(idleRPM, maxRPM, car.SpeedPercent);
        float throttleBonus = throttle * (maxRPM - speedRPM) * 0.25f;

        if (car.IsBoosting)
            throttleBonus += (maxRPM - speedRPM) * 0.15f;

        targetRPM = speedRPM + throttleBonus;
        targetRPM = Mathf.Clamp(targetRPM, idleRPM, maxRPM);

        float smoothTime = throttle > lastThrottle ? 0.12f : 0.2f;
        currentRPM = Mathf.SmoothDamp(currentRPM, targetRPM, ref rpmVelocity, smoothTime);
    }

    void UpdateEngineSound()
    {
        float rpmNorm = (currentRPM - idleRPM) / (maxRPM - idleRPM);

        // Pitch based on RPM - keep it in pleasant range
        float basePitch = 0.5f + rpmNorm * 1.4f;
        engineSource.pitch = basePitch;

        // Volume increases with throttle and RPM
        float targetVol = engineMaxVol * masterVolume;
        targetVol *= (0.4f + throttle * 0.4f + rpmNorm * 0.2f);

        if (car.IsBoosting)
            targetVol *= 1.15f;

        engineVol = Mathf.SmoothDamp(engineVol, targetVol, ref engineVolVel, 0.06f);
        engineSource.volume = engineVol;
    }

    void UpdateExhaustSound()
    {
        float rpmNorm = (currentRPM - idleRPM) / (maxRPM - idleRPM);

        exhaustSource.pitch = 0.6f + rpmNorm * 1.0f;

        float targetVol = exhaustMaxVol * masterVolume;
        targetVol *= (0.25f + throttle * 0.5f + rpmNorm * 0.25f);

        if (car.IsBoosting)
            targetVol *= 1.25f;

        exhaustVol = Mathf.SmoothDamp(exhaustVol, targetVol, ref exhaustVolVel, 0.08f);
        exhaustSource.volume = exhaustVol;
    }

    void UpdateTireSound()
    {
        float targetVol = 0f;

        if (car.IsDrifting && car.IsGrounded)
        {
            float intensity = car.SpeedPercent * (0.5f + Mathf.Abs(car.SteerInput) * 0.5f);
            targetVol = tireMaxVol * masterVolume * intensity;
            tireSource.pitch = 0.7f + car.SpeedPercent * 0.4f;
        }

        tireVol = Mathf.SmoothDamp(tireVol, targetVol, ref tireVolVel, 0.1f);
        tireSource.volume = tireVol;
    }

    void UpdateWindSound()
    {
        float targetVol = 0f;

        if (car.SpeedPercent > 0.25f)
        {
            targetVol = windMaxVol * masterVolume * Mathf.Pow((car.SpeedPercent - 0.25f) / 0.75f, 1.3f);
        }

        if (car.IsBoosting)
            targetVol *= 1.2f;

        windSource.pitch = 0.8f + car.SpeedPercent * 0.2f;

        windVol = Mathf.SmoothDamp(windVol, targetVol, ref windVolVel, 0.15f);
        windSource.volume = windVol;
    }

    void UpdateTurboSound()
    {
        float targetVol = 0f;

        if (car.IsBoosting)
        {
            // Full turbo volume while boosting, scaled by speed for intensity
            targetVol = turboMaxVol * masterVolume * (0.7f + car.SpeedPercent * 0.3f);

            // Higher pitch as speed increases - turbo spooling up
            float rpmNorm = (currentRPM - idleRPM) / (maxRPM - idleRPM);
            turboSource.pitch = 1.0f + rpmNorm * 0.6f + car.SpeedPercent * 0.4f;
        }
        else
        {
            // Turbo spin-down when boost ends - gradual pitch drop
            turboSource.pitch = Mathf.Max(0.6f, turboSource.pitch * 0.98f);
        }

        // Smooth volume transitions - quick attack, slower release
        float smoothTime = car.IsBoosting ? 0.05f : 0.2f;
        turboVol = Mathf.SmoothDamp(turboVol, targetVol, ref turboVolVel, smoothTime);
        turboSource.volume = turboVol;
    }

    // Turbo whoosh - deep, powerful turbo sound (not high-pitched whine)
    AudioClip CreateTurboClip()
    {
        int samples = sampleRate * 2;
        float[] data = new float[samples];

        // Low-pass filter buffer for smoothing noise
        float[] lpBuffer = new float[32];
        int lpIdx = 0;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            // Deep turbo whoosh - low base frequency for powerful sound
            float baseFreq = 160f;

            // Primary turbo tone - emphasize lower frequencies
            float sample = Mathf.Sin(t * baseFreq * 2f * Mathf.PI) * 0.4f;        // 160Hz - main tone
            sample += Mathf.Sin(t * baseFreq * 0.5f * 2f * Mathf.PI) * 0.35f;     // 80Hz - sub-bass
            sample += Mathf.Sin(t * baseFreq * 1.5f * 2f * Mathf.PI) * 0.2f;      // 240Hz - warmth
            sample += Mathf.Sin(t * baseFreq * 2f * 2f * Mathf.PI) * 0.08f;       // 320Hz - minimal upper harmonic

            // Deep sub-bass foundation for power feel
            sample += Mathf.Sin(t * 50f * 2f * Mathf.PI) * 0.25f;
            sample += Mathf.Sin(t * 100f * 2f * Mathf.PI) * 0.2f;

            // Slow wobble/pulsation for organic turbo spool feel
            float wobble = 1f + Mathf.Sin(t * 5f * 2f * Mathf.PI) * 0.1f;
            sample *= wobble;

            // Filtered noise for soft air rush (low-passed for whoosh, not hiss)
            float noise = (float)rand.NextDouble() * 2f - 1f;
            lpBuffer[lpIdx] = noise;
            lpIdx = (lpIdx + 1) % lpBuffer.Length;

            float filtered = 0f;
            for (int j = 0; j < lpBuffer.Length; j++)
                filtered += lpBuffer[j];
            filtered /= lpBuffer.Length;

            sample += filtered * 0.15f;

            data[i] = sample * 0.45f;
        }

        AudioClip clip = AudioClip.Create("Turbo", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // === Public Methods ===

    public void OnCollision(float impactForce)
    {
        float intensity = Mathf.Clamp01(impactForce / 20f);
        if (intensity < 0.25f) return;

        AudioClip clip = CreateImpactClip(intensity);
        oneShotSource.PlayOneShot(clip, intensity * 0.5f * masterVolume);
    }

    AudioClip CreateImpactClip(float intensity)
    {
        int samples = (int)(sampleRate * 0.25f);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float env = Mathf.Exp(-t * 10f);

            // Deep thump - low frequencies only
            float thump = Mathf.Sin(t * sampleRate * 40f / sampleRate * 2f * Mathf.PI) * env * 0.5f;
            thump += Mathf.Sin(t * sampleRate * 70f / sampleRate * 2f * Mathf.PI) * env * 0.3f;
            thump += Mathf.Sin(t * sampleRate * 100f / sampleRate * 2f * Mathf.PI) * env * 0.2f;

            // Subtle crunch (filtered noise)
            float crunch = ((float)rand.NextDouble() - 0.5f) * env * 0.15f;

            data[i] = (thump + crunch) * intensity;
        }

        AudioClip clip = AudioClip.Create("Impact", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlayBoostStart()
    {
        AudioClip clip = CreateBoostClip();
        oneShotSource.PlayOneShot(clip, 0.4f * masterVolume);
    }

    AudioClip CreateBoostClip()
    {
        int samples = (int)(sampleRate * 0.5f);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;

            // Big bass punch at start
            float punch = Mathf.Sin(t * 45f * 2f * Mathf.PI) * Mathf.Exp(-t * 5f) * 0.6f;
            punch += Mathf.Sin(t * 90f * 2f * Mathf.PI) * Mathf.Exp(-t * 6f) * 0.35f;

            // Rising low whoosh that builds
            float freq = 40f + t * t * 200f;
            float whoosh = Mathf.Sin(t * freq * 2f * Mathf.PI) * Mathf.Sin(t * Mathf.PI) * 0.4f;

            // Sustained low rumble
            float rumble = Mathf.Sin(t * 55f * 2f * Mathf.PI) * (1f - t * 0.5f) * 0.3f;

            // Subtle air rush (heavily filtered)
            float noise = ((float)rand.NextDouble() - 0.5f) * Mathf.Sin(t * Mathf.PI) * 0.1f;

            data[i] = punch + whoosh + rumble + noise;
        }

        AudioClip clip = AudioClip.Create("Boost", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlayLapComplete()
    {
        AudioClip clip = CreateLapClip();
        oneShotSource.PlayOneShot(clip, 0.35f * masterVolume);
    }

    AudioClip CreateLapClip()
    {
        int samples = (int)(sampleRate * 0.45f);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            // Pleasant low-mid tones (C4, E4, G4 - major chord but lower)
            float env1 = Mathf.Exp(-t * 5f);
            float note1 = Mathf.Sin(t * 262f * 2f * Mathf.PI) * env1 * 0.35f; // C4

            float env2 = t > 0.08f ? Mathf.Exp(-(t - 0.08f) * 4.5f) : 0f;
            float note2 = Mathf.Sin(t * 330f * 2f * Mathf.PI) * env2 * 0.3f; // E4

            float env3 = t > 0.16f ? Mathf.Exp(-(t - 0.16f) * 4f) : 0f;
            float note3 = Mathf.Sin(t * 392f * 2f * Mathf.PI) * env3 * 0.3f; // G4

            data[i] = note1 + note2 + note3;
        }

        AudioClip clip = AudioClip.Create("Lap", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public void PlayCheckpoint()
    {
        AudioClip clip = CreateCheckpointClip();
        oneShotSource.PlayOneShot(clip, 0.25f * masterVolume);
    }

    AudioClip CreateCheckpointClip()
    {
        int samples = (int)(sampleRate * 0.1f);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 12f);

            // Simple pleasant tone (G4 - not too high)
            float tone = Mathf.Sin(t * 392f * 2f * Mathf.PI) * env * 0.4f;
            tone += Mathf.Sin(t * 196f * 2f * Mathf.PI) * env * 0.25f; // G3 undertone

            data[i] = tone;
        }

        AudioClip clip = AudioClip.Create("Checkpoint", samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
