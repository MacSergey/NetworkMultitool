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
        protected override bool IsReseted => true;

        private bool IsCorrect => IsHoverNode && HoverNode.Id.GetNode().CountSegments() == 2;

        protected override string GetInfo()
        {
            if (!IsHoverNode)
                return Localize.Tool_RemoveNode_Info_Select + UndergroundInfo;
            else if (!IsCorrect)
                return Localize.Mode_RemoveNode_Info_NotAllow.AddErrorColor() + StepOverInfo;
            else
                return Localize.Tool_RemoveNode_Info_ClickToRemove.AddActionColor() + StepOverInfo;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsCorrect)
            {
                var nodeId = HoverNode.Id;
                SimulationManager.instance.AddAction(() =>
                {
                    RemoveNode(nodeId);
                    PlayNodeEffect(nodeId, true);
                });

                Reset(this);
            }
        }
        private static new bool RemoveNode(ushort nodeId)
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
                invert &= segment.IsStartNode(nodeId) ^ i != 0 ^ segment.IsInvert();
                RemoveSegment(segmentIds[i]);
            }

            BaseNetworkMultitoolMode.RemoveNode(nodeId);

            return CreateSegment(out _, info, nodeIds[0], nodeIds[1], directions[0], directions[1], invert);
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RenderNearNodes(cameraInfo);
            RenderSegmentNodes(cameraInfo);

            if (IsHoverNode)
                HoverNode.Render(new OverlayData(cameraInfo) { Color = IsCorrect ? CommonColors.Green : CommonColors.Red, RenderLimit = Underground });
        }
    }
}
