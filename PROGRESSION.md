# The Crucible Path: levelling, training and ascension

Every powered unit walks its own **Path**, separate from WorldBox's unit level and saved with the world.
It is the mod's own system. Heroes grow from what they actually do (facets), break through to new
**Stages** only by surviving a **Crucible** in battle, and carry **marks** from their deeds.
The code is `Code/Progression/Path.cs`.

## Facets: you become what you do

Four facets, each running from 0 to 40. They grow from use, not from a shared XP pool:

| Facet | Grows from | Effect per level |
|---|---|---|
| **Vigor** | taking hits (1.5 per hit, at most every 0.4 s), drilling in the city (60/yr), kills (4) | +4% max health |
| **Edge** | landing techniques (2, at most every 0.3 s), dummy training (90/yr), kills (8 + 10 × the victim's tier) | +3% technique power |
| **Ward** | guarding a blow (3), being hit mid-technique (1.5), protecting an ally (15), sparring (15–20) | 1% less damage taken (up to 40%), +0.5% guard chance |
| **Flow** | dashes, blinks and leaps (3), duels (15–30), sparring (15–25) | 1.2% shorter cooldowns (down to half), +0.5% speed |

The XP for the next level of a facet is `20 + 6 × level + level² / 2`: 20 at level 0, 202 at level 20 and
1,000 at level 39. Training rates are per in-game year (60 seconds of world time), so game speed doesn't
change how fast units progress. Training XP and battle XP each have a multiplier in the settings.

**Training boosts.** A mentor in the same city (someone 10+ facet levels ahead) gives ×1.5, and a
Training Ground (god power) gives ×2. Drilling and dummy work also drill a random known technique,
which counts toward its mastery.

## Stages

| Stage | Name | Facet cap | Health | Damage | Techniques |
|---|---|---|---|---|---|
| I | Kindled | 8 | ×1 | ×1 | Level I |
| II | Forged | 16 | ×1.6 | ×1.3 | Level II |
| III | Tempered | 24 | ×2.5 | ×1.7 | Level II |
| IV | Hallowed | 32 | ×4 | ×2.2 | Level III |
| V | Paragon | 40 | ×6 | ×3 | Level III |

The health and damage multipliers (and Vigor's bonus) are applied to the unit's own stats whenever
WorldBox recalculates them, so they show in the unit window. Together with Vigor, a Paragon with
Vigor 40 has **15.6×** the health its traits give it.

**Higher tiers start further along.** The first time the mod sees a powered unit, it lifts it to its
tier's Stage, with facets at the start of it: Visions Kindled, Exalted Forged, Archons Tempered, Dragon
Sovereigns and Shades Hallowed, Moons, Nibelung and the Heavenly Principles Paragon. A unit whose tier
rises (an Exalted, a new Archon) is lifted the same way.

## Primed, and the Crucible

When **two facets reach the Stage cap**, the unit is **primed** (a *PRIMED* label pops up and the panel
shows it). From then on training alone can't take it further.

Its next serious fight becomes its **Crucible**:
- it drops below 35% health;
- it enters a duel; or
- it faces five or more foes within four tiles.

A *CRUCIBLE* title, a moment of hit-stop and a spotlight. The unit flares with its element for 20 seconds.
If it is **still alive when the time runs out**, or **wins the duel**, it **breaks through**: a ceremony
(the world stops, rings close in, a pillar of light), a full heal, and the next Stage. If it dies, it dies.

A unit that stays primed for **15 years** without a Crucible breaks through quietly, by meditation.
Paragon is the summit: facets keep growing to 40, but there is no further Stage.

## Marks: deeds that leave a perk

| Mark | Earned by | Perk |
|---|---|---|
| Survivor | surviving below 10% health | last stands heal 40% instead of 20%; +5% health |
| Kingslayer | slaying a ruler | +15% damage against rulers and powered units |
| Duelist | winning 3 duels | +15% momentum in duels |
| Giantslayer | slaying a unit of a higher tier | +20% damage against higher tiers |
| Warden | protecting allies 3 times (bodyguard) | +10% guard chance |
| Reaper | 100 kills | every kill heals 2% |
| Unbowed | still standing after an enemy's God ability | 25% less damage from God abilities |
| Crowned | becoming a king or city leader | +10% health |

## Ordinary people and Visions

Mortals have **potential** (0–100). Drilling gives 18 a year, dummies 40, each kill 6, and Training
Grounds double training. At 100, a Vision may answer each year: base 8% a year, ×2 for kings, ×1.3–4 for
traits like *ambitious*, *chosen one*, *lucky* or *miracle born*, and ×6 in a desperate battle (under
25% health). The element follows the kingdom's Archon half the time.

## Beyond your tier

- **Exaltation:** a Vision bearer at Hallowed with 2 marks may become (10% a year):
  - an Abyss Herald, if touched by the Abyss;
  - a Fatui Harbinger, if it holds a Delusion or serves the Cryo Archon's kingdom (while fewer than 11 exist);
  - a Hexenzirkel Witch, if its intelligence is 15+;
  - otherwise an Adeptus.
- **Godhood:** a Paragon Exalted with 4 marks may claim an empty Archon throne (5% a year).
- **Succession:** when an Archon dies, the Gnosis passes to the best successor: a bearer of the same
  element in the same kingdom, then any powered unit of the kingdom, then any bearer of the element anywhere.

## God powers

| Power | Effect |
|---|---|
| Inspect | Opens the unit window with its Path panel |
| Grant Vision | Awakens a Vision; on a bearer, +3 to every facet |
| Shift Element | Cycles a bearer's element |
| Grant Delusion | Gives a Fatui Delusion |
| Temper | +5 to every facet, up to the Stage cap (can prime the unit) |
| Grant Mark | A mark the unit hasn't earned yet |
| Crown Archon | Crowns the unit Archon of its element, taking the Gnosis from its holder |
| Training Ground | Marks a place where units train ×2 (click again to remove) |

## Settings (group *Path*)

On/off, facet growth from training, facet growth from battle, sparring, Vision awakening and its rate,
succession, Exaltation, Godhood, legend announcements.

## Saved data

Everything lives in each unit's custom data, under `gi_p_*` keys: Stage, the four facet levels and XP,
marks, when it was primed, kills, duels and spars won, rescues and training sessions. Mortal potential
is `gi_potential`. A Crucible in progress is not saved: reloading ends it without a breakthrough.
