using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;

using static WindowPainless.NativeMethods;

namespace WindowPainless
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings
    {
        public Settings()
        {
            InitializeComponent();
        }

        private int _hotkeyId;

        private IntPtr _windowHandle;
        private HwndSource _source;

        private uint _lastKey;
        private int _repeatCount;

        // TODO: remove these in favor of user configuration
        private const int HORIZONTAL_DIVISIONS = 5;
        private const int VERTICAL_DIVISIONS = 5;

        private enum Orientation
        {
            TopLeft,
            TopCenter,
            TopRight,

            MiddleLeft,
            MiddleCenter,
            MiddleRight,

            BottomLeft,
            BottomCenter,
            BottomRight
        }

        private readonly Dictionary<Orientation, List<Division>> _orientationToDivisions = new Dictionary<Orientation, List<Division>>();

        private readonly Dictionary<uint, Orientation> _virtualKeyToOrientation = new Dictionary<uint, Orientation>()
        {
            {VK_NUMPAD7, Orientation.TopLeft},
            {VK_NUMPAD8, Orientation.TopCenter},
            {VK_NUMPAD9, Orientation.TopRight},

            {VK_NUMPAD4, Orientation.MiddleLeft},
            {VK_NUMPAD5, Orientation.MiddleCenter},
            {VK_NUMPAD6, Orientation.MiddleRight},

            {VK_NUMPAD1, Orientation.BottomLeft},
            {VK_NUMPAD2, Orientation.BottomCenter},
            {VK_NUMPAD3, Orientation.BottomRight}
        };

        private static bool IsOdd(int number) => number % 2 != 0;

        private struct Grid
        {
            public int Width { get; }
            public int Height { get; }

            public Grid(int width, int height)
            {
                Width = width;
                Height = height;
            }

            public override string ToString()
                => $"Grid: {Width}x{Height}";
        }

        private struct Division
        {
            public int X { get; }
            public int Y { get; }

            private Grid _grid;

            public Division(Grid grid, int x, int y)
            {
                _grid = grid;

                X = x;
                Y = y;
            }

            public int Width => _grid.Width;
            public int Height => _grid.Height;

            public InteropRectangle Bounds(Rect workArea)
            {
                var divisionWidth = workArea.Width / Width;
                var divisionHeight = workArea.Height / Height;

                return new InteropRectangle()
                {
                    Left = (int)(workArea.Left + (divisionWidth * (X - 1))),
                    Top = (int)(workArea.Top + (divisionHeight * (Y - 1))),
                    Right = (int)(workArea.Left + (divisionWidth * X)),
                    Bottom = (int)(workArea.Top + (divisionHeight * Y)),
                };
            }

            public override string ToString()
                => $"Division: {_grid}, X: {X}, Y: {Y}";
        }

        private static int RoundUpHalf(int number)
            => (int)Math.Round(((double)number / 2) + 0.05);

        private static int RoundDownHalf(int number)
            => (int)Math.Round(((double)number / 2) - 0.05);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            _windowHandle = new WindowInteropHelper(this).Handle;
            _source = HwndSource.FromHwnd(_windowHandle);

            Debug.Assert(_source != null, "_source != null");

            _hotkeyId = _source.GetType().GetHashCode();
            _source.AddHook(HwndHook);

            InitializeAlignment();
            RegisterHotkeys();
        }

        private void InitializeAlignment()
        {
            foreach (Orientation orientation in Enum.GetValues(typeof(Orientation)))
            {
                _orientationToDivisions[orientation] = new List<Division>();
            }

            var divisions = new List<Division>();

            // TODO: construct grid list from user preferences
            var grids = from width in 1.To(HORIZONTAL_DIVISIONS)
                        from height in 1.To(VERTICAL_DIVISIONS)
                        select new Grid(width, height);

            foreach (var grid in grids)
            {
                divisions.AddRange(from x in 1.To(grid.Width)
                                   from y in 1.To(grid.Height)
                                   select new Division(grid, x, y));
            }

            // Top keys
            _orientationToDivisions[Orientation.TopLeft].AddRange(divisions.Where(d =>
                d.X <= RoundDownHalf(d.Width) &&
                d.Y <= RoundDownHalf(d.Height)).ToArray());

            _orientationToDivisions[Orientation.TopCenter].AddRange(divisions.Where(d =>
                (d.Width > 1 || d.Height > 1) &&
                IsOdd(d.Width) &&
                d.X == (d.Width / 2) + 1 &&
                d.Y <= RoundDownHalf(d.Height)).ToArray());

            _orientationToDivisions[Orientation.TopRight].AddRange(divisions.Where(d =>
                d.X > RoundUpHalf(d.Width) &&
                d.Y <= RoundDownHalf(d.Height)).ToArray());

            // Middle keys
            _orientationToDivisions[Orientation.MiddleLeft].AddRange(divisions.Where(d =>
                (d.Width > 1 || d.Height > 1) &&
                IsOdd(d.Height) &&
                d.X <= RoundDownHalf(d.Width) &&
                d.Y == (d.Height / 2) + 1));

            _orientationToDivisions[Orientation.MiddleCenter].AddRange(divisions.Where(d =>
                IsOdd(d.Width) &&
                IsOdd(d.Height) &&
                d.X == (d.Width / 2) + 1 &&
                d.Y == (d.Height / 2) + 1));

            _orientationToDivisions[Orientation.MiddleRight].AddRange(divisions.Where(d =>
                (d.Width > 1 || d.Height > 1) &&
                IsOdd(d.Height) &&
                IsOdd(d.Height) &&
                d.X > RoundUpHalf(d.Width) &&
                d.Y == (d.Height / 2) + 1));

            // Bottom keys
            _orientationToDivisions[Orientation.BottomLeft].AddRange(divisions.Where(d =>
                d.X <= RoundDownHalf(d.Width) &&
                d.Y > RoundUpHalf(d.Height)).ToArray());

            _orientationToDivisions[Orientation.BottomCenter].AddRange(divisions.Where(d =>
                (d.Width > 1 || d.Height > 1) &&
                IsOdd(d.Width) &&
                d.X == (d.Width / 2) + 1 &&
                d.Y > RoundUpHalf(d.Height)).ToArray());

            _orientationToDivisions[Orientation.BottomRight].AddRange(divisions.Where(d =>
                d.X > RoundUpHalf(d.Width) &&
                d.Y > RoundUpHalf(d.Height)).ToArray());

            foreach (var gridToOrientation in _orientationToDivisions)
            {
                Console.WriteLine(gridToOrientation.Key);

                var strings = gridToOrientation.Value.Select(d => d.ToString()).ToArray();

                foreach (var s in strings)
                {
                    Console.WriteLine($"  {s}");
                }
            }
        }

        private void RegisterHotkeys()
        {
            // TODO: get modifier from user configuration
            var modifier = MOD_CONTROL | MOD_ALT;

            var hotkeys = new[]
            {
                VK_NUMPAD0,
                VK_NUMPAD1,
                VK_NUMPAD2,
                VK_NUMPAD3,
                VK_NUMPAD4,
                VK_NUMPAD5,
                VK_NUMPAD6,
                VK_NUMPAD7,
                VK_NUMPAD8,
                VK_NUMPAD9
            };

            foreach (var hotkey in hotkeys)
            {
                var result = RegisterHotKey(_windowHandle, _hotkeyId, modifier, hotkey);

                Console.WriteLine(result ? "Hotkey {0} registered." : "Hotkey {0} not registered.", hotkey);
            }
        }

        private static void LogRect(string prefix, InteropRectangle rect)
            => Console.WriteLine($"{prefix + ":",-15} {rect.Left,-5} {rect.Top,-5} {rect.Right,-5} {rect.Bottom,-5}");

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg != WM_HOTKEY || wParam.ToInt32() != _hotkeyId)
            {
                return IntPtr.Zero;
            }

            var virtualKey = (uint)((int)lParam >> 16) & 0xFFFF;

            if (!_virtualKeyToOrientation.ContainsKey(virtualKey))
            {
                Console.WriteLine($"virtual key not found: {virtualKey}");

                return IntPtr.Zero;
            }

            var orientation = _virtualKeyToOrientation[virtualKey];

            if (virtualKey == _lastKey)
            {
                _repeatCount = (_repeatCount + 1) % _orientationToDivisions[orientation].Count;
            }
            else
            {
                _repeatCount = 0;
            }

            _lastKey = virtualKey;

            var foregroundWindow = GetForegroundWindow();

            // Normalizing here seems to affect the extended frame if the window was previously in the maximized state
            NormalizeWindow(foregroundWindow);

            var windowRect = GetWindowRect(foregroundWindow);
            var extendedFrameRect = GetExtendedFrameBounds(foregroundWindow);
            var frameRect = extendedFrameRect - windowRect;

            var division = _orientationToDivisions[orientation][_repeatCount];
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

            #region Logging
            Console.WriteLine($"Orientation: {orientation}");
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
            #endregion

            handled = true;

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);

            UnregisterHotKey(_windowHandle, _hotkeyId);

            base.OnClosed(e);
        }
    }
}
