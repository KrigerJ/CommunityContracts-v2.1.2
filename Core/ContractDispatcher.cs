using StardewModdingAPI;
using StardewValley;

using static CommunityContracts.Core.NPC.AbigailProfile;
using static CommunityContracts.Core.NPC.AlexProfile;
using static CommunityContracts.Core.NPC.CarolineProfile;
using static CommunityContracts.Core.NPC.DemetriusProfile;
using static CommunityContracts.Core.NPC.ElliottProfile;
using static CommunityContracts.Core.NPC.EmilyProfile;
using static CommunityContracts.Core.NPC.EvelynProfile;
using static CommunityContracts.Core.NPC.GeorgeProfile;
using static CommunityContracts.Core.NPC.HaleyProfile;
using static CommunityContracts.Core.NPC.JasProfile;
using static CommunityContracts.Core.NPC.JodiProfile;
using static CommunityContracts.Core.NPC.LeahProfile;
using static CommunityContracts.Core.NPC.LeoProfile;
using static CommunityContracts.Core.NPC.LinusProfile;
using static CommunityContracts.Core.NPC.MaruProfile;
using static CommunityContracts.Core.NPC.NPCProfile;
using static CommunityContracts.Core.NPC.PamProfile;
using static CommunityContracts.Core.NPC.PennyProfile;
using static CommunityContracts.Core.NPC.SamProfile;
using static CommunityContracts.Core.NPC.SandyProfile;
using static CommunityContracts.Core.NPC.SebastianProfile;
using static CommunityContracts.Core.NPC.ShaneProfile;
using static CommunityContracts.Core.NPC.VincentProfile;
using static CommunityContracts.Core.NPC.WizardProfile;

namespace CommunityContracts.Core
{
    public static class ContractDispatcher
    {
        private static readonly Dictionary<string, Action> npcIntros = new()
        {
            { "Abigail", () => AbigailContract.AbigailIntroduction() },
            { "Alex", () => AlexContract.AlexIntroduction() },
            { "Caroline", () => CarolineContract.CarolineIntroduction() },
            { "Demetrius", () => DemetriusContract.DemetriusIntroduction() },
            { "Elliott", () => ElliottContract.ElliottIntroduction() },
            { "Emily", () => EmilyContract.EmilyIntroduction() },
            { "Evelyn", () => EvelynContract.EvelynIntroduction() },
            { "George", () => GeorgeContract.GeorgeIntroduction() },
            { "Haley", () => HaleyContract.HaleyIntroduction() },
            { "Jas", () => JasContract.JasIntroduction() },
            { "Jodi", () => JodiContract.JodiIntroduction() },
            { "Leah", () => LeahContract.LeahIntroduction() },
            { "Leo", () => LeoContract.LeoIntroduction() },
            { "Linus", () => LinusContract.LinusIntroduction() },
            { "Maru", () => MaruContract.MaruIntroduction() },
            { "Pam", () => PamContract.PamIntroduction() },
            { "Penny", () => PennyContract.PennyIntroduction() },
            { "Sam", () => SamContract.SamIntroduction() },
            { "Sandy", () => SandyContract.SandyIntroduction() },
            { "Sebastian", () => SebastianContract.SebastianIntroduction() },
            { "Shane", () => ShaneContract.ShaneIntroduction() },
            { "Vincent", () => VincentContract.VincentIntroduction() },
            { "Wizard", () => WizardContract.WizardIntroduction() },
        };
        public static void TryRunIntro(string npcName)
        {
            if (!Game1.player.friendshipData.ContainsKey(npcName))
            {
                NPCContract.NFNPCIntroduction(npcName);
                ModEntry.Instance.Monitor.Log(ModEntry.T("SkippingContractIntro", new { npc = npcName }), LogLevel.Trace);

                return;
            }
            if (npcIntros.TryGetValue(npcName, out var intro))
            {
                intro();
            }
            else
            {
                    NPCContract.NPCIntroduction(npcName);
            }
        }
    }
}
