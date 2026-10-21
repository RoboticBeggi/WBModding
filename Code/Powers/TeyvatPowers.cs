using System;
using System.Linq;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Progression;
using TeyvatChronicle.UI;
using UnityEngine;

namespace TeyvatChronicle.Powers
{
    /// <summary>God powers in the Teyvat tab. Select one, then click a unit (or a place, for Training Ground).</summary>
    public static class TeyvatPowers
    {
        public static readonly string[] Ids =
        {
            "gi_power_inspect", "gi_power_vision", "gi_power_element", "gi_power_delusion",
            "gi_power_ascend", "gi_power_constellation", "gi_power_archon", "gi_power_ground"
        };

        public static void Init()
        {
            OnUnit("gi_power_inspect", a => { ActionLibrary.openUnitWindow(a); CharacterPanel.Show(a, true); return true; });

            OnUnit("gi_power_vision", GrantVision);
            OnUnit("gi_power_element", ShiftElement);
            OnUnit("gi_power_delusion", GrantDelusion);
            OnUnit("gi_power_ascend", Ascend);
            OnUnit("gi_power_constellation", AddConstellation);
            OnUnit("gi_power_archon", CrownArchon);

            Add("gi_power_ground", (tile, id) =>
            {
                if (tile == null || Debounced(id)) return false;
                TrainingGrounds.Toggle(tile.posV3);
                return true;
            });
        }

        // ---------------------------------------------------------------- actions (also used by the character panel)

        /// <summary>Awakens a Vision (element from the kingdom's Archon or fate); on a Vision holder, +5 ranks.</summary>
        public static bool GrantVision(Actor a)
        {
            if (a.getTraits().Any(t => t.id.StartsWith("gi_vision_")))
            {
                Resonance.SetRank(a, Resonance.Rank(a) + 5);
                Resonance.Celebrate(a, $"RANK {Resonance.Rank(a)}", "heal", 0.7f);
            }
            else Training.Awaken(a, null);
            return true;
        }

        /// <summary>Cycles a Vision holder's element: Anemo → Geo → … → Cryo → Anemo.</summary>
        public static bool ShiftElement(Actor a)
        {
            string current = Elements.All.FirstOrDefault(e => a.hasTrait("gi_vision_" + e));
            int next = current == null ? 0 : (Array.IndexOf(Elements.All, current) + 1) % Elements.All.Length;
            Training.Awaken(a, Elements.All[next]);
            return true;
        }

        public static bool GrantDelusion(Actor a)
        {
            a.addTrait("gi_delusion");
            Resonance.Celebrate(a, "DELUSION", "dark", 0.8f);
            return true;
        }

        public static bool Ascend(Actor a)
        {
            if (!Resonance.IsPowered(a)) Training.Awaken(a, null);
            Resonance.SetRank(a, Resonance.Rank(a) + 10);
            Resonance.Celebrate(a, $"ASCEND → RANK {Resonance.Rank(a)}", "charge", 1f);
            return true;
        }

        public static bool AddConstellation(Actor a)
        {
            if (!Resonance.IsPowered(a)) Training.Awaken(a, null);
            Resonance.SetConstellation(a, Resonance.Constellation(a) + 1);
            Resonance.Celebrate(a, $"CONSTELLATION C{Resonance.Constellation(a)}", "moon", 0.9f);
            return true;
        }

        /// <summary>Crowns the unit Archon of its Vision's element (or a random one), taking the Gnosis from its holder.</summary>
        public static bool CrownArchon(Actor a)
        {
            string element = Elements.All.FirstOrDefault(e => a.hasTrait("gi_vision_" + e))
                             ?? Elements.All[Randy.randomInt(0, Elements.All.Length)];
            Ascension.Crown(a, element);
            return true;
        }

        private static float _last_use;
        private static string _last_power;

        /// <summary>The game repeats clicks while the mouse is held, so each power fires at most every 0.6 s.</summary>
        private static bool Debounced(string pId)
        {
            if (_last_power == pId && Time.unscaledTime - _last_use < 0.6f) return true;
            _last_power = pId;
            _last_use = Time.unscaledTime;
            return false;
        }

        private static void OnUnit(string pId, Func<Actor, bool> pAction)
        {
            Add(pId, (tile, id) =>
            {
                Actor actor = ActionLibrary.getActorFromTile(tile);
                if (actor == null || !actor.isAlive() || Debounced(pId)) return false;
                return pAction(actor);
            });
        }

        private static void Add(string pId, PowerActionWithID pAction)
        {
            if (AssetManager.powers.has(pId)) return;
            var power = new GodPower
            {
                id = pId,
                name = pId,
                path_icon = "gi/icons/" + pId,
                allow_unit_selection = true,
                unselect_when_window = true
            };
            power.click_action = pAction;
            AssetManager.powers.add(power);
        }
    }
}
