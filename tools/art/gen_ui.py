"""Generates the Teyvat UI kit: Genshin-style panels with gold trim, buttons, bars, tabs, stars,
constellation nodes, element emblems, tier badges and the spotlight vignette.

Run from the repo root:  python tools/art/gen_ui.py
Writes ui/*.png (loaded by the mod itself, with 9-slice borders set in code). Needs Pillow + numpy.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
OUT = os.path.join(ROOT, "ui")

NAVY = (24, 30, 46)
NAVY_2 = (34, 42, 62)
GOLD = (217, 181, 106)
GOLD_L = (244, 222, 160)
GOLD_D = (140, 108, 52)
CREAM = (236, 229, 216)
CREAM_D = (196, 186, 166)


def rgba(c, a=255):
    return tuple(c[:3]) + (a,)


def save(img, name):
    os.makedirs(OUT, exist_ok=True)
    img.save(os.path.join(OUT, name + ".png"))


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def panel():
    """48x48, 9-slice border 14. Navy gradient, double gold frame, diamond corners."""
    s = 48
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for y in range(s):
        c = lerp(NAVY_2, NAVY, y / s)
        d.line([(2, y), (s - 3, y)], fill=rgba(c, 255))
    d.rectangle((1, 1, s - 2, s - 2), outline=rgba(GOLD_D))
    d.rectangle((2, 2, s - 3, s - 3), outline=rgba(GOLD))
    d.rectangle((5, 5, s - 6, s - 6), outline=rgba(GOLD_D, 170))
    for cx, cy in ((3, 3), (s - 4, 3), (3, s - 4), (s - 4, s - 4)):
        d.polygon([(cx, cy - 3), (cx + 3, cy), (cx, cy + 3), (cx - 3, cy)], fill=rgba(GOLD_L))
        d.point((cx, cy), fill=rgba(NAVY))
    save(img, "panel")


def panel_inner():
    """32x32, 9-slice border 6. A darker inset card."""
    s = 32
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.rectangle((0, 0, s - 1, s - 1), fill=rgba((16, 20, 32), 255), outline=rgba(GOLD_D, 220))
    d.line([(1, 1), (s - 2, 1)], fill=rgba((60, 70, 96), 200))
    save(img, "panel_inner")


def header():
    """48x18, border 8. A gold-rimmed title strip."""
    w, h = 48, 18
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for y in range(h):
        d.line([(0, y), (w - 1, y)], fill=rgba(lerp((52, 44, 30), (28, 24, 18), y / h), 240))
    d.line([(0, h - 1), (w - 1, h - 1)], fill=rgba(GOLD))
    d.line([(0, h - 2), (w - 1, h - 2)], fill=rgba(GOLD_D))
    d.line([(0, 0), (w - 1, 0)], fill=rgba(GOLD_D))
    save(img, "header")


def button(name, top, bottom, edge, text_hint=None):
    """32x16, border 6. Rounded pill."""
    w, h = 32, 16
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for y in range(h):
        c = lerp(top, bottom, y / h)
        x0 = 2 if y in (0, h - 1) else (1 if y in (1, h - 2) else 0)
        d.line([(x0, y), (w - 1 - x0, y)], fill=rgba(c))
    d.rounded_rectangle((0, 0, w - 1, h - 1), radius=4, outline=rgba(edge))
    d.line([(3, 1), (w - 4, 1)], fill=rgba(lerp(top, (255, 255, 255), 0.4), 180))
    save(img, name)


def bar():
    w, h = 16, 8
    bg = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(bg)
    d.rectangle((0, 0, w - 1, h - 1), fill=rgba((10, 12, 20), 230), outline=rgba(GOLD_D))
    save(bg, "bar_bg")
    fill = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(fill)
    for y in range(h):
        v = 255 if y < 2 else (225 if y < h - 2 else 180)
        d.line([(0, y), (w - 1, y)], fill=(v, v, v, 255))
    save(fill, "bar_fill")


def tab(name, active):
    w, h = 32, 16
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    top = (70, 62, 44) if active else (36, 42, 60)
    bottom = (40, 34, 24) if active else (24, 28, 42)
    for y in range(h):
        d.line([(1, y), (w - 2, y)], fill=rgba(lerp(top, bottom, y / h), 245))
    d.rectangle((0, 0, w - 1, h - 1), outline=rgba(GOLD if active else GOLD_D))
    if active:
        d.line([(2, h - 2), (w - 3, h - 2)], fill=rgba(GOLD_L))
    save(img, name)


def star(name, filled):
    s = 11
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pts = []
    for i in range(10):
        r = 5 if i % 2 == 0 else 2.2
        a = math.radians(-90 + i * 36)
        pts.append((5 + r * math.cos(a), 5.2 + r * math.sin(a)))
    if filled:
        d.polygon(pts, fill=rgba(GOLD_L), outline=rgba(GOLD_D))
    else:
        d.polygon(pts, fill=rgba((40, 44, 60), 220), outline=rgba(GOLD_D, 200))
    save(img, name)


def node(name, lit):
    s = 13
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    if lit:
        for r in range(6, 2, -1):
            d.ellipse((6 - r, 6 - r, 6 + r, 6 + r), fill=(255, 255, 255, int(40 + (6 - r) * 30)))
        d.ellipse((3, 3, 9, 9), fill=(255, 255, 255, 255))
    else:
        d.ellipse((3, 3, 9, 9), fill=(30, 34, 48, 230), outline=(150, 150, 170, 200))
    save(img, name)


def emblem(element):
    """16x16 white glyph per element, tinted in code."""
    s = 16
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    W = (255, 255, 255, 255)
    if element == "anemo":
        d.arc((2, 2, 13, 13), 200, 470, fill=W, width=2)
        d.arc((5, 5, 10, 10), 20, 290, fill=W, width=1)
    elif element == "geo":
        d.polygon([(8, 1), (14, 8), (8, 15), (2, 8)], outline=W)
        d.polygon([(8, 4), (11, 8), (8, 12), (5, 8)], fill=W)
    elif element == "electro":
        d.polygon([(9, 1), (4, 9), (8, 9), (6, 15), (12, 6), (8, 6)], fill=W)
    elif element == "dendro":
        d.ellipse((3, 2, 13, 12), outline=W, width=2)
        d.line([(8, 3), (8, 15)], fill=W)
        d.line([(8, 8), (11, 5)], fill=W)
    elif element == "hydro":
        d.polygon([(8, 1), (12, 8), (4, 8)], fill=W)
        d.ellipse((4, 6, 12, 14), fill=W)
        d.point((6, 10), fill=(0, 0, 0, 0))
    elif element == "pyro":
        d.polygon([(8, 1), (12, 7), (11, 13), (8, 15), (5, 13), (4, 7), (7, 9)], fill=W)
        d.polygon([(8, 8), (10, 11), (8, 13), (6, 11)], fill=(0, 0, 0, 0))
    elif element == "cryo":
        for a in range(0, 180, 60):
            r = math.radians(a)
            d.line([(8 - 6 * math.cos(r), 8 - 6 * math.sin(r)), (8 + 6 * math.cos(r), 8 + 6 * math.sin(r))], fill=W, width=1)
        d.ellipse((6, 6, 10, 10), fill=W)
    else:  # generic sigil for abyss, lunar, divine, life, death, time, space, delusion
        d.ellipse((2, 2, 13, 13), outline=W, width=1)
        d.polygon([(8, 3), (12, 8), (8, 13), (4, 8)], fill=W)
    save(img, "emblem_" + element)


def tier_badge(tier):
    """18x18 hexagonal badge with the tier number in pips."""
    s = 18
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    pts = [(9 + 8 * math.cos(math.radians(60 * i - 90)), 9 + 8 * math.sin(math.radians(60 * i - 90))) for i in range(6)]
    d.polygon(pts, fill=(255, 255, 255, 255), outline=(60, 50, 30, 255))
    for i in range(tier):
        a = math.radians(-90 + i * 360 / max(1, tier))
        x, y = 9 + 4 * math.cos(a), 9 + 4 * math.sin(a)
        d.rectangle((x - 1, y - 1, x, y), fill=(40, 30, 20, 255))
    save(img, f"tier_{tier}")


def close():
    s = 12
    img = Image.new("RGBA", (s, s), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    d.ellipse((0, 0, s - 1, s - 1), fill=rgba((60, 30, 30), 230), outline=rgba(GOLD))
    d.line([(3, 3), (8, 8)], fill=rgba(GOLD_L), width=2)
    d.line([(8, 3), (3, 8)], fill=rgba(GOLD_L), width=2)
    save(img, "close")


def divider():
    w, h = 64, 5
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    for x in range(w):
        a = int(255 * math.sin(math.pi * x / (w - 1)))
        d.point((x, 2), fill=rgba(GOLD, a))
    d.polygon([(32, 0), (34, 2), (32, 4), (30, 2)], fill=rgba(GOLD_L))
    save(img, "divider")


def vignette():
    """256x256 spotlight: transparent centre, dark edges."""
    s = 256
    y, x = np.mgrid[0:s, 0:s].astype(np.float32)
    r = np.hypot(x - s / 2, y - s / 2) / (s / 2)
    a = np.clip((r - 0.32) / 0.55, 0, 1) ** 1.6
    rgba_arr = np.zeros((s, s, 4), np.uint8)
    rgba_arr[..., 3] = (a * 235).astype(np.uint8)
    Image.fromarray(rgba_arr, "RGBA").save(os.path.join(OUT, "vignette.png"))


def main():
    panel()
    panel_inner()
    header()
    button("button", CREAM, CREAM_D, (120, 104, 76))
    button("button_dark", (54, 62, 86), (30, 36, 52), GOLD)
    button("button_gold", GOLD_L, GOLD, GOLD_D)
    bar()
    tab("tab", False)
    tab("tab_active", True)
    star("star", True)
    star("star_empty", False)
    node("node_lit", True)
    node("node_dim", False)
    for e in ("anemo", "geo", "electro", "dendro", "hydro", "pyro", "cryo", "other"):
        emblem(e)
    for t in range(1, 8):
        tier_badge(t)
    close()
    divider()
    vignette()
    print("ui sprites written to", OUT)


if __name__ == "__main__":
    main()
