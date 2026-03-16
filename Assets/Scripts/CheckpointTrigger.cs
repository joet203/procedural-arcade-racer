using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public int checkpointIndex = 0;
    public bool isFinishLine = false;

    private MeshRenderer meshRenderer;
    private Color originalColor;
    private float flashTimer;
    private bool wasHit = false;

    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            originalColor = meshRenderer.material.color;
        }

        // Make trigger area
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        if (flashTimer > 0)
        {
            flashTimer -= Time.deltaTime;

            if (meshRenderer != null)
            {
                float t = Mathf.PingPong(flashTimer * 10f, 1f);
                Color flashColor = isFinishLine
                    ? Color.Lerp(Color.white, Color.green, t)
                    : Color.Lerp(originalColor, Color.yellow, t);
                meshRenderer.material.color = flashColor;
            }

            if (flashTimer <= 0 && meshRenderer != null)
            {
                meshRenderer.material.color = wasHit ? new Color(0.3f, 0.8f, 0.3f, 0.5f) : originalColor;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        CarController car = other.GetComponent<CarController>();
        if (car == null)
            car = other.GetComponentInParent<CarController>();

        if (car != null)
        {
            if (LapSystem.Instance != null)
            {
                LapSystem.Instance.OnCheckpointHit(checkpointIndex, isFinishLine);
            }

            // Play checkpoint sound
            if (!isFinishLine && !wasHit)
            {
                var audio = car.GetComponent<CarAudio>();
                audio?.PlayCheckpoint();
            }

            // Visual feedback
            flashTimer = 0.5f;
            wasHit = true;

            // Reset hit state on new lap for non-finish-line checkpoints
            if (isFinishLine)
            {
                ResetAllCheckpoints();
            }
        }
    }

    void ResetAllCheckpoints()
    {
        CheckpointTrigger[] allCheckpoints = FindObjectsByType<CheckpointTrigger>(FindObjectsSortMode.None);
        foreach (var cp in allCheckpoints)
        {
            if (!cp.isFinishLine)
            {
                cp.wasHit = false;
                if (cp.meshRenderer != null)
                {
                    cp.meshRenderer.material.color = cp.originalColor;
                }
            }
        }
    }
}
