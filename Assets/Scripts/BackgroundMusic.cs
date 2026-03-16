using UnityEngine;
using System.Collections;
using System;

public class BackgroundMusic : MonoBehaviour
{
    // Victory event - fired when player reaches 10 cop kills
    public static event Action OnVictory;
    private AudioSource musicSource;
    private AudioSource startSource;
    private AudioSource pauseFillSource; // For alternative bass during pauses
    private AudioSource percussionSource; // For kick/hi-hat percussion layer

    [Header("Music")]
    public float musicVolume = 0.25f; // Audible but not overwhelming
    public float bpm = 95f;

    [Header("Pauses")]
    public float minTimeBetweenPauses = 8f;   // Pauses happen frequently (every 8-20 seconds)
    public float maxTimeBetweenPauses = 20f;  // Maximum time between pauses
    public float minPauseDuration = 2f;       // Short pauses for tension
    public float maxPauseDuration = 4f;       // Maximum pause length
    [Range(0f, 1f)]
    public float silenceChance = 0.75f;       // 75% chance of complete silence during pause (mostly silence)

    [Header("Cop Kill Speed Boost")]
    public int maxCopKillsForSpeedup = 9;     // Reach double speed at 9 kills
    public int victoryKillCount = 10;         // Victory at 10 kills
    public float maxSpeedMultiplier = 2.0f;   // Double speed at max

    [Header("Dynamic Music")]
    public float keyChangeMinTime = 30f;      // Minimum seconds before key change
    public float keyChangeMaxTime = 60f;      // Maximum seconds before key change
    public float phraseLengthBars = 8f;       // Bars per phrase for fills

    // Static instance for global access
    public static BackgroundMusic Instance { get; private set; }

    // Cop kill tracking
    private int copKillCount = 0;
    private float currentSpeedMultiplier = 1.0f;
    private float targetSpeedMultiplier = 1.0f;
    private float speedTransitionRate = 0.5f; // How fast to transition to new speed

    private int sampleRate;
    private float nextPauseTime;
    private bool isPaused = false;
    private float pauseEndTime;
    private bool playingPauseFill = false;

    // Walking bass state
    private int currentNote = 0;
    private int currentPattern = 0;
    private int currentKeyIndex = 0;
    private float nextKeyChangeTime;
    private int currentPhrase = 0;
    private int barsInCurrentPhrase = 0;

    // Percussion state
    private float percussionVolume = 0f;
    private float targetPercussionVolume = 0f;
    private AudioClip percussionLoopClip;

    // Percussion intermittent playback control
    private bool percussionActive = false;
    private float nextPercussionToggleTime = 0f;
    private float percussionFadeTarget = 0f;
    private float percussionFadeCurrent = 0f;

    [Header("Percussion Behavior")]
    public float minPercussionOnTime = 4f;      // Minimum seconds percussion plays
    public float maxPercussionOnTime = 12f;     // Maximum seconds percussion plays
    public float minPercussionOffTime = 6f;     // Minimum seconds of silence
    public float maxPercussionOffTime = 16f;    // Maximum seconds of silence
    public float percussionFadeSpeed = 0.8f;    // How fast percussion fades in/out

    // Keys/root notes (in Hz) - A minor, E minor, D minor, G major
    private float[][] keyScales = {
        // A minor scale (A1 = 55Hz base)
        new float[] { 55f, 61.74f, 65.41f, 73.42f, 82.41f, 87.31f, 98f, 110f },
        // E minor scale (E1 = 41.2Hz base)
        new float[] { 41.20f, 46.25f, 49.00f, 55f, 61.74f, 65.41f, 73.42f, 82.41f },
        // D minor scale (D1 = 36.71Hz base, but use D2 = 73.42Hz for better sound)
        new float[] { 36.71f, 41.20f, 43.65f, 49.00f, 55f, 58.27f, 65.41f, 73.42f },
        // G major scale (G1 = 49Hz base)
        new float[] { 49.00f, 55f, 61.74f, 65.41f, 73.42f, 82.41f, 92.50f, 98f },
    };

    private string[] keyNames = { "A minor", "E minor", "D minor", "G major" };

