using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using static CommunityContracts.Core.CollectionHelpers;
using static CommunityContracts.Core.CollectionUtilities;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.DirectionHelper;
using static ModEntry;
using SObject = StardewValley.Object;


namespace CommunityContracts.Core
{
    public class NPCServiceMenu : IClickableMenu
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        private readonly List<ClickableComponent> menuButtons = new();

        public static Dictionary<string, string> Specialties = new()
        {
            { "Abigail", T("SpecialtyAbigail") },
            { "Alex", T("SpecialtyAlex") },
            { "Caroline", T("SpecialtyCaroline") },
            { "Demetrius", T("SpecialtyDemetrius") },
            { "Elliott", T("SpecialtyElliott") },
            { "Emily", T("SpecialtyEmily") },
            { "Evelyn", T("SpecialtyEvelyn") },
            { "George", T("SpecialtyGeorge") },
            { "Haley", T("SpecialtyHaley") },
            { "Jas", T("SpecialtyJas") },
            { "Jodi", T("SpecialtyJodi") },
            { "Leah", T("SpecialtyLeah") },
            { "Leo", T("SpecialtyLeo") },
            { "Linus", T("SpecialtyLinus") },
            { "Maru", T("SpecialtyMaru") },
            { "Pam", T("SpecialtyPam") },
            { "Penny", T("SpecialtyPenny") },
            { "Sam", T("SpecialtySam") },
            { "Sandy", T("SpecialtySandy") },
            { "Sebastian", T("SpecialtySebastian") },
            { "Shane", T("SpecialtyShane") },
            { "Vincent", T("SpecialtyVincent") },
            { "Wizard", T("SpecialtyWizard") },
            { "General", T("SpecialtyGeneral") }
        };
        public enum ServiceId
        {
            CrabPots,
            SetCrabPots,
            BaitCrabPots,
            Forageables,
            Hardwood,
            Honey,
            Stone,
            Weeds,
            Wood,
            Crops,
            Tappers,
            Producers,
            Till,
            Water,
            Fertilize,
            Seeds,
            PlaceInvisiblePots,
            PlaceTappers,
            PlaceBeeHouse,
            SeedMaker,
            Sashimi,
            Ore,
            Animals,
            Juice,
            AnimalProducts,
            Warp
        }
        private List<(ServiceId id, string label)> GetEnabledServices()
        {
            var services = new List<(ServiceId, string)>
            {
            };

            if (ButtCount("Pots" ?? "") > 0)
                services.Add((ServiceId.SetCrabPots, T("ServiceSetCrabPots")));

            if (ButtCount("Bait" ?? "") > 0 && !Game1.player.professions.Contains(11))
                services.Add((ServiceId.BaitCrabPots, T("ServiceBaitCrabPots")));

            if (ButtCount("Crab" ?? "") > 0)
                services.Add((ServiceId.CrabPots, T("ServiceCrabPots")));

            if (ButtCount("Crop" ?? "") > 0)
                services.Add((ServiceId.Crops, T("ServiceCrops")));

            if (ButtCount("Forge" ?? "") > 0)
                services.Add((ServiceId.Forageables, T("ServiceForageables")));

            if (ButtCount("Hard" ?? "") > 0)
                services.Add((ServiceId.Hardwood, T("ServiceHardwood")));

            if (ButtCount("Bee" ?? "") > 0)
                services.Add((ServiceId.Honey, T("ServiceHoney")));

            if (ButtCount("Stone" ?? "") > 0)
                services.Add((ServiceId.Stone, T("ServiceStone")));

            if (ButtCount("Tapper" ?? "") > 0)
                services.Add((ServiceId.Tappers, T("ServiceTappers")));

            if (ButtCount("Weed" ?? "") > 0)
                services.Add((ServiceId.Weeds, T("ServiceWeeds")));

            if (ButtCount("Wood" ?? "") > 0)
                services.Add((ServiceId.Wood, T("ServiceWood")));

            if (ButtCount("Till" ?? "") > 0)
                services.Add((ServiceId.Till, T("ServiceTill")));

            if (ButtCount("Water" ?? "") > 0)
                services.Add((ServiceId.Water, T("ServiceWater")));

            if (ButtCount("Fertilize" ?? "") > 0)
                services.Add((ServiceId.Fertilize, T("ServiceFertilize")));

            if (ButtCount("Seeds" ?? "") > 0)
                services.Add((ServiceId.Seeds, T("ServiceSeeds")));

            if (ButtCount("InvisiblePots" ?? "") > 0)
                services.Add((ServiceId.PlaceInvisiblePots, T("ServiceInvisiblePots")));

            if (ButtCount("PlaceTappers" ?? "") > 0)
                services.Add((ServiceId.PlaceTappers, T("ServicePlaceTappers")));

            if (ButtCount("PlaceBeeHouse" ?? "") > 0)
                services.Add((ServiceId.PlaceBeeHouse, T("ServicePlaceBeeHouse")));

            if (ButtCount("SeedMaker" ?? "") > 0)
                services.Add((ServiceId.SeedMaker, T("ServiceSeedMaker")));

            if (ButtCount("Sashimi" ?? "") > 0)
                services.Add((ServiceId.Sashimi, T("ServiceSashimi")));

            if (ButtCount("Ore" ?? "") > 0)
                services.Add((ServiceId.Ore, T("ServiceOre")));

            if (ButtCount("Animals" ?? "") > 0)
                services.Add((ServiceId.Animals, T("ServiceAnimals")));

            if (ButtCount("Juice" ?? "") > 0)
                services.Add((ServiceId.Juice, T("ServiceJuice")));

            if (ButtCount("AnimalProducts" ?? "") > 0)
                services.Add((ServiceId.AnimalProducts, T("AnimalProducts")));

            return services;
        }

