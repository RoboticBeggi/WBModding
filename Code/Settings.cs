using NeoModLoader.api;
using TeyvatChronicle.Chronicle;

namespace TeyvatChronicle
{
    /// <summary>
    /// Typed access to default_config.json. Values are read live, so changes in the
    /// in-game mod settings window apply immediately.
    /// </summary>
    public static class Settings
    {
        private static ModConfig Config => ModClass.Instance.GetConfig();

        private static ModConfigItem Item(string pGroup, string pId)
        {
            var config = Config;
            if (config == null) return null;
            try
            {
                return config[pGroup].TryGetValue(pId, out ModConfigItem item) ? item : null;
            }
            catch (System.Collections.Generic.KeyNotFoundException)
            {
                return null;
            }
        }

        private static bool Bool(string pGroup, string pId, bool pDefault = true) => Item(pGroup, pId)?.BoolVal ?? pDefault;
        private static int Int(string pGroup, string pId, int pDefault = 0) => Item(pGroup, pId)?.IntVal ?? pDefault;
        private static float Float(string pGroup, string pId, float pDefault = 1f) => Item(pGroup, pId)?.FloatVal ?? pDefault;

        // Chronicle
        public static Pacing Pacing => (Pacing)Int("Chronicle", "Pacing", (int)Pacing.Normal);
        public static int StartEra => Int("Chronicle", "StartEra");
        public static float CustomTimeScale => Float("Chronicle", "CustomTimeScale");
        public static bool CatchUp => Bool("Chronicle", "CatchUp");
        public static bool CountdownWarnings => Bool("Chronicle", "CountdownWarnings");
        public static bool MinorEvents => Bool("Chronicle", "MinorEvents");

        // Announcements
        public static bool Banner => Bool("Announcements", "Banner");
        public static float BannerSeconds => Float("Announcements", "BannerSeconds", 7f);
        public static bool ScreenFlash => Bool("Announcements", "ScreenFlash");
        public static bool ScreenShake => Bool("Announcements", "ScreenShake");
        public static bool Sound => Bool("Announcements", "Sound");
        public static bool CameraJump => Bool("Announcements", "CameraJump");
        public static bool LorePopup => Bool("Announcements", "LorePopup", false);
        public static bool WorldLog => Bool("Announcements", "WorldLog");

        // Rules
        public static bool UniqueHolders => Bool("Rules", "UniqueHolders");
        public static bool DestructiveEffects => Bool("Rules", "DestructiveEffects");
        public static bool WarEffects => Bool("Rules", "WarEffects");

        public static bool EraEnabled(string pEraId) => Bool("Eras", "Era_" + pEraId);
    }
}
