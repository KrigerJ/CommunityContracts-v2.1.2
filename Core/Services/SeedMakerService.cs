using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class SeedMakerService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int friendshipCounter = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string seedMakernpcName;
        public int cropsToSeed = 0;
        public readonly CollectionServiceManager manager;
        public SeedMakerService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferSeedMakingService(IMonitor monitor, string SeedMakernpcName)
        {
            this.monitor = monitor;
            this.seedMakernpcName = SeedMakernpcName;
            var npc = Game1.getCharacterFromName(this.seedMakernpcName);
            this.friendshipPointsEarned = 0;
            this.cropsToSeed = 0;
            this.totalFeesPaid = 0;
            int farmerSkill = Game1.player.farmingLevel.Value;
            int npcLevel = UpdateNPCLevel(this.seedMakernpcName);
            int feePerItem = Config.SeviceContractFees[ServiceId.SeedMaker];
            int delay = Config.CollectionDelay / (SafeMultiplier(npcLevel + farmerSkill));

            var cropStacks = Game1.player.Items
                .OfType<SObject>()
                .Where(o => (o.Category == -75 || o.Category == -79) && o.Stack > 0)
                .Where(o => GetSeedIdForCrop(o) != null)
                .ToList();


            if (cropStacks.Count == 0)
            {
                Game1.showGlobalMessage("You have no crops to convert into seeds.");
                return;
            }

            int totalPossible = cropStacks.Sum(o => o.Stack);
            int maxAffordable = Game1.player.Money / feePerItem;
            int conversions = Math.Min(totalPossible, maxAffordable);

            string dialogText =
                T("SeedmakerOffer", new { Name = this.seedMakernpcName, Fee = feePerItem }) + "\n\n" +
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
                    Game1.showGlobalMessage($"{this.seedMakernpcName} " + T("FriendshipInitial"));

                    Dictionary<string, SObject> seedMap = new();

                    void ScheduleNext(int delayTicks)
                    {
                        Game1.delayedActions.Add(new DelayedAction(delayTicks, () =>
                        {
                            var crop = Game1.player.Items
                                .OfType<SObject>()
                                .Where(o => (o.Category == -75 || o.Category == -79) && o.Stack > 0)
                                .Where(o => GetSeedIdForCrop(o) != null)
                                .FirstOrDefault();

                            if (crop == null)
                            {
                                FinalizeSeedMaker(seedMap);
                                return;
                            }

                            if (GetSeedIdForCrop(crop) == null)
                            {
                                ScheduleNext(delay);
                                return;
                            }

                            ProcessSingleCrop(crop, seedMap, npcLevel, feePerItem, monitor);

                            ScheduleNext(delay);
                        }));
                    }

                    ScheduleNext(1);
                });
        }
        private static string GetSeedIdForCrop(SObject crop)
        {
            foreach (var entry in Game1.cropData)
            {
                string seedId = entry.Key;
                var cropData = entry.Value;

                if (cropData.HarvestItemId == crop.ItemId)
                    return seedId;
            }

            return null;
        }
        private void ProcessSingleCrop(
            SObject crop,
            Dictionary<string, SObject> seedMap,
            int npcLevel,
            int feePerItem,
            IMonitor monitor)
        {
            string seedId = GetSeedIdForCrop(crop);

            if (seedId == null)
            {
                return;
            }

            int qty = 2;
            qty += crop.Quality switch
            {
                SObject.medQuality => 1,
                SObject.highQuality => 2,
                SObject.bestQuality => 4,
                _ => 0
            };

            crop.Stack--;
            if (crop.Stack <= 0)
                Game1.player.removeItemFromInventory(crop);

            var seed = ItemRegistry.Create(seedId) as SObject;
            seed.Stack = qty;

            if (!seedMap.TryGetValue(seedId, out var existing))
                seedMap[seedId] = seed;
            else
                existing.Stack += qty;

            if (!TryChargeFeeOrStopSimple(feePerItem, this.seedMakernpcName, monitor))
                return;

            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 50);
            this.cropsToSeed++;
            this.totalFeesPaid += feePerItem;

            while (seedMap[seedId].Stack >= 999)
            {
                var chunk = seedMap[seedId].getOne();
                chunk.Stack = 999;
                seedMap[seedId].Stack -= 999;

                DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = new List<Item> { chunk },
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);
            }
        }
        private void FinalizeSeedMaker(Dictionary<string, SObject> seedMap)
        {
            var finalItems = seedMap.Values
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
                var npc = Game1.getCharacterFromName(this.seedMakernpcName);
                Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.seedMakernpcName, points = this.friendshipPointsEarned }));
            }

            Game1.showGlobalMessage(T("SeedMakerFinalMessage", new { Name = this.seedMakernpcName, count = this.cropsToSeed, Fee = this.totalFeesPaid }));
        }
    }
}
