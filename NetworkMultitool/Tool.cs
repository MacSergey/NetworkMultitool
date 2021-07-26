using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class NetworkMultitoolTool : BaseTool<Mod, NetworkMultitoolTool, ToolModeType>
    {
        public static NetworkMultitoolShortcut ActivationShortcut { get; } = new NetworkMultitoolShortcut(nameof(ActivationShortcut), nameof(CommonLocalize.Settings_ShortcutActivateTool), SavedInputKey.Encode(KeyCode.T, true, false, false));
        public static NetworkMultitoolShortcut SelectionStepOverShortcut { get; } = new NetworkMultitoolShortcut(nameof(SelectionStepOverShortcut), nameof(CommonLocalize.Settings_ShortcutSelectionStepOver), SavedInputKey.Encode(KeyCode.Space, true, false, false), () => SingletonTool<NetworkMultitoolTool>.Instance.SelectionStepOver());

        public static Dictionary<ToolModeType, NetworkMultitoolShortcut> ModeShortcuts { get; } = InitModeShortcuts();
        private static Dictionary<ToolModeType, NetworkMultitoolShortcut> InitModeShortcuts()
        {
            var dictionary = new Dictionary<ToolModeType, NetworkMultitoolShortcut>();

            foreach (var mode in EnumExtension.GetEnumValues<ToolModeType>(m => m.IsItem()))
            {
                var shortcut = new NetworkMultitoolShortcut(mode.ToString(), string.Empty, SavedInputKey.Encode((KeyCode)((int)KeyCode.Alpha1 + dictionary.Count), true, false, false), () => SingletonTool<NetworkMultitoolTool>.Instance.SetMode(mode));
                dictionary[mode] = shortcut;
            }

            return dictionary;
        }

        public static IEnumerable<Shortcut> ToolShortcuts
        {
            get
            {
                yield return SelectionStepOverShortcut;
            }
        }
        public override IEnumerable<Shortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in ToolShortcuts)
                    yield return shortcut;
                foreach (var shortcut in ModeShortcuts.Values)
                    yield return shortcut;
                if (Mode is BaseNetworkMultitoolMode mode)
                {
                    foreach (var shortcut in mode.Shortcuts)
                        yield return shortcut;
                }
            }
        }

        public override Shortcut Activation => ActivationShortcut;

        protected override bool ShowToolTip => true;

        protected override IToolMode DefaultMode => ToolModes[ToolModeType.AddNode];

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<AddNodeMode>();
            yield return CreateToolMode<RemoveNodeMode>();
            yield return CreateToolMode<IntersectSegmentMode>();
            yield return CreateToolMode<SlopeNodeMode>();
            yield return CreateToolMode<ArrangeLineMode>();
            yield return CreateToolMode<CreateLoopMode>();
        }
        protected override void OnReset()
        {
            base.OnReset();
            Singleton<InfoManager>.instance.SetCurrentMode(InfoManager.InfoMode.None, InfoManager.SubInfoMode.Default);
        }
        protected override bool CheckInfoMode(InfoManager.InfoMode mode, InfoManager.SubInfoMode subInfo) => (mode == InfoManager.InfoMode.None || mode == InfoManager.InfoMode.Underground) && subInfo == InfoManager.SubInfoMode.Default;

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

    public enum ToolModeType
    {
        [NotItem]
        None = 0,

        AddNode = 1,
        RemoveNode = 2,
        IntersectSegment = 4,
        SlopeNode = 8,
        ArrangeAtLine = 16,

        CreateLoop = 32,

        [NotItem]
        Line = SlopeNode | ArrangeAtLine,

        [NotItem]
        Any = int.MaxValue,
    }
    public abstract class BaseNetworkMultitoolMode : BaseSelectToolMode<NetworkMultitoolTool>, IToolMode<ToolModeType>, ISelectToolMode
    {
        public abstract ToolModeType Type { get; }
        public virtual IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield break;
            }
        }

        protected string GetStepOverInfo() => NetworkMultitoolTool.SelectionStepOverShortcut.NotSet ? string.Empty : "\n\n" + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, NetworkMultitoolTool.SelectionStepOverShortcut.InputKey);

        protected override bool CheckSegment(ushort segmentId) => segmentId.GetSegment().m_flags.CheckFlags(0, NetSegment.Flags.Untouchable) && base.CheckSegment(segmentId);

        protected override bool CheckItemClass(ItemClass itemClass) => itemClass.m_layer == ItemClass.Layer.Default || itemClass.m_layer == ItemClass.Layer.MetroTunnels;

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (!Underground && Utility.OnlyShiftIsPressed)
                Underground = true;
            else if (Underground && !Utility.OnlyShiftIsPressed)
                Underground = false;
        }

        protected bool CreateNode(out ushort newNodeId, NetInfo info, Vector3 position) => Singleton<NetManager>.instance.CreateNode(out newNodeId, ref Singleton<SimulationManager>.instance.m_randomizer, info, position, Singleton<SimulationManager>.instance.m_currentBuildIndex);
        protected bool CreateSegment(out ushort newSegmentId, NetInfo info, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir, bool invert = false) => Singleton<NetManager>.instance.CreateSegment(out newSegmentId, ref Singleton<SimulationManager>.instance.m_randomizer, info, startId, endId, startDir, endDir, Singleton<SimulationManager>.instance.m_currentBuildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, invert);

        protected void RemoveNode(ushort nodeId) => Singleton<NetManager>.instance.ReleaseNode(nodeId);
        protected void RemoveSegment(ushort segmentId, bool keepNodes = true) => Singleton<NetManager>.instance.ReleaseSegment(segmentId, keepNodes);
    }
    public interface ISelectToolMode
    {
        public void IgnoreSelected();
    }

    public class NetworkMultitoolShortcut : ToolShortcut<Mod, NetworkMultitoolTool, ToolModeType>
    {
        public NetworkMultitoolShortcut(string name, string labelKey, InputKey key, Action action = null, ToolModeType modeType = ToolModeType.Any) : base(name, labelKey, key, action, modeType) { }
    }
    public class NetworkMultitoolThreadingExtension : BaseThreadingExtension<NetworkMultitoolTool> { }
    public class NetworkMultitoolLoadingExtension : BaseToolLoadingExtension<NetworkMultitoolTool> { }
}
