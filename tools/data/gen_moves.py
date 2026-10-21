"""Builds data/moves.json: every power tier's moveset.

Run from the repo root:  python tools/data/gen_moves.py
Edit this script (or copy data/moves.json to data/moves.custom.json) to change moves.
Every tier has at least five moves; higher tiers hit harder through their own stats plus bigger
power values, more hits, wider areas, impact frames and cutscenes.
"""
import json
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
ELEMENTS = ["anemo", "geo", "electro", "dendro", "hydro", "pyro", "cryo"]

MOVES = {}
SETS = {}

# Element flavour: VFX sets, sounds and the status each element's skills inflict.
FLAVOUR = {
    "anemo":   {"field": "tornado", "proj": "orb",   "sfx": "wind",    "status": "slowness"},
    "geo":     {"field": "spikes",  "proj": "orb",   "sfx": "rock",    "status": "stunned"},
    "electro": {"field": "pillar",  "proj": "orb",   "sfx": "thunder", "status": "stunned"},
    "dendro":  {"field": "bloom",   "proj": "orb",   "sfx": "nature",  "status": "poisoned"},
    "hydro":   {"field": "wave",    "proj": "orb",   "sfx": "splash",  "status": "slowness"},
    "pyro":    {"field": "burst",   "proj": "orb",   "sfx": "fire",    "status": "burning"},
    "cryo":    {"field": "spikes",  "proj": "orb",   "sfx": "ice",     "status": "frozen"},
}


def move(mid, **kw):
    MOVES[mid] = {k: v for k, v in kw.items() if v is not None}
    return mid


def moveset(trait, tier, element, moves):
    assert len(moves) >= 5, trait
    SETS[trait] = {"tier": tier, "element": element, "moves": moves}


# ------------------------------------------------------------------ Tier 1: Visions

VISION_NAMES = {
    "anemo":   ["Gale Cut", "Wind Spiral", "Updraft Leap", "Windbarrier", "Tempest Eye"],
    "geo":     ["Stone Fist", "Jade Stele", "Rockslide Dash", "Jade Shield", "Meteor Fall"],
    "electro": ["Spark Slash", "Thunder Mark", "Lightning Step", "Static Field", "Thunderclap Musou"],
    "dendro":  ["Thorn Lash", "Seed of Skandha", "Vine Swing", "Verdant Bloom", "Garden of Wisdom"],
    "hydro":   ["Tide Slash", "Water Prison", "Wave Surf", "Rain Ward", "Deluge"],
    "pyro":    ["Ember Strike", "Blazing Burst", "Flame Dash", "Warmth Ward", "Phoenix Descent"],
    "cryo":    ["Frost Edge", "Ice Lotus", "Ice Glide", "Frost Armor", "Glacial Waltz"],
}