    // Walking patterns - more variety with different rhythmic feels
    // Format: scale degree (0-7), -1 = rest, 8+ = octave up
    private int[][] walkingPatterns = {
        // Classic walking patterns
        new int[] { 0, 2, 4, 2 },       // Root, 3rd, 5th, 3rd
        new int[] { 0, 4, 3, 4 },       // Root, 5th, 4th, 5th
        new int[] { 0, 2, 3, 4 },       // Walking up
        new int[] { 7, 5, 4, 2 },       // Walking down from octave
        new int[] { 0, 0, 4, 5 },       // Rhythmic root
        new int[] { 4, 3, 2, 0 },       // Walking down
        new int[] { 0, 7, 4, 2 },       // Jump to octave
        new int[] { 2, 4, 5, 7 },       // Climbing up

        // Syncopated patterns (using rests)
        new int[] { 0, -1, 4, 2 },      // Syncopated root-5th
        new int[] { -1, 0, -1, 4 },     // Off-beat hits
        new int[] { 0, 2, -1, 4 },      // Syncopated walk
        new int[] { 4, -1, 2, 0 },      // Syncopated descent

        // Octave jump patterns (8+ means add octave)
        new int[] { 0, 8, 4, 2 },       // Root to octave jump
        new int[] { 0, 4, 8, 4 },       // Mid-phrase octave
        new int[] { 8, 7, 5, 4 },       // Octave descent run
        new int[] { 0, 2, 4, 8 },       // Climbing to octave

        // Runs and fills
        new int[] { 0, 1, 2, 3 },       // Chromatic-ish run up
        new int[] { 7, 6, 5, 4 },       // Run down
        new int[] { 0, 3, 5, 7 },       // Arpeggiated up
        new int[] { 7, 4, 2, 0 },       // Arpeggiated down

        // Rhythmic/groove patterns
        new int[] { 0, 0, 0, 4 },       // Driving root
        new int[] { 0, 4, 0, 4 },       // Root-5th pulse
        new int[] { 0, -1, 0, 2 },      // Sparse groove
        new int[] { 4, 4, 0, 0 },       // Inverted pulse
    };

    // Fill patterns for phrase boundaries (more dramatic)
    private int[][] fillPatterns = {
        new int[] { 0, 2, 4, 7 },       // Ascending run
        new int[] { 8, 7, 5, 4 },       // Descending from octave
        new int[] { 0, 4, 8, 4 },       // Octave bounce
        new int[] { 7, 4, 0, 4 },       // Wide jump fill
        new int[] { 0, 1, 2, 3 },       // Chromatic climb
        new int[] { 4, 3, 2, 0 },       // Smooth descent
    };

    // Alternative bass notes for pause fills (E minor - different key)
    private float[] altBassNotes = {
        41.20f, // E1
        46.25f, // F#1
        49.00f, // G1
        55f,    // A1
        61.74f, // B1
        65.41f, // C2
        73.42f, // D2
        82.41f, // E2
    };

    // Alternative patterns for pause fills (more sparse, different rhythm)
    private int[][] altWalkingPatterns = {
        new int[] { 0, -1, 4, -1 },     // Root, rest, 5th, rest (sparse)
        new int[] { 0, 0, -1, 4 },      // Syncopated
        new int[] { -1, 0, -1, 2 },     // Off-beat emphasis
        new int[] { 0, 3, -1, -1 },     // Very sparse
        new int[] { 4, -1, 0, -1 },     // Inverted sparse
        new int[] { 0, -1, -1, 0 },     // Minimal pulse
    };

    // File names to look for in Resources folder
    public string musicFileName = "music";
    public string startSoundFileName = "start_sound";

    private AudioClip loadedMusic;
    private AudioClip loadedStartSound;
    private AudioClip[] pauseFillClips; // Pre-generated alternative bass clips
    private AudioClip[] bassClipsPerKey; // Pre-generated bass clips for each key

    void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;

        // Try to load audio files from Resources
        loadedMusic = Resources.Load<AudioClip>(musicFileName);
        loadedStartSound = Resources.Load<AudioClip>(startSoundFileName);

        if (loadedMusic != null)
            Debug.Log("Loaded music: " + musicFileName);
        else
            Debug.Log("No music file found, using procedural bassline");

        if (loadedStartSound != null)
            Debug.Log("Loaded start sound: " + startSoundFileName);
        else
            Debug.Log("No start sound file found, using procedural beeps");

        // Start sound source
        startSource = gameObject.AddComponent<AudioSource>();
        startSource.spatialBlend = 0f;
        startSource.volume = 0.2f; // Reduced from 0.5f - startup sounds were too loud

        // Music source
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.spatialBlend = 0f;

        // Pause fill source (for alternative bass during pauses)
        pauseFillSource = gameObject.AddComponent<AudioSource>();
        pauseFillSource.loop = false;
        pauseFillSource.volume = musicVolume * 0.8f; // Slightly quieter
        pauseFillSource.spatialBlend = 0f;

        // Percussion source (kicks and hi-hats)
        percussionSource = gameObject.AddComponent<AudioSource>();
        percussionSource.loop = true;
        percussionSource.volume = 0f; // Starts silent, fades in with cop kills
        percussionSource.spatialBlend = 0f;

        // Pre-generate clips
        GeneratePauseFillClips();
        GenerateBassClipsForAllKeys();
        GeneratePercussionLoop();

        // Initialize key change timing
        ScheduleNextKeyChange();

