"""Generates the Dragon Sovereign models: eight hand-designed pixel-art dragons, none of them based on the
WorldBox dragon, each with its own silhouette, anatomy and materials, in eight animated poses.

Run from the repo root:  python tools/art/gen_dragons.py
Writes GameResources/gi/dragons/<id>/<pose>_NN.png and data/dragons/<id>.json (canvas, mouth, width). Needs Pillow and numpy.

How a frame is drawn: a skeleton (spine, neck, head, wings, legs) is posed for the frame, then every part
is painted back to front as a shaded volume. Tubes get fake 3D normals lit from the upper left, flat parts
(wings, plates, horns) get facet shading, and every material has its own stepped palette. A 1px outline,
separation lines between overlapping parts and rim highlights finish it. Alpha is 0 or 255 except a few
glow pixels that hug the body.
"""
import json
import math
import os

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "GameResources", "gi", "dragons")
META = os.path.join(ROOT, "data", "dragons")
TAU = 2 * math.pi
LIGHT = np.array([-0.45, -0.75, 0.55])
LIGHT = LIGHT / np.linalg.norm(LIGHT)
BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]], np.float32) / 16.0 - 0.5


def hexrgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], np.float32)


def pal(*stops):
    return [hexrgb(s) for s in stops]


# ------------------------------------------------------------------ canvas

class Canvas:
    def __init__(self, w, h, outline, pad=0):
        # The drawing space is w x h (the design size); the arrays add a margin so nothing ever clips.
        self.lw, self.lh = w, h
        w, h = w + pad * 2, h + pad * 2
        self.w, self.h = w, h
        self.rgb = np.zeros((h, w, 3), np.float32)
        self.a = np.zeros((h, w), np.float32)
        self.part = np.full((h, w), -1, np.int32)
        self.outline = hexrgb(outline)
        self.glow = []  # (mask, rgb, alpha)
        self.next_id = 0
        y, x = np.mgrid[0:h, 0:w].astype(np.float32)
        self.x, self.y = x + 0.5 - pad, y + 0.5 - pad

    def paint(self, mask, shade, palette, sep=True, dither=0.07):
        """Paints a part. shade 0..1 picks a palette tone; edges over other parts get a separation line."""
        if not mask.any():
            return
        pid = self.next_id
        self.next_id += 1
        s = np.clip(shade + BAYER[(np.arange(self.h) % 4)[:, None], (np.arange(self.w) % 4)[None, :]] * dither, 0, 0.999)
        idx = (s * len(palette)).astype(int)
        cols = np.array(palette)[idx]
        if sep:
            edge = mask & ~erode(mask)
            under = edge & (self.a > 0)
        self.rgb[mask] = cols[mask]
        self.a[mask] = 1
        self.part[mask] = pid
        if sep:
            self.rgb[under] = self.rgb[under] * 0.35 + self.outline * 0.65

    def flat(self, mask, rgb):
        self.rgb[mask] = rgb
        self.a[mask] = 1

    def add_glow(self, mask, rgb, alpha=0.6):
        self.glow.append((mask, hexrgb(rgb) if isinstance(rgb, str) else rgb, alpha))

    def finish(self):
        solid = self.a > 0
        # Rim highlight: body pixels whose upper-left neighbour is empty.
        ul = np.zeros_like(solid)
        ul[1:, 1:] = ~solid[:-1, :-1]
        up = np.zeros_like(solid)
        up[1:, :] = ~solid[:-1, :]
        rim = solid & (ul | up) & ~(np.roll(~solid, -1, 0))
        self.rgb[rim] = np.minimum(255, self.rgb[rim] * 1.18 + 18)
        # Outline
        out = dilate(solid) & ~solid
        rgba = np.zeros((self.h, self.w, 4), np.float32)
        rgba[..., :3] = self.rgb
        rgba[..., 3] = self.a * 255
        rgba[out, :3] = self.outline
        rgba[out, 3] = 255
        # Glow pixels just outside the silhouette (alpha >= 0.5)
        filled = rgba[..., 3] > 0
        for mask, rgb, alpha in self.glow:
            ring = dilate(mask) & ~filled
            rgba[ring, :3] = rgb
            rgba[ring, 3] = max(0.5, alpha) * 255
            filled |= ring
        return Image.fromarray(rgba.astype(np.uint8), "RGBA")


def erode(m):
    e = m.copy()
    e[1:, :] &= m[:-1, :]
    e[:-1, :] &= m[1:, :]
    e[:, 1:] &= m[:, :-1]
    e[:, :-1] &= m[:, 1:]
    return e


def dilate(m):
    d = m.copy()
    d[1:, :] |= m[:-1, :]
    d[:-1, :] |= m[1:, :]
    d[:, 1:] |= m[:, :-1]
    d[:, :-1] |= m[:, 1:]
    return d


# ------------------------------------------------------------------ geometry

def catmull(pts, n=12):
    pts = [np.array(p, np.float32) for p in pts]
    if len(pts) < 2:
        return np.array(pts)
    ext = [pts[0] * 2 - pts[1]] + pts + [pts[-1] * 2 - pts[-2]]
    out = []
    for i in range(1, len(ext) - 2):
        p0, p1, p2, p3 = ext[i - 1], ext[i], ext[i + 1], ext[i + 2]
        for k in range(n):
            t = k / n
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    out.append(pts[-1])
    return np.array(out)


def tube(cv, pts, radii, n=10):
    """A shaded tube along a spline. Returns (mask, shade, t along 0..1, side offset (+ = belly), offset y)."""
    path = catmull(pts, n)
    rs = np.interp(np.linspace(0, 1, len(path)), np.linspace(0, 1, len(radii)), radii)
    px, py = cv.x.ravel(), cv.y.ravel()
    best = np.full(px.shape, 1e9, np.float32)
    bi = np.zeros(px.shape, np.int32)
    for i, (c, r) in enumerate(zip(path, rs)):
        d = np.hypot(px - c[0], py - c[1]) / max(r, 0.3)
        upd = d < best
        best[upd] = d[upd]
        bi[upd] = i
    mask = (best <= 1.0).reshape(cv.h, cv.w)
    c = path[bi]
    r = rs[bi]
    ox = (px - c[:, 0]) / np.maximum(r, 0.3)
    oy = (py - c[:, 1]) / np.maximum(r, 0.3)
    l2 = np.clip(ox * ox + oy * oy, 0, 1)
    nz = np.sqrt(1 - l2)
    shade = np.clip(ox * LIGHT[0] + oy * LIGHT[1] + nz * LIGHT[2], 0, 1) * 0.85 + 0.12
    nxt = np.minimum(bi + 1, len(path) - 1)
    prv = np.maximum(bi - 1, 0)
    tang = path[nxt] - path[prv]
    tl = np.maximum(np.hypot(tang[:, 0], tang[:, 1]), 1e-4)
    nrm = np.stack([-tang[:, 1] / tl, tang[:, 0] / tl], 1)
    side = ox * nrm[:, 0] + oy * nrm[:, 1]
    t = bi / max(1, len(path) - 1)
    R = lambda v: v.reshape(cv.h, cv.w)
    return mask, R(shade), R(t), R(side), R(oy), path, rs


def poly(cv, pts):
    pts = np.array(pts, np.float32)
    x, y = cv.x, cv.y
    inside = np.zeros(x.shape, bool)
    n = len(pts)
    for i in range(n):
        x0, y0 = pts[i]
        x1, y1 = pts[(i + 1) % n]
        cond = (y0 > y) != (y1 > y)
        xi = (x1 - x0) * (y - y0) / ((y1 - y0) + 1e-9) + x0
        inside ^= cond & (x < xi)
    return inside


def ellipse(cv, cx, cy, rx, ry, ang=0.0):
    c, s = math.cos(ang), math.sin(ang)
    dx, dy = cv.x - cx, cv.y - cy
    u = (dx * c + dy * s) / rx
    v = (-dx * s + dy * c) / ry
    d = u * u + v * v
    return d <= 1, u, v


def facet(cv, mask, nx, ny, base=0.55):
    """Flat shading for a planar part tilted towards (nx, ny)."""
    k = nx * LIGHT[0] + ny * LIGHT[1]
    return np.full(mask.shape, np.clip(base + k * 0.6, 0.05, 0.95), np.float32)


