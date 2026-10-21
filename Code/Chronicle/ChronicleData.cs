using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TeyvatChronicle.Chronicle
{
    public enum Pacing
    {
        Lore = 0,
        Normal = 1,
        Fast = 2,
        Custom = 3
    }

    public class ChronicleEra
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("title")] public string Title;
    }

    public class ChronicleDelay
    {
        [JsonProperty("lore")] public float Lore;
        [JsonProperty("normal")] public float Normal;
        [JsonProperty("fast")] public float Fast;
    }

    public class ChronicleEvent
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("era")] public string Era;
        [JsonProperty("major")] public bool Major;
        [JsonProperty("title")] public string Title;
        [JsonProperty("banner")] public string Banner;
        [JsonProperty("when")] public string When;
        [JsonProperty("text")] public string Text;
        [JsonProperty("color")] public string Color = "#F3961F";
        [JsonProperty("flash")] public string Flash = "none";
        [JsonProperty("shake")] public float Shake;
        [JsonProperty("sound")] public string Sound;
        [JsonProperty("enabled")] public bool Enabled = true;
        [JsonProperty("delay")] public ChronicleDelay Delay = new ChronicleDelay();

        /// <summary>Effects are kept as raw JSON objects so new fields can be added without code changes.</summary>
        [JsonProperty("effects")] public List<JObject> Effects = new List<JObject>();
    }

    public class ChronicleFile
    {
        [JsonProperty("eras")] public List<ChronicleEra> Eras = new List<ChronicleEra>();
        [JsonProperty("events")] public List<ChronicleEvent> Events = new List<ChronicleEvent>();
    }

    public static class ChronicleData
    {
        public static ChronicleFile Timeline = new ChronicleFile();
        public static string LoadedFrom;

        /// <summary>
        /// Loads data/chronicle.custom.json if present, otherwise data/chronicle.json.
        /// The custom file lets players keep their own timeline across mod updates.
        /// </summary>
        public static void Load(string pModFolder)
        {
            string custom = Path.Combine(pModFolder, "data", "chronicle.custom.json");
            string standard = Path.Combine(pModFolder, "data", "chronicle.json");
            string path = File.Exists(custom) ? custom : standard;
            Timeline = JsonConvert.DeserializeObject<ChronicleFile>(File.ReadAllText(path)) ?? new ChronicleFile();
            LoadedFrom = path;
        }

        public static int EraIndex(string pEraId)
        {
            for (int i = 0; i < Timeline.Eras.Count; i++)
            {
                if (Timeline.Eras[i].Id == pEraId) return i;
            }
            return -1;
        }

        public static string EraTitle(string pEraId)
        {
            int index = EraIndex(pEraId);
            return index >= 0 ? Timeline.Eras[index].Title : pEraId;
        }

        /// <summary>Index of the first event of an era, or -1.</summary>
        public static int FirstEventOfEra(string pEraId)
        {
            for (int i = 0; i < Timeline.Events.Count; i++)
            {
                if (Timeline.Events[i].Era == pEraId) return i;
            }
            return -1;
        }
    }
}
