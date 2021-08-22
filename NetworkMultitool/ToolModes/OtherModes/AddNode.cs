using ColossalFramework;
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
        protected override bool IsReseted => true;

        protected override Color32 NodeColor => Colors.Red;
        private bool IsPossibleInsertNode { get; set; }
        private Vector3 InsertPosition { get; set; }

        protected override string GetInfo()
        {
            if (!IsHoverSegment)
                return Localize.Mode_AddNode_Info_SelectToAdd + UndergroundInfo;
            else if (!IsPossibleInsertNode)
                return Localize.Mode_AddNode_Info_TooCloseNode.AddErrorColor() + StepOverInfo;
            else
                return Localize.Mode_AddNode_Info_ClickToAdd.AddActionColor() + StepOverInfo;
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
            {
                var segmentId = HoverSegment.Id;
                var position = InsertPosition;
                SimulationManager.instance.AddAction(() =>
                {
                    InsertNode(segmentId, position);
                    PlayEffect(new EffectInfo.SpawnArea(position, Vector3.zero, segmentId.GetSegment().Info.m_halfWidth), true);
                });
            }
        }
        public bool PossibleInsertNode(Vector3 position)
        {
            if (!IsHoverSegment)
                return false;

            foreach (var data in HoverSegment.Datas)
            {
                var gap = Mathf.Min(data.halfWidth, 8f) + data.halfWidth * 2f * Mathf.Sqrt(1 - data.DeltaAngleCos * data.DeltaAngleCos);
                if ((data.Position - position).sqrMagnitude < gap * gap)
                    return false;
            }

            return true;
        }
        private static bool InsertNode(ushort segmentId, Vector3 position)
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
            RenderNearNodes(cameraInfo);
            RenderSegmentNodes(cameraInfo);

            if (IsHoverSegment)
            {
                var segment = HoverSegment.Id.GetSegment();
                var bezier = new BezierTrajectory(ref segment);
                bezier.Trajectory.GetHitPosition(Tool.Ray, out _, out var t, out var position);
                var direction = bezier.Tangent(t).MakeFlatNormalized();
                var halfWidth = segment.Info.m_halfWidth;

                var color = PossibleInsertNode(position) ? Colors.Green : Colors.Red;
                if (2f * halfWidth > Selection.BorderOverlayWidth)
                {
                    var overlayData = new OverlayData(cameraInfo)
                    {
                        Width = halfWidth * 2,
                        Color = color,
#if DEBUG
                        AlphaBlend = Selection.AlphaBlendOverlay,
#else
                        AlphaBlend = false,
#endif
                        Cut = true,
                        RenderLimit = Underground
                    };

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
                {
                    var overlayData = new OverlayData(cameraInfo)
                    {
                        Width = Mathf.Max(2f * halfWidth, Selection.BorderOverlayWidth / 2),
                        Color = color,
#if DEBUG
                        AlphaBlend = Selection.AlphaBlendOverlay,
#else
                        AlphaBlend = false,
#endif
                        RenderLimit = Underground
                    };
                    position.RenderCircle(overlayData);
                }
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
