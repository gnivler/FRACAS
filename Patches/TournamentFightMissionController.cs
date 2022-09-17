using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.MountAndBlade;
using static FRACAS.SubModule;
using static FRACAS.Helpers;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Local 
// ReSharper disable InconsistentNaming

namespace FRACAS.Patches
{
    [HarmonyPatch(typeof(TournamentFightMissionController), "PrepareForMatch")]
    public class TournamentFightMissionControllerPrepareForMatchPatch
    {
        // assembly copy rewrite so teams are not identical
        private static bool Prefix(TournamentMatch ____match)
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
                mountMap.Add(team, 0);
                foreach (var participant in team.Participants)
                {
                    EquipParticipant(team, mountMap, participant);
                }

                qualityMap.Add(team, SumTeamEquipmentValue(team));

                // use the first team's random build value as the baseline
                // act after the first team is populated, re-rolling to find a suitable delta
                if (qualityMap.Keys.Count > 1)
                {
                    while (Math.Abs(qualityMap.Values.ElementAt(0) - qualityMap[team]) > 3
                           || mountMap.Values.ElementAt(0) != mountMap[team])
                    {
                        Log("RE-ROLLING TEAM");
                        mountMap[team] = 0;
                        foreach (var participant in team.Participants)
                        {
                            EquipParticipant(team, mountMap, participant);
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
