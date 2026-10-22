"""Generates the bespoke VFX for every combat style (the "Arts" system), plus the particle and glow sprites.

Run from the repo root:  python tools/art/gen_arts.py
Writes GameResources/gi/arts/<set>/<set>_NN.png. Needs Pillow and numpy.

Unlike the old tintable shapes, most sets here are drawn in their own colours with 3–5 tone
palettes, so wind feathers, rock pillars, lightning, vines, water, flame, ice, bats, runes, rifts,
clock faces and celestial nails each look like what they are. Sets drawn in white/grey are tinted at
runtime (marked "tint").
"""
import math
import os

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "GameResources", "gi", "arts")
RNG = np.random.default_rng(23)
TAU = 2 * math.pi


# ------------------------------------------------------------------ helpers

def grid(w, h):
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    return x + 0.5 - w / 2, y + 0.5 - h / 2


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def palette(stops):
    return [hexrgb(s) for s in stops]


def shade_to_rgb(shade, pal):
    """Maps shade 0..1 to a stepped palette (pixel-art look: no smooth gradients)."""
    idx = np.clip((shade * len(pal)).astype(int), 0, len(pal) - 1)
    out = np.zeros(shade.shape + (3,), np.float32)
    for i, c in enumerate(pal):
        out[idx == i] = c
    return out


def img(alpha, shade=None, pal=None):
    """alpha 0..1; shade 0..1 picks a palette colour (or white levels if no palette)."""
    a = np.round(np.clip(alpha, 0, 1) * 4) / 4
    if shade is None:
        shade = np.ones_like(alpha)
    if pal is None:
        v = 150 + 105 * np.round(np.clip(shade, 0, 1) * 3) / 3
        rgb = np.stack([v, v, v], -1)
    else:
        rgb = shade_to_rgb(np.clip(shade, 0, 0.999), pal)
    rgba = np.concatenate([rgb, (a * 255)[..., None]], -1).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def save(name, frames):
    folder = os.path.join(OUT, name)
    os.makedirs(folder, exist_ok=True)
    for f in os.listdir(folder):
        if f.endswith(".png"):
            os.remove(os.path.join(folder, f))
    for i, im in enumerate(frames):
        im.save(os.path.join(folder, f"{name}_{i:02d}.png"))
    return len(frames)


def ring(r, radius, width):
    return np.clip(1 - np.abs(r - radius) / max(width, 0.4), 0, 1)


def disc(x, y, cx, cy, r):
    return np.clip(r + 0.5 - np.hypot(x - cx, y - cy), 0, 1)


def seg_dist(x, y, x0, y0, x1, y1):
    vx, vy = x1 - x0, y1 - y0
    L = max(1e-6, vx * vx + vy * vy)
    t = np.clip(((x - x0) * vx + (y - y0) * vy) / L, 0, 1)
    return np.hypot(x - (x0 + t * vx), y - (y0 + t * vy))


def line(x, y, pts, width):
    d = np.full(x.shape, 1e9, np.float32)
    for (x0, y0), (x1, y1) in zip(pts[:-1], pts[1:]):
        d = np.minimum(d, seg_dist(x, y, x0, y0, x1, y1))
    return np.clip(width + 0.5 - d, 0, 1)


def poly_mask(x, y, pts):
    """Even-odd polygon fill."""
    inside = np.zeros(x.shape, bool)
    n = len(pts)
    for i in range(n):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % n]
        cond = ((y0 > y) != (y1 > y)) & (x < (x1 - x0) * (y - y0) / (y1 - y0 + 1e-9) + x0)
        inside ^= cond
    return inside.astype(np.float32)


def noise(w, h, seed):
    r = np.random.default_rng(seed)
    return r.random((h, w)).astype(np.float32)


# ------------------------------------------------------------------ palettes

P = {
    "wind": palette(["#1E7E6E", "#46C9A8", "#9CF2D6", "#F0FFF9"]),
    "rock": palette(["#4A3218", "#7A5428", "#B88A44", "#E8C77A"]),
    "jade": palette(["#1F5E3E", "#3E9E6A", "#8BE0A8", "#F4E8A0"]),
    "volt": palette(["#3A1670", "#7A3CE0", "#C79BFF", "#FFFFFF"]),
    "leaf": palette(["#1C4A18", "#3E8E28", "#8ACD46", "#E0F7A0"]),
    "bloom": palette(["#7A2E5E", "#E06AA8", "#FFB6D9", "#FFF0A0"]),
    "water": palette(["#0E3A78", "#2672C4", "#62B6F2", "#E4F6FF"]),
    "fire": palette(["#5A0E08", "#C2321A", "#FF7A22", "#FFD25A", "#FFF6C8"]),
    "ice": palette(["#2E5A7A", "#6AA6CC", "#B4E6F6", "#FFFFFF"]),
    "blood": palette(["#2A0608", "#7A1018", "#D02A38", "#FF8A8A"]),
    "abyss": palette(["#120A22", "#3A1A66", "#7A3CC8", "#D8A8FF"]),
    "gold": palette(["#6A4A12", "#B8862A", "#F2CC5E", "#FFF6CC"]),
    "star": palette(["#1A2A5A", "#4A6ACE", "#A8C4FF", "#FFFFFF"]),
    "lunar": palette(["#2A2050", "#6A5AB0", "#C9B8FF", "#FFFFFF"]),
    "dust": palette(["#5A4A3A", "#8A7A62", "#B8A88E", "#DCD0BC"]),
    "life": palette(["#1E5A2E", "#4EB860", "#B6FF9A", "#FFFFE0"]),
    "death": palette(["#141018", "#3A3248", "#7A6A9A", "#D8D0E8"]),
    "time": palette(["#1A4A44", "#3A9A88", "#9AF0DC", "#FFFFFF"]),
    "space": palette(["#200A3A", "#5A2A9A", "#B07CFF", "#F0E0FF"]),
    "light": palette(["#B89A50", "#F2DA90", "#FFF6D6", "#FFFFFF"]),
    "spark": palette(["#C26A10", "#FFB43A", "#FFF09A", "#FFFFFF"]),
}


# ------------------------------------------------------------------ generic

def impact(size=48, n=7):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    spikes = (np.cos(ang * 6) * 0.5 + 0.5) ** 5 + 0.25 * (np.cos(ang * 13 + 1) * 0.5 + 0.5) ** 8
    out = []
    for i in range(n):
        t = i / (n - 1)
        reach = size * (0.16 + 0.34 * t ** 0.7)
        star = np.clip(1 - r / (reach * (0.35 + 0.65 * spikes)), 0, 1) * (1 - t ** 1.5)
        rg = ring(r, reach * 1.05, 1.6 * (1 - t) + 0.6) * (1 - t)
        core = np.clip(1 - r / (size * 0.12 * (1 - t) + 0.01), 0, 1)
        a = np.maximum.reduce([star, rg, core])
        out.append(img(a, np.maximum(core, star * 0.7)))
    return out


def shock(size=64, n=8):
    x, y = grid(size, size)
    r = np.hypot(x, y / 0.62)          # flattened: a ground shockwave in perspective
    out = []
    dots = RNG.uniform(0, TAU, 18)
    for i in range(n):
        t = i / (n - 1)
        R = size * (0.06 + 0.44 * t)
        a = ring(r, R, 2.2 * (1 - t) + 0.6) * (1 - t * 0.7)
        a = np.maximum(a, ring(r, R * 0.7, 1.0) * (1 - t) * 0.6)
        for th in dots:
            px, py = math.cos(th) * R * 1.08, math.sin(th) * R * 0.62 * 1.08
            a = np.maximum(a, disc(x, y, px, py, 0.8) * (1 - t))
        out.append(img(a, a))
    return out


def dust(size=48, n=8):
    x, y = grid(size, size)
    nz = noise(size, size, 3)
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((size, size), np.float32)
        for k in range(7):
            th = k / 7 * TAU + 0.3
            cx, cy = math.cos(th) * size * 0.28 * t, math.sin(th) * size * 0.14 * t + size * 0.1 - t * 6
            a = np.maximum(a, disc(x, y, cx, cy, size * (0.1 + 0.12 * t)))
        a *= np.clip((nz - t * 0.85) * 3, 0, 1) * (1 - t * 0.4)
        out.append(img(a, 1 - (y / size + 0.5) * 0.7, P["dust"]))
    return out


def debris(size=40, n=8):
    x, y = grid(size, size)
    chunks = [(RNG.uniform(-1, 1), RNG.uniform(-1.6, -0.6), RNG.uniform(1.0, 2.2)) for _ in range(9)]
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((size, size), np.float32)
        sh = np.zeros_like(a)
        for vx, vy, s in chunks:
            px = vx * size * 0.45 * t
            py = vy * size * 0.45 * t + size * 0.55 * t * t
            m = (np.abs(x - px) < s) & (np.abs(y - py) < s * 0.8)
            a = np.maximum(a, m * (1 - max(0, t - 0.8) * 5))
            sh = np.maximum(sh, m * (x - px > 0) * 0.6 + m * 0.3)
        out.append(img(a, sh, P["rock"]))
    return out


