using System.Collections.Generic;
using strings;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Powers
{
    /// <summary>
    /// Every Teyvat power is an ActorTrait that can be given to any unit from the trait editor.
    /// This file defines the trait groups, the traits and their base stats. Movesets are added
    /// on top of these traits in later phases.
    /// </summary>
    public static class TeyvatTraits
    {
        public const string GroupElements = "gi_elements";
        public const string GroupExalted = "gi_exalted";
        public const string GroupDivine = "gi_divine";
        public const string GroupPrimordial = "gi_primordial";
        public const string GroupCurses = "gi_curses";
        public const string GroupProgress = "gi_progress";

        public static readonly string[] Elements = { "anemo", "geo", "electro", "dendro", "hydro", "pyro", "cryo" };

        /// <summary>Traits that only one unit in the world may hold at a time (when the Unique holders rule is on).</summary>
        public static readonly HashSet<string> UniqueTraits = new HashSet<string>
        {
            "gi_heavenly_principles", "gi_dormant_principles", "gi_nibelung", "gi_trilunar",
            "gi_shade_life", "gi_shade_death", "gi_shade_time", "gi_shade_space"
        };

        static TeyvatTraits()
        {
            foreach (string element in Elements)
            {
                UniqueTraits.Add("gi_gnosis_" + element);
                UniqueTraits.Add("gi_sovereign_" + element);
            }
        }

        public static void Init()
        {
            AddGroup(GroupElements, "#7FE3FF");
            AddGroup(GroupExalted, "#FFB27F");
            AddGroup(GroupDivine, "#FFE58A");
            AddGroup(GroupPrimordial, "#FFF6D5");
            AddGroup(GroupCurses, "#B04BFF");
            AddGroup(GroupProgress, "#FFE58A");

            // Tier 1: Vision holders, Delusion users and Allogenes
            foreach (string element in Elements)
            {
                Add("gi_vision_" + element, GroupElements, TraitType.Positive, new Dictionary<string, float>
                {
                    [S.health] = 60, [S.damage] = 10, [S.armor] = 3, [S.speed] = 2,
                    [S.critical_chance] = 0.05f, [S.mana] = 20, [S.lifespan] = 10, [S.skill_spell] = 0.1f
                });
            }
            Add("gi_delusion", GroupElements, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 80, [S.damage] = 25, [S.attack_speed] = 10, [S.speed] = 3, [S.lifespan] = -15
            });
            Add("gi_allogene", GroupElements, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 120, [S.damage] = 18, [S.armor] = 5, [S.speed] = 4,
                [S.critical_chance] = 0.08f, [S.mana] = 40, [S.lifespan] = 500, [S.skill_spell] = 0.2f
            });

            // Tier 2: Exalted
            Add("gi_adeptus", GroupExalted, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 350, [S.damage] = 45, [S.armor] = 12, [S.speed] = 6, [S.lifespan] = 2000, [S.mana] = 80
            });
            Add("gi_harbinger", GroupExalted, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 300, [S.damage] = 50, [S.armor] = 10, [S.speed] = 4, [S.warfare] = 5, [S.lifespan] = 80
            });
            Add("gi_hexenzirkel", GroupExalted, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 250, [S.damage] = 40, [S.armor] = 8, [S.mana] = 150, [S.lifespan] = 1500,
                [S.skill_spell] = 0.5f, [S.intelligence] = 10
            });
            Add("gi_abyss_herald", GroupExalted, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 320, [S.damage] = 48, [S.armor] = 10, [S.speed] = 5, [S.lifespan] = 300
            });
            Add("gi_demon_god", GroupExalted, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 800, [S.damage] = 90, [S.armor] = 18, [S.lifespan] = 3000, [S.area_of_effect] = 1
            });

            // Tier 3: Archons and Tier 4: Dragon Sovereigns
            foreach (string element in Elements)
            {
                Add("gi_gnosis_" + element, GroupDivine, TraitType.Positive, new Dictionary<string, float>
                {
                    [S.health] = 2500, [S.damage] = 200, [S.armor] = 30, [S.speed] = 8, [S.mana] = 300,
                    [S.lifespan] = 1500, [S.area_of_effect] = 1, [S.diplomacy] = 10, [S.stewardship] = 10
                });
                Add("gi_sovereign_" + element, GroupDivine, TraitType.Positive, new Dictionary<string, float>
                {
                    [S.health] = 5000, [S.damage] = 350, [S.armor] = 40, [S.speed] = 6, [S.mana] = 400,
                    [S.lifespan] = 3000, [S.area_of_effect] = 2
                });
            }
            Add("gi_nibelung", GroupDivine, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 18000, [S.damage] = 900, [S.armor] = 55, [S.speed] = 8, [S.mana] = 900,
                [S.lifespan] = 9000, [S.area_of_effect] = 3
            });

            // Tiers 5–7: the Four Shades, the Moons and the Heavenly Principles
            foreach (string shade in new[] { "life", "death", "time", "space" })
            {
                Add("gi_shade_" + shade, GroupPrimordial, TraitType.Positive, new Dictionary<string, float>
                {
                    [S.health] = 8000, [S.damage] = 500, [S.armor] = 50, [S.speed] = 10, [S.mana] = 600,
                    [S.lifespan] = 6000, [S.area_of_effect] = 2
                });
            }
            Add("gi_moon_goddess", GroupPrimordial, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 7000, [S.damage] = 420, [S.armor] = 45, [S.speed] = 10, [S.mana] = 800,
                [S.lifespan] = 6000, [S.area_of_effect] = 2
            });
            Add("gi_trilunar", GroupPrimordial, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 10000, [S.damage] = 600, [S.armor] = 55, [S.speed] = 12, [S.mana] = 1200,
                [S.lifespan] = 8000, [S.area_of_effect] = 3
            });
            Add("gi_heavenly_principles", GroupPrimordial, TraitType.Positive, new Dictionary<string, float>
            {
                [S.health] = 20000, [S.damage] = 1000, [S.armor] = 60, [S.speed] = 12, [S.mana] = 2000,
                [S.lifespan] = 9999, [S.area_of_effect] = 4
            });
            Add("gi_dormant_principles", GroupPrimordial, TraitType.Other, new Dictionary<string, float>
            {
                [S.health] = 6000, [S.damage] = 300, [S.armor] = 40, [S.speed] = -10, [S.lifespan] = 9999
            });

            // Curses
            Add("gi_abyssal", GroupCurses, TraitType.Negative, new Dictionary<string, float>
            {
                [S.health] = 40, [S.damage] = 15, [S.speed] = 2, [S.diplomacy] = -5, [S.lifespan] = -10
            });
            Add("gi_curse_immortality", GroupCurses, TraitType.Other, new Dictionary<string, float>
            {
                [S.lifespan] = 5000, [S.health] = 50, [S.diplomacy] = -3
            });

            // Progress markers: Ascension phases and Constellations (set by the Resonance system)
            for (int i = 1; i <= 6; i++)
            {
                Add("gi_ascension_" + i, GroupProgress, TraitType.Positive, new Dictionary<string, float>
                {
                    [S.health] = 30 * i, [S.damage] = 4 * i, [S.armor] = i, [S.speed] = i, [S.mana] = 10 * i
                });
                Add("gi_constellation_" + i, GroupProgress, TraitType.Positive, new Dictionary<string, float>
                {
                    [S.critical_chance] = 0.02f * i, [S.damage] = 3 * i, [S.health] = 15 * i
                });
            }

            // When an Archon dies, the Gnosis passes to an heir.
            foreach (string element in Elements)
            {
                AssetManager.traits.get("gi_gnosis_" + element).action_death = Progression.Ascension.OnArchonDeath;
            }
        }

        /// <summary>
        /// Divine and primordial beings glow with their element, and Dragon Sovereigns that are dragons
        /// wear an element-coloured body.
        /// </summary>
        private static bool DivinePresence(BaseSimObject pTarget, WorldTile pTile)
        {
            if (pTarget is Actor actor) MainThread.Enqueue(() => Presence(actor));
            return true;
        }

        private static bool Presence(Actor actor)
        {
            if (actor == null || !actor.isAlive()) return false;
            MoveSet set = CombatCore.ResolveSet(actor, out string trait_id);
            if (set == null) return false;
            if (trait_id.StartsWith("gi_sovereign_")) DragonBodies.Apply(actor, set.Element);
            else if (trait_id == "gi_nibelung") DragonBodies.Apply(actor, "lunar");

            // Dragons (and other units that skip normal fighting) never trigger the attack hook,
            // so divine beings also look for a nearby enemy here and use a move on it.
            Actor foe = actor.has_attack_target ? actor.attack_target as Actor : null;
            if (foe == null && actor.asset.skip_fight_logic)
            {
                float best = 100f;
                foreach (Actor unit in World.world.units.getSimpleList())
                {
                    if (!CombatCore.IsEnemy(actor, unit)) continue;
                    float d = ((Vector2)unit.current_position - actor.current_position).sqrMagnitude;
                    if (d < best) { best = d; foe = unit; }
                }
            }
            if (foe != null) CombatAI.Decide(actor, foe);
            return true; // auras are drawn by AuraSystem for every tier
        }

        private static void AddGroup(string pId, string pColor)
        {
            if (AssetManager.trait_groups.has(pId)) return;
            AssetManager.trait_groups.add(new ActorTraitGroupAsset
            {
                id = pId,
                name = "trait_group_" + pId,
                color = pColor
            });
        }

        private static void Add(string pId, string pGroup, TraitType pType, Dictionary<string, float> pStats)
        {
            if (AssetManager.traits.has(pId)) return;
            var trait = new ActorTrait
            {
                id = pId,
                path_icon = "gi/icons/traits/" + pId,
                group_id = pGroup,
                type = pType,
                needs_to_be_explored = false,
                can_be_given = true,
                can_be_removed = true,
                rate_birth = 0,
                rate_inherit = 0
            };
            // Every power can use moves; curses can't.
            if (pGroup != GroupCurses && pGroup != GroupProgress) trait.action_attack_target = CombatAI.OnAttack;
            if (pGroup == GroupDivine || pGroup == GroupPrimordial)
            {
                trait.special_effect_interval = 3f;
                trait.action_special_effect = DivinePresence;
            }

            // The trait library creates base_stats inside add(), so stats are set afterwards.
            AssetManager.traits.add(trait);
            foreach (KeyValuePair<string, float> stat in pStats)
            {
                trait.base_stats[stat.Key] = stat.Value;
            }
        }
    }
}
