using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.DirectionHelper;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class PotPlacementService
    {
        public static ModConfig config;
        private IMonitor monitor;

        public int friendshipPointsEarned = 0;
        public int potsPlaced = 0;
        public int totalFeesPaid = 0;
        public int friendshipCounter = 0;
        public bool summaryPending = false;

        private string invisiblePotName;
        private StardewValley.NPC npc;

        public readonly CollectionServiceManager manager;

        public PotPlacementService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        private void RunFinalSummary()
        {
            if (!this.summaryPending)
                return;

            this.summaryPending = false;

            if (this.friendshipPointsEarned > 0)
            {
                Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                Game1.showGlobalMessage(
                    T("FriendshipSummary", new { npc = this.invisiblePotName, points = this.friendshipPointsEarned })
                );
            }

            Game1.showGlobalMessage(T("GardenPotPlaced", new { Name = this.invisiblePotName, Number = this.potsPlaced, Fee = this.totalFeesPaid }));
        }
        public void PotPlacementContract(IMonitor monitor, string InvisiblePotName)
        {
            this.monitor = monitor;
            this.invisiblePotName = InvisiblePotName;
            var npc = Game1.getCharacterFromName(this.invisiblePotName);
            this.npc = npc;
            this.friendshipPointsEarned = 0;
            this.potsPlaced = 0;
            this.totalFeesPaid = 0;
            this.summaryPending = false;

            GameLocation tileLocation = Game1.player.currentLocation;
            GameLocation loc = Game1.player.currentLocation;
            var candidateTiles = new List<Vector2>();

            for (int x = 0; x < loc.Map.DisplayWidth / Game1.tileSize; x++)
            {
                for (int y = 0; y < loc.Map.DisplayHeight / Game1.tileSize; y++)
                {
                    Vector2 tile = new Vector2(x, y);

                    var pot = new IndoorPot(tile);

                    if (!loc.objects.ContainsKey(tile))
                    {
                            candidateTiles.Add(tile);
                    }
                }
            }

            int potsInInventory = Game1.player.Items
                .OfType<SObject>()
                .Where(o => o.QualifiedItemId == "(BC)62")
                .Sum(o => o.Stack);

            candidateTiles = SortTileSquare();

            if (candidateTiles.Count == 0)
            {
                return;
            }

            Vector2 sampleTile = candidateTiles[0];

            int placementFee = Config.SeviceContractFees[ServiceId.PlaceInvisiblePots];
            int potPurchaseFee = Config.CraftablFee["GardenPot"];
            int potsToBuy = Math.Max(0, candidateTiles.Count - potsInInventory);
            int estimatedTotalFee = (potsInInventory * placementFee) + (potsToBuy * (placementFee + potPurchaseFee));

            if (candidateTiles.Count <= 0)
            {
                Game1.showGlobalMessage(T("NoSeeds"));
                return;
            }

            string dialogText =
                T("GardenPotPrompt", new { Name = this.invisiblePotName, Number = candidateTiles.Count }) + "\n\n" +
                T("GardenPotFee", new { FeeToPlace = placementFee, PurchaseFeePerItem = Config.CraftablFee["GardenPot"], FeeTotal = estimatedTotalFee }) + "\n\n" +
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

                    int npcLevel = UpdateNPCLevel(this.invisiblePotName);
                    int farmerSkill = Game1.player.farmingLevel.Value;
                    int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, this.npc);
                    Game1.showGlobalMessage($"{this.invisiblePotName} " + T("FriendshipInitial"));

                    for (int i = 0; i < candidateTiles.Count; i++)
                    {
                        int index = i;

                        Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                        {
                            if (index >= candidateTiles.Count)
                                return;

                            Vector2 tile = candidateTiles[index];

                            GameLocation locForTile = tileLocation;

                            if (locForTile.objects.ContainsKey(tile))
                                return;

                            var potItem = Game1.player.Items
                                .OfType<SObject>()
                                .FirstOrDefault(o => o.QualifiedItemId == "(BC)62");

                            bool hasInventoryPot = potItem != null && potItem.Stack > 0;

                            SObject potToPlace = hasInventoryPot
                                ? potItem
                                : new SObject(Vector2.Zero, "62", true);

                            int x = (int)tile.X * Game1.tileSize;
                            int y = (int)tile.Y * Game1.tileSize;

                            bool placed = potToPlace.placementAction(locForTile, x, y, Game1.player);

                            if (!placed)
                                return;

                            this.potsPlaced++;

                            if (locForTile.objects.TryGetValue(tile, out var placedObj) &&
                                placedObj is IndoorPot placedPot)
                            {
                                placedPot.performToolAction(new WateringCan());
                            }

                            int feePerPot = hasInventoryPot
                                ? Config.SeviceContractFees[ServiceId.PlaceInvisiblePots]
                                : Config.SeviceContractFees[ServiceId.PlaceInvisiblePots] + Config.CraftablFee["GardenPot"];

                            if (!TryChargeFeeOrStopSimple(feePerPot, this.invisiblePotName, monitor))
                            {
                                return;
                            }

                            this.totalFeesPaid += feePerPot;

                            if (hasInventoryPot)
                            {
                                potItem.Stack--;
                                if (potItem.Stack <= 0)
                                    Game1.player.removeItemFromInventory(potItem);
                            }

                            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 20);

                            if (index == candidateTiles.Count - 1)
                            {
                                this.summaryPending = true;
                                this.RunFinalSummary();
                            }
                        }));
                    }
                });
        }
    }
}
