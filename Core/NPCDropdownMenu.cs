using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using static CommunityContracts.Core.ContractUtilities;
using static CommunityContracts.Core.NPCServiceMenu;
using static ModEntry;
using SDV_NPC = StardewValley.NPC;

namespace CommunityContracts.Core
{
    public class NPCDropdownMenu : IClickableMenu
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        public static ModConfig config;

        private List<string> npcNames;

        public void BuildNpcList()
        {
            var all = Utility.getAllCharacters() ?? new List<SDV_NPC>();

            npcNames = all
                .Where(npc =>
                    npc is SDV_NPC &&
                    npc.CanSocialize &&
                    !npc.IsMonster &&
                    Game1.player.friendshipData.ContainsKey(npc.Name))
                .Select(npc => npc.Name)
                .Distinct()
                .ToList();

            npcNames ??= new List<string>();
        }

        private List<NPCMenuOption> options = new List<NPCMenuOption>();
        private Dictionary<string, Texture2D> npcPortraits = new();
        private int selectedIndex = -1;
        private const int ButtonWidth = 160;
        private const int ButtonHeight = 74;
        private const int HSpacing = 10;
        private const int WSpacing = 120;
        public static int CurrentFriendship { get; set; } = 0;
        public static int NPCLevel { get; set; } = 0;
        private static string ReturnToolbarTooltip => T("ReturnToolbarTooltip");
        private ClickableComponent ReturnToolbarButton;

