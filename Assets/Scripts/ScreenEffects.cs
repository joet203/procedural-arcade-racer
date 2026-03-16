using UnityEngine;

public class ScreenEffects : MonoBehaviour
{
    private CarController car;
    private Texture2D vignetteTexture;
    private Texture2D speedLinesTexture;
    private Texture2D gradientSkyTexture;

    // Smoothed values
    private float currentIntensity;
    private float intensityVelocity;
    private float currentChroma;
    private float chromaVelocity;
    private float speedLineAlpha;
    private float speedLineVelocity;

    [Header("Vignette")]
    public float vignetteIntensity = 0.2f;
    public float vignetteBoostIntensity = 0.35f;

    [Header("Speed Effects")]
    public float speedLinesStartSpeed = 0.75f;
    public float maxSpeedLineAlpha = 0.12f;

    [Header("Color Grading")]
    public float saturationBoost = 1.05f;
    public float contrastBoost = 1.02f;

    [Header("Film Grain")]
    private Texture2D grainTexture;
    private float grainIntensity = 0.015f;

    [Header("Bloom Simulation")]
    private Texture2D bloomTexture;
    private float bloomIntensity = 0f; // Disabled - was causing washed out look

    [Header("God Rays")]
    private Texture2D godRayTexture;
    private float godRayIntensity = 0f; // Disabled at night, too bright

    void Start()
    {
        car = FindFirstObjectByType<CarController>();
        CreateVignetteTexture();
        CreateSpeedLinesTexture();
        CreateGradientSky();
        CreateFilmGrain();
        CreateBloomTexture();
        CreateGodRayTexture();
    }

