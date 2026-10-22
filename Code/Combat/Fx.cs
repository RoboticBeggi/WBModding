using System.Collections.Generic;
using System.Linq;
using TeyvatChronicle.Cinematics;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// A small pooled sprite-animation player for technique VFX, plus a pooled particle system and soft
    /// additive glows. Frames come from GameResources/gi/arts/&lt;set&gt;/ (or the older gi/fx/). Everything
    /// runs on Cinematics time, so it freezes during hit-stop and slows down in slow motion.
    ///
    /// Layering, as in anime and fighting-game effects: a core sprite, an additive glow under it,
    /// particles that fly, linger and fall, and trails behind anything that moves.
    /// </summary>
    public static class Fx
    {
        public class Instance
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public Sprite[] Frames;
            public float FrameTime;
            public float Age;
            public float Life;
            public bool Loop;
            public Color Color;
            public float BaseScale;
            public float GrowTo = 1f;
            public Vector3 From, To;
            public float MoveTime;
            public BaseSimObject Follow;
            public bool Active;

            /// <summary>Drawn with additive blending (light adds up instead of covering).</summary>
            public bool Additive;

            /// <summary>Fraction of life spent fading in (0 = pops in).</summary>
            public float FadeIn;

            // Trail: particles emitted behind the effect while it moves.
            public string TrailSet;
            public Color TrailFrom, TrailTo;
            public float TrailRate, TrailSize, TrailAcc;
            public Vector3 LastPos;
        }

        /// <summary>One particle: a sprite with velocity, drag, gravity, spin, size- and colour-over-life.</summary>
        public class Particle
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public Vector3 Pos, Vel;
            public float Drag, Gravity, Age, Life, Size0, Size1, Rot, Spin, Unit;
            public bool Align;
            public Color From, To;
            public BaseSimObject Follow;
            public float OrbitRadius, OrbitAngle, OrbitSpeed;
            public bool Additive;
        }

        private const int MaxActive = 520;
        public const int MaxParticles = 600;
        private static readonly List<Particle> _particles = new List<Particle>();
        private static readonly Stack<Particle> _particle_pool = new Stack<Particle>();
        private static Material _default_material, _additive_material;
        private static bool _additive_checked;
        private static readonly Dictionary<string, Sprite[]> _sets = new Dictionary<string, Sprite[]>();
        private static readonly List<Instance> _active = new List<Instance>();
        private static readonly Stack<Instance> _pool = new Stack<Instance>();
        private static Transform _root;

        public static Sprite[] Frames(string pSet)
        {
            if (string.IsNullOrEmpty(pSet)) return null;
            if (_sets.TryGetValue(pSet, out Sprite[] frames)) return frames;
            // "arts/<set>" names the bespoke technique art; plain names the older shared shapes.
            string path = pSet.Contains("/") ? "gi/" + pSet : "gi/fx/" + pSet;
            frames = Resources.LoadAll<Sprite>(path).OrderBy(s => s.name).ToArray();
            _sets[pSet] = frames;
            if (frames.Length == 0) ModClass.LogWarning($"[Fx] No frames for set '{pSet}'");
            return frames;
        }

        /// <summary>Plays an effect. Size is in tiles; life ≤ 0 plays the animation once.</summary>
        public static Instance Play(string pSet, Vector3 pPos, float pSize, Color pColor, float pRotation = 0f,
            float pLife = 0f, bool pLoop = false, float pFps = 18f, string pLayer = "EffectsTop")
        {
            Sprite[] frames = Frames(pSet);
            if (frames == null || frames.Length == 0 || _active.Count >= MaxActive) return null;

            Instance fx = Get(pLayer);
            fx.Frames = frames;
            fx.FrameTime = 1f / pFps;
            fx.Age = 0f;
            fx.Loop = pLoop;
            fx.Life = pLife > 0f ? pLife : frames.Length * fx.FrameTime;
            fx.Color = pColor;
            fx.GrowTo = 1f;
            fx.MoveTime = 0f;
            fx.Follow = null;
            fx.From = fx.To = pPos;
            fx.FadeIn = 0f;
            fx.TrailSet = null;
            fx.LastPos = pPos;
            SetAdditive(fx, false);

            float width = Mathf.Max(1f, frames[0].bounds.size.x);
            fx.BaseScale = pSize / width;
            fx.Go.transform.position = pPos;
            fx.Go.transform.rotation = Quaternion.Euler(0f, 0f, pRotation);
            fx.Go.transform.localScale = Vector3.one * fx.BaseScale;
            fx.Renderer.sprite = frames[0];
            fx.Renderer.flipX = false;
            // Sprites drawn facing right stay upright when they're turned to face left.
            float turn = Mathf.Repeat(pRotation, 360f);
            fx.Renderer.flipY = turn > 90f && turn < 270f;
            fx.Renderer.color = pColor;
            fx.Go.SetActive(true);
            fx.Active = true;
            _active.Add(fx);
            return fx;
        }

        /// <summary>Moves an effect in a straight line over time (projectiles, falling meteors).</summary>
        public static void MoveTo(Instance pFx, Vector3 pTo, float pTime, bool pRotate = true)
        {
            if (pFx == null) return;
            pFx.From = pFx.Go.transform.position;
            pFx.To = pTo;
            pFx.MoveTime = Mathf.Max(0.01f, pTime);
            if (!pRotate) return;
            Vector3 dir = pTo - pFx.From;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            pFx.Go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            float turn = Mathf.Repeat(angle, 360f);
            pFx.Renderer.flipY = turn > 90f && turn < 270f;
        }

        /// <summary>Stretches an effect between two points (beams, dash streaks).</summary>
        public static Instance Line(string pSet, Vector3 pFrom, Vector3 pTo, float pThickness, Color pColor,
            float pLife, bool pLoop = true)
        {
            Vector3 dir = pTo - pFrom;
            Instance fx = Play(pSet, (pFrom + pTo) * 0.5f, 1f, pColor, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg,
                pLife, pLoop);
            if (fx == null) return null;
            Vector2 size = fx.Frames[0].bounds.size;
            fx.Go.transform.localScale = new Vector3(dir.magnitude / Mathf.Max(1f, size.x),
                pThickness / Mathf.Max(1f, size.y), 1f);
            fx.BaseScale = 0f; // keep the stretched scale
            return fx;
        }

        /// <summary>Keeps an effect glued to a unit (auras, shields).</summary>
        public static void Attach(Instance pFx, BaseSimObject pTarget)
        {
            if (pFx != null) pFx.Follow = pTarget;
        }

        /// <summary>
        /// An afterimage: a fading, tinted copy of the unit's current sprite, left behind by dashes,
        /// teleports, dodges and knockbacks.
        /// </summary>
        public static Instance Ghost(Actor pActor, Color pColor, float pLife = 0.35f)
        {
            if (pActor == null || !pActor.isAlive() || _active.Count >= MaxActive) return null;
            // Units with their own avatar (dragons, UFOs…) draw through a SpriteRenderer; everyone else
            // through the batch renderer's coloured sprite. Anything bigger than a unit frame is not a
            // unit sprite (it would be a whole texture sheet), so it's skipped.
            Sprite sprite = null;
            if (pActor.avatar != null)
            {
                var renderer = pActor.avatar.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null) sprite = renderer.sprite;
            }
            if (sprite == null) sprite = pActor.getLastColoredSprite();
            if (sprite == null || sprite.rect.width > 96f || sprite.rect.height > 96f) return null;
            Instance fx = Get("EffectsBack");
            fx.Frames = new[] { sprite };
            fx.FrameTime = pLife;
            fx.Age = 0f;
            fx.Loop = false;
            fx.Life = pLife;
            fx.Color = new Color(pColor.r, pColor.g, pColor.b, 0.65f);
            fx.GrowTo = 1f;
            fx.MoveTime = 0f;
            fx.Follow = null;
            fx.FadeIn = 0f;
            fx.TrailSet = null;
            SetAdditive(fx, false);
            fx.BaseScale = 0f;
            fx.Go.transform.position = pActor.current_position;
            fx.Go.transform.rotation = Quaternion.identity;
            fx.Go.transform.localScale = pActor.avatar != null ? pActor.avatar.transform.lossyScale : pActor.current_scale;
            fx.Renderer.sprite = sprite;
            fx.Renderer.flipX = pActor.flip;
            fx.Renderer.flipY = false;
            fx.Renderer.color = fx.Color;
            fx.Go.SetActive(true);
            fx.Active = true;
            _active.Add(fx);
            return fx;
        }

        public static void Grow(Instance pFx, float pFactor)
        {
            if (pFx != null) pFx.GrowTo = pFactor;
        }

        public static void Update(float pDelta)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                Instance fx = _active[i];
                fx.Age += pDelta;
                if (fx.Age >= fx.Life || fx.Go == null)
                {
                    Release(fx, i);
                    continue;
                }

                int frame = (int)(fx.Age / fx.FrameTime);
                frame = fx.Loop ? frame % fx.Frames.Length : Mathf.Min(frame, fx.Frames.Length - 1);
                fx.Renderer.sprite = fx.Frames[frame];

                float t = fx.Age / fx.Life;
                Color c = fx.Color;
                c.a *= t > 0.75f ? (1f - t) / 0.25f : 1f;
                if (fx.FadeIn > 0f && t < fx.FadeIn) c.a *= t / fx.FadeIn;
                fx.Renderer.color = c;

                if (fx.BaseScale > 0f && fx.GrowTo != 1f)
                {
                    fx.Go.transform.localScale = Vector3.one * fx.BaseScale * Mathf.Lerp(1f, fx.GrowTo, t);
                }
                if (fx.Follow != null && fx.Follow.isAlive())
                {
                    fx.Go.transform.position = fx.Follow.current_position;
                }
                else if (fx.MoveTime > 0f)
                {
                    fx.Go.transform.position = Vector3.Lerp(fx.From, fx.To, Mathf.Clamp01(fx.Age / fx.MoveTime));
                }
                if (fx.TrailSet != null) EmitTrail(fx, pDelta);
            }
            UpdateParticles(pDelta);
        }

        // ---------------------------------------------------------------- additive glow

        /// <summary>
        /// The additive material, if the game ships one of the particle shaders. When none exists the
        /// default sprite material is used, which still looks fine (just not as bright).
        /// </summary>
        private static Material AdditiveMaterial
        {
            get
            {
                if (_additive_checked) return _additive_material;
                _additive_checked = true;
                foreach (string name in new[]
                         {
                             "Legacy Shaders/Particles/Additive", "Particles/Additive", "Mobile/Particles/Additive",
                             "Particles/Standard Unlit"
                         })
                {
                    Shader shader = Shader.Find(name);
                    if (shader == null) continue;
                    _additive_material = new Material(shader);
                    if (_additive_material.HasProperty("_TintColor"))
                        _additive_material.SetColor("_TintColor", new Color(0.5f, 0.5f, 0.5f, 0.5f));
                    ModClass.LogInfo($"[Fx] Additive glow uses '{name}'");
                    break;
                }
                if (_additive_material == null) ModClass.LogInfo("[Fx] No additive shader; glows use normal blending");
                return _additive_material;
            }
        }

        public static void SetAdditive(Instance pFx, bool pOn)
        {
            if (pFx?.Renderer == null) return;
            Material add = pOn ? AdditiveMaterial : null;
            pFx.Additive = pOn && add != null;
            pFx.Renderer.sharedMaterial = pFx.Additive ? add : _default_material;
        }

        /// <summary>A soft additive glow (fades in and out). Size in tiles.</summary>
        public static Instance Glow(Vector3 pPos, float pSize, Color pColor, float pLife, BaseSimObject pFollow = null,
            string pLayer = "EffectsTop")
        {
            if (!Director.OnScreen(pPos)) return null;
            Instance fx = Play("arts/glow", pPos, pSize, pColor, 0f, Mathf.Max(0.05f, pLife), true, 1f, pLayer);
            if (fx == null) return null;
            SetAdditive(fx, true);
            fx.FadeIn = 0.2f;
            fx.Renderer.sortingOrder = 45;
            fx.Follow = pFollow;
            return fx;
        }

        /// <summary>Makes an effect leave a particle trail while it moves (projectiles, meteors).</summary>
        public static void Trail(Instance pFx, string pSet, Color pFrom, Color pTo, float pRate = 40f, float pSize = 0.35f)
        {
            if (pFx == null) return;
            pFx.TrailSet = pSet;
            pFx.TrailFrom = pFrom;
            pFx.TrailTo = pTo;
            pFx.TrailRate = pRate;
            pFx.TrailSize = pSize;
            pFx.TrailAcc = 0f;
            pFx.LastPos = pFx.Go.transform.position;
        }

        private static void EmitTrail(Instance fx, float pDelta)
        {
            Vector3 p = fx.Go.transform.position;
            Vector3 moved = p - fx.LastPos;
            fx.LastPos = p;
            if (moved.sqrMagnitude < 0.0004f) return;
            fx.TrailAcc += fx.TrailRate * pDelta;
            int count = (int)fx.TrailAcc;
            if (count <= 0) return;
            fx.TrailAcc -= count;
            Vector3 back = -moved.normalized;
            for (int k = 0; k < count; k++)
            {
                Vector3 at = p - moved * (k / (float)count);
                Vector2 jitter = Random.insideUnitCircle * 0.6f;
                Spawn(fx.TrailSet, at, (back * Random.Range(0.3f, 1.2f)) + (Vector3)jitter, Random.Range(0.25f, 0.5f),
                    fx.TrailFrom, fx.TrailTo, 0f, 2.5f, fx.TrailSize, fx.TrailSize * 0.2f, true, false);
            }
        }

        // ---------------------------------------------------------------- particles

        public static int ParticleCount => _particles.Count;

        /// <summary>
        /// A burst of particles. Speeds are in tiles per second, angle and spread in degrees (spread 360 = all
        /// round). Colour goes from <paramref name="pFrom"/> to <paramref name="pTo"/> over life while the size
        /// shrinks to a third; streak-like sets (spark, streak) turn to face their direction of travel.
        /// </summary>
        public static void Particles(string pSet, Vector3 pPos, int pCount, float pSpeedMin, float pSpeedMax, float pLife,
            Color pFrom, Color pTo, float pGravity = 0f, float pSize = 0.4f, float pSpread = 360f, float pAngle = 0f,
            bool pAdditive = true, float pDrag = 2f)
        {
            if (pCount <= 0 || !Director.OnScreen(pPos)) return;
            pCount = Mathf.Min(pCount, MaxParticles - _particles.Count);
            bool align = pSet == "spark" || pSet == "streak";
            for (int i = 0; i < pCount; i++)
            {
                float a = (pAngle + Random.Range(-pSpread / 2f, pSpread / 2f)) * Mathf.Deg2Rad;
                Vector3 vel = new Vector3(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(pSpeedMin, pSpeedMax);
                float life = pLife * Random.Range(0.7f, 1.3f);
                float size = pSize * Random.Range(0.7f, 1.3f);
                Spawn(pSet, pPos, vel, life, pFrom, pTo, pGravity, pDrag, size, size * 0.33f, pAdditive, align);
            }
        }

        /// <summary>Particles that circle a unit (auras, orbiting motes and moons).</summary>
        public static void Orbit(string pSet, BaseSimObject pFollow, float pRadius, int pCount, float pPeriod, float pLife,
            Color pColor, float pSize = 0.3f, bool pAdditive = true)
        {
            if (pFollow == null || !Director.OnScreen(pFollow.current_position)) return;
            pCount = Mathf.Min(pCount, MaxParticles - _particles.Count);
            float offset = Random.Range(0f, Mathf.PI * 2f);
            for (int i = 0; i < pCount; i++)
            {
                Particle p = Spawn(pSet, pFollow.current_position, Vector3.zero, pLife, pColor,
                    new Color(pColor.r, pColor.g, pColor.b, 0f), 0f, 0f, pSize, pSize, pAdditive, false);
                if (p == null) return;
                p.Follow = pFollow;
                p.OrbitRadius = pRadius;
                p.OrbitAngle = offset + i * Mathf.PI * 2f / pCount;
                p.OrbitSpeed = Mathf.PI * 2f / Mathf.Max(0.2f, pPeriod);
            }
        }

        private static Particle Spawn(string pSet, Vector3 pPos, Vector3 pVel, float pLife, Color pFrom, Color pTo,
            float pGravity, float pDrag, float pSize0, float pSize1, bool pAdditive, bool pAlign)
        {
            if (_particles.Count >= MaxParticles) return null;
            Sprite[] frames = Frames("arts/" + pSet);
            if (frames == null || frames.Length == 0) return null;
            Particle p = _particle_pool.Count > 0 ? _particle_pool.Pop() : null;
            if (p == null || p.Go == null)
            {
                EnsureRoot();
                p = new Particle { Go = new GameObject("particle", typeof(SpriteRenderer)) };
                p.Go.transform.SetParent(_root, false);
                p.Renderer = p.Go.GetComponent<SpriteRenderer>();
                if (_default_material == null) _default_material = p.Renderer.sharedMaterial;
                p.Renderer.sortingLayerName = "EffectsTop";
                p.Renderer.sortingOrder = 55;
            }
            Sprite sprite = frames[Random.Range(0, frames.Length)];
            p.Renderer.sprite = sprite;
            p.Unit = 1f / Mathf.Max(0.01f, sprite.bounds.size.x);
            p.Pos = pPos;
            p.Vel = pVel;
            p.Life = Mathf.Max(0.05f, pLife);
            p.Age = 0f;
            p.From = pFrom;
            p.To = pTo;
            p.Gravity = pGravity;
            p.Drag = pDrag;
            p.Size0 = pSize0;
            p.Size1 = pSize1;
            p.Align = pAlign;
            p.Rot = pAlign ? Mathf.Atan2(pVel.y, pVel.x) * Mathf.Rad2Deg : Random.Range(0f, 360f);
            p.Spin = pAlign ? 0f : Random.Range(-240f, 240f);
            p.Follow = null;
            p.OrbitRadius = 0f;
            Material add = pAdditive ? AdditiveMaterial : null;
            p.Additive = add != null;
            p.Renderer.sharedMaterial = p.Additive ? add : _default_material;
            p.Renderer.color = pFrom;
            p.Go.transform.position = pPos;
            p.Go.transform.localScale = Vector3.one * pSize0 * p.Unit;
            p.Go.transform.rotation = Quaternion.Euler(0f, 0f, p.Rot);
            p.Go.SetActive(true);
            _particles.Add(p);
            return p;
        }

        private static void UpdateParticles(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle p = _particles[i];
                p.Age += dt;
                if (p.Age >= p.Life || p.Go == null)
                {
                    ReleaseParticle(p, i);
                    continue;
                }
                float t = p.Age / p.Life;
                if (p.Follow != null && p.OrbitRadius > 0f)
                {
                    if (!p.Follow.isAlive())
                    {
                        ReleaseParticle(p, i);
                        continue;
                    }
                    p.OrbitAngle += p.OrbitSpeed * dt;
                    Vector3 c = p.Follow.current_position;
                    p.Pos = c + new Vector3(Mathf.Cos(p.OrbitAngle), Mathf.Sin(p.OrbitAngle) * 0.55f) * p.OrbitRadius;
                }
                else
                {
                    p.Vel *= Mathf.Max(0f, 1f - p.Drag * dt);
                    p.Vel.y -= p.Gravity * dt;
                    p.Pos += p.Vel * dt;
                    if (p.Align && p.Vel.sqrMagnitude > 0.01f) p.Rot = Mathf.Atan2(p.Vel.y, p.Vel.x) * Mathf.Rad2Deg;
                    else p.Rot += p.Spin * dt;
                }
                Color col = Color.Lerp(p.From, p.To, t);
                if (t > 0.7f) col.a *= (1f - t) / 0.3f;
                p.Renderer.color = col;
                Transform tr = p.Go.transform;
                tr.position = p.Pos;
                tr.rotation = Quaternion.Euler(0f, 0f, p.Rot);
                float size = Mathf.Lerp(p.Size0, p.Size1, t);
                tr.localScale = p.Align
                    ? new Vector3(size * p.Unit * (1f + Mathf.Min(2f, p.Vel.magnitude * 0.12f)), size * p.Unit, 1f)
                    : Vector3.one * size * p.Unit;
            }
        }

        private static void ReleaseParticle(Particle p, int pIndex)
        {
            _particles.RemoveAt(pIndex);
            p.Follow = null;
            if (p.Go == null) return;
            p.Go.SetActive(false);
            _particle_pool.Push(p);
        }

        public static void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Release(_active[i], i);
            for (int i = _particles.Count - 1; i >= 0; i--) ReleaseParticle(_particles[i], i);
        }

        private static void EnsureRoot()
        {
            if (_root != null) return;
            _root = new GameObject("gi_fx").transform;
            Object.DontDestroyOnLoad(_root.gameObject);
        }

        private static Instance Get(string pLayer)
        {
            EnsureRoot();
            Instance fx = _pool.Count > 0 ? _pool.Pop() : null;
            if (fx == null || fx.Go == null)
            {
                fx = new Instance { Go = new GameObject("fx", typeof(SpriteRenderer)) };
                fx.Go.transform.SetParent(_root, false);
                fx.Renderer = fx.Go.GetComponent<SpriteRenderer>();
                if (_default_material == null) _default_material = fx.Renderer.sharedMaterial;
            }
            fx.Renderer.sortingLayerName = pLayer;
            fx.Renderer.sortingOrder = 50;
            return fx;
        }

        private static void Release(Instance pFx, int pIndex)
        {
            _active.RemoveAt(pIndex);
            pFx.Active = false;
            pFx.Follow = null;
            pFx.TrailSet = null;
            if (pFx.Go == null) return;
            if (pFx.Additive) SetAdditive(pFx, false);
            pFx.Go.SetActive(false);
            _pool.Push(pFx);
        }
    }
}
