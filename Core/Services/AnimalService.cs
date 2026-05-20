using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    public class AnimalService
    {
        public static ModConfig config;
        private IMonitor monitor;
        public int friendshipPointsEarned = 0;
        public int totalFeesPaid = 0;
        private StardewValley.NPC npc;
        private string animalnpcName;
        public int AnimalsTended = 0;
        public readonly CollectionServiceManager manager;
        public AnimalService(CollectionServiceManager manager)
        {
            this.manager = manager;
        }
        public void RunAnimalService(IMonitor monitor, string AnimalnpcName)
        {
            this.monitor = monitor;
            this.animalnpcName = AnimalnpcName;
            var npc = Game1.getCharacterFromName(this.animalnpcName);
            this.friendshipPointsEarned = 0;
            this.AnimalsTended = 0;
            this.totalFeesPaid = 0;

            Farm farm = Game1.getFarm();

            int animalsToPet = 0;
            int troughsToFill = 0;

            foreach (var animal in farm.animals.Values)
            {
                if (!animal.wasPet.Value)
                    animalsToPet++;
            }

            foreach (var b in farm.buildings)
            {
                if (b.indoors.Value is AnimalHouse house)
                {
                    foreach (var tile in GetEmptyTroughTiles(house))
                        troughsToFill++;

                    foreach (var animal in house.animals.Values)
                    {
                        if (!animal.wasPet.Value)
                            animalsToPet++;
                    }
                }
            }

            int totalTasks = animalsToPet + troughsToFill;
            if (totalTasks == 0)
            {
                Game1.showGlobalMessage(T("AnimalServiceNothingToDo"));
                return;
            }

            int feePerTask = Config.SeviceContractFees[ServiceId.Animals];

            string dialogText =
                T("AnimalServiceOffer", new { Name = this.animalnpcName, Fee = feePerTask }) + "\n\n" +
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

                    int npcLevel = UpdateNPCLevel(this.animalnpcName);
                    int farmerSkill = Game1.player.farmingLevel.Value;
                    int delay = Config.CollectionDelay / SafeMultiplier(npcLevel + farmerSkill);

                    int friendshipCounter = 0;
                    this.friendshipPointsEarned = 0;
                    this.totalFeesPaid = 0;

                    FriendshipInitialAward(ref this.friendshipPointsEarned);
                    Game1.player.changeFriendship(1, npc);
                    Game1.showGlobalMessage($"{this.animalnpcName} " + T("FriendshipInitial"));

                    int taskIndex = 0;

                    foreach (var animal in farm.animals.Values)
                    {
                        if (!animal.wasPet.Value)
                        {
                            int index = taskIndex++;
                            Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                            {
                                if (!TryChargeFeeOrStopSimple(feePerTask, this.animalnpcName, monitor))
                                {
                                    return;
                                }

                                animal.pet(Game1.player);
                                FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 6);
                                this.totalFeesPaid += feePerTask;
                            }));
                        }
                    }

                    foreach (var b in farm.buildings)
                    {
                        if (b.indoors.Value is AnimalHouse house)
                        {
                            foreach (var tile in GetEmptyTroughTiles(house))
                            {
                                int index = taskIndex++;
                                Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                                {
                                    if (!TryChargeFeeOrStopSimple(feePerTask, this.animalnpcName, monitor))
                                    {
                                        return;
                                    }

                                    house.objects[tile] = new SObject("Hay", 1);
                                    FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 6);
                                    this.totalFeesPaid += feePerTask;
                                }));
                            }

                            foreach (var animal in house.animals.Values)
                            {
                                if (!animal.wasPet.Value)
                                {
                                    int index = taskIndex++;
                                    Game1.delayedActions.Add(new DelayedAction(1 + (index * delay), () =>
                                    {
                                        if (!TryChargeFeeOrStopSimple(feePerTask, this.animalnpcName, monitor))
                                        {
                                            return;
                                        }

                                        animal.pet(Game1.player);
                                        FriendshipProgressTick(ref friendshipCounter, ref this.friendshipPointsEarned, 6);
                                        this.totalFeesPaid += feePerTask;
                                    }));
                                }
                            }
                        }
                    }

                    Game1.delayedActions.Add(new DelayedAction(1 + (taskIndex * delay), () =>
                    {
                        if (this.friendshipPointsEarned > 0)
                        {
                            Game1.player.changeFriendship(this.friendshipPointsEarned, npc);
                            Game1.showGlobalMessage(T("FriendshipSummary", new { npc = this.animalnpcName, points = this.friendshipPointsEarned }));
                        }

                        Game1.showGlobalMessage(T("AnimalServiceComplete", new { Name = this.animalnpcName, Fee = this.totalFeesPaid }));

                    }));
                });
        }
    }
}
