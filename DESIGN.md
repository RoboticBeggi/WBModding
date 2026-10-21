# Teyvat Chronicle — a Genshin Impact chronology mod for WorldBox

Research notes and design plan, revision 3. The mod makes WorldBox play out Teyvat's history, from
the Old World of the Dragon Sovereigns to the present-day Snezhnaya crisis. Powers are **traits only**: no new
units or races. You can give any trait to any creature. Each power tier has its own moveset, its own
art and its own sound, and units grow stronger by **training**.

> Status: v0.1.0 is built: the Chronicle engine, 39 power traits with icons, and announcements with sounds.
> It compiles against the game DLL but hasn't been run in-game yet. Movesets and levelling are next.
> Game API names were checked against the `Assembly-CSharp-Publicized.dll` and `strings.dll`
> that ship with NeoModLoader `master` (NML 1.2.1.1, commit dated 2026-10-19).

**Changes in revision 3**
- The timeline was re-researched from the Genshin Impact Wiki's master timeline and its region
  History pages. It is now 45 events in 11 eras, starting at the Old World. See [CHRONICLE.md](CHRONICLE.md).
- The Chronicle can start from any event, can be edited completely (`data/chronicle.custom.json`), and
  every era, effect type and announcement channel can be turned on or off.
- Platform confirmed: PC (Steam).

**Changes in revision 2**
- Up to date with Genshin as of October 2026: Snezhnaya, Versions 7.0 and 7.1.
- Added the Trilunar Authority and the Four Shades as power tiers.
- New levelling system, separate from WorldBox's own levels, with training as the main way to progress.
- Every tier has 5 or more moves, and the moves get stronger as the unit moves up a tier.
- The Chronicle's pacing can be set (Lore / Normal / Fast / Custom, all within a few hundred years), and every event comes with a big global announcement.
- All art and audio is made for the mod: icons, animations, effects (VFX) and sound effects (SFX).

---

## Part 1 — Genshin Impact research

### 1.1 Where the story is now (October 2026)

| Version | Released | What it added |
|---|---|---|
| 6.x Nod-Krai | 2025–26 | Columbina takes on the **Trilunar Authority**. Moon-based "Lunar" reactions. Dottore is defeated. |
| **7.0 "Everwinter Without Mercy"** | 12 Aug 2026 | **Snezhnaya** opens (Chapter VII, Acts I–II). Odette (5★ Cryo sword), Alyosha (4★ Electro polearm). The Eye of Graeae (Prime Ice). Pierro takes a lead role. Ronova, the Ruler of Death, first appears. |
| **7.1 "A Rekviem for the Underworld"** | 23 Sep 2026 | Acts III–IV. Vesna (5★ Anemo, Stellar-Swirl), Vodyanitsa (5★ Hydro). The Traveler meets the **Tsaritsa**. Ronova is the final boss. |
| 7.2 | ~4 Nov 2026 (estimate; leaks only) | Mitya (5★ Electro, Stellar-Conduct) and Valeriy are rumoured. |

**What the Snezhnaya story revealed (7.0–7.1):**
- **Where the Gnoses came from.** Long ago the Tsaritsa was a court herald. The Belyi Tsar persuaded her
  and the **Third Descender** to open a gateway to the Abyss. The Heavenly Principles erased her. The Third
  Descender sacrificed himself and **his remains became the seven Gnoses**. His terms: the Tsaritsa is restored
  and his Khaenri'ahn descendants are protected, or a curse falls on Celestia.
- **The Cataclysm, 500 years ago, broke that deal.** The curse went off and **forced Celestia into dormancy**.
  The Tsaritsa collects the Gnoses to stand up to Celestia and break the Third Descender's seal.
- **Ronova (Death) and Rhinedottir (now holding Life)** used a loophole: Ronova killed people and Rhinedottir
  revived them before Celestia's next inspection. Snezhnaya fell under an unauthorised **Curse of Death**.
- **Pierro** built the **Eye of the Fatui**, a new Delusion powered by life energy rather than burning the
  user's own life, with Arlecchino's Crimson Moon blood as a key part.
- **Vesna** carries a fragment of the Life authority and can pull out **Seals of Life** (memories that give life meaning).
- **The ending:** the Tsaritsa traps Rhinedottir in slowed time using six Gnoses. Celestia takes back Ronova's
  Death authority and Ronova vanishes. Ronova merged **EITR** (a Hyperborean substance that alters life) with
  Snezhnaya's **Nail of Retribution**. That threw the Celestial Nails out of order across Teyvat, and **Holy Soil**
  (land soaked in Light Realm energy, which favours dragons) is starting to spread.

