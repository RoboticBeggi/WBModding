# Movesets, duels and cinematics

Every Teyvat power trait comes with a moveset of five to seven moves. A unit uses the moveset of the
**highest-tier** Teyvat trait it holds, so giving a Vision holder a Gnosis upgrades its whole kit.
Moves get stronger as a unit tiers up in two ways: its trait stats raise its base damage, and
higher-tier moves have bigger power values, more hits, wider areas, impact frames and cutscenes.

## How fights play out

- **Moves:** whenever a powered unit attacks, it may use a ready move that fits the range. A burst is
  used as soon as it has 100 energy. Energy comes from attacking and landing hits.
- **Elemental reactions:** a hit leaves an element aura on its target for 4 seconds. A different element
  landing on it triggers a reaction and a floating label:
  - Vaporize, Melt
  - Overloaded (explodes nearby)
  - Superconduct (slows)
  - Electro-Charged
  - Frozen
  - Burning
  - Bloom (a seed bursts after a moment)
  - Aggravate
  - Swirl (spreads the element to nearby enemies)
  - Crystallize (the attacker gets a shield)
- **Duels:** when two powered units fight on screen, there's a chance (25% by default, at most 2 at a
  time) the fight turns into a choreographed duel. Both fighters are locked in place and trade 4–9 beats:
  | Beat | What happens |
  |---|---|
  | **Exchange** | Blades meet over and over, with sparks at the centre; both take chip damage |
  | **Barrage** | A 6–10 hit flurry ending in a knockback |
  | **Parry** | A heavy swing is parried with a burst of sparks, then countered |
  | **Weave** | Three strikes miss as the defender sidesteps, leaving afterimages; then the defender hits back |
  | **Combo** | Strike, blink behind, flurry, launcher, finisher, ending in "COMBO xN" |

  The healthier fighter finishes with their strongest move, followed by an impact frame.
- **Impact frames** (strong moves and duel finishers):
  1. Hit-stop
  2. A white flash
  3. A black frame with radial speed lines
  4. An element-tinted frame
  5. A camera punch and a screen shake
- **Cutscenes** (ultimates of Archons and above, and major Chronicle events): letterbox bars, the camera
  flies in, time slows down, and a title card shows the move and its caster. The ultimate lands at the climax.
- **Dragon Sovereigns** spawned by the Chronicle are real dragons, recoloured to their element. They
  breathe, roar and fight with their moveset. If you give a Sovereign trait to a non-dragon, it glows
  with the element's aura instead.

Effects only play near the camera, so far-off battles don't cost performance or flood the screen;
damage still applies everywhere. Every part can be turned off or tuned under **Combat** and
**Cinematics** in the mod settings.

## All movesets

### Tier 1 · Vision / Delusion / Allogene

| Trait | Moves (archetype) |
|---|---|
| `gi_vision_anemo` | **Gale Cut** (strike) → **Wind Spiral** (field) → **Updraft Leap** (dash) → **Windbarrier** (shield) → **Tempest Eye** (field, burst) |
| `gi_vision_geo` | **Stone Fist** (strike) → **Jade Stele** (field) → **Rockslide Dash** (dash) → **Jade Shield** (shield) → **Meteor Fall** (rain, burst) |
| `gi_vision_electro` | **Spark Slash** (barrage) → **Thunder Mark** (rain) → **Lightning Step** (teleport) → **Static Field** (field) → **Thunderclap Musou** (beam, burst) |
| `gi_vision_dendro` | **Thorn Lash** (strike) → **Seed of Skandha** (projectile) → **Vine Swing** (dash) → **Verdant Bloom** (heal) → **Garden of Wisdom** (field, burst) |
| `gi_vision_hydro` | **Tide Slash** (strike) → **Water Prison** (projectile) → **Wave Surf** (dash) → **Rain Ward** (heal) → **Deluge** (nova, burst) |
| `gi_vision_pyro` | **Ember Strike** (strike) → **Blazing Burst** (projectile) → **Flame Dash** (dash) → **Warmth Ward** (buff) → **Phoenix Descent** (ultimate, burst) |
| `gi_vision_cryo` | **Frost Edge** (strike) → **Ice Lotus** (field) → **Ice Glide** (dash) → **Frost Armor** (shield) → **Glacial Waltz** (field, burst) |
| `gi_delusion` | **Delusion Bolt** (projectile) → **Overdrive** (buff) → **Mirror Maiden's Veil** (teleport) → **Cicin Swarm** (barrage) → **Life-Burn Detonation** (nova, burst · impact frame) |
| `gi_allogene` | **Foreign Blade** (barrage) → **Starfell Sword** (projectile) → **Traveler's Step** (dash) → **Elemental Resonance** (nova) → **Abyss Resonance** (field) → **Gust of Another World** (ultimate, burst · impact frame) |

### Tier 2 · Exalted