for e in ELEMENTS:
    f = FLAVOUR[e]
    n = VISION_NAMES[e]
    strike = move(f"{e}_strike", name=n[0], archetype="barrage" if e == "electro" else "strike",
                  fx="slash", fx2="hit", sfx="swing", sfx2="hit", power=0.45 if e == "electro" else 1.0,
                  hits=3 if e == "electro" else 1, interval=0.09, range=2, cooldown=1.2)
    if e in ("anemo", "geo", "cryo", "electro"):
        skill = move(f"{e}_skill", name=n[1], archetype="field" if e != "electro" else "rain",
                     fx=f["field"], sfx=f["sfx"], power=0.4 if e != "electro" else 0.9,
                     hits=4 if e != "electro" else 3, interval=0.35, duration=1.4, radius=1.6, range=5,
                     status=f["status"], status_time=2, cooldown=5, scale=1.0)
    else:
        skill = move(f"{e}_skill", name=n[1], archetype="projectile", fx=f["proj"], fx2=f["field"],
                     sfx="cast", sfx2=f["sfx"], power=1.6, radius=1.4, range=7, status=f["status"],
                     status_time=3, cooldown=5)
    if e == "electro":
        mobility = move(f"{e}_mobility", name=n[2], archetype="teleport", fx="vanish", fx2="slash_big",
                        sfx="dash", sfx2="hit_heavy", power=1.4, range=8, cooldown=6)
    else:
        mobility = move(f"{e}_mobility", name=n[2], archetype="dash", fx="streak", fx2="slash_big",
                        sfx="dash", sfx2="hit_heavy", power=1.4, range=7, cooldown=6,
                        radius=1.2 if e in ("anemo", "geo") else 0)
    if e in ("dendro", "hydro"):
        utility = move(f"{e}_utility", name=n[3], archetype="heal", fx="aura", sfx="heal", heal=0.25,
                       radius=4, range=0, cooldown=12)
    elif e == "pyro":
        utility = move(f"{e}_utility", name=n[3], archetype="buff", fx="aura", sfx="fire",
                       self_status="powerup", status_time=8, range=0, cooldown=12)
    elif e == "electro":
        utility = move(f"{e}_utility", name=n[3], archetype="field", fx="circle", sfx="thunder", power=0.3,
                       hits=5, interval=0.3, duration=1.5, radius=2, range=4, status="slowness", cooldown=10)
    else:
        utility = move(f"{e}_utility", name=n[3], archetype="shield", fx="shield", sfx="cast",
                       self_status="shield", status_time=6, range=0, cooldown=12)
    burst_arch = {"anemo": "field", "geo": "rain", "electro": "beam", "dendro": "field",
                  "hydro": "nova", "pyro": "ultimate", "cryo": "field"}[e]
    burst = move(f"{e}_burst", name=n[4], archetype=burst_arch, fx=f["field"] if burst_arch != "beam" else "beam",
                 fx2="burst", sfx="charge", sfx2="boom", power=0.9 if burst_arch in ("field", "beam") else 2.6,
                 hits=6 if burst_arch in ("field", "beam") else (3 if burst_arch == "rain" else 1),
                 interval=0.25, duration=2.0, radius=2.6, range=7, status=f["status"], status_time=3,
                 energy=100, cooldown=15, impact="minor", scale=1.5)
    moveset(f"gi_vision_{e}", 1, e, [strike, skill, mobility, utility, burst])

# ------------------------------------------------------------------ Tier 1b: Delusion and Allogene

moveset("gi_delusion", 1, "delusion", [
    move("delusion_bolt", name="Delusion Bolt", archetype="projectile", element="random", fx="orb", fx2="burst",
         sfx="cast", sfx2="explode", power=1.5, radius=1.2, range=8, cooldown=2.5),
    move("delusion_overdrive", name="Overdrive", archetype="buff", fx="aura", sfx="charge",
         self_status="powerup", status_time=10, range=0, cooldown=14),
    move("delusion_veil", name="Mirror Maiden's Veil", archetype="teleport", fx="vanish", fx2="slash_big",
         sfx="dash", sfx2="hit_heavy", power=1.3, range=7, cooldown=7),
    move("delusion_cicin", name="Cicin Swarm", archetype="barrage", element="electro", fx="spark", fx2="hit",
         sfx="thunder", sfx2="hit", power=0.35, hits=7, interval=0.08, range=6, cooldown=6),
    move("delusion_lifeburn", name="Life-Burn Detonation", archetype="nova", element="delusion", fx="nova",
         fx2="burst", sfx="charge", sfx2="boom", power=3.2, radius=3.2, range=3, energy=100, cooldown=18,
         impact="major", scale=1.6),
])

moveset("gi_allogene", 1, "anemo", [
    move("allogene_blade", name="Foreign Blade", archetype="barrage", fx="slash", fx2="hit", sfx="swing",
         sfx2="hit", power=0.5, hits=4, interval=0.08, range=2, cooldown=1.4),
    move("allogene_starfell", name="Starfell Sword", archetype="projectile", fx="orb", fx2="burst", sfx="cast",
         sfx2="explode", power=1.8, radius=1.6, range=8, cooldown=5),
    move("allogene_step", name="Traveler's Step", archetype="dash", fx="streak", fx2="slash_big", sfx="dash",
         sfx2="hit_heavy", power=1.5, range=7, cooldown=6),
    move("allogene_resonance", name="Elemental Resonance", archetype="nova", fx="nova", sfx="cast", power=1.2,
         radius=2.4, range=2, cooldown=9),
    move("allogene_abyss", name="Abyss Resonance", archetype="field", element="abyss", fx="circle", sfx="dark",
         power=0.5, hits=5, interval=0.3, duration=1.5, radius=2.2, range=5, status="cursed", cooldown=12),
    move("allogene_burst", name="Gust of Another World", archetype="ultimate", fx="tornado", fx2="burst",
         sfx="charge", sfx2="boom", power=3.4, radius=3, range=7, energy=100, cooldown=16, impact="major",
         scale=1.6),
])

