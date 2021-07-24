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
        public static NetworkMultitoolShortcut Enter { get; } = new NetworkMultitoolShortcut(nameof(Enter), string.Empty, SavedInputKey.Encode(KeyCode.Return, false, false, false), () => SingletonTool<NetworkMultitoolTool>.Instance.PressEnter(), ToolModeType.Line);

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
        private static IEnumerable<Shortcut> ToolShortcutsWithModes
        {
            get
            {
                yield return Enter;

                foreach (var shortcut in ToolShortcuts)
                    yield return shortcut;
                foreach (var shortcut in ModeShortcuts.Values)
                    yield return shortcut;
            }
        }
        public override IEnumerable<Shortcut> Shortcuts => ToolShortcutsWithModes;

        public override Shortcut Activation => ActivationShortcut;

        protected override bool ShowToolTip => true;

        protected override IToolMode DefaultMode => ToolModes[ToolModeType.AddNode];

        protected override IEnumerable<IToolMode<ToolModeType>> GetModes()
        {
            yield return CreateToolMode<AddNodeMode>();
            yield return CreateToolMode<RemoveNodeMode>();
            yield return CreateToolMode<IntersectSegmentMode>();
            yield return CreateToolMode<SlopeNodeMode>();
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
        public bool InsertNode(NetTool.ControlPoint controlPoint, out ushort newId)
        {
            if (NetTool.CreateNode(controlPoint.m_segment.GetSegment().Info, controlPoint, controlPoint, controlPoint, NetTool.m_nodePositionsSimulation, 0, false, false, true, false, false, false, 0, out newId, out _, out _, out _) != ToolErrors.None)
            {
                newId = 0;
                return false;
            }
            else
            {
                ref var node = ref newId.GetNode();
                node.m_flags |= NetNode.Flags.Middle;
                node.m_flags &= ~NetNode.Flags.Moveable;
                return true;
            }
        }
        public bool RemoveNode(ushort nodeId)
        {
            var node = nodeId.GetNode();
            var segmentIds = node.SegmentIds().ToArray();

            if (segmentIds.Length != 2)
                return false;

            var info = node.Info;
            var nodeIds = new ushort[2];
            var directions = new Vector3[2];
            var invert = true;
            for (var i = 0; i < 2; i += 1)
            {
                var segment = segmentIds[i].GetSegment();
                nodeIds[i] = segment.GetOtherNode(nodeId);
                directions[i] = segment.IsStartNode(nodeId) ? segment.m_endDirection : segment.m_startDirection;
                invert &= segment.m_flags.IsSet(NetSegment.Flags.Invert);
                Singleton<NetManager>.instance.ReleaseSegment(segmentIds[i], true);
            }

            Singleton<NetManager>.instance.ReleaseNode(nodeId);

            return CreateSegment(out _, info, nodeIds[0], nodeIds[1], directions[0], directions[1], invert);
        }
        public bool IntersectSegments(ushort firstId, ushort secondId)
        {
            if (firstId == 0 || secondId == 0 || firstId == secondId)
                return false;

            var firstSegment = firstId.GetSegment();
            var secondSegment = secondId.GetSegment();

            if (!firstSegment.m_flags.CheckFlags(NetSegment.Flags.Created, NetSegment.Flags.Deleted) || !secondSegment.m_flags.CheckFlags(NetSegment.Flags.Created, NetSegment.Flags.Deleted))
                return false;

            var firstTrajectory = new BezierTrajectory(ref firstSegment);
            var secondTrajectory = new BezierTrajectory(ref secondSegment);

            if (!Intersection.CalculateSingle(firstTrajectory, secondTrajectory, out var firstT, out var secondT))
                return false;

            var firstPos = firstTrajectory.Position(firstT);
            var firstDir = firstTrajectory.Tangent(firstT).normalized;

            var secondPos = secondTrajectory.Position(secondT);
            var secondDir = secondTrajectory.Tangent(secondT).normalized;

            var pos = (firstPos + secondPos) / 2f;

            Singleton<NetManager>.instance.ReleaseSegment(firstId, true);
            Singleton<NetManager>.instance.ReleaseSegment(secondId, true);

            if (!CreateNode(out var newNodeId, firstSegment.Info, pos))
                return false;

            var isFirstInvert = firstSegment.m_flags.IsSet(NetSegment.Flags.Invert);
            var isSecondInvert = secondSegment.m_flags.IsSet(NetSegment.Flags.Invert);

            CreateSegment(out _, firstSegment.Info, firstSegment.m_startNode, newNodeId, firstSegment.m_startDirection, -firstDir, isFirstInvert);
            CreateSegment(out _, firstSegment.Info, newNodeId, firstSegment.m_endNode, firstDir, firstSegment.m_endDirection, isFirstInvert);

            CreateSegment(out _, secondSegment.Info, secondSegment.m_startNode, newNodeId, secondSegment.m_startDirection, -secondDir, isSecondInvert);
            CreateSegment(out _, secondSegment.Info, newNodeId, secondSegment.m_endNode, secondDir, secondSegment.m_endDirection, isSecondInvert);

            return true;
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

                var commonSegmentId = (ushort)0;
                foreach (var segmentId in firstId.GetNode().SegmentIds())
                {
                    if (segmentId.GetSegment().NodeIds().Any(n => n == secondId))
                    {
                        commonSegmentId = segmentId;
                        break;
                    }
                }
                if (commonSegmentId == 0)
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

        private bool CreateNode(out ushort newNodeId, NetInfo info, Vector3 position) => Singleton<NetManager>.instance.CreateNode(out newNodeId, ref Singleton<SimulationManager>.instance.m_randomizer, info, position, Singleton<SimulationManager>.instance.m_currentBuildIndex);
        private bool CreateSegment(out ushort newSegmentId, NetInfo info, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir, bool invert = false) => Singleton<NetManager>.instance.CreateSegment(out newSegmentId, ref Singleton<SimulationManager>.instance.m_randomizer, info, startId, endId, startDir, endDir, Singleton<SimulationManager>.instance.m_currentBuildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, invert);

        private void PressEnter()
        {
            if (Mode is BaseNodeLine mode)
                mode.PressEnter();
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

        [NotItem]
        Line = SlopeNode | 16,

        [NotItem]
        Any = int.MaxValue,
    }
    public abstract class BaseNetworkMultitoolMode : BaseSelectToolMode<NetworkMultitoolTool>, IToolMode<ToolModeType>, ISelectToolMode
    {
        public abstract ToolModeType Type { get; }

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
