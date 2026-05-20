using CommunityContracts.Core.Services;

namespace CommunityContracts.Core
{
    public class CollectionServiceManager
    {
        public bool SummaryPending { get; set; }
        public int PotsPlaced { get; set; }
        public int TotalFeesPaid { get; set; }
        public int DirtWatered { get; set; }
        public int CropsHarvested { get; set; }
        public int CrabPotsSet { get; set; }
        public int CrabPotsHarvested { get; set; }
        public int TappersCollected { get; set; }
        public int BaitSet { get; set; }
        public int FriendshipPointsEarned { get; set; }

        public PotPlacementService PotPlacement { get; }
        public WateringService Watering { get; }
        public HarvestService Harvest { get; }
        public CrabPotSetService CrabPotSet { get; }
        public CrabPotCatchService CrabPotCatch { get; }
        public BaitService Bait { get; }
        public TapperCollectService TapperCollect { get; }
        public TillingService Tilling { get; }
        public PlantingService Planting { get; }
        public HoneyService Honey { get; }
        public HardwoodService Hardwood { get; }
        public WeedsService Weeds { get; }
        public WoodService Wood { get; }
        public StoneService Stone { get; }
        public BeeHouseService PlaceBeeHouse { get; }
        public TapperSetService PlaceTappers { get; }
        public SeedMakerService SeedMaker { get; }
        public ForageablesService Forageables { get; }
        public SashimiService Sashimi { get; }
        public OreService Ore { get; }
        public AnimalService Animals { get; }
        public JuiceService Juice { get; }
        public AnimalProductsService AnimalProducts { get; }
        public CollectionServiceManager()
        {
            PotPlacement = new PotPlacementService(this);
            Watering = new WateringService(this);
            Harvest = new HarvestService(this);
            CrabPotSet = new CrabPotSetService(this);
            CrabPotCatch = new CrabPotCatchService(this);
            Bait = new BaitService(this);
            TapperCollect = new TapperCollectService(this);
            Tilling = new TillingService(this);
            Planting = new PlantingService(this);
            Honey = new HoneyService(this);
            Hardwood = new HardwoodService(this);
            Weeds = new WeedsService(this);
            Wood = new WoodService(this);
            Stone = new StoneService(this);
            PlaceBeeHouse = new BeeHouseService(this);
            PlaceTappers = new TapperSetService(this);
            SeedMaker = new SeedMakerService(this);
            Forageables = new ForageablesService(this);
            Sashimi = new SashimiService(this);
            Ore = new OreService(this);
            Animals = new AnimalService(this);
            Juice = new JuiceService(this);
            AnimalProducts = new AnimalProductsService(this);
        }
    }
}
