using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.MountAndBlade;
using static FRACAS.Mod;
using static FRACAS.Helpers;
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local

// ReSharper disable InconsistentNaming

namespace FRACAS.Patches
{
    public class TournamentFightMissionController
    {
        [HarmonyPatch(typeof(SandBox.TournamentFightMissionController), "PrepareForMatch")]
        public class TournamentFightMissionControllerPrepareForMatchPatch
        {
            // assembly copy rewrite so teams are not identical
            private static bool Prefix(SandBox.TournamentFightMissionController __instance,
                TournamentMatch ____match, CultureObject ____culture)
            {
                if (GameNetwork.IsClientOrReplay)
                {
                    return false;
                }

                Log(new string('=', 50), LogLevel.Debug);
                foreach (var team in ____match.Teams)
                {
                    foreach (var participant in team.Participants)
                    {
                        participant.MatchEquipment = BuildViableEquipmentSet();
                        for (var i = 0; i < 4; i++)
                        {
                            Log("  " + participant.MatchEquipment[i], LogLevel.Debug);
                        }

                        AccessTools.Method(typeof(SandBox.TournamentFightMissionController), "AddRandomClothes")
                            .Invoke(__instance, new object[] {____culture, participant});
                    }
                }

                return false;
            }
        }
    }
}
