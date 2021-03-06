﻿using System;
using System.IO;
using System.Reflection;
using FRACAS.Patches;
using HarmonyLib;
using Newtonsoft.Json;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
// ReSharper disable FieldCanBeMadeReadOnly.Global 
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
        internal static Settings ModSettings;
        private readonly Harmony harmony = new Harmony("ca.gnivler.bannerlord.FRACAS");

        internal static void Log(object input)
        {
            //FileLog.Log($"[FRACAS] {input ?? "null"}");
        }

        internal class Settings
        {
            public bool ArmyMode = false;
            public bool TournamentBalance = true;
            public int DifferenceThreshold = 3;
        }

        protected override void OnSubModuleLoad()
        {
            Log("Startup " + DateTime.Now.ToShortTimeString());
            try
            {
                ModSettings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("..\\..\\Modules\\FRACAS\\mod_settings.json"));
            }
            catch (Exception ex)
            {
                ModSettings = new Settings();
                Log(ex);
            }
            finally
            {
                ModSettings.DifferenceThreshold = Mathf.Clamp(ModSettings.DifferenceThreshold, 1, ModSettings.DifferenceThreshold);
            }

            harmony.PatchAll(Assembly.GetExecutingAssembly());
            var original = AccessTools.Method(typeof(Agent), "EquipItemsFromSpawnEquipment");
            var transpiler = AccessTools.Method(typeof(AgentPatches.AgentEquipItemsFromSpawnEquipmentPatch),
                nameof(AgentPatches.AgentEquipItemsFromSpawnEquipmentPatch.Transpiler));
            harmony.Patch(original, null, null, new HarmonyMethod(transpiler));
        }
    }
}
