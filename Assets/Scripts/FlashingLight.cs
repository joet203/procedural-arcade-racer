using UnityEngine;

public class FlashingLight : MonoBehaviour
{
    public Color flashColor = Color.red;
    public float flashSpeed = 3f;

    private Light light;
    private Renderer meshRenderer;
    private float timer;

    void Start()
    {
        light = GetComponent<Light>();
        meshRenderer = GetComponentInParent<Renderer>();

        // Offset timer based on color (so red and blue alternate)
        if (flashColor == Color.blue)
        {
            timer = Mathf.PI; // 180 degrees out of phase
        }
    }

    void Update()
    {
        timer += Time.deltaTime * flashSpeed;

        // Pulsing intensity
        float intensity = Mathf.Sin(timer) * 0.5f + 0.5f;

        if (light != null)
        {
            light.intensity = intensity * 1.5f;  // Reduced from 5 to prevent white blowout
        }

        // Also pulse the mesh emission if available
        if (meshRenderer != null && meshRenderer.material != null)
        {
            Color emissive = flashColor * intensity * 0.8f;  // Reduced from 2 to prevent blowout
            meshRenderer.material.SetColor("_EmissionColor", emissive);
            meshRenderer.material.EnableKeyword("_EMISSION");
        }
    }
}
