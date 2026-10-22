# Teyvat Chronicle

A WorldBox mod that plays out Genshin Impact's history of Teyvat on your world, from the
Seven Dragon Sovereigns of the Old World to the Snezhnaya crisis of Version 7.1. Every power is a
trait, so you can give a Vision, a Gnosis or a Shade's Authority to anyone.

> **Status: v0.5.1.** Balanced fights between powers (no more one-shots between equals) and a simpler
> Chronicle tab built around a timeline slider. **v0.5.0 was the combat rework:** combat now works like the *Combat Gods* mod: 37 fighting
> styles with **238 techniques and 23 God abilities**, each with its own choreography, art and sound.
> It also brings a tactical mind, technique levels I–III, Guard, new duels, team arts, battle situations
> and world-stopping God abilities. All the effects (80 new animated sets) and combat sounds (83) are new.
> The interface glitches from v0.4 are fixed: the panel no longer gets cut off or shows the HUD
> through it, and the pink rectangle near dragons is gone.
> Only v0.1.0 has been tested in-game; v0.5.0 compiles against the game DLL but hasn't been run in-game yet.
>
> **New here? Read [GUIDE.md](GUIDE.md):** how to play, the flow of a playthrough, and every button.

## Install (PC / Steam)

1. Subscribe to **NeoModLoader** on the Steam Workshop and start the game once.
2. Put this folder at `<WorldBox>/Mods/TeyvatChronicle`. Either clone it there
   (`git clone https://github.com/RoboticBeggi/WBModding TeyvatChronicle`) or download the ZIP and
   extract it there. `<WorldBox>` is usually `Steam/steamapps/common/worldbox`.
3. Start WorldBox. NML compiles the mod by itself, so you don't need to build anything.
   A **Teyvat** tab appears in the god-powers bar.

## Playing

The full walkthrough is in **[GUIDE.md](GUIDE.md)**. You can also read it in the game: **Teyvat tab → How to Play**.

1. Make a world and let some kingdoms grow.
2. Open the **Teyvat** tab, then **Teyvat Archive**.
3. In the Chronicle tab, drag the timeline slider to any moment, set the pace slider and press **Begin here**.
4. Click glowing units to open their **Path panel**: Stage, facets, Marks, whether they're Primed or in
   a Crucible, what they're doing, their techniques and God gauge.
5. Use the god powers (second row of the Teyvat tab) to raise, crown and shape your heroes.

**The interface**
- **Teyvat Archive:** a draggable window with four tabs:
  - **Chronicle:** a timeline slider across all 45 events, the chosen event's story, one button to begin or
    jump there, and a pace slider.
  - **Legends:** the mightiest powers.
  - **Powers:** the tier codex and every fighting style with its techniques.
  - **Guide:** how to play.
- **Path panel:** opens beside WorldBox's unit window and updates live.
- **HUD** (top right): the next Chronicle event with its countdown, and the top 5 legends.

Each world remembers its own place in the timeline, because progress is saved with the world.

**Giving powers to anyone:** open a unit's trait editor and look for the *Teyvat* groups.

| Group | Traits |
|---|---|
| Elements | 7 Visions, Delusion, Allogene |
| Exalted | Adeptus, Fatui Harbinger, Hexenzirkel Witch, Abyss Herald, Demon God |
| Divine | 7 Gnoses (Archons), 7 Dragon Sovereigns, Nibelung |
| Primordial | the Four Shades, Moon Goddess, Trilunar Authority, Heavenly Principles (active and dormant) |
| Curses | Abyssal Corruption, Curse of Immortality |

## Combat

Every power is a **fighting style** with 6–7 techniques of its own, and nothing is shared between
styles. A few examples:

- **Zephyr Arts (Anemo Vision):** hops in on wind rings and juggles foes in a roaming cyclone.
- **Thunderclap Iai (Electro Vision):** a still draw, then one cut through the target.
- **Rex Lapis (Geo Archon):** a stone fist clutches a foe and crushes it into the earth.
- **Istaroth's Hourglass (Shade of Time):** stops the second and rewinds.
- **Heavenly Principles:** celestial nails fall on everyone in sight.

How fighting works:
- **Tactical mind:** fighters pick techniques from what they see: slams for crowds, fissures and beams
  for lines, executions for the wounded, leaps for fliers, dashes for far foes, guards when hurt.
- **Levels I–III:** techniques grow with Stage and practice, and training drills them too. Level III
  burns white-hot, cracks the land and freezes the world on impact.
- **Guard:** masters wave off weak blows and turn arrows back.
- **Duels:** stare-downs, blade locks, aerial clashes, chases, feints and technique trades. They end
  in a finisher, or between the mighty in a **catastrophe**. Gods send each other across the map.
- **Team arts:** Sky Relay, Twin Meteors, Crossed Beams and Merged Aura Wave.
- **Situations:** last stands, bodyguards, vengeance and rampages.
- **God abilities:** Archons and above fill a God gauge, then stop the world for their signature art.

Every tier also wears its own aura:

