using UnityEngine;

public class CopChallenge : MonoBehaviour
{
    public static CopChallenge Instance { get; private set; }

    [Header("Challenge Settings")]
    public int targetKills = 10;
    public int totalCopsSpawned = 15;

    [Header("Combo Settings")]
    public float comboWindow = 4f; // Seconds to chain kills
    public float comboTimeBonus = 1.5f; // Seconds removed per combo kill

    [Header("Wanted Level")]
    public int[] wantedThresholds = { 0, 3, 6, 9 }; // Kills to reach each star
    public float[] copSpeedMultipliers = { 1f, 1.15f, 1.3f, 1.5f }; // Speed per wanted level

    // State
    private static int copsDestroyed = 0;
    private static float challengeStartTime = 0;
    private static float challengeEndTime = 0;
    private static bool challengeStarted = false;
    private static bool challengeComplete = false;
    private static float bestTime = float.MaxValue;

    // Combo state
    private static int currentCombo = 0;
    private static float lastKillTime = 0;
    private static float totalTimeBonus = 0;

    // Wanted level
    private static int wantedLevel = 0;

    // Slow-mo
    private static float slowMoTimer = 0f;
    private static float slowMoDuration = 1.5f;

    // UI
    private Texture2D panelBg;
    private Texture2D progressBg;
    private Texture2D progressFill;
    private Texture2D whiteTex;

    // Colors
    private Color bgColor = new Color(0.08f, 0.08f, 0.1f, 0.85f);
    private Color accentRed = new Color(0.95f, 0.3f, 0.2f);
    private Color accentGold = new Color(1f, 0.85f, 0.3f);
    private Color accentBlue = new Color(0.3f, 0.7f, 1f);
    private Color textColor = new Color(0.95f, 0.95f, 0.95f);

    // Animation
    private float completionFlash = 0f;
    private float killFlash = 0f;
    private float comboFlash = 0f;

    // Public getters
    public static int WantedLevel => wantedLevel;
    public static int CurrentCombo => currentCombo;
    public static float TotalTimeBonus => totalTimeBonus;

    public static int CopsDestroyed => copsDestroyed;
    public static bool ChallengeComplete => challengeComplete;
    public static float CurrentTime => challengeStarted && !challengeComplete ? Time.time - challengeStartTime : 0;
    public static float CompletionTime => challengeEndTime - challengeStartTime;
    public static float BestTime => bestTime < float.MaxValue ? bestTime : 0;

    void Awake()
    {
        Instance = this;
        CreateTextures();
    }

    void CreateTextures()
    {
        panelBg = MakeTex(bgColor);
        progressBg = MakeTex(new Color(0.2f, 0.2f, 0.22f, 0.9f));
        progressFill = MakeTex(accentRed);
        whiteTex = MakeTex(Color.white);
    }

    Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    void Update()
    {
        // Decay flash effects
        if (killFlash > 0) killFlash -= Time.unscaledDeltaTime * 4f;
        if (completionFlash > 0) completionFlash -= Time.unscaledDeltaTime * 2f;
        if (comboFlash > 0) comboFlash -= Time.unscaledDeltaTime * 3f;

        // Handle slow-mo
        if (slowMoTimer > 0)
        {
            slowMoTimer -= Time.unscaledDeltaTime;
            Time.timeScale = Mathf.Lerp(0.2f, 1f, 1f - (slowMoTimer / slowMoDuration));
            if (slowMoTimer <= 0)
            {
                Time.timeScale = 1f;
            }
        }

        // Reset combo if too much time passed
        if (currentCombo > 0 && Time.time - lastKillTime > Instance.comboWindow)
        {
            currentCombo = 0;
        }

        // Update wanted level based on kills
        UpdateWantedLevel();
    }

    void UpdateWantedLevel()
    {
        int newLevel = 0;
        for (int i = wantedThresholds.Length - 1; i >= 0; i--)
        {
            if (copsDestroyed >= wantedThresholds[i])
            {
                newLevel = i;
                break;
            }
        }

        if (newLevel != wantedLevel)
        {
            wantedLevel = newLevel;
            // Update all cop speeds
            UpdateCopSpeeds();
        }
    }

    void UpdateCopSpeeds()
    {
        float multiplier = copSpeedMultipliers[Mathf.Min(wantedLevel, copSpeedMultipliers.Length - 1)];
        foreach (CopCar cop in FindObjectsByType<CopCar>(FindObjectsSortMode.None))
        {
            cop.speed = 18f * multiplier;
            cop.fleeSpeed = 28f * multiplier;
        }
    }