# ------------------------------------------------------------------ Tier 2: Exalted

moveset("gi_adeptus", 2, "anemo", [
    move("adeptus_dive", name="Adeptal Spear Dive", archetype="dash", element="anemo", fx="streak",
         fx2="slash_big", sfx="dash", sfx2="hit_heavy", power=2.0, radius=1.4, range=9, cooldown=4),
    move("adeptus_mask", name="Yaksha Mask", archetype="buff", element="anemo", fx="aura", sfx="dark",
         self_status="powerup", status_time=12, range=0, cooldown=14),
    move("adeptus_cloud", name="Cloud Step", archetype="teleport", element="anemo", fx="vanish",
         fx2="slash_big", sfx="wind", sfx2="hit_heavy", power=1.8, range=10, cooldown=6),
    move("adeptus_purge", name="Karma Purge", archetype="nova", element="divine", fx="nova", sfx="cast",
         power=1.5, radius=3, range=3, cooldown=10),
    move("adeptus_jade_rain", name="Adeptal Arts: Jade Rain", archetype="rain", element="geo", fx="pillar",
         fx2="burst", sfx="rock", power=1.1, hits=6, interval=0.12, radius=2.5, range=8, cooldown=9),
    move("adeptus_beast", name="Divine Beast Form", archetype="ultimate", element="anemo", fx="tornado",
         fx2="burst", sfx="charge", sfx2="boom", power=4.5, radius=3.4, range=8, energy=100, cooldown=18,
         impact="major", scale=1.8),
])

moveset("gi_harbinger", 2, "cryo", [
    move("harbinger_edge", name="Harbinger's Edge", archetype="barrage", fx="slash", fx2="hit", sfx="swing",
         sfx2="hit", power=0.6, hits=5, interval=0.08, range=2, cooldown=1.6),
    move("harbinger_execution", name="Frost Execution", archetype="dash", fx="streak", fx2="slash_big",
         sfx="dash", sfx2="hit_heavy", power=2.2, range=8, cooldown=6),
    move("harbinger_decree", name="Snezhnayan Decree", archetype="nova", fx="nova", sfx="ice", power=1.4,
         radius=3, range=3, status="slowness", cooldown=9),
    move("harbinger_legacy", name="Foul Legacy", archetype="buff", element="delusion", fx="aura", sfx="dark",
         self_status="powerup", status_time=12, range=0, cooldown=15),
    move("harbinger_volley", name="Delusion Volley", archetype="rain", element="delusion", fx="orb", fx2="burst",
         sfx="cast", sfx2="explode", power=1.0, hits=5, interval=0.14, radius=2.2, range=9, cooldown=8),
    move("harbinger_overload", name="Delusion Overload", archetype="ultimate", element="delusion", fx="nova",
         fx2="burst", sfx="charge", sfx2="boom", power=4.8, radius=3.6, range=7, energy=100, cooldown=18,
         impact="major", scale=1.8),
])

moveset("gi_hexenzirkel", 2, "lunar", [
    move("witch_sigil", name="Arcane Sigil", archetype="projectile", fx="orb", fx2="circle", sfx="cast",
         sfx2="explode", power=1.6, radius=1.4, range=9, cooldown=2.2),
    move("witch_hex", name="Hex", archetype="projectile", element="abyss", fx="orb", fx2="vanish", sfx="dark",
         power=1.0, range=9, status="cursed", status_time=6, cooldown=6),
    move("witch_flight", name="Witch's Flight", archetype="teleport", fx="vanish", fx2="nova", sfx="wind",
         power=1.0, radius=1.6, range=10, cooldown=7),
    move("witch_brew", name="Time-Lost Brew", archetype="heal", element="life", fx="aura", sfx="heal", heal=0.4,
         radius=4, range=0, cooldown=12),
    move("witch_ritual", name="Grand Ritual", archetype="rain", fx="pillar", fx2="burst", sfx="thunder",
         power=1.2, hits=7, interval=0.12, radius=3, range=9, cooldown=10),
    move("witch_storm", name="Witchcraft Tempest", archetype="ultimate", fx="circle", fx2="burst",
         sfx="charge", sfx2="boom", power=4.4, radius=3.6, range=9, energy=100, cooldown=18, impact="major",
         scale=1.8),
])

