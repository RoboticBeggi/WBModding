using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// Tier 4: the seven Dragon Sovereigns, and the Nibelung above them. Dragons are drawn through their own
    /// avatars, so these styles are written for stationary colossi: breath, claws that reach across the
    /// field, wing gusts, roars, and the sky and the ground turned against their enemies.
    /// </summary>
    public static partial class Styles
    {
        static partial void RegisterSovereigns()
        {
            SvSkyTyrant();
            SvEarthshaker();
            SvStormWyrm();
            SvVerdant();
            SvWaters();
            SvFlame();
            SvFrost();
            SvNibelung();
        }

        // ================================================================ shared dragon helpers

        /// <summary>Where breath leaves the dragon: a little ahead and above its centre.</summary>
        private static Vector3 SvMouth(Cast c) => c.P + c.Dir * 1.0f + new Vector3(0f, 0.4f);

        /// <summary>Damages every enemy inside a cone.</summary>
        private static int SvCone(Cast c, Vector3 origin, Vector3 dir, float len, float halfAngle, float pow,
            string status = null, float statusTime = 3f)
        {
            int n = 0;
            foreach (Actor u in c.Foes(origin, len))
            {
                Vector3 d = (Vector3)u.current_position - origin;
                if (d.sqrMagnitude > 0.04f && Vector3.Angle(dir, d) > halfAngle) continue;
                c.Hit(u, pow, status, statusTime);
                n++;
            }
            return n;
        }

        /// <summary>A breath cone drawn from its source point.</summary>
        private static Fx.Instance SvBreathFx(Cast c, Vector3 origin, Vector3 dir, float len, float life, Color col)
        {
            return c.Fc("breath", origin + dir * (len * 0.5f), len, col, Cast.AngleOf(dir), life, true, 18f);
        }

        private static void SvRoarFx(Cast c, float size)
        {
            Fx.Instance r = c.F("roar", c.P, size, 0f, 0f, false, 16f, true);
            Cast.Grow(r, 1.6f);
            c.Snd("dragon_roar");
        }

        /// <summary>Enemies within a radius, closest first, at most a count.</summary>
        private static List<Actor> SvTargets(Cast c, Vector3 center, float radius, int max)
        {
            List<Actor> foes = c.Foes(center, radius);
            foes.Sort((x, y) => ((Vector3)x.current_position - center).sqrMagnitude
                .CompareTo(((Vector3)y.current_position - center).sqrMagnitude));
            if (foes.Count > max) foes.RemoveRange(max, foes.Count - max);
            return foes;
        }

        // ================================================================ Anemo · Sky Tyrant
        // Owns the air: blows armies away, drops columns of wind, juggles fliers.

        private static void SvSkyTyrant()
        {
            Library.Style("gi_sovereign_anemo", "Sky Tyrant", 4, "anemo")
                .T("tempest_breath", "Tempest Breath", Shape.Line, 8f, 7f, SvTempestBreath,
                    "A howling cone of wind that hurls everything in it far away.")
                .T("wingstorm", "Wingstorm", Shape.Crowd, 5f, 9f, SvWingstorm,
                    "Three great wingbeats; each throws a ring of wind outward.")
                .T("skyhook", "Skyhook Talons", Shape.AntiAir, 7f, 7f, SvSkyhook,
                    "Talons reach out, fling the foe skyward and feathers shred it in the air.")
                .T("downburst", "Downburst", Shape.Crowd, 8f, 10f, SvDownburst,
                    "A marked circle — then a column of air slams down and pins everyone.")
                .T("twin_gyres", "Twin Gyres", Shape.Line, 8f, 12f, SvTwinGyres,
                    "Two cyclones spawn on either flank and grind together across the line.")
                .T("gale_mantle", "Gale Mantle", Shape.Guard, 0f, 18f, SvGaleMantle,
                    "A mantle of wind: untouchable, and anything close is flung off.")
                .T("tempest_eye", "Eye of the Tempest", Shape.Crowd, 6f, 24f, SvTempestEye,
                    "A storm forms around the dragon, drags the battlefield in and spits it out.", 2, 3f, true)
                .God("sky_sundering", "Sky-Sundering Roar", SvSkySundering,
                    "A roar crosses the map: every foe within 22 tiles is hurled into the sky and buried in feathers.");
        }

        private static IEnumerator SvTempestBreath(Cast c)
        {
            Vector3 m = SvMouth(c), d = c.Dir;
            float len = c.L(6f, 7.5f, 9f);
            c.Snd("dragon_breath", m);
            c.Snd("wind_howl", m);
            for (int i = 0; i < 4; i++)
            {
                SvBreathFx(c, m, Cast.Rot(d, Random.Range(-6f, 6f)), len, 0.3f, c.Hot);
                c.F("wind_ring", m + d * (len * (0.3f + 0.17f * i)), 1.5f + i * 0.4f, Cast.AngleOf(d), 0f, false, 24f);
                yield return c.W(0.12f);
            }
            foreach (Actor u in c.Foes(m, len))
            {
                Vector3 to = (Vector3)u.current_position - m;
                if (Vector3.Angle(d, to) > 30f) continue;
                c.Hit(u, 1.2f);
                c.Push(u, to, c.L(3f, 4.5f, 6f), 0.4f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator SvWingstorm(Cast c)
        {
            int beats = c.Li(3, 3, 4);
            for (int i = 0; i < beats; i++)
            {
                c.F("wingbeat", c.P + new Vector3(0f, 0.6f), c.L(4f, 4.6f, 5.4f), 0f, 0f, false, 22f, true);
                c.Snd("wing_flap");
                Fx.Instance ring = c.F("wind_ring", c.P, 2f + i, 0f, 0.5f, false, 18f);
                Cast.Grow(ring, 3f);
                float r = c.L(3.5f, 4.2f, 5f) + i * 0.6f;
                c.Area(c.P, r, 0.6f);
                c.PushAll(c.P, r, 1.4f);
                for (int k = 0; k < 4; k++) c.Dust(c.P + Cast.Rand(r), 1.1f);
                Cast.Shake(0.3f);
                yield return c.W(0.32f);
            }
        }

        private static IEnumerator SvSkyhook(Cast c)
        {
            Actor v = c.T;
            c.F("claw", c.TP, c.L(2.4f, 2.8f, 3.2f), c.Ang - 90f, 0f, false, 24f, true);
            c.Snd("slash_heavy", c.TP);
            c.Hit(v, 0.8f);
            float h = c.L(3f, 3.8f, 4.5f);
            c.Launch(v, h, c.L(0.9f, 1.1f, 1.4f));
            c.F("wind_spout", c.TP + new Vector3(0f, 1f), 2f, 0f, 0f, false, 22f);
            yield return c.W(0.25f);
            int feathers = c.Li(6, 9, 13);
            for (int i = 0; i < feathers && c.TOk; i++)
            {
                Vector3 at = (Vector3)v.current_position + new Vector3(0f, v.position_height * 0.25f);
                Vector3 from = at + Cast.Rot(Vector3.right, Random.Range(0f, 360f)) * 3.5f;
                c.Shot("feather", from, at, 0.9f, 0.12f);
                c.Snd("feather_flutter", at);
                yield return c.W(0.06f);
                c.Hit(v, 0.22f);
                c.Spark(at, 0.8f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator SvDownburst(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2.4f, 3f, 3.8f);
            c.F("wind_ring", p, r * 2f, 0f, 0.7f, true, 12f, false, true);
            c.Snd("wind_gust", p);
            yield return c.W(0.65f);
            c.F("wind_spout", p + new Vector3(0f, 1.6f), r * 1.6f, 180f, 0f, false, 26f);
            yield return c.W(0.12f);
            c.Burst(p, r * 1.8f);
            c.Crater(p, r * 0.8f);
            c.Snd("crater_boom", p);
            c.Impact(p, c.L(0.5f, 0.7f, 0.95f));
            foreach (Actor u in c.Foes(p, r))
            {
                c.Hit(u, 1.6f);
                Cast.Hold(u, c.L(1f, 1.5f, 2.2f));
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator SvTwinGyres(Cast c)
        {
            Vector3 t = c.TP, side = Cast.Rot(c.Dir, 90f);
            Vector3 a = t + side * 4f, b = t - side * 4f;
            float size = c.L(2f, 2.4f, 2.9f);
            Fx.Instance ga = c.F("cyclone", a, size, 0f, 1.6f, true, 16f);
            Fx.Instance gb = c.F("cyclone", b, size, 0f, 1.6f, true, 16f);
            Vector3 ea = t - side * 1.5f, eb = t + side * 1.5f;
            Fx.MoveTo(ga, ea, 1.4f, false);
            Fx.MoveTo(gb, eb, 1.4f, false);
            c.Snd("vortex", t);
            for (float s = 0f; s < 1.4f; s += 0.2f)
            {
                float k = s / 1.4f;
                c.Area(Vector3.Lerp(a, ea, k), size * 0.45f, 0.35f);
                c.Area(Vector3.Lerp(b, eb, k), size * 0.45f, 0.35f);
                c.PullAll(t, 1.5f + 3f * (1f - k), 0.2f);
                yield return c.W(0.2f);
            }
            c.Burst(t, 3f);
            c.Lane(t - side * 3f, t + side * 3f, 1.2f, 1.6f);
            c.Snd("shockwave", t);
            c.Impact(t, 0.6f);
        }

        private static IEnumerator SvGaleMantle(Cast c)
        {
            Fx.Instance m = c.F("wind_ring", c.P, 3f, 0f, c.L(4f, 5f, 6f), true, 12f);
            Cast.Stick(m, c.A);
            c.Snd("wind_howl");
            c.A.addStatusEffect("dodge", c.L(5f, 6f, 8f));
            c.A.addStatusEffect("shield", c.L(4f, 5f, 6f));
            for (int i = 0; i < 3; i++)
            {
                c.PushAll(c.P, 3f, 2f);
                c.Area(c.P, 3f, 0.3f);
                c.F("feather", c.P + Cast.Rand(2f), 1f, Random.Range(0f, 360f), 0f, false, 20f);
                yield return c.W(0.4f);
            }
        }

        private static IEnumerator SvTempestEye(Cast c)
        {
            Vector3 p = c.P;
            float r = c.L(6f, 7f, 8.5f);
            Fx.Instance storm = c.F("cyclone", p, c.L(5f, 6f, 7.5f), 0f, 2.6f, true, 14f, false, true);
            c.F("vacuum", p, r, 0f, 2.2f, true, 12f);
            c.Snd("wind_howl", p);
            for (int i = 0; i < 8; i++)
            {
                c.PullAll(p, r, 0.25f);
                foreach (Actor u in c.Foes(p, r)) c.Hit(u, 0.3f);
                for (int k = 0; k < 3; k++)
                {
                    Vector3 f = p + Cast.Rand(r);
                    c.Shot("feather", f, p + Cast.Rand(1f), 0.9f, 0.25f);
                }
                if (i % 2 == 0) c.Snd("vortex", p);
                yield return c.W(0.25f);
            }
            c.Burst(p, r);
            c.Area(p, r * 0.7f, 2f);
            foreach (Actor u in c.Foes(p, r))
            {
                c.Launch(u, 2.5f, 0.3f);
                c.Push(u, (Vector3)u.current_position - p, 4f, 0.5f);
            }
            c.Snd("explosion_big", p);
            c.Impact(p, 1f);
        }

        private static IEnumerator SvSkySundering(Cast c)
        {
            Vector3 p = c.P;
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance r = c.F("roar", p, 4f + i * 3f, 0f, 0.8f, false, 14f, true);
                Cast.Grow(r, 5f);
                c.Snd("dragon_roar", p);
                Cast.Shake(0.8f);
                yield return c.W(0.25f);
            }
            List<Actor> foes = SvTargets(c, p, 22f, 40);
            foreach (Actor u in foes)
            {
                c.F("wind_spout", (Vector3)u.current_position + new Vector3(0f, 1f), 2f, 0f, 0f, false, 22f);
                c.Hit(u, 1.5f);
                c.Launch(u, Random.Range(3f, 5f), 1.4f);
            }
            c.Snd("wind_howl", p);
            yield return c.W(0.4f);
            for (int wave = 0; wave < 4; wave++)
            {
                foreach (Actor u in foes)
                {
                    if (!CombatCore.Alive(u)) continue;
                    Vector3 at = u.current_position;
                    c.Shot("feather", at + new Vector3(Random.Range(-3f, 3f), 4f), at, 1f, 0.15f);
                    c.Hit(u, 0.8f);
                }
                c.Snd("feather_flutter", p);
                yield return c.W(0.2f);
            }
            yield return c.W(0.6f);
            foreach (Actor u in foes)
                if (CombatCore.Alive(u)) c.Dust(u.current_position, 1.5f);
            c.Snd("crater_boom", p);
            c.Impact(p, 1f);
        }

        // ================================================================ Geo · Earthshaker (Azhdaha)
        // The ground itself: ripples of pillars, cracks that close back, petrification, a falling mountain.

        private static void SvEarthshaker()
        {
            Library.Style("gi_sovereign_geo", "Earthshaker", 4, "geo")
                .T("boulder_spit", "Boulder Spit", Shape.Lone, 8f, 6f, SvBoulderSpit,
                    "Spits a molten boulder in a high arc; it bursts into shrapnel and stuns.")
                .T("tectonic_stomp", "Tectonic Stomp", Shape.Crowd, 5f, 9f, SvTectonicStomp,
                    "Rings of rock pillars ripple outward from the dragon's feet.")
                .T("fault_rend", "Fault Rend", Shape.Line, 9f, 9f, SvFaultRend,
                    "A claw rakes open a crack; pillars burst from it, far end first, racing back.")
                .T("petrify", "Petrifying Roar", Shape.Execute, 6f, 12f, SvPetrify,
                    "A roar that turns the foes before it to stone — then shatters them.")
                .T("crystal_hail", "Crystal Hail", Shape.Crowd, 8f, 10f, SvCrystalHail,
                    "Shards of crystallised earth hail down across a wide field.")
                .T("bedrock_hide", "Bedrock Hide", Shape.Guard, 0f, 18f, SvBedrockHide,
                    "Plates of stone close over the dragon and a wall of pillars rises around it.")
                .T("mountains_fall", "Mountain's Fall", Shape.Crowd, 9f, 26f, SvMountainsFall,
                    "A whole mountain descends on the enemy, and the land splits around it.", 2, 3f, true)
                .God("primordial_quake", "Primordial Quake", SvPrimordialQuake,
                    "The earth wakes: cracks web across 22 tiles and pillars erupt beneath every foe.");
        }

        private static IEnumerator SvBoulderSpit(Cast c)
        {
            Vector3 m = SvMouth(c), t = c.TP;
            c.Snd("boulder_hit", m);
            Vector3 apex = Vector3.Lerp(m, t, 0.5f) + new Vector3(0f, c.L(3f, 3.5f, 4f));
            Fx.Instance rock = c.Shot("geo_meteor", m, apex, c.L(1.2f, 1.5f, 1.9f), 0.25f, false, false);
            yield return c.W(0.25f);
            if (rock != null) Fx.MoveTo(rock, t, 0.2f, false);
            yield return c.W(0.2f);
            c.Burst(t, 2f);
            c.F("debris", t, 2.4f, 0f, 0f, false, 18f);
            c.Snd("stone_crack", t);
            c.Crater(t, c.L(1f, 1.3f, 1.8f));
            c.Impact(t, c.L(0.4f, 0.6f, 0.85f));
            if (c.TOk)
            {
                c.Hit(c.T, 1.8f);
                Cast.Hold(c.T, c.L(1f, 1.4f, 2f));
            }
            c.Area(t, c.L(1.4f, 1.8f, 2.3f), 0.6f);
        }

        private static IEnumerator SvTectonicStomp(Cast c)
        {
            c.Snd("rumble");
            Cast.Shake(0.6f);
            int rings = c.Li(2, 3, 4);
            for (int ring = 1; ring <= rings; ring++)
            {
                float r = ring * 1.6f;
                int n = 6 + ring * 3;
                for (int i = 0; i < n; i++)
                {
                    Vector3 at = c.P + Cast.Rot(Vector3.right, 360f * i / n + ring * 15f) * r;
                    c.F("rock_pillar", at + new Vector3(0f, 0.8f), 1.2f, 0f, 0f, false, 20f);
                }
                c.Snd("stone_rise", c.P);
                foreach (Actor u in c.Foes(c.P, r + 0.6f))
                {
                    float d = Vector2.Distance(u.current_position, c.P);
                    if (d < r - 1f) continue;
                    c.Hit(u, 0.9f);
                    c.Launch(u, 1.2f, 0.15f);
                }
                yield return c.W(0.22f);
            }
        }

        private static IEnumerator SvFaultRend(Cast c)
        {
            Vector3 from = c.P + c.Dir * 1f, to = c.P + c.Dir * c.L(7f, 8.5f, 10f);
            c.F("claw", c.Front(1.4f), 2.2f, c.Ang - 90f, 0f, false, 24f, true);
            c.Snd("slash_heavy");
            c.Fissure(from, to, 1.2f);
            c.Snd("stone_crack", Vector3.Lerp(from, to, 0.5f));
            yield return c.W(0.3f);
            int n = c.Li(6, 8, 10);
            for (int i = n; i >= 1; i--)
            {
                Vector3 at = Vector3.Lerp(from, to, i / (float)n);
                c.F("rock_pillar", at + new Vector3(0f, 0.9f), 1.4f, 0f, 0f, false, 22f);
                foreach (Actor u in c.Foes(at, 1f))
                {
                    c.Hit(u, 0.8f);
                    c.Launch(u, 1.8f, 0.2f);
                }
                c.Snd("stone_rise", at);
                yield return c.W(0.06f);
            }
            c.Impact(to, 0.5f);
        }

        private static IEnumerator SvPetrify(Cast c)
        {
            SvRoarFx(c, c.L(4f, 5f, 6f));
            float len = c.L(5f, 6f, 7f);
            var stoned = new List<Actor>();
            foreach (Actor u in c.Foes(c.P, len))
            {
                Vector3 d = (Vector3)u.current_position - c.P;
                if (Vector3.Angle(c.Dir, d) > 40f) continue;
                stoned.Add(u);
                Cast.Hold(u, 2.2f);
                c.F("lattice", u.current_position, 1.3f, 0f, 1.4f, true, 10f);
            }
            c.Snd("crystal_chime");
            yield return c.W(c.L(1.2f, 1.2f, 1.4f));
            foreach (Actor u in stoned)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("debris", u.current_position, 1.6f, 0f, 0f, false, 20f);
                float wounded = u.getHealth() < u.getMaxHealth() * 0.4f ? 2f : 1f;
                c.Hit(u, 1.3f * wounded);
            }
            c.Snd("glass_break");
            c.Impact(c.TP, 0.6f);
        }

        private static IEnumerator SvCrystalHail(Cast c)
        {
            Vector3 center = c.TP;
            float r = c.L(3.5f, 4.5f, 5.5f);
            int shards = c.Li(10, 15, 22);
            c.Snd("crystal_chime", center);
            for (int i = 0; i < shards; i++)
            {
                Vector3 at = center + Cast.Rand(r);
                c.Shot("geo_meteor", at + new Vector3(-1.2f, 4f), at, 0.8f, 0.18f, false, false);
                ModClass.Instance.StartCoroutine(SvShardLand(c, at));
                yield return c.W(0.06f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator SvShardLand(Cast c, Vector3 at)
        {
            yield return c.W(0.18f);
            c.F("lattice", at, 0.8f, Random.Range(0f, 60f), 0f, false, 24f);
            c.Dust(at, 0.8f);
            c.Area(at, 0.9f, 0.45f);
            c.Snd("stone_crack", at);
        }

        private static IEnumerator SvBedrockHide(Cast c)
        {
            Fx.Instance hide = c.F("lattice", c.P, 3f, 0f, c.L(4f, 5f, 6f), true, 10f);
            Cast.Stick(hide, c.A);
            c.A.addStatusEffect("shield", c.L(5f, 6f, 8f));
            c.A.addStatusEffect("powerup", c.L(5f, 6f, 8f));
            c.Snd("stone_rise");
            for (int i = 0; i < 10; i++)
            {
                Vector3 at = c.P + Cast.Rot(Vector3.right, i * 36f) * 2.4f;
                c.F("rock_pillar", at + new Vector3(0f, 0.8f), 1.3f, 0f, 0f, false, 18f);
                yield return c.W(0.03f);
            }
            c.PushAll(c.P, 2.5f, 1.5f);
            c.Area(c.P, 2.5f, 0.5f);
        }

        private static IEnumerator SvMountainsFall(Cast c)
        {
            Vector3 t = c.TP;
            float size = c.L(4f, 5f, 6.5f);
            c.Snd("rumble", t);
            for (int i = 0; i < 4; i++)
            {
                c.Dust(t + Cast.Rand(size * 0.6f), 1.4f);
                yield return c.W(0.1f);
            }
            Fx.Instance mountain = c.F("planet", t + new Vector3(0f, 9f), size, 0f, 1.25f, true, 10f);
            Fx.MoveTo(mountain, t, 1.1f, false);
            yield return c.W(1.1f);
            c.Burst(t, size * 1.6f);
            c.Crater(t, size * 0.7f);
            for (int i = 0; i < c.Li(4, 5, 7); i++)
                c.Fissure(t, t + Cast.Rot(Vector3.right, i * 360f / c.Li(4, 5, 7) + 20f) * size * 1.3f, 1.2f);
            c.Snd("crater_boom", t);
            c.Snd("explosion_big", t);
            c.Impact(t, 1f);
            c.Area(t, size * 0.6f, 4f);
            c.PushAll(t, size, 2.5f);
            foreach (Actor u in c.Foes(t, size)) Cast.Hold(u, 1.2f);
        }

        private static IEnumerator SvPrimordialQuake(Cast c)
        {
            Vector3 p = c.P;
            c.Snd("rumble", p);
            Cast.Shake(1f);
            for (int i = 0; i < 10; i++)
            {
                Vector3 dir = Cast.Rot(Vector3.right, i * 36f + Random.Range(-10f, 10f));
                c.Fissure(p, p + dir * Random.Range(12f, 22f), 1.4f);
            }
            yield return c.W(0.5f);
            List<Actor> foes = SvTargets(c, p, 22f, 50);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 at = u.current_position;
                c.F("rock_pillar", at + new Vector3(0f, 1f), 2f, 0f, 0f, false, 20f);
                c.Hit(u, 3f);
                c.Launch(u, 3f, 0.4f);
                c.Snd("stone_rise", at);
                yield return c.W(0.04f);
            }
            yield return c.W(0.6f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.Crater(u.current_position, 1.2f);
                c.Hit(u, 1.5f);
            }
            c.Snd("crater_boom", p);
            c.Impact(p, 1f);
        }

        // ================================================================ Electro · Storm Wyrm
        // Never still: flickering beams, strikes from the sky, sigils, ball lightning, a storm overhead.

        private static void SvStormWyrm()
        {
            Library.Style("gi_sovereign_electro", "Storm Wyrm", 4, "electro")
                .T("lightning_breath", "Lightning Breath", Shape.Line, 9f, 7f, SvLightningBreath,
                    "A crackling stream of lightning that flickers down a line for a full second.")
                .T("thunder_claw", "Thunder Claw", Shape.Lone, 7f, 6f, SvThunderClaw,
                    "A raking claw, then bolts from the sky on the same spot.")
                .T("storm_crown", "Storm Crown", Shape.Crowd, 8f, 10f, SvStormCrown,
                    "Sigils brand every enemy around; they detonate one after another.")
                .T("ion_wings", "Ion Wings", Shape.Gap, 12f, 9f, SvIonWings,
                    "A wingbeat flings a slow ball of lightning that zaps all it passes.")
                .T("static_field", "Static Field", Shape.Crowd, 4f, 12f, SvStaticField,
                    "The air around the dragon becomes a field of arcs that numbs and burns.")
                .T("conductor", "Conductor's Scales", Shape.Guard, 0f, 16f, SvConductor,
                    "Charged scales: a shield that lashes out at anyone nearby.")
                .T("heavenfall", "Heavenfall Thunder", Shape.Crowd, 10f, 24f, SvHeavenfall,
                    "A storm eye opens above the enemy and the sky empties itself.", 2, 3f, true)
                .God("storm_judgment", "Judgment of the Storm Wyrm", SvStormJudgment,
                    "A storm eye over every foe within 22 tiles; each is struck in turn.");
        }

        private static IEnumerator SvLightningBreath(Cast c)
        {
            Vector3 m = SvMouth(c);
            Vector3 end = m + c.Dir * c.L(7f, 8.5f, 10f);
            c.Snd("thunder_crack", m);
            int ticks = c.Li(6, 8, 10);
            for (int i = 0; i < ticks; i++)
            {
                Vector3 jitter = Cast.Rot(c.Dir, 90f) * Random.Range(-0.4f, 0.4f);
                c.Beam("bolt", m, end + jitter, c.L(1f, 1.2f, 1.5f), 0.1f, true, true);
                c.Lane(m, end, 0.8f, 0.3f);
                if (i % 2 == 0) c.Snd("zap", Vector3.Lerp(m, end, Random.value));
                c.Spark(end + jitter, 1f);
                yield return c.W(0.1f);
            }
            c.Lane(m, end, 0.8f, 0.4f, "stunned");
        }

        private static IEnumerator SvThunderClaw(Cast c)
        {
            c.F("claw", c.TP, 2.4f, c.Ang - 90f, 0f, false, 24f, true);
            c.Snd("slash_heavy", c.TP);
            c.Hit(c.T, 0.9f);
            yield return c.W(0.2f);
            int strikes = c.Li(1, 2, 3);
            for (int i = 0; i < strikes; i++)
            {
                Vector3 t = c.TP + Cast.Rand(0.3f);
                c.Beam("bolt", t + new Vector3(0f, 7f), t, 1.2f, 0.18f, true, true);
                c.F("shock", t, 1.8f, 0f, 0f, false, 24f, true);
                c.Snd("thunder_crack", t);
                c.Area(t, 1.2f, 0.9f, "stunned", 1f);
                c.Impact(t, 0.45f);
                yield return c.W(0.22f);
            }
            c.Crater(c.TP, 1f);
        }

        private static IEnumerator SvStormCrown(Cast c)
        {
            List<Actor> marked = SvTargets(c, c.TP, c.L(4f, 5f, 6f), c.Li(4, 6, 9));
            foreach (Actor u in marked)
            {
                Fx.Instance s = c.F("sigil", u.current_position, 1.4f, 0f, 1.6f, true, 14f);
                Cast.Stick(s, u);
            }
            c.Snd("static_buzz", c.TP);
            yield return c.W(0.7f);
            foreach (Actor u in marked)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 t = u.current_position;
                c.Beam("bolt", t + new Vector3(0f, 6f), t, 1f, 0.15f, true, true);
                c.F("cross_cut", t, 1.6f, 0f, 0f, false, 24f, true);
                c.Hit(u, 1.4f);
                c.Snd("thunder_crack", t);
                yield return c.W(0.09f);
            }
        }

        private static IEnumerator SvIonWings(Cast c)
        {
            c.F("wingbeat", c.P + new Vector3(0f, 0.5f), 4f, 0f, 0f, false, 22f, true);
            c.Snd("wing_flap");
            Vector3 from = SvMouth(c), to = c.TP;
            float time = c.L(1.2f, 1.1f, 1f);
            Fx.Instance ball = c.Shot("fx:orb", from, to, c.L(1.4f, 1.7f, 2f), time, true, false);
            float t = 0f;
            while (t < time)
            {
                Vector3 at = Vector3.Lerp(from, to, t / time);
                Actor near = c.NearestFoe(at, 2.5f);
                if (near != null)
                {
                    c.Beam("bolt", at, near.current_position, 0.6f, 0.1f, true, true);
                    c.Hit(near, 0.3f);
                    c.Snd("zap", at);
                }
                t += 0.12f;
                yield return c.W(0.12f);
            }
            c.Burst(to, 3f);
            c.Snd("explosion_small", to);
            c.Area(to, c.L(2f, 2.5f, 3f), 1.6f, "stunned", 1.2f);
            c.Impact(to, 0.6f);
        }

        private static IEnumerator SvStaticField(Cast c)
        {
            float r = c.L(3.5f, 4.2f, 5f);
            c.F("sigil", c.P, r * 2f, 0f, 2.6f, true, 10f, true, true);
            c.Snd("static_buzz");
            for (int i = 0; i < 10; i++)
            {
                foreach (Actor u in c.Foes(c.P, r))
                {
                    if (Random.value > 0.5f) continue;
                    c.Beam("bolt", c.P + Cast.Rand(r * 0.5f), u.current_position, 0.5f, 0.08f, true, true);
                    c.Hit(u, 0.25f, "slowness", 2f);
                }
                if (i % 3 == 0) c.Snd("zap");
                yield return c.W(0.25f);
            }
        }

        private static IEnumerator SvConductor(Cast c)
        {
            c.A.addStatusEffect("shield", c.L(5f, 6f, 8f));
            Fx.Instance aura = c.F("fx:aura_flame", c.P, 2.6f, 0f, 3f, true, 16f, true);
            Cast.Stick(aura, c.A);
            c.Snd("static_buzz");
            for (int i = 0; i < c.Li(3, 4, 6); i++)
            {
                foreach (Actor u in c.Foes(c.P, 3.2f))
                {
                    c.Beam("bolt", c.P, u.current_position, 0.6f, 0.1f, true, true);
                    c.Hit(u, 0.5f);
                }
                c.Snd("zap");
                yield return c.W(0.45f);
            }
        }

        private static IEnumerator SvHeavenfall(Cast c)
        {
            Vector3 t = c.TP;
            float r = c.L(4f, 5f, 6.5f);
            c.F("storm_eye", t + new Vector3(0f, 4.5f), r * 1.6f, 0f, 3.4f, true, 10f);
            c.Snd("thunder_crack", t);
            int bolts = c.Li(10, 14, 20);
            for (int i = 0; i < bolts; i++)
            {
                Vector3 at = t + Cast.Rand(r);
                c.Beam("bolt", at + new Vector3(0f, 5f), at, 0.9f, 0.12f, true, true);
                c.F("impact", at, 1f, 0f, 0f, false, 26f, true);
                c.Area(at, 1.1f, 0.6f);
                if (i % 2 == 0) c.Snd("zap", at);
                yield return c.W(0.12f);
            }
            c.Beam("bolt", t + new Vector3(0f, 8f), t, 3f, 0.35f, true, true);
            c.Burst(t, r * 1.2f);
            c.Crater(t, r * 0.5f);
            c.Snd("explosion_big", t);
            c.Impact(t, 1f);
            c.Area(t, r * 0.6f, 3f, "stunned", 1.5f);
        }

        private static IEnumerator SvStormJudgment(Cast c)
        {
            Vector3 p = c.P;
            List<Actor> foes = SvTargets(c, p, 22f, 30);
            foreach (Actor u in foes)
            {
                Fx.Instance eye = c.F("storm_eye", (Vector3)u.current_position + new Vector3(0f, 3f), 3f, 0f, 2.5f, true, 10f);
                Cast.Stick(eye, u);
            }
            c.Snd("static_buzz", p);
            yield return c.W(0.8f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 at = u.current_position;
                c.Beam("bolt", at + new Vector3(0f, 8f), at, 1.8f, 0.2f, true, true);
                c.F("shock", at, 2.2f, 0f, 0f, false, 22f, true);
                c.Hit(u, 4f);
                c.Snd("thunder_crack", at);
                Cast.Shake(0.4f);
                yield return c.W(0.07f);
            }
            c.Impact(p, 1f);
        }

        // ================================================================ Dendro · Verdant Sovereign (Apep)
        // Slow, patient, inescapable: roots that crawl, seeds that wait, a garden that eats.

        private static void SvVerdant()
        {
            Library.Style("gi_sovereign_dendro", "Verdant Sovereign", 4, "dendro")
                .T("withering_breath", "Withering Breath", Shape.Line, 8f, 7f, SvWitheringBreath,
                    "A sickly breath that poisons and leaves rotting pools along its path.")
                .T("strangling_roots", "Strangling Roots", Shape.Lone, 9f, 8f, SvStranglingRoots,
                    "Roots crawl across the ground, seize the foe and squeeze.")
                .T("thorn_forest", "Thorn Forest", Shape.Crowd, 8f, 10f, SvThornForest,
                    "Whips of thorn burst up in a spiral across the field.")
                .T("spore_bloom", "Spore Bloom", Shape.Crowd, 7f, 11f, SvSporeBloom,
                    "Seeds settle on every enemy; a heartbeat later each one bursts into a poison flower.")
                .T("devouring_maw", "Devouring Maw", Shape.Execute, 9f, 9f, SvDevouringMaw,
                    "Vines drag a wounded foe across the field into the dragon's claws.")
                .T("oasis_heart", "Oasis Heart", Shape.Support, 0f, 16f, SvOasisHeart,
                    "A bloom of life opens around the dragon, healing it and its allies.")
                .T("apeps_dominion", "Apep's Dominion", Shape.Crowd, 9f, 26f, SvApepsDominion,
                    "A tree erupts from the enemy's ground; its roots bind everything and drink it.", 2, 3f, true)
                .God("garden_oblivion", "Garden of Oblivion", SvGardenOblivion,
                    "Everything within 20 tiles is overgrown, bound and withered in one breath.");
        }

        private static IEnumerator SvWitheringBreath(Cast c)
        {
            Vector3 m = SvMouth(c);
            float len = c.L(6f, 7.5f, 9f);
            Color sick = Color.Lerp(c.Hot, new Color(0.4f, 0.3f, 0.5f), 0.4f);
            c.Snd("dragon_breath", m);
            for (int i = 0; i < 3; i++)
            {
                SvBreathFx(c, m, c.Dir, len, 0.4f, sick);
                SvCone(c, m, c.Dir, len, 25f, 0.4f, "poisoned", 6f);
                yield return c.W(0.2f);
            }
            for (int i = 1; i <= c.Li(2, 3, 4); i++)
            {
                Vector3 at = m + c.Dir * (len * i / (c.Li(2, 3, 4) + 1f));
                c.F("corrosion", at, 1.6f, 0f, 2.5f, true, 10f, false, true);
                ModClass.Instance.StartCoroutine(SvPool(c, at, 2.4f));
            }
        }

        private static IEnumerator SvPool(Cast c, Vector3 at, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.5f)
            {
                c.Area(at, 0.9f, 0.15f, "poisoned", 3f);
                yield return c.W(0.5f);
            }
        }

        private static IEnumerator SvStranglingRoots(Cast c)
        {
            Vector3 from = c.P, to = c.TP;
            int steps = c.Li(5, 6, 7);
            c.Snd("root_creak");
            for (int i = 1; i <= steps; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, i / (float)steps);
                c.F("root", at, 1.4f, c.Ang, 0f, false, 18f);
                c.Dust(at, 0.6f);
                yield return c.W(0.07f);
            }
            if (!c.TOk) yield break;
            Actor v = c.T;
            Cast.Hold(v, c.L(1.8f, 2.3f, 3f));
            Fx.Instance grip = c.F("thorn_whip", v.current_position, 1.6f, 90f, c.L(1.6f, 2f, 2.6f), true, 14f);
            Cast.Stick(grip, v);
            c.Snd("vine_lash", v.current_position);
            for (int i = 0; i < c.Li(4, 5, 7) && c.TOk; i++)
            {
                c.Hit(v, 0.35f);
                c.F("leaves", v.current_position, 0.9f, Random.Range(0f, 360f), 0f, false, 20f);
                yield return c.W(0.3f);
            }
        }

        private static IEnumerator SvThornForest(Cast c)
        {
            Vector3 center = c.TP;
            int n = c.Li(10, 14, 20);
            c.Snd("vine_lash", center);
            for (int i = 0; i < n; i++)
            {
                float a = i * 40f;
                float r = 0.4f + i * c.L(0.3f, 0.32f, 0.3f);
                Vector3 at = center + Cast.Rot(Vector3.right, a) * r;
                c.F("thorn_whip", at + new Vector3(0f, 0.6f), 1.4f, 90f + Random.Range(-15f, 15f), 0f, false, 22f);
                c.Area(at, 0.9f, 0.55f);
                if (i % 3 == 0) c.Snd("vine_lash", at);
                yield return c.W(0.05f);
            }
        }

        private static IEnumerator SvSporeBloom(Cast c)
        {
            List<Actor> hosts = SvTargets(c, c.TP, c.L(4f, 5f, 6f), c.Li(5, 7, 10));
            foreach (Actor u in hosts)
            {
                c.Shot("seed", c.P + new Vector3(0f, 1f), u.current_position, 0.7f, 0.3f);
            }
            c.Snd("leaf_rustle");
            yield return c.W(0.35f);
            foreach (Actor u in hosts)
            {
                Fx.Instance s = c.F("seed", u.current_position, 0.7f, 0f, 1f, true, 12f);
                Cast.Stick(s, u);
            }
            yield return c.W(c.L(1f, 0.9f, 0.8f));
            foreach (Actor u in hosts)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 at = u.current_position;
                c.F("flower", at, 1.6f, 0f, 0f, false, 20f);
                c.Snd("bloom_pop", at);
                c.Area(at, 1.4f, 0.9f, "poisoned", 5f);
            }
        }

        private static IEnumerator SvDevouringMaw(Cast c)
        {
            if (!c.TOk) yield break;
            Actor v = c.T;
            c.Beam("thorn_whip", SvMouth(c), v.current_position, 1f, 0.5f, false, true);
            c.Snd("vine_lash", v.current_position);
            c.Hit(v, 0.5f);
            yield return c.W(0.15f);
            Vector3 front = c.P + c.Dir * 1.4f;
            yield return c.Knock(v, front - (Vector3)v.current_position,
                Mathf.Max(0f, Vector2.Distance(v.current_position, front)), 0.35f);
            if (!c.TOk) yield break;
            c.F("claw", v.current_position, 2.6f, c.Ang - 90f, 0f, false, 24f, true);
            c.Snd("slash_heavy", v.current_position);
            float wounded = v.getHealth() < v.getMaxHealth() * 0.35f ? 1.8f : 1f;
            c.Hit(v, 2f * wounded);
            c.Impact(v.current_position, 0.7f);
            if (!v.isAlive()) c.Heal(c.A, 0.08f);
        }

        private static IEnumerator SvOasisHeart(Cast c)
        {
            c.F("vine_bloom", c.P, c.L(3f, 3.6f, 4.2f), 0f, 1.6f, true, 12f, false, true);
            c.Snd("bloom_pop");
            for (int i = 0; i < 3; i++)
            {
                foreach (Actor a in c.Allies(5f))
                {
                    c.Heal(a, c.L(0.06f, 0.08f, 0.11f));
                    c.F("leaves", a.current_position, 0.9f, 0f, 0f, false, 18f);
                }
                yield return c.W(0.45f);
            }
        }

        private static IEnumerator SvApepsDominion(Cast c)
        {
            Vector3 t = c.TP;
            float r = c.L(4f, 5f, 6f);
            c.Snd("root_creak", t);
            for (int i = 0; i < 8; i++)
                c.F("root", t + Cast.Rot(Vector3.right, i * 45f) * r * 0.5f, 1.8f, i * 45f, 0f, false, 16f);
            yield return c.W(0.3f);
            c.F("world_tree", t + new Vector3(0f, 2f), c.L(4f, 5f, 6f), 0f, 2.6f, false, 8f);
            c.Snd("bloom_pop", t);
            c.Impact(t, 0.7f);
            List<Actor> bound = c.Foes(t, r);
            foreach (Actor u in bound) Cast.Hold(u, 2.6f);
            for (int i = 0; i < 6; i++)
            {
                float drained = 0f;
                foreach (Actor u in bound)
                {
                    if (!CombatCore.Alive(u)) continue;
                    c.Hit(u, 0.55f);
                    drained += 0.01f;
                    if (i % 2 == 0) c.F("leaves", u.current_position, 1f, 0f, 0f, false, 18f);
                }
                c.Heal(c.A, Mathf.Min(0.05f, drained));
                c.Snd("leaf_rustle", t);
                yield return c.W(0.35f);
            }
            c.Burst(t, r);
            c.Area(t, r, 1.5f);
        }

        private static IEnumerator SvGardenOblivion(Cast c)
        {
            Vector3 p = c.P;
            c.F("world_tree", p + new Vector3(0f, 3f), 7f, 0f, 3.5f, false, 7f);
            c.Snd("root_creak", p);
            List<Actor> foes = SvTargets(c, p, 20f, 45);
            foreach (Actor u in foes)
            {
                Cast.Hold(u, 3f);
                Fx.Instance v = c.F("vine_bloom", u.current_position, 1.6f, 0f, 2.2f, true, 10f);
                Cast.Stick(v, u);
            }
            yield return c.W(0.8f);
            c.Snd("leaf_rustle", p);
            for (int k = 0; k < 3; k++)
            {
                foreach (Actor u in foes)
                {
                    if (!CombatCore.Alive(u)) continue;
                    c.Hit(u, 1.4f, "poisoned", 6f);
                }
                yield return c.W(0.4f);
            }
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("flower", u.current_position, 1.8f, 0f, 0f, false, 18f);
                c.Hit(u, 2f);
            }
            c.Snd("bloom_pop", p);
            c.Impact(p, 1f);
        }

        // ================================================================ Hydro · Lord of Waters
        // Sweeps and surges: beams that swing, waves that carry people off, geysers, the deep sea.

        private static void SvWaters()
        {
            Library.Style("gi_sovereign_hydro", "Lord of Waters", 4, "hydro")
                .T("torrent_sweep", "Torrent Sweep", Shape.Line, 8f, 7f, SvTorrentSweep,
                    "A pressurised jet of water swung across the field like a blade.")
                .T("tidal_claw", "Tidal Claw", Shape.Lone, 7f, 7f, SvTidalClaw,
                    "Two lashes of water, then the foe is sealed in a rising bubble.")
                .T("rising_tide", "Rising Tide", Shape.Crowd, 6f, 10f, SvRisingTide,
                    "Waves roll out in every direction and carry the enemy away.")
                .T("geyser_field", "Geyser Field", Shape.Crowd, 9f, 10f, SvGeyserField,
                    "The ground spits geysers under one enemy after another.")
                .T("leviathan_lunge", "Leviathan Lunge", Shape.Line, 10f, 12f, SvLeviathanLunge,
                    "A serpent of water bursts from beside the dragon straight through the line.")
                .T("rain_of_mercy", "Rain of Mercy", Shape.Support, 0f, 16f, SvRainOfMercy,
                    "A gentle rain over allies that closes their wounds.")
                .T("deluge", "Deluge", Shape.Crowd, 9f, 26f, SvDeluge,
                    "A wall of water sweeps across the battlefield, then the sky keeps falling.", 2, 3f, true)
                .God("primordial_sea", "Primordial Sea", SvPrimordialSea,
                    "The old sea returns: waves from every side converge on all foes within 22 tiles.");
        }

        private static IEnumerator SvTorrentSweep(Cast c)
        {
            Vector3 m = SvMouth(c);
            float len = c.L(6.5f, 8f, 9.5f);
            float arc = c.L(60f, 80f, 100f);
            c.Snd("water_whip", m);
            int steps = 9;
            for (int i = 0; i <= steps; i++)
            {
                Vector3 dir = Cast.Rot(c.Dir, -arc / 2f + arc * i / steps);
                Vector3 end = m + dir * len;
                c.Beam("water_whip", m, end, c.L(1f, 1.2f, 1.4f), 0.12f, false, true);
                c.F("droplet", end, 0.8f, Cast.AngleOf(dir), 0f, false, 16f);
                c.Lane(m, end, 0.7f, 0.35f);
                if (i % 3 == 0) c.Snd("splash_big", end);
                yield return c.W(0.06f);
            }
        }

        private static IEnumerator SvTidalClaw(Cast c)
        {
            if (!c.TOk) yield break;
            Actor v = c.T;
            for (int i = 0; i < 2; i++)
            {
                Vector3 off = Cast.Rot(c.Dir, i == 0 ? 25f : -25f);
                c.Beam("water_whip", SvMouth(c), (Vector3)v.current_position + off * 0.4f, 1f, 0.2f);
                c.Snd("water_whip", v.current_position);
                c.Hit(v, 0.6f);
                yield return c.W(0.18f);
            }
            if (!c.TOk) yield break;
            Fx.Instance b = c.F("bubble", v.current_position, 1.6f, 0f, c.L(1.4f, 1.8f, 2.2f), true, 12f);
            Cast.Stick(b, v);
            c.Snd("bubble_pop", v.current_position);
            c.Launch(v, c.L(1.6f, 2f, 2.6f), c.L(0.8f, 1.1f, 1.5f));
            yield return c.W(c.L(1f, 1.3f, 1.7f));
            if (!c.TOk) yield break;
            c.F("shatter", v.current_position, 1.6f, 0f, 0f, false, 22f);
            c.Snd("splash_big", v.current_position);
            c.Hit(v, 1.4f);
        }

        private static IEnumerator SvRisingTide(Cast c)
        {
            int dirs = c.Li(4, 6, 8);
            float reach = c.L(5f, 6f, 7.5f);
            c.Snd("wave_crash");
            for (int i = 0; i < dirs; i++)
            {
                Vector3 d = Cast.Rot(Vector3.right, 360f * i / dirs);
                c.Shot("riptide", c.P + d, c.P + d * reach, 1.8f, 0.6f);
            }
            for (float t = 0f; t < 0.6f; t += 0.15f)
            {
                float r = 1f + reach * (t / 0.6f);
                foreach (Actor u in c.Foes(c.P, r))
                {
                    if (Vector2.Distance(u.current_position, c.P) < r - 1.5f) continue;
                    c.Hit(u, 0.45f);
                    c.Push(u, (Vector3)u.current_position - c.P, 1.2f, 0.2f);
                }
                yield return c.W(0.15f);
            }
        }

        private static IEnumerator SvGeyserField(Cast c)
        {
            List<Actor> targets = SvTargets(c, c.TP, c.L(4f, 5f, 6f), c.Li(4, 6, 8));
            foreach (Actor u in targets)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 at = u.current_position;
                c.F("bubble", at, 0.8f, 0f, 0f, false, 22f, false, true);
                yield return c.W(0.12f);
                c.F("spring", at + new Vector3(0f, 1f), 1.8f, 0f, 0f, false, 20f);
                c.Snd("splash_big", at);
                c.Hit(u, 1f);
                c.Launch(u, c.L(1.8f, 2.4f, 3f), 0.25f);
                yield return c.W(0.08f);
            }
        }

        private static IEnumerator SvLeviathanLunge(Cast c)
        {
            Vector3 side = Cast.Rot(c.Dir, 90f) * 1.2f;
            Vector3 from = c.P + side, to = from + c.Dir * c.L(8f, 9.5f, 11f);
            c.F("spring", from + new Vector3(0f, 1f), 1.8f, 0f, 0f, false, 20f);
            c.Snd("wave_crash", from);
            yield return c.W(0.25f);
            c.Shot("leviathan", from, to, c.L(3f, 3.5f, 4.2f), 0.5f, false, true, 18f);
            c.Snd("water_whip", from);
            for (float t = 0f; t <= 0.5f; t += 0.1f)
            {
                Vector3 at = Vector3.Lerp(from, to, t / 0.5f);
                foreach (Actor u in c.Foes(at, 1.3f))
                {
                    c.Hit(u, 0.8f);
                    c.Push(u, Cast.Rot(c.Dir, Random.value < 0.5f ? 90f : -90f), 1.6f, 0.25f);
                }
                yield return c.W(0.1f);
            }
            c.F("riptide", to, 2.4f, 0f, 0f, false, 18f);
            c.Snd("splash_big", to);
        }

        private static IEnumerator SvRainOfMercy(Cast c)
        {
            c.Snd("splash_big");
            for (int i = 0; i < 8; i++)
            {
                foreach (Actor a in c.Allies(5f))
                {
                    if (Random.value > 0.5f) continue;
                    Vector3 at = a.current_position;
                    c.Shot("droplet", at + new Vector3(0f, 3f), at, 0.7f, 0.15f);
                    c.Heal(a, c.L(0.02f, 0.03f, 0.04f));
                }
                yield return c.W(0.2f);
            }
        }

        private static IEnumerator SvDeluge(Cast c)
        {
            Vector3 d = c.Dir, side = Cast.Rot(d, 90f);
            Vector3 start = c.P + d * 1f;
            float reach = c.L(9f, 10.5f, 12f);
            float width = c.L(4f, 5f, 6f);
            c.Snd("wave_crash", start);
            for (int k = -2; k <= 2; k++)
                c.Shot("riptide", start + side * k * width * 0.4f, start + side * k * width * 0.4f + d * reach, 3f, 1f);
            for (float t = 0f; t <= 1f; t += 0.2f)
            {
                Vector3 front = start + d * reach * t;
                c.Lane(front - side * width, front + side * width, 1.2f, 0.6f);
                foreach (Actor u in CombatCore.EnemiesAlong(c.A, front - side * width, front + side * width, 1.2f))
                    c.Push(u, d, 1.6f, 0.2f);
                yield return c.W(0.2f);
            }
            Vector3 center = start + d * reach * 0.6f;
            for (int i = 0; i < c.Li(10, 14, 20); i++)
            {
                Vector3 at = center + Cast.Rand(width);
                c.Shot("droplet", at + new Vector3(0.5f, 4f), at, 0.9f, 0.18f);
                c.Area(at, 0.9f, 0.4f);
                yield return c.W(0.05f);
            }
            c.Impact(center, 0.9f);
        }

        private static IEnumerator SvPrimordialSea(Cast c)
        {
            Vector3 p = c.P;
            List<Actor> foes = SvTargets(c, p, 22f, 45);
            c.Snd("wave_crash", p);
            for (int i = 0; i < 12; i++)
            {
                Vector3 d = Cast.Rot(Vector3.right, i * 30f);
                c.Shot("riptide", p + d * 20f, p + d * 2f, 4f, 1.2f);
            }
            yield return c.W(1f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                Fx.Instance b = c.F("bubble", u.current_position, 1.8f, 0f, 1.8f, true, 12f);
                Cast.Stick(b, u);
                c.Hit(u, 2f);
                c.Launch(u, 3f, 1.2f);
            }
            c.Snd("bubble_pop", p);
            yield return c.W(1.5f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("spring", (Vector3)u.current_position + new Vector3(0f, 1f), 2f, 0f, 0f, false, 20f);
                c.Hit(u, 2.5f);
            }
            c.Snd("splash_big", p);
            c.Impact(p, 1f);
        }

        // ================================================================ Pyro · Flame Sovereign
        // A walking volcano: breath that keeps burning, magma, embers from the sky, a second sun.

        private static void SvFlame()
        {
            Library.Style("gi_sovereign_pyro", "Flame Sovereign", 4, "pyro")
                .T("inferno_breath", "Inferno Breath", Shape.Line, 8f, 7f, SvInfernoBreath,
                    "A long, roaring breath that sets the ground alight behind it.")
                .T("magma_claw", "Magma Claw", Shape.Lone, 7f, 7f, SvMagmaClaw,
                    "Three furrows of magma raked across the foe; they erupt a moment later.")
                .T("ember_hail", "Ember Hail", Shape.Crowd, 9f, 11f, SvEmberHail,
                    "Burning embers rain over a wide field, each one bursting.")
                .T("phoenix_brood", "Phoenix Brood", Shape.Gap, 12f, 10f, SvPhoenixBrood,
                    "Phoenixes hatch from the dragon's fire and hunt separate foes.")
                .T("eruption_ring", "Eruption Ring", Shape.Crowd, 8f, 12f, SvEruptionRing,
                    "A ring of fire pillars closes in on the enemy from all sides.")
                .T("heat_haze", "Heat Haze", Shape.Guard, 0f, 16f, SvHeatHaze,
                    "The dragon's body burns so hot that anything close catches fire.")
                .T("second_sun", "Second Sun", Shape.Crowd, 8f, 26f, SvSecondSun,
                    "A sun rises over the dragon, lances every foe around, then falls.", 2, 3f, true)
                .God("pyroclasm", "Pyroclasm", SvPyroclasm,
                    "The land becomes a volcano: fire erupts beneath every foe within 22 tiles.");
        }

        private static IEnumerator SvInfernoBreath(Cast c)
        {
            Vector3 m = SvMouth(c);
            float len = c.L(6f, 7.5f, 9f);
            c.Snd("fire_roar", m);
            int ticks = c.Li(5, 6, 8);
            for (int i = 0; i < ticks; i++)
            {
                SvBreathFx(c, m, Cast.Rot(c.Dir, Mathf.Sin(i) * 6f), len, 0.25f, c.Hot);
                SvCone(c, m, c.Dir, len, 22f, 0.35f, "burning", 4f);
                if (i % 2 == 0) c.F("ember", m + c.Dir * Random.Range(1f, len), 0.6f, 0f, 0f, false, 16f);
                yield return c.W(0.15f);
            }
            for (int i = 1; i <= 3; i++) Cast.Scorch(m + c.Dir * len * i / 3.5f, 0.8f, 2);
            c.Snd("sizzle", m + c.Dir * len * 0.6f);
        }

        private static IEnumerator SvMagmaClaw(Cast c)
        {
            Vector3 t = c.TP;
            c.F("claw", t, 2.6f, c.Ang - 90f, 0f, false, 24f, true);
            c.Snd("slash_heavy", t);
            c.Hit(c.T, 0.9f);
            Vector3 across = Cast.Rot(c.Dir, 90f);
            var furrows = new List<Vector3>();
            for (int i = -1; i <= 1; i++)
            {
                Vector3 a = t + c.Dir * (i * 0.45f) - across * 1.6f, b = t + c.Dir * (i * 0.45f) + across * 1.6f;
                c.Fissure(a, b, 0.6f);
                furrows.Add((a + b) * 0.5f);
            }
            c.Snd("sizzle", t);
            yield return c.W(c.L(0.6f, 0.5f, 0.45f));
            foreach (Vector3 f in furrows)
            {
                c.F("fire_pillar", f + new Vector3(0f, 0.9f), 1.4f, 0f, 0f, false, 20f);
                c.Area(f, 1f, 0.7f, "burning", 4f);
                yield return c.W(0.08f);
            }
            c.Snd("flame_burst", t);
        }

        private static IEnumerator SvEmberHail(Cast c)
        {
            Vector3 center = c.TP;
            float r = c.L(4f, 5f, 6.5f);
            int n = c.Li(12, 18, 26);
            c.Snd("fire_roar", center);
            for (int i = 0; i < n; i++)
            {
                Vector3 at = center + Cast.Rand(r);
                c.Shot("ember", at + new Vector3(Random.Range(-2f, 2f), 5f), at, 0.9f, 0.22f, false, true);
                ModClass.Instance.StartCoroutine(SvEmberLand(c, at));
                yield return c.W(0.05f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator SvEmberLand(Cast c, Vector3 at)
        {
            yield return c.W(0.22f);
            c.F("impact", at, 0.9f, Random.Range(0f, 360f), 0f, false, 26f, true);
            c.Area(at, 0.8f, 0.45f, "burning", 3f);
            if (Random.value < 0.3f) Cast.Scorch(at, 0.3f, 1);
            if (Random.value < 0.3f) c.Snd("explosion_small", at);
        }

        private static IEnumerator SvPhoenixBrood(Cast c)
        {
            List<Actor> prey = SvTargets(c, c.TP, 6f, c.Li(2, 3, 5));
            if (prey.Count == 0 && c.TOk) prey.Add(c.T);
            c.Snd("phoenix_cry");
            int i = 0;
            foreach (Actor u in prey)
            {
                Vector3 start = SvMouth(c);
                Vector3 bend = start + Cast.Rot(c.Dir, i % 2 == 0 ? 50f : -50f) * 2.5f + new Vector3(0f, 1f);
                ModClass.Instance.StartCoroutine(SvPhoenixFlight(c, start, bend, u));
                i++;
                yield return c.W(0.12f);
            }
            yield return c.W(0.7f);
        }

        private static IEnumerator SvPhoenixFlight(Cast c, Vector3 start, Vector3 bend, Actor u)
        {
            Fx.Instance bird = c.Shot("phoenix", start, bend, 1.6f, 0.25f, false, true, 18f);
            yield return c.W(0.25f);
            if (!CombatCore.Alive(u)) yield break;
            Vector3 t = u.current_position;
            if (bird != null) Fx.MoveTo(bird, t, 0.25f);
            yield return c.W(0.25f);
            c.F("fire_pillar", t + new Vector3(0f, 0.8f), 1.6f, 0f, 0f, false, 20f);
            c.Burst(t, 1.8f);
            c.Snd("flame_burst", t);
            c.Area(t, 1.3f, 1.3f, "burning", 4f);
        }

        private static IEnumerator SvEruptionRing(Cast c)
        {
            Vector3 t = c.TP;
            float r = c.L(4f, 4.5f, 5.5f);
            int n = 12;
            c.Snd("rumble", t);
            for (float rad = r; rad > 0.4f; rad -= c.L(1.2f, 1f, 0.9f))
            {
                for (int i = 0; i < n; i++)
                {
                    Vector3 at = t + Cast.Rot(Vector3.right, 360f * i / n + rad * 20f) * rad;
                    c.F("fire_pillar", at + new Vector3(0f, 0.8f), 1.1f, 0f, 0f, false, 22f);
                }
                foreach (Actor u in c.Foes(t, rad + 0.6f))
                {
                    if (Vector2.Distance(u.current_position, t) < rad - 0.8f) continue;
                    c.Hit(u, 0.5f, "burning", 3f);
                    c.Push(u, t - (Vector3)u.current_position, 0.8f, 0.15f);
                }
                c.Snd("flame_burst", t);
                yield return c.W(0.2f);
            }
            c.F("fire_pillar", t + new Vector3(0f, 1.5f), 3f, 0f, 0f, false, 18f);
            c.Burst(t, 2.5f);
            c.Area(t, 1.6f, 2f);
            Cast.Scorch(t, 1.5f, 4);
            c.Impact(t, 0.8f);
        }

        private static IEnumerator SvHeatHaze(Cast c)
        {
            c.A.addStatusEffect("powerup", c.L(5f, 7f, 9f));
            if (c.Lv >= 3) c.A.addStatusEffect("shield", 5f);
            Fx.Instance aura = c.F("fx:aura_flame", c.P, 3f, 0f, 3.2f, true, 18f, true);
            Cast.Stick(aura, c.A);
            c.Snd("fire_roar");
            for (int i = 0; i < 6; i++)
            {
                c.Area(c.P, 2.6f, 0.3f, "burning", 3f);
                c.F("ember", c.P + Cast.Rand(2f), 0.7f, 0f, 0f, false, 16f);
                yield return c.W(0.5f);
            }
        }

        private static IEnumerator SvSecondSun(Cast c)
        {
            Vector3 sky = c.P + new Vector3(0f, 4f);
            float r = c.L(6f, 7f, 8.5f);
            Fx.Instance sun = c.F("sun", sky, 2f, 0f, 3.2f, true, 12f);
            Cast.Grow(sun, 2.2f);
            c.Snd("fire_roar", sky);
            yield return c.W(0.9f);
            List<Actor> foes = SvTargets(c, c.P, r, c.Li(6, 9, 14));
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.Beam("fx:beam", sky, u.current_position, 0.8f, 0.2f, true);
                c.F("impact", u.current_position, 1.2f, 0f, 0f, false, 26f, true);
                c.Hit(u, 1.2f, "burning", 4f);
                c.Snd("flame_burst", u.current_position);
                yield return c.W(0.08f);
            }
            yield return c.W(0.4f);
            Vector3 t = c.TP;
            Fx.Instance fall = c.F("sun", sky, 4f, 0f, 0.45f, false, 12f);
            Fx.MoveTo(fall, t, 0.4f, false);
            yield return c.W(0.4f);
            c.Burst(t, 6f);
            c.Crater(t, 3f);
            Cast.Scorch(t, 3f, 8);
            c.Snd("explosion_big", t);
            c.Impact(t, 1f);
            c.Area(t, 3f, 3f, "burning", 5f);
        }

        private static IEnumerator SvPyroclasm(Cast c)
        {
            Vector3 p = c.P;
            c.F("sun", p + new Vector3(0f, 5f), 6f, 0f, 3.5f, true, 10f);
            c.Snd("rumble", p);
            Cast.Shake(1f);
            List<Actor> foes = SvTargets(c, p, 22f, 50);
            foreach (Actor u in foes)
                c.F("corrosion", u.current_position, 1.4f, 0f, 1f, true, 12f, false, true);
            yield return c.W(0.8f);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                Vector3 at = u.current_position;
                c.F("fire_pillar", at + new Vector3(0f, 1.2f), 2.4f, 0f, 0f, false, 18f);
                c.Hit(u, 4f, "burning", 6f);
                c.Launch(u, 2.5f, 0.3f);
                Cast.Scorch(at, 0.8f, 1);
                c.Snd("flame_burst", at);
                yield return c.W(0.04f);
            }
            yield return c.W(0.4f);
            foreach (Actor u in foes)
                if (CombatCore.Alive(u)) c.Crater(u.current_position, 1.2f);
            c.Snd("explosion_big", p);
            c.Impact(p, 1f);
        }

        // ================================================================ Cryo · Frost Sovereign
        // Patient cold: everything freezes first and breaks second.

        private static void SvFrost()
        {
            Library.Style("gi_sovereign_cryo", "Frost Sovereign", 4, "cryo")
                .T("glacial_breath", "Glacial Breath", Shape.Line, 8f, 8f, SvGlacialBreath,
                    "A freezing breath that locks foes in ice and leaves spikes behind.")
                .T("icicle_barrage", "Icicle Barrage", Shape.Lone, 9f, 7f, SvIcicleBarrage,
                    "Icicles form in a halo around the foe and spear inward one by one.")
                .T("frost_talon", "Frost Talon", Shape.Execute, 7f, 8f, SvFrostTalon,
                    "A talon strike that shatters anything frozen or failing.")
                .T("permafrost", "Permafrost", Shape.Crowd, 6f, 11f, SvPermafrost,
                    "Ice spreads outward in rings, slowing all — then the whole field cracks.")
                .T("blizzard", "Roaming Blizzard", Shape.Crowd, 9f, 12f, SvBlizzard,
                    "A blizzard drifts across the enemy lines, numbing and cutting.")
                .T("rime_armor", "Rime Armour", Shape.Guard, 0f, 18f, SvRimeArmor,
                    "Plates of rime encase the dragon; attackers freeze on contact.")
                .T("endless_winter", "Endless Winter", Shape.Crowd, 9f, 26f, SvEndlessWinter,
                    "Absolute cold at one point: everything around freezes, then breaks.", 2, 3f, true)
                .God("white_silence", "White Silence", SvWhiteSilence,
                    "Snow buries everything within 22 tiles; when it stops, every foe shatters.");
        }

        private static IEnumerator SvGlacialBreath(Cast c)
        {
            Vector3 m = SvMouth(c);
            float len = c.L(6f, 7.5f, 9f);
            c.Snd("blizzard", m);
            for (int i = 0; i < 3; i++)
            {
                SvBreathFx(c, m, c.Dir, len, 0.35f, c.Hot);
                c.F("snowflake", m + c.Dir * Random.Range(1f, len), 0.9f, 0f, 0f, false, 18f);
                yield return c.W(0.15f);
            }
            SvCone(c, m, c.Dir, len, 25f, 0.9f, "frozen", c.L(1.5f, 2f, 2.8f));
            c.Snd("ice_form", m);
            for (int i = 1; i <= c.Li(3, 4, 5); i++)
                c.F("ice_garden", m + c.Dir * len * i / (c.Li(3, 4, 5) + 0.5f), 1.6f, 0f, 2.5f, false, 14f, false, true);
        }

        private static IEnumerator SvIcicleBarrage(Cast c)
        {
            if (!c.TOk) yield break;
            Actor v = c.T;
            int n = c.Li(5, 7, 10);
            c.Snd("ice_form", v.current_position);
            for (int i = 0; i < n && c.TOk; i++)
            {
                Vector3 at = v.current_position;
                Vector3 from = at + Cast.Rot(Vector3.right, 360f * i / n) * 3f + new Vector3(0f, 1f);
                c.Shot("ice_lance", from, at, 1.2f, 0.15f);
                yield return c.W(0.15f);
                c.F("shatter", at, 0.9f, 0f, 0f, false, 24f);
                c.Hit(v, 0.35f, i == n - 1 ? "frozen" : null, 1.5f);
                c.Snd("ice_crack", at);
            }
        }

        private static IEnumerator SvFrostTalon(Cast c)
        {
            if (!c.TOk) yield break;
            Actor v = c.T;
            bool brittle = v.hasStatus("frozen") || v.getHealth() < v.getMaxHealth() * 0.35f;
            c.F("claw", v.current_position, 2.6f, c.Ang - 90f, 0f, false, 24f, true);
            c.Snd("slash_heavy", v.current_position);
            c.Hit(v, brittle ? 3f : 1.2f);
            yield return c.W(0.1f);
            if (brittle)
            {
                c.F("shatter", v.current_position, 2.2f, 0f, 0f, false, 20f);
                c.Snd("frost_shatter", v.current_position);
                c.Say("SHATTER", (Vector3)v.current_position + new Vector3(0f, 1.2f), 12);
                c.Impact(v.current_position, 0.8f);
                c.Area(v.current_position, 1.5f, 0.8f);
            }
            else
            {
                c.Hit(v, 0.2f, "frozen", 1.5f);
            }
        }

        private static IEnumerator SvPermafrost(Cast c)
        {
            int rings = c.Li(3, 4, 5);
            c.Snd("ice_form");
            for (int ring = 1; ring <= rings; ring++)
            {
                float r = ring * 1.3f;
                int n = 5 + ring * 3;
                for (int i = 0; i < n; i++)
                    c.F("ice_garden", c.P + Cast.Rot(Vector3.right, 360f * i / n) * r, 1.2f, 0f, 2.4f, false, 14f, false, true);
                foreach (Actor u in c.Foes(c.P, r + 0.5f)) c.Hit(u, 0.2f, "slowness", 4f);
                yield return c.W(0.18f);
            }
            yield return c.W(0.5f);
            float max = rings * 1.3f + 0.5f;
            c.Snd("frost_shatter");
            foreach (Actor u in c.Foes(c.P, max))
            {
                c.F("shatter", u.current_position, 1.2f, 0f, 0f, false, 22f);
                c.Hit(u, 1.2f);
            }
            c.Impact(c.P, 0.6f);
        }

        private static IEnumerator SvBlizzard(Cast c)
        {
            Vector3 from = c.TP - c.Dir * 2f, to = c.TP + c.Dir * 3f;
            float r = c.L(2.5f, 3f, 3.8f);
            c.Snd("blizzard", from);
            float dur = c.L(2.2f, 2.6f, 3f);
            for (float t = 0f; t < dur; t += 0.2f)
            {
                Vector3 at = Vector3.Lerp(from, to, t / dur);
                for (int k = 0; k < 3; k++)
                    c.Shot("snowflake", at + Cast.Rand(r) + new Vector3(-1f, 1.5f), at + Cast.Rand(r), 0.8f, 0.3f, false, false);
                foreach (Actor u in c.Foes(at, r)) c.Hit(u, 0.2f, "slowness", 2f);
                yield return c.W(0.2f);
            }
            if (c.Lv >= 3) c.Area(to, r, 1f, "frozen", 1.5f);
        }

        private static IEnumerator SvRimeArmor(Cast c)
        {
            c.A.addStatusEffect("shield", c.L(5f, 6f, 8f));
            for (int i = 0; i < 8; i++)
                c.F("ice_garden", c.P + Cast.Rot(Vector3.right, i * 45f) * 1.4f, 1.2f, 0f, 3f, false, 12f, false, true);
            c.Snd("ice_form");
            for (int i = 0; i < 4; i++)
            {
                foreach (Actor u in c.Foes(c.P, 2.2f)) c.Hit(u, 0.3f, "frozen", 1.2f);
                yield return c.W(0.6f);
            }
        }

        private static IEnumerator SvEndlessWinter(Cast c)
        {
            Vector3 t = c.TP;
            float r = c.L(4f, 5f, 6.5f);
            Fx.Instance z = c.F("zero", t, 1.5f, 0f, 2f, false, 12f);
            Cast.Grow(z, r * 1.4f / 1.5f);
            c.Snd("blizzard", t);
            yield return c.W(0.6f);
            List<Actor> frozen = c.Foes(t, r);
            foreach (Actor u in frozen)
            {
                c.Hit(u, 0.8f, "frozen", 2.5f);
                Cast.Hold(u, 2f);
            }
            c.Snd("ice_form", t);
            yield return c.W(1.4f);
            c.F("shatter", t, r * 1.5f, 0f, 0f, false, 18f);
            c.Snd("frost_shatter", t);
            c.Snd("glass_break", t);
            foreach (Actor u in frozen)
                if (CombatCore.Alive(u)) c.Hit(u, 2.8f);
            c.Impact(t, 1f);
        }

        private static IEnumerator SvWhiteSilence(Cast c)
        {
            Vector3 p = c.P;
            List<Actor> foes = SvTargets(c, p, 22f, 50);
            c.Snd("blizzard", p);
            for (int k = 0; k < 4; k++)
            {
                Fx.Instance z = c.F("zero", p, 4f + k * 5f, 0f, 0.8f, false, 12f);
                Cast.Grow(z, 1.5f);
                foreach (Actor u in foes)
                {
                    if (!CombatCore.Alive(u)) continue;
                    Vector3 at = u.current_position;
                    c.Shot("snowflake", at + new Vector3(-1f, 3f), at, 0.9f, 0.25f, false, false);
                }
                yield return c.W(0.35f);
            }
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.Hit(u, 1.5f, "frozen", 3f);
                c.F("ice_garden", u.current_position, 1.4f, 0f, 1.5f, false, 12f);
            }
            yield return c.W(1f);
            c.Snd("frost_shatter", p);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("shatter", u.current_position, 1.6f, 0f, 0f, false, 22f);
                c.Hit(u, 4f);
                yield return c.W(0.02f);
            }
            c.Impact(p, 1f);
        }

        // ================================================================ Nibelung · King of Dragons
        // The last king: abyssal fire and lunar steel, and an answer to the heavens.

        private static void SvNibelung()
        {
            Library.Style("gi_nibelung", "King of Dragons", 6, "lunar")
                .T("abyssal_breath", "Abyssal Breath", Shape.Line, 10f, 7f, SvAbyssalBreath,
                    "Black fire down a line, raising columns of abyssal flame as it goes.")
                .T("moonshard_volley", "Moonshard Volley", Shape.Crowd, 9f, 9f, SvMoonshardVolley,
                    "Shards of moon orbit the king, then each flies at a different foe.")
                .T("crown_roar", "Crown of Dragons", Shape.Crowd, 7f, 12f, SvCrownRoar,
                    "A roar of kingship: everything around kneels, stunned.")
                .T("lunar_claw", "Lunar Claw", Shape.Lone, 8f, 6f, SvLunarClaw,
                    "A claw followed by twin crescents that cross on the foe.")
                .T("abyss_rift", "Abyss Rift", Shape.Line, 10f, 12f, SvAbyssRift,
                    "A rift tears open, swallows the line toward it, and erupts.")
                .T("sovereign_will", "Sovereign Will", Shape.Guard, 0f, 20f, SvSovereignWill,
                    "Moonlight pours down on the king: briefly untouchable.")
                .T("moonfall", "Moonfall", Shape.Crowd, 10f, 28f, SvMoonfall,
                    "A moon is pulled from the sky onto the enemy, ringed by pillars of light.", 2, 3.5f, true)
                .God("twilight", "Twilight of Dragons", SvTwilight,
                    "Moon pillars, abyssal fire and a final roar for every foe within 25 tiles.");
        }

        private static readonly Color SvAbyssColor = new Color(0.55f, 0.3f, 1f);

        private static IEnumerator SvAbyssalBreath(Cast c)
        {
            Vector3 m = SvMouth(c);
            float len = c.L(8f, 9.5f, 11f);
            c.Snd("dragon_breath", m);
            c.Snd("abyss_hum", m);
            SvBreathFx(c, m, c.Dir, len, 0.6f, SvAbyssColor);
            int n = c.Li(5, 7, 9);
            for (int i = 1; i <= n; i++)
            {
                Vector3 at = m + c.Dir * (len * i / n);
                c.F("abyss_flame", at + new Vector3(0f, 0.8f), 1.6f, 0f, 0f, false, 18f);
                c.Area(at, 1.1f, 0.7f);
                yield return c.W(0.05f);
            }
            c.Lane(m, m + c.Dir * len, 1f, 0.5f);
        }

        private static IEnumerator SvMoonshardVolley(Cast c)
        {
            int n = c.Li(5, 7, 10);
            var shards = new List<Fx.Instance>();
            for (int i = 0; i < n; i++)
            {
                Vector3 at = c.P + Cast.Rot(Vector3.right, 360f * i / n) * 2f + new Vector3(0f, 0.8f);
                Fx.Instance s = c.F("moon_shard", at, 0.9f, 360f * i / n + 90f, 1.2f, true, 12f);
                shards.Add(s);
            }
            c.Snd("moon_hum");
            yield return c.W(0.6f);
            List<Actor> foes = SvTargets(c, c.TP, 8f, n);
            for (int i = 0; i < shards.Count && foes.Count > 0; i++)
            {
                Actor u = foes[i % foes.Count];
                if (!CombatCore.Alive(u)) continue;
                Vector3 from = c.P + Cast.Rot(Vector3.right, 360f * i / n) * 2f + new Vector3(0f, 0.8f);
                c.Shot("moon_shard", from, u.current_position, 1f, 0.18f);
                ModClass.Instance.StartCoroutine(SvShardHit(c, u));
                c.Snd("star_chime", from);
                yield return c.W(0.05f);
            }
        }

        private static IEnumerator SvShardHit(Cast c, Actor u)
        {
            yield return c.W(0.18f);
            if (!CombatCore.Alive(u)) yield break;
            c.Spark(u.current_position, 1f);
            c.Hit(u, 1.1f);
        }

        private static IEnumerator SvCrownRoar(Cast c)
        {
            c.F("fury_crown", c.P + new Vector3(0f, 1.5f), 2.4f, 0f, 1.2f, true, 12f);
            SvRoarFx(c, c.L(6f, 7f, 8.5f));
            Cast.Shake(0.8f);
            float r = c.L(5f, 6f, 7.5f);
            foreach (Actor u in c.Foes(c.P, r))
            {
                Cast.Hold(u, c.L(1.5f, 2f, 2.8f));
                c.Hit(u, 0.8f, "stunned", c.L(1.5f, 2f, 2.8f));
                c.Dust(u.current_position, 1f);
            }
            yield return c.W(0.4f);
        }

        private static IEnumerator SvLunarClaw(Cast c)
        {
            Vector3 t = c.TP;
            c.F("claw", t, 3f, c.Ang - 90f, 0f, false, 24f, true);
            c.Snd("slash_heavy", t);
            c.Hit(c.T, 1.2f);
            yield return c.W(0.15f);
            Vector3 side = Cast.Rot(c.Dir, 90f) * 3f;
            c.Shot("crescent_blade", t + side - c.Dir, t - side + c.Dir, 1.6f, 0.2f);
            c.Shot("crescent_blade", t - side - c.Dir, t + side + c.Dir, 1.6f, 0.2f);
            c.Snd("moon_hum", t);
            yield return c.W(0.12f);
            c.F("x_cut", t, 2.2f, 0f, 0f, false, 24f, true);
            c.Hit(c.T, 1.6f);
            c.Impact(t, 0.6f);
        }

        private static IEnumerator SvAbyssRift(Cast c)
        {
            Vector3 t = c.TP;
            c.F("rift", t + new Vector3(0f, 1f), c.L(2f, 2.4f, 3f), 0f, 1.6f, true, 12f);
            c.Snd("portal_open", t);
            for (int i = 0; i < 4; i++)
            {
                c.PullAll(t, c.L(4f, 5f, 6f), 0.25f);
                c.Area(t, 1.2f, 0.4f);
                yield return c.W(0.25f);
            }
            for (int i = 0; i < 6; i++)
                c.F("abyss_flame", t + Cast.Rot(Vector3.right, i * 60f) * 1f + new Vector3(0f, 0.8f), 1.8f, 0f, 0f, false, 18f);
            c.Snd("dark_pulse", t);
            c.Burst(t, 3f);
            c.Area(t, 2.2f, 2.2f);
            c.Impact(t, 0.8f);
        }

        private static IEnumerator SvSovereignWill(Cast c)
        {
            c.F("moon_pillar", c.P + new Vector3(0f, 2f), 2.4f, 0f, 1.2f, false, 14f);
            c.Snd("holy_choir");
            c.A.addStatusEffect("invincible", c.L(2f, 3f, 4f));
            c.A.addStatusEffect("powerup", c.L(5f, 6f, 8f));
            c.Heal(c.A, c.L(0.05f, 0.08f, 0.12f));
            yield return c.W(0.4f);
            c.PushAll(c.P, 3f, 2f);
        }

        private static IEnumerator SvMoonfall(Cast c)
        {
            Vector3 t = c.TP;
            float r = c.L(4.5f, 5.5f, 7f);
            for (int i = 0; i < 6; i++)
                c.F("moon_pillar", t + Cast.Rot(Vector3.right, i * 60f) * r + new Vector3(0f, 2f), 1.8f, 0f, 1.8f, false, 14f);
            c.Snd("moon_hum", t);
            Fx.Instance moon = c.Fc("fx:orb", t + new Vector3(0f, 10f), 2f, new Color(0.9f, 0.88f, 1f), 0f, 1.3f, true, 10f);
            Cast.Grow(moon, 2.5f);
            Fx.MoveTo(moon, t, 1.2f, false);
            yield return c.W(1.2f);
            c.Burst(t, r * 1.6f);
            c.F("star_rift", t, r * 1.2f, 0f, 0f, false, 16f);
            c.Crater(t, r * 0.6f);
            for (int i = 0; i < 6; i++) c.Fissure(t, t + Cast.Rot(Vector3.right, i * 60f + 30f) * r * 1.3f, 1.2f);
            c.Snd("explosion_big", t);
            c.Snd("crater_boom", t);
            c.Impact(t, 1f);
            c.Area(t, r, 4f);
            c.PushAll(t, r + 1f, 3f);
        }

        private static IEnumerator SvTwilight(Cast c)
        {
            Vector3 p = c.P;
            List<Actor> foes = SvTargets(c, p, 25f, 50);
            c.Snd("moon_hum", p);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("moon_pillar", (Vector3)u.current_position + new Vector3(0f, 2f), 2f, 0f, 0f, false, 16f);
                c.Hit(u, 2.5f);
                yield return c.W(0.03f);
            }
            yield return c.W(0.4f);
            c.Snd("abyss_hum", p);
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.F("abyss_flame", (Vector3)u.current_position + new Vector3(0f, 0.8f), 2f, 0f, 0f, false, 18f);
                c.Hit(u, 2.5f);
            }
            yield return c.W(0.5f);
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance r = c.F("roar", p, 6f + i * 6f, 0f, 0.7f, false, 14f, true);
                Cast.Grow(r, 3f);
                c.Snd("dragon_roar", p);
                Cast.Shake(1f);
                yield return c.W(0.2f);
            }
            foreach (Actor u in foes)
            {
                if (!CombatCore.Alive(u)) continue;
                c.Hit(u, 3f);
                c.Push(u, (Vector3)u.current_position - p, 3f, 0.4f);
            }
            c.Impact(p, 1f);
        }
    }
}
