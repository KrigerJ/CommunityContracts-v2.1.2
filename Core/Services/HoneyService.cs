using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class HoneyService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string honeynpcName;
        public int honeyCollected = 0;
        public bool Shipping = false;
        public bool ConvertMead = false;
        public readonly CollectionServiceManager manager;
        public HoneyService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferHoneyService(IMonitor monitor, string HoneynpcName)
        {
            this.monitor = monitor;
            this.honeynpcName = HoneynpcName;
            var npc = Game1.getCharacterFromName(this.honeynpcName);
            this.friendshipPointsEarned = 0;
            this.honeyCollected = 0;
            this.totalFeesPaid = 0;
            int npcLevel = UpdateNPCLevel(HoneynpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Honey)
               ? SpecialtyNames[ServiceId.Honey]
               : T("ServiceWeeds");

            var honeySources = ScanReadyBeeHouses();

            if (honeySources.Count == 0)
            {
                Game1.showGlobalMessage(T("NoHoneyReady"));
                return;
            }

            int feePerHouse = Config.SeviceContractFees[ServiceId.Honey];
            int maxAffordable = Game1.player.Money / feePerHouse;
            int housesToCollect = Math.Min(honeySources.Count, maxAffordable);
            int totalFee = housesToCollect * feePerHouse;

            if (ConvertMead)
                feePerHouse *= 2;

                string dialogText =
                T("ServiceOffer", new { npc = this.honeynpcName, quantity = housesToCollect, item = serviceLabels }) + "\n\n" +
                T("ServiceFeeItem", new { Fee = totalFee, feePerItem = feePerHouse }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new[]
                {
                    new Response("Yes", T("ResponseHoneyYes")),
                    new Response("Deliver", T("ResponseDeliverMead")),
                    new Response("Ship", T("ResponseShipMead")),
                    new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer == "No")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                        return;
                    }

                    if (answer == "Deliver")
                    {
                        ConvertMead = true;
                    }

                    if (answer == "Ship")
                    {
                        ConvertMead = true;
                        Shipping = true;
                    }
                    else
                    {     
                        Shipping = false; 
                    }

                    int friendshipCounter = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.honeynpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();

                    for (int i = 0; i < housesToCollect; i++)
                    {
                        var (loc, tile, honey) = honeySources[i];

                        if (!TryChargeFeeOrStop(feePerHouse, this.honeynpcName, itemMap, monitor, Config))
                        {
                            return;
                        }

                        this.totalFeesPaid += feePerHouse;
                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 30);

                        string id = ConvertMead ? "459" : honey.ItemId;

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
                        this.honeyCollected++;

                        if (stacked.Stack >= 999)
                        {
                            var deliveryChunk = new List<Item> { stacked.getOne() };
                            deliveryChunk[0].Stack = 999;

                            stacked.Stack -= 999;

                            if (Shipping)
                            {
                                ShipContractItems(new List<ContractsDelivery>
                                {
                                    new ContractsDelivery
                                    {
                                        Items = deliveryChunk,
                                        RecipientID = Game1.player.UniqueMultiplayerID
                                    }
                                }, Config);
                            }
                            else
                                DeliverContractsItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = deliveryChunk,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);
                        }

                        if (loc.Objects[tile] is SObject beeHouse)
                        {
                            beeHouse.heldObject.Value = null;
                            beeHouse.readyForHarvest.Value = false;
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
                        if (Shipping)
                        {
                            ShipContractItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = finalItems,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);
                        }
                        else
                            DeliverContractsItems(new List<ContractsDelivery>
                        {
                            new ContractsDelivery
                            {
                                Items = finalItems,
                                RecipientID = Game1.player.UniqueMultiplayerID
                            }
                        }, Config);
                    }

                    if (friendshipPointsEarned > 0)
                    {
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.honeynpcName, points = this.friendshipPointsEarned }));
                    }
                    if (ConvertMead)
                        Game1.showGlobalMessage(T("HoneyProcessedToMead", new { npc = this.honeynpcName, Number = this.honeyCollected }));
                });
        }
    }
}
