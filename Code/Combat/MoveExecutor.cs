using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Cinematics;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>Plays a move: animation, VFX, sound, damage, statuses, impact frames and cutscenes.</summary>
    public static class MoveExecutor
    {
        public static void Cast(Actor pCaster, Actor pTarget, MoveAsset pMove, MoveSet pSet)
        {
            ModClass.Instance.StartCoroutine(Run(pCaster, pTarget, pMove, pSet));
        }

        public static IEnumerator Run(Actor pCaster, Actor pTarget, MoveAsset pMove, MoveSet pSet)
        {
            if (!CombatCore.Alive(pCaster)) yield break;
            string element = CombatCore.ElementOf(pMove, pSet);
            Color color = Elements.Color(element);
            float scale = pMove.Scale;
            Vector3 target_pos = CombatCore.Alive(pTarget) ? CombatCore.Pos(pTarget) : CombatCore.Pos(pCaster);
            CombatCore.Face(pCaster, target_pos);

            bool visible = Director.OnScreen(CombatCore.Pos(pCaster)) || Director.OnScreen(target_pos);
            if (visible && pSet != null && pSet.Tier >= 2)
            {
                FloatingText.Show(pMove.Name, CombatCore.Pos(pCaster), color, pMove.Archetype == "ultimate" ? 15 : 10);
            }

            switch (pMove.Archetype)
            {
                case "strike": yield return Strike(pCaster, pTarget, pMove, element, color, scale); break;
                case "barrage": yield return Barrage(pCaster, pTarget, pMove, element, color, scale); break;
                case "dash": yield return Dash(pCaster, pTarget, pMove, element, color, scale); break;
                case "teleport": yield return Teleport(pCaster, pTarget, pMove, element, color, scale); break;
                case "projectile": yield return Projectile(pCaster, pTarget, pMove, element, color, scale); break;
                case "beam": yield return Beam(pCaster, pTarget, pMove, element, color, scale); break;
                case "nova": yield return Nova(pCaster, pMove, element, color, scale); break;
                case "field": yield return Field(pCaster, target_pos, pMove, element, color, scale); break;
                case "rain": yield return Rain(pCaster, target_pos, pMove, element, color, scale); break;
                case "pillar": yield return Pillar(pCaster, target_pos, pMove, element, color, scale); break;
                case "shield":
                case "buff": SelfBuff(pCaster, pMove, color, scale); break;
                case "heal": Heal(pCaster, pMove, color, scale); break;
                case "ultimate": yield return Ultimate(pCaster, pTarget, pMove, pSet, element, color, scale); break;
            }

            if (pMove.Impact == "major")
            {
                Director.Impact(target_pos, color, 1f);
                if (pMove.Archetype != "ultimate" && pMove.Archetype != "pillar") Crater(target_pos, 2.5f * scale);
            }
            else if (pMove.Impact == "minor" && pSet != null && pSet.Tier >= 2) Director.Impact(target_pos, color, 0.45f);
        }

        // ---------------------------------------------------------------- melee

        private static IEnumerator Strike(Actor c, Actor t, MoveAsset m, string e, Color col, float s)
        {
            if (!CombatCore.Alive(t)) yield break;
            Vector3 p = CombatCore.Pos(t);
            CombatCore.Sound(m.Sfx, p);
            float angle = Angle(c, t) + Random.Range(-25f, 25f);
            Fx.Play("smear", p, 2.6f * s, new Color(col.r, col.g, col.b, 0.6f), angle, 0f, false, 26f, "EffectsBack");
            Fx.Play(m.Fx ?? "slash", p, 2.2f * s, col, angle);
            yield return CombatCore.Wait(0.06f);
            Fx.Play(m.Fx2 ?? "hit", p, 1.4f * s, Color.white);
            CombatCore.Sound(m.Sfx2, p);
            HitArea(c, t, p, m, e);
        }

        private static IEnumerator Barrage(Actor c, Actor t, MoveAsset m, string e, Color col, float s)
        {
            for (int i = 0; i < Mathf.Max(1, m.Hits); i++)
            {
                if (!CombatCore.Alive(t) || !CombatCore.Alive(c)) yield break;
                Vector3 p = CombatCore.Pos(t) + (Vector3)Random.insideUnitCircle * 0.35f;
                Fx.Play(m.Fx ?? "slash", p, 1.8f * s, col, Random.Range(0f, 360f), 0f, false, 26f);
                if (i % 2 == 0) CombatCore.Sound(m.Sfx, p);
                Fx.Play(m.Fx2 ?? "hit", p, 1.1f * s, Color.white);
                HitArea(c, t, p, m, e);
                yield return CombatCore.Wait(m.Interval);
            }
            CombatCore.Sound(m.Sfx2, CombatCore.Pos(t));
        }

        private static IEnumerator Dash(Actor c, Actor t, MoveAsset m, string e, Color col, float s)
        {
            if (!CombatCore.Alive(t)) yield break;
            Vector3 from = CombatCore.Pos(c);
            Vector3 dir = ((Vector3)CombatCore.Pos(t) - from).normalized;
            Vector3 to = CombatCore.Pos(t) - dir * 0.9f;
            CombatCore.Sound(m.Sfx, from);
            CombatCore.Sound("whoosh", from);
            Fx.Line(m.Fx ?? "streak", from, to, 0.9f * s, col, 0.3f);
            Fx.Play("vanish", from, 1.6f, col);
            yield return CombatCore.Slide(c, to, 0.12f, Settings.Afterimages ? col : (Color?)null);
            if (!CombatCore.Alive(t)) yield break;
            Vector3 p = CombatCore.Pos(t);
            Fx.Play(m.Fx2 ?? "slash_big", p, 3f * s, col, Angle(c, t));
            Fx.Play("hit", p, 1.6f * s, Color.white);
            CombatCore.Sound(m.Sfx2, p);
            if (m.Radius > 0f) Fx.Play("nova", to, m.Radius * 2f, col);
            HitArea(c, t, p, m, e);
        }

        private static IEnumerator Teleport(Actor c, Actor t, MoveAsset m, string e, Color col, float s)
        {
            if (!CombatCore.Alive(t)) yield break;
            Vector3 from = CombatCore.Pos(c);
            Vector3 tp = CombatCore.Pos(t);
            Vector3 behind = tp + (tp - from).normalized * 1f;
            Fx.Play(m.Fx ?? "vanish", from, 2f * s, col);
            if (Settings.Afterimages) Fx.Ghost(c, col, 0.5f);
            CombatCore.Sound(m.Sfx, from);
            yield return CombatCore.Wait(0.08f);
            c.setCurrentTilePosition(CombatCore.TileAt(behind));
            c.current_position = behind;
            Fx.Play(m.Fx ?? "vanish", behind, 2f * s, col);
            CombatCore.Face(c, tp);
            yield return CombatCore.Wait(0.06f);
            if (!CombatCore.Alive(t)) yield break;
            Fx.Play(m.Fx2 ?? "slash_big", CombatCore.Pos(t), 3f * s, col, Angle(c, t));
            Fx.Play("hit", CombatCore.Pos(t), 1.6f * s, Color.white);
            CombatCore.Sound(m.Sfx2, CombatCore.Pos(t));
            if (m.Radius > 0f) Fx.Play("nova", CombatCore.Pos(t), m.Radius * 2f, col);
            HitArea(c, t, CombatCore.Pos(t), m, e);
        }

        // ---------------------------------------------------------------- ranged

        private static IEnumerator Projectile(Actor c, Actor t, MoveAsset m, string e, Color col, float s)
        {
            Vector3 from = CombatCore.Pos(c);
            Vector3 to = CombatCore.Alive(t) ? (Vector3)CombatCore.Pos(t) : from;
            int count = Mathf.Max(1, m.Hits);
            CombatCore.Sound(m.Sfx, from);
            for (int i = 0; i < count; i++)
            {
                Vector3 aim = CombatCore.Alive(t) ? (Vector3)CombatCore.Pos(t) : to;
                float travel = Mathf.Clamp(Vector3.Distance(from, aim) / 16f, 0.12f, 0.6f);
                Fx.Instance orb = Fx.Play(m.Fx ?? "orb", from, 1.2f * s, col, 0f, travel, true, 20f);
                Fx.MoveTo(orb, aim, travel);
                yield return CombatCore.Wait(travel);
                Fx.Play(m.Fx2 ?? "burst", aim, Mathf.Max(1.6f, m.Radius * 2f) * s, col);
                CombatCore.Sound(m.Sfx2, aim);
                HitArea(c, t, aim, m, e);
                if (i < count - 1) yield return CombatCore.Wait(m.Interval);
            }
        }

        private static IEnumerator Beam(Actor c, Actor t, MoveAsset m, string e, Color col, float s)
        {
            if (!CombatCore.Alive(t)) yield break;
            Vector3 from = CombatCore.Pos(c);
            Vector3 dir = ((Vector3)CombatCore.Pos(t) - from).normalized;
            Vector3 to = from + dir * Mathf.Max(2f, m.Range);
            CombatCore.Sound(m.Sfx, from);
            Fx.Line(m.Fx ?? "beam", from, to, Mathf.Max(0.6f, m.Radius * 1.6f) * s, col, m.Duration);
            Fx.Play("burst", from + dir * 0.6f, 1.6f * s, col);
            int ticks = Mathf.Max(1, m.Hits);
            for (int i = 0; i < ticks; i++)
            {
                if (!CombatCore.Alive(c)) yield break;
                if (i % 2 == 0) Fx.Play(m.Fx2 ?? "hit", to - dir * Random.Range(0f, m.Range), 1.6f * s, col);
                CombatCore.HitAll(c, CombatCore.EnemiesAlong(c, from, to, Mathf.Max(0.6f, m.Radius)), m.Power, e,
                    m.Status, m.StatusTime);
                yield return CombatCore.Wait(m.Interval);
            }
        }

        // ---------------------------------------------------------------- areas

        private static IEnumerator Nova(Actor c, MoveAsset m, string e, Color col, float s)
        {
            Vector3 p = CombatCore.Pos(c);
            CombatCore.Sound(m.Sfx, p);
            Fx.Instance ring = Fx.Play(m.Fx ?? "nova", p, m.Radius * 1.2f * s, col);
            Fx.Grow(ring, 1.8f);
            if (!string.IsNullOrEmpty(m.Fx2)) Fx.Play(m.Fx2, p, m.Radius * 1.4f * s, col);
            yield return CombatCore.Wait(0.1f);
            CombatCore.HitAll(c, CombatCore.EnemiesNear(c, p, m.Radius), m.Power, e, m.Status, m.StatusTime);
            CombatCore.Sound(m.Sfx2, p);
        }

        private static IEnumerator Field(Actor c, Vector3 p, MoveAsset m, string e, Color col, float s)
        {
            CombatCore.Sound(m.Sfx, p);
            Fx.Play("circle", p, m.Radius * 2.2f * s, col, 0f, m.Duration, true, 14f, "EffectsBack");
            Fx.Play(m.Fx ?? "tornado", p, m.Radius * 2f * s, col, 0f, m.Duration, true);
            int ticks = Mathf.Max(1, m.Hits);
            float gap = Mathf.Max(m.Interval, m.Duration / ticks);
            for (int i = 0; i < ticks; i++)
            {
                if (!CombatCore.Alive(c)) yield break;
                CombatCore.HitAll(c, CombatCore.EnemiesNear(c, p, m.Radius), m.Power, e, m.Status, m.StatusTime);
                yield return CombatCore.Wait(gap);
            }
        }

        private static IEnumerator Rain(Actor c, Vector3 p, MoveAsset m, string e, Color col, float s)
        {
            CombatCore.Sound(m.Sfx, p);
            int count = Mathf.Max(1, m.Hits);
            for (int i = 0; i < count; i++)
            {
                if (!CombatCore.Alive(c)) yield break;
                Vector3 spot = p + (Vector3)(Random.insideUnitCircle * m.Radius);
                if (m.Fx == "orb")
                {
                    Fx.Instance drop = Fx.Play("orb", spot + new Vector3(2f, 7f), 1.4f * s, col, 0f, 0.22f, true);
                    Fx.MoveTo(drop, spot, 0.22f);
                    ModClass.Instance.StartCoroutine(Land(c, spot, m, e, col, s, 0.22f));
                }
                else
                {
                    Fx.Play(m.Fx ?? "pillar", spot + new Vector3(0f, 1.6f * s), 2.4f * s, col);
                    ModClass.Instance.StartCoroutine(Land(c, spot, m, e, col, s, 0.05f));
                }
                yield return CombatCore.Wait(m.Interval);
            }
        }

        private static IEnumerator Land(Actor c, Vector3 spot, MoveAsset m, string e, Color col, float s, float delay)
        {
            yield return CombatCore.Wait(delay);
            Fx.Play(m.Fx2 ?? "burst", spot, 1.8f * s, col);
            CombatCore.Sound(m.Sfx2 ?? m.Sfx, spot);
            if (CombatCore.Alive(c))
            {
                CombatCore.HitAll(c, CombatCore.EnemiesNear(c, spot, 1.2f), m.Power, e, m.Status, m.StatusTime);
            }
        }

        private static IEnumerator Pillar(Actor c, Vector3 p, MoveAsset m, string e, Color col, float s)
        {
            CombatCore.Sound(m.Sfx, p);
            Fx.Play("circle", p, m.Radius * 2f, col, 0f, 0.5f, true, 14f, "EffectsBack");
            yield return CombatCore.Wait(0.35f);
            Fx.Play(m.Fx ?? "pillar", p + new Vector3(0f, 2.4f * s), 4f * s, col);
            Fx.Instance ring = Fx.Play(m.Fx2 ?? "nova", p, m.Radius * 1.4f, col);
            Fx.Grow(ring, 1.6f);
            CombatCore.Sound(m.Sfx2, p);
            Crater(p, m.Radius * 1.4f);
            CombatCore.HitAll(c, CombatCore.EnemiesNear(c, p, m.Radius), m.Power, e, m.Status, m.StatusTime);
        }

        // ---------------------------------------------------------------- support

        private static void SelfBuff(Actor c, MoveAsset m, Color col, float s)
        {
            Vector3 p = CombatCore.Pos(c);
            CombatCore.Sound(m.Sfx, p);
            Fx.Instance fx = Fx.Play(m.Fx ?? "aura", p, 2.6f * s, col, 0f, Mathf.Min(4f, m.StatusTime), true, 12f);
            Fx.Attach(fx, c);
            if (!string.IsNullOrEmpty(m.SelfStatus)) c.addStatusEffect(m.SelfStatus, m.StatusTime);
        }

        private static void Heal(Actor c, MoveAsset m, Color col, float s)
        {
            CombatCore.Sound(m.Sfx, CombatCore.Pos(c));
            Fx.Play("nova", CombatCore.Pos(c), m.Radius * 1.6f, col);
            foreach (Actor ally in CombatCore.AlliesNear(c, Mathf.Max(1f, m.Radius)))
            {
                Fx.Instance fx = Fx.Play(m.Fx ?? "aura", CombatCore.Pos(ally), 1.8f * s, col, 0f, 1.2f, true, 12f);
                Fx.Attach(fx, ally);
                ally.restoreHealth(Mathf.CeilToInt(ally.getMaxHealth() * m.Heal));
            }
        }

        // ---------------------------------------------------------------- ultimates

        private static IEnumerator Ultimate(Actor c, Actor t, MoveAsset m, MoveSet set, string e, Color col, float s)
        {
            Vector3 p = CombatCore.Alive(t) ? (Vector3)CombatCore.Pos(t) : CombatCore.Pos(c);

            if (m.Cutscene && Director.OnScreen(CombatCore.Pos(c)) && Director.CanPlayCutscene())
            {
                yield return Director.Cutscene(m.Name, c.getName(), CombatCore.Pos(c), col, 1.1f);
                if (!CombatCore.Alive(c)) yield break;
                if (CombatCore.Alive(t)) p = CombatCore.Pos(t);
            }

            // Charge: a magic circle under the caster, a gathering aura, and the spotlight
            CombatCore.Sound(m.Sfx, CombatCore.Pos(c));
            if (set != null && set.Tier >= 2 && Director.OnScreen(CombatCore.Pos(c)))
            {
                ModClass.Instance.StartCoroutine(Director.Spotlight(CombatCore.Pos(c), 1.1f));
            }
            for (int i = 0; i < 2; i++)
            {
                Fx.Instance gather = Fx.Play("nova", CombatCore.Pos(c), 4.5f * s, col, 0f, 0.5f);
                Fx.Grow(gather, 0.2f);
            }
            Fx.Play("circle", CombatCore.Pos(c), 3.2f * s, col, 0f, 0.7f, true, 14f, "EffectsBack");
            Fx.Instance aura = Fx.Play("aura", CombatCore.Pos(c), 2.6f * s, col, 0f, 0.7f, true, 16f);
            Fx.Attach(aura, c);
            yield return CombatCore.Wait(0.6f);
            if (!CombatCore.Alive(c)) yield break;

            // Barrage of strikes across the area, then the main blast
            Fx.Play("circle", p, m.Radius * 2.2f, col, 0f, 1.2f, true, 14f, "EffectsBack");
            for (int i = 0; i < 5; i++)
            {
                Vector3 spot = p + (Vector3)(Random.insideUnitCircle * m.Radius * 0.8f);
                Fx.Play("pillar", spot + new Vector3(0f, 1.6f * s * 0.6f), 2.4f * s * 0.6f, col);
                Fx.Play("hit", spot, 1.6f, Color.white);
                CombatCore.HitAll(c, CombatCore.EnemiesNear(c, spot, 1.4f), m.Power * 0.15f, e);
                yield return CombatCore.Wait(0.07f);
            }
            Fx.Play(m.Fx ?? "burst", p, m.Radius * 1.6f, col, 0f, 1f);
            Fx.Instance blast = Fx.Play(m.Fx2 ?? "nova", p, m.Radius * 1.2f, col);
            Fx.Grow(blast, 2f);
            Fx.Play("burst", p, m.Radius * 1.1f, Color.white);
            Fx.Instance shock = Fx.Play("nova", p, m.Radius * 0.8f, Color.white, 0f, 0.6f);
            Fx.Grow(shock, 3f);
            Crater(p, m.Radius * 1.6f);
            CombatCore.Sound(m.Sfx2, p);
            CombatCore.HitAll(c, CombatCore.EnemiesNear(c, p, m.Radius), m.Power, e, m.Status, m.StatusTime);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>A scorched crater that lingers on the ground after heavy blows.</summary>
        public static void Crater(Vector3 pPos, float pSize)
        {
            if (!Settings.Craters) return;
            Fx.Play("crater", pPos, Mathf.Clamp(pSize, 1.5f, 10f), new Color(0.16f, 0.12f, 0.1f, 0.85f), 0f, 4.5f,
                false, 1f, "EffectsBack");
        }

        private static void HitArea(Actor c, Actor t, Vector3 p, MoveAsset m, string e)
        {
            if (m.Radius > 0f)
            {
                CombatCore.HitAll(c, CombatCore.EnemiesNear(c, p, m.Radius), m.Power, e, m.Status, m.StatusTime);
            }
            else if (CombatCore.Alive(t))
            {
                CombatCore.Hit(c, t, m.Power, e, m.Status, m.StatusTime);
            }
        }

        private static float Angle(BaseSimObject pFrom, BaseSimObject pTo)
        {
            Vector2 d = pTo.current_position - pFrom.current_position;
            return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }
    }
}
