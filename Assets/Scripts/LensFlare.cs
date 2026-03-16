using UnityEngine;

public class LensFlare : MonoBehaviour
{
    private Light sunLight;
    private Camera mainCam;

    private GameObject[] flareElements;
    private Material flareMat;

    [Header("Settings")]
    public float flareIntensity = 0.6f;
    public float fadeAngle = 45f;

    void Start()
    {
        mainCam = Camera.main;

        // Find the sun light
        Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional && light.gameObject.name == "Sun")
            {
                sunLight = light;
                break;
            }
        }

        if (sunLight == null) return;

        CreateFlareMaterial();
        CreateFlareElements();
    }

    void CreateFlareMaterial()
    {
        flareMat = new Material(Shader.Find("Sprites/Default"));
    }

    void CreateFlareElements()
    {
        flareElements = new GameObject[6];

        // Different sizes and colors for flare elements
        float[] sizes = { 0.3f, 0.15f, 0.08f, 0.2f, 0.12f, 0.5f };
        float[] positions = { 0.2f, 0.4f, 0.6f, 0.8f, 1.2f, 1.5f };
        Color[] colors = {
            new Color(1f, 0.9f, 0.7f, 0.4f),
            new Color(1f, 0.8f, 0.5f, 0.3f),
            new Color(0.8f, 0.9f, 1f, 0.25f),
            new Color(1f, 0.7f, 0.4f, 0.2f),
            new Color(0.9f, 0.95f, 1f, 0.15f),
            new Color(1f, 0.85f, 0.6f, 0.1f),
        };

        for (int i = 0; i < flareElements.Length; i++)
        {
            GameObject flare = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flare.name = $"FlareElement_{i}";
            flare.transform.SetParent(transform);
            Destroy(flare.GetComponent<Collider>());

            Material mat = new Material(flareMat);
            mat.color = colors[i];
            flare.GetComponent<Renderer>().material = mat;
            flare.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flare.GetComponent<Renderer>().receiveShadows = false;

            flareElements[i] = flare;
        }
    }

    void LateUpdate()
    {
        if (sunLight == null || flareElements == null) return;

        // Don't show lens flare at night
        if (NightMode.Instance != null && NightMode.Instance.isNight)
        {
            foreach (var flare in flareElements)
            {
                if (flare != null) flare.SetActive(false);
            }
            return;
        }

        // Get sun direction
        Vector3 sunDir = -sunLight.transform.forward;
        Vector3 camForward = mainCam.transform.forward;

        // Check if sun is in front of camera
        float dot = Vector3.Dot(sunDir, camForward);

        if (dot < 0)
        {
            // Sun behind camera, hide flares
            foreach (var flare in flareElements)
            {
                flare.SetActive(false);
            }
            return;
        }

        // Calculate sun screen position
        Vector3 sunWorldPos = mainCam.transform.position + sunDir * 500f;
        Vector3 sunScreenPos = mainCam.WorldToViewportPoint(sunWorldPos);

        // Check if sun is visible
        bool visible = sunScreenPos.x > -0.5f && sunScreenPos.x < 1.5f &&
                       sunScreenPos.y > -0.5f && sunScreenPos.y < 1.5f &&
                       sunScreenPos.z > 0;

        if (!visible)
        {
            foreach (var flare in flareElements)
            {
                flare.SetActive(false);
            }
            return;
        }

        // Calculate fade based on angle from center
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);
        Vector2 sunPos2D = new Vector2(sunScreenPos.x, sunScreenPos.y);
        float distFromCenter = Vector2.Distance(sunPos2D, screenCenter);

        // Fade intensity near edges
        float edgeFade = 1f - Mathf.Clamp01(distFromCenter * 1.5f);
        float currentIntensity = flareIntensity * edgeFade * Mathf.Clamp01(dot * 2f);

        // Position flare elements
        float[] positions = { 0.2f, 0.4f, 0.6f, 0.8f, 1.2f, 1.5f };
        float[] sizes = { 0.3f, 0.15f, 0.08f, 0.2f, 0.12f, 0.5f };

        for (int i = 0; i < flareElements.Length; i++)
        {
            flareElements[i].SetActive(true);

            // Flare extends from sun through center to opposite side
            Vector2 flarePos2D = screenCenter + (screenCenter - sunPos2D) * positions[i];
            Vector3 flareViewport = new Vector3(flarePos2D.x, flarePos2D.y, 20f);
            Vector3 flareWorld = mainCam.ViewportToWorldPoint(flareViewport);

            flareElements[i].transform.position = flareWorld;
            flareElements[i].transform.LookAt(mainCam.transform);
            flareElements[i].transform.localScale = Vector3.one * sizes[i] * currentIntensity * 5f;

            // Update alpha
            Color c = flareElements[i].GetComponent<Renderer>().material.color;
            c.a = currentIntensity * (0.1f + (1f - positions[i]) * 0.4f);
            flareElements[i].GetComponent<Renderer>().material.color = c;
        }
    }
}
