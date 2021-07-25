﻿using ColossalFramework;
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
        private SegmentSelection First { get; set; }
        private bool IsFirstSelect => First != null;
        private SegmentSelection Second => HoverSegment;
        private bool IsSecondSelect => IsHoverSegment;
        private Result State { get; set; }

        protected override bool IsValidSegment(ushort segmentId) => !IsFirstSelect || First.Id != segmentId;
        public override string GetToolInfo()
        {
            if (!IsFirstSelect)
            {
                if (!IsHoverSegment)
                    return "Select first segment";
                else
                    return string.Format("Segment #{0}\nClick to select first segment", HoverSegment.Id) + GetStepOverInfo();
            }
            else
            {
                if (!IsSecondSelect)
                    return "Select second segment";
                else if (State == Result.NotIntersect)
                    return "These segments are not intersect";
                else if (State == Result.Incorrect)
                    return "Edge of first segment too close to end of second segment";
                else
                    return string.Format("Segment #{0}\nClick to select second segment", HoverSegment.Id) + GetStepOverInfo();
            }
        }

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsFirstSelect && IsSecondSelect)
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
            else
                State = Result.None;
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
                IntersectSegments(First.Id, Second.Id);
                Reset(this);
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsFirstSelect)
                Reset(this);
        }

        private bool IntersectSegments(ushort firstId, ushort secondId)
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
            var color = State switch
            {
                Result.None => Colors.White,
                Result.Correct => Colors.Green,
                _ => Colors.Red,
            };

            if (IsFirstSelect)
                First.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
            if(IsSecondSelect)
                Second.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
        }

        private enum Result
        {
            None,
            Correct,
            Incorrect,
            NotIntersect,
        }
    }
}
