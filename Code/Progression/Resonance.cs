using System.Collections.Generic;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Progression
{
    /// <summary>
    /// Ley Line Resonance: the mod's own levelling, separate from WorldBox levels and saved with each unit.
    /// Rank 1–90 with six Breakthroughs (20/40/50/60/70/80) that each need a Trial, three talents
    /// (Strike / Skill / Burst) and Constellations C0–C6 earned through fateful moments.
    /// </summary>
    public static class Resonance
    {
        public const int MaxRank = 90;
        public static readonly int[] Gates = { 20, 40, 50, 60, 70, 80 };

        // Saved keys (actor.data custom data)
        private const string KXp = "gi_xp", KRank = "gi_rank", KPhase = "gi_phase", KConst = "gi_const";
        private const string KTalS = "gi_tal_s", KTalK = "gi_tal_k", KTalB = "gi_tal_b";
        private const string KMoments = "gi_moments", KSessions = "gi_sessions", KTrainTime = "gi_train_time";
        private const string KSparWins = "gi_spar_wins", KDuelWins = "gi_duel_wins", KElites = "gi_elites";
        private const string KKills = "gi_kills", KPotential = "gi_potential", KGateSince = "gi_gate_since";

        // ---------------------------------------------------------------- accessors

        private static int GetInt(Actor a, string k, int d = 0) { a.data.get(k, out int v, d); return v; }
        private static float GetFloat(Actor a, string k, float d = 0f) { a.data.get(k, out float v, d); return v; }
        private static void Set(Actor a, string k, int v) => a.data.set(k, v);
        private static void Set(Actor a, string k, float v) => a.data.set(k, v);

        public static int Rank(Actor a) => Mathf.Max(1, GetInt(a, KRank, 1));
        public static float Xp(Actor a) => GetFloat(a, KXp);
        public static int Phase(Actor a) => GetInt(a, KPhase);
        public static int Constellation(Actor a) => GetInt(a, KConst);
        public static int TalentStrike(Actor a) => Mathf.Max(1, GetInt(a, KTalS, 1));
        public static int TalentSkill(Actor a) => Mathf.Max(1, GetInt(a, KTalK, 1));
        public static int TalentBurst(Actor a) => Mathf.Max(1, GetInt(a, KTalB, 1));
        public static int Sessions(Actor a) => GetInt(a, KSessions);
        public static int SparWins(Actor a) => GetInt(a, KSparWins);
        public static int DuelWins(Actor a) => GetInt(a, KDuelWins);
        public static int Elites(Actor a) => GetInt(a, KElites);
        public static int Kills(Actor a) => GetInt(a, KKills);
        public static float Potential(Actor a) => GetFloat(a, KPotential);

        /// <summary>
        /// First time a powered unit is seen: higher tiers start at a fitting rank
        /// (Exalted 25, Archon 40, Sovereign 55, Shade 70, Moons 85, Heavenly Principles 90).
        /// </summary>
        public static void EnsureInit(Actor a)
        {
            if (GetInt(a, KRank, 0) != 0) return;
            int tier = Tier(a);
            if (tier <= 0) return;
            SetRank(a, tier <= 1 ? 1 : Mathf.Min(MaxRank, 10 + (tier - 1) * 15));
        }

        public static float XpToNext(int pRank) => 30f + 4f * pRank + 0.25f * pRank * pRank;

        public static bool IsPowered(Actor a) => CombatCore.ResolveSet(a, out _) != null;

        public static int Tier(Actor a)
        {
            MoveSet set = CombatCore.ResolveSet(a, out _);
            return set?.Tier ?? 0;
        }

        /// <summary>Breakthrough the unit is waiting at, or 0 if it can still gain XP freely.</summary>
        public static int StuckAtGate(Actor a)
        {
            int rank = Rank(a), phase = Phase(a);
            return phase < Gates.Length && rank >= Gates[phase] ? Gates[phase] : 0;
        }

        // ---------------------------------------------------------------- XP

        /// <summary>Adds XP and handles rank-ups. Training XP is halved above rank 60.</summary>
        public static void AddXp(Actor a, float pXp, bool pFromTraining)
        {
            if (!Settings.Resonance || a == null || !a.isAlive() || pXp <= 0f) return;
            if (!IsPowered(a)) return;
            pXp *= pFromTraining ? Settings.TrainingXp : Settings.CombatXp;
            if (pFromTraining && Rank(a) > 60) pXp *= 0.5f;

            float xp = Xp(a) + pXp;
            int rank = Rank(a);
            bool ranked_up = false;
            while (rank < MaxRank && StuckAtGate(a) == 0)
            {
                float need = XpToNext(rank);
                if (xp < need) break;
                xp -= need;
                rank++;
                Set(a, KRank, rank);
                ranked_up = true;
            }
            if (StuckAtGate(a) != 0)
            {
                // XP waits at the gate (capped) until the Trial is passed.
                xp = Mathf.Min(xp, XpToNext(rank));
                if (GetFloat(a, KGateSince) <= 0f) Set(a, KGateSince, (float)World.world.getCurWorldTime());
            }
            Set(a, KXp, xp);
            if (ranked_up)
            {
                SpendTalents(a);
                if (rank % 10 == 0) Celebrate(a, $"RANK {rank}", "heal", 0.6f);
            }
        }

        // ---------------------------------------------------------------- trials

        /// <summary>The Trial for the unit's next Breakthrough, as a short description.</summary>
        public static string TrialText(int pGate) => pGate switch
        {
            20 => "Finish 3 training sessions",
            40 => "Win a spar or a duel, or defeat 5 foes",
            50 => "Defeat 10 foes in battle",
            60 => "Defeat an elite (tier 2+) or win a duel",
            70 => "Win 2 duels",
            80 => "Earn 300 renown, or wait a year for an Archon's blessing",
            _ => "—"
        };

        private static bool TrialPassed(Actor a, int pGate)
        {
            switch (pGate)
            {
                case 20: return Sessions(a) >= 3;
                case 40: return SparWins(a) + DuelWins(a) >= 1 || Kills(a) >= 5;
                case 50: return Kills(a) >= 10;
                case 60: return Elites(a) >= 1 || DuelWins(a) >= 1;
                case 70: return DuelWins(a) >= 2;
                case 80:
                    if (a.data.renown >= 300) return true;
                    float since = GetFloat(a, KGateSince);
                    return since > 0f && World.world.getCurWorldTime() - since >= 60.0 && KingdomHasArchon(a);
            }
            return false;
        }

        private static bool KingdomHasArchon(Actor a)
        {
            if (a.kingdom == null) return false;
            foreach (Actor unit in a.kingdom.getUnits())
            {
                if (unit == null || !unit.isAlive()) continue;
                foreach (string e in Elements.All)
                {
                    if (unit.hasTrait("gi_gnosis_" + e)) return true;
                }
            }
            return false;
        }

        /// <summary>Checks the Breakthrough Trial; on success the unit ascends to the next phase.</summary>
        public static void CheckBreakthrough(Actor a)
        {
            int gate = StuckAtGate(a);
            if (gate == 0 || !TrialPassed(a, gate)) return;
            int phase = Phase(a) + 1;
            Set(a, KPhase, phase);
            Set(a, KGateSince, 0f);
            for (int i = 1; i <= 6; i++) a.removeTrait("gi_ascension_" + i);
            a.addTrait("gi_ascension_" + phase);
            SpendTalents(a);
            Celebrate(a, "ASCENSION " + new string('★', phase), "charge", 1f);
            if (phase == 3) Moment(a, Moments.Phase3);
            if (phase == 6) Moment(a, Moments.Phase6);
            AddXp(a, 0.01f, false); // let banked XP flow into the newly opened ranks
        }

        // ---------------------------------------------------------------- talents

        public static int TalentCap(Actor a) => Mathf.Min(10, 2 + Phase(a) + Phase(a) / 2);

        /// <summary>One talent point per three ranks, spent by personality: aggressive units favour Burst,
        /// rational ones Skill, everyone else Strike.</summary>
        public static void SpendTalents(Actor a)
        {
            int points = (Rank(a) - 1) / 3 - (TalentStrike(a) + TalentSkill(a) + TalentBurst(a) - 3);
            int cap = TalentCap(a);
            float aggression = a.stats["personality_aggression"];
            float rationality = a.stats["personality_rationality"];
            int guard = 0;
            while (points > 0 && guard++ < 40)
            {
                string first = aggression >= rationality ? KTalB : KTalK;
                string[] order = { first, KTalS, first == KTalB ? KTalK : KTalB };
                bool spent = false;
                foreach (string key in order)
                {
                    int level = Mathf.Max(1, GetInt(a, key, 1));
                    if (level >= cap) continue;
                    Set(a, key, level + 1);
                    points--;
                    spent = true;
                    break;
                }
                if (!spent) break;
            }
        }

        public enum Category { Strike, Skill, Burst }

        public static Category CategoryOf(MoveAsset pMove)
        {
            if (pMove.Energy > 0f || pMove.Archetype == "ultimate") return Category.Burst;
            if (pMove.Archetype == "strike" || pMove.Archetype == "barrage") return Category.Strike;
            return Category.Skill;
        }

        /// <summary>Talent level including Constellation bonuses (C3 and C5 each add 2, up to 13).</summary>
        public static int EffectiveTalent(Actor a, Category c)
        {
            int level = c == Category.Strike ? TalentStrike(a) : c == Category.Skill ? TalentSkill(a) : TalentBurst(a);
            int cons = Constellation(a);
            if (cons >= 3) level += 2;
            if (cons >= 5) level += 2;
            return Mathf.Min(13, level);
        }

        /// <summary>Damage multiplier from rank and talent for a move.</summary>
        public static float MoveBoost(Actor a, MoveAsset pMove)
        {
            if (!Settings.Resonance) return 1f;
            float rank = 1f + (Rank(a) - 1) / 45f;
            float talent = 1f + 0.08f * (EffectiveTalent(a, CategoryOf(pMove)) - 1);
            float c6 = Constellation(a) >= 6 && CategoryOf(pMove) == Category.Burst ? 1.5f : 1f;
            return rank * talent * c6;
        }

        /// <summary>Cooldown multiplier: 3% shorter per talent level.</summary>
        public static float CooldownFactor(Actor a, MoveAsset pMove)
        {
            if (!Settings.Resonance) return 1f;
            return Mathf.Max(0.5f, 1f - 0.03f * (EffectiveTalent(a, CategoryOf(pMove)) - 1));
        }

        /// <summary>A copy of the move with Resonance applied (power, and the C6 Burst upgrade).</summary>
        public static MoveAsset Boosted(Actor a, MoveAsset pMove)
        {
            MoveAsset copy = pMove.Clone();
            copy.Power *= MoveBoost(a, pMove);
            if (Settings.Resonance && Constellation(a) >= 6 && CategoryOf(pMove) == Category.Burst)
            {
                copy.Scale *= 1.25f;
                copy.Impact = "major";
                copy.Name = pMove.Name + " (C6)";
            }
            return copy;
        }

        // ---------------------------------------------------------------- constellations

        [System.Flags]
        public enum Moments
        {
            None = 0, NearDeath = 1, KingSlayer = 2, DuelVictor = 4, Phase3 = 8, EliteSlayer = 16,
            Warrior = 32, Renowned = 64, Crowned = 128, Phase6 = 256
        }

        public static string MomentText(Moments m) => m switch
        {
            Moments.NearDeath => "Survived on the brink of death",
            Moments.KingSlayer => "Slew a king",
            Moments.DuelVictor => "Won a duel",
            Moments.Phase3 => "Reached Ascension Phase 3",
            Moments.EliteSlayer => "Defeated an elite",
            Moments.Warrior => "Defeated 50 foes",
            Moments.Renowned => "Earned 500 renown",
            Moments.Crowned => "Became a ruler",
            Moments.Phase6 => "Reached Ascension Phase 6",
            _ => ""
        };

        /// <summary>Records a fateful moment; each new one unlocks the next Constellation (up to C6).</summary>
        public static void Moment(Actor a, Moments pMoment)
        {
            if (!Settings.Resonance || !IsPowered(a)) return;
            int seen = GetInt(a, KMoments);
            if ((seen & (int)pMoment) != 0) return;
            Set(a, KMoments, seen | (int)pMoment);
            int cons = Constellation(a);
            if (cons >= 6) return;
            cons++;
            Set(a, KConst, cons);
            for (int i = 1; i <= 6; i++) a.removeTrait("gi_constellation_" + i);
            a.addTrait("gi_constellation_" + cons);
            Celebrate(a, $"CONSTELLATION C{cons}", "moon", 0.8f, MomentText(pMoment));
        }

        public static void SetConstellation(Actor a, int pValue)
        {
            pValue = Mathf.Clamp(pValue, 0, 6);
            Set(a, KConst, pValue);
            for (int i = 1; i <= 6; i++) a.removeTrait("gi_constellation_" + i);
            if (pValue > 0) a.addTrait("gi_constellation_" + pValue);
        }

        // ---------------------------------------------------------------- events from combat and training

        public static void OnKill(Actor pKiller, Actor pVictim)
        {
            if (pKiller == null || pVictim == null || !pKiller.isAlive()) return;
            Set(pKiller, KKills, Kills(pKiller) + 1);
            int tier = Tier(pVictim);
            int victim_rank = IsPowered(pVictim) ? Rank(pVictim) : 1;
            AddXp(pKiller, 20f + victim_rank * 2f + tier * 30f, false);
            if (tier >= 2 || pVictim.asset.id == "dragon")
            {
                Set(pKiller, KElites, Elites(pKiller) + 1);
                Moment(pKiller, Moments.EliteSlayer);
            }
            if (pVictim.isKing()) Moment(pKiller, Moments.KingSlayer);
            if (Kills(pKiller) >= 50) Moment(pKiller, Moments.Warrior);
            CheckBreakthrough(pKiller);
        }

        public static void OnDuel(Actor pWinner, Actor pLoser)
        {
            if (pWinner != null && pWinner.isAlive())
            {
                Set(pWinner, KDuelWins, DuelWins(pWinner) + 1);
                AddXp(pWinner, 80f, false);
                Moment(pWinner, Moments.DuelVictor);
                CheckBreakthrough(pWinner);
            }
            if (pLoser != null && pLoser.isAlive()) AddXp(pLoser, 30f, false);
        }

        public static void OnSpar(Actor pWinner, Actor pLoser)
        {
            Set(pWinner, KSparWins, SparWins(pWinner) + 1);
            AddXp(pWinner, 45f, true);
            AddXp(pLoser, 35f, true);
            CheckBreakthrough(pWinner);
            CheckBreakthrough(pLoser);
        }

        /// <summary>Training time in years; every quarter year counts as one session.</summary>
        public static void AddTrainingTime(Actor a, float pYears)
        {
            float time = GetFloat(a, KTrainTime) + pYears;
            while (time >= 0.25f)
            {
                time -= 0.25f;
                Set(a, KSessions, Sessions(a) + 1);
            }
            Set(a, KTrainTime, time);
        }

        public static void AddPotential(Actor a, float pAmount) => Set(a, KPotential, Potential(a) + pAmount);

        public static void SetRank(Actor a, int pRank)
        {
            pRank = Mathf.Clamp(pRank, 1, MaxRank);
            // Phases follow the rank when set directly (god powers, Exaltation).
            int phase = 0;
            while (phase < Gates.Length && pRank > Gates[phase]) phase++;
            Set(a, KRank, pRank);
            Set(a, KPhase, phase);
            Set(a, KXp, 0f);
            for (int i = 1; i <= 6; i++) a.removeTrait("gi_ascension_" + i);
            if (phase > 0) a.addTrait("gi_ascension_" + phase);
            SpendTalents(a);
        }

        // ---------------------------------------------------------------- presentation

        public static void Celebrate(Actor a, string pLabel, string pSfx, float pSize, string pSubLabel = null)
        {
            MoveSet set = CombatCore.ResolveSet(a, out _);
            Color color = Elements.Color(set?.Element ?? "divine");
            Vector3 p = a.current_position;
            if (!Director.OnScreen(p)) return;
            Fx.Play("pillar", p + new Vector3(0f, 2.2f * pSize), 4f * pSize, color);
            Fx.Instance ring = Fx.Play("nova", p, 3f * pSize, color);
            Fx.Grow(ring, 1.8f);
            Fx.Play("circle", p, 2.6f * pSize, color, 0f, 1.2f, true, 14f, "EffectsBack");
            CombatCore.Sound(pSfx, p);
            FloatingText.Show(pLabel, p, color, 14);
            if (pSubLabel != null) FloatingText.Show(pSubLabel, p + new Vector3(0f, -1.2f), Color.white, 9);
        }

        public static string Stars(int pPhase) => new string('★', pPhase) + new string('☆', 6 - pPhase);
    }
}
