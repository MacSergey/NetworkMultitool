using ColossalFramework;
using ColossalFramework.Math;
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
using static ColossalFramework.Math.VectorUtils;

namespace NetworkMultitool
{
    public abstract class BaseNetworkMultitoolMode : BaseSelectToolMode<NetworkMultitoolTool>, IToolMode<ToolModeType>, ISelectToolMode
    {
        public static NetworkMultitoolShortcut ApplyShortcut { get; } = GetShortcut(KeyCode.Return, nameof(ApplyShortcut), nameof(Localize.Settings_Shortcut_Apply), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as BaseNetworkMultitoolMode)?.Apply());

        protected static NetworkMultitoolShortcut GetShortcut(KeyCode keyCode, Action action, ToolModeType mode = ToolModeType.Any, bool ctrl = false, bool shift = false, bool alt = false, bool repeat = false, bool ignoreModifiers = false) => GetShortcut(keyCode, string.Empty, string.Empty, action, mode, ctrl, shift, alt, repeat, ignoreModifiers);
        protected static NetworkMultitoolShortcut GetShortcut(KeyCode keyCode, string name, string labelKey, Action action, ToolModeType mode = ToolModeType.Any, bool ctrl = false, bool shift = false, bool alt = false, bool repeat = false, bool ignoreModifiers = false) => new NetworkMultitoolShortcut(name, labelKey, SavedInputKey.Encode(keyCode, ctrl, shift, alt), action, mode) { CanRepeat = repeat, IgnoreModifiers = ignoreModifiers };

        public abstract ToolModeType Type { get; }
        public string Title => SingletonMod<Mod>.Instance.GetLocalizeString(Type.GetAttr<DescriptionAttribute, ToolModeType>().Description);
        protected abstract bool IsReseted { get; }
        protected virtual bool CanSwitchUnderground => true;
        private bool ForbiddenSwitchUnderground { get; set; }

