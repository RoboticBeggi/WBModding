using TeyvatChronicle.Cinematics;
using TeyvatChronicle.Progression;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// Every tier wears its own aura, so you can read a battlefield at a glance. Auras are built from soft
    /// additive glows and particles sized to the unit (1.2–3 tiles), never from big sprites:
    /// T1 a faint shimmer and a few rising motes · T2 embers of ki rising off the body ·
    /// T3 a golden halo over gold and element embers · T4 element motes circling ·
    /// T5 crimson embers and dark smoke · T6 three small moons in orbit · T7 a radiant crown of light.
    /// Units drawn through their own avatar (dragons) get no aura: they have their own models.
    /// Only units on screen get auras, in small batches each second.
    /// </summary>
    public static class AuraSystem
    {
        private const float Interval = 1.2f;
        private const int MaxPerPass = 60;

        /// <summary>Auras stop spawning particles once this many are alive, leaving room for techniques.</summary>
        private const int ParticleBudget = 380;

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
                if (a == null || !a.isAlive() || a.avatar != null) continue;
                MoveSet set = CombatCore.ResolveSet(a, out _);
                if (set == null) continue;
                Show(a, set);
                shown++;
            }
        }

        /// <summary>A sane aura size for a unit: about its own body, clamped to 1.2–3 tiles.</summary>
        public static float SizeOf(Actor a) => Mathf.Clamp(a.current_scale.x * 12f, 1.2f, 3f);

        private static void Show(Actor a, MoveSet pSet)
        {
            Vector3 p = a.current_position;
            if (!Director.OnScreen(p)) return;
            Color element = Elements.Color(CombatCore.ElementFor(a, pSet));
            float life = Interval + 0.25f;
            float size = SizeOf(a);
            bool room = Fx.ParticleCount < ParticleBudget;
            Color clear = Fade(element, 0f);

            switch (pSet.Tier)
            {
                case 1:
                    Fx.Glow(p, size * 1.1f, Fade(element, 0.28f), life, a);
                    if (room) Rise(a, "mote", 2, element, size, 0.18f);
                    break;
                case 2:
                    Fx.Glow(p, size * 1.3f, Fade(element, 0.4f), life, a);
                    if (room) Rise(a, "mote", 5, element, size, 0.22f);
                    break;
                case 3:
                    Color gold = Tiers.Color(3);
                    Fx.Glow(p, size * 1.4f, Fade(element, 0.42f), life, a);
                    Halo(a, size, Fade(gold, 0.95f), life);
                    if (room)
                    {
                        Rise(a, "mote", 4, element, size, 0.22f);
                        Rise(a, "mote", 3, gold, size, 0.18f);
                    }
                    break;
                case 4:
                    Fx.Glow(p, size * 1.5f, Fade(element, 0.45f), life, a);
                    if (room) Fx.Orbit("mote", a, size * 0.55f, 5, 1.6f, life, element, 0.24f);
                    break;
                case 5:
                    Color crimson = Tiers.Color(5);
                    Fx.Glow(p, size * 1.5f, Fade(crimson, 0.45f), life, a);
                    if (room)
                    {
                        Rise(a, "mote", 5, crimson, size, 0.24f);
                        Fx.Particles("smoke", p, 2, 0.2f, 0.6f, life, new Color(0.12f, 0.08f, 0.14f, 0.55f),
                            new Color(0.05f, 0.03f, 0.06f, 0f), -0.6f, size * 0.35f, 360f, 0f, false, 1f);
                    }
                    break;
                case 6:
                    Fx.Glow(p, size * 1.5f, Fade(element, 0.5f), life, a);
                    if (room) Fx.Orbit("mote", a, size * 0.65f, 3, 2.4f, life, Color.Lerp(element, Color.white, 0.5f), 0.34f);
                    break;
                default:
                    Color light = Tiers.Color(7);
                    Fx.Glow(p, size * 1.9f, Fade(light, 0.55f), life, a);
                    Halo(a, size * 1.2f, Fade(light, 1f), life);
                    if (room)
                    {
                        Fx.Particles("streak", p, 6, 1.5f, 2.5f, life * 0.6f, Fade(light, 0.9f), Fade(light, 0f), 0f,
                            size * 0.35f, 360f, 0f, true, 1f);
                        Rise(a, "mote", 4, light, size, 0.2f);
                    }
                    break;
            }
        }

        /// <summary>Particles that drift up off the body like embers of ki.</summary>
        private static void Rise(Actor a, string pSet, int pCount, Color pColor, float pSize, float pParticleSize)
        {
            for (int i = 0; i < pCount; i++)
            {
                Vector3 at = (Vector3)a.current_position + new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(-0.2f, 0.3f)) * pSize;
                Fx.Particles(pSet, at, 1, 0.5f, 1.4f, Random.Range(0.6f, 1.2f), Color.Lerp(pColor, Color.white, 0.35f),
                    Fade(pColor, 0f), -0.8f, pParticleSize, 40f, 90f, true, 0.8f);
            }
        }

        private static void Halo(Actor a, float pSize, Color pColor, float pLife)
        {
            Vector3 p = (Vector3)a.current_position + new Vector3(0f, pSize * 0.4f);
            Fx.Instance fx = Fx.Play("halo", p, pSize * 0.55f, pColor, 0f, pLife, true, 10f, "EffectsTop");
            Fx.SetAdditive(fx, true);
        }

        private static Color Fade(Color c, float a) => new Color(c.r, c.g, c.b, a);
    }
}
