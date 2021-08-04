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
    public class ArrangeLineMode : BaseNodeLineMode
    {
        public override ToolModeType Type => ToolModeType.ArrangeAtLine;

        private ushort FirstGuide { get; set; }
        private ushort LastGuide { get; set; }
        private Result Calculated { get; set; }
        private List<Point> Points { get; set; }

        private bool IsHoverEnd
        {
            get
            {
                if (Nodes.Count < 3 || !IsHoverSegment)
                    return false;
                ref var segment = ref HoverSegment.Id.GetSegment();
                return segment.Contains(Nodes[0].Id) || segment.Contains(Nodes[Nodes.Count - 1].Id);
            }
        }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            FirstGuide = 0;
            LastGuide = 0;
            Calculated = Result.None;
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
            Calculated = Result.None;
        }
        private ushort GetGuide(bool isFirst)
        {
            if (Nodes.Count < 2)
                return 0;

            var nodeId = isFirst ? Nodes[0].Id : Nodes[Nodes.Count - 1].Id;
            var nextNodeId = isFirst ? Nodes[1].Id : Nodes[Nodes.Count - 2].Id;

            if (nodeId.GetNode().CountSegments() <= 1)
                return 0;

            NetExtension.GetCommon(nodeId, nextNodeId, out var commonId);

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
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (Calculated == Result.None)
            {
                Calculated = GetPoint(out var points) ? Result.Complite : Result.Failed;
                Points = points;
            }
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            base.OnPrimaryMouseClicked(e);

            if (IsHoverEnd)
            {
                ref var segment = ref HoverSegment.Id.GetSegment();
                if (segment.Contains(Nodes[0].Id))
                {
                    FirstGuide = HoverSegment.Id;
                    Calculated = Result.None;
                }
                else if (segment.Contains(Nodes[Nodes.Count - 1].Id))
                {
                    LastGuide = HoverSegment.Id;
                    Calculated = Result.None;
                }
            }
        }
        protected override void Apply()
        {
            if (Calculated == Result.Complite)
            {
                Arrange();
                Reset(this);
            }
        }

        private void Arrange()
        {
            var segmentIds = new ushort[Nodes.Count - 1];
            for (var i = 1; i < Nodes.Count; i += 1)
                NetExtension.GetCommon(Nodes[i - 1].Id, Nodes[i].Id, out segmentIds[i - 1]);
            var terrainRect = GetTerrainRect(segmentIds);

            for (var i = 1; i < Nodes.Count - 1; i += 1)
                MoveNode(Nodes[i].Id, Points[i].Position);

            for (var i = 0; i < Nodes.Count; i += 1)
            {
                var dir = Points[i].Direction;

                if (i != 0)
                    SetDirection(Nodes[i].Id, Nodes[i - 1].Id, -dir);
                if (i != Nodes.Count - 1)
                    SetDirection(Nodes[i].Id, Nodes[i + 1].Id, dir);
            }

            //var trajectory = GetTrajectory();
            //var partLength = trajectory.Length / (Nodes.Count - 1);
            //var ts = new List<float>() { 0f };
            //for (var i = 1; i < Nodes.Count - 1; i += 1)
            //{
            //    ts.Add(trajectory.Travel(ts.Last(), partLength));
            //}
            //ts.Add(1f);

            //for (var i = 1; i < Nodes.Count - 1; i += 1)
            //{
            //    var pos = trajectory.Position(ts[i]);
            //    MoveNode(Nodes[i].Id, pos);
            //}
            //for (var i = 0; i < Nodes.Count; i += 1)
            //{
            //    var dir = trajectory.Tangent(ts[i]).normalized;

            //    if (i != 0)
            //        SetDirection(Nodes[i].Id, Nodes[i - 1].Id, -dir);
            //    if (i != Nodes.Count - 1)
            //        SetDirection(Nodes[i].Id, Nodes[i + 1].Id, dir);
            //}
            foreach (var node in Nodes)
                NetManager.instance.UpdateNode(node.Id);

            UpdateTerrain(terrainRect);
        }
        private void MoveNode(ushort nodeId, Vector3 newPos)
        {
            ref var node = ref nodeId.GetNode();
            var segmentIds = node.SegmentIds().ToArray();

            var startDirections = new Dictionary<ushort, Vector3>();
            foreach (var segmentId in segmentIds)
            {
                ref var otherNode = ref segmentId.GetSegment().GetOtherNode(nodeId).GetNode();
                var dir = (node.m_position - otherNode.m_position).MakeFlat();
                startDirections[segmentId] = dir;
            }

            newPos.y = node.m_position.y;
            NetManager.instance.MoveNode(nodeId, newPos);

            foreach (var segmentId in segmentIds)
            {
                ref var otherNode = ref segmentId.GetSegment().GetOtherNode(nodeId).GetNode();
                var newDir = (node.m_position - otherNode.m_position).MakeFlat();
                var oldDir = startDirections[segmentId];

                var delta = MathExtention.GetAngle(oldDir, newDir);
                ref var segment = ref segmentId.GetSegment();
                if (segment.IsStartNode(nodeId))
                    segment.m_startDirection = segment.m_startDirection.TurnRad(delta, false);
                else
                    segment.m_endDirection = segment.m_endDirection.TurnRad(delta, false);

                NetManager.instance.UpdateSegmentRenderer(segmentId, true);
            }
        }
        private bool GetPoint(out List<Point> result)
        {
            if (Nodes.Count < 3)
            {
                result = new List<Point>();
                return false;
            }

            var startPos = Nodes[0].Id.GetNode().m_position;
            var endPos = Nodes[Nodes.Count - 1].Id.GetNode().m_position;
            var startDir = NormalizeXZ(GetDirection(true, out var firstCount));
            var endDir = NormalizeXZ(GetDirection(false, out var secondCount));

            if (firstCount == 1 && secondCount == 1)
                return GetStraightPoints(startPos, endPos, out result);
            else if (firstCount == 1 && secondCount != 1)
            {
                startDir = CalculateDirection(endPos, endDir, startPos);
                return GetCurvePoints(startPos, endPos, startDir, endDir, out result);
            }
            else if (firstCount != 1 && secondCount == 1)
            {
                endDir = CalculateDirection(startPos, startDir, endPos);
                return GetCurvePoints(startPos, endPos, startDir, endDir, out result);
            }
            else
                return GetCurvePoints(startPos, endPos, startDir, endDir, out result);

            static Vector3 CalculateDirection(Vector3 startPos, Vector3 startDir, Vector3 endPos)
            {
                var centerDir = startPos - endPos;
                var delta = MathExtention.GetAngle(centerDir, startDir);
                var endDir = -centerDir.TurnRad(-delta, false);
                return endDir;
            }
        }
        private bool GetStraightPoints(Vector3 startPos, Vector3 endPos, out List<Point> result)
        {
            var line = new StraightTrajectory(startPos, endPos);
            result = new List<Point>();
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                var t = 1f / (Nodes.Count - 1) * i;
                result.Add(new Point(line.Position(t), line.Tangent(t)));
            }
            return true;
        }
        private bool GetCurvePoints(Vector3 startPos, Vector3 endPos, Vector3 startDir, Vector3 endDir, out List<Point> result)
        {
            result = new List<Point>();

            var connect = endPos - endDir;
            var startSide = NormalizeCrossXZ(startDir, connect) >= 0f;
            var endSide = NormalizeCrossXZ(endDir, -connect) >= 0f;

            if (startSide == endSide)
                return false;

            var startNormal = startDir.Turn90(!startSide).MakeFlatNormalized();
            var endNormal = endDir.Turn90(!endSide).MakeFlatNormalized();

            if (!Line2.Intersect(XZ(startPos), XZ(startPos + startNormal), XZ(endPos), XZ(endPos + endNormal), out var startT, out var endT) || startT >= 0f || endT >= 0)
                return false;

            var center = ((startPos + startNormal * startT) + (endPos + endNormal * endT)) / 2f;
            var angle = MathExtention.GetAngle(startNormal, endNormal);

            var count = Mathf.CeilToInt(Mathf.Abs(angle) / (Mathf.PI / 32f));
            var deltaAngle = angle / count;

            var radii = new List<float>();
            var lengths = new List<float>();
            var radiusDelta = (endT - startT) / count;
            for (var i = 0; i < count; i += 1)
            {
                var radius = Mathf.Abs(startT + radiusDelta * (i + 0.5f));
                radii.Add(radius);
                var length = Mathf.Abs(radius * deltaAngle);
                lengths.Add(length);
            }
            var sum = lengths.Sum();
            var partLength = sum / (Nodes.Count - 1);

            var currentSum = 0f;
            var currentI = -1;
            var deltas = new List<float>();
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                var required = partLength * i;
                while (currentSum < required)
                {
                    currentI += 1;
                    currentSum += lengths[currentI];
                }

                var delta = currentSum - required;
                var index = Math.Max(currentI, 0);
                deltas.Add(index + delta / lengths[index]);
            }

            foreach (var delta in deltas)
            {
                var thisAngle = deltaAngle * delta;
                var centerDir = startNormal.TurnRad(thisAngle, false);
                var radius = Mathf.Abs(startT + radiusDelta * delta);
                var position = center + centerDir * radius;
                var dirAngle = Mathf.PI / 2f;
                var direction = centerDir.TurnRad(dirAngle, angle <= 0f);
                result.Add(new Point(position, direction));
            }

            return true;
        }

        private Vector3 GetDirection(bool isFirst, out int segmentCount)
        {
            var nodeId = (isFirst ? Nodes[0] : Nodes[Nodes.Count - 1]).Id;
            segmentCount = nodeId.GetNode().CountSegments();
            if (nodeId.GetNode().CountSegments() == 1)
                return Vector3.zero;
            else
            {
                var segment = (isFirst ? FirstGuide : LastGuide).GetSegment();
                if (segment.Contains((isFirst ? Nodes[1] : Nodes[Nodes.Count - 2]).Id))
                    return -(segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection);
                else
                    return segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection;
            }
        }
        private void SetDirection(ushort nodeId, ushort anotherNodeId, Vector3 direction)
        {
            if (!NetExtension.GetCommon(nodeId, anotherNodeId, out var commonId))
                return;

            ref var segment = ref commonId.GetSegment();
            if (segment.IsStartNode(nodeId))
                segment.m_startDirection = direction;
            else
                segment.m_endDirection = direction;

            CalculateSegmentDirections(commonId);
            NetManager.instance.UpdateSegmentRenderer(commonId, true);
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (Nodes.Count >= 3)
                RenderParts(Points, cameraInfo, Colors.Yellow, 8f);

            base.RenderOverlay(cameraInfo);

            if (IsHoverEnd)
                HoverSegment.Render(new OverlayData(cameraInfo) { Color = Colors.Blue });
        }

        private enum Result
        {
            None,
            Complite,
            Failed
        }
    }
}
