using System.Collections;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Progression;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// Duels between two powered fighters, built from beats: a stare-down, blade locks, aerial clashes,
    /// chases, feints and technique trades, ending in a finisher or (between the mighty) a catastrophe
    /// where their strongest techniques collide. When two gods duel, blows send them across the map.
    /// </summary>
    public static class Duels
    {
        private static int _active;

        public static bool TryStart(Actor a, Actor b, MoveSet sa)
        {
            if (!Settings.Duels || _active >= Settings.MaxDuels) return false;
            CombatData da = CombatCore.Data(a), db = CombatCore.Data(b);
            if (da.InDuel || db.InDuel || db.Casting) return false;
            MoveSet sb = CombatCore.ResolveSet(b, out _);
            if (sb == null || !Director.OnScreen(a.current_position)) return false;
            if (!Cast.CanMove(a) || !Cast.CanMove(b)) return false;
            if (Vector2.Distance(a.current_position, b.current_position) > 7f) return false;
            if (!Randy.randomChance(Settings.DuelChance)) return false;
            ModClass.Instance.StartCoroutine(Run(a, b, sa, sb));
            return true;
        }

        private class Side
        {
            public Cast C;
            public MoveSet Set;
            public Actor Me => C.A;
        }

        private static IEnumerator Run(Actor a, Actor b, MoveSet sa, MoveSet sb)
        {
            _active++;
            Path.OnDuelStart(a, b);
            CombatData da = CombatCore.Data(a), db = CombatCore.Data(b);
            da.InDuel = db.InDuel = true;
            var x = new Side { C = new Cast(a, b, null, sa, Engine.StageLevel(a), CombatCore.ElementFor(a, sa)), Set = sa };
            var y = new Side { C = new Cast(b, a, null, sb, Engine.StageLevel(b), CombatCore.ElementFor(b, sb)), Set = sb };
            x.C.Pow = Path.TechniquePower(a);
            y.C.Pow = Path.TechniquePower(b);
            bool gods = sa.Tier >= 3 && sb.Tier >= 3;

            yield return Engine.Safe(Body(x, y, gods), "duel");

            foreach (Side s in new[] { x, y })
            {
                if (!s.C.Ok) continue;
                Cast.SetHeight(s.Me, 0f);
                Cast.Release(s.Me);
            }
            da.InDuel = db.InDuel = false;
            _active--;
        }

        private static void Hold(Side x, Side y, float s = 3f)
        {
            Cast.Hold(x.Me, s);
            Cast.Hold(y.Me, s);
        }

        private static bool Alive(Side x, Side y) => x.C.Ok && y.C.Ok;

        private static IEnumerator Body(Side x, Side y, bool gods)
        {
            Hold(x, y, 4f);
            yield return StareDown(x, y, gods);
            int beats = Random.Range(3, 5) + Mathf.Min(3, Mathf.Max(x.Set.Tier, y.Set.Tier) / 2);
            for (int i = 0; i < beats && Alive(x, y); i++)
            {
                Hold(x, y, 4f);
                // Whoever has the momentum leads the beat.
                Side lead = Random.value < Momentum(x, y) ? x : y;
                Side other = lead == x ? y : x;
                int kind = Random.Range(0, 6);
                if (kind == 0) yield return BladeLock(lead, other);
                else if (kind == 1) yield return AerialClash(lead, other);
                else if (kind == 2) yield return Chase(lead, other);
                else if (kind == 3) yield return Feint(lead, other);
                else yield return Trade(lead, other);
                if (gods && Alive(x, y) && Random.value < 0.35f) yield return Hurl(lead, other);
                yield return x.C.W(0.15f);
            }
            if (!Alive(x, y)) yield break;

            Hold(x, y, 5f);
            Side winner = Random.value < Momentum(x, y) ? x : y;
            Side loser = winner == x ? y : x;
            if (Mathf.Max(x.Set.Tier, y.Set.Tier) >= 2 && Random.value < (gods ? 0.75f : 0.35f))
                yield return Catastrophe(x, y);
            if (!Alive(x, y)) yield break;
            yield return Finisher(winner, loser);
            if (winner.C.Ok) Path.OnDuel(winner.Me, loser.Me);
        }

        private static float Momentum(Side x, Side y)
        {
            float sx = (x.Set.Tier * 25f + Path.Stage(x.Me) * 12f + Path.FacetTotal(x.Me) * 0.4f +
                        30f * x.Me.getHealth() / Mathf.Max(1f, x.Me.getMaxHealth())) * Path.DuelMomentum(x.Me);
            float sy = (y.Set.Tier * 25f + Path.Stage(y.Me) * 12f + Path.FacetTotal(y.Me) * 0.4f +
                        30f * y.Me.getHealth() / Mathf.Max(1f, y.Me.getMaxHealth())) * Path.DuelMomentum(y.Me);
            return Mathf.Clamp(sx / Mathf.Max(1f, sx + sy), 0.15f, 0.85f);
        }

        private static Vector3 Mid(Side x, Side y) => (x.C.P + y.C.P) * 0.5f;

        // ---------------------------------------------------------------- beats

        private static IEnumerator StareDown(Side x, Side y, bool gods)
        {
            Vector3 mid = Mid(x, y);
            CombatCore.Face(x.Me, y.C.P);
            CombatCore.Face(y.Me, x.C.P);
            ModClass.Instance.StartCoroutine(Director.Spotlight(mid, 1.6f));
            FloatingText.Show(gods ? "CLASH OF GODS" : "DUEL", mid + new Vector3(0f, 1.8f), Color.white, gods ? 17 : 15);
            x.C.Snd("stare", mid);
            x.C.F("fx:aura_flame", x.C.P, 1.8f, 0f, 0.9f, true, 14f, true);
            y.C.F("fx:aura_flame", y.C.P, 1.8f, 0f, 0.9f, true, 14f, true);
            // A gust between them as their auras meet.
            for (int i = 0; i < 3; i++)
            {
                x.C.Dust(mid + Cast.Rand(1f), 1.2f);
                yield return x.C.W(0.18f);
            }
            if (Vector2.Distance(x.C.P, y.C.P) > 2.2f)
            {
                Vector3 meet = mid;
                ModClass.Instance.StartCoroutine(y.C.Dash(meet + (y.C.P - meet).normalized * 0.7f, 0.14f));
                yield return x.C.Dash(meet + (x.C.P - meet).normalized * 0.7f, 0.14f);
            }
            x.C.Snd("sword_clash", Mid(x, y));
            x.C.F("lock_sparks", Mid(x, y), 1.4f, 0f, 0f, false, 24f);
            x.C.Impact(Mid(x, y), 0.4f);
        }

        private static IEnumerator BladeLock(Side lead, Side other)
        {
            Vector3 mid = Mid(lead, other);
            Vector3 d = (other.C.P - lead.C.P).normalized;
            ModClass.Instance.StartCoroutine(Cast.Move(other.Me, mid + d * 0.45f, 0.1f));
            yield return Cast.Move(lead.Me, mid - d * 0.45f, 0.1f);
            FloatingText.Show("LOCK", mid + new Vector3(0f, 1.3f), Color.white, 11);
            // Grinding back and forth.
            for (int i = 0; i < 5; i++)
            {
                float shove = (i % 2 == 0 ? 1f : -1f) * 0.25f;
                Vector3 m = mid + d * shove;
                Cast.Place(lead.Me, m - d * 0.45f);
                Cast.Place(other.Me, m + d * 0.45f);
                lead.C.F("lock_sparks", m, 1.2f, Random.Range(0f, 360f), 0f, false, 30f);
                lead.C.Fc("impact", m, 0.9f, Color.Lerp(lead.C.Hot, other.C.Hot, i % 2), 0f, 0f, false, 30f);
                lead.C.Snd("lock_clash", m);
                Cast.Shake(0.15f);
                yield return lead.C.W(0.12f);
            }
            // The stronger one breaks it.
            lead.C.F("shock", mid, 1.8f, 0f, 0f, false, 22f, true);
            lead.C.Snd("release", mid);
            lead.C.Hit(other.Me, 0.8f);
            yield return lead.C.Knock(other.Me, d, 2.4f, 0.28f);
        }

        private static IEnumerator AerialClash(Side lead, Side other)
        {
            lead.C.Snd("leap", lead.C.P);
            ModClass.Instance.StartCoroutine(Cast.Rise(other.Me, 3f, 0.3f));
            yield return Cast.Rise(lead.Me, 3f, 0.3f);
            for (int i = 0; i < 4 && Alive(lead, other); i++)
            {
                Side striker = i % 2 == 0 ? lead : other;
                Side target = striker == lead ? other : lead;
                Vector3 m = Mid(lead, other);
                Vector3 side = m + new Vector3(Random.Range(-1f, 1f), Random.Range(-0.6f, 0.6f)).normalized * 0.8f;
                striker.C.Blink(side, "wind_ring");
                Cast.SetHeight(striker.Me, 3f + Random.Range(-0.3f, 0.6f));
                Cast.SetHeight(target.Me, 3f);
                Vector3 hit = (Vector3)target.Me.current_position + new Vector3(0f, target.Me.position_height * 0.25f);
                striker.C.F("impact", hit, 1.6f, Random.Range(0f, 360f), 0f, false, 30f, true);
                striker.C.F("lock_sparks", hit, 1.2f, 0f, 0f, false, 30f);
                striker.C.Snd(i % 2 == 0 ? "punch" : "sword_clash", hit);
                striker.C.Hit(target.Me, 0.45f);
                Cast.Shake(0.25f);
                yield return lead.C.W(0.16f);
            }
            // The leader spikes the other down.
            lead.C.F("impact", other.C.P, 2.2f, -90f, 0f, false, 24f, true);
            ModClass.Instance.StartCoroutine(Cast.Fall(lead.Me, 0.3f));
            yield return Cast.Fall(other.Me, 0.12f);
            lead.C.Burst(other.C.P, 2.6f);
            lead.C.Crater(other.C.P, 1.2f);
            lead.C.Snd("slam", other.C.P);
            lead.C.Impact(other.C.P, 0.7f);
            lead.C.Hit(other.Me, 1f);
            yield return lead.C.W(0.25f);
        }

        private static IEnumerator Chase(Side lead, Side other)
        {
            Vector3 away = (other.C.P - lead.C.P).normalized;
            for (int i = 0; i < 3 && Alive(lead, other); i++)
            {
                Vector3 flee = Cast.Clamp(other.C.P + Cast.Rot(away, Random.Range(-60f, 60f)) * Random.Range(2.5f, 4f));
                yield return Cast.Move(other.Me, flee, 0.12f, other.C.Hot);
                other.C.Dust(flee, 1f);
                yield return lead.C.W(0.08f);
                yield return lead.C.Dash(other.C.P - (other.C.P - lead.C.P).normalized * 0.8f, 0.12f);
                lead.C.F("impact", other.C.P, 1.3f, Random.Range(0f, 360f), 0f, false, 28f, true);
                lead.C.Snd("whoosh_light", lead.C.P);
                lead.C.Hit(other.Me, 0.35f);
            }
            if (!Alive(lead, other)) yield break;
            FloatingText.Show("CAUGHT", other.C.P + new Vector3(0f, 1.3f), lead.C.Hot, 11);
            lead.C.F("cross_cut", other.C.P, 2f, 0f, 0f, false, 22f, true);
            lead.C.Snd("slash_heavy", other.C.P);
            lead.C.Hit(other.Me, 1f);
            yield return lead.C.Knock(other.Me, (other.C.P - lead.C.P).normalized, 1.5f, 0.2f);
        }

        private static IEnumerator Feint(Side lead, Side other)
        {
            // A big wind-up... then the strike comes from behind.
            lead.C.F("fx:aura_flame", lead.C.P, 1.6f, 0f, 0.35f, true, 18f, true);
            lead.C.Snd("charge", lead.C.P);
            yield return lead.C.W(0.25f);
            other.C.F("lock_sparks", other.C.P + (lead.C.P - other.C.P).normalized * 0.5f, 1f, 0f, 0f, false, 28f);
            other.C.Snd("guard_tap", other.C.P);
            lead.C.Blink(lead.C.Behind(other.Me, 0.8f), "vacuum");
            FloatingText.Show("FEINT", lead.C.P + new Vector3(0f, 1.3f), lead.C.Hot, 11);
            CombatCore.Face(lead.Me, other.C.P);
            yield return lead.C.W(0.08f);
            lead.C.F("x_cut", other.C.P, 1.8f, 0f, 0f, false, 26f, true);
            lead.C.Snd("slash_heavy", other.C.P);
            lead.C.Hit(other.Me, 1.1f);
            lead.C.Impact(other.C.P, 0.5f);
            yield return lead.C.Knock(other.Me, (other.C.P - lead.C.P).normalized, 1.2f, 0.18f);
        }

        /// <summary>Each fighter answers with one of their real techniques.</summary>
        private static IEnumerator Trade(Side lead, Side other)
        {
            foreach (Side s in new[] { lead, other })
            {
                if (!Alive(lead, other)) yield break;
                Technique t = Tactics.AnyAttack(s.Me, s.Set);
                if (t == null) continue;
                var c = new Cast(s.Me, s == lead ? other.Me : lead.Me, t, s.Set, Engine.Level(s.Me, t), s.C.Element)
                {
                    Pow = s.C.Pow * 0.6f
                };
                c.Call();
                yield return Engine.Safe(t.Script(c), t.Id);
                Hold(lead, other, 4f);
                Cast.SetHeight(s.Me, 0f);
                yield return lead.C.W(0.15f);
            }
        }

        /// <summary>Between gods: a blow that sends the other flying across the map, and a pursuit.</summary>
        private static IEnumerator Hurl(Side lead, Side other)
        {
            Vector3 d = (other.C.P - lead.C.P).normalized;
            if (d.sqrMagnitude < 0.01f) d = Vector3.right;
            float distance = Random.Range(14f, 24f);
            lead.C.F("shock", other.C.P, 3f, 0f, 0f, false, 20f, true);
            lead.C.Snd("crater_boom", other.C.P);
            lead.C.Impact(other.C.P, 1f);
            lead.C.Hit(other.Me, 1.2f);
            Vector3 from = other.C.P;
            Vector3 to = Cast.Clamp(from + d * distance);
            FloatingText.Show("SENT FLYING", from + new Vector3(0f, 1.5f), lead.C.Hot, 12);
            ModClass.Instance.StartCoroutine(Follow(other.Me, 0.55f));
            yield return Cast.Move(other.Me, to, 0.5f, other.C.Hot);
            // A trench where they skidded to a halt.
            lead.C.Fissure(Vector3.Lerp(from, to, 0.55f), to, 1.2f);
            lead.C.Crater(to, 2f);
            lead.C.Burst(to, 3f);
            lead.C.Snd("slam", to);
            // The pursuer streaks after them.
            lead.C.Beam("iai", lead.C.P, to - d, 0.6f, 0.35f, true, false);
            yield return lead.C.Dash(to - d * 1.1f, 0.25f);
            lead.C.Snd("blink", lead.C.P);
        }

        private static IEnumerator Follow(Actor pActor, float pSeconds)
        {
            float t = 0f;
            while (t < pSeconds + 0.3f && CombatCore.Alive(pActor))
            {
                t += Time.unscaledDeltaTime;
                World.world.locatePosition(pActor.current_position);
                yield return null;
            }
        }

        /// <summary>Their strongest techniques meet in the middle and the land pays for it.</summary>
        private static IEnumerator Catastrophe(Side x, Side y)
        {
            Vector3 mid = Mid(x, y);
            Vector3 d = (y.C.P - x.C.P).normalized;
            yield return x.C.Dash(mid - d * 3.5f, 0.15f);
            yield return y.C.Dash(mid + d * 3.5f, 0.15f);
            mid = Mid(x, y);
            ModClass.Instance.StartCoroutine(Director.Spotlight(mid, 2.2f));
            FloatingText.Show("CATASTROPHE", mid + new Vector3(0f, 2.2f), Color.white, 17);
            x.C.F("lifeburn", x.C.P, 2.4f, 0f, 0.9f, true, 16f);
            y.C.F("lifeburn", y.C.P, 2.4f, 0f, 0.9f, true, 16f);
            x.C.Snd("charge", mid);
            yield return x.C.W(0.7f);
            // Two beams push against each other.
            float t = 0f;
            float balance = Momentum(x, y);
            while (t < 1.1f)
            {
                float k = Mathf.Sin(t * 9f) * 0.12f + (balance - 0.5f) * t;
                Vector3 meet = Vector3.Lerp(x.C.P, y.C.P, 0.5f + k);
                x.C.Beam("fx:beam", x.C.P, meet, 1f + t, 0.12f, true);
                y.C.Beam("fx:beam", y.C.P, meet, 1f + t, 0.12f, true);
                x.C.F("lock_sparks", meet, 1.4f + t, Random.Range(0f, 360f), 0f, false, 30f);
                if (Random.value < 0.4f) x.C.Snd("beam_fire", meet);
                Cast.Shake(0.3f);
                t += 0.1f;
                yield return x.C.W(0.1f);
            }
            Vector3 blast = Vector3.Lerp(x.C.P, y.C.P, Mathf.Clamp01(0.5f + (balance - 0.5f) * 1.1f));
            x.C.Burst(blast, 7f);
            y.C.F("shock", blast, 6f, 0f, 0f, false, 16f, true);
            x.C.F("star_rift", blast, 5f, 0f, 0f, false, 16f);
            x.C.Impact(blast, 1f);
            Director.HitStop(0.2f);
            x.C.Snd("explosion_big", blast);
            x.C.Crater(blast, 4f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 r = Cast.Rot(d, Random.Range(0f, 360f));
                x.C.Fissure(blast, blast + r * Random.Range(4f, 7f), 1.1f);
            }
            Cast.Scorch(blast, 4f, 6);
            x.C.Area(blast, 4.5f, 2f);
            y.C.Area(blast, 4.5f, 2f);
            x.C.Hit(y.Me, 0.8f);
            y.C.Hit(x.Me, 0.8f);
            ModClass.Instance.StartCoroutine(y.C.Knock(x.Me, -d, 2.5f, 0.35f));
            yield return x.C.Knock(y.Me, d, 2.5f, 0.35f);
        }

        private static IEnumerator Finisher(Side winner, Side loser)
        {
            Technique best = Tactics.Strongest(winner.Me, winner.Set);
            FloatingText.Show("FINISHER", winner.C.P + new Vector3(0f, 1.8f), winner.C.Hot, 14);
            if (best == null)
            {
                winner.C.Hit(loser.Me, 3f);
                yield break;
            }
            var c = new Cast(winner.Me, loser.Me, best, winner.Set, Mathf.Max(2, Engine.Level(winner.Me, best)),
                winner.C.Element) { Pow = winner.C.Pow * 1.5f };
            c.Call();
            yield return Engine.Safe(best.Script(c), best.Id);
            if (CombatCore.Alive(loser.Me)) Director.Impact(loser.C.P, winner.C.Hot, 0.8f);
        }
    }
}
