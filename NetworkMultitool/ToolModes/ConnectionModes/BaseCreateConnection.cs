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
    public abstract class BaseCreateConnectionMode : BaseCreateMode
    {
        protected int Count => 2;
        public int SelectCircle { get; protected set; }
        public int SelectOffset { get; protected set; }
        public List<bool?> Side { get; private set; }
        public List<float?> Radius { get; private set; }
        public List<float> Offset { get; private set; }

        public List<Vector3> StartPos { get; private set; }
        public List<Vector3> StartDir { get; private set; }
        public List<Vector3> CenterPos { get; private set; }
        public List<Vector3> CenterDir { get; private set; }
        public List<Vector3> CurveStart { get; private set; }
        public List<Vector3> CurveEnd { get; private set; }
        public List<float> Angle { get; private set; }

        public List<InfoLabel> RadiusLabel { get; private set; }
        public List<InfoLabel> OffsetLabel { get; private set; }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is BaseCreateConnectionMode connectionMode)
            {
                First = connectionMode.First;
                Second = connectionMode.Second;
                IsFirstStart = connectionMode.IsFirstStart;
                IsSecondStart = connectionMode.IsSecondStart;

                SelectCircle = connectionMode.SelectCircle;
                SelectOffset = connectionMode.SelectOffset;
                Side = connectionMode.Side;
                Radius = connectionMode.Radius;
                Offset = connectionMode.Offset;
            }

            RadiusLabel = new List<InfoLabel>() { AddLabel(), AddLabel() };
            OffsetLabel = new List<InfoLabel>() { AddLabel(), AddLabel() };
        }
        protected override void ResetParams()
        {
            base.ResetParams();

            StartPos = new List<Vector3>() { Vector3.zero, Vector3.zero };
            StartDir = new List<Vector3>() { Vector3.zero, Vector3.zero };
            CenterPos = new List<Vector3>() { Vector3.zero, Vector3.zero };
            CenterDir = new List<Vector3>() { Vector3.zero, Vector3.zero };
            CurveStart = new List<Vector3>() { Vector3.zero, Vector3.zero };
            CurveEnd = new List<Vector3>() { Vector3.zero, Vector3.zero };
            Angle = new List<float>() { 0f, 0f };

            Radius = new List<float?>() { null, null };
            Side = new List<bool?>() { null, null };
            Offset = new List<float>() { 0f, 0f };
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();
            Setlabels();
        }
        private void Setlabels()
        {
            for (var i = 0; i < Count; i += 1)
            {
                if (State == Result.Calculated)
                {
                    var info = Info;

                    RadiusLabel[i].isVisible = true;
                    RadiusLabel[i].text = $"{GetRadiusString(Radius[i].Value)}\n{GetAngleString(Mathf.Abs(Angle[i]))}";
                    RadiusLabel[i].Direction = Mathf.Abs(Angle[i]) <= Mathf.PI ? CenterDir[i] : -CenterDir[i];
                    RadiusLabel[i].WorldPosition = CenterPos[i] + RadiusLabel[i].Direction * 5f;

                    OffsetLabel[i].isVisible = true;
                    OffsetLabel[i].text = GetRadiusString(Offset[i]);
                    OffsetLabel[i].Direction = (CurveStart[i] - CenterPos[i]).normalized;
                    OffsetLabel[i].WorldPosition = (StartPos[i] + CurveStart[i]) / 2f + OffsetLabel[i].Direction * (info.m_halfWidth + 7f);
                }
                else
                {
                    RadiusLabel[i].isVisible = false;
                    OffsetLabel[i].isVisible = false;
                }
            }
        }

        protected override IEnumerable<Point> Calculate(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory)
        {
            StartPos[0] = firstTrajectory.StartPosition;
            StartPos[1] = secondTrajectory.StartPosition;
            StartDir[0] = firstTrajectory.Direction;
            StartDir[1] = secondTrajectory.Direction;
            CurveStart[0] = firstTrajectory.Position(Offset[0]);
            CurveStart[1] = secondTrajectory.Position(Offset[1]);

            GetSides(firstTrajectory, secondTrajectory);

            var firstStartRadiusDir = -firstTrajectory.Direction.Turn90(Side[0].Value);
            var secondStartRadiusDir = -secondTrajectory.Direction.Turn90(Side[1].Value);

            GetRadii(firstStartRadiusDir, secondStartRadiusDir);

            var centerConnect = new StraightTrajectory(CenterPos[0].MakeFlat(), CenterPos[1].MakeFlat());
            GetAngles(centerConnect, firstStartRadiusDir, secondStartRadiusDir);

            var firstEndRadiusDir = firstStartRadiusDir.TurnRad(Angle[0], true);
            var secondEndRadiusDir = secondStartRadiusDir.TurnRad(Angle[1], true);

            CurveEnd[0] = CenterPos[0] + firstEndRadiusDir * Radius[0].Value;
            CurveEnd[1] = CenterPos[1] + secondEndRadiusDir * Radius[1].Value;

            if (!CheckRadii(centerConnect))
                return new Point[0];

            var connectEnds = new StraightTrajectory(CurveEnd[0].MakeFlat(), CurveEnd[1].MakeFlat(), false);
            FixAngles(firstTrajectory, secondTrajectory, connectEnds);

            CenterDir[0] = (connectEnds.Direction - firstTrajectory.Direction).normalized;
            CenterDir[1] = (-connectEnds.Direction - secondTrajectory.Direction).normalized;

            State = Result.Calculated;
            return GetParts(firstTrajectory, secondTrajectory, connectEnds);
        }
        private void GetSides(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory)
        {
            if (Side[0] != null && Side[1] != null)
                return;

            var connect = new StraightTrajectory(CurveStart[0].MakeFlat(), CurveStart[1].MakeFlat());

            Side[0] = CrossXZ(firstTrajectory.Direction, connect.Direction) >= 0;
            Side[1] = CrossXZ(secondTrajectory.Direction, -connect.Direction) >= 0;
            var firstDot = DotXZ(firstTrajectory.Direction, connect.Direction) >= 0;
            var secondDot = DotXZ(secondTrajectory.Direction, -connect.Direction) >= 0;

            if (firstDot != secondDot && Side[0] != Side[1])
            {
                if (!firstDot)
                    Side[0] = !Side[0];
                else if (!secondDot)
                    Side[1] = !Side[1];
            }
        }
        private void GetRadii(Vector3 firstRadiusDir, Vector3 secondRadiusDir)
        {
            Radius[0] = Mathf.Clamp(Radius[0] ?? 50f, MinPossibleRadius, 1000f);
            Radius[1] = Mathf.Clamp(Radius[1] ?? 50f, MinPossibleRadius, 1000f);

            CenterPos[0] = CurveStart[0] - firstRadiusDir * Radius[0].Value;
            CenterPos[1] = CurveStart[1] - secondRadiusDir * Radius[1].Value;
        }
        private void GetAngles(StraightTrajectory centerConnect, Vector3 firstRadiusDir, Vector3 secondRadiusDir)
        {
            if (Side[0] == Side[1])
            {
                var delta = centerConnect.Length / (Radius[0].Value + Radius[1].Value) * Radius[0].Value;
                var deltaAngle = Mathf.Acos(Radius[0].Value / delta);

                Angle[0] = GetAngle(firstRadiusDir, centerConnect.Direction, deltaAngle, Side[0].Value);
                Angle[1] = GetAngle(secondRadiusDir, -centerConnect.Direction, deltaAngle, Side[1].Value);
            }
            else
            {
                var deltaAngle = Mathf.Asin(Mathf.Abs(Radius[0].Value - Radius[1].Value) / centerConnect.Length);

                Angle[0] = GetAngle(firstRadiusDir, centerConnect.Direction, Mathf.PI / 2f + Mathf.Sign(Radius[1].Value - Radius[0].Value) * deltaAngle, Side[0].Value);
                Angle[1] = GetAngle(secondRadiusDir, -centerConnect.Direction, Mathf.PI / 2f + Mathf.Sign(Radius[0].Value - Radius[1].Value) * deltaAngle, Side[1].Value);
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
            if (Side[0].Value == Side[1].Value)
            {
                if (centerConnect.Length < Radius[0].Value + Radius[1].Value)
                {
                    State = Result.BigRadius;
                    return false;
                }
            }
            else
            {
                if (centerConnect.Length + Radius[0].Value < Radius[1].Value || centerConnect.Length + Radius[1].Value < Radius[0].Value)
                {
                    State = Result.WrongShape;
                    return false;
                }
            }
            return true;
        }
        private void FixAngles(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, StraightTrajectory connectEnds)
        {
            if (Mathf.Abs(Angle[0]) < Mathf.PI && Intersection.CalculateSingle(firstTrajectory, connectEnds, out var firstT, out _) && firstT < Offset[0])
                Angle[0] = Angle[0] - Mathf.Sign(Angle[0]) * 2 * Mathf.PI;
            if (Mathf.Abs(Angle[1]) < Mathf.PI && Intersection.CalculateSingle(secondTrajectory, connectEnds, out var secondT, out _) && secondT < Offset[1])
                Angle[1] = Angle[1] - Mathf.Sign(Angle[1]) * 2 * Mathf.PI;
        }
        private IEnumerable<Point> GetParts(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, StraightTrajectory centerConnection)
        {
            if (Offset[0] >= 8f)
            {
                var offset = firstTrajectory.Cut(0f, Offset[0]);
                foreach (var point in GetStraightParts(offset))
                    yield return point;

                yield return new Point(firstTrajectory.Position(Offset[0]), firstTrajectory.Direction);
            }

            foreach (var point in GetCurveParts(CenterPos[0], CurveStart[0] - CenterPos[0], firstTrajectory.Direction, Radius[0].Value, Angle[0]))
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

            foreach (var point in GetCurveParts(CenterPos[1], CurveEnd[1] - CenterPos[1], centerConnection.Direction, Radius[1].Value, -Angle[1]))
                yield return point;

            if (Offset[1] >= 8f)
            {
                yield return new Point(secondTrajectory.Position(Offset[1]), -secondTrajectory.Direction);

                var offset = secondTrajectory.Cut(Offset[1], 0f);
                foreach (var point in GetStraightParts(offset))
                    yield return point;
            }
        }

        protected override void SetFirstNode(ref NetSegment segment, ushort nodeId)
        {
            base.SetFirstNode(ref segment, nodeId);
            if (Side[0].HasValue)
                Side[0] = !Side[0];
        }
        protected override void SetSecondNode(ref NetSegment segment, ushort nodeId)
        {
            base.SetSecondNode(ref segment, nodeId);
            if (Side[1].HasValue)
                Side[1] = !Side[1];
        }

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            for (var i = 0; i < Count; i += 1)
            {
                RenderCenter(cameraInfo, info, CenterPos[i], CurveStart[i], CurveEnd[i], Radius[i].Value);
                RenderScale(cameraInfo, StartPos[i], CurveStart[i], (CurveStart[i] - CenterPos[i]).normalized, info, SelectOffset == i ? Colors.Yellow : Colors.White);
            }
        }
        protected override void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            if (State == Result.BigRadius || State == Result.SmallRadius || State == Result.WrongShape)
            {
                for (var i = 0; i < Count; i += 1)
                {
                    RenderRadius(cameraInfo, info, CenterPos[i], CurveStart[i], Radius[i].Value, Colors.Red);
                    RenderCenter(cameraInfo, CenterPos[i], Colors.Red);
                }
            }
        }
        protected void RenderCenter(RenderManager.CameraInfo cameraInfo, int i, Color32 color) => CenterPos[i].RenderCircle(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground }, 7f, 5f);
        protected void RenderCircle(RenderManager.CameraInfo cameraInfo, int i, Color32 color) => CenterPos[i].RenderCircle(new OverlayData(cameraInfo) { Width = Radius[i].Value * 2f, Color = color, RenderLimit = Underground });
    }
    public abstract class BaseAdditionalCreateConnectionMode : BaseCreateConnectionMode
    {
        public int Edit { get; protected set; }
        protected bool IsEdit => Edit != -1;

        public override void OnMouseUp(Event e)
        {
            Tool.SetMode(ToolModeType.CreateConnection);
        }

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            for (var i = 0; i < Count; i += 1)
                RenderCircle(cameraInfo, i, i == Edit ? Colors.Green : Colors.Green.SetAlpha(64));

            base.RenderCalculatedOverlay(cameraInfo, info);
        }
        protected override void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            for (var i = 0; i < Count; i += 1)
                RenderCircle(cameraInfo, i, Colors.Red);

            base.RenderFailedOverlay(cameraInfo, info);
        }
    }
}
