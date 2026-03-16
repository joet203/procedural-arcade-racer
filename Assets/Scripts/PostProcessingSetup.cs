using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Sets up Tunic-style post-processing at runtime.
/// Attach to Main Camera or any GameObject in the scene.
/// </summary>
public class PostProcessingSetup : MonoBehaviour
{
    [Header("Style Presets")]
    public PostProcessingStyle style = PostProcessingStyle.TunicClean;

    [Header("Manual Overrides")]
    public bool useManualSettings = false;
    [Range(0, 1)] public float bloomIntensity = 0.5f;
    [Range(-100, 100)] public float saturation = 10f;
    [Range(-100, 100)] public float contrast = 10f;
    [Range(0, 1)] public float vignetteIntensity = 0.25f;

    private PostProcessVolume volume;
    private PostProcessLayer layer;

    public enum PostProcessingStyle
    {
        TunicClean,      // Soft bloom, light AO, vibrant colors
        TunicDramatic,   // More contrast, stronger vignette
        Retro32Bit,      // Color banding, chromatic aberration
        NightDrive,      // High contrast, neon bloom
        SunnyDay         // Bright, warm, soft
    }

    void Start()
    {
        SetupPostProcessing();
    }

    void SetupPostProcessing()
    {
        // Find or create camera
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("PostProcessingSetup: No main camera found!");
            return;
        }

        // Add Post Process Layer to camera if not present
        layer = cam.GetComponent<PostProcessLayer>();
        if (layer == null)
        {
            layer = cam.gameObject.AddComponent<PostProcessLayer>();
            layer.volumeTrigger = cam.transform;
            layer.volumeLayer = LayerMask.GetMask("Default"); // Process all layers
            layer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
            layer.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.High;
        }

        // Create global volume
        GameObject volumeObj = new GameObject("PostProcessVolume");
        volume = volumeObj.AddComponent<PostProcessVolume>();
        volume.isGlobal = true;
        volume.priority = 1;

        // Create profile
        PostProcessProfile profile = ScriptableObject.CreateInstance<PostProcessProfile>();
        volume.profile = profile;

        // Apply style
        if (useManualSettings)
        {
            ApplyManualSettings(profile);
        }
        else
        {
            ApplyStyle(profile, style);
        }

