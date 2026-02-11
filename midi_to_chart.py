"""
MIDI to TilesWorld Chart JSON Converter v3
Smart chart generation with difficulty-based note selection, gap-fill, and auto track analysis.

Usage: 
  py midi_to_chart.py <midi_file> <music_id> [options]

Options:
  --tempo <bpm>          Override tempo (default: auto-detect from MIDI)
  --output <path>        Output JSON path
  --tracks <1,2,3>       Select specific tracks (default: auto-select based on difficulty)
  --offset <n>           MIDI note offset (default: 35)
  --difficulty <level>   easy|medium|hard|expert (default: medium)
  --no-fill              Disable gap-fill echoes

Examples:
  py midi_to_chart.py song.mid 23 --difficulty easy
  py midi_to_chart.py song.mid 23 --difficulty hard --tracks 1,2,3,5
  py midi_to_chart.py song.mid 23 --tempo 160 --output my_song.json
"""
import json
import sys
import os
import math
from collections import defaultdict, deque

try:
    import mido
except ImportError:
    print("Installing mido...")
    os.system("pip install mido")
    import mido


# === GAME CONSTANTS ===
SOUND_RESOURCE_IDXS = [
    [24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44],  # Lane 0: high
    [19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39],  # Lane 1
    [15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35],  # Lane 2: mid
    [10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30],  # Lane 3: mid
    [5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25],      # Lane 4
    [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21],          # Lane 5: low
]

NOTE_LENGTH_FACTORS = [1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56]

# Difficulty profiles
DIFFICULTY_PROFILES = {
    "easy":   {"max_nps": 4.0, "quantize_grid": 4, "max_simultaneous": 1, "tracks": "melody+bass","fill_gap": 0.5, "max_dur": 1},
    "medium": {"max_nps": 6.0, "quantize_grid": 8, "max_simultaneous": 2, "tracks": "all_main",  "fill_gap": 0.3, "max_dur": 1},
    "hard":   {"max_nps": 10.0,"quantize_grid": 16,"max_simultaneous": 3, "tracks": "all",       "fill_gap": 0.2, "max_dur": 1},
    "expert": {"max_nps": 99,  "quantize_grid": 32,"max_simultaneous": 6, "tracks": "all",       "fill_gap": 0.0, "max_dur": 1},
}


# === REVERSE LOOKUP ===
def build_smart_reverse_lookup():
    """Build sound_index -> (lane, pitch) with register-aware lane assignment."""
    lookup = {}
    for sound_idx in range(45):
        if sound_idx >= 30:
            for lane in [0, 1, 2]:
                if sound_idx in SOUND_RESOURCE_IDXS[lane]:
                    pitch = SOUND_RESOURCE_IDXS[lane].index(sound_idx)
                    lookup[sound_idx] = (lane, pitch)
                    break
        elif sound_idx >= 15:
            for lane in [2, 3, 1, 4]:
                if sound_idx in SOUND_RESOURCE_IDXS[lane]:
                    pitch = SOUND_RESOURCE_IDXS[lane].index(sound_idx)
                    lookup[sound_idx] = (lane, pitch)
                    break
        else:
            for lane in [4, 5, 3]:
                if sound_idx in SOUND_RESOURCE_IDXS[lane]:
                    pitch = SOUND_RESOURCE_IDXS[lane].index(sound_idx)
                    lookup[sound_idx] = (lane, pitch)
                    break
    return lookup

SMART_LOOKUP = build_smart_reverse_lookup()


def score_note_importance(start_sec, midi_note, dur_sec, velocity, tempo_bpm):
    """Heuristic note importance for chart reduction decisions."""
    beat_dur = 60.0 / max(1e-6, tempo_bpm)
    beat_offset = start_sec % beat_dur
    # Distance to nearest beat (normalized: 0 on-beat, 1 mid-beat)
    beat_distance = min(beat_offset, beat_dur - beat_offset) / max(1e-6, beat_dur * 0.5)
    beat_score = (1.0 - min(1.0, beat_distance)) * 35.0

    pitch_score = midi_note * 0.45
    vel_score = velocity * 0.30
    dur_score = min(dur_sec, 1.5) * 18.0
    return pitch_score + vel_score + dur_score + beat_score


