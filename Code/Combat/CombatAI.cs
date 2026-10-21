using System.Collections.Generic;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// Decides when a powered unit uses a move. Hooked into every Teyvat trait's attack action, so it
    /// runs whenever the unit attacks something. Picks the strongest ready move that fits the range,
    /// spends energy on bursts, and sometimes turns the fight into a choreographed duel.
    /// </summary>
    public static class CombatAI
    {
        private const float CastGap = 0.9f;

        /// <summary>The trait hook: only queues the decision so it runs on the main thread.</summary>
        public static bool OnAttack(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile)
        {
            if (!(pSelf is Actor caster) || !(pTarget is Actor target)) return false;
            MainThread.Enqueue(() => Decide(caster, target));
            return false;
        }

        public static bool Decide(Actor caster, Actor target)
        {
            if (!Settings.Moves || !ChronicleReady()) return false;
            if (!caster.isAlive() || !target.isAlive()) return false;

            CombatData data = CombatCore.Data(caster);
            if (data.InDuel || CombatCore.Now < data.NextCast) return false;

            MoveSet set = CombatCore.ResolveSet(caster, out _);
            if (set == null) return false;
            data.Energy += 4f * set.Tier;

            if (Duel.TryStart(caster, target, set)) return true;

            MoveAsset move = Choose(caster, target, set, data, out string move_id);
            if (move == null) return false;

            data.NextCast = CombatCore.Now + CastGap;
            data.ReadyAt[move_id] = CombatCore.Now + move.Cooldown / Mathf.Max(0.1f, Settings.CooldownScale);
            if (move.Energy > 0f) data.Energy = Mathf.Max(0f, data.Energy - move.Energy);
            MoveExecutor.Cast(caster, target, move, set);
            return true;
        }

        private static bool ChronicleReady() => World.world != null && !Config.worldLoading;

        /// <summary>Bursts first when charged, then whatever ready move fits; random among equals.</summary>
        public static MoveAsset Choose(Actor pCaster, Actor pTarget, MoveSet pSet, CombatData pData, out string pId)
        {
            pId = null;
            float distance = Vector2.Distance(pCaster.current_position, pTarget.current_position);
            float health = pCaster.getHealth() / (float)Mathf.Max(1, pCaster.getMaxHealth());
            var options = new List<string>();
            string burst = null;

            foreach (string id in pSet.Moves)
            {
                MoveAsset move = MoveData.Get(id);
                if (move == null) continue;
                if (pData.ReadyAt.TryGetValue(id, out float ready) && ready > CombatCore.Now) continue;
                if (move.Energy > 0f && pData.Energy < move.Energy) continue;
                bool self_move = move.Archetype == "shield" || move.Archetype == "buff" || move.Archetype == "heal";
                if (!self_move && distance > Mathf.Max(1.5f, move.Range) + 0.5f) continue;
                if (move.Archetype == "heal" && health > 0.6f) continue;
                if (move.Energy > 0f) burst = id;
                else options.Add(id);
            }

            if (burst != null) pId = burst;
            else if (options.Count > 0) pId = options[Randy.randomInt(0, options.Count)];
            return pId != null ? MoveData.Get(pId) : null;
        }

        /// <summary>The strongest move the unit can use right now, ignoring cooldowns (duel finishers).</summary>
        public static MoveAsset Strongest(MoveSet pSet, out string pId)
        {
            pId = null;
            MoveAsset best = null;
            foreach (string id in pSet.Moves)
            {
                MoveAsset move = MoveData.Get(id);
                if (move == null || move.Archetype == "heal" || move.Archetype == "buff" || move.Archetype == "shield") continue;
                if (best == null || move.Power * Mathf.Max(1, move.Hits) > best.Power * Mathf.Max(1, best.Hits))
                {
                    best = move;
                    pId = id;
                }
            }
            return best;
        }
    }
}
