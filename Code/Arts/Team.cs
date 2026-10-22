using System.Collections;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// Team arts: two powered allies fighting the same enemy combine their styles.
    /// Sky Relay passes the enemy around the sky, Twin Meteors crash down together, Crossed Beams meet at
    /// the enemy, and a Merged Aura Wave rolls out from both fighters at once.
    /// </summary>
    public static class Team
    {
        public static bool TryStart(Actor a, Actor target, MoveSet set)
        {
            if (!Settings.TeamArts || !Director.OnScreen(a.current_position)) return false;
            CombatData da = CombatCore.Data(a);
            if (CombatCore.Now - da.LastTeam < 25f || !Randy.randomChance(0.2f)) return false;

            Actor partner = null;
            MoveSet partner_set = null;
            foreach (Actor ally in CombatCore.AlliesNear(a, 5f))
            {
                if (ally == a) continue;
                MoveSet s = CombatCore.ResolveSet(ally, out _);
                if (s == null) continue;
                CombatData d = CombatCore.Data(ally);
                if (d.Casting || d.InDuel || CombatCore.Now - d.LastTeam < 25f) continue;
                partner = ally;
                partner_set = s;
                break;
            }
            if (partner == null) return false;

            CombatData db = CombatCore.Data(partner);
            da.LastTeam = db.LastTeam = CombatCore.Now;
            var ca = new Cast(a, target, null, set, Engine.StageLevel(a), CombatCore.ElementFor(a, set)) { Pow = 1.3f };
            var cb = new Cast(partner, target, null, partner_set, Engine.StageLevel(partner),
                CombatCore.ElementFor(partner, partner_set)) { Pow = 1.3f };
            ModClass.Instance.StartCoroutine(Run(ca, cb));
            return true;
        }

        private static IEnumerator Run(Cast a, Cast b)
        {
            CombatData da = CombatCore.Data(a.A), db = CombatCore.Data(b.A);
            da.Casting = db.Casting = true;
            Cast.Hold(a.A, 5f);
            Cast.Hold(b.A, 5f);

            bool movable = Cast.CanMove(a.A) && Cast.CanMove(b.A) && Cast.CanMove(a.T);
            int pick = Random.Range(0, movable ? 4 : 2);
            string[] names = { "Crossed Beams", "Merged Aura Wave", "Sky Relay", "Twin Meteors" };
            Vector3 mid = (a.P + b.P) * 0.5f;
            FloatingText.Show("TEAM ART", mid + new Vector3(0f, 2f), Color.white, 11);
            FloatingText.Show(names[pick], mid + new Vector3(0f, 1.4f), Color.Lerp(a.Hot, b.Hot, 0.5f), 15);
            a.Snd("team_sting", mid);

            IEnumerator routine = pick switch
            {
                0 => CrossedBeams(a, b),
                1 => AuraWave(a, b),
                2 => SkyRelay(a, b),
                _ => TwinMeteors(a, b)
            };
            yield return Engine.Safe(routine, "team." + names[pick]);

            foreach (Cast c in new[] { a, b })
            {
                if (!c.Ok) continue;
                Cast.SetHeight(c.A, 0f);
                Cast.Release(c.A);
            }
            da.Casting = db.Casting = false;
        }

        private static IEnumerator CrossedBeams(Cast a, Cast b)
        {
            Vector3 t = a.TP;
            // Swing into firing positions at right angles around the enemy.
            Vector3 da = (a.P - t).normalized, db = Cast.Rot(da, 90f);
            yield return a.Dash(t + da * 4f, 0.15f);
            yield return b.Dash(t + db * 4f, 0.15f);
            a.F("sigil", a.P, 1.6f, 0f, 0.5f, true, 20f, true);
            b.F("sigil", b.P, 1.6f, 0f, 0.5f, true, 20f, true);
            a.Snd("charge", a.P);
            yield return a.W(0.45f);
            a.Beam("fx:beam", a.P, t - da * 2f, 1.2f, 0.6f, true);
            b.Beam("fx:beam", b.P, t - db * 2f, 1.2f, 0.6f, true);
            a.Snd("beam_fire", t);
            yield return a.W(0.15f);
            a.F("x_cut", t, 3.5f, 45f, 0f, false, 18f, true);
            b.F("cross_cut", t, 3f, 0f, 0f, false, 18f, true);
            a.Burst(t, 4f);
            a.Impact(t, 1f);
            a.Snd("explosion_big", t);
            a.Area(t, 2.5f, 3f);
            b.Area(t, 2.5f, 3f);
            a.Crater(t, 2f);
            yield return a.W(0.4f);
        }

        private static IEnumerator AuraWave(Cast a, Cast b)
        {
            Vector3 mid = (a.P + b.P) * 0.5f;
            a.F("lifeburn", a.P, 2.5f, 0f, 0.9f, true, 16f);
            b.F("lifeburn", b.P, 2.5f, 0f, 0.9f, true, 16f);
            a.Snd("charge", mid);
            yield return a.W(0.6f);
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance ra = a.F("shock", mid, 2f + i, 0f, 0.6f, false, 14f, true);
                Fx.Instance rb = b.F("wind_ring", mid, 2.5f + i, 0f, 0.6f, false, 14f, true);
                Cast.Grow(ra, 4f);
                Cast.Grow(rb, 4f);
                a.Snd("shockwave", mid);
                a.Area(mid, 3f + i * 1.5f, 1.2f);
                b.Area(mid, 3f + i * 1.5f, 1.2f);
                a.PushAll(mid, 3f + i * 1.5f, 1.5f);
                Cast.Shake(0.5f);
                yield return a.W(0.22f);
            }
            a.Impact(mid, 0.8f);
        }

        private static IEnumerator SkyRelay(Cast a, Cast b)
        {
            Actor v = a.T;
            yield return a.Dash(a.Behind(v, -0.9f), 0.12f);
            a.F("impact", v.current_position, 1.6f, 90f, 0f, false, 24f, true);
            a.Snd("launch", v.current_position);
            a.Hit(v, 0.8f);
            Cast.Hold(v, 3f);
            yield return Cast.Rise(v, 3.5f, 0.25f);
            ModClass.Instance.StartCoroutine(Cast.Hang(v, 1.6f));
            // The enemy is passed back and forth between the two in mid-air.
            Cast[] order = { b, a, b, a };
            for (int i = 0; i < order.Length && CombatCore.Alive(v); i++)
            {
                Cast c = order[i];
                Vector3 side = (Vector3)v.current_position + new Vector3(i % 2 == 0 ? 1f : -1f, 0f) * 0.9f;
                c.Blink(side, "vacuum");
                Cast.SetHeight(c.A, v.position_height);
                c.F("impact", v.current_position, 1.8f, Random.Range(0f, 360f), 0f, false, 28f, true);
                c.Spark(v.current_position, 1.4f);
                c.Snd(i % 2 == 0 ? "punch" : "kick", v.current_position);
                c.Hit(v, 0.7f);
                yield return c.W(0.18f);
            }
            if (!CombatCore.Alive(v)) yield break;
            // Spike it down together.
            b.F("impact", v.current_position, 2.4f, -90f, 0f, false, 24f, true);
            yield return Cast.Fall(v, 0.12f);
            Cast.SetHeight(a.A, 0f);
            Cast.SetHeight(b.A, 0f);
            Vector3 p = v.current_position;
            a.Burst(p, 3.5f);
            a.Crater(p, 1.6f);
            a.Impact(p, 1f);
            a.Snd("slam", p);
            a.Hit(v, 2.5f);
            b.Area(p, 1.8f, 1.2f);
        }

        private static IEnumerator TwinMeteors(Cast a, Cast b)
        {
            Vector3 t = a.TP;
            a.Snd("leap", a.P);
            ModClass.Instance.StartCoroutine(Cast.Rise(b.A, 5f, 0.35f));
            yield return Cast.Rise(a.A, 5f, 0.35f);
            a.F("fx:aura_flame", a.P, 2f, 0f, 0.5f, true, 16f, true);
            b.F("fx:aura_flame", b.P, 2f, 0f, 0.5f, true, 16f, true);
            yield return a.W(0.4f);
            // Both come down on the same spot from either side.
            Vector3 la = t + new Vector3(-0.6f, 0f), lb = t + new Vector3(0.6f, 0f);
            ModClass.Instance.StartCoroutine(Cast.Arc(b.A, lb, 0f, 0.2f, b.Hot));
            a.Shot("geo_meteor", a.P + new Vector3(-3f, 5f), t, 1.6f, 0.2f);
            b.Shot("geo_meteor", b.P + new Vector3(3f, 5f), t, 1.6f, 0.2f);
            yield return Cast.Arc(a.A, la, 0f, 0.2f, a.Hot);
            Cast.SetHeight(a.A, 0f);
            Cast.SetHeight(b.A, 0f);
            a.Burst(t, 5f);
            b.F("shock", t, 4f, 0f, 0f, false, 18f, true);
            a.Crater(t, 3f);
            a.Fissure(t + new Vector3(-3f, -1f), t + new Vector3(3f, 1f), 1.2f);
            a.Impact(t, 1f);
            a.Snd("crater_boom", t);
            a.Area(t, 3f, 2.5f);
            b.Area(t, 3f, 2.5f);
            a.PushAll(t, 3.5f, 2f);
            yield return a.W(0.4f);
        }
    }
}
