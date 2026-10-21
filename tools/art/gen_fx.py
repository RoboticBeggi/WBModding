"""Generates every animated combat VFX as white/grey frames that the mod tints per element.

Run from the repo root:  python tools/art/gen_fx.py
Writes GameResources/gi/fx/<set>/<set>_NN.png. Needs Pillow and numpy.
Shapes are computed per pixel (distance fields), then hard-quantised for a crisp pixel-art look.
"""
import math
import os

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "GameResources", "gi", "fx")
RNG = np.random.default_rng(11)


def grid(w, h):
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    return x + 0.5 - w / 2, y + 0.5 - h / 2


def to_image(alpha, bright=None):
    """alpha, bright: float arrays 0..1. Quantised to 4 alpha steps and 3 brightness steps."""
    alpha = np.clip(alpha, 0, 1)
    a = np.round(alpha * 4) / 4
    if bright is None:
        bright = np.ones_like(alpha)
    b = np.round(np.clip(bright, 0, 1) * 3) / 3
    v = (155 + 100 * b).astype(np.uint8)
    rgba = np.stack([v, v, v, (a * 255).astype(np.uint8)], axis=-1)
    return Image.fromarray(rgba, "RGBA")


def save(name, frames):
    folder = os.path.join(OUT, name)
    os.makedirs(folder, exist_ok=True)
    for f in os.listdir(folder):
        if f.endswith(".png"):
            os.remove(os.path.join(folder, f))
    for i, img in enumerate(frames):
        img.save(os.path.join(folder, f"{name}_{i:02d}.png"))


def ring(r, radius, width):
    return np.clip(1 - np.abs(r - radius) / max(width, 0.5), 0, 1)


# ------------------------------------------------------------------ shapes

def slash(size=32, n=6, double=False):
    """A crescent sweep: it opens fast, holds at full thickness, then thins and fades."""
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    a0, span = -2.3, 3.6
    for i in range(n):
        t = i / (n - 1)
        reveal = min(1.0, t * 2.8 + 0.25)
        within = (ang > a0) & (ang < a0 + span * reveal)
        along = np.clip((ang - a0) / span, 0, 1)
        thick = size * 0.13 * math.sin(math.pi * (0.25 + 0.75 * min(1, t * 1.4))) * (1 - 0.5 * t) + 1
        # the blade edge is thickest in the middle of the arc
        local = thick * np.sin(np.clip(along, 0, 1) * math.pi) + 0.6
        rad = size * 0.34
        a = np.clip(1 - np.abs(r - rad) / local, 0, 1) * within
        if double:
            a = np.maximum(a, np.clip(1 - np.abs(r - rad * 0.74) / (local * 0.6), 0, 1) * within * 0.8)
        fade = 1.0 if t < 0.55 else 1 - (t - 0.55) / 0.5
        bright = np.clip(1 - np.abs(r - rad * 1.05) / (local + 0.1), 0, 1)
        frames.append(to_image(a * fade, bright))
    return frames


def hit(size=24, n=5):
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    for i in range(n):
        t = i / (n - 1)
        spikes = (np.cos(ang * 4) * 0.5 + 0.5) ** 6
        reach = size * (0.18 + 0.32 * t)
        a = np.clip(1 - r / (reach * (0.4 + 0.6 * spikes)), 0, 1) * (1 - t * 0.7)
        core = np.clip(1 - r / (size * 0.12 * (1 - t) + 0.01), 0, 1)
        frames.append(to_image(np.maximum(a, core), np.maximum(core, 0.5)))
    return frames


def spark(size=24, n=5):
    frames = []
    x, y = grid(size, size)
    rays = RNG.uniform(0, 2 * math.pi, 9)
    for i in range(n):
        t = i / (n - 1)
        a = np.zeros((size, size), np.float32)
        for th in rays:
            d = np.abs(-np.sin(th) * x + np.cos(th) * y)
            along = np.cos(th) * x + np.sin(th) * y
            start, end = size * 0.08 + size * 0.3 * t, size * 0.18 + size * 0.34 * t
            a = np.maximum(a, (d < 0.9) * (along > start) * (along < end))
        core = np.clip(1 - np.hypot(x, y) / (size * 0.14 * (1 - t) + 0.5), 0, 1)
        frames.append(to_image(np.maximum(a * (1 - 0.3 * t), core)))
    return frames


