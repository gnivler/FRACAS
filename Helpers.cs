using System;
using System.Collections.Generic;
using System.Linq;
using FRACAS.Patches;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Menu.Overlay;
using static FRACAS.SubModule;

// ReSharper disable InconsistentNaming

namespace FRACAS
{
    public static class Helpers
    {
        internal static readonly List<EquipmentElement> EquipmentItems = new();
        internal static List<ItemObject> Arrows = new();
        internal static List<ItemObject> Bolts = new();
        internal static List<ItemObject> Mounts = new();
        internal static List<ItemObject> Saddles = new();
        internal static readonly Dictionary<ItemObject.ItemTypeEnum, List<ItemObject>> ArmourTypes = new();
        internal static readonly Random Rng = new();
        internal static List<ItemObject> All = new();

        internal static void BuildItems()
        {
            var verbotenItems = new[]
            {
                "bound_adarga",
                "old_kite_sparring_shield_shoulder",
                "old_horsemans_kite_shield_shoulder",
                "western_riders_kite_sparring_shield_shoulder",
                "old_horsemans_kite_shield",
                "banner_mid",
                "banner_big",
                "campaign_banner_small",
                "battania_targe_b_sparring",
                "eastern_spear_1_t2_blunt",
                "khuzait_polearm_1_t4_blunt",
                "eastern_javelin_1_t2_blunt",
                "aserai_axe_2_t2_blunt",
                "battania_2haxe_1_t2_blunt",
                "western_javelin_1_t2_blunt",
                "empire_lance_1_t3_blunt",
                "billhook_polearm_t2_blunt",
                "vlandia_lance_1_t3_blunt",
                "sturgia_axe_2_t2_blunt",
                "northern_throwing_axe_1_t1_blunt",
                "northern_spear_1_t2_blunt",
                "torch",
                "wooden_sword_t1",
                "wooden_sword_t2",
                "wooden_2hsword_t1",
                "practice_spear_t1",
                "horse_whip",
                "push_fork",
                "mod_banner_1",
                "mod_banner_2",
                "mod_banner_3",
                "throwing_stone",
                "ballista_projectile",
                "ballista_projectile_burning",
                "boulder",
                "pot",
                "grapeshot_stack",
                "grapeshot_fire_stack",
                "grapeshot_projectile",
                "grapeshot_fire_projectile",
                "bound_desert_round_sparring_shield",
                "northern_round_sparring_shield",
                "western_riders_kite_sparring_shield",
                "western_kite_sparring_shield",
                "oval_shield",
                "old_kite_sparring_shield ",
                "western_kite_sparring_shield_shoulder"
            };

            All = Items.All.Where(i =>
                !i.IsCivilian
                && !i.IsCraftedByPlayer
                && i.ItemType is not (ItemObject.ItemTypeEnum.Goods
                    or ItemObject.ItemTypeEnum.Horse
                    or ItemObject.ItemTypeEnum.HorseHarness
                    or ItemObject.ItemTypeEnum.Animal
                    or ItemObject.ItemTypeEnum.Banner
                    or ItemObject.ItemTypeEnum.Book
                    or ItemObject.ItemTypeEnum.Invalid)
                && i.ItemCategory.StringId != "garment").ToList();
            All.RemoveAll(i => verbotenItems.Contains(i.StringId));
            Arrows = All.Where(i => i.ItemType == ItemObject.ItemTypeEnum.Arrows).ToList();
            Bolts = All.Where(i => i.ItemType == ItemObject.ItemTypeEnum.Bolts).ToList();
            var oneHanded = All.Where(i => i.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon);
            var twoHanded = All.Where(i => i.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon);
            var polearm = All.Where(i => i.ItemType == ItemObject.ItemTypeEnum.Polearm);
            var thrown = All.Where(i => i.ItemType == ItemObject.ItemTypeEnum.Thrown);
            var shields = All.Where(i => i.ItemType == ItemObject.ItemTypeEnum.Shield);
            var bows = All.Where(i => i.ItemType is ItemObject.ItemTypeEnum.Bow or ItemObject.ItemTypeEnum.Crossbow);
            Mounts = Items.All.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Horse).Where(x => !x.StringId.Contains("unmountable")).ToList();
            Saddles = Items.All.Where(x => x.ItemType == ItemObject.ItemTypeEnum.HorseHarness && !x.StringId.ToLower().Contains("mule")).ToList();
            var any = new List<ItemObject>(oneHanded.Concat(twoHanded).Concat(polearm).Concat(thrown).Concat(shields).Concat(bows).ToList());
            any.Do(i => EquipmentItems.Add(new EquipmentElement(i)));
            foreach (ItemObject.ItemTypeEnum value in Enum.GetValues(typeof(ItemObject.ItemTypeEnum)))
            {
                if (value.ToString().Contains("Armor") || value.ToString() == "Cape")
                    ArmourTypes[value] = All.WhereQ(item => item.Type == value).ToListQ();
            }
        }

