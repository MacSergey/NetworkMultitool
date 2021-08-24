using ColossalFramework;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using NetworkMultitool.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    public abstract class BaseCreateMode : BaseNetworkMultitoolMode, ICostMode
    {
        public static NetworkMultitoolShortcut SwitchFollowTerrainShortcut { get; } = GetShortcut(KeyCode.F, nameof(SwitchFollowTerrainShortcut), nameof(Localize.Settings_Shortcut_SwitchFollowTerrain), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as BaseCreateMode)?.SwitchFollowTerrain(), ctrl: true);

        protected override bool IsReseted => !IsFirst;
        protected override bool CanSwitchUnderground => !IsBoth;


        protected override Color32 SegmentColor => Colors.Blue;
        protected override Color32 NodeColor => Colors.Blue;

        protected override bool CheckUnderground => !IsBoth;
        protected override bool SelectNodes => IsBoth;
        private static float TurnAngle { get; } = Mathf.PI / 90f;
        protected override bool IsValidSegment(ushort segmentId) => !IsBoth && segmentId != First?.Id && segmentId != Second?.Id;
        protected override bool IsValidNode(ushort nodeId) => (!IsBoth && base.IsValidNode(nodeId)) || (IsBoth && (First.Id.GetSegment().Contains(nodeId) || Second.Id.GetSegment().Contains(nodeId)));
        protected override bool AllowUntouch => true;

        protected SegmentSelection First { get; set; }
        protected SegmentSelection Second { get; set; }

        protected bool IsFirstStart { get; set; }
        protected bool IsSecondStart { get; set; }
        protected bool IsFirst => First != null;
        protected bool IsSecond => Second != null;
        protected bool IsBoth => IsFirst && IsSecond;

        protected ushort FirstNodeId => IsFirst ? First.Id.GetSegment().GetNode(IsFirstStart) : 0;
        protected ushort SecondNodeId => IsSecond ? Second.Id.GetSegment().GetNode(IsSecondStart) : 0;

        protected float Height { get; set; }
        protected StraightTrajectory FirstTrajectory { get; set; }
        protected StraightTrajectory SecondTrajectory { get; set; }

        protected InitResult InitState { get; private set; }
        protected CalcResult CalcState { get; private set; }

        protected bool FollowTerrain { get; private set; }
        protected bool IsFollowTerrain => FollowTerrain && FirstNodeId.GetNode().m_flags.IsFlagSet(NetNode.Flags.OnGround) && SecondNodeId.GetNode().m_flags.IsFlagSet(NetNode.Flags.OnGround);

        private List<Point> Points { get; set; } = new List<Point>();
        protected NetInfo Info => GetNetInfo() ?? First.Id.GetSegment().Info;
        protected float MinPossibleRadius => Info != null ? Info.m_halfWidth * 2f : 16f;
        protected float MaxPossibleRadius => 3000f;

        private bool ForceUnderground => IsBoth && (First.Id.GetSegment().Nodes().Any(n => n.m_flags.IsSet(NetNode.Flags.Underground)) || Second.Id.GetSegment().Nodes().Any(n => n.m_flags.IsSet(NetNode.Flags.Underground)));

        public int Cost { get; private set; }
        private new bool EnoughMoney => !Settings.NeedMoney || EnoughMoney(Cost);

        protected static Func<float> MaxLengthGetter { get; private set; }
        private static Func<float> DefaultMaxLengthGetter { get; } = () => Settings.SegmentLength;

        public BaseCreateMode()
        {
            if (Mod.NodeSpacerEnabled)
            {
                try
                {
                    var method = System.Type.GetType("NodeSpacer.NT_CreateNode").GetMethod("GetMaxLength");
                    if (MaxLengthGetter?.Method != method)
                    {
                        MaxLengthGetter = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), method);
                        SingletonMod<Mod>.Logger.Debug("Segment length linked to Node Spacer");
                    }
                    return;
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error("Cant access to Node Spacer", error);
                }
            }

            MaxLengthGetter = DefaultMaxLengthGetter;
        }

        protected string GetBaseInfo()
        {
            if (!IsFirst)
            {
                if (!IsHoverSegment)
                    return Localize.Mode_Info_SelectFirstSegment + UndergroundInfo;
                else
                    return Localize.Mode_Info_ClickFirstSegment.AddActionColor() + StepOverInfo;
            }
            else if (!IsSecond)
            {
                if (!IsHoverSegment)
                    return Localize.Mode_Info_SelectSecondSegment + UndergroundInfo;
                else
                    return Localize.Mode_Info_ClickSecondSegment.AddActionColor() + StepOverInfo;
            }
            else if (IsHoverNode)
                return Localize.Mode_Info_ClickToChangeCreateDir.AddActionColor();
            else
                return null;
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            ResetParams();
            Cost = 0;

            if (prevMode is BaseCreateMode createMode)
            {
                First = createMode.First;
                Second = createMode.Second;
                IsFirstStart = createMode.IsFirstStart;
                IsSecondStart = createMode.IsSecondStart;
                FollowTerrain = createMode.FollowTerrain;

                if (createMode.InitState == InitResult.Inited)
                    Reinit();
            }
            else
            {
                First = null;
                Second = null;
                FollowTerrain = Settings.FollowTerrain;
            }
        }
        protected virtual void ResetParams()
        {
            InitState = InitResult.None;
            CalcState = CalcResult.None;

            IsFirstStart = true;
            IsSecondStart = true;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (InitState == InitResult.NotInited)
                Init();

            if (InitState == InitResult.Inited && CalcState == CalcResult.None)
            {
                Points = new List<Point>();
                Points.Add(new Point(FirstTrajectory.StartPosition.SetHeight(Height), FirstTrajectory.Direction));
                Points.AddRange(Calculate(out var result));
                Points.Add(new Point(SecondTrajectory.StartPosition.SetHeight(Height), -SecondTrajectory.Direction));

                CalcState = result;
                if (CalcState == CalcResult.Calculated)
                {
                    if (!CheckOutOfMap())
                        CalcState = CalcResult.OutOfMap;

                    if (Settings.NeedMoney)
                        Cost = GetCost(Points.ToArray(), Info);
                }
            }
        }
        protected abstract Point[] Calculate(out CalcResult result);

        private bool CheckOutOfMap()
        {
            var delta = Info.m_halfWidth;
            foreach (var point in Points)
            {
                if (Math.Abs(point.Position.x) + delta > 8640f || Math.Abs(point.Position.z) + delta > 8640f)
                    return false;
            }
            return true;
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
                {
                    Second = HoverSegment;
                    Reinit();
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
            ref var firstSegment = ref First.Id.GetSegment();
            ref var secondSegment = ref Second.Id.GetSegment();

            var firstPos = firstSegment.GetNode(IsFirstStart).GetNode().m_position;
            var secondPos = secondSegment.GetNode(IsSecondStart).GetNode().m_position;
            var firstDir = -firstSegment.GetDirection(IsFirstStart).MakeFlatNormalized();
            var secondDir = -secondSegment.GetDirection(IsSecondStart).MakeFlatNormalized();

            Height = (firstPos.y + secondPos.y) / 2f;

            FirstTrajectory = new StraightTrajectory(firstPos, firstPos + firstDir, false);
            SecondTrajectory = new StraightTrajectory(secondPos, secondPos + secondDir, false);
            Points = new List<Point>();

            CalcState = Init(FirstTrajectory, SecondTrajectory, out var calcState) ? CalcResult.None : calcState;
            SetInited();
        }
        protected abstract bool Init(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, out CalcResult calcState);

        protected virtual void SetFirstNode(ref NetSegment segment, ushort nodeId)
        {
            if (IsFirstStart != segment.IsStartNode(nodeId))
            {
                IsFirstStart = !IsFirstStart;
                Reinit();
            }
        }
        protected virtual void SetSecondNode(ref NetSegment segment, ushort nodeId)
        {
            if (IsSecondStart != segment.IsStartNode(nodeId))
            {
                IsSecondStart = !IsSecondStart;
                Reinit();
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
            if (CalcState == CalcResult.Calculated && EnoughMoney && Info is NetInfo info)
            {
                var points = Points.ToArray();
                var firstId = First.Id;
                var secondId = Second.Id;
                var isFirstStart = IsFirstStart;
                var isSecondStart = IsSecondStart;
                var followTerrain = IsFollowTerrain;
                var cost = Cost;
                SimulationManager.instance.AddAction(() =>
                {
                    Create(points, firstId, secondId, isFirstStart, isSecondStart, info, followTerrain, cost);
                    PlayEffect(points, info.m_halfWidth, true);
                });

                Reset(null);
            }
        }
        private static void Create(Point[] points, ushort firstId, ushort secondId, bool isFirstStart, bool isSecondStart, NetInfo info, bool followTerrain, int cost)
        {
            var startNodeId = firstId.GetSegment().GetNode(isFirstStart);
            var endNodeId = secondId.GetSegment().GetNode(isSecondStart);

            if (followTerrain)
                SetTerrain(points, startNodeId.GetNode().m_position.y, endNodeId.GetNode().m_position.y);
            else
                SetSlope(points, startNodeId.GetNode().m_position.y, endNodeId.GetNode().m_position.y);

            FixEdgePoint(true, points[0], points[1], firstId, isFirstStart);
            FixEdgePoint(false, points[points.Length - 1], points[points.Length - 2], secondId, isSecondStart);

            var nodeIds = new List<ushort>();

            nodeIds.Add(startNodeId);
            for (var i = 1; i < points.Length - 1; i += 1)
            {
                CreateNode(out var newNodeId, info, points[i].Position);
                nodeIds.Add(newNodeId);
            }
            nodeIds.Add(endNodeId);

            for (var i = 1; i < nodeIds.Count; i += 1)
            {
                CreateSegmentAuto(out var newSegmentId, info, nodeIds[i - 1], nodeIds[i], points[i - 1].ForwardDirection, points[i].BackwardDirection);
                CalculateSegmentDirections(newSegmentId);
            }

            ChangeMoney(cost, info);
        }
        private static void FixEdgePoint(bool isFirst, Point point, Point nextPoint, ushort startSegmentId, bool isStart)
        {
            ref var selectSegment = ref startSegmentId.GetSegment();
            var nodeId = isStart ? selectSegment.m_startNode : selectSegment.m_endNode;
            ref var node = ref nodeId.GetNode();

            foreach (var segmentId in node.SegmentIds())
            {
                ref var segment = ref segmentId.GetSegment();
                var startDir = segment.IsStartNode(nodeId) ? segment.m_startDirection : segment.m_endDirection;
                var segmentAngle = MathExtention.GetAngle(isFirst ? point.ForwardDirection : point.BackwardDirection, startDir);

                if (Mathf.Abs(segmentAngle) < TurnAngle)
                {
                    var otherNodeId = segment.GetOtherNode(nodeId);
                    ref var otherNode = ref otherNodeId.GetNode();
                    var partDir = nextPoint.Position - point.Position;
                    var segmentDir = otherNode.m_position - node.m_position;
                    var angle = MathExtention.GetAngle(partDir, segmentDir);

                    if (segmentAngle == 0)
                    {
                        point.ForwardDirection = point.ForwardDirection.TurnRad(TurnAngle, angle >= 0);
                        point.BackwardDirection = point.BackwardDirection.TurnRad(TurnAngle, angle >= 0);
                    }
                    else if (Mathf.Sign(angle) == Mathf.Sign(segmentAngle))
                    {
                        var delta = TurnAngle - Mathf.Abs(segmentAngle);
                        point.ForwardDirection = point.ForwardDirection.TurnRad(delta, segmentAngle >= 0);
                        point.BackwardDirection = point.BackwardDirection.TurnRad(delta, segmentAngle >= 0);
                    }
                    else
                    {
                        var delta = TurnAngle + Mathf.Abs(segmentAngle);
                        point.ForwardDirection = point.ForwardDirection.TurnRad(delta, segmentAngle <= 0);
                        point.BackwardDirection = point.BackwardDirection.TurnRad(delta, segmentAngle <= 0);
                    }
                    break;
                }
            }
        }

        public void Reinit() => InitState = InitResult.NotInited;
        protected void SetInited()
        {
            InitState = InitResult.Inited;
            Underground = ForceUnderground;
        }
        public void Recalculate() => CalcState = CalcResult.None;
        protected virtual void IncreaseRadius() { }
        protected virtual void DecreaseRadius() { }
        private void SwitchFollowTerrain() => FollowTerrain = !FollowTerrain;

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
            var color = CalcState switch
            {
                CalcResult.BigRadius or CalcResult.SmallRadius or CalcResult.WrongShape or CalcResult.NotIntersect => Colors.Red,
                CalcResult.Calculated => Colors.White.SetAlpha(64),
                _ => Colors.White,
            };

            if (IsFirst)
                First.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
            if (IsSecond)
                Second.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });

            if (CalcState == CalcResult.Calculated)
            {
                var info = Info;
                RenderCalculatedOverlay(cameraInfo, Info);
                if (Settings.NetworkPreview != (int)Settings.PreviewType.Mesh)
                    RenderParts(Points, cameraInfo, EnoughMoney ? Colors.Yellow : Colors.Red, info.m_halfWidth * 2f);
            }
            else if (CalcState != CalcResult.None)
            {
                var info = Info;
                RenderFailedOverlay(cameraInfo, info);
                if (Settings.NetworkPreview != (int)Settings.PreviewType.Mesh)
                    RenderParts(Points, cameraInfo);
            }

            base.RenderOverlay(cameraInfo);
        }
        protected virtual void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info) { }
        protected virtual void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info) { }
        public override void RenderGeometry(RenderManager.CameraInfo cameraInfo)
        {
            if (CalcState == CalcResult.Calculated && Settings.NetworkPreview != (int)Settings.PreviewType.Overlay)
            {
                var points = Points.ToArray();

                if (IsFollowTerrain)
                    SetTerrain(points, FirstTrajectory.StartPosition.y, SecondTrajectory.StartPosition.y);
                else
                    SetSlope(points, FirstTrajectory.StartPosition.y, SecondTrajectory.StartPosition.y);

                RenderParts(points, Info);
            }

            base.RenderGeometry(cameraInfo);
        }

        public enum InitResult
        {
            None,
            NotInited,
            Inited,
        }
        public enum CalcResult
        {
            None,
            NotIntersect,
            SmallRadius,
            BigRadius,
            WrongShape,
            OutOfMap,
            Calculated,
        }
        public enum Direction
        {
            Right,
            Left,
        }
        public class Circle
        {
            public float Height { get; }
            private Vector3 _centerPos;
            public virtual Vector3 CenterPos
            {
                get => _centerPos;
                set
                {
                    _centerPos = value;
                    _centerPos.y = Height;
                }
            }
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
            public float Length => Radius * Angle;
            public bool IsShort => Length < 8f;

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
                    var curveLength = Radius * Mathf.Abs(angle);

                    var minByLength = Mathf.CeilToInt(curveLength / 50f);
                    var maxByLength = Mathf.CeilToInt(curveLength / MaxLengthGetter());
                    var maxByAngle = Mathf.CeilToInt(Mathf.Abs(angle) / Mathf.PI * 2f);

                    var curveCount = Math.Max(maxByLength, Mathf.Min(minByLength, maxByAngle));

                    for (var i = 1; i < curveCount; i += 1)
                    {
                        var deltaAngle = angle / curveCount * i;
                        var point = new Point(CenterPos + StartRadiusDir.TurnRad(deltaAngle, ClockWise) * Radius, StartDir.TurnRad(deltaAngle, ClockWise));
                        yield return point;
                    }
                }
            }
            public Point StartPoint => new Point(StartPos, StartDir);
            public Point MiddlePoint
            {
                get
                {
                    var angle = Angle / 2f;
                    return new Point(CenterPos + StartRadiusDir.TurnRad(angle, ClockWise) * Radius, StartDir.TurnRad(angle, ClockWise));
                }
            }
            public Point EndPoint => new Point(EndPos, EndDir);

            public static float MinSnapping => 10f;
            public static float MinDelta => 0.01f;
            public virtual bool PossiblePositionSnapping => true;
            public virtual bool PossibleRadiusSnapping => true;

            public Circle(InfoLabel label, float height)
            {
                Height = height;
                Label = label;
            }

            public void Update(bool show)
            {
                if (Label is InfoLabel label)
                {
                    label.Show = show;
                    if (show)
                    {
                        label.text = IsCorrect ? $"{GetLengthString(Radius)}\n{GetAngleString(Mathf.Abs(Angle))}" : GetLengthString(Radius);
                        label.Direction = IsCorrect ? CenterDir : Vector3.forward;
                        label.WorldPosition = CenterPos + label.Direction * 5f;

                        label.UpdateInfo();
                    }
                }
            }
            public virtual void Calculate(float minRadius, float maxRadius)
            {
                MinRadius = minRadius;
                MaxRadius = maxRadius;
                Radius = Mathf.Clamp(Radius, MinRadius, MaxRadius);
            }

            protected static StraightTrajectory GetConnectCenter(Circle first, Circle second) => new StraightTrajectory(first.CenterPos.MakeFlat(), second.CenterPos.MakeFlat());
            public static void SetConnect(Circle first, Circle second)
            {
                var centerConnect = GetConnectCenter(first, second);
                var delta = GetDelta(first, second);

                if (first.Direction == second.Direction)
                {
                    if (delta < MinDelta)
                    {
                        var direction = first.Radius >= second.Radius ? centerConnect.Direction : -centerConnect.Direction;
                        first.EndRadiusDir = direction;
                        second.StartRadiusDir = direction;
                    }
                    else
                    {
                        var deltaAngle = Mathf.Asin(Mathf.Abs(first.Radius - second.Radius) / centerConnect.Length);
                        first.EndRadiusDir = centerConnect.Direction.TurnRad(Mathf.PI / 2f + (first.Radius >= second.Radius ? -deltaAngle : deltaAngle), !first.ClockWise);
                        second.StartRadiusDir = -centerConnect.Direction.TurnRad(Mathf.PI / 2f + (second.Radius >= first.Radius ? -deltaAngle : deltaAngle), second.ClockWise);
                    }
                }
                else
                {
                    if (delta < MinDelta)
                    {
                        first.EndRadiusDir = centerConnect.Direction;
                        second.StartRadiusDir = -centerConnect.Direction;
                    }
                    else
                    {
                        var deltaAngle = Mathf.Acos((first.Radius + second.Radius) / centerConnect.Length);
                        first.EndRadiusDir = centerConnect.Direction.TurnRad(deltaAngle, !first.ClockWise);
                        second.StartRadiusDir = -centerConnect.Direction.TurnRad(deltaAngle, second.ClockWise);
                    }
                }
            }
            public static bool CheckRadii(Circle first, Circle second)
            {
                var centerConnect = GetConnectCenter(first, second);

                if (first.Direction == second.Direction)
                    return second.Radius - first.Radius <= centerConnect.Length + MinDelta && first.Radius - second.Radius <= centerConnect.Length + MinDelta;
                else
                    return first.Radius + second.Radius <= centerConnect.Length + MinDelta;
            }
            public static Straight GetStraight(Circle first, Circle second, InfoLabel label, float height)
            {
                Vector3 labelDir;
                if (first.Direction == second.Direction)
                    labelDir = first.EndRadiusDir;
                else if (first.Radius >= second.Radius)
                    labelDir = -first.EndRadiusDir;
                else
                    labelDir = -second.StartRadiusDir;

                var start = first.EndPos;
                var end = second.StartPos;
                if ((end - start).sqrMagnitude >= 1f)
                {
                    var straight = new Straight(start, end, labelDir, label, height);
                    return straight;
                }
                else if (first.Direction == second.Direction)
                {
                    var pos = (start + end) / 2f;
                    var dir = (first.EndRadiusDir + second.StartRadiusDir).normalized.Turn90(first.ClockWise);
                    var straight = new Straight(pos - dir, pos + dir, labelDir, label, height);
                    return straight;
                }
                else
                {
                    var pos = (start + end) / 2f;
                    var dir = GetConnectCenter(first, second).Direction.Turn90(first.ClockWise);
                    var straight = new Straight(pos - dir, pos + dir, labelDir, label, height);
                    return straight;
                }
            }
            public IEnumerable<Point> GetParts(Straight before, Straight after)
            {
                if (!IsShort)
                {
                    if (!before.IsShort)
                        yield return StartPoint;

                    foreach (var part in Parts)
                        yield return part;

                    if (!after.IsShort)
                        yield return EndPoint;
                }
                else if (!before.IsShort && !after.IsShort)
                    yield return MiddlePoint;
            }
            public virtual void SnappingPosition(List<Circle> circles)
            {
                if (circles.Count <= 1)
                    return;

                var index = circles.IndexOf(this);

                if (index == 0 || index == circles.Count - 1)
                {
                    var other = circles[index == 0 ? index + 1 : index - 1];
                    if (CanSnapping(this, other))
                        SnappingOnePosition(other);
                }
                else
                {
                    var before = circles[index - 1];
                    var after = circles[index + 1];

                    var beforeSnapping = CanSnapping(this, before);
                    var afterSnapping = CanSnapping(this, after);

                    if (beforeSnapping && afterSnapping)
                        SnappingTwoPositions(before, after);
                    else if (beforeSnapping)
                        SnappingOnePosition(before);
                    else if (afterSnapping)
                        SnappingOnePosition(after);
                }
            }
            protected virtual void SnappingOnePosition(Circle other)
            {
                var connect = GetConnectCenter(other, this);
                if (other.Direction != Direction)
                    CenterPos = other.CenterPos + connect.Direction * (other.Radius + Radius);
                else if (Math.Abs(Radius - other.Radius) >= MinSnapping)
                    CenterPos = other.CenterPos + connect.Direction * Mathf.Abs(Radius - other.Radius);
            }
            protected virtual void SnappingTwoPositions(Circle before, Circle after)
            {
                var otherConnect = GetConnectCenter(before, after);
                var beforeConnect = GetConnectCenter(before, this);
                var afterConnect = GetConnectCenter(after, this);

                var a = otherConnect.Length;
                var b = before.Direction == Direction ? Mathf.Abs(before.Radius - Radius) : before.Radius + Radius;
                var c = Direction == after.Direction ? Mathf.Abs(after.Radius - Radius) : after.Radius + Radius;

                var angleBefore = Mathf.Acos((a * a + b * b - c * c) / (2f * a * b));
                var angleAfter = Mathf.Acos((a * a + c * c - b * b) / (2f * a * c));

                var beforeClock = NormalizeCrossXZ(otherConnect.Direction, beforeConnect.Direction) >= 0f;
                var afterClock = NormalizeCrossXZ(-otherConnect.Direction, afterConnect.Direction) >= 0f;

                var posBefore = before.CenterPos + otherConnect.Direction.TurnRad(angleBefore, beforeClock) * b;
                var posAfter = after.CenterPos - otherConnect.Direction.TurnRad(angleAfter, afterClock) * c;

                CenterPos = (posBefore + posAfter) / 2f;
            }
            public void SetSnappingRadius(List<Circle> circles)
            {
                if (GetSnappingRadius(circles, out var snappingRadius))
                    Radius = snappingRadius;
            }
            public virtual bool GetSnappingRadius(List<Circle> circles, out float snappingRadius)
            {
                if (circles.Count > 1)
                {
                    var index = circles.IndexOf(this);

                    if (index == 0 || index == circles.Count - 1)
                    {
                        var other = circles[index == 0 ? index + 1 : index - 1];
                        if (CanSnapping(this, other))
                            return GetSnappingRadius(other, out snappingRadius);
                    }
                    else
                    {
                        var before = circles[index - 1];
                        var after = circles[index + 1];

                        var beforeSnapping = CanSnapping(this, before);
                        var afterSnapping = CanSnapping(this, after);

                        if (beforeSnapping && afterSnapping)
                        {
                            if (GetDelta(this, before) < GetDelta(this, after))
                                return GetSnappingRadius(before, out snappingRadius);
                            else
                                return GetSnappingRadius(after, out snappingRadius);
                        }
                        else if (beforeSnapping)
                            return GetSnappingRadius(before, out snappingRadius);
                        else if (afterSnapping)
                            return GetSnappingRadius(after, out snappingRadius);
                    }
                }

                snappingRadius = 0f;
                return false;
            }

            public virtual bool GetSnappingRadius(Circle other, out float snappingRadius)
            {
                var centerConnect = GetConnectCenter(this, other);
                if (Direction != other.Direction)
                {
                    snappingRadius = centerConnect.Length - other.Radius;
                    return true;
                }
                else if (centerConnect.Length > 0.5f)
                {
                    var radius1 = other.Radius + centerConnect.Length;
                    var radius2 = other.Radius - centerConnect.Length;
                    snappingRadius = Mathf.Abs(Radius - radius1) < Mathf.Abs(Radius - radius2) ? radius1 : radius2;
                    return true;
                }
                else
                {
                    snappingRadius = 0f;
                    return false;
                }
            }
            public static float GetDelta(Circle first, Circle second)
            {
                var centerConnect = GetConnectCenter(first, second);
                if (first.Direction == second.Direction)
                    return Mathf.Abs(Mathf.Abs(first.Radius - second.Radius) - centerConnect.Length);
                else
                    return Mathf.Abs(centerConnect.Length - (first.Radius + second.Radius));
            }
            public static bool IsSnapping(Circle first, Circle second) => GetDelta(first, second) < MinDelta;
            public static bool CanSnapping(Circle first, Circle second) => (first.Direction != second.Direction || Math.Abs(first.Radius - second.Radius) >= 1f) && GetDelta(first, second) < MinSnapping;

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
        public class Straight : BaseStraight
        {
            public bool IsShort => Length < 8f;

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
            public Point StartPoint => new Point(StartPosition, StartDirection);
            public Point MiddlePoint => new Point(Position(0.5f), Tangent(0.5f));
            public Point EndPoint => new Point(EndPosition, -EndDirection);

            public Straight(Vector3 start, Vector3 end, Vector3 labelDir, InfoLabel label, float height) : base(start, end, labelDir, label, height) { }

            public void Update(NetInfo info, bool show) => Update(info.m_halfWidth + 7f, show);

            public void Render(RenderManager.CameraInfo cameraInfo, NetInfo info, Color color, Color colorArrow, bool underground) => this.RenderMeasure(cameraInfo, info.m_halfWidth, 5f, LabelDir, color, colorArrow, underground);
        }
    }
}
