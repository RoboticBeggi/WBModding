using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// Tiers 5–7: the Four Shades, the Moon Goddess, the Trilunar Authority and the Heavenly Principles.
    /// The grandest styles in the mod: their God abilities reach across the whole battlefield.
    /// Every method here is prefixed "Pr" so it never collides with the other style files.
    /// </summary>
    public static partial class Styles
    {
        static partial void RegisterPrimordials()
        {
            PrShadeTime();
            PrShadeSpace();
            PrShadeLife();
            PrShadeDeath();
            PrMoonGoddess();
            PrTrilunar();
            PrHeavenly();
            PrDormant();
        }

        // ================================================================ shared helpers

        private static readonly Color PrGold = new Color(1f, 0.84f, 0.38f);
        private static readonly Color PrSilver = new Color(0.86f, 0.88f, 1f);
        private static readonly Color PrViolet = new Color(0.72f, 0.55f, 1f);
        private static readonly Color PrCrimson = new Color(1f, 0.42f, 0.5f);

        /// <summary>Up to <paramref name="pMax"/> enemies around a point, nearest first.</summary>
        private static List<Actor> PrTargets(Cast c, Vector3 pCenter, float pRadius, int pMax)
        {
            List<Actor> foes = c.Foes(pCenter, pRadius);
            foes.Sort((x, y) => ((Vector3)x.current_position - pCenter).sqrMagnitude
                .CompareTo(((Vector3)y.current_position - pCenter).sqrMagnitude));
            if (foes.Count > pMax) foes.RemoveRange(pMax, foes.Count - pMax);
            return foes;
        }

        /// <summary>Keeps an effect circling a unit, hurting enemies it passes.</summary>
        private static IEnumerator PrOrbit(Cast c, Fx.Instance fx, float pRadius, float pSpeed, float pSeconds,
            float pPhase, float pPower)
        {
            if (fx == null) yield break;
            float t = 0f, next_hit = 0f;
            while (t < pSeconds && c.Ok && fx.Active)
            {
                t += Time.unscaledDeltaTime * Director.FxTimeScale;
                float a = pPhase + t * pSpeed;
                Vector3 at = c.P + new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.7f) * pRadius;
                fx.Go.transform.position = at;
                if (t >= next_hit)
                {
                    foreach (Actor u in c.Foes(at, 0.9f)) c.Hit(u, pPower);
                    next_hit = t + 0.2f;
                }
                yield return null;
            }
        }

        /// <summary>A nail falling straight down onto a point; damage lands on impact.</summary>
        private static IEnumerator PrNail(Cast c, Vector3 pAt, float pSize, float pFall, float pRadius, float pPower,
            string pStatus = null)
        {
            c.Shot("nail", pAt + new Vector3(0f, 7f), pAt, pSize, pFall, false, false, 12f);
            yield return c.W(pFall);
            c.F("light_cross", pAt, pSize * 1.2f, 0f, 0f, false, 20f);
            c.Dust(pAt, pSize);
            c.Snd("nail_fall", pAt);
            c.Area(pAt, pRadius, pPower, pStatus);
            c.Crater(pAt, pRadius * 0.7f);
        }

        // ================================================================ Shade of Time · Istaroth's Hourglass
        // Nothing moves fast: it is the world that stops around her. Clocks, sand, echoes and rewinds.

        private static void PrShadeTime()
        {
            Library.Style("gi_shade_time", "Istaroth's Hourglass", 5, "time")
                .T("stopped_second", "Stopped Second", Shape.Crowd, 6f, 9f, PrStoppedSecond,
                    "A clock face freezes everyone inside; every blow they 'received' lands at once when it restarts.")
                .T("echo_strike", "Echo Strike", Shape.Gap, 10f, 6f, PrEchoStrike,
                    "Echoes from the last few seconds converge on the foe from where she once stood.")
                .T("sand_burial", "Sand Burial", Shape.Lone, 5f, 7f, PrSandBurial,
                    "Columns of hourglass sand pour onto one foe, pinning and crushing them.")
                .T("clock_hands", "Clockwork Hands", Shape.Line, 7f, 8f, PrClockHands,
                    "Two blades of light sweep around like clock hands, cutting everything they pass.")
                .T("age_of_ruin", "Age of Ruin", Shape.Execute, 6f, 10f, PrAgeOfRuin,
                    "A year passes for the foe every tick; at III they crumble to dust.")
                .T("rewind", "Rewind", Shape.Guard, 0f, 18f, PrRewind,
                    "Rolls her own wounds back and returns her to where she stood moments ago.")
                .T("hourglass_collapse", "Hourglass Collapse", Shape.Crowd, 7f, 22f, PrHourglassCollapse,
                    "A colossal hourglass drains a whole battlefield of time, then snaps it back.", 2, 3f, true)
                .God("moment_eternal", "Moment Eternal", PrMomentEternal,
                    "In a stopped world she walks to every enemy in sight and marks them; when time resumes, all the marks strike.");
        }

        private static IEnumerator PrStoppedSecond(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2.4f, 3f, 3.8f);
            float hold = c.L(1.4f, 2f, 2.8f);
            c.F("clock", p, r * 1.6f, 0f, hold, true, 10f, false, true);
            c.Snd("clock_tick", p);
            List<Actor> frozen = c.Foes(p, r);
            foreach (Actor u in frozen)
            {
                Cast.Hold(u, hold + 0.3f);
                c.Fc("clock", (Vector3)u.current_position + new Vector3(0f, 0.8f), 0.6f, c.Fade(0.8f), 0f, hold, true, 12f);
            }
            // Ticks pile up silently while they're frozen.
            int ticks = 0;
            for (float t = 0f; t < hold; t += 0.25f)
            {
                ticks++;
                foreach (Actor u in frozen)
                    if (CombatCore.Alive(u)) c.Spark((Vector3)u.current_position + Cast.Rand(0.4f), 0.5f);
                c.Snd("clock_tick", p);
                yield return c.W(0.25f);
            }
            c.Snd("release", p);
            Fx.Instance ring = c.F("shock", p, r, 0f, 0f, false, 22f, true);
            Cast.Grow(ring, 1.6f);
            foreach (Actor u in frozen) c.Hit(u, 0.18f * ticks);
            c.Impact(p, c.L(0.4f, 0.6f, 0.9f));
        }

        private static IEnumerator PrEchoStrike(Cast c)
        {
            int echoes = c.Li(2, 3, 4);
            Vector3 start = c.P;
            var points = new List<Vector3>();
            for (int i = 0; i < echoes; i++)
                points.Add(c.TP + Cast.Rot(-c.Dir, -60f + 120f * i / Mathf.Max(1, echoes - 1)) * 2.2f);
            // The echoes appear where she "was"...
            foreach (Vector3 e in points)
            {
                c.Fc("clock", e, 0.9f, c.Fade(0.7f), 0f, 0.6f, false, 14f);
                c.F("sand", e, 0.8f, 0f, 0.5f, false, 14f);
            }
            c.Snd("time_rewind");
            yield return c.W(0.3f);
            // ...and strike together.
            foreach (Vector3 e in points) c.Beam("iai", e, c.TP, 0.5f, 0.25f, true, false);
            c.Blink(c.Behind(c.T, 0.8f), "sand");
            c.Snd("slash_heavy", c.TP);
            for (int i = 0; i < echoes + 1 && c.TOk; i++)
            {
                c.Spark(c.TP + Cast.Rand(0.3f), 1.1f);
                c.Hit(c.T, 0.45f);
            }
            c.Impact(c.TP, 0.4f);
            yield return c.W(0.2f);
            if (c.Lv >= 3) Cast.Place(c.A, start);
        }

        private static IEnumerator PrSandBurial(Cast c)
        {
            Actor v = c.T;
            int columns = c.Li(3, 4, 6);
            Cast.Hold(v, 0.4f * columns + 0.5f);
            c.Snd("rumble", c.TP);
            for (int i = 0; i < columns && c.TOk; i++)
            {
                Vector3 at = c.TP + Cast.Rand(0.25f);
                c.F("sand", at + new Vector3(0f, 0.9f), c.L(1.2f, 1.5f, 1.9f), 0f, 0f, false, 18f);
                c.Dust(at, 1f);
                c.Hit(v, 0.4f, "slowness", 4f);
                c.Snd("stone_crack", at);
                yield return c.W(0.3f);
            }
            if (!c.TOk) yield break;
            c.F("debris", c.TP, 1.8f, 0f, 0f, false, 16f);
            c.Hit(v, c.L(0.8f, 1f, 1.4f));
            c.Crater(c.TP, 1f);
        }

        private static IEnumerator PrClockHands(Cast c)
        {
            Vector3 center = c.TP;
            float len = c.L(3f, 3.8f, 4.6f);
            int steps = c.Li(8, 10, 12);
            float start = c.Ang;
            c.F("clock", center, len * 1.8f, 0f, steps * 0.07f + 0.3f, true, 12f, false, true);
            c.Snd("clock_tick", center);
            for (int i = 0; i < steps; i++)
            {
                // Minute hand sweeps fast, hour hand slow.
                float minute = start - i * 30f, hour = start - i * 6f;
                Vector3 mEnd = center + Cast.Rot(Vector3.right, minute) * len;
                Vector3 hEnd = center + Cast.Rot(Vector3.right, hour) * len * 0.6f;
                c.Beam("iai", center, mEnd, 0.6f, 0.12f, true, false);
                c.Beam("iai", center, hEnd, 0.9f, 0.12f, true, false);
                c.Lane(center, mEnd, 0.5f, 0.3f);
                c.Lane(center, hEnd, 0.5f, 0.2f);
                if (i % 3 == 0) c.Snd("clock_tick", center);
                yield return c.W(0.07f);
            }
            c.F("shock", center, 2f, 0f, 0f, false, 20f, true);
            c.Snd("bell_toll", center);
            c.Area(center, 1.4f, 0.8f);
        }

        private static IEnumerator PrAgeOfRuin(Cast c)
        {
            Actor v = c.T;
            int ticks = c.Li(5, 7, 9);
            Fx.Instance clock = c.F("clock", c.TP + new Vector3(0f, 1f), 0.9f, 0f, ticks * 0.22f, true, 30f);
            Cast.Stick(clock, v);
            for (int i = 0; i < ticks && c.TOk; i++)
            {
                c.F("sand", c.TP + new Vector3(0f, 0.6f), 0.6f, 0f, 0f, false, 22f);
                c.Hit(v, 0.22f + 0.03f * i, "slowness", 3f);
                c.Snd("clock_tick", c.TP);
                yield return c.W(0.22f);
            }
            if (!c.TOk) yield break;
            if (c.Lv >= 3 && v.getHealth() < v.getMaxHealth() * 0.3f)
            {
                c.Say("CRUMBLED", c.TP + new Vector3(0f, 1.2f), 12, PrSilver);
                c.F("dust", c.TP, 1.6f, 0f, 0f, false, 12f);
                c.Hit(v, 6f);
            }
            else c.Hit(v, 0.8f);
        }

        private static IEnumerator PrRewind(Cast c)
        {
            Vector3 back = Cast.Clamp(c.P - c.Dir * c.L(2f, 3f, 4f));
            c.Snd("time_rewind");
            c.F("clock", c.P, 2f, 0f, 0.7f, false, 24f, false, true);
            // A trail of afterimages running backwards.
            for (int i = 0; i < 6; i++)
            {
                Fx.Ghost(c.A, c.Hot, 0.5f);
                Cast.Place(c.A, Vector3.Lerp(c.P, back, 0.2f));
                c.F("sand", c.P, 0.7f, 0f, 0f, false, 20f);
                yield return c.W(0.05f);
            }
            Cast.Place(c.A, back);
            c.Heal(c.A, c.L(0.25f, 0.35f, 0.5f));
            if (c.Lv >= 2) c.A.addStatusEffect("shield", 5f);
            if (c.Lv >= 3) c.A.addStatusEffect("invincible", 1.5f);
            c.F("halo", c.P, 1.4f, 0f, 0f, false, 18f);
            yield return c.W(0.2f);
        }

        private static IEnumerator PrHourglassCollapse(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(4f, 5f, 6.5f);
            ModClass.Instance.StartCoroutine(Director.Spotlight(p, 3f));
            c.F("clock", p, r * 2f, 0f, 2.6f, true, 6f, false, true);
            c.Snd("chant", p);
            // Sand pours into a ring; everyone inside slows to a crawl.
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f;
                c.F("sand", p + Cast.Rot(Vector3.right, a) * r + new Vector3(0f, 0.8f), 1.3f, 0f, 1.6f, true, 14f);
            }
            foreach (Actor u in c.Foes(p, r)) u.addStatusEffect("slowness", 6f);
            for (int i = 0; i < 6; i++)
            {
                c.Snd("clock_tick", p);
                yield return c.W(0.25f);
            }
            // Time snaps back: three hits in one instant.
            c.Snd("glass_break", p);
            Director.HitStop(0.12f);
            c.F("shatter", p, r * 1.4f, 0f, 0f, false, 18f);
            Fx.Instance ring = c.F("shock", p, r, 0f, 0f, false, 18f, true);
            Cast.Grow(ring, 2f);
            for (int k = 0; k < 3; k++) c.Area(p, r, c.L(0.9f, 1.1f, 1.3f));
            c.Impact(p, 1f);
            c.Crater(p, r * 0.5f);
        }

        private static IEnumerator PrMomentEternal(Cast c)
        {
            List<Actor> marks = PrTargets(c, c.P, 22f, c.Li(10, 12, 16));
            Vector3 home = c.P;
            c.Say("Time stands still.", home + new Vector3(0f, 2f), 13, PrSilver);
            yield return c.W(0.4f);
            // She walks through the stopped world, touching each enemy.
            foreach (Actor u in marks)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 at = (Vector3)u.current_position + new Vector3(-0.7f, 0f);
                if (Cast.CanMove(c.A))
                {
                    Fx.Ghost(c.A, c.Hot, 0.8f);
                    Cast.Place(c.A, at);
                }
                c.Fc("clock", (Vector3)u.current_position + new Vector3(0f, 0.9f), 0.8f, c.Hot, 0f, 3.5f, true, 3f);
                c.Snd("clock_tick", u.current_position);
                yield return c.W(0.12f);
            }
            if (Cast.CanMove(c.A)) Cast.Place(c.A, home);
            c.F("clock", home, 4f, 0f, 1f, false, 24f, false, true);
            c.Snd("bell_toll", home);
            yield return c.W(0.6f);
            // All the clocks reach twelve.
            c.Snd("glass_break", home);
            foreach (Actor u in marks)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("shatter", u.current_position, 1.6f, 0f, 0f, false, 20f);
                c.Spark(u.current_position, 1.4f);
                c.Hit(u, 4f);
            }
            Director.Impact(home, c.Hot, 1f);
            yield return c.W(0.4f);
        }

        // ================================================================ Shade of Space · Folded Heavens
        // Distance means nothing: portals, swaps, cubes of folded space, tears in the world.

        private static void PrShadeSpace()
        {
            Library.Style("gi_shade_space", "Folded Heavens", 5, "space")
                .T("portal_punch", "Portal Barrage", Shape.Lone, 9f, 6f, PrPortalBarrage,
                    "Punches into a portal at her side; the blows land out of portals all around the foe.")
                .T("pocket_cube", "Pocket Cube", Shape.Execute, 7f, 9f, PrPocketCube,
                    "Folds space into a cube around the foe and crushes it smaller and smaller.")
                .T("transposition", "Transposition", Shape.Gap, 12f, 7f, PrTransposition,
                    "Swaps places with the foe, leaving them stunned where she stood, and strikes from behind.")
                .T("spatial_rend", "Spatial Rend", Shape.Line, 8f, 8f, PrSpatialRend,
                    "Tears a seam of rifts down a line, then zips it shut on everything caught inside.")
                .T("gravity_fold", "Gravity Fold", Shape.Crowd, 6f, 10f, PrGravityFold,
                    "Folds the ground inward so a crowd tumbles into one point, then lets it spring back.")
                .T("distance_denied", "Distance Denied", Shape.AntiAir, 12f, 8f, PrDistanceDenied,
                    "Opens a portal behind a far or flying foe and drops them at her feet, face first.")
                .T("collapse_lattice", "Collapse Lattice", Shape.Crowd, 8f, 22f, PrCollapseLattice,
                    "Fills the field with a grid of cubes that implode one after another.", 2, 3f, true)
                .God("folded_sky", "The Folded Sky", PrFoldedSky,
                    "Portals open above every enemy on the field; they all fall through into one point before her, and space closes on them.");
        }

        private static IEnumerator PrPortalBarrage(Cast c)
        {
            Actor v = c.T;
            c.F("portal", c.Front(0.7f), 1.2f, 0f, 1.2f, true, 16f);
            c.Snd("portal_open");
            int blows = c.Li(4, 6, 8);
            Cast.Hold(v, blows * 0.12f + 0.4f);
            for (int i = 0; i < blows && c.TOk; i++)
            {
                Vector3 dir = Cast.Rot(Vector3.right, Random.Range(0f, 360f));
                Vector3 gate = c.TP + dir * 1f;
                c.F("portal", gate, 0.8f, 0f, 0.3f, false, 24f);
                c.Fc("impact", c.TP - dir * 0.2f, 1.1f, c.Hot, Cast.AngleOf(-dir), 0f, false, 28f);
                c.Snd(i % 2 == 0 ? "punch" : "kick", c.TP);
                c.Hit(v, 0.3f);
                yield return c.W(0.11f);
            }
            if (!c.TOk) yield break;
            c.F("portal", c.TP - c.Dir * 1f, 1.4f, 0f, 0.4f, false, 20f);
            c.Burst(c.TP, 1.8f);
            c.Hit(v, 0.9f);
            c.Push(v, c.Dir, c.L(1.5f, 2.2f, 3f));
        }

        private static IEnumerator PrPocketCube(Cast c)
        {
            Actor v = c.T;
            float hold = c.L(1.2f, 1.6f, 2.2f);
            Cast.Hold(v, hold + 0.4f);
            Fx.Instance cube = c.F("cube", c.TP, 2.4f, 0f, hold + 0.2f, true, 10f);
            Cast.Grow(cube, 0.35f);
            Cast.Stick(cube, v);
            c.Snd("space_warp", c.TP);
            for (float t = 0f; t < hold && c.TOk; t += 0.2f)
            {
                c.Hit(v, 0.25f);
                c.F("lock_sparks", c.TP + Cast.Rand(0.3f), 0.7f, 0f, 0f, false, 26f);
                yield return c.W(0.2f);
            }
            if (!c.TOk) yield break;
            c.Snd("glass_break", c.TP);
            c.F("shatter", c.TP, 1.6f, 0f, 0f, false, 18f);
            bool low = v.getHealth() < v.getMaxHealth() * 0.35f;
            c.Hit(v, low ? c.L(2f, 2.6f, 3.4f) : 1f);
            c.Impact(c.TP, 0.5f);
        }

        private static IEnumerator PrTransposition(Cast c)
        {
            Actor v = c.T;
            Vector3 mine = c.P, theirs = c.TP;
            c.F("portal", mine, 1.4f, 0f, 0.4f, false, 18f);
            c.F("portal", theirs, 1.4f, 0f, 0.4f, false, 18f);
            c.Snd("portal_open");
            yield return c.W(0.25f);
            Fx.Ghost(c.A, c.Hot, 0.5f);
            Cast.Place(c.A, theirs);
            Cast.Place(v, mine);
            Cast.Hold(v, c.L(0.8f, 1.2f, 1.6f));
            c.Say("?", mine + new Vector3(0f, 1f), 12, Color.white);
            c.Snd("space_warp");
            yield return c.W(0.15f);
            c.Blink(c.Behind(v, 0.7f), "portal");
            c.F("x_cut", c.TP, 1.8f, 0f, 0f, false, 24f, true);
            c.Snd("slash_heavy", c.TP);
            c.Hit(v, c.L(1.1f, 1.4f, 1.8f), "stunned", 1.5f);
        }

        private static IEnumerator PrSpatialRend(Cast c)
        {
            Vector3 from = c.Front(0.8f), to = c.P + c.Dir * c.L(6f, 7.5f, 9f);
            int tears = c.Li(4, 5, 7);
            var spots = new List<Vector3>();
            for (int i = 0; i < tears; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, i / (float)Mathf.Max(1, tears - 1));
                spots.Add(at);
                c.F("rift", at + new Vector3(0f, 0.6f), 1.4f, 0f, 0.9f, true, 14f);
                c.Snd("space_warp", at);
                yield return c.W(0.06f);
            }
            // Things near the seam are dragged in before it zips shut.
            foreach (Vector3 s in spots) c.PullAll(s, 1.6f, 0.25f);
            yield return c.W(0.45f);
            c.Beam("iai", from, to, 1.1f, 0.3f, true, false);
            c.Snd("slash_heavy", (from + to) * 0.5f);
            c.Lane(from, to, 1f, c.L(1.4f, 1.8f, 2.2f));
            c.Fissure(from, to, 0.8f);
        }

        private static IEnumerator PrGravityFold(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3f, 3.8f, 4.6f);
            c.F("vacuum", p, r * 1.4f, 0f, 0.8f, false, 14f);
            c.F("cube", p, r, 45f, 0.8f, false, 12f);
            c.Snd("space_warp", p);
            c.PullAll(p, r + 0.5f, 0.35f);
            yield return c.W(0.4f);
            List<Actor> pile = c.Foes(p, 1.6f);
            foreach (Actor u in pile) { Cast.Hold(u, 0.6f); c.Hit(u, 0.6f); }
            c.Snd("boulder_hit", p);
            yield return c.W(0.35f);
            Fx.Instance ring = c.F("shock", p, r, 0f, 0f, false, 18f, true);
            Cast.Grow(ring, 1.8f);
            c.Snd("shockwave", p);
            c.Area(p, r, 0.8f);
            foreach (Actor u in pile) c.Launch(u, Random.Range(1.2f, 2.4f), 0.2f);
            c.PushAll(p, r, c.L(2f, 2.6f, 3.2f));
            c.Impact(p, c.L(0.4f, 0.6f, 0.9f));
        }

        private static IEnumerator PrDistanceDenied(Cast c)
        {
            Actor v = c.T;
            c.F("portal", c.TP + new Vector3(0f, 0.5f), 1.3f, 0f, 0.4f, false, 18f);
            Vector3 drop = c.Front(0.9f);
            c.F("portal", drop + new Vector3(0f, 2.5f), 1.3f, 0f, 0.6f, false, 18f);
            c.Snd("portal_open", drop);
            yield return c.W(0.2f);
            if (!c.TOk) yield break;
            Cast.Place(v, drop);
            Cast.Hold(v, 1f);
            Cast.SetHeight(v, 2.5f);
            yield return Cast.Fall(v, 0.15f);
            c.Burst(drop, 1.8f);
            c.Snd("slam", drop);
            c.Hit(v, c.L(1.2f, 1.5f, 2f), "stunned", 1f);
            c.Crater(drop, 0.9f);
            c.Impact(drop, 0.5f);
        }

        private static IEnumerator PrCollapseLattice(Cast c)
        {
            Vector3 p = c.TP;
            int side = c.Li(3, 3, 4);
            float gap = 1.6f;
            var cells = new List<Vector3>();
            for (int x = 0; x < side; x++)
                for (int y = 0; y < side; y++)
                    cells.Add(p + new Vector3((x - (side - 1) / 2f) * gap, (y - (side - 1) / 2f) * gap));
            foreach (Vector3 cell in cells) c.F("cube", cell, 1.5f, 0f, 1.6f, true, 10f);
            c.Snd("chant", p);
            yield return c.W(0.6f);
            // Spiral order, so the implosions chase each other round the grid.
            cells.Sort((a, b) => Cast.AngleOf(a - p).CompareTo(Cast.AngleOf(b - p)));
            foreach (Vector3 cell in cells)
            {
                c.PullAll(cell, 1.1f, 0.12f);
                c.F("shatter", cell, 1.2f, 0f, 0f, false, 22f);
                c.Area(cell, 1f, 0.9f);
                c.Snd("glass_break", cell);
                yield return c.W(0.07f);
            }
            c.F("star_rift", p, side * gap * 1.2f, 0f, 0f, false, 16f);
            c.Area(p, side * gap * 0.7f, 1.2f);
            c.Impact(p, 1f);
        }

        private static IEnumerator PrFoldedSky(Cast c)
        {
            List<Actor> all = PrTargets(c, c.P, 24f, c.Li(16, 18, 24));
            Vector3 well = c.Front(2.5f);
            foreach (Actor u in all) c.F("portal", (Vector3)u.current_position + new Vector3(0f, 0.3f), 1.2f, 0f, 0.7f, false, 16f);
            c.F("portal", well + new Vector3(0f, 3f), 3f, 0f, 2.2f, true, 12f);
            c.Snd("portal_open", well);
            yield return c.W(0.6f);
            // Everyone falls through into one heap.
            foreach (Actor u in all)
            {
                if (!CombatCore.Alive(u)) continue;
                Cast.Place(u, well + Cast.Rand(0.8f));
                Cast.Hold(u, 2.5f);
                Cast.SetHeight(u, 3f + Random.Range(0f, 2f));
                ModClass.Instance.StartCoroutine(Cast.Fall(u, Random.Range(0.25f, 0.45f)));
            }
            yield return c.W(0.5f);
            c.Burst(well, 3f);
            c.Snd("slam", well);
            foreach (Actor u in all) c.Hit(u, 1.5f);
            // Space closes on the pile.
            Fx.Instance cube = c.F("cube", well, 5f, 0f, 1f, false, 10f);
            Cast.Grow(cube, 0.2f);
            c.Snd("space_warp", well);
            yield return c.W(0.9f);
            c.F("star_rift", well, 6f, 0f, 0f, false, 14f);
            c.F("shatter", well, 4f, 0f, 0f, false, 16f);
            c.Snd("explosion_big", well);
            foreach (Actor u in all) c.Hit(u, 3.5f);
            c.Crater(well, 3f);
            Director.Impact(well, c.Hot, 1f);
            yield return c.W(0.3f);
        }

        // ================================================================ Shade of Life · Genesis Bloom
        // Creation as a weapon: vines that bind, blooms that heal, gold that turns flesh to statue.

        private static void PrShadeLife()
        {
            Library.Style("gi_shade_life", "Genesis Bloom", 5, "life")
                .T("vine_garden", "Vine Garden", Shape.Crowd, 6f, 8f, PrVineGarden,
                    "Blooms burst open across a crowd; their vines bind and poison whoever stands there.")
                .T("renewal", "Bloom of Renewal", Shape.Support, 0f, 14f, PrRenewal,
                    "Flowers open at the feet of every hurt ally, mending them and shielding them.")
                .T("golden_alchemy", "Golden Alchemy", Shape.Lone, 6f, 10f, PrGoldenAlchemy,
                    "Transmutes the foe into a golden statue, then shatters it.")
                .T("thornspring", "Thornspring", Shape.Line, 8f, 7f, PrThornspring,
                    "A wave of roots runs down a line, flinging each foe skyward in turn.")
                .T("life_drain", "Life Thread", Shape.Execute, 7f, 9f, PrLifeThread,
                    "A thorny thread ties the foe to her; their life flows along it into her.")
                .T("cocoon", "Seed of Genesis", Shape.Guard, 0f, 18f, PrCocoon,
                    "Closes into an unbreakable flower bud, heals, and bursts open throwing foes back.")
                .T("garden_of_creation", "Garden of Creation", Shape.Crowd, 6f, 22f, PrGardenOfCreation,
                    "A tree of creation rises and the garden spreads ring by ring around it.", 2, 3f, true)
                .God("genesis", "Genesis", PrGenesis,
                    "The whole land blooms: every ally on the field is made whole and every enemy is swallowed by flowering vines.");
        }

        private static IEnumerator PrVineGarden(Cast c)
        {
            Vector3 p = c.TP;
            int blooms = c.Li(3, 4, 6);
            for (int i = 0; i < blooms; i++)
            {
                Vector3 at = p + Cast.Rand(c.L(1.8f, 2.4f, 3f));
                c.F("vine_bloom", at, 1.6f, 0f, 1.2f, false, 12f, false, true);
                c.Snd("bloom_pop", at);
                foreach (Actor u in c.Foes(at, 1.3f))
                {
                    Cast.Hold(u, c.L(1f, 1.4f, 1.8f));
                    c.Hit(u, 0.5f, "poisoned", 5f);
                }
                yield return c.W(0.12f);
            }
            c.Snd("vine_lash", p);
        }

        private static IEnumerator PrRenewal(Cast c)
        {
            List<Actor> allies = c.Allies(c.L(5f, 6f, 8f));
            c.F("flower", c.P, 2f, 0f, 0f, false, 12f);
            c.Snd("bloom_pop");
            foreach (Actor a in allies)
            {
                if (a.getHealth() >= a.getMaxHealth()) continue;
                c.F("flower", a.current_position, 1.2f, 0f, 0f, false, 12f);
                c.F("leaves", a.current_position, 1f, 0f, 0f, false, 14f);
                c.Heal(a, c.L(0.25f, 0.35f, 0.5f));
                if (c.Lv >= 2) a.addStatusEffect("shield", 5f);
                yield return c.W(0.05f);
            }
            c.Snd("holy_choir");
        }

        private static IEnumerator PrGoldenAlchemy(Cast c)
        {
            Actor v = c.T;
            float hold = c.L(1.2f, 1.6f, 2f);
            Cast.Hold(v, hold + 0.3f);
            c.Fc("ritual", c.TP, 2.4f, PrGold, 0f, hold, true, 10f, true);
            c.Snd("chant", c.TP);
            Fx.Instance shell = c.Fc("lattice", c.TP, 1.4f, PrGold, 0f, hold, true, 8f);
            Cast.Stick(shell, v);
            for (float t = 0f; t < hold && c.TOk; t += 0.3f)
            {
                c.Fc("impact", c.TP + Cast.Rand(0.4f), 0.6f, PrGold, 0f, 0f, false, 24f);
                c.Snd("crystal_chime", c.TP);
                yield return c.W(0.3f);
            }
            if (!c.TOk) yield break;
            c.Snd("glass_break", c.TP);
            c.F("shatter", c.TP, 1.8f, 0f, 0f, false, 18f);
            c.F("debris", c.TP, 1.6f, 0f, 0f, false, 16f);
            c.Hit(v, c.L(1.8f, 2.4f, 3f));
            c.Impact(c.TP, 0.6f);
        }

        private static IEnumerator PrThornspring(Cast c)
        {
            Vector3 from = c.P, to = c.P + c.Dir * c.L(6f, 7.5f, 9f);
            int steps = 8;
            var tossed = new HashSet<Actor>();
            for (int i = 1; i <= steps; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, i / (float)steps);
                c.F("root", at, 1.3f, c.Ang, 0f, false, 20f);
                c.Snd("root_creak", at);
                foreach (Actor u in c.Foes(at, 0.9f))
                {
                    if (!tossed.Add(u)) continue;
                    c.Hit(u, 0.8f);
                    c.Launch(u, c.L(1.6f, 2.2f, 2.8f), 0.3f);
                }
                yield return c.W(0.05f);
            }
            c.Fissure(from + c.Dir * 0.6f, to, 0.7f);
        }

        private static IEnumerator PrLifeThread(Cast c)
        {
            Actor v = c.T;
            int ticks = c.Li(5, 7, 9);
            Cast.Hold(v, ticks * 0.18f + 0.3f);
            c.Snd("vine_lash");
            for (int i = 0; i < ticks && c.TOk; i++)
            {
                c.Beam("thorn_whip", c.P, c.TP, 0.6f, 0.2f, false, true);
                c.Hit(v, 0.3f);
                c.Heal(c.A, 0.03f);
                c.Shot("seed", c.TP, c.P, 0.5f, 0.18f);
                if (i % 2 == 0) c.Snd("heartbeat", c.TP);
                yield return c.W(0.18f);
            }
            if (c.TOk && v.getHealth() < v.getMaxHealth() * 0.25f)
            {
                c.F("vine_bloom", c.TP, 1.8f, 0f, 0f, false, 14f);
                c.Hit(v, c.L(2f, 2.5f, 3.5f));
                c.Heal(c.A, 0.1f);
            }
        }

        private static IEnumerator PrCocoon(Cast c)
        {
            float hold = c.L(1f, 1.4f, 1.8f);
            Fx.Instance bud = c.F("flower", c.P, 1.8f, 0f, hold, true, 6f);
            Cast.Stick(bud, c.A);
            c.A.addStatusEffect("invincible", hold);
            c.Snd("leaf_rustle");
            for (float t = 0f; t < hold; t += 0.3f)
            {
                c.Heal(c.A, 0.06f);
                c.F("leaves", c.P + Cast.Rand(0.4f), 0.9f, 0f, 0f, false, 16f);
                yield return c.W(0.3f);
            }
            c.F("vine_bloom", c.P, 2.6f, 0f, 0f, false, 16f);
            c.Snd("bloom_pop");
            c.Area(c.P, 2.2f, 0.7f);
            c.PushAll(c.P, 2.4f, c.L(1.5f, 2f, 2.8f));
        }

        private static IEnumerator PrGardenOfCreation(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(4f, 5f, 6.5f);
            c.F("world_tree", p + new Vector3(0f, 1.4f), c.L(3f, 3.6f, 4.4f), 0f, 3f, false, 8f);
            c.Snd("root_creak", p);
            for (int ring = 1; ring <= 3; ring++)
            {
                float rr = r * ring / 3f;
                int n = 5 + ring * 3;
                for (int i = 0; i < n; i++)
                {
                    Vector3 at = p + Cast.Rot(Vector3.right, i * 360f / n + ring * 17f) * rr;
                    c.F(i % 2 == 0 ? "flower" : "vine_bloom", at, 1.1f, 0f, 0f, false, 14f);
                }
                c.Snd("bloom_pop", p);
                c.Area(p, rr, 0.8f, ring == 3 ? "poisoned" : null, 5f);
                foreach (Actor a in c.Allies(rr + 2f)) c.Heal(a, 0.1f);
                yield return c.W(0.35f);
            }
            for (int i = 0; i < 8; i++) c.F("leaves", p + Cast.Rand(r), 1.2f, 0f, 0f, false, 14f);
            c.Impact(p, 0.6f);
        }

        private static IEnumerator PrGenesis(Cast c)
        {
            c.F("world_tree", c.P + new Vector3(0f, 2f), 5f, 0f, 3.5f, false, 6f);
            c.Snd("holy_choir");
            yield return c.W(0.5f);
            foreach (Actor a in c.Allies(25f))
            {
                c.F("flower", a.current_position, 1.2f, 0f, 0f, false, 12f);
                c.Heal(a, 1f);
                a.addStatusEffect("shield", 8f);
            }
            c.Snd("bloom_pop");
            yield return c.W(0.4f);
            List<Actor> foes = PrTargets(c, c.P, 25f, 30);
            foreach (Actor u in foes)
            {
                c.F("vine_bloom", u.current_position, 1.8f, 0f, 2f, true, 10f, false, true);
                Cast.Hold(u, 2.5f);
            }
            c.Snd("vine_lash");
            for (int k = 0; k < 4; k++)
            {
                foreach (Actor u in foes) c.Hit(u, 1f, "poisoned", 8f);
                c.Snd("root_creak");
                yield return c.W(0.35f);
            }
            foreach (Actor u in foes)
                if (CombatCore.Alive(u)) c.F("leaves", u.current_position, 1.4f, 0f, 0f, false, 14f);
            Director.Impact(c.P, c.Hot, 0.8f);
        }

        // ================================================================ Shade of Death · Last Rites
        // Slow, solemn and final: scythes, bells, skulls and marks that come due.

        private static void PrShadeDeath()
        {
            Library.Style("gi_shade_death", "Last Rites", 5, "death")
                .T("reaping_arc", "Reaping Arc", Shape.Crowd, 3f, 7f, PrReapingArc,
                    "Spins the scythe around herself, one full turn per level.")
                .T("death_mark", "Death Mark", Shape.Execute, 9f, 10f, PrDeathMark,
                    "A skull settles over the foe. When it closes its jaw, the weak simply die.")
                .T("soul_wail", "Soul Wail", Shape.Crowd, 6f, 10f, PrSoulWail,
                    "Skulls scream outward in rings; the living freeze in terror.")
                .T("bone_field", "Bone Field", Shape.Lone, 6f, 7f, PrBoneField,
                    "Spikes of bone erupt under one foe three times, each closer to the heart.")
                .T("bell_toll", "Funeral Bell", Shape.Line, 8f, 8f, PrFuneralBell,
                    "Each toll of a great bell sends a heavier wave rolling down a line.")
                .T("reapers_step", "Reaper's Step", Shape.Gap, 11f, 6f, PrReapersStep,
                    "Fades into shadow and rises behind the foe, scything them up into the air.")
                .T("danse_macabre", "Danse Macabre", Shape.Crowd, 7f, 22f, PrDanseMacabre,
                    "A ring of skulls dances around her, then each flies to a different victim.", 2, 3f, true)
                .God("final_rest", "Final Rest", PrFinalRest,
                    "A skull hangs over every enemy on the field; the bell tolls three times, and the third toll is a scythe across the world.");
        }

        private static IEnumerator PrReapingArc(Cast c)
        {
            int turns = c.Li(1, 2, 3);
            float r = c.L(2f, 2.5f, 3f);
            c.Snd("whoosh_heavy");
            for (int k = 0; k < turns; k++)
            {
                for (int i = 0; i < 4; i++)
                {
                    float a = i * 90f + k * 45f;
                    c.F("scythe", c.P + Cast.Rot(Vector3.right, a) * r * 0.5f, r * 1.2f, a, 0f, false, 26f);
                    c.Snd("slash_heavy");
                    yield return c.W(0.06f);
                }
                c.Area(c.P, r, 0.9f);
                c.PushAll(c.P, r, 0.8f);
            }
        }

        private static IEnumerator PrDeathMark(Cast c)
        {
            Actor v = c.T;
            Fx.Instance skull = c.F("skull", c.TP + new Vector3(0f, 1f), 1f, 0f, 1.4f, false, 5f);
            Cast.Stick(skull, v);
            c.Snd("whisper", c.TP);
            yield return c.W(c.L(1.3f, 1.1f, 0.9f));
            if (!c.TOk) yield break;
            c.Snd("bell_toll", c.TP);
            float threshold = c.L(0.3f, 0.4f, 0.5f);
            if (v.getHealth() < v.getMaxHealth() * threshold)
            {
                c.Say("REAPED", c.TP + new Vector3(0f, 1.3f), 13, c.Hot);
                c.F("scythe", c.TP, 2f, -90f, 0f, false, 24f);
                c.Hit(v, 10f);
                Director.HitStop(0.1f);
            }
            else
            {
                c.F("skull", c.TP, 1.4f, 0f, 0f, false, 20f);
                c.Hit(v, 1.4f, "slowness", 4f);
            }
        }

        private static IEnumerator PrSoulWail(Cast c)
        {
            Vector3 p = c.P;
            int rings = c.Li(2, 3, 4);
            for (int i = 0; i < rings; i++)
            {
                float r = 1.5f + i * 1.3f;
                for (int k = 0; k < 6; k++)
                    c.F("skull", p + Cast.Rot(Vector3.right, k * 60f + i * 30f) * r, 0.8f, 0f, 0f, false, 22f);
                Fx.Instance ring = c.F("shock", p, r * 1.4f, 0f, 0f, false, 18f, true);
                Cast.Grow(ring, 1.4f);
                c.Snd("soul_wail", p);
                foreach (Actor u in c.Foes(p, r + 0.6f))
                {
                    c.Hit(u, 0.45f);
                    if (i == rings - 1) Cast.Hold(u, c.L(0.8f, 1.2f, 1.6f));
                }
                yield return c.W(0.22f);
            }
        }

        private static IEnumerator PrBoneField(Cast c)
        {
            Actor v = c.T;
            for (int i = 0; i < 3 && c.TOk; i++)
            {
                float off = 1.2f - i * 0.5f;
                Vector3 at = c.TP + Cast.Rand(off);
                c.Fc("fx:spikes", at, 1.4f + 0.3f * i, new Color(0.9f, 0.87f, 0.8f), 0f, 0f, false, 20f);
                c.Snd("stone_crack", at);
                yield return c.W(0.18f);
                if (!c.TOk) yield break;
                c.Area(at, 0.9f, 0.5f + 0.3f * i);
                if (i == 2) c.Launch(v, c.L(1.4f, 2f, 2.6f), 0.25f);
            }
            if (c.Lv >= 2 && c.TOk) c.F("debris", c.TP, 1.4f, 0f, 0f, false, 16f);
        }

        private static IEnumerator PrFuneralBell(Cast c)
        {
            Vector3 dir = c.Dir;
            int tolls = c.Li(2, 3, 3);
            float reach = c.L(6f, 7f, 8.5f);
            for (int i = 0; i < tolls; i++)
            {
                c.Snd("bell_toll");
                c.F("halo", c.P + new Vector3(0f, 1.3f), 1.2f + i * 0.3f, 0f, 0f, false, 18f);
                for (int s = 1; s <= 6; s++)
                {
                    Vector3 at = c.P + dir * reach * s / 6f;
                    c.Fc("fx:wave", at, 1.2f + i * 0.3f, c.Fade(0.8f), Cast.AngleOf(dir), 0f, false, 20f);
                }
                c.Lane(c.P, c.P + dir * reach, 0.9f + i * 0.2f, 0.5f + 0.25f * i);
                foreach (Actor u in CombatCore.EnemiesAlong(c.A, c.P, c.P + dir * reach, 1f)) c.Push(u, dir, 0.6f + 0.3f * i);
                Cast.Shake(0.3f);
                yield return c.W(0.45f);
            }
        }

        private static IEnumerator PrReapersStep(Cast c)
        {
            Actor v = c.T;
            Fx.Ghost(c.A, new Color(0.2f, 0.15f, 0.3f), 0.6f);
            c.F("skull", c.P, 1f, 0f, 0f, false, 22f);
            c.Snd("whisper");
            if (Cast.CanMove(c.A)) Cast.Place(c.A, c.Behind(v, 0.8f));
            yield return c.W(0.25f);
            if (!c.TOk) yield break;
            c.F("scythe", c.TP, 1.8f, 90f, 0f, false, 26f);
            c.Snd("slash_heavy", c.TP);
            c.Hit(v, c.L(1.1f, 1.4f, 1.8f));
            c.Launch(v, c.L(2f, 2.6f, 3.2f), 0.4f);
            if (c.Lv >= 3)
            {
                yield return c.W(0.35f);
                c.F("scythe", c.TP, 2f, -90f, 0f, false, 26f);
                c.Hit(v, 1f);
            }
        }

        private static IEnumerator PrDanseMacabre(Cast c)
        {
            int count = c.Li(6, 8, 10);
            var skulls = new List<Fx.Instance>();
            for (int i = 0; i < count; i++)
            {
                Fx.Instance s = c.F("skull", c.P, 0.9f, 0f, 2f, true, 10f);
                if (s == null) continue;
                skulls.Add(s);
                ModClass.Instance.StartCoroutine(PrOrbit(c, s, 1.8f, 5f, 1.2f, i * Mathf.PI * 2f / count, 0.3f));
            }
            c.Snd("soul_wail");
            yield return c.W(1.2f);
            List<Actor> victims = PrTargets(c, c.P, 8f, count);
            for (int i = 0; i < skulls.Count; i++)
            {
                Fx.Instance s = skulls[i];
                if (victims.Count == 0) break;
                Actor u = victims[i % victims.Count];
                if (!CombatCore.Alive(u)) continue;
                Fx.MoveTo(s, u.current_position, 0.2f, false);
                ModClass.Instance.StartCoroutine(PrSkullBite(c, u));
                yield return c.W(0.05f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator PrSkullBite(Cast c, Actor u)
        {
            yield return c.W(0.2f);
            if (!CombatCore.Alive(u)) yield break;
            c.F("shock", u.current_position, 1.2f, 0f, 0f, false, 22f, true);
            c.Snd("dark_pulse", u.current_position);
            c.Hit(u, 1.2f);
        }

        private static IEnumerator PrFinalRest(Cast c)
        {
            List<Actor> doomed = PrTargets(c, c.P, 22f, 30);
            foreach (Actor u in doomed)
                c.F("skull", (Vector3)u.current_position + new Vector3(0f, 1f), 0.9f, 0f, 2.6f, true, 4f);
            c.Say("Rest now.", c.P + new Vector3(0f, 2f), 13, c.Hot);
            yield return c.W(0.5f);
            for (int toll = 1; toll <= 3; toll++)
            {
                c.Snd("bell_toll");
                Fx.Instance ring = c.F("shock", c.P, 2f, 0f, 0f, false, 12f, true);
                Cast.Grow(ring, 6f);
                Cast.Shake(0.4f * toll);
                if (toll < 3)
                {
                    foreach (Actor u in doomed) c.Hit(u, 1f);
                    yield return c.W(0.6f);
                    continue;
                }
                // The last toll: one scythe across the whole field.
                Vector3 left = c.P + new Vector3(-20f, 0f), right = c.P + new Vector3(20f, 0f);
                c.Beam("iai", left, right, 2f, 0.4f, true, false);
                for (int i = -4; i <= 4; i++) c.F("scythe", c.P + new Vector3(i * 4f, 0f), 4f, 0f, 0f, false, 20f);
                c.Snd("slash_heavy");
                foreach (Actor u in doomed)
                {
                    c.Hit(u, 4f);
                    if (CombatCore.Alive(u)) c.F("skull", u.current_position, 1.2f, 0f, 0f, false, 20f);
                }
                Director.Impact(c.P, c.Hot, 1f);
            }
            yield return c.W(0.3f);
        }

        // ================================================================ Moon Goddess · Lunar Hymn
        // Graceful, dancing, tidal: crescents that come back, pillars of moonlight, the pull of the tide.

        private static void PrMoonGoddess()
        {
            Library.Style("gi_moon_goddess", "Lunar Hymn", 6, "lunar")
                .T("crescent_volley", "Returning Crescents", Shape.Line, 8f, 6f, PrReturningCrescents,
                    "Crescent blades fly out down a line and arc back to her hand, cutting twice.")
                .T("moonbeam", "Moonbeam Pillars", Shape.Crowd, 7f, 8f, PrMoonbeams,
                    "Pillars of moonlight fall one after another across a crowd.")
                .T("tidal_pull", "Tidal Pull", Shape.Crowd, 7f, 10f, PrTidalPull,
                    "The moon's pull lifts a crowd off its feet and drags it to her.")
                .T("silver_waltz", "Silver Waltz", Shape.Lone, 4f, 7f, PrSilverWaltz,
                    "Dances a full circle around one foe, a crescent cut at every step.")
                .T("shard_rain", "Shard Ascension", Shape.AntiAir, 9f, 7f, PrShardAscension,
                    "Hurls moon shards up at a flier, then brings them down like hail.")
                .T("moonlit_veil", "Moonlit Veil", Shape.Support, 0f, 16f, PrMoonlitVeil,
                    "A soft halo settles on every ally nearby, shielding them.")
                .T("eclipse", "Eclipse", Shape.Crowd, 7f, 22f, PrEclipse,
                    "Darkens the field under a black moon ringed in crescents, then lets it burst.", 2, 3f, true)
                .God("full_moon_hymn", "Hymn of the Full Moon", PrFullMoonHymn,
                    "A full moon rises; a pillar of moonlight answers every enemy on the field, and the tide of light sweeps it clean.");
        }

        private static IEnumerator PrReturningCrescents(Cast c)
        {
            int n = c.Li(2, 3, 4);
            float reach = c.L(6f, 7f, 8.5f);
            Vector3 home = c.P;
            for (int i = 0; i < n; i++)
            {
                float off = (i - (n - 1) / 2f) * 10f;
                Vector3 dir = Cast.Rot(c.Dir, off);
                Vector3 far = home + dir * reach;
                c.Shot("crescent_blade", home, far, 1.1f, 0.3f);
                c.Snd("whoosh_light");
                ModClass.Instance.StartCoroutine(PrCrescentReturn(c, home, far));
                yield return c.W(0.06f);
            }
            yield return c.W(0.75f);
        }

        private static IEnumerator PrCrescentReturn(Cast c, Vector3 home, Vector3 far)
        {
            yield return c.W(0.25f);
            c.Lane(home, far, 0.6f, 0.6f);
            // Back along a curve: through a side point.
            Vector3 side = Vector3.Lerp(home, far, 0.6f) + Cast.Rot((far - home).normalized, 90f) * 1.5f;
            c.Shot("crescent_blade", far, side, 1.1f, 0.15f);
            yield return c.W(0.15f);
            c.Shot("crescent_blade", side, c.P, 1.1f, 0.15f);
            c.Lane(far, side, 0.6f, 0.4f);
            c.Lane(side, c.P, 0.6f, 0.4f);
            c.Snd("moon_hum");
        }

        private static IEnumerator PrMoonbeams(Cast c)
        {
            int n = c.Li(3, 4, 6);
            Vector3 p = c.TP;
            for (int i = 0; i < n; i++)
            {
                Vector3 at = i == 0 ? p : p + Cast.Rand(2.4f);
                c.F("halo", at, 1.4f, 0f, 0.3f, false, 20f, false, true);
                yield return c.W(0.12f);
                c.F("moon_pillar", at + new Vector3(0f, 2f), c.L(1.8f, 2.1f, 2.5f), 0f, 0f, false, 18f);
                c.Snd("moon_hum", at);
                c.Area(at, 1.2f, 0.9f);
                c.Dust(at, 1f);
                yield return c.W(0.1f);
            }
            c.Impact(p, 0.4f);
        }

        private static IEnumerator PrTidalPull(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3f, 3.6f, 4.4f);
            c.F("moon_shard", c.P + new Vector3(0f, 2f), 1.4f, 90f, 1f, true, 10f);
            c.Fc("fx:wave", p, r * 1.6f, c.Fade(0.7f), 0f, 0.8f, false, 12f);
            c.Snd("wave_crash", p);
            List<Actor> caught = c.Foes(p, r);
            foreach (Actor u in caught) c.Launch(u, 1.2f, 0.6f);
            yield return c.W(0.3f);
            foreach (Actor u in caught)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 d = c.P - (Vector3)u.current_position;
                c.Push(u, d, Mathf.Max(0f, d.magnitude - 1.2f), 0.45f);
            }
            yield return c.W(0.5f);
            c.F("shock", c.Front(1f), 2f, 0f, 0f, false, 20f, true);
            c.Area(c.Front(1f), 1.8f, c.L(1f, 1.3f, 1.7f));
            c.Snd("splash_big");
        }

        private static IEnumerator PrSilverWaltz(Cast c)
        {
            Actor v = c.T;
            int steps = c.Li(4, 6, 8);
            Cast.Hold(v, steps * 0.12f + 0.4f);
            for (int i = 0; i < steps && c.TOk; i++)
            {
                float a = i * 360f / steps;
                Vector3 spot = c.TP + Cast.Rot(Vector3.right, a) * 1.1f;
                if (Cast.CanMove(c.A))
                {
                    Fx.Ghost(c.A, c.Hot, 0.35f);
                    Cast.Place(c.A, spot);
                    CombatCore.Face(c.A, c.TP);
                }
                c.F("crescent_blade", c.TP, 1.2f, a + 180f, 0.15f, false, 20f);
                c.Snd("slash_light", c.TP);
                c.Hit(v, 0.35f);
                yield return c.W(0.11f);
            }
            if (c.TOk)
            {
                c.F("halo", c.TP, 1.6f, 0f, 0f, false, 20f);
                c.Hit(v, 0.8f);
            }
        }

        private static IEnumerator PrShardAscension(Cast c)
        {
            Actor v = c.T;
            int n = c.Li(4, 6, 8);
            for (int i = 0; i < n; i++)
            {
                c.Shot("moon_shard", c.P, c.TP + new Vector3(Random.Range(-0.5f, 0.5f), 1.5f), 0.8f, 0.2f);
                c.Snd("star_chime");
                yield return c.W(0.05f);
            }
            if (c.TOk)
            {
                c.Hit(v, 0.6f);
                if (v.position_height < 0.5f) c.Launch(v, 2f, 0.3f);
            }
            yield return c.W(0.35f);
            // Down they come.
            Vector3 p = c.TP;
            for (int i = 0; i < n; i++)
            {
                Vector3 at = p + Cast.Rand(1.4f);
                c.Shot("moon_shard", at + new Vector3(0.6f, 4f), at, 0.8f, 0.18f);
            }
            yield return c.W(0.18f);
            if (c.TOk && Cast.CanMove(v)) Cast.SetHeight(v, 0f);
            c.Area(p, 1.6f, 1f);
            c.Dust(p, 1.4f);
            c.Snd("glass_break", p);
        }

        private static IEnumerator PrMoonlitVeil(Cast c)
        {
            c.Snd("moon_hum");
            foreach (Actor a in c.Allies(c.L(5f, 6f, 8f)))
            {
                Fx.Instance h = c.F("halo", a.current_position, 1.1f, 0f, 2f, true, 12f);
                Cast.Stick(h, a);
                a.addStatusEffect("shield", c.L(4f, 6f, 8f));
                if (c.Lv >= 2) c.Heal(a, 0.15f);
                yield return c.W(0.03f);
            }
            c.A.addStatusEffect("dodge", 4f);
        }

        private static IEnumerator PrEclipse(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3.5f, 4.5f, 5.5f);
            ModClass.Instance.StartCoroutine(Director.Spotlight(p, 2.6f));
            Fx.Instance moon = c.Fc("fx:orb", p + new Vector3(0f, 2.5f), r * 0.7f, new Color(0.12f, 0.1f, 0.18f, 0.95f),
                0f, 2.2f, true, 8f);
            c.Snd("moon_hum", p);
            for (int i = 0; i < 8; i++)
                c.F("crescent_blade", p + Cast.Rot(Vector3.right, i * 45f) * r, 1.2f, i * 45f + 90f, 1.6f, true, 10f);
            foreach (Actor u in c.Foes(p, r)) u.addStatusEffect("slowness", 4f);
            yield return c.W(1.2f);
            // The crescents converge.
            for (int i = 0; i < 8; i++)
                c.Shot("crescent_blade", p + Cast.Rot(Vector3.right, i * 45f) * r, p, 1.2f, 0.18f);
            c.Snd("slash_heavy", p);
            yield return c.W(0.2f);
            if (moon != null) Fx.MoveTo(moon, p, 0.2f, false);
            yield return c.W(0.2f);
            c.Burst(p, r * 1.4f);
            c.Fc("halo", p, r * 1.2f, PrSilver, 0f, 0f, false, 16f);
            c.Snd("explosion_big", p);
            c.Area(p, r, c.L(2.2f, 2.8f, 3.4f));
            c.Crater(p, r * 0.5f);
            c.Impact(p, 1f);
        }

        private static IEnumerator PrFullMoonHymn(Cast c)
        {
            Vector3 sky = c.P + new Vector3(0f, 4f);
            Fx.Instance moon = c.Fc("fx:orb", c.P, 2f, PrSilver, 0f, 4f, true, 8f);
            Fx.MoveTo(moon, sky, 0.8f, false);
            c.Snd("holy_choir");
            yield return c.W(0.9f);
            List<Actor> all = PrTargets(c, c.P, 22f, 30);
            foreach (Actor u in all)
            {
                Vector3 at = u.current_position;
                c.F("moon_pillar", at + new Vector3(0f, 2f), 2.2f, 0f, 0f, false, 16f);
                c.Snd("moon_hum", at);
                c.Hit(u, 2f);
                yield return c.W(0.05f);
            }
            yield return c.W(0.3f);
            // The tide of light: a ring sweeping out across the field.
            for (int i = 1; i <= 5; i++)
            {
                Fx.Instance ring = c.Fc("halo", c.P, i * 6f, c.Fade(0.6f), 0f, 0.5f, false, 14f);
                Cast.Grow(ring, 1.2f);
                yield return c.W(0.1f);
            }
            c.Snd("wave_crash");
            foreach (Actor u in all) c.Hit(u, 2.5f);
            foreach (Actor a in c.Allies(22f)) c.Heal(a, 0.3f);
            Director.Impact(c.P, c.Hot, 1f);
        }

        // ================================================================ Trilunar Authority
        // Three moons, three colours, always in threes: orbits, phases, converging crescents, moonfall.

        private static readonly Color[] PrMoons =
        {
            new Color(0.9f, 0.9f, 1f), new Color(1f, 0.7f, 0.85f), new Color(0.6f, 0.75f, 1f)
        };

        private static void PrTrilunar()
        {
            Library.Style("gi_trilunar", "Trilunar Authority", 6, "lunar")
                .T("triple_orbit", "Triple Orbit", Shape.Crowd, 3f, 9f, PrTripleOrbit,
                    "Three moons whirl around her, battering everything that comes close.")
                .T("phase_shift", "Phase Shift", Shape.Gap, 12f, 6f, PrPhaseShift,
                    "Crosses the distance in three phases (new, half, full), striking once at each.")
                .T("waxing_lance", "Waxing Lance", Shape.Line, 9f, 8f, PrWaxingLance,
                    "A lance of moonlight that grows thicker with each of three pulses.")
                .T("lunar_tides", "Three Tides", Shape.Crowd, 6f, 10f, PrThreeTides,
                    "Three tides of different moons roll in turn: out, in, then out again.")
                .T("crescent_trinity", "Crescent Trinity", Shape.Lone, 7f, 7f, PrCrescentTrinity,
                    "Three crescents close on one foe from three directions at once.")
                .T("lunar_sanctuary", "Lunar Sanctuary", Shape.Guard, 0f, 18f, PrLunarSanctuary,
                    "The three moons lock into a triangle of light around her: nothing gets through.")
                .T("moonfall", "Moonfall", Shape.Crowd, 9f, 24f, PrMoonfall,
                    "Pulls a moon out of the sky onto the battlefield.", 2, 3.5f, true)
                .God("convergence", "Trilunar Convergence", PrConvergence,
                    "Three moons fall at once on three distant hosts, then pour their light back into her in one blinding line.");
        }

        private static IEnumerator PrTripleOrbit(Cast c)
        {
            float dur = c.L(2f, 2.6f, 3.2f);
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance m = c.Fc("fx:orb", c.P, 0.9f, PrMoons[i], 0f, dur, true, 10f);
                ModClass.Instance.StartCoroutine(PrOrbit(c, m, c.L(1.6f, 2f, 2.4f), 6f, dur, i * 2.094f, 0.35f));
            }
            c.Snd("moon_hum");
            yield return c.W(dur);
            c.F("shock", c.P, 2.4f, 0f, 0f, false, 20f, true);
            c.PushAll(c.P, 2.6f, 1.2f);
        }

        private static IEnumerator PrPhaseShift(Cast c)
        {
            Actor v = c.T;
            Vector3 start = c.P;
            for (int i = 0; i < 3 && c.TOk; i++)
            {
                Vector3 spot = i < 2 ? Vector3.Lerp(start, c.TP, (i + 1) / 3f) + Cast.Rot(c.Dir, 90f) * (i == 0 ? 1f : -1f)
                    : c.Behind(v, 0.8f);
                c.Fc("fx:orb", c.P, 0.8f, PrMoons[i], 0f, 0.3f, false, 18f);
                c.Blink(spot, "vacuum");
                c.Beam("iai", c.P, c.TP, 0.4f, 0.15f, true, false);
                c.Fc("crescent_blade", c.TP, 1f, PrMoons[i], Cast.AngleOf(c.TP - c.P), 0f, false, 22f);
                c.Snd("slash_light", c.TP);
                c.Hit(v, 0.5f + 0.2f * i);
                yield return c.W(0.12f);
            }
        }

        private static IEnumerator PrWaxingLance(Cast c)
        {
            Vector3 from = c.Front(0.6f), to = c.P + c.Dir * c.L(7f, 8f, 9.5f);
            c.F("halo", c.P, 1.2f, 0f, 0.4f, false, 18f);
            c.Snd("charge");
            yield return c.W(0.25f);
            for (int i = 0; i < 3; i++)
            {
                Fx.Line("beam", from, to, 0.5f + 0.5f * i, PrMoons[i], 0.2f, true);
                c.Snd("beam_fire", (from + to) * 0.5f);
                c.Lane(from, to, 0.5f + 0.3f * i, 0.5f + 0.3f * i);
                yield return c.W(0.2f);
            }
            c.F("moon_shard", to, 1.4f, c.Ang, 0f, false, 18f);
            c.Fissure(from, to, 0.6f);
        }

        private static IEnumerator PrThreeTides(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3f, 3.6f, 4.4f);
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance ring = c.Fc("fx:wave", p, r * (i == 1 ? 1.6f : 0.8f), PrMoons[i], 0f, 0.4f, false, 18f);
                Cast.Grow(ring, i == 1 ? 0.4f : 2f);
                c.Snd("wave_crash", p);
                c.Area(p, r, 0.6f);
                if (i == 1) c.PullAll(p, r + 0.5f, 0.3f);
                else c.PushAll(p, r, 1.5f);
                yield return c.W(0.35f);
            }
        }

        private static IEnumerator PrCrescentTrinity(Cast c)
        {
            Actor v = c.T;
            Cast.Hold(v, 0.7f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 from = c.TP + Cast.Rot(Vector3.right, 90f + i * 120f) * 3f;
                c.Fc("crescent_blade", from, 1.3f, PrMoons[i], 0f, 0.3f, false, 18f);
            }
            c.Snd("moon_hum", c.TP);
            yield return c.W(0.3f);
            for (int i = 0; i < 3; i++)
            {
                Vector3 from = c.TP + Cast.Rot(Vector3.right, 90f + i * 120f) * 3f;
                Fx.Instance s = Fx.Play("arts/crescent_blade", from, 1.3f, PrMoons[i], 0f, 0.22f, true);
                Fx.MoveTo(s, c.TP, 0.2f);
            }
            yield return c.W(0.2f);
            if (!c.TOk) yield break;
            c.F("x_cut", c.TP, 1.8f, 0f, 0f, false, 22f, true);
            c.Snd("slash_heavy", c.TP);
            c.Hit(v, c.L(1.6f, 2f, 2.6f));
            c.Impact(c.TP, 0.5f);
        }

        private static IEnumerator PrLunarSanctuary(Cast c)
        {
            float dur = c.L(2f, 3f, 4f);
            var corners = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                corners[i] = c.P + Cast.Rot(Vector3.up, i * 120f) * 1.4f;
                c.Fc("fx:orb", corners[i], 0.8f, PrMoons[i], 0f, dur, true, 10f);
            }
            for (int i = 0; i < 3; i++) Fx.Line("beam", corners[i], corners[(i + 1) % 3], 0.3f, c.Fade(0.7f), dur, true);
            c.A.addStatusEffect("invincible", dur);
            c.Heal(c.A, c.L(0.1f, 0.2f, 0.3f));
            c.Snd("moon_hum");
            yield return c.W(0.3f);
            c.PushAll(c.P, 2f, 1.5f);
        }

        private static IEnumerator PrMoonfall(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(4f, 5f, 6.5f);
            ModClass.Instance.StartCoroutine(Director.Spotlight(p, 2.6f));
            Fx.Instance moon = c.Fc("fx:orb", p + new Vector3(-3f, 10f), r * 0.9f, PrMoons[Random.Range(0, 3)], 0f, 1.7f,
                true, 8f);
            Fx.MoveTo(moon, p, 1.4f, false);
            c.Fc("fx:orb", p, r * 0.8f, new Color(0f, 0f, 0f, 0.35f), 0f, 1.4f, false, 8f, true);
            c.Snd("rumble", p);
            for (int i = 0; i < 4; i++)
            {
                Cast.Shake(0.15f + i * 0.1f);
                yield return c.W(0.35f);
            }
            c.Burst(p, r * 1.6f);
            c.F("debris", p, r, 0f, 0f, false, 14f);
            c.Fc("halo", p, r * 1.6f, PrSilver, 0f, 0f, false, 14f);
            c.Snd("crater_boom", p);
            c.Area(p, r, c.L(3f, 3.8f, 4.6f));
            c.PushAll(p, r + 1f, 2.5f);
            c.Crater(p, r * 0.8f);
            for (int i = 0; i < 5; i++) c.Fissure(p, p + Cast.Rot(Vector3.right, i * 72f + 15f) * r * 1.4f, 1f);
            Cast.Scorch(p, r * 0.6f, 4);
            c.Impact(p, 1f);
        }

        private static IEnumerator PrConvergence(Cast c)
        {
            // Pick three hosts: the nearest enemy, then the two farthest from what's already picked.
            List<Actor> all = PrTargets(c, c.P, 26f, 40);
            if (all.Count == 0) yield break;
            var centers = new List<Vector3> { all[0].current_position };
            while (centers.Count < 3)
            {
                Vector3 best = centers[0];
                float bd = -1f;
                foreach (Actor u in all)
                {
                    float d = float.MaxValue;
                    foreach (Vector3 ce in centers) d = Mathf.Min(d, ((Vector3)u.current_position - ce).sqrMagnitude);
                    if (d > bd) { bd = d; best = u.current_position; }
                }
                centers.Add(best);
            }
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance m = c.Fc("fx:orb", centers[i] + new Vector3(0f, 9f), 4f, PrMoons[i], 0f, 1.3f, true, 8f);
                Fx.MoveTo(m, centers[i], 1.1f, false);
            }
            c.Snd("rumble");
            yield return c.W(1.1f);
            for (int i = 0; i < 3; i++)
            {
                c.Burst(centers[i], 6f);
                c.Fc("halo", centers[i], 5f, PrMoons[i], 0f, 0f, false, 14f);
                c.Crater(centers[i], 3f);
                c.Area(centers[i], 5f, 4f);
            }
            c.Snd("crater_boom");
            Cast.Shake(1f);
            yield return c.W(0.5f);
            // Their light pours back into her.
            for (int i = 0; i < 3; i++) Fx.Line("beam", centers[i], c.P, 1.6f, PrMoons[i], 0.6f, true);
            c.Snd("beam_fire");
            foreach (Vector3 ce in centers) c.Lane(ce, c.P, 1.2f, 2f);
            yield return c.W(0.5f);
            c.Burst(c.P, 5f);
            c.Area(c.P, 4f, 2f);
            Director.Impact(c.P, c.Hot, 1f);
        }

        // ================================================================ Heavenly Principles
        // Law from above: nails driven from the sky, halos that close like vices, crosses of light that erase.

        private static void PrHeavenly()
        {
            Library.Style("gi_heavenly_principles", "Heavenly Principles", 7, "divine")
                .T("nail_procession", "Nail Procession", Shape.Line, 10f, 7f, PrNailProcession,
                    "Celestial nails fall one after another down a line, marching towards the foe.")
                .T("halo_judgment", "Halo Judgment", Shape.Lone, 9f, 8f, PrHaloJudgment,
                    "A halo descends on one foe and tightens until it bursts into a cross of light.")
                .T("erasure", "Erasure", Shape.Execute, 12f, 10f, PrErasure,
                    "Outlines the foe in light, and removes them from the world.")
                .T("sky_law", "Sky Law", Shape.Crowd, 8f, 10f, PrSkyLaw,
                    "Decrees that nothing may move; everyone under the seal stands frozen as a nail finds each one.")
                .T("radiant_wings", "Radiant Wings", Shape.AntiAir, 10f, 7f, PrRadiantWings,
                    "Rays fan upward from her back and strike fliers out of the sky.")
                .T("mandate", "Heaven's Mandate", Shape.Guard, 0f, 18f, PrMandate,
                    "A halo crowns her: untouchable, restored, and every blow at her answered by light.")
                .T("false_sky", "Shattered Firmament", Shape.Crowd, 10f, 24f, PrShatteredFirmament,
                    "The sky cracks in crosses of light, and dozens of nails pour through the cracks.", 2, 4f, true)
                .God("false_sky_descends", "The False Sky Descends", PrFalseSkyDescends,
                    "Every enemy on the field is marked by a cross, and a colossal nail falls for each; the sky whites out.");
        }

        private static IEnumerator PrNailProcession(Cast c)
        {
            int n = c.Li(4, 6, 8);
            Vector3 from = c.Front(1f), to = c.P + c.Dir * c.L(7f, 8.5f, 10f);
            for (int i = 0; i < n; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, i / (float)Mathf.Max(1, n - 1));
                ModClass.Instance.StartCoroutine(PrNail(c, at, 1.2f, 0.18f, 1f, 0.8f));
                yield return c.W(0.08f);
            }
            yield return c.W(0.25f);
        }

        private static IEnumerator PrHaloJudgment(Cast c)
        {
            Actor v = c.T;
            Cast.Hold(v, 1.2f);
            Fx.Instance halo = c.F("halo", c.TP + new Vector3(0f, 2f), 2.6f, 0f, 1f, true, 12f);
            Fx.MoveTo(halo, c.TP, 0.5f, false);
            Cast.Grow(halo, 0.35f);
            c.Snd("holy_choir", c.TP);
            yield return c.W(0.6f);
            for (int i = 0; i < c.Li(2, 3, 4) && c.TOk; i++)
            {
                c.Hit(v, 0.4f);
                c.Spark(c.TP, 1f);
                yield return c.W(0.1f);
            }
            if (!c.TOk) yield break;
            c.F("light_cross", c.TP, 2.6f, 0f, 0f, false, 18f);
            c.Snd("decree_gong", c.TP);
            c.Hit(v, c.L(1.4f, 1.8f, 2.4f));
            c.Area(c.TP, 1.4f, 0.6f);
            c.Impact(c.TP, 0.6f);
        }

        private static IEnumerator PrErasure(Cast c)
        {
            Actor v = c.T;
            Cast.Hold(v, 1.2f);
            Fx.Instance cross = c.F("light_cross", c.TP, 1.6f, 0f, 1f, true, 10f);
            Cast.Stick(cross, v);
            c.Snd("holy_choir", c.TP);
            yield return c.W(c.L(1f, 0.8f, 0.6f));
            if (!c.TOk) yield break;
            Director.HitStop(0.12f);
            c.Fc("fx:radiance", c.TP, 3f, Color.white, 0f, 0f, false, 20f);
            c.Snd("glass_break", c.TP);
            bool weak = v.getHealth() < v.getMaxHealth() * c.L(0.4f, 0.5f, 0.6f);
            if (weak) c.Say("ERASED", c.TP + new Vector3(0f, 1.3f), 13, Color.white);
            c.Hit(v, weak ? 12f : 1.6f);
        }

        private static IEnumerator PrSkyLaw(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3.5f, 4.5f, 5.5f);
            c.F("decree", p, r * 2f, 0f, 1.6f, true, 10f, false, true);
            c.Snd("decree_gong", p);
            List<Actor> bound = c.Foes(p, r);
            foreach (Actor u in bound) Cast.Hold(u, 1.8f);
            c.Say("BE STILL", p + new Vector3(0f, 1.8f), 13, c.Hot);
            yield return c.W(0.5f);
            foreach (Actor u in bound)
            {
                if (!CombatCore.Alive(u)) continue;
                ModClass.Instance.StartCoroutine(PrNail(c, u.current_position, 1f, 0.16f, 0.6f, c.L(1f, 1.3f, 1.6f)));
                yield return c.W(0.06f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator PrRadiantWings(Cast c)
        {
            Actor v = c.T;
            c.Fc("fx:radiance", c.P, 3f, c.Fade(0.9f), 0f, 0.5f, false, 18f);
            c.Snd("holy_choir");
            int rays = c.Li(3, 5, 7);
            for (int i = 0; i < rays; i++)
            {
                Vector3 end = c.TP + new Vector3((i - rays / 2) * 0.6f, 1.5f);
                Fx.Line("beam", c.P, end, 0.4f, c.Hot, 0.2f, true);
                c.Snd("beam_fire");
                yield return c.W(0.05f);
            }
            if (!c.TOk) yield break;
            c.Hit(v, c.L(1.4f, 1.8f, 2.2f));
            if (Cast.CanMove(v) && v.position_height > 0.2f) yield return Cast.Fall(v, 0.15f);
            c.Burst(c.TP, 1.6f);
            c.Hit(v, 0.5f, "stunned", 1f);
        }

        private static IEnumerator PrMandate(Cast c)
        {
            float dur = c.L(2f, 3f, 4f);
            Fx.Instance halo = c.F("halo", c.P + new Vector3(0f, 0.9f), 1.2f, 0f, dur, true, 12f);
            Cast.Stick(halo, c.A);
            c.A.addStatusEffect("invincible", dur);
            c.Heal(c.A, c.L(0.2f, 0.3f, 0.45f));
            c.Snd("holy_choir");
            // Light answers whoever is close.
            for (float t = 0f; t < dur; t += 0.5f)
            {
                foreach (Actor u in c.Foes(c.P, 2.5f))
                {
                    Fx.Line("beam", c.P, u.current_position, 0.3f, c.Hot, 0.15f, true);
                    c.Hit(u, 0.4f);
                }
                yield return c.W(0.5f);
            }
        }

        private static IEnumerator PrShatteredFirmament(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(5f, 6f, 7.5f);
            ModClass.Instance.StartCoroutine(Director.Spotlight(p, 3f));
            for (int i = 0; i < 5; i++)
                c.F("light_cross", p + Cast.Rand(r) + new Vector3(0f, 3f), 2.4f, 0f, 1.5f, true, 10f);
            c.Snd("glass_break", p);
            yield return c.W(0.4f);
            int nails = c.Li(14, 20, 28);
            for (int i = 0; i < nails; i++)
            {
                ModClass.Instance.StartCoroutine(PrNail(c, p + Cast.Rand(r), Random.Range(0.9f, 1.5f), 0.2f, 1f, 0.6f));
                yield return c.W(0.04f);
            }
            yield return c.W(0.3f);
            c.F("halo", p, r * 1.6f, 0f, 0f, false, 14f);
            c.Area(p, r, 1.5f);
            c.Impact(p, 1f);
        }

        private static IEnumerator PrFalseSkyDescends(Cast c)
        {
            List<Actor> all = PrTargets(c, c.P, 25f, 30);
            c.Say("The sky is false. Kneel.", c.P + new Vector3(0f, 2.2f), 13, c.Hot);
            foreach (Actor u in all) c.F("light_cross", u.current_position, 1.4f, 0f, 2.4f, true, 6f);
            c.Snd("holy_choir");
            yield return c.W(0.8f);
            foreach (Actor u in all)
            {
                if (!CombatCore.Alive(u)) continue;
                ModClass.Instance.StartCoroutine(PrNail(c, u.current_position, 2.4f, 0.3f, 1.4f, 5f, "stunned"));
                yield return c.W(0.06f);
            }
            yield return c.W(0.5f);
            // Whiteout.
            c.Fc("fx:radiance", c.P, 14f, Color.white, 0f, 0f, false, 14f);
            c.Snd("explosion_big");
            foreach (Actor u in all) c.Hit(u, 3f);
            Director.Impact(c.P, Color.white, 1f);
            yield return c.W(0.4f);
        }

        // ================================================================ Dormant Principles
        // Celestia asleep: everything is slow, heavy and dreadful. Long wind-ups, enormous blows.

        private static void PrDormant()
        {
            Library.Style("gi_dormant_principles", "Dormant Principles", 5, "divine")
                .T("remnant_nail", "Remnant Nail", Shape.Lone, 10f, 9f, PrRemnantNail,
                    "A single, ancient nail takes an age to fall and leaves nothing standing where it lands.")
                .T("sleeping_halo", "Sleeping Halo", Shape.Crowd, 6f, 9f, PrSleepingHalo,
                    "A faded halo pulses slowly; each pulse makes the living heavier and slower.")
                .T("dream_shards", "Drifting Dreams", Shape.Line, 9f, 8f, PrDriftingDreams,
                    "Shards of a dream drift lazily down a line and burst one by one.")
                .T("curse_echo", "Curse Echo", Shape.Execute, 9f, 10f, PrCurseEcho,
                    "Seals the foe with the Descender's curse; it rings again and again until it kills.")
                .T("weight_of_celestia", "Weight of Celestia", Shape.Crowd, 6f, 11f, PrWeightOfCelestia,
                    "The floating isle's weight presses down on a crowd, grinding them into the ground.")
                .T("somnolent_ward", "Somnolent Ward", Shape.Guard, 0f, 20f, PrSomnolentWard,
                    "Wraps itself in slumbering light; wounds close while it sleeps.")
                .T("remnants", "Celestial Remnants", Shape.Crowd, 9f, 26f, PrCelestialRemnants,
                    "Dim, broken nails rain down slowly over a wide field.", 2, 3f, true)
                .God("dream_of_celestia", "Dream of Celestia", PrDreamOfCelestia,
                    "The sky dims and the sleeping heavens dream of war: one immense nail after another, slow and grave, across the field.");
        }

        private static readonly Color PrDim = new Color(0.75f, 0.7f, 0.55f, 0.85f);

        private static IEnumerator PrRemnantNail(Cast c)
        {
            Vector3 p = c.TP;
            c.Fc("halo", p, 1.6f, PrDim, 0f, 1.6f, true, 6f, true);
            c.Snd("bell_toll", p);
            Fx.Instance nail = Fx.Play("arts/nail", p + new Vector3(0f, 8f), 2.6f, PrDim, 0f, 1.5f, true, 6f);
            Fx.MoveTo(nail, p, 1.4f, false);
            yield return c.W(1.4f);
            c.Burst(p, 3f);
            c.F("light_cross", p, 2.6f, 0f, 0f, false, 14f);
            c.Snd("crater_boom", p);
            c.Area(p, 1.4f, c.L(2.4f, 3f, 3.8f));
            c.Crater(p, 1.6f);
            c.Impact(p, 0.8f);
        }

        private static IEnumerator PrSleepingHalo(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3f, 3.6f, 4.4f);
            for (int i = 0; i < c.Li(3, 4, 5); i++)
            {
                Fx.Instance h = c.Fc("halo", p, r, PrDim, 0f, 0.6f, false, 10f);
                Cast.Grow(h, 1.3f);
                c.Snd("moon_hum", p);
                foreach (Actor u in c.Foes(p, r)) c.Hit(u, 0.35f, "slowness", 5f);
                yield return c.W(0.55f);
            }
        }

        private static IEnumerator PrDriftingDreams(Cast c)
        {
            int n = c.Li(4, 5, 7);
            Vector3 from = c.Front(0.8f), to = c.P + c.Dir * c.L(7f, 8f, 9f);
            for (int i = 0; i < n; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, (i + 1f) / n) + Cast.Rand(0.4f);
                Fx.Instance s = Fx.Play("arts/star_sword", c.P, 0.9f, PrDim, 0f, 1.1f, true, 8f);
                Fx.MoveTo(s, at, 0.9f);
                ModClass.Instance.StartCoroutine(PrDreamBurst(c, at, 0.9f + i * 0.08f));
                yield return c.W(0.08f);
            }
            c.Snd("star_chime");
            yield return c.W(1.2f);
        }

        private static IEnumerator PrDreamBurst(Cast c, Vector3 at, float delay)
        {
            yield return c.W(delay);
            c.Fc("shatter", at, 1.2f, PrDim, 0f, 0f, false, 18f);
            c.Snd("glass_break", at);
            c.Area(at, 1.1f, 0.8f);
        }

        private static IEnumerator PrCurseEcho(Cast c)
        {
            Actor v = c.T;
            Fx.Instance seal = c.F("decree", c.TP, 1.4f, 0f, 2.5f, true, 6f, false, true);
            Cast.Stick(seal, v);
            c.Snd("decree_gong", c.TP);
            int echoes = c.Li(3, 4, 5);
            for (int i = 0; i < echoes && c.TOk; i++)
            {
                yield return c.W(0.45f);
                if (!c.TOk) yield break;
                c.Snd("bell_toll", c.TP);
                c.Fc("shock", c.TP, 1.2f + 0.2f * i, PrDim, 0f, 0f, false, 18f);
                float missing = 1f - v.getHealth() / (float)Mathf.Max(1, v.getMaxHealth());
                c.Hit(v, 0.4f + missing * 1.2f);
            }
        }

        private static IEnumerator PrWeightOfCelestia(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3f, 3.6f, 4.4f);
            c.Fc("fx:orb", p + new Vector3(0f, 4f), r * 1.2f, new Color(0.5f, 0.48f, 0.4f, 0.6f), 0f, 1.6f, true, 6f);
            c.Snd("rumble", p);
            List<Actor> pinned = c.Foes(p, r);
            foreach (Actor u in pinned) Cast.Hold(u, 1.8f);
            for (int i = 0; i < 4; i++)
            {
                foreach (Actor u in pinned)
                {
                    if (!CombatCore.Alive(u)) continue;
                    c.Dust(u.current_position, 0.8f);
                    c.Hit(u, 0.4f);
                }
                Cast.Shake(0.25f + 0.1f * i);
                yield return c.W(0.35f);
            }
            c.Crater(p, r * 0.6f);
            c.Snd("crater_boom", p);
            c.Area(p, r, 0.8f);
        }

        private static IEnumerator PrSomnolentWard(Cast c)
        {
            float dur = c.L(2.5f, 3.5f, 4.5f);
            Fx.Instance glow = c.Fc("halo", c.P, 1.8f, PrDim, 0f, dur, true, 6f);
            Cast.Stick(glow, c.A);
            c.A.addStatusEffect("shield", dur + 2f);
            c.Snd("moon_hum");
            for (float t = 0f; t < dur; t += 0.5f)
            {
                c.Heal(c.A, 0.05f);
                yield return c.W(0.5f);
            }
        }

        private static IEnumerator PrCelestialRemnants(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(5f, 6f, 7f);
            ModClass.Instance.StartCoroutine(Director.Spotlight(p, 3f));
            c.Snd("bell_toll", p);
            int n = c.Li(10, 14, 18);
            for (int i = 0; i < n; i++)
            {
                Vector3 at = p + Cast.Rand(r);
                Fx.Instance nail = Fx.Play("arts/nail", at + new Vector3(0f, 6f), Random.Range(1f, 1.6f), PrDim, 0f, 0.8f,
                    true, 8f);
                Fx.MoveTo(nail, at, 0.7f, false);
                ModClass.Instance.StartCoroutine(PrRemnantLand(c, at));
                yield return c.W(0.1f);
            }
            yield return c.W(0.8f);
            c.Impact(p, 0.8f);
        }

        private static IEnumerator PrRemnantLand(Cast c, Vector3 at)
        {
            yield return c.W(0.7f);
            c.Dust(at, 1.2f);
            c.Snd("nail_fall", at);
            c.Area(at, 1.1f, 0.8f, "slowness", 4f);
        }

        private static IEnumerator PrDreamOfCelestia(Cast c)
        {
            ModClass.Instance.StartCoroutine(Director.Spotlight(c.P, 4f));
            c.Say("...", c.P + new Vector3(0f, 2f), 14, PrDim);
            c.Snd("bell_toll");
            yield return c.W(0.6f);
            List<Actor> all = PrTargets(c, c.P, 18f, 12);
            foreach (Actor u in all)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 at = u.current_position;
                c.Fc("halo", at, 2f, PrDim, 0f, 0.6f, false, 8f);
                Fx.Instance nail = Fx.Play("arts/nail", at + new Vector3(0f, 8f), 3f, PrDim, 0f, 0.55f, true, 6f);
                Fx.MoveTo(nail, at, 0.5f, false);
                yield return c.W(0.5f);
                c.Burst(at, 3f);
                c.F("light_cross", at, 2.6f, 0f, 0f, false, 14f);
                c.Snd("crater_boom", at);
                c.Area(at, 2f, 5f);
                c.Crater(at, 1.6f);
                Cast.Shake(0.6f);
            }
            Director.Impact(c.P, PrDim, 0.9f);
        }
    }
}
