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
        private IEnumerable<ModeButton> Buttons => components.OfType<ModeButton>();
        public Vector2 DefaultSize
        {
            get
            {
                var count = Buttons.Count();
                return new Vector2(2 * Padding + Mathf.Min(count, 2) * ModeButtonSize, 2 * Padding + (count / 2) * ModeButtonSize);
            }
        }
        private OpenSide _openSide = OpenSide.Down;
        private OpenSide Open
        {
            get => _openSide;
            set
            {
                if (value != _openSide)
                {
                    _openSide = value;
                    SetPosition();
                }
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
        public override void Start()
        {
            base.Start();

            var root = parent;
            while (root.parent != null)
                root = root.parent;

            root.eventPositionChanged += ParentPositionChanged;
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
        private void ParentPositionChanged(UIComponent parent, Vector2 value) => SetOpenSide();
        private new void FitChildren()
        {
            size = DefaultSize;
            SetOpenSide();
            SetPosition();
            SetButtonsPosition();
        }
        private void SetOpenSide()
        {
            UIView uiView = parent.GetUIView();
            var screen = uiView.GetScreenResolution();
            Open = parent.absolutePosition.y + parent.height + DefaultSize.y <= screen.y ? OpenSide.Down : OpenSide.Up;
        }
        private void SetPosition() => relativePosition = new Vector2((parent.width - width) / 2f, Open == OpenSide.Down ? parent.height : -height);
        public void SetState(bool show)
        {
            ValueAnimator.Cancel(nameof(NetworkMultitoolTool));
            if (show)
            {
                SetOpenSide();
                Show();
                ValueAnimator.Animate(nameof(NetworkMultitoolTool), OnAnimate, new AnimatedFloat(20f, DefaultSize.y, 0.2f, EasingType.CubicEaseOut));
            }
            else
                ValueAnimator.Animate(nameof(NetworkMultitoolTool), OnAnimate, new AnimatedFloat(height, 20f, 0.2f, EasingType.CubicEaseIn), Hide);
        }
        private void SetButtonsPosition()
        {
            var buttons = Buttons.ToArray();
            var delta = height - DefaultSize.y;

            for (var i = 0; i < buttons.Length; i += 1)
            {
                var x = Padding + (i % 2) * ModeButtonSize;
                var y = Padding + (i / 2) * ModeButtonSize + (Open == OpenSide.Down ? delta : 0f);
                buttons[i].relativePosition = new Vector2(x, y);
            }
        }
        private void OnAnimate(float val)
        {
            height = val;
            SetPosition();
            SetButtonsPosition();
        }

        private enum OpenSide
        {
            Down,
            Up,
        }
    }
}