def compress_gap_subdivisions(gap_subdivisions, max_dur_idx):
    """Soft-compress long gaps so charts remain playable without losing phrasing."""
    if gap_subdivisions <= 1:
        return 1.0

    if max_dur_idx <= 2:
        pivot = 4.0
        ratio = 0.45
    elif max_dur_idx <= 3:
        pivot = 6.0
        ratio = 0.55
    else:
        pivot = 8.0
        ratio = 0.70

    gap = float(gap_subdivisions)
    if gap <= pivot:
        return gap
    return pivot + (gap - pivot) * ratio


# === MIDI PARSING ===
def parse_midi_with_tempo_map(midi_path):
    """Parse MIDI file with proper tempo map support."""
    mid = mido.MidiFile(midi_path)
    
    tempo_map = []
    for track in mid.tracks:
        tick = 0
        for msg in track:
            tick += msg.time
            if msg.type == 'set_tempo':
                tempo_map.append((tick, msg.tempo))
    
    if not tempo_map:
        tempo_map = [(0, 500000)]
    tempo_map.sort()
    
    primary_bpm = mido.tempo2bpm(tempo_map[0][1])
    
    def tick_to_seconds(target_tick):
        current_tick = 0
        current_tempo = tempo_map[0][1]
        total_seconds = 0.0
        tempo_idx = 0
        
        while current_tick < target_tick:
            next_change_tick = target_tick
            if tempo_idx + 1 < len(tempo_map):
                next_change_tick = min(target_tick, tempo_map[tempo_idx + 1][0])
            
            delta_ticks = next_change_tick - current_tick
            total_seconds += mido.tick2second(delta_ticks, mid.ticks_per_beat, current_tempo)
            current_tick = next_change_tick
            
            if tempo_idx + 1 < len(tempo_map) and current_tick >= tempo_map[tempo_idx + 1][0]:
                tempo_idx += 1
                current_tempo = tempo_map[tempo_idx][1]
        
        return total_seconds
    
    return mid, tempo_map, primary_bpm, tick_to_seconds


# === TRACK ANALYSIS ===
def analyze_tracks(mid, tick_to_seconds):
    """Analyze each track and classify by musical role."""
    track_info = []
    
    for idx, track in enumerate(mid.tracks):
        t = 0
        notes = []
        active = defaultdict(list)
        
        for msg in track:
            t += msg.time
            if msg.type == 'note_on' and msg.velocity > 0:
                active[msg.note].append((t, msg.velocity))
            elif msg.type == 'note_off' or (msg.type == 'note_on' and msg.velocity == 0):
                if active[msg.note]:
                    start_tick, vel = active[msg.note].pop()
                    start_sec = tick_to_seconds(start_tick)
                    end_sec = tick_to_seconds(t)
                    notes.append((start_sec, msg.note, end_sec - start_sec, vel))
        
        if not notes:
            track_info.append({"idx": idx, "name": track.name, "notes": [], "role": "empty",
                              "avg_pitch": 0, "density": 0, "start": 0, "end": 0, "score": 0})
            continue
        
        midi_pitches = [n[1] for n in notes]
        avg_pitch = sum(midi_pitches) / len(midi_pitches)
        start_time = notes[0][0]
        end_time = notes[-1][0] + notes[-1][2]
        duration = end_time - start_time
        density = len(notes) / max(0.1, duration)  # notes per second
        
        # Classify role by register
        if avg_pitch >= 65:
            role = "melody_high"
        elif avg_pitch >= 55:
            role = "melody"
        elif avg_pitch >= 45:
            role = "harmony"
        elif avg_pitch >= 35:
            role = "bass"
        else:
            role = "bass_deep"
        
        # Score: prefer tracks that start early, have medium density, and are in melody range
        track_tick_length = sum(msg.time for msg in track)
        coverage = duration / max(0.1, tick_to_seconds(track_tick_length))
        melodic_score = max(0, 100 - abs(avg_pitch - 62) * 3)  # Peak at ~D4
        density_score = min(100, density * 15)  # Reward reasonable density
        early_score = max(0, 100 - start_time * 2)  # Reward early entry
        coverage_score = min(100, coverage * 100)

        total_score = (
            melodic_score * 0.35
            + density_score * 0.25
            + early_score * 0.20
            + coverage_score * 0.20
        )
        
        track_info.append({
            "idx": idx, "name": track.name, "notes": notes, "role": role,
            "avg_pitch": avg_pitch, "density": density,
            "start": start_time, "end": end_time, "score": total_score
        })
    
    return track_info


