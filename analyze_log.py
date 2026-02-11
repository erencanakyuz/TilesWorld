import csv
from collections import Counter, defaultdict

data = []
with open("note_debug_log.csv", encoding="utf-8-sig") as f:
    reader = csv.DictReader(f)
    for row in reader:
        status = row.get("Status") or ""
        if "PLAYED" in status or "DROPPED" in status:
            data.append(row)

played = [d for d in data if "PLAYED" in (d.get("Status") or "")]
dropped = [d for d in data if "DROPPED" in (d.get("Status") or "")]
dup_dropped = [d for d in dropped if "Duplicate" in (d.get("Status") or "")]
other_dropped = [d for d in dropped if "Duplicate" not in (d.get("Status") or "")]

print("=" * 65)
print("  TURK MARCH V2 - KAPSAMLI ANALIZ RAPORU")
print("=" * 65)

# 1. Genel istatistik
print(f"\n[1] GENEL ISTATISTIK")
print(f"  Toplam event: {len(data)}")
print(f"  Played: {len(played)}")
print(f"  Dropped (duplicate): {len(dup_dropped)}")
print(f"  Dropped (diger): {len(other_dropped)}")
if played:
    times = [float(d["GameTime"]) for d in played]
    dur = times[-1] - times[0]
    print(f"  Sure: {dur:.1f}s ({dur/60:.1f} dakika)")
    print(f"  NPS (nota/sn): {len(played)/dur:.1f}")

# 2. Duplicate prevention sonuclari
print(f"\n[2] DUPLICATE PREVENTION")
print(f"  Engellenen duplicate: {len(dup_dropped)}")
if played:
    print(f"  Oran: {len(dup_dropped)*100//(len(played)+len(dup_dropped))}%")
if dup_dropped:
    for d in dup_dropped[:5]:
        print(f"    {d.get('Status','').strip()}")

# 3. Volume analizi
print(f"\n[3] VOLUME DAGILIMI (scaling etkisi)")
volumes = [float(d["Volume"]) for d in played if d.get("Volume")]
if volumes:
    vol_buckets = Counter()
    for v in volumes:
        if v >= 0.25: vol_buckets["0.25-0.30"] = vol_buckets.get("0.25-0.30", 0) + (1 if v < 0.30 else 0)
        if 0.20 <= v < 0.25: vol_buckets["0.20-0.25"] += 1
        if 0.15 <= v < 0.20: vol_buckets["0.15-0.20"] += 1
        if 0.10 <= v < 0.15: vol_buckets["0.10-0.15"] += 1
        if v < 0.10: vol_buckets["<0.10"] += 1
    print(f"  Min vol: {min(volumes):.3f}")
    print(f"  Max vol: {max(volumes):.3f}")
    print(f"  Avg vol: {sum(volumes)/len(volumes):.3f}")
    # Count how many were scaled down
    max_vol = max(volumes)
    scaled = sum(1 for v in volumes if v < max_vol * 0.95)
    print(f"  Scaled down: {scaled}/{len(played)} ({scaled*100//len(played)}%)")

# 4. Esanli nota analizi
print(f"\n[4] ESANLI NOTA ANALIZI")
time_groups = defaultdict(list)
for d in played:
    time_groups[d["DspTime"]].append(d)
simul = {k: v for k, v in time_groups.items() if len(v) > 1}
simul_counts = Counter(len(v) for v in simul.values())
print(f"  Toplam zaman dilimi: {len(time_groups)}")
print(f"  2+ nota ayni anda: {len(simul)} ({len(simul)*100//max(1,len(time_groups))}%)")
for cnt in sorted(simul_counts.keys()):
    print(f"    {cnt} nota birden: {simul_counts[cnt]} kez")

# Ayni clip ayni anda
dup_clips_simul = 0
for k, v in simul.items():
    clips = [d["ClipName"] for d in v]
    if len(clips) != len(set(clips)):
        dup_clips_simul += 1
print(f"  Ayni clip ayni anda (faz cakismasi): {dup_clips_simul}")

