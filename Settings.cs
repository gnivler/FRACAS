using System;
using System.Collections.Generic;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Settings.Base;
using MCM.Abstractions.Settings.Base.Global;

namespace FRACAS
{
    public class Settings : AttributeGlobalSettings<Settings>
    {
        public override string FormatType => "json";
        public override string FolderName => "FRACAS";
        public override string Id { get; } = "FRACAS";
        public override string DisplayName { get; } = "FRACAS";
        

        [SettingPropertyBool("Only randomize weaponry", HintText = "Only randomize weaponry", Order = 0, RequireRestart = false)]
        public bool OnlyWeapons { get; private set; } = false;
    }
}
