RESOURCES FOLDER
================

Drop your custom assets here. They load automatically at runtime.


=== TEXTURES ===
Location: Assets/Resources/Textures/

| File          | Size              | Format   | Notes                    |
|---------------|-------------------|----------|--------------------------|
| grass.png     | 512x512 / 1024x1024 | PNG/JPG  | Seamless/tileable        |
| road.png      | 512x512 / 1024x1024 | PNG/JPG  | Seamless/tileable        |
| car_wrap.png  | 1024x1024 / 2048x2048 | PNG    | Player car body texture  |


=== AUDIO ===
Location: Assets/Resources/

| File              | Format        | Specs                    | Notes                    |
|-------------------|---------------|--------------------------|--------------------------|
| music.ogg         | OGG/MP3/WAV   | 44100 Hz, stereo/mono    | Background music, loops  |
| start_sound.ogg   | OGG/WAV       | 44100 Hz, 1-3 seconds    | Countdown/ready sound    |

RECOMMENDED AUDIO SPECS:
- Sample rate: 44100 Hz (standard)
- Format: OGG (best compression + quality for Unity)
- Bit depth: 16-bit for WAV, 128-320 kbps for OGG/MP3
- Music: Something loopable, 100-140 BPM works well
- Start sound: Countdown beeps, fanfare, or "3-2-1-GO!"

FALLBACK:
If no files found, procedural audio plays automatically:
- Music → Groovy synth walking bassline (95 BPM)
- Start → Ascending beeps + synth chord


=== HOW TO ADD ===

1. Drop files into the correct folder
2. Name them EXACTLY as shown (no extension in the name for code)
3. Unity auto-imports on save
4. Press Play - assets load automatically!

For textures: Textures/grass.png, Textures/road.png, Textures/car_wrap.png
For audio: music.ogg, start_sound.ogg (in Resources root)
