using System;
using System.Collections.Generic;
using System.IO;
using NeoModLoader.General;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TeyvatChronicle.UI
{
    /// <summary>
    /// The Teyvat UI kit: its own overlay canvas that scales with the game's windows, Genshin-style
    /// 9-slice sprites from the mod's ui/ folder, and small widget builders. Positions are in UI units
    /// from the top-left of the parent, which keeps layouts simple and predictable.
    /// </summary>
    public static class Kit
    {
        public static readonly Color Gold = new Color32(244, 222, 160, 255);
        public static readonly Color GoldDim = new Color32(217, 181, 106, 255);
        public static readonly Color Cream = new Color32(236, 229, 216, 255);
        public static readonly Color Muted = new Color32(160, 168, 190, 255);
        public static readonly Color Warn = new Color32(255, 178, 127, 255);
        public static readonly Color Ink = new Color32(40, 34, 26, 255);

        private static readonly Dictionary<string, int> Borders = new Dictionary<string, int>
        {
            ["panel"] = 14, ["panel_inner"] = 6, ["header"] = 8, ["button"] = 6, ["button_dark"] = 6,
            ["button_gold"] = 6, ["bar_bg"] = 3, ["bar_fill"] = 2, ["tab"] = 6, ["tab_active"] = 6
        };

        private static readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        private static Canvas _canvas;
        private static CanvasScaler _scaler;
        private static string _folder;

        public static void Init(string pModFolder) => _folder = Path.Combine(pModFolder, "ui");

        // ---------------------------------------------------------------- canvas

        public static Transform Root
        {
            get
            {
                if (_canvas == null) CreateCanvas();
                return _canvas.transform;
            }
        }

        private static void CreateCanvas()
        {
            var go = new GameObject("gi_ui", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Object.DontDestroyOnLoad(go);
            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 28000;
            _scaler = go.GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            SyncScale();
        }

        /// <summary>Keeps our UI the same size as the game's windows (called every frame).</summary>
        public static void SyncScale()
        {
            if (_scaler == null || CanvasMain.instance == null) return;
            float scale = CanvasMain.instance.canvas_windows.scaleFactor * Settings.UiScale;
            if (Mathf.Abs(_scaler.scaleFactor - scale) > 0.001f) _scaler.scaleFactor = scale;
        }

        // ---------------------------------------------------------------- sprites

        public static Sprite Sprite(string pName)
        {
            if (_sprites.TryGetValue(pName, out Sprite cached)) return cached;
            Sprite sprite = null;
            string path = Path.Combine(_folder ?? "", pName + ".png");
            if (File.Exists(path))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.LoadImage(File.ReadAllBytes(path));
                tex.wrapMode = TextureWrapMode.Clamp;
                int b = Borders.TryGetValue(pName, out int border) ? border : 0;
                sprite = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
            }
            _sprites[pName] = sprite;
            return sprite;
        }

        public static Sprite Emblem(string pElement)
        {
            Sprite s = Sprite("emblem_" + pElement);
            return s != null ? s : Sprite("emblem_other");
        }

        // ---------------------------------------------------------------- layout primitives

        public static RectTransform Rect(Transform pParent, string pName, float pX, float pY, float pW, float pH)
        {
            var go = new GameObject(pName, typeof(RectTransform));
            go.transform.SetParent(pParent, false);
            var rect = go.GetComponent<RectTransform>();
            Place(rect, pX, pY, pW, pH);
            return rect;
        }

        public static void Place(RectTransform pRect, float pX, float pY, float pW, float pH)
        {
            pRect.anchorMin = pRect.anchorMax = new Vector2(0f, 1f);
            pRect.pivot = new Vector2(0f, 1f);
            pRect.anchoredPosition = new Vector2(pX, -pY);
            pRect.sizeDelta = new Vector2(pW, pH);
        }

        public static Image Image(Transform pParent, string pSprite, float pX, float pY, float pW, float pH,
            Color? pColor = null)
        {
            RectTransform rect = Rect(pParent, pSprite, pX, pY, pW, pH);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Sprite(pSprite);
            image.type = Borders.ContainsKey(pSprite) ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.color = pColor ?? Color.white;
            image.raycastTarget = false;
            return image;
        }

        public static Image Icon(Transform pParent, Sprite pSprite, float pX, float pY, float pSize, Color? pColor = null)
        {
            RectTransform rect = Rect(pParent, "icon", pX, pY, pSize, pSize);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = pSprite;
            image.preserveAspect = true;
            image.color = pColor ?? Color.white;
            image.raycastTarget = false;
            return image;
        }

        public static Text Label(Transform pParent, string pText, float pX, float pY, float pW, float pH,
            int pSize = 8, Color? pColor = null, TextAnchor pAlign = TextAnchor.MiddleLeft, bool pBold = false)
        {
            RectTransform rect = Rect(pParent, "label", pX, pY, pW, pH);
            var text = rect.gameObject.AddComponent<Text>();
            OT.InitializeCommonText(text);
            text.text = pText;
            text.fontSize = pSize;
            text.color = pColor ?? Cream;
            text.alignment = pAlign;
            text.fontStyle = pBold ? FontStyle.Bold : FontStyle.Normal;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var shadow = rect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
            shadow.effectDistance = new Vector2(0.6f, -0.6f);
            return text;
        }

        public static Button Button(Transform pParent, string pLabel, float pX, float pY, float pW, float pH,
            UnityAction pOnClick, string pStyle = "button", int pSize = 7)
        {
            Image bg = Image(pParent, pStyle, pX, pY, pW, pH);
            bg.raycastTarget = true;
            var button = bg.gameObject.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1.12f, 1.08f, 0.95f);
            colors.pressedColor = new Color(0.8f, 0.78f, 0.7f);
            button.colors = colors;
            if (pOnClick != null) button.onClick.AddListener(pOnClick);
            button.onClick.AddListener(() => MusicBox.playSound("event:/SFX/UI/Click", -1f, -1f));
            Color text_color = pStyle == "button_dark" ? Gold : Ink;
            Label(bg.transform, pLabel, 2, 0, pW - 4, pH, pSize, text_color, TextAnchor.MiddleCenter, true);
            return button;
        }

        /// <summary>A progress bar; returns the fill image (set fillAmount).</summary>
        public static Image Bar(Transform pParent, float pX, float pY, float pW, float pH, Color pColor)
        {
            Image bg = Image(pParent, "bar_bg", pX, pY, pW, pH);
            Image fill = Image(bg.transform, "bar_fill", 1, 1, pW - 2, pH - 2, pColor);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillAmount = 0f;
            return fill;
        }

        public static Image Divider(Transform pParent, float pX, float pY, float pW) =>
            Image(pParent, "divider", pX, pY, pW, 5);

        /// <summary>Six (or n) stars, the first k filled. Returns the images so they can be refreshed.</summary>
        public static Image[] Stars(Transform pParent, float pX, float pY, int pCount, float pSize = 9f)
        {
            var stars = new Image[pCount];
            for (int i = 0; i < pCount; i++) stars[i] = Image(pParent, "star_empty", pX + i * (pSize + 1), pY, pSize, pSize);
            return stars;
        }

        public static void SetStars(Image[] pStars, int pFilled)
        {
            for (int i = 0; i < pStars.Length; i++) pStars[i].sprite = Sprite(i < pFilled ? "star" : "star_empty");
        }

        // ---------------------------------------------------------------- windows

        /// <summary>A draggable window with a title strip and a close button.</summary>
        public static RectTransform Window(string pName, string pTitle, float pW, float pH, out Text pTitleText,
            Action pOnClose)
        {
            Image panel = Image(Root, "panel", 0, 0, pW, pH);
            panel.name = pName;
            panel.raycastTarget = true;
            RectTransform rect = panel.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;

            Image header = Image(rect, "header", 6, 6, pW - 12, 18);
            header.raycastTarget = true;
            header.gameObject.AddComponent<Draggable>().Target = rect;
            pTitleText = Label(header.transform, pTitle, 8, 0, pW - 40, 18, 9, Gold, TextAnchor.MiddleLeft, true);

            Image close = Image(rect, "close", pW - 20, 9, 12, 12);
            close.raycastTarget = true;
            close.gameObject.AddComponent<Button>().onClick.AddListener(() => pOnClose?.Invoke());
            return rect;
        }

        /// <summary>Keeps a centre-anchored window fully on screen.</summary>
        public static void ClampToScreen(RectTransform pRect)
        {
            if (pRect == null || _scaler == null) return;
            float scale = Mathf.Max(0.01f, _scaler.scaleFactor);
            float half_w = Screen.width / scale / 2f, half_h = Screen.height / scale / 2f;
            Vector2 size = pRect.sizeDelta;
            Vector2 p = pRect.anchoredPosition;
            float max_x = Mathf.Max(0f, half_w - size.x / 2f - 4f), max_y = Mathf.Max(0f, half_h - size.y / 2f - 4f);
            pRect.anchoredPosition = new Vector2(Mathf.Clamp(p.x, -max_x, max_x), Mathf.Clamp(p.y, -max_y, max_y));
        }

        /// <summary>A vertical scroll area. Returns the content transform; set its height as you fill it.</summary>
        public static RectTransform Scroll(Transform pParent, float pX, float pY, float pW, float pH)
        {
            Image frame = Image(pParent, "panel_inner", pX, pY, pW, pH);
            frame.raycastTarget = true;
            var mask = frame.gameObject.AddComponent<RectMask2D>();
            mask.padding = new Vector4(1, 1, 1, 1);
            var scroll = frame.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 18f;
            RectTransform content = Rect(frame.transform, "content", 0, 0, pW, 10);
            scroll.content = content;
            scroll.viewport = frame.rectTransform;
            return content;
        }

        public static void Clear(Transform pParent)
        {
            for (int i = pParent.childCount - 1; i >= 0; i--) Object.Destroy(pParent.GetChild(i).gameObject);
        }

        public static string Hex(Color pColor) => "#" + ColorUtility.ToHtmlStringRGB(pColor);
    }

    /// <summary>Drag a window by its header.</summary>
    public class Draggable : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        public RectTransform Target;
        private Vector2 _offset;

        public void OnBeginDrag(PointerEventData pData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Target.parent, pData.position,
                null, out Vector2 local);
            _offset = Target.anchoredPosition - local;
        }

        public void OnDrag(PointerEventData pData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Target.parent, pData.position,
                null, out Vector2 local);
            Target.anchoredPosition = local + _offset;
            if (Target.anchorMin == new Vector2(0.5f, 0.5f)) Kit.ClampToScreen(Target);
        }
    }
}