def auto_select_tracks(track_info, difficulty_tracks):
    """Auto-select tracks based on difficulty setting."""
    scored = [t for t in track_info if t["role"] != "empty"]
    scored.sort(key=lambda t: t["score"], reverse=True)
    
    if difficulty_tracks == "melody":
        # Pick top melody/melody_high track
        melody_tracks = [t for t in scored if "melody" in t["role"]]
        if melody_tracks:
            return {melody_tracks[0]["idx"]}
        return {scored[0]["idx"]} if scored else set()
    
    elif difficulty_tracks == "melody+bass":
        selected = set()
        # Pick best melody
        melody_tracks = [t for t in scored if "melody" in t["role"]]
        if melody_tracks:
            selected.add(melody_tracks[0]["idx"])
        # Pick best bass
        bass_tracks = [t for t in scored if "bass" in t["role"]]
        if bass_tracks:
            selected.add(bass_tracks[0]["idx"])
        if not selected and scored:
            selected = {scored[0]["idx"]}
        return selected
    
    elif difficulty_tracks == "all_main":
        # Pick top 3-4 scoring tracks
        return {t["idx"] for t in scored[:4]}
    
    else:  # "all"
        return {t["idx"] for t in scored}


# === NOTE PROCESSING ===
def extract_notes(mid, tick_to_seconds, track_indices):
    """Extract notes from selected tracks."""
    notes = []
    for track_idx, track in enumerate(mid.tracks):
        if track_indices and track_idx not in track_indices:
            continue
        current_time = 0
        active_notes = defaultdict(list)
        for msg in track:
            current_time += msg.time
            if msg.type == 'note_on' and msg.velocity > 0:
                active_notes[msg.note].append((current_time, msg.velocity))
            elif msg.type == 'note_off' or (msg.type == 'note_on' and msg.velocity == 0):
                if active_notes[msg.note]:
                    start_tick, velocity = active_notes[msg.note].pop()
                    start_sec = tick_to_seconds(start_tick)
                    end_sec = tick_to_seconds(current_time)
                    dur_sec = end_sec - start_sec
                    notes.append((start_sec, msg.note, dur_sec, velocity))
    notes.sort(key=lambda x: x[0])
    return notes


def quantize_notes(notes, tempo_bpm, grid_divisions):
    """Quantize note timings to a beat grid."""
    if grid_divisions <= 0 or not notes:
        return notes
    
    beat_dur = 60.0 / tempo_bpm
    grid_size = beat_dur / (grid_divisions / 4.0)  # grid_divisions per whole note
    
    quantized = []
    for start, note, dur, vel in notes:
        q_start = round(start / grid_size) * grid_size
        quantized.append((q_start, note, dur, vel))
    
    return quantized


def apply_nps_limit(notes, max_nps, max_simultaneous, tempo_bpm):
    """Filter notes to stay within target notes-per-second.
    
    Strategy: Use a sliding window. When NPS exceeds limit, keep notes
    with highest 'musical importance' (melody priority).
    Score: higher pitch notes get slightly more priority (tend to be melody).
    """
    if max_nps >= 50 or not notes:  # expert = no limit
        return notes
    
    window_size = 1.0  # real sliding 1-second window
    result = []
    kept_times = deque()

    # Group by quantized start instant, then keep most important notes first.
    slots = defaultdict(list)
    for n in notes:
        slot_key = round(n[0], 4)  # 0.1ms precision
        slots[slot_key].append(n)

    for slot in sorted(slots.keys()):
        slot_notes = slots[slot]
        slot_notes.sort(
            key=lambda n: score_note_importance(n[0], n[1], n[2], n[3], tempo_bpm),
            reverse=True,
        )
        slot_kept = 0
        for n in slot_notes:
            t = n[0]
            while kept_times and kept_times[0] < t - window_size:
                kept_times.popleft()
            if len(kept_times) >= max_nps:
                continue
            if slot_kept >= max_simultaneous:
                continue
            result.append(n)
            kept_times.append(t)
            slot_kept += 1

    return result


