using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class CrabPotCatchService
    {
        public static ModConfig config;
        private IMonitor monitor;

        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        public int baitSet = 0;
        private StardewValley.NPC npc;
        private string crabCollectnpcName;
        public int crabPotsHarvested = 0;

        public readonly CollectionServiceManager manager;
        public CrabPotCatchService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferCrabPotService(IMonitor monitor, string CrabCollectnpcName)
        {
            this.monitor = monitor;
            this.crabCollectnpcName = CrabCollectnpcName;
            var npc = Game1.getCharacterFromName(this.crabCollectnpcName);
            int npcLevel = UpdateNPCLevel(this.crabCollectnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.fishingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            this.friendshipPointsEarned = 0;
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.CrabPots)
               ? SpecialtyNames[ServiceId.CrabPots]
               : T("ServiceWeeds");
            int friendshipCounter = 0;
            this.baitSet = 0;
            this.totalFeesPaid = 0;
            this.crabPotsHarvested = 0;

            var pots = ScanCrabPots();

            if (pots.Count == 0)
            {
                Game1.showGlobalMessage(T("NoCrabPotsReady"));
                return;
            }

            int feePerPot = Config.SeviceContractFees[ServiceId.CrabPots];
            int maxAffordable = Game1.player.Money / feePerPot;
            int potsToCollect = Math.Min(pots.Count, maxAffordable);
            int totalFee = potsToCollect * feePerPot;
            int baitPurchaseFee = Config.CraftablFee["Bait"];

            string dialogText =
                T("ServiceOffer", new { npc = this.crabCollectnpcName, quantity = potsToCollect, item = serviceLabels }) + "\n\n" +
                T("ServiceFeeItem", new { Fee = totalFee, feePerItem = feePerPot }) + "\n\n" +
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

                    this.friendshipPointsEarned = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.crabCollectnpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();
                    var sashimiQueue = new List<SObject>();

                    for (int i = 0; i < potsToCollect; i++)
                    {
                        var (loc, tile, pot) = pots[i];

                        if (!TryChargeFeeOrStop(feePerPot, this.crabCollectnpcName, itemMap, monitor, Config))
                        {
                            return;
                        }

                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 50);

                        if (pot.heldObject.Value is SObject catchObj)
                        {
                            AddCrabPotItemToMap(itemMap, catchObj, quality);

                            var key = (catchObj.ItemId, quality);
                            if (itemMap.TryGetValue(key, out var stacked) &&
                                stacked.Stack >= 999)
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

                            this.crabPotsHarvested++;
                        }

                        pot.heldObject.Value = null;
                        pot.readyForHarvest.Value = false;

                        if (!Game1.player.professions.Contains(11))
                        {
                            SObject? baitItem = TakeBaitFromInventory();

                            int feeThisPot = Config.SeviceContractFees[ServiceId.BaitCrabPots];

                            if (baitItem == null)
                            {
                                feeThisPot = baitPurchaseFee + Config.SeviceContractFees[ServiceId.BaitCrabPots];

                                if (!TryChargeFeeOrStopSimple(feeThisPot, this.crabCollectnpcName, monitor))
                                {
                                    return;
                                }

                                baitItem = new SObject("685", 1);
                                this.totalFeesPaid += feeThisPot;
                                this.baitSet++;
                            }

                            bool accepted = pot.performObjectDropInAction(baitItem, false, Game1.player);

                            if (!accepted)
                                pot.bait.Value = baitItem;

                            this.baitSet++;
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
                        var npc = Game1.getCharacterFromName(this.crabCollectnpcName);
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.crabCollectnpcName, points = this.friendshipPointsEarned }));
                    }

                    Game1.showGlobalMessage(T("CrabPotFinalMessage", new { count = this.crabPotsHarvested, npc = this.crabCollectnpcName, Fee = this.totalFeesPaid }));
                });

            SObject? TakeBaitFromInventory()
            {
                var reg = Game1.player.Items.FirstOrDefault(it => it is SObject o && o.ParentSheetIndex == 685) as SObject;
                if (reg != null)
                {
                    reg.Stack--;
                    if (reg.Stack <= 0)
                        Game1.player.removeItemFromInventory(reg);
                    return new SObject("685", 1);
                }

                return null;
            }
        }
    }
}
