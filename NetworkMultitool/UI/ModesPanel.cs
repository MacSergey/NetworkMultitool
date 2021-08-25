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
        public static int InRow => Settings.PanelColumns;
        private IEnumerable<ModeButton> Buttons => components.OfType<ModeButton>();
        private string AnimationId => $"{nameof(ModesPanel)}{GetHashCode()}";
        public Vector2 DefaultSize
        {
            get
            {
                var count = Buttons.Count();
                return new Vector2(2 * Padding + Mathf.Min(count, InRow) * ModeButtonSize, 2 * Padding + (count / InRow + Math.Min(count % InRow, 1)) * ModeButtonSize);
            }
        }
        public bool IsHover
        {
            get
            {
                var uiView = UIView.GetAView();
                var mouse = uiView.ScreenPointToGUI(Input.mousePosition / uiView.inputScale);
                return (isVisible && this.IsHover(mouse)) || (parent.isVisible && parent.IsHover(mouse));
            }
        }
        private OpenState State { get; set; } = OpenState.Close;
        private OpenSide OpenSide { get; set; } = OpenSide.Down;
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

            SingletonTool<NetworkMultitoolTool>.Instance.OnStateChanged += ToolStateChanged;

            foreach (var mode in SingletonTool<NetworkMultitoolTool>.Instance.Modes.OfType<BaseNetworkMultitoolMode>())
            {
                if (mode.IsMain)
                    ModeButton.Add(this, mode);
            }

            parent.eventMouseEnter += ParentMouseEnter;
            parent.eventMouseLeave += ParentMouseLeave;

            var root = parent;
            while (root.parent != null)
                root = root.parent;

            root.eventPositionChanged += ParentPositionChanged;
        }
        public override void OnDestroy()
        {
            base.OnDestroy();

            SingletonTool<NetworkMultitoolTool>.Instance.OnStateChanged -= ToolStateChanged;
            ValueAnimator.Cancel(AnimationId);
        }

        private void ToolStateChanged(bool state) => SetState(state);

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
        private void ParentPositionChanged(UIComponent parent, Vector2 value) => SetOpenSide(true);
        public new void FitChildren()
        {
            size = DefaultSize;
            SetOpenSide();
            SetPosition();
            SetButtonsPosition();
        }
        public void SetOpenSide(bool forceSetPosition = false)
        {
            var oldSide = OpenSide;

            if (Settings.PanelOpenSide == (int)OpenSide.Down)
                OpenSide = parent.absolutePosition.y + parent.height + DefaultSize.y <= parent.GetUIView().GetScreenResolution().y ? OpenSide.Down : OpenSide.Up;
            else
                OpenSide = parent.absolutePosition.y >= DefaultSize.y ? OpenSide.Up : OpenSide.Down;

            if (oldSide != OpenSide || forceSetPosition)
                SetPosition();
        }
        private void SetPosition()
        {
            UIView uiView = parent.GetUIView();
            var screen = uiView.GetScreenResolution();
            var parentPos = parent.absolutePosition;
            var x = Mathf.Max(Mathf.Min(parentPos.x + (parent.width - width) / 2f, screen.x - width), 0f);
            var y = parentPos.y + (OpenSide == OpenSide.Down ? parent.height : -height);
            absolutePosition = new Vector2(x, y);
        }

        public void SetState(bool show, bool auto = false)
        {
            var autoHide = Settings.AutoHideModePanel.value;
            var enabled = SingletonTool<NetworkMultitoolTool>.Instance.enabled;
            var isHover = IsHover;
            if (show)
            {
                if (State != OpenState.Open && State != OpenState.Opening)
                {
                    if (enabled && ((!auto && (!autoHide || isHover)) || (auto && autoHide)))
                    {
                        StartOpening();
                        SetOpenSide();
                        var time = 0.2f * (1 - Mathf.Max(height - 20f, 0f) / DefaultSize.y);
                        ValueAnimator.Cancel(AnimationId);
                        ValueAnimator.Animate(AnimationId, OnAnimate, new AnimatedFloat(height, DefaultSize.y, time, EasingType.CubicEaseOut), EndOpening);
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
                        var time = 0.2f * (height / DefaultSize.y);
                        ValueAnimator.Cancel(AnimationId);
                        ValueAnimator.Animate(AnimationId, OnAnimate, new AnimatedFloat(height, 20f, time, EasingType.CubicEaseIn), EndClosing);
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
                var x = Padding + (i % InRow) * ModeButtonSize;
                var y = Padding + (i / InRow) * ModeButtonSize + (OpenSide == OpenSide.Down ? delta : 0f);
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
