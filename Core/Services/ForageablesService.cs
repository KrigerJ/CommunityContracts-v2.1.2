using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class ForageablesService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string forageablesName;
        public int forageablesCollected = 0;
        public readonly CollectionServiceManager manager;
        public ForageablesService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferForageablesService(IMonitor monitor, string ForageablesName)
        {
            this.monitor = monitor;
            this.forageablesName = ForageablesName;
            var npc = Game1.getCharacterFromName(this.forageablesName);
            this.friendshipPointsEarned = 0;
            this.forageablesCollected = 0;
            this.totalFeesPaid = 0;
            int npcLevel = UpdateNPCLevel(this.forageablesName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.foragingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Forageables)
               ? SpecialtyNames[ServiceId.Forageables]
               : T("ServiceWeeds");

            var sources = ScanForageableSources();

            if (sources.Count == 0)
            {
                Game1.showGlobalMessage(T("NoForageablesReady"));
                return;
            }

            int feePerUnit = Config.SeviceContractFees[ServiceId.Forageables];
            int maxAffordable = Game1.player.Money / feePerUnit;
            int itemsToCollect = Math.Min(sources.Count, maxAffordable);
            int totalFee = itemsToCollect * feePerUnit;

            string dialogText =
                T("ServiceOffer", new { npc = this.forageablesName, quantity = itemsToCollect, item = serviceLabels }) + "\n\n" +
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
                    Game1.showGlobalMessage($"{this.forageablesName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();

                    for (int i = 0; i < itemsToCollect; i++)
                    {
                        var (loc, tile, item, fromProducer) = sources[i];

                        if (!TryChargeFeeOrStop(feePerUnit, this.forageablesName, itemMap, monitor, Config))
                        {
                            return;
                        }
                        this.totalFeesPaid+= feePerUnit;

                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 30);

                        string id = item.ItemId;
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
                        this.forageablesCollected += 1;

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

                        if (fromProducer)
                        {
                            if (loc.Objects.TryGetValue(tile, out var prod) &&
                                prod is SObject box &&
                                box.heldObject.Value is not null)
                            {
                                box.heldObject.Value = null;
                            }
                        }

                        else if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                            feature is Tree tree &&
                            tree.hasMoss.Value)
                        {
                            tree.performUseAction(tile);
                        }

                        else
                        {
                            loc.Objects.Remove(tile);
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
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.forageablesName, points = this.friendshipPointsEarned }));
                    }
                });
        }
    }
}
