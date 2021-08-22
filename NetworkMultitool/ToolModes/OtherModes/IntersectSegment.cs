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
    public class IntersectSegmentMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.IntersectSegment;
        protected override bool SelectNodes => false;
        protected override Color32 SegmentColor => Colors.Blue;
        protected override bool IsReseted => !IsFirstSelect;
        protected override bool CanSwitchUnderground => !IsFirstSelect;

        private SegmentSelection First { get; set; }
        private bool IsFirstSelect => First != null;
        private SegmentSelection Second => HoverSegment;
        private bool IsSecondSelect => IsHoverSegment;
        private Result State { get; set; }

        protected override bool IsValidSegment(ushort segmentId) => !IsFirstSelect || First.Id != segmentId;
        protected override string GetInfo()
        {
            if (!IsFirstSelect)
            {
                if (!IsHoverSegment)
                    return Localize.Mode_Info_SelectFirstSegment + UndergroundInfo;
                else
                    return AddActionColor(Localize.Mode_Info_ClickFirstSegment) + StepOverInfo;
            }
            else
            {
                if (!IsSecondSelect)
                    return Localize.Mode_Info_SelectSecondSegment;
                else if(State == Result.CommonNode)
                    return AddErrorColor(Localize.Mode_IntersectSegment_Info_CommonNode);
                else if (State == Result.NotIntersect)
                    return AddErrorColor(Localize.Mode_IntersectSegment_Info_NotIntersect);
                else if (State == Result.Incorrect)
                    return AddErrorColor(Localize.Mode_IntersectSegment_Info_EdgeTooClose);
                else
                    return AddActionColor(Localize.Mode_Info_ClickSecondSegment) + StepOverInfo;
            }
        }

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (!IsFirstSelect || !IsSecondSelect)
                State = Result.None;
            else if (NetExtension.IsCommon(First.Id, Second.Id))
                State = Result.CommonNode;
            else
            {
                var count = 0;
                foreach (var firstLine in First.BetweenDataLines)
                {
                    foreach (var secondLine in Second.BetweenDataLines)
                    {
                        if (Intersection.CalculateSingle(firstLine, secondLine, out _, out _))
                            count += 1;
                    }
                }

                if (count == 0)
                    State = Result.NotIntersect;
                else if (count == 4)
                    State = Result.Correct;
                else
                    State = Result.Incorrect;
            }
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            First = null;
            State = Result.None;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsFirstSelect)
            {
                if (IsHoverSegment)
                    First = HoverSegment;
            }
            else if (State == Result.Correct)
            {
                var firstId = First.Id;
                var secondId = Second.Id;
                SimulationManager.instance.AddAction(() =>
                {
                    IntersectSegments(firstId, secondId);
                    PlaySegmentEffect(firstId, true);
                    PlaySegmentEffect(secondId, true);
                    ClearSelectionBuffer();
                });

                Reset(this);
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsFirstSelect)
                Reset(this);
        }

        private static bool IntersectSegments(ushort firstId, ushort secondId)
        {
            if (firstId == 0 || secondId == 0 || firstId == secondId)
                return false;

            var firstSegment = firstId.GetSegment();
            var secondSegment = secondId.GetSegment();

            if (!firstSegment.m_flags.CheckFlags(NetSegment.Flags.Created, NetSegment.Flags.Deleted) || !secondSegment.m_flags.CheckFlags(NetSegment.Flags.Created, NetSegment.Flags.Deleted))
                return false;

            var firstTrajectory = new BezierTrajectory(ref firstSegment);
            var secondTrajectory = new BezierTrajectory(ref secondSegment);

            if (!Intersection.CalculateSingle(firstTrajectory, secondTrajectory, out var firstT, out var secondT))
                return false;

            var firstPos = firstTrajectory.Position(firstT);
            var firstDir = firstTrajectory.Tangent(firstT).normalized;

            var secondPos = secondTrajectory.Position(secondT);
            var secondDir = secondTrajectory.Tangent(secondT).normalized;

            var pos = (firstPos + secondPos) / 2f;

            RemoveSegment(firstId);
            RemoveSegment(secondId);

            if (!CreateNode(out var newNodeId, firstSegment.Info, pos))
                return false;

            var isFirstInvert = firstSegment.IsInvert();
            var isSecondInvert = secondSegment.IsInvert();

            CreateSegment(out _, firstSegment.Info, firstSegment.m_startNode, newNodeId, firstSegment.m_startDirection, -firstDir, isFirstInvert);
            CreateSegment(out _, firstSegment.Info, newNodeId, firstSegment.m_endNode, firstDir, firstSegment.m_endDirection, isFirstInvert);

            CreateSegment(out _, secondSegment.Info, secondSegment.m_startNode, newNodeId, secondSegment.m_startDirection, -secondDir, isSecondInvert);
            CreateSegment(out _, secondSegment.Info, newNodeId, secondSegment.m_endNode, secondDir, secondSegment.m_endDirection, isSecondInvert);

            return true;
        }

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsFirstSelect)
            {
                var color = State switch
                {
                    Result.None => Colors.White,
                    Result.Correct => Colors.Green,
                    _ => Colors.Red,
                };

                First.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
                if (IsSecondSelect)
                    Second.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
            }
            else
                base.RenderOverlay(cameraInfo);
        }

        private enum Result
        {
            None,
            Correct,
            CommonNode,
            Incorrect,
            NotIntersect,
        }
    }
}
