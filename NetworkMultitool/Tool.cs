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

            foreach(var mode in EnumExtension.GetEnumValues<ToolModeType>(m => m.IsItem()))
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
                foreach(var shortcut in ToolShortcuts)
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
            for (var i = 0; i < 2; i += 1)
            {
                var segment = segmentIds[i].GetSegment();
                nodeIds[i] = segment.GetOtherNode(nodeId);
                directions[i] = segment.IsStartNode(nodeId) ? segment.m_endDirection : segment.m_startDirection;
                Singleton<NetManager>.instance.ReleaseSegment(segmentIds[i], true);
            }

            Singleton<NetManager>.instance.ReleaseNode(nodeId);

            Singleton<NetManager>.instance.CreateSegment(out _, ref Singleton<SimulationManager>.instance.m_randomizer, info, nodeIds[0], nodeIds[1], directions[0], directions[1], Singleton<SimulationManager>.instance.m_currentBuildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, false);

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
