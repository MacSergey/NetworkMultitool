using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public class RemoveNodeMode : BaseNetworkMultitoolMode
    {
        public override ToolModeType Type => ToolModeType.RemoveNode;
        protected override bool SelectSegments => false;
        private bool IsCorrect => IsHoverNode && HoverNode.Id.GetNode().CountSegments() == 2;

        public override string GetToolInfo()
        {
            if (!IsHoverNode)
                return Localize.Tool_InfoSelectToRemove;
            else if(!IsCorrect)
                return string.Format(Localize.Tool_InfoNotAllowToRemove, HoverNode.Id) + GetStepOverInfo();
            else
                return string.Format(Localize.Tool_InfoClickToRemove, HoverNode.Id) + GetStepOverInfo();
        }

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (IsCorrect)
                Tool.RemoveNode(HoverNode.Id);
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (IsHoverNode)
                HoverNode.Render(new OverlayData(cameraInfo) { Color = IsCorrect ? Colors.Green : Colors.Red, RenderLimit = Underground });
        }
    }
}
