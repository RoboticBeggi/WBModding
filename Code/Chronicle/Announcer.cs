using System.Collections;
using NeoModLoader.General;
using TeyvatChronicle.Cinematics;
using UnityEngine;
using UnityEngine.UI;

namespace TeyvatChronicle.Chronicle
{
    /// <summary>
    /// Makes chronicle events impossible to miss: a big banner, a full-screen flash, screen shake,
    /// a sound stinger, a world-log entry, a camera jump and an optional lore pop-up.
    /// Each channel can be turned off in the mod settings.
    /// </summary>
    public static class Announcer
    {
        private const string LogAssetId = "gi_chronicle";
        private const string LoreWindowId = "gi_chronicle_lore";

        private static CanvasGroup _flash_group;
        private static Image _flash_image;
        private static Coroutine _flash_routine;
        private static Text _lore_text;

        public static void Init()
        {
            AssetManager.world_log_library.add(new WorldLogAsset
            {
                id = LogAssetId,
                group = "disasters",
                path_icon = "gi/icons/chronicle",
                color = Toolbox.makeColor("#FFE58A"),
                locale_id = "gi_worldlog_chronicle",
                text_replacer = (WorldLogMessage pMessage, ref string pText) => pText = pMessage.special1
            });
        }

        // ---------------------------------------------------------------- public entry points

        public static void Announce(ChronicleEvent pEvent, WorldTile pFocus)
        {
            string banner = Format(pEvent.Banner ?? pEvent.Title);
            string era = ChronicleData.EraTitle(pEvent.Era);

            // Major events get a full cutscene (letterbox, slow motion, title card) instead of the banner.
            bool cutscene = pEvent.Major && Settings.ChronicleCutscenes && Director.CanPlayCutscene(true);
            if (cutscene)
            {
                Vector3? focus = Settings.CameraJump && pFocus != null ? pFocus.posV3 : (Vector3?)null;
                ModClass.Instance.StartCoroutine(Director.Cutscene(banner, $"{era} · {pEvent.When}", focus,
                    Toolbox.makeColor(pEvent.Color), 2.2f, true));
            }
            else if (Settings.Banner)
            {
                WorldTip.showNow($"{banner}\n{era} · {pEvent.When}", false, "center", Settings.BannerSeconds, pEvent.Color);
            }
            if (Settings.ScreenFlash) Flash(pEvent.Flash, pEvent.Major ? 0.85f : 0.45f, pEvent.Major ? 1.6f : 0.8f);
            if (Settings.ScreenShake && pEvent.Shake > 0f)
            {
                World.world.startShake(0.3f + pEvent.Shake * 1.2f, 0.01f, 1f + pEvent.Shake * 4f, true, true);
            }
            if (Settings.Sound) PlaySound(pEvent.Sound);
            if (Settings.WorldLog) Log($"{era}: {banner}", pFocus);
            if (!cutscene && Settings.CameraJump && pFocus != null) World.world.locatePosition(pFocus.posV3);
            if (Settings.LorePopup) ShowLore(pEvent);
        }

        public static void AnnounceStart(ChronicleEvent pFirst)
        {
            string era = ChronicleData.EraTitle(pFirst.Era);
            WorldTip.showNow($"★ The Teyvat Chronicle begins\n{era}", false, "center", Settings.BannerSeconds, "#FFE58A");
            if (Settings.ScreenFlash) Flash("gold", 0.6f, 1.2f);
            if (Settings.Sound) PlaySound("divine");
            if (Settings.WorldLog) Log($"The Teyvat Chronicle begins: {era}", null);
        }

        public static void AnnounceCountdown(ChronicleEvent pEvent, int pYearsLeft)
        {
            string years = pYearsLeft == 1 ? "1 year" : $"{pYearsLeft} years";
            WorldTip.showNowTop($"⏳ {Format(pEvent.Title)} draws near… ({years})", false);
            if (Settings.Sound) PlaySound("warning");
        }

        public static void AnnounceSimple(string pText, string pColor)
        {
            WorldTip.showNow(pText, false, "center", Settings.BannerSeconds, pColor);
            if (Settings.WorldLog) Log(pText, null);
        }

        private static float _last_legend = -999f;

        /// <summary>
        /// Announces a unit's rise (awakening, Exaltation, a new Archon). Major ones can play a cutscene;
        /// minor ones show a top banner, at most one every 15 seconds, and always go to the world log.
        /// </summary>
        public static void AnnounceLegend(string pText, Color pColor, WorldTile pFocus, bool pMajor)
        {
            if (!Settings.LegendAnnouncements) return;
            if (Settings.WorldLog) Log(pText, pFocus);
            if (pMajor && pFocus != null && Director.CanPlayCutscene())
            {
                ModClass.Instance.StartCoroutine(Director.Cutscene(pText, "A legend rises", pFocus.posV3, pColor, 1.4f));
                return;
            }
            if (Time.unscaledTime - _last_legend < 15f && !pMajor) return;
            _last_legend = Time.unscaledTime;
            WorldTip.showNowTop(pText, false);
            if (Settings.Sound) PlaySound(pMajor ? "divine" : "hope");
        }

