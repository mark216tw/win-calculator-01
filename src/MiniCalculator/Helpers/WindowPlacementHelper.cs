using System.Windows;
using Com.MiniCalculator.Models;

namespace Com.MiniCalculator.Helpers;

public static class WindowPlacementHelper
{
    public static void Restore(Window window, WindowPlacement placement)
    {
        if (!placement.HasValue)
        {
            return;
        }

        double width = Math.Max(placement.Width, window.MinWidth);
        double height = Math.Max(placement.Height, window.MinHeight);
        double left = placement.Left;
        double top = placement.Top;

        var target = new Rect(left, top, width, height);
        Rect workArea = SystemParameters.WorkArea;
        var virtualScreen = new Rect(
            SystemParameters.VirtualScreenLeft,
            SystemParameters.VirtualScreenTop,
            SystemParameters.VirtualScreenWidth,
            SystemParameters.VirtualScreenHeight);

        if (Intersects(target, workArea))
        {
            width = Math.Min(width, workArea.Width);
            height = Math.Min(height, workArea.Height);
            left = Math.Clamp(left, workArea.Left, Math.Max(workArea.Left, workArea.Right - width));
            top = Math.Clamp(top, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - height));
        }
        else if (!Intersects(target, virtualScreen))
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            return;
        }

        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = left;
        window.Top = top;
        window.Width = width;
        window.Height = height;

        if (placement.Maximized)
        {
            window.WindowState = WindowState.Maximized;
        }
    }

    public static void Save(Window window, WindowPlacement placement)
    {
        Rect bounds = window.RestoreBounds;
        if (bounds.IsEmpty || double.IsNaN(bounds.Width) || bounds.Width <= 0)
        {
            bounds = new Rect(window.Left, window.Top, window.Width, window.Height);
        }

        placement.Left = bounds.Left;
        placement.Top = bounds.Top;
        placement.Width = bounds.Width;
        placement.Height = bounds.Height;
        placement.Maximized = window.WindowState == WindowState.Maximized;
        placement.HasValue = true;
    }

    private static bool Intersects(Rect value, Rect bounds)
    {
        return value.Left < bounds.Right
            && value.Right > bounds.Left
            && value.Top < bounds.Bottom
            && value.Bottom > bounds.Top;
    }
}
