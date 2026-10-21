# Teyvat Chronicle

A WorldBox mod that plays out Genshin Impact's history of Teyvat on your world, from the
Seven Dragon Sovereigns of the Old World to the Snezhnaya crisis of Version 7.1. Every power is a
trait, so you can give a Vision, a Gnosis or a Shade's Authority to anyone.

> **Status: v0.1.0, early.** The Chronicle engine, 39 power traits with icons, and the announcement
> system (with sounds) are in. Movesets, Resonance levelling and power VFX come next; see
> [DESIGN.md](DESIGN.md). The code compiles against the game DLL that ships with NeoModLoader
> (October 2026), but it **has not been run in-game yet**.

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

**For full control, edit the timeline itself.** Copy `data/chronicle.json` to
`data/chronicle.custom.json` and change anything: the text, years, colours, sounds and effects,
or add your own events. The custom file is used whenever it exists and is never overwritten by updates.
Every field is documented in [CHRONICLE.md](CHRONICLE.md).

## Repository layout

| Path | Contents |
|---|---|
| `Code/` | The mod's C# code. NML compiles it in-game. |
| `data/chronicle.json` | The timeline: eras, events, effects |
| `GameResources/gi/` | Icons and sounds, loaded automatically by NML |
| `Locales/en.json` | All in-game text |
| `tools/art`, `tools/sfx` | Scripts that generate every icon and sound |
| `DESIGN.md`, `CHRONICLE.md` | Research, design and the timeline reference |

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
