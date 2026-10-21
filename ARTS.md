# Arts: how Teyvat Chronicle fights

Since v0.5 combat works like the *Combat Gods* mod rather than like Genshin Impact. Every power trait is a
**fighting style** with its own techniques, art and sounds, and fighters read the battlefield to choose
what to use. This page explains the system and lists every technique.

## The system

**Styles.** A unit fights with the style of its highest-tier power. Tier 1–2 styles have 6 techniques,
Archons and above 6–7 plus a **God ability**. Harbingers, Adepti and Allogenes fight in the element of
the holder's Vision when they have one.

**The tactical mind.** Each time a powered unit attacks, every ready technique it knows is scored
against what it sees:

| Shape | Chosen when |
|---|---|
| Crowd | 3+ enemies are bunched around the target |
| Line | 3+ enemies are strung out between the fighter and the target |
| Lone | the target is alone |
| Execute | the target is under 35% health |
| AntiAir | the target flies or is in the air |
| Gap | the target is more than 3.5 tiles away |
| Guard | the fighter is under 40% health |
| Support | an ally nearby is under 50% health |

The best score (with some randomness) wins. Every technique has its own range and cooldown.

**Levels I–III.** A technique's level is the higher of its rank level and its mastery:
- Rank level: I below rank 30, II from 30, III from 60. Archons start at II; Shades and above at III.
  A technique with an *unlock* of II or III stays locked until the rank level reaches it.
- Mastery: 12 uses for II, 35 for III. Training drills a random known technique, so practice counts.

Level II hits ×1.4 and Level III ×1.9 on top of Resonance (rank, talents, C6). Scripts also grow with
level: more projectiles, wider areas, extra beats. Level III colours burn white-hot, heavy blows freeze
the world for an instant, and ultimates open with a cutscene.

**Guard.** Ordinary blows (weapons and plain attacks) from a weaker fighter can be waved off by a
master, with a better chance the wider the tier gap, from rank 40, and while mid-technique. Exalted and
above send arrows and spells back at the shooter for half damage. Techniques always land.

**Duels.** Two powered fighters on screen may duel (chance and limit in the settings). A stare-down,
then 3–7 beats: blade lock, aerial clash, chase, feint, technique trade. Gods also hurl each other
14–24 tiles across the map and give chase. The mighty may finish in a **catastrophe** (their beams
push against each other until the land explodes), then the winner's strongest technique as a
**finisher**. Winning a duel counts for Resonance.

**Team arts.** Two powered allies near the same foe sometimes combine (25 s cooldown each):
Sky Relay, Twin Meteors, Crossed Beams, Merged Aura Wave.

**Situations.** Last stand (under 20% health, once per 90 s): a heal, a powerup, and the strongest
technique at ×1.6. Bodyguard: a stronger ally blinks between a wounded weaker ally and its attacker.
Vengeance: when a powered unit dies, powered allies within 12 tiles hunt its killer for 40 s. Rampage:
five kills in eight seconds fills a fifth of the God gauge.

**God abilities.** Tier 3+ fighters fill a God gauge (landing blows, taking blows, attacking). At 100,
and if no other God ability played in the last *GodCooldown* seconds (setting, default 40), the world
stops: letterbox bars, a title card, and the ability plays while everything else is frozen.

**Terrain.** Craters, fissures and debris linger; with *destructive effects* on, fire techniques set the
ground alight.

## Writing your own techniques

Styles are C# in `Code/Arts/Styles/`. NML compiles them in-game, so editing a file and restarting the
game is enough. A technique is a coroutine taking a `Cast`; `Code/Arts/Cast.cs` documents the toolkit
(effects, projectiles, beams, sounds, damage, dashes, blinks, arcs, launches, knockbacks, craters,
fissures). Art sets live in `GameResources/gi/arts/` (made by `tools/art/gen_arts.py`), sounds in
`GameResources/gi/sfx/arts/` (made by `tools/sfx/gen_arts_sfx.py`). After changing styles, run
`python tools/data/gen_arts_doc.py` to refresh the roster below.

## Every style

<!-- ROSTER -->

### Zephyr Arts

`gi_vision_anemo` · Tier 1 (Vision) · anemo

| Technique | Best against | Description |
|---|---|---|
| Feather Fan | Line | A fan of razor feathers down a line. At III they pin their targets. |
| Updraft Kick | AntiAir | A wind spout throws the foe up; a leaping kick slams them back down. |
| Gale Step | Gap | Three wind-ring hops close any distance and the last one cuts through. |
| Vacuum Pocket | Crowd | Sucks a crowd into a pocket of air, then bursts it outwards. |
| Tailwind Veil | Guard | Wraps the user in wind: dodging and fleet-footed. |
| Cyclone Ascent *(ultimate)* | Crowd | A roaming cyclone that juggles everything it touches. |

