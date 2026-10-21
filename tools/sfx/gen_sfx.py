"""Synthesises the Teyvat Chronicle announcement stingers.

Run from the repo root:  python tools/sfx/gen_sfx.py
Writes GameResources/gi/sfx/stinger_<name>.wav plus the .json NML reads for volume and mode.
Needs numpy. Everything is generated from oscillators and noise, so the sounds are original.
"""
import json
import os
import wave

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "GameResources", "gi", "sfx")
RATE = 44100
RNG = np.random.default_rng(7)


def t_axis(seconds):
    return np.arange(int(seconds * RATE)) / RATE


def note(name):
    """Frequency of a note like 'A4' or 'C#3'."""
    names = {"C": -9, "C#": -8, "D": -7, "D#": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "G#": -1, "A": 0, "A#": 1, "B": 2}
    pitch, octave = name[:-1], int(name[-1])
    return 440.0 * 2 ** ((names[pitch] + (octave - 4) * 12) / 12)


def env(n, attack, release, total):
    """Attack/sustain/release envelope over n samples (times in seconds)."""
    e = np.ones(n)
    a = max(1, int(attack * RATE))
    r = max(1, int(release * RATE))
    e[:a] = np.linspace(0, 1, a)
    e[-r:] *= np.linspace(1, 0, r) ** 2
    return e


def sine(freq, seconds, phase=0.0):
    return np.sin(2 * np.pi * freq * t_axis(seconds) + phase)


def saw(freq, seconds, harmonics=12):
    t = t_axis(seconds)
    out = np.zeros_like(t)
    for k in range(1, harmonics + 1):
        out += np.sin(2 * np.pi * freq * k * t) / k
    return out * 0.6


def lowpass(signal, cutoff):
    """One-pole low-pass filter."""
    a = np.exp(-2 * np.pi * cutoff / RATE)
    out = np.empty_like(signal)
    acc = 0.0
    for i, x in enumerate(signal):
        acc = (1 - a) * x + a * acc
        out[i] = acc
    return out


def reverb(signal, seconds=1.6, mix=0.35):
    """Convolution with decaying noise: a cheap but convincing hall."""
    ir_t = t_axis(seconds)
    ir = RNG.standard_normal(len(ir_t)) * np.exp(-ir_t * 4.0 / seconds)
    ir /= np.sqrt(np.sum(ir ** 2))
    wet = np.convolve(signal, ir)
    dry = np.concatenate([signal, np.zeros(len(wet) - len(signal))])
    return dry * (1 - mix) + wet * mix


def place(buffer, sound, at):
    start = int(at * RATE)
    end = min(len(buffer), start + len(sound))
    buffer[start:end] += sound[: end - start]


def bell(freq, seconds):
    t = t_axis(seconds)
    partials = [(1, 1.0), (2.76, 0.45), (5.4, 0.25), (8.9, 0.12)]
    out = sum(a * np.sin(2 * np.pi * freq * m * t) * np.exp(-t * (2.5 + m)) for m, a in partials)
    return out


def drum(seconds=0.9, pitch=70):
    t = t_axis(seconds)
    freq = pitch * (1 + 2.5 * np.exp(-t * 18))
    body = np.sin(2 * np.pi * np.cumsum(freq) / RATE) * np.exp(-t * 5)
    hit = RNG.standard_normal(len(t)) * np.exp(-t * 40) * 0.4
    return body + lowpass(hit, 1800)


def pad(freqs, seconds, attack=0.6, release=1.2, bright=2500):
    n = int(seconds * RATE)
    out = np.zeros(n)
    for f in freqs:
        detune = 1 + RNG.uniform(-0.003, 0.003)
        out += saw(f * detune, seconds)
    return lowpass(out / len(freqs), bright) * env(n, attack, release, seconds)


# ------------------------------------------------------------------ stingers

def stinger_divine():
    """Heavenly: a bright major choir swell with a bell on top."""
    s = 4.0
    buf = pad([note("C4"), note("E4"), note("G4"), note("C5"), note("E5")], s, 0.4, 1.8, 3500)
    place(buf, bell(note("C6"), 3.0) * 0.6, 0.05)
    place(buf, bell(note("G6"), 2.5) * 0.4, 0.35)
    return reverb(buf, 2.4, 0.45)


