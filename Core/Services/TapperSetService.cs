using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.DirectionHelper;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class TapperSetService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string tapperSetnpcName;
        public int tappersPlaced = 0;
        public readonly CollectionServiceManager manager;
        public TapperSetService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void TapperSetContract(IMonitor monitor, string TapperSetnpcName)
        {
            this.monitor = monitor;
            this.tapperSetnpcName = TapperSetnpcName;
            var npc = Game1.getCharacterFromName(this.tapperSetnpcName);
            this.friendshipPointsEarned = 0;
            this.tappersPlaced = 0;
            this.totalFeesPaid = 0;
            GameLocation loc = Game1.player.currentLocation;

            var candidateTrees = new List<Tree>();

            foreach (var pair in loc.terrainFeatures.Pairs)
            {
                if (pair.Value is Tree t)
                {
                    if (t.growthStage.Value >= 5 && !t.tapped.Value)
                    {
                        if (IsWithinScanSquare(Game1.player, pair.Key))
                            candidateTrees.Add(t);
                    }
                }
            }

            if (candidateTrees.Count == 0)
            {
                Game1.showGlobalMessage("No tappable trees found.");
                return;
            }

            var tapperStacks = new Queue<SObject>();

            foreach (var item in Game1.player.Items)
            {
                if (item is SObject obj &&
                    (obj.QualifiedItemId == "(BC)105" || obj.QualifiedItemId == "(BC)264"))
                {
                    tapperStacks.Enqueue(obj);
                }
            }

            int totalTappersInInventory = tapperStacks.Sum(o => o.Stack);
            int placementFee = Config.SeviceContractFees[ServiceId.PlaceTappers];
            int tapperPurchaseFee = Config.CraftablFee["Tapper"];
            int treesToTap = candidateTrees.Count;
            int tappersFromInventory = Math.Min(treesToTap, totalTappersInInventory);
            int tappersToBuy = Math.Max(0, treesToTap - totalTappersInInventory);
            int estimatedTotalFee =
                (tappersFromInventory * placementFee) +
                (tappersToBuy * (placementFee + tapperPurchaseFee));

            string dialogText =
                T("TapperPrompt", new { Name = this.tapperSetnpcName, Number = treesToTap }) + "\n\n" +
                T("TapperFee", new { FeeToPlace = placementFee, PurchaseFeePerItem = tapperPurchaseFee, FeeTotal = estimatedTotalFee }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new[]
                {
                    new Response("Yes", T("ResponseYes")),
                    new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer != "Yes")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                        return;
                    }

                    int npcLevel = UpdateNPCLevel(this.tapperSetnpcName);
                    int farmerSkill = Game1.player.farmingLevel.Value;
                    int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

                    int friendshipCounter = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.tapperSetnpcName} " + T("FriendshipInitial"));

                    for (int i = 0; i < treesToTap; i++)
                    {
                        int index = i;

                        Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                        {
                            if (index >= candidateTrees.Count)
                                return;

                            Tree tree = candidateTrees[index];
                            Vector2 tile = tree.Tile;

                            bool hasInventoryTapper = tapperStacks.Count > 0;
                            string tapperId;

                            if (hasInventoryTapper)
                            {
                                var stack = tapperStacks.Peek();
                                tapperId = stack.QualifiedItemId;

                                stack.Stack--;

                                if (stack.Stack <= 0)
                                {
                                    tapperStacks.Dequeue();
                                    Game1.player.removeItemFromInventory(stack);
                                }
                            }
                            else
                            {
                                tapperId = "(BC)105";
                            }

                            string parentSheetIndex = tapperId == "(BC)264" ? "264" : "105";

                            var tapper = new StardewValley.Object(
                                tile,
                                parentSheetIndex,
                                true
                            );

                            loc.objects[tile] = tapper;

                            int feePerTapper = hasInventoryTapper
                                ? placementFee
                                : placementFee + tapperPurchaseFee;

                            if (!TryChargeFeeOrStopSimple(feePerTapper, this.tapperSetnpcName, monitor))
                            {
                                return;
                            }

                            this.totalFeesPaid += feePerTapper;
                            this.tappersPlaced++;
                            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 20);

                            if (index == treesToTap - 1)
                            {
                                if (this.friendshipPointsEarned > 0)
                                {
                                    Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                                    Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.tapperSetnpcName, points = this.friendshipPointsEarned }));
                                }

                                Game1.showGlobalMessage(T("TapperPlaced", new { Name = this.tapperSetnpcName, Number = this.tappersPlaced, Fee = this.totalFeesPaid }));
                            }
                        }));
                    }
                });
        }
    }
}