### Bedrock Arts

`gi_vision_geo` · Tier 1 (Vision) · geo

| Technique | Best against | Description |
|---|---|---|
| Stone Fist Barrage | Lone | Plants their feet and hammers the foe with stone fists, then shoves them away. |
| Pillar Procession | Line | Pillars of rock erupt one after another down a line, tossing everyone on it. |
| Meteor Drop | Gap | Calls rocks out of the sky onto a far foe; each impact cracks the ground. |
| Jade Lattice | Guard | A hexagonal lattice of jade wraps the user. At III it bursts into shards. |
| Fault Line Stomp | Crowd | Leaps and stomps: radial cracks tear outward and stun whoever stands on them. |
| Mountain Crown *(ultimate)* | Crowd | A ring of pillars cages the foes, then collapses inward on them. |

### Thunderclap Iai

`gi_vision_electro` · Tier 1 (Vision) · electro

| Technique | Best against | Description |
|---|---|---|
| Iai Draw | Lone | A held breath, a flash, and they are already behind the foe. The cut lands a beat later. |
| Chain Bolt | Crowd | Lightning leaps from foe to foe, 3 to 7 times. |
| Thunder Sigil | Crowd | Brands a sigil under the foes; bolts fall on it from a clear sky. |
| Starburst Flurry | Execute | Blinks around a wounded foe from every side, carving a star of cuts. |
| Static Step | Gap | Travels as a zigzag of lightning and arrives with a stunning jolt. |
| Eye of the Storm *(ultimate)* | Crowd | Opens a storm's eye over the battlefield that rains bolts on every foe. |

### Verdant Arts

`gi_vision_dendro` · Tier 1 (Vision) · dendro

| Technique | Best against | Description |
|---|---|---|
| Thorn Lasso | Lone | Lashes a thorned whip around the foe, drags them in and kicks them away. |
| Seed Volley | Line | Plants a row of seeds down a line; a moment later they all burst into bloom. |
| Root Snare | Crowd | Roots race outward through the ground and bind every foe they reach. |
| Healing Bloom | Support | Flowers open at hurt allies' feet and mend them. |
| Leaf Gale Rider | Gap | Rides a stream of razor leaves to a far foe, cutting all along the way. |
| World Tree Sprout *(ultimate)* | Crowd | A great tree bursts from the ground: falling leaves cut foes and its roots hold them. |

### Tidecaller Arts

`gi_vision_hydro` · Tier 1 (Vision) · hydro

| Technique | Best against | Description |
|---|---|---|
| Crosscurrent Lash | Lone | Two whips cross over the foe, then a third lifts and slams them. |
| Riptide Rush | Gap | Surfs a wave into the fight, bowling over everyone in its path. |
| Bubble Prison | Execute | Seals the foe in a bubble, floats it up, and bursts it. |
| Spring Font | Support | A healing geyser bursts under the most hurt ally and tosses foes away. |
| Droplet Barrage | Crowd | Flings water skyward that comes back down as a hail of piercing droplets. |
| Leviathan's Rise *(ultimate)* | Line | A serpent of water breaches the ground three times down a line. |

### Ember Fist

`gi_vision_pyro` · Tier 1 (Vision) · pyro

| Technique | Best against | Description |
|---|---|---|
| Rising Flame Combo | Lone | Jab, jab, a flaming uppercut, and a leaping hammer blow on the way down. |
| Wall of Pyres | Line | Draws a burning trail down a line that erupts into a wall of fire all at once. |
| Ember Scatter | Crowd | Leaps above the crowd and sprays a rain of embers that sets the ground alight. |
| Phoenix Dive | Gap | Becomes a diving phoenix and crashes into the foe. |
| Blazing Heart | Guard | Ignites their own spirit: empowered, and pulsing heat that scorches the adjacent. |
| Little Sun *(ultimate)* | Crowd | Forges a small sun over their head and hurls it down. |

### Frostbound Arts

`gi_vision_cryo` · Tier 1 (Vision) · cryo

| Technique | Best against | Description |
|---|---|---|
| Glacial Lance | Line | Forms a lance of ice and hurls it through a line. At III, three lances fly abreast. |
| Frozen Garden | Crowd | Frost spreads in rings around the target, freezing everyone it reaches. |
| Shatter Palm | Execute | Steps in and strikes with an open palm; frozen foes break apart. |
| Snowflake Shuriken | Lone | Throws spinning snowflakes that curve out and come back through the foe. |
| Glacier Wall | Guard | Raises a wall of ice spikes between the user and the foe, shoving them back. |
| Absolute Zero *(ultimate)* | Crowd | Everything nearby freezes solid, then shatters at once. |

