"""
MIDI to TilesWorld Chart JSON Converter v2
Converts standard MIDI files to the game's custom JSON chart format.

Fixes in v2:
- Proper variable tempo support from MIDI file
- Correct MIDI-to-sound-index offset (auto-detect or manual)
- pitch >= 0 allowed (game bug was fixed)
- Track selection support (--tracks)
- Better duration mapping using NOTE_LENGTH_FACTORS
- Auto-detect tempo from MIDI

Usage: 
  py midi_to_chart.py <midi_file> <music_id> [options]

Options:
  --tempo <bpm>        Override tempo (default: auto-detect from MIDI)
  --output <path>      Output JSON path
  --tracks <1,2,3>     Select specific tracks (default: all)
  --offset <n>         MIDI note offset (default: 35, meaning piano_snd000 = MIDI 35)
"""
import json
import sys
import os

try:
    import mido
except ImportError:
    print("Installing mido...")
    os.system("pip install mido")
    import mido


# === GAME'S SOUND_RESOURCE_IDXS MAPPING ===
SOUND_RESOURCE_IDXS = [
    [24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44],  # Lane 0
    [19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39],  # Lane 1
    [15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35],  # Lane 2
    [10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30],  # Lane 3
    [5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25],      # Lane 4
    [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21],          # Lane 5
]

# Game's NOTE_LENGTH_FACTORS (from GameNoteCreator.cs)
# Index is the duration value in the chart, value is the timing factor
NOTE_LENGTH_FACTORS = [1, 2, 4, 8, 16, 32, 3, 6, 12, 24, 48, 7, 14, 28, 56]


def build_reverse_lookup():
    """Build sound_index -> (lane, pitch) mapping, preferring central lanes."""
    lookup = {}
    # Process lanes from outer to inner so inner lanes override (preferred)
    # Higher notes -> lane 0-1, middle -> lane 2-3, low -> lane 4-5
    lane_priority = [0, 5, 1, 4, 2, 3]
    for lane in lane_priority:
        for pitch, sound_idx in enumerate(SOUND_RESOURCE_IDXS[lane]):
            # pitch >= 0 is now valid (game bug was fixed)
            lookup[sound_idx] = (lane, pitch)
    return lookup

REVERSE_LOOKUP = build_reverse_lookup()


def build_smart_reverse_lookup():
    """Build sound_index -> (lane, pitch) with register-aware lane assignment.
    High notes -> lane 0-1, Middle -> lane 2-3, Low -> lane 4-5"""
    lookup = {}
    for sound_idx in range(45):
        if sound_idx >= 30:
            # High register -> prefer lane 0, then 1
            for lane in [0, 1, 2]:
                if sound_idx in SOUND_RESOURCE_IDXS[lane]:
                    pitch = SOUND_RESOURCE_IDXS[lane].index(sound_idx)
                    lookup[sound_idx] = (lane, pitch)
                    break
        elif sound_idx >= 15:
            # Middle register -> prefer lane 2, then 3
            for lane in [2, 3, 1, 4]:
                if sound_idx in SOUND_RESOURCE_IDXS[lane]:
                    pitch = SOUND_RESOURCE_IDXS[lane].index(sound_idx)
                    lookup[sound_idx] = (lane, pitch)
                    break
        else:
            # Low register -> prefer lane 4, then 5
            for lane in [4, 5, 3]:
                if sound_idx in SOUND_RESOURCE_IDXS[lane]:
                    pitch = SOUND_RESOURCE_IDXS[lane].index(sound_idx)
                    lookup[sound_idx] = (lane, pitch)
                    break
    return lookup

SMART_LOOKUP = build_smart_reverse_lookup()


def midi_note_to_sound_index(midi_note, offset=35, clip_count=45):
    """Convert MIDI note number to game sound index."""
    sound_index = midi_note - offset
    return max(0, min(clip_count - 1, sound_index))


def sound_index_to_lane_pitch(sound_index):
    """Convert sound index to (lane, pitch) using smart register-aware lookup."""
    if sound_index in SMART_LOOKUP:
        return SMART_LOOKUP[sound_index]
    closest = min(SMART_LOOKUP.keys(), key=lambda k: abs(k - sound_index))
    return SMART_LOOKUP[closest]


