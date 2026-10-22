using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using TeyvatChronicle.Chronicle;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Progression
{
    /// <summary>
    /// The Crucible Path: the mod's own way for powered units to grow, saved with each unit.
    ///
    /// - Four FACETS grow from what a unit actually does: Vigor (taking hits, drilling), Edge (landing
    ///   techniques, dummies, kills), Ward (guarding, being hit mid-technique, protecting allies) and Flow
    ///   (dashes, duels, sparring). Each runs 0–40 and is capped by the Stage (8 per Stage).
    /// - Five STAGES: Kindled, Forged, Tempered, Hallowed, Paragon. Each multiplies health and damage.
    /// - When two facets reach the Stage's cap the unit is PRIMED. The breakthrough is forged in battle:
    ///   its next serious fight is a CRUCIBLE, and if it is still standing 20 seconds later (or wins the
    ///   duel) it BREAKS THROUGH. A unit primed for 15 years without a fight breaks through by meditation.
    /// - MARKS are deeds that leave a lasting perk: Survivor, Kingslayer, Duelist, Giantslayer, Warden,
    ///   Reaper, Unbowed, Crowned.
    /// </summary>
    public static class Path
    {
        public enum Facet { Vigor, Edge, Ward, Flow }

        [Flags]
        public enum Mark
        {
            None = 0, Survivor = 1, Kingslayer = 2, Duelist = 4, Giantslayer = 8,
            Warden = 16, Reaper = 32, Unbowed = 64, Crowned = 128
        }

        public const int MaxStage = 5;
        public const int CapPerStage = 8;
        public const int MaxFacet = CapPerStage * MaxStage;
        public static readonly Facet[] Facets = { Facet.Vigor, Facet.Edge, Facet.Ward, Facet.Flow };
        public static readonly Mark[] AllMarks =
        {
            Mark.Survivor, Mark.Kingslayer, Mark.Duelist, Mark.Giantslayer, Mark.Warden, Mark.Reaper, Mark.Unbowed, Mark.Crowned
        };

        private static readonly string[] StageNames = { "Unawakened", "Kindled", "Forged", "Tempered", "Hallowed", "Paragon" };
        private static readonly string[] Numerals = { "0", "I", "II", "III", "IV", "V" };
        private static readonly float[] StageHealth = { 1f, 1f, 1.6f, 2.5f, 4f, 6f };
        private static readonly float[] StageDamage = { 1f, 1f, 1.3f, 1.7f, 2.2f, 3f };

        private const float CrucibleSeconds = 20f;
        private const float MeditationYears = 15f;
        private const float YearSeconds = 60f;

        // Saved keys (actor.data custom data)
        private const string KStage = "gi_p_stage", KMarks = "gi_p_marks", KPrimedSince = "gi_p_primed";
        private const string KKills = "gi_p_kills", KDuelWins = "gi_p_duels", KSparWins = "gi_p_spars";
        private const string KSessions = "gi_p_sessions", KRescues = "gi_p_rescues", KTrainTime = "gi_p_train_time";
        private const string KPotential = "gi_potential";
        private static string KLevel(Facet f) => "gi_p_lv_" + (int)f;
        private static string KXp(Facet f) => "gi_p_xp_" + (int)f;

        /// <summary>Units in a Crucible right now (not saved: a reload ends it).</summary>
        private static readonly Dictionary<long, float> _crucibles = new Dictionary<long, float>();
        private static readonly Dictionary<long, float> _rate = new Dictionary<long, float>();

        // ---------------------------------------------------------------- accessors

        private static int GetInt(Actor a, string k, int d = 0) { a.data.get(k, out int v, d); return v; }
        private static float GetFloat(Actor a, string k, float d = 0f) { a.data.get(k, out float v, d); return v; }
        private static void Set(Actor a, string k, int v) => a.data.set(k, v);
        private static void Set(Actor a, string k, float v) => a.data.set(k, v);

        public static bool IsPowered(Actor a) => a != null && CombatCore.ResolveSet(a, out _) != null;

        public static int Tier(Actor a) => a == null ? 0 : CombatCore.ResolveSet(a, out _)?.Tier ?? 0;

        /// <summary>The unit's Stage, 1–5 (0 for mortals and units not yet seen).</summary>
        public static int Stage(Actor a) => a == null ? 0 : Mathf.Clamp(GetInt(a, KStage), 0, MaxStage);

        public static string StageName(int pStage) => StageNames[Mathf.Clamp(pStage, 0, MaxStage)];
        public static string Numeral(int pStage) => Numerals[Mathf.Clamp(pStage, 0, MaxStage)];

        public static int Level(Actor a, Facet f) => Mathf.Clamp(GetInt(a, KLevel(f)), 0, MaxFacet);
        public static float FacetXp(Actor a, Facet f) => GetFloat(a, KXp(f));
        public static float XpToNext(int pLevel) => 20f + 6f * pLevel + 0.5f * pLevel * pLevel;
        public static int Cap(Actor a) => CapPerStage * Mathf.Max(1, Stage(a));
        public static int FacetTotal(Actor a)
        {
            int total = 0;
            foreach (Facet f in Facets) total += Level(a, f);
            return total;
        }

        public static int Kills(Actor a) => GetInt(a, KKills);
        public static int DuelWins(Actor a) => GetInt(a, KDuelWins);
        public static int SparWins(Actor a) => GetInt(a, KSparWins);
        public static int Sessions(Actor a) => GetInt(a, KSessions);
        public static int Rescues(Actor a) => GetInt(a, KRescues);
        public static float Potential(Actor a) => GetFloat(a, KPotential);
        public static void AddPotential(Actor a, float pAmount) => Set(a, KPotential, Potential(a) + pAmount);

        public static Mark Marks(Actor a) => (Mark)GetInt(a, KMarks);
        public static bool HasMark(Actor a, Mark m) => a != null && (Marks(a) & m) != 0;
        public static int MarkCount(Actor a)
        {
            int n = 0;
            foreach (Mark m in AllMarks) if (HasMark(a, m)) n++;
            return n;
        }

        /// <summary>Two facets at the Stage cap, below Paragon.</summary>
        public static bool IsPrimed(Actor a)
        {
            int stage = Stage(a);
            if (stage <= 0 || stage >= MaxStage) return false;
            int cap = Cap(a), at_cap = 0;
            foreach (Facet f in Facets) if (Level(a, f) >= cap) at_cap++;
            return at_cap >= 2;
        }

        public static bool InCrucible(Actor a) => a != null && _crucibles.ContainsKey(a.getID());

        public static float CrucibleLeft(Actor a) =>
            a != null && _crucibles.TryGetValue(a.getID(), out float until) ? Mathf.Max(0f, until - Time.unscaledTime) : 0f;

        // ---------------------------------------------------------------- multipliers (for Balance, Engine and the stats patch)

        public static float HealthMultiplier(Actor a)
        {
            int stage = Stage(a);
            float m = StageHealth[stage] * (1f + 0.04f * Level(a, Facet.Vigor));
            if (HasMark(a, Mark.Survivor)) m *= 1.05f;
            if (HasMark(a, Mark.Crowned)) m *= 1.10f;
            return m;
        }

        public static float StatDamageMultiplier(Actor a) => StageDamage[Stage(a)];

        /// <summary>Technique power from Edge: +3% per level.</summary>
        public static float TechniquePower(Actor a) => !Settings.Path || a == null ? 1f : 1f + 0.03f * Level(a, Facet.Edge);

        /// <summary>Cooldown multiplier from Flow: 1.2% shorter per level (down to half).</summary>
        public static float CooldownFactor(Actor a) =>
            !Settings.Path || a == null ? 1f : Mathf.Max(0.5f, 1f - 0.012f * Level(a, Facet.Flow));

        /// <summary>Extra Guard chance from Ward (+0.5% per level) and the Warden mark (+10%).</summary>
        public static float GuardBonus(Actor a)
        {
            if (!Settings.Path || a == null) return 0f;
            return 0.005f * Level(a, Facet.Ward) + (HasMark(a, Mark.Warden) ? 0.1f : 0f);
        }

        /// <summary>Damage dealt multiplier from marks: Kingslayer and Giantslayer.</summary>
        public static float DamageMultiplier(Actor pAttacker, Actor pVictim)
        {
            if (!Settings.Path || pAttacker == null || pVictim == null) return 1f;
            float m = 1f;
            if (HasMark(pAttacker, Mark.Kingslayer) && (pVictim.isKing() || pVictim.isCityLeader() || IsPowered(pVictim))) m *= 1.15f;
            if (HasMark(pAttacker, Mark.Giantslayer) && Tier(pVictim) > Tier(pAttacker)) m *= 1.2f;
            return m;
        }

        /// <summary>Damage taken multiplier: Ward (1% less per level, at most 40%) and Unbowed against God abilities.</summary>
        public static float DamageTaken(Actor pVictim, bool pGod)
        {
            if (!Settings.Path || pVictim == null || Stage(pVictim) == 0) return 1f;
            float m = 1f - Mathf.Min(0.4f, 0.01f * Level(pVictim, Facet.Ward));
            if (pGod && HasMark(pVictim, Mark.Unbowed)) m *= 0.75f;
            return m;
        }

        /// <summary>Technique level from Stage: Forged and up know Level II, Hallowed and up Level III.</summary>
        public static int TechniqueLevel(Actor a)
        {
            int stage = Stage(a);
            return stage >= 4 ? 3 : stage >= 2 ? 2 : 1;
        }

        /// <summary>Duel momentum bonus (Duelist mark).</summary>
        public static float DuelMomentum(Actor a) => HasMark(a, Mark.Duelist) ? 1.15f : 1f;

        /// <summary>How much a last stand heals (Survivor heals twice as much).</summary>
        public static float LastStandHeal(Actor a) => HasMark(a, Mark.Survivor) ? 0.4f : 0.2f;

        /// <summary>A single score for rankings: tier, then stage, facets and marks.</summary>
        public static int Score(Actor a) => Tier(a) * 100000 + Stage(a) * 10000 + FacetTotal(a) * 20 + MarkCount(a) * 5;

        // ---------------------------------------------------------------- start

        /// <summary>The Stage a tier starts at: Visions Kindled, Exalted Forged, Archons Tempered, Sovereigns and Shades Hallowed, Moons and the Principles Paragon.</summary>
        public static int FloorFor(int pTier) => pTier switch
        {
            <= 0 => 0,
            1 => 1,
            2 => 2,
            3 => 3,
            4 or 5 => 4,
            _ => 5
        };

        /// <summary>First time a powered unit is seen (and whenever its tier rises): lift it to its tier's Stage.</summary>
        public static void EnsureInit(Actor a)
        {
            if (!Settings.Path || a == null || !a.isAlive()) return;
            int floor = FloorFor(Tier(a));
            if (floor <= 0 || Stage(a) >= floor) return;
            SetStage(a, floor, true);
        }

        /// <summary>Sets the Stage and lifts facets to the start of it, then refreshes stats and heals to full.</summary>
        public static void SetStage(Actor a, int pStage, bool pHeal = true)
        {
            pStage = Mathf.Clamp(pStage, 1, MaxStage);
            int start = CapPerStage * (pStage - 1) + (pStage > 1 ? 1 : 0);
            Set(a, KStage, pStage);
            foreach (Facet f in Facets)
            {
                if (Level(a, f) < start) Set(a, KLevel(f), start);
            }
            Set(a, KPrimedSince, 0f);
            RefreshStats(a, pHeal);
        }

        /// <summary>Recalculates the unit's stats (so the stage and facet multipliers apply) and optionally heals it.</summary>
        public static void RefreshStats(Actor a, bool pHeal)
        {
            if (a == null || !a.isAlive()) return;
            a.setStatsDirty();
            a.updateStats();
            if (pHeal) a.setMaxHealth();
        }

        // ---------------------------------------------------------------- facets

        /// <summary>Adds XP to a facet. Levels stop at the Stage cap; once two facets sit there, the unit is primed.</summary>
        public static void AddFacetXp(Actor a, Facet f, float pXp, bool pFromTraining)
        {
            if (!Settings.Path || a == null || !a.isAlive() || pXp <= 0f) return;
            if (Stage(a) == 0)
            {
                EnsureInit(a);
                if (Stage(a) == 0) return;
            }
            pXp *= pFromTraining ? Settings.TrainingXp : Settings.CombatXp;
            int cap = Cap(a);
            int level = Level(a, f);
            if (level >= cap) return;
            bool was_primed = IsPrimed(a);
            float xp = FacetXp(a, f) + pXp;
            bool leveled = false;
            while (level < cap && xp >= XpToNext(level))
            {
                xp -= XpToNext(level);
                level++;
                leveled = true;
            }
            if (level >= cap) xp = 0f;
            Set(a, KLevel(f), level);
            Set(a, KXp(f), xp);
            if (!leveled) return;
            if (f == Facet.Vigor) RefreshStats(a, false);
            if (level % 8 == 0) FloatingText.Show($"{f} {level}", (Vector3)a.current_position + new Vector3(0f, 1.3f), FacetColor(f), 10);
            if (!was_primed && IsPrimed(a))
            {
                Set(a, KPrimedSince, (float)World.world.getCurWorldTime());
                FloatingText.Show("PRIMED", (Vector3)a.current_position + new Vector3(0f, 1.6f), new Color(1f, 0.85f, 0.5f), 12);
            }
        }

        public static Color FacetColor(Facet f) => f switch
        {
            Facet.Vigor => new Color32(255, 118, 110, 255),
            Facet.Edge => new Color32(255, 214, 120, 255),
            Facet.Ward => new Color32(127, 200, 255, 255),
            _ => new Color32(140, 240, 190, 255)
        };

        public static string FacetText(Facet f) => f switch
        {
            Facet.Vigor => "Vigor: +4% health per level. Grows by taking hits and surviving, and by drilling.",
            Facet.Edge => "Edge: +3% technique power per level. Grows by landing techniques, dummy training and kills.",
            Facet.Ward => "Ward: 1% less damage taken per level and better guarding. Grows by guarding and protecting allies.",
            _ => "Flow: 1.2% shorter cooldowns per level and a little speed. Grows by dashing, duelling and sparring."
        };

        /// <summary>Rate limit for frequent combat hooks: at most once per interval per unit and kind.</summary>
        private static bool Throttle(Actor a, int pKind, float pInterval)
        {
            long key = a.getID() * 8 + pKind;
            float now = Time.unscaledTime;
            if (_rate.TryGetValue(key, out float last) && now - last < pInterval) return false;
            if (_rate.Count > 2000) _rate.Clear();
            _rate[key] = now;
            return true;
        }

        // ---------------------------------------------------------------- combat hooks

        /// <summary>A technique of this unit landed on someone.</summary>
        public static void OnTechniqueLanded(Actor pCaster)
        {
            if (pCaster == null || !Throttle(pCaster, 0, 0.3f)) return;
            AddFacetXp(pCaster, Facet.Edge, 2f, false);
        }

        /// <summary>This unit took a hit. Mid-technique hits also temper Ward; a primed unit under 35% starts its Crucible.</summary>
        public static void OnHitTaken(Actor pVictim)
        {
            if (pVictim == null || !pVictim.isAlive() || Stage(pVictim) == 0) return;
            if (Throttle(pVictim, 1, 0.4f))
            {
                AddFacetXp(pVictim, Facet.Vigor, 1.5f, false);
                if (CombatCore.Data(pVictim).Casting) AddFacetXp(pVictim, Facet.Ward, 1.5f, false);
            }
            float health = pVictim.getHealth() / (float)Mathf.Max(1, pVictim.getMaxHealth());
            if (health < 0.1f) Earn(pVictim, Mark.Survivor);
            if (health < 0.35f && IsPrimed(pVictim)) StartCrucible(pVictim, "on the brink");
        }

        /// <summary>This unit guarded a blow.</summary>
        public static void OnGuard(Actor pVictim)
        {
            if (pVictim == null || !Throttle(pVictim, 2, 0.3f)) return;
            AddFacetXp(pVictim, Facet.Ward, 3f, false);
        }

        /// <summary>This unit dashed, blinked or leapt.</summary>
        public static void OnDash(Actor a)
        {
            if (a == null || !Throttle(a, 3, 0.5f)) return;
            AddFacetXp(a, Facet.Flow, 3f, false);
        }

        /// <summary>This unit stepped in to protect an ally.</summary>
        public static void OnRescue(Actor a)
        {
            if (a == null || !a.isAlive()) return;
            Set(a, KRescues, Rescues(a) + 1);
            AddFacetXp(a, Facet.Ward, 15f, false);
            if (Rescues(a) >= 3) Earn(a, Mark.Warden);
        }

        /// <summary>This unit survived a God ability aimed at it.</summary>
        public static void OnSurvivedGod(Actor a)
        {
            if (a != null && a.isAlive() && Stage(a) > 0) Earn(a, Mark.Unbowed);
        }

        public static void OnKill(Actor pKiller, Actor pVictim)
        {
            if (pKiller == null || pVictim == null || !pKiller.isAlive() || Stage(pKiller) == 0) return;
            Set(pKiller, KKills, Kills(pKiller) + 1);
            int tier = Tier(pVictim);
            AddFacetXp(pKiller, Facet.Edge, 8f + tier * 10f, false);
            AddFacetXp(pKiller, Facet.Vigor, 4f, false);
            if (pVictim.isKing()) Earn(pKiller, Mark.Kingslayer);
            if (tier > Tier(pKiller)) Earn(pKiller, Mark.Giantslayer);
            if (Kills(pKiller) >= 100) Earn(pKiller, Mark.Reaper);
            if (HasMark(pKiller, Mark.Reaper)) pKiller.restoreHealth(Mathf.CeilToInt(pKiller.getMaxHealth() * 0.02f));
        }

        /// <summary>A duel between two powered units begins: a primed duellist enters its Crucible.</summary>
        public static void OnDuelStart(Actor a, Actor b)
        {
            if (IsPrimed(a)) StartCrucible(a, "in a duel");
            if (IsPrimed(b)) StartCrucible(b, "in a duel");
        }

        public static void OnDuel(Actor pWinner, Actor pLoser)
        {
            if (pWinner != null && pWinner.isAlive())
            {
                Set(pWinner, KDuelWins, DuelWins(pWinner) + 1);
                AddFacetXp(pWinner, Facet.Flow, 30f, false);
                AddFacetXp(pWinner, Facet.Edge, 20f, false);
                AddFacetXp(pWinner, Facet.Ward, 20f, false);
                if (DuelWins(pWinner) >= 3) Earn(pWinner, Mark.Duelist);
                // Winning the duel completes a Crucible at once.
                if (InCrucible(pWinner)) BreakThrough(pWinner, false);
            }
            if (pLoser != null && pLoser.isAlive())
            {
                AddFacetXp(pLoser, Facet.Flow, 15f, false);
                AddFacetXp(pLoser, Facet.Ward, 15f, false);
            }
        }

        /// <summary>A primed unit facing many foes at once enters its Crucible.</summary>
        public static void OnOutnumbered(Actor a, int pFoes)
        {
            if (pFoes >= 5 && IsPrimed(a)) StartCrucible(a, "against a horde");
        }

        // ---------------------------------------------------------------- training hooks

        /// <summary>Drilling in the city tempers Vigor; a dummy tempers Edge. Every quarter year is a session.</summary>
        public static void OnTraining(Actor a, float pYears, bool pDummy, float pMultiplier)
        {
            if (Stage(a) == 0) return;
            if (pDummy)
            {
                AddFacetXp(a, Facet.Edge, 90f * pYears * pMultiplier, true);
                AddFacetXp(a, Facet.Vigor, 20f * pYears * pMultiplier, true);
            }
            else
            {
                AddFacetXp(a, Facet.Vigor, 60f * pYears * pMultiplier, true);
                AddFacetXp(a, Facet.Edge, 20f * pYears * pMultiplier, true);
            }
            float time = GetFloat(a, KTrainTime) + pYears;
            while (time >= 0.25f)
            {
                time -= 0.25f;
                Set(a, KSessions, Sessions(a) + 1);
            }
            Set(a, KTrainTime, time);
        }

        public static void OnSpar(Actor pWinner, Actor pLoser)
        {
            Set(pWinner, KSparWins, SparWins(pWinner) + 1);
            AddFacetXp(pWinner, Facet.Flow, 25f, true);
            AddFacetXp(pWinner, Facet.Ward, 15f, true);
            AddFacetXp(pLoser, Facet.Flow, 15f, true);
            AddFacetXp(pLoser, Facet.Ward, 20f, true);
        }

        /// <summary>Yearly bookkeeping: marks from status, and the quiet breakthrough after 15 primed years.</summary>
        public static void YearlyChecks(Actor a)
        {
            if (Stage(a) == 0) return;
            if (a.isKing() || a.isCityLeader()) Earn(a, Mark.Crowned);
            if (!IsPrimed(a))
            {
                if (GetFloat(a, KPrimedSince) > 0f) Set(a, KPrimedSince, 0f);
                return;
            }
            float since = GetFloat(a, KPrimedSince);
            if (since <= 0f)
            {
                Set(a, KPrimedSince, (float)World.world.getCurWorldTime());
                return;
            }
            if (!InCrucible(a) && World.world.getCurWorldTime() - since >= MeditationYears * YearSeconds)
                BreakThrough(a, true);
        }

        /// <summary>Years spent primed (for the panel).</summary>
        public static float PrimedYears(Actor a)
        {
            float since = GetFloat(a, KPrimedSince);
            return since <= 0f ? 0f : (float)(World.world.getCurWorldTime() - since) / YearSeconds;
        }

        // ---------------------------------------------------------------- the Crucible

        public static void StartCrucible(Actor a, string pReason)
        {
            if (!Settings.Path || a == null || !a.isAlive() || InCrucible(a) || !IsPrimed(a)) return;
            _crucibles[a.getID()] = Time.unscaledTime + CrucibleSeconds;
            ModClass.Instance.StartCoroutine(CrucibleRoutine(a, pReason));
        }

        private static IEnumerator CrucibleRoutine(Actor a, string pReason)
        {
            long id = a.getID();
            Vector3 p = a.current_position;
            Color color = Elements.Color(CombatCore.ElementFor(a, CombatCore.ResolveSet(a, out _)));
            if (Director.OnScreen(p))
            {
                FloatingText.Show("CRUCIBLE", p + new Vector3(0f, 1.9f), new Color(1f, 0.55f, 0.35f), 16);
                FloatingText.Show($"Stage {Numeral(Stage(a) + 1)} awaits, {pReason}", p + new Vector3(0f, 1.2f), Color.white, 9);
                CombatCore.Sound("last_stand", p, "gi/sfx/arts/");
                Director.HitStop(0.35f);
                Director.Punch(0.85f, 0.45f);
                ModClass.Instance.StartCoroutine(Director.Spotlight(p, 1.4f));
            }
            // The unit burns with its element while the Crucible lasts.
            float next_flare = 0f;
            while (_crucibles.TryGetValue(id, out float until) && Time.unscaledTime < until)
            {
                if (!CombatCore.Alive(a)) break;
                if (Time.unscaledTime >= next_flare && Director.OnScreen(a.current_position))
                {
                    next_flare = Time.unscaledTime + 1.2f;
                    Fx.Instance flare = Fx.Play("arts/lifeburn", a.current_position, 2.2f, color, 0f, 1.1f, false, 12f, "EffectsBack");
                    Fx.Attach(flare, a);
                }
                yield return null;
            }
            bool alive = CombatCore.Alive(a);
            bool still = _crucibles.ContainsKey(id);
            _crucibles.Remove(id);
            // If the duel already broke it through, the entry is gone and nothing more happens here.
            if (still && alive) BreakThrough(a, false);
        }

        /// <summary>The unit rises to its next Stage: ceremony, full heal, new multipliers.</summary>
        public static void BreakThrough(Actor a, bool pQuiet)
        {
            if (a == null || !a.isAlive()) return;
            int stage = Stage(a);
            if (stage <= 0 || stage >= MaxStage) return;
            _crucibles.Remove(a.getID());
            Set(a, KStage, stage + 1);
            Set(a, KPrimedSince, 0f);
            RefreshStats(a, true);
            string title = $"STAGE {Numeral(stage + 1)} · {StageName(stage + 1).ToUpper()}";
            Celebrate(a, "BREAKTHROUGH", "charge", 0.9f + 0.15f * stage, pQuiet ? title + " (by meditation)" : title);
            if (stage + 1 >= 4 && Settings.LegendAnnouncements)
            {
                MoveSet set = CombatCore.ResolveSet(a, out _);
                Announcer.AnnounceLegend($"{a.getName()} breaks through to {StageName(stage + 1)}",
                    Elements.Color(set?.Element ?? "divine"), a.current_tile, false);
            }
        }

        // ---------------------------------------------------------------- marks

        public static string MarkName(Mark m) => m.ToString();

        public static string MarkText(Mark m) => m switch
        {
            Mark.Survivor => "Survived below 10% health. Last stands heal 40%; +5% health.",
            Mark.Kingslayer => "Slew a ruler. +15% damage against rulers and powers.",
            Mark.Duelist => "Won 3 duels. +15% momentum in duels.",
            Mark.Giantslayer => "Slew a higher tier. +20% damage against higher tiers.",
            Mark.Warden => "Protected allies 3 times. +10% guard chance.",
            Mark.Reaper => "100 kills. Every kill heals 2%.",
            Mark.Unbowed => "Survived a God ability. Takes 25% less from God abilities.",
            Mark.Crowned => "Became a ruler. +10% health.",
            _ => ""
        };

        public static Color MarkColor(Mark m) => m switch
        {
            Mark.Survivor => new Color32(255, 140, 120, 255),
            Mark.Kingslayer => new Color32(255, 214, 120, 255),
            Mark.Duelist => new Color32(255, 180, 90, 255),
            Mark.Giantslayer => new Color32(255, 110, 150, 255),
            Mark.Warden => new Color32(127, 200, 255, 255),
            Mark.Reaper => new Color32(190, 150, 255, 255),
            Mark.Unbowed => new Color32(240, 240, 255, 255),
            _ => new Color32(255, 229, 138, 255)
        };

        /// <summary>Gives a mark if the unit doesn't have it yet, with a little ceremony.</summary>
        public static bool Earn(Actor a, Mark m)
        {
            if (!Settings.Path || a == null || !a.isAlive() || Stage(a) == 0 || HasMark(a, m)) return false;
            Set(a, KMarks, (int)(Marks(a) | m));
            if (m == Mark.Survivor || m == Mark.Crowned) RefreshStats(a, false);
            Celebrate(a, "MARK · " + m.ToString().ToUpper(), "moon", 0.7f, MarkText(m));
            return true;
        }

        /// <summary>A random mark the unit doesn't have yet (god power). Returns None if it has them all.</summary>
        public static Mark GrantRandomMark(Actor a)
        {
            var missing = new List<Mark>();
            foreach (Mark m in AllMarks) if (!HasMark(a, m)) missing.Add(m);
            if (missing.Count == 0) return Mark.None;
            Mark pick = missing[UnityEngine.Random.Range(0, missing.Count)];
            Earn(a, pick);
            return pick;
        }

        /// <summary>+5 to every facet (god power). Can prime the unit.</summary>
        public static void Temper(Actor a, int pLevels = 5)
        {
            EnsureInit(a);
            if (Stage(a) == 0) return;
            bool was_primed = IsPrimed(a);
            int cap = Cap(a);
            foreach (Facet f in Facets)
            {
                Set(a, KLevel(f), Mathf.Min(cap, Level(a, f) + pLevels));
                Set(a, KXp(f), 0f);
            }
            RefreshStats(a, false);
            if (!was_primed && IsPrimed(a)) Set(a, KPrimedSince, (float)World.world.getCurWorldTime());
        }

        // ---------------------------------------------------------------- stats patch

        public static void Patch(Harmony pHarmony)
        {
            pHarmony.Patch(AccessTools.Method(typeof(Actor), nameof(Actor.updateStats)),
                prefix: new HarmonyMethod(typeof(Path), nameof(StatsPrefix)),
                postfix: new HarmonyMethod(typeof(Path), nameof(StatsPostfix)));
        }

        private static void StatsPrefix(Actor __instance, out bool __state)
        {
            __state = __instance.isStatsDirty();
        }

        /// <summary>Applies Stage and facet multipliers after the game has rebuilt the unit's stats.</summary>
        private static void StatsPostfix(Actor __instance, bool __state)
        {
            if (!__state || !Settings.Path || !__instance.isAlive()) return;
            int stage = Stage(__instance);
            if (stage == 0 || !IsPowered(__instance)) return;
            __instance.stats["health"] = __instance.stats["health"] * HealthMultiplier(__instance);
            __instance.stats["damage"] = __instance.stats["damage"] * StatDamageMultiplier(__instance);
            __instance.stats["speed"] = __instance.stats["speed"] * (1f + 0.005f * Level(__instance, Facet.Flow));
        }

        // ---------------------------------------------------------------- presentation

        public static void Celebrate(Actor a, string pLabel, string pSfx, float pSize, string pSubLabel = null)
        {
            MoveSet set = CombatCore.ResolveSet(a, out _);
            Color color = Elements.Color(set?.Element ?? "divine");
            Vector3 p = a.current_position;
            if (!Director.OnScreen(p)) return;
            if (pLabel.StartsWith("BREAK") || pLabel.StartsWith("A NEW") || pLabel.StartsWith("EXALT") || pLabel.StartsWith("VISION"))
            {
                Director.Ceremony(a, pLabel, color, pSize);
                if (pSubLabel != null) FloatingText.Show(pSubLabel, p + new Vector3(0f, -1.2f), Color.white, 9);
                return;
            }
            Fx.Play("pillar", p + new Vector3(0f, 2.2f * pSize), 4f * pSize, color);
            Fx.Instance ring = Fx.Play("nova", p, 3f * pSize, color);
            Fx.Grow(ring, 1.8f);
            Fx.Play("circle", p, 2.6f * pSize, color, 0f, 1.2f, true, 14f, "EffectsBack");
            CombatCore.Sound(pSfx, p);
            FloatingText.Show(pLabel, p, color, 13);
            if (pSubLabel != null) FloatingText.Show(pSubLabel, p + new Vector3(0f, -1.2f), Color.white, 9);
        }

        public static void ClearAll()
        {
            _crucibles.Clear();
            _rate.Clear();
        }
    }
}