def rot(p, ang):
    c, s = math.cos(ang), math.sin(ang)
    return (p[0] * c - p[1] * s, p[0] * s + p[1] * c)


def xf(origin, ang, scale, flip_y=1.0):
    """Local-to-canvas transform: x forward, y down."""
    def f(p):
        q = rot((p[0] * scale, p[1] * scale * flip_y), ang)
        return (origin[0] + q[0], origin[1] + q[1])
    return f


def lerp(a, b, t):
    return a + (b - a) * t


def lerp2(a, b, t):
    return (lerp(a[0], b[0], t), lerp(a[1], b[1], t))


# ------------------------------------------------------------------ shared parts

def line_mask(cv, pts, width):
    path = catmull(pts, 8) if len(pts) > 2 else np.array(pts, np.float32)
    m = np.zeros((cv.h, cv.w), bool)
    for a, b in zip(path[:-1], path[1:]):
        ax, ay = a
        bx, by = b
        dx, dy = bx - ax, by - ay
        l2 = dx * dx + dy * dy + 1e-9
        t = np.clip(((cv.x - ax) * dx + (cv.y - ay) * dy) / l2, 0, 1)
        d = np.hypot(cv.x - (ax + t * dx), cv.y - (ay + t * dy))
        m |= d <= width / 2
    return m


def horn(cv, base, ang, length, curve, width, palette, segs=5):
    """A tapered, curving horn from base along ang (radians), bending by curve per segment."""
    pts, a, p = [base], ang, base
    for i in range(segs):
        a += curve
        p = (p[0] + math.cos(a) * length / segs, p[1] + math.sin(a) * length / segs)
        pts.append(p)
    radii = np.linspace(width / 2, 0.35, len(pts))
    m, sh, t, side, oy, _, _ = tube(cv, pts, radii, 6)
    cv.paint(m, sh * 0.9 + t * 0.1, palette)
    return pts[-1]


def spikes_along(cv, path, rs, t0, t1, every, height, palette, lean=-0.5, side=-1, width=0.7, jitter=0.0, seed=0):
    """Triangular plates/spikes on one side of a spine (side -1 = back, +1 = belly)."""
    rng = np.random.default_rng(seed)
    n = len(path)
    i0, i1 = int(t0 * (n - 1)), int(t1 * (n - 1))
    for i in range(i0, i1 + 1, max(1, every)):
        c, r = path[i], rs[i]
        nxt = path[min(i + 1, n - 1)] - path[max(i - 1, 0)]
        tl = max(1e-4, math.hypot(*nxt))
        tx, ty = nxt[0] / tl, nxt[1] / tl
        nx, ny = -ty * side, tx * side
        base = (c[0] + nx * r * 0.7, c[1] + ny * r * 0.7)
        h = height * (0.6 + 0.4 * math.sin(math.pi * (i - i0) / max(1, i1 - i0))) * (1 + jitter * rng.uniform(-1, 1))
        tip = (base[0] + nx * h + tx * lean * h, base[1] + ny * h + ty * lean * h)
        w = max(1.2, r * width)
        tri = [(base[0] - tx * w, base[1] - ty * w), tip, (base[0] + tx * w, base[1] + ty * w)]
        m = poly(cv, tri)
        cv.paint(m, facet(cv, m, -tx, -ty, 0.6), palette)


def leg(cv, root, knee, foot, r0, r1, palette, claw_rgb, claws=3, toes_dir=1.0):
    m, sh, *_ = tube(cv, [root, knee, foot], [r0, (r0 + r1) / 2, r1], 6)
    cv.paint(m, sh * 0.85, palette)
    for k in range(claws):
        a = 0.2 + k * 0.45
        tip = (foot[0] + toes_dir * math.cos(a) * (r1 + 2.2), foot[1] + math.sin(a) * 1.4 + 1.2)
        cm = line_mask(cv, [foot, tip], 1.1)
        cv.flat(cm, claw_rgb)


# ------------------------------------------------------------------ wings

