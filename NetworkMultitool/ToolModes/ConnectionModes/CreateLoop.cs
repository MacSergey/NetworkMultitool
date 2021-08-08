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
    public abstract class BaseCreateLoopMode : BaseCreateMode
    {
        public MiddleCircle Circle { get; private set; }
        public Straight StartStraight { get; private set; }
        public Straight EndStraight { get; private set; }
        public bool IsHoverCenter { get; protected set; }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is BaseCreateLoopMode loopMode)
            {
                First = loopMode.First;
                Second = loopMode.Second;
                IsFirstStart = loopMode.IsFirstStart;
                IsSecondStart = loopMode.IsSecondStart;

                FirstTrajectory = loopMode.FirstTrajectory;
                SecondTrajectory = loopMode.SecondTrajectory;

                Height = loopMode.Height;

                Circle = loopMode.Circle;
                StartStraight = loopMode.StartStraight;
                EndStraight = loopMode.EndStraight;

                IsHoverCenter = loopMode.IsHoverCenter;

                if (Circle != null)
                    Circle.Label = AddLabel();
                if (StartStraight != null)
                    StartStraight.Label = AddLabel();
                if (EndStraight != null)
                    EndStraight.Label = AddLabel();

                Underground = ForceUnderground;
            }
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

            if (!Intersection.CalculateSingle(firstTrajectory, secondTrajectory, out var firstT, out var secondT) || Mathf.Abs(firstT) > 5000f || Mathf.Abs(secondT) > 5000f)
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
            if (!StartStraight.IsShort)
            {
                foreach (var point in StartStraight.Parts)
                    yield return point;
            }

            foreach (var part in Circle.GetParts(StartStraight, EndStraight))
                yield return part;

            if (!EndStraight.IsShort)
            {
                foreach (var point in EndStraight.Parts)
                    yield return point;
            }
        }
        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            Circle.Render(cameraInfo, info, Colors.Gray224, Underground);
            StartStraight.Render(cameraInfo, info, Colors.Gray224, Colors.Gray224, Underground);
            EndStraight.Render(cameraInfo, info, Colors.Gray224, Colors.Gray224, Underground);

            if (IsHoverCenter)
                Circle.RenderCenterHover(cameraInfo, Colors.Blue, Underground);
        }
        protected override void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            if (IsHoverCenter)
                Circle.RenderCenterHover(cameraInfo, Colors.Blue, Underground);
        }

        public class MiddleCircle : Circle
        {
            private StraightTrajectory StartGuide { get; }
            private StraightTrajectory EndGuide { get; }
            private float StartT { get; }
            private float EndT { get; }
            private float GuideAngle { get; set; }
            private float HalfAbsGuideAngle => Mathf.Abs(GuideAngle) / 2f;

            public override Vector3 CenterPos
            {
                get
                {
                    var distant = Radius / Mathf.Sin(HalfAbsGuideAngle);
                    var position = CenterLine.Position(IsDirect ? -distant : distant);
                    position.y = Height;
                    return position;
                }
                set
                {
                    var normal = CenterLine.Direction.Turn90(true);
                    var normalLine = new StraightTrajectory(value, value + normal, false);
                    Intersection.CalculateSingle(CenterLine, normalLine, out var t, out _);

                    var radius = t * Mathf.Sin(HalfAbsGuideAngle);
                    if (PossibleDirect)
                    {
                        radius = Math.Abs(radius);
                        if (radius < MinRadius)
                            ForceLoop = !ForceLoop;

                        Radius = radius;
                    }
                    else
                        Radius = Mathf.Max(radius, 0f);
                }
            }
            public bool PossibleDirect => StartT >= 0f && EndT >= 0f;
            public bool ForceLoop { get; set; }
            public bool IsDirect => PossibleDirect && !ForceLoop;
            public override float Radius
            {
                get => base.Radius;
                set => base.Radius = Mathf.Clamp(value, MinRadius, MaxRadius);
            }
            public float MinInsideRadius { get; private set; }
            public float MaxInsideRadius { get; private set; }
            public float MinOutsideRadius { get; private set; }
            public float MaxOutsideRadius { get; private set; }
            public override float MinRadius
            {
                get => IsDirect ? MinInsideRadius : MinOutsideRadius;
                set { }
            }
            public override float MaxRadius
            {
                get => IsDirect ? MaxInsideRadius : MaxOutsideRadius;
                set { }
            }

            public override Vector3 StartRadiusDir
            {
                get => StartGuide.Direction.Turn90(Direction == Direction.Left/* ^ IsDirect*/);
                set { }
            }
            public override Vector3 EndRadiusDir
            {
                get => EndGuide.Direction.Turn90(Direction == Direction.Right/* ^ IsDirect*/);
                set { }
            }
            public override Direction Direction
            {
                get => IsDirect == GuideAngle >= 0 ? Direction.Right : Direction.Left;
                set { }
            }
            private StraightTrajectory CenterLine { get; set; }

            public MiddleCircle(InfoLabel label, StraightTrajectory startGuide, StraightTrajectory endGuide, float height) : base(label, height)
            {
                StartGuide = startGuide;
                EndGuide = endGuide;
                GuideAngle = MathExtention.GetAngle(StartGuide.Direction, EndGuide.Direction);

                Intersection.CalculateSingle(StartGuide, EndGuide, out var startT, out var endT);
                StartT = startT;
                EndT = endT;

                var intersect = (StartGuide.Position(StartT) + EndGuide.Position(EndT)) / 2f;
                var centerDir = (StartGuide.Direction + EndGuide.Direction).normalized;
                CenterLine = new StraightTrajectory(intersect, intersect + centerDir, false);
            }

            public override bool Calculate(float minRadius, float maxRadius, out Result result)
            {
                MinInsideRadius = minRadius;
                MaxInsideRadius = Mathf.Tan(HalfAbsGuideAngle) * Mathf.Min(StartT, EndT);

                MinOutsideRadius = Mathf.Max(Mathf.Tan(HalfAbsGuideAngle) * Mathf.Max(-Mathf.Min(StartT, 0f), -Mathf.Min(EndT, 0f)), minRadius);
                MaxOutsideRadius = Mathf.Max(MinOutsideRadius + 200f, 500f);

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
    public class CreateLoopMode : BaseCreateLoopMode
    {
        public static NetworkMultitoolShortcut SwitchIsLoopShortcut = GetShortcut(KeyCode.Tab, nameof(SwitchIsLoopShortcut), nameof(Localize.Settings_Shortcut_SwitchIsLoop), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateLoopMode)?.SwitchIsLoop());

        public override ToolModeType Type => ToolModeType.CreateLoop;

        protected bool IsPressedCenter { get; private set; }

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in base.Shortcuts)
                    yield return shortcut;

                yield return SwitchIsLoopShortcut;
            }
        }

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
            IsPressedCenter = false;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (State == Result.Calculated)
            {
                IsHoverCenter = false;
                if (!IsHoverNode && Tool.MouseRayValid)
                {
                    var mousePosition = GetMousePosition(Circle.CenterPos.y);
                    if ((XZ(Circle.CenterPos) - XZ(mousePosition)).sqrMagnitude <= 25f)
                        IsHoverCenter = true;
                }
            }
        }
        public override void OnMouseDown(Event e)
        {
            IsPressedCenter = IsHoverCenter;
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            IsPressedCenter = false;
            base.OnPrimaryMouseClicked(e);
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsPressedCenter)
                Tool.SetMode(ToolModeType.CreateLoopMoveCircle);
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
            Circle.ForceLoop = !Circle.ForceLoop;
            State = Result.None;
        }
    }
    public class CreateLoopMoveCircleMode : BaseCreateLoopMode
    {
        public override ToolModeType Type => ToolModeType.CreateLoopMoveCircle;
        public override bool CreateButton => false;
        private Vector3 PrevPos { get; set; }

        protected override string GetInfo() => Localize.Mode_Connection_Info_SlowMove;
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            PrevPos = GetMousePosition(Circle.CenterPos.y);
        }
        public override void OnMouseUp(Event e)
        {
            Tool.SetMode(ToolModeType.CreateLoop);
        }
        public override void OnMouseDrag(Event e)
        {
            if (Circle is Circle circle)
            {
                var newPos = GetMousePosition(circle.CenterPos.y);
                var dir = newPos - PrevPos;
                PrevPos = newPos;

                if (Utility.OnlyCtrlIsPressed)
                    circle.CenterPos += dir * 0.1f;
                else if (Utility.OnlyAltIsPressed)
                    circle.CenterPos += dir * 0.01f;
                else
                    circle.CenterPos += dir;

                State = Result.None;
            }
        }
    }
}
