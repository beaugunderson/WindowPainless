using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace WindowPainless
{
    public static class NativeMethods
    {
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_WIN = 0x0008;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_SHIFT = 0x0004;

        public const uint VK_NUMPAD0 = 0x60;
        public const uint VK_NUMPAD1 = 0x61;
        public const uint VK_NUMPAD2 = 0x62;
        public const uint VK_NUMPAD3 = 0x63;
        public const uint VK_NUMPAD4 = 0x64;
        public const uint VK_NUMPAD5 = 0x65;
        public const uint VK_NUMPAD6 = 0x66;
        public const uint VK_NUMPAD7 = 0x67;
        public const uint VK_NUMPAD8 = 0x68;
        public const uint VK_NUMPAD9 = 0x69;

        internal const int WM_HOTKEY = 0x0312;

        internal const uint SWP_SHOW_WINDOW = 0x0040;
        internal const uint SWP_FRAME_CHANGED = 0x0020;

        private const int SW_SHOW_MINIMIZED = 2;
        private const int SW_SHOW_NORMAL = 1;
        private const int SW_SHOW_MAXIMIZED = 3;

        private const uint DWMWA_EXTENDED_FRAME_BOUNDS = 9;

        public static bool MaximizeWindow(IntPtr hWnd)
        {
            return ShowWindow(hWnd, SW_SHOW_MAXIMIZED);
        }

        public static bool NormalizeWindow(IntPtr hWnd)
        {
            return ShowWindow(hWnd, SW_SHOW_NORMAL);
        }

        public static InteropRectangle GetWindowRect(IntPtr hWnd)
        {
            InteropRectangle result;

            GetWindowRect(hWnd, out result);

            return result;
        }

        public static InteropRectangle GetExtendedFrameBounds(IntPtr hWnd)
        {
            InteropRectangle result;

            DwmGetWindowAttribute(hWnd, DWMWA_EXTENDED_FRAME_BOUNDS, out result, Marshal.SizeOf(typeof(InteropRectangle)));

            return result;
        }

        public static void ResizeForegroundWindow(Division division)
        {
            var foregroundWindow = GetForegroundWindow();

            // Normalizing here seems to affect the extended frame if the window was previously in the maximized state
            NormalizeWindow(foregroundWindow);

            var windowRect = GetWindowRect(foregroundWindow);
            var extendedFrameRect = GetExtendedFrameBounds(foregroundWindow);
            var frameRect = extendedFrameRect - windowRect;

            var bounds = division.Bounds(SystemParameters.WorkArea);

            var x = bounds.Left - frameRect.Left;
            var y = bounds.Top - frameRect.Top;

            var width = bounds.Width + frameRect.Left - frameRect.Right;
            var height = bounds.Height + frameRect.Top - frameRect.Bottom;

            SetWindowPos(foregroundWindow, IntPtr.Zero, x, y, width, height, SWP_SHOW_WINDOW);

            // If the window should be maximized then actually maximize it or the title bar will appear slightly out of frame
            if (bounds.Left <= SystemParameters.WorkArea.Left &&
                bounds.Top <= SystemParameters.WorkArea.Top &&
                bounds.Right >= SystemParameters.WorkArea.Right &&
                bounds.Bottom >= SystemParameters.WorkArea.Bottom)
            {
                Console.WriteLine("maximized");

                MaximizeWindow(foregroundWindow);
            }

            Console.WriteLine(division);
            Console.WriteLine(bounds);
            Console.WriteLine();

            LogRect("window", windowRect);
            LogRect("extended frame", extendedFrameRect);

            Console.WriteLine("frame size:     {0,-5} {1,-5} {2,-5} {3,-5}", frameRect.Left, frameRect.Top, frameRect.Right, frameRect.Bottom);
            Console.WriteLine("bounds:         {0,-5} {1,-5} {2,-5} {3,-5}", bounds.Left, bounds.Top, bounds.Width, bounds.Height);
            Console.WriteLine("desired size:   {0,-5} {1,-5} {2,-5} {3,-5}", x, y, width, height);

            var afterWindowRect = GetWindowRect(foregroundWindow);
            var afterExtendedFrameRect = GetExtendedFrameBounds(foregroundWindow);

            LogRect("new size", afterWindowRect);
            LogRect("new extended", afterExtendedFrameRect);
            LogRect("difference", bounds - afterExtendedFrameRect);

            Console.WriteLine();
        }

        [DllImport("user32.dll")]
        internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetWindowRect(IntPtr hWnd, out InteropRectangle lpRect);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hWnd, uint dwAttribute, out InteropRectangle pvAttribute, int cbAttribute);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static void LogRect(string prefix, InteropRectangle rect)
            => Console.WriteLine($"{prefix + ":", -15} {rect.Left, -5} {rect.Top, -5} {rect.Right, -5} {rect.Bottom, -5}");

        [StructLayout(LayoutKind.Sequential)]
        public struct InteropRectangle
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public static InteropRectangle operator -(InteropRectangle a, InteropRectangle b)
                => new InteropRectangle()
                {
                    Left = a.Left - b.Left,
                    Right = a.Right - b.Right,
                    Top = a.Top - b.Top,
                    Bottom = a.Bottom - b.Bottom
                };

            public override string ToString()
                => $"InteropRectangle: {Left}, {Top}, {Right}, {Bottom}, {Width}x{Height}";
        }
    }
}