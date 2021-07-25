using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ToolBase;

namespace NetworkMultitool
{
    public class AddNodeMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.AddNode;
        protected override bool SelectNodes => false;

        private bool IsPossibleInsertNode { get; set; }
        private Vector3 InsertPosition { get; set; }

        public override string GetToolInfo()
        {
            if (!IsHoverSegment)
                return Localize.Tool_InfoSelectToInsert;
            else if (!IsPossibleInsertNode)
                return Localize.Tool_InfoTooCloseNode + GetStepOverInfo();
            else
                return string.Format(Localize.Tool_InfoClickToInsert, HoverSegment.Id) + GetStepOverInfo();
        }

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsHoverSegment)
            {
                var bezier = new BezierTrajectory(HoverSegment.Id);
                bezier.Trajectory.GetHitPosition(Tool.Ray, out _, out _, out var position);
                IsPossibleInsertNode = PossibleInsertNode(position);
                InsertPosition = position;
            }
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverSegment && IsPossibleInsertNode)
                InsertNode(HoverSegment.Id, InsertPosition);
        }
        public bool PossibleInsertNode(Vector3 position)
        {
            if (!IsHoverSegment)
                return false;

            foreach (var data in HoverSegment.Datas)
            {
                var gap = 8f + data.halfWidth * 2f * Mathf.Sqrt(1 - data.DeltaAngleCos * data.DeltaAngleCos);
                if ((data.Position - position).sqrMagnitude < gap * gap)
                    return false;
            }

            return true;
        }
        private bool InsertNode(ushort segmentId, Vector3 position)
        {
            var segment = segmentId.GetSegment();
            segment.GetClosestPositionAndDirection(position, out var pos, out var dir);

            RemoveSegment(segmentId);

            CreateNode(out var newNodeId, segment.Info, pos);
            var invert = segment.IsInvert();
            CreateSegment(out _, segment.Info, segment.m_startNode, newNodeId, segment.m_startDirection, -dir, invert);
            CreateSegment(out _, segment.Info, newNodeId, segment.m_endNode, dir, segment.m_endDirection, invert);

            return true;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var otherOverlay = new OverlayData(cameraInfo) { Color = new Color(1f, 1f, 1f, 0.3f), RenderLimit = Underground };
            if (IsHoverSegment)
            {
                var segment = HoverSegment.Id.GetSegment();

                if (!Underground ^ segment.m_startNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground))
                    new NodeSelection(segment.m_startNode).Render(otherOverlay);

                if (!Underground ^ segment.m_endNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground))
                    new NodeSelection(segment.m_endNode).Render(otherOverlay);

                var bezier = new BezierTrajectory(ref segment);
                bezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out var position);
                var direction = bezier.Tangent(t).MakeFlatNormalized();
                var halfWidth = segment.Info.m_halfWidth;

                var overlayData = new OverlayData(cameraInfo) { Width = halfWidth * 2, Color = PossibleInsertNode(position) ? Colors.Green : Colors.Red, AlphaBlend = false, Cut = true, RenderLimit = Underground };

                var middle = new Bezier3()
                {
                    a = position + direction,
                    b = position,
                    c = position,
                    d = position - direction,
                };
                middle.RenderBezier(overlayData);

                overlayData.Width = Mathf.Min(halfWidth * 2, Selection.BorderOverlayWidth);
                overlayData.Cut = false;

                var normal = direction.MakeFlatNormalized().Turn90(true);
                RenderBorder(overlayData, position + direction, normal, halfWidth);
                RenderBorder(overlayData, position - direction, normal, halfWidth);
            }
            else
                base.RenderOverlay(cameraInfo);
        }
        private void RenderBorder(OverlayData overlayData, Vector3 position, Vector3 normal, float halfWidth)
        {
            var delta = Mathf.Max(halfWidth - Selection.BorderOverlayWidth / 2, 0f);
            var bezier = new Bezier3
            {
                a = position + normal * delta,
                b = position,
                c = position,
                d = position - normal * delta,
            };
            bezier.RenderBezier(overlayData);
        }
    }
}
