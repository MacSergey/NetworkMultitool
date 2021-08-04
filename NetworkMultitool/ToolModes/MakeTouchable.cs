using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class MakeTouchableMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.MakeTouchable;
        protected override bool IsReseted => true;

        protected override bool SelectNodes => false;
        protected override bool AllowUntouch => true;
        protected override Color32 SegmentColor => HoverSegment.Id.GetSegment().m_flags.IsSet(NetSegment.Flags.Untouchable) ? Colors.Green : Colors.Yellow;

        protected override string GetInfo()
        {
            if (!IsHoverSegment)
                return Localize.Mode_MakeTouchable_Info_ChangeTouchability;
            if (HoverSegment.Id.GetSegment().m_flags.IsSet(NetSegment.Flags.Untouchable))
                return Localize.Mode_MakeTouchable_Info_MakeTouchable + StepOverInfo;
            else
                return Localize.Mode_MakeTouchable_Info_MakeUntouchable + StepOverInfo;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if(IsHoverSegment)
            {
                ref var segment = ref HoverSegment.Id.GetSegment();

                if (segment.m_flags.IsSet(NetSegment.Flags.Untouchable))
                    segment.m_flags &= ~NetSegment.Flags.Untouchable;
                else
                    segment.m_flags |= NetSegment.Flags.Untouchable;
            }
        }
    }
}
