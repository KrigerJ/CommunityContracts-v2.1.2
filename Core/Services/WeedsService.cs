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
    public class WeedsService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string weedsnpcName;
        public int weedsCollected = 0;
        public readonly CollectionServiceManager manager;
        public WeedsService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferWeedsService(IMonitor monitor, string WeedsnpcName)
        {
            this.monitor = monitor;
            this.weedsnpcName = WeedsnpcName;
            var npc = Game1.getCharacterFromName(this.weedsnpcName);
            this.friendshipPointsEarned = 0;
            this.weedsCollected = 0;
            this.totalFeesPaid = 0;
            int npcLevel = UpdateNPCLevel(WeedsnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.foragingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Weeds)
               ? SpecialtyNames[ServiceId.Weeds]
               : T("ServiceWeeds");

            var weeds = ScanWeeds();

            if (weeds.Count == 0)
            {
                Game1.showGlobalMessage(T("NoWeedsReady"));
                return;
            }

            weeds = weeds.OrderBy(t => Vector2.Distance(Game1.player.Tile, t.tile)).ToList();

            int feePerUnit = Config.SeviceContractFees[ServiceId.Weeds];
            int totalUnits = weeds.Count;
            int maxAffordableUnits = Game1.player.Money / feePerUnit;
            int unitsToCollect = Math.Min(totalUnits, maxAffordableUnits);
            int totalFee = unitsToCollect * feePerUnit;

            string dialogText =
                T("ServiceOffer", new { npc = this.weedsnpcName, quantity = unitsToCollect, item = serviceLabels }) + "\n\n" +
                T("ServiceFeeItem", new { Fee = totalFee, feePerItem = feePerUnit }) + "\n\n" +
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

                    int friendshipCounter = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.weedsnpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();
                    string fiberId = "771";

                    for (int i = 0; i < unitsToCollect; i++)
                    {
                        var (loc, tile, weed) = weeds[i];

                        if (!TryChargeFeeOrStop(feePerUnit, this.weedsnpcName, itemMap, monitor, Config))
                        {
                            return;
                        }

                        this.totalFeesPaid += feePerUnit;
                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 100);

                        var key = (fiberId, 1);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject(fiberId, 1);
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
                        }

                        loc.Objects.Remove(tile);
                        this.weedsCollected++;

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
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.weedsnpcName, points = this.friendshipPointsEarned }));
                    }
                });
        }
    }
}
