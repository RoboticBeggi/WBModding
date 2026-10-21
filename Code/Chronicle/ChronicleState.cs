using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeyvatChronicle.Chronicle
{
    /// <summary>
    /// The chronicle's progress lives in the world's own save data (map_stats.custom_data),
    /// so each world remembers where it is in the timeline.
    /// </summary>
    public static class ChronicleState
    {
        private const string KeyActive = "gi_chr_active";
        private const string KeyPaused = "gi_chr_paused";
        private const string KeyIndex = "gi_chr_index";
        private const string KeyNextYear = "gi_chr_next_year";
        private const string KeyWarned = "gi_chr_warned";
        private const string KeyTagPrefix = "gi_chr_tag_";

        private static BaseSystemData Data => World.world?.map_stats?.custom_data;

        public static bool Ready => World.world?.map_stats?.custom_data != null && !Config.worldLoading;

        public static bool Active
        {
            get { Data.get(KeyActive, out bool value); return value; }
            private set => Data.set(KeyActive, value);
        }

        public static bool Paused
        {
            get { Data.get(KeyPaused, out bool value); return value; }
            set => Data.set(KeyPaused, value);
        }

        /// <summary>Index of the next event to fire.</summary>
        public static int Index
        {
            get { Data.get(KeyIndex, out int value); return value; }
            private set => Data.set(KeyIndex, value);
        }

        public static int NextYear
        {
            get { Data.get(KeyNextYear, out int value); return value; }
            private set => Data.set(KeyNextYear, value);
        }

        /// <summary>Which countdown warning was already shown for the next event (0 = none, 10, 1).</summary>
        private static int Warned
        {
            get { Data.get(KeyWarned, out int value); return value; }
            set => Data.set(KeyWarned, value);
        }

        public static List<ChronicleEvent> Events => ChronicleData.Timeline.Events;

        public static ChronicleEvent NextEvent => Active && Index < Events.Count ? Events[Index] : null;

        // ---------------------------------------------------------------- kingdom tags

        public static void TagKingdom(string pTag, Kingdom pKingdom) => Data.set(KeyTagPrefix + pTag, pKingdom.getID().ToString());

        public static Kingdom GetTaggedKingdom(string pTag)
        {
            Data.get(KeyTagPrefix + pTag, out string id);
            if (string.IsNullOrEmpty(id) || !long.TryParse(id, out long kingdomId)) return null;
            Kingdom kingdom = World.world.kingdoms.get(kingdomId);
            return kingdom != null && kingdom.isAlive() ? kingdom : null;
        }

        public static string TaggedKingdomName(string pTag, string pFallback)
        {
            Kingdom kingdom = GetTaggedKingdom(pTag);
            return kingdom != null ? kingdom.name : pFallback;
        }

        // ---------------------------------------------------------------- control

        /// <summary>Starts (or restarts) the chronicle at an event. Earlier events can be replayed silently.</summary>
        public static void StartAt(int pEventIndex)
        {
            if (!Ready || Events.Count == 0) return;
            pEventIndex = Mathf.Clamp(pEventIndex, 0, Events.Count - 1);

            if (Settings.CatchUp)
            {
                for (int i = 0; i < pEventIndex; i++)
                {
                    if (IsEnabled(Events[i])) ChronicleEffects.Run(Events[i], true);
                }
            }

            Active = true;
            Paused = false;
            Index = pEventIndex;
            NextYear = Date.getCurrentYear();
            Warned = 0;
            Announcer.AnnounceStart(Events[pEventIndex]);
        }

        public static void StartAtEra(int pEraIndex)
        {
            var eras = ChronicleData.Timeline.Eras;
            if (eras.Count == 0) return;
            int first = ChronicleData.FirstEventOfEra(eras[Mathf.Clamp(pEraIndex, 0, eras.Count - 1)].Id);
            StartAt(Math.Max(0, first));
        }

        /// <summary>Fires the next event right now.</summary>
        public static void Advance()
        {
            if (!Ready || !Active) return;
            NextYear = Date.getCurrentYear();
            Warned = 0;
            Tick();
        }

        public static void Stop()
        {
            if (!Ready) return;
            Active = false;
            Paused = false;
            Announcer.AnnounceSimple("The Teyvat Chronicle has been stopped.", "#C9C9C9");
        }

        public static bool IsEnabled(ChronicleEvent pEvent)
        {
            if (!pEvent.Enabled) return false;
            if (!Settings.EraEnabled(pEvent.Era)) return false;
            if (!pEvent.Major && !Settings.MinorEvents) return false;
            return true;
        }

        /// <summary>Years to wait before an event, for the current pacing setting.</summary>
        public static int DelayFor(ChronicleEvent pEvent)
        {
            float years = Settings.Pacing switch
            {
                Pacing.Lore => pEvent.Delay.Lore,
                Pacing.Fast => pEvent.Delay.Fast,
                Pacing.Custom => pEvent.Delay.Normal * Settings.CustomTimeScale,
                _ => pEvent.Delay.Normal
            };
            return Mathf.Max(1, Mathf.RoundToInt(years));
        }

        // ---------------------------------------------------------------- tick

        public static void Tick()
        {
            if (!Ready || !Active || Paused) return;

            // Disabled events are skipped instantly, without waiting for their delay.
            while (Index < Events.Count && !IsEnabled(Events[Index])) Index++;

            if (Index >= Events.Count)
            {
                Active = false;
                Announcer.AnnounceSimple("★ The Teyvat Chronicle is complete — the story continues…", "#FFE58A");
                return;
            }

            ChronicleEvent next = Events[Index];
            int year = Date.getCurrentYear();
            int yearsLeft = NextYear - year;

            if (Settings.CountdownWarnings && next.Major)
            {
                if ((yearsLeft == 10 || yearsLeft == 1) && Warned != yearsLeft)
                {
                    Warned = yearsLeft;
                    Announcer.AnnounceCountdown(next, yearsLeft);
                }
            }

            if (yearsLeft > 0) return;

            WorldTile focus = ChronicleEffects.Run(next, false);
            Announcer.Announce(next, focus);

            Index++;
            Warned = 0;
            while (Index < Events.Count && !IsEnabled(Events[Index])) Index++;
            if (Index < Events.Count) NextYear = year + DelayFor(Events[Index]);
        }
    }
}