def stinger_doom():
    """Catastrophe: a falling sub tone, distorted rumble and a tritone."""
    s = 4.5
    t = t_axis(s)
    fall = np.sin(2 * np.pi * np.cumsum(90 * np.exp(-t * 0.5)) / RATE) * env(len(t), 0.02, 1.5, s)
    rumble = np.tanh(lowpass(RNG.standard_normal(len(t)), 160) * 6) * 0.5 * env(len(t), 0.05, 2.0, s)
    tritone = pad([note("C3"), note("F#3"), note("C4")], s, 0.3, 2.0, 1200) * 0.8
    buf = fall * 0.9 + rumble + tritone
    place(buf, drum(1.2, 45) * 1.2, 0.0)
    return reverb(buf, 2.0, 0.35)


def stinger_war():
    """War: three taiko hits and a minor brass call."""
    s = 3.6
    buf = np.zeros(int(s * RATE))
    for i, at in enumerate((0.0, 0.42, 0.84)):
        place(buf, drum(1.0, 62 - i * 4) * 1.1, at)
    brass = pad([note("D3"), note("A3"), note("D4"), note("F4")], 2.4, 0.08, 1.2, 1800) * 1.1
    place(buf, brass, 1.1)
    return reverb(buf, 1.4, 0.3)


def stinger_moon():
    """Moonlight: a soft minor pad with slow tremolo and falling chimes."""
    s = 4.0
    t = t_axis(s)
    buf = pad([note("A3"), note("C4"), note("E4"), note("F4")], s, 1.0, 1.8, 1600)
    buf *= 0.75 + 0.25 * np.sin(2 * np.pi * 3.2 * t)
    for i, n in enumerate(("E6", "C6", "A5", "E5")):
        place(buf, bell(note(n), 2.2) * 0.35, 0.3 + i * 0.35)
    return reverb(buf, 2.6, 0.5)


def stinger_cosmic():
    """Primordial: a deep drone with shimmering high partials."""
    s = 4.5
    t = t_axis(s)
    drone = (sine(note("C2"), s) + 0.6 * sine(note("G2"), s) + 0.3 * sine(note("C3"), s)) * env(len(t), 1.2, 1.5, s)
    shimmer = sum(np.sin(2 * np.pi * note("C6") * m * t) * (0.5 + 0.5 * np.sin(2 * np.pi * (0.5 + m) * t))
                  for m in (1, 1.5, 2.0)) * 0.12 * env(len(t), 1.8, 1.5, s)
    return reverb(drone * 0.8 + shimmer, 2.8, 0.5)


def stinger_hope():
    """Hope: a rising major arpeggio of bells."""
    s = 3.4
    buf = np.zeros(int(s * RATE))
    for i, n in enumerate(("C5", "E5", "G5", "C6", "E6")):
        place(buf, bell(note(n), 2.2) * 0.55, i * 0.16)
    place(buf, pad([note("C4"), note("G4"), note("E5")], 2.6, 0.5, 1.4, 2200) * 0.5, 0.2)
    return reverb(buf, 2.0, 0.4)


def stinger_frost():
    """Snezhnaya: glassy crystal pings over a cold wind."""
    s = 3.6
    t = t_axis(s)
    wind = lowpass(RNG.standard_normal(len(t)), 900) * 0.35 * env(len(t), 0.8, 1.4, s)
    wind *= 0.6 + 0.4 * np.sin(2 * np.pi * 0.7 * t)
    buf = wind
    for i, n in enumerate(("B6", "F#6", "D7", "A6", "E7")):
        place(buf, bell(note(n), 1.6) * 0.3, 0.15 + i * 0.22)
    place(buf, pad([note("B3"), note("D4"), note("F#4")], 2.8, 0.6, 1.4, 1400) * 0.5, 0.3)
    return reverb(buf, 2.4, 0.5)


def stinger_warning():
    """Countdown: two low horn pulses."""
    s = 1.8
    buf = np.zeros(int(s * RATE))
    for at in (0.0, 0.7):
        place(buf, pad([note("A2"), note("E3")], 0.55, 0.03, 0.25, 900) * 1.4, at)
    return reverb(buf, 1.0, 0.25)


