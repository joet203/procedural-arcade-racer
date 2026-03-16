using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Speed Settings")]
    public float maxSpeed = 42f;  // Slower top speed
    public float acceleration = 38f;
    public float brakeForce = 55f;
    public float reverseMaxSpeed = 15f;

    [Header("Steering")]
    public float steeringSpeed = 85f;
    public float steeringTightness = 0.92f;
    public float downforce = 12f;

    [Header("Drift")]
    [Range(0.9f, 1f)]
    public float normalGrip = 0.96f;
    [Range(0.6f, 0.95f)]
    public float driftGrip = 0.75f;
    public float driftTriggerAngle = 12f;
    public float driftSteerBoost = 1.4f;

    [Header("Boost")]
    public float boostMultiplier = 1.55f;
    public float boostDuration = 2.8f;
    public float boostCooldown = 3.5f;

    [Header("Air Control - Rocket League Style")]
    public float airSteerMultiplier = 0.15f;
    public float groundCheckDistance = 0.65f;
    public float airPitchSpeed = 12f;     // Moderate pitch control
    public float airRollSpeed = 14f;      // Moderate roll control
    public float airYawSpeed = 6f;        // Moderate yaw
    public float airSoarLift = 5f;        // Lift when nose is up
    public float maxTiltAngle = 50f;      // Max degrees from horizontal

    [Header("Jump")]
    public float jumpForce = 8f;
    public float doubleJumpForce = 6f;
    public float jumpCooldown = 0.3f;
    private bool canDoubleJump = false;

    [Header("Handbrake")]
    public float handbrakeGrip = 0.4f;
    public float handbrakeTurnBoost = 2.2f;

    [Header("Wall Bounce Recovery")]
    public float wallBounceForce = 15f;       // Impulse force pushing car away from wall (increased)
    public float wallBounceTorque = 6f;       // Rotation torque to angle car away (increased)
    public float wallBounceMinSpeed = 1f;     // Minimum impact speed to trigger bounce (lowered)

    // Components
    private Rigidbody rb;
    private Transform bodyTransform;

    // State
    private float currentSpeed;
    private float steerInput;
    private float smoothSteerInput;
    private float steerVelocity;
    private float moveInput;
    private float smoothMoveInput;
    private float moveVelocity;
    private bool isDrifting;
    private float boostTimer;
    private float boostCooldownTimer;
    private bool isBoosting;
    private bool isGrounded;
    private float airTime;
    private float driftScore;
    private float currentDriftScore;
    private bool isHandbraking;
    private float jumpCooldownTimer;

    // Public getters
    public float CurrentSpeed => currentSpeed;
    public float MaxSpeed => isBoosting ? maxSpeed * boostMultiplier : maxSpeed;
    public bool IsDrifting => isDrifting;
    public bool IsBoosting => isBoosting;
    public bool IsGrounded => isGrounded;
    public float AirTime => airTime;
    public bool IsHandbraking => isHandbraking;
    public float BoostCooldownPercent => 1f - (boostCooldownTimer / boostCooldown);
    public float SpeedPercent => Mathf.Clamp01(Mathf.Abs(currentSpeed) / MaxSpeed);
    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public float SteerInput => smoothSteerInput;
    public float DriftScore => driftScore;
    public float CurrentDriftScore => currentDriftScore;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.mass = 1500f;  // Heavier car
        rb.linearDamping = 0.02f;
        rb.angularDamping = 5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.centerOfMass = new Vector3(0, -0.5f, 0.15f);  // Lower center of mass (engine heavy)

        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        bodyTransform = transform.Find("Body");
    }

    void Update()
    {
        // Keyboard input
        moveInput = Input.GetAxisRaw("Vertical");
        steerInput = Input.GetAxis("Horizontal");

        if (Input.GetKey(KeyCode.UpArrow)) moveInput = 1f;
        if (Input.GetKey(KeyCode.DownArrow)) moveInput = -1f;
        if (Input.GetKey(KeyCode.LeftArrow)) steerInput = -1f;
        if (Input.GetKey(KeyCode.RightArrow)) steerInput = 1f;

        // Gamepad input (new Input System)
        var gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 stick = gp.leftStick.ReadValue();
            if (Mathf.Abs(stick.y) > 0.1f) moveInput = stick.y;
            if (Mathf.Abs(stick.x) > 0.1f) steerInput = stick.x;
        }

        // Gamepad triggers for gas/brake
        // Left trigger: handbrake when moving forward, reverse when stopped
        bool leftTriggerHandbrake = false;
        if (gp != null)
        {
            float rt = gp.rightTrigger.ReadValue();
            float lt = gp.leftTrigger.ReadValue();

            if (rt > 0.1f) moveInput = rt;

            if (lt > 0.1f)
            {
                // If moving forward at decent speed, left trigger = handbrake/drift
                if (currentSpeed > 5f)
                {
                    leftTriggerHandbrake = true;
                    // Don't change moveInput - let them keep accelerating while drifting
                }
                else
                {
                    // Stopped or slow - left trigger = reverse
                    moveInput = -lt;
                }
            }
        }

        // Smooth input
        smoothMoveInput = Mathf.SmoothDamp(smoothMoveInput, moveInput, ref moveVelocity, 0.03f);
        smoothSteerInput = Mathf.SmoothDamp(smoothSteerInput, steerInput, ref steerVelocity, 0.025f);

        // Handbrake (Shift, West button X/Square, or Left Trigger when moving forward)
        isHandbraking = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
            || (gp != null && gp.buttonWest.isPressed)
            || leftTriggerHandbrake;

        // Jump (Space or South button - A/Cross)
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space)
            || (gp != null && gp.buttonSouth.wasPressedThisFrame);
        if (jumpPressed && jumpCooldownTimer <= 0)
        {
            if (isGrounded)
            {
                Jump();
                canDoubleJump = true; // Enable double jump after first jump
            }
            else if (canDoubleJump)
            {
                DoubleJump();
                canDoubleJump = false; // Used up double jump
            }
        }

        // Boost (B/N/Ctrl or East button - B/Circle)
        bool boostPressed = Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.N)
            || Input.GetKeyDown(KeyCode.LeftControl)
            || (gp != null && gp.buttonEast.wasPressedThisFrame);
        // Boost works anytime (including in air) - just needs cooldown to be ready
        if (boostPressed && boostCooldownTimer <= 0)
        {
            StartBoost();
        }

        UpdateBoost();
        UpdateDriftScore();
        UpdateJumpCooldown();
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        jumpCooldownTimer = jumpCooldown;

        var cam = Camera.main?.GetComponent<CameraFollow>();
        cam?.Shake(0.1f);
    }

    void DoubleJump()
    {
        // Cancel vertical velocity first for consistent double jump height
        Vector3 vel = rb.linearVelocity;
        vel.y = 0;
        rb.linearVelocity = vel;

        // Apply double jump force
        rb.AddForce(Vector3.up * doubleJumpForce, ForceMode.VelocityChange);
        jumpCooldownTimer = jumpCooldown;

        // Level the car slightly on double jump
        Vector3 euler = transform.eulerAngles;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, euler.y, 0), 0.3f);

        var cam = Camera.main?.GetComponent<CameraFollow>();
        cam?.Shake(0.08f);

        // Play a little sound
        PlayDoubleJumpSound();
    }

    void PlayDoubleJumpSound()
    {
        GameObject audioObj = new GameObject("DoubleJump");
        audioObj.transform.position = transform.position;
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        int sampleRate = 44100;
        float duration = 0.12f;
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("DoubleJump", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleCount;
            // Soft low thump with slight rise
            float freq = 80f + t * 60f;
            float env = Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-t * 3f);
            samples[i] = Mathf.Sin(t * freq * 2f * Mathf.PI) * env * 0.4f;
            // Add a bit of air noise
            samples[i] += (Random.value - 0.5f) * env * 0.1f;
        }

        clip.SetData(samples, 0);
        audio.clip = clip;
        audio.volume = 0.2f;
        audio.spatialBlend = 0.5f;
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }

    void UpdateJumpCooldown()
    {
        if (jumpCooldownTimer > 0)
            jumpCooldownTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        CheckGrounded();
        currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        // Detect drift with smoothing - handbrake forces drift
        if (rb.linearVelocity.magnitude > 3f && isGrounded)
        {
            float angle = Vector3.Angle(rb.linearVelocity, transform.forward);
            isDrifting = (angle > driftTriggerAngle && Mathf.Abs(currentSpeed) > 2f) || (isHandbraking && Mathf.Abs(currentSpeed) > 3f);
        }
        else
        {
            isDrifting = false;
        }

        if (isGrounded)
        {
            ApplyDownforce();
            ApplyAcceleration();
            ApplySteering();
            ApplyGrip();
        }
        else
        {
            ApplyAirControl();
            airTime += Time.fixedDeltaTime;
        }

        // Boost force applied in FixedUpdate for smooth physics
        ApplyBoostForce();

        ClampSpeed();
        UpdateBodyTilt();
    }

    void CheckGrounded()
    {
        RaycastHit hit;
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out hit, groundCheckDistance + 0.2f);

        // Toggle rotation constraints based on grounded state
        if (isGrounded && !wasGrounded)
        {
            // Just landed - lock rotation and auto-level
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            // Snap to upright (smooth would be better but this works)
            Vector3 euler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0, euler.y, 0);
            rb.angularVelocity = Vector3.zero;

            // Reset double jump
            canDoubleJump = false;

            if (airTime > 0.3f)
            {
                var cam = Camera.main?.GetComponent<CameraFollow>();
                cam?.Shake(airTime * 0.12f);
            }
            airTime = 0;
        }
        else if (!isGrounded && wasGrounded)
        {
            // Just went airborne - unlock rotation for air control
            rb.constraints = RigidbodyConstraints.None;
        }
        else if (!isGrounded)
        {
            // In air - keep tracking air time
        }
        else
        {
            airTime = 0;
        }
    }

    void ApplyDownforce()
    {
        float speedDownforce = downforce * SpeedPercent;
        rb.AddForce(-Vector3.up * speedDownforce, ForceMode.Acceleration);
    }

    void ApplyAcceleration()
    {
        float effectiveMaxSpeed = MaxSpeed;
        float accel = acceleration * (isBoosting ? 1.4f : 1f);

        if (smoothMoveInput > 0.1f)
        {
            float speedRatio = currentSpeed / effectiveMaxSpeed;
            float accelCurve = 1f - Mathf.Pow(Mathf.Clamp01(speedRatio), 1.8f);
            accelCurve = Mathf.Clamp(accelCurve, 0.15f, 1f);

            rb.AddForce(transform.forward * accel * smoothMoveInput * accelCurve, ForceMode.Acceleration);
        }
        else if (smoothMoveInput < -0.1f)
        {
            if (currentSpeed > 1f)
            {
                rb.AddForce(-transform.forward * brakeForce, ForceMode.Acceleration);
            }
            else
            {
                float reverseRatio = Mathf.Abs(currentSpeed) / reverseMaxSpeed;
                float reverseCurve = 1f - Mathf.Pow(reverseRatio, 2);
                rb.AddForce(transform.forward * accel * 0.45f * smoothMoveInput * reverseCurve, ForceMode.Acceleration);
            }
        }
        else
        {
            // Natural deceleration
            if (rb.linearVelocity.magnitude > 0.5f)
            {
                rb.AddForce(-rb.linearVelocity.normalized * 4f, ForceMode.Acceleration);
            }
        }
    }

    void ApplySteering()
    {
        if (Mathf.Abs(currentSpeed) < 0.5f) return;

        float speedFactor = Mathf.Abs(currentSpeed) / maxSpeed;

        // Steering effectiveness curve - responsive at low speed, stable at high
        float steerEffectiveness = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(speedFactor * 2.5f));
        steerEffectiveness *= Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(speedFactor - 0.25f));

        if (isDrifting)
        {
            steerEffectiveness *= driftSteerBoost;
        }

        // Handbrake gives much tighter turning like Rocket League
        if (isHandbraking)
        {
            steerEffectiveness *= handbrakeTurnBoost;
        }

        float direction = currentSpeed >= 0 ? 1f : -1f;
        float steerAmount = smoothSteerInput * steeringSpeed * steerEffectiveness * direction * Time.fixedDeltaTime;

        Quaternion deltaRotation = Quaternion.Euler(0f, steerAmount, 0f);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, rb.rotation * deltaRotation, steeringTightness));
    }

    void ApplyGrip()
    {
        Vector3 forwardVel = transform.forward * Vector3.Dot(rb.linearVelocity, transform.forward);
        Vector3 sidewaysVel = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);

        float grip = isDrifting ? driftGrip : normalGrip;

        // Handbrake drastically reduces grip for powerslide like Rocket League
        if (isHandbraking)
        {
            grip = handbrakeGrip;
        }

        // Progressive grip loss at higher speeds
        if (SpeedPercent > 0.7f)
        {
            grip *= Mathf.Lerp(1f, 0.92f, (SpeedPercent - 0.7f) / 0.3f);
        }

        Vector3 targetVel = forwardVel + sidewaysVel * (1f - grip);
        targetVel.y = rb.linearVelocity.y;

        // Smooth grip application
        rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, targetVel, grip * 0.95f);
    }

    void ApplyAirControl()
    {
        // Rocket League style air control
        var gp = Gamepad.current;

        // Get pitch input - use RAW input for air control (separate from drive input)
        // This way holding down tilts up even if also holding accelerate
        float rawPitchInput = 0f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) rawPitchInput = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) rawPitchInput = -1f;

        if (gp != null)
        {
            float stickY = gp.leftStick.ReadValue().y;
            if (Mathf.Abs(stickY) > 0.1f) rawPitchInput = stickY;
        }

        // Forward on stick = tilt nose DOWN, Back on stick = tilt nose UP
        float pitchInput = rawPitchInput;

        // Get roll input (Q/E on keyboard, or bumpers on controller)
        float rollInput = 0f;
        if (Input.GetKey(KeyCode.Q)) rollInput = 1f;
        if (Input.GetKey(KeyCode.E)) rollInput = -1f;
        if (gp != null)
        {
            if (gp.leftShoulder.isPressed) rollInput = 1f;
            if (gp.rightShoulder.isPressed) rollInput = -1f;
        }

        // Get yaw input (left/right steering)
        float yawInput = smoothSteerInput;

        // Check horizontal speed
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float horizontalSpeed = horizontalVel.magnitude;
        bool hasInput = Mathf.Abs(pitchInput) > 0.1f || Mathf.Abs(rollInput) > 0.1f || Mathf.Abs(yawInput) > 0.1f;

        // === CHECK IF BUMPER IS NEAR GROUND ===
        // Raycast from front and back of car to detect ground proximity
        float carLength = 2.5f;
        float bumperCheckDist = 1.5f;

        Vector3 frontBumper = transform.position + transform.forward * carLength;
        Vector3 rearBumper = transform.position - transform.forward * carLength;

        bool frontNearGround = Physics.Raycast(frontBumper, Vector3.down, bumperCheckDist);
        bool rearNearGround = Physics.Raycast(rearBumper, Vector3.down, bumperCheckDist);
        bool centerNearGround = Physics.Raycast(transform.position, Vector3.down, bumperCheckDist);

        // Calculate how tilted we are
        Vector3 currentUp = transform.up;
        float tiltAmount = 1f - Vector3.Dot(currentUp, Vector3.up); // 0 = flat, 2 = upside down

        // === FORCE LEVEL WHEN BUMPER TOUCHING/NEAR GROUND ===
        // If any part is near ground and we're tilted, force level immediately
        bool nearGround = frontNearGround || rearNearGround || centerNearGround;

        if (nearGround && tiltAmount > 0.1f)
        {
            // Very strong auto-level - we're about to land wonky
            Vector3 levelTorque = Vector3.Cross(currentUp, Vector3.up);
            float levelStrength = 40f; // Strong correction
            rb.AddTorque(levelTorque * levelStrength, ForceMode.Acceleration);

            // Heavy damping to stop rotation
            rb.angularVelocity = rb.angularVelocity * 0.8f;

            // Push down to help land
            rb.AddForce(Vector3.down * 15f, ForceMode.Acceleration);
        }
        // === AUTO-LEVEL WHEN SLOW/NO INPUT ===
        else if (horizontalSpeed < 8f && !hasInput)
        {
            if (tiltAmount > 0.05f)
            {
                // Strong auto-level torque - car wants to be flat
                Vector3 levelTorque = Vector3.Cross(currentUp, Vector3.up);
                float levelStrength = 25f * (1f - horizontalSpeed / 8f); // Stronger when slower
                rb.AddTorque(levelTorque * levelStrength, ForceMode.Acceleration);

                // Extra damping when auto-leveling
                rb.angularVelocity = rb.angularVelocity * 0.9f;
            }
        }
        else
        {
            // === MANUAL AIR CONTROL ===
            // Check current tilt angles to enforce limits
            float currentPitch = Vector3.Angle(transform.forward, Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized);
            if (Vector3.Dot(transform.forward, Vector3.up) < 0) currentPitch = -currentPitch; // Nose down is negative

            float currentRoll = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);

            // Only allow tilt input if not at max, or if input would reduce tilt
            float effectivePitchInput = pitchInput;
            float effectiveRollInput = rollInput;

            // Clamp pitch - don't allow more tilt if at limit
            if (Mathf.Abs(currentPitch) > maxTiltAngle)
            {
                // Only allow input that reduces the tilt
                if (currentPitch > 0 && pitchInput > 0) effectivePitchInput = 0; // Nose up, trying to go more up
                if (currentPitch < 0 && pitchInput < 0) effectivePitchInput = 0; // Nose down, trying to go more down
            }

            // Clamp roll - don't allow more tilt if at limit
            if (Mathf.Abs(currentRoll) > maxTiltAngle)
            {
                if (currentRoll > 0 && rollInput < 0) effectiveRollInput = 0;
                if (currentRoll < 0 && rollInput > 0) effectiveRollInput = 0;
            }

            // Apply rotations with limits
            Vector3 torque = Vector3.zero;
            torque += transform.right * effectivePitchInput * airPitchSpeed;   // Pitch
            torque += transform.forward * effectiveRollInput * airRollSpeed;    // Roll
            torque += transform.up * yawInput * airYawSpeed;                    // Yaw (no limit)

            rb.AddTorque(torque, ForceMode.Acceleration);

            // Light damping for smooth control
            rb.angularVelocity = rb.angularVelocity * 0.95f;
        }

        // === SOARING EFFECT ===
        // When nose is tilted up AND moving fast AND high enough, car generates lift
        float nosePitch = Vector3.Dot(transform.forward, Vector3.up);

        if (nosePitch > 0.1f && horizontalSpeed > 10f && !nearGround)
        {
            // More lift when nose is higher, scaled by speed
            float liftAmount = nosePitch * airSoarLift * Mathf.Clamp01(horizontalSpeed / 20f);

            // Lift acts upward
            rb.AddForce(Vector3.up * liftAmount, ForceMode.Acceleration);

            // Also push slightly forward when soaring
            rb.AddForce(transform.forward * liftAmount * 0.5f, ForceMode.Acceleration);
        }

        // Normal gravity still applies
        rb.AddForce(Vector3.down * 3f, ForceMode.Acceleration);
    }

    void ClampSpeed()
    {
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float limit = (smoothMoveInput < 0 && currentSpeed < 0) ? reverseMaxSpeed : MaxSpeed;

        if (horizontalVel.magnitude > limit)
        {
            horizontalVel = horizontalVel.normalized * limit;
            rb.linearVelocity = new Vector3(horizontalVel.x, rb.linearVelocity.y, horizontalVel.z);
        }
    }

    void UpdateBodyTilt()
    {
        if (bodyTransform == null) return;

        // Smooth body tilt for visual feedback
        float tiltZ = -smoothSteerInput * 6f * SpeedPercent;
        float tiltX = -smoothMoveInput * 2.5f * (isBoosting ? 1.3f : 1f);

        Quaternion targetRot = Quaternion.Euler(tiltX, 0, tiltZ);
        bodyTransform.localRotation = Quaternion.Slerp(bodyTransform.localRotation, targetRot, 6f * Time.deltaTime);
    }

    void UpdateDriftScore()
    {
        if (isDrifting && isGrounded)
        {
            float driftAngle = Vector3.Angle(rb.linearVelocity, transform.forward);
            currentDriftScore += driftAngle * SpeedPercent * Time.deltaTime;
        }
        else if (currentDriftScore > 0)
        {
            driftScore += currentDriftScore;
            currentDriftScore = 0;
        }
    }

    void StartBoost()
    {
        isBoosting = true;
        boostTimer = boostDuration;
        boostCooldownTimer = boostCooldown;

        // Always push forward with a strong impulse (works in air too)
        rb.AddForce(transform.forward * 12f, ForceMode.VelocityChange);

        var cam = Camera.main?.GetComponent<CameraFollow>();
        cam?.OnBoostStart();

        // Play boost sound
        var audio = GetComponent<CarAudio>();
        audio?.PlayBoostStart();
    }

    void UpdateBoost()
    {
        // Timer management in Update
        if (isBoosting)
        {
            boostTimer -= Time.deltaTime;
            if (boostTimer <= 0)
            {
                isBoosting = false;
                var cam = Camera.main?.GetComponent<CameraFollow>();
                cam?.OnBoostEnd();
            }
        }

        if (boostCooldownTimer > 0)
            boostCooldownTimer -= Time.deltaTime;
    }

    void ApplyBoostForce()
    {
        // Called in FixedUpdate for smooth physics
        if (isBoosting)
        {
            rb.AddForce(transform.forward * 28f, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision col)
    {
        float impact = col.relativeVelocity.magnitude;

        // Check if this is a wall/barrier collision for bounce recovery
        bool isWall = IsWallCollision(col);

        if (isWall && impact > wallBounceMinSpeed)
        {
            // Wall bounce recovery - push car away and rotate slightly
            ApplyWallBounceRecovery(col);
        }

        if (impact > 5f)
        {
            // Reduce velocity loss when bouncing off walls (recovery-friendly)
            rb.linearVelocity *= isWall ? 0.7f : 0.55f;

            var cam = Camera.main?.GetComponent<CameraFollow>();
            cam?.Shake(impact * 0.03f);

            // Play impact sound
            var audio = GetComponent<CarAudio>();
            audio?.OnCollision(impact);

            // Trigger sparks on hard impacts
            if (impact > 8f)
            {
                var effects = GetComponent<CarEffects>();
                effects?.TriggerSparks();
            }

            if (impact > 12f && isBoosting)
            {
                isBoosting = false;
                cam?.OnBoostEnd();
            }

            currentDriftScore = 0;
        }
    }

    bool IsWallCollision(Collision col)
    {
        GameObject obj = col.gameObject;

        // Exclude other vehicles (cars, cops) - these shouldn't trigger wall bounce
        string name = obj.name.ToLower();
        if (name.Contains("car") || name.Contains("cop") || name.Contains("police") || name.Contains("vehicle"))
            return false;

        // Exclude ground/floor surfaces by name
        if (name.Contains("ground") || name.Contains("floor") || name.Contains("terrain") ||
            name.Contains("road") || name.Contains("track") || name.Contains("grass") ||
            name.Contains("parking") || name.Contains("lot") || name.Contains("drive") || name.Contains("lane"))
            return false;

        // Check by tag - exclude vehicles
        if (obj.CompareTag("Player") || obj.CompareTag("Enemy") || obj.CompareTag("Vehicle"))
            return false;

        // Exclude ground/floor surfaces by tag
        if (obj.CompareTag("Ground") || obj.CompareTag("Terrain"))
            return false;

        // Check contact normal - if pointing mostly upward, it's a floor/ground surface, not a wall
        Vector3 avgNormal = Vector3.zero;
        foreach (ContactPoint contact in col.contacts)
        {
            avgNormal += contact.normal;
        }
        avgNormal = avgNormal.normalized;

        // If the normal is pointing mostly upward (Y > 0.7), it's a ground surface
        if (avgNormal.y > 0.7f)
            return false;

        // Check if object has a Rigidbody that isn't kinematic - dynamic objects aren't walls
        Rigidbody otherRb = obj.GetComponent<Rigidbody>();
        if (otherRb != null && !otherRb.isKinematic)
            return false;

        // Check if this is a CarController (another car)
        if (obj.GetComponent<CarController>() != null)
            return false;

        // Check parent objects too (collider might be on child)
        Transform parent = obj.transform.parent;
        if (parent != null)
        {
            string parentName = parent.name.ToLower();
            if (parentName.Contains("car") || parentName.Contains("cop") || parentName.Contains("police") || parentName.Contains("vehicle"))
                return false;
            // Also check parent for ground-related names
            if (parentName.Contains("ground") || parentName.Contains("floor") || parentName.Contains("terrain") ||
                parentName.Contains("road") || parentName.Contains("track") || parentName.Contains("grass") ||
                parentName.Contains("parking") || parentName.Contains("lot") || parentName.Contains("drive") || parentName.Contains("lane"))
                return false;
            if (parent.GetComponent<CarController>() != null)
                return false;
        }

        // Everything else is treated as a wall/obstacle for bounce recovery purposes
        // This includes: barriers, walls, buildings, rocks, trees, curbs, props, etc.
        return true;
    }

    void ApplyWallBounceRecovery(Collision col)
    {
        // Get the average contact normal (direction away from wall)
        Vector3 bounceDirection = Vector3.zero;
        foreach (ContactPoint contact in col.contacts)
        {
            bounceDirection += contact.normal;
        }
        bounceDirection = bounceDirection.normalized;

        // Apply impulse force pushing car away from the wall (HORIZONTAL ONLY)
        // Scale force based on impact but cap it for predictability
        float impactScale = Mathf.Clamp(col.relativeVelocity.magnitude / 20f, 0.3f, 1.0f);
        Vector3 bounceForce = bounceDirection * wallBounceForce * impactScale;
        bounceForce.y = 0; // Keep bounce purely horizontal to avoid ground clipping
        rb.AddForce(bounceForce, ForceMode.VelocityChange);

        // Ensure car doesn't have downward velocity after bounce
        Vector3 vel = rb.linearVelocity;
        if (vel.y < 0) vel.y = 0;
        rb.linearVelocity = vel;

        // Keep car above ground level
        Vector3 pos = transform.position;
        if (pos.y < 0.5f)
        {
            pos.y = 0.5f;
            transform.position = pos;
        }

        // Calculate rotation direction - rotate away from the wall
        // Cross product of up and bounce direction gives rotation axis
        // We want to angle the car so it's pointing more away from the wall
        Vector3 carForward = transform.forward;
        float dotToWall = Vector3.Dot(carForward, -bounceDirection);

        // Only rotate if car was heading into the wall
        if (dotToWall > 0.2f)
        {
            // Determine which way to rotate based on which side hit the wall
            Vector3 avgContactPoint = Vector3.zero;
            foreach (ContactPoint contact in col.contacts)
            {
                avgContactPoint += contact.point;
            }
            avgContactPoint /= col.contacts.Length;

            // Compare contact point to car center to determine rotation direction
            Vector3 localContact = transform.InverseTransformPoint(avgContactPoint);
            float rotationSign = localContact.x > 0 ? -1f : 1f; // Hit on right? Rotate left

            // Apply torque to rotate car away from wall
            Vector3 torque = Vector3.up * wallBounceTorque * rotationSign * impactScale;
            rb.AddTorque(torque, ForceMode.VelocityChange);
        }
    }

    void OnCollisionStay(Collision col)
    {
        // Ledge climbing assistance - help car get over small obstacles
        if (!isGrounded || currentSpeed < 2f) return;

        foreach (ContactPoint contact in col.contacts)
        {
            // Check if we're hitting something at wheel height (small ledge)
            float contactHeight = contact.point.y - transform.position.y;

            // If contact is low (below body center) and normal is mostly horizontal
            if (contactHeight < 0.5f && contactHeight > -0.3f)
            {
                float horizontalNormal = Mathf.Abs(contact.normal.x) + Mathf.Abs(contact.normal.z);

                // If hitting a vertical surface (ledge/curb)
                if (horizontalNormal > 0.7f && Mathf.Abs(contact.normal.y) < 0.5f)
                {
                    // Check if moving towards the obstacle
                    float dotToward = Vector3.Dot(rb.linearVelocity.normalized, -contact.normal);

                    if (dotToward > 0.3f)
                    {
                        // Small upward boost to help climb over
                        float climbForce = Mathf.Clamp(currentSpeed * 0.15f, 2f, 8f);
                        rb.AddForce(Vector3.up * climbForce, ForceMode.Acceleration);

                        // Slight push forward to maintain momentum
                        rb.AddForce(transform.forward * 3f, ForceMode.Acceleration);
                    }
                }
            }
        }
    }
}
