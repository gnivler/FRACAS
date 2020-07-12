using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
            Info,
            Error,
            Debug
        }

        private const LogLevel logging = LogLevel.Disabled;
        private readonly Harmony harmony = new Harmony("ca.gnivler.bannerlord.FRACAS");
        private static readonly Random Rng = new Random();
        private static List<EquipmentElement> equipmentItems;
        private static List<ItemObject> arrows;
        private static List<ItemObject> bolts;

        protected override void OnSubModuleLoad()
        {
            //Harmony.DEBUG = true;
            Log("Startup " + DateTime.Now.ToShortTimeString(), LogLevel.Info);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            //Harmony.DEBUG = false;
        }

        internal static void Log(object input, LogLevel logLevel)
        {
            if (logging >= logLevel)
            {
                FileLog.Log($"[FRACAS] {input ?? "null"}");
            }
        }

        [HarmonyPatch(typeof(MapScreen), "OnInitialize")]
        public static class CampaignOnInitializePatch
        {
            private static void Postfix()
            {
                var all = ItemObject.All.ToList();
                var oneHanded = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon);
                var twoHanded = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon);
                var thrown = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Thrown);
                var shields = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Shield);
                var bows = all.Where(x =>
                    x.ItemType == ItemObject.ItemTypeEnum.Bow ||
                    x.ItemType == ItemObject.ItemTypeEnum.Crossbow);
                arrows = all.Where(x =>
                        x.ItemType == ItemObject.ItemTypeEnum.Arrows)
                    .Where(x => !x.Name.Contains("Ballista")).ToList();
                bolts = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Bolts).ToList();
                var any = new List<ItemObject>(oneHanded.Concat(twoHanded).Concat(thrown)
                    .Concat(shields).Concat(bows).Where(x => !x.Name.Contains("Crafted")).ToList());
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

                foreach (TournamentTeam team in ____match.Teams)
                {
                    var weaponEquipmentList = new List<Equipment>();
                    for (var i = 0; i < 16; i++)
                    {
                        weaponEquipmentList.Add(GetRandomWeapons());
                    }

                    foreach (TournamentParticipant participant in team.Participants)
                    {
                        var index = Rng.Next(0, 16);
                        participant.MatchEquipment = weaponEquipmentList[index].Clone();
                        Traverse.Create(__instance).Method("AddRandomClothes", ____culture, participant).GetValue();
                    }
                }

                return false;
            }

            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                var target = codes.FindIndex(x => x.opcode == OpCodes.Stloc_0);
                target++;

                var helper = AccessTools.Method(typeof(TournamentFightMissionControllerPrepareForMatchPatch), nameof(GetWeaponList));
                var stack = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Call, helper),
                    new CodeInstruction(OpCodes.Stloc_0)
                };

                codes.InsertRange(target, stack);

                return codes.AsEnumerable();
            }

            private static List<Equipment> GetWeaponList()
            {
                var gear = new List<Equipment>();
                while (gear.Count < 16)
                {
                    gear.Add(GetRandomWeapons());
                }

                return gear;
            }

            private static int OneToFifteen() => Rng.Next(0, 16);
        }

        private static Equipment GetRandomWeapons()
        {
            try
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

                return gear.Clone();
            }
            catch (Exception ex)
            {
                Log(ex, LogLevel.Error);
            }

            return null;
        }
    }
}
