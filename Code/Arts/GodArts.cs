using System.Collections;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// God abilities. Archons and every tier above fill a God gauge by landing and taking blows; when it is
    /// full the world stops, a title card names the ability, and it plays out while everything else
    /// stands frozen. Every divine style has its own.
    /// </summary>
    public static class GodArts
    {
        private static bool _running;
        private static float _last;
        private static float _started;

        /// <summary>A safety net: never leave the world stopped if an ability was cut off.</summary>
        public static void Update()
        {
            if (!_running || Time.unscaledTime - _started < 15f) return;
            Director.EndWorldStop();
            _running = false;
        }

        public static bool TryStart(Actor a, Actor target, MoveSet set)
        {
            if (!Settings.GodAbilities || set.God == null || set.Tier < 3 || _running) return false;
            CombatData data = CombatCore.Data(a);
            if (data.Gauge < 100f) return false;
            if (CombatCore.Now - _last < Settings.GodCooldown) return false;
            Technique god = Library.Get(set.God);
            if (god == null || !Director.OnScreen(a.current_position)) return false;
            data.Gauge = 0f;
            _last = CombatCore.Now;
            ModClass.Instance.StartCoroutine(Run(a, target, god, set));
            return true;
        }

        /// <summary>Plays a God ability now, whatever the gauge says (god powers and the Chronicle use this).</summary>
        public static void Force(Actor a, Actor target)
        {
            MoveSet set = CombatCore.ResolveSet(a, out _);
            Technique god = set != null ? Library.Get(set.God) : null;
            if (god == null || _running) return;
            CombatCore.Data(a).Gauge = 0f;
            ModClass.Instance.StartCoroutine(Run(a, target ?? a, god, set));
        }

        private static IEnumerator Run(Actor a, Actor target, Technique god, MoveSet set)
        {
            _running = true;
            _started = Time.unscaledTime;
            CombatData data = CombatCore.Data(a);
            data.Casting = true;
            var c = new Cast(a, target, god, set, 3, CombatCore.ElementFor(a, set))
            {
                God = true,
                Pow = Progression.Path.TechniquePower(a) * 2f
            };
            Vector3 p = a.current_position;
            World.world.locatePosition(p);
            Director.BeginWorldStop(god.Name, "GOD ABILITY · " + set.Name, c.Hot);
            ModClass.Instance.StartCoroutine(Director.Spotlight(p, 1.2f));
            CombatCore.Sound("god_stop", p, "gi/sfx/arts/");
            Fx.Instance ring = Fx.Play("arts/shock", p, 1.5f, c.Hot, 0f, 0f, false, 14f);
            Fx.Grow(ring, 6f);
            yield return new WaitForSecondsRealtime(0.9f);
            // The ability runs while the world is frozen; its own effects keep full speed.
            _started = Time.unscaledTime;
            yield return Engine.Safe(god.Script(c), god.Id);
            // Powers that stood in its path and still stand earn the Unbowed mark.
            foreach (Actor foe in CombatCore.EnemiesNear(a, p, 25f))
            {
                if (CombatCore.Alive(foe)) Progression.Path.OnSurvivedGod(foe);
            }
            yield return new WaitForSecondsRealtime(0.25f);
            Director.EndWorldStop();
            CombatCore.Sound("god_resume", p, "gi/sfx/arts/");
            if (CombatCore.Alive(a))
            {
                Cast.SetHeight(a, 0f);
                Cast.Release(a);
            }
            data.Casting = false;
            _running = false;
        }
    }
}
