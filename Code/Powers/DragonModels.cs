using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace TeyvatChronicle.Powers
{
    /// <summary>
    /// The Dragon Sovereigns' own bodies. WorldBox's dragon is only used as an invisible carrier for
    /// movement: its sprite is hidden every frame and one of eight hand-made models is drawn in its place
    /// (GameResources/gi/dragons/&lt;id&gt;/, made by tools/art/gen_dragons.py), with its own poses: fly,
    /// hover, perch, breath, roar, claw, hurt and death. Techniques ask for poses through <see cref="Pose"/>.
    /// </summary>
    public static class DragonModels
    {
        private class Model
        {
            public Dictionary<string, Sprite[]> Poses = new Dictionary<string, Sprite[]>();
            public float WidthTiles = 9f;
            public Vector2 Mouth = new Vector2(0.9f, 0.6f);
        }

        private class Body
        {
            public GameObject Go;
            public SpriteRenderer Renderer;
            public string ModelId;
            public string Pose = "fly";
            public float PoseUntil;
            public float Age;
            public bool Once;
            public float HurtUntil;
        }

        private static readonly Dictionary<string, Model> _models = new Dictionary<string, Model>();
        private static readonly Dictionary<long, Body> _bodies = new Dictionary<long, Body>();
        private static readonly string[] PoseNames = { "fly", "hover", "perch", "breath", "roar", "claw", "hurt", "death" };
        private static Transform _root;
        private static float _cleanup_at;

        /// <summary>The model id for a unit, or null if it doesn't wear one.</summary>
        public static string ModelFor(Actor pActor)
        {
            if (pActor == null || pActor.avatar == null || !Settings.DragonBodies) return null;
            if (pActor.hasTrait("gi_nibelung")) return "nibelung";
            foreach (string element in TeyvatTraits.Elements)
            {
                if (pActor.hasTrait("gi_sovereign_" + element)) return element;
            }
            return null;
        }

        public static bool Wears(Actor pActor) => pActor != null && _bodies.ContainsKey(pActor.getID());

        /// <summary>Plays a pose for a while (breath, roar, claw, hurt…), then returns to flying.</summary>
        public static void Pose(Actor pActor, string pPose, float pSeconds = 0.8f)
        {
            if (pActor == null || !_bodies.TryGetValue(pActor.getID(), out Body body)) return;
            if (body.Pose == "death") return;
            if (pPose == "hurt" && body.PoseUntil > Time.unscaledTime) return; // don't interrupt attacks
            body.Pose = pPose;
            body.PoseUntil = Time.unscaledTime + pSeconds;
            body.Age = 0f;
            body.Once = true;
        }

        /// <summary>Where the mouth is in the world (breaths come from here).</summary>
        public static Vector3 Mouth(Actor pActor)
        {
            if (pActor == null || !_bodies.TryGetValue(pActor.getID(), out Body body) || body.Go == null)
                return pActor != null ? (Vector3)pActor.current_position : Vector3.zero;
            Model model = GetModel(body.ModelId);
            Bounds b = body.Renderer.bounds;
            float x = Mathf.Lerp(b.min.x, b.max.x, body.Renderer.flipX ? 1f - model.Mouth.x : model.Mouth.x);
            float y = Mathf.Lerp(b.min.y, b.max.y, model.Mouth.y);
            return new Vector3(x, y, 0f);
        }

        private static Model GetModel(string pId)
        {
            if (_models.TryGetValue(pId, out Model model)) return model;
            model = new Model();
            Sprite[] all = Resources.LoadAll<Sprite>("gi/dragons/" + pId);
            foreach (string pose in PoseNames)
            {
                Sprite[] frames = all.Where(s => s.name.StartsWith(pose + "_")).OrderBy(s => s.name).ToArray();
                if (frames.Length > 0) model.Poses[pose] = frames;
            }
            if (!model.Poses.ContainsKey("fly") && all.Length > 0) model.Poses["fly"] = all.OrderBy(s => s.name).ToArray();
            ReadMeta(pId, model);
            _models[pId] = model;
            if (all.Length == 0) ModClass.LogWarning($"[Dragons] No model frames for '{pId}'");
            return model;
        }

        private static void ReadMeta(string pId, Model pModel)
        {
            try
            {
                string path = Path.Combine(ModClass.Instance.GetDeclaration().FolderPath, "data", "dragons", pId + ".json");
                if (!File.Exists(path)) return;
                JObject meta = JObject.Parse(File.ReadAllText(path));
                if (meta["width_tiles"] != null) pModel.WidthTiles = (float)meta["width_tiles"];
                if (meta["mouth"] is JArray mouth && mouth.Count == 2 && meta["canvas"] is JArray canvas && canvas.Count == 2)
                {
                    // Pixels from the top-left → 0..1 from the bottom-left.
                    pModel.Mouth = new Vector2((float)mouth[0] / (float)canvas[0], 1f - (float)mouth[1] / (float)canvas[1]);
                }
            }
            catch (System.Exception e)
            {
                ModClass.LogWarning($"[Dragons] Bad model data for '{pId}': {e.Message}");
            }
        }

        // ---------------------------------------------------------------- per frame

        public static void Update()
        {
            if (World.world?.units == null || Config.worldLoading)
            {
                if (_bodies.Count > 0) ClearAll();
                return;
            }
            // Pick up new wearers among visible units.
            ActorVisibleDataArray visible = World.world.units.visible_units;
            if (visible != null)
            {
                for (int i = 0; i < visible.count; i++)
                {
                    Actor a = visible.array[i];
                    if (a == null || a.avatar == null || _bodies.ContainsKey(a.getID())) continue;
                    string id = ModelFor(a);
                    if (id != null) Attach(a, id);
                }
            }

            var gone = new List<long>();
            float dt = Time.unscaledDeltaTime * (Config.paused ? 0f : 1f);
            foreach (KeyValuePair<long, Body> pair in _bodies)
            {
                Actor a = World.world.units.get(pair.Key);
                Body body = pair.Value;
                if (a == null || a.avatar == null || body.Go == null || ModelFor(a) == null)
                {
                    gone.Add(pair.Key);
                    continue;
                }
                Draw(a, body, dt);
            }
            foreach (long id in gone) Detach(id);

            if (Time.unscaledTime > _cleanup_at)
            {
                _cleanup_at = Time.unscaledTime + 10f;
                foreach (long id in _bodies.Keys.Where(k => World.world.units.get(k) == null).ToList()) Detach(id);
            }
        }

        private static void Attach(Actor a, string pId)
        {
            if (_root == null)
            {
                _root = new GameObject("gi_dragons").transform;
                Object.DontDestroyOnLoad(_root.gameObject);
            }
            var go = new GameObject("gi_dragon_" + pId, typeof(SpriteRenderer));
            go.transform.SetParent(_root, false);
            var body = new Body { Go = go, Renderer = go.GetComponent<SpriteRenderer>(), ModelId = pId };
            _bodies[a.getID()] = body;
        }

        private static void Detach(long pId)
        {
            if (_bodies.TryGetValue(pId, out Body body) && body.Go != null) Object.Destroy(body.Go);
            _bodies.Remove(pId);
            Actor a = World.world?.units?.get(pId);
            if (a != null && a.avatar != null) SetVanillaVisible(a, true);
        }

        public static void ClearAll()
        {
            foreach (long id in _bodies.Keys.ToList()) Detach(id);
        }

        private static void SetVanillaVisible(Actor a, bool pVisible)
        {
            // The game re-applies the dragon's sprite every frame, so hide it three ways: renderer off,
            // rendering forced off, and fully transparent. This also runs in LateUpdate, after the game.
            foreach (SpriteRenderer r in a.avatar.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (r.enabled != pVisible) r.enabled = pVisible;
                if (r.forceRenderingOff == pVisible) r.forceRenderingOff = !pVisible;
                Color c = r.color;
                float alpha = pVisible ? 1f : 0f;
                if (!Mathf.Approximately(c.a, alpha)) r.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        /// <summary>Runs after every other update so nothing can show the WorldBox dragon again this frame.</summary>
        public static void LateUpdate()
        {
            if (World.world?.units == null || _bodies.Count == 0) return;
            foreach (KeyValuePair<long, Body> pair in _bodies)
            {
                Actor a = World.world.units.get(pair.Key);
                if (a == null || a.avatar == null || pair.Value.Renderer == null || !pair.Value.Renderer.enabled) continue;
                SetVanillaVisible(a, false);
                SpriteRenderer vanilla = a.avatar.GetComponentInChildren<SpriteRenderer>(true);
                if (vanilla != null) pair.Value.Go.transform.position = vanilla.transform.position;
            }
        }

        private static void Draw(Actor a, Body body, float dt)
        {
            Model model = GetModel(body.ModelId);
            SpriteRenderer vanilla = a.avatar.GetComponentInChildren<SpriteRenderer>(true);
            string pose = ChoosePose(a, body);
            if (!model.Poses.TryGetValue(pose, out Sprite[] frames) && !model.Poses.TryGetValue("fly", out frames))
            {
                // No model art: show the ordinary dragon rather than nothing.
                body.Renderer.enabled = false;
                SetVanillaVisible(a, true);
                return;
            }
            SetVanillaVisible(a, false);
            body.Age += dt;
            float fps = pose == "fly" ? 10f : pose == "hover" || pose == "perch" ? 6f : 12f;
            int index = (int)(body.Age * fps);
            index = body.Once || pose == "death" ? Mathf.Min(index, frames.Length - 1) : index % frames.Length;

            body.Renderer.enabled = a.is_visible;
            body.Renderer.sprite = frames[index];
            body.Renderer.flipX = a.flip;
            if (vanilla != null)
            {
                body.Renderer.sortingLayerID = vanilla.sortingLayerID;
                body.Renderer.sortingOrder = vanilla.sortingOrder + 1;
            }
            // Flash on hurt, fade on death.
            Color c = Color.white;
            if (Time.unscaledTime < body.HurtUntil) c = new Color(1f, 0.6f, 0.6f);
            if (!a.isAlive()) c.a = Mathf.Clamp01(1f - (body.Age - 0.6f));
            body.Renderer.color = c;

            float width = Mathf.Max(1f, frames[0].bounds.size.x);
            float scale = model.WidthTiles / width;
            body.Go.transform.localScale = new Vector3(scale, scale, 1f);
            Vector3 at = vanilla != null ? vanilla.transform.position : (Vector3)a.current_position;
            body.Go.transform.position = new Vector3(at.x, at.y, at.z);
        }

        private static string ChoosePose(Actor a, Body body)
        {
            if (!a.isAlive())
            {
                if (body.Pose != "death")
                {
                    body.Pose = "death";
                    body.Age = 0f;
                }
                return "death";
            }
            if (body.Once && Time.unscaledTime < body.PoseUntil) return body.Pose;
            if (body.Once)
            {
                body.Once = false;
                body.Age = 0f;
            }
            Dragon dragon = a.avatar.GetComponent<Dragon>();
            DragonState state = dragon != null ? dragon.state : DragonState.Fly;
            switch (state)
            {
                case DragonState.Idle:
                case DragonState.SleepStart:
                case DragonState.SleepLoop:
                case DragonState.SleepUp:
                case DragonState.LandAttack:
                    body.Pose = "perch";
                    break;
                default:
                    body.Pose = a.is_moving ? "fly" : "hover";
                    break;
            }
            return body.Pose;
        }

        /// <summary>Flashes the body and plays a recoil (called when it takes a technique).</summary>
        public static void Hurt(Actor pActor)
        {
            if (pActor == null || !_bodies.TryGetValue(pActor.getID(), out Body body)) return;
            body.HurtUntil = Time.unscaledTime + 0.12f;
            Pose(pActor, "hurt", 0.25f);
        }
    }
}
