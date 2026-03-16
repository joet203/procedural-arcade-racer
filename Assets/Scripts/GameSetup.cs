using UnityEngine;
using UnityEngine.InputSystem;

public class GameSetup : MonoBehaviour
{
    private GameObject car;
    private Vector3 startPosition = new Vector3(0, 0.5f, -75);

    void Awake()
    {
        // Fix DualShock4 gyro/touchpad flooding the input buffer
        InputSystem.settings.maxEventBytesPerUpdate = 0; // Unlimited
    }

    // Color palette - warm sunset aesthetic
    private Color skyTop = new Color(0.4f, 0.55f, 0.85f);
    private Color skyHorizon = new Color(1f, 0.75f, 0.5f);
    private Color sunColor = new Color(1f, 0.9f, 0.7f);
    private Color grassColor = new Color(0.22f, 0.38f, 0.18f);
    private Color trackColor = new Color(0.18f, 0.18f, 0.2f);
    private Color wallColor = new Color(0.85f, 0.85f, 0.9f);
    private Color accentColor = new Color(0.95f, 0.4f, 0.2f);

    void Start()
    {
        Application.targetFrameRate = 60;

        // Maximum quality settings
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowDistance = 600f;
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.shadowCascades = 4;
        QualitySettings.shadowProjection = ShadowProjection.StableFit;
        QualitySettings.antiAliasing = 8; // 8x MSAA for smooth edges
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.lodBias = 2f;
        QualitySettings.pixelLightCount = 12;
        QualitySettings.softParticles = true;
        QualitySettings.softVegetation = true;
        QualitySettings.realtimeReflectionProbes = true;

        // Texture manager for custom textures (must be before track/car creation)
        gameObject.AddComponent<TextureManager>();

        SetupLighting();
        car = CreateCar();
        CreateTrack();
        SetupCamera(car);
        CreateUI();
        CreateLapSystem();
        CreateReflectionProbe();

        // Night mode (set isNight = true for night, false for day)
        GameObject nightModeObj = new GameObject("NightMode");
        NightMode nm = nightModeObj.AddComponent<NightMode>();
        nm.isNight = true; // Enable night mode

        // Background music
        GameObject musicObj = new GameObject("BackgroundMusic");
        musicObj.AddComponent<BackgroundMusic>();

        // Car selector for switching between imported models
        GameObject selectorObj = new GameObject("CarSelector");
        selectorObj.AddComponent<CarSelector>();

        // Spawn cop cars and destructibles
        SpawnCopCars(28);
        SpawnDestructibles();

        // Spawn Dunkin Donuts stores
        SpawnDunkinStores();

        // Spawn power-ups around the track
        SpawnPowerUps();

        // Add player health system
        car.AddComponent<PlayerHealth>();

        // Cop takedown challenge
        gameObject.AddComponent<CopChallenge>();

        // Minimap
        gameObject.AddComponent<Minimap>();

        // Leaderboard
        gameObject.AddComponent<Leaderboard>();
    }

