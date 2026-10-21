using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Progression
{
    /// <summary>
    /// Walks the world a slice at a time and lets units train. Powered units earn Resonance XP from drills,
    /// training dummies and sparring, boosted by mentors and Training Grounds. Ordinary people build up
    /// potential the same way, and a strong enough wish can awaken a Vision.
    /// </summary>
    public static class Training
    {
        private const int SliceSize = 250;
        private const float YearSeconds = 60f;

        private const float DrillXpPerYear = 400f;
        private const float DummyXpPerYear = 800f;

        private static int _cursor;
        private static readonly Dictionary<long, double> _last_seen = new Dictionary<long, double>();
        private static readonly Dictionary<long, int> _last_kills = new Dictionary<long, int>();
        private static readonly Dictionary<City, int> _city_best_rank = new Dictionary<City, int>();
        private static readonly HashSet<long> _sparring = new HashSet<long>();
        private static object _world_marker;

        // Traits that make a Vision more likely: the gods notice strong wishes.
        private static readonly Dictionary<string, float> WishTraits = new Dictionary<string, float>
        {
            ["ambitious"] = 2f, ["chosen_one"] = 4f, ["strong"] = 1.5f, ["lucky"] = 1.5f, ["genius"] = 1.5f,
            ["wise"] = 1.3f, ["bloodlust"] = 1.3f, ["miracle_born"] = 3f, ["blessed"] = 1.5f
        };

        public static void Tick()
        {
            if (!Settings.Resonance || World.world?.units == null || Config.worldLoading) return;
            if (!ReferenceEquals(_world_marker, World.world.map_stats))
            {
                _world_marker = World.world.map_stats;
                _last_seen.Clear();
                _last_kills.Clear();
                _city_best_rank.Clear();
                _cursor = 0;
                TrainingGrounds.Reload();
            }

            List<Actor> units = World.world.units.getSimpleList();
            if (units.Count == 0) return;
            if (_cursor >= units.Count)
            {
                _cursor = 0;
                _city_best_rank.Clear();
            }
            double now = World.world.getCurWorldTime();
            int end = Mathf.Min(units.Count, _cursor + SliceSize);
            for (int i = _cursor; i < end; i++) Process(units[i], now);
            _cursor = end;
            TrainingGrounds.DrawMarkers();
        }

        private static void Process(Actor a, double pNow)
        {
            if (a == null || !a.isAlive()) return;
            long id = a.getID();
            if (!_last_seen.TryGetValue(id, out double last))
            {
                _last_seen[id] = pNow;
                _last_kills[id] = a.data.kills;
                return;
            }
            _last_seen[id] = pNow;
            float years = Mathf.Clamp((float)(pNow - last) / YearSeconds, 0f, 0.5f);
            if (years <= 0f) return;

            int kills = a.data.kills;
            int new_kills = _last_kills.TryGetValue(id, out int old) ? Mathf.Max(0, kills - old) : 0;
            _last_kills[id] = kills;

            if (Resonance.IsPowered(a)) ProcessPowered(a, years);
            else if (a.isSapient()) ProcessMortal(a, years, new_kills);
        }

        // ---------------------------------------------------------------- powered units

        private static void ProcessPowered(Actor a, float pYears)
        {
            Resonance.EnsureInit(a);
            int rank = Resonance.Rank(a);
            if (a.city != null)
            {
                _city_best_rank.TryGetValue(a.city, out int best);
                if (rank > best) _city_best_rank[a.city] = rank;
            }

            // Fateful moments that can be seen from the outside
            float health = a.getHealth() / (float)Mathf.Max(1, a.getMaxHealth());
            if (health < 0.1f && a.has_attack_target) Resonance.Moment(a, Resonance.Moments.NearDeath);
            if (a.data.renown >= 500) Resonance.Moment(a, Resonance.Moments.Renowned);
            if (a.isKing()) Resonance.Moment(a, Resonance.Moments.Crowned);

            if (a.isSapient() && CanTrain(a))
            {
                bool dummy = a.isTask("warrior_train_with_dummy");
                float xp = (dummy ? DummyXpPerYear : DrillXpPerYear) * pYears * Multiplier(a, rank);
                Resonance.AddXp(a, xp, true);
                Resonance.AddTrainingTime(a, pYears);
                if (Randy.randomChance(0.3f)) TrainingVisual(a, dummy);
                if (Settings.Sparring && Randy.randomChance(0.05f)) TrySpar(a);
            }

            Resonance.CheckBreakthrough(a);
            Ascension.YearlyChecks(a, pYears);
        }

        /// <summary>What the unit is doing for its Resonance right now, for the character panel.</summary>
        public static string Activity(Actor a)
        {
            if (CombatCore.Data(a).InDuel) return "<color=#FF8A6A>Locked in a duel</color>";
            if (_sparring.Contains(a.getID())) return "<color=#FFD27F>Sparring</color>";
            if (a.has_attack_target) return "<color=#FF8A6A>In battle</color>";
            if (!Resonance.IsPowered(a)) return a.isSapient() ? "Living an ordinary life" : "—";
            if (!a.isSapient() || !CanTrain(a)) return "Wandering (not training)";
            string what = a.isTask("warrior_train_with_dummy") ? "Training at a dummy" : "Drilling";
            int rank = Resonance.Rank(a);
            var bonus = new List<string>();
            if (a.city != null && _city_best_rank.TryGetValue(a.city, out int best) && best > rank + 5) bonus.Add("mentor ×1.5");
            if (TrainingGrounds.Contains(a.current_position)) bonus.Add("Training Ground ×2");
            if (rank > 60) bonus.Add("rank 60+ ×0.5");
            return bonus.Count > 0 ? $"{what} ({string.Join(", ", bonus)})" : what;
        }

        private static bool CanTrain(Actor a)
        {
            if (a.has_attack_target || a.city == null || a.is_inside_boat) return false;
            if (CombatCore.Data(a).InDuel || _sparring.Contains(a.getID())) return false;
            return a.current_tile?.zone?.city == a.city || TrainingGrounds.Contains(a.current_position);
        }

        /// <summary>Mentors (a higher rank in the same city) ×1.5; Training Grounds ×2.</summary>
        private static float Multiplier(Actor a, int pRank)
        {
            float m = 1f;
            if (a.city != null && _city_best_rank.TryGetValue(a.city, out int best) && best > pRank + 5) m *= 1.5f;
            if (TrainingGrounds.Contains(a.current_position)) m *= 2f;
            return m;
        }

        private static void TrainingVisual(Actor a, bool pDummy)
        {
            Vector3 p = a.current_position;
            if (!Director.OnScreen(p)) return;
            MoveSet set = CombatCore.ResolveSet(a, out _);
            Color color = Elements.Color(set?.Element);
            Vector3 front = p + new Vector3(a.flip ? -0.7f : 0.7f, 0.2f);
            Fx.Play(pDummy ? "spark" : "slash", front, 1.2f, color * new Color(1f, 1f, 1f, 0.8f),
                Random.Range(0f, 360f), 0f, false, 22f);
        }

        // ---------------------------------------------------------------- sparring

        private static void TrySpar(Actor a)
        {
            Actor partner = null;
            float best = 64f;
            foreach (Actor other in a.city.units)
            {
                if (other == a || other == null || !other.isAlive() || !other.isSapient()) continue;
                if (!Resonance.IsPowered(other) || !CanTrain(other)) continue;
                float d = ((Vector2)other.current_position - a.current_position).sqrMagnitude;
                if (d < best) { best = d; partner = other; }
            }
            if (partner == null) return;
            ModClass.Instance.StartCoroutine(Spar(a, partner));
        }

        /// <summary>A friendly bout: real-looking exchanges, no damage, and XP for both.</summary>
        private static IEnumerator Spar(Actor a, Actor b)
        {
            _sparring.Add(a.getID());
            _sparring.Add(b.getID());
            a.addStatusEffect(Duel.StatusId, 4f);
            b.addStatusEffect(Duel.StatusId, 4f);
            Color ca = Elements.Color(CombatCore.ResolveSet(a, out _)?.Element);
            Color cb = Elements.Color(CombatCore.ResolveSet(b, out _)?.Element);
            bool visible = Director.OnScreen(a.current_position);
            if (visible) FloatingText.Show("SPAR", ((Vector3)a.current_position + (Vector3)b.current_position) * 0.5f, Color.white, 11);

            for (int i = 0; i < 6; i++)
            {
                if (!CombatCore.Alive(a) || !CombatCore.Alive(b)) break;
                CombatCore.Face(a, b.current_position);
                CombatCore.Face(b, a.current_position);
                if (visible)
                {
                    Vector3 mid = ((Vector3)a.current_position + (Vector3)b.current_position) * 0.5f;
                    Actor striker = i % 2 == 0 ? a : b;
                    Fx.Play("slash", (Vector3)striker.current_position + (mid - (Vector3)striker.current_position) * 0.7f,
                        1.4f, i % 2 == 0 ? ca : cb, Random.Range(-40f, 40f), 0f, false, 26f);
                    Fx.Play("spark", mid, 1.2f, Color.white);
                    CombatCore.Sound(i % 3 == 0 ? "clang" : "swing", mid);
                }
                yield return CombatCore.Wait(0.22f);
            }

            if (CombatCore.Alive(a) && CombatCore.Alive(b))
            {
                float score_a = Resonance.Rank(a) + Random.Range(0f, 25f);
                float score_b = Resonance.Rank(b) + Random.Range(0f, 25f);
                Actor winner = score_a >= score_b ? a : b;
                Actor loser = winner == a ? b : a;
                if (visible) FloatingText.Show("WIN", winner.current_position, winner == a ? ca : cb, 10);
                Resonance.OnSpar(winner, loser);
                a.finishStatusEffect(Duel.StatusId);
                b.finishStatusEffect(Duel.StatusId);
            }
            _sparring.Remove(a.getID());
            _sparring.Remove(b.getID());
        }

        // ---------------------------------------------------------------- mortals and Vision awakening

        private static void ProcessMortal(Actor a, float pYears, int pNewKills)
        {
            if (a.kingdom == null || !a.kingdom.isCiv()) return;
            if (CanTrain(a))
            {
                bool dummy = a.isTask("warrior_train_with_dummy");
                float gain = (dummy ? 40f : 18f) * pYears * (TrainingGrounds.Contains(a.current_position) ? 2f : 1f);
                Resonance.AddPotential(a, gain);
            }
            if (pNewKills > 0) Resonance.AddPotential(a, pNewKills * 6f);
            if (!Settings.VisionAwakening || Resonance.Potential(a) < 100f) return;

            float chance = 0.02f * Settings.AwakeningRate * pYears * 4f;
            foreach (KeyValuePair<string, float> wish in WishTraits)
            {
                if (a.hasTrait(wish.Key)) chance *= wish.Value;
            }
            if (a.isKing()) chance *= 2f;
            // In Genshin, Visions often answer a desperate wish in the moment of greatest need.
            float health = a.getHealth() / (float)Mathf.Max(1, a.getMaxHealth());
            if (a.has_attack_target && health < 0.25f) chance *= 6f;
            if (Randy.randomChance(Mathf.Min(0.5f, chance))) Awaken(a, null);
        }

        /// <summary>Gives a Vision. The element follows the kingdom's Archon half the time, otherwise fate decides.</summary>
        public static string Awaken(Actor a, string pElement)
        {
            string element = pElement ?? ElementFor(a);
            foreach (string e in Elements.All) a.removeTrait("gi_vision_" + e);
            a.addTrait("gi_vision_" + element);
            Resonance.SetRank(a, Mathf.Max(1, Resonance.Rank(a)));
            Resonance.Celebrate(a, "VISION AWAKENED", "charge", 0.9f, a.getName());
            Chronicle.Announcer.AnnounceLegend($"{a.getName()} awakens a {Capital(element)} Vision", Elements.Color(element), a.current_tile, false);
            return element;
        }

        private static string ElementFor(Actor a)
        {
            if (a.kingdom != null && Randy.randomBool())
            {
                foreach (Actor unit in a.kingdom.getUnits())
                {
                    if (unit == null || !unit.isAlive()) continue;
                    foreach (string e in Elements.All)
                    {
                        if (unit.hasTrait("gi_gnosis_" + e)) return e;
                    }
                }
            }
            return Elements.All[Randy.randomInt(0, Elements.All.Length)];
        }

        public static string Capital(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);
    }

    /// <summary>Areas painted with the Training Ground power. Units there train twice as fast. Saved with the world.</summary>
    public static class TrainingGrounds
    {
        private const string Key = "gi_grounds";
        public const float Radius = 8f;
        private static readonly List<Vector2> _centers = new List<Vector2>();
        private static float _next_draw;

        public static IReadOnlyList<Vector2> Centers => _centers;

        public static void Reload()
        {
            _centers.Clear();
            World.world.map_stats.custom_data.get(Key, out string raw, "");
            if (string.IsNullOrEmpty(raw)) return;
            foreach (string part in raw.Split(';'))
            {
                string[] xy = part.Split(',');
                if (xy.Length == 2 && float.TryParse(xy[0], out float x) && float.TryParse(xy[1], out float y))
                {
                    _centers.Add(new Vector2(x, y));
                }
            }
        }

        private static void Save()
        {
            string raw = string.Join(";", _centers.Select(c => $"{c.x:0.#},{c.y:0.#}"));
            World.world.map_stats.custom_data.set(Key, raw);
        }

        public static bool Contains(Vector2 pPos)
        {
            for (int i = 0; i < _centers.Count; i++)
            {
                if ((_centers[i] - pPos).sqrMagnitude <= Radius * Radius) return true;
            }
            return false;
        }

        /// <summary>Adds a ground at the position, or removes the one already there.</summary>
        public static bool Toggle(Vector2 pPos)
        {
            for (int i = 0; i < _centers.Count; i++)
            {
                if ((_centers[i] - pPos).sqrMagnitude <= Radius * Radius)
                {
                    _centers.RemoveAt(i);
                    Save();
                    WorldTip.showNowTop("Training Ground removed", false);
                    return false;
                }
            }
            _centers.Add(pPos);
            Save();
            Fx.Play("circle", pPos, Radius * 2f, Toolbox.makeColor("#FFE58A"), 0f, 2f, true, 10f, "EffectsBack");
            WorldTip.showNowTop("Training Ground marked: units here train twice as fast", false);
            return true;
        }

        public static void DrawMarkers()
        {
            if (CombatCore.Now < _next_draw) return;
            _next_draw = CombatCore.Now + 2.5f;
            foreach (Vector2 c in _centers)
            {
                if (!Director.OnScreen(c)) continue;
                Fx.Play("circle", c, Radius * 2f, new Color(1f, 0.9f, 0.55f, 0.35f), 0f, 2.6f, true, 6f, "EffectsBack");
            }
        }
    }
}