| Tier | Aura |
|---|---|
| Vision | A shimmer |
| Exalted | Ki flames |
| Archon | A golden halo |
| Sovereign | Element motes |
| Shade | Crimson flames |
| Lunar | Orbiting moons |
| Heavenly Principles | Radiant rays |

The whole system and every technique are listed in **[ARTS.md](ARTS.md)**.

## Levelling and training

Powered units grow along **the Crucible Path**, the mod's own levelling system:
- **Four facets grow from what a unit actually does:**
  - **Vigor:** taking hits and surviving, and drilling. It raises health.
  - **Edge:** landing techniques, dummy training and kills. It raises technique power.
  - **Ward:** guarding and protecting allies. It reduces damage taken.
  - **Flow:** dashes, duels and sparring. It shortens cooldowns.
- **Five Stages:** Kindled, Forged, Tempered, Hallowed and Paragon. Each one multiplies health (up to
  ×6) and damage (up to ×3), and unlocks technique Levels II and III.
- **The Crucible:** when a unit's facets hit the stage cap it becomes **Primed**. Its breakthrough has
  to be forged in battle: the next serious fight becomes a **Crucible**, and if it survives it breaks
  through. If it dies, it dies.
- **Marks:** deeds earn Marks with unique perks: Survivor, Kingslayer, Duelist, Giantslayer, Warden,
  Reaper, Unbowed and Crowned.

Ordinary people who train can **awaken a Vision**. Legends can rise further: Exaltation at Stage IV
with two Marks, and an empty Archon throne at Stage V with four. Details are in
[PROGRESSION.md](PROGRESSION.md).

**Power scale:** a fresh Vision bearer is worth two or three good soldiers. Exalted have thousands of
health, Archons 60,000, Dragon Sovereigns 250,000, Shades 400,000, the Moons around 800,000, the
Nibelung 1.2 million and the Heavenly Principles 2 million. Each tier above an ordinary soldier turns
away 45% more of the soldier's blows, so no army of mortals can grind down an Archon. Between powered
fighters, damage is a share of health, so fights stay long and fair.

## Customising

Open **Mods → Teyvat Chronicle → settings** in-game.

- **Pacing:** Lore (~900 years), Normal (~470), Fast (~160), or Custom (Normal × a time-scale slider).
- **Start from era:** any of the 11 eras. The Timeline window lets you pick any single event instead.
- **Catch-up:** when you start mid-timeline, earlier events are replayed silently, so the Archons
  already exist if you start in the present day.
- **Eras to include:** turn whole eras on or off. You can also turn minor events off.
- **Announcements:** turn each channel on or off: banner (and how long it stays up), screen flash,
  screen shake, sound, camera jump, world history, lore pop-up.
- **Rules:** one holder per unique power (Gnosis, Shade…); destructive effects; wars.
- **Combat:** techniques on/off, technique damage, cooldown speed, guard, team arts, situations,
  God abilities (and the time between them), duels (chance and how many at
  once), combat sounds, floating labels, auras, dragon models, Sovereign rivalry.
- **Path:** training and combat XP multipliers, sparring, Vision awakening (and its rate),
  Archon succession, Exaltation, godhood, announcements.
- **Cinematics:** impact frames on/off and their intensity, cutscenes for ultimates (with a cooldown),
  cutscenes for major Chronicle events.

**For full control, edit the timeline itself.** Copy `data/chronicle.json` to
`data/chronicle.custom.json` and change anything: the text, years, colours, sounds and effects,
or add your own events. The custom file is used whenever it exists and is never overwritten by updates.
Every field is documented in [CHRONICLE.md](CHRONICLE.md).

## Repository layout

| Path | Contents |
|---|---|
| `Code/` | The mod's C# code. NML compiles it in-game. |
| `data/chronicle.json` | The timeline: eras, events, effects |
| `Code/Arts/` | The combat engine; `Code/Arts/Styles/` holds every fighting style |
| `GameResources/gi/` | Icons, effect art (`arts/`) and sounds (`sfx/arts/`), loaded automatically by NML |
| `Locales/en.json` | All in-game text |
| `tools/art`, `tools/sfx`, `tools/data` | Scripts that generate every icon, effect frame and sound, and the ARTS.md roster |
| `DESIGN.md`, `CHRONICLE.md`, `ARTS.md`, `PROGRESSION.md` | Research and design; references for the timeline, combat and levelling |

## Building (optional)

NML compiles the source in-game, so building is only for checking the code compiles:

```
dotnet build                          # when cloned inside <WorldBox>/Mods/
dotnet build -p:RefDir=/path/to/dlls  # anywhere else; a folder with the game + NML DLLs
```

Never leave a `.dll` in the mod folder itself. NML treats any DLL there as a precompiled mod and
skips compiling the source.

## Credits

Genshin Impact and its characters belong to HoYoverse. This is a non-commercial fan mod.
The code, icons and sounds were made with AI assistance (declared in `mod.json`), and the icons
and sounds come entirely from the scripts in `tools/`. Lore is based on the community-maintained
Genshin Impact Wiki and coverage of Versions 7.0 and 7.1.
