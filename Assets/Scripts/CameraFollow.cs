using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public CarController car;

    [Header("Chase Position")]
    public float distance = 2.8f;
    public float height = 2.7f;
    public float smoothTime = 0.15f;
    public float rotationSmoothTime = 0.08f;

    [Header("Look At")]
    public float lookAheadDistance = 5f;
    public float lookHeight = 1.2f;

    [Header("Speed Effects")]
    public float minFOV = 58f;
    public float maxFOV = 72f;
    public float boostFOV = 88f;
    public float fovSmoothTime = 0.3f;

    [Header("Drift Camera")]
    public float driftOffsetAmount = 2.5f;
    public float driftRotationAmount = 6f;
    public float driftSmoothTime = 0.25f;

    [Header("Boost Effects")]
    public float boostDistanceAdd = 1.2f;
    public float boostHeightAdd = 0.3f;

    [Header("Subtle Motion")]
    public float breathingAmount = 0.08f;
    public float breathingSpeed = 1.2f;

    private Camera cam;
    private float currentFOV;
    private float fovVelocity;
    private float currentDriftOffset;
    private float currentDriftRotation;
    private float driftOffsetVelocity;
    private float driftRotationVelocity;
    private Vector3 positionVelocity;
    private float currentDistance;
    private float currentHeight;
    private float distanceVelocity;
    private float heightVelocity;
    private float rotationVelocity;
    private float currentRotationAngle;
    private Quaternion rotationVelocityQ;

    // Speed lines - subtle
    private LineRenderer[] speedLines;
    private int speedLineCount = 16;

    void Start()
    {
        cam = GetComponent<Camera>();
        cam.fieldOfView = minFOV;
        currentFOV = minFOV;
        currentDistance = distance;
        currentHeight = height;

        if (target != null && car == null)
            car = target.GetComponent<CarController>();

        if (target != null)
            currentRotationAngle = target.eulerAngles.y;

        CreateSpeedLines();
    }

    void CreateSpeedLines()
    {
        speedLines = new LineRenderer[speedLineCount];

        Material lineMat = new Material(Shader.Find("Sprites/Default"));

        for (int i = 0; i < speedLineCount; i++)
        {
            GameObject lineObj = new GameObject($"SpeedLine_{i}");
            lineObj.transform.SetParent(transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.015f;
            lr.endWidth = 0.008f;
            lr.material = lineMat;
            lr.startColor = new Color(1, 1, 1, 0);
            lr.endColor = new Color(1, 1, 1, 0);
            lr.useWorldSpace = false;

            speedLines[i] = lr;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        UpdatePosition();
        UpdateRotation();
        UpdateFOV();
        UpdateSpeedLines();
    }

    void UpdatePosition()
    {
        // Target distance and height with boost modification
        float boostT = car != null && car.IsBoosting ? 1f : 0f;
        float targetDist = distance + boostDistanceAdd * boostT;
        float targetHeight = height + boostHeightAdd * boostT;

        // Smooth distance and height
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDist, ref distanceVelocity, smoothTime * 1.5f);
        currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, smoothTime * 1.5f);

        // Smooth rotation following
        float targetRotationAngle = target.eulerAngles.y;
        currentRotationAngle = Mathf.SmoothDampAngle(currentRotationAngle, targetRotationAngle, ref rotationVelocity, rotationSmoothTime);

        // Drift offset - smooth lateral shift
        float targetDriftOffset = 0f;
        float targetDriftRot = 0f;

        if (car != null && car.IsDrifting)
        {
            targetDriftOffset = car.SteerInput * driftOffsetAmount;
            targetDriftRot = car.SteerInput * driftRotationAmount;
        }

        currentDriftOffset = Mathf.SmoothDamp(currentDriftOffset, targetDriftOffset, ref driftOffsetVelocity, driftSmoothTime);
        currentDriftRotation = Mathf.SmoothDamp(currentDriftRotation, targetDriftRot, ref driftRotationVelocity, driftSmoothTime);

        // Calculate camera position
        Quaternion rotation = Quaternion.Euler(0, currentRotationAngle + currentDriftRotation, 0);
        Vector3 offset = rotation * new Vector3(currentDriftOffset, 0, -currentDistance);

        Vector3 targetPos = target.position + offset;
        targetPos.y = target.position.y + currentHeight;

        // Subtle breathing motion for organic feel
        float breathOffset = Mathf.Sin(Time.time * breathingSpeed) * breathingAmount;
        targetPos.y += breathOffset;

        // Ultra-smooth position
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref positionVelocity, smoothTime);
    }

    void UpdateRotation()
    {
        // Look at point ahead of car
        Vector3 lookTarget = target.position + target.forward * lookAheadDistance;
        lookTarget.y = target.position.y + lookHeight;

        // Calculate target rotation
        Vector3 direction = lookTarget - transform.position;
        if (direction.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Add subtle tilt when steering
            if (car != null)
            {
                float tilt = -car.SteerInput * 3f * car.SpeedPercent;
                targetRotation *= Quaternion.Euler(0, 0, tilt);
            }

            // Very smooth rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 8f);
        }
    }

    void UpdateFOV()
    {
        if (car == null) return;

        float targetFOV;

        if (car.IsBoosting)
        {
            targetFOV = Mathf.Lerp(maxFOV, boostFOV, car.SpeedPercent);
        }
        else
        {
            targetFOV = Mathf.Lerp(minFOV, maxFOV, car.SpeedPercent);
        }

        if (car.IsDrifting)
        {
            targetFOV += 3f;
        }

        // Smooth FOV transitions
        currentFOV = Mathf.SmoothDamp(currentFOV, targetFOV, ref fovVelocity, fovSmoothTime);
        cam.fieldOfView = currentFOV;
    }

    void UpdateSpeedLines()
    {
        if (car == null) return;

        // Only show at high speeds, very subtle
        float alpha = 0f;
        if (car.SpeedPercent > 0.7f)
        {
            alpha = (car.SpeedPercent - 0.7f) / 0.3f * 0.25f;
        }
        if (car.IsBoosting)
        {
            alpha = 0.4f;
        }

        for (int i = 0; i < speedLines.Length; i++)
        {
            if (alpha > 0.01f)
            {
                // Position lines around edges of view
                float angle = (i / (float)speedLineCount) * Mathf.PI * 2f + Time.time * 0.3f;
                float radius = 2.5f + (i % 3) * 0.5f;
                float depth = 10f + (i % 4) * 2f;

                Vector3 start = new Vector3(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius,
                    depth
                );
                Vector3 end = start + Vector3.forward * (2f + car.SpeedPercent * 2f);

                speedLines[i].SetPosition(0, start);
                speedLines[i].SetPosition(1, end);

                // Subtle white or warm orange during boost
                Color lineColor = car.IsBoosting
                    ? new Color(1f, 0.8f, 0.5f, alpha)
                    : new Color(1f, 1f, 1f, alpha * 0.6f);

                speedLines[i].startColor = lineColor;
                speedLines[i].endColor = new Color(lineColor.r, lineColor.g, lineColor.b, 0);
            }
            else
            {
                speedLines[i].startColor = new Color(1, 1, 1, 0);
                speedLines[i].endColor = new Color(1, 1, 1, 0);
            }
        }
    }

    public void Shake(float intensity)
    {
        // Subtle position offset instead of harsh shake
        positionVelocity += Random.insideUnitSphere * intensity * 2f;
    }

    public void OnBoostStart()
    {
        Shake(0.15f);
    }

    public void OnBoostEnd()
    {
        // Subtle effect
    }
}
