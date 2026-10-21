using NeoModLoader.api;
using TeyvatChronicle.Chronicle;
using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Combat;
using TeyvatChronicle.Powers;
using TeyvatChronicle.UI;
using UnityEngine;

namespace TeyvatChronicle
{
    public class ModClass : BasicMod<ModClass>
    {
        private const float TickInterval = 0.25f;
        private float _tick_timer;

        protected override void OnModLoad()
        {
            ChronicleData.Load(GetDeclaration().FolderPath);
            LogInfo($"Loaded {ChronicleData.Timeline.Events.Count} chronicle events from {ChronicleData.LoadedFrom}");

            MoveData.Load(GetDeclaration().FolderPath);
            LogInfo($"Loaded {MoveData.Data.Moves.Count} moves in {MoveData.Data.MoveSets.Count} movesets");

            TeyvatTraits.Init();
            Duel.Init();
            Announcer.Init();
            TeyvatTab.Init();
        }

        private void Update()
        {
            MainThread.Drain();
            Fx.Update(Time.unscaledDeltaTime * Director.FxTimeScale * (Config.paused ? 0f : 1f));

            _tick_timer -= Time.unscaledDeltaTime;
            if (_tick_timer > 0f) return;
            _tick_timer = TickInterval;
            ChronicleState.Tick();
            if (World.world != null) CombatCore.Cleanup();
        }
    }
}
