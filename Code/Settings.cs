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

        // Combat
        public static bool Moves => Bool("Combat", "Moves");
        public static float MoveDamage => Float("Combat", "MoveDamage", 1f);
        public static bool BalancedCombat => Bool("Combat", "BalancedCombat");
        public static float FightLength => Float("Combat", "FightLength", 1f);
        public static float CooldownScale => Float("Combat", "CooldownScale", 1f);
        public static bool Duels => Bool("Combat", "Duels");
        public static float DuelChance => Float("Combat", "DuelChance", 0.25f);
        public static int MaxDuels => Int("Combat", "MaxDuels", 2);
        public static bool Guard => Bool("Combat", "Guard");
        public static bool TeamArts => Bool("Combat", "TeamArts");
        public static bool Situations => Bool("Combat", "Situations");
        public static bool GodAbilities => Bool("Combat", "GodAbilities");
        public static float GodCooldown => Float("Combat", "GodCooldown", 40f);
        public static bool CombatSounds => Bool("Combat", "CombatSounds");
        public static bool FloatingText => Bool("Combat", "FloatingText");
        public static bool Auras => Bool("Combat", "Auras");
        public static bool DragonBodies => Bool("Combat", "DragonBodies");

        // Cinematics
        public static bool ImpactFrames => Bool("Cinematics", "ImpactFrames");
        public static float ImpactIntensity => Float("Cinematics", "ImpactIntensity", 1f);
        public static bool Cutscenes => Bool("Cinematics", "Cutscenes");
        public static float CutsceneCooldown => Float("Cinematics", "CutsceneCooldown", 45f);
        public static bool ChronicleCutscenes => Bool("Cinematics", "ChronicleCutscenes");
        public static bool Spotlight => Bool("Cinematics", "Spotlight");
        public static bool Ceremonies => Bool("Cinematics", "Ceremonies");
        public static bool Afterimages => Bool("Cinematics", "Afterimages");
        public static bool Craters => Bool("Cinematics", "Craters");

        // Resonance (levelling)
        public static bool Resonance => Bool("Resonance", "Resonance");
        public static float TrainingXp => Float("Resonance", "TrainingXp", 1f);
        public static float CombatXp => Float("Resonance", "CombatXp", 1f);
        public static bool Sparring => Bool("Resonance", "Sparring");
        public static bool VisionAwakening => Bool("Resonance", "VisionAwakening");
        public static float AwakeningRate => Float("Resonance", "AwakeningRate", 1f);
        public static bool Succession => Bool("Resonance", "Succession");
        public static bool Exaltation => Bool("Resonance", "Exaltation");
        public static bool Godhood => Bool("Resonance", "Godhood");
        public static bool LegendAnnouncements => Bool("Resonance", "LegendAnnouncements");

        // Interface
        public static bool CharacterPanel => Bool("Interface", "CharacterPanel");
        public static bool Hud => Bool("Interface", "Hud");
        public static float UiScale => Float("Interface", "UiScale", 1f);

        public static bool EraEnabled(string pEraId) => Bool("Eras", "Era_" + pEraId);
    }
}
