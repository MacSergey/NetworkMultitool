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
    public class CreateLoopMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.CreateLoop;

        protected NetworkMultitoolShortcut Enter { get; }
        protected NetworkMultitoolShortcut Plus { get; }
        protected NetworkMultitoolShortcut Minus { get; }
        protected NetworkMultitoolShortcut Tab { get; }

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return Enter;
                yield return Plus;
                yield return Minus;
                yield return Tab;
            }
        }

        public CreateLoopMode()
        {
            Enter = new NetworkMultitoolShortcut(nameof(Enter), string.Empty, SavedInputKey.Encode(KeyCode.Return, false, false, false), PressEnter, ToolModeType.CreateLoop);
            Plus = new NetworkMultitoolShortcut(nameof(Enter), string.Empty, SavedInputKey.Encode(KeyCode.Equals, false, false, false), PressPlus, ToolModeType.CreateLoop) { CanRepeat = true};
            Minus = new NetworkMultitoolShortcut(nameof(Enter), string.Empty, SavedInputKey.Encode(KeyCode.Minus, false, false, false), PressMinus, ToolModeType.CreateLoop) { CanRepeat = true };
            Tab = new NetworkMultitoolShortcut(nameof(Enter), string.Empty, SavedInputKey.Encode(KeyCode.Tab, false, false, false), PressTab, ToolModeType.CreateLoop);
        }

        protected override bool IsValidSegment(ushort segmentId) => !IsBoth && segmentId != First?.Id && segmentId != Second?.Id;
        protected override bool IsValidNode(ushort nodeId) => IsBoth && (First.Id.GetSegment().Contains(nodeId) || Second.Id.GetSegment().Contains(nodeId));

        private SegmentSelection First { get; set; }
        private SegmentSelection Second { get; set; }
        private bool IsFirstStart { get; set; }
        private bool IsSecondStart { get; set; }

        private bool IsFirst => First != null;
        private bool IsSecond => Second != null;
        private bool IsBoth => IsFirst && IsSecond;


        private Result State { get; set; }

        private float MinRadius { get; set; }
        private float MaxRadius { get; set; }
        private float? Radius { get; set; }
        private bool IsLoop { get; set; }
        private List<Point> Points { get; } = new List<Point>();
        private NetInfo Info => ToolsModifierControl.toolController.Tools.OfType<NetTool>().FirstOrDefault().Prefab?.m_netAI.m_info ?? First.Id.GetSegment().Info;
        private float MinPossibleRadius => Info != null ? Info.m_halfWidth + 5f : 16f;

        public override string GetToolInfo()
        {
            if (!IsFirst)
                return "Select first segment" + GetStepOverInfo();
            else if (!IsSecond)
                return "Select second segment" + GetStepOverInfo();
            else if (State != Result.Calculated)
                return "Choose nodes to select create direction";
            else
                return $"Radius: {Radius.Value}\nPress Minus to decrease radius\nPress Plus to increase radius\nPress Enter to create\nPress Tab to change loop";
        }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            First = null;
            Second = null;
            State = Result.None;
            IsFirstStart = true;
            IsSecondStart = true;
            IsLoop = false;
            Points.Clear();
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsBoth && State == Result.None)
            {
                Calculate();
            }
        }
        private void Calculate()
        {
            var firstSegment = First.Id.GetSegment();
            var secondSegment = Second.Id.GetSegment();

            var firstPos = (IsFirstStart ? firstSegment.m_startNode : firstSegment.m_endNode).GetNode().m_position;
            var secondPos = (IsSecondStart ? secondSegment.m_startNode : secondSegment.m_endNode).GetNode().m_position;
            var firstDir = -(IsFirstStart ? firstSegment.m_startDirection : firstSegment.m_endDirection).MakeFlatNormalized();
            var secondDir = -(IsSecondStart ? secondSegment.m_startDirection : secondSegment.m_endDirection).MakeFlatNormalized();

            var firstTrajectory = new StraightTrajectory(firstPos, firstPos + firstDir, false);
            var secondTrajectory = new StraightTrajectory(secondPos, secondPos + secondDir, false);

            if (!Intersection.CalculateSingle(firstTrajectory, secondTrajectory, out var firtsT, out var secondT))
            {
                State = Result.NotIntersect;
                return;
            }

            Points.Clear();
            Points.Add(new Point(firstPos, firstDir));
                  
            var angle = GetAngle(firstDir, secondDir);
            var halfAbsAngle = Mathf.Abs(angle) / 2f;

            var direct = firtsT >= 0f && secondT >= 0f && !IsLoop;
            if (direct)
            {
                MinRadius = MinPossibleRadius;
                MaxRadius = Mathf.Tan(halfAbsAngle) * Mathf.Min(firtsT, secondT);
            }
            else
            {
                MinRadius = Mathf.Max(Mathf.Tan(halfAbsAngle) * Mathf.Max(-Mathf.Min(firtsT, 0f), -Mathf.Min(secondT, 0f)), MinPossibleRadius);
                MaxRadius = Mathf.Max(MinRadius + 200f, 500f);
            }
            Radius = Mathf.Clamp(Radius ?? 50f, MinRadius, MaxRadius);

            var delta = Radius.Value / Mathf.Tan(halfAbsAngle);
            var startLenght = direct ? firtsT - delta : firtsT + delta;
            var endLenght = direct ? secondT - delta : secondT + delta;
            //var startLenght = firtsT >= 0 && !IsLoop ? firtsT - delta : firtsT + delta;
            //var endLenght = secondT >= 0 && !IsLoop ? secondT - delta : secondT + delta;

            var sign = direct ? -1 : 1;
            var intersect = (firstTrajectory.Position(firtsT) + secondTrajectory.Position(secondT)) / 2f;
            var dir = (firstDir + secondDir).normalized;
            var distant = Radius.Value / Mathf.Sin(halfAbsAngle);
            var center = intersect + sign * dir * distant;

            angle = -sign * Mathf.Sign(angle) * (Mathf.PI + sign * Mathf.Abs(angle));

            Calculate(firstTrajectory, secondTrajectory, startLenght, endLenght, center, angle);

            Points.Add(new Point(secondPos, -secondDir));
            State = Result.Calculated;
        }
        private void Calculate(StraightTrajectory startTrajectory, StraightTrajectory endTrajectory, float startLenght, float endLenght, Vector3 center, float angle)
        {
            if (startLenght >= 8f)
            {
                var count = Mathf.CeilToInt(startLenght / 80f);
                var partLenght = startLenght / count;
                for (var i = 1; i < count + 1; i += 1)
                {
                    var point = new Point(startTrajectory.Position(partLenght * i), startTrajectory.Tangent(partLenght * i));
                    Points.Add(point);
                }
            }

            var startCurve = startTrajectory.Position(startLenght);

            var curveLenght = Radius.Value * Mathf.Abs(angle);
            var curveCount = curveLenght < 50f ? 1 : Math.Max(Mathf.CeilToInt(curveLenght / 80f), Mathf.CeilToInt(Mathf.Abs(angle) / Mathf.PI * 3));

            var direction = startCurve - center;
            for (var i = 1; i < curveCount; i += 1)
            {
                var deltaAngle = angle / curveCount * i;
                var point = new Point(center + direction.TurnRad(deltaAngle, true), startTrajectory.Direction.TurnRad(deltaAngle, true));
                Points.Add(point);
            }

            if (endLenght >= 8f)
            {
                var count = Mathf.CeilToInt(endLenght / 80f);
                var partLenght = endLenght / count;
                for (var i = count; i > 0; i -= 1)
                {
                    var point = new Point(endTrajectory.Position(partLenght * i), -endTrajectory.Tangent(partLenght * i));
                    Points.Add(point);
                }
            }
        }

        private float GetAngle(Vector3 cornerDir, Vector3 segmentDir)
        {
            var first = NormalizeXZ(cornerDir);
            var second = NormalizeXZ(segmentDir);

            var sign = -Mathf.Sign(CrossXZ(first, second));
            var angle = Mathf.Acos(Mathf.Clamp(DotXZ(first, second), -1f, 1f));

            return sign * angle;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (!IsFirst)
            {
                if (IsHoverSegment)
                    First = HoverSegment;
            }
            else if (!IsSecond)
            {
                if (IsHoverSegment)
                    Second = HoverSegment;
            }
            else if (IsHoverNode)
            {
                var firstSegment = First.Id.GetSegment();
                var secondSegment = Second.Id.GetSegment();

                if (firstSegment.Contains(HoverNode.Id))
                {
                    IsFirstStart = firstSegment.IsStartNode(HoverNode.Id);
                    Radius = null;
                    State = Result.None;
                }
                else if (secondSegment.Contains(HoverNode.Id))
                {
                    IsSecondStart = secondSegment.IsStartNode(HoverNode.Id);
                    Radius = null;
                    State = Result.None;
                }
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsSecond)
            {
                Second = null;
                State = Result.None;
            }
            else if (IsFirst)
            {
                First = null;
                State = Result.None;
            }
            else
                base.OnSecondaryMouseClicked();
        }
        private void PressEnter()
        {
            if (State == Result.Calculated && Info is NetInfo info)
            {
                var nodeIds = new List<ushort>();

                nodeIds.Add(IsFirstStart ? First.Id.GetSegment().m_startNode : First.Id.GetSegment().m_endNode);
                for (var i = 1; i < Points.Count - 1; i += 1)
                {
                    CreateNode(out var newNodeId, info, Points[i].Position);
                    nodeIds.Add(newNodeId);
                }
                nodeIds.Add(IsSecondStart ? Second.Id.GetSegment().m_startNode : Second.Id.GetSegment().m_endNode);

                for (var i = 1; i < nodeIds.Count; i += 1)
                    CreateSegment(out _, info, nodeIds[i - 1], nodeIds[i], Points[i - 1].Direction, -Points[i].Direction);

                Tool.SetSlope(nodeIds.ToArray());

                Reset(this);
            }
        }
        private float Step => Utility.OnlyShiftIsPressed ? 100f : (Utility.OnlyCtrlIsPressed ? 1f : 10f);
        private void PressPlus()
        {
            if (Radius != null)
            {
                var step = Step;
                Radius = Mathf.Min((Radius.Value + step).RoundToNearest(step), MaxRadius);
                State = Result.None;
            }
        }
        private void PressMinus()
        {
            if (Radius != null)
            {
                var step = Step;
                Radius = Mathf.Max((Radius.Value - step).RoundToNearest(step), MinRadius);
                State = Result.None;
            }
        }
        private void PressTab()
        {
            IsLoop = !IsLoop;
            Radius = null;
            State = Result.None;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (IsFirst)
                First.Render(new OverlayData(cameraInfo) { RenderLimit = Underground });
            if (IsSecond)
                Second.Render(new OverlayData(cameraInfo) { RenderLimit = Underground });

            if (State == Result.Calculated)
            {
                var data = new OverlayData(cameraInfo) { Color = Colors.Yellow, Width = 16f, Cut = true };
                for (var i = 1; i < Points.Count; i += 1)
                {
                    var trajectory = new BezierTrajectory(Points[i - 1].Position, Points[i - 1].Direction, Points[i].Position, -Points[i].Direction);
                    trajectory.Render(data);
                }
            }
        }

        private enum Result
        {
            None,
            NotIntersect,
            SmallRadius,
            BigRadius,
            Calculated,
        }
        private struct Point
        {
            public Vector3 Position;
            public Vector3 Direction;

            public Point(Vector3 position, Vector3 direction)
            {
                Position = position;
                Direction = direction;
            }
        }
    }
}
