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
    public class WoodService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string woodnpcName;
        public int woodCollected = 0;
        public readonly CollectionServiceManager manager;
        public WoodService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferWoodService(IMonitor monitor, string WoodnpcName)
        {
            this.monitor = monitor;
            this.woodnpcName = WoodnpcName;
            var npc = Game1.getCharacterFromName(this.woodnpcName);
            this.friendshipPointsEarned = 0;
            this.woodCollected = 0;
            this.totalFeesPaid = 0;
            int npcLevel = UpdateNPCLevel(this.woodnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.foragingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Wood)
               ? SpecialtyNames[ServiceId.Wood]
               : T("ServiceWeeds");

            var debris = ScanWoodDebris();

            if (debris.Count == 0)
            {
                Game1.showGlobalMessage(T("NoWoodReady"));
                return;
            }

            debris = debris.OrderBy(t => Vector2.Distance(Game1.player.Tile, t.tile)).ToList();

            int feePerUnit = Config.SeviceContractFees[ServiceId.Wood];
            int totalUnits = debris.Count;
            int maxAffordableUnits = Game1.player.Money / feePerUnit;
            int unitsToCollect = Math.Min(totalUnits, maxAffordableUnits);

            if (unitsToCollect <= 0)
            {
                Game1.showRedMessage(T("CannotAffordService"));
                return;
            }

            int totalFee = unitsToCollect * feePerUnit;

            string dialogText =
                T("ServiceOffer", new { npc = this.woodnpcName, quantity = unitsToCollect, item = serviceLabels }) + "\n\n" +
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
                    Game1.showGlobalMessage($"{this.woodnpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();
                    string woodId = "388";

                    for (int i = 0; i < unitsToCollect; i++)
                    {
                        var (loc, tile, obj) = debris[i];

                        if (!TryChargeFeeOrStop(feePerUnit, this.woodnpcName, itemMap, monitor, Config))
                        {
                            return;
                        }

                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 80);
                        this.totalFeesPaid += feePerUnit;
                        this.woodCollected++;

                        int totalYield = 1 + quality;
                        var key = (woodId, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject(woodId, totalYield);
                            itemMap[key] = stacked;
                        }
                        else
                        {
                            stacked.Stack += totalYield;
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
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.woodnpcName, points = this.friendshipPointsEarned }));
                    }
                });
        }
    }
}
