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
    public abstract class BaseCreateMode : BaseNetworkMultitoolMode
    {
        protected NetworkMultitoolShortcut Enter { get; }
        protected NetworkMultitoolShortcut Plus { get; }
        protected NetworkMultitoolShortcut LargePlus { get; }
        protected NetworkMultitoolShortcut SmallPlus { get; }
        protected NetworkMultitoolShortcut Minus { get; }
        protected NetworkMultitoolShortcut LargeMinus { get; }
        protected NetworkMultitoolShortcut SmallMinus { get; }

        protected override Color32 SegmentColor => Colors.Blue;
        protected override Color32 NodeColor => Colors.Green;

        protected override bool IsValidSegment(ushort segmentId) => !IsBoth && segmentId != First?.Id && segmentId != Second?.Id;
        protected override bool IsValidNode(ushort nodeId) => IsBoth && (First.Id.GetSegment().Contains(nodeId) || Second.Id.GetSegment().Contains(nodeId));

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return Enter;

                yield return Plus;
                yield return LargePlus;
                yield return SmallPlus;

                yield return Minus;
                yield return LargeMinus;
                yield return SmallMinus;
            }
        }
        public BaseCreateMode()
        {
            Enter = GetShortcut(KeyCode.Return, PressEnter, ToolModeType.Create);

            Plus = GetShortcut(KeyCode.Equals, PressPlus, ToolModeType.Create, repeat: true);
            LargePlus = GetShortcut(KeyCode.Equals, PressPlus, ToolModeType.Create, shift: true, repeat: true);
            SmallPlus = GetShortcut(KeyCode.Equals, PressPlus, ToolModeType.Create, ctrl: true, repeat: true);

            Minus = GetShortcut(KeyCode.Minus, PressMinus, ToolModeType.Create, repeat: true);
            LargeMinus = GetShortcut(KeyCode.Minus, PressMinus, ToolModeType.Create, shift: true, repeat: true);
            SmallMinus = GetShortcut(KeyCode.Minus, PressMinus, ToolModeType.Create, ctrl: true, repeat: true);
        }

        protected SegmentSelection First { get; set; }
        protected SegmentSelection Second { get; set; }
        protected bool IsFirstStart { get; set; }
        protected bool IsSecondStart { get; set; }

        protected bool IsFirst => First != null;
        protected bool IsSecond => Second != null;
        protected bool IsBoth => IsFirst && IsSecond;

        protected Result State { get; set; }

        private List<Point> Points { get; set; } = new List<Point>();
        protected NetInfo Info => ToolsModifierControl.toolController.Tools.OfType<NetTool>().FirstOrDefault().Prefab?.m_netAI?.m_info ?? First.Id.GetSegment().Info;
        protected float MinPossibleRadius => Info != null ? Info.m_halfWidth * 2f : 16f;

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            First = null;
            Second = null;
            State = Result.None;
        }
        protected virtual void ResetParams()
        {
            State = Result.None;

            IsFirstStart = true;
            IsSecondStart = true;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsBoth && State == Result.None)
            {
                var firstSegment = First.Id.GetSegment();
                var secondSegment = Second.Id.GetSegment();

                var firstPos = (IsFirstStart ? firstSegment.m_startNode : firstSegment.m_endNode).GetNode().m_position;
                var secondPos = (IsSecondStart ? secondSegment.m_startNode : secondSegment.m_endNode).GetNode().m_position;
                var firstDir = -(IsFirstStart ? firstSegment.m_startDirection : firstSegment.m_endDirection).MakeFlatNormalized();
                var secondDir = -(IsSecondStart ? secondSegment.m_startDirection : secondSegment.m_endDirection).MakeFlatNormalized();

                var firstTrajectory = new StraightTrajectory(firstPos, firstPos + firstDir, false);
                var secondTrajectory = new StraightTrajectory(secondPos, secondPos + secondDir, false);

                Points = Calculate(firstTrajectory, secondTrajectory).ToList();
                Points.Insert(0, new Point(firstTrajectory.StartPosition, firstTrajectory.Direction));
                Points.Add(new Point(secondTrajectory.StartPosition, -secondTrajectory.Direction));
            }
        }
        protected abstract IEnumerable<Point> Calculate(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory);
        protected static IEnumerable<Point> GetCurveParts(Vector3 center, Vector3 radiusDir, Vector3 dir, float radius, float angle)
        {
            var curveLenght = radius * Mathf.Abs(angle);

            var minByLenght = Mathf.CeilToInt(curveLenght / 50f);
            var maxByLenght = Mathf.CeilToInt(curveLenght / 80f);
            var maxByAngle = Mathf.CeilToInt(Mathf.Abs(angle) / Mathf.PI * 3);

            var curveCount = Math.Max(maxByLenght, Mathf.Min(minByLenght, maxByAngle));

            for (var i = 1; i < curveCount; i += 1)
            {
                var deltaAngle = angle / curveCount * i;
                var point = new Point(center + radiusDir.TurnRad(deltaAngle, true), dir.TurnRad(deltaAngle, true));
                yield return point;
            }
        }
        protected static IEnumerable<Point> GetStraightParts(StraightTrajectory straight)
        {
            var lenght = straight.Length;
            var count = Mathf.CeilToInt(lenght / 80f);
            for (var i = 1; i < count; i += 1)
            {
                var point = new Point(straight.Position(1f / count * i), straight.Direction);
                yield return point;
            }
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
                    State = Result.None;
                }
                else if (secondSegment.Contains(HoverNode.Id))
                {
                    IsSecondStart = secondSegment.IsStartNode(HoverNode.Id);
                    State = Result.None;
                }
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsSecond)
            {
                Second = null;
                ResetParams();
            }
            else if (IsFirst)
            {
                First = null;
                ResetParams();
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
        protected abstract void PressPlus();
        protected abstract void PressMinus();

        protected float Step => Utility.OnlyShiftIsPressed ? 100f : (Utility.OnlyCtrlIsPressed ? 1f : 10f);

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            var color = State switch
            {
                Result.BigRadius or Result.SmallRadius or Result.WrongShape => Colors.Red,
                _ => Colors.White,
            };

            if (IsFirst)
                First.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
            if (IsSecond)
                Second.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });

            var info = Info;
            if (State == Result.Calculated)
            {
                var data = new OverlayData(cameraInfo) { Color = Colors.Yellow, Width = info.m_halfWidth * 2f, Cut = true };

                for (var i = 1; i < Points.Count; i += 1)
                {
                    var trajectory = new BezierTrajectory(Points[i - 1].Position, Points[i - 1].Direction, Points[i].Position, -Points[i].Direction);
                    trajectory.Render(data);
                }

                RenderCalculatedOverlay(cameraInfo, info);
            }
            else if (State != Result.None)
                RenderFailedOverlay(cameraInfo, info);
        }
        protected virtual void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info) { }
        protected virtual void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info) { }
        protected void RenderCenter(RenderManager.CameraInfo cameraInfo, NetInfo info, Vector3 center, Vector3 startCurve, Vector3 endCurve, float radius)
        {
            RenderRadius(cameraInfo, info, center, startCurve, radius, Colors.Yellow);
            RenderRadius(cameraInfo, info, center, endCurve, radius, Colors.Yellow);
            RenderCenter(cameraInfo, center, Colors.Yellow);
        }
        protected void RenderRadius(RenderManager.CameraInfo cameraInfo, NetInfo info, Vector3 center, Vector3 curve, float radius, Color color)
        {
            var startBezier = new StraightTrajectory(curve, center).Cut(info.m_halfWidth / radius, 1f);
            startBezier.Render(new OverlayData(cameraInfo) { Color = color });
        }
        protected void RenderCenter(RenderManager.CameraInfo cameraInfo, Vector3 center, Color color)
        {
            center.RenderCircle(new OverlayData(cameraInfo) { Color = color }, 5f, 0f);
        }

        protected enum Result
        {
            None,
            NotIntersect,
            SmallRadius,
            BigRadius,
            WrongShape,
            Calculated,
        }
        protected struct Point
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
