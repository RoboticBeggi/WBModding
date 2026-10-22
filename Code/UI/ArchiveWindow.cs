using System.Collections.Generic;
using System.Linq;
using NeoModLoader.api;
using NeoModLoader.ui;
using TeyvatChronicle.Chronicle;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TeyvatChronicle.UI
{
    /// <summary>
    /// The Teyvat Archive: one window for everything. Chronicle (a timeline slider to start or jump anywhere), Legends (the mightiest powers), Powers (the tier codex and every fighting style) and a Guide.
    /// </summary>
    public static class ArchiveWindow
    {
        public enum Tab { Chronicle, Legends, Powers, Guide }

        private const float W = 330f, H = 410f;
        private static RectTransform _root, _body;
        private static Text _title;
        private static readonly Image[] _tab_images = new Image[4];
        private static Tab _tab;
        private static float _refresh_at;
        private static int _selected_event = -1;

        // Chronicle tab widgets that refresh live
        private static Text _status_era, _status_next, _pause_label;

        public static bool IsOpen => _root != null && _root.gameObject.activeSelf;

        public static void Show(Tab pTab)
        {
            if (_root == null) Build();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
            Kit.ClampToScreen(_root);
            Select(pTab);
        }

        public static void Toggle()
        {
            if (IsOpen) Hide();
            else Show(_tab);
        }

        public static void Hide()
        {
            if (_root != null) _root.gameObject.SetActive(false);
        }

        public static void Update()
        {
            if (!IsOpen || Time.unscaledTime < _refresh_at) return;
            _refresh_at = Time.unscaledTime + 1f;
            if (_tab == Tab.Chronicle) RefreshStatus();
        }

        private static void Build()
        {
            _root = Kit.Window("gi_archive", "Teyvat Archive", W, H, out _title, Hide);
            string[] names = { "Chronicle", "Legends", "Powers", "Guide" };
            float tw = (W - 24) / 4f;
            for (int i = 0; i < 4; i++)
            {
                Tab tab = (Tab)i;
                Image img = Kit.Image(_root, "tab", 12 + i * tw, 28, tw - 2, 16);
                img.raycastTarget = true;
                img.gameObject.AddComponent<Button>().onClick.AddListener(() => Select(tab));
                Kit.Label(img.transform, names[i], 0, 0, tw - 2, 16, 8, Kit.Gold, TextAnchor.MiddleCenter, true);
                _tab_images[i] = img;
            }
            _body = Kit.Rect(_root, "body", 0, 48, W, H - 52);
        }

        private static void Select(Tab pTab)
        {
            _tab = pTab;
            for (int i = 0; i < 4; i++) _tab_images[i].sprite = Kit.Sprite(i == (int)pTab ? "tab_active" : "tab");
            Kit.Clear(_body);
            switch (pTab)
            {
                case Tab.Chronicle: BuildChronicle(); break;
                case Tab.Legends: BuildLegends(); break;
                case Tab.Powers: BuildPowers(); break;
                case Tab.Guide: BuildGuide(); break;
            }
        }

        // ---------------------------------------------------------------- Chronicle
        // One timeline slider to pick a moment, one card that tells its story, one button to go there.

        private const float TrackX = 34f;
        private static float TrackW => W - 68f;

        private static NotchSlider _timeline, _pace;
        private static RectTransform _now_marker;
        private static Text _sel_era, _sel_count, _card_title, _card_meta, _card_text, _go_label, _pace_label;
        private static Image _go_image;

        private static void BuildChronicle()
        {
            Transform b = _body;
            List<ChronicleEvent> events = ChronicleState.Events;
            int count = Mathf.Max(1, events.Count);
            if (_selected_event < 0 || _selected_event >= events.Count)
                _selected_event = ChronicleState.Ready && ChronicleState.Active ? Mathf.Min(ChronicleState.Index, count - 1) : 0;

            // Where we are now
            Kit.Image(b, "panel_inner", 12, 0, W - 24, 34);
            _status_era = Kit.Label(b, "", 20, 3, W - 40, 14, 10, Kit.Gold, TextAnchor.MiddleLeft, true);
            _status_next = Kit.Label(b, "", 20, 17, W - 40, 13, 7);

            // The timeline
            _sel_count = Kit.Label(b, "", W - 120, 40, 106, 10, 6, Kit.Muted, TextAnchor.MiddleRight);
            Kit.Label(b, "TIMELINE", 14, 40, 80, 10, 6, Kit.GoldDim, TextAnchor.MiddleLeft, true);
            DrawEraBands(b, events, 56f);
            Kit.Button(b, "<", 12, 56, 18, 16, () => Step(-1), "button_dark", 9);
            Kit.Button(b, ">", W - 30, 56, 18, 16, () => Step(1), "button_dark", 9);
            _timeline = Kit.Slider(b, TrackX, 56, TrackW, count, _selected_event, OnTimeline, false);
            _now_marker = Kit.Image(b, "bar_fill", TrackX, 73, 3, 4, new Color32(155, 255, 138, 255)).rectTransform;
            string first = events.Count > 0 ? ChronicleData.EraTitle(events[0].Era) : "";
            string last = events.Count > 0 ? ChronicleData.EraTitle(events[events.Count - 1].Era) : "";
            Kit.Label(b, first, TrackX - 4, 77, 110, 10, 6, Kit.Muted);
            Kit.Label(b, last, TrackX + TrackW - 106, 77, 110, 10, 6, Kit.Muted, TextAnchor.MiddleRight);
            _sel_era = Kit.Label(b, "", TrackX + 60, 77, TrackW - 120, 10, 7, Kit.Gold, TextAnchor.MiddleCenter, true);

            // The chosen moment
            Kit.Image(b, "panel_inner", 12, 92, W - 24, 156);
            _card_title = Kit.Label(b, "", 20, 96, W - 40, 15, 10, Kit.Gold, TextAnchor.MiddleLeft, true);
            _card_meta = Kit.Label(b, "", 20, 111, W - 40, 10, 6, Kit.Muted);
            RectTransform story = Kit.Scroll(b, 16, 124, W - 32, 120);
            story.GetComponentInParent<Image>().color = new Color(1f, 1f, 1f, 0f);
            _card_text = Kit.Label(story, "", 4, 2, W - 44, 10, 7, Kit.Cream, TextAnchor.UpperLeft);

            // Go there
            Button go = Kit.Button(b, "Begin here", 12, 254, W - 24, 20, GoHere, "button_gold", 9);
            _go_image = go.GetComponent<Image>();
            _go_label = go.GetComponentInChildren<Text>();
            float bw = (W - 28) / 3f;
            Button pause = Kit.Button(b, "Pause", 12, 278, bw, 15, () =>
            {
                if (!ChronicleState.Ready || !ChronicleState.Active) return;
                ChronicleState.Paused = !ChronicleState.Paused;
                RefreshStatus();
            }, "button_dark");
            _pause_label = pause.GetComponentInChildren<Text>();
            Kit.Button(b, "Next event now", 14 + bw, 278, bw, 15, () => { ChronicleState.Advance(); RefreshStatus(); }, "button_dark");
            Kit.Button(b, "Stop", 16 + bw * 2, 278, bw, 15, () => { ChronicleState.Stop(); RefreshStatus(); }, "button_dark");

            // Pace
            Kit.Divider(b, 20, 299, W - 40);
            Kit.Label(b, "PACE", 14, 308, 40, 12, 6, Kit.GoldDim, TextAnchor.MiddleLeft, true);
            _pace = Kit.Slider(b, 52, 306, 110, 4, (int)Settings.Pacing, SetPacing);
            string[] stops = { "Lore", "Normal", "Fast", "Custom" };
            for (int i = 0; i < 4; i++)
                Kit.Label(b, stops[i], 52 + Kit.StopX(110, 4, i) - 18, 322, 36, 9, 6, Kit.Muted, TextAnchor.MiddleCenter);
            _pace_label = Kit.Label(b, "", 170, 306, W - 186, 16, 7, Kit.Cream, TextAnchor.MiddleRight);
            Kit.Button(b, "More settings", W - 92, 334, 80, 14, OpenSettings, "button_dark", 6);

            ShowEvent(_selected_event);
            RefreshStatus();
        }

        /// <summary>Era colours under the track and a tick for every event (taller for major ones).</summary>
        private static void DrawEraBands(Transform b, List<ChronicleEvent> events, float y)
        {
            int n = events.Count;
            if (n == 0) return;
            int start = 0;
            for (int i = 1; i <= n; i++)
            {
                if (i < n && events[i].Era == events[start].Era) continue;
                float x0 = Kit.StopX(TrackW, n, start), x1 = Kit.StopX(TrackW, n, i - 1);
                Color c = Toolbox.makeColor(events[start].Color);
                c.a = 0.55f;
                Kit.Image(b, "bar_fill", TrackX + x0 - 2, y + 4, Mathf.Max(4f, x1 - x0 + 4), 8, c);
                start = i;
            }
            for (int i = 0; i < n; i++)
            {
                float h = events[i].Major ? 5f : 3f;
                Kit.Image(b, "bar_fill", TrackX + Kit.StopX(TrackW, n, i) - 0.5f, y - h + 3, 1, h,
                    events[i].Major ? Kit.Gold : new Color(1f, 1f, 1f, 0.45f));
            }
        }

        private static void Step(int pDelta)
        {
            if (_timeline == null) return;
            _timeline.SetValue(_timeline.Value + pDelta, true);
        }

        private static void OnTimeline(int pIndex)
        {
            _selected_event = pIndex;
            ShowEvent(pIndex);
            RefreshStatus();
        }

        private static void ShowEvent(int pIndex)
        {
            List<ChronicleEvent> events = ChronicleState.Events;
            if (_card_title == null || pIndex < 0 || pIndex >= events.Count) return;
            ChronicleEvent ev = events[pIndex];
            _card_title.text = Announcer.Format(ev.Title);
            _card_title.color = Toolbox.makeColor(ev.Color);
            _card_meta.text = $"{ev.When}" + (ev.Major ? "  ·  <color=#FFE58A>major event</color>" : "") +
                              (ChronicleState.IsEnabled(ev) ? "" : "  ·  <color=#FFB27F>turned off in settings</color>");
            _card_text.text = Announcer.Format(ev.Text);
            _card_text.rectTransform.sizeDelta = new Vector2(W - 44, _card_text.preferredHeight + 4);
            ((RectTransform)_card_text.transform.parent).sizeDelta = new Vector2(W - 32, _card_text.preferredHeight + 8);
            _sel_era.text = ChronicleData.EraTitle(ev.Era);
            _sel_count.text = $"{pIndex + 1} / {events.Count}";
        }

        private static void GoHere()
        {
            if (!ChronicleState.Ready) return;
            if (ChronicleState.Active && ChronicleState.Index == _selected_event) return;
            if (ChronicleState.Active) ChronicleState.JumpTo(_selected_event);
            else ChronicleState.StartAt(_selected_event);
            RefreshStatus();
        }

        private static void RefreshStatus()
        {
            if (_status_era == null) return;
            int total = ChronicleState.Events.Count;
            bool running = ChronicleState.Ready && ChronicleState.Active;
            if (!ChronicleState.Ready)
            {
                _status_era.text = "No world loaded";
                _status_next.text = "Load or create a world first.";
            }
            else if (ChronicleState.NextEvent == null)
            {
                _status_era.text = ChronicleState.Active ? "The Chronicle is complete" : "The Chronicle hasn't begun";
                _status_next.text = ChronicleState.Active ? "Every event has played. Pick a moment to relive it."
                    : "Slide to the moment you want history to start from, then press <b>Begin here</b>.";
            }
            else
            {
                ChronicleEvent next = ChronicleState.NextEvent;
                int years = Mathf.Max(0, ChronicleState.NextYear - Date.getCurrentYear());
                _status_era.text = ChronicleData.EraTitle(next.Era) + $"  <size=7><color=#A0A8BE>Year {Date.getCurrentYear()}</color></size>";
                _status_next.text = ChronicleState.Paused
                    ? $"<color=#FFB27F>Paused</color> before <color={next.Color}>{Announcer.Format(next.Title)}</color>"
                    : $"Next: <color={next.Color}>{Announcer.Format(next.Title)}</color> in {years} year{(years == 1 ? "" : "s")}";
            }

            if (_now_marker != null)
            {
                _now_marker.gameObject.SetActive(running);
                if (running)
                {
                    int now = Mathf.Min(ChronicleState.Index, Mathf.Max(0, total - 1));
                    _now_marker.anchoredPosition = new Vector2(TrackX + Kit.StopX(TrackW, Mathf.Max(1, total), now) - 1.5f,
                        _now_marker.anchoredPosition.y);
                }
            }
            if (_go_label != null)
            {
                bool here = running && ChronicleState.Index == _selected_event;
                _go_label.text = !ChronicleState.Ready ? "No world loaded"
                    : here ? "You are here"
                    : running ? (_selected_event > ChronicleState.Index ? "Jump ahead to here" : "Go back to here")
                    : "Begin here";
                _go_image.sprite = Kit.Sprite(here || !ChronicleState.Ready ? "button_dark" : "button_gold");
                _go_label.color = here || !ChronicleState.Ready ? Kit.Gold : Kit.Ink;
            }
            if (_pause_label != null) _pause_label.text = ChronicleState.Paused ? "Resume" : "Pause";
            if (_pace != null && _pace.Value != (int)Settings.Pacing) _pace.SetValue((int)Settings.Pacing, false);
            if (_pace_label != null)
            {
                string[] names = { "Lore", "Normal", "Fast", "Custom" };
                int pacing = Mathf.Clamp((int)Settings.Pacing, 0, 3);
                _pace_label.text = ChronicleState.Ready
                    ? $"{names[pacing]} · whole timeline <b>~{ChronicleState.TotalYears()} years</b>"
                    : names[pacing];
            }
        }

        private static void SetPacing(int pIndex)
        {
            ModConfig config = ModClass.Instance.GetConfig();
            config["Chronicle"]["Pacing"].SetValue(pIndex);
            config.Save();
            RefreshStatus();
        }

        private static void OpenSettings() => ModConfigureWindow.ShowWindow(ModClass.Instance.GetConfig());

        // ---------------------------------------------------------------- Legends

        private static void BuildLegends()
        {
            Transform b = _body;
            Kit.Label(b, "The mightiest powers in this world. Click one to open their unit window.", 14, 0, W - 28, 12, 7, Kit.Muted);
            RectTransform list = Kit.Scroll(b, 12, 16, W - 24, H - 72);
            var legends = Legends.Top(40);
            float y = 2f;
            if (legends.Count == 0)
            {
                Kit.Label(list, "No legends yet. Give someone a Vision, or begin the Chronicle!", 8, 6, W - 40, 24, 8, Kit.Cream);
                y = 30f;
            }
            int place = 1;
            foreach (Actor unit in legends)
            {
                MoveSet set = CombatCore.ResolveSet(unit, out string trait);
                Color element = Elements.Color(set.Element);
                Image row = Kit.Image(list, "panel_inner", 4, y, W - 34, 22, new Color(1f, 1f, 1f, 0.7f));
                row.raycastTarget = true;
                Actor target = unit;
                row.gameObject.AddComponent<Button>().onClick.AddListener(() =>
                {
                    World.world.locatePosition(target.current_position);
                    ActionLibrary.openUnitWindow(target);
                });
                Kit.Label(row.transform, $"{place++}", 2, 0, 18, 22, 9, Kit.Gold, TextAnchor.MiddleCenter, true);
                Kit.Icon(row.transform, Kit.Emblem(Elements.IsElement(set.Element) ? set.Element : "other"), 22, 3, 16, element);
                Kit.Label(row.transform, unit.getName(), 42, 1, 150, 11, 8, Kit.Cream, TextAnchor.MiddleLeft, true);
                Kit.Label(row.transform, $"<color={Kit.Hex(Tiers.Color(set.Tier))}>{Tiers.Name(set.Tier)}</color> · {NeoModLoader.General.LM.Get("trait_" + trait)}",
                    42, 11, 190, 10, 6, Kit.Muted);
                Kit.Label(row.transform, $"R{Resonance.Rank(unit)}  {Resonance.Stars(Resonance.Phase(unit))}  C{Resonance.Constellation(unit)}",
                    W - 140, 0, 104, 22, 6, Kit.Gold, TextAnchor.MiddleRight);
                y += 24f;
            }
            list.sizeDelta = new Vector2(W - 24, y + 4);
        }

        // ---------------------------------------------------------------- Powers

        private static void BuildPowers()
        {
            RectTransform list = Kit.Scroll(_body, 12, 0, W - 24, H - 56);
            float y = 4f;
            for (int tier = 0; tier <= 7; tier++)
            {
                Color color = Tiers.Color(tier);
                if (tier > 0) Kit.Icon(list, Kit.Sprite("tier_" + tier), 6, y, 18, color);
                Kit.Label(list, $"{(tier == 0 ? "Mortals" : $"Tier {tier} · {Tiers.Name(tier)}")}", 28, y, W - 60, 12, 9, color, TextAnchor.MiddleLeft, true);
                y += 14f;
                Kit.Label(list, Tiers.Description(tier), 28, y, W - 64, 26, 7, Kit.Cream, TextAnchor.UpperLeft);
                y += 26f;
                if (tier > 0)
                {
                    Kit.Label(list, "How: " + Tiers.HowToGet(tier), 28, y, W - 64, 10, 6, Kit.Muted);
                    y += 13f;
                }
                Kit.Divider(list, 20, y, W - 60);
                y += 7f;
            }
            Kit.Label(list, "How fighters fight", 8, y, W - 40, 12, 9, Kit.Gold, TextAnchor.MiddleLeft, true);
            y += 14f;
            Text how = Kit.Label(list,
                "Every power is a <b>fighting style</b> with its own techniques. A tactical mind picks them: crowds draw slams, " +
                "lines draw fissures and beams, a lone wounded foe draws an execution, fliers draw leaps, far foes draw dashes.\n" +
                "Techniques grow from <color=#FFD678>I</color> to <color=#FFD678>III</color> through rank and practice " +
                "(training drills them too). Level III burns white-hot, cracks the land and freezes the world on impact.\n" +
                "Masters <b>guard</b> weak blows and turn arrows back. Allies combine into <b>team arts</b>. " +
                "<b>Duels</b> bring locks, aerial clashes, chases, feints and catastrophes. Archons and above fill a " +
                "<b>God gauge</b> and stop the world with a <b>God ability</b>.",
                8, y, W - 44, 10, 7, Kit.Cream, TextAnchor.UpperLeft);
            how.rectTransform.sizeDelta = new Vector2(W - 44, how.preferredHeight + 4);
            y += how.preferredHeight + 10f;
            foreach (MoveSet style in Arts.Library.Styles.Values.OrderBy(st => st.Tier))
            {
                Color sc = Elements.Color(style.Element);
                Kit.Label(list, $"<color={Kit.Hex(sc)}>{style.Name}</color>  <color=#A0A8BE>{NeoModLoader.General.LM.Get("trait_" + style.TraitId)} · T{style.Tier}</color>",
                    8, y, W - 40, 11, 8, Kit.Cream, TextAnchor.MiddleLeft, true);
                y += 12f;
                var names = style.Moves.Select(Arts.Library.Get).Where(t => t != null).Select(t => t.Ultimate ? t.Name + "*" : t.Name).ToList();
                Arts.Technique god = Arts.Library.Get(style.God);
                if (god != null) names.Add($"<color=#FFD678>God: {god.Name}</color>");
                Text line = Kit.Label(list, string.Join(" · ", names), 16, y, W - 52, 10, 6, Kit.Muted, TextAnchor.UpperLeft);
                line.rectTransform.sizeDelta = new Vector2(W - 52, line.preferredHeight + 2);
                y += line.preferredHeight + 6f;
            }
            list.sizeDelta = new Vector2(W - 24, y);
        }

        // ---------------------------------------------------------------- Guide

        private static void BuildGuide()
        {
            RectTransform list = Kit.Scroll(_body, 12, 0, W - 24, H - 56);
            Text text = Kit.Label(list, Guide.Text, 8, 4, W - 44, 10, 7, Kit.Cream, TextAnchor.UpperLeft);
            text.rectTransform.sizeDelta = new Vector2(W - 44, text.preferredHeight + 8);
            list.sizeDelta = new Vector2(W - 24, text.preferredHeight + 16);
        }
    }

    /// <summary>The ranking used by the Archive and the on-screen HUD.</summary>
    public static class Legends
    {
        public static List<Actor> Top(int pCount)
        {
            return World.world.units.getSimpleList()
                .Where(u => u != null && u.isAlive() && Resonance.IsPowered(u))
                .OrderByDescending(u => Resonance.Tier(u) * 100000 + Resonance.Rank(u) * 100 + Resonance.Constellation(u) * 10 + Resonance.Phase(u))
                .Take(pCount).ToList();
        }
    }
}
