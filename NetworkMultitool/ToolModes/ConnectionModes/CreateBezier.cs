using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    public class CreateBezierMode : BaseCreateMode
    {
        public override ToolModeType Type => ToolModeType.CreateBezier;

        private BezierTrajectory Bezier { get; set; }
        private bool? IsStartHover { get; set; }
        private bool? IsStartPressed { get; set; }
        protected override bool SelectNodes => base.SelectNodes && IsStartPressed == null;
        protected override bool AllowUntouch => true;

        protected override string GetInfo()
        {
            if (GetBaseInfo() is string baseInfo)
                return baseInfo;
            else if (IsStartPressed != null)
                return null;
            else if (State != Result.Calculated)
                return Localize.Mode_Info_ClickOnNodeToChangeCreateDir;
            else
                return
                    Localize.Mode_Info_ClickOnNodeToChangeCreateDir + "\n" +
                    Localize.Mode_Info_CreateBezier_MoveGiude + "\n\n" +
                    string.Format(Localize.Mode_Info_Create, ApplyShortcut);
        }
        protected override void ResetParams()
        {
            base.ResetParams();

            IsStartHover = null;
            IsStartPressed = null;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (State == Result.Calculated)
            {
                if (!IsHoverNode && IsStartPressed == null && Tool.MouseRayValid)
                {
                    var mousePosition = GetMousePosition(Height);
                    if ((XZ(Bezier.Trajectory.b) - XZ(mousePosition)).sqrMagnitude < 25f)
                        IsStartHover = true;
                    else if ((XZ(Bezier.Trajectory.c) - XZ(mousePosition)).sqrMagnitude < 25f)
                        IsStartHover = false;
                    else
                        IsStartHover = null;
                }
            }
        }
        public override void OnMouseDown(Event e)
        {
            IsStartPressed = IsStartHover;
            if(IsStartPressed != null)
                PrevPos = IsStartPressed == true ? Bezier.Trajectory.b : Bezier.Trajectory.c;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            IsStartPressed = null;
            base.OnPrimaryMouseClicked(e);
        }
        private Vector3 PrevPos { get; set; }
        public override void OnMouseDrag(Event e)
        {
            if (IsStartPressed != null)
            {
                var newPos = GetMousePosition(PrevPos.y);
                var dir = newPos - PrevPos;
                PrevPos = newPos;

                newPos = (IsStartPressed == true ? Bezier.Trajectory.b : Bezier.Trajectory.c) + dir;
                var guide = IsStartPressed == true ? FirstTrajectory : SecondTrajectory;
                var normal = guide.Direction.Turn90(true);
                Intersection.CalculateSingle(guide, new StraightTrajectory(newPos, newPos + normal, false), out var t, out _);
                newPos = guide.Position(Mathf.Max(t, Info.m_halfWidth));

                var bezier = new Bezier3
                {
                    a = Bezier.Trajectory.a,
                    b = IsStartPressed == true ? newPos : Bezier.Trajectory.b,
                    c = IsStartPressed == false ? newPos : Bezier.Trajectory.c,
                    d = Bezier.Trajectory.d,
                };
                Bezier = new BezierTrajectory(bezier);

                Recalculate();
            }
        }

        protected override Result Init(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory)
        {
            var startPos = firstTrajectory.StartPosition.SetHeight(Height);
            var startDir = firstTrajectory.StartDirection.MakeFlatNormalized();
            var endPos = secondTrajectory.StartPosition.SetHeight(Height);
            var endDir = secondTrajectory.StartDirection.MakeFlatNormalized();

            Bezier = new BezierTrajectory(startPos, startDir, endPos, endDir, forceSmooth: true);

            return Result.None;
        }
        protected override Point[] Calculate(out Result result)
        {
            var count = Mathf.CeilToInt(Bezier.Length / MaxLengthGetter());
            var partLength = Bezier.Length / count;
            var points = new Point[count - 1];

            var t = 0f;
            for (var i = 1; i < count; i += 1)
            {
                t = Bezier.Travel(t, partLength);
                var point = new Point(Bezier.Position(t), Bezier.Tangent(t));
                points[i - 1] = point;
            }

            result = Result.Calculated;
            return points;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (State == Result.Calculated)
            {
                var start = new StraightTrajectory(Bezier.Trajectory.a, Bezier.Trajectory.b);
                var end = new StraightTrajectory(Bezier.Trajectory.d, Bezier.Trajectory.c);
                RenderGuide(cameraInfo, start);
                RenderGuide(cameraInfo, end);

                if (IsStartHover != null)
                {
                    (IsStartHover == true ? Bezier.Trajectory.b : Bezier.Trajectory.c).RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Blue, RenderLimit = Underground }, 7f, 5f);
                }
            }
        }
        private void RenderGuide(RenderManager.CameraInfo cameraInfo, StraightTrajectory guide)
        {
            guide.Render(new OverlayData(cameraInfo) { RenderLimit = Underground });
            guide.EndPosition.RenderCircle(new OverlayData(cameraInfo) { RenderLimit = Underground }, 5f, 0f);
        }
    }
}
