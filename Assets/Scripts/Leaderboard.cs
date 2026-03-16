using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Leaderboard : MonoBehaviour
{
    public static Leaderboard Instance { get; private set; }

    private const string LEADERBOARD_KEY = "CopChallengeTimes";
    private const int MAX_ENTRIES = 10;

    private List<float> bestTimes = new List<float>();

    // UI State
    private bool showLeaderboard = false;
    private float showTimer = 0f;

    // Colors
    private Color bgColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);
    private Color goldColor = new Color(1f, 0.85f, 0.3f);
    private Color silverColor = new Color(0.75f, 0.75f, 0.8f);
    private Color bronzeColor = new Color(0.8f, 0.5f, 0.2f);
    private Color textColor = new Color(0.9f, 0.9f, 0.9f);

    // Cached textures
    private Texture2D bgTex;
    private Texture2D highlightTex;

    void Awake()
    {
        Instance = this;
        LoadTimes();
        CreateTextures();
    }

    void CreateTextures()
    {
        bgTex = MakeTex(bgColor);
        highlightTex = MakeTex(new Color(goldColor.r, goldColor.g, goldColor.b, 0.2f));
    }

    void Update()
    {
        // Show leaderboard on Tab or Select button
        var gp = Gamepad.current;
        if (Input.GetKeyDown(KeyCode.Tab) || (gp != null && gp.selectButton.wasPressedThisFrame))
        {
            showLeaderboard = !showLeaderboard;
        }

        // Auto-show when challenge completes
        if (CopChallenge.ChallengeComplete)
        {
            showTimer += Time.unscaledDeltaTime;
            if (showTimer > 1.5f)
            {
                showLeaderboard = true;
            }
        }
        else
        {
            showTimer = 0f;
            // Only auto-hide, let manual toggle work
        }
    }

    public void AddTime(float time)
    {
        bestTimes.Add(time);
        bestTimes.Sort();

        // Keep only top entries
        while (bestTimes.Count > MAX_ENTRIES)
        {
            bestTimes.RemoveAt(bestTimes.Count - 1);
        }

        SaveTimes();
    }

    public int GetRank(float time)
    {
        for (int i = 0; i < bestTimes.Count; i++)
        {
            if (Mathf.Approximately(time, bestTimes[i]))
            {
                return i + 1;
            }
        }
        return -1;
    }

    public bool IsNewRecord(float time)
    {
        if (bestTimes.Count == 0) return true;
        return time < bestTimes[0];
    }

    public void Hide()
    {
        showLeaderboard = false;
        showTimer = 0f;
    }

    void SaveTimes()
    {
        string data = string.Join(",", bestTimes);
        PlayerPrefs.SetString(LEADERBOARD_KEY, data);
        PlayerPrefs.Save();
    }

    void LoadTimes()
    {
        bestTimes.Clear();
        string data = PlayerPrefs.GetString(LEADERBOARD_KEY, "");

        if (!string.IsNullOrEmpty(data))
        {
            string[] parts = data.Split(',');
            foreach (string part in parts)
            {
                if (float.TryParse(part, out float time))
                {
                    bestTimes.Add(time);
                }
            }
        }
    }

    void OnGUI()
    {
        // Hint to show leaderboard
        if (!showLeaderboard && !CopChallenge.ChallengeComplete)
        {
            GUIStyle hintStyle = new GUIStyle
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight
            };
            hintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.65f);
            GUI.Label(new Rect(Screen.width - 160, 10, 150, 20), "TAB: Leaderboard", hintStyle);
        }

        if (!showLeaderboard) return;

        DrawLeaderboard();
    }

    void DrawLeaderboard()
    {
        float panelWidth = 240;
        float panelHeight = 30 + (Mathf.Min(bestTimes.Count, 10) + 1) * 24 + 20;
        float x = Screen.width / 2 - panelWidth / 2;
        float y = Screen.height / 2 - panelHeight / 2;

        // Background
        GUI.DrawTexture(new Rect(x, y, panelWidth, panelHeight), bgTex);

        // Title
        GUIStyle titleStyle = new GUIStyle
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = goldColor;
        GUI.Label(new Rect(x, y + 8, panelWidth, 24), "BEST TIMES", titleStyle);

        // Entries
        float entryY = y + 38;

        if (bestTimes.Count == 0)
        {
            GUIStyle emptyStyle = new GUIStyle
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            emptyStyle.normal.textColor = new Color(0.5f, 0.5f, 0.55f);
            GUI.Label(new Rect(x, entryY, panelWidth, 24), "No times yet!", emptyStyle);
        }
        else
        {
            for (int i = 0; i < Mathf.Min(bestTimes.Count, 10); i++)
            {
                DrawEntry(x + 15, entryY + i * 24, panelWidth - 30, i + 1, bestTimes[i]);
            }
        }

        // Close hint
        GUIStyle closeStyle = new GUIStyle
        {
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };
        closeStyle.normal.textColor = new Color(0.5f, 0.5f, 0.55f);
        GUI.Label(new Rect(x, y + panelHeight - 18, panelWidth, 16), "Press TAB to close", closeStyle);
    }

    void DrawEntry(float x, float y, float width, int rank, float time)
    {
        // Rank
        GUIStyle rankStyle = new GUIStyle
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };

        Color rankColor = rank switch
        {
            1 => goldColor,
            2 => silverColor,
            3 => bronzeColor,
            _ => textColor
        };
        rankStyle.normal.textColor = rankColor;

        string medal = rank switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => $"{rank}th"
        };

        GUI.Label(new Rect(x, y, 40, 22), medal, rankStyle);

        // Time
        GUIStyle timeStyle = new GUIStyle
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleRight
        };
        timeStyle.normal.textColor = textColor;

        GUI.Label(new Rect(x + 40, y, width - 40, 22), FormatTime(time), timeStyle);

        // Highlight current time
        if (CopChallenge.ChallengeComplete)
        {
            float currentTime = CopChallenge.CompletionTime;
            if (Mathf.Abs(time - currentTime) < 0.01f)
            {
                // Draw highlight bar
                GUI.DrawTexture(new Rect(x - 5, y + 2, width + 10, 18), highlightTex);
            }
        }
    }

    string FormatTime(float time)
    {
        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return string.Format("{0}:{1:00.00}", minutes, seconds);
    }

    Texture2D MakeTex(Color col)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }
}
