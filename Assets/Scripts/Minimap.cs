using UnityEngine;

public class Minimap : MonoBehaviour
{
    [Header("Size")]
    public float mapSize = 140f;
    public float mapScale = 200f; // World units shown on map

    [Header("Colors")]
    private Color bgColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
    private Color trackColor = new Color(0.25f, 0.25f, 0.3f);
    private Color playerColor = new Color(0.2f, 0.8f, 0.3f);
    private Color copColor = new Color(0.95f, 0.3f, 0.2f);
    private Color powerUpColor = new Color(1f, 0.85f, 0.3f);

    // Textures
    private Texture2D bgTex;
    private Texture2D dotTex;
    private Texture2D playerTex;
    private Texture2D trackTex;
    private Texture2D copTex;
    private Texture2D borderTex;
    private Texture2D ammoTex;
    private Texture2D speedTex;
    private Texture2D shieldTex;

    // References
    private Transform player;

    void Start()
    {
        // Find player
        CarController playerCar = FindFirstObjectByType<CarController>();
        if (playerCar != null)
        {
            player = playerCar.transform;
        }

        CreateTextures();
    }

    void CreateTextures()
    {
        // Background
        bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, bgColor);
        bgTex.Apply();

        // Dot for cops/items
        dotTex = new Texture2D(1, 1);
        dotTex.SetPixel(0, 0, Color.white);
        dotTex.Apply();

        // Player arrow (simple triangle texture)
        playerTex = CreateArrowTexture(16, playerColor);

        // Cached color textures
        trackTex = MakeTex(trackColor);
        copTex = MakeTex(copColor);
        borderTex = MakeTex(new Color(0.4f, 0.4f, 0.45f));
        ammoTex = MakeTex(new Color(1f, 0.7f, 0.2f));
        speedTex = MakeTex(new Color(0.2f, 0.8f, 1f));
        shieldTex = MakeTex(new Color(0.5f, 1f, 0.5f));
    }

    Texture2D CreateArrowTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        Color clear = new Color(0, 0, 0, 0);

        // Fill with transparent
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                tex.SetPixel(x, y, clear);
            }
        }

        // Draw arrow pointing up
        int centerX = size / 2;
        for (int y = 0; y < size; y++)
        {
            int width = (int)((size - y) * 0.5f);
            for (int x = centerX - width; x <= centerX + width; x++)
            {
                if (x >= 0 && x < size)
                {
                    tex.SetPixel(x, y, color);
                }
            }
        }

        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (player == null) return;

        DrawMinimap();
    }

    void DrawMinimap()
    {
        float padding = 15f;
        float x = Screen.width - mapSize - padding;
        float y = Screen.height - mapSize - padding;

        // Background circle (using rect for simplicity)
        GUI.DrawTexture(new Rect(x - 5, y - 5, mapSize + 10, mapSize + 10), bgTex);

        // Draw track outline (oval)
        DrawTrackOutline(x, y);

        // Draw power-ups
        DrawPowerUps(x, y);

        // Draw cops
        DrawCops(x, y);

        // Draw player
        DrawPlayer(x, y);

        // Border
        DrawBorder(x, y);
    }

    void DrawTrackOutline(float mapX, float mapY)
    {
        // Draw a simplified oval for the track
        float centerX = mapX + mapSize / 2;
        float centerY = mapY + mapSize / 2;

        // Draw track as dots forming an oval
        int segments = 36;
        float trackRadius = 140f; // World track radius

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * 360f * Mathf.Deg2Rad;
            float worldX = Mathf.Cos(angle) * trackRadius;
            float worldZ = Mathf.Sin(angle) * trackRadius;

            Vector2 mapPos = WorldToMap(new Vector3(worldX, 0, worldZ), mapX, mapY);
            GUI.DrawTexture(new Rect(mapPos.x - 1, mapPos.y - 1, 3, 3), trackTex);
        }
    }

    void DrawPowerUps(float mapX, float mapY)
    {
        PowerUp[] powerUps = FindObjectsByType<PowerUp>(FindObjectsSortMode.None);

        foreach (PowerUp pu in powerUps)
        {
            if (pu == null) continue;

            Vector2 mapPos = WorldToMap(pu.transform.position, mapX, mapY);

            // Check if within map bounds
            if (IsInMapBounds(mapPos, mapX, mapY))
            {
                // Different colors per type
                Texture2D puTex = pu.type switch
                {
                    PowerUpType.Ammo => ammoTex,
                    PowerUpType.SpeedBoost => speedTex,
                    PowerUpType.Shield => shieldTex,
                    _ => ammoTex
                };

                GUI.DrawTexture(new Rect(mapPos.x - 3, mapPos.y - 3, 6, 6), puTex);
            }
        }
    }

    void DrawCops(float mapX, float mapY)
    {
        CopCar[] cops = FindObjectsByType<CopCar>(FindObjectsSortMode.None);

        foreach (CopCar cop in cops)
        {
            if (cop == null) continue;

            Vector2 mapPos = WorldToMap(cop.transform.position, mapX, mapY);

            // Check if within map bounds
            if (IsInMapBounds(mapPos, mapX, mapY))
            {
                // Pulse effect for fleeing cops
                float size = cop.state == CopState.Fleeing ? 6f + Mathf.Sin(Time.time * 8f) * 1f : 5f;
                GUI.DrawTexture(new Rect(mapPos.x - size / 2, mapPos.y - size / 2, size, size), copTex);
            }
        }
    }

    void DrawPlayer(float mapX, float mapY)
    {
        Vector2 mapPos = WorldToMap(player.position, mapX, mapY);

        // Rotate player arrow based on facing direction
        float angle = player.eulerAngles.y;

        // Save current matrix
        Matrix4x4 savedMatrix = GUI.matrix;

        // Rotate around player position
        GUIUtility.RotateAroundPivot(angle, new Vector2(mapPos.x, mapPos.y));

        // Draw player arrow
        float size = 12f;
        GUI.DrawTexture(new Rect(mapPos.x - size / 2, mapPos.y - size / 2, size, size), playerTex);

        // Restore matrix
        GUI.matrix = savedMatrix;
    }

    void DrawBorder(float mapX, float mapY)
    {
        float thickness = 2f;

        // Top
        GUI.DrawTexture(new Rect(mapX - 5, mapY - 5, mapSize + 10, thickness), borderTex);
        // Bottom
        GUI.DrawTexture(new Rect(mapX - 5, mapY + mapSize + 3, mapSize + 10, thickness), borderTex);
        // Left
        GUI.DrawTexture(new Rect(mapX - 5, mapY - 5, thickness, mapSize + 10), borderTex);
        // Right
        GUI.DrawTexture(new Rect(mapX + mapSize + 3, mapY - 5, thickness, mapSize + 10), borderTex);
    }

    Vector2 WorldToMap(Vector3 worldPos, float mapX, float mapY)
    {
        // Convert world position to map position
        float mapCenterX = mapX + mapSize / 2;
        float mapCenterY = mapY + mapSize / 2;

        float normalizedX = worldPos.x / mapScale;
        float normalizedZ = worldPos.z / mapScale;

        return new Vector2(
            mapCenterX + normalizedX * (mapSize / 2),
            mapCenterY - normalizedZ * (mapSize / 2) // Flip Z for map
        );
    }

    bool IsInMapBounds(Vector2 mapPos, float mapX, float mapY)
    {
        return mapPos.x >= mapX && mapPos.x <= mapX + mapSize &&
               mapPos.y >= mapY && mapPos.y <= mapY + mapSize;
    }

    Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }
}