    public static void OnCopDestroyed()
    {
        if (challengeComplete) return;

        // Start challenge on first kill
        if (!challengeStarted)
        {
            challengeStarted = true;
            challengeStartTime = Time.time;
        }

        copsDestroyed++;

        // Combo system
        if (Time.time - lastKillTime < Instance.comboWindow)
        {
            currentCombo++;
            totalTimeBonus += Instance.comboTimeBonus;
            if (Instance != null) Instance.comboFlash = 1f;
            Instance.PlayComboSound(currentCombo);
        }
        else
        {
            currentCombo = 1;
        }
        lastKillTime = Time.time;

        // Flash effect
        if (Instance != null) Instance.killFlash = 1f;

        // Screen shake
        var cam = Camera.main?.GetComponent<CameraFollow>();
        cam?.Shake(0.3f);

        // Check for completion
        if (Instance != null && copsDestroyed >= Instance.targetKills)
        {
            challengeComplete = true;
            challengeEndTime = Time.time - totalTimeBonus; // Apply time bonus
            Instance.completionFlash = 1f;

            // Slow-mo for final kill!
            slowMoTimer = slowMoDuration;
            Time.timeScale = 0.2f;

            // Check for new best time
            float completionTime = challengeEndTime - challengeStartTime;
            if (completionTime < bestTime)
            {
                bestTime = completionTime;
            }

            // Save to leaderboard
            if (Leaderboard.Instance != null)
            {
                Leaderboard.Instance.AddTime(completionTime);
            }

            // Play victory sound
            Instance.PlayVictorySound();
        }
    }

    void PlayComboSound(int combo)
    {
        GameObject audioObj = new GameObject("ComboSound");
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        int sampleRate = 44100;
        float duration = 0.15f;
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Combo", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];
        // Higher pitch for higher combos
        float freq = 400f + (combo * 100f);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float envelope = Mathf.Exp(-t * 15f);
            samples[i] = Mathf.Sin(t * freq * Mathf.PI * 2f) * envelope * 0.4f;
        }

