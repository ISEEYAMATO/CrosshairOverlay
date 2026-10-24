using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Drawing;

namespace CrosshairOverlay
{
    public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);
        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public Settings Settings { get; set; } = new Settings();

        private NotifyIcon? _tray;
        private SettingsWindow? _settingsWindow;
        private IntPtr _hwnd;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            Loaded += (s, e) => { InitializeOverlay(); App.HandleArgs(App.PendingArgs); App.PendingArgs = Array.Empty<string>(); };
        }

        private void InitializeOverlay()
        {
            Left = 0;
            Top = 0;
            Width = SystemParameters.PrimaryScreenWidth;
            Height = SystemParameters.PrimaryScreenHeight;

            _hwnd = new WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_LAYERED);

            var source = HwndSource.FromHwnd(_hwnd);
            source?.AddHook(WndProc);

            RegisterHotKey(_hwnd, 1, 0, 0x78); // F9 toggle
            RegisterHotKey(_hwnd, 2, 0, 0x79); // F10 settings
            RegisterHotKey(_hwnd, 3, 0x0002, 0x25); // Ctrl+Left
            RegisterHotKey(_hwnd, 4, 0x0002, 0x26); // Ctrl+Up
            RegisterHotKey(_hwnd, 5, 0x0002, 0x27); // Ctrl+Right
            RegisterHotKey(_hwnd, 6, 0x0002, 0x28); // Ctrl+Down

            SetupTray();
            OverlayCanvas.SizeChanged += (s, e) => DrawCrosshair();
            ApplySettings();
        }

        private void SetupTray()
        {
            System.Drawing.Icon trayIcon;
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string? resName = null;
                foreach (var n in asm.GetManifestResourceNames())
                {
                    if (n.EndsWith("crosshair.ico", StringComparison.OrdinalIgnoreCase)) { resName = n; break; }
                }
                if (resName != null)
                {
                    using var stream = asm.GetManifestResourceStream(resName);
                    trayIcon = stream != null ? new System.Drawing.Icon(stream) : System.Drawing.SystemIcons.Application;
                }
                else trayIcon = System.Drawing.SystemIcons.Application;
            }
            catch { trayIcon = System.Drawing.SystemIcons.Application; }

            _tray = new NotifyIcon
            {
                Icon = trayIcon,
                Text = "Crosshair Overlay",
                Visible = true
            };
            var menu = new ContextMenuStrip();
            var toggle = new ToolStripMenuItem("Toggle (F9)");
            toggle.Click += (s, e) => ToggleVisibility();
            var settings = new ToolStripMenuItem("Settings (F10)");
            settings.Click += (s, e) => OpenSettings();
            var exit = new ToolStripMenuItem("Exit");
            exit.Click += (s, e) => { _tray.Visible = false; Close(); };
            menu.Items.Add(toggle);
            menu.Items.Add(settings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exit);
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (s, e) => OpenSettings();
        }

        public void ToggleVisibility()
        {
            Settings.Visible = !Settings.Visible;
            ApplySettings();
            SaveSettings();
        }

        public void OpenSettings()
        {
            if (_settingsWindow == null || !_settingsWindow.IsLoaded)
            {
                _settingsWindow = new SettingsWindow(this);
                _settingsWindow.Show();
            }
            else
            {
                _settingsWindow.Activate();
            }
        }

        public void ApplySettings()
        {
            Topmost = Settings.Visible;
            Opacity = Settings.Visible ? Settings.Opacity : 0.0;
            DrawCrosshair();
            SaveSettings();
        }

        private void DrawBar(double x, double y, double w, double h, System.Windows.Media.Brush brush, System.Windows.Media.Brush outlineBrush, double outlineExtra)
        {
            var outline = new System.Windows.Shapes.Rectangle
            {
                Width = w + outlineExtra * 2,
                Height = h + outlineExtra * 2,
                Fill = outlineBrush
            };
            System.Windows.Controls.Canvas.SetLeft(outline, x - outlineExtra);
            System.Windows.Controls.Canvas.SetTop(outline, y - outlineExtra);
            OverlayCanvas.Children.Add(outline);

            var bar = new System.Windows.Shapes.Rectangle
            {
                Width = w,
                Height = h,
                Fill = brush
            };
            System.Windows.Controls.Canvas.SetLeft(bar, x);
            System.Windows.Controls.Canvas.SetTop(bar, y);
            OverlayCanvas.Children.Add(bar);
        }

        private void DrawCrosshair()
        {
            OverlayCanvas.Children.Clear();
            if (!Settings.Visible) return;

            double cx = (OverlayCanvas.ActualWidth > 0 ? OverlayCanvas.ActualWidth / 2 : Width / 2) + Settings.OffsetX;
            double cy = (OverlayCanvas.ActualHeight > 0 ? OverlayCanvas.ActualHeight / 2 : Height / 2) + Settings.OffsetY;

            System.Windows.Media.Color color;
            try { color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Settings.Color); }
            catch { color = System.Windows.Media.Colors.White; }
            System.Windows.Media.Color outlineColor;
            try { outlineColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(Settings.OutlineColor); }
            catch { outlineColor = System.Windows.Media.Colors.Black; }
            var brush = new System.Windows.Media.SolidColorBrush(color);
            var outlineBrush = new System.Windows.Media.SolidColorBrush(outlineColor);

            double L = Settings.Length;
            double T = Settings.Thickness;
            double G = Settings.Gap;

            switch (Settings.Shape)
            {
                case "Default":
                    {
                        double dotR = Math.Max(2, T);
                        double ringR = Math.Max(6, L * 0.5);
                        double barLen = L;
                        double sep = Math.Max(4, G);
                        double barThick = Math.Max(2, T);
                        double outlineExtra = 2;

                        // Outer bars
                        DrawBar(cx - barThick / 2, cy - (ringR + sep + barLen), barThick, barLen, brush, outlineBrush, outlineExtra);
                        DrawBar(cx - barThick / 2, cy + (ringR + sep), barThick, barLen, brush, outlineBrush, outlineExtra);
                        DrawBar(cx - (ringR + sep + barLen), cy - barThick / 2, barLen, barThick, brush, outlineBrush, outlineExtra);
                        DrawBar(cx + (ringR + sep), cy - barThick / 2, barLen, barThick, brush, outlineBrush, outlineExtra);

                        // Center ring
                        double ringOuter = ringR + outlineExtra;
                        var ringOutline = new Ellipse
                        {
                            Width = ringOuter * 2, Height = ringOuter * 2,
                            Stroke = outlineBrush, StrokeThickness = T + outlineExtra,
                            Fill = System.Windows.Media.Brushes.Transparent
                        };
                        System.Windows.Controls.Canvas.SetLeft(ringOutline, cx - ringOuter);
                        System.Windows.Controls.Canvas.SetTop(ringOutline, cy - ringOuter);
                        OverlayCanvas.Children.Add(ringOutline);

                        var ring = new Ellipse
                        {
                            Width = ringR * 2, Height = ringR * 2,
                            Stroke = brush, StrokeThickness = T,
                            Fill = System.Windows.Media.Brushes.Transparent
                        };
                        System.Windows.Controls.Canvas.SetLeft(ring, cx - ringR);
                        System.Windows.Controls.Canvas.SetTop(ring, cy - ringR);
                        OverlayCanvas.Children.Add(ring);

                        // Center dot
                        double dotOuter = dotR + outlineExtra;
                        var dotOutline = new Ellipse
                        {
                            Width = dotOuter * 2, Height = dotOuter * 2,
                            Fill = outlineBrush
                        };
                        System.Windows.Controls.Canvas.SetLeft(dotOutline, cx - dotOuter);
                        System.Windows.Controls.Canvas.SetTop(dotOutline, cy - dotOuter);
                        OverlayCanvas.Children.Add(dotOutline);

                        var dot = new Ellipse
                        {
                            Width = dotR * 2, Height = dotR * 2,
                            Fill = brush
                        };
                        System.Windows.Controls.Canvas.SetLeft(dot, cx - dotR);
                        System.Windows.Controls.Canvas.SetTop(dot, cy - dotR);
                        OverlayCanvas.Children.Add(dot);
                    }
                    break;
                case "Dot":
                    {
                        double r = Math.Max(1, L / 2);
                        var dotBrush = new System.Windows.Media.SolidColorBrush(color) { Opacity = Settings.Opacity };
                        var blackDot = new System.Windows.Shapes.Ellipse { Width = (r + 1.5) * 2, Height = (r + 1.5) * 2, Fill = System.Windows.Media.Brushes.Black };
                        System.Windows.Controls.Canvas.SetLeft(blackDot, cx - (r + 1.5));
                        System.Windows.Controls.Canvas.SetTop(blackDot, cy - (r + 1.5));
                        OverlayCanvas.Children.Add(blackDot);
                        var el = new System.Windows.Shapes.Ellipse { Width = r * 2, Height = r * 2, Fill = dotBrush };
                        System.Windows.Controls.Canvas.SetLeft(el, cx - r);
                        System.Windows.Controls.Canvas.SetTop(el, cy - r);
                        OverlayCanvas.Children.Add(el);
                    }
                    break;
                case "Circle":
                    {
                        double r = Math.Max(4, L);
                        var blackRing = new Ellipse { Width = (r + 1) * 2, Height = (r + 1) * 2, Stroke = System.Windows.Media.Brushes.Black, StrokeThickness = T + 2, Fill = System.Windows.Media.Brushes.Transparent };
                        System.Windows.Controls.Canvas.SetLeft(blackRing, cx - (r + 1));
                        System.Windows.Controls.Canvas.SetTop(blackRing, cy - (r + 1));
                        OverlayCanvas.Children.Add(blackRing);
                        var el = new Ellipse { Width = r * 2, Height = r * 2, Stroke = brush, StrokeThickness = T, Fill = System.Windows.Media.Brushes.Transparent };
                        System.Windows.Controls.Canvas.SetLeft(el, cx - r);
                        System.Windows.Controls.Canvas.SetTop(el, cy - r);
                        OverlayCanvas.Children.Add(el);
                        double dr = Math.Max(1, T);
                        var dotBlack = new Ellipse { Width = (dr + 1) * 2, Height = (dr + 1) * 2, Fill = System.Windows.Media.Brushes.Black };
                        System.Windows.Controls.Canvas.SetLeft(dotBlack, cx - (dr + 1));
                        System.Windows.Controls.Canvas.SetTop(dotBlack, cy - (dr + 1));
                        OverlayCanvas.Children.Add(dotBlack);
                        var dot = new Ellipse { Width = dr * 2, Height = dr * 2, Fill = brush };
                        System.Windows.Controls.Canvas.SetLeft(dot, cx - dr);
                        System.Windows.Controls.Canvas.SetTop(dot, cy - dr);
                        OverlayCanvas.Children.Add(dot);
                    }
                    break;
                case "T-Bar":
                    {
                        AddLine(cx - L, cy, cx + L, cy, brush, T);
                        AddLine(cx, cy + G / 2, cx, cy + G / 2 + L, brush, T);
                    }
                    break;
                default: // Crosshair
                    {
                        AddLine(cx - G / 2 - L, cy, cx - G / 2, cy, brush, T);
                        AddLine(cx + G / 2, cy, cx + G / 2 + L, cy, brush, T);
                        AddLine(cx, cy - G / 2 - L, cx, cy - G / 2, brush, T);
                        AddLine(cx, cy + G / 2, cx, cy + G / 2 + L, brush, T);
                        // center dot
                        double r = Math.Max(1, Settings.DotSize);
                        var dotBrush = new System.Windows.Media.SolidColorBrush(color) { Opacity = Settings.DotOpacity };
                        var blackDot = new System.Windows.Shapes.Ellipse { Width = (r + 1) * 2, Height = (r + 1) * 2, Fill = System.Windows.Media.Brushes.Black };
                        System.Windows.Controls.Canvas.SetLeft(blackDot, cx - (r + 1));
                        System.Windows.Controls.Canvas.SetTop(blackDot, cy - (r + 1));
                        OverlayCanvas.Children.Add(blackDot);
                        var dot = new System.Windows.Shapes.Ellipse { Width = r * 2, Height = r * 2, Fill = dotBrush };
                        System.Windows.Controls.Canvas.SetLeft(dot, cx - r);
                        System.Windows.Controls.Canvas.SetTop(dot, cy - r);
                        OverlayCanvas.Children.Add(dot);
                    }
                    break;
            }
        }

        private void AddLine(double x1, double y1, double x2, double y2, System.Windows.Media.Brush brush, double thickness)
        {
            // black outline
            var outline = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = System.Windows.Media.Brushes.Black, StrokeThickness = thickness + 2,
                StrokeStartLineCap = PenLineCap.Flat,
                StrokeEndLineCap = PenLineCap.Flat
            };
            OverlayCanvas.Children.Add(outline);
            var line = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = brush, StrokeThickness = thickness,
                StrokeStartLineCap = PenLineCap.Flat,
                StrokeEndLineCap = PenLineCap.Flat
            };
            OverlayCanvas.Children.Add(line);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == 1) ToggleVisibility();
                else if (id == 2) OpenSettings();
                else if (id == 3) { Settings.OffsetX -= 1; ApplySettings(); SaveSettings(); }
                else if (id == 4) { Settings.OffsetY -= 1; ApplySettings(); SaveSettings(); }
                else if (id == 5) { Settings.OffsetX += 1; ApplySettings(); SaveSettings(); }
                else if (id == 6) { Settings.OffsetY += 1; ApplySettings(); SaveSettings(); }
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void LoadSettings()
        {
            try
            {
                var path = SettingsPath();
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var s = JsonSerializer.Deserialize<Settings>(json);
                    if (s != null) Settings = s;
                }
            }
            catch { }
        }

        public void SaveSettings()
        {
            try
            {
                var path = SettingsPath();
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        private static string SettingsPath()
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CrosshairOverlay", "settings.json");
        }

        protected override void OnClosed(EventArgs e)
        {
            SaveSettings();
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
            if (_hwnd != IntPtr.Zero)
            {
                UnregisterHotKey(_hwnd, 1);
                UnregisterHotKey(_hwnd, 2);
                UnregisterHotKey(_hwnd, 3);
                UnregisterHotKey(_hwnd, 4);
                UnregisterHotKey(_hwnd, 5);
                UnregisterHotKey(_hwnd, 6);
            }
            base.OnClosed(e);
        }
    }

    public class Settings
    {
        public string Shape { get; set; } = "Crosshair";
        public string Color { get; set; } = "#FFFFFF";
        public string OutlineColor { get; set; } = "#000000";
        public double Length { get; set; } = 18;
        public double Thickness { get; set; } = 3;
        public double Gap { get; set; } = 8;
        public double Opacity { get; set; } = 1.0;
        public bool Visible { get; set; } = true;
        public double OffsetX { get; set; } = 1;
        public double OffsetY { get; set; } = 0;
        public double DotSize { get; set; } = 4;
        public double DotOpacity { get; set; } = 1.0;
    }
}