def fissure():
    w, h = 64, 14
    x, y = grid(w, h)
    pts = [(-w / 2, 0)]
    for k in range(1, 9):
        pts.append((-w / 2 + k * w / 8, RNG.uniform(-3, 3)))
    a = line(x, y, pts, 1.1)
    for k in range(2, 8, 2):
        px, py = pts[k]
        a = np.maximum(a, line(x, y, [(px, py), (px + RNG.uniform(-4, 4), py + RNG.choice([-5, 5]))], 0.5))
    glow = line(x, y, pts, 2.6) * 0.5
    return [img(np.maximum(a, glow), a * 0.0 + (a > 0.5) * 0.1 + glow * 0.4, palette(["#140C06", "#3A2414", "#FF7A22"]))]


def lock_sparks(size=32, n=6):
    x, y = grid(size, size)
    out = []
    parts = [(RNG.uniform(-1, 1), RNG.uniform(-1.8, -0.6)) for _ in range(16)]
    for i in range(n):
        t = (i + 1) / n
        a = np.zeros((size, size), np.float32)
        for vx, vy in parts:
            px, py = vx * size * 0.4 * t, vy * size * 0.4 * t + size * 0.5 * t * t
            a = np.maximum(a, line(x, y, [(px, py), (px - vx * 2, py - vy * 2)], 0.45) * (1 - t * 0.6))
        a = np.maximum(a, disc(x, y, 0, 0, 3 * (1 - t)))
        out.append(img(a, a, P["spark"]))
    return out


# ------------------------------------------------------------------ Anemo

def feather():
    w, h = 20, 10
    x, y = grid(w, h)
    out = []
    for i in range(4):
        wob = math.sin(i / 4 * TAU) * 0.6
        body = poly_mask(x, y, [(-9, wob), (-2, -3.5), (8, -1 + wob * 0.5), (9, 0), (8, 1), (-2, 3.0)])
        rib = line(x, y, [(-8, wob * 0.5), (8, 0)], 0.4)
        out.append(img(body, 0.55 + 0.45 * rib + (y < 0) * 0.15, P["wind"]))
    return out


def wind_spout(w=28, h=56, n=8):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        ph = i / n * TAU
        a = np.zeros((h, w), np.float32)
        for k in range(9):
            yy = h * 0.44 - k * h * 0.11
            rx = w * (0.1 + 0.042 * k)
            e = np.hypot((x - math.sin(ph + k * 0.7) * 1.4) / rx, (y - yy) / (rx * 0.3))
            a = np.maximum(a, ring(e, 1, 0.25) * (0.55 + 0.45 * math.cos(ph + k)))
        streak = np.clip(1 - np.abs(x - np.sin(y * 0.3 + ph) * 3) / 1.0, 0, 1) * 0.6
        a = np.maximum(a, streak * (np.abs(y) < h * 0.42))
        out.append(img(a, a, P["wind"]))
    return out