        // builds a set of 4 weapons that won't include more than 1 bow or shield, nor any lack of ammo
        private static Equipment BuildViableEquipmentSet()
        {
            var gear = new Equipment();
            var haveShield = false;
            var haveBow = false;
            try
            {
                for (var slot = 0; slot < 4; slot++)
                {
                    EquipmentElement randomElement = default;
                    switch (slot)
                    {
                        case 0:
                        case 1:
                            randomElement = EquipmentItems.GetRandomElement();
                            break;
                        case 2 when !gear[3].IsEmpty:
                            randomElement = EquipmentItems.Where(x =>
                                x.Item.ItemType is not ItemObject.ItemTypeEnum.Bow
                                    or ItemObject.ItemTypeEnum.Crossbow).ToList().GetRandomElement();
                            break;
                        case 2:
                        case 3:
                            randomElement = EquipmentItems.GetRandomElement();
                            break;
                    }

                    // matches here by obtaining a bow, which then stuffed ammo into [3]
                    if (slot == 3 && !gear[3].IsEmpty)
                    {
                        break;
                    }

                    if (randomElement.Item.ItemType is ItemObject.ItemTypeEnum.Bow or ItemObject.ItemTypeEnum.Crossbow)
                    {
                        if (slot < 3)
                        {
                            // try again, try harder
                            if (haveBow)
                            {
                                slot--;
                                continue;
                            }

                            haveBow = true;
                            gear[slot] = randomElement;
                            if (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Bow)
                            {
                                gear[3] = new EquipmentElement(Arrows.ToList()[Rng.Next(0, Arrows.Count)]);
                            }
                            else if (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                            {
                                gear[3] = new EquipmentElement(Bolts.ToList()[Rng.Next(0, Bolts.Count)]);
                            }

                            continue;
                        }

                        randomElement = EquipmentItems.Where(x =>
                            x.Item.ItemType != ItemObject.ItemTypeEnum.Bow &&
                            x.Item.ItemType != ItemObject.ItemTypeEnum.Crossbow).ToList().GetRandomElement();
                    }

                    if (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Shield)
                    {
                        // try again, try harder
                        if (haveShield)
                        {
                            slot--;
                            continue;
                        }

                        haveShield = true;
                    }

                    gear[slot] = randomElement;
                }

                // 20% chance to get a mount
                if (Rng.NextDouble() < 0.2f)
                {
                    var mount = Mounts.GetRandomElement();
                    var mountId = mount.StringId.ToLower();
                    gear[10] = new EquipmentElement(mount);
                    if (mountId.Contains("camel"))
                    {
                        gear[11] = new EquipmentElement(Saddles.Where(saddle =>
                            saddle.Name.ToString().ToLower().Contains("camel")).ToList().GetRandomElement());
                    }
                    else
                    {
                        gear[11] = new EquipmentElement(Saddles.Where(saddle =>
                            !saddle.Name.ToString().ToLower().Contains("camel")).ToList().GetRandomElement());
                    }
                }

                //Mod.Log("-----");
                //for (var i = 0; i < 12; i++)
                //{
                //    Mod.Log($"Slot {i}: {gear[i].Item?.Name}");
                //}

                Log("");
            }
            catch (Exception ex)
            {
                Log(ex);
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
            TournamentTeam team,
            Dictionary<TournamentTeam, int> mountMap,
            TournamentParticipant participant)
        {
            var equipment = BuildViableEquipmentSet();
            participant.MatchEquipment = new Equipment();
            if (!equipment[10].IsEmpty)
                mountMap[team]++;
            for (var index = 0; index < 4; index++)
                participant.MatchEquipment[index] = equipment[index];
            if (SubModule.Settings.OnlyWeapons)
            {
                participant.MatchEquipment[5] = participant.Character.Equipment[5];
                participant.MatchEquipment[6] = participant.Character.Equipment[6];
                participant.MatchEquipment[7] = participant.Character.Equipment[7];
                participant.MatchEquipment[8] = participant.Character.Equipment[8];
                participant.MatchEquipment[9] = participant.Character.Equipment[9];
            }

            else
            {
                participant.MatchEquipment[5] = new EquipmentElement(ArmourTypes[ItemObject.ItemTypeEnum.HeadArmor].GetRandomElement());
                participant.MatchEquipment[6] = new EquipmentElement(ArmourTypes[ItemObject.ItemTypeEnum.BodyArmor].GetRandomElement());
                participant.MatchEquipment[7] = new EquipmentElement(ArmourTypes[ItemObject.ItemTypeEnum.LegArmor].GetRandomElement());
                participant.MatchEquipment[8] = new EquipmentElement(ArmourTypes[ItemObject.ItemTypeEnum.HandArmor].GetRandomElement());
                participant.MatchEquipment[9] = new EquipmentElement(ArmourTypes[ItemObject.ItemTypeEnum.Cape].GetRandomElement());
            }

            participant.MatchEquipment[10] = equipment[10];
            participant.MatchEquipment[11] = equipment[11];
        }
    }
}
