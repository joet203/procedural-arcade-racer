using UnityEngine;

public class CarEffects : MonoBehaviour
{
    private CarController car;
    private TrailRenderer[] tireTrails;
    private ParticleSystem driftSmoke;
    private ParticleSystem boostFlames;
    private ParticleSystem boostGlow;
    private ParticleSystem dustTrail;
    private ParticleSystem sparkParticles;
    private ParticleSystem heatShimmer;
    private Light boostLight;
    private Light[] brakeLights;
    private Light[] underglowLights;
    private GameObject[] underglowStrips;
    private Transform[] wheels;
    private float wheelRotation;

    // Smooth color transitions
    private Color currentTrailColor;
    private float trailColorVelocityR, trailColorVelocityG, trailColorVelocityB;

    [Header("Trail Colors")]
    public Color normalTrailColor = new Color(0.08f, 0.08f, 0.08f, 0.4f);
    public Color driftTrailColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
    public Color boostTrailColor = new Color(0.95f, 0.5f, 0.2f, 0.5f);

    void Start()
    {
        car = GetComponent<CarController>();
        currentTrailColor = normalTrailColor;

        CreateTireTrails();
        CreateDriftSmoke();
        CreateBoostEffects();
        CreateBoostLight();
        CreateBrakeLights();
        CreateDustTrail();
        CreateSparkParticles();
        CreateHeatShimmer();
        CreateUnderglow();
        FindWheels();
    }

    void Update()
    {
        UpdateWheelRotation();
        UpdateTrails();
        UpdateSmoke();
        UpdateBoostEffects();
        UpdateBrakeLights();
        UpdateDustTrail();
        UpdateHeatShimmer();
        UpdateUnderglow();
    }

    void FindWheels()
    {
        Transform body = transform.Find("Body");
        if (body == null) return;

        wheels = new Transform[4];
        string[] wheelNames = { "Wheel_FL", "Wheel_FR", "Wheel_BL", "Wheel_BR" };

        for (int i = 0; i < wheelNames.Length; i++)
        {
            Transform wheel = body.Find(wheelNames[i]);
            if (wheel != null)
            {
                wheels[i] = wheel;
            }
        }
    }

    void UpdateWheelRotation()
    {
        if (wheels == null) return;

        float rotationSpeed = car.CurrentSpeed * 180f;
        wheelRotation += rotationSpeed * Time.deltaTime;

        foreach (var wheel in wheels)
        {
            if (wheel != null)
            {
                // Rotate the wheel around X axis (forward rotation)
                wheel.localRotation = Quaternion.Euler(wheelRotation, 0, 0);
            }
        }
    }

    void CreateTireTrails()
    {
        tireTrails = new TrailRenderer[4];

        Vector3[] wheelPositions = new Vector3[]
        {
            new Vector3(-0.8f, 0.02f, -1.25f),
            new Vector3(0.8f, 0.02f, -1.25f),
            new Vector3(-0.8f, 0.02f, 1.35f),
            new Vector3(0.8f, 0.02f, 1.35f),
        };

        Material trailMat = new Material(Shader.Find("Sprites/Default"));

        for (int i = 0; i < 4; i++)
        {
            GameObject trailObj = new GameObject($"TireTrail_{i}");
            trailObj.transform.SetParent(transform);
            trailObj.transform.localPosition = wheelPositions[i];

            TrailRenderer tr = trailObj.AddComponent<TrailRenderer>();
            tr.time = 4f;
            tr.startWidth = 0.22f;
            tr.endWidth = 0.15f;
            tr.material = trailMat;
            tr.startColor = normalTrailColor;
            tr.endColor = new Color(0, 0, 0, 0);
            tr.minVertexDistance = 0.2f;
            tr.emitting = false;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            tr.numCornerVertices = 3;
            tr.numCapVertices = 3;

            tireTrails[i] = tr;
        }
    }

