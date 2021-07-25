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
    public abstract class BaseNodeLine : BaseNetworkMultitoolMode
    {
        protected NetworkMultitoolShortcut Enter { get; }

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return Enter;
            }
        }
        protected override bool SelectSegments => false;

        protected List<NodeSelection> Nodes { get; } = new List<NodeSelection>();
        protected Result State { get; private set; }

        public BaseNodeLine()
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
        public override string GetToolInfo()
        {
            if (State == Result.One || State == Result.InStart || State == Result.InEnd)
                return "Click to select node" + GetStepOverInfo();
            else if (State == Result.IsFirst || State == Result.IsLast)
                return "Click to unselect node" + GetStepOverInfo();
            else if (State == Result.NotConnect)
                return "This node can't be selected\nbecause not connect with others" + GetStepOverInfo();
            else
                return "Select node";
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
        public override void OnPrimaryMouseClicked(UnityEngine.Event e)
        {
            if (State == Result.One || State == Result.InEnd)
                Nodes.Add(HoverNode);
            else if (State == Result.InStart)
                Nodes.Insert(0, HoverNode);
            else if (State == Result.IsFirst)
                Nodes.RemoveAt(0);
            else if (State == Result.IsLast)
                Nodes.RemoveAt(Nodes.Count - 1);
        }
        public override void OnSecondaryMouseClicked() => Reset(this);
        public virtual void PressEnter() => Reset(this);

        public override void RenderOverlay(RenderManager.CameraInfo cameraInfo)
        {
            for (var i = 0; i < Nodes.Count; i += 1)
            {
                if ((i == 0 && State == Result.IsFirst) || (i == Nodes.Count - 1 && State == Result.IsLast))
                    continue;
                else
                    Nodes[i].Center.RenderCircle(new OverlayData(cameraInfo) { Color = Colors.White, Width = 16f });
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
        }

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
