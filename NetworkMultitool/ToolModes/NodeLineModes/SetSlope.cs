using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class SlopeNodeMode : BaseNodeLine
    {
        public override ToolModeType Type => ToolModeType.SlopeNode;
        private new List<InfoLabel> Labels { get; } = new List<InfoLabel>();

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

        }
        public override void PressEnter()
        {
            if (Nodes.Count >= 3)
            {
                Tool.SetSlope(Nodes.Select(n => n.Id).ToArray());
                Reset(this);
            }
        }

        protected override void AddFirst(NodeSelection selection)
        {
            base.AddFirst(selection);
            var label = AddLabel();
            ApplyLabel(label, Nodes[0].Id, Nodes[1].Id);
            Labels.Insert(0, label);
        }
        protected override void AddLast(NodeSelection selection)
        {
            base.AddLast(selection);
            if (Nodes.Count > 1)
            {
                var label = AddLabel();
                ApplyLabel(label, Nodes[Labels.Count - 2].Id, Nodes[Labels.Count - 1].Id);
                Labels.Add(label);
            }
        }
        protected override void RemoveFirst()
        {
            base.RemoveFirst();
            RemoveLabel(Labels[0]);
            Labels.RemoveAt(0);
        }
        protected override void RemoveLast()
        {
            base.RemoveLast();
            RemoveLabel(Labels[Labels.Count - 1]);
            Labels.RemoveAt(Labels.Count - 1);
        }
        private void ApplyLabel(InfoLabel label, ushort firstId, ushort secondId)
        {
            NetExtension.GetCommon(firstId, secondId, out ushort segmentId);
            var bezier = new BezierTrajectory(segmentId);
            var angle = bezier.Length < Vector3.kEpsilon ? 0f : Mathf.Abs(bezier.StartPosition.y - bezier.EndPosition.y) / bezier.Length * 100f;

            label.isVisible = true;
            label.text = angle.ToString("0.0");
            label.WorldPosition = bezier.Position(0.5f) + new Vector3(0f, 5f, 0f);
        }
    }
}
