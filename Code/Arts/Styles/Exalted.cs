using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>Tier 2: Adepti, Harbingers, Witches, Abyss Heralds and Demon Gods.</summary>
    public static partial class Styles
    {
        static partial void RegisterExalted()
        {
            AdeptusStyle();
            HarbingerStyle();
            WitchStyle();
            HeraldStyle();
            DemonGodStyle();
        }

        // ================================================================ Adeptus · Adeptal Arts
        // A Liyue illuminated beast: a jade spear, cloud-riding leaps, talismans and the yaksha's plunge.

        private static void AdeptusStyle()
        {
            Library.Style("gi_adeptus", "Adeptal Arts", 2, "anemo").VisionElement()
                .T("jade_javelin", "Jade Javelin", Shape.Line, 8f, 6f, JadeJavelin,
                    "A thrown spear that pierces a line, plants itself and rings out. At III the adeptus follows it.")
                .T("cloud_stride", "Cloud Stride", Shape.AntiAir, 9f, 7f, CloudStride,
                    "Rides a cloud over the foe and drives the spear down through them.")
                .T("talisman_seal", "Talisman Seal", Shape.Lone, 5f, 9f, TalismanSeal,
                    "Talismans circle the foe and close in, sealing them in place.")
                .T("lotus_plunge", "Lotus Plunge", Shape.Crowd, 4f, 9f, LotusPlunge,
                    "Rises high into the sky and plunges straight down into a crowd.")
                .T("yaksha_mask", "Yaksha Mask", Shape.Guard, 0f, 20f, YakshaMask,
                    "Dons the mask: stronger and faster, at the cost of karmic pain.")
                .T("bane_of_evil", "Bane of All Evil", Shape.Crowd, 7f, 24f, BaneOfEvil,
                    "Masked, the yaksha plunges from foe to foe in a chain of falling strikes.", 2, 3f, true);
        }

        private static IEnumerator JadeJavelin(Cast c)
        {
            Vector3 from = c.P + c.Dir * 0.4f;
            Vector3 to = Cast.Clamp(c.TP + c.Dir * c.L(2f, 3f, 4f));
            c.Snd("whoosh_heavy");
            c.Shot("jade_spear", from, to, c.L(1.4f, 1.6f, 1.9f), 0.22f);
            yield return c.W(0.18f);
            c.Lane(from, to, 0.6f, 1f);
            // Planted, it rings and bursts.
            c.Fc("jade_spear", to + new Vector3(0f, 0.4f), 1.4f, Color.white, -80f, 0.6f);
            c.Snd("jade_ring", to);
            yield return c.W(0.35f);
            Fx.Instance ring = c.F("shock", to, 1.4f, 0f, 0f, false, 22f, true);
            Cast.Grow(ring, 1.8f);
            c.Area(to, c.L(1.2f, 1.6f, 2f), 0.8f);
            if (c.Lv >= 3)
            {
                c.Blink(to + new Vector3(0f, -0.3f), "wind_ring");
                c.Snd("jade_ring", to);
            }
        }

        private static IEnumerator CloudStride(Cast c)
        {
            if (!c.TOk) yield break;
            Fx.Instance cloud = c.F("cloud", c.P, 1.4f, 0f, 0.9f, true, 12f);
            Cast.Stick(cloud, c.A);
            c.Snd("wind_gust");
            Vector3 over = c.TP + c.Dir * 0.2f;
            yield return Cast.Arc(c.A, over, c.L(3.5f, 4.2f, 5f), 0.4f, c.Hot);
            Cast.SetHeight(c.A, c.L(3f, 3.5f, 4f));
            yield return Cast.Hang(c.A, 0.15f);
            c.Fc("jade_spear", c.P + new Vector3(0f, 1f), 1.5f, Color.white, -90f, 0.2f);
            yield return Cast.Fall(c.A, 0.1f);
            Vector3 p = c.P;
            c.F("impact", p, 1.8f, -90f, 0f, false, 26f, true);
            c.F("cloud", p, 2f, 0f, 0f, false, 14f);
            c.Snd("slam", p);
            c.Hit(c.T, c.L(1.3f, 1.6f, 2f));
            c.Area(p, 1.2f, 0.6f);
            c.Impact(p, c.L(0.4f, 0.55f, 0.75f));
        }

        private static IEnumerator TalismanSeal(Cast c)
        {
            if (!c.TOk) yield break;
            int count = c.Li(3, 4, 5);
            var runes = new List<Fx.Instance>();
            for (int i = 0; i < count; i++)
            {
                runes.Add(c.F("rune", c.P, 0.8f, 0f, 1.5f, true, 14f, true));
                c.Snd("chant");
            }
            float t = 0f;
            while (t < 0.9f && c.TOk)
            {
                t += Time.unscaledDeltaTime * Cinematics.Director.FxTimeScale;
                float radius = Mathf.Lerp(2f, 0.2f, t / 0.9f);
                for (int i = 0; i < runes.Count; i++)
                {
                    if (runes[i] == null || !runes[i].Active) continue;
                    float a = t * 6f + i * Mathf.PI * 2f / runes.Count;
                    Vector3 orbit = Vector3.Lerp(c.P, c.TP, Mathf.Clamp01(t * 3f));
                    runes[i].Go.transform.position = Vector3.Lerp(orbit, c.TP, Mathf.Clamp01(t * 3f)) +
                                                    new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                }
                yield return null;
            }
            if (!c.TOk) yield break;
            c.F("decree", c.TP, c.L(1.4f, 1.7f, 2f), 0f, 0.8f, false, 16f);
            c.Snd("decree_gong", c.TP);
            Cast.Hold(c.T, c.L(1.5f, 2.2f, 3f));
            c.Hit(c.T, c.L(0.9f, 1.1f, 1.4f), "stunned", c.L(1.5f, 2.2f, 3f));
            c.Say("SEALED", c.TP + new Vector3(0f, 1.2f), 10);
        }

        private static IEnumerator LotusPlunge(Cast c)
        {
            c.Snd("leap");
            yield return Cast.Rise(c.A, c.L(5f, 6f, 7f), 0.35f);
            if (c.TOk) Cast.Place(c.A, c.TP);
            yield return Cast.Hang(c.A, 0.25f);
            c.F("wind_spout", c.P + new Vector3(0f, 1f), 1.4f, 180f, 0.2f, false, 26f);
            yield return Cast.Fall(c.A, 0.12f);
            Vector3 p = c.P;
            float r = c.L(2f, 2.6f, 3.2f);
            c.F("flower", p, r, 0f, 0f, false, 18f, true);
            c.Burst(p, r * 1.3f);
            c.Crater(p, r * 0.6f);
            c.Snd("crater_boom", p);
            c.Impact(p, c.L(0.55f, 0.75f, 0.95f));
            c.Area(p, r, 1.3f);
            foreach (Actor u in c.Foes(p, r + 0.5f)) c.Launch(u, 1.6f, 0.2f);
            yield return c.W(0.3f);
        }

        private static IEnumerator YakshaMask(Cast c)
        {
            Fx.Instance mask = c.F("yaksha_mask", c.P + new Vector3(0f, 0.3f), 1f, 0f, 1.2f);
            Cast.Stick(mask, c.A);
            c.Snd("mask_drum");
            yield return c.W(0.25f);
            c.Snd("mask_drum");
            c.F("fx:aura_flame", c.P, 1.8f, 0f, c.L(4f, 6f, 8f), true, 14f, true, true);
            c.A.addStatusEffect("powerup", c.L(6f, 8f, 10f));
            c.A.addStatusEffect("dash", c.L(6f, 8f, 10f));
            PayLife(c, c.L(0.06f, 0.05f, 0.03f));
            c.Say("KARMA", c.P + new Vector3(0f, 1.4f), 11);
        }

        private static IEnumerator BaneOfEvil(Cast c)
        {
            Fx.Instance mask = c.F("yaksha_mask", c.P, 1.2f, 0f, 3f);
            Cast.Stick(mask, c.A);
            c.Snd("mask_drum");
            yield return c.W(0.3f);
            int plunges = c.Li(3, 4, 6);
            for (int i = 0; i < plunges; i++)
            {
                Actor v = i == 0 && c.TOk ? c.T : c.NearestFoe(c.P, 7f);
                if (v == null) break;
                c.Snd("leap");
                yield return Cast.Arc(c.A, v.current_position, 3.5f, 0.24f, c.Hot);
                Vector3 p = c.P;
                c.F("cloud", p, 1.8f, 0f, 0f, false, 18f);
                c.F("impact", p, 1.6f, -90f, 0f, false, 28f, true);
                c.Snd("slam", p);
                c.Area(p, 1.4f, 1.1f);
                c.Dust(p, 1.4f);
                Cast.Shake(0.35f);
                yield return c.W(0.08f);
            }
            // The last one comes from the highest point.
            yield return Cast.Rise(c.A, 6f, 0.25f);
            yield return Cast.Hang(c.A, 0.2f);
            yield return Cast.Fall(c.A, 0.1f);
            Vector3 q = c.P;
            float r = c.L(3f, 3.6f, 4.4f);
            c.Burst(q, r * 1.4f);
            c.F("wind_ring", q, r * 1.4f, 0f, 0f, false, 18f);
            c.Crater(q, r * 0.7f);
            c.Fissure(q, q + Cast.Rot(Vector3.right, Random.Range(0f, 360f)) * r, 1f);
            c.Snd("crater_boom", q);
            c.Impact(q, 1f);
            c.Area(q, r, 2.2f);
            c.PushAll(q, r, 1.8f);
        }

        // ================================================================ Harbinger · Fatui Protocol
        // The Tsaritsa's executives: orders, delusions, artillery and discipline.

        private static void HarbingerStyle()
        {
            Library.Style("gi_harbinger", "Fatui Protocol", 2, "cryo").VisionElement()
                .T("execution_decree", "Execution Decree", Shape.Execute, 6f, 8f, ExecutionDecree,
                    "A seal stamped under the foe. When it closes, the weak are executed outright.")
                .T("ordnance", "Ordnance Barrage", Shape.Crowd, 8f, 10f, Ordnance,
                    "Fatui artillery arcs in from behind the lines and walks across the area.")
                .T("delusion_surge", "Delusion Surge", Shape.Lone, 2.5f, 7f, DelusionSurge,
                    "A burst of borrowed power: a five-hit combo from both sides, ending in a launch.")
                .T("frost_chains", "Frost Chains", Shape.Line, 7f, 9f, FrostChains,
                    "Chains of ice link foe to foe, then freeze them all at once.")
                .T("iron_discipline", "Iron Discipline", Shape.Support, 0f, 18f, IronDiscipline,
                    "An order barked to every ally around: shields up, press on.")
                .T("final_protocol", "Final Protocol", Shape.Crowd, 7f, 24f, FinalProtocol,
                    "A seal over the battlefield, then a storm of executions across it.", 2, 3f, true);
        }

        private static IEnumerator ExecutionDecree(Cast c)
        {
            if (!c.TOk) yield break;
            Actor v = c.T;
            Fx.Instance seal = c.F("decree", c.TP, c.L(1.6f, 1.9f, 2.3f), 0f, 0.9f, false, 14f, false, true);
            Cast.Stick(seal, v);
            c.Snd("decree_gong", c.TP);
            Cast.Hold(v, 1f);
            yield return c.W(c.L(0.8f, 0.7f, 0.6f));
            if (!CombatCore.Alive(v)) yield break;
            bool weak = v.getHealth() < v.getMaxHealth() * c.L(0.3f, 0.35f, 0.45f);
            c.F("x_cut", v.current_position, 2f, 0f, 0f, false, 26f, true);
            c.Snd("slash_heavy", v.current_position);
            if (weak)
            {
                c.Say("EXECUTED", (Vector3)v.current_position + new Vector3(0f, 1.3f), 13);
                c.Hit(v, 6f);
                c.Impact(v.current_position, 0.8f);
            }
            else
            {
                c.Hit(v, 1.2f);
            }
        }

        private static IEnumerator Ordnance(Cast c)
        {
            Vector3 p = c.TP;
            Vector3 battery = c.P - c.Dir * 12f + new Vector3(0f, 6f);
            int shells = c.Li(5, 7, 10);
            float r = c.L(2.5f, 3f, 3.6f);
            c.Say("FIRE!", c.P + new Vector3(0f, 1.3f), 11);
            c.Snd("gunshot");
            for (int i = 0; i < shells; i++)
            {
                Vector3 at = p + c.Dir * Mathf.Lerp(-r, r, i / (float)Mathf.Max(1, shells - 1)) + Cast.Rand(1f);
                c.Shot("bullet", battery + Cast.Rand(1f), at, 0.9f, 0.3f);
                ModClass.Instance.StartCoroutine(ShellLands(c, at));
                yield return c.W(0.12f);
            }
            yield return c.W(0.4f);
        }

        private static IEnumerator ShellLands(Cast c, Vector3 at)
        {
            yield return c.W(0.3f);
            c.F("impact", at, 1.4f, 0f, 0f, false, 24f, true);
            c.Dust(at, 1.2f);
            c.Snd("explosion_small", at);
            Cast.Shake(0.2f);
            c.Area(at, 1.1f, 0.7f);
            Cast.Scorch(at, 0.6f, 1);
        }

        private static IEnumerator DelusionSurge(Cast c)
        {
            if (!c.TOk) yield break;
            c.F("lifeburn", c.P, 1.4f, 0f, 0f, false, 24f);
            c.Snd("heartbeat");
            PayLife(c, 0.03f);
            yield return c.W(0.15f);
            int hits = c.Li(5, 5, 7);
            for (int i = 0; i < hits && c.TOk; i++)
            {
                Vector3 side = c.TP + Cast.Rot(Vector3.right, i % 2 == 0 ? 0f : 180f) * 0.8f + new Vector3(0f, (i % 3 - 1) * 0.3f);
                c.Blink(side, "impact");
                CombatCore.Face(c.A, c.TP);
                c.F("impact", c.TP, 1.2f, Random.Range(0f, 360f), 0f, false, 30f, true);
                c.Snd(i % 2 == 0 ? "punch" : "kick", c.TP);
                c.Hit(c.T, 0.45f);
                yield return c.W(0.08f);
            }
            if (!c.TOk) yield break;
            c.F("shock", c.TP, 1.6f, 0f, 0f, false, 24f, true);
            c.Snd("launch", c.TP);
            c.Hit(c.T, 0.9f);
            c.Launch(c.T, c.L(2.5f, 3f, 3.6f), 0.4f);
        }

        private static IEnumerator FrostChains(Cast c)
        {
            int links = c.Li(3, 4, 6);
            var chain = new List<Actor>();
            Actor current = c.TOk ? c.T : c.NearestFoe(c.P, 7f);
            while (current != null && chain.Count < links)
            {
                chain.Add(current);
                Actor next = null;
                float best = 16f;
                foreach (Actor u in c.Foes(current.current_position, 4f))
                {
                    if (chain.Contains(u)) continue;
                    float d = ((Vector3)u.current_position - (Vector3)current.current_position).sqrMagnitude;
                    if (d < best) { best = d; next = u; }
                }
                current = next;
            }
            Vector3 prev = c.P;
            foreach (Actor u in chain)
            {
                c.Beam("fx:beam", prev, u.current_position, 0.35f, 0.9f, true);
                c.F("ice_garden", u.current_position, 1.2f, 0f, 0f, false, 20f);
                c.Snd("ice_form", u.current_position);
                Cast.Hold(u, 1.2f);
                prev = u.current_position;
                yield return c.W(0.12f);
            }
            yield return c.W(0.35f);
            foreach (Actor u in chain)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("shatter", u.current_position, 1.2f, 0f, 0f, false, 22f);
                c.Hit(u, 0.9f, "frozen", c.L(1.5f, 2.2f, 3f));
            }
            c.Snd("frost_shatter", chain.Count > 0 ? (Vector3)chain[0].current_position : c.P);
        }

        private static IEnumerator IronDiscipline(Cast c)
        {
            c.F("decree", c.P, 2.4f, 0f, 1f, false, 14f, false, true);
            c.Snd("decree_gong");
            c.Say("HOLD THE LINE", c.P + new Vector3(0f, 1.5f), 11);
            yield return c.W(0.3f);
            foreach (Actor ally in c.Allies(c.L(4f, 5f, 6f)))
            {
                c.F("lattice", ally.current_position, 1f, 0f, 0.6f, false, 16f, true);
                ally.addStatusEffect("shield", c.L(4f, 5f, 7f));
                ally.addStatusEffect("powerup", c.L(3f, 4f, 6f));
                if (c.Lv >= 3) c.Heal(ally, 0.1f);
            }
            c.Snd("stare");
        }

        private static IEnumerator FinalProtocol(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(5f, 6f, 7f);
            c.F("decree", p, r * 1.4f, 0f, 2.6f, true, 10f, true, true);
            c.Snd("decree_gong", p);
            c.Say("FINAL PROTOCOL", p + new Vector3(0f, 2f), 14);
            yield return c.W(0.6f);
            int cuts = c.Li(8, 12, 16);
            for (int i = 0; i < cuts; i++)
            {
                List<Actor> foes = c.Foes(p, r);
                Vector3 at = foes.Count > 0 ? (Vector3)foes[Random.Range(0, foes.Count)].current_position : p + Cast.Rand(r);
                c.F("x_cut", at, 1.5f, Random.Range(0f, 90f), 0f, false, 30f, true);
                c.Snd("slash_light", at);
                c.Area(at, 0.9f, 0.6f);
                yield return c.W(0.07f);
            }
            c.F("shatter", p, r, 0f, 0f, false, 18f);
            c.Burst(p, r * 1.2f);
            c.Snd("frost_shatter", p);
            c.Impact(p, 1f);
            c.Area(p, r, 1.5f, "frozen", 1.5f);
        }

        // ================================================================ Hexenzirkel · Moonlit Grimoire
        // A witch of the Hexenzirkel: familiars, runes, portals and curses under the moon.

        private static void WitchStyle()
        {
            Library.Style("gi_hexenzirkel", "Moonlit Grimoire", 2, "lunar")
                .T("owl_familiars", "Owl Familiars", Shape.Any, 7f, 6f, OwlFamiliars,
                    "Owls loosed on curving flights that hunt down the nearest foes.")
                .T("rune_circle", "Rune Circle", Shape.Crowd, 6f, 10f, RuneCircle,
                    "A spinning circle slows everyone inside, then moonlight erupts from it.")
                .T("portal_waltz", "Portal Waltz", Shape.Gap, 10f, 8f, PortalWaltz,
                    "Two portals: the witch steps through one and the foe is pulled out of the other. They trade places.")
                .T("hex_bolt", "Hex Bolt", Shape.Lone, 6f, 6f, HexBolt,
                    "A rune that circles its victim twice before striking, leaving a lingering curse.")
                .T("mirror_curse", "Mirror Curse", Shape.Guard, 0f, 16f, MirrorCurse,
                    "A cursed mirror shields the witch and leaves nearby foes confused.")
                .T("sabbath", "Witches' Sabbath", Shape.Crowd, 7f, 24f, Sabbath,
                    "A great ritual: owls circle and runes rain, then pillars of moonlight fall.", 2, 3f, true);
        }

        private static IEnumerator OwlFamiliars(Cast c)
        {
            int owls = c.Li(3, 5, 7);
            for (int i = 0; i < owls; i++)
            {
                ModClass.Instance.StartCoroutine(OwlFlight(c, i));
                c.Snd("feather_flutter");
                yield return c.W(0.1f);
            }
            yield return c.W(0.5f);
        }

        private static IEnumerator OwlFlight(Cast c, int i)
        {
            Fx.Instance owl = c.F("owl", c.P, 0.8f, 0f, 1.2f, true, 14f);
            if (owl == null) yield break;
            Vector3 swing = c.P + Cast.Rot(c.Dir, i % 2 == 0 ? 70f : -70f) * 2f + new Vector3(0f, 1f);
            Fx.MoveTo(owl, swing, 0.3f);
            yield return c.W(0.3f);
            Actor prey = c.NearestFoe(swing, 7f);
            if (prey == null) yield break;
            Fx.MoveTo(owl, prey.current_position, 0.25f);
            yield return c.W(0.25f);
            if (!CombatCore.Alive(prey)) yield break;
            c.F("rune", prey.current_position, 0.9f, 0f, 0f, false, 24f, true);
            c.Snd("star_chime", prey.current_position);
            c.Hit(prey, 0.55f);
        }

        private static IEnumerator RuneCircle(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2.2f, 2.8f, 3.4f);
            c.F("ritual", p, r * 2f, 0f, 1.4f, true, 12f, true, true);
            c.Snd("chant", p);
            foreach (Actor u in c.Foes(p, r)) u.addStatusEffect("slowness", 3f);
            yield return c.W(1f);
            c.F("fx:pillar", p + new Vector3(0f, 2f), r * 1.6f, 0f, 0f, false, 18f, true);
            c.F("moon_pillar", p + new Vector3(0f, 1.5f), r, 0f, 0f, false, 18f);
            c.Snd("moon_hum", p);
            c.Area(p, r, 1.5f);
            c.Impact(p, c.L(0.4f, 0.6f, 0.8f));
        }

        private static IEnumerator PortalWaltz(Cast c)
        {
            if (!c.TOk) yield break;
            Vector3 mine = c.P, theirs = c.TP;
            c.F("portal", mine, 1.4f, 0f, 0.7f, false, 16f);
            c.F("portal", theirs, 1.4f, 0f, 0.7f, false, 16f);
            c.Snd("portal_open");
            yield return c.W(0.3f);
            // They trade places.
            c.Blink(theirs + c.Dir * 0.3f, "vacuum");
            Cast.Place(c.T, mine);
            c.F("vacuum", mine, 1.2f, 0f, 0f, false, 26f);
            c.Snd("space_warp", mine);
            Cast.Hold(c.T, c.L(0.6f, 0.9f, 1.3f));
            c.Hit(c.T, c.L(0.7f, 0.9f, 1.2f));
            yield return c.W(0.2f);
            if (c.Lv >= 2 && c.TOk)
            {
                c.Shot("rune", c.P, c.TP, 0.8f, 0.2f, true);
                yield return c.W(0.2f);
                c.Hit(c.T, 0.6f);
            }
        }

        private static IEnumerator HexBolt(Cast c)
        {
            if (!c.TOk) yield break;
            Fx.Instance bolt = c.F("rune", c.P, 0.8f, 0f, 1.4f, true, 18f, true);
            c.Snd("whisper");
            float t = 0f;
            Vector3 from = c.P;
            while (t < 1f && c.TOk)
            {
                t += Time.unscaledDeltaTime * Cinematics.Director.FxTimeScale;
                float a = t * Mathf.PI * 4f;
                float radius = Mathf.Lerp(1.4f, 0f, t);
                Vector3 centre = Vector3.Lerp(from, c.TP, Mathf.Clamp01(t * 2.5f));
                if (bolt != null && bolt.Active) bolt.Go.transform.position = centre + new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                yield return null;
            }
            if (!c.TOk) yield break;
            c.F("corrosion", c.TP, 1.2f, 0f, 0f, false, 18f);
            c.Snd("dark_pulse", c.TP);
            c.Hit(c.T, c.L(1f, 1.3f, 1.6f), "poisoned", c.L(3f, 5f, 7f));
        }

        private static IEnumerator MirrorCurse(Cast c)
        {
            Fx.Instance m = c.F("mirror", c.P + new Vector3(0f, 0.6f), 1.2f, 0f, 1.2f, false, 12f);
            Cast.Stick(m, c.A);
            c.Snd("glass_break");
            c.A.addStatusEffect("shield", c.L(4f, 5f, 7f));
            foreach (Actor u in c.Foes(c.P, c.L(3f, 3.5f, 4.5f)))
            {
                u.addStatusEffect("confused", c.L(3f, 4f, 6f));
                c.F("rune", u.current_position, 0.7f, 0f, 0f, false, 20f, true);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator Sabbath(Cast c)
        {
            Vector3 p = c.P;
            float r = c.L(5f, 6f, 7f);
            c.F("ritual", p, r * 1.8f, 0f, 3.2f, true, 10f, true, true);
            c.Snd("chant", p);
            c.Snd("moon_hum", p);
            for (int i = 0; i < 4; i++)
            {
                Fx.Instance owl = c.F("owl", p, 0.8f, 0f, 2.8f, true, 14f);
                ModClass.Instance.StartCoroutine(Circle(owl, p, 2.2f, i * Mathf.PI / 2f, 2.8f));
            }
            int runes = c.Li(10, 14, 18);
            for (int i = 0; i < runes; i++)
            {
                List<Actor> foes = c.Foes(p, r);
                if (foes.Count > 0)
                {
                    Actor v = foes[Random.Range(0, foes.Count)];
                    c.Shot("rune", p + Cast.Rand(1.5f) + new Vector3(0f, 1.5f), v.current_position, 0.7f, 0.15f, true);
                    ModClass.Instance.StartCoroutine(DelayedHit(c, v, 0.15f, 0.5f));
                }
                yield return c.W(0.15f);
            }
            List<Actor> last = c.Foes(p, r);
            for (int i = 0; i < Mathf.Min(3, last.Count); i++)
            {
                Vector3 at = last[i].current_position;
                c.F("moon_pillar", at + new Vector3(0f, 1.5f), 1.6f, 0f, 0f, false, 18f);
                c.Snd("moon_hum", at);
                c.Area(at, 1.4f, 2f);
            }
            c.Impact(p, 0.9f);
        }

        private static IEnumerator Circle(Fx.Instance pFx, Vector3 pCentre, float pRadius, float pPhase, float pLife)
        {
            float t = 0f;
            while (t < pLife && pFx != null && pFx.Active)
            {
                t += Time.unscaledDeltaTime * Cinematics.Director.FxTimeScale;
                float a = pPhase + t * 2.5f;
                pFx.Go.transform.position = pCentre + new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.7f) * pRadius;
                yield return null;
            }
        }

        private static IEnumerator DelayedHit(Cast c, Actor v, float pDelay, float pPower)
        {
            yield return c.W(pDelay);
            if (!CombatCore.Alive(v)) yield break;
            c.Spark(v.current_position, 0.8f);
            c.Hit(v, pPower);
        }

        // ================================================================ Abyss Herald · Abyssal Liturgy
        // A herald of the Abyss Order: hands from below, black flame, bats and rifts.

        private static void HeraldStyle()
        {
            Library.Style("gi_abyss_herald", "Abyssal Liturgy", 2, "abyss")
                .T("abyss_grasp", "Grasp of the Abyss", Shape.Lone, 6f, 7f, AbyssGrasp,
                    "A hand rises under the foe, seizes them and drags them to the herald.")
                .T("corrosive_trail", "Corrosive Trail", Shape.Line, 7f, 9f, CorrosiveTrail,
                    "Rushes down a line leaving pools of corrosion that eat at anyone standing in them.")
                .T("black_flame_ring", "Black Flame Ring", Shape.Crowd, 3f, 10f, BlackFlameRing,
                    "Rings of abyssal flame burst outward from the herald in waves.")
                .T("bat_swarm", "Bat Swarm", Shape.AntiAir, 7f, 7f, BatSwarm,
                    "Bats spiral up around the foe; fliers are dragged out of the sky.")
                .T("rift_walk", "Rift Walk", Shape.Guard, 0f, 14f, RiftWalk,
                    "Sinks into a rift, untouchable, and rises behind the nearest foe.")
                .T("abyssal_tide", "Abyssal Tide", Shape.Crowd, 7f, 24f, AbyssalTide,
                    "A rift opens; hands erupt everywhere and drag every foe into it before it bursts.", 2, 3f, true);
        }

        private static IEnumerator AbyssGrasp(Cast c)
        {
            if (!c.TOk) yield break;
            Actor v = c.T;
            c.F("shadow_hand", (Vector3)v.current_position + new Vector3(0f, 0.5f), c.L(1.2f, 1.4f, 1.7f), 0f, 0f, false, 16f);
            c.Snd("whisper", v.current_position);
            Cast.Hold(v, 1.2f);
            yield return c.W(0.35f);
            if (!CombatCore.Alive(v)) yield break;
            c.Hit(v, 0.6f);
            Vector3 toward = c.P - (Vector3)v.current_position;
            c.Snd("dark_pulse", v.current_position);
            yield return c.Knock(v, toward, Mathf.Max(0f, toward.magnitude - 0.8f), 0.3f);
            if (!CombatCore.Alive(v)) yield break;
            c.F("abyss_flame", (Vector3)v.current_position + new Vector3(0f, 0.4f), 1.2f, 0f, 0f, false, 20f);
            c.Snd("slash_heavy", v.current_position);
            c.Hit(v, c.L(1f, 1.3f, 1.7f));
        }

        private static IEnumerator CorrosiveTrail(Cast c)
        {
            Vector3 start = c.P;
            Vector3 end = Cast.Clamp(c.TP + c.Dir * 1.5f);
            c.Snd("dash");
            yield return c.Dash(end, 0.18f);
            int pools = c.Li(3, 4, 5);
            var spots = new List<Vector3>();
            for (int i = 0; i < pools; i++)
            {
                Vector3 at = Vector3.Lerp(start, end, (i + 0.5f) / pools);
                spots.Add(at);
                c.F("corrosion", at, 1.4f, 0f, 3f, true, 10f, false, true);
            }
            c.Snd("abyss_hum", end);
            for (int tick = 0; tick < c.Li(4, 5, 6); tick++)
            {
                foreach (Vector3 s in spots) c.Area(s, 0.9f, 0.25f, "poisoned", 2f);
                yield return c.W(0.5f);
            }
        }

        private static IEnumerator BlackFlameRing(Cast c)
        {
            Vector3 p = c.P;
            int waves = c.Li(2, 3, 4);
            for (int w = 1; w <= waves; w++)
            {
                float radius = w * 1.2f;
                int flames = 6 + w * 3;
                for (int i = 0; i < flames; i++)
                {
                    Vector3 at = p + Cast.Rot(Vector3.right, i * 360f / flames + w * 15f) * radius;
                    c.F("abyss_flame", at + new Vector3(0f, 0.5f), 1f, 0f, 0f, false, 18f);
                }
                c.Snd("fire_roar", p);
                c.Area(p, radius + 0.5f, 0.6f);
                c.PushAll(p, radius + 0.5f, 0.8f);
                Cast.Shake(0.25f);
                yield return c.W(0.22f);
            }
        }

        private static IEnumerator BatSwarm(Cast c)
        {
            if (!c.TOk) yield break;
            Actor v = c.T;
            int bats = c.Li(6, 9, 12);
            c.Snd("bat_swarm", v.current_position);
            for (int i = 0; i < bats; i++)
            {
                Fx.Instance bat = c.F("bat", c.P, 0.6f, 0f, 1.2f, true, 16f);
                ModClass.Instance.StartCoroutine(Spiral(bat, v, i * 0.6f, 1.1f));
            }
            for (int i = 0; i < c.Li(4, 5, 6) && CombatCore.Alive(v); i++)
            {
                c.Hit(v, 0.3f);
                yield return c.W(0.2f);
            }
            if (!CombatCore.Alive(v)) yield break;
            if (v.position_height > 0.3f || v.asset.flying)
            {
                c.Say("DOWNED", (Vector3)v.current_position + new Vector3(0f, 1.2f), 10);
                yield return Cast.Fall(v, 0.15f);
                c.Burst(v.current_position, 1.6f);
                c.Hit(v, 1.4f);
            }
        }

        private static IEnumerator Spiral(Fx.Instance pFx, Actor pAround, float pPhase, float pLife)
        {
            float t = 0f;
            while (t < pLife && pFx != null && pFx.Active && CombatCore.Alive(pAround))
            {
                t += Time.unscaledDeltaTime * Cinematics.Director.FxTimeScale;
                float a = pPhase + t * 7f;
                float r = Mathf.Lerp(1.5f, 0.5f, t / pLife);
                pFx.Go.transform.position = (Vector3)pAround.current_position + new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r * 0.6f + t * 1.2f);
                yield return null;
            }
        }

        private static IEnumerator RiftWalk(Cast c)
        {
            c.F("rift", c.P + new Vector3(0f, 0.6f), 1.4f, 0f, 0.8f, false, 14f);
            c.Snd("portal_open");
            c.A.addStatusEffect("invincible", 1.2f);
            yield return c.W(0.5f);
            Actor foe = c.NearestFoe(c.P, 8f);
            if (foe != null)
            {
                Vector3 back = c.Behind(foe, 0.8f);
                c.F("rift", back + new Vector3(0f, 0.6f), 1.4f, 0f, 0.6f, false, 14f);
                c.Blink(back, "vacuum");
                c.F("x_cut", foe.current_position, 1.4f, 0f, 0f, false, 28f, true);
                c.Snd("slash_heavy", foe.current_position);
                c.Hit(foe, 0.9f);
            }
            c.Heal(c.A, c.L(0.08f, 0.12f, 0.16f));
        }

        private static IEnumerator AbyssalTide(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(5f, 6f, 7.5f);
            Fx.Instance rift = c.F("rift", p + new Vector3(0f, 1f), 2.6f, 0f, 2.6f, true, 12f);
            Cast.Grow(rift, 1.4f);
            c.Snd("portal_open", p);
            c.Snd("abyss_hum", p);
            yield return c.W(0.5f);
            List<Actor> foes = c.Foes(p, r);
            int hands = Mathf.Min(foes.Count, c.Li(4, 6, 8));
            for (int i = 0; i < hands; i++)
            {
                Actor v = foes[i];
                c.F("shadow_hand", (Vector3)v.current_position + new Vector3(0f, 0.5f), 1.3f, 0f, 0f, false, 18f);
                c.Hit(v, 0.6f);
                Cast.Hold(v, 1.5f);
                c.Snd("whisper", v.current_position);
                yield return c.W(0.08f);
            }
            yield return c.W(0.3f);
            c.PullAll(p, r + 1f, 0.5f);
            yield return c.W(0.55f);
            c.F("abyss_flame", p + new Vector3(0f, 1f), 3.2f, 0f, 0f, false, 16f);
            c.Burst(p, r);
            c.Snd("dark_pulse", p);
            c.Snd("explosion_big", p);
            c.Impact(p, 1f);
            c.Area(p, r * 0.6f, 2.5f);
            c.Crater(p, 2f);
            c.PushAll(p, r * 0.7f, 2f);
        }

        // ================================================================ Demon God · Archdemon Authority
        // A god of the Archon War: domains, overwhelming will and fury that reshapes the land.

        private static void DemonGodStyle()
        {
            Library.Style("gi_demon_god", "Archdemon Authority", 2, "divine")
                .T("dominion", "Domain of Dominion", Shape.Crowd, 6f, 12f, Dominion,
                    "A domain dome falls over an area: everyone inside slows and is struck three times.")
                .T("crushing_will", "Crushing Will", Shape.Lone, 6f, 8f, CrushingWill,
                    "A raised hand, and the foe is pressed into the earth until it cracks.")
                .T("divine_spear", "Divine Spear", Shape.Line, 9f, 9f, DivineSpear,
                    "A colossal spear of light thrown down a line; the ground splits along its path.")
                .T("calamity_fist", "Calamity Fist", Shape.Gap, 10f, 8f, CalamityFist,
                    "Leaps across the field and lands fist-first, sending an earthquake rolling on.")
                .T("fury_crown", "Fury Crown", Shape.Guard, 0f, 20f, FuryCrown,
                    "A crown of fury: untouchable for a breath, healing and empowered.")
                .T("demon_wrath", "Demon God's Wrath", Shape.Crowd, 8f, 26f, DemonWrath,
                    "Rises above the field and unleashes three waves of fury, then a land-breaking slam.", 2, 3.5f, true);
        }

        private static IEnumerator Dominion(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3f, 3.6f, 4.4f);
            Fx.Instance dome = c.F("dome", p, r * 2f, 0f, 1.8f, true, 10f, true);
            c.Snd("holy_choir", p);
            foreach (Actor u in c.Foes(p, r)) u.addStatusEffect("slowness", 4f);
            yield return c.W(0.35f);
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance ring = c.F("shock", p, r * 0.6f, 0f, 0f, false, 20f, true);
                Cast.Grow(ring, 3f);
                c.Snd("shockwave", p);
                c.Area(p, r, 0.7f);
                Cast.Shake(0.3f);
                yield return c.W(0.35f);
            }
            if (c.Lv >= 3) c.Crater(p, r * 0.5f);
        }

        private static IEnumerator CrushingWill(Cast c)
        {
            if (!c.TOk) yield break;
            Actor v = c.T;
            c.Snd("rumble", v.current_position);
            Cast.Hold(v, 1.5f);
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance ring = c.F("shock", v.current_position, 3f - i * 0.6f, 0f, 0f, false, 20f, true);
                Cast.Grow(ring, 0.3f);
                Cast.Shake(0.15f + 0.1f * i);
                yield return c.W(0.2f);
            }
            if (!CombatCore.Alive(v)) yield break;
            Vector3 p = v.current_position;
            c.F("impact", p, 2.2f, -90f, 0f, false, 24f, true);
            c.Crater(p, c.L(1.2f, 1.6f, 2f));
            for (int i = 0; i < c.Li(2, 3, 5); i++)
                c.Fissure(p, p + Cast.Rot(Vector3.right, i * 360f / c.Li(2, 3, 5) + 20f) * c.L(2f, 2.5f, 3.2f), 0.9f);
            c.Snd("crater_boom", p);
            c.Impact(p, c.L(0.6f, 0.8f, 1f));
            c.Hit(v, c.L(2f, 2.4f, 3f), "stunned", 1.5f);
        }

        private static IEnumerator DivineSpear(Cast c)
        {
            Vector3 hold = c.P + new Vector3(0f, 1.6f);
            c.Fc("jade_spear", hold, c.L(2.6f, 3f, 3.6f), new Color(1f, 0.92f, 0.55f), 0f, 0.5f);
            c.F("halo", hold, 1.6f, 0f, 0.5f, false, 16f);
            c.Snd("holy_choir");
            yield return c.W(0.45f);
            Vector3 to = Cast.Clamp(c.TP + c.Dir * c.L(4f, 5f, 6f));
            Fx.Instance spear = Fx.Play("arts/jade_spear", hold, c.L(2.6f, 3f, 3.6f), new Color(1f, 0.92f, 0.55f), 0f, 0.25f, true);
            Fx.MoveTo(spear, to, 0.2f);
            c.Snd("whoosh_heavy");
            yield return c.W(0.2f);
            c.Lane(c.P, to, 0.9f, c.L(1.6f, 1.9f, 2.3f));
            c.Fissure(c.P + c.Dir * 0.8f, to, 1.1f);
            c.F("light_cross", to, 2f, 0f, 0f, false, 18f);
            c.Snd("stone_crack", to);
            c.Impact(to, c.L(0.5f, 0.7f, 0.9f));
        }

        private static IEnumerator CalamityFist(Cast c)
        {
            if (!c.TOk) yield break;
            Vector3 land = c.Behind(c.T, -0.7f);
            c.Snd("leap");
            yield return Cast.Arc(c.A, land, c.L(3f, 3.6f, 4.2f), 0.4f, c.Hot);
            Vector3 dir = c.Dir;
            c.F("impact", c.TP, 2f, c.Ang, 0f, false, 24f, true);
            c.Snd("punch", c.TP);
            c.Hit(c.T, c.L(1.4f, 1.7f, 2.1f));
            c.Impact(c.P, c.L(0.5f, 0.7f, 0.9f));
            // An earthquake rolls on beyond the target.
            int quakes = c.Li(3, 4, 6);
            for (int i = 1; i <= quakes; i++)
            {
                Vector3 at = Cast.Clamp(c.P + dir * (i * 1.3f));
                c.F("debris", at, 1.4f, 0f, 0f, false, 18f);
                c.Dust(at, 1.3f);
                c.Snd("rumble", at);
                foreach (Actor u in c.Foes(at, 1f))
                {
                    c.Hit(u, 0.6f);
                    c.Launch(u, 1.2f, 0.1f);
                }
                yield return c.W(0.1f);
            }
            c.Fissure(c.P, Cast.Clamp(c.P + dir * quakes * 1.3f), 1f);
        }

        private static IEnumerator FuryCrown(Cast c)
        {
            Fx.Instance crown = c.F("fury_crown", c.P, 1.4f, 0f, 2.5f, true, 12f);
            Cast.Stick(crown, c.A);
            c.Snd("rampage");
            c.A.addStatusEffect("invincible", c.L(1.5f, 2f, 3f));
            c.A.addStatusEffect("powerup", c.L(6f, 8f, 10f));
            c.Heal(c.A, c.L(0.15f, 0.2f, 0.25f));
            Fx.Instance ring = c.F("shock", c.P, 1.5f, 0f, 0f, false, 20f, true);
            Cast.Grow(ring, 2f);
            yield return c.W(0.3f);
        }

        private static IEnumerator DemonWrath(Cast c)
        {
            Vector3 p = c.P;
            float r = c.L(6f, 7f, 8f);
            c.Snd("rumble", p);
            yield return Cast.Rise(c.A, 3.5f, 0.4f);
            c.F("dome", p, r * 1.6f, 0f, 2.4f, true, 10f, true, true);
            c.F("fury_crown", p, 1.6f, 0f, 2.2f, true, 12f);
            for (int w = 0; w < 3; w++)
            {
                Fx.Instance ring = c.F("shock", p, 2f, 0f, 0f, false, 16f, true);
                Cast.Grow(ring, r / 1.4f);
                c.Snd("shockwave", p);
                foreach (Actor u in c.Foes(p, r))
                {
                    c.Hit(u, 0.8f);
                    c.Launch(u, 1.5f + w * 0.6f, 0.2f);
                }
                Cast.Shake(0.5f);
                yield return c.W(0.4f);
            }
            yield return Cast.Fall(c.A, 0.12f);
            c.Burst(p, r * 1.2f);
            c.Crater(p, r * 0.5f);
            for (int i = 0; i < 6; i++) c.Fissure(p, p + Cast.Rot(Vector3.right, i * 60f + 15f) * r * 0.9f, 1.2f);
            Cast.Scorch(p, r * 0.6f, 5);
            c.Snd("crater_boom", p);
            c.Snd("explosion_big", p);
            c.Impact(p, 1f);
            c.Area(p, r * 0.7f, 2.5f);
        }
    }
}
