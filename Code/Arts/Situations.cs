using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Progression;
using UnityEngine;

namespace TeyvatChronicle.Arts
{
    /// <summary>
    /// Battle situations that override the usual choice of technique:
    /// a last stand when a fighter is nearly dead, a bodyguard stepping in front of a hurt ally,
    /// vengeance for a fallen comrade, and a rampage when a champion cuts through a crowd.
    /// </summary>
    public static class Situations
    {
        private class Grudge
        {
            public long Killer;
            public string VictimName;
            public float Until;
        }

        private static readonly Dictionary<long, Grudge> _grudges = new Dictionary<long, Grudge>();
        private static readonly Dictionary<long, float> _guard_cd = new Dictionary<long, float>();
        private static readonly Dictionary<long, List<float>> _kills = new Dictionary<long, List<float>>();

        public static void ClearAll()
        {
            _grudges.Clear();
            _guard_cd.Clear();
            _kills.Clear();
        }

        public static bool Check(Actor a, Actor target, MoveSet set)
        {
            if (!Settings.Situations) return false;
            return LastStand(a, target, set) || Vengeance(a, set) || Bodyguard(a, target, set);
        }

        // ---------------------------------------------------------------- last stand

        private static bool LastStand(Actor a, Actor target, MoveSet set)
        {
            CombatData data = CombatCore.Data(a);
            if (a.getHealth() > a.getMaxHealth() * 0.2f || CombatCore.Now - data.LastStandUsed < 90f) return false;
            Technique best = Tactics.Strongest(a, set);
            if (best == null) return false;
            data.LastStandUsed = CombatCore.Now;
            ModClass.Instance.StartCoroutine(LastStandRoutine(a, target, set, best));
            return true;
        }

        private static IEnumerator LastStandRoutine(Actor a, Actor target, MoveSet set, Technique best)
        {
            CombatData data = CombatCore.Data(a);
            data.Casting = true;
            Cast.Hold(a, 2f);
            Color c = Elements.Color(CombatCore.ElementFor(a, set));
            Vector3 p = a.current_position;
            if (Director.OnScreen(p))
            {
                FloatingText.Show("LAST STAND", p + new Vector3(0f, 1.8f), new Color(1f, 0.35f, 0.3f), 16);
                CombatCore.Sound("last_stand", p, "gi/sfx/arts/");
                Fx.Instance flare = Fx.Play("arts/lifeburn", p, 3.5f, Color.white, 0f, 1.1f, true, 14f, "EffectsBack");
                Fx.Attach(flare, a);
                Fx.Instance ring = Fx.Play("arts/shock", p, 2f, c, 0f, 0f, false, 18f);
                Fx.Grow(ring, 2.5f);
                Director.Punch(0.8f, 0.5f);
                ModClass.Instance.StartCoroutine(Director.Spotlight(p, 1.4f));
            }
            a.restoreHealth(Mathf.CeilToInt(a.getMaxHealth() * Path.LastStandHeal(a)));
            a.addStatusEffect("powerup", 12f);
            yield return CombatCore.Wait(0.7f);
            data.Casting = false;
            if (CombatCore.Alive(a) && CombatCore.Alive(target)) Engine.Run(a, target, best, set, 1.6f, false);
        }

        // ---------------------------------------------------------------- vengeance

        public static void OnKill(Actor pKiller, Actor pVictim)
        {
            if (!Settings.Situations || pVictim == null || !Path.IsPowered(pVictim)) return;
            Vector3 p = pVictim.current_position;
            foreach (Actor ally in CombatCore.AlliesNear(pVictim, 12f))
            {
                if (ally == pVictim || !Path.IsPowered(ally)) continue;
                _grudges[ally.getID()] = new Grudge
                {
                    Killer = pKiller.getID(), VictimName = pVictim.getName(), Until = CombatCore.Now + 40f
                };
            }
            RecordKill(pKiller);
        }

        private static bool Vengeance(Actor a, MoveSet set)
        {
            if (!_grudges.TryGetValue(a.getID(), out Grudge g)) return false;
            if (CombatCore.Now > g.Until)
            {
                _grudges.Remove(a.getID());
                return false;
            }
            Actor killer = World.world.units.get(g.Killer);
            if (!CombatCore.Alive(killer))
            {
                _grudges.Remove(a.getID());
                return false;
            }
            if (Vector2.Distance(a.current_position, killer.current_position) > 12f) return false;
            Technique best = Tactics.Strongest(a, set);
            if (best == null) return false;
            _grudges.Remove(a.getID());
            ModClass.Instance.StartCoroutine(VengeanceRoutine(a, killer, set, best, g.VictimName));
            return true;
        }