        int WarpFee = Config.SeviceContractFees[ServiceId.Warp];
        public class NPCMenuOption
        {
            public string name;
            public ClickableComponent nameButton;
            public Rectangle portraitRect;
            public int Friendship;
            public int Level;
        }
        public NPCDropdownMenu(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            Helper = helper;
            Monitor = monitor;
            Config = config;
            
            int startX = 120;
            int startY = 120;

            BuildNpcList();

            foreach (var name in npcNames)
            {
                try
                {
                    npcPortraits[name] = Game1.content.Load<Texture2D>($"Portraits/{name}");
                }
                catch
                {

                }
            }

            npcNames = npcNames
                .OrderByDescending(n =>
                    Game1.player.friendshipData.TryGetValue(n, out var data) ? data.Points : 0
                )
                .ToList();
            for (int i = 0; i < npcNames.Count; i++)
            {
                string name = npcNames[i];
                SDV_NPC npc = Game1.getCharacterFromName(name, mustBeVillager: false);

                if (npc == null || npc.currentLocation == null || npc.Position == Vector2.Zero)
                {
                    Instance.Monitor.Log(T("SkippingNPC", new { npc = name }), LogLevel.Trace);
                    continue;
                }

                int col = options.Count % Config.MenuColumns;
                int row = options.Count / Config.MenuColumns;

                int x = startX + col * (ButtonWidth + WSpacing);
                int y = startY + row * (ButtonHeight + HSpacing);

                int friendship = Game1.player.friendshipData.TryGetValue(name, out var data) ? data.Points : 0;
                int level = UpdateNPCLevel(name);

                var nameButton = new ClickableComponent(new Rectangle(x + 4, y, ButtonWidth, ButtonHeight), name);
                var portraitRect = new Rectangle(x - 70, y, 72, 72);

                options.Add(new NPCMenuOption
                {
                    name = name,
                    nameButton = nameButton,
                    portraitRect = portraitRect,

                    Friendship = friendship,
                    Level = level
                });
            }

            int buttonX = startX + (((ButtonWidth + WSpacing) * Config.MenuColumns) / 2) - 220;
            int buttonY = startY - 70;

            ReturnToolbarButton = new ClickableComponent(
                new Rectangle(buttonX, buttonY, 440, 60),
                "ReturnToolbar"
            );
        }
        public override void draw(SpriteBatch b)
        {
            int framePadding = 20;
            int frameWidth = (ButtonWidth + WSpacing) * Config.MenuColumns + framePadding * 6 - 80;
            int frameHeight = ((npcNames.Count + Config.MenuColumns) / Config.MenuColumns) * (ButtonHeight + HSpacing * 2) + framePadding * 2 + 120;
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

            string text = T("ReturnToolbar");
            Vector2 textSize = Game1.smallFont.MeasureString(text);

            float textX = ReturnToolbarButton.bounds.X + (ReturnToolbarButton.bounds.Width / 2f) - (textSize.X / 2f);
            float textY = ReturnToolbarButton.bounds.Y + (ReturnToolbarButton.bounds.Height / 2f) - (textSize.Y / 2f);

            Utility.drawTextWithShadow(
                b,
                text,
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

            base.draw(b);

            foreach (var option in options)
            {
                Color boxColor = option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ? Color.Gold : Color.White;

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

                if (npcPortraits.TryGetValue(option.name, out var portrait))
                {
                    Rectangle sourceRect = new Rectangle(0, 0, 64, 64);
                    b.Draw(portrait, option.portraitRect, sourceRect, Color.White);
                }

                Vector2 nameSize = Game1.smallFont.MeasureString(option.name);

                float nameX = option.nameButton.bounds.X + (option.nameButton.bounds.Width / 2f) - (nameSize.X / 2f);
                float nameY = option.nameButton.bounds.Y + 8;

                Utility.drawTextWithShadow(
                    b,
                    option.name,
                    Game1.smallFont,
                    new Vector2(nameX, nameY),
                    Game1.textColor
                );

                string infoLine = $"{option.Level} /  {option.Friendship}";

                Vector2 infoSize = Game1.smallFont.MeasureString(infoLine);

                float infoX = option.nameButton.bounds.X + (option.nameButton.bounds.Width / 2f) - (infoSize.X / 2f);
                float infoY = option.nameButton.bounds.Y + 32;

                Utility.drawTextWithShadow(
                    b,
                    infoLine,
                    Game1.smallFont,
                    new Vector2(infoX, infoY),
                    Game1.textColor * 0.85f
                );
            }

            drawMouse(b);

            foreach (var option in options)
            {
                if (option.portraitRect.Contains(Game1.getMouseX(), Game1.getMouseY()))
                {
                    CurrentFriendship = Game1.player.friendshipData.TryGetValue(option.name, out var data) ? data.Points : 0;
                    string tooltip = T("Warp", new { npc = option.name, Fee = WarpFee });

                    drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont
                    );
                    break;
                }
            }

            foreach (var option in options)
            {
                if (option.nameButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
                {
                    CurrentFriendship = Game1.player.friendshipData.TryGetValue(option.name, out var data) ? data.Points : 0;
                    string tooltip = T("GoToMenu", new { npc = option.name, points = CurrentFriendship });

                    drawHoverText(
                        b,
                        tooltip,
                        Game1.smallFont,
                        xOffset: -30
                    );
                    break;
                }
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
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            foreach (var option in options)
            {
                string npcName = option.name;
                SDV_NPC npc = Game1.getCharacterFromName(npcName, mustBeVillager: false);

                if (option.portraitRect.Contains(x, y))
                {
                    if (Game1.player.Money >= WarpFee)
                    {
                        Vector2 tile = npc.Position / Game1.tileSize;
                        Game1.warpFarmer(npc.currentLocation.Name, (int)tile.X, (int)tile.Y, false);
                        Game1.player.Money -= WarpFee;
                        Game1.exitActiveMenu();
                        Game1.playSound("wand");
                        return;
                    }
                    else
                        return;
                }

                if (option.nameButton.containsPoint(x, y))
                {
                    Game1.exitActiveMenu();
                    Game1.activeClickableMenu = new NPCServiceMenu(npcName);
                    return;
                }
                
                if (ReturnToolbarButton.containsPoint(x, y))
                {
                    Game1.exitActiveMenu();
                    Game1.activeClickableMenu = new CCToolbar(Helper, Monitor, Config);
                    return;
                }
            }

            base.receiveLeftClick(x, y, playSound);
        }
    }
}
