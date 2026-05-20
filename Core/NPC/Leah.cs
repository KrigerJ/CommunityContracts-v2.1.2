
using StardewModdingAPI;
using StardewValley;
using System.Text;
using static ModEntry;
using SObject = StardewValley.Object;
using static CommunityContracts.Core.ContractUtilities;

namespace CommunityContracts.Core.NPC
{
    public class LeahProfile
    {
        public int SelectedItemID { get; set; }
        public SObject BaseItem { get; set; }
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
        public int NPCLevel { get; set; } = 0;
        public string CharacterName { get; set; } = "Leah";
        public int FarmerSkillLevel { get; set; }
        public string PreparedItemName { get; set; }
        public int SeasonIndex { get; set; }

        private readonly IMonitor Monitor;
        public async Task<List<Item>> GenerateProductShipmentWithDelay(IMonitor monitor)
        {
            var itemMap = new Dictionary<(int index, int quality), SObject>();

            if (BaseItem == null)
            {
                monitor.Log(T("FailedBaseItem", new { id = SelectedItemID }), LogLevel.Warn);
                return itemMap.Values.Cast<Item>().ToList();
            }

            if (BaseItem.Edibility < 15)
                BaseItem.Edibility = 15;

            if (ProcessorsOperated > 0)
            {
                for (int i = 0; i < ProcessorsOperated; i++)
                {
                    var ProcessedItem = ItemRegistry.Create("348") as SObject;
                    if (ProcessedItem != null)
                    {
                        var key = (ProcessedItem.ParentSheetIndex, ProcessedItem.Quality);
                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject("348", 1);
                            stacked.Quality = Quality;
                            stacked.Price = (int)(ItemPrice * ProductOneMultiplier * QualityMultiplier);
                            stacked.Edibility = BaseItem.Edibility * SafeMultiplier(NPCLevel);
                            stacked.Name = PreparedItemName;
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

                for (int i = 0; i < ProcessorsOperated; i++)
                {
                    var ProcessedItem = ItemRegistry.Create("350") as SObject;
                    if (ProcessedItem != null)
                    {
                        var key = (ProcessedItem.ParentSheetIndex, ProcessedItem.Quality);
                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject("350", 1);
                            stacked.Quality = Quality;
                            stacked.Price = (int)(ItemPrice * ProductOneMultiplier * QualityMultiplier);
                            stacked.Edibility = BaseItem.Edibility * SafeMultiplier(NPCLevel);
                            stacked.Name = PreparedItemName;
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

            for (int i = 0; i < Quantity; i++)
            {
                var UnRefined = BaseItem.getOne() as SObject;
                if (UnRefined != null)
                {
                    var key = (UnRefined.ParentSheetIndex, UnRefined.Quality);
                    if (!itemMap.TryGetValue(key, out var stacked))
                    {
                        stacked = new SObject($"{UnRefined.ParentSheetIndex}", 1);
                        stacked.Quality = Quality;

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

            var finalItems = itemMap.Values.Where(i => i.Stack > 0).Cast<Item>().ToList();
            return finalItems;
        }
        public static class LeahContract
        {
            private static LeahProfile profile = new LeahProfile();
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
                        T("LeahFermentedProducts", new { count = profile.ProcessorsOperated, quality = profile.QualityName, item = profile.PreparedItemName }) + "\n\n" +
                        T("LeahPackShipment");
                }

                string dialogText =
                    T("ContractOffer", new { npc = profile.CharacterName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName }) + "\n\n" +
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

                            StardewValley.NPC leah = Game1.getCharacterFromName(profile.CharacterName);
                            if (leah != null)
                            {
                                Game1.player.changeFriendship((int)FriendshipAdd, leah);
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
                profile.ItemPrice = profile.BaseItem?.Price ?? 50;
                profile.QualityMultiplier = GetQualityMultiplier(profile.Quality);
                profile.ProductOneMultiplier = 3.0f + (profile.NPCLevel * 0.50f);
                profile.ProductTwoMultiplier = 2.5f + (profile.NPCLevel * 0.50f);
                profile.BaseProductMultiplier = 1.0f + (profile.NPCLevel * 0.20f);

                totalValue += (int)((profile.ItemPrice * profile.ProductOneMultiplier * profile.QualityMultiplier * profile.ProcessorsOperated) + (profile.ItemPrice * profile.ProductTwoMultiplier * profile.QualityMultiplier * profile.ProcessorsOperated) + (profile.ItemPrice * profile.BaseProductMultiplier * profile.QualityMultiplier * profile.Quantity));

                totalValue = GetTotalValue(totalValue);

                return totalValue;
            }
            public static void LeahIntroduction()
            {
                profile.NPCLevel = UpdateNPCLevel(profile.CharacterName);
                profile.ProcessorsOperated = CountProcessors("Keg");
                profile.Quality = GetQuality(profile.NPCLevel);
                profile.QualityName = GetQualityName(profile.Quality);
                profile.SeasonIndex = GetSeasonIndex(Game1.currentSeason);
                profile.FarmerSkillLevel = Game1.player.foragingLevel.Value;

                int[][] SeasonalCollect = new int[][]
                {
                    new int[] { 16, 18, 22, 638 },
                    new int[] { 396, 398, 402, 398 },
                    new int[] { 404, 406, 408, 406 },
                    new int[] { 412, 414, 416, 414 }
                };

                var seasonalOptions = SeasonalCollect[profile.SeasonIndex]
                    .Concat(SeasonalCollect[profile.SeasonIndex])
                    .ToList();

                Random rng = new Random();
                profile.SelectedItemID = seasonalOptions[rng.Next(seasonalOptions.Count)];
                profile.BaseItem = ItemRegistry.Create(profile.SelectedItemID.ToString()) as SObject;
                profile.PreparedItemName = GetItemName(profile.SelectedItemID.ToString());
                profile.Quantity = SafeMultiplier(profile.FarmerSkillLevel) * SafeMultiplier(profile.NPCLevel);


                string dialogText =
                    T("LeahAskContractCollected", new { npc = profile.CharacterName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName });

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
                                LeahStats();
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
            public static void LeahStats()
            {
                int currentFriendship = Game1.player.friendshipData.TryGetValue(profile.CharacterName, out var data) ? data.Points : 0;

                string dialogText =
                    T("LeahCollectorInfo", new { npc = profile.CharacterName, level = profile.NPCLevel, quality = profile.QualityName }) + "\n\n" +
                    T("LeahKegInfo", new { npc = profile.CharacterName, count = profile.ProcessorsOperated }) + "\n\n" +
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
                                LeahIntroduction();
                        }));
                    }
                );
            }
        }
    }
}