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
    public class CreateConnectionMode : BaseCreateConnectionMode
    {
        public static NetworkMultitoolShortcut IncreaseRadiiShortcut { get; } = GetShortcut(KeyCode.Equals, nameof(IncreaseRadiiShortcut), nameof(Localize.Settings_Shortcut_IncreaseRadii), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.IncreaseRadius(), ToolModeType.Create, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseRadiiShortcut { get; } = GetShortcut(KeyCode.Minus, nameof(DecreaseRadiiShortcut), nameof(Localize.Settings_Shortcut_DecreaseRadii), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.DecreaseRadius(), ToolModeType.Create, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut SwitchSelectShortcut { get; } = GetShortcut(KeyCode.Tab, nameof(SwitchSelectShortcut), nameof(Localize.Settings_Shortcut_SwitchSelect), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.SwitchSelectRadius());
        public static NetworkMultitoolShortcut IncreaseOneRadiusShortcut { get; } = GetShortcut(KeyCode.RightBracket, nameof(IncreaseOneRadiusShortcut), nameof(Localize.Settings_Shortcut_IncreaseOneRadius), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.IncreaseOneRadius(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseOneRadiusShortcut { get; } = GetShortcut(KeyCode.LeftBracket, nameof(DecreaseOneRadiusShortcut), nameof(Localize.Settings_Shortcut_DecreaseOneRadius), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.DecreaseOneRadius(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);

        public static NetworkMultitoolShortcut SwitchOffsetShortcut { get; } = GetShortcut(KeyCode.Tab, nameof(SwitchOffsetShortcut), nameof(Localize.Settings_Shortcut_SwitchOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.SwitchSelectOffset(), ctrl: true);
        public static NetworkMultitoolShortcut IncreaseOffsetShortcut { get; } = GetShortcut(KeyCode.Backslash, nameof(IncreaseOffsetShortcut), nameof(Localize.Settings_Shortcut_IncreaseOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.IncreaseOffset(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseOffsetShortcut { get; } = GetShortcut(KeyCode.Quote, nameof(DecreaseOffsetShortcut), nameof(Localize.Settings_Shortcut_DecreaseOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.DecreaseOffset(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                yield return ApplyShortcut;
                yield return IncreaseRadiiShortcut;
                yield return DecreaseRadiiShortcut;

                yield return SwitchSelectShortcut;
                yield return IncreaseOneRadiusShortcut;
                yield return DecreaseOneRadiusShortcut;

                yield return SwitchOffsetShortcut;
                yield return IncreaseOffsetShortcut;
                yield return DecreaseOffsetShortcut;

                yield return SwitchFollowTerrainShortcut;
            }
        }

        public override ToolModeType Type => ToolModeType.CreateConnection;

        public int HoverCenter { get; private set; }
        protected bool IsHoverCenter => HoverCenter != -1;
        public int HoverCircle { get; private set; }
        protected bool IsHoverCircle => HoverCircle != -1;
        public int HoverStraight { get; private set; }
        protected bool IsHoverStraight => HoverStraight != -1;
        public int PressedCenter { get; private set; }
        protected bool IsPressedCenter => PressedCenter != -1;
        public int PressedCircle { get; private set; }
        protected bool IsPressedCircle => PressedCircle != -1;

        private Vector3 LastPos { get; set; }
        private float PosTime { get; set; }

        protected override string GetInfo()
        {
            if (GetBaseInfo() is string baseInfo)
                return baseInfo;
            else if (IsHoverCenter)
            {
                var result = Localize.Mode_Connection_Info_DragToMove;
                if (HoverCenter != 0 && HoverCenter != Circles.Count - 1)
                    result += "\n" + Localize.Mode_Connection_Info_DoubleClickToRemove;
                return result;
            }
            else if (IsHoverCircle)
                return Localize.Mode_Connection_Info_DragToChangeRadius;
            else if (IsHoverStraight)
                return Localize.Mode_Connection_Info_DoubleClickToAdd;
            else if (State == Result.BigRadius)
                return Localize.Mode_Info_RadiusTooBig;
            else if (State == Result.WrongShape)
                return Localize.Mode_Info_WrongShape;
            else if (State == Result.OutOfMap)
                return Localize.Mode_Info_OutOfMap;
            else if (State != Result.Calculated)
                return
                    Localize.Mode_Info_ClickOnNodeToChangeCreateDir + "\n" +
                    Localize.Mode_Connection_Info_DoubleClickOnCenterToChangeDir;
            else
            {
                if (Tool.MousePosition != LastPos)
                {
                    LastPos = Tool.MousePosition;
                    PosTime = Time.realtimeSinceStartup;
                }

                var text =
                    Localize.Mode_Info_ClickOnNodeToChangeCreateDir + "\n" +
                    Localize.Mode_Connection_Info_DoubleClickOnCenterToChangeDir;

                if (Time.realtimeSinceStartup - PosTime >= 2f)
                {
                    text += "\n\n" +
                    string.Format(Localize.Mode_Info_ChangeBothRadius, DecreaseRadiiShortcut, IncreaseRadiiShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeCircle, SwitchSelectShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeOneRadius, DecreaseOneRadiusShortcut, IncreaseOneRadiusShortcut) + "\n" +
                    //string.Format(Localize.Mode_Info_SwitchOffset, SwitchOffsetShortcut) + "\n" +
                    //string.Format(Localize.Mode_Info_ChangeOffset, DecreaseOffsetShortcut, IncreaseOffsetShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_SwitchFollowTerrain, SwitchFollowTerrainShortcut) + "\n" +
                    Localize.Mode_Info_Step + "\n" +
                    string.Format(Localize.Mode_Info_Connection_Create, ApplyShortcut);
                }

                return text;
            }
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (State != Result.None)
            {
                HoverCenter = -1;
                HoverCircle = -1;
                HoverStraight = -1;
                if (!IsHoverNode && Tool.MouseRayValid)
                {
                    for (var i = 0; i < Circles.Count; i += 1)
                    {
                        if (Circles[i] == null)
                            continue;

                        var mousePosition = GetMousePosition(Circles[i].CenterPos.y);
                        if ((XZ(Circles[i].CenterPos) - XZ(mousePosition)).sqrMagnitude <= 25f)
                        {
                            HoverCenter = i;
                            break;
                        }
                    }
                    for (var i = 0; i < Circles.Count; i += 1)
                    {
                        if (Circles[i] == null)
                            continue;

                        var mousePosition = GetMousePosition(Circles[i].CenterPos.y);
                        var magnitude = (XZ(Circles[i].CenterPos) - XZ(mousePosition)).magnitude;
                        if (Circles[i].Radius - 5f <= magnitude && magnitude <= Circles[i].Radius + 5f)
                        {
                            HoverCircle = i;
                            break;
                        }
                    }
                    var info = Info;
                    for (var i = 1; i < Straights.Count - 1; i += 1)
                    {
                        if (Straights[i] == null || Straights[i].IsShort)
                            continue;

                        var mousePosition = GetMousePosition(Circles[i].CenterPos.y);
                        var normal = new StraightTrajectory(mousePosition, mousePosition + Straights[i].Direction.Turn90(true), false);
                        if (Intersection.CalculateSingle(Straights[i], normal, out _, out var t) && Mathf.Abs(t) <= info.m_halfWidth)
                        {
                            HoverStraight = i;
                            break;
                        }
                    }
                }
            }
        }
        public override void OnPrimaryMouseClicked(Event e)
        {
            PressedCenter = -1;
            PressedCircle = -1;

            if (IsHoverCenter)
                SelectCircle = HoverCenter;
            else if (!IsHoverCircle)
                base.OnPrimaryMouseClicked(e);
        }
        public override void OnPrimaryMouseDoubleClicked(Event e)
        {
            if (IsHoverCenter)
            {
                Circles[SelectCircle].Direction = Circles[SelectCircle].Direction == Direction.Right ? Direction.Left : Direction.Right;
                Recalculate();
            }
            else if (IsHoverStraight)
            {
                var circle = new Circle(AddLabel(), Height);
                circle.CenterPos = Tool.MouseWorldPosition;
                Circles.Insert(HoverStraight, circle);
                Straights.Insert(HoverStraight, null);
                Recalculate();
            }
        }
        public override void OnSecondaryMouseClicked()
        {
            if (!IsHoverCenter)
                base.OnSecondaryMouseClicked();
        }
        public override void OnSecondaryMouseDoubleClicked()
        {
            if (IsHoverCenter && HoverCenter != 0 && HoverCenter != Circles.Count - 1)
            {
                RemoveLabel(Circles[HoverCenter].Label);
                RemoveLabel(Straights[HoverCenter + 1].Label);
                Circles.RemoveAt(HoverCenter);
                Straights.RemoveAt(HoverCenter + 1);
                Recalculate();
            }
        }

        public override void OnMouseDown(Event e)
        {
            PressedCenter = HoverCenter;
            PressedCircle = HoverCircle;

            if (IsHoverCenter)
                SelectCircle = HoverCenter;
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsPressedCenter)
                Tool.SetMode(ToolModeType.CreateConnectionMoveCircle);
            else if (IsPressedCircle)
                Tool.SetMode(ToolModeType.CreateConnectionChangeRadius);
        }
        protected override void ResetParams()
        {
            base.ResetParams();

            HoverCenter = -1;
            HoverCircle = -1;
            HoverStraight = -1;
            PressedCenter = -1;
            PressedCircle = -1;
        }

        protected override void IncreaseRadius()
        {
            foreach (var circle in Circles)
                ChangeRadius(circle, true);

            Recalculate();
        }
        protected override void DecreaseRadius()
        {
            foreach (var circle in Circles)
                ChangeRadius(circle, false);

            Recalculate();
        }
        private void IncreaseOneRadius()
        {
            ChangeRadius(Circles[SelectCircle], true);
            Recalculate();
        }
        private void DecreaseOneRadius()
        {
            ChangeRadius(Circles[SelectCircle], false);
            Recalculate();
        }
        private void ChangeRadius(Circle circle, bool increase)
        {
            var step = Step;
            circle.Radius = (circle.Radius + (increase ? step : -step)).RoundToNearest(step);
        }
        private void SwitchSelectRadius() => SelectCircle = (SelectCircle + 1) % Circles.Count;

        private void IncreaseOffset()
        {
            ChangeOffset((SelectOffset ? Circles.First() : Circles.Last()) as EdgeCircle, true);
            Recalculate();
        }
        private void DecreaseOffset()
        {
            ChangeOffset((SelectOffset ? Circles.First() : Circles.Last()) as EdgeCircle, false);
            Recalculate();
        }
        private void ChangeOffset(EdgeCircle circle, bool increase)
        {
            var step = Step;
            circle.Offset = Mathf.Clamp((circle.Offset + (increase ? step : -step)).RoundToNearest(step), 0f, 500f);
        }
        private void SwitchSelectOffset() => SelectOffset = !SelectOffset;

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            if (IsHoverStraight)
                Straights[HoverStraight].Render(new OverlayData(cameraInfo) { Width = info.m_halfWidth * 2f + 0.7f, RenderLimit = Underground, Cut = true });

            for (var i = 0; i < Circles.Count; i += 1)
                Circles[i].RenderCircle(cameraInfo, i == HoverCircle ? Colors.Blue : Colors.Green.SetAlpha(64), Underground);

            foreach (var circle in Circles)
                circle.Render(cameraInfo, info, Colors.Gray224, Underground);

            for (var i = 0; i < Straights.Count; i += 1)
            {
                if (i == 0 || i == Straights.Count - 1 || !Straights[i].IsShort)
                {
                    var colorArrow = i == (SelectOffset ? 0 : Straights.Count - 1) ? Colors.Yellow : Colors.Gray224;
                    Straights[i].Render(cameraInfo, info, Colors.Gray224, colorArrow, Underground);
                }
            }

            for (var i = 0; i < Circles.Count; i += 1)
                RenderCenter(cameraInfo, i);
        }
        protected override void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            for (var i = 0; i < Circles.Count; i += 1)
                Circles[i].RenderCircle(cameraInfo, i == HoverCircle ? Colors.Blue : Colors.Red, Underground);

            base.RenderFailedOverlay(cameraInfo, info);

            for (var i = 0; i < Circles.Count; i += 1)
                RenderCenter(cameraInfo, i);
        }
        private void RenderCenter(RenderManager.CameraInfo cameraInfo, int i)
        {
            if (i == HoverCenter)
                Circles[i].RenderCenterHover(cameraInfo, Colors.Blue, Underground);
            else if (i == SelectCircle)
                Circles[i].RenderCenterHover(cameraInfo, Colors.Yellow, Underground);
        }
    }

    public class CreateConnectionMoveCircleMode : BaseAdditionalCreateConnectionMode
    {
        public override ToolModeType Type => ToolModeType.CreateConnectionMoveCircle;
        private Vector3 PrevCursor { get; set; }
        private Vector3 PrevCenter { get; set; }

        protected override string GetInfo() => Localize.Mode_Connection_Info_SlowMove;
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is CreateConnectionMode connectionMode)
            {
                Edit = connectionMode.PressedCenter;
                PrevCursor = GetMousePosition(Circles[Edit].CenterPos.y);
                PrevCenter = Circles[Edit].CenterPos;
            }
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsEdit && Circles[Edit] is Circle circle)
            {
                var newCursor = GetMousePosition(circle.CenterPos.y);
                var dir = newCursor - PrevCursor;
                PrevCursor = newCursor;

                if (Utility.OnlyCtrlIsPressed)
                    circle.CenterPos += dir * 0.1f;
                else if (Utility.OnlyAltIsPressed)
                    circle.CenterPos += dir * 0.01f;
                else
                {
                    var newCenter = PrevCenter + dir;
                    circle.CenterPos = newCenter;
                    PrevCenter = circle.CenterPos;
                    circle.SnappingPosition(Circles);
                }

                Recalculate();
            }
        }
        protected override bool IsSnapping(Circle circle) => circle.PossiblePositionSnapping;
    }

    public class CreateConnectionChangeRadiusMode : BaseAdditionalCreateConnectionMode
    {
        public override ToolModeType Type => ToolModeType.CreateConnectionChangeRadius;

        private Vector2 PrevCursor { get; set; }

        protected override string GetInfo() => Localize.Mode_Connection_Info_RadiusStep;
        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is CreateConnectionMode connectionMode)
            {
                Edit = connectionMode.PressedCircle;
                PrevCursor = XZ(Circles[Edit].CenterPos);
            }
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsEdit && Circles[Edit] is Circle circle)
            {
                var radius = (XZ(GetMousePosition(PrevCursor.y)) - PrevCursor).magnitude;

                if (Utility.OnlyShiftIsPressed)
                    circle.Radius = radius.RoundToNearest(10f);
                else if (Utility.OnlyCtrlIsPressed)
                    circle.Radius = radius.RoundToNearest(1f);
                else if (Utility.OnlyAltIsPressed)
                    circle.Radius = radius.RoundToNearest(0.1f);
                else
                {
                    circle.Radius = radius;
                    circle.SnappingRadius(Circles);
                }

                Recalculate();
            }
        }
        protected override bool IsSnapping(Circle circle) => circle.PossibleRadiusSnapping;
    }
}