moveset("gi_abyss_herald", 2, "abyss", [
    move("herald_rend", name="Abyssal Rend", archetype="barrage", fx="slash", fx2="hit", sfx="swing",
         sfx2="hit", power=0.6, hits=4, interval=0.09, range=2, cooldown=1.5),
    move("herald_shadow", name="Shadow Step", archetype="teleport", fx="vanish", fx2="slash_big", sfx="dark",
         sfx2="hit_heavy", power=1.8, range=9, cooldown=6),
    move("herald_corrosion", name="Corrosion", archetype="field", fx="circle", sfx="dark", power=0.45, hits=6,
         interval=0.3, duration=1.8, radius=2, range=6, status="poisoned", cooldown=8),
    move("herald_rift", name="Abyssal Rift", archetype="field", fx="vanish", sfx="dark", power=0.6, hits=5,
         interval=0.3, duration=1.5, radius=2.4, range=6, status="slowness", cooldown=10, scale=1.6),
    move("herald_mantle", name="Abyss Mantle", archetype="shield", fx="shield", sfx="dark",
         self_status="shield", status_time=8, range=0, cooldown=14),
    move("herald_curse", name="Hilichurl Curse", archetype="ultimate", fx="nova", fx2="burst", sfx="charge",
         sfx2="boom", power=4.2, radius=3.4, range=6, status="cursed", energy=100, cooldown=18,
         impact="major", scale=1.7),
])

moveset("gi_demon_god", 2, "divine", [
    move("demon_fury", name="Divine Fury", archetype="barrage", fx="slash_big", fx2="hit", sfx="swing",
         sfx2="hit_heavy", power=0.8, hits=5, interval=0.1, range=3, cooldown=1.8),
    move("demon_domain", name="God's Domain", archetype="field", fx="circle", sfx="cast", power=0.6, hits=6,
         interval=0.3, duration=1.8, radius=3, range=6, cooldown=8),
    move("demon_calamity", name="Calamity", archetype="rain", fx="pillar", fx2="burst", sfx="thunder",
         power=1.4, hits=6, interval=0.12, radius=3, range=9, cooldown=9),
    move("demon_warcry", name="War Cry", archetype="nova", fx="nova", sfx="boom", power=1.6, radius=3,
         range=3, status="stunned", status_time=1.5, cooldown=10),
    move("demon_avatar", name="Demon Avatar", archetype="ultimate", fx="pillar", fx2="burst", sfx="charge",
         sfx2="boom", power=5.0, radius=3.6, range=8, energy=100, cooldown=18, impact="major", scale=1.9),
])

# ------------------------------------------------------------------ Tier 3: Archons

ARCHON_BURSTS = {
    "anemo": ("Wind's Grand Ode", "tornado"),
    "geo": ("Planet Befall", "spikes"),
    "electro": ("Musou Isshin", "beam"),
    "dendro": ("Illusory Heart", "bloom"),
    "hydro": ("Grand Trial", "wave"),
    "pyro": ("Sunfall Anthem", "burst"),
    "cryo": ("Everwinter", "spikes"),
}

for e in ELEMENTS:
    f = FLAVOUR[e]
    title, fx = ARCHON_BURSTS[e]
    moveset(f"gi_gnosis_{e}", 3, e, [
        move(f"archon_{e}_strike", name="Divine " + VISION_NAMES[e][0], archetype="barrage", fx="slash_big",
             fx2="hit", sfx="swing", sfx2="hit_heavy", power=0.9, hits=4, interval=0.08, range=3, cooldown=1.2),
        move(f"archon_{e}_skill", name="Divine " + VISION_NAMES[e][1], archetype="field", fx=f["field"],
             sfx=f["sfx"], power=0.8, hits=6, interval=0.3, duration=1.8, radius=3, range=8,
             status=f["status"], cooldown=6, scale=1.6),
        move(f"archon_{e}_weather", name="Divine Weather", archetype="rain", fx="pillar", fx2=f["field"],
             sfx=f["sfx"], power=1.6, hits=8, interval=0.1, radius=4, range=10, cooldown=10),
        move(f"archon_{e}_resonance", name="Gnosis Resonance", archetype="heal", fx="aura", sfx="heal",
             heal=0.5, radius=6, range=0, cooldown=14),
        move(f"archon_{e}_avatar", name="Divine Avatar", archetype="buff", fx="aura", sfx="charge",
             self_status="invincible", status_time=4, range=0, cooldown=24),
        move(f"archon_{e}_burst", name=title, archetype="ultimate", fx=fx, fx2="burst", sfx="charge",
             sfx2="boom", power=8.0, radius=5, range=12, status=f["status"], energy=100, cooldown=25,
             impact="major", cutscene=True, scale=2.4),
    ])

