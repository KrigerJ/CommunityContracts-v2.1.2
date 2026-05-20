
using StardewModdingAPI;
using StardewValley;
using System.Text;
using static ModEntry;
using SObject = StardewValley.Object;
using static CommunityContracts.Core.ContractUtilities;

namespace CommunityContracts.Core.NPC
{
    public class AlexProfile
    {
        public int SelectedItemID { get; set; }
        public SObject BaseItem1 { get; set; }
        public int ItemPrice { get; set; }
        public int Quantity { get; set; }
        public int Quality { get; set; } = 0;
        public float QualityMultiplier { get; set; }
        public string QualityName { get; set; }
        public float EstimatedValue { get; set; }
        public float BaseProductMultiplier { get; set; }
        public float ProductOneMultiplier { get; set; }
        public float ProductTwoMultiplier { get; set; }
        public int ProcessorsOperated { get; set; } = 0;
        public int PrimaryProcessors { get; set; } = 0;
        public int SecondaryProcessors { get; set; } = 0;
        public int NPCLevel { get; set; } = 0;
        public string CharacterName { get; set; } = "Alex";
        public int FarmerSkillLevel { get; set; } = 0;
        public string PreparedItemName { get; set; }
        public int SeasonIndex { get; set; }

        private readonly IMonitor Monitor;
        public async Task<List<Item>> GenerateProductShipmentWithDelay(IMonitor monitor)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();

            if (BaseItem1 == null)
            {
                monitor.Log(T("FailedBaseItem", new { id = SelectedItemID }), LogLevel.Warn);
                return itemMap.Values.Cast<Item>().ToList();
            }

            if (ProcessorsOperated > 0)
            {
                for (int i = 0; i < ProcessorsOperated; i++)
                {
                    var MetalBars = ItemRegistry.Create($"{SelectedItemID}") as SObject;
                    if (MetalBars != null)
                    {
                        var key = (MetalBars.ParentSheetIndex, 1);
                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{SelectedItemID}", 1);
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += 1;
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

                            Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                        }

                        if (!Game1.newDay)
                        {
                            await Task.Delay(Config.CollectionDelay / SafeMultiplier(NPCLevel + FarmerSkillLevel));
                        }
                        else
                        {

                        }
                    }
                }
            }

