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

            var interfaceSection = GeneralTab.AddOptionsSection(Localize.Settings_Interface);
            AddToolButton<NetworkMultitoolTool, NetworkMultitoolButton>(interfaceSection);
            interfaceSection.AddToggle(CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            interfaceSection.AddToggle(Localize.Settings_AutoHideModePanel, AutoHideModePanel, OnAutoHideChanged);

            if (NetworkMultitoolTool.IsUUIEnabled)
                interfaceSection.AddTogglePanel(Localize.Settings_PanelOpenSide, PanelOpenSide, new string[] { Localize.Settings_PanelOpenSideDown, Localize.Settings_PanelOpenSideUp }, OnOpenSideChanged);

            var column = interfaceSection.AddIntField(Localize.Settings_PanelColumns, PanelColumns, 1, 5, OnColumnChanged);
            column.Control.width = 60f;
            interfaceSection.AddToggle(Localize.Settings_PlayEffects, PlayEffects);
            interfaceSection.AddTogglePanel(Localize.Settings_PreviewType, NetworkPreview, new string[] { Localize.Settings_PreviewTypeOverlay, Localize.Settings_PreviewTypeMesh, Localize.Settings_PreviewTypeBoth });
            interfaceSection.AddToggle(Localize.Settings_SlopeColors, SlopeColors, OnSlopeColorChanged);

            var gameplaySection = GeneralTab.AddOptionsSection(Localize.Settings_Gameplay);
            gameplaySection.AddToggle(Localize.Settings_NeedMoney, NeedMoney);
            gameplaySection.AddToggle(Localize.Settings_FollowTerrain, FollowTerrain);
            gameplaySection.AddTogglePanel(Localize.Settings_LengthUnit, LengthUnite, new string[] { Localize.Settings_LengthUniteMeters, Localize.Settings_LengthUniteUnits }, OnSlopeUniteChanged);
            gameplaySection.AddTogglePanel(Localize.Settings_SlopeUnit, SlopeUnite, new string[] { Localize.Settings_SlopeUnitPercentages, Localize.Settings_SlopeUnitDegrees }, OnSlopeUniteChanged);
            gameplaySection.AddToggle(Localize.Settings_AutoConnect, AutoConnect);
            if (Utility.InGame && !Mod.NetworkAnarchyEnabled)
                gameplaySection.AddIntField(Localize.Settings_SegmentLength, SegmentLength, 50, 200);

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
            var modesSection = ShortcutsTab.AddOptionsSection(Localize.Settings_ActivationShortcuts);
            modesSection.AddKeyMappingButton(NetworkMultitoolTool.ActivationShortcut);
            foreach (var shortcut in NetworkMultitoolTool.ModeShortcuts.Values)
                modesSection.AddKeyMappingButton(shortcut);

            var generalSection = ShortcutsTab.AddOptionsSection(Localize.Settings_CommonShortcuts);
            generalSection.AddKeyMappingButton(NetworkMultitoolTool.SelectionStepOverShortcut);
            generalSection.AddKeyMappingButton(BaseNetworkMultitoolMode.ApplyShortcut);

            var commonSection = ShortcutsTab.AddOptionsSection(Localize.Settings_CommonCreateShortcuts);
            commonSection.AddKeyMappingButton(BaseCreateMode.SwitchFollowTerrainShortcut);
            commonSection.AddKeyMappingButton(BaseCreateMode.SwitchOffsetShortcut);
            commonSection.AddKeyMappingButton(BaseCreateMode.IncreaseAngleShortcut);
            commonSection.AddKeyMappingButton(BaseCreateMode.DecreaseAngleShortcut);
            commonSection.AddKeyMappingButton(BaseNetworkMultitoolMode.InvertNetworkShortcut);

            var connectionSection = ShortcutsTab.AddOptionsSection(Localize.Mode_CreateConnection);
            connectionSection.AddKeyMappingButton(CreateConnectionMode.IncreaseRadiiShortcut);
            modesSection.AddKeyMappingButton(CreateConnectionMode.DecreaseRadiiShortcut);
            modesSection.AddKeyMappingButton(CreateConnectionMode.SwitchSelectShortcut);
            modesSection.AddKeyMappingButton(CreateConnectionMode.IncreaseOneRadiusShortcut);
            generalSection.AddKeyMappingButton(CreateConnectionMode.DecreaseOneRadiusShortcut);
            generalSection.AddKeyMappingButton(CreateConnectionMode.IncreaseOffsetShortcut);
            generalSection.AddKeyMappingButton(CreateConnectionMode.DecreaseOffsetShortcut);

            var loopSection = ShortcutsTab.AddOptionsSection(Localize.Mode_CreateLoop);
            loopSection.AddKeyMappingButton(CreateLoopMode.IncreaseRadiusShortcut);
            loopSection.AddKeyMappingButton(CreateLoopMode.DecreaseRadiusShortcut);
            loopSection.AddKeyMappingButton(CreateLoopMode.SwitchIsLoopShortcut);

            var parallelSection = ShortcutsTab.AddOptionsSection(Localize.Mode_CreateParallerl);
            parallelSection.AddKeyMappingButton(CreateParallelMode.IncreaseShiftShortcut);
            parallelSection.AddKeyMappingButton(CreateParallelMode.DecreaseShiftShortcut);
            parallelSection.AddKeyMappingButton(CreateParallelMode.IncreaseHeightShortcut);
            parallelSection.AddKeyMappingButton(CreateParallelMode.DecreaseHeightShortcut);
            parallelSection.AddKeyMappingButton(CreateParallelMode.ChangeSideShortcut);

            var arrangeCircleSection = ShortcutsTab.AddOptionsSection(Localize.Mode_ArrangeAtCircle);
            arrangeCircleSection.AddKeyMappingButton(ArrangeCircleCompleteMode.ResetArrangeCircleShortcut);
            arrangeCircleSection.AddKeyMappingButton(ArrangeCircleCompleteMode.DistributeEvenlyShortcut);
            arrangeCircleSection.AddKeyMappingButton(ArrangeCircleCompleteMode.DistributeIntersectionsShortcut);
            arrangeCircleSection.AddKeyMappingButton(ArrangeCircleCompleteMode.DistributeBetweenIntersectionsShortcut);
        }

        #endregion

        #region DEBUG
#if DEBUG
        private void AddDebug(UIComponent helper)
        {
            var overlaySection = helper.AddOptionsSection("Selection overlay");

            Selection.AddAlphaBlendOverlay(overlaySection);
            Selection.AddRenderOverlayCentre(overlaySection);
            Selection.AddRenderOverlayBorders(overlaySection);
            Selection.AddBorderOverlayWidth(overlaySection);
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

