using ModsCommon.Utilities;
using System.Collections.Generic;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    public class CreateCurveMode : BaseCreateMode
    {
        public override ToolModeType Type => ToolModeType.CreateCurve;

        private BezierTrajectory Bezier { get; set; }
        private Vector3 StartPos { get; set; }
        private Vector3 StartDir { get; set; }
        private Vector3 EndPos { get; set; }
        private Vector3 EndDir { get; set; }
        protected override bool AllowUntouch => true;

        private int HoverControl { get; set; }
        protected bool IsHoverControl => HoverControl != -1;
        private int PressedControl { get; set; }
        protected bool IsPressedControl => PressedControl != -1;

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return ApplyShortcut;

                yield return InvertNetworkShortcut;
                yield return SwitchOffsetShortcut;
                yield return SwitchFollowTerrainShortcut;
            }
        }

        protected override string GetInfo()
        {
            if (GetBaseInfo() is string baseInfo)
                return baseInfo;
            else if (CalcState == CalcResult.WrongShape)
                return Localize.Mode_Info_WrongShape.AddErrorColor();
            else if (CalcState == CalcResult.OutOfMap)
                return Localize.Mode_Info_OutOfMap.AddErrorColor();
            else if (CalcState != CalcResult.Calculated)
                return Localize.Mode_Info_ClickOnNodeToChangeCreateDir;
            else if (IsHoverControl || IsPressedControl)
                return Localize.Mode_Info_DragControlPoint;
            else
                return
                    CostInfo +
                    Localize.Mode_Info_ClickOnNodeToChangeCreateDir + "\n" +
                    Localize.Mode_Info_DragControlPoint + "\n" +
                    (IsInvertable(Info) ? string.Format(Localize.Mode_Info_InvertNetwork, InvertNetworkShortcut.AddInfoColor()) + "\n" : string.Empty) +
                    (CanFollowTerrain ? string.Format(Localize.Mode_Info_SwitchFollowTerrain, SwitchFollowTerrainShortcut.AddInfoColor()) + "\n" : string.Empty) +
                    string.Format(Localize.Mode_Info_Create, ApplyShortcut.AddInfoColor());
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (CalcState != CalcResult.None)
            {
                HoverControl = -1;

                var mousePositionB = GetMousePosition(Bezier.Trajectory.b.y);
                if ((XZ(Bezier.Trajectory.b) - XZ(mousePositionB)).sqrMagnitude <= 25f)
                {
                    HoverControl = 0;
                    return;
                }

                var mousePositionC = GetMousePosition(Bezier.Trajectory.c.y);
                if ((XZ(Bezier.Trajectory.c) - XZ(mousePositionC)).sqrMagnitude <= 25f)
                {
                    HoverControl = 1;
                    return;
                }
            }
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            base.OnPrimaryMouseClicked(e);
            PressedControl = -1;
        }
        public override void OnMouseDown(Event e)
        {
            PressedControl = HoverControl;
        }
        public override void OnMouseDrag(Event e)
        {
            var pos = PressedControl == 0 ? StartPos : EndPos;
            var dir = PressedControl == 0 ? StartDir : EndDir;
            var hit = GetMousePosition(pos.y);

            var guide = new StraightTrajectory(pos, pos + dir, true, false);
            var normal = new StraightTrajectory(hit, hit + dir.Turn90(true), false);

            if(Intersection.CalculateSingle(guide, normal, out var t, out _))
            {
                var trajectory = Bezier.Trajectory;

                if (PressedControl == 0)
                    trajectory.b = guide.Position(t);
                else
                    trajectory.c = guide.Position(t);

                Bezier = new BezierTrajectory(trajectory);

                Recalculate();
            }
        }
        protected override void ResetParams()
        {
            base.ResetParams();

            HoverControl = -1;
            PressedControl = -1;
        }

        protected override bool Init(bool reinit, StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, out CalcResult calcState)
        {
            var connect = new StraightTrajectory(firstTrajectory.StartPosition, secondTrajectory.EndPosition);
            if (NormalizeDotXZ(firstTrajectory.StartDirection, connect.Direction) < -0.7 || NormalizeDotXZ(secondTrajectory.StartDirection, -connect.Direction) < -0.7)
            {
                calcState = CalcResult.WrongShape;
                return false;
            }

            StartPos = firstTrajectory.StartPosition.SetHeight(Height);
            StartDir = firstTrajectory.StartDirection.MakeFlatNormalized();
            EndPos = secondTrajectory.StartPosition.SetHeight(Height);
            EndDir = secondTrajectory.StartDirection.MakeFlatNormalized();

            Bezier = new BezierTrajectory(StartPos, StartDir, EndPos, EndDir, new BezierTrajectory.Data(false, true, true, false));

            calcState = CalcResult.None;
            return true;
        }
        protected override Point[] Calculate(out CalcResult result)
        {
            var totalLength = Bezier.Length;
            var partLength = MaxLengthGetter();
            var count = Mathf.CeilToInt(totalLength / partLength);
            partLength = totalLength / count;
            var points = new Point[count - 1];

            var t = 0f;
            for (var i = 1; i < count; i += 1)
            {
                t = Bezier.Travel(t, partLength);
                var point = new Point(Bezier.Position(t), Bezier.Tangent(t).MakeFlatNormalized());
                points[i - 1] = point;
            }

            result = CalcResult.Calculated;
            return points;
        }

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            base.RenderCalculatedOverlay(cameraInfo, info);

            RenderCenter(cameraInfo, Bezier.Trajectory.a, Bezier.Trajectory.b);
            RenderCenter(cameraInfo, Bezier.Trajectory.d, Bezier.Trajectory.c);

            if (HoverControl == 0)
                Bezier.Trajectory.b.RenderCircle(new OverlayData(cameraInfo) { Color = CommonColors.Blue, RenderLimit = Underground }, 7f, 5f);
            else if (HoverControl == 1)
                Bezier.Trajectory.c.RenderCircle(new OverlayData(cameraInfo) { Color = CommonColors.Blue, RenderLimit = Underground }, 7f, 5f);
        }

        private void RenderCenter(RenderManager.CameraInfo cameraInfo, Vector3 start, Vector3 end)
        {
            new StraightTrajectory(start, end).Render(new OverlayData(cameraInfo) { Color = CommonColors.Gray224, RenderLimit = Underground });
            end.RenderCircle(new OverlayData(cameraInfo) { Color = CommonColors.Gray224, RenderLimit = Underground }, 5f, 0f);
        }
    }
}
