"""
MIDI to TilesWorld Chart JSON Converter
Converts standard MIDI files to the game's custom JSON chart format.

Usage: python midi_to_chart.py <midi_file> <music_id> <tempo_bpm> [--output <output.json>]
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
# [lane][pitch] -> sound_index
SOUND_RESOURCE_IDXS = [
    # Lane 0: Piano high octave (24-44)
    [24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44],
    # Lane 1: Piano mid-high (19-39)
    [19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35,36,37,38,39],
    # Lane 2: Piano mid (15-35)
    [15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32,33,34,35],
    # Lane 3: Piano mid-low (10-30)
    [10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30],
    # Lane 4: Piano low (5-25)
    [5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25],
    # Lane 5: Piano lowest (1-21)
    [1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21],
]

# Build reverse lookup: sound_index -> best (lane, pitch)
# Prefer middle lanes (2-3) for melody, outer lanes for extremes
def build_reverse_lookup():
    """Build sound_index -> (lane, pitch) mapping, preferring central lanes."""
    lookup = {}
    # Process lanes from outer to inner so inner lanes override (preferred)
    lane_priority = [0, 5, 1, 4, 2, 3]  # 3 and 2 have highest priority
    for lane in lane_priority:
        for pitch, sound_idx in enumerate(SOUND_RESOURCE_IDXS[lane]):
            if pitch > 0:  # pitch 0 is filtered out by GameNoteCreator (pitch > 0 check)
                lookup[sound_idx] = (lane, pitch)
    return lookup

REVERSE_LOOKUP = build_reverse_lookup()


def midi_note_to_sound_index(midi_note, clip_count=45):
    """
    Convert MIDI note number to game sound index.
    Assumes piano clips are chromatic starting from a low note.
    Sound indices range 1-44 in the game.
    
    Common piano sample sets start at C1 (MIDI 24) or C2 (MIDI 36).
    We'll try to map so middle C (MIDI 60) falls in the mid range (~index 20-25).
    """
    # Offset: MIDI 36 (C2) -> sound_index 1
    # This puts MIDI 60 (C4) -> sound_index 25, which is Lane 2 pitch 10 or Lane 3 pitch 15
    sound_index = midi_note - 35
    
    # Clamp to valid range
    sound_index = max(1, min(clip_count - 1, sound_index))
    return sound_index


def sound_index_to_lane_pitch(sound_index):
    """Convert sound index to (lane, pitch) using reverse lookup."""
    if sound_index in REVERSE_LOOKUP:
        return REVERSE_LOOKUP[sound_index]
    
    # Fallback: find closest match
    closest = min(REVERSE_LOOKUP.keys(), key=lambda k: abs(k - sound_index))
    return REVERSE_LOOKUP[closest]


def duration_to_game_duration(duration_seconds, tempo_bpm):
    """
    Convert note duration in seconds to game duration value.
    Game duration values roughly map to musical note lengths:
    1 = very short (32nd note)
    2 = short (16th note)  
    3 = quarter note
    7 = half note
    8 = whole note
    """
    beat_duration = 60.0 / tempo_bpm  # seconds per beat
    beats = duration_seconds / beat_duration
    
    if beats <= 0.125:
        return 1  # 32nd note
    elif beats <= 0.25:
        return 2  # 16th note
    elif beats <= 0.5:
        return 3  # 8th note
    elif beats <= 1.0:
        return 3  # quarter note
    elif beats <= 1.5:
        return 5  # dotted quarter
    elif beats <= 2.0:
        return 7  # half note
    elif beats <= 3.0:
        return 7  # dotted half
    else:
        return 8  # whole note or longer


def parse_midi(midi_path, tempo_bpm=None):
    """Parse MIDI file and extract note events with timing."""
    mid = mido.MidiFile(midi_path)
    
    # Get tempo from MIDI if not provided
    if tempo_bpm is None:
        for track in mid.tracks:
            for msg in track:
                if msg.type == 'set_tempo':
                    tempo_bpm = mido.tempo2bpm(msg.tempo)
                    break
            if tempo_bpm:
                break
    if tempo_bpm is None:
        tempo_bpm = 120  # default
    
    print(f"Tempo: {tempo_bpm} BPM")
    print(f"Tracks: {len(mid.tracks)}")
    
    # Collect all note events from all tracks
    notes = []  # (start_time_seconds, midi_note, duration_seconds, velocity)
    
    for track_idx, track in enumerate(mid.tracks):
        current_time = 0  # in ticks
        active_notes = {}  # midi_note -> (start_tick, velocity)
        
        for msg in track:
            current_time += msg.time
            
            if msg.type == 'note_on' and msg.velocity > 0:
                active_notes[msg.note] = (current_time, msg.velocity)
                
            elif msg.type == 'note_off' or (msg.type == 'note_on' and msg.velocity == 0):
                if msg.note in active_notes:
                    start_tick, velocity = active_notes.pop(msg.note)
                    duration_ticks = current_time - start_tick
                    
                    # Convert ticks to seconds
                    start_sec = mido.tick2second(start_tick, mid.ticks_per_beat, mido.bpm2tempo(tempo_bpm))
                    dur_sec = mido.tick2second(duration_ticks, mid.ticks_per_beat, mido.bpm2tempo(tempo_bpm))
                    
                    notes.append((start_sec, msg.note, dur_sec, velocity))
    
    # Sort by start time
    notes.sort(key=lambda x: x[0])
    
    print(f"Total notes: {len(notes)}")
    if notes:
        print(f"Duration: {notes[-1][0] + notes[-1][2]:.1f} seconds")
        midi_range = [n[1] for n in notes]
        print(f"MIDI note range: {min(midi_range)} - {max(midi_range)}")
    
    return notes, tempo_bpm


def notes_to_chart(notes, tempo_bpm, music_id, subdivisions_per_seq=52):
    """Convert parsed notes to game chart JSON format."""
    
    if not notes:
        return {"sequences": []}
    
    # Calculate timing
    # Base timing: 32nd note duration in seconds
    beat_duration = 60.0 / tempo_bpm
    subdivision_duration = beat_duration / 8.0  # 32nd note
    
    # Total song duration
    total_duration = notes[-1][0] + notes[-1][2]
    total_subdivisions = int(total_duration / subdivision_duration) + 1
    
    # Calculate number of sequences needed
    num_sequences = max(1, (total_subdivisions + subdivisions_per_seq - 1) // subdivisions_per_seq)
    
    print(f"\nChart generation:")
    print(f"  Subdivision duration: {subdivision_duration*1000:.1f}ms")
    print(f"  Total subdivisions: {total_subdivisions}")
    print(f"  Sequences: {num_sequences}")
    
    # Initialize chart grid: [sequence][lane][subdivision] = (pitch, duration) or None
    chart = []
    for seq in range(num_sequences):
        seq_data = {}
        for lane in range(6):
            seq_data[lane] = [None] * subdivisions_per_seq
        chart.append(seq_data)
    
    # Place notes on the grid
    placed = 0
    skipped = 0
    
    for start_sec, midi_note, dur_sec, velocity in notes:
        # Which subdivision does this note fall on?
        global_sub = int(start_sec / subdivision_duration)
        
        # Which sequence and local subdivision?
        seq_idx = global_sub // subdivisions_per_seq
        local_sub = global_sub % subdivisions_per_seq
        
        if seq_idx >= num_sequences:
            skipped += 1
            continue
        
        # Convert MIDI note to game format
        sound_idx = midi_note_to_sound_index(midi_note)
        lane, pitch = sound_index_to_lane_pitch(sound_idx)
        game_dur = duration_to_game_duration(dur_sec, tempo_bpm)
        
        # Place note if slot is empty
        if chart[seq_idx][lane][local_sub] is None:
            chart[seq_idx][lane][local_sub] = (pitch, game_dur)
            placed += 1
        else:
            # Slot taken, try adjacent lanes
            found = False
            for alt_lane in range(6):
                if alt_lane != lane and chart[seq_idx][alt_lane][local_sub] is None:
                    # Find a valid pitch for this sound index on the alt lane
                    alt_pitch = sound_idx - SOUND_RESOURCE_IDXS[alt_lane][0]
                    if 1 <= alt_pitch < len(SOUND_RESOURCE_IDXS[alt_lane]):
                        chart[seq_idx][alt_lane][local_sub] = (alt_pitch, game_dur)
                        placed += 1
                        found = True
                        break
            if not found:
                skipped += 1
    
    print(f"  Notes placed: {placed}")
    print(f"  Notes skipped: {skipped}")
    
    # Convert grid to JSON format
    sequences = []
    for seq_idx in range(num_sequences):
        seq_obj = {
            "music_id": music_id,
            "seq": seq_idx,
        }
        
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
    if len(sys.argv) < 4:
        print("Usage: python midi_to_chart.py <midi_file> <music_id> <tempo_bpm> [--output <output.json>]")
        print("\nExample:")
        print("  python midi_to_chart.py ode_to_joy.mid 23 120")
        print("  python midi_to_chart.py nocturne.mid 24 69 --output nocturne_chopin.json")
        sys.exit(1)
    
    midi_path = sys.argv[1]
    music_id = int(sys.argv[2])
    tempo_bpm = float(sys.argv[3])
    
    # Output path
    output_path = None
    if "--output" in sys.argv:
        idx = sys.argv.index("--output")
        if idx + 1 < len(sys.argv):
            output_path = sys.argv[idx + 1]
    
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
    print()
    
    # Parse MIDI
    notes, actual_tempo = parse_midi(midi_path, tempo_bpm)
    
    # Convert to chart
    chart_data = notes_to_chart(notes, tempo_bpm, music_id)
    
    # Write JSON
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    with open(output_path, 'w', encoding='utf-8') as f:
        json.dump(chart_data, f, indent=4)
    
    print(f"\nChart saved to: {output_path}")
    print(f"Sequences: {len(chart_data['sequences'])}")


if __name__ == "__main__":
    main()
