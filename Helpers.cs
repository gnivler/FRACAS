using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using static FRACAS.Mod;

namespace FRACAS
{
    public static class Helpers
    {
        internal static List<EquipmentElement> EquipmentItems;
        internal static List<ItemObject> Arrows;
        internal static List<ItemObject> Bolts;
        internal static List<ItemObject> Mounts;
        internal static List<ItemObject> Saddles;
        internal static readonly Random Rng = new Random();

        // builds a set of 4 weapons that won't include more than 1 bow or shield, nor any lack of ammo
        internal static Equipment BuildViableEquipmentSet()
        {
            var gear = new Equipment();
            var haveShield = false;
            var haveBow = false;
            for (var i = 0; i < 4; i++)
            {
                // maybe... if (i == 3 && !gear[3].IsEmpty)?
                if (!gear[0].IsEmpty && !gear[1].IsEmpty && !gear[2].IsEmpty && !gear[3].IsEmpty)
                {
                    break;
                }

                var randomElement = EquipmentItems.GetRandomElement();

                if (!gear[3].IsEmpty && i == 3 &&
                    (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Bow ||
                     randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow))
                {
                    randomElement = EquipmentItems.Where(x =>
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
                            gear[3] = new EquipmentElement(Arrows.ToList()[Rng.Next(0, Arrows.Count)]);
                        }

                        if (randomElement.Item.ItemType == ItemObject.ItemTypeEnum.Crossbow)
                        {
                            gear[3] = new EquipmentElement(Bolts.ToList()[Rng.Next(0, Bolts.Count)]);
                        }

                        continue;
                    }

                    randomElement = EquipmentItems.Where(x =>
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
                var mount = Mounts.GetRandomElement();
                var mountId = mount.StringId.ToLower();
                gear[10] = new EquipmentElement(mount);
                Log(mountId, LogLevel.Debug);
                if (mountId.Contains("horse"))
                {
                    gear[11] = new EquipmentElement(Saddles.Where(x =>
                        !x.Name.ToLower().Contains("camel")).GetRandomElement());
                    Log(gear[11].ToString(), LogLevel.Debug);
                }
                else if (mount.StringId.ToLower().Contains("camel"))
                {
                    gear[11] = new EquipmentElement(Saddles.Where(x =>
                        x.Name.ToLower().Contains("camel")).GetRandomElement());
                    Log(gear[11].ToString(), LogLevel.Debug);
                }
            }

            return gear.Clone();
        }
    }
}
