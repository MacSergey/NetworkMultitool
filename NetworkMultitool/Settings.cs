using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using NetworkMultitool.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static ModsCommon.SettingsHelper;

namespace NetworkMultitool
{
    public class Settings : BaseSettings<Mod>
    {
        #region PROPERTIES

        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedBool AutoHideModePanel { get; } = new SavedBool(nameof(AutoHideModePanel), SettingsFile, true, true);
        public static SavedInt SlopeUnite { get; } = new SavedInt(nameof(SlopeUnite), SettingsFile, 0, true);
        public static SavedInt SegmentLength { get; } = new SavedInt(nameof(SegmentLength), SettingsFile, 80, true);
        public static SavedInt PanelColumns { get; } = new SavedInt(nameof(PanelColumns), SettingsFile, 2, true);
        public static SavedBool PlayEffects { get; } = new SavedBool(nameof(PlayEffects), SettingsFile, true, true);

        protected UIAdvancedHelper ShortcutsTab => GetTab(nameof(ShortcutsTab));

        #endregion

        #region BASIC

        protected override IEnumerable<KeyValuePair<string, string>> AdditionalTabs
        {
            get
            {
                yield return new KeyValuePair<string, string>(nameof(ShortcutsTab), CommonLocalize.Settings_Shortcuts);
            }
        }
        protected override void FillSettings()
        {
            base.FillSettings();

            AddGeneral();
            AddShortcuts();
#if DEBUG
            AddDebug(DebugTab);
#endif
            
        }

        #endregion

        #region GENERAL

        private void AddGeneral()
        {
            AddLanguage(GeneralTab);

            var generalGroup = GeneralTab.AddGroup(CommonLocalize.Settings_General);
            AddCheckBox(generalGroup, CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            AddCheckBox(generalGroup, Localize.Settings_AutoHideModePanel, AutoHideModePanel, OnAutoHideChanged);
            AddIntField(generalGroup, Localize.Settings_PanelColumns, PanelColumns, 2, 1, 5, OnColumnChanged);
            AddCheckBox(generalGroup, Localize.Settings_PlayEffects, PlayEffects);
            AddCheckboxPanel(generalGroup, Localize.Settings_SlopeUnit, SlopeUnite, new string[] { Localize.Settings_SlopeUnitPercentages, Localize.Settings_SlopeUnitDegrees }, OnSlopeUniteChanged);
            if (Utility.InGame && !Mod.NodeSpacerEnabled)
                AddIntField(generalGroup, Localize.Settings_SegmentLength, SegmentLength, 80, 50, 200);

            AddNotifications(GeneralTab);

            static void OnAutoHideChanged()
            {
                foreach (var panel in UIView.GetAView().GetComponentsInChildren<ModesPanel>())
                {
                    if (AutoHideModePanel)
                        panel.SetState(false, true);
                    else
                        panel.SetState(true);
                }
            }
            static void OnColumnChanged()
            {
                foreach (var panel in UIView.GetAView().GetComponentsInChildren<ModesPanel>())
                    panel.FitChildren();
            }
            static void OnSlopeUniteChanged()
            {
                if (SingletonTool<NetworkMultitoolTool>.Instance?.Mode is SlopeNodeMode slopeNode)
                    slopeNode.RefreshLabels();
            }
        }

        #endregion

        #region SHORTCUTS

        private void AddShortcuts()
        {
            var modesGroup = ShortcutsTab.AddGroup(Localize.Settings_ActivationShortcuts);
            var modesKeymapping = AddKeyMappingPanel(modesGroup);
            modesKeymapping.AddKeymapping(NetworkMultitoolTool.ActivationShortcut);
            foreach (var shortcut in NetworkMultitoolTool.ModeShortcuts.Values)
                modesKeymapping.AddKeymapping(shortcut);

            var generalGroup = ShortcutsTab.AddGroup(Localize.Settings_CommonShortcuts);
            var generalKeymapping = AddKeyMappingPanel(generalGroup);
            generalKeymapping.AddKeymapping(NetworkMultitoolTool.SelectionStepOverShortcut);
            generalKeymapping.AddKeymapping(BaseNetworkMultitoolMode.ApplyShortcut);

            var connectionGroup = ShortcutsTab.AddGroup(Localize.Mode_CreateConnection);
            var connectionKeymapping = AddKeyMappingPanel(connectionGroup);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.IncreaseRadiiShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.DecreaseRadiiShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.SwitchSelectShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.IncreaseOneRadiusShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.DecreaseOneRadiusShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.SwitchOffsetShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.IncreaseOffsetShortcut);
            connectionKeymapping.AddKeymapping(CreateConnectionMode.DecreaseOffsetShortcut);

            var loopGroup = ShortcutsTab.AddGroup(Localize.Mode_CreateLoop);
            var loopKeymapping = AddKeyMappingPanel(loopGroup);
            loopKeymapping.AddKeymapping(CreateLoopMode.IncreaseRadiusShortcut);
            loopKeymapping.AddKeymapping(CreateLoopMode.DecreaseRadiusShortcut);
            loopKeymapping.AddKeymapping(CreateLoopMode.SwitchIsLoopShortcut);

            var parallelGroup = ShortcutsTab.AddGroup(Localize.Mode_CreateParallerl);
            var parallelKeymapping = AddKeyMappingPanel(parallelGroup);
            parallelKeymapping.AddKeymapping(CreateParallelMode.IncreaseShiftShortcut);
            parallelKeymapping.AddKeymapping(CreateParallelMode.DecreaseShiftShortcut);
            parallelKeymapping.AddKeymapping(CreateParallelMode.InvertShiftShortcut);

            var arrangeCircleGroup = ShortcutsTab.AddGroup(Localize.Mode_ArrangeAtCircle);
            var arrangeCircleKeymapping = AddKeyMappingPanel(arrangeCircleGroup);
            arrangeCircleKeymapping.AddKeymapping(ArrangeCircleCompleteMode.ResetArrangeCircleShortcut);
            arrangeCircleKeymapping.AddKeymapping(ArrangeCircleCompleteMode.DistributeEvenlyShortcut);
            arrangeCircleKeymapping.AddKeymapping(ArrangeCircleCompleteMode.DistributeIntersectionsShortcut);
            arrangeCircleKeymapping.AddKeymapping(ArrangeCircleCompleteMode.DistributeBetweenIntersectionsShortcut);
        }

        #endregion

        #region DEBUG
#if DEBUG
        private void AddDebug(UIAdvancedHelper helper)
        {
            var overlayGroup = helper.AddGroup("Selection overlay");

            Selection.AddAlphaBlendOverlay(overlayGroup);
            Selection.AddRenderOverlayCentre(overlayGroup);
            Selection.AddRenderOverlayBorders(overlayGroup);
            Selection.AddBorderOverlayWidth(overlayGroup);
        }
#endif
        #endregion
    }
}

