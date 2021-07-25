using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class SlopeNodeMode : BaseNodeLine
    {
        public override ToolModeType Type => ToolModeType.SlopeNode;

        public override void PressEnter()
        {
            if (Nodes.Count >= 3)
            {
                SetSlope(Nodes.Select(n => n.Id).ToArray());
                Reset(this);
            }
        }
        private bool SetSlope(ushort[] nodeIds)
        {
            var startY = nodeIds.First().GetNode().m_position.y;
            var endY = nodeIds.Last().GetNode().m_position.y;

            var list = new List<ITrajectory>();

            for (var i = 1; i < nodeIds.Length; i += 1)
            {
                var firstId = nodeIds[i - 1];
                var secondId = nodeIds[i];

                if (!NetExtension.GetCommon(firstId, secondId, out var commonSegmentId))
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
    }
}
