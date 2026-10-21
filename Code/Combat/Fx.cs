using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// A small pooled sprite-animation player for move VFX. Frames come from GameResources/gi/fx/&lt;set&gt;/
    /// and are drawn white so any element colour can tint them. Effects run on Cinematics time,
    /// so they freeze during hit-stop and slow down in slow motion.
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
        }

        private const int MaxActive = 360;
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
            }
        }

        public static void ClearAll()
        {
            for (int i = _active.Count - 1; i >= 0; i--) Release(_active[i], i);
        }

        private static Instance Get(string pLayer)
        {
            if (_root == null)
            {
                _root = new GameObject("gi_fx").transform;
                Object.DontDestroyOnLoad(_root.gameObject);
            }
            Instance fx = _pool.Count > 0 ? _pool.Pop() : null;
            if (fx == null || fx.Go == null)
            {
                fx = new Instance { Go = new GameObject("fx", typeof(SpriteRenderer)) };
                fx.Go.transform.SetParent(_root, false);
                fx.Renderer = fx.Go.GetComponent<SpriteRenderer>();
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
            if (pFx.Go == null) return;
            pFx.Go.SetActive(false);
            _pool.Push(pFx);
        }
    }
}
