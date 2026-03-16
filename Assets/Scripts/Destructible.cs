using UnityEngine;

public class Destructible : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Effects")]
    public bool spawnDebris = true;
    public int debrisCount = 5;
    public bool spawnExplosion = true;

    [Header("Scoring")]
    public int scoreValue = 100;

    [Header("Type")]
    public bool isCopCar = false;

    private bool isDestroyed = false;
    private Renderer[] renderers;
    private Color originalColor;

    void Start()
    {
        currentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0 && renderers[0].material != null)
        {
            originalColor = renderers[0].material.color;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        // Flash red on hit
        StartCoroutine(FlashDamage());

        // Check if this will kill the target
        bool willDie = currentHealth <= 0;

        // If this is a cop car and NOT about to die, play brief siren
        if (isCopCar && !willDie)
        {
            CopCar cop = GetComponent<CopCar>();
            if (cop != null)
            {
                cop.OnTakeDamage();
            }
        }

        if (willDie)
        {
            Die();
        }
    }

    System.Collections.IEnumerator FlashDamage()
    {
        // Flash white
        foreach (var r in renderers)
        {
            if (r != null && r.material != null)
            {
                r.material.color = Color.white;
            }
        }

        yield return new WaitForSeconds(0.08f);

        // If low health, stay red/orange to show weakened state
        float healthPercent = currentHealth / maxHealth;
        Color targetColor = originalColor;

        if (healthPercent < 0.6f && healthPercent > 0)
        {
            // Tint red based on damage - more damaged = more red
            float damageIntensity = 1f - healthPercent;
            targetColor = Color.Lerp(originalColor, new Color(1f, 0.3f, 0.1f), damageIntensity);
        }

        foreach (var r in renderers)
        {
            if (r != null && r.material != null)
            {
                r.material.color = targetColor;
            }
        }
    }

    void Die()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (spawnExplosion)
        {
            CreateExplosion();
        }

        if (spawnDebris)
        {
            CreateDebris();
        }

        // Add score
        GameUI.Score += scoreValue;
        GameUI.DestroyedCount++;

        // Notify cop car challenge and background music
        if (isCopCar)
        {
            CopChallenge.OnCopDestroyed();
            BackgroundMusic.OnCopKilled();
        }

        Destroy(gameObject);
    }

    void CreateExplosion()
    {
        // Play explosion sound
        PlayExplosionSound();

        // Create explosion particle effect
        GameObject explosion = new GameObject("Explosion");
        explosion.transform.position = transform.position;

        // Fire/explosion particles
        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 8f;
        main.startSize = 1.5f;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.6f, 0.1f),
            new Color(1f, 0.2f, 0f)
        );
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 30)
        });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.8f, 0.3f), 0f),
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0, 1, 1, 2));

        // Add light flash
        GameObject lightObj = new GameObject("ExplosionLight");
        lightObj.transform.position = transform.position;
        Light light = lightObj.AddComponent<Light>();
        light.color = new Color(1f, 0.5f, 0.1f);
        light.intensity = 8f;
        light.range = 15f;

        // Fade light
        lightObj.AddComponent<FadeLight>();

        Destroy(explosion, 2f);
        Destroy(lightObj, 0.5f);
    }

    void CreateDebris()
    {
        for (int i = 0; i < debrisCount; i++)
        {
            GameObject debris = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debris.transform.position = transform.position + Random.insideUnitSphere * 0.5f;
            debris.transform.localScale = Vector3.one * Random.Range(0.2f, 0.5f);
            debris.transform.rotation = Random.rotation;

            Rigidbody rb = debris.AddComponent<Rigidbody>();
            rb.AddExplosionForce(500f, transform.position, 5f);
            rb.mass = 0.5f;

            // Random dark color
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(
                Random.Range(0.1f, 0.3f),
                Random.Range(0.1f, 0.3f),
                Random.Range(0.1f, 0.3f)
            );
            debris.GetComponent<Renderer>().material = mat;

            Destroy(debris, 3f);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Only take damage from player car collisions, not from other cops or ground
        CarController playerCar = collision.gameObject.GetComponent<CarController>();
        if (playerCar != null && collision.relativeVelocity.magnitude > 10f)
        {
            float damage = collision.relativeVelocity.magnitude * 2f;
            TakeDamage(damage);
        }
    }

    void PlayExplosionSound()
    {
        // Create temporary audio source for explosion sound
        GameObject audioObj = new GameObject("ExplosionSound");
        audioObj.transform.position = transform.position;
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        // Generate beefy procedural explosion sound
        int sampleRate = 44100;
        float duration = 1.0f; // Longer for more impact
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Explosion", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        System.Random rand = new System.Random();

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;

            // BIG initial thump (very low frequency for chest punch)
            float thump = Mathf.Sin(t * 35f * Mathf.PI * 2f) * Mathf.Exp(-t * 8f);

            // Secondary bass hit
            float bass = Mathf.Sin(t * 55f * Mathf.PI * 2f) * Mathf.Exp(-t * 12f) * 0.7f;

            // Mid punch
            float punch = Mathf.Sin(t * 90f * Mathf.PI * 2f) * Mathf.Exp(-t * 18f) * 0.5f;

            // Satisfying crunch noise
            float crunch = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 5f) * 0.6f;

            // Metal debris rattle
            float debris = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 3f) * 0.25f;

            // High sizzle
            float sizzle = ((float)rand.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 10f) * 0.15f;

            // Combine - heavy on the bass
            samples[i] = thump * 0.45f + bass * 0.25f + punch * 0.12f + crunch * 0.1f + debris * 0.05f + sizzle * 0.03f;

            // Punchy envelope - fast attack, satisfying decay
            float envelope = Mathf.Pow(Mathf.Exp(-t * 3f), 0.7f);
            samples[i] *= envelope * 1.3f; // Boost overall
        }

        clip.SetData(samples, 0);

        audio.clip = clip;
        audio.volume = 0.85f; // Louder
        audio.spatialBlend = 0.7f;
        audio.minDistance = 8f;
        audio.maxDistance = 120f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }
}

// Helper class to fade explosion light
public class FadeLight : MonoBehaviour
{
    private Light light;
    private float startIntensity;

    void Start()
    {
        light = GetComponent<Light>();
        if (light != null)
            startIntensity = light.intensity;
    }

    void Update()
    {
        if (light != null)
        {
            light.intensity = Mathf.Lerp(light.intensity, 0, Time.deltaTime * 10f);
        }
    }
}
