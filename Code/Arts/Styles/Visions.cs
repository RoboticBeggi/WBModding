using System.Collections;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>Tier 1: the seven Vision schools.</summary>
    public static partial class Styles
    {
        static partial void RegisterVisions()
        {
            AnemoVision();
            GeoVision();
            ElectroVision();
            DendroVision();
            HydroVision();
            PyroVision();
            CryoVision();
        }

        // ================================================================ Anemo · Zephyr Arts
        // Light, airborne and evasive: feathers, hops and pockets of air that throw people around.

        private static void AnemoVision()
        {
            Library.Style("gi_vision_anemo", "Zephyr Arts", 1, "anemo")
                .T("feather_fan", "Feather Fan", Shape.Line, 7f, 5f, FeatherFan,
                    "A fan of razor feathers down a line. At III they pin their targets.")
                .T("updraft_kick", "Updraft Kick", Shape.AntiAir, 2.5f, 6f, UpdraftKick,
                    "A wind spout throws the foe up; a leaping kick slams them back down.")
                .T("gale_step", "Gale Step", Shape.Gap, 9f, 5f, GaleStep,
                    "Three wind-ring hops close any distance and the last one cuts through.")
                .T("vacuum_pocket", "Vacuum Pocket", Shape.Crowd, 5f, 9f, VacuumPocket,
                    "Sucks a crowd into a pocket of air, then bursts it outwards.")
                .T("tailwind", "Tailwind Veil", Shape.Guard, 0f, 16f, Tailwind,
                    "Wraps the user in wind: dodging and fleet-footed.")
                .T("cyclone_ascent", "Cyclone Ascent", Shape.Crowd, 5f, 20f, CycloneAscent,
                    "A roaming cyclone that juggles everything it touches.", 2, 2.5f, true);
        }

        private static IEnumerator FeatherFan(Cast c)
        {
            int count = c.Li(3, 5, 7);
            float spread = c.L(30f, 44f, 56f);
            float reach = c.L(6f, 7.5f, 9f);
            c.F("wind_ring", c.Front(0.6f), 1.2f, c.Ang, 0f, false, 26f);
            c.Snd("feather_flutter");
            yield return c.W(0.12f);
            for (int i = 0; i < count; i++)
            {
                float a = -spread / 2f + spread * i / Mathf.Max(1, count - 1);
                Vector3 dir = Cast.Rot(c.Dir, a);
                Vector3 from = c.P + dir * 0.4f, to = c.P + dir * reach;
                c.Shot("feather", from, to, c.L(0.9f, 1f, 1.2f), 0.28f);
                ModClass.Instance.StartCoroutine(FeatherLands(c, from, to));
                c.Snd("whoosh_light");
                yield return c.W(0.035f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator FeatherLands(Cast c, Vector3 from, Vector3 to)
        {
            yield return c.W(0.2f);
            int hit = c.Lane(from, to, 0.55f, 0.6f, c.Lv >= 3 ? "slowness" : null);
            if (hit > 0) c.Spark(to, 0.9f);
        }

        private static IEnumerator UpdraftKick(Cast c)
        {
            Actor v = c.T;
            Vector3 p = c.TP;
            c.F("wind_spout", p + new Vector3(0f, 0.9f), c.L(1.6f, 1.9f, 2.3f), 0f, 0f, false, 20f);
            c.Snd("wind_gust", p);
            c.Hit(v, 0.5f);
            float height = c.L(2.2f, 2.8f, 3.4f);
            c.Launch(v, height, 0.5f);
            yield return c.W(0.12f);
            // Leap up after them.
            c.Snd("leap");
            yield return Cast.Arc(c.A, c.Behind(v, -0.5f), height + 0.4f, 0.28f, c.Hot);
            Cast.SetHeight(c.A, height + 0.2f);
            yield return c.W(0.05f);
            Vector3 at = (Vector3)v.current_position;
            c.F("impact", at, c.L(1.4f, 1.7f, 2.1f), -70f, 0f, false, 26f, true);
            c.F("wind_ring", at, 1.3f, 0f, 0f, false, 24f);
            c.Snd("kick", at);
            c.Hit(v, 1.1f);
            yield return Cast.Fall(c.A, 0.2f);
            yield return c.W(0.25f);
            if (c.Lv >= 2)
            {
                c.Dust(v.current_position, 1.6f);
                c.Crater(v.current_position, c.L(0f, 1f, 1.6f));
                c.Impact(v.current_position, c.L(0.3f, 0.45f, 0.7f));
            }
        }

        private static IEnumerator GaleStep(Cast c)
        {
            Vector3 start = c.P;
            Vector3 end = c.Behind(c.T, 1f);
            int hops = 3;
            for (int i = 1; i <= hops; i++)
            {
                Vector3 hop = Vector3.Lerp(start, end, i / (float)hops) +
                              Cast.Rot(c.Dir, 90f) * (i % 2 == 0 ? 0.6f : -0.6f) * (i < hops ? 1f : 0f);
                c.F("wind_ring", c.P, 1.2f, 0f, 0f, false, 30f);
                c.Snd("dash");
                yield return c.Dash(hop, 0.07f);
                c.Dust(c.P, 0.8f);
                yield return c.W(0.05f);
            }
            // The last hop went through the target: cut along the path.
            c.Beam("iai", start, c.P, 0.5f, 0.25f, true, false);
            c.Snd("slash_light");
            c.Lane(Vector3.Lerp(start, c.P, 0.5f), c.P, 0.7f, c.L(1f, 1.2f, 1.5f));
            if (c.TOk) c.Push(c.T, c.P - start, c.L(0.8f, 1.2f, 1.8f));
            yield return c.W(0.2f);
        }

        private static IEnumerator VacuumPocket(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2.2f, 2.8f, 3.4f);
            int pops = c.Li(1, 1, 2);
            for (int n = 0; n < pops; n++)
            {
                c.F("vacuum", p, r * 1.2f, 0f, 0.6f, false, 18f);
                c.Snd("vortex", p);
                c.PullAll(p, r + 1f, 0.4f);
                yield return c.W(0.5f);
                Fx.Instance ring = c.F("wind_ring", p, r, 0f, 0f, false, 20f);
                Cast.Grow(ring, 2f);
                c.Burst(p, r * 1.2f);
                c.Snd("shockwave", p);
                c.Area(p, r, 1f);
                c.PushAll(p, r + 0.5f, c.L(1.5f, 2f, 2.6f));
                c.Impact(p, c.L(0.3f, 0.5f, 0.7f));
                yield return c.W(0.3f);
            }
        }

        private static IEnumerator Tailwind(Cast c)
        {
            Fx.Instance veil = c.F("wind_ring", c.P, 1.6f, 0f, c.L(3f, 4f, 5f), true, 14f);
            Cast.Stick(veil, c.A);
            for (int i = 0; i < 4; i++) c.Shot("feather", c.P + Cast.Rand(1.2f), c.P + Cast.Rand(0.3f), 0.7f, 0.25f);
            c.Snd("wind_howl");
            c.A.addStatusEffect("dodge", c.L(4f, 6f, 8f));
            c.A.addStatusEffect("dash", c.L(3f, 4f, 5f));
            if (c.Lv >= 3) c.A.addStatusEffect("shield", 4f);
            yield return c.W(0.3f);
        }

        private static IEnumerator CycloneAscent(Cast c)
        {
            Vector3 p = c.Front(1.5f);
            Vector3 goal = c.TP;
            float size = c.L(2.6f, 3.2f, 4f);
            Fx.Instance storm = c.F("cyclone", p, size, 0f, 2.8f, true, 14f);
            Fx.MoveTo(storm, goal, 2.6f, false);
            c.Snd("wind_howl", p);
            float t = 0f;
            while (t < 2.6f)
            {
                Vector3 at = Vector3.Lerp(p, goal, t / 2.6f);
                foreach (Actor u in c.Foes(at, size * 0.45f))
                {
                    c.Hit(u, 0.35f);
                    if (u.position_height < 0.5f) c.Launch(u, Random.Range(1.5f, 3f), 0.35f);
                }
                c.Dust(at + Cast.Rand(0.8f), 1f);
                if (Random.value < 0.4f) c.F("leaves", at + Cast.Rand(1f), 1f, Random.Range(0f, 360f), 0f, false, 18f);
                c.Snd("vortex", at);
                t += 0.25f;
                yield return c.W(0.25f);
            }
            c.Burst(goal, size);
            c.Area(goal, size * 0.6f, 1.4f);
            c.PushAll(goal, size * 0.7f, 2.5f);
            c.Impact(goal, c.L(0.5f, 0.7f, 1f));
        }

        // ================================================================ Geo · Bedrock Arts
        // Heavy and grounded: fists of stone, pillars, meteors and a land that cracks under every blow.

        private static void GeoVision()
        {
            Library.Style("gi_vision_geo", "Bedrock Arts", 1, "geo")
                .T("stone_barrage", "Stone Fist Barrage", Shape.Lone, 2f, 5f, GeoStoneBarrage,
                    "Plants their feet and hammers the foe with stone fists, then shoves them away.")
                .T("pillar_rise", "Pillar Procession", Shape.Line, 8f, 7f, GeoPillarRise,
                    "Pillars of rock erupt one after another down a line, tossing everyone on it.")
                .T("meteor_drop", "Meteor Drop", Shape.Gap, 8f, 10f, GeoMeteorDrop,
                    "Calls rocks out of the sky onto a far foe; each impact cracks the ground.")
                .T("jade_lattice", "Jade Lattice", Shape.Guard, 0f, 16f, GeoLattice,
                    "A hexagonal lattice of jade wraps the user. At III it bursts into shards.")
                .T("fault_line", "Fault Line Stomp", Shape.Crowd, 3f, 9f, GeoFaultLine,
                    "Leaps and stomps: radial cracks tear outward and stun whoever stands on them.")
                .T("mountain_crown", "Mountain Crown", Shape.Crowd, 5f, 20f, GeoMountainCrown,
                    "A ring of pillars cages the foes, then collapses inward on them.", 2, 2.5f, true);
        }

        private static IEnumerator GeoStoneBarrage(Cast c)
        {
            Actor v = c.T;
            if (c.Dist > 1.3f) yield return c.Dash(c.Behind(v, -0.9f), 0.1f, false);
            c.Dust(c.P, 1f);
            Cast.Hold(v, 2f);
            int punches = c.Li(4, 6, 8);
            for (int i = 0; i < punches && c.TOk; i++)
            {
                Vector3 off = Cast.Rot(c.Dir, 90f) * (i % 2 == 0 ? 0.25f : -0.25f);
                c.F("stone_fist", c.P + c.Dir * 0.6f + off, 0.9f, c.Ang, 0f, false, 30f);
                c.F("debris", c.TP, 0.8f, 0f, 0f, false, 24f);
                c.Snd(i % 2 == 0 ? "punch" : "boulder_hit", c.TP);
                c.Hit(v, 0.3f);
                Cast.Shake(0.1f);
                yield return c.W(0.07f);
            }
            if (!c.TOk) yield break;
            c.F("stone_fist", c.P + c.Dir * 0.7f, 1.6f, c.Ang, 0f, false, 24f);
            c.Burst(c.TP, 1.6f);
            c.Snd("stone_crack", c.TP);
            c.Hit(v, 1f);
            c.Impact(c.TP, c.L(0.3f, 0.5f, 0.75f));
            yield return c.Knock(v, c.Dir, c.L(2f, 2.6f, 3.4f), 0.3f);
            if (c.Lv >= 3) c.Fissure(c.P, c.TP, 0.8f);
        }

        private static IEnumerator GeoPillarRise(Cast c)
        {
            Vector3 start = c.P, dir = c.Dir;
            int count = c.Li(5, 7, 9);
            c.Snd("stone_crack");
            c.Dust(c.P, 1.2f);
            for (int i = 1; i <= count; i++)
            {
                Vector3 p = Cast.Clamp(start + dir * (i * 1.05f));
                c.F("rock_pillar", p + new Vector3(0f, 0.8f), c.L(1.1f, 1.3f, 1.5f), 0f, 0f, false, 20f);
                c.F("debris", p, 1f, 0f, 0f, false, 20f);
                c.Snd("stone_rise", p);
                foreach (Actor u in c.Foes(p, 0.8f))
                {
                    c.Hit(u, 0.7f);
                    c.Launch(u, c.L(1.2f, 1.6f, 2.2f), 0.2f);
                }
                Cast.Shake(0.12f);
                yield return c.W(0.07f);
            }
            if (c.Lv >= 2) c.Fissure(start + dir * 0.8f, start + dir * (count * 1.05f), 0.9f);
            yield return c.W(0.3f);
        }

        private static IEnumerator GeoMeteorDrop(Cast c)
        {
            int rocks = c.Li(1, 2, 3);
            c.F("fx:aura_flame", c.P, 1.3f, 0f, 0.5f, true, 16f, true);
            c.Snd("rumble");
            yield return c.W(0.3f);
            for (int i = 0; i < rocks; i++)
            {
                Vector3 land = c.TP + (i == 0 ? Vector3.zero : Cast.Rand(1.6f));
                Vector3 sky = land + new Vector3(-3.5f, 5f);
                c.F("shock", land, 1f, 0f, 0.4f, false, 10f, true);
                c.Shot("geo_meteor", sky, land, c.L(1.4f, 1.7f, 2.1f), 0.35f, false, false);
                c.Snd("whoosh_heavy", land);
                yield return c.W(0.35f);
                c.Burst(land, c.L(2f, 2.4f, 3f));
                c.Crater(land, c.L(1.2f, 1.5f, 2f));
                c.Fissure(land, land + Cast.Rot(Vector3.right, Random.Range(0f, 360f)) * c.L(2f, 2.5f, 3.5f));
                c.Snd("crater_boom", land);
                c.Area(land, c.L(1.6f, 2f, 2.4f), 1.3f, "stunned", 1f);
                c.Impact(land, c.L(0.45f, 0.6f, 0.85f));
                yield return c.W(0.15f);
            }
        }

        private static IEnumerator GeoLattice(Cast c)
        {
            Fx.Instance shield = c.F("lattice", c.P, c.L(1.8f, 2.1f, 2.5f), 0f, c.L(4f, 5f, 6f), true, 12f);
            Cast.Stick(shield, c.A);
            c.Snd("crystal_chime");
            c.A.addStatusEffect("shield", c.L(4f, 5f, 6f));
            yield return c.W(c.L(0.3f, 0.3f, 1.4f));
            if (c.Lv < 3) yield break;
            // Shards fly off the lattice into everyone nearby.
            foreach (Actor u in c.Foes(c.P, 3.5f))
            {
                c.Shot("geo_meteor", c.P, u.current_position, 0.7f, 0.18f);
                c.Hit(u, 0.8f);
            }
            c.Snd("glass_break");
        }

        private static IEnumerator GeoFaultLine(Cast c)
        {
            c.Snd("leap");
            yield return Cast.Rise(c.A, c.L(1.6f, 2f, 2.6f), 0.22f);
            yield return c.W(0.08f);
            yield return Cast.Fall(c.A, 0.1f);
            Vector3 p = c.P;
            c.Burst(p, c.L(2f, 2.6f, 3.2f));
            c.Snd("slam", p);
            c.Snd("stone_crack", p);
            c.Impact(p, c.L(0.5f, 0.7f, 0.9f));
            int cracks = c.Li(3, 4, 6);
            float len = c.L(3f, 4f, 5f);
            float base_angle = Random.Range(0f, 360f);
            for (int i = 0; i < cracks; i++)
            {
                Vector3 d = Cast.Rot(Vector3.right, base_angle + 360f / cracks * i);
                Vector3 end = p + d * len;
                c.Fissure(p, end, 1f);
                c.F("debris", Vector3.Lerp(p, end, 0.6f), 1f, 0f, 0f, false, 18f);
                c.Lane(p, end, 0.6f, 1f, "stunned");
            }
            c.Area(p, 1.4f, 0.8f);
            c.Crater(p, c.L(1f, 1.4f, 1.9f));
            yield return c.W(0.3f);
        }

        private static IEnumerator GeoMountainCrown(Cast c)
        {
            Vector3 center = c.TP;
            float r = c.L(2.4f, 2.8f, 3.4f);
            int n = c.Li(6, 8, 10);
            c.Snd("rumble", center);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = center + Cast.Rot(Vector3.right, 360f / n * i) * r;
                c.F("rock_pillar", p + new Vector3(0f, 0.9f), 1.5f, 0f, 1.6f, false, 14f);
                c.F("debris", p, 0.9f, 0f, 0f, false, 20f);
                c.Snd("stone_rise", p);
                Cast.Shake(0.1f);
                yield return c.W(0.05f);
            }
            foreach (Actor u in c.Foes(center, r)) Cast.Hold(u, 1.6f);
            c.PullAll(center, r + 0.5f, 0.5f);
            yield return c.W(0.8f);
            // The crown falls in.
            for (int i = 0; i < n; i++)
            {
                Vector3 p = center + Cast.Rot(Vector3.right, 360f / n * i) * r;
                c.Shot("geo_meteor", p + new Vector3(0f, 1.5f), center + Cast.Rand(0.6f), 1.3f, 0.22f);
            }
            c.Snd("whoosh_heavy", center);
            yield return c.W(0.22f);
            c.Burst(center, r * 2f);
            c.F("debris", center, r * 1.6f, 0f, 0f, false, 14f);
            c.Crater(center, r);
            c.Snd("crater_boom", center);
            c.Area(center, r + 0.4f, 3f);
            c.Impact(center, 1f);
            for (int i = 0; i < 4; i++)
                c.Fissure(center, center + Cast.Rot(Vector3.right, 45f + 90f * i) * (r + 2f), 1.1f);
        }

        // ================================================================ Electro · Thunderclap Iai
        // Stillness, then a flash: drawn cuts, lightning hops and traps that strike from a clear sky.

        private static void ElectroVision()
        {
            Library.Style("gi_vision_electro", "Thunderclap Iai", 1, "electro")
                .T("iai_draw", "Iai Draw", Shape.Lone, 5f, 6f, ElectroIai,
                    "A held breath, a flash, and they are already behind the foe. The cut lands a beat later.")
                .T("chain_bolt", "Chain Bolt", Shape.Crowd, 6f, 7f, ElectroChain,
                    "Lightning leaps from foe to foe, 3 to 7 times.")
                .T("sigil_trap", "Thunder Sigil", Shape.Crowd, 6f, 9f, ElectroSigil,
                    "Brands a sigil under the foes; bolts fall on it from a clear sky.")
                .T("cross_flurry", "Starburst Flurry", Shape.Execute, 3f, 8f, ElectroFlurry,
                    "Blinks around a wounded foe from every side, carving a star of cuts.")
                .T("static_step", "Static Step", Shape.Gap, 10f, 5f, ElectroStaticStep,
                    "Travels as a zigzag of lightning and arrives with a stunning jolt.")
                .T("storm_eye", "Eye of the Storm", Shape.Crowd, 6f, 20f, ElectroStormEye,
                    "Opens a storm's eye over the battlefield that rains bolts on every foe.", 2, 2.5f, true);
        }

        private static IEnumerator ElectroIai(Cast c)
        {
            Actor v = c.T;
            c.F("sigil", c.P, 1.2f, 0f, 0.4f, true, 20f, back: true);
            c.Snd("static_buzz");
            yield return c.W(c.L(0.4f, 0.32f, 0.25f));
            Vector3 from = c.P;
            Vector3 to = c.Behind(v, 1.2f);
            c.Snd("iai_draw");
            c.Blink(to, "shock");
            c.Beam("iai", from, c.P, c.L(0.5f, 0.6f, 0.8f), 0.4f, false, false);
            CombatCore.Face(c.A, from);
            yield return c.W(0.25f);
            if (!c.TOk) yield break;
            c.F("cross_cut", c.TP, c.L(1.4f, 1.7f, 2f), c.Ang, 0f, false, 26f);
            c.Snd("slash_heavy", c.TP);
            c.Lane(from, c.P, 0.6f, c.L(1.2f, 1.5f, 1.8f));
            c.Impact(c.TP, c.L(0.35f, 0.5f, 0.8f));
            if (c.Lv >= 3 && c.TOk)
            {
                yield return c.W(0.15f);
                c.Beam("iai", c.TP + new Vector3(-1.2f, 1.2f), c.TP + new Vector3(1.2f, -1.2f), 0.7f, 0.3f, false, false);
                c.Snd("zap", c.TP);
                c.Hit(v, 1f, "stunned", 1f);
            }
        }

        private static IEnumerator ElectroChain(Cast c)
        {
            int jumps = c.Li(3, 5, 7);
            Vector3 from = c.P + new Vector3(0f, 0.3f);
            Actor cur = c.T;
            var struck = new System.Collections.Generic.HashSet<Actor>();
            for (int i = 0; i < jumps && CombatCore.Alive(cur); i++)
            {
                struck.Add(cur);
                Vector3 to = cur.current_position;
                c.Beam("bolt", from, to, 0.7f, 0.18f);
                c.Spark(to, 1f);
                c.Snd("zap", to);
                c.Hit(cur, 0.7f, c.Lv >= 2 ? "stunned" : null, 0.5f);
                from = to;
                yield return c.W(0.09f);
                Actor next = null;
                float best = 16f;
                foreach (Actor u in c.Foes(to, 4f))
                {
                    if (struck.Contains(u)) continue;
                    float d = ((Vector3)u.current_position - to).sqrMagnitude;
                    if (d < best) { best = d; next = u; }
                }
                cur = next;
            }
            c.Snd("thunder_crack", from);
        }

        private static IEnumerator ElectroSigil(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(1.8f, 2.3f, 2.8f);
            Fx.Instance mark = c.F("sigil", p, r * 2f, 0f, 1.2f, true, 16f, back: true);
            Cast.Grow(mark, 1.15f);
            c.Snd("chant", p);
            yield return c.W(0.6f);
            int bolts = c.Li(3, 5, 7);
            for (int i = 0; i < bolts; i++)
            {
                Vector3 at = p + Cast.Rand(r * 0.9f);
                c.Beam("bolt", at + new Vector3(0.4f, 7f), at, 1f, 0.15f);
                c.F("shock", at, 1.2f, 0f, 0f, false, 26f, true);
                c.Snd("thunder_crack", at);
                c.Area(at, 1f, 0.8f, "stunned", 0.6f);
                Cast.Shake(0.15f);
                yield return c.W(0.08f);
            }
            c.Area(p, r, 0.6f);
            if (c.Lv >= 2) c.Crater(p, r * 0.6f);
        }

        private static IEnumerator ElectroFlurry(Cast c)
        {
            Actor v = c.T;
            Cast.Hold(v, 2f);
            int cuts = c.Li(4, 5, 6);
            for (int i = 0; i < cuts && c.TOk; i++)
            {
                float a = 360f / cuts * i + 20f;
                Vector3 side = c.TP + Cast.Rot(Vector3.right, a) * 1.1f;
                Vector3 across = c.TP - Cast.Rot(Vector3.right, a) * 1.1f;
                c.Blink(side, "shock");
                CombatCore.Face(c.A, c.TP);
                c.Beam("iai", side, across, 0.45f, 0.5f, false, false);
                c.Snd("slash_light", c.TP);
                c.Hit(v, 0.45f);
                yield return c.W(0.07f);
            }
            if (!c.TOk) yield break;
            yield return c.W(0.15f);
            c.F("cross_cut", c.TP, c.L(2f, 2.4f, 2.9f), 0f, 0f, false, 22f);
            c.Spark(c.TP, 2f);
            c.Snd("thunder_crack", c.TP);
            c.Hit(v, c.L(1.4f, 1.8f, 2.4f));
            c.Impact(c.TP, c.L(0.4f, 0.6f, 0.9f));
        }

        private static IEnumerator ElectroStaticStep(Cast c)
        {
            Vector3 start = c.P, end = c.Behind(c.T, -1f);
            Vector3 side = Cast.Rot(c.Dir, 90f);
            Vector3 prev = start;
            for (int i = 1; i <= 3; i++)
            {
                Vector3 p = Vector3.Lerp(start, end, i / 3f) + side * (i == 3 ? 0f : (i % 2 == 0 ? 1.1f : -1.1f));
                c.Beam("bolt", prev, p, 0.8f, 0.2f);
                Cast.Place(c.A, p);
                c.F("shock", p, 0.9f, 0f, 0f, false, 30f, true);
                c.Snd("zap", p);
                c.Lane(prev, p, 0.5f, 0.4f);
                prev = p;
                yield return c.W(0.06f);
            }
            if (!c.TOk) yield break;
            c.Beam("bolt", c.P, c.TP, 1f, 0.2f);
            c.Spark(c.TP, 1.4f);
            c.Snd("static_buzz", c.TP);
            c.Hit(c.T, c.L(0.9f, 1.1f, 1.4f), "stunned", c.L(0.6f, 1f, 1.5f));
        }

        private static IEnumerator ElectroStormEye(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(3.2f, 4f, 5f);
            Fx.Instance eye = c.F("storm_eye", p + new Vector3(0f, 4f), r * 1.5f, 0f, 3f, true, 12f);
            c.Snd("rumble", p);
            yield return c.W(0.5f);
            float t = 0f;
            while (t < 2.2f)
            {
                var foes = c.Foes(p, r);
                Vector3 at = foes.Count > 0 && Random.value < 0.7f
                    ? (Vector3)foes[Random.Range(0, foes.Count)].current_position
                    : p + Cast.Rand(r);
                c.Beam("bolt", p + new Vector3(Random.Range(-1f, 1f), 4f), at, 1.1f, 0.14f);
                c.F("shock", at, 1.1f, 0f, 0f, false, 28f, true);
                c.Snd("thunder_crack", at);
                c.Area(at, 0.9f, 0.55f);
                Cast.Shake(0.12f);
                t += 0.12f;
                yield return c.W(0.12f);
            }
            c.Beam("bolt", p + new Vector3(0f, 4f), p, 2.2f, 0.3f);
            c.Burst(p, r * 1.2f);
            c.Crater(p, 1.6f);
            c.Snd("explosion_big", p);
            c.Area(p, r, 1.6f, "stunned", 1f);
            c.Impact(p, 1f);
        }

        // ================================================================ Dendro · Verdant Arts
        // Patient and rooted: whips that drag, seeds that wait, roots that hold and blossoms that heal.

        private static void DendroVision()
        {
            Library.Style("gi_vision_dendro", "Verdant Arts", 1, "dendro")
                .T("thorn_whip", "Thorn Lasso", Shape.Lone, 5f, 5f, DendroThornWhip,
                    "Lashes a thorned whip around the foe, drags them in and kicks them away.")
                .T("seed_volley", "Seed Volley", Shape.Line, 7f, 6f, DendroSeeds,
                    "Plants a row of seeds down a line; a moment later they all burst into bloom.")
                .T("root_snare", "Root Snare", Shape.Crowd, 6f, 9f, DendroRoots,
                    "Roots race outward through the ground and bind every foe they reach.")
                .T("healing_bloom", "Healing Bloom", Shape.Support, 0f, 14f, DendroHealing,
                    "Flowers open at hurt allies' feet and mend them.")
                .T("leaf_storm", "Leaf Gale Rider", Shape.Gap, 8f, 7f, DendroLeafStorm,
                    "Rides a stream of razor leaves to a far foe, cutting all along the way.")
                .T("world_tree", "World Tree Sprout", Shape.Crowd, 5f, 20f, DendroWorldTree,
                    "A great tree bursts from the ground: falling leaves cut foes and its roots hold them.", 2, 2.5f, true);
        }

        private static IEnumerator DendroThornWhip(Cast c)
        {
            Actor v = c.T;
            c.Snd("vine_lash");
            c.Beam("thorn_whip", c.P, c.TP, 0.7f, 0.35f);
            yield return c.W(0.1f);
            if (!c.TOk) yield break;
            c.Hit(v, 0.6f, "poisoned", 3f);
            // Reel them in.
            Vector3 pull = c.P - c.TP;
            if (pull.magnitude > 1.2f) yield return c.Knock(v, pull, pull.magnitude - 0.9f, 0.22f);
            if (!c.TOk) yield break;
            c.F("leaves", c.TP, 1f, 0f, 0f, false, 22f);
            c.F("impact", c.TP, 1.3f, c.Ang, 0f, false, 26f, true);
            c.Snd("kick", c.TP);
            c.Hit(v, c.L(0.9f, 1.1f, 1.4f));
            yield return c.Knock(v, c.Dir, c.L(1.8f, 2.4f, 3f), 0.25f);
        }

        private static IEnumerator DendroSeeds(Cast c)
        {
            int n = c.Li(3, 4, 5);
            var spots = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                spots[i] = Cast.Clamp(c.P + c.Dir * (1.5f + i * 1.3f));
                c.Shot("seed", c.P, spots[i], 0.6f, 0.18f);
                c.Snd("whoosh_light");
                yield return c.W(0.05f);
            }
            for (int i = 0; i < n; i++) c.F("seed", spots[i], 0.7f, 0f, 0.7f, true, 10f);
            c.Snd("root_creak");
            yield return c.W(c.L(0.75f, 0.65f, 0.55f));
            for (int i = 0; i < n; i++)
            {
                c.F("flower", spots[i], c.L(1.3f, 1.6f, 2f), 0f, 0f, false, 18f);
                c.F("leaves", spots[i], 1.2f, Random.Range(0f, 360f), 0f, false, 18f);
                c.Snd("bloom_pop", spots[i]);
                c.Area(spots[i], c.L(1f, 1.2f, 1.5f), 0.9f, c.Lv >= 3 ? "poisoned" : null, 4f);
                yield return c.W(0.06f);
            }
        }

        private static IEnumerator DendroRoots(Cast c)
        {
            float r = c.L(3f, 4f, 5f);
            Vector3 p = c.P;
            c.Snd("root_creak", p);
            for (int ring = 1; ring <= 3; ring++)
            {
                float rr = r * ring / 3f;
                int n = 4 + ring * 2;
                for (int i = 0; i < n; i++)
                {
                    float a = 360f / n * i + ring * 15f;
                    Vector3 at = p + Cast.Rot(Vector3.right, a) * rr;
                    c.F("root", at, 1f, a, 0f, false, 18f, back: true);
                }
                yield return c.W(0.09f);
            }
            float hold = c.L(1.5f, 2f, 3f);
            foreach (Actor u in c.Foes(p, r))
            {
                Cast.Hold(u, hold);
                c.F("root", u.current_position, 1.1f, 90f, hold, true, 10f);
                c.Hit(u, 0.6f, "poisoned", 4f);
            }
            c.Snd("vine_lash", p);
            yield return c.W(0.3f);
        }

        private static IEnumerator DendroHealing(Cast c)
        {
            float amount = c.L(0.1f, 0.15f, 0.22f);
            int shown = 0;
            foreach (Actor ally in c.Allies(5f))
            {
                if (ally.getHealth() >= ally.getMaxHealth()) continue;
                c.F("flower", ally.current_position, 1.4f, 0f, 0f, false, 14f);
                c.F("leaves", ally.current_position, 1.2f, 0f, 0f, false, 14f);
                c.Heal(ally, amount);
                if (++shown >= 10) break;
            }
            c.F("flower", c.P, 1.8f, 0f, 0f, false, 14f);
            c.Heal(c.A, amount);
            c.Snd("bloom_pop");
            yield return c.W(0.4f);
        }

        private static IEnumerator DendroLeafStorm(Cast c)
        {
            Vector3 start = c.P, end = c.Behind(c.T, 1f);
            float t = 0f, dur = 0.4f;
            Fx.Instance stream = c.F("leaves", start, 1.6f, 0f, dur + 0.2f, true, 22f);
            Cast.Stick(stream, c.A);
            c.Snd("leaf_rustle");
            ModClass.Instance.StartCoroutine(c.Dash(end, dur));
            Vector3 last = start;
            while (t < dur)
            {
                t += 0.08f;
                yield return c.W(0.08f);
                c.Lane(last, c.P, 0.8f, 0.35f);
                c.F("leaves", c.P + Cast.Rand(0.4f), 1f, Random.Range(0f, 360f), 0f, false, 24f);
                last = c.P;
            }
            c.Snd("slash_light", c.P);
            if (c.Lv >= 2)
            {
                c.Beam("thorn_whip", start, c.P, 0.6f, 0.3f);
                c.Lane(start, c.P, 0.8f, c.L(0f, 0.6f, 1f));
            }
        }

        private static IEnumerator DendroWorldTree(Cast c)
        {
            Vector3 p = c.Front(1.2f);
            float r = c.L(3.5f, 4.2f, 5f);
            c.Snd("root_creak", p);
            c.Dust(p, 2f);
            c.F("world_tree", p + new Vector3(0f, 1.6f), c.L(3.2f, 3.8f, 4.5f), 0f, 3.2f, false, 8f);
            Cast.Shake(0.4f);
            yield return c.W(0.6f);
            foreach (Actor u in c.Foes(p, r))
            {
                Cast.Hold(u, 2f);
                c.F("root", u.current_position, 1.1f, 90f, 2f, true, 10f);
            }
            float t = 0f;
            while (t < 2f)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 at = p + Cast.Rand(r);
                    c.Shot("leaves", at + new Vector3(0.6f, 2f), at, 0.9f, 0.25f, false, false);
                    c.Area(at, 0.8f, 0.3f);
                }
                c.Snd("leaf_rustle", p);
                t += 0.3f;
                yield return c.W(0.3f);
            }
            foreach (Actor ally in c.Allies(r))
            {
                c.F("flower", ally.current_position, 1.2f, 0f, 0f, false, 14f);
                c.Heal(ally, c.L(0.15f, 0.2f, 0.3f));
            }
            c.F("flower", p, r, 0f, 0f, false, 12f);
            c.Snd("bloom_pop", p);
            c.Area(p, r, 1.5f);
            c.Impact(p, c.L(0.5f, 0.7f, 1f));
        }

        // ================================================================ Hydro · Tidecaller Arts
        // Flowing and relentless: lashing whips, waves to ride, prisons of water and a serpent of the deep.

        private static void HydroVision()
        {
            Library.Style("gi_vision_hydro", "Tidecaller Arts", 1, "hydro")
                .T("water_whip", "Crosscurrent Lash", Shape.Lone, 4f, 5f, HydroWhip,
                    "Two whips cross over the foe, then a third lifts and slams them.")
                .T("riptide_rush", "Riptide Rush", Shape.Gap, 9f, 7f, HydroRiptide,
                    "Surfs a wave into the fight, bowling over everyone in its path.")
                .T("bubble_prison", "Bubble Prison", Shape.Execute, 5f, 9f, HydroBubble,
                    "Seals the foe in a bubble, floats it up, and bursts it.")
                .T("spring_font", "Spring Font", Shape.Support, 0f, 14f, HydroSpring,
                    "A healing geyser bursts under the most hurt ally and tosses foes away.")
                .T("droplet_rain", "Droplet Barrage", Shape.Crowd, 7f, 8f, HydroDroplets,
                    "Flings water skyward that comes back down as a hail of piercing droplets.")
                .T("leviathan", "Leviathan's Rise", Shape.Line, 8f, 20f, HydroLeviathan,
                    "A serpent of water breaches the ground three times down a line.", 2, 2.5f, true);
        }

        private static IEnumerator HydroWhip(Cast c)
        {
            Actor v = c.T;
            Vector3 side = Cast.Rot(c.Dir, 90f) * 0.9f;
            c.Snd("water_whip");
            c.Beam("water_whip", c.P + side, c.TP - side, 0.7f, 0.25f);
            c.Hit(v, 0.5f);
            yield return c.W(0.14f);
            if (!c.TOk) yield break;
            c.Beam("water_whip", c.P - side, c.TP + side, 0.7f, 0.25f);
            c.Snd("water_whip");
            c.Hit(v, 0.5f);
            yield return c.W(0.14f);
            if (!c.TOk) yield break;
            c.Beam("water_whip", c.P, c.TP, 0.9f, 0.3f);
            c.Launch(v, c.L(1.4f, 1.8f, 2.4f), 0.15f);
            yield return c.W(0.45f);
            if (!c.TOk) yield break;
            c.F("riptide", c.TP, 1.5f, 0f, 0f, false, 18f);
            c.Snd("splash_big", c.TP);
            c.Hit(v, c.L(1f, 1.3f, 1.7f));
            c.Impact(c.TP, c.L(0.3f, 0.45f, 0.7f));
        }

        private static IEnumerator HydroRiptide(Cast c)
        {
            Vector3 start = c.P, end = c.Behind(c.T, -0.8f);
            Vector3 dir = (end - start).normalized;
            float dur = 0.45f;
            Fx.Instance wave = c.F("riptide", start, c.L(1.6f, 1.9f, 2.3f), 0f, dur + 0.1f, true, 18f);
            Fx.MoveTo(wave, end, dur, false);
            c.Snd("wave_crash");
            ModClass.Instance.StartCoroutine(c.Dash(end, dur, false));
            var tossed = new System.Collections.Generic.HashSet<Actor>();
            float t = 0f;
            while (t < dur)
            {
                t += 0.07f;
                yield return c.W(0.07f);
                c.F("droplet", c.P + Cast.Rand(0.5f), 0.6f, Random.Range(0f, 360f), 0.3f);
                foreach (Actor u in c.Foes(c.P, 1.1f))
                {
                    if (!tossed.Add(u)) continue;
                    c.Hit(u, 0.8f);
                    Vector3 away = Cast.Rot(dir, Vector3.Cross(dir, (Vector3)u.current_position - c.P).z > 0 ? 90f : -90f);
                    c.Push(u, away, c.L(1.5f, 2f, 2.6f));
                }
            }
            c.F("riptide", c.P + dir * 0.6f, 1.8f, 0f, 0f, false, 18f);
            c.Snd("splash_big", c.P);
        }

        private static IEnumerator HydroBubble(Cast c)
        {
            Actor v = c.T;
            c.Shot("bubble", c.P, c.TP, 0.8f, 0.2f);
            c.Snd("bubble_pop");
            yield return c.W(0.2f);
            if (!c.TOk) yield break;
            float hang = c.L(0.9f, 1.2f, 1.5f);
            Fx.Instance prison = c.F("bubble", c.TP, c.L(1.4f, 1.6f, 1.9f), 0f, hang + 0.4f, true, 10f);
            Cast.Stick(prison, v);
            c.Launch(v, c.L(1.8f, 2.2f, 2.8f), hang);
            int ticks = c.Li(2, 3, 4);
            for (int i = 0; i < ticks && c.TOk; i++)
            {
                yield return c.W(hang / ticks);
                c.Hit(v, 0.25f);
            }
            if (!c.TOk) yield break;
            c.F("riptide", c.TP, 1.6f, 0f, 0f, false, 22f);
            for (int i = 0; i < 6; i++)
                c.Shot("droplet", c.TP, c.TP + Cast.Rot(Vector3.right, 60f * i) * 1.6f, 0.5f, 0.2f);
            c.Snd("splash_big", c.TP);
            c.Hit(v, c.L(1.2f, 1.6f, 2.2f));
            c.Area(c.TP, 1.4f, 0.5f);
        }

        private static IEnumerator HydroSpring(Cast c)
        {
            Actor ward = c.A;
            float worst = 2f;
            foreach (Actor ally in c.Allies(6f))
            {
                float ratio = ally.getHealth() / (float)Mathf.Max(1, ally.getMaxHealth());
                if (ratio < worst) { worst = ratio; ward = ally; }
            }
            Vector3 p = ward.current_position;
            c.F("spring", p + new Vector3(0f, 0.8f), c.L(1.4f, 1.7f, 2f), 0f, 1f, true, 16f);
            c.Snd("splash_big", p);
            foreach (Actor u in c.Foes(p, 1.8f)) c.Launch(u, 1.4f, 0.2f);
            foreach (Actor ally in c.Allies(6f))
            {
                if (Vector2.Distance(ally.current_position, p) < c.L(1.5f, 2.2f, 3f)) c.Heal(ally, c.L(0.12f, 0.18f, 0.25f));
            }
            c.Heal(ward, c.L(0.08f, 0.1f, 0.12f));
            yield return c.W(0.5f);
        }

        private static IEnumerator HydroDroplets(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(1.8f, 2.3f, 2.9f);
            for (int i = 0; i < 5; i++) c.Shot("droplet", c.P, c.P + new Vector3(Random.Range(-0.6f, 0.6f), 4f), 0.7f, 0.2f);
            c.Snd("water_whip");
            yield return c.W(0.35f);
            int n = c.Li(10, 14, 20);
            for (int i = 0; i < n; i++)
            {
                Vector3 at = p + Cast.Rand(r);
                c.Shot("droplet", at + new Vector3(0.5f, 4f), at, 0.6f, 0.15f);
                if (i % 3 == 0) c.Snd("bubble_pop", at);
                ModClass.Instance.StartCoroutine(HydroDropletLand(c, at));
                yield return c.W(0.035f);
            }
            yield return c.W(0.2f);
            c.F("riptide", p, r * 1.2f, 0f, 0f, false, 16f);
            c.Snd("splash_big", p);
        }

        private static IEnumerator HydroDropletLand(Cast c, Vector3 at)
        {
            yield return c.W(0.15f);
            c.Area(at, 0.6f, 0.3f, c.Lv >= 3 ? "slowness" : null);
        }

        private static IEnumerator HydroLeviathan(Cast c)
        {
            Vector3 start = c.P, dir = c.Dir;
            float step = c.L(2.4f, 2.8f, 3.2f);
            c.Snd("rumble");
            c.F("spring", c.P + new Vector3(0f, 0.6f), 1.2f, 0f, 0f, false, 18f);
            yield return c.W(0.3f);
            for (int i = 1; i <= 3; i++)
            {
                Vector3 p = Cast.Clamp(start + dir * step * i);
                c.F("leviathan", p + new Vector3(0f, 1f), c.L(2.6f, 3f, 3.6f), dir.x < 0 ? 180f : 0f, 0f, false, 14f);
                c.F("riptide", p, 2f, 0f, 0f, false, 16f);
                c.Snd("wave_crash", p);
                foreach (Actor u in c.Foes(p, step * 0.6f))
                {
                    c.Hit(u, 1.3f);
                    c.Launch(u, c.L(2f, 2.5f, 3f), 0.3f);
                }
                c.Impact(p, 0.5f);
                yield return c.W(0.32f);
            }
            Vector3 end = Cast.Clamp(start + dir * step * 3.5f);
            c.F("spring", end + new Vector3(0f, 1f), 2.4f, 0f, 0f, false, 16f);
            c.Snd("splash_big", end);
            c.Area(end, 2.2f, 2f, "slowness", 3f);
            c.Impact(end, 0.9f);
        }

        // ================================================================ Pyro · Ember Fist
        // Explosive close combat: rising combos, fire walls, embers from above and the dive of a phoenix.

        private static void PyroVision()
        {
            Library.Style("gi_vision_pyro", "Ember Fist", 1, "pyro")
                .T("flame_combo", "Rising Flame Combo", Shape.Lone, 2f, 5f, PyroCombo,
                    "Jab, jab, a flaming uppercut, and a leaping hammer blow on the way down.")
                .T("pillar_wall", "Wall of Pyres", Shape.Line, 7f, 7f, PyroPillarWall,
                    "Draws a burning trail down a line that erupts into a wall of fire all at once.")
                .T("ember_scatter", "Ember Scatter", Shape.Crowd, 6f, 8f, PyroEmbers,
                    "Leaps above the crowd and sprays a rain of embers that sets the ground alight.")
                .T("phoenix_dive", "Phoenix Dive", Shape.Gap, 10f, 8f, PyroPhoenix,
                    "Becomes a diving phoenix and crashes into the foe.")
                .T("blazing_heart", "Blazing Heart", Shape.Guard, 0f, 16f, PyroHeart,
                    "Ignites their own spirit: empowered, and pulsing heat that scorches the adjacent.")
                .T("little_sun", "Little Sun", Shape.Crowd, 6f, 20f, PyroSun,
                    "Forges a small sun over their head and hurls it down.", 2, 2.5f, true);
        }

        private static IEnumerator PyroCombo(Cast c)
        {
            Actor v = c.T;
            if (c.Dist > 1.2f) yield return c.Dash(c.Behind(v, -0.8f), 0.08f);
            Cast.Hold(v, 1.6f);
            for (int i = 0; i < 2 && c.TOk; i++)
            {
                c.F("flame_fist", c.P + c.Dir * 0.5f, 0.8f, c.Ang, 0f, false, 30f);
                c.Snd("punch", c.TP);
                c.Hit(v, 0.4f);
                yield return c.W(0.1f);
            }
            if (!c.TOk) yield break;
            c.F("flame_fist", c.TP, 1.2f, 90f, 0f, false, 26f);
            c.F("fire_pillar", c.TP + new Vector3(0f, 0.6f), 0.9f, 0f, 0f, false, 26f);
            c.Snd("flame_burst", c.TP);
            c.Hit(v, 0.7f, "burning", 3f);
            float h = c.L(1.8f, 2.2f, 2.8f);
            c.Launch(v, h, 0.3f);
            yield return c.W(0.1f);
            c.Snd("leap");
            yield return Cast.Rise(c.A, h + 0.4f, 0.18f);
            c.F("flame_fist", c.TP, 1.4f, -90f, 0f, false, 26f);
            c.Snd("slam", c.TP);
            c.Hit(v, c.L(0.9f, 1.2f, 1.6f));
            ModClass.Instance.StartCoroutine(Cast.Fall(c.A, 0.15f));
            yield return c.W(0.25f);
            c.F("ember", c.TP, 1.2f, 0f, 0f, false, 20f);
            c.Burst(c.TP, 1.5f);
            if (c.Lv >= 2) c.Crater(c.TP, c.L(0f, 1f, 1.5f));
            c.Impact(c.TP, c.L(0.3f, 0.5f, 0.8f));
        }

        private static IEnumerator PyroPillarWall(Cast c)
        {
            Vector3 start = c.P + c.Dir * 0.8f, end = Cast.Clamp(c.P + c.Dir * c.L(6f, 7f, 8.5f));
            c.Snd("sizzle");
            // The fuse: embers race along the line.
            for (int i = 0; i <= 6; i++)
            {
                c.F("ember", Vector3.Lerp(start, end, i / 6f), 0.8f, 0f, 0.6f, true, 14f);
                yield return c.W(0.04f);
            }
            yield return c.W(0.2f);
            int n = c.Li(5, 6, 8);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = Vector3.Lerp(start, end, i / (float)(n - 1));
                c.F("fire_pillar", p + new Vector3(0f, 0.9f), c.L(1.2f, 1.4f, 1.7f), 0f, 1f, true, 16f);
            }
            c.Snd("fire_roar", Vector3.Lerp(start, end, 0.5f));
            c.Lane(start, end, 0.8f, 1.2f, "burning");
            Cast.Shake(0.3f);
            Cast.Scorch(Vector3.Lerp(start, end, 0.5f), 2f, c.Li(1, 2, 3));
            yield return c.W(0.5f);
            c.Lane(start, end, 0.8f, 0.5f);
        }

        private static IEnumerator PyroEmbers(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2f, 2.6f, 3.2f);
            c.Snd("leap");
            yield return Cast.Arc(c.A, Vector3.Lerp(c.P, p, 0.6f), 0f, 0.01f);
            yield return Cast.Rise(c.A, c.L(2.4f, 2.8f, 3.4f), 0.25f);
            int n = c.Li(10, 14, 20);
            c.Snd("flame_burst");
            for (int i = 0; i < n; i++)
            {
                Vector3 at = p + Cast.Rand(r);
                c.Shot("ember", c.P + new Vector3(0f, c.A.position_height), at, 0.7f, 0.18f);
                if (i % 4 == 0) c.Snd("sizzle", at);
                ModClass.Instance.StartCoroutine(PyroEmberLand(c, at));
                yield return c.W(0.03f);
            }
            yield return Cast.Fall(c.A, 0.2f);
            c.Dust(c.P, 1.2f);
            Cast.Scorch(p, r, c.Li(2, 3, 5));
        }

        private static IEnumerator PyroEmberLand(Cast c, Vector3 at)
        {
            yield return c.W(0.18f);
            c.F("fire_pillar", at + new Vector3(0f, 0.3f), 0.5f, 0f, 0f, false, 24f);
            c.Area(at, 0.7f, 0.35f, "burning", 3f);
        }

        private static IEnumerator PyroPhoenix(Cast c)
        {
            Vector3 start = c.P, land = c.TP;
            c.Snd("phoenix_cry");
            c.F("lifeburn", c.P, 1.4f, 0f, 0f, false, 20f);
            yield return Cast.Rise(c.A, 2.5f, 0.2f);
            c.Shot("phoenix", start + new Vector3(0f, 2.5f), land, c.L(1.8f, 2.2f, 2.7f), 0.3f);
            yield return Cast.Arc(c.A, c.Behind(c.T, -0.6f), 0.5f, 0.3f, c.Hot);
            Cast.SetHeight(c.A, 0f);
            c.Burst(land, c.L(2.2f, 2.8f, 3.4f));
            c.F("fire_pillar", land + new Vector3(0f, 1f), 1.8f, 0f, 0f, false, 18f);
            c.Snd("explosion_small", land);
            c.Area(land, c.L(1.5f, 1.9f, 2.4f), 1.5f, "burning", 4f);
            c.PushAll(land, 2f, 1.5f);
            c.Crater(land, 1.2f);
            Cast.Scorch(land, 1.5f, c.Li(1, 2, 3));
            c.Impact(land, c.L(0.45f, 0.65f, 0.9f));
        }

        private static IEnumerator PyroHeart(Cast c)
        {
            c.F("lifeburn", c.P, c.L(1.8f, 2.2f, 2.6f), 0f, 0f, false, 18f);
            Fx.Instance ring = c.F("shock", c.P, 1.2f, 0f, 0f, false, 20f, true);
            Cast.Grow(ring, 2f);
            c.Snd("fire_roar");
            c.A.addStatusEffect("powerup", c.L(5f, 7f, 9f));
            int pulses = c.Li(2, 3, 4);
            for (int i = 0; i < pulses && c.Ok; i++)
            {
                yield return c.W(0.45f);
                c.F("fx:aura_flame", c.P, 1.8f, 0f, 0.4f, false, 20f, true);
                c.Snd("flame_burst");
                c.Area(c.P, 1.5f, 0.5f, "burning", 2f);
            }
        }

        private static IEnumerator PyroSun(Cast c)
        {
            Vector3 over = c.P + new Vector3(0f, 2.6f);
            Fx.Instance sun = c.F("sun", over, 1f, 0f, 1.5f, true, 12f);
            Cast.Grow(sun, c.L(2.4f, 2.8f, 3.4f));
            c.Snd("charge");
            c.Snd("fire_roar");
            yield return c.W(1.2f);
            Vector3 land = c.TP;
            c.Shot("sun", over, land, c.L(2.4f, 2.8f, 3.4f), 0.3f);
            c.Snd("whoosh_heavy", land);
            yield return c.W(0.3f);
            float r = c.L(2.6f, 3.2f, 4f);
            c.Burst(land, r * 2f);
            c.F("fire_pillar", land + new Vector3(0f, 1.4f), 2.6f, 0f, 0f, false, 14f);
            c.Crater(land, r);
            for (int i = 0; i < 8; i++) c.Shot("ember", land, land + Cast.Rot(Vector3.right, 45f * i) * r, 0.8f, 0.3f);
            c.Snd("explosion_big", land);
            c.Area(land, r, 3f, "burning", 5f);
            c.PushAll(land, r + 0.5f, 2f);
            Cast.Scorch(land, r, c.Li(3, 4, 6));
            c.Impact(land, 1f);
        }

        // ================================================================ Cryo · Frostbound Arts
        // Cold precision: lances, spreading frost, shattering the frozen and a moment of absolute zero.

        private static void CryoVision()
        {
            Library.Style("gi_vision_cryo", "Frostbound Arts", 1, "cryo")
                .T("ice_lance", "Glacial Lance", Shape.Line, 8f, 6f, CryoLance,
                    "Forms a lance of ice and hurls it through a line. At III, three lances fly abreast.")
                .T("frozen_garden", "Frozen Garden", Shape.Crowd, 5f, 9f, CryoGarden,
                    "Frost spreads in rings around the target, freezing everyone it reaches.")
                .T("shatter_palm", "Shatter Palm", Shape.Execute, 4f, 7f, CryoShatter,
                    "Steps in and strikes with an open palm; frozen foes break apart.")
                .T("snowflake_shuriken", "Snowflake Shuriken", Shape.Lone, 6f, 5f, CryoSnowflakes,
                    "Throws spinning snowflakes that curve out and come back through the foe.")
                .T("glacier_wall", "Glacier Wall", Shape.Guard, 0f, 15f, CryoWall,
                    "Raises a wall of ice spikes between the user and the foe, shoving them back.")
                .T("absolute_zero", "Absolute Zero", Shape.Crowd, 5f, 20f, CryoZero,
                    "Everything nearby freezes solid, then shatters at once.", 2, 2.5f, true);
        }

        private static IEnumerator CryoLance(Cast c)
        {
            c.F("snowflake", c.Front(0.6f), 0.9f, 0f, 0.4f, true, 18f);
            c.Snd("ice_form");
            yield return c.W(0.3f);
            int lances = c.Lv >= 3 ? 3 : 1;
            float reach = c.L(7f, 8f, 9f);
            Vector3 side = Cast.Rot(c.Dir, 90f);
            for (int i = 0; i < lances; i++)
            {
                Vector3 off = side * (i - (lances - 1) / 2f) * 0.8f;
                Vector3 from = c.P + off + c.Dir * 0.4f, to = Cast.Clamp(c.P + off + c.Dir * reach);
                c.Shot("ice_lance", from, to, c.L(1.3f, 1.6f, 1.6f), 0.22f);
                ModClass.Instance.StartCoroutine(CryoLanceLand(c, from, to));
            }
            c.Snd("whoosh_heavy");
            yield return c.W(0.35f);
        }

        private static IEnumerator CryoLanceLand(Cast c, Vector3 from, Vector3 to)
        {
            yield return c.W(0.15f);
            c.Lane(from, to, 0.55f, c.L(1.1f, 1.4f, 1.4f), "slowness");
            c.F("shatter", to, 1f, 0f, 0f, false, 22f);
            c.Snd("ice_crack", to);
        }

        private static IEnumerator CryoGarden(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2f, 2.6f, 3.2f);
            c.Snd("ice_form", p);
            for (int ring = 0; ring < 3; ring++)
            {
                float rr = r * (ring + 1) / 3f;
                int n = ring == 0 ? 1 : 4 + ring * 3;
                for (int i = 0; i < n; i++)
                {
                    Vector3 at = ring == 0 ? p : p + Cast.Rot(Vector3.right, 360f / n * i) * rr;
                    c.F("ice_garden", at, 1.1f, 0f, 2f, false, 14f, back: true);
                }
                c.Snd("ice_crack", p);
                yield return c.W(0.12f);
            }
            float freeze = c.L(1.5f, 2.2f, 3f);
            foreach (Actor u in c.Foes(p, r))
            {
                c.Hit(u, 0.7f, "frozen", freeze);
                Cast.Hold(u, freeze);
            }
            yield return c.W(0.2f);
        }

        private static IEnumerator CryoShatter(Cast c)
        {
            Actor v = c.T;
            yield return c.Dash(c.Behind(v, -0.8f), 0.1f);
            if (!c.TOk) yield break;
            bool frozen = v.hasStatus("frozen");
            c.F("snowflake", c.TP, 1.2f, 0f, 0f, false, 24f);
            c.Snd("punch", c.TP);
            yield return c.W(0.1f);
            if (!c.TOk) yield break;
            c.F("shatter", c.TP, c.L(1.6f, 2f, 2.5f), 0f, 0f, false, 20f);
            c.Snd(frozen ? "frost_shatter" : "ice_crack", c.TP);
            float power = c.L(1.3f, 1.7f, 2.2f) * (frozen ? 2f : 1f);
            if (frozen) v.finishStatusEffect("frozen");
            c.Hit(v, power);
            c.Impact(c.TP, frozen ? c.L(0.5f, 0.7f, 1f) : 0.3f);
            if (c.TOk) yield return c.Knock(v, c.Dir, frozen ? 2.5f : 1.2f, 0.25f);
        }

        private static IEnumerator CryoSnowflakes(Cast c)
        {
            int n = c.Li(2, 3, 4);
            c.Snd("whoosh_light");
            for (int i = 0; i < n; i++)
            {
                float bend = (i % 2 == 0 ? 1f : -1f) * (1f + i * 0.3f);
                ModClass.Instance.StartCoroutine(CryoBoomerang(c, bend));
                yield return c.W(0.08f);
            }
            yield return c.W(0.9f);
        }

        private static IEnumerator CryoBoomerang(Cast c, float bend)
        {
            Vector3 start = c.P, target = c.TP;
            Vector3 mid = Vector3.Lerp(start, target, 0.5f) + Cast.Rot((target - start).normalized, 90f) * bend;
            Fx.Instance flake = c.F("snowflake", start, 0.9f, 0f, 0.9f, true, 24f);
            Fx.MoveTo(flake, mid, 0.2f, false);
            yield return c.W(0.2f);
            Fx.MoveTo(flake, target, 0.15f, false);
            yield return c.W(0.15f);
            if (c.TOk) { c.Hit(c.T, 0.45f, "slowness", 2f); c.Snd("ice_crack", target); }
            Vector3 back = Vector3.Lerp(target, c.P, 0.5f) - Cast.Rot((target - start).normalized, 90f) * bend;
            Fx.MoveTo(flake, back, 0.18f, false);
            yield return c.W(0.18f);
            Fx.MoveTo(flake, c.P, 0.15f, false);
            c.Area(back, 0.7f, 0.3f);
        }

        private static IEnumerator CryoWall(Cast c)
        {
            Vector3 center = c.P + c.Dir * 1.3f;
            Vector3 side = Cast.Rot(c.Dir, 90f);
            int n = c.Li(4, 5, 7);
            c.Snd("ice_form");
            for (int i = 0; i < n; i++)
            {
                Vector3 at = center + side * (i - (n - 1) / 2f) * 0.7f;
                c.F("ice_garden", at, 1.2f, 0f, c.L(2.5f, 3f, 4f), false, 18f);
                yield return c.W(0.03f);
            }
            c.Snd("ice_crack");
            foreach (Actor u in c.Foes(center, n * 0.4f + 0.6f))
            {
                c.Hit(u, 0.6f, "slowness", 3f);
                c.Push(u, c.Dir, c.L(1.5f, 2f, 2.5f));
            }
            c.A.addStatusEffect("shield", c.L(3f, 4f, 5f));
            yield return c.W(0.3f);
        }

        private static IEnumerator CryoZero(Cast c)
        {
            Vector3 p = c.P;
            float r = c.L(3.2f, 4f, 5f);
            c.Snd("blizzard", p);
            Fx.Instance zero = c.F("zero", p, 1.5f, 0f, 1.6f, false, 10f);
            Cast.Grow(zero, r * 1.4f / 1.5f);
            yield return c.W(0.5f);
            var frozen = c.Foes(p, r);
            foreach (Actor u in frozen)
            {
                u.addStatusEffect("frozen", 2.5f);
                Cast.Hold(u, 1.6f);
                c.F("ice_garden", u.current_position, 1f, 0f, 1.2f, false, 12f);
            }
            c.Snd("ice_form", p);
            Cast.Shake(0.4f);
            yield return c.W(c.L(0.9f, 0.8f, 0.7f));
            c.F("shatter", p, r * 1.5f, 0f, 0f, false, 16f);
            c.Snd("frost_shatter", p);
            c.Snd("glass_break", p);
            foreach (Actor u in frozen)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("shatter", u.current_position, 1.2f, 0f, 0f, false, 22f);
                u.finishStatusEffect("frozen");
                c.Hit(u, 3f);
            }
            c.Impact(p, 1f);
        }
    }
}
