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
            if (IsHoverGuideSegment)
                return Localize.Mode_ArrangeLine_Info_ClickToSelectDirection + UndergroundInfo;
            else if (AddState == AddResult.None && Nodes.Count >= 3)
                return
                    Localize.Mode_NodeLine_Info_SelectNode + "\n" +
                    Localize.Mode_ArrangeLine_Info_SelectDirection + "\n" +
                    string.Format(Localize.Mode_Info_ArrangeLine_Apply, ApplyShortcut) +
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

            if (IsHoverGuideSegment)
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
                var nodeIds = Nodes.Select(n => n.Id).ToArray();
                var firstGuide = FirstGuide;
                var lastGuide = LastGuide;
                SimulationManager.instance.AddAction(() =>
                {
                    Arrange(nodeIds, firstGuide, lastGuide);
                    PlayAudio(true);
                });

                Reset(this);
            }
        }

        private static void Arrange(ushort[] nodeIds, ushort firstGuideId, ushort lastGuideId)
        {
            var segmentIds = new ushort[nodeIds.Length - 1];
            for (var i = 1; i < nodeIds.Length; i += 1)
                NetExtension.GetCommon(nodeIds[i - 1], nodeIds[i], out segmentIds[i - 1]);
            var terrainRect = GetTerrainRect(segmentIds);

            var trajectory = GetTrajectory(nodeIds, firstGuideId, lastGuideId);
            var partLength = trajectory.Length / (nodeIds.Length - 1);
            var ts = new List<float>() { 0f };
            for (var i = 1; i < nodeIds.Length - 1; i += 1)
                ts.Add(trajectory.Travel(ts.Last(), partLength));
            ts.Add(1f);

            for (var i = 1; i < nodeIds.Length - 1; i += 1)
            {
                var pos = trajectory.Position(ts[i]);
                MoveNode(nodeIds[i], pos);
            }
            for (var i = 0; i < nodeIds.Length; i += 1)
            {
                var dir = trajectory.Tangent(ts[i]).normalized;

                if (i != 0)
                    SetSegmentDirection(nodeIds[i], nodeIds[i - 1], -dir);
                if (i != nodeIds.Length - 1)
                    SetSegmentDirection(nodeIds[i], nodeIds[i + 1], dir);
            }

            foreach (var nodeId in nodeIds)
                NetManager.instance.UpdateNode(nodeId);

            UpdateTerrain(terrainRect);
        }
        private static ITrajectory GetTrajectory(ushort[] nodeIds, ushort firstGuideId, ushort lastGuideId)
        {
            var startPos = nodeIds[0].GetNode().m_position;
            var endPos = nodeIds[nodeIds.Length - 1].GetNode().m_position;
            var startDir = NormalizeXZ(GetDirection(nodeIds, true, firstGuideId, out var firstCount));
            var endDir = NormalizeXZ(GetDirection(nodeIds, false, lastGuideId, out var secondCount));

            if (firstCount == 1 && secondCount == 1)
                return new StraightTrajectory(startPos, endPos);
            else if (firstCount == 1 && secondCount != 1)
                return new BezierTrajectory(endPos, endDir, startPos, true).Invert();
            else if (firstCount != 1 && secondCount == 1)
                return new BezierTrajectory(startPos, startDir, endPos, true);
            else
                return new BezierTrajectory(startPos, startDir, endPos, endDir, forceSmooth: true);
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
            if (Nodes.Count >= 3)
            {
                var nodeIds = Nodes.Select(n => n.Id).ToArray();
                var trajectory = GetTrajectory(nodeIds, FirstGuide, LastGuide);
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

            if (IsHoverGuideSegment)
                HoverSegment.Render(new OverlayData(cameraInfo) { Color = Colors.Purple });
        }
    }
}