        Debug.Log($"Post-processing setup complete: {style}");
    }

    void ApplyStyle(PostProcessProfile profile, PostProcessingStyle selectedStyle)
    {
        switch (selectedStyle)
        {
            case PostProcessingStyle.TunicClean:
                ApplyTunicClean(profile);
                break;
            case PostProcessingStyle.TunicDramatic:
                ApplyTunicDramatic(profile);
                break;
            case PostProcessingStyle.Retro32Bit:
                ApplyRetro32Bit(profile);
                break;
            case PostProcessingStyle.NightDrive:
                ApplyNightDrive(profile);
                break;
            case PostProcessingStyle.SunnyDay:
                ApplySunnyDay(profile);
                break;
        }
    }

    void ApplyTunicClean(PostProcessProfile profile)
    {
        // Bloom - very soft glow to prevent white blowout from headlights
        var bloom = profile.AddSettings<Bloom>();
        bloom.enabled.value = true;
        bloom.intensity.value = 0.1f;
        bloom.threshold.value = 1.6f;
        bloom.softKnee.value = 0.5f;
        bloom.diffusion.value = 4f;
        bloom.intensity.overrideState = true;
        bloom.threshold.overrideState = true;
        bloom.softKnee.overrideState = true;
        bloom.diffusion.overrideState = true;

        // Note: AO disabled - causes issues in Built-in RP without compute shaders
        // The bloom + color grading provide most of the Tunic look

        // Color Grading - vibrant but natural
        var colorGrading = profile.AddSettings<ColorGrading>();
        colorGrading.enabled.value = true;
        colorGrading.tonemapper.value = Tonemapper.ACES;
        colorGrading.saturation.value = 15f;
        colorGrading.contrast.value = 8f;
        colorGrading.temperature.value = 5f; // Slightly warm
        colorGrading.tonemapper.overrideState = true;
        colorGrading.saturation.overrideState = true;
        colorGrading.contrast.overrideState = true;
        colorGrading.temperature.overrideState = true;

        // Vignette - subtle frame
        var vignette = profile.AddSettings<Vignette>();
        vignette.enabled.value = true;
        vignette.intensity.value = 0.2f;
        vignette.smoothness.value = 0.5f;
        vignette.intensity.overrideState = true;
        vignette.smoothness.overrideState = true;
    }

    void ApplyTunicDramatic(PostProcessProfile profile)
    {
        // Start with clean, then amp it up
        ApplyTunicClean(profile);

        // Override with more dramatic settings
        var colorGrading = profile.GetSetting<ColorGrading>();
        colorGrading.contrast.value = 20f;
        colorGrading.saturation.value = 25f;

        var vignette = profile.GetSetting<Vignette>();
        vignette.intensity.value = 0.35f;

        var bloom = profile.GetSetting<Bloom>();
        bloom.intensity.value = 0.15f;
        bloom.threshold.value = 1.5f;
    }

    void ApplyRetro32Bit(PostProcessProfile profile)
    {
        // Bloom - subtle, high threshold to avoid blowout
        var bloom = profile.AddSettings<Bloom>();
        bloom.enabled.value = true;
        bloom.intensity.value = 0.12f;
        bloom.threshold.value = 1.5f;
        bloom.intensity.overrideState = true;
        bloom.threshold.overrideState = true;

        // Color Grading - slightly washed, limited palette feel
        var colorGrading = profile.AddSettings<ColorGrading>();
        colorGrading.enabled.value = true;
        colorGrading.tonemapper.value = Tonemapper.None;
        colorGrading.saturation.value = -5f;
        colorGrading.contrast.value = 15f;
        colorGrading.tonemapper.overrideState = true;
        colorGrading.saturation.overrideState = true;
        colorGrading.contrast.overrideState = true;

        // Chromatic Aberration - retro feel
        var chromaticAberration = profile.AddSettings<ChromaticAberration>();
        chromaticAberration.enabled.value = true;
        chromaticAberration.intensity.value = 0.15f;
        chromaticAberration.intensity.overrideState = true;

        // Grain - film grain
        var grain = profile.AddSettings<Grain>();
        grain.enabled.value = true;
        grain.intensity.value = 0.2f;
        grain.size.value = 1.5f;
        grain.intensity.overrideState = true;
        grain.size.overrideState = true;

        // Vignette
        var vignette = profile.AddSettings<Vignette>();
        vignette.enabled.value = true;
        vignette.intensity.value = 0.3f;
        vignette.intensity.overrideState = true;
    }

    void ApplyNightDrive(PostProcessProfile profile)
    {
        // Bloom - controlled neon glow, higher threshold to prevent headlight blowout
        var bloom = profile.AddSettings<Bloom>();
        bloom.enabled.value = true;
        bloom.intensity.value = 0.15f;
        bloom.threshold.value = 1.5f;
        bloom.diffusion.value = 6f;
        bloom.intensity.overrideState = true;
        bloom.threshold.overrideState = true;
        bloom.diffusion.overrideState = true;

        // Color Grading - high contrast, cool
        var colorGrading = profile.AddSettings<ColorGrading>();
        colorGrading.enabled.value = true;
        colorGrading.tonemapper.value = Tonemapper.ACES;
        colorGrading.saturation.value = 30f;
        colorGrading.contrast.value = 25f;
        colorGrading.temperature.value = -15f; // Cool blue
        colorGrading.tonemapper.overrideState = true;
        colorGrading.saturation.overrideState = true;
        colorGrading.contrast.overrideState = true;
        colorGrading.temperature.overrideState = true;

        // Chromatic Aberration - cyberpunk feel
        var chromaticAberration = profile.AddSettings<ChromaticAberration>();
        chromaticAberration.enabled.value = true;
        chromaticAberration.intensity.value = 0.25f;
        chromaticAberration.intensity.overrideState = true;

        // Vignette - strong
        var vignette = profile.AddSettings<Vignette>();
        vignette.enabled.value = true;
        vignette.intensity.value = 0.4f;
        vignette.intensity.overrideState = true;
    }

    void ApplySunnyDay(PostProcessProfile profile)
    {
        // Bloom - bright and soft but controlled threshold
        var bloom = profile.AddSettings<Bloom>();
        bloom.enabled.value = true;
        bloom.intensity.value = 0.12f;
        bloom.threshold.value = 1.5f;
        bloom.softKnee.value = 0.8f;
        bloom.diffusion.value = 6f;
        bloom.intensity.overrideState = true;
        bloom.threshold.overrideState = true;
        bloom.softKnee.overrideState = true;
        bloom.diffusion.overrideState = true;

        // Note: AO disabled - causes issues in Built-in RP

        // Color Grading - warm and bright
        var colorGrading = profile.AddSettings<ColorGrading>();
        colorGrading.enabled.value = true;
        colorGrading.tonemapper.value = Tonemapper.ACES;
        colorGrading.saturation.value = 20f;
        colorGrading.contrast.value = 5f;
        colorGrading.temperature.value = 15f; // Warm
        colorGrading.lift.value = new Vector4(1.02f, 1.02f, 1.0f, 0); // Slight lift
        colorGrading.tonemapper.overrideState = true;
        colorGrading.saturation.overrideState = true;
        colorGrading.contrast.overrideState = true;
        colorGrading.temperature.overrideState = true;
        colorGrading.lift.overrideState = true;

        // Vignette - very subtle
        var vignette = profile.AddSettings<Vignette>();
        vignette.enabled.value = true;
        vignette.intensity.value = 0.15f;
        vignette.intensity.overrideState = true;
    }

    void ApplyManualSettings(PostProcessProfile profile)
    {
        // Bloom - use manual intensity but ensure high threshold
        var bloom = profile.AddSettings<Bloom>();
        bloom.enabled.value = true;
        bloom.intensity.value = Mathf.Min(bloomIntensity, 0.2f); // Cap at 0.2 to prevent blowout
        bloom.threshold.value = 1.5f;
        bloom.intensity.overrideState = true;
        bloom.threshold.overrideState = true;

        // Note: AO disabled - causes issues in Built-in RP

        // Color Grading
        var colorGrading = profile.AddSettings<ColorGrading>();
        colorGrading.enabled.value = true;
        colorGrading.saturation.value = saturation;
        colorGrading.contrast.value = contrast;
        colorGrading.saturation.overrideState = true;
        colorGrading.contrast.overrideState = true;

        // Vignette
        var vignette = profile.AddSettings<Vignette>();
        vignette.enabled.value = true;
        vignette.intensity.value = vignetteIntensity;
        vignette.intensity.overrideState = true;
    }

    // Runtime style switching
    public void SetStyle(PostProcessingStyle newStyle)
    {
        style = newStyle;
        if (volume != null && volume.profile != null)
        {
            // Clear existing settings
            volume.profile.settings.Clear();
            ApplyStyle(volume.profile, newStyle);
        }
    }
}
