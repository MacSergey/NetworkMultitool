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
    public abstract class BaseNodeLineMode : BaseNodeSet
    {
        protected override void UpdateProcess()
        {
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
        }
    }

}
