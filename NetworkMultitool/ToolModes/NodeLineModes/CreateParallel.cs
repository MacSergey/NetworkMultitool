using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using NetworkMultitool.Utilities;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    public class CreateParallelMode : BaseNodeLineMode, ICostMode, IInvertNetworkMode
    {
        public static NetworkMultitoolShortcut IncreaseShiftShortcut { get; } = GetShortcut(KeyCode.Equals, nameof(IncreaseShiftShortcut), nameof(Localize.Settings_Shortcut_IncreaseShift), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateParallelMode)?.IncreaseShift(), repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseShiftShortcut { get; } = GetShortcut(KeyCode.Minus, nameof(DecreaseShiftShortcut), nameof(Localize.Settings_Shortcut_DecreaseShift), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateParallelMode)?.DecreaseShift(), repeat: true, ignoreModifiers: true);

        public static NetworkMultitoolShortcut IncreaseHeightShortcut { get; } = GetShortcut(KeyCode.RightBracket, nameof(IncreaseHeightShortcut), nameof(Localize.Settings_Shortcut_IncreaseHeight), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateParallelMode)?.IncreaseHeight(), repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseHeightShortcut { get; } = GetShortcut(KeyCode.LeftBracket, nameof(DecreaseHeightShortcut), nameof(Localize.Settings_Shortcut_DecreaseHeight), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateParallelMode)?.DecreaseHeight(), repeat: true, ignoreModifiers: true);

        public static NetworkMultitoolShortcut ChangeSideShortcut { get; } = GetShortcut(KeyCode.Tab, nameof(ChangeSideShortcut), nameof(Localize.Settings_Shortcut_InvertShift), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateParallelMode)?.ChangeSide());

        public override ToolModeType Type => ToolModeType.CreateParallel;
        private bool Calculated { get; set; }
        private List<Point> Points { get; set; }
        protected NetInfo Info => GetNetInfo() ?? Nodes[0].Id.GetNode().Info;
        protected override bool AllowUntouch => true;

        private bool Side { get; set; }
        private bool Invert { get; set; }
        private bool ResultInvert => Side ^ Invert;

        private float? Shift { get; set; }
        private float DeltaHeight { get; set; }
        private MeasureStraight StartLine { get; set; }
        private MeasureStraight EndLine { get; set; }
        private InfoLabel StartHeightLabel { get; set; }
        private InfoLabel EndHeightLabel { get; set; }
        private bool AllowHeight => Info.m_segments.All(s => !s.m_requireHeightMap);
        public int Cost { get; private set; }
        private new bool EnoughMoney => !NeedMoney || EnoughMoney(Cost);

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in base.Shortcuts)
                    yield return shortcut;

                yield return IncreaseShiftShortcut;
                yield return DecreaseShiftShortcut;

                yield return IncreaseHeightShortcut;
                yield return DecreaseHeightShortcut;

                yield return ChangeSideShortcut;
                yield return InvertNetworkShortcut;
            }
        }

        protected override string GetInfo()
        {
            if (AddState == AddResult.None && Nodes.Count >= 2)
            {
                var text = CostInfo +
                    Localize.Mode_NodeLine_Info_SelectNode + "\n\n" +
                    string.Format(Localize.Mode_Info_ChangeShift, DecreaseShiftShortcut.AddInfoColor(), IncreaseShiftShortcut.AddInfoColor()) + "\n" +
                    (AllowHeight ? (string.Format(Localize.Mode_Info_Parallel_ChangeHeight, DecreaseHeightShortcut.AddInfoColor(), IncreaseHeightShortcut.AddInfoColor()) + "\n") : string.Empty) +
                    Localize.Mode_Info_Step + "\n" +
                    string.Format(Localize.Mode_Info_Parallel_ChangeShift, ChangeSideShortcut.AddInfoColor()) + "\n" +
                    (IsInvertable(Info) ? string.Format(Localize.Mode_Info_InvertNetwork, InvertNetworkShortcut.AddInfoColor()) + "\n" : string.Empty) +
                    string.Format(Localize.Mode_Info_Parallel_Create, ApplyShortcut.AddInfoColor()) +
                    UndergroundInfo;

                return text;
            }
            else
                return base.GetInfo();
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Calculated = false;
            Shift = null;
            Side = true;
            Invert = false;
            DeltaHeight = 0f;
            Cost = 0;

            if (StartLine != null && StartLine.Label != null)
            {
                RemoveLabel(StartLine.Label);
                StartLine.Label = null;
            }
            if (EndLine != null && EndLine.Label != null)
            {
                RemoveLabel(EndLine.Label);
                EndLine.Label = null;
            }

            StartLine = null;
            EndLine = null;

            StartHeightLabel ??= AddLabel(1f);
            EndHeightLabel ??= AddLabel(1f);
        }
        protected override void ClearLabels()
        {
            base.ClearLabels();

            if (StartLine != null)
                StartLine.Label = null;

            if (EndLine != null)
                EndLine.Label = null;

            StartHeightLabel = null;
            EndHeightLabel = null;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (Nodes.Count >= 2 && !Calculated)
                Calculate();

            StartLine?.Update(Calculated);
            EndLine?.Update(Calculated);

            if (Calculated && DeltaHeight != 0)
            {
                StartHeightLabel.Show = true;
                EndHeightLabel.Show = true;

                var text = (DeltaHeight >= 0f ? "+" : "-") + GetLengthString(Mathf.Abs(DeltaHeight));

                var delta = Settings.ShowMesh ? DeltaHeight : 0f;
                StartHeightLabel.WorldPosition = Points[0].Position.AddHeight(delta);
                StartHeightLabel.Direction = (Points[0].Position - Nodes[0].Id.GetNode().m_position).Turn90(Side).MakeFlatNormalized();
                StartHeightLabel.text = text;

                EndHeightLabel.WorldPosition = Points[Points.Count - 1].Position.AddHeight(delta);
                EndHeightLabel.Direction = (Points[Points.Count - 1].Position - Nodes[Nodes.Count - 1].Id.GetNode().m_position).Turn90(!Side).MakeFlatNormalized();
                EndHeightLabel.text = text;
            }
            else
            {
                StartHeightLabel.Show = false;
                EndHeightLabel.Show = false;
            }
        }
        private void Calculate()
        {
            Points = new List<Point>(Nodes.Count);

            if (Shift == null)
            {
                var sum = 0f;
                for (var i = 1; i < Nodes.Count; i += 1)
                {
                    NetExtension.GetCommonSegment(Nodes[i - 1].Id, Nodes[i].Id, out var segmentId);
                    sum += segmentId.GetSegment().Info.m_halfWidth;
                }
                Shift = sum / (Nodes.Count - 1) + Info.m_halfWidth;
            }

            var shift = Side ? Shift.Value : -Shift.Value;
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                var nodeId = Nodes[i].Id;
                ref var node = ref nodeId.GetNode();

                if (i == 0)
                {
                    NetExtension.GetCommonSegment(nodeId, Nodes[i + 1].Id, out var segmentId);
                    ref var segment = ref segmentId.GetSegment();
                    var direction = segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection;
                    var shiftDir = direction.Turn90(false).MakeFlatNormalized();
                    Points.Add(new Point(node.m_position + shiftDir * shift + Vector3.up * DeltaHeight, direction, Vector3.zero));
                }
                else if (i == Nodes.Count - 1)
                {
                    NetExtension.GetCommonSegment(nodeId, Nodes[i - 1].Id, out var segmentId);
                    ref var segment = ref segmentId.GetSegment();
                    var direction = segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection;
                    var shiftDir = direction.Turn90(true).MakeFlatNormalized();
                    Points.Add(new Point(node.m_position + shiftDir * shift + Vector3.up * DeltaHeight, Vector3.zero, direction));
                }
                else
                {
                    NetExtension.GetCommonSegment(nodeId, Nodes[i - 1].Id, out var backwardSegmentId);
                    NetExtension.GetCommonSegment(nodeId, Nodes[i + 1].Id, out var forwardSegmentId);

                    ref var backwardSegment = ref backwardSegmentId.GetSegment();
                    ref var forwardSegment = ref forwardSegmentId.GetSegment();

                    var backwardEndDir = backwardSegment.GetDirection(Nodes[i - 1].Id);
                    var backwardStartDir = backwardSegment.GetDirection(nodeId);
                    var forwardStartDir = forwardSegment.GetDirection(nodeId);
                    var forwardEndDir = forwardSegment.GetDirection(Nodes[i + 1].Id);

                    var backwardEndPos = Nodes[i - 1].Id.GetNode().m_position + backwardEndDir.Turn90(false).MakeFlatNormalized() * shift + Vector3.up * DeltaHeight;
                    var backwardStartPos = node.m_position + backwardStartDir.Turn90(true).MakeFlatNormalized() * shift + Vector3.up * DeltaHeight;
                    var forwardStartPos = node.m_position + forwardStartDir.Turn90(false).MakeFlatNormalized() * shift + Vector3.up * DeltaHeight;
                    var forwardEndPos = Nodes[i + 1].Id.GetNode().m_position + forwardEndDir.Turn90(true).MakeFlatNormalized() * shift + Vector3.up * DeltaHeight;

                    var backward = new BezierTrajectory(backwardStartPos, backwardStartDir, backwardEndPos, backwardEndDir);
                    var forward = new BezierTrajectory(forwardStartPos, forwardStartDir, forwardEndPos, forwardEndDir);

                    if (Intersection.CalculateSingle(backward, forward, out var backwardT, out var forwardT))
                    {
                        var position = (backward.Position(backwardT) + forward.Position(forwardT)) / 2f;
                        Points.Add(new Point(position, forward.Tangent(forwardT), backward.Tangent(backwardT)));
                    }
                    else if ((backward.StartPosition - forward.StartPosition).sqrMagnitude < 64f)
                    {
                        var position = (backward.StartPosition + forward.StartPosition) / 2f;
                        Points.Add(new Point(position, forward.StartDirection, backward.StartDirection));
                    }
                    else
                    {
                        Points.Add(new Point(backward.StartPosition, -backward.StartDirection, backward.StartDirection));
                        Points.Add(new Point(forward.StartPosition, forward.StartDirection, -forward.StartDirection));
                    }
                }
            }

            Calculated = true;

            if (NeedMoney)
                Cost = GetCost(Points.ToArray(), Info);

            ref var startNode = ref Nodes[0].Id.GetNode();
            ref var endNode = ref Nodes[Nodes.Count - 1].Id.GetNode();

            var startDir = (Points[0].Position - startNode.m_position).Turn90(!Side).MakeFlatNormalized();
            var endDir = (Points[Points.Count - 1].Position - endNode.m_position).Turn90(Side).MakeFlatNormalized();

            var startLength = startNode.Segments().Max(s => s.Info.m_halfWidth) + 2f;
            var endLength = endNode.Segments().Max(s => s.Info.m_halfWidth) + 2f;

            StartLine = new MeasureStraight(startNode.m_position, Points[0].Position, startDir, startLength, StartLine?.Label ?? AddLabel(), startNode.m_position.y);
            EndLine = new MeasureStraight(endNode.m_position, Points[Points.Count - 1].Position, endDir, endLength, EndLine?.Label ?? AddLabel(), endNode.m_position.y);
        }
        protected override void Apply()
        {
            if (Nodes.Count >= 2 && EnoughMoney)
            {
                var points = Points.ToArray();
                var invert = ResultInvert;
                var info = Info;
                var cost = Cost;

                SimulationManager.instance.AddAction(() =>
                {
                    Create(points, invert, info, cost);
                    PlayEffect(points, info.m_halfWidth, true);
                });
            }
        }
        private static void Create(Point[] points, bool invert, NetInfo info, int cost)
        {
            var nodeIds = new List<ushort>();

            for (var i = 0; i < points.Length; i += 1)
            {
                ushort newNodeId;
                if ((i == 0 || i == points.Length - 1) && Settings.AutoConnect)
                    FindOrCreateNode(out newNodeId, info, points[i].Position);
                else
                    CreateNode(out newNodeId, info, points[i].Position);
                nodeIds.Add(newNodeId);
            }

            for (var i = 1; i < nodeIds.Count; i += 1)
            {
                ushort newSegmentId;

                if (invert)
                    CreateSegmentAuto(out newSegmentId, info, nodeIds[i], nodeIds[i - 1], points[i].BackwardDirection, points[i - 1].ForwardDirection);
                else
                    CreateSegmentAuto(out newSegmentId, info, nodeIds[i - 1], nodeIds[i], points[i - 1].ForwardDirection, points[i].BackwardDirection);

                CalculateSegmentDirections(newSegmentId);
            }

            ChangeMoney(cost, info);
        }
        private void IncreaseShift() => ChangeShift(true);
        private void DecreaseShift() => ChangeShift(false);
        private void ChangeShift(bool increase)
        {
            var step = 1f;
            if (Utility.OnlyShiftIsPressed)
                step = 10f;
            else if (Utility.OnlyCtrlIsPressed)
                step = 0.1f;
            else if (Utility.OnlyAltIsPressed)
                step = 0.01f;

            Shift = Mathf.Max((Shift.Value + (increase ? step : -step)).RoundToNearest(step), 0f);
            Calculated = false;
        }
        private void IncreaseHeight() => ChangeHeight(true);
        private void DecreaseHeight() => ChangeHeight(false);
        private void ChangeHeight(bool increase)
        {
            if (!AllowHeight)
                return;

            var step = 1f;
            if (Utility.OnlyShiftIsPressed)
                step = 10f;
            else if (Utility.OnlyCtrlIsPressed)
                step = 0.1f;
            else if (Utility.OnlyAltIsPressed)
                step = 0.01f;

            DeltaHeight = (DeltaHeight + (increase ? step : -step)).RoundToNearest(step);
            Calculated = false;
        }

        private void ChangeSide()
        {
            Side = !Side;
            Calculated = false;
        }
        public void SetInvert()
        {
            if (IsInvertable(Info))
            {
                Invert = !Invert;
                Calculated = false;
            }
        }
        protected override void AddFirst(NodeSelection selection)
        {
            base.AddFirst(selection);
            Calculated = false;
        }
        protected override void AddLast(NodeSelection selection)
        {
            base.AddLast(selection);
            Calculated = false;
        }
        protected override void RemoveFirst()
        {
            base.RemoveFirst();
            Calculated = false;
        }
        protected override void RemoveLast()
        {
            base.RemoveLast();
            Calculated = false;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (Calculated)
            {
                if (Settings.ShowOverlay)
                    RenderPartsOverlay(cameraInfo, Points, EnoughMoney ? CommonColors.Yellow : CommonColors.Red, Info.m_halfWidth * 2f);

                RenderPartsArrows(cameraInfo, Points, Info, ResultInvert);

                StartLine.Render(cameraInfo, CommonColors.Gray224, CommonColors.Gray224, Underground);
                EndLine.Render(cameraInfo, CommonColors.Gray224, CommonColors.Gray224, Underground);
            }
        }
        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo)
        {
            if (Calculated && Settings.ShowMesh)
            {
                var points = Points.ToArray();
                RenderPartsGeometry(points, Info, ResultInvert);
            }

            base.RenderGeometry(cameraInfo);
        }
    }
}