STINGERS = {
    "divine": stinger_divine,
    "doom": stinger_doom,
    "war": stinger_war,
    "moon": stinger_moon,
    "cosmic": stinger_cosmic,
    "hope": stinger_hope,
    "frost": stinger_frost,
    "warning": stinger_warning,
}


# ------------------------------------------------------------------ combat sounds (short, played often)

def noise(seconds):
    return RNG.standard_normal(int(seconds * RATE))


def sweep(f0, f1, seconds):
    t = t_axis(seconds)
    freq = f0 * (f1 / f0) ** (t / seconds)
    return np.sin(2 * np.pi * np.cumsum(freq) / RATE)


def bandnoise(seconds, lo, hi):
    return lowpass(noise(seconds), hi) - lowpass(noise(seconds), lo) * 0.5


def decay(n, rate):
    return np.exp(-np.arange(n) / RATE * rate)


def sfx_swing():
    s = 0.22
    sig = bandnoise(s, 400, 3500)
    return sig * np.sin(np.linspace(0, np.pi, len(sig))) ** 2


def sfx_hit():
    s = 0.18
    return (drum(s, 140) * 0.7 + lowpass(noise(s), 2500) * decay(int(s * RATE), 40) * 0.6)


def sfx_hit_heavy():
    s = 0.45
    return reverb(drum(s, 70) + lowpass(noise(s), 1500) * decay(int(s * RATE), 18) * 0.7, 0.5, 0.2)


def sfx_clang():
    s = 0.6
    t = t_axis(s)
    metal = sum(np.sin(2 * np.pi * f * t) * a for f, a in ((1840, 1), (2710, 0.6), (4120, 0.4), (5530, 0.25)))
    return reverb(metal * np.exp(-t * 9) + noise(s) * np.exp(-t * 60) * 0.5, 0.6, 0.25)


def sfx_cast():
    s = 0.5
    t = t_axis(s)
    return reverb(sweep(500, 1600, s) * np.exp(-t * 4) * 0.6 + bell(note("E6"), s) * 0.4, 0.8, 0.35)


def sfx_explode():
    s = 0.8
    t = t_axis(s)
    return reverb(np.tanh(lowpass(noise(s), 700) * 4) * np.exp(-t * 5) + drum(s, 50) * 0.8, 0.8, 0.25)


def sfx_thunder():
    s = 0.9
    t = t_axis(s)
    crack = noise(s) * np.exp(-t * 30)
    rumble = lowpass(noise(s), 250) * np.exp(-t * 3) * 3
    return reverb(crack + rumble, 1.0, 0.3)


def sfx_wind():
    s = 0.7
    sig = bandnoise(s, 300, 1600)
    t = t_axis(s)
    return sig * np.sin(np.linspace(0, np.pi, len(sig))) * (0.7 + 0.3 * np.sin(2 * np.pi * 6 * t))


def sfx_splash():
    s = 0.6
    t = t_axis(s)
    bubbles = sum(np.sin(2 * np.pi * RNG.uniform(500, 1400) * t) * np.exp(-((t - RNG.uniform(0.05, 0.4)) ** 2) / 0.0004)
                  for _ in range(10)) * 0.25
    return bandnoise(s, 800, 5000) * np.exp(-t * 7) + bubbles


def sfx_fire():
    s = 0.7
    t = t_axis(s)
    crackle = (RNG.random(len(t)) > 0.997) * RNG.uniform(-1, 1, len(t)) * 3
    return lowpass(noise(s), 900) * np.sin(np.linspace(0, np.pi, len(t))) * 1.6 + lowpass(crackle, 4000)


def sfx_ice():
    s = 0.6
    out = np.zeros(int(s * RATE))
    for i, n in enumerate(("B6", "E7", "G#6")):
        place(out, bell(note(n), 0.5) * 0.5, i * 0.05)
    place(out, noise(0.1) * decay(int(0.1 * RATE), 40) * 0.6, 0)
    return reverb(out, 0.7, 0.35)


def sfx_rock():
    s = 0.6
    t = t_axis(s)
    return drum(s, 55) + np.tanh(lowpass(noise(s), 400) * 5) * np.exp(-t * 8) * 0.7


