using System.Collections.Generic;
using TeyvatChronicle.Combat;
using UnityEngine;

namespace TeyvatChronicle.Powers
{
    /// <summary>
    /// Gives each Dragon Sovereign its own body: the game's dragon sprites, recoloured at runtime to the
    /// Sovereign's element. The recoloured frames are built once per element and shared by all dragons.
    /// </summary>
    public static class DragonBodies
    {
        private static readonly Dictionary<string, DragonAsset> _bodies = new Dictionary<string, DragonAsset>();
        private static readonly Dictionary<Texture2D, Texture2D> _readable = new Dictionary<Texture2D, Texture2D>();

        /// <summary>Applies the element body if the actor is a dragon and doesn't already wear it.</summary>
        public static void Apply(Actor pActor, string pElement)
        {
            if (!Settings.DragonBodies || pActor?.avatar == null) return;
            var dragon = pActor.avatar.GetComponent<Dragon>();
            if (dragon == null || dragon.dragonAsset == null) return;

            DragonAsset body = GetBody(pElement, dragon.dragonAsset);
            if (body == null || dragon.dragonAsset == body) return;
            dragon.dragonAsset = body;
            dragon.setFrames(dragon.state, true);
        }

        private static DragonAsset GetBody(string pElement, DragonAsset pCurrent)
        {
            if (_bodies.TryGetValue(pElement, out DragonAsset cached)) return cached;
            DragonAsset source = PrefabLibrary.instance.dragonAsset;
            if (source == null || source.list == null) return null;

            Color tint = Elements.Color(pElement);
            Color.RGBToHSV(tint, out float hue, out float saturation, out _);

            var body = ScriptableObject.CreateInstance<DragonAsset>();
            body.name = "gi_dragon_" + pElement;
            body.list = new DragonAssetContainer[source.list.Length];
            for (int i = 0; i < source.list.Length; i++)
            {
                DragonAssetContainer from = source.list[i];
                var frames = new Sprite[from.frames.Length];
                for (int f = 0; f < frames.Length; f++) frames[f] = Recolor(from.frames[f], hue, saturation);
                body.list[i] = new DragonAssetContainer
                {
                    name = from.name,
                    id = from.id,
                    states = from.states,
                    speed = from.speed,
                    frames = frames
                };
            }
            _bodies[pElement] = body;
            return body;
        }

        /// <summary>
        /// Moves every coloured pixel to the element's hue while keeping its brightness, so shading,
        /// outlines and eyes survive. Near-grey pixels (outlines, highlights) are left alone.
        /// </summary>
        private static Sprite Recolor(Sprite pSprite, float pHue, float pSaturation)
        {
            if (pSprite == null) return null;
            Texture2D atlas = Readable(pSprite.texture);
            Rect rect = pSprite.textureRect;
            int x = Mathf.RoundToInt(rect.x), y = Mathf.RoundToInt(rect.y);
            int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
            Color[] pixels = atlas.GetPixels(x, y, w, h);

            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                if (c.a <= 0.01f) continue;
                Color.RGBToHSV(c, out _, out float s, out float v);
                if (s < 0.18f) continue;
                Color shifted = Color.HSVToRGB(pHue, Mathf.Clamp01(s * 0.5f + pSaturation * 0.5f), v);
                shifted.a = c.a;
                pixels[i] = shifted;
            }

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixels(pixels);
            texture.Apply();
            Vector2 pivot = new Vector2(pSprite.pivot.x / rect.width, pSprite.pivot.y / rect.height);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, w, h), pivot, pSprite.pixelsPerUnit);
            sprite.name = pSprite.name;
            return sprite;
        }

        /// <summary>Game textures aren't CPU-readable, so copy them through a RenderTexture once.</summary>
        private static Texture2D Readable(Texture2D pTexture)
        {
            if (_readable.TryGetValue(pTexture, out Texture2D copy)) return copy;
            RenderTexture rt = RenderTexture.GetTemporary(pTexture.width, pTexture.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(pTexture, rt);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            copy = new Texture2D(pTexture.width, pTexture.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, pTexture.width, pTexture.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            _readable[pTexture] = copy;
            return copy;
        }
    }
}
