using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Shield")]
    public float shieldDuration = 0f;
    public bool hasShield => shieldDuration > 0;

    // Visual
    private GameObject shieldVisual;
    private float damageFlash = 0f;

    // UI
    private Texture2D healthBarBg;
    private Texture2D healthBarFill;
    private Texture2D shieldBarFill;
    private Texture2D panelBg;
    private Texture2D whiteTex;

    void Start()
    {
        currentHealth = maxHealth;
        CreateTextures();
        CreateShieldVisual();
    }

    void CreateTextures()
    {
        healthBarBg = MakeTex(new Color(0.2f, 0.2f, 0.22f, 0.9f));
        healthBarFill = MakeTex(new Color(0.2f, 0.85f, 0.3f));
        shieldBarFill = MakeTex(new Color(0.3f, 0.8f, 1f));
        panelBg = MakeTex(new Color(0.08f, 0.08f, 0.1f, 0.8f));
        whiteTex = MakeTex(Color.white);
    }

    Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    void CreateShieldVisual()
    {
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVisual.name = "ShieldBubble";
        shieldVisual.transform.SetParent(transform);
        shieldVisual.transform.localPosition = Vector3.up * 0.5f;
        shieldVisual.transform.localScale = Vector3.one * 4f;
        Destroy(shieldVisual.GetComponent<Collider>());

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.8f, 1f, 0.2f);
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        mat.SetColor("_EmissionColor", new Color(0.3f, 0.8f, 1f) * 0.5f);
        mat.EnableKeyword("_EMISSION");
        shieldVisual.GetComponent<Renderer>().material = mat;
        shieldVisual.SetActive(false);
    }

    void Update()
    {
        // Shield countdown
        if (shieldDuration > 0)
        {
            shieldDuration -= Time.deltaTime;
            shieldVisual.SetActive(true);
            // Pulse effect
            float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.1f;
            shieldVisual.transform.localScale = Vector3.one * 4f * pulse;
        }
        else
        {
            shieldVisual.SetActive(false);
        }

        // Damage flash decay
        if (damageFlash > 0)
            damageFlash -= Time.deltaTime * 3f;
    }

    public void TakeDamage(float damage)
    {
        if (hasShield)
        {
            // Shield absorbs damage
            PlayShieldHitSound();
            return;
        }

        currentHealth -= damage;
        damageFlash = 1f;

        // Screen shake
        var cam = Camera.main?.GetComponent<CameraFollow>();
        cam?.Shake(0.2f);

        PlayDamageSound();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    public void AddShield(float duration)
    {
        shieldDuration = duration;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }

    void Die()
    {
        // For now, just restart
        // Could add death animation later
        GameSetup setup = FindFirstObjectByType<GameSetup>();
        if (setup != null)
        {
            // Reset health on restart
            currentHealth = maxHealth;
        }
    }

    void PlayDamageSound()
    {
        GameObject audioObj = new GameObject("DamageSound");
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        int sampleRate = 44100;
        float duration = 0.2f;
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Damage", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float noise = Random.Range(-1f, 1f) * Mathf.Exp(-t * 10f);
            float thump = Mathf.Sin(t * 100f * Mathf.PI * 2f) * Mathf.Exp(-t * 20f);
            samples[i] = (noise * 0.5f + thump * 0.5f);
        }

        clip.SetData(samples, 0);
        audio.clip = clip;
        audio.volume = 0.4f;
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }

    void PlayShieldHitSound()
    {
        GameObject audioObj = new GameObject("ShieldHit");
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        int sampleRate = 44100;
        float duration = 0.3f;
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("ShieldHit", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            // Electric zap sound
            float zap = Mathf.Sin(t * 800f * Mathf.PI * 2f) * Mathf.Exp(-t * 15f);
            float buzz = Mathf.Sin(t * 200f * Mathf.PI * 2f) * Mathf.Exp(-t * 8f) * 0.3f;
            samples[i] = zap + buzz;
        }

        clip.SetData(samples, 0);
        audio.clip = clip;
        audio.volume = 0.4f;
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }

    void OnGUI()
    {
        DrawHealthBar();
    }

    void DrawHealthBar()
    {
        float barWidth = 160;
        float barHeight = 12;
        float x = Screen.width / 2 - barWidth / 2;
        float y = Screen.height - 50;

        // Background
        GUI.DrawTexture(new Rect(x - 5, y - 20, barWidth + 10, 35), panelBg);

        // Label
        GUIStyle labelStyle = new GUIStyle
        {
            fontSize = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        labelStyle.normal.textColor = hasShield ? new Color(0.3f, 0.8f, 1f) : new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(x, y - 18, barWidth, 16), hasShield ? "SHIELDED" : "HEALTH", labelStyle);

        // Bar background
        GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), healthBarBg);

        // Health fill - use GUI.color to tint white texture
        float healthPercent = currentHealth / maxHealth;
        Color healthColor = Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.85f, 0.3f), healthPercent);
        if (damageFlash > 0)
            healthColor = Color.Lerp(healthColor, Color.white, damageFlash);

        Color savedColor = GUI.color;
        GUI.color = healthColor;
        GUI.DrawTexture(new Rect(x, y, barWidth * healthPercent, barHeight), whiteTex);
        GUI.color = savedColor;

        // Shield overlay
        if (hasShield)
        {
            float shieldAlpha = 0.5f + Mathf.Sin(Time.time * 6f) * 0.2f;
            GUI.color = new Color(0.3f, 0.8f, 1f, shieldAlpha);
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), whiteTex);
            GUI.color = savedColor;
        }
    }
}