def burst(size=48, n=8):
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    noise = RNG.random((size, size)).astype(np.float32)
    for i in range(n):
        t = i / (n - 1)
        radius = size * (0.12 + 0.36 * t ** 0.6)
        body = np.clip(1 - r / radius, 0, 1)
        holes = np.clip((noise - t * 0.9) * 3, 0, 1) if t > 0.35 else 1
        a = (body > 0) * holes * (1 - t ** 3)
        a = np.maximum(a, ring(r, radius, 1.4) * (1 - t))
        bright = np.clip(body * 1.6, 0, 1)
        frames.append(to_image(a, bright))
    return frames


def nova(size=64, n=8):
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    for i in range(n):
        t = i / (n - 1)
        radius = size * (0.08 + 0.4 * t)
        a = ring(r, radius, 2.2 * (1 - t) + 0.8) * (1 - t * 0.6)
        inner = np.clip(1 - r / radius, 0, 1) * 0.25 * (1 - t)
        frames.append(to_image(np.maximum(a, inner), a))
    return frames


def orb(size=16, n=4):
    frames = []
    x, y = grid(size, size)
    for i in range(n):
        pulse = 1 + 0.12 * math.sin(i / n * 2 * math.pi)
        r = np.hypot(x - 2, y)
        core = np.clip(1.2 - r / (size * 0.22 * pulse), 0, 1)
        # trail toward -x (sprite is rotated toward travel direction)
        trail = np.clip(1 - np.abs(y) / (size * 0.16), 0, 1) * np.clip((x + size * 0.45) / (size * 0.5), 0, 1) * (x < 2)
        a = np.maximum(core, trail * 0.7)
        frames.append(to_image(a, core))
    return frames


def beam(w=32, h=12, n=4):
    frames = []
    x, y = grid(w, h)
    for i in range(n):
        wobble = np.sin(x * 0.6 + i * 1.6) * 0.8
        core = np.clip(1 - np.abs(y - wobble * 0.3) / (h * 0.16), 0, 1)
        glow = np.clip(1 - np.abs(y - wobble) / (h * 0.45), 0, 1)
        frames.append(to_image(np.maximum(core, glow * 0.6), core))
    return frames


def pillar(w=24, h=64, n=6):
    frames = []
    x, y = grid(w, h)
    for i in range(n):
        t = i / (n - 1)
        width = w * (0.2 + 0.35 * math.sin(min(1, t * 1.8) * math.pi / 2)) * (1 - t * 0.6)
        drop = np.clip((y + h / 2) / (h * min(1, 0.4 + t * 2)), 0, 1) <= 1
        core = np.clip(1 - np.abs(x) / max(0.6, width * 0.45), 0, 1)
        glow = np.clip(1 - np.abs(x) / max(0.8, width), 0, 1)
        flare = np.clip(1 - np.hypot(x, (y - h * 0.44) * 2) / (w * 0.5 * (1 - t * 0.5) + 1), 0, 1)
        a = np.maximum(np.maximum(core, glow * 0.6) * drop, flare)
        frames.append(to_image(a * (1 - t * 0.5), core))
    return frames


def tornado(w=32, h=40, n=6):
    frames = []
    x, y = grid(w, h)
    for i in range(n):
        a = np.zeros((h, w), np.float32)
        for k in range(7):
            yy = h * 0.42 - k * h * 0.13
            rx = w * (0.12 + 0.05 * k)
            phase = i / n * 2 * math.pi + k
            cx = math.sin(phase) * 2
            e = np.hypot((x - cx) / rx, (y - yy) / (rx * 0.32))
            a = np.maximum(a, ring(e, 1, 0.22) * (0.6 + 0.4 * math.cos(phase)))
        frames.append(to_image(a, a))
    return frames


