using ColossalFramework;
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
    public abstract class BaseCreateConnectionMode : BaseCreateMode
    {
        public List<Circle> Circles { get; private set; } = new List<Circle>();
        public List<Straight> Straights { get; private set; } = new List<Straight>();

        public int SelectCircle { get; protected set; }
        public bool SelectOffset { get; protected set; }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is BaseCreateConnectionMode connectionMode)
            {
                First = connectionMode.First;
                Second = connectionMode.Second;
                IsFirstStart = connectionMode.IsFirstStart;
                IsSecondStart = connectionMode.IsSecondStart;

                FirstTrajectory = connectionMode.FirstTrajectory;
                SecondTrajectory = connectionMode.SecondTrajectory;

                Height = connectionMode.Height;

                SelectCircle = connectionMode.SelectCircle;
                SelectOffset = connectionMode.SelectOffset;

                Circles.AddRange(connectionMode.Circles);
                foreach (var circle in Circles)
                {
                    if (circle != null)
                        circle.Label = AddLabel();
                }

                Straights.AddRange(connectionMode.Straights);
                foreach (var straight in Straights)
                {
                    if (straight != null)
                        straight.Label = AddLabel();
                }

                SetInited();
            }
        }
        protected override void ResetParams()
        {
            base.ResetParams();
            ResetData();
        }
        private void ResetData()
        {
            foreach (var circle in Circles)
            {
                if (circle?.Label != null)
                {
                    RemoveLabel(circle.Label);
                    circle.Label = null;
                }
            }
            foreach (var straight in Straights)
            {
                if (straight?.Label != null)
                {
                    RemoveLabel(straight.Label);
                    straight.Label = null;
                }
            }

            Circles.Clear();
            Straights.Clear();
        }
        protected override void ClearLabels()
        {
            base.ClearLabels();

            foreach (var circle in Circles)
            {
                if (circle != null)
                    circle.Label = null;
            }
            foreach (var straight in Straights)
            {
                if (straight != null)
                    straight.Label = null;
            }
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (CalcState != CalcResult.None)
            {
                foreach (var circle in Circles)
                    circle?.Update(CalcState != CalcResult.None);

                var info = Info;
                for (var i = 0; i < Straights.Count; i += 1)
                {
                    if (Straights[i] is Straight straight)
                    {
                        var show = CalcState == CalcResult.Calculated && (i == 0 || i == Straights.Count - 1 || !straight.IsShort);
                        straight.Update(info, show);
                    }
                }
            }
        }

        protected override bool Init(StraightTrajectory firstTrajectory, StraightTrajectory secondTrajectory, out CalcResult calcState)
        {
            ResetData();

            var first = new EdgeCircle(CircleType.First, AddLabel(), firstTrajectory, Height);
            var last = new EdgeCircle(CircleType.Last, AddLabel(), secondTrajectory, Height);
            Circles.Add(first);
            Circles.Add(last);
            Straights.Add(null);
            Straights.Add(null);
            Straights.Add(null);

            EdgeCircle.GetSides(first, last);
            Circle.SetConnect(first, last);

            calcState = CalcResult.None;
            return true;
        }
        protected override Point[] Calculate(out CalcResult result)
        {
            foreach (var circle in Circles)
            {
                circle.Calculate(MinPossibleRadius, MaxPossibleRadius);
                circle.IsCorrect = true;
            }

            for (var i = 1; i < Circles.Count; i += 1)
            {
                var isCorrect = Circle.CheckRadii(Circles[i - 1], Circles[i]);
                Circles[i - 1].IsCorrect &= isCorrect;
                Circles[i].IsCorrect &= isCorrect;
            }

            for (var i = 0; i < Straights.Count; i += 1)
            {
                var label = Straights[i]?.Label ?? AddLabel();

                if (i == 0)
                    Straights[i] = (Circles.FirstOrDefault() as EdgeCircle).GetStraight(label);
                else if (i == Straights.Count - 1)
                    Straights[i] = (Circles.LastOrDefault() as EdgeCircle).GetStraight(label);
                else
                {
                    Circle.SetConnect(Circles[i - 1], Circles[i]);
                    Straights[i] = Circle.GetStraight(Circles[i - 1], Circles[i], label, Height);
                }
            }

            result = Circles.All(c => c.IsCorrect) ? CalcResult.Calculated : CalcResult.WrongShape;
            return GetParts().ToArray();
        }
        private IEnumerable<Point> GetParts()
        {
            for (var i = 0; i < Circles.Count + Straights.Count; i += 1)
            {
                if (i % 2 == 0)
                {
                    var j = i / 2;
                    if (j != 0 && j != Straights.Count - 1 && !Circles[j - 1].IsCorrect && !Circles[j].IsCorrect)
                    {
                        yield return Point.Empty;
                        continue;
                    }

                    var straight = Straights[j];
                    if (!straight.IsShort)
                    {
                        if (j != 0 && !Circles[j - 1].IsCorrect)
                            yield return straight.StartPoint;

                        foreach (var part in straight.Parts)
                            yield return part;

                        if (j != Straights.Count - 1 && !Circles[j].IsCorrect)
                            yield return straight.EndPoint;
                    }
                    else if (j != 0 && j != Straights.Count - 1)
                    {
                        if (!Circles[j - 1].IsShort && !Circles[j].IsShort)
                            yield return straight.MiddlePoint;
                        else if (!Circles[j - 1].IsShort)
                            yield return straight.StartPoint;
                        else if (!Circles[j].IsShort)
                            yield return straight.EndPoint;
                    }
                }
                else
                {
                    var j = i / 2;
                    if (Circles[j].IsCorrect)
                    {
                        foreach (var part in Circles[j].GetParts(Straights[j], Straights[j + 1]))
                            yield return part;
                    }
                }
            }
        }

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            foreach (var circle in Circles)
                circle.Render(cameraInfo, info, Colors.Gray224, Underground);

            for (var i = 0; i < Straights.Count; i += 1)
            {
                if (i == 0 || i == Straights.Count - 1 || !Straights[i].IsShort)
                    Straights[i].Render(cameraInfo, info, Colors.Gray224, Colors.Gray224, Underground);
            }
        }
        protected override void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            if (CalcState == CalcResult.BigRadius || CalcState == CalcResult.SmallRadius || CalcState == CalcResult.WrongShape)
            {
                foreach (var circle in Circles)
                {
                    circle.RenderCircle(cameraInfo, circle.IsCorrect ? Colors.Green : Colors.Red, Underground);
                    circle.RenderCenter(cameraInfo, circle.IsCorrect ? Colors.Green : Colors.Red, Underground);
                }
            }
        }

        protected class EdgeCircle : Circle
        {
            public CircleType Type { get; }
            private StraightTrajectory Guide { get; }

            public override Vector3 CenterPos
            {
                get => Guide.StartPosition.SetHeight(Height) - MainDir * Radius + Guide.Direction * Offset;
                set
                {
                    var normal = new StraightTrajectory(value, value + MainDir, false);
                    Intersection.CalculateSingle(Guide, normal, out var t, out _);
                    Offset = t;
                }
            }
            public override Vector3 StartRadiusDir
            {
                get => base.StartRadiusDir;
                set
                {
                    if (Type == CircleType.Last)
                        base.StartRadiusDir = value;
                }
            }
            public override Vector3 EndRadiusDir
            {
                get => base.EndRadiusDir;
                set
                {
                    if (Type == CircleType.First)
                        base.EndRadiusDir = value;
                }
            }
            private Vector3 MainDir => Type switch
            {
                CircleType.First => StartRadiusDir,
                CircleType.Last => EndRadiusDir,
            };

            public override Vector3 StartPos => Type == CircleType.First ? Guide.Position(Offset).SetHeight(Height) : base.StartPos;
            public override Vector3 EndPos => Type == CircleType.Last ? Guide.Position(Offset).SetHeight(Height) : base.EndPos;
            public override Vector3 StartDir => Type == CircleType.First ? Guide.Direction : base.StartDir;
            public override Vector3 EndDir => Type == CircleType.Last ? -Guide.Direction : base.EndDir;

            private float _offset;
            public float Offset
            {
                get => _offset;
                set => _offset = Mathf.Clamp(value, 0f, 500f);
            }

            public EdgeCircle(CircleType type, InfoLabel label, StraightTrajectory guide, float height) : base(label, height)
            {
                Type = type;
                Guide = guide;
            }

            public override void Calculate(float minRadius, float maxRadius)
            {
                base.Calculate(minRadius, maxRadius);

                var dir = Guide.Direction.Turn90(Direction == Direction.Right);
                if (Type == CircleType.First)
                    base.StartRadiusDir = -dir;
                else
                    base.EndRadiusDir = dir;
            }
            public Straight GetStraight(InfoLabel label) => Type switch
            {
                CircleType.First => new Straight(Guide.StartPosition, StartPos, StartRadiusDir, label, Height),
                CircleType.Last => new Straight(EndPos, Guide.StartPosition, EndRadiusDir, label, Height),
            };

            public static void GetSides(EdgeCircle first, EdgeCircle last)
            {
                var connect = new StraightTrajectory(first.StartPos.MakeFlat(), last.EndPos.MakeFlat());

                var firstDir = CrossXZ(first.StartDir, connect.Direction) >= 0;
                var lastDir = CrossXZ(-last.EndDir, -connect.Direction) >= 0;
                var firstDot = DotXZ(first.StartDir, connect.Direction) >= 0;
                var lastDot = DotXZ(-last.EndDir, -connect.Direction) >= 0;

                if (firstDot != lastDot && firstDir != lastDir)
                {
                    if (!firstDot)
                        lastDir = !lastDir;
                    else if (!lastDot)
                        lastDir = !lastDir;
                }

                first.Direction = firstDir ? Direction.Right : Direction.Left;
                last.Direction = lastDir ? Direction.Left : Direction.Right;
            }
            protected override void SnappingOnePosition(Circle other)
            {
                var normal = new StraightTrajectory(other.CenterPos, other.CenterPos + MainDir, false);
                Intersection.CalculateSingle(Guide, normal, out var otherOffset, out var otherHeight);

                var length = other.Direction == Direction ? Mathf.Abs(other.Radius - Radius) : other.Radius + Radius;
                var height = Mathf.Sign(otherHeight) * (otherHeight - Radius);
                var delta = Mathf.Sqrt(length * length - height * height);

                if (!float.IsNaN(delta))
                {
                    var side = NormalizeDotXZ(GetConnectCenter(other, this).Direction, Guide.Direction);
                    Offset = otherOffset + Mathf.Sign(side) * delta;
                }
            }
            protected override void SnappingTwoPositions(Circle before, Circle after) { }
            public override bool GetSnappingRadius(Circle other, out float snappingRadius)
            {
                var normal = new StraightTrajectory(other.CenterPos, other.CenterPos + MainDir, false);
                Intersection.CalculateSingle(Guide, normal, out var otherOffset, out var otherHeight);

                if (other.Radius + otherHeight <= 0f)
                {
                    snappingRadius = 0f;
                    return false;
                }

                var delta = Mathf.Abs(Offset - otherOffset);
                var height = Mathf.Abs(otherHeight);
                if (otherHeight < 0f)
                {
                    var radius = (other.Radius * other.Radius - height * height - delta * delta) / (2f * (height - other.Radius));
                    snappingRadius = radius;
                    return true;
                }
                else
                {
                    var radius1 = (other.Radius * other.Radius - height * height - delta * delta) / (2f * (other.Radius - height));
                    var radius2 = (height * height + delta * delta - other.Radius * other.Radius) / (2f * (other.Radius + height));
                    snappingRadius = Mathf.Abs(Radius - radius1) < Mathf.Abs(Radius - radius2) ? radius1 : radius2;
                    return true;
                }
            }
        }
        public enum CircleType
        {
            First,
            Last,
        }
    }
    public abstract class BaseAdditionalCreateConnectionMode : BaseCreateConnectionMode
    {
        public int Edit { get; protected set; }
        protected bool IsEdit => Edit >= 0;

        public override void OnMouseUp(Event e) => Tool.SetMode(ToolModeType.CreateConnection);
        public override bool OnEscape()
        {
            Tool.SetMode(ToolModeType.CreateConnection);
            return true;
        }

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            for (var i = 0; i < Circles.Count; i += 1)
            {
                if (IsEdit && IsSnapping(Circles[Edit]) && Utility.NotPressed && Math.Abs(i - Edit) == 1 && Circle.IsSnapping(Circles[i], Circles[Edit]))
                    Circles[i].RenderCircle(cameraInfo, Colors.Orange, Underground);
                else
                    Circles[i].RenderCircle(cameraInfo, i == Edit ? Colors.Green : Colors.Green.SetAlpha(64), Underground);
            }

            base.RenderCalculatedOverlay(cameraInfo, info);
        }
        protected override void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            for (var i = 0; i < Circles.Count; i += 1)
            {
                var color = Circles[i].IsCorrect ? (i == Edit ? Colors.Green : Colors.Green.SetAlpha(64)) : Colors.Red;
                Circles[i].RenderCircle(cameraInfo, color, Underground);
            }

            base.RenderFailedOverlay(cameraInfo, info);
        }
        protected abstract bool IsSnapping(Circle circle);
    }
}
