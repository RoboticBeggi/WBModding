using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>Tier 3: the seven Archons, each with a God ability that stops the world.</summary>
    public static partial class Styles
    {
        static partial void RegisterArchons() => ArchonArts.Register();
    }

    /// <summary>The Archon scripts, scoped so their names can't clash with other style files.</summary>
    internal static class ArchonArts
    {
        public static void Register()
        {
            AnemoArchon();
            GeoArchon();
            ElectroArchon();
            DendroArchon();
            HydroArchon();
            PyroArchon();
            CryoArchon();
        }

        private static void Go(IEnumerator pRoutine) => ModClass.Instance.StartCoroutine(pRoutine);

        /// <summary>Enemies of a God ability: everyone hostile within a map-scale radius, nearest first.</summary>
        private static List<Actor> GodFoes(Cast c, float pRadius, int pMax = 60)
        {
            List<Actor> foes = c.Foes(c.P, pRadius);
            Vector3 p = c.P;
            foes.Sort((x, y) => ((Vector3)x.current_position - p).sqrMagnitude
                .CompareTo(((Vector3)y.current_position - p).sqrMagnitude));
            if (foes.Count > pMax) foes.RemoveRange(pMax, foes.Count - pMax);
            return foes;
        }

        // ================================================================ Anemo · Thousand Winds (Barbatos)
        // The bard god: music in rhythm, gales that carry people far, freedom of the open sky.

        private static void AnemoArchon()
        {
            Library.Style("gi_gnosis_anemo", "Thousand Winds", 3, "anemo")
                .T("dandelion_gale", "Dandelion Gale", Shape.Line, 9f, 7f, DandelionGale,
                    "A road of wind spouts that carries everyone on it far down the line.")
                .T("lyre_refrain", "Lyre Refrain", Shape.Support, 0f, 14f, LyreRefrain,
                    "Four beats of a lyre: each pulse heals allies and quickens their feet.")
                .T("skyward_lift", "Skyward Lift", Shape.Crowd, 6f, 10f, SkywardLift,
                    "Every foe nearby is lifted into the sky at once, held, then dropped.")
                .T("stormeye_arrow", "Stormeye Arrow", Shape.Lone, 8f, 7f, StormeyeArrow,
                    "Leaps back, draws, and looses one arrow that drags its target away.")
                .T("wind_wall", "Wind Wall", Shape.Guard, 0f, 16f, WindWall,
                    "A cyclone curtain around the god repels everything that comes near.")
                .T("ballad_of_freedom", "Ballad of Freedom", Shape.Crowd, 7f, 22f, BalladOfFreedom,
                    "Five cyclones rise around the foe and converge into one.", 2, 3f, true)
                .God("thousand_winds", "Thousand Winds of Time", ThousandWindsOfTime,
                    "Every foe for miles is lifted by a spout, then one gale sweeps the world.");
        }

        private static IEnumerator DandelionGale(Cast c)
        {
            Vector3 dir = c.Dir;
            float reach = c.L(8f, 10f, 12f);
            int spouts = c.Li(5, 7, 9);
            c.Snd("wind_howl");
            for (int i = 1; i <= spouts; i++)
            {
                Vector3 at = Cast.Clamp(c.P + dir * (reach * i / spouts));
                c.F("wind_spout", at + new Vector3(0f, 0.8f), 1.6f, 0f, 0f, false, 22f);
                c.F("leaves", at, 1.2f, Cast.AngleOf(dir), 0f, false, 20f);
                foreach (Actor u in c.Foes(at, 1.2f))
                {
                    c.Hit(u, 0.45f);
                    c.Push(u, dir, c.L(3f, 4f, 5f), 0.4f);
                }
                if (i % 2 == 0) c.Snd("wind_gust", at);
                yield return c.W(0.06f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator LyreRefrain(Cast c)
        {
            c.Snd("chant");
            for (int beat = 0; beat < 4; beat++)
            {
                Fx.Instance ring = c.F("wind_ring", c.P, 1.2f, 0f, 0.5f, false, 18f);
                Cast.Grow(ring, c.L(4f, 5f, 6f));
                c.F("feather", c.P + Cast.Rand(1.5f), 0.8f, Random.Range(0f, 360f), 0.6f);
                foreach (Actor ally in c.Allies(c.L(5f, 6f, 7f)))
                {
                    c.Heal(ally, c.L(0.04f, 0.06f, 0.08f));
                    if (beat == 3) ally.addStatusEffect("dash", 5f);
                }
                c.Snd("feather_flutter");
                yield return c.W(0.35f);
            }
        }

        private static IEnumerator SkywardLift(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3f, 3.8f, 4.6f);
            c.F("vacuum", p, r * 1.4f, 0f, 0.5f, false, 18f);
            c.Snd("vortex", p);
            List<Actor> foes = c.Foes(p, r);
            foreach (Actor u in foes)
            {
                c.F("wind_spout", (Vector3)u.current_position + new Vector3(0f, 0.8f), 1.2f, 0f, 0f, false, 20f);
                Cast.Hold(u, 2.5f);
                Go(Cast.Rise(u, Random.Range(3f, 4.5f), 0.4f));
            }
            yield return c.W(0.5f);
            foreach (Actor u in foes) Go(Cast.Hang(u, 0.7f));
            c.Snd("wind_howl", p);
            yield return c.W(0.75f);
            foreach (Actor u in foes) Go(Cast.Fall(u, 0.15f));
            yield return c.W(0.16f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.Dust(u.current_position, 1.3f);
                c.Hit(u, c.L(1.2f, 1.6f, 2f));
            }
            c.Snd("slam", p);
            c.Impact(p, c.L(0.5f, 0.7f, 0.9f));
        }

        private static IEnumerator StormeyeArrow(Cast c)
        {
            Vector3 back = Cast.Clamp(c.P - c.Dir * 2.5f);
            c.Snd("leap");
            yield return c.Arc(back, 1.4f, 0.3f);
            CombatCore.Face(c.A, c.TP);
            c.F("wind_ring", c.Front(0.5f), 1.4f, c.Ang, 0.6f, true, 22f);
            c.Snd("charge");
            yield return c.W(0.45f);
            Vector3 from = c.Front(0.5f), to = c.TP;
            c.Shot("feather", from, to, c.L(1.6f, 2f, 2.4f), 0.12f);
            c.Snd("release");
            yield return c.W(0.12f);
            if (!c.TOk) yield break;
            c.Spark(c.TP, 1.6f);
            c.Hit(c.T, c.L(1.6f, 2f, 2.6f));
            // The arrow carries its target away with it.
            Vector3 carry = Cast.Clamp(c.TP + c.Dir * c.L(4f, 5f, 6f));
            c.Shot("feather", c.TP, carry, 1.6f, 0.35f);
            yield return c.Knock(c.T, c.Dir, Vector3.Distance(c.TP, carry), 0.35f);
            c.Burst(carry, 1.8f);
            c.Snd("boulder_hit", carry);
            c.Impact(carry, 0.5f);
        }

        private static IEnumerator WindWall(Cast c)
        {
            float life = c.L(3f, 4f, 5f);
            Fx.Instance wall = c.F("cyclone", c.P, 2.6f, 0f, life, true, 16f);
            Cast.Stick(wall, c.A);
            c.A.addStatusEffect("dodge", life);
            c.Snd("wind_howl");
            float t = 0f;
            while (t < life && c.Ok)
            {
                foreach (Actor u in c.Foes(c.P, 2.2f))
                {
                    c.Push(u, (Vector3)u.current_position - c.P, 2f, 0.25f);
                    c.Hit(u, 0.25f);
                }
                t += 0.4f;
                yield return c.W(0.4f);
            }
        }

        private static IEnumerator BalladOfFreedom(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(4f, 5f, 6f);
            c.Snd("leap");
            yield return Cast.Rise(c.A, 2.5f, 0.3f);
            var storms = new List<Fx.Instance>();
            for (int i = 0; i < 5; i++)
            {
                Vector3 at = p + Cast.Rot(Vector3.right, i * 72f) * r;
                Fx.Instance s = c.F("cyclone", at, 2.2f, 0f, 2.2f, true, 16f);
                Fx.MoveTo(s, p, 1.6f, false);
                storms.Add(s);
                c.Snd("vortex", at);
                yield return c.W(0.08f);
            }
            float t = 0f;
            while (t < 1.6f)
            {
                float k = 1f - t / 1.6f;
                for (int i = 0; i < 5; i++)
                {
                    Vector3 at = p + Cast.Rot(Vector3.right, i * 72f + t * 90f) * r * k;
                    c.Area(at, 1.2f, 0.25f);
                    c.PullAll(at, 1.6f, 0.2f);
                }
                t += 0.2f;
                yield return c.W(0.2f);
            }
            c.F("cyclone", p, c.L(4f, 5f, 6f), 0f, 1.2f, true, 18f);
            c.Snd("wind_howl", p);
            foreach (Actor u in c.Foes(p, 3f)) c.Launch(u, 3.5f, 0.6f);
            c.Area(p, 3f, 2.5f);
            yield return Cast.Fall(c.A, 0.3f);
            yield return c.W(0.7f);
            c.Burst(p, 5f);
            c.Impact(p, 1f);
        }

        private static IEnumerator ThousandWindsOfTime(Cast c)
        {
            List<Actor> foes = GodFoes(c, 26f);
            c.Snd("wind_howl");
            c.F("cyclone", c.P, 4f, 0f, 3.5f, true, 16f);
            for (int i = 0; i < foes.Count; i++)
            {
                Actor u = foes[i];
                if (!CombatCore.Alive(u)) continue;
                c.F("wind_spout", (Vector3)u.current_position + new Vector3(0f, 0.8f), 1.8f, 0f, 0f, false, 20f);
                Cast.Hold(u, 4f);
                Go(Cast.Rise(u, Random.Range(3f, 6f), 0.4f));
                c.Hit(u, 1.5f);
                if (i % 4 == 0)
                {
                    c.Snd("wind_gust", u.current_position);
                    yield return c.W(0.05f);
                }
            }
            yield return c.W(0.8f);
            // One gale sweeps the whole world from west to east.
            Vector3 left = c.P + new Vector3(-26f, 0f), right = c.P + new Vector3(26f, 0f);
            for (int row = -3; row <= 3; row++)
            {
                Vector3 off = new Vector3(0f, row * 4f);
                c.Shot("feather", left + off, right + off, 2.5f, 0.8f);
                c.Beam("fx:wave", left + off, right + off, 2f, 0.9f, true);
            }
            c.Snd("wind_howl");
            yield return c.W(0.4f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.Hit(u, 3f);
                Go(Cast.Fall(u, 0.25f));
                c.Push(u, Vector3.right, 4f, 0.5f);
            }
            c.Impact(c.P, 1f);
            yield return c.W(0.6f);
        }

        // ================================================================ Geo · Rex Lapis (Morax)
        // The God of Contracts: crushing weight from above, steles that resonate, unbreakable promises.

        private static void GeoArchon()
        {
            Library.Style("gi_gnosis_geo", "Rex Lapis", 3, "geo")
                .T("rain_of_jade", "Rain of Jade", Shape.Crowd, 6f, 9f, RainOfJade,
                    "Jade spears fall like rain over the crowd and stay planted.")
                .T("stele_resonance", "Stele Resonance", Shape.Crowd, 6f, 10f, SteleResonance,
                    "Three steles rise around the foe and hum, striking everything between them.")
                .T("contract_shield", "Jade Shield of Contract", Shape.Guard, 0f, 18f, ContractShield,
                    "An unbreakable lattice for the god and every ally near.")
                .T("dominus_lapidis", "Dominus Lapidis", Shape.Lone, 4f, 7f, DominusLapidis,
                    "A fist of stone rises under the foe, clutches it high and crushes it into the earth.")
                .T("spear_of_contract", "Spear of Contract", Shape.Gap, 10f, 6f, SpearOfContract,
                    "A thrown spear pins the foe, and the god is drawn to it for the reckoning.")
                .T("planet_befall", "Planet Befall", Shape.Crowd, 7f, 24f, PlanetBefall,
                    "A planet falls from the sky and turns the foes it crushes to stone.", 2, 3.5f, true)
                .God("heavens_weight", "Heavens' Weight", HeavensWeight,
                    "Planets rain on every army in sight and all who remain are petrified.");
        }

        private static IEnumerator RainOfJade(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2.5f, 3.2f, 4f);
            int spears = c.Li(10, 16, 24);
            c.Snd("jade_ring");
            for (int i = 0; i < spears; i++)
            {
                Vector3 at = p + Cast.Rand(r);
                c.Shot("jade_spear", at + new Vector3(0.6f, 6f), at, 1.4f, 0.18f);
                Go(JadeLands(c, at));
                yield return c.W(0.04f);
            }
            yield return c.W(0.3f);
            c.Impact(p, 0.5f);
        }

        private static IEnumerator JadeLands(Cast c, Vector3 at)
        {
            yield return c.W(0.18f);
            c.F("debris", at, 0.9f, 0f, 0f, false, 20f);
            c.Fc("jade_spear", at + new Vector3(0f, 0.4f), 1.2f, Color.white, -80f, 2f);
            if (c.Area(at, 0.8f, 0.5f) > 0) c.Snd("stone_crack", at);
        }

        private static IEnumerator SteleResonance(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2.2f, 2.8f, 3.4f);
            var steles = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                steles[i] = Cast.Clamp(p + Cast.Rot(Vector3.up, i * 120f) * r);
                c.F("rock_pillar", steles[i] + new Vector3(0f, 0.9f), 1.6f, 0f, c.L(2.2f, 2.8f, 3.4f), false, 16f);
                c.Snd("stone_rise", steles[i]);
                c.Area(steles[i], 0.9f, 0.6f);
                yield return c.W(0.1f);
            }
            int pulses = c.Li(3, 4, 6);
            for (int n = 0; n < pulses; n++)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 a = steles[i], b = steles[(i + 1) % 3];
                    c.Beam("fx:beam", a, b, 0.4f, 0.2f, true);
                    c.Lane(a, b, 0.6f, 0.3f);
                }
                c.Area(p, r * 0.7f, 0.35f);
                c.F("lattice", p, 1.4f, 0f, 0f, false, 22f);
                c.Snd("crystal_chime", p);
                yield return c.W(0.4f);
            }
            for (int i = 0; i < 3; i++)
            {
                c.F("debris", steles[i], 1.6f, 0f, 0f, false, 18f);
                c.Area(steles[i], 1.2f, 1f);
            }
            c.Snd("stone_crack", p);
            c.Impact(p, 0.6f);
        }

        private static IEnumerator ContractShield(Cast c)
        {
            float life = c.L(5f, 7f, 9f);
            Fx.Instance mine = c.F("lattice", c.P, 2.4f, 0f, life, true, 10f);
            Cast.Stick(mine, c.A);
            c.A.addStatusEffect("shield", life);
            if (c.Lv >= 2) c.A.addStatusEffect("invincible", c.L(0f, 1.5f, 2.5f));
            c.Snd("jade_ring");
            foreach (Actor ally in c.Allies(6f))
            {
                if (ally == c.A) continue;
                ally.addStatusEffect("shield", life * 0.6f);
                Fx.Instance s = c.F("lattice", ally.current_position, 1.4f, 0f, 1.2f, false, 12f);
                Cast.Stick(s, ally);
                yield return c.W(0.04f);
            }
            Fx.Instance ring = c.F("shock", c.P, 2f, 0f, 0f, false, 18f, true);
            Cast.Grow(ring, 3f);
            c.PushAll(c.P, 2.5f, 1.5f);
            yield return c.W(0.3f);
        }

        private static IEnumerator DominusLapidis(Cast c)
        {
            Actor v = c.T;
            c.Snd("stone_crack", c.TP);
            c.F("debris", c.TP, 1.4f, 0f, 0f, false, 18f);
            yield return c.W(0.2f);
            if (!c.TOk) yield break;
            Cast.Hold(v, 2.5f);
            // The fist erupts and closes.
            Fx.Instance fist = c.F("stone_fist", c.TP, 1.8f, 90f, 1.6f, true, 10f);
            c.Snd("stone_rise", c.TP);
            c.Hit(v, 0.6f);
            Go(Cast.Rise(v, 2.8f, 0.35f));
            if (fist != null) Fx.MoveTo(fist, c.TP + new Vector3(0f, 2.8f), 0.35f, false);
            yield return c.W(0.4f);
            for (int i = 0; i < c.Li(2, 3, 4); i++)
            {
                c.F("debris", (Vector3)v.current_position + new Vector3(0f, v.position_height * 0.3f), 1f, 0f, 0f, false, 22f);
                c.Snd("boulder_hit", v.current_position);
                c.Hit(v, 0.35f);
                Cast.Shake(0.2f);
                yield return c.W(0.18f);
            }
            if (fist != null) Fx.MoveTo(fist, c.TP, 0.12f, false);
            yield return Cast.Fall(v, 0.12f);
            c.Burst(c.TP, 2.6f);
            c.Crater(c.TP, c.L(1.2f, 1.6f, 2.2f));
            c.Snd("crater_boom", c.TP);
            c.Hit(v, c.L(1.6f, 2f, 2.6f));
            c.Impact(c.TP, c.L(0.6f, 0.8f, 1f));
        }

        private static IEnumerator SpearOfContract(Cast c)
        {
            Vector3 from = c.Front(0.5f);
            c.Shot("jade_spear", from, c.TP, 1.8f, 0.18f);
            c.Snd("whoosh_heavy");
            yield return c.W(0.18f);
            if (!c.TOk) yield break;
            Vector3 pin = c.TP;
            c.Hit(c.T, 0.8f, "stunned", 1.5f);
            Cast.Hold(c.T, 1.2f);
            c.Fc("jade_spear", pin, 1.6f, Color.white, Cast.AngleOf(c.Dir), 1.2f);
            c.Snd("jade_ring", pin);
            c.Beam("fx:beam", c.P, pin, 0.2f, 0.4f, true);
            yield return c.W(0.25f);
            // Drawn along the chain of the contract.
            yield return c.Dash(c.Behind(c.T, -0.8f), 0.18f);
            c.F("impact", c.TP, 2f, c.Ang, 0f, false, 24f, true);
            c.Fissure(c.TP, c.TP + c.Dir * 3f, 0.8f);
            c.Snd("boulder_hit", c.TP);
            c.Hit(c.T, c.L(1.4f, 1.8f, 2.3f));
            yield return c.Knock(c.T, c.Dir, c.L(1.5f, 2f, 3f), 0.25f);
        }

        private static IEnumerator PlanetBefall(Cast c)
        {
            Vector3 p = c.TP;
            float size = c.L(3f, 3.6f, 4.4f);
            Vector3 sky = p + new Vector3(3f, 9f);
            Fx.Instance planet = c.Shot("planet", sky, p, size, 1f, false, false, 10f);
            c.F("decree", p, size * 0.9f, 0f, 1f, false, 12f, true, true);
            c.Snd("rumble", p);
            yield return c.W(1f);
            c.Burst(p, size * 1.6f);
            c.F("debris", p, size, 0f, 0f, false, 16f);
            c.Crater(p, size * 0.8f);
            for (int i = 0; i < 4; i++) c.Fissure(p, p + Cast.Rot(Vector3.right, i * 90f + 30f) * size * 1.3f, 1f);
            c.Snd("crater_boom", p);
            c.Impact(p, 1f);
            foreach (Actor u in c.Foes(p, size * 0.8f))
            {
                c.Hit(u, c.L(3f, 3.5f, 4.5f), "stunned", c.L(2f, 3f, 4f));
                Cast.Hold(u, c.L(2f, 3f, 4f));
                c.Fc("lattice", u.current_position, 1f, new Color(0.85f, 0.7f, 0.4f), 0f, c.L(2f, 3f, 4f), true, 4f);
            }
            yield return c.W(0.4f);
        }

        private static IEnumerator HeavensWeight(Cast c)
        {
            List<Actor> foes = GodFoes(c, 26f);
            c.Snd("rumble");
            // Pick up to six cluster centres among the foes.
            var centres = new List<Vector3>();
            foreach (Actor u in foes)
            {
                Vector3 at = u.current_position;
                bool near = false;
                foreach (Vector3 ct in centres) if ((ct - at).sqrMagnitude < 25f) { near = true; break; }
                if (!near) centres.Add(at);
                if (centres.Count >= 6) break;
            }
            if (centres.Count == 0) centres.Add(c.TP);
            foreach (Vector3 ct in centres)
            {
                c.F("decree", ct, 4f, 0f, 1.4f, false, 12f, true, true);
                c.Shot("planet", ct + new Vector3(4f, 12f), ct, 4.5f, 1.1f, false, false, 10f);
                yield return c.W(0.12f);
            }
            yield return c.W(0.9f);
            foreach (Vector3 ct in centres)
            {
                c.Burst(ct, 6f);
                c.Crater(ct, 4f);
                for (int i = 0; i < 3; i++) c.Fissure(ct, ct + Cast.Rot(Vector3.right, Random.Range(0f, 360f)) * 6f, 1.2f);
                c.Snd("crater_boom", ct);
                c.Area(ct, 4f, 4f);
                yield return c.W(0.1f);
            }
            c.Impact(c.P, 1f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.Hit(u, 1.5f, "stunned", 5f);
                Cast.Hold(u, 5f);
                c.Fc("lattice", u.current_position, 1f, new Color(0.85f, 0.7f, 0.4f), 0f, 5f, true, 4f);
            }
            c.Snd("crystal_chime");
            yield return c.W(0.6f);
        }

        // ================================================================ Electro · Musou (Raiden)
        // Eternity: stillness, then a single cut. A judging eye. Marks that all fall at once.

        private static void ElectroArchon()
        {
            Library.Style("gi_gnosis_electro", "Musou", 3, "electro")
                .T("eye_of_judgment", "Eye of Stormy Judgment", Shape.Crowd, 7f, 11f, EyeOfJudgment,
                    "An eye opens over the field and strikes whoever stands nearest beneath it.")
                .T("baleful_omen", "Baleful Omen", Shape.Crowd, 8f, 9f, BalefulOmen,
                    "Marks foe after foe with omens; then every mark falls as lightning at once.")
                .T("shogun_pursuit", "Shogun's Pursuit", Shape.Gap, 9f, 7f, ShogunPursuit,
                    "Teleports from foe to foe, cutting each as she passes.")
                .T("euthymia", "Euthymia", Shape.Guard, 0f, 16f, Euthymia,
                    "Perfect stillness: untouchable, then a circle of cuts at whoever came close.")
                .T("eternal_pressure", "Eternity's Pressure", Shape.AntiAir, 7f, 8f, EternalPressure,
                    "A lance of lightning skewers a flier; a bolt from above nails it to the ground.")
                .T("musou_hitotachi", "Musou no Hitotachi", Shape.Line, 12f, 22f, MusouHitotachi,
                    "Silence, a drawn breath, and one cut that splits the land in a line.", 2, 4f, true)
                .God("final_cut", "Plane of Euthymia", PlaneOfEuthymia,
                    "Omens hang over every foe in sight. She sheathes. Everything is cut at once.");
        }

        private static IEnumerator EyeOfJudgment(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3.5f, 4.5f, 5.5f);
            float life = c.L(2.4f, 3f, 3.8f);
            c.F("storm_eye", p + new Vector3(0f, 3.5f), r * 1.2f, 0f, life + 0.3f, true, 12f);
            c.Snd("static_buzz", p);
            float t = 0f;
            while (t < life)
            {
                Actor u = c.NearestFoe(p, r);
                if (u != null)
                {
                    Vector3 at = u.current_position;
                    c.Beam("bolt", at + new Vector3(0f, 5f), at, 0.9f, 0.15f, false, false);
                    c.Spark(at, 1.2f);
                    c.Hit(u, 0.7f);
                    c.Snd("zap", at);
                }
                t += 0.35f;
                yield return c.W(0.35f);
            }
        }

        private static IEnumerator BalefulOmen(Cast c)
        {
            List<Actor> foes = c.Foes(c.TP, c.L(4f, 5f, 6f));
            int marks = Mathf.Min(foes.Count, c.Li(3, 5, 8));
            var marked = new List<Actor>();
            for (int i = 0; i < marks; i++)
            {
                Actor u = foes[i];
                Fx.Instance s = c.F("sigil", u.current_position, 1.2f, 0f, 1.6f, true, 14f);
                Cast.Stick(s, u);
                marked.Add(u);
                c.Snd("chant", u.current_position);
                yield return c.W(0.12f);
            }
            yield return c.W(0.5f);
            c.Snd("thunder_crack", c.TP);
            foreach (Actor u in marked)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 at = u.current_position;
                c.Beam("bolt", at + new Vector3(0f, 6f), at, 1.1f, 0.2f, false, false);
                c.F("impact", at, 1.6f, 0f, 0f, false, 26f, true);
                c.Hit(u, c.L(1.4f, 1.8f, 2.4f), "stunned", 1f);
            }
            c.Impact(c.TP, 0.6f);
        }

        private static IEnumerator ShogunPursuit(Cast c)
        {
            int hops = c.Li(3, 4, 6);
            Actor v = c.T;
            var cut = new HashSet<Actor>();
            for (int i = 0; i < hops && CombatCore.Alive(v) && c.Ok; i++)
            {
                Vector3 from = c.P;
                Vector3 to = c.Behind(v, 0.8f);
                c.Beam("iai", from, to, 0.5f, 0.25f, true, false);
                c.Blink(to, "vacuum");
                c.F("cross_cut", v.current_position, 1.5f, Random.Range(0f, 90f), 0f, false, 26f, true);
                c.Snd("iai_draw", v.current_position);
                c.Hit(v, 0.9f);
                cut.Add(v);
                yield return c.W(0.12f);
                Actor next = null;
                float best = 64f;
                foreach (Actor u in c.Foes(c.P, 8f))
                {
                    if (cut.Contains(u)) continue;
                    float d = ((Vector3)u.current_position - c.P).sqrMagnitude;
                    if (d < best) { best = d; next = u; }
                }
                v = next;
            }
            yield return c.W(0.2f);
        }

        private static IEnumerator Euthymia(Cast c)
        {
            float life = c.L(1.5f, 2f, 2.5f);
            c.A.addStatusEffect("invincible", life + 0.3f);
            Fx.Instance s = c.F("sigil", c.P, 2.2f, 0f, life, true, 8f, true, true);
            Cast.Stick(s, c.A);
            c.Snd("static_buzz");
            yield return c.W(life);
            float r = c.L(2.5f, 3f, 3.6f);
            for (int i = 0; i < 6; i++)
            {
                Vector3 d = Cast.Rot(Vector3.right, i * 60f);
                c.Beam("iai", c.P, c.P + d * r, 0.4f, 0.2f, true, false);
            }
            c.Snd("iai_draw");
            c.Area(c.P, r, c.L(1.2f, 1.6f, 2f));
            c.PushAll(c.P, r, 1.5f);
        }

        private static IEnumerator EternalPressure(Cast c)
        {
            Actor v = c.T;
            c.Shot("bolt", c.Front(0.4f), c.TP + new Vector3(0f, Mathf.Max(0f, v.position_height * 0.3f)), 1.2f, 0.12f);
            c.Snd("zap");
            yield return c.W(0.12f);
            if (!c.TOk) yield break;
            c.Hit(v, 1f, "stunned", 1.2f);
            Cast.Hold(v, 1.2f);
            yield return c.W(0.25f);
            Vector3 at = c.TP;
            c.Beam("bolt", at + new Vector3(0f, 7f), at, 1.4f, 0.25f, true, false);
            c.Snd("thunder_crack", at);
            yield return Cast.Fall(v, 0.1f);
            c.Burst(at, 2.2f);
            c.Crater(at, c.L(1f, 1.4f, 1.8f));
            c.Hit(v, c.L(1.6f, 2f, 2.6f));
            c.Impact(at, 0.7f);
        }

        private static IEnumerator MusouHitotachi(Cast c)
        {
            Vector3 dir = c.Dir;
            float reach = c.L(10f, 13f, 16f);
            c.F("sigil", c.P - dir * 0.8f, 2.6f, 0f, 1.2f, true, 10f, true, true);
            c.Snd("chant");
            yield return c.W(0.9f);
            c.Snd("iai_draw");
            Vector3 end = Cast.Clamp(c.P + dir * reach);
            c.Beam("iai", c.P, end, c.L(1.2f, 1.6f, 2f), 0.6f, true, false);
            c.Beam("bolt", c.P, end, 0.8f, 0.4f, false, false);
            Cinematics.Director.HitStop(0.18f);
            yield return c.W(0.3f);
            c.Fissure(c.P + dir, end, 1.4f);
            c.Lane(c.P, end, c.L(1.2f, 1.5f, 1.9f), c.L(3f, 3.8f, 5f), "stunned");
            for (int i = 1; i <= 4; i++) c.Spark(Vector3.Lerp(c.P, end, i / 4f), 1.8f);
            c.Snd("thunder_crack", end);
            c.Impact(Vector3.Lerp(c.P, end, 0.4f), 1f);
        }

        private static IEnumerator PlaneOfEuthymia(Cast c)
        {
            List<Actor> foes = GodFoes(c, 26f);
            foreach (Actor u in foes)
            {
                Fx.Instance s = c.F("sigil", u.current_position, 1.4f, 0f, 2.6f, true, 10f, true);
                Cast.Stick(s, u);
            }
            c.Snd("chant");
            yield return c.W(1.2f);
            c.Snd("stare");
            yield return c.W(0.8f);
            // The single cut, across the whole world.
            c.Beam("iai", c.P + new Vector3(-30f, 0f), c.P + new Vector3(30f, 0f), 2.4f, 0.7f, true, false);
            c.Snd("iai_draw");
            Cinematics.Director.HitStop(0.25f);
            yield return c.W(0.3f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("x_cut", u.current_position, 1.8f, 0f, 0f, false, 24f, true);
                c.Hit(u, 5f);
            }
            c.Snd("thunder_crack");
            c.Fissure(c.P + new Vector3(-20f, 0f), c.P + new Vector3(20f, 0f), 1.6f);
            c.Impact(c.P, 1f);
            yield return c.W(0.6f);
        }

        // ================================================================ Dendro · Irminsul (Nahida)
        // Wisdom: links between minds, dreams, a domain of illusion and the great tree.

        private static void DendroArchon()
        {
            Library.Style("gi_gnosis_dendro", "Irminsul", 3, "dendro")
                .T("seeds_of_skandha", "Seeds of Skandha", Shape.Crowd, 7f, 9f, SeedsOfSkandha,
                    "Binds foes together with vines; every pulse wounds all of them as one.")
                .T("dream_lull", "Dream Lull", Shape.Lone, 6f, 9f, DreamLull,
                    "Sings a foe to sleep inside a ring of leaves, then wakes it with thorns.")
                .T("akasha_scan", "Akasha Scan", Shape.Support, 0f, 14f, AkashaScan,
                    "A wave of knowledge heals allies and lays seeds on every foe it reads.")
                .T("root_ascend", "Root Ascension", Shape.AntiAir, 7f, 8f, RootAscension,
                    "Roots climb into the sky, seize a flier and drag it into the earth.")
                .T("kusanali_step", "Kusanali Step", Shape.Gap, 9f, 6f, KusanaliStep,
                    "Scatters into leaves and blooms back beside the foe in a burst of flowers.")
                .T("shrine_of_maya", "Shrine of Maya", Shape.Crowd, 6f, 22f, ShrineOfMaya,
                    "An illusory garden: foes inside wither and slow, allies inside heal.", 2, 3f, true)
                .God("irminsul_dream", "Irminsul's Dream", IrminsulDream,
                    "The world tree rises and drains every foe in sight through its roots.");
        }

        private static IEnumerator SeedsOfSkandha(Cast c)
        {
            List<Actor> foes = c.Foes(c.TP, c.L(4f, 5f, 6f));
            int n = Mathf.Min(foes.Count, c.Li(4, 6, 8));
            if (n == 0) yield break;
            foes = foes.GetRange(0, n);
            foreach (Actor u in foes)
            {
                c.Shot("seed", c.P, u.current_position, 0.8f, 0.2f);
                yield return c.W(0.04f);
            }
            c.Snd("vine_lash");
            yield return c.W(0.2f);
            int pulses = c.Li(3, 3, 4);
            for (int p = 0; p < pulses; p++)
            {
                for (int i = 0; i < foes.Count; i++)
                {
                    Actor a = foes[i], b = foes[(i + 1) % foes.Count];
                    if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) continue;
                    c.Beam("thorn_whip", a.current_position, b.current_position, 0.5f, 0.3f, false, true);
                }
                foreach (Actor u in foes)
                {
                    if (!CombatCore.Alive(u)) continue;
                    c.F("flower", u.current_position, 0.9f, 0f, 0f, false, 20f);
                    c.Hit(u, c.L(0.6f, 0.8f, 1f));
                }
                c.Snd("bloom_pop", c.TP);
                yield return c.W(0.45f);
            }
        }

        private static IEnumerator DreamLull(Cast c)
        {
            Actor v = c.T;
            float sleep = c.L(2f, 2.8f, 3.6f);
            Cast.Hold(v, sleep + 0.3f);
            v.addStatusEffect("stunned", sleep);
            Fx.Instance ring = c.F("leaves", v.current_position, 1.8f, 0f, sleep, true, 8f);
            Cast.Stick(ring, v);
            c.Snd("chant", v.current_position);
            float t = 0f;
            while (t < sleep && CombatCore.Alive(v))
            {
                c.Fc("seed", (Vector3)v.current_position + new Vector3(0.3f, 0.8f + t * 0.2f), 0.6f, Color.white, 0f, 0.5f);
                t += 0.5f;
                yield return c.W(0.5f);
            }
            if (!CombatCore.Alive(v)) yield break;
            for (int i = 0; i < 4; i++)
            {
                c.Beam("thorn_whip", (Vector3)v.current_position + Cast.Rot(Vector3.right, i * 90f) * 1.4f,
                    v.current_position, 0.5f, 0.25f, false, false);
            }
            c.Snd("vine_lash", v.current_position);
            c.Hit(v, c.L(2f, 2.6f, 3.4f));
            c.Impact(v.current_position, 0.5f);
        }

        private static IEnumerator AkashaScan(Cast c)
        {
            float r = c.L(6f, 7f, 8f);
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance ring = c.F("shock", c.P, 1.5f, 0f, 0.6f, false, 14f, true);
                Cast.Grow(ring, r / 1.5f * 2f);
                yield return c.W(0.15f);
            }
            c.Snd("leaf_rustle");
            foreach (Actor ally in c.Allies(r))
            {
                c.Heal(ally, c.L(0.12f, 0.16f, 0.22f));
                c.F("flower", ally.current_position, 0.9f, 0f, 0f, false, 18f);
            }
            foreach (Actor u in c.Foes(c.P, r))
            {
                c.F("seed", u.current_position, 0.7f, 0f, 1f, true, 10f);
                c.Hit(u, 0.4f, "slowness", 3f);
            }
        }

        private static IEnumerator RootAscension(Cast c)
        {
            Actor v = c.T;
            Vector3 ground = c.TP;
            for (int i = 0; i < 3; i++)
            {
                Vector3 start = ground + Cast.Rot(Vector3.right, i * 120f) * 1.5f;
                c.Beam("root", start, ground + new Vector3(0f, 2f), 0.6f, 0.9f, false, true);
            }
            c.Snd("root_creak", ground);
            c.Hit(v, 0.8f);
            Cast.Hold(v, 2f);
            yield return c.W(0.35f);
            if (!c.TOk) yield break;
            yield return Cast.Fall(v, 0.1f);
            c.Burst(ground, 2f);
            c.Crater(ground, 1.2f);
            c.Snd("slam", ground);
            c.Hit(v, c.L(1.4f, 1.8f, 2.4f), "slowness", 4f);
            c.F("root", ground + new Vector3(-0.8f, 0f), 1.8f, 0f, 2f, true, 10f);
        }

        private static IEnumerator KusanaliStep(Cast c)
        {
            Vector3 to = c.Behind(c.T, -0.9f);
            c.F("leaves", c.P, 1.6f, 0f, 0f, false, 22f);
            c.Snd("leaf_rustle");
            for (int i = 0; i < 6; i++) c.Shot("leaves", c.P + Cast.Rand(0.6f), to + Cast.Rand(0.6f), 0.8f, 0.25f);
            Cast.Place(c.A, to);
            yield return c.W(0.25f);
            c.F("flower", c.P, 1.6f, 0f, 0f, false, 20f);
            for (int i = 0; i < c.Li(3, 5, 7); i++) c.F("flower", c.TP + Cast.Rand(1.2f), 0.9f, 0f, 0f, false, 22f);
            c.Snd("bloom_pop");
            c.Area(c.TP, 1.6f, c.L(1.2f, 1.5f, 2f));
            yield return c.W(0.2f);
        }

        private static IEnumerator ShrineOfMaya(Cast c)
        {
            Vector3 p = c.P + c.Dir * 1.5f;
            float r = c.L(4f, 5f, 6f);
            float life = c.L(3f, 3.6f, 4.4f);
            c.F("ritual", p, r * 2f, 0f, life, true, 8f, true, true);
            c.F("world_tree", p + new Vector3(0f, 1.6f), 3f, 0f, life, false, 8f);
            c.Snd("chant", p);
            float t = 0f;
            while (t < life)
            {
                foreach (Actor u in c.Foes(p, r)) c.Hit(u, 0.35f, "slowness", 1.5f);
                foreach (Actor ally in c.Allies(r)) c.Heal(ally, 0.03f);
                c.F("flower", p + Cast.Rand(r), 1f, 0f, 0f, false, 18f);
                c.F("leaves", p + Cast.Rand(r), 1.2f, Random.Range(0f, 360f), 0f, false, 16f);
                t += 0.4f;
                yield return c.W(0.4f);
            }
            c.Area(p, r, 1.8f);
            c.Snd("bloom_pop", p);
            c.Impact(p, 0.6f);
        }

        private static IEnumerator IrminsulDream(Cast c)
        {
            List<Actor> foes = GodFoes(c, 26f);
            c.F("world_tree", c.P + new Vector3(0f, 3f), 7f, 0f, 3.6f, false, 6f);
            c.F("ritual", c.P, 10f, 0f, 3.6f, true, 6f, true, true);
            c.Snd("chant");
            yield return c.W(0.6f);
            foreach (Actor u in foes) c.Fc("seed", u.current_position, 0.9f, Color.white, 0f, 2.6f, true, 8f);
            c.Snd("leaf_rustle");
            for (int pulse = 0; pulse < 3; pulse++)
            {
                foreach (Actor u in foes)
                {
                    if (!CombatCore.Alive(u)) continue;
                    c.Beam("root", c.P, u.current_position, 0.5f, 0.35f, false, true);
                    c.Hit(u, 1.6f, "slowness", 4f);
                }
                foreach (Actor ally in c.Allies(26f)) c.Heal(ally, 0.1f);
                Fx.Instance ring = c.F("shock", c.P, 3f, 0f, 0.6f, false, 14f, true);
                Cast.Grow(ring, 8f);
                c.Snd("bloom_pop");
                yield return c.W(0.55f);
            }
            for (int i = 0; i < 24; i++) c.F("leaves", c.P + Cast.Rand(18f), 1.4f, Random.Range(0f, 360f), 0f, false, 14f);
            c.Impact(c.P, 1f);
        }

        // ================================================================ Hydro · Grand Tribunal (Furina / Focalors)
        // Theatre and judgment: a troupe of water companions, verdicts, a ballet on the tide, the flood.

        private static void HydroArchon()
        {
            Library.Style("gi_gnosis_hydro", "Grand Tribunal", 3, "hydro")
                .T("salon_solitaire", "Salon Solitaire", Shape.Crowd, 6f, 10f, SalonSolitaire,
                    "Three bubble companions orbit her, then dart out one after another into the crowd.")
                .T("verdict", "Verdict of the Oratrice", Shape.Execute, 7f, 9f, Verdict,
                    "A seal of judgment over the guilty; the gong sounds; a column of water falls.")
                .T("tidal_ballet", "Tidal Ballet", Shape.Gap, 10f, 6f, TidalBallet,
                    "Glides in a spiralling curve on the tide, splashing everyone in the way.")
                .T("pneuma_ousia", "Pneuma and Ousia", Shape.Guard, 0f, 15f, PneumaOusia,
                    "Pneuma heals her; Ousia answers with a ring of bursting droplets.")
                .T("applause", "Standing Ovation", Shape.Support, 0f, 14f, Applause,
                    "Fountains spring up beneath every ally to heal them.")
                .T("let_people_rejoice", "Let the People Rejoice", Shape.Line, 10f, 22f, LetPeopleRejoice,
                    "A tide wave rolls out down the line and sweeps everything with it.", 2, 3.5f, true)
                .God("great_flood", "The Great Flood", GreatFlood,
                    "The prophecy is fulfilled: every foe is drowned in a bubble as the flood rolls through.");
        }

        private static IEnumerator SalonSolitaire(Cast c)
        {
            int members = c.Li(3, 3, 4);
            float t = 0f;
            c.Snd("bubble_pop");
            while (t < 0.8f)
            {
                for (int i = 0; i < members; i++)
                {
                    Vector3 at = c.P + Cast.Rot(Vector3.right, i * 360f / members + t * 360f) * 1.2f;
                    c.F("bubble", at, 0.9f, 0f, 0.12f, false, 18f);
                }
                t += 0.1f;
                yield return c.W(0.1f);
            }
            List<Actor> foes = c.Foes(c.TP, 4f);
            int dives = c.Li(6, 9, 12);
            for (int i = 0; i < dives && foes.Count > 0; i++)
            {
                Actor u = foes[i % foes.Count];
                if (!CombatCore.Alive(u)) continue;
                Vector3 from = c.P + Cast.Rot(Vector3.right, i * 120f) * 1.2f;
                c.Shot("bubble", from, u.current_position, 1f, 0.15f);
                Go(BubbleBurst(c, u));
                yield return c.W(0.1f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator BubbleBurst(Cast c, Actor u)
        {
            yield return c.W(0.15f);
            if (!CombatCore.Alive(u)) yield break;
            c.F("droplet", u.current_position, 0.8f, Random.Range(0f, 360f), 0f);
            c.Snd("bubble_pop", u.current_position);
            c.Hit(u, 0.55f);
        }

        private static IEnumerator Verdict(Cast c)
        {
            Actor v = c.T;
            Cast.Hold(v, 1.6f);
            c.F("decree", (Vector3)v.current_position + new Vector3(0f, 1.6f), 1.8f, 0f, 1.2f, true, 10f, true);
            c.Snd("chant", v.current_position);
            yield return c.W(0.9f);
            c.Snd("decree_gong", v.current_position);
            yield return c.W(0.2f);
            if (!c.TOk) yield break;
            Vector3 at = c.TP;
            c.F("spring", at + new Vector3(0f, 1f), 2.4f, 180f, 0f, false, 20f);
            c.Shot("droplet", at + new Vector3(0f, 6f), at, 2f, 0.12f, false, true);
            yield return c.W(0.12f);
            c.Burst(at, 2.4f);
            c.Snd("splash_big", at);
            float bonus = v.getHealth() < v.getMaxHealth() * 0.35f ? 1.6f : 1f;
            c.Hit(v, c.L(2f, 2.5f, 3.2f) * bonus);
            c.Impact(at, 0.7f);
        }

        private static IEnumerator TidalBallet(Cast c)
        {
            Vector3 start = c.P, end = c.Behind(c.T, 0.8f);
            Vector3 side = Cast.Rot(c.Dir, 90f) * c.L(2f, 2.5f, 3f);
            c.Snd("wave_crash");
            int steps = 8;
            for (int i = 1; i <= steps; i++)
            {
                float k = i / (float)steps;
                Vector3 at = Vector3.Lerp(start, end, k) + side * Mathf.Sin(k * Mathf.PI);
                c.F("riptide", c.P, 1.4f, 0f, 0f, false, 22f);
                yield return c.Dash(at, 0.05f);
                c.Area(c.P, 1f, 0.35f);
            }
            c.F("water_whip", c.P, 2f, Random.Range(0f, 360f), 0f, false, 22f);
            c.Snd("splash_big");
            c.Area(c.P, 1.8f, c.L(1f, 1.3f, 1.7f));
            c.PushAll(c.P, 1.8f, 1.2f);
        }

        private static IEnumerator PneumaOusia(Cast c)
        {
            Fx.Instance glow = c.F("bubble", c.P, 2f, 0f, 1f, true, 10f);
            Cast.Stick(glow, c.A);
            c.Heal(c.A, c.L(0.15f, 0.2f, 0.28f));
            c.A.addStatusEffect("shield", 4f);
            c.Snd("chant");
            yield return c.W(0.7f);
            int drops = c.Li(10, 14, 18);
            for (int i = 0; i < drops; i++)
            {
                Vector3 d = Cast.Rot(Vector3.right, i * 360f / drops);
                c.Shot("droplet", c.P, c.P + d * c.L(2.5f, 3f, 3.6f), 0.9f, 0.2f);
            }
            c.Snd("splash_big");
            yield return c.W(0.2f);
            c.Area(c.P, c.L(2.5f, 3f, 3.6f), 1f);
        }

        private static IEnumerator Applause(Cast c)
        {
            c.Snd("chant");
            foreach (Actor ally in c.Allies(c.L(6f, 7f, 8f)))
            {
                c.F("spring", (Vector3)ally.current_position + new Vector3(0f, 0.9f), 1.4f, 0f, 0f, false, 20f);
                c.Heal(ally, c.L(0.15f, 0.2f, 0.26f));
                c.Snd("splash_big", ally.current_position);
                yield return c.W(0.06f);
            }
        }

        private static IEnumerator LetPeopleRejoice(Cast c)
        {
            Vector3 dir = c.Dir;
            float reach = c.L(9f, 11f, 13f);
            Vector3 end = Cast.Clamp(c.P + dir * reach);
            Fx.Instance wave = c.F("riptide", c.P + dir, c.L(3f, 3.6f, 4.4f), 0f, 1.2f, true, 16f);
            Fx.MoveTo(wave, end, 1f, false);
            c.Snd("wave_crash");
            float t = 0f;
            var swept = new HashSet<Actor>();
            while (t < 1f)
            {
                Vector3 at = Vector3.Lerp(c.P + dir, end, t);
                foreach (Actor u in c.Foes(at, 1.6f))
                {
                    if (!swept.Add(u)) continue;
                    c.Hit(u, c.L(1.4f, 1.8f, 2.4f));
                    c.Push(u, dir, Vector3.Distance(at, end) + 1f, 0.6f);
                }
                c.F("droplet", at + Cast.Rand(1f), 0.8f, Random.Range(0f, 360f));
                t += 0.1f;
                yield return c.W(0.1f);
            }
            c.Burst(end, 3f);
            c.Snd("splash_big", end);
            c.Impact(end, 0.8f);
        }

        private static IEnumerator GreatFlood(Cast c)
        {
            List<Actor> foes = GodFoes(c, 26f);
            c.Snd("wave_crash");
            foreach (Actor u in foes)
            {
                Fx.Instance b = c.F("bubble", u.current_position, 1.4f, 0f, 2.6f, true, 10f);
                Cast.Stick(b, u);
                Cast.Hold(u, 3.5f);
                Go(Cast.Rise(u, 1.5f, 0.5f));
            }
            yield return c.W(0.6f);
            Vector3 left = c.P + new Vector3(-28f, -6f), right = c.P + new Vector3(28f, 6f);
            for (int i = -2; i <= 2; i++)
            {
                c.Shot("leviathan", left + new Vector3(0f, i * 5f), right + new Vector3(0f, i * 5f), 6f, 1.4f, false, false, 12f);
            }
            for (int i = -3; i <= 3; i++)
            {
                c.Shot("riptide", left + new Vector3(0f, i * 4f), right + new Vector3(0f, i * 4f), 5f, 1.2f, false, false, 14f);
            }
            c.Snd("wave_crash");
            yield return c.W(1.2f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("droplet", u.current_position, 1.2f, Random.Range(0f, 360f));
                c.Hit(u, 5f);
                Go(Cast.Fall(u, 0.2f));
            }
            c.Snd("splash_big");
            c.Impact(c.P, 1f);
            yield return c.W(0.5f);
        }

        // ================================================================ Pyro · Sol Invictus (Mavuika)
        // The war god of Natlan: the flamestrider charge, banners of fire, the sun itself.

        private static void PyroArchon()
        {
            Library.Style("gi_gnosis_pyro", "Sol Invictus", 3, "pyro")
                .T("flamestrider", "Flamestrider Charge", Shape.Line, 10f, 8f, FlamestriderCharge,
                    "Rides straight through the line, fire pillars bursting up in her wake.")
                .T("banner_plunge", "War Banner Plunge", Shape.Crowd, 7f, 10f, WarBannerPlunge,
                    "Leaps high and plunges down; rings of fire roll out from the crater.")
                .T("nightsoul_blaze", "Nightsoul Blaze", Shape.Guard, 0f, 16f, NightsoulBlaze,
                    "Her body takes fire: stronger, and everyone near her burns.")
                .T("phoenix_escort", "Phoenix Escort", Shape.AntiAir, 8f, 8f, PhoenixEscort,
                    "Two phoenixes spiral up and dive on a flier together.")
                .T("hearths_grip", "Hearth's Grip", Shape.Lone, 2.5f, 7f, HearthsGrip,
                    "Seizes the foe, lifts it overhead, detonates it in her hand and hurls it away.")
                .T("crucible_sun", "Crucible of the Sun", Shape.Crowd, 8f, 24f, CrucibleOfTheSun,
                    "A sun sinks over the battlefield, swells, and falls.", 2, 4f, true)
                .God("sunfell_slice", "Sunfell Slice", SunfellSlice,
                    "A blazing sun rises and three world-wide arcs of fire cut through every foe.");
        }

        private static IEnumerator FlamestriderCharge(Cast c)
        {
            Vector3 start = c.P, dir = c.Dir;
            Vector3 end = Cast.Clamp(start + dir * c.L(8f, 10f, 12f));
            c.F("fx:aura_flame", c.P, 1.8f, 0f, 0.3f, false, 20f, true);
            c.Snd("fire_roar");
            yield return c.W(0.15f);
            Go(c.Dash(end, 0.35f));
            for (int i = 1; i <= 8; i++)
            {
                Vector3 at = Vector3.Lerp(start, end, i / 8f);
                c.Area(at, 1f, 0.6f, "burning", 3f);
                Go(FireTrail(c, at, 0.25f + i * 0.04f));
                yield return c.W(0.044f);
            }
            yield return c.W(0.4f);
            c.Burst(end, 2f);
        }

        private static IEnumerator FireTrail(Cast c, Vector3 at, float delay)
        {
            yield return c.W(delay);
            c.F("fire_pillar", at + new Vector3(0f, 1f), 1.4f, 0f, 0f, false, 18f);
            c.Snd("flame_burst", at);
            c.Area(at, 1f, 0.5f, "burning", 3f);
            Cast.Scorch(at, 0.5f, 1);
        }

        private static IEnumerator WarBannerPlunge(Cast c)
        {
            Vector3 p = c.TP;
            c.Snd("leap");
            yield return Cast.Rise(c.A, 4.5f, 0.35f);
            Cast.Place(c.A, p);
            Cast.SetHeight(c.A, 4.5f);
            c.F("flame_fist", c.P + new Vector3(0f, 1f), 1.6f, -90f, 0.3f, true, 20f);
            yield return Cast.Fall(c.A, 0.14f);
            c.Burst(p, 3f);
            c.Crater(p, c.L(1.6f, 2f, 2.6f));
            c.Snd("crater_boom", p);
            c.Area(p, 1.8f, c.L(1.8f, 2.2f, 2.8f));
            c.Impact(p, 0.8f);
            int rings = c.Li(2, 3, 4);
            for (int r = 1; r <= rings; r++)
            {
                int n = 6 + r * 3;
                for (int i = 0; i < n; i++)
                {
                    Vector3 at = p + Cast.Rot(Vector3.right, i * 360f / n) * (r * 1.4f);
                    c.F("fire_pillar", at + new Vector3(0f, 0.8f), 1.1f, 0f, 0f, false, 22f);
                }
                c.Area(p, r * 1.4f + 0.6f, 0.6f, "burning", 3f);
                c.Snd("flame_burst", p);
                yield return c.W(0.18f);
            }
            Cast.Scorch(p, 2.5f, 4);
        }

        private static IEnumerator NightsoulBlaze(Cast c)
        {
            float life = c.L(3f, 4f, 5f);
            Fx.Instance aura = c.F("fx:aura_flame", c.P, 2.2f, 0f, life, true, 14f, true);
            Cast.Stick(aura, c.A);
            c.A.addStatusEffect("powerup", life + 3f);
            c.Snd("fire_roar");
            float t = 0f;
            while (t < life && c.Ok)
            {
                foreach (Actor u in c.Foes(c.P, 2.4f)) c.Hit(u, 0.25f, "burning", 2f);
                c.F("ember", c.P + Cast.Rand(1.6f), 0.5f, 0f, 0f, false, 12f);
                t += 0.5f;
                yield return c.W(0.5f);
            }
        }

        private static IEnumerator PhoenixEscort(Cast c)
        {
            Vector3 at = c.TP;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 up = c.P + new Vector3(side * 2.5f, 5f);
                c.Shot("phoenix", c.P, up, 1.6f, 0.4f);
                Go(PhoenixDive(c, up, side));
            }
            c.Snd("phoenix_cry");
            yield return c.W(0.85f);
        }

        private static IEnumerator PhoenixDive(Cast c, Vector3 from, int side)
        {
            yield return c.W(0.42f);
            Vector3 to = c.TP;
            c.Shot("phoenix", from, to, 1.8f, 0.2f);
            yield return c.W(0.2f);
            c.F("flame_fist", to, 1.8f, 0f, 0f, false, 20f);
            c.Snd("flame_burst", to);
            if (c.TOk)
            {
                c.Hit(c.T, c.L(1.2f, 1.5f, 2f), "burning", 3f);
                if (side > 0) Go(Cast.Fall(c.T, 0.15f));
            }
            c.Area(to, 1.2f, 0.5f);
        }

        private static IEnumerator HearthsGrip(Cast c)
        {
            Actor v = c.T;
            yield return c.Dash(c.Behind(v, -0.5f), 0.08f);
            if (!c.TOk) yield break;
            c.Snd("punch");
            Cast.Hold(v, 2f);
            yield return Cast.Rise(v, 1.6f, 0.25f);
            c.F("ember", v.current_position, 1f, 0f, 0.4f, true, 16f);
            c.Snd("sizzle", v.current_position);
            yield return c.W(0.35f);
            c.F("flame_fist", v.current_position, 2.2f, 0f, 0f, false, 20f);
            c.Snd("flame_burst", v.current_position);
            c.Hit(v, c.L(1.6f, 2f, 2.6f), "burning", 4f);
            c.Impact(v.current_position, 0.6f);
            Cast.SetHeight(v, 0f);
            yield return c.Knock(v, c.Dir, c.L(3f, 4f, 5f), 0.3f);
            c.Dust(v.current_position, 1.4f);
            c.Fissure(c.P, v.current_position, 0.7f);
        }

        private static IEnumerator CrucibleOfTheSun(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3.5f, 4.2f, 5f);
            Fx.Instance sun = c.F("sun", p + new Vector3(0f, 6f), 2f, 0f, 2.2f, true, 12f);
            if (sun != null) { Fx.MoveTo(sun, p + new Vector3(0f, 3f), 1.5f, false); Cast.Grow(sun, 2.2f); }
            c.Snd("fire_roar", p);
            float t = 0f;
            while (t < 1.5f)
            {
                c.F("ember", p + Cast.Rand(r), 0.6f, 0f, 0f, false, 14f);
                foreach (Actor u in c.Foes(p, r)) c.Hit(u, 0.2f);
                t += 0.25f;
                yield return c.W(0.25f);
            }
            c.Shot("sun", p + new Vector3(0f, 3f), p, 4f, 0.25f, false, false);
            yield return c.W(0.25f);
            c.Burst(p, r * 1.8f);
            c.F("fire_pillar", p + new Vector3(0f, 1.6f), 3f, 0f, 0f, false, 16f);
            c.Crater(p, r * 0.8f);
            c.Snd("explosion_big", p);
            c.Area(p, r, c.L(3f, 3.6f, 4.6f), "burning", 5f);
            Cast.Scorch(p, r, 8);
            c.Impact(p, 1f);
        }

        private static IEnumerator SunfellSlice(Cast c)
        {
            List<Actor> foes = GodFoes(c, 26f);
            c.Snd("fire_roar");
            yield return Cast.Rise(c.A, 3f, 0.4f);
            c.F("sun", c.P + new Vector3(0f, 6f), 8f, 0f, 3.2f, true, 10f);
            yield return c.W(0.6f);
            for (int s = 0; s < 3; s++)
            {
                float ang = -25f + s * 25f;
                Vector3 d = Cast.Rot(Vector3.right, ang);
                Vector3 a = c.P - d * 30f, b = c.P + d * 30f;
                c.Beam("fx:beam", a, b, 2.4f, 0.4f, true);
                c.Beam("iai", a, b, 1.4f, 0.4f, true, false);
                c.Snd("slash_heavy");
                Cinematics.Director.HitStop(0.12f);
                foreach (Actor u in foes)
                {
                    if (!CombatCore.Alive(u)) continue;
                    c.Hit(u, 2f, "burning", 6f);
                    if (s == 2) c.F("flame_fist", u.current_position, 1.4f, Random.Range(0f, 360f));
                }
                yield return c.W(0.45f);
            }
            foreach (Actor u in foes) if (CombatCore.Alive(u)) Cast.Scorch(u.current_position, 1f, 1);
            yield return Cast.Fall(c.A, 0.3f);
            c.Snd("explosion_big");
            c.Impact(c.P, 1f);
        }

        // ================================================================ Cryo · Winter's Heart (the Tsaritsa)
        // A god of love turned to ice: decrees that freeze the land, a cold that holds and then breaks.

        private static void CryoArchon()
        {
            Library.Style("gi_gnosis_cryo", "Winter's Heart", 3, "cryo")
                .T("glacial_decree", "Glacial Decree", Shape.Crowd, 7f, 9f, GlacialDecree,
                    "A seal is pressed into the ground and ice spikes ripple outward ring by ring.")
                .T("heart_of_winter", "Heart of Winter", Shape.Guard, 0f, 16f, HeartOfWinter,
                    "Ice grows around her like armour and freezes whoever stands too close.")
                .T("frozen_throne", "Frozen Throne", Shape.Lone, 7f, 8f, FrozenThrone,
                    "Encases a foe in ice, drags it to her feet and breaks it with one touch.")
                .T("avalanche", "Avalanche", Shape.Line, 10f, 9f, Avalanche,
                    "A wall of snow and ice rolls down the line, burying everyone in it.")
                .T("winters_grasp", "Winter's Grasp", Shape.AntiAir, 8f, 8f, WintersGrasp,
                    "Ice lances shoot up from the ground at a flier and bring it down frozen.")
                .T("eternal_winter", "Eternal Winter", Shape.Crowd, 7f, 24f, EternalWinter,
                    "A blizzard settles over the field, then everything in it shatters.", 2, 4f, true)
                .God("frozen_world", "The Frozen World", FrozenWorld,
                    "Every foe in sight is sealed in ice; her heart beats three times; the world shatters.");
        }

        private static IEnumerator GlacialDecree(Cast c)
        {
            Vector3 p = c.TP;
            c.F("decree", p, 2f, 0f, 0.6f, false, 14f, true, true);
            c.Snd("decree_gong", p);
            yield return c.W(0.4f);
            int rings = c.Li(3, 4, 5);
            for (int r = 0; r < rings; r++)
            {
                float rad = 0.4f + r * 1.1f;
                int n = r == 0 ? 1 : 5 + r * 3;
                for (int i = 0; i < n; i++)
                {
                    Vector3 at = p + Cast.Rot(Vector3.right, i * 360f / n + r * 13f) * rad;
                    c.F("ice_garden", at, 1.2f, 0f, 0f, false, 20f);
                }
                c.Snd("ice_form", p);
                foreach (Actor u in c.Foes(p, rad + 0.6f)) c.Hit(u, 0.45f, r == rings - 1 ? "frozen" : "slowness", 2.5f);
                yield return c.W(0.14f);
            }
        }

        private static IEnumerator HeartOfWinter(Cast c)
        {
            float life = c.L(3f, 4f, 5f);
            c.A.addStatusEffect("shield", life);
            for (int i = 0; i < 6; i++)
            {
                Vector3 at = c.P + Cast.Rot(Vector3.right, i * 60f) * 1f;
                c.F("ice_garden", at, 1f, 0f, life, false, 6f);
            }
            Fx.Instance flake = c.F("snowflake", c.P, 1.8f, 0f, life, true, 8f);
            Cast.Stick(flake, c.A);
            c.Snd("ice_form");
            float t = 0f;
            while (t < life && c.Ok)
            {
                foreach (Actor u in c.Foes(c.P, 1.8f)) c.Hit(u, 0.3f, "frozen", 1.5f);
                t += 0.5f;
                yield return c.W(0.5f);
            }
            c.F("shatter", c.P, 2f, 0f, 0f, false, 18f);
            c.Snd("frost_shatter");
        }

        private static IEnumerator FrozenThrone(Cast c)
        {
            Actor v = c.T;
            c.F("ice_garden", v.current_position, 1.4f, 0f, 1.6f, false, 14f);
            c.Snd("ice_form", v.current_position);
            c.Hit(v, 0.6f, "frozen", 2f);
            Cast.Hold(v, 2f);
            yield return c.W(0.35f);
            if (!c.TOk) yield break;
            // Dragged across the frost to her feet.
            Vector3 feet = c.P + c.Dir * 0.8f;
            c.Beam("ice_lance", v.current_position, feet, 0.6f, 0.3f, false, false);
            yield return Cast.Move(v, feet, 0.25f);
            yield return c.W(0.25f);
            c.F("shatter", c.TP, 2f, 0f, 0f, false, 20f);
            c.Snd("frost_shatter", c.TP);
            c.Hit(v, c.L(2f, 2.6f, 3.4f));
            c.Impact(c.TP, 0.6f);
        }

        private static IEnumerator Avalanche(Cast c)
        {
            Vector3 dir = c.Dir, side = Cast.Rot(dir, 90f);
            float reach = c.L(8f, 10f, 12f);
            c.Snd("blizzard");
            for (int i = 1; i <= 10; i++)
            {
                Vector3 at = Cast.Clamp(c.P + dir * (reach * i / 10f));
                for (int s = -1; s <= 1; s++)
                {
                    c.F(s == 0 ? "shatter" : "dust", at + side * s * 0.9f, 1.4f, 0f, 0f, false, 20f);
                }
                foreach (Actor u in c.Foes(at, 1.6f))
                {
                    c.Hit(u, 0.5f, "slowness", 3f);
                    c.Push(u, dir, 1.2f, 0.15f);
                }
                yield return c.W(0.07f);
            }
            Vector3 end = Cast.Clamp(c.P + dir * reach);
            c.F("ice_garden", end, 2.4f, 0f, 2f, false, 12f);
            c.Area(end, 2f, 1.6f, "frozen", 2f);
            c.Snd("ice_crack", end);
            c.Impact(end, 0.6f);
        }

        private static IEnumerator WintersGrasp(Cast c)
        {
            Actor v = c.T;
            Vector3 ground = c.TP;
            int lances = c.Li(3, 4, 6);
            for (int i = 0; i < lances; i++)
            {
                Vector3 from = ground + Cast.Rot(Vector3.right, i * 360f / lances) * 1.6f;
                c.Shot("ice_lance", from, ground + new Vector3(0f, 1.5f), 1.4f, 0.15f);
                c.Snd("ice_form", from);
                yield return c.W(0.05f);
            }
            yield return c.W(0.1f);
            if (!c.TOk) yield break;
            c.Hit(v, c.L(1.2f, 1.6f, 2f), "frozen", 2.5f);
            Cast.Hold(v, 2f);
            yield return Cast.Fall(v, 0.15f);
            c.F("shatter", ground, 1.8f, 0f, 0f, false, 20f);
            c.Dust(ground, 1.4f);
            c.Snd("ice_crack", ground);
            c.Hit(v, 0.8f);
        }

        private static IEnumerator EternalWinter(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(4f, 5f, 6f);
            float life = c.L(2.4f, 3f, 3.8f);
            c.Snd("blizzard", p);
            float t = 0f;
            while (t < life)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 at = p + Cast.Rand(r);
                    c.Shot("snowflake", at + new Vector3(-1.5f, 3f), at, 0.8f, 0.3f);
                }
                foreach (Actor u in c.Foes(p, r)) c.Hit(u, 0.25f, "slowness", 2f);
                t += 0.3f;
                yield return c.W(0.3f);
            }
            c.F("zero", p, r * 1.6f, 0f, 0f, false, 14f);
            c.Snd("ice_form", p);
            List<Actor> frozen = c.Foes(p, r);
            foreach (Actor u in frozen) { c.Hit(u, 0.5f, "frozen", 2f); Cast.Hold(u, 1f); }
            yield return c.W(0.6f);
            foreach (Actor u in frozen)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("shatter", u.current_position, 1.4f, 0f, 0f, false, 20f);
                c.Hit(u, c.L(2.5f, 3f, 4f));
            }
            c.Snd("frost_shatter", p);
            c.Impact(p, 1f);
        }

        private static IEnumerator FrozenWorld(Cast c)
        {
            List<Actor> foes = GodFoes(c, 26f);
            c.F("zero", c.P, 12f, 0f, 0f, false, 10f);
            c.Snd("blizzard");
            foreach (Actor u in foes)
            {
                c.F("ice_garden", u.current_position, 1.4f, 0f, 3.2f, false, 8f);
                u.addStatusEffect("frozen", 5f);
                Cast.Hold(u, 4f);
            }
            yield return c.W(0.6f);
            // Three heartbeats, each breaking a wider ring of the world.
            for (int beat = 1; beat <= 3; beat++)
            {
                c.Snd("heartbeat");
                Fx.Instance ring = c.F("shock", c.P, 2f, 0f, 0.5f, false, 14f, true);
                Cast.Grow(ring, 5f * beat);
                float reach = 9f * beat;
                foreach (Actor u in foes)
                {
                    if (!CombatCore.Alive(u)) continue;
                    if (((Vector3)u.current_position - c.P).sqrMagnitude > reach * reach) continue;
                    c.F("shatter", u.current_position, 1.4f, 0f, 0f, false, 20f);
                    c.Hit(u, 2f);
                }
                c.Snd("frost_shatter");
                Cinematics.Director.HitStop(0.08f);
                yield return c.W(0.55f);
            }
            c.Impact(c.P, 1f);
        }
    }
}
