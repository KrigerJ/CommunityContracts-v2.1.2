using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace CommunityContracts.Core
{
    internal class DirectionMenu : IClickableMenu
    {
        private readonly List<DirectionPickupTarget> targets;

        private readonly ClickableTextureComponent grabAllButton;
        private readonly ClickableTextureComponent cancelButton;

        private readonly int listStartY;
        private readonly int listItemHeight = 64;

        public DirectionMenu(List<DirectionPickupTarget> targets)
            : base(
                Game1.viewport.Width / 2 - 300,
                Game1.viewport.Height / 2 - 200,
                600,
                400,
                showUpperRightCloseButton: false
            )
        {
            this.targets = targets;

            listStartY = yPositionOnScreen + 80;

            grabAllButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + 80, yPositionOnScreen + height - 90, 128, 64),
                Game1.mouseCursors,
                new Rectangle(128, 256, 64, 64),
                2f
            );

            cancelButton = new ClickableTextureComponent(
                new Rectangle(xPositionOnScreen + 360, yPositionOnScreen + height - 90, 128, 64),
                Game1.mouseCursors,
                new Rectangle(192, 256, 64, 64),
                2f
            );
        }

        public override void draw(SpriteBatch b)
        {
            drawBackground(b);

            SpriteText.drawStringHorizontallyCenteredAt(
                b,
                "Pick Up Items",
                xPositionOnScreen + width / 2,
                yPositionOnScreen + 20
            );

            int y = listStartY;

            foreach (var t in targets)
            {
                b.Draw(
                    Game1.objectSpriteSheet,
                    new Vector2(xPositionOnScreen + 60, y),
                    Game1.getSourceRectForStandardTileSheet(
                        Game1.objectSpriteSheet,
                        t.Object.ParentSheetIndex,
                        16,
                        16
                    ),
                    Color.White,
                    0f,
                    Vector2.Zero,
                    3f,
                    SpriteEffects.None,
                    1f
                );

                SpriteText.drawString(
                    b,
                    t.Object.DisplayName,
                    xPositionOnScreen + 140,
                    y + 20
                );

                y += listItemHeight;
            }

            grabAllButton.draw(b);
            cancelButton.draw(b);

            drawMouse(b);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (grabAllButton.containsPoint(x, y))
            {
                Game1.playSound("coin");
                Game1.exitActiveMenu();
            }

            if (cancelButton.containsPoint(x, y))
            {
                Game1.playSound("bigDeSelect");
                Game1.exitActiveMenu();
            }
        }
    }
}