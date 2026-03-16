using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DunkinStore : MonoBehaviour
{
    public static List<DunkinStore> AllStores = new List<DunkinStore>();

    [Header("Parking")]
    public int maxParkingSpots = 3;
    public List<Transform> parkingSpots = new List<Transform>();
    public List<CopCar> parkedCops = new List<CopCar>();

    [Header("Visuals")]
    public Color buildingColor = new Color(1f, 0.4f, 0.2f); // Dunkin orange
    public Color accentColor = new Color(0.85f, 0.2f, 0.5f); // Dunkin pink/magenta

    private GameObject building;
    private GameObject sign;
    private Light signLight;

    // Textures (loaded from Resources or generated procedurally)
    private static Texture2D buildingTexture;
    private static Texture2D signTexture;
    private static Material proceduralBuildingMaterial;
    private static Material proceduralSignMaterial;
    private static bool texturesLoaded = false;

    void Awake()
    {
        AllStores.Add(this);
        LoadTextures();
    }

    void OnDestroy()
    {
        AllStores.Remove(this);
    }

    // Process texture to make white/near-white pixels transparent
    static Texture2D ProcessTextureForTransparency(Texture2D source)
    {
        // Create a readable copy
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        // Process pixels - make white/near-white transparent
        Color[] pixels = readable.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = pixels[i];
            // If pixel is very bright (close to white), make it transparent
            float brightness = (c.r + c.g + c.b) / 3f;
            if (brightness > 0.95f)
            {
                pixels[i] = new Color(c.r, c.g, c.b, 0f); // Fully transparent
            }
            else if (brightness > 0.9f)
            {
                // Fade out near-white pixels
                float alpha = 1f - ((brightness - 0.9f) / 0.05f);
                pixels[i] = new Color(c.r, c.g, c.b, alpha);
            }
        }

        readable.SetPixels(pixels);
        readable.Apply();

        Debug.Log($"Processed Dunkin texture: made white areas transparent");
        return readable;
    }

    static void LoadTextures()
    {
        if (texturesLoaded) return;
        texturesLoaded = true;

        // Try to load custom textures from Resources
        buildingTexture = Resources.Load<Texture2D>("Textures/dunkin_building");
        signTexture = Resources.Load<Texture2D>("Textures/dunkin_sign");

        if (buildingTexture != null)
        {
            Debug.Log("Loaded Dunkin building texture from Resources");
        }
        else
        {
            // Generate procedural building texture
            buildingTexture = TextureManager.GenerateDunkinBuildingTexture();
            proceduralBuildingMaterial = TextureManager.CreateDunkinBuildingMaterial();
            Debug.Log("Generated procedural Dunkin building texture (brick/stucco with brand colors)");
        }

        if (signTexture != null)
        {
            Debug.Log("Loaded Dunkin sign texture from Resources");
        }
        else
        {
            // Generate procedural sign texture
            signTexture = TextureManager.GenerateDunkinSignTexture();
            proceduralSignMaterial = TextureManager.CreateDunkinSignMaterial();
            Debug.Log("Generated procedural Dunkin sign texture (branded gradient with donuts)");
        }
    }

    public void Build()
    {
        CreateBuilding();
        CreateSign();
        CreateParkingLot();
        CreateDriveThru();
        CreateDetails();
    }

    void CreateFacade()
    {
        // Get actual building world position and place facade relative to it
        Vector3 buildingWorldPos = building.transform.position;
        Vector3 buildingScale = building.transform.lossyScale;

        GameObject facade = GameObject.CreatePrimitive(PrimitiveType.Quad);
        facade.name = "DunkinFacade";

        // Place in WORLD space, in front of building (+Z in building's forward direction)
        // Building front face is at buildingWorldPos + forward * (depth/2)
        Vector3 buildingForward = building.transform.forward;
        Vector3 frontFaceCenter = buildingWorldPos + buildingForward * (buildingScale.z / 2f + 0.05f);

        facade.transform.position = frontFaceCenter;
        // Rotate 180 so quad faces OUTWARD (away from building center)
        facade.transform.rotation = building.transform.rotation * Quaternion.Euler(0, 180, 0);
        facade.transform.localScale = new Vector3(buildingScale.x, buildingScale.y, 1f);

        Destroy(facade.GetComponent<Collider>());

        // Calculate texture aspect ratio to properly fill the facade
        float textureAspect = (float)buildingTexture.width / buildingTexture.height;
        float facadeAspect = buildingScale.x / buildingScale.y; // 28/14 = 2.0

        // Use standard shader with the texture stretched to fill the entire facade
        // No transparency processing - we want the full storefront image to cover the wall
        Material facadeMat = new Material(Shader.Find("Standard"));
        facadeMat.mainTexture = buildingTexture;
        facadeMat.SetFloat("_Glossiness", 0.3f);
        facadeMat.SetFloat("_Metallic", 0f);

        // Adjust UV tiling to fill the quad while maintaining aspect ratio (cover mode)
        // If texture is wider than facade, scale height up (clip sides)
        // If texture is taller than facade, scale width up (clip top/bottom)
        Vector2 tiling = Vector2.one;
        Vector2 offset = Vector2.zero;

        if (textureAspect > facadeAspect)
        {
            // Texture is wider - scale to fill height, center horizontally
            float scale = facadeAspect / textureAspect;
            tiling = new Vector2(scale, 1f);
            offset = new Vector2((1f - scale) / 2f, 0f);
        }
        else
        {
            // Texture is taller - scale to fill width, center vertically
            float scale = textureAspect / facadeAspect;
            tiling = new Vector2(1f, scale);
            offset = new Vector2(0f, (1f - scale) / 2f);
        }

        facadeMat.mainTextureScale = tiling;
        facadeMat.mainTextureOffset = offset;

        facade.GetComponent<Renderer>().material = facadeMat;

        Debug.Log($"Dunkin facade: texture {buildingTexture.width}x{buildingTexture.height} (aspect {textureAspect:F2}), facade {buildingScale.x}x{buildingScale.y} (aspect {facadeAspect:F2}), tiling={tiling}, offset={offset}");
    }

    void CreateBuilding()
    {
        // Main building - MUCH BIGGER
        building = GameObject.CreatePrimitive(PrimitiveType.Cube);
        building.name = "DunkinBuilding";
        building.transform.SetParent(transform);
        building.transform.localPosition = new Vector3(0, 7f, 0);
        building.transform.localScale = new Vector3(28f, 14f, 20f);
        // Note: Not marking as static so collision works properly with dynamic objects
        // The BoxCollider from CreatePrimitive is kept for collision

        // Building body uses simple off-white material (sides and back)
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = new Color(0.95f, 0.92f, 0.88f); // Off-white
        bodyMat.SetFloat("_Glossiness", 0.1f);
        building.GetComponent<Renderer>().material = bodyMat;

        // Front facade with the Dunkin storefront image
        if (buildingTexture != null)
        {
            CreateFacade();
        }

        // Create shared materials for branding
        Material orangeMat = new Material(Shader.Find("Standard"));
        orangeMat.color = buildingColor;
        orangeMat.SetColor("_EmissionColor", buildingColor * 0.3f);
        orangeMat.EnableKeyword("_EMISSION");
        orangeMat.SetFloat("_Glossiness", 0.7f);

        Material pinkMat = new Material(Shader.Find("Standard"));
        pinkMat.color = accentColor;
        pinkMat.SetColor("_EmissionColor", accentColor * 0.2f);
        pinkMat.EnableKeyword("_EMISSION");
        pinkMat.SetFloat("_Glossiness", 0.7f);

        // === BRANDING STRIPS ON ALL SIDES (offset further to prevent z-fighting) ===
        float offset = 0.505f;  // Slightly outside surface to prevent flickering

        // FRONT SIDE (Z+) - skip if we have facade texture
        if (buildingTexture == null)
        {
            CreateBrandingStrip(new Vector3(0, 0.4f, offset), new Vector3(1.01f, 0.08f, 0.01f), orangeMat);
            CreateBrandingStrip(new Vector3(0, 0.35f, offset), new Vector3(1.01f, 0.04f, 0.01f), pinkMat);
            CreateBrandingStrip(new Vector3(0, -0.35f, offset), new Vector3(1.01f, 0.06f, 0.01f), orangeMat);
            CreateBrandingStrip(new Vector3(0, -0.4f, offset), new Vector3(1.01f, 0.03f, 0.01f), pinkMat);
        }

        // BACK SIDE (Z-)
        CreateBrandingStrip(new Vector3(0, 0.4f, -offset), new Vector3(1.01f, 0.08f, 0.01f), orangeMat);
        CreateBrandingStrip(new Vector3(0, 0.35f, -offset), new Vector3(1.01f, 0.04f, 0.01f), pinkMat);
        CreateBrandingStrip(new Vector3(0, -0.35f, -offset), new Vector3(1.01f, 0.06f, 0.01f), orangeMat);
        CreateBrandingStrip(new Vector3(0, -0.4f, -offset), new Vector3(1.01f, 0.03f, 0.01f), pinkMat);

        // LEFT SIDE (X-)
        CreateBrandingStrip(new Vector3(-offset, 0.4f, 0), new Vector3(0.01f, 0.08f, 1.01f), orangeMat);
        CreateBrandingStrip(new Vector3(-offset, 0.35f, 0), new Vector3(0.01f, 0.04f, 1.01f), pinkMat);
        CreateBrandingStrip(new Vector3(-offset, -0.35f, 0), new Vector3(0.01f, 0.06f, 1.01f), orangeMat);
        CreateBrandingStrip(new Vector3(-offset, -0.4f, 0), new Vector3(0.01f, 0.03f, 1.01f), pinkMat);

        // RIGHT SIDE (X+)
        CreateBrandingStrip(new Vector3(offset, 0.4f, 0), new Vector3(0.01f, 0.08f, 1.01f), orangeMat);
        CreateBrandingStrip(new Vector3(offset, 0.35f, 0), new Vector3(0.01f, 0.04f, 1.01f), pinkMat);
        CreateBrandingStrip(new Vector3(offset, -0.35f, 0), new Vector3(0.01f, 0.06f, 1.01f), orangeMat);
        CreateBrandingStrip(new Vector3(offset, -0.4f, 0), new Vector3(0.01f, 0.03f, 1.01f), pinkMat);

        // VERTICAL CORNER STRIPES (orange pillars at corners - offset outward)
        // Skip front corners if we have facade texture
        if (buildingTexture == null)
        {
            CreateBrandingStrip(new Vector3(-0.49f, 0, 0.49f), new Vector3(0.03f, 1.01f, 0.03f), orangeMat);
            CreateBrandingStrip(new Vector3(0.49f, 0, 0.49f), new Vector3(0.03f, 1.01f, 0.03f), orangeMat);
        }
        CreateBrandingStrip(new Vector3(-0.49f, 0, -0.49f), new Vector3(0.03f, 1.01f, 0.03f), orangeMat);
        CreateBrandingStrip(new Vector3(0.49f, 0, -0.49f), new Vector3(0.03f, 1.01f, 0.03f), orangeMat);

        // Windows and door on front - skip if we have facade texture (image already shows them)
        if (buildingTexture == null)
        {
            CreateWindow(new Vector3(-0.25f, 0.1f, 0.505f), new Vector3(0.18f, 0.35f, 0.01f));
            CreateWindow(new Vector3(0f, 0.1f, 0.505f), new Vector3(0.18f, 0.35f, 0.01f));
            CreateWindow(new Vector3(0.25f, 0.1f, 0.505f), new Vector3(0.18f, 0.35f, 0.01f));

            // Door
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "Door";
            door.transform.SetParent(building.transform);
            door.transform.localPosition = new Vector3(-0.12f, -0.3f, 0.51f);
            door.transform.localScale = new Vector3(0.1f, 0.38f, 0.02f);
            Destroy(door.GetComponent<Collider>());

            Material doorMat = new Material(Shader.Find("Standard"));
            doorMat.color = new Color(0.2f, 0.2f, 0.25f);
            doorMat.SetFloat("_Glossiness", 0.8f);
            door.GetComponent<Renderer>().material = doorMat;
        }

        // Windows on sides (offset to prevent z-fighting)
        CreateWindow(new Vector3(-0.505f, 0.1f, 0.2f), new Vector3(0.01f, 0.3f, 0.15f));
        CreateWindow(new Vector3(-0.505f, 0.1f, -0.2f), new Vector3(0.01f, 0.3f, 0.15f));
        CreateWindow(new Vector3(0.505f, 0.1f, 0.2f), new Vector3(0.01f, 0.3f, 0.15f));
        CreateWindow(new Vector3(0.505f, 0.1f, -0.2f), new Vector3(0.01f, 0.3f, 0.15f));

        // Roof - matches 28x14x20 building (top at y=14)
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.name = "Roof";
        roof.transform.SetParent(transform);
        roof.transform.localPosition = new Vector3(0, 14.3f, 0);
        roof.transform.localScale = new Vector3(30f, 0.6f, 22f);
        roof.isStatic = true;
        Destroy(roof.GetComponent<Collider>());

        Material roofMat = new Material(Shader.Find("Standard"));
        roofMat.color = new Color(0.3f, 0.25f, 0.2f);
        roofMat.SetFloat("_Glossiness", 0.2f);
        roof.GetComponent<Renderer>().material = roofMat;

        // Roof edge trim (orange)
        CreateRoofTrim(orangeMat);
    }

    void CreateBrandingStrip(Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = "BrandingStrip";
        strip.transform.SetParent(building.transform);
        strip.transform.localPosition = localPos;
        strip.transform.localScale = localScale;
        Destroy(strip.GetComponent<Collider>());
        strip.GetComponent<Renderer>().material = mat;
    }

    void CreateRoofTrim(Material mat)
    {
        // Orange trim around roof edge - matches 28x14x20 building
        float y = 14.5f;  // Just above roof at 14.3
        float h = 0.5f;

        // Front trim (building front is at z=+10)
        GameObject front = GameObject.CreatePrimitive(PrimitiveType.Cube);
        front.transform.SetParent(transform);
        front.transform.localPosition = new Vector3(0, y, 11f);
        front.transform.localScale = new Vector3(30f, h, 0.4f);
        Destroy(front.GetComponent<Collider>());
        front.GetComponent<Renderer>().material = mat;

        // Back trim (building back is at z=-10)
        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.transform.SetParent(transform);
        back.transform.localPosition = new Vector3(0, y, -11f);
        back.transform.localScale = new Vector3(30f, h, 0.4f);
        Destroy(back.GetComponent<Collider>());
        back.GetComponent<Renderer>().material = mat;

        // Left trim (building left is at x=-14)
        GameObject left = GameObject.CreatePrimitive(PrimitiveType.Cube);
        left.transform.SetParent(transform);
        left.transform.localPosition = new Vector3(-15f, y, 0);
        left.transform.localScale = new Vector3(0.4f, h, 22f);
        Destroy(left.GetComponent<Collider>());
        left.GetComponent<Renderer>().material = mat;

        // Right trim (building right is at x=+14)
        GameObject right = GameObject.CreatePrimitive(PrimitiveType.Cube);
        right.transform.SetParent(transform);
        right.transform.localPosition = new Vector3(15f, y, 0);
        right.transform.localScale = new Vector3(0.4f, h, 22f);
        Destroy(right.GetComponent<Collider>());
        right.GetComponent<Renderer>().material = mat;
    }

    void CreateWindow(Vector3 localPos, Vector3 localScale)
    {
        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = "Window";
        window.transform.SetParent(building.transform);
        window.transform.localPosition = localPos;
        window.transform.localScale = localScale;
        Destroy(window.GetComponent<Collider>());

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.6f, 0.85f, 0.95f, 0.8f);
        mat.SetFloat("_Glossiness", 0.95f);
        mat.SetFloat("_Metallic", 0.1f);
        // Slight interior glow
        mat.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.8f) * 0.3f);
        mat.EnableKeyword("_EMISSION");
        window.GetComponent<Renderer>().material = mat;
    }

    void CreateSign()
    {
        // Sign post - TALL (next to building, building right at x=+14)
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "SignPost";
        post.transform.SetParent(transform);
        post.transform.localPosition = new Vector3(18f, 8f, 18f);
        post.transform.localScale = new Vector3(0.6f, 8f, 0.6f);
        // Note: Not marking as static so collision works properly with dynamic objects

        Material postMat = new Material(Shader.Find("Standard"));
        postMat.color = new Color(0.4f, 0.4f, 0.4f);
        postMat.SetFloat("_Metallic", 0.8f);
        post.GetComponent<Renderer>().material = postMat;

        // Sign board - BIGGER
        sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.name = "DunkinSign";
        sign.transform.SetParent(transform);
        sign.transform.localPosition = new Vector3(18f, 18f, 18f);
        sign.transform.localScale = new Vector3(10f, 6f, 0.6f);
        Destroy(sign.GetComponent<Collider>());

        // Apply sign material - use procedural if available
        Material signMat;
        if (proceduralSignMaterial != null)
        {
            // Use pre-created procedural material with branded design
            signMat = new Material(proceduralSignMaterial);
        }
        else if (signTexture != null)
        {
            signMat = new Material(Shader.Find("Standard"));
            signMat.mainTexture = signTexture;
            signMat.color = Color.white;
            signMat.SetColor("_EmissionColor", Color.white * 0.5f);
            signMat.EnableKeyword("_EMISSION");
            signMat.SetFloat("_Glossiness", 0.7f);
        }
        else
        {
            signMat = new Material(Shader.Find("Standard"));
            signMat.color = buildingColor;
            signMat.SetColor("_EmissionColor", buildingColor * 0.8f);
            signMat.EnableKeyword("_EMISSION");
            signMat.SetFloat("_Glossiness", 0.7f);
        }
        sign.GetComponent<Renderer>().material = signMat;

        // Sign light - brighter
        GameObject lightObj = new GameObject("SignLight");
        lightObj.transform.SetParent(sign.transform);
        lightObj.transform.localPosition = new Vector3(0, -0.6f, 1.2f);
        signLight = lightObj.AddComponent<Light>();
        signLight.type = LightType.Point;
        signLight.color = buildingColor;
        signLight.intensity = 3f;
        signLight.range = 20f;

        // "DUNKIN" text sign on building - skip if we have facade texture (it already shows the logo)
        if (buildingTexture == null)
        {
            GameObject buildingSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buildingSign.name = "BuildingSign";
            buildingSign.transform.SetParent(transform);
            buildingSign.transform.localPosition = new Vector3(0, 11f, 10.2f);
            buildingSign.transform.localScale = new Vector3(16f, 3f, 0.4f);
            Destroy(buildingSign.GetComponent<Collider>());

            // Apply building-mounted sign material - use procedural if available
            Material bsignMat;
            if (proceduralSignMaterial != null)
            {
                bsignMat = new Material(proceduralSignMaterial);
                bsignMat.SetColor("_EmissionColor", new Color(1f, 0.6f, 0.4f) * 0.4f);
            }
            else
            {
                bsignMat = new Material(Shader.Find("Standard"));
                bsignMat.color = buildingColor;
                bsignMat.SetColor("_EmissionColor", buildingColor * 0.6f);
                bsignMat.EnableKeyword("_EMISSION");
            }
            buildingSign.GetComponent<Renderer>().material = bsignMat;
        }
    }

    void CreateParkingLot()
    {
        // Parking surface - Y=0.08 to prevent z-fighting with ground (Y=0)
        // Needs at least 0.03 separation from ground plane
        GameObject lot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lot.name = "ParkingLot";
        lot.transform.SetParent(transform);
        lot.transform.localPosition = new Vector3(0, 0.08f, 22f); // Front of building + parking area
        lot.transform.localScale = new Vector3(32f, 0.04f, 20f);   // Very thin, wider for bigger building
        lot.isStatic = true;
        Destroy(lot.GetComponent<Collider>()); // No collision - just visual

        Material lotMat = new Material(Shader.Find("Standard"));
        lotMat.color = new Color(0.15f, 0.15f, 0.17f);
        lotMat.SetFloat("_Glossiness", 0.2f);
        lot.GetComponent<Renderer>().material = lotMat;

        // Parking lines and spots (more spread out for bigger lot)
        for (int i = 0; i < maxParkingSpots; i++)
        {
            float x = -10f + i * 10f;

            // Parking line - Y=0.12 above parking lot (Y=0.08) to prevent z-fighting
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "ParkingLine";
            line.transform.SetParent(transform);
            line.transform.localPosition = new Vector3(x - 4f, 0.12f, 22f);
            line.transform.localScale = new Vector3(0.15f, 0.01f, 12f);
            line.isStatic = true;
            Destroy(line.GetComponent<Collider>());

            Material lineMat = new Material(Shader.Find("Standard"));
            lineMat.color = new Color(0.9f, 0.9f, 0.5f);
            line.GetComponent<Renderer>().material = lineMat;

            // Create parking spot transform
            GameObject spot = new GameObject($"ParkingSpot_{i}");
            spot.transform.SetParent(transform);
            spot.transform.localPosition = new Vector3(x, 0.5f, 22f);
            spot.transform.localRotation = Quaternion.Euler(0, 180, 0); // Face the building
            parkingSpots.Add(spot.transform);
        }
    }

    void CreateDriveThru()
    {
        // Drive-thru lane - Y=0.08 to prevent z-fighting with ground (Y=0)
        GameObject lane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lane.name = "DriveThruLane";
        lane.transform.SetParent(transform);
        lane.transform.localPosition = new Vector3(-18f, 0.08f, 0);
        lane.transform.localScale = new Vector3(8f, 0.04f, 28f);
        lane.isStatic = true;
        Destroy(lane.GetComponent<Collider>()); // No collision - just visual

        Material laneMat = new Material(Shader.Find("Standard"));
        laneMat.color = new Color(0.12f, 0.12f, 0.14f);
        laneMat.SetFloat("_Glossiness", 0.15f);
        lane.GetComponent<Renderer>().material = laneMat;

        // Drive-thru window (building left face at x=-14)
        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.name = "DriveThruWindow";
        window.transform.SetParent(transform);
        window.transform.localPosition = new Vector3(-14.2f, 4f, -3f);
        window.transform.localScale = new Vector3(0.3f, 3f, 3f);
        Destroy(window.GetComponent<Collider>());

        Material winMat = new Material(Shader.Find("Standard"));
        winMat.color = new Color(0.5f, 0.8f, 0.9f);
        winMat.SetFloat("_Glossiness", 0.9f);
        winMat.SetColor("_EmissionColor", new Color(1f, 0.9f, 0.7f) * 0.4f);
        winMat.EnableKeyword("_EMISSION");
        window.GetComponent<Renderer>().material = winMat;

        // Menu board (bigger, for larger building - at drive-thru entrance)
        GameObject menu = GameObject.CreatePrimitive(PrimitiveType.Cube);
        menu.name = "MenuBoard";
        menu.transform.SetParent(transform);
        menu.transform.localPosition = new Vector3(-18f, 4f, 6f);
        menu.transform.localScale = new Vector3(0.5f, 6f, 5f);
        menu.transform.localRotation = Quaternion.Euler(0, 15, 0);
        Destroy(menu.GetComponent<Collider>());

        Material menuMat = new Material(Shader.Find("Standard"));
        menuMat.color = new Color(0.1f, 0.1f, 0.1f);
        menuMat.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.9f) * 0.3f);
        menuMat.EnableKeyword("_EMISSION");
        menu.GetComponent<Renderer>().material = menuMat;
    }

    void CreateDetails()
    {
        // Trash can - no collision so car can drive through (front of building at z=+10)
        GameObject trash = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trash.name = "TrashCan";
        trash.transform.SetParent(transform);
        trash.transform.localPosition = new Vector3(12f, 0.6f, 12f);
        trash.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
        Destroy(trash.GetComponent<Collider>()); // No collision

        Material trashMat = new Material(Shader.Find("Standard"));
        trashMat.color = new Color(0.3f, 0.3f, 0.35f);
        trash.GetComponent<Renderer>().material = trashMat;

        // Bench - no collision so car can drive through
        GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bench.name = "Bench";
        bench.transform.SetParent(transform);
        bench.transform.localPosition = new Vector3(10f, 0.5f, 11.5f);
        bench.transform.localScale = new Vector3(3f, 0.6f, 0.8f);
        Destroy(bench.GetComponent<Collider>()); // No collision

        Material benchMat = new Material(Shader.Find("Standard"));
        benchMat.color = new Color(0.5f, 0.35f, 0.2f);
        bench.GetComponent<Renderer>().material = benchMat;

        // Coffee cups on ground (Easter egg - in parking lot area)
        CreateCoffeeCup(new Vector3(3f, 0.15f, 18f));
        CreateCoffeeCup(new Vector3(-2f, 0.15f, 24f));
        CreateCoffeeCup(new Vector3(7f, 0.15f, 26f));

        // Light poles in parking lot - no collision (further out)
        CreateLightPole(new Vector3(-12f, 0, 24f));
        CreateLightPole(new Vector3(12f, 0, 24f));
    }

    void CreateCoffeeCup(Vector3 pos)
    {
        GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cup.name = "CoffeeCup";
        cup.transform.SetParent(transform);
        cup.transform.localPosition = pos;
        cup.transform.localScale = new Vector3(0.25f, 0.18f, 0.25f);
        cup.transform.localRotation = Quaternion.Euler(Random.Range(80, 100), Random.Range(0, 360), 0);
        Destroy(cup.GetComponent<Collider>());

        Material cupMat = new Material(Shader.Find("Standard"));
        cupMat.color = Color.white;
        cup.GetComponent<Renderer>().material = cupMat;
    }

    void CreateLightPole(Vector3 pos)
    {
        // Pole - taller for bigger building
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "LightPole";
        pole.transform.SetParent(transform);
        pole.transform.localPosition = pos + new Vector3(0, 6f, 0);
        pole.transform.localScale = new Vector3(0.4f, 6f, 0.4f);
        Destroy(pole.GetComponent<Collider>()); // No collision

        Material poleMat = new Material(Shader.Find("Standard"));
        poleMat.color = new Color(0.3f, 0.3f, 0.35f);
        poleMat.SetFloat("_Metallic", 0.7f);
        pole.GetComponent<Renderer>().material = poleMat;

        // Light fixture
        GameObject light = GameObject.CreatePrimitive(PrimitiveType.Cube);
        light.transform.SetParent(pole.transform);
        light.transform.localPosition = new Vector3(0, 1.1f, 0);
        light.transform.localScale = new Vector3(2f, 0.12f, 0.8f);
        Destroy(light.GetComponent<Collider>());

        Material lightMat = new Material(Shader.Find("Standard"));
        lightMat.color = new Color(0.9f, 0.85f, 0.7f);
        lightMat.SetColor("_EmissionColor", new Color(1f, 0.95f, 0.8f) * 0.5f);
        lightMat.EnableKeyword("_EMISSION");
        light.GetComponent<Renderer>().material = lightMat;

        // Actual light - softer to avoid blowout
        Light spotLight = light.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = new Color(1f, 0.95f, 0.8f);
        spotLight.intensity = 1.2f;
        spotLight.range = 18f;
        spotLight.spotAngle = 90f;
        spotLight.transform.localRotation = Quaternion.Euler(90, 0, 0);
    }

    // === Parking Management ===

    public bool HasOpenSpot()
    {
        return parkedCops.Count < parkingSpots.Count;
    }

    public Transform GetOpenSpot()
    {
        if (!HasOpenSpot()) return null;
        return parkingSpots[parkedCops.Count];
    }

    public void ParkCop(CopCar cop)
    {
        if (!parkedCops.Contains(cop))
        {
            parkedCops.Add(cop);
        }
    }

    public void UnparkCop(CopCar cop)
    {
        parkedCops.Remove(cop);
    }

    public static DunkinStore GetNearestStore(Vector3 position)
    {
        DunkinStore nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var store in AllStores)
        {
            float dist = Vector3.Distance(position, store.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = store;
            }
        }

        return nearest;
    }

    public static DunkinStore GetNearestStoreWithOpenSpot(Vector3 position)
    {
        DunkinStore nearest = null;
        float nearestDist = float.MaxValue;

        foreach (var store in AllStores)
        {
            if (!store.HasOpenSpot()) continue;

            float dist = Vector3.Distance(position, store.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = store;
            }
        }

        return nearest;
    }

    // === Collision Events ===

    void OnCollisionEnter(Collision col)
    {
        // Player rammed the Dunkin!
        CarController player = col.gameObject.GetComponent<CarController>();
        if (player != null && col.relativeVelocity.magnitude > 10f)
        {
            // All cops get angry!
            Debug.Log("DUNKIN RAMMED! All cops aggro!");

            // Could trigger wanted level increase here
            // For now, make all parked cops immediately chase
            foreach (var cop in parkedCops.ToArray())
            {
                cop.EndDonutBreak();
            }

            // Screen shake
            var cam = Camera.main?.GetComponent<CameraFollow>();
            cam?.Shake(0.3f);
        }
    }

    // === Static Factory ===

    public static DunkinStore Create(Vector3 position, float rotation = 0f)
    {
        GameObject obj = new GameObject("DunkinStore");
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(0, rotation, 0);

        DunkinStore store = obj.AddComponent<DunkinStore>();
        store.Build();

        return store;
    }
}
