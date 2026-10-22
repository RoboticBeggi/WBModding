"""Draws the Dragon Sovereigns' own models (v0.6.1, top-down).

Run from the repo root:  python -I tools/art/gen_dragons.py
Writes GameResources/gi/dragons/<id>/<pose>_NN.png and data/dragons/<id>.json. Needs numpy and Pillow.

WorldBox looks down on its world at a slight angle, so the dragons are drawn the same way: seen from above,
facing right, wings spread to either side. Every part is a smooth shape drawn at 4x and reduced, then shaded
as a rounded form (light from the top-left), quantised to a few tones per material and outlined. Each
Sovereign has its own anatomy: wing type, body build, head, horns, tail and markings.
"""
import json
import math
import os

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "GameResources", "gi", "dragons")
META = os.path.join(ROOT, "data", "dragons")
SS = 4                      # supersampling
W, H = 150, 196             # working canvas (native pixels), body centre at the middle
TAU = 2 * math.pi
LIGHT = np.array([-0.55, -0.65, 0.55])
LIGHT /= np.linalg.norm(LIGHT)
POSES = {"fly": 8, "hover": 6, "perch": 4, "breath": 6, "roar": 6, "claw": 6, "hurt": 2, "death": 6}


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def ramp(*hexes):
    return [hexrgb(h) for h in hexes]


# ------------------------------------------------------------------ canvas

class Canvas:
    """Hi-res layers: coverage, material id and a 0..1 tone (already shaded)."""

    def __init__(self):
        self.h, self.w = H * SS, W * SS
        self.mat = np.full((self.h, self.w), -1, np.int16)
        self.tone = np.zeros((self.h, self.w), np.float32)
        y, x = np.mgrid[0:self.h, 0:self.w].astype(np.float32)
        self.x = (x + 0.5) / SS
        self.y = (y + 0.5) / SS

    def paint(self, mask, mat, tone):
        mask = mask.astype(bool)
        self.mat[mask] = mat
        if np.ndim(tone) == 0:
            self.tone[mask] = tone
        else:
            self.tone[mask] = tone[mask]


def dome_tone(cv, height, base=0.0):
    """Lambert shading from a height field (rounded forms)."""
    gy, gx = np.gradient(height * 6.0)
    n = np.stack([-gx, -gy, np.ones_like(height)], -1)
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    lam = np.clip(n @ LIGHT, 0, 1)
    return np.clip(0.18 + 0.85 * lam + base, 0, 1)


