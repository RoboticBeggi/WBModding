using NeoModLoader.General;
using NeoModLoader.General.UI.Prefabs;
using TeyvatChronicle.Chronicle;
using UnityEngine;
using UnityEngine.UI;

namespace TeyvatChronicle.UI
{
    /// <summary>
    /// Lists every event in the timeline. Clicking one starts (or restarts) the chronicle there.
    /// </summary>
    public static class ChronicleWindow
    {
        public const string WindowId = "gi_chronicle_timeline";
        private static ScrollWindow _window;

        public static void Show()
        {
            if (_window == null) Build();
            ScrollWindow.showWindow(WindowId);
        }

        private static void Build()
        {
            _window = WindowCreator.CreateEmptyWindow(WindowId, "gi_window_timeline_title", "gi/icons/chronicle");
            RectTransform content = _window.transform_content;

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 2;
            layout.padding = new RectOffset(4, 4, 6, 6);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Sprite icon = SpriteTextureLoader.getSprite("gi/icons/chronicle");
            string current_era = null;
            for (int i = 0; i < ChronicleState.Events.Count; i++)
            {
                ChronicleEvent ev = ChronicleState.Events[i];
                if (ev.Era != current_era)
                {
                    current_era = ev.Era;
                    AddHeader(content, ChronicleData.EraTitle(ev.Era));
                }
                int index = i;
                SimpleButton button = SimpleButton.Instantiate(content);
                string label = $"{(ev.Major ? "★ " : "")}{Announcer.Format(ev.Title)}  ({ev.When})";
                button.Setup(() =>
                {
                    ChronicleState.StartAt(index);
                    ScrollWindow.hideAllEvent();
                }, icon, label, new Vector2(200, 18));
                button.Text.alignment = TextAnchor.MiddleLeft;
                button.Text.fontSize = 7;
            }
        }

        private static void AddHeader(Transform pParent, string pTitle)
        {
            var header = new GameObject("Era", typeof(RectTransform), typeof(Text));
            header.transform.SetParent(pParent, false);
            header.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 14);
            var text = header.GetComponent<Text>();
            OT.InitializeCommonText(text);
            text.text = pTitle;
            text.fontSize = 9;
            text.fontStyle = FontStyle.Bold;
            text.color = Toolbox.makeColor("#FFE58A");
            text.alignment = TextAnchor.MiddleCenter;
        }
    }
}