            foreach (var pair in AlexContract.GetMinerGiftQuantities())
            {
                for (int i = 0; i < pair.Value; i++)
                {
                    var raw = ItemRegistry.Create($"{pair.Key}") as SObject;
                    if (raw != null)
                    {
                        var key = (raw.ParentSheetIndex, 1);
                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject($"{raw.ParentSheetIndex}", 1);
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += 1;
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

                            Game1.showGlobalMessage(T("PartialDelivery2", new { item = deliveryChunk[0].DisplayName }));
                        }

                        if (!Game1.newDay)
                        {
                            await Task.Delay(Config.CollectionDelay / SafeMultiplier(NPCLevel + FarmerSkillLevel));
                        }
                        else
                        {

                        }
                    }
                }
            }

            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            return finalItems;
        }
        public static class AlexContract
        {
            private static AlexProfile profile = new AlexProfile();
            public static async void OfferDetailedContract()
            {
                profile.EstimatedValue = EstimateContractValue();

                int ContractPercent = GetContractPercent("Custom");
                float estimatedValue = profile.EstimatedValue;
                float contractorCut = estimatedValue * ContractPercent / 100.0f;
                float FriendshipAdd = ContractPercent / 10;
                string processingLine = "";

                contractorCut = GetCut(contractorCut);

                if (profile.ProcessorsOperated > 0)
                {
                    processingLine =
                        T("AlexRefinedItems", new { count = profile.ProcessorsOperated, item = profile.PreparedItemName }) + "\n\n" +
                        T("AlexPackShipment");
                }

                string dialogText =
                    T("AlexOfferContractOre", new { npc = profile.CharacterName }) + "\n\n" +
                    processingLine + "\n\n" +
                    T("ContractEstimatedValue", new { value = (int)estimatedValue }) + "\n\n" +
                    T("ContractPrepayAmount", new { percent = ContractPercent, cut = (int)contractorCut }) + "\n\n" +
                    T("ContractAcceptPrompt");

                Game1.currentLocation.createQuestionDialogue(
                    dialogText,
                    new Response[]
                    {
                        new Response("Yes", T("ResponseYes")),
                        new Response("No", T("ResponseNo"))
                    },
                    async (farmer, answer) =>
                    {
                        if (answer == "Yes")
                        {
                            if (Game1.player.Money < (int)contractorCut)
                            {
                                Game1.showGlobalMessage(T("NotEnoughGold2", new { npc = profile.CharacterName }));
                                return;
                            }

                            Game1.player.Money -= (int)contractorCut;

                            var builder = new StringBuilder();
                            builder.AppendLine(T("ContractAccepted", new { amount = (int)contractorCut, npc = profile.CharacterName }));

                            StardewValley.NPC Alex = Game1.getCharacterFromName(profile.CharacterName);
                            if (Alex != null)
                            {
                                Game1.player.changeFriendship((int)FriendshipAdd, Alex);
                                builder.AppendLine(T("FriendshipIncreased", new { npc = profile.CharacterName, points = (int)FriendshipAdd }));
                            }
                            var shipment = await profile.GenerateProductShipmentWithDelay(ModMonitor);

                            DeliverContractsItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = shipment,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);
                        }
                        else if (answer == "No")
                        {
                            Game1.showGlobalMessage(T("MaybeLater"));
                        }
                    });
            }
            public static float EstimateContractValue()
            {
                int totalValue = 0;
                profile.ItemPrice = profile.BaseItem1?.Price ?? 50;
                profile.QualityMultiplier = GetQualityMultiplier(profile.Quality);
                profile.ProductOneMultiplier = 4.0f + (profile.NPCLevel * 0.50f);
                profile.ProductTwoMultiplier = 2.5f + (profile.NPCLevel * 0.25f);
                profile.BaseProductMultiplier = 1.0f + (profile.NPCLevel * 0.20f);

                foreach (var pair in GetMinerGiftQuantities())
                {
                    var Item = ItemRegistry.Create($"(O){pair.Key}") as SObject;
                    totalValue += Item.Price * pair.Value;
                }

                totalValue += (profile.ItemPrice * profile.ProcessorsOperated);

                totalValue = GetTotalValue(totalValue);

                return totalValue;
            }
            public static Dictionary<int, int> GetMinerGiftQuantities()
            {
                int mineDepth = Game1.player.deepestMineLevel;
                int CopperQty = Math.Max(0, mineDepth);
                int IronQty = Math.Max(0, mineDepth - 40);
                int GoldQty = Math.Max(0, mineDepth - 80);
                int IridiumQty = Math.Max(0, mineDepth - 105);

                int CoalQty = (mineDepth);

                return new Dictionary<int, int>
                {
                    { 378, CopperQty },
                    { 380, IronQty },
                    { 382, CoalQty },
                    { 384, GoldQty },
                    { 386, IridiumQty }
                };
            }
            public static void AlexIntroduction()
            {
                profile.NPCLevel = UpdateNPCLevel(profile.CharacterName);
                profile.FarmerSkillLevel = Game1.player.miningLevel.Value;
                profile.PrimaryProcessors = CountProcessors("Furnace");
                profile.SecondaryProcessors = CountProcessors("Heavy Furnace");
                profile.ProcessorsOperated = profile.PrimaryProcessors + (profile.SecondaryProcessors * 5);
                profile.Quality = GetQuality(profile.NPCLevel);
                profile.QualityName = GetQualityName(profile.Quality);
                profile.SeasonIndex = GetSeasonIndex(Game1.currentSeason);

                int[][] SeasonalCollect = new int[][]
                {
                    new int[] { 334, 335, 336 },
	                new int[] { 334, 335, 336 },
	                new int[] { 334, 335, 336 },
	                new int[] { 334, 335, 336 }
 			    };

                var seasonalOptions = SeasonalCollect[profile.SeasonIndex]
                    .Concat(SeasonalCollect[profile.SeasonIndex])
                    .ToList();

                Random rng = new Random();
                profile.SelectedItemID = seasonalOptions[rng.Next(seasonalOptions.Count)];
                profile.BaseItem1 = ItemRegistry.Create(profile.SelectedItemID.ToString()) as SObject;
                profile.PreparedItemName = GetItemName(profile.SelectedItemID.ToString());
                profile.Quantity = SafeMultiplier(profile.FarmerSkillLevel) * SafeMultiplier(profile.NPCLevel);

                string dialogText =
                    T("AlexAskContractOre", new { npc = profile.CharacterName });

                var responses = new List<Response>
                {
                    new Response("Accept", T("ResponseAccept")),
                    new Response("Decline", T("ResponseDecline")),
                    new Response("Stats", T("ResponseStats", new { npc = profile.CharacterName }))
                };

                Game1.currentLocation.createQuestionDialogue(
                    dialogText,
                    responses.ToArray(),
                    (farmer, answer) =>
                    {
                        if (answer == "Stats")
                        {
                            Game1.delayedActions.Add(new DelayedAction(100, () =>
                            {
                                AlexStats();
                            }));
                        }

                        if (answer == "Accept")
                        {
                            Game1.delayedActions.Add(new DelayedAction(100, () =>
                            {
                                OfferDetailedContract();
                            }));
                        }

                        else if (answer == "Decline")
                        {
                            Game1.showGlobalMessage(T("MaybeLater"));
                        }
                    }
                );
            }
            public static void AlexStats()
            {
                int currentFriendship = Game1.player.friendshipData.TryGetValue(profile.CharacterName, out var data) ? data.Points : 0;

                foreach (var pair in AlexContract.GetMinerGiftQuantities())
                {
                    ModMonitor.Log($"{pair.Key}: {pair.Value}.", LogLevel.Debug);
                    ModMonitor.Log($"Mine Depth: {Game1.player.deepestMineLevel}.", LogLevel.Debug);
                    ModMonitor.Log($"Scull Depth: {Game1.player.stats.Get("skullCavernLevel")}.", LogLevel.Debug);
                }

                string dialogText =
                    T("AlexMinerInfo", new { npc = profile.CharacterName, level = profile.NPCLevel }) + "\n\n" +
                    T("AlexFurnaceInfo", new { npc = profile.CharacterName, primary = profile.PrimaryProcessors, secondary = profile.SecondaryProcessors }) + "\n\n" +
                    T("FriendshipLine", new { npc = profile.CharacterName, points = currentFriendship });

                Game1.currentLocation.createQuestionDialogue(
                    dialogText,
                    new Response[]
                    {
                        new Response("OK", T("ResponseOK"))
                    },
                    (farmer, answer) =>
                    {
                        Game1.delayedActions.Add(new DelayedAction(100, () =>
                        {
                            if (answer == "OK")
                                AlexIntroduction();
                        }));
                    }
                );
            }
        }
    }
}