# ------------------------------------------------------------------ Tier 4: Dragon Sovereigns and Nibelung

for e in ELEMENTS:
    f = FLAVOUR[e]
    moveset(f"gi_sovereign_{e}", 4, e, [
        move(f"sovereign_{e}_talon", name="Talon Sweep", archetype="barrage", fx="slash_big", fx2="hit",
             sfx="swing", sfx2="hit_heavy", power=1.0, hits=4, interval=0.1, range=3, radius=1.2, cooldown=1.5),
        move(f"sovereign_{e}_breath", name="Dragon Breath", archetype="beam", fx="beam", fx2=f["field"],
             sfx="breath", power=1.2, hits=6, interval=0.15, duration=1.0, radius=0.8, range=10,
             status=f["status"], cooldown=5),
        move(f"sovereign_{e}_roar", name="Primal Roar", archetype="nova", fx="nova", sfx="roar", power=1.8,
             radius=4, range=4, status="stunned", status_time=2, cooldown=10, scale=1.6),
        move(f"sovereign_{e}_scales", name="Sovereign's Scales", archetype="shield", fx="shield", sfx="cast",
             self_status="shield", status_time=10, range=0, cooldown=16),
        move(f"sovereign_{e}_soil", name="Holy Soil", archetype="field", fx=f["field"], sfx=f["sfx"], power=0.9,
             hits=8, interval=0.3, duration=2.4, radius=4, range=10, cooldown=10, scale=1.8),
        move(f"sovereign_{e}_cataclysm", name="Elemental Cataclysm", archetype="ultimate", fx=f["field"],
             fx2="burst", sfx="roar", sfx2="boom", power=10.0, radius=6, range=14, status=f["status"],
             energy=100, cooldown=28, impact="major", cutscene=True, scale=2.8),
    ])

moveset("gi_nibelung", 6, "lunar", [
    move("nibelung_breaths", name="Seven Breaths", archetype="beam", element="random", fx="beam", fx2="burst",
         sfx="breath", power=1.6, hits=8, interval=0.12, duration=1.2, radius=1, range=12, cooldown=4),
    move("nibelung_talon", name="King's Talon", archetype="barrage", fx="slash_big", fx2="hit", sfx="swing",
         sfx2="hit_heavy", power=1.2, hits=5, interval=0.09, range=3, radius=1.4, cooldown=1.5),
    move("nibelung_abyss", name="Abyssal Flame", archetype="field", element="abyss", fx="circle", sfx="dark",
         power=1.0, hits=8, interval=0.3, duration=2.4, radius=4.5, range=12, status="cursed", cooldown=9),
    move("nibelung_roar", name="Roar of the Dragon King", archetype="nova", fx="nova", sfx="roar", power=2.2,
         radius=5, range=5, status="stunned", status_time=2.5, cooldown=10, scale=2),
    move("nibelung_moons", name="Moon-Making", archetype="rain", element="lunar", fx="orb", fx2="burst",
         sfx="moon", sfx2="boom", power=2.0, hits=9, interval=0.1, radius=4.5, range=12, cooldown=12),
    move("nibelung_funerary", name="Funerary Flame", archetype="ultimate", element="abyss", fx="burst",
         fx2="nova", sfx="roar", sfx2="boom", power=14.0, radius=7, range=14, energy=100, cooldown=30,
         impact="major", cutscene=True, scale=3.2),
])

# ------------------------------------------------------------------ Tier 5: the Four Shades

