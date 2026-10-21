# Teyvat Chronicle

A WorldBox mod that plays out Genshin Impact's history of Teyvat on your world, from the
Seven Dragon Sovereigns of the Old World to the Snezhnaya crisis of Version 7.1. Every power is a
trait, so you can give a Vision, a Gnosis or a Shade's Authority to anyone.

> **Status: v0.3.0.** The Chronicle, 39 power traits, 202 animated moves, reactions, duels, impact
> frames, cutscenes and dragon Sovereigns, plus **Ley Line Resonance**: ranks 1–90 earned by training,
> Breakthrough Trials, talents, Constellations, Vision awakening, Archon succession, Exaltation and
> godhood, with 9 new god powers and a Legends leaderboard. v0.1.0 was tested in-game; v0.2.0 and
> v0.3.0 compile against the game DLL but haven't been run in-game yet.

## Install (PC / Steam)

1. Subscribe to **NeoModLoader** on the Steam Workshop and start the game once.
2. Put this folder at `<WorldBox>/Mods/TeyvatChronicle`. Either clone it there
   (`git clone https://github.com/RoboticBeggi/WBModding TeyvatChronicle`) or download the ZIP and
   extract it there. `<WorldBox>` is usually `Steam/steamapps/common/worldbox`.
3. Start WorldBox. NML compiles the mod by itself, so you don't need to build anything.
   A **Teyvat** tab appears in the god-powers bar.

## Playing

**The Teyvat tab**

| Button | What it does |
|---|---|
| Begin the Chronicle | Starts from the era picked in the mod settings (default: the Old World) |
| Chronicle Timeline | Lists all 45 events. Click any one to start the Chronicle **from that event** |
| Advance | Makes the next event happen now |
| Pause / Resume | Pauses the Chronicle (the world keeps running) |
| Status | Shows the next event and how many years are left |
| Stop | Ends the Chronicle on this world |

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

Every power trait has its own moveset of 5–7 moves: strikes, flurries, dashes, teleports,
projectiles, beams, fields, rains of meteors or lightning, shields, heals and ultimates. Each move
has animated, element-coloured effects and sounds. When two powered units fight on screen they may
break into a **duel**: blade exchanges, barrages, parries and counters, weaving dodges and combos,
ending in a finisher with an **impact frame**. Archons and above open their ultimates with a
**cutscene**. The full list and the format for editing moves are in [MOVES.md](MOVES.md).

## Levelling and training

Powered units grow through **Ley Line Resonance**: ranks 1–90, earned by drilling in their city,
hitting training dummies, sparring with each other, learning from mentors and fighting. At ranks
20/40/50/60/70/80 they must pass a **Trial** to break through. Talents sharpen their moves and
**Constellations** light up through fateful moments; C6 upgrades their Burst. Ordinary people who
train can **awaken a Vision**. Legends can rise further: they become Adepti, Harbingers or Witches,
inherit a dead Archon's Gnosis, or claim an empty throne. The Teyvat tab has god powers to inspect,
grant, raise and crown units, and to mark Training Grounds. Details are in [PROGRESSION.md](PROGRESSION.md).

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
- **Combat:** moves on/off, move damage, cooldown speed, reactions, duels (chance and how many at
  once), combat sounds, floating labels, auras, dragon bodies.
- **Resonance:** training and combat XP multipliers, sparring, Vision awakening (and its rate),
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
| `data/moves.json` | All movesets and moves (generated by `tools/data/gen_moves.py`) |
| `GameResources/gi/` | Icons and sounds, loaded automatically by NML |
| `Locales/en.json` | All in-game text |
| `tools/art`, `tools/sfx`, `tools/data` | Scripts that generate every icon, VFX frame, sound and the move data |
| `DESIGN.md`, `CHRONICLE.md`, `MOVES.md`, `PROGRESSION.md` | Research and design; references for the timeline, moves and levelling |

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
