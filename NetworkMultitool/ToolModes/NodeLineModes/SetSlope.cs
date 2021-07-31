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

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            OrderLabels.Clear();
        }
        protected override void Apply()
        {
            if (Nodes.Count >= 3)
            {
                Tool.SetSlope(Nodes.Select(n => n.Id).ToArray());
                RefreshLabels();
            }
        }

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
            NetExtension.GetCommon(firstId, secondId, out ushort segmentId);
            ref var segment = ref segmentId.GetSegment();
            var bezier = new BezierTrajectory(ref segment);

            var slope = 0f;
            if (bezier.Length > Vector3.kEpsilon)
            {
                var delta = (segment.IsStartNode(firstId) ? 1 : -1) * (bezier.StartPosition.y - bezier.EndPosition.y);
                slope = Settings.SlopeUnite == 0 ? (delta / bezier.Length * 100f) : (Mathf.Atan2(delta, bezier.Length));
            }
            slope = slope.RoundToNearest(0.1f);

            label.isVisible = true;
            var sign = slope > 0 ? "+" : (slope < 0f ? "-" : string.Empty);
            var value = Settings.SlopeUnite == 0 ? GetPercentagesString(Mathf.Abs(slope)) : GetAngleString(Mathf.Abs(slope), "0.0");
            label.text = sign + value;
            label.WorldPosition = bezier.Position(0.5f) + new Vector3(0f, 5f, 0f);
        }
    }
}
