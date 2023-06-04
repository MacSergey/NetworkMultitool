using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    public class ArrangeLineMode : BaseNodeLineMode, ICostMode
    {
        public override ToolModeType Type => ToolModeType.ArrangeAtLine;
        private bool Calculated { get; set; }
        private List<Point> Points { get; set; }
        private ushort FirstGuide { get; set; }
        private ushort LastGuide { get; set; }

        private ITrajectory Trajectory { get; set; }
        private Vector3 StartPos { get; set; }
        private Vector3 StartDir { get; set; }
        private Vector3 EndPos { get; set; }
        private Vector3 EndDir { get; set; }

        private int HoverControl { get; set; }
        protected bool IsHoverControl => HoverControl != -1;
        private int PressedControl { get; set; }
        protected bool IsPressedControl => PressedControl != -1;

        protected override bool SelectSegments => !IsHoverControl && !IsPressedControl && base.SelectSegments;

        public int Cost { get; private set; }
        private new bool EnoughMoney => !NeedMoney || EnoughMoney(Cost);

        private bool IsHoverGuideSegment
        {
            get
            {
                if (Nodes.Count < 3 || !IsHoverSegment)
                    return false;
                ref var segment = ref HoverSegment.Id.GetSegment();
                return segment.Contains(Nodes[0].Id) || segment.Contains(Nodes[Nodes.Count - 1].Id);
            }
        }

        protected override string GetInfo()
        {
            if (IsHoverControl || IsPressedControl)
                return Localize.Mode_Info_DragControlPoint;
            else if (IsHoverGuideSegment)
                return Localize.Mode_ArrangeLine_Info_ClickToSelectDirection + UndergroundInfo; 
            else if (AddState == AddResult.None && Nodes.Count >= 3)
                return
                    CostInfo +
                    Localize.Mode_NodeLine_Info_SelectNode + "\n" +
                    Localize.Mode_ArrangeLine_Info_SelectDirection + "\n" +
                    Localize.Mode_Info_DragControlPoint + "\n" +
                    string.Format(Localize.Mode_Info_ArrangeLine_Apply, ApplyShortcut.AddInfoColor()) +
                    UndergroundInfo;
            else
                return base.GetInfo();
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            Calculated = false;
            Trajectory = null;
            FirstGuide = 0;
            LastGuide = 0;
            Cost = 0;

            HoverControl = -1;
            PressedControl = -1;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (Nodes.Count >= 2)
            {
                if (!Calculated)
                    Calculate();
                else if(Trajectory is BezierTrajectory bezier)
                {
                    HoverControl = -1;

                    var mousePositionB = GetMousePosition(bezier.Trajectory.b.y);
                    if ((XZ(bezier.Trajectory.b) - XZ(mousePositionB)).sqrMagnitude <= 25f)
                    {
                        HoverControl = 0;
                        return;
                    }

                    var mousePositionC = GetMousePosition(bezier.Trajectory.c.y);
                    if ((XZ(bezier.Trajectory.c) - XZ(mousePositionC)).sqrMagnitude <= 25f)
                    {
                        HoverControl = 1;
                        return;
                    }
                }
            }
        }
        private void Calculate()
        {
            var nodeIds = Nodes.Select(n => n.Id).ToArray();

            if (Trajectory == null)
                CalculateTrajectory(nodeIds, FirstGuide, LastGuide);

            var trajectory = Trajectory;
            var partLength = trajectory.Length / (nodeIds.Length - 1);
            var ts = new List<float>() { 0f };
            for (var i = 1; i < nodeIds.Length - 1; i += 1)
                ts.Add(trajectory.Travel(ts.Last(), partLength));
            ts.Add(1f);

            Points = new List<Point>(Nodes.Count);
            foreach (var t in ts)
                Points.Add(new Point(trajectory.Position(t), trajectory.Tangent(t)));

            Calculated = true;

            if (NeedMoney)
            {
                Cost = 0;
                for (var i = 1; i < nodeIds.Length; i += 1)
                {
                    NetExtension.GetCommonSegment(nodeIds[i - 1], nodeIds[i], out var segmentId);
                    ref var segment = ref segmentId.GetSegment();
                    var segmentTrajectory = new BezierTrajectory(ref segment);
                    var delta = partLength - segmentTrajectory.Length;
                    var cost = GetCost(Mathf.Abs(delta), segment.Info);
                    if (delta >= 0f)
                        Cost += cost;
                    else
                        Cost -= cost * 3 / 4;
                }
            }
        }

        protected override void AddFirst(NodeSelection selection)
        {
            base.AddFirst(selection);
            GetGuides();
        }
        protected override void AddLast(NodeSelection selection)
        {
            base.AddLast(selection);
            GetGuides();
        }
        protected override void RemoveFirst()
        {
            base.RemoveFirst();
            GetGuides();
        }
        protected override void RemoveLast()
        {
            base.RemoveLast();
            GetGuides();
        }
        private void GetGuides()
        {
            FirstGuide = GetGuide(true);
            LastGuide = GetGuide(false);
            Trajectory = null;
            Calculated = false;
        }
        private ushort GetGuide(bool isFirst)
        {
            if (Nodes.Count < 2)
                return 0;

            var nodeId = isFirst ? Nodes[0].Id : Nodes[Nodes.Count - 1].Id;
            var nextNodeId = isFirst ? Nodes[1].Id : Nodes[Nodes.Count - 2].Id;

            if (nodeId.GetNode().CountSegments() <= 1)
                return 0;

            NetExtension.GetCommonSegment(nodeId, nextNodeId, out var commonId);

            ref var commonSegment = ref commonId.GetSegment();
            var commonDir = commonSegment.IsStartNode(nodeId) ? commonSegment.m_startDirection : commonSegment.m_endDirection;

            var resultId = (ushort)0;
            var resultDot = 1f;

            foreach (var segmentId in nodeId.GetNode().SegmentIds())
            {
                if (segmentId == commonId)
                    continue;

                ref var segment = ref segmentId.GetSegment();
                var dir = segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection;
                var dot = NormalizeDotXZ(commonDir, dir);
                if (dot < resultDot)
                {
                    resultDot = dot;
                    resultId = segmentId;
                }
            }

            return resultId;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            base.OnPrimaryMouseClicked(e);

            if(IsPressedControl)
                PressedControl = -1;
            else if (IsHoverGuideSegment)
            {
                ref var segment = ref HoverSegment.Id.GetSegment();
                if (segment.Contains(Nodes[0].Id))
                    FirstGuide = HoverSegment.Id;
                else if (segment.Contains(Nodes[Nodes.Count - 1].Id))
                    LastGuide = HoverSegment.Id;

                Trajectory = null;
                Calculated = false;
            }
        }
        public override void OnMouseDown(Event e)
        {
            if (Calculated && !IsHoverGuideSegment)
                PressedControl = HoverControl;
        }
        public override void OnMouseDrag(Event e)
        {
            if (PressedControl != -1 && Trajectory is BezierTrajectory bezier)
            {
                var pos = PressedControl == 0 ? StartPos : EndPos;
                var dir = PressedControl == 0 ? StartDir : EndDir;
                var hit = GetMousePosition(pos.y);

                var guide = new StraightTrajectory(pos, pos + dir, true, false);
                var normal = new StraightTrajectory(hit, hit + dir.Turn90(true), false);

                if (Intersection.CalculateSingle(guide, normal, out var t, out _))
                {
                    var trajectory = bezier.Trajectory;

                    if (PressedControl == 0)
                        trajectory.b = guide.Position(t);
                    else
                        trajectory.c = guide.Position(t);

                    Trajectory = new BezierTrajectory(trajectory);

                    Calculated = false;
                }
            }
        }
        protected override void Apply()
        {
            if (Nodes.Count >= 3 && EnoughMoney)
            {
                var nodeIds = Nodes.Select(n => n.Id).ToArray();
                var points = Points.ToArray();
                var cost = Cost;
                SimulationManager.instance.AddAction(() =>
                {
                    Arrange(nodeIds, points, cost);
                    PlayAudio(true);
                    ClearSelectionBuffer();
                });

                Reset(this);
            }
        }

        private static void Arrange(ushort[] nodeIds, Point[] points, int cost)
        {
            var segmentIds = new ushort[nodeIds.Length - 1];
            for (var i = 1; i < nodeIds.Length; i += 1)
                NetExtension.GetCommonSegment(nodeIds[i - 1], nodeIds[i], out segmentIds[i - 1]);
            var terrainRect = GetTerrainRect(segmentIds);

            for (var i = 1; i < nodeIds.Length - 1; i += 1)
                MoveNode(nodeIds[i], points[i].Position);

            for (var i = 0; i < nodeIds.Length; i += 1)
            {
                if (i != 0)
                    SetSegmentDirection(nodeIds[i], nodeIds[i - 1], points[i].BackwardDirection);
                if (i != nodeIds.Length - 1)
                    SetSegmentDirection(nodeIds[i], nodeIds[i + 1], points[i].ForwardDirection);
            }
            for (var i = 1; i < nodeIds.Length; i += 1)
            {
                NetExtension.GetCommonSegment(nodeIds[i - 1], nodeIds[i], out var segmentId);
                UpdateZones(segmentId);
            }

            foreach (var nodeId in nodeIds)
                NetManager.instance.UpdateNode(nodeId);

            UpdateTerrain(terrainRect);
            ChangeMoney(cost, segmentIds[0].GetSegment().Info);
        }
        private void CalculateTrajectory(ushort[] nodeIds, ushort firstGuideId, ushort lastGuideId)
        {
            StartPos = nodeIds[0].GetNode().m_position;
            EndPos = nodeIds[nodeIds.Length - 1].GetNode().m_position;
            StartDir = NormalizeXZ(GetDirection(nodeIds, true, firstGuideId, out var firstCount));
            EndDir = NormalizeXZ(GetDirection(nodeIds, false, lastGuideId, out var secondCount));

            if (firstCount == 1 && secondCount == 1)
                Trajectory = new StraightTrajectory(StartPos, EndPos);
            else if (firstCount == 1 && secondCount != 1)
                Trajectory = new BezierTrajectory(EndPos, EndDir, StartPos, new BezierTrajectory.Data(false, true, true)).Invert();
            else if (firstCount != 1 && secondCount == 1)
                Trajectory = new BezierTrajectory(StartPos, StartDir, EndPos, new BezierTrajectory.Data(false, true, true));
            else
                Trajectory = new BezierTrajectory(StartPos, StartDir, EndPos, EndDir, new BezierTrajectory.Data(false, true, true));
        }
        private static Vector3 GetDirection(ushort[] nodeIds, bool isFirst, ushort guideId, out int segmentCount)
        {
            var nodeId = isFirst ? nodeIds[0] : nodeIds[nodeIds.Length - 1];
            segmentCount = nodeId.GetNode().CountSegments();
            if (nodeId.GetNode().CountSegments() == 1)
                return Vector3.zero;
            else
            {
                var segment = guideId.GetSegment();
                if (segment.Contains(isFirst ? nodeIds[1] : nodeIds[nodeIds.Length - 2]))
                    return segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection;
                else
                    return -(segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection);
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (Calculated)
            {
                var color = EnoughMoney ? CommonColors.Yellow : CommonColors.Red;
                for (var i = 1; i < Points.Count; i += 1)
                {
                    if (!Points[i - 1].IsEmpty && !Points[i].IsEmpty)
                    {
                        NetExtension.GetCommonSegment(Nodes[i - 1].Id, Nodes[i].Id, out var segmentId);
                        var data = new OverlayData(cameraInfo) { Color = color, Width = segmentId.GetSegment().Info.m_halfWidth * 2f, RenderLimit = Underground, Cut = true };
                        GetTrajectory(Points[i - 1], Points[i]).Render(data);
                    }
                }

                if(Trajectory is BezierTrajectory bezier)
                {
                    RenderCenter(cameraInfo, bezier.Trajectory.a, bezier.Trajectory.b);
                    RenderCenter(cameraInfo, bezier.Trajectory.d, bezier.Trajectory.c);

                    if (HoverControl == 0)
                        bezier.Trajectory.b.RenderCircle(new OverlayData(cameraInfo) { Color = CommonColors.Blue, RenderLimit = Underground }, 7f, 5f);
                    else if (HoverControl == 1)
                        bezier.Trajectory.c.RenderCircle(new OverlayData(cameraInfo) { Color = CommonColors.Blue, RenderLimit = Underground }, 7f, 5f);
                }
            }

            base.RenderOverlay(cameraInfo);

            if (IsHoverGuideSegment)
                HoverSegment.Render(new OverlayData(cameraInfo) { Color = CommonColors.Purple });
        }
        private void RenderCenter(RenderManager.CameraInfo cameraInfo, Vector3 start, Vector3 end)
        {
            new StraightTrajectory(start, end).Render(new OverlayData(cameraInfo) { Color = CommonColors.Gray224, RenderLimit = Underground });
            end.RenderCircle(new OverlayData(cameraInfo) { Color = CommonColors.Gray224, RenderLimit = Underground }, 5f, 0f);
        }
    }
}