### 1.2 The cosmic powers

**The Trilunar Authority.** The Dragon King **Nibelung** created **three Moon Goddesses** (fan wikis name
them Sonnet, Aria and Canon). They held the world's primordial laws, including life and death, before the
Heavenly Principles came. They stayed out of the dragons' war with Phanes. In Nod-Krai, **Columbina**
inherits the authority of all three moons and becomes the "Trilune" goddess.
*(The goddesses' names and some details come from fan wikis and aren't official.)*

**The Four Shades.** When Phanes (the Primordial One) invaded, he made four "luminous shadows" of himself.
Their powers had once belonged to the moons. They were revealed in the July 2025 teaser "The God's Limits":

| Shade | Authority | Status (Oct 2026) |
|---|---|---|
| **Istaroth** | Time | Unknown; often hinted at through the Traveler's dreams |
| **Asmoday** | Space | Seen in 7.1 with a hooded figure, probably the Abyss sibling |
| **Naberius** | Life | Gone; the Life authority is now held by **Rhinedottir** |
| **Ronova** | Death | Defeated in 7.1. Her authority was taken back by Celestia (or returned to the moons) |

**The Dragons.** There were seven elemental Dragon Sovereigns, and Nibelung was the Dragon King. Phanes
attacked while Nibelung was away from Teyvat. The war lasted over forty years and Nibelung died in the last battle.

### 1.3 Mortal power (unchanged from revision 1)

| Concept | What it is | Who has it |
|---|---|---|
| **Vision** | Gives one element to a mortal whose wish catches the gods' eye | Most characters |
| **Gnosis** | Connects an Archon to Celestia. Now known to be the Third Descender's remains. | The Seven Archons |
| **Delusion** | The Fatui's artificial Vision. It drains the user's life. | Fatui |
| **Eye of the Fatui** | Pierro's new Delusion, which runs on life energy instead (7.1) | Pierro |
| **Allogene** | Uses an element without a Vision | The Traveler, Aloy |
| **Abyss** | Corrupting power from outside Teyvat | Abyss Order |
| **Adeptus** | Liyue's divine beasts and Yakshas | Xiao, Cloud Retainer |

**Elements → nations → Archons:** Anemo/Mondstadt/Barbatos · Geo/Liyue/Morax ·
Electro/Inazuma/Beelzebul · Dendro/Sumeru/Buer · Hydro/Fontaine/Focalors · Pyro/Natlan/Mavuika ·
Cryo/Snezhnaya/the Tsaritsa.

**Reactions.**
- **Classic:** Vaporize, Melt, Overloaded, Superconduct, Electro-Charged, Frozen, Swirl, Crystallize, Burning, Bloom, Quicken.
- **Lunar (Nod-Krai):** Moon-boosted versions of some classic reactions, such as Lunar-Charged and Lunar-Bloom.
- **Stellar (Snezhnaya):** **Stellar-Conduct** (a stronger Superconduct) and **Stellar-Swirl** (a Cryo swirl that builds into a "Polestar" storm and then bursts).

### 1.4 Genshin's progression, for reference

Character level 1–90, with six Ascension "breakthroughs" at 20/40/50/60/70/80 · Talents 1–10 (up to 13) ·
Constellations C0–C6 · weapon Refinement R1–R5 · artifacts.

### 1.5 Timeline used by the mod (oldest first)

The full event list, the sources and how conflicting sources were resolved are in
[CHRONICLE.md](CHRONICLE.md). In short, using the wiki's own era names:

1. **The Old World.** The Seven Sovereigns rule under Nibelung. The Primordial Sea is the cradle of life.
   Nibelung raises the Three Moons and leaves Teyvat. The voyager Anacharsis warns him the world will end.
2. **When the Doves Held Branches.** The Heavenly Principles descend, take the Sovereigns' Authorities and
   create the Four Shades. The False Sky closes. After 40 years the Sovereigns fall.
3. **The Unified Civilization** (~6,000 years ago). Humanity is created (the "Ark's Opening"). Arcadia,
   Sal Vindagnyr, Lang-Gan, Ys, Hyperborea and Enkanomiya flourish, until humanity's pride angers the heavens.
