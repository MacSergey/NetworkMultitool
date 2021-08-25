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
        private UIComponent Parent { get; set; }
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
                return (isVisible && this.IsHover(mouse)) || (Parent.isVisible && Parent.IsHover(mouse));
            }
        }
        private UIComponent Root
        {
            get
            {
                if (Parent is UIComponent root)
                {
                    while (root.parent != null)
                        root = root.parent;
                    return root;
                }
                else
                    return null;
            }
        }
        private OpenState State { get; set; } = OpenState.Close;
        private OpenSide OpenSide { get; set; } = OpenSide.Down;
        public ModesPanel()
        {
            isVisible = false;
            atlas = TextureHelper.InGameAtlas;
            backgroundSprite = "ButtonWhite";
            color = new Color32(64, 64, 64, 255);
            clipChildren = true;
        }
        public static ModesPanel Add(UIComponent parent)
        {
            var view = UIView.GetAView();
            var panel = view.AddUIComponent(typeof(ModesPanel)) as ModesPanel;
            panel.Parent = parent;
            return panel;
        }
        public override void Start()
        {
            base.Start();
            enabled = Parent.isVisible;

            SingletonTool<NetworkMultitoolTool>.Instance.OnStateChanged += ToolStateChanged;

            foreach (var mode in SingletonTool<NetworkMultitoolTool>.Instance.Modes.OfType<BaseNetworkMultitoolMode>())
            {
                if (mode.IsMain)
                    ModeButton.Add(this, mode);
            }
            size = DefaultSize;
            height = 20f;

            Parent.eventMouseEnter += ParentMouseEnter;
            Parent.eventMouseLeave += ParentMouseLeave;
            Parent.eventPositionChanged += ParentPositionChanged;
            Parent.eventVisibilityChanged += ParentVisibilityChanged;

            if (Root is UIComponent root)
            {
                root.eventPositionChanged += ParentPositionChanged;
                root.eventZOrderChanged += RootZOrderChanged;
            }
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
        private void ParentPositionChanged(UIComponent parent, Vector2 value) => SetOpenSide(true);
        private void ParentVisibilityChanged(UIComponent component, bool value) => enabled = value;
        private void RootZOrderChanged(UIComponent component, int value) => SetOrder(component);
        public new void FitChildren()
        {
            size = DefaultSize;

            if (State == OpenState.Close)
                height = 20f;
            else if (State == OpenState.Open)
            {
                SetOpenSide();
                SetPosition();
                SetButtonsPosition();
            }
        }
        public void SetOpenSide(bool forceSetPosition = false)
        {
            var oldSide = OpenSide;

            if (Settings.PanelOpenSide == (int)OpenSide.Down)
                OpenSide = Parent.absolutePosition.y + Parent.height + DefaultSize.y <= Parent.GetUIView().GetScreenResolution().y ? OpenSide.Down : OpenSide.Up;
            else
                OpenSide = Parent.absolutePosition.y >= DefaultSize.y ? OpenSide.Up : OpenSide.Down;

            if (oldSide != OpenSide || forceSetPosition)
                SetPosition();
        }
        private void SetPosition()
        {
            UIView uiView = Parent.GetUIView();
            var screen = uiView.GetScreenResolution();
            var parentPos = Parent.absolutePosition;
            var x = Mathf.Max(Mathf.Min(parentPos.x + (Parent.width - width) / 2f, screen.x - width), 0f);
            var y = parentPos.y + (OpenSide == OpenSide.Down ? Parent.height : -height);
            absolutePosition = new Vector2(x, y);
        }
        private void SetOrder(UIComponent component)
        {
            if (component.zOrder >= zOrder)
                zOrder = component.zOrder + 1;
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
                    if (!auto || (autoHide && !isHover))
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
            if (Root is UIComponent root)
                SetOrder(root);
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
