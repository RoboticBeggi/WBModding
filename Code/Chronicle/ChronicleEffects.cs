using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using TeyvatChronicle.Powers;

namespace TeyvatChronicle.Chronicle
{
    /// <summary>
    /// Runs the effects listed on a chronicle event. Every effect is optional and fails softly:
    /// a missing trait, an empty world or an unknown type is logged and skipped.
    /// </summary>
    public static class ChronicleEffects
    {
        /// <summary>Effects that only change who holds what. These are replayed when starting mid-timeline.</summary>
        private static readonly HashSet<string> PersistentTypes = new HashSet<string>
        {
            "age", "grant_trait", "grant_set", "remove_trait", "mark_kingdom", "kingdom_traits", "gather_gnosis"
        };

        private static readonly HashSet<string> DestructiveTypes = new HashSet<string>
        {
            "meteor", "earthquake", "destroy_city", "disaster"
        };

        /// <summary>Runs the event's effects and returns a tile worth pointing the camera at, if any.</summary>
        public static WorldTile Run(ChronicleEvent pEvent, bool pCatchUp)
        {
            WorldTile focus = null;
            foreach (JObject effect in pEvent.Effects)
            {
                string type = (string)effect["type"] ?? "";
                if (pCatchUp && !PersistentTypes.Contains(type)) continue;
                if (DestructiveTypes.Contains(type) && !Settings.DestructiveEffects) continue;
                if (type == "war" && !Settings.WarEffects) continue;
                try
                {
                    WorldTile tile = RunOne(type, effect);
                    if (focus == null) focus = tile;
                }
                catch (Exception e)
                {
                    ModClass.LogWarning($"[Chronicle] Effect '{type}' of event '{pEvent.Id}' failed: {e.Message}");
                }
            }
            return focus;
        }

        private static WorldTile RunOne(string pType, JObject pEffect)
        {
            switch (pType)
            {
                case "age": return SetAge((string)pEffect["id"]);
                case "grant_trait":
                    return GrantTrait((string)pEffect["trait"], Int(pEffect, "count", 1), (string)pEffect["pick"] ?? "random",
                        Bool(pEffect, "unique", false));
                case "grant_set":
                    return GrantSet(pEffect["traits"]?.ToObject<List<string>>() ?? new List<string>(),
                        (string)pEffect["pick"] ?? "random");
                case "remove_trait":
                    return RemoveTrait((string)pEffect["trait"], Int(pEffect, "count", -1), Bool(pEffect, "kill", false),
                        (string)pEffect["replace_with"], Bool(pEffect, "pass_on", false));
                case "war": return StartWars(Int(pEffect, "count", 1));
                case "meteor": return Meteors(Int(pEffect, "count", 1), (string)pEffect["target"] ?? "random");
                case "earthquake": return Earthquakes(Int(pEffect, "count", 1));
                case "destroy_city": return DestroyCities(Int(pEffect, "count", 1), (string)pEffect["target"] ?? "random");
                case "mark_kingdom":
                    return MarkKingdom((string)pEffect["tag"], (string)pEffect["pick"] ?? "random", Bool(pEffect, "rename", true));
                case "kingdom_traits":
                    return KingdomTraits((string)pEffect["tag"], (string)pEffect["trait"], Float(pEffect, "fraction", 0.5f));
                case "disaster": return Disaster((string)pEffect["id"]);
                case "spawn_unit":
                    return SpawnUnits((string)pEffect["actor"] ?? "dragon", Int(pEffect, "count", 1),
                        pEffect["traits"]?.ToObject<List<string>>() ?? new List<string>());
                case "gather_gnosis": return GatherGnosis((string)pEffect["from"], (string)pEffect["to"]);
                default:
                    ModClass.LogWarning($"[Chronicle] Unknown effect type '{pType}'");
                    return null;
            }
        }

        // ---------------------------------------------------------------- units

        private static List<Actor> AliveUnits() =>
            World.world.units.getSimpleList().Where(a => a != null && a.isAlive()).ToList();

