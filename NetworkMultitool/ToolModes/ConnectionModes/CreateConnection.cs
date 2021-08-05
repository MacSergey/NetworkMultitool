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
        public static NetworkMultitoolShortcut SwitchSelectShortcut = GetShortcut(KeyCode.Tab, nameof(SwitchSelectShortcut), nameof(Localize.Settings_Shortcut_SwitchSelect), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.SwitchSelectRadius());
        public static NetworkMultitoolShortcut IncreaseOneRadiusShortcut { get; } = GetShortcut(KeyCode.RightBracket, nameof(IncreaseOneRadiusShortcut), nameof(Localize.Settings_Shortcut_IncreaseOneRadius), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.IncreaseOneRadius(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseOneRadiusShortcut { get; } = GetShortcut(KeyCode.LeftBracket, nameof(DecreaseOneRadiusShortcut), nameof(Localize.Settings_Shortcut_DecreaseOneRadius), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.DecreaseOneRadius(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);

        public static NetworkMultitoolShortcut SwitchOffsetShortcut = GetShortcut(KeyCode.Tab, nameof(SwitchOffsetShortcut), nameof(Localize.Settings_Shortcut_SwitchOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.SwitchSelectOffset(), ctrl: true);
        public static NetworkMultitoolShortcut IncreaseOffsetShortcut { get; } = GetShortcut(KeyCode.Backslash, nameof(IncreaseOffsetShortcut), nameof(Localize.Settings_Shortcut_IncreaseOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.IncreaseOffset(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);
        public static NetworkMultitoolShortcut DecreaseOffsetShortcut { get; } = GetShortcut(KeyCode.Quote, nameof(DecreaseOffsetShortcut), nameof(Localize.Settings_Shortcut_DecreaseOffset), () => (SingletonTool<NetworkMultitoolTool>.Instance.Mode as CreateConnectionMode)?.DecreaseOffset(), ToolModeType.CreateConnection, repeat: true, ignoreModifiers: true);

        public override IEnumerable<NetworkMultitoolShortcut> Shortcuts
        {
            get
            {
                foreach (var shortcut in base.Shortcuts)
                    yield return shortcut;

                yield return SwitchSelectShortcut;
                yield return IncreaseOneRadiusShortcut;
                yield return DecreaseOneRadiusShortcut;

                yield return SwitchOffsetShortcut;
                yield return IncreaseOffsetShortcut;
                yield return DecreaseOffsetShortcut;
            }
        }

        public override ToolModeType Type => ToolModeType.CreateConnection;

        public int HoverCenter { get; private set; }
        protected bool IsHoverCenter => HoverCenter != -1;
        public int HoverCircle { get; private set; }
        protected bool IsHoverCircle => HoverCircle != -1;
        public int HoverStraight { get; private set; }
        protected bool IsHoverStraight => HoverStraight != -1;
        private Vector3 LastPos { get; set; }
        private float PosTime { get; set; }

        protected override string GetInfo()
        {
            if (!IsFirst)
            {
                if (!IsHoverSegment)
                    return Localize.Mode_Info_SelectFirstSegment + UndergroundInfo;
                else
                    return Localize.Mode_Info_ClickFirstSegment + StepOverInfo;
            }
            else if (!IsSecond)
            {
                if (!IsHoverSegment)
                    return Localize.Mode_Info_SelectSecondSegment + UndergroundInfo;
                else
                    return Localize.Mode_Info_ClickSecondSegment + StepOverInfo;
            }
            else if (IsHoverNode)
                return Localize.Mode_Info_ClickToChangeCreateDir;
            else if (IsHoverCenter)
            {
                var result = Localize.Mode_Connection_Info_DragToMove;
                if (HoverCenter == 0 || HoverCenter == Circles.Count - 1)
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
                    string.Format(Localize.Mode_Info_ChangeBothRadius, DecreaseRadiusShortcut, IncreaseRadiusShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeCircle, SwitchSelectShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeOneRadius, DecreaseOneRadiusShortcut, IncreaseOneRadiusShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_SwitchOffset, SwitchOffsetShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeOffset, DecreaseOffsetShortcut, IncreaseOffsetShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_Create, ApplyShortcut);
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
                if (!IsHoverNode)
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
                State = Result.None;
            }
            else if (IsHoverStraight)
            {
                var circle = new Circle(AddLabel(), Height);
                circle.CenterPos = Tool.MouseWorldPosition;
                Circles.Insert(HoverStraight, circle);
                Straights.Insert(HoverStraight, null);
                State = Result.None;
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
                Circles.RemoveAt(HoverCenter);
                Straights.RemoveAt(HoverCenter + 1);
                State = Result.None;
            }
        }

        public override void OnMouseDown(Event e)
        {
            if (IsHoverCenter)
                SelectCircle = HoverCenter;
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsHoverCenter)
                Tool.SetMode(ToolModeType.CreateConnectionMoveCircle);
            else if (IsHoverCircle)
                Tool.SetMode(ToolModeType.CreateConnectionChangeRadius);
        }
        protected override void ResetParams()
        {
            base.ResetParams();

            HoverCenter = -1;
            HoverCircle = -1;
            HoverStraight = -1;
        }

        protected override void IncreaseRadius()
        {
            foreach (var circle in Circles)
                ChangeRadius(circle, true);

            State = Result.None;
        }
        protected override void DecreaseRadius()
        {
            foreach (var circle in Circles)
                ChangeRadius(circle, false);

            State = Result.None;
        }
        private void IncreaseOneRadius()
        {
            ChangeRadius(Circles[SelectCircle], true);
            State = Result.None;
        }
        private void DecreaseOneRadius()
        {
            ChangeRadius(Circles[SelectCircle], false);
            State = Result.None;
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
            State = Result.None;
        }
        private void DecreaseOffset()
        {
            ChangeOffset((SelectOffset ? Circles.First() : Circles.Last()) as EdgeCircle, false);
            State = Result.None;
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
                Straights[i].Render(cameraInfo, info, Colors.Gray224, i == (SelectOffset ? 0 : Straights.Count - 1) ? Colors.Yellow : Colors.Gray224, Underground);

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
        public override bool CreateButton => false;
        private Vector3 PrevPos { get; set; }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is CreateConnectionMode connectionMode)
            {
                Edit = connectionMode.HoverCenter;
                PrevPos = GetMousePosition(Circles[Edit].CenterPos.y);
            }
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsEdit && Circles[Edit] is Circle circle)
            {
                var newPos = GetMousePosition(circle.CenterPos.y);
                var dir = newPos - PrevPos;
                PrevPos = newPos;

                if (Utility.OnlyCtrlIsPressed)
                    circle.CenterPos += dir * 0.1f;
                else if (Utility.OnlyAltIsPressed)
                    circle.CenterPos += dir * 0.01f;
                else
                    circle.CenterPos += dir;

                State = Result.None;
            }
        }
    }

    public class CreateConnectionChangeRadiusMode : BaseAdditionalCreateConnectionMode
    {
        public override ToolModeType Type => ToolModeType.CreateConnectionChangeRadius;
        public override bool CreateButton => false;

        private Vector2 PrevPos { get; set; }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is CreateConnectionMode connectionMode)
            {
                Edit = connectionMode.HoverCircle;
                PrevPos = XZ(Circles[Edit].CenterPos);
            }
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();
            PrevPos = XZ(Circles[Edit].CenterPos);
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsEdit && Circles[Edit] is Circle circle)
            {
                var radius = (XZ(GetMousePosition(PrevPos.y)) - PrevPos).magnitude;

                if (Utility.ShiftIsPressed)
                    radius = radius.RoundToNearest(1f);

                circle.Radius = radius;
                State = Result.None;
            }
        }
    }
}
