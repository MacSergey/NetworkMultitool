using ColossalFramework;
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
    public class CreateLoopMode : BaseCreateMode
    {
        public static NetworkMultitoolShortcut SwitchIsLoopShortcut = GetShortcut(KeyCode.Tab, nameof(SwitchIsLoopShortcut), nameof(Localize.Settings_Shortcut_SwitchIsLoop), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateLoopMode)?.SwitchIsLoop());

        public override ToolModeType Type => ToolModeType.CreateLoop;

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in base.Shortcuts)
                    yield return shortcut;

                yield return SwitchIsLoopShortcut;
            }
        }

        private MiddleCircle Circle { get; set; }
        private Straight StartStraight { get; set; }
        private Straight EndStraight { get; set; }

        protected override string GetInfo()
        {
            if (!IsFirst)
            {
                if (!IsHoverSegment)
                    return Localize.Mode_Info_SelectFirstSegment + UndergroundInfo;
                else
                    return Localize.Mode_Info_ClickFirstSegment + StepOverInfo;
            }
            else if (!IsSecond)
            {
                if (!IsHoverSegment)
                    return Localize.Mode_Info_SelectSecondSegment + UndergroundInfo;
                else
                    return Localize.Mode_Info_ClickSecondSegment + StepOverInfo;
            }
            else if (IsHoverNode)
                return Localize.Mode_Info_ClickToChangeCreateDir;
            else if (State == Result.BigRadius)
                return Localize.Mode_Info_RadiusTooBig;
            else if (State == Result.SmallRadius)
                return Localize.Mode_Info_RadiusTooSmall;
            else if (State != Result.Calculated)
                return Localize.Mode_Info_ClickOnNodeToChangeCreateDir;
            else
                return
                    Localize.Mode_Info_ClickOnNodeToChangeCreateDir + "\n\n" +
                    string.Format(Localize.Mode_Info_ChangeRadius, DecreaseRadiusShortcut, IncreaseRadiusShortcut) + "\n" +
                    string.Format(Localize.Mode_CreateLoop_Info_Change, SwitchIsLoopShortcut) + "\n" +
                    Localize.Mode_Info_Step + "\n" +
                    string.Format(Localize.Mode_Info_Create, ApplyShortcut);
        }

        protected override void ResetParams()
        {
            base.ResetParams();
            ResetData();
        }
        private void ResetData()
        {
            if (Circle is MiddleCircle circle)
            {
                if (circle.Label != null)
                {
                    RemoveLabel(circle.Label);
                    circle.Label = null;
                }
            }
            if (StartStraight is Straight oldStart)
            {
                if (oldStart.Label != null)
                {
                    RemoveLabel(oldStart.Label);
                    oldStart.Label = null;
                }
            }
            if (EndStraight is Straight oldEnd)
            {
                if (oldEnd.Label != null)
                {
                    RemoveLabel(oldEnd.Label);
                    oldEnd.Label = null;
                }
            }

            Circle = null;
            StartStraight = null;
            EndStraight = null;
        }
        protected override void ClearLabels()
        {
            base.ClearLabels();

            if (Circle != null)
                Circle.Label = null;

            if (StartStraight != null)
                StartStraight.Label = null;

            if (EndStraight != null)
                EndStraight.Label = null;
        }

        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (State != Result.None)
            {
                var info = Info;
                Circle?.Update(State == Result.Calculated);
                StartStraight?.Update(info, State == Result.Calculated);
                EndStraight?.Update(info, State == Result.Calculated);
            }
        }
        protected override void Init(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory)
        {
            ResetData();

            if (!Intersection.CalculateSingle(firstTrajectory, secondTrajectory, out _, out _))
            {
                State = Result.NotIntersect;
                return;
            }

            Circle = new MiddleCircle(Circle?.Label ?? AddLabel(), firstTrajectory, secondTrajectory, Height);
        }
        protected override IEnumerable<Point> Calculate()
        {
            if (!Circle.Calculate(MinPossibleRadius, float.MaxValue, out var result))
            {
                State = result;
                return new Point[] { Point.Empty };
            }

            Circle.GetStraight(StartStraight?.Label ?? AddLabel(), EndStraight?.Label ?? AddLabel(), Height, out var start, out var end);
            StartStraight = start;
            EndStraight = end;

            State = Result.Calculated;
            return GetParts();
        }
        private IEnumerable<Point> GetParts()
        {
            if (StartStraight.Length >= 8f)
            {
                foreach (var point in StartStraight.Parts)
                    yield return point;
                yield return new Point(Circle.StartPos, StartStraight.Direction);
            }
            foreach (var point in Circle.Parts)
                yield return point;

            if (EndStraight.Length >= 8f)
            {
                yield return new Point(Circle.EndPos, EndStraight.Direction);
                foreach (var point in EndStraight.Parts)
                    yield return point;
            }
        }

        protected override void IncreaseRadius()
        {
            var step = Step;
            Circle.Radius = (Circle.Radius + step).RoundToNearest(step);
            State = Result.None;
        }
        protected override void DecreaseRadius()
        {
            var step = Step;
            Circle.Radius = (Circle.Radius - step).RoundToNearest(step);
            State = Result.None;
        }
        private void SwitchIsLoop()
        {
            Circle.IsLoop = !Circle.IsLoop;
            State = Result.None;
        }
        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            Circle.Render(cameraInfo, info, Colors.Gray224, Underground);
            StartStraight.Render(cameraInfo, info, Colors.Gray224, Colors.Gray224, Underground);
            EndStraight.Render(cameraInfo, info, Colors.Gray224, Colors.Gray224, Underground);
        }

        protected class MiddleCircle : Circle
        {
            private StraightTrajectory StartGuide { get; }
            private StraightTrajectory EndGuide { get; }

            public override Vector3 CenterPos
            {
                get => base.CenterPos;
                set { }
            }
            public override Vector3 StartRadiusDir
            {
                get => base.StartRadiusDir;
                set { }
            }
            public override Vector3 EndRadiusDir
            {
                get => base.EndRadiusDir;
                set { }
            }
            public override Direction Direction
            {
                get => base.Direction;
                set { }
            }
            public bool IsLoop { get; set; }

            public MiddleCircle(InfoLabel label, StraightTrajectory startGuide, StraightTrajectory endGuide, float height) : base(label, height)
            {
                StartGuide = startGuide;
                EndGuide = endGuide;
            }

            public override bool Calculate(float minRadius, float maxRadius, out Result result)
            {
                var angle = MathExtention.GetAngle(StartGuide.Direction, EndGuide.Direction);
                var halfAbsAngle = Mathf.Abs(angle) / 2f;

                Intersection.CalculateSingle(StartGuide, EndGuide, out var firtsT, out var secondT);
                var direct = firtsT >= 0f && secondT >= 0f && !IsLoop;

                base.Direction = direct == angle >= 0 ? Direction.Right : Direction.Left;

                if (direct)
                    maxRadius = Mathf.Tan(halfAbsAngle) * Mathf.Min(firtsT, secondT);
                else
                {
                    minRadius = Mathf.Max(Mathf.Tan(halfAbsAngle) * Mathf.Max(-Mathf.Min(firtsT, 0f), -Mathf.Min(secondT, 0f)), minRadius);
                    maxRadius = Mathf.Max(MinRadius + 200f, 500f);
                }

                if (!base.Calculate(minRadius, maxRadius, out result))
                    return false;

                if (Radius > 1000f)
                {
                    result = Result.BigRadius;
                    return false;
                }
                if (MaxRadius < MinRadius)
                {
                    result = Result.SmallRadius;
                    return false;
                }

                var delta = Radius / Mathf.Tan(halfAbsAngle);
                var startLenghth = direct ? firtsT - delta : firtsT + delta;
                var endLength = direct ? secondT - delta : secondT + delta;

                var sign = direct ? -1 : 1;
                var intersect = (StartGuide.Position(firtsT) + EndGuide.Position(secondT)) / 2f;
                var centerDir = sign * (StartGuide.Direction + EndGuide.Direction).normalized;
                var distant = Radius / Mathf.Sin(halfAbsAngle);

                base.CenterPos = intersect + centerDir * distant;
                base.StartRadiusDir = (StartGuide.Position(startLenghth) - CenterPos).MakeFlatNormalized();
                base.EndRadiusDir = (EndGuide.Position(endLength) - CenterPos).MakeFlatNormalized();

                result = Result.Calculated;
                return true;
            }

            public void GetStraight(InfoLabel startLabel, InfoLabel endLabel, float height, out Straight start, out Straight end)
            {
                start = new Straight(StartGuide.StartPosition, StartPos, StartRadiusDir, startLabel, height);
                end = new Straight(EndPos, EndGuide.StartPosition, EndRadiusDir, endLabel, height);
            }
        }
    }
}
