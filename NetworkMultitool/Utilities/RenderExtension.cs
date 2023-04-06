using ModsCommon.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ColossalFramework.Math.VectorUtils;
using static ModsCommon.Utilities.VectorUtilsExtensions;

namespace NetworkMultitool.Utilities
{
    public static class RenderExtension
    {
        public static void RenderMeasure(this StraightTrajectory trajectory, RenderManager.CameraInfo cameraInfo, float shift, float length, Vector3 shiftDir, Color color, Color colorArrow, bool underground)
        {
            var data = new OverlayData(cameraInfo) { Color = color, RenderLimit = underground };
            var dataArrow = new OverlayData(cameraInfo) { Color = colorArrow, RenderLimit = underground };

            var isShort = trajectory.Length <= 10f;

            var startShift = trajectory.StartPosition + shiftDir * (shift + length) + (isShort ? -trajectory.Direction : trajectory.Direction) * 0.5f;
            var endShift = trajectory.EndPosition + shiftDir * (shift + length) + (isShort ? trajectory.Direction : -trajectory.Direction) * 0.5f;

            new StraightTrajectory(trajectory.StartPosition + shiftDir * shift, trajectory.StartPosition + shiftDir * (shift + length + 2f)).Render(data);
            new StraightTrajectory(trajectory.EndPosition + shiftDir * shift, trajectory.EndPosition + shiftDir * (shift + length + 2f)).Render(data);
            new StraightTrajectory(startShift, endShift).Render(dataArrow);

            var cross = CrossXZ(shiftDir, trajectory.Direction) > 0f;
            var dirP45 = shiftDir.TurnDeg(45f, isShort ^ cross);
            var dirM45 = shiftDir.TurnDeg(45f, !isShort ^ cross);

            new StraightTrajectory(startShift, startShift + dirP45 * 3f).Render(dataArrow);
            new StraightTrajectory(startShift, startShift - dirM45 * 3f).Render(dataArrow);

            new StraightTrajectory(endShift, endShift - dirP45 * 3f).Render(dataArrow);
            new StraightTrajectory(endShift, endShift + dirM45 * 3f).Render(dataArrow);
        }

        public static void RenderMeasure(this BezierTrajectory trajectory, RenderManager.CameraInfo cameraInfo, float shift, float length, Color color, Color colorArrow, bool underground)
        {
            var data = new OverlayData(cameraInfo) { Color = color, RenderLimit = underground };
            var dataArrow = new OverlayData(cameraInfo) { Color = colorArrow, RenderLimit = underground };

            var startShiftDir = trajectory.StartDirection.Turn90(true);
            var endShiftDir = trajectory.EndDirection.Turn90(false);

            var isShort = trajectory.Length <= 10f;

            var startShift = trajectory.StartPosition + startShiftDir * (shift + length) + (isShort ? -trajectory.Direction : trajectory.Direction) * 0.5f;
            var endShift = trajectory.EndPosition + endShiftDir * (shift + length) + (isShort ? trajectory.Direction : -trajectory.Direction) * 0.5f;

            new StraightTrajectory(trajectory.StartPosition + startShiftDir * shift, trajectory.StartPosition + startShiftDir * (shift + length + 2f)).Render(data);
            new StraightTrajectory(trajectory.EndPosition + endShiftDir * shift, trajectory.EndPosition + endShiftDir * (shift + length + 2f)).Render(data);
            new BezierTrajectory(startShift, trajectory.StartDirection, endShift, trajectory.EndDirection, BezierTrajectory.Data.Default).Render(dataArrow);

            var startCross = CrossXZ(startShiftDir, trajectory.StartDirection) > 0f;
            var startDirP45 = startShiftDir.TurnDeg(45f, isShort ^ startCross);
            var startDirM45 = startShiftDir.TurnDeg(45f, !isShort ^ startCross);
            new StraightTrajectory(startShift, startShift + startDirP45 * 3f).Render(dataArrow);
            new StraightTrajectory(startShift, startShift - startDirM45 * 3f).Render(dataArrow);

            var endCross = CrossXZ(endShiftDir, -trajectory.EndDirection) > 0f;
            var endDirP45 = endShiftDir.TurnDeg(45f, isShort ^ endCross);
            var endDirM45 = endShiftDir.TurnDeg(45f, !isShort ^ endCross);
            new StraightTrajectory(endShift, endShift - endDirP45 * 3f).Render(dataArrow);
            new StraightTrajectory(endShift, endShift + endDirM45 * 3f).Render(dataArrow);
        }
    }
}
