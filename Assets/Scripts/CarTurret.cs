using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CarTurret : MonoBehaviour
{
    [Header("Turret Settings")]
    public float rotationSpeed = 180f;
    public float fireRate = 8f;
    public float projectileSpeed = 80f;
    public float projectileLifetime = 3f;
    public int maxAmmo = 100;

    [Header("Bazooka Settings")]
    public float bazookaFireRate = 1.5f;      // Slower than regular
    public float bazookaSpeed = 55f;          // Faster than before
    public float bazookaDamage = 105f;        // Triple damage
    public int bazookaMaxAmmo = 8;

    [Header("Visuals")]
    public Color turretColor = new Color(0.15f, 0.15f, 0.18f);
    public Color muzzleFlashColor = new Color(1f, 0.8f, 0.3f);
    public Color projectileColor = new Color(1f, 0.6f, 0.1f);
    public Color bazookaColor = new Color(1f, 0.3f, 0.1f);

    [Header("Auto-Center Settings")]
    public float autoCenterDelay = 2.5f;           // Seconds before auto-centering starts
    public float autoCenterSpeed = 35f;            // Degrees per second when auto-centering

    private Transform turretBase;
    private Transform turretBarrel;
    private Transform muzzle;
    private float fireTimer;
    private float bazookaTimer;
    private int currentAmmo;
    private int currentBazookaAmmo;
    private CarController car;
    private List<GameObject> activeProjectiles = new List<GameObject>();
    private AudioSource audioSource;
    private float lastTurretInputTime;            // Tracks when turret was last moved by player

    void Start()
    {
        car = GetComponent<CarController>();
        currentAmmo = maxAmmo;
        currentBazookaAmmo = bazookaMaxAmmo;
        CreateTurret();
        SetupAudio();
    }

    void SetupAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f;
        audioSource.volume = 0.4f;
    }

    void CreateTurret()
    {
        Transform body = transform.Find("Body");
        if (body == null) return;

        // Turret base (rotating platform on roof)
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = "TurretBase";
        baseObj.transform.SetParent(body);
        baseObj.transform.localPosition = new Vector3(0, 0.9f, -0.3f);
        baseObj.transform.localScale = new Vector3(0.5f, 0.08f, 0.5f);
        Destroy(baseObj.GetComponent<Collider>());

        Material baseMat = new Material(Shader.Find("Standard"));
        baseMat.color = turretColor;
        baseMat.SetFloat("_Metallic", 0.9f);
        baseMat.SetFloat("_Glossiness", 0.7f);
        baseObj.GetComponent<Renderer>().material = baseMat;

        turretBase = baseObj.transform;

        // Turret housing (the part that rotates)
        GameObject housingObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        housingObj.name = "TurretHousing";
        housingObj.transform.SetParent(turretBase);
        housingObj.transform.localPosition = new Vector3(0, 1.2f, 0);
        housingObj.transform.localScale = new Vector3(0.7f, 0.35f, 0.5f);
        Destroy(housingObj.GetComponent<Collider>());
        housingObj.GetComponent<Renderer>().material = baseMat;

        // Side armor plates
        GameObject leftPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftPlate.transform.SetParent(housingObj.transform);
        leftPlate.transform.localPosition = new Vector3(-0.55f, 0, 0);
        leftPlate.transform.localScale = new Vector3(0.15f, 1.1f, 0.9f);
        Destroy(leftPlate.GetComponent<Collider>());
        leftPlate.GetComponent<Renderer>().material = baseMat;

        GameObject rightPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightPlate.transform.SetParent(housingObj.transform);
        rightPlate.transform.localPosition = new Vector3(0.55f, 0, 0);
        rightPlate.transform.localScale = new Vector3(0.15f, 1.1f, 0.9f);
        Destroy(rightPlate.GetComponent<Collider>());
        rightPlate.GetComponent<Renderer>().material = baseMat;

        // Barrel
        GameObject barrelObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrelObj.name = "TurretBarrel";
        barrelObj.transform.SetParent(housingObj.transform);
        barrelObj.transform.localPosition = new Vector3(0, 0, 0.8f);
        barrelObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        barrelObj.transform.localScale = new Vector3(0.12f, 0.5f, 0.12f);
        Destroy(barrelObj.GetComponent<Collider>());

        Material barrelMat = new Material(Shader.Find("Standard"));
        barrelMat.color = new Color(0.1f, 0.1f, 0.12f);
        barrelMat.SetFloat("_Metallic", 0.95f);
        barrelMat.SetFloat("_Glossiness", 0.8f);
        barrelObj.GetComponent<Renderer>().material = barrelMat;

        turretBarrel = barrelObj.transform;

        // Muzzle brake
        GameObject muzzleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        muzzleObj.name = "Muzzle";
        muzzleObj.transform.SetParent(barrelObj.transform);
        muzzleObj.transform.localPosition = new Vector3(0, 1.1f, 0);
        muzzleObj.transform.localScale = new Vector3(1.4f, 0.15f, 1.4f);
        Destroy(muzzleObj.GetComponent<Collider>());
        muzzleObj.GetComponent<Renderer>().material = barrelMat;

        muzzle = muzzleObj.transform;

        // Second barrel for twin gun look
        GameObject barrel2Obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel2Obj.name = "TurretBarrel2";
        barrel2Obj.transform.SetParent(housingObj.transform);
        barrel2Obj.transform.localPosition = new Vector3(0.15f, -0.1f, 0.7f);
        barrel2Obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        barrel2Obj.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
        Destroy(barrel2Obj.GetComponent<Collider>());
        barrel2Obj.GetComponent<Renderer>().material = barrelMat;

        GameObject barrel3Obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel3Obj.name = "TurretBarrel3";
        barrel3Obj.transform.SetParent(housingObj.transform);
        barrel3Obj.transform.localPosition = new Vector3(-0.15f, -0.1f, 0.7f);
        barrel3Obj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        barrel3Obj.transform.localScale = new Vector3(0.08f, 0.4f, 0.08f);
        Destroy(barrel3Obj.GetComponent<Collider>());
        barrel3Obj.GetComponent<Renderer>().material = barrelMat;

        // Ammo drum on back
        GameObject drumObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        drumObj.name = "AmmoDrum";
        drumObj.transform.SetParent(housingObj.transform);
        drumObj.transform.localPosition = new Vector3(0, 0, -0.4f);
        drumObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        drumObj.transform.localScale = new Vector3(0.5f, 0.2f, 0.5f);
        Destroy(drumObj.GetComponent<Collider>());

        Material drumMat = new Material(Shader.Find("Standard"));
        drumMat.color = new Color(0.6f, 0.55f, 0.3f); // Brass/gold ammo drum
        drumMat.SetFloat("_Metallic", 0.85f);
        drumMat.SetFloat("_Glossiness", 0.6f);
        drumObj.GetComponent<Renderer>().material = drumMat;
    }

    void Update()
    {
        if (turretBase == null) return;

        // Update fire timers
        if (fireTimer > 0)
            fireTimer -= Time.deltaTime;
        if (bazookaTimer > 0)
            bazookaTimer -= Time.deltaTime;

        var gp = Gamepad.current;

        // Turret rotation with Q/E or J/L (keyboard) - incremental rotation
        float rotateInput = 0f;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.LeftBracket) || Input.GetKey(KeyCode.J)) rotateInput = -1f;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.RightBracket) || Input.GetKey(KeyCode.L)) rotateInput = 1f;

        // Track if player is providing turret input this frame
        bool playerControllingTurret = false;

        // Controller: right stick for DIRECT AIM (twin-stick shooter style)
        bool usingStickAim = false;
        if (gp != null)
        {
            Vector2 rightStick = gp.rightStick.ReadValue();
            if (rightStick.magnitude > 0.3f) // Deadzone
            {
                usingStickAim = true;
                playerControllingTurret = true;
                lastTurretInputTime = Time.time;

                // Convert stick direction to world direction (relative to car)
                Vector3 aimDirection = transform.forward * rightStick.y + transform.right * rightStick.x;
                aimDirection.y = 0;
                aimDirection.Normalize();

                // Smoothly rotate turret to face aim direction
                Quaternion targetRot = Quaternion.LookRotation(aimDirection);
                turretBase.rotation = Quaternion.RotateTowards(
                    turretBase.rotation,
                    targetRot,
                    rotationSpeed * 2f * Time.deltaTime // Faster for direct aim
                );
            }
        }

        // Mouse aiming option (right mouse button)
        if (!usingStickAim && Input.GetMouseButton(1))
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            if (mouseWorld != Vector3.zero)
            {
                Vector3 direction = mouseWorld - turretBase.position;
                direction.y = 0;
                if (direction.magnitude > 0.1f)
                {
                    playerControllingTurret = true;
                    lastTurretInputTime = Time.time;

                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    turretBase.rotation = Quaternion.RotateTowards(
                        turretBase.rotation,
                        targetRot,
                        rotationSpeed * Time.deltaTime
                    );
                }
            }
        }
        // Keyboard rotation (Q/E keys)
        else if (!usingStickAim && Mathf.Abs(rotateInput) > 0.1f)
        {
            playerControllingTurret = true;
            lastTurretInputTime = Time.time;

            turretBase.Rotate(0, rotateInput * rotationSpeed * Time.deltaTime, 0, Space.World);
        }

        // Auto-center turret when idle
        if (!playerControllingTurret && Time.time - lastTurretInputTime > autoCenterDelay)
        {
            // Target rotation: aligned with car's forward direction
            Quaternion centerRotation = Quaternion.LookRotation(transform.forward, Vector3.up);

            // Smoothly rotate towards center
            turretBase.rotation = Quaternion.RotateTowards(
                turretBase.rotation,
                centerRotation,
                autoCenterSpeed * Time.deltaTime
            );
        }

        // Regular fire with left mouse, F/K key, or R1 (right shoulder)
        bool fireHeld = Input.GetMouseButton(0) || Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.K)
            || (gp != null && gp.rightShoulder.isPressed);
        if (fireHeld && fireTimer <= 0 && currentAmmo > 0)
        {
            Fire();
        }

        // Bazooka fire with V key or L1 (left shoulder)
        bool bazookaHeld = Input.GetKey(KeyCode.V) || (gp != null && gp.leftShoulder.isPressed);
        if (bazookaHeld && bazookaTimer <= 0 && currentBazookaAmmo > 0)
        {
            FireBazooka();
        }

        // Reload with G/H or North button (Y/Triangle)
        bool reloadPressed = Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.H)
            || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        if (reloadPressed)
        {
            if (currentAmmo < maxAmmo) currentAmmo = maxAmmo;
            if (currentBazookaAmmo < bazookaMaxAmmo) currentBazookaAmmo = bazookaMaxAmmo;
        }

        // Clean up destroyed projectiles
        activeProjectiles.RemoveAll(p => p == null);
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        float distance;
        if (groundPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }

    void Fire()
    {
        if (muzzle == null || turretBase == null) return;

        fireTimer = 1f / fireRate;
        currentAmmo--;

        // Create projectile - MUCH BIGGER for visibility (2.5x larger)
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Projectile";

        // Spawn bullet at muzzle position but at a consistent height for hitting ground targets
        Vector3 spawnPos = muzzle.position;
        spawnPos.y = transform.position.y + 1.2f; // Consistent height relative to car
        projectile.transform.position = spawnPos;
        projectile.transform.localScale = Vector3.one * 0.5f; // 2.5x bigger!

        // Projectile material - MUCH brighter glowing yellow/orange tracer
        Material projMat = new Material(Shader.Find("Standard"));
        Color brightTracerColor = new Color(1f, 0.75f, 0.2f); // Bright yellow-orange
        projMat.color = brightTracerColor;
        projMat.SetColor("_EmissionColor", brightTracerColor * 8f); // Much brighter glow
        projMat.EnableKeyword("_EMISSION");
        projMat.SetFloat("_Metallic", 0.9f);
        projMat.SetFloat("_Glossiness", 1f);
        projectile.GetComponent<Renderer>().material = projMat;

        // Add rigidbody for physics
        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.1f;
        rb.linearDamping = 0;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Add projectile behavior
        Projectile proj = projectile.AddComponent<Projectile>();
        proj.damage = 35f; // More damage to destroy cops faster
        proj.lifetime = projectileLifetime;
        proj.owner = gameObject;

        // Fire in turret's forward direction - KEEP IT LEVEL (no Y component)
        Vector3 fireDirection = turretBase.forward;
        fireDirection.y = 0; // Force horizontal firing
        fireDirection.Normalize();
        rb.linearVelocity = fireDirection * projectileSpeed + (car != null ? car.Velocity * 0.3f : Vector3.zero);

        activeProjectiles.Add(projectile);

        // Muzzle flash effect
        CreateMuzzleFlash();

        // Play sound
        PlayFireSound();

        // Camera shake
        var cam = Camera.main?.GetComponent<CameraFollow>();
        cam?.Shake(0.05f);
    }

    void FireBazooka()
    {
        if (muzzle == null || turretBase == null) return;

        bazookaTimer = 1f / bazookaFireRate;
        currentBazookaAmmo--;

        // Create bazooka projectile - MASSIVE rocket
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Bazooka";

        Vector3 spawnPos = muzzle.position;
        spawnPos.y = transform.position.y + 1.2f;
        projectile.transform.position = spawnPos;
        projectile.transform.localScale = Vector3.one * 0.8f; // 4x bigger than original!

        // INTENSELY glowing red/orange rocket material
        Material projMat = new Material(Shader.Find("Standard"));
        Color rocketColor = new Color(1f, 0.4f, 0.15f); // Bright orange-red
        projMat.color = rocketColor;
        projMat.SetColor("_EmissionColor", rocketColor * 10f); // Super bright glow
        projMat.EnableKeyword("_EMISSION");
        projMat.SetFloat("_Metallic", 0.7f);
        projMat.SetFloat("_Glossiness", 0.9f);
        projectile.GetComponent<Renderer>().material = projMat;

        // Physics
        Rigidbody rb = projectile.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.mass = 0.5f;
        rb.linearDamping = 0;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Add bazooka projectile behavior
        BazookaProjectile proj = projectile.AddComponent<BazookaProjectile>();
        proj.damage = bazookaDamage;
        proj.lifetime = projectileLifetime * 1.5f;
        proj.owner = gameObject;

        // Fire slower
        Vector3 fireDirection = turretBase.forward;
        fireDirection.y = 0;
        fireDirection.Normalize();
        rb.linearVelocity = fireDirection * bazookaSpeed + (car != null ? car.Velocity * 0.2f : Vector3.zero);

        activeProjectiles.Add(projectile);

        // Bigger muzzle flash
        CreateBazookaMuzzleFlash();

        // Play deeper sound
        PlayBazookaSound();

        // Bigger camera shake
        var cam = Camera.main?.GetComponent<CameraFollow>();
        cam?.Shake(0.15f);
    }

    void CreateBazookaMuzzleFlash()
    {
        if (muzzle == null) return;

        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "BazookaMuzzleFlash";
        flash.transform.position = muzzle.position;
        flash.transform.localScale = Vector3.one * 0.8f; // Bigger flash
        Destroy(flash.GetComponent<Collider>());

        Material flashMat = new Material(Shader.Find("Standard"));
        flashMat.color = bazookaColor;
        flashMat.SetColor("_EmissionColor", bazookaColor * 6f);
        flashMat.EnableKeyword("_EMISSION");
        flashMat.SetFloat("_Mode", 3);
        flashMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        flashMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        flashMat.renderQueue = 3000;
        flash.GetComponent<Renderer>().material = flashMat;

        Light flashLight = flash.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = bazookaColor;
        flashLight.intensity = 5f;
        flashLight.range = 8f;

        Destroy(flash, 0.08f);
    }

    void PlayBazookaSound()
    {
        if (audioSource == null) return;

        // Deeper, booming bazooka sound
        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.3f);
        AudioClip clip = AudioClip.Create("Bazooka", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            // Very deep bass thump
            float deepBass = Mathf.Sin(2 * Mathf.PI * 30f * t) * Mathf.Exp(-t * 8f);
            // Mid boom
            float midBoom = Mathf.Sin(2 * Mathf.PI * 60f * t) * Mathf.Exp(-t * 15f) * 0.7f;
            // Whoosh
            float whoosh = Random.Range(-1f, 1f) * Mathf.Exp(-t * 12f) * 0.4f;
            data[i] = (deepBass * 0.8f + midBoom + whoosh);
        }
        clip.SetData(data, 0);

        audioSource.pitch = Random.Range(0.6f, 0.75f);
        audioSource.PlayOneShot(clip, 0.7f);
    }

    void CreateMuzzleFlash()
    {
        if (muzzle == null) return;

        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "MuzzleFlash";
        flash.transform.position = muzzle.position;
        flash.transform.localScale = Vector3.one * 0.4f;
        Destroy(flash.GetComponent<Collider>());

        Material flashMat = new Material(Shader.Find("Standard"));
        flashMat.color = muzzleFlashColor;
        flashMat.SetColor("_EmissionColor", muzzleFlashColor * 5f);
        flashMat.EnableKeyword("_EMISSION");
        flashMat.SetFloat("_Mode", 3);
        flashMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        flashMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        flashMat.renderQueue = 3000;
        flash.GetComponent<Renderer>().material = flashMat;

        // Add point light for flash
        Light flashLight = flash.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = muzzleFlashColor;
        flashLight.intensity = 3f;
        flashLight.range = 5f;

        Destroy(flash, 0.05f);
    }

    void PlayFireSound()
    {
        if (audioSource == null) return;

        // Generate a deeper, punchier gunshot sound
        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.15f); // 0.15 second for more bass
        AudioClip clip = AudioClip.Create("GunShot", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            // Deep bass punch (lower frequency)
            float bassPunch = Mathf.Sin(2 * Mathf.PI * 45f * t) * Mathf.Exp(-t * 20f);
            // Mid thump
            float midPunch = Mathf.Sin(2 * Mathf.PI * 90f * t) * Mathf.Exp(-t * 35f) * 0.6f;
            // Sharp attack noise
            float noise = Random.Range(-1f, 1f) * Mathf.Exp(-t * 40f) * 0.3f;
            // Combine for deep, punchy sound
            data[i] = (bassPunch * 0.7f + midPunch + noise);
        }
        clip.SetData(data, 0);

        audioSource.pitch = Random.Range(0.75f, 0.9f); // Lower pitch for deeper sound
        audioSource.PlayOneShot(clip, 0.6f);
    }

    // Public getters for UI
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public float AmmoPercent => (float)currentAmmo / maxAmmo;
    public int CurrentBazookaAmmo => currentBazookaAmmo;
    public int MaxBazookaAmmo => bazookaMaxAmmo;
    public float BazookaAmmoPercent => (float)currentBazookaAmmo / bazookaMaxAmmo;
}

