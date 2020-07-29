using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static FRACAS.Helpers;

// ReSharper disable UnusedMember.Local  
// ReSharper disable UnusedType.Global

namespace FRACAS.Patches
{
    public static class AgentPatches
    {
        public class AgentEquipItemsFromSpawnEquipmentPatch
        {
            internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilg)
            {
                if (!Mod.ModSettings.ArmyMode)
                {
                    return instructions;
                }

                var codes = instructions.ToList();
                var target = codes.FindIndex(c =>
                    c.opcode == OpCodes.Call &&
                    (MethodInfo) c.operand == AccessTools.Method(typeof(MissionWeapon), "GetWeaponData"));
                target -= 7;
                for (int i = target; i < target + 34; i++)
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
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetMissionWeapon))),
                    // dupe it for storing and the following Call
                    new CodeInstruction(OpCodes.Dup),
                    new CodeInstruction(OpCodes.Stloc_S, 10),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetWeaponData), new[] {typeof(MissionWeapon)})),
                    // store where the method is expecting it, repeat
                    new CodeInstruction(OpCodes.Stloc_2),
                    new CodeInstruction(OpCodes.Ldloc_S, 10),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetWeaponStatsData))),
                    new CodeInstruction(OpCodes.Stloc_3),
                    new CodeInstruction(OpCodes.Ldloc_S, 10),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetAmmoWeaponData))),
                    new CodeInstruction(OpCodes.Stloc_S, 4),
                    new CodeInstruction(OpCodes.Ldloc_S, 10),
                    new CodeInstruction(OpCodes.Call, AccessTools.Method(
                        typeof(AgentEquipItemsFromSpawnEquipmentPatch), nameof(GetAmmoWeaponStatsData))),
                    new CodeInstruction(OpCodes.Stloc_S, 5),
                };

                codes.InsertRange(target, stack);
                codes.Do(x => Mod.Log($"{x.opcode,-15}{x.operand}"));
                return codes.AsEnumerable();
            }

            private static MissionWeapon GetMissionWeapon(int index, Agent agent)
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
                var formationClass = agent.Character?.GetFormationClass(agent.Origin.BattleCombatant);
                var item = SelectValidItem(index, agent, hasBow, hasShield,
                    formationClass == FormationClass.Ranged ||
                    formationClass == FormationClass.Skirmisher,
                    formationClass == FormationClass.HorseArcher);
                var missionWeapon = new MissionWeapon(item, Hero.MainHero.ClanBanner);
                Traverse.Create(agent.Equipment).Field<MissionWeapon[]>("_weaponSlots").Value[index] = missionWeapon;
                return missionWeapon;
            }

            private static ItemObject SelectValidItem(int index, Agent agent, bool hasBow, bool hasShield, bool isArcher, bool isHorseArcher)
            {
                ItemObject item;
                if (isArcher || isHorseArcher && !hasBow)
                {
                    // we must get a bow for the archers
                    hasBow = true;
                    var bow = Rng.Next(0, 2) == 0;
                    MissionWeapon missionWeapon;

                    if (bow || isHorseArcher)
                    {
                        item = EquipmentItems.Select(x => x.Item).Where(x =>
                            x.ItemType == ItemObject.ItemTypeEnum.Bow).GetRandomElement();
                        missionWeapon = new MissionWeapon(item, Hero.MainHero.ClanBanner);
                    }
                    else
                    {
                        item = EquipmentItems.Select(x => x.Item).Where(x =>
                            x.ItemType == ItemObject.ItemTypeEnum.Crossbow).GetRandomElement();
                        missionWeapon = new MissionWeapon(item, Hero.MainHero.ClanBanner);
                    }

                    AddAmmo(agent, missionWeapon);
                }
                else
                {
                    // not an archer so get something else
                    item = EquipmentItems.Where(x =>
                            x.Item.ItemType != ItemObject.ItemTypeEnum.Bow &&
                            x.Item.ItemType != ItemObject.ItemTypeEnum.Crossbow)
                        .GetRandomElement().Item;
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

                return item;
            }

            private static void AddAmmo(Agent agent, MissionWeapon missionWeapon)
            {
                if (missionWeapon.PrimaryItem.ItemType == ItemObject.ItemTypeEnum.Bow)
                {
                    Mod.Log("Adding arrows");
                    var ammo = new MissionWeapon(Arrows.GetRandomElement(), Hero.MainHero.ClanBanner);
                    Traverse.Create(agent.Equipment).Field<MissionWeapon[]>("_weaponSlots").Value[3] = ammo;
                }
                else
                {
                    Mod.Log("Adding bolts");
                    var ammo = new MissionWeapon(Bolts.GetRandomElement(), Hero.MainHero.ClanBanner);
                    Traverse.Create(agent.Equipment).Field<MissionWeapon[]>("_weaponSlots").Value[3] = ammo;
                }
            }

            private static void CheckForBowsOrShields(Agent agent, ref bool hasBow, ref bool hasShield)
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
                return missionWeapon.GetWeaponData(false);
            }

            private static WeaponData GetAmmoWeaponData(MissionWeapon missionWeapon)
            {
                return missionWeapon.GetAmmoWeaponData(false);
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
