using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using static FRACAS.Mod;

// ReSharper disable InconsistentNaming

namespace FRACAS
{
    public static class Helpers
    {
        internal static List<ItemObject> EquipmentItems = new();
        internal static List<ItemObject> Arrows = new();
        internal static List<ItemObject> Bolts = new();
        internal static List<ItemObject> Mounts = new();
        internal static List<ItemObject> Saddles = new();
        internal static readonly Random Rng = new ();

        // builds a set of 4 weapons that won't include more than 1 bow or shield, nor any lack of ammo
        internal static Equipment BuildViableEquipmentSet()
        {
            var gear = new Equipment();
            var haveShield = false;
            var haveBow = false;
            for (var i = 0; i < 4; i++)
            {
                if (i == 3 && !gear[3].IsEmpty)
                {
                    break;
                }

                var randomElement = EquipmentItems.GetRandomElement();

                if (!gear[3].IsEmpty && i == 3 &&
                    (randomElement.ItemType == ItemObject.ItemTypeEnum.Bow ||
                     randomElement.ItemType == ItemObject.ItemTypeEnum.Crossbow))
                {
                    randomElement = EquipmentItems.Where(x =>
                        x.ItemType != ItemObject.ItemTypeEnum.Bow &&
                        x.ItemType != ItemObject.ItemTypeEnum.Crossbow).ToList().GetRandomElement();
                }

                if (randomElement.ItemType == ItemObject.ItemTypeEnum.Bow ||
                    randomElement.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                {
                    if (i < 3)
                    {
                        if (haveBow)
                        {
                            i--;
                            continue;
                        }

                        haveBow = true;
                        gear[i] = new EquipmentElement(randomElement);
                        if (randomElement.ItemType == ItemObject.ItemTypeEnum.Bow)
                        {
                            gear[3] = new EquipmentElement(Arrows.ToList()[Rng.Next(0, Arrows.Count)]);
                        }

                        if (randomElement.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                        {
                            gear[3] = new EquipmentElement(Bolts.ToList()[Rng.Next(0, Bolts.Count)]);
                        }

                        continue;
                    }

                    randomElement = EquipmentItems.Where(x =>
                        x.ItemType != ItemObject.ItemTypeEnum.Bow &&
                        x.ItemType != ItemObject.ItemTypeEnum.Crossbow).ToList().GetRandomElement();
                }

                if (randomElement.ItemType == ItemObject.ItemTypeEnum.Shield)
                {
                    if (haveShield)
                    {
                        i--;
                        continue;
                    }

                    haveShield = true;
                }

                gear[i] = new EquipmentElement(randomElement);
            }

            // 20% chance to get a mount
            if (Rng.NextDouble() < 0.2f)
            {
                var mount = Mounts.GetRandomElement();
                var mountId = mount.StringId.ToLower();
                gear[10] = new EquipmentElement(mount);
                if (mountId.Contains("camel"))
                {
                    gear[11] = new EquipmentElement(Saddles.Where(x =>
                        x.Name.ToLower().Contains("camel")).ToList().GetRandomElement());
                }
                else
                {
                    gear[11] = new EquipmentElement(Saddles.Where(x =>
                        !x.Name.ToLower().Contains("camel")).ToList().GetRandomElement());
                }
            }

            return gear.Clone();
        }

        internal static float SumTeamEquipmentValue(TournamentTeam team)
        {
            float result = default;
            try
            {
                foreach (var participant in team.Participants)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        result += participant.MatchEquipment[i].Item.Tierf;
                    }

                    if (participant.MatchEquipment[10].IsEmpty)
                    {
                        return result;
                    }

                    result += participant.MatchEquipment[10].Item.Tierf;

                    if (participant.MatchEquipment[11].IsEmpty)
                    {
                        return result;
                    }

                    result += participant.MatchEquipment[11].Item.Tierf;
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }

            return result;
        }

        internal static void EquipParticipant(
            SandBox.TournamentFightMissionController __instance,
            CultureObject ____culture,
            TournamentTeam team,
            Dictionary<TournamentTeam, int> mountMap,
            TournamentParticipant participant)
        {
            var equipment = BuildViableEquipmentSet();
            Log(new string('-', 50));
            LogWeaponsAndMount(equipment);
            if (!equipment[10].IsEmpty)
            {
                mountMap[team]++;
            }

            participant.MatchEquipment = equipment;
            AccessTools.Method(typeof(SandBox.TournamentFightMissionController), "AddRandomClothes")
                .Invoke(__instance, new object[] {____culture, participant});
        }

        private static void LogWeaponsAndMount(Equipment equipment)
        {
            Log(equipment[0]);
            Log(equipment[1]);
            Log(equipment[2]);
            Log(equipment[3]);
            if (equipment[10].IsEmpty)
            {
                return;
            }

            Log(equipment[10]);
            if (equipment[11].IsEmpty)
            {
                return;
            }

            Log(equipment[11]);
        }
    }
}
