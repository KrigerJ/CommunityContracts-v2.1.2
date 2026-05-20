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
    public class AnimalProductsService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int friendshipCounter = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string AnimalProductsnpcName;
        public int AnimalProductsMade = 0;
        public readonly CollectionServiceManager manager;
        public bool Shipping = false;
        public AnimalProductsService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void ProcessAnimalProducts(IMonitor monitor, string animalProductsnpcName)
        {
            this.monitor = monitor;
            this.AnimalProductsnpcName = animalProductsnpcName;
            var npc = Game1.getCharacterFromName(this.AnimalProductsnpcName);
            this.friendshipPointsEarned = 0;
            this.AnimalProductsMade = 0;
            this.totalFeesPaid = 0;

            int npcLevel = UpdateNPCLevel(this.AnimalProductsnpcName);
            int quality = GetQuality(npcLevel);
            int farmerSkill = Game1.player.farmingLevel.Value;
            int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);
            int feePerItem = Config.SeviceContractFees[ServiceId.AnimalProducts];

            string dialogText =
                T("AnimalProductsOffer", new { Name = this.AnimalProductsnpcName, Fee = feePerItem }) + "\n\n" +
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
                    Game1.player.changeFriendship(1, Game1.getCharacterFromName(this.AnimalProductsnpcName));
                    Game1.showGlobalMessage($"{this.AnimalProductsnpcName} " + T("FriendshipInitial"));

                    Dictionary<(string id, int quality), SObject> ProductMap = new();

                    void ScheduleNext(int delayTicks)
                    {
                        Game1.delayedActions.Add(new DelayedAction(delayTicks, () =>
                        {
                            bool processedAny = false;

                            var farm = Game1.getFarm();

                            List<GameLocation> interiors = new List<GameLocation>();

                            foreach (var building in farm.buildings)
                            {
                                if (building.indoors.Value != null)
                                    interiors.Add(building.indoors.Value);
                            }

                            var autograbbers = interiors
                                .SelectMany(loc => loc.Objects.Pairs)
                                .Where(p => p.Value is SObject obj &&
                                            obj.bigCraftable.Value &&
                                            obj.ParentSheetIndex == 165)
                                .ToList();

                            foreach (var pair in autograbbers)
                            {
                                var autograbber = pair.Value;
                                var chest = autograbber.heldObject.Value as Chest;

                                if (chest == null)
                                    continue;

                                var GrabAnimalRaw = chest.Items
                                    .OfType<SObject>()
                                    .Where(o => RawToProductMap.ContainsKey(o.ItemId))
                                    .FirstOrDefault();

                                if (GrabAnimalRaw != null)
                                {
                                    processedAny = true;

                                    ProcessSingleAnimalProductStack(GrabAnimalRaw, ProductMap, feePerItem, monitor);

                                    if (GrabAnimalRaw.Stack <= 0)
                                        chest.Items.Remove(GrabAnimalRaw);

                                    break;
                                }
                            }

                            if (processedAny)
                            {
                                ScheduleNext(delay);
                                return;
                            }

                            var AnimalRaw = Game1.player.Items
                                .OfType<SObject>()
                                .Where(o => RawToProductMap.ContainsKey(o.ItemId))
                                .FirstOrDefault();

                            if (AnimalRaw != null)
                            {
                                ProcessSingleAnimalProductStack(AnimalRaw, ProductMap, feePerItem, monitor);

                                if (AnimalRaw.Stack <= 0)
                                    Game1.player.removeItemFromInventory(AnimalRaw);

                                ScheduleNext(delay);
                                return;

                            }

                            FinalizeAnimalProductsService(ProductMap, this.AnimalProductsnpcName);
                        }));

                    }
                    ScheduleNext(1);
                });
        }
        private void ProcessSingleAnimalProductStack(
            SObject AnimalRaw,
            Dictionary<(string id, int quality), SObject> ProductMap,
            int feePerItem,
            IMonitor monitor)
        {
            int Yield = Config.RawToYieldMap[AnimalRaw.ItemId];

            if (!TryChargeFeeOrStopSimple(feePerItem, this.AnimalProductsnpcName, monitor))
                return;

            FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 50);
            this.AnimalProductsMade++;
            this.totalFeesPaid += feePerItem;

            string AnimalProductId = RawToProductMap[AnimalRaw.ItemId];
            var AnimalProduct = ItemRegistry.Create(AnimalProductId) as SObject;

            AnimalRaw.Stack -= 1;
            if (AnimalRaw.Stack <= 0)
                Game1.player.removeItemFromInventory(AnimalRaw);

            AnimalProduct.Stack = Yield;
            AnimalProduct.Quality = AnimalRaw.Quality;

            var key = (AnimalProductId, AnimalRaw.Quality);

            if (!ProductMap.TryGetValue(key, out var existing))
            {
                ProductMap[key] = AnimalProduct;
            }
            else
            {
                existing.Stack += Yield;
            }

            while (ProductMap[key].Stack >= 999)
            {
                var chunk = ProductMap[key].getOne();
                chunk.Stack = 999;
                chunk.Quality = AnimalRaw.Quality;
                ProductMap[key].Stack -= 999;

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
        private void FinalizeAnimalProductsService(Dictionary<(string id, int quality), SObject> RawToProductMap, string npcName)
        {
            var finalItems = RawToProductMap.Values
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

            Game1.showGlobalMessage(T("AnimalProductsFinalMessage", new { Name = npcName, Count = this.AnimalProductsMade, Fee = this.totalFeesPaid }));
        }
    }
}
