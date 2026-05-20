using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;

namespace CommunityContracts.Core
{
    public static class CollectionUtilities
    {
        public static ModConfig config;

        public static void NPCService(string NewNPCName, ServiceId serviceId, IMonitor monitor)
        {
            if (serviceId == ServiceId.CrabPots)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.CrabPotCatch.OfferCrabPotService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.SetCrabPots)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.CrabPotSet.OfferCrabPotContract(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.BaitCrabPots)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Bait.OfferCrabPotBaitContract(Instance.Monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Honey)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Honey.OfferHoneyService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Crops)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Harvest.OfferHarvestingService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Hardwood)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Hardwood.OfferHardwoodService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Wood)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Wood.OfferWoodService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Forageables)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Forageables.OfferForageablesService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Stone)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Stone.OfferStoneService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Weeds)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Weeds.OfferWeedsService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Tappers)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.TapperCollect.OfferTapperService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Till)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Tilling.OfferTillingService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Water)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Watering.OfferWateringService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Seeds)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Planting.OfferPlantingService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.PlaceTappers)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.PlaceTappers.TapperSetContract(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.PlaceInvisiblePots)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.PotPlacement.PotPlacementContract(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.PlaceBeeHouse)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.PlaceBeeHouse.OfferBeeHouseContract(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.SeedMaker)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.SeedMaker.OfferSeedMakingService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Sashimi)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Sashimi.RunSashimiService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Ore)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Ore.OreToIngotService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Animals)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Animals.RunAnimalService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.Juice)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.Juice.RunJuiceService(monitor, NewNPCName);
                }));
            }

            if (serviceId == ServiceId.AnimalProducts)
            {
                Game1.delayedActions.Add(new DelayedAction(100, () =>
                {
                    ModEntry.Services.AnimalProducts.ProcessAnimalProducts(monitor, NewNPCName);
                }));
            }
        }
    }
}
