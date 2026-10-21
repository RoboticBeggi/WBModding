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
    /// The Teyvat Archive: one window for everything. Chronicle (status, pacing, controls, every event,
    /// start anywhere), Legends (the mightiest powers), Powers (the tier codex and reactions) and a Guide.
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
        private static Text _status_era, _status_next, _status_detail, _pause_label;
        private static Image _progress;
        private static readonly Image[] _pacing = new Image[4];

        public static bool IsOpen => _root != null && _root.gameObject.activeSelf;

        public static void Show(Tab pTab)
        {
            if (_root == null) Build();
            _root.gameObject.SetActive(true);
            _root.SetAsLastSibling();
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

        private static void BuildChronicle()
        {
            Transform b = _body;
            Kit.Image(b, "panel_inner", 12, 0, W - 24, 62);
            _status_era = Kit.Label(b, "", 18, 3, W - 36, 13, 9, Kit.Gold, TextAnchor.MiddleLeft, true);
            _status_next = Kit.Label(b, "", 18, 16, W - 36, 11, 7);
            _progress = Kit.Bar(b, 18, 29, W - 36, 6, Kit.GoldDim);
            _status_detail = Kit.Label(b, "", 18, 36, W - 36, 10, 6, Kit.Muted);

            string[] pacing = { "Lore", "Normal", "Fast", "Custom" };
            float pw = (W - 90) / 4f;
            Kit.Label(b, "Pacing", 18, 48, 40, 12, 7, Kit.GoldDim);
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                Button button = Kit.Button(b, pacing[i], 60 + i * (pw + 2), 47, pw, 13, () => SetPacing(index), "button_dark", 6);
                _pacing[i] = button.GetComponent<Image>();
            }

            float bw = (W - 24 - 8) / 5f;
            Kit.Button(b, "Begin", 12, 68, bw, 15, () =>
            {
                if (_selected_event >= 0) ChronicleState.StartAt(_selected_event);
                else ChronicleState.StartAtEra(Settings.StartEra);
                RefreshStatus();
            }, "button_gold");
            Kit.Button(b, "Advance", 14 + bw, 68, bw, 15, () => { ChronicleState.Advance(); RefreshStatus(); });
            Button pause = Kit.Button(b, "Pause", 16 + bw * 2, 68, bw, 15, () =>
            {
                if (!ChronicleState.Ready || !ChronicleState.Active) return;
                ChronicleState.Paused = !ChronicleState.Paused;
                RefreshStatus();
            });
            _pause_label = pause.GetComponentInChildren<Text>();
            Kit.Button(b, "Stop", 18 + bw * 3, 68, bw, 15, () => { ChronicleState.Stop(); RefreshStatus(); });
            Kit.Button(b, "Settings", 20 + bw * 4, 68, bw, 15, OpenSettings, "button_dark");

            // Event list
            RectTransform list = Kit.Scroll(b, 12, 88, W - 24, 168);
            float y = 2f;
            string era = null;
            List<ChronicleEvent> events = ChronicleState.Events;
            for (int i = 0; i < events.Count; i++)
            {
                ChronicleEvent ev = events[i];
                if (ev.Era != era)
                {
                    era = ev.Era;
                    Kit.Label(list, ChronicleData.EraTitle(era).ToUpper(), 6, y, W - 40, 11, 7, Kit.GoldDim, TextAnchor.MiddleLeft, true);
                    y += 12f;
                }
                int index = i;
                Image row = Kit.Image(list, "panel_inner", 4, y, W - 34, 12, new Color(1f, 1f, 1f, 0.5f));
                row.raycastTarget = true;
                row.gameObject.AddComponent<Button>().onClick.AddListener(() => SelectEvent(index));
                string marker = Marker(i);
                string star = ev.Major ? "<color=#FFE58A>★</color> " : "";
                Kit.Label(row.transform, $"{marker} {star}{Announcer.Format(ev.Title)}", 4, 0, W - 120, 12, 7);
                Kit.Label(row.transform, ev.When, W - 120, 0, 82, 12, 6, Kit.Muted, TextAnchor.MiddleRight);
                y += 13f;
            }
            list.sizeDelta = new Vector2(W - 24, y + 4);

            // Details
            Kit.Image(b, "panel_inner", 12, 260, W - 24, 96);
            if (_selected_event >= 0 && _selected_event < events.Count) BuildDetails(b, events[_selected_event]);
            else
            {
                Kit.Label(b, "Click an event to read its story. <b>Begin</b> then starts the Chronicle from it.\n" +
                             "With no event selected, <b>Begin</b> starts from the era chosen in Settings.",
                    18, 266, W - 36, 80, 7, Kit.Muted, TextAnchor.UpperLeft);
            }
            RefreshStatus();
        }

        private static void BuildDetails(Transform b, ChronicleEvent ev)
        {
            Kit.Label(b, Announcer.Format(ev.Title), 18, 263, W - 36, 13, 9, Toolbox.makeColor(ev.Color), TextAnchor.MiddleLeft, true);
            Kit.Label(b, $"{ChronicleData.EraTitle(ev.Era)} · {ev.When}" + (ev.Major ? " · major event" : ""),
                18, 276, W - 36, 10, 6, Kit.Muted);
            Kit.Label(b, Announcer.Format(ev.Text), 18, 287, W - 36, 50, 7, Kit.Cream, TextAnchor.UpperLeft);
            string effects = string.Join(", ", ev.Effects.Select(e => (string)e["type"]).Distinct());
            Kit.Label(b, "Effects: " + (effects.Length > 0 ? effects : "story only"), 18, 340, W - 120, 12, 6, Kit.Muted);
            Kit.Button(b, "Start here", W - 98, 338, 80, 14, () =>
            {
                ChronicleState.StartAt(_selected_event);
                Select(Tab.Chronicle);
            }, "button_gold");
        }

        private static void SelectEvent(int pIndex)
        {
            _selected_event = pIndex;
            Select(Tab.Chronicle);
        }

        private static string Marker(int pIndex)
        {
            if (!ChronicleState.Ready || !ChronicleState.Active) return "<color=#5A6280>-</color>";
            if (pIndex < ChronicleState.Index) return "<color=#9BFF8A>+</color>";
            if (pIndex == ChronicleState.Index) return "<color=#FFE58A>></color>";
            return "<color=#5A6280>-</color>";
        }

        private static void RefreshStatus()
        {
            if (_status_era == null) return;
            int total = ChronicleState.Events.Count;
            if (!ChronicleState.Ready)
            {
                _status_era.text = "No world loaded";
                _status_next.text = "";
                _status_detail.text = "";
                _progress.fillAmount = 0f;
                return;
            }
            ChronicleEvent next = ChronicleState.NextEvent;
            if (next == null)
            {
                _status_era.text = "The Chronicle is not running";
                _status_next.text = "Press <b>Begin</b>, or pick any event below and press <b>Start here</b>.";
                _progress.fillAmount = 0f;
            }
            else
            {
                int years = Mathf.Max(0, ChronicleState.NextYear - Date.getCurrentYear());
                _status_era.text = ChronicleData.EraTitle(next.Era);
                _status_next.text = $"Next: <color={next.Color}>{Announcer.Format(next.Title)}</color> in {years} year{(years == 1 ? "" : "s")}" +
                                    (ChronicleState.Paused ? "  <color=#FFB27F>(paused)</color>" : "");
                _progress.fillAmount = total > 0 ? ChronicleState.Index / (float)total : 0f;
            }
            _status_detail.text = $"Event {Mathf.Min(ChronicleState.Index + 1, total)} of {total} · Year {Date.getCurrentYear()} · " +
                                  $"{(Settings.ChronicleCutscenes ? "cutscenes on" : "cutscenes off")}";
            int pacing = (int)Settings.Pacing;
            for (int i = 0; i < 4; i++) _pacing[i].sprite = Kit.Sprite(i == pacing ? "button_gold" : "button_dark");
            if (_pause_label != null) _pause_label.text = ChronicleState.Paused ? "Resume" : "Pause";
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
            Kit.Label(list, "Elemental reactions", 8, y, W - 40, 12, 9, Kit.Gold, TextAnchor.MiddleLeft, true);
            y += 14f;
            Kit.Label(list,
                "A hit leaves its element on the target for 4 seconds. Another element on top reacts:\n" +
                "<color=#FFD27F>Vaporize</color> Hydro+Pyro ×1.8 · <color=#FFB27F>Melt</color> Cryo+Pyro ×1.8 · " +
                "<color=#FF8A9A>Overloaded</color> Electro+Pyro, explodes · <color=#C7B8FF>Superconduct</color> Cryo+Electro, slows · " +
                "<color=#D49CFF>Electro-Charged</color> ×1.4 · <color=#BFEFFF>Frozen</color> Hydro+Cryo · " +
                "<color=#FF9A3A>Burning</color> Dendro+Pyro · <color=#9BFF8A>Bloom</color> Dendro+Hydro, a seed bursts · " +
                "<color=#C8FF6A>Aggravate</color> Dendro+Electro ×1.45 · <color=#74E5C5>Swirl</color> Anemo spreads it · " +
                "<color=#F2D27A>Crystallize</color> Geo, the attacker gets a shield.",
                8, y, W - 44, 80, 7, Kit.Cream, TextAnchor.UpperLeft);
            y += 84f;
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
