using ColossalFramework.UI;
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
using static ColossalFramework.Plugins.PluginManager;

namespace NetworkMultitool
{
    public class Mod : BasePatcherMod<Mod>
    {
        #region PROPERTIES

        protected override string IdRaw => nameof(NetworkMultitool);
        public override List<ModVersion> Versions { get; } = new List<ModVersion>
        {
            new ModVersion(new Version("1.3.3"), new DateTime(2022, 9, 17)),
            new ModVersion(new Version("1.3.2"), new DateTime(2022, 9, 14)),
            new ModVersion(new Version("1.3.1"), new DateTime(2022, 6, 18)),
            new ModVersion(new Version("1.3"), new DateTime(2022, 6, 2)),
            new ModVersion(new Version("1.2"), new DateTime(2021, 8, 23)),
            new ModVersion(new Version("1.1"), new DateTime(2021, 8, 7)),
            new ModVersion(new Version("1.0"), new DateTime(2021, 7, 30)),
        };
        protected override Version RequiredGameVersion => new Version(1, 16, 1, 2);

        public override string NameRaw => "Network Multitool";
        public override string Description => !IsBeta ? Localize.Mod_Description : CommonLocalize.Mod_DescriptionBeta;
        protected override ulong StableWorkshopId => 2560782729ul;
        protected override ulong BetaWorkshopId => 2556133736ul;

        public override string CrowdinUrl => "https://crowdin.com/translate/macsergey-other-mods/114";

#if BETA
        public override bool IsBeta => true;
#else
        public override bool IsBeta => false;
#endif
        #endregion

        protected override LocalizeManager LocalizeManager => Localize.LocaleManager;

        private static PluginSearcher NetworkAnarchySearcher { get; } = PluginUtilities.GetSearcher("Network Anarchy", 2862881785ul);
        private static PluginSearcher FRTSearcher { get; } = PluginUtilities.GetSearcher("Fine Road Tool", 1844442251ul);
        private static PluginSearcher NodeSpacerSearcher { get; } = PluginUtilities.GetSearcher("Node Spacer", 2085018096ul);

        public static bool IsNetworkAnarchy => NetworkAnarchySearcher.GetPlugin() != null;
        public static bool IsFRT => FRTSearcher.GetPlugin() != null;
        public static bool IsNodeSpacer => NodeSpacerSearcher.GetPlugin() != null;

        public static bool NetworkAnarchyEnabled => NetworkAnarchySearcher.GetPlugin() is PluginInfo plugin && plugin.isEnabled;
        public static bool FRTEnabled => FRTSearcher.GetPlugin() is PluginInfo plugin && plugin.isEnabled;
        public static bool NodeSpacerEnabled => NodeSpacerSearcher.GetPlugin() is PluginInfo plugin && plugin.isEnabled;

        static Type _networkAnarchyType;
        static Type _frtType;
        public static Type NetworkAnarchyType => _networkAnarchyType ??= Type.GetType("NetworkAnarchy.NetworkAnarchy");
        public static Type FRTType => _frtType ??= Type.GetType("FineRoadTool.FineRoadTool");

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

            AddAssetPanelOnButtonClicked(typeof(EditRoadPlacementPanel), ref success);
            AddAssetPanelOnButtonClicked(typeof(BeautificationPanel), ref success);
            AddAssetPanelOnButtonClicked(typeof(PublicTransportPanel), ref success);
            AddAssetPanelOnButtonClicked(typeof(RoadsPanel), ref success);

            if (IsNetworkAnarchy)
            {
                success &= NetworkAnarchyUpdate();
                success &= NetworkAnarchyOnGUI();
                success &= NetworkAnarchyCreateOptionPanel();
            }
            else
            {
                if (IsFRT)
                {
                    success &= FineRoadToolUpdate();
                    success &= FineRoadToolOnGUI();
                }
                if (IsNodeSpacer)
                    success &= NodeSpacerStart();
            }

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
        private void AddAssetPanelOnButtonClicked(Type panelType, ref bool success)
        {
            success &= AddPrefix(typeof(Patcher), nameof(Patcher.AssetPanelOnButtonClickedPrefix), panelType, "OnButtonClicked");
            success &= AddPostfix(typeof(Patcher), nameof(Patcher.AssetPanelOnButtonClickedPostfix), panelType, "OnButtonClicked");
        }


