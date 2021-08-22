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
    public class UnlockSegmentMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.UnlockSegment;
        protected override bool IsReseted => true;

        protected override bool SelectNodes => false;
        protected override bool AllowUntouch => true;
        protected override Color32 SegmentColor => HoverSegment.Id.GetSegment().m_flags.IsSet(NetSegment.Flags.Untouchable) ? Colors.Green : Colors.Yellow;

        protected override string GetInfo()
        {
            if (!IsHoverSegment)
                return Localize.Mode_UnlockSegment_Info_ChangeLock;
            if (HoverSegment.Id.GetSegment().m_flags.IsSet(NetSegment.Flags.Untouchable))
                return Localize.Mode_UnlockSegment_Info_ClickToUnlock.AddActionColor() + StepOverInfo;
            else
                return Localize.Mode_UnlockSegment_Info_ClickToLock.AddActionColor() + StepOverInfo;
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if(IsHoverSegment)
            {
                var segmentId = HoverSegment.Id;
                SimulationManager.instance.AddAction(() =>
                {
                    ChangeLock(segmentId);
                    PlaySegmentEffect(segmentId, true);
                });
            }
        }
        private static void ChangeLock(ushort segmentId)
        {
            ref var segment = ref segmentId.GetSegment();

            if (segment.m_flags.IsSet(NetSegment.Flags.Untouchable))
                segment.m_flags &= ~NetSegment.Flags.Untouchable;
            else
                segment.m_flags |= NetSegment.Flags.Untouchable;
        }
    }
}
