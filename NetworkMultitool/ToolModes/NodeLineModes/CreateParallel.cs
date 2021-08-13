using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using NetworkMultitool.Utilities;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    class CreateParallelMode : BaseNodeLineMode
    {
        public static NetworkMultitoolShortcut IncreaseShiftShortcut { get; } = GetShortcut(KeyCode.Equals, nameof(IncreaseShiftShortcut), nameof(Localize.Settings_Shortcut_IncreaseShift), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateParallelMode)?.IncreaseShift(), repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseShiftShortcut { get; } = GetShortcut(KeyCode.Minus, nameof(DecreaseShiftShortcut), nameof(Localize.Settings_Shortcut_DecreaseShift), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateParallelMode)?.DecreaseShift(), repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut InvertShiftShortcut { get; } = GetShortcut(KeyCode.Tab, nameof(InvertShiftShortcut), nameof(Localize.Settings_Shortcut_InvertShift), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateParallelMode)?.InvertShift());

        public override ToolModeType Type => ToolModeType.CreateParallel;
        private bool Calculated { get; set; }
        private List<Point> Points { get; set; }
        protected NetInfo Info => ToolsModifierControl.toolController.Tools.OfType<NetTool>().FirstOrDefault().Prefab?.m_netAI?.m_info ?? Nodes[0].Id.GetNode().Info;
        private float Shift { get; set; }
        private Straight StartLine { get; set; }
        private Straight EndLine { get; set; }

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in base.Shortcuts)
                    yield return shortcut;

                yield return IncreaseShiftShortcut;
                yield return DecreaseShiftShortcut;
                yield return InvertShiftShortcut;
            }
        }

        protected override string GetInfo()
        {
            if (AddState == AddResult.None && Nodes.Count >= 2)
                return
                    Localize.Mode_NodeLine_Info_SelectNode + "\n" +
                    string.Format(Localize.Mode_Info_ChangeShift, DecreaseShiftShortcut, IncreaseShiftShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_Parallel_ChangeShift, InvertShiftShortcut) + "\n" +
                    Localize.Mode_Info_Step + "\n" +
                    string.Format(Localize.Mode_Info_Parallel_Create, ApplyShortcut) +
                    UndergroundInfo;
            else
                return base.GetInfo();
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Calculated = false;
            Shift = 16f;

            if (StartLine != null && StartLine.Label != null)
            {
                RemoveLabel(StartLine.Label);
                StartLine.Label = null;
            }
            if (EndLine != null && EndLine.Label != null)
            {
                RemoveLabel(EndLine.Label);
                EndLine.Label = null;
            }

            StartLine = null;
            EndLine = null;
        }
        protected override void ClearLabels()
        {
            base.ClearLabels();

            if (StartLine != null)
                StartLine.Label = null;

            if (EndLine != null)
                EndLine.Label = null;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (Nodes.Count >= 2 && !Calculated)
                Calculate();

            StartLine?.Update(Calculated);
            EndLine?.Update(Calculated);
        }
        private void Calculate()
        {
            Points = new List<Point>(Nodes.Count);

            for (var i = 0; i < Nodes.Count; i += 1)
            {
                var direction = Vector3.zero;
                if (i != 0)
                {
                    NetExtension.GetCommon(Nodes[i].Id, Nodes[i - 1].Id, out var segmentId);
                    ref var segment = ref segmentId.GetSegment();
                    direction += segment.IsStartNode(Nodes[i].Id) ? -segment.m_startDirection : -segment.m_endDirection;
                }
                if (i != Nodes.Count - 1)
                {
                    NetExtension.GetCommon(Nodes[i].Id, Nodes[i + 1].Id, out var segmentId);
                    ref var segment = ref segmentId.GetSegment();
                    direction += segment.IsStartNode(Nodes[i].Id) ? segment.m_startDirection : segment.m_endDirection;
                }
                if (i != 0 && i != Nodes.Count - 1)
                    direction /= 2f;

                var shiftDir = direction.Turn90(true).MakeFlatNormalized();
                ref var node = ref Nodes[i].Id.GetNode();
                Points.Add(new Point(node.m_position + shiftDir * Shift, direction));
            }

            Calculated = true;

            ref var startNode = ref Nodes[0].Id.GetNode();
            ref var endNode = ref Nodes[Nodes.Count - 1].Id.GetNode();

            var startDir = (Points[0].Position - startNode.m_position).Turn90(Shift >= 0f).MakeFlatNormalized();
            var endDir = (Points[Points.Count - 1].Position - endNode.m_position).Turn90(Shift <= 0f).MakeFlatNormalized();

            var startLenght = startNode.Segments().Max(s => s.Info.m_halfWidth) + 2f;
            var endLenght = endNode.Segments().Max(s => s.Info.m_halfWidth) + 2f;

            StartLine = new Straight(startNode.m_position, Points[0].Position, startDir, startLenght, StartLine?.Label ?? AddLabel(), startNode.m_position.y);
            EndLine = new Straight(endNode.m_position, Points[Points.Count - 1].Position, endDir, endLenght, EndLine?.Label ?? AddLabel(), endNode.m_position.y);
        }
        protected override void Apply()
        {
            if (Nodes.Count >= 2)
            {
                var nodeIds = new List<ushort>();
                var info = Info;

                for (var i = 0; i < Points.Count; i += 1)
                {
                    CreateNode(out var newNodeId, info, Points[i].Position);
                    nodeIds.Add(newNodeId);
                }

                for (var i = 1; i < nodeIds.Count; i += 1)
                {
                    CreateSegmentAuto(out var newSegmentId, info, nodeIds[i - 1], nodeIds[i], Points[i - 1].Direction, -Points[i].Direction);
                    CalculateSegmentDirections(newSegmentId);
                }
            }
        }
        private void IncreaseShift() => ChangeShift(true);
        private void DecreaseShift() => ChangeShift(false);
        private void ChangeShift(bool increase)
        {
            var step = 1f;
            if (Utility.OnlyShiftIsPressed)
                step = 10f;
            else if (Utility.OnlyCtrlIsPressed)
                step = 0.1f;
            else if (Utility.OnlyAltIsPressed)
                step = 0.01f;

            Shift = (Shift + (increase ? step : -step)).RoundToNearest(step);
            Calculated = false;
        }
        private void InvertShift()
        {
            Shift = -Shift;
            Calculated = false;
        }
        protected override void AddFirst(NodeSelection selection)
        {
            base.AddFirst(selection);
            Calculated = false;
        }
        protected override void AddLast(NodeSelection selection)
        {
            base.AddLast(selection);
            Calculated = false;
        }
        protected override void RemoveFirst()
        {
            base.RemoveFirst();
            Calculated = false;
        }
        protected override void RemoveLast()
        {
            base.RemoveLast();
            Calculated = false;
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            base.RenderOverlay(cameraInfo);

            if (Calculated)
            {
                var info = Info;
                RenderParts(Points, cameraInfo, Colors.Yellow, info.m_halfWidth * 2f);

                StartLine.Render(cameraInfo, Colors.Gray224, Colors.Gray224, Underground);
                EndLine.Render(cameraInfo, Colors.Gray224, Colors.Gray224, Underground);
            }
        }

        public class Straight : BaseStraight
        {
            public float MeasureLength { get; }
            public Straight(Vector3 start, Vector3 end, Vector3 labelDir, float measureLength, InfoLabel label, float height) : base(start, end, labelDir, label, height)
            {
                MeasureLength = measureLength;
            }

            public void Update(bool show) => Update(MeasureLength, show);
            public void Render(RenderManager.CameraInfo cameraInfo, Color color, Color colorArrow, bool underground) => this.RenderMeasure(cameraInfo, 0f, MeasureLength + 2f, LabelDir, color, colorArrow, underground);
        }
    }
}
