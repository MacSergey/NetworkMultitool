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
        protected override Color32 SegmentColor => CommonColors.Green;

        protected override string GetInfo()
        {
            if (!IsHoverSegment)
                return Localize.Mode_InvertSegment_Info_SelectToReverse;
            else
                return Localize.Mode_InvertSegment_Info_ClickToReverse.AddActionColor();
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsHoverSegment)
            {
                var segmentId = HoverSegment.Id;
                SimulationManager.instance.AddAction(() =>
                {
                    ReverseAndInvert(ref segmentId);
                    PlaySegmentEffect(segmentId, true);
                });
            }
        }
        private static void ReverseAndInvert(ref ushort segmentId)
        {
            var segment = segmentId.GetSegment();
            RemoveSegment(segmentId);
            CreateSegment(out segmentId, segment.Info, segment.m_endNode, segment.m_startNode, segment.m_endDirection, segment.m_startDirection, !segment.IsInvert());
        }
        private static void InvertSegment(ushort segmentId)
        {
            var segment = segmentId.GetSegment();
            RemoveSegment(segmentId);
            CreateSegment(out _, segment.Info, segment.m_startNode, segment.m_endNode, segment.m_startDirection, segment.m_endDirection, !segment.IsInvert());
        }
        private static void ReverseSegment(ushort segmentId)
        {
            var segment = segmentId.GetSegment();
            RemoveSegment(segmentId);
            CreateSegment(out _, segment.Info, segment.m_endNode, segment.m_startNode, segment.m_endDirection, segment.m_startDirection, segment.IsInvert());
        }
    }
}
