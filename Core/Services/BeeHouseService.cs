using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.DirectionHelper;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class BeeHouseService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string beeHousenpcName;
        public int beeHousesPlaced = 0;
        public readonly CollectionServiceManager manager;
        public BeeHouseService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferBeeHouseContract(IMonitor monitor, string BeeHousenpcName)
        {
            this.monitor = monitor;
            this.beeHousenpcName = BeeHousenpcName;
            var npc = Game1.getCharacterFromName(this.beeHousenpcName);
            this.friendshipPointsEarned = 0;
            this.beeHousesPlaced = 0;
            this.totalFeesPaid = 0;
            GameLocation loc = Game1.player.currentLocation;

            if (!loc.IsOutdoors)
            {
                Game1.showGlobalMessage("Bee Houses can only be placed outdoors.");
                return;
            }

            var candidateTiles = new List<Vector2>();

            for (int x = 0; x < loc.Map.DisplayWidth / Game1.tileSize; x++)
            {
                for (int y = 0; y < loc.Map.DisplayHeight / Game1.tileSize; y++)
                {
                    Vector2 tile = new Vector2(x, y);

                    if (!IsWithinScanSquare(Game1.player, tile))
                        continue;

                    var beeHouse = new SObject(tile, "10", true);

                    if (CanPlaceObjectHere(loc, tile, beeHouse))
                        candidateTiles.Add(tile);
                }
            }

            if (candidateTiles.Count == 0)
            {
                Game1.showGlobalMessage("No valid outdoor tiles for Bee Houses.");
                return;
            }

            int beeHousesInInventory = Game1.player.Items
                .OfType<SObject>()
                .Where(o => o.QualifiedItemId == "(BC)10")
                .Sum(o => o.Stack);

            candidateTiles = SortTileSquare();

            int placementFee = Config.SeviceContractFees[ServiceId.PlaceBeeHouse];
            int purchaseFee = Config.CraftablFee["BeeHouse"];
            int housesToPlace = candidateTiles.Count;
            int housesFromInventory = Math.Min(housesToPlace, beeHousesInInventory);
            int housesToBuy = Math.Max(0, housesToPlace - beeHousesInInventory);

            int estimatedTotalFee =
                (housesFromInventory * placementFee) +
                (housesToBuy * (placementFee + purchaseFee));

            string dialogText =
                T("BeeHousePrompt", new { Name = this.beeHousenpcName, Number = housesToPlace }) + "\n\n" +
                T("BeeHouseFee", new { FeeToPlace = placementFee, PurchaseFeePerItem = purchaseFee, FeeTotal = estimatedTotalFee }) + "\n\n" +
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

                    int npcLevel = UpdateNPCLevel(this.beeHousenpcName);
                    int farmerSkill = Game1.player.farmingLevel.Value;
                    int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

                    int friendshipCounter = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.beeHousenpcName} " + T("FriendshipInitial"));

                    for (int i = 0; i < housesToPlace; i++)
                    {
                        int index = i;

                        Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                        {
                            if (index >= candidateTiles.Count)
                                return;

                            Vector2 tile = candidateTiles[index];

                            var beeHouse = new StardewValley.Object(tile, "10", true);
                            loc.objects[tile] = beeHouse;
                            this.beeHousesPlaced++;

                            var item = Game1.player.Items
                                .OfType<SObject>()
                                .FirstOrDefault(o => o.QualifiedItemId == "10");

                            bool hasInventory = item != null && item.Stack > 0;

                            int feePer = hasInventory
                                ? placementFee
                                : placementFee + purchaseFee;

                            if (!TryChargeFeeOrStopSimple(feePer, this.beeHousenpcName, monitor))
                            {
                                return;
                            }
                            this.totalFeesPaid += feePer;

                            if (hasInventory)
                            {
                                item.Stack--;
                                if (item.Stack <= 0)
                                    Game1.player.removeItemFromInventory(item);
                            }

                            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 20);

                            if (index == housesToPlace - 1)
                            {
                                if (this.friendshipPointsEarned > 0)
                                {
                                    Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                                    Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.beeHousenpcName, points = this.friendshipPointsEarned }));
                                }

                                Game1.showGlobalMessage(T("BeeHousePlaced", new { Name = this.beeHousenpcName, Number = this.beeHousesPlaced, Fee = this.totalFeesPaid }));
                            }
                        }));
                    }
                });
        }
    }
}
