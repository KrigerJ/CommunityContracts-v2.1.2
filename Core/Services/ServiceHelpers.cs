using StardewValley;
using SObject = StardewValley.Object;

namespace CommunityContracts.Core.Services
{
    internal class ServiceHelpers
    {
        public static SObject CreateSmokedFishProduct(SObject inputFish, int npcLevel)
        {
            if (inputFish == null)
                return null;

            var output = ItemRegistry.Create("SmokedFish") as SObject;
            if (output == null)
                return null;

            output.displayName = $"Smoked {inputFish.DisplayName}";

            const float priceMultiplier = 1.5f;
            const float edibilityMultiplier = 1.2f;

            output.Price = (int)(inputFish.Price * priceMultiplier);
            output.Edibility = (int)(inputFish.Edibility * edibilityMultiplier);

            output.Quality = GetQualityFromNPCLevel(npcLevel);

            return output;
        }
        private static int GetQualityFromNPCLevel(int level)
        {
            if (level >= 10) return SObject.bestQuality;
            if (level >= 7) return SObject.highQuality;
            if (level >= 4) return SObject.medQuality;
            return SObject.lowQuality;
        }
    }
}
