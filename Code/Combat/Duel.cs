using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Cinematics;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// Choreographed fights between two powered units: blade exchanges, barrages, parries and counters,
    /// weaving dodges and combos, ending in a finisher with an impact frame. Both fighters are held in
    /// place by the gi_duel status while the duel directs their movement.
    /// </summary>
    public static class Duel
    {
        public const string StatusId = "gi_duel";
        private static int _active;

        public static void Init()
        {
            var status = new StatusAsset
            {
                id = StatusId,
                locale_id = "status_title_gi_duel",
                locale_description = "status_description_gi_duel",
                path_icon = "gi/icons/chronicle",
                duration = 20f,
                cancel_actor_job = true,
                allow_timer_reset = true
            };
            AssetManager.status.add(status);
            status.base_stats.addTag("immovable");
            status.base_stats.addTag("frozen_ai");
            status.base_stats.addTag("stop_idle_animation");
            status.base_stats["speed"] = -10000f;
        }

        public static bool TryStart(Actor pA, Actor pB, MoveSet pSetA)
        {
            if (!Settings.Duels || _active >= Settings.MaxDuels) return false;
            CombatData da = CombatCore.Data(pA), db = CombatCore.Data(pB);
            if (da.InDuel || db.InDuel) return false;
            MoveSet set_b = CombatCore.ResolveSet(pB, out _);
            if (set_b == null) return false;
            if (!Director.OnScreen(CombatCore.Pos(pA))) return false;
            if (Vector2.Distance(pA.current_position, pB.current_position) > 6f) return false;
            if (!Randy.randomChance(Settings.DuelChance)) return false;

            ModClass.Instance.StartCoroutine(Run(pA, pB, pSetA, set_b));
            return true;
        }

        private static IEnumerator Run(Actor a, Actor b, MoveSet sa, MoveSet sb)
        {
            _active++;
            CombatData da = CombatCore.Data(a), db = CombatCore.Data(b);
            da.InDuel = db.InDuel = true;
            a.addStatusEffect(StatusId, 20f);
            b.addStatusEffect(StatusId, 20f);

            Color ca = Elements.Color(sa.Element), cb = Elements.Color(sb.Element);
            Vector3 mid = ((Vector3)a.current_position + (Vector3)b.current_position) * 0.5f;
            FloatingText.Show("DUEL!", mid, Color.white, 16);
            CombatCore.Sound("clang", mid);

            // Close the distance
            if (Vector2.Distance(a.current_position, b.current_position) > 1.6f)
            {
                Vector3 dir = ((Vector3)b.current_position - (Vector3)a.current_position).normalized;
                Vector3 to = (Vector3)b.current_position - dir * 1.2f;
                Fx.Line("streak", a.current_position, to, 0.9f, ca, 0.25f);
                yield return CombatCore.Slide(a, to, 0.14f);
            }

            int beats = Random.Range(4, 7) + Mathf.Min(3, Mathf.Max(sa.Tier, sb.Tier) / 2);
            Actor attacker = a, defender = b;
            for (int i = 0; i < beats; i++)
            {
                if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) break;
                CombatCore.Face(a, b.current_position);
                CombatCore.Face(b, a.current_position);

                // Faster and stronger fighters take the initiative more often
                float edge = (a.stats["speed"] + a.stats["damage"]) / Mathf.Max(1f,
                    a.stats["speed"] + a.stats["damage"] + b.stats["speed"] + b.stats["damage"]);
                bool a_leads = Random.value < edge;
                attacker = a_leads ? a : b;
                defender = a_leads ? b : a;
                Color col = a_leads ? ca : cb;
                Color def_col = a_leads ? cb : ca;
                MoveSet att_set = a_leads ? sa : sb;

                float roll = Random.value;
                if (roll < 0.28f) yield return Exchange(attacker, defender, col, def_col);
                else if (roll < 0.48f) yield return Barrage(attacker, defender, col, att_set);
                else if (roll < 0.68f) yield return Parry(attacker, defender, col, def_col);
                else if (roll < 0.84f) yield return Weave(attacker, defender, col, def_col);
                else yield return Combo(attacker, defender, col, att_set);

                yield return CombatCore.Wait(0.22f);
            }

            // Finisher by whoever is healthier
            if (CombatCore.Alive(a) && CombatCore.Alive(b))
            {
                float ra = a.getHealth() / (float)Mathf.Max(1, a.getMaxHealth());
                float rb = b.getHealth() / (float)Mathf.Max(1, b.getMaxHealth());
                Actor winner = ra >= rb ? a : b;
                Actor loser = winner == a ? b : a;
                MoveSet ws = winner == a ? sa : sb;
                MoveAsset finisher = CombatAI.Strongest(ws, out _);
                if (finisher != null)
                {
                    FloatingText.Show("FINISH!", winner.current_position, Elements.Color(ws.Element), 18);
                    yield return MoveExecutor.Run(winner, loser, finisher, ws);
                    Director.Impact(loser.current_position, Elements.Color(ws.Element), Mathf.Clamp01(0.5f + ws.Tier * 0.1f));
                }
            }

            foreach (Actor fighter in new List<Actor> { a, b })
            {
                if (!CombatCore.Alive(fighter)) continue;
                fighter.finishStatusEffect(StatusId);
                CombatCore.Data(fighter).InDuel = false;
                CombatCore.Data(fighter).NextCast = CombatCore.Now + 1.5f;
            }
            da.InDuel = db.InDuel = false;
            _active--;
        }

        // ---------------------------------------------------------------- beats

        /// <summary>Blades meet again and again: sparks at the midpoint, both take chip damage.</summary>
        private static IEnumerator Exchange(Actor a, Actor b, Color ca, Color cb)
        {
            int clashes = Random.Range(4, 7);
            for (int i = 0; i < clashes; i++)
            {
                if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
                Vector3 mid = ((Vector3)a.current_position + (Vector3)b.current_position) * 0.5f;
                Fx.Play("slash", (Vector3)a.current_position + (mid - (Vector3)a.current_position) * 0.6f, 1.6f, ca,
                    Random.Range(-40f, 40f), 0f, false, 28f);
                Fx.Play("slash", (Vector3)b.current_position + (mid - (Vector3)b.current_position) * 0.6f, 1.6f, cb,
                    180f + Random.Range(-40f, 40f), 0f, false, 28f);
                Fx.Play("spark", mid + (Vector3)Random.insideUnitCircle * 0.2f, 1.6f, Color.white, Random.Range(0f, 360f));
                CombatCore.Sound(i % 2 == 0 ? "clang" : "swing", mid);
                CombatCore.Hit(a, b, 0.2f, "physical");
                CombatCore.Hit(b, a, 0.2f, "physical");
                yield return CombatCore.Wait(0.11f);
            }
        }

        /// <summary>A flurry of hits ending in a knockback.</summary>
        private static IEnumerator Barrage(Actor a, Actor b, Color ca, MoveSet sa)
        {
            int hits = Random.Range(6, 11);
            for (int i = 0; i < hits; i++)
            {
                if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
                Vector3 p = (Vector3)b.current_position + (Vector3)Random.insideUnitCircle * 0.4f;
                Fx.Play("slash", p, 1.6f, ca, Random.Range(0f, 360f), 0f, false, 30f);
                Fx.Play("hit", p, 1f, Color.white);
                if (i % 2 == 0) CombatCore.Sound("hit", p);
                CombatCore.Hit(a, b, 0.22f, sa.Element);
                yield return CombatCore.Wait(0.06f);
            }
            if (!CombatCore.Alive(b)) yield break;
            FloatingText.Show($"{hits} HITS", b.current_position, ca, 12);
            Vector3 dir = ((Vector3)b.current_position - (Vector3)a.current_position).normalized;
            Fx.Play("slash_big", b.current_position, 2.6f, ca, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            CombatCore.Sound("hit_heavy", b.current_position);
            yield return CombatCore.Slide(b, (Vector3)b.current_position + dir * 1.6f, 0.16f);
        }

        /// <summary>A heavy swing is parried with a flash of sparks, then countered.</summary>
        private static IEnumerator Parry(Actor a, Actor b, Color ca, Color cb)
        {
            if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
            Fx.Play("circle", a.current_position, 1.6f, ca, 0f, 0.25f, true, 20f, "EffectsBack");
            yield return CombatCore.Wait(0.18f);
            Vector3 mid = ((Vector3)a.current_position + (Vector3)b.current_position) * 0.5f;
            Fx.Play("slash_big", mid, 2.4f, ca, Random.Range(-30f, 30f));
            Fx.Play("spark", mid, 2.6f, Color.white);
            Fx.Play("shield", b.current_position, 1.8f, cb);
            CombatCore.Sound("clang", mid);
            FloatingText.Show("PARRY!", b.current_position, cb, 14);
            yield return CombatCore.Wait(0.15f);
            if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
            // Counter-attack
            Fx.Line("streak", b.current_position, a.current_position, 0.7f, cb, 0.2f);
            Fx.Play("slash_big", a.current_position, 2.8f, cb, Random.Range(0f, 360f));
            Fx.Play("hit", a.current_position, 1.4f, Color.white);
            CombatCore.Sound("hit_heavy", a.current_position);
            CombatCore.Hit(b, a, 1.2f, "physical");
            FloatingText.Show("COUNTER", a.current_position, cb, 11);
        }

        /// <summary>Strikes that miss as the defender weaves aside, leaving afterimages, then answers.</summary>
        private static IEnumerator Weave(Actor a, Actor b, Color ca, Color cb)
        {
            for (int i = 0; i < 3; i++)
            {
                if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
                Vector3 old_pos = b.current_position;
                Vector3 side = Vector3.Cross(((Vector3)b.current_position - (Vector3)a.current_position).normalized,
                    Vector3.forward) * (i % 2 == 0 ? 0.9f : -0.9f);
                Fx.Play("slash", old_pos, 1.8f, ca, Random.Range(0f, 360f), 0f, false, 28f);
                CombatCore.Sound("swing", old_pos);
                Fx.Play("vanish", old_pos, 1.2f, cb);
                Fx.Line("streak", old_pos, old_pos + side, 0.6f, cb, 0.2f);
                yield return CombatCore.Slide(b, old_pos + side, 0.08f);
                FloatingText.Show("MISS", old_pos, Color.white, 9);
                yield return CombatCore.Wait(0.08f);
            }
            if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
            Fx.Play("slash_big", a.current_position, 2.6f, cb, Random.Range(0f, 360f));
            CombatCore.Sound("hit_heavy", a.current_position);
            CombatCore.Hit(b, a, 1.0f, "physical");
        }

        /// <summary>A chain: strike, blink behind, flurry, launcher, finisher burst.</summary>
        private static IEnumerator Combo(Actor a, Actor b, Color ca, MoveSet sa)
        {
            int count = 0;
            if (!CombatCore.Alive(b)) yield break;
            Fx.Play("slash", b.current_position, 2f, ca, 0f);
            CombatCore.Hit(a, b, 0.4f, sa.Element);
            count++;
            yield return CombatCore.Wait(0.1f);

            if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
            Vector3 behind = (Vector3)b.current_position + ((Vector3)b.current_position - (Vector3)a.current_position).normalized;
            Fx.Play("vanish", a.current_position, 1.4f, ca);
            a.setCurrentTilePosition(CombatCore.TileAt(behind));
            a.current_position = behind;
            Fx.Play("vanish", behind, 1.4f, ca);
            CombatCore.Sound("dash", behind);
            yield return CombatCore.Wait(0.06f);

            for (int i = 0; i < 4; i++)
            {
                if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
                Fx.Play("slash", b.current_position, 1.7f, ca, Random.Range(0f, 360f), 0f, false, 30f);
                Fx.Play("hit", b.current_position, 1f, Color.white);
                CombatCore.Hit(a, b, 0.25f, sa.Element);
                count++;
                yield return CombatCore.Wait(0.06f);
            }

            if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) yield break;
            Fx.Play("slash_big", b.current_position, 3f, ca, 90f);
            Fx.Play("burst", b.current_position, 2.6f, ca);
            CombatCore.Sound("hit_heavy", b.current_position);
            CombatCore.Hit(a, b, 1.1f, sa.Element);
            count++;
            FloatingText.Show($"COMBO x{count}", b.current_position, ca, 13);
            if (sa.Tier >= 2) Director.Impact(b.current_position, ca, 0.4f);
        }
    }
}