def duration_to_game_duration(duration_seconds, tempo_bpm):
    """
    Convert note duration to game duration index.
    Maps to NOTE_LENGTH_FACTORS indices based on how many 32nd notes the duration spans.
    """
    beat_duration = 60.0 / tempo_bpm
    thirty_second = beat_duration / 8.0
    
    # How many 32nd notes does this duration span?
    subdivisions = duration_seconds / thirty_second
    
    # Find the closest NOTE_LENGTH_FACTORS value
    best_idx = 0
    best_diff = float('inf')
    for i, factor in enumerate(NOTE_LENGTH_FACTORS):
        diff = abs(factor - subdivisions)
        if diff < best_diff:
            best_diff = diff
            best_idx = i
    
    return best_idx


def parse_midi_with_tempo_map(midi_path):
    """Parse MIDI file with proper tempo map support (handles tempo changes)."""
    mid = mido.MidiFile(midi_path)
    
    # Build tempo map from track 0
    tempo_map = []  # [(tick, tempo_microseconds)]
    for track in mid.tracks:
        tick = 0
        for msg in track:
            tick += msg.time
            if msg.type == 'set_tempo':
                tempo_map.append((tick, msg.tempo))
    
    if not tempo_map:
        tempo_map = [(0, 500000)]  # Default 120 BPM
    
    tempo_map.sort()
    
    # Get primary tempo (first one)
    primary_bpm = mido.tempo2bpm(tempo_map[0][1])
    
    print(f"Tempo map: {len(tempo_map)} tempo changes")
    for tick, tempo in tempo_map[:5]:
        print(f"  tick {tick}: {mido.tempo2bpm(tempo):.1f} BPM")
    print(f"Primary tempo: {primary_bpm:.1f} BPM")
    print(f"Tracks: {len(mid.tracks)}")
    
    # Helper: convert tick to seconds using tempo map
    def tick_to_seconds(target_tick):
        current_tick = 0
        current_tempo = tempo_map[0][1] if tempo_map else 500000
        total_seconds = 0.0
        tempo_idx = 0
        
        while current_tick < target_tick:
            # Find next tempo change
            next_change_tick = target_tick
            if tempo_idx + 1 < len(tempo_map):
                next_change_tick = min(target_tick, tempo_map[tempo_idx + 1][0])
            
            # Calculate time for this segment
            delta_ticks = next_change_tick - current_tick
            total_seconds += mido.tick2second(delta_ticks, mid.ticks_per_beat, current_tempo)
            current_tick = next_change_tick
            
            # Update tempo if we hit a change
            if tempo_idx + 1 < len(tempo_map) and current_tick >= tempo_map[tempo_idx + 1][0]:
                tempo_idx += 1
                current_tempo = tempo_map[tempo_idx][1]
        
        return total_seconds
    
    return mid, tempo_map, primary_bpm, tick_to_seconds


def extract_notes(mid, tick_to_seconds, track_indices=None):
    """Extract notes from selected tracks."""
    notes = []
    
    for track_idx, track in enumerate(mid.tracks):
        if track_indices and track_idx not in track_indices:
            continue
        
        current_time = 0
        active_notes = {}
        
        for msg in track:
            current_time += msg.time
            
            if msg.type == 'note_on' and msg.velocity > 0:
                active_notes[msg.note] = (current_time, msg.velocity)
            elif msg.type == 'note_off' or (msg.type == 'note_on' and msg.velocity == 0):
                if msg.note in active_notes:
                    start_tick, velocity = active_notes.pop(msg.note)
                    start_sec = tick_to_seconds(start_tick)
                    end_sec = tick_to_seconds(current_time)
                    dur_sec = end_sec - start_sec
                    notes.append((start_sec, msg.note, dur_sec, velocity))
    
    notes.sort(key=lambda x: x[0])
    
    print(f"Total notes extracted: {len(notes)}")
    if notes:
        print(f"Duration: {notes[-1][0] + notes[-1][2]:.1f} seconds")
        midi_range = [n[1] for n in notes]
        names = ['C','C#','D','D#','E','F','F#','G','G#','A','A#','B']
        lo, hi = min(midi_range), max(midi_range)
        print(f"MIDI range: {lo} ({names[lo%12]}{lo//12-1}) - {hi} ({names[hi%12]}{hi//12-1})")
    
    return notes


