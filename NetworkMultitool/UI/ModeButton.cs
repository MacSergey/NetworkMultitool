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
    public class ModeButton : MultyAtlasUIButton
    {
        private static Dictionary<ToolModeType, List<ModeButton>> ButtonsDic { get; } = new Dictionary<ToolModeType, List<ModeButton>>();

        public static int Size => IconSize + 2 * IconPadding;
        public static int IconSize => 25;
        public static int IconPadding => 2;
        private static Color32 HoverColor => new Color32(112, 112, 112, 255);
        private static Color32 PressedColor => new Color32(144, 144, 144, 255);
        private static Color32 FocusedColor => new Color32(144, 144, 144, 255);

        private BaseNetworkMultitoolMode Mode { get; set; }
        public bool Activate 
        { 
            set 
            {
                if (value)
                {
                    normalBgSprite = CommonTextures.HeaderHover;
                    hoveredBgColor = FocusedColor;
                    pressedBgColor = FocusedColor;
                    focusedBgColor = FocusedColor;
                }
                else
                {
                    normalBgSprite = string.Empty;
                    hoveredBgColor = HoverColor;
                    pressedBgColor = PressedColor;
                    focusedBgColor = PressedColor;
                }
            } 
        }
        public ModeButton()
        {
            atlasBackground = CommonTextures.Atlas;
            atlasForeground = NetworkMultitoolTextures.Atlas;
            hoveredBgSprite = CommonTextures.HeaderHover;
            pressedBgSprite = CommonTextures.HeaderHover;
            focusedBgSprite = CommonTextures.HeaderHover;
            normalBgColor = FocusedColor;
            size = new Vector2(Size, Size);
            clipChildren = true;
            minimumSize = size;
            foregroundSpriteMode = UIForegroundSpriteMode.Fill;

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
            button.normalFgSprite = sprite;
            button.hoveredFgSprite = sprite;
            button.pressedFgSprite = sprite;

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
