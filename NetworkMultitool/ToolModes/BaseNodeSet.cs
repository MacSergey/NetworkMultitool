using ColossalFramework;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NetworkMultitool
{
    public abstract class BaseNodeSet : BaseNetworkMultitoolMode
    {
        protected override bool IsReseted => Nodes.Count == 0;

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return ApplyShortcut;
            }
        }

        protected List<NodeSelection> Nodes { get; } = new List<NodeSelection>();
        public IEnumerable<NodeSelection> SelectedNodes => Nodes;
        protected HashSet<NodeSelection> ToAdd { get; } = new HashSet<NodeSelection>();
        protected AddResult AddState { get; set; }

        protected override bool IsValidNode(ushort nodeId)
        {
            for (var i = 1; i < Nodes.Count - 1; i += 1)
            {
                if (Nodes[i].Id == nodeId)
                    return false;
            }
            if (Nodes.Count != 0 && (nodeId == Nodes[0].Id || nodeId == Nodes[Nodes.Count - 1].Id))
                return true;
            else
                return base.IsValidNode(nodeId);
        }

        protected override string GetInfo()
        {
            if (AddState == AddResult.None)
                return Localize.Mode_NodeLine_Info_SelectNode + UndergroundInfo;
            else if (AddState == AddResult.One || AddState == AddResult.InStart || AddState == AddResult.InEnd)
                return Localize.Mode_Info_ClickSelectNode.AddActionColor() + StepOverInfo;
            else if (AddState == AddResult.IsFirst || AddState == AddResult.IsLast)
                return Localize.Mode_Info_ClickUnselectNode.AddActionColor() + StepOverInfo;
            else if (AddState == AddResult.NotConnect)
                return Localize.Mode_NodeLine_Info_NotConnected.AddErrorColor() + StepOverInfo;
            else
                return string.Format(Localize.Mode_Info_Apply, Colors.AddInfoColor(ApplyShortcut));
        }
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Nodes.Clear();
            ToAdd.Clear();
            ResetAdd();
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (!IsHoverNode)
            {
                ResetAdd();
                ToAdd.Clear();
            }
            else if (AddState == AddResult.None)
            {
                ToAdd.Clear();
                UpdateProcess();
            }
        }
        protected abstract void UpdateProcess();
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (AddState == AddResult.InStart)
            {
                foreach (var node in ToAdd)
                    AddFirst(node);
            }
            else if (AddState == AddResult.One || AddState == AddResult.Full || AddState == AddResult.InEnd)
            {
                foreach (var node in ToAdd)
                    AddLast(node);
            }
            else if (AddState == AddResult.IsFirst)
                RemoveFirst();
            else if (AddState == AddResult.IsLast)
                RemoveLast();
        }
        public override void OnSecondaryMouseClicked()
        {
            if (!IsReseted)
                Reset(this);
            else
                base.OnSecondaryMouseClicked();
        }
        protected virtual void AddFirst(NodeSelection selection)
        {
            if(Nodes.Count == 0 || selection.Id != Nodes[0].Id)
                Nodes.Insert(0, selection);
            ResetAdd();
        }
        protected virtual void AddLast(NodeSelection selection)
        {
            if(Nodes.Count == 0 || selection.Id != Nodes[Nodes.Count - 1].Id)
                Nodes.Add(selection);
            ResetAdd();
        }
        protected virtual void RemoveFirst()
        {
            Nodes.RemoveAt(0);
            ResetAdd();
        }
        protected virtual void RemoveLast()
        {
            Nodes.RemoveAt(Nodes.Count - 1);
            ResetAdd();
        }
        protected void ResetAdd() => AddState = AddResult.None;
        protected override void Apply() => Reset(this);

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RenderNearNodes(cameraInfo);
            RenderSegmentNodes(cameraInfo, AllowRenderNode);

            RenderExistOverlay(cameraInfo);

            if (IsHoverNode)
                RenderAddedOverlay(cameraInfo);
        }
        protected void RenderExistOverlay(RenderManager.CameraInfo cameraInfo)
        {
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                if ((i != 0 || AddState != AddResult.IsFirst) && (i != Nodes.Count - 1 || AddState != AddResult.IsLast))
                    Nodes[i].Render(new OverlayData(cameraInfo) { Color = Colors.White, RenderLimit = Underground });
            }
        }
        protected void RenderAddedOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (AddState == AddResult.One || AddState == AddResult.InStart || AddState == AddResult.InEnd)
            {
                foreach (var node in ToAdd)
                    node.Render(new OverlayData(cameraInfo) { Color = Colors.Green, RenderLimit = Underground });
            }
            else
            {
                var color = AddState switch
                {
                    AddResult.IsFirst or AddResult.IsLast => Colors.Yellow,
                    AddResult.NotConnect => Colors.Red,
                    _ => Colors.Red,
                };
                HoverNode.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
            }
        }

        protected virtual bool AllowRenderNode(ushort nodeId) => Nodes.All(n => n.Id != nodeId);
        protected override bool AllowRenderNear(ushort nodeId) => base.AllowRenderNear(nodeId) && AllowRenderNode(nodeId) && ToAdd.All(n => n.Id != nodeId);

        protected bool Check(bool isStart, ushort nodeId, ushort startId, ushort endId, ushort stopId, out HashSet<ushort> toAdd)
        {
            ref var node = ref startId.GetNode();
            var segmentIds = isStart ? node.SegmentIds() : node.SegmentIds().Reverse();
            foreach (var id in segmentIds)
            {
                var segmentId = id;
                var nextId = segmentId.GetSegment().GetOtherNode(startId);

                if (nextId == stopId)
                    continue;

                toAdd = new HashSet<ushort>();

                for (var i = 0; i < 20; i += 1)
                {
                    if (nextId == endId)
                        break;

                    toAdd.Add(nextId);
                    if (nextId == nodeId)
                        return true;

                    node = ref nextId.GetNode();
                    if (node.CountSegments() != 2)
                        break;

                    segmentId = node.SegmentIds().FirstOrDefault(s => s != segmentId);
                    nextId = segmentId.GetSegment().GetOtherNode(nextId);
                }
            }

            toAdd = null;
            return false;
        }

        protected enum AddResult
        {
            None,
            One,
            Full,
            InStart,
            InEnd,
            IsFirst,
            IsLast,
            NotConnect
        }
    }
}