def apply_gap_fill(notes, tempo_bpm, max_gap_beats):
    """Fill gaps between notes with sustain echo notes.
    
    When there's a gap longer than max_gap_beats, insert echo notes
    (same pitch as previous note, short duration) to keep engagement.
    """
    if max_gap_beats <= 0 or not notes:
        return notes
    
    beat_dur = 60.0 / tempo_bpm
    max_gap_sec = max_gap_beats * beat_dur
    # Use sparse, musically placed passing notes (not machine-gun echoes).
    min_spacing = beat_dur * 0.5
    
    filled = list(notes)
    echoes_added = 0
    
    for i in range(len(notes) - 1):
        curr_end = notes[i][0] + notes[i][2]
        next_start = notes[i + 1][0]
        gap = next_start - curr_end
        
        if gap > max_gap_sec:
            prev_note = notes[i][1]
            next_note = notes[i + 1][1]
            base_vel = notes[i][3]

            max_echoes = min(2, int(gap / max(min_spacing, 1e-6)) - 1)
            if max_echoes <= 0:
                continue

            half_beat = beat_dur * 0.5
            for k in range(max_echoes):
                alpha = (k + 1) / (max_echoes + 1)
                echo_time = curr_end + alpha * gap
                echo_time = round(echo_time / half_beat) * half_beat
                if echo_time <= curr_end + 0.04 or echo_time >= next_start - 0.04:
                    continue

                # Move toward next pitch for natural passing motion.
                echo_note = int(round(prev_note + (next_note - prev_note) * alpha))
                echo_vel = max(24, int(base_vel * (0.60 - 0.15 * k)))
                echo_dur = min(beat_dur * 0.30, 0.18)
                filled.append((echo_time, echo_note, echo_dur, echo_vel))
                echoes_added += 1
    
    filled.sort(key=lambda x: x[0])
    if echoes_added > 0:
        print(f"  Gap-fill: +{echoes_added} echo notes added")
    return filled


# === CHART GENERATION ===
def midi_note_to_sound_index(midi_note, offset=35, clip_count=45):
    """Convert MIDI note to sound index. If out of range, transpose by octaves
    instead of clamping, so the note keeps its musical character."""
    sound_index = midi_note - offset
    # Transpose by octaves (12 semitones) until in range
    while sound_index >= clip_count:
        sound_index -= 12
    while sound_index < 0:
        sound_index += 12
    # Final safety clamp (should rarely trigger after octave transpose)
    return max(0, min(clip_count - 1, sound_index))


def sound_index_to_lane_pitch(sound_index):
    if sound_index in SMART_LOOKUP:
        return SMART_LOOKUP[sound_index]
    closest = min(SMART_LOOKUP.keys(), key=lambda k: abs(k - sound_index))
    return SMART_LOOKUP[closest]


def duration_to_game_duration(duration_seconds, tempo_bpm, max_dur_idx=2):
    """Convert duration. max_dur_idx caps the maximum duration to prevent long sustain."""
    beat_duration = 60.0 / tempo_bpm
    thirty_second = beat_duration / 8.0
    subdivisions = duration_seconds / thirty_second
    best_idx = 0
    best_diff = float('inf')
    for i, factor in enumerate(NOTE_LENGTH_FACTORS):
        diff = abs(factor - subdivisions)
        if diff < best_diff:
            best_diff = diff
            best_idx = i
    # Cap duration to prevent annoyingly long sustain sounds
    return min(best_idx, max_dur_idx)


