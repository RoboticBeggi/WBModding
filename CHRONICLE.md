# The Teyvat Chronicle: timeline and customisation reference

This file covers what the Chronicle plays out and how to change any of it.

## 1. The timeline

The Chronicle has 45 events in 11 eras, in the order the lore gives them. ★ marks a **major** event:
it has a bigger flash, countdown warnings beforehand, and can't be hidden by the "minor events" setting.
*Delay* is the number of in-game years after the previous event, for the **Lore / Normal / Fast** presets.

| Event | When (in Teyvat) | Delay (L / N / F) | id | Effects |
|---|---|---|---|---|
| **The Old World** | | | | |
| 1. ★ The Seven Sovereigns | Before recorded time | 0 / 0 / 0 | `ow_sovereigns` | age, spawn_unit |
| 2. The Primordial Sea | Before recorded time | 15 / 8 / 3 | `ow_primordial_sea` | — |
| 3. ★ The Three Moons Rise | Before recorded time | 20 / 10 / 4 | `ow_three_moons` | age, grant_trait |
| 4. The Voyager's Warning | Before recorded time | 15 / 8 / 3 | `ow_voyager` | — |
| **When the Doves Held Branches** | | | | |
| 5. ★ The Heavenly Principles Descend | ~6,400 years ago | 25 / 12 / 5 | `doves_descent` | age, grant_set, grant_trait |
| 6. The False Sky | ~6,400 years ago | 10 / 5 / 2 | `doves_false_sky` | earthquake |
| 7. ★ The Sovereigns Fall | ~6,360 years ago | 40 / 20 / 6 | `doves_victory` | remove_trait, war |
| **The Unified Civilization** | | | | |
| 8. ★ The Ark's Opening | ~6,000 years ago | 50 / 25 / 8 | `uc_ark` | age |
| 9. The Ancient Kingdoms | ~6,000 years ago | 25 / 12 / 4 | `uc_cities` | — |
| 10. The Pride of Humanity | ~6,000 years ago | 25 / 12 / 4 | `uc_pride` | war |
| **The War of Funerary Flame** | | | | |
| 11. ★ Nibelung Returns | ~6,000 years ago | 15 / 8 / 3 | `wff_return` | age, spawn_unit, war |
| 12. ★ The Moons Shatter | ~6,000 years ago | 15 / 8 / 3 | `wff_moons_fall` | age, remove_trait |
| 13. ★ The Celestial Nails | ~6,000 years ago | 10 / 5 / 2 | `wff_nails` | destroy_city, meteor, remove_trait |
| 14. The Forbidden History | ~6,000 years ago | 10 / 5 / 2 | `wff_seelie` | age |
| 15. ★ Khaenri'ah is Founded | ~6,000 years ago | 15 / 8 / 3 | `wff_khaenriah` | mark_kingdom |
| **The Archon War** | | | | |
| 16. ★ Seven Thrones Open | ~6,000 years ago | 30 / 15 / 5 | `aw_thrones` | age, grant_trait, war |
| 17. The Fall of the Guili Assembly | ~3,700 years ago | 45 / 22 / 7 | `aw_guizhong` | earthquake, grant_trait |
| 18. ★ The Third Descender | ~3,200–2,000 years ago | 40 / 20 / 6 | `aw_third_descender` | disaster, grant_trait |
| 19. The Fall of Decarabian | ~2,600 years ago | 40 / 20 / 6 | `aw_mondstadt` | war |
| 20. King Deshret's Fall | Thousands of years ago | 25 / 12 / 4 | `aw_deshret` | earthquake |
| 21. Xbalanque Slays Xiuhcoatl | ~2,000 years ago | 25 / 12 / 4 | `aw_natlan` | war |
| 22. ★ The Seven Are Crowned | ~2,000 years ago | 20 / 10 / 3 | `aw_seven` | age, grant_set, remove_trait |
| **The Age of the Seven** | | | | |
| 23. The Gods' Gaze | 2,000–500 years ago | 20 / 10 / 3 | `as_visions` | grant_set |
| 24. Dynasties of Khaenri'ah | Before the Cataclysm | 40 / 20 / 6 | `as_crimson_moon` | kingdom_traits |
| 25. Vennessa's Rebellion | ~1,000 years ago | 50 / 25 / 8 | `as_vennessa` | war |
| 26. Thrones Change Hands | 2,000–500 years ago | 50 / 25 / 8 | `as_archons_pass` | — |
| **The Cataclysm** | | | | |
| 27. ★ The Abyss Invades | ~500 years ago | 30 / 15 / 5 | `cat_invasion` | age, disaster, grant_trait, war |
| 28. ★ Khaenri'ah Falls | ~500 years ago | 5 / 3 / 1 | `cat_khaenriah_falls` | destroy_city, kingdom_traits, meteor |
| 29. ★ The Fallen Archons | ~500 years ago | 5 / 3 / 1 | `cat_archons_fall` | remove_trait |
| 30. ★ The Curse Takes Hold | ~500 years ago | 5 / 3 / 1 | `cat_celestia_sleeps` | age, remove_trait |
| **After the Cataclysm** | | | | |
| 31. The Age of Hilichurls | 500–100 years ago | 30 / 15 / 5 | `pc_hilichurls` | — |
| 32. ★ The Fatui Are Founded | After the Cataclysm | 30 / 15 / 5 | `pc_fatui` | grant_trait |
| 33. The Abyss Order | >100 years ago | 50 / 25 / 8 | `pc_abyss_order` | grant_trait |
| **The Present Day** | | | | |
| 34. ★ The Traveler Awakens | The present day | 60 / 30 / 10 | `pd_traveler` | age, grant_trait |
| 35. Song of the Dragon and Freedom | Version 1.0 | 2 / 2 / 1 | `pd_mondstadt` | gather_gnosis |
| 36. Farewell, Archaic Lord | Version 1.1 | 2 / 2 / 1 | `pd_liyue` | earthquake, gather_gnosis |
| 37. Omnipresence Over Mortals | Version 2.0 | 2 / 2 / 1 | `pd_inazuma` | gather_gnosis |
| 38. Akasha Pulses, the Kalpa Flame Rises | Version 3.0 | 2 / 2 / 1 | `pd_sumeru` | gather_gnosis |
| 39. ★ The Prophecy of Fontaine | Version 4.0 | 2 / 2 / 1 | `pd_fontaine` | remove_trait |
| 40. ★ Incandescent Ode of Resurrection | Version 5.0 | 2 / 2 / 1 | `pd_natlan` | gather_gnosis, remove_trait |
| **Song of the Welkin Moon** | | | | |
| 41. ★ The True Moon | Versions Luna I–VIII | 2 / 2 / 1 | `nk_columbina` | age, grant_trait |
| **The Snezhnaya Crisis** | | | | |
| 42. ★ Everwinter Without Mercy | Version 7.0 | 2 / 2 / 1 | `sz_everwinter` | age |
| 43. ★ The Curse of Death | Version 7.1 | 1 / 1 / 1 | `sz_curse_of_death` | war |
| 44. ★ A Rekviem for the Underworld | Version 7.1 | 1 / 1 / 1 | `sz_tsaritsa` | remove_trait |
| 45. ★ The Holy Soil Spreads | Present | 2 / 2 / 1 | `sz_holy_soil` | earthquake, meteor |

