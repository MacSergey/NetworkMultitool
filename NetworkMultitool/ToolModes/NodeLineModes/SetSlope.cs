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
        public override string ModeName => "SET SLOPE MODE";

        public override void PressEnter()
        {
            if (Nodes.Count >= 3)
            {
                Tool.SetSlope(Nodes.Select(n => n.Id).ToArray());
                Reset(this);
            }
        }
        
    }
}
