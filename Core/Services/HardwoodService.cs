using Microsoft.Xna.Framework;
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
    public class HardwoodService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string hardwoodnpcName;
        public int hardwoodCollected = 0;
        public readonly CollectionServiceManager manager;
        public HardwoodService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferHardwoodService(IMonitor monitor, string HardwoodnpcName)
        {
            this.monitor = monitor;
            this.hardwoodnpcName = HardwoodnpcName;
            var npc = Game1.getCharacterFromName(this.hardwoodnpcName);
            this.friendshipPointsEarned = 0;
            this.hardwoodCollected = 0;
            this.totalFeesPaid = 0;
            int npcLevel = UpdateNPCLevel(HardwoodnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.foragingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Hardwood)
               ? SpecialtyNames[ServiceId.Hardwood]
               : T("ServiceWeeds");

            var clumps = ScanHardwoodClumps();

            if (clumps.Count == 0)
            {
                Game1.showGlobalMessage(T("NoHardwoodReady"));
                return;
            }

            int feePerUnit = Config.SeviceContractFees[ServiceId.Hardwood];
            int totalBaseUnits = clumps.Sum(c =>
                c.clump.parentSheetIndex.Value == ResourceClump.stumpIndex ? 2 : 8);

            int maxAffordableUnits = Game1.player.Money / feePerUnit;
            int unitsToCollect = Math.Min(totalBaseUnits, maxAffordableUnits);
            int runningUnits = 0;
            int clumpsToCollect = 0;

            foreach (var c in clumps)
            {
                int baseYield = c.clump.parentSheetIndex.Value == ResourceClump.stumpIndex ? 2 : 8;
                if (runningUnits + baseYield > unitsToCollect)
                    break;

                runningUnits += baseYield;
                clumpsToCollect++;
            }

            clumps = clumps
                .OrderBy(t =>
                {
                    var (_, clump) = t;
                    var box = clump.getBoundingBox();
                    var clumpTile = new Vector2(
                        (box.X + box.Width / 2f) / 64f,
                        (box.Y + box.Height / 2f) / 64f
                    );
                    return Vector2.Distance(Game1.player.Tile, clumpTile);
                })
                .ToList();

            int totalFee = runningUnits * feePerUnit;

            string dialogText =
                T("ServiceOffer", new { npc = this.hardwoodnpcName, quantity = runningUnits, item = serviceLabels }) + "\n\n" +
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
                    Game1.showGlobalMessage($"{this.hardwoodnpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();

                    for (int i = 0; i < clumpsToCollect; i++)
                    {
                        var (loc, clump) = clumps[i];

                        int baseYield = clump.parentSheetIndex.Value == ResourceClump.stumpIndex ? 2 : 8;
                        int totalYield = baseYield + quality;

                        if (!TryChargeFeeOrStop(feePerUnit * baseYield, this.hardwoodnpcName, itemMap, monitor, Config))
                        {
                            return;
                        }

                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 80);

                        string id = "709";
                        var key = (id, quality);

                        if (!itemMap.TryGetValue(key, out var stacked))
                        {
                            stacked = new SObject(id, totalYield);
                            itemMap[key] = stacked;
                            this.hardwoodCollected += totalYield;
                        }
                        else
                        {
                            stacked.Stack += totalYield;
                            this.hardwoodCollected += totalYield;
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

                        loc.resourceClumps.Remove(clump);

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
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.hardwoodnpcName, points = this.friendshipPointsEarned }));
                    }
                });
        }
    }
}
