using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class PlantingService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string plantingnpcName;
        public int plotsPlanted = 0;
        public readonly CollectionServiceManager manager;
        public PlantingService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferPlantingService(IMonitor monitor, string PlantingnpcName)
        {
            this.monitor = monitor;
            this.plantingnpcName = PlantingnpcName;
            var npc = Game1.getCharacterFromName(this.plantingnpcName);
            this.friendshipPointsEarned = 0;
            this.plotsPlanted = 0;
            this.totalFeesPaid = 0;
            GameLocation tileLocation = Game1.player.currentLocation;
            GameLocation loc = Game1.player.currentLocation;
            int npcLevel = UpdateNPCLevel(this.plantingnpcName);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            int friendshipCounter = 0;

            bool TryPlantAtTile(GameLocation locForTile, Vector2 tile, SObject seed, out bool planted)
            {
                planted = false;

                GameLocation originalLocation = Game1.player.currentLocation;
                Vector2 originalPosition = Game1.player.Position;

                try
                {
                    Game1.player.currentLocation = locForTile;
                    Game1.player.Position = tile * Game1.tileSize;

                    if (locForTile.objects.TryGetValue(tile, out var obj) &&
                        obj is IndoorPot pot &&
                        pot.hoeDirt.Value is HoeDirt potDirt)
                    {
                        planted = potDirt.plant(seed.ItemId, Game1.player, false);
                    }

                    else if (locForTile.terrainFeatures.TryGetValue(tile, out var feature) &&
                             feature is HoeDirt dirt)
                    {
                        planted = dirt.plant(seed.ItemId, Game1.player, false);
                    }
                }
                finally
                {
                    Game1.player.currentLocation = originalLocation;
                    Game1.player.Position = originalPosition;
                }

                return planted;
            }

            var candidateTiles = new List<(GameLocation loc, Vector2 tile)>();

            foreach (var location in GetAllLocations_ForDirt())
            {
                int width = location.Map.DisplayWidth / Game1.tileSize;
                int height = location.Map.DisplayHeight / Game1.tileSize;

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector2 tile = new Vector2(x, y);

                        if (location.objects.TryGetValue(tile, out var obj) &&
                            obj is not IndoorPot)
                            continue;

                        if (obj is IndoorPot pot &&
                            pot.hoeDirt.Value is HoeDirt potDirt &&
                            potDirt.crop == null)
                        {
                            candidateTiles.Add((location, tile));
                            potDirt.state.Value = HoeDirt.watered;

                            continue;
                        }

                        if (location.terrainFeatures.TryGetValue(tile, out var feature) &&
                            feature is HoeDirt dirt &&
                            dirt.crop == null)
                        {
                            candidateTiles.Add((location, tile));
                            dirt.state.Value = HoeDirt.watered;

                            continue;
                        }
                    }
                }
            }

            int feePerTile = Config.SeviceContractFees[ServiceId.Seeds];

            string dialogText =
                T("PlantingContractOffer", new { npc = this.plantingnpcName, PerItem = feePerTile }) + "\n\n" +
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

                    int npcLevel = UpdateNPCLevel(this.plantingnpcName);
                    int farmerSkill = Game1.player.farmingLevel.Value;
                    int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

                    Game1.delayedActions.Add(new DelayedAction(1, () =>
                    {
                        FriendshipInitialAward(ref this.friendshipPointsEarned);
                        Game1.player.changeFriendship(1, npc);
                        Game1.showGlobalMessage($"{this.plantingnpcName} " + T("FriendshipInitial"));

                        for (int i = 0; i < candidateTiles.Count; i++)
                        {
                            int index = i;

                            Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                            {
                                if (index >= candidateTiles.Count)
                                    return;

                                var (locForTile, tile) = candidateTiles[index];

                                SObject seedToUse = null;
                                bool planted = false;

                                if (locForTile.objects.TryGetValue(tile, out var obj) &&
                                    obj is not IndoorPot)
                                {
                                    return;
                                }

                                foreach (var item in Game1.player.Items.OfType<SObject>())
                                {
                                    if (item.Category != SObject.SeedsCategory)
                                        continue;

                                    if (TryPlantAtTile(locForTile, tile, item, out planted) && planted)
                                    {
                                        seedToUse = item;
                                        break;
                                    }
                                }
                                if (!planted || seedToUse == null)
                                    return;

                                if (!TryChargeFeeOrStopSimple(feePerTile, this.plantingnpcName, monitor))
                                {
                                    FinishPlantingService();
                                    return;
                                }

                                seedToUse.Stack--;
                                if (seedToUse.Stack <= 0)
                                    Game1.player.removeItemFromInventory(seedToUse);

                                FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 70);

                                this.plotsPlanted++;
                                this.totalFeesPaid += feePerTile;

                                if (index == candidateTiles.Count - 1)
                                {
                                    FinishPlantingService();
                                }
                            }));
                        }
                    }));
                });
        }
        private void FinishPlantingService()
        {
            if (this.friendshipPointsEarned > 0)
            {
                var npc = Game1.getCharacterFromName(this.plantingnpcName);
                Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.plantingnpcName, points = this.friendshipPointsEarned }));
            }

            Game1.showGlobalMessage(T("PlantingComplete", new { npc = this.plantingnpcName, quantity = this.plotsPlanted, Fee = this.totalFeesPaid }));
        }
    }
}
