using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon.UI;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool.UI
{
    public class ModesPanel : UIPanel
    {
        public static float ModeButtonSize => 29f;
        public static float Padding => 2f;
        public Vector2 DefaultSize
        {
            get
            {
                var count = components.OfType<ModeButton>().Count();
                return new Vector2(2 * Padding + Mathf.Min(count, 2) * ModeButtonSize, 2 * Padding + (count / 2) * ModeButtonSize);
            }
        }

        public ModesPanel()
        {
            size = new Vector2(ModeButtonSize, 0f);
            isVisible = false;
            atlas = TextureHelper.InGameAtlas;
            backgroundSprite = "ButtonWhite";
            color = new Color32(64, 64, 64, 255);
            clipChildren = true;
        }
        protected override void OnClick(UIMouseEventParameter p) { }
        protected override void OnComponentAdded(UIComponent child)
        {
            base.OnComponentAdded(child);
            FitChildren();
        }

        protected override void OnComponentRemoved(UIComponent child)
        {
            base.OnComponentRemoved(child);
            FitChildren();
        }
        private new void FitChildren()
        {
            var buttons = components.OfType<ModeButton>().ToArray();

            for (var i = 0; i < buttons.Length; i += 1)
                buttons[i].relativePosition = new Vector2(Padding + (i % 2) * ModeButtonSize, Padding + (i / 2) * ModeButtonSize);

            size = DefaultSize;
            relativePosition = new Vector2((parent.width - width) / 2f, parent.height);
        }
        public void SetState(bool show)
        {
            ValueAnimator.Cancel(nameof(NetworkMultitoolTool));
            if (show)
            {
                Show();
                ValueAnimator.Animate(nameof(NetworkMultitoolTool), val => height = val, new AnimatedFloat(20f, DefaultSize.y, 0.2f, EasingType.CubicEaseOut));
            }
            else
                ValueAnimator.Animate(nameof(NetworkMultitoolTool), val => height = val, new AnimatedFloat(height, 20f, 0.2f, EasingType.CubicEaseIn), () => Hide());
        }
    }
}
