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

        public static NetworkMultitoolShortcut IncreaseRadiusShortcut { get; } = GetShortcut(KeyCode.Equals, nameof(IncreaseRadiusShortcut), nameof(Localize.Settings_Shortcut_IncreaseRadius), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as BaseCreateMode)?.IncreaseRadius(), ToolModeType.Create, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseRadiusShortcut { get; } = GetShortcut(KeyCode.Minus, nameof(DecreaseRadiusShortcut), nameof(Localize.Settings_Shortcut_DecreaseRadius), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as BaseCreateMode)?.DecreaseRadius(), ToolModeType.Create, repeat: true, ignoreModifiers: true);


        protected override Color32 SegmentColor => Colors.Blue;
        protected override Color32 NodeColor => Colors.Blue;

        protected override bool IsValidSegment(ushort segmentId) => !IsBoth && segmentId != First?.Id && segmentId != Second?.Id;
        protected override bool IsValidNode(ushort nodeId) => base.IsValidNode(nodeId) && IsBoth && (First.Id.GetSegment().Contains(nodeId) || Second.Id.GetSegment().Contains(nodeId));

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return ApplyShortcut;
                yield return IncreaseRadiusShortcut;
                yield return DecreaseRadiusShortcut;
            }
        }
        static BaseCreateMode()
        {
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

        protected StraightTrajectory FirstTrajectory { get; set; }
        protected StraightTrajectory SecondTrajectory { get; set; }

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
                Points = Calculate().ToList();
                Points.Insert(0, new Point(FirstTrajectory.StartPosition, FirstTrajectory.Direction));
                Points.Add(new Point(SecondTrajectory.StartPosition, -SecondTrajectory.Direction));
            }
        }
        protected abstract IEnumerable<Point> Calculate();

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
                {
                    Second = HoverSegment;
                    Init();
                }
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
        private void Init()
        {
            State = Result.None;

            var firstSegment = First.Id.GetSegment();
            var secondSegment = Second.Id.GetSegment();

            var firstPos = (IsFirstStart ? firstSegment.m_startNode : firstSegment.m_endNode).GetNode().m_position;
            var secondPos = (IsSecondStart ? secondSegment.m_startNode : secondSegment.m_endNode).GetNode().m_position;
            var firstDir = -(IsFirstStart ? firstSegment.m_startDirection : firstSegment.m_endDirection).MakeFlatNormalized();
            var secondDir = -(IsSecondStart ? secondSegment.m_startDirection : secondSegment.m_endDirection).MakeFlatNormalized();

            FirstTrajectory = new StraightTrajectory(firstPos, firstPos + firstDir, false);
            SecondTrajectory = new StraightTrajectory(secondPos, secondPos + secondDir, false);

            Init(FirstTrajectory, SecondTrajectory);
        }
        protected abstract void Init(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory);

        protected virtual void SetFirstNode(ref NetSegment segment, ushort nodeId)
        {
            if (IsFirstStart != segment.IsStartNode(nodeId))
            {
                IsFirstStart = !IsFirstStart;
                Init();
            }
        }
        protected virtual void SetSecondNode(ref NetSegment segment, ushort nodeId)
        {
            if (IsSecondStart != segment.IsStartNode(nodeId))
            {
                IsSecondStart = !IsSecondStart;
                Init();
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
        protected virtual void IncreaseRadius() { }
        protected virtual void DecreaseRadius() { }

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
                Result.BigRadius or Result.SmallRadius or Result.WrongShape or Result.NotIntersect => Colors.Red,
                Result.Calculated => Colors.White.SetAlpha(64),
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
                RenderParts(Points, cameraInfo, Colors.Yellow, info.m_halfWidth * 2f);
            }
            else if (State != Result.None)
            {
                RenderFailedOverlay(cameraInfo, Info);
                RenderParts(Points, cameraInfo);
            }
        }
        protected virtual void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info) { }
        protected virtual void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info) { }

        public enum Result
        {
            None,
            NotIntersect,
            SmallRadius,
            BigRadius,
            WrongShape,
            Calculated,
        }
        public class Circle
        {
            public virtual Vector3 CenterPos { get; set; }
            public virtual Vector3 StartRadiusDir { get; set; }
            public virtual Vector3 EndRadiusDir { get; set; }
            public virtual Direction Direction { get; set; }
            public virtual float Radius { get; set; } = 50f;
            public virtual float MinRadius { get; set; }
            public virtual float MaxRadius { get; set; }

            public bool _isCorrect = true;
            public bool IsCorrect
            {
                get => _isCorrect && MinRadius <= Radius && Radius <= MaxRadius;
                set => _isCorrect = value;
            }

            public virtual Vector3 StartPos => CenterPos + StartRadiusDir * Radius;
            public virtual Vector3 EndPos => CenterPos + EndRadiusDir * Radius;
            public virtual Vector3 StartDir => StartRadiusDir.Turn90(Direction == Direction.Right);
            public virtual Vector3 EndDir => EndRadiusDir.Turn90(Direction == Direction.Right);
            public Vector3 CenterDir => -(StartRadiusDir.MakeFlat() + EndRadiusDir.MakeFlat()).normalized;
            public float Angle
            {
                get
                {
                    var angle = MathExtention.GetAngle(StartDir, EndDir);
                    switch (Direction)
                    {
                        case Direction.Right:
                            if (angle > 0f)
                                angle -= Mathf.PI * 2f;
                            break;
                        case Direction.Left:
                            if (angle < 0f)
                                angle += Mathf.PI * 2f;
                            break;
                    }
                    return Mathf.Abs(angle);
                }
            }
            public bool ClockWise => Direction == Direction.Right;

            private InfoLabel _label;
            public InfoLabel Label
            {
                get => _label;
                set
                {
                    _label = value;
                    if (_label != null)
                    {
                        _label.textScale = 1.5f;
                        _label.opacity = 0.75f;
                    }
                }
            }

            public IEnumerable<Point> Parts
            {
                get
                {
                    var angle = Angle;
                    var curveLenght = Radius * Mathf.Abs(angle);

                    var minByLenght = Mathf.CeilToInt(curveLenght / 50f);
                    var maxByLenght = Mathf.CeilToInt(curveLenght / MaxLengthGetter());
                    var maxByAngle = Mathf.CeilToInt(Mathf.Abs(angle) / Mathf.PI * 3);

                    var curveCount = Math.Max(maxByLenght, Mathf.Min(minByLenght, maxByAngle));

                    for (var i = 1; i < curveCount; i += 1)
                    {
                        var deltaAngle = angle / curveCount * i;
                        var point = new Point(CenterPos + StartRadiusDir.TurnRad(deltaAngle, ClockWise) * Radius, StartDir.TurnRad(deltaAngle, ClockWise));
                        yield return point;
                    }
                }
            }

            public Circle(InfoLabel label)
            {
                Label = label;
            }

            public void Update(bool show)
            {
                if (Label is InfoLabel label)
                {
                    label.isVisible = show;
                    if (show)
                    {
                        label.text = $"{GetRadiusString(Radius)}\n{GetAngleString(Mathf.Abs(Angle))}";
                        label.Direction = CenterDir;
                        label.WorldPosition = CenterPos + label.Direction * 5f;

                        label.UpdateInfo();
                    }
                }
            }
            public virtual bool Calculate(float minRadius, float maxRadius, out Result result)
            {
                MinRadius = minRadius;
                MaxRadius = maxRadius;
                Radius = Mathf.Clamp(Radius, MinRadius, MaxRadius);
                result = Result.Calculated;
                return true;
            }

            private static StraightTrajectory GetConnectCenter(Circle first, Circle second) => new StraightTrajectory(first.CenterPos.MakeFlat(), second.CenterPos.MakeFlat());
            public static void SetConnect(Circle first, Circle second)
            {
                var centerConnect = GetConnectCenter(first, second);

                if (first.Direction == second.Direction)
                {
                    var deltaAngle = Mathf.Asin(Mathf.Abs(first.Radius - second.Radius) / centerConnect.Length);
                    first.EndRadiusDir = centerConnect.Direction.TurnRad(Mathf.PI / 2f + (first.Radius >= second.Radius ? -deltaAngle : deltaAngle), !first.ClockWise);
                    second.StartRadiusDir = -centerConnect.Direction.TurnRad(Mathf.PI / 2f + (second.Radius >= first.Radius ? -deltaAngle : deltaAngle), second.ClockWise);
                }
                else
                {
                    var deltaAngle = Mathf.Acos((first.Radius + second.Radius) / centerConnect.Length);
                    first.EndRadiusDir = centerConnect.Direction.TurnRad(deltaAngle, second.ClockWise);
                    second.StartRadiusDir = -centerConnect.Direction.TurnRad(deltaAngle, second.ClockWise);
                }
            }
            public static bool CheckRadii(Circle first, Circle second)
            {
                var centerConnect = GetConnectCenter(first, second);

                if (first.Direction == second.Direction)
                    return centerConnect.Length + first.Radius >= second.Radius && centerConnect.Length + second.Radius >= first.Radius;
                else
                    return first.Radius + second.Radius <= centerConnect.Length;
            }
            public static Straight GetStraight(Circle first, Circle second, InfoLabel label)
            {
                Vector3 labelDir;
                if (first.Direction == second.Direction)
                    labelDir = first.EndRadiusDir;
                else if (first.Radius >= second.Radius)
                    labelDir = -first.EndRadiusDir;
                else
                    labelDir = -second.StartRadiusDir;

                var straight = new Straight(first.EndPos, second.StartPos, labelDir, label);
                return straight;
            }

            public void Render(RenderManager.CameraInfo cameraInfo, NetInfo info, Color32 color, bool underground)
            {
                RenderCenter(cameraInfo, color, underground);
                RenderStartRadius(cameraInfo, info, color, underground);
                RenderEndRadius(cameraInfo, info, color, underground);
            }
            public void RenderCenterHover(RenderManager.CameraInfo cameraInfo, Color32 color, bool underground) => CenterPos.RenderCircle(new OverlayData(cameraInfo) { Color = color, RenderLimit = underground }, 7f, 5f);
            public void RenderCenter(RenderManager.CameraInfo cameraInfo, Color32 color, bool underground) => CenterPos.RenderCircle(new OverlayData(cameraInfo) { Color = color, RenderLimit = underground }, 5f, 0f);

            private void RenderStartRadius(RenderManager.CameraInfo cameraInfo, NetInfo info, Color color, bool underground) => RenderRadius(cameraInfo, info, StartPos, color, underground);
            private void RenderEndRadius(RenderManager.CameraInfo cameraInfo, NetInfo info, Color color, bool underground) => RenderRadius(cameraInfo, info, EndPos, color, underground);
            private void RenderRadius(RenderManager.CameraInfo cameraInfo, NetInfo info, Vector3 curvePos, Color color, bool underground)
            {
                var bezier = new StraightTrajectory(curvePos, CenterPos).Cut(info.m_halfWidth / Radius, 1f);
                bezier.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = underground });
            }
            public void RenderCircle(RenderManager.CameraInfo cameraInfo, Color32 color, bool underground) => CenterPos.RenderCircle(new OverlayData(cameraInfo) { Width = Radius * 2f, Color = color, RenderLimit = underground });
        }
        public enum Direction
        {
            Right,
            Left,
        }
        public class Straight : StraightTrajectory
        {
            public Vector3 LabelDir { get; }
            private InfoLabel _label;
            public InfoLabel Label
            {
                get => _label;
                set
                {
                    _label = value;
                    if (_label != null)
                    {
                        _label.textScale = 1.5f;
                        _label.opacity = 0.75f;
                    }
                }
            }

            public IEnumerable<Point> Parts
            {
                get
                {
                    var count = Mathf.CeilToInt(Length / MaxLengthGetter());
                    for (var i = 1; i < count; i += 1)
                    {
                        var point = new Point(Position(1f / count * i), Direction);
                        yield return point;
                    }
                }
            }

            public Straight(Vector3 start, Vector3 end, Vector3 labelDir, InfoLabel label) : base(start, end)
            {
                LabelDir = labelDir;
                Label = label;
            }

            public void Update(NetInfo info, bool show)
            {
                if (Label is InfoLabel label)
                {
                    label.isVisible = show;
                    if (show)
                    {
                        label.text = GetRadiusString(Length);
                        label.Direction = LabelDir;
                        label.WorldPosition = Position(0.5f) + label.Direction * (info.m_halfWidth + 7f);

                        label.UpdateInfo();
                    }
                }
            }
            public void Render(RenderManager.CameraInfo cameraInfo, NetInfo info, Color color, Color colorArrow, bool underground)
            {
                var data = new OverlayData(cameraInfo) { Color = color, RenderLimit = underground };
                var dataArrow = new OverlayData(cameraInfo) { Color = colorArrow, RenderLimit = underground };

                var dir = LabelDir;
                var isShort = Length <= 10f;

                var startShift = StartPosition + dir * (info.m_halfWidth + 5f) + (isShort ? -Direction : Direction) * 0.5f;
                var endShift = EndPosition + dir * (info.m_halfWidth + 5f) + (isShort ? Direction : -Direction) * 0.5f;

                new StraightTrajectory(StartPosition + dir * info.m_halfWidth, StartPosition + dir * (info.m_halfWidth + 7f)).Render(data);
                new StraightTrajectory(EndPosition + dir * info.m_halfWidth, EndPosition + dir * (info.m_halfWidth + 7f)).Render(data);
                new StraightTrajectory(startShift, endShift).Render(dataArrow);

                var cross = CrossXZ(dir, Direction) > 0f;
                var dirP45 = dir.TurnDeg(45f, isShort ^ cross);
                var dirM45 = dir.TurnDeg(45f, !isShort ^ cross);

                new StraightTrajectory(startShift, startShift + dirP45 * 3f).Render(dataArrow);
                new StraightTrajectory(startShift, startShift - dirM45 * 3f).Render(dataArrow);

                new StraightTrajectory(endShift, endShift - dirP45 * 3f).Render(dataArrow);
                new StraightTrajectory(endShift, endShift + dirM45 * 3f).Render(dataArrow);
            }
        }
    }
}