### Crimson Overdrive

`gi_delusion` · Tier 1 (Vision) · delusion

| Technique | Best against | Description |
|---|---|---|
| Blood Rush | Gap | Pays blood to zigzag through everyone between the user and the target. |
| Redline Barrage | Line | A gun fanned at the nearest foes, shot after shot. At III the last round ricochets. |
| Overdrive Haymaker | Lone | A charged punch that freezes the moment and sends the target skidding. |
| Redline Stomp | Crowd | Leaps, hangs, and drops in a burst of burning life that throws a crowd into the air. |
| Adrenal Spike | Guard | Three heartbeats: the body forces itself back into the fight. |
| Burnout *(ultimate)* | Crowd | Burns a quarter of the user's life to blink through every foe nearby, then explodes. |

### Starless Wanderer

`gi_allogene` · Tier 1 (Vision) · anemo (or the holder's Vision)

| Technique | Best against | Description |
|---|---|---|
| Star Sword Volley | Line | Star swords gather overhead, then fire one by one down the line. |
| Rift Step | Gap | Steps into a rift and out behind the foe, kicking them back the way they came. |
| Mirror Hall | Guard | Starlit mirrors orbit the user, dodging blows and shooting shards back. |
| Cosmic Cleave | Lone | From above: two crossing cuts, then a tear in space that pins the foe. |
| Starfall Shower | Crowd | A slanting shower of falling stars over an area. |
| Falling Heaven *(ultimate)* | Crowd | A rift to the starless void drags everything in, then collapses outward. |

### Adeptal Arts

`gi_adeptus` · Tier 2 (Exalted) · anemo (or the holder's Vision)

| Technique | Best against | Description |
|---|---|---|
| Jade Javelin | Line | A thrown spear that pierces a line, plants itself and rings out. At III the adeptus follows it. |
| Cloud Stride | AntiAir | Rides a cloud over the foe and drives the spear down through them. |
| Talisman Seal | Lone | Talismans circle the foe and close in, sealing them in place. |
| Lotus Plunge | Crowd | Rises high into the sky and plunges straight down into a crowd. |
| Yaksha Mask | Guard | Dons the mask: stronger and faster, at the cost of karmic pain. |
| Bane of All Evil *(ultimate)* | Crowd | Masked, the yaksha plunges from foe to foe in a chain of falling strikes. |

### Fatui Protocol

`gi_harbinger` · Tier 2 (Exalted) · cryo (or the holder's Vision)

| Technique | Best against | Description |
|---|---|---|
| Execution Decree | Execute | A seal stamped under the foe. When it closes, the weak are executed outright. |
| Ordnance Barrage | Crowd | Fatui artillery arcs in from behind the lines and walks across the area. |
| Delusion Surge | Lone | A burst of borrowed power: a five-hit combo from both sides, ending in a launch. |
| Frost Chains | Line | Chains of ice link foe to foe, then freeze them all at once. |
| Iron Discipline | Support | An order barked to every ally around: shields up, press on. |
| Final Protocol *(ultimate)* | Crowd | A seal over the battlefield, then a storm of executions across it. |

### Moonlit Grimoire

`gi_hexenzirkel` · Tier 2 (Exalted) · lunar

| Technique | Best against | Description |
|---|---|---|
| Owl Familiars | Any | Owls loosed on curving flights that hunt down the nearest foes. |
| Rune Circle | Crowd | A spinning circle slows everyone inside, then moonlight erupts from it. |
| Portal Waltz | Gap | Two portals: the witch steps through one and the foe is pulled out of the other. They trade places. |
| Hex Bolt | Lone | A rune that circles its victim twice before striking, leaving a lingering curse. |
| Mirror Curse | Guard | A cursed mirror shields the witch and leaves nearby foes confused. |
| Witches' Sabbath *(ultimate)* | Crowd | A great ritual: owls circle and runes rain, then pillars of moonlight fall. |

### Abyssal Liturgy

`gi_abyss_herald` · Tier 2 (Exalted) · abyss

| Technique | Best against | Description |
|---|---|---|
| Grasp of the Abyss | Lone | A hand rises under the foe, seizes them and drags them to the herald. |
| Corrosive Trail | Line | Rushes down a line leaving pools of corrosion that eat at anyone standing in them. |
| Black Flame Ring | Crowd | Rings of abyssal flame burst outward from the herald in waves. |
| Bat Swarm | AntiAir | Bats spiral up around the foe; fliers are dragged out of the sky. |
| Rift Walk | Guard | Sinks into a rift, untouchable, and rises behind the nearest foe. |
| Abyssal Tide *(ultimate)* | Crowd | A rift opens; hands erupt everywhere and drag every foe into it before it bursts. |

### Archdemon Authority

`gi_demon_god` · Tier 2 (Exalted) · divine

| Technique | Best against | Description |
|---|---|---|
| Domain of Dominion | Crowd | A domain dome falls over an area: everyone inside slows and is struck three times. |
| Crushing Will | Lone | A raised hand, and the foe is pressed into the earth until it cracks. |
| Divine Spear | Line | A colossal spear of light thrown down a line; the ground splits along its path. |
| Calamity Fist | Gap | Leaps across the field and lands fist-first, sending an earthquake rolling on. |
| Fury Crown | Guard | A crown of fury: untouchable for a breath, healing and empowered. |
| Demon God's Wrath *(ultimate)* | Crowd | Rises above the field and unleashes three waves of fury, then a land-breaking slam. |

### Thousand Winds

`gi_gnosis_anemo` · Tier 3 (Archon) · anemo

| Technique | Best against | Description |
|---|---|---|
| Dandelion Gale | Line | A road of wind spouts that carries everyone on it far down the line. |
| Lyre Refrain | Support | Four beats of a lyre: each pulse heals allies and quickens their feet. |
| Skyward Lift | Crowd | Every foe nearby is lifted into the sky at once, held, then dropped. |
| Stormeye Arrow | Lone | Leaps back, draws, and looses one arrow that drags its target away. |
| Wind Wall | Guard | A cyclone curtain around the god repels everything that comes near. |
| Ballad of Freedom *(ultimate)* | Crowd | Five cyclones rise around the foe and converge into one. |
| **Thousand Winds of Time** *(God ability)* | world-stop | Every foe for miles is lifted by a spout, then one gale sweeps the world. |

### Rex Lapis

`gi_gnosis_geo` · Tier 3 (Archon) · geo

| Technique | Best against | Description |
|---|---|---|
| Rain of Jade | Crowd | Jade spears fall like rain over the crowd and stay planted. |
| Stele Resonance | Crowd | Three steles rise around the foe and hum, striking everything between them. |
| Jade Shield of Contract | Guard | An unbreakable lattice for the god and every ally near. |
| Dominus Lapidis | Lone | A fist of stone rises under the foe, clutches it high and crushes it into the earth. |
| Spear of Contract | Gap | A thrown spear pins the foe, and the god is drawn to it for the reckoning. |
| Planet Befall *(ultimate)* | Crowd | A planet falls from the sky and turns the foes it crushes to stone. |
| **Heavens' Weight** *(God ability)* | world-stop | Planets rain on every army in sight and all who remain are petrified. |

### Musou

`gi_gnosis_electro` · Tier 3 (Archon) · electro

| Technique | Best against | Description |
|---|---|---|
| Eye of Stormy Judgment | Crowd | An eye opens over the field and strikes whoever stands nearest beneath it. |
| Baleful Omen | Crowd | Marks foe after foe with omens; then every mark falls as lightning at once. |
| Shogun's Pursuit | Gap | Teleports from foe to foe, cutting each as she passes. |
| Euthymia | Guard | Perfect stillness: untouchable, then a circle of cuts at whoever came close. |
| Eternity's Pressure | AntiAir | A lance of lightning skewers a flier; a bolt from above nails it to the ground. |
| Musou no Hitotachi *(ultimate)* | Line | Silence, a drawn breath, and one cut that splits the land in a line. |
| **Plane of Euthymia** *(God ability)* | world-stop | Omens hang over every foe in sight. She sheathes. Everything is cut at once. |

### Irminsul

`gi_gnosis_dendro` · Tier 3 (Archon) · dendro

| Technique | Best against | Description |
|---|---|---|
| Seeds of Skandha | Crowd | Binds foes together with vines; every pulse wounds all of them as one. |
| Dream Lull | Lone | Sings a foe to sleep inside a ring of leaves, then wakes it with thorns. |
| Akasha Scan | Support | A wave of knowledge heals allies and lays seeds on every foe it reads. |
| Root Ascension | AntiAir | Roots climb into the sky, seize a flier and drag it into the earth. |
| Kusanali Step | Gap | Scatters into leaves and blooms back beside the foe in a burst of flowers. |
| Shrine of Maya *(ultimate)* | Crowd | An illusory garden: foes inside wither and slow, allies inside heal. |
| **Irminsul's Dream** *(God ability)* | world-stop | The world tree rises and drains every foe in sight through its roots. |

### Grand Tribunal

`gi_gnosis_hydro` · Tier 3 (Archon) · hydro

| Technique | Best against | Description |
|---|---|---|
| Salon Solitaire | Crowd | Three bubble companions orbit her, then dart out one after another into the crowd. |
| Verdict of the Oratrice | Execute | A seal of judgment over the guilty; the gong sounds; a column of water falls. |
| Tidal Ballet | Gap | Glides in a spiralling curve on the tide, splashing everyone in the way. |
| Pneuma and Ousia | Guard | Pneuma heals her; Ousia answers with a ring of bursting droplets. |
| Standing Ovation | Support | Fountains spring up beneath every ally to heal them. |
| Let the People Rejoice *(ultimate)* | Line | A tide wave rolls out down the line and sweeps everything with it. |
| **The Great Flood** *(God ability)* | world-stop | The prophecy is fulfilled: every foe is drowned in a bubble as the flood rolls through. |

### Sol Invictus

`gi_gnosis_pyro` · Tier 3 (Archon) · pyro

| Technique | Best against | Description |
|---|---|---|
| Flamestrider Charge | Line | Rides straight through the line, fire pillars bursting up in her wake. |
| War Banner Plunge | Crowd | Leaps high and plunges down; rings of fire roll out from the crater. |
| Nightsoul Blaze | Guard | Her body takes fire: stronger, and everyone near her burns. |
| Phoenix Escort | AntiAir | Two phoenixes spiral up and dive on a flier together. |
| Hearth's Grip | Lone | Seizes the foe, lifts it overhead, detonates it in her hand and hurls it away. |
| Crucible of the Sun *(ultimate)* | Crowd | A sun sinks over the battlefield, swells, and falls. |
| **Sunfell Slice** *(God ability)* | world-stop | A blazing sun rises and three world-wide arcs of fire cut through every foe. |

### Winter's Heart

`gi_gnosis_cryo` · Tier 3 (Archon) · cryo

| Technique | Best against | Description |
|---|---|---|
| Glacial Decree | Crowd | A seal is pressed into the ground and ice spikes ripple outward ring by ring. |
| Heart of Winter | Guard | Ice grows around her like armour and freezes whoever stands too close. |
| Frozen Throne | Lone | Encases a foe in ice, drags it to her feet and breaks it with one touch. |
| Avalanche | Line | A wall of snow and ice rolls down the line, burying everyone in it. |
| Winter's Grasp | AntiAir | Ice lances shoot up from the ground at a flier and bring it down frozen. |
| Eternal Winter *(ultimate)* | Crowd | A blizzard settles over the field, then everything in it shatters. |
| **The Frozen World** *(God ability)* | world-stop | Every foe in sight is sealed in ice; her heart beats three times; the world shatters. |

### Sky Tyrant

`gi_sovereign_anemo` · Tier 4 (Dragon Sovereign) · anemo

| Technique | Best against | Description |
|---|---|---|
| Tempest Breath | Line | A howling cone of wind that hurls everything in it far away. |
| Wingstorm | Crowd | Three great wingbeats; each throws a ring of wind outward. |
| Skyhook Talons | AntiAir | Talons reach out, fling the foe skyward and feathers shred it in the air. |
| Downburst | Crowd | A marked circle — then a column of air slams down and pins everyone. |
| Twin Gyres | Line | Two cyclones spawn on either flank and grind together across the line. |
| Gale Mantle | Guard | A mantle of wind: untouchable, and anything close is flung off. |
| Eye of the Tempest *(ultimate)* | Crowd | A storm forms around the dragon, drags the battlefield in and spits it out. |
| **Sky-Sundering Roar** *(God ability)* | world-stop | A roar crosses the map: every foe within 22 tiles is hurled into the sky and buried in feathers. |

### Earthshaker

`gi_sovereign_geo` · Tier 4 (Dragon Sovereign) · geo

| Technique | Best against | Description |
|---|---|---|
| Boulder Spit | Lone | Spits a molten boulder in a high arc; it bursts into shrapnel and stuns. |
| Tectonic Stomp | Crowd | Rings of rock pillars ripple outward from the dragon's feet. |
| Fault Rend | Line | A claw rakes open a crack; pillars burst from it, far end first, racing back. |
| Petrifying Roar | Execute | A roar that turns the foes before it to stone — then shatters them. |
| Crystal Hail | Crowd | Shards of crystallised earth hail down across a wide field. |
| Bedrock Hide | Guard | Plates of stone close over the dragon and a wall of pillars rises around it. |
| Mountain's Fall *(ultimate)* | Crowd | A whole mountain descends on the enemy, and the land splits around it. |
| **Primordial Quake** *(God ability)* | world-stop | The earth wakes: cracks web across 22 tiles and pillars erupt beneath every foe. |

### Storm Wyrm

`gi_sovereign_electro` · Tier 4 (Dragon Sovereign) · electro

| Technique | Best against | Description |
|---|---|---|
| Lightning Breath | Line | A crackling stream of lightning that flickers down a line for a full second. |
| Thunder Claw | Lone | A raking claw, then bolts from the sky on the same spot. |
| Storm Crown | Crowd | Sigils brand every enemy around; they detonate one after another. |
| Ion Wings | Gap | A wingbeat flings a slow ball of lightning that zaps all it passes. |
| Static Field | Crowd | The air around the dragon becomes a field of arcs that numbs and burns. |
| Conductor's Scales | Guard | Charged scales: a shield that lashes out at anyone nearby. |
| Heavenfall Thunder *(ultimate)* | Crowd | A storm eye opens above the enemy and the sky empties itself. |
| **Judgment of the Storm Wyrm** *(God ability)* | world-stop | A storm eye over every foe within 22 tiles; each is struck in turn. |

### Verdant Sovereign

`gi_sovereign_dendro` · Tier 4 (Dragon Sovereign) · dendro

| Technique | Best against | Description |
|---|---|---|
| Withering Breath | Line | A sickly breath that poisons and leaves rotting pools along its path. |
| Strangling Roots | Lone | Roots crawl across the ground, seize the foe and squeeze. |
| Thorn Forest | Crowd | Whips of thorn burst up in a spiral across the field. |
| Spore Bloom | Crowd | Seeds settle on every enemy; a heartbeat later each one bursts into a poison flower. |
| Devouring Maw | Execute | Vines drag a wounded foe across the field into the dragon's claws. |
| Oasis Heart | Support | A bloom of life opens around the dragon, healing it and its allies. |
| Apep's Dominion *(ultimate)* | Crowd | A tree erupts from the enemy's ground; its roots bind everything and drink it. |
| **Garden of Oblivion** *(God ability)* | world-stop | Everything within 20 tiles is overgrown, bound and withered in one breath. |

### Lord of Waters

`gi_sovereign_hydro` · Tier 4 (Dragon Sovereign) · hydro

| Technique | Best against | Description |
|---|---|---|
| Torrent Sweep | Line | A pressurised jet of water swung across the field like a blade. |
| Tidal Claw | Lone | Two lashes of water, then the foe is sealed in a rising bubble. |
| Rising Tide | Crowd | Waves roll out in every direction and carry the enemy away. |
| Geyser Field | Crowd | The ground spits geysers under one enemy after another. |
| Leviathan Lunge | Line | A serpent of water bursts from beside the dragon straight through the line. |
| Rain of Mercy | Support | A gentle rain over allies that closes their wounds. |
| Deluge *(ultimate)* | Crowd | A wall of water sweeps across the battlefield, then the sky keeps falling. |
| **Primordial Sea** *(God ability)* | world-stop | The old sea returns: waves from every side converge on all foes within 22 tiles. |

### Flame Sovereign

`gi_sovereign_pyro` · Tier 4 (Dragon Sovereign) · pyro

| Technique | Best against | Description |
|---|---|---|
| Inferno Breath | Line | A long, roaring breath that sets the ground alight behind it. |
| Magma Claw | Lone | Three furrows of magma raked across the foe; they erupt a moment later. |
| Ember Hail | Crowd | Burning embers rain over a wide field, each one bursting. |
| Phoenix Brood | Gap | Phoenixes hatch from the dragon's fire and hunt separate foes. |
| Eruption Ring | Crowd | A ring of fire pillars closes in on the enemy from all sides. |
| Heat Haze | Guard | The dragon's body burns so hot that anything close catches fire. |
| Second Sun *(ultimate)* | Crowd | A sun rises over the dragon, lances every foe around, then falls. |
| **Pyroclasm** *(God ability)* | world-stop | The land becomes a volcano: fire erupts beneath every foe within 22 tiles. |

### Frost Sovereign

`gi_sovereign_cryo` · Tier 4 (Dragon Sovereign) · cryo

| Technique | Best against | Description |
|---|---|---|
| Glacial Breath | Line | A freezing breath that locks foes in ice and leaves spikes behind. |
| Icicle Barrage | Lone | Icicles form in a halo around the foe and spear inward one by one. |
| Frost Talon | Execute | A talon strike that shatters anything frozen or failing. |
| Permafrost | Crowd | Ice spreads outward in rings, slowing all — then the whole field cracks. |
| Roaming Blizzard | Crowd | A blizzard drifts across the enemy lines, numbing and cutting. |
| Rime Armour | Guard | Plates of rime encase the dragon; attackers freeze on contact. |
| Endless Winter *(ultimate)* | Crowd | Absolute cold at one point: everything around freezes, then breaks. |
| **White Silence** *(God ability)* | world-stop | Snow buries everything within 22 tiles; when it stops, every foe shatters. |

### King of Dragons

`gi_nibelung` · Tier 6 (Lunar) · lunar

| Technique | Best against | Description |
|---|---|---|
| Abyssal Breath | Line | Black fire down a line, raising columns of abyssal flame as it goes. |
| Moonshard Volley | Crowd | Shards of moon orbit the king, then each flies at a different foe. |
| Crown of Dragons | Crowd | A roar of kingship: everything around kneels, stunned. |
| Lunar Claw | Lone | A claw followed by twin crescents that cross on the foe. |
| Abyss Rift | Line | A rift tears open, swallows the line toward it, and erupts. |
| Sovereign Will | Guard | Moonlight pours down on the king: briefly untouchable. |
| Moonfall *(ultimate)* | Crowd | A moon is pulled from the sky onto the enemy, ringed by pillars of light. |
| **Twilight of Dragons** *(God ability)* | world-stop | Moon pillars, abyssal fire and a final roar for every foe within 25 tiles. |

### Istaroth's Hourglass

`gi_shade_time` · Tier 5 (Shade) · time

| Technique | Best against | Description |
|---|---|---|
| Stopped Second | Crowd | A clock face freezes everyone inside; every blow they 'received' lands at once when it restarts. |
| Echo Strike | Gap | Echoes from the last few seconds converge on the foe from where she once stood. |
| Sand Burial | Lone | Columns of hourglass sand pour onto one foe, pinning and crushing them. |
| Clockwork Hands | Line | Two blades of light sweep around like clock hands, cutting everything they pass. |
| Age of Ruin | Execute | A year passes for the foe every tick; at III they crumble to dust. |
| Rewind | Guard | Rolls her own wounds back and returns her to where she stood moments ago. |
| Hourglass Collapse *(ultimate)* | Crowd | A colossal hourglass drains a whole battlefield of time, then snaps it back. |
| **Moment Eternal** *(God ability)* | world-stop | In a stopped world she walks to every enemy in sight and marks them; when time resumes, all the marks strike. |

### Folded Heavens

`gi_shade_space` · Tier 5 (Shade) · space

| Technique | Best against | Description |
|---|---|---|
| Portal Barrage | Lone | Punches into a portal at her side; the blows land out of portals all around the foe. |
| Pocket Cube | Execute | Folds space into a cube around the foe and crushes it smaller and smaller. |
| Transposition | Gap | Swaps places with the foe, leaving them stunned where she stood, and strikes from behind. |
| Spatial Rend | Line | Tears a seam of rifts down a line, then zips it shut on everything caught inside. |
| Gravity Fold | Crowd | Folds the ground inward so a crowd tumbles into one point, then lets it spring back. |
| Distance Denied | AntiAir | Opens a portal behind a far or flying foe and drops them at her feet, face first. |
| Collapse Lattice *(ultimate)* | Crowd | Fills the field with a grid of cubes that implode one after another. |
| **The Folded Sky** *(God ability)* | world-stop | Portals open above every enemy on the field; they all fall through into one point before her, and space closes on them. |

### Genesis Bloom

`gi_shade_life` · Tier 5 (Shade) · life

| Technique | Best against | Description |
|---|---|---|
| Vine Garden | Crowd | Blooms burst open across a crowd; their vines bind and poison whoever stands there. |
| Bloom of Renewal | Support | Flowers open at the feet of every hurt ally, mending them and shielding them. |
| Golden Alchemy | Lone | Transmutes the foe into a golden statue, then shatters it. |
| Thornspring | Line | A wave of roots runs down a line, flinging each foe skyward in turn. |
| Life Thread | Execute | A thorny thread ties the foe to her; their life flows along it into her. |
| Seed of Genesis | Guard | Closes into an unbreakable flower bud, heals, and bursts open throwing foes back. |
| Garden of Creation *(ultimate)* | Crowd | A tree of creation rises and the garden spreads ring by ring around it. |
| **Genesis** *(God ability)* | world-stop | The whole land blooms: every ally on the field is made whole and every enemy is swallowed by flowering vines. |

### Last Rites

`gi_shade_death` · Tier 5 (Shade) · death

| Technique | Best against | Description |
|---|---|---|
| Reaping Arc | Crowd | Spins the scythe around herself, one full turn per level. |
| Death Mark | Execute | A skull settles over the foe. When it closes its jaw, the weak simply die. |
| Soul Wail | Crowd | Skulls scream outward in rings; the living freeze in terror. |
| Bone Field | Lone | Spikes of bone erupt under one foe three times, each closer to the heart. |
| Funeral Bell | Line | Each toll of a great bell sends a heavier wave rolling down a line. |
| Reaper's Step | Gap | Fades into shadow and rises behind the foe, scything them up into the air. |
| Danse Macabre *(ultimate)* | Crowd | A ring of skulls dances around her, then each flies to a different victim. |
| **Final Rest** *(God ability)* | world-stop | A skull hangs over every enemy on the field; the bell tolls three times, and the third toll is a scythe across the world. |

### Lunar Hymn

`gi_moon_goddess` · Tier 6 (Lunar) · lunar

| Technique | Best against | Description |
|---|---|---|
| Returning Crescents | Line | Crescent blades fly out down a line and arc back to her hand, cutting twice. |
| Moonbeam Pillars | Crowd | Pillars of moonlight fall one after another across a crowd. |
| Tidal Pull | Crowd | The moon's pull lifts a crowd off its feet and drags it to her. |
| Silver Waltz | Lone | Dances a full circle around one foe, a crescent cut at every step. |
| Shard Ascension | AntiAir | Hurls moon shards up at a flier, then brings them down like hail. |
| Moonlit Veil | Support | A soft halo settles on every ally nearby, shielding them. |
| Eclipse *(ultimate)* | Crowd | Darkens the field under a black moon ringed in crescents, then lets it burst. |
| **Hymn of the Full Moon** *(God ability)* | world-stop | A full moon rises; a pillar of moonlight answers every enemy on the field, and the tide of light sweeps it clean. |

### Trilunar Authority

`gi_trilunar` · Tier 6 (Lunar) · lunar

| Technique | Best against | Description |
|---|---|---|
| Triple Orbit | Crowd | Three moons whirl around her, battering everything that comes close. |
| Phase Shift | Gap | Crosses the distance in three phases (new, half, full), striking once at each. |
| Waxing Lance | Line | A lance of moonlight that grows thicker with each of three pulses. |
| Three Tides | Crowd | Three tides of different moons roll in turn: out, in, then out again. |
| Crescent Trinity | Lone | Three crescents close on one foe from three directions at once. |
| Lunar Sanctuary | Guard | The three moons lock into a triangle of light around her: nothing gets through. |
| Moonfall *(ultimate)* | Crowd | Pulls a moon out of the sky onto the battlefield. |
| **Trilunar Convergence** *(God ability)* | world-stop | Three moons fall at once on three distant hosts, then pour their light back into her in one blinding line. |

### Heavenly Principles

`gi_heavenly_principles` · Tier 7 (Heavenly) · divine

| Technique | Best against | Description |
|---|---|---|
| Nail Procession | Line | Celestial nails fall one after another down a line, marching towards the foe. |
| Halo Judgment | Lone | A halo descends on one foe and tightens until it bursts into a cross of light. |
| Erasure | Execute | Outlines the foe in light, and removes them from the world. |
| Sky Law | Crowd | Decrees that nothing may move; everyone under the seal stands frozen as a nail finds each one. |
| Radiant Wings | AntiAir | Rays fan upward from her back and strike fliers out of the sky. |
| Heaven's Mandate | Guard | A halo crowns her: untouchable, restored, and every blow at her answered by light. |
| Shattered Firmament *(ultimate)* | Crowd | The sky cracks in crosses of light, and dozens of nails pour through the cracks. |
| **The False Sky Descends** *(God ability)* | world-stop | Every enemy on the field is marked by a cross, and a colossal nail falls for each; the sky whites out. |

### Dormant Principles

`gi_dormant_principles` · Tier 5 (Shade) · divine

| Technique | Best against | Description |
|---|---|---|
| Remnant Nail | Lone | A single, ancient nail takes an age to fall and leaves nothing standing where it lands. |
| Sleeping Halo | Crowd | A faded halo pulses slowly; each pulse makes the living heavier and slower. |
| Drifting Dreams | Line | Shards of a dream drift lazily down a line and burst one by one. |
| Curse Echo | Execute | Seals the foe with the Descender's curse; it rings again and again until it kills. |
| Weight of Celestia | Crowd | The floating isle's weight presses down on a crowd, grinding them into the ground. |
| Somnolent Ward | Guard | Wraps itself in slumbering light; wounds close while it sleeps. |
| Celestial Remnants *(ultimate)* | Crowd | Dim, broken nails rain down slowly over a wide field. |
| **Dream of Celestia** *(God ability)* | world-stop | The sky dims and the sleeping heavens dream of war: one immense nail after another, slow and grave, across the field. |

<!-- /ROSTER -->