public class Projectile : MonoBehaviour
{
    public float damage = 25f;
    public float lifetime = 3f;
    public GameObject owner;

    private float timer;
    private TrailRenderer trail;
    private Light bulletLight;

    void Start()
    {
        timer = lifetime;

        // Add BIGGER, more dramatic tracer trail
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.3f; // Longer trail
        trail.startWidth = 0.4f; // Much wider
        trail.endWidth = 0.05f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.8f, 0.3f, 1f); // Bright yellow
        trail.endColor = new Color(1f, 0.5f, 0.1f, 0f); // Fades to orange
        trail.numCapVertices = 5;
        trail.numCornerVertices = 5;

        // Add dynamic point light for dramatic tracer effect
        bulletLight = gameObject.AddComponent<Light>();
        bulletLight.type = LightType.Point;
        bulletLight.color = new Color(1f, 0.75f, 0.3f);
        bulletLight.intensity = 4f;
        bulletLight.range = 8f;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }

        // Subtle light pulsing for visual interest
        if (bulletLight != null)
        {
            bulletLight.intensity = 3.5f + Mathf.Sin(Time.time * 30f) * 0.5f;
        }
    }

    void OnCollisionEnter(Collision col)
    {
        // Don't hit owner
        if (col.gameObject == owner) return;
        if (col.transform.IsChildOf(owner.transform)) return;

        // Deal damage to destructibles (like cop cars)
        Destructible destructible = col.gameObject.GetComponent<Destructible>();
        if (destructible != null)
        {
            destructible.TakeDamage(damage);
        }

        // Create BIGGER, more satisfying impact effect
        CreateImpactEffect(col.contacts[0].point, col.contacts[0].normal);

        // Destroy projectile
        Destroy(gameObject);
    }

    void CreateImpactEffect(Vector3 position, Vector3 normal)
    {
        // MORE spark particles, BIGGER
        for (int i = 0; i < 15; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "Spark";
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f); // Bigger sparks
            Destroy(spark.GetComponent<Collider>());

            Material sparkMat = new Material(Shader.Find("Standard"));
            Color sparkColor = new Color(1f, Random.Range(0.5f, 0.9f), Random.Range(0.2f, 0.4f));
            sparkMat.color = sparkColor;
            sparkMat.SetColor("_EmissionColor", sparkColor * 6f); // Brighter
            sparkMat.EnableKeyword("_EMISSION");
            spark.GetComponent<Renderer>().material = sparkMat;

            Rigidbody rb = spark.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 0.01f;
            Vector3 randomDir = (normal + Random.insideUnitSphere).normalized;
            rb.linearVelocity = randomDir * Random.Range(8f, 22f); // Faster sparks

            Destroy(spark, Random.Range(0.4f, 0.8f));
        }

        // BIGGER, BRIGHTER impact flash
        GameObject flash = new GameObject("ImpactFlash");
        flash.transform.position = position;
        Light impactLight = flash.AddComponent<Light>();
        impactLight.type = LightType.Point;
        impactLight.color = new Color(1f, 0.7f, 0.3f);
        impactLight.intensity = 8f; // Brighter
        impactLight.range = 7f; // Larger radius
        Destroy(flash, 0.15f);

        // Add visible impact sphere flash
        GameObject impactSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impactSphere.name = "ImpactFlashSphere";
        impactSphere.transform.position = position;
        impactSphere.transform.localScale = Vector3.one * 0.6f;
        Destroy(impactSphere.GetComponent<Collider>());

        Material impactMat = new Material(Shader.Find("Standard"));
        Color impactColor = new Color(1f, 0.8f, 0.4f);
        impactMat.color = impactColor;
        impactMat.SetColor("_EmissionColor", impactColor * 10f);
        impactMat.EnableKeyword("_EMISSION");
        impactMat.SetFloat("_Mode", 3);
        impactMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        impactMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        impactMat.renderQueue = 3000;
        impactSphere.GetComponent<Renderer>().material = impactMat;
        Destroy(impactSphere, 0.1f);
    }
}