    void SpawnCopCars(int count)
    {
        GameObject copPrefab = Resources.Load<GameObject>("Cars/Cop");

        for (int i = 0; i < count; i++)
        {
            // Spawn at different positions around the track
            float angle = (i / (float)count) * 360f * Mathf.Deg2Rad;
            float radius = 120f + Random.Range(-30f, 30f);
            Vector3 spawnPos = new Vector3(
                Mathf.Cos(angle) * radius,
                1.5f, // Higher spawn to avoid getting stuck
                Mathf.Sin(angle) * radius
            );

            GameObject cop = null;
            if (copPrefab != null)
            {
                cop = Instantiate(copPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0));
                cop.transform.localScale = Vector3.one * 2.86f; // Bigger cop cars (10% increase)

                // Remove existing colliders from model
                foreach (Collider col in cop.GetComponentsInChildren<Collider>())
                {
                    Destroy(col);
                }
            }
            else
            {
                // Fallback: create simple box cop car (bigger)
                cop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cop.transform.position = spawnPos;
                cop.transform.localScale = new Vector3(2.86f, 1.54f, 5.72f); // 10% increase
            }

            cop.name = $"CopCar_{i}";

            // Add collider (bigger for easier projectile hits)
            BoxCollider box = cop.AddComponent<BoxCollider>();
            box.size = new Vector3(1.8f, 1.2f, 3.5f);
            box.center = new Vector3(0, 0.6f, 0);

            // Add components
            cop.AddComponent<CopCar>();
            Destructible dest = cop.AddComponent<Destructible>();
            dest.maxHealth = 80f;  // 3 regular shots (35 dmg) or 1 bazooka to kill
            dest.scoreValue = 500;
            dest.isCopCar = true;

            // Apply cop car material (black and white)
            ApplyCopCarMaterial(cop);
        }
    }

    void ApplyCopCarMaterial(GameObject cop)
    {
        Material blackMat = new Material(Shader.Find("Standard"));
        blackMat.color = new Color(0.1f, 0.1f, 0.1f);
        blackMat.SetFloat("_Metallic", 0.8f);
        blackMat.SetFloat("_Glossiness", 0.9f);

        foreach (Renderer r in cop.GetComponentsInChildren<Renderer>())
        {
            r.material = blackMat;
        }

        // Add police lights
        CreatePoliceLight(cop.transform, new Vector3(0, 1.2f, 0), Color.red, -0.3f);
        CreatePoliceLight(cop.transform, new Vector3(0, 1.2f, 0), Color.blue, 0.3f);
    }

    void CreatePoliceLight(Transform parent, Vector3 localPos, Color color, float xOffset)
    {
        GameObject lightBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lightBar.transform.SetParent(parent);
        lightBar.transform.localPosition = localPos + new Vector3(xOffset, 0, 0);
        lightBar.transform.localScale = new Vector3(0.3f, 0.15f, 0.2f);
        Destroy(lightBar.GetComponent<Collider>());

        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        lightBar.GetComponent<Renderer>().material = mat;

        // Add flashing light
        GameObject lightObj = new GameObject("PoliceLight");
        lightObj.transform.SetParent(lightBar.transform);
        lightObj.transform.localPosition = Vector3.zero;

        Light light = lightObj.AddComponent<Light>();
        light.color = color;
        light.intensity = 1.5f;
        light.range = 10f;

        lightObj.AddComponent<FlashingLight>().flashColor = color;
    }

    void SpawnDestructibles()
    {
        // Spawn destructible crates around the track
        Vector3[] cratePositions = {
            new Vector3(50, 0.5f, 80),
            new Vector3(-60, 0.5f, 90),
            new Vector3(70, 0.5f, -50),
            new Vector3(-80, 0.5f, -70),
            new Vector3(30, 0.5f, 120),
            new Vector3(-40, 0.5f, -120),
            new Vector3(100, 0.5f, 30),
            new Vector3(-100, 0.5f, -30),
        };

        foreach (Vector3 pos in cratePositions)
        {
            CreateDestructibleCrate(pos);
        }

        // Spawn destructible barrels
        Vector3[] barrelPositions = {
            new Vector3(40, 0.5f, 60),
            new Vector3(-50, 0.5f, 70),
            new Vector3(60, 0.5f, -40),
            new Vector3(-70, 0.5f, -60),
            new Vector3(90, 0.5f, 10),
            new Vector3(-90, 0.5f, -10),
        };

        foreach (Vector3 pos in barrelPositions)
        {
            CreateDestructibleBarrel(pos);
        }
    }

    void CreateDestructibleCrate(Vector3 pos)
    {
        GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crate.name = "Crate";
        crate.transform.position = pos;
        crate.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        crate.transform.rotation = Quaternion.Euler(0, Random.Range(0, 45), 0);

        Rigidbody rb = crate.AddComponent<Rigidbody>();
        rb.mass = 50f;

        Destructible dest = crate.AddComponent<Destructible>();
        dest.maxHealth = 30f;
        dest.scoreValue = 50;
        dest.debrisCount = 4;

        // Wood-like material
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.6f, 0.4f, 0.2f);
        mat.SetFloat("_Glossiness", 0.2f);
        crate.GetComponent<Renderer>().material = mat;
    }

    void CreateDestructibleBarrel(Vector3 pos)
    {
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.position = pos;
        barrel.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);

        Rigidbody rb = barrel.AddComponent<Rigidbody>();
        rb.mass = 30f;

        Destructible dest = barrel.AddComponent<Destructible>();
        dest.maxHealth = 20f;
        dest.scoreValue = 25;
        dest.debrisCount = 3;

        // Red barrel (explosive look)
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.8f, 0.2f, 0.1f);
        mat.SetFloat("_Glossiness", 0.6f);
        barrel.GetComponent<Renderer>().material = mat;
    }

    void SpawnDunkinStores()
    {
        // Spawn Dunkin Donuts stores around the track (outside the racing line)
        // Positioned at corners and along edges where cops can easily access
        // Spacing increased ~1.5-2x to spread stores further apart

        // Corner stores (brought ~12% closer: 280 -> 245)
        DunkinStore.Create(new Vector3(245, 0, 245), -45);   // NE corner
        DunkinStore.Create(new Vector3(-245, 0, 245), 45);   // NW corner
        DunkinStore.Create(new Vector3(-245, 0, -245), 135); // SW corner
        DunkinStore.Create(new Vector3(245, 0, -245), -135); // SE corner

        // Cardinal direction stores (brought ~12% closer: 260 -> 228)
        DunkinStore.Create(new Vector3(228, 0, 0), 90);      // East side
        DunkinStore.Create(new Vector3(-228, 0, 0), -90);    // West side
        DunkinStore.Create(new Vector3(0, 0, 228), 0);       // North side
        DunkinStore.Create(new Vector3(0, 0, -228), 180);    // South side
    }

    void SpawnPowerUps()
    {
        // Spawn power-ups at strategic positions around the track
        // Ammo pickups
        PowerUp.Create(PowerUpType.Ammo, new Vector3(80, 1.5f, 0));
        PowerUp.Create(PowerUpType.Ammo, new Vector3(-80, 1.5f, 0));
        PowerUp.Create(PowerUpType.Ammo, new Vector3(0, 1.5f, 100));
        PowerUp.Create(PowerUpType.Ammo, new Vector3(0, 1.5f, -100));

        // Speed boosts on straights
        PowerUp.Create(PowerUpType.SpeedBoost, new Vector3(60, 1.5f, 60));
        PowerUp.Create(PowerUpType.SpeedBoost, new Vector3(-60, 1.5f, -60));
        PowerUp.Create(PowerUpType.SpeedBoost, new Vector3(60, 1.5f, -60));
        PowerUp.Create(PowerUpType.SpeedBoost, new Vector3(-60, 1.5f, 60));

        // Shields in more exposed areas
        PowerUp.Create(PowerUpType.Shield, new Vector3(100, 1.5f, 50));
        PowerUp.Create(PowerUpType.Shield, new Vector3(-100, 1.5f, -50));
    }

    void Update()
    {
        // Restart with R key or controller Start button
        var gp = Gamepad.current;
        if (Input.GetKeyDown(KeyCode.R) || (gp != null && gp.startButton.wasPressedThisFrame))
        {
            RestartGame();
        }
    }

    void RestartGame()
    {
        if (car != null)
        {
            car.transform.position = startPosition;
            car.transform.rotation = Quaternion.identity;

            Rigidbody rb = car.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        if (LapSystem.Instance != null)
        {
            LapSystem.Instance.RestartRace();
        }

        // Reset cop challenge
        CopChallenge.Reset();

        // Hide leaderboard when restarting
        if (Leaderboard.Instance != null)
        {
            Leaderboard.Instance.Hide();
        }

        // Reset score
        GameUI.Score = 0;
        GameUI.DestroyedCount = 0;

        // Respawn cop cars (destroy remaining and spawn new ones)
        foreach (CopCar cop in FindObjectsByType<CopCar>(FindObjectsSortMode.None))
        {
            Destroy(cop.gameObject);
        }
        SpawnCopCars(28);
    }

    void SetupLighting()
    {
        // === MAIN SUN - Golden hour key light ===
        // Low angle for dramatic long shadows and warm color temperature
        GameObject sunObj = new GameObject("Sun");
        Light sun = sunObj.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.85f, 0.6f); // Warmer, more orange for sunset
        sun.intensity = 1.8f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.65f; // Slightly stronger for more definition
        sun.shadowBias = 0.015f;
        sun.shadowNormalBias = 0.25f;
        sun.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;
        sun.cookieSize = 100f;
        sunObj.transform.rotation = Quaternion.Euler(15, -30, 0); // Lower angle for longer shadows

        // === SKY FILL LIGHT - Simulates sky dome bounce ===
        // Cool blue to contrast the warm sun, comes from above/opposite
        GameObject fillObj = new GameObject("FillLight");
        Light fill = fillObj.AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.5f, 0.65f, 0.95f); // Cooler blue for sky bounce
        fill.intensity = 0.35f;
        fill.shadows = LightShadows.None;
        fill.renderMode = LightRenderMode.ForceVertex; // Performance optimization
        fillObj.transform.rotation = Quaternion.Euler(70, 160, 0);

        // === WARM RIM LIGHT - Enhances object silhouettes ===
        // Creates a warm glow on edges, especially on the car
        GameObject rimObj = new GameObject("RimLight");
        Light rim = rimObj.AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.color = new Color(1f, 0.75f, 0.45f); // Golden orange rim
        rim.intensity = 0.5f;
        rim.shadows = LightShadows.None;
        rim.renderMode = LightRenderMode.ForceVertex;
        rimObj.transform.rotation = Quaternion.Euler(12, 195, 0); // Behind and slightly above

        // === SUBTLE KICK LIGHT - Fills dark shadows ===
        // Very subtle light to lift shadow areas without washing them out
        GameObject kickObj = new GameObject("KickLight");
        Light kick = kickObj.AddComponent<Light>();
        kick.type = LightType.Directional;
        kick.color = new Color(0.7f, 0.6f, 0.85f); // Subtle purple/lavender
        kick.intensity = 0.15f;
        kick.shadows = LightShadows.None;
        kick.renderMode = LightRenderMode.ForceVertex;
        kickObj.transform.rotation = Quaternion.Euler(45, 90, 0); // From the side

        // === AMBIENT LIGHTING - Rich trilight for depth ===
        // Tunic-style uses vibrant ambient colors
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.45f, 0.55f, 0.8f);     // Cool sky blue
        RenderSettings.ambientEquatorColor = new Color(0.6f, 0.5f, 0.45f); // Warm midtones
        RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.18f); // Rich earth tones
        RenderSettings.ambientIntensity = 1.1f; // Slightly boost ambient

        // === ATMOSPHERIC FOG - Sunset haze ===
        // Warm-tinted fog for atmospheric depth and sunset feel
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.85f, 0.75f, 0.65f); // Warm sunset haze
        RenderSettings.fogDensity = 0.0012f; // Slightly less dense for better visibility

        // === CAMERA SETTINGS ===
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(0.4f, 0.5f, 0.75f); // Warm sunset sky base
        Camera.main.nearClipPlane = 0.3f;
        Camera.main.farClipPlane = 1500f;
        Camera.main.allowHDR = true; // Enable HDR for better color range

        // === REFLECTION SETTINGS ===
        RenderSettings.reflectionIntensity = 0.8f;
        RenderSettings.defaultReflectionResolution = 256;

        // Create sun glow in sky
        CreateSunGlow();
    }

    void CreateSunGlow()
    {
        // Sun position based on light direction (matches SetupLighting sun rotation)
        Vector3 sunDirection = Quaternion.Euler(15, -30, 0) * Vector3.forward;
        Vector3 sunWorldPos = Camera.main.transform.position - sunDirection * 450f;

        // === SUN DISC - Bright core ===
        GameObject sunDisc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sunDisc.name = "SunDisc";
        sunDisc.transform.position = sunWorldPos;
        sunDisc.transform.localScale = Vector3.one * 28f;
        Destroy(sunDisc.GetComponent<Collider>());

        Material sunMat = new Material(Shader.Find("Standard"));
        sunMat.color = new Color(1f, 0.92f, 0.75f);
        sunMat.SetColor("_EmissionColor", new Color(1f, 0.85f, 0.5f) * 4f); // Stronger emission
        sunMat.EnableKeyword("_EMISSION");
        sunMat.SetFloat("_Glossiness", 1f);
        sunMat.SetFloat("_Metallic", 0f);
        sunDisc.GetComponent<Renderer>().material = sunMat;
        sunDisc.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === INNER GLOW - Soft warm aura ===
        GameObject innerGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        innerGlow.name = "SunInnerGlow";
        innerGlow.transform.position = sunWorldPos;
        innerGlow.transform.localScale = Vector3.one * 55f;
        Destroy(innerGlow.GetComponent<Collider>());

        Material innerMat = new Material(Shader.Find("Standard"));
        innerMat.color = new Color(1f, 0.8f, 0.45f, 0.2f);
        innerMat.SetFloat("_Mode", 3); // Transparent
        innerMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        innerMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One); // Additive
        innerMat.SetInt("_ZWrite", 0);
        innerMat.DisableKeyword("_ALPHATEST_ON");
        innerMat.EnableKeyword("_ALPHABLEND_ON");
        innerMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        innerMat.renderQueue = 3000;
        innerMat.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.35f) * 0.8f);
        innerMat.EnableKeyword("_EMISSION");
        innerGlow.GetComponent<Renderer>().material = innerMat;
        innerGlow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === OUTER GLOW - Large atmospheric halo ===
        GameObject outerGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        outerGlow.name = "SunOuterGlow";
        outerGlow.transform.position = sunWorldPos;
        outerGlow.transform.localScale = Vector3.one * 120f;
        Destroy(outerGlow.GetComponent<Collider>());

        Material outerMat = new Material(Shader.Find("Standard"));
        outerMat.color = new Color(1f, 0.7f, 0.4f, 0.08f);
        outerMat.SetFloat("_Mode", 3);
        outerMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        outerMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        outerMat.SetInt("_ZWrite", 0);
        outerMat.DisableKeyword("_ALPHATEST_ON");
        outerMat.EnableKeyword("_ALPHABLEND_ON");
        outerMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        outerMat.renderQueue = 2999;
        outerMat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.25f) * 0.3f);
        outerMat.EnableKeyword("_EMISSION");
        outerGlow.GetComponent<Renderer>().material = outerMat;
        outerGlow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // === HORIZON GLOW - Gradient below sun ===
        // Creates a warm horizon gradient effect
        GameObject horizonGlow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        horizonGlow.name = "HorizonGlow";
        horizonGlow.transform.position = sunWorldPos + Vector3.down * 80f;
        horizonGlow.transform.localScale = new Vector3(300f, 100f, 1f);
        horizonGlow.transform.rotation = Quaternion.LookRotation(sunDirection);
        Destroy(horizonGlow.GetComponent<Collider>());

        Material horizonMat = new Material(Shader.Find("Standard"));
        horizonMat.color = new Color(1f, 0.65f, 0.35f, 0.1f);
        horizonMat.SetFloat("_Mode", 3);
        horizonMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        horizonMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        horizonMat.SetInt("_ZWrite", 0);
        horizonMat.EnableKeyword("_ALPHABLEND_ON");
        horizonMat.renderQueue = 2998;
        horizonMat.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.2f) * 0.25f);
        horizonMat.EnableKeyword("_EMISSION");
        horizonGlow.GetComponent<Renderer>().material = horizonMat;
        horizonGlow.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    GameObject CreateCar()
    {
        GameObject carObj = new GameObject("Car");
        carObj.transform.position = startPosition;

        Rigidbody rb = carObj.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 4f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.centerOfMass = new Vector3(0, -0.35f, 0.1f);

        BoxCollider col = carObj.AddComponent<BoxCollider>();
        col.size = new Vector3(1.9f, 0.6f, 4.4f);
        col.center = new Vector3(0, 0.3f, 0);

        PhysicsMaterial carMat = new PhysicsMaterial("CarMat");
        carMat.dynamicFriction = 0.25f;
        carMat.staticFriction = 0.25f;
        carMat.bounciness = 0.05f;
        carMat.frictionCombine = PhysicsMaterialCombine.Minimum;
        col.material = carMat;

        carObj.AddComponent<CarController>();
        carObj.AddComponent<CarEffects>();
        carObj.AddComponent<CarAudio>();
        carObj.AddComponent<CarTurret>();

        GameObject body = CreateCarBody();
        body.transform.SetParent(carObj.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = Vector3.one * 1.485f; // Bigger car (10% increase)
        body.name = "Body";

        // Apply custom car wrap texture if available
        if (TextureManager.Instance != null)
        {
            TextureManager.Instance.ApplyCarWrap(body);
        }

        return carObj;
    }

    GameObject CreateCarBody()
    {
        GameObject body = new GameObject("CarBody");

        // WRX Rally Blue with gold accents
        Color carColor = new Color(0.0f, 0.15f, 0.45f); // Subaru World Rally Blue
        Color goldAccent = new Color(0.85f, 0.7f, 0.2f); // Gold/bronze wheels
        Color blackTrim = new Color(0.05f, 0.05f, 0.05f);

        // Main body - boxy WRX sedan shape
        GameObject mainBody = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.28f, 0), new Vector3(1.85f, 0.42f, 4.5f));
        ApplyCarPaint(mainBody, carColor, 0.75f, 0.88f);

        // Front end - aggressive WRX nose
        GameObject frontLower = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.18f, 2.1f), new Vector3(1.8f, 0.22f, 0.5f));
        ApplyCarPaint(frontLower, carColor, 0.75f, 0.88f);

        // Front grille - hexagonal WRX style
        GameObject grille = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.22f, 2.28f), new Vector3(1.1f, 0.28f, 0.08f));
        grille.GetComponent<Renderer>().material = CreateMaterial(blackTrim);

        // Front bumper lower intake
        GameObject lowerIntake = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.08f, 2.25f), new Vector3(1.4f, 0.12f, 0.1f));
        lowerIntake.GetComponent<Renderer>().material = CreateMaterial(blackTrim);

        // Side air intakes (fog light area)
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.7f, 0.12f, 2.2f), new Vector3(0.35f, 0.15f, 0.12f))
            .GetComponent<Renderer>().material = CreateMaterial(blackTrim);
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.7f, 0.12f, 2.2f), new Vector3(0.35f, 0.15f, 0.12f))
            .GetComponent<Renderer>().material = CreateMaterial(blackTrim);

        // Hood with scoop cutout area
        GameObject hood = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.48f, 1.2f), new Vector3(1.7f, 0.08f, 1.8f));
        ApplyCarPaint(hood, carColor, 0.75f, 0.88f);

        // WRX Hood Scoop - iconic feature!
        GameObject scoopBase = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.55f, 1.0f), new Vector3(0.45f, 0.1f, 0.7f));
        ApplyCarPaint(scoopBase, blackTrim, 0.3f, 0.5f);

        GameObject scoopTop = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.62f, 0.85f), new Vector3(0.4f, 0.08f, 0.5f));
        ApplyCarPaint(scoopTop, blackTrim, 0.3f, 0.5f);

        // Scoop intake opening
        GameObject scoopIntake = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.58f, 1.15f), new Vector3(0.35f, 0.06f, 0.15f));
        scoopIntake.GetComponent<Renderer>().material = CreateMaterial(new Color(0.02f, 0.02f, 0.02f));

        // Rear bumper
        GameObject rearBumper = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.18f, -2.15f), new Vector3(1.8f, 0.28f, 0.3f));
        ApplyCarPaint(rearBumper, carColor, 0.75f, 0.88f);

        // Wide fender flares - WRX STI style
        // Front left fender
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.95f, 0.32f, 1.35f), new Vector3(0.15f, 0.35f, 0.9f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);
        // Front right fender
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.95f, 0.32f, 1.35f), new Vector3(0.15f, 0.35f, 0.9f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);
        // Rear left fender
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.95f, 0.32f, -1.2f), new Vector3(0.15f, 0.38f, 1.0f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);
        // Rear right fender
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.95f, 0.32f, -1.2f), new Vector3(0.15f, 0.38f, 1.0f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Cabin/greenhouse - WRX profile
        GameObject cabin = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.62f, -0.15f), new Vector3(1.5f, 0.36f, 1.6f));
        Material glassMat = new Material(Shader.Find("Standard"));
        glassMat.color = new Color(0.08f, 0.1f, 0.12f, 0.95f);
        glassMat.SetFloat("_Metallic", 0.9f);
        glassMat.SetFloat("_Glossiness", 0.95f);
        cabin.GetComponent<Renderer>().material = glassMat;

        // Roof
        GameObject roof = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.82f, -0.25f), new Vector3(1.45f, 0.06f, 1.3f));
        ApplyCarPaint(roof, carColor, 0.75f, 0.88f);

        // A-pillar (windshield frame) left
        GameObject aPillarL = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.7f, 0.68f, 0.55f), new Vector3(0.08f, 0.22f, 0.5f));
        aPillarL.transform.localRotation = Quaternion.Euler(0, 0, -12);
        aPillarL.GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // A-pillar right
        GameObject aPillarR = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.7f, 0.68f, 0.55f), new Vector3(0.08f, 0.22f, 0.5f));
        aPillarR.transform.localRotation = Quaternion.Euler(0, 0, 12);
        aPillarR.GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // C-pillar left
        GameObject cPillarL = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.68f, 0.65f, -0.85f), new Vector3(0.1f, 0.28f, 0.4f));
        cPillarL.transform.localRotation = Quaternion.Euler(0, 0, 8);
        cPillarL.GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // C-pillar right
        GameObject cPillarR = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.68f, 0.65f, -0.85f), new Vector3(0.1f, 0.28f, 0.4f));
        cPillarR.transform.localRotation = Quaternion.Euler(0, 0, -8);
        cPillarR.GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Trunk/rear deck
        GameObject trunk = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.52f, -1.65f), new Vector3(1.6f, 0.12f, 0.9f));
        ApplyCarPaint(trunk, carColor, 0.75f, 0.88f);

        // STI Wing - big rally spoiler
        GameObject wingMain = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0, 0.95f, -1.95f), new Vector3(1.55f, 0.06f, 0.35f));
        ApplyCarPaint(wingMain, carColor, 0.75f, 0.88f);

        // Wing endplates
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.78f, 0.92f, -1.95f), new Vector3(0.04f, 0.18f, 0.4f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.78f, 0.92f, -1.95f), new Vector3(0.04f, 0.18f, 0.4f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Wing supports/risers
        GameObject wingSupport1 = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.5f, 0.75f, -1.85f), new Vector3(0.08f, 0.35f, 0.08f));
        wingSupport1.GetComponent<Renderer>().material = CreateMaterial(blackTrim);
        GameObject wingSupport2 = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.5f, 0.75f, -1.85f), new Vector3(0.08f, 0.35f, 0.08f));
        wingSupport2.GetComponent<Renderer>().material = CreateMaterial(blackTrim);

        // Side skirts
        GameObject skirtL = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.92f, 0.08f, 0), new Vector3(0.08f, 0.1f, 3.8f));
        skirtL.GetComponent<Renderer>().material = CreateMaterial(blackTrim);
        GameObject skirtR = CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.92f, 0.08f, 0), new Vector3(0.08f, 0.1f, 3.8f));
        skirtR.GetComponent<Renderer>().material = CreateMaterial(blackTrim);

        // Side mirrors
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(-0.95f, 0.58f, 0.6f), new Vector3(0.15f, 0.08f, 0.12f))
            .GetComponent<Renderer>().material = CreateMaterial(blackTrim);
        CreatePrimitive(PrimitiveType.Cube, body.transform,
            new Vector3(0.95f, 0.58f, 0.6f), new Vector3(0.15f, 0.08f, 0.12f))
            .GetComponent<Renderer>().material = CreateMaterial(blackTrim);

        // Wheels - gold BBS style for WRX
        CreateWRXWheel(body.transform, new Vector3(-0.85f, 0.22f, 1.35f), "Wheel_FL", goldAccent);
        CreateWRXWheel(body.transform, new Vector3(0.85f, 0.22f, 1.35f), "Wheel_FR", goldAccent);
        CreateWRXWheel(body.transform, new Vector3(-0.85f, 0.22f, -1.25f), "Wheel_BL", goldAccent);
        CreateWRXWheel(body.transform, new Vector3(0.85f, 0.22f, -1.25f), "Wheel_BR", goldAccent);

        // Headlights - WRX hawk-eye style
        CreateWRXHeadlight(body.transform, new Vector3(-0.6f, 0.32f, 2.2f));
        CreateWRXHeadlight(body.transform, new Vector3(0.6f, 0.32f, 2.2f));

        // Taillights - WRX C-shape style (positioned outside bumper to avoid z-fighting)
        CreateWRXTaillight(body.transform, new Vector3(-0.7f, 0.42f, -2.32f));
        CreateWRXTaillight(body.transform, new Vector3(0.7f, 0.42f, -2.32f));

        // Exhaust tips - quad exhaust
        CreateExhaust(body.transform, new Vector3(-0.5f, 0.1f, -2.3f));
        CreateExhaust(body.transform, new Vector3(-0.35f, 0.1f, -2.3f));
        CreateExhaust(body.transform, new Vector3(0.35f, 0.1f, -2.3f));
        CreateExhaust(body.transform, new Vector3(0.5f, 0.1f, -2.3f));

        // ===== SMOOTHING ELEMENTS - Soften the boxy look =====

        // Front bumper corners - rounded
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(-0.85f, 0.18f, 2.2f), new Vector3(0.25f, 0.22f, 0.25f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(0.85f, 0.18f, 2.2f), new Vector3(0.25f, 0.22f, 0.25f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Rear bumper corners - rounded
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(-0.85f, 0.18f, -2.2f), new Vector3(0.25f, 0.25f, 0.2f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(0.85f, 0.18f, -2.2f), new Vector3(0.25f, 0.25f, 0.2f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Hood front edge - soft curve
        CreatePrimitive(PrimitiveType.Capsule, body.transform,
            new Vector3(0, 0.42f, 2.05f), new Vector3(1.6f, 0.08f, 0.08f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Roof front edge - windshield top curve
        CreatePrimitive(PrimitiveType.Capsule, body.transform,
            new Vector3(0, 0.78f, 0.5f), new Vector3(1.35f, 0.06f, 0.06f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Roof rear edge
        CreatePrimitive(PrimitiveType.Capsule, body.transform,
            new Vector3(0, 0.78f, -0.9f), new Vector3(1.35f, 0.06f, 0.06f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Roof side edges (capsules running front to back)
        GameObject roofEdgeL = CreatePrimitive(PrimitiveType.Capsule, body.transform,
            new Vector3(-0.7f, 0.8f, -0.2f), new Vector3(0.06f, 0.75f, 0.06f));
        roofEdgeL.transform.localRotation = Quaternion.Euler(90, 0, 0);
        roofEdgeL.GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        GameObject roofEdgeR = CreatePrimitive(PrimitiveType.Capsule, body.transform,
            new Vector3(0.7f, 0.8f, -0.2f), new Vector3(0.06f, 0.75f, 0.06f));
        roofEdgeR.transform.localRotation = Quaternion.Euler(90, 0, 0);
        roofEdgeR.GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Fender arch curves - front wheels
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(-0.85f, 0.4f, 1.35f), new Vector3(0.35f, 0.2f, 0.55f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(0.85f, 0.4f, 1.35f), new Vector3(0.35f, 0.2f, 0.55f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Fender arch curves - rear wheels
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(-0.85f, 0.42f, -1.2f), new Vector3(0.38f, 0.22f, 0.6f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(0.85f, 0.42f, -1.2f), new Vector3(0.38f, 0.22f, 0.6f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Body side contour lines (subtle bulge)
        GameObject sideLineL = CreatePrimitive(PrimitiveType.Capsule, body.transform,
            new Vector3(-0.92f, 0.35f, 0), new Vector3(0.05f, 2.0f, 0.05f));
        sideLineL.transform.localRotation = Quaternion.Euler(90, 0, 0);
        sideLineL.GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        GameObject sideLineR = CreatePrimitive(PrimitiveType.Capsule, body.transform,
            new Vector3(0.92f, 0.35f, 0), new Vector3(0.05f, 2.0f, 0.05f));
        sideLineR.transform.localRotation = Quaternion.Euler(90, 0, 0);
        sideLineR.GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Trunk lid curve
        CreatePrimitive(PrimitiveType.Sphere, body.transform,
            new Vector3(0, 0.55f, -1.9f), new Vector3(1.5f, 0.15f, 0.3f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        // Wing rounded edges
        CreatePrimitive(PrimitiveType.Capsule, body.transform,
            new Vector3(0, 0.95f, -2.08f), new Vector3(1.5f, 0.04f, 0.04f))
            .GetComponent<Renderer>().material = CreateCarPaintMaterial(carColor, 0.75f, 0.88f);

        return body;
    }

    void CreateWRXWheel(Transform parent, Vector3 pos, string name, Color rimColor)
    {
        GameObject wheelHolder = new GameObject(name);
        wheelHolder.transform.SetParent(parent);
        wheelHolder.transform.localPosition = pos;

        // Tire - wider for rally
        GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tire.name = "Tire";
        tire.transform.SetParent(wheelHolder.transform);
        tire.transform.localPosition = Vector3.zero;
        tire.transform.localRotation = Quaternion.Euler(0, 0, 90);
        tire.transform.localScale = new Vector3(0.55f, 0.22f, 0.55f);
        Destroy(tire.GetComponent<Collider>());

        Material tireMat = new Material(Shader.Find("Standard"));
        tireMat.color = new Color(0.1f, 0.1f, 0.1f);
        tireMat.SetFloat("_Glossiness", 0.15f);
        tire.GetComponent<Renderer>().material = tireMat;

        // Gold rim
        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Rim";
        rim.transform.SetParent(wheelHolder.transform);
        rim.transform.localPosition = new Vector3(pos.x > 0 ? 0.1f : -0.1f, 0, 0);
        rim.transform.localRotation = Quaternion.Euler(0, 0, 90);
        rim.transform.localScale = new Vector3(0.4f, 0.1f, 0.4f);
        Destroy(rim.GetComponent<Collider>());

        Material rimMat = new Material(Shader.Find("Standard"));
        rimMat.color = rimColor;
        rimMat.SetFloat("_Metallic", 0.95f);
        rimMat.SetFloat("_Glossiness", 0.9f);
        rim.GetComponent<Renderer>().material = rimMat;

        // Center cap - STI logo area
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.transform.SetParent(wheelHolder.transform);
        cap.transform.localPosition = new Vector3(pos.x > 0 ? 0.12f : -0.12f, 0, 0);
        cap.transform.localRotation = Quaternion.Euler(0, 0, 90);
        cap.transform.localScale = new Vector3(0.15f, 0.05f, 0.15f);
        Destroy(cap.GetComponent<Collider>());
        cap.GetComponent<Renderer>().material = CreateMaterial(new Color(0.9f, 0.1f, 0.2f)); // Red STI center
    }

    void CreateWRXHeadlight(Transform parent, Vector3 pos)
    {
        // Main headlight housing
        GameObject housing = CreatePrimitive(PrimitiveType.Cube, parent, pos, new Vector3(0.45f, 0.18f, 0.1f));
        Material glassMat = new Material(Shader.Find("Standard"));
        glassMat.color = new Color(0.85f, 0.9f, 0.95f);
        glassMat.SetFloat("_Metallic", 0.1f);
        glassMat.SetFloat("_Glossiness", 0.95f);
        housing.GetComponent<Renderer>().material = glassMat;

        // LED strip inside
        GameObject led = CreatePrimitive(PrimitiveType.Cube, parent,
            pos + new Vector3(0, -0.04f, 0.02f), new Vector3(0.38f, 0.03f, 0.02f));
        Material ledMat = new Material(Shader.Find("Standard"));
        ledMat.color = new Color(0.95f, 0.98f, 1f);
        ledMat.SetColor("_EmissionColor", new Color(0.95f, 0.98f, 1f) * 0.4f);  // Reduced from 1.2 to prevent blowout
        ledMat.EnableKeyword("_EMISSION");
        led.GetComponent<Renderer>().material = ledMat;
    }

    void CreateWRXTaillight(Transform parent, Vector3 pos)
    {
        // C-shaped taillight - unlit to prevent flickering
        GameObject light = CreatePrimitive(PrimitiveType.Cube, parent, pos, new Vector3(0.35f, 0.2f, 0.06f));
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.9f, 0.15f, 0.1f);
        light.GetComponent<Renderer>().material = mat;
        light.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        light.GetComponent<Renderer>().receiveShadows = false;
    }

    void CreateExhaust(Transform parent, Vector3 pos)
    {
        GameObject exhaust = CreatePrimitive(PrimitiveType.Cylinder, parent, pos, new Vector3(0.08f, 0.05f, 0.08f));
        exhaust.transform.localRotation = Quaternion.Euler(90, 0, 0);
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.3f, 0.32f);
        mat.SetFloat("_Metallic", 0.9f);
        mat.SetFloat("_Glossiness", 0.8f);
        exhaust.GetComponent<Renderer>().material = mat;
    }

    Material CreateCarPaintMaterial(Color color, float metallic, float smoothness)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);
        Color fresnelColor = Color.Lerp(color, Color.white, 0.1f) * 0.15f;
        mat.SetColor("_EmissionColor", fresnelColor);
        mat.EnableKeyword("_EMISSION");
        return mat;
    }

    void CreateHeadlightRounded(Transform parent, Vector3 pos)
    {
        GameObject light = CreatePrimitive(PrimitiveType.Sphere, parent, pos, new Vector3(0.28f, 0.12f, 0.08f));
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.9f, 0.95f, 1f);
        mat.SetColor("_EmissionColor", new Color(0.9f, 0.95f, 1f) * 0.5f);  // Reduced from 1.5 to prevent blowout
        mat.EnableKeyword("_EMISSION");
        light.GetComponent<Renderer>().material = mat;
    }

    void CreateTaillightRounded(Transform parent, Vector3 pos)
    {
        GameObject light = CreatePrimitive(PrimitiveType.Sphere, parent, pos, new Vector3(0.2f, 0.12f, 0.06f));
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.9f, 0.15f, 0.1f);
        light.GetComponent<Renderer>().material = mat;
        light.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        light.GetComponent<Renderer>().receiveShadows = false;
    }

    void ApplyCarPaint(GameObject obj, Color color, float metallic, float smoothness)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", smoothness);

        // Add subtle fresnel-like effect by using emission on edges
        // This simulates the rim lighting effect on car paint
        Color fresnelColor = Color.Lerp(color, Color.white, 0.1f) * 0.15f;
        mat.SetColor("_EmissionColor", fresnelColor);
        mat.EnableKeyword("_EMISSION");

        obj.GetComponent<Renderer>().material = mat;
    }

    void CreateWheel(Transform parent, Vector3 pos, string name)
    {
        GameObject wheelHolder = new GameObject(name);
        wheelHolder.transform.SetParent(parent);
        wheelHolder.transform.localPosition = pos;

        // Tire
        GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tire.name = "Tire";
        tire.transform.SetParent(wheelHolder.transform);
        tire.transform.localPosition = Vector3.zero;
        tire.transform.localRotation = Quaternion.Euler(0, 0, 90);
        tire.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
        Destroy(tire.GetComponent<Collider>());

        Material tireMat = new Material(Shader.Find("Standard"));
        tireMat.color = new Color(0.12f, 0.12f, 0.12f);
        tireMat.SetFloat("_Glossiness", 0.2f);
        tire.GetComponent<Renderer>().material = tireMat;

        // Rim - brushed metal look
        GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "Rim";
        rim.transform.SetParent(wheelHolder.transform);
        rim.transform.localPosition = new Vector3(pos.x > 0 ? 0.1f : -0.1f, 0, 0);
        rim.transform.localRotation = Quaternion.Euler(0, 0, 90);
        rim.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
        Destroy(rim.GetComponent<Collider>());

        Material rimMat = new Material(Shader.Find("Standard"));
        rimMat.color = new Color(0.5f, 0.52f, 0.55f);
        rimMat.SetFloat("_Metallic", 0.9f);
        rimMat.SetFloat("_Glossiness", 0.85f);
        rim.GetComponent<Renderer>().material = rimMat;

        // Center cap with orange accent
        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cap.transform.SetParent(wheelHolder.transform);
        cap.transform.localPosition = new Vector3(pos.x > 0 ? 0.12f : -0.12f, 0, 0);
        cap.transform.localRotation = Quaternion.Euler(0, 0, 90);
        cap.transform.localScale = new Vector3(0.12f, 0.04f, 0.12f);
        Destroy(cap.GetComponent<Collider>());
        cap.GetComponent<Renderer>().material = CreateMaterial(accentColor);
    }

    void CreateHeadlight(Transform parent, Vector3 pos)
    {
        GameObject light = CreatePrimitive(PrimitiveType.Cube, parent, pos, new Vector3(0.35f, 0.08f, 0.02f));
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.9f, 0.95f, 1f);
        mat.SetColor("_EmissionColor", new Color(0.9f, 0.95f, 1f) * 0.5f);  // Reduced from 1.5 to prevent blowout
        mat.EnableKeyword("_EMISSION");
        light.GetComponent<Renderer>().material = mat;
    }

    void CreateTaillight(Transform parent, Vector3 pos, bool isCenter = false)
    {
        Vector3 scale = isCenter ? new Vector3(0.8f, 0.04f, 0.02f) : new Vector3(0.25f, 0.1f, 0.02f);
        GameObject light = CreatePrimitive(PrimitiveType.Cube, parent, pos, scale);
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = new Color(0.9f, 0.15f, 0.1f);
        light.GetComponent<Renderer>().material = mat;
        light.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        light.GetComponent<Renderer>().receiveShadows = false;
    }

    GameObject CreatePrimitive(PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = pos;
        obj.transform.localScale = scale;
        Destroy(obj.GetComponent<Collider>());
        return obj;
    }

    void SetupCamera(GameObject carObj)
    {
        CameraFollow follow = Camera.main.gameObject.AddComponent<CameraFollow>();
        follow.target = carObj.transform;
        follow.car = carObj.GetComponent<CarController>();

        // Ensure audio listener exists
        if (Camera.main.GetComponent<AudioListener>() == null)
        {
            Camera.main.gameObject.AddComponent<AudioListener>();
        }

        // Add screen effects for polish
        Camera.main.gameObject.AddComponent<ScreenEffects>();

        // Add lens flare
        Camera.main.gameObject.AddComponent<LensFlare>();

        // Add post-processing (Tunic-style)
        PostProcessingSetup postFX = Camera.main.gameObject.AddComponent<PostProcessingSetup>();
        postFX.style = PostProcessingSetup.PostProcessingStyle.TunicClean;
    }

    void CreateUI()
    {
        GameObject ui = new GameObject("GameUI");
        ui.AddComponent<GameUI>();
    }

    void CreateLapSystem()
    {
        GameObject lapSystem = new GameObject("LapSystem");
        lapSystem.AddComponent<LapSystem>();

        // Add skid marks system
        GameObject skidMarks = new GameObject("SkidMarks");
        skidMarks.AddComponent<SkidMarks>();
    }

    void CreateReflectionProbe()
    {
        // Main reflection probe for the scene
        GameObject probeObj = new GameObject("ReflectionProbe");
        probeObj.transform.position = new Vector3(0, 30, 0);

        ReflectionProbe probe = probeObj.AddComponent<ReflectionProbe>();
        probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        probe.resolution = 256;
        probe.size = new Vector3(600, 100, 600);
        probe.intensity = 1f;
        probe.boxProjection = false;
        probe.hdr = true;

        // Render once at start
        probe.RenderProbe();

        // Car-following reflection probe for better car reflections
        GameObject carProbeObj = new GameObject("CarReflectionProbe");
        carProbeObj.transform.SetParent(car.transform);
        carProbeObj.transform.localPosition = new Vector3(0, 2, 0);

        ReflectionProbe carProbe = carProbeObj.AddComponent<ReflectionProbe>();
        carProbe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        carProbe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
        carProbe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
        carProbe.resolution = 128;
        carProbe.size = new Vector3(15, 8, 15);
        carProbe.intensity = 1.2f;
        carProbe.boxProjection = false;
        carProbe.hdr = true;
    }

    void CreateTrack()
    {
        CreateGround();
        CreateTrackSurface();
        CreateWalls();
        CreateRamps();
        CreateDecorations();
        CreateCheckpoints();
        CreatePuddles();
        CreateRubberMarks();
    }

    void CreateGround()
    {
        // Large ground plane with nice grass color (or custom texture)
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(150, 1, 150);

        // Use TextureManager for grass texture if available
        Material grassMat;
        if (TextureManager.Instance != null)
        {
            grassMat = TextureManager.Instance.CreateGrassMaterial(grassColor);
        }
        else
        {
            grassMat = new Material(Shader.Find("Standard"));
            grassMat.color = grassColor;
            grassMat.SetFloat("_Glossiness", 0.1f);
        }

        ground.GetComponent<Renderer>().material = grassMat;
        ground.isStatic = true;
    }

    void CreateTrackSurface()
    {
        // Track material - procedural asphalt texture from TextureManager
        Material roadMat;
        if (TextureManager.Instance != null)
        {
            roadMat = TextureManager.Instance.CreateAsphaltMaterial();
        }
        else
        {
            // Fallback if TextureManager not available
            roadMat = new Material(Shader.Find("Standard"));
            roadMat.color = trackColor;
            roadMat.SetFloat("_Glossiness", 0.3f);
            roadMat.SetFloat("_Metallic", 0.0f);
        }

        float trackWidth = 24f;

        // Main straights - Y=0.03 to prevent z-fighting with ground at Y=0
        CreateRoadPieceTextured(new Vector3(0, 0.03f, 150), new Vector3(trackWidth, 0.02f, 165), roadMat);
        CreateRoadPieceTextured(new Vector3(0, 0.03f, -150), new Vector3(trackWidth, 0.02f, 165), roadMat);

        // Sides
        CreateRoadPieceTextured(new Vector3(165, 0.03f, 0), new Vector3(165, 0.02f, trackWidth), roadMat);
        CreateRoadPieceTextured(new Vector3(-165, 0.03f, 0), new Vector3(165, 0.02f, trackWidth), roadMat);

        // Corners - smoother with more segments
        CreateCornerPieceTextured(new Vector3(126, 0.03f, 126), 0, trackWidth, roadMat);
        CreateCornerPieceTextured(new Vector3(-126, 0.03f, 126), 90, trackWidth, roadMat);
        CreateCornerPieceTextured(new Vector3(-126, 0.03f, -126), 180, trackWidth, roadMat);
        CreateCornerPieceTextured(new Vector3(126, 0.03f, -126), 270, trackWidth, roadMat);

        // Start/finish - Y=0.05 above track surface
        CreateStartFinish(new Vector3(0, 0.05f, -75));

        // Track markings
        CreateTrackMarkings();

        // Curbs - subtle
        CreateCurbs();
    }

    void CreateRoadPiece(Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.position = pos;
        road.transform.localScale = scale;
        road.GetComponent<Renderer>().material = mat;
        road.isStatic = true;
        Destroy(road.GetComponent<Collider>());
    }

    void CreateRoadPieceTextured(Vector3 pos, Vector3 scale, Material baseMat)
    {
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "Road";
        road.transform.position = pos;
        road.transform.localScale = scale;

        // Create a new material instance to avoid sharing tiling across pieces
        Material mat = new Material(baseMat);
        road.GetComponent<Renderer>().material = mat;

        // Apply asphalt texture with proper tiling based on world scale
        if (TextureManager.Instance != null)
        {
            TextureManager.Instance.ApplyAsphaltToRoad(road.GetComponent<Renderer>(), scale);
        }

        road.isStatic = true;
        Destroy(road.GetComponent<Collider>());
    }

    void CreateCornerPiece(Vector3 center, float startAngle, float width, Material mat)
    {
        int segments = 16; // More segments for smoother corners
        for (int i = 0; i < segments; i++)
        {
            float angle = (startAngle + i * (90f / segments) + 45f / segments) * Mathf.Deg2Rad;
            float radius = 60f;

            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 0.03f, Mathf.Sin(angle) * radius);

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "CornerRoad";
            seg.transform.position = pos;
            seg.transform.localScale = new Vector3(width, 0.02f, 25f);
            seg.transform.rotation = Quaternion.Euler(0, startAngle + i * (90f / segments) + 45f / segments, 0);
            seg.GetComponent<Renderer>().material = mat;
            seg.isStatic = true;
            Destroy(seg.GetComponent<Collider>());
        }
    }

    void CreateCornerPieceTextured(Vector3 center, float startAngle, float width, Material baseMat)
    {
        int segments = 16; // More segments for smoother corners
        for (int i = 0; i < segments; i++)
        {
            float angle = (startAngle + i * (90f / segments) + 45f / segments) * Mathf.Deg2Rad;
            float radius = 60f;

            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 0.03f, Mathf.Sin(angle) * radius);
            Vector3 scale = new Vector3(width, 0.02f, 25f);

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seg.name = "CornerRoad";
            seg.transform.position = pos;
            seg.transform.localScale = scale;
            seg.transform.rotation = Quaternion.Euler(0, startAngle + i * (90f / segments) + 45f / segments, 0);

            // Create a new material instance to avoid sharing tiling across pieces
            Material mat = new Material(baseMat);
            seg.GetComponent<Renderer>().material = mat;

            // Apply asphalt texture with proper tiling based on world scale
            if (TextureManager.Instance != null)
            {
                TextureManager.Instance.ApplyAsphaltToRoad(seg.GetComponent<Renderer>(), scale);
            }

            seg.isStatic = true;
            Destroy(seg.GetComponent<Collider>());
        }
    }

    void CreateStartFinish(Vector3 pos)
    {
        int checks = 14;
        float width = 24f / checks;

        for (int i = 0; i < checks; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                bool white = (i + j) % 2 == 0;
                GameObject check = GameObject.CreatePrimitive(PrimitiveType.Cube);
                check.transform.position = pos + new Vector3(-12f + i * width + width / 2, 0, -1.5f + j * 3f);
                check.transform.localScale = new Vector3(width * 0.95f, 0.01f, 2.9f);

                Material mat = new Material(Shader.Find("Standard"));
                mat.color = white ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.1f, 0.1f, 0.1f);
                mat.SetFloat("_Glossiness", 0.4f);
                check.GetComponent<Renderer>().material = mat;
                check.isStatic = true;
                Destroy(check.GetComponent<Collider>());
            }
        }
    }

    void CreateTrackMarkings()
    {
        Material lineMat = new Material(Shader.Find("Standard"));
        lineMat.color = new Color(0.9f, 0.9f, 0.9f);
        lineMat.SetFloat("_Glossiness", 0.2f);

        // Center dashed lines on straights
        for (float z = -210; z <= 210; z += 10)
        {
            if (Mathf.Abs(z) < 66 || Mathf.Abs(z) > 225) continue;

            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.transform.position = new Vector3(0, 0.07f, z);
            line.transform.localScale = new Vector3(0.25f, 0.01f, 5f);
            line.GetComponent<Renderer>().material = lineMat;
            line.isStatic = true;
            Destroy(line.GetComponent<Collider>());
        }

        // Edge lines - Y=0.07 above track surface (Y=0.03)
        CreateEdgeLine(new Vector3(-11.5f, 0.07f, 150), new Vector3(0.2f, 0.01f, 165));
        CreateEdgeLine(new Vector3(11.5f, 0.07f, 150), new Vector3(0.2f, 0.01f, 165));
        CreateEdgeLine(new Vector3(-11.5f, 0.07f, -150), new Vector3(0.2f, 0.01f, 165));
        CreateEdgeLine(new Vector3(11.5f, 0.07f, -150), new Vector3(0.2f, 0.01f, 165));
    }

    void CreateEdgeLine(Vector3 pos, Vector3 scale)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.transform.position = pos;
        line.transform.localScale = scale;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.9f, 0.9f, 0.9f);
        line.GetComponent<Renderer>().material = mat;
        line.isStatic = true;
        Destroy(line.GetComponent<Collider>());
    }

    void CreateCurbs()
    {
        // Subtle curbs at corners - alternating orange and white
        Vector3[] curbCenters = new Vector3[]
        {
            new Vector3(96, 0, 96),
            new Vector3(-96, 0, 96),
            new Vector3(-96, 0, -96),
            new Vector3(96, 0, -96),
        };

        foreach (var center in curbCenters)
        {
            for (int i = 0; i < 14; i++)
            {
                float angle = (Mathf.Atan2(center.z, center.x) * Mathf.Rad2Deg + 45 + i * 6.5f) * Mathf.Deg2Rad;
                // Y=0.06 to be clearly above track surface (Y=0.03)
                Vector3 pos = center + new Vector3(Mathf.Cos(angle) * 18f, 0.06f, Mathf.Sin(angle) * 18f);

                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.transform.position = pos;
                curb.transform.localScale = new Vector3(3f, 0.08f, 1.2f);
                curb.transform.rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg + 90, 0);

                Color curbColor = i % 2 == 0 ? accentColor : new Color(0.95f, 0.95f, 0.95f);
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = curbColor;
                mat.SetFloat("_Glossiness", 0.3f);
                curb.GetComponent<Renderer>().material = mat;
                curb.isStatic = true;
                Destroy(curb.GetComponent<Collider>());
            }
        }
    }

    void CreateRamps()
    {
        CreateRamp(new Vector3(0, 0, 180), 0);
        CreateBump(new Vector3(195, 0, 0), 90);
        CreateBump(new Vector3(-195, 0, 0), 90);
    }

    void CreateRamp(Vector3 pos, float angle)
    {
        // Smooth ramp
        GameObject rampBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampBase.name = "RampBase";
        rampBase.transform.position = pos + new Vector3(0, 0.4f, -4f);
        rampBase.transform.localScale = new Vector3(18f, 0.8f, 7f);

        Material rampMat = new Material(Shader.Find("Standard"));
        rampMat.color = new Color(0.25f, 0.25f, 0.28f);
        rampMat.SetFloat("_Glossiness", 0.35f);
        rampBase.GetComponent<Renderer>().material = rampMat;

        // Ramp surface
        GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "Ramp";
        ramp.transform.position = pos + new Vector3(0, 0.85f, 0);
        ramp.transform.localScale = new Vector3(18f, 0.3f, 14f);
        ramp.transform.rotation = Quaternion.Euler(-12, angle, 0);
        ramp.GetComponent<Renderer>().material = rampMat;

        // Warning stripes
        Material stripeMat = new Material(Shader.Find("Standard"));
        stripeMat.color = new Color(1f, 0.85f, 0.2f);
        stripeMat.SetFloat("_Glossiness", 0.4f);

        for (int i = -4; i <= 4; i++)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.position = pos + new Vector3(i * 2f, 0.9f, 0);
            stripe.transform.localScale = new Vector3(0.25f, 0.06f, 11f);
            stripe.transform.rotation = Quaternion.Euler(-12, angle, 0);
            stripe.GetComponent<Renderer>().material = stripeMat;
            Destroy(stripe.GetComponent<Collider>());
        }
    }

    void CreateBump(Vector3 pos, float angle)
    {
        GameObject bump = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bump.name = "Bump";
        bump.transform.position = pos;
        bump.transform.localScale = new Vector3(16f, 0.45f, 6f);
        bump.transform.rotation = Quaternion.Euler(0, angle, 0);

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.22f, 0.22f, 0.25f);
        mat.SetFloat("_Glossiness", 0.3f);
        bump.GetComponent<Renderer>().material = mat;
    }

    void CreateWalls()
    {
        // Clean, modern barriers with concrete texture
        Material wallMat;
        if (TextureManager.Instance != null)
        {
            // Use procedural concrete texture with normal map
            wallMat = TextureManager.Instance.CreateConcreteMaterial(wallColor);
        }
        else
        {
            wallMat = new Material(Shader.Find("Standard"));
            wallMat.color = wallColor;
            wallMat.SetFloat("_Glossiness", 0.2f);
        }

        Material accentMat = new Material(Shader.Find("Standard"));
        accentMat.color = accentColor;
        accentMat.SetFloat("_Glossiness", 0.4f);

        float wallHeight = 1.4f;
        float wallThickness = 1f;

        // Outer walls - reduced size for less obtrusive barriers
        CreateBarrier(new Vector3(0, 0, 250), new Vector3(400, wallHeight, wallThickness), 0, wallMat, accentMat);
        CreateBarrier(new Vector3(0, 0, -250), new Vector3(400, wallHeight, wallThickness), 0, wallMat, accentMat);

        // Inner walls removed - track is now more open
    }

    void CreateBarrier(Vector3 pos, Vector3 scale, float angle, Material wallMat, Material accentMat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Barrier";
        wall.transform.position = pos + new Vector3(0, scale.y / 2, 0);
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().material = wallMat;
        wall.isStatic = true;

        PhysicsMaterial bounceMat = new PhysicsMaterial("WallPhys");
        bounceMat.bounciness = 0.2f;
        bounceMat.dynamicFriction = 0.5f;
        wall.GetComponent<Collider>().material = bounceMat;

        // Orange accent stripe
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.transform.SetParent(wall.transform);
        stripe.transform.localPosition = new Vector3(0, 0.3f, 0);
        stripe.transform.localScale = new Vector3(1.01f, 0.15f, 1.01f);
        stripe.GetComponent<Renderer>().material = accentMat;
        stripe.isStatic = true;
        Destroy(stripe.GetComponent<Collider>());
    }

    void CreateDecorations()
    {
        // Trees - varied sizes and positions (scaled up and more spread out)
        Vector3[] treePositions = new Vector3[]
        {
            new Vector3(-216, 0, 216), new Vector3(-180, 0, 225), new Vector3(180, 0, 225), new Vector3(216, 0, 216),
            new Vector3(-225, 0, -195), new Vector3(-195, 0, -225), new Vector3(195, 0, -225), new Vector3(225, 0, -195),
            new Vector3(-225, 0, 60), new Vector3(-225, 0, -60), new Vector3(225, 0, 60), new Vector3(225, 0, -60),
            new Vector3(15, 0, 15), new Vector3(-24, 0, -9), new Vector3(36, 0, -24), new Vector3(-15, 0, 30),
            new Vector3(-225, 0, 120), new Vector3(-225, 0, -120), new Vector3(225, 0, 120), new Vector3(225, 0, -120),
            new Vector3(0, 0, 230), new Vector3(230, 0, 0), new Vector3(-230, 0, 0), new Vector3(0, 0, -230),
        };

        foreach (var pos in treePositions)
        {
            CreateTree(pos, Random.Range(5f, 9f));
        }

        // Center island
        GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        island.transform.position = new Vector3(0, 0.1f, 0);
        island.transform.localScale = new Vector3(66, 0.1f, 66);

        Material islandMat = new Material(Shader.Find("Standard"));
        islandMat.color = new Color(0.2f, 0.35f, 0.15f);
        islandMat.SetFloat("_Glossiness", 0.1f);
        island.GetComponent<Renderer>().material = islandMat;
        island.isStatic = true;
        Destroy(island.GetComponent<Collider>());

        // Grandstands
        CreateGrandstand(new Vector3(0, 0, -270), 0);
        CreateGrandstand(new Vector3(270, 0, 0), 90);

        // Light poles around track
        CreateLightPoles();

        // Racing flags
        CreateRacingFlags();

        // Tire barriers at corners
        CreateTireBarriers();

        // Advertising boards
        CreateAdBoards();
    }

    void CreateLightPoles()
    {
        Vector3[] polePositions = new Vector3[]
        {
            new Vector3(-14, 0, 210), new Vector3(14, 0, 210),
            new Vector3(-14, 0, 140), new Vector3(14, 0, 140),
            new Vector3(-14, 0, -210), new Vector3(14, 0, -210),
            new Vector3(-14, 0, -140), new Vector3(14, 0, -140),
            new Vector3(210, 0, -14), new Vector3(210, 0, 14),
            new Vector3(140, 0, -14), new Vector3(140, 0, 14),
            new Vector3(-210, 0, -14), new Vector3(-210, 0, 14),
            new Vector3(-140, 0, -14), new Vector3(-140, 0, 14),
        };

        Material poleMat = new Material(Shader.Find("Standard"));
        poleMat.color = new Color(0.4f, 0.4f, 0.42f);
        poleMat.SetFloat("_Metallic", 0.8f);
        poleMat.SetFloat("_Glossiness", 0.6f);

        foreach (var pos in polePositions)
        {
            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.position = pos + new Vector3(0, 8, 0);
            pole.transform.localScale = new Vector3(0.3f, 8f, 0.3f);
            pole.GetComponent<Renderer>().material = poleMat;
            pole.isStatic = true;
            Destroy(pole.GetComponent<Collider>());

            // Light housing
            GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.transform.position = pos + new Vector3(0, 15.5f, 0);
            housing.transform.localScale = new Vector3(3f, 0.6f, 1.5f);
            housing.GetComponent<Renderer>().material = poleMat;
            housing.isStatic = true;
            Destroy(housing.GetComponent<Collider>());

            // Light panel (emissive)
            GameObject lightPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lightPanel.transform.position = pos + new Vector3(0, 15.2f, 0);
            lightPanel.transform.localScale = new Vector3(2.8f, 0.2f, 1.3f);

            Material lightMat = new Material(Shader.Find("Standard"));
            lightMat.color = new Color(1f, 0.98f, 0.9f);
            lightMat.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.85f) * 0.5f);
            lightMat.EnableKeyword("_EMISSION");
            lightPanel.GetComponent<Renderer>().material = lightMat;
            lightPanel.isStatic = true;
            Destroy(lightPanel.GetComponent<Collider>());
        }
    }

    void CreateRacingFlags()
    {
        Vector3[] flagPositions = new Vector3[]
        {
            new Vector3(-10, 0, -84), new Vector3(10, 0, -84), // Start line
            new Vector3(-30, 0, 150), new Vector3(30, 0, 150),
            new Vector3(150, 0, 30), new Vector3(150, 0, -30),
            new Vector3(-150, 0, 30), new Vector3(-150, 0, -30),
        };

        for (int i = 0; i < flagPositions.Length; i++)
        {
            CreateFlag(flagPositions[i], i % 3);
        }
    }

    void CreateFlag(Vector3 pos, int colorType)
    {
        // Pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.transform.position = pos + new Vector3(0, 4, 0);
        pole.transform.localScale = new Vector3(0.12f, 4f, 0.12f);

        Material poleMat = new Material(Shader.Find("Standard"));
        poleMat.color = new Color(0.6f, 0.6f, 0.62f);
        poleMat.SetFloat("_Metallic", 0.7f);
        pole.GetComponent<Renderer>().material = poleMat;
        pole.isStatic = true;
        Destroy(pole.GetComponent<Collider>());

        // Flag
        GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flag.transform.position = pos + new Vector3(0.8f, 7.2f, 0);
        flag.transform.localScale = new Vector3(1.5f, 1f, 0.05f);
        flag.transform.rotation = Quaternion.Euler(0, 0, -8);

        Color flagColor = colorType switch
        {
            0 => accentColor,
            1 => new Color(0.2f, 0.2f, 0.8f),
            _ => new Color(0.9f, 0.9f, 0.9f)
        };

        Material flagMat = new Material(Shader.Find("Standard"));
        flagMat.color = flagColor;
        flagMat.SetFloat("_Glossiness", 0.3f);
        flag.GetComponent<Renderer>().material = flagMat;
        flag.isStatic = true;
        Destroy(flag.GetComponent<Collider>());
    }

    void CreateTireBarriers()
    {
        // Inner corner tire stacks
        Vector3[] tirePositions = new Vector3[]
        {
            new Vector3(75, 0, 75), new Vector3(-75, 0, 75),
            new Vector3(-75, 0, -75), new Vector3(75, 0, -75),
        };

        Material tireMat = new Material(Shader.Find("Standard"));
        tireMat.color = new Color(0.12f, 0.12f, 0.12f);
        tireMat.SetFloat("_Glossiness", 0.15f);

        Material bandMat = new Material(Shader.Find("Standard"));
        bandMat.color = accentColor;

        foreach (var pos in tirePositions)
        {
            for (int stack = 0; stack < 3; stack++)
            {
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f + stack * 45f;
                    Vector3 offset = new Vector3(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * 1.2f,
                        stack * 0.5f + 0.25f,
                        Mathf.Sin(angle * Mathf.Deg2Rad) * 1.2f
                    );

                    GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    tire.transform.position = pos + offset;
                    tire.transform.localScale = new Vector3(0.7f, 0.25f, 0.7f);
                    tire.GetComponent<Renderer>().material = (stack == 1) ? bandMat : tireMat;
                    tire.isStatic = true;
                    Destroy(tire.GetComponent<Collider>());
                }
            }
        }
    }

    void CreateAdBoards()
    {
        Vector3[] boardPositions = new Vector3[]
        {
            new Vector3(0, 0, 231),
            new Vector3(231, 0, 0),
            new Vector3(-231, 0, 0),
            new Vector3(0, 0, -231),
        };

        float[] rotations = { 0, 90, 90, 0 };

        string[] adColors = { "SPEED", "RACING", "TURBO", "NITRO" };

        for (int i = 0; i < boardPositions.Length; i++)
        {
            // Board back
            GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.position = boardPositions[i] + new Vector3(0, 3.5f, 0);
            board.transform.localScale = new Vector3(40f, 6f, 0.6f);
            board.transform.rotation = Quaternion.Euler(0, rotations[i], 0);

            Material boardMat = new Material(Shader.Find("Standard"));
            boardMat.color = new Color(0.15f, 0.15f, 0.18f);
            board.GetComponent<Renderer>().material = boardMat;
            board.isStatic = true;
            Destroy(board.GetComponent<Collider>());

            // Colored stripe
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.position = boardPositions[i] + new Vector3(0, 3.5f, 0);
            stripe.transform.localScale = new Vector3(38f, 0.7f, 0.65f);
            stripe.transform.rotation = Quaternion.Euler(0, rotations[i], 0);

            Material stripeMat = new Material(Shader.Find("Standard"));
            stripeMat.color = accentColor;
            stripeMat.SetColor("_EmissionColor", accentColor * 0.3f);
            stripeMat.EnableKeyword("_EMISSION");
            stripe.GetComponent<Renderer>().material = stripeMat;
            stripe.isStatic = true;
            Destroy(stripe.GetComponent<Collider>());
        }
    }

    void CreateTree(Vector3 pos, float scale)
    {
        // Trunk
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.position = pos + new Vector3(0, scale * 0.35f, 0);
        trunk.transform.localScale = new Vector3(scale * 0.12f, scale * 0.35f, scale * 0.12f);

        Material trunkMat = new Material(Shader.Find("Standard"));
        trunkMat.color = new Color(0.35f, 0.25f, 0.15f);
        trunkMat.SetFloat("_Glossiness", 0.15f);
        trunk.GetComponent<Renderer>().material = trunkMat;
        trunk.isStatic = true;
        Destroy(trunk.GetComponent<Collider>());

        // Foliage - multiple spheres for fuller look
        Color leafColor = new Color(0.18f + Random.Range(0, 0.08f), 0.4f + Random.Range(0, 0.15f), 0.12f);
        Material leafMat = new Material(Shader.Find("Standard"));
        leafMat.color = leafColor;
        leafMat.SetFloat("_Glossiness", 0.15f);

        for (int i = 0; i < 3; i++)
        {
            GameObject foliage = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Vector3 offset = new Vector3(
                Random.Range(-0.3f, 0.3f) * scale * 0.3f,
                scale * (0.85f + i * 0.2f),
                Random.Range(-0.3f, 0.3f) * scale * 0.3f
            );
            foliage.transform.position = pos + offset;
            float foliageScale = scale * (0.7f - i * 0.15f);
            foliage.transform.localScale = new Vector3(foliageScale, foliageScale * 0.8f, foliageScale);
            foliage.GetComponent<Renderer>().material = leafMat;
            foliage.isStatic = true;
            Destroy(foliage.GetComponent<Collider>());
        }
    }

    void CreateGrandstand(Vector3 pos, float angle)
    {
        // Main structure
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.transform.position = pos + new Vector3(0, 6, 0);
        stand.transform.localScale = new Vector3(80, 12, 18);
        stand.transform.rotation = Quaternion.Euler(0, angle, 0);

        Material standMat = new Material(Shader.Find("Standard"));
        standMat.color = new Color(0.35f, 0.35f, 0.4f);
        standMat.SetFloat("_Glossiness", 0.2f);
        stand.GetComponent<Renderer>().material = standMat;
        stand.isStatic = true;
        Destroy(stand.GetComponent<Collider>());

        // Roof
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.position = pos + new Vector3(0, 14f, 0);
        roof.transform.localScale = new Vector3(86, 2f, 22);
        roof.transform.rotation = Quaternion.Euler(0, angle, 0);

        Material roofMat = new Material(Shader.Find("Standard"));
        roofMat.color = new Color(0.2f, 0.2f, 0.22f);
        roofMat.SetFloat("_Glossiness", 0.4f);
        roof.GetComponent<Renderer>().material = roofMat;
        roof.isStatic = true;
        Destroy(roof.GetComponent<Collider>());

        // Orange accent band
        GameObject band = GameObject.CreatePrimitive(PrimitiveType.Cube);
        band.transform.position = pos + new Vector3(0, 11.5f, 0);
        band.transform.localScale = new Vector3(81, 0.6f, 18.5f);
        band.transform.rotation = Quaternion.Euler(0, angle, 0);
        band.GetComponent<Renderer>().material = CreateMaterial(accentColor);
        band.isStatic = true;
        Destroy(band.GetComponent<Collider>());
    }

    void CreateCheckpoints()
    {
        // Start/Finish line
        GameObject finishLine = new GameObject("StartFinishLine");
        finishLine.transform.position = new Vector3(0, 1, -180);
        BoxCollider finishTrigger = finishLine.AddComponent<BoxCollider>();
        finishTrigger.size = new Vector3(24, 3, 3);
        finishTrigger.isTrigger = true;
        finishLine.AddComponent<CheckpointTrigger>().isFinishLine = true;

        // Checkpoint 1 - North straight
        CreateCheckpoint(new Vector3(0, 1, 180), new Vector3(24, 3, 3), 0, 1);

        // Checkpoint 2 - East side
        CreateCheckpoint(new Vector3(180, 1, 0), new Vector3(3, 3, 24), 0, 2);

        // Checkpoint 3 - West side
        CreateCheckpoint(new Vector3(-180, 1, 0), new Vector3(3, 3, 24), 0, 3);

        // Update LapSystem checkpoint count
        if (LapSystem.Instance != null)
        {
            LapSystem.Instance.totalCheckpoints = 4; // finish + 3 checkpoints
        }
    }

    void CreateCheckpoint(Vector3 pos, Vector3 size, float angle, int index)
    {
        GameObject checkpoint = new GameObject($"Checkpoint_{index}");
        checkpoint.transform.position = pos;
        checkpoint.transform.rotation = Quaternion.Euler(0, angle, 0);
        BoxCollider trigger = checkpoint.AddComponent<BoxCollider>();
        trigger.size = size;
        trigger.isTrigger = true;
        CheckpointTrigger ct = checkpoint.AddComponent<CheckpointTrigger>();
        ct.isFinishLine = false;
        ct.checkpointIndex = index;
    }

    void CreatePuddles()
    {
        // Reflective puddles on the track - Y=0.08 above track markings (Y=0.07)
        Vector3[] puddlePositions = new Vector3[]
        {
            new Vector3(-6, 0.08f, 135),
            new Vector3(4, 0.08f, 165),
            new Vector3(180, 0.08f, 6),
            new Vector3(165, 0.08f, -10),
            new Vector3(-5, 0.08f, -165),
            new Vector3(-180, 0.08f, 4),
            new Vector3(-165, 0.08f, -6),
            new Vector3(6, 0.08f, 200),
            new Vector3(-6, 0.08f, -200),
            new Vector3(200, 0.08f, -6),
            new Vector3(-200, 0.08f, 6),
        };

        Material puddleMat = new Material(Shader.Find("Standard"));
        puddleMat.color = new Color(0.15f, 0.18f, 0.22f, 0.85f);
        puddleMat.SetFloat("_Metallic", 0.95f);
        puddleMat.SetFloat("_Glossiness", 0.98f);
        puddleMat.SetFloat("_Mode", 3); // Transparent
        puddleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        puddleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        puddleMat.SetInt("_ZWrite", 1);
        puddleMat.EnableKeyword("_ALPHABLEND_ON");
        puddleMat.renderQueue = 2450;

        foreach (var pos in puddlePositions)
        {
            float sizeX = Random.Range(3f, 6f);
            float sizeZ = Random.Range(2f, 4f);

            GameObject puddle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            puddle.name = "Puddle";
            puddle.transform.position = pos;
            puddle.transform.localScale = new Vector3(sizeX, 0.01f, sizeZ);
            puddle.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
            puddle.GetComponent<Renderer>().material = puddleMat;
            puddle.isStatic = true;
            Destroy(puddle.GetComponent<Collider>());
        }
    }

    void CreateRubberMarks()
    {
        // Pre-baked rubber marks on corners (old tire marks)
        Material rubberMat = new Material(Shader.Find("Sprites/Default"));
        rubberMat.color = new Color(0.08f, 0.08f, 0.08f, 0.35f);

        // Corner marks - Y=0.06 above track surface (Y=0.03)
        Vector3[] corners = new Vector3[]
        {
            new Vector3(105, 0.06f, 105),
            new Vector3(-105, 0.06f, 105),
            new Vector3(-105, 0.06f, -105),
            new Vector3(105, 0.06f, -105),
        };

        foreach (var corner in corners)
        {
            // Create several rubber streaks per corner
            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.Atan2(corner.z, corner.x) * Mathf.Rad2Deg + 45 + Random.Range(-30f, 30f);
                float radius = 36f + Random.Range(-9f, 9f);

                Vector3 pos = corner + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    0,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius
                );

                GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mark.name = "RubberMark";
                mark.transform.position = pos;
                mark.transform.localScale = new Vector3(0.3f, 0.005f, Random.Range(8f, 16f));
                mark.transform.rotation = Quaternion.Euler(0, angle + 90 + Random.Range(-10f, 10f), 0);
                mark.GetComponent<Renderer>().material = rubberMat;
                mark.isStatic = true;
                Destroy(mark.GetComponent<Collider>());
            }
        }
    }

    Material CreateMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }
}
