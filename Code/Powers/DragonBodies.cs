using System.Collections.Generic;
using UnityEngine;

namespace TeyvatChronicle.Powers
{
    /// <summary>
    /// Custom Dragon Sovereign designs. Every frame of every dragon animation (fly, land, attack, slide,
    /// sleep…) is redrawn at runtime: a four-tone element palette, an element pattern (wind streaks,
    /// rock plates, lightning veins, leaf mottling, scale shimmer, molten cracks, frost tips), pale parts
    /// recast as accents, and a glowing rim around the silhouette. Because only the pixels change, all of
    /// the game's dragon animations keep working.
    /// </summary>
    public static class DragonBodies
    {
        private class Design
        {
            public Color Shadow, Base, Light, High, Accent, Glow;
            public string Pattern;
        }

        private static readonly Dictionary<string, Design> Designs = new Dictionary<string, Design>
        {
            ["anemo"] = D("#1F6B5E", "#3FB89A", "#7FE8C8", "#D8FFF2", "#F4F8E8", "#9FFFE0", "wind"),
            ["geo"] = D("#5E3F16", "#A87A28", "#E2B652", "#FFE9A8", "#FFF3C8", "#FFD86B", "plates"),
            ["electro"] = D("#2E1652", "#5E35A6", "#9A68F2", "#E2CCFF", "#FFFFFF", "#D49CFF", "lightning"),
            ["dendro"] = D("#1E461A", "#45902A", "#8BCB46", "#D8F7A0", "#F7D6E8", "#B6FF7A", "leaves"),
            ["hydro"] = D("#103A72", "#2670C2", "#5CB0F0", "#C8ECFF", "#E8F8FF", "#7FD0FF", "scales"),
            ["pyro"] = D("#4E100C", "#9C2A16", "#EC6426", "#FFC060", "#FFE6A0", "#FF8A3A", "molten"),
            ["cryo"] = D("#33566F", "#6AA2C2", "#A6DAEE", "#F0FCFF", "#FFFFFF", "#CFF6FF", "frost"),
            ["lunar"] = D("#16112A", "#352760", "#6656AC", "#C9B8FF", "#FFE58A", "#E6E0FF", "stars"),
        };

        private static readonly Dictionary<string, DragonAsset> _bodies = new Dictionary<string, DragonAsset>();
        private static readonly Dictionary<Texture2D, Texture2D> _readable = new Dictionary<Texture2D, Texture2D>();

        private static Design D(string s, string b, string l, string h, string a, string g, string p) => new Design
        {
            Shadow = Toolbox.makeColor(s), Base = Toolbox.makeColor(b), Light = Toolbox.makeColor(l),
            High = Toolbox.makeColor(h), Accent = Toolbox.makeColor(a), Glow = Toolbox.makeColor(g), Pattern = p
        };

        /// <summary>Applies the element design if the actor is a dragon and doesn't already wear it.</summary>
        public static void Apply(Actor pActor, string pElement)
        {
            if (!Settings.DragonBodies || pActor?.avatar == null) return;
            var dragon = pActor.avatar.GetComponent<Dragon>();
            if (dragon == null || dragon.dragonAsset == null) return;
            if (!Designs.ContainsKey(pElement)) pElement = "lunar";

            DragonAsset body = GetBody(pElement);
            if (body == null || dragon.dragonAsset == body) return;
            dragon.dragonAsset = body;
            dragon.setFrames(dragon.state, true);
        }

        private static DragonAsset GetBody(string pElement)
        {
            if (_bodies.TryGetValue(pElement, out DragonAsset cached)) return cached;
            DragonAsset source = PrefabLibrary.instance.dragonAsset;
            if (source == null || source.list == null) return null;
            Design design = Designs[pElement];

            var body = ScriptableObject.CreateInstance<DragonAsset>();
            body.name = "gi_dragon_" + pElement;
            body.list = new DragonAssetContainer[source.list.Length];
            for (int i = 0; i < source.list.Length; i++)
            {
                DragonAssetContainer from = source.list[i];
                var frames = new Sprite[from.frames.Length];
                for (int f = 0; f < frames.Length; f++) frames[f] = Redraw(from.frames[f], design);
                body.list[i] = new DragonAssetContainer
                {
                    name = from.name, id = from.id, states = from.states, speed = from.speed, frames = frames
                };
            }
            _bodies[pElement] = body;
            return body;
        }

        // ---------------------------------------------------------------- the redraw

        private static Sprite Redraw(Sprite pSprite, Design pDesign)
        {
            if (pSprite == null) return null;
            Texture2D atlas = Readable(pSprite.texture);
            Rect rect = pSprite.textureRect;
            int x0 = Mathf.RoundToInt(rect.x), y0 = Mathf.RoundToInt(rect.y);
            int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
            Color[] src = atlas.GetPixels(x0, y0, w, h);

            // One pixel of padding all round for the glowing rim.
            int W = w + 2, H = h + 2;
            var dst = new Color[W * H];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = src[y * w + x];
                    if (c.a <= 0.02f) continue;
                    dst[(y + 1) * W + (x + 1)] = Paint(c, x, y, w, h, src, pDesign);
                }
            }

