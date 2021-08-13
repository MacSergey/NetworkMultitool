using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NetworkMultitool.UI;
using NetworkMultitool.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class NetworkMultitoolTool : BaseTool<Mod, NetworkMultitoolTool, ToolModeType>
    {
        public static NetworkMultitoolShortcut ActivationShortcut { get; } = new NetworkMultitoolShortcut(nameof(ActivationShortcut), nameof(CommonLocalize.Settings_ShortcutActivateTool), SavedInputKey.Encode(KeyCode.T, true, false, false));
        public static NetworkMultitoolShortcut SelectionStepOverShortcut { get; } = new NetworkMultitoolShortcut(nameof(SelectionStepOverShortcut), nameof(CommonLocalize.Settings_ShortcutSelectionStepOver), SavedInputKey.Encode(KeyCode.Space, true, false, false), () => SingletonTool<NetworkMultitoolTool>.Instance.SelectionStepOver());

        public static IEnumerable<ToolModeType> ModeTypes => EnumExtension.GetEnumValues<ToolModeType>(m => m.IsItem());
        public static Dictionary<ToolModeType, NetworkMultitoolShortcut> ModeShortcuts { get; }

        private static IEnumerable<Shortcut> ToolShortcuts
        {
            get
            {
                yield return SelectionStepOverShortcut;
                foreach (var shortcut in ModeShortcuts.Values)
                    yield return shortcut;
            }
        }
        public override IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in ToolShortcuts)
                    yield return shortcut;
                if (Mode is BaseNetworkMultitoolMode mode)
                {
                    foreach (var shortcut in mode.Shortcuts)
                        yield return shortcut;
                }
            }
        }

        public override Shortcut Activation => ActivationShortcut;
        public override bool MouseRayValid => !UIView.HasModalInput() && (UIInput.hoveredComponent?.isInteractive != true || UIInput.hoveredComponent is InfoLabel) && Cursor.visible;
        protected override bool ShowToolTip => (base.ShowToolTip || UIInput.hoveredComponent is InfoLabel) && Settings.ShowToolTip;
        private IToolMode LastMode { get; set; }
        protected override IToolMode DefaultMode => LastMode ?? ToolModes[ToolModeType.AddNode];

        protected override UITextureAtlas UUIAtlas => NetworkMultitoolTextures.Atlas;
        protected override string UUINormalSprite => NetworkMultitoolTextures.UUINormal;
        protected override string UUIHoveredSprite => NetworkMultitoolTextures.UUIHovered;
        protected override string UUIPressedSprite => NetworkMultitoolTextures.UUIPressed;
        protected override string UUIDisabledSprite => /*NodeControllerTextures.UUIDisabled;*/string.Empty;

        static NetworkMultitoolTool()
        {
            ModeShortcuts = new Dictionary<ToolModeType, NetworkMultitoolShortcut>();

            AddModeShortcut(ToolModeType.AddNode, KeyCode.Alpha1);
            AddModeShortcut(ToolModeType.RemoveNode, KeyCode.Alpha2);
            AddModeShortcut(ToolModeType.UnionNode, KeyCode.Alpha3);
            AddModeShortcut(ToolModeType.SplitNode, KeyCode.Alpha0);
            AddModeShortcut(ToolModeType.IntersectSegment, KeyCode.Alpha4);
            AddModeShortcut(ToolModeType.InvertSegment, KeyCode.Alpha9);
            AddModeShortcut(ToolModeType.SlopeNode, KeyCode.Alpha5);
            AddModeShortcut(ToolModeType.ArrangeAtLine, KeyCode.Alpha6);
            AddModeShortcut(ToolModeType.ArrangeAtCircle, KeyCode.Alpha4, false, false, true);
            AddModeShortcut(ToolModeType.CreateLoop, KeyCode.Alpha7);
            AddModeShortcut(ToolModeType.CreateConnection, KeyCode.Alpha8);
            AddModeShortcut(ToolModeType.CreateParallel, KeyCode.Alpha2, false, false, true);
            //AddModeShortcut(ToolModeType.CreateBezier, KeyCode.Alpha3, false, false, true);
            AddModeShortcut(ToolModeType.UnlockSegment, KeyCode.Alpha1, false, false, true);
        }
        private static void AddModeShortcut(ToolModeType mode, KeyCode key, bool ctrl = true, bool shift = false, bool alt = false)
        {
            ModeShortcuts[mode] = new NetworkMultitoolShortcut(mode.ToString(), mode.GetAttr<DescriptionAttribute, ToolModeType>().Description, SavedInputKey.Encode(key, ctrl, shift, alt), () => SingletonTool<NetworkMultitoolTool>.Instance.SetMode(mode));
        }

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<AddNodeMode>();
            yield return CreateToolMode<RemoveNodeMode>();
            yield return CreateToolMode<UnionNodeMode>();
            yield return CreateToolMode<SplitNodeMode>();
            yield return CreateToolMode<IntersectSegmentMode>();
            yield return CreateToolMode<InvertSegmentMode>();
            yield return CreateToolMode<SlopeNodeMode>();
            yield return CreateToolMode<ArrangeLineMode>();
            yield return CreateToolMode<ArrangeCircleMode>();
            yield return CreateToolMode<ArrangeCircleCompleteMode>();
            yield return CreateToolMode<ArrangeCircleMoveCenterMode>();
            yield return CreateToolMode<ArrangeCircleRadiusMode>();
            yield return CreateToolMode<ArrangeCircleMoveNodeMode>();
            yield return CreateToolMode<CreateLoopMode>();
            yield return CreateToolMode<CreateLoopMoveCircleMode>();
            yield return CreateToolMode<CreateConnectionMode>();
            yield return CreateToolMode<CreateConnectionMoveCircleMode>();
            yield return CreateToolMode<CreateConnectionChangeRadiusMode>();
            yield return CreateToolMode<CreateParallelMode>();
            //yield return CreateToolMode<CreateBezierMode>();
            yield return CreateToolMode<UnlockSegmentMode>();
        }
        protected override void OnReset()
        {
            base.OnReset();
            Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
        }
        protected override void SetModeNow(IToolMode mode)
        {
            base.SetModeNow(mode);

            if (mode != null)
                LastMode = Mode;
        }
        protected override bool CheckInfoMode(InfoManager.InfoMode mode, InfoManager.SubInfoMode subInfo) => (mode == InfoManager.InfoMode.None || mode == InfoManager.InfoMode.Underground) && subInfo == InfoManager.SubInfoMode.Default;

        public override void RegisterUUI()
        {
            base.RegisterUUI();
            if (IsInit && UUIRegistered)
                UUIButton.AddUIComponent<ModesPanel>();
        }

        private void SelectionStepOver()
        {
            if (Mode is ISelectToolMode selectMode)
                selectMode.IgnoreSelected();
        }
    }

    public class NetworkMultitoolShortcut : ToolShortcut<Mod, NetworkMultitoolTool, ToolModeType>
    {
        public NetworkMultitoolShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.Any) : base(name, labelKey, key, action, modeType) { }
    }
    public class NetworkMultitoolThreadingExtension : BaseUUIThreadingExtension<NetworkMultitoolTool> { }
    public class NetworkMultitoolLoadingExtension : BaseUUIToolLoadingExtension<NetworkMultitoolTool> { }
}
