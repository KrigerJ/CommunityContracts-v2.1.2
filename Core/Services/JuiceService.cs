using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class JuiceService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int friendshipCounter = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string JuicenpcName;
        public int JuiceMade = 0;
        public readonly CollectionServiceManager manager;
        public bool Shipping = false;
        public JuiceService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void RunJuiceService(IMonitor monitor, string juicenpcName)
        {
            this.monitor = monitor;
            this.JuicenpcName = juicenpcName;
            var npc = Game1.getCharacterFromName(this.JuicenpcName);
            this.friendshipPointsEarned = 0;
            this.JuiceMade = 0;
            this.totalFeesPaid = 0;

            int npcLevel = UpdateNPCLevel(this.JuicenpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            int feePerItem = Config.SeviceContractFees[ServiceId.Juice];

            var ProduceStacks = Game1.player.Items
                .OfType<SObject>()
                .Where(o =>
                    (o.Category == SObject.FruitsCategory ||
                     o.Category == SObject.VegetableCategory ||
                     o.Category == SObject.GreensCategory ||
                     o.HasContextTag("forage_item")) &&
                    o.Stack > 0
                )
                .ToList();


            int totalPossible = ProduceStacks.Sum(o => o.Stack);
            int maxAffordable = Game1.player.Money / feePerItem;
            int conversions = Math.Min(totalPossible, maxAffordable);

            if (conversions <= 0)
            {
                Game1.showGlobalMessage(T("NoProduce"));
                return;
            }

            string dialogText =
                T("JuiceOffer", new { Name = this.JuicenpcName, Fee = feePerItem }) + "\n\n" +
                T("ContractAcceptPrompt");

            Game1.currentLocation.createQuestionDialogue(
                dialogText,
                new[]
                {
                     new Response("Yes", T("ResponseYesDeliver")),
                     new Response("Ship", T("ResponseShip")),
                     new Response("No", T("ResponseNo"))
                },
                async (farmer, answer) =>
                {
                    if (answer == "No")
                    {
                        Game1.showGlobalMessage(T("MaybeLater"));
                        return;
                    }

                    if (answer == "Ship")
                    {
                        Shipping = true;
                    }
                    else
                    {
                        Shipping = false;
                    }

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, Game1.getCharacterFromName(this.JuicenpcName));
                    Game1.showGlobalMessage($"{this.JuicenpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> JuiceMap = new();

                    void ScheduleNext(int delayTicks)
                    {
                        Game1.delayedActions.Add(new DelayedAction(delayTicks, () =>
                        {
                            var produce = Game1.player.Items
                                .OfType<SObject>()
                                .Where(o =>
                                    (o.Category == SObject.FruitsCategory ||
                                     o.Category == SObject.VegetableCategory ||
                                     o.Category == SObject.GreensCategory ||
                                     o.HasContextTag("forage_item")) &&
                                    o.Stack > 0
                                )
                                .FirstOrDefault();

                            if (produce == null)
                            {
                                FinalizeJuiceService(JuiceMap, this.JuicenpcName);
                                return;
                            }

                            ProcessSingleProduce(produce, JuiceMap, feePerItem, monitor);

                            ScheduleNext(delay);
                        }));
                    }
                    ScheduleNext(1);
                });
        }
        private void ProcessSingleProduce(
            SObject produce,
            Dictionary<(string id, int quality), SObject> JuiceMap,
            int feePerItem,
            IMonitor monitor)
        {
            if (!TryChargeFeeOrStopSimple(feePerItem, this.JuicenpcName, monitor))
                return;

            this.JuiceMade++;
            this.totalFeesPaid += feePerItem;
            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 50);

            const string JuiceId = "350";

            int plates = Math.Max(1, produce.Price / 40);

            produce.Stack--;
            if (produce.Stack <= 0)
                Game1.player.removeItemFromInventory(produce);

            var Juice = ItemRegistry.Create(JuiceId) as SObject;
            Juice.Stack = plates;
            Juice.Quality = produce.Quality;

            var key = (JuiceId, produce.Quality);

            if (!JuiceMap.TryGetValue(key, out var existing))
            {
                JuiceMap[key] = Juice;
            }
            else
            {
                existing.Stack += plates;
            }

            while (JuiceMap[key].Stack >= 999)
            {
                var chunk = JuiceMap[key].getOne();
                chunk.Stack = 999;
                chunk.Quality = produce.Quality;
                JuiceMap[key].Stack -= 999;

                if (Shipping)
                {
                    ShipContractItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = new List<Item> { chunk },
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);
                }
                else
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
        private void FinalizeJuiceService(Dictionary<(string id, int quality), SObject> JuiceMap, string npcName)
        {
            var finalItems = JuiceMap.Values
                .Where(i => i.Stack > 0)
                .Cast<Item>()
                .ToList();

            if (finalItems.Count > 0)
            {
                if (Shipping)
                {
                    ShipContractItems(new List<ContractsDelivery>
                    {
                        new ContractsDelivery
                        {
                            Items = finalItems,
                            RecipientID = Game1.player.UniqueMultiplayerID
                        }
                    }, Config);
                }
                else
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

            Game1.showGlobalMessage(T("JuiceFinalMessage", new { Name = npcName, Count = this.JuiceMade, Fee = this.totalFeesPaid }));
        }
    }
}
