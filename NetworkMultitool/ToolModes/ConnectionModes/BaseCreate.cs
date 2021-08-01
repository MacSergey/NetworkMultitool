using ColossalFramework;
using ColossalFramework.UI;
using HarmonyLib;
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
        protected static Func<float> MaxLengthGetter { get; private set; }
        protected override bool IsReseted => !IsFirst;
        protected override bool CanSwitchUnderground => !IsBoth;

        protected NetworkMultitoolShortcut IncreaseRadiusShortcut { get; }
        protected NetworkMultitoolShortcut DecreaseRadiusShortcut { get; }
        protected NetworkMultitoolShortcut IncreaseRadiusNumPadShortcut { get; }
        protected NetworkMultitoolShortcut DecreaseRadiusNumPadShortcut { get; }


        protected override Color32 SegmentColor => Colors.Blue;
        protected override Color32 NodeColor => Colors.Green;

        protected override bool IsValidSegment(ushort segmentId) => !IsBoth && segmentId != First?.Id && segmentId != Second?.Id;
        protected override bool IsValidNode(ushort nodeId) => base.IsValidNode(nodeId) && IsBoth && (First.Id.GetSegment().Contains(nodeId) || Second.Id.GetSegment().Contains(nodeId));

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return ApplyShortcut;
                yield return IncreaseRadiusShortcut;
                yield return DecreaseRadiusShortcut;
                yield return IncreaseRadiusNumPadShortcut;
                yield return DecreaseRadiusNumPadShortcut;
            }
        }
        public BaseCreateMode()
        {
            IncreaseRadiusShortcut = GetShortcut(KeyCode.Equals, IncreaseRadius, ToolModeType.Create, repeat: true, ignoreModifiers: true);
            DecreaseRadiusShortcut = GetShortcut(KeyCode.Minus, DecreaseRadius, ToolModeType.Create, repeat: true, ignoreModifiers: true);
            IncreaseRadiusNumPadShortcut = GetShortcut(KeyCode.KeypadPlus, IncreaseRadius, ToolModeType.Create, repeat: true, ignoreModifiers: true);
            DecreaseRadiusNumPadShortcut = GetShortcut(KeyCode.KeypadMinus, DecreaseRadius, ToolModeType.Create, repeat: true, ignoreModifiers: true);

            if (Mod.IsNodeSpacer)
            {
                try
                {
                    var method = AccessTools.Method(System.Type.GetType("NodeSpacer.NT_CreateNode"), "GetMaxLength");
                    MaxLengthGetter = AccessTools.MethodDelegate<Func<float>>(method);
                    SingletonMod<Mod>.Logger.Debug("Segment length linked to Node Spacer");
                    return;
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error("Cant access to Node Spacer", error);
                }
            }
            MaxLengthGetter = () => Settings.SegmentLength;
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

            ResetParams();
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
            var maxByLenght = Mathf.CeilToInt(curveLenght / MaxLengthGetter());
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
            var count = Mathf.CeilToInt(lenght / MaxLengthGetter());
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
                ref var firstSegment = ref First.Id.GetSegment();
                ref var secondSegment = ref Second.Id.GetSegment();

                if (firstSegment.Contains(HoverNode.Id))
                    SetFirstNode(ref firstSegment, HoverNode.Id);
                else if (secondSegment.Contains(HoverNode.Id))
                    SetSecondNode(ref secondSegment, HoverNode.Id);
            }
        }
        protected virtual void SetFirstNode(ref NetSegment segment, ushort nodeId)
        {
            IsFirstStart = segment.IsStartNode(nodeId);
            State = Result.None;
        }
        protected virtual void SetSecondNode(ref NetSegment segment, ushort nodeId)
        {
            IsSecondStart = segment.IsStartNode(nodeId);
            State = Result.None;
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
        protected override void Apply()
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
        public void Recalculate() => State = Result.None;
        protected abstract void IncreaseRadius();
        protected abstract void DecreaseRadius();

        protected float Step
        {
            get
            {
                if (Utility.OnlyShiftIsPressed)
                    return 100f;
                else if (Utility.OnlyCtrlIsPressed)
                    return 1f;
                else if (Utility.OnlyAltIsPressed)
                    return 0.1f;
                else
                    return 10f;
            }
        }

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

            if (State == Result.Calculated)
            {
                var info = Info;
                RenderCalculatedOverlay(cameraInfo, info);

                var data = new OverlayData(cameraInfo) { Color = Colors.Yellow, Width = info.m_halfWidth * 2f, Cut = true, RenderLimit = Underground };
                for (var i = 1; i < Points.Count; i += 1)
                {
                    var trajectory = new BezierTrajectory(Points[i - 1].Position, Points[i - 1].Direction, Points[i].Position, -Points[i].Direction);
                    trajectory.Render(data);
                }
            }
            else if (State != Result.None)
                RenderFailedOverlay(cameraInfo, Info);
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
            startBezier.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
        }
        protected void RenderCenter(RenderManager.CameraInfo cameraInfo, Vector3 center, Color color)
        {
            center.RenderCircle(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground }, 5f, 0f);
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
