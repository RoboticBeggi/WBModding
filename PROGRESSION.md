# Ley Line Resonance: levelling, training and ascension

Every powered unit has its own **Resonance**, separate from WorldBox's unit level and saved with the world.

## Ranks and Breakthroughs

- **Rank 1 → 90.** Rank multiplies move damage: rank 46 hits twice as hard as rank 1, and rank 90 nearly three times as hard.
- **XP to the next rank** = 30 + 4 × rank + rank² / 4. That's 34 XP at rank 1, 210 at rank 20, 1,170 at rank 60 and 2,370 at rank 89.
- **Breakthroughs** at ranks 20, 40, 50, 60, 70 and 80. XP stops counting until the unit passes a Trial:

| Breakthrough | Trial |
|---|---|
| 20 | Finish 3 training sessions (one session = a quarter year of training) |
| 40 | Win a spar or a duel, or defeat 5 foes |
| 50 | Defeat 10 foes |
| 60 | Defeat an elite (a tier 2+ unit or a dragon), or win a duel |
| 70 | Win 2 duels |
| 80 | Earn 300 renown, or wait a year at the gate while your kingdom has an Archon (an Archon's blessing) |

Each passed Trial raises the **Ascension phase** (★ to ★★★★★★). The unit gets the matching *Ascension*
trait, with growing stats, plus an ascension celebration: a light pillar, a ring and a sound.

**Higher tiers start further along.** The first time the mod sees a unit with a high-tier power, it
starts at a fitting rank: Exalted 25, Archon 40, Sovereign 55, Shade 70, Moons and Nibelung 85,
Heavenly Principles 90.

## Training

The mod checks every unit in the world in turn, about 250 units every half second. Rates below are XP per
in-game year (60 seconds of world time), so game speed doesn't change how fast units progress.

| Activity | XP / year | When |
|---|---|---|
| **Drills** | 400 | A powered unit in its own city, not fighting. Small slashes in its element's colour |
| **Training dummy** | 800 | While doing WorldBox's own training-dummy task. Sparks fly |
| **Sparring** | 35–45 per bout | Sometimes two powered units in the same city spar: real-looking exchanges, no damage, a "SPAR" label and a winner |
| **Mentor** | ×1.5 | Someone at least 5 ranks higher trains in the same city |
| **Training Ground** | ×2 | Inside an area you marked with the Training Ground power |
| **Above rank 60** | ×0.5 | Training alone only goes so far; the top ranks need real fights |
| Combat: using a move | +1.5 | |
| Combat: a kill | 20 + 2 × victim rank + 30 × victim tier | |
| Duel | win +80, loss +30 | |

A dedicated trainer at a dummy on a Training Ground with a mentor reaches rank 40 in about 4 years and
rank 60 in about 11. Someone who only drills takes a lifetime. Tune both rates with the **Training XP**
and **Combat XP** sliders.

## Talents

Every powered unit has three talents: **Strike** (strikes and barrages), **Skill** (most other moves) and **Burst** (moves that cost energy, and ultimates).

- One talent point every 3 ranks. The talent cap rises with Ascension: 2 at phase 0, 10 at phase 6.
- Points are spent automatically by personality: **aggressive** units favour Burst, **rational** ones Skill, and Strike comes second for everyone.
- Each level adds **+8% damage** and **3% shorter cooldowns** to moves of that category.

## Constellations

Constellations C1–C6 come from **fateful moments**. Each different moment lights one more star:

- Surviving at under 10% health in a fight
- Slaying a king
- Winning a duel
- Defeating an elite
- Defeating 50 foes
- Earning 500 renown
- Becoming a ruler
- Reaching Ascension phase 3
- Reaching Ascension phase 6

**C3** and **C5** each add +2 to every talent (up to 13). **C6** upgrades the unit's Burst: ×1.5 power,
bigger effects, and an impact frame every time.

## Vision awakening

Ordinary people build **potential** by training (more at a dummy or on a Training Ground) and by fighting.
At 100 potential, each year there's a chance the gods answer their wish and they awaken a Vision.

- **More likely for:** kings (×2), and units with traits like *ambitious*, *chosen one*, *strong*, *lucky*, *genius* or *miracle born*.
- **Far more likely (×6) when the wish is desperate:** in the middle of a fight, below 25% health.
- **Element:** half the time it's the element of the kingdom's Archon; otherwise fate decides.

Every awakening gets a celebration, a banner and a world-history entry.

## Beyond: ascension paths

| Path | How |
|---|---|
| **Archon succession** | When an Archon dies, the Gnosis passes to the highest-rank Vision holder of that element in the same kingdom. If there's none, it goes to the strongest power in the kingdom, then to any Vision holder of the element anywhere |
| **Exaltation** | A Vision holder at rank 70+, Ascension ★5+ and C4+ may become Exalted (about a 10% chance a year). They become an **Abyss Herald** if Abyss-touched, a **Fatui Harbinger** if they have a Delusion or serve the Cryo Archon (Harbingers are capped at 11), a **Hexenzirkel Witch** if very intelligent, otherwise an **Adeptus** |
| **Godhood** | An Exalted at rank 90 and C6 may claim an **empty Archon throne** (about a 5% chance a year), with a cutscene: "A NEW ARCHON" |

## God powers (Teyvat tab, second row)

| Power | What it does |
|---|---|
| Teyvat Legends | Lists the 25 mightiest powers in the world. Click one to jump to them and open their card |
| Inspect Resonance | Click a unit: its rank, XP, Breakthrough Trial, Ascension, talents, Constellation, record and moves |
| Grant Vision | Awakens a Vision. On a Vision holder, +5 ranks |
| Shift Element | Cycles a Vision holder's element |
| Grant Delusion | Gives a Fatui Delusion |
| Ascend | +10 ranks (awakens a Vision first if needed) |
| Bestow Constellation | +1 Constellation |
| Crown Archon | Makes the unit Archon of its element. The Gnosis leaves its current holder |
| Training Ground | Marks an area where units train twice as fast. Click it again to remove it. Saved with the world |

## Settings (mod settings → Resonance)

- Resonance on/off
- Training XP and Combat XP multipliers
- Sparring on/off
- Vision awakening on/off and its rate
- Succession, Exaltation and Godhood on/off
- Announcements for awakenings and ascensions on/off
