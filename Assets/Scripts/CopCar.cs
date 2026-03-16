using UnityEngine;

public enum CopState
{
    Patrol,
    Fleeing,
    DonutBreak,     // Craving donuts, driving to Dunkin
    AtDunkin,       // Parked at Dunkin, eating donuts
    Pursuit         // Aggressive pursuit mode - ramming the player
}

public class CopCar : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 18f;
    public float fleeSpeed = 28f;
    public float turnSpeed = 4f;
    public float fleeDistance = 50f;

    [Header("Shooting")]
    public float shootRange = 40f;
    public float shootCooldown = 2f;
    public float bulletSpeed = 50f;
    public float bulletDamage = 10f;

    [Header("Donut Break")]
    public float donutCravingChance = 0.003f;  // Per frame chance when patrolling
    public float donutBreakDuration = 20f;     // How long they stay at Dunkin
    public float reactionDelayAtDunkin = 2.5f; // Slow to react when at Dunkin

    [Header("Pursuit Mode")]
    public float pursuitChance = 0.25f;        // 25% chance to enter pursuit mode when player gets close
    public float pursuitSpeed = 32f;           // Faster than flee speed
    public float pursuitTurnSpeed = 6f;        // More aggressive turning
    public float pursuitDuration = 15f;        // How long pursuit mode lasts
    public float pursuitRamDistance = 8f;      // Distance to attempt ramming
    public float pursuitActivationDistance = 45f; // Distance at which pursuit can trigger

    [Header("State")]
    public CopState state = CopState.Patrol;

    private Transform player;
    private Rigidbody rb;
    private Vector3 patrolTarget;
    private float patrolTimer;
    private float shootTimer = 0f;

    // Donut break state
    private DunkinStore targetDunkin;
    private Transform parkingSpot;
    private float donutTimer;
    private float reactionTimer;
    private bool lightsOff = false;

    // Pursuit mode state
    private float pursuitTimer;
    private bool inPursuitMode = false;
    private GameObject pursuitSirenObj;
    private AudioSource pursuitSirenAudio;
    private bool isDestroyed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.mass = 1200f;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.5f;
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Find player
        CarController playerCar = FindFirstObjectByType<CarController>();
        if (playerCar != null)
        {
            player = playerCar.transform;
        }

        SetNewPatrolTarget();
    }

    void FixedUpdate()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case CopState.Patrol:
                HandlePatrolState(distanceToPlayer);
                break;

            case CopState.Fleeing:
                HandleFleeingState(distanceToPlayer);
                break;

            case CopState.DonutBreak:
                HandleDonutBreakState(distanceToPlayer);
                break;

            case CopState.AtDunkin:
                HandleAtDunkinState(distanceToPlayer);
                break;

            case CopState.Pursuit:
                HandlePursuitState(distanceToPlayer);
                break;
        }
    }

    void HandlePatrolState(float distanceToPlayer)
    {
        // Check if player is close - either flee or enter pursuit mode
        if (distanceToPlayer < pursuitActivationDistance)
        {
            // Random chance to enter pursuit mode instead of fleeing
            if (Random.value < pursuitChance)
            {
                StartPursuitMode();
                return;
            }
            else if (distanceToPlayer < fleeDistance)
            {
                state = CopState.Fleeing;
                return;
            }
        }

        // Random chance to crave donuts
        if (Random.value < donutCravingChance && DunkinStore.AllStores.Count > 0)
        {
            StartDonutBreak();
            return;
        }

        Patrol();

        // Shoot at player if in range
        shootTimer -= Time.fixedDeltaTime;
        if (distanceToPlayer < shootRange && shootTimer <= 0)
        {
            ShootAtPlayer();
            shootTimer = shootCooldown;
        }
    }

    void HandleFleeingState(float distanceToPlayer)
    {
        // Stop fleeing if player is far enough
        if (distanceToPlayer > fleeDistance * 2f)
        {
            state = CopState.Patrol;
            return;
        }

        FleeFromPlayer();

        // Shoot while fleeing
        shootTimer -= Time.fixedDeltaTime;
        if (distanceToPlayer < shootRange && shootTimer <= 0)
        {
            ShootAtPlayer();
            shootTimer = shootCooldown;
        }
    }

    void HandleDonutBreakState(float distanceToPlayer)
    {
        // If player gets very close, abandon donut run
        if (distanceToPlayer < fleeDistance * 0.7f)
        {
            CancelDonutBreak();
            state = CopState.Fleeing;
            return;
        }

        // Drive to Dunkin
        if (targetDunkin == null || parkingSpot == null)
        {
            state = CopState.Patrol;
            return;
        }

        float distToSpot = Vector3.Distance(transform.position, parkingSpot.position);

        if (distToSpot < 3f)
        {
            // Arrived at parking spot
            ArriveAtDunkin();
            return;
        }

        // Drive towards parking spot
        DriveTowards(parkingSpot.position, speed);
    }

    void HandleAtDunkinState(float distanceToPlayer)
    {
        // Stay parked, eat donuts
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.fixedDeltaTime * 5f);

        donutTimer -= Time.fixedDeltaTime;

        // If player gets close, react (with delay)
        if (distanceToPlayer < fleeDistance)
        {
            reactionTimer -= Time.fixedDeltaTime;
            if (reactionTimer <= 0)
            {
                EndDonutBreak();
                state = CopState.Fleeing;
                return;
            }
        }
        else
        {
            reactionTimer = reactionDelayAtDunkin; // Reset reaction timer
        }

        // Done with break
        if (donutTimer <= 0)
        {
            EndDonutBreak();
            state = CopState.Patrol;
        }
    }

    void HandlePursuitState(float distanceToPlayer)
    {
        pursuitTimer -= Time.fixedDeltaTime;

        // End pursuit if timer expires or player is too far
        if (pursuitTimer <= 0 || distanceToPlayer > pursuitActivationDistance * 2f)
        {
            EndPursuitMode();
            state = CopState.Patrol;
            return;
        }

        // Aggressively chase and ram the player
        ChaseAndRamPlayer(distanceToPlayer);

        // Shoot while pursuing (more frequently)
        shootTimer -= Time.fixedDeltaTime;
        if (distanceToPlayer < shootRange && shootTimer <= 0)
        {
            ShootAtPlayer();
            shootTimer = shootCooldown * 0.6f; // Shoot faster in pursuit mode
        }
    }

    void StartPursuitMode()
    {
        if (inPursuitMode) return;

        state = CopState.Pursuit;
        inPursuitMode = true;
        pursuitTimer = pursuitDuration + Random.Range(-3f, 5f);

        // Make sure lights are on and flashing faster
        SetLights(true);

        // Start continuous siren
        StartPursuitSiren();

        Debug.Log(gameObject.name + " entering PURSUIT MODE! Ramming enabled!");
    }

    void EndPursuitMode()
    {
        if (!inPursuitMode) return;

        inPursuitMode = false;

        // Stop the pursuit siren
        StopPursuitSiren();

        Debug.Log(gameObject.name + " ending pursuit mode.");
    }

    void ChaseAndRamPlayer(float distanceToPlayer)
    {
        if (player == null) return;

        // Calculate direction to player with prediction
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        Vector3 targetPos = player.position;

        // Lead the target if player is moving
        if (playerRb != null && distanceToPlayer > pursuitRamDistance)
        {
            float timeToReach = distanceToPlayer / pursuitSpeed;
            targetPos += playerRb.linearVelocity * timeToReach * 0.5f;
        }

        Vector3 directionToPlayer = (targetPos - transform.position).normalized;
        directionToPlayer.y = 0;

        // Aggressive turning towards player
        if (directionToPlayer.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, pursuitTurnSpeed * Time.fixedDeltaTime);
        }

        // Ram speed - go faster when close to attempt the ram
        float currentSpeed = pursuitSpeed;
        if (distanceToPlayer < pursuitRamDistance)
        {
            currentSpeed = pursuitSpeed * 1.3f; // Extra burst for ramming
        }

        // Drive aggressively towards player
        Vector3 targetVelocity = transform.forward * currentSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 8f);
    }

    void StartPursuitSiren()
    {
        if (isDestroyed) return;

        // Create looping siren audio source
        pursuitSirenObj = new GameObject("PursuitSiren");
        pursuitSirenObj.transform.SetParent(transform);
        pursuitSirenObj.transform.localPosition = Vector3.zero;

        pursuitSirenAudio = pursuitSirenObj.AddComponent<AudioSource>();
        pursuitSirenAudio.loop = true;
        pursuitSirenAudio.spatialBlend = 0.9f;
        pursuitSirenAudio.volume = 0.4f;
        pursuitSirenAudio.minDistance = 10f;
        pursuitSirenAudio.maxDistance = 100f;
        pursuitSirenAudio.rolloffMode = AudioRolloffMode.Linear;

        // Create a longer siren loop clip
        int sampleRate = 44100;
        float duration = 2f; // 2 second loop
        int sampleCount = (int)(sampleRate * duration);
        AudioClip sirenClip = AudioClip.Create("PursuitSirenLoop", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;

            // Two-tone siren (wee-woo wee-woo)
            float cyclePos = (t % 1f);
            float freq;
            if (cyclePos < 0.5f)
            {
                freq = 700f; // High tone
            }
            else
            {
                freq = 500f; // Low tone
            }

            // Smooth transition between tones
            float transitionTime = 0.05f;
            if (cyclePos < transitionTime || (cyclePos > 0.5f - transitionTime && cyclePos < 0.5f + transitionTime) || cyclePos > 1f - transitionTime)
            {
                freq = Mathf.Lerp(500f, 700f, (Mathf.Sin(cyclePos * Mathf.PI * 20f) + 1f) * 0.5f);
            }

            samples[i] = Mathf.Sin(t * freq * 2f * Mathf.PI) * 0.35f;
        }

        sirenClip.SetData(samples, 0);
        pursuitSirenAudio.clip = sirenClip;
        pursuitSirenAudio.Play();
    }

    void StopPursuitSiren()
    {
        if (pursuitSirenAudio != null)
        {
            pursuitSirenAudio.Stop();
        }
        if (pursuitSirenObj != null)
        {
            Destroy(pursuitSirenObj);
            pursuitSirenObj = null;
            pursuitSirenAudio = null;
        }
    }

    void StartDonutBreak()
    {
        targetDunkin = DunkinStore.GetNearestStoreWithOpenSpot(transform.position);
        if (targetDunkin == null) return;

        parkingSpot = targetDunkin.GetOpenSpot();
        if (parkingSpot == null) return;

        state = CopState.DonutBreak;
        Debug.Log(gameObject.name + " craving donuts, heading to Dunkin!");
    }

    void ArriveAtDunkin()
    {
        state = CopState.AtDunkin;
        donutTimer = donutBreakDuration + Random.Range(-5f, 10f);
        reactionTimer = reactionDelayAtDunkin;

        // Park the car
        targetDunkin.ParkCop(this);

        // Turn off lights
        SetLights(false);

        // Snap to parking spot
        transform.position = parkingSpot.position;
        transform.rotation = parkingSpot.rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Debug.Log(gameObject.name + " parked at Dunkin, eating donuts for " + donutTimer.ToString("F0") + "s");
    }

    public void EndDonutBreak()
    {
        if (targetDunkin != null)
        {
            targetDunkin.UnparkCop(this);
        }

        targetDunkin = null;
        parkingSpot = null;
        SetLights(true);

        Debug.Log(gameObject.name + " leaving Dunkin!");
    }

    void CancelDonutBreak()
    {
        targetDunkin = null;
        parkingSpot = null;
    }

    void SetLights(bool on)
    {
        lightsOff = !on;
        // Find and toggle police lights
        foreach (Light light in GetComponentsInChildren<Light>())
        {
            light.enabled = on;
        }
        foreach (FlashingLight flash in GetComponentsInChildren<FlashingLight>())
        {
            flash.enabled = on;
        }
    }

    void DriveTowards(Vector3 target, float moveSpeed)
    {
        Vector3 direction = (target - transform.position).normalized;
        direction.y = 0;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        }

        Vector3 targetVelocity = transform.forward * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 4f);
    }

    void ShootAtPlayer()
    {
        if (player == null) return;

        // Create bullet - MUCH BIGGER for visibility (2.5x larger)
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "CopBullet";
        bullet.transform.position = transform.position + Vector3.up * 1f + transform.forward * 2f;
        bullet.transform.localScale = Vector3.one * 0.5f; // 2.5x bigger!

        // Glowing red/orange tracer-style for cop bullets (reduced to prevent blowout)
        Material mat = new Material(Shader.Find("Standard"));
        Color bulletColor = new Color(1f, 0.35f, 0.15f); // Orange-red
        mat.color = bulletColor;
        mat.SetColor("_EmissionColor", bulletColor * 1.5f); // Reduced from 6 to prevent blowout
        mat.EnableKeyword("_EMISSION");
        mat.SetFloat("_Metallic", 0.8f);
        mat.SetFloat("_Glossiness", 0.9f);
        bullet.GetComponent<Renderer>().material = mat;

        // Physics
        Rigidbody bulletRb = bullet.AddComponent<Rigidbody>();
        bulletRb.useGravity = false;
        bulletRb.mass = 0.1f;
        bulletRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Aim at player with some lead
        Vector3 toPlayer = (player.position - bullet.transform.position).normalized;
        bulletRb.linearVelocity = toPlayer * bulletSpeed;

        // Add cop bullet behavior (handles trail and light in Start())
        CopBullet cb = bullet.AddComponent<CopBullet>();
        cb.damage = bulletDamage;

        // Destroy after time
        Destroy(bullet, 3f);

        // Create muzzle flash for cop shot
        CreateCopMuzzleFlash();

        // Sound
        PlayShootSound();
    }

    void CreateCopMuzzleFlash()
    {
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = "CopMuzzleFlash";
        flash.transform.position = transform.position + Vector3.up * 1f + transform.forward * 2.5f;
        flash.transform.localScale = Vector3.one * 0.6f;
        Destroy(flash.GetComponent<Collider>());

        Material flashMat = new Material(Shader.Find("Standard"));
        Color flashColor = new Color(1f, 0.4f, 0.2f);
        flashMat.color = flashColor;
        flashMat.SetColor("_EmissionColor", flashColor * 2f);  // Reduced from 8 to prevent blowout
        flashMat.EnableKeyword("_EMISSION");
        flashMat.SetFloat("_Mode", 3);
        flashMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        flashMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        flashMat.renderQueue = 3000;
        flash.GetComponent<Renderer>().material = flashMat;

        Light flashLight = flash.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = flashColor;
        flashLight.intensity = 1.5f;  // Reduced from 4 to prevent blowout
        flashLight.range = 5f;

        Destroy(flash, 0.07f);
    }

    void PlayShootSound()
    {
        GameObject audioObj = new GameObject("CopShot");
        audioObj.transform.position = transform.position;
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        int sampleRate = 44100;
        float duration = 0.1f;
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("CopShot", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float noise = Random.Range(-1f, 1f) * Mathf.Exp(-t * 25f);
            samples[i] = noise * 0.4f;
        }

        clip.SetData(samples, 0);
        audio.clip = clip;
        audio.volume = 0.3f;
        audio.spatialBlend = 0.8f;
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }

    void FleeFromPlayer()
    {
        // Run AWAY from player
        Vector3 directionAway = (transform.position - player.position).normalized;
        directionAway.y = 0;

        // Turn away from player
        if (directionAway.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionAway);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * 1.5f * Time.fixedDeltaTime);
        }

        // Move forward fast using velocity directly
        Vector3 targetVelocity = transform.forward * fleeSpeed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 6f);
    }

    void Patrol()
    {
        patrolTimer -= Time.fixedDeltaTime;

        if (patrolTimer <= 0 || Vector3.Distance(transform.position, patrolTarget) < 15f)
        {
            SetNewPatrolTarget();
        }

        Vector3 directionToTarget = (patrolTarget - transform.position).normalized;
        directionToTarget.y = 0;

        // Turn towards target
        if (directionToTarget.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime);
        }

        // Move forward at patrol speed using velocity directly
        float patrolSpeed = speed * 0.6f;
        Vector3 targetVelocity = transform.forward * patrolSpeed;
        targetVelocity.y = rb.linearVelocity.y; // Preserve vertical velocity for gravity
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVelocity, Time.fixedDeltaTime * 3f);
    }

    void SetNewPatrolTarget()
    {
        // Random point on the track (track is at radius ~120-180)
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float radius = Random.Range(100f, 160f);
        patrolTarget = new Vector3(
            Mathf.Cos(angle) * radius,
            0.5f,
            Mathf.Sin(angle) * radius
        );
        patrolTimer = Random.Range(8f, 15f);
    }

    void OnCollisionEnter(Collision collision)
    {
        // If hit by player at high speed, take damage
        if (collision.gameObject.GetComponent<CarController>() != null)
        {
            Destructible destructible = GetComponent<Destructible>();
            if (destructible != null && collision.relativeVelocity.magnitude > 8f)
            {
                destructible.TakeDamage(collision.relativeVelocity.magnitude * 3f);
                PlayBriefSiren();
            }

            // If at Dunkin, getting rammed wakes them up immediately
            if (state == CopState.AtDunkin)
            {
                EndDonutBreak();
                state = CopState.Fleeing;
            }
        }
    }

    // Called when cop takes damage from projectiles
    public void OnTakeDamage()
    {
        PlayBriefSiren();
    }

    void PlayBriefSiren()
    {
        GameObject audioObj = new GameObject("CopSiren");
        audioObj.transform.position = transform.position;
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        // Create a brief ~1 second siren wail (soft)
        int sampleRate = 44100;
        float duration = 0.8f;
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("BriefSiren", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / sampleCount;

            // Siren frequency sweep (woop sound)
            float freq = 600f + Mathf.Sin(t * 8f) * 200f;

            // Fade in and out
            float envelope = Mathf.Sin(progress * Mathf.PI);

            // Generate siren tone
            float sample = Mathf.Sin(t * freq * 2f * Mathf.PI) * envelope;

            samples[i] = sample * 0.15f; // Soft volume
        }

        clip.SetData(samples, 0);
        audio.clip = clip;
        audio.volume = 0.25f;  // Soft
        audio.spatialBlend = 0.8f;
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }
}
