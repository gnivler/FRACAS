using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Authentication;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterQueries;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static FRACAS.Helpers;

// ReSharper disable UnusedMember.Local  
// ReSharper disable UnusedType.Global

namespace FRACAS.Patches
{
    public static class Agent
    {
        public class AgentEquipItemsFromSpawnEquipmentPatch
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
            {
                var codes = instructions.ToList();
                var target = codes.FindIndex(c =>
                    c.opcode == OpCodes.Call &&
                    (MethodInfo) c.operand == AccessTools.Method(typeof(MissionWeapon), "GetWeaponData"));
                target -= 6;
                for (int i = target; i < target + 32; i++)
                {
                    codes[i].opcode = OpCodes.Nop;
                    codes[i].operand = null;
                }

                ilg.DeclareLocal(typeof(MissionWeapon));
                var stack = new List<CodeInstruction>
                {
                    // load the iterator, and `this` Agent
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetMissionWeapon))),
                    // dupe it for storing and the following Call
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Stloc_S, 10),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetWeaponData), new[] {typeof(MissionWeapon)})),
                    // store where the method is expecting it, repeat
                    new CodeInstruction(OpCodes.Stloc_2),
                    new CodeInstruction(OpCodes.Ldloc_S, 10),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetWeaponStatsData))),
                    new CodeInstruction(OpCodes.Stloc_3),
                    new CodeInstruction(OpCodes.Ldloc_S, 10),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetAmmoWeaponData))),
                    new CodeInstruction(OpCodes.Stloc_S, 4),
                    new CodeInstruction(OpCodes.Ldloc_S, 10),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetAmmoWeaponStatsData))),
                    new CodeInstruction(OpCodes.Stloc_S, 5),
                };

                codes.InsertRange(target, stack);
                //codes.Do(x => Mod.Log($"{x.opcode,-15}{x.operand}", Mod.LogLevel.Debug));
                return codes.AsEnumerable();
            }

            private static MissionWeapon GetMissionWeapon(int index, TaleWorlds.MountAndBlade.Agent agent)
            {
                if (Hero.All.Contains(((CharacterObject) agent.Character)?.HeroObject) ||
                    !agent.Equipment[3].IsEmpty && index == 3)
                {
                    return agent.Equipment[index];
                }

                // evaluate the Agent
                var hasShield = false;
                var hasBow = false;
                CheckForBowsOrShields(agent, ref hasBow, ref hasShield);
                var item = EquipmentItems.GetRandomElement().Item;
                MissionWeapon missionWeapon = default;
                if (item.ItemType == ItemObject.ItemTypeEnum.Bow || item.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                {
                    // need room for ammo, so don't add a bow in the last slot
                    if (!hasBow && index < 3)
                    {
                        Mod.Log("Adding bow at index " + index, Mod.LogLevel.Debug);
                        hasBow = true;
                        missionWeapon = new MissionWeapon(item, Hero.MainHero.ClanBanner);
                        if (missionWeapon.PrimaryItem.ItemType == ItemObject.ItemTypeEnum.Bow)
                        {
                            Mod.Log("Adding arrows", Mod.LogLevel.Debug);
                            var ammo = new MissionWeapon(Arrows.GetRandomElement(), Hero.MainHero.ClanBanner);
                            Traverse.Create(agent.Equipment).Field<MissionWeapon[]>("_weaponSlots").Value[3] = ammo;
                        }
                        else
                        {
                            Mod.Log("Adding bolts", Mod.LogLevel.Debug);
                            var ammo = new MissionWeapon(Bolts.GetRandomElement(), Hero.MainHero.ClanBanner);
                            Traverse.Create(agent.Equipment).Field<MissionWeapon[]>("_weaponSlots").Value[3] = ammo;
                        }
                    }
                    else
                    {
                        // we got a bow we can't use, so take something else
                        item = EquipmentItems.Select(x => x.Item).Where(x =>
                                x.ItemType != ItemObject.ItemTypeEnum.Bow &&
                                x.ItemType != ItemObject.ItemTypeEnum.Crossbow)
                            .GetRandomElement();
                    }
                }

                if (item.ItemType == ItemObject.ItemTypeEnum.Shield &&
                    hasShield)
                {
                    // we can't take a shield, make a subset for next filter
                    var selection = EquipmentItems.Select(x => x.Item).Where(x =>
                        x.ItemType != ItemObject.ItemTypeEnum.Shield);
                    // we also can't take a bow now
                    if (hasBow || index > 2)
                    {
                        selection = selection.Where(x =>
                            x.ItemType != ItemObject.ItemTypeEnum.Bow &&
                            x.ItemType != ItemObject.ItemTypeEnum.Crossbow);
                    }

                    // pick from subset
                    item = selection.GetRandomElement();
                }

                missionWeapon = new MissionWeapon(item, Banner.CreateOneColoredEmptyBanner(0));
                Traverse.Create(agent.Equipment).Field<MissionWeapon[]>("_weaponSlots").Value[index] = missionWeapon;
                return missionWeapon;
            }

            private static void CheckForBowsOrShields(TaleWorlds.MountAndBlade.Agent agent, ref bool hasBow, ref bool hasShield)
            {
                for (var i = 0; i < 4; i++)
                {
                    if (agent.Equipment[i].IsEmpty)
                    {
                        break;
                    }

                    var agentItemType = agent.Equipment[i].PrimaryItem.ItemType;
                    if (agentItemType == ItemObject.ItemTypeEnum.Bow || agentItemType == ItemObject.ItemTypeEnum.Crossbow)
                    {
                        hasBow = true;
                        continue;
                    }

                    if (agentItemType == ItemObject.ItemTypeEnum.Shield)
                    {
                        hasShield = true;
                    }
                }
            }

            private static WeaponData GetWeaponData(MissionWeapon missionWeapon)
            {
                return missionWeapon.GetWeaponData();
            }

            private static WeaponData GetAmmoWeaponData(MissionWeapon missionWeapon)
            {
                return missionWeapon.GetAmmoWeaponData();
            }

            private static WeaponStatsData[] GetWeaponStatsData(MissionWeapon missionWeapon)
            {
                return missionWeapon.GetWeaponStatsData();
            }

            private static WeaponStatsData[] GetAmmoWeaponStatsData(MissionWeapon missionWeapon)
            {
                return missionWeapon.GetWeaponStatsData();
            }
        }
    }
}