        private List<ModeButton> Buttons { get; } = new List<ModeButton>();
        public NetworkMultitoolShortcut ActivationShortcut => NetworkMultitoolTool.ModeShortcuts[Type];
        public virtual IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield break;
            }
        }
        protected List<InfoLabel> Labels { get; } = new List<InfoLabel>();
        protected static string GetRadiusString(float radius, string format = "0.0") => string.Format(Localize.Mode_RadiusFormat, radius.ToString(format));
        protected static string GetAngleString(float angle, string format = "0") => string.Format(Localize.Mode_AngleFormat, (angle * Mathf.Rad2Deg).ToString(format));
        protected static string GetPercentagesString(float percent, string format = "0.0") => string.Format(Localize.Mode_PercentagesFormat, percent.ToString(format));

        public override void Activate(IToolMode prevMode)
        {
            base.Activate(prevMode);
            ForbiddenSwitchUnderground = false;
            foreach (var button in Buttons)
                button.Activate = true;
        }
        public override void Deactivate()
        {
            base.Deactivate();
            foreach (var button in Buttons)
                button.Activate = false;
            ClearLabels();
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            ClearLabels();
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (!CanSwitchUnderground)
                ForbiddenSwitchUnderground = Utility.ShiftIsPressed && !Underground;
            else if (ForbiddenSwitchUnderground)
                ForbiddenSwitchUnderground = Utility.ShiftIsPressed;
            else if (!Underground && Utility.OnlyShiftIsPressed)
                Underground = true;
            else if (Underground && !Utility.OnlyShiftIsPressed)
                Underground = false;
        }
        protected virtual void Apply() { }
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
        public void AddButton(UIComponent parent)
        {
            var button = parent.AddUIComponent<ModeButton>();
            button.Init(this, Type.ToString());
            button.eventClicked += ButtonClicked;
            Buttons.Add(button);
        }
        private void ButtonClicked(UIComponent component, UIMouseEventParameter eventParam) => Tool.SetMode(this);

        public sealed override string GetToolInfo()
        {
            var info = GetInfo();
            if (string.IsNullOrEmpty(info))
                return string.Empty;
            else
                return $"{Title.ToUpper()}\n\n{info}";
        }
        protected virtual string GetInfo() => string.Empty;
        protected string StepOverInfo => NetworkMultitoolTool.SelectionStepOverShortcut.NotSet ? string.Empty : "\n\n" + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, NetworkMultitoolTool.SelectionStepOverShortcut.InputKey);
        protected string UndergroundInfo => $"\n{Localize.Mode_Info_UndergroundMode}";

        protected override bool CheckSegment(ushort segmentId) => segmentId.GetSegment().m_flags.CheckFlags(0, NetSegment.Flags.Untouchable) && base.CheckSegment(segmentId);

        protected override bool CheckItemClass(ItemClass itemClass) => itemClass.m_layer == ItemClass.Layer.Default || itemClass.m_layer == ItemClass.Layer.MetroTunnels;


        protected bool CreateNode(out ushort newNodeId, NetInfo info, Vector3 position) => Singleton<NetManager>.instance.CreateNode(out newNodeId, ref Singleton<SimulationManager>.instance.m_randomizer, info, position, Singleton<SimulationManager>.instance.m_currentBuildIndex);
        protected bool CreateSegment(out ushort newSegmentId, NetInfo info, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir, bool invert = false) => Singleton<NetManager>.instance.CreateSegment(out newSegmentId, ref Singleton<SimulationManager>.instance.m_randomizer, info, startId, endId, startDir, endDir, Singleton<SimulationManager>.instance.m_currentBuildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, invert);

        protected void RemoveNode(ushort nodeId) => Singleton<NetManager>.instance.ReleaseNode(nodeId);
        protected void RemoveSegment(ushort segmentId, bool keepNodes = true) => Singleton<NetManager>.instance.ReleaseSegment(segmentId, keepNodes);
        protected void RelinkSegment(ushort segmentId, ushort sourceNodeId, ushort targetNodeId)
        {
            var segment = segmentId.GetSegment();
            var otherNodeId = segment.GetOtherNode(sourceNodeId);
            var info = segment.Info;
            var otherDir = segment.IsStartNode(sourceNodeId) ? segment.m_endDirection : segment.m_startDirection;
            var sourceDir = segment.IsStartNode(sourceNodeId) ? segment.m_startDirection : segment.m_endDirection;
            var invert = segment.IsStartNode(sourceNodeId) ^ segment.IsInvert();

            var otherNode = otherNodeId.GetNode();

            var sourceNode = sourceNodeId.GetNode();
            var targetNode = targetNodeId.GetNode();
            var oldDir = new StraightTrajectory(otherNode.m_position.MakeFlat(), sourceNode.m_position.MakeFlat());
            var newDir = new StraightTrajectory(otherNode.m_position.MakeFlat(), targetNode.m_position.MakeFlat());
            var angle = MathExtention.GetAngle(oldDir.Direction, newDir.Direction);

            otherDir = otherDir.TurnRad(angle, false);
            sourceDir = sourceDir.TurnRad(angle, false);

            RemoveSegment(segmentId);
            CreateSegment(out _, info, otherNodeId, targetNodeId, otherDir, sourceDir, invert);
        }
        protected Rect GetTerrainRect(params ushort[] segmentIds) => segmentIds.Select(i => (ITrajectory)new BezierTrajectory(i)).GetRect();
        protected void UpdateTerrain(params ushort[] segmentIds)
        {
            if (segmentIds.Length != 0)
                UpdateTerrain(GetTerrainRect(segmentIds));
        }
        protected void UpdateTerrain(Rect rect) => TerrainModify.UpdateArea(rect.xMin, rect.yMin, rect.xMax, rect.yMax, true, true, false);

        protected InfoLabel AddLabel()
        {
            var view = UIView.GetAView();
            var label = view.AddUIComponent(typeof(InfoLabel)) as InfoLabel;
            Labels.Add(label);
            return label;
        }
        protected void RemoveLabel(InfoLabel label)
        {
            Labels.Remove(label);
            Destroy(label.gameObject);
        }
        private void ClearLabels()
        {
            foreach (var label in Labels)
                Destroy(label.gameObject);

            Labels.Clear();
        }

        protected void RenderSegmentNodes(RenderManager.CameraInfo cameraInfo, Func<ushort, bool> isAllow = null)
        {
            if (IsHoverSegment)
            {
                var data = new OverlayData(cameraInfo) { Color = Colors.Blue, RenderLimit = Underground };

                var segment = HoverSegment.Id.GetSegment();
                if (!Underground ^ segment.m_startNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground) && isAllow?.Invoke(segment.m_startNode) != false)
                    new NodeSelection(segment.m_startNode).Render(data);

                if (!Underground ^ segment.m_endNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground) && isAllow?.Invoke(segment.m_endNode) != false)
                    new NodeSelection(segment.m_endNode).Render(data);
            }
        }
        protected void RenderNearNodes(RenderManager.CameraInfo cameraInfo, Vector3? position = null, float radius = 300f, Func<ushort, bool> isAllow = null)
        {
            position ??= Tool.MouseWorldPosition;
            isAllow ??= AllowRenderNear;

            var minX = Min(position.Value.x - radius);
            var minZ = Min(position.Value.z - radius);
            var maxX = Max(position.Value.x + radius);
            var maxZ = Max(position.Value.z + radius);
            var xzPosition = XZ(position.Value);

            for (int i = minZ; i <= maxZ; i++)
            {
                for (int j = minX; j <= maxX; j++)
                {
                    var nodeId = NetManager.instance.m_nodeGrid[i * 270 + j];
                    int count = 0;

                    while (nodeId != 0u && count < NetManager.MAX_SEGMENT_COUNT)
                    {
                        ref var node = ref nodeId.GetNode();
                        var magnitude = (XZ(node.m_position) - xzPosition).magnitude;
                        if (!Underground ^ node.m_flags.IsSet(NetNode.Flags.Underground) && magnitude <= radius && isAllow?.Invoke(nodeId) != false)
                        {
                            var color = Colors.Blue;
                            color.a = (byte)((1 - magnitude / radius) * 255f);
                            node.m_position.RenderCircle(new OverlayData(cameraInfo) { Width = Mathf.Min(8f, node.Info.m_halfWidth * 2f), Color = color, RenderLimit = Underground });
                        }

                        nodeId = node.m_nextGridNode;
                    }
                }
            }


            static int Min(float value) => Mathf.Max((int)((value - 16f) / 64f + 135f) - 1, 0);
            static int Max(float value) => Mathf.Min((int)((value + 16f) / 64f + 135f) + 1, 269);
        }
        protected virtual bool AllowRenderNear(ushort nodeId)
        {
            if (IsHoverNode)
                return nodeId != HoverNode.Id;
            else if (IsHoverSegment)
                return !HoverSegment.Id.GetSegment().Contains(nodeId);
            else
                return true;
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

        [Description(nameof(Localize.Mode_SplitNode))]
        SplitNode = UnionNode << 1,


        [Description(nameof(Localize.Mode_IntersectSegment))]
        IntersectSegment = SplitNode << 1,

        [Description(nameof(Localize.Mode_InvertSegment))]
        InvertSegment = IntersectSegment << 1,

        [Description(nameof(Localize.Mode_SlopeNode))]
        SlopeNode = InvertSegment << 1,

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
    public class InfoLabel : CustomUILabel
    {
        public Vector3 WorldPosition { get; set; }
        public Vector3 Direction { get; set; }

        public InfoLabel()
        {
            isVisible = false;
            color = Colors.White;
            textScale = 2f;
        }

        public override void Update()
        {
            base.Update();

            var uIView = GetUIView();
            var startScreenPosition = Camera.main.WorldToScreenPoint(WorldPosition);
            var endScreenPosition = Camera.main.WorldToScreenPoint(WorldPosition + Direction);
            var screenDir = ((Vector2)(endScreenPosition - startScreenPosition)).normalized;
            screenDir.y *= -1;
            var relativePosition = uIView.ScreenPointToGUI(startScreenPosition / uIView.inputScale) - size * 0.5f + screenDir * (size.magnitude * 0.5f);

            this.relativePosition = relativePosition;
        }
    }
}
