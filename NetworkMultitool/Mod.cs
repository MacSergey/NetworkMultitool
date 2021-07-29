using HarmonyLib;
using ICities;
using ModsCommon;
using ModsCommon.Utilities;
using NetworkMultitool.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Emit;
using System.Resources;
using System.Text;

namespace NetworkMultitool
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        protected override string IdRaw => nameof(NetworkMultitool);
        public override List<Version> Versions { get; } = new List<Version>
        {
            new Version("1.0")
        };

        public override string NameRaw => "Network Multitool";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;
        protected override ulong StableWorkshopId => 0ul;
        protected override ulong BetaWorkshopId => 2556133736ul;

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        #endregion

        protected override ResourceManager LocalizeManager => Localize.ResourceManager;
        private static PluginSearcher FRTSearcher { get; } = PluginUtilities.GetSearcher("Fine Road Tool", 1844442251ul);
        private static bool IsFRT => FRTSearcher.GetPlugin() != null;

        #region BASIC

        protected override void GetSettings(UIHelperBase helper)
        {
            var settings = new Settings();
            settings.OnSettingsUI(helper);
        }
        protected override void SetCulture(CultureInfo culture) => Localize.Culture = culture;

        #endregion

        #region PATCHES

        protected override bool PatchProcess()
        {
            var success = true;

            success &= AddTool();
            success &= AddNetToolButton();
            success &= ToolOnEscape();
            if (IsFRT)
                success &= FineRoadToolUpdate();

            return success;
        }

        private bool AddTool()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.ToolControllerAwakeTranspiler), typeof(ToolController), "Awake");
        }
        private bool AddNetToolButton()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.GeneratedScrollPanelCreateOptionPanelPostfix), typeof(GeneratedScrollPanel), "CreateOptionPanel");
        }
        private bool ToolOnEscape()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.GameKeyShortcutsEscapeTranspiler), typeof(GameKeyShortcuts), "Escape");
        }
        private bool FineRoadToolUpdate()
        {
            if (!AddTranspiler(typeof(Patcher), nameof(Patcher.FineRoadToolTranspiler), Type.GetType("FineRoadTool.FineRoadTool"), "Update"))
                return AddTranspiler(typeof(Patcher), nameof(Patcher.FineRoadToolTranspiler), Type.GetType("FineRoadTool.FineRoadTool"), "FpsBoosterUpdate");
            else
                return true;
        }

        #endregion
    }
    public static class Patcher
    {
        public static IEnumerable<CodeInstruction> ToolControllerAwakeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.ToolControllerAwakeTranspiler<Mod, NetworkMultitoolTool>(generator, instructions);

        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result) => ModsCommon.Patcher.GeneratedScrollPanelCreateOptionPanelPostfix<Mod, NetworkMultitoolButton>(templateName, ref __result, ModsCommon.Patcher.RoadsOptionPanel, ModsCommon.Patcher.PathsOptionPanel, ModsCommon.Patcher.CanalsOptionPanel, ModsCommon.Patcher.QuaysOptionPanel, ModsCommon.Patcher.FloodWallsOptionPanel);

        public static IEnumerable<CodeInstruction> GameKeyShortcutsEscapeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.GameKeyShortcutsEscapeTranspiler<Mod, NetworkMultitoolTool>(generator, instructions);

        public static IEnumerable<CodeInstruction> FineRoadToolTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions)
        {
            var patched = false;
            foreach(var instruction in instructions)
            {
                yield return instruction;

                if (!patched && instruction.opcode == OpCodes.Brtrue_S)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Enabled)));
                    yield return instruction;
                }
            }
        }
        private static bool Enabled() => SingletonTool<NetworkMultitoolTool>.Instance.enabled;
    }
    public class LoadingExtension : BaseLoadingExtension<Mod>
    {
        protected override void OnLoad()
        {
            SingletonTool<NetworkMultitoolTool>.Instance.RegisterUUI();
            base.OnLoad();
        }
    }
}
