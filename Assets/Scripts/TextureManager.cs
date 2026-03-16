using UnityEngine;

/// <summary>
/// Manages loading and applying custom textures for ground, cars, etc.
/// Place textures in Assets/Resources/Textures/
///
/// TEXTURE SPECS:
/// - Grass/Ground: 512x512 or 1024x1024, seamless/tileable PNG
/// - Car Wrap: 1024x1024 or 2048x2048 PNG
/// - Road: 512x512 or 1024x1024, seamless PNG
/// </summary>
public class TextureManager : MonoBehaviour
{
    public static TextureManager Instance { get; private set; }

    // Loaded textures
    public Texture2D GrassTexture { get; private set; }
    public Texture2D RoadTexture { get; private set; }
    public Texture2D CarWrapTexture { get; private set; }
    public Texture2D ProceduralAsphaltTexture { get; private set; }

    // Procedural textures with normal maps
    public Texture2D ProceduralGrassTexture { get; private set; }
    public Texture2D ProceduralGrassNormal { get; private set; }
    public Texture2D ProceduralAsphaltNormal { get; private set; }
    public Texture2D ProceduralConcreteTexture { get; private set; }
    public Texture2D ProceduralConcreteNormal { get; private set; }

    // Tiling settings
    public float grassTiling = 50f;
    public float roadTiling = 10f;

    // Asphalt texture settings
    public int asphaltTextureSize = 512;
    public float asphaltTiling = 0.15f; // Tiles per world unit

    // Grass texture settings
    public int grassTextureSize = 512;
    public float proceduralGrassTiling = 0.08f; // Tiles per world unit

    // Concrete/barrier texture settings
    public int concreteTextureSize = 512;
    public float concreteTiling = 0.2f; // Tiles per world unit

    void Awake()
    {
        Instance = this;
        LoadTextures();
        GenerateProceduralAsphalt();
        GenerateProceduralAsphaltNormal();
        GenerateProceduralGrass();
        GenerateProceduralConcrete();
    }

    void LoadTextures()
    {
        // Load from Resources/Textures folder
        // Users can drop their textures there and they'll be automatically loaded
        GrassTexture = Resources.Load<Texture2D>("Textures/grass");
        RoadTexture = Resources.Load<Texture2D>("Textures/road");
        CarWrapTexture = Resources.Load<Texture2D>("Textures/car_wrap");

        if (GrassTexture != null)
        {
            GrassTexture.wrapMode = TextureWrapMode.Repeat;
            GrassTexture.filterMode = FilterMode.Bilinear;
            Debug.Log("Loaded grass texture: " + GrassTexture.width + "x" + GrassTexture.height);
        }

        if (RoadTexture != null)
        {
            RoadTexture.wrapMode = TextureWrapMode.Repeat;
            RoadTexture.filterMode = FilterMode.Bilinear;
            Debug.Log("Loaded road texture: " + RoadTexture.width + "x" + RoadTexture.height);
        }

        if (CarWrapTexture != null)
        {
            CarWrapTexture.filterMode = FilterMode.Bilinear;
            Debug.Log("Loaded car wrap texture: " + CarWrapTexture.width + "x" + CarWrapTexture.height);
        }
    }

    /// <summary>
    /// Creates a tiled ground material with grass texture and normal map
    /// </summary>
    public Material CreateGrassMaterial(Color fallbackColor)
    {
        Material mat = new Material(Shader.Find("Standard"));

        if (GrassTexture != null)
        {
            mat.mainTexture = GrassTexture;
            mat.mainTextureScale = new Vector2(grassTiling, grassTiling);
            mat.color = Color.white; // Don't tint the texture
        }
        else if (ProceduralGrassTexture != null)
        {
            mat.mainTexture = ProceduralGrassTexture;
            mat.mainTextureScale = new Vector2(grassTiling, grassTiling);
            mat.color = Color.white;

            // Apply normal map for grass blade depth
            if (ProceduralGrassNormal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", ProceduralGrassNormal);
                mat.SetTextureScale("_BumpMap", new Vector2(grassTiling, grassTiling));
                mat.SetFloat("_BumpScale", 0.8f); // Normal map intensity
            }
        }
        else
        {
            mat.color = fallbackColor;
        }

        mat.SetFloat("_Glossiness", 0.1f);
        mat.SetFloat("_Metallic", 0f);

        return mat;
    }

    /// <summary>
    /// Creates a tiled road material
    /// </summary>
    public Material CreateRoadMaterial(Color fallbackColor)
    {
        Material mat = new Material(Shader.Find("Standard"));

        if (RoadTexture != null)
        {
            mat.mainTexture = RoadTexture;
            mat.mainTextureScale = new Vector2(roadTiling, roadTiling);
            mat.color = Color.white;
        }
        else
        {
            mat.color = fallbackColor;
        }

        mat.SetFloat("_Glossiness", 0.3f);
        mat.SetFloat("_Metallic", 0f);

        return mat;
    }

