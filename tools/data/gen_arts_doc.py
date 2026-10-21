"""Writes the style roster section of ARTS.md from the style scripts in Code/Arts/Styles.

Run from the repo root:  python tools/data/gen_arts_doc.py
Everything between the ROSTER markers in ARTS.md is replaced.
"""
import glob
import os
import re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ORDER = ["Visions", "Outsiders", "Exalted", "Archons", "Sovereigns", "Primordials"]
TIER = {1: "Vision", 2: "Exalted", 3: "Archon", 4: "Dragon Sovereign", 5: "Shade", 6: "Lunar", 7: "Heavenly"}
STR = r'"((?:[^"\\]|\\.)*)"'


def chunks(src):
    """Splits a file into (style call, its builder chain) pieces."""
    starts = [m.start() for m in re.finditer(r"Library\.Style\(", src)]
    for i, s in enumerate(starts):
        end = starts[i + 1] if i + 1 < len(starts) else len(src)
        body = src[s:end]
        # The chain ends at the statement's closing ");" (descriptions may contain semicolons).
        stop = re.search(r"\)\s*;\s*\n", body)
        yield body[: stop.start() + 1 if stop else len(body)]


def main():
    out = []
    for group in ORDER:
        path = os.path.join(ROOT, "Code", "Arts", "Styles", group + ".cs")
        if not os.path.exists(path):
            continue
        src = open(path, encoding="utf-8").read()
        for chain in chunks(src):
            m = re.match(r'Library\.Style\("([a-z_]+)",\s*' + STR + r",\s*(\d+),\s*\"([a-z]+)\"", chain)
            if not m:
                continue
            trait, name, tier, element = m.group(1), m.group(2), int(m.group(3)), m.group(4)
            vision = ".VisionElement()" in chain
            out.append(f"### {name}\n")
            el = f"{element} (or the holder's Vision)" if vision else element
            out.append(f"`{trait}` · Tier {tier} ({TIER.get(tier, '')}) · {el}\n")
            out.append("| Technique | Best against | Description |")
            out.append("|---|---|---|")
            for t in re.finditer(r"\.T\(\s*" + STR + r",\s*" + STR + r",\s*Shape\.(\w+),(.*?)(?=\n\s*\.(?:T|God)\(|\Z)",
                                 chain, re.S):
                rest = t.group(4)
                desc = re.search(STR, rest)
                ult = re.search(r",\s*true\s*\)\s*$", rest.strip()) is not None
                label = t.group(2) + (" *(ultimate)*" if ult else "")
                out.append(f"| {label} | {t.group(3)} | {desc.group(1) if desc else ''} |")
            g = re.search(r"\.God\(\s*" + STR + r",\s*" + STR + r",\s*\w+(?:,\s*" + STR + r")?", chain)
            if g:
                out.append(f"| **{g.group(2)}** *(God ability)* | world-stop | {g.group(3) or ''} |")
            out.append("")
    doc_path = os.path.join(ROOT, "ARTS.md")
    doc = open(doc_path, encoding="utf-8").read()
    a, b = doc.index("<!-- ROSTER -->"), doc.index("<!-- /ROSTER -->")
    doc = doc[: a + len("<!-- ROSTER -->")] + "\n\n" + "\n".join(out) + "\n" + doc[b:]
    open(doc_path, "w", encoding="utf-8").write(doc)
    print(f"{len([l for l in out if l.startswith('### ')])} styles written to ARTS.md")


if __name__ == "__main__":
    main()