        clip.SetData(samples, 0);
        audio.clip = clip;
        audio.volume = 0.5f;
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }

    public static void Reset()
    {
        copsDestroyed = 0;
        challengeStartTime = 0;
        challengeEndTime = 0;
        challengeStarted = false;
        challengeComplete = false;
        currentCombo = 0;
        lastKillTime = 0;
        totalTimeBonus = 0;
        wantedLevel = 0;
        slowMoTimer = 0;
        Time.timeScale = 1f;
        // Keep best time across resets
    }

    void PlayVictorySound()
    {
        GameObject audioObj = new GameObject("VictorySound");
        AudioSource audio = audioObj.AddComponent<AudioSource>();

        // Generate victory fanfare
        int sampleRate = 44100;
        float duration = 1.2f;
        int sampleCount = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Victory", sampleCount, 1, sampleRate, false);

        float[] samples = new float[sampleCount];

        // Three-note victory jingle
        float[] notes = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
        float[] noteTimes = { 0f, 0.15f, 0.3f };
        float[] noteDurations = { 0.2f, 0.2f, 0.6f };

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float sample = 0;

            for (int n = 0; n < notes.Length; n++)
            {
                if (t >= noteTimes[n] && t < noteTimes[n] + noteDurations[n])
                {
                    float noteT = t - noteTimes[n];
                    float envelope = Mathf.Exp(-noteT * 3f);
                    sample += Mathf.Sin(noteT * notes[n] * Mathf.PI * 2f) * envelope * 0.3f;
                    // Add harmonic
                    sample += Mathf.Sin(noteT * notes[n] * 2f * Mathf.PI * 2f) * envelope * 0.1f;
                }
            }

            samples[i] = sample;
        }

        clip.SetData(samples, 0);
        audio.clip = clip;
        audio.volume = 0.6f;
        audio.Play();

        Destroy(audioObj, duration + 0.1f);
    }

    void OnGUI()
    {
        DrawChallengePanel();
    }

    void DrawChallengePanel()
    {
        float panelWidth = 220;
        float panelHeight = challengeComplete ? 140 : 110;
        float x = Screen.width / 2 - panelWidth / 2;
        float y = 15;

        // Flash effect on kill/combo
        Color flashColor = Color.Lerp(bgColor, accentRed, killFlash * 0.3f);
        flashColor = Color.Lerp(flashColor, accentBlue, comboFlash * 0.4f);
        if (challengeComplete)
        {
            flashColor = Color.Lerp(flashColor, accentGold, completionFlash * 0.4f);
        }
        Color savedColor = GUI.color;
        GUI.color = flashColor;
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), whiteTex);
        GUI.color = savedColor;

        // Wanted stars at top
        DrawWantedStars(x + 10, y + 5, panelWidth - 20);

        // Title
        GUIStyle titleStyle = new GUIStyle
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = challengeComplete ? accentGold : accentRed;

        string title = challengeComplete ? "CHALLENGE COMPLETE!" : "COP TAKEDOWN";
        GUI.Label(new Rect(x, y + 22, panelWidth, 20), title, titleStyle);

        // Progress text
        GUIStyle progressStyle = new GUIStyle
        {
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        progressStyle.normal.textColor = textColor;

        string progressText = $"{copsDestroyed}/{targetKills}";
        GUI.Label(new Rect(x, y + 38, panelWidth, 32), progressText, progressStyle);

        // Combo display
        if (currentCombo > 1 && !challengeComplete)
        {
            GUIStyle comboStyle = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            float comboPulse = 0.7f + comboFlash * 0.3f;
            comboStyle.normal.textColor = new Color(accentBlue.r, accentBlue.g, accentBlue.b, comboPulse);
            GUI.Label(new Rect(x, y + 68, panelWidth, 20), $"COMBO x{currentCombo}  -{totalTimeBonus:F1}s", comboStyle);
        }

        // Progress bar
        float barX = x + 20;
        float barY = y + (currentCombo > 1 ? 88 : 72);
        float barWidth = panelWidth - 40;
        float barHeight = 6;

        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), progressBg);

        float progress = Mathf.Clamp01((float)copsDestroyed / targetKills);
        Color fillColor = challengeComplete ? accentGold : accentRed;
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(barX, barY, barWidth * progress, barHeight), whiteTex);
        GUI.color = savedColor;

        // Timer
        GUIStyle timerStyle = new GUIStyle
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter
        };

        float timerY = barY + 12;

        if (challengeComplete)
        {
            // Show completion time
            timerStyle.normal.textColor = accentGold;
            float time = challengeEndTime - challengeStartTime;
            string bonusText = totalTimeBonus > 0 ? $" (Bonus: -{totalTimeBonus:F1}s)" : "";
            GUI.Label(new Rect(x, timerY, panelWidth, 18), $"TIME: {FormatTime(time)}{bonusText}", timerStyle);

            // Best time
            if (bestTime < float.MaxValue)
            {
                timerStyle.normal.textColor = new Color(0.7f, 0.9f, 0.7f);
                GUI.Label(new Rect(x, timerY + 18, panelWidth, 18), $"BEST: {FormatTime(bestTime)}", timerStyle);
            }
        }
        else if (challengeStarted)
        {
            // Show running timer
            timerStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            float time = Time.time - challengeStartTime;
            GUI.Label(new Rect(x, timerY, panelWidth, 18), FormatTime(time), timerStyle);
        }
        else
        {
            // Hint
            timerStyle.normal.textColor = new Color(0.6f, 0.6f, 0.65f);
            GUI.Label(new Rect(x, timerY, panelWidth, 18), "Destroy a cop to start!", timerStyle);
        }
    }

    void DrawWantedStars(float x, float y, float width)
    {
        GUIStyle starStyle = new GUIStyle
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };

        string stars = "";
        for (int i = 0; i < 4; i++)
        {
            if (i < wantedLevel)
                stars += "★ ";
            else
                stars += "☆ ";
        }

        Color starColor = wantedLevel switch
        {
            0 => new Color(0.5f, 0.5f, 0.5f),
            1 => new Color(1f, 0.9f, 0.3f),
            2 => new Color(1f, 0.6f, 0.2f),
            3 => new Color(1f, 0.3f, 0.2f),
            _ => accentRed
        };

        starStyle.normal.textColor = starColor;
        GUI.Label(new Rect(x, y, width, 18), stars, starStyle);
    }

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return string.Format("{0}:{1:00.00}", minutes, seconds);
    }
}
