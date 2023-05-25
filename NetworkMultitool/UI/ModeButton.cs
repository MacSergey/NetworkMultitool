using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NetworkMultitool.Utilities;
using System.Collections.Generic;
using UnityEngine;
using static ModsCommon.Utilities.CommonTextures;

namespace NetworkMultitool.UI
{
    public class ModeButton : CustomUIButton
    {
        private static Dictionary<ToolModeType, List<ModeButton>> ButtonsDic { get; } = new Dictionary<ToolModeType, List<ModeButton>>();

        public static int ButtonSize => ButtonIconSize + 2 * ButtonIconPadding;
        private static int ButtonIconSize => 25;
        public static int ButtonIconPadding => 2;
        private static Color32 HoverColor => new Color32(112, 112, 112, 255);
        private static Color32 PressedColor => new Color32(144, 144, 144, 255);
        private static Color32 FocusedColor => new Color32(144, 144, 144, 255);

        private BaseNetworkMultitoolMode Mode { get; set; }
        public ModeButton()
        {
            bgAtlas = CommonTextures.Atlas;
            bgSprites = new SpriteSet(string.Empty, HeaderHover, HeaderHover, HeaderHover, string.Empty);
            bgColors = new ColorSet(default, HoverColor, PressedColor, PressedColor, default);
            selBgSprites = HeaderHover;
            selBgColors = FocusedColor;

            fgAtlas = NetworkMultitoolTextures.Atlas;

            size = new Vector2(ButtonSize, ButtonSize);
            clipChildren = true;
            minimumSize = size;
            IconMode = SpriteMode.Fill;

            IsSelected = false;
        }
        public static void SetState(ToolModeType modeType, bool state)
        {
            if (ButtonsDic.TryGetValue(modeType & ToolModeType.Group, out var buttons))
            {
                foreach (var button in buttons)
                    button.IsSelected = state;
            }
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
            if (ButtonsDic.TryGetValue(Mode.Type & ToolModeType.Group, out var buttons))
                buttons.Remove(this);
        }
        protected override void OnTooltipEnter(UIMouseEventParameter p)
        {
            var shortcut = Mode.ActivationShortcut;
            tooltip = shortcut?.NotSet != false ? Mode.Title : $"{Mode.Title} ({shortcut})";
            base.OnTooltipEnter(p);
        }

        public static void Add(UIComponent parent, BaseNetworkMultitoolMode mode)
        {
            var button = parent.AddUIComponent<ModeButton>();

            button.Mode = mode;
            button.FgSprites = mode.Type.ToString();

            if (!ButtonsDic.TryGetValue(mode.Type & ToolModeType.Group, out var buttons))
            {
                buttons = new List<ModeButton>();
                ButtonsDic[mode.Type & ToolModeType.Group] = buttons;
            }
            buttons.Add(button);
        }
        protected override void OnClick(UIMouseEventParameter p)
        {
            base.OnClick(p);
            SingletonTool<NetworkMultitoolTool>.Instance.SetMode(Mode);
        }
    }
}
