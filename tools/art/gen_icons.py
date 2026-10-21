"""Generates every Teyvat Chronicle icon as 32x32 pixel art.

Run from the repo root:  python tools/art/gen_icons.py
Writes into GameResources/gi/icons/ and icon.png. Needs Pillow.
"""
import math
import os
import sys

from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "GameResources", "gi", "icons")
SIZE = 32

ELEMENTS = {
    "anemo": "#74E5C5",
    "geo": "#F2B53D",
    "electro": "#B46CFF",
    "dendro": "#9BD53C",
    "hydro": "#3FA9FF",
    "pyro": "#FF6A2A",
    "cryo": "#9EE6F5",
}
GOLD = "#F5D27A"
GOLD_DARK = "#A8812F"
OUTLINE = (24, 18, 32, 255)


def hex_rgb(value, alpha=255):
    value = value.lstrip("#")
    return tuple(int(value[i:i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def shade(color, factor):
    r, g, b, a = hex_rgb(color) if isinstance(color, str) else color
    if factor >= 1:
        return (int(r + (255 - r) * (factor - 1)), int(g + (255 - g) * (factor - 1)), int(b + (255 - b) * (factor - 1)), a)
    return (int(r * factor), int(g * factor), int(b * factor), a)


def canvas():
    return Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))


def outlined(img):
    """Adds a 1px dark outline around every opaque pixel, the usual WorldBox icon look."""
    src = img.load()
    out = img.copy()
    dst = out.load()
    for y in range(SIZE):
        for x in range(SIZE):
            if src[x, y][3] > 0:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < SIZE and 0 <= ny < SIZE and src[nx, ny][3] > 0:
                    dst[x, y] = OUTLINE
                    break
    return out


def star_points(cx, cy, r_outer, r_inner, points, rotation=-90):
    pts = []
    for i in range(points * 2):
        r = r_outer if i % 2 == 0 else r_inner
        a = math.radians(rotation + i * 180 / points)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


def glow(draw, color, radius=13):
    for r in range(radius, 4, -2):
        alpha = int(70 * (1 - r / radius)) + 10
        draw.ellipse((16 - r, 16 - r, 15 + r, 15 + r), fill=hex_rgb(color, alpha) if isinstance(color, str) else color[:3] + (alpha,))


# ------------------------------------------------------------------ shapes

def vision(color):
    """A Vision: an element gem in a gold star frame."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.polygon(star_points(16, 16, 14, 9, 4, rotation=-90), fill=hex_rgb(GOLD))
    d.polygon(star_points(16, 16, 11, 7, 4, rotation=-90), fill=hex_rgb(GOLD_DARK))
    d.polygon([(16, 7), (24, 16), (16, 25), (8, 16)], fill=hex_rgb(color))
    d.polygon([(16, 7), (24, 16), (16, 16)], fill=shade(color, 1.35))
    d.polygon([(16, 25), (8, 16), (16, 16)], fill=shade(color, 0.7))
    d.point([(14, 11), (13, 12)], fill=(255, 255, 255, 255))
    return outlined(img)


def gnosis(color):
    """A Gnosis: a chess-piece shape with an element core."""
    img = canvas()
    d = ImageDraw.Draw(img)
    glow(d, color)
    d.polygon([(10, 27), (22, 27), (20, 23), (12, 23)], fill=hex_rgb(GOLD_DARK))
    d.polygon([(12, 23), (20, 23), (19, 14), (13, 14)], fill=hex_rgb(GOLD))
    d.ellipse((10, 5, 22, 17), fill=hex_rgb(color))
    d.ellipse((12, 7, 17, 11), fill=shade(color, 1.4))
    d.rectangle((15, 2, 16, 5), fill=hex_rgb(GOLD))
    return outlined(img)


def sovereign(color):
    """A Dragon Sovereign: horned dragon head facing left."""
    img = canvas()
    d = ImageDraw.Draw(img)
    base = hex_rgb(color)
    dark = shade(color, 0.6)
    d.polygon([(4, 18), (12, 12), (22, 10), (28, 14), (27, 22), (18, 25), (8, 23)], fill=base)
    d.polygon([(4, 18), (12, 20), (18, 25), (8, 23)], fill=dark)
    d.polygon([(20, 11), (26, 3), (24, 11)], fill=hex_rgb(GOLD))
    d.polygon([(15, 12), (17, 4), (19, 11)], fill=hex_rgb(GOLD))
    d.rectangle((18, 15, 20, 16), fill=(255, 245, 180, 255))
    d.point((19, 15), fill=OUTLINE)
    d.line([(6, 19), (10, 19)], fill=shade(color, 1.4))
    return outlined(img)


def crescent(color, cx=16, cy=16, r=11):
    img = canvas()
    d = ImageDraw.Draw(img)
    glow(d, color, 14)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=hex_rgb(color))
    d.ellipse((cx - r + 6, cy - r - 2, cx + r + 6, cy + r - 2), fill=(0, 0, 0, 0))
    return outlined(img)


def trilunar():
    img = canvas()
    d = ImageDraw.Draw(img)
    glow(d, "#E6E0FF", 15)
    for (cx, cy, color) in ((16, 9, "#F4F0FF"), (9, 21, "#C9B8FF"), (23, 21, "#9ED8FF")):
        d.ellipse((cx - 5, cy - 5, cx + 5, cy + 5), fill=hex_rgb(color))
        d.ellipse((cx - 2, cy - 6, cx + 8, cy + 4), fill=(0, 0, 0, 0))
    return outlined(img)


def heavenly(dormant=False):
    img = canvas()
    d = ImageDraw.Draw(img)
    ray = "#8A8A8A" if dormant else "#FFF6D5"
    if not dormant:
        glow(d, "#FFF6D5", 16)
    d.polygon(star_points(16, 16, 15, 5, 8), fill=hex_rgb(ray))
    d.ellipse((8, 11, 24, 21), fill=hex_rgb("#5A5A5A" if dormant else "#FFFFFF"))
    if dormant:
        d.line([(9, 16), (23, 16)], fill=OUTLINE)
    else:
        d.ellipse((13, 13, 19, 19), fill=hex_rgb(GOLD))
        d.ellipse((15, 15, 17, 17), fill=OUTLINE)
    return outlined(img)


def shade_icon(kind):
    img = canvas()
    d = ImageDraw.Draw(img)
    colors = {"life": "#8DFF6A", "death": "#7A6A9A", "time": "#74E5C5", "space": "#B46CFF"}
    c = colors[kind]
    d.ellipse((3, 3, 28, 28), outline=hex_rgb(GOLD), width=2)
    glow(d, c, 11)
    if kind == "life":
        d.polygon([(16, 6), (24, 15), (16, 26), (8, 15)], fill=hex_rgb(c))
        d.line([(16, 9), (16, 24)], fill=shade(c, 0.5))
    elif kind == "death":
        d.ellipse((10, 8, 22, 19), fill=hex_rgb("#E8E4F0"))
        d.rectangle((12, 18, 20, 22), fill=hex_rgb("#E8E4F0"))
        d.rectangle((12, 12, 14, 14), fill=OUTLINE)
        d.rectangle((18, 12, 20, 14), fill=OUTLINE)
        d.line([(14, 20), (14, 22)], fill=OUTLINE)
        d.line([(18, 20), (18, 22)], fill=OUTLINE)
    elif kind == "time":
        d.polygon([(10, 7), (22, 7), (16, 16)], fill=hex_rgb(c))
        d.polygon([(10, 25), (22, 25), (16, 16)], fill=shade(c, 0.7))
        d.line([(9, 6), (23, 6)], fill=hex_rgb(GOLD))
        d.line([(9, 26), (23, 26)], fill=hex_rgb(GOLD))
    else:
        d.polygon(star_points(16, 16, 10, 3, 4, rotation=-45), fill=hex_rgb(c))
        d.ellipse((13, 13, 18, 18), fill=OUTLINE)
    return outlined(img)


def simple(kind):
    img = canvas()
    d = ImageDraw.Draw(img)
    if kind == "delusion":
        d.polygon([(16, 5), (25, 16), (16, 27), (7, 16)], fill=hex_rgb("#C8333A"))
        d.polygon([(16, 5), (25, 16), (16, 16)], fill=hex_rgb("#F06A6A"))
        d.line([(12, 10), (17, 16), (14, 22)], fill=OUTLINE)
        d.line([(20, 13), (17, 16)], fill=OUTLINE)
    elif kind == "allogene":
        glow(d, GOLD, 15)
        d.polygon(star_points(16, 16, 14, 4, 4), fill=hex_rgb(GOLD))
        d.polygon(star_points(16, 16, 8, 3, 4, rotation=-45), fill=hex_rgb("#FFF6D5"))
    elif kind == "adeptus":
        d.ellipse((5, 12, 17, 24), fill=hex_rgb("#6FD3A8"))
        d.ellipse((13, 8, 27, 22), fill=hex_rgb("#8FF0C4"))
        d.ellipse((9, 17, 25, 27), fill=hex_rgb("#4FB98C"))
        d.arc((15, 11, 23, 19), 180, 450, fill=hex_rgb(GOLD), width=2)
    elif kind == "harbinger":
        d.polygon([(4, 12), (16, 6), (28, 12), (24, 22), (16, 26), (8, 22)], fill=hex_rgb("#E8F4FF"))
        d.polygon([(8, 13), (14, 13), (13, 17), (8, 16)], fill=OUTLINE)
        d.polygon([(18, 13), (24, 13), (24, 16), (19, 17)], fill=OUTLINE)
        d.polygon(star_points(16, 21, 4, 1.5, 6), fill=hex_rgb("#9EE6F5"))
    elif kind == "hexenzirkel":
        d.polygon([(16, 2), (23, 20), (9, 20)], fill=hex_rgb("#6A3FB8"))
        d.ellipse((3, 18, 29, 26), fill=hex_rgb("#4A2A88"))
        d.rectangle((10, 17, 22, 19), fill=hex_rgb(GOLD))
        d.point([(18, 8), (14, 12)], fill=(255, 255, 255, 255))
    elif kind == "abyss_herald":
        glow(d, "#7A3CFF", 14)
        d.polygon([(15, 3), (17, 3), (18, 22), (14, 22)], fill=hex_rgb("#3A2A55"))
        d.line([(16, 4), (16, 21)], fill=hex_rgb("#B07CFF"))
        d.rectangle((9, 21, 23, 23), fill=hex_rgb("#5A3A88"))
        d.rectangle((15, 23, 17, 28), fill=hex_rgb("#5A3A88"))
    elif kind == "demon_god":
        glow(d, "#FF5A3C", 15)
        d.polygon([(6, 24), (6, 12), (11, 17), (16, 8), (21, 17), (26, 12), (26, 24)], fill=hex_rgb(GOLD))
        d.rectangle((6, 22, 26, 25), fill=hex_rgb(GOLD_DARK))
        for x in (11, 16, 21):
            d.point((x, 21), fill=hex_rgb("#FF3A2A"))
    elif kind == "abyssal":
        glow(d, "#7A3CFF", 15)
        d.ellipse((5, 10, 27, 22), fill=hex_rgb("#2A1A45"))
        d.ellipse((11, 11, 21, 21), fill=hex_rgb("#B04BFF"))
        d.rectangle((15, 12, 16, 20), fill=OUTLINE)
    elif kind == "curse_immortality":
        d.polygon([(9, 5), (23, 5), (16, 16)], fill=hex_rgb("#C8333A"))
        d.polygon([(9, 27), (23, 27), (16, 16)], fill=hex_rgb("#7A1A20"))
        d.line([(8, 4), (24, 4)], fill=hex_rgb(GOLD_DARK), width=2)
        d.line([(8, 28), (24, 28)], fill=hex_rgb(GOLD_DARK), width=2)
        d.line([(10, 16), (22, 16)], fill=hex_rgb("#E0B062"))
    return outlined(img)


# ------------------------------------------------------------------ UI icons

def chronicle():
    """A tome with the seven element gems on its cover."""
    img = canvas()
    d = ImageDraw.Draw(img)
    d.rectangle((5, 4, 26, 28), fill=hex_rgb("#5A3A22"))
    d.rectangle((7, 5, 26, 27), fill=hex_rgb("#8A5A32"))
    d.rectangle((5, 4, 7, 28), fill=hex_rgb(GOLD_DARK))
    d.ellipse((11, 9, 23, 21), outline=hex_rgb(GOLD), width=1)
    for i, color in enumerate(ELEMENTS.values()):
        a = math.radians(-90 + i * 360 / 7)
        x, y = 17 + 5 * math.cos(a), 15 + 5 * math.sin(a)
        d.rectangle((x - 1, y - 1, x, y), fill=hex_rgb(color))
    return outlined(img)


def button(kind):
    img = canvas()
    d = ImageDraw.Draw(img)
    if kind == "begin":
        glow(d, GOLD, 15)
        d.polygon([(10, 6), (25, 16), (10, 26)], fill=hex_rgb(GOLD))
    elif kind == "advance":
        d.polygon([(5, 7), (16, 16), (5, 25)], fill=hex_rgb("#9EE6F5"))
        d.polygon([(15, 7), (26, 16), (15, 25)], fill=hex_rgb("#74E5C5"))
    elif kind == "pause":
        d.rectangle((9, 7, 13, 25), fill=hex_rgb("#E8F4FF"))
        d.rectangle((19, 7, 23, 25), fill=hex_rgb("#E8F4FF"))
    elif kind == "stop":
        d.rectangle((8, 8, 24, 24), fill=hex_rgb("#C8333A"))
    elif kind == "status":
        d.ellipse((5, 5, 27, 27), fill=hex_rgb("#E8F4FF"))
        d.line([(16, 16), (16, 8)], fill=OUTLINE, width=2)
        d.line([(16, 16), (21, 19)], fill=OUTLINE, width=2)
    elif kind == "timeline":
        d.line([(6, 16), (26, 16)], fill=hex_rgb(GOLD), width=2)
        for x, color in zip((7, 13, 19, 25), ("#FFF6D5", "#B04BFF", "#FF2A2A", "#9EE6F5")):
            d.ellipse((x - 3, 13, x + 2, 18), fill=hex_rgb(color))
    return outlined(img)


def save(img, name, scale=1):
    path = os.path.join(OUT, name + ".png")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if scale != 1:
        img = img.resize((SIZE * scale, SIZE * scale), Image.NEAREST)
    img.save(path)


def main():
    for element, color in ELEMENTS.items():
        save(vision(color), f"traits/gi_vision_{element}")
        save(gnosis(color), f"traits/gi_gnosis_{element}")
        save(sovereign(color), f"traits/gi_sovereign_{element}")
    save(sovereign("#E6E0FF"), "traits/gi_nibelung")
    for kind in ("life", "death", "time", "space"):
        save(shade_icon(kind), f"traits/gi_shade_{kind}")
    save(crescent("#E6E0FF"), "traits/gi_moon_goddess")
    save(trilunar(), "traits/gi_trilunar")
    save(heavenly(), "traits/gi_heavenly_principles")
    save(heavenly(dormant=True), "traits/gi_dormant_principles")
    for kind in ("delusion", "allogene", "adeptus", "harbinger", "hexenzirkel", "abyss_herald", "demon_god",
                 "abyssal", "curse_immortality"):
        save(simple(kind), f"traits/gi_{kind}")

    save(chronicle(), "chronicle")
    save(chronicle(), "tab")
    for kind in ("begin", "advance", "pause", "stop", "status", "timeline"):
        save(button(kind), f"btn_{kind}")

    chronicle().resize((128, 128), Image.NEAREST).save(os.path.join(ROOT, "icon.png"))
    print("icons written to", OUT)


if __name__ == "__main__":
    sys.exit(main())
