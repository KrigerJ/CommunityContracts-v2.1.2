
using StardewModdingAPI;
using StardewValley;
using System.Text;
using static ModEntry;
using SObject = StardewValley.Object;
using static CommunityContracts.Core.ContractUtilities;

namespace CommunityContracts.Core.NPC
{
    public class NPCProfile
    {
        public int SelectedItemID { get; set; }
        public int RecycleItemID { get; set; }
        public SObject BaseItem { get; set; }
        public SObject RecycleItem { get; set; }
        public int ItemPrice { get; set; }
        public int Quantity { get; set; }
        public int RecycleQuantity { get; set; }
        public int Quality { get; set; } = 0;
        public float EstimatedValue { get; set; }
        public float QualityMultiplier { get; set; }
        public string QualityName { get; set; }
        public float BaseProductMultiplier { get; set; }
        public float ProductOneMultiplier { get; set; }
        public float ProductTwoMultiplier { get; set; }
        public int ProcessorsOperated { get; set; } = 0;
        public string NPCName { get; private set; }
        public int NPCLevel { get; set; } = 0;
        public int FarmerSkillLevel { get; set; }
        public string PreparedItemName { get; set; }
        public string RecycledItemName { get; set; }
        public int SeasonIndex { get; set; }

        private readonly IMonitor Monitor;
        public async Task<List<Item>> GenerateProductShipmentWithDelay(IMonitor monitor)
        {
            var itemMap = new Dictionary<(string id, int quality), SObject>();

            if (BaseItem == null)
            {
                monitor.Log(T("FailedBaseItem", new { id = SelectedItemID }), LogLevel.Warn);
                return itemMap.Values.Cast<Item>().ToList();
            }

            if (RecycleQuantity > 0)
            {
                for (int i = 0; i < RecycleQuantity; i++)
                {
                    if (RecycleItem != null)
                    {
                        var key = (RecycleItem.ItemId, 1);
                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = RecycleItem;
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
                    var key = (UnRefined.ItemId, UnRefined.Quality);
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
        public static class NPCContract
        {
            private static NPCProfile profile = new NPCProfile();
            public static async void OfferDetailedContract()
            {
                profile.EstimatedValue = EstimateContractValue();

                int ContractPercent = GetContractPercent("Basic");
                float estimatedValue = profile.EstimatedValue;
                float ContractCut = estimatedValue * ContractPercent / 100.0f;
                float FriendshipAdd = ContractPercent / 10;
                string processingLine = "";

                ContractCut = GetCut(ContractCut);

                if (profile.ProcessorsOperated > 0)
                {
                    processingLine =
                        T("NPCRecycleItems", new { count = profile.RecycleQuantity, item = profile.RecycledItemName }) + "\n\n" +
                        T("NPCPackShipment");
                }

                string dialogText =
                    T("NPCOfferContract", new { npc = profile.NPCName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName }) + "\n\n" +
                    processingLine + "\n\n" +
                    T("ContractEstimatedValue", new { value = (int)estimatedValue }) + "\n\n" +
                    T("NPCContractPrepay", new { percent = ContractPercent, gold = (int)ContractCut }) + "\n\n" +
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
                            if (Game1.player.Money < (int)ContractCut)
                            {
                                Game1.showGlobalMessage(T("NPCNotEnoughGold"));
                                return;
                            }

                            Game1.player.Money -= (int)ContractCut;

                            var builder = new StringBuilder();
                            builder.AppendLine(T("NPCAcceptContract", new { gold = (int)ContractCut, npc = profile.NPCName }));

                            StardewValley.NPC NPC = Game1.getCharacterFromName(profile.NPCName);
                            if (NPC != null)
                            {
                                Game1.player.changeFriendship((int)FriendshipAdd, NPC);
                                builder.AppendLine(T("NPCFriendshipIncrease", new { npc = profile.NPCName, points = (int)FriendshipAdd }));
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
                profile.BaseProductMultiplier = 1.0f + (profile.NPCLevel * 0.50f);

                totalValue += (int)(profile.ItemPrice * profile.BaseProductMultiplier * profile.QualityMultiplier * profile.Quantity);

                totalValue = GetTotalValue(totalValue);

                return totalValue;
            }
            public static void NPCIntroduction(string NewNPCName)
            {
                profile.NPCName = NewNPCName;
                profile.NPCLevel = UpdateNPCLevel(NewNPCName);
                profile.Quality = GetQuality(profile.NPCLevel);
                profile.QualityName = GetQualityName(profile.Quality);
                profile.SeasonIndex = GetSeasonIndex(Game1.currentSeason);
                profile.ProcessorsOperated = CountProcessors("Recycling Machine");
                profile.FarmerSkillLevel = Game1.player.farmingLevel.Value + Game1.player.fishingLevel.Value + Game1.player.miningLevel.Value + Game1.player.foragingLevel.Value + Game1.player.combatLevel.Value; // Player Skill Level

                int[][] SeasonalCollect = new int[][]
                {
                    new int[] { 16, 18, 22, 78, 129, 131, 132, 137, 142, 145, 147, 148, 176, 180, 372, 393, 404, 442, 591, 597, 638, 700, 702, 706, 708, 718, 719, 722, 723, 724, 725, 726 }, // Spring
                    new int[] { 78, 132, 138, 142, 145, 146, 149, 150, 176, 180, 372, 376, 393, 396, 398, 402, 404, 421, 442, 593, 700, 701, 702, 706, 708, 718, 719, 722, 723, 724, 725, 726 }, // Summer
                    new int[] { 78, 129, 131, 132, 137, 139, 142, 143, 148, 150, 154, 176, 180, 372, 393, 404, 406, 408, 421, 442, 595, 700, 701, 702, 705, 706, 718, 719, 722, 723, 724, 725, 726 }, // Fall
                    new int[] { 78, 131, 132, 141, 142, 146, 147, 154, 176, 180, 372, 393, 404, 412, 414, 416, 442, 700, 702, 705, 708, 718, 719, 722, 723, 724, 725, 726 }  // Winter
                };

                var seasonalOptions = SeasonalCollect[profile.SeasonIndex]
                    .Concat(SeasonalCollect[profile.SeasonIndex])
                    .ToList();

                Random rng = new Random();
                profile.SelectedItemID = seasonalOptions[rng.Next(seasonalOptions.Count)];
                profile.BaseItem = ItemRegistry.Create(profile.SelectedItemID.ToString()) as SObject;
                profile.Quantity = SafeMultiplier(profile.FarmerSkillLevel) + SafeMultiplier(profile.NPCLevel);
                profile.PreparedItemName = GetItemName(profile.SelectedItemID.ToString());

                int[][] SeasonalRecycle = new int[][]
                {
                    new int[] { 93, 338, 380, 382, 388, 390, 428 },
                    new int[] { 93, 338, 380, 382, 388, 390, 428 },
                    new int[] { 93, 338, 380, 382, 388, 390, 428 },
                    new int[] { 93, 338, 380, 382, 388, 390, 428 }
                };

                    var RecycleOptions = SeasonalRecycle[profile.SeasonIndex]
                    .Concat(SeasonalRecycle[profile.SeasonIndex])
                    .ToList();

                profile.RecycleItemID = RecycleOptions[rng.Next(RecycleOptions.Count)];
                profile.RecycleItem = ItemRegistry.Create(profile.RecycleItemID.ToString()) as SObject;
                profile.RecycledItemName = GetItemName(profile.RecycleItemID.ToString());
                profile.RecycleQuantity = profile.ProcessorsOperated * GetRecycleQuantity(profile.RecycleItemID);


                string dialogText =
                    T("NPCAskContract", new { npc = profile.NPCName, quantity = profile.Quantity, quality = profile.QualityName, item = profile.PreparedItemName });

                var responses = new List<Response>
                {
                    new Response("Accept", T("ResponseAccept")),
                    new Response("Decline", T("ResponseDecline")),
                    new Response("Stats", T("NPCStatsResponse", new { npc = profile.NPCName }))
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
                                NPCStats();
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
            public static void NPCStats()
            {
                int currentFriendship = Game1.player.friendshipData.TryGetValue(profile.NPCName, out var data) ? data.Points : 0;

                string dialogText =
                    T("NPCLevelInfo", new { npc = profile.NPCName, level = profile.NPCLevel, quality = profile.QualityName }) + "\n\n" +
                    T("NPCRecycleInfo", new { npc = profile.NPCName, count = profile.ProcessorsOperated }) + "\n\n" +
                    T("NPCFriendshipInfo", new { npc = profile.NPCName, points = currentFriendship });

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
                                NPCIntroduction(profile.NPCName);
                        }));
                    }
                );
            }
            private static Item GetWeightedRandomGift()
            {
                var pool = new List<(int id, double weight)>
                {
                    (72, 0.1),
                    (74, 0.05),
                    (107, 0.1),
                    (243, 0.3),
                    (253, 0.3),
                    (279, 0.02),
                    (288, 0.3),
                    (289, 0.1),
                    (337, 0.1),
                    (347, 0.05),
                    (349, 0.2),
                    (351, 0.2),
                    (392, 0.1),
                    (499, 0.1),
                    (770, 0.5),
                    (787, 0.2),
                    (797, 0.05)
                };

                double roll = Game1.random.NextDouble() * pool.Sum(p => p.weight);
                double cumulative = 0;

                foreach (var (id, weight) in pool)
                {
                    cumulative += weight;
                    if (roll <= cumulative)
                        return ItemRegistry.Create($"(O){id}") as SObject;
                }

                return null;
            }
            public static void NFNPCIntroduction(string npcName)
            {
                profile.NPCName = npcName;
                var gift = GetWeightedRandomGift();

                Game1.player.addItemByMenuIfNecessary(gift);

                string dialogText =
                    T("NPCGiftMessage");

                Game1.currentLocation.createQuestionDialogue(
                    dialogText,
                    new Response[]
                    {
                        new Response("OK", T("NPCThanksResponse", new { npc = npcName })),
                    },
                    (farmer, answer) =>
                    {
                        Game1.delayedActions.Add(new DelayedAction(100, () =>
                        {
                            if (answer == "OK")
                                return;
                        }));
                    }
                );
            }
        }
    }
}