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
        protected Result State { get; private set; }

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
            State = Result.None;
        }
        protected override string GetInfo()
        {
            if (State == Result.None)
                return Localize.Mode_NodeLine_Info_SelectNode + UndergroundInfo;
            else if (State == Result.One || State == Result.InStart || State == Result.InEnd)
                return Localize.Mode_Info_ClickSelectNode + StepOverInfo;
            else if (State == Result.IsFirst || State == Result.IsLast)
                return Localize.Mode_Info_ClickUnselectNode + StepOverInfo;
            else if (State == Result.NotConnect)
                return Localize.Mode_NodeLine_Info_NotConnected + StepOverInfo;
            else
                return string.Format(Localize.Mode_Info_Apply, ApplyShortcut);
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (IsHoverNode)
            {
                if (Nodes.Count == 0)
                    State = Result.One;
                else if (HoverNode.Id == Nodes.First().Id)
                    State = Result.IsFirst;
                else if (HoverNode.Id == Nodes.Last().Id)
                    State = Result.IsLast;
                else if (Check(HoverNode.Id, Nodes.First().Id))
                    State = Result.InStart;
                else if (Check(HoverNode.Id, Nodes.Last().Id))
                    State = Result.InEnd;
                else
                    State = Result.NotConnect;
            }
            else
                State = Result.None;

            static bool Check(ushort firstId, ushort secondId) => firstId.GetNode().Segments().Any(s => s.NodeIds().Any(n => n == secondId));
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            if (State == Result.InStart)
                AddFirst(HoverNode);
            else if (State == Result.One || State == Result.InEnd)
                AddLast(HoverNode);
            else if (State == Result.IsFirst)
                RemoveFirst();
            else if (State == Result.IsLast)
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
                if ((i != 0 || State != Result.IsFirst) && (i != Nodes.Count - 1 || State != Result.IsLast))
                    Nodes[i].Render(new OverlayData(cameraInfo) { Color = Colors.White, RenderLimit = Underground });
            }

            if (IsHoverNode)
            {
                var color = State switch
                {
                    Result.One or Result.InStart or Result.InEnd => Colors.Green,
                    Result.IsFirst or Result.IsLast => Colors.Yellow,
                    Result.NotConnect => Colors.Red,
                    _ => Colors.Red,
                };
                HoverNode.Render(new OverlayData(cameraInfo) { Color = color, RenderLimit = Underground });
            }
        }
        private bool AllowRenderNode(ushort nodeId) => Nodes.All(n => n.Id != nodeId);
        protected override bool AllowRenderNear(ushort nodeId) => base.AllowRenderNear(nodeId) && AllowRenderNode(nodeId);

        protected enum Result
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
