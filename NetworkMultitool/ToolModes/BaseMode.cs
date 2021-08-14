using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using HarmonyLib;
using ModsCommon;
using ModsCommon.UI;
using ModsCommon.Utilities;
using NetworkMultitool.UI;
using NetworkMultitool.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;

namespace NetworkMultitool
{
    public abstract class BaseNetworkMultitoolMode : BaseSelectToolMode<NetworkMultitoolTool>, IToolMode<ToolModeType>, ISelectToolMode
    {
        public static NetworkMultitoolShortcut ApplyShortcut { get; } = GetShortcut(KeyCode.Return, nameof(ApplyShortcut), nameof(Localize.Settings_Shortcut_Apply), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as BaseNetworkMultitoolMode)?.Apply());

        protected static NetworkMultitoolShortcut GetShortcut(KeyCode keyCode, Action action, ToolModeType mode = ToolModeType.Any, bool ctrl = false, bool shift = false, bool alt = false, bool repeat = false, bool ignoreModifiers = false) => GetShortcut(keyCode, string.Empty, string.Empty, action, mode, ctrl, shift, alt, repeat, ignoreModifiers);
        protected static NetworkMultitoolShortcut GetShortcut(KeyCode keyCode, string name, string labelKey, Action action, ToolModeType mode = ToolModeType.Any, bool ctrl = false, bool shift = false, bool alt = false, bool repeat = false, bool ignoreModifiers = false) => new NetworkMultitoolShortcut(name, labelKey, SavedInputKey.Encode(keyCode, ctrl, shift, alt), action, mode) { CanRepeat = repeat, IgnoreModifiers = ignoreModifiers };

        public abstract ToolModeType Type { get; }
        public bool IsMain => Type.IsItem();
        public string Title => SingletonMod<Mod>.Instance.GetLocalizeString(Type.GetAttr<DescriptionAttribute, ToolModeType>()?.Description);
        protected abstract bool IsReseted { get; }
        protected virtual bool CanSwitchUnderground => true;
        private bool ForbiddenSwitchUnderground { get; set; }
        protected virtual bool AllowUntouch => false;

