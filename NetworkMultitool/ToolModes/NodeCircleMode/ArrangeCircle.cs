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

            var points = Nodes.Select(n => n.Id.GetNode().m_position).ToArray();

            var centre = Vector3.zero;
            var radius = 1000f;

            for (var i = 0; i < points.Length; i += 1)
            {
                for (var j = i + 1; j < points.Length; j += 1)
                {
                    GetCircle2Points(points, i, j, ref centre, ref radius);

                    for (var k = j + 1; k < points.Length; k += 1)
                        GetCircle3Points(points, i, j, k, ref centre, ref radius);
                }
            }

            Center = centre;
            Radius = radius;
        }
        private void GetCircle2Points(Vector3[] points, int i, int j, ref Vector3 centre, ref float radius)
        {
            var newCentre = (points[i] + points[j]) / 2;
            var newRadius = (points[i] - points[j]).magnitude / 2;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private void GetCircle3Points(Vector3[] points, int i, int j, int k, ref Vector3 centre, ref float radius)
        {
            var pos1 = (points[i] + points[j]) / 2;
            var pos2 = (points[j] + points[k]) / 2;

            var dir1 = (points[i] - points[j]).Turn90(true).normalized;
            var dir2 = (points[j] - points[k]).Turn90(true).normalized;

            Line2.Intersect(XZ(pos1), XZ(pos1 + dir1), XZ(pos2), XZ(pos2 + dir2), out float p, out _);
            var newCentre = pos1 + dir1 * p;
            var newRadius = (newCentre - points[i]).magnitude;

            if (newRadius >= radius)
                return;

            if (AllPointsInCircle(points, newCentre, newRadius, i, j, k))
            {
                centre = newCentre;
                radius = newRadius;
            }
        }
        private bool AllPointsInCircle(Vector3[] points, Vector3 centre, float radius, params int[] ignore)
        {
            for (var i = 0; i < points.Length; i += 1)
            {
                if (ignore.Any(j => j == i))
                    continue;

                if ((centre - points[i]).magnitude > radius)
                    return false;
            }

            return true;
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
