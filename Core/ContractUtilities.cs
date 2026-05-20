using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using xTile.Dimensions;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;
using XnaRectangle = Microsoft.Xna.Framework.Rectangle;

namespace CommunityContracts.Core
{
    public static class ContractUtilities
    {
        public class ContractsDelivery
        {
            public List<Item> Items { get; set; } = new();
            public long RecipientID { get; set; }
        }
        public static int SafeMultiplier(int value)
        {
            return Math.Max(1, value);
        }
        public static int GetContractPercent(string ProductType)
        {
            return Config.NPCContractPercents.TryGetValue(ProductType, out var value)
                ? value
                : Config.NPCContractPercents["Basic"];
        }
        public static string GetItemTypeLabel(string ItemType)
        {
            return ItemTypeLabels.TryGetValue(ItemType, out var value)
                ? value
                : ItemTypeLabels["Weeds"];
        }
        public static int GetQuality(int NPCLevel)
        {
            return NPCLevel switch
            {
                >= 10 => 4,
                >= 7 => 2,
                >= 3 => 1,
                _ => 0
            };
        }
        public static float GetQualityMultiplier(int ProductQuality)
        {
            return ProductQuality switch
            {
                0 => 1.0f,
                1 => 1.25f,
                2 => 1.5f,
                4 => 2.0f,
                _ => 1.0f
            };
        }
        public static float GetHoneyMultiplier(int Flower)
        {
            return Flower switch
            {
                376 => 3.8f,
                418 => 1.0f,
                421 => 2.6f,
                591 => 1.6f,
                593 => 2.8f,
                595 => 6.8f,
                597 => 2.0f,
                _ => 1.0f
            };
        }
        public static float GetCut(float contractorCut)
        {
            if (contractorCut < 1000)
                contractorCut = (contractorCut / 10) * 10;
            else if (contractorCut < 5000)
                contractorCut = (contractorCut / 50) * 50;
            else if (contractorCut < 20000)
                contractorCut = (contractorCut / 100) * 100;
            else if (contractorCut < 100000)
                contractorCut = (contractorCut / 500) * 500;
            else
                contractorCut = (contractorCut / 1000) * 1000;

            return contractorCut;
        }
        public static int GetTotalValue(int contractorCut)
        {
            if (contractorCut < 1000)
                contractorCut = (contractorCut / 10) * 10;
            else if (contractorCut < 5000)
                contractorCut = (contractorCut / 50) * 50;
            else if (contractorCut < 20000)
                contractorCut = (contractorCut / 100) * 100;
            else if (contractorCut < 100000)
                contractorCut = (contractorCut / 500) * 500;
            else
                contractorCut = (contractorCut / 1000) * 1000;

            return contractorCut;
        }
        public static string GetQualityName(int Quality)
        {
            return Quality switch
            {
                >= 4 => T("QualityIridium"),
                >= 2 => T("QualityGold"),
                >= 1 => T("QualitySilver"),
                _ => T("QualityNormal")
            };
        }
        public static int GetSeasonIndex(string Season)
        {
            return Season switch
            {
                "spring" => 0,
                "summer" => 1,
                "fall" => 2,
                "winter" => 3,
                _ => 0
            };
        }
        public static int CountProcessors(string processorName)
        {
            var allLocations = new List<GameLocation>(Game1.locations);

            if (Game1.getLocationFromName("Farm") is Farm farm)
            {
                foreach (var building in farm.buildings)
                {
                    if (building.indoors?.Value != null)
                        allLocations.Add(building.indoors.Value);
                }
            }

            return allLocations
                .SelectMany(loc => loc.objects.Values)
                .Count(obj => obj != null && obj.Name == processorName);
        }
        public static string GetItemName(string id)
        {
            return ItemRegistry.GetData(id)?.DisplayName
                ?? T("UnknownItem", new { id });
        }
        public static void DeliverContractsItems(List<ContractsDelivery> deliveries, ModConfig config)
        {
            if (deliveries.Count == 0)
                return;

            foreach (var delivery in deliveries)
            {
                var farmer = Game1.getAllFarmers().FirstOrDefault(f => f.UniqueMultiplayerID == delivery.RecipientID);
                if (farmer == null)
                    continue;

                GameLocation location = Game1.getLocationFromName(config.DropLocationName);

                if (location == null)
                {
                    location = Game1.getLocationFromName("Farm");
                }

                Vector2 dropTile = new Vector2(config.DropTileX, config.DropTileY);

                if (location is Farm farmLocation &&
                    !farmLocation.isTileLocationOpen(new Location((int)dropTile.X, (int)dropTile.Y)))
                {
                    Instance.Monitor.Log(T("DropTileBlocked", new { tile = dropTile }), LogLevel.Trace);
                    dropTile = new Vector2(59, 15);
                }

                if (!location.objects.TryGetValue(dropTile, out var obj) || obj is not Chest chest)
                {
                    chest = new Chest(true);
                    location.objects[dropTile] = chest;
                    chest.name = T("ContractDeliveryChest");

                    if (config.ChestColors.TryGetValue(config.DeliveryChestColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                        chest.modData["CommunityContracts/DeliveryColor"] = config.DeliveryChestColor;
                    }
                }
                else if (obj is Chest existingChest)
                {
                    chest = existingChest;

                    if (chest.modData.TryGetValue("CommunityContracts/DeliveryColor", out var savedColor) &&
                        config.ChestColors.TryGetValue(savedColor, out var tint))
                    {
                        chest.playerChoiceColor.Value = tint;
                    }
                }

                var chestItems = chest.GetItemsForPlayer(farmer.UniqueMultiplayerID);
                foreach (var item in delivery.Items)
                    chestItems.Add(item);

                if (farmer.IsLocalPlayer)
                {
                    string summary = string.Join(", ", delivery.Items.Select(i => $"{i.Stack} {i.DisplayName}"));
                    Game1.showGlobalMessage( T("ShipmentDelivered", new { summary }));
                }
            }
            deliveries.Clear();
        }
        public static void ShipContractItems(List<ContractsDelivery> deliveries, ModConfig config)
        {
            if (deliveries.Count == 0)
                return;

            foreach (var delivery in deliveries)
            {
                var farmer = Game1.getAllFarmers()
                    .FirstOrDefault(f => f.UniqueMultiplayerID == delivery.RecipientID);

                if (farmer == null)
                    continue;

                var shippingBin = Game1.getFarm().getShippingBin(farmer);

                foreach (var item in delivery.Items)
                {
                    shippingBin.Add(item);
                }

                if (farmer.IsLocalPlayer)
                {
                    string summary = string.Join(", ", delivery.Items.Select(i => $"{i.Stack} {i.DisplayName}"));
                    Game1.showGlobalMessage(T("ShipmentShipped", new { summary }));
                }
            }

            deliveries.Clear();
        }

        public static int GetRecycleQuantity(int itemID) => itemID switch
        {
            93 => 3,
            380 => 3,
            382 => 5,
            388 => 15,
            390 => 10,
            _ => 1
        };
        public static int UpdateNPCLevel(string NPCName)
        {
            return (int)((Game1.player.friendshipData.TryGetValue(NPCName, out var data) ? data.Points : 0) / 224);
        }
        public static void ApplyProductionTimeReduction(ModConfig Config, IMonitor Monitor)
        {
            if (!Config.EnableProcessTimeReduction)
                return;

            int friendshipPoints = Game1.player.friendshipData.Values.Sum(f => f.Points);
            int totalSkillLevels = Game1.player.FarmingLevel + Game1.player.ForagingLevel +
                                   Game1.player.FishingLevel + Game1.player.MiningLevel +
                                   Game1.player.CombatLevel;

            const int MaxFriendship = 85000;
            const int MaxSkills = 50;
            const int MaxTime = 8640;
            const int MinTime = 10;

            float pFriend = Math.Clamp((float)friendshipPoints / MaxFriendship, 0f, 1f);
            float pSkill = Math.Clamp((float)totalSkillLevels / MaxSkills, 0f, 1f);
            float pRaw = (pFriend + pSkill) * 0.5f;
            float pPrime = (float)Math.Pow(pRaw, 0.85f);

            int effectiveTime = (int)(MinTime + (MaxTime - MinTime) * (1f - pPrime));
            float scale = (float)effectiveTime / MaxTime;

            Monitor.Log($"[Production Accelerator] Scale: {scale:F2}", LogLevel.Info);

            foreach (GameLocation location in Game1.locations)
            {
                foreach (var pair in location.Objects.Pairs)
                {
                    if (pair.Value is SObject obj && obj.bigCraftable.Value && obj.minutesUntilReady.Value > 0)
                    {
                        int currentTime = obj.minutesUntilReady.Value;
                        int newTime = Math.Max(MinTime, (int)(currentTime * scale));
                        obj.minutesUntilReady.Value = newTime;

                        Monitor.Log($"[Production Accelerator] {obj.Name} at {location.Name} tile {pair.Key}: {currentTime} → {newTime}", LogLevel.Trace);
                    }
                }
            }
        }
        public static void DrawDeliveryLocationHighlight(
            SpriteBatch spriteBatch,
            string dropLocationName,
            ModConfig config,
            Func<string, string> T
        )
        {
            if (Game1.currentLocation?.Name != dropLocationName)
                return;

            Vector2 tile = new Vector2(config.DropTileX, config.DropTileY);

            XnaRectangle rect = new XnaRectangle(
                (int)(tile.X * Game1.tileSize) - Game1.viewport.X,
                (int)(tile.Y * Game1.tileSize) - Game1.viewport.Y,
                Game1.tileSize,
                Game1.tileSize
            );

            Color highlightColor = config.HighlightColors.TryGetValue(
                config.HighlightColor,
                out var c
            ) ? c : Color.Yellow * 0.75f;

            spriteBatch.Draw(Game1.staminaRect, rect, highlightColor);

            SpriteFont font = Game1.smallFont;
            string[] lines = { T("DeliveryLabel"), T("LocationLabel") };

            Color fontColor = config.FontColors.TryGetValue(
                config.FontColor,
                out var colr
            ) ? colr : Color.Black;

            for (int i = 0; i < lines.Length; i++)
            {
                Vector2 lineSize = font.MeasureString(lines[i]);
                Vector2 linePos = new Vector2(
                    rect.X,
                    rect.Y - lineSize.Y * (lines.Length - i) - 8
                );

                spriteBatch.DrawString(font, lines[i], linePos, fontColor);
            }
        }

        public static Dictionary<string, string> OreToBarMap = new()
        {
            { "378", "334" },
            { "380", "335" },
            { "384", "336" },
            { "386", "337" },
            { "909", "910" },
        };

        public static Dictionary<string, string> RawToProductMap = new()
        {
            { "436", "426" }, //Goat Milk
            { "438", "426" }, //L Goat Milk
            { "184", "424" }, // Milk
            { "186", "424" }, //L Milk
            { "176", "306" }, //Egg
	        { "180", "306" }, //Egg
	        { "174", "306" }, //L Egg
	        { "182", "306" }, //L Egg
	        { "289", "306" }, //Ostrich Egg
	        { "928", "306" }, //Golden Egg
	        { "107", "807" }, //Dinosaur Egg
	        { "305", "308" }, //Void Egg
	        { "442", "307" }, //Duck Egg
	        { "440", "428" }, //Wool
	        { "430", "432" }, //Truffle
        };

        public static Dictionary<string, string> ItemTypeLabels = new()
        {
            { "Crab Pots", T("ItemTypeCrabPots") },
            { "Crops", T("ItemTypeCrops") },
            { "Forageables", T("ItemTypeForageables") },
            { "Hardwood", T("ItemTypeHardwood") },
            { "Honey", T("ItemTypeHoney") },
            { "Stone", T("ItemTypeStone") },
            { "Weeds", T("ItemTypeWeeds") },
            { "Wood", T("ItemTypeWood") },
            { "Tappers", T("ItemTypeTappers") },
            { "Till", T("ItemTypeTill") },
            { "Water", T("ItemTypeWater") },
            { "PlaceTappers", T("ItemTypePlaceTappers") },
            { "Fertilize", T("ItemTypeFertilize") },
            { "Seeds", T("ItemTypeSeeds") },
            { "InvisiblePots", T("ItemTypeInvisiblePots") },
            { "PlaceBeeHouse", T("ServicePlaceBeeHouse") }
        };

        public static Dictionary<ServiceId, string> ServiceTypeLabels = new()
        {
            { ServiceId.CrabPots, T("ItemTypeCrabPots") },
            { ServiceId.Crops, T("ItemTypeCrops") },
            { ServiceId.Forageables, T("ItemTypeForageables") },
            { ServiceId.Hardwood, T("ItemTypeHardwood") },
            { ServiceId.Honey, T("ItemTypeHoney") },
            { ServiceId.Stone, T("ItemTypeStone") },
            { ServiceId.Weeds, T("ItemTypeWeeds") },
            { ServiceId.Wood, T("ItemTypeWood") },
            { ServiceId.Tappers, T("ItemTypeTappers") },
            { ServiceId.Till, T("ItemTypeTill") },
            { ServiceId.Water, T("ItemTypeWater") },
            { ServiceId.PlaceTappers, T("ItemTypePlaceTappers") },
            { ServiceId.Fertilize, T("ItemTypeFertilize") },
            { ServiceId.Seeds, T("ItemTypeSeeds") },
            { ServiceId.PlaceInvisiblePots, T("ItemTypeInvisiblePots") },
            { ServiceId.PlaceBeeHouse, T("ServicePlaceBeeHouse") }
        };
    }
}
