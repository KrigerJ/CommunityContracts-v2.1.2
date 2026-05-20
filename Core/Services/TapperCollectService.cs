using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class TapperCollectService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string tappernpcName;
        public int tappersCollected = 0;
        public readonly CollectionServiceManager manager;
        public TapperCollectService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferTapperService(IMonitor monitor, string TappernpcName)
        {
            this.monitor = monitor;
            this.tappernpcName = TappernpcName;
            var npc = Game1.getCharacterFromName(this.tappernpcName);
            this.friendshipPointsEarned = 0;
            this.tappersCollected = 0;
            this.totalFeesPaid = 0;
            int npcLevel = UpdateNPCLevel(this.tappernpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.foragingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Tappers)
               ? SpecialtyNames[ServiceId.Tappers]
               : T("ServiceWeeds");
            int friendshipCounter = 0;

            var tappers = ScanTappers();

            if (tappers.Count == 0)
            {
                Game1.showGlobalMessage(T("NoTappersReady"));
                return;
            }

            int feePerTapper = Config.SeviceContractFees[ServiceId.Tappers];
            int maxAffordable = Game1.player.Money / feePerTapper;
            int tappersToCollect = Math.Min(tappers.Count, maxAffordable);
            int totalFee = tappersToCollect * feePerTapper;

            string dialogText =
                T("ServiceOffer", new { npc = this.tappernpcName, quantity = tappersToCollect, item = serviceLabels }) + "\n\n" +
                T("ServiceFeeItem", new { Fee = totalFee, feePerItem = feePerTapper }) + "\n\n" +
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
                    Game1.showGlobalMessage($"{this.tappernpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();

                    for (int i = 0; i < tappersToCollect; i++)
                    {
                        var (loc, tile, tapper) = tappers[i];

                        if (!TryChargeFeeOrStop(feePerTapper, this.tappernpcName, itemMap, monitor, Config))
                        {
                            return;
                        }

                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 30);
                        this.totalFeesPaid += feePerTapper;

                        if (tapper.heldObject.Value is SObject tappedProduct)
                        {
                            AddTapperItemToMap(itemMap, tappedProduct, quality);

                            var key = (tappedProduct.ItemId, quality);
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
                        }

                        tapper.heldObject.Value = null;
                        tapper.readyForHarvest.Value = false;

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
                        var npc = Game1.getCharacterFromName(this.tappernpcName);
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.tappernpcName, points = this.friendshipPointsEarned }));
                    }
                });
        }
    }
}
