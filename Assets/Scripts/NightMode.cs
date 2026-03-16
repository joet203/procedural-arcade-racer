using UnityEngine;

public class NightMode : MonoBehaviour
{
    public static NightMode Instance { get; private set; }

    public bool isNight = true;

    private Light sunLight;
    private Light[] trackLights;
    private GameObject starField;
    private GameObject moon;
    private Material[] emissiveMaterials;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (isNight)
        {
            SetupNightMode();
        }
    }

    void SetupNightMode()
    {
        // Modify sun to moon
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional && light.gameObject.name == "Sun")
            {
                sunLight = light;
                // Twilight sun - golden hour fading
                light.color = new Color(0.7f, 0.55f, 0.45f);
                light.intensity = 0.9f;
                light.transform.rotation = Quaternion.Euler(8, -30, 0);
                break;
            }
        }

        // Twilight ambient - brighter golden hour
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.35f, 0.28f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.35f, 0.32f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.15f, 0.12f);

        // Twilight fog - warm purple/orange haze
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.25f, 0.18f, 0.22f);
        RenderSettings.fogDensity = 0.0015f;

        // Twilight sky - warm sunset gradient
        Camera.main.backgroundColor = new Color(0.2f, 0.12f, 0.18f);

        CreateStarField();
        CreateMoon();
        CreateTrackLights();
        SetupCarLights();
        CreateNeonAccents();
    }

    void CreateStarField()
    {
        starField = new GameObject("StarField");

        // Fewer stars visible in twilight
        int starCount = 80;
        Material starMat = new Material(Shader.Find("Standard"));
        starMat.color = Color.white;
        starMat.SetColor("_EmissionColor", Color.white * 0.5f);
        starMat.EnableKeyword("_EMISSION");

        for (int i = 0; i < starCount; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.transform.SetParent(starField.transform);

            // Random position on sky dome
            float theta = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float phi = Random.Range(15f, 85f) * Mathf.Deg2Rad;
            float radius = 450f;

            star.transform.position = new Vector3(
                Mathf.Cos(theta) * Mathf.Sin(phi) * radius,
                Mathf.Cos(phi) * radius,
                Mathf.Sin(theta) * Mathf.Sin(phi) * radius
            );

            float size = Random.Range(0.3f, 1.2f);
            star.transform.localScale = Vector3.one * size;

            // Vary brightness
            Material mat = new Material(starMat);
            float brightness = Random.Range(0.3f, 0.8f);
            mat.SetColor("_EmissionColor", Color.white * brightness);
            star.GetComponent<Renderer>().material = mat;

            Destroy(star.GetComponent<Collider>());
        }
    }

    void CreateMoon()
    {
        moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moon.name = "Moon";
        moon.transform.position = new Vector3(-200, 300, 150);
        moon.transform.localScale = Vector3.one * 40f;
        Destroy(moon.GetComponent<Collider>());

        Material moonMat = new Material(Shader.Find("Standard"));
        moonMat.color = new Color(0.95f, 0.95f, 0.9f);
        moonMat.SetColor("_EmissionColor", new Color(0.8f, 0.82f, 0.9f) * 0.6f);
        moonMat.EnableKeyword("_EMISSION");
        moonMat.SetFloat("_Glossiness", 0.1f);
        moon.GetComponent<Renderer>().material = moonMat;

        // Moon glow
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = "MoonGlow";
        glow.transform.position = moon.transform.position;
        glow.transform.localScale = Vector3.one * 80f;
        Destroy(glow.GetComponent<Collider>());

        Material glowMat = new Material(Shader.Find("Standard"));
        glowMat.color = new Color(0.6f, 0.65f, 0.8f, 0.1f);
        glowMat.SetFloat("_Mode", 3);
        glowMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        glowMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        glowMat.SetInt("_ZWrite", 0);
        glowMat.EnableKeyword("_ALPHABLEND_ON");
        glowMat.renderQueue = 3000;
        glowMat.SetColor("_EmissionColor", new Color(0.4f, 0.45f, 0.6f) * 0.15f);
        glowMat.EnableKeyword("_EMISSION");
        glow.GetComponent<Renderer>().material = glowMat;
    }

    void CreateTrackLights()
    {
        // Find existing light poles and add actual lights
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (var obj in allObjects)
        {
            if (obj.name == "EngineLowAudio") continue; // Skip audio objects

            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null) continue;

            // Make light panels actually emit light
            if (obj.transform.localScale.y < 0.3f && obj.transform.position.y > 14f)
            {
                // This is likely a light panel
                Material mat = renderer.material;
                mat.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f) * 0.8f);
                mat.EnableKeyword("_EMISSION");

                // Add point light - softer for twilight
                GameObject lightObj = new GameObject("TrackLight");
                lightObj.transform.position = obj.transform.position - Vector3.up * 0.5f;
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Spot;
                light.color = new Color(1f, 0.9f, 0.75f);
                light.intensity = 4f;
                light.range = 45f;
                light.spotAngle = 100f;
                light.transform.rotation = Quaternion.Euler(90, 0, 0);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.7f;
            }
        }
    }

    void SetupCarLights()
    {
        CarController car = FindFirstObjectByType<CarController>();
        if (car == null) return;

        // Front headlights
        CreateHeadlight(car.transform, new Vector3(-0.6f, 0.35f, 2.15f), true);
        CreateHeadlight(car.transform, new Vector3(0.6f, 0.35f, 2.15f), true);

        // Tail lights disabled - GameSetup handles taillight geometry with Unlit shader
    }

    void CreateHeadlight(Transform parent, Vector3 localPos, bool isHeadlight)
    {
        GameObject lightObj = new GameObject("Headlight");
        lightObj.transform.SetParent(parent);
        lightObj.transform.localPosition = localPos;
        lightObj.transform.localRotation = Quaternion.identity;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(1f, 0.95f, 0.85f);
        light.intensity = 0.8f;  // Reduced from 6 to prevent white blowout
        light.range = 80f;       // Reduced range
        light.spotAngle = 45f;
        light.innerSpotAngle = 25f;
        light.shadows = LightShadows.Hard;
        light.shadowStrength = 0.6f;

        // Beam cone visualization
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.name = "HeadlightBeam";
        beam.transform.SetParent(lightObj.transform);
        beam.transform.localPosition = new Vector3(0, 0, 8f);
        beam.transform.localScale = new Vector3(0.8f, 0.4f, 15f);
        Destroy(beam.GetComponent<Collider>());

        Material beamMat = new Material(Shader.Find("Standard"));
        beamMat.color = new Color(1f, 1f, 0.9f, 0.01f);  // More transparent beam
        beamMat.SetFloat("_Mode", 3);
        beamMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        beamMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        beamMat.SetInt("_ZWrite", 0);
        beamMat.EnableKeyword("_ALPHABLEND_ON");
        beamMat.renderQueue = 3100;
        beam.GetComponent<Renderer>().material = beamMat;
    }

    void CreateCarLight(Transform parent, Vector3 localPos, Color color, float intensity, float range)
    {
        // Just add point light for glow - GameSetup already creates taillight geometry
        GameObject lightObj = new GameObject("TailLight");
        lightObj.transform.SetParent(parent);
        lightObj.transform.localPosition = localPos;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.renderMode = LightRenderMode.ForcePixel;
        light.shadows = LightShadows.None;
    }

    void CreateNeonAccents()
    {
        // Add neon strips to barriers and track edges
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (var obj in allObjects)
        {
            if (obj.name == "Barrier")
            {
                // Find the orange stripe child and make it glow
                foreach (Transform child in obj.transform)
                {
                    Renderer r = child.GetComponent<Renderer>();
                    if (r != null)
                    {
                        Material mat = r.material;
                        Color c = mat.color;
                        if (c.r > 0.8f && c.g < 0.6f) // Orange-ish
                        {
                            mat.SetColor("_EmissionColor", c * 0.5f);
                            mat.EnableKeyword("_EMISSION");
                        }
                    }
                }
            }

            // Make curbs glow
            if (obj.name == "CornerRoad" || obj.name.Contains("Curb"))
            {
                Renderer r = obj.GetComponent<Renderer>();
                if (r != null)
                {
                    Color c = r.material.color;
                    if (c.r > 0.8f) // Orange curbs
                    {
                        r.material.SetColor("_EmissionColor", c * 0.3f);
                        r.material.EnableKeyword("_EMISSION");
                    }
                }
            }
        }

        // Add ground-level neon strips along track edges - subtle for twilight
        CreateNeonStrip(new Vector3(11.5f, 0.05f, 0), new Vector3(0.12f, 0.05f, 480f), new Color(0f, 0.5f, 0.7f));
        CreateNeonStrip(new Vector3(-11.5f, 0.05f, 0), new Vector3(0.12f, 0.05f, 480f), new Color(0.7f, 0.2f, 0.5f));
    }

    void CreateNeonStrip(Vector3 pos, Vector3 scale, Color color)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "NeonStrip";
        strip.transform.position = pos;
        strip.transform.localScale = scale;
        Destroy(strip.GetComponent<Collider>());

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetColor("_EmissionColor", color * 0.4f);
        mat.EnableKeyword("_EMISSION");
        strip.GetComponent<Renderer>().material = mat;

        // Add subtle light - dimmer for twilight
        GameObject lightObj = new GameObject("NeonLight");
        lightObj.transform.position = pos + Vector3.up * 0.5f;
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 0.3f;
        light.range = 4f;
    }

    void Update()
    {
        // Toggle night mode with N key
        if (Input.GetKeyDown(KeyCode.N))
        {
            // Reload scene to toggle (simplified)
        }
    }
}