SHADES = {
    "time": ("time", [
        ("Moment's Halt", "field", dict(fx="circle", sfx="time", power=0.2, hits=4, interval=0.4, duration=1.6,
                                        radius=4, range=10, status="frozen", status_time=3, cooldown=10)),
        ("Accelerate", "buff", dict(fx="aura", sfx="time", self_status="caffeinated", status_time=10, range=0,
                                    cooldown=12)),
        ("Age", "projectile", dict(fx="orb", fx2="vanish", sfx="time", power=2.4, radius=1.6, range=12,
                                   cooldown=4)),
        ("Temporal Echo", "barrage", dict(fx="slash_big", fx2="hit", sfx="swing", sfx2="hit_heavy", power=1.0,
                                          hits=6, interval=0.06, range=4, cooldown=1.5)),
        ("Rewind", "heal", dict(fx="aura", sfx="heal", heal=0.6, radius=6, range=0, cooldown=14)),
        ("Eternal Recurrence", "ultimate", dict(fx="circle", fx2="nova", sfx="charge", sfx2="boom", power=12.0,
                                                radius=6, range=14, status="frozen", energy=100, cooldown=30,
                                                impact="major", cutscene=True, scale=3)),
    ]),
    "space": ("space", [
        ("Fold", "teleport", dict(fx="vanish", fx2="slash_big", sfx="space", sfx2="hit_heavy", power=2.4,
                                  range=14, cooldown=4)),
        ("Spatial Tear", "beam", dict(fx="beam", fx2="vanish", sfx="space", power=1.6, hits=6, interval=0.1,
                                      duration=0.8, radius=0.8, range=12, cooldown=5)),
        ("Distance Lock", "field", dict(fx="circle", sfx="space", power=0.4, hits=5, interval=0.35,
                                        duration=1.8, radius=4, range=10, status="slowness", cooldown=10)),
        ("Compression", "nova", dict(fx="nova", fx2="burst", sfx="boom", power=2.6, radius=4, range=8,
                                     cooldown=8)),
        ("Spatial Barrier", "shield", dict(fx="shield", sfx="space", self_status="shield", status_time=10,
                                           range=0, cooldown=16)),
        ("World Partition", "ultimate", dict(fx="vanish", fx2="nova", sfx="charge", sfx2="boom", power=12.0,
                                             radius=6, range=14, energy=100, cooldown=30, impact="major",
                                             cutscene=True, scale=3)),
    ]),
    "life": ("life", [
        ("Bloom of Life", "heal", dict(fx="bloom", sfx="heal", heal=0.8, radius=7, range=0, cooldown=10)),
        ("Seal of Life", "buff", dict(fx="aura", sfx="heal", self_status="invincible", status_time=5, range=0,
                                      cooldown=20)),
        ("Life Theft", "beam", dict(fx="beam", fx2="bloom", sfx="nature", power=1.4, hits=6, interval=0.12,
                                    duration=0.8, radius=0.8, range=10, cooldown=5)),
        ("Genesis", "field", dict(fx="bloom", sfx="nature", power=0.6, hits=6, interval=0.3, duration=1.8,
                                  radius=4, range=10, status="poisoned", cooldown=9)),
        ("Thornwrath", "barrage", dict(fx="slash_big", fx2="bloom", sfx="swing", sfx2="hit_heavy", power=1.0,
                                       hits=5, interval=0.08, range=4, cooldown=1.6)),
        ("EITR Tide", "ultimate", dict(fx="bloom", fx2="nova", sfx="charge", sfx2="boom", power=11.0, radius=6,
                                       range=14, energy=100, cooldown=30, impact="major", cutscene=True,
                                       scale=3)),
    ]),
    "death": ("death", [
        ("Death Touch", "strike", dict(fx="slash_big", fx2="hit", sfx="dark", sfx2="hit_heavy", power=3.0,
                                       range=3, cooldown=2)),
        ("Mark of Death", "projectile", dict(fx="orb", fx2="vanish", sfx="dark", power=2.0, radius=1.4,
                                             range=12, status="cursed", status_time=8, cooldown=5)),
        ("Reaper's Field", "field", dict(fx="circle", sfx="dark", power=0.9, hits=8, interval=0.3, duration=2.4,
                                         radius=4, range=10, status="poisoned", cooldown=9)),
        ("Underworld Gate", "nova", dict(fx="vanish", fx2="nova", sfx="dark", power=2.4, radius=4, range=6,
                                         cooldown=10, scale=1.8)),
        ("Curse of Death", "rain", dict(fx="pillar", fx2="vanish", sfx="dark", power=1.6, hits=8,
                                        interval=0.1, radius=4.5, range=12, status="cursed", cooldown=11)),
        ("Final Verdict", "ultimate", dict(fx="pillar", fx2="nova", sfx="charge", sfx2="boom", power=14.0,
                                           radius=6, range=14, energy=100, cooldown=32, impact="major",
                                           cutscene=True, scale=3)),
    ]),
}

