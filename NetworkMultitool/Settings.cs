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

        #endregion

        #region BASIC

        protected override void FillSettings()
        {
            base.FillSettings();

            AddLanguage(GeneralTab);

            var keymappingsGroup = GeneralTab.AddGroup(CommonLocalize.Settings_Shortcuts);
            var keymappings = AddKeyMappingPanel(keymappingsGroup);
            keymappings.AddKeymapping(NetworkMultitoolTool.ActivationShortcut);
            foreach (var shortcut in NetworkMultitoolTool.BindShortcuts)
                keymappings.AddKeymapping(shortcut);


            var generalGroup = GeneralTab.AddGroup(CommonLocalize.Settings_General);
            AddCheckBox(generalGroup, CommonLocalize.Settings_ShowTooltips, ShowToolTip);
            AddCheckBox(generalGroup, Localize.Settings_AutoHideModePanel, AutoHideModePanel, OnAutoHideChanged);
            AddCheckboxPanel(generalGroup, Localize.Settings_SlopeUnit, SlopeUnite, new string[] { Localize.Settings_SlopeUnitPercentages, Localize.Settings_SlopeUnitDegrees }, OnSlopeUniteChanged);
            if (!Mod.IsNodeSpacer)
                AddIntField(generalGroup, Localize.Settings_SegmentLength, SegmentLength, 80, 50, 200);

            AddNotifications(GeneralTab);
#if DEBUG
            AddDebug(DebugTab);
#endif
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
            static void OnSlopeUniteChanged()
            {
                if (SingletonTool<NetworkMultitoolTool>.Instance?.Mode is SlopeNodeMode slopeNode)
                    slopeNode.RefreshLabels();
            }
        }

        #endregion

        #region GENERAL

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

