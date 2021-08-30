using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using ModsCommon;
using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool
{
    public class CreateCurveMode : BaseCreateMode
    {
        public override ToolModeType Type => ToolModeType.CreateCurve;

        private BezierTrajectory Bezier { get; set; }
        protected override bool AllowUntouch => true;

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return ApplyShortcut;

                yield return SwitchOffsetShortcut;
                //yield return IncreaseAngleShortcut;
                //yield return DecreaseAngleShortcut;

                yield return SwitchFollowTerrainShortcut;
            }
        }

        protected override string GetInfo()
        {
            if (GetBaseInfo() is string baseInfo)
                return baseInfo;
            else if (CalcState == CalcResult.WrongShape)
                return Localize.Mode_Info_WrongShape.AddErrorColor();
            else if (CalcState == CalcResult.OutOfMap)
                return Localize.Mode_Info_OutOfMap.AddErrorColor();
            else if (CalcState != CalcResult.Calculated)
                return Localize.Mode_Info_ClickOnNodeToChangeCreateDir;
            else
                return
                    CostInfo +
                    Localize.Mode_Info_ClickOnNodeToChangeCreateDir + "\n" +
                    (IsFollowTerrain ? string.Format(Localize.Mode_Info_SwitchFollowTerrain, SwitchFollowTerrainShortcut.AddInfoColor()) + "\n" : string.Empty) +
                    string.Format(Localize.Mode_Info_Create, ApplyShortcut.AddInfoColor());
        }
        protected override bool Init(bool reinit, StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, out CalcResult calcState)
        {
            var connect = new StraightTrajectory(firstTrajectory.StartPosition, secondTrajectory.EndPosition);
            if (NormalizeDotXZ(firstTrajectory.StartDirection, connect.Direction) < -0.7 || NormalizeDotXZ(secondTrajectory.StartDirection, -connect.Direction) < -0.7)
            {
                calcState = CalcResult.WrongShape;
                return false;
            }

            var startPos = firstTrajectory.StartPosition.SetHeight(Height);
            var startDir = firstTrajectory.StartDirection.MakeFlatNormalized();
            var endPos = secondTrajectory.StartPosition.SetHeight(Height);
            var endDir = secondTrajectory.StartDirection.MakeFlatNormalized();

            Bezier = new BezierTrajectory(startPos, startDir, endPos, endDir, forceSmooth: true);

            calcState = CalcResult.None;
            return true;
        }
        protected override Point[] Calculate(out CalcResult result)
        {
            var count = Mathf.CeilToInt(Bezier.Length / MaxLengthGetter());
            var partLength = Bezier.Length / count;
            var points = new Point[count - 1];

            var t = 0f;
            for (var i = 1; i < count; i += 1)
            {
                t = Bezier.Travel(t, partLength);
                var point = new Point(Bezier.Position(t), Bezier.Tangent(t).MakeFlatNormalized());
                points[i - 1] = point;
            }

            result = CalcResult.Calculated;
            return points;
        }
    }
}
