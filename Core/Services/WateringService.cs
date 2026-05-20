using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;

namespace CommunityContracts.Core.Services
{
    public class WateringService
    {
        public static ModConfig config;

        private IMonitor monitor;

        public int friendshipPointsEarned = 0;
        public int dirtWatered = 0;
        public int totalFeesPaid = 0;

        public bool summaryPending = false;

        private string wateringnpcName;
        private StardewValley.NPC npc;

        public readonly CollectionServiceManager manager;

        public WateringService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferWateringService(IMonitor monitor, string WateringnpcName)
        {
            this.monitor = monitor;
            this.wateringnpcName = WateringnpcName;
            var candidateTiles = new List<(GameLocation loc, Vector2 tile)>();
            int npcLevel = UpdateNPCLevel(this.wateringnpcName);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / (SafeMultiplier(npcLevel + farmerSkill) * 5);
            var npc = Game1.getCharacterFromName(this.wateringnpcName);
            this.friendshipPointsEarned = 0;
            this.dirtWatered = 0;
            this.totalFeesPaid = 0;

            foreach (GameLocation loc in GetAllLocations_ForDirt())
            {
                foreach (var pair in loc.terrainFeatures.Pairs)
                {
                    if (pair.Value is HoeDirt dirt && dirt.state.Value == HoeDirt.dry)
                        candidateTiles.Add((loc, pair.Key));
                }

                foreach (var pair in loc.objects.Pairs)
                {
                    if (pair.Value is IndoorPot pot &&
                        pot.hoeDirt.Value is HoeDirt potDirt &&
                        potDirt.state.Value == HoeDirt.dry)
                    {
                        candidateTiles.Add((loc, pair.Key));
                    }
                }
            }

            if (candidateTiles.Count == 0)
            {
                Game1.showGlobalMessage(T("NoDirt"));
                return;
            }

            candidateTiles = candidateTiles.OrderBy(t => Vector2.Distance(Game1.player.Tile, t.tile)).ToList();

            int feePerTile = Config.SeviceContractFees[ServiceId.Water];
            int maxAffordable = Game1.player.Money / feePerTile;

            int tilesToWater = Math.Min(candidateTiles.Count, maxAffordable);
            int totalFee = tilesToWater * feePerTile;

            string dialogText =
                T("WateringContractOffer", new { npc = this.wateringnpcName, quantity = tilesToWater }) + "\n\n" +
                T("WateringValue", new { Fee = totalFee, PerPot = feePerTile }) + "\n\n" +
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

                    int friendshipCounter = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.wateringnpcName} " + T("FriendshipInitial"));

                    for (int i = 0; i < tilesToWater; i++)
                    {
                        var (loc, tile) = candidateTiles[i];

                        if (loc.terrainFeatures.TryGetValue(tile, out var feature) &&
                            feature is HoeDirt dirt)
                        {
                            if (!TryChargeFeeOrStopSimple(feePerTile, this.wateringnpcName, monitor))
                            {
                                return;
                            }

                            dirt.performToolAction(new WateringCan(), 0, tile);

                            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 150);
                            this.totalFeesPaid += feePerTile;
                            this.dirtWatered++;
                        }

                        else if (loc.objects.TryGetValue(tile, out var obj) &&
                            obj is IndoorPot pot)
                        {
                            if (!TryChargeFeeOrStopSimple(feePerTile, this.wateringnpcName, monitor))
                            {
                                return;
                            }

                            pot.performToolAction(new WateringCan());

                            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 150);
                            this.totalFeesPaid += feePerTile;
                            this.dirtWatered++;
                        }

                        if (!Game1.newDay)
                            await Task.Delay(delay);
                    }

                    if (this.friendshipPointsEarned > 0)
                    {
                        var npc = Game1.getCharacterFromName(this.wateringnpcName);
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.wateringnpcName, points = this.friendshipPointsEarned }));
                    }
                    Game1.showGlobalMessage(T("WateringComplete", new { npc = this.wateringnpcName, quantity = this.dirtWatered, Fee = this.totalFeesPaid }));
                });
        }
    }
}
