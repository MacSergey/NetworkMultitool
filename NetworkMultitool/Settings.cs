using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
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

        public static SavedBool AutoHideModePanel { get; } = new SavedBool(nameof(AutoHideModePanel), SettingsFile, false, true);

        #endregion

        #region BASIC

        protected override void FillSettings()
        {
            base.FillSettings();

            AddLanguage(GeneralTab);

            var keymappingsGroup = GeneralTab.AddGroup(CommonLocalize.Settings_Shortcuts);
            var keymappings = AddKeyMappingPanel(keymappingsGroup);
            keymappings.AddKeymapping(NetworkMultitoolTool.ActivationShortcut);
            foreach (var shortcut in NetworkMultitoolTool.ToolShortcuts)
                keymappings.AddKeymapping(shortcut);


            var generalGroup = GeneralTab.AddGroup(CommonLocalize.Settings_General);
            AddCheckBox(generalGroup, "Auto hide modes panel", AutoHideModePanel, OnAutoHideChanged);

            AddNotifications(GeneralTab);
#if DEBUG
            AddDebug(DebugTab);
#endif
            static void OnAutoHideChanged()
            {
                if(SingletonTool<NetworkMultitoolTool>.Instance?.ModesPanel is UI.ModesPanel panel)
                {
                    if (AutoHideModePanel)
                        panel.SetState(false, true);
                    else
                        panel.SetState(true);
                }
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

