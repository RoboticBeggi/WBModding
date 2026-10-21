"""Synthesises every combat sound of the v0.5 Arts system.

Run from the repo root:  python tools/sfx/gen_arts_sfx.py
Writes GameResources/gi/sfx/arts/<name>.wav plus the .json NML reads for volume and mode.
Needs numpy only. Every sound is built from oscillators, noise, FFT filters and a synthetic
reverb, so all of it is original.
"""
import json
import os
import wave

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "GameResources", "gi", "sfx", "arts")
RATE = 22050
RNG = np.random.default_rng(1105)


# ------------------------------------------------------------------ building blocks

def t_axis(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def silence(seconds):
    return np.zeros(int(seconds * RATE))


def noise(seconds):
    return RNG.standard_normal(int(seconds * RATE))


def decay(seconds, rate):
    """Exponential decay envelope; rate = 1/time constant."""
    return np.exp(-t_axis(seconds) * rate)


def adsr(seconds, attack=0.005, release=0.1):
    n = int(seconds * RATE)
    e = np.ones(n)
    a = max(1, min(n, int(attack * RATE)))
    r = max(1, min(n, int(release * RATE)))
    e[:a] = np.linspace(0, 1, a)
    e[-r:] *= np.linspace(1, 0, r) ** 2
    return e


def sweep(f0, f1, seconds, curve="exp", shape="sine"):
    """Oscillator whose pitch glides from f0 to f1."""
    t = t_axis(seconds)
    k = t / max(seconds, 1e-6)
    if curve == "exp":
        f = f0 * (f1 / f0) ** k
    else:
        f = f0 + (f1 - f0) * k
    phase = 2 * np.pi * np.cumsum(f) / RATE
    if shape == "sine":
        return np.sin(phase)
    if shape == "saw":
        return 2 * ((phase / (2 * np.pi)) % 1.0) - 1
    if shape == "square":
        return np.sign(np.sin(phase))
    if shape == "tri":
        return 2 * np.abs(2 * ((phase / (2 * np.pi)) % 1.0) - 1) - 1
    raise ValueError(shape)


def tone(freq, seconds, shape="sine"):
    return sweep(freq, freq, seconds, "lin", shape)


def fft_filter(sig, lo=None, hi=None, soft=1.3):
    """Band filter in the frequency domain with smooth (log-domain) skirts."""
    n = len(sig)
    if n == 0:
        return sig
    spec = np.fft.rfft(sig)
    f = np.fft.rfftfreq(n, 1 / RATE) + 1e-3
    g = np.ones_like(f)
    if lo is not None:
        g *= 1 / (1 + (lo / f) ** (2 * soft * 2))
    if hi is not None:
        g *= 1 / (1 + (f / hi) ** (2 * soft * 2))
    return np.fft.irfft(spec * g, n)


def lp(sig, hi):
    return fft_filter(sig, hi=hi)


def hp(sig, lo):
    return fft_filter(sig, lo=lo)


def bp(sig, lo, hi):
    return fft_filter(sig, lo=lo, hi=hi)


def swept_noise(seconds, f0, f1, width=0.5, steps=24):
    """Noise through a band-pass whose centre glides from f0 to f1 (whooshes, risers)."""
    n = int(seconds * RATE)
    src = noise(seconds)
    out = np.zeros(n)
    edges = np.linspace(0, n, steps + 1).astype(int)
    win_len = max(2, edges[1] - edges[0])
    for i in range(steps):
        k = (i + 0.5) / steps
        fc = f0 * (f1 / f0) ** k
        band = bp(src, fc * (1 - width), fc * (1 + width))
        # crossfade windows
        a, b = edges[i], edges[i + 1]
        lo_i, hi_i = max(0, a - win_len // 2), min(n, b + win_len // 2)
        w = np.hanning(hi_i - lo_i)
        out[lo_i:hi_i] += band[lo_i:hi_i] * w
    return out


def partials(freqs, amps, seconds, rates):
    """Inharmonic metallic partials, each with its own decay."""
    out = np.zeros(int(seconds * RATE))
    for f, a, r in zip(freqs, amps, rates):
        out += a * tone(f, seconds) * decay(seconds, r)
    return out


def thump(f0=110, f1=38, seconds=0.45, rate=9):
    return sweep(f0, f1, seconds) * decay(seconds, rate)


def click(seconds=0.02, lo=2000):
    return hp(noise(seconds), lo) * decay(seconds, 200)


def reverb(sig, seconds=1.0, mix=0.3, bright=6000):
    ir_t = t_axis(seconds)
    ir = lp(RNG.standard_normal(len(ir_t)), bright) * np.exp(-ir_t * 5.0 / seconds)
    ir /= np.sqrt(np.sum(ir ** 2)) + 1e-9
    n = len(sig) + len(ir) - 1
    size = 1 << (n - 1).bit_length()
    wet = np.fft.irfft(np.fft.rfft(sig, size) * np.fft.rfft(ir, size), size)[:n]
    dry = np.concatenate([sig, np.zeros(n - len(sig))])
    return dry * (1 - mix) + wet * mix


def mix(length, *layers):
    """layers: (signal, start_seconds, gain)."""
    out = np.zeros(int(length * RATE))
    for sig, at, gain in layers:
        s = int(at * RATE)
        e = min(len(out), s + len(sig))
        if e > s:
            out[s:e] += sig[: e - s] * gain
    return out


def fit(sig, seconds):
    n = int(seconds * RATE)
    if len(sig) >= n:
        out = sig[:n].copy()
        r = min(n, int(0.03 * RATE))
        out[-r:] *= np.linspace(1, 0, r)
        return out
    return np.concatenate([sig, np.zeros(n - len(sig))])


def distort(sig, drive=3.0):
    return np.tanh(sig * drive) / np.tanh(drive)


def chord(freqs, seconds, shape="sine", detune=0.0):
    out = np.zeros(int(seconds * RATE))
    for f in freqs:
        out += tone(f, seconds, shape)
        if detune:
            out += tone(f * (1 + detune), seconds, shape) * 0.7
    return out / max(1, len(freqs))


def vibrato_tone(freq, seconds, depth=0.01, rate=5.5, shape="sine"):
    t = t_axis(seconds)
    f = freq * (1 + depth * np.sin(2 * np.pi * rate * t))
    phase = 2 * np.pi * np.cumsum(f) / RATE
    if shape == "saw":
        return 2 * ((phase / (2 * np.pi)) % 1.0) - 1
    return np.sin(phase)


def am(sig, rate, depth=1.0):
    t = np.arange(len(sig)) / RATE
    return sig * (1 - depth + depth * (0.5 + 0.5 * np.sin(2 * np.pi * rate * t)))


def note(name):
    names = {"C": -9, "C#": -8, "D": -7, "D#": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "G#": -1,
             "A": 0, "A#": 1, "B": 2}
    return 440.0 * 2 ** ((names[name[:-1]] + (int(name[-1]) - 4) * 12) / 12)


def crackle(seconds, density=60, lo=1500):
    """Sparse random impulses: fire crackle, static, gravel."""
    n = int(seconds * RATE)
    out = np.zeros(n)
    count = int(density * seconds)
    for p in RNG.integers(0, max(1, n - 200), count):
        L = int(RNG.integers(20, 160))
        out[p:p + L] += RNG.standard_normal(L) * np.exp(-np.arange(L) / (L / 4)) * RNG.uniform(0.3, 1)
    return hp(out, lo)


# ------------------------------------------------------------------ generic

def whoosh_light():
    s = 0.28
    w = swept_noise(s, 700, 3200, 0.45) * np.sin(np.pi * t_axis(s) / s) ** 2
    return w


def whoosh_heavy():
    s = 0.55
    env = np.sin(np.pi * t_axis(s) / s) ** 1.5
    return swept_noise(s, 180, 900, 0.5) * env + sweep(90, 50, s) * env * 0.4


def punch():
    s = 0.3
    return mix(s, (thump(150, 55, 0.25, 18), 0, 1.0), (lp(noise(0.06), 2500) * decay(0.06, 60), 0, 0.8),
               (click(0.015, 3000), 0, 0.5))


def kick():
    s = 0.35
    return mix(s, (swept_noise(0.09, 1500, 500, 0.5), 0, 0.5), (thump(200, 60, 0.28, 14), 0.06, 1.0),
               (bp(noise(0.08), 300, 2000) * decay(0.08, 45), 0.06, 0.7))


def slam():
    s = 0.9
    body = thump(90, 30, 0.8, 5)
    dirt = lp(noise(0.7), 1200) * decay(0.7, 7)
    return reverb(mix(s, (body, 0, 1.0), (dirt, 0.0, 0.6), (click(0.02, 1500), 0, 0.6)), 0.6, 0.25)


def crater_boom():
    s = 1.5
    body = thump(70, 22, 1.4, 3) * 1.1
    rumble = lp(noise(1.4), 400) * decay(1.4, 2.5)
    debris = crackle(1.2, 50, 1200) * decay(1.2, 3)
    return reverb(distort(mix(s, (body, 0, 1), (rumble, 0, 0.9), (debris, 0.08, 0.5)), 1.8), 1.2, 0.3)


def rumble():
    s = 1.3
    r = lp(noise(s), 160) * adsr(s, 0.2, 0.5)
    return am(r, 7, 0.5) + tone(42, s) * adsr(s, 0.3, 0.5) * 0.3


def land():
    s = 0.3
    return mix(s, (thump(120, 50, 0.2, 20), 0, 1), (lp(noise(0.2), 1800) * decay(0.2, 22), 0, 0.6))


def leap():
    s = 0.35
    return mix(s, (lp(noise(0.06), 1500) * decay(0.06, 50), 0, 0.6),
               (swept_noise(0.3, 400, 2400, 0.4) * np.sin(np.pi * t_axis(0.3) / 0.3), 0.03, 0.8))


def launch():
    s = 0.6
    return mix(s, (punch(), 0, 0.9), (swept_noise(0.5, 300, 4000, 0.35) * decay(0.5, 3), 0.05, 0.7),
               (sweep(200, 900, 0.4) * decay(0.4, 6) * 0.3, 0.05, 1))


def blink():
    s = 0.32
    a = sweep(3000, 300, 0.12) * decay(0.12, 20)
    b = sweep(300, 2600, 0.14) * decay(0.14, 18)
    air = hp(noise(0.3), 3000) * decay(0.3, 14)
    return mix(s, (a, 0, 0.5), (b, 0.12, 0.5), (air, 0, 0.4))


def dash():
    s = 0.3
    w = swept_noise(s, 2500, 600, 0.4) * decay(s, 7)
    return mix(s, (w, 0, 1), (lp(noise(0.05), 1500) * decay(0.05, 60), 0, 0.5))


def charge():
    s = 1.0
    t = t_axis(s)
    rise = sweep(110, 660, s, shape="saw")
    rise = lp(rise, 2500) * (t / s) ** 1.5
    shimmer = swept_noise(s, 800, 6000, 0.2) * (t / s) ** 2
    return am(rise, 14, 0.4) * 0.6 + shimmer * 0.5


def release():
    s = 0.6
    return reverb(mix(s, (sweep(900, 120, 0.4, shape="saw") * decay(0.4, 7), 0, 0.5),
                      (hp(noise(0.3), 1500) * decay(0.3, 12), 0, 0.6), (thump(130, 50, 0.3, 12), 0, 0.7)), 0.5, 0.25)


def shockwave():
    s = 0.9
    ring = sweep(400, 60, 0.7) * decay(0.7, 5)
    air = swept_noise(0.8, 3000, 200, 0.5) * decay(0.8, 4)
    return reverb(mix(s, (thump(100, 35, 0.6, 7), 0, 1), (ring, 0, 0.4), (air, 0, 0.7)), 0.8, 0.3)


def explosion_big():
    s = 1.6
    blast = distort(lp(noise(1.5), 2200) * decay(1.5, 3), 2.5)
    body = thump(80, 25, 1.5, 3)
    crack = click(0.04, 800)
    debris = crackle(1.4, 70, 1500) * decay(1.4, 2.5)
    return reverb(mix(s, (blast, 0, 0.9), (body, 0, 1), (crack, 0, 0.8), (debris, 0.1, 0.4)), 1.4, 0.35)


def explosion_small():
    s = 0.7
    return reverb(mix(s, (distort(lp(noise(0.6), 3500) * decay(0.6, 8), 2), 0, 0.9),
                      (thump(160, 50, 0.4, 10), 0, 0.8)), 0.5, 0.25)


def beam_fire():
    s = 0.9
    t = t_axis(s)
    core = chord([220, 330, 445], s, "saw", 0.006)
    core = lp(core, 3000) * adsr(s, 0.02, 0.35)
    fizz = hp(noise(s), 4000) * adsr(s, 0.01, 0.4) * 0.3
    zap = sweep(1800, 400, 0.12) * decay(0.12, 15)
    return mix(s, (am(core, 30, 0.3), 0, 0.7), (fizz, 0, 1), (zap, 0, 0.5))


def sword_clash():
    s = 0.9
    metal = partials([1850, 2470, 3320, 4410, 5230], [1, .7, .55, .4, .3], s, [6, 7, 9, 11, 14])
    return reverb(mix(s, (click(0.02, 2000), 0, 1), (metal, 0, 0.6), (hp(noise(0.08), 3000) * decay(0.08, 40), 0, 0.5)),
                  0.7, 0.25)


def lock_clash():
    s = 0.35
    metal = partials([1300, 1750, 2900], [1, .7, .5], s, [18, 20, 25])
    grind = bp(noise(s), 1500, 5000) * decay(s, 12)
    return mix(s, (metal, 0, 0.6), (grind, 0, 0.7), (click(0.015, 2500), 0, 0.7))


def slash_light():
    s = 0.25
    t = t_axis(s)
    return swept_noise(s, 2000, 7000, 0.3) * np.exp(-((t - 0.06) / 0.05) ** 2) * 1.2


def slash_heavy():
    s = 0.5
    t = t_axis(s)
    w = swept_noise(s, 600, 4500, 0.35) * np.exp(-((t - 0.1) / 0.08) ** 2)
    return mix(s, (w, 0, 1.2), (thump(140, 60, 0.3, 10), 0.08, 0.6),
               (partials([2200, 3100], [.4, .3], 0.4, [10, 12]), 0.08, 0.5))


def guard_tap():
    s = 0.25
    return mix(s, (partials([2600, 3900, 5200], [1, .6, .4], 0.25, [25, 30, 35]), 0, 0.6), (click(0.01, 3000), 0, 0.8))


def deflect():
    s = 0.45
    ping = partials([3100, 4700], [1, .5], 0.4, [9, 12])
    zing = sweep(2500, 5500, 0.3) * decay(0.3, 9)
    return mix(s, (click(0.012, 3000), 0, 0.8), (ping, 0, 0.5), (zing, 0.03, 0.35))


def glass_break():
    s = 0.9
    shards = np.zeros(int(s * RATE))
    for _ in range(16):
        f = RNG.uniform(2500, 7000)
        at = RNG.uniform(0, 0.35)
        shards = shards + mix(s, (tone(f, 0.25) * decay(0.25, RNG.uniform(15, 30)), at, RNG.uniform(.2, .5)))
    return reverb(mix(s, (hp(noise(0.3), 2500) * decay(0.3, 12), 0, 0.7), (shards, 0, 0.5)), 0.6, 0.3)


def stare():
    s = 1.3
    wind = bp(noise(s), 300, 1200) * adsr(s, 0.3, 0.5) * 0.5
    drone = lp(chord([55, 82.5], s, "saw"), 600) * adsr(s, 0.4, 0.5)
    ting = partials([2900], [1], 0.8, [4])
    return mix(s, (am(wind, 0.7, 0.6), 0, 1), (drone, 0, 0.5), (ting, 0.45, 0.3))


def team_sting():
    s = 1.2
    notes = [note("D5"), note("F#5"), note("A5"), note("D6")]
    out = np.zeros(int(s * RATE))
    for i, f in enumerate(notes):
        out = out + mix(s, (partials([f, f * 2.01, f * 3.02], [1, .4, .2], 0.8, [4, 6, 9]), i * 0.07, 0.5))
    return reverb(out + mix(s, (thump(120, 50, 0.3, 10), 0, 0.5)), 1.0, 0.35)


def last_stand():
    s = 1.5
    hb = np.zeros(int(s * RATE))
    for at in (0, 0.22, 0.75, 0.97):
        hb = hb + mix(s, (thump(70, 40, 0.18, 22), at, 1))
    roar = lp(chord([73.4, 110, 146.8], 1.0, "saw", 0.01), 1200) * adsr(1.0, 0.2, 0.4)
    return reverb(mix(s, (hb, 0, 1), (distort(roar, 2), 0.45, 0.5)), 1.0, 0.3)


def vengeance():
    s = 1.4
    stab = lp(chord([note("C3"), note("D#3"), note("F#3")], 1.2, "saw", 0.008), 2000) * decay(1.2, 2.5)
    rev = swept_noise(0.4, 300, 3000, 0.4) * np.linspace(0, 1, int(0.4 * RATE)) ** 2
    return reverb(mix(s, (rev, 0, 0.6), (stab, 0.4, 0.8), (thump(90, 35, 0.5, 6), 0.4, 0.9)), 1.2, 0.35)


def rampage():
    s = 1.2
    out = np.zeros(int(s * RATE))
    for i, at in enumerate((0, 0.14, 0.28)):
        out = out + mix(s, (thump(160 - i * 20, 50, 0.25, 12), at, 1), (lp(noise(0.1), 2000) * decay(0.1, 30), at, 0.6))
    horn = lp(chord([110, 164.8], 0.7, "saw", 0.01), 1500) * adsr(0.7, 0.05, 0.3)
    return reverb(mix(s, (out, 0, 1), (distort(horn, 2), 0.45, 0.6)), 0.9, 0.3)


def god_stop():
    s = 2.4
    swell_len = 1.6
    t = t_axis(swell_len)
    # a reversed hit: the tail swells up into a cut
    hit = reverb(mix(1.2, (thump(70, 25, 1.0, 4), 0, 1), (lp(noise(1.0), 1500) * decay(1.0, 4), 0, 0.6)), 1.4, 0.6)
    rev = hit[::-1][-int(swell_len * RATE):]
    drone = chord([36.7, 55, 73.4], swell_len, "saw", 0.004)
    drone = lp(drone, 500) * (t / swell_len) ** 2
    swell = mix(swell_len, (rev, 0, 1), (drone, 0, 0.5))
    after = reverb(partials([880, 1320, 1760], [.3, .2, .1], 0.8, [3, 4, 5]), 0.8, 0.5)
    return mix(s, (swell, 0, 1), (after, swell_len, 0.25))


def god_resume():
    s = 1.0
    t = t_axis(s)
    whoosh = swept_noise(s, 150, 2500, 0.4) * np.exp(-t * 3) * 0.8
    return reverb(mix(s, (thump(60, 120, 0.4, 6), 0, 0.6), (whoosh, 0, 1),
                      (sweep(80, 320, 0.6, shape="saw") * decay(0.6, 4) * 0.2, 0, 1)), 0.6, 0.3)


def chant():
    s = 1.4
    t = t_axis(s)
    voices = np.zeros(int(s * RATE))
    for f in (note("D3"), note("A3"), note("D4")):
        v = vibrato_tone(f, s, 0.006, 5, "saw")
        voices = voices + bp(v, 300, 1100) + bp(v, 2200, 2800) * 0.3
    return reverb(voices * adsr(s, 0.25, 0.5) * 0.5, 1.4, 0.45)


# ------------------------------------------------------------------ anemo

def wind_gust():
    s = 0.8
    t = t_axis(s)
    return swept_noise(s, 400, 1600, 0.5) * np.sin(np.pi * t / s) ** 1.3 + hp(noise(s), 4000) * np.sin(np.pi * t / s) * 0.15


def wind_howl():
    s = 1.4
    t = t_axis(s)
    howl = vibrato_tone(520, s, 0.08, 1.3) * 0.25 + vibrato_tone(780, s, 0.06, 1.7) * 0.15
    air = bp(noise(s), 500, 2500)
    return am(air, 2.2, 0.5) * adsr(s, 0.3, 0.5) * 0.7 + howl * adsr(s, 0.4, 0.5)


def feather_flutter():
    s = 0.45
    out = np.zeros(int(s * RATE))
    for i in range(9):
        out = out + mix(s, (bp(noise(0.04), 1500, 5000) * decay(0.04, 60), i * 0.045, RNG.uniform(.4, .9)))
    return out


def vortex():
    s = 1.2
    t = t_axis(s)
    swirl = bp(noise(s), 300, 1800)
    swirl = am(swirl, 6 + 6 * t / s, 0.7)
    tone_ = sweep(180, 420, s) * 0.2
    return (swirl + tone_) * adsr(s, 0.2, 0.4)


# ------------------------------------------------------------------ geo

def stone_rise():
    s = 0.8
    grind = lp(crackle(0.7, 160, 200), 3000) * adsr(0.7, 0.05, 0.3)
    return reverb(mix(s, (grind, 0, 0.8), (sweep(60, 120, 0.6) * adsr(0.6, 0.05, 0.2), 0, 0.6),
                      (thump(130, 50, 0.2, 18), 0.55, 0.9)), 0.5, 0.2)


def stone_crack():
    s = 0.6
    return reverb(mix(s, (click(0.03, 600), 0, 1), (crackle(0.4, 120, 800) * decay(0.4, 9), 0.01, 0.7),
                      (thump(180, 70, 0.2, 18), 0, 0.6)), 0.5, 0.2)


def boulder_hit():
    s = 0.9
    return reverb(mix(s, (thump(80, 35, 0.7, 6), 0, 1), (lp(crackle(0.6, 90, 300), 2500) * decay(0.6, 6), 0.02, 0.8),
                      (lp(noise(0.08), 1500) * decay(0.08, 40), 0, 0.6)), 0.6, 0.25)


def crystal_chime():
    s = 1.3
    base = note("E6")
    out = np.zeros(int(s * RATE))
    for i, m in enumerate((1, 1.5, 2.0, 2.52)):
        out = out + mix(s, (partials([base * m, base * m * 2.76], [1, .3], 1.0, [4, 8]), i * 0.06, 0.4))
    return reverb(out, 1.2, 0.4)


# ------------------------------------------------------------------ electro

def thunder_crack():
    s = 1.4
    crack = distort(hp(noise(0.06), 500) * decay(0.06, 50), 3)
    roll = am(lp(noise(1.3), 600) * decay(1.3, 2.5), 9, 0.5)
    return reverb(mix(s, (crack, 0, 1), (roll, 0.03, 0.9), (thump(70, 30, 0.6, 5), 0, 0.6)), 1.2, 0.35)


def zap():
    s = 0.3
    buzz = sweep(1600, 200, s, shape="square")
    buzz = bp(buzz, 300, 6000) * decay(s, 12)
    return mix(s, (buzz, 0, 0.5), (hp(noise(0.05), 3000) * decay(0.05, 50), 0, 0.6))


def iai_draw():
    s = 0.9
    ring = partials([3700, 5550, 7400], [1, .5, .3], 0.6, [5, 7, 10])
    slide = bp(noise(0.12), 3000, 9000) * np.linspace(0.2, 1, int(0.12 * RATE))
    hiss = swept_noise(0.35, 6000, 1500, 0.3) * decay(0.35, 6)
    return reverb(mix(s, (slide, 0, 0.6), (ring, 0.12, 0.45), (hiss, 0.4, 0.9), (click(0.01, 4000), 0.12, 0.6)), 0.6, 0.25)


def static_buzz():
    s = 0.7
    hum = bp(tone(120, s, "square") + tone(240, s, "square") * 0.5, 100, 4000)
    spit = crackle(s, 120, 2500)
    return (am(hum, 30, 0.5) * 0.3 + spit * 0.6) * adsr(s, 0.02, 0.2)


# ------------------------------------------------------------------ dendro

def vine_lash():
    s = 0.45
    t = t_axis(s)
    whip = swept_noise(0.2, 800, 5000, 0.35) * np.exp(-((t_axis(0.2) - 0.12) / 0.04) ** 2)
    snap = click(0.02, 1500)
    creak = bp(sweep(300, 180, 0.25, shape="saw"), 200, 1500) * decay(0.25, 10)
    return mix(s, (whip, 0, 1), (snap, 0.13, 0.9), (creak, 0.15, 0.3))


def leaf_rustle():
    s = 0.8
    return bp(crackle(s, 400, 1500), 1500, 7000) * adsr(s, 0.1, 0.3) * 0.8


def bloom_pop():
    s = 0.6
    pop = sweep(300, 900, 0.06) * decay(0.06, 40)
    sparkle = np.zeros(int(0.5 * RATE))
    for i, m in enumerate((1, 1.25, 1.5)):
        sparkle = sparkle + mix(0.5, (tone(note("G5") * m, 0.3) * decay(0.3, 10), i * 0.05, 0.3))
    return reverb(mix(s, (pop, 0, 0.9), (lp(noise(0.05), 2000) * decay(0.05, 50), 0, 0.5), (sparkle, 0.05, 0.6)), 0.6, 0.3)


def root_creak():
    s = 0.9
    t = t_axis(s)
    f = 140 + 40 * np.sin(2 * np.pi * 2.2 * t)
    creak = np.sign(np.sin(2 * np.pi * np.cumsum(f) / RATE))
    creak = bp(creak, 150, 1600) * adsr(s, 0.05, 0.3)
    return mix(s, (am(creak, 25, 0.7) * 0.4, 0, 1), (lp(crackle(s, 60, 300), 1500), 0, 0.5))


# ------------------------------------------------------------------ hydro

def water_whip():
    s = 0.5
    whip = swept_noise(0.22, 600, 3500, 0.4) * np.exp(-((t_axis(0.22) - 0.12) / 0.05) ** 2)
    splash = bp(noise(0.3), 400, 3000) * decay(0.3, 12)
    blub = sweep(500, 250, 0.08) * decay(0.08, 30)
    return mix(s, (whip, 0, 0.9), (splash, 0.12, 0.7), (blub, 0.13, 0.4))


def splash_big():
    s = 1.0
    body = lp(noise(0.9), 2500) * decay(0.9, 4)
    drops = np.zeros(int(0.9 * RATE))
    for _ in range(14):
        drops = drops + mix(0.9, (sweep(RNG.uniform(600, 1400), RNG.uniform(1500, 2500), 0.04) * decay(0.04, 60),
                                  RNG.uniform(0.1, 0.8), RNG.uniform(.2, .4)))
    return reverb(mix(s, (thump(110, 45, 0.3, 12), 0, 0.7), (body, 0, 0.8), (drops, 0, 0.6)), 0.6, 0.25)


def bubble_pop():
    s = 0.35
    out = np.zeros(int(s * RATE))
    for i in range(3):
        f = RNG.uniform(500, 900)
        out = out + mix(s, (sweep(f, f * 2.5, 0.05) * decay(0.05, 45), i * 0.08, 0.6))
    return out


def wave_crash():
    s = 1.5
    t = t_axis(s)
    rise = swept_noise(0.7, 200, 1200, 0.6) * np.linspace(0.2, 1, int(0.7 * RATE)) ** 2
    crash = lp(noise(0.9), 3500) * decay(0.9, 3.5)
    return reverb(mix(s, (rise, 0, 0.6), (crash, 0.6, 1.0), (thump(90, 40, 0.5, 6), 0.6, 0.6)), 0.8, 0.3)


# ------------------------------------------------------------------ pyro

def fire_roar():
    s = 1.3
    roar = lp(noise(s), 900) * adsr(s, 0.1, 0.5)
    roar = am(roar, 11, 0.35)
    return mix(s, (roar, 0, 1), (crackle(s, 90, 2000) * adsr(s, 0.1, 0.4), 0, 0.5),
               (sweep(70, 50, s) * adsr(s, 0.1, 0.5), 0, 0.3))


def flame_burst():
    s = 0.7
    burst = distort(lp(noise(0.6), 2500) * decay(0.6, 7), 2)
    return mix(s, (swept_noise(0.12, 300, 2000, 0.5) * np.linspace(0, 1, int(0.12 * RATE)), 0, 0.5),
               (burst, 0.1, 1), (crackle(0.5, 80, 2000) * decay(0.5, 5), 0.1, 0.5))


def sizzle():
    s = 0.7
    return (hp(noise(s), 5000) * 0.4 + crackle(s, 200, 3000) * 0.6) * adsr(s, 0.01, 0.4)


def phoenix_cry():
    s = 1.2
    t = t_axis(s)
    f = 900 + 500 * np.sin(np.pi * t / s) - 300 * t
    cry = np.sin(2 * np.pi * np.cumsum(f * (1 + 0.02 * np.sin(2 * np.pi * 22 * t))) / RATE)
    cry = cry + 0.4 * np.sin(2 * 2 * np.pi * np.cumsum(f) / RATE)
    return reverb(mix(s, (cry * adsr(s, 0.05, 0.4) * 0.5, 0, 1), (fire_roar()[: int(s * RATE)] * 0.4, 0, 1)), 1.0, 0.35)


# ------------------------------------------------------------------ cryo

def ice_form():
    s = 0.7
    out = np.zeros(int(s * RATE))
    for i in range(10):
        f = 1800 + i * 240
        out = out + mix(s, (tone(f, 0.15) * decay(0.15, 25), i * 0.035, 0.25))
    return reverb(mix(s, (out, 0, 1), (crackle(0.5, 150, 3000) * adsr(0.5, 0.02, 0.2), 0, 0.4)), 0.6, 0.35)


def ice_crack():
    s = 0.6
    return reverb(mix(s, (click(0.02, 1000), 0, 1), (partials([1400, 2600, 3900], [.6, .4, .3], 0.4, [14, 18, 22]), 0, 0.6),
                      (crackle(0.3, 160, 2500) * decay(0.3, 12), 0.01, 0.6)), 0.6, 0.3)


def frost_shatter():
    s = 1.0
    return mix(s, (glass_break(), 0, 0.9), (thump(130, 55, 0.3, 12), 0, 0.5), (hp(noise(0.6), 6000) * decay(0.6, 5), 0.02, 0.3))


def blizzard():
    s = 1.5
    t = t_axis(s)
    gale = am(bp(noise(s), 800, 4000), 3.3, 0.6) * adsr(s, 0.3, 0.5)
    whistle = vibrato_tone(1150, s, 0.05, 0.9) * adsr(s, 0.5, 0.5) * 0.15
    return gale * 0.8 + whistle + hp(crackle(s, 120, 5000), 5000) * 0.2


# ------------------------------------------------------------------ dark

def abyss_hum():
    s = 1.4
    drone = lp(chord([41.2, 43.6, 61.7], s, "saw"), 400) * adsr(s, 0.3, 0.5)
    rev = reverb(bp(noise(s), 200, 700) * adsr(s, 0.4, 0.5) * 0.3, 1.0, 0.5)
    return mix(s, (am(drone, 0.8, 0.4), 0, 1), (rev, 0, 1))


def dark_pulse():
    s = 0.8
    pulse = sweep(220, 40, 0.6, shape="saw") * decay(0.6, 5)
    return reverb(mix(s, (lp(pulse, 1200), 0, 0.8), (thump(90, 30, 0.5, 7), 0, 0.8),
                      (sweep(1500, 200, 0.4) * decay(0.4, 8) * 0.2, 0, 1)), 0.8, 0.4)


def bat_swarm():
    s = 1.1
    out = np.zeros(int(s * RATE))
    for _ in range(40):
        f = RNG.uniform(2500, 6000)
        out = out + mix(s, (sweep(f, f * 0.6, 0.03) * decay(0.03, 80), RNG.uniform(0, 0.95), RNG.uniform(.1, .3)))
    flaps = am(bp(noise(s), 300, 1500), 16, 0.9) * adsr(s, 0.1, 0.3)
    return out + flaps * 0.6


def whisper():
    s = 1.2
    t = t_axis(s)
    breath = bp(noise(s), 1500, 5000) + bp(noise(s), 2500, 3200) * 2
    syll = np.clip(np.sin(2 * np.pi * 4.5 * t), 0, 1) ** 2
    return reverb(breath * syll * adsr(s, 0.1, 0.3) * 0.5, 1.2, 0.5)


def gunshot():
    s = 0.7
    crack = distort(hp(noise(0.03), 800) * decay(0.03, 80), 4)
    return reverb(mix(s, (crack, 0, 1), (thump(220, 70, 0.15, 25), 0, 0.8), (lp(noise(0.4), 1500) * decay(0.4, 9), 0.01, 0.4),
                      (tone(3500, 0.06) * decay(0.06, 60) * 0.2, 0.08, 1)), 0.8, 0.3)


# ------------------------------------------------------------------ celestial

def star_chime():
    s = 1.3
    out = np.zeros(int(s * RATE))
    for i, n in enumerate(("B5", "E6", "G#6", "B6", "E7")):
        f = note(n)
        out = out + mix(s, (partials([f, f * 2.0, f * 4.07], [1, .3, .15], 0.9, [5, 7, 10]), i * 0.05, 0.3))
    return reverb(out + hp(noise(s), 7000) * decay(s, 4) * 0.08, 1.2, 0.45)


def moon_hum():
    s = 1.5
    pad = chord([note("A3"), note("E4"), note("C5")], s, "sine", 0.004)
    shimmer = am(chord([note("A5"), note("E6")], s), 3, 0.6) * 0.3
    return reverb((pad + shimmer) * adsr(s, 0.35, 0.5) * 0.7, 1.4, 0.5)


def holy_choir():
    s = 2.4
    voices = np.zeros(int(s * RATE))
    for f in (note("C4"), note("E4"), note("G4"), note("C5")):
        v = vibrato_tone(f, s, 0.005, 5.2, "saw")
        voices = voices + bp(v, 350, 900) + bp(v, 1800, 2600) * 0.4
    return reverb(voices * adsr(s, 0.5, 0.8) * 0.35, 1.8, 0.5)


def nail_fall():
    s = 1.3
    t = t_axis(0.6)
    whistle = sweep(4000, 900, 0.6) * (t / 0.6) * 0.3 + swept_noise(0.6, 5000, 1200, 0.2) * (t / 0.6) * 0.5
    clang = partials([620, 1110, 1730, 2440], [1, .7, .5, .4], 0.7, [4, 5, 7, 9])
    return reverb(mix(s, (whistle, 0, 1), (thump(90, 35, 0.5, 6), 0.6, 1), (clang, 0.6, 0.5)), 1.0, 0.35)


def decree_gong():
    s = 1.6
    gong = partials([110, 163, 228, 296, 370, 512], [1, .8, .6, .5, .35, .25], s, [1.6, 2, 2.4, 3, 3.5, 4])
    gong = gong * (1 + 0.15 * np.sin(2 * np.pi * 3.5 * t_axis(s)))
    return reverb(mix(s, (click(0.02, 400), 0, 0.5), (gong, 0, 0.8)), 1.2, 0.35)


def mask_drum():
    s = 1.1
    out = np.zeros(int(s * RATE))
    for at, g, f in ((0, 1, 95), (0.25, 0.7, 110), (0.5, 1.1, 85)):
        hit = mix(0.45, (thump(f * 1.6, f, 0.45, 7), 0, 1), (lp(noise(0.05), 1500) * decay(0.05, 50), 0, 0.5))
        out = out + mix(s, (hit, at, g))
    return reverb(out, 0.8, 0.3)


def jade_ring():
    s = 1.0
    ring = partials([1240, 1970, 3160, 4310], [1, .6, .4, .25], s, [3.5, 4.5, 6, 8])
    return reverb(mix(s, (click(0.01, 2500), 0, 0.6), (ring, 0, 0.6)), 0.9, 0.4)


# ------------------------------------------------------------------ primordial

def clock_tick():
    s = 1.0
    out = np.zeros(int(s * RATE))
    for i in range(4):
        tick = mix(0.05, (partials([2600, 4100], [1, .5], 0.05, [80, 100]), 0, 1), (click(0.01, 3000), 0, 0.5))
        tock = partials([1700, 2900], [1, .5], 0.05, [80, 100])
        out = out + mix(s, (tick if i % 2 == 0 else tock, i * 0.25, 0.7))
    return reverb(out, 0.5, 0.3)


def time_rewind():
    s = 1.2
    fwd = reverb(mix(0.9, (partials([880, 1320, 1760], [1, .6, .4], 0.6, [5, 6, 8]), 0, 0.5),
                     (thump(120, 50, 0.3, 10), 0, 0.5)), 0.6, 0.5)
    rev = fwd[::-1]
    t = t_axis(s)
    warble = sweep(1200, 200, s, shape="tri") * 0.1 * adsr(s, 0.1, 0.3)
    return mix(s, (rev, 0, 1), (warble, 0, 1))


def space_warp():
    s = 1.1
    t = t_axis(s)
    f = 200 * 2 ** (2 * np.sin(np.pi * t / s))
    warp = np.sin(2 * np.pi * np.cumsum(f) / RATE) + 0.5 * np.sin(2 * np.pi * np.cumsum(f * 1.5) / RATE)
    tear = swept_noise(s, 3000, 300, 0.4) * np.sin(np.pi * t / s)
    return reverb((warp * 0.3 + tear * 0.6) * adsr(s, 0.05, 0.3), 0.9, 0.4)


def portal_open():
    s = 1.2
    t = t_axis(s)
    spin = am(bp(noise(s), 400, 2500), 4 + 14 * t / s, 0.7)
    rise = sweep(110, 440, 0.8, shape="saw")
    rise = lp(rise, 2000) * (t_axis(0.8) / 0.8)
    pop = thump(80, 160, 0.3, 8)
    return reverb(mix(s, (spin * adsr(s, 0.1, 0.3), 0, 0.6), (rise, 0, 0.35), (pop, 0.8, 0.7)), 0.9, 0.4)


def heartbeat():
    s = 1.2
    out = np.zeros(int(s * RATE))
    for at in (0, 0.2, 0.7, 0.9):
        out = out + mix(s, (lp(thump(60, 35, 0.2, 20), 300), at, 1 if at in (0, 0.7) else 0.7))
    return out


def bell_toll():
    s = 1.6
    f = 196
    bell = partials([f * 0.5, f, f * 1.19, f * 1.5, f * 2, f * 2.5, f * 3.0], [.6, 1, .5, .6, .4, .3, .2], s,
                    [1.2, 1.8, 2.2, 2.6, 3.2, 4, 5])
    return reverb(mix(s, (click(0.015, 800), 0, 0.4), (bell, 0, 0.8)), 1.4, 0.4)


def soul_wail():
    s = 1.5
    t = t_axis(s)
    f = 420 + 260 * np.sin(np.pi * t / s) ** 2 - 120 * t
    wail = np.sin(2 * np.pi * np.cumsum(f * (1 + 0.015 * np.sin(2 * np.pi * 6 * t))) / RATE)
    wail2 = np.sin(2 * np.pi * np.cumsum(f * 1.33) / RATE) * 0.5
    air = bp(noise(s), 800, 3000) * 0.3
    return reverb((wail + wail2 + air) * adsr(s, 0.2, 0.5) * 0.45, 1.5, 0.55)


# ------------------------------------------------------------------ dragon

def dragon_roar():
    s = 1.6
    t = t_axis(s)
    f = 90 + 40 * np.sin(np.pi * t / s)
    growl = np.sign(np.sin(2 * np.pi * np.cumsum(f * (1 + 0.04 * RNG.standard_normal(len(t)).cumsum() / len(t))) / RATE))
    growl = lp(growl, 1800) + bp(noise(s), 200, 1500) * 0.8
    growl = am(growl, 23, 0.4)
    return reverb(distort(growl * adsr(s, 0.1, 0.6), 2.5) * 0.7, 1.2, 0.35)


def wing_flap():
    s = 0.8
    out = np.zeros(int(s * RATE))
    for at in (0, 0.38):
        f = lp(noise(0.35), 600) * np.sin(np.pi * t_axis(0.35) / 0.35) ** 2
        out = out + mix(s, (f, at, 1), (thump(70, 40, 0.2, 12), at + 0.15, 0.5))
    return out


def dragon_breath():
    s = 1.5
    t = t_axis(s)
    inhale = swept_noise(0.35, 300, 1200, 0.5) * np.linspace(0, 1, int(0.35 * RATE)) ** 2
    blast = lp(noise(1.1), 1600) * adsr(1.1, 0.03, 0.5)
    blast = am(blast, 9, 0.3)
    return reverb(mix(s, (inhale, 0, 0.5), (distort(blast, 1.8), 0.35, 1.0), (crackle(1.0, 80, 2000), 0.4, 0.4),
                      (sweep(60, 45, 1.1) * adsr(1.1, 0.05, 0.5), 0.35, 0.4)), 0.8, 0.3)


# ------------------------------------------------------------------ output

SOUNDS = {
    # generic
    "whoosh_light": whoosh_light, "whoosh_heavy": whoosh_heavy, "punch": punch, "kick": kick, "slam": slam,
    "crater_boom": crater_boom, "rumble": rumble, "land": land, "leap": leap, "launch": launch, "blink": blink,
    "dash": dash, "charge": charge, "release": release, "shockwave": shockwave, "explosion_big": explosion_big,
    "explosion_small": explosion_small, "beam_fire": beam_fire, "sword_clash": sword_clash, "lock_clash": lock_clash,
    "slash_light": slash_light, "slash_heavy": slash_heavy, "guard_tap": guard_tap, "deflect": deflect,
    "glass_break": glass_break, "stare": stare, "team_sting": team_sting, "last_stand": last_stand,
    "vengeance": vengeance, "rampage": rampage, "god_stop": god_stop, "god_resume": god_resume, "chant": chant,
    # anemo
    "wind_gust": wind_gust, "wind_howl": wind_howl, "feather_flutter": feather_flutter, "vortex": vortex,
    # geo
    "stone_rise": stone_rise, "stone_crack": stone_crack, "boulder_hit": boulder_hit, "crystal_chime": crystal_chime,
    # electro
    "thunder_crack": thunder_crack, "zap": zap, "iai_draw": iai_draw, "static_buzz": static_buzz,
    # dendro
    "vine_lash": vine_lash, "leaf_rustle": leaf_rustle, "bloom_pop": bloom_pop, "root_creak": root_creak,
    # hydro
    "water_whip": water_whip, "splash_big": splash_big, "bubble_pop": bubble_pop, "wave_crash": wave_crash,
    # pyro
    "fire_roar": fire_roar, "flame_burst": flame_burst, "sizzle": sizzle, "phoenix_cry": phoenix_cry,
    # cryo
    "ice_form": ice_form, "ice_crack": ice_crack, "frost_shatter": frost_shatter, "blizzard": blizzard,
    # dark
    "abyss_hum": abyss_hum, "dark_pulse": dark_pulse, "bat_swarm": bat_swarm, "whisper": whisper, "gunshot": gunshot,
    # celestial
    "star_chime": star_chime, "moon_hum": moon_hum, "holy_choir": holy_choir, "nail_fall": nail_fall,
    "decree_gong": decree_gong, "mask_drum": mask_drum, "jade_ring": jade_ring,
    # primordial
    "clock_tick": clock_tick, "time_rewind": time_rewind, "space_warp": space_warp, "portal_open": portal_open,
    "heartbeat": heartbeat, "bell_toll": bell_toll, "soul_wail": soul_wail,
    # dragon
    "dragon_roar": dragon_roar, "wing_flap": wing_flap, "dragon_breath": dragon_breath,
}

LONG = {"god_stop": 2.5, "holy_choir": 2.5}

# Quieter for sounds that repeat constantly in big battles.
VOLUME = {"whoosh_light": 30, "guard_tap": 28, "deflect": 32, "punch": 34, "kick": 34, "slash_light": 30,
          "dash": 30, "land": 30, "clock_tick": 34, "lock_clash": 32, "god_stop": 55, "god_resume": 45,
          "crater_boom": 46, "explosion_big": 46, "team_sting": 44, "last_stand": 44, "vengeance": 44,
          "rampage": 44, "holy_choir": 42, "dragon_roar": 48}


def finish(sig, name):
    sig = np.asarray(sig, dtype=np.float64)
    sig = sig - np.mean(sig)
    limit = LONG.get(name, 1.6)
    n = int(limit * RATE)
    if len(sig) > n:
        sig = fit(sig, limit)
    # trim trailing silence
    thresh = np.max(np.abs(sig)) * 0.002
    idx = np.nonzero(np.abs(sig) > thresh)[0]
    if len(idx):
        sig = sig[: min(len(sig), idx[-1] + int(0.02 * RATE))]
    if len(sig) < int(0.15 * RATE):
        sig = np.concatenate([sig, np.zeros(int(0.15 * RATE) - len(sig))])
    fade = min(len(sig), int(0.01 * RATE))
    sig[-fade:] *= np.linspace(1, 0, fade)
    sig[:8] *= np.linspace(0, 1, 8)
    peak = np.max(np.abs(sig)) + 1e-9
    return sig / peak * 0.89


def write(name, sig):
    data = (sig * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    meta = {"Mode": 0, "Volume": VOLUME.get(name, 38), "Type": 1, "LoopCount": 0, "Ramp": False}
    with open(os.path.join(OUT, name + ".json"), "w") as f:
        json.dump(meta, f, indent=4)


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, make in SOUNDS.items():
        write(name, finish(make(), name))
    print(f"{len(SOUNDS)} sounds -> {OUT}")


if __name__ == "__main__":
    main()
