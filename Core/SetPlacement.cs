
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using static ModEntry;

namespace CommunityContracts.Core
{
    public class SetPlacement : IClickableMenu
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        private readonly ModConfig Config;

        private readonly List<ClickableComponent> menuButtons = new();

        private TextBox widthBox;
        private TextBox heightBox;

        private ClickableComponent widthBoxClickable;
        private ClickableComponent heightBoxClickable;

        private ClickableTextureComponent widthMinusButton;
        private ClickableTextureComponent widthPlusButton;

        private ClickableTextureComponent heightMinusButton;
        private ClickableTextureComponent heightPlusButton;
        private static string ReturnToolbarTooltip => T("ReturnToolbarTooltip");
        private ClickableComponent ReturnToolbarButton;
        private static string ReturnCharactersTooltip => T("ReturnCharactersTooltip");
        private ClickableComponent ReturnCharactersButton;

        public SetPlacement(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            Helper = helper;
            Monitor = monitor;
            Config = config;

            int startX = 20;
            int startY = 20;
            int boxX = startX + 70;
            int widthBoxY = startY + 50;

            widthBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )
            {
                X = boxX,
                Y = widthBoxY,
                Width = 80,
                Text = Config.RectangleWidth.ToString()
            };

            widthBoxClickable = new ClickableComponent(
                new Rectangle(boxX, widthBoxY, 80, 42),
                "WidthBox"
            );

            heightBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )
            {
                X = boxX + 220,
                Y = widthBoxY,
                Width = 80,
                Text = Config.RectangleLength.ToString()
            };

            heightBoxClickable = new ClickableComponent(
                new Rectangle(boxX + 220, widthBoxY, 80, 42),
                "HeightBox"
            );

            menuButtons.Add(widthBoxClickable);
            menuButtons.Add(heightBoxClickable);

            int buttonSize = 30;

            widthMinusButton = new ClickableTextureComponent(
                new Rectangle(widthBox.X - buttonSize + 1, widthBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(100, 245, 13, 15),
                2f
            );

            widthPlusButton = new ClickableTextureComponent(
                new Rectangle(widthBox.X + widthBox.Width + 4, widthBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(0, 410, 15, 15),
                2f
            );

            heightMinusButton = new ClickableTextureComponent(
                new Rectangle(heightBox.X - buttonSize + 1, heightBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(100, 245, 13, 15),
                2f
            );

            heightPlusButton = new ClickableTextureComponent(
                new Rectangle(heightBox.X + heightBox.Width + 4, heightBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(0, 410, 15, 15),
                2f
            );

            menuButtons.Add(widthMinusButton);
            menuButtons.Add(widthPlusButton);
            menuButtons.Add(heightMinusButton);
            menuButtons.Add(heightPlusButton);

            ShowPlacementOverlay = true;

            int buttonX = startX + 520;
            int buttonY = startY + 25;

            ReturnToolbarButton = new ClickableComponent(
                new Rectangle(buttonX, buttonY, 440, 60),
                "ReturnToolbar"
            );

            ReturnCharactersButton = new ClickableComponent(
                new Rectangle(buttonX + 300, buttonY, 440, 60),
                "ReturnCharacters"
            );
        }
        public override void draw(SpriteBatch b)
        {
            int framePadding = 20;
            int frameWidth = 1200;
            int frameHeight = 110;
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

            base.draw(b);

            Utility.drawTextWithShadow(
                b,
                T("RectWidthLabel"),
                Game1.smallFont,
                new Vector2(widthBox.X - 42, widthBox.Y - 34),
                Game1.textColor
            );

            widthBox.Draw(b, false);
            heightBox.Draw(b, false);

            widthMinusButton.draw(b);
            widthPlusButton.draw(b);

            heightMinusButton.draw(b);
            heightPlusButton.draw(b);

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

            if (widthBoxClickable.containsPoint(x, y))
            {
                widthBox.Selected = true;
                heightBox.Selected = false;
                Game1.keyboardDispatcher.Subscriber = widthBox;
            }
            else if (heightBoxClickable.containsPoint(x, y))
            {
                heightBox.Selected = true;
                widthBox.Selected = false;
                Game1.keyboardDispatcher.Subscriber = heightBox;
            }
            else
            {
                widthBox.Selected = false;
                heightBox.Selected = false;
            }

            if (widthMinusButton.containsPoint(x, y))
            {
                Config.RectangleWidth = Math.Max(1, Config.RectangleWidth - 2);
                widthBox.Text = Config.RectangleWidth.ToString();
                Game1.playSound("smallSelect");
            }

            if (widthPlusButton.containsPoint(x, y))
            {
                Config.RectangleWidth = Math.Min(200, Config.RectangleWidth + 2);
                widthBox.Text = Config.RectangleWidth.ToString();
                Game1.playSound("smallSelect");
            }

            if (heightMinusButton.containsPoint(x, y))
            {
                Config.RectangleLength = Math.Max(1, Config.RectangleLength - 1);
                heightBox.Text = Config.RectangleLength.ToString();
                Game1.playSound("smallSelect");
            }

            if (heightPlusButton.containsPoint(x, y))
            {
                Config.RectangleLength = Math.Min(200, Config.RectangleLength + 1);
                heightBox.Text = Config.RectangleLength.ToString();
                Game1.playSound("smallSelect");
            }

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

            Instance.Helper.WriteConfig(Config);
        }
        public override void update(GameTime time)
        {
            base.update(time);

            if (int.TryParse(widthBox.Text, out int w))
                Config.RectangleWidth = Math.Clamp(w, 1, 200);

            if (int.TryParse(heightBox.Text, out int h))
                Config.RectangleLength = Math.Clamp(h, 1, 200);

            Instance.Helper.WriteConfig(Config);
        }
    }
}