        public NetworkMultitoolShortcut ActivationShortcut => NetworkMultitoolTool.ModeShortcuts.TryGetValue(Type, out var shortcut) ? shortcut : null;
        public virtual IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield break;
            }
        }
        protected List<InfoLabel> Labels { get; } = new List<InfoLabel>();

        protected static Func<bool> UndergroundDefaultGetter { get; private set; }
        private static Func<bool> DefaultUndergroundDefaultGetter { get; } = () => false;
        public BaseNetworkMultitoolMode()
        {
            if (Mod.FRTEnabled)
            {
                try
                {
                    if (UndergroundDefaultGetter == null || UndergroundDefaultGetter == DefaultUndergroundDefaultGetter)
                    {
                        var definition = new DynamicMethod("UndergroundGetter", typeof(bool), new Type[0], true);
                        var generator = definition.GetILGenerator();

                        var instanceField = AccessTools.Field(System.Type.GetType("FineRoadTool.FineRoadTool"), "instance");
                        generator.Emit(OpCodes.Ldsfld, instanceField);
                        var modeField = AccessTools.Field(System.Type.GetType("FineRoadTool.FineRoadTool"), "m_mode");
                        generator.Emit(OpCodes.Ldfld, modeField);
                        generator.Emit(OpCodes.Ldc_I4_S, 4);
                        generator.Emit(OpCodes.Ceq);
                        generator.Emit(OpCodes.Ret);

                        UndergroundDefaultGetter = (Func<bool>)definition.CreateDelegate(typeof(Func<bool>));
                    }
                    return;
                }
                catch (Exception error)
                {
                    SingletonMod<Mod>.Logger.Error("Cant access to Node Spacer", error);
                }
            }

            UndergroundDefaultGetter = DefaultUndergroundDefaultGetter;
        }

        protected static string GetRadiusString(float radius, string format = "0.0") => string.Format(Localize.Mode_RadiusFormat, radius.ToString(format));
        protected static string GetAngleString(float angle, string format = "0") => string.Format(Localize.Mode_AngleFormat, (angle * Mathf.Rad2Deg).ToString(format));
        protected static string GetPercentagesString(float percent, string format = "0.0") => string.Format(Localize.Mode_PercentagesFormat, percent.ToString(format));

        public override void Activate(IToolMode prevMode)
        {
            base.Activate(prevMode);
            ForbiddenSwitchUnderground = false;
            ModeButton.SetState(Type, true);
        }
        public override void Deactivate()
        {
            base.Deactivate();
            ModeButton.SetState(Type, false);
            ClearLabels();
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            ClearLabels();
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (!CanSwitchUnderground)
                ForbiddenSwitchUnderground = Utility.ShiftIsPressed && !Underground;
            else if (ForbiddenSwitchUnderground)
                ForbiddenSwitchUnderground = Utility.ShiftIsPressed;
            else if (!Underground && (Utility.OnlyShiftIsPressed ^ UndergroundDefaultGetter()))
                Underground = true;
            else if (Underground && !(Utility.OnlyShiftIsPressed ^ UndergroundDefaultGetter()))
                Underground = false;
        }
        protected virtual void Apply() { }
        public override bool OnEscape()
        {
            if (!IsReseted)
            {
                Reset(this);
                return true;
            }
            else
                return false;
        }

        public sealed override string GetToolInfo()
        {
            var info = GetInfo();
            if (string.IsNullOrEmpty(info))
                return string.Empty;
            else
                return $"{Title.ToUpper()}\n\n{info}";
        }
        protected virtual string GetInfo() => string.Empty;
        protected string StepOverInfo => NetworkMultitoolTool.SelectionStepOverShortcut.NotSet ? string.Empty : "\n\n" + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, NetworkMultitoolTool.SelectionStepOverShortcut.InputKey);
        protected string UndergroundInfo => $"\n\n{Localize.Mode_Info_UndergroundMode}";

        protected override bool CheckSegment(ushort segmentId) => (AllowUntouch || segmentId.GetSegment().m_flags.CheckFlags(0, NetSegment.Flags.Untouchable)) && base.CheckSegment(segmentId);

        protected override bool CheckItemClass(ItemClass itemClass) => itemClass.m_layer == ItemClass.Layer.Default || itemClass.m_layer == ItemClass.Layer.MetroTunnels;


        protected static bool CreateNode(out ushort newNodeId, NetInfo info, Vector3 position)
        {
            if (Singleton<NetManager>.instance.CreateNode(out newNodeId, ref Singleton<SimulationManager>.instance.m_randomizer, info, position, Singleton<SimulationManager>.instance.m_currentBuildIndex))
            {
                ref var node = ref newNodeId.GetNode();
                var elevated = node.m_position.y - Singleton<TerrainManager>.instance.SampleRawHeightSmooth(node.m_position);

                if (elevated < -8f && (info.m_netAI.SupportUnderground() || info.m_netAI.IsUnderground()))
                    node.m_flags |= NetNode.Flags.Underground;
                else if (elevated <= 1f && !info.m_netAI.IsOverground())
                    node.m_flags |= NetNode.Flags.OnGround;

                return true;
            }
            else
                return false;
        }
        protected static bool CreateSegmentAuto(out ushort newSegmentId, NetInfo info, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir)
        {
            ref var startNode = ref startId.GetNode();
            ref var endNode = ref endId.GetNode();
            var startPos = startNode.m_position;
            var endPos = endNode.m_position;

            var startElevated = startPos.y - Singleton<TerrainManager>.instance.SampleRawHeightSmooth(startPos);
            var endElevated = endPos.y - Singleton<TerrainManager>.instance.SampleRawHeightSmooth(endPos);
            if (0f < startElevated && startElevated <= 1f)
                startElevated = 0f;
            if (0f < endElevated && endElevated <= 1f)
                endElevated = 0f;

            var minElevated = Mathf.Min(startElevated, endElevated);
            var maxElevated = Mathf.Max(startElevated, endElevated);

            var erroe = ToolBase.ToolErrors.None;
            var selectedInfo = info.m_netAI.GetInfo(minElevated, maxElevated, (endPos - startPos).magnitude, false, false, false, false, ref erroe);

            info = erroe == ToolBase.ToolErrors.None ? selectedInfo : info;

            if (startNode.m_flags.IsSet(NetNode.Flags.Underground) || !endNode.m_flags.IsSet(NetNode.Flags.Underground))
                return CreateSegment(out newSegmentId, info, startId, endId, startDir, endDir, false);
            else
                return CreateSegment(out newSegmentId, info, endId, startId, endDir, startDir, true);
        }
        protected static bool CreateSegment(out ushort newSegmentId, NetInfo info, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir, bool invert = false) => Singleton<NetManager>.instance.CreateSegment(out newSegmentId, ref Singleton<SimulationManager>.instance.m_randomizer, info, startId, endId, startDir, endDir, Singleton<SimulationManager>.instance.m_currentBuildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, invert);

        protected static void RemoveNode(ushort nodeId) => Singleton<NetManager>.instance.ReleaseNode(nodeId);
        protected static void RemoveSegment(ushort segmentId, bool keepNodes = true) => Singleton<NetManager>.instance.ReleaseSegment(segmentId, keepNodes);
        protected static void RelinkSegment(ushort segmentId, ushort sourceNodeId, ushort targetNodeId)
        {
            var segment = segmentId.GetSegment();
            var otherNodeId = segment.GetOtherNode(sourceNodeId);
            var info = segment.Info;
            var otherDir = segment.IsStartNode(sourceNodeId) ? segment.m_endDirection : segment.m_startDirection;
            var sourceDir = segment.IsStartNode(sourceNodeId) ? segment.m_startDirection : segment.m_endDirection;
            var invert = segment.IsStartNode(sourceNodeId) ^ segment.IsInvert();

            var otherNode = otherNodeId.GetNode();

            var sourceNode = sourceNodeId.GetNode();
            var targetNode = targetNodeId.GetNode();
            var oldDir = new StraightTrajectory(otherNode.m_position.MakeFlat(), sourceNode.m_position.MakeFlat());
            var newDir = new StraightTrajectory(otherNode.m_position.MakeFlat(), targetNode.m_position.MakeFlat());
            var angle = MathExtention.GetAngle(oldDir.Direction, newDir.Direction);

            otherDir = otherDir.TurnRad(angle, false);
            sourceDir = sourceDir.TurnRad(angle, false);

            RemoveSegment(segmentId);
            CreateSegment(out var newSegmentId, info, otherNodeId, targetNodeId, otherDir, sourceDir, invert);
            CalculateSegmentDirections(newSegmentId);
        }
        protected static void MoveNode(ushort nodeId, Vector3 newPos)
        {
            ref var node = ref nodeId.GetNode();
            var segmentIds = node.SegmentIds().ToArray();

            var startDirections = new Dictionary<ushort, Vector3>();
            foreach (var segmentId in segmentIds)
            {
                ref var otherNode = ref segmentId.GetSegment().GetOtherNode(nodeId).GetNode();
                var dir = (node.m_position - otherNode.m_position).MakeFlat();
                startDirections[segmentId] = dir;
            }

            newPos.y = node.m_position.y;
            var oldPos = node.m_position;
            NetManager.instance.MoveNode(nodeId, newPos);

            if (node.m_building != 0)
            {
                BuildingManager.instance.m_buildings.m_buffer[node.m_building].m_position += (newPos - oldPos);
                BuildingManager.instance.UpdateBuilding(node.m_building);
            }

            foreach (var segmentId in segmentIds)
            {
                ref var otherNode = ref segmentId.GetSegment().GetOtherNode(nodeId).GetNode();
                var newDir = (node.m_position - otherNode.m_position).MakeFlat();
                var oldDir = startDirections[segmentId];

                var delta = MathExtention.GetAngle(oldDir, newDir);
                ref var segment = ref segmentId.GetSegment();
                segment.m_startDirection = segment.m_startDirection.TurnRad(delta, false);
                segment.m_endDirection = segment.m_endDirection.TurnRad(delta, false);

                CalculateSegmentDirections(segmentId);
                NetManager.instance.UpdateSegment(segmentId);
            }
        }
        protected static void SetSegmentDirection(ushort nodeId, ushort anotherNodeId, Vector3 direction)
        {
            if (NetExtension.GetCommon(nodeId, anotherNodeId, out var commonId))
                SetSegmentDirection(commonId, commonId.GetSegment().IsStartNode(nodeId), direction);
        }
        protected static void SetSegmentDirection(ushort segmentId, bool start, Vector3 direction)
        {
            ref var segment = ref segmentId.GetSegment();
            if (start)
                segment.m_startDirection = direction;
            else
                segment.m_endDirection = direction;

            CalculateSegmentDirections(segmentId);
            NetManager.instance.UpdateSegment(segmentId);
        }
        protected static void CalculateSegmentDirections(ushort segmentId)
        {
            ref var segment = ref segmentId.GetSegment();

            segment.m_startDirection = NormalizeXZ(segment.m_startDirection);
            segment.m_endDirection = NormalizeXZ(segment.m_endDirection);

            segment.m_startDirection = segment.FindDirection(segmentId, segment.m_startNode);
            segment.m_endDirection = segment.FindDirection(segmentId, segment.m_endNode);
        }
        protected delegate void DirectionGetterDelegate<Type>(Type first, Type second, out Vector3 firstDir, out Vector3 secondDir);
        protected static void SetSlope<Type>(IEnumerable<Type> items, Func<Type, Vector3> positionGetter, DirectionGetterDelegate<Type> directionGetter, Action<Type, Vector3> positionSetter)
        {
            var itemsList = items.ToArray();
            var startY = positionGetter(itemsList[0]).y;
            var endY = positionGetter(itemsList[itemsList.Length - 1]).y;

            var list = new List<ITrajectory>();
            for (var i = 1; i < itemsList.Length; i += 1)
            {
                var startPos = positionGetter(itemsList[i - 1]);
                var endPos = positionGetter(itemsList[i]);
                directionGetter(itemsList[i - 1], itemsList[i], out var startDir, out var endDir);

                startPos.y = 0;
                endPos.y = 0;
                startDir = startDir.MakeFlatNormalized();
                endDir = endDir.MakeFlatNormalized();

                list.Add(new BezierTrajectory(startPos, startDir, endPos, endDir));
            }

            var sumLength = list.Sum(t => t.Length);
            var currentLength = 0f;

            for (var i = 1; i < itemsList.Length - 1; i += 1)
            {
                currentLength += list[i - 1].Length;
                var position = positionGetter(itemsList[i]);
                position.y = Mathf.Lerp(startY, endY, currentLength / sumLength);
                positionSetter(itemsList[i], position);
            }
        }
        protected void SetSlope(IEnumerable<Point> points) => SetSlope(points, PositionGetter, DirectionGetter, PositionSetter);
        private static Vector3 PositionGetter(Point point) => point.Position;
        private static void DirectionGetter(Point first, Point second, out Vector3 firstDir, out Vector3 secondDir)
        {
            firstDir = first.Direction;
            secondDir = -second.Direction;
        }
        private static void PositionSetter(Point point, Vector3 position) => point.Position = position;

        protected static Rect GetTerrainRect(params ushort[] segmentIds) => segmentIds.Select(i => (ITrajectory)new BezierTrajectory(i)).GetRect();
        protected static void UpdateTerrain(params ushort[] segmentIds)
        {
            if (segmentIds.Length != 0)
                UpdateTerrain(GetTerrainRect(segmentIds));
        }
        protected static void UpdateTerrain(Rect rect) => TerrainModify.UpdateArea(rect.xMin, rect.yMin, rect.xMax, rect.yMax, true, true, false);

        protected InfoLabel AddLabel()
        {
            var view = UIView.GetAView();
            var label = view.AddUIComponent(typeof(InfoLabel)) as InfoLabel;
            label.zOrder = 0;
            Labels.Add(label);
            return label;
        }
        protected void RemoveLabel(InfoLabel label)
        {
            Labels.Remove(label);
            Destroy(label.gameObject);
        }
        protected virtual void ClearLabels()
        {
            foreach (var label in Labels)
                Destroy(label.gameObject);

            Labels.Clear();
        }

        protected Vector3 GetMousePosition(float height) => Underground ? Tool.Ray.GetRayPosition(height, out _) : Tool.MouseWorldPosition;
        protected void RenderSegmentNodes(RenderManager.CameraInfo cameraInfo, Func<ushort, bool> isAllow = null)
        {
            if (IsHoverSegment)
            {
                var data = new OverlayData(cameraInfo) { Color = Colors.Blue, RenderLimit = Underground };

                var segment = HoverSegment.Id.GetSegment();
                if (!Underground ^ segment.m_startNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground) && isAllow?.Invoke(segment.m_startNode) != false)
                    new NodeSelection(segment.m_startNode).Render(data);

                if (!Underground ^ segment.m_endNode.GetNode().m_flags.IsSet(NetNode.Flags.Underground) && isAllow?.Invoke(segment.m_endNode) != false)
                    new NodeSelection(segment.m_endNode).Render(data);
            }
        }
        protected void RenderNearNodes(RenderManager.CameraInfo cameraInfo, Vector3? position = null, float radius = 300f, Func<ushort, bool> isAllow = null)
        {
            position ??= Tool.MouseWorldPosition;
            isAllow ??= AllowRenderNear;

            var minX = Min(position.Value.x - radius);
            var minZ = Min(position.Value.z - radius);
            var maxX = Max(position.Value.x + radius);
            var maxZ = Max(position.Value.z + radius);
            var xzPosition = XZ(position.Value);

            for (int i = minZ; i <= maxZ; i++)
            {
                for (int j = minX; j <= maxX; j++)
                {
                    var nodeId = NetManager.instance.m_nodeGrid[i * 270 + j];
                    int count = 0;

                    while (nodeId != 0u && count < NetManager.MAX_SEGMENT_COUNT)
                    {
                        ref var node = ref nodeId.GetNode();
                        var magnitude = (XZ(node.m_position) - xzPosition).magnitude;
                        if (!Underground ^ node.m_flags.IsSet(NetNode.Flags.Underground) && magnitude <= radius && isAllow?.Invoke(nodeId) != false)
                        {
                            var color = Colors.Blue;
                            color.a = (byte)((1 - magnitude / radius) * 255f);
                            node.m_position.RenderCircle(new OverlayData(cameraInfo) { Width = Mathf.Min(8f, node.Info.m_halfWidth * 2f), Color = color, RenderLimit = Underground });
                        }

                        nodeId = node.m_nextGridNode;
                    }
                }
            }


            static int Min(float value) => Mathf.Max((int)((value - 16f) / 64f + 135f) - 1, 0);
            static int Max(float value) => Mathf.Min((int)((value + 16f) / 64f + 135f) + 1, 269);
        }
        protected virtual bool AllowRenderNear(ushort nodeId)
        {
            if (IsHoverNode)
                return nodeId != HoverNode.Id;
            else if (IsHoverSegment)
                return !HoverSegment.Id.GetSegment().Contains(nodeId);
            else
                return true;
        }
        protected static BezierTrajectory GetTrajectory(Point first, Point second) => new BezierTrajectory(first.Position, first.Direction, second.Position, -second.Direction);
        protected void RenderParts(List<Point> points, RenderManager.CameraInfo cameraInfo, Color? color = null, float? width = null)
        {
            var data = new OverlayData(cameraInfo) { Color = color, Width = width, RenderLimit = Underground, Cut = true };
            for (var i = 1; i < points.Count; i += 1)
            {
                if (!points[i - 1].IsEmpty && !points[i].IsEmpty)
                    GetTrajectory(points[i - 1], points[i]).Render(data);
            }
        }
        protected static void PlayEffect(EffectInfo.SpawnArea spawnArea, bool create)
        {
            if (Settings.PlayEffects)
            {
                var effectInfo = create ? Singleton<NetManager>.instance.m_properties.m_placementEffect : Singleton<NetManager>.instance.m_properties.m_bulldozeEffect;
                Singleton<EffectManager>.instance.DispatchEffect(effectInfo, spawnArea, Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, avoidMultipleAudio: true);
            }
        }
        protected static void PlayEffect(BezierTrajectory trajectory, float halfWidth, bool create) => PlayEffect(new EffectInfo.SpawnArea(trajectory.Trajectory, halfWidth, 0f), create);
        protected static void PlaySegmentEffect(ushort segmentId, bool create) => PlayEffect(new EffectInfo.SpawnArea(new BezierTrajectory(segmentId).Trajectory, segmentId.GetSegment().Info.m_halfWidth, 0f), create);
        protected static void PlayNodeEffect(ushort nodeId, bool create)
        {
            ref var node = ref nodeId.GetNode();
            PlayEffect(new EffectInfo.SpawnArea(node.m_position, Vector3.zero, node.Info.m_halfWidth), create);
        }
        protected static void PlayEffect(Point[] points, float halfWidth, bool create)
        {
            for (var i = 1; i < points.Length; i += 1)
                PlayEffect(GetTrajectory(points[i - 1], points[i]), halfWidth, true);
        }
        protected static void PlayAudio(bool create)
        {
            var effectInfo = create ? Singleton<NetManager>.instance.m_properties.m_placementEffect : Singleton<NetManager>.instance.m_properties.m_bulldozeEffect;
            Singleton<EffectManager>.instance.DispatchEffect(effectInfo, new EffectInfo.SpawnArea(), Vector3.zero, 0f, 1f, Singleton<AudioManager>.instance.DefaultGroup, 0u, avoidMultipleAudio: true);
        }

        public class Point
        {
            public Vector3 Position;
            public Vector3 Direction;
            public bool IsEmpty => Position == Vector3.zero && Direction == Vector3.zero;

            public Point(Vector3 position, Vector3 direction)
            {
                Position = position;
                Direction = direction;
            }

            public static Point Empty => new Point(Vector3.zero, Vector3.zero);
        }

        public class BaseStraight : StraightTrajectory
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

            public BaseStraight(Vector3 start, Vector3 end, Vector3 labelDir, InfoLabel label, float height) : base(SetHeight(start, height), SetHeight(end, height))
            {
                LabelDir = labelDir;
                Label = label;
            }
            static Vector3 SetHeight(Vector3 vector, float height)
            {
                vector.y = height;
                return vector;
            }

            public void Update(float shift, bool show)
            {
                if (Label is InfoLabel label)
                {
                    label.isVisible = show;
                    if (show)
                    {
                        label.text = GetRadiusString(Length);
                        label.Direction = LabelDir;
                        label.WorldPosition = Position(0.5f) + label.Direction * shift;

                        label.UpdateInfo();
                    }
                }
            }
        }
    }
    public enum ToolModeType
    {
        [NotItem]
        None = 0,

        [NotItem]
        Group = int.MaxValue << 8,


        [Description(nameof(Localize.Mode_AddNode))]
        AddNode = 1 << (1 + 8),

        [Description(nameof(Localize.Mode_RemoveNode))]
        RemoveNode = AddNode << 1,

        [Description(nameof(Localize.Mode_UnionNode))]
        UnionNode = RemoveNode << 1,

        [Description(nameof(Localize.Mode_SplitNode))]
        SplitNode = UnionNode << 1,


        [Description(nameof(Localize.Mode_IntersectSegment))]
        IntersectSegment = SplitNode << 1,

        [Description(nameof(Localize.Mode_InvertSegment))]
        InvertSegment = IntersectSegment << 1,

        [Description(nameof(Localize.Mode_UnlockSegment))]
        UnlockSegment = InvertSegment << 1,

        [Description(nameof(Localize.Mode_SlopeNode))]
        SlopeNode = UnlockSegment << 1,

        [Description(nameof(Localize.Mode_ArrangeAtLine))]
        ArrangeAtLine = SlopeNode << 1,

        [Description(nameof(Localize.Mode_ArrangeAtCircle))]
        ArrangeAtCircle = ArrangeAtLine << 1,

        [NotItem]
        [Description(nameof(Localize.Mode_ArrangeAtCircle))]
        ArrangeAtCircleComplete = ArrangeAtCircle + 1,

        [NotItem]
        [Description(nameof(Localize.Mode_ArrangeAtCircle))]
        ArrangeAtCircleMoveCenter = ArrangeAtCircle + 2,

        [NotItem]
        [Description(nameof(Localize.Mode_ArrangeAtCircle))]
        ArrangeAtCircleRadius = ArrangeAtCircle + 3,

        [NotItem]
        [Description(nameof(Localize.Mode_ArrangeAtCircle))]
        ArrangeAtCircleMoveNode = ArrangeAtCircle + 4,


        [Description(nameof(Localize.Mode_CreateLoop))]
        CreateLoop = ArrangeAtCircle << 1,

        [NotItem]
        [Description(nameof(Localize.Mode_CreateLoop))]
        CreateLoopMoveCircle = CreateLoop + 1,

        [Description(nameof(Localize.Mode_CreateConnection))]
        CreateConnection = CreateLoop << 1,

        [NotItem]
        [Description(nameof(Localize.Mode_CreateConnection))]
        CreateConnectionMoveCircle = CreateConnection + 1,

        [NotItem]
        [Description(nameof(Localize.Mode_CreateConnection))]
        CreateConnectionChangeRadius = CreateConnection + 2,

        [Description(nameof(Localize.Mode_CreateParallerl))]
        CreateParallel = CreateConnection << 1,

        [Description(nameof(Localize.Mode_CreateBezier))]
        CreateBezier = CreateParallel << 1,


        [NotItem]
        Line = SlopeNode | ArrangeAtLine,

        [NotItem]
        Create = CreateLoop | CreateConnection | CreateBezier | CreateParallel,

        [NotItem]
        Any = int.MaxValue,
    }

    public interface ISelectToolMode
    {
        public void IgnoreSelected();
    }
    public class InfoLabel : CustomUILabel
    {
        public Vector3 WorldPosition { get; set; }
        public Vector3 Direction { get; set; }

        public InfoLabel()
        {
            isVisible = false;
            color = Colors.White;
            textScale = 2f;
            textAlignment = UIHorizontalAlignment.Center;
        }

        public override void Update()
        {
            base.Update();
            UpdateInfo();
        }
        public void UpdateInfo()
        {
            var uIView = GetUIView();
            var startScreenPosition = Camera.main.WorldToScreenPoint(WorldPosition);
            var endScreenPosition = Camera.main.WorldToScreenPoint(WorldPosition + Direction);
            var screenDir = ((Vector2)(endScreenPosition - startScreenPosition)).normalized;
            screenDir.y *= -1;

            var dirLine = new Line2(size / 2f, size / 2f + screenDir);
            var line1 = new Line2(Vector2.zero, Vector2.zero + screenDir.Turn90(true));
            var line2 = new Line2(new Vector2(width, 0f), new Vector2(width, 0f) + screenDir.Turn90(false));
            dirLine.Intersect(line1, out var t1, out _);
            dirLine.Intersect(line2, out var t2, out _);
            var delta = Mathf.Max(Mathf.Abs(t1), Mathf.Abs(t2));

            var relativePosition = uIView.ScreenPointToGUI(startScreenPosition / uIView.inputScale) - size * 0.5f + screenDir * delta;

            this.relativePosition = relativePosition;
        }
    }
}