        private static bool IsCiv(Actor pActor) => pActor.isSapient() && pActor.kingdom != null && pActor.kingdom.isCiv();

        private static float Power(Actor pActor) => pActor.stats["health"] + pActor.stats["damage"] * 5f;

        private static bool HasAnyTeyvatTrait(Actor pActor, string pPrefix) =>
            pActor.getTraits().Any(t => t.id.StartsWith(pPrefix));

        /// <summary>Candidates for a pick mode, best first.</summary>
        private static List<Actor> Candidates(string pPick)
        {
            List<Actor> units = AliveUnits();
            switch (pPick)
            {
                case "strongest":
                    return units.OrderByDescending(Power).ToList();
                case "strongest_civ":
                    return Fallback(units.Where(IsCiv).OrderByDescending(Power).ToList(), units.OrderByDescending(Power));
                case "kings":
                    return Fallback(units.Where(a => a.isKing()).ToList(), units.Where(IsCiv).OrderByDescending(Power));
                case "creatures":
                    return Fallback(units.Where(a => !a.isSapient()).OrderByDescending(Power).ToList(), units.OrderByDescending(Power));
                case "random_civ":
                    return Fallback(Shuffle(units.Where(IsCiv).ToList()), Shuffle(units));
                default:
                    return Shuffle(units);
            }
        }

        private static List<Actor> Fallback(List<Actor> pPreferred, IEnumerable<Actor> pOthers)
        {
            return pPreferred.Count > 0 ? pPreferred : pOthers.ToList();
        }

        private static List<T> Shuffle<T>(List<T> pList)
        {
            for (int i = pList.Count - 1; i > 0; i--)
            {
                int j = Randy.randomInt(0, i + 1);
                (pList[i], pList[j]) = (pList[j], pList[i]);
            }
            return pList;
        }

        private static List<Actor> Holders(string pTraitPattern)
        {
            if (string.IsNullOrEmpty(pTraitPattern)) return new List<Actor>();
            if (pTraitPattern.EndsWith("*"))
            {
                string prefix = pTraitPattern.TrimEnd('*');
                return AliveUnits().Where(a => HasAnyTeyvatTrait(a, prefix)).ToList();
            }
            return AliveUnits().Where(a => a.hasTrait(pTraitPattern)).ToList();
        }

        private static bool TraitExists(string pTraitId)
        {
            if (AssetManager.traits.has(pTraitId)) return true;
            ModClass.LogWarning($"[Chronicle] Trait '{pTraitId}' does not exist; skipped.");
            return false;
        }

        private static bool MustBeUnique(string pTraitId, bool pUnique) =>
            pUnique || (Settings.UniqueHolders && TeyvatTraits.UniqueTraits.Contains(pTraitId));

        // ---------------------------------------------------------------- effects

        private static WorldTile SetAge(string pAgeId)
        {
            WorldAgeAsset age = AssetManager.era_library.get(pAgeId);
            if (age != null) World.world.era_manager.setCurrentAge(age);
            return null;
        }

        private static WorldTile GrantTrait(string pTraitId, int pCount, string pPick, bool pUnique)
        {
            if (!TraitExists(pTraitId)) return null;
            if (MustBeUnique(pTraitId, pUnique))
            {
                if (Holders(pTraitId).Count > 0) return null;
                pCount = 1;
            }
            WorldTile focus = null;
            int given = 0;
            foreach (Actor actor in Candidates(pPick))
            {
                if (given >= pCount) break;
                if (actor.hasTrait(pTraitId)) continue;
                actor.addTrait(pTraitId);
                focus ??= actor.current_tile;
                given++;
            }
            return focus;
        }

