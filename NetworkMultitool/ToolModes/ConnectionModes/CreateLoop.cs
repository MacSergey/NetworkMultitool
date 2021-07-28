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
        public override ToolModeType Type => ToolModeType.CreateLoop;

        protected NetworkMultitoolShortcut Tab { get; }

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in base.Shortcuts)
                    yield return shortcut;

                yield return Plus;
                yield return LargePlus;
                yield return SmallPlus;
                yield return Minus;
                yield return LargeMinus;
                yield return SmallMinus;
                yield return Tab;
            }
        }

        public CreateLoopMode()
        {
            Tab = GetShortcut(KeyCode.Tab, PressTab, ToolModeType.CreateLoop);
        }

        private float? Radius { get; set; }
        private Vector3 Center { get; set; }
        private Vector3 CenterDir { get; set; }
        private Vector3 StartCurve { get; set; }
        private Vector3 EndCurve { get; set; }
        private float Angle { get; set; }

        private bool IsLoop { get; set; }
        private InfoLabel Label { get; set; }

        protected override string GetInfo()
        {
            if (!IsFirst)
                return "Select first segment" + GetStepOverInfo();
            else if (!IsSecond)
                return "Select second segment" + GetStepOverInfo();
            else if (State == Result.BigRadius)
                return "Radius too big";
            else if (State == Result.SmallRadius)
                return "Radius too small";
            else if (State != Result.Calculated)
                return "Choose nodes to select create direction";
            else
                return $"Press Minus to decrease radius\nPress Plus to increase radius\nPress Enter to create\nPress Tab to change loop";
        }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            IsLoop = false;
            Label = AddLabel();
        }
        protected override void ResetParams()
        {
            base.ResetParams();
            Radius = null;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (State == Result.Calculated)
            {
                Label.isVisible = true;
                Label.text = $"{Radius.Value:0.0}m\n{Mathf.Abs(Angle) * Mathf.Rad2Deg:0}°";
                Label.WorldPosition = Center + CenterDir * 5f;
                Label.Direction = CenterDir;
            }
            else
                Label.isVisible = false;
        }
        protected override IEnumerable<Point> Calculate(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory)
        {
            if (!Intersection.CalculateSingle(firstTrajectory, secondTrajectory, out var firtsT, out var secondT))
            {
                State = Result.NotIntersect;
                yield break;
            }

            var angle = MathExtention.GetAngle(firstTrajectory.Direction, secondTrajectory.Direction);
            var halfAbsAngle = Mathf.Abs(angle) / 2f;

            var direct = firtsT >= 0f && secondT >= 0f && !IsLoop;
            float minRadius;
            float maxRadius;
            if (direct)
            {
                minRadius = MinPossibleRadius;
                maxRadius = Mathf.Tan(halfAbsAngle) * Mathf.Min(firtsT, secondT);
            }
            else
            {
                minRadius = Mathf.Max(Mathf.Tan(halfAbsAngle) * Mathf.Max(-Mathf.Min(firtsT, 0f), -Mathf.Min(secondT, 0f)), MinPossibleRadius);
                maxRadius = Mathf.Max(minRadius + 200f, 500f);
            }
            Radius = Mathf.Clamp(Radius ?? 50f, minRadius, maxRadius);
            if(Radius.Value > 1000f)
            {
                State = Result.BigRadius;
                yield break;
            }
            if(maxRadius < minRadius)
            {
                State = Result.SmallRadius;
                yield break;
            }

            var delta = Radius.Value / Mathf.Tan(halfAbsAngle);
            var startLenght = direct ? firtsT - delta : firtsT + delta;
            var endLenght = direct ? secondT - delta : secondT + delta;

            var sign = direct ? -1 : 1;
            var intersect = (firstTrajectory.Position(firtsT) + secondTrajectory.Position(secondT)) / 2f;
            CenterDir = sign * (firstTrajectory.Direction + secondTrajectory.Direction).normalized;
            var distant = Radius.Value / Mathf.Sin(halfAbsAngle);
            Center = intersect + CenterDir * distant;

            Angle = -sign * Mathf.Sign(angle) * (Mathf.PI + sign * Mathf.Abs(angle));

            StartCurve = firstTrajectory.Position(startLenght);
            EndCurve = secondTrajectory.Position(endLenght);

            if (startLenght >= 8f)
            {
                foreach (var point in GetStraightParts(new StraightTrajectory(firstTrajectory.StartPosition, firstTrajectory.Position(startLenght))))
                    yield return point;
                yield return new Point(StartCurve, firstTrajectory.Direction);
            }

            foreach (var point in GetCurveParts(Center, StartCurve - Center, firstTrajectory.Direction, Radius.Value, Angle))
                yield return point;

            if (endLenght >= 8f)
            {
                yield return new Point(EndCurve, -secondTrajectory.Direction);
                foreach (var point in GetStraightParts(new StraightTrajectory(secondTrajectory.Position(endLenght), secondTrajectory.StartPosition)))
                    yield return point;
            }

            State = Result.Calculated;
        }

        protected override void PressPlus()
        {
            if (Radius != null)
            {
                var step = Step;
                Radius = (Radius.Value + step).RoundToNearest(step);
                State = Result.None;
            }
        }
        protected override void PressMinus()
        {
            if (Radius != null)
            {
                var step = Step;
                Radius = (Radius.Value - step).RoundToNearest(step);
                State = Result.None;
            }
        }
        private void PressTab()
        {
            IsLoop = !IsLoop;
            Radius = null;
            State = Result.None;
        }
        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            RenderCenter(cameraInfo, info, Center, StartCurve, EndCurve, Radius.Value);
        }
    }
}
