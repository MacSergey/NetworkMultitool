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
    public class CreateConnectionMode : BaseCreateMode
    {
        public override ToolModeType Type => ToolModeType.CreateConnection;

        protected NetworkMultitoolShortcut OncePlus { get; }
        protected NetworkMultitoolShortcut OnceLargePlus { get; }
        protected NetworkMultitoolShortcut OnceSmallPlus { get; }
        protected NetworkMultitoolShortcut OnceMinus { get; }
        protected NetworkMultitoolShortcut OnceLargeMinus { get; }
        protected NetworkMultitoolShortcut OnceSmallMinus { get; }
        protected NetworkMultitoolShortcut Tab { get; }

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in base.Shortcuts)
                    yield return shortcut;

                yield return OncePlus;
                yield return OnceLargePlus;
                yield return OnceSmallPlus;

                yield return OnceMinus;
                yield return OnceLargeMinus;
                yield return OnceSmallMinus;

                yield return Tab;
            }
        }

        public CreateConnectionMode()
        {
            OncePlus = GetShortcut(KeyCode.RightBracket, PressOncePlus, ToolModeType.CreateConnection, repeat: true);
            OnceLargePlus = GetShortcut(KeyCode.RightBracket, PressOncePlus, ToolModeType.CreateConnection, shift: true, repeat: true);
            OnceSmallPlus = GetShortcut(KeyCode.RightBracket, PressOncePlus, ToolModeType.CreateConnection, ctrl: true, repeat: true);

            OnceMinus = GetShortcut(KeyCode.LeftBracket, PressOnceMinus, ToolModeType.CreateConnection, repeat: true);
            OnceLargeMinus = GetShortcut(KeyCode.LeftBracket, PressOnceMinus, ToolModeType.CreateConnection, shift: true, repeat: true);
            OnceSmallMinus = GetShortcut(KeyCode.LeftBracket, PressOnceMinus, ToolModeType.CreateConnection, ctrl: true, repeat: true);

            Tab = GetShortcut(KeyCode.Tab, PressTab, ToolModeType.CreateConnection);
        }

        private bool Select { get; set; }
        private bool? FirstSide { get; set; }
        private bool? SecondSide { get; set; }
        private float? FirstRadius { get; set; }
        private float? SecondRadius { get; set; }
        private Vector3 FirstCenter { get; set; }
        private Vector3 SecondCenter { get; set; }
        private Vector3 FirstCenterDir { get; set; }
        private Vector3 SecondCenterDir { get; set; }
        private Vector3 FirstStartCurve { get; set; }
        private Vector3 SecondStartCurve { get; set; }
        private Vector3 FirstEndCurve { get; set; }
        private Vector3 SecondEndCurve { get; set; }
        private float FirstAngle { get; set; }
        private float SecondAngle { get; set; }

        private InfoLabel FirstLabel { get; set; }
        private InfoLabel SecondLabel { get; set; }

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
            else if (State == Result.WrongShape)
                return Localize.Mode_Info_WrongShape;
            else if (State != Result.Calculated)
                return Localize.Mode_Info_ChooseDirestion;
            else
                return
                    Localize.Mode_Info_ChooseDirestion + "\n\n" +
                    string.Format(Localize.Mode_Info_DecreaseBothRadius, Minus) + "\n" +
                    string.Format(Localize.Mode_Info_IncreaseBothRadius, Plus) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeCircle, Tab) + "\n" +
                    string.Format(Localize.Mode_Info_DecreaseOneRadius, OnceMinus) + "\n" +
                    string.Format(Localize.Mode_Info_IncreaseOneRadius, OncePlus) + "\n" +
                    string.Format(Localize.Mode_Info_Create, Enter);
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            FirstLabel = AddLabel();
            SecondLabel = AddLabel();
        }
        protected override void ResetParams()
        {
            base.ResetParams();
            FirstRadius = null;
            SecondRadius = null;
            FirstSide = null;
            SecondSide = null;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (State == Result.Calculated)
            {
                FirstLabel.isVisible = true;
                FirstLabel.text = $"{GetRadiusString(FirstRadius.Value)}\n{GetAngleString(FirstAngle)}";
                FirstLabel.WorldPosition = FirstCenter + FirstCenterDir * 5f;
                FirstLabel.Direction = FirstCenterDir;

                SecondLabel.isVisible = true;
                SecondLabel.text = $"{GetRadiusString(SecondRadius.Value)}\n{GetAngleString(SecondAngle)}";
                SecondLabel.WorldPosition = SecondCenter + SecondCenterDir * 5f;
                SecondLabel.Direction = SecondCenterDir;
            }
            else
            {
                FirstLabel.isVisible = false;
                SecondLabel.isVisible = false;
            }
        }

        protected override IEnumerable<Point> Calculate(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory)
        {
            FirstStartCurve = firstTrajectory.StartPosition;
            SecondStartCurve = secondTrajectory.StartPosition;

            GetSides(firstTrajectory, secondTrajectory);

            var firstStartRadiusDir = -firstTrajectory.Direction.Turn90(FirstSide.Value);
            var secondStartRadiusDir = -secondTrajectory.Direction.Turn90(SecondSide.Value);

            GetRadii(firstStartRadiusDir, secondStartRadiusDir);

            var centerConnect = new StraightTrajectory(FirstCenter.MakeFlat(), SecondCenter.MakeFlat());
            GetAngles(centerConnect, firstStartRadiusDir, secondStartRadiusDir);

            var firstEndRadiusDir = firstStartRadiusDir.TurnRad(FirstAngle, true);
            var secondEndRadiusDir = secondStartRadiusDir.TurnRad(SecondAngle, true);

            FirstEndCurve = FirstCenter + firstEndRadiusDir * FirstRadius.Value;
            SecondEndCurve = SecondCenter + secondEndRadiusDir * SecondRadius.Value;

            if (!CheckRadii(centerConnect))
                return new Point[0];

            var connectEnds = new StraightTrajectory(FirstEndCurve.MakeFlat(), SecondEndCurve.MakeFlat(), false);
            FixAngles(firstTrajectory, secondTrajectory, connectEnds);

            FirstCenterDir = (connectEnds.Direction - firstTrajectory.Direction).normalized;
            SecondCenterDir = (-connectEnds.Direction - secondTrajectory.Direction).normalized;

            State = Result.Calculated;
            return GetParts(firstTrajectory, secondTrajectory, connectEnds);
        }
        private void GetSides(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory)
        {
            if (FirstSide != null && SecondSide != null)
                return;

            var connect = new StraightTrajectory(firstTrajectory.StartPosition.MakeFlat(), secondTrajectory.EndPosition.MakeFlat());

            FirstSide = CrossXZ(firstTrajectory.Direction, connect.Direction) >= 0;
            SecondSide = CrossXZ(secondTrajectory.Direction, -connect.Direction) >= 0;
            var firstDot = DotXZ(firstTrajectory.Direction, connect.Direction) >= 0;
            var secondDot = DotXZ(secondTrajectory.Direction, -connect.Direction) >= 0;

            if (firstDot != secondDot && FirstSide != SecondSide)
            {
                if (!firstDot)
                    FirstSide = !FirstSide;
                else if (!secondDot)
                    SecondSide = !SecondSide;
            }
        }
        private void GetRadii(Vector3 firstRadiusDir, Vector3 secondRadiusDir)
        {
            FirstRadius = Mathf.Clamp(FirstRadius ?? 50f, MinPossibleRadius, 1000f);
            SecondRadius = Mathf.Clamp(SecondRadius ?? 50f, MinPossibleRadius, 1000f);

            FirstCenter = FirstStartCurve - firstRadiusDir * FirstRadius.Value;
            SecondCenter = SecondStartCurve - secondRadiusDir * SecondRadius.Value;
        }
        private void GetAngles(StraightTrajectory centerConnect, Vector3 firstRadiusDir, Vector3 secondRadiusDir)
        {
            if (FirstSide == SecondSide)
            {
                var delta = centerConnect.Length / (FirstRadius.Value + SecondRadius.Value) * FirstRadius.Value;
                var deltaAngle = Mathf.Acos(FirstRadius.Value / delta);

                FirstAngle = GetAngle(firstRadiusDir, centerConnect.Direction, deltaAngle, FirstSide.Value);
                SecondAngle = GetAngle(secondRadiusDir, -centerConnect.Direction, deltaAngle, SecondSide.Value);
            }
            else
            {
                var deltaAngle = Mathf.Asin(Mathf.Abs(FirstRadius.Value - SecondRadius.Value) / centerConnect.Length);

                FirstAngle = GetAngle(firstRadiusDir, centerConnect.Direction, Mathf.PI / 2f + Mathf.Sign(SecondRadius.Value - FirstRadius.Value) * deltaAngle, FirstSide.Value);
                SecondAngle = GetAngle(secondRadiusDir, -centerConnect.Direction, Mathf.PI / 2f + Mathf.Sign(FirstRadius.Value - SecondRadius.Value) * deltaAngle, SecondSide.Value);
            }

            static float GetAngle(Vector3 radiusDir, Vector3 connectDir, float deltaAngle, bool cross)
            {
                var angle = MathExtention.GetAngle(radiusDir, connectDir);
                if (angle > 0f == cross)
                    angle -= Mathf.Sign(angle) * 2 * Mathf.PI;
                angle = -Mathf.Sign(angle) * (Mathf.Abs(angle) - deltaAngle);
                return angle;
            }
        }
        private bool CheckRadii(StraightTrajectory centerConnect)
        {
            if (FirstSide.Value == SecondSide.Value)
            {
                if (centerConnect.Length < FirstRadius.Value + SecondRadius.Value)
                {
                    State = Result.BigRadius;
                    return false;
                }
            }
            else
            {
                if (centerConnect.Length + FirstRadius.Value < SecondRadius.Value || centerConnect.Length + SecondRadius.Value < FirstRadius.Value)
                {
                    State = Result.WrongShape;
                    return false;
                }
            }
            return true;
        }
        private void FixAngles(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, StraightTrajectory connectEnds)
        {
            if (FirstSide.Value != SecondSide.Value)
            {
                if (Mathf.Abs(FirstAngle) < Mathf.PI && Intersection.CalculateSingle(firstTrajectory, connectEnds, out var firstT, out _) && firstT < 0f)
                    FirstAngle -= Mathf.Sign(FirstAngle) * 2 * Mathf.PI;
                if (Mathf.Abs(SecondAngle) < Mathf.PI && Intersection.CalculateSingle(secondTrajectory, connectEnds, out var secondT, out _) && secondT < 0f)
                    SecondAngle -= Mathf.Sign(SecondAngle) * 2 * Mathf.PI;
            }
        }
        private IEnumerable<Point> GetParts(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, StraightTrajectory centerConnection)
        {
            foreach (var point in GetCurveParts(FirstCenter, FirstStartCurve - FirstCenter, firstTrajectory.Direction, FirstRadius.Value, FirstAngle))
                yield return point;

            if (centerConnection.Length >= 8f)
            {
                yield return new Point(centerConnection.StartPosition, centerConnection.Direction);
                foreach (var point in GetStraightParts(centerConnection))
                    yield return point;
                yield return new Point(centerConnection.EndPosition, centerConnection.Direction);
            }
            else
                yield return new Point(centerConnection.Position(0.5f), centerConnection.Direction);

            foreach (var point in GetCurveParts(SecondCenter, SecondStartCurve - SecondCenter, -secondTrajectory.Direction, SecondRadius.Value, SecondAngle).Reverse())
                yield return point;
        }

        protected override void SetFirstNode(ref NetSegment segment, ushort nodeId)
        {
            base.SetFirstNode(ref segment, nodeId);
            if (FirstSide.HasValue)
                FirstSide = !FirstSide;
        }
        protected override void SetSecondNode(ref NetSegment segment, ushort nodeId)
        {
            base.SetSecondNode(ref segment, nodeId);
            if (SecondSide.HasValue)
                SecondSide = !SecondSide;
        }
        protected override void PressPlus()
        {
            FirstRadius = PressPlus(FirstRadius);
            SecondRadius = PressPlus(SecondRadius);
            State = Result.None;
        }
        protected override void PressMinus()
        {
            FirstRadius = PressMinus(FirstRadius);
            SecondRadius = PressMinus(SecondRadius);
            State = Result.None;
        }
        private void PressOncePlus()
        {
            if (Select)
                FirstRadius = PressPlus(FirstRadius);
            else
                SecondRadius = PressPlus(SecondRadius);
            State = Result.None;
        }
        private void PressOnceMinus()
        {
            if (Select)
                FirstRadius = PressMinus(FirstRadius);
            else
                SecondRadius = PressMinus(SecondRadius);
            State = Result.None;
        }
        private float? PressPlus(float? radius)
        {
            if (radius != null)
            {
                var step = Step;
                return (radius.Value + step).RoundToNearest(step);
            }
            else
                return radius;
        }
        private float? PressMinus(float? radius)
        {
            if (radius != null)
            {
                var step = Step;
                return (radius.Value - step).RoundToNearest(step);
            }
            else
                return radius;
        }
        private void PressTab() => Select = !Select;

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            //FirstCenter.RenderCircle(new OverlayData(cameraInfo) { Width = FirstRadius.Value * 2f, Color = Color.red });
            //SecondCenter.RenderCircle(new OverlayData(cameraInfo) { Width = SecondRadius.Value * 2f, Color = Color.red });

            //new StraightTrajectory(FirstStartCurve, SecondStartCurve).Render(new OverlayData(cameraInfo) { Color = Color.black });
            //new StraightTrajectory(FirstCenter, SecondCenter).Render(new OverlayData(cameraInfo) { Color = Color.black });

            RenderCenter(cameraInfo, info, FirstCenter, FirstStartCurve, FirstEndCurve, FirstRadius.Value);
            RenderCenter(cameraInfo, info, SecondCenter, SecondStartCurve, SecondEndCurve, SecondRadius.Value);
            (Select ? FirstCenter : SecondCenter).RenderCircle(new OverlayData(cameraInfo) { RenderLimit = Underground }, 7f, 5f);

            //new StraightTrajectory(FirstStartCurve, FirstEndCurve).Render(new OverlayData(cameraInfo));
            //new StraightTrajectory(SecondStartCurve, SecondEndCurve).Render(new OverlayData(cameraInfo));
            //new StraightTrajectory(FirstEndCurve, SecondEndCurve).Render(new OverlayData(cameraInfo));
        }
        protected override void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            if (State == Result.BigRadius || State == Result.SmallRadius || State == Result.WrongShape)
            {
                FirstCenter.RenderCircle(new OverlayData(cameraInfo) { Width = FirstRadius.Value * 2f, Color = Color.red, RenderLimit = Underground });
                SecondCenter.RenderCircle(new OverlayData(cameraInfo) { Width = SecondRadius.Value * 2f, Color = Color.red, RenderLimit = Underground });

                RenderRadius(cameraInfo, info, FirstCenter, FirstStartCurve, FirstRadius.Value, Colors.Red);
                RenderCenter(cameraInfo, FirstCenter, Colors.Red);

                RenderRadius(cameraInfo, info, SecondCenter, SecondStartCurve, SecondRadius.Value, Colors.Red);
                RenderCenter(cameraInfo, SecondCenter, Colors.Red);

                (Select ? FirstCenter : SecondCenter).RenderCircle(new OverlayData(cameraInfo) { RenderLimit = Underground }, 7f, 5f);
            }
        }
    }
}
