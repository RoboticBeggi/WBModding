using System.Collections;
using System.Linq;
using TeyvatChronicle.Chronicle;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Progression
{
    /// <summary>
    /// How units climb beyond their tier:
    /// - Archon succession: when an Archon dies, the Gnosis passes to an heir.
    /// - Exaltation: legendary Vision holders become Adepti, Harbingers, Witches or Abyss Heralds.
    /// - Godhood: a perfected Exalted can claim an empty throne and become an Archon.
    /// </summary>
    public static class Ascension
    {
        /// <summary>Called on every gnosis trait's death action.</summary>
        public static bool OnArchonDeath(BaseSimObject pTarget, WorldTile pTile)
        {
            if (!(pTarget is Actor dead)) return false;
            var gnoses = Elements.All.Where(e => dead.hasTrait("gi_gnosis_" + e)).ToList();
            Kingdom kingdom = dead.kingdom;
            string name = dead.getName();
            MainThread.Enqueue(() =>
            {
                foreach (string element in gnoses) Succeed(element, kingdom, name);
            });
            return true;
        }

        private static void Succeed(string pElement, Kingdom pKingdom, string pPrevious)
        {
            if (!Settings.Succession) return;
            string trait = "gi_gnosis_" + pElement;
            var units = World.world.units.getSimpleList().Where(u => u != null && u.isAlive()).ToList();
            if (units.Any(u => u.hasTrait(trait))) return; // someone already holds it (e.g. the Chronicle passed it on)

            Actor heir = units.Where(u => u.kingdom == pKingdom && u.hasTrait("gi_vision_" + pElement))
                             .OrderByDescending(Resonance.Rank).FirstOrDefault()
                         ?? units.Where(u => u.kingdom == pKingdom && u.isSapient() && Resonance.Tier(u) >= 1)
                             .OrderByDescending(Resonance.Rank).FirstOrDefault()
                         ?? units.Where(u => u.hasTrait("gi_vision_" + pElement))
                             .OrderByDescending(Resonance.Rank).FirstOrDefault();
            if (heir == null) return;
            Crown(heir, pElement, $"{heir.getName()} inherits the {Training.Capital(pElement)} Gnosis from {pPrevious}");
        }

        /// <summary>Makes a unit the Archon of an element, taking the Gnosis from any current holder.</summary>
        public static void Crown(Actor pActor, string pElement, string pMessage = null)
        {
            string trait = "gi_gnosis_" + pElement;
            foreach (Actor holder in World.world.units.getSimpleList().Where(u => u != null && u != pActor && u.hasTrait(trait)).ToList())
            {
                holder.removeTrait(trait);
            }
            pActor.addTrait(trait);
            Resonance.Moment(pActor, Resonance.Moments.Crowned);
            Color color = Elements.Color(pElement);
            string message = pMessage ?? $"{pActor.getName()} is crowned {Training.Capital(pElement)} Archon";
            Resonance.Celebrate(pActor, "A NEW ARCHON", "boom", 1.4f, message);
            Announcer.AnnounceLegend(message, color, pActor.current_tile, true);
        }

        // ---------------------------------------------------------------- yearly checks

        public static void YearlyChecks(Actor a, float pYears)
        {
            int tier = Resonance.Tier(a);
            if (tier == 1 && Settings.Exaltation && Resonance.Rank(a) >= 70 && Resonance.Phase(a) >= 5 &&
                Resonance.Constellation(a) >= 4 && Randy.randomChance(0.1f * pYears))
            {
                Exalt(a);
            }
            else if (tier == 2 && Settings.Godhood && Resonance.Rank(a) >= Resonance.MaxRank &&
                     Resonance.Constellation(a) >= 6 && Randy.randomChance(0.05f * pYears))
            {
                ClaimThrone(a);
            }
        }

        private static void Exalt(Actor a)
        {
            string path;
            if (a.hasTrait("gi_abyssal")) path = "gi_abyss_herald";
            else if (a.hasTrait("gi_delusion") || KingdomHas(a, "gi_gnosis_cryo") && CountHolders("gi_harbinger") < 11) path = "gi_harbinger";
            else if (a.stats["intelligence"] >= 15f) path = "gi_hexenzirkel";
            else path = "gi_adeptus";

            a.addTrait(path);
            string title = path switch
            {
                "gi_abyss_herald" => "an Abyss Herald",
                "gi_harbinger" => "a Fatui Harbinger",
                "gi_hexenzirkel" => "a Hexenzirkel Witch",
                _ => "an Adeptus"
            };
            Color color = Elements.Color(CombatCore.ResolveSet(a, out _)?.Element);
            Resonance.Celebrate(a, "EXALTED", "boom", 1.2f, title);
            Announcer.AnnounceLegend($"★ {a.getName()} has become {title}", color, a.current_tile, true);
        }

        private static void ClaimThrone(Actor a)
        {
            string own = Elements.All.FirstOrDefault(e => a.hasTrait("gi_vision_" + e));
            var vacant = Elements.All.Where(e => CountHolders("gi_gnosis_" + e) == 0).ToList();
            if (vacant.Count == 0) return;
            string element = own != null && vacant.Contains(own) ? own : vacant[Randy.randomInt(0, vacant.Count)];
            Crown(a, element, $"{a.getName()} ascends to the empty {Training.Capital(element)} throne");
        }

        private static bool KingdomHas(Actor a, string pTrait) =>
            a.kingdom != null && a.kingdom.getUnits().Any(u => u != null && u.isAlive() && u.hasTrait(pTrait));

        private static int CountHolders(string pTrait) =>
            World.world.units.getSimpleList().Count(u => u != null && u.isAlive() && u.hasTrait(pTrait));
    }
}