        public static Dictionary<ServiceId, string> SpecialtyNames = new()
        {
            { ServiceId.CrabPots, T("ServiceCrabPots") },
            { ServiceId.SetCrabPots, T("ServiceSetCrabPots") },
            { ServiceId.BaitCrabPots, T("ServiceBaitCrabPots") },
            { ServiceId.Forageables, T("ServiceForageables") },
            { ServiceId.Hardwood, T("ServiceHardwood") },
            { ServiceId.Honey, T("ServiceHoney") },
            { ServiceId.Stone, T("ServiceStone") },
            { ServiceId.Weeds, T("ServiceWeeds") },
            { ServiceId.Wood, T("ServiceWood") },
            { ServiceId.Crops, T("ServiceCrops") },
            { ServiceId.Tappers, T("ServiceTappers") },
            { ServiceId.Producers, T("ServiceProducers") },
            { ServiceId.Till, T("ServiceTill") },
            { ServiceId.Water, T("ServiceWater") },
            { ServiceId.Fertilize, T("ServiceFertilize") },
            { ServiceId.Seeds, T("ServiceSeeds") },
            { ServiceId.PlaceInvisiblePots, T("ServicePlaceInvisiblePots") },
            { ServiceId.PlaceTappers, T("ServicePlaceTappers") },
            { ServiceId.PlaceBeeHouse, T("ServicePlaceBeeHouse") },
            { ServiceId.SeedMaker, T("ServiceSeedMaker") },
            { ServiceId.Sashimi, T("ServiceSashimi") },
            { ServiceId.Ore, T("ServiceOre") },
            { ServiceId.Animals, T("ServiceAnimals") },
            { ServiceId.Juice, T("ServiceJuice") },
            { ServiceId.AnimalProducts, T("AnimalProducts") },
            { ServiceId.Warp, T("Warp") },
        };

