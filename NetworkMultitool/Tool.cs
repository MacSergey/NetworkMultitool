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
        public static IEnumerable<Shortcut> BindShortcuts
        {
            get
            {
                foreach (var shortcut in ToolShortcuts)
                    yield return shortcut;

                yield return BaseNetworkMultitoolMode.ApplyShortcut;
                yield return CreateConnectionMode.SwitchSelectShortcut;
                yield return CreateLoopMode.SwitchIsLoopShortcut;
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
        protected override bool ShowToolTip => base.ShowToolTip && Settings.ShowToolTip;
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
            AddModeShortcut(ToolModeType.IntersectSegment, KeyCode.Alpha4);
            AddModeShortcut(ToolModeType.InvertSegment, KeyCode.Alpha9);
            AddModeShortcut(ToolModeType.SlopeNode, KeyCode.Alpha5);
            AddModeShortcut(ToolModeType.ArrangeAtLine, KeyCode.Alpha6);
            AddModeShortcut(ToolModeType.CreateLoop, KeyCode.Alpha7);
            AddModeShortcut(ToolModeType.CreateConnection, KeyCode.Alpha8);
        }
        private static void AddModeShortcut(ToolModeType mode, KeyCode key)
        {
            ModeShortcuts[mode] = new NetworkMultitoolShortcut(mode.ToString(), mode.GetAttr<DescriptionAttribute, ToolModeType>().Description, SavedInputKey.Encode(key, true, false, false), () => SingletonTool<NetworkMultitoolTool>.Instance.SetMode(mode));
        }

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<AddNodeMode>();
            yield return CreateToolMode<RemoveNodeMode>();
            yield return CreateToolMode<UnionNodeMode>();
            yield return CreateToolMode<IntersectSegmentMode>();
            yield return CreateToolMode<InvertSegmentMode>();
            yield return CreateToolMode<SlopeNodeMode>();
            yield return CreateToolMode<ArrangeLineMode>();
            yield return CreateToolMode<CreateLoopMode>();
            yield return CreateToolMode<CreateConnectionMode>();
        }
        protected override void OnReset()
        {
            base.OnReset();
            Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
        }
        protected override void InitProcess()
        {
            base.InitProcess();
            AddModePanel();
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
            if (IsInit)
                AddModePanel();
        }
        private void AddModePanel()
        {
            if (UUIRegistered)
                UUIButton.AddUIComponent<ModesPanel>();
        }

        private void SelectionStepOver()
        {
            if (Mode is ISelectToolMode selectMode)
                selectMode.IgnoreSelected();
        }
        public bool SetSlope(ushort[] nodeIds)
        {
            var startY = nodeIds.First().GetNode().m_position.y;
            var endY = nodeIds.Last().GetNode().m_position.y;

            var list = new List<ITrajectory>();

            for (var i = 1; i < nodeIds.Length; i += 1)
            {
                var firstId = nodeIds[i - 1];
                var secondId = nodeIds[i];

                if (!NetExtension.GetCommon(firstId, secondId, out var commonSegmentId))
                    return false;
                else
                {
                    var segment = commonSegmentId.GetSegment();

                    var startPos = segment.m_startNode.GetNode().m_position;
                    var endPos = segment.m_endNode.GetNode().m_position;
                    var startDir = segment.m_startDirection.MakeFlatNormalized();
                    var endDir = segment.m_endDirection.MakeFlatNormalized();

                    startPos.y = 0;
                    endPos.y = 0;

                    list.Add(new BezierTrajectory(startPos, startDir, endPos, endDir));
                }
            }

            var sumLenght = list.Sum(t => t.Length);
            var currentLenght = 0f;
            for (var i = 1; i < nodeIds.Length - 1; i += 1)
            {
                currentLenght += list[i - 1].Length;
                var position = nodeIds[i].GetNode().m_position;
                position.y = Mathf.Lerp(startY, endY, currentLenght / sumLenght);
                NetManager.instance.MoveNode(nodeIds[i], position);
            }
            return true;
        }
    }

    public class NetworkMultitoolShortcut : ToolShortcut<Mod, NetworkMultitoolTool, ToolModeType>
    {
        public NetworkMultitoolShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.Any) : base(name, labelKey, key, action, modeType) { }
    }
    public class NetworkMultitoolThreadingExtension : BaseUUIThreadingExtension<NetworkMultitoolTool> { }
    public class NetworkMultitoolLoadingExtension : BaseToolLoadingExtension<NetworkMultitoolTool> { }
}
