# 🎹 Piano Sample Sets in Rhythm / Piano-Tiles Style Games

This document summarizes common practices and design decisions when working with per-note audio samples ("note clips") in casual rhythm or piano-tiles style games.

## 1. What does **one sample file** represent?
* A single **AudioClip** (`.ogg`, `.wav`) recorded at a specific pitch.
* Equivalent to a **MIDI note** or one physical key on a real piano.
* Usually captured as a short, clean **Note-On** segment ‑ no long tail / sustain pedal.

## 2. Typical sample-set sizes
| #Files | Coverage                      | Pros                              | Cons |
|-------:|------------------------------|------------------------------------|------|
| 12-15  | 1 octave (chromatic)         | Tiny APK size                      | Heavy pitch-shift artifacts outside the octave |
| 30-50  | ≈3–4 octaves (chromatic)     | Good compromise for mobile; acceptable quality | Extremes (A0, C8) missing |
| 60-70  | 5–6 octaves                  | High quality; most classical pieces | Larger memory/download |
| 88     | Full grand piano (A0–C8)     | Studio quality, no pitch-shift      | Large size; long load times |

> In **Piano Tiles**-like games 30-50 samples are the sweet spot: mid-register classics sound authentic while keeping package size <10 MB.

## 3. Our current set (TilesWorld)
* Files: `piano_snd000.ogg … piano_snd044.ogg` → **45 samples**
* Assumed mapping: lowest file ≈ **A2 / 110 Hz** (just an example ‑ verify by ear).
* The `AudioConstants.SOUND_RESOURCE_IDXS` table distributes these 45 indices over **6 lanes** to keep higher pitches on top rows:

```text
Lane 0 : 24-44  (highest)
Lane 1 : 19-39
Lane 2 : 15-35
Lane 3 : 10-30
Lane 4 :  5-25
Lane 5 :  1-21  (lowest)
```

### Example
`GetSoundIndex(lane:1, pitch:6)` → `25` → `piano_snd025.ogg`

## 4. Completing missing notes
1. **Add more raw samples** and expand the array/tables.
2. **Pitch-shift in code**: `AudioSource.pitch = semitoneFactor`, but quality drops beyond ±3 semitones.
3. Combine: record every 3rd note (≈30 samples), pitch-shift the two in-between.

## 5. Implementing in Unity
```csharp
// Load clip array per instrument
AudioClip[] pianoClips = Resources.LoadAll<AudioClip>("Resources/Audio/Piano");

// Mapping
int soundIdx = AudioConstants.GetSoundIndex(lane, pitch); // table lookup
soundIdx = Mathf.Clamp(soundIdx, 0, pianoClips.Length - 1);

AudioSource src = GetAvailableSource();
src.clip   = pianoClips[soundIdx];
src.volume = master * sfx;
src.Play();
```
* Keep an **AudioSource pool** (200-500) to avoid runtime allocation spikes.
* Optional **fade-out & recycle** coroutine so a single tap sample doesn't hog the pool.

## 6. Best-practice tips
* Record samples dry; add reverb with AudioMixer at runtime (reusable across clips).
* Normalize but do **not** hard-limit ‑ retain natural dynamics; scale via `AudioSource.volume`.
* Name files sequentially (`piano_snd000`) to simplify integer indexing.
* Preload clips via `Addressables` or `Resources.LoadAll` during loading screen.

## 7. Advanced techniques (optional)

### 7.1 Middleware (Wwise / FMOD)
Standalone audio engines offer:
* Sample-accurate scheduling & ultra-low-latency playback.
* Built-in pitch-shift, randomization, ADSR, convolution reverb.
* Real-time parameter control (RTPC) — great for tempo curves or in-game key changes.

### 7.2 Velocity-layering
Record each note at 2-4 dynamic layers (pp, mf, ff…). Choose layer by touch pressure, hit accuracy or combo level for a more expressive sound.

### 7.3 Humanization / random variation
Apply subtle per-note randomization to reduce the "machine-gun" effect:
* Pitch ±1–2 cent
* Volume ±0.5 dB

### 7.4 Adaptive & parallel sample sets
For dynamic arrangements load multiple articulations (e.g. staccato vs legato) and cross-fade between them according to gameplay state.

---
### Road-map suggestion
| Step | Task |
|-----:|------|
| 🎚️ 1 | Keep the core set around **30-50** samples (current 45 is good). |
| 🛠️ 2 | Finalize **AudioSource pool** & Mixer routing. |
| ⚙️ 3 | Evaluate **FMOD / Wwise** integration for advanced control. |
| 🎵 4 | Add **velocity layers** & humanization tweaks. |

*Last updated: 2025-06-29* 