        private List<NPCMenuOption> options = new List<NPCMenuOption>();
        private Dictionary<string, Texture2D> npcPortraits = new();
        private const int ButtonWidth = 220;
        private const int ButtonHeight = 60;
        private const int Columns = 4;
        private const int HSpacing = 10;
        private const int WSpacing = 20;
        private string Specialty = T("SpecialtyGeneral");
        private string NPCName = "";
        public static string ItemTypeLabel { get; set; }
        private ClickableComponent setSpecialtyContractButton;
        private string SpecialtyContractTooltip = T("SpecialtyContractTooltip");
        private static string ReturnToolbarTooltip => T("ReturnToolbarTooltip");
        private ClickableComponent ReturnToolbarButton;
        private static string ReturnCharactersTooltip => T("ReturnCharactersTooltip");
        private ClickableComponent ReturnCharactersButton;
        private ClickableComponent npcPortraitButton;
        private ClickableComponent npcNameButton;
        public static int NPCLevel { get; set; } = 0;
        public static int Quality { get; set; } = 0;
        public static int CurrentFriendship { get; set; } = 0;
        public static string QualityName { get; set; }
        public class NPCMenuOption
        {
            public ServiceId ServiceId { get; set; }
            public string name { get; set; }
            public ClickableComponent nameButton { get; set; }

        }
        public NPCServiceMenu(string NewNPCName)
        {
            int startX = 20;
            int startY = 20;

            NPCName = NewNPCName;
            NPCLevel = UpdateNPCLevel(NPCName);
            Quality = GetQuality(NPCLevel);
            QualityName = GetQualityName(Quality);
            CurrentFriendship = Game1.player.friendshipData.TryGetValue(NPCName, out var data) ? data.Points : 0;

            Specialty = Specialties.ContainsKey(NPCName)
                ? Specialties[NPCName]
                : T("SpecialtyGeneral");

            try
            {
                npcPortraits[NPCName] = Game1.content.Load<Texture2D>($"Portraits/{NPCName}");
            }
            
            catch (Exception ex)
            {

            }

            for (int i = 0; i < GetEnabledServices().Count; i++)
            {
                var (id, label) = GetEnabledServices()[i];

                var nameButton = new ClickableComponent(
                    new Rectangle(0, 0, ButtonWidth, ButtonHeight),
                    label
                );

                options.Add(new NPCMenuOption
                {
                    ServiceId = id,
                    name = label,
                    nameButton = nameButton,
                });

                menuButtons.Add(nameButton);
            }

            int PortraitX = startX + 20;
            int PortraitY = startY + 30;

            npcPortraitButton = new ClickableComponent(new Rectangle(PortraitX, PortraitY, 200, 200), "NPCPortraits");

            int buttonX = startX + 260;
            int buttonY = startY + 20;

            SpecialtyContractTooltip = T("SpecialtyContractTooltipDynamic", new { specialty = Specialty, npc = NPCName });

            setSpecialtyContractButton = new ClickableComponent( new Rectangle(buttonX, buttonY, 510, 60), "SetSpecialtyContract" );

            menuButtons.Add(setSpecialtyContractButton);

            int NPCNameButtonX = PortraitX;
            int NPCNameButtonY = PortraitY + 220;

            npcNameButton = new ClickableComponent( new Rectangle(NPCNameButtonX, NPCNameButtonY, 200, 60), NewNPCName);

            ReturnToolbarButton = new ClickableComponent(
                new Rectangle(buttonX + 400, buttonY, 440, 60),
                "ReturnToolbar"
            );

            ReturnCharactersButton = new ClickableComponent(
                new Rectangle(buttonX + 660, buttonY, 440, 60),
                "ReturnCharacters"
            );
        }
        public override void draw(SpriteBatch b)
        {
            int framePadding = 20;
            int frameWidth = (ButtonWidth + WSpacing) * Columns + framePadding * 6 + 200;
            int frameHeight = (ButtonHeight + HSpacing * 2) + framePadding * (5 * 5) + 10;
            int frameX = 20;
            int frameY = 20;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                frameX,
                frameY,
                frameWidth,
                frameHeight,
                Color.White,
                drawShadow: false
            );

            if (npcPortraits.TryGetValue(NPCName, out var portrait))
            {
                Rectangle sourceRect = new Rectangle(0, 0, 64, 64);
                Rectangle destRect = new Rectangle(npcPortraitButton.bounds.X, npcPortraitButton.bounds.Y, npcPortraitButton.bounds.Width, npcPortraitButton.bounds.Height);

                b.Draw(
                    portrait,
                    destRect,
                    sourceRect,
                    Color.White
                );
            }

            string text = T("NewSpecialtyContract", new { specialty = Specialty });

            Vector2 SpecialtytextSize = Game1.smallFont.MeasureString(text);

            int SpecialtyadjustedWidth = (int)SpecialtytextSize.X + 40;

            setSpecialtyContractButton.bounds = new Rectangle(
                setSpecialtyContractButton.bounds.X,
                setSpecialtyContractButton.bounds.Y,
                SpecialtyadjustedWidth,
                setSpecialtyContractButton.bounds.Height
            );

