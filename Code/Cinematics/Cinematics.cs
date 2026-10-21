using System;
using System.Collections;
using NeoModLoader.General;
using TeyvatChronicle.Combat;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TeyvatChronicle.Cinematics
{
    /// <summary>
    /// Anime-style presentation: hit-stop, black-and-white impact frames with speed lines, camera punches,
    /// slow motion and letterboxed cutscenes with title cards. Everything runs on real time so it works
    /// while the world itself is frozen or slowed.
    /// </summary>
    public static class Director
    {
        /// <summary>Multiplier for VFX time: 0 during hit-stop, below 1 in slow motion.</summary>
        public static float FxTimeScale { get; private set; } = 1f;

        public static bool CutscenePlaying { get; private set; }

        private static WorldTimeScaleAsset _saved_speed;
        private static WorldTimeScaleAsset _slow_speed;
        private static int _time_locks;
        private static float _last_cutscene = -999f;
        private static float _last_impact = -999f;

        private static Canvas _canvas;
        private static RectTransform _bar_top, _bar_bottom;
        private static Image _impact, _lines, _spot;
        private static float _last_ceremony = -999f;
        private static Text _title, _subtitle;

        // ---------------------------------------------------------------- visibility

        /// <summary>True when a world position is on screen and the camera is close enough to care.</summary>
        public static bool OnScreen(Vector3 pWorld)
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector3 v = cam.WorldToViewportPoint(pWorld);
            return v.z >= 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f
                   && cam.orthographicSize < 90f;
        }

        // ---------------------------------------------------------------- time

        private static void PushTime(float pMultiplier, float pFx)
        {
            if (_time_locks == 0) _saved_speed = Config.time_scale_asset;
            _time_locks++;
            _slow_speed ??= new WorldTimeScaleAsset { id = "gi_cinematic", ticks = 1 };
            // Copy the display fields so the speed button keeps its icon while we slow time.
            _slow_speed.path_icon = _saved_speed?.path_icon;
            _slow_speed.locale_key = _saved_speed?.locale_key;
            _slow_speed.multiplier = Mathf.Max(0.001f, (_saved_speed?.multiplier ?? 1f) * pMultiplier);
            Config.time_scale_asset = _slow_speed;
            FxTimeScale = pFx;
        }

        private static void PopTime()
        {
            _time_locks = Mathf.Max(0, _time_locks - 1);
            if (_time_locks > 0) return;
            if (_saved_speed != null) Config.time_scale_asset = _saved_speed;
            FxTimeScale = 1f;
        }

        // ---------------------------------------------------------------- impact frames

        /// <summary>
        /// Hit-stop, then a white flash, a black frame with radial speed lines, an element-tinted frame,
        /// a camera punch and a shake. Strength 0..1 scales every part.
        /// </summary>
        public static void Impact(Vector3 pWorld, Color pColor, float pStrength = 1f)
        {
            if (!Settings.ImpactFrames || !OnScreen(pWorld)) return;
            if (Time.unscaledTime - _last_impact < 0.6f) return;
            _last_impact = Time.unscaledTime;
            ModClass.Instance.StartCoroutine(ImpactRoutine(pWorld, pColor, Mathf.Clamp01(pStrength) * Settings.ImpactIntensity));
        }

        private static IEnumerator ImpactRoutine(Vector3 pWorld, Color pColor, float pStrength)
        {
            EnsureCanvas();
            PushTime(0.02f, 0f);
            PositionLines(pWorld);
            CombatCore.Sound("impact", pWorld);

            // 1: white flash
            SetImpact(Color.white, 0.95f * pStrength, false);
            yield return new WaitForSecondsRealtime(0.035f);
            // 2: black frame with speed lines (the classic anime impact frame)
            SetImpact(Color.black, 0.9f * pStrength, true);
            _lines.color = Color.white;
            yield return new WaitForSecondsRealtime(0.06f + 0.05f * pStrength);
            // 3: inverted, element-tinted frame
            SetImpact(pColor, 0.55f * pStrength, true);
            _lines.color = Color.black;
            yield return new WaitForSecondsRealtime(0.05f);

            PopTime();
            World.world.startShake(0.35f + 0.4f * pStrength, 0.01f, 2f + 5f * pStrength, true, true);
            ModClass.Instance.StartCoroutine(ZoomPunch(0.82f, 0.4f));

            // Fade out
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - t / 0.25f;
                _impact.color = new Color(pColor.r, pColor.g, pColor.b, 0.4f * pStrength * k);
                _lines.color = new Color(1f, 1f, 1f, 0.6f * k);
                yield return null;
            }
            SetImpact(Color.clear, 0f, false);
        }

        private static IEnumerator ZoomPunch(float pFactor, float pSeconds)
        {
            MoveCamera cam = MoveCamera.instance;
            if (cam == null) yield break;
            float original = cam.getTargetZoom();
            cam.setTargetZoom(Mathf.Max(10f, original * pFactor));
            yield return new WaitForSecondsRealtime(pSeconds);
            cam.setTargetZoom(original);
        }

        // ---------------------------------------------------------------- cutscenes

        public static bool CanPlayCutscene(bool pForce = false)
        {
            if (!Settings.Cutscenes || CutscenePlaying) return false;
            return pForce || Time.unscaledTime - _last_cutscene >= Settings.CutsceneCooldown;
        }

        /// <summary>
        /// Letterbox bars slide in, the camera flies to the focus and zooms in, time slows down, a title card
        /// appears, then everything eases back. Yield on it to run code after the cutscene.
        /// </summary>
        public static IEnumerator Cutscene(string pTitle, string pSubtitle, Vector3? pFocus, Color pColor,
            float pHold = 1.6f, bool pForce = false, Action pOnClimax = null)
        {
            if (!CanPlayCutscene(pForce))
            {
                pOnClimax?.Invoke();
                yield break;
            }
            CutscenePlaying = true;
            _last_cutscene = Time.unscaledTime;
            EnsureCanvas();

            MoveCamera cam = MoveCamera.instance;
            float original_zoom = cam != null ? cam.getTargetZoom() : 0f;
            if (pFocus.HasValue)
            {
                World.world.locatePosition(pFocus.Value);
                if (cam != null) cam.setTargetZoom(Mathf.Clamp(original_zoom * 0.55f, 14f, 40f));
            }

            PushTime(0.2f, 0.35f);
            if (pFocus.HasValue) ModClass.Instance.StartCoroutine(Spotlight(pFocus.Value, pHold + 1.3f));
            _title.text = pTitle;
            _subtitle.text = pSubtitle ?? "";
            _title.color = new Color(pColor.r, pColor.g, pColor.b, 0f);
            _subtitle.color = new Color(1f, 1f, 1f, 0f);

            // Bars and title in
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                SetBars(k);
                _title.color = new Color(pColor.r, pColor.g, pColor.b, k);
                _subtitle.color = new Color(1f, 1f, 1f, k * 0.9f);
                _title.transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, k);
                yield return null;
            }

            yield return new WaitForSecondsRealtime(pHold);
            pOnClimax?.Invoke();
            yield return new WaitForSecondsRealtime(0.5f);

            // Out
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.SmoothStep(0f, 1f, t / 0.4f);
                SetBars(k);
                _title.color = new Color(pColor.r, pColor.g, pColor.b, k);
                _subtitle.color = new Color(1f, 1f, 1f, k * 0.9f);
                yield return null;
            }
            SetBars(0f);
            PopTime();
            if (cam != null && pFocus.HasValue) cam.setTargetZoom(original_zoom);
            CutscenePlaying = false;
        }

        // ---------------------------------------------------------------- spotlight and ceremonies

        /// <summary>Darkens the screen around a world position (the camera spotlight).</summary>
        public static IEnumerator Spotlight(Vector3 pWorld, float pSeconds)
        {
            if (!Settings.Spotlight) yield break;
            EnsureCanvas();
            if (_spot.sprite == null) yield break;
            float t = 0f;
            while (t < pSeconds)
            {
                t += Time.unscaledDeltaTime;
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 screen = cam.WorldToScreenPoint(pWorld);
                    var rect = _spot.rectTransform;
                    rect.position = new Vector3(screen.x, screen.y, 0f);
                    float size = Mathf.Max(Screen.width, Screen.height) * 2.6f;
                    rect.sizeDelta = new Vector2(size, size) / Mathf.Max(0.01f, _canvas.scaleFactor);
                }
                float k = Mathf.Min(1f, t / 0.3f) * Mathf.Min(1f, (pSeconds - t) / 0.4f);
                _spot.color = new Color(1f, 1f, 1f, Mathf.Clamp01(k));
                yield return null;
            }
            _spot.color = Color.clear;
        }

        /// <summary>
        /// An ascension ceremony: the world stops, the spotlight falls on the unit, rings close in on
        /// it, then a pillar of light and a blast outward with the title.
        /// </summary>
        public static void Ceremony(Actor pActor, string pTitle, Color pColor, float pSize = 1f)
        {
            if (pActor == null || !OnScreen(pActor.current_position)) return;
            if (!Settings.Ceremonies || Time.unscaledTime - _last_ceremony < 2f)
            {
                FloatingText.Show(pTitle, pActor.current_position, pColor, 14);
                return;
            }
            _last_ceremony = Time.unscaledTime;
            ModClass.Instance.StartCoroutine(CeremonyRoutine(pActor, pTitle, pColor, pSize));
        }

        private static IEnumerator CeremonyRoutine(Actor a, string pTitle, Color pColor, float pSize)
        {
            Vector3 p = a.current_position;
            PushTime(0.02f, 1f);
            ModClass.Instance.StartCoroutine(Spotlight(p, 1.7f));
            CombatCore.Sound("ascend", p);
            for (int i = 0; i < 3; i++)
            {
                Fx.Instance ring = Fx.Play("nova", p, 6f * pSize, pColor, 0f, 0.5f);
                Fx.Grow(ring, 0.15f);
                yield return new WaitForSecondsRealtime(0.16f);
            }
            Fx.Play("circle", p, 3.2f * pSize, pColor, 0f, 1.4f, true, 14f, "EffectsBack");
            yield return new WaitForSecondsRealtime(0.25f);
            PopTime();
            Fx.Play("pillar", p + new Vector3(0f, 3f * pSize), 5.5f * pSize, pColor);
            Fx.Play("burst", p, 3f * pSize, Color.white);
            Fx.Instance outward = Fx.Play("nova", p, 2f * pSize, pColor);
            Fx.Grow(outward, 2.4f);
            World.world.startShake(0.4f, 0.01f, 3f, true, true);
            FloatingText.Show(pTitle, p, pColor, 18);
        }

        // ---------------------------------------------------------------- overlay

        private static void SetImpact(Color pColor, float pAlpha, bool pLines)
        {
            _impact.color = new Color(pColor.r, pColor.g, pColor.b, pAlpha);
            _lines.enabled = pLines && _lines.sprite != null;
        }

        private static void SetBars(float pAmount)
        {
            float height = Screen.height * 0.13f * pAmount;
            _bar_top.sizeDelta = new Vector2(0f, height);
            _bar_bottom.sizeDelta = new Vector2(0f, height);
        }

        private static void PositionLines(Vector3 pWorld)
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(pWorld);
            var rect = _lines.rectTransform;
            rect.position = new Vector3(screen.x, screen.y, 0f);
            float size = Mathf.Max(Screen.width, Screen.height) * 2.2f;
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
        }

        private static void EnsureCanvas()
        {
            if (_canvas != null) return;
            var root = new GameObject("gi_cinematics", typeof(Canvas));
            Object.DontDestroyOnLoad(root);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 29990;

            _spot = MakeImage(root.transform, "Spotlight", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            _spot.sprite = UI.Kit.Sprite("vignette");
            _impact = MakeImage(root.transform, "Impact", Vector2.zero, Vector2.one);
            _lines = MakeImage(root.transform, "Lines", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            Sprite[] lines = Fx.Frames("speedlines");
            _lines.sprite = lines != null && lines.Length > 0 ? lines[0] : null;
            _lines.enabled = false;

            _bar_top = MakeImage(root.transform, "BarTop", new Vector2(0f, 1f), new Vector2(1f, 1f)).rectTransform;
            _bar_top.pivot = new Vector2(0.5f, 1f);
            _bar_bottom = MakeImage(root.transform, "BarBottom", new Vector2(0f, 0f), new Vector2(1f, 0f)).rectTransform;
            _bar_bottom.pivot = new Vector2(0.5f, 0f);
            _bar_top.GetComponent<Image>().color = Color.black;
            _bar_bottom.GetComponent<Image>().color = Color.black;
            SetBars(0f);

            _title = MakeText(root.transform, "Title", 0.2f, 34);
            _subtitle = MakeText(root.transform, "Subtitle", 0.12f, 16);
            SetImpact(Color.clear, 0f, false);
        }

        private static Image MakeImage(Transform pParent, string pName, Vector2 pMin, Vector2 pMax)
        {
            var go = new GameObject(pName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(pParent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = pMin;
            rect.anchorMax = pMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = Color.clear;
            return image;
        }

        private static Text MakeText(Transform pParent, string pName, float pAnchorY, int pSize)
        {
            var go = new GameObject(pName, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(pParent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, pAnchorY);
            rect.anchorMax = new Vector2(1f, pAnchorY);
            rect.sizeDelta = new Vector2(0f, pSize * 2.2f);
            var text = go.GetComponent<Text>();
            OT.InitializeCommonText(text);
            text.fontSize = pSize;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
            go.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }
}
