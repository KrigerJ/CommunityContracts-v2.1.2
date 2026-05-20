using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using static ModEntry;

namespace CommunityContracts.Core
{
    public class Settings : IClickableMenu
    {
        private readonly IModHelper Helper;
        private readonly IMonitor Monitor;
        private readonly ModConfig Config;

        private readonly List<ClickableComponent> menuButtons = new();

        private TextBox delayBox;

        private ClickableComponent delayBoxClickable;

        private ClickableTextureComponent delayMinusButton;
        private ClickableTextureComponent delayPlusButton;
        private static string DelayTooltip => T("DelayTooltip");

        private static string ReturnToolbarTooltip => T("ReturnToolbarTooltip");
        private ClickableComponent ReturnToolbarButton;
        private static string ReturnCharactersTooltip => T("ReturnCharactersTooltip");
        private ClickableComponent ReturnCharactersButton;

        private TextBox HotkeyBox;
        private ClickableComponent HotKeyBoxClickable;
        private static string HotkeyTooltip => T("HotkeyTooltip");

        private ClickableTextureComponent TimeReductionCheckbox;
        private static string ReductionTooltip => T("ReductionTooltip");

        private TextBox ColumnBox;
        private ClickableComponent ColumnBoxClickable;
        private ClickableTextureComponent ColumnMinusButton;
        private ClickableTextureComponent ColumnPlusButton;
        private static string ColumnTooltip => T("ColumnTooltip");

        private ClickableTextureComponent ChestColorButton;
        private List<string> ChestColorNames;
        private int ChestColorIndex;
        private static string ChestColorTooltip => T("ChestColorTooltip");

        private ClickableTextureComponent HighlightColorButton;
        private List<string> HighlightColorNames;
        private int HighlightColorIndex;
        private static string HighlightColorTooltip => T("HighlightColorTooltip");

        private ClickableTextureComponent FontColorButton;
        private List<string> FontColorNames;
        private int FontColorIndex;
        private static string FontColorTooltip => T("FontColorTooltip");
        public Settings(IModHelper helper, IMonitor monitor, ModConfig config)
        {
            Helper = helper;
            Monitor = monitor;
            Config = config;

            int startX = 20;
            int startY = 20;

            int buttonX = startX + 100;
            int buttonY = startY + 20;

            ReturnToolbarButton = new ClickableComponent(
                new Rectangle(buttonX, buttonY, 440, 60),
                "ReturnToolbar"
            );

            ReturnCharactersButton = new ClickableComponent(
                new Rectangle(buttonX + 300, buttonY, 440, 60),
                "ReturnCharacters"
            );

            int HotkeyBoxX = startX + 60;
            int HotkeyBoxY = startY + 110;

            HotkeyBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )

            {
                X = HotkeyBoxX,
                Y = HotkeyBoxY,
                Width = 50,
                Text = Config.CheatMenuHotkey.ToString()
            };
            
            HotkeyBox.OnEnterPressed += (tb) =>
            {
                ApplyHotkeyChange();
                HotkeyBox.Selected = false;
            };
            
            HotKeyBoxClickable = new ClickableComponent(
                new Rectangle(HotkeyBoxX, HotkeyBoxY, 60, 42),
                "HotkeyBox"
            );

            menuButtons.Add(HotKeyBoxClickable);

            int ReductionX = startX + 66;
            int ReductionY = startY + 175;

            TimeReductionCheckbox = new ClickableTextureComponent(
                new Rectangle(ReductionX, ReductionY, 64, 64),
                Game1.mouseCursors,
                new Rectangle(227, 425, 9, 9), 
                4f
            )
            {
                name = "TimeReduction"
            };

            int delayBoxX = startX + 90;
            int delayBoxY = startY + 236;

            delayBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )
            {
                X = delayBoxX,
                Y = delayBoxY,
                Width = 80,
                Text = Config.CollectionDelay.ToString()
            };

            delayBox.OnEnterPressed += (tb) =>
            {
                if (int.TryParse(delayBox.Text, out int w))
                {
                    Config.CollectionDelay = w;
                    Game1.showGlobalMessage(T("DelaySet", new { Delay = w }));
                    Helper.WriteConfig(Config);
                }
                delayBox.Selected = false;
            };

            delayBoxClickable = new ClickableComponent(
                new Rectangle(delayBoxX, delayBoxY, 80, 40),
                "delayBox"
            );

            menuButtons.Add(delayBoxClickable);

            int buttonSize = 30;