def notes_to_chart(notes, tempo_bpm, music_id, midi_offset=35, subdivisions_per_seq=52, max_dur_idx=2):
    """Convert notes to game chart JSON format."""
    if not notes:
        return {"sequences": []}
    
    beat_duration = 60.0 / tempo_bpm
    subdivision_duration = beat_duration / 8.0
    
    total_duration = notes[-1][0] + notes[-1][2]
    total_subdivisions = int(total_duration / subdivision_duration) + 1
    num_sequences = max(1, (total_subdivisions + subdivisions_per_seq - 1) // subdivisions_per_seq)
    
    print(f"\nChart generation:")
    print(f"  Tempo: {tempo_bpm:.1f} BPM")
    print(f"  Subdivision: {subdivision_duration*1000:.1f}ms")
    print(f"  Sequences: {num_sequences}")
    
    chart = []
    for seq in range(num_sequences):
        seq_data = {}
        for lane in range(6):
            seq_data[lane] = [None] * subdivisions_per_seq
        chart.append(seq_data)
    
    placed = 0
    skipped = 0
    out_of_range = 0
    
    for start_sec, midi_note, dur_sec, velocity in notes:
        global_sub = int(round(start_sec / subdivision_duration))
        if global_sub < 0:
            global_sub = 0
        seq_idx = global_sub // subdivisions_per_seq
        local_sub = global_sub % subdivisions_per_seq
        
        if seq_idx >= num_sequences:
            skipped += 1
            continue
        
        sound_idx = midi_note_to_sound_index(midi_note, offset=midi_offset)
        if midi_note - midi_offset < 0 or midi_note - midi_offset >= 45:
            out_of_range += 1
        
        lane, pitch = sound_index_to_lane_pitch(sound_idx)
        # Placeholder duration - will be recalculated in post-processing
        game_dur = 0
        
        if chart[seq_idx][lane][local_sub] is None:
            chart[seq_idx][lane][local_sub] = (pitch, game_dur)
            placed += 1
        else:
            found = False
            for alt_lane in range(6):
                if alt_lane != lane and chart[seq_idx][alt_lane][local_sub] is None:
                    alt_pitch = sound_idx - SOUND_RESOURCE_IDXS[alt_lane][0]
                    if 0 <= alt_pitch < len(SOUND_RESOURCE_IDXS[alt_lane]):
                        chart[seq_idx][alt_lane][local_sub] = (alt_pitch, game_dur)
                        placed += 1
                        found = True
                        break
            if not found:
                skipped += 1
    
    # ========================================================
    # POST-PROCESSING: Recalculate duration based on GAP TO NEXT NOTE
    # This is the critical fix - duration controls wait time until next note,
    # NOT how long the current note sustains.
    # ========================================================
    
    # 1. Collect all occupied global positions
    occupied = []
    for seq_idx in range(num_sequences):
        for sub in range(subdivisions_per_seq):
            if any(chart[seq_idx][lane][sub] is not None for lane in range(6)):
                global_pos = seq_idx * subdivisions_per_seq + sub
                occupied.append((seq_idx, sub, global_pos))
    
    # 2. For each occupied slot, calculate gap to next and set duration
    for i in range(len(occupied)):
        seq_idx, sub, global_pos = occupied[i]
        
        if i + 1 < len(occupied):
            next_global = occupied[i + 1][2]
            gap = next_global - global_pos  # subdivisions until next note
        else:
            gap = 1  # Last note - minimal wait

        gap = compress_gap_subdivisions(gap, max_dur_idx)
        
        # Convert gap (in subdivisions) to closest NOTE_LENGTH_FACTORS index
        gap_dur = 0
        best_diff = float('inf')
        for idx, factor in enumerate(NOTE_LENGTH_FACTORS):
            diff = abs(factor - gap)
            if diff < best_diff:
                best_diff = diff
                gap_dur = idx
        
        # Cap to max duration
        gap_dur = min(gap_dur, max_dur_idx)
        
        # Update ALL notes in this time slot with the gap-based duration
        for lane in range(6):
            if chart[seq_idx][lane][sub] is not None:
                pitch, _ = chart[seq_idx][lane][sub]
                chart[seq_idx][lane][sub] = (pitch, gap_dur)
    
    print(f"  Placed: {placed} | Skipped: {skipped} | Out-of-range: {out_of_range}")
    
    # Stats
    non_empty = sum(1 for s in chart if any(s[l][sub] is not None for l in range(6) for sub in range(subdivisions_per_seq)))
    total_notes = placed
    nps = total_notes / max(0.1, total_duration)
    print(f"  Non-empty seqs: {non_empty}/{num_sequences}")
    print(f"  Average NPS: {nps:.1f} notes/sec")
    
    # Duration distribution
    dur_counts = defaultdict(int)
    for seq_idx in range(num_sequences):
        for lane in range(6):
            for sub in range(subdivisions_per_seq):
                if chart[seq_idx][lane][sub] is not None:
                    _, d = chart[seq_idx][lane][sub]
                    dur_counts[d] += 1
    print(f"  Duration distribution: {dict(sorted(dur_counts.items()))}")
    
    # Convert to JSON
    sequences = []
    for seq_idx in range(num_sequences):
        seq_obj = {"music_id": music_id, "seq": seq_idx}
        for lane in range(6):
            line_parts = []
            for sub in range(subdivisions_per_seq):
                cell = chart[seq_idx][lane][sub]
                if cell is None:
                    line_parts.append("_,_")
                else:
                    pitch, dur = cell
                    line_parts.append(f"{pitch},{dur}")
            seq_obj[f"line{lane + 1}"] = "/".join(line_parts) + "/"
        sequences.append(seq_obj)
    
    return {"sequences": sequences}


# === MAIN ===
def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    
    midi_path = sys.argv[1]
    music_id = int(sys.argv[2])
    
    # Parse options
    tempo_override = None
    output_path = None
    track_indices = None
    midi_offset = 35
    difficulty = "medium"
    enable_fill = True
    
    i = 3
    while i < len(sys.argv):
        arg = sys.argv[i]
        if arg == "--tempo" and i + 1 < len(sys.argv):
            tempo_override = float(sys.argv[i + 1]); i += 2
        elif arg == "--output" and i + 1 < len(sys.argv):
            output_path = sys.argv[i + 1]; i += 2
        elif arg == "--tracks" and i + 1 < len(sys.argv):
            track_indices = set(int(t) for t in sys.argv[i + 1].split(",")); i += 2
        elif arg == "--offset" and i + 1 < len(sys.argv):
            midi_offset = int(sys.argv[i + 1]); i += 2
        elif arg == "--difficulty" and i + 1 < len(sys.argv):
            difficulty = sys.argv[i + 1].lower(); i += 2
        elif arg == "--no-fill":
            enable_fill = False; i += 1
        else:
            i += 1
    
    if difficulty not in DIFFICULTY_PROFILES:
        print(f"Unknown difficulty: {difficulty}. Use: easy, medium, hard, expert")
        sys.exit(1)
    
    profile = DIFFICULTY_PROFILES[difficulty]
    
    if output_path is None:
        base = os.path.splitext(os.path.basename(midi_path))[0]
        output_path = os.path.join(
            os.path.dirname(os.path.abspath(__file__)),
            "Assets", "Resources", "Song_Note_Jsons", "Individual",
            f"{base}.json"
        )
    
    print(f"=== MIDI to Chart v3 ===")
    print(f"Input:      {midi_path}")
    print(f"Output:     {output_path}")
    print(f"Music ID:   {music_id}")
    print(f"Difficulty: {difficulty.upper()}")
    print(f"  Max NPS:  {profile['max_nps']}")
    print(f"  Max Sim:  {profile['max_simultaneous']}")
    print(f"  Grid:     1/{profile['quantize_grid']} note")
    print(f"  Gap-fill: {'ON' if enable_fill and profile['fill_gap'] > 0 else 'OFF'}")
    print()
    
    # 1. Parse MIDI
    mid, tempo_map, primary_bpm, tick_to_seconds = parse_midi_with_tempo_map(midi_path)
    chart_tempo = tempo_override if tempo_override else primary_bpm
    
    print(f"MIDI tempo: {primary_bpm:.1f} BPM ({len(tempo_map)} tempo changes)")
    print(f"Chart tempo: {chart_tempo:.1f} BPM")
    
    # 2. Analyze tracks
    track_info = analyze_tracks(mid, tick_to_seconds)
    
    print(f"\nTrack Analysis:")
    for t in track_info:
        if t["role"] != "empty":
            print(f"  [{t['idx']}] {t['name']:15s} | {t['role']:12s} | {len(t['notes']):4d} notes | "
                  f"avg MIDI {t['avg_pitch']:.0f} | {t['density']:.1f} n/s | "
                  f"start {t['start']:.1f}s | score {t['score']:.0f}")
    
    # 3. Select tracks
    if track_indices is None:
        track_indices = auto_select_tracks(track_info, profile["tracks"])
        print(f"\nAuto-selected tracks for {difficulty}: {sorted(track_indices)}")
    else:
        print(f"\nManual tracks: {sorted(track_indices)}")
    
    # 4. Extract notes
    notes = extract_notes(mid, tick_to_seconds, track_indices)
    print(f"Raw notes: {len(notes)}")
    
    if not notes:
        print("ERROR: No notes extracted!")
        sys.exit(1)
    
    # 5. Quantize to beat grid
    notes = quantize_notes(notes, chart_tempo, profile["quantize_grid"])
    print(f"After quantize: {len(notes)}")
    
    # 6. Apply NPS limit
    notes = apply_nps_limit(notes, profile["max_nps"], profile["max_simultaneous"], chart_tempo)
    print(f"After NPS limit ({profile['max_nps']}): {len(notes)}")
    
    # 7. Gap-fill
    if enable_fill and profile["fill_gap"] > 0:
        notes = apply_gap_fill(notes, chart_tempo, profile["fill_gap"])
        print(f"After gap-fill: {len(notes)}")
    
    # 8. Generate chart
    chart_data = notes_to_chart(notes, chart_tempo, music_id, midi_offset=midi_offset, max_dur_idx=profile.get("max_dur", 2))
    
    # 9. Write JSON
    out_dir = os.path.dirname(output_path)
    if out_dir:
        os.makedirs(out_dir, exist_ok=True)
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(chart_data, f, indent=4)
    
    print(f"\nSaved: {output_path}")
    print(f"Sequences: {len(chart_data['sequences'])}")
    print(f"Suggested tempo for songs_database.json: {int(chart_tempo)}")


if __name__ == "__main__":
    main()
