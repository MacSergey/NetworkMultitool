using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Math;
using ModsCommon;
using ModsCommon.Utilities;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    public class ArrangeCircleMode : BaseNodeCircle
    {
        public override ToolModeType Type => ToolModeType.ArrangeAtCircle;
        private Vector3 Center { get; set; }
        private float Radius { get; set; }
        private bool IsCompleteHover => Nodes.Count != 0 && ((HoverNode.Id == Nodes[0].Id && AddState == AddResult.InEnd) || (HoverNode.Id == Nodes[Nodes.Count - 1].Id && AddState == AddResult.InStart));

        protected override string GetInfo()
        {
            if (CircleComplete)
                return string.Format(Localize.Mode_Info_Apply, ApplyShortcut);
            else if (IsHoverNode && IsCompleteHover)
                return Localize.Mode_ArrangeCircle_Info_ClickToComplite + StepOverInfo;
            else
                return base.GetInfo();
        }
        protected override void Complite()
        {
            base.Complite();

            var center = Vector3.zero;
            foreach (var node in Nodes)
            {
                var pos = node.Id.GetNode().m_position.MakeFlat();
                center += pos;
            }
            Center = center / Nodes.Count;

            var radius = 0f;
            foreach (var node in Nodes)
            {
                var pos = node.Id.GetNode().m_position.MakeFlat();
                var magnitude = (Center - pos).magnitude;
                radius += magnitude;
            }
            Radius = radius / Nodes.Count;
        }
        protected override void Apply()
        {
            if (CircleComplete)
            {
                var segmentIds = new ushort[Nodes.Count];
                for (var i = 0; i < Nodes.Count; i += 1)
                    NetExtension.GetCommon(Nodes[i].Id, Nodes[(i + 1) % Nodes.Count].Id, out segmentIds[i]);
                var terrainRect = GetTerrainRect(segmentIds);

                foreach (var node in Nodes)
                {
                    var dir = (node.Id.GetNode().m_position - Center).MakeFlatNormalized();
                    var newPos = Center + dir * Radius;
                    MoveNode(node.Id, newPos);
                }
                for (var i = 0; i < Nodes.Count; i += 1)
                {
                    SetDirection(i, (i + 1) % Nodes.Count);
                    SetDirection(i, (i + Nodes.Count - 1) % Nodes.Count);
                }

                foreach (var node in Nodes)
                    NetManager.instance.UpdateNode(node.Id);

                UpdateTerrain(terrainRect);

                Reset(this);
            }
        }
        private void SetDirection(int i, int j)
        {
            var centerDir = (Nodes[i].Id.GetNode().m_position - Center).MakeFlatNormalized();

            NetExtension.GetCommon(Nodes[i].Id, Nodes[j].Id, out var segmentId);
            ref var segment = ref segmentId.GetSegment();
            var direction = Nodes[j].Id.GetNode().m_position - Nodes[i].Id.GetNode().m_position;
            var newDirection = centerDir.Turn90(NormalizeCrossXZ(centerDir, direction) >= 0f);
            SetSegmentDirection(segmentId, segment.IsStartNode(Nodes[i].Id), newDirection);
        }
        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (!CircleComplete)
            {
                RenderNearNodes(cameraInfo);
                RenderSegmentNodes(cameraInfo, AllowRenderNode);


                if (!IsHoverNode)
                {
                    foreach (var node in Nodes)
                        node.Render(new OverlayData(cameraInfo) { Color = Colors.White, RenderLimit = Underground });
                }
                else if (IsCompleteHover)
                {
                    foreach (var node in Nodes)
                        node.Render(new OverlayData(cameraInfo) { Color = Colors.Purple, RenderLimit = Underground });
                    foreach (var node in ToAdd)
                        node.Render(new OverlayData(cameraInfo) { Color = Colors.Purple, RenderLimit = Underground });
                }
                else
                {
                    RenderExistOverlay(cameraInfo);
                    RenderAddedOverlay(cameraInfo);
                }
            }
            else
            {
                Center.RenderCircle(new OverlayData(cameraInfo) { Width = Radius * 2f, Color = Colors.Yellow, RenderLimit = Underground });
                Center.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.Yellow, RenderLimit = Underground }, 5f, 0f);
            }
        }
    }
}
