using System.Collections.Generic;
using HarmonyLib;
using NeoModLoader.General;
using TeyvatChronicle.Arts;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Powers;
using TeyvatChronicle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TeyvatChronicle.UI
{
    /// <summary>
    /// The Resonance character panel. It opens beside WorldBox's unit window whenever you inspect a unit
    /// and updates live: power and tier, rank and XP, Ascension, the next Trial, talents, Constellation,
    /// battle record, what the unit is doing, and every move with its cooldown.
    /// </summary>
    public static class CharacterPanel
    {
        private const float W = 230f, H = 392f;
        private static RectTransform _root;
        private static Actor _actor;
        private static float _refresh_at;

        // Widgets
        private static Text _title, _name, _power, _tier, _rank, _xp, _trial, _activity, _record, _cons_note, _hint;
        private static Image _emblem, _badge, _xp_fill;
        private static Image[] _stars, _nodes;
        private static Image[] _talent_fill = new Image[3];
        private static Text[] _talent_text = new Text[3];
        private static RectTransform _moves, _powered_group, _mortal_group, _actions;
        private static Text _style;
        private static Image _potential_fill;
        private static Text _potential_text, _wish_text;

        public static void Patch(Harmony pHarmony)
        {
            pHarmony.Patch(AccessTools.Method(typeof(UnitWindow), nameof(UnitWindow.OnEnable)),
                postfix: new HarmonyMethod(typeof(CharacterPanel), nameof(OnUnitWindowOpen)));
            pHarmony.Patch(AccessTools.Method(typeof(UnitWindow), nameof(UnitWindow.OnDisable)),
                postfix: new HarmonyMethod(typeof(CharacterPanel), nameof(OnUnitWindowClose)));
        }

        private static void OnUnitWindowOpen()
        {
            if (Settings.CharacterPanel) Show(SelectedUnit.unit, true);
        }

        private static void OnUnitWindowClose() => Hide();

        // ---------------------------------------------------------------- show / hide

        public static void Show(Actor pActor, bool pBesideUnitWindow = false)
        {
            if (pActor == null || !pActor.isAlive()) return;
            if (_root == null) Build();
            _actor = pActor;
            _root.gameObject.SetActive(true);
            if (pBesideUnitWindow) _root.anchoredPosition = new Vector2(268f, 0f);
            _root.SetAsLastSibling();
            Kit.ClampToScreen(_root);
            Refresh();
        }

        public static void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
            _actor = null;
        }

        public static bool IsOpen => _root != null && _root.gameObject.activeSelf;

        /// <summary>Called every frame; refreshes twice a second and follows the selected unit.</summary>
        public static void Update()
        {
            if (!IsOpen) return;
            if (Time.unscaledTime < _refresh_at) return;
            _refresh_at = Time.unscaledTime + 0.5f;
            Actor selected = SelectedUnit.unit;
            if (selected != null && selected != _actor && selected.isAlive() && ScrollWindow.isWindowActive()) _actor = selected;
            if (_actor == null || !_actor.isAlive())
            {
                Hide();
                return;
            }
            Refresh();
        }

        // ---------------------------------------------------------------- build

        private static void Build()
        {
            _root = Kit.Window("gi_character_panel", "Resonance", W, H, out _title, Hide);

            // Identity
            _emblem = Kit.Icon(_root, Kit.Emblem("other"), 12, 30, 22);
            _name = Kit.Label(_root, "", 38, 28, W - 70, 14, 10, Kit.Gold, TextAnchor.MiddleLeft, true);
            _power = Kit.Label(_root, "", 38, 42, W - 70, 11, 7);
            _badge = Kit.Icon(_root, Kit.Sprite("tier_1"), W - 32, 30, 20);
            _tier = Kit.Label(_root, "", 12, 54, W - 24, 10, 7, Kit.Muted, TextAnchor.MiddleLeft);
            Kit.Divider(_root, 10, 66, W - 20);

            // Powered layout
            _powered_group = Kit.Rect(_root, "powered", 0, 70, W, H - 70);
            Transform p = _powered_group;
            _rank = Kit.Label(p, "", 12, 0, 120, 12, 9, Kit.Cream, TextAnchor.MiddleLeft, true);
            _stars = Kit.Stars(p, W - 76, 2, 6, 9);
            _xp_fill = Kit.Bar(p, 12, 14, W - 24, 7, Color.white);
            _xp = Kit.Label(p, "", 12, 21, W - 24, 10, 6, Kit.Muted);
            _trial = Kit.Label(p, "", 12, 31, W - 24, 18, 6, Kit.Warn);

            Kit.Label(p, "Talents", 12, 50, 80, 10, 7, Kit.GoldDim, TextAnchor.MiddleLeft, true);
            string[] names = { "Strike", "Skill", "Burst" };
            Color[] colors = { new Color32(236, 229, 216, 255), new Color32(127, 216, 255, 255), new Color32(255, 214, 120, 255) };
            for (int i = 0; i < 3; i++)
            {
                float y = 61 + i * 11;
                Kit.Label(p, names[i], 12, y, 40, 10, 7);
                _talent_fill[i] = Kit.Bar(p, 52, y + 2, W - 96, 6, colors[i]);
                _talent_text[i] = Kit.Label(p, "", W - 42, y, 30, 10, 7, Kit.Cream, TextAnchor.MiddleRight);
            }

            Kit.Label(p, "Constellation", 12, 96, 90, 10, 7, Kit.GoldDim, TextAnchor.MiddleLeft, true);
            _nodes = new Image[6];
            for (int i = 0; i < 6; i++)
            {
                float x = 100 + i * 18, y = 95 + (i % 2 == 0 ? 0 : 4);
                _nodes[i] = Kit.Image(p, "node_dim", x, y, 11, 11);
            }
            _cons_note = Kit.Label(p, "", 12, 108, W - 24, 10, 6, Kit.Muted);

            Kit.Divider(p, 10, 119, W - 20);
            _activity = Kit.Label(p, "", 12, 123, W - 24, 10, 7);
            _record = Kit.Label(p, "", 12, 134, W - 24, 20, 6, Kit.Muted);

            _style = Kit.Label(p, "Techniques", 12, 156, W - 24, 10, 7, Kit.GoldDim, TextAnchor.MiddleLeft, true);
            _moves = Kit.Rect(p, "moves", 12, 167, W - 24, 90);

            // Mortal layout
            _mortal_group = Kit.Rect(_root, "mortal", 0, 70, W, H - 70);
            Transform m = _mortal_group;
            Kit.Label(m, "No Teyvat power… yet.", 12, 0, W - 24, 12, 8, Kit.Cream, TextAnchor.MiddleLeft, true);
            Kit.Label(m, "Potential", 12, 16, 60, 10, 7, Kit.GoldDim);
            _potential_fill = Kit.Bar(m, 64, 18, W - 76, 7, Kit.GoldDim);
            _potential_text = Kit.Label(m, "", 12, 28, W - 24, 10, 6, Kit.Muted);
            _wish_text = Kit.Label(m, "", 12, 40, W - 24, 40, 7);

            // Actions
            _actions = Kit.Rect(_root, "actions", 0, H - 58, W, 50);
            float bw = (W - 32) / 3f;
            Kit.Button(_actions, "Grant Vision", 12, 0, bw, 15, () => Act(TeyvatPowers.GrantVision));
            Kit.Button(_actions, "Ascend +10", 16 + bw, 0, bw, 15, () => Act(TeyvatPowers.Ascend));
            Kit.Button(_actions, "+ Constellation", 20 + bw * 2, 0, bw, 15, () => Act(TeyvatPowers.AddConstellation), "button", 6);
            Kit.Button(_actions, "Follow", 12, 19, bw, 15, () =>
            {
                if (_actor != null) MoveCamera.setFocusUnit(_actor);
            }, "button_dark");
            Kit.Button(_actions, "Legends", 16 + bw, 19, bw, 15, () => ArchiveWindow.Show(ArchiveWindow.Tab.Legends), "button_dark");
            Kit.Button(_actions, "Archive", 20 + bw * 2, 19, bw, 15, () => ArchiveWindow.Show(ArchiveWindow.Tab.Chronicle), "button_dark");
            _hint = Kit.Label(_actions, "Drag the title bar to move this panel.", 12, 36, W - 24, 10, 6, Kit.Muted, TextAnchor.MiddleCenter);
        }

        private static void Act(System.Func<Actor, bool> pAction)
        {
            if (_actor == null || !_actor.isAlive()) return;
            pAction(_actor);
            Refresh();
        }

        // ---------------------------------------------------------------- refresh

        private static void Refresh()
        {
            Actor a = _actor;
            if (a == null) return;
            MoveSet set = CombatCore.ResolveSet(a, out string trait_id);
            _name.text = a.getName();

            if (set == null)
            {
                _powered_group.gameObject.SetActive(false);
                _mortal_group.gameObject.SetActive(true);
                _emblem.sprite = Kit.Emblem("other");
                _emblem.color = Kit.Muted;
                _badge.gameObject.SetActive(false);
                _power.text = a.isSapient() ? "Mortal" : "Creature";
                _tier.text = Tiers.Description(0);
                float potential = Resonance.Potential(a);
                _potential_fill.fillAmount = Mathf.Clamp01(potential / 100f);
                _potential_text.text = $"{potential:0} / 100";
                _wish_text.text = potential >= 100f
                    ? "Their wish burns bright. Each year a Vision may answer, more likely in desperate battle."
                    : "Training, fighting and danger build potential toward a Vision.\nOr use <b>Grant Vision</b> below.";
                _title.text = "Resonance";
                return;
            }

            _powered_group.gameObject.SetActive(true);
            _mortal_group.gameObject.SetActive(false);
            Color element = Elements.Color(set.Element);
            Color tier_color = Tiers.Color(set.Tier);
            _emblem.sprite = Kit.Emblem(Elements.IsElement(set.Element) ? set.Element : "other");
            _emblem.color = element;
            _badge.gameObject.SetActive(true);
            _badge.sprite = Kit.Sprite("tier_" + Mathf.Clamp(set.Tier, 1, 7));
            _badge.color = tier_color;
            _power.text = $"<color={Kit.Hex(element)}>{LM.Get("trait_" + trait_id)}</color>";
            _tier.text = $"<color={Kit.Hex(tier_color)}>Tier {set.Tier} · {Tiers.Name(set.Tier)}</color>";
            _title.text = $"Resonance · {Training.Capital(set.Element)}";

            int rank = Resonance.Rank(a), phase = Resonance.Phase(a), cons = Resonance.Constellation(a);
            _rank.text = $"Rank {rank} <size=7><color=#A0A8BE>/ {Resonance.MaxRank}</color></size>";
            Kit.SetStars(_stars, phase);
            int gate = Resonance.StuckAtGate(a);
            if (rank >= Resonance.MaxRank)
            {
                _xp_fill.fillAmount = 1f;
                _xp.text = "Peak of Resonance";
            }
            else
            {
                float need = Resonance.XpToNext(rank);
                _xp_fill.fillAmount = Mathf.Clamp01(Resonance.Xp(a) / need);
                _xp.text = $"XP {Resonance.Xp(a):0} / {need:0}";
            }
            _xp_fill.color = element;
            _trial.text = gate > 0 ? $"! Breakthrough {gate}: {Resonance.TrialText(gate)} — {TrialProgress(a, gate)}" : "";

            var cats = new[] { Resonance.Category.Strike, Resonance.Category.Skill, Resonance.Category.Burst };
            for (int i = 0; i < 3; i++)
            {
                int level = Resonance.EffectiveTalent(a, cats[i]);
                _talent_fill[i].fillAmount = level / 13f;
                _talent_text[i].text = $"Lv {level}";
            }
            for (int i = 0; i < 6; i++)
            {
                _nodes[i].sprite = Kit.Sprite(i < cons ? "node_lit" : "node_dim");
                _nodes[i].color = i < cons ? element : Color.white;
            }
            _cons_note.text = cons >= 6 ? "C6 — Burst awakened (×1.5, impact frames)"
                : cons >= 5 ? "C5 — +2 to every talent" : cons >= 3 ? "C3 — +2 to every talent"
                : "Fateful moments light the stars.";

            _activity.text = "Now: " + Training.Activity(a);
            _record.text = $"Foes {Resonance.Kills(a)} · Elites {Resonance.Elites(a)} · Duels won {Resonance.DuelWins(a)} · " +
                           $"Spars won {Resonance.SparWins(a)}\nTraining sessions {Resonance.Sessions(a)} · Talent cap {Resonance.TalentCap(a)}";

            RefreshMoves(a, set);
        }

        private static string TrialProgress(Actor a, int pGate) => pGate switch
        {
            20 => $"{Mathf.Min(3, Resonance.Sessions(a))}/3 sessions",
            40 => $"{Resonance.SparWins(a) + Resonance.DuelWins(a)} wins, {Mathf.Min(5, Resonance.Kills(a))}/5 foes",
            50 => $"{Mathf.Min(10, Resonance.Kills(a))}/10 foes",
            60 => $"{Resonance.Elites(a)} elites, {Resonance.DuelWins(a)} duels",
            70 => $"{Mathf.Min(2, Resonance.DuelWins(a))}/2 duels",
            80 => $"{a.data.renown}/300 renown",
            _ => ""
        };

        private static readonly string[] Numerals = { "I", "II", "III" };

        private static void RefreshMoves(Actor a, MoveSet pSet)
        {
            Kit.Clear(_moves);
            CombatData data = CombatCore.Data(a);
            _style.text = $"{pSet.Name} <color=#A0A8BE>· techniques</color>";
            float y = 0f;
            foreach (string id in pSet.Moves)
            {
                Technique t = Library.Get(id);
                if (t == null) continue;
                bool known = Engine.Knows(a, t);
                int level = Engine.Level(a, t);
                string lv = known ? $"<color=#FFD678>{Numerals[level - 1]}</color>" : "<color=#5A6278>-</color>";
                string name = known ? t.Name : $"<color=#6A7288>{t.Name}</color>";
                string ult = t.Ultimate ? " <color=#FFD678>*</color>" : "";
                float left = data.ReadyAt.TryGetValue(id, out float ready) ? ready - CombatCore.Now : 0f;
                string state = !known ? $"<color=#6A7288>at {Numerals[t.Unlock - 1]}</color>"
                    : left > 0.1f ? $"<color=#A0A8BE>{left:0}s</color>"
                    : $"<color=#9BFF8A>{ShapeTag(t.Shape)}</color>";
                Kit.Label(_moves, $"{lv}  {name}{ult}", 0, y, W - 80, 10.5f, 7);
                Kit.Label(_moves, state, W - 80, y, 54, 10.5f, 6, null, TextAnchor.MiddleRight);
                y += 10.5f;
            }
            Technique god = Library.Get(pSet.God);
            if (god != null)
            {
                string gauge = data.Gauge >= 100f ? "<color=#FFD678>READY</color>" : $"<color=#A0A8BE>{data.Gauge:0}%</color>";
                Kit.Label(_moves, $"<color=#FFD678>GOD</color>  {god.Name}", 0, y, W - 80, 10.5f, 7);
                Kit.Label(_moves, gauge, W - 80, y, 54, 10.5f, 6, null, TextAnchor.MiddleRight);
            }
        }

        private static string ShapeTag(Shape pShape) => pShape switch
        {
            Shape.Crowd => "vs crowds",
            Shape.Line => "vs lines",
            Shape.Lone => "duelist",
            Shape.Execute => "execute",
            Shape.AntiAir => "anti-air",
            Shape.Gap => "closer",
            Shape.Guard => "guard",
            Shape.Support => "support",
            _ => "ready"
        };
    }
}