| Trait | Moves (archetype) |
|---|---|
| `gi_adeptus` | **Adeptal Spear Dive** (dash) → **Yaksha Mask** (buff) → **Cloud Step** (teleport) → **Karma Purge** (nova) → **Adeptal Arts: Jade Rain** (rain) → **Divine Beast Form** (ultimate, burst · impact frame) |
| `gi_harbinger` | **Harbinger's Edge** (barrage) → **Frost Execution** (dash) → **Snezhnayan Decree** (nova) → **Foul Legacy** (buff) → **Delusion Volley** (rain) → **Delusion Overload** (ultimate, burst · impact frame) |
| `gi_hexenzirkel` | **Arcane Sigil** (projectile) → **Hex** (projectile) → **Witch's Flight** (teleport) → **Time-Lost Brew** (heal) → **Grand Ritual** (rain) → **Witchcraft Tempest** (ultimate, burst · impact frame) |
| `gi_abyss_herald` | **Abyssal Rend** (barrage) → **Shadow Step** (teleport) → **Corrosion** (field) → **Abyssal Rift** (field) → **Abyss Mantle** (shield) → **Hilichurl Curse** (ultimate, burst · impact frame) |
| `gi_demon_god` | **Divine Fury** (barrage) → **God's Domain** (field) → **Calamity** (rain) → **War Cry** (nova) → **Demon Avatar** (ultimate, burst · impact frame) |

### Tier 3 · Archons

| Trait | Moves (archetype) |
|---|---|
| `gi_gnosis_anemo` | **Divine Gale Cut** (barrage) → **Divine Wind Spiral** (field) → **Divine Weather** (rain) → **Gnosis Resonance** (heal) → **Divine Avatar** (buff) → **Wind's Grand Ode** (ultimate, burst · impact frame · cutscene) |
| `gi_gnosis_geo` | **Divine Stone Fist** (barrage) → **Divine Jade Stele** (field) → **Divine Weather** (rain) → **Gnosis Resonance** (heal) → **Divine Avatar** (buff) → **Planet Befall** (ultimate, burst · impact frame · cutscene) |
| `gi_gnosis_electro` | **Divine Spark Slash** (barrage) → **Divine Thunder Mark** (field) → **Divine Weather** (rain) → **Gnosis Resonance** (heal) → **Divine Avatar** (buff) → **Musou Isshin** (ultimate, burst · impact frame · cutscene) |
| `gi_gnosis_dendro` | **Divine Thorn Lash** (barrage) → **Divine Seed of Skandha** (field) → **Divine Weather** (rain) → **Gnosis Resonance** (heal) → **Divine Avatar** (buff) → **Illusory Heart** (ultimate, burst · impact frame · cutscene) |
| `gi_gnosis_hydro` | **Divine Tide Slash** (barrage) → **Divine Water Prison** (field) → **Divine Weather** (rain) → **Gnosis Resonance** (heal) → **Divine Avatar** (buff) → **Grand Trial** (ultimate, burst · impact frame · cutscene) |
| `gi_gnosis_pyro` | **Divine Ember Strike** (barrage) → **Divine Blazing Burst** (field) → **Divine Weather** (rain) → **Gnosis Resonance** (heal) → **Divine Avatar** (buff) → **Sunfall Anthem** (ultimate, burst · impact frame · cutscene) |
| `gi_gnosis_cryo` | **Divine Frost Edge** (barrage) → **Divine Ice Lotus** (field) → **Divine Weather** (rain) → **Gnosis Resonance** (heal) → **Divine Avatar** (buff) → **Everwinter** (ultimate, burst · impact frame · cutscene) |

### Tier 4 · Dragon Sovereigns

| Trait | Moves (archetype) |
|---|---|
| `gi_sovereign_anemo` | **Talon Sweep** (barrage) → **Dragon Breath** (beam) → **Primal Roar** (nova) → **Sovereign's Scales** (shield) → **Holy Soil** (field) → **Elemental Cataclysm** (ultimate, burst · impact frame · cutscene) |
| `gi_sovereign_geo` | **Talon Sweep** (barrage) → **Dragon Breath** (beam) → **Primal Roar** (nova) → **Sovereign's Scales** (shield) → **Holy Soil** (field) → **Elemental Cataclysm** (ultimate, burst · impact frame · cutscene) |
| `gi_sovereign_electro` | **Talon Sweep** (barrage) → **Dragon Breath** (beam) → **Primal Roar** (nova) → **Sovereign's Scales** (shield) → **Holy Soil** (field) → **Elemental Cataclysm** (ultimate, burst · impact frame · cutscene) |
| `gi_sovereign_dendro` | **Talon Sweep** (barrage) → **Dragon Breath** (beam) → **Primal Roar** (nova) → **Sovereign's Scales** (shield) → **Holy Soil** (field) → **Elemental Cataclysm** (ultimate, burst · impact frame · cutscene) |
| `gi_sovereign_hydro` | **Talon Sweep** (barrage) → **Dragon Breath** (beam) → **Primal Roar** (nova) → **Sovereign's Scales** (shield) → **Holy Soil** (field) → **Elemental Cataclysm** (ultimate, burst · impact frame · cutscene) |
| `gi_sovereign_pyro` | **Talon Sweep** (barrage) → **Dragon Breath** (beam) → **Primal Roar** (nova) → **Sovereign's Scales** (shield) → **Holy Soil** (field) → **Elemental Cataclysm** (ultimate, burst · impact frame · cutscene) |
| `gi_sovereign_cryo` | **Talon Sweep** (barrage) → **Dragon Breath** (beam) → **Primal Roar** (nova) → **Sovereign's Scales** (shield) → **Holy Soil** (field) → **Elemental Cataclysm** (ultimate, burst · impact frame · cutscene) |

