using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using static ModEntry;

namespace CommunityContracts.Core
{
    internal static class DirectionHelper
    {
        public static List<Vector2> SortTilesForPickup(Farmer farmer, bool excludeBehind = false)
        {
            return SortTilesCore(farmer, excludeBehind);
        }
        private static List<Vector2> SortTilesCore(Farmer farmer, bool excludeBehind)
        {
            int radius = Config.ConeRange;
            Vector2 farmerTile = farmer.Tile;

            Vector2 facingDir = farmer.FacingDirection switch
            {
                0 => new Vector2(0, -1),
                1 => new Vector2(1, 0),
                2 => new Vector2(0, 1),
                3 => new Vector2(-1, 0),
                _ => Vector2.Zero
            };

            facingDir.Normalize();

            float halfAngle = Config.ConeAngleDegrees / 2f;
            float coneCos = MathF.Cos(MathHelper.ToRadians(halfAngle));

            List<Vector2> tiles = new();

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    tiles.Add(new Vector2(
                        (int)(farmerTile.X + dx),
                        (int)(farmerTile.Y + dy)
                    ));
                }
            }

            return tiles
                .Where(t => IsRealTile(farmer.currentLocation, t))
                .Where(t =>
                {
                    Vector2 offset = t - farmerTile;
                    Vector2 dir = Vector2.Normalize(offset);
                    float dot = Vector2.Dot(dir, facingDir);

                    if (excludeBehind && dot < 0)
                        return false;

                    return dot >= coneCos;
                })
                .OrderBy(t =>
                {
                    Vector2 offset = t - farmerTile;

                    float forward = Vector2.Dot(offset, facingDir);

                    float distance = offset.Length();

                    float anglePenalty = Math.Abs(
                        Vector2.Dot(
                            Vector2.Normalize(offset),
                            new Vector2(-facingDir.Y, facingDir.X)
                        )
                    );

                    float baseKey = (distance * 1000f) + anglePenalty;

                    float sortKey = Config.PlaceNearToFar
                        ? baseKey
                        : -baseKey;

                    return sortKey;

                })
                .ToList();
        }
        public static bool IsRealTile(GameLocation location, Vector2 tile)
        {
            int x = (int)tile.X;
            int y = (int)tile.Y;

            if (x < 0 || y < 0 || x >= location.Map.Layers[0].LayerWidth || y >= location.Map.Layers[0].LayerHeight)
                return false;

            var backLayer = location.Map.GetLayer("Back");
            if (backLayer == null)
                return false;

            return backLayer.Tiles[x, y] != null;
        }
        public static Item GetSeedFromPot(StardewValley.Object obj)
        {
            if (obj is not IndoorPot pot)
                return null;

            var dirt = pot.hoeDirt.Value;
            var crop = dirt?.crop;

            if (crop == null)
                return null;

            string seedId = crop.netSeedIndex.Value;

            if (string.IsNullOrEmpty(seedId))
                return null;

            return ItemRegistry.Create(seedId);
        }
        public static string GetObjectName(StardewValley.Object obj)
        {
            if (obj == null)
                return "Unknown";

            if (obj is IndoorPot)
                return "Garden Pot";

            if (obj.bigCraftable.Value)
                return obj.DisplayName;

            return obj.DisplayName ?? obj.Name ?? "Object";
        }
        public static bool IsWithinScanCone(Farmer farmer, Vector2 targetTile)
        {
            int radius = Config.ConeRange;
            float halfAngle = Config.ConeAngleDegrees / 2f;
            float coneCos = MathF.Cos(MathHelper.ToRadians(halfAngle));

            Vector2 farmerTile = farmer.Tile;

            if (Vector2.Distance(farmerTile, targetTile) > radius)
                return false;

            Vector2 facingDir = farmer.FacingDirection switch
            {
                0 => new Vector2(0, -1),
                1 => new Vector2(1, 0),
                2 => new Vector2(0, 1),
                3 => new Vector2(-1, 0),
                _ => Vector2.Zero
            };

            facingDir.Normalize();

            Vector2 offset = targetTile - farmerTile;

            if (offset == Vector2.Zero)
                return false;

            Vector2 dir = Vector2.Normalize(offset);

            float dot = Vector2.Dot(dir, facingDir);

            return dot >= coneCos;
        }
        public static List<Vector2> SortExistingTilesSquare(
                Farmer farmer,
                List<Vector2> tiles
            )
        {
            Vector2 farmerTile = farmer.Tile;

            Vector2 forward = farmer.FacingDirection switch
            {
                0 => new Vector2(0, -1),
                1 => new Vector2(1, 0),
                2 => new Vector2(0, 1),
                3 => new Vector2(-1, 0),
                _ => Vector2.Zero
            };

            Vector2 right = new Vector2(forward.Y, -forward.X);
            Vector2 start = farmerTile + forward;

            int length = Config.RectangleLength;
            int width = Config.RectangleWidth;
            int halfWidth = width / 2;

            var filtered = tiles
                .Where(t =>
                {
                    Vector2 offset = t - start;

                    float forwardDist = Vector2.Dot(offset, forward);
                    float rightDist = Vector2.Dot(offset, right);

                    if (forwardDist < 0 || forwardDist >= length)
                        return false;

                    if (rightDist < -halfWidth || rightDist > halfWidth)
                        return false;

                    return true;
                });

            return Config.PlaceNearToFar
                ? filtered.OrderBy(t => Vector2.Distance(t, farmerTile)).ToList()
                : filtered.OrderByDescending(t => Vector2.Distance(t, farmerTile)).ToList();
        }
        public static List<Vector2> SortTileSquare()
        {
            var results = new List<Vector2>();
            GameLocation loc = Game1.player.currentLocation;

            Vector2 farmerTile = Game1.player.Tile;

            Vector2 forward = Game1.player.FacingDirection switch
            {
                0 => new Vector2(0, -1),
                1 => new Vector2(1, 0),
                2 => new Vector2(0, 1),
                3 => new Vector2(-1, 0),
                _ => Vector2.Zero
            };

            Vector2 right = new Vector2(forward.Y, -forward.X);
            Vector2 start = farmerTile + forward;

            int halfWidth = Config.RectangleWidth / 2;

            for (int f = 0; f < Config.RectangleLength; f++)
            {
                for (int w = -halfWidth; w <= halfWidth; w++)
                {
                    Vector2 tile =
                        start +
                        (forward * f) +
                        (right * w);

                    if (IsRealTile(loc, tile))
                        results.Add(tile);
                }
            }

            return Config.PlaceNearToFar
                ? results.OrderBy(t => Vector2.Distance(t, farmerTile)).ToList()
                : results.OrderByDescending(t => Vector2.Distance(t, farmerTile)).ToList();
        }
        public static bool IsWithinScanSquare(Farmer farmer, Vector2 targetTile)
        {
            Vector2 farmerTile = farmer.Tile;

            Vector2 forward = farmer.FacingDirection switch
            {
                0 => new Vector2(0, -1),
                1 => new Vector2(1, 0),
                2 => new Vector2(0, 1),
                3 => new Vector2(-1, 0),
                _ => Vector2.Zero
            };

            Vector2 right = new Vector2(forward.Y, -forward.X);

            Vector2 start = farmerTile + forward;

            int length = Config.RectangleLength;
            int width = Config.RectangleWidth;
            int halfWidth = width / 2;

            Vector2 offset = targetTile - start;

            float forwardDist = Vector2.Dot(offset, forward);
            float rightDist = Vector2.Dot(offset, right);

            if (forwardDist < 0 || forwardDist >= length)
                return false;

            if (rightDist < -halfWidth || rightDist > halfWidth)
                return false;

            return true;
        }
    }
}