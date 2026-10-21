# Teyvat Chronicle — a Genshin Impact chronology mod for WorldBox

Research notes and design plan. The goal is to make WorldBox play out Teyvat's history:
dragons, then the Primordial One, then the Archon War, the seven nations, the Khaenri'ah
Cataclysm and the present day. Units gain Visions and level up the way Genshin characters do.

> Status: research and design only. No mod code yet.
> Game API names below were checked against the `Assembly-CSharp-Publicized.dll` and `strings.dll`
> that ship with NeoModLoader `master` (NML 1.2.1.1, commit dated 2026-10-19).

---

## Part 1 — Genshin Impact research

### 1.1 Power system

| Concept | What it is in the lore | Who has it |
|---|---|---|
| **Vision** | A gem that lets a mortal use one element. Said to be granted by the gods when a person's wish catches their eye. | Most playable characters |
| **Gnosis** | A chess-piece-shaped device that connects an Archon to Celestia. The Fatui are collecting them for the Tsaritsa. | The Seven Archons |
| **Delusion** | The Fatui's artificial Vision. Strong, but it eats the user's life force. Non-Harbingers using it burn out fast. | Fatui agents and Harbingers |
| **Allogene** | Someone who uses an element without a Vision. | Very rare (e.g. Aloy, the Traveler) |
| **Abyss power** | Corruption from beyond Teyvat. It powers the Abyss Order and twists Khaenri'ahn survivors. | Abyss Order, Abyss Heralds/Lectors, the Traveler's sibling |
| **Adeptus** | Long-lived divine beasts of Liyue. Includes the Yakshas, who fight demonic karma. | Xiao, Cloud Retainer, Madame Ping… |

**The seven elements:** Anemo (wind), Geo (rock), Electro (lightning), Dendro (plants), Hydro (water),
Pyro (fire) and Cryo (ice). Each one maps to a nation, its Archon and its ideal:

| Element | Nation | Archon | Ideal |
|---|---|---|---|
| Anemo | Mondstadt | Barbatos (Venti) | Freedom |
| Geo | Liyue | Morax (Zhongli) | Contracts |
| Electro | Inazuma | Beelzebul (Ei / Raiden Shogun) | Eternity |
| Dendro | Sumeru | Buer (Nahida); Rukkhadevata before her | Wisdom |
| Hydro | Fontaine | Focalors (Furina); Egeria before her | Justice |
| Pyro | Natlan | Mavuika (the title passes between humans; Xbalanque was the first) | War |
| Cryo | Snezhnaya | The Tsaritsa | Love |

**Elemental reactions.** Combat revolves around applying one element and then hitting with another:

| Reaction | Elements | Effect |
|---|---|---|
| Vaporize | Hydro + Pyro | Damage ×1.5 or ×2 |
| Melt | Pyro + Cryo | Damage ×1.5 or ×2 |
| Overloaded | Pyro + Electro | Pyro explosion with knockback |
| Superconduct | Cryo + Electro | Area damage, enemy physical resistance down |
| Electro-Charged | Hydro + Electro | Repeated Electro ticks |
| Frozen | Hydro + Cryo | Target can't move |
| Swirl | Anemo + Pyro/Hydro/Electro/Cryo | Spreads the other element to nearby enemies |
| Crystallize | Geo + Pyro/Hydro/Electro/Cryo | Gives the attacker a shield |
| Burning | Dendro + Pyro | Damage over time |
| Bloom | Dendro + Hydro | Spawns seeds that explode (Hyperbloom and Burgeon are follow-ups) |
| Quicken | Dendro + Electro | Unlocks Aggravate and Spread for bonus damage |

Nod-Krai (Version 6.x) added Moon-themed "Lunar" reactions. These can come later.

**Divine hierarchy (strongest first):**
1. The Heavenly Principles / Primordial One (Phanes), plus the Four Shining Shades
2. The Seven Dragon Sovereigns, who ruled before the Primordial One came
3. The Seven Archons (each holds a Gnosis)
4. Other gods and Demon Gods (most died in the Archon War)
5. Adepti, the Fatui Harbingers, the Hexenzirkel witches, and strong Vision holders
6. Ordinary Vision and Delusion users
7. Ordinary people

### 1.2 Levelling and progression

