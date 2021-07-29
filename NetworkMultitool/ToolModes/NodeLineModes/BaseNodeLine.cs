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
        protected NetworkMultitoolShortcut Enter { get; }

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return Enter;
            }
        }

        protected List<NodeSelection> Nodes { get; } = new List<NodeSelection>();
        protected Result State { get; private set; }

        public BaseNodeLineMode()
        {
            Enter = new NetworkMultitoolShortcut(nameof(Enter), string.Empty, SavedInputKey.Encode(KeyCode.Return, false, false, false), PressEnter, ToolModeType.Line);
        }

        protected override bool IsValidNode(ushort nodeId)
        {
            for (var i = 1; i < Nodes.Count - 1; i += 1)
            {
                if (Nodes[i].Id == nodeId)
                    return false;
            }
            return true;
        }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            Nodes.Clear();
            State = Result.None;
        }
        protected override string GetInfo()
        {
            if (State == Result.One || State == Result.InStart || State == Result.InEnd)
                return Localize.Mode_Info_ClickSelectNode + GetStepOverInfo();
            else if (State == Result.IsFirst || State == Result.IsLast)
                return Localize.Mode_Info_ClickUnselectNode + GetStepOverInfo();
            else if (State == Result.NotConnect)
                return Localize.Mode_NodeLine_Info_NotConnected + GetStepOverInfo();
            else
                return string.Format(Localize.Mode_Info_Apply, Enter);
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
        public virtual void PressEnter() => Reset(this);

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                if ((i != 0 || State != Result.IsFirst) && (i != Nodes.Count - 1 || State != Result.IsLast))
                    Nodes[i].Render(new OverlayData(cameraInfo) { Color = Colors.White});
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
                HoverNode.Render(new OverlayData(cameraInfo) { Color = color });
            }
            RenderSegmentNodes(cameraInfo, AllowRenderNode);
        }
        private bool AllowRenderNode(ushort nodeId) => Nodes.All(n => n.Id != nodeId);

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
