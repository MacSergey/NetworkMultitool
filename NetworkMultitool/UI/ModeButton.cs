using ColossalFramework.UI;
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
        public void Init(BaseNetworkMultitoolMode mode, string sprite)
        {
            Mode = mode;
            Icon.normalBgSprite = sprite;
            Icon.hoveredBgSprite = sprite;
            Icon.pressedBgSprite = sprite;
        }
        public override void Update()
        {
            base.Update();
            if (state == ButtonState.Focused)
                state = ButtonState.Normal;
        }
        protected override void OnTooltipEnter(UIMouseEventParameter p)
        {
            tooltip = $"{Mode.Title} ({Mode.ActivationShortcut})";
            base.OnTooltipEnter(p);
        }
    }
}
