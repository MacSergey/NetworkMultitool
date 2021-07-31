using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class InvertSegmentMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.InvertSegment;
        protected override bool IsReseted => true;

        protected override bool SelectNodes => false;
        protected override Color32 SegmentColor => Colors.Green;

        protected override string GetInfo()
        {
            if (!IsHoverSegment)
                return Localize.Mode_Info_SelectSegment;
            else
                return Localize.Mode_InvertSegment_Info;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverSegment)
                InvertSegment(HoverSegment.Id);
        }
        public override void OnSecondaryMouseClicked()
        {
            if (IsHoverSegment)
                ReverseSegment(HoverSegment.Id);
        }
        private void InvertSegment(ushort segmentId)
        {
            var segment = segmentId.GetSegment();
            RemoveSegment(segmentId);
            CreateSegment(out _, segment.Info, segment.m_startNode, segment.m_endNode, segment.m_startDirection, segment.m_endDirection, !segment.IsInvert());
        }
        private void ReverseSegment(ushort segmentId)
        {
            var segment = segmentId.GetSegment();
            RemoveSegment(segmentId);
            CreateSegment(out _, segment.Info, segment.m_endNode, segment.m_startNode, segment.m_endDirection, segment.m_startDirection, segment.IsInvert());
        }
    }
}
