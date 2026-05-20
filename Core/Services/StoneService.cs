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
    public class StoneService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string stonenpcName;
        public int stoneCollected = 0;
        public readonly CollectionServiceManager manager;
        public StoneService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferStoneService(IMonitor monitor, string StonenpcName)
        {
            this.monitor = monitor;
            this.stonenpcName = StonenpcName;
            var npc = Game1.getCharacterFromName(this.stonenpcName);
            this.friendshipPointsEarned = 0;
            this.stoneCollected = 0;
            this.totalFeesPaid = 0;
            int npcLevel = UpdateNPCLevel(this.stonenpcName);
            int quality = GetQuality(npcLevel);
            int minerSkill = Game1.player.miningLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + minerSkill);
            var serviceLabels = SpecialtyNames.ContainsKey(ServiceId.Stone)
               ? SpecialtyNames[ServiceId.Stone]
               : T("ServiceWeeds");

            var stones = ScanStoneDebris();

            if (stones.Count == 0)
            {
                Game1.showGlobalMessage(T("NoStoneReady"));
                return;
            }

            stones = stones.OrderBy(t => Vector2.Distance(Game1.player.Tile, t.tile)).ToList();

            int feePerUnit = Config.SeviceContractFees[ServiceId.Stone];
            int totalBaseUnits = stones.Sum(s => s.baseYield);
            int maxAffordableUnits = Game1.player.Money / feePerUnit;
            int unitsToCollect = Math.Min(totalBaseUnits, maxAffordableUnits);
            int runningUnits = 0;
            int debrisToCollect = 0;

            foreach (var s in stones)
            {
                if (runningUnits + s.baseYield > unitsToCollect)
                    break;

                runningUnits += s.baseYield;
                debrisToCollect++;
            }

            int totalFee = runningUnits * feePerUnit;

            string dialogText =
                T("ServiceOffer", new { npc = this.stonenpcName, quantity = unitsToCollect, item = serviceLabels }) + "\n\n" +
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
                    Game1.showGlobalMessage($"{this.stonenpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> itemMap = new();

                    string stoneId = "390";
                    string coalId = "382";

                    for (int i = 0; i < debrisToCollect; i++)
                    {
                        var (loc, tile, baseYield, isClump) = stones[i];

                        if (!TryChargeFeeOrStop(feePerUnit * baseYield, this.stonenpcName, itemMap, monitor, Config))
                        {
                            return;
                        }

                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 70);
                        this.totalFeesPaid += feePerUnit;

                        int totalYield = baseYield + quality;
                        var stoneKey = (stoneId, quality);

                        if (!itemMap.TryGetValue(stoneKey, out var stackedStone))
                        {
                            stackedStone = new SObject(stoneId, totalYield);
                            itemMap[stoneKey] = stackedStone;
                            this.stoneCollected += totalYield;
                        }
                        else
                        {
                            stackedStone.Stack += totalYield;
                            this.stoneCollected += totalYield;
                        }

                        if (Game1.random.NextDouble() < 0.15)
                        {
                            var coalKey = (coalId, 0);

                            if (!itemMap.TryGetValue(coalKey, out var stackedCoal))
                            {
                                stackedCoal = new SObject(coalId, 1);
                                itemMap[coalKey] = stackedCoal;
                            }
                            else
                            {
                                stackedCoal.Stack += 1;
                            }
                        }

                        if (stackedStone.Stack >= 999)
                        {
                            var deliveryChunk = new List<Item> { stackedStone.getOne() };
                            deliveryChunk[0].Stack = 999;

                            stackedStone.Stack -= 999;

                            DeliverContractsItems(new List<ContractsDelivery>
                            {
                                new ContractsDelivery
                                {
                                    Items = deliveryChunk,
                                    RecipientID = Game1.player.UniqueMultiplayerID
                                }
                            }, Config);
                        }

                        if (isClump)
                        {
                            var clump = loc.resourceClumps.FirstOrDefault(c =>
                                c.Tile == tile && c.parentSheetIndex.Value == ResourceClump.boulderIndex);

                            if (clump != null)
                                loc.resourceClumps.Remove(clump);
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
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.stonenpcName, points = this.friendshipPointsEarned }));
                    }
                });
        }
    }
}