        private bool NetworkAnarchyUpdate()
        {
            if (!AddTranspiler(typeof(Patcher), nameof(Patcher.NetworkAnarchyUpdateTranspiler), NetworkAnarchyType, "Update"))
                return AddTranspiler(typeof(Patcher), nameof(Patcher.NetworkAnarchyUpdateTranspiler), NetworkAnarchyType, "FpsBoosterUpdate");
            else
                return true;
        }
        private bool NetworkAnarchyOnGUI()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.NetworkAnarchyOnGUITranspiler), NetworkAnarchyType, "OnGUI");
        }
        private bool NetworkAnarchyCreateOptionPanel()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.NetworkAnarchyCreateOptionPanelPostfix), Type.GetType("NetworkAnarchy.UIToolOptionsButton"), "CreateOptionPanel");
        }


        private bool FineRoadToolUpdate()
        {
            if (!AddTranspiler(typeof(Patcher), nameof(Patcher.FineRoadToolUpdateTranspiler), FRTType, "Update"))
                return AddTranspiler(typeof(Patcher), nameof(Patcher.FineRoadToolUpdateTranspiler), FRTType, "FpsBoosterUpdate");
            else
                return true;
        }
        private bool FineRoadToolOnGUI()
        {
            return AddTranspiler(typeof(Patcher), nameof(Patcher.FineRoadToolOnGUITranspiler), FRTType, "OnGUI");
        }
        private bool NodeSpacerStart()
        {
            return AddPostfix(typeof(Patcher), nameof(Patcher.NodeSpacerPostfix), Type.GetType("NodeSpacer.ModUI"), "Start");
        }



        #endregion
    }
    public static class Patcher
    {
        public static IEnumerable<CodeInstruction> ToolControllerAwakeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.ToolControllerAwakeTranspiler<Mod, NetworkMultitoolTool>(generator, instructions);

        public static void GeneratedScrollPanelCreateOptionPanelPostfix(string templateName, ref OptionPanelBase __result) => ModsCommon.Patcher.GeneratedScrollPanelCreateOptionPanelPostfix<Mod, NetworkMultitoolButton>(templateName, ref __result, ModsCommon.Patcher.RoadsOptionPanel, ModsCommon.Patcher.TracksOptionPanel, ModsCommon.Patcher.PathsOptionPanel, ModsCommon.Patcher.CanalsOptionPanel, ModsCommon.Patcher.QuaysOptionPanel, ModsCommon.Patcher.FloodWallsOptionPanel);

        private static bool WasActive { get; set; }
        public static void AssetPanelOnButtonClickedPrefix()
        {
            WasActive = SingletonTool<NetworkMultitoolTool>.Instance.enabled;
        }
        public static void AssetPanelOnButtonClickedPostfix()
        {
            if (WasActive)
                SingletonTool<NetworkMultitoolTool>.Instance.Enable();
        }

        public static IEnumerable<CodeInstruction> GameKeyShortcutsEscapeTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => ModsCommon.Patcher.GameKeyShortcutsEscapeTranspiler<Mod, NetworkMultitoolTool>(generator, instructions);


        public static IEnumerable<CodeInstruction> FineRoadToolUpdateTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => UpdateTranspiler(generator, instructions, Mod.FRTType);
        public static IEnumerable<CodeInstruction> NetworkAnarchyUpdateTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => UpdateTranspiler(generator, instructions, Mod.NetworkAnarchyType);


        public static IEnumerable<CodeInstruction> UpdateTranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, Type type)
        {
            var enabledProperty = AccessTools.PropertyGetter(typeof(UnityEngine.Behaviour), nameof(UnityEngine.Behaviour.enabled));
            var netToolField = AccessTools.Field(type, "m_netTool");
            var prefabField = AccessTools.Field(typeof(NetTool), nameof(NetTool.m_prefab));
            var prev = default(CodeInstruction);
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (prev != null && prev.opcode == OpCodes.Ldfld && prev.operand == netToolField && instruction.opcode == OpCodes.Callvirt && instruction.operand == enabledProperty)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.Enabled), new Type[] { typeof(bool)}));
                }
                else if (instruction.opcode == OpCodes.Ldfld && instruction.operand == prefabField)
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.GetPrefab)));
                }
                prev = instruction;
            }
        }
        private static bool Enabled(bool netToolEnabled) => netToolEnabled || SingletonTool<NetworkMultitoolTool>.Instance.enabled;
        private static bool Enabled() => SingletonTool<NetworkMultitoolTool>.Instance.enabled;
        private static NetInfo GetPrefab(NetInfo info) => info ?? PrefabCollection<NetInfo>.FindLoaded("Basic Road");


        public static IEnumerable<CodeInstruction> NetworkAnarchyOnGUITranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => OnGUITranspiler(generator, instructions, Mod.FRTType);
        public static IEnumerable<CodeInstruction> FineRoadToolOnGUITranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions) => OnGUITranspiler(generator, instructions, Mod.NetworkAnarchyType);

        public static IEnumerable<CodeInstruction> OnGUITranspiler(ILGenerator generator, IEnumerable<CodeInstruction> instructions, Type type)
        {
            var modeField = AccessTools.Field(type, "m_mode");
            var prev = default(CodeInstruction);
            var label = generator.DefineLabel();
            var count = 0;
            var labelAdded = false;
            foreach (var inst in instructions)
            {
                var instruction = inst;
                if (prev != null && prev.opcode == OpCodes.Ldarg_0 && instruction.opcode == OpCodes.Ldfld && instruction.operand == modeField)
                {
                    count += 1;
                    if (count == 3)
                    {
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patcher), nameof(Patcher.Enabled), new Type[0]))
                        {
                            labels = prev.labels,
                        };
                        yield return new CodeInstruction(OpCodes.Brtrue_S, label);
                        prev.labels = new List<Label>();
                    }
                }
                else if (instruction.opcode == OpCodes.Leave_S && !labelAdded)
                {
                    labelAdded = true;
                    instruction.labels.Add(label);
                }
                else if (instruction.opcode == OpCodes.Ldc_I4_S && instruction.operand is sbyte value && value == (sbyte)InfoManager.InfoMode.Traffic)
                    instruction = new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)InfoManager.InfoMode.Underground);

                if (prev != null)
                    yield return prev;

                prev = instruction;
            }
            yield return prev;
        }

        public static void NodeSpacerPostfix(UISlider ___m_maxLengthSlider)
        {
            ___m_maxLengthSlider.eventValueChanged += (_, _) =>
            {
                if (SingletonTool<NetworkMultitoolTool>.Instance.Mode is BaseCreateMode mode)
                    mode.Recalculate();
            };
        }

        public static void NetworkAnarchyCreateOptionPanelPostfix(UISlider ___m_maxSegmentLengthSlider)
        {
            ___m_maxSegmentLengthSlider.eventValueChanged += (_, _) =>
            {
                if (SingletonTool<NetworkMultitoolTool>.Instance.Mode is BaseCreateMode mode)
                    mode.Recalculate();
            };
        }
    }
    public class LoadingExtension : BaseLoadingExtension<Mod> { }
}
