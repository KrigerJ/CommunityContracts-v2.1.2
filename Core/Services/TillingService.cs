using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.DirectionHelper;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;

namespace CommunityContracts.Core.Services
{
    public class TillingService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string tillingnpcName;
        public int dirtTilled = 0;
        public readonly CollectionServiceManager manager;
        public TillingService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferTillingService(IMonitor monitor, string TillingnpcName)
        {
            this.monitor = monitor;
            this.tillingnpcName = TillingnpcName;
            var npc = Game1.getCharacterFromName(this.tillingnpcName);
            this.friendshipPointsEarned = 0;
            this.dirtTilled = 0;
            this.totalFeesPaid = 0;
            GameLocation tileLocation = Game1.player.currentLocation;
            GameLocation loc = Game1.player.currentLocation;
            var candidateTiles = new List<Vector2>();
            int npcLevel = UpdateNPCLevel(this.tillingnpcName);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

            int friendshipCounter = 0;

            for (int x = 0; x < loc.Map.DisplayWidth / Game1.tileSize; x++)
            {
                for (int y = 0; y < loc.Map.DisplayHeight / Game1.tileSize; y++)
                {
                    if (IsTillable(loc, x, y))
                        candidateTiles.Add(new Vector2(x, y));
                }
            }

            candidateTiles = SortExistingTilesSquare(Game1.player, candidateTiles);

            Vector2 sampleTile = candidateTiles.Count > 0 ? candidateTiles[0] : Vector2.Zero;

            int seedsAvailable = CountSeedsAllowedHere(tileLocation, sampleTile);

            if (seedsAvailable <= 0)
            {
                Game1.showGlobalMessage(T("NoSeeds"));
                return;
            }

            int feePerTile = Config.SeviceContractFees[ServiceId.Till];
            int maxAffordable = Game1.player.Money / feePerTile;

            int tilesToTill = Math.Min(candidateTiles.Count, Math.Min(seedsAvailable, maxAffordable));

            int totalFee = tilesToTill * feePerTile;

            string dialogText =
                T("TillingContractOffer", new { npc = this.tillingnpcName, quantity = tilesToTill }) + "\n\n" +
                T("TillingValue", new { Fee = totalFee, PerTile = feePerTile }) + "\n\n" +
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

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.tillingnpcName} " + T("FriendshipInitial"));

                    int npcLevel = UpdateNPCLevel(this.tillingnpcName);
                    int farmerSkill = Game1.player.farmingLevel.Value;
                    int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

                    for (int i = 0; i < tilesToTill; i++)
                    {
                        int index = i;

                        Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                        {
                            if (index >= candidateTiles.Count)
                                return;

                            Vector2 tile = candidateTiles[index];

                            if (!TryChargeFeeOrStopSimple(feePerTile, this.tillingnpcName, monitor))
                            {
                                FinishTillingService();
                                return;
                            }

                            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 40);

                            this.dirtTilled++;
                            this.totalFeesPaid += feePerTile;

                            tileLocation.makeHoeDirt(tile);

                            HoeDirt dirt = null;
                            if (tileLocation.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt d)
                            {
                                d.performToolAction(new Hoe(), 0, tile);

                                if (tileLocation.terrainFeatures.TryGetValue(tile, out var newFeature) && newFeature is HoeDirt d2)
                                    dirt = d2;
                                else
                                    dirt = d;
                            }

                            var seed = GetNextValidSeed(tileLocation, tile);

                            if (seed != null)
                            {
                                if (dirt != null)
                                {
                                    bool planted = dirt.plant(
                                        seed.ItemId,
                                        Game1.player,
                                        false
                                    );

                                    if (!planted)
                                    {
                                        FinishTillingService();
                                        return;
                                    }

                                    seed.Stack--;
                                    if (seed.Stack <= 0)
                                        Game1.player.removeItemFromInventory(seed);

                                    dirt.performToolAction(new WateringCan(), 0, tile);

                                }
                            }

                            if (index == tilesToTill - 1)
                            {
                                FinishTillingService();
                            }
                        }));
                    }
                });
        }
        private void FinishTillingService()
        {
            if (this.friendshipPointsEarned > 0)
            {
                var npc = Game1.getCharacterFromName(this.tillingnpcName);
                Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.tillingnpcName, points = this.friendshipPointsEarned }));
            }

            Game1.showGlobalMessage(T("PlantingComplete", new { npc = this.tillingnpcName, quantity = this.dirtTilled, Fee = this.totalFeesPaid }));
        }
    }
}
