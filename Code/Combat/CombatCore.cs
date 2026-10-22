using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Cinematics;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>Runtime combat data for one unit. Not saved: cooldowns and energy reset on load.</summary>
    public class CombatData
    {
        public readonly Dictionary<string, float> ReadyAt = new Dictionary<string, float>();
        public float Energy;
        public float NextCast;
        public bool InDuel;

        /// <summary>A technique is running; no other starts until it ends.</summary>
        public bool Casting;

        /// <summary>The God gauge, 0–100. Fills by landing and taking blows; Archons and above spend it on a God ability.</summary>
        public float Gauge;

        public float LastStandUsed = -999f;
        public float LastTeam = -999f;
        public string Aura;
        public float AuraUntil;
    }

    /// <summary>Shared helpers: unit lookup, targeting, damage, timing.</summary>
    public static class CombatCore
    {
        private static readonly Dictionary<long, CombatData> _data = new Dictionary<long, CombatData>();
        private static float _cleanup_at;

        public static float Now => Time.unscaledTime;

        public static CombatData Data(Actor pActor)
        {
            long id = pActor.getID();
            if (!_data.TryGetValue(id, out CombatData data))
            {
                data = new CombatData();
                _data[id] = data;
            }
            return data;
        }

        /// <summary>Drops data for dead units every so often.</summary>
        public static void Cleanup()
        {
            if (Now < _cleanup_at) return;
            _cleanup_at = Now + 30f;
            var dead = new List<long>();
            foreach (long id in _data.Keys)
            {
                Actor actor = World.world.units.get(id);
                if (actor == null || !actor.isAlive()) dead.Add(id);
            }
            foreach (long id in dead) _data.Remove(id);
        }

        public static void ClearAll() => _data.Clear();

        // ---------------------------------------------------------------- tiers and movesets

        /// <summary>The style of the highest-tier Teyvat trait the unit holds, or null.</summary>
        public static MoveSet ResolveSet(Actor pActor, out string pTraitId)
        {
            pTraitId = null;
            MoveSet best = null;
            foreach (ActorTrait trait in pActor.getTraits())
            {
                if (!trait.id.StartsWith("gi_")) continue;
                MoveSet set = Arts.Library.StyleOf(trait.id);
                if (set == null) continue;
                if (best == null || set.Tier > best.Tier)
                {
                    best = set;
                    pTraitId = trait.id;
                }
            }
            return best;
        }

        public static string ResolveSetTrait(Actor pActor)
        {
            ResolveSet(pActor, out string trait);
            return trait;
        }

        /// <summary>
        /// The element a unit fights in. Styles that take a Vision's element (Harbingers, Adepti, Allogenes)
        /// use the holder's Vision when it has one.
        /// </summary>
        public static string ElementFor(Actor pActor, MoveSet pSet)
        {
            if (pSet == null) return "physical";
            if (!pSet.TakesVisionElement) return pSet.Element;
            foreach (string element in Elements.All)
            {
                if (pActor.hasTrait("gi_vision_" + element)) return element;
            }
            return pSet.Element;
        }

        // ---------------------------------------------------------------- positions and targets

        public static Vector3 Pos(BaseSimObject pObject) => pObject.current_position;

        public static bool Alive(BaseSimObject pObject) => pObject != null && pObject.isAlive();

        public static bool IsEnemy(Actor pCaster, Actor pOther)
        {
            if (pOther == null || pOther == pCaster || !pOther.isAlive()) return false;
            if (pCaster.attack_target == pOther) return true;
            return pCaster.kingdom != null && pOther.kingdom != null && pCaster.kingdom != pOther.kingdom &&
                   pCaster.kingdom.isEnemy(pOther.kingdom);
        }

        public static List<Actor> EnemiesNear(Actor pCaster, Vector3 pCenter, float pRadius)
        {
            var result = new List<Actor>();
            float r2 = pRadius * pRadius;
            foreach (Actor unit in World.world.units.getSimpleList())
            {
                if (!IsEnemy(pCaster, unit)) continue;
                if (((Vector3)unit.current_position - pCenter).sqrMagnitude <= r2) result.Add(unit);
            }
            return result;
        }

        public static List<Actor> AlliesNear(Actor pCaster, float pRadius)
        {
            var result = new List<Actor>();
            float r2 = pRadius * pRadius;
            Vector3 center = Pos(pCaster);
            foreach (Actor unit in World.world.units.getSimpleList())
            {
                if (unit == null || !unit.isAlive()) continue;
                if (unit != pCaster && (pCaster.kingdom == null || unit.kingdom != pCaster.kingdom)) continue;
                if (((Vector3)unit.current_position - center).sqrMagnitude <= r2) result.Add(unit);
            }
            return result;
        }

        /// <summary>Enemies within a thickness of the segment from a to b (beams).</summary>
        public static List<Actor> EnemiesAlong(Actor pCaster, Vector3 pFrom, Vector3 pTo, float pThickness)
        {
            var result = new List<Actor>();
            Vector3 seg = pTo - pFrom;
            float len2 = Mathf.Max(0.001f, seg.sqrMagnitude);
            foreach (Actor unit in World.world.units.getSimpleList())
            {
                if (!IsEnemy(pCaster, unit)) continue;
                Vector3 p = unit.current_position;
                float t = Mathf.Clamp01(Vector3.Dot(p - pFrom, seg) / len2);
                if ((pFrom + seg * t - p).magnitude <= pThickness) result.Add(unit);
            }
            return result;
        }

        public static WorldTile TileAt(Vector3 pPos)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt(pPos.x), 0, MapBox.width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(pPos.y), 0, MapBox.height - 1);
            return World.world.GetTile(x, y);
        }

        public static void Face(Actor pActor, Vector3 pTarget)
        {
            pActor.setFlip(pTarget.x < pActor.current_position.x);
        }

        // ---------------------------------------------------------------- damage

        /// <summary>Deals technique damage (balanced, past the Guard), feeds both God gauges and applies a status.</summary>
        public static void Hit(Actor pCaster, Actor pVictim, float pPower, string pElement, string pStatus = null,
            float pStatusTime = 3f, bool pGod = false)
        {
            if (!Alive(pCaster) || !Alive(pVictim)) return;
            // Armour is already part of the balanced damage, so the game doesn't reduce it again.
            float damage = Balance.Technique(pCaster, pVictim, pPower, pGod);
            Arts.Guard.Bypass = true;
            try
            {
                pVictim.getHit(damage, true, AttackType.Other, pCaster, false, false, false);
            }
            finally
            {
                Arts.Guard.Bypass = false;
            }
            Data(pCaster).Gauge = Mathf.Min(100f, Data(pCaster).Gauge + 1.5f + pPower * 0.5f);
            if (!pVictim.isAlive())
            {
                Progression.Resonance.OnKill(pCaster, pVictim);
                Arts.Situations.OnKill(pCaster, pVictim);
                return;
            }
            Data(pVictim).Gauge = Mathf.Min(100f, Data(pVictim).Gauge + 1f);
            if (!string.IsNullOrEmpty(pStatus)) pVictim.addStatusEffect(pStatus, pStatusTime);
        }

        public static void HitAll(Actor pCaster, IEnumerable<Actor> pVictims, float pPower, string pElement,
            string pStatus = null, float pStatusTime = 3f)
        {
            foreach (Actor victim in pVictims) Hit(pCaster, victim, pPower, pElement, pStatus, pStatusTime);
        }

        /// <summary>Waits on effect time, so it freezes during hit-stop and slows in slow motion.</summary>
        public static IEnumerator Wait(float pSeconds)
        {
            float t = 0f;
            while (t < pSeconds)
            {
                t += Time.unscaledDeltaTime * Director.FxTimeScale * (Config.paused ? 0f : 1f);
                yield return null;
            }
        }

        /// <summary>Slides a unit to a position over time, then settles it on that tile.</summary>
        public static IEnumerator Slide(Actor pActor, Vector3 pTo, float pSeconds, Color? pGhost = null)
        {
            if (!Alive(pActor)) yield break;
            Vector3 from = pActor.current_position;
            float t = 0f, next_ghost = 0f;
            while (t < pSeconds && Alive(pActor))
            {
                t += Time.unscaledDeltaTime * Director.FxTimeScale;
                if (pGhost.HasValue && t >= next_ghost)
                {
                    Fx.Ghost(pActor, pGhost.Value);
                    next_ghost = t + 0.03f;
                }
                Vector3 p = Vector3.Lerp(from, pTo, Mathf.SmoothStep(0f, 1f, t / pSeconds));
                pActor.current_position = p;
                yield return null;
            }
            if (Alive(pActor)) pActor.setCurrentTilePosition(TileAt(pTo));
        }

        private static readonly Dictionary<string, float> _sound_last = new Dictionary<string, float>();
        private static float _sound_window;
        private static int _sound_count;

        /// <summary>Plays a combat sound if it's on screen, at most once per 50 ms per sound and 18 per second overall.</summary>
        public static void Sound(string pSfx, Vector3 pPos) => Sound(pSfx, pPos, "gi/sfx/combat/");

        public static void Sound(string pSfx, Vector3 pPos, string pFolder)
        {
            if (string.IsNullOrEmpty(pSfx) || !Settings.CombatSounds || !Director.OnScreen(pPos)) return;
            if (_sound_last.TryGetValue(pSfx, out float last) && Now - last < 0.05f) return;
            if (Now - _sound_window > 1f)
            {
                _sound_window = Now;
                _sound_count = 0;
            }
            if (++_sound_count > 18) return;
            _sound_last[pSfx] = Now;
            MusicBox.playSound(pFolder + pSfx, pPos.x, pPos.y);
        }
    }
}