| System | Range | What it does |
|---|---|---|
| Character level | 1 → 90 | Base stats grow. Levelling is gated by Ascension. |
| Ascension phases | 6 phases, at Lv 20/40/50/60/70/80 | Each phase raises the level cap and adds a stat bonus. Phases 1 and 4 unlock passive talents. |
| Talents | 1 → 10 (up to 13 with Constellations) | Normal Attack, Elemental Skill, Elemental Burst |
| Constellations | C0 → C6 | Unlocked with duplicate copies. Each one is a permanent upgrade. C3 and C5 add +3 talent levels. |
| Weapons | Lv 1 → 90, Refinement R1 → R5 | Weapon stats and passive effect |
| Artifacts | 1★ to 5★, up to +20 | Main and sub stats (Crit Rate, Crit DMG, ATK%, Elemental Mastery…) |
| Adventure Rank / World Level | AR 1 → 60, WL 0 → 8 | How strong the world's enemies are overall |
| Rarity | 4★ and 5★ characters | Base power tier |

### 1.3 Chronology (in-game time, oldest first)

Most of these dates are "about" figures. The in-game text is vague on purpose.

1. **Age of the Dragons.** The Seven Dragon Sovereigns rule Teyvat, each embodying one element.
2. **The Primordial One arrives.** Phanes and the Shades defeat the dragons and create the Heavenly
   Principles, Irminsul and the first humans.
3. **The Second Who Came / Ancient civilisations.** A second outsider descends and is fought off.
   Ancient nations rise and fall: **Remuria** (an ancient Fontaine empire), **Sal Vindagnyr**
   (Dragonspine, destroyed when Celestia dropped a "nail" on it), the **Sunchildren of Enkanomiya**,
   and the **King Deshret** civilisation in Sumeru's desert.
4. **The Archon War (ended roughly 2,000 years ago).** Demon Gods fight for the seven thrones of Celestia.
   The winners include Barbatos (who overthrew Decarabian), Morax (Liyue; Guizhong died in this war),
   the Electro twins Makoto and Ei, Rukkhadevata, Egeria and the first Pyro Archon. Liyue Harbor and Mondstadt are founded.
5. **Khaenri'ah's golden age.** A godless nation deep underground. It builds the Ruin Machines and
   runs the Crimson Moon Dynasty, then the Eclipse Dynasty.
6. **The Cataclysm (about 500 years ago).** Khaenri'ah is destroyed by the gods. Monsters made by
   the alchemist Rhinedottir ("Gold") spill across Teyvat. Makoto (Electro) and Rukkhadevata (Dendro)
   die, among many others. Non-pure-blooded Khaenri'ahns become Hilichurls; the pure-blooded are cursed with immortality.
   The Abyss Order rises.
7. **The Fontaine prophecy (≈500 years ago → present).** Focalors pretends to be the Archon (as Furina)
   for 500 years to outwit the prophecy that all Fontainians will dissolve.
8. **The present: the Traveler's journey.** Mondstadt → Liyue → Inazuma → Sumeru → Fontaine →
   Natlan → Nod-Krai (Version 6.x) → Snezhnaya (Version 7.0, announced in mid-2026). The Fatui go after every Gnosis.

**Lore caveat:** the current Snezhnaya story is still being released. The mod should treat
everything after Nod-Krai as "to be filled in" and never claim official facts it can't source.

---

## Part 2 — How to mod WorldBox effectively

### 2.1 Toolchain

