# Cop Takedown

Unity 6 arcade driving game with fully procedural audio, textures, and geometry. Drive a WRX around an oval track, destroy 10 cop cars with a roof-mounted turret to win. Cops have a 5-state AI, there's a combo system with wanted levels, and slow-mo on the final kill.

## What Makes It Interesting

- **Procedural audio engine** — `BackgroundMusic.cs` (1100+ lines) generates a walking bassline with 24+ patterns, key changes, fills, and percussion entirely at the sample level. `CarAudio.cs` synthesizes engine, exhaust, turbo, tire, and wind sounds. No audio files.
- **Procedural textures** — `TextureManager.cs` generates asphalt, grass, concrete, and brick textures with normal maps using multi-octave Perlin noise and domain-warped crack patterns.
- **Procedural car body** — the player's WRX is built from ~40 Unity primitives (body panels, spoiler, hood scoop, headlights, exhaust tips, wheels).
- **Cop AI state machine** — 5 states (Patrol, Flee, DonutBreak, Pursuit, Chase) with emergent behavior. Cops navigate to random targets, flee when shot, take donut breaks at Dunkin stores, and pursue with ramming and shooting.
- **Full game loop** — combo system, 4-star wanted level escalation, slow-motion final kill, leaderboard with persistence, restart support.

## Controls

**Keyboard:**
- WASD / Arrow keys — drive
- Mouse — aim turret
- Left click — fire
- Space — handbrake
- R — reload
- Tab — turret mode toggle
- E — boost
- Q — jump

**Controller (Xbox/PS):**
- Left stick — steer
- RT/LT — gas/brake
- Right stick — aim turret
- A/X — fire
- B/Circle — reload
- Y/Triangle — jump
- LB — boost

## Running

1. Open in Unity Hub (Unity 6 / 6000.3.2f1)
2. Open the scene `Assets/t.unity`
3. Press Play

## Tech Stack

- Unity 6, C# (27 scripts, ~12,000 lines)
- Built-in Render Pipeline + Post Processing Stack v2
- New Input System (keyboard + controller)
- Zero external audio/texture/model assets
