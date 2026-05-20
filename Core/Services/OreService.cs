using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class OreService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int friendshipCounter = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string orenpcName;
        public int ingotsMade = 0;
        public readonly CollectionServiceManager manager;
        public OreService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        private static bool IsOre(SObject obj)
        {
            return OreToBarMap.ContainsKey(obj.ItemId);
        }
        public void OreToIngotService(IMonitor monitor, string OrenpcName)
        {
            this.monitor = monitor;
            this.orenpcName = OrenpcName;
            var npc = Game1.getCharacterFromName(this.orenpcName);
            this.friendshipPointsEarned = 0;
            this.ingotsMade = 0;
            this.totalFeesPaid = 0;

            int npcLevel = UpdateNPCLevel(this.orenpcName);
            int farmerSkill = Game1.player.miningLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            int feePerItem = Config.SeviceContractFees[ServiceId.Ore];

            string dialogText =
                T("OreOffer", new { Name = this.orenpcName, Fee = feePerItem }) + "\n\n" +
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

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, Game1.getCharacterFromName(this.orenpcName));
                    Game1.showGlobalMessage($"{this.orenpcName} " + T("FriendshipInitial"));

                    Dictionary<string, SObject> barMap = new();

                    void ScheduleNext(int delayTicks)
                    {
                        Game1.delayedActions.Add(new DelayedAction(delayTicks, () =>
                        {
                            var ore = Game1.player.Items
                                .OfType<SObject>()
                                .Where(o => IsOre(o) && o.Stack >= 5)
                                .FirstOrDefault();

                            if (ore == null)
                            {
                                FinalizeBarService(barMap, this.orenpcName);
                                return;
                            }

                            ProcessSingleOreStack(ore, barMap, feePerItem, monitor);

                            ScheduleNext(delay);
                        }));
                    }

                    ScheduleNext(1);
                });
        }
        private void ProcessSingleOreStack(
            SObject ore,
            Dictionary<string, SObject> barMap,
            int feePerBar,
            IMonitor monitor)
        {
            
            if (!TryChargeFeeOrStopSimple(feePerBar, this.orenpcName, monitor))
                return;

            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 50);
            this.ingotsMade++;
            this.totalFeesPaid += feePerBar;

            string barId = OreToBarMap[ore.ItemId];

            ore.Stack -= 5;
            if (ore.Stack <= 0)
                Game1.player.removeItemFromInventory(ore);

            var bar = ItemRegistry.Create(barId) as SObject;
            bar.Stack = 1;

            if (!barMap.TryGetValue(barId, out var existing))
            {
                barMap[barId] = bar;
            }
            else
            {
                existing.Stack += 1;
            }

            while (barMap[barId].Stack >= 999)
            {
                var chunk = barMap[barId].getOne();
                chunk.Stack = 999;
                barMap[barId].Stack -= 999;

                DeliverContractsItems(new List<ContractsDelivery>
                {
                    new ContractsDelivery
                    {
                        Items = new List<Item> { chunk },
                        RecipientID = Game1.player.UniqueMultiplayerID
                    }
                }, Config);
            }
        }
        private void FinalizeBarService(Dictionary<string, SObject> barMap, string npcName)
        {
            var finalItems = barMap.Values
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
                var npc = Game1.getCharacterFromName(npcName);
                Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                Game1.showGlobalMessage(T("FriendshipSummary", new { npc = npcName, points = this.friendshipPointsEarned }));
            }

            Game1.showGlobalMessage(T("OreServiceFinalMessage", new { Name = npcName, Count = this.ingotsMade, Fee = this.totalFeesPaid }));
        }
    }
}