- **Loader: [NeoModLoader (NML)](https://github.com/WorldBoxOpenMods/ModLoader).** It is the standard
  mod loader after NCMS. Install it from the Steam Workshop (it updates itself), or put `NeoModLoader.dll`
  in `worldbox_Data/StreamingAssets/mods/` and turn on experimental mode.
- **Start from the template: [ModTemplate](https://github.com/WorldBoxOpenMods/ModTemplate).** Clone it into the game's `Mods/` folder. It has:
  - `mod.json`: name, author, version, description, `iconPath` (NML also supports a `GUID` for dependencies)
  - `ModClass.cs`: `class ModClass : BasicMod<ModClass>` with an `OnModLoad()` override
  - `default_config.json`: settings that the player can change in-game
  - `Locales/en.json`: translated text
  - `GameResources/`: sprites. Every `.png` in this folder is loaded automatically, using the same
    path you'd pass to `Resources.Load`.
- **NML compiles your `.cs` source when the game starts**, so you don't need to build a DLL yourself
  to test. The `.csproj` is only there so your editor (VS Code / Rider / Visual Studio) can autocomplete.
  It points to `Assembly-CSharp-Publicized.dll`, a copy of the game code where every private member is public.
- **Hot reload:** implement `IReloadable` and mark methods `[Hotfixable]`. You can then edit a method
  body and press the mod's reload button without restarting the game. You can't change a method's signature this way.
- **Harmony** (bundled) lets you patch any game method, for example to run code after `Actor.levelUp()`.
- **Debugging:** NML has a log (`LogInfo`, `LogWarning`). Unity Explorer works with NML 1.2.0.1 and later.
- **Tutorials:** the [NML wiki](https://github.com/WorldBoxOpenMods/ModLoader/wiki/ModTutorialHome) has
  English basics. The advanced pages (traits, status effects, spells, eras, kingdoms, Harmony) are in
  Chinese; machine translation works fine on them.

### 2.2 Game systems we'll use (names checked against the current game DLL)

| Genshin idea | WorldBox class / library | Key fields and methods |
|---|---|---|
| Vision, Gnosis, Delusion, Constellations | `ActorTrait` → `AssetManager.traits` | `id`, `path_icon`, `group_id`, `type`, `rate_birth`, `rate_acquire_grow_up`, `base_stats[S.xxx]`, `action_attack_target`, `action_special_effect`, `action_get_hit`, `action_death`, `addSpell()`, `addOpposite()` |
| Elemental aura (what reactions read) | `StatusAsset` → `AssetManager.status` | `id`, `duration`, `base_stats`, `action`, `action_interval`, `opposite_status`, `remove_status`, `path_icon`, `texture` |
| Elemental skills and bursts | `SpellAsset` → `AssetManager.spells` | `cost_mana`, `chance`, `min_distance`, `cast_target`, `action` |
| Trait categories (an "Elements" tab) | `ActorTraitGroupAsset` → `AssetManager.trait_groups` | `id`, `name` |
| Eras (Age of Dragons, Archon War…) | `WorldAgeAsset` → `AssetManager.era_library` | `years_min/max`, `rate`, overlay/particle flags, `special_effect_action` |
| Nations and factions | `KingdomAsset` → `AssetManager.kingdoms`; plus `kingdoms_traits` | `addFriendlyTag`, `addEnemyTag`, `civ`, `mobs` |
| History messages | `WorldLogAsset` → `AssetManager.world_log_library` | `locale_id`, `path_icon`, `color` |
| New creatures (Hilichurls, Ruin Machines, dragons) | `ActorAsset` → `AssetManager.actor_library` | many fields; also needs sprites |
| God-power buttons (start the chronicle, give a Vision) | `GodPower` → `AssetManager.powers` + NML `PowerButtonCreator` | |

**Actor methods we'll call:** `addTrait`, `hasTrait`, `removeTrait`, `addStatusEffect`, `addExperience`,
`levelUp`, `getExpToLevelup`, `getMaxPossibleLevel`, `get_level`, `addRenown`, `isKing`, `setKingdom`, `getHit`.
`ActorData` stores `level`, `experience`, `mana`, `kills` and `renown`.

**Delegate signatures** (needed when writing actions):
- `AttackAction(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile)` — return `bool`
- `WorldAction(BaseSimObject pTarget, WorldTile pTile)`
- `GetHitAction(BaseSimObject pSelf, BaseSimObject pAttackedBy, WorldTile pTile)`

**Stat keys (`S.*`) we'll use:** `damage`, `health`, `armor`, `speed`, `attack_speed`, `critical_chance`,
`critical_damage_multiplier`, `area_of_effect`, `knockback`, `range`, `mana`, `stamina`, `lifespan`,
`skill_spell`, `skill_combat`, `intelligence`, `warfare`, `diplomacy`, `stewardship`, `multiplier_damage`,
`multiplier_health`, `multiplier_crit`. Existing statuses we can reuse: `burning`, `frozen`, `slowness`,
`shield`, `stunned`, `poisoned`, `powerup`, `enchanted`. Existing spells: `cast_fire`, `summon_lightning`,
`summon_tornado`, `cast_shield`, `cast_curse`, `cast_cure`, `teleport`.

**Locale keys:** a trait's name is `trait_{id}` and its description is `trait_{id}_info`. Other assets follow the same pattern.

### 2.3 Example: a Pyro Vision trait

This is a sketch. It hasn't been compiled against the game yet.

```csharp
var pyro = new ActorTrait
{
    id = "gi_vision_pyro",
    path_icon = "ui/icons/gi/vision_pyro",   // GameResources/ui/icons/gi/vision_pyro.png
    group_id = "gi_visions",
    type = TraitType.Positive,
    rate_birth = 0,                           // Visions are earned, not inherited
    rate_acquire_grow_up = 2,                 // 0–100 scale
    action_attack_target = (self, target, tile) =>
    {
        Elements.Apply(self, target, Element.Pyro);   // our reaction engine
        return true;
    },
};
pyro.base_stats[S.damage] = 8f;
pyro.base_stats[S.skill_spell] = 0.1f;
pyro.addSpell("cast_fire");                  // Elemental Skill
AssetManager.traits.add(pyro);
```

### 2.4 Good practice

- **Prefix every id with `gi_`** so nothing clashes with the base game or other mods.
- **Keep data separate from logic.** Put element, stat and reaction tables in one place so balancing doesn't mean hunting through code.
- **Expose balance numbers in `default_config.json`** (Vision chance, reaction multipliers, how fast eras advance).
- **Use the game's own hooks first** (`action_*` delegates). Reach for Harmony patches only when there's no hook.
- **Pin the game version.** WorldBox 0.50+ broke many older mods. Write down which game build and NML version the mod was tested on.
- **Avoid per-frame work.** Use `special_effect_interval` and `action_interval` instead of patching `Update`.
- **Ship small, playable steps.** Each phase below can be released and played on its own.

---

## Part 3 — Mod design

### 3.1 Power tiers (applied as traits)

| Tier | Trait | How you get it | Effect |
|---|---|---|---|
| 0 Mortal | — | default | — |
| 1 Vision holder | `gi_vision_<element>` (7) | Small chance on growing up. Higher for kings, people with many kills, and the "ambitious" or "chosen_one" traits. Or the god power gives one. | Element on hit, stat bonus, one element spell |
| 1b Delusion user | `gi_delusion` | Members of a Fatui kingdom | Big stat boost. Drains `lifespan` and `health` over time. |
| 2 Ascended | `gi_ascension_1..6` | Reaching level thresholds (see 3.2) | Stacking stat bonus |
| 3 Constellations | `gi_constellation_1..6` | Kills or renown milestones while holding a Vision | C1–C6 bonuses; C6 adds a stronger "burst" |
| 4 Adeptus / Harbinger / Witch | `gi_adeptus`, `gi_harbinger`, `gi_hexenzirkel` | Special events, or a C6 holder with very high renown | `long_liver`, big stats |
| 5 Archon | `gi_gnosis_<element>` (at most one per element in the world) | Wins the Archon War event, or is crowned by the god power | Near-immortal, nation-wide buffs, can grant Visions |
| 6 Dragon Sovereign / Primordial | `gi_dragon_sovereign_<element>`, `gi_heavenly_principles` | Spawned by chronicle events only | Boss tier |

### 3.2 Levelling (Genshin → WorldBox)

WorldBox units already have `level` and `experience`. A Harmony postfix on `Actor.levelUp()` handles the mapping:
- **Ascension:** at WorldBox levels 2/4/5/6/7/8, which mirror Genshin's 20/40/50/60/70/80, add `gi_ascension_N` and remove N−1.
- **Talents:** raise `skill_spell` and `skill_combat` gradually with level.
- **Constellations:** separate from level. Driven by `kills` and `renown`, so famous fighters reach C6.
- **"World Level":** a world-wide setting that scales Abyss and monster spawns as the chronicle moves forward.

The exact level thresholds depend on WorldBox's real level cap (`getMaxPossibleLevel()`). Check this in-game first.

### 3.3 Elemental reaction engine

- Each element has an **aura status** (`gi_aura_pyro`, `gi_aura_hydro`…) that lasts a few seconds.
- When an attack applies element **B** to a target already carrying aura **A**, look up `(A, B)` in a reaction table:
  - Vaporize / Melt → extra damage (`getHit` with a multiplier)
  - Overloaded → small explosion (reuse the game's explosion effect)
  - Frozen → the existing `frozen` status
  - Superconduct → a `gi_superconduct` status that lowers armour
  - Electro-Charged → damage over time on the target and anyone nearby who is wet
  - Swirl → copy aura A onto enemies nearby
  - Crystallize → the existing `shield` status on the attacker
  - Burning → the existing `burning` status
  - Bloom → spawn a delayed burst
- Both auras are used up by the reaction. Otherwise the new aura replaces the old one.

### 3.4 Nations and factions

Seven nation presets, each with an element bias: a city's people get that nation's Vision more often,
and its king can be crowned Archon. Plus three hostile factions: the **Abyss Order**, the **Fatui**
(Snezhnaya-aligned, Delusions), and **Hilichurls** (wild tribes). Nations can either be real
`KingdomAsset`s or **kingdom traits** on normal civs. Kingdom traits are simpler and work with any species.

### 3.5 The chronology mode

A god power, **"Begin the Teyvat Chronicle"**, starts a scripted timeline on the current world. A
new **era** marks each stage, and world-log messages narrate it.

| Stage | Era asset | What happens |
|---|---|---|
| 1 Age of Dragons | `gi_age_dragons` | Seven Dragon Sovereigns spawn, one per element (WorldBox's dragon, recoloured). |
| 2 The Primordial One | `gi_age_primordial` | A Heavenly Principles boss appears, fights the dragons and wins. Humans are guaranteed. |
| 3 Ancient Civilisations | `gi_age_ancients` | Remuria, Sal Vindagnyr and the King Deshret kingdoms get boosts. A "Celestial Nail" event wipes out one city. |
| 4 Archon War | `gi_age_archon_war` | Many units get "Demon God" traits and kingdoms go to war. When it ends, the strongest of each element gets a Gnosis (at most 7). |
| 5 Khaenri'ah's Rise | `gi_age_khaenriah` | A godless kingdom gets a tech/stat boost. Ruin Machines appear. |
| 6 The Cataclysm | `gi_age_cataclysm` | Khaenri'ah falls. Its people become Hilichurls or immortals. Abyss monsters spread. Two Archons may die. |
| 7 The Present Day | `gi_age_present` | Visions are common. The Fatui go after Gnoses. A "Traveler" unit can spawn. |

The timeline can run automatically (after a set number of years) or one step at a time (a button per stage).

### 3.6 Art

Each Vision, Gnosis and status needs a 16×16 or 32×32 icon. New creature sprites (Hilichurls,
Ruin Guards) are the largest art job, so they come last. Early phases reuse game sprites with recolours.

---

## Part 4 — Roadmap

| Phase | Deliverable | Can be played on its own? |
|---|---|---|
| 0 | Project set up from ModTemplate; loads and logs "Teyvat Chronicle loaded" | — |
| 1 | 7 Vision traits, an "Elements" trait group, aura statuses, the reaction engine | Yes |
| 2 | Ascension and Constellation traits; level-up hook | Yes |
| 3 | Gnosis and Archon tier; Delusions; "Grant Vision" and "Crown Archon" god powers | Yes |
| 4 | Nation kingdom traits and the Abyss, Fatui and Hilichurl factions | Yes |
| 5 | Eras and the chronicle timeline with world-log narration | Yes |
| 6 | Custom creatures and sprites; balancing; Steam Workshop upload | Release |

---

## Sources

- NeoModLoader: https://github.com/WorldBoxOpenMods/ModLoader (README, wiki and `assembly-dependencies/`)
- NML wiki, mod tutorial: https://github.com/WorldBoxOpenMods/ModLoader/wiki/ModTutorialHome
- ModTemplate: https://github.com/WorldBoxOpenMods/ModTemplate
- NML Steam Workshop changelog: https://steamcommunity.com/sharedfiles/filedetails/changelog/3080294469
- Community trait example: https://steamcommunity.com/app/1206560/discussions/0/4360121479831748605
- Khaenri'ah history (fan wiki): https://genshin-impact.fandom.com/wiki/Khaenri%27ah/History
- Genshin version news (GameWith): https://gamewith.net/genshin-impact/
