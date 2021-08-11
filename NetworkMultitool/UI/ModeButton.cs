using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NetworkMultitool.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool.UI
{
    public class ModeButton : CustomUIButton
    {
        private static Dictionary<ToolModeType, List<ModeButton>> ButtonsDic { get; } = new Dictionary<ToolModeType, List<ModeButton>>();

        public static int Size => IconSize + 2 * IconPadding;
        public static int IconSize => 25;
        public static int IconPadding => 2;
        private static Color32 HoverColor => new Color32(112, 112, 112, 255);
        private static Color32 PressedColor => new Color32(144, 144, 144, 255);
        private static Color32 FocusedColor => new Color32(144, 144, 144, 255);

        public CustomUIButton Icon { get; }
        private BaseNetworkMultitoolMode Mode { get; set; }
        public bool Activate 
        { 
            set 
            {
                if (value)
                {
                    normalBgSprite = CommonTextures.HeaderHoverSprite;
                    hoveredColor = FocusedColor;
                    pressedColor = FocusedColor;
                    focusedColor = FocusedColor;
                }
                else
                {
                    normalBgSprite = string.Empty;
                    hoveredColor = HoverColor;
                    pressedColor = PressedColor;
                    focusedColor = PressedColor;
                }
            } 
        }
        public ModeButton()
        {
            atlas = CommonTextures.Atlas;
            hoveredBgSprite = CommonTextures.HeaderHoverSprite;
            pressedBgSprite = CommonTextures.HeaderHoverSprite;
            focusedBgSprite = CommonTextures.HeaderHoverSprite;
            color = FocusedColor;
            size = new Vector2(Size, Size);
            clipChildren = true;
            minimumSize = size;

            Icon = AddUIComponent<CustomUIButton>();
            Icon.size = new Vector2(IconSize, IconSize);
            Icon.relativePosition = new Vector2(IconPadding, IconPadding);
            Icon.atlas = NetworkMultitoolTextures.Atlas;

            Activate = false;
        }
        public static void SetState(ToolModeType modeType, bool state)
        {
            if (ButtonsDic.TryGetValue(modeType & ToolModeType.Group, out var buttons))
            {
                foreach (var button in buttons)
                    button.Activate = state;
            }
        }

        public override void Update()
        {
            base.Update();
            if (state == ButtonState.Focused)
                state = ButtonState.Normal;
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
            var sprite = mode.Type.ToString();
            button.Icon.normalBgSprite = sprite;
            button.Icon.hoveredBgSprite = sprite;
            button.Icon.pressedBgSprite = sprite;

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
