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

        protected override string GetInfo()
        {
            if (IsHoverEnd)
                return Localize.Mode_ArrangeLine_Info_ClickToSelectDirection + UndergroundInfo;
            else if (AddState == AddResult.None && Nodes.Count >= 3)
                return
                    Localize.Mode_NodeLine_Info_SelectNode + "\n" +
                    Localize.Mode_ArrangeLine_Info_SelectDirection +
                    UndergroundInfo;
            else
                return base.GetInfo();
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            FirstGuide = 0;
            LastGuide = 0;
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
        public override void OnPrimaryMouseClicked(Event e)
        {
            base.OnPrimaryMouseClicked(e);

            if (IsHoverEnd)
            {
                ref var segment = ref HoverSegment.Id.GetSegment();
                if (segment.Contains(Nodes[0].Id))
                    FirstGuide = HoverSegment.Id;
                else if (segment.Contains(Nodes[Nodes.Count - 1].Id))
                    LastGuide = HoverSegment.Id;
            }
        }
        protected override void Apply()
        {
            if (Nodes.Count >= 3)
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

            var trajectory = GetTrajectory();
            var partLength = trajectory.Length / (Nodes.Count - 1);
            var ts = new List<float>() { 0f };
            for (var i = 1; i < Nodes.Count - 1; i += 1)
                ts.Add(trajectory.Travel(ts.Last(), partLength));
            ts.Add(1f);

            for (var i = 1; i < Nodes.Count - 1; i += 1)
            {
                var pos = trajectory.Position(ts[i]);
                MoveNode(Nodes[i].Id, pos);
            }
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                var dir = trajectory.Tangent(ts[i]).normalized;

                if (i != 0)
                    SetDirection(Nodes[i].Id, Nodes[i - 1].Id, -dir);
                if (i != Nodes.Count - 1)
                    SetDirection(Nodes[i].Id, Nodes[i + 1].Id, dir);
            }

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
        private ITrajectory GetTrajectory()
        {
            var startPos = Nodes[0].Id.GetNode().m_position;
            var endPos = Nodes[Nodes.Count - 1].Id.GetNode().m_position;
            var startDir = NormalizeXZ(GetDirection(true, out var firstCount));
            var endDir = NormalizeXZ(GetDirection(false, out var secondCount));

            if (firstCount == 1 && secondCount == 1)
                return new StraightTrajectory(startPos, endPos);
            else if (firstCount == 1 && secondCount != 1)
                return new BezierTrajectory(endPos, endDir, startPos, true).Invert();
            else if (firstCount != 1 && secondCount == 1)
                return new BezierTrajectory(startPos, startDir, endPos, true);
            else
                return new BezierTrajectory(startPos, startDir, endPos, endDir, forceSmooth: true);
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
                    return segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection;
                else
                    return -(segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection);
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
            {
                var trajectory = GetTrajectory();
                var partLength = trajectory.Length / (Nodes.Count - 1);
                var ts = new List<float>() { 0f };
                for (var i = 1; i < Nodes.Count - 1; i += 1)
                    ts.Add(trajectory.Travel(ts.Last(), partLength));
                ts.Add(1f);

                var data = new OverlayData(cameraInfo) { Color = Colors.Yellow, Width = 8f, RenderLimit = Underground, Cut = true };
                for (var i = 1; i < ts.Count; i += 1)
                    trajectory.Cut(ts[i - 1], ts[i]).Render(data);
            }

            base.RenderOverlay(cameraInfo);

            if (IsHoverEnd)
                HoverSegment.Render(new OverlayData(cameraInfo) { Color = Colors.Blue });
        }
    }
}
