using System.Collections.Generic;
using HarmonyLib;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Progression;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// Masters wave off ordinary blows from weaker fighters and turn arrows and spells back at whoever
    /// sent them. Only plain attacks are guarded: techniques, fire, plague and the like always land.
    /// </summary>
    public static class Guard
    {
        /// <summary>Set while a technique deals its own damage, so the Guard lets it through.</summary>
        public static bool Bypass;

        private static readonly Dictionary<long, float> _last_fx = new Dictionary<long, float>();

        public static void Patch(Harmony pHarmony)
        {
            pHarmony.Patch(AccessTools.Method(typeof(Actor), nameof(Actor.getHit)),
                prefix: new HarmonyMethod(typeof(Guard), nameof(Prefix)));
        }

        private static bool Prefix(Actor __instance, ref float pDamage, AttackType pAttackType, BaseSimObject pAttacker)
        {
            if (Bypass) return true;
            if (pAttackType != AttackType.Weapon && pAttackType != AttackType.Other) return true;
            if (!(pAttacker is Actor attacker) || attacker == __instance || !__instance.isAlive()) return true;
            // Ordinary blows between powers are rescaled so fights between the mighty last.
            pDamage = Balance.Blow(attacker, __instance, pDamage);
            if (!Settings.Guard) return true;

            MoveSet mine = CombatCore.ResolveSet(__instance, out _);
            if (mine == null) return true;
            int my_tier = mine.Tier;
            int their_tier = CombatCore.ResolveSet(attacker, out _)?.Tier ?? 0;
            int stage = Path.Stage(__instance);
            if (my_tier <= their_tier && stage < 3) return true;

            CombatData data = CombatCore.Data(__instance);
            float chance = 0.18f * (my_tier - their_tier) + (stage >= 3 ? 0.12f : 0f) + (data.Casting ? 0.25f : 0f) +
                           Path.GuardBonus(__instance);
            if (my_tier < 2 && their_tier >= my_tier) chance *= 0.5f;
            if (!Randy.randomChance(Mathf.Clamp(chance, 0f, 0.85f))) return true;

            data.Gauge = Mathf.Min(100f, data.Gauge + 0.5f);
            Path.OnGuard(__instance);
            bool ranged = Vector2.Distance(__instance.current_position, attacker.current_position) > 2.2f;
            Show(__instance, attacker, mine, ranged);
            if (ranged && my_tier >= 2)
            {
                // Turned back: half the damage goes home.
                Bypass = true;
                try { attacker.getHit(Balance.Blow(__instance, attacker, pDamage * 0.5f), true, AttackType.Other, __instance, false); }
                finally { Bypass = false; }
            }
            return false;
        }

        private static void Show(Actor pVictim, Actor pAttacker, MoveSet pSet, bool pRanged)
        {
            Vector3 p = pVictim.current_position;
            if (!Director.OnScreen(p)) return;
            long id = pVictim.getID();
            if (_last_fx.TryGetValue(id, out float last) && CombatCore.Now - last < 0.35f) return;
            _last_fx[id] = CombatCore.Now;
            if (_last_fx.Count > 400) _last_fx.Clear();

            Color c = Elements.Color(CombatCore.ElementFor(pVictim, pSet));
            Vector3 toward = ((Vector3)pAttacker.current_position - p).normalized;
            Vector3 at = p + toward * 0.45f;
            if (pRanged)
            {
                Fx.Instance back = Fx.Play("arts/lock_sparks", at, 1.1f, Color.white, 0f, 0f, false, 26f);
                Fx.Instance shot = Fx.Play("arts/bullet", at, 0.8f, c, 0f, 0.25f, true);
                Fx.MoveTo(shot, pAttacker.current_position, 0.22f);
                CombatCore.Sound("deflect", p, "gi/sfx/arts/");
                if (Randy.randomChance(0.3f)) FloatingText.Show("DEFLECT", p + new Vector3(0f, 1.2f), c, 10);
            }
            else
            {
                Fx.Play("arts/lock_sparks", at, 1f, Color.white, Random.Range(0f, 360f), 0f, false, 28f);
                CombatCore.Sound("guard_tap", p, "gi/sfx/arts/");
                if (Randy.randomChance(0.15f)) FloatingText.Show("GUARD", p + new Vector3(0f, 1.2f), c, 10);
            }
        }
    }
}
