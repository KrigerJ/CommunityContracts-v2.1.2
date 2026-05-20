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
    public class BaitService
    {
        public static ModConfig config;
        private IMonitor monitor;

        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string baitnpcName;
        public int baitSet = 0;

        public readonly CollectionServiceManager manager;
        public BaitService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void OfferCrabPotBaitContract(IMonitor monitor, string BaitnpcName)
        {
            this.monitor = monitor;
            this.baitnpcName = BaitnpcName;
            var unbaitedPots = new List<(GameLocation location, Vector2 tile, CrabPot pot)>();
            var npc = Game1.getCharacterFromName(this.baitnpcName);
            this.friendshipPointsEarned = 0;
            this.baitSet = 0;
            this.totalFeesPaid = 0;
            int friendshipCounter = 0;

            foreach (GameLocation loc in Game1.locations)
            {
                foreach (var kvp in loc.Objects.Pairs)
                {
                    if (kvp.Value is CrabPot cp)
                    {
                        bool hasCatch = cp.heldObject.Value != null;
                        bool isBaited = cp.bait != null && cp.bait.Value != null;

                        if (!isBaited && !hasCatch)
                            unbaitedPots.Add((loc, kvp.Key, cp));
                    }
                }
            }

            int baitRegular = Game1.player.Items
                .OfType<SObject>()
                .Where(i => i.ParentSheetIndex == 685)
                .Sum(i => i.Stack);

            int potsToBait = unbaitedPots.Count;
            int feePerPot = Config.SeviceContractFees[ServiceId.BaitCrabPots];
            int baitPurchaseFee = Config.CraftablFee["Bait"];
            int baitToBuy = Math.Max(0, potsToBait - baitRegular);
            int totalFee = (potsToBait * feePerPot) + (baitPurchaseFee * baitToBuy);

            if (potsToBait <= 0)
            {
                Game1.showGlobalMessage(T("NoBaitablePotsOrBaitOrGold"));
                return;
            }

            int friendshipPoints = Math.Max(1, potsToBait * feePerPot / 200);

            string dialogText =
                T("BaitContractOffer", new { npc = this.baitnpcName, quantity = potsToBait }) + "\n\n" +
                T("BaitValue", new { Fee = totalFee, PerPot = feePerPot, PerBait = baitPurchaseFee }) + "\n\n" +
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
                    if (answer != "Yes")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                        return;
                    }

                    int npcLevel = UpdateNPCLevel(this.baitnpcName);
                    int farmerSkill = Game1.player.fishingLevel.Value;
                    int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
                    int friendshipCounter = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.baitnpcName} " + T("FriendshipInitial"));

                    int baitedCount = 0;

                    for (int i = 0; i < unbaitedPots.Count && i < potsToBait; i++)
                    {
                        int index = i;

                        Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                        {
                            if (index >= unbaitedPots.Count)
                                return;

                            var (location, tile, pot) = unbaitedPots[index];

                            SObject? baitItem = TakeBaitFromInventory();

                            int feeThisPot = Config.SeviceContractFees[ServiceId.BaitCrabPots];

                            if (baitItem == null)
                            {
                                feeThisPot = baitPurchaseFee + Config.SeviceContractFees[ServiceId.BaitCrabPots];

                                baitItem = new SObject("685", 1);
                            }

                            bool accepted = pot.performObjectDropInAction(baitItem, false, Game1.player);

                            if (!accepted)
                            {
                                if (!TryChargeFeeOrStopSimple(feeThisPot, this.baitnpcName, monitor))
                                {
                                    return;
                                }

                                FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 30);
                                this.totalFeesPaid += feeThisPot;
                                this.baitSet++;

                                pot.bait.Value = baitItem;
                            }

                            if (index == potsToBait - 1)
                            {
                                if (this.friendshipPointsEarned > 0)
                                {
                                    var npc = Game1.getCharacterFromName(this.baitnpcName);
                                    Game1.player.changeFriendship(this.friendshipPointsEarned, npc);

                                    Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.baitnpcName, points = this.friendshipPointsEarned }));
                                }

                                Game1.showGlobalMessage(T("BaitingServiceComplete", new { npc = this.baitnpcName, numberofpots = potsToBait, fee = totalFee }));
                            }
                        }));
                    }

                    if (this.friendshipPointsEarned > 0)
                    {
                        var npc = Game1.getCharacterFromName(this.baitnpcName);
                        Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                        Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.baitnpcName, points = this.friendshipPointsEarned }));
                    }
                }
            );

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
