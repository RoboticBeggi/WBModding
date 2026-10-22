using System.Collections.Generic;
using TeyvatChronicle.Chronicle;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TeyvatChronicle.UI
{
    /// <summary>
    /// A small on-screen panel (top right): the next Chronicle event with its countdown, and the five
    /// mightiest powers. Click a name to jump to them. Toggle it from the Teyvat tab or the settings.
    /// </summary>
    public static class Hud
    {
        private const float W = 168f, H = 96f;
        private static RectTransform _root;
        private static Text _next;
        private static readonly List<Text> _rows = new List<Text>();
        private static readonly List<Actor> _actors = new List<Actor>();
        private static float _refresh_at;
        private static bool _hidden_by_user;

        public static void Toggle()
        {
            _hidden_by_user = !_hidden_by_user;
            if (_root != null) _root.gameObject.SetActive(!_hidden_by_user);
        }

        public static void Update()
        {
            bool want = Settings.Hud && !_hidden_by_user && World.world != null && !Config.worldLoading;
            if (!want)
            {
                if (_root != null && _root.gameObject.activeSelf) _root.gameObject.SetActive(false);
                return;
            }
            if (_root == null) Build();
            if (!_root.gameObject.activeSelf) _root.gameObject.SetActive(true);
            if (Time.unscaledTime < _refresh_at) return;
            _refresh_at = Time.unscaledTime + 2f;
            Refresh();
        }

        private static void Build()
        {
            Image panel = Kit.Image(Kit.Root, "panel", 0, 0, W, H, new Color(1f, 1f, 1f, 0.9f));
            panel.name = "gi_hud";
            panel.raycastTarget = true;
            _root = panel.rectTransform;
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(1f, 1f);
            _root.anchoredPosition = new Vector2(-8f, -64f);
            Image grip = Kit.Image(_root, "header", 5, 5, W - 10, 13);
            grip.raycastTarget = true;
            grip.gameObject.AddComponent<Draggable>().Target = _root;
            Kit.Label(grip.transform, "TEYVAT", 4, 0, 60, 13, 7, Kit.Gold, TextAnchor.MiddleLeft, true);
            Button open = Kit.Button(_root, "Archive", W - 52, 6, 44, 11, () => ArchiveWindow.Show(ArchiveWindow.Tab.Chronicle), "button_dark", 6);
            open.transform.SetAsLastSibling();
            _next = Kit.Label(_root, "", 9, 20, W - 18, 20, 6, Kit.Cream, TextAnchor.UpperLeft);
            for (int i = 0; i < 5; i++)
            {
                int index = i;
                Text row = Kit.Label(_root, "", 9, 40 + i * 10, W - 18, 10, 6);
                row.raycastTarget = true;
                row.gameObject.AddComponent<Button>().onClick.AddListener(() => Jump(index));
                _rows.Add(row);
            }
        }

        private static void Jump(int pIndex)
        {
            if (pIndex >= _actors.Count || _actors[pIndex] == null || !_actors[pIndex].isAlive()) return;
            World.world.locatePosition(_actors[pIndex].current_position);
            ActionLibrary.openUnitWindow(_actors[pIndex]);
        }

        private static void Refresh()
        {
            ChronicleEvent next = ChronicleState.Ready ? ChronicleState.NextEvent : null;
            if (next != null)
            {
                int years = Mathf.Max(0, ChronicleState.NextYear - Date.getCurrentYear());
                _next.text = $"<color={next.Color}>{Announcer.Format(next.Title)}</color>\nin {years} yr{(years == 1 ? "" : "s")}" +
                             (ChronicleState.Paused ? " (paused)" : "");
            }
            else _next.text = "<color=#A0A8BE>Chronicle not running</color>";

            _actors.Clear();
            _actors.AddRange(Legends.Top(5));
            for (int i = 0; i < _rows.Count; i++)
            {
                if (i >= _actors.Count)
                {
                    _rows[i].text = i == 0 ? "<color=#A0A8BE>No legends yet</color>" : "";
                    continue;
                }
                Actor a = _actors[i];
                MoveSet set = CombatCore.ResolveSet(a, out _);
                string name = a.getName();
                if (name.Length > 16) name = name.Substring(0, 15) + "…";
                _rows[i].text = $"{i + 1}. <color={Kit.Hex(Elements.Color(set?.Element))}>{name}</color>" +
                                $"  <color={Kit.Hex(Tiers.Color(set?.Tier ?? 0))}>T{set?.Tier}</color> {Path.Numeral(Path.Stage(a))}";
            }
        }
    }
}
