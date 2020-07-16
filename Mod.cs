using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Source.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

// ReSharper disable ClassNeverInstantiated.Global   
// ReSharper disable UnusedMember.Global    
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedType.Local  
// ReSharper disable InconsistentNaming

namespace FRACAS
{
    public class Mod : MBSubModuleBase
    {
        public enum LogLevel
        {
            Disabled,
            Warning,
            Error,
            Debug
        }

        private const LogLevel logging = LogLevel.Debug;
        private readonly Harmony harmony = new Harmony("ca.gnivler.bannerlord.FRACAS");
        private static readonly Random Rng = new Random();
        private static List<EquipmentElement> equipmentItems;
        private static List<ItemObject> arrows;
        private static List<ItemObject> bolts;
        private static List<ItemObject> mounts;
        private static List<ItemObject> saddles;

        internal static void Log(object input, LogLevel logLevel)
        {
            if (logging >= logLevel)
            {
                FileLog.Log($"[FRACAS] {input ?? "null"}");
            }
        }

        protected override void OnSubModuleLoad()
        {
            //Harmony.DEBUG = true;
            Log("Startup " + DateTime.Now.ToShortTimeString(), LogLevel.Warning);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //Harmony.DEBUG = false;
        }

        [HarmonyPatch(typeof(MapScreen), "OnInitialize")]
        public static class CampaignOnInitializePatch
        {
            private static void Postfix()
            {
                var all = ItemObject.All.Where(x => !x.Name.Contains("Crafted") &&
                                                    !x.Name.Contains("Wooden") &&
                                                    x.Name.ToString() != "Torch" &&
                                                    x.Name.ToString() != "Horse Whip" &&
                                                    x.Name.ToString() != "Bound Crossbow").ToList();
                var oneHanded = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon);
                var twoHanded = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon);
                var polearm = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Polearm);
                var thrown = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Thrown &&
                                            x.Name.ToString() != "Boulder" && x.Name.ToString() != "Fire Pot");
                var shields = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Shield);
                var bows = all.Where(x =>
                    x.ItemType == ItemObject.ItemTypeEnum.Bow ||
                    x.ItemType == ItemObject.ItemTypeEnum.Crossbow);
                arrows = all.Where(x =>
                        x.ItemType == ItemObject.ItemTypeEnum.Arrows)
                    .Where(x => !x.Name.Contains("Ballista")).ToList();
                bolts = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Bolts).ToList();
                mounts = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Horse).ToList();
                saddles = all.Where(x =>
                    x.ItemType == ItemObject.ItemTypeEnum.HorseHarness && !x.StringId.ToLower().Contains("mule")).ToList();
                var any = new List<ItemObject>(oneHanded.Concat(twoHanded).Concat(polearm).Concat(thrown).Concat(shields).Concat(bows).ToList());
                equipmentItems = new List<EquipmentElement>();
                any.Do(x => equipmentItems.Add(new EquipmentElement(x)));
            }
        }

        [HarmonyPatch(typeof(TournamentFightMissionController), "PrepareForMatch")]
        public class TournamentFightMissionControllerPrepareForMatchPatch
        {
            // assembly copy rewrite so teams are not identical
            private static bool Prefix(TournamentFightMissionController __instance,
                TournamentMatch ____match, CultureObject ____culture)
            {
                if (GameNetwork.IsClientOrReplay)
                {
                    return false;
                }

                Log(new string('=', 50), LogLevel.Debug);
                foreach (TournamentTeam team in ____match.Teams)
                {
                    foreach (TournamentParticipant participant in team.Participants)
                    {
                        participant.MatchEquipment = BuildViableEquipmentSet();
                        for (var i = 0; i < 4; i++)
                        {
                            Log("  " + participant.MatchEquipment[i], LogLevel.Debug);
                        }

                        AccessTools.Method(typeof(TournamentFightMissionController), "AddRandomClothes")
                            .Invoke(__instance, new object[] {____culture, participant});
                    }
                }

                return false;
            }
        }

        private static Equipment BuildViableEquipmentSet()
        {
            var gear = new Equipment();
            var haveShield = false;
            var haveBow = false;
            for (var i = 0; i < 4; i++)
            {
                if (!gear[0].IsEmpty && !gear[1].IsEmpty && !gear[2].IsEmpty && !gear[3].IsEmpty)
                {
                    break;
                }

                var randomElement = equipmentItems.GetRandomElement();

                if (!gear[3].IsEmpty && i == 3 &&
                    (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Bow ||
                     randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow))
                {
                    randomElement = equipmentItems.Where(x =>
                        x.Item.ItemType != ItemObject.ItemTypeEnum.Bow &&
                        x.Item.ItemType != ItemObject.ItemTypeEnum.Crossbow).GetRandomElement();
                }

                if (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Bow ||
                    randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                {
                    if (i < 3)
                    {
                        if (haveBow)
                        {
                            i--;
                            continue;
                        }

                        haveBow = true;
                        gear[i] = randomElement;
                        if (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Bow)
                        {
                            gear[3] = new EquipmentElement(arrows.ToList()[Rng.Next(0, arrows.Count)]);
                        }

                        if (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                        {
                            gear[3] = new EquipmentElement(bolts.ToList()[Rng.Next(0, bolts.Count)]);
                        }

                        continue;
                    }

                    randomElement = equipmentItems.Where(x =>
                        x.Item.ItemType != ItemObject.ItemTypeEnum.Bow &&
                        x.Item.ItemType != ItemObject.ItemTypeEnum.Crossbow).GetRandomElement();
                }

                if (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Shield)
                {
                    if (haveShield)
                    {
                        i--;
                        continue;
                    }

                    haveShield = true;
                }

                gear[i] = randomElement;
            }

            // 20% chance to get a mount
            if (Rng.NextDouble() < 0.2f)
            {
                var mount = mounts.GetRandomElement();
                var mountId = mount.StringId.ToLower();
                gear[10] = new EquipmentElement(mount);
                Log(mountId, LogLevel.Debug);
                if (mountId.Contains("horse"))
                {
                    gear[11] = new EquipmentElement(saddles.Where(x =>
                        !x.Name.ToLower().Contains("camel")).GetRandomElement());
                    Log(gear[11].ToString(), LogLevel.Debug);
                }
                else if (mount.StringId.ToLower().Contains("camel"))
                {
                    gear[11] = new EquipmentElement(saddles.Where(x =>
                        x.Name.ToLower().Contains("camel")).GetRandomElement());
                    Log(gear[11].ToString(), LogLevel.Debug);
                }
            }

            return gear.Clone();
        }
    }
}
