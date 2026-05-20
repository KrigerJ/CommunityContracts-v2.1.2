using Microsoft.Xna.Framework;
using StardewModdingAPI;
using static CommunityContracts.Core.NPCServiceMenu;

public class ModConfig
{
    public SButton CheatMenuHotkey { get; set; } = (SButton)86;
    public string DeliveryChestColor { get; set; } = "White";
    public string HighlightColor { get; set; } = "Yellow";
    public string FontColor { get; set; } = "Black";
    public bool EnableProcessTimeReduction { get; set; } = true;
    public Dictionary<string, int> NPCContractPercents { get; set; } = new()
    {
        { "Basic", 80 },
        { "Custom", 70 },
    };
    public Dictionary<string, int> CraftablFee { get; set; } = new()
    {
        { "GardenPot", 400 },
        { "BeeHouse", 2000 },
        { "Tapper", 900 },
        { "Bait", 5 },
    };
    public Dictionary<ServiceId, int> SeviceContractFees { get; set; } = new()
    {
        { ServiceId.SetCrabPots, 100 },
        { ServiceId.BaitCrabPots, 2 },
        { ServiceId.CrabPots, 3 },
        { ServiceId.Crops, 4 },
        { ServiceId.Forageables, 3 },
        { ServiceId.Hardwood, 5 },
        { ServiceId.Honey, 5 },
        { ServiceId.Stone, 4 },
        { ServiceId.Weeds, 2 },
        { ServiceId.Wood, 4 },
        { ServiceId.Tappers, 3 },
        { ServiceId.Producers, 3 },
        { ServiceId.Till, 5 },
        { ServiceId.Water, 1 },
        { ServiceId.PlaceTappers, 50 },
        { ServiceId.Fertilize, 1 },
        { ServiceId.Seeds, 3 },
        { ServiceId.PlaceInvisiblePots, 100 },
        { ServiceId.PlaceBeeHouse, 100 },
        { ServiceId.SeedMaker, 10 },
        { ServiceId.Sashimi, 5 },
        { ServiceId.Ore, 100 },
        { ServiceId.Animals, 5 },
        { ServiceId.Juice, 50 },
        { ServiceId.AnimalProducts, 5 },
        { ServiceId.Warp, 2 }
    };
    public float ConeAngleDegrees { get; set; } = 90f;
    public int ConeRange { get; set; } = 90;
    public int RectangleWidth { get; set; } = 7;
    public int RectangleLength { get; set; } = 5;

    public bool PlaceNearToFar = false;
    public int MenuColumns { get; set; } = 5;
    public int CollectionDelay { get; set; } = 2000;
    public string DropLocationName { get; set; } = "Farm";
    public int DropTileX { get; set; } = 68;
    public int DropTileY { get; set; } = 15;
    public Dictionary<string, Color> ChestColors { get; set; } = new()
    {
        { "White", Color.White },
        { "Sky Blue", Color.LightSkyBlue },
        { "Gold", Color.Gold },
        { "Green", Color.LightGreen },
        { "Purple", Color.MediumPurple },
        { "Orange", Color.Orange },
        { "Pink", Color.Pink }
    };
    public Dictionary<string, Color> HighlightColors { get; set; } = new()
    {
        { "Yellow", Color.Yellow * 0.75f },
        { "Red", Color.Red * 0.6f },
        { "Green", Color.LimeGreen * 0.6f },
        { "Blue", Color.DeepSkyBlue * 0.6f },
        { "White", Color.White * 0.5f },
        { "Purple", Color.MediumPurple * 0.6f }
    };
    public Dictionary<string, Color> FontColors { get; set; } = new()
    {
        { "Black", Color.Black },
        { "White", Color.White },
        { "Yellow", Color.Yellow },
        { "Red", Color.Red },
        { "Green", Color.LimeGreen },
        { "Blue", Color.DeepSkyBlue },
        { "Purple", Color.MediumPurple }
    };

    public Dictionary<string, int> RawToYieldMap = new()
        {
            { "436", 1 }, //Goat Milk
            { "438", 2 }, //L Goat Milk
            { "184", 1 }, // Milk
            { "186", 2 }, //L Milk
            { "176", 1 }, //Egg
	        { "180", 1 }, //Egg
	        { "174", 2 }, //L Egg
	        { "182", 2 }, //L Egg
	        { "289", 12 }, //Ostrich Egg
	        { "928", 10 }, //Golden Egg
	        { "107", 1 }, //Dinosaur Egg
	        { "305", 1 }, //Void Egg
	        { "442", 1 }, //Duck Egg
	        { "440", 1 }, //Wool
	        { "430", 1 }, //Truffle
        };
}
