using HarmonyLib;
using NeoModLoader.api;
using TeyvatChronicle.Arts;
using TeyvatChronicle.Chronicle;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Powers;
using TeyvatChronicle.Progression;
using TeyvatChronicle.UI;
using UnityEngine;

namespace TeyvatChronicle
{
    public class ModClass : BasicMod<ModClass>
    {
        private const float TickInterval = 0.25f;
        private float _tick_timer;
        private bool _train_tick;

        protected override void OnModLoad()
        {
            ChronicleData.Load(GetDeclaration().FolderPath);
            LogInfo($"Loaded {ChronicleData.Timeline.Events.Count} chronicle events from {ChronicleData.LoadedFrom}");

            Kit.Init(GetDeclaration().FolderPath);
            TeyvatTraits.Init();
            Engine.Init();
            TeyvatPowers.Init();
            Announcer.Init();
            TeyvatTab.Init();

            var harmony = new Harmony("RoboticBeggi.TeyvatChronicle");
            CharacterPanel.Patch(harmony);
            Guard.Patch(harmony);
            DragonRule.Patch(harmony);
            Path.Patch(harmony);
        }

        private void Update()
        {
            MainThread.Drain();
            Fx.Update(Time.unscaledDeltaTime * Director.FxTimeScale * (Config.paused ? 0f : 1f));
            Kit.SyncScale();
            AuraSystem.Update();
            GodArts.Update();
            DragonModels.Update();
            DragonRule.Tick();
            CharacterPanel.Update();
            ArchiveWindow.Update();
            Hud.Update();

            _tick_timer -= Time.unscaledDeltaTime;
            if (_tick_timer > 0f) return;
            _tick_timer = TickInterval;
            ChronicleState.Tick();
            if (World.world != null) CombatCore.Cleanup();
            // Training runs every other tick (twice a second)
            _train_tick = !_train_tick;
            if (_train_tick) Training.Tick();
        }
    }
}