def sfx_nature():
    s = 0.6
    t = t_axis(s)
    return reverb(sweep(300, 900, s) * np.exp(-t * 5) * 0.4 + bandnoise(s, 1500, 6000) * np.exp(-t * 9) * 0.4, 0.6, 0.3)


def sfx_charge():
    s = 0.9
    t = t_axis(s)
    rise = sweep(120, 900, s) * (t / s) ** 2
    shimmer = bandnoise(s, 2000, 8000) * (t / s) * 0.3
    return rise + shimmer


def sfx_boom():
    s = 1.4
    t = t_axis(s)
    return reverb(drum(s, 38) * 1.4 + np.tanh(lowpass(noise(s), 300) * 6) * np.exp(-t * 3), 1.4, 0.35)


def sfx_dash():
    s = 0.25
    sig = bandnoise(s, 900, 6000)
    return sig * np.linspace(1, 0, len(sig)) ** 2 + sweep(900, 300, s) * 0.2


def sfx_heal():
    s = 0.9
    out = np.zeros(int(s * RATE))
    for i, n in enumerate(("C6", "E6", "G6", "C7")):
        place(out, bell(note(n), 0.6) * 0.35, i * 0.08)
    return reverb(out, 1.0, 0.45)


def sfx_dark():
    s = 0.9
    t = t_axis(s)
    return reverb(sweep(220, 60, s) * np.exp(-t * 2.5) + lowpass(noise(s), 400) * np.exp(-t * 4) * 0.8, 1.0, 0.4)


def sfx_breath():
    s = 1.0
    t = t_axis(s)
    return np.tanh(lowpass(noise(s), 1200) * 3) * np.sin(np.linspace(0, np.pi, len(t))) * 0.8


def sfx_roar():
    s = 1.2
    t = t_axis(s)
    growl = saw(70, s) * (1 + 0.4 * np.sin(2 * np.pi * 23 * t))
    return reverb(np.tanh(lowpass(growl, 900) * 3 + lowpass(noise(s), 600)) * np.sin(np.linspace(0, np.pi, len(t))), 1.2, 0.35)


def sfx_moon():
    s = 0.9
    out = np.zeros(int(s * RATE))
    for i, n in enumerate(("A6", "E6", "C6")):
        place(out, bell(note(n), 0.7) * 0.35, i * 0.1)
    return reverb(out, 1.4, 0.5)


def sfx_space():
    s = 0.6
    t = t_axis(s)
    return reverb(sweep(1500, 200, s) * np.exp(-t * 4) + sweep(200, 1500, s) * np.exp(-t * 4) * 0.5, 0.8, 0.4)


def sfx_time():
    s = 0.8
    out = np.zeros(int(s * RATE))
    for i in range(6):
        place(out, drum(0.05, 2000) * 0.25, i * 0.12)
    place(out, sweep(800, 400, 0.8) * 0.15, 0)
    return reverb(out, 1.0, 0.5)


COMBAT = {name[4:]: fn for name, fn in globals().items() if name.startswith("sfx_")}


def write_wav(path, signal):
    peak = np.max(np.abs(signal)) or 1.0
    data = (signal / peak * 0.89 * 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, make in STINGERS.items():
        base = os.path.join(OUT, f"stinger_{name}")
        write_wav(base + ".wav", make())
        # NML sound settings: Mode 0 = Basic (not positional), Type 2 = UI channel.
        with open(base + ".json", "w") as f:
            json.dump({"Mode": 0, "Volume": 80, "Type": 2, "LoopCount": 0, "Ramp": False}, f, indent=4)
        print("wrote", base + ".wav")

    combat = os.path.join(OUT, "combat")
    os.makedirs(combat, exist_ok=True)
    for name, make in COMBAT.items():
        base = os.path.join(combat, name)
        write_wav(base + ".wav", make())
        # Mode 0 = Basic, Type 1 = sound effects channel; quieter because these play often.
        with open(base + ".json", "w") as f:
            json.dump({"Mode": 0, "Volume": 38, "Type": 1, "LoopCount": 0, "Ramp": False}, f, indent=4)
    print(f"wrote {len(COMBAT)} combat sounds")


if __name__ == "__main__":
    main()
