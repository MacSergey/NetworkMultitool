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
    public abstract class BaseNodeLineMode : BaseNetworkMultitoolMode
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
        protected NodeSelection LastHover { get; set; }
        protected HashSet<NodeSelection> ToAdd { get; } = new HashSet<NodeSelection>();
        protected AddResult AddState { get; set; }

        protected override bool IsValidNode(ushort nodeId)
        {
            for (var i = 1; i < Nodes.Count - 1; i += 1)
            {
                if (Nodes[i].Id == nodeId)
                    return false;
            }
            if (Nodes.Count != 0 && (nodeId == Nodes.First().Id || nodeId == Nodes.Last().Id))
                return true;
            else
                return base.IsValidNode(nodeId);
        }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Nodes.Clear();
            ToAdd.Clear();
            LastHover = null;
            AddState = AddResult.None;
        }
        protected override string GetInfo()
        {
            if (AddState == AddResult.None)
                return Localize.Mode_NodeLine_Info_SelectNode + UndergroundInfo;
            else if (AddState == AddResult.One || AddState == AddResult.InStart || AddState == AddResult.InEnd)
                return Localize.Mode_Info_ClickSelectNode + StepOverInfo;
            else if (AddState == AddResult.IsFirst || AddState == AddResult.IsLast)
                return Localize.Mode_Info_ClickUnselectNode + StepOverInfo;
            else if (AddState == AddResult.NotConnect)
                return Localize.Mode_NodeLine_Info_NotConnected + StepOverInfo;
            else
                return string.Format(Localize.Mode_Info_Apply, ApplyShortcut);
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (!IsHoverNode)
            {
                AddState = AddResult.None;
                LastHover = null;
                ToAdd.Clear();
            }
            else if (AddState == AddResult.None || !HoverNode.Equals(LastHover))
            {
                LastHover = HoverNode;
                ToAdd.Clear();

                if (Nodes.Count == 0)
                {
                    AddState = AddResult.One;
                    ToAdd.Add(HoverNode);
                }
                else if (HoverNode.Id == Nodes[0].Id)
                    AddState = AddResult.IsFirst;
                else if (HoverNode.Id == Nodes[Nodes.Count - 1].Id)
                    AddState = AddResult.IsLast;
                else
                {
                    var inStart = Check(true, HoverNode.Id, Nodes[0].Id, Nodes[Nodes.Count - 1].Id, (Nodes.Count == 1 ? 0 : Nodes[1].Id), out var toAddStart);
                    var inEnd = Check(false, HoverNode.Id, Nodes[Nodes.Count - 1].Id, Nodes[0].Id, (Nodes.Count == 1 ? 0 : Nodes[Nodes.Count - 2].Id), out var toAddEnd);

                    if (inStart && inEnd)
                    {
                        inStart = toAddStart.Count <= toAddEnd.Count;
                        inEnd = toAddEnd.Count < toAddStart.Count;
                    }

                    if (inStart)
                    {
                        AddState = AddResult.InStart;
                        ToAdd.AddRange(toAddStart.Select(i => new NodeSelection(i)));
                    }
                    else if (inEnd)
                    {
                        AddState = AddResult.InEnd;
                        ToAdd.AddRange(toAddEnd.Select(i => new NodeSelection(i)));
                    }
                    else
                        AddState = AddResult.NotConnect;
                }
                //else if(NetExtension.GetCommon(HoverNode.Id, Nodes[0].Id, out _))
                //{
                //    AddState = AddResult.InStart;
                //    ToAdd.Add(HoverNode);
                //}
                //else if (NetExtension.GetCommon(HoverNode.Id, Nodes[Nodes.Count - 1].Id, out _))
                //{
                //    AddState = AddResult.InEnd;
                //    ToAdd.Add(HoverNode);
                //}
                //else if (Check(HoverNode.Id, Nodes[0].Id, Nodes[Nodes.Count - 1].Id, (Nodes.Count == 1 ? 0 : Nodes[1].Id), out var toAddStart))
                //{
                //    AddState = AddResult.InStart;
                //    ToAdd.AddRange(toAddStart.Select(i => new NodeSelection(i)));
                //}
                //else if (Check(HoverNode.Id, Nodes[Nodes.Count - 1].Id, Nodes[0].Id, (Nodes.Count == 1 ? 0 : Nodes[Nodes.Count - 2].Id), out var toAddEnd))
                //{
                //    AddState = AddResult.InEnd;
                //    ToAdd.AddRange(toAddEnd.Select(i => new NodeSelection(i)));
                //}
                //else
                //    AddState = AddResult.NotConnect;
            }

        }
        private bool Check(bool isStart, ushort nodeId, ushort startId, ushort endId, ushort stopId, out HashSet<ushort> toAdd)
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

        public override void OnPrimaryMouseClicked(Event e)
        {
            if (AddState == AddResult.InStart)
            {
                foreach (var node in ToAdd)
                    AddFirst(node);
            }
            else if (AddState == AddResult.One || AddState == AddResult.InEnd)
            {
                foreach (var node in ToAdd)
                    AddLast(node);
            }
            else if (AddState == AddResult.IsFirst)
                RemoveFirst();
            else if (AddState == AddResult.IsLast)
                RemoveLast();
        }
        protected virtual void AddFirst(NodeSelection selection) => Nodes.Insert(0, selection);
        protected virtual void AddLast(NodeSelection selection) => Nodes.Add(selection);
        protected virtual void RemoveFirst() => Nodes.RemoveAt(0);
        protected virtual void RemoveLast() => Nodes.RemoveAt(Nodes.Count - 1);

        public override void OnSecondaryMouseClicked() => Reset(this);
        protected override void Apply() => Reset(this);

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            RenderNearNodes(cameraInfo);
            RenderSegmentNodes(cameraInfo, AllowRenderNode);

            for (var i = 0; i < Nodes.Count; i += 1)
            {
                if ((i != 0 || AddState != AddResult.IsFirst) && (i != Nodes.Count - 1 || AddState != AddResult.IsLast))
                    Nodes[i].Render(new OverlayData(cameraInfo) { Color = Colors.White, RenderLimit = Underground });
            }

            if (IsHoverNode)
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
        }
        private bool AllowRenderNode(ushort nodeId) => Nodes.All(n => n.Id != nodeId);
        protected override bool AllowRenderNear(ushort nodeId) => base.AllowRenderNear(nodeId) && AllowRenderNode(nodeId) && ToAdd.All(n => n.Id != nodeId);

        protected enum AddResult
        {
            None,
            One,
            InStart,
            InEnd,
            IsFirst,
            IsLast,
            NotConnect
        }
    }

}