def catmull(pts, n=10):
    pts = [np.array(p, np.float32) for p in pts]
    pts = [pts[0]] + pts + [pts[-1]]
    out = []
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for t in np.linspace(0, 1, n, endpoint=False):
            t2, t3 = t * t, t * t * t
            out.append(0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(pts[-2])
    return np.array(out)


def tube(cv, path, radii):
    """A soft tube along a path: returns (mask, height 0..1)."""
    height = np.zeros(cv.mat.shape, np.float32)
    for (px, py), r in zip(path, radii):
        if r <= 0.05:
            continue
        x0, x1 = int(max(0, (px - r - 1) * SS)), int(min(cv.w, (px + r + 1) * SS))
        y0, y1 = int(max(0, (py - r - 1) * SS)), int(min(cv.h, (py + r + 1) * SS))
        if x0 >= x1 or y0 >= y1:
            continue
        dx = cv.x[y0:y1, x0:x1] - px
        dy = cv.y[y0:y1, x0:x1] - py
        h = 1.0 - (dx * dx + dy * dy) / (r * r)
        np.maximum(height[y0:y1, x0:x1], np.sqrt(np.clip(h, 0, 1)), out=height[y0:y1, x0:x1])
    return height > 0, height


def poly_mask(cv, pts):
    pts = np.asarray(pts, np.float32)
    x, y = cv.x, cv.y
    inside = np.zeros(cv.mat.shape, bool)
    n = len(pts)
    j = n - 1
    for i in range(n):
        xi, yi = pts[i]
        xj, yj = pts[j]
        cond = ((yi > y) != (yj > y)) & (x < (xj - xi) * (y - yi) / (yj - yi + 1e-9) + xi)
        inside ^= cond
        j = i
    return inside


def ellipse_mask(cv, cx, cy, rx, ry, ang=0.0):
    c, s = math.cos(ang), math.sin(ang)
    dx, dy = cv.x - cx, cv.y - cy
    u, v = dx * c + dy * s, -dx * s + dy * c
    d = (u / rx) ** 2 + (v / ry) ** 2
    return d <= 1.0, np.sqrt(np.clip(1 - d, 0, 1))


def seg_dist(cv, a, b):
    ax, ay = a
    bx, by = b
    vx, vy = bx - ax, by - ay
    L2 = vx * vx + vy * vy + 1e-9
    t = np.clip(((cv.x - ax) * vx + (cv.y - ay) * vy) / L2, 0, 1)
    return np.hypot(cv.x - (ax + t * vx), cv.y - (ay + t * vy)), t


def stroke(cv, pts, w0, w1=None):
    """A tapered stroke (horns, bones, claws, spikes)."""
    if w1 is None:
        w1 = w0
    pts = np.asarray(pts, np.float32)
    lens = np.r_[0, np.cumsum(np.hypot(*np.diff(pts, axis=0).T))]
    total = max(lens[-1], 1e-6)
    m = np.zeros(cv.mat.shape, bool)
    for i in range(len(pts) - 1):
        d, t = seg_dist(cv, pts[i], pts[i + 1])
        along = (lens[i] + t * (lens[i + 1] - lens[i])) / total
        m |= d <= (w0 + (w1 - w0) * along) / 2
    return m


# ------------------------------------------------------------------ parts

def membrane(cv, D, mats, root, hip, side, span, sweep, style, fold, seed):
    """One wing seen from above. side = -1 (far, up the screen) or +1 (near, down the screen).

    The arm (shoulder → elbow → wrist) forms the leading edge; finger bones fan from the wrist, from
    pointing outward to pointing back toward the tail; the membrane spans between them and joins the body
    at the hip, with a scalloped (bat), rounded (feather, fin) or faceted (crystal) trailing edge.
    """
    rng = np.random.default_rng(seed)
    sx, sy = root
    L = D["wing"] * span
    back = fold * 0.85 + sweep * 0.4                     # folding sweeps everything back along the body
    elbow = (sx - 3 - 10 * back, sy + side * L * 0.36)
    wrist = (elbow[0] + 6 - 16 * back, elbow[1] + side * L * 0.36)
    n = D.get("fingers", 4)
    tips = []
    for k in range(n):
        a = k / max(1, n - 1)
        ang = math.radians(100 + 95 * a + 20 * back)        # from outward to back toward the tail
        reach = L * (0.6 + 0.12 * a) * (1 - 0.3 * fold)
        tips.append((wrist[0] + math.cos(ang) * reach, wrist[1] + side * math.sin(ang) * reach))
    depth = {"membrane": D.get("scallop", 0.28), "tattered": D.get("scallop", 0.3) + 0.12,
             "crystal": 0.06, "fin": -0.1, "feather": -0.18}[style]
    edge = []
    for k in range(n - 1):
        p0, p1 = np.array(tips[k]), np.array(tips[k + 1])
        mid = (p0 + p1) / 2
        edge.append(tuple(mid + (np.array(wrist) - mid) * depth))
        edge.append(tuple(p1))
    # Last tip back to the hip, gently curved.
    last = np.array(tips[-1])
    mid = (last + np.array(hip)) / 2
    edge.append(tuple(mid + (np.array(wrist) - mid) * max(0.0, depth) * 0.6))
    outline = [root, elbow, wrist, tips[0]] + edge + [hip]
    m = poly_mask(cv, outline)
    if style == "tattered":
        for _ in range(5):
            k = rng.integers(0, n)
            c = np.array(wrist) * 0.35 + np.array(tips[k]) * 0.65
            hole, _ = ellipse_mask(cv, c[0], c[1], rng.uniform(1.2, 2.4), rng.uniform(1, 1.8))
            m &= ~hole
    # Membrane tone: lit near the arm, falling off toward the trailing edge, with panels between fingers.
    d_arm, _ = seg_dist(cv, elbow, wrist)
    d_root, _ = seg_dist(cv, root, elbow)
    tone = np.clip(0.8 - np.minimum(d_arm, d_root) / (L * 0.75), 0.15, 0.8)
    for k in range(n - 1):
        if k % 2 == 1:
            panel = poly_mask(cv, [wrist, tips[k], tips[k + 1]])
            tone = np.where(panel, tone - 0.1, tone)
    if style == "crystal":
        for k in range(n - 1):
            panel = poly_mask(cv, [wrist, tips[k], tips[k + 1]])
            tone = np.where(panel, 0.45 + 0.15 * ((k * 7) % 3), tone)
    cv.paint(m, mats["wing"], tone)
    if style == "feather":
        # Feather vanes: a line per feather along the trailing edge.
        for k in range(n):
            cv.paint(stroke(cv, [wrist, tips[k]], 0.8, 0.5) & m, mats["accent"], 0.9)
        for k in range(10):
            a = k / 9
            base = np.array(root) * (1 - a) + np.array(wrist) * a
            tip = base + np.array([-L * 0.28 * (1 - 0.5 * a), side * L * 0.1])
            cv.paint(stroke(cv, [tuple(base), tuple(tip)], 0.7, 0.3) & m, mats["accent"], 0.75)
    if D.get("wing_trim"):
        trim = np.zeros_like(m)
        pts = [tips[0]] + edge
        for k in range(len(pts) - 1):
            trim |= stroke(cv, [pts[k], pts[k + 1]], 1.2)
        cv.paint(trim & m, mats["trim"], 0.85)
    if style != "feather":
        for t in tips:
            cv.paint(stroke(cv, [wrist, t], 1.7, 0.7), mats["bone"], 0.75)
    cv.paint(stroke(cv, [root, elbow, wrist], 3.6, 2.4), mats["bone"], 0.85)
    if D.get("wrist_claw") or style in ("membrane", "tattered"):
        cv.paint(stroke(cv, [wrist, (wrist[0] + 3.2, wrist[1] - side * 0.6)], 1.5, 0.4), mats["horn"], 0.9)
    return wrist


def legs(cv, D, mats, spine, radii, pose, side, phase):
    """Short limbs seen from above: tucked back in flight, splayed when perched."""
    sets = [("front", D["shoulder_t"]), ("back", D["hip_t"])]
    if D.get("no_forelegs"):
        sets = sets[1:]
    for which, t in sets:
        i = int(t * (len(spine) - 1))
        px, py = spine[i]
        r = radii[i]
        if pose == "perch":
            knee = (px + (2 if which == "front" else -2), py + side * (r + 4))
            foot = (knee[0] + (3 if which == "front" else -1), knee[1] + side * 3)
        elif pose == "claw" and which == "front" and side > 0:
            swing = math.sin(phase * math.pi)
            knee = (px + 4 + 6 * swing, py + side * (r + 2))
            foot = (knee[0] + 4 + 6 * swing, knee[1] + side * (1 - 3 * swing))
        else:
            knee = (px - 3, py + side * (r + 1.5))
            foot = (knee[0] - 4, knee[1] + side * 0.5)
        w = D["leg_w"]
        m = stroke(cv, [(px, py), knee, foot], w, w * 0.6)
        _, h = tube(cv, [(px, py), knee, foot], [w / 2, w / 2.4, w / 3])
        cv.paint(m, mats["body"], dome_tone(cv, h, -0.12))
        for c in (-1, 0, 1):
            cv.paint(stroke(cv, [foot, (foot[0] + 2.2, foot[1] + c * 1.2)], 1.0, 0.3), mats["horn"], 0.85)


def body_and_head(cv, D, mats, P):
    """Spine, body, dorsal ridge, markings, tail ornament, head, horns, eyes."""
    cx, cy = W / 2 + P["dx"], H / 2 + P["dy"]
    sway = P["sway"]
    L = D["length"]
    # Spine from tail tip (left) to snout (right), as fractions of length.
    ctrl = []
    for t, off in D["spine"]:
        x = cx + (t - 0.55) * L
        bend = math.sin(t * math.pi * D.get("waves", 1.0) + sway) * D.get("wiggle", 2.0) * (1 - t) ** 1.2
        ctrl.append((x, cy + off + bend + P["curl"] * (1 - t) ** 2 * 18))
    # Head reach and lift.
    ctrl[-1] = (ctrl[-1][0] + P["neck"], ctrl[-1][1] + P["head_y"])
    ctrl[-2] = (ctrl[-2][0] + P["neck"] * 0.5, ctrl[-2][1] + P["head_y"] * 0.5)
    spine = catmull(ctrl, 14)
    n = len(spine)
    ts = np.linspace(0, 1, n)
    radii = np.interp(ts, *zip(*D["profile"])) * D["girth"] * P["breathe"]
    return spine, radii, ts


def draw_body(cv, D, mats, spine, radii, ts, P):
    m, h = tube(cv, spine[:-int(len(spine) * 0.12)], radii[:-int(len(spine) * 0.12)])
    tone = dome_tone(cv, h)
    cv.paint(m, mats["body"], tone)
    # Back stripe / belly showing at the edge.
    stripe = h > 0.82
    cv.paint(stripe & m, mats["accent"], np.clip(tone + 0.1, 0, 1))
    # Markings.
    rng = np.random.default_rng(D["seed"])
    mark = D.get("marks")
    if mark:
        for _ in range(D.get("mark_n", 14)):
            i = rng.integers(int(len(spine) * 0.1), int(len(spine) * 0.8))
            px, py = spine[i]
            r = radii[i]
            ox, oy = rng.uniform(-r * 0.6, r * 0.6), rng.uniform(-r * 0.6, r * 0.6)
            if mark == "cracks":
                a = rng.uniform(0, TAU)
                pts = [(px + ox, py + oy), (px + ox + math.cos(a) * 2.5, py + oy + math.sin(a) * 2.5),
                       (px + ox + math.cos(a + 0.7) * 4.5, py + oy + math.sin(a + 0.7) * 4.5)]
                cv.paint(stroke(cv, pts, 0.9) & m, mats["glow"], 0.95)
            elif mark == "dots":
                dm, _ = ellipse_mask(cv, px + ox, py + oy, 0.7, 0.7)
                cv.paint(dm & m, mats["glow"], 0.95)
            elif mark == "plates":
                pm, ph = ellipse_mask(cv, px + ox * 0.5, py + oy * 0.5, r * 0.45, r * 0.32, rng.uniform(0, 1))
                cv.paint(pm & m, mats["accent"], dome_tone(cv, ph, -0.05))
            elif mark == "flowers":
                fx, fy = px + ox, py + oy
                for k in range(5):
                    a = k * TAU / 5
                    pm, _ = ellipse_mask(cv, fx + math.cos(a) * 1.1, fy + math.sin(a) * 1.1, 0.9, 0.9)
                    cv.paint(pm & m, mats["trim"], 0.85)
                cm, _ = ellipse_mask(cv, fx, fy, 0.6, 0.6)
                cv.paint(cm & m, mats["glow"], 0.95)
            elif mark == "scales":
                sm, _ = ellipse_mask(cv, px + ox, py + oy, 1.4, 0.9)
                cv.paint(sm & m, mats["accent"], 0.85)
    # Dorsal ridge.
    ridge = D.get("ridge")
    if ridge:
        for i in range(int(len(spine) * ridge[0]), int(len(spine) * ridge[1]), ridge[2]):
            px, py = spine[i]
            ln = ridge[3] * (0.6 + 0.4 * math.sin(i / len(spine) * math.pi))
            cv.paint(stroke(cv, [(px + ln * 0.6, py), (px - ln, py - 0.4)], ridge[4], 0.3), mats["horn"], 0.8)
    # Tail ornament at the tail tip.
    tx, ty = spine[0]
    orn = D.get("tail")
    if orn == "plume":
        for k in range(5):
            a = math.pi + (k - 2) * 0.32
            pts = [(tx, ty), (tx + math.cos(a) * 7, ty + math.sin(a) * 7)]
            cv.paint(stroke(cv, pts, 2.4, 0.6), mats["wing"], 0.6 + 0.08 * k)
    elif orn == "mace":
        mm, mh = ellipse_mask(cv, tx - 1, ty, 3.2, 3.2)
        cv.paint(mm, mats["glow"], dome_tone(cv, mh, 0.05))
    elif orn == "blade":
        cv.paint(poly_mask(cv, [(tx + 2, ty - 1.5), (tx - 8, ty - 3.5), (tx - 3, ty), (tx - 8, ty + 3.5), (tx + 2, ty + 1.5)]),
                 mats["horn"], 0.85)
    elif orn == "flame":
        for k in range(3):
            fm, fh = ellipse_mask(cv, tx - 2 - k * 2.2, ty + (k - 1) * 0.8, 3.2 - k * 0.8, 1.8 - k * 0.4)
            cv.paint(fm, mats["glow"], 0.95 - k * 0.15)
    elif orn == "crystal":
        cv.paint(poly_mask(cv, [(tx + 1, ty - 2), (tx - 7, ty - 0.5), (tx - 9, ty), (tx - 7, ty + 0.5), (tx + 1, ty + 2)]),
                 mats["trim"], 0.9)
    elif orn == "fin":
        cv.paint(poly_mask(cv, [(tx + 2, ty), (tx - 6, ty - 5), (tx - 4, ty), (tx - 6, ty + 5)]), mats["wing"], 0.6)
    elif orn == "barb":
        cv.paint(poly_mask(cv, [(tx + 2, ty - 1), (tx - 6, ty - 4), (tx - 3, ty), (tx - 6, ty + 4), (tx + 2, ty + 1)]),
                 mats["trim"], 0.9)
    return m


def draw_head(cv, D, mats, spine, radii, P):
    """Neck end → skull → snout, horns sweeping back, glowing eyes, open jaw if asked."""
    k = int(len(spine) * 0.88)
    nx, ny = spine[k]
    hx, hy = spine[-1]
    ang = math.atan2(hy - ny, hx - nx)
    hl = D["head"][0]
    hw = D["head"][1]
    c, s = math.cos(ang), math.sin(ang)
    skull = (hx - c * hl * 0.15, hy - s * hl * 0.15)
    # Neck.
    nm, nh = tube(cv, spine[k - 6:], np.linspace(radii[k - 6], hw * 0.55, len(spine) - (k - 6)))
    cv.paint(nm, mats["body"], dome_tone(cv, nh))
    # Horns first (behind the skull).
    for hdef in D.get("horns", []):
        side, length, bend, width = hdef
        base = (skull[0] - c * hl * 0.25 - s * side * hw * 0.5, skull[1] - s * hl * 0.25 + c * side * hw * 0.5)
        pts = [base]
        a = ang + math.pi - side * 0.85
        for j in range(1, 5):
            a += side * bend
            pts.append((pts[-1][0] + math.cos(a) * length / 4, pts[-1][1] + math.sin(a) * length / 4))
        cv.paint(stroke(cv, pts, width, 0.4), mats["horn"], 0.75)
    # Skull and snout as two ellipses; the jaw opens into a V seen from above.
    jaw = P["jaw"]
    sm, sh = ellipse_mask(cv, skull[0], skull[1], hl * 0.42, hw * 0.5, ang)
    cv.paint(sm, mats["body"], dome_tone(cv, sh, 0.05))
    for side in (-1, 1):
        a = ang + side * jaw * 0.45
        sx = skull[0] + math.cos(ang) * hl * 0.35
        sy = skull[1] + math.sin(ang) * hl * 0.35
        snout = (sx + math.cos(a) * hl * 0.32, sy + math.sin(a) * hl * 0.32)
        m2, h2 = ellipse_mask(cv, snout[0], snout[1], hl * 0.36, hw * (0.3 if jaw > 0.05 else 0.36), a)
        cv.paint(m2, mats["body"], dome_tone(cv, h2, 0.08))
    if jaw > 0.05:
        mouth = (skull[0] + c * hl * 0.62, skull[1] + s * hl * 0.62)
        gm, _ = ellipse_mask(cv, mouth[0], mouth[1], hl * 0.28 * jaw + 0.6, hw * 0.16 * jaw + 0.4, ang)
        cv.paint(gm, mats["glow"], 0.95)
    # Brow ridge / crest.
    crest = D.get("crest")
    if crest == "plumes":
        for j in range(4):
            a = ang + math.pi + (j - 1.5) * 0.35
            cv.paint(stroke(cv, [skull, (skull[0] + math.cos(a) * 8, skull[1] + math.sin(a) * 8)], 2.0, 0.5),
                     mats["wing"], 0.7)
    elif crest == "hood":
        for j in range(7):
            a = ang + math.pi / 2 + j * (math.pi / 6)
            lm, lh = ellipse_mask(cv, skull[0] - c * 2 + math.cos(a) * hw * 0.9, skull[1] - s * 2 + math.sin(a) * hw * 0.9,
                                  3.2, 1.6, a)
            cv.paint(lm, mats["wing"], dome_tone(cv, lh, -0.1))
    elif crest == "crown":
        for j in range(7):
            a = ang + math.pi + (j - 3) * 0.32
            cv.paint(stroke(cv, [skull, (skull[0] + math.cos(a) * 7, skull[1] + math.sin(a) * 7)], 1.6, 0.3),
                     mats["trim"], 0.9)
    elif crest == "whiskers":
        for side in (-1, 1):
            base = (skull[0] + c * hl * 0.55, skull[1] + s * hl * 0.55)
            pts = [base]
            a = ang + side * 1.9
            for j in range(4):
                a -= side * 0.35
                pts.append((pts[-1][0] + math.cos(a) * 3, pts[-1][1] + math.sin(a) * 3))
            cv.paint(stroke(cv, pts, 0.9, 0.4), mats["trim"], 0.85)
    # Eyes: one each side of the skull.
    for side in (-1, 1):
        ex = skull[0] + c * hl * 0.12 - s * side * hw * 0.38
        ey = skull[1] + s * hl * 0.12 + c * side * hw * 0.38
        em, _ = ellipse_mask(cv, ex, ey, 1.1, 0.8, ang)
        cv.paint(em, mats["eye"], 1.0)


# ------------------------------------------------------------------ poses

def params(pose, i, n):
    f = i / n
    P = dict(span=1.0, sweep=0.0, fold=0.0, dx=0.0, dy=0.0, sway=0.0, neck=0.0, head_y=0.0, jaw=0.0, curl=0.0,
             breathe=1.0, leg="fly", phase=0.0)
    if pose == "fly":
        a = f * TAU
        P.update(span=0.62 + 0.38 * math.cos(a), sweep=0.35 * max(0, math.sin(a)), dy=-1.6 * math.cos(a), sway=a)
    elif pose == "hover":
        a = f * TAU
        P.update(span=0.8 + 0.18 * math.cos(a), sweep=0.15 * math.sin(a), dy=-0.8 * math.cos(a), sway=a * 0.5)
    elif pose == "perch":
        a = f * TAU
        P.update(span=0.42, fold=1.0, breathe=1.0 + 0.03 * math.sin(a), leg="perch", sway=a * 0.25)
    elif pose == "breath":
        back = [0.0, -3.0, -1.0, 4.0, 5.0, 4.0][i]
        P.update(span=0.95, neck=back, jaw=[0, 0, 0.3, 1.0, 1.0, 0.7][i], dx=-back * 0.2)
    elif pose == "roar":
        P.update(span=1.12, sweep=-0.1, jaw=[0.2, 0.8, 1.0, 1.0, 0.9, 0.5][i], neck=2.0,
                 head_y=[0, -1, 1, -1, 1, 0][i] * 1.2)
    elif pose == "claw":
        P.update(span=0.85, leg="claw", phase=f * 1.2, dx=2.0 * math.sin(f * math.pi), neck=1.5 * math.sin(f * math.pi))
    elif pose == "hurt":
        P.update(span=0.7 + 0.3 * i, dx=-2.5 + i, jaw=0.5, neck=-2.0, head_y=1.0)
    elif pose == "death":
        P.update(span=max(0.25, 0.9 - f * 0.8), fold=f, curl=f * 0.9, neck=-3 * f, head_y=4 * f, jaw=0.4,
                 leg="perch", sweep=0.4 * f)
    return P


def render(D, pose, i, n):
    cv = Canvas()
    mats = D["mats_idx"]
    P = params(pose, i, n)
    spine, radii, ts = body_and_head(cv, D, mats, P)
    root_i = int(D["shoulder_t"] * (len(spine) - 1))
    root = tuple(spine[root_i])
    wing_style = D.get("wings")
    # Far side first (up the screen), then body, then near side.
    hip_i = int(max(0.05, D["hip_t"] if D["hip_t"] > 0 else 0.35) * (len(spine) - 1))
    hip = spine[hip_i]
    if wing_style:
        membrane(cv, D, mats, (root[0], root[1] - radii[root_i] * 0.5), (hip[0], hip[1] - radii[hip_i] * 0.6), -1,
                 P["span"] * 0.92, P["sweep"], wing_style, P["fold"], D["seed"] + 1)
    legs(cv, D, mats, spine, radii, P["leg"], -1, P["phase"])
    draw_body(cv, D, mats, spine, radii, ts, P)
    legs(cv, D, mats, spine, radii, P["leg"], +1, P["phase"])
    draw_head(cv, D, mats, spine, radii, P)
    if wing_style:
        membrane(cv, D, mats, (root[0], root[1] + radii[root_i] * 0.5), (hip[0], hip[1] + radii[hip_i] * 0.6), +1,
                 P["span"], P["sweep"], wing_style, P["fold"], D["seed"] + 2)
    return resolve(cv, D)


def resolve(cv, D):
    """Downsample: a pixel is solid if half its subpixels are; its tone is their mean, mapped to a ramp."""
    hh, ww = H, W
    mat = cv.mat.reshape(hh, SS, ww, SS)
    tone = cv.tone.reshape(hh, SS, ww, SS)
    solid = (mat >= 0).reshape(hh, SS, ww, SS).mean(axis=(1, 3)) >= 0.5
    out = np.zeros((hh, ww, 4), np.uint8)
    mats = D["mats"]
    flat_m = mat.transpose(0, 2, 1, 3).reshape(hh, ww, SS * SS)
    flat_t = tone.transpose(0, 2, 1, 3).reshape(hh, ww, SS * SS)
    for y, x in zip(*np.nonzero(solid)):
        ms = flat_m[y, x]
        valid = ms >= 0
        vals, counts = np.unique(ms[valid], return_counts=True)
        m = vals[np.argmax(counts)]
        t = flat_t[y, x][ms == m].mean()
        r = mats[m]
        out[y, x, :3] = r[min(len(r) - 1, int(t * len(r)))]
        out[y, x, 3] = 255
    # 1px dark outline outside the silhouette.
    a = out[..., 3] > 0
    edge = np.zeros_like(a)
    edge[1:] |= a[:-1]
    edge[:-1] |= a[1:]
    edge[:, 1:] |= a[:, :-1]
    edge[:, :-1] |= a[:, 1:]
    edge &= ~a
    out[edge] = np.r_[D["outline"], 255]
    return out


# ------------------------------------------------------------------ the eight designs

def design(id_, **k):
    base = dict(length=66, girth=8.0, wing=44, wing_root_back=14, fingers=4, scallop=0.28, head=(17, 12), leg_w=3.8,
                shoulder_t=0.62, hip_t=0.36, seed=7, wiggle=2.0, waves=1.0,
                spine=[(0.0, 0), (0.18, 0), (0.36, 0), (0.55, 0), (0.7, 0), (0.86, 0), (1.0, 0)],
                profile=[(0, 0.1), (0.18, 0.3), (0.34, 0.8), (0.48, 0.95), (0.6, 1.0), (0.74, 0.62), (0.86, 0.55), (1, 0.5)])
    base.update(k)
    names = list(base["ramps"].keys())
    base["mats_idx"] = {n: i for i, n in enumerate(names)}
    base["mats"] = [base["ramps"][n] for n in names]
    base["id"] = id_
    return base


DESIGNS = [
    design("anemo", length=78, girth=6.2, wing=44, fingers=7, wings="feather", tail="plume", crest="plumes", wiggle=3.5, waves=1.6,
           ridge=(0.15, 0.8, 5, 2.4, 1.0), marks="dots", mark_n=10, head=(15, 10), seed=11,
           outline=hexrgb("#0C2E2A"),
           ramps=dict(body=ramp("#14564B", "#21806C", "#38AE90", "#6BD7B6", "#B8F4DE"),
                      accent=ramp("#B8F4DE", "#E6FFF5"), wing=ramp("#2A8C78", "#4FC4A4", "#8DE8CC", "#D8FFF0", "#FFFFFF"),
                      bone=ramp("#E6FFF5", "#FFFFFF"), horn=ramp("#C8F0E2", "#FFFFFF"), trim=ramp("#FFFFFF"),
                      glow=ramp("#C8FFF0"), eye=ramp("#FFF6A0"))),
    design("geo", length=64, girth=11.0, wing=28, fingers=3, scallop=0.15, wings="membrane", tail="mace", leg_w=4.6,
           horns=[(-1, 13, 0.32, 2.4), (1, 13, 0.32, 2.4), (-1, 8, -0.4, 1.6), (1, 8, -0.4, 1.6)],
           ridge=(0.2, 0.75, 6, 3.0, 2.0), marks="plates", mark_n=18, head=(18, 14), seed=21, wiggle=1.0,
           profile=[(0, 0.2), (0.2, 0.5), (0.36, 0.95), (0.5, 1.0), (0.62, 1.0), (0.78, 0.6), (0.9, 0.7), (1, 0.55)],
           outline=hexrgb("#2A1A08"),
           ramps=dict(body=ramp("#4A3214", "#6E4D1E", "#97702E", "#C29A48", "#E6C47A"),
                      accent=ramp("#8C6A3A", "#B38D52", "#D8B678"), wing=ramp("#5A4020", "#7E5C30", "#A07C46"),
                      bone=ramp("#C9A564", "#E8CC8A"), horn=ramp("#C08A20", "#F2B53D", "#FFE08A"),
                      trim=ramp("#FFD86B"), glow=ramp("#F2B53D", "#FFD86B", "#FFF0B0"), eye=ramp("#FFE58A"))),
    design("electro", length=70, girth=7.0, wing=46, fingers=4, scallop=0.32, wings="membrane", tail="blade",
           no_forelegs=True, wrist_claw=True, wing_trim=True, horns=[(-1, 11, -0.15, 1.8), (1, 11, -0.15, 1.8)],
           ridge=(0.15, 0.8, 4, 2.6, 1.0), marks="cracks", mark_n=10, head=(18, 11), seed=31,
           outline=hexrgb("#160A2A"),
           ramps=dict(body=ramp("#2A1650", "#3F2378", "#5A35A6", "#7F55D2", "#B48CF5"),
                      accent=ramp("#8F6AE0", "#C8AEFF"), wing=ramp("#2F1A5C", "#45288A", "#6440BA", "#8C66E0"),
                      bone=ramp("#C8B4FF", "#F0E8FF"), horn=ramp("#E8DEFF", "#FFFFFF"), trim=ramp("#D49CFF", "#F0D8FF"),
                      glow=ramp("#E8C8FF", "#FFFFFF"), eye=ramp("#FFFFFF"))),
    design("dendro", length=100, girth=8.0, wings=None, tail="fin", crest="hood", wiggle=7.0, waves=2.4, leg_w=0,
           no_forelegs=True, marks="flowers", mark_n=14, head=(18, 13), seed=41, shoulder_t=0.6, hip_t=-1,
           spine=[(0.0, 6), (0.15, -4), (0.3, 6), (0.45, -4), (0.6, 4), (0.75, -2), (0.88, 0), (1.0, 0)],
           profile=[(0, 0.15), (0.2, 0.55), (0.4, 0.85), (0.6, 1.0), (0.8, 0.8), (0.9, 0.7), (1, 0.6)],
           outline=hexrgb("#0E2410"),
           ramps=dict(body=ramp("#1E4A1A", "#2E6E24", "#45952E", "#6FBF3E", "#A8E066"),
                      accent=ramp("#C8E890", "#EAF8C8"), wing=ramp("#2E7A2A", "#4BA83A", "#7FD45A", "#B8F080"),
                      bone=ramp("#A8E066"), horn=ramp("#E8D8A8", "#FFF4D0"), trim=ramp("#F7B8D8", "#FFE0F0"),
                      glow=ramp("#FFF0A0"), eye=ramp("#E8FF8A"))),
    design("hydro", length=84, girth=7.0, wing=40, fingers=5, scallop=0.08, wings="fin", tail="fin", crest="whiskers",
           wiggle=4.5, waves=1.8, marks="scales", mark_n=22, head=(17, 11), seed=51,
           ridge=(0.15, 0.85, 3, 2.0, 1.4), outline=hexrgb("#08223F"),
           ramps=dict(body=ramp("#103A72", "#1C58A0", "#2E7CC8", "#55A6EC", "#9AD4FF"),
                      accent=ramp("#8ACCFF", "#D0EEFF"), wing=ramp("#1E5EA8", "#3A86D0", "#6CB0F0", "#A8D8FF", "#E0F4FF"),
                      bone=ramp("#C8ECFF", "#FFFFFF"), horn=ramp("#D8F2FF", "#FFFFFF"), trim=ramp("#E8F8FF"),
                      glow=ramp("#BFF0FF"), eye=ramp("#FFFFFF"))),
    design("pyro", length=70, girth=9.5, wing=46, fingers=4, scallop=0.34, wings="membrane", tail="flame", wing_trim=True,
           horns=[(-1, 12, 0.22, 2.4), (1, 12, 0.22, 2.4)], ridge=(0.15, 0.8, 5, 3.0, 1.6), marks="cracks", mark_n=18,
           head=(19, 14), leg_w=4.4, seed=61, outline=hexrgb("#1E0806"),
           ramps=dict(body=ramp("#3E0E0A", "#6A1A10", "#9C2A16", "#CC4A1E", "#F07A32"),
                      accent=ramp("#E8823A", "#FFB060"), wing=ramp("#1E1414", "#2E1E1C", "#46302A", "#5E4036"),
                      bone=ramp("#7A3A22", "#A8552E"), horn=ramp("#2A1E1A", "#4A3A32", "#8A7060"),
                      trim=ramp("#FF8A3A", "#FFC060"), glow=ramp("#FFA040", "#FFD060", "#FFF0A0"), eye=ramp("#FFF0A0"))),
    design("cryo", length=72, girth=7.4, wing=44, fingers=5, scallop=0.12, wings="crystal", tail="crystal",
           horns=[(-1, 10, 0.05, 1.8), (1, 10, 0.05, 1.8), (-1, 6, 0.3, 1.2), (1, 6, 0.3, 1.2)],
           ridge=(0.12, 0.85, 3, 3.4, 1.4), marks="dots", mark_n=16, head=(17, 11.5), seed=71, outline=hexrgb("#1E3A50"),
           ramps=dict(body=ramp("#3E6A88", "#5A8EAE", "#7EB4D0", "#AEDCEE", "#E8FAFF"),
                      accent=ramp("#C8EEFF", "#FFFFFF"), wing=ramp("#7EB8D8", "#A6D8EE", "#CDEFFA", "#F0FCFF"),
                      bone=ramp("#E8FAFF", "#FFFFFF"), horn=ramp("#D8F4FF", "#FFFFFF"), trim=ramp("#FFFFFF"),
                      glow=ramp("#E0FAFF", "#FFFFFF"), eye=ramp("#9EE6F5"))),
    design("nibelung", length=86, girth=11.0, wing=56, fingers=5, scallop=0.3, wings="tattered", tail="barb", wing_trim=True,
           horns=[(-1, 15, 0.25, 2.8), (1, 15, 0.25, 2.8)], crest="crown", ridge=(0.12, 0.85, 4, 3.2, 1.8),
           marks="dots", mark_n=26, head=(21, 15), leg_w=5.0, seed=81, outline=hexrgb("#000000"),
           ramps=dict(body=ramp("#0E0C16", "#1A1626", "#282236", "#3A3250", "#56486E"),
                      accent=ramp("#8A6A28", "#C9A040", "#F0D070"), wing=ramp("#0C0A14", "#16121E", "#221C2E", "#2E2640"),
                      bone=ramp("#C9A040", "#F0D070"), horn=ramp("#A07C2C", "#D9B556", "#FFE58A"),
                      trim=ramp("#C8C8E8", "#F0F0FF"), glow=ramp("#FFE58A", "#FFFFFF"), eye=ramp("#FFD040"))),
]


def main():
    sheet_rows = []
    for D in DESIGNS:
        frames = {p: [render(D, p, i, n) for i in range(n)] for p, n in POSES.items()}
        # One crop for every frame of this dragon, centred on the body.
        alpha = np.zeros((H, W), bool)
        for fs in frames.values():
            for f in fs:
                alpha |= f[..., 3] > 0
        ys, xs = np.nonzero(alpha)
        half_w = int(max(W / 2 - xs.min(), xs.max() - W / 2)) + 2
        half_h = int(max(H / 2 - ys.min(), ys.max() - H / 2)) + 2
        x0, x1 = int(W / 2 - half_w), int(W / 2 + half_w)
        y0, y1 = int(H / 2 - half_h), int(H / 2 + half_h)
        folder = os.path.join(OUT, D["id"])
        os.makedirs(folder, exist_ok=True)
        for f in os.listdir(folder):
            if f.endswith(".png"):
                os.remove(os.path.join(folder, f))
        for p, fs in frames.items():
            for i, f in enumerate(fs):
                Image.fromarray(f[max(0, y0):y1, max(0, x0):x1], "RGBA").save(os.path.join(folder, f"{p}_{i:02d}.png"))
        cw, ch = x1 - x0, y1 - y0
        # Mouth on breath_04: the brightest glow pixel nearest the right edge.
        b4 = frames["breath"][4][max(0, y0):y1, max(0, x0):x1]
        glow = D["mats"][D["mats_idx"]["glow"]][-1]
        hits = np.nonzero(np.all(b4[..., :3] == glow.astype(np.uint8), -1) & (b4[..., 3] > 0))
        if len(hits[0]):
            k = np.argmax(hits[1])
            mouth = [int(hits[1][k]), int(hits[0][k])]
        else:
            mouth = [int(cw * 0.9), int(ch * 0.5)]
        os.makedirs(META, exist_ok=True)
        width_tiles = round(D["length"] * 0.12 * cw / max(1, D["length"] + 10), 2)
        width_tiles = round(cw * 0.085, 2)
        with open(os.path.join(META, D["id"] + ".json"), "w") as fh:
            json.dump({"canvas": [cw, ch], "mouth": mouth, "width_tiles": width_tiles}, fh)
        print(D["id"], (cw, ch), mouth, width_tiles)
        sheet_rows.append([frames[p][i][max(0, y0):y1, max(0, x0):x1] for p, i in
                           (("fly", 0), ("fly", 2), ("fly", 4), ("perch", 0), ("breath", 4), ("roar", 2), ("claw", 3), ("death", 5))])
    return sheet_rows


if __name__ == "__main__":
    rows = main()
    sheet = os.environ.get("DRAGON_SHEET")
    if sheet:
        cell = max(max(r.shape[0], r.shape[1]) for row in rows for r in row) + 4
        img = Image.new("RGBA", (cell * 8, cell * len(rows)), (74, 104, 60, 255))
        for j, row in enumerate(rows):
            for i, f in enumerate(row):
                im = Image.fromarray(f, "RGBA")
                img.alpha_composite(im, (i * cell + (cell - im.width) // 2, j * cell + (cell - im.height) // 2))
        img = img.resize((img.width * 2, img.height * 2), Image.NEAREST)
        img.save(sheet)
