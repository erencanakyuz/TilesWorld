import csv

# Read log
data = []
with open("note_debug_log.csv", encoding="utf-8-sig") as f:
    reader = csv.DictReader(f)
    for row in reader:
        s = row.get("Status") or ""
        if "PLAYED" in s or "DROPPED" in s:
            data.append(row)

# Timeline - show EVERY event including drops
print(f"=== FULL TIMELINE ({len(data)} events) ===")
print(f"{'#':>4} {'Time':>8} {'Status':>8} {'Lane':>4} {'InP':>4} {'FP':>4} {'Vol':>6} {'Clip':>16}")
for d in data[:50]:
    idx = d.get("Index", "?")
    gt = d.get("GameTime", "?")
    st = d.get("Status", "?").strip()
    lane = d.get("InputLine", "?")
    ip = d.get("InputPitch", "?")
    fp = d.get("FinalPitch", "?")
    vol = d.get("Volume", "?")
    clip = d.get("ClipName", "?")
    
    if "DROPPED" in st:
        status = "DROP"
        clip = st
    else:
        status = "PLAY"
    
    print(f"{idx:>4} {gt:>8} {status:>8} {lane:>4} {ip:>4} {fp:>4} {vol:>6} {clip}")

# Count gap analysis
played = [d for d in data if "PLAYED" in (d.get("Status") or "")]
if len(played) > 1:
    times = [float(d["GameTime"]) for d in played]
    gaps = [times[i] - times[i-1] for i in range(1, len(times))]
    long_gaps = [(i, g) for i, g in enumerate(gaps) if g > 0.5]
    print(f"\n=== LONG GAPS (>0.5s) ===")
    print(f"Total: {len(long_gaps)} out of {len(gaps)} gaps")
    for i, g in long_gaps[:20]:
        print(f"  Note {i+1}->{i+2}: {g:.2f}s gap (t={times[i]:.1f}s)")
