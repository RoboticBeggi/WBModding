using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace TeyvatChronicle.Combat
{
    /// <summary>One move. The archetype decides how it plays; everything else tunes it.</summary>
    public class MoveAsset
    {
        [JsonProperty("name")] public string Name;

        /// <summary>strike, barrage, dash, teleport, projectile, beam, nova, field, rain, pillar, shield, buff, heal, ultimate</summary>
        [JsonProperty("archetype")] public string Archetype = "strike";

        /// <summary>Element of the move; "self" uses the caster's element.</summary>
        [JsonProperty("element")] public string Element = "self";

        /// <summary>VFX set under GameResources/gi/fx/ (e.g. slash, burst, tornado).</summary>
        [JsonProperty("fx")] public string Fx;

        /// <summary>Secondary VFX (impact / landing / charge).</summary>
        [JsonProperty("fx2")] public string Fx2;

        [JsonProperty("sfx")] public string Sfx;
        [JsonProperty("sfx2")] public string Sfx2;

        /// <summary>Damage per hit = caster damage × power.</summary>
        [JsonProperty("power")] public float Power = 1f;

        /// <summary>Maximum distance to the target, in tiles.</summary>
        [JsonProperty("range")] public float Range = 3f;

        /// <summary>Area radius in tiles (0 = single target).</summary>
        [JsonProperty("radius")] public float Radius;

        [JsonProperty("hits")] public int Hits = 1;

        /// <summary>Seconds between hits or ticks.</summary>
        [JsonProperty("interval")] public float Interval = 0.12f;

        /// <summary>Seconds a field/beam lasts.</summary>
        [JsonProperty("duration")] public float Duration = 1f;

        [JsonProperty("cooldown")] public float Cooldown = 2f;

        /// <summary>Energy needed (and spent). Bursts use 100.</summary>
        [JsonProperty("energy")] public float Energy;

        /// <summary>Status given to targets hit (game status id).</summary>
        [JsonProperty("status")] public string Status;
        [JsonProperty("status_time")] public float StatusTime = 3f;

        /// <summary>Status given to the caster (shield, powerup, invincible…).</summary>
        [JsonProperty("self_status")] public string SelfStatus;

        /// <summary>none, minor or major. Major adds hit-stop, an impact frame and a camera punch.</summary>
        [JsonProperty("impact")] public string Impact = "none";

        /// <summary>Plays a short cinematic (letterbox, slow motion, title card) before the move.</summary>
        [JsonProperty("cutscene")] public bool Cutscene;

        /// <summary>VFX size multiplier.</summary>
        [JsonProperty("scale")] public float Scale = 1f;

        /// <summary>Fraction of the caster's max health healed on allies (heal archetype).</summary>
        [JsonProperty("heal")] public float Heal;
    }

    public class MoveSet
    {
        /// <summary>1 = Vision … 7 = Heavenly Principles. The highest tier a unit holds decides its moveset.</summary>
        [JsonProperty("tier")] public int Tier = 1;
        [JsonProperty("element")] public string Element = "physical";
        [JsonProperty("moves")] public List<string> Moves = new List<string>();
    }

    public class MoveFile
    {
        [JsonProperty("movesets")] public Dictionary<string, MoveSet> MoveSets = new Dictionary<string, MoveSet>();
        [JsonProperty("moves")] public Dictionary<string, MoveAsset> Moves = new Dictionary<string, MoveAsset>();
    }

    public static class MoveData
    {
        public static MoveFile Data = new MoveFile();

        /// <summary>Loads data/moves.custom.json if present, otherwise data/moves.json.</summary>
        public static void Load(string pModFolder)
        {
            string custom = Path.Combine(pModFolder, "data", "moves.custom.json");
            string standard = Path.Combine(pModFolder, "data", "moves.json");
            string path = File.Exists(custom) ? custom : standard;
            Data = JsonConvert.DeserializeObject<MoveFile>(File.ReadAllText(path)) ?? new MoveFile();
            foreach (KeyValuePair<string, MoveSet> set in Data.MoveSets)
            {
                set.Value.Moves.RemoveAll(id =>
                {
                    if (Data.Moves.ContainsKey(id)) return false;
                    ModClass.LogWarning($"[Combat] Moveset {set.Key} lists unknown move '{id}'");
                    return true;
                });
            }
        }

        public static MoveAsset Get(string pId) => Data.Moves.TryGetValue(pId, out MoveAsset move) ? move : null;
    }
}