4. **The War of Funerary Flame** (~6,000 years ago). Nibelung returns with the Abyss. The Eternal and
   Iridescent Moons are shattered and the Celestial Nails fall. History is forbidden and the Angels become
   Seelies. Khaenri'ah is founded.
5. **The Archon War** (~6,000–2,000 years ago). The seven thrones open. Guizhong falls and Liyue Harbor is
   founded (3,700). The Third Descender's sacrifice creates the Gnoses. Decarabian falls (2,600), King
   Deshret falls, and Xbalanque kills Xiuhcoatl. The Seven are crowned.
6. **The Age of the Seven** (2,000–500). Visions appear. Khaenri'ah's dynasties rule. Vennessa's rebellion.
7. **The Cataclysm** (500). The Abyss invades and Khaenri'ah falls. Makoto and Egeria die. The curse puts
   Celestia to sleep.
8. **After the Cataclysm.** Hilichurls spread, the Fatui and the Abyss Order are founded.
9. **The present day.** The Traveler crosses Mondstadt, Liyue, Inazuma, Sumeru, Fontaine and Natlan.
10. **Song of the Welkin Moon** (Nod-Krai, Versions Luna I–VIII). Columbina takes the Trilunar Authority.
11. **The Snezhnaya crisis** (7.0–7.1). Ronova's Curse of Death, the Tsaritsa's stand, and Holy Soil spreading.

---

## Part 2 — How to mod WorldBox effectively

### 2.1 Toolchain