public class BazookaProjectile : MonoBehaviour
{
    public float damage = 105f;
    public float lifetime = 4.5f;
    public GameObject owner;

    private float timer;
    private TrailRenderer trail;
    private Light rocketLight;

    void Start()
    {
        timer = lifetime;

        // MUCH bigger, more dramatic fiery trail
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.6f; // Longer trail
        trail.startWidth = 0.6f; // Much wider
        trail.endWidth = 0.1f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.5f, 0.15f, 1f); // Bright orange
        trail.endColor = new Color(1f, 0.2f, 0.1f, 0f); // Fades to deep red
        trail.numCapVertices = 6;
        trail.numCornerVertices = 6;

        // Add dramatic rocket light
        rocketLight = gameObject.AddComponent<Light>();
        rocketLight.type = LightType.Point;
        rocketLight.color = new Color(1f, 0.4f, 0.15f);
        rocketLight.intensity = 6f;
        rocketLight.range = 12f;

        // Add smoke trail effect
        CreateSmokeTrail();
    }

    void CreateSmokeTrail()
    {
        // Add a secondary particle-like smoke effect
        GameObject smoke = new GameObject("SmokeTrail");
        smoke.transform.SetParent(transform);
        smoke.transform.localPosition = Vector3.zero;

        TrailRenderer smokeTrail = smoke.AddComponent<TrailRenderer>();
        smokeTrail.time = 1.2f; // Longer smoke
        smokeTrail.startWidth = 0.25f;
        smokeTrail.endWidth = 0.7f; // Expands more
        smokeTrail.material = new Material(Shader.Find("Sprites/Default"));
        smokeTrail.startColor = new Color(0.4f, 0.35f, 0.3f, 0.7f);
        smokeTrail.endColor = new Color(0.25f, 0.25f, 0.25f, 0f);
        smokeTrail.numCapVertices = 4;
        smokeTrail.numCornerVertices = 4;
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            Destroy(gameObject);
        }

        // Flickering light for rocket engine effect
        if (rocketLight != null)
        {
            rocketLight.intensity = 5f + Mathf.Sin(Time.time * 50f) * 1.5f + Random.Range(-0.5f, 0.5f);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        // Don't hit owner
        if (col.gameObject == owner) return;
        if (col.transform.IsChildOf(owner.transform)) return;

        // Deal damage to destructibles
        Destructible destructible = col.gameObject.GetComponent<Destructible>();
        if (destructible != null)
        {
            destructible.TakeDamage(damage);
        }

        // MASSIVE explosion effect
        CreateExplosion(col.contacts[0].point);

        // Destroy projectile
        Destroy(gameObject);
    }

    void CreateExplosion(Vector3 position)
    {
        // LOTS of debris/sparks - more satisfying explosion
        for (int i = 0; i < 35; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "ExplosionDebris";
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * Random.Range(0.08f, 0.22f); // Bigger debris
            Destroy(spark.GetComponent<Collider>());

            Material sparkMat = new Material(Shader.Find("Standard"));
            Color sparkColor = new Color(1f, Random.Range(0.3f, 0.8f), Random.Range(0.1f, 0.3f));
            sparkMat.color = sparkColor;
            sparkMat.SetColor("_EmissionColor", sparkColor * 7f); // Brighter
            sparkMat.EnableKeyword("_EMISSION");
            spark.GetComponent<Renderer>().material = sparkMat;

            Rigidbody rb = spark.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 0.02f;
            rb.linearVelocity = Random.insideUnitSphere * Random.Range(12f, 30f); // Faster debris

            Destroy(spark, Random.Range(0.6f, 1.3f));
        }

        // MASSIVE explosion flash
        GameObject flash = new GameObject("ExplosionFlash");
        flash.transform.position = position;
        Light explosionLight = flash.AddComponent<Light>();
        explosionLight.type = LightType.Point;
        explosionLight.color = new Color(1f, 0.55f, 0.2f);
        explosionLight.intensity = 18f; // Much brighter
        explosionLight.range = 20f; // Much larger
        Destroy(flash, 0.25f);

        // Add visible explosion fireball
        GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireball.name = "ExplosionFireball";
        fireball.transform.position = position;
        fireball.transform.localScale = Vector3.one * 1.5f; // Big fireball
        Destroy(fireball.GetComponent<Collider>());

        Material fireballMat = new Material(Shader.Find("Standard"));
        Color fireColor = new Color(1f, 0.6f, 0.2f);
        fireballMat.color = fireColor;
        fireballMat.SetColor("_EmissionColor", fireColor * 12f);
        fireballMat.EnableKeyword("_EMISSION");
        fireballMat.SetFloat("_Mode", 3);
        fireballMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        fireballMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        fireballMat.renderQueue = 3000;
        fireball.GetComponent<Renderer>().material = fireballMat;
        Destroy(fireball, 0.15f);

        // Explosion sound
        PlayExplosionSound(position);

        // Camera shake for nearby explosions
        var cam = Camera.main?.GetComponent<CameraFollow>();
        cam?.Shake(0.25f);
    }

    void PlayExplosionSound(Vector3 position)
    {
        GameObject audioObj = new GameObject("Explosion");
        audioObj.transform.position = position;
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        int sampleRate = 44100;
        int samples = (int)(sampleRate * 0.6f); // Longer explosion sound
        AudioClip clip = AudioClip.Create("Boom", samples, 1, sampleRate, false);

        float[] data = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            // Deep rumble
            float bass = Mathf.Sin(2 * Mathf.PI * 28f * t) * Mathf.Exp(-t * 3f); // Lower, longer bass
            // Mid crunch
            float mid = Mathf.Sin(2 * Mathf.PI * 70f * t) * Mathf.Exp(-t * 6f) * 0.7f;
            // Noise burst
            float noise = Random.Range(-1f, 1f) * Mathf.Exp(-t * 5f) * 0.6f;
            data[i] = (bass + mid + noise) * 0.9f;
        }
        clip.SetData(data, 0);

        audio.clip = clip;
        audio.volume = 0.75f; // Louder
        audio.spatialBlend = 0.6f;
        audio.Play();

        Destroy(audioObj, 0.7f);
    }
}
