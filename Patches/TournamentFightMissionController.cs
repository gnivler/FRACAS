using System;
using System.Collections.Generic;
using System.Linq;
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

            var qualityMap = new Dictionary<TournamentTeam, float>();
            var mountMap = new Dictionary<TournamentTeam, int>();
            Log("");
            Log(new string('=', 50));
            Log("NEW MATCH");
            foreach (var team in ____match.Teams)
            {
                if (!ModSettings.TournamentBalance)
                {
                    foreach (var participant in team.Participants)
                    {
                        EquipParticipant(__instance, ____culture, team, mountMap, participant);
                    }

                    continue;
                }

                mountMap.Add(team, 0);
                foreach (var participant in team.Participants)
                {
                    EquipParticipant(__instance, ____culture, team, mountMap, participant);
                }

                qualityMap.Add(team, SumTeamEquipmentValue(team));

                // use the first team's random build value as the baseline
                // act after the first team is populated, re-rolling to find a suitable delta
                if (qualityMap.Keys.Count > 1)
                {
                    while (Math.Abs(qualityMap.Values.ElementAt(0) - qualityMap[team]) > ModSettings.DifferenceThreshold ||
                           mountMap.Values.ElementAt(0) != mountMap[team])
                    {
                        Log("RE-ROLLING TEAM");
                        mountMap[team] = 0;
                        foreach (var participant in team.Participants)
                        {
                            EquipParticipant(__instance, ____culture, team, mountMap, participant);
                        }

                        qualityMap[team] = SumTeamEquipmentValue(team);
                    }
                }
            }

            Log(new string('-', 50));
            for (var i = 0; i < ____match.Teams.Count(); i++)
            {
                Log($"Team{i + 1} value {qualityMap.Values.ElementAt(i):F2}");
            }

            return false;
        }
    }
}