    void CreateDriftSmoke()
    {
        GameObject smokeObj = new GameObject("DriftSmoke");
        smokeObj.transform.SetParent(transform);
        smokeObj.transform.localPosition = new Vector3(0, 0.1f, -1.3f);

        driftSmoke = smokeObj.AddComponent<ParticleSystem>();

        var main = driftSmoke.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSize3D = true;
        main.startSizeXMultiplier = 1f;
        main.startSizeYMultiplier = 1f;
        main.startSizeZMultiplier = 1f;
        main.startColor = new Color(0.92f, 0.92f, 0.92f, 0.06f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 800;
        main.gravityModifier = -0.015f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

        var emission = driftSmoke.emission;
        emission.rateOverTime = 0;

        var shape = driftSmoke.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.25f;
        shape.radiusThickness = 0.8f;

        var velocityOverLifetime = driftSmoke.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        // All axes must be same mode (TwoConstants)
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var rotationOverLifetime = driftSmoke.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        // All axes must be same mode (TwoConstants)
        rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);

        var sizeOverLifetime = driftSmoke.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(new Keyframe(0f, 0.2f, 0f, 2f));
        sizeCurve.AddKey(new Keyframe(0.15f, 0.7f, 1.5f, 1f));
        sizeCurve.AddKey(new Keyframe(0.5f, 1.5f, 0.5f, 0.3f));
        sizeCurve.AddKey(new Keyframe(1f, 3.5f, 1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = driftSmoke.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.98f, 0.98f, 0.98f), 0f),
                new GradientColorKey(new Color(0.92f, 0.92f, 0.92f), 0.3f),
                new GradientColorKey(new Color(0.85f, 0.85f, 0.87f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.08f, 0.05f),
                new GradientAlphaKey(0.05f, 0.3f),
                new GradientAlphaKey(0.02f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Perlin noise for organic turbulent movement
        var noise = driftSmoke.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.4f;
        noise.damping = true;
        noise.octaveCount = 2;
        noise.octaveMultiplier = 0.5f;
        noise.octaveScale = 2f;

        var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetFloat("_Mode", 2);
        renderer.material.SetFloat("_SoftParticlesEnabled", 1f);
        renderer.material.SetFloat("_SoftParticlesNearFadeDistance", 0.1f);
        renderer.material.SetFloat("_SoftParticlesFarFadeDistance", 1f);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0.01f;
        renderer.maxParticleSize = 2f;

        driftSmoke.Stop();
    }

    void CreateBoostEffects()
    {
        // Dual exhaust flames
        for (int i = 0; i < 2; i++)
        {
            GameObject flameObj = new GameObject($"BoostFlame_{i}");
            flameObj.transform.SetParent(transform);
            flameObj.transform.localPosition = new Vector3(i == 0 ? -0.35f : 0.35f, 0.28f, -2.15f);
            flameObj.transform.localRotation = Quaternion.Euler(0, 180, 0);

            ParticleSystem flames = flameObj.AddComponent<ParticleSystem>();

            var main = flames.main;
            main.startLifetime = 0.12f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(18f, 28f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            main.startColor = new Color(1f, 0.8f, 0.4f, 1f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = flames.emission;
            emission.rateOverTime = 80;

            var shape = flames.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 6f;
            shape.radius = 0.06f;

            var colorOverLifetime = flames.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 1f, 0.8f), 0f),
                    new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0.4f),
                    new GradientColorKey(new Color(0.9f, 0.3f, 0.1f), 0.8f),
                    new GradientColorKey(new Color(0.4f, 0.15f, 0.05f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = grad;

            var renderer = flameObj.GetComponent<ParticleSystemRenderer>();
            Material flameMat = new Material(Shader.Find("Particles/Standard Unlit"));
            flameMat.SetInt("_Cull", 0); // Render both sides
            renderer.material = flameMat;

            flames.Stop();

            if (i == 0) boostFlames = flames;
        }

        // Subtle glow particles
        GameObject glowObj = new GameObject("BoostGlow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = new Vector3(0, 0.3f, -2.1f);

        boostGlow = glowObj.AddComponent<ParticleSystem>();

        var glowMain = boostGlow.main;
        glowMain.startLifetime = 0.3f;
        glowMain.startSpeed = 0.5f;
        glowMain.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        glowMain.startColor = new Color(1f, 0.6f, 0.3f, 0.3f);
        glowMain.simulationSpace = ParticleSystemSimulationSpace.World;

        var glowEmission = boostGlow.emission;
        glowEmission.rateOverTime = 20;

        var glowShape = boostGlow.shape;
        glowShape.shapeType = ParticleSystemShapeType.Sphere;
        glowShape.radius = 0.3f;

        var glowSize = boostGlow.sizeOverLifetime;
        glowSize.enabled = true;
        glowSize.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        var glowColor = boostGlow.colorOverLifetime;
        glowColor.enabled = true;
        Gradient glowGrad = new Gradient();
        glowGrad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.7f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.2f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        glowColor.color = glowGrad;

        var glowRenderer = glowObj.GetComponent<ParticleSystemRenderer>();
        glowRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        boostGlow.Stop();
    }

    void CreateBoostLight()
    {
        GameObject lightObj = new GameObject("BoostLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, 0.35f, -2.2f);

        boostLight = lightObj.AddComponent<Light>();
        boostLight.type = LightType.Point;
        boostLight.color = new Color(1f, 0.6f, 0.3f);
        boostLight.intensity = 3f;
        boostLight.range = 6f;
        boostLight.enabled = false;
    }

    void CreateBrakeLights()
    {
        brakeLights = new Light[2];

        for (int i = 0; i < 2; i++)
        {
            GameObject lightObj = new GameObject($"BrakeLight_{i}");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = new Vector3(i == 0 ? -0.65f : 0.65f, 0.42f, -2.15f);

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.15f, 0.1f);
            light.intensity = 1.5f;
            light.range = 2.5f;
            light.enabled = false;

            brakeLights[i] = light;
        }
    }

    void CreateDustTrail()
    {
        GameObject dustObj = new GameObject("DustTrail");
        dustObj.transform.SetParent(transform);
        dustObj.transform.localPosition = new Vector3(0, 0.05f, -1.8f);

        dustTrail = dustObj.AddComponent<ParticleSystem>();

        var main = dustTrail.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
        main.startColor = new Color(0.55f, 0.5f, 0.42f, 0.12f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;
        main.gravityModifier = -0.01f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

        var emission = dustTrail.emission;
        emission.rateOverTime = 0;

        var shape = dustTrail.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.6f;
        shape.radiusThickness = 1f;
        shape.arc = 180f;
        shape.rotation = new Vector3(-90f, 0f, 0f);

        var velocityOverLifetime = dustTrail.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        // All axes must be same mode (TwoConstants)
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        var rotationOverLifetime = dustTrail.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        // All axes must be same mode (TwoConstants)
        rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(0f, 0f);
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

        var sizeOverLifetime = dustTrail.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(new Keyframe(0f, 0.3f, 0f, 2f));
        sizeCurve.AddKey(new Keyframe(0.2f, 0.8f, 1f, 0.5f));
        sizeCurve.AddKey(new Keyframe(0.6f, 1.5f, 0.4f, 0.2f));
        sizeCurve.AddKey(new Keyframe(1f, 2.5f, 0.5f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = dustTrail.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.6f, 0.55f, 0.45f), 0f),
                new GradientColorKey(new Color(0.55f, 0.5f, 0.42f), 0.4f),
                new GradientColorKey(new Color(0.5f, 0.48f, 0.42f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.1f, 0.05f),
                new GradientAlphaKey(0.06f, 0.4f),
                new GradientAlphaKey(0.02f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        // Noise for organic dispersal
        var noise = dustTrail.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;
        noise.octaveCount = 2;

        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;

        dustTrail.Stop();
    }

    void CreateSparkParticles()
    {
        GameObject sparkObj = new GameObject("Sparks");
        sparkObj.transform.SetParent(transform);
        sparkObj.transform.localPosition = new Vector3(0, 0.1f, -2f);

        sparkParticles = sparkObj.AddComponent<ParticleSystem>();

        var main = sparkParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 15f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = new Color(1f, 0.8f, 0.4f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;
        main.gravityModifier = 2f;

        var emission = sparkParticles.emission;
        emission.rateOverTime = 0;

        var shape = sparkParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.1f;

        var colorOverLifetime = sparkParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.8f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.2f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.3f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.5f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        Material sparkMat = new Material(Shader.Find("Particles/Standard Unlit"));
        sparkMat.SetColor("_EmissionColor", new Color(1f, 0.7f, 0.3f) * 0.8f);  // Reduced from 2 to prevent blowout
        renderer.material = sparkMat;

        sparkParticles.Stop();
    }

    void CreateHeatShimmer()
    {
        GameObject shimmerObj = new GameObject("HeatShimmer");
        shimmerObj.transform.SetParent(transform);
        shimmerObj.transform.localPosition = new Vector3(0, 0.4f, -2.3f);

        heatShimmer = shimmerObj.AddComponent<ParticleSystem>();

        var main = heatShimmer.main;
        main.startLifetime = 1.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
        main.startColor = new Color(1f, 1f, 1f, 0.03f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.gravityModifier = -0.3f;

        var emission = heatShimmer.emission;
        emission.rateOverTime = 0;

        var shape = heatShimmer.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(0.6f, 0.1f, 0.3f);

        var noise = heatShimmer.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
        noise.frequency = 2f;
        noise.scrollSpeed = 1f;

        var sizeOverLifetime = heatShimmer.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1.2f);
        sizeCurve.AddKey(1f, 0.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = heatShimmer.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.04f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var renderer = shimmerObj.GetComponent<ParticleSystemRenderer>();
        Material shimmerMat = new Material(Shader.Find("Particles/Standard Unlit"));
        shimmerMat.SetFloat("_Mode", 1); // Additive-ish
        renderer.material = shimmerMat;
        renderer.sortingOrder = 100;

        heatShimmer.Stop();
    }

    void UpdateHeatShimmer()
    {
        if (heatShimmer == null) return;

        var emission = heatShimmer.emission;

        // Heat shimmer when moving at speed
        if (car.SpeedPercent > 0.2f && car.IsGrounded)
        {
            if (!heatShimmer.isPlaying)
                heatShimmer.Play();

            float rate = 20f * car.SpeedPercent;
            if (car.IsBoosting)
                rate *= 2f;

            emission.rateOverTime = rate;
        }
        else
        {
            emission.rateOverTime = 0;
        }
    }

    void CreateUnderglow()
    {
        Color glowColor = new Color(0f, 0.6f, 0.8f); // Subtle cyan underglow

        underglowLights = new Light[4];
        underglowStrips = new GameObject[4];

        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, 0.06f, 1.5f),   // Front
            new Vector3(0, 0.06f, -1.5f),  // Rear
            new Vector3(-0.9f, 0.06f, 0),  // Left
            new Vector3(0.9f, 0.06f, 0),   // Right
        };

        Vector3[] scales = new Vector3[]
        {
            new Vector3(1.4f, 0.015f, 0.1f),
            new Vector3(1.4f, 0.015f, 0.1f),
            new Vector3(0.1f, 0.015f, 3.2f),
            new Vector3(0.1f, 0.015f, 3.2f),
        };

        for (int i = 0; i < 4; i++)
        {
            // Light strip
            GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = $"UnderglowStrip_{i}";
            strip.transform.SetParent(transform);
            strip.transform.localPosition = positions[i];
            strip.transform.localScale = scales[i];
            Destroy(strip.GetComponent<Collider>());

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = glowColor;
            mat.SetColor("_EmissionColor", glowColor * 1.5f);
            mat.EnableKeyword("_EMISSION");
            strip.GetComponent<Renderer>().material = mat;
            strip.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            underglowStrips[i] = strip;

            // Point light - subtle
            GameObject lightObj = new GameObject($"UnderglowLight_{i}");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = positions[i] - Vector3.up * 0.03f;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glowColor;
            light.intensity = 1.5f;
            light.range = 3f;

            underglowLights[i] = light;
        }
    }

    void UpdateUnderglow()
    {
        if (underglowLights == null) return;

        // Pulse effect based on speed
        float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.15f;
        float speedBoost = 1f + car.SpeedPercent * 0.5f;

        Color baseColor = new Color(0f, 0.8f, 1f);

        // Change color when boosting
        if (car.IsBoosting)
        {
            baseColor = new Color(1f, 0.4f, 0f); // Orange when boosting
            pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.3f;
        }
        else if (car.IsDrifting)
        {
            baseColor = new Color(1f, 0f, 0.8f); // Pink when drifting
        }

        for (int i = 0; i < underglowLights.Length; i++)
        {
            underglowLights[i].color = baseColor;
            underglowLights[i].intensity = 1.2f * pulse * speedBoost;

            if (underglowStrips[i] != null)
            {
                Material mat = underglowStrips[i].GetComponent<Renderer>().material;
                mat.color = baseColor;
                mat.SetColor("_EmissionColor", baseColor * 1.2f * pulse);
            }
        }
    }

    void UpdateDustTrail()
    {
        var emission = dustTrail.emission;

        if (car.IsGrounded && car.SpeedPercent > 0.3f && !car.IsDrifting)
        {
            if (!dustTrail.isPlaying)
                dustTrail.Play();

            emission.rateOverTime = 15f * car.SpeedPercent;
        }
        else if (car.IsDrifting && car.IsGrounded)
        {
            if (!dustTrail.isPlaying)
                dustTrail.Play();

            emission.rateOverTime = 30f * car.SpeedPercent;
        }
        else
        {
            emission.rateOverTime = 0;
        }
    }

    public void TriggerSparks()
    {
        if (sparkParticles != null)
        {
            sparkParticles.transform.localPosition = new Vector3(
                Random.Range(-0.8f, 0.8f),
                0.1f,
                Random.Range(-1.8f, -2.2f)
            );
            sparkParticles.Emit(Random.Range(10, 25));
        }
    }

    void UpdateSkidMarks()
    {
        if (SkidMarks.Instance == null) return;

        Vector3[] wheelOffsets = new Vector3[]
        {
            new Vector3(-0.8f, 0.02f, -1.25f),
            new Vector3(0.8f, 0.02f, -1.25f),
        };

        float intensity = 0f;
        if (car.IsDrifting && car.IsGrounded)
        {
            intensity = car.SpeedPercent * (0.5f + Mathf.Abs(car.SteerInput) * 0.5f);
        }

        for (int i = 0; i < wheelOffsets.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(wheelOffsets[i]);

            // Raycast to find ground
            if (Physics.Raycast(worldPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1f))
            {
                SkidMarks.Instance.AddSkidMark(
                    GetInstanceID() * 10 + i,
                    hit.point,
                    transform.forward,
                    hit.normal,
                    intensity
                );
            }
        }
    }

    void UpdateTrails()
    {
        UpdateSkidMarks();

        bool shouldEmit = Mathf.Abs(car.CurrentSpeed) > 15f || car.IsDrifting;
        Color targetColor = normalTrailColor;

        if (car.IsBoosting)
        {
            targetColor = boostTrailColor;
            shouldEmit = true;
        }
        else if (car.IsDrifting)
        {
            targetColor = driftTrailColor;
        }

        // Smooth color transition
        currentTrailColor.r = Mathf.SmoothDamp(currentTrailColor.r, targetColor.r, ref trailColorVelocityR, 0.2f);
        currentTrailColor.g = Mathf.SmoothDamp(currentTrailColor.g, targetColor.g, ref trailColorVelocityG, 0.2f);
        currentTrailColor.b = Mathf.SmoothDamp(currentTrailColor.b, targetColor.b, ref trailColorVelocityB, 0.2f);
        currentTrailColor.a = targetColor.a;

        for (int i = 0; i < tireTrails.Length; i++)
        {
            // Rear tires always, front only when drifting
            bool emit = shouldEmit && car.IsGrounded && (i < 2 || car.IsDrifting);
            tireTrails[i].emitting = emit;
            tireTrails[i].startColor = currentTrailColor;
        }
    }

    void UpdateSmoke()
    {
        var emission = driftSmoke.emission;

        if (car.IsDrifting && car.IsGrounded && !car.IsBoosting)
        {
            if (!driftSmoke.isPlaying)
                driftSmoke.Play();

            emission.rateOverTime = 35f * car.SpeedPercent;
        }
        else
        {
            emission.rateOverTime = 0;
        }
    }

    void UpdateBoostEffects()
    {
        ParticleSystem[] allFlames = GetComponentsInChildren<ParticleSystem>();

        foreach (var ps in allFlames)
        {
            if (ps.gameObject.name.StartsWith("BoostFlame"))
            {
                if (car.IsBoosting)
                {
                    if (!ps.isPlaying) ps.Play();
                }
                else
                {
                    if (ps.isPlaying) ps.Stop();
                }
            }
        }

        if (car.IsBoosting)
        {
            if (!boostGlow.isPlaying) boostGlow.Play();
        }
        else
        {
            if (boostGlow.isPlaying) boostGlow.Stop();
        }

        boostLight.enabled = car.IsBoosting;
        if (car.IsBoosting)
        {
            // Subtle pulsing
            boostLight.intensity = 2.5f + Mathf.Sin(Time.time * 20f) * 0.5f;
        }
    }

    void UpdateBrakeLights()
    {
        bool braking = Input.GetAxisRaw("Vertical") < -0.1f && car.CurrentSpeed > 1f;

        foreach (var light in brakeLights)
        {
            if (light != null)
                light.enabled = braking;
        }
    }

    public void OnLanding(float airTime)
    {
        // Could add landing dust here if desired
    }
}
