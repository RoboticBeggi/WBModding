# Teyvat Chronicle

A WorldBox mod that plays out Genshin Impact's history of Teyvat on your world, from the
Seven Dragon Sovereigns of the Old World to the Snezhnaya crisis of Version 7.1. Every power is a
trait, so you can give a Vision, a Gnosis or a Shade's Authority to anyone.

> **Status: v0.4.0.** Everything from before (the Chronicle, 39 powers, 202 animated moves, duels,
> reactions, Resonance levelling and training), now with a **custom Genshin-style interface**:
> - a Resonance panel beside every unit window
> - the Teyvat Archive window
> - an on-screen HUD
>
> It also adds **custom Dragon Sovereign designs**, **tier auras**, afterimages, craters, a camera
> spotlight, ascension ceremonies, and refined effects and sounds.
> v0.1.0 has been tested in-game; later versions compile against the game DLL but haven't been run in-game yet.
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
3. In the Chronicle tab, pick a pace and press **Begin**. Or click any event, then **Start here**.
4. Click glowing units to open their **Resonance panel**: rank, Ascension, Trial, talents,
   Constellation, what they're doing, and their moves.
5. Use the god powers (second row of the Teyvat tab) to raise, crown and shape your heroes.

**The interface**
- **Teyvat Archive:** a draggable window with four tabs:
  - **Chronicle:** status, pacing, controls, and every event with its story and a *Start here* button.
  - **Legends:** the mightiest powers.
  - **Powers:** the tier codex and reactions.
  - **Guide:** how to play.
- **Resonance panel:** opens beside WorldBox's unit window and updates live.
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

Every power trait has its own moveset of 5–7 moves: strikes, flurries, dashes, teleports,
projectiles, beams, fields, rains of meteors or lightning, shields, heals and ultimates. Each move
has animated, element-coloured effects and sounds. When two powered units fight on screen they may
break into a **duel**: blade exchanges, barrages, parries and counters, weaving dodges and combos,
ending in a finisher with an **impact frame**. Archons and above open their ultimates with a
**cutscene**. Every tier wears its own aura, as listed below.

| Tier | Aura |
|---|---|
| Vision | A shimmer |
| Exalted | Ki flames |
| Archon | A golden halo |
| Sovereign | Element motes |
| Shade | Crimson flames |
| Lunar | Orbiting moons |
| Heavenly Principles | Radiant rays |

Dashes leave afterimages, heavy blows leave craters, and ascensions play a ceremony. The Dragon
Sovereigns have their own element designs. The full list and the format for editing moves are in
[MOVES.md](MOVES.md).

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