def spikes(size=32, n=6):
    frames = []
    x, y = grid(size, size)
    shards = [(-9, 6, 4, 0.8), (-3, 9, 5, 1.0), (4, 7, 4, 0.9), (9, 4, 3, 0.7), (0, 12, 3, 0.6)]
    for i in range(n):
        t = i / (n - 1)
        grow = min(1, t * 2.2)
        fade = 1 if t < 0.7 else 1 - (t - 0.7) / 0.3
        a = np.zeros((size, size), np.float32)
        b = np.zeros_like(a)
        base = size * 0.38
        for sx, height, half, s in shards:
            hgt = height * grow * 1.3 * s + 1
            inside = (y <= base) & (y >= base - hgt) & (np.abs(x - sx) <= half * (y - (base - hgt)) / hgt)
            a = np.maximum(a, inside * fade)
            b = np.maximum(b, inside * (x > sx))
        frames.append(to_image(a, 0.5 + 0.5 * b))
    return frames


def bloom(size=32, n=6):
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    for i in range(n):
        t = i / (n - 1)
        open_ = min(1, t * 1.6)
        petal = (np.cos(ang * 5 + t) * 0.5 + 0.5) ** 2
        reach = size * (0.12 + 0.3 * open_)
        a = (r < reach * (0.35 + 0.65 * petal)) * (1 - max(0, t - 0.75) * 4)
        core = r < size * 0.08
        frames.append(to_image(np.maximum(a, core), petal))
    return frames


def wave(w=48, h=32, n=6):
    """A breaking wave rolling forward: a curling crest over a body of water."""
    frames = []
    x, y = grid(w, h)
    for i in range(n):
        t = i / (n - 1)
        cx = -w * 0.22 + w * 0.32 * t
        rad = h * 0.42
        r = np.hypot(x - cx, y - h * 0.08)
        ang = np.arctan2(y - h * 0.08, x - cx)
        crest = np.clip(1 - np.abs(r - rad) / 2.2, 0, 1) * (ang < -0.2) * (ang > -2.9)
        dome = (r < rad) & (y <= h * 0.08)
        base = (y > h * 0.08) & (y < h * 0.34) & (np.abs(x - cx) < rad * 1.05)
        body = (dome | base) * np.clip((x - (cx - rad)) / (2 * rad), 0.25, 0.75)
        curl = np.clip(1 - np.hypot(x - (cx + rad * 0.55), y - (h * 0.08 - rad * 0.55)) / 3, 0, 1)
        fade = 1 if t < 0.7 else 1 - (t - 0.7) / 0.3
        a = np.maximum.reduce([crest, body, curl]) * fade
        frames.append(to_image(a, np.maximum(crest, curl)))
    return frames


def shield(size=32, n=6):
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    for i in range(n):
        shimmer = (np.sin((x + y) * 0.7 + i * 1.2) * 0.5 + 0.5)
        edge = ring(r, size * 0.42, 1.4)
        fill = (r < size * 0.42) * 0.25
        glint = (r < size * 0.42) * (shimmer > 0.92) * 0.5
        a = np.maximum.reduce([edge, fill, glint])
        frames.append(to_image(a, np.maximum(edge, glint)))
    return frames


def streak(w=32, h=8, n=5):
    frames = []
    x, y = grid(w, h)
    for i in range(n):
        t = i / (n - 1)
        along = np.clip((x + w / 2) / w, 0, 1)
        a = np.clip(1 - np.abs(y) / (h * 0.45 * along + 0.5), 0, 1) * along * (1 - t)
        frames.append(to_image(a, along))
    return frames


def vanish(size=24, n=6):
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    dots = RNG.uniform(-size / 2, size / 2, (10, 2))
    for i in range(n):
        t = i / (n - 1)
        puff = ring(r, size * (0.1 + 0.3 * t), 2 * (1 - t) + 0.6) * (1 - t)
        a = puff.copy()
        for dx, dy in dots:
            px, py = dx * (0.3 + t), dy * (0.3 + t) - t * 4
            a = np.maximum(a, (np.hypot(x - px, y - py) < 0.9) * (1 - t))
        frames.append(to_image(a))
    return frames


