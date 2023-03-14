using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Settings;
using ModsCommon.Utilities;
using NetworkMultitool.UI;
using System.Collections.Generic;
using static ModsCommon.Settings.Helper;

namespace NetworkMultitool
{
    public class Settings : BaseSettings<Mod>
    {
        #region PROPERTIES

        public static SavedBool ShowToolTip { get; } = new SavedBool(nameof(ShowToolTip), SettingsFile, true, true);
        public static SavedBool AutoHideModePanel { get; } = new SavedBool(nameof(AutoHideModePanel), SettingsFile, true, true);
        public static SavedInt PanelOpenSide { get; } = new SavedInt(nameof(PanelOpenSide), SettingsFile, (int)OpenSide.Down, true);
        public static SavedInt SlopeUnite { get; } = new SavedInt(nameof(SlopeUnite), SettingsFile, 0, true);
        public static SavedBool SlopeColors { get; } = new SavedBool(nameof(SlopeColors), SettingsFile, true, true);
        public static SavedInt LengthUnite { get; } = new SavedInt(nameof(LengthUnite), SettingsFile, 0, true);
        public static SavedInt SegmentLength { get; } = new SavedInt(nameof(SegmentLength), SettingsFile, 80, true);
        public static SavedInt PanelColumns { get; } = new SavedInt(nameof(PanelColumns), SettingsFile, 2, true);
        public static SavedBool PlayEffects { get; } = new SavedBool(nameof(PlayEffects), SettingsFile, true, true);
        public static SavedInt NetworkPreview { get; } = new SavedInt(nameof(NetworkPreview), SettingsFile, (int)PreviewType.Both, true);
        public static SavedBool FollowTerrain { get; } = new SavedBool(nameof(FollowTerrain), SettingsFile, false, true);
        public static SavedBool NeedMoney { get; } = new SavedBool(nameof(NeedMoney), SettingsFile, true, true);
        public static SavedBool AutoConnect { get; } = new SavedBool(nameof(AutoConnect), SettingsFile, true, true);

        public static bool ShowOverlay => NetworkPreview != (int)PreviewType.Mesh;
        public static bool ShowMesh => NetworkPreview != (int)PreviewType.Overlay;

        protected UIComponent ShortcutsTab => GetTabContent(nameof(ShortcutsTab));

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

            var interfaceGroup = GeneralTab.AddOptionsGroup(Localize.Settings_Interface);
            AddToolButton<NetworkMultitoolTool, NetworkMultitoolButton>(interfaceGroup);
            interfaceGroup.AddToggle(CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            interfaceGroup.AddToggle(Localize.Settings_AutoHideModePanel, AutoHideModePanel, OnAutoHideChanged);

            if (NetworkMultitoolTool.IsUUIEnabled)
                interfaceGroup.AddTogglePanel(Localize.Settings_PanelOpenSide, PanelOpenSide, new string[] { Localize.Settings_PanelOpenSideDown, Localize.Settings_PanelOpenSideUp }, OnOpenSideChanged);

            interfaceGroup.AddIntField(Localize.Settings_PanelColumns, PanelColumns, 1, 5, OnColumnChanged);
            interfaceGroup.AddToggle(Localize.Settings_PlayEffects, PlayEffects);
            interfaceGroup.AddTogglePanel(Localize.Settings_PreviewType, NetworkPreview, new string[] { Localize.Settings_PreviewTypeOverlay, Localize.Settings_PreviewTypeMesh, Localize.Settings_PreviewTypeBoth });
            interfaceGroup.AddToggle(Localize.Settings_SlopeColors, SlopeColors, OnSlopeColorChanged);

            var gameplayGroup = GeneralTab.AddOptionsGroup(Localize.Settings_Gameplay);
            gameplayGroup.AddToggle(Localize.Settings_NeedMoney, NeedMoney);
            gameplayGroup.AddToggle(Localize.Settings_FollowTerrain, FollowTerrain);
            gameplayGroup.AddTogglePanel(Localize.Settings_LengthUnit, LengthUnite, new string[] { Localize.Settings_LengthUniteMeters, Localize.Settings_LengthUniteUnits }, OnSlopeUniteChanged);
            gameplayGroup.AddTogglePanel(Localize.Settings_SlopeUnit, SlopeUnite, new string[] { Localize.Settings_SlopeUnitPercentages, Localize.Settings_SlopeUnitDegrees }, OnSlopeUniteChanged);
            gameplayGroup.AddToggle(Localize.Settings_AutoConnect, AutoConnect);
            if (Utility.InGame && !Mod.NodeSpacerEnabled)
                gameplayGroup.AddIntField(Localize.Settings_SegmentLength, SegmentLength, 50, 200);

            AddNotifications(GeneralTab);

            static void OnAutoHideChanged(bool state)
            {
                foreach (var panel in UIView.GetAView().GetComponentsInChildren<ModesPanel>())
                {
                    if (state)
                        panel.SetState(false, true);
                    else
                        panel.SetState(true);
                }
            }
            static void OnOpenSideChanged(int value)
            {
                foreach (var panel in UIView.GetAView().GetComponentsInChildren<ModesPanel>())
                    panel.SetOpenSide();
            }
            static void OnColumnChanged(int column)
            {
                foreach (var panel in UIView.GetAView().GetComponentsInChildren<ModesPanel>())
                    panel.FitChildren();
            }
            static void OnSlopeColorChanged(bool value)
            {
                if (SingletonTool<NetworkMultitoolTool>.Instance?.Mode is SlopeNodeMode slopeNode)
                    slopeNode.RefreshLabels();
            }
            static void OnSlopeUniteChanged(int value)
            {
                if (SingletonTool<NetworkMultitoolTool>.Instance?.Mode is SlopeNodeMode slopeNode)
                    slopeNode.RefreshLabels();
            }
        }

