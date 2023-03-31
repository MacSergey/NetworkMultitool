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
using static NetworkMultitool.BaseCreateMode;

namespace NetworkMultitool
{
    public abstract class BaseNetworkMultitoolMode : BaseSelectToolMode<NetworkMultitoolTool>, IToolMode<ToolModeType>, ISelectToolMode
    {
        public static NetworkMultitoolShortcut ApplyShortcut { get; } = GetShortcut(KeyCode.Return, nameof(ApplyShortcut), nameof(Localize.Settings_Shortcut_Apply), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as BaseNetworkMultitoolMode)?.Apply());

        public static NetworkMultitoolShortcut InvertNetworkShortcut { get; } = GetShortcut(KeyCode.I, nameof(InvertNetworkShortcut), nameof(Localize.Settings_Shortcut_InvertNetwork), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as IInvertNetworkMode)?.SetInvert(), ctrl: true);

        protected static NetworkMultitoolShortcut GetShortcut(KeyCode keyCode, Action action, ToolModeType mode = ToolModeType.Any, bool ctrl = false, bool shift = false, bool alt = false, bool repeat = false, bool ignoreModifiers = false) => GetShortcut(keyCode, string.Empty, string.Empty, action, mode, ctrl, shift, alt, repeat, ignoreModifiers);
        protected static NetworkMultitoolShortcut GetShortcut(KeyCode keyCode, string name, string labelKey, Action action, ToolModeType mode = ToolModeType.Any, bool ctrl = false, bool shift = false, bool alt = false, bool repeat = false, bool ignoreModifiers = false) => new NetworkMultitoolShortcut(name, labelKey, SavedInputKey.Encode(keyCode, ctrl, shift, alt), action, mode) { CanRepeat = repeat, IgnoreModifiers = ignoreModifiers };

        public abstract ToolModeType Type { get; }
        public bool IsMain => Type.IsItem();
        public string Title => SingletonMod<Mod>.Instance.GetLocalizedString(Type.GetAttr<DescriptionAttribute, ToolModeType>()?.Description);
        protected abstract bool IsReseted { get; }
        protected virtual bool CanSwitchUnderground => true;
        private bool ForbiddenSwitchUnderground { get; set; }
        protected virtual bool AllowUntouch => false;
        protected bool NeedMoney => Settings.NeedMoney && Utility.OnGame;

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
            UndergroundDefaultGetter = DefaultUndergroundDefaultGetter;
        }

        protected static string GetLengthString(float radius, string format = null)
        {
            if (Settings.LengthUnite == 0)
                return string.Format(Localize.Mode_RadiusFormat, radius.ToString(format ?? "0.0"));
            else
                return string.Format(Localize.Mode_UnitsFormat, Mathf.Round(radius / 8f).ToString(format ?? "0"));
        }
        protected static string GetAngleString(float angle, string format = "0") => string.Format(Localize.Mode_AngleFormat, angle.ToString(format));
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

        #region INFO

        public sealed override string GetToolInfo()
        {
            var info = GetInfo();
            if (string.IsNullOrEmpty(info))
                return string.Empty;
            else
                return $"{Title.ToUpper()}\n\n{info}";
        }
        protected virtual string GetInfo() => string.Empty;
        protected string StepOverInfo => NetworkMultitoolTool.SelectionStepOverShortcut.NotSet ? string.Empty : "\n\n" + string.Format(CommonLocalize.Tool_InfoSelectionStepOver, CommonColors.AddInfoColor(NetworkMultitoolTool.SelectionStepOverShortcut));
        protected string UndergroundInfo => $"\n\n{string.Format(Localize.Mode_Info_UndergroundMode, LocalizeExtension.Shift.AddInfoColor())}";
        protected string CostInfo
        {
            get
            {
                if (this is not ICostMode costMode || !NeedMoney)
                    return string.Empty;
                else if (costMode.Cost < 0)
                    return string.Format(Localize.Mode_Info_Refund, -costMode.Cost / 100).AddInfoColor() + "\n\n";
                else if (!EnoughMoney(costMode.Cost))
                    return (string.Format(Localize.Mode_Info_ConstructionCost, costMode.Cost / 100) + "\n" + Localize.Mode_Info_NotEnoughMoney).AddErrorColor() + "\n\n";
                else return string.Format(Localize.Mode_Info_ConstructionCost, costMode.Cost / 100).AddInfoColor() + "\n\n";
            }
        }
        protected string MoveSlowerInfo =>
            string.Format(Localize.Mode_Info_HoldToMoveSlower, LocalizeExtension.Ctrl.AddInfoColor(), CommonColors.AddInfoColor("10")) + "\n" +
            string.Format(Localize.Mode_Info_HoldToMoveSlower, LocalizeExtension.Alt.AddInfoColor(), CommonColors.AddInfoColor("100"));
        protected string RadiusStepInfo =>
            string.Format(Localize.Mode_Info_HoldToStep, LocalizeExtension.Shift.AddInfoColor(), string.Format(Localize.Mode_RadiusFormat, 10f).AddInfoColor()) + "\n" +
            string.Format(Localize.Mode_Info_HoldToStep, LocalizeExtension.Ctrl.AddInfoColor(), string.Format(Localize.Mode_RadiusFormat, 1f).AddInfoColor()) + "\n" +
            string.Format(Localize.Mode_Info_HoldToStep, LocalizeExtension.Alt.AddInfoColor(), string.Format(Localize.Mode_RadiusFormat, 0.1f).AddInfoColor());

        #endregion

        protected override bool CheckSegment(ushort segmentId) => (AllowUntouch || segmentId.GetSegment().m_flags.CheckFlags(0, NetSegment.Flags.Untouchable)) && base.CheckSegment(segmentId);

        protected override bool CheckItemClass(ItemClass itemClass) => itemClass.m_layer == ItemClass.Layer.Default || itemClass.m_layer == ItemClass.Layer.MetroTunnels;

        #region CREATE

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
        protected static bool FindOrCreateNode(out ushort newNodeId, NetInfo info, Vector3 position)
        {
            if (FindCloseNode(out newNodeId, info, position))
                return true;
            else
                return CreateNode(out newNodeId, info, position);
        }
        protected static bool CreateNodeFromSource(out ushort newNodeId, ushort source, Vector3 position)
        {
            ref var sourceNode = ref source.GetNode();

            if (Singleton<NetManager>.instance.CreateNode(out newNodeId, ref Singleton<SimulationManager>.instance.m_randomizer, sourceNode.Info, position, Singleton<SimulationManager>.instance.m_currentBuildIndex))
            {
                ref var node = ref newNodeId.GetNode();
                node.m_flags = sourceNode.m_flags;
                return true;
            }
            else
                return false;
        }
        protected static bool FindOrCreateNodeFromSource(out ushort newNodeId, ushort source, Vector3 position)
        {
            ref var sourceNode = ref source.GetNode();
            if (FindCloseNode(out newNodeId, sourceNode.Info, position))
                return true;
            else
                return CreateNodeFromSource(out newNodeId, source, position);
        }
        private static bool FindCloseNode(out ushort nodeId, NetInfo info, Vector3 position)
        {

            var gridMinX = MinCell(position.x);
            var gridMinZ = MinCell(position.z);
            var gridMaxX = MaxCell(position.x);
            var gridMaxZ = MaxCell(position.z);
            for (int i = gridMinZ; i <= gridMaxZ; i++)
            {
                for (int j = gridMinX; j <= gridMaxX; j++)
                {
                    nodeId = NetManager.instance.m_nodeGrid[i * 270 + j];
                    ref var node = ref nodeId.GetNode();

                    if (info.m_class == node.Info.m_class && (position - node.m_position).magnitude < 0.5f)
                        return true;
                }
            }

            nodeId = 0;
            return false;
        }
        private static int MinCell(float value) => Mathf.Max((int)((value - 16f) / 64f + 135f) - 1, 0);
        private static int MaxCell(float value) => Mathf.Min((int)((value + 16f) / 64f + 135f) + 1, 269);

        protected static bool CreateSegmentAuto(out ushort newSegmentId, NetInfo info, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir)
        {
            ref var startNode = ref startId.GetNode();
            ref var endNode = ref endId.GetNode();
            info = GetInfo(info, startNode.m_position, endNode.m_position, out var invert);

            if (invert)
                return CreateSegment(out newSegmentId, info, endId, startId, endDir, startDir, true);
            else
                return CreateSegment(out newSegmentId, info, startId, endId, startDir, endDir, false);
        }
        protected static bool CreateSegmentFromSource(out ushort newSegmentId, NetInfo info, NetInfo sourceInfo, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir)
        {
            var netInfoType = NetInfoType.Auto;

            {
                if (sourceInfo.m_netAI is RoadAI roadAI)
                    netInfoType = GetInfoType(sourceInfo, roadAI.m_info, roadAI.m_bridgeInfo, roadAI.m_elevatedInfo, roadAI.m_tunnelInfo, roadAI.m_slopeInfo, roadAI.m_connectedInfo, roadAI.m_connectedElevatedInfo);
                else if (sourceInfo.m_netAI is TrainTrackAI trainTrackAI)
                    netInfoType = GetInfoType(sourceInfo, trainTrackAI.m_info, trainTrackAI.m_bridgeInfo, trainTrackAI.m_elevatedInfo, trainTrackAI.m_tunnelInfo, trainTrackAI.m_slopeInfo, trainTrackAI.m_connectedInfo, trainTrackAI.m_connectedElevatedInfo);
                else if (sourceInfo.m_netAI is MetroTrackAI metroTrackAI)
                    netInfoType = GetInfoType(sourceInfo, metroTrackAI.m_info, metroTrackAI.m_bridgeInfo, metroTrackAI.m_elevatedInfo, metroTrackAI.m_tunnelInfo, metroTrackAI.m_slopeInfo, null, null);
                else if (sourceInfo.m_netAI is PedestrianWayAI pedestrianWayAI)
                    netInfoType = GetInfoType(sourceInfo, pedestrianWayAI.m_info, pedestrianWayAI.m_bridgeInfo, pedestrianWayAI.m_elevatedInfo, pedestrianWayAI.m_tunnelInfo, pedestrianWayAI.m_slopeInfo, null, null);
                else if (sourceInfo.m_netAI is PedestrianPathAI pedestrianPathAI)
                    netInfoType = GetInfoType(sourceInfo, pedestrianPathAI.m_info, pedestrianPathAI.m_bridgeInfo, pedestrianPathAI.m_elevatedInfo, pedestrianPathAI.m_tunnelInfo, pedestrianPathAI.m_slopeInfo, null, null);
            }

            var selecteInfo = info;
            {
                if (info.m_netAI is RoadAI roadAI)
                    selecteInfo = GetInfoFromType(netInfoType, roadAI.m_info, roadAI.m_bridgeInfo, roadAI.m_elevatedInfo, roadAI.m_tunnelInfo, roadAI.m_slopeInfo, roadAI.m_connectedInfo, roadAI.m_connectedElevatedInfo);
                else if (info.m_netAI is TrainTrackAI trainTrackAI)
                    selecteInfo = GetInfoFromType(netInfoType, trainTrackAI.m_info, trainTrackAI.m_bridgeInfo, trainTrackAI.m_elevatedInfo, trainTrackAI.m_tunnelInfo, trainTrackAI.m_slopeInfo, trainTrackAI.m_connectedInfo, trainTrackAI.m_connectedElevatedInfo);
                else if (info.m_netAI is MetroTrackAI metroTrackAI)
                    selecteInfo = GetInfoFromType(netInfoType, metroTrackAI.m_info, metroTrackAI.m_bridgeInfo, metroTrackAI.m_elevatedInfo, metroTrackAI.m_tunnelInfo, metroTrackAI.m_slopeInfo, null, null);
                else if (info.m_netAI is PedestrianWayAI pedestrianWayAI)
                    selecteInfo = GetInfoFromType(netInfoType, pedestrianWayAI.m_info, pedestrianWayAI.m_bridgeInfo, pedestrianWayAI.m_elevatedInfo, pedestrianWayAI.m_tunnelInfo, pedestrianWayAI.m_slopeInfo, null, null);
                else if (info.m_netAI is PedestrianPathAI pedestrianPathAI)
                    selecteInfo = GetInfoFromType(netInfoType, pedestrianPathAI.m_info, pedestrianPathAI.m_bridgeInfo, pedestrianPathAI.m_elevatedInfo, pedestrianPathAI.m_tunnelInfo, pedestrianPathAI.m_slopeInfo, null, null);
            }
            selecteInfo ??= info;

            return CreateSegment(out newSegmentId, selecteInfo, startId, endId, startDir, endDir, false);
        }
        protected static bool CreateSegment(out ushort newSegmentId, NetInfo info, ushort startId, ushort endId, Vector3 startDir, Vector3 endDir, bool invert = false) => Singleton<NetManager>.instance.CreateSegment(out newSegmentId, ref Singleton<SimulationManager>.instance.m_randomizer, info, startId, endId, startDir, endDir, Singleton<SimulationManager>.instance.m_currentBuildIndex, Singleton<SimulationManager>.instance.m_currentBuildIndex, invert);
        protected static NetInfo GetInfo(NetInfo info, Vector3 startPos, Vector3 endPos, out bool invert)
        {
            var startElevated = startPos.y - Singleton<TerrainManager>.instance.SampleRawHeightSmooth(startPos);
            var endElevated = endPos.y - Singleton<TerrainManager>.instance.SampleRawHeightSmooth(endPos);
            if (0f < startElevated && startElevated <= 1f)
                startElevated = 0f;
            if (0f < endElevated && endElevated <= 1f)
                endElevated = 0f;

            var minElevated = Mathf.Min(startElevated, endElevated);
            var maxElevated = Mathf.Max(startElevated, endElevated);

            var error = ToolBase.ToolErrors.None;
            var selectedInfo = info.m_netAI.GetInfo(minElevated, maxElevated, (endPos - startPos).magnitude, false, false, false, false, ref error);
            selectedInfo = error == ToolBase.ToolErrors.None ? selectedInfo : info;

            invert = selectedInfo.m_netAI.IsUnderground() && startElevated > -8f && endElevated <= -8f;
            return selectedInfo;
        }
        private static NetInfoType GetInfoType(NetInfo source, NetInfo normal, NetInfo bridge, NetInfo elevated, NetInfo tunnel, NetInfo slope, NetInfo connected, NetInfo connectedElevated)
        {
            if (source == normal)
                return NetInfoType.Normal;
            else if (source == bridge)
                return NetInfoType.Bridge;
            else if (source == elevated)
                return NetInfoType.Elevated;
            else if (source == tunnel)
                return NetInfoType.Tunnel;
            else if (source == slope)
                return NetInfoType.Slope;
            else if (source == connected)
                return NetInfoType.Connected;
            else if (source == connectedElevated)
                return NetInfoType.ConnectedElevated;
            else
                return NetInfoType.Normal;
        }
        private static NetInfo GetInfoFromType(NetInfoType netInfoType, NetInfo normal, NetInfo bridge, NetInfo elevated, NetInfo tunnel, NetInfo slope, NetInfo connected, NetInfo connectedElevated)
        {
            switch (netInfoType)
            {
                case NetInfoType.Normal: return normal;
                case NetInfoType.Bridge: return bridge;
                case NetInfoType.Elevated: return elevated;
                case NetInfoType.Tunnel: return tunnel;
                case NetInfoType.Slope: return slope;
                case NetInfoType.Connected: return connected;
                case NetInfoType.ConnectedElevated: return connectedElevated;
                default: return normal;
            }
        }
        enum NetInfoType
        {
            Auto,
            Normal,
            Bridge,
            Elevated,
            Tunnel,
            Slope,
            Connected,
            ConnectedElevated,
        }

        #endregion

        #region REMOVE

        protected static void RemoveNode(ushort nodeId) => Singleton<NetManager>.instance.ReleaseNode(nodeId);
        protected static void RemoveSegment(ushort segmentId, bool keepNodes = true) => Singleton<NetManager>.instance.ReleaseSegment(segmentId, keepNodes);
        protected static void ReleaseSegmentBlock(ref ushort segmentBlock)
        {
            if (segmentBlock != 0)
            {
                ZoneManager.instance.ReleaseBlock(segmentBlock);
                segmentBlock = 0;
            }
        }

        #endregion

        #region CHANGE

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
                UpdateZones(segmentId);
                NetManager.instance.UpdateSegment(segmentId);
            }
        }
        protected static void SetSegmentDirection(ushort nodeId, ushort anotherNodeId, Vector3 direction)
        {
            if (NetExtension.GetCommonSegment(nodeId, anotherNodeId, out var commonId))
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
        protected static void UpdateZones(ushort segmentId)
        {
            ref var segment = ref segmentId.GetSegment();
            if (segment.Info.m_netAI is RoadAI roadAI)
            {
                ReleaseSegmentBlock(ref segment.m_blockStartLeft);
                ReleaseSegmentBlock(ref segment.m_blockStartRight);
                ReleaseSegmentBlock(ref segment.m_blockEndLeft);
                ReleaseSegmentBlock(ref segment.m_blockEndRight);
                roadAI.CreateZoneBlocks(segmentId, ref segment);
            }
        }
        protected delegate void DirectionGetterDelegate<Type>(Type first, Type second, out Vector3 firstDir, out Vector3 secondDir);
        protected delegate Vector3 PositionGetterDelegate<Type>(ref Type item);
        protected delegate void PositionSetterDelegate<Type>(ref Type item, Vector3 position);

        protected static void SetSlope<Type>(Type[] items, float startY, float endY, PositionGetterDelegate<Type> positionGetter, DirectionGetterDelegate<Type> directionGetter, PositionSetterDelegate<Type> positionSetter, out float deltaHeight)
        {
            var list = new List<ITrajectory>();

            for (var i = 1; i < items.Length; i += 1)
            {
                var startPos = positionGetter(ref items[i - 1]);
                var endPos = positionGetter(ref items[i]);
                directionGetter(items[i - 1], items[i], out var startDir, out var endDir);

                startPos.y = 0;
                endPos.y = 0;
                startDir = startDir.MakeFlatNormalized();
                endDir = endDir.MakeFlatNormalized();

                list.Add(new BezierTrajectory(startPos, startDir, endPos, endDir));
            }

            var sumLength = list.Sum(t => t.Length);
            var currentLength = 0f;
            deltaHeight = (endY - startY) / sumLength;

            for (var i = 1; i < items.Length - 1; i += 1)
            {
                currentLength += list[i - 1].Length;
                var position = positionGetter(ref items[i]);
                position.y = Mathf.Lerp(startY, endY, currentLength / sumLength);
                positionSetter(ref items[i], position);
            }
        }
        protected static void SetSlope<Type>(Type[] items, PositionGetterDelegate<Type> positionGetter, DirectionGetterDelegate<Type> directionGetter, PositionSetterDelegate<Type> positionSetter)
        {
            var startY = positionGetter(ref items[0]).y;
            var endY = positionGetter(ref items[items.Length - 1]).y;

            SetSlope(items, startY, endY, positionGetter, directionGetter, positionSetter, out _);
        }
        protected static void SetSlope(Point[] points, float startY, float endY)
        {
            SetSlope(points, startY, endY, PositionGetter, DirectionGetter, PositionSetter, out var deltaHeight);

            points[0].Position.y = startY;
            points[points.Length - 1].Position.y = endY;

            for (var i = 1; i < points.Length - 1; i += 1)
            {
                points[i].ForwardDirection = points[i].ForwardDirection.SetHeight(deltaHeight);
                points[i].BackwardDirection = points[i].BackwardDirection.SetHeight(-deltaHeight);
            }
        }
        protected static void SetTerrain(Point[] points, float startY, float endY)
        {
            var hasWater = new bool[points.Length];
            for (var i = 0; i < points.Length; i += 1)
            {
                hasWater[i] = TerrainManager.instance.HasWater(XZ(points[i].Position));
                if (!hasWater[i])
                    points[i].Position = points[i].Position.SetHeight(TerrainManager.instance.SampleRawHeightSmooth(points[i].Position));
            }
            points[0].Position.y = startY;
            points[points.Length - 1].Position.y = endY;

            if (hasWater.All(i => i))
                SetSlope(points, startY, endY);
            else
            {
                var index = 0;
                while (index < points.Length)
                {
                    var startI = Array.FindIndex(hasWater, index, i => i);
                    if (startI == -1)
                        break;
                    startI = Math.Max(startI - 1, 0);
                    var endI = Array.FindIndex(hasWater, startI + 1, i => !i);
                    if (endI == -1)
                        endI = points.Length - 1;

                    var toSlope = new Point[endI - startI + 1];
                    Array.Copy(points, startI, toSlope, 0, toSlope.Length);
                    SetSlope(toSlope, toSlope[0].Position.y, toSlope[toSlope.Length - 1].Position.y);
                    Array.Copy(toSlope, 0, points, startI, toSlope.Length);

                    index = endI + 1;
                }

                for (var i = 1; i < points.Length - 1; i += 1)
                {
                    var before = new BezierTrajectory(points[i - 1].Position.MakeFlat(), points[i - 1].ForwardDirection, points[i].Position.MakeFlat(), points[i].BackwardDirection);
                    var after = new BezierTrajectory(points[i].Position.MakeFlat(), points[i].ForwardDirection, points[i + 1].Position.MakeFlat(), points[i + 1].BackwardDirection);

                    var beforeTan = (points[i].Position.y - points[i - 1].Position.y) / before.Length;
                    var afterTan = (points[i + 1].Position.y - points[i].Position.y) / after.Length;
                    var tan = (beforeTan + afterTan) / 2f;

                    points[i].ForwardDirection = points[i].ForwardDirection.SetHeight(tan);
                    points[i].BackwardDirection = points[i].BackwardDirection.SetHeight(-tan);
                }

                if (points.Length == 2)
                {
                    var line = new BezierTrajectory(points[0].Position.MakeFlat(), points[0].ForwardDirection, points[1].Position.MakeFlat(), points[1].BackwardDirection);
                    var tan = (points[1].Position.y - points[0].Position.y) / line.Length;

                    points[0].ForwardDirection = points[0].ForwardDirection.SetHeight(tan);
                    points[1].BackwardDirection = points[1].BackwardDirection.SetHeight(-tan);
                }
            }
        }

        private static Vector3 PositionGetter(ref Point point) => point.Position;
        private static void DirectionGetter(Point first, Point second, out Vector3 firstDir, out Vector3 secondDir)
        {
            firstDir = first.ForwardDirection;
            secondDir = second.BackwardDirection;
        }
        private static void PositionSetter(ref Point point, Vector3 position) => point.Position = position;

        #endregion

        #region TERRAIN

        protected static Rect GetTerrainRect(params ushort[] segmentIds) => segmentIds.Select(i => (ITrajectory)new BezierTrajectory(i)).GetRect();
        protected static void UpdateTerrain(params ushort[] segmentIds)
        {
            if (segmentIds.Length != 0)
                UpdateTerrain(GetTerrainRect(segmentIds));
        }
        protected static void UpdateTerrain(Rect rect) => TerrainModify.UpdateArea(rect.xMin, rect.yMin, rect.xMax, rect.yMax, true, true, false);

        #endregion

        #region MONEY

        protected static bool EnoughMoney(int amount) => EconomyManager.instance.PeekResource(EconomyManager.Resource.Construction, amount) >= amount;
        protected static void FetchMoney(int amount, NetInfo info) => EconomyManager.instance.FetchResource(EconomyManager.Resource.Construction, amount, info.m_class);
        protected static void AddMoney(int amount, NetInfo info) => EconomyManager.instance.AddResource(EconomyManager.Resource.RefundAmount, amount, info.m_class);
        protected static void ChangeMoney(int amount, NetInfo info)
        {
            if (amount > 0)
                FetchMoney(amount, info);
            else
                AddMoney(-amount, info);
        }

        protected static int GetCost(Point[] points, NetInfo info)
        {
            var cost = 0;

            for (var i = 1; i < points.Length; i += 1)
            {
                info = GetInfo(info, points[i - 1].Position, points[i].Position, out _);
                var trajectory = GetTrajectory(points[i - 1], points[i]);
                cost += GetCost(trajectory.Length, info);
            }

            return cost;
        }
        protected static int GetCost(float length, NetInfo info) => Mathf.RoundToInt(length / 8f) * info.GetConstructionCost();

        #endregion

        #region LABELS

        protected InfoLabel AddLabel(float size = 2f, Color? color = null)
        {
            var view = UIView.GetAView();
            var label = view.AddUIComponent(typeof(InfoLabel)) as InfoLabel;
            label.color = color ?? CommonColors.White;
            label.textScale = size;
            label.HorizontalAlignment = UIHorizontalAlignment.Center;
            label.SendToBack();
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

        #endregion

        #region RENDER

        protected void RenderSegmentNodes(RenderManager.CameraInfo cameraInfo, Func<ushort, bool> isAllow = null)
        {
            if (IsHoverSegment)
            {
                var data = new OverlayData(cameraInfo) { Color = CommonColors.Blue, RenderLimit = Underground };

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

                    while (nodeId != 0u && count < NetManager.MAX_NODE_COUNT)
                    {
                        ref var node = ref nodeId.GetNode();
                        var magnitude = (XZ(node.m_position) - xzPosition).magnitude;
                        if (!Underground ^ node.m_flags.IsSet(NetNode.Flags.Underground) && magnitude <= radius && isAllow?.Invoke(nodeId) != false)
                        {
                            var color = CommonColors.Blue;
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
            if (!CheckItemClass(nodeId.GetNode().Info.GetConnectionClass()))
                return false;

            if (IsHoverNode)
                return nodeId != HoverNode.Id;
            else if (IsHoverSegment)
                return !HoverSegment.Id.GetSegment().Contains(nodeId);
            else
                return true;
        }
        protected static BezierTrajectory GetTrajectory(Point first, Point second) => new BezierTrajectory(first.Position, first.ForwardDirection, second.Position, second.BackwardDirection);
        protected void RenderPartsOverlay(RenderManager.CameraInfo cameraInfo, List<Point> points, Color? color = null, float? width = null)
        {
            var data = new OverlayData(cameraInfo) { Color = color, Width = width, RenderLimit = Underground, Cut = true };
            for (var i = 1; i < points.Count; i += 1)
            {
                if (!points[i - 1].IsEmpty && !points[i].IsEmpty)
                    GetTrajectory(points[i - 1], points[i]).Render(data);
            }
        }
        protected void RenderPartsArrows(RenderManager.CameraInfo cameraInfo, List<Point> points, NetInfo info, bool invert = false)
        {
            if (info.m_laneTypes != NetInfo.LaneType.None && IsInvertable(info))
            {
                for (var i = 1; i < points.Count; i += 1)
                {
                    if (!points[i - 1].IsEmpty && !points[i].IsEmpty)
                        RenderArrows(cameraInfo, points[i - 1], points[i], invert);
                }
            }
        }
        protected void RenderPartsGeometry(Point[] points, NetInfo info, bool invert = false)
        {
            for (var i = 1; i < points.Length; i += 1)
            {
                if (!points[i - 1].IsEmpty && !points[i].IsEmpty)
                    RenderSegment(points[i - 1], points[i], info, invert);
            }
        }
        protected void RenderSegment(Point start, Point end, NetInfo info, bool forceInvert)
        {
            info = GetInfo(info, start.Position, end.Position, out var tunnelInvert);

            var startNormal = start.ForwardDirection.Turn90(true).MakeFlatNormalized();
            var endNormal = end.BackwardDirection.Turn90(false).MakeFlatNormalized();

            var right = new BezierTrajectory(start.Position + startNormal * info.m_halfWidth, start.ForwardDirection, end.Position + endNormal * info.m_halfWidth, end.BackwardDirection).Trajectory;
            var left = new BezierTrajectory(start.Position - startNormal * info.m_halfWidth, start.ForwardDirection, end.Position - endNormal * info.m_halfWidth, end.BackwardDirection).Trajectory;

            var position = (start.Position + end.Position) / 2f;
            var vScale = info.m_netAI.GetVScale();
            var rightMatrix = NetSegment.CalculateControlMatrix(right.a, right.b, right.c, right.d, left.a, left.b, left.c, left.d, position, vScale);
            var leftMatrix = NetSegment.CalculateControlMatrix(left.a, left.b, left.c, left.d, right.a, right.b, right.c, right.d, position, vScale);

            var instance = NetManager.instance;

            instance.m_materialBlock.Clear();
            instance.m_materialBlock.SetMatrix(instance.ID_RightMatrix, rightMatrix);
            instance.m_materialBlock.SetMatrix(instance.ID_LeftMatrix, leftMatrix);
            instance.m_materialBlock.SetVector(instance.ID_ObjectIndex, RenderManager.DefaultColorLocation);
            instance.m_materialBlock.SetColor(instance.ID_Color, info.m_color);

            if (info.m_requireSurfaceMaps)
            {
                TerrainManager.instance.GetSurfaceMapping(position, out var surfaceTexA, out var surfaceTexB, out var surfaceMapping);
                instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexA, surfaceTexA);
                instance.m_materialBlock.SetTexture(instance.ID_SurfaceTexB, surfaceTexB);
                instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, surfaceMapping);
            }
            else if (info.m_requireHeightMap)
            {
                TerrainManager.instance.GetHeightMapping(position, out var heightMap, out var heightMapping, out var surfaceMapping);
                instance.m_materialBlock.SetTexture(instance.ID_HeightMap, heightMap);
                instance.m_materialBlock.SetVector(instance.ID_HeightMapping, heightMapping);
                instance.m_materialBlock.SetVector(instance.ID_SurfaceMapping, surfaceMapping);
            }

            if (info.m_netAI is RoadBaseAI roadAI && !roadAI.IsOverground())
                position += Vector3.up;

            foreach (var segment in info.m_segments)
            {
                if (segment.CheckFlags(NetSegment.Flags.None, out var invert))
                {
                    var isTunnel = info.m_netAI.IsUnderground();
                    if (invert ^ (isTunnel ? tunnelInvert : forceInvert))
                    {
                        var scale = new Vector4(-0.5f / info.m_halfWidth, -1f / info.m_segmentLength, 1f, 1f);
                        instance.m_materialBlock.SetVector(instance.ID_MeshScale, scale);
                    }
                    else
                    {
                        var scale = new Vector4(0.5f / info.m_halfWidth, 1f / info.m_segmentLength, 1f, 1f);
                        instance.m_materialBlock.SetVector(instance.ID_MeshScale, scale);
                    }

                    Graphics.DrawMesh(segment.m_segmentMesh, position, Quaternion.identity, segment.m_segmentMaterial, segment.m_layer, null, 0, instance.m_materialBlock);
                }
            }
        }
        protected void RenderArrows(RenderManager.CameraInfo cameraInfo, Point start, Point end, bool invert)
        {
            if ((start.Position - end.Position).magnitude < 10f)
                return;

            var properties = Singleton<GameAreaManager>.instance.m_properties;
            if (properties != null)
            {
                var trajectory = new BezierTrajectory(start.Position, start.ForwardDirection, end.Position, end.BackwardDirection);
                var arrowPos = trajectory.Position(0.5f);
                var arrowDir = trajectory.Tangent(0.5f).MakeFlatNormalized();
                var arrowNormal = arrowDir.Turn90(true);

                if (!invert)
                {
                    var quad = new Quad3()
                    {
                        a = arrowPos,
                        b = arrowPos + 2.5f * arrowNormal,
                        c = arrowPos + 2.5f * arrowDir,
                        d = arrowPos - 2.5f * arrowNormal,
                    };
                    Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, CommonColors.White192, quad, -10f, 1280f, false, false);

                    quad = new Quad3()
                    {
                        a = arrowPos - 0.75f * arrowNormal,
                        b = arrowPos - 0.75f * arrowNormal - 4f * arrowDir,
                        c = arrowPos + 0.75f * arrowNormal - 4f * arrowDir,
                        d = arrowPos + 0.75f * arrowNormal,
                    };
                    Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, CommonColors.White192, quad, -10f, 1280f, false, false);
                }
                else
                {
                    var quad = new Quad3()
                    {
                        a = arrowPos - 2.5f * arrowNormal,
                        b = arrowPos - 2.5f * arrowDir,
                        c = arrowPos + 2.5f * arrowNormal,
                        d = arrowPos,
                    };
                    Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, CommonColors.White192, quad, -10f, 1280f, false, false);

                    quad = new Quad3()
                    {
                        a = arrowPos + 0.75f * arrowNormal,
                        b = arrowPos + 0.75f * arrowNormal + 4f * arrowDir,
                        c = arrowPos - 0.75f * arrowNormal + 4f * arrowDir,
                        d = arrowPos - 0.75f * arrowNormal,
                    };
                    Singleton<RenderManager>.instance.OverlayEffect.DrawQuad(cameraInfo, CommonColors.White192, quad, -10f, 1280f, false, false);
                }
            }
        }

        #endregion

        #region EFFECTS

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

        #endregion

        protected Vector3 GetMousePosition(float height) => Underground ? Tool.Ray.GetRayPosition(height, out _) : Tool.MouseWorldPosition;
        protected NetInfo GetNetInfo()
        {
            var info = ToolsModifierControl.toolController.Tools.OfType<NetTool>().FirstOrDefault().Prefab?.m_netAI?.m_info;
            return info != null && CheckItemClass(info.GetConnectionClass()) ? info : null;
        }
        protected bool IsInvertable(NetInfo info)
        {
            if (info.m_netAI is RoadBaseAI)
                return info.m_forwardVehicleLaneCount != info.m_backwardVehicleLaneCount;
            else if (info.m_netAI is DecorationWallAI)
                return true;
            else
                return false;
        }

        public struct Point
        {
            public Vector3 Position;
            public Vector3 ForwardDirection;
            public Vector3 BackwardDirection;
            public Vector3 Direction
            {
                set
                {
                    ForwardDirection = value;
                    BackwardDirection = -value;
                }
            }
            public bool IsEmpty => Position == Vector3.zero && ForwardDirection == Vector3.zero && BackwardDirection == Vector3.zero;

            public Point(Vector3 position, Vector3 forwardDirection, Vector3 backwardDirection)
            {
                Position = position;
                ForwardDirection = forwardDirection;
                BackwardDirection = backwardDirection;
            }
            public Point(Vector3 position, Vector3 direction) : this(position, Vector3.zero, Vector3.zero)
            {
                Direction = direction;
            }

            public static Point Empty => new Point(Vector3.zero, Vector3.zero);
        }

        public class BaseStraight
        {
            public StraightTrajectory Trajectory { get; }
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

            public BaseStraight(Vector3 start, Vector3 end, Vector3 labelDir, InfoLabel label, float height)
            {
                Trajectory = new StraightTrajectory(start.SetHeight(height), end.SetHeight(height));
                LabelDir = labelDir;
                Label = label;
            }

            protected virtual string GetText() => GetLengthString(Trajectory.Length);
            public void Update(float shift, bool show)
            {
                if (Label is InfoLabel label)
                {
                    label.Show = show;
                    if (show)
                    {
                        label.text = GetText();
                        label.Direction = LabelDir;
                        label.WorldPosition = Trajectory.Position(0.5f) + label.Direction * shift;

                        label.UpdateInfo();
                    }
                }
            }
        }
        public class MeasureStraight : BaseStraight
        {
            public float MeasureLength { get; }
            public MeasureStraight(Vector3 start, Vector3 end, Vector3 labelDir, float measureLength, InfoLabel label, float height) : base(start, end, labelDir, label, height)
            {
                MeasureLength = measureLength;
            }

            public void Update(bool show) => Update(MeasureLength, show);
            public void Render(RenderManager.CameraInfo cameraInfo, Color color, Color colorArrow, bool underground) => Trajectory.RenderMeasure(cameraInfo, 0f, MeasureLength, LabelDir, color, colorArrow, underground);
        }

        public class BaseCurve
        {
            protected BezierTrajectory Trajectory { get; }
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

            public BaseCurve(Bezier3 bezier, InfoLabel label)
            {
                Trajectory = new BezierTrajectory(bezier);
                Label = label;
            }

            protected virtual string GetText() => GetLengthString(Trajectory.Length);
            public void Update(float shift, bool show)
            {
                if (Label is InfoLabel label)
                {
                    label.Show = show;
                    if (show)
                    {
                        label.text = GetText();
                        label.Direction = Trajectory.Tangent(0.5f).Turn90(true).normalized;
                        label.WorldPosition = Trajectory.Position(0.5f) + label.Direction * shift;

                        label.UpdateInfo();
                    }
                }
            }
        }

        public class MeasureCurve : BaseCurve
        {
            public float MeasureLength { get; }

            public MeasureCurve(Bezier3 bezier, InfoLabel label, float measureLength) : base(bezier, label)
            {
                MeasureLength = measureLength;
            }

            public void Update(bool show) => Update(MeasureLength, show);
            public void Render(RenderManager.CameraInfo cameraInfo, Color color, Color colorArrow, bool underground) => Trajectory.RenderMeasure(cameraInfo, 0f, MeasureLength, color, colorArrow, underground);
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

        [Description(nameof(Localize.Mode_CreateCurve))]
        CreateCurve = CreateConnection << 1,

        [Description(nameof(Localize.Mode_CreateParallerl))]
        CreateParallel = CreateCurve << 1,


        [NotItem]
        Line = SlopeNode | ArrangeAtLine,

        [NotItem]
        Create = CreateLoop | CreateConnection | CreateCurve | CreateParallel,

        [NotItem]
        Any = int.MaxValue,
    }

    public interface ISelectToolMode
    {
        public void IgnoreSelected();
    }
    public interface ICostMode
    {
        public int Cost { get; }
    }
    public interface IInvertNetworkMode
    {
        public void SetInvert();
    }

    public class InfoLabel : CustomUILabel
    {
        public Vector3 WorldPosition { get; set; }
        public Vector3 Direction { get; set; }
        public new bool Show { get; set; }

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

            if (isVisible = Show && startScreenPosition.z > 0f)
            {
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
}
