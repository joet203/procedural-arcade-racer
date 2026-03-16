using UnityEngine;

public enum PowerUpType
{
    Ammo,
    SpeedBoost,
    Shield
}

public class PowerUp : MonoBehaviour
{
    public PowerUpType type;
    public float respawnTime = 15f;

    private bool isCollected = false;
    private float respawnTimer = 0f;
    private Renderer[] renderers;
    private Collider col;
    private Light glowLight;

    // Colors per type
    private static readonly Color[] typeColors = {
        new Color(1f, 0.7f, 0.2f),    // Ammo - orange
        new Color(0.2f, 0.8f, 1f),    // Speed - cyan
        new Color(0.5f, 1f, 0.5f)     // Shield - green
    };

    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
        col = GetComponent<Collider>();

        // Add glow light
        GameObject lightObj = new GameObject("Glow");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;
        glowLight = lightObj.AddComponent<Light>();
        glowLight.type = LightType.Point;
        glowLight.color = typeColors[(int)type];
        glowLight.intensity = 2f;
        glowLight.range = 8f;

        ApplyColor();
    }

    void ApplyColor()
    {
        Color c = typeColors[(int)type];
        foreach (var r in renderers)
        {
            if (r != null && r.material != null)
            {
                r.material.color = c;
                r.material.SetColor("_EmissionColor", c * 2f);
                r.material.EnableKeyword("_EMISSION");
            }
        }
    }

    void Update()
    {
        // Bob and rotate
        if (!isCollected)
        {
            transform.position = new Vector3(
                transform.position.x,
                1.5f + Mathf.Sin(Time.time * 2f) * 0.3f,
                transform.position.z
            );
            transform.Rotate(0, 90f * Time.deltaTime, 0);
        }

        // Respawn timer
        if (isCollected)
        {
            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0)
            {
                Respawn();
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        CarController car = other.GetComponent<CarController>();
        if (car != null)
        {
            Collect(car);
        }
    }

    void Collect(CarController car)
    {
        isCollected = true;
        respawnTimer = respawnTime;

        // Hide
        foreach (var r in renderers)
            if (r != null) r.enabled = false;
        if (col != null) col.enabled = false;
        if (glowLight != null) glowLight.enabled = false;

        // Apply effect
        switch (type)
        {
            case PowerUpType.Ammo:
                CarTurret turret = car.GetComponent<CarTurret>();
                if (turret != null)
                {
                    // Refill ammo via reflection or public method
                    var field = typeof(CarTurret).GetField("currentAmmo",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null) field.SetValue(turret, turret.MaxAmmo);
                }
                ShowPickupText("AMMO REFILLED!");
                break;

            case PowerUpType.SpeedBoost:
                // Temporary speed boost
                car.StartCoroutine(SpeedBoostRoutine(car));
                ShowPickupText("SPEED BOOST!");
                break;

            case PowerUpType.Shield:
                // Grant shield to player
                PlayerHealth health = car.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.AddShield(5f);
                }
                ShowPickupText("SHIELD ACTIVE!");
                break;
        }

        PlayPickupSound();
    }

    System.Collections.IEnumerator SpeedBoostRoutine(CarController car)
    {
        float originalMax = car.maxSpeed;
        float originalAccel = car.acceleration;
        car.maxSpeed *= 1.4f;
        car.acceleration *= 1.3f;

        yield return new WaitForSeconds(5f);

        car.maxSpeed = originalMax;
        car.acceleration = originalAccel;
    }

    void Respawn()
    {
        isCollected = false;
        foreach (var r in renderers)
            if (r != null) r.enabled = true;
        if (col != null) col.enabled = true;
        if (glowLight != null) glowLight.enabled = true;
    }

    void ShowPickupText(string text)
    {
        // Create floating text
        GameObject textObj = new GameObject("PickupText");
        textObj.transform.position = transform.position + Vector3.up * 2f;
        FloatingText ft = textObj.AddComponent<FloatingText>();
        ft.text = text;
        ft.color = typeColors[(int)type];
    }

    void PlayPickupSound()
    {
        GameObject audioObj = new GameObject("PickupSound");
        audioObj.transform.position = transform.position;
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        int sampleRate = 44100;
        float duration = 0.2f;
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Pickup", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            // Softer, lower pitched pickup sound - gentle "boop"
            float freq = 180f + t * 120f; // Much lower: 180-300Hz instead of 400-1000Hz
            float envelope = Mathf.Sin(t * Mathf.PI) * 0.3f; // Smooth bell curve, quieter
            samples[i] = Mathf.Sin(t * freq * Mathf.PI * 2f) * envelope;
        }

        clip.SetData(samples, 0);
        audio.clip = clip;
        audio.volume = 0.25f; // Quieter
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }

    public static GameObject Create(PowerUpType type, Vector3 position)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"PowerUp_{type}";
        obj.transform.position = position;
        obj.transform.localScale = Vector3.one * 1.2f;
        obj.transform.rotation = Quaternion.Euler(45, 0, 45);

        // Make trigger
        Collider col = obj.GetComponent<Collider>();
        col.isTrigger = true;

        // Add larger trigger collider
        SphereCollider trigger = obj.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = 2f;

        PowerUp pu = obj.AddComponent<PowerUp>();
        pu.type = type;

        return obj;
    }
}

public class FloatingText : MonoBehaviour
{
    public string text;
    public Color color = Color.white;
    private float lifetime = 1.5f;
    private float timer = 0;

    void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * Time.deltaTime * 2f;

        if (timer >= lifetime)
            Destroy(gameObject);
    }

    void OnGUI()
    {
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
        if (screenPos.z > 0)
        {
            float alpha = 1f - (timer / lifetime);
            GUIStyle style = new GUIStyle
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.textColor = new Color(color.r, color.g, color.b, alpha);

            float x = screenPos.x - 100;
            float y = Screen.height - screenPos.y - 15;
            GUI.Label(new Rect(x, y, 200, 30), text, style);
        }
    }
}
