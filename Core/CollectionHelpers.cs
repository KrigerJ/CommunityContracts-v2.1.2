using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using xTile.Dimensions;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.DirectionHelper;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core
{
    internal class CollectionHelpers
    {
        public static bool IsTillable(GameLocation loc, int x, int y)
        {
            Vector2 tile = new Vector2(x, y);

            if (loc.doesTileHaveProperty(x, y, "Diggable", "Back") == null)
                return false;

            if (loc.objects.ContainsKey(tile))
                return false;

            if (loc.terrainFeatures.ContainsKey(tile))
                return false;

            if (!loc.isTileLocationOpen(tile))
                return false;

            if (!loc.isTilePassable(new Location(x * 64, y * 64), Game1.viewport))
                return false;

            return true;
        }
        public static bool IsWaterableTile(GameLocation loc, Vector2 tile)
        {
            if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                feature is HoeDirt dirt &&
                dirt.state.Value == HoeDirt.dry)
            {
                return true;
            }

            if (loc.objects.TryGetValue(tile, out var obj) &&
                obj is IndoorPot pot &&
                pot.hoeDirt.Value is HoeDirt potDirt &&
                potDirt.state.Value == HoeDirt.dry)
            {
                return true;
            }

            return false;
        }

        private static bool warnedAboutResettingLocation_Conversion = false;
        public static int CountFertilizer()
        {
            return Game1.player.Items
                .OfType<SObject>()
                .Where(IsFertilizerItem)
                .Sum(i => i.Stack);
        }
        private static bool IsFertilizerItem(SObject obj)
        {
            if (obj == null)
                return false;

            if (obj.HasContextTag("fertilizer"))
                return true;

            if (obj.Category == SObject.fertilizerCategory)
                return true;

            switch (obj.ParentSheetIndex)
            {
                case 368:
                case 369:
                case 370:
                case 371:
                case 465:
                case 466:
                case 918:
                case 919:
                    return true;
            }

            return false;
        }
        public static bool ConsumeOneFertilizer()
        {
            foreach (var item in Game1.player.Items.OfType<SObject>())
            {
                if (IsFertilizerItem(item))
                {
                    item.Stack--;
                    if (item.Stack <= 0)
                        Game1.player.removeItemFromInventory(item);

                    return true;
                }
            }

            return false;
        }
        public static bool IsFertilizableTile(GameLocation loc, Vector2 tile)
        {
            if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                feature is HoeDirt dirt)
            {
                return dirt.fertilizer.Value == "0";
            }

            return false;
        }
        public static bool IsHarvestableTile(GameLocation loc, Vector2 tile)
        {
            if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                feature is HoeDirt dirt &&
                dirt.crop is Crop crop &&
                !crop.dead.Value)
            {
                string seedId = crop.netSeedIndex.Value;
                if (!string.IsNullOrEmpty(seedId) &&
                    Game1.cropData.TryGetValue(seedId, out var data))
                {
                    int phase = crop.currentPhase.Value;
                    int finalPhase = crop.phaseDays.Count - 1;

                    if (phase >= finalPhase && data.HarvestItemId != null)
                        return true;
                }
            }

            if (loc.objects.TryGetValue(tile, out var obj) &&
                obj is IndoorPot pot &&
                pot.hoeDirt?.Value?.crop is Crop potCrop &&
                !potCrop.dead.Value)
            {
                string seedId = potCrop.netSeedIndex.Value;
                if (!string.IsNullOrEmpty(seedId) &&
                    Game1.cropData.TryGetValue(seedId, out var data))
                {
                    int phase = potCrop.currentPhase.Value;
                    int finalPhase = potCrop.phaseDays.Count - 1;

                    if (phase >= finalPhase && data.HarvestItemId != null)
                        return true;
                }
            }

            return false;
        }

        public static int GetVirtualCropYield(Crop crop, Farmer farmer)
        {
            if (!Game1.cropData.TryGetValue(crop.rowInSpriteSheet.Value.ToString(), out var data))
                return 1;

            int amount = data.HarvestMinStack;

            if (data.HarvestMaxStack > data.HarvestMinStack)
            {
                amount = Game1.random.Next(
                    data.HarvestMinStack,
                    data.HarvestMaxStack + 1
                );

                if (data.HarvestMaxIncreasePerFarmingLevel > 0)
                {
                    int bonus = (int)(farmer.FarmingLevel / data.HarvestMaxIncreasePerFarmingLevel);
                    amount += bonus;
                }
            }

            if (data.ExtraHarvestChance > 0f)
            {
                while (Game1.random.NextDouble() < data.ExtraHarvestChance)
                    amount++;
            }

            return amount < 1 ? 1 : amount;
        }
        public static bool TryChargeFeeOrStop(
            int feePerTile,
            string npcName,
            Dictionary<(string id, int quality), SObject> itemMap,
            IMonitor monitor,
            ModConfig Config)
        {
            if (Game1.player.Money < feePerTile)
            {
                var partialItems = itemMap.Values
                    .Where(i => i.Stack > 0)
                    .Cast<Item>()
                    .ToList();

                if (partialItems.Count > 0)
                {
                    DeliverContractsItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = partialItems,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);
                }

                Game1.showGlobalMessage(T("StoppedEarlyGold", new { npc = npcName }));
                return false;
            }

            Game1.player.Money -= feePerTile;
            return true;
        }
        public static bool TryChargeFeeOrStopSimple(
            int feePerTile,
            string npcName,
            IMonitor monitor)
        {
            if (Game1.player.Money < feePerTile)
            {
                Game1.showGlobalMessage(T("StoppedEarlyGold", new { npc = npcName }));
                return false;
            }

            Game1.player.Money -= feePerTile;
            return true;
        }
        public static List<(GameLocation loc, Vector2 tile, CrabPot pot)> ScanCrabPots()
        {
            var results = new List<(GameLocation, Vector2, CrabPot)>();

            foreach (var loc in Game1.locations)
            {
                foreach (var pair in loc.objects.Pairs)
                {
                    if (pair.Value is CrabPot pot &&
                        pot.readyForHarvest.Value &&
                        pot.heldObject.Value is SObject)
                    {
                        results.Add((loc, pair.Key, pot));
                    }
                }
            }

            return results;
        }
        public static void AddCrabPotItemToMap(
            Dictionary<(string id, int quality), SObject> itemMap,
            SObject catchObj,
            int quality)
        {
            string id = catchObj.ItemId;
            var key = (id, quality);

            if (!itemMap.TryGetValue(key, out var stacked))
            {
                stacked = new SObject(id, 1) { Quality = quality };
                itemMap[key] = stacked;
            }
            else
            {
                stacked.Stack += 1;
            }
        }
        public static void FriendshipProgressTick(
        ref int counter,
        ref int totalPoints,
        int threshold)
        {
            counter++;

            if (counter >= threshold)
            {
                totalPoints++;
                counter = 0;
            }
        }
        public static void FriendshipInitialAward(ref int totalPoints)
        {
            totalPoints++;
        }
        public static List<(GameLocation loc, Vector2 tile, SObject tapper)> ScanTappers()
        {
            var results = new List<(GameLocation, Vector2, SObject)>();

            foreach (var loc in Game1.locations)
            {
                foreach (var pair in loc.objects.Pairs)
                {
                    if (pair.Value is SObject tapper &&
                        tapper.bigCraftable.Value &&
                        (tapper.ParentSheetIndex == 105 || tapper.ParentSheetIndex == 264) &&
                        tapper.readyForHarvest.Value &&
                        tapper.heldObject.Value is SObject)
                    {
                        results.Add((loc, pair.Key, tapper));
                    }
                }
            }

            return results;
        }
        public static void AddTapperItemToMap(
            Dictionary<(string id, int quality), SObject> itemMap,
            SObject tappedProduct,
            int quality)
        {
            string id = tappedProduct.ItemId;
            int amount = tappedProduct.Stack + (1 + quality);

            var key = (id, quality);

            if (!itemMap.TryGetValue(key, out var stacked))
            {
                stacked = new SObject(id, amount);
                itemMap[key] = stacked;
            }
            else
            {
                stacked.Stack += amount;
            }
        }

        public static bool IsSafeCrabPotTile(GameLocation loc, Vector2 tile)
        {
            if(!IsStableLocation(loc))
            {
                Game1.showGlobalMessage("This area resets daily. Items should not be placed here.");
                return false;
            }

            if (loc.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Water", "Back") == null)
                return false;

            if (loc.objects.ContainsKey(tile))
                return false;

            if (!loc.isTilePassable(tile))
                return false;

            return true;
        }
        public static List<(GameLocation loc, Vector2 tile, SObject honey)> ScanReadyBeeHouses()
        {
            var list = new List<(GameLocation, Vector2, SObject)>();

            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        obj.bigCraftable.Value &&
                        obj.ParentSheetIndex == 10 &&
                        obj.readyForHarvest.Value &&
                        obj.heldObject.Value is SObject honey)
                    {
                        list.Add((location, pair.Key, honey));
                    }
                }
            }

            return list;
        }
        public static List<(GameLocation loc, ResourceClump clump)> ScanHardwoodClumps()
        {
            var list = new List<(GameLocation, ResourceClump)>();

            foreach (var location in Game1.locations)
            {
                foreach (var clump in location.resourceClumps.ToList())
                {
                    if (clump.parentSheetIndex.Value == ResourceClump.stumpIndex ||
                        clump.parentSheetIndex.Value == ResourceClump.hollowLogIndex)
                    {
                        list.Add((location, clump));
                    }
                }
            }
            return list;
        }
        public static List<(GameLocation loc, Vector2 tile, SObject weed)> ScanWeeds()
        {
            var list = new List<(GameLocation, Vector2, SObject)>();

            int[] weedIndices =
            {
                0, 313, 314, 315, 316, 317, 318,
                452,
                674, 675, 676, 677, 678, 679,
                747, 748, 750, 784, 785, 786, 792, 793, 794,
                882, 883, 884
            };

            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        weedIndices.Contains(obj.ParentSheetIndex) &&
                        !obj.bigCraftable.Value)
                    {
                        list.Add((location, pair.Key, obj));
                    }
                }
            }

            return list;
        }
        public static List<(GameLocation loc, Vector2 tile, SObject debris)> ScanWoodDebris()
        {
            var list = new List<(GameLocation, Vector2, SObject)>();

            int[] woodIndices =
            {
                30, 294, 295, 388
            };

            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        woodIndices.Contains(obj.ParentSheetIndex) &&
                        !obj.bigCraftable.Value)
                    {
                        list.Add((location, pair.Key, obj));
                    }
                }
            }

            return list;
        }
        public static List<(GameLocation loc, Vector2 tile, int baseYield, bool isClump)> ScanStoneDebris()
        {
            var list = new List<(GameLocation, Vector2, int, bool)>();

            int[] smallStoneIndices = { 343, 450, 668, 670 };

            foreach (var location in Game1.locations)
            {
                foreach (var clump in location.resourceClumps.ToList())
                {
                    if (clump.parentSheetIndex.Value == ResourceClump.boulderIndex)
                    {
                        list.Add((location, clump.Tile, 15, true));
                    }
                }

                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is SObject obj &&
                        smallStoneIndices.Contains(obj.ParentSheetIndex) &&
                        !obj.bigCraftable.Value &&
                        obj.canBeGrabbed.Value)
                    {
                        list.Add((location, pair.Key, 1, false));
                    }
                }
            }
            return list;
        }
        public static List<(GameLocation loc, Vector2 tile, SObject item, bool fromProducer)> ScanForageableSources()
        {
            var list = new List<(GameLocation, Vector2, SObject, bool)>();

            foreach (var location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs.ToList())
                {
                    if (pair.Value is not SObject obj)
                        continue;

                    if (!obj.bigCraftable.Value && obj.canBeGrabbed.Value && obj.IsSpawnedObject)
                    {
                        list.Add((location, pair.Key, obj, false));
                    }

                    else if (obj.bigCraftable.Value &&
                             (obj.Name == "Mushroom Box" || obj.Name == "Mushroom Log") &&
                             obj.heldObject.Value is SObject held)
                    {
                        list.Add((location, pair.Key, held, true));
                    }
                }
            }
            foreach (var location in GetAllLocations())
            {
                foreach (var pair in location.terrainFeatures.Pairs)
                {
                    if (pair.Value is Tree tree && tree.hasMoss.Value)
                    {
                        var moss = new SObject("815", 1);
                        list.Add((location, pair.Key, moss, false));
                    }
                }
            }
            return list;
        }
        public static IEnumerable<GameLocation> GetAllLocations()
        {
            foreach (var loc in Game1.locations)
            {
                yield return loc;

                if (loc is Farm farm)
                {
                    foreach (var building in farm.buildings)
                    {
                        if (building.indoors.Value != null)
                            yield return building.indoors.Value;
                    }
                }
            }
        }
        public static bool IsStableLocation(GameLocation loc)
        {
            return loc is not MineShaft
                && loc is not VolcanoDungeon
                && !loc.IsTemporary;
        }
        public static void DrawSquarePlacementOverlay(SpriteBatch spriteBatch)
        {
            Farmer farmer = Game1.player;
            GameLocation loc = farmer.currentLocation;

            int length = Config.RectangleWidth;
            int width = Config.RectangleLength;

            var tiles = SortTileSquare();

            foreach (var tile in tiles)
            {
                var previewObj = new IndoorPot(tile);

                bool placeable = CanPlaceObjectHere(loc, tile, previewObj);

                Color color = placeable
                    ? new Color(60, 255, 60, 140)
                    : new Color(255, 60, 60, 140);

                DrawTileHighlight(spriteBatch, tile, color);
            }
        }
        private static void DrawTileHighlight(SpriteBatch spriteBatch, Vector2 tile, Color color)
        {
            Microsoft.Xna.Framework.Rectangle rect = new Microsoft.Xna.Framework.Rectangle(
                (int)(tile.X * Game1.tileSize) - Game1.viewport.X,
                (int)(tile.Y * Game1.tileSize) - Game1.viewport.Y,
                Game1.tileSize,
                Game1.tileSize
            );

            spriteBatch.Draw(Game1.staminaRect, rect, color);
        }
        public static IEnumerable<GameLocation> GetAllLocations_ForDirt()
        {
            HashSet<GameLocation> result = new();

            foreach (var loc in Game1.locations)
                result.Add(loc);

            var farmhouse = Game1.getLocationFromName("FarmHouse");
            if (farmhouse != null)
                result.Add(farmhouse);

            for (int i = 0; i < 10; i++)
            {
                var cabin = Game1.getLocationFromName($"Cabin{i}");
                if (cabin != null)
                    result.Add(cabin);
            }

            if (Game1.getLocationFromName("Farm") is Farm farm)
            {
                foreach (var building in farm.buildings)
                {
                    if (building.indoors.Value != null)
                        result.Add(building.indoors.Value);
                }
            }

            return result;
        }
        public static bool CanPlaceObjectHere(GameLocation loc, Vector2 tile, SObject obj)
        {
            if (!loc.isTileOnMap(tile))
                return false;

            if (loc.objects.ContainsKey(tile))
                return false;

            return obj.canBePlacedHere(loc, tile);
        }
        public static bool IsSeedAllowedHere(string seedId, GameLocation loc, Vector2 tile)
        {
            if (loc.objects.TryGetValue(tile, out var obj) &&
                obj is IndoorPot pot &&
                pot.hoeDirt.Value is HoeDirt potDirt)
            {
                if (!loc.IsOutdoors)
                    return seedId != "RiceShoot";
            }

            if (loc.Name.StartsWith("Island"))
                return true;

            if (seedId == "MixedSeeds" || seedId == "770" || seedId.EndsWith("770"))
            {
                if (loc.IsGreenhouse)
                    return true;

                if (!loc.IsFarm || !loc.IsOutdoors)
                    return false;

                return Game1.currentSeason is "spring" or "summer" or "fall";
            }

            if (!Game1.cropData.TryGetValue(seedId, out var data))
                return false;

            if (loc.IsGreenhouse)
                return true;

            if (data.Seasons == null || data.Seasons.Count == 0)
                return false;

            if (!Enum.TryParse<Season>(Game1.currentSeason, true, out var currentSeasonEnum))
                return false;

            return data.Seasons.Contains(currentSeasonEnum);
        }
        public static SObject GetNextValidSeed(GameLocation loc, Vector2 tile)
        {
            foreach (var item in Game1.player.Items.OfType<SObject>())
            {
                if (item.Category != SObject.SeedsCategory)
                    continue;

                if (IsSeedAllowedHere(item.ItemId, loc, tile))
                    return item;
            }

            return null;
        }
        public static int CountSeedsAllowedHere(GameLocation loc, Vector2 tile)
        {
            return Game1.player.Items
                .OfType<SObject>()
                .Where(i => i.Category == SObject.SeedsCategory &&
                            IsSeedAllowedHere(i.ItemId, loc, tile))
                .Sum(i => i.Stack);
        }
        public static IEnumerable<Vector2> GetEmptyTroughTiles(AnimalHouse house)
        {
            var layer = house.map.GetLayer("Buildings");
            if (layer == null)
                yield break;

            int width = layer.LayerWidth;
            int height = layer.LayerHeight;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = layer.Tiles[x, y];
                    if (tile != null && tile.Properties.ContainsKey("Trough"))
                    {
                        Vector2 pos = new Vector2(x, y);

                        if (!house.objects.ContainsKey(pos))
                            yield return pos;
                    }
                }
            }
        }
    }
}