def notes_to_chart(notes, tempo_bpm, music_id, midi_offset=35, subdivisions_per_seq=52):
    """Convert parsed notes to game chart JSON format."""
    if not notes:
        return {"sequences": []}
    
    beat_duration = 60.0 / tempo_bpm
    subdivision_duration = beat_duration / 8.0  # 32nd note
    
    total_duration = notes[-1][0] + notes[-1][2]
    total_subdivisions = int(total_duration / subdivision_duration) + 1
    num_sequences = max(1, (total_subdivisions + subdivisions_per_seq - 1) // subdivisions_per_seq)
    
    print(f"\nChart generation:")
    print(f"  Tempo for grid: {tempo_bpm:.1f} BPM")
    print(f"  Subdivision: {subdivision_duration*1000:.1f}ms (32nd note)")
    print(f"  Total subdivisions: {total_subdivisions}")
    print(f"  Sequences: {num_sequences}")
    print(f"  MIDI offset: {midi_offset}")
    
    # Initialize chart grid
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
        global_sub = int(start_sec / subdivision_duration)
        seq_idx = global_sub // subdivisions_per_seq
        local_sub = global_sub % subdivisions_per_seq
        
        if seq_idx >= num_sequences:
            skipped += 1
            continue
        
        sound_idx = midi_note_to_sound_index(midi_note, offset=midi_offset)
        
        # Check if note was clamped (out of range)
        if midi_note - midi_offset < 0 or midi_note - midi_offset >= 45:
            out_of_range += 1
        
        lane, pitch = sound_index_to_lane_pitch(sound_idx)
        game_dur = duration_to_game_duration(dur_sec, tempo_bpm)
        
        # Place note if slot is empty
        if chart[seq_idx][lane][local_sub] is None:
            chart[seq_idx][lane][local_sub] = (pitch, game_dur)
            placed += 1
        else:
            # Try adjacent lanes
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
    
    print(f"  Notes placed: {placed}")
    print(f"  Notes skipped (collision): {skipped}")
    print(f"  Notes out of range (clamped): {out_of_range}")
    
    # Convert grid to JSON
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
    
    i = 3
    while i < len(sys.argv):
        if sys.argv[i] == "--tempo" and i + 1 < len(sys.argv):
            tempo_override = float(sys.argv[i + 1])
            i += 2
        elif sys.argv[i] == "--output" and i + 1 < len(sys.argv):
            output_path = sys.argv[i + 1]
            i += 2
        elif sys.argv[i] == "--tracks" and i + 1 < len(sys.argv):
            track_indices = set(int(t) for t in sys.argv[i + 1].split(","))
            i += 2
        elif sys.argv[i] == "--offset" and i + 1 < len(sys.argv):
            midi_offset = int(sys.argv[i + 1])
            i += 2
        else:
            i += 1
    
    if output_path is None:
        base = os.path.splitext(os.path.basename(midi_path))[0]
        output_path = os.path.join(
            os.path.dirname(os.path.abspath(__file__)),
            "Assets", "Resources", "Song_Note_Jsons", "Individual",
            f"{base}.json"
        )
    
    print(f"Input: {midi_path}")
    print(f"Output: {output_path}")
    print(f"Music ID: {music_id}")
    if track_indices:
        print(f"Selected tracks: {sorted(track_indices)}")
    print()
    
    # Parse MIDI with tempo map
    mid, tempo_map, primary_bpm, tick_to_seconds = parse_midi_with_tempo_map(midi_path)
    
    # Use override tempo or MIDI tempo
    chart_tempo = tempo_override if tempo_override else primary_bpm
    
    # List tracks
    print("\nAvailable tracks:")
    for idx, track in enumerate(mid.tracks):
        note_count = sum(1 for m in track if m.type == 'note_on' and m.velocity > 0)
        print(f"  [{idx}] {track.name!r:20s} - {note_count} notes")
    print()
    
    # Extract notes
    notes = extract_notes(mid, tick_to_seconds, track_indices)
    
    # Convert to chart
    chart_data = notes_to_chart(notes, chart_tempo, music_id, midi_offset=midi_offset)
    
    # Write JSON
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(chart_data, f, indent=4)
    
    print(f"\nChart saved to: {output_path}")
    print(f"Sequences: {len(chart_data['sequences'])}")
    print(f"\nSuggested songs_database.json entry:")
    print(f'  "tempo": {int(chart_tempo)},')


if __name__ == "__main__":
    main()