            delayMinusButton = new ClickableTextureComponent(
                new Rectangle(delayBox.X - buttonSize + 1, delayBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(100, 245, 13, 15),
                2f
            );

            delayPlusButton = new ClickableTextureComponent(
                new Rectangle(delayBox.X + delayBox.Width + 4, delayBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(0, 410, 15, 15),
                2f
            );

            menuButtons.Add(delayMinusButton);
            menuButtons.Add(delayPlusButton);

            int ColumnBoxX = startX + 90;
            int ColumnBoxY = startY + 300;

            ColumnBox = new TextBox(
                Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
                null,
                Game1.smallFont,
                Game1.textColor
            )
            {
                X = ColumnBoxX,
                Y = ColumnBoxY,
                Width = 40,
                Text = Config.MenuColumns.ToString()
            };

            ColumnBox.OnEnterPressed += (tb) =>
            {
                if (int.TryParse(ColumnBox.Text, out int w))
                {
                    Config.MenuColumns = w;
                    Game1.showGlobalMessage(T("ColumnSet", new { Column = w }));
                    Helper.WriteConfig(Config);
                }
                ColumnBox.Selected = false;
            };

            ColumnBoxClickable = new ClickableComponent(
                new Rectangle(ColumnBoxX, ColumnBoxY, 40, 40),
                "ColumnBox"
            );

            menuButtons.Add(ColumnBoxClickable);


            ColumnMinusButton = new ClickableTextureComponent(
                new Rectangle(ColumnBox.X - buttonSize + 1, ColumnBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(100, 245, 13, 15),
                2f
            );

            ColumnPlusButton = new ClickableTextureComponent(
                new Rectangle(ColumnBox.X + ColumnBox.Width + 4, ColumnBox.Y + 5, buttonSize, buttonSize),
                Game1.mouseCursors,
                new Rectangle(0, 410, 15, 15),
                2f
            );

            menuButtons.Add(ColumnMinusButton);
            menuButtons.Add(ColumnPlusButton);

            ChestColorNames = Config.ChestColors.Keys.ToList();

            ChestColorIndex = ChestColorNames.IndexOf(Config.DeliveryChestColor);
            if (ChestColorIndex < 0)
                ChestColorIndex = 0;

            ChestColorButton = new ClickableTextureComponent(
                new Rectangle(startX + 60, startY + 360, 200, 60),
                Game1.mouseCursors,
                new Rectangle(0, 0, 1, 1),
                1f
            );

            HighlightColorNames = Config.HighlightColors.Keys.ToList();

            HighlightColorIndex = HighlightColorNames.IndexOf(Config.HighlightColor);
            if (HighlightColorIndex < 0)
                HighlightColorIndex = 0;

            HighlightColorButton = new ClickableTextureComponent(
                new Rectangle(startX + 60, startY + 430, 200, 60),
                Game1.mouseCursors,
                new Rectangle(0, 0, 1, 1),
                1f
            );

            FontColorNames = Config.FontColors.Keys.ToList();

            FontColorIndex = FontColorNames.IndexOf(Config.FontColor);
            if (FontColorIndex < 0)
                FontColorIndex = 0;

            FontColorButton = new ClickableTextureComponent(
                new Rectangle(startX + 60, startY + 500, 200, 60),
                Game1.mouseCursors,
                new Rectangle(0, 0, 1, 1),
                1f
            );
        }
        public override void draw(SpriteBatch b)
        {
            int framePadding = 20;
            int frameWidth = 700;
            int frameHeight = 640;
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
                T("HotkeyBoxLabel"),
                Game1.smallFont,
                new Vector2(HotkeyBox.X + 120, HotkeyBox.Y + 8),
                Game1.textColor
            );

            HotkeyBox.Draw(b, false);

            Utility.drawTextWithShadow(
                b,
                T("ReductionLabel"),
                Game1.smallFont,
                new Vector2(TimeReductionCheckbox.bounds.X + 120, TimeReductionCheckbox.bounds.Y + 6),
                Game1.textColor
            );

            Utility.drawTextWithShadow(
                b,
                T("DelayLabel"),
                Game1.smallFont,
                new Vector2(delayBox.X + 140, delayBox.Y + 8),
                Game1.textColor
            );

            delayBox.Draw(b, false);

            delayMinusButton.draw(b);
            delayPlusButton.draw(b);

            Utility.drawTextWithShadow(
                b,
                T("ColumnLabel"),
                Game1.smallFont,
                new Vector2(ColumnBox.X + 140, ColumnBox.Y + 8),
                Game1.textColor
            );

            ColumnBox.Draw(b, false);

            ColumnMinusButton.draw(b);
            ColumnPlusButton.draw(b);

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

            int ReturnToolbaradjusteddelay = (int)ReturnToolbarTextSize.X + 40;

            ReturnToolbarButton.bounds = new Rectangle(
                ReturnToolbarButton.bounds.X,
                ReturnToolbarButton.bounds.Y,
                ReturnToolbaradjusteddelay,
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

            int ReturnCharactersadjusteddelay = (int)ReturnCharactersTextSize.X + 40;

            ReturnCharactersButton.bounds = new Rectangle(
                ReturnCharactersButton.bounds.X,
                ReturnCharactersButton.bounds.Y,
                ReturnCharactersadjusteddelay,
                ReturnCharactersButton.bounds.Height
            );

            TimeReductionCheckbox.draw(b);

            if (Config.EnableProcessTimeReduction)
            {
                b.Draw(
                    Game1.mouseCursors,
                    new Vector2(TimeReductionCheckbox.bounds.X + 0, TimeReductionCheckbox.bounds.Y + 0),
                    new Rectangle(236, 425, 9, 9),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    1f
                );
            }

            Utility.drawTextWithShadow(
                b,
                T("ChestColorLabel"),
                Game1.smallFont,
                new Vector2(ChestColorButton.bounds.X + 220, ChestColorButton.bounds.Y + 12),
                Game1.textColor
            );

            IClickableMenu.drawTextureBox(
                b,
                ChestColorButton.bounds.X,
                ChestColorButton.bounds.Y,
                ChestColorButton.bounds.Width,
                ChestColorButton.bounds.Height,
                Color.White
            );

            string name = ChestColorNames[ChestColorIndex];
            b.DrawString(
                Game1.smallFont,
                name,
                new Vector2(ChestColorButton.bounds.X + 16, ChestColorButton.bounds.Y + 16),
                Color.Black
            );

            Color preview = Config.ChestColors[name];

            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    ChestColorButton.bounds.Right - 60,
                    ChestColorButton.bounds.Y + 12,
                    44,
                    38
                ),
                preview
            );

            Utility.drawTextWithShadow(
                b,
                T("HighlightColorLabel"),
                Game1.smallFont,
                new Vector2(HighlightColorButton.bounds.X + 220, HighlightColorButton.bounds.Y + 12),
                Game1.textColor
            );

            drawTextureBox(
                b,
                HighlightColorButton.bounds.X,
                HighlightColorButton.bounds.Y,
                HighlightColorButton.bounds.Width,
                HighlightColorButton.bounds.Height,
                Color.White
            );

            string HCname = HighlightColorNames[HighlightColorIndex];
            b.DrawString(
                Game1.smallFont,
                HCname,
                new Vector2(HighlightColorButton.bounds.X + 16, HighlightColorButton.bounds.Y + 16),
                Color.Black
            );

            Color HCpreview = Config.HighlightColors[HCname];

            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    HighlightColorButton.bounds.Right - 60,
                    HighlightColorButton.bounds.Y + 12,
                    44,
                    38
                ),
                HCpreview
            );

            Utility.drawTextWithShadow(
                b,
                T("FontColorLabel"),
                Game1.smallFont,
                new Vector2(FontColorButton.bounds.X + 220, FontColorButton.bounds.Y + 12),
                Game1.textColor
            );

            drawTextureBox(
                b,
                FontColorButton.bounds.X,
                FontColorButton.bounds.Y,
                FontColorButton.bounds.Width,
                FontColorButton.bounds.Height,
                Color.White
            );

            string FCname = FontColorNames[FontColorIndex];
            b.DrawString(
                Game1.smallFont,
                FCname,
                new Vector2(FontColorButton.bounds.X + 16, FontColorButton.bounds.Y + 16),
                Color.Black
            );

            Color FCpreview = Config.FontColors[FCname];

            b.Draw(
                Game1.staminaRect,
                new Rectangle(
                    FontColorButton.bounds.Right - 60,
                    FontColorButton.bounds.Y + 12,
                    44,
                    38
                ),
                FCpreview
            );

            drawMouse(b);

            if (ReturnToolbarButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReturnToolbarTooltip,
                    Game1.smallFont,
                    xOffset: -100
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

            if (HotKeyBoxClickable.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    HotkeyTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (delayBoxClickable.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    DelayTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (TimeReductionCheckbox.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ReductionTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (ColumnBoxClickable.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ColumnTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (ChestColorButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    ChestColorTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (HighlightColorButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    HighlightColorTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }

            if (FontColorButton.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
            {
                drawHoverText(
                    b,
                    FontColorTooltip,
                    Game1.smallFont,
                    xOffset: -10
                );
            }
        }
        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (HotKeyBoxClickable.containsPoint(x, y))
            {
                HotkeyBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = HotkeyBox;
            }

            else
            {
                if (HotkeyBox.Selected)
                   ApplyHotkeyChange();

                HotkeyBox.Selected = false;
            }

            if (delayBoxClickable.containsPoint(x, y))
            {
                delayBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = delayBox;
            }
            else
            {
                delayBox.Selected = false;
            }

            if (delayMinusButton.containsPoint(x, y))
            {
                Config.CollectionDelay = Math.Max(100, Config.CollectionDelay - 100);
                delayBox.Text = Config.CollectionDelay.ToString();
                Game1.playSound("smallSelect");
            }

            if (delayPlusButton.containsPoint(x, y))
            {
                Config.CollectionDelay = Math.Min(3000, Config.CollectionDelay + 100);
                delayBox.Text = Config.CollectionDelay.ToString();
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

            if (TimeReductionCheckbox.containsPoint(x, y))
            {
                Config.EnableProcessTimeReduction = !Config.EnableProcessTimeReduction;
                Game1.playSound("drumkit6");

                if (Config.EnableProcessTimeReduction)
                {
                    ContractUtilities.ApplyProductionTimeReduction(Config, Instance.Monitor);
                }

                Helper.WriteConfig(Config);
            }

            if (ColumnBoxClickable.containsPoint(x, y))
            {
                ColumnBox.Selected = true;
                Game1.keyboardDispatcher.Subscriber = ColumnBox;
            }
            else
            {
                ColumnBox.Selected = false;
            }

            if (ColumnMinusButton.containsPoint(x, y))
            {
                Config.MenuColumns = Math.Max(3, Config.MenuColumns - 1);
                ColumnBox.Text = Config.MenuColumns.ToString();
                Game1.playSound("smallSelect");
            }

            if (ColumnPlusButton.containsPoint(x, y))
            {
                Config.MenuColumns = Math.Min(8, Config.MenuColumns + 1);
                ColumnBox.Text = Config.MenuColumns.ToString();
                Game1.playSound("smallSelect");
            }

            if (ChestColorButton.containsPoint(x, y))
            {
                ChestColorIndex++;

                if (ChestColorIndex >= ChestColorNames.Count)
                    ChestColorIndex = 0;

                string selected = ChestColorNames[ChestColorIndex];
                Config.DeliveryChestColor = selected;

                Game1.playSound("smallSelect");

                Helper.WriteConfig(Config);
            }

            if (HighlightColorButton.containsPoint(x, y))
            {
                HighlightColorIndex++;

                if (HighlightColorIndex >= HighlightColorNames.Count)
                    HighlightColorIndex = 0;

                string selected = HighlightColorNames[HighlightColorIndex];
                Config.HighlightColor = selected;

                Game1.playSound("smallSelect");

                Helper.WriteConfig(Config);
            }

            if (FontColorButton.containsPoint(x, y))
            {
                FontColorIndex++;

                if (FontColorIndex >= FontColorNames.Count)
                    FontColorIndex = 0;

                string selected = FontColorNames[FontColorIndex];
                Config.FontColor = selected;

                Game1.playSound("smallSelect");

                Helper.WriteConfig(Config);
            }
        }
        public override void update(GameTime time)
        {
            base.update(time);

            if (int.TryParse(delayBox.Text, out int w))
            { 
                Config.CollectionDelay = Math.Clamp(w, 100, 3000);
                Helper.WriteConfig(Config);
            }

            if (int.TryParse(ColumnBox.Text, out int Q))
            {
                Config.MenuColumns = Math.Clamp(Q, 1, 9);
                Helper.WriteConfig(Config);
            }
        }
        private void ApplyHotkeyChange()
        {
            HotkeyBox.Text = HotkeyBox.Text.ToUpperInvariant();

            if (Enum.TryParse(HotkeyBox.Text, true, out SButton newKey))
            {
                Config.CheatMenuHotkey = newKey;
                Helper.WriteConfig(Config);
                Game1.showGlobalMessage(T("HotKeySet", new { Key = newKey }));
            }
            else
            {
                Game1.showRedMessage(T("InvalidKey"));
            }
        }
    }
}