        #endregion

        #region SHORTCUTS

        private void AddShortcuts()
        {
            var modesGroup = ShortcutsTab.AddOptionsGroup(Localize.Settings_ActivationShortcuts);
            modesGroup.AddKeyMappingButton(NetworkMultitoolTool.ActivationShortcut);
            foreach (var shortcut in NetworkMultitoolTool.ModeShortcuts.Values)
                modesGroup.AddKeyMappingButton(shortcut);

            var generalGroup = ShortcutsTab.AddOptionsGroup(Localize.Settings_CommonShortcuts);
            generalGroup.AddKeyMappingButton(NetworkMultitoolTool.SelectionStepOverShortcut);
            generalGroup.AddKeyMappingButton(BaseNetworkMultitoolMode.ApplyShortcut);

            var commonGroup = ShortcutsTab.AddOptionsGroup(Localize.Settings_CommonCreateShortcuts);
            commonGroup.AddKeyMappingButton(BaseCreateMode.SwitchFollowTerrainShortcut);
            commonGroup.AddKeyMappingButton(BaseCreateMode.SwitchOffsetShortcut);
            commonGroup.AddKeyMappingButton(BaseCreateMode.IncreaseAngleShortcut);
            commonGroup.AddKeyMappingButton(BaseCreateMode.DecreaseAngleShortcut);
            commonGroup.AddKeyMappingButton(BaseNetworkMultitoolMode.InvertNetworkShortcut);

            var connectionGroup = ShortcutsTab.AddOptionsGroup(Localize.Mode_CreateConnection);
            connectionGroup.AddKeyMappingButton(CreateConnectionMode.IncreaseRadiiShortcut);
            modesGroup.AddKeyMappingButton(CreateConnectionMode.DecreaseRadiiShortcut);
            modesGroup.AddKeyMappingButton(CreateConnectionMode.SwitchSelectShortcut);
            modesGroup.AddKeyMappingButton(CreateConnectionMode.IncreaseOneRadiusShortcut);
            generalGroup.AddKeyMappingButton(CreateConnectionMode.DecreaseOneRadiusShortcut);
            generalGroup.AddKeyMappingButton(CreateConnectionMode.IncreaseOffsetShortcut);
            generalGroup.AddKeyMappingButton(CreateConnectionMode.DecreaseOffsetShortcut);

            var loopGroup = ShortcutsTab.AddOptionsGroup(Localize.Mode_CreateLoop);
            loopGroup.AddKeyMappingButton(CreateLoopMode.IncreaseRadiusShortcut);
            loopGroup.AddKeyMappingButton(CreateLoopMode.DecreaseRadiusShortcut);
            loopGroup.AddKeyMappingButton(CreateLoopMode.SwitchIsLoopShortcut);

            var parallelGroup = ShortcutsTab.AddOptionsGroup(Localize.Mode_CreateParallerl);
            parallelGroup.AddKeyMappingButton(CreateParallelMode.IncreaseShiftShortcut);
            parallelGroup.AddKeyMappingButton(CreateParallelMode.DecreaseShiftShortcut);
            parallelGroup.AddKeyMappingButton(CreateParallelMode.IncreaseHeightShortcut);
            parallelGroup.AddKeyMappingButton(CreateParallelMode.DecreaseHeightShortcut);
            parallelGroup.AddKeyMappingButton(CreateParallelMode.ChangeSideShortcut);

            var arrangeCircleGroup = ShortcutsTab.AddOptionsGroup(Localize.Mode_ArrangeAtCircle);
            arrangeCircleGroup.AddKeyMappingButton(ArrangeCircleCompleteMode.ResetArrangeCircleShortcut);
            arrangeCircleGroup.AddKeyMappingButton(ArrangeCircleCompleteMode.DistributeEvenlyShortcut);
            arrangeCircleGroup.AddKeyMappingButton(ArrangeCircleCompleteMode.DistributeIntersectionsShortcut);
            arrangeCircleGroup.AddKeyMappingButton(ArrangeCircleCompleteMode.DistributeBetweenIntersectionsShortcut);
        }

        #endregion

        #region DEBUG
#if DEBUG
        private void AddDebug(UIComponent helper)
        {
            var overlayGroup = helper.AddOptionsGroup("Selection overlay");

            Selection.AddAlphaBlendOverlay(overlayGroup);
            Selection.AddRenderOverlayCentre(overlayGroup);
            Selection.AddRenderOverlayBorders(overlayGroup);
            Selection.AddBorderOverlayWidth(overlayGroup);
        }
#endif
        #endregion

        public enum PreviewType
        {
            Overlay = 0,
            Mesh = 1,
            Both = 2,
        }
    }
}

