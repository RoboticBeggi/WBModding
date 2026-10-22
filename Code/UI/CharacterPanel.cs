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
    /// The Path panel. It opens beside WorldBox's unit window whenever you inspect a unit and updates live:
    /// power and tier, Stage on the Crucible Path (and whether it is primed or in its Crucible), the four
    /// facets, marks, battle record, what the unit is doing, every technique and the God gauge.
    /// </summary>
    public static class CharacterPanel
    {
        private const float W = 230f, H = 392f;
        private static RectTransform _root;
        private static Actor _actor;
        private static float _refresh_at;

        // Widgets
        private static Text _title, _name, _power, _tier, _stage, _state, _state_note, _activity, _record, _marks_note, _hint;
        private static Image _emblem, _badge;
        private static readonly Image[] _facet_fill = new Image[4];
        private static readonly Text[] _facet_text = new Text[4];
        private static readonly Image[] _mark_chips = new Image[8];
        private static readonly Text[] _mark_text = new Text[8];
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
            _root = Kit.Window("gi_character_panel", "Path", W, H, out _title, Hide);

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
            _stage = Kit.Label(p, "", 12, 0, W - 90, 13, 9, Kit.Cream, TextAnchor.MiddleLeft, true);
            _state = Kit.Label(p, "", W - 90, 0, 78, 13, 8, Kit.Warn, TextAnchor.MiddleRight, true);
            _state_note = Kit.Label(p, "", 12, 13, W - 24, 18, 6, Kit.Muted, TextAnchor.UpperLeft);

            Kit.Label(p, "Facets", 12, 31, 80, 10, 7, Kit.GoldDim, TextAnchor.MiddleLeft, true);
            for (int i = 0; i < 4; i++)
            {
                Path.Facet f = Path.Facets[i];
                float y = 42 + i * 11;
                Kit.Label(p, f.ToString(), 12, y, 40, 10, 7, Path.FacetColor(f));
                _facet_fill[i] = Kit.Bar(p, 52, y + 2, W - 110, 6, Path.FacetColor(f));
                _facet_text[i] = Kit.Label(p, "", W - 56, y, 44, 10, 6, Kit.Cream, TextAnchor.MiddleRight);
            }

            Kit.Label(p, "Marks", 12, 88, 80, 10, 7, Kit.GoldDim, TextAnchor.MiddleLeft, true);
            float cw = (W - 30) / 4f;
            for (int i = 0; i < 8; i++)
            {
                float x = 12 + (i % 4) * (cw + 2), y = 99 + (i / 4) * 12;
                _mark_chips[i] = Kit.Image(p, "button", x, y, cw, 11);
                _mark_text[i] = Kit.Label(_mark_chips[i].transform, "", 1, 0, cw - 2, 11, 6, Kit.Cream, TextAnchor.MiddleCenter, true);
            }
            _marks_note = Kit.Label(p, "", 12, 123, W - 24, 10, 6, Kit.Muted);

            Kit.Divider(p, 10, 134, W - 20);
            _activity = Kit.Label(p, "", 12, 138, W - 24, 10, 7);
            _record = Kit.Label(p, "", 12, 149, W - 24, 10, 6, Kit.Muted);

            _style = Kit.Label(p, "Techniques", 12, 160, W - 24, 10, 7, Kit.GoldDim, TextAnchor.MiddleLeft, true);
            _moves = Kit.Rect(p, "moves", 12, 171, W - 24, 86);

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
            Kit.Button(_actions, "Temper +5", 16 + bw, 0, bw, 15, () => Act(TeyvatPowers.Temper));
            Kit.Button(_actions, "Grant Mark", 20 + bw * 2, 0, bw, 15, () => Act(TeyvatPowers.GrantMark));
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
                float potential = Path.Potential(a);
                _potential_fill.fillAmount = Mathf.Clamp01(potential / 100f);
                _potential_text.text = $"{potential:0} / 100";
                _wish_text.text = potential >= 100f
                    ? "Their wish burns bright. Each year a Vision may answer, more likely in desperate battle."
                    : "Training, fighting and danger build potential toward a Vision.\nOr use <b>Grant Vision</b> below.";
                _title.text = "Path";
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
            _title.text = $"Path · {Training.Capital(set.Element)}";

            int stage = Path.Stage(a), cap = Path.Cap(a);
            _stage.text = stage > 0
                ? $"Stage {Path.Numeral(stage)} · {Path.StageName(stage)}"
                : "Unawakened";
            if (Path.InCrucible(a))
            {
                _state.text = $"CRUCIBLE {Path.CrucibleLeft(a):0}s";
                _state_note.text = $"Still standing when the time runs out, it breaks through to {Path.StageName(stage + 1)}. If it falls, it falls.";
            }
            else if (Path.IsPrimed(a))
            {
                _state.text = "PRIMED";
                float years = Mathf.Max(0f, 15f - Path.PrimedYears(a));
                _state_note.text = $"Its next serious fight is its Crucible (or {years:0} more years of meditation).";
            }
            else
            {
                _state.text = "";
                _state_note.text = stage >= Path.MaxStage
                    ? "The summit of the Path. Facets can still grow to 40."
                    : $"Facets cap at {cap}. Reach it in two facets to become primed for the Crucible.";
            }

            for (int i = 0; i < 4; i++)
            {
                Path.Facet f = Path.Facets[i];
                int level = Path.Level(a, f);
                float progress = level >= cap ? 1f : Path.FacetXp(a, f) / Path.XpToNext(level);
                _facet_fill[i].fillAmount = (level + Mathf.Clamp01(progress)) / Path.MaxFacet;
                _facet_text[i].text = level >= cap ? $"<color=#FFD678>{level}</color>/{cap}" : $"{level}/{cap}";
            }

            int shown = 0;
            foreach (Path.Mark m in Path.AllMarks)
            {
                if (!Path.HasMark(a, m)) continue;
                _mark_chips[shown].gameObject.SetActive(true);
                _mark_chips[shown].color = Path.MarkColor(m);
                _mark_text[shown].text = m.ToString();
                _mark_text[shown].color = Kit.Ink;
                shown++;
            }
            for (int i = shown; i < 8; i++) _mark_chips[i].gameObject.SetActive(false);
            Path.Mark latest = Path.Mark.None;
            foreach (Path.Mark m in Path.AllMarks) if (Path.HasMark(a, m)) latest = m;
            _marks_note.text = shown == 0
                ? "No marks yet. Deeds leave marks: surviving, slaying rulers, duels, rescues."
                : $"{shown}/8 · {latest}: {Path.MarkText(latest)}";

            _activity.text = "Now: " + Training.Activity(a);
            _record.text = $"Foes {Path.Kills(a)} · Duels won {Path.DuelWins(a)} · Spars won {Path.SparWins(a)} · " +
                           $"Rescues {Path.Rescues(a)} · Sessions {Path.Sessions(a)}";

            RefreshMoves(a, set);
        }

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
