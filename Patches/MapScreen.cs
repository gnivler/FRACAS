using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using static FRACAS.Helpers;
// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global

namespace FRACAS.Patches
{
    public class MapScreen
    {
        // populate globals with all the info
        public static class MapScreenOnInitializePatch
        {
            internal static void Postfix()
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
                Arrows = all.Where(x =>
                        x.ItemType == ItemObject.ItemTypeEnum.Arrows)
                    .Where(x => !x.Name.Contains("Ballista")).ToList();
                Bolts = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Bolts).ToList();
                Mounts = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Horse).ToList();
                Saddles = all.Where(x =>
                    x.ItemType == ItemObject.ItemTypeEnum.HorseHarness && !x.StringId.ToLower().Contains("mule")).ToList();
                var any = new List<ItemObject>(oneHanded.Concat(twoHanded).Concat(polearm).Concat(thrown).Concat(shields).Concat(bows).ToList());
                EquipmentItems = new List<EquipmentElement>();
                any.Do(x => EquipmentItems.Add(new EquipmentElement(x)));
            }
        }
    }
}
