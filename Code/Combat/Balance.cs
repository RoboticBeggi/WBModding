using System.Collections.Generic;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// How hard blows land between powered fighters. Raw WorldBox damage scales with the attacker's stats
    /// only, so two Archons (200 damage, 2500 health) would erase each other in a few techniques. Between
    /// powers, damage is instead a share of the victim's health, set by:
    /// - the blow's power (with diminishing returns, so stacked multipliers don't explode),
    /// - the tier gap (×1.6 per tier up, ÷1.6 per tier down),
    /// - combo decay: each hit on the same victim within 1.5 s lands a little softer (down to 40%),
    /// - a cap: no single blow takes more than 22% of an equal or stronger fighter's health
    ///   (35% for a God ability), so nobody of their own rank is one-shot.
    /// The raw damage still applies when it's lower, so a weak fighter can't steal a percentage of a god.
    /// Ordinary weapon blows between powers follow the same rules at a smaller share, and blows from mortals
    /// are cut by 45% per tier of the victim (an Archon takes about a sixth, the Heavenly Principles almost nothing).
    /// </summary>
    public static class Balance
    {
        private const float TechniqueShare = 0.018f;
        private const float BlowShare = 0.008f;
        private const float ComboWindow = 1.5f;
        private const float MortalBlow = 0.55f;

        private class Combo
        {
            public float Last;
            public int Hits;
        }

        private static readonly Dictionary<long, Combo> _combos = new Dictionary<long, Combo>();

        public static void ClearAll() => _combos.Clear();

        public static int TierOf(Actor pActor) => CombatCore.ResolveSet(pActor, out _)?.Tier ?? 0;

        public static float TierFactor(int pAttacker, int pVictim) =>
            Mathf.Clamp(Mathf.Pow(1.6f, pAttacker - pVictim), 0.12f, 8f);

        /// <summary>Damage for a technique hit. Power already includes the cast's multipliers.</summary>
        public static float Technique(Actor pCaster, Actor pVictim, float pPower, bool pGod)
        {
            float raw = Mathf.Max(1f, pCaster.stats["damage"]) * pPower * Settings.MoveDamage;
            int victim_tier = TierOf(pVictim);
            if (!Settings.BalancedCombat || victim_tier == 0)
            {
                // Ordinary folk: raw damage through armour, the WorldBox way.
                return raw * Mathf.Clamp01(1f - pVictim.stats["armor"] / 100f);
            }
            int caster_tier = TierOf(pCaster);
            float share = TechniqueShare * Mathf.Pow(Mathf.Max(0.05f, pPower), 0.7f) * TierFactor(caster_tier, victim_tier);
            share *= Settings.MoveDamage * ComboFactor(pVictim) / Mathf.Max(0.1f, Settings.FightLength);
            if (caster_tier <= victim_tier) share = Mathf.Min(share, pGod ? 0.35f : 0.22f);
            float damage = share * pVictim.getMaxHealth();
            return Mathf.Max(1f, Mathf.Min(damage, raw));
        }

        /// <summary>
        /// Rescales an ordinary blow (sword, claw, arrow) between two powered fighters. Returns the new
        /// damage, before the game's own armour reduction.
        /// </summary>
        public static float Blow(Actor pAttacker, Actor pVictim, float pDamage)
        {
            if (!Settings.BalancedCombat) return pDamage;
            int victim_tier = TierOf(pVictim);
            if (victim_tier == 0) return pDamage;
            int attacker_tier = TierOf(pAttacker);
            // Mortals barely scratch the powered: each tier turns away 45% more of an ordinary blow.
            if (attacker_tier == 0) return Mathf.Max(1f, pDamage * Mathf.Pow(MortalBlow, victim_tier));
            float share = BlowShare * TierFactor(attacker_tier, victim_tier) * ComboFactor(pVictim) /
                          Mathf.Max(0.1f, Settings.FightLength);
            if (attacker_tier <= victim_tier) share = Mathf.Min(share, 0.06f);
            // Armour is applied afterwards by the game; undo it here so armour doesn't count twice.
            float armour = Mathf.Clamp(1f - pVictim.stats["armor"] / 100f, 0.2f, 1f);
            return Mathf.Max(1f, Mathf.Min(pDamage, share * pVictim.getMaxHealth() / armour));
        }

        /// <summary>Each hit on the same victim within the combo window lands 8% softer, down to 40%.</summary>
        private static float ComboFactor(Actor pVictim)
        {
            long id = pVictim.getID();
            float now = CombatCore.Now;
            if (!_combos.TryGetValue(id, out Combo combo))
            {
                if (_combos.Count > 600) _combos.Clear();
                combo = new Combo();
                _combos[id] = combo;
            }
            combo.Hits = now - combo.Last > ComboWindow ? 0 : combo.Hits + 1;
            combo.Last = now;
            return Mathf.Max(0.4f, 1f - 0.08f * combo.Hits);
        }
    }
}
