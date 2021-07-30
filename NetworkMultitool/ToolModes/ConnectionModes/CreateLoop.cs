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
            else if (State == Result.BigRadius)
                return Localize.Mode_Info_RadiusTooBig;
            else if (State == Result.SmallRadius)
                return Localize.Mode_Info_RadiusTooSmall;
            else if (State != Result.Calculated)
                return Localize.Mode_Info_ChooseDirestion;
            else
                return
                    Localize.Mode_Info_ChooseDirestion + "\n\n" +
                    string.Format(Localize.Mode_Info_DecreaseRadius, Minus) + "\n" +
                    string.Format(Localize.Mode_Info_IncreaseRadius, Plus) + "\n" +
                    string.Format(Localize.Mode_CreateLoop_Info_Change, Tab) + "\n" +
                    string.Format(Localize.Mode_Info_Create, Enter);
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
                Label.text = $"{GetRadiusString(Radius.Value)}\n{GetAngleString(Mathf.Abs(Angle))}";
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
