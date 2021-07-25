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
    public class RemoveNodeMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.RemoveNode;
        protected override bool SelectSegments => false;
        private bool IsCorrect => IsHoverNode && HoverNode.Id.GetNode().CountSegments() == 2;

        public override string GetToolInfo()
        {
            if (!IsHoverNode)
                return Localize.Tool_InfoSelectToRemove;
            else if(!IsCorrect)
                return string.Format(Localize.Tool_InfoNotAllowToRemove, HoverNode.Id) + GetStepOverInfo();
            else
                return string.Format(Localize.Tool_InfoClickToRemove, HoverNode.Id) + GetStepOverInfo();
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsCorrect)
                RemoveNode(HoverNode.Id);
        }
        private new bool RemoveNode(ushort nodeId)
        {
            var node = nodeId.GetNode();
            var segmentIds = node.SegmentIds().ToArray();

            if (segmentIds.Length != 2)
                return false;

            var info = node.Info;
            var nodeIds = new ushort[2];
            var directions = new Vector3[2];
            var invert = true;
            for (var i = 0; i < 2; i += 1)
            {
                var segment = segmentIds[i].GetSegment();
                nodeIds[i] = segment.GetOtherNode(nodeId);
                directions[i] = segment.IsStartNode(nodeId) ? segment.m_endDirection : segment.m_startDirection;
                invert &= segment.IsInvert();
                Singleton<NetManager>.instance.ReleaseSegment(segmentIds[i], true);
            }

            base.RemoveNode(nodeId);

            return CreateSegment(out _, info, nodeIds[0], nodeIds[1], directions[0], directions[1], invert);
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverNode)
                HoverNode.Render(new OverlayData(cameraInfo) { Color = IsCorrect ? Colors.Green : Colors.Red, RenderLimit = Underground });
        }
    }
}
