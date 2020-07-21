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
using static FRACAS.Helpers;

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

        internal static void Log(object input, LogLevel logLevel)
        {
            if (logging >= logLevel)
            {
                FileLog.Log($"[FRACAS] {input ?? "null"}");
            }
        }

        protected override void OnSubModuleLoad()
        {
            Log("Startup " + DateTime.Now.ToShortTimeString(), LogLevel.Warning);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            var original = AccessTools.Method(typeof(Agent), "EquipItemsFromSpawnEquipment");
            var transpiler = AccessTools.Method(typeof(Patches.Agent.AgentEquipItemsFromSpawnEquipmentPatch),
                nameof(Patches.Agent.AgentEquipItemsFromSpawnEquipmentPatch.Transpiler));
            harmony.Patch(original, null, null, new HarmonyMethod(transpiler));

            original = AccessTools.Method(typeof(MapScreen), "OnInitialize");
            var postfix = AccessTools.Method(typeof(Patches.MapScreen.MapScreenOnInitializePatch), nameof(Patches.MapScreen.MapScreenOnInitializePatch.Postfix));
            harmony.Patch(original, null, new HarmonyMethod(postfix));
        }
    }
}
