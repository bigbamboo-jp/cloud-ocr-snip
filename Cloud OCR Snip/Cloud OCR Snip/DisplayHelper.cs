using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Cloud_OCR_Snip
{
    /// <summary>
    /// ディスプレイの物理座標を取得するヘルパー。
    /// アプリケーションがPer-Monitor DPI対応で動作していることを前提に、
    /// Screen.Boundsをキャプチャ用の物理ピクセル座標として扱う。
    /// </summary>
    internal static class DisplayHelper
    {
        public static IReadOnlyList<Rectangle> GetMonitorBounds()
        {
            return System.Windows.Forms.Screen.AllScreens
                .Select(screen => screen.Bounds)
                .ToArray();
        }

        public static Rectangle GetVirtualBounds(IReadOnlyList<Rectangle> monitor_bounds)
        {
            if (monitor_bounds.Count == 0)
                return Rectangle.Empty;

            int left = monitor_bounds.Min(bounds => bounds.Left);
            int top = monitor_bounds.Min(bounds => bounds.Top);
            int right = monitor_bounds.Max(bounds => bounds.Right);
            int bottom = monitor_bounds.Max(bounds => bounds.Bottom);

            return Rectangle.FromLTRB(left, top, right, bottom);
        }
    }
}