**Totals:** Lore ≈ 910 years · Normal ≈ 466 years · Fast ≈ 159 years. Custom = Normal × your time scale (0.1–5).

### How the timeline was researched

The order follows the community-maintained **Genshin Impact Wiki** master *Timeline* and its
region *History* pages (Mondstadt, Liyue, Inazuma, Sumeru, Fontaine, Natlan, Snezhnaya,
Khaenri'ah), plus coverage of the **Version 7.0 and 7.1** Archon Quests (Aug–Sep 2026).
The wiki labels its own timeline "a very rough estimation", so the "When" column gives approximate in-lore dates.

The wiki's era names are used as-is:
- Old World
- When the Doves Held Branches
- Unified Civilization
- War of Funerary Flame
- Archon War (~6,000–2,000 years ago)
- Age of the Seven (2,000–500)
- Cataclysm (500)
- After the Cataclysm
- The present day, with Nod-Krai's "Song of the Welkin Moon" (Versions Luna I–VIII) and Snezhnaya (7.x)

**Where the sources disagree, these are the choices the mod makes:**
- **When the Gnoses were made.** The wiki dates the seven thrones opening to ~6,000 years ago.
  Version 7.1 reveals that the Gnoses are the remains of the Third Descender, who opened the Shadow
  Realm gate with Anastasya 3,200–2,000 years ago. The mod has the thrones open at the start of the
  Archon War and the Gnoses forged near its end, just before the Seven are crowned.
- **The first Cryo Archon.** The Snezhnaya history page names the Belyi Tsar as the first Cryo Archon.
  Anastasya, the Tsaritsa, succeeded him around the Cataclysm. The mod's Gnosis traits stand for the
  throne, not the person, so either reading works.
- **Snezhnaya (7.x) is still being released.** The final event, *The Holy Soil Spreads*, is
  deliberately open-ended. Add events to `chronicle.custom.json` as new Archon Quests come out.

## 2. Ways to customise, from easiest to most powerful

1. **In-game settings** (Mods → Teyvat Chronicle): pacing, start era, catch-up, eras on/off,
   minor events on/off, every announcement channel, unique holders, destructive effects, wars.
2. **The Timeline window** (Teyvat tab): start from *any* event.
3. **The Advance / Pause / Stop buttons** let you control the Chronicle as it plays.
4. **Your own timeline file:** copy `data/chronicle.json` to `data/chronicle.custom.json`.
   When the custom file exists the mod loads it instead, and updates never overwrite it. Restart the
   game after editing.

## 3. Timeline file format

```jsonc
{
  "eras":   [ { "id": "old_world", "title": "The Old World" }, ... ],
  "events": [
    {
      "id": "doves_descent",          // unique id
      "era": "doves",                 // must match an era id
      "major": true,                  // bigger flash + countdown warnings
      "enabled": true,                // optional; false skips the event
      "title": "The Heavenly Principles Descend",
      "banner": "⚠ THE HEAVENLY PRINCIPLES DESCEND",   // big centre text
      "when": "~6,400 years ago",                     // shown under the banner
      "text": "Lore shown in the pop-up…",
      "color": "#FFF6D5",             // banner colour
      "flash": "white",               // white gold red blue violet cyan green black none
      "shake": 1.0,                   // 0 to 1
      "sound": "divine",              // divine doom war moon cosmic hope frost (or none)
      "delay": { "lore": 25, "normal": 12, "fast": 5 },   // years after the previous event
      "effects": [ { "type": "age", "id": "age_sun" }, ... ]
    }
  ]
}
```

Text can include `{khaenriah}`, which is replaced with the name of the kingdom marked as Khaenri'ah.

## 4. Effect reference

Effects run in order. An effect that can't run (no units, missing trait, unknown type) is skipped
with a warning in the NML log; it never stops the Chronicle.

| type | Fields | What it does |
|---|---|---|
| `age` | `id` | Switches the world era. Ids: `age_hope`, `age_sun`, `age_dark`, `age_tears`, `age_moon`, `age_chaos`, `age_wonders`, `age_ice`, `age_ash`, `age_despair` |
| `grant_trait` | `trait`, `count`, `pick`, `unique` | Gives a trait to `count` units chosen by `pick` |
| `grant_set` | `traits` (list), `pick` | Gives each trait to a **different** unit, e.g. one Gnosis per Archon |
| `remove_trait` | `trait` (`*` suffix = prefix match), `count` (-1 = all), `kill`, `replace_with`, `pass_on` | Takes a trait away. It can kill the holder, swap in another trait, or pass the trait to the strongest unit of the holder's kingdom |
| `war` | `count` | Starts wars between random civilised kingdoms |
| `meteor` | `count`, `target` | Meteors on `cities`, `random` tiles, or `kingdom:<tag>` |
| `earthquake` | `count` | Earthquakes at random places |
| `destroy_city` | `count`, `target` | Ruins cities: buildings fall and most citizens die. Target `random` or `kingdom:<tag>` |
| `mark_kingdom` | `tag`, `pick` (`random`, `largest`, `smallest`), `rename` | Remembers a kingdom under a tag. The `khaenriah` tag also renames it Khaenri'ah |
| `kingdom_traits` | `tag`, `trait`, `fraction` | Gives a trait to that fraction of a tagged kingdom's people |
| `disaster` | `id` | Runs a built-in disaster: `hellspawn`, `ice_ones_awoken`, `dragon_from_farlands`, `tornado`, `heatwave`, `small_meteorite`, `small_earthquake`… |
| `spawn_unit` | `actor` (default `dragon`), `count`, `traits` (list) | Spawns units on random land. With a trait list, unit N gets trait N. The Old World uses this to spawn seven real dragons as the Sovereigns, and Nibelung returns as a dragon |
| `gather_gnosis` | `from`, `to` | Moves a Gnosis trait from its holder to the holder of another trait (the Fatui collecting Gnoses for the Tsaritsa) |

**`pick` values** (best match first):
- `strongest` / `strongest_civ`: by health and damage
- `random` / `random_civ`
- `kings`
- `creatures`: non-sapient creatures, such as dragons and beasts

When a `*_civ` or `creatures` pick finds nobody, it falls back to any unit.

**Rules settings that affect effects:**
- **Unique holders:** a Gnosis, Sovereign, Shade, Nibelung, Trilunar or Heavenly Principles trait
  is never given while someone already holds it.
- **Destructive effects off:** skips `meteor`, `earthquake`, `destroy_city` and `disaster`.
- **Wars off:** skips `war`.
- **Destructive effects off** does not stop `spawn_unit`.
- **Catch-up:** only replays the effects that set up the world's state (`age`, the trait effects,
  `mark_kingdom`, `gather_gnosis`). It never replays destruction.

## 5. Trait ids

`gi_vision_<element>` · `gi_gnosis_<element>` · `gi_sovereign_<element>` (elements: anemo geo electro dendro hydro pyro cryo)
· `gi_delusion` · `gi_allogene` · `gi_adeptus` · `gi_harbinger` · `gi_hexenzirkel` · `gi_abyss_herald` · `gi_demon_god`
· `gi_nibelung` · `gi_shade_life` · `gi_shade_death` · `gi_shade_time` · `gi_shade_space` · `gi_moon_goddess` · `gi_trilunar`
· `gi_heavenly_principles` · `gi_dormant_principles` · `gi_abyssal` · `gi_curse_immortality`

Built-in WorldBox traits (for example `strong` or `immortal`) work in effects too.
