using System.Collections.Generic;
using System.Linq;
using NeoModLoader.General;
using NeoModLoader.General.UI.Prefabs;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TeyvatChronicle.UI
{
    /// <summary>The Resonance card for one unit, and the Teyvat Legends leaderboard.</summary>
    public static class ResonanceWindows
    {
        private const string CardId = "gi_resonance_card";
        private const string LegendsId = "gi_legends";
        private static Text _card_text;
        private static ScrollWindow _legends;
        private static RectTransform _legends_content;

        // ---------------------------------------------------------------- card

        public static void ShowCard(Actor a)
        {
            if (a == null || !a.isAlive()) return;
            if (_card_text == null)
            {
                ScrollWindow window = WindowCreator.CreateEmptyWindow(CardId, "gi_window_card_title", "gi/icons/btn_inspect");
                _card_text = MakeText(window.transform_content, new Vector2(196, 300));
            }
            _card_text.text = Describe(a);
            ScrollWindow.showWindow(CardId);
        }

        public static string Describe(Actor a)
        {
            var lines = new List<string> { $"<b><size=10>{a.getName()}</size></b>" };
            MoveSet set = CombatCore.ResolveSet(a, out string trait_id);
            if (set == null)
            {
                lines.Add("<i>No Teyvat power… yet.</i>");
                lines.Add("");
                lines.Add($"Potential: {Resonance.Potential(a):0} / 100");
                lines.Add(Resonance.Potential(a) >= 100f
                    ? "Their wish is strong. A Vision may answer."
                    : "Training and battle build potential toward a Vision.");
                return string.Join("\n", lines);
            }

            string hex = "#" + ColorUtility.ToHtmlStringRGB(Elements.Color(set.Element));
            int rank = Resonance.Rank(a), phase = Resonance.Phase(a), cons = Resonance.Constellation(a);
            lines.Add($"<color={hex}>{LM.Get("trait_" + trait_id)}</color> · Tier {set.Tier}");
            lines.Add("");
            lines.Add($"<b>Resonance Rank {rank} / {Resonance.MaxRank}</b>");
            lines.Add($"Ascension {Resonance.Stars(phase)}");
            int gate = Resonance.StuckAtGate(a);
            if (rank >= Resonance.MaxRank) lines.Add("Max rank reached");
            else lines.Add($"XP {Resonance.Xp(a):0} / {Resonance.XpToNext(rank):0}");
            if (gate > 0) lines.Add($"<color=#FFB27F>Breakthrough {gate}: {Resonance.TrialText(gate)}</color>");
            lines.Add("");
            lines.Add($"Talents  Strike {Resonance.EffectiveTalent(a, Resonance.Category.Strike)} · " +
                      $"Skill {Resonance.EffectiveTalent(a, Resonance.Category.Skill)} · " +
                      $"Burst {Resonance.EffectiveTalent(a, Resonance.Category.Burst)}  (cap {Resonance.TalentCap(a)})");
            lines.Add($"Constellation <b>C{cons}</b>{(cons >= 6 ? " — Burst upgraded" : "")}");
            lines.Add("");
            lines.Add($"Training sessions {Resonance.Sessions(a)} · Spar wins {Resonance.SparWins(a)}");
            lines.Add($"Duel wins {Resonance.DuelWins(a)} · Foes defeated {Resonance.Kills(a)} · Elites {Resonance.Elites(a)}");
            lines.Add($"Move power ×{Resonance.MoveBoost(a, new MoveAsset { Archetype = "strike" }):0.00} (Strike)");
            lines.Add("");
            lines.Add("<b>Moves</b>");
            foreach (string id in set.Moves)
            {
                MoveAsset m = MoveData.Get(id);
                if (m != null) lines.Add($"· {m.Name}{(m.Energy > 0 ? " <color=#FFE58A>(Burst)</color>" : "")}");
            }
            return string.Join("\n", lines);
        }

        // ---------------------------------------------------------------- legends

        public static void ShowLegends()
        {
            if (_legends == null)
            {
                _legends = WindowCreator.CreateEmptyWindow(LegendsId, "gi_window_legends_title", "gi/icons/btn_legends");
                _legends_content = _legends.transform_content;
                var layout = _legends_content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.spacing = 2;
                layout.padding = new RectOffset(4, 4, 6, 6);
                _legends_content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
            for (int i = _legends_content.childCount - 1; i >= 0; i--) Object.Destroy(_legends_content.GetChild(i).gameObject);

            var legends = World.world.units.getSimpleList()
                .Where(u => u != null && u.isAlive() && Resonance.IsPowered(u))
                .Select(u => new { Unit = u, Tier = Resonance.Tier(u), Rank = Resonance.Rank(u) })
                .OrderByDescending(x => x.Tier * 1000 + x.Rank * 10 + Resonance.Constellation(x.Unit))
                .Take(25).ToList();

            if (legends.Count == 0)
            {
                MakeText(_legends_content, new Vector2(200, 30)).text = "No legends yet. Give someone a Vision!";
            }
            int place = 1;
            foreach (var entry in legends)
            {
                Actor unit = entry.Unit;
                MoveSet set = CombatCore.ResolveSet(unit, out _);
                SimpleButton button = SimpleButton.Instantiate(_legends_content);
                string label = $"{place++}. {unit.getName()}  T{entry.Tier} · R{entry.Rank} · C{Resonance.Constellation(unit)}";
                Sprite icon = SpriteTextureLoader.getSprite("gi/icons/traits/" + CombatCore.ResolveSetTrait(unit));
                button.Setup(() =>
                {
                    World.world.locatePosition(unit.current_position);
                    ShowCard(unit);
                }, icon, label, new Vector2(200, 18));
                button.Text.alignment = TextAnchor.MiddleLeft;
                button.Text.fontSize = 7;
                button.Text.color = Elements.Color(set?.Element);
            }
            ScrollWindow.showWindow(LegendsId);
        }

        private static Text MakeText(Transform pParent, Vector2 pSize)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(pParent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = pSize;
            rect.localPosition = new Vector3(pSize.x / 2f + 20f, -pSize.y / 2f - 4f);
            var text = go.GetComponent<Text>();
            OT.InitializeCommonText(text);
            text.alignment = TextAnchor.UpperLeft;
            text.supportRichText = true;
            text.fontSize = 7;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
