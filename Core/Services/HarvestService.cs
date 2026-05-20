using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class HarvestService
    {
        public static ModConfig config;
        private IMonitor monitor;

        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string harvestingnpcName;
        public int cropsHarvested = 0;

        public readonly CollectionServiceManager manager;
        public HarvestService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }

        public void OfferHarvestingService(IMonitor monitor, string HarvestingnpcName)
        {
            this.monitor = monitor;
            this.harvestingnpcName = HarvestingnpcName;
            var candidateTiles = new List<(GameLocation loc, Vector2 tile)>();
            int npcLevel = UpdateNPCLevel(this.harvestingnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / (SafeMultiplier(npcLevel + farmerSkill));
            var npc = Game1.getCharacterFromName(this.harvestingnpcName);
            this.friendshipPointsEarned = 0;
            this.cropsHarvested = 0;
            this.totalFeesPaid = 0;
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Crops)
               ? SpecialtyNames[ServiceId.Crops]
               : T("ServiceWeeds");
            int friendshipCounter = 0;

            var visited = new HashSet<(GameLocation loc, Vector2 tile)>();

            List<GameLocation> allLocations = new();

            foreach (var loc in Game1.locations)
            {
                if (loc != null)
                    allLocations.Add(loc);
            }

            foreach (var building in Game1.getFarm().buildings)
            {
                var indoors = building.indoors.Value;
                if (indoors != null)
                    allLocations.Add(indoors);
            }

            foreach (var loc in allLocations)
            {
                int width = loc.Map.Layers[0].LayerWidth;
                int height = loc.Map.Layers[0].LayerHeight;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector2 tile = new Vector2(x, y);

                        if (IsHarvestableTile(loc, tile))
                        {
                            var key = (loc, tile);

                            if (!visited.Contains(key))
                            {
                                visited.Add(key);
                                candidateTiles.Add(key);
                            }
                        }
                    }
                }
            }

            if (candidateTiles.Count == 0)
            {
                Game1.showGlobalMessage(T("NoCropsReady"));
                return;
            }

            int feePerTile = Config.SeviceContractFees[ServiceId.Crops];
            int maxAffordable = Game1.player.Money / feePerTile;

            int tilesToHarvest = Math.Min(candidateTiles.Count, maxAffordable);
            int totalFee = tilesToHarvest * feePerTile;

            string dialogText =
                T("ServiceOffer", new { npc = this.harvestingnpcName, quantity = tilesToHarvest, item = serviceLabels }) + "\n\n" +
                T("ServiceFeeItem", new { Fee = totalFee, feePerItem = feePerTile }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new[]
                {
                    new Response("Yes", T("ResponseYesDeliver")),
                    new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer != "Yes")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                        return;
                    }

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.harvestingnpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();

                    for (int i = 0; i < tilesToHarvest; i++)
                    {
                        var (loc, tile) = candidateTiles[i];

                        if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                            feature is HoeDirt dirt && dirt.crop != null)
                        {
                            var crop = dirt.crop;

                            if (!TryChargeFeeOrStop(feePerTile, this.harvestingnpcName, itemMap, monitor, Config))
                            {
                                return;
                            }

                            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 50);
                            this.totalFeesPaid += feePerTile;
                            this.cropsHarvested++;

                            string seedId = crop.netSeedIndex.Value;
                            if (string.IsNullOrEmpty(seedId) || !Game1.cropData.TryGetValue(seedId, out var data))
                                continue;

                            string id = crop.indexOfHarvest.Value;
                            int amount = GetVirtualCropYield(crop, Game1.player);

                            var key = (id, quality);
                            if (!itemMap.TryGetValue(key, out var stacked))
                            {
                                stacked = new SObject(id, amount) { Quality = quality };
                                itemMap[key] = stacked;
                            }
                            else
                            {
                                stacked.Stack += amount;
                            }

                            if (stacked.Stack >= 999)
                            {
                                var deliveryChunk = new List<Item> { stacked.getOne() };
                                deliveryChunk[0].Stack = 999;

                                stacked.Stack -= 999;

                                DeliverContractsItems(new List<ContractsDelivery>
                                {
                                    new ContractsDelivery
                                    {
                                        Items = deliveryChunk,
                                        RecipientID = Game1.player.UniqueMultiplayerID
                                    }
                                }, Config);
                            }

                            if (data.RegrowDays >= 0)
                            {
                                int regrowPhase = crop.phaseDays.Count - 2;
                                crop.currentPhase.Value = regrowPhase;
                                crop.dayOfCurrentPhase.Value = 0;
                                crop.fullyGrown.Value = false;
                                crop.updateDrawMath(tile);
                                dirt.crop = crop;
                            }
                            else
                            {
                                dirt.crop = null;
                            }
                        }

                        if (loc.objects.TryGetValue(tile, out var obj) && obj is IndoorPot pot)
                        {
                            var potdirt = pot.hoeDirt?.Value;
                            if (potdirt?.crop == null)
                                continue;

                            var crop = potdirt.crop;

                            if (!TryChargeFeeOrStop(feePerTile, this.harvestingnpcName, itemMap, monitor, Config))
                            {
                                return;
                            }

                            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 50);
                            this.totalFeesPaid += feePerTile;
                            this.cropsHarvested++;

                            string seedId = crop.netSeedIndex.Value;
                            if (string.IsNullOrEmpty(seedId) || !Game1.cropData.TryGetValue(seedId, out var data))
                                continue;

                            string id = crop.indexOfHarvest.Value;
                            int amount = GetVirtualCropYield(crop, Game1.player);

                            var key = (id, quality);
                            if (!itemMap.TryGetValue(key, out var stacked))
                            {
                                stacked = new SObject(id, amount) { Quality = quality };
                                itemMap[key] = stacked;
                            }
                            else
                            {
                                stacked.Stack += amount;
                            }

                            if (stacked.Stack >= 999)
                            {
                                var deliveryChunk = new List<Item> { stacked.getOne() };
                                deliveryChunk[0].Stack = 999;
                                stacked.Stack -= 999;

                                DeliverContractsItems(new List<ContractsDelivery>
                                {
                                    new ContractsDelivery
                                    {
                                        Items = deliveryChunk,
                                        RecipientID = Game1.player.UniqueMultiplayerID
                                    }
                                }, Config);
                            }

                            if (data.RegrowDays >= 0)
                            {
                                int regrowPhase = crop.phaseDays.Count - 2;
                                crop.currentPhase.Value = regrowPhase;
                                crop.dayOfCurrentPhase.Value = 0;
                                crop.fullyGrown.Value = false;
                                crop.updateDrawMath(tile);
                                potdirt.crop = crop;
                            }
                            else
                            {
                                potdirt.crop = null;
                            }
                        }

                        if (!Game1.newDay)
                            await Task.Delay(delay);
                    }

                    var finalItems = itemMap.Values
                        .Where(i => i.Stack > 0)
                        .Cast<Item>()
                        .ToList();

                    if (finalItems.Count > 0)
                    {
                        DeliverContractsItems(new List<ContractsDelivery>
                        {
                            new ContractsDelivery
                            {
                                Items = finalItems,
                                RecipientID = Game1.player.UniqueMultiplayerID
                            }
                        }, Config);
                    }

                    if (this.friendshipPointsEarned > 0)
                    {
                        var npc = Game1.getCharacterFromName(this.harvestingnpcName);
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.harvestingnpcName, points = this.friendshipPointsEarned }));
                    }
                });
        }
    }
}