- **[NeoModLoader (NML)](https://github.com/WorldBoxOpenMods/ModLoader)** is the mod loader. Install it from the
  Steam Workshop so it updates itself. On Android it runs through MelonLoader.
- **[ModTemplate](https://github.com/WorldBoxOpenMods/ModTemplate)** is the starting project. Its parts:
  - `mod.json`: the mod's name, author, version and icon
  - `ModClass.cs`: `BasicMod<ModClass>.OnModLoad()`, where the mod starts
  - `default_config.json`: settings the player can change in-game
  - `Locales/en.json`: all the mod's text
  - `GameResources/`: every image in this folder is loaded automatically
- **NML compiles the mod's C# itself** when the game starts. The `.csproj` is only for editor
  autocomplete. It points to `Assembly-CSharp-Publicized.dll`, where every private game member is public.
- **Hot reload:** implement `IReloadable` and mark methods `[Hotfixable]`. **Harmony** is bundled for patching game methods.
- **Debugging:** NML's log, plus Unity Explorer.
- **Tutorials:** the [NML wiki](https://github.com/WorldBoxOpenMods/ModLoader/wiki/ModTutorialHome). The advanced pages are in Chinese.

### 2.2 Game systems the mod uses (checked against the game DLL)

| Need | Game API |
|---|---|
| Powers as traits | `ActorTrait` → `AssetManager.traits`. `can_be_given = true` makes a trait show up in the trait editor, so you can give it to anyone. Hooks: `action_attack_target`, `action_special_effect` + `special_effect_interval`, `action_get_hit`, `action_death`, `addSpell()`, `addCombatAction()`, `base_stats[S.*]` |
| Moves | `CombatActionAsset` (`cooldown`, `chance`, `cost_mana`, `cost_stamina`, `action`) and `SpellAsset` (`cost_mana`, `chance`, `min_distance`, `cast_target`, `action`) |
| Auras and buffs | `StatusAsset` → `AssetManager.status`: `duration`, `action_interval`, `base_stats`, an animated `texture`, `opposite_status` |
| Projectiles | `ProjectileAsset`: `texture`, `frames`, `trail_effect_id`, `end_effect`, `sound_launch`, `sound_impact`, `shake_*`, `impact_actions` |
| Visual effects | `EffectAsset` (`sprite_path`, `time_between_frames`, `sound_launch`, `draw_light_area`) and `EffectsLibrary.spawnAt / spawnAtTile / spawnExplosionWave / spawnThunderFlash / spawnNukeFlash` |
| Sound | NML `SoundUtils.LoadCustomSound(wavName, x, y)` plays the mod's own `.wav` files. The game's FMOD sounds are available through `MusicBox.playSound`. |
| On-screen announcements | `WorldTip.showNowCenter / showNowTop` (a big centre or top banner), `WorldLogAsset` (an entry in the world history log), NML `WindowCreator` (a pop-up window) |
| Camera | `MapBox.locatePosition`, `MapBox.followUnit`, and the game's screen-shake fields |
| Saving per-unit progress | `actor.data.set("gi_xp", float)` and `actor.data.get(...)`. This custom data is saved with the world. |
| Training | The game already has training dummies (the `warrior_train_with_dummy` task and `order_training_dummy`). The mod adds its own training ticks on top. |
| Eras | `WorldAgeAsset` → `AssetManager.era_library` |
| God-power buttons | `GodPower` → `AssetManager.powers`, with NML `PowerButtonCreator` and `PowerTabNames` |

**Delegate signatures:** `AttackAction(pSelf, pTarget, pTile) → bool`, `WorldAction(pTarget, pTile)`,
`GetHitAction(pSelf, pAttackedBy, pTile)`. **Actor methods:** `addTrait`, `hasTrait`, `removeTrait`,
`addStatusEffect`, `getHit`, `addRenown`, `isKing`.

### 2.3 Good practice

- **Start every id with `gi_`** so nothing clashes with the game or other mods.
- **Keep data and code apart.** Moves, tiers and the timeline live in data tables, so you can rebalance without touching code.
- **Put balance numbers in `default_config.json`.**
- **Use the game's own hooks before Harmony patches.** Run timed logic on intervals, never every frame.
- **Pin the game and NML versions** the mod was tested on. WorldBox 0.50+ broke many older mods.
- **Ship in small steps** that are each playable on their own.

---

## Part 3 — Mod design

### 3.1 Giving out powers

- **Every power is a trait**, with `can_be_given = true`. Open any unit's trait editor and add it to a
  human, an orc, a sheep or a dragon. No new units or races are added.
- **Trait groups:** *Teyvat: Elements* (the 7 Visions, Delusion, Allogene) · *Teyvat: Exalted* ·
  *Teyvat: Divine* (Archons, Dragons) · *Teyvat: Primordial* (Shades, Trilunar, Heavenly Principles) ·
  *Teyvat: Progress* (ascension traits and constellations, so you can see a unit's progress).
- **God powers in a "Teyvat" tab:** Grant Vision (pick an element) · Grant Delusion · Crown Archon ·
  Bestow Gnosis · Awaken Dragon Sovereign · Invest Shade Authority · Trilunar Blessing · Training Ground
  (marks an area where units train faster) · Begin/Advance Chronicle.
- **Rules about who can hold what:** at most one Archon per element, one holder per Shade authority and one
  Trilunar holder. Each limit can be turned off in config, in case you want chaos.

### 3.2 Levelling: Ley Line Resonance

This is a separate system from WorldBox's own unit level. It's stored per unit (`gi_res_xp`,
`gi_res_rank`, `gi_talent_*`, `gi_const`) and saved with the world.

**Resonance Rank 1–90.** Each rank adds a small stat bonus and makes the unit's moves stronger.

**Breakthroughs (the Ascension gates).** At ranks **20 / 40 / 50 / 60 / 70 / 80**, XP stops counting until the unit passes a **Trial**:

| Gate | Trial (any one of these) | Reward |
|---|---|---|
| 20 | Finish 3 training sessions | Phase 1. Unlocks move 3. First passive. |
| 40 | Win a fight against an enemy of the same rank or higher | Phase 2. Unlocks move 4. |
| 50 | Survive a battle with 10 or more combatants | Phase 3. Talent cap raised to 8. |
| 60 | Defeat an "Elite" (a unit of tier 2 or higher) or a monster boss | Phase 4. Unlocks move 5 (the Burst). Second passive. |
| 70 | Win a duel against a unit of a different element | Phase 5. Talent cap raised to 10. |
| 80 | Earn a big renown milestone, or be blessed by an Archon | Phase 6. Unlocks the signature move upgrade. |

**Where XP comes from.** Training is the main source on purpose:

| Source | XP | Notes |
|---|---|---|
| **Training: drills** | +++ | Inside its own city and not fighting. Ticks every few seconds; uses stamina. |
| **Training: dummy** | ++++ | Hitting a training dummy. Hooks into the game's own dummy job. |
| **Training: sparring** | +++++ | Two units with Visions in the same city spar. Both gain XP; no one dies. Small sparks VFX. |
| **Training: mentorship** | ×1.5 multiplier | A unit of higher rank or tier in the same city (think Knights of Favonius). |
| **Training Ground** god power | ×2 multiplier | Marks an area you paint on the map. |
| Combat hits | + | Real fights |
| Kills | ++ to +++++ | Scales with the target's rank and tier |
| Using moves | + | Each move used |

**Training only goes so far.** Above rank 60, training XP is halved, so the top ranks need real fights.
Old units train more slowly, and the gain from repeating a drill shrinks over time.

**Talents.** Each unit has three talents, **Strike / Skill / Burst**, from 1 to 10. Each rank-up gives one
talent point. AI units spend points according to their personality (aggressive units favour Burst, for
example). Talents scale move damage and cut cooldowns.

**Constellations C0–C6** come from big moments: surviving at under 5% HP, killing a king, winning a war as
its general, a Trial passed with no injuries. **C3 and C5** give +2 talent levels. **C6** turns the Burst
into its "C6 form" and changes its VFX.

**Tiers stack on Resonance.** A unit keeps its Resonance rank when it moves up a tier. The new tier's moves
replace or extend the old ones, and use the same rank for scaling.

**Move power formula:**
`damage = move_base × tier_multiplier × (1 + rank/45) × (1 + 0.1 × talent) × reaction_bonus`.
Tier multipliers: **T1 ×1 · T2 ×2.5 · T3 ×6 · T4 ×10 · T5 ×16 · T6 ×22 · T7 ×30** (all set in config).

### 3.3 Power tiers and movesets

Every tier has **5 or more active moves** and 1–2 passives. Moves unlock as the unit's Resonance rank grows,
but a unit given a high tier directly gets all moves 1–3 straight away. Each move has its own VFX and SFX
(see 3.6). Abbreviations: **CD** = cooldown, **AoE** = area of effect, **DoT** = damage over time.

#### Tier 0 — Mortal
Plain WorldBox units. They can still **train**, and once they reach 15 training sessions they have a chance
to **Awaken** a Vision. Awakening is likelier for ambitious units, kings and people in danger: in Genshin,
Visions answer a strong wish.

#### Tier 1 — Vision Holder (seven elements)

Every element follows the same five-slot pattern, so the elements feel distinct but stay balanced.
**1 Strike** is a normal attack that applies the element. **2 Skill** is the signature short-cooldown move.
**3 Mobility** is a dash or movement move. **4 Utility** is a shield, heal or crowd-control move. **5 Burst**
needs Energy built up from hits and unlocks at Phase 4. Each element also has 2 passives (Phases 1 and 4).

| Element | 1 Strike | 2 Skill | 3 Mobility | 4 Utility | 5 Burst |
|---|---|---|---|---|---|
| **Anemo** | Gale Cut (wind hit) | Wind Spiral (pulls enemies in) | Updraft Leap (high jump; landing makes an AoE) | Windbarrier (blocks projectiles) | **Tempest Eye** (a moving tornado that swirls elements) |
| **Geo** | Stone Fist | Jade Stele (a pillar that pulses damage) | Rockslide Dash | Jade Shield (strong shield) | **Meteor Fall** (a meteor that petrifies, like Zhongli's) |
| **Electro** | Spark Slash | Thunder Mark (chain lightning between marked enemies) | Lightning Step (short teleport) | Static Field (slows and stuns) | **Thunderclap Musou** (a sweeping slash plus a lightning storm) |
| **Dendro** | Thorn Lash | Seed of Skandha (links enemies so they share damage) | Vine Swing | Verdant Bloom (heals allies) | **Garden of Wisdom** (an area that spawns Bloom cores) |
| **Hydro** | Tide Slash | Water Prison (bubble that holds the target) | Wave Surf (dash across water and land) | Rain Ward (heals over time) | **Deluge** (a tidal wave with knockback) |
| **Pyro** | Ember Strike | Blazing Burst (cone of fire) | Flame Dash (leaves a trail of fire) | Warmth Ward (resists cold; buffs attack) | **Phoenix Descent** (a dive-bomb explosion) |
| **Cryo** | Frost Edge | Ice Lotus (a spike field that slows) | Ice Glide | Frost Armor (shield that slows attackers) | **Glacial Waltz** (a blizzard that freezes everything inside) |

**Passives (examples):** Anemo: faster movement. Geo: Crystallize shields last longer. Pyro: kills cause
small explosions. Cryo: frozen enemies take extra damage.

#### Tier 1b — Delusion User (Fatui)

Very strong from the start, but costly: the unit **loses lifespan** with every move used.

| # | Move | Effect |
|---|---|---|
| 1 | Delusion Bolt | A ranged bolt of any element; cycles through elements |
| 2 | Overdrive | 20 seconds of +50% damage and speed; costs health |
| 3 | Mirror Maiden's Veil | Mirror clones that distract enemies |
| 4 | Cicin Swarm | Electro bats that chase targets |
| 5 | Life-Burn Detonation | Huge explosion; costs years of lifespan |
| P | **Eye of the Fatui** (upgrade) | Pierro's version. The lifespan cost disappears; moves run on life energy instead. |

**Allogene** works like a Vision holder but can **switch element** between fights, like the Traveler.
It gets **Abyss Resonance** as a 6th move.

#### Tier 2 — Exalted (four paths, 6 moves each)

| Path | Moves |
|---|---|
| **Adeptus** (Liyue) | 1 Adeptal Spear Dive · 2 Yaksha Mask (berserk: big damage, wears down HP) · 3 Cloud Step (flies over terrain) · 4 Karma Purge (removes curses and Abyss status from an area) · 5 Adeptal Arts: Jade Rain (a rain of jade blades) · 6 Divine Beast Form (a short transformation with large stat gains and a glowing aura) |
| **Fatui Harbinger** | 1 Harbinger's Edge (element plus the Delusion element) · 2 Foul Legacy (a stronger form) · 3 Snezhnayan Decree (debuffs enemies nearby) · 4 Command (Fatui allies nearby get stronger) · 5 Delusion Overload (takes in its own Delusion: an AoE nova) · 6 Harbinger Rank (a passive that grows with rank, numbered 11 → 1) |
| **Hexenzirkel Witch** | 1 Arcane Sigil · 2 Hex (curses the target with a random debuff) · 3 Witch's Flight · 4 Time-Lost Brew (reverses a nearby ally's injuries) · 5 Grand Ritual (casts a random weather or disaster) · 6 Familiar (a temporary magic orb that fights beside the witch) |
| **Abyss Herald** | 1 Abyssal Rend · 2 Shadow Step · 3 Corrosion (armour down plus DoT) · 4 Abyssal Rift (a small portal that pulls units in) · 5 Hilichurl Curse (turns nearby mortals feral for a short time) · 6 Abyss Mantle (a damage-reflecting shield) |

#### Tier 3 — Archon (Gnosis holder, 6 moves)

Shared god moves, plus a **unique Archon Burst** for each element.

| # | Move | Effect |
|---|---|---|
| 1 | Divine Element | Upgraded versions of all the Tier 1 moves for the unit's element (×6 power) |
| 2 | Gnosis Resonance | Heals and empowers all Vision holders of the unit's element in its kingdom |
| 3 | Grant Vision | Can awaken Visions in loyal followers (with a cooldown) |
| 4 | Divine Weather | Changes the weather in an area: Anemo storms, Geo quakes, Electro lightning, Dendro growth, Hydro rain, Pyro heat, Cryo blizzard |
| 5 | Archon Burst (unique; see below) | Map-changing ultimate |
| 6 | Divine Avatar | A big glowing transformation that leaves the unit nearly invulnerable for a short time |

**Unique Archon Bursts:** Barbatos — *Wind's Grand Ode* (a giant tornado) · Morax — *Planet Befall*
(a huge meteor) · Beelzebul — *Musou Isshin* (one slash across the screen) · Buer — *Illusory Heart*
(controls minds in an area) · Focalors — *Grand Trial* (a flood plus a judgement beam) · Mavuika —
*Sunfall Anthem* (a firestorm) · the Tsaritsa — *Everwinter* (freezes a region; turns on Stellar reactions for her kingdom).

#### Tier 4 — Dragon Sovereign (6 moves, one per element) and Nibelung

1 Dragon Breath (a line of the element) · 2 Primal Roar (AoE fear and stun) · 3 Sovereign's Scales (passive
resistance to their own element) · 4 Holy Soil (spreads Light Realm terrain that makes dragon-aligned units
stronger) · 5 Elemental Cataclysm (a terrain-changing ultimate: Hydro floods, Pyro volcanoes and so on)
· 6 Draconic Ascension (stats grow in the Age of Dragons).
**Nibelung** (the Dragon King, a boss trait) has all seven Breaths and *Moon-Making* (can grant the Trilunar Authority).

#### Tier 5 — The Four Shades (6 moves each)

| Shade | Moves |
|---|---|
| **Istaroth — Time** | 1 Moment's Halt (freezes an area in time; units stop moving) · 2 Rewind (undoes the last 10 seconds of damage on allies) · 3 Accelerate (allies act ×2 faster) · 4 Age (makes enemies older) · 5 Temporal Echo (repeats its last move) · 6 **Eternal Recurrence** (rewinds a whole battle area to an earlier state) |
| **Asmoday — Space** | 1 Fold (teleports itself or others) · 2 Spatial Tear (a damaging line) · 3 Distance Lock (enemies can't approach) · 4 Portal Network (moves allied armies) · 5 Compression (crushes an area) · 6 **World Partition** (seals an area inside an unbreakable barrier) |
| **Naberius — Life** | 1 Bloom of Life (big heal) · 2 Seal of Life (makes an ally immortal for a while) · 3 Genesis (creates life: grows plants or spawns animals) · 4 Life Theft (drains life) · 5 Mass Revival (brings back units that died nearby; time-limited) · 6 **EITR Tide** (changes nearby units' traits) |
| **Ronova — Death** | 1 Death Touch (instant kill on low-HP targets) · 2 Mark of Death (the target dies when the mark runs out unless it's healed) · 3 Reaper's Field (DoT area) · 4 Curse of Death (stops a region from healing) · 5 Underworld Gate (raises dead units as allies) · 6 **Final Verdict** (instantly kills every mortal in an area; long cooldown) |

These Shades can't permanently kill or revive anyone without the Heavenly Principles' approval. When the
Heavenly Principles are absent from the world, kills by Death count as unauthorised and can be undone if a
Life holder acts quickly, just like Ronova and Rhinedottir in 7.1.

#### Tier 6 — Trilunar Authority (7 moves)

1 Moonbeam (a beam of Lunar damage) · 2 Moonsign (gives allies the Lunar version of their reactions) ·
3 Waxing / Waning (switch between a buff mode and a debuff mode) · 4 Moonlit Veil (allies turn invisible and
can't be targeted) · 5 Tidal Pull (pulls whole armies around) · 6 Law of the Moons (overrides one world law
for a while: no death, no birth, no aging) · 7 **Three Moons Rise** (three moons appear, the light changes,
every Lunar reaction becomes stronger and enemies are cursed).

#### Tier 7 — Heavenly Principles / the Primordial One (7 moves)

1 Celestial Nail (a giant spike falls and changes the terrain) · 2 Erasure (removes a unit from history: no
record, no corpse) · 3 Divine Judgement (lightning from Celestia) · 4 Irminsul Rewrite (changes a unit's
traits) · 5 Summon Shade (calls a temporary Shade ally) · 6 Primordial Law (Shades and Archons in the world
get much stronger) · 7 **Light Realm Descent** (the screen goes white and all enemies in a large area take
huge damage).

#### Reactions engine

- Each element has an **aura status**, and reactions follow Genshin's rules.
- **Lunar reactions** happen within a Trilunar holder's area of influence or under Moonsign.
- **Stellar reactions** happen in a Cryo Archon's kingdom while Everwinter is active.

### 3.4 The Chronicle

**Start:** the god power *Begin the Teyvat Chronicle*. It can run on its own, or step by step with an
*Advance Chronicle* button.

**Pacing presets** (set in config): Lore ≈ 910 years, Normal ≈ 466, Fast ≈ 159, Custom = Normal ×
a time-scale slider (0.1–5). Each event has its own delay for each preset. The full table is in
[CHRONICLE.md](CHRONICLE.md).

**Starting point:** any era (from settings) or any single event (from the Timeline window). With
catch-up on, earlier events' set-up effects (who holds which Gnosis, which kingdom is Khaenri'ah, the
current era) are applied silently first.

**Announcements**, so you never miss an event:
1. **A big banner** across the middle of the screen (`WorldTip.showNowCenter`), with the stage title in the element's colour. Example: *"⚠ THE CATACLYSM — Khaenri'ah falls"*.
2. **A screen flash and screen shake** suited to the event (white for a Celestial Nail, red for the Cataclysm, blue for the moons).
3. **A sound stinger** for each stage (2–4 seconds), plus a deep warning sound for the countdowns.
4. **A world log entry** with a custom icon, so the event stays in the history.
5. **The camera jumps** to where the event happens (can be turned off).
6. **A lore pop-up** with a short story text and a *Continue* button (can be turned off).
7. **Optional auto-pause** when a stage begins.
8. **Countdown warnings** 10 years and 1 year before each big event, such as *"The Cataclysm draws near…"*.

### 3.5 Settings (`default_config.json`)

- **Chronicle:** pacing preset · custom years per stage · auto-advance on/off · camera jump · lore pop-ups · auto-pause · announcement volume
- **Levelling:** XP multiplier for training and for combat · how hard Trials are · how often Visions awaken
- **Balance:** tier multipliers · Delusion lifespan cost · Lunar/Stellar reactions on/off
- **Rules:** unique-holder limits on/off (one Archon per element, one per Shade, one Trilunar)

### 3.6 Art and audio (made for the mod)

No new unit sprites. All art is **icons, effect animations and sound**:

| Asset | Amount | Format |
|---|---|---|
| Trait icons | ~45 | 32×32 pixel art, in element colours |
| Move icons (for status and tooltips) | ~110 | 16×16 |
| Aura and status overlays | ~20 | Animated sprite sheets that loop |
| Move VFX: slashes, beams, novas, tornadoes, meteors, portals | ~70 | Sprite sheets, 6–12 frames, 32–128 px, registered as `EffectAsset`/`ProjectileAsset` |
| Announcement art (banner icons, screen-flash overlays) | 10 stages | Larger sprites |
| SFX for moves, reactions and auras | ~120 | `.wav` |
| Chronicle stingers and warnings | ~15 | `.wav`, 2–4 seconds each |

**How they're made:** all of it is generated by scripts kept in the repo (`tools/art/` and `tools/sfx/`).
Pixel-art icons and effect frames are drawn with Python image tools, and sound effects are synthesised from
noise, sine waves and filters. Each element has its own palette and sound character: Anemo is airy whooshes,
Geo is low stone cracks, Electro is crackle, Dendro is soft chimes, Hydro is splashes, Pyro is roaring bursts,
Cryo is glassy crystal. Higher tiers add layers on top: larger effect sprites, more particles, bigger screen
shake, deeper bass. Because everything comes from scripts, the art and sound are original, consistent, and easy to re-tune.

---

## Part 4 — Roadmap

| Phase | Deliverable |
|---|---|
| 0 ✅ | Project set up; the mod loads; the Teyvat tab, trait groups and 39 power traits (stats and icons) |
| 1 | Resonance levelling: training, Trials, talents, constellations, save data |
| 2 | Tier 1 (7 Visions + Delusion + Allogene): 5 moves each, the reaction engine, element VFX/SFX |
| 3 | Tiers 2–3: Exalted paths and Archons; god powers for granting them |
| 4 | Tiers 4–7: Dragons, Shades, Trilunar, Heavenly Principles; Lunar and Stellar reactions |
| 5 ✅ (core) | The Chronicle: 45 events, pacing presets, start anywhere, an editable timeline, the announcement system and 8 sound stingers. Power VFX per event come with Phases 2–4 |
| 6 | Balance pass, all settings, translations, Steam Workshop release |

Art and sound for each phase are made in that same phase.

---

## Sources

**Genshin**
- 7.0 launch (HoYoverse): https://www.hoyoverse.com/en-us/news/165689
- 7.1 launch (Gematsu): https://www.gematsu.com/2026/09/genshin-impact-version-version-7-1-update-a-rekviem-for-the-underworld-launches-september-23
- 7.1 story recap: https://uz.kursiv.media/en/2026-10-08/genshin-impact-snezhnaya-archon-quests-recapped-and-explaine/
- Four Shades reveal: https://gamerant.com/genshin-impact-four-shades-interlude-trailer/
- Columbina and the Trilunar Authority (fan wiki): https://genshin-impact.fandom.com/wiki/Columbina/Storyline
- Stellar reactions: https://www.icy-veins.com/genshin-impact/stellar-reactions
- 7.2 leaks (unconfirmed): https://allthings.how/genshin-impact-7-2-leaks-new-snezhnaya-characters-and-map-expansion/
- Khaenri'ah history (fan wiki): https://genshin-impact.fandom.com/wiki/Khaenri%27ah/History

**Modding**
- NeoModLoader: https://github.com/WorldBoxOpenMods/ModLoader
- NML wiki: https://github.com/WorldBoxOpenMods/ModLoader/wiki/ModTutorialHome
- ModTemplate: https://github.com/WorldBoxOpenMods/ModTemplate
- NML Workshop changelog: https://steamcommunity.com/sharedfiles/filedetails/changelog/3080294469
