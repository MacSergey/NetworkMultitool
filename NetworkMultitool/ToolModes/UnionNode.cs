using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class UnionNodeMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.UnionNode;
        protected override bool IsReseted => !IsSource;

        protected override bool IsValidNode(ushort nodeId) => base.IsValidNode(nodeId) && !IsSource || nodeId != Source.Id;

        private NodeSelection Source { get; set; }
        private bool IsSource => Source != null;
        private NodeSelection Target => HoverNode;
        private bool IsTarget => Target != null;

        private int Count
        {
            get
            {
                var count = 0;
                if (IsSource)
                    count += Source.Id.GetNode().CountSegments();
                if (IsTarget)
                    count += Target.Id.GetNode().CountSegments();
                return count;
            }
        }
        private bool IsCorrectCount => Count <= 8;
        private bool IsConnected
        {
            get
            {
                if (!IsTarget || !IsSource)
                    return false;
                else
                    return NetExtension.GetCommon(Source.Id, Target.Id, out _);
            }
        }
        private bool IsFar => (Source.Id.GetNode().m_position - Target.Id.GetNode().m_position).sqrMagnitude > 40000f;
        private bool IsCorrect => IsCorrectCount && !IsConnected && !IsFar;

        protected override string GetInfo()
        {
            if (!IsSource)
            {
                if (IsHoverNode)
                    return Localize.Mode_UnionNode_Info_ClickSource + StepOverInfo;
                else
                    return Localize.Mode_UnionNode_Info_SelectSource + UndergroundInfo;
            }
            else if (!IsTarget)
                return Localize.Mode_UnionNode_Info_SelectTarget;
            else if (IsConnected)
                return Localize.Mode_UnionNode_Info_NoCommon;
            else if (!IsCorrectCount)
                return Localize.Mode_UnionNode_Info_Overflow + StepOverInfo;
            else if(IsFar)
                return Localize.Mode_UnionNode_Info_TooFar + StepOverInfo;
            else
                return Localize.Mode_UnionNode_Info_ClickUnion + StepOverInfo;
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Source = null;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsHoverNode)
                return;
            else if (!IsSource)
                Source = HoverNode;
            else if (IsCorrect)
            {
                Union(Source.Id, Target.Id);
                Reset(this);
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsSource)
                Source = null;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!IsSource)
            {
                if (IsHoverNode)
                    HoverNode.Render(new OverlayData(cameraInfo) { Color = Colors.Green, RenderLimit = Underground });
                else
                    RenderSegmentNodes(cameraInfo, IsValidNode);
            }
            else if (!IsTarget)
            {
                Source.Render(new OverlayData(cameraInfo) { RenderLimit = Underground });
                RenderSegmentNodes(cameraInfo, IsValidNode);
            }
            else if (!IsCorrect)
            {
                Source.Render(new OverlayData(cameraInfo) { Color = Colors.Red, RenderLimit = Underground });
                Target.Render(new OverlayData(cameraInfo) { Color = Colors.Red, RenderLimit = Underground });
            }
            else
            {
                Source.Render(new OverlayData(cameraInfo) { Color = Colors.Green, RenderLimit = Underground });
                Target.Render(new OverlayData(cameraInfo) { Color = Colors.Green, RenderLimit = Underground });
            }
        }

        private bool Union(ushort sourceId, ushort targetId)
        {
            var sourceNode = sourceId.GetNode();
            var segmentIds = sourceNode.SegmentIds().ToArray();
            var terrainRect = GetTerrainRect(segmentIds);

            foreach (var segmentId in segmentIds)
                RelinkSegment(segmentId, sourceId, targetId);

            RemoveNode(sourceId);
            UpdateTerrain(terrainRect);

            return true;
        }
    }
}