        /// <summary>Gives each trait in the set to a different unit (e.g. one Gnosis per Archon).</summary>
        private static WorldTile GrantSet(List<string> pTraits, string pPick)
        {
            WorldTile focus = null;
            var used = new HashSet<Actor>();
            List<Actor> candidates = Candidates(pPick);
            foreach (string trait in pTraits)
            {
                if (!TraitExists(trait)) continue;
                if (MustBeUnique(trait, false) && Holders(trait).Count > 0) continue;
                Actor actor = candidates.FirstOrDefault(a => !used.Contains(a) && !a.hasTrait(trait));
                if (actor == null) break;
                actor.addTrait(trait);
                used.Add(actor);
                focus ??= actor.current_tile;
            }
            return focus;
        }

        private static WorldTile RemoveTrait(string pPattern, int pCount, bool pKill, string pReplaceWith, bool pPassOn)
        {
            WorldTile focus = null;
            List<Actor> holders = Holders(pPattern);
            if (pCount >= 0) holders = holders.Take(pCount).ToList();
            foreach (Actor actor in holders)
            {
                focus ??= actor.current_tile;
                var removed = pPattern.EndsWith("*")
                    ? actor.getTraits().Where(t => t.id.StartsWith(pPattern.TrimEnd('*'))).Select(t => t.id).ToList()
                    : new List<string> { pPattern };
                foreach (string id in removed) actor.removeTrait(id);

                if (!string.IsNullOrEmpty(pReplaceWith) && TraitExists(pReplaceWith)) actor.addTrait(pReplaceWith);

                if (pPassOn)
                {
                    Actor successor = AliveUnits()
                        .Where(a => a != actor && IsCiv(a) && a.kingdom == actor.kingdom)
                        .OrderByDescending(Power).FirstOrDefault()
                        ?? Candidates("strongest_civ").FirstOrDefault(a => a != actor);
                    foreach (string id in removed) successor?.addTrait(id);
                }

                if (pKill) actor.die(false, AttackType.Divine);
            }
            return focus;
        }

        private static WorldTile StartWars(int pCount)
        {
            List<Kingdom> kingdoms = World.world.kingdoms.list.Where(k => k.isAlive() && k.isCiv()).ToList();
            WarTypeAsset type = AssetManager.war_types_library.get("normal");
            WorldTile focus = null;
            for (int i = 0; i < pCount && kingdoms.Count >= 2; i++)
            {
                Kingdom attacker = kingdoms.GetRandom();
                Kingdom defender = kingdoms.Where(k => k != attacker && !attacker.isInWarWith(k)).ToList().GetRandom();
                if (defender == null) continue;
                World.world.diplomacy.startWar(attacker, defender, type);
                focus ??= attacker.capital?.getTile();
            }
            return focus;
        }

        private static List<City> TargetCities(string pTarget)
        {
            if (pTarget.StartsWith("kingdom:"))
            {
                Kingdom kingdom = ChronicleState.GetTaggedKingdom(pTarget.Substring("kingdom:".Length));
                if (kingdom != null) return kingdom.cities.Where(c => c.isAlive()).ToList();
            }
            return World.world.cities.list.Where(c => c.isAlive()).ToList();
        }

        private static WorldTile Meteors(int pCount, string pTarget)
        {
            WorldTile focus = null;
            List<City> cities = pTarget == "random" ? new List<City>() : TargetCities(pTarget);
            for (int i = 0; i < pCount; i++)
            {
                WorldTile tile = cities.Count > 0 ? cities.GetRandom().getTile() : World.world.tiles_list.GetRandom();
                if (tile == null) continue;
                Meteorite.spawnMeteorite(tile);
                focus ??= tile;
            }
            return focus;
        }

        private static WorldTile Earthquakes(int pCount)
        {
            WorldTile focus = null;
            for (int i = 0; i < pCount; i++)
            {
                WorldTile tile = World.world.tiles_list.GetRandom();
                Earthquake.startQuake(tile, EarthquakeType.SmallDisaster);
                focus ??= tile;
            }
            return focus;
        }