        private static IEnumerator VengeanceRoutine(Actor a, Actor killer, MoveSet set, Technique best, string pName)
        {
            CombatData data = CombatCore.Data(a);
            data.Casting = true;
            Color c = Elements.Color(CombatCore.ElementFor(a, set));
            Vector3 p = a.current_position;
            if (Director.OnScreen(p))
            {
                FloatingText.Show("VENGEANCE", p + new Vector3(0f, 1.8f), c, 16);
                if (!string.IsNullOrEmpty(pName)) FloatingText.Show("for " + pName, p + new Vector3(0f, 1.1f), Color.white, 10);
                CombatCore.Sound("vengeance", p, "gi/sfx/arts/");
            }
            yield return CombatCore.Wait(0.35f);
            // Close in on the killer.
            if (Cast.CanMove(a) && CombatCore.Alive(killer))
            {
                Vector3 to = (Vector3)killer.current_position +
                             ((Vector3)a.current_position - (Vector3)killer.current_position).normalized * 1.1f;
                if (Settings.Afterimages) Fx.Ghost(a, c, 0.45f);
                Fx.Line("arts/iai", a.current_position, to, 0.6f, c, 0.3f, false);
                CombatCore.Sound("blink", p, "gi/sfx/arts/");
                yield return CombatCore.Slide(a, Cast.Clamp(to), 0.12f, Settings.Afterimages ? c : (Color?)null);
            }
            data.Casting = false;
            if (CombatCore.Alive(a) && CombatCore.Alive(killer))
            {
                a.addStatusEffect("powerup", 10f);
                Engine.Run(a, killer, best, set, 1.6f, false);
            }
        }

        // ---------------------------------------------------------------- bodyguard

        private static bool Bodyguard(Actor a, Actor target, MoveSet set)
        {
            if (!(target.attack_target is Actor ward) || ward == a || !ward.isAlive()) return false;
            if (ward.kingdom != a.kingdom || ward.getHealth() > ward.getMaxHealth() * 0.5f) return false;
            int ward_tier = CombatCore.ResolveSet(ward, out _)?.Tier ?? 0;
            if (ward_tier >= set.Tier) return false;
            if (_guard_cd.TryGetValue(a.getID(), out float ready) && ready > CombatCore.Now) return false;
            if (Vector2.Distance(a.current_position, ward.current_position) > 6f) return false;
            if (!Cast.CanMove(a) || !Randy.randomChance(0.6f)) return false;
            Technique strike = Tactics.AnyAttack(a, set);
            if (strike == null) return false;
            _guard_cd[a.getID()] = CombatCore.Now + 20f;
            ModClass.Instance.StartCoroutine(BodyguardRoutine(a, target, ward, set, strike));
            return true;
        }

        private static IEnumerator BodyguardRoutine(Actor a, Actor enemy, Actor ward, MoveSet set, Technique strike)
        {
            CombatData data = CombatCore.Data(a);
            data.Casting = true;
            Color c = Elements.Color(CombatCore.ElementFor(a, set));
            Vector3 between = Vector3.Lerp(ward.current_position, enemy.current_position, 0.45f);
            if (Settings.Afterimages) Fx.Ghost(a, c, 0.45f);
            Fx.Play("arts/vacuum", a.current_position, 1.4f, Color.white, 0f, 0f, false, 30f);
            Cast.Place(a, between);
            Fx.Play("arts/shock", between, 2f, c, 0f, 0f, false, 22f);
            Fx.Play("arts/lock_sparks", between, 1.2f, Color.white, 0f, 0f, false, 26f);
            CombatCore.Sound("guard_tap", between, "gi/sfx/arts/");
            FloatingText.Show("PROTECT", between + new Vector3(0f, 1.6f), c, 13);
            ward.addStatusEffect("shield", 6f);
            Path.OnRescue(a);
            yield return CombatCore.Wait(0.3f);
            data.Casting = false;
            if (CombatCore.Alive(a) && CombatCore.Alive(enemy)) Engine.Run(a, enemy, strike, set, 1.2f);
        }

        // ---------------------------------------------------------------- rampage

        private static void RecordKill(Actor pKiller)
        {
            if (pKiller == null || !Path.IsPowered(pKiller)) return;
            long id = pKiller.getID();
            if (!_kills.TryGetValue(id, out List<float> list)) _kills[id] = list = new List<float>();
            list.Add(CombatCore.Now);
            list.RemoveAll(t => CombatCore.Now - t > 8f);
            if (list.Count < 5) return;
            list.Clear();
            Vector3 p = pKiller.current_position;
            if (!Director.OnScreen(p)) return;
            MoveSet set = CombatCore.ResolveSet(pKiller, out _);
            Color c = Elements.Color(CombatCore.ElementFor(pKiller, set));
            FloatingText.Show("RAMPAGE", p + new Vector3(0f, 1.8f), c, 15);
            CombatCore.Sound("rampage", p, "gi/sfx/arts/");
            Fx.Instance crown = Fx.Play("arts/fury_crown", p + new Vector3(0f, 0.9f), 1.6f, Color.white, 0f, 2.5f, true, 12f);
            Fx.Attach(crown, pKiller);
            CombatCore.Data(pKiller).Gauge = Mathf.Min(100f, CombatCore.Data(pKiller).Gauge + 20f);
        }
    }
}