        // Play ready-start sequence, then start music
        StartCoroutine(ReadyStartSequence());
    }

    void ScheduleNextKeyChange()
    {
        nextKeyChangeTime = Time.time + UnityEngine.Random.Range(keyChangeMinTime, keyChangeMaxTime);
    }

    void ChangeKey()
    {
        int previousKey = currentKeyIndex;

        // Pick a different key
        do {
            currentKeyIndex = UnityEngine.Random.Range(0, keyScales.Length);
        } while (currentKeyIndex == previousKey && keyScales.Length > 1);

        Debug.Log($"Key change: {keyNames[previousKey]} -> {keyNames[currentKeyIndex]}");

        // Switch to the new key's bass clip
        if (bassClipsPerKey != null && bassClipsPerKey.Length > currentKeyIndex)
        {
            float currentTime = musicSource.time;
            musicSource.clip = bassClipsPerKey[currentKeyIndex];
            musicSource.time = currentTime % musicSource.clip.length;
            musicSource.Play();
        }

        ScheduleNextKeyChange();
    }

    IEnumerator ReadyStartSequence()
    {
        // Use loaded start sound if available, otherwise procedural
        if (loadedStartSound != null)
        {
            startSource.PlayOneShot(loadedStartSound, 0.08f); // Reduced from 0.2f - startup sounds were too loud
            yield return new WaitForSeconds(loadedStartSound.length + 0.2f);
        }
        else
        {
            // Procedural: Three beeps then GO - softer and warmer
            float beepInterval = 0.6f;

            // "Ready" beeps - ascending pitch, reduced volume (was 0.25, 0.28, 0.3)
            PlayBeep(440f, 0.15f, 0.1f);  // A4
            yield return new WaitForSeconds(beepInterval);

            PlayBeep(554.37f, 0.15f, 0.11f);  // C#5
            yield return new WaitForSeconds(beepInterval);

            PlayBeep(659.25f, 0.15f, 0.12f);  // E5
            yield return new WaitForSeconds(beepInterval);

            // "GO!" - big chord, lower volume
            PlayGoSound();
            yield return new WaitForSeconds(0.3f);
        }

        // Use loaded music if available, otherwise procedural bassline
        if (loadedMusic != null)
        {
            Debug.Log("Playing loaded music file");
            musicSource.clip = loadedMusic;
        }
        else
        {
            Debug.Log("Creating procedural walking bassline...");
            // Start with first key (A minor)
            currentKeyIndex = 0;
            if (bassClipsPerKey != null && bassClipsPerKey.Length > 0)
            {
                musicSource.clip = bassClipsPerKey[0];
            }
            else
            {
                musicSource.clip = CreateWalkingBassClip(keyScales[0], true);
            }
            Debug.Log("Bassline created: " + musicSource.clip.length + " seconds");
        }
        musicSource.Play();

        // Start percussion loop (initially silent)
        if (percussionLoopClip != null)
        {
            percussionSource.clip = percussionLoopClip;
            percussionSource.Play();
        }

        Debug.Log("Music playing: " + musicSource.isPlaying + " volume: " + musicSource.volume);

        // Schedule first pause
        ScheduleNextPause();
    }

    void PlayBeep(float freq, float duration, float volume)
    {
        int samples = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Beep", samples, 1, sampleRate, false);
        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Sin((float)i / samples * Mathf.PI); // Smooth envelope

            // Clean synth tone with slight detune for thickness
            float sample = Mathf.Sin(t * freq * 2f * Mathf.PI) * 0.6f;
            sample += Mathf.Sin(t * freq * 2.01f * 2f * Mathf.PI) * 0.3f; // Slight detune
            sample += Mathf.Sin(t * freq * 0.5f * 2f * Mathf.PI) * 0.2f; // Sub octave

            data[i] = sample * env * 0.5f;
        }

        clip.SetData(data, 0);
        startSource.PlayOneShot(clip, volume);
    }

    void PlayGoSound()
    {
        float duration = 0.5f;
        int samples = (int)(sampleRate * duration);
        AudioClip clip = AudioClip.Create("Go", samples, 1, sampleRate, false);
        float[] data = new float[samples];

        // Big synth chord: A major (A4, C#5, E5) with bass
        float[] chordFreqs = { 220f, 277.18f, 329.63f, 440f, 554.37f, 110f };

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 3f) * (1f - Mathf.Exp(-t * 50f)); // Fast attack, medium decay

            float sample = 0f;
            for (int n = 0; n < chordFreqs.Length; n++)
            {
                float noteVol = n < 3 ? 0.25f : (n == 5 ? 0.35f : 0.15f);
                sample += Mathf.Sin(t * chordFreqs[n] * 2f * Mathf.PI) * noteVol;
            }

            // Add some synth grit
            sample += Mathf.Sin(t * 220f * 3f * 2f * Mathf.PI) * 0.1f * env; // 3rd harmonic

            data[i] = sample * env * 0.7f;
        }

        clip.SetData(data, 0);
        startSource.PlayOneShot(clip, 0.15f); // Reduced from 0.35f - startup sounds were too loud
    }

    void GenerateBassClipsForAllKeys()
    {
        bassClipsPerKey = new AudioClip[keyScales.Length];
        for (int i = 0; i < keyScales.Length; i++)
        {
            bassClipsPerKey[i] = CreateWalkingBassClip(keyScales[i], true);
            Debug.Log($"Generated bass clip for {keyNames[i]}");
        }
    }

    AudioClip CreateWalkingBassClip(float[] bassNotes, bool withVariety)
    {
        // Create 16 bars of walking bass (loops) - longer for more variety
        float beatsPerBar = 4f;
        float bars = 16f;
        float totalBeats = beatsPerBar * bars;
        float secondsPerBeat = 60f / bpm;
        float totalDuration = totalBeats * secondsPerBeat;

        int totalSamples = (int)(sampleRate * totalDuration);
        float[] data = new float[totalSamples];

        int samplesPerBeat = (int)(sampleRate * secondsPerBeat);

        // Track which pattern we're using and when to switch
        int currentPatternIndex = UnityEngine.Random.Range(0, walkingPatterns.Length);
        int barsUntilPatternChange = UnityEngine.Random.Range(2, 5);

        // Track phrase for fills
        int currentBar = 0;
        int phraseBars = (int)phraseLengthBars;

        // Slide state
        float slideFromFreq = 0f;
        float slideToFreq = 0f;
        bool isSliding = false;
        int slideSamples = 0;

        // Generate each beat
        for (int beat = 0; beat < (int)totalBeats; beat++)
        {
            int beatInBar = beat % 4;
            int bar = beat / 4;

            // Check for bar change
            if (beatInBar == 0 && bar != currentBar)
            {
                currentBar = bar;
                barsUntilPatternChange--;

                // Change pattern periodically
                if (barsUntilPatternChange <= 0)
                {
                    currentPatternIndex = UnityEngine.Random.Range(0, walkingPatterns.Length);
                    barsUntilPatternChange = UnityEngine.Random.Range(2, 5);
                }

                // Check for phrase boundary (every 8 or 16 bars)
                if (bar % phraseBars == phraseBars - 1)
                {
                    // Use a fill pattern for variety
                    if (UnityEngine.Random.value > 0.4f) // 60% chance of fill
                    {
                        currentPatternIndex = walkingPatterns.Length + UnityEngine.Random.Range(0, fillPatterns.Length);
                    }
                }
            }

            // Get pattern (either regular or fill)
            int[] pattern;
            if (currentPatternIndex >= walkingPatterns.Length)
            {
                pattern = fillPatterns[currentPatternIndex - walkingPatterns.Length];
            }
            else
            {
                pattern = walkingPatterns[currentPatternIndex];
            }

            // Get note from pattern
            int noteIdx = pattern[beatInBar];

            // Skip rests
            if (noteIdx < 0)
            {
                continue;
            }

            // Handle octave jumps (8+ means add octave)
            float octaveMultiplier = 1f;
            if (noteIdx >= 8)
            {
                noteIdx -= 8;
                octaveMultiplier = 2f;
            }

            float noteFreq = bassNotes[noteIdx % bassNotes.Length] * octaveMultiplier;

            // Slight humanization
            float freqVariation = 1f + (UnityEngine.Random.value - 0.5f) * 0.008f;
            noteFreq *= freqVariation;

            // Occasional slide to next note (5% chance on beat 4)
            if (beatInBar == 3 && UnityEngine.Random.value < 0.05f && withVariety)
            {
                isSliding = true;
                slideFromFreq = noteFreq;
                // Slide to next bar's first note
                int nextNoteIdx = pattern[0];
                if (nextNoteIdx >= 0)
                {
                    float nextOctave = nextNoteIdx >= 8 ? 2f : 1f;
                    int nextIdx = nextNoteIdx >= 8 ? nextNoteIdx - 8 : nextNoteIdx;
                    slideToFreq = bassNotes[nextIdx % bassNotes.Length] * nextOctave;
                    slideSamples = samplesPerBeat / 4; // Slide over last quarter of beat
                }
                else
                {
                    isSliding = false;
                }
            }

            int startSample = beat * samplesPerBeat;

            // Generate the bass note with groove
            for (int i = 0; i < samplesPerBeat && (startSample + i) < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float beatPos = (float)i / samplesPerBeat;

                // Handle slide
                float currentFreq = noteFreq;
                if (isSliding && beatPos > 0.75f)
                {
                    float slideProgress = (beatPos - 0.75f) / 0.25f;
                    currentFreq = Mathf.Lerp(slideFromFreq, slideToFreq, slideProgress);
                }

                // Envelope: punchy attack, sustain, slight release at end
                float attack = 1f - Mathf.Exp(-t * 60f);
                float sustain = Mathf.Exp(-t * 2.5f) * 0.7f + 0.3f;
                float release = beatPos > 0.85f ? (1f - (beatPos - 0.85f) / 0.15f) : 1f;
                float env = attack * sustain * release;

                // Synth bass with harmonics for that funky sound
                float sample = 0f;

                // Fundamental
                sample += Mathf.Sin(t * currentFreq * 2f * Mathf.PI) * 0.5f;

                // Sub (octave down for weight)
                sample += Mathf.Sin(t * currentFreq * 0.5f * 2f * Mathf.PI) * 0.25f;

                // 2nd harmonic (adds brightness)
                sample += Mathf.Sin(t * currentFreq * 2f * 2f * Mathf.PI) * 0.15f;

                // 3rd harmonic (adds grit/growl)
                sample += Mathf.Sin(t * currentFreq * 3f * 2f * Mathf.PI) * 0.08f;

                // Slight saw-like character (odd harmonics)
                sample += Mathf.Sin(t * currentFreq * 5f * 2f * Mathf.PI) * 0.04f;

                // Filter sweep on attack (simulated with envelope on harmonics)
                float filterEnv = Mathf.Exp(-t * 8f);
                sample += Mathf.Sin(t * currentFreq * 4f * 2f * Mathf.PI) * 0.06f * filterEnv;

                // Ghost note / pickup on off-beats for groove
                if (beatInBar == 1 || beatInBar == 3)
                {
                    float ghostTime = t - secondsPerBeat * 0.45f;
                    if (ghostTime > 0 && ghostTime < 0.08f)
                    {
                        float ghostEnv = Mathf.Sin(ghostTime / 0.08f * Mathf.PI);
                        float ghostNote = bassNotes[(noteIdx + 2) % bassNotes.Length] * 2f; // Higher ghost
                        sample += Mathf.Sin(ghostTime * ghostNote * 2f * Mathf.PI) * 0.12f * ghostEnv;
                    }
                }

                // Accent on beat 1
                float accent = (beatInBar == 0) ? 1.2f : 1f;

                // Extra accent at phrase boundaries
                if (bar % phraseBars == 0 && beatInBar == 0)
                {
                    accent *= 1.15f;
                }

                data[startSample + i] += sample * env * accent * 0.8f;
            }

            // Reset slide state
            if (beatInBar == 3)
            {
                isSliding = false;
            }
        }

        // Soft clip / saturation for warmth
        for (int i = 0; i < totalSamples; i++)
        {
            data[i] = Mathf.Atan(data[i] * 1.5f) / 1.2f;
        }

        AudioClip clip = AudioClip.Create("WalkingBass", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void GeneratePercussionLoop()
    {
        // Create 16 bars of percussion (kick and hi-hat) with built-in gaps
        float beatsPerBar = 4f;
        float bars = 16f;
        float totalBeats = beatsPerBar * bars;
        float secondsPerBeat = 60f / bpm;
        float totalDuration = totalBeats * secondsPerBeat;

        int totalSamples = (int)(sampleRate * totalDuration);
        float[] data = new float[totalSamples];

        int samplesPerBeat = (int)(sampleRate * secondsPerBeat);
        int samplesPerEighth = samplesPerBeat / 2;

        // Generate kick and hi-hat pattern with gaps and variation
        for (int beat = 0; beat < (int)totalBeats; beat++)
        {
            int beatInBar = beat % 4;
            int bar = beat / 4;
            int startSample = beat * samplesPerBeat;

            // Skip percussion on certain bars for built-in breaks
            // Bars 4, 5, 12, 13 are silent (25% of the loop is silent)
            bool isRestBar = (bar == 4 || bar == 5 || bar == 12 || bar == 13);
            if (isRestBar)
            {
                continue;
            }

            // Kick drum only on beat 1 (not beat 3) - half as many kicks
            // Occasionally add kick on beat 3 for variety (30% chance)
            bool playKick = (beatInBar == 0) || (beatInBar == 2 && UnityEngine.Random.value < 0.3f);
            if (playKick)
            {
                GenerateKickDrum(data, startSample, samplesPerBeat);
            }

            // Hi-hats on 8th notes only (not 16ths) - half as fast
            // Skip some hi-hats randomly for breathing room (20% skip chance)
            for (int eighth = 0; eighth < 2; eighth++)
            {
                // Random chance to skip this hi-hat
                if (UnityEngine.Random.value < 0.2f)
                {
                    continue;
                }

                int hihatStart = startSample + eighth * samplesPerEighth;

                // Accent pattern: stronger on downbeats, lighter on off-beats
                float hihatVolume = 0.12f; // Base volume reduced
                if (eighth == 0 && beatInBar == 0) hihatVolume = 0.18f; // Downbeat of bar
                else if (eighth == 0) hihatVolume = 0.15f; // Other downbeats

                GenerateHiHat(data, hihatStart, samplesPerEighth, hihatVolume);
            }
        }

        // Normalize and soft clip
        float maxAmp = 0f;
        for (int i = 0; i < totalSamples; i++)
        {
            maxAmp = Mathf.Max(maxAmp, Mathf.Abs(data[i]));
        }
        if (maxAmp > 0.9f)
        {
            float normalize = 0.9f / maxAmp;
            for (int i = 0; i < totalSamples; i++)
            {
                data[i] *= normalize;
            }
        }

        percussionLoopClip = AudioClip.Create("PercussionLoop", totalSamples, 1, sampleRate, false);
        percussionLoopClip.SetData(data, 0);
        Debug.Log("Generated relaxed percussion loop with gaps");
    }

    void GenerateKickDrum(float[] data, int startSample, int maxSamples)
    {
        // Synthesize a punchy kick drum
        float duration = 0.15f;
        int samples = Mathf.Min((int)(sampleRate * duration), maxSamples);

        for (int i = 0; i < samples && (startSample + i) < data.Length; i++)
        {
            float t = (float)i / sampleRate;

            // Pitch envelope - starts high, drops quickly
            float pitchEnv = Mathf.Exp(-t * 40f);
            float freq = 50f + 100f * pitchEnv;

            // Amplitude envelope
            float ampEnv = Mathf.Exp(-t * 15f) * (1f - Mathf.Exp(-t * 200f));

            // Sine wave with pitch drop
            float phase = 0f;
            for (int j = 0; j <= i; j++)
            {
                float tj = (float)j / sampleRate;
                float fj = 50f + 100f * Mathf.Exp(-tj * 40f);
                phase += fj / sampleRate;
            }

            float sample = Mathf.Sin(phase * 2f * Mathf.PI) * ampEnv * 0.6f;

            // Add some click/attack
            if (t < 0.005f)
            {
                sample += (UnityEngine.Random.value - 0.5f) * 0.3f * (1f - t / 0.005f);
            }

            data[startSample + i] += sample;
        }
    }

    void GenerateHiHat(float[] data, int startSample, int maxSamples, float volume)
    {
        // Synthesize a hi-hat using filtered noise
        float duration = 0.05f;
        int samples = Mathf.Min((int)(sampleRate * duration), maxSamples);

        for (int i = 0; i < samples && (startSample + i) < data.Length; i++)
        {
            float t = (float)i / sampleRate;

            // Sharp attack, quick decay
            float env = Mathf.Exp(-t * 80f) * (1f - Mathf.Exp(-t * 500f));

            // High-frequency noise (simulate with multiple high frequencies)
            float sample = 0f;
            sample += Mathf.Sin(t * 6000f * 2f * Mathf.PI) * 0.3f;
            sample += Mathf.Sin(t * 8000f * 2f * Mathf.PI) * 0.25f;
            sample += Mathf.Sin(t * 10000f * 2f * Mathf.PI) * 0.2f;
            sample += Mathf.Sin(t * 12000f * 2f * Mathf.PI) * 0.15f;

            // Add some noise character
            sample += (UnityEngine.Random.value - 0.5f) * 0.4f;

            data[startSample + i] += sample * env * volume;
        }
    }

    void Update()
    {
        if (musicSource == null || musicSource.clip == null) return;

        float time = Time.time;

        // Smoothly transition to target speed
        if (Mathf.Abs(currentSpeedMultiplier - targetSpeedMultiplier) > 0.001f)
        {
            currentSpeedMultiplier = Mathf.Lerp(currentSpeedMultiplier, targetSpeedMultiplier,
                Time.deltaTime * speedTransitionRate);
            musicSource.pitch = currentSpeedMultiplier;
            pauseFillSource.pitch = currentSpeedMultiplier;
            percussionSource.pitch = currentSpeedMultiplier;
        }

        // Smoothly transition percussion volume
        if (Mathf.Abs(percussionVolume - targetPercussionVolume) > 0.001f)
        {
            percussionVolume = Mathf.Lerp(percussionVolume, targetPercussionVolume, Time.deltaTime * 0.5f);
            percussionSource.volume = percussionVolume * musicVolume;
        }

        // Check for key change (only when not paused and using procedural music)
        if (!isPaused && loadedMusic == null && time >= nextKeyChangeTime)
        {
            ChangeKey();
        }

        // Handle pauses
        if (isPaused)
        {
            // Check if pause should end
            if (time >= pauseEndTime)
            {
                isPaused = false;
                playingPauseFill = false;

                // Stop any pause fill that might be playing
                if (pauseFillSource.isPlaying)
                {
                    pauseFillSource.Stop();
                }

                musicSource.UnPause();
                if (percussionSource.clip != null)
                {
                    percussionSource.UnPause();
                }
                ScheduleNextPause();
            }
        }
        else
        {
            // Check if it's time to pause
            if (time >= nextPauseTime && musicSource.isPlaying)
            {
                isPaused = true;
                musicSource.Pause();
                if (percussionSource.isPlaying)
                {
                    percussionSource.Pause();
                }
                float pauseDuration = UnityEngine.Random.Range(minPauseDuration, maxPauseDuration);
                pauseEndTime = time + pauseDuration;

                // Decide whether to play silence or alternative bass
                if (UnityEngine.Random.value > silenceChance)
                {
                    // Play alternative bass fill
                    StartPauseFill(pauseDuration);
                }
                // Otherwise complete silence
            }

            // Ensure music keeps playing (in case it stopped somehow)
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }
    }

    void ScheduleNextPause()
    {
        nextPauseTime = Time.time + UnityEngine.Random.Range(minTimeBetweenPauses, maxTimeBetweenPauses);
    }

    /// <summary>
    /// Called when a cop is destroyed. Increases music speed gradually.
    /// </summary>
    public static void OnCopKilled()
    {
        if (Instance != null)
        {
            Instance.RegisterCopKill();
        }
    }

    /// <summary>
    /// Instance method to register a cop kill and update speed.
    /// </summary>
    public void RegisterCopKill()
    {
        copKillCount++;

        // Check for victory condition at 10 kills
        if (copKillCount >= victoryKillCount)
        {
            TriggerVictory();
            return;
        }

        // Calculate target speed multiplier based on kills
        // At 0 kills: 1.0x, at maxCopKillsForSpeedup: maxSpeedMultiplier
        float progress = Mathf.Clamp01((float)copKillCount / maxCopKillsForSpeedup);

        // Use smooth easing for more natural acceleration feel
        float easedProgress = EaseInQuad(progress);

        targetSpeedMultiplier = 1.0f + (maxSpeedMultiplier - 1.0f) * easedProgress;

        // Increase percussion volume based on cop kills
        // Starts fading in at 2 kills, full volume at 6+ kills
        if (copKillCount >= 2)
        {
            float percProgress = Mathf.Clamp01((float)(copKillCount - 2) / 4f);
            targetPercussionVolume = percProgress * 0.7f; // Max 70% of music volume
        }

        Debug.Log($"Cop killed! Count: {copKillCount}, Speed multiplier: {targetSpeedMultiplier:F2}x, Percussion: {targetPercussionVolume:F2}");
    }

    /// <summary>
    /// Triggers the victory state - resets music speed and pauses music.
    /// </summary>
    private void TriggerVictory()
    {
        Debug.Log($"VICTORY! Player destroyed {victoryKillCount} cops!");

        // Reset speed to normal
        targetSpeedMultiplier = 1.0f;
        currentSpeedMultiplier = 1.0f;
        if (musicSource != null)
        {
            musicSource.pitch = 1.0f;
        }
        if (pauseFillSource != null)
        {
            pauseFillSource.pitch = 1.0f;
        }
        if (percussionSource != null)
        {
            percussionSource.pitch = 1.0f;
        }

        // Pause the music
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Pause();
        }
        if (pauseFillSource != null && pauseFillSource.isPlaying)
        {
            pauseFillSource.Stop();
        }
        if (percussionSource != null && percussionSource.isPlaying)
        {
            percussionSource.Pause();
        }

        // Mark as paused to stop the Update loop from restarting music
        isPaused = true;
        pauseEndTime = float.MaxValue; // Keep it paused indefinitely

        // Fire victory event for other scripts to listen to
        OnVictory?.Invoke();
    }

    /// <summary>
    /// Returns true if the player has achieved victory.
    /// </summary>
    public bool HasAchievedVictory()
    {
        return copKillCount >= victoryKillCount;
    }

    /// <summary>
    /// Quadratic ease-in for smooth speed ramping.
    /// </summary>
    private float EaseInQuad(float t)
    {
        return t * t;
    }

    /// <summary>
    /// Resets cop kill count and music speed (e.g., for new game).
    /// Also resumes music if it was paused due to victory.
    /// </summary>
    public void ResetCopKills()
    {
        copKillCount = 0;
        targetSpeedMultiplier = 1.0f;
        currentSpeedMultiplier = 1.0f;
        targetPercussionVolume = 0f;
        percussionVolume = 0f;

        if (musicSource != null)
        {
            musicSource.pitch = 1.0f;
        }
        if (pauseFillSource != null)
        {
            pauseFillSource.pitch = 1.0f;
        }
        if (percussionSource != null)
        {
            percussionSource.pitch = 1.0f;
            percussionSource.volume = 0f;
        }

        // Reset to first key
        currentKeyIndex = 0;
        if (bassClipsPerKey != null && bassClipsPerKey.Length > 0 && loadedMusic == null)
        {
            musicSource.clip = bassClipsPerKey[0];
        }

        // If paused due to victory, unpause and schedule next pause
        if (isPaused && pauseEndTime == float.MaxValue)
        {
            isPaused = false;
            if (musicSource != null)
            {
                musicSource.UnPause();
            }
            if (percussionSource != null && percussionSource.clip != null)
            {
                percussionSource.UnPause();
            }
            ScheduleNextPause();
        }

        ScheduleNextKeyChange();
    }

    /// <summary>
    /// Gets the current cop kill count.
    /// </summary>
    public int GetCopKillCount()
    {
        return copKillCount;
    }

    /// <summary>
    /// Pre-generate several pause fill clips for variety.
    /// </summary>
    void GeneratePauseFillClips()
    {
        int numClips = 4;
        pauseFillClips = new AudioClip[numClips];

        for (int i = 0; i < numClips; i++)
        {
            pauseFillClips[i] = CreateAlternativeBassClip(i);
        }

        Debug.Log($"Generated {numClips} pause fill clips");
    }

    /// <summary>
    /// Creates an alternative bass clip with different key and pattern.
    /// </summary>
    AudioClip CreateAlternativeBassClip(int variation)
    {
        // Create 4 bars of alternative walking bass
        float beatsPerBar = 4f;
        float bars = 4f;
        float totalBeats = beatsPerBar * bars;
        float secondsPerBeat = 60f / bpm;
        float totalDuration = totalBeats * secondsPerBeat;

        int totalSamples = (int)(sampleRate * totalDuration);
        float[] data = new float[totalSamples];

        int samplesPerBeat = (int)(sampleRate * secondsPerBeat);

        // Select pattern based on variation
        int patternOffset = variation % altWalkingPatterns.Length;

        // Generate each beat
        for (int beat = 0; beat < (int)totalBeats; beat++)
        {
            int beatInBar = beat % 4;
            int bar = beat / 4;

            // Pick pattern based on bar and variation
            int patternIdx = (bar + patternOffset) % altWalkingPatterns.Length;
            int[] pattern = altWalkingPatterns[patternIdx];

            // Get note from pattern (-1 means rest/silence)
            int noteIdx = pattern[beatInBar];

            if (noteIdx < 0) continue; // Skip rests

            float noteFreq = altBassNotes[noteIdx % altBassNotes.Length];

            // Slight humanization
            float freqVariation = 1f + (UnityEngine.Random.value - 0.5f) * 0.01f;
            noteFreq *= freqVariation;

            int startSample = beat * samplesPerBeat;

            // Generate the bass note with different character than main bass
            for (int i = 0; i < samplesPerBeat && (startSample + i) < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float beatPos = (float)i / samplesPerBeat;

                // Softer envelope for pause fills
                float attack = 1f - Mathf.Exp(-t * 40f);
                float sustain = Mathf.Exp(-t * 3.5f) * 0.6f + 0.4f;
                float release = beatPos > 0.8f ? (1f - (beatPos - 0.8f) / 0.2f) : 1f;
                float env = attack * sustain * release;

                // Darker, moodier bass tone
                float sample = 0f;

                // Fundamental (stronger for darker sound)
                sample += Mathf.Sin(t * noteFreq * 2f * Mathf.PI) * 0.55f;

                // Deep sub
                sample += Mathf.Sin(t * noteFreq * 0.5f * 2f * Mathf.PI) * 0.35f;

                // Less bright harmonics
                sample += Mathf.Sin(t * noteFreq * 2f * 2f * Mathf.PI) * 0.08f;

                // Slight triangle wave character
                float phase = (t * noteFreq) % 1f;
                float triangle = Mathf.Abs(phase - 0.5f) * 4f - 1f;
                sample += triangle * 0.06f;

                data[startSample + i] += sample * env * 0.7f;
            }
        }

        // Soft clip for warmth
        for (int i = 0; i < totalSamples; i++)
        {
            data[i] = Mathf.Atan(data[i] * 1.3f) / 1.1f;
        }

        AudioClip clip = AudioClip.Create($"AltBass_{variation}", totalSamples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    /// <summary>
    /// Start playing an alternative bass fill during a pause.
    /// </summary>
    void StartPauseFill(float pauseDuration)
    {
        if (pauseFillClips == null || pauseFillClips.Length == 0) return;

        // Pick a random fill clip
        int clipIndex = UnityEngine.Random.Range(0, pauseFillClips.Length);
        AudioClip fillClip = pauseFillClips[clipIndex];

        if (fillClip == null) return;

        playingPauseFill = true;
        pauseFillSource.pitch = currentSpeedMultiplier;
        pauseFillSource.clip = fillClip;
        pauseFillSource.loop = true; // Loop if pause is longer than clip
        pauseFillSource.Play();

        Debug.Log($"Playing pause fill variation {clipIndex}");
    }
}