def circle(size=48, n=6):
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    for i in range(n):
        rot = i / n * (2 * math.pi / 6)
        outer = ring(r, size * 0.45, 0.9)
        inner = ring(r, size * 0.33, 0.8)
        runes = ring(r, size * 0.39, 1.8) * ((np.sin((ang + rot) * 12) > 0.55))
        # hexagram
        tri = np.zeros_like(r)
        for k in range(6):
            th = rot + k * math.pi / 3
            d = np.abs(np.cos(th) * x + np.sin(th) * y - size * 0.165)
            tri = np.maximum(tri, (d < 0.7) * (r < size * 0.34))
        a = np.maximum.reduce([outer, inner, runes, tri * 0.8])
        frames.append(to_image(a, np.maximum(outer, runes)))
    return frames


def aura(size=32, n=6):
    frames = []
    x, y = grid(size, size)
    for i in range(n):
        a = np.zeros((size, size), np.float32)
        for k in range(8):
            ph = (i / n + k / 8) % 1
            cx = math.sin(k * 2.4) * size * 0.3
            cy = size * 0.35 - ph * size * 0.7
            rad = size * 0.09 * (1 - ph) + 0.6
            a = np.maximum(a, np.clip(1 - np.hypot(x - cx, (y - cy) * 0.7) / rad, 0, 1) * (1 - ph))
        base = ring(np.hypot(x / 1.0, (y - size * 0.32) * 2.6), size * 0.34, 1.2) * 0.8
        frames.append(to_image(np.maximum(a, base), a))
    return frames


def speedlines(size=256):
    x, y = grid(size, size)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    lines = np.zeros((size, size), np.float32)
    for th in RNG.uniform(-math.pi, math.pi, 90):
        width = RNG.uniform(0.004, 0.02)
        lines = np.maximum(lines, (np.abs(np.angle(np.exp(1j * (ang - th)))) < width).astype(np.float32))
    a = lines * np.clip((r - size * 0.18) / (size * 0.2), 0, 1)
    return [to_image(a)]


def motes(size=32, n=8):
    """Small particles drifting upward (dragon trails, high-tier auras)."""
    frames = []
    x, y = grid(size, size)
    pts = RNG.uniform(-size * 0.4, size * 0.4, (14, 2))
    for i in range(n):
        t = i / n
        a = np.zeros((size, size), np.float32)
        for k, (px, py) in enumerate(pts):
            ph = (t + k / len(pts)) % 1
            cy = size * 0.4 - ph * size * 0.85
            cx = px + math.sin(ph * 6 + k) * 1.5
            a = np.maximum(a, (np.hypot(x - cx, y - cy) < 0.9 + 0.5 * (1 - ph)) * (1 - ph))
        frames.append(to_image(a))
    return frames


def aura_flame(size=32, n=6):
    """Ki-style flames licking upward around the body (tier 2 and tier 5 auras)."""
    frames = []
    x, y = grid(size, size)
    for i in range(n):
        ph = i / n * 2 * math.pi
        width = size * 0.36 * (1 + 0.1 * np.sin(y * 0.5 + ph))
        tongue = np.sin(x * 0.9 + ph * 2) * 0.5 + 0.5
        height = size * (0.25 + 0.2 * tongue)
        body = (np.abs(x) < width) & (y > size * 0.38 - height * 2) & (y < size * 0.42)
        edge = np.clip((width - np.abs(x)) / 3, 0, 1) * np.clip((y - (size * 0.38 - height * 2)) / 6, 0, 1)
        hollow = np.clip(1 - np.hypot(x / (size * 0.22), (y - size * 0.1) / (size * 0.3)), 0, 1)
        a = body * np.clip(edge - hollow * 0.8, 0, 1)
        frames.append(to_image(a, np.clip(edge * 1.4, 0, 1)))
    return frames


def halo(size=32, n=6):
    """A golden ring above the head with orbiting embers (Archons)."""
    frames = []
    x, y = grid(size, size)
    for i in range(n):
        ph = i / n * 2 * math.pi
        e = np.hypot(x / (size * 0.32), (y + size * 0.3) / (size * 0.1))
        a = np.clip(1 - np.abs(e - 1) / 0.25, 0, 1)
        for k in range(3):
            th = ph + k * 2 * math.pi / 3
            cx, cy = math.cos(th) * size * 0.34, math.sin(th) * size * 0.14 + size * 0.05
            a = np.maximum(a, np.clip(1 - np.hypot(x - cx, y - cy) / 1.6, 0, 1))
        frames.append(to_image(a, a))
    return frames