# 5. Timing gaps
print(f"\n[5] TIMING ARALIKLARI")
if played:
    times = sorted(float(d["GameTime"]) for d in played)
    gaps = [times[i]-times[i-1] for i in range(1, len(times))]
    brackets = {
        "<20ms (anlIk)": sum(1 for g in gaps if g < 0.02),
        "20-50ms (cok hIzlI)": sum(1 for g in gaps if 0.02 <= g < 0.05),
        "50-100ms (hIzlI)": sum(1 for g in gaps if 0.05 <= g < 0.1),
        "100-200ms (normal)": sum(1 for g in gaps if 0.1 <= g < 0.2),
        "200-400ms (rahat)": sum(1 for g in gaps if 0.2 <= g < 0.4),
        "400ms+ (yavas)": sum(1 for g in gaps if g >= 0.4),
    }
    for label, cnt in brackets.items():
        pct = cnt * 100 // len(gaps)
        bar = "#" * (pct // 2)
        print(f"  {label:25s} {cnt:4d} ({pct:2d}%) {bar}")

# 6. Clip dagilimi
print(f"\n[6] EN COK CALANAN CLIPLER (top 15)")
clips = Counter(d["ClipName"] for d in played)
for clip, cnt in clips.most_common(15):
    pct = cnt * 100 // len(played)
    bar = "#" * (pct // 2)
    print(f"  {clip:16s} {cnt:4d} ({pct:2d}%) {bar}")

# 7. Lane dagilimi
print(f"\n[7] LANE DAGILIMI")
lanes = Counter(d["InputLine"] for d in played)
for lane in sorted(lanes.keys()):
    cnt = lanes[lane]
    pct = cnt * 100 // len(played)
    bar = "#" * (pct)
    print(f"  Lane {lane}: {cnt:4d} ({pct:2d}%) {bar}")

# 8. Pitch dagilimi histogramI
print(f"\n[8] FINALPITCH HISTOGRAM (pitch cesitliligi)")
fp = Counter(int(d["FinalPitch"]) for d in played)
# Group by ranges
ranges = {"0-9 (bas)": 0, "10-19": 0, "20-29 (orta)": 0, "30-39": 0, "40-44 (tiz)": 0}
for p, c in fp.items():
    if p < 10: ranges["0-9 (bas)"] += c
    elif p < 20: ranges["10-19"] += c
    elif p < 30: ranges["20-29 (orta)"] += c
    elif p < 40: ranges["30-39"] += c
    else: ranges["40-44 (tiz)"] += c
for label, cnt in ranges.items():
    pct = cnt * 100 // len(played)
    bar = "#" * (pct // 2)
    print(f"  {label:18s} {cnt:4d} ({pct:2d}%) {bar}")
unique_pitches = len(fp)
print(f"  Benzersiz pitch sayisi: {unique_pitches}/45")

# 9. Patlama bolgesi tespiti (en yogun anlar)
print(f"\n[9] EN YOGUN ANLAR (1sn pencere)")
if played:
    times_all = [float(d["GameTime"]) for d in played]
    window = 1.0
    max_density = 0
    max_t = 0
    for i, t in enumerate(times_all):
        cnt = sum(1 for t2 in times_all if t <= t2 < t + window)
        if cnt > max_density:
            max_density = cnt
            max_t = t
    print(f"  En yogun 1sn: t={max_t:.1f}s - {max_density} nota")
    # top 5 densest moments
    densities = []
    step = 0.5
    t = times_all[0]
    while t < times_all[-1]:
        cnt = sum(1 for t2 in times_all if t <= t2 < t + window)
        densities.append((t, cnt))
        t += step
    densities.sort(key=lambda x: -x[1])
    for t, cnt in densities[:5]:
        print(f"    t={t:.1f}s: {cnt} nota/sn")

# 10. Sonuc ve oneriler
print(f"\n{'='*65}")
print(f"  SONUC VE ONERILER")
print(f"{'='*65}")
if dup_clips_simul == 0:
    print(f"  [OK] Duplicate clip cakismasi: YOK (daha once 21, simdi 0)")
else:
    print(f"  [!!] Hala {dup_clips_simul} duplicate clip cakismasi var!")

if played:
    avg_vol = sum(volumes) / len(volumes)
    if avg_vol < 0.15:
        print(f"  [!!] Ortalama volume cok dusuk: {avg_vol:.3f}")
    else:
        print(f"  [OK] Volume dengeli: avg={avg_vol:.3f}")

if len(dup_dropped) > 0:
    print(f"  [OK] {len(dup_dropped)} duplicate engellendi (temiz ses)")

fast_notes = sum(1 for g in gaps if g < 0.02) if played else 0
if fast_notes > len(played) * 0.3:
    print(f"  [!!] Cok fazla anlik nota: {fast_notes} (<20ms aralarla)")
else:
    print(f"  [OK] Timing dagilimi saglikli")

print(f"  [INFO] Benzersiz pitch: {unique_pitches}/45 clip kullaniliyor")
