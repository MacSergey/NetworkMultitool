using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using UnityEngine;

namespace NetworkMultitool
{
    public class AddNodeMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.AddNode;
        protected override bool IsReseted => true;

        protected override Color32 NodeColor => CommonColors.Red;
        private bool IsPossibleInsertNode { get; set; }
        private Vector3 InsertPosition { get; set; }
        private SnapTo SnapTo { get; set; }

        private MeasureCurve StartSnapLine { get; set; }
        private MeasureCurve EndSnapLine { get; set; }

        protected override string GetInfo()
        {
            if (!IsHoverSegment)
                return Localize.Mode_AddNode_Info_SelectToAdd + UndergroundInfo;
            else if (!IsPossibleInsertNode)
                return Localize.Mode_AddNode_Info_TooCloseNode.AddErrorColor() + StepOverInfo;
            else if (!Utility.CtrlIsPressed)
                return 
                    Localize.Mode_AddNode_Info_ClickToAdd.AddActionColor() + 
                    $"\n\n{string.Format(Localize.Mode_AddNode_Info_PreciseMeasurement, LocalizeExtension.Ctrl.AddInfoColor())}" + 
                    StepOverInfo;
            else
                return null;
        }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (StartSnapLine != null && StartSnapLine.Label != null)
            {
                RemoveLabel(StartSnapLine.Label);
                StartSnapLine.Label = null;
            }
            if (EndSnapLine != null && EndSnapLine.Label != null)
            {
                RemoveLabel(EndSnapLine.Label);
                EndSnapLine.Label = null;
            }

            StartSnapLine = null;
            EndSnapLine = null;
            SnapTo = SnapTo.None;
        }
        protected override void ClearLabels()
        {
            base.ClearLabels();

            if (StartSnapLine != null)
                StartSnapLine.Label = null;

            if (EndSnapLine != null)
                EndSnapLine.Label = null;
        }

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsHoverSegment)
            {
                IsPossibleInsertNode = GetHitPosition(out var position, out _, out var snapTo);
                InsertPosition = position;
                SnapTo = snapTo;
            }

            StartSnapLine?.Update(IsHoverSegment && SnapTo != SnapTo.None);
            EndSnapLine?.Update(IsHoverSegment && SnapTo != SnapTo.None);
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
        private bool GetHitPosition(out Vector3 insertPosition, out float insertT, out SnapTo snapTo)
        {
            var bezier = new BezierTrajectory(HoverSegment.Id);
            bezier.GetHitPosition(Tool.Ray, out _, out insertT, out insertPosition);
            //if (Utility.CtrlIsPressed)
            //{
            //    var startDistance = bezier.Distance(0f, insertT);
            //    var endDistance = bezier.Length - startDistance;
            //    var startRoundDistance = Mathf.Round(startDistance / 8f) * 8f;
            //    var endRoundDistance = Mathf.Round(endDistance / 8f) * 8f;

            //    var startDelta = Mathf.Abs(startDistance - startRoundDistance);
            //    var endDelta = Mathf.Abs(endDistance - endRoundDistance);

            //    float firstDistance;
            //    float secondDistance;
            //    SnapTo firstSnap;
            //    SnapTo secondSnap;

            //    if (startDelta <= endDelta)
            //    {
            //        firstDistance = startRoundDistance;
            //        secondDistance = bezier.Length - endRoundDistance;
            //        firstSnap = SnapTo.Start;
            //        secondSnap = SnapTo.End;
            //    }
            //    else
            //    {
            //        firstDistance = bezier.Length - endRoundDistance;
            //        secondDistance = startRoundDistance;
            //        firstSnap = SnapTo.End;
            //        secondSnap = SnapTo.Start;
            //    }

            //    if(Mathf.Abs(firstDistance - secondDistance) <= 0.1f)
            //    {
            //        tempT = bezier.Travel((firstDistance + secondDistance) * 0.5f);
            //        var tempPosition = bezier.Position(tempT);
            //        if (PossibleInsertNode(tempPosition))
            //        {
            //            insertPosition = tempPosition;
            //            insertT = tempT;
            //            snapTo = SnapTo.Both;
            //            return true;
            //        }
            //    }

            //    {
            //        tempT = bezier.Travel(firstDistance);
            //        var tempPosition = bezier.Position(tempT);
            //        if (PossibleInsertNode(tempPosition))
            //        {
            //            insertPosition = tempPosition;
            //            insertT = tempT;
            //            snapTo = firstSnap;
            //            return true;
            //        }
            //    }

            //    {
            //        tempT = bezier.Travel(secondDistance);
            //        var tempPosition = bezier.Position(tempT);
            //        if (PossibleInsertNode(tempPosition))
            //        {
            //            insertPosition = tempPosition;
            //            insertT = tempT;
            //            snapTo = secondSnap;
            //            return true;
            //        }
            //    }
            //}

            snapTo = Utility.CtrlIsPressed ? SnapTo.Both : SnapTo.None;
            return PossibleInsertNode(insertPosition);
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
                var possibleInsert = GetHitPosition(out var position, out var t, out var snapTo);
                var bezier = new BezierTrajectory(HoverSegment.Id);
                var direction = bezier.Tangent(t).normalized;
                var halfWidth = HoverSegment.Id.GetSegment().Info.m_halfWidth;

                var color = possibleInsert ? CommonColors.Green : CommonColors.Red;
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

                if(possibleInsert && snapTo != SnapTo.None)
                {
                    StartSnapLine = new MeasureCurve(bezier.Cut(0f, t), StartSnapLine?.Label ?? AddLabel(), halfWidth + 2f);
                    StartSnapLine.Render(cameraInfo, CommonColors.White, (snapTo & SnapTo.Start) != 0 ? CommonColors.Yellow : CommonColors.White, Underground);

                    EndSnapLine = new MeasureCurve(bezier.Cut(t, 1f), EndSnapLine?.Label ?? AddLabel(), halfWidth + 2f);
                    EndSnapLine.Render(cameraInfo, CommonColors.White, (snapTo & SnapTo.End) != 0 ? CommonColors.Yellow : CommonColors.White, Underground);
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

    [Flags]
    public enum SnapTo
    {
        None = 0,
        Start = 1,
        End = 2,
        Both = Start | End,
    }
}