### Tier 5 · Shades

| Trait | Moves (archetype) |
|---|---|
| `gi_shade_time` | **Moment's Halt** (field) → **Accelerate** (buff) → **Age** (projectile) → **Temporal Echo** (barrage) → **Rewind** (heal) → **Eternal Recurrence** (ultimate, burst · impact frame · cutscene) |
| `gi_shade_space` | **Fold** (teleport) → **Spatial Tear** (beam) → **Distance Lock** (field) → **Compression** (nova) → **Spatial Barrier** (shield) → **World Partition** (ultimate, burst · impact frame · cutscene) |
| `gi_shade_life` | **Bloom of Life** (heal) → **Seal of Life** (buff) → **Life Theft** (beam) → **Genesis** (field) → **Thornwrath** (barrage) → **EITR Tide** (ultimate, burst · impact frame · cutscene) |
| `gi_shade_death` | **Death Touch** (strike) → **Mark of Death** (projectile) → **Reaper's Field** (field) → **Underworld Gate** (nova) → **Curse of Death** (rain) → **Final Verdict** (ultimate, burst · impact frame · cutscene) |
| `gi_dormant_principles` | **Celestial Nail** (pillar) → **Divine Judgement** (rain) → **Erasure** (beam) → **Irminsul Rewrite** (field) → **Primordial Law** (nova) |

### Tier 6 · Moons & Nibelung

| Trait | Moves (archetype) |
|---|---|
| `gi_nibelung` | **Seven Breaths** (beam) → **King's Talon** (barrage) → **Abyssal Flame** (field) → **Roar of the Dragon King** (nova) → **Moon-Making** (rain) → **Funerary Flame** (ultimate, burst · impact frame · cutscene) |
| `gi_moon_goddess` | **Moonbeam** (beam) → **Waxing Crescent** (barrage) → **Moonlit Veil** (teleport) → **Tidal Pull** (field) → **Lunar Grace** (heal) |
| `gi_trilunar` | **Moonbeam** (beam) → **Waxing Crescent** (barrage) → **Moonlit Veil** (teleport) → **Tidal Pull** (field) → **Lunar Grace** (heal) → **Law of the Moons** (rain) → **Three Moons Rise** (ultimate, burst · impact frame · cutscene) |

### Tier 7 · Heavenly Principles

| Trait | Moves (archetype) |
|---|---|
| `gi_heavenly_principles` | **Celestial Nail** (pillar) → **Divine Judgement** (rain) → **Erasure** (beam) → **Irminsul Rewrite** (field) → **Primordial Law** (nova) → **Light Realm Descent** (ultimate, burst · impact frame · cutscene) |

## Editing moves

Copy `data/moves.json` to `data/moves.custom.json` and edit it, or edit `tools/data/gen_moves.py` and
re-run it. Each move has these fields:

| Field | Meaning |
|---|---|
| `archetype` | `strike`, `barrage`, `dash`, `teleport`, `projectile`, `beam`, `nova`, `field`, `rain`, `pillar`, `shield`, `buff`, `heal`, `ultimate` |
| `element` | `self` (the caster's element), an element name, `random`, or `abyss` / `lunar` / `divine` / `death` / `life` / `time` / `space` / `delusion` |
| `fx`, `fx2` | VFX sets: `slash` `slash_big` `hit` `spark` `burst` `nova` `orb` `beam` `pillar` `tornado` `spikes` `bloom` `wave` `shield` `streak` `vanish` `circle` `aura` |
| `sfx`, `sfx2` | Sounds in `GameResources/gi/sfx/combat/` |
| `power` | Damage per hit, as a multiple of the caster's damage stat |
| `range`, `radius` | In tiles |
| `hits`, `interval`, `duration` | Number of hits or ticks, the gap between them, and how long a field or beam lasts |
| `cooldown`, `energy` | Seconds before reuse; energy needed (bursts use 100) |
| `status`, `status_time`, `self_status` | Game statuses to apply, e.g. `burning` `frozen` `slowness` `stunned` `poisoned` `cursed` `shield` `powerup` `invincible` `caffeinated` |
| `impact` | `none`, `minor` or `major` (impact frame) |
| `cutscene` | `true` plays a cutscene before the move |
| `scale` | VFX size multiplier |
| `heal` | For `heal`: fraction of max health restored to allies in the radius |

`movesets` maps each trait id to `tier`, `element` and its list of move ids.