def wind_ring(size=40, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        e = np.hypot(x / (size * (0.2 + 0.25 * t)), y / (size * (0.06 + 0.08 * t)))
        a = ring(e, 1, 0.18) * (1 - t * 0.6)
        ang = np.arctan2(y, x)
        a *= 0.5 + 0.5 * (np.sin(ang * 5 + t * 6) > 0)
        out.append(img(a, 0.6 + 0.4 * (y < 0), P["wind"]))
    return out


def vacuum(size=44, n=10):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        t = i / (n - 1)
        if t < 0.75:
            k = t / 0.75
            orb = disc(x, y, 0, 0, size * (0.22 - 0.12 * k))
            streaks = (np.cos(ang * 10 + k * 8) > 0.85) * np.clip(1 - np.abs(r - size * (0.45 - 0.25 * k)) / 4, 0, 1)
            a = np.maximum(orb * 0.8, streaks)
            sh = orb * 0.5 + streaks
        else:
            k = (t - 0.75) / 0.25
            a = ring(r, size * (0.1 + 0.38 * k), 2.5) * (1 - k * 0.5)
            sh = a
        out.append(img(a, sh, P["wind"]))
    return out


def cyclone(w=56, h=72, n=8):
    x, y = grid(w, h)
    out = []
    rocks = RNG.uniform(0, TAU, 8)
    for i in range(n):
        ph = i / n * TAU
        a = np.zeros((h, w), np.float32)
        for k in range(12):
            yy = h * 0.45 - k * h * 0.08
            rx = w * (0.08 + 0.034 * k)
            e = np.hypot((x - math.sin(ph * 2 + k * 0.5) * 2) / rx, (y - yy) / (rx * 0.28))
            a = np.maximum(a, ring(e, 1, 0.22) * (0.5 + 0.5 * math.cos(ph + k * 0.8)))
        for j, th in enumerate(rocks):
            k = (j * 1.3 + ph) % TAU
            px, py = math.cos(k) * w * (0.15 + 0.03 * j), h * 0.3 - j * h * 0.08
            a = np.maximum(a, disc(x, y, px, py, 1.2) * (math.sin(k) > -0.2))
        out.append(img(a, a, P["wind"]))
    return out


# ------------------------------------------------------------------ Geo

def rock_pillar(w=26, h=56, n=7):
    x, y = grid(w, h)
    out = []
    nz = noise(w, h, 5)
    for i in range(n):
        t = i / (n - 1)
        rise = min(1, t * 2.4)
        crumble = max(0, (t - 0.65) / 0.35)
        top = h / 2 - h * 0.92 * rise
        body = (np.abs(x) < w * 0.32 - (y - top) * 0.0) & (y > top) & (y < h / 2)
        tip = poly_mask(x, y, [(-w * 0.32, top + 6), (0, top - 2), (w * 0.32, top + 6)])
        a = np.maximum(body.astype(np.float32), tip)
        a *= np.clip((nz - crumble) * 4, 0, 1)
        cracks = (np.abs(np.sin(y * 0.6 + x * 0.9)) < 0.12) * 0.0
        sh = 0.35 + 0.45 * (x > 0) + 0.2 * (np.sin(y * 0.8) > 0.6) - cracks
        out.append(img(a, sh, P["rock"]))
    return out


def stone_fist(size=36, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        off = -size * 0.3 + size * 0.45 * min(1, t * 2)
        fist = disc(x, y, off, 0, size * 0.22)
        knuckles = np.maximum.reduce([disc(x, y, off + size * 0.18, k * 4 - 6, 3) for k in range(4)])
        arm = (np.abs(y) < size * 0.12) & (x < off) & (x > -size * 0.5)
        a = np.maximum.reduce([fist, knuckles, arm.astype(np.float32)]) * (1 - max(0, t - 0.75) * 4)
        sh = 0.4 + 0.5 * (y < -2) + 0.1 * knuckles
        out.append(img(a, sh, P["rock"]))
    return out


def geo_meteor(size=26, n=4):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        rock = disc(x, y, 3, 3, size * 0.24)
        trail = np.clip(1 - seg_dist(x, y, 3, 3, -size / 2, -size / 2) / (3 + i % 2), 0, 1) * (x < 3)
        a = np.maximum(rock, trail * 0.8)
        sh = rock * (0.3 + 0.4 * (x + y > 6)) + trail * 0.9
        pal = palette(["#4A3218", "#8A6030", "#FF9A3A", "#FFE08A"])
        out.append(img(a, sh, pal))
    return out


def lattice(size=44, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        ph = i / n
        a = np.zeros((size, size), np.float32)
        hexes = 0
        for k in range(6):
            th = k / 6 * TAU + ph * 0.5
            pts = [(math.cos(th + j * TAU / 6) * size * 0.42, math.sin(th + j * TAU / 6) * size * 0.42) for j in range(7)]
            a = np.maximum(a, line(x, y, pts, 0.7) * 0.9)
        r = np.hypot(x, y)
        a = np.maximum(a, (r < size * 0.42) * 0.25)
        glint = (np.abs(x + y - (ph * size * 2 - size)) < 2) * (r < size * 0.42)
        a = np.maximum(a, glint * 0.8)
        out.append(img(a, 0.5 + 0.5 * glint + 0.3 * (a > 0.8), P["jade"]))
    return out


def planet(size=72, n=8):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        r = np.hypot(x, y)
        sphere = disc(x, y, 0, 0, size * 0.34)
        stripes = (np.sin((y + x * 0.3) * 0.35 + t * 2) > 0.3)
        rings_ = ring(np.hypot(x / 1.0, y / 0.28), size * 0.47, 1.4) * (y > -2)
        a = np.maximum(sphere, rings_)
        sh = sphere * (0.25 + 0.35 * stripes + 0.3 * (x < -size * 0.08)) + rings_ * 0.9
        out.append(img(a, sh, P["rock"]))
    return out


# ------------------------------------------------------------------ Electro

def iai(w=72, h=10, n=6):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        core = np.clip(1 - np.abs(y) / (0.6 + 1.6 * (1 - t)), 0, 1) * (np.abs(x) < w / 2 * min(1, 0.4 + t * 3))
        glow = np.clip(1 - np.abs(y) / 4.5, 0, 1) * 0.5 * (1 - t)
        a = np.maximum(core * (1 - t * 0.5), glow)
        out.append(img(a, core, P["volt"]))
    return out


def bolt(w=36, h=14, n=4):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        r = np.random.default_rng(100 + i)
        pts = [(-w / 2, 0)] + [(-w / 2 + k * w / 6, r.uniform(-4.5, 4.5)) for k in range(1, 6)] + [(w / 2, 0)]
        core = line(x, y, pts, 0.6)
        glow = line(x, y, pts, 2.4) * 0.45
        out.append(img(np.maximum(core, glow), core * 0.9 + glow * 0.5, P["volt"]))
    return out


def sigil(size=52, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        rot = i / n * TAU / 3
        a = ring(r, size * 0.46, 0.8) + ring(r, size * 0.38, 0.6) * 0.7
        for k in range(3):
            th = rot + k * TAU / 3
            cx, cy = math.cos(th) * size * 0.16, math.sin(th) * size * 0.16
            comma = disc(x, y, cx, cy, size * 0.08)
            tail = np.clip(1 - np.abs(np.hypot(x - cx * 0.2, y - cy * 0.2) - size * 0.14) / 1.4, 0, 1) * \
                   (np.cos(ang - th - 1.0) > 0.4)
            a = np.maximum(a, np.maximum(comma, tail))
        ticks = (np.cos(ang * 18 + rot * 3) > 0.92) * ring(r, size * 0.42, 1.6)
        a = np.clip(np.maximum(a, ticks), 0, 1)
        out.append(img(a, 0.4 + 0.6 * (r < size * 0.25), P["volt"]))
    return out


def cross_cut(size=52, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        L = size * 0.48 * min(1, t * 3)
        th = math.pi / 4
        c1 = line(x, y, [(-L * math.cos(th), -L * math.sin(th)), (L * math.cos(th), L * math.sin(th))], 1.2 * (1 - t) + 0.4)
        c2 = line(x, y, [(-L * math.cos(th), L * math.sin(th)), (L * math.cos(th), -L * math.sin(th))], 1.2 * (1 - t) + 0.4)
        a = np.maximum(c1, c2 * (t > 0.2)) * (1 - max(0, t - 0.6) * 2.5)
        out.append(img(a, a, P["volt"]))
    return out


def storm_eye(w=72, h=48, n=8):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        open_ = math.sin(min(1, t * 1.6) * math.pi / 2) * (1 - max(0, t - 0.8) * 5)
        lid = np.hypot(x / (w * 0.42), y / (h * 0.36 * open_ + 0.01))
        eye = (lid < 1).astype(np.float32)
        iris = disc(x, y, 0, 0, h * 0.2 * open_)
        pupil = (np.abs(x) < 1.2) & (np.abs(y) < h * 0.18 * open_)
        lashes = np.zeros_like(x)
        for k in range(7):
            th = -math.pi * (0.15 + 0.7 * k / 6)
            lashes = np.maximum(lashes, line(x, y, [(math.cos(th) * w * 0.4, math.sin(th) * h * 0.36 * open_),
                                                     (math.cos(th) * w * 0.52, math.sin(th) * h * 0.5 * open_)], 0.5))
        a = np.maximum.reduce([ring(lid, 1, 0.08), iris, lashes, eye * 0.35])
        sh = 0.3 + 0.7 * iris - 0.6 * pupil
        out.append(img(a, sh, P["volt"]))
    return out


# ------------------------------------------------------------------ Dendro

def thorn_whip(w=52, h=14, n=6):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        ph = i / n * TAU
        cy = np.sin(x * 0.25 + ph) * 3
        vine = np.clip(1.3 - np.abs(y - cy), 0, 1)
        thorns = ((np.sin(x * 0.9) > 0.85) & (np.abs(y - cy - 2) < 1.4)) | ((np.sin(x * 0.9 + 2) > 0.85) & (np.abs(y - cy + 2) < 1.4))
        a = np.maximum(vine, thorns * 1.0)
        out.append(img(a, 0.4 + 0.3 * (y < cy) + 0.3 * thorns, P["leaf"]))
    return out


def seed(size=18, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        p = 1 + 0.15 * math.sin(i / n * TAU)
        pod = np.hypot(x / (5 * p), y / (7 * p)) < 1
        stripe = np.abs(x) < 0.8
        glow = disc(x, y, 0, 0, 8 * p) * 0.3
        a = np.maximum(pod.astype(np.float32), glow)
        out.append(img(a, 0.3 + 0.4 * pod + 0.3 * stripe * pod, P["leaf"]))
    return out


def root(w=40, h=20, n=6):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((h, w), np.float32)
        for k in range(5):
            bx = -w * 0.4 + k * w * 0.2
            hgt = h * 0.8 * min(1, t * 2.5 - k * 0.15)
            if hgt <= 0:
                continue
            pts = [(bx, h / 2), (bx + 2, h / 2 - hgt * 0.5), (bx - 1, h / 2 - hgt)]
            a = np.maximum(a, line(x, y, pts, 1.2 - 0.3 * (k % 2)))
        a *= 1 - max(0, t - 0.8) * 5
        out.append(img(a, 0.3 + 0.5 * (x > 0), P["leaf"]))
    return out


def leaves(size=44, n=8):
    x, y = grid(size, size)
    out = []
    lf = [(RNG.uniform(0, TAU), RNG.uniform(0.2, 0.45)) for _ in range(12)]
    for i in range(n):
        ph = i / n * TAU
        a = np.zeros((size, size), np.float32)
        sh = np.zeros_like(a)
        for k, (th0, rr) in enumerate(lf):
            th = th0 + ph
            cx, cy = math.cos(th) * size * rr, math.sin(th) * size * rr * 0.6
            leaf = np.hypot((x - cx) / 2.6, (y - cy) / 1.4) < 1
            a = np.maximum(a, leaf)
            sh = np.maximum(sh, leaf * (0.5 + 0.5 * (k % 2)))
        out.append(img(a, sh, P["leaf"]))
    return out


def flower(size=36, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        t = i / (n - 1)
        open_ = min(1, t * 1.8)
        petal = (np.cos(ang * 6) * 0.5 + 0.5) ** 1.5
        a = (r < size * (0.1 + 0.32 * open_ * (0.5 + 0.5 * petal))).astype(np.float32)
        center = r < size * 0.1
        a *= 1 - max(0, t - 0.8) * 5
        sh = 0.2 + 0.5 * petal + 0.4 * center
        out.append(img(a, sh, P["bloom"]))
    return out


def world_tree(w=72, h=88, n=10):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        g = min(1, t * 1.6)
        trunk = (np.abs(x) < 3 + (y - (-h * 0.1)) * 0.06) & (y > h / 2 - h * 0.6 * g) & (y < h / 2)
        canopy = np.hypot(x / (w * 0.46 * g + 0.01), (y + h * 0.18) / (h * 0.3 * g + 0.01)) < 1
        branches = np.zeros_like(x)
        for k in range(6):
            th = -math.pi * (0.1 + 0.8 * k / 5)
            branches = np.maximum(branches, line(x, y, [(0, -h * 0.05), (math.cos(th) * w * 0.35 * g, -h * 0.05 + math.sin(th) * h * 0.25 * g)], 1.0))
        a = np.maximum.reduce([trunk.astype(np.float32), canopy * 0.85, branches]) * (1 - max(0, t - 0.85) * 6)
        sh = trunk * 0.15 + canopy * (0.45 + 0.35 * (np.sin(x * 0.5) * np.sin(y * 0.5) > 0.2)) + branches * 0.2
        out.append(img(a, sh, P["leaf"]))
    return out


# ------------------------------------------------------------------ Hydro

def water_whip(w=52, h=14, n=6):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        ph = i / n * TAU
        cy = np.sin(x * 0.18 + ph) * 3.5
        body = np.clip(2.2 - np.abs(y - cy), 0, 1)
        foam = (np.abs(y - cy + 1.6) < 0.7) & (np.sin(x * 0.7 + ph * 2) > 0.2)
        drops = noise(w, h, 7 + i) > 0.97
        a = np.maximum.reduce([body, foam * 1.0, drops * 1.0])
        out.append(img(a, 0.45 + 0.4 * foam + 0.15 * (y < cy), P["water"]))
    return out


def bubble(size=26, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        p = 1 + 0.06 * math.sin(i / n * TAU)
        r = np.hypot(x, y) / p
        a = np.maximum(ring(r, size * 0.42, 1.0), (r < size * 0.42) * 0.3)
        hi = disc(x, y, -size * 0.15, -size * 0.15, 2.5)
        out.append(img(np.maximum(a, hi), 0.4 + 0.6 * hi + 0.3 * ring(r, size * 0.42, 1.0), P["water"]))
    return out


def riptide(w=56, h=28, n=6):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        cx = -w * 0.2 + w * 0.3 * t
        crest = np.clip(1 - np.abs(y - (-np.cos((x - cx) * 0.12) * h * 0.3)) / 2, 0, 1) * (np.abs(x - cx) < w * 0.35)
        body = (y > -np.cos((x - cx) * 0.12) * h * 0.3) & (y < h * 0.4) & (np.abs(x - cx) < w * 0.35)
        a = np.maximum(crest, body * 0.6) * (1 - max(0, t - 0.75) * 4)
        out.append(img(a, 0.3 + 0.6 * crest + 0.1 * body, P["water"]))
    return out


def droplet():
    w, h = 16, 7
    x, y = grid(w, h)
    head = np.hypot((x - 4) / 3, y / 2.4) < 1
    tail = np.clip(1 - np.abs(y) / (1.2 * np.clip((x + 8) / 12, 0, 1) + 0.01), 0, 1) * (x < 4)
    return [img(np.maximum(head, tail * 0.8), head * 0.7 + tail * 0.4 + 0.2, P["water"])]


def spring(w=32, h=44, n=8):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((h, w), np.float32)
        for k in range(9):
            vx = (k - 4) * 0.7
            for s in range(6):
                tt = (t + s / 6) % 1
                px, py = vx * tt * 6, h * 0.4 - tt * h * 0.9 + tt * tt * h * 0.6
                a = np.maximum(a, disc(x, y, px, py, 1.1 * (1 - tt * 0.5)))
        out.append(img(a, 0.5 + 0.5 * (y < 0), P["water"]))
    return out


def leviathan(w=72, h=44, n=10):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        rise = math.sin(min(1, t * 1.4) * math.pi / 2) * (1 - max(0, t - 0.85) * 6)
        cy = h * 0.35 - (np.sin((x / w + 0.1) * math.pi) * h * 0.7 * rise)
        body = np.clip(3.2 - np.abs(y - cy), 0, 1) * (np.abs(x) < w * 0.45)
        head = disc(x, y, w * 0.4, h * 0.35 - np.sin((0.5 + 0.1) * math.pi) * h * 0.7 * rise, 4.5 * rise)
        fins = ((np.sin(x * 0.4) > 0.8) & (y < cy - 2) & (y > cy - 6)) * (np.abs(x) < w * 0.4)
        a = np.maximum.reduce([body, head, fins * 1.0])
        out.append(img(a, 0.3 + 0.4 * (y < cy) + 0.3 * fins, P["water"]))
    return out


# ------------------------------------------------------------------ Pyro

def flame_fist(size=28, n=6):
    x, y = grid(size, size)
    out = []
    nz = noise(size, size, 11)
    for i in range(n):
        t = i / (n - 1)
        off = -4 + 10 * min(1, t * 2)
        fist = disc(x, y, off, 0, 6.5)
        flame = np.clip(1 - np.hypot((x - off + 6) / 9, y / 5), 0, 1) * (x < off)
        flick = np.clip((nz - 0.4 + flame) * 1.5, 0, 1) * flame
        a = np.maximum(fist, flick) * (1 - max(0, t - 0.75) * 4)
        sh = fist * 0.85 + flick * 0.5
        out.append(img(a, sh, P["fire"]))
    return out


def fire_pillar(w=28, h=60, n=8):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        ph = i * 1.3
        rise = min(1, t * 2.5)
        width = w * 0.3 * (1 + 0.15 * np.sin(y * 0.4 + ph)) * (1 - max(0, t - 0.7) * 2)
        body = (np.abs(x) < width) & (y > h / 2 - h * rise) & (y < h / 2)
        tongues = (np.sin(x * 0.8 + ph) * 3 + (y - (h / 2 - h * rise))) < 4
        core = np.abs(x) < width * 0.4
        a = (body & tongues).astype(np.float32)
        sh = 0.3 + 0.35 * core + 0.3 * (y > h * 0.2)
        out.append(img(a, sh, P["fire"]))
    return out


def phoenix(w=52, h=36, n=6):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        flap = math.sin(i / n * TAU)
        bodyp = np.hypot((x - 4) / 9, y / 3.2) < 1
        headp = disc(x, y, 14, -2, 3.2)
        wing_up = poly_mask(x, y, [(-2, -1), (6, -2), (-8, -h * 0.45 * (0.4 + 0.6 * (flap + 1) / 2)), (-20, -h * 0.2)])
        wing_dn = poly_mask(x, y, [(-2, 1), (6, 1), (-10, h * 0.2 * (1 - (flap + 1) / 2) + 2)])
        tail = poly_mask(x, y, [(-6, -1), (-6, 1), (-26, 6 + flap * 2), (-24, -1), (-26, -7 - flap * 2)])
        a = np.maximum.reduce([bodyp * 1.0, headp, wing_up, wing_dn, tail * 0.9])
        sh = 0.25 + 0.5 * bodyp + 0.6 * headp + 0.3 * wing_up + 0.1 * tail
        out.append(img(a, sh, P["fire"]))
    return out


def ember(size=10, n=4):
    x, y = grid(size, size)
    return [img(disc(x, y, 0, 0, 2.2 + (i % 2)), disc(x, y, 0, 0, 1.6), P["fire"]) for i in range(n)]


def sun(size=72, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        rot = i / n * TAU / 12
        diskp = r < size * 0.24
        rays = (np.cos((ang + rot) * 12) > 0.6) * (r < size * (0.36 + 0.08 * np.sin(ang * 6 + i))) * (r > size * 0.22)
        corona = ring(r, size * 0.28, 2.0)
        a = np.maximum.reduce([diskp * 1.0, rays * 0.9, corona])
        sh = 0.55 + 0.4 * diskp * (r < size * 0.16) + 0.1 * corona
        out.append(img(a, sh, P["fire"]))
    return out


# ------------------------------------------------------------------ Cryo

def ice_lance():
    w, h = 28, 9
    x, y = grid(w, h)
    body = poly_mask(x, y, [(-13, -1.2), (6, -3), (13, 0), (6, 3), (-13, 1.2)])
    facet = (y < 0) & (x > -10)
    return [img(body, 0.3 + 0.5 * facet + 0.2 * (x > 6), P["ice"])]


def ice_garden(w=44, h=34, n=8):
    x, y = grid(w, h)
    out = []
    shards = [(RNG.uniform(-w * 0.4, w * 0.4), RNG.uniform(5, 14), RNG.uniform(2, 4), RNG.uniform(-0.3, 0.3)) for _ in range(9)]
    for i in range(n):
        t = i / (n - 1)
        g = min(1, t * 2)
        a = np.zeros((h, w), np.float32)
        sh = np.zeros_like(a)
        base = h * 0.4
        for sx, hg, hw, lean in shards:
            top = base - hg * g * 1.4
            pts = [(sx - hw, base), (sx + lean * hg, top), (sx + hw, base)]
            m = poly_mask(x, y, pts)
            a = np.maximum(a, m)
            sh = np.maximum(sh, m * (0.4 + 0.5 * (x > sx)))
        a *= 1 - max(0, t - 0.8) * 5
        out.append(img(a, sh, P["ice"]))
    return out


def snowflake(size=26, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        rot = i / n * math.pi / 3
        a = np.zeros((size, size), np.float32)
        for k in range(6):
            th = rot + k * math.pi / 3
            ex, ey = math.cos(th) * size * 0.42, math.sin(th) * size * 0.42
            a = np.maximum(a, line(x, y, [(0, 0), (ex, ey)], 0.7))
            mx, my = ex * 0.6, ey * 0.6
            for s in (-1, 1):
                a = np.maximum(a, line(x, y, [(mx, my), (mx + math.cos(th + s * 0.8) * 4, my + math.sin(th + s * 0.8) * 4)], 0.5))
        out.append(img(a, 0.6 + 0.4 * (np.hypot(x, y) < 4), P["ice"]))
    return out


def shatter(size=44, n=7):
    x, y = grid(size, size)
    out = []
    shards = [(RNG.uniform(0, TAU), RNG.uniform(0.6, 1.0)) for _ in range(12)]
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((size, size), np.float32)
        sh = np.zeros_like(a)
        for th, sp in shards:
            cx, cy = math.cos(th) * size * 0.45 * t * sp, math.sin(th) * size * 0.45 * t * sp
            pts = [(cx + math.cos(th) * 4, cy + math.sin(th) * 4), (cx + math.cos(th + 2.2) * 2, cy + math.sin(th + 2.2) * 2),
                   (cx + math.cos(th - 2.2) * 2, cy + math.sin(th - 2.2) * 2)]
            m = poly_mask(x, y, pts)
            a = np.maximum(a, m * (1 - t * 0.6))
            sh = np.maximum(sh, m * (0.5 + 0.5 * (math.sin(th) > 0)))
        out.append(img(a, sh, P["ice"]))
    return out


def zero(size=72, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        t = i / (n - 1)
        R = size * (0.1 + 0.38 * t)
        wave = ring(r, R, 2.4 * (1 - t) + 0.7)
        crystals = (np.cos(ang * 16) > 0.7) * np.clip(1 - np.abs(r - R * 0.85) / (size * 0.08), 0, 1) * (r < R)
        frost = (r < R) * 0.2 * (1 - t)
        a = np.maximum.reduce([wave, crystals, frost])
        out.append(img(a, 0.3 + 0.6 * wave + 0.4 * crystals, P["ice"]))
    return out


# ------------------------------------------------------------------ Delusion / Allogene / Exalted

def bullet():
    w, h = 12, 4
    x, y = grid(w, h)
    a = np.clip(1.4 - np.abs(y), 0, 1) * np.clip((x + w / 2) / w * 1.6, 0, 1)
    return [img(a, np.clip((x + w / 2) / w, 0, 1), P["blood"])]


def bat(w=18, h=12, n=4):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        flap = math.sin(i / n * TAU)
        body = np.hypot(x / 2.2, y / 3) < 1
        wl = poly_mask(x, y, [(-1, -1), (-8, -4 - flap * 3), (-6, 1), (-3, 2)])
        wr = poly_mask(x, y, [(1, -1), (8, -4 - flap * 3), (6, 1), (3, 2)])
        a = np.maximum.reduce([body * 1.0, wl, wr])
        sh = 0.2 + 0.4 * body + 0.3 * (y < -2)
        out.append(img(a, sh, P["volt"]))
    return out


def mirror(size=28, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((size, size), np.float32)
        for k in range(7):
            th = k / 7 * TAU + 0.4
            cx, cy = math.cos(th) * size * 0.35 * t, math.sin(th) * size * 0.35 * t
            a = np.maximum(a, poly_mask(x, y, [(cx, cy - 3), (cx + 2.5, cy + 2), (cx - 2.5, cy + 1)]))
        if t < 0.3:
            a = np.maximum(a, (np.abs(x) < 6) * (np.abs(y) < 9) * 1.0)
        out.append(img(a * (1 - t * 0.5), 0.5 + 0.5 * (x + y < 0), palette(["#5A6A8A", "#A8B8D8", "#E8F0FF", "#FFFFFF"])))
    return out


def lifeburn(size=56, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        t = i / (n - 1)
        R = size * (0.15 + 0.3 * t)
        flames = (r < R * (0.8 + 0.25 * np.sin(ang * 9 + i * 1.7)))
        hollow = r < R * 0.5 * (1 - t)
        a = (flames & ~hollow).astype(np.float32) * (1 - max(0, t - 0.7) * 3)
        sh = np.clip(1 - r / R, 0, 1)
        out.append(img(a, sh, P["blood"]))
    return out


def star_sword():
    w, h = 32, 10
    x, y = grid(w, h)
    blade = poly_mask(x, y, [(-14, -1.5), (10, -1.5), (15, 0), (10, 1.5), (-14, 1.5)])
    guard = (np.abs(x + 9) < 1.2) & (np.abs(y) < 4.5)
    glow = np.clip(1 - np.abs(y) / 4.5, 0, 1) * (x > -14) * 0.4
    return [img(np.maximum.reduce([blade, guard * 1.0, glow]), 0.3 + 0.6 * blade + 0.1 * guard, P["star"])]


def star_rift(size=72, n=10):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    stars = RNG.uniform(-size / 2, size / 2, (24, 2))
    for i in range(n):
        t = i / (n - 1)
        open_ = math.sin(min(1, t * 1.5) * math.pi / 2) * (1 - max(0, t - 0.85) * 6)
        tear = np.hypot(x / (size * 0.1 * open_ + 0.01), y / (size * 0.45 * open_ + 0.01)) < 1
        edge = ring(np.hypot(x / (size * 0.1 * open_ + 0.01), y / (size * 0.45 * open_ + 0.01)), 1, 0.12)
        st = np.zeros_like(x)
        for sx, sy in stars:
            st = np.maximum(st, disc(x, y, sx * open_, sy * open_, 0.8))
        a = np.maximum.reduce([tear * 0.85, edge, st * open_])
        out.append(img(a, 0.1 + 0.8 * edge + 0.9 * st, P["star"]))
    return out


def jade_spear():
    w, h = 40, 8
    x, y = grid(w, h)
    shaft = (np.abs(y) < 0.8) & (x < 12) & (x > -19)
    head = poly_mask(x, y, [(12, -3), (19, 0), (12, 3), (13, 0)])
    tassel = (np.abs(x + 12) < 1.5) & (np.abs(y) < 3.5)
    return [img(np.maximum.reduce([shaft * 1.0, head, tassel * 1.0]), 0.4 + 0.5 * head + 0.2 * tassel, P["jade"])]


def yaksha_mask():
    s = 26
    x, y = grid(s, s)
    face = np.hypot(x / 9, y / 11) < 1
    horns = poly_mask(x, y, [(-7, -6), (-11, -13), (-4, -8)]) + poly_mask(x, y, [(7, -6), (11, -13), (4, -8)])
    eyes = (np.hypot((x + 3.5) / 2.5, (y + 1) / 1.4) < 1) | (np.hypot((x - 3.5) / 2.5, (y + 1) / 1.4) < 1)
    mouth = (np.abs(y - 5) < 1) & (np.abs(x) < 5)
    a = np.clip(face + horns, 0, 1)
    sh = 0.6 - 0.55 * eyes - 0.5 * mouth + 0.2 * (y < -3)
    return [img(a, sh, P["jade"])]


def cloud(w=36, h=18, n=4):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        ph = i / n * TAU
        a = np.zeros((h, w), np.float32)
        for k in range(5):
            cx = -w * 0.32 + k * w * 0.16 + math.sin(ph + k) * 1.2
            a = np.maximum(a, disc(x, y, cx, 2 - (k % 2) * 3, 5 + (k % 2)))
        swirl = ring(np.hypot(x + 6, y), 3, 0.6)
        out.append(img(a, 0.5 + 0.4 * (y < 0) - 0.3 * swirl, palette(["#5A8A9A", "#A8D8E0", "#E8FBFF", "#FFFFFF"])))
    return out


def decree(size=52, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        t = i / (n - 1)
        rot = t * 0.5
        a = ring(r, size * 0.44, 0.9) * min(1, t * 3)
        for k in range(6):
            th = rot + k * math.pi / 3
            a = np.maximum(a, line(x, y, [(0, 0), (math.cos(th) * size * 0.38, math.sin(th) * size * 0.38)], 0.8) * min(1, t * 2))
        eye = (np.hypot(x / 7, y / 3.5) < 1) * min(1, max(0, t * 2 - 0.5))
        pupil = disc(x, y, 0, 0, 2)
        a = np.maximum(a, eye)
        out.append(img(a * (1 - max(0, t - 0.85) * 6), 0.4 + 0.6 * (r > size * 0.4) - 0.4 * pupil * eye, P["ice"]))
    return out


def x_cut(size=52, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        L = size * 0.46 * min(1, t * 2.5)
        w = 2.0 * (1 - t) + 0.5
        a = np.maximum(line(x, y, [(-L, -L), (L, L)], w), line(x, y, [(-L, L), (L, -L)], w)) * (1 - max(0, t - 0.6) * 2.5)
        out.append(img(a, a, P["blood"]))
    return out


def rune(size=26, n=6):
    x, y = grid(size, size)
    r = np.hypot(x, y)
    out = []
    for i in range(n):
        p = 1 + 0.1 * math.sin(i / n * TAU)
        a = ring(r, size * 0.42 * p, 0.7)
        a = np.maximum(a, line(x, y, [(-6, 7), (0, -8), (6, 7), (-7, -2), (7, -2), (-6, 7)], 0.6))
        out.append(img(a, 0.5 + 0.5 * (r < 4), P["lunar"]))
    return out


def owl(w=18, h=14, n=4):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        flap = math.sin(i / n * TAU)
        body = np.hypot(x / 3.5, (y - 1) / 4) < 1
        eyes = (np.hypot(x + 1.5, y + 1) < 1.1) | (np.hypot(x - 1.5, y + 1) < 1.1)
        wl = poly_mask(x, y, [(-2, 0), (-9, -3 - flap * 3), (-7, 3)])
        wr = poly_mask(x, y, [(2, 0), (9, -3 - flap * 3), (7, 3)])
        a = np.maximum.reduce([body * 1.0, wl, wr])
        out.append(img(a, 0.4 + 0.2 * body + 0.5 * eyes, P["lunar"]))
    return out


def ritual(size=72, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        rot = i / n * TAU / 5
        a = ring(r, size * 0.46, 0.8) + ring(r, size * 0.34, 0.6)
        pts = [(math.cos(rot + k * 2 * TAU / 5 - math.pi / 2) * size * 0.34, math.sin(rot + k * 2 * TAU / 5 - math.pi / 2) * size * 0.34) for k in range(6)]
        a = np.maximum(a, line(x, y, pts, 0.7))
        glyphs = (np.cos((ang - rot) * 20) > 0.8) * ring(r, size * 0.40, 1.6)
        a = np.clip(np.maximum(a, glyphs), 0, 1)
        out.append(img(a, 0.4 + 0.6 * glyphs, P["lunar"]))
    return out


def rift(w=44, h=60, n=8):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        open_ = math.sin(min(1, t * 1.6) * math.pi / 2) * (1 - max(0, t - 0.85) * 6)
        jag = np.sin(y * 0.6) * 2 * open_
        tear = np.abs(x - jag) < (w * 0.22 * open_) * np.clip(1 - np.abs(y) / (h * 0.46), 0, 1) ** 0.6
        edge = np.abs(np.abs(x - jag) - (w * 0.22 * open_) * np.clip(1 - np.abs(y) / (h * 0.46), 0, 1) ** 0.6) < 1.2
        a = np.maximum(tear * 0.9, edge * 1.0)
        out.append(img(a, 0.05 + 0.85 * edge, P["abyss"]))
    return out


def shadow_hand(w=26, h=34, n=6):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        rise = min(1, t * 2)
        grip = max(0, t - 0.5) * 2
        palm = np.hypot(x / 6, (y - h * 0.2 + h * 0.3 * rise) / 5) < 1
        arm = (np.abs(x) < 3.5) & (y > h * 0.2 - h * 0.3 * rise) & (y < h / 2)
        fingers = np.zeros_like(x)
        for k in range(4):
            fx = -4.5 + k * 3
            top = h * 0.2 - h * 0.3 * rise - 6
            fingers = np.maximum(fingers, line(x, y, [(fx, top + 4), (fx * (1 - grip * 0.6), top - 4 + grip * 5)], 1.0))
        a = np.maximum.reduce([palm * 1.0, arm * 1.0, fingers])
        out.append(img(a, 0.2 + 0.5 * (x > 0) + 0.3 * fingers, P["abyss"]))
    return out


def corrosion(size=34, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        pool = np.hypot(x / (size * 0.4), (y - size * 0.25) / (size * 0.12)) < 1
        drips = np.zeros_like(x)
        for k in range(6):
            dx = -size * 0.35 + k * size * 0.14
            drips = np.maximum(drips, (np.abs(x - dx) < 1) & (y < size * 0.25) & (y > size * 0.25 - (4 + (k * 7 + i * 3) % 9)))
        bubbles = noise(size, size, 40 + i) > 0.96
        a = np.maximum.reduce([pool * 0.9, drips * 1.0, bubbles * pool * 1.0])
        out.append(img(a, 0.3 + 0.5 * drips + 0.4 * bubbles, palette(["#1A2A0A", "#4A7A1A", "#9AE03A", "#E8FF9A"])))
    return out


def fury_crown(size=44, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        ph = i / n * TAU
        band = (np.abs(y - 6) < 3) & (np.abs(x) < size * 0.38)
        spikes = np.zeros_like(x)
        for k in range(5):
            cx = -size * 0.3 + k * size * 0.15
            hgt = 10 + 4 * math.sin(ph + k)
            spikes = np.maximum(spikes, poly_mask(x, y, [(cx - 3, 4), (cx, 4 - hgt), (cx + 3, 4)]))
        flames = (noise(size, size, 60 + i) > 0.7) * (y < 0) * (np.abs(x) < size * 0.4) * (y > -16)
        a = np.maximum.reduce([band * 1.0, spikes, flames * 0.7])
        out.append(img(a, 0.4 + 0.4 * band + 0.3 * flames, P["gold"]))
    return out


def dome(size=72, n=8):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        R = size * 0.46 * min(1, t * 2)
        e = np.hypot(x, y / 0.9)
        shell = ring(e, R, 1.2) * (y < size * 0.1)
        fill = (e < R) * (y < size * 0.1) * 0.22
        hexes = (np.sin(x * 0.6) * np.sin(y * 0.6) > 0.7) * (e < R) * (y < size * 0.1) * 0.5
        base = ring(np.hypot(x, (y - size * 0.1) / 0.3), R, 1.0)
        a = np.maximum.reduce([shell, fill, hexes, base]) * (1 - max(0, t - 0.85) * 6)
        out.append(img(a, 0.4 + 0.5 * shell + 0.3 * hexes, P["gold"]))
    return out


# ------------------------------------------------------------------ Dragons

def breath(w=56, h=28, n=6):
    """A flame cone pointing right; tinted per element."""
    x, y = grid(w, h)
    out = []
    for i in range(n):
        nz = noise(w, h, 80 + i)
        along = np.clip((x + w / 2) / w, 0, 1)
        cone = np.abs(y) < (1.5 + along * h * 0.45)
        a = cone * np.clip((nz - 0.25 + (1 - along) * 0.4) * 2, 0, 1)
        core = np.abs(y) < (0.8 + along * h * 0.15)
        out.append(img(np.maximum(a, core * 1.0), 0.3 + 0.5 * core + 0.2 * (1 - along)))
    return out


def claw(size=48, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((size, size), np.float32)
        for k in range(3):
            off = (k - 1) * 7
            L = size * 0.45 * min(1, t * 2.5)
            a = np.maximum(a, line(x, y, [(-L + off, -L), (L * 0.4 + off, L)], 1.6 * (1 - t) + 0.5))
        out.append(img(a * (1 - max(0, t - 0.6) * 2.5), a))
    return out


def wingbeat(w=72, h=36, n=6):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        e = np.hypot(x / (w * (0.2 + 0.28 * t)), (y - h * 0.3) / (h * (0.3 + 0.4 * t)))
        a = ring(e, 1, 0.12) * (y < h * 0.3) * (1 - t * 0.7)
        streaks = (np.sin(x * 0.4 + t * 5) > 0.9) * (e < 1) * (y < h * 0.3) * 0.5
        out.append(img(np.maximum(a, streaks), a))
    return out


def roar(size=72, n=8):
    x, y = grid(size, size)
    r = np.hypot(x, y)
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((size, size), np.float32)
        for k in range(3):
            R = size * (0.08 + 0.42 * ((t + k / 3) % 1))
            a = np.maximum(a, ring(r, R, 1.4) * (1 - (t + k / 3) % 1))
        out.append(img(a, a))
    return out


def abyss_flame(w=34, h=44, n=8):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        ph = i * 1.1
        width = w * 0.36 * np.clip((y + h / 2) / h, 0.15, 1) * (1 + 0.2 * np.sin(y * 0.5 + ph))
        body = (np.abs(x) < width) & ((np.sin(x * 0.7 + ph) * 3 + y) > -h * 0.4)
        core = np.abs(x) < width * 0.4
        out.append(img(body * 1.0, 0.25 + 0.5 * core + 0.25 * (y > 0), P["abyss"]))
    return out


def moon_shard(size=18, n=4):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        rot = i / n * TAU
        m = disc(x, y, 0, 0, 7) * (1 - disc(x, y, math.cos(rot) * 4, math.sin(rot) * 4, 6))
        out.append(img(np.clip(m, 0, 1), 0.6 + 0.4 * (x < 0), P["lunar"]))
    return out


# ------------------------------------------------------------------ Shades

def clock(size=56, n=12):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        th_m = -math.pi / 2 + i / n * TAU
        th_h = -math.pi / 2 + i / n * TAU / 12
        face = ring(r, size * 0.44, 1.0) + ring(r, size * 0.38, 0.5) * 0.6
        ticks = (np.cos(ang * 12) > 0.96) * ring(r, size * 0.41, 2)
        hands = np.maximum(line(x, y, [(0, 0), (math.cos(th_m) * size * 0.34, math.sin(th_m) * size * 0.34)], 0.7),
                           line(x, y, [(0, 0), (math.cos(th_h) * size * 0.22, math.sin(th_h) * size * 0.22)], 1.1))
        a = np.clip(np.maximum.reduce([face, ticks, hands, (r < size * 0.44) * 0.15]), 0, 1)
        out.append(img(a, 0.3 + 0.6 * hands + 0.3 * ticks, P["time"]))
    return out


def sand(w=24, h=40, n=8):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        a = np.zeros((h, w), np.float32)
        for k in range(30):
            tt = ((k / 30) + i / n) % 1
            px = math.sin(k * 2.3) * 2 * (1 - tt)
            py = -h * 0.45 + tt * h * 0.9
            a = np.maximum(a, disc(x, y, px, py, 0.7))
        out.append(img(a, 0.6 + 0.4 * (y < 0), P["gold"]))
    return out


def cube(size=48, n=8):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        rot = i / n * math.pi / 2
        s = size * 0.28
        verts = []
        for cx in (-1, 1):
            for cy in (-1, 1):
                for cz in (-1, 1):
                    xr = cx * math.cos(rot) - cz * math.sin(rot)
                    zr = cx * math.sin(rot) + cz * math.cos(rot)
                    yr = cy * math.cos(0.5) - zr * math.sin(0.5)
                    verts.append((xr * s, yr * s))
        edges = [(0, 1), (2, 3), (4, 5), (6, 7), (0, 2), (1, 3), (4, 6), (5, 7), (0, 4), (1, 5), (2, 6), (3, 7)]
        a = np.zeros((size, size), np.float32)
        for e0, e1 in edges:
            a = np.maximum(a, line(x, y, [verts[e0], verts[e1]], 0.7))
        out.append(img(a, a, P["space"]))
    return out


def portal(size=44, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y / 0.7), np.arctan2(y, x)
    out = []
    for i in range(n):
        ph = i / n * TAU
        swirl = (np.sin(ang * 3 + r * 0.5 - ph) > 0.3) * (r < size * 0.42)
        edge = ring(r, size * 0.42, 1.2)
        a = np.maximum.reduce([swirl * 0.8, edge, (r < size * 0.42) * 0.3])
        out.append(img(a, 0.2 + 0.4 * swirl + 0.5 * edge, P["space"]))
    return out


def vine_bloom(size=48, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        t = i / (n - 1)
        g = min(1, t * 1.6)
        vines = np.zeros_like(x)
        for k in range(5):
            th = k / 5 * TAU
            pts = [(math.cos(th + s * 0.25) * size * 0.45 * g * s / 4, math.sin(th + s * 0.25) * size * 0.45 * g * s / 4) for s in range(5)]
            vines = np.maximum(vines, line(x, y, pts, 0.8))
        petal = (np.cos(ang * 5) * 0.5 + 0.5)
        bloom = r < size * 0.18 * g * (0.6 + 0.4 * petal)
        a = np.maximum(vines, bloom * 1.0) * (1 - max(0, t - 0.85) * 6)
        out.append(img(a, 0.3 + 0.6 * bloom + 0.2 * vines, P["life"]))
    return out


def scythe(size=52, n=6):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        t = i / (n - 1)
        a0 = -2.6 + 3.2 * t
        blade = (ang > a0 - 0.9) & (ang < a0) & (r > size * 0.3) & (r < size * 0.46 - (a0 - ang) * 4)
        trail = (ang > a0 - 2.4) & (ang < a0 - 0.9) & ring(r, size * 0.42, 1.5).astype(bool)
        a = np.maximum(blade * 1.0, trail * 0.5 * (1 - t))
        out.append(img(a, 0.3 + 0.6 * blade, P["death"]))
    return out


def skull(size=26, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        head = np.hypot(x / 8, (y + 2) / 7) < 1
        jaw = (np.abs(x) < 5) & (y > 3) & (y < 9)
        eyes = (np.hypot(x + 3.2, y + 1) < 2) | (np.hypot(x - 3.2, y + 1) < 2)
        teeth = jaw & (np.sin(x * 1.5) > 0) & (y > 6)
        a = np.clip(head + jaw, 0, 1) * min(1, t * 3) * (1 - max(0, t - 0.75) * 4)
        out.append(img(a, 0.75 - 0.7 * eyes - 0.3 * teeth, P["death"]))
    return out


# ------------------------------------------------------------------ Moon / Heaven

def crescent_blade(size=34, n=4):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        rot = i / n * TAU
        m = disc(x, y, 0, 0, 14) * (1 - disc(x, y, math.cos(rot) * 7, math.sin(rot) * 7, 13))
        out.append(img(np.clip(m, 0, 1), 0.5 + 0.5 * (np.hypot(x, y) > 10), P["lunar"]))
    return out


def moon_pillar(w=26, h=72, n=8):
    x, y = grid(w, h)
    out = []
    for i in range(n):
        t = i / (n - 1)
        width = w * 0.35 * math.sin(min(1, t * 2) * math.pi / 2) * (1 - max(0, t - 0.7) * 3)
        core = np.clip(1 - np.abs(x) / (width * 0.35 + 0.3), 0, 1)
        glow = np.clip(1 - np.abs(x) / (width + 0.3), 0, 1) * 0.55
        motes = (noise(w, h, 90 + i) > 0.97) * (np.abs(x) < width * 1.4)
        a = np.maximum.reduce([core, glow, motes * 1.0])
        out.append(img(a, 0.3 + 0.7 * core, P["lunar"]))
    return out


def nail():
    w, h = 18, 72
    x, y = grid(w, h)
    spike = poly_mask(x, y, [(-6, -h / 2), (6, -h / 2), (2, h / 2 - 4), (0, h / 2), (-2, h / 2 - 4)])
    runes = (np.abs(np.sin(y * 0.4)) > 0.9) & (np.abs(x) < 2) & (y < h * 0.3)
    return [img(spike, 0.35 + 0.4 * (x < 0) + 0.3 * runes, P["light"])]


def halo(size=56, n=8):
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    out = []
    for i in range(n):
        rot = i / n * TAU / 8
        rg = ring(r, size * 0.3, 1.4)
        feathers = (np.cos((ang + rot) * 16) > 0.5) * (r > size * 0.33) * (r < size * (0.42 + 0.04 * np.sin(ang * 8)))
        a = np.maximum(rg, feathers * 0.8)
        out.append(img(a, 0.5 + 0.5 * rg, P["light"]))
    return out


def light_cross(size=56, n=6):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        L = size * 0.48 * min(1, t * 2.5)
        w = 2.5 * (1 - t) + 0.6
        a = np.maximum(line(x, y, [(0, -L), (0, L)], w), line(x, y, [(-L * 0.7, -L * 0.25), (L * 0.7, -L * 0.25)], w * 0.8))
        a = np.maximum(a, disc(x, y, 0, -L * 0.25, 5 * (1 - t)))
        out.append(img(a * (1 - max(0, t - 0.7) * 3), a, P["light"]))
    return out



# ------------------------------------------------------------------ v0.6: particles, glow and upgraded core sets
# These come after the originals on purpose: the redefinitions below replace impact, shock, breath, claw and
# lock_sparks with larger, longer, white-hot versions, keeping their names so every technique script still works.

def img_soft(alpha, value=None):
    """Smooth alpha (no quantising) for soft additive sprites. value: grey level 0..1 (default white)."""
    a = np.clip(alpha, 0, 1)
    v = np.ones_like(a) if value is None else np.clip(value, 0, 1)
    g = (150 + 105 * v)
    rgba = np.stack([g, g, g, a * 255], -1).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA")


def glow(size=64):
    """A soft radial glow that reaches zero well inside the canvas, so it can never show as a square."""
    x, y = grid(size, size)
    r = np.hypot(x, y) / (size * 0.5)
    a = np.clip(1 - r / 0.86, 0, 1) ** 2.2
    return [img_soft(a)]


def ring_soft(size=64):
    x, y = grid(size, size)
    r = np.hypot(x, y) / (size * 0.5)
    a = np.exp(-((r - 0.62) / 0.11) ** 2) * (r < 0.9)
    return [img_soft(a)]


def spark(w=14, h=4):
    """A hot streak; particles rotate it to their velocity."""
    x, y = grid(w, h)
    along = np.clip((x + w / 2) / w, 0, 1)
    a = np.clip(1 - np.abs(y) / (0.6 + along * 1.4), 0, 1) * along ** 0.7
    return [img_soft(a, 0.6 + 0.4 * along)]


def mote(size=8):
    x, y = grid(size, size)
    r = np.hypot(x, y) / (size * 0.5)
    a = np.clip(1 - r / 0.95, 0, 1) ** 1.3
    return [img_soft(a, 1 - r * 0.4)]


def streak(w=28, h=4):
    x, y = grid(w, h)
    along = np.clip((x + w / 2) / w, 0, 1)
    a = np.clip(1 - np.abs(y) / 1.6, 0, 1) * along ** 1.6
    return [img_soft(a)]


def smoke(size=28, n=4):
    x, y = grid(size, size)
    out = []
    for i in range(n):
        nz = noise(size, size, 300 + i)
        r = np.hypot(x, y) / (size * 0.5)
        lumps = np.zeros_like(r)
        for k in range(5):
            th = k / 5 * TAU + i
            lumps = np.maximum(lumps, np.clip(1 - np.hypot(x - math.cos(th) * size * 0.16,
                                                            y - math.sin(th) * size * 0.16) / (size * 0.24), 0, 1))
        a = np.clip(lumps * (0.75 + 0.35 * nz) - r * 0.25, 0, 1) * (r < 0.92)
        out.append(img_soft(a * 0.85, 0.55 + 0.35 * (y < 0)))
    return out


def rock_chunk(size=7):
    x, y = grid(size, size)
    pts = [(-3, -1), (-1, -3), (2, -2.5), (3, 0.5), (1, 3), (-2, 2.5)]
    a = poly_mask(x, y, pts)
    sh = 0.35 + 0.5 * (x - y > 0) + 0.15 * (y < -1)
    return [img(a, sh)]


def impact(size=64, n=10):
    """An anime hit flash: a white-hot core that pops, a bold four-to-six point star, speed lines, a ring."""
    x, y = grid(size, size)
    r, ang = np.hypot(x, y), np.arctan2(y, x)
    spikes = (np.cos(ang * 4 + 0.3) * 0.5 + 0.5) ** 2.5 * 0.75 + (np.cos(ang * 6 + 1.1) * 0.5 + 0.5) ** 4 * 0.45
    lines = (np.cos(ang * 14 + 0.4) * 0.5 + 0.5) ** 30
    out = []
    for i in range(n):
        t = i / (n - 1)
        pop = 1 - t ** 1.3
        reach = size * (0.18 + 0.32 * t ** 0.55)
        star = np.clip(1.15 - r / (reach * (0.28 + 0.9 * spikes)), 0, 1) * pop
        core = np.clip(1.2 - r / (size * (0.2 if i < 2 else 0.12) * (1 - t * 0.9) + 0.01), 0, 1)
        rg = ring(r, reach * 1.15, 2.2 * (1 - t) + 0.6) * pop * (i >= 1)
        sl = lines * (r > reach * 0.7) * (r < reach * 1.45) * (1 - t) * (1 <= i <= 6)
        a = np.maximum.reduce([star, core * pop, rg, sl * 0.9])
        out.append(img(a, np.maximum(core, star * 0.85 + sl * 0.4)))
    return out


def shock(size=80, n=10):
    """A ground shockwave in perspective: a bright leading ring, a fainter inner one and flung grit."""
    x, y = grid(size, size)
    r = np.hypot(x, y / 0.6)
    dots = RNG.uniform(0, TAU, 26)
    out = []
    for i in range(n):
        t = i / (n - 1)
        R = size * (0.05 + 0.45 * t ** 0.8)
        lead = ring(r, R, 2.6 * (1 - t) + 0.6) * (1 - t * 0.75)
        inner = ring(r, R * 0.72, 1.2) * (1 - t) * 0.55
        fill = np.clip(1 - r / max(R, 1), 0, 1) * (1 - t) ** 3 * 0.45
        a = np.maximum.reduce([lead, inner, fill])
        for th in dots:
            px, py = math.cos(th) * R * 1.12, math.sin(th) * R * 0.6 * 1.12
            a = np.maximum(a, disc(x, y, px, py, 0.9) * (1 - t))
        out.append(img(a, np.maximum(lead, fill * 1.5)))
    return out


def breath(w=80, h=36, n=8):
    """A roaring cone pointing right (source at the left edge): white-hot core, turbulent body, ragged edges."""
    x, y = grid(w, h)
    out = []
    for i in range(n):
        nz = noise(w, h, 90 + i)
        nz2 = noise(w, h, 140 + i)
        along = np.clip((x + w / 2) / w, 0, 1)
        width = 1.5 + along * h * 0.46
        body = np.clip(1 - np.abs(y) / width, 0, 1)
        turb = np.clip((nz * 0.6 + nz2 * 0.4) - 0.2 + (1 - along) * 0.5, 0, 1)
        a = np.clip(body * 2.2 * turb, 0, 1) * (along < 0.98)
        core = np.clip(1 - np.abs(y) / (0.8 + along * h * 0.13), 0, 1) * (along < 0.9)
        mid = np.clip(1 - np.abs(y) / (1.2 + along * h * 0.27), 0, 1)
        shade = 0.25 + 0.35 * mid + 0.4 * core
        out.append(img(np.maximum(a, core), shade))
    return out


def claw(size=56, n=7):
    """Three raking slashes with hot cores and a fading afterglow."""
    x, y = grid(size, size)
    out = []
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((size, size), np.float32)
        hot = np.zeros_like(a)
        L = size * 0.46 * min(1, t * 3)
        for k in range(3):
            off = (k - 1) * 8
            seg = [(-L + off, -L), (L * 0.35 + off, L)]
            a = np.maximum(a, line(x, y, seg, 2.2 * (1 - t) + 0.7))
            hot = np.maximum(hot, line(x, y, seg, 0.8 * (1 - t) + 0.2))
        fade = 1 - max(0, t - 0.55) * 2.2
        out.append(img(a * fade, 0.4 + 0.6 * hot))
    return out


def lock_sparks(size=40, n=8):
    x, y = grid(size, size)
    out = []
    parts = [(RNG.uniform(-1.2, 1.2), RNG.uniform(-2.0, -0.4)) for _ in range(22)]
    for i in range(n):
        t = (i + 1) / n
        a = np.zeros((size, size), np.float32)
        for vx, vy in parts:
            px, py = vx * size * 0.42 * t, vy * size * 0.42 * t + size * 0.55 * t * t
            a = np.maximum(a, line(x, y, [(px, py), (px - vx * 2.6, py - vy * 2.6)], 0.5) * (1 - t * 0.55))
        a = np.maximum(a, disc(x, y, 0, 0, 4.5 * (1 - t) ** 1.5))
        out.append(img(a, a, P["spark"]))
    return out


SETS = {
    # generic
    "impact": impact, "shock": shock, "dust": dust, "debris": debris, "fissure": fissure, "lock_sparks": lock_sparks,
    # anemo
    "feather": feather, "wind_spout": wind_spout, "wind_ring": wind_ring, "vacuum": vacuum, "cyclone": cyclone,
    # geo
    "rock_pillar": rock_pillar, "stone_fist": stone_fist, "geo_meteor": geo_meteor, "lattice": lattice, "planet": planet,
    # electro
    "iai": iai, "bolt": bolt, "sigil": sigil, "cross_cut": cross_cut, "storm_eye": storm_eye,
    # dendro
    "thorn_whip": thorn_whip, "seed": seed, "root": root, "leaves": leaves, "flower": flower, "world_tree": world_tree,
    # hydro
    "water_whip": water_whip, "bubble": bubble, "riptide": riptide, "droplet": droplet, "spring": spring, "leviathan": leviathan,
    # pyro
    "flame_fist": flame_fist, "fire_pillar": fire_pillar, "phoenix": phoenix, "ember": ember, "sun": sun,
    # cryo
    "ice_lance": ice_lance, "ice_garden": ice_garden, "snowflake": snowflake, "shatter": shatter, "zero": zero,
    # delusion / allogene / exalted
    "bullet": bullet, "bat": bat, "mirror": mirror, "lifeburn": lifeburn, "star_sword": star_sword, "star_rift": star_rift,
    "jade_spear": jade_spear, "yaksha_mask": yaksha_mask, "cloud": cloud, "decree": decree, "x_cut": x_cut,
    "rune": rune, "owl": owl, "ritual": ritual, "rift": rift, "shadow_hand": shadow_hand, "corrosion": corrosion,
    "fury_crown": fury_crown, "dome": dome,
    # dragons
    "breath": breath, "claw": claw, "wingbeat": wingbeat, "roar": roar, "abyss_flame": abyss_flame, "moon_shard": moon_shard,
    # shades
    "clock": clock, "sand": sand, "cube": cube, "portal": portal, "vine_bloom": vine_bloom, "scythe": scythe, "skull": skull,
    # v0.6 particles and glow
    "glow": glow, "ring_soft": ring_soft, "spark": spark, "mote": mote, "streak": streak, "smoke": smoke,
    "rock_chunk": rock_chunk,
    # moon / heaven
    "crescent_blade": crescent_blade, "moon_pillar": moon_pillar, "nail": nail, "halo": halo, "light_cross": light_cross,
}


def main():
    total = 0
    for name, make in SETS.items():
        frames = make()
        total += save(name, frames)
    print(f"{len(SETS)} sets, {total} frames -> {OUT}")


if __name__ == "__main__":
    main()
