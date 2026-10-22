using System;
using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Progression;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// The Arts engine. Hooked into every power trait's attack: it reads the battlefield, picks the technique
    /// that fits it best, and plays that technique's script. Before that it gives battle situations, duels,
    /// team arts and God abilities their chance.
    /// </summary>
    public static class Engine
    {
        public const string Held = "gi_held";
        private const float CastGap = 0.7f;

        public static void Init()
        {
            var status = new StatusAsset
            {
                id = Held,
                locale_id = "status_title_gi_held",
                locale_description = "status_description_gi_held",
                path_icon = "gi/icons/chronicle",
                duration = 3f,
                cancel_actor_job = true,
                allow_timer_reset = true
            };
            AssetManager.status.add(status);
            status.base_stats.addTag("immovable");
            status.base_stats.addTag("frozen_ai");
            status.base_stats.addTag("stop_idle_animation");
            status.base_stats["speed"] = -10000f;

            Library.Init();
        }

        /// <summary>The trait hook: only queues the decision so it runs on the main thread.</summary>
        public static bool OnAttack(BaseSimObject pSelf, BaseSimObject pTarget, WorldTile pTile)
        {
            if (!(pSelf is Actor caster) || !(pTarget is Actor target)) return false;
            MainThread.Enqueue(() => Decide(caster, target));
            return false;
        }

        public static bool Decide(Actor caster, Actor target)
        {
            if (World.world == null || Config.worldLoading || !CombatCore.Alive(caster)) return false;
            if (!target.isAlive())
            {
                Path.OnKill(caster, target);
                return false;
            }
            if (!Settings.Moves || Director.WorldStopped) return false;

            CombatData data = CombatCore.Data(caster);
            if (data.InDuel || data.Casting || CombatCore.Now < data.NextCast) return false;

            MoveSet set = CombatCore.ResolveSet(caster, out _);
            if (set == null) return false;
            Path.EnsureInit(caster);
            data.Gauge = Mathf.Min(100f, data.Gauge + 0.6f * set.Tier);
            // A primed fighter facing a horde enters its Crucible.
            if (Path.IsPrimed(caster)) Path.OnOutnumbered(caster, CombatCore.EnemiesNear(caster, caster.current_position, 4f).Count);

            if (Situations.Check(caster, target, set)) return true;
            if (Duels.TryStart(caster, target, set)) return true;
            if (Team.TryStart(caster, target, set)) return true;
            if (GodArts.TryStart(caster, target, set)) return true;

            Technique tech = Tactics.Choose(caster, target, set, data);
            if (tech == null) return false;
            Run(caster, target, tech, set);
            return true;
        }

        // ---------------------------------------------------------------- levels

        /// <summary>Level from the unit's Stage on the Path: what techniques it knows (Forged → II, Hallowed → III).</summary>
        public static int StageLevel(Actor a) => Path.TechniqueLevel(a);

        private static string MasteryKey(Technique t) => "gi_m_" + t.Id;

        public static int Uses(Actor a, Technique t)
        {
            a.data.get(MasteryKey(t), out int uses, 0);
            return uses;
        }

        /// <summary>Mastery from practice: 12 uses for Level II, 35 for Level III.</summary>
        public static int Mastery(Actor a, Technique t)
        {
            int uses = Uses(a, t);
            return uses >= 35 ? 3 : uses >= 12 ? 2 : 1;
        }

        public static int Level(Actor a, Technique t) => Mathf.Max(StageLevel(a), Mastery(a, t));

        public static bool Knows(Actor a, Technique t) => StageLevel(a) >= t.Unlock;

        public static void AddUse(Actor a, Technique t, int pAmount = 1) =>
            a.data.set(MasteryKey(t), Uses(a, t) + pAmount);

        /// <summary>Training drills a known technique (called by the training system).</summary>
        public static void Drill(Actor a)
        {
            MoveSet set = CombatCore.ResolveSet(a, out _);
            if (set == null || set.Moves.Count == 0) return;
            Technique t = Library.Get(set.Moves[UnityEngine.Random.Range(0, set.Moves.Count)]);
            if (t != null && Knows(a, t)) AddUse(a, t);
        }

        // ---------------------------------------------------------------- running

        /// <summary>Starts a technique. Returns the cast (null if it couldn't start).</summary>
        public static Cast Run(Actor pCaster, Actor pTarget, Technique pTech, MoveSet pSet, float pPower = 1f,
            bool pCountCooldown = true)
        {
            CombatData data = CombatCore.Data(pCaster);
            if (data.Casting) return null;
            int level = Level(pCaster, pTech);
            var cast = new Cast(pCaster, pTarget, pTech, pSet, level, CombatCore.ElementFor(pCaster, pSet))
            {
                Pow = pPower * Path.TechniquePower(pCaster) * (level >= 3 ? 1.9f : level == 2 ? 1.4f : 1f),
                God = pTech.God
            };

            data.NextCast = CombatCore.Now + CastGap;
            if (pCountCooldown)
            {
                float cooldown = pTech.Cooldown * Path.CooldownFactor(pCaster);
                data.ReadyAt[pTech.Id] = CombatCore.Now + cooldown / Mathf.Max(0.1f, Settings.CooldownScale);
            }
            AddUse(pCaster, pTech);
            // Dashes, blinks and leaps temper Flow.
            if (pTech.Shape == Shape.Gap || pTech.Shape == Shape.AntiAir) Path.OnDash(pCaster);
            ModClass.Instance.StartCoroutine(Exec(cast));
            return cast;
        }

        private static IEnumerator Exec(Cast c)
        {
            CombatData data = CombatCore.Data(c.A);
            data.Casting = true;
            Cast.Hold(c.A, 4f);
            if (c.TOk) CombatCore.Face(c.A, c.TP);

            bool cutscene = c.Tech.Ultimate && !c.God && (c.Lv >= 3 || c.Style.Tier >= 3) &&
                            Director.OnScreen(c.P) && Director.CanPlayCutscene();
            if (cutscene)
            {
                yield return Director.Cutscene(c.Tech.Name, c.Style.Name, c.P, c.Hot, 0.9f);
            }
            else if (c.Lv >= 2 || c.Tech.Ultimate)
            {
                c.Call();
            }

            yield return Safe(c.Tech.Script(c), c.Tech.Id);

            if (CombatCore.Alive(c.A))
            {
                Cast.SetHeight(c.A, 0f);
                Cast.Release(c.A);
            }
            data.Casting = false;
        }

        private static readonly HashSet<string> _reported = new HashSet<string>();

        /// <summary>Runs a script and stops it quietly (logging once) if it throws.</summary>
        public static IEnumerator Safe(IEnumerator pScript, string pName)
        {
            if (pScript == null) yield break;
            while (true)
            {
                object current;
                try
                {
                    if (!pScript.MoveNext()) yield break;
                    current = pScript.Current;
                }
                catch (Exception e)
                {
                    if (_reported.Add(pName)) ModClass.LogWarning($"[Arts] {pName} failed: {e}");
                    yield break;
                }
                yield return current;
            }
        }
    }

    /// <summary>
    /// The tactical mind: reads the fight and scores every ready technique against it. Crowds draw slams,
    /// lines draw fissures and beams, a lone wounded enemy draws an execution, fliers draw leaps, far
    /// enemies draw dashes, and a hurt fighter guards or heals.
    /// </summary>
    public static class Tactics
    {
        public struct Read
        {
            public float Distance;
            public int Crowd;
            public int Lane;
            public bool Wounded;
            public bool Flier;
            public bool SelfHurt;
            public bool AllyHurt;
        }

        public static Read Survey(Actor a, Actor t)
        {
            var r = new Read
            {
                Distance = Vector2.Distance(a.current_position, t.current_position),
                Crowd = CombatCore.EnemiesNear(a, t.current_position, 2.5f).Count,
                Wounded = t.getHealth() < t.getMaxHealth() * 0.35f,
                Flier = t.asset.flying || t.position_height > 0.5f,
                SelfHurt = a.getHealth() < a.getMaxHealth() * 0.4f
            };
            Vector3 from = a.current_position;
            Vector3 dir = ((Vector3)t.current_position - from).normalized;
            r.Lane = CombatCore.EnemiesAlong(a, from, from + dir * 9f, 0.9f).Count;
            foreach (Actor ally in CombatCore.AlliesNear(a, 5f))
            {
                if (ally != a && ally.getHealth() < ally.getMaxHealth() * 0.5f) { r.AllyHurt = true; break; }
            }
            return r;
        }

        public static float Score(Technique t, Read r)
        {
            switch (t.Shape)
            {
                case Shape.Crowd: return r.Crowd >= 3 ? 4f + r.Crowd * 0.3f : r.Crowd == 2 ? 1.2f : 0.4f;
                case Shape.Line: return r.Lane >= 3 ? 4f + r.Lane * 0.3f : r.Lane == 2 ? 1.4f : 0.5f;
                case Shape.Lone: return r.Crowd <= 1 ? 3f : 0.8f;
                case Shape.Execute: return r.Wounded ? 4.5f : 0.3f;
                case Shape.AntiAir: return r.Flier ? 5f : 0.5f;
                case Shape.Gap: return r.Distance > 3.5f ? 4f : 0.4f;
                case Shape.Guard: return r.SelfHurt ? 5f : 0.05f;
                case Shape.Support: return r.AllyHurt ? 4f : 0.05f;
                default: return 1.5f;
            }
        }

        public static Technique Choose(Actor a, Actor target, MoveSet set, CombatData data)
        {
            Read read = Survey(a, target);
            Technique best = null;
            float best_score = 0f;
            foreach (string id in set.Moves)
            {
                Technique t = Library.Get(id);
                if (t == null || !Engine.Knows(a, t)) continue;
                if (data.ReadyAt.TryGetValue(id, out float ready) && ready > CombatCore.Now) continue;
                bool self = t.Shape == Shape.Guard || t.Shape == Shape.Support;
                if (!self && read.Distance > t.Range + 0.5f) continue;
                float score = Score(t, read) * (t.Ultimate ? 1.3f : 1f) * UnityEngine.Random.Range(0.6f, 1.4f);
                if (score > best_score)
                {
                    best_score = score;
                    best = t;
                }
            }
            return best_score >= 0.25f ? best : null;
        }

        /// <summary>The strongest known attacking technique (duel finishers and catastrophes).</summary>
        public static Technique Strongest(Actor a, MoveSet set)
        {
            Technique best = null;
            foreach (string id in set.Moves)
            {
                Technique t = Library.Get(id);
                if (t == null || !Engine.Knows(a, t) || t.Shape == Shape.Guard || t.Shape == Shape.Support) continue;
                if (best == null || t.Power + (t.Ultimate ? 2f : 0f) > best.Power + (best.Ultimate ? 2f : 0f)) best = t;
            }
            return best;
        }

        /// <summary>A random known attacking technique.</summary>
        public static Technique AnyAttack(Actor a, MoveSet set)
        {
            var options = new List<Technique>();
            foreach (string id in set.Moves)
            {
                Technique t = Library.Get(id);
                if (t != null && Engine.Knows(a, t) && t.Shape != Shape.Guard && t.Shape != Shape.Support) options.Add(t);
            }
            return options.Count > 0 ? options[UnityEngine.Random.Range(0, options.Count)] : null;
        }
    }
}