    /// <summary>
    /// Creates a car body material with optional wrap texture
    /// </summary>
    public Material CreateCarMaterial(Color fallbackColor, float metallic = 0.75f, float glossiness = 0.88f)
    {
        Material mat = new Material(Shader.Find("Standard"));

        if (CarWrapTexture != null)
        {
            mat.mainTexture = CarWrapTexture;
            mat.color = Color.white; // Don't tint
        }
        else
        {
            mat.color = fallbackColor;
        }

        mat.SetFloat("_Metallic", metallic);
        mat.SetFloat("_Glossiness", glossiness);

        return mat;
    }

    /// <summary>
    /// Applies car wrap texture to all renderers on a car body
    /// </summary>
    public void ApplyCarWrap(GameObject carBody)
    {
        if (CarWrapTexture == null) return;

        foreach (Renderer r in carBody.GetComponentsInChildren<Renderer>())
        {
            // Only apply to main body parts, not trim/wheels
            if (r.material.color.b > 0.3f) // Blue-ish = main body
            {
                r.material.mainTexture = CarWrapTexture;
                r.material.color = Color.white;
            }
        }
    }

    /// <summary>
    /// Reload textures at runtime (for hot-reloading during development)
    /// </summary>
    public void ReloadTextures()
    {
        Resources.UnloadUnusedAssets();
        LoadTextures();
        GenerateProceduralAsphalt();
        GenerateProceduralAsphaltNormal();
        GenerateProceduralGrass();
        GenerateProceduralConcrete();
        Debug.Log("Textures reloaded!");
    }

