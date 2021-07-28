using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon;
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
        public bool IsHover
        {
            get
            {
                var mouse = SingletonTool<NetworkMultitoolTool>.Instance.MousePosition;
                return (isVisible && this.IsHover(mouse)) || (parent.isVisible && parent.IsHover(mouse));
            }
        }
        private OpenState State { get; set; } = OpenState.Close;
        private OpenSide _openSide = OpenSide.Down;
        private OpenSide OpenSide
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

            parent.eventMouseEnter += ParentMouseEnter;
            parent.eventMouseLeave += ParentMouseLeave;

            var root = parent;
            while (root.parent != null)
                root = root.parent;

            root.eventPositionChanged += ParentPositionChanged;
        }
        private void ParentMouseEnter(UIComponent component, UIMouseEventParameter eventParam) => SetState(true, true);
        private void ParentMouseLeave(UIComponent component, UIMouseEventParameter eventParam) => SetState(false, true);
        protected override void OnMouseLeave(UIMouseEventParameter eventParam)
        {
            base.OnMouseLeave(eventParam);
            SetState(false, true);
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
            OpenSide = parent.absolutePosition.y + parent.height + DefaultSize.y <= screen.y ? OpenSide.Down : OpenSide.Up;
        }
        private void SetPosition() => relativePosition = new Vector2((parent.width - width) / 2f, OpenSide == OpenSide.Down ? parent.height : -height);

        public void SetState(bool show, bool auto = false)
        {
            var autoHide = Settings.AutoHideModePanel.value;
            var enabled = SingletonTool<NetworkMultitoolTool>.Instance.enabled;
            var isHover = IsHover;
            if (show)
            {
                if (State != OpenState.Open && State != OpenState.Opening)
                {
                    if(enabled && ((!auto && (!autoHide || isHover)) || (auto && autoHide)))
                    {
                        StartOpening();
                        SetOpenSide();
                        var time = 0.2f / DefaultSize.y * height;
                        ValueAnimator.Cancel(nameof(NetworkMultitoolTool));
                        ValueAnimator.Animate(nameof(NetworkMultitoolTool), OnAnimate, new AnimatedFloat(height, DefaultSize.y, time, EasingType.CubicEaseOut), EndOpening);
                    }
                }
            }
            else
            {
                if (State != OpenState.Close && State != OpenState.Closing)
                {
                    if (!auto || autoHide)
                    {
                        StartClosing();
                        var time = 0.2f / DefaultSize.y * height;
                        ValueAnimator.Cancel(nameof(NetworkMultitoolTool));
                        ValueAnimator.Animate(nameof(NetworkMultitoolTool), OnAnimate, new AnimatedFloat(height, 20f, time, EasingType.CubicEaseIn), EndClosing);
                    }
                }
            }
        }
        private void StartOpening()
        {
            State = OpenState.Opening;
            Show();
        }
        private void EndOpening() => State = OpenState.Open;
        private void StartClosing() => State = OpenState.Closing;
        private void EndClosing()
        {
            State = OpenState.Close;
            Hide();
        }

        private void SetButtonsPosition()
        {
            var buttons = Buttons.ToArray();
            var delta = height - DefaultSize.y;

            for (var i = 0; i < buttons.Length; i += 1)
            {
                var x = Padding + (i % 2) * ModeButtonSize;
                var y = Padding + (i / 2) * ModeButtonSize + (OpenSide == OpenSide.Down ? delta : 0f);
                buttons[i].relativePosition = new Vector2(x, y);
            }
        }
        private void OnAnimate(float val)
        {
            height = val;
            SetPosition();
            SetButtonsPosition();
        }
    }
    public enum OpenSide
    {
        Down,
        Up,
    }
    public enum OpenState
    {
        Close,
        Open,
        Closing,
        Opening,
    }
}