            Color SpecColor = setSpecialtyContractButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                                ? Color.LimeGreen
                                : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                setSpecialtyContractButton.bounds.X,
                setSpecialtyContractButton.bounds.Y,
                setSpecialtyContractButton.bounds.Width,
                setSpecialtyContractButton.bounds.Height,
                SpecColor,
                1f,
                false
            );

            float SpecialtytextX = setSpecialtyContractButton.bounds.X + (setSpecialtyContractButton.bounds.Width / 2f) - (SpecialtytextSize.X / 2f);
            float SpecialtytextY = setSpecialtyContractButton.bounds.Y + (setSpecialtyContractButton.bounds.Height / 2f) - (SpecialtytextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                text,
                Game1.smallFont,
                new Vector2(SpecialtytextX, SpecialtytextY),
                Game1.textColor
            );

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                npcNameButton.bounds.X,
                npcNameButton.bounds.Y,
                npcNameButton.bounds.Width,
                npcNameButton.bounds.Height,
                Color.White,
                1f,
                false
            );

            Vector2 NameButtonTextSize = Game1.smallFont.MeasureString(NPCName);

            float NameButtonTextX = npcNameButton.bounds.X + (npcNameButton.bounds.Width / 2f) - (NameButtonTextSize.X / 2f);
            float NameButtonTextY = npcNameButton.bounds.Y + (npcNameButton.bounds.Height / 2f) - (NameButtonTextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                NPCName,
                Game1.smallFont,
                new Vector2(NameButtonTextX, NameButtonTextY),
                Game1.textColor
            );

            base.draw(b);

            int spacing = 20;
            int rowSpacing = 20;
            int maxWidth = (ButtonWidth + WSpacing) * Columns - 20;
            int startY = setSpecialtyContractButton.bounds.Bottom + 20;

            List<List<(NPCMenuOption option, int width)>> rows = new();
            List<(NPCMenuOption, int)> currentRow = new();
            int currentRowWidth = 0;

            foreach (var option in options)
            {
                Vector2 ButtontextSize = Game1.smallFont.MeasureString(option.name);
                int adjustedWidth = (int)ButtontextSize.X + 40;

                int extraSpacing = currentRow.Count > 0 ? spacing : 0;

                if (currentRowWidth + adjustedWidth + extraSpacing > maxWidth)
                {
                    rows.Add(currentRow);
                    currentRow = new List<(NPCMenuOption, int)>();
                    currentRowWidth = 0;
                    extraSpacing = 0;
                }

                currentRow.Add((option, adjustedWidth));
                currentRowWidth += adjustedWidth + extraSpacing;
            }

            if (currentRow.Count > 0)
                rows.Add(currentRow);

            int currentY = startY;

            foreach (var row in rows)
            {
                int rowWidth = row.Sum(r => r.width) + spacing * (row.Count - 1);
                int rowStartX = frameX + 260;
                int currentX = rowStartX;

                foreach (var (option, adjustedWidth) in row)
                {
                    option.nameButton.bounds = new Rectangle(currentX, currentY, adjustedWidth, ButtonHeight);

                    Color boxColor = option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                                        ? Color.LimeGreen
                                        : Color.White;

                    drawTextureBox(
                        b,
                        Game1.menuTexture,
                        new Rectangle(0, 256, 60, 60),
                        option.nameButton.bounds.X,
                        option.nameButton.bounds.Y,
                        option.nameButton.bounds.Width,
                        option.nameButton.bounds.Height,
                        boxColor,
                        1f,
                        false
                    );

                    Vector2 ButtontextSize = Game1.smallFont.MeasureString(option.name);
                    float ButtontextX = option.nameButton.bounds.X + (option.nameButton.bounds.Width / 2f) - (ButtontextSize.X / 2f);
                    float ButtontextY = option.nameButton.bounds.Y + (option.nameButton.bounds.Height / 2f) - (ButtontextSize.Y / 2f);

                    Utility.drawTextWithShadow(
                        b,
                        option.name,
                        Game1.smallFont,
                        new Vector2(ButtontextX, ButtontextY),
                        Game1.textColor
                    );

                    currentX += adjustedWidth + spacing;
                }

                currentY += ButtonHeight + rowSpacing;
            }

            Color ReturnToolbarColor = ReturnToolbarButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                                ? Color.LimeGreen
                                : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                ReturnToolbarButton.bounds.X,
                ReturnToolbarButton.bounds.Y,
                ReturnToolbarButton.bounds.Width,
                ReturnToolbarButton.bounds.Height,
                ReturnToolbarColor,
                1f,
                false
            );

            string textReturn = T("ReturnToolbar");
            Vector2 textSize = Game1.smallFont.MeasureString(textReturn);

            float textX = ReturnToolbarButton.bounds.X + (ReturnToolbarButton.bounds.Width / 2f) - (textSize.X / 2f);
            float textY = ReturnToolbarButton.bounds.Y + (ReturnToolbarButton.bounds.Height / 2f) - (textSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                textReturn,
                Game1.smallFont,
                new Vector2(textX, textY),
                Game1.textColor
            );

            string ReturnToolbarText = T("ReturnToolbar");
            Vector2 ReturnToolbarTextSize = Game1.smallFont.MeasureString(ReturnToolbarText);

            int ReturnToolbaradjustedWidth = (int)ReturnToolbarTextSize.X + 40;

            ReturnToolbarButton.bounds = new Rectangle(
                ReturnToolbarButton.bounds.X,
                ReturnToolbarButton.bounds.Y,
                ReturnToolbaradjustedWidth,
                ReturnToolbarButton.bounds.Height
            );

            Color ReturnCharactersColor = ReturnCharactersButton.containsPoint(Game1.getMouseX(), Game1.getMouseY())
                                ? Color.LimeGreen
                                : Color.White;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                ReturnCharactersButton.bounds.X,
                ReturnCharactersButton.bounds.Y,
                ReturnCharactersButton.bounds.Width,
                ReturnCharactersButton.bounds.Height,
                ReturnCharactersColor,
                1f,
                false
            );

            string Characterstext = T("MainMenuButton");
            Vector2 CharacterstextSize = Game1.smallFont.MeasureString(Characterstext);

            float CharacterstextX = ReturnCharactersButton.bounds.X + (ReturnCharactersButton.bounds.Width / 2f) - (CharacterstextSize.X / 2f);
            float CharacterstextY = ReturnCharactersButton.bounds.Y + (ReturnCharactersButton.bounds.Height / 2f) - (CharacterstextSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                Characterstext,
                Game1.smallFont,
                new Vector2(CharacterstextX, CharacterstextY),
                Game1.textColor
            );

            string ReturnCharactersText = T("MainMenuButton");
            Vector2 ReturnCharactersTextSize = Game1.smallFont.MeasureString(ReturnCharactersText);

            int ReturnCharactersadjustedWidth = (int)ReturnCharactersTextSize.X + 40;

            ReturnCharactersButton.bounds = new Rectangle(
                ReturnCharactersButton.bounds.X,
                ReturnCharactersButton.bounds.Y,
                ReturnCharactersadjustedWidth,
                ReturnCharactersButton.bounds.Height
            );

            drawMouse(b);

            foreach (var option in options)
            {
                if (option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    ItemTypeLabel = GetItemTypeLabel(option.name);
                    int feeRate = Config.SeviceContractFees[option.ServiceId];

                    string tooltip = T("NPCCollectsForFee", new { npc = NPCName, item = option.name, rate = feeRate });

                    drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont,
                        xOffset: -300
                    );
                    break;
                }
            }

            if (npcPortraitButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) || npcNameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    T("HoverAboutNPC", new { npc = NPCName }) + "\n" +
                    T("HoverNPCContractorInfo", new { npc = NPCName, level = NPCLevel, quality = QualityName, specialty = Specialty }) + "\n" +
                    T("HoverNPCFriendship", new { npc = NPCName, points = CurrentFriendship }),
                    Game1.smallFont
                );
            }

            if (setSpecialtyContractButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    SpecialtyContractTooltip,
                    Game1.smallFont,
                    xOffset: -400
                );
            }

            if (ReturnToolbarButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReturnToolbarTooltip,
                    Game1.smallFont,
                    xOffset: -300
                );
            }

            if (ReturnCharactersButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReturnCharactersTooltip,
                    Game1.smallFont,
                    xOffset: -300
                );
            }
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (ReturnToolbarButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new CCToolbar(Helper, Monitor, Config);
                return;
            }

            if (ReturnCharactersButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.activeClickableMenu = new NPCDropdownMenu(Helper, Monitor, Config);
                return;
            }

            foreach (var button in this.menuButtons)
            {
                if (button.containsPoint(x, y))
                {
                    OnMenuButtonClicked(button);
                    return;
                }
            }

            if (this.isWithinBounds(x, y))
                return;
        }
        private void OnMenuButtonClicked(ClickableComponent button)
        {
            if (button == npcPortraitButton)
            {
                Game1.playSound("smallSelect");
                return;
            }

            if (button == setSpecialtyContractButton)
            {
                Game1.exitActiveMenu();
                ContractDispatcher.TryRunIntro(NPCName);
                return;
            }

            var option = this.options.FirstOrDefault(o => o.nameButton == button);
            if (option != null)
            {
                Game1.exitActiveMenu();
                NPCService(NPCName, option.ServiceId, Instance.Monitor);
                return;
            }
        }
        private static string GetSeedIdForCrop(SObject crop)
        {
            foreach (var entry in Game1.cropData)
            {
                string seedId = entry.Key;
                var cropData = entry.Value;

                if (cropData.HarvestItemId == crop.ItemId)
                    return seedId;
            }

            return null;
        }
        private static bool IsOre(SObject obj)
        {
            return OreToBarMap.ContainsKey(obj.ItemId);
        }
        public static int ButtCount(string Butt)
        {
            if (string.IsNullOrEmpty(Butt))
                return 0;

            int Count = 0;

            int[] weedIndices = new int[]
            {
                0, 313, 314, 315, 316, 317, 318,
                452,
                674, 675, 676, 677, 678, 679,
                747, 748, 750, 784, 785, 786, 792, 793, 794,
                882, 883, 884
            };

            if (Butt == "Bee")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Honey])
                    return 0;

                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject obj &&
                            obj.bigCraftable.Value &&
                            obj.ParentSheetIndex == 10 &&
                            obj.readyForHarvest.Value &&
                            obj.heldObject.Value is SObject honey)
                            return 1;
                    }
                }
            }

            if (Butt == "Crab")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.CrabPots])
                    return 0;

                foreach (var loc in Game1.locations)
                {
                    foreach (var pair in loc.objects.Pairs)
                    {
                        if (pair.Value is CrabPot pot &&
                            pot.readyForHarvest.Value &&
                            pot.heldObject.Value is SObject)
                            return 1;
                        
                    }
                }
            }

            if (Butt == "Weed")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Weeds])
                    return 0;

                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject weeed &&
                            weedIndices.Contains(weeed.ParentSheetIndex) &&
                            !weeed.bigCraftable.Value)
                            return 1;
                    }
                }
            }

            if (Butt == "Wood")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Wood])
                    return 0;

                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject woody &&
                            (woody.ParentSheetIndex == 30 || woody.ParentSheetIndex == 294 || woody.ParentSheetIndex == 295 || woody.ParentSheetIndex == 388) &&
                            !woody.bigCraftable.Value)
                            return 1;
                    }
                }
            }

            if (Butt == "Stone")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Stone])
                    return 0;

                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject sto &&
                            (sto.ParentSheetIndex == 343 || sto.ParentSheetIndex == 450 || sto.ParentSheetIndex == 668 || sto.ParentSheetIndex == 670) &&
                            !sto.bigCraftable.Value &&
                            sto.canBeGrabbed.Value)
                            return 1;
                    }
                }
            }

            if (Butt == "Forge")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Forageables])
                    return 0;

                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject forg)
                        {
                            if (!forg.bigCraftable.Value && forg.canBeGrabbed.Value && forg.IsSpawnedObject)
                            {
                                return 1;
                            }
                            else if (forg.bigCraftable.Value &&
                                     (forg.Name == "Mushroom Box" || forg.Name == "Mushroom Log") &&
                                     forg.heldObject.Value is SObject held)
                                return 1;
                        }
                    }
                }
            }

            if (Butt == "Tapper")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Tappers])
                    return 0;

                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var pair in location.Objects.Pairs.ToList())
                    {
                        if (pair.Value is SObject tapp &&
                            tapp.bigCraftable.Value &&
                            (tapp.ParentSheetIndex == 105 || tapp.ParentSheetIndex == 264) &&
                            tapp.readyForHarvest.Value &&
                            tapp.heldObject.Value is SObject tappedProduct)
                            return 1;
                    }
                }
            }

            if (Butt == "Crop")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Crops])
                    return 0;

                foreach (var loc in Game1.locations)
                {
                    if (loc == null)
                        continue;

                    int width = loc.Map.Layers[0].LayerWidth;
                    int height = loc.Map.Layers[0].LayerHeight;

                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            Vector2 tile = new Vector2(x, y);

                            if (IsHarvestableTile(loc, tile))
                                return 1;
                        }
                    }
                }
            }

            if (Butt == "Bait")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.BaitCrabPots])
                    return 0;

                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var kvp in location.Objects.Pairs)
                    {
                        if (kvp.Value is CrabPot cp)
                        {
                            bool hasCatch = cp.heldObject.Value != null;
                            bool isBaited = cp.bait != null && cp.bait.Value != null;

                            if (!isBaited && !hasCatch)
                                return 1;
                        }
                    }
                }
            }

            if (Butt == "Hard")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Hardwood])
                    return 0;

                foreach (GameLocation location in Game1.locations)
                {
                    foreach (var clump in location.resourceClumps.ToList())
                    {
                        if (clump.parentSheetIndex.Value == ResourceClump.stumpIndex ||
                            clump.parentSheetIndex.Value == ResourceClump.hollowLogIndex)
                            return 1;
                    }
                }
            }

            if (Butt == "Pots")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.SetCrabPots])
                    return 0;

                GameLocation currentLocation = Game1.player.currentLocation;
                var candidateTiles = new List<Vector2>();

                for (int x = 0; x < currentLocation.Map.DisplayWidth / Game1.tileSize; x++)
                {
                    for (int y = 0; y < currentLocation.Map.DisplayHeight / Game1.tileSize; y++)
                    {
                        var tile = new Vector2(x, y);
                        int potsInInventory = Game1.player.Items
                            .OfType<SObject>()
                            .Where(i => i.ParentSheetIndex == 710)
                            .Sum(i => i.Stack);

                        if (IsSafeCrabPotTile(currentLocation, tile) && potsInInventory > 0)
                            return 1;
                    }
                }
            }

            if (Butt == "InvisiblePots")
            {
                GameLocation loc = Game1.player.currentLocation;

                for (int x = 0; x < loc.Map.DisplayWidth / Game1.tileSize; x++)
                {
                    for (int y = 0; y < loc.Map.DisplayHeight / Game1.tileSize; y++)
                    {
                        Vector2 tile = new Vector2(x, y);

                        var pot = new IndoorPot(tile);

                        if (!loc.objects.ContainsKey(tile))
                        {
                            return 1;
                        }
                    }
                }
            }

            if (Butt == "Till")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Till])
                    return 0;

                GameLocation currentLocation = Game1.player.currentLocation;

                for (int x = 0; x < currentLocation.Map.DisplayWidth / Game1.tileSize; x++)
                {
                    for (int y = 0; y < currentLocation.Map.DisplayHeight / Game1.tileSize; y++)
                    {
                        if (IsTillable(currentLocation, x, y))
                        {
                            Vector2 tile = new Vector2(x, y);

                            if (CountSeedsAllowedHere(currentLocation, tile) > 0)
                                return 1;
                        }
                    }
                }
            }

            if (Butt == "Water")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Water])
                    return 0;

                var candidateTiles = new List<(GameLocation loc, Vector2 tile)>();

                    foreach (GameLocation loc in Game1.locations)
                    {
                    for (int x = 0; x < loc.Map.DisplayWidth / Game1.tileSize; x++)
                    {
                        for (int y = 0; y < loc.Map.DisplayHeight / Game1.tileSize; y++)
                        {
                            Vector2 tile = new Vector2(x, y);

                            if (IsWaterableTile(loc, tile))
                                return 1;
                        }
                    }
                }
            }

            if (Butt == "Fertilize")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Fertilize])
                    return 0;

                GameLocation currentLocation = Game1.player.currentLocation;

                for (int x = 0; x < currentLocation.Map.DisplayWidth / Game1.tileSize; x++)
                {
                    for (int y = 0; y < currentLocation.Map.DisplayHeight / Game1.tileSize; y++)
                    {
                        Vector2 tile = new Vector2(x, y);

                        if (IsFertilizableTile(currentLocation, tile) && CountFertilizer() > 1)
                        {
                            Count += 1;
                            if (Count > 0)
                                return Count;
                        }
                    }
                }
            }

            if (Butt == "Seeds")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Seeds])
                    return 0;

                foreach (var item in Game1.player.Items.OfType<SObject>())
                {
                    if (item.Category == SObject.SeedsCategory)
                        return 1;
                }
            }

            if (Butt == "PlaceTappers")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.PlaceTappers])
                    return 0;

                GameLocation currentLocation = Game1.player.currentLocation;

                if (!IsStableLocation(currentLocation))
                    return 0;

                var candidateTrees = new List<Tree>();

                foreach (var pair in currentLocation.terrainFeatures.Pairs)
                {
                    if (pair.Value is Tree t)
                    {
                        if (t.growthStage.Value >= 5 && !t.tapped.Value)
                        {
                            if (IsWithinScanCone(Game1.player, pair.Key))
                                return 1;
                        }
                    }
                }
            }

            if (Butt == "PlaceBeeHouse")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.PlaceBeeHouse])
                    return 0;

                GameLocation currentLocation = Game1.player.currentLocation;

                if (!IsStableLocation(currentLocation))
                    return 0;

                var candidateTiles = new List<Vector2>();

                for (int x = 0; x < currentLocation.Map.DisplayWidth / Game1.tileSize; x++)
                {
                    for (int y = 0; y < currentLocation.Map.DisplayHeight / Game1.tileSize; y++)
                    {
                        Vector2 tile = new Vector2(x, y);

                        if (!currentLocation.IsOutdoors)
                            return 0;

                        if (!IsWithinScanCone(Game1.player, tile))
                            continue;

                        var beeHouse = new SObject(tile, "10", false);
                        if (CanPlaceObjectHere(currentLocation, tile, beeHouse))

                            return 1;
                    }
                }
            }

            if (Butt == "SeedMaker")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.SeedMaker])
                    return 0;

                var cropStacks = Game1.player.Items
                    .OfType<SObject>()
                    .Where(o => (o.Category == -75 || o.Category == -79))
                    .Where(o => o.Stack > 0)
                    .Where(o => GetSeedIdForCrop(o) != null)
                    .ToList();

                if (cropStacks.Count > 0)
                    return 1;
            }

            if (Butt == "Sashimi")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Sashimi])
                    return 0;

                var junkFishStacks = Game1.player.Items
                    .OfType<SObject>()
                    .Where(o =>
                        o.Category == SObject.FishCategory &&
                        o.Stack > 0 &&
                        !o.HasContextTag("legendary_fish")
                    )
                    .ToList();

                if (junkFishStacks.Count > 0)
                    return 1;
            }

            if (Butt == "Ore")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Ore])
                    return 0;

                var ore = Game1.player.Items
                    .OfType<SObject>()
                    .Where(o => IsOre(o) && o.Stack >= 5)
                    .ToList();

                if (ore.Count >= 1)
                    return 1;
                
            }

            if (Butt == "Animals")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Animals])
                    return 0;

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

                if(animalsToPet + troughsToFill > 0)
                    return 1;
            }

            if (Butt == "Juice")
            {
                if (Game1.player.Money < Config.SeviceContractFees[ServiceId.Juice])
                    return 0;

                var ProduceStacks = Game1.player.Items
                .OfType<SObject>()
                .Where(o =>
                    (o.Category == SObject.FruitsCategory ||
                     o.Category == SObject.VegetableCategory ||
                     o.HasContextTag("forage_item")) &&
                    o.Stack > 0
                )
                .ToList();

                if (ProduceStacks.Count > 0)
                    return 1;
            }

            if (Butt == "AnimalProducts")
            {
                if ( Game1.player.Money < Config.SeviceContractFees[ServiceId.AnimalProducts])
                    return 0;

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

                var AnimalRaw = Game1.player.Items
                    .OfType<SObject>()
                    .Where(o => RawToProductMap.ContainsKey(o.ItemId))
                    .FirstOrDefault();

                bool anyGrabberHasRaw = autograbbers
                    .Select(p => p.Value.heldObject.Value as Chest)
                    .Where(chest => chest != null)
                    .Any(chest => chest.Items
                        .OfType<SObject>()
                        .Any(o => RawToProductMap.ContainsKey(o.ItemId))
                    );

                if (anyGrabberHasRaw)
                    return 1;

                if (AnimalRaw != null)
                    return 1;
            }
            return Count;
        } 
    }
}
