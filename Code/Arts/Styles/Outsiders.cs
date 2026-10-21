using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>Tier 1 outsiders: Delusion users and Allogenes.</summary>
    public static partial class Styles
    {
        static partial void RegisterOutsiders()
        {
            DelusionStyle();
            AllogeneStyle();
        }

        /// <summary>Spends a share of the user's own health (never the last point).</summary>
        private static void PayLife(Cast c, float pFraction)
        {
            if (!c.Ok) return;
            float cost = Mathf.Min(c.A.getHealth() - 1f, c.A.getMaxHealth() * pFraction);
            if (cost >= 1f) c.A.getHit(cost, false, AttackType.Other, null, false, false, false);
        }

        // ================================================================ Delusion · Crimson Overdrive
        // A Fatui brawler-gunslinger who burns their own life: every big move costs blood and hits like a truck.

        private static void DelusionStyle()
        {
            Library.Style("gi_delusion", "Crimson Overdrive", 1, "delusion")
                .T("blood_rush", "Blood Rush", Shape.Gap, 8f, 5f, BloodRush,
                    "Pays blood to zigzag through everyone between the user and the target.")
                .T("gun_barrage", "Redline Barrage", Shape.Line, 7f, 6f, GunBarrage,
                    "A gun fanned at the nearest foes, shot after shot. At III the last round ricochets.")
                .T("haymaker", "Overdrive Haymaker", Shape.Lone, 2.2f, 7f, Haymaker,
                    "A charged punch that freezes the moment and sends the target skidding.")
                .T("redline_stomp", "Redline Stomp", Shape.Crowd, 3.5f, 9f, RedlineStomp,
                    "Leaps, hangs, and drops in a burst of burning life that throws a crowd into the air.")
                .T("adrenal_spike", "Adrenal Spike", Shape.Guard, 0f, 18f, AdrenalSpike,
                    "Three heartbeats: the body forces itself back into the fight.")
                .T("burnout", "Burnout", Shape.Crowd, 5f, 24f, Burnout,
                    "Burns a quarter of the user's life to blink through every foe nearby, then explodes.", 2, 2.5f, true);
        }

        private static IEnumerator BloodRush(Cast c)
        {
            PayLife(c, 0.03f);
            c.F("lifeburn", c.P, 1.4f, 0f, 0f, false, 24f);
            c.Snd("heartbeat");
            Vector3 start = c.P, end = c.Behind(c.T, 1.2f);
            int zigs = c.Li(3, 4, 5);
            Vector3 side = Cast.Rot(c.Dir, 90f);
            for (int i = 1; i <= zigs; i++)
            {
                Vector3 at = Vector3.Lerp(start, end, i / (float)zigs) + side * (i == zigs ? 0f : (i % 2 == 0 ? 0.9f : -0.9f));
                Vector3 from = c.P;
                yield return c.Dash(at, 0.06f);
                c.Beam("iai", from, c.P, 0.45f, 0.2f, true, false);
                c.Snd("dash");
                foreach (Actor u in CombatCore.EnemiesAlong(c.A, from, c.P, 0.8f))
                {
                    c.Hit(u, 0.45f);
                    c.Spark(u.current_position, 1f);
                }
                yield return c.W(0.03f);
            }
            c.Snd("slash_heavy");
            yield return c.W(0.15f);
        }

        private static IEnumerator GunBarrage(Cast c)
        {
            int shots = c.Li(4, 6, 9);
            List<Actor> foes = c.Foes(c.P, 8f);
            foes.Sort((x, y) => Vector2.Distance(x.current_position, c.P).CompareTo(Vector2.Distance(y.current_position, c.P)));
            if (foes.Count == 0 && c.TOk) foes.Add(c.T);
            if (foes.Count == 0) yield break;
            Actor last = null;
            for (int i = 0; i < shots; i++)
            {
                Actor v = foes[i % Mathf.Min(foes.Count, 3)];
                if (!CombatCore.Alive(v)) continue;
                CombatCore.Face(c.A, v.current_position);
                Vector3 muzzle = c.P + ((Vector3)v.current_position - c.P).normalized * 0.5f;
                c.F("impact", muzzle, 0.6f, 0f, 0f, false, 32f, true);
                c.Shot("bullet", muzzle, v.current_position, 0.7f, 0.08f);
                c.Snd("gunshot");
                yield return c.W(0.08f);
                c.Hit(v, 0.4f);
                c.Spark(v.current_position, 0.7f);
                last = v;
                yield return c.W(0.05f);
            }
            if (c.Lv >= 3 && CombatCore.Alive(last))
            {
                // The final round skips off the target into the next one.
                Actor next = c.NearestFoe(last.current_position, 5f, last);
                if (next != null)
                {
                    c.Shot("bullet", last.current_position, next.current_position, 0.8f, 0.1f);
                    c.Snd("deflect", last.current_position);
                    yield return c.W(0.1f);
                    c.Hit(next, 1f);
                    c.Spark(next.current_position, 1.2f);
                }
            }
        }

        private static IEnumerator Haymaker(Cast c)
        {
            PayLife(c, 0.04f);
            Fx.Instance charge = c.F("lifeburn", c.P, 1.2f, 0f, 0.55f, true, 18f);
            Cast.Stick(charge, c.A);
            Cast.Grow(charge, 1.8f);
            c.Snd("charge");
            yield return c.W(c.L(0.5f, 0.45f, 0.4f));
            if (!c.TOk) yield break;
            yield return c.Dash(c.Behind(c.T, -0.6f), 0.07f);
            Vector3 at = c.TP;
            c.F("impact", at, c.L(1.8f, 2.2f, 2.8f), c.Ang, 0f, false, 24f, true);
            c.F("shock", at, 1.6f, 0f, 0f, false, 24f, true);
            c.Snd("punch", at);
            c.Snd("crater_boom", at);
            c.Hit(c.T, c.L(1.8f, 2.1f, 2.6f));
            c.Impact(at, c.L(0.6f, 0.75f, 1f));
            if (c.TOk) yield return c.Knock(c.T, c.Dir, c.L(3f, 4f, 5.5f), 0.35f);
            if (c.Lv >= 2 && c.TOk) c.Crater(c.TP, 1f);
        }

        private static IEnumerator RedlineStomp(Cast c)
        {
            PayLife(c, 0.05f);
            Vector3 land = c.TP;
            c.Snd("leap");
            yield return Cast.Arc(c.A, land, c.L(3f, 3.8f, 4.5f), 0.32f, c.Hot);
            Cast.SetHeight(c.A, c.L(2.5f, 3f, 3.6f));
            c.F("lifeburn", c.P, 1.5f, 0f, 0.3f, true, 22f);
            yield return Cast.Hang(c.A, 0.22f);
            yield return Cast.Fall(c.A, 0.1f);
            Vector3 p = c.P;
            float r = c.L(2.2f, 2.7f, 3.3f);
            c.F("lifeburn", p, r * 1.4f, 0f, 0f, false, 20f);
            c.Burst(p, r * 1.3f);
            c.Crater(p, r * 0.6f);
            c.Snd("slam", p);
            c.Snd("explosion_small", p);
            c.Impact(p, c.L(0.5f, 0.7f, 0.9f));
            foreach (Actor u in c.Foes(p, r))
            {
                c.Hit(u, 1.1f);
                c.Launch(u, Random.Range(1.5f, 2.6f), 0.25f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator AdrenalSpike(Cast c)
        {
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance pulse = c.F("lifeburn", c.P, 1.2f + i * 0.4f, 0f, 0f, false, 22f);
                Cast.Stick(pulse, c.A);
                c.Snd("heartbeat");
                yield return c.W(0.3f);
            }
            c.Heal(c.A, c.L(0.15f, 0.2f, 0.28f));
            c.A.addStatusEffect("powerup", c.L(5f, 7f, 9f));
            c.A.addStatusEffect("dash", 4f);
            c.Say("ADRENALINE", c.P + new Vector3(0f, 1.3f), 11);
        }

        private static IEnumerator Burnout(Cast c)
        {
            PayLife(c, 0.25f);
            Vector3 origin = c.P;
            for (int i = 0; i < 3; i++)
            {
                c.Snd("heartbeat", origin);
                Fx.Instance ring = c.F("lifeburn", origin, 1.5f + i, 0f, 0f, false, 20f);
                Cast.Grow(ring, 1.4f);
                Cast.Shake(0.2f + 0.1f * i);
                yield return c.W(0.25f);
            }
            int blinks = c.Li(6, 8, 10);
            List<Actor> foes = c.Foes(origin, 6f);
            for (int i = 0; i < blinks && foes.Count > 0; i++)
            {
                Actor v = foes[i % foes.Count];
                if (!CombatCore.Alive(v)) continue;
                Vector3 from = c.P;
                Vector3 side = (Vector3)v.current_position + (Vector3)Random.insideUnitCircle.normalized * 0.8f;
                c.Blink(side, "impact");
                c.Beam("iai", from, c.P, 0.35f, 0.15f, true, false);
                CombatCore.Face(c.A, v.current_position);
                c.F("x_cut", v.current_position, 1.4f, Random.Range(0f, 90f), 0f, false, 30f, true);
                c.Snd(i % 2 == 0 ? "punch" : "slash_light", v.current_position);
                c.Hit(v, 0.6f);
                yield return c.W(0.07f);
            }
            c.Blink(origin, "impact");
            yield return c.W(0.15f);
            float r = c.L(3.5f, 4.2f, 5f);
            c.F("lifeburn", origin, r * 1.6f, 0f, 0f, false, 16f);
            c.Burst(origin, r * 1.5f);
            c.Crater(origin, r * 0.7f);
            c.Snd("explosion_big", origin);
            c.Impact(origin, 1f);
            c.Area(origin, r, 2f);
            c.PushAll(origin, r + 0.5f, 2.5f);
        }

        // ================================================================ Allogene · Starless Wanderer
        // A visitor from beyond the sky: summoned star swords, rifts in space and mirrors of starlight.

        private static void AllogeneStyle()
        {
            Library.Style("gi_allogene", "Starless Wanderer", 1, "anemo").VisionElement()
                .T("star_volley", "Star Sword Volley", Shape.Line, 8f, 6f, StarVolley,
                    "Star swords gather overhead, then fire one by one down the line.")
                .T("rift_step", "Rift Step", Shape.Gap, 9f, 6f, RiftStep,
                    "Steps into a rift and out behind the foe, kicking them back the way they came.")
                .T("mirror_hall", "Mirror Hall", Shape.Guard, 0f, 16f, MirrorHall,
                    "Starlit mirrors orbit the user, dodging blows and shooting shards back.")
                .T("cosmic_cleave", "Cosmic Cleave", Shape.Lone, 2.5f, 7f, CosmicCleave,
                    "From above: two crossing cuts, then a tear in space that pins the foe.")
                .T("starfall_shower", "Starfall Shower", Shape.Crowd, 6f, 10f, StarfallShower,
                    "A slanting shower of falling stars over an area.")
                .T("falling_heaven", "Falling Heaven", Shape.Crowd, 6f, 22f, FallingHeaven,
                    "A rift to the starless void drags everything in, then collapses outward.", 2, 2.5f, true);
        }

        private static IEnumerator StarVolley(Cast c)
        {
            int count = c.Li(3, 4, 5);
            var slots = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                slots[i] = c.P + new Vector3((i - (count - 1) / 2f) * 0.7f, 1.4f + Mathf.Abs(i - (count - 1) / 2f) * -0.2f);
                c.F("star_sword", slots[i], 1.1f, -90f, 0.15f + 0.08f * i, false, 16f);
                c.F("impact", slots[i], 0.6f, 0f, 0f, false, 30f, true);
                c.Snd("star_chime", slots[i]);
                yield return c.W(0.08f);
            }
            yield return c.W(0.15f);
            Vector3 dir = c.Dir;
            for (int i = 0; i < count; i++)
            {
                Vector3 to = c.TP + dir * 3f + Cast.Rand(0.4f);
                c.Shot("star_sword", slots[i], to, 1.2f, 0.16f);
                c.Snd("whoosh_heavy", slots[i]);
                yield return c.W(0.12f);
                c.Lane(slots[i], to, 0.6f, 0.75f);
                c.Spark(to, 0.9f);
            }
        }

        private static IEnumerator RiftStep(Cast c)
        {
            Vector3 origin = c.P;
            c.F("star_rift", origin, 1.6f, 0f, 0.6f, false, 18f);
            c.Snd("portal_open");
            yield return c.W(0.18f);
            Vector3 exit = c.Behind(c.T, 0.9f);
            c.F("star_rift", exit, 1.6f, 0f, 0.6f, false, 18f);
            c.Blink(exit, "vacuum");
            CombatCore.Face(c.A, c.TP);
            yield return c.W(0.08f);
            if (!c.TOk) yield break;
            c.F("impact", c.TP, 1.6f, Cast.AngleOf(origin - c.TP), 0f, false, 26f, true);
            c.Snd("kick");
            c.Hit(c.T, c.L(1f, 1.3f, 1.6f));
            Vector3 back = origin - c.TP;
            yield return c.Knock(c.T, back, Mathf.Min(back.magnitude, c.L(3f, 4f, 5f)), 0.3f);
            if (c.Lv >= 3 && c.TOk)
            {
                // Hurled back into the first rift, which snaps shut on them.
                c.F("star_rift", c.TP, 1.4f, 0f, 0f, false, 26f);
                c.Snd("space_warp", c.TP);
                c.Hit(c.T, 0.8f);
            }
        }

        private static IEnumerator MirrorHall(Cast c)
        {
            int count = c.Li(2, 3, 4);
            float life = c.L(3f, 4f, 5f);
            c.A.addStatusEffect("dodge", life);
            c.Snd("glass_break");
            var mirrors = new Fx.Instance[count];
            for (int i = 0; i < count; i++) mirrors[i] = c.F("mirror", c.P, 0.9f, 0f, life, true, 12f);
            ModClass.Instance.StartCoroutine(OrbitMirrors(c, mirrors, life));
            yield return c.W(0.3f);
        }

        private static IEnumerator OrbitMirrors(Cast c, Fx.Instance[] mirrors, float life)
        {
            float t = 0f, next_shot = 0.4f;
            int shooter = 0;
            while (t < life && c.Ok)
            {
                t += Time.unscaledDeltaTime * Cinematics.Director.FxTimeScale;
                for (int i = 0; i < mirrors.Length; i++)
                {
                    if (mirrors[i] == null || !mirrors[i].Active) continue;
                    float a = t * 3f + i * Mathf.PI * 2f / mirrors.Length;
                    mirrors[i].Go.transform.position = c.P + new Vector3(Mathf.Cos(a), Mathf.Sin(a) * 0.7f) * 1.1f;
                }
                if (t >= next_shot)
                {
                    next_shot = t + 0.5f;
                    Actor foe = c.NearestFoe(c.P, 5f);
                    Fx.Instance m = mirrors[shooter++ % mirrors.Length];
                    if (foe != null && m != null && m.Active)
                    {
                        c.Shot("moon_shard", m.Go.transform.position, foe.current_position, 0.6f, 0.12f, true);
                        c.Snd("star_chime", foe.current_position);
                        c.Hit(foe, 0.3f);
                    }
                }
                yield return null;
            }
        }

        private static IEnumerator CosmicCleave(Cast c)
        {
            if (!c.TOk) yield break;
            Vector3 above = c.TP + new Vector3(0f, 0.3f);
            c.Blink(above, "vacuum");
            Cast.SetHeight(c.A, 2.2f);
            yield return c.W(0.1f);
            c.F("x_cut", c.TP, c.L(1.6f, 1.9f, 2.3f), 20f, 0f, false, 26f, true);
            c.Snd("slash_heavy");
            c.Hit(c.T, 0.8f);
            yield return Cast.Fall(c.A, 0.12f);
            c.F("x_cut", c.TP, c.L(1.6f, 1.9f, 2.3f), 65f, 0f, false, 26f, true);
            c.Snd("slash_heavy");
            c.Hit(c.T, 0.9f);
            yield return c.W(0.1f);
            if (!c.TOk) yield break;
            c.F("star_rift", c.TP, c.L(1.4f, 1.8f, 2.4f), 0f, 0f, false, 20f);
            c.Snd("space_warp", c.TP);
            Cast.Hold(c.T, c.L(1f, 1.5f, 2f));
            c.Impact(c.TP, c.L(0.4f, 0.55f, 0.75f));
        }

        private static IEnumerator StarfallShower(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(2.5f, 3.2f, 4f);
            int stars = c.Li(8, 12, 18);
            Vector3 slant = new Vector3(-2.5f, 5f);
            for (int i = 0; i < stars; i++)
            {
                Vector3 at = p + Cast.Rand(r);
                c.Shot("moon_shard", at + slant, at, 0.7f, 0.18f, true);
                ModClass.Instance.StartCoroutine(StarLands(c, at));
                if (i % 3 == 0) c.Snd("star_chime", at);
                yield return c.W(0.06f);
            }
            yield return c.W(0.3f);
        }

        private static IEnumerator StarLands(Cast c, Vector3 at)
        {
            yield return c.W(0.18f);
            c.F("impact", at, 0.8f, Random.Range(0f, 360f), 0f, false, 26f, true);
            c.Area(at, 0.8f, 0.35f);
        }

        private static IEnumerator FallingHeaven(Cast c)
        {
            Vector3 p = c.TP;
            float r = c.L(4f, 5f, 6f);
            Fx.Instance rift = c.F("star_rift", p, r * 0.9f, 0f, 2.4f, true, 12f);
            Cast.Grow(rift, 1.4f);
            c.Snd("portal_open", p);
            c.Snd("rumble", p);
            float t = 0f;
            while (t < 1.6f)
            {
                c.PullAll(p, r, 0.3f);
                c.Area(p, r * 0.6f, 0.25f);
                c.Shot("moon_shard", p + Cast.Rand(r), p, 0.5f, 0.25f, true);
                Cast.Shake(0.2f);
                t += 0.3f;
                yield return c.W(0.3f);
            }
            // Collapse: shards fly back out.
            c.Burst(p, r * 1.3f);
            for (int i = 0; i < 12; i++)
            {
                Vector3 d = Cast.Rot(Vector3.right, i * 30f);
                c.Shot("moon_shard", p, p + d * r, 0.8f, 0.2f, true);
            }
            c.Snd("explosion_big", p);
            c.Impact(p, 1f);
            c.Area(p, r * 0.8f, 2.5f);
            c.PushAll(p, r, 2f);
            c.Crater(p, r * 0.5f);
            yield return c.W(0.3f);
        }
    }
}
