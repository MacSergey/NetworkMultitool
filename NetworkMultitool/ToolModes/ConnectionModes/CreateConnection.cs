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
        public static NetworkMultitoolShortcut SwitchSelectShortcut = GetShortcut(KeyCode.Tab, nameof(SwitchSelectShortcut), nameof(Localize.Settings_Shortcut_SwitchSelect), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.SwitchSelectRadius());
        public static NetworkMultitoolShortcut IncreaseOneRadiusShortcut { get; } = GetShortcut(KeyCode.RightBracket, nameof(IncreaseOneRadiusShortcut), nameof(Localize.Settings_Shortcut_IncreaseOneRadius), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.IncreaseOneRadius(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseOneRadiusShortcut { get; } = GetShortcut(KeyCode.LeftBracket, nameof(DecreaseOneRadiusShortcut), nameof(Localize.Settings_Shortcut_DecreaseOneRadius), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.DecreaseOneRadius(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);

        public static NetworkMultitoolShortcut SwitchOffsetShortcut = GetShortcut(KeyCode.Tab, nameof(SwitchOffsetShortcut), nameof(Localize.Settings_Shortcut_SwitchOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.SwitchSelectOffset(), ctrl: true);
        public static NetworkMultitoolShortcut IncreaseOffsetShortcut { get; } = GetShortcut(KeyCode.Backslash, nameof(IncreaseOffsetShortcut), nameof(Localize.Settings_Shortcut_IncreaseOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.IncreaseOffset(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseOffsetShortcut { get; } = GetShortcut(KeyCode.Quote, nameof(DecreaseOffsetShortcut), nameof(Localize.Settings_Shortcut_DecreaseOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.DecreaseOffset(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);

        public override ToolModeType Type => ToolModeType.CreateConnection;

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in base.Shortcuts)
                    yield return shortcut;

                yield return SwitchSelectShortcut;
                yield return IncreaseOneRadiusShortcut;
                yield return DecreaseOneRadiusShortcut;

                yield return SwitchOffsetShortcut;
                yield return IncreaseOffsetShortcut;
                yield return DecreaseOffsetShortcut;
            }
        }

        private bool SelectRadius { get; set; }
        private bool SelectOffset { get; set; }
        private bool? FirstSide { get; set; }
        private bool? SecondSide { get; set; }
        private float? FirstRadius { get; set; }
        private float? SecondRadius { get; set; }
        private float FirstOffset { get; set; }
        private float SecondOffset { get; set; }

        private Vector3 FirstStart { get; set; }
        private Vector3 SecondStart { get; set; }
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

        private InfoLabel FirstRadiusLabel { get; set; }
        private InfoLabel SecondRadiusLabel { get; set; }
        private InfoLabel FirstOffsetLabel { get; set; }
        private InfoLabel SecondOffsetLabel { get; set; }

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
                return
                    Localize.Mode_Info_ChooseDirestion + "\n" +
                    Localize.Mode_Info_CurveDurection;
            else
                return
                    Localize.Mode_Info_ChooseDirestion + "\n" +
                    Localize.Mode_Info_CurveDurection + "\n\n" +
                    string.Format(Localize.Mode_Info_ChangeBothRadius, DecreaseRadiusShortcut, IncreaseRadiusShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeCircle, SwitchSelectShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeOneRadius, DecreaseOneRadiusShortcut, IncreaseOneRadiusShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_SwitchOffset, SwitchOffsetShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeOffset, DecreaseOffsetShortcut, IncreaseOffsetShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_Create, ApplyShortcut);
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            FirstRadiusLabel = AddLabel();
            SecondRadiusLabel = AddLabel();
            FirstOffsetLabel = AddLabel();
            SecondOffsetLabel = AddLabel();
        }
        protected override void ResetParams()
        {
            base.ResetParams();
            FirstRadius = null;
            SecondRadius = null;
            FirstSide = null;
            SecondSide = null;
            FirstOffset = 0f;
            SecondOffset = 0f;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (State == Result.Calculated)
            {
                FirstRadiusLabel.isVisible = true;
                FirstRadiusLabel.text = $"{GetRadiusString(FirstRadius.Value)}\n{GetAngleString(Mathf.Abs(FirstAngle))}";
                FirstRadiusLabel.WorldPosition = FirstCenter + FirstCenterDir * 5f;
                FirstRadiusLabel.Direction = FirstCenterDir;

                SecondRadiusLabel.isVisible = true;
                SecondRadiusLabel.text = $"{GetRadiusString(SecondRadius.Value)}\n{GetAngleString(Mathf.Abs(SecondAngle))}";
                SecondRadiusLabel.WorldPosition = SecondCenter + SecondCenterDir * 5f;
                SecondRadiusLabel.Direction = SecondCenterDir;


                var info = Info;

                FirstOffsetLabel.isVisible = true;
                FirstOffsetLabel.text = GetRadiusString(FirstOffset);
                FirstOffsetLabel.Direction = (FirstStartCurve - FirstCenter).normalized;
                FirstOffsetLabel.WorldPosition = (FirstStart + FirstStartCurve) / 2f + FirstOffsetLabel.Direction * (info.m_halfWidth + 7f);

                SecondOffsetLabel.isVisible = true;
                SecondOffsetLabel.text = GetRadiusString(SecondOffset);
                SecondOffsetLabel.Direction = (SecondStartCurve - SecondCenter).normalized;
                SecondOffsetLabel.WorldPosition = (SecondStart + SecondStartCurve) / 2f + SecondOffsetLabel.Direction * (info.m_halfWidth + 7f);
            }
            else
            {
                FirstRadiusLabel.isVisible = false;
                SecondRadiusLabel.isVisible = false;
                FirstOffsetLabel.isVisible = false;
                SecondOffsetLabel.isVisible = false;
            }
        }

        protected override IEnumerable<Point> Calculate(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory)
        {
            FirstStart = firstTrajectory.StartPosition;
            SecondStart = secondTrajectory.StartPosition;
            FirstStartCurve = firstTrajectory.Position(FirstOffset);
            SecondStartCurve = secondTrajectory.Position(SecondOffset);

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

            var connect = new StraightTrajectory(FirstStartCurve.MakeFlat(), SecondStartCurve.MakeFlat());

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
            if (Mathf.Abs(FirstAngle) < Mathf.PI && Intersection.CalculateSingle(firstTrajectory, connectEnds, out var firstT, out _) && firstT < FirstOffset)
                FirstAngle = FirstAngle - Mathf.Sign(FirstAngle) * 2 * Mathf.PI;
            if (Mathf.Abs(SecondAngle) < Mathf.PI && Intersection.CalculateSingle(secondTrajectory, connectEnds, out var secondT, out _) && secondT < SecondOffset)
                SecondAngle = SecondAngle - Mathf.Sign(SecondAngle) * 2 * Mathf.PI;
        }
        private IEnumerable<Point> GetParts(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, StraightTrajectory centerConnection)
        {
            if (FirstOffset >= 8f)
            {
                var offset = firstTrajectory.Cut(0f, FirstOffset);
                foreach (var point in GetStraightParts(offset))
                    yield return point;

                yield return new Point(firstTrajectory.Position(FirstOffset), firstTrajectory.Direction);
            }

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

            foreach (var point in GetCurveParts(SecondCenter, SecondEndCurve - SecondCenter, centerConnection.Direction, SecondRadius.Value, -SecondAngle))
                yield return point;

            if (SecondOffset >= 8f)
            {
                yield return new Point(secondTrajectory.Position(SecondOffset), -secondTrajectory.Direction);

                var offset = secondTrajectory.Cut(SecondOffset, 0f);
                foreach (var point in GetStraightParts(offset))
                    yield return point;
            }
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
        protected override void IncreaseRadius()
        {
            FirstRadius = ChangeRadius(FirstRadius, true);
            SecondRadius = ChangeRadius(SecondRadius, true);
            State = Result.None;
        }
        protected override void DecreaseRadius()
        {
            FirstRadius = ChangeRadius(FirstRadius, false);
            SecondRadius = ChangeRadius(SecondRadius, false);
            State = Result.None;
        }
        private void IncreaseOneRadius()
        {
            if (SelectRadius)
                FirstRadius = ChangeRadius(FirstRadius, true);
            else
                SecondRadius = ChangeRadius(SecondRadius, true);
            State = Result.None;
        }
        private void DecreaseOneRadius()
        {
            if (SelectRadius)
                FirstRadius = ChangeRadius(FirstRadius, false);
            else
                SecondRadius = ChangeRadius(SecondRadius, false);
            State = Result.None;
        }
        private float? ChangeRadius(float? radius, bool increase)
        {
            if (radius != null)
            {
                var step = Step;
                return (radius.Value + (increase ? step : -step)).RoundToNearest(step);
            }
            else
                return radius;
        }
        private void SwitchSelectRadius() => SelectRadius = !SelectRadius;

        private void IncreaseOffset()
        {
            if (SelectOffset)
                FirstOffset = ChangeOffset(FirstOffset, true);
            else
                SecondOffset = ChangeOffset(SecondOffset, true);
            State = Result.None;
        }
        private void DecreaseOffset()
        {
            if (SelectOffset)
                FirstOffset = ChangeOffset(FirstOffset, false);
            else
                SecondOffset = ChangeOffset(SecondOffset, false);
            State = Result.None;
        }
        private float ChangeOffset(float offset, bool increase)
        {
            var step = Step;
            return Mathf.Clamp((offset + (increase ? step : -step)).RoundToNearest(step), 0f, 500f);
        }
        private void SwitchSelectOffset() => SelectOffset = !SelectOffset;

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            //FirstCenter.RenderCircle(new OverlayData(cameraInfo) { Width = FirstRadius.Value * 2f, Color = Color.red });
            //SecondCenter.RenderCircle(new OverlayData(cameraInfo) { Width = SecondRadius.Value * 2f, Color = Color.red });

            //new StraightTrajectory(FirstStartCurve, SecondStartCurve).Render(new OverlayData(cameraInfo) { Color = Color.black });
            //new StraightTrajectory(FirstCenter, SecondCenter).Render(new OverlayData(cameraInfo) { Color = Color.black });

            RenderCenter(cameraInfo, info, FirstCenter, FirstStartCurve, FirstEndCurve, FirstRadius.Value);
            RenderCenter(cameraInfo, info, SecondCenter, SecondStartCurve, SecondEndCurve, SecondRadius.Value);
            (SelectRadius ? FirstCenter : SecondCenter).RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = Underground }, 7f, 5f);

            RenderScale(cameraInfo, FirstStart, FirstStartCurve, (FirstStartCurve - FirstCenter).normalized, info, SelectOffset ? Colors.Yellow : Colors.White);
            RenderScale(cameraInfo, SecondStart, SecondStartCurve, (SecondStartCurve - SecondCenter).normalized, info, !SelectOffset ? Colors.Yellow : Colors.White);

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

                (SelectRadius ? FirstCenter : SecondCenter).RenderCircle(new OverlayData(cameraInfo) { RenderLimit = Underground }, 7f, 5f);
            }
        }
    }
}
