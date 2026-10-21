using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Powers;
using TeyvatChronicle.Progression;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// Every tier wears its own aura, so you can read a battlefield at a glance:
    /// T1 a faint element shimmer · T2 element ki flames · T3 a golden halo over flames ·
    /// T4 dragons trail element motes · T5 crimson flames and dark motes · T6 three orbiting moons ·
    /// T7 turning rays of light. Only units on screen get auras, in small batches each second.
    /// </summary>
    public static class AuraSystem
    {
        private const float Interval = 1.2f;
        private const int MaxPerPass = 70;
        private static float _next;
        private static int _cursor;

        public static void Update()
        {
            if (!Settings.Auras || World.world?.units == null || Config.worldLoading) return;
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + Interval;

            ActorVisibleDataArray visible = World.world.units.visible_units;
            if (visible == null || visible.count == 0) return;
            if (Camera.main != null && Camera.main.orthographicSize > 70f) return; // zoomed far out: skip
            int shown = 0;
            for (int n = 0; n < visible.count && shown < MaxPerPass; n++)
            {
                if (_cursor >= visible.count) _cursor = 0;
                Actor a = visible.array[_cursor++];
                if (a == null || !a.isAlive()) continue;
                MoveSet set = CombatCore.ResolveSet(a, out string trait);
                if (set == null) continue;
                if (trait.StartsWith("gi_sovereign_")) DragonBodies.Apply(a, set.Element);
                else if (trait == "gi_nibelung") DragonBodies.Apply(a, "lunar");
                Show(a, set);
                shown++;
            }
        }

        private static void Show(Actor a, MoveSet pSet)
        {
            Vector3 p = a.current_position;
            if (!Director.OnScreen(p)) return;
            Color element = Elements.Color(pSet.Element);
            float life = Interval + 0.2f;
            float size = Mathf.Max(1.4f, a.current_scale.x * 14f);
            int rank = Resonance.Rank(a);
            float strength = 0.35f + Mathf.Clamp01(rank / 90f) * 0.45f;

            switch (pSet.Tier)
            {
                case 1:
                    Aura(a, "aura", size * 1.1f, Fade(element, 0.3f + strength * 0.4f), life);
                    break;
                case 2:
                    Aura(a, "aura_flame", size * 1.4f, Fade(element, 0.55f), life);
                    break;
                case 3:
                    Aura(a, "aura_flame", size * 1.5f, Fade(element, 0.6f), life);
                    Aura(a, "halo", size * 1.3f, Fade(Tiers.Color(3), 0.95f), life, "EffectsTop", new Vector3(0f, size * 0.35f));
                    break;
                case 4:
                    Aura(a, "motes", size * 1.6f, Fade(element, 0.9f), life, "EffectsTop");
                    Aura(a, "aura_flame", size * 1.6f, Fade(element, 0.45f), life);
                    break;
                case 5:
                    Aura(a, "aura_flame", size * 1.7f, Fade(Tiers.Color(5), 0.7f), life);
                    Aura(a, "motes", size * 1.6f, Fade(element, 0.9f), life, "EffectsTop");
                    break;
                case 6:
                    Aura(a, "orbit", size * 2f, Fade(element, 0.95f), life, "EffectsTop");
                    Aura(a, "aura", size * 1.4f, Fade(element, 0.55f), life);
                    break;
                default:
                    Aura(a, "radiance", size * 2.4f, Fade(Tiers.Color(7), 0.8f), life);
                    Aura(a, "motes", size * 1.8f, Fade(Tiers.Color(7), 0.9f), life, "EffectsTop");
                    break;
            }
        }

        private static void Aura(Actor a, string pSet, float pSize, Color pColor, float pLife,
            string pLayer = "EffectsBack", Vector3 pOffset = default)
        {
            Fx.Instance fx = Fx.Play(pSet, (Vector3)a.current_position + pOffset, pSize, pColor, 0f, pLife, true, 10f, pLayer);
            if (pOffset == default) Fx.Attach(fx, a);
        }

        private static Color Fade(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
