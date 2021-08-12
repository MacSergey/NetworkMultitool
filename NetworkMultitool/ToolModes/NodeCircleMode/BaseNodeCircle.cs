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
    public abstract class BaseNodeCircle : BaseNodeSet
    {
        protected bool CircleComplete { get; private set; }
        protected override bool CanSwitchUnderground => !CircleComplete;
        protected override bool SelectNodes => !CircleComplete;

        protected override bool IsValidNode(ushort nodeId) => base.IsValidNode(nodeId) && (Nodes.Count == 0 || nodeId.GetNode().CountSegments() >= 3);

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);
            CircleComplete = false;
        }
        protected override void UpdateProcess()
        {
            if (CircleComplete)
                return;

            bool inStart;
            bool inEnd;
            HashSet<ushort> toAddStart;
            HashSet<ushort> toAddEnd;
            if (Nodes.Count != 0)
            {
                inStart = Check(true, HoverNode.Id, Nodes[0].Id, 0, (Nodes.Count == 1 ? 0 : Nodes[1].Id), out toAddStart);
                inEnd = Check(false, HoverNode.Id, Nodes[Nodes.Count - 1].Id, 0, (Nodes.Count == 1 ? 0 : Nodes[Nodes.Count - 2].Id), out toAddEnd);
            }
            else if (HoverNode.Id.GetNode().CountSegments() == 2)
            {
                inStart = Check(true, HoverNode.Id, HoverNode.Id, 0, 0, out toAddStart);
                inEnd = Check(false, HoverNode.Id, HoverNode.Id, 0, 0, out toAddEnd);
            }
            else
            {
                AddState = AddResult.One;
                ToAdd.Add(HoverNode);
                return;
            }

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
            else if (Nodes.Count == 0)
            {
                AddState = AddResult.One;
                ToAdd.Add(HoverNode);
            }
            else if (HoverNode.Id == Nodes[0].Id)
                AddState = AddResult.IsFirst;
            else if (HoverNode.Id == Nodes[Nodes.Count - 1].Id)
                AddState = AddResult.IsLast;
            else
                AddState = AddResult.NotConnect;
        }
        protected override void AddFirst(NodeSelection selection)
        {
            if (CircleComplete)
                return;
            else if (Nodes.Count != 0 && selection.Id == Nodes[Nodes.Count - 1].Id)
                Complite();
            else
                base.AddFirst(selection);
        }
        protected override void AddLast(NodeSelection selection)
        {
            if (CircleComplete)
                return;
            else if (Nodes.Count != 0 && selection.Id == Nodes[0].Id)
                Complite();
            else
                base.AddLast(selection);
        }
        protected override void RemoveFirst()
        {
            var count = 1;
            for (var i = 1; i < Nodes.Count; i += 1)
            {
                if (Nodes[i].Id.GetNode().CountSegments() >= 3)
                    break;
                else
                    count += 1;
            }

            Nodes.RemoveRange(0, count);
            ResetAdd();
        }
        protected override void RemoveLast()
        {
            var count = 1;
            for (var i = Nodes.Count - 2; i >= 0; i -= 1)
            {
                if (Nodes[i].Id.GetNode().CountSegments() >= 3)
                    break;
                else
                    count += 1;
            }

            Nodes.RemoveRange(Nodes.Count - count, count);
            ResetAdd();
        }
        protected virtual void Complite()
        {
            CircleComplete = true;
        }


        protected override bool AllowRenderNode(ushort nodeId) => base.AllowRenderNode(nodeId) && (Nodes.Count == 0 || nodeId.GetNode().CountSegments() >= 3);
    }
}
