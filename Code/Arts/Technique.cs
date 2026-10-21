using System;
using System.Collections;
using System.Collections.Generic;
using TeyvatChronicle.Arts;
using TeyvatChronicle.Combat;

namespace TeyvatChronicle.Combat
{
    /// <summary>
    /// A fighting style: the techniques of one power trait. The highest-tier style a unit holds is the one
    /// it fights with.
    /// </summary>
    public class MoveSet
    {
        public string TraitId;

        /// <summary>The school's display name, e.g. "Zephyr Arts".</summary>
        public string Name;

        /// <summary>1 = Vision … 7 = Heavenly Principles.</summary>
        public int Tier = 1;

        public string Element = "physical";

        /// <summary>When set, a holder with a Vision fights in that Vision's element instead (Harbingers, Adepti).</summary>
        public bool TakesVisionElement;

        /// <summary>Technique ids, in the order shown in the Resonance panel.</summary>
        public readonly List<string> Moves = new List<string>();

        /// <summary>The style's God ability (Archons and above), or null.</summary>
        public string God;
    }
}

namespace TeyvatChronicle.Arts
{
    /// <summary>What a technique is good at. The tactical mind matches these against the battlefield.</summary>
    public enum Shape
    {
        /// <summary>Fine anywhere.</summary>
        Any,
        /// <summary>Several enemies bunched together: slams, novas, fields.</summary>
        Crowd,
        /// <summary>Enemies strung out in a line: fissures, beams, lances.</summary>
        Line,
        /// <summary>One enemy alone: iai cuts, combos, grapples.</summary>
        Lone,
        /// <summary>A badly wounded enemy: executions.</summary>
        Execute,
        /// <summary>Flying enemies: leaps, launches, sky strikes.</summary>
        AntiAir,
        /// <summary>A far-off enemy: dashes, blinks, long shots.</summary>
        Gap,
        /// <summary>The user is hurt: guards, heals, escapes.</summary>
        Guard,
        /// <summary>Allies are hurt nearby: heals, shields, rallies.</summary>
        Support
    }

    /// <summary>
    /// One technique. Its script is a coroutine that directs everything it does: movement, effects,
    /// sounds, damage and terrain. Scripts read their mastery level (I–III) from the cast.
    /// </summary>
    public class Technique
    {
        public string Id;
        public string Name;
        public string Desc;
        public MoveSet Style;
        public Shape Shape = Shape.Any;

        /// <summary>Distance to the target (tiles) the technique can be started from.</summary>
        public float Range = 3f;

        public float Cooldown = 6f;

        /// <summary>Mastery level needed to know the technique at all (1–3).</summary>
        public int Unlock = 1;

        /// <summary>An ultimate: shown with a cutscene at Level III (and always for gods).</summary>
        public bool Ultimate;

        /// <summary>A God ability: needs a full God gauge and stops the world.</summary>
        public bool God;

        /// <summary>Damage multiplier used by the Resonance panel when listing techniques.</summary>
        public float Power = 1f;

        public Func<Cast, IEnumerator> Script;
    }

    /// <summary>All styles and techniques, built in code by the Styles classes.</summary>
    public static class Library
    {
        public static readonly Dictionary<string, MoveSet> Styles = new Dictionary<string, MoveSet>();
        public static readonly Dictionary<string, Technique> Techniques = new Dictionary<string, Technique>();

        public static Technique Get(string pId) => pId != null && Techniques.TryGetValue(pId, out Technique t) ? t : null;

        public static MoveSet StyleOf(string pTraitId) =>
            pTraitId != null && Styles.TryGetValue(pTraitId, out MoveSet s) ? s : null;

        /// <summary>Starts a style. Add techniques to it with the returned builder.</summary>
        public static StyleBuilder Style(string pTraitId, string pName, int pTier, string pElement)
        {
            var set = new MoveSet { TraitId = pTraitId, Name = pName, Tier = pTier, Element = pElement };
            Styles[pTraitId] = set;
            return new StyleBuilder(set);
        }

        public static void Init()
        {
            Styles.Clear();
            Techniques.Clear();
            Arts.Styles.Register();
            int count = 0;
            foreach (MoveSet set in Styles.Values) count += set.Moves.Count + (set.God != null ? 1 : 0);
            ModClass.LogInfo($"[Arts] {Styles.Count} styles, {count} techniques");
        }
    }

    public class StyleBuilder
    {
        public readonly MoveSet Set;

        public StyleBuilder(MoveSet pSet) { Set = pSet; }

        public StyleBuilder VisionElement()
        {
            Set.TakesVisionElement = true;
            return this;
        }

        /// <summary>Adds a technique. Ids are prefixed with the style so names can repeat across styles.</summary>
        public StyleBuilder T(string pId, string pName, Shape pShape, float pRange, float pCooldown,
            Func<Cast, IEnumerator> pScript, string pDesc = null, int pUnlock = 1, float pPower = 1f,
            bool pUltimate = false)
        {
            var tech = new Technique
            {
                Id = Set.TraitId + "." + pId, Name = pName, Desc = pDesc, Style = Set, Shape = pShape,
                Range = pRange, Cooldown = pCooldown, Script = pScript, Unlock = pUnlock, Power = pPower,
                Ultimate = pUltimate
            };
            Library.Techniques[tech.Id] = tech;
            Set.Moves.Add(tech.Id);
            return this;
        }

        /// <summary>Sets the God ability (Archon tier and above).</summary>
        public StyleBuilder God(string pId, string pName, Func<Cast, IEnumerator> pScript, string pDesc = null)
        {
            var tech = new Technique
            {
                Id = Set.TraitId + "." + pId, Name = pName, Desc = pDesc, Style = Set, Shape = Shape.Any,
                Range = 40f, Cooldown = 0f, Script = pScript, God = true, Ultimate = true, Power = 6f
            };
            Library.Techniques[tech.Id] = tech;
            Set.God = tech.Id;
            return this;
        }
    }
}