    /// <summary>
    /// Generates a procedural asphalt texture with realistic grain and subtle cracks
    /// </summary>
    void GenerateProceduralAsphalt()
    {
        int size = asphaltTextureSize;
        ProceduralAsphaltTexture = new Texture2D(size, size, TextureFormat.RGB24, true);
        ProceduralAsphaltTexture.wrapMode = TextureWrapMode.Repeat;
        ProceduralAsphaltTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[size * size];

        // Base asphalt color (dark gray)
        float baseGray = 0.18f;

        // Random seed for consistent results
        Random.InitState(42);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;

                // Start with base gray
                float gray = baseGray;

                // Add multi-octave Perlin noise for color variation
                float noiseScale1 = 0.02f;
                float noiseScale2 = 0.08f;
                float noiseScale3 = 0.25f;

                float noise1 = Mathf.PerlinNoise(x * noiseScale1, y * noiseScale1) - 0.5f;
                float noise2 = Mathf.PerlinNoise(x * noiseScale2 + 100, y * noiseScale2 + 100) - 0.5f;
                float noise3 = Mathf.PerlinNoise(x * noiseScale3 + 200, y * noiseScale3 + 200) - 0.5f;

                // Combine noise octaves
                gray += noise1 * 0.04f;  // Large scale variation
                gray += noise2 * 0.025f; // Medium scale variation
                gray += noise3 * 0.015f; // Fine detail

                // Add fine grain/aggregate texture
                float grain = (Random.value - 0.5f) * 0.06f;
                gray += grain;

                // Add small aggregate stones (brighter spots)
                if (Random.value > 0.97f)
                {
                    gray += Random.Range(0.03f, 0.08f);
                }

                // Add darker aggregate (darker spots)
                if (Random.value > 0.96f)
                {
                    gray -= Random.Range(0.02f, 0.05f);
                }

                // Subtle cracks using domain-warped noise
                float crackNoise = GenerateCrackPattern(x, y, size);
                gray -= crackNoise * 0.08f;

                // Subtle weathering patches (slightly lighter areas)
                float weatherNoise = Mathf.PerlinNoise(x * 0.01f + 500, y * 0.01f + 500);
                if (weatherNoise > 0.7f)
                {
                    gray += (weatherNoise - 0.7f) * 0.1f;
                }

                // Clamp and add slight color variation (very subtle blue-gray tint)
                gray = Mathf.Clamp(gray, 0.08f, 0.35f);

                // Slight color variation - asphalt has subtle blue/brown undertones
                float r = gray;
                float g = gray;
                float b = gray + 0.01f; // Tiny blue tint

                // Some areas have slight warm (brownish) tint
                float warmNoise = Mathf.PerlinNoise(x * 0.015f + 300, y * 0.015f + 300);
                if (warmNoise > 0.6f)
                {
                    r += 0.008f;
                    b -= 0.005f;
                }

                pixels[idx] = new Color(r, g, b);
            }
        }

        ProceduralAsphaltTexture.SetPixels(pixels);
        ProceduralAsphaltTexture.Apply(true); // Generate mipmaps

        Debug.Log("Generated procedural asphalt texture: " + size + "x" + size);
    }

    /// <summary>
    /// Generates crack pattern using domain-warped noise
    /// </summary>
    float GenerateCrackPattern(int x, int y, int size)
    {
        // Domain warping for more natural crack shapes
        float warpScale = 0.03f;
        float warpX = Mathf.PerlinNoise(x * warpScale, y * warpScale) * 20f;
        float warpY = Mathf.PerlinNoise(x * warpScale + 50, y * warpScale + 50) * 20f;

        float crackX = x + warpX;
        float crackY = y + warpY;

        // Multiple crack layers at different scales
        float crack1 = Mathf.PerlinNoise(crackX * 0.05f, crackY * 0.05f);
        float crack2 = Mathf.PerlinNoise(crackX * 0.1f + 100, crackY * 0.1f + 100);

        // Sharp threshold for crack lines
        float crackValue = 0f;

        // Main cracks (sparse)
        if (crack1 > 0.48f && crack1 < 0.52f)
        {
            crackValue = 1f - Mathf.Abs(crack1 - 0.5f) * 25f;
            crackValue = Mathf.Clamp01(crackValue);
        }

        // Secondary cracks (finer)
        if (crack2 > 0.47f && crack2 < 0.53f)
        {
            float secondary = 1f - Mathf.Abs(crack2 - 0.5f) * 16f;
            secondary = Mathf.Clamp01(secondary) * 0.5f;
            crackValue = Mathf.Max(crackValue, secondary);
        }

        return crackValue;
    }

    /// <summary>
    /// Creates a road material with procedural asphalt texture and normal map
    /// Falls back to loaded texture if available, otherwise uses procedural
    /// </summary>
    public Material CreateAsphaltMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));

        // Prefer loaded texture, then procedural, then fallback color
        if (RoadTexture != null)
        {
            mat.mainTexture = RoadTexture;
            mat.mainTextureScale = new Vector2(roadTiling, roadTiling);
        }
        else if (ProceduralAsphaltTexture != null)
        {
            mat.mainTexture = ProceduralAsphaltTexture;
            // Calculate tiling based on typical road piece size
            // asphaltTiling is tiles per world unit

            // Apply normal map for road surface detail (bumps, cracks)
            if (ProceduralAsphaltNormal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", ProceduralAsphaltNormal);
                mat.SetFloat("_BumpScale", 0.6f); // Subtle normal map for asphalt
            }
        }
        else
        {
            mat.color = new Color(0.18f, 0.18f, 0.2f);
        }

        mat.SetFloat("_Glossiness", 0.25f); // Slightly rough
        mat.SetFloat("_Metallic", 0f);

        return mat;
    }

    /// <summary>
    /// Creates a concrete/barrier material with procedural texture and normal map
    /// </summary>
    public Material CreateConcreteMaterial(Color tint = default)
    {
        Material mat = new Material(Shader.Find("Standard"));

        if (ProceduralConcreteTexture != null)
        {
            mat.mainTexture = ProceduralConcreteTexture;
            mat.mainTextureScale = new Vector2(concreteTiling * 10f, concreteTiling * 10f);
            mat.color = tint == default ? Color.white : tint;

            // Apply normal map for concrete surface roughness
            if (ProceduralConcreteNormal != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", ProceduralConcreteNormal);
                mat.SetTextureScale("_BumpMap", new Vector2(concreteTiling * 10f, concreteTiling * 10f));
                mat.SetFloat("_BumpScale", 0.7f); // Medium normal map intensity
            }
        }
        else
        {
            mat.color = tint == default ? new Color(0.85f, 0.85f, 0.9f) : tint;
        }

        mat.SetFloat("_Glossiness", 0.15f); // Matte concrete
        mat.SetFloat("_Metallic", 0f);

        return mat;
    }

    /// <summary>
    /// Applies asphalt texture to a road piece with proper tiling based on world scale
    /// </summary>
    public void ApplyAsphaltToRoad(Renderer renderer, Vector3 worldScale)
    {
        if (ProceduralAsphaltTexture == null && RoadTexture == null)
        {
            return;
        }

        Material mat = renderer.material;
        Texture2D tex = RoadTexture != null ? RoadTexture : ProceduralAsphaltTexture;

        mat.mainTexture = tex;

        // Calculate tiling based on world scale
        // We want the texture to repeat at a consistent world-space rate
        float tilingX = worldScale.x * asphaltTiling;
        float tilingZ = worldScale.z * asphaltTiling;

        mat.mainTextureScale = new Vector2(tilingX, tilingZ);
        mat.color = Color.white; // Don't tint the texture

        // Apply normal map for road surface detail
        if (ProceduralAsphaltNormal != null && RoadTexture == null)
        {
            mat.EnableKeyword("_NORMALMAP");
            mat.SetTexture("_BumpMap", ProceduralAsphaltNormal);
            mat.SetTextureScale("_BumpMap", new Vector2(tilingX, tilingZ));
            mat.SetFloat("_BumpScale", 0.6f);
        }
    }

    // ==================== PROCEDURAL DUNKIN TEXTURES ====================

    /// <summary>
    /// Generates a procedural brick/stucco texture for Dunkin building exterior.
    /// Uses Dunkin brand colors for accent bricks.
    /// </summary>
    public static Texture2D GenerateDunkinBuildingTexture(int width = 512, int height = 512)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);

        // Dunkin brand colors
        Color dunkinOrange = new Color(1f, 0.4f, 0.2f);
        Color dunkinPink = new Color(0.85f, 0.2f, 0.5f);

        // Base stucco/building color (off-white with slight warmth)
        Color baseStucco = new Color(0.92f, 0.90f, 0.85f);
        Color mortarColor = new Color(0.75f, 0.73f, 0.70f);

        // Brick dimensions (in texture pixels)
        int brickWidth = width / 8;
        int brickHeight = height / 16;
        int mortarThickness = 2;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Determine which row of bricks we're in
                int row = y / brickHeight;
                bool isOddRow = (row % 2) == 1;

                // Offset for brick pattern (staggered)
                int xOffset = isOddRow ? brickWidth / 2 : 0;
                int adjustedX = (x + xOffset) % width;

                // Check if we're in mortar (gap between bricks)
                bool inMortarX = (adjustedX % brickWidth) < mortarThickness;
                bool inMortarY = (y % brickHeight) < mortarThickness;

                Color pixelColor;

                if (inMortarX || inMortarY)
                {
                    // Mortar with slight noise
                    float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f) * 0.1f;
                    pixelColor = mortarColor + new Color(noise, noise, noise, 0);
                }
                else
                {
                    // Brick face
                    int brickIndex = (adjustedX / brickWidth) + row * 8;

                    // Add stucco texture noise
                    float stuccoNoise = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.15f;
                    float fineNoise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.05f;

                    // Occasionally add brand-colored accent bricks
                    float accentChance = Mathf.PerlinNoise(brickIndex * 0.3f, row * 0.5f);

                    if (accentChance > 0.85f)
                    {
                        // Orange accent brick
                        pixelColor = dunkinOrange;
                        pixelColor += new Color(stuccoNoise * 0.3f, stuccoNoise * 0.2f, stuccoNoise * 0.1f, 0);
                    }
                    else if (accentChance > 0.78f)
                    {
                        // Pink accent brick
                        pixelColor = dunkinPink;
                        pixelColor += new Color(stuccoNoise * 0.2f, stuccoNoise * 0.1f, stuccoNoise * 0.2f, 0);
                    }
                    else
                    {
                        // Regular stucco brick
                        pixelColor = baseStucco;
                        pixelColor += new Color(stuccoNoise - 0.075f, stuccoNoise - 0.075f, stuccoNoise - 0.075f, 0);
                        pixelColor += new Color(fineNoise - 0.025f, fineNoise - 0.025f, fineNoise - 0.025f, 0);
                    }

                    // Add slight variation per brick
                    float brickVariation = Mathf.Sin(brickIndex * 1.7f) * 0.03f;
                    pixelColor += new Color(brickVariation, brickVariation, brickVariation, 0);
                }

                // Clamp colors
                pixelColor.r = Mathf.Clamp01(pixelColor.r);
                pixelColor.g = Mathf.Clamp01(pixelColor.g);
                pixelColor.b = Mathf.Clamp01(pixelColor.b);
                pixelColor.a = 1f;

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    /// <summary>
    /// Generates a procedural sign texture for Dunkin branding.
    /// Features gradient background with stylized text area and decorative elements.
    /// </summary>
    public static Texture2D GenerateDunkinSignTexture(int width = 512, int height = 256)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);

        // Dunkin brand colors
        Color dunkinOrange = new Color(1f, 0.4f, 0.2f);
        Color dunkinPink = new Color(0.85f, 0.2f, 0.5f);
        Color darkOrange = new Color(0.8f, 0.25f, 0.1f);
        Color cream = new Color(1f, 0.98f, 0.9f);

        float centerX = width / 2f;
        float centerY = height / 2f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)width;
                float ny = y / (float)height;

                Color pixelColor;

                // Create gradient background from orange to pink
                float gradientT = nx;
                Color bgColor = Color.Lerp(dunkinOrange, dunkinPink, gradientT);

                // Add radial highlight in center
                float distFromCenter = Vector2.Distance(
                    new Vector2(x, y),
                    new Vector2(centerX, centerY)
                ) / (width * 0.5f);

                float highlightIntensity = 1f - Mathf.Clamp01(distFromCenter * 0.8f);
                bgColor = Color.Lerp(bgColor, cream, highlightIntensity * 0.3f);

                // Add horizontal stripe pattern
                float stripeY = Mathf.Sin(ny * Mathf.PI * 8f) * 0.5f + 0.5f;
                bgColor = Color.Lerp(bgColor, darkOrange, stripeY * 0.1f);

                // Create central text banner area (lighter rectangle)
                float bannerPadding = 0.15f;
                bool inBanner = nx > bannerPadding && nx < (1f - bannerPadding) &&
                               ny > 0.25f && ny < 0.75f;

                if (inBanner)
                {
                    // Banner background with rounded appearance (simulated)
                    float bannerX = (nx - bannerPadding) / (1f - 2f * bannerPadding);
                    float bannerY = (ny - 0.25f) / 0.5f;

                    // Edge darkening for depth
                    float edgeDist = Mathf.Min(bannerX, 1f - bannerX, bannerY, 1f - bannerY);
                    float edgeFade = Mathf.Clamp01(edgeDist * 8f);

                    Color bannerColor = Color.Lerp(cream, Color.white, edgeFade * 0.5f);

                    // Add subtle noise texture to banner
                    float bannerNoise = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.05f;
                    bannerColor -= new Color(bannerNoise, bannerNoise, bannerNoise, 0);

                    pixelColor = bannerColor;

                    // Create stylized "DUNKIN" letter blocks
                    float letterBlockWidth = 0.12f;
                    float letterSpacing = 0.14f;
                    float letterStartX = 0.09f;

                    for (int letterIdx = 0; letterIdx < 6; letterIdx++) // D-U-N-K-I-N
                    {
                        float letterCenterX = letterStartX + letterIdx * letterSpacing;
                        float letterLeft = letterCenterX - letterBlockWidth / 2f;
                        float letterRight = letterCenterX + letterBlockWidth / 2f;

                        if (bannerX > letterLeft && bannerX < letterRight &&
                            bannerY > 0.25f && bannerY < 0.75f)
                        {
                            // Alternate orange and pink letters
                            Color letterColor = (letterIdx % 2 == 0) ? dunkinOrange : dunkinPink;

                            // Add 3D bevel effect
                            float letterLocalX = (bannerX - letterLeft) / letterBlockWidth;
                            float letterLocalY = (bannerY - 0.25f) / 0.5f;

                            float bevelTop = Mathf.Clamp01((1f - letterLocalY) * 4f);
                            float bevelLeft = Mathf.Clamp01((1f - letterLocalX) * 4f);

                            letterColor = Color.Lerp(letterColor, Color.white, (bevelTop + bevelLeft) * 0.15f);

                            // Bottom/right shadow
                            float shadowBottom = Mathf.Clamp01(letterLocalY * 3f - 2f);
                            float shadowRight = Mathf.Clamp01(letterLocalX * 3f - 2f);
                            letterColor = Color.Lerp(letterColor, darkOrange, (shadowBottom + shadowRight) * 0.2f);

                            pixelColor = letterColor;
                        }
                    }
                }
                else
                {
                    pixelColor = bgColor;
                }

                // Add border/frame around entire sign
                float borderWidth = 0.03f;
                if (nx < borderWidth || nx > (1f - borderWidth) ||
                    ny < borderWidth || ny > (1f - borderWidth))
                {
                    // Gold/brown border
                    pixelColor = new Color(0.6f, 0.4f, 0.2f);

                    // Inner highlight
                    if ((nx > borderWidth * 0.5f && nx < borderWidth) ||
                        (ny > borderWidth * 0.5f && ny < borderWidth))
                    {
                        pixelColor = Color.Lerp(pixelColor, cream, 0.3f);
                    }
                }

                // Add corner donuts (circles in corners)
                float donutRadius = 0.08f;
                Vector2[] donutCenters = new Vector2[]
                {
                    new Vector2(0.08f, 0.15f),
                    new Vector2(0.92f, 0.15f),
                    new Vector2(0.08f, 0.85f),
                    new Vector2(0.92f, 0.85f)
                };

                foreach (var center in donutCenters)
                {
                    float distToDonut = Vector2.Distance(new Vector2(nx, ny), center);
                    if (distToDonut < donutRadius)
                    {
                        // Outer donut ring
                        if (distToDonut > donutRadius * 0.4f)
                        {
                            float ringT = (distToDonut - donutRadius * 0.4f) / (donutRadius * 0.6f);
                            Color donutColor = Color.Lerp(dunkinPink, dunkinOrange, ringT);
                            // Add shine
                            float shine = Mathf.Clamp01(1f - distToDonut / donutRadius);
                            donutColor = Color.Lerp(donutColor, Color.white, shine * 0.3f);
                            pixelColor = donutColor;
                        }
                        else
                        {
                            // Donut hole
                            pixelColor = cream;
                        }
                    }
                }

                // Clamp and set
                pixelColor.r = Mathf.Clamp01(pixelColor.r);
                pixelColor.g = Mathf.Clamp01(pixelColor.g);
                pixelColor.b = Mathf.Clamp01(pixelColor.b);
                pixelColor.a = 1f;

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        return texture;
    }

    /// <summary>
    /// Creates a material for Dunkin building exterior with procedural brick/stucco texture.
    /// </summary>
    public static Material CreateDunkinBuildingMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        Texture2D tex = GenerateDunkinBuildingTexture();

        mat.mainTexture = tex;
        mat.mainTextureScale = new Vector2(3f, 3f); // Tile the texture
        mat.color = Color.white;
        mat.SetFloat("_Glossiness", 0.15f);
        mat.SetFloat("_Metallic", 0f);

        return mat;
    }

    /// <summary>
    /// Creates a material for Dunkin sign with procedural branded texture.
    /// </summary>
    public static Material CreateDunkinSignMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        Texture2D tex = GenerateDunkinSignTexture();

        mat.mainTexture = tex;
        mat.color = Color.white;
        mat.SetFloat("_Glossiness", 0.7f);
        mat.SetFloat("_Metallic", 0.1f);

        // Add emission for sign glow
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.4f) * 0.5f);

        return mat;
    }

    // ==================== PROCEDURAL GRASS TEXTURE ====================

    /// <summary>
    /// Generates a procedural grass texture with natural variation and blade patterns
    /// </summary>
    void GenerateProceduralGrass()
    {
        int size = grassTextureSize;
        ProceduralGrassTexture = new Texture2D(size, size, TextureFormat.RGB24, true);
        ProceduralGrassTexture.wrapMode = TextureWrapMode.Repeat;
        ProceduralGrassTexture.filterMode = FilterMode.Bilinear;

        ProceduralGrassNormal = new Texture2D(size, size, TextureFormat.RGB24, true);
        ProceduralGrassNormal.wrapMode = TextureWrapMode.Repeat;
        ProceduralGrassNormal.filterMode = FilterMode.Bilinear;

        Color[] colorPixels = new Color[size * size];
        Color[] normalPixels = new Color[size * size];

        // Base grass colors
        Color grassDark = new Color(0.15f, 0.32f, 0.12f);
        Color grassMid = new Color(0.22f, 0.42f, 0.18f);
        Color grassLight = new Color(0.35f, 0.55f, 0.25f);
        Color grassYellow = new Color(0.45f, 0.48f, 0.22f); // Dry patches

        Random.InitState(123); // Consistent results

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;

                // Multi-octave noise for base color variation
                float noise1 = Mathf.PerlinNoise(x * 0.02f, y * 0.02f);
                float noise2 = Mathf.PerlinNoise(x * 0.06f + 100, y * 0.06f + 100);
                float noise3 = Mathf.PerlinNoise(x * 0.15f + 200, y * 0.15f + 200);

                // Combine for overall shade
                float shade = noise1 * 0.5f + noise2 * 0.3f + noise3 * 0.2f;

                // Blend between grass colors based on shade
                Color baseColor;
                if (shade < 0.35f)
                    baseColor = Color.Lerp(grassDark, grassMid, shade / 0.35f);
                else if (shade < 0.65f)
                    baseColor = Color.Lerp(grassMid, grassLight, (shade - 0.35f) / 0.3f);
                else
                    baseColor = Color.Lerp(grassLight, grassYellow, (shade - 0.65f) / 0.35f);

                // Add fine grain for grass blade effect
                float fineNoise = (Random.value - 0.5f) * 0.08f;
                baseColor.r += fineNoise;
                baseColor.g += fineNoise * 1.2f; // Greens vary more
                baseColor.b += fineNoise * 0.6f;

                // Grass blade pattern - vertical streaks
                float bladeNoise = Mathf.PerlinNoise(x * 0.5f, y * 0.08f);
                float bladeFactor = Mathf.Pow(Mathf.Abs(bladeNoise - 0.5f) * 2f, 2f);
                baseColor = Color.Lerp(baseColor, grassDark, bladeFactor * 0.15f);

                // Occasional small flowers/clover
                float flowerNoise = Mathf.PerlinNoise(x * 0.3f + 500, y * 0.3f + 500);
                if (flowerNoise > 0.92f)
                {
                    // White/yellow flower spots
                    baseColor = Color.Lerp(baseColor, new Color(0.9f, 0.9f, 0.7f), 0.4f);
                }

                // Clamp colors
                baseColor.r = Mathf.Clamp01(baseColor.r);
                baseColor.g = Mathf.Clamp01(baseColor.g);
                baseColor.b = Mathf.Clamp01(baseColor.b);

                colorPixels[idx] = baseColor;

                // === NORMAL MAP GENERATION ===
                // Create grass blade normal patterns
                float nx = 0.5f; // Neutral X
                float ny = 0.5f; // Neutral Y

                // Grass blades lean in various directions
                float bladeAngle = Mathf.PerlinNoise(x * 0.4f + 300, y * 0.1f + 300) * Mathf.PI * 2f;
                float bladeStrength = Mathf.PerlinNoise(x * 0.3f, y * 0.05f) * 0.3f;

                // Add blade direction to normal
                nx += Mathf.Cos(bladeAngle) * bladeStrength;
                ny += Mathf.Sin(bladeAngle) * bladeStrength;

                // Fine detail bumps
                float bumpNoise = (Mathf.PerlinNoise(x * 0.8f + 400, y * 0.8f + 400) - 0.5f) * 0.1f;
                nx += bumpNoise;
                ny += bumpNoise;

                // Z component (always pointing up, stronger = flatter)
                float nz = 1f;

                // Normalize and encode to color (0-1 range)
                Vector3 normal = new Vector3(nx - 0.5f, ny - 0.5f, nz).normalized;
                normalPixels[idx] = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z * 0.5f + 0.5f
                );
            }
        }

        ProceduralGrassTexture.SetPixels(colorPixels);
        ProceduralGrassTexture.Apply(true);

        ProceduralGrassNormal.SetPixels(normalPixels);
        ProceduralGrassNormal.Apply(true);

        Debug.Log("Generated procedural grass texture with normal map: " + size + "x" + size);
    }

    // ==================== PROCEDURAL ASPHALT NORMAL MAP ====================

    /// <summary>
    /// Generates a normal map for the asphalt texture (bumps, cracks, aggregate)
    /// </summary>
    void GenerateProceduralAsphaltNormal()
    {
        int size = asphaltTextureSize;
        ProceduralAsphaltNormal = new Texture2D(size, size, TextureFormat.RGB24, true);
        ProceduralAsphaltNormal.wrapMode = TextureWrapMode.Repeat;
        ProceduralAsphaltNormal.filterMode = FilterMode.Bilinear;

        Color[] normalPixels = new Color[size * size];

        Random.InitState(42); // Match asphalt texture seed

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;

                float nx = 0.5f;
                float ny = 0.5f;

                // Aggregate bumps - small stones in asphalt
                float bumpScale1 = 0.25f;
                float bumpScale2 = 0.5f;

                float bump1 = Mathf.PerlinNoise(x * bumpScale1, y * bumpScale1);
                float bump2 = Mathf.PerlinNoise(x * bumpScale2 + 100, y * bumpScale2 + 100);

                // Calculate derivatives for normal direction
                float dx1 = Mathf.PerlinNoise((x + 1) * bumpScale1, y * bumpScale1) -
                           Mathf.PerlinNoise((x - 1) * bumpScale1, y * bumpScale1);
                float dy1 = Mathf.PerlinNoise(x * bumpScale1, (y + 1) * bumpScale1) -
                           Mathf.PerlinNoise(x * bumpScale1, (y - 1) * bumpScale1);

                float dx2 = Mathf.PerlinNoise((x + 1) * bumpScale2 + 100, y * bumpScale2 + 100) -
                           Mathf.PerlinNoise((x - 1) * bumpScale2 + 100, y * bumpScale2 + 100);
                float dy2 = Mathf.PerlinNoise(x * bumpScale2 + 100, (y + 1) * bumpScale2 + 100) -
                           Mathf.PerlinNoise(x * bumpScale2 + 100, (y - 1) * bumpScale2 + 100);

                // Combine bump derivatives
                float dx = dx1 * 0.6f + dx2 * 0.4f;
                float dy = dy1 * 0.6f + dy2 * 0.4f;

                // Crack normals (using same crack pattern as diffuse)
                float crackValue = GenerateCrackPattern(x, y, size);
                if (crackValue > 0.1f)
                {
                    // Cracks create depth - add sharp edge to normal
                    float crackDx = GenerateCrackPattern(x + 1, y, size) - GenerateCrackPattern(x - 1, y, size);
                    float crackDy = GenerateCrackPattern(x, y + 1, size) - GenerateCrackPattern(x, y - 1, size);
                    dx += crackDx * 1.5f;
                    dy += crackDy * 1.5f;
                }

                // Fine grain noise
                float grain = (Random.value - 0.5f) * 0.02f;
                dx += grain;
                dy += grain;

                // Apply to normal with controlled strength
                nx += dx * 0.15f;
                ny += dy * 0.15f;

                // Clamp and normalize
                nx = Mathf.Clamp(nx, 0f, 1f);
                ny = Mathf.Clamp(ny, 0f, 1f);
                float nz = 1f;

                Vector3 normal = new Vector3(nx - 0.5f, ny - 0.5f, nz).normalized;
                normalPixels[idx] = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z * 0.5f + 0.5f
                );
            }
        }

        ProceduralAsphaltNormal.SetPixels(normalPixels);
        ProceduralAsphaltNormal.Apply(true);

        Debug.Log("Generated procedural asphalt normal map: " + size + "x" + size);
    }

    // ==================== PROCEDURAL CONCRETE TEXTURE ====================

    /// <summary>
    /// Generates a procedural concrete/barrier texture with surface detail
    /// </summary>
    void GenerateProceduralConcrete()
    {
        int size = concreteTextureSize;
        ProceduralConcreteTexture = new Texture2D(size, size, TextureFormat.RGB24, true);
        ProceduralConcreteTexture.wrapMode = TextureWrapMode.Repeat;
        ProceduralConcreteTexture.filterMode = FilterMode.Bilinear;

        ProceduralConcreteNormal = new Texture2D(size, size, TextureFormat.RGB24, true);
        ProceduralConcreteNormal.wrapMode = TextureWrapMode.Repeat;
        ProceduralConcreteNormal.filterMode = FilterMode.Bilinear;

        Color[] colorPixels = new Color[size * size];
        Color[] normalPixels = new Color[size * size];

        // Concrete base colors (light gray with warm/cool variation)
        float baseGray = 0.78f;

        Random.InitState(789);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = y * size + x;

                // Large scale color variation (formwork marks, patches)
                float largeNoise = Mathf.PerlinNoise(x * 0.015f, y * 0.015f);
                float medNoise = Mathf.PerlinNoise(x * 0.05f + 50, y * 0.05f + 50);
                float fineNoise = Mathf.PerlinNoise(x * 0.15f + 100, y * 0.15f + 100);

                float gray = baseGray;
                gray += (largeNoise - 0.5f) * 0.08f;  // Large patches
                gray += (medNoise - 0.5f) * 0.05f;   // Medium variation
                gray += (fineNoise - 0.5f) * 0.03f;  // Fine grain

                // Random aggregate specks
                if (Random.value > 0.97f)
                {
                    gray += Random.Range(-0.08f, 0.06f); // Darker or lighter spots
                }

                // Subtle pitting/holes
                float pitNoise = Mathf.PerlinNoise(x * 0.2f + 200, y * 0.2f + 200);
                if (pitNoise > 0.85f)
                {
                    gray -= (pitNoise - 0.85f) * 0.3f; // Dark pits
                }

                // Slight color temperature variation (warm vs cool gray)
                float warmth = Mathf.PerlinNoise(x * 0.01f + 300, y * 0.01f + 300);
                float r = gray + (warmth - 0.5f) * 0.03f;
                float g = gray;
                float b = gray - (warmth - 0.5f) * 0.02f;

                colorPixels[idx] = new Color(
                    Mathf.Clamp01(r),
                    Mathf.Clamp01(g),
                    Mathf.Clamp01(b)
                );

                // === NORMAL MAP FOR CONCRETE ===
                float nx = 0.5f;
                float ny = 0.5f;

                // Surface roughness from noise derivatives
                float bumpScale = 0.1f;
                float dx = Mathf.PerlinNoise((x + 1) * bumpScale, y * bumpScale) -
                          Mathf.PerlinNoise((x - 1) * bumpScale, y * bumpScale);
                float dy = Mathf.PerlinNoise(x * bumpScale, (y + 1) * bumpScale) -
                          Mathf.PerlinNoise(x * bumpScale, (y - 1) * bumpScale);

                // Pitting creates depressions
                if (pitNoise > 0.85f)
                {
                    float pitDx = Mathf.PerlinNoise((x + 2) * 0.2f + 200, y * 0.2f + 200) -
                                 Mathf.PerlinNoise((x - 2) * 0.2f + 200, y * 0.2f + 200);
                    float pitDy = Mathf.PerlinNoise(x * 0.2f + 200, (y + 2) * 0.2f + 200) -
                                 Mathf.PerlinNoise(x * 0.2f + 200, (y - 2) * 0.2f + 200);
                    dx += pitDx * 0.5f;
                    dy += pitDy * 0.5f;
                }

                // Trowel marks - subtle horizontal lines
                float trowelAngle = Mathf.PerlinNoise(x * 0.005f, y * 0.08f);
                dy += (trowelAngle - 0.5f) * 0.05f;

                // Fine random grain
                float grain = (Random.value - 0.5f) * 0.015f;
                dx += grain;
                dy += grain;

                nx += dx * 0.12f;
                ny += dy * 0.12f;

                nx = Mathf.Clamp(nx, 0f, 1f);
                ny = Mathf.Clamp(ny, 0f, 1f);

                Vector3 normal = new Vector3(nx - 0.5f, ny - 0.5f, 1f).normalized;
                normalPixels[idx] = new Color(
                    normal.x * 0.5f + 0.5f,
                    normal.y * 0.5f + 0.5f,
                    normal.z * 0.5f + 0.5f
                );
            }
        }

        ProceduralConcreteTexture.SetPixels(colorPixels);
        ProceduralConcreteTexture.Apply(true);

        ProceduralConcreteNormal.SetPixels(normalPixels);
        ProceduralConcreteNormal.Apply(true);

        Debug.Log("Generated procedural concrete texture with normal map: " + size + "x" + size);
    }
}