        /// <summary>Replaces {tag} placeholders with the names of tagged kingdoms.</summary>
        public static string Format(string pText)
        {
            if (string.IsNullOrEmpty(pText)) return pText;
            return pText.Replace("{khaenriah}", ChronicleState.TaggedKingdomName("khaenriah", "Khaenri'ah"));
        }

        // ---------------------------------------------------------------- channels

        private static void Log(string pText, WorldTile pFocus)
        {
            WorldLogAsset asset = AssetManager.world_log_library.get(LogAssetId);
            if (asset == null) return;
            var message = new WorldLogMessage(asset, pText);
            if (pFocus != null) message.location = pFocus.posV3;
            message.add();
        }

        private static void PlaySound(string pSound)
        {
            if (string.IsNullOrEmpty(pSound)) return;
            // NML patches MusicBox.playSound to play .wav files registered from GameResources.
            MusicBox.playSound("gi/sfx/stinger_" + pSound, 0f, 0f);
        }

        private static Color FlashColor(string pFlash) => pFlash switch
        {
            "white" => Color.white,
            "gold" => Toolbox.makeColor("#FFD86B"),
            "red" => Toolbox.makeColor("#FF2020"),
            "blue" => Toolbox.makeColor("#3F8CFF"),
            "violet" => Toolbox.makeColor("#A050FF"),
            "cyan" => Toolbox.makeColor("#6FF0FF"),
            "green" => Toolbox.makeColor("#6CFF6A"),
            "black" => Color.black,
            _ => Color.clear
        };

        private static void Flash(string pFlash, float pPeak, float pSeconds)
        {
            Color color = FlashColor(pFlash);
            if (color.a <= 0f) return;
            EnsureFlashOverlay();
            if (_flash_routine != null) ModClass.Instance.StopCoroutine(_flash_routine);
            _flash_image.color = color;
            _flash_routine = ModClass.Instance.StartCoroutine(FadeFlash(pPeak, pSeconds));
        }

        private static IEnumerator FadeFlash(float pPeak, float pSeconds)
        {
            float time = 0f;
            while (time < pSeconds)
            {
                time += Time.unscaledDeltaTime;
                float t = time / pSeconds;
                // Snap in quickly, then ease out.
                _flash_group.alpha = pPeak * (t < 0.08f ? t / 0.08f : Mathf.Pow(1f - (t - 0.08f) / 0.92f, 2f));
                yield return null;
            }
            _flash_group.alpha = 0f;
            _flash_routine = null;
        }

        private static void EnsureFlashOverlay()
        {
            if (_flash_group != null) return;
            var root = new GameObject("gi_chronicle_flash", typeof(Canvas), typeof(CanvasGroup));
            Object.DontDestroyOnLoad(root);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30000;
            _flash_group = root.GetComponent<CanvasGroup>();
            _flash_group.alpha = 0f;
            _flash_group.blocksRaycasts = false;
            _flash_group.interactable = false;

            var image_object = new GameObject("Image", typeof(RectTransform), typeof(Image));
            image_object.transform.SetParent(root.transform, false);
            var rect = image_object.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _flash_image = image_object.GetComponent<Image>();
            _flash_image.raycastTarget = false;
        }

        // ---------------------------------------------------------------- lore pop-up

        private static void ShowLore(ChronicleEvent pEvent)
        {
            EnsureLoreWindow();
            _lore_text.text = $"<b>{Format(pEvent.Title)}</b>\n<i>{ChronicleData.EraTitle(pEvent.Era)} · {pEvent.When}</i>\n\n{Format(pEvent.Text)}";
            ScrollWindow.showWindow(LoreWindowId);
        }

        private static void EnsureLoreWindow()
        {
            if (_lore_text != null) return;
            ScrollWindow window = WindowCreator.CreateEmptyWindow(LoreWindowId, "gi_window_lore_title", "gi/icons/chronicle");
            var text_object = new GameObject("LoreText", typeof(RectTransform), typeof(Text));
            text_object.transform.SetParent(window.transform_content, false);
            var rect = text_object.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(190, 250);
            rect.localPosition = new Vector3(130, -130);
            _lore_text = text_object.GetComponent<Text>();
            OT.InitializeCommonText(_lore_text);
            _lore_text.alignment = TextAnchor.UpperLeft;
            _lore_text.supportRichText = true;
            _lore_text.fontSize = 8;
            _lore_text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _lore_text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}
