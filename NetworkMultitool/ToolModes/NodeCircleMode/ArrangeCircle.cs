using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    public class ArrangeCircleMode : BaseNodeCircle
    {
        public override ToolModeType Type => ToolModeType.ArrangeAtCircle;
        private bool IsCompleteHover => Nodes.Count != 0 && ((HoverNode.Id == Nodes[0].Id && AddState == AddResult.InEnd) || (HoverNode.Id == Nodes[Nodes.Count - 1].Id && AddState == AddResult.InStart));

        protected override string GetInfo()
        {
            if (IsHoverNode && IsCompleteHover)
                return Localize.Mode_ArrangeCircle_Info_ClickToComplite.AddActionColor() + StepOverInfo;
            else
                return base.GetInfo();
        }
        protected override void Complite() => Tool.SetMode(ToolModeType.ArrangeAtCircleComplete);
        protected override void Apply() { }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RenderNearNodes(cameraInfo);
            RenderSegmentNodes(cameraInfo, AllowRenderNode);

            if (!IsHoverNode)
            {
                foreach (var node in Nodes)
                    node.Render(new OverlayData(cameraInfo) { Color = Colors.White, RenderLimit = Underground });
            }
            else if (IsCompleteHover)
            {
                foreach (var node in Nodes)
                    node.Render(new OverlayData(cameraInfo) { Color = Colors.Purple, RenderLimit = Underground });
                foreach (var node in ToAdd)
                    node.Render(new OverlayData(cameraInfo) { Color = Colors.Purple, RenderLimit = Underground });
            }
            else
            {
                RenderExistOverlay(cameraInfo);
                RenderAddedOverlay(cameraInfo);
            }
        }
    }
    public abstract class BaseArrangeCircleCompleteMode : BaseNetworkMultitoolMode
    {
        protected override bool IsReseted => true;
        protected override bool CanSwitchUnderground => false;

        protected List<CirclePoint> Nodes { get; } = new List<CirclePoint>();

        protected Vector3 Center { get; set; }
        protected float Radius { get; set; }
        private bool IsClockWise { get; set; }

        private List<bool> States { get; } = new List<bool>();
        protected bool IsWrongOrder => States.Any(s => !s);
        protected bool IsBigDelta { get; private set; }
        protected Color CircleColor => IsWrongOrder ? Colors.Red : (IsBigDelta ? Colors.Orange : Colors.Green);

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
                    _label.Show = true;
                }
            }
        }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Nodes.Clear();

            if (prevMode is ArrangeCircleMode arrangeMode)
            {
                var points = arrangeMode.SelectedNodes.Select(n => n.Id.GetNode().m_position).ToArray();
                var sumAngle = 0f;
                for (var i = 0; i < points.Length; i += 1)
                {
                    var first = points[i] - points[(i - 1 + points.Length) % points.Length];
                    var second = points[(i + 1) % points.Length] - points[i];
                    var angle = MathExtention.GetAngle(first, second);
                    sumAngle += angle;
                }
                IsClockWise = sumAngle >= 0f;

                Calculate(IsClockWise ? arrangeMode.SelectedNodes : arrangeMode.SelectedNodes.Reverse(), n => n.Id.GetNode().m_position, n => n.Id);
            }
            else if (prevMode is BaseArrangeCircleCompleteMode arrangeCompliteMode)
            {
                Nodes.AddRange(arrangeCompliteMode.Nodes);
                Center = arrangeCompliteMode.Center;
                Radius = arrangeCompliteMode.Radius;
                IsClockWise = arrangeCompliteMode.IsClockWise;
            }

            States.Clear();
            States.AddRange(Nodes.Select(_ => true));

            Label ??= AddLabel();
        }
        protected override void ClearLabels()
        {
            base.ClearLabels();
            Label = null;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            var anglesList = new List<float>(Nodes.Count);
            var idxs = new List<int>(Nodes.Count);

            for (var i = 0; i < Nodes.Count; i += 1)
            {
                var index = anglesList.BinarySearch(Nodes[i].Angle);
                if (index < 0)
                    index = ~index;
                anglesList.Insert(index, Nodes[i].Angle);
                idxs.Insert(index, i);
            }

            IsBigDelta = false;
            for (var j = 0; j < idxs.Count; j += 1)
            {
                var i = (j - 1 + idxs.Count) % idxs.Count;
                var k = (j + 1) % idxs.Count;
                var delta1 = (idxs[j] != 0 ? idxs[j] : idxs.Count) - idxs[i];
                var delta2 = (idxs[k] != 0 ? idxs[k] : idxs.Count) - idxs[j];
                States[idxs[j]] = delta1 == 1 || delta2 == 1;
                IsBigDelta |= (anglesList[k] + (k == 0 ? Mathf.PI * 2f : 0f) - anglesList[j]) > Mathf.PI / 2f;
            }

            if (Label is InfoLabel label)
            {
                label.text = GetLengthString(Radius);
                label.Direction = Tool.CameraDirection;
                label.WorldPosition = Center + Tool.CameraDirection * 5f;
            }
        }
        protected virtual void Calculate<Type>(IEnumerable<Type> source, Func<Type, Vector3> posGetter, Func<Type, ushort> idGetter)
        {
            var points = source.Select(s => posGetter(s)).ToArray();
            var centre = Vector3.zero;
            var radius = 1000f;

            for (var i = 0; i < points.Length; i += 1)
            {
                for (var j = i + 1; j < points.Length; j += 1)
                {
                    GetCircle2Points(points, i, j, ref centre, ref radius);

                    for (var k = j + 1; k < points.Length; k += 1)
                        GetCircle3Points(points, i, j, k, ref centre, ref radius);
                }
            }

            Center = centre;
            Radius = radius;

            foreach (var item in source)
                Nodes.Add(GetPoint(Center, idGetter(item), posGetter(item)));
        }
        protected CirclePoint GetPoint(Vector3 center, ushort id, Vector3 position)
        {
            position.y = center.y;
            var newPos = center + (position - center).MakeFlatNormalized() * Radius;
            var angle = (newPos - center).AbsoluteAngle();
            return new CirclePoint(id, angle);
        }
        private void GetCircle2Points(Vector3[] points, int i, int j, ref Vector3 centre, ref float radius)
        {
            var newCentre = (points[i] + points[j]) / 2;
            var newRadius = (points[i] - points[j]).magnitude / 2;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private void GetCircle3Points(Vector3[] points, int i, int j, int k, ref Vector3 centre, ref float radius)
        {
            var pos1 = (points[i] + points[j]) / 2;
            var pos2 = (points[j] + points[k]) / 2;

            var dir1 = (points[i] - points[j]).Turn90(true).normalized;
            var dir2 = (points[j] - points[k]).Turn90(true).normalized;

            Line2.Intersect(XZ(pos1), XZ(pos1 + dir1), XZ(pos2), XZ(pos2 + dir2), out float p, out _);
            var newCentre = pos1 + dir1 * p;
            var newRadius = (newCentre - points[i]).magnitude;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j, k))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private bool AllPointsInCircle(Vector3[] points, Vector3 centre, float radius, params int[] ignore)
        {
            for (var i = 0; i < points.Length; i += 1)
            {
                if (ignore.Any(j => j == i))
                    continue;

                if ((centre - points[i]).magnitude > radius)
                    return false;
            }

            return true;
        }
        protected void RenderCircle(RenderManager.CameraInfo cameraInfo, Color color) => Center.RenderCircle(new OverlayData(cameraInfo) { Width = Radius * 2f, Color = color, RenderLimit = Underground });
        protected void RenderCenter(RenderManager.CameraInfo cameraInfo, Color color) => Center.RenderCircle(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground }, 5f, 0f);
        protected void RenderHoverCenter(RenderManager.CameraInfo cameraInfo, Color color) => Center.RenderCircle(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground }, 7f, 5f);
        protected void RenderNodes(RenderManager.CameraInfo cameraInfo, int hover = -1)
        {
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                var color = States[i] ? Colors.White : Colors.Red;
                Nodes[i].GetPositions(Center, Radius, out var currentPos, out var newPos);
                if ((currentPos - newPos).sqrMagnitude > 1f)
                {
                    var line = new StraightTrajectory(currentPos, newPos);
                    if (!States[i])
                        line.Render(new OverlayData(cameraInfo) { Width = 0.5f, RenderLimit = Underground });
                    line.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
                }
                if (i == hover)
                    newPos.RenderCircle(new OverlayData(cameraInfo) { Color = States[i] ? Colors.Blue : Colors.White, RenderLimit = Underground }, 4f, 2f);
                newPos.RenderCircle(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground }, 2f, 0f);
            }
        }

        protected struct CirclePoint
        {
            public ushort Id;
            public float Angle;

            public CirclePoint(ushort id, float angle)
            {
                Id = id;
                Angle = angle % (Mathf.PI * 2f) + (angle < 0f ? Mathf.PI * 2f : 0f);
            }

            public void GetPositions(Vector3 center, float radius, out Vector3 currentPos, out Vector3 newPos)
            {
                currentPos = Id.GetNode().m_position;
                currentPos.y = center.y;
                newPos = center + Angle.Direction() * radius;
            }
            public CirclePoint Turn(float delta) => new CirclePoint(Id, Angle + delta);
            public override string ToString() => $"{Id}: {Angle}";
        }
    }
    public class ArrangeCircleCompleteMode : BaseArrangeCircleCompleteMode
    {
        public static NetworkMultitoolShortcut ResetArrangeCircleShortcut { get; } = GetShortcut(KeyCode.R, nameof(ResetArrangeCircleShortcut), nameof(Localize.Settings_Shortcut_ResetArrangeCircle), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as ArrangeCircleCompleteMode)?.Recalculate(), alt: true);
        public static NetworkMultitoolShortcut DistributeEvenlyShortcut { get; } = GetShortcut(KeyCode.A, nameof(DistributeEvenlyShortcut), nameof(Localize.Settings_Shortcut_DistributeEvenly), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as ArrangeCircleCompleteMode)?.DistributeAllEvenly(), alt: true);
        public static NetworkMultitoolShortcut DistributeIntersectionsShortcut { get; } = GetShortcut(KeyCode.A, nameof(DistributeIntersectionsShortcut), nameof(Localize.Settings_Shortcut_DistributeIntersections), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as ArrangeCircleCompleteMode)?.DistributeIntersectionsEvenly(), ctrl: true, shift: true);
        public static NetworkMultitoolShortcut DistributeBetweenIntersectionsShortcut { get; } = GetShortcut(KeyCode.A, nameof(DistributeBetweenIntersectionsShortcut), nameof(Localize.Settings_Shortcut_DistributeBetweenIntersections), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as ArrangeCircleCompleteMode)?.DistributeBetweenIntersections(), alt: true, shift: true);

        public override ToolModeType Type => ToolModeType.ArrangeAtCircleComplete;

        private Vector3 DefaultCenter { get; set; }
        public bool IsHoverCenter { get; private set; }
        public bool IsHoverCircle { get; private set; }
        public int HoveredNode { get; private set; }
        public bool IsPressedCenter { get; private set; }
        public bool IsPressedCircle { get; private set; }
        public int PressedNode { get; private set; }

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return ApplyShortcut;
                yield return ResetArrangeCircleShortcut;
                yield return DistributeEvenlyShortcut;
                yield return DistributeIntersectionsShortcut;
                yield return DistributeBetweenIntersectionsShortcut;
            }
        }

        private Vector3 LastPos { get; set; }
        private float PosTime { get; set; }

        protected override string GetInfo()
        {
            if (IsHoverCenter)
                return
                    Localize.Mode_Info_ArrangeCircle_DragToMoveCenter + "\n" +
                    Localize.Mode_Info_ArrangeCircle_DoubleClickToResetCenter;
            else if (HoveredNode != -1)
                return
                    Localize.Mode_Info_ArrangeCircle_DragToMoveNode + "\n" +
                    string.Format(Localize.Mode_Info_ArrangeCircle_MoveAll, LocalizeExtension.Shift.AddInfoColor()) + "\n" +
                    Localize.Mode_Info_ArrangeCircle_DoubleClickToResetNode;
            else if (IsHoverCircle)
                return Localize.Mode_Info_ArrangeCircle_DragToChangeRadius;
            else
            {
                if (Tool.MousePosition != LastPos)
                {
                    LastPos = Tool.MousePosition;
                    PosTime = Time.realtimeSinceStartup;
                }

                var result = string.Empty;
                if (IsWrongOrder)
                    result += Localize.Mode_Info_ArrangeCircle_WrongOrder.AddErrorColor();
                else if (IsBigDelta)
                    result += Localize.Mode_Info_ArrangeCircle_BigDelta.AddWarningColor();

                if (!string.IsNullOrEmpty(result))
                    result += "\n\n";

                if (Time.realtimeSinceStartup - PosTime >= 2f)
                {
                    result +=
                        string.Format(Localize.Mode_Info_ArrangeCircle_PressToDistributeEvenly, DistributeEvenlyShortcut.AddInfoColor()) + "\n" +
                        string.Format(Localize.Mode_Info_ArrangeCircle_PressToDistributeIntersections, DistributeIntersectionsShortcut.AddInfoColor()) + "\n" +
                        string.Format(Localize.Mode_Info_ArrangeCircle_PressToDistributeBetweenIntersections, DistributeBetweenIntersectionsShortcut.AddInfoColor()) + "\n";
                }

                result +=
                    string.Format(Localize.Mode_Info_ArrangeCircle_PressToReset, ResetArrangeCircleShortcut.AddInfoColor()) + "\n" +
                    string.Format(Localize.Mode_Info_ArrangeCircle_Apply, ApplyShortcut.AddInfoColor());

                return result;
            }
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            IsHoverCenter = false;
            IsHoverCircle = false;
            HoveredNode = -1;
            IsPressedCenter = false;
            IsPressedCircle = false;
            PressedNode = -1;

            if (prevMode is ArrangeCircleCompleteMode mode)
            {
                Nodes.Clear();
                Calculate(mode.Nodes, n => n.Id.GetNode().m_position, n => n.Id);
            }
        }
        protected override void Calculate<Type>(IEnumerable<Type> source, Func<Type, Vector3> posGetter, Func<Type, ushort> idGetter)
        {
            base.Calculate(source, posGetter, idGetter);
            DefaultCenter = Center;
            DistributeIntersectionsEvenly();
        }
        private void Recalculate()
        {
            var nodes = new List<CirclePoint>(Nodes);
            Nodes.Clear();
            Calculate(nodes, n => n.Id.GetNode().m_position, n => n.Id);
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            var mousePosition = XZ(GetMousePosition(Center.y));
            var magnitude = (XZ(Center) - mousePosition).magnitude;
            IsHoverCenter = Tool.MouseRayValid && magnitude <= 5f;
            IsHoverCircle = Tool.MouseRayValid && Radius - 5f <= magnitude && magnitude <= Radius + 5f;
            HoveredNode = -1;
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                Nodes[i].GetPositions(Center, Radius, out _, out var position);
                if ((XZ(position) - mousePosition).sqrMagnitude <= 4f)
                {
                    HoveredNode = i;
                    break;
                }
            }
        }
        public override void OnMouseDown(Event e)
        {
            IsPressedCenter = IsHoverCenter;
            IsPressedCircle = IsHoverCircle;
            PressedNode = HoveredNode;
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsPressedCenter)
                Tool.SetMode(ToolModeType.ArrangeAtCircleMoveCenter);
            else if (PressedNode != -1)
                Tool.SetMode(ToolModeType.ArrangeAtCircleMoveNode);
            else if (IsPressedCircle)
                Tool.SetMode(ToolModeType.ArrangeAtCircleRadius);
        }
        public override void OnPrimaryMouseDoubleClicked(Event e)
        {
            if (IsHoverCenter)
                Center = DefaultCenter;
            else if (HoveredNode != -1)
                Nodes[HoveredNode] = GetPoint(DefaultCenter, Nodes[HoveredNode].Id, Nodes[HoveredNode].Id.GetNode().m_position);
        }
        public override void OnSecondaryMouseClicked() => Tool.SetMode(ToolModeType.ArrangeAtCircle);
        public override bool OnEscape()
        {
            Tool.SetMode(ToolModeType.ArrangeAtCircle);
            return true;
        }
        protected override void Apply()
        {
            if (!IsWrongOrder)
            {
                var nodes = Nodes.ToArray();
                var center = Center;
                var radius = Radius;
                SimulationManager.instance.AddAction(() =>
                {
                    Arrange(nodes, center, radius);
                    PlayAudio(true);

                    if(Tool.Mode is ArrangeCircleMode mode)
                        mode.NeedClearSelectionBuffer();
                });

                Tool.SetMode(ToolModeType.ArrangeAtCircle);
            }
        }
        private static void Arrange(CirclePoint[] nodes, Vector3 center, float radius)
        {
            var segmentIds = new ushort[nodes.Length];
            for (var i = 0; i < nodes.Length; i += 1)
                NetExtension.GetCommon(nodes[i].Id, nodes[(i + 1) % nodes.Length].Id, out segmentIds[i]);
            var terrainRect = GetTerrainRect(segmentIds);

            foreach (var node in nodes)
            {
                node.GetPositions(center, radius, out _, out var newPos);
                MoveNode(node.Id, newPos);
            }
            for (var i = 0; i < nodes.Length; i += 1)
            {
                SetDirection(nodes, i, (i + 1) % nodes.Length, center);
                SetDirection(nodes, i, (i + nodes.Length - 1) % nodes.Length, center);
            }
            for (var i = 0; i < nodes.Length; i += 1)
            {
                NetExtension.GetCommon(nodes[i].Id, nodes[(i + 1) % nodes.Length].Id, out var segmentId);
                UpdateZones(segmentId);
            }

            foreach (var node in nodes)
                NetManager.instance.UpdateNode(node.Id);

            UpdateTerrain(terrainRect);
        }
        private static void SetDirection(CirclePoint[] nodes, int i, int j, Vector3 center)
        {
            var centerDir = (nodes[i].Id.GetNode().m_position - center).MakeFlatNormalized();

            NetExtension.GetCommon(nodes[i].Id, nodes[j].Id, out var segmentId);
            ref var segment = ref segmentId.GetSegment();
            var direction = nodes[j].Id.GetNode().m_position - nodes[i].Id.GetNode().m_position;
            var newDirection = centerDir.Turn90(NormalizeCrossXZ(centerDir, direction) >= 0f);
            SetSegmentDirection(segmentId, segment.IsStartNode(nodes[i].Id), newDirection);
        }
        private void DistributeBetweenIntersections()
        {
            var intersections = new List<int>();
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                if (Nodes[i].Id.GetNode().CountSegments() >= 3)
                    intersections.Add(i);
            }

            if (intersections.Count == 1)
            {
                var index0 = intersections[0];
                var delta = Mathf.PI * 2f / Nodes.Count;
                for (var i = 1; i < Nodes.Count; i += 1)
                {
                    var index = (index0 + i) % Nodes.Count;
                    Nodes[index] = new CirclePoint(Nodes[index].Id, Nodes[index0].Angle + delta * i);
                }
            }
            else if (intersections.Count != 0)
            {
                for (var j = 0; j < intersections.Count; j += 1)
                {
                    var index1 = intersections[j];
                    var index2 = intersections[(j + 1) % intersections.Count];

                    var count = index2 + (index1 < index2 ? 0 : Nodes.Count) - index1;
                    var delta = (Nodes[index2].Angle + (Nodes[index1].Angle < Nodes[index2].Angle ? 0 : Mathf.PI * 2f) - Nodes[index1].Angle) / count;
                    for (var i = 1; i < count; i += 1)
                    {
                        var index = (index1 + i) % Nodes.Count;
                        Nodes[index] = new CirclePoint(Nodes[index].Id, Nodes[index1].Angle + delta * i);
                    }
                }
            }
            else
                DistributeAllEvenly();
        }
        private void DistributeIntersectionsEvenly()
        {
            var intersections = new List<int>();
            var nodes = new List<CirclePoint>();

            for (var i = 0; i < Nodes.Count; i += 1)
            {
                if (Nodes[i].Id.GetNode().CountSegments() >= 3)
                {
                    intersections.Add(i);
                    nodes.Add(Nodes[i]);
                }
            }

            DistributeEvenly(nodes, Radius);

            for (var i = 0; i < intersections.Count; i += 1)
                Nodes[intersections[i]] = nodes[i];

            DistributeBetweenIntersections();
        }
        private void DistributeAllEvenly() => DistributeEvenly(Nodes, Radius);
        private static void DistributeEvenly(List<CirclePoint> nodes, float radius)
        {
            var startI = 0;
            var maxDelta = 0f;
            for (var i = 0; i < nodes.Count; i += 1)
            {
                var thisAngle = nodes[i].Angle;
                var nextAngle = nodes[(i + 1) % nodes.Count].Angle;
                var delta = nextAngle + (nextAngle < thisAngle ? Mathf.PI * 2f : 0f) - thisAngle;
                if (delta > maxDelta)
                {
                    startI = i;
                    maxDelta = delta;
                }
            }

            var evenDelta = Mathf.PI * 2f / nodes.Count;
            var possibleDelta = maxDelta - evenDelta;
            var length = possibleDelta * radius;
            var count = Mathf.CeilToInt(length);

            var minI = 0;
            var minDelta = float.MaxValue;
            for (var i = 0; i <= count; i += 1)
            {
                var sumDelta = 0f;
                var startDelta = possibleDelta / count * i;
                for (var j = 0; j < nodes.Count; j += 1)
                {
                    var angle = (nodes[startI].Angle + startDelta + evenDelta * j) % (Mathf.PI * 2f);
                    var thisAngle = nodes[(startI + j) % nodes.Count].Angle;
                    var thisDelta = Mathf.Min(Mathf.Abs(angle - thisAngle), Mathf.Abs(angle + (Mathf.PI * 2f) - thisAngle), Mathf.Abs(angle - (Mathf.PI * 2f) - thisAngle));
                    sumDelta += thisDelta;

                    if (sumDelta > minDelta)
                        break;
                }

                if (sumDelta + 0.001f < minDelta || (sumDelta - minDelta < 0.001f && Math.Abs(count / 2 - i) < Math.Abs(count / 2 - minI)))
                {
                    minDelta = sumDelta;
                    minI = i;
                }
            }

            var startAngle = nodes[startI].Angle + possibleDelta / count * minI;
            for (var i = 0; i < nodes.Count; i += 1)
            {
                var index = (startI + i) % nodes.Count;
                nodes[index] = new CirclePoint(nodes[index].Id, startAngle + evenDelta * i);
            }
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var color = CircleColor;
            RenderCircle(cameraInfo, IsHoverCircle && !IsHoverCenter && HoveredNode == -1 ? Colors.Blue : color);
            RenderNodes(cameraInfo, IsHoverCenter ? -1 : HoveredNode);
            if (IsHoverCenter)
                RenderHoverCenter(cameraInfo, Colors.White);
            RenderCenter(cameraInfo, color);
        }
    }
    public class ArrangeCircleMoveCenterMode : BaseArrangeCircleCompleteMode
    {
        public override ToolModeType Type => ToolModeType.ArrangeAtCircleMoveCenter;
        private Vector3 PrevPos { get; set; }

        protected override string GetInfo() => MoveSlowerInfo;
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            PrevPos = GetMousePosition(Center.y);
        }
        public override void OnMouseUp(Event e) => Tool.SetMode(ToolModeType.ArrangeAtCircleComplete);
        public override bool OnEscape()
        {
            Tool.SetMode(ToolModeType.ArrangeAtCircleComplete);
            return true;
        }
        public override void OnMouseDrag(Event e)
        {
            var newPos = GetMousePosition(Center.y);
            var dir = newPos - PrevPos;
            PrevPos = newPos;

            if (Utility.OnlyCtrlIsPressed)
                Center += dir * 0.1f;
            else if (Utility.OnlyAltIsPressed)
                Center += dir * 0.01f;
            else
                Center += dir;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var color = CircleColor;
            RenderCircle(cameraInfo, color);
            RenderNodes(cameraInfo);
            RenderHoverCenter(cameraInfo, Colors.White);
            RenderCenter(cameraInfo, color);
        }
    }
    public class ArrangeCircleRadiusMode : BaseArrangeCircleCompleteMode
    {
        public override ToolModeType Type => ToolModeType.ArrangeAtCircleRadius;
        private float MinRadius { get; set; }

        protected override string GetInfo() => RadiusStepInfo;
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            var minRadius = 0f;
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                NetExtension.GetCommon(Nodes[i].Id, Nodes[(i + 1) % Nodes.Count].Id, out var segmentId);
                minRadius = Mathf.Max(minRadius, segmentId.GetSegment().Info.m_halfWidth * 2f);
            }
            MinRadius = minRadius;
        }
        public override void OnMouseUp(Event e) => Tool.SetMode(ToolModeType.ArrangeAtCircleComplete);
        public override bool OnEscape()
        {
            Tool.SetMode(ToolModeType.ArrangeAtCircleComplete);
            return true;
        }
        public override void OnMouseDrag(Event e)
        {
            var radius = (XZ(GetMousePosition(Center.y)) - XZ(Center)).magnitude;

            if (Utility.OnlyShiftIsPressed)
                radius = radius.RoundToNearest(10f);
            else if (Utility.OnlyCtrlIsPressed)
                radius = radius.RoundToNearest(1f);
            else if (Utility.OnlyAltIsPressed)
                radius = radius.RoundToNearest(0.1f);

            Radius = Mathf.Clamp(radius, MinRadius, 3000f);
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var color = CircleColor;
            RenderCircle(cameraInfo, color);
            RenderNodes(cameraInfo);
            RenderCenter(cameraInfo, color);
        }
    }
    public class ArrangeCircleMoveNodeMode : BaseArrangeCircleCompleteMode
    {
        public override ToolModeType Type => ToolModeType.ArrangeAtCircleMoveNode;
        private int Edit { get; set; }
        private Vector3 PrevDir { get; set; }

        protected override string GetInfo()
        {
            var result = string.Empty;

            if (IsWrongOrder)
                result += Localize.Mode_Info_ArrangeCircle_WrongOrder.AddErrorColor();
            else if (IsBigDelta)
                result += Localize.Mode_Info_ArrangeCircle_BigDelta.AddWarningColor();

            result += (!string.IsNullOrEmpty(result) ? "\n\n" : string.Empty) + string.Format(Localize.Mode_Info_ArrangeCircle_MoveAll, LocalizeExtension.Shift.AddInfoColor());

            return result;
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Edit = prevMode is ArrangeCircleCompleteMode mode ? mode.PressedNode : -1;
            PrevDir = GetMousePosition(Center.y) - Center;
        }
        public override void OnMouseUp(Event e) => Tool.SetMode(ToolModeType.ArrangeAtCircleComplete);
        public override bool OnEscape()
        {
            Tool.SetMode(ToolModeType.ArrangeAtCircleComplete);
            return true;
        }
        public override void OnMouseDrag(Event e)
        {
            var direction = GetMousePosition(Center.y) - Center;
            var delta = MathExtention.GetAngle(PrevDir, direction);
            PrevDir = direction;

            if (Utility.OnlyShiftIsPressed)
            {
                for (var i = 0; i < Nodes.Count; i += 1)
                    Nodes[i] = Nodes[i].Turn(delta);
            }
            else if (Edit != -1)
                Nodes[Edit] = Nodes[Edit].Turn(delta);
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            var color = CircleColor;
            RenderCircle(cameraInfo, color);
            RenderNodes(cameraInfo, Utility.OnlyShiftIsPressed ? -1 : Edit);
            RenderCenter(cameraInfo, color);
        }
    }
}
