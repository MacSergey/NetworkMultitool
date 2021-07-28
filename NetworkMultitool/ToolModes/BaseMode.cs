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
    public abstract class BaseNetworkMultitoolMode : BaseSelectToolMode<NetworkMultitoolTool>, IToolMode<ToolModeType>, ISelectToolMode
    {
        public abstract ToolModeType Type { get; }
        public string Title => SingletonMod<Mod>.Instance.GetLocalizeString(Type.GetAttr<DescriptionAttribute, ToolModeType>().Description);
        protected abstract bool IsReseted { get; }

        private ModeButton Button { get; }
        public NetworkMultitoolShortcut ActivationShortcut => NetworkMultitoolTool.ModeShortcuts[Type];
        public virtual IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield break;
            }
        }


        public BaseNetworkMultitoolMode()
        {
            Button = new GameObject(Type.ToString()).AddComponent<ModeButton>();
            Button.Init(this, Type.ToString());
            Button.eventClicked += ButtonClicked;
        }

        public override void Activate(IToolMode prevMode)
        {
            base.Activate(prevMode);
            Button.Activate = true;
        }
        public override void Deactivate()
        {
            base.Deactivate();
            Button.Activate = false;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (!Underground && Utility.OnlyShiftIsPressed)
                Underground = true;
            else if (Underground && !Utility.OnlyShiftIsPressed)
                Underground = false;
        }
        public override bool OnEscape()
        {
            if (!IsReseted)
            {
                Reset(this);
                return true;
            }
            else
                return false;
        }
        public void AttachButton(UIComponent parent)
        {
            if (Button.parent != null)
            {
                Button.parent.RemoveUIComponent(Button);
                Button.transform.parent = null;
            }

            parent.AttachUIComponent(Button.gameObject);
            Button.transform.parent = parent.cachedTransform;
        }
        private void ButtonClicked(UIComponent component, UIMouseEventParameter eventParam) => Tool.SetMode(this);

        protected NetworkMultitoolShortcut GetShortcut(KeyCode keyCode, Action action, ToolModeType mode = ToolModeType.Any, bool ctrl = false, bool shift = false, bool alt = false, bool repeat = false) => new NetworkMultitoolShortcut(string.Empty, string.Empty, SavedInputKey.Encode(keyCode, ctrl, shift, alt), action, mode) { CanRepeat = repeat };

        public sealed override string GetToolInfo()
        {
            var info = GetInfo();
            if (string.IsNullOrEmpty(info))
                return string.Empty;
            else
                return $"{Title.ToUpper()}\n\n{info}";
        }
        protected virtual string GetInfo() => string.Empty;
        protected string GetStepOverInfo() => NetworkMultitoolTool.SelectionStepOverShortcut.NotSet ? string.Empty : "\n\n" + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, NetworkMultitoolTool.SelectionStepOverShortcut.InputKey);

        protected override bool CheckSegment(ushort segmentId) => segmentId.GetSegment().m_flags.CheckFlags(0, NetSegment.Flags.Untouchable) && base.CheckSegment(segmentId);

        protected override bool CheckItemClass(ItemClass itemClass) => itemClass.m_layer == ItemClass.Layer.Default || itemClass.m_layer == ItemClass.Layer.MetroTunnels;


        protected bool CreateNode(out ushort newNodeId, NetInfo info, Vector3 position) => Singleton<NetManager>.instance.CreateNode(out newNodeId, ref Singleton<SimulationManager>.instance.m_randomizer, info, position, Singleton<SimulationManager>.instance.m_currentBuildIndex);
        protected bool CreateSegment(out ushort newSegmentId, NetInfo info, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir, bool invert = false) => Singleton<NetManager>.instance.CreateSegment(out newSegmentId, ref Singleton<SimulationManager>.instance.m_randomizer, info, startId, endId, startDir, endDir, Singleton<SimulationManager>.instance.m_currentBuildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, invert);

        protected void RemoveNode(ushort nodeId) => Singleton<NetManager>.instance.ReleaseNode(nodeId);
        protected void RemoveSegment(ushort segmentId, bool keepNodes = true) => Singleton<NetManager>.instance.ReleaseSegment(segmentId, keepNodes);

        protected void RenderSegmentNodes(RenderManager.CameraInfo cameraInfo, Func<ushort, bool> action = null)
        {
            if (IsHoverSegment)
            {
                var data = new OverlayData(cameraInfo) { Color = Colors.Blue, RenderLimit = Underground };

                var segment = HoverSegment.Id.GetSegment();
                if (action?.Invoke(segment.m_startNode) == true && !Underground ^ segment.m_startNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground))
                    new NodeSelection(segment.m_startNode).Render(data);

                if (action?.Invoke(segment.m_endNode) == true && !Underground ^ segment.m_endNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground))
                    new NodeSelection(segment.m_endNode).Render(data);
            }
        }
    }
    public enum ToolModeType
    {
        [NotItem]
        None = 0,

        [Description(nameof(Localize.Mode_AddNode))]
        AddNode = 1,

        [Description(nameof(Localize.Mode_RemoveNode))]
        RemoveNode = AddNode << 1,

        [Description(nameof(Localize.Mode_UnionNode))]
        UnionNode = RemoveNode << 1,


        [Description(nameof(Localize.Mode_IntersectSegment))]
        IntersectSegment = UnionNode << 1,

        [Description(nameof(Localize.Mode_SlopeNode))]
        SlopeNode = IntersectSegment << 1,

        [Description(nameof(Localize.Mode_ArrangeAtLine))]
        ArrangeAtLine = SlopeNode << 1,


        [Description(nameof(Localize.Mode_CreateLoop))]
        CreateLoop = ArrangeAtLine << 1,

        [Description(nameof(Localize.Mode_CreateConnection))]
        CreateConnection = CreateLoop << 1,

        [NotItem]
        Line = SlopeNode | ArrangeAtLine,

        [NotItem]
        Create = CreateLoop | CreateConnection,

        [NotItem]
        Any = int.MaxValue,
    }

    public interface ISelectToolMode
    {
        public void IgnoreSelected();
    }
}
