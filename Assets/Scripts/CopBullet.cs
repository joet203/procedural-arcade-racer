using UnityEngine;

public class CopBullet : MonoBehaviour
{
    public float damage = 10f;

    private TrailRenderer trail;
    private Light bulletLight;

    void Start()
    {
        // Add dramatic tracer trail
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.startWidth = 0.35f;
        trail.endWidth = 0.05f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = new Color(1f, 0.3f, 0.2f, 1f);
        trail.endColor = new Color(1f, 0.1f, 0.1f, 0f);
        trail.numCapVertices = 4;
        trail.numCornerVertices = 4;

        // Add dynamic point light for dramatic effect
        bulletLight = gameObject.AddComponent<Light>();
        bulletLight.type = LightType.Point;
        bulletLight.color = new Color(1f, 0.3f, 0.2f);
        bulletLight.intensity = 3f;
        bulletLight.range = 6f;
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we hit the player
        CarController player = collision.gameObject.GetComponent<CarController>();
        if (player != null)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        // Create satisfying impact effect
        CreateImpactEffect(collision.contacts[0].point, collision.contacts[0].normal);

        // Destroy bullet on any collision
        Destroy(gameObject);
    }

    void CreateImpactEffect(Vector3 position, Vector3 normal)
    {
        // Spark particles - red/orange for cop bullets
        for (int i = 0; i < 10; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spark.name = "CopBulletSpark";
            spark.transform.position = position;
            spark.transform.localScale = Vector3.one * Random.Range(0.04f, 0.1f);
            Object.Destroy(spark.GetComponent<Collider>());

            Material sparkMat = new Material(Shader.Find("Standard"));
            Color sparkColor = new Color(1f, Random.Range(0.2f, 0.5f), 0.2f);
            sparkMat.color = sparkColor;
            sparkMat.SetColor("_EmissionColor", sparkColor * 5f);
            sparkMat.EnableKeyword("_EMISSION");
            spark.GetComponent<Renderer>().material = sparkMat;

            Rigidbody rb = spark.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 0.01f;
            Vector3 randomDir = (normal + Random.insideUnitSphere).normalized;
            rb.linearVelocity = randomDir * Random.Range(6f, 18f);

            Object.Destroy(spark, 0.6f);
        }

        // Impact flash - bright red
        GameObject flash = new GameObject("CopImpactFlash");
        flash.transform.position = position;
        Light impactLight = flash.AddComponent<Light>();
        impactLight.type = LightType.Point;
        impactLight.color = new Color(1f, 0.3f, 0.2f);
        impactLight.intensity = 6f;
        impactLight.range = 5f;
        Object.Destroy(flash, 0.12f);
    }
}