            // Rim: transparent pixels next to the body glow in the element colour.
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (dst[y * W + x].a > 0f) continue;
                    if (Solid(dst, W, H, x + 1, y) || Solid(dst, W, H, x - 1, y) ||
                        Solid(dst, W, H, x, y + 1) || Solid(dst, W, H, x, y - 1))
                    {
                        Color g = pDesign.Glow;
                        g.a = 0.55f;
                        dst[y * W + x] = g;
                    }
                }
            }

            var texture = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            texture.SetPixels(dst);
            texture.Apply();
            Vector2 pivot = new Vector2((pSprite.pivot.x + 1f) / W, (pSprite.pivot.y + 1f) / H);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, W, H), pivot, pSprite.pixelsPerUnit);
            sprite.name = pSprite.name;
            return sprite;
        }

        private static bool Solid(Color[] pPixels, int pW, int pH, int pX, int pY) =>
            pX >= 0 && pY >= 0 && pX < pW && pY < pH && pPixels[pY * pW + pX].a > 0.5f;

        private static Color Paint(Color c, int x, int y, int w, int h, Color[] src, Design d)
        {
            Color.RGBToHSV(c, out float hue, out float s, out float v);
            Color result;

            if (hue > 0.1f && hue < 0.2f && s > 0.5f && v > 0.85f)
            {
                // Bright yellows (eyes, the glow of the breath) burn in the design's glow colour.
                result = Color.Lerp(d.Glow, Color.white, 0.25f);
            }
            else if (v < 0.16f)
            {
                // Outlines stay dark but take a hint of the element.
                result = Color.Lerp(c, d.Shadow * 0.6f, 0.5f);
            }
            else if (s < 0.42f && v > 0.55f)
            {
                // Pale parts (belly plates, claws, horns) become the design's accent.
                result = Color.Lerp(d.Light, d.Accent, Mathf.InverseLerp(0.55f, 0.9f, v));
            }
            else
            {
                // Body: four-tone palette by perceived brightness (luminance), so the saturated reds of
                // the original keep their shading depth instead of washing out.
                float lum = 0.3f * c.r + 0.59f * c.g + 0.11f * c.b;
                result = lum < 0.25f ? Color.Lerp(d.Shadow, d.Base, lum / 0.25f)
                    : lum < 0.45f ? Color.Lerp(d.Base, d.Light, (lum - 0.25f) / 0.2f)
                    : Color.Lerp(d.Light, d.High, (lum - 0.45f) / 0.35f);
                result = ApplyPattern(result, x, y, w, h, src, d);
            }
            result.a = c.a;
            return result;
        }

        /// <summary>Element markings, computed from the pixel position and the silhouette.</summary>
        private static Color ApplyPattern(Color c, int x, int y, int w, int h, Color[] src, Design d)
        {
            bool top_edge = y + 1 < h && src[(y + 1) * w + x].a <= 0.02f; // back and wing edges (y grows upward)
            float n = Hash(x, y);
            switch (d.Pattern)
            {
                case "wind":
                    if (Mathf.Sin(x * 0.55f + y * 0.9f) > 0.82f) c = Color.Lerp(c, d.High, 0.45f);
                    if (top_edge) c = Color.Lerp(c, d.Accent, 0.7f);
                    break;
                case "plates":
                    if (((x / 3) + (y / 3)) % 2 == 0) c *= 0.9f;
                    if (n > 0.93f) c = d.Glow;
                    if (top_edge) c = Color.Lerp(c, d.High, 0.5f);
                    break;
                case "lightning":
                    int zig = Mathf.Abs((y % 6) - 3);
                    if ((x + zig) % 7 == 0) c = Color.Lerp(c, d.Accent, 0.75f);
                    break;
                case "leaves":
                    if (n > 0.8f) c *= 0.82f;
                    if (n > 0.985f) c = d.Accent;
                    if (top_edge) c = Color.Lerp(c, d.High, 0.4f);
                    break;
                case "scales":
                    if ((x + y) % 4 == 0) c = Color.Lerp(c, d.High, 0.3f);
                    c = Color.Lerp(c, d.Light, Mathf.Clamp01(1f - y / (float)h) * 0.25f);
                    break;
                case "molten":
                    if (Mathf.Abs(Mathf.Sin(x * 0.7f) + Mathf.Cos(y * 0.9f + x * 0.2f)) < 0.18f) c = Color.Lerp(d.High, d.Accent, n);
                    else if (n > 0.97f) c = d.Glow;
                    break;
                case "frost":
                    if (top_edge) c = d.Accent;
                    if (n > 0.95f) c = Color.Lerp(c, d.Accent, 0.8f);
                    break;
                case "stars":
                    if (n > 0.96f) c = d.Accent;
                    else if (n > 0.9f) c = Color.Lerp(c, d.High, 0.5f);
                    if (top_edge) c = Color.Lerp(c, d.Glow, 0.6f);
                    break;
            }
            return c;
        }

        private static float Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
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
