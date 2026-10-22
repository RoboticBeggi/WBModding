using System.Collections.Generic;
using ai.behaviours;
using HarmonyLib;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Powers
{
    /// <summary>
    /// How the Dragon Sovereigns behave. In the lore they ruled the Old World over their own elemental
    /// creatures; they didn't raze every living thing. So a Sovereign:
    /// - never uses WorldBox's dragon attacks (fire breath, swoop, burning tiles, city raids); it fights
    ///   only with its own style's techniques,
    /// - keeps to a territory around its lair,
    /// - fights whoever attacks it, and powerful beings that stray close, but leaves ordinary folk alone,
    /// - now and then clashes with a rival Sovereign. A clash is a contest, not a death match: it ends
    ///   when one is driven below 30% health, and that one yields.
    /// </summary>
    public static class DragonRule
    {
        private const float TerritoryRadius = 22f;
        private const float RivalFloor = 0.3f;

        private class Clash
        {
            public Actor A, B;
            public float Until;
        }

        private static Clash _clash;
        private static float _next_clash = 120f;
        private static float _tick;

        public static bool IsSovereign(Actor pActor)
        {
            if (pActor == null) return false;
            if (pActor.hasTrait("gi_nibelung")) return true;
            foreach (string element in TeyvatTraits.Elements)
            {
                if (pActor.hasTrait("gi_sovereign_" + element)) return true;
            }
            return false;
        }

        /// <summary>A dragon-bodied Sovereign whose WorldBox dragon behaviour is replaced.</summary>
        private static bool Ruled(Actor pActor) => pActor != null && pActor.avatar != null && IsSovereign(pActor);

        public static void Patch(Harmony pHarmony)
        {
            Prefix(pHarmony, typeof(BehDragonSlide), nameof(BehDragonSlide.execute), nameof(StopAttack));
            Prefix(pHarmony, typeof(BehDragonLandAttack), nameof(BehDragonLandAttack.execute), nameof(StopAttack));
            Prefix(pHarmony, typeof(BehDragonCheckAttackCity), nameof(BehDragonCheckAttackCity.execute), nameof(NoRaid));
            Prefix(pHarmony, typeof(BehDragonCheckOverTargetCity), nameof(BehDragonCheckOverTargetCity.execute), nameof(NoRaid));
            Prefix(pHarmony, typeof(BehDragonFindRandomTile), nameof(BehDragonFindRandomTile.execute), nameof(Roam));
            Prefix(pHarmony, typeof(Dragon), nameof(Dragon.attackTile), nameof(NoBurn));
            Prefix(pHarmony, typeof(Dragon), nameof(Dragon.playSound), nameof(NoBurn));
        }

        private static void Prefix(Harmony pHarmony, System.Type pType, string pMethod, string pPatch)
        {
            try
            {
                pHarmony.Patch(AccessTools.Method(pType, pMethod), prefix: new HarmonyMethod(typeof(DragonRule), pPatch));
            }
            catch (System.Exception e)
            {
                ModClass.LogWarning($"[Dragons] Could not patch {pType.Name}.{pMethod}: {e.Message}");
            }
        }

        // ---------------------------------------------------------------- vanilla suppression

        private static bool StopAttack(Actor pActor, ref BehResult __result)
        {
            if (!Ruled(pActor)) return true;
            __result = BehResult.Stop;
            return false;
        }

        private static bool NoRaid(Actor pActor, ref BehResult __result)
        {
            if (!Ruled(pActor)) return true;
            pActor.data.removeLong("cityToAttack");
            pActor.data.removeInt("attacksForCity");
            __result = BehResult.Continue;
            return false;
        }

        private static bool NoBurn(Dragon __instance) => !Ruled(__instance.actor);

        /// <summary>Sovereigns wander their territory, or fly to their rival during a clash.</summary>
        private static bool Roam(Actor pActor, ref BehResult __result)
        {
            if (!Ruled(pActor) || pActor.beh_tile_target != null) return true;
            Actor rival = RivalOf(pActor);
            WorldTile target;
            if (rival != null)
            {
                target = rival.current_tile;
            }
            else
            {
                Vector2 lair = Lair(pActor);
                Vector2 p = lair + Random.insideUnitCircle * TerritoryRadius;
                target = CombatCore.TileAt(p);
            }
            if (target == null) return true;
            pActor.beh_tile_target = target;
            __result = BehResult.Continue;
            return false;
        }

        public static Vector2 Lair(Actor pActor)
        {
            pActor.data.get("gi_lair_x", out float x, -1f);
            pActor.data.get("gi_lair_y", out float y, -1f);
            if (x < 0f)
            {
                x = pActor.current_position.x;
                y = pActor.current_position.y;
                pActor.data.set("gi_lair_x", x);
                pActor.data.set("gi_lair_y", y);
            }
            return new Vector2(x, y);
        }

        // ---------------------------------------------------------------- whom they fight

        /// <summary>
        /// The foe a Sovereign turns its techniques on: its rival during a clash, whoever is attacking it,
        /// or a powerful being (Exalted or above) close by. Ordinary folk are left alone.
        /// </summary>
        public static Actor FoeFor(Actor pDragon)
        {
            Actor rival = RivalOf(pDragon);
            if (rival != null && Vector2.Distance(rival.current_position, pDragon.current_position) < 14f) return rival;
            if (pDragon.attackedBy is Actor attacker && attacker.isAlive() && attacker != pDragon &&
                Vector2.Distance(attacker.current_position, pDragon.current_position) < 14f && !IsSovereign(attacker))
                return attacker;
            Actor best = null;
            float best_d = 12f * 12f;
            foreach (Actor unit in World.world.units.getSimpleList())
            {
                if (unit == null || unit == pDragon || !unit.isAlive() || IsSovereign(unit)) continue;
                if (unit.attack_target != pDragon && Balance.TierOf(unit) < 2) continue;
                if (!CombatCore.IsEnemy(pDragon, unit) && unit.attack_target != pDragon) continue;
                float d = ((Vector2)unit.current_position - pDragon.current_position).sqrMagnitude;
                if (d < best_d)
                {
                    best_d = d;
                    best = unit;
                }
            }
            return best;
        }

        // ---------------------------------------------------------------- rival clashes

        public static Actor RivalOf(Actor pActor)
        {
            if (_clash == null || CombatCore.Now > _clash.Until) return null;
            if (pActor == _clash.A) return Alive(_clash.B) ? _clash.B : null;
            if (pActor == _clash.B) return Alive(_clash.A) ? _clash.A : null;
            return null;
        }

        private static bool Alive(Actor a) => a != null && a.isAlive();

        public static void Tick()
        {
            if (World.world?.units == null || Config.worldLoading || CombatCore.Now < _tick) return;
            _tick = CombatCore.Now + 2f;
            if (_clash != null && (CombatCore.Now > _clash.Until || !Alive(_clash.A) || !Alive(_clash.B))) End(null);
            if (_clash != null || CombatCore.Now < _next_clash || !Settings.DragonRivalry) return;

            var sovereigns = new List<Actor>();
            foreach (Actor unit in World.world.units.getSimpleList())
            {
                if (Alive(unit) && Ruled(unit)) sovereigns.Add(unit);
            }
            if (sovereigns.Count < 2)
            {
                _next_clash = CombatCore.Now + 60f;
                return;
            }
            Actor a = sovereigns[Random.Range(0, sovereigns.Count)];
            Actor b = null;
            float best = 60f * 60f;
            foreach (Actor other in sovereigns)
            {
                if (other == a) continue;
                if (other.getHealth() < other.getMaxHealth() * 0.6f || a.getHealth() < a.getMaxHealth() * 0.6f) continue;
                float d = ((Vector2)other.current_position - a.current_position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    b = other;
                }
            }
            if (b == null)
            {
                _next_clash = CombatCore.Now + 60f;
                return;
            }
            _clash = new Clash { A = a, B = b, Until = CombatCore.Now + 45f };
            a.beh_tile_target = null;
            b.beh_tile_target = null;
            Vector3 mid = ((Vector3)a.current_position + (Vector3)b.current_position) * 0.5f;
            string title = $"{a.getName()} challenges {b.getName()}";
            FloatingText.Show("SOVEREIGNS CLASH", mid + new Vector3(0f, 3f), new Color(1f, 0.85f, 0.45f), 16);
            WorldTip.showNow(title, false, "top", 4f);
        }

        /// <summary>Ends the clash; the loser (if any) yields and flies home.</summary>
        private static void End(Actor pLoser)
        {
            if (_clash == null) return;
            if (pLoser != null && Alive(pLoser))
            {
                FloatingText.Show("YIELDS", (Vector3)pLoser.current_position + new Vector3(0f, 2.5f), Color.white, 14);
                Vector2 lair = Lair(pLoser);
                pLoser.beh_tile_target = CombatCore.TileAt(lair);
            }
            _clash = null;
            _next_clash = CombatCore.Now + Random.Range(150f, 300f);
        }

        /// <summary>
        /// Keeps a clash from becoming a death match: a Sovereign hit by its rival never drops below 30%
        /// health; reaching that line ends the clash.
        /// </summary>
        public static float ClampRivalDamage(Actor pAttacker, Actor pVictim, float pDamage)
        {
            if (_clash == null || RivalOf(pVictim) != pAttacker) return pDamage;
            float floor = pVictim.getMaxHealth() * RivalFloor;
            float room = pVictim.getHealth() - floor;
            if (pDamage < room) return pDamage;
            End(pVictim);
            return Mathf.Max(0f, room);
        }
    }
}
