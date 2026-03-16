using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static int Score { get; set; } = 0;
    public static int DestroyedCount { get; set; } = 0;

    private CarController car;
    private CarTurret turret;

    // Textures
    private Texture2D panelBg;
    private Texture2D barBg;
    private Texture2D barFill;
    private Texture2D boostFill;
    private Texture2D accentColor;
    private Texture2D whiteTex;
    private Texture2D accentBlueTex;
    private Texture2D overlayBgTex;
    private Texture2D panelOverlayTex;
    private Texture2D dividerTex;

    // Colors - modern aesthetic
    private Color bgColor = new Color(0.08f, 0.08f, 0.1f, 0.75f);
    private Color textColor = new Color(0.95f, 0.95f, 0.95f);
    private Color dimTextColor = new Color(0.7f, 0.7f, 0.75f);
    private Color accentOrange = new Color(0.95f, 0.5f, 0.2f);
    private Color accentBlue = new Color(0.4f, 0.7f, 1f);

    // Cached styles
    private GUIStyle speedNumberStyle;
    private GUIStyle speedUnitStyle;
    private GUIStyle labelStyle;
    private GUIStyle lapStyle;
    private GUIStyle timeStyle;
    private GUIStyle controlStyle;
    private GUIStyle driftStyle;

    private float driftPulse;

    // Controls overlay
    private float controlsOverlayAlpha = 1f;
    private bool controlsDismissed = false;

    void Start()
    {
        car = FindFirstObjectByType<CarController>();
        turret = FindFirstObjectByType<CarTurret>();
        CreateTextures();
        CreateStyles();
    }

    void Update()
    {
        // Handle controls overlay - dismiss on any key press
        if (!controlsDismissed && Input.anyKeyDown)
        {
            controlsDismissed = true;
        }

        // Fade out when dismissed
        if (controlsDismissed && controlsOverlayAlpha > 0)
        {
            controlsOverlayAlpha = Mathf.Max(0f, controlsOverlayAlpha - Time.deltaTime * 4f);
        }
    }

    void CreateTextures()
    {
        panelBg = MakeRoundedTex(bgColor, 8);
        barBg = MakeTex(new Color(0.15f, 0.15f, 0.18f, 0.9f));
        barFill = MakeTex(new Color(0.3f, 0.85f, 0.4f, 1f));
        boostFill = MakeTex(accentOrange);
        accentColor = MakeTex(accentOrange);
        whiteTex = MakeTex(Color.white);
        accentBlueTex = MakeTex(accentBlue);
        overlayBgTex = MakeTex(new Color(0f, 0f, 0f, 0.85f));
        panelOverlayTex = MakeTex(new Color(0.1f, 0.1f, 0.12f, 0.95f));
        dividerTex = MakeTex(new Color(0.3f, 0.3f, 0.35f, 1f));
    }

    Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    Texture2D MakeRoundedTex(Color col, int size)
    {
        Texture2D tex = new Texture2D(size, size);
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        return tex;
    }

    void CreateStyles()
    {
        speedNumberStyle = new GUIStyle
        {
            fontSize = 52,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        speedNumberStyle.normal.textColor = textColor;

        speedUnitStyle = new GUIStyle
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleRight
        };
        speedUnitStyle.normal.textColor = dimTextColor;

        labelStyle = new GUIStyle
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle.normal.textColor = dimTextColor;

        lapStyle = new GUIStyle
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        lapStyle.normal.textColor = textColor;

        timeStyle = new GUIStyle
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        timeStyle.normal.textColor = dimTextColor;

        controlStyle = new GUIStyle
        {
            fontSize = 13,
            alignment = TextAnchor.UpperLeft
        };
        controlStyle.normal.textColor = new Color(1f, 1f, 1f, 0.5f);

        driftStyle = new GUIStyle
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    void OnGUI()
    {
        if (car == null) return;

        DrawSpeedPanel();
        DrawBoostBar();
        DrawAmmoPanel();
        DrawLapPanel();
        DrawControls();
        DrawPrompts();

        // Draw controls overlay on top
        if (controlsOverlayAlpha > 0.01f)
        {
            DrawControlsOverlay();
        }
    }

    void DrawScore()
    {
        float panelWidth = 140;
        float panelHeight = 60;
        float x = Screen.width / 2 - panelWidth / 2;
        float y = 25;

        // Background
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelBg);

        // Score label
        GUIStyle scoreLabel = new GUIStyle(labelStyle);
        scoreLabel.alignment = TextAnchor.MiddleCenter;
        scoreLabel.normal.textColor = accentOrange;
        GUI.Label(new Rect(x, y + 8, panelWidth, 20), "SCORE", scoreLabel);

        // Score value
        GUIStyle scoreValue = new GUIStyle(speedNumberStyle);
        scoreValue.fontSize = 28;
        scoreValue.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(x, y + 25, panelWidth, 35), Score.ToString(), scoreValue);
    }

    void DrawSpeedPanel()
    {
        float panelWidth = 160;
        float panelHeight = 100;
        float x = Screen.width - panelWidth - 25;
        float y = Screen.height - panelHeight - 25;

        // Background panel
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelBg);

        // Speed number
        float speed = Mathf.Abs(car.CurrentSpeed);
        int displaySpeed = Mathf.RoundToInt(speed * 5f);

        GUI.Label(new Rect(x, y + 10, panelWidth - 15, 55), displaySpeed.ToString(), speedNumberStyle);

        // km/h label
        GUI.Label(new Rect(x, y + 60, panelWidth - 15, 20), "KM/H", speedUnitStyle);

        // Speed bar background
        float barX = x + 12;
        float barY = y + panelHeight - 18;
        float barWidth = panelWidth - 24;
        float barHeight = 6;

        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), barBg);

        // Speed bar fill
        float fillWidth = barWidth * car.SpeedPercent;
        Texture2D fillTex = car.IsBoosting ? boostFill : barFill;
        GUI.DrawTexture(new Rect(barX, barY, fillWidth, barHeight), fillTex);
    }

    void DrawBoostBar()
    {
        float panelWidth = 180;
        float panelHeight = 50;
        float x = 25;
        float y = Screen.height - panelHeight - 25;

        // Background
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelBg);

        // Label
        string boostText = "BOOST";
        if (car.BoostCooldownPercent >= 1f) boostText = "READY";
        if (car.IsBoosting) boostText = "ACTIVE";

        labelStyle.normal.textColor = car.IsBoosting ? accentOrange :
            (car.BoostCooldownPercent >= 1f ? accentBlue : dimTextColor);
        GUI.Label(new Rect(x + 12, y + 8, 80, 20), boostText, labelStyle);

        // Bar background
        float barX = x + 12;
        float barY = y + 30;
        float barWidth = panelWidth - 24;
        float barHeight = 8;

        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), barBg);

        // Bar fill
        float fillWidth = barWidth * car.BoostCooldownPercent;
        Color savedColor = GUI.color;
        if (car.IsBoosting)
        {
            // Pulsing effect when active
            float pulse = 0.7f + Mathf.Sin(Time.time * 15f) * 0.3f;
            GUI.color = accentOrange * pulse + Color.white * (1f - pulse) * 0.3f;
            fillWidth = barWidth;
        }
        else if (car.BoostCooldownPercent >= 1f)
        {
            GUI.color = accentBlue;
        }
        else
        {
            GUI.color = accentOrange;
        }
        GUI.DrawTexture(new Rect(barX, barY, fillWidth, barHeight), whiteTex);
        GUI.color = savedColor;
    }

    void DrawLapPanel()
    {
        if (LapSystem.Instance == null) return;

        var lap = LapSystem.Instance;

        float panelWidth = 160;
        float panelHeight = 80;
        float x = Screen.width - panelWidth - 25;
        float y = 25;

        // Background
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelBg);

        // Lap info
        if (lap.RaceFinished)
        {
            lapStyle.normal.textColor = new Color(0.4f, 1f, 0.5f);
            GUI.Label(new Rect(x, y + 10, panelWidth, 30), "FINISHED", lapStyle);
        }
        else if (lap.CurrentLap > 0)
        {
            lapStyle.normal.textColor = textColor;
            GUI.Label(new Rect(x, y + 10, panelWidth, 30), $"LAP {lap.CurrentLap}/{lap.TotalLaps}", lapStyle);
        }
        else
        {
            lapStyle.normal.textColor = accentOrange;
            GUI.Label(new Rect(x, y + 10, panelWidth, 30), "START", lapStyle);
        }

        // Current time
        timeStyle.normal.textColor = dimTextColor;
        GUI.Label(new Rect(x, y + 38, panelWidth, 20), lap.FormatTime(lap.CurrentLapTime), timeStyle);

        // Best time
        if (lap.BestLapTime > 0)
        {
            timeStyle.normal.textColor = new Color(0.5f, 0.9f, 0.5f);
            GUI.Label(new Rect(x, y + 56, panelWidth, 20), $"BEST {lap.FormatTime(lap.BestLapTime)}", timeStyle);
        }
    }

    void DrawDriftIndicator()
    {
        if (!car.IsDrifting)
        {
            driftPulse = 0;
            return;
        }

        driftPulse += Time.deltaTime * 8f;

        float alpha = 0.7f + Mathf.Sin(driftPulse) * 0.3f;
        Color driftColor = Color.Lerp(accentOrange, new Color(1f, 0.85f, 0.4f), Mathf.Sin(driftPulse * 0.5f) * 0.5f + 0.5f);
        driftColor.a = alpha;

        driftStyle.normal.textColor = driftColor;

        // Centered at top
        float x = Screen.width / 2 - 75;
        float y = 80;

        // Shadow
        GUIStyle shadowStyle = new GUIStyle(driftStyle);
        shadowStyle.normal.textColor = new Color(0, 0, 0, alpha * 0.4f);
        GUI.Label(new Rect(x + 2, y + 2, 150, 50), "DRIFT", shadowStyle);

        // Main text
        GUI.Label(new Rect(x, y, 150, 50), "DRIFT", driftStyle);
    }

    void DrawAmmoPanel()
    {
        if (turret == null) return;

        float panelWidth = 140;
        float panelHeight = 50;
        float x = 25;
        float y = Screen.height - panelHeight - 90; // Above boost bar

        // Background
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelBg);

        // Label
        string ammoText = turret.CurrentAmmo > 0 ? "AMMO" : "EMPTY";
        Color ammoColor = turret.AmmoPercent > 0.3f ? new Color(0.9f, 0.7f, 0.2f) :
            (turret.CurrentAmmo > 0 ? accentOrange : new Color(0.9f, 0.2f, 0.2f));

        labelStyle.normal.textColor = ammoColor;
        GUI.Label(new Rect(x + 12, y + 8, 60, 20), ammoText, labelStyle);

        // Ammo count
        GUIStyle ammoCountStyle = new GUIStyle(labelStyle);
        ammoCountStyle.alignment = TextAnchor.MiddleRight;
        ammoCountStyle.normal.textColor = textColor;
        GUI.Label(new Rect(x + 60, y + 8, panelWidth - 72, 20),
            $"{turret.CurrentAmmo}/{turret.MaxAmmo}", ammoCountStyle);

        // Bar background
        float barX = x + 12;
        float barY = y + 30;
        float barWidth = panelWidth - 24;
        float barHeight = 8;

        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), barBg);

        // Bar fill
        float fillWidth = barWidth * turret.AmmoPercent;
        Color savedColor = GUI.color;
        GUI.color = ammoColor;
        GUI.DrawTexture(new Rect(barX, barY, fillWidth, barHeight), whiteTex);
        GUI.color = savedColor;
    }

    void DrawControls()
    {
        // Only show after overlay is dismissed
        if (!controlsDismissed) return;

        // Hide control hints after 2 cops have been killed
        if (CopChallenge.CopsDestroyed >= 2) return;

        float panelX = 12;
        float panelY = Screen.height / 2 - 170;
        float panelWidth = 210;
        float panelHeight = 340;

        // Dark background panel
        Color savedColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.75f);
        GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), whiteTex);
        GUI.color = savedColor;

        // Title style
        GUIStyle titleStyle = new GUIStyle
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        titleStyle.normal.textColor = accentOrange;

        // Control style
        GUIStyle ctrlStyle = new GUIStyle
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };
        ctrlStyle.normal.textColor = new Color(0.95f, 0.95f, 0.95f);

        float x = panelX + 10;
        float y = panelY + 8;

        // Keyboard section
        GUI.Label(new Rect(x, y, 180, 18), "KEYBOARD", titleStyle);
        y += 20;

        string[] kbControls = {
            "WASD / Arrows - Drive",
            "SHIFT - Drift/Brake",
            "SPACE - Jump",
            "B - Boost",
            "Q/E or J/L - Turret",
            "F or K - Fire",
            "G or H - Reload",
            "R - Restart | TAB - Scores"
        };

        foreach (string line in kbControls)
        {
            GUI.Label(new Rect(x, y, 180, 16), line, ctrlStyle);
            y += 15;
        }

        y += 8;

        // Controller section
        titleStyle.normal.textColor = accentBlue;
        GUI.Label(new Rect(x, y, 180, 18), "CONTROLLER", titleStyle);
        y += 20;

        string[] gpControls = {
            "L Stick - Steer",
            "RT/LT - Gas/Brake",
            "X/□ - Drift",
            "A/✕ - Jump | B/○ - Boost",
            "R Stick - Aim Turret",
            "L1/R1 - Fire",
            "Y/△ - Reload",
            "START - Restart",
            "SELECT - Scores"
        };

        foreach (string line in gpControls)
        {
            GUI.Label(new Rect(x, y, 180, 16), line, ctrlStyle);
            y += 15;
        }
    }

    void DrawPrompts()
    {
        // Show contextual prompts
        GUIStyle promptStyle = new GUIStyle
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        float y = Screen.height / 2 + 80;

        // Show reload prompt when ammo is zero
        if (turret != null && turret.CurrentAmmo <= 0)
        {
            float pulse = Mathf.Sin(Time.time * 4f) * 0.3f + 0.7f;
            promptStyle.normal.textColor = new Color(1f, 0.7f, 0.2f, pulse);
            GUI.Label(new Rect(0, y, Screen.width, 30), "Press G / Y to Reload", promptStyle);
            y += 35;
        }

        // Show restart prompt after challenge complete
        if (CopChallenge.ChallengeComplete)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.3f + 0.7f;
            promptStyle.normal.textColor = new Color(0.4f, 1f, 0.5f, pulse);
            GUI.Label(new Rect(0, y, Screen.width, 30), "Press R / START to Restart", promptStyle);
        }
    }

    void DrawControlsOverlay()
    {
        // Semi-transparent dark background
        Color savedColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, controlsOverlayAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayBgTex);

        // Center panel - wider for two columns
        float panelWidth = 580;
        float panelHeight = 380;
        float x = (Screen.width - panelWidth) / 2;
        float y = (Screen.height - panelHeight) / 2;

        // Panel background
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), panelOverlayTex);
        GUI.color = savedColor;

        // Title
        GUIStyle titleStyle = new GUIStyle
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(accentOrange.r, accentOrange.g, accentOrange.b, controlsOverlayAlpha);
        GUI.Label(new Rect(x, y + 15, panelWidth, 40), "CONTROLS", titleStyle);

        // Column headers
        GUIStyle headerStyle = new GUIStyle
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = new Color(accentBlue.r, accentBlue.g, accentBlue.b, controlsOverlayAlpha);

        float col1X = x + 30;
        float col2X = x + panelWidth / 2 + 20;
        float colWidth = panelWidth / 2 - 50;

        GUI.Label(new Rect(col1X, y + 60, colWidth, 25), "KEYBOARD", headerStyle);
        GUI.Label(new Rect(col2X, y + 60, colWidth, 25), "CONTROLLER", headerStyle);

        // Divider line
        Color divColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, controlsOverlayAlpha);
        GUI.DrawTexture(new Rect(x + panelWidth / 2 - 1, y + 90, 2, 230), dividerTex);
        GUI.color = divColor;

        // Control styles
        GUIStyle keyStyle = new GUIStyle
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleLeft
        };
        keyStyle.normal.textColor = new Color(1f, 1f, 1f, controlsOverlayAlpha);

        GUIStyle labelStyle2 = new GUIStyle
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft
        };
        labelStyle2.normal.textColor = new Color(0.7f, 0.7f, 0.7f, controlsOverlayAlpha);

        // Keyboard controls (left column)
        string[,] keyboardControls = {
            {"WASD", "Drive"},
            {"SHIFT", "Drift"},
            {"SPACE", "Jump"},
            {"N", "Boost"},
            {"J / L", "Turret"},
            {"K", "Fire"},
            {"H", "Reload"},
            {"R", "Restart"}
        };

        float lineY = y + 95;
        for (int i = 0; i < keyboardControls.GetLength(0); i++)
        {
            GUI.Label(new Rect(col1X, lineY, 70, 24), keyboardControls[i, 0], keyStyle);
            GUI.Label(new Rect(col1X + 75, lineY, 100, 24), keyboardControls[i, 1], labelStyle2);
            lineY += 26;
        }

        // Controller controls (right column)
        string[,] controllerControls = {
            {"Left Stick", "Drive"},
            {"LT", "Drift"},
            {"A", "Jump"},
            {"B", "Boost"},
            {"LB / RB", "Turret"},
            {"X", "Fire"},
            {"Y", "Reload"},
            {"Start", "Restart"}
        };

        lineY = y + 95;
        for (int i = 0; i < controllerControls.GetLength(0); i++)
        {
            GUI.Label(new Rect(col2X, lineY, 80, 24), controllerControls[i, 0], keyStyle);
            GUI.Label(new Rect(col2X + 85, lineY, 100, 24), controllerControls[i, 1], labelStyle2);
            lineY += 26;
        }

        // Hint at bottom
        GUIStyle hintStyle = new GUIStyle
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        float pulse = Mathf.Sin(Time.time * 3f) * 0.3f + 0.7f;
        hintStyle.normal.textColor = new Color(1f, 1f, 1f, controlsOverlayAlpha * pulse);
        GUI.Label(new Rect(x, y + panelHeight - 40, panelWidth, 30), "Press any key to start", hintStyle);
    }
}
