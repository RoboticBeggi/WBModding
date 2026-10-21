using System.Collections.Generic;
using UnityEngine;

namespace TeyvatChronicle.Combat
{
    /// <summary>Element colours and helpers to work out a unit's element and power tier from its traits.</summary>
    public static class Elements
    {
        public static readonly string[] All = { "anemo", "geo", "electro", "dendro", "hydro", "pyro", "cryo" };

        private static readonly Dictionary<string, string> Hex = new Dictionary<string, string>
        {
            ["anemo"] = "#74E5C5",
            ["geo"] = "#F2B53D",
            ["electro"] = "#B46CFF",
            ["dendro"] = "#9BD53C",
            ["hydro"] = "#3FA9FF",
            ["pyro"] = "#FF6A2A",
            ["cryo"] = "#9EE6F5",
            // Non-elemental powers
            ["abyss"] = "#9A4BFF",
            ["lunar"] = "#E6E0FF",
            ["divine"] = "#FFF2B0",
            ["death"] = "#7A6A9A",
            ["life"] = "#8DFF6A",
            ["time"] = "#74E5C5",
            ["space"] = "#C27CFF",
            ["delusion"] = "#FF4A55",
            ["physical"] = "#FFFFFF"
        };

        private static readonly Dictionary<string, Color> _cache = new Dictionary<string, Color>();

        public static Color Color(string pElement)
        {
            if (string.IsNullOrEmpty(pElement)) pElement = "physical";
            if (_cache.TryGetValue(pElement, out Color c)) return c;
            c = Toolbox.makeColor(Hex.TryGetValue(pElement, out string hex) ? hex : "#FFFFFF");
            _cache[pElement] = c;
            return c;
        }

        public static bool IsElement(string pElement) => System.Array.IndexOf(All, pElement) >= 0;

        /// <summary>The element suffix of a trait id like gi_vision_pyro, or null.</summary>
        public static string FromTraitId(string pTraitId)
        {
            foreach (string element in All)
            {
                if (pTraitId.EndsWith("_" + element)) return element;
            }
            return null;
        }
    }
}
