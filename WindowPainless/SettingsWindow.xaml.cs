using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using WindowPainless.Properties;
using WindowPainless.WPF;

using static WindowPainless.NativeMethods;

namespace WindowPainless
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private int _hotkeyId;

        private IntPtr _windowHandle;
        private HwndSource _source;

        private uint _lastKey;
        private int _repeatCount;

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
            { VK_NUMPAD7, Orientation.TopLeft },
            { VK_NUMPAD8, Orientation.TopCenter },
            { VK_NUMPAD9, Orientation.TopRight },
            { VK_NUMPAD4, Orientation.MiddleLeft },
            { VK_NUMPAD5, Orientation.MiddleCenter },
            { VK_NUMPAD6, Orientation.MiddleRight },
            { VK_NUMPAD1, Orientation.BottomLeft },
            { VK_NUMPAD2, Orientation.BottomCenter },
            { VK_NUMPAD3, Orientation.BottomRight }
        };

        private static bool IsOdd(int number) => number % 2 != 0;

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
        }

        private void InitializeAlignment()
        {
            foreach (Orientation orientation in Enum.GetValues(typeof(Orientation)))
            {
                _orientationToDivisions[orientation] = new List<Division>();
            }

            // Top keys
            _orientationToDivisions[Orientation.TopLeft].AddRange(ActiveDevisions.Where(d =>
                d.X <= RoundDownHalf(d.Width) &&
                d.Y <= RoundDownHalf(d.Height)).ToArray());

            _orientationToDivisions[Orientation.TopCenter].AddRange(ActiveDevisions.Where(d =>
                (d.Width > 1 || d.Height > 1) &&
                IsOdd(d.Width) &&
                d.X == (d.Width / 2) + 1 &&
                d.Y <= RoundDownHalf(d.Height)).ToArray());

            _orientationToDivisions[Orientation.TopRight].AddRange(ActiveDevisions.Where(d =>
                d.X > RoundUpHalf(d.Width) &&
                d.Y <= RoundDownHalf(d.Height)).ToArray());

            // Middle keys
            _orientationToDivisions[Orientation.MiddleLeft].AddRange(ActiveDevisions.Where(d =>
                (d.Width > 1 || d.Height > 1) &&
                IsOdd(d.Height) &&
                d.X <= RoundDownHalf(d.Width) &&
                d.Y == (d.Height / 2) + 1));

            _orientationToDivisions[Orientation.MiddleCenter].AddRange(ActiveDevisions.Where(d =>
                IsOdd(d.Width) &&
                IsOdd(d.Height) &&
                d.X == (d.Width / 2) + 1 &&
                d.Y == (d.Height / 2) + 1));

            _orientationToDivisions[Orientation.MiddleRight].AddRange(ActiveDevisions.Where(d =>
                (d.Width > 1 || d.Height > 1) &&
                IsOdd(d.Height) &&
                IsOdd(d.Height) &&
                d.X > RoundUpHalf(d.Width) &&
                d.Y == (d.Height / 2) + 1));

            // Bottom keys
            _orientationToDivisions[Orientation.BottomLeft].AddRange(ActiveDevisions.Where(d =>
                d.X <= RoundDownHalf(d.Width) &&
                d.Y > RoundUpHalf(d.Height)).ToArray());

            _orientationToDivisions[Orientation.BottomCenter].AddRange(ActiveDevisions.Where(d =>
                (d.Width > 1 || d.Height > 1) &&
                IsOdd(d.Width) &&
                d.X == (d.Width / 2) + 1 &&
                d.Y > RoundUpHalf(d.Height)).ToArray());

            _orientationToDivisions[Orientation.BottomRight].AddRange(ActiveDevisions.Where(d =>
                d.X > RoundUpHalf(d.Width) &&
                d.Y > RoundUpHalf(d.Height)).ToArray());

            //foreach (var gridToOrientation in _orientationToDivisions)
            //{
            //    Console.WriteLine(gridToOrientation.Key);

            //    var strings = gridToOrientation.Value.Select(d => d.ToString()).ToArray();

            //    foreach (var s in strings)
            //    {
            //        Console.WriteLine($"  {s}");
            //    }
            //}
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

            Console.WriteLine($"Orientation: {orientation}");

            if (_orientationToDivisions[orientation].Count == 0)
            {
                return IntPtr.Zero;
            }

            if (virtualKey == _lastKey)
            {
                _repeatCount = (_repeatCount + 1) % _orientationToDivisions[orientation].Count;
            }
            else
            {
                _repeatCount = 0;
            }

            _lastKey = virtualKey;

            var division = _orientationToDivisions[orientation][_repeatCount];

            ResizeForegroundWindow(division);

            handled = true;

            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _source.RemoveHook(HwndHook);
            _source.Dispose();

            UnregisterHotKey(_windowHandle, _hotkeyId);

            base.OnClosed(e);
        }

        private readonly List<WPF.GridControl> _grids = new List<GridControl>();
        private bool _updatingGrids;

        private void UpdateGrids()
        {
            if (gridWrapPanel == null)
            {
                return;
            }

            _updatingGrids = true;

            _grids.Clear();
            gridWrapPanel.Children.Clear();

            for (var i = 1; i <= maximumRows.Value; i++)
            {
                for (var j = 1; j <= maximumColumns.Value; j++)
                {
                    var grid = new WPF.GridControl() { Rows = i, Columns = j };

                    grid.DivisionsChanged += GridOnDivisionsChanged;

                    _grids.Add(grid);
                    gridWrapPanel.Children.Add(grid);
                }
            }

            if (Settings.Default.DivisionPreferences == null)
            {
                Console.WriteLine("Initializing settings for the first time");

                Settings.Default.DivisionPreferences = new DivisionPreferences();

                foreach (var divisionRectangle in _grids.SelectMany(gridControl => gridControl.Divisions))
                {
                    Settings.Default.DivisionPreferences[divisionRectangle.Division] = true;
                }

                Settings.Default.Save();
            }

            foreach (var divisionRectangle in _grids.SelectMany(gridControl => gridControl.Divisions))
            {
                if (Settings.Default.DivisionPreferences.ContainsKey(divisionRectangle.Division))
                {
                    Console.WriteLine($"set {divisionRectangle.Division} to {Settings.Default.DivisionPreferences[divisionRectangle.Division]} from settings");

                    divisionRectangle.Enabled =
                        Settings.Default.DivisionPreferences[divisionRectangle.Division];
                }
                else
                {
                    Console.WriteLine($"set {divisionRectangle.Division} to True");

                    divisionRectangle.Enabled = true;
                }
            }

            _updatingGrids = false;
        }

        private void GridOnDivisionsChanged(object sender, DivisionsChangedEventArgs divisionsChangedEventArgs)
        {
            if (_updatingGrids)
            {
                return;
            }

            Console.WriteLine("GridOnDvisionsChanged");

            SaveSettings();
        }

        private void SaveSettings()
        {
            foreach (var divisionRectangle in _grids.SelectMany(gridControl => gridControl.Divisions))
            {
                Console.WriteLine($"set settings {divisionRectangle.Division} to {divisionRectangle.Enabled}");

                Settings.Default.DivisionPreferences[divisionRectangle.Division] =
                    divisionRectangle.Enabled;
            }

            InitializeAlignment();

            Settings.Default.Save();
        }

        private static IEnumerable<Division> ActiveDevisions
        {
            get
            {
                return Settings.Default.DivisionPreferences
                    .Where(kvp => kvp.Value)
                    .Select(kvp => kvp.Key);
            }
        }

        private void RemoveUnneededGridSizes()
        {
            if (Settings.Default.DivisionPreferences == null)
            {
                return;
            }

            // Remove any divisions that are no longer needed
            var divisionsToRemove = Settings.Default.DivisionPreferences.Where(d =>
                d.Key.Width > Settings.Default.MaximumColumns ||
                d.Key.Height > Settings.Default.MaximumRows).ToArray();

            foreach (var d in divisionsToRemove)
            {
                Settings.Default.DivisionPreferences.Remove(d.Key);
            }

            if (divisionsToRemove.Length > 0)
            {
                Settings.Default.Save();
            }
        }

        private void maximumRows_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RemoveUnneededGridSizes();
            UpdateGrids();
        }

        private void maximumColumns_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            RemoveUnneededGridSizes();
            UpdateGrids();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Settings.Default.Upgrade();

            UpdateGrids();

            InitializeAlignment();
            RegisterHotkeys();
        }
    }
}