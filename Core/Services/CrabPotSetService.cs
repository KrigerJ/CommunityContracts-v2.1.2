using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class CrabPotSetService
    {
        public static ModConfig config;
        private IMonitor monitor;

        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string crabPotSetnpcName;
        public int crabPotsSet = 0;
        public int baitSet = 0;

        public readonly CollectionServiceManager manager;
        public CrabPotSetService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferCrabPotContract(IMonitor monitor, string CrabPotSetnpcName)
        {
            this.monitor = monitor;
            this.crabPotSetnpcName = CrabPotSetnpcName;
            GameLocation currentLocation = Game1.player.currentLocation;
            var candidateTiles = new List<Vector2>();
            var npc = Game1.getCharacterFromName(this.crabPotSetnpcName);
            this.friendshipPointsEarned = 0;
            this.crabPotsSet = 0;
            this.baitSet = 0;
            this.totalFeesPaid = 0;
            int friendshipCounter = 0;
            int baitPurchaseFee = Config.CraftablFee["Bait"];

            for (int x = 0; x < currentLocation.Map.DisplayWidth / Game1.tileSize; x++)
            {
                for (int y = 0; y < currentLocation.Map.DisplayHeight / Game1.tileSize; y++)
                {
                    var tile = new Vector2(x, y);
                    if (IsSafeCrabPotTile(currentLocation, tile))
                    {
                        candidateTiles.Add(tile);
                    }
                }
            }

            Vector2 farmerTile = new Vector2((int)(Game1.player.Position.X / Game1.tileSize), (int)(Game1.player.Position.Y / Game1.tileSize));

            candidateTiles = candidateTiles
                .OrderBy(t => Vector2.Distance(farmerTile, t))
                .ToList();

            int potsInInventory = Game1.player.Items
                .OfType<SObject>()
                .Where(i => i.ParentSheetIndex == 710)
                .Sum(i => i.Stack);

            int feePerPot = Config.SeviceContractFees[ServiceId.SetCrabPots];
            int maxAffordable = Game1.player.Money / feePerPot;

            int potsToPlace = Math.Min(candidateTiles.Count, Math.Min(potsInInventory, maxAffordable));

            if (potsToPlace <= 0)
            {
                Game1.showGlobalMessage(T("CheckInventoryCrabPots"));
                return;
            }

            int totalFee = potsToPlace * feePerPot;

            string dialogText =
                T("PotPlaceOffer", new { npc = this.crabPotSetnpcName, quantity = potsToPlace }) + "\n\n" +
                T("PotPlaceValue", new { Fee = totalFee, PerPot = feePerPot }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new Response[]
                {
                    new Response("Yes", T("ResponseYes")),
                    new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer == "Yes")
                    {
                        int npcLevel = UpdateNPCLevel(this.crabPotSetnpcName);
                        int farmerSkill = Game1.player.fishingLevel.Value;
                        int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
                        int friendshipCounter = 0;

                        FriendshipInitialAward(ref this.friendshipPointsEarned);

                        Game1.player.changeFriendship(1, npc);
                        Game1.showGlobalMessage($"{this.crabPotSetnpcName} " + T("FriendshipInitial"));

                        for (int i = 0; i < potsToPlace; i++)
                        {
                            int index = i;

                            Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                            {
                                if (index >= candidateTiles.Count)
                                    return;

                                var tile = candidateTiles[index];

                                currentLocation.Objects[tile] = new CrabPot();
                                CrabPot placedPot = currentLocation.Objects[tile] as CrabPot;

                                var item = Game1.player.Items.FirstOrDefault(it => it is SObject obj && obj.ParentSheetIndex == 710);
                                if (item is SObject potItem)
                                {
                                    if (!TryChargeFeeOrStopSimple(feePerPot, this.crabPotSetnpcName, monitor))
                                    {
                                        return;
                                    }

                                    FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 20);
                                    this.totalFeesPaid += feePerPot;
                                    this.crabPotsSet++;

                                    potItem.Stack--;
                                    if (potItem.Stack <= 0)
                                        Game1.player.removeItemFromInventory(potItem);
                                }

                                if (!Game1.player.professions.Contains(11))
                                {
                                    SObject? baitItem = TakeBaitFromInventory();

                                    int feeThisPot = Config.SeviceContractFees[ServiceId.BaitCrabPots];

                                    if (baitItem == null)
                                    {
                                        feeThisPot = baitPurchaseFee + Config.SeviceContractFees[ServiceId.BaitCrabPots];

                                        if (!TryChargeFeeOrStopSimple(feeThisPot, this.crabPotSetnpcName, monitor))
                                        {
                                            return;
                                        }

                                        baitItem = new SObject("685", 1);
                                        this.totalFeesPaid += feeThisPot;
                                        this.baitSet++;
                                    }

                                    bool accepted = placedPot.performObjectDropInAction(baitItem, false, Game1.player);

                                    if (!accepted)
                                        placedPot.bait.Value = baitItem;
                                }

                                if (index == potsToPlace - 1)
                                {
                                    if (this.friendshipPointsEarned > 0)
                                    {
                                        var npc = Game1.getCharacterFromName(this.crabPotSetnpcName);
                                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.crabPotSetnpcName, points = this.friendshipPointsEarned }));
                                    }

                                    Game1.showGlobalMessage(T("GlobalPotsPlaced", new { npc = this.crabPotSetnpcName, numberofpots = this.crabPotsSet, fee = totalFeesPaid }));
                                }
                            }));
                        }
                    }
                    else if (answer == "No")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                    }
                });

            SObject? TakeBaitFromInventory()
            {
                var reg = Game1.player.Items.FirstOrDefault(it => it is SObject o && o.ParentSheetIndex == 685) as SObject;
                if (reg != null)
                {
                    reg.Stack--;
                    if (reg.Stack <= 0)
                        Game1.player.removeItemFromInventory(reg);
                    return new SObject("685", 1);
                }

                return null;
            }
        }
    }
}
