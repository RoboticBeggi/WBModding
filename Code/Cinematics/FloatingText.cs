using System.Collections;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.UI;

namespace TeyvatChronicle.Cinematics
{
    /// <summary>Short labels that pop up over the battlefield: reaction names, PARRY!, COMBO x5, FINISH!</summary>
    public static class FloatingText
    {
        private static Canvas _canvas;
        private const int MaxLive = 24;
        private static int _live;

        public static void Show(string pText, Vector3 pWorld, Color pColor, int pSize = 14)
        {
            if (!Settings.FloatingText || _live >= MaxLive || !Director.OnScreen(pWorld)) return;
            Ensure();
            var go = new GameObject("gi_label", typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(_canvas.transform, false);
            var text = go.GetComponent<Text>();
            OT.InitializeCommonText(text);
            text.text = pText;
            text.fontSize = pSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.raycastTarget = false;
            text.color = pColor;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, pSize * 2f);
            _live++;
            ModClass.Instance.StartCoroutine(Animate(go, text, pWorld, pColor));
        }

        private static IEnumerator Animate(GameObject pGo, Text pText, Vector3 pWorld, Color pColor)
        {
            float t = 0f;
            const float life = 0.9f;
            var rect = pGo.GetComponent<RectTransform>();
            while (t < life && pGo != null)
            {
                t += Time.unscaledDeltaTime;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 screen = cam.WorldToScreenPoint(pWorld);
                    rect.position = new Vector3(screen.x, screen.y + 30f + 40f * t, 0f);
                }
                float pop = t < 0.12f ? Mathf.Lerp(1.6f, 1f, t / 0.12f) : 1f;
                rect.localScale = Vector3.one * pop;
                pText.color = new Color(pColor.r, pColor.g, pColor.b, t > 0.6f ? 1f - (t - 0.6f) / 0.3f : 1f);
                yield return null;
            }
            if (pGo != null) Object.Destroy(pGo);
            _live--;
        }

        private static void Ensure()
        {
            if (_canvas != null) return;
            var root = new GameObject("gi_labels", typeof(Canvas));
            Object.DontDestroyOnLoad(root);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 29980;
        }
    }
}
