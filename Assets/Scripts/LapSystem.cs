using UnityEngine;
using System.Collections.Generic;

public class LapSystem : MonoBehaviour
{
    public static LapSystem Instance { get; private set; }

    [Header("Settings")]
    public int totalLaps = 3;
    public int totalCheckpoints = 4;

    // State
    private int currentLap = 0;
    private int checkpointsHit = 0;
    private HashSet<int> hitCheckpoints = new HashSet<int>();
    private float lapStartTime;
    private float currentLapTime;
    private float bestLapTime = float.MaxValue;
    private float totalRaceTime;
    private bool raceStarted = false;
    private bool raceFinished = false;
    private List<float> lapTimes = new List<float>();

    // Public getters
    public int CurrentLap => currentLap;
    public int TotalLaps => totalLaps;
    public float CurrentLapTime => currentLapTime;
    public float BestLapTime => bestLapTime < float.MaxValue ? bestLapTime : 0f;
    public float TotalRaceTime => totalRaceTime;
    public bool RaceFinished => raceFinished;
    public int CheckpointsHit => checkpointsHit;
    public int TotalCheckpoints => totalCheckpoints;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (raceStarted && !raceFinished)
        {
            currentLapTime = Time.time - lapStartTime;
            totalRaceTime += Time.deltaTime;
        }
    }

    public void OnCheckpointHit(int checkpointIndex, bool isFinishLine)
    {
        if (raceFinished) return;

        if (isFinishLine)
        {
            OnFinishLineHit();
        }
        else
        {
            if (!hitCheckpoints.Contains(checkpointIndex))
            {
                hitCheckpoints.Add(checkpointIndex);
                checkpointsHit++;
            }
        }
    }

    void OnFinishLineHit()
    {
        // First time crossing - start the race
        if (!raceStarted)
        {
            raceStarted = true;
            currentLap = 1;
            lapStartTime = Time.time;
            hitCheckpoints.Clear();
            checkpointsHit = 0;
            PlayStartSound();
            return;
        }

        // Check if all checkpoints were hit
        if (checkpointsHit < totalCheckpoints - 1) // -1 because finish line is a checkpoint
        {
            // Missed checkpoints, don't count the lap
            return;
        }

        // Complete the lap
        float lapTime = currentLapTime;
        lapTimes.Add(lapTime);

        if (lapTime < bestLapTime)
        {
            bestLapTime = lapTime;
        }

        // Play lap complete sound
        PlayLapSound();

        // Check if race is finished
        if (currentLap >= totalLaps)
        {
            raceFinished = true;
            PlayFinishSound();
            return;
        }

        // Start next lap
        currentLap++;
        lapStartTime = Time.time;
        hitCheckpoints.Clear();
        checkpointsHit = 0;
    }

    void PlayStartSound()
    {
        var car = FindFirstObjectByType<CarController>();
        if (car != null)
        {
            var audio = car.GetComponent<CarAudio>();
            audio?.PlayLapComplete();
        }
    }

    void PlayLapSound()
    {
        var car = FindFirstObjectByType<CarController>();
        if (car != null)
        {
            var audio = car.GetComponent<CarAudio>();
            audio?.PlayLapComplete();
        }
    }

    void PlayFinishSound()
    {
        var car = FindFirstObjectByType<CarController>();
        if (car != null)
        {
            var audio = car.GetComponent<CarAudio>();
            // Play multiple times for finish
            audio?.PlayLapComplete();
        }
    }

    public void RestartRace()
    {
        currentLap = 0;
        checkpointsHit = 0;
        hitCheckpoints.Clear();
        lapStartTime = 0;
        currentLapTime = 0;
        totalRaceTime = 0;
        raceStarted = false;
        raceFinished = false;
        lapTimes.Clear();
        // Don't reset best lap time to keep track across restarts
    }

    public string FormatTime(float time)
    {
        if (time <= 0 || time == float.MaxValue) return "--:--.---";

        int minutes = (int)(time / 60);
        float seconds = time % 60;
        return string.Format("{0}:{1:00.000}", minutes, seconds);
    }

    public float GetLapTime(int lapIndex)
    {
        if (lapIndex >= 0 && lapIndex < lapTimes.Count)
            return lapTimes[lapIndex];
        return 0f;
    }
}
