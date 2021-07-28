using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModsCommon.Utilities;
using UnityEngine;

namespace NetworkMultitool
{
    public class ArrangeLineMode : BaseNodeLine
    {
        public override ToolModeType Type => ToolModeType.ArrangeAtLine;

        public override void PressEnter()
        {
            if (Nodes.Count >= 3)
            {
                Arrange(Nodes.Select(n => n.Id).ToArray());
                Reset(this);
            }
        }

        private void Arrange(ushort[] nodeIds)
        {
            var startPos = nodeIds[0].GetNode().m_position;
            var endPos = nodeIds[nodeIds.Length - 1].GetNode().m_position;
            var startDir = GetDirection(nodeIds[0], nodeIds[1], out var firstCount).normalized;
            var endDir = GetDirection(nodeIds[nodeIds.Length - 1], nodeIds[nodeIds.Length - 2], out var secondCount).normalized;

            var trajectory = default(ITrajectory);
            if (firstCount == 1 && secondCount == 1)
                trajectory = new StraightTrajectory(startPos, endPos);
            else if (firstCount == 1 && secondCount != 1)
                trajectory = new BezierTrajectory(endPos, endDir, startPos).Invert();
            else if (firstCount != 1 && secondCount == 1)
                trajectory = new BezierTrajectory(startPos, startDir, endPos);
            else
                trajectory = new BezierTrajectory(startPos, startDir, endPos, endDir);

            for (var i = 0; i < nodeIds.Length; i += 1)
            {
                var t = 1f / (nodeIds.Length - 1) * i;
                var pos = trajectory.Position(t);
                var dir = trajectory.Tangent(t).normalized;
                NetManager.instance.MoveNode(nodeIds[i], pos);

                if (i != 0)
                    SetDirection(nodeIds[i], nodeIds[i - 1], -dir);
                if(i != nodeIds.Length - 1)
                    SetDirection(nodeIds[i], nodeIds[i + 1], dir);             
            }
            foreach(var nodeId in nodeIds)
                NetManager.instance.UpdateNode(nodeId);

            static Vector3 GetDirection(ushort nodeId, ushort anotherNodeId, out int segmentCount)
            {
                NetExtension.GetCommon(nodeId, anotherNodeId, out var commonId);
                var segmentIds = nodeId.GetNode().SegmentIds().ToArray();
                segmentCount = segmentIds.Length;
                if (segmentIds.Length == 1)
                {
                    var segment = commonId.GetSegment();
                    return Vector3.zero;
                }
                else
                {
                    var anotherSegmentId = segmentIds.FirstOrDefault(i => i != commonId);
                    var segment = anotherSegmentId.GetSegment();
                    return -(segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection);
                }
            }
            static void SetDirection(ushort nodeId, ushort anotherNodeId, Vector3 direction)
            {
                if (!NetExtension.GetCommon(nodeId, anotherNodeId, out var commonId))
                    return;

                ref var segment = ref commonId.GetSegment();
                if (segment.IsStartNode(nodeId))
                    segment.m_startDirection = direction;
                else
                    segment.m_endDirection = direction;

                NetManager.instance.UpdateSegmentRenderer(commonId, true);
            }
        }
    }
}
