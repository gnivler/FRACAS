using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using SandBox.View.Map;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static FRACAS.Helpers;

// ReSharper disable UnusedMember.Global 
// ReSharper disable ClassNeverInstantiated.Global 
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Global

namespace FRACAS.Patches
{
    [HarmonyPatch]
    public class ItemObjectInit
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(MBGameManager), "OnCampaignStart");
            yield return AccessTools.Method(typeof(MapScreen), "OnInitialize");
        }
        
        private static void Postfix()
        {
            try
            {
                // populate globals with all the info
                var verboten = new[]
                {
                    "Sparring Targe",
                    "Trash Item",
                    "Torch",
                    "Horse Whip",
                    "Push Fork",
                    "Bound Crossbow"
                };
                var all = ItemObject.All.Where(x =>
                    x.ItemType != ItemObject.ItemTypeEnum.Goods
                    && x.ItemType != ItemObject.ItemTypeEnum.Horse
                    && x.ItemType != ItemObject.ItemTypeEnum.HorseHarness
                    && x.ItemType != ItemObject.ItemTypeEnum.Animal
                    && x.ItemType != ItemObject.ItemTypeEnum.Banner
                    && x.ItemType != ItemObject.ItemTypeEnum.Book
                    && x.ItemType != ItemObject.ItemTypeEnum.Invalid
                    && !x.Name.Contains("Crafted")
                    && !x.Name.Contains("Wooden")
                    && !x.Name.Contains("Practice")
                    && !verboten.Contains(x.Name.ToString())).ToList();
                Arrows = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Arrows)
                    .Where(x => !x.Name.Contains("Ballista")).ToList();
                Bolts = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Bolts).ToList();
                var oneHanded = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon);
                var twoHanded = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon);
                var polearm = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Polearm);
                var thrown = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Thrown &&
                                            x.Name.ToString() != "Boulder" && x.Name.ToString() != "Fire Pot");
                var shields = all.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Shield);
                var bows = all.Where(x =>
                    x.ItemType == ItemObject.ItemTypeEnum.Bow ||
                    x.ItemType == ItemObject.ItemTypeEnum.Crossbow);
                var any = new List<ItemObject>(oneHanded.Concat(twoHanded).Concat(polearm).Concat(thrown).Concat(shields).Concat(bows).ToList());
                any.Do(x => EquipmentItems.Add(new EquipmentElement(x).Item));
                Mounts = ItemObject.All.Where(x => x.ItemType == ItemObject.ItemTypeEnum.Horse).Where(x => !x.StringId.Contains("unmountable")).ToList();
                Saddles = ItemObject.All.Where(x => x.ItemType == ItemObject.ItemTypeEnum.HorseHarness && !x.StringId.ToLower().Contains("mule")).ToList();
                EquipmentItems = any;
            }
            catch (Exception ex)
            {
                Mod.Log(ex);
            }
        }
    }
}
