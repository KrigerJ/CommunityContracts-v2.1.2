using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class SashimiService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int friendshipCounter = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string sashiminpcName;
        public int sashimiMade = 0;
        public bool Shipping = false;
        public readonly CollectionServiceManager manager;
        public SashimiService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void RunSashimiService(IMonitor monitor, string SashiminpcName)
        {
            this.monitor = monitor;
            this.sashiminpcName = SashiminpcName;
            var npc = Game1.getCharacterFromName(this.sashiminpcName);
            this.friendshipPointsEarned = 0;
            this.sashimiMade = 0;
            this.totalFeesPaid = 0;

            int npcLevel = UpdateNPCLevel(this.sashiminpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.fishingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            int feePerItem = Config.SeviceContractFees[ServiceId.Sashimi];

            var junkFishStacks = Game1.player.Items
                                .OfType<SObject>()
                                .Where(o =>
                                    o.Category == SObject.FishCategory &&
                                    o.Stack > 0 &&
                                    !o.HasContextTag("legendary_fish")
                                )
                                .ToList();

            int totalPossible = junkFishStacks.Sum(o => o.Stack);
            int maxAffordable = Game1.player.Money / feePerItem;
            int conversions = Math.Min(totalPossible, maxAffordable);

            if (conversions <= 0)
            {
                Game1.showGlobalMessage(T("NoJunkFish"));
                return;
            }

            string dialogText =
                T("SashimiOffer", new { Name = this.sashiminpcName, Fee = feePerItem }) + "\n\n" +
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
                    Game1.player.changeFriendship(1, Game1.getCharacterFromName(this.sashiminpcName));
                    Game1.showGlobalMessage($"{this.sashiminpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> sashimiMap = new();

                    void ScheduleNext(int delayTicks)
                    {
                        Game1.delayedActions.Add(new DelayedAction(delayTicks, () =>
                        {
                            var fish = Game1.player.Items
                                .OfType<SObject>()
                                .Where(o =>
                                    o.Category == SObject.FishCategory &&
                                    o.Stack > 0 &&
                                    !o.HasContextTag("legendary_fish")
                                )
                                .FirstOrDefault();

                            if (fish == null)
                            {
                                FinalizeSashimiService(sashimiMap, this.sashiminpcName);
                                return;
                            }

                            ProcessSingleJunkFish(fish, sashimiMap, feePerItem, monitor);

                            ScheduleNext(delay);
                        }));
                    }

                    ScheduleNext(1);
                });
        }
        private void ProcessSingleJunkFish(
            SObject fish,
            Dictionary<(string id, int quality), SObject> sashimiMap,
            int feePerItem,
            IMonitor monitor)
        {
            this.sashimiMade++;
            this.totalFeesPaid += feePerItem;
            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 50);

            const string sashimiId = "227";

            int plates = Math.Max(1, fish.Price / 20);

            fish.Stack--;
            if (fish.Stack <= 0)
                Game1.player.removeItemFromInventory(fish);

            var sashimi = ItemRegistry.Create(sashimiId) as SObject;
            sashimi.Stack = plates;
            sashimi.Quality = fish.Quality;

            var key = (sashimiId, fish.Quality);

            if (!sashimiMap.TryGetValue(key, out var existing))
            {
                sashimiMap[key] = sashimi;
            }
            else
            {
                existing.Stack += plates;
            }

            while (sashimiMap[key].Stack >= 999)
            {
                var chunk = sashimiMap[key].getOne();
                chunk.Stack = 999;
                chunk.Quality = fish.Quality;
                sashimiMap[key].Stack -= 999;

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
        private void FinalizeSashimiService(Dictionary<(string id, int quality), SObject> sashimiMap, string npcName)
        {
            var finalItems = sashimiMap.Values
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

            Game1.showGlobalMessage(T("SashimiFinalMessage", new { Name = npcName, Count = this.sashimiMade, Fee = this.totalFeesPaid }));
        }
    }
}
