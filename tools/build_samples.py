"""Turn a folder of single-note WAV recordings into a game sample set.

usage: python3 build_samples.py <input_dir> <output_dir> <prefix> [--vibrato] [--max-sec 4.5] [--fmin 25]
Needs ffmpeg and numpy. Writes <prefix>_<Note>.bytes (16-bit mono 48 kHz) + samples.txt.
Pitch is MEASURED from the audio (file names are only used for display).
"""
import sys, os, glob, subprocess, numpy as np
sr = 48000
args = sys.argv[1:]
src, dst, prefix = args[0], args[1], args[2]
vibrato = "--vibrato" in args
max_sec = float(args[args.index("--max-sec") + 1]) if "--max-sec" in args else 4.5
fmin = float(args[args.index("--fmin") + 1]) if "--fmin" in args else 25.0
names = ["C", "C#", "D", "Eb", "E", "F", "F#", "G", "Ab", "A", "Bb", "B"]
os.makedirs(dst, exist_ok=True)

def load(path):
    raw = subprocess.run(["ffmpeg", "-loglevel", "error", "-i", path, "-ac", "1", "-ar", str(sr), "-f", "f32le", "-"],
                         capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype="<f4").astype(np.float64)

def yin(seg, fmin, fmax=2500):
    n = len(seg); maxlag = int(sr / fmin); minlag = max(2, int(sr / fmax))
    d = np.array([np.sum((seg[:n - maxlag] - seg[l:n - maxlag + l]) ** 2) for l in range(maxlag + 1)])
    cm = d.copy(); cm[1:] = d[1:] * np.arange(1, len(d)) / np.maximum(np.cumsum(d[1:]), 1e-12); cm[0] = 1
    idx = np.where(cm[minlag:] < 0.2)[0]
    l = (idx[0] if len(idx) else np.argmin(cm[minlag:])) + minlag
    while l + 1 < len(cm) and cm[l + 1] < cm[l]: l += 1
    a, b, c = cm[l - 1], cm[l], cm[l + 1]; l = l + 0.5 * (a - c) / (a - 2 * b + c + 1e-12)
    return sr / l

lines = ["# file rootMidi rootHz loopStart loopEnd   (mono 16-bit PCM, 48000 Hz, little-endian)"]
if vibrato: lines.append("@recordedVibrato")
results = []
for path in sorted(glob.glob(os.path.join(src, "*.wav"))):
    x = load(path)
    hop = 480
    env = np.array([np.sqrt(np.mean(x[i:i + hop] ** 2)) for i in range(0, len(x) - hop, hop)])
    mx = env.max()
    on = max(0, int(np.argmax(env > 0.05 * mx)) - 1) * hop
    x = x[on:on + int(max_sec * sr)]
    env = np.array([np.sqrt(np.mean(x[i:i + 2400] ** 2)) for i in range(0, len(x) - 2400, 2400)])
    # pitch: median over several windows of the sustain (robust to vibrato)
    win = int(max(4800, 6 * sr / fmin))
    pts = [int(len(x) * f) for f in (0.3, 0.4, 0.5, 0.6)]
    hz0 = float(np.median([yin(x[p:p + win], fmin) for p in pts if p + win < len(x)]))   # octave-safe estimate
    # refine: autocorrelation over 1 s of sustain averages out vibrato
    s0 = int(len(x) * 0.3); n = min(sr, len(x) - s0 - int(sr / hz0 * 1.3) - 2)
    seg = x[s0:s0 + n + int(sr / hz0 * 1.3) + 2]
    lo, hi = int(sr / (hz0 * 1.06)), int(sr / (hz0 / 1.06)) + 1
    def C(l): return float(np.dot(seg[:n], seg[l:l + n]))
    cs = [C(l) for l in range(lo, hi + 1)]; k = int(np.argmax(cs)); bl = lo + k
    a_, b_, c_ = C(bl - 1), C(bl), C(bl + 1)
    hz = sr / (bl + 0.5 * (a_ - c_) / (a_ - 2 * b_ + c_))
    midi_f = 69 + 12 * np.log2(hz / 440); midi = int(round(midi_f))
    # steady loop region (low envelope variation, loud enough), at least 0.35 s
    best = None
    for a in range(3, len(env)):
        for b in range(a + 7, len(env) - 1):
            w = env[a:b + 1]
            if w.std() / w.mean() < 0.08 and w.mean() > 0.55 * env.max():
                if best is None or b - a > best[1] - best[0]: best = (a, b)
    if best is None: best = (int(len(env) * 0.3), int(len(env) * 0.7))
    ls = best[0] * 2400 + 1200; target = best[1] * 2400
    period = sr / hz; bc = None
    for k in range(-int(period * 1.5) - 1, int(period * 1.5) + 1):
        le = target + k
        if le + 256 >= len(x): continue
        c = np.sum((x[ls - 128:ls + 256] - x[le - 128:le + 256]) ** 2)
        if bc is None or c < bc[0]: bc = (c, le)
    le = bc[1]
    # Final tuning reference = average pitch from the attack through the loop (what the listener hears).
    seg = x[int(0.12 * sr):le + int(sr / hz * 1.3) + 2]; n = len(seg) - int(sr / hz * 1.3) - 2
    lo, hi = int(sr / (hz * 1.06)), int(sr / (hz / 1.06)) + 1
    def C2(l): return float(np.dot(seg[:n], seg[l:l + n]))
    bl = max(range(lo + 1, hi), key=C2)
    a_, b_, c_ = C2(bl - 1), C2(bl), C2(bl + 1)
    hz = sr / (bl + 0.5 * (a_ - c_) / (a_ - 2 * b_ + c_))
    midi_f = 69 + 12 * np.log2(hz / 440); midi = int(round(midi_f))
    rms = np.sqrt(np.mean(x[ls:le] ** 2))
    x = x * min(0.22 / rms, 0.95 / np.abs(x).max())
    f = int(0.003 * sr); x[:f] *= np.linspace(0, 1, f); f = int(0.08 * sr); x[-f:] *= np.linspace(1, 0, f)
    label = names[midi % 12] + str(midi // 12 - 1)
    fn = f"{prefix}_{label}"
    (np.clip(x, -1, 1) * 32767).astype("<i2").tofile(os.path.join(dst, fn + ".bytes"))
    lines.append(f"{fn} {midi} {hz:.3f} {ls} {le}")
    results.append((midi, f"{os.path.basename(path):10s} -> {label:4s} {hz:8.2f} Hz ({(midi_f - midi) * 100:+5.1f} c)  len {len(x) / sr:.2f}s  loop {(le - ls) / sr:.2f}s"))
for _, r in sorted(results): print(r)
open(os.path.join(dst, "samples.txt"), "w").write("\n".join(lines) + "\n")
