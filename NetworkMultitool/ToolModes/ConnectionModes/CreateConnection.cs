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
            else if (State == Result.BigRadius)
                return Localize.Mode_Info_RadiusTooBig;
            else if (State == Result.WrongShape)
                return Localize.Mode_Info_WrongShape;
            else if (State != Result.Calculated)
                return
                    Localize.Mode_Info_ChooseDirestion + "\n" +
                    Localize.Mode_Info_CurveDurection;
            else
                return
                    Localize.Mode_Info_ChooseDirestion + "\n" +
                    Localize.Mode_Info_CurveDurection + "\n\n" +
                    string.Format(Localize.Mode_Info_ChangeBothRadius, DecreaseRadiusShortcut, IncreaseRadiusShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeCircle, SwitchSelectShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeOneRadius, DecreaseOneRadiusShortcut, IncreaseOneRadiusShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_SwitchOffset, SwitchOffsetShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_ChangeOffset, DecreaseOffsetShortcut, IncreaseOffsetShortcut) + "\n" +
                    string.Format(Localize.Mode_Info_Create, ApplyShortcut);
        }
        public override void OnToolUpdate()
        {
            base.OnToolUpdate();

            if (State != Result.None)
            {
                HoverCenter = -1;
                HoverCircle = -1;
                if (!IsHoverNode)
                {
                    var position = XZ(Tool.MouseWorldPosition);
                    for (var i = 0; i < Count; i += 1)
                    {
                        if ((XZ(CenterPos[i]) - position).sqrMagnitude <= 25f)
                        {
                            HoverCenter = i;
                            break;
                        }
                    }
                    for (var i = 0; i < Count; i += 1)
                    {
                        var magnitude = (XZ(CenterPos[i]) - position).magnitude;
                        if (Radius[i] - 5f <= magnitude && magnitude <= Radius[i] + 5f)
                        {
                            HoverCircle = i;
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
        public override void OnMouseDown(Event e)
        {
            if (IsHoverCenter)
                SelectCircle = HoverCenter;
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsHoverCenter)
                Tool.SetMode(ToolModeType.CreateConnectionMoveCircle);
            else if(IsHoverCircle)
                Tool.SetMode(ToolModeType.CreateConnectionChangeRadius);
        }
        protected override void ResetParams()
        {
            base.ResetParams();

            HoverCenter = -1;
            HoverCircle = -1;
        }

        protected override void IncreaseRadius()
        {
            Radius[0] = ChangeRadius(Radius[0], true);
            Radius[1] = ChangeRadius(Radius[1], true);
            State = Result.None;
        }
        protected override void DecreaseRadius()
        {
            Radius[0] = ChangeRadius(Radius[0], false);
            Radius[1] = ChangeRadius(Radius[1], false);
            State = Result.None;
        }
        private void IncreaseOneRadius()
        {
            Radius[SelectCircle] = ChangeRadius(Radius[SelectCircle], true);
            State = Result.None;
        }
        private void DecreaseOneRadius()
        {
            Radius[SelectCircle] = ChangeRadius(Radius[SelectCircle], false);
            State = Result.None;
        }
        private float? ChangeRadius(float? radius, bool increase)
        {
            if (radius != null)
            {
                var step = Step;
                return (radius.Value + (increase ? step : -step)).RoundToNearest(step);
            }
            else
                return radius;
        }
        private void SwitchSelectRadius() => SelectCircle = (SelectCircle + 1) % Count;

        private void IncreaseOffset()
        {
            Offset[SelectOffset] = ChangeOffset(Offset[SelectOffset], true);
            State = Result.None;
        }
        private void DecreaseOffset()
        {
            Offset[SelectOffset] = ChangeOffset(Offset[SelectOffset], false);
            State = Result.None;
        }
        private float ChangeOffset(float offset, bool increase)
        {
            var step = Step;
            return Mathf.Clamp((offset + (increase ? step : -step)).RoundToNearest(step), 0f, 500f);
        }
        private void SwitchSelectOffset() => SelectOffset = (SelectOffset + 1) % Count;

        protected override void RenderCalculatedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            for (var i = 0; i < Count; i += 1)
                RenderCircle(cameraInfo, i, i == HoverCircle ? Colors.Blue : Colors.Green.SetAlpha(64));

            base.RenderCalculatedOverlay(cameraInfo, info);

            for (var i = 0; i < Count; i += 1)
                RenderCenter(cameraInfo, i);
        }
        protected override void RenderFailedOverlay(RenderManager.CameraInfo cameraInfo, NetInfo info)
        {
            for (var i = 0; i < Count; i += 1)
                RenderCircle(cameraInfo, i, i == HoverCircle ? Colors.Blue : Colors.Red);

            base.RenderFailedOverlay(cameraInfo, info);

            for (var i = 0; i < Count; i += 1)
                RenderCenter(cameraInfo, i);
        }
        private void RenderCenter(RenderManager.CameraInfo cameraInfo, int i)
        {
            if (i == HoverCenter)
                RenderCenter(cameraInfo, i, Colors.Blue);
            else if (i == SelectCircle)
                RenderCenter(cameraInfo, i, Colors.Yellow);
        }
    }

    public class CreateConnectionMoveCircleMode : BaseAdditionalCreateConnectionMode
    {
        public override ToolModeType Type => ToolModeType.CreateConnectionMoveCircle;

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is CreateConnectionMode connectionMode)
                Edit = connectionMode.HoverCenter;
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsEdit)
            {
                var line = new StraightTrajectory(StartPos[Edit], StartPos[Edit] + StartDir[Edit], false);
                var normal = new StraightTrajectory(Tool.MouseWorldPosition, Tool.MouseWorldPosition + (CenterPos[Edit] - CurveStart[Edit]).normalized, false);

                Intersection.CalculateSingle(line, normal, out var t, out _);
                Offset[Edit] = Mathf.Clamp(t, 0f, 500f);
                State = Result.None;
            }
        }
    }

    public class CreateConnectionChangeRadiusMode : BaseAdditionalCreateConnectionMode
    {
        public override ToolModeType Type => ToolModeType.CreateConnectionChangeRadius;

        private Vector2 BeginPos { get; set; }

        protected override void Reset(IToolMode prevMode)
        {
            base.Reset(prevMode);

            if (prevMode is CreateConnectionMode connectionMode)
            {
                Edit = connectionMode.HoverCircle;
                BeginPos = XZ(connectionMode.CenterPos[Edit]);
            }
        }
        public override void OnMouseDrag(Event e)
        {
            if (IsEdit)
            {
                Radius[Edit] = (XZ(Tool.MouseWorldPosition) - BeginPos).magnitude;
                State = Result.None;
            }
        }
    }
}