    void CreateGodRayTexture()
    {
        int width = 512;
        int height = 512;
        godRayTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Vector2 sunPos = new Vector2(width * 0.3f, height * 0.15f); // Top-left sun position

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2 pos = new Vector2(x, y);
                Vector2 dir = (pos - sunPos).normalized;
                float dist = Vector2.Distance(pos, sunPos);

                // Radial rays
                float angle = Mathf.Atan2(dir.y, dir.x);
                float ray = Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 12f)), 8f);

                // Fade with distance
                float fade = 1f - Mathf.Clamp01(dist / (width * 0.9f));
                fade = Mathf.Pow(fade, 1.5f);

                // Noise variation
                float noise = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);

                float alpha = ray * fade * (0.7f + noise * 0.3f) * 0.4f;

                godRayTexture.SetPixel(x, y, new Color(1f, 0.95f, 0.8f, alpha));
            }
        }

        godRayTexture.Apply();
        godRayTexture.wrapMode = TextureWrapMode.Clamp;
        godRayTexture.filterMode = FilterMode.Bilinear;
    }

    void CreateBloomTexture()
    {
        // Create a soft radial gradient for bloom simulation
        int size = 256;
        bloomTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalized = Mathf.Clamp01(dist / maxDist);

                // Soft bloom falloff
                float bloom = 1f - Mathf.Pow(normalized, 0.8f);
                bloom = Mathf.Pow(bloom, 2.5f);

                bloomTexture.SetPixel(x, y, new Color(1f, 0.95f, 0.85f, bloom));
            }
        }

        bloomTexture.Apply();
        bloomTexture.wrapMode = TextureWrapMode.Clamp;
        bloomTexture.filterMode = FilterMode.Bilinear;
    }

    void CreateFilmGrain()
    {
        int size = 128;
        grainTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        grainTexture.filterMode = FilterMode.Point;

        System.Random rand = new System.Random();
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = (float)rand.NextDouble();
                grainTexture.SetPixel(x, y, new Color(noise, noise, noise, 0.5f));
            }
        }
        grainTexture.Apply();
    }

    void CreateVignetteTexture()
    {
        int size = 512;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size * 0.7f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalized = Mathf.Clamp01(dist / maxDist);

                // Smooth falloff curve
                float vignette = Mathf.Pow(normalized, 2.2f);
                vignette = Mathf.SmoothStep(0f, 1f, vignette);

                vignetteTexture.SetPixel(x, y, new Color(0, 0, 0, vignette));
            }
        }

        vignetteTexture.Apply();
    }

    void CreateSpeedLinesTexture()
    {
        int width = 512;
        int height = 512;
        speedLinesTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        Vector2 center = new Vector2(width / 2f, height / 2f);

        // Clear to transparent
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        System.Random rand = new System.Random(42);

        // Draw radial lines from center
        int numLines = 80;
        for (int i = 0; i < numLines; i++)
        {
            float angle = (float)i / numLines * Mathf.PI * 2f;
            float length = 0.3f + (float)rand.NextDouble() * 0.2f;
            float thickness = 1f + (float)rand.NextDouble() * 2f;

            // Line from outer edge toward center
            for (float t = 0.5f; t < 0.5f + length; t += 0.002f)
            {
                float x = center.x + Mathf.Cos(angle) * t * width;
                float y = center.y + Mathf.Sin(angle) * t * height;

                // Fade based on distance from center
                float fade = Mathf.Clamp01((t - 0.5f) / length);
                fade = Mathf.Pow(fade, 0.5f);

                int px = Mathf.Clamp((int)x, 0, width - 1);
                int py = Mathf.Clamp((int)y, 0, height - 1);

                // Draw with some thickness
                for (int dx = -(int)thickness; dx <= (int)thickness; dx++)
                {
                    for (int dy = -(int)thickness; dy <= (int)thickness; dy++)
                    {
                        int fx = Mathf.Clamp(px + dx, 0, width - 1);
                        int fy = Mathf.Clamp(py + dy, 0, height - 1);
                        int idx = fy * width + fx;

                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float edgeFade = 1f - Mathf.Clamp01(dist / thickness);

                        Color existing = pixels[idx];
                        float newAlpha = fade * edgeFade * 0.6f;
                        pixels[idx] = new Color(1, 1, 1, Mathf.Max(existing.a, newAlpha));
                    }
                }
            }
        }

        speedLinesTexture.SetPixels(pixels);
        speedLinesTexture.Apply();
    }

    void CreateGradientSky()
    {
        // Apply gradient sky to camera
        Camera cam = Camera.main;
        if (cam == null) return;

        // Create a simple gradient background
        int height = 256;
        gradientSkyTexture = new Texture2D(1, height, TextureFormat.RGB24, false);

        Color skyTop = new Color(0.35f, 0.5f, 0.85f);
        Color skyMid = new Color(0.6f, 0.7f, 0.9f);
        Color skyHorizon = new Color(1f, 0.85f, 0.65f);
        Color skyGlow = new Color(1f, 0.7f, 0.5f);

        for (int y = 0; y < height; y++)
        {
            float t = (float)y / height;

            Color color;
            if (t < 0.3f)
            {
                // Horizon glow
                color = Color.Lerp(skyGlow, skyHorizon, t / 0.3f);
            }
            else if (t < 0.6f)
            {
                // Horizon to mid
                color = Color.Lerp(skyHorizon, skyMid, (t - 0.3f) / 0.3f);
            }
            else
            {
                // Mid to top
                color = Color.Lerp(skyMid, skyTop, (t - 0.6f) / 0.4f);
            }

            gradientSkyTexture.SetPixel(0, y, color);
        }

        gradientSkyTexture.wrapMode = TextureWrapMode.Clamp;
        gradientSkyTexture.filterMode = FilterMode.Bilinear;
        gradientSkyTexture.Apply();
    }

    void Update()
    {
        if (car == null) return;

        // Calculate target intensity
        float targetIntensity = vignetteIntensity;
        float targetChroma = 0f;
        float targetSpeedLines = 0f;

        if (car.IsBoosting)
        {
            targetIntensity = vignetteBoostIntensity;
            targetChroma = 0.008f;
        }

        if (car.SpeedPercent > speedLinesStartSpeed)
        {
            float speedFactor = (car.SpeedPercent - speedLinesStartSpeed) / (1f - speedLinesStartSpeed);
            targetSpeedLines = maxSpeedLineAlpha * speedFactor;

            if (car.IsBoosting)
                targetSpeedLines *= 1.5f;
        }

        // Smooth transitions
        currentIntensity = Mathf.SmoothDamp(currentIntensity, targetIntensity, ref intensityVelocity, 0.15f);
        currentChroma = Mathf.SmoothDamp(currentChroma, targetChroma, ref chromaVelocity, 0.1f);
        speedLineAlpha = Mathf.SmoothDamp(speedLineAlpha, targetSpeedLines, ref speedLineVelocity, 0.2f);
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;

        // Draw god rays (only in day mode - check if NightMode exists and is active)
        bool isNight = NightMode.Instance != null && NightMode.Instance.isNight;
        if (!isNight && godRayTexture != null && godRayIntensity > 0)
        {
            GUI.color = new Color(1f, 1f, 1f, godRayIntensity);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), godRayTexture, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }

        // Draw subtle bloom overlay (warm glow in center)
        if (bloomTexture != null && bloomIntensity > 0)
        {
            float intensity = bloomIntensity;
            if (car != null && car.IsBoosting)
                intensity *= 1.5f;

            // Adjust bloom color for night
            Color bloomColor = isNight ? new Color(0.5f, 0.6f, 1f, intensity) : new Color(1f, 1f, 1f, intensity);
            GUI.color = bloomColor;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), bloomTexture, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }

        // Draw speed lines
        if (speedLineAlpha > 0.01f && speedLinesTexture != null)
        {
            Color prevColor = GUI.color;
            GUI.color = new Color(1, 1, 1, speedLineAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), speedLinesTexture, ScaleMode.StretchToFill);
            GUI.color = prevColor;
        }

        // Draw vignette
        if (currentIntensity > 0.01f && vignetteTexture != null)
        {
            Color prevColor = GUI.color;
            GUI.color = new Color(0, 0, 0, currentIntensity);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture, ScaleMode.StretchToFill);
            GUI.color = prevColor;
        }

        // Draw subtle chromatic aberration overlay (red/cyan edges)
        if (currentChroma > 0.001f)
        {
            // Left edge - subtle cyan
            Color cyanTint = new Color(0f, 1f, 1f, currentChroma * 15f);
            GUI.color = cyanTint;
            GUI.DrawTexture(new Rect(0, 0, Screen.width * 0.08f, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);

            // Right edge - subtle red
            Color redTint = new Color(1f, 0f, 0f, currentChroma * 15f);
            GUI.color = redTint;
            GUI.DrawTexture(new Rect(Screen.width * 0.92f, 0, Screen.width * 0.08f, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);

            GUI.color = Color.white;
        }

        // Subtle film grain for cinematic look
        if (grainTexture != null && grainIntensity > 0)
        {
            GUI.color = new Color(0.5f, 0.5f, 0.5f, grainIntensity);
            // Offset randomly each frame for animated grain
            float offsetX = Random.Range(0f, 64f);
            float offsetY = Random.Range(0f, 64f);
            GUI.DrawTextureWithTexCoords(
                new Rect(0, 0, Screen.width, Screen.height),
                grainTexture,
                new Rect(offsetX / 128f, offsetY / 128f, Screen.width / 256f, Screen.height / 256f)
            );
            GUI.color = Color.white;
        }
    }
}