        private static WorldTile DestroyCities(int pCount, string pTarget)
        {
            WorldTile focus = null;
            foreach (City city in Shuffle(TargetCities(pTarget)).Take(pCount).ToList())
            {
                WorldTile tile = city.getTile();
                focus ??= tile;
                if (tile != null) Meteorite.spawnMeteorite(tile);
                foreach (Building building in city.buildings.ToList()) building.startDestroyBuilding();
                foreach (Actor unit in AliveUnits().Where(a => a.city == city && Randy.randomChance(0.7f)))
                {
                    unit.die(false, AttackType.Divine);
                }
            }
            return focus;
        }

        private static WorldTile MarkKingdom(string pTag, string pPick, bool pRename)
        {
            List<Kingdom> kingdoms = World.world.kingdoms.list.Where(k => k.isAlive() && k.isCiv()).ToList();
            if (kingdoms.Count == 0) return null;
            Kingdom chosen = pPick switch
            {
                "largest" => kingdoms.OrderByDescending(k => k.countUnits()).First(),
                "smallest" => kingdoms.OrderBy(k => k.countUnits()).First(),
                _ => kingdoms.GetRandom()
            };
            ChronicleState.TagKingdom(pTag, chosen);
            if (pRename && pTag == "khaenriah") chosen.setName("Khaenri'ah");
            return chosen.capital?.getTile();
        }

        private static WorldTile KingdomTraits(string pTag, string pTrait, float pFraction)
        {
            Kingdom kingdom = ChronicleState.GetTaggedKingdom(pTag);
            if (kingdom == null || !TraitExists(pTrait)) return null;
            WorldTile focus = null;
            foreach (Actor unit in AliveUnits().Where(a => a.kingdom == kingdom && Randy.randomChance(pFraction)))
            {
                unit.addTrait(pTrait);
                focus ??= unit.current_tile;
            }
            return focus;
        }

        /// <summary>
        /// Spawns units (by default real dragons) on random land. With a trait list, unit N gets trait N,
        /// and the count becomes the list length. Unique traits that already have a holder are skipped.
        /// </summary>
        private static WorldTile SpawnUnits(string pActorId, int pCount, List<string> pTraits)
        {
            if (AssetManager.actor_library.get(pActorId) == null)
            {
                ModClass.LogWarning($"[Chronicle] Unknown actor '{pActorId}'");
                return null;
            }
            if (pTraits.Count > 0) pCount = pTraits.Count;
            WorldTile focus = null;
            for (int i = 0; i < pCount; i++)
            {
                string trait = i < pTraits.Count ? pTraits[i] : null;
                if (trait != null && (!TraitExists(trait) || MustBeUnique(trait, false) && Holders(trait).Count > 0)) continue;
                WorldTile tile = World.world.islands_calculator.getRandomIslandGround()?.getRandomTile()
                                 ?? World.world.tiles_list.GetRandom();
                Actor unit = World.world.units.spawnNewUnit(pActorId, tile, true, true);
                if (unit == null) continue;
                if (trait != null) unit.addTrait(trait);
                focus ??= tile;
            }
            return focus;
        }

        private static WorldTile Disaster(string pId)
        {
            DisasterAsset asset = AssetManager.disasters.get(pId);
            asset?.action?.Invoke(asset);
            return null;
        }

        /// <summary>The Fatui collect a Gnosis: it moves from its Archon to the holder of the target trait.</summary>
        private static WorldTile GatherGnosis(string pFrom, string pTo)
        {
            Actor from = Holders(pFrom).FirstOrDefault();
            Actor to = Holders(pTo).FirstOrDefault();
            if (from == null || to == null || from == to) return null;
            from.removeTrait(pFrom);
            to.addTrait(pFrom);
            return to.current_tile;
        }

        // ---------------------------------------------------------------- json helpers

        private static int Int(JObject pObj, string pKey, int pDefault) => pObj[pKey]?.Value<int>() ?? pDefault;
        private static float Float(JObject pObj, string pKey, float pDefault) => pObj[pKey]?.Value<float>() ?? pDefault;
        private static bool Bool(JObject pObj, string pKey, bool pDefault) => pObj[pKey]?.Value<bool>() ?? pDefault;
    }
}
