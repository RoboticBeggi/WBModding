namespace TeyvatChronicle.UI
{
    /// <summary>The in-game how-to-play text shown in the Archive's Guide tab (GUIDE.md has the long version).</summary>
    public static class Guide
    {
        public const string Text =
            "<b><color=#F4DEA0>HOW TO PLAY TEYVAT CHRONICLE</color></b>\n\n" +

            "<b><color=#D9B56A>1. Make a world</color></b>\n" +
            "Generate any world and let a few kingdoms grow for a few years. More people means more Vision bearers, " +
            "bigger wars and better legends.\n\n" +

            "<b><color=#D9B56A>2. Begin the Chronicle</color></b>\n" +
            "Open the <b>Teyvat</b> tab → <b>Teyvat Archive</b> → <b>Chronicle</b>. Press <b>Begin</b> to start at the " +
            "Old World, or click any event and press <b>Start here</b>. Choose a pace: Lore (~900 years), Normal (~470), " +
            "Fast (~160) or Custom. Each era is announced with a cutscene, a banner, a sound and a world-history entry. " +
            "Countdown warnings appear 10 years and 1 year before major events.\n\n" +

            "<b><color=#D9B56A>3. Watch Teyvat's history happen</color></b>\n" +
            "Dragon Sovereigns rise, the Heavenly Principles descend with the Four Shades, the moons shatter, Celestial " +
            "Nails fall, Khaenri'ah is founded, the Archon War crowns the Seven, the Cataclysm strikes, the Fatui rise, " +
            "the Traveler awakens, Columbina takes the Trilunar Authority, and Snezhnaya's crisis unfolds. " +
            "Use <b>Advance</b> to skip ahead, <b>Pause</b> to hold, or <b>Stop</b> to end it.\n\n" +

            "<b><color=#D9B56A>4. Follow your heroes</color></b>\n" +
            "Click any unit: the <b>Resonance panel</b> opens beside its window. It shows its power and tier, rank and XP, " +
            "Ascension stars, its next Breakthrough Trial and progress, talents, Constellation, battle record, what it's doing " +
            "right now, and every move with its cooldown. Drag the panel by its title.\n" +
            "The <b>Legends</b> list (Archive tab and the HUD at the top right) ranks the mightiest powers. Click a name to jump there.\n\n" +

            "<b><color=#D9B56A>5. How heroes grow</color></b>\n" +
            "• <b>Awakening:</b> ordinary people build potential by training and fighting. At 100, a Vision may answer, most " +
            "often in a desperate battle.\n" +
            "• <b>Training:</b> Vision bearers drill in their city (400 XP/yr), train at dummies (800 XP/yr) and spar. A higher-rank " +
            "mentor in the city gives ×1.5; a Training Ground (god power) gives ×2. Above rank 60, training gives half XP.\n" +
            "• <b>Battle:</b> kills, moves and duels give XP. Duels give the most.\n" +
            "• <b>Breakthroughs</b> at ranks 20/40/50/60/70/80 need a Trial (sessions, wins, foes, elites, duels, renown). " +
            "Passing one adds an Ascension star.\n" +
            "• <b>Talents</b> (Strike/Skill/Burst) grow every 3 ranks. <b>Constellations</b> C1–C6 come from fateful moments " +
            "(surviving near death, slaying a king, winning a duel…). C6 awakens the Burst.\n" +
            "• <b>Beyond:</b> rank 70, ★5, C4 → Exalted (Adeptus, Harbinger, Witch or Abyss Herald). Rank 90, C6 Exalted can " +
            "claim an empty Archon throne. When an Archon dies, the Gnosis passes on.\n\n" +

            "<b><color=#D9B56A>6. Battle</color></b>\n" +
            "Powered units use 5–7 moves of their tier: strikes, flurries, dashes, teleports, projectiles, beams, fields, " +
            "meteor and lightning rains, shields, heals and ultimates. Elements react (Vaporize, Melt, Swirl…). " +
            "When two powers meet on screen they may <b>duel</b>: exchanges, barrages, parries, weaving dodges and combos, " +
            "then a finisher with an <b>impact frame</b>. Archons and above open their ultimates with a <b>cutscene</b>.\n\n" +

            "<b><color=#D9B56A>7. Play god</color></b>\n" +
            "Teyvat tab, second row (select, then click a unit):\n" +
            "Inspect · Grant Vision (+5 ranks on a bearer) · Shift Element · Grant Delusion · Ascend (+10 ranks) · " +
            "Bestow Constellation · Crown Archon · Training Ground (click a place; click again to remove).\n" +
            "Every power is also a trait: open a unit's trait editor and look for the <b>Teyvat</b> groups to give anyone a " +
            "Vision, a Gnosis, a Shade's Authority or the Trilunar Authority.\n\n" +

            "<b><color=#D9B56A>8. Make it yours</color></b>\n" +
            "<b>Settings</b> (Archive → Chronicle → Settings, or Mods → Teyvat Chronicle):\n" +
            "• <b>Chronicle:</b> pacing, start era, catch-up, which eras, minor events\n" +
            "• <b>Announcements:</b> banner, flash, shake, sound, camera, lore pop-ups\n" +
            "• <b>Combat:</b> move damage and speed, reactions, duels, sounds, labels, auras, dragon designs\n" +
            "• <b>Cinematics:</b> impact frames, cutscenes and their spacing\n" +
            "• <b>Resonance:</b> training and combat XP, sparring, awakening, ascension paths\n" +
            "• <b>Interface:</b> the Resonance panel, the HUD, UI size\n" +
            "To rewrite history, copy data/chronicle.json to chronicle.custom.json. To rewrite moves, copy data/moves.json to " +
            "moves.custom.json.\n\n" +

            "<b><color=#D9B56A>Tips</color></b>\n" +
            "• Want epic fights quickly? Start at <b>The Present Day</b> with catch-up on: the Archons already reign.\n" +
            "• Mark a Training Ground in a capital to raise a generation of champions.\n" +
            "• Too many cutscenes? Raise \"Seconds between move cutscenes\" or turn move cutscenes off and keep the " +
            "Chronicle ones.";
    }
}