for shade, (element, entries) in SHADES.items():
    ids = []
    for i, (name, arch, params) in enumerate(entries):
        ids.append(move(f"shade_{shade}_{i}", name=name, archetype=arch, **params))
    moveset(f"gi_shade_{shade}", 5, element, ids)

# ------------------------------------------------------------------ Tier 6: Moons

LUNAR = [
    ("Moonbeam", "beam", dict(fx="beam", fx2="burst", sfx="moon", power=1.8, hits=6, interval=0.12, duration=0.9,
                              radius=0.9, range=12, cooldown=3)),
    ("Waxing Crescent", "barrage", dict(fx="slash_big", fx2="hit", sfx="swing", sfx2="hit_heavy", power=1.1,
                                        hits=6, interval=0.07, range=4, cooldown=1.4)),
    ("Moonlit Veil", "teleport", dict(fx="vanish", fx2="slash_big", sfx="moon", sfx2="hit_heavy", power=2.4,
                                      range=14, cooldown=5)),
    ("Tidal Pull", "field", dict(fx="wave", sfx="splash", power=0.8, hits=6, interval=0.3, duration=1.8,
                                 radius=4.5, range=12, status="slowness", cooldown=9)),
    ("Lunar Grace", "heal", dict(fx="aura", sfx="heal", heal=0.6, radius=7, range=0, cooldown=12)),
]
moon_ids = [move(f"moon_{i}", name=n, archetype=a, element="lunar", **p) for i, (n, a, p) in enumerate(LUNAR)]
moveset("gi_moon_goddess", 6, "lunar", moon_ids)
moveset("gi_trilunar", 6, "lunar", moon_ids + [
    move("trilunar_law", name="Law of the Moons", archetype="rain", element="lunar", fx="orb", fx2="nova",
         sfx="moon", sfx2="boom", power=2.4, hits=10, interval=0.1, radius=5, range=14, cooldown=12),
    move("trilunar_rise", name="Three Moons Rise", archetype="ultimate", element="lunar", fx="nova", fx2="burst",
         sfx="moon", sfx2="boom", power=15.0, radius=7, range=16, status="slowness", energy=100, cooldown=32,
         impact="major", cutscene=True, scale=3.4),
])

# ------------------------------------------------------------------ Tier 7: Heavenly Principles

moveset("gi_heavenly_principles", 7, "divine", [
    move("heaven_nail", name="Celestial Nail", archetype="pillar", fx="pillar", fx2="nova", sfx="thunder",
         sfx2="boom", power=6.0, radius=3, range=16, cooldown=6, impact="minor", scale=2.4),
    move("heaven_judgement", name="Divine Judgement", archetype="rain", fx="pillar", fx2="burst", sfx="thunder",
         power=2.6, hits=12, interval=0.08, radius=5, range=16, cooldown=8),
    move("heaven_erasure", name="Erasure", archetype="beam", fx="beam", fx2="vanish", sfx="space", power=2.6,
         hits=8, interval=0.1, duration=0.9, radius=1, range=16, cooldown=5),
    move("heaven_rewrite", name="Irminsul Rewrite", archetype="field", fx="circle", sfx="time", power=1.2,
         hits=8, interval=0.25, duration=2, radius=5, range=16, status="stunned", cooldown=10),
    move("heaven_law", name="Primordial Law", archetype="nova", fx="nova", fx2="burst", sfx="boom", power=3.0,
         radius=5, range=6, cooldown=9, scale=2),
    move("heaven_light", name="Light Realm Descent", archetype="ultimate", fx="pillar", fx2="nova",
         sfx="charge", sfx2="boom", power=20.0, radius=8, range=18, energy=100, cooldown=35, impact="major",
         cutscene=True, scale=3.6),
])

moveset("gi_dormant_principles", 5, "divine", [
    "heaven_nail", "heaven_judgement", "heaven_erasure", "heaven_rewrite", "heaven_law",
])


def main():
    out = {"movesets": SETS, "moves": MOVES}
    path = os.path.join(ROOT, "data", "moves.json")
    with open(path, "w") as f:
        json.dump(out, f, indent=2)
    print(f"{len(SETS)} movesets, {len(MOVES)} moves -> {path}")


if __name__ == "__main__":
    main()