def orbit(size=40, n=8):
    """Three moons orbiting (Lunar powers)."""
    frames = []
    x, y = grid(size, size)
    for i in range(n):
        ph = i / n * 2 * math.pi
        a = np.clip(1 - np.abs(np.hypot(x / (size * 0.42), y / (size * 0.18)) - 1) / 0.08, 0, 1) * 0.35
        for k in range(3):
            th = ph + k * 2 * math.pi / 3
            cx, cy = math.cos(th) * size * 0.42, math.sin(th) * size * 0.18
            moon = np.hypot(x - cx, y - cy) < 2.6
            bite = np.hypot(x - cx - 1.2, y - cy - 0.8) < 2.2
            a = np.maximum(a, (moon & ~bite).astype(np.float32))
        frames.append(to_image(a, a))
    return frames


def radiance(size=48, n=6):
    """Slowly turning rays of light (the Heavenly Principles)."""
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    for i in range(n):
        rot = i / n * (math.pi / 6)
        rays = (np.cos((ang + rot) * 12) * 0.5 + 0.5) ** 8
        a = rays * np.clip(1 - r / (size * 0.48), 0, 1) * np.clip(r / (size * 0.12), 0, 1)
        a = np.maximum(a, np.clip(1 - np.abs(r - size * 0.18) / 1.2, 0, 1) * 0.8)
        frames.append(to_image(a, rays))
    return frames


def crater(size=40, n=1):
    """A scorched crater left on the ground by heavy impacts (tinted dark, fades slowly)."""
    x, y = grid(size, size)
    e = np.hypot(x / (size * 0.46), y / (size * 0.3))
    rim = np.clip(1 - np.abs(e - 0.85) / 0.18, 0, 1)
    pit = np.clip(1 - e / 0.8, 0, 1) * 0.75
    cracks = np.zeros_like(e)
    for th in RNG.uniform(0, 2 * math.pi, 7):
        d = np.abs(-np.sin(th) * x + np.cos(th) * y)
        along = np.cos(th) * x + np.sin(th) * y
        cracks = np.maximum(cracks, (d < 0.7) * (along > size * 0.2) * (along < size * 0.5) * (e < 1.3))
    a = np.maximum.reduce([rim * 0.9, pit, cracks * 0.8])
    return [to_image(a, 1 - pit)]


def smear(size=40, n=5):
    """A thick motion smear: the blur of a fast swing."""
    frames = []
    x, y = grid(size, size)
    r = np.hypot(x, y)
    ang = np.arctan2(y, x)
    for i in range(n):
        t = i / (n - 1)
        a0 = -2.0 + 0.6 * t
        span = 2.8
        within = (ang > a0) & (ang < a0 + span)
        along = np.clip((ang - a0) / span, 0, 1)
        inner, outer = size * 0.14, size * (0.44 - 0.06 * t)
        band = (r > inner + (outer - inner) * (1 - along) * 0.7) & (r < outer)
        a = within * band * along * (1 - t * 0.8)
        frames.append(to_image(a, along))
    return frames


SETS = {
    "slash": slash,
    "slash_big": lambda: slash(48, 7, double=True),
    "hit": hit,
    "spark": spark,
    "burst": burst,
    "nova": nova,
    "orb": orb,
    "beam": beam,
    "pillar": pillar,
    "tornado": tornado,
    "spikes": spikes,
    "bloom": bloom,
    "wave": wave,
    "shield": shield,
    "streak": streak,
    "vanish": vanish,
    "circle": circle,
    "aura": aura,
    "speedlines": speedlines,
    "motes": motes,
    "aura_flame": aura_flame,
    "halo": halo,
    "orbit": orbit,
    "radiance": radiance,
    "crater": crater,
    "smear": smear,
}


def main():
    for name, make in SETS.items():
        frames = make()
        save(name, frames)
        print(f"{name}: {len(frames)} frames {frames[0].size}")


if __name__ == "__main__":
    main()