def wing(cv, root, phi, length, style, D, far=False, fold=0.0, spread=1.0, seed=1):
    """
    A wing from root. phi: -1 (down) .. 1 (up). fold 0..1 folds it along the body (perch).
    style: membrane, feather, stone, fin, smoke, ice, tattered, forelimb.
    """
    rng = np.random.default_rng(seed)
    pmem = D["wing_far"] if far else D["wing"]
    pbone = D["bone_far"] if far else D["bone"]
    # Arm: shoulder -> elbow -> wrist, rotating with phi.
    # The whole stroke stays behind the shoulder, so a wing never sweeps forward over the head.
    arm = -100 - 60 * (1 - phi)  # phi 1: straight up · 0: swept back · -1: down and back
    up = math.radians(arm) * (1 - fold) + math.radians(-170) * fold  # angle of the arm
    if far:
        up += math.radians(8)
    L = length * (0.92 if far else 1.0) * (1 - 0.55 * fold)
    elbow = (root[0] + math.cos(up - 0.5) * L * 0.36, root[1] + math.sin(up - 0.5) * L * 0.36)
    wrist = (elbow[0] + math.cos(up + 0.25) * L * 0.34, elbow[1] + math.sin(up + 0.25) * L * 0.34)
    nf = 4 if style not in ("fin", "stone") else 3
    tips = []
    for k in range(nf):
        fa = up + 0.55 - (k * (1.15 / (nf - 1)) * spread) - 0.15 * fold
        fl = L * (0.78 - 0.13 * k) * (1.0 if style != "stone" else 0.7)
        if style == "feather":
            fl *= 1.05
        tips.append((wrist[0] + math.cos(fa + math.pi * 0.08) * fl, wrist[1] + math.sin(fa + math.pi * 0.08) * fl * (1 if phi > -0.6 else 0.8)))
    # Trailing edge back to the body (the root of the membrane sits behind the shoulder)
    body_end = (root[0] - length * 0.42 * (1 - fold * 0.5), root[1] + 3)

    if style in ("membrane", "smoke", "tattered", "fin", "forelimb"):
        edge = [wrist]
        for k, tip in enumerate(tips):
            edge.append(tip)
            nxt = tips[k + 1] if k + 1 < len(tips) else body_end
            mid = lerp2(tip, nxt, 0.5)
            mid = lerp2(mid, wrist, 0.22 if style != "fin" else 0.08)
            edge.append(mid)
        edge.append(body_end)
        edge.append(root)
        m = poly(cv, edge)
        # Membrane shading: brighter near bones, ribbed for fins
        sh = 0.5 + 0.18 * np.sin((cv.x * 0.9 + cv.y * 0.5)) * (1 if style == "fin" else 0.3)
        if style == "tattered":
            for _ in range(7):
                k = rng.integers(0, len(tips))
                c = lerp2(lerp2(tips[k], body_end, rng.uniform(0.2, 0.7)), wrist, rng.uniform(0.0, 0.25))
                hole, *_ = ellipse(cv, c[0], c[1], rng.uniform(1.2, 2.8), rng.uniform(0.8, 2.0), rng.uniform(0, 3))
                m &= ~hole
        cv.paint(m, sh - (0.12 if far else 0), pmem, dither=0.12)
        if style in ("smoke", "tattered"):
            edge_m = m & ~erode(m)
            far_from_root = np.hypot(cv.x - root[0], cv.y - root[1]) > length * 0.4
            cv.flat(edge_m & far_from_root, hexrgb(D["edge"]))
            if not far:
                cv.add_glow(edge_m & far_from_root, D["edge"], 0.55)
        if style == "fin":
            for tip in tips:
                for j in range(1, 4):
                    q = lerp2(wrist, tip, j / 4)
        # Bones
        bm = line_mask(cv, [root, elbow, wrist], 2.6 if not far else 2.0)
        for tip in tips:
            bm |= line_mask(cv, [wrist, tip], 1.3)
        cv.paint(bm, np.full(bm.shape, 0.65), pbone, sep=False)
        # Wrist claw
        claw = line_mask(cv, [wrist, (wrist[0] + 2.5, wrist[1] - 2.5)], 1.2)
        cv.flat(claw, hexrgb(D["claw"]))
    elif style == "feather":
        # Arm, then rows of long feathers fanning from wrist and forearm.
        feathers = []
        for k in range(9):
            t = k / 8
            base = lerp2(wrist, lerp2(elbow, root, 0.6), t)
            tipdir = lerp2(tips[0], body_end, t)
            ang = math.atan2(tipdir[1] - base[1], tipdir[0] - base[0])
            fl = math.hypot(tipdir[0] - base[0], tipdir[1] - base[1]) * (1.05 - 0.25 * t)
            feathers.append((base, ang, fl))
        for base, ang, fl in feathers[::-1]:
            tip = (base[0] + math.cos(ang) * fl, base[1] + math.sin(ang) * fl)
            m, sh, t, side, oy, _, _ = tube(cv, [base, lerp2(base, tip, 0.5), tip], [1.4, 2.3, 0.6], 4)
            cv.paint(m, sh * 0.6 + 0.3 - (0.1 if far else 0), pmem)
            # White tips
            tip_m = m & (np.hypot(cv.x - tip[0], cv.y - tip[1]) < 3.2)
            cv.flat(tip_m, hexrgb(D["edge"]) * (0.8 if far else 1))
        cover = [root, elbow, wrist, lerp2(wrist, tips[-1], 0.35), lerp2(root, body_end, 0.6)]
        m = poly(cv, cover)
        cv.paint(m, np.full(m.shape, 0.55 - (0.1 if far else 0)), pmem)
        bm = line_mask(cv, [root, elbow, wrist], 2.6)
        cv.paint(bm, np.full(bm.shape, 0.7), pbone, sep=False)
    elif style == "stone":
        # Short wings of overlapping stone plates
        plates = []
        for k in range(5):
            t = k / 4
            a = lerp2(root, wrist, 0.3 + 0.7 * (1 - t))
            b = lerp2(tips[min(2, k // 2)], body_end, t * 0.8)
            plates.append((a, b))
        for k, (a, b) in enumerate(plates):
            w = 3.2 - k * 0.3
            ang = math.atan2(b[1] - a[1], b[0] - a[0])
            nx, ny = -math.sin(ang), math.cos(ang)
            q = [(a[0] + nx * w, a[1] + ny * w), (b[0] + nx * w * 0.6, b[1] + ny * w * 0.6), b,
                 (b[0] - nx * w * 0.5, b[1] - ny * w * 0.5), (a[0] - nx * w, a[1] - ny * w)]
            m = poly(cv, q)
            cv.paint(m, facet(cv, m, nx, ny, 0.5 - (0.1 if far else 0)), pmem)
            crack = line_mask(cv, [lerp2(a, b, 0.2), lerp2(a, b, 0.55), (lerp2(a, b, 0.8)[0] + nx, lerp2(a, b, 0.8)[1] + ny)], 0.8)
            cv.flat(crack & m, hexrgb(D["glow"]))
            if not far:
                cv.add_glow(crack & m, D["glow"], 0.5)
        bm = line_mask(cv, [root, elbow, wrist], 3.0)
        cv.paint(bm, np.full(bm.shape, 0.6), pbone, sep=False)
    elif style == "ice":
        # Angular crystal sheets, faceted, jagged trailing edge
        pts = [root, elbow, wrist]
        for k, tip in enumerate(tips):
            pts.append(tip)
            nxt = tips[k + 1] if k + 1 < len(tips) else body_end
            pts.append(lerp2(lerp2(tip, nxt, 0.5), wrist, -0.05))
        pts.append(body_end)
        m = poly(cv, pts)
        # facets: split by lines from wrist to tips
        sh = np.full(m.shape, 0.45)
        for k, tip in enumerate(tips):
            side = (cv.x - wrist[0]) * (tip[1] - wrist[1]) - (cv.y - wrist[1]) * (tip[0] - wrist[0])
            sh = np.where(side > 0, sh + 0.12, sh)
        cv.paint(m, np.clip(sh - (0.12 if far else 0), 0, 1), pmem, dither=0.0)
        for tip in tips:
            fm = line_mask(cv, [wrist, tip], 1.0)
            cv.flat(fm & m, hexrgb(D["edge"]))
        bm = line_mask(cv, [root, elbow, wrist], 2.4)
        cv.paint(bm, np.full(bm.shape, 0.75), pbone, sep=False)
        if not far:
            cv.add_glow(m & ~erode(m) & (cv.y < root[1]), D["glow"], 0.5)
    return wrist, tips


# ------------------------------------------------------------------ head

def head(cv, D, anchor, ang, scale, jaw, eye_on=True):
    """Draws the head; returns the mouth point (canvas coords)."""
    f = xf(anchor, ang, scale)
    hd = D["head"]
    # Lower jaw (rotated open around the hinge)
    hinge = hd["hinge"]
    jaw_pts = [rot((p[0] - hinge[0], p[1] - hinge[1]), jaw) for p in hd["jaw"]]
    jaw_pts = [f((p[0] + hinge[0], p[1] + hinge[1])) for p in jaw_pts]
    if jaw > 0.05:
        # Mouth interior
        upper_under = [f(p) for p in hd["mouth_top"]]
        inside = poly(cv, [f(hinge)] + upper_under[::-1] + [jaw_pts[1]])
        cv.flat(inside, hexrgb(D["mouth"]))
        cv.add_glow(inside, D["breath_glow"], 0.6) if D.get("breath_glow") else None
    jm = poly(cv, jaw_pts)
    cv.paint(jm, np.full(jm.shape, 0.4), D["body"])
    # Skull + snout
    sk = [f(p) for p in hd["skull"]]
    m = poly(cv, sk)
    e, u, v = ellipse(cv, *f(hd["cranium"][:2]), hd["cranium"][2] * scale, hd["cranium"][3] * scale, ang)
    m |= e
    nz = np.sqrt(np.clip(1 - u * u - v * v, 0, 1))
    sh = np.where(e, np.clip(u * LIGHT[0] + v * LIGHT[1] + nz * LIGHT[2], 0, 1) * 0.8 + 0.15, 0.62)
    cv.paint(m, sh, D["body"])
    # Brow ridge
    brow = line_mask(cv, [f(p) for p in hd["brow"]], 1.4 * scale)
    cv.flat(brow & m, D["body"][1])
    # Teeth
    if jaw > 0.05:
        for p in hd["teeth"]:
            q = f(p)
            tm = (np.abs(cv.x - q[0]) < 0.6) & (cv.y > q[1] - 0.2) & (cv.y < q[1] + 1.7)
            cv.flat(tm, hexrgb("#F4F0E6"))
    # Horns, crest, whiskers
    for hspec in hd.get("horns", []):
        base = f(hspec[0])
        horn(cv, base, ang + hspec[1], hspec[2] * scale, hspec[3], hspec[4] * scale, D["horn"], hspec[5] if len(hspec) > 5 else 5)
    if hd.get("extra"):
        hd["extra"](cv, D, f, ang, scale)
    # Eye
    ex, ey = f(hd["eye"])
    em = (np.abs(cv.x - ex) <= 1.2 * max(1, scale * 0.7)) & (np.abs(cv.y - ey) <= 0.8 * max(1, scale * 0.6))
    if eye_on:
        cv.flat(em, hexrgb(D["eye"]))
        cv.add_glow(em, D["eye"], 0.65)
    else:
        cv.flat(em, D["body"][0])
    # Nostril
    nx_, ny_ = f(hd["nostril"])
    nm = (np.abs(cv.x - nx_) < 0.7) & (np.abs(cv.y - ny_) < 0.7)
    cv.flat(nm, D["body"][0])
    mouth = f(hd["mouth"]) if jaw <= 0.05 else lerp2(f(hd["mouth"]), jaw_pts[1], 0.5)
    return mouth


# ------------------------------------------------------------------ poses

POSES = {"fly": 8, "hover": 6, "perch": 4, "breath": 6, "roar": 6, "claw": 6, "hurt": 2, "death": 6}


def pose_params(pose, i, n):
    """Common animation curves for a frame."""
    t = i / n
    p = dict(phi=0.2, bob=0.0, sway=math.sin(TAU * t) * 0.6, neck=0.0, jaw=0.0, fold=0.0, spread=1.0,
             lean=0.0, claw=0.0, ground=False, eye=True, drop=0.0, tilt=0.0, head_fwd=0.0, wing_lag=0.0)
    if pose == "fly":
        p["phi"] = math.cos(TAU * t)
        p["bob"] = math.sin(TAU * t) * 2.0
        p["sway"] = math.sin(TAU * t + 1.2)
        p["wing_lag"] = 0.35
    elif pose == "hover":
        p["phi"] = 0.25 + 0.35 * math.cos(TAU * t)
        p["bob"] = math.sin(TAU * t) * 1.2
        p["sway"] = math.sin(TAU * t) * 0.8
    elif pose == "perch":
        p["fold"] = 1.0
        p["ground"] = True
        p["bob"] = math.sin(TAU * t) * 0.5
        p["neck"] = -0.05 + 0.04 * math.sin(TAU * t)
        p["sway"] = math.sin(TAU * t) * 0.3
    elif pose == "breath":
        k = [0.0, -0.35, -0.5, 0.35, 0.45, 0.4][i]
        p["neck"] = -0.15 + (-0.25 if i < 3 else 0.12)
        p["head_fwd"] = k
        p["jaw"] = [0.0, 0.1, 0.35, 0.7, 0.75, 0.7][i]
        p["phi"] = [0.5, 0.75, 0.85, 0.55, 0.5, 0.55][i]
        p["lean"] = [0, -2, -3, 2.5, 3, 2.5][i]
    elif pose == "roar":
        p["neck"] = [-0.2, -0.45, -0.6, -0.65, -0.6, -0.4][i]
        p["jaw"] = [0.0, 0.3, 0.8, 0.9, 0.85, 0.4][i]
        p["phi"] = [0.4, 0.9, 1.0, 1.0, 0.95, 0.6][i]
        p["spread"] = 1.25
        p["lean"] = [0, -1.5, -2.5, -2.5, -2, -1][i]
    elif pose == "claw":
        p["claw"] = [0.0, -0.6, -1.0, 0.6, 1.0, 0.3][i]
        p["lean"] = [0, -1.5, -2.5, 3, 3.5, 1][i]
        p["phi"] = [0.3, 0.6, 0.8, 0.45, 0.35, 0.3][i]
        p["jaw"] = [0, 0, 0.2, 0.5, 0.4, 0.1][i]
    elif pose == "hurt":
        p["lean"] = -3.5 if i == 0 else -2
        p["neck"] = -0.4
        p["jaw"] = 0.5
        p["phi"] = 0.9 if i == 0 else 0.6
        p["tilt"] = -0.08
    elif pose == "death":
        k = i / (n - 1)
        p["drop"] = k * 22
        p["tilt"] = k * 0.45
        p["phi"] = lerp(0.5, -0.9, k)
        p["neck"] = lerp(-0.3, 0.6, k)
        p["jaw"] = 0.6 if k < 0.6 else 0.3
        p["eye"] = k < 0.75
        p["fold"] = 0.0
    return p


# ------------------------------------------------------------------ assemblers

def winged(cv, D, P):
    """Generic winged dragon: tail, body, legs, neck, head, two wings. Driven by the design D."""
    W, H = cv.lw, cv.lh
    cx = W * 0.47 + P["lean"]
    gy = H * 0.86  # ground line
    by = (gy - D["stand_h"]) if P["ground"] else (H * D["air_y"] + P["bob"])
    by += P["drop"]
    if P["drop"] > 0:
        by = min(by, gy - D["body_r"] * 0.8)
    tilt = P["tilt"]
    tp = lambda x, y: (cx + (x * math.cos(tilt) - y * math.sin(tilt)), by + (x * math.sin(tilt) + y * math.cos(tilt)))

    L = D["body_len"]
    hip = tp(-L * 0.42, 0)
    chest = tp(L * 0.32, -1.5)
    shoulder = tp(L * 0.18, -D["body_r"] * 0.65)
    wroot = (shoulder[0] - 2, shoulder[1] - 1)
    sway = P["sway"]

    # Far wing
    style = D["wing_style"]
    if style != "none":
        wing(cv, (wroot[0] - 4, wroot[1] - 2), P["phi"] + P["wing_lag"] * 0.6, D["wing_len"], style, D, far=True,
             fold=P["fold"], spread=P["spread"])
    # Far legs
    if D.get("legs", 4) >= 4:
        legs_pair(cv, D, P, hip, shoulder, far=True)
    elif D.get("legs") == 2:
        legs_pair(cv, D, P, hip, None, far=True)

    # Tail + body as one spine
    tl = D["tail_len"]
    tail_pts = [tp(-L * 0.42 - tl * k / 4, (k * k) * D["tail_drop"] / 16 + sway * k * 2.2 - (k * 2 if P["ground"] else 0) * 0) for k in range(4, 0, -1)]
    spine = tail_pts + [hip, tp(-L * 0.05, -1), chest]
    radii = [0.6, D["body_r"] * 0.22, D["body_r"] * 0.42, D["body_r"] * 0.7, D["body_r"] * 0.95, D["body_r"], D["body_r"] * 0.92]
    m, sh, t, side, oy, path, rs = tube(cv, spine, radii)
    belly = side > 0.42
    cv.paint(m & ~belly, sh, D["body"])
    cv.paint(m & belly, sh * 0.8 + 0.2, D["belly"], sep=False)
    if D.get("tail_tip"):
        D["tail_tip"](cv, D, path, rs)
    if D.get("back"):
        D["back"](cv, D, path, rs, m, side, t)

    # Near legs
    if D.get("legs", 4) >= 4:
        legs_pair(cv, D, P, hip, shoulder, far=False)
    elif D.get("legs") == 2:
        legs_pair(cv, D, P, hip, None, far=False)

    # Near wing: over the body but under the neck and head, so it never hides the face
    if style != "none":
        wing(cv, wroot, P["phi"], D["wing_len"], style, D, far=False, fold=P["fold"], spread=P["spread"])

    # Neck
    na = D["neck_ang"] + P["neck"]
    nl = D["neck_len"]
    n1 = (chest[0] + math.cos(na) * nl * 0.45, chest[1] + math.sin(na) * nl * 0.45)
    hang = na + 0.55 + P["neck"] * 0.6 - P["tilt"] * 0.5
    fwd = P["head_fwd"] * 5
    n2 = (chest[0] + math.cos(na) * nl + fwd, chest[1] + math.sin(na) * nl + abs(fwd) * 0.2)
    nm, nsh, nt, nside, *_ , npath, nrs = tube(cv, [tp(L * 0.12, -1), chest, n1, n2], [D["body_r"] * 0.85, D["body_r"] * 0.75, D["neck_r"], D["neck_r"] * 0.85])
    nbelly = nside > 0.45
    cv.paint(nm & ~nbelly, nsh, D["body"])
    cv.paint(nm & nbelly, nsh * 0.8 + 0.2, D["belly"], sep=False)
    if D.get("neck_deco"):
        D["neck_deco"](cv, D, npath, nrs)

    # Head
    mouth = head(cv, D, n2, hang * 0.55 + D["head_tilt"], D["head_scale"], P["jaw"], P["eye"])

    return mouth


def legs_pair(cv, D, P, hip, shoulder, far):
    pal_ = D["body_far"] if far else D["body"]
    claw = hexrgb(D["claw"]) * (0.75 if far else 1)
    off = -3 if far else 0
    r = D["leg_r"]
    gy = cv.lh * 0.86
    ground = P["ground"]
    # Hind leg
    if ground:
        foot = (hip[0] + 2 + off, gy)
        knee = (hip[0] + 6 + off, (hip[1] + gy) / 2 - 1)
    else:
        foot = (hip[0] - 7 + off, hip[1] + D["body_r"] + 7)
        knee = (hip[0] + 3 + off, hip[1] + D["body_r"] * 0.9)
    leg(cv, (hip[0] + off, hip[1] + 1), knee, foot, r * 1.35, r * 0.7, pal_, claw)
    if shoulder is None:
        return
    # Fore leg (claw swipe arcs it forward)
    c = P["claw"] if not far else P["claw"] * 0.3
    root = (shoulder[0] + 2 + off, shoulder[1] + D["body_r"] * 0.7)
    if ground and c == 0:
        foot = (root[0] + 3, gy)
        knee = (root[0] + 4, (root[1] + gy) / 2)
    else:
        a = math.radians(70 - 95 * max(0, -c) + 0 * c) if c <= 0 else math.radians(70 - 95 + 120 * c)
        a = math.radians(60) - c * math.radians(80)
        ln = D["body_r"] * 1.6 + 4
        knee = (root[0] + math.cos(a - 0.4) * ln * 0.5, root[1] + math.sin(a - 0.4) * ln * 0.5)
        foot = (root[0] + math.cos(a) * ln, root[1] + math.sin(a) * ln)
    leg(cv, root, knee, foot, r * 1.15, r * 0.6, pal_, claw)
    if c > 0.5 and not far:
        # Claw swipe streak
        for k in range(3):
            s = line_mask(cv, [(foot[0] + 2 + k * 1.5, foot[1] - 6 + k), (foot[0] + 7 + k * 1.5, foot[1] + 2 + k)], 0.9)
            cv.add_glow(s, D["glow"], 0.6)


def serpent(cv, D, P, pose, i, n):
    """Apep: a wingless colossal serpent. Coils on the ground, the forebody raised like a cobra."""
    W, H = cv.lw, cv.lh
    t = i / max(1, n)
    gy = H * 0.86
    rise = 0.0 if P["drop"] == 0 else P["drop"] / 22
    lift = 0 if P["ground"] else -6 - P["bob"]
    wave = TAU * t
    # Coil path on the ground (back half), rising neck at the front.
    base_y = gy - 8 + lift * 0.4 + P["drop"] * 0.25
    coil = []
    for k in range(9):
        a = k / 8
        x = W * 0.15 + a * W * 0.55 + P["lean"] * a
        y = base_y + math.sin(a * 7.5 + wave * (1 if pose in ("fly", "hover") else 0.3)) * 5 * (1 - a * 0.4)
        coil.append((x, y))
    neck_base = coil[-1]
    na = math.radians(-78) + P["neck"] * 0.9 + rise * 1.3
    nl = D["neck_len"] * (1 - rise * 0.5)
    fwd = P["head_fwd"] * 6 + P["claw"] * 8
    n1 = (neck_base[0] + math.cos(na) * nl * 0.5 + fwd * 0.3, neck_base[1] + math.sin(na) * nl * 0.5)
    n2 = (neck_base[0] + math.cos(na + 0.35) * nl + fwd, neck_base[1] + math.sin(na + 0.35) * nl * 0.95)
    # Far coil loop behind
    loop = [(W * 0.30 + math.cos(a) * 14, base_y - 6 + math.sin(a) * 6) for a in np.linspace(0, TAU, 7)]
    m, sh, *_ , path, rs = tube(cv, loop, [D["body_r"] * 0.6] * 7)
    cv.paint(m, sh * 0.7, D["body_far"])
    spine = coil + [n1, n2]
    radii = [1.0, 3, 5, 7, 8.5, 9.5, 10, 10, 9.5, 8, 6.5]
    m, sh, tt, side, oy, path, rs = tube(cv, spine, radii)
    belly = side > 0.35
    cv.paint(m & ~belly, sh, D["body"])
    cv.paint(m & belly, sh * 0.7 + 0.3, D["belly"], sep=False)
    # Banding pattern (rose bands)
    bands = m & ~belly & (np.sin(tt * 60) > 0.6)
    cv.rgb[bands] = cv.rgb[bands] * 0.6 + hexrgb(D["glow"]) * 0.4
    D["back"](cv, D, path, rs, m, side, tt)
    # Hood frill: fan of leaves behind the head
    hood_c = n2
    for k in range(9):
        a = math.radians(-200 + k * 25) + P["neck"] * 0.3
        L = 15 + 4 * math.sin(k * 1.7)
        base = (hood_c[0] + math.cos(a) * 3, hood_c[1] + math.sin(a) * 3)
        tip = (hood_c[0] + math.cos(a) * L, hood_c[1] + math.sin(a) * L)
        lm, lsh, *_ = tube(cv, [base, lerp2(base, tip, 0.55), tip], [1.5, 3.6, 0.5], 4)
        cv.paint(lm, lsh * 0.6 + 0.3, D["wing"])
        vein = line_mask(cv, [base, tip], 0.7) & lm
        cv.flat(vein, D["wing"][0])
    hang = na + 1.35 + P["neck"] * 0.4
    mouth = head(cv, D, n2, hang - math.pi / 2 + 0.25 + P["head_fwd"] * 0.3, D["head_scale"], P["jaw"], P["eye"])
    return mouth


# ------------------------------------------------------------------ the eight designs

def head_wyvern(horns, extra=None, long=1.0):
    return dict(
        skull=[(-3, -5), (6, -6), (14 * long, -3.5), (19 * long, -1), (18.5 * long, 1.5), (5, 2.5), (-3, 3)],
        cranium=(1.5, -1, 5.5, 5),
        hinge=(1, 2),
        jaw=[(1, 2), (17 * long, 2.2), (15 * long, 5), (2, 5)],
        mouth_top=[(3, 2.3), (17.5 * long, 1.6)],
        teeth=[(6 + k * 2.6 * long, 2.4) for k in range(5)],
        brow=[(2, -5), (7, -5.5), (10, -4)],
        eye=(6.5, -3),
        nostril=(17 * long, -1.2),
        mouth=(19 * long, 0.5),
        horns=horns,
        extra=extra,
    )


def d_anemo():
    def back(cv, D, path, rs, m, side, t):
        # Wind plumes along the back: soft white crests
        spikes_along(cv, path, rs, 0.35, 0.95, 4, 4.5, pal("#4FB9A0", "#9AEBD6", "#E8FFF8"), lean=-0.9, width=0.5)
        # Swirl streaks
        sw = m & (np.sin(cv.x * 0.55 + cv.y * 0.9) > 0.85) & (side < 0.2)
        cv.rgb[sw] = cv.rgb[sw] * 0.5 + hexrgb("#D8FFF2") * 0.5

    def tail_tip(cv, D, path, rs):
        tip = path[0]
        for k in range(5):
            a = math.radians(160 + k * 14)
            e = (tip[0] + math.cos(a) * 9, tip[1] + math.sin(a) * 9)
            fm, fsh, *_ = tube(cv, [tuple(tip), lerp2(tuple(tip), e, 0.5), e], [1.0, 2.0, 0.5], 3)
            cv.paint(fm, fsh * 0.5 + 0.4, D["wing"])
            cv.flat(fm & (np.hypot(cv.x - e[0], cv.y - e[1]) < 2.5), hexrgb("#FFFFFF"))

    def crest(cv, D, f, ang, scale):
        for k in range(4):
            base = f((-1 - k * 1.5, -4 + k * 0.6))
            a = ang + math.radians(-160 - k * 12)
            e = (base[0] + math.cos(a) * (9 - k), base[1] + math.sin(a) * (9 - k))
            fm, fsh, *_ = tube(cv, [base, lerp2(base, e, 0.5), e], [1.0, 1.9, 0.4], 3)
            cv.paint(fm, fsh * 0.5 + 0.45, D["wing"])
        # whiskers
        for k in (0, 1):
            w = line_mask(cv, [f((15, 2)), f((19, 5 + k * 2)), f((24, 4 + k * 3))], 0.8)
            cv.flat(w, hexrgb("#E8FFF8"))

    return dict(
        id="anemo", size=(128, 96), outline="#0B2A2A", wing_style="feather", legs=4,
        body=pal("#13564C", "#1F7A68", "#33A58A", "#5ACCAE", "#9AEBD6"),
        body_far=pal("#0E3E37", "#165A4D", "#247866", "#3F9B84", "#6FC0AA"),
        belly=pal("#A9D9C8", "#CFEFE2", "#EFFFF8", "#FFFFFF"),
        wing=pal("#3E9C88", "#6BC9B2", "#A7EBDA", "#E2FFF6"),
        wing_far=pal("#2B6F61", "#4A9886", "#77BFAE", "#A8DDD0"),
        bone=pal("#2A7A68", "#4EA690", "#8AD8C2"), bone_far=pal("#1E5A4D", "#357D6C", "#5FA894"),
        horn=pal("#6E8F86", "#B8D6CC", "#F2FFF9"),
        claw="#EAF7F2", edge="#FFFFFF", glow="#B8FFE8", eye="#F8FFB0", mouth="#3B1A2A", breath_glow="#B8FFE8",
        body_len=46, body_r=7.0, tail_len=44, tail_drop=-6, neck_len=24, neck_r=4.2, neck_ang=math.radians(-48),
        head_tilt=0.15, head_scale=0.95, wing_len=50, leg_r=2.0, air_y=0.55, stand_h=15,
        head=head_wyvern([((0, -4), math.radians(-150), 9, 0.05, 2.0)], crest, 1.08),
        back=back, tail_tip=tail_tip,
    )


def d_geo():
    def back(cv, D, path, rs, m, side, t):
        # Heavy overlapping stone plates along the back, and glowing gold veins across the body
        spikes_along(cv, path, rs, 0.3, 0.98, 5, 6.5, D["horn"], lean=-0.2, width=1.3, jitter=0.25, seed=3)
        rng = np.random.default_rng(7)
        for k in range(7):
            i = rng.integers(len(path) // 3, len(path) - 2)
            c = path[i]
            pts = [tuple(c)]
            for _ in range(4):
                c = c + rng.normal(0, 2.2, 2)
                pts.append(tuple(c))
            v = line_mask(cv, pts, 0.9) & m
            cv.flat(v, hexrgb("#FFD86B"))
            cv.add_glow(v & (side < -0.3), "#FFC94A", 0.5)

    def tail_tip(cv, D, path, rs):
        c = path[2]
        club, u, v = ellipse(cv, c[0], c[1], 5, 4)
        cv.paint(club, np.clip(0.6 - v * 0.3, 0, 1), D["horn"])

    def antlers(cv, D, f, ang, scale):
        # Branching amber antler-horns (Azhdaha)
        for side_ in (0, 1):
            base = f((-1 + side_ * 2, -5))
            a0 = ang + math.radians(-150 + side_ * 25)
            tip = horn(cv, base, a0, 15 * scale, 0.12, 2.6 * scale, D["horn"], 5)
            mid = lerp2(base, tip, 0.5)
            horn(cv, mid, a0 + 0.9, 7 * scale, 0.1, 1.6 * scale, D["horn"], 3)
            horn(cv, lerp2(base, tip, 0.78), a0 - 0.7, 5 * scale, -0.1, 1.3 * scale, D["horn"], 3)

    return dict(
        id="geo", size=(128, 96), outline="#1E1206", wing_style="stone", legs=4,
        body=pal("#3A2610", "#5E3F1C", "#86602C", "#B0873E", "#D9B464"),
        body_far=pal("#2A1B0B", "#432E14", "#614520", "#82632E", "#A6874A"),
        belly=pal("#8C6A2E", "#B88E3E", "#DDB560", "#F6D98A"),
        wing=pal("#4A3418", "#6E5226", "#937238", "#BC9A55"),
        wing_far=pal("#352511", "#4F3B1C", "#6C5329", "#8C713E"),
        bone=pal("#3A2610", "#5E3F1C", "#86602C"), bone_far=pal("#2A1B0B", "#432E14", "#614520"),
        horn=pal("#8A5A14", "#C88A26", "#F2B53D", "#FFE08A"),
        claw="#F2E2B8", edge="#F2B53D", glow="#FFD86B", eye="#FFE58A", mouth="#3A1206", breath_glow="#FFC94A",
        body_len=50, body_r=11.5, tail_len=30, tail_drop=10, neck_len=18, neck_r=6.5, neck_ang=math.radians(-25),
        head_tilt=0.2, head_scale=1.15, wing_len=30, leg_r=3.2, air_y=0.58, stand_h=20,
        head=head_wyvern([], antlers, 0.95),
        back=back, tail_tip=tail_tip,
    )


def d_electro():
    def back(cv, D, path, rs, m, side, t):
        # Crackling violet spine plates, jagged
        spikes_along(cv, path, rs, 0.15, 0.98, 3, 5.5, pal("#4A2280", "#8A4FE0", "#D7B8FF"), lean=-0.7, width=0.6, jitter=0.4, seed=5)
        rng = np.random.default_rng(11)
        for k in range(3):
            i = rng.integers(5, len(path) - 5)
            c = path[i]
            pts = [tuple(c + np.array([0, -rs[i] * 0.8]))]
            for _ in range(5):
                c = c + np.array([rng.uniform(1, 3), rng.uniform(-2, 2)])
                pts.append(tuple(c + np.array([0, -rs[i] * 0.6])))
            v = line_mask(cv, pts, 0.8) & m
            cv.flat(v, hexrgb("#F2E6FF"))
            cv.add_glow(v, "#C79BFF", 0.55)

    def tail_tip(cv, D, path, rs):
        tip = path[0]
        prev = path[6]
        d = tip - prev
        a = math.atan2(d[1], d[0])
        blade = [tuple(tip + np.array([math.cos(a + 1.6) * 3, math.sin(a + 1.6) * 3])),
                 tuple(tip + np.array([math.cos(a) * 12, math.sin(a) * 12])),
                 tuple(tip + np.array([math.cos(a - 1.2) * 5, math.sin(a - 1.2) * 5])),
                 tuple(tip + np.array([math.cos(a + 0.4) * 5, math.sin(a + 0.4) * 5]))]
        bm = poly(cv, blade)
        cv.paint(bm, facet(cv, bm, 0, -1, 0.6), D["horn"])
        cv.add_glow(bm, "#C79BFF", 0.5)

    return dict(
        id="electro", size=(128, 96), outline="#120826", wing_style="forelimb", legs=2,
        body=pal("#1E0F3A", "#33195E", "#4F2C8A", "#7448BE", "#A47CE8"),
        body_far=pal("#160A2B", "#251246", "#3A2066", "#53348E", "#7656B4"),
        belly=pal("#3D2A66", "#5E4596", "#8D72C8", "#C2AEF0"),
        wing=pal("#2A1450", "#40207A", "#5E36A6", "#8C64D4"),
        wing_far=pal("#1E0E3A", "#2E1758", "#43267A", "#6044A0"),
        bone=pal("#2A1450", "#5A34A0", "#9C74E6"), bone_far=pal("#1E0E3A", "#3E2470", "#6A4CA8"),
        horn=pal("#3A1E66", "#7A4CC8", "#C9A6FF", "#F4EAFF"),
        claw="#E9DDFF", edge="#C79BFF", glow="#D49CFF", eye="#F8F0FF", mouth="#2A0A30", breath_glow="#E2C2FF",
        body_len=44, body_r=8.0, tail_len=40, tail_drop=2, neck_len=20, neck_r=4.6, neck_ang=math.radians(-40),
        head_tilt=0.1, head_scale=1.0, wing_len=56, leg_r=2.4, air_y=0.6, stand_h=17,
        head=head_wyvern([((-1, -4), math.radians(-165), 12, 0.04, 2.2, 4),
                          ((1, -5), math.radians(-130), 9, -0.08, 1.8, 4),
                          ((3, -5), math.radians(-105), 6, 0.0, 1.4, 3)], None, 1.12),
        back=back, tail_tip=tail_tip,
    )


def d_dendro():
    def back(cv, D, path, rs, m, side, t):
        rng = np.random.default_rng(4)
        # Vines and small flowers along the back of the coils
        for i in range(4, len(path) - 6, 7):
            c = path[i]
            r = rs[i]
            p = (c[0] + rng.uniform(-2, 2), c[1] - r * 0.8)
            leaf_tip = (p[0] + rng.uniform(-4, 4), p[1] - rng.uniform(3, 6))
            lm, lsh, *_ = tube(cv, [p, lerp2(p, leaf_tip, 0.5), leaf_tip], [0.8, 2.0, 0.4], 3)
            cv.paint(lm, lsh * 0.5 + 0.35, D["wing"])
            if rng.uniform() < 0.45:
                fl, *_ = ellipse(cv, p[0] + 1, p[1] - 1, 1.6, 1.6)
                cv.flat(fl, hexrgb("#FF9EC8"))
                cv.flat((np.abs(cv.x - p[0] - 1) < 0.6) & (np.abs(cv.y - p[1] + 1) < 0.6), hexrgb("#FFF2A8"))

    return dict(
        id="dendro", size=(128, 96), outline="#0C1E0A", wing_style="none", legs=0,
        body=pal("#163A12", "#25591C", "#3D8028", "#62A83C", "#9BD55E"),
        body_far=pal("#0F2A0C", "#1A4214", "#2C5E1E", "#47802E", "#6FA548"),
        belly=pal("#A8B86A", "#CCD88A", "#E8F2B0", "#FFFFD8"),
        wing=pal("#2E6E22", "#4C9A30", "#7CC448", "#B6E87A"),
        wing_far=pal("#1E4E16", "#34702A", "#529A38", "#7ABE58"),
        bone=pal("#2E6E22", "#4C9A30", "#7CC448"), bone_far=pal("#1E4E16", "#34702A", "#529A38"),
        horn=pal("#6E3A4E", "#B0607E", "#E89AB8", "#FFD6E6"),
        claw="#F0E8D0", edge="#FF9EC8", glow="#E07AA8", eye="#FFE07A", mouth="#3A0E1E", breath_glow="#B6FF7A",
        body_r=10, neck_len=42, head_scale=1.15,
        head=head_wyvern([((0, -4), math.radians(-170), 8, 0.12, 2.2, 4), ((2, -4.5), math.radians(-150), 6, 0.1, 1.6, 3)], None, 1.0),
        back=back,
    )


def d_hydro():
    def back(cv, D, path, rs, m, side, t):
        # Wave-crest mane: flowing curls along the neck/back; scale shimmer
        spikes_along(cv, path, rs, 0.5, 0.98, 3, 5, pal("#2A7FD0", "#7FD0FF", "#E8F8FF"), lean=-1.1, width=0.6, seed=2)
        sh = m & (((cv.x + cv.y * 0.5) % 4) < 1) & (((cv.x * 0.5 - cv.y) % 4) < 1) & (side < 0.3)
        cv.rgb[sh] = np.minimum(255, cv.rgb[sh] * 1.25 + 10)

    def tail_tip(cv, D, path, rs):
        tip = path[0]
        fan = [tuple(tip), (tip[0] - 12, tip[1] - 8), (tip[0] - 9, tip[1]), (tip[0] - 12, tip[1] + 8)]
        fm = poly(cv, fan)
        sh = 0.5 + 0.2 * np.sin(cv.y * 1.6)
        cv.paint(fm, sh, D["wing"], dither=0.0)

    def whiskers(cv, D, f, ang, scale):
        for k, (dy, ln) in enumerate(((1, 16), (3, 13))):
            pts = [f((16, dy)), f((21, dy + 4)), f((25, dy + 2)), f((29, dy + 6))]
            w = line_mask(cv, pts[: 2 + (1 if ln > 14 else 2)], 0.9)
            cv.flat(w, hexrgb("#BFEAFF"))
        for k in range(3):
            base = f((-2 - k * 2, -3 + k))
            a = ang + math.radians(-170 - k * 10)
            e = (base[0] + math.cos(a) * 11, base[1] + math.sin(a) * 11 + 3)
            fm, fsh, *_ = tube(cv, [base, lerp2(base, e, 0.5), e], [1.2, 2.2, 0.4], 3)
            cv.paint(fm, fsh * 0.5 + 0.4, D["wing"])

    return dict(
        id="hydro", size=(128, 96), outline="#06183A", wing_style="fin", legs=2,
        body=pal("#0E2E66", "#18468F", "#2766B8", "#4A92DC", "#86C4F4"),
        body_far=pal("#0A2250", "#12346E", "#1C4E92", "#3470B4", "#5E98D2"),
        belly=pal("#7FBFE6", "#A8DAF4", "#D2F0FF", "#F4FCFF"),
        wing=pal("#2C78C8", "#55A4E8", "#8CCCF8", "#D2F0FF"),
        wing_far=pal("#1E5A9C", "#3A80C4", "#64A8DE", "#98CCEE"),
        bone=pal("#1A4E92", "#3C82CC", "#7CC0F2"), bone_far=pal("#123A70", "#2C64A4", "#5498D0"),
        horn=pal("#3A6E9C", "#7FB4DC", "#C6E6FA", "#FFFFFF"),
        claw="#E6F6FF", edge="#D2F0FF", glow="#7FD0FF", eye="#F0FFFF", mouth="#1A0A2A", breath_glow="#7FD0FF",
        body_len=48, body_r=7.5, tail_len=48, tail_drop=-2, neck_len=26, neck_r=4.4, neck_ang=math.radians(-42),
        head_tilt=0.12, head_scale=1.0, wing_len=40, leg_r=2.2, air_y=0.55, stand_h=14,
        head=head_wyvern([((0, -4), math.radians(-160), 10, 0.1, 1.8, 4)], whiskers, 1.05),
        back=back, tail_tip=tail_tip,
    )


def d_pyro():
    def back(cv, D, path, rs, m, side, t):
        spikes_along(cv, path, rs, 0.25, 0.95, 4, 5.5, D["horn"], lean=-0.4, width=0.9, seed=8)
        # Molten cracks glowing on chest and belly
        rng = np.random.default_rng(13)
        bellyish = side > 0.05
        for k in range(10):
            i = rng.integers(len(path) // 2, len(path) - 1)
            c = path[i] + np.array([0, rs[i] * 0.35])
            pts = [tuple(c)]
            for _ in range(3):
                c = c + rng.normal(0, 2.0, 2)
                pts.append(tuple(c))
            v = line_mask(cv, pts, 1.0) & m & bellyish
            cv.flat(v, hexrgb("#FFB43A"))
            cv.add_glow(v, "#FF7A1A", 0.55)

    def tail_tip(cv, D, path, rs):
        tip = path[0]
        flame, u, v = ellipse(cv, tip[0] - 2, tip[1] - 3, 2.5, 4.5, 0.4)
        cv.flat(flame, hexrgb("#FFB43A"))
        core, *_ = ellipse(cv, tip[0] - 2, tip[1] - 2, 1.2, 2.2, 0.4)
        cv.flat(core, hexrgb("#FFF2B0"))
        cv.add_glow(flame, "#FF7A1A", 0.6)

    return dict(
        id="pyro", size=(128, 96), outline="#1A0604", wing_style="smoke", legs=4,
        body=pal("#2A0A08", "#4A1410", "#701E14", "#9C3018", "#C8502A"),
        body_far=pal("#1E0706", "#350E0B", "#52160F", "#722414", "#943C20"),
        belly=pal("#5A1A0E", "#8A3414", "#C0581E", "#E8862E"),
        wing=pal("#1C1414", "#2E2220", "#44322C", "#5E4438"),
        wing_far=pal("#140E0E", "#221816", "#33251F", "#47332A"),
        bone=pal("#3A1410", "#62261A", "#8E3E26"), bone_far=pal("#2A0E0C", "#461C14", "#682E1E"),
        horn=pal("#3A2620", "#6E4A38", "#A87A5A", "#E2BC92"),
        claw="#F0D8B8", edge="#FF8A3A", glow="#FFB43A", eye="#FFF06A", mouth="#5A0A04", breath_glow="#FF8A3A",
        body_len=52, body_r=10.5, tail_len=36, tail_drop=6, neck_len=19, neck_r=6.0, neck_ang=math.radians(-35),
        head_tilt=0.18, head_scale=1.12, wing_len=54, leg_r=3.0, air_y=0.58, stand_h=19,
        head=head_wyvern([((-1, -4), math.radians(-175), 13, 0.18, 2.8, 5),
                          ((2, -5), math.radians(-140), 8, 0.15, 2.0, 4)], None, 1.0),
        back=back, tail_tip=tail_tip,
    )


def d_cryo():
    def back(cv, D, path, rs, m, side, t):
        # Crystalline ice-shard spikes, tall and sharp
        spikes_along(cv, path, rs, 0.12, 0.97, 3, 8, pal("#7FB8D8", "#C8EEFF", "#FFFFFF"), lean=-0.3, width=0.55, jitter=0.5, seed=9)
        fac = m & (((cv.x - cv.y) % 6) < 1.0) & (side < 0.3)
        cv.rgb[fac] = np.minimum(255, cv.rgb[fac] * 1.2 + 15)

    def tail_tip(cv, D, path, rs):
        tip = path[0]
        for a, ln in ((-2.6, 10), (-3.0, 8), (2.8, 7)):
            e = (tip[0] + math.cos(a) * ln, tip[1] + math.sin(a) * ln)
            sm = poly(cv, [(tip[0], tip[1] - 1.5), e, (tip[0], tip[1] + 1.5)])
            cv.paint(sm, facet(cv, sm, 0, -1, 0.65), D["horn"])

    return dict(
        id="cryo", size=(128, 96), outline="#0E2234", wing_style="ice", legs=4,
        body=pal("#2E4E66", "#4A7896", "#74A6C4", "#A8D6EC", "#E4F8FF"),
        body_far=pal("#223A4E", "#365A72", "#56809C", "#7EA8C2", "#AACCDE"),
        belly=pal("#B8D8E8", "#D8EEF8", "#F2FCFF", "#FFFFFF"),
        wing=pal("#6EA8CC", "#A2D4F0", "#D2F0FF", "#F8FFFF"),
        wing_far=pal("#4E84A8", "#7AAECE", "#A6D0E8", "#CCE8F4"),
        bone=pal("#4A7896", "#86B8D6", "#D6F2FF"), bone_far=pal("#365A72", "#62909E", "#9CC4D8"),
        horn=pal("#7FB8D8", "#B8E2F6", "#E8FAFF", "#FFFFFF"),
        claw="#F4FCFF", edge="#FFFFFF", glow="#CFF6FF", eye="#E0F8FF", mouth="#1A2A3A", breath_glow="#CFF6FF",
        body_len=46, body_r=8.0, tail_len=42, tail_drop=0, neck_len=23, neck_r=4.8, neck_ang=math.radians(-45),
        head_tilt=0.1, head_scale=1.02, wing_len=50, leg_r=2.4, air_y=0.56, stand_h=16,
        head=head_wyvern([((-1, -4), math.radians(-160), 12, 0.0, 1.8, 3),
                          ((1, -5), math.radians(-120), 8, -0.05, 1.5, 3),
                          ((-3, -2), math.radians(-185), 7, 0.0, 1.4, 3)], None, 1.08),
        back=back, tail_tip=tail_tip,
    )


def d_nibelung():
    def back(cv, D, path, rs, m, side, t):
        spikes_along(cv, path, rs, 0.15, 0.98, 4, 7, pal("#3A2A10", "#8A6A20", "#E2B84A", "#FFE58A"), lean=-0.5, width=0.8, seed=12)
        # Gold scale trim along the flank, moon-silver glints
        trim = m & (np.abs(side - 0.35) < 0.08)
        cv.rgb[trim] = hexrgb("#C9A23A")
        glints = m & (((cv.x * 3 + cv.y * 7) % 23) < 0.9) & (side < 0.2)
        cv.flat(glints, hexrgb("#E6E0FF"))

    def tail_tip(cv, D, path, rs):
        tip = path[0]
        for a, ln in ((-2.7, 9), (2.9, 8)):
            e = (tip[0] + math.cos(a) * ln, tip[1] + math.sin(a) * ln)
            sm = poly(cv, [(tip[0], tip[1] - 1.5), e, (tip[0], tip[1] + 1.5)])
            cv.paint(sm, facet(cv, sm, 0, -1, 0.65), D["horn"])

    def crown(cv, D, f, ang, scale):
        # A crown of many golden horns
        for k in range(7):
            base = f((-3 + k * 1.2, -5 + abs(k - 3) * 0.3))
            a = ang + math.radians(-175 + k * 18)
            horn(cv, base, a, (8 + (3 - abs(k - 3)) * 2.5) * scale, -0.06 if k < 3 else 0.06, 1.7 * scale, D["horn"], 4)

    return dict(
        id="nibelung", size=(160, 112), outline="#05040A", wing_style="tattered", legs=4,
        body=pal("#07060C", "#121018", "#1E1A28", "#2E283E", "#463E5C"),
        body_far=pal("#050409", "#0C0B12", "#16131E", "#221E2E", "#332D46"),
        belly=pal("#3A2E14", "#6A5420", "#A88A34", "#E2C060"),
        wing=pal("#0A0910", "#16131E", "#241F32", "#373048"),
        wing_far=pal("#07060C", "#100E16", "#1A1724", "#282336"),
        bone=pal("#2A2010", "#6A5420", "#C9A23A"), bone_far=pal("#1E180C", "#4A3A16", "#8A7028"),
        horn=pal("#6A4E14", "#B08A2A", "#E8C24A", "#FFF0A0"),
        claw="#FFE58A", edge="#E6E0FF", glow="#FFD86B", eye="#FFD84A", mouth="#3A0A10", breath_glow="#E6E0FF",
        body_len=64, body_r=12.5, tail_len=54, tail_drop=4, neck_len=28, neck_r=7.0, neck_ang=math.radians(-40),
        head_tilt=0.12, head_scale=1.35, wing_len=72, leg_r=3.4, air_y=0.56, stand_h=22,
        head=head_wyvern([], crown, 1.05),
        back=back, tail_tip=tail_tip,
    )


DESIGNS = [d_anemo, d_geo, d_electro, d_dendro, d_hydro, d_pyro, d_cryo, d_nibelung]


PAD = 48  # every frame is drawn on an oversized canvas, then all frames of a dragon share one crop


def render(D, pose, i, n):
    w, h = D["size"]
    cv = Canvas(w, h, D["outline"], PAD)
    P = pose_params(pose, i, n)
    if D["id"] == "dendro":
        mouth = serpent(cv, D, P, pose, i, n)
    else:
        mouth = winged(cv, D, P)
    return cv.finish(), (mouth[0] + PAD, mouth[1] + PAD)


def anchor_of(D):
    """The body's resting point on the oversized canvas: the crop is centred on it."""
    w, h = D["size"]
    return PAD + w * 0.47, PAD + h * D.get("air_y", 0.55)


def main():
    total = 0
    for make in DESIGNS:
        D = make()
        folder = os.path.join(OUT, D["id"])
        os.makedirs(folder, exist_ok=True)
        for f in os.listdir(folder):
            if f.endswith(".png"):
                os.remove(os.path.join(folder, f))
        frames = []
        mouth4 = None
        for pose, n in POSES.items():
            for i in range(n):
                img, mouth = render(D, pose, i, n)
                frames.append((pose, i, img))
                if pose == "breath" and i == 4:
                    mouth4 = mouth
        # One shared crop, symmetric around the body so the sprite's centre stays on the unit.
        ax, ay = anchor_of(D)
        x0, y0, x1, y1 = ax, ay, ax, ay
        for _, _, img in frames:
            bb = img.getbbox()
            if bb:
                x0, y0, x1, y1 = min(x0, bb[0]), min(y0, bb[1]), max(x1, bb[2]), max(y1, bb[3])
        hw = int(math.ceil(max(ax - x0, x1 - ax))) + 2
        hh = int(math.ceil(max(ay - y0, y1 - ay))) + 2
        box = (int(round(ax - hw)), int(round(ay - hh)), int(round(ax + hw)), int(round(ay + hh)))
        for pose, i, img in frames:
            img.crop(box).save(os.path.join(folder, f"{pose}_{i:02d}.png"))
            total += 1
        cw, ch = box[2] - box[0], box[3] - box[1]
        # Keep the designed scale: 128 px of the original canvas = 9 tiles (Nibelung 160 px = 12 tiles).
        tiles_per_px = 12 / 160 if D["id"] == "nibelung" else 9 / 128
        meta = {"canvas": [cw, ch],
                "mouth": [int(round(mouth4[0] - box[0])), int(round(mouth4[1] - box[1]))],
                "width_tiles": round(cw * tiles_per_px, 2)}
        os.makedirs(META, exist_ok=True)
        with open(os.path.join(META, D["id"] + ".json"), "w") as fh:
            json.dump(meta, fh, indent=2)
    print(f"{len(DESIGNS)} dragons, {total} frames -> {OUT}")


if __name__ == "__main__":
    main()
