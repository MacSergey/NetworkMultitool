using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class SlopeNodeMode : BaseNodeLineMode
    {
        public override ToolModeType Type => ToolModeType.SlopeNode;
        private List<InfoLabel> OrderLabels { get; } = new List<InfoLabel>();
        private bool NeedRefreshLabels { get; set; } = false;

        protected override string GetInfo()
        {
            if (AddState == AddResult.None && Nodes.Count >= 3)
                return
                    Localize.Mode_NodeLine_Info_SelectNode + "\n" +
                    string.Format(Localize.Mode_Info_Slope_Apply, ApplyShortcut.AddInfoColor()) +
                    UndergroundInfo;
            else
                return base.GetInfo();
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            OrderLabels.Clear();
            NeedRefreshLabels = false;
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (NeedRefreshLabels)
            {
                NeedRefreshLabels = false;
                RefreshLabels();
            }
        }
        protected override void Apply()
        {
            if (Nodes.Count >= 3)
            {
                var nodeIds = Nodes.Select(n => n.Id).ToArray();
                SimulationManager.instance.AddAction(() =>
                {
                    SetSlope(nodeIds);
                    PlayAudio(true);
                    NeedRefreshLabels = true;
                });
            }
        }
        private static void SetSlope(ushort[] nodeIds)
        {
            SetSlope(nodeIds, PositionGetter, DirectionGetter, PositionSetter);
            for (var i = 1; i < nodeIds.Length; i += 1)
            {
                NetExtension.GetCommonSegment(nodeIds[i - 1], nodeIds[i], out var segmentId);
                CalculateSegmentDirections(segmentId);
            }
        }
        private static Vector3 PositionGetter(ref ushort nodeId) => nodeId.GetNode().m_position;
        private static void DirectionGetter(ushort firstId, ushort secondId, out Vector3 firstDir, out Vector3 secondDir)
        {
            NetExtension.GetCommonSegment(firstId, secondId, out var commonSegmentId);

            var segment = commonSegmentId.GetSegment();

            if (segment.IsStartNode(firstId))
            {
                firstDir = segment.m_startDirection;
                secondDir = segment.m_endDirection;
            }
            else
            {
                firstDir = segment.m_endDirection;
                secondDir = segment.m_startDirection;
            }
        }
        private static void PositionSetter(ref ushort nodeId, Vector3 position) => NetManager.instance.MoveNode(nodeId, position);

        protected override void AddFirst(NodeSelection selection)
        {
            base.AddFirst(selection);
            var label = AddLabel();
            OrderLabels.Insert(0, label);
            ApplyLabel(label, Nodes[0].Id, Nodes[1].Id);
        }
        protected override void AddLast(NodeSelection selection)
        {
            base.AddLast(selection);
            if (Nodes.Count > 1)
            {
                var label = AddLabel();
                OrderLabels.Add(label);
                ApplyLabel(label, Nodes[Nodes.Count - 2].Id, Nodes[Nodes.Count - 1].Id);
            }
        }
        protected override void RemoveFirst()
        {
            base.RemoveFirst();
            RemoveLabel(OrderLabels[0]);
            OrderLabels.RemoveAt(0);
        }
        protected override void RemoveLast()
        {
            base.RemoveLast();
            RemoveLabel(OrderLabels[OrderLabels.Count - 1]);
            OrderLabels.RemoveAt(OrderLabels.Count - 1);
        }
        public void RefreshLabels()
        {
            for (var i = 0; i < OrderLabels.Count; i += 1)
                ApplyLabel(OrderLabels[i], Nodes[i].Id, Nodes[i + 1].Id);
        }
        private void ApplyLabel(InfoLabel label, ushort firstId, ushort secondId)
        {
            NetExtension.GetCommonSegment(firstId, secondId, out ushort segmentId);
            ref var segment = ref segmentId.GetSegment();
            var bezier = new BezierTrajectory(ref segment);

            var slope = 0f;
            if (bezier.Length > Vector3.kEpsilon)
            {
                var delta = (segment.IsStartNode(firstId) ? 1 : -1) * (bezier.StartPosition.y - bezier.EndPosition.y);
                if (Settings.SlopeUnite == 0)
                    slope = Mathf.Tan(Mathf.Asin(delta / bezier.Length)) * 100f;
                else
                    slope = Mathf.Asin(delta / bezier.Length) * Mathf.Rad2Deg;
            }
            slope = slope.RoundToNearest(0.1f);

            label.Show = true;
            var sign = slope > 0 ? "+" : (slope < 0f ? "-" : string.Empty);
            var value = Settings.SlopeUnite == 0 ? GetPercentagesString(Mathf.Abs(slope)) : GetAngleString(Mathf.Abs(slope), "0.0");
            label.text = sign + value;
            label.WorldPosition = bezier.Position(0.5f) + new Vector3(0f, 5f, 0f);
            label.textColor = GetLabelColor(slope);
        }
        private Color32 GetLabelColor(float slope)
        {
            var value = Mathf.Abs(slope);
            if (!Settings.SlopeColors)
                return CommonColors.White;
            else if (Settings.SlopeUnite == 0)
                return value switch
                {
                    < 2f => CommonColors.Green,
                    < 6f => CombineColor(CommonColors.Green, CommonColors.Yellow, 2f, 6f, value),
                    < 10f => CombineColor(CommonColors.Yellow, CommonColors.Red, 6f, 10f, value),
                    _ => CommonColors.Red,
                };
            else
                return value switch
                {
                    < 1.15f => CommonColors.Green,
                    < 3.4f => CombineColor(CommonColors.Green, CommonColors.Yellow, 1.15f, 3.4f, value),
                    < 5.7f => CombineColor(CommonColors.Yellow, CommonColors.Red, 3.4f, 5.7f, value),
                    _ => CommonColors.Red,
                };

            static Color32 CombineColor(Color32 first, Color32 second, float from, float to, float value)
            {
                var t = (value - from) / (to - from);
                var r = (byte)Mathf.Lerp(first.r, second.r, t);
                var g = (byte)Mathf.Lerp(first.g, second.g, t);
                var b = (byte)Mathf.Lerp(first.b, second.b, t);
                return new Color32(r, g, b, 255);
            }
        }
    }
}
