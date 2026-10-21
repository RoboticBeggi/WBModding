using System.Collections.Generic;
using NeoModLoader.General;
using NeoModLoader.General.UI.Tab;
using TeyvatChronicle.Chronicle;
using UnityEngine;
using UnityEngine.Events;

namespace TeyvatChronicle.UI
{
    /// <summary>The "Teyvat" tab in the god-powers bar.</summary>
    public static class TeyvatTab
    {
        private const string GroupChronicle = "chronicle";

        public static void Init()
        {
            PowersTab tab = TabManager.CreateTab("Teyvat", "gi_tab_title", "gi_tab_desc",
                SpriteTextureLoader.getSprite("gi/icons/tab"));
            tab.SetLayout(new List<string> { GroupChronicle });

            AddButton(tab, "gi_btn_begin", "gi/icons/btn_begin", () => ChronicleState.StartAtEra(Settings.StartEra));
            AddButton(tab, "gi_btn_timeline", "gi/icons/btn_timeline", ChronicleWindow.Show);
            AddButton(tab, "gi_btn_advance", "gi/icons/btn_advance", ChronicleState.Advance);
            AddButton(tab, "gi_btn_pause", "gi/icons/btn_pause", TogglePause);
            AddButton(tab, "gi_btn_status", "gi/icons/btn_status", ShowStatus);
            AddButton(tab, "gi_btn_stop", "gi/icons/btn_stop", ChronicleState.Stop);

            tab.UpdateLayout();
        }

        private static void AddButton(PowersTab pTab, string pId, string pIcon, UnityAction pAction)
        {
            PowerButton button = PowerButtonCreator.CreateSimpleButton(pId, pAction, SpriteTextureLoader.getSprite(pIcon));
            pTab.AddPowerButton(GroupChronicle, button);
        }

        private static void TogglePause()
        {
            if (!ChronicleState.Ready || !ChronicleState.Active) return;
            ChronicleState.Paused = !ChronicleState.Paused;
            WorldTip.showNowTop(ChronicleState.Paused ? "The Chronicle is paused." : "The Chronicle resumes.", false);
        }

        private static void ShowStatus()
        {
            if (!ChronicleState.Ready) return;
            ChronicleEvent next = ChronicleState.NextEvent;
            if (next == null)
            {
                WorldTip.showNowTop("The Teyvat Chronicle is not running.", false);
                return;
            }
            int years = Mathf.Max(0, ChronicleState.NextYear - Date.getCurrentYear());
            string paused = ChronicleState.Paused ? " (paused)" : "";
            WorldTip.showNowTop($"Next: {Announcer.Format(next.Title)} in {years} years{paused}", false);
        }
    }
}
