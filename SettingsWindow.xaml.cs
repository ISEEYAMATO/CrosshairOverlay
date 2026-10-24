using System;
using System.Windows;
using System.Windows.Controls;

namespace CrosshairOverlay
{
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow _main;
        private Settings _settings;
        private bool _loading;

        public SettingsWindow(MainWindow main)
        {
            InitializeComponent();
            _main = main;
            _settings = CloneSettings(main.Settings);
            LoadValues();
            // live apply
            ShapeBox.SelectionChanged += (s, e) => { UpdateDotSettingsVisibility(); Apply(); };
            ColorBox.TextChanged += (s, e) => Apply();
            OutlineColorBox.TextChanged += (s, e) => Apply();
            LengthSlider.ValueChanged += (s, e) => Apply();
            ThicknessSlider.ValueChanged += (s, e) => Apply();
            GapSlider.ValueChanged += (s, e) => Apply();
            OpacitySlider.ValueChanged += (s, e) => Apply();
            DotSizeSlider.ValueChanged += (s, e) => Apply();
            DotOpacitySlider.ValueChanged += (s, e) => Apply();
            OffsetXSlider.ValueChanged += (s, e) => { if (!_loading) OffsetXBox.Text = OffsetXSlider.Value.ToString("0.0"); Apply(); };
            OffsetYSlider.ValueChanged += (s, e) => { if (!_loading) OffsetYBox.Text = OffsetYSlider.Value.ToString("0.0"); Apply(); };
            OffsetXBox.TextChanged += (s, e) => { if (!_loading && double.TryParse(OffsetXBox.Text, out var v)) { OffsetXSlider.Value = v; } };
            OffsetYBox.TextChanged += (s, e) => { if (!_loading && double.TryParse(OffsetYBox.Text, out var v)) { OffsetYSlider.Value = v; } };
            VisibleCheck.Checked += (s, e) => Apply();
            VisibleCheck.Unchecked += (s, e) => Apply();
        }

        private void UpdateDotSettingsVisibility()
        {
            string shape = SelectedShape();
            DotSettingsPanel.Visibility = shape == "Crosshair" ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LoadValues()
        {
            _loading = true;
            ShapeBox.SelectedIndex = _settings.Shape switch
            {
                "Crosshair" => 1,
                "Dot" => 2,
                "Circle" => 3,
                "T-Bar" => 4,
                _ => 0
            };
            ColorBox.Text = _settings.Color;
            OutlineColorBox.Text = _settings.OutlineColor;
            LengthSlider.Value = _settings.Length;
            ThicknessSlider.Value = _settings.Thickness;
            GapSlider.Value = _settings.Gap;
            OpacitySlider.Value = _settings.Opacity;
            DotSizeSlider.Value = _settings.DotSize;
            DotOpacitySlider.Value = _settings.DotOpacity;
            OffsetXSlider.Value = _settings.OffsetX;
            OffsetYSlider.Value = _settings.OffsetY;
            OffsetXBox.Text = _settings.OffsetX.ToString("0.0");
            OffsetYBox.Text = _settings.OffsetY.ToString("0.0");
            VisibleCheck.IsChecked = _settings.Visible;
            _loading = false;
            UpdateDotSettingsVisibility();
            DrawPreview();
        }

        private string SelectedShape()
        {
            return ShapeBox.SelectedIndex switch
            {
                1 => "Crosshair",
                2 => "Dot",
                3 => "Circle",
                4 => "T-Bar",
                _ => "Default"
            };
        }

        private void Apply()
        {
            if (_loading) return;
            _settings.Shape = SelectedShape();
            _settings.Color = ColorBox.Text;
            _settings.OutlineColor = OutlineColorBox.Text;
            _settings.Length = LengthSlider.Value;
            _settings.Thickness = ThicknessSlider.Value;
            _settings.Gap = GapSlider.Value;
            _settings.Opacity = OpacitySlider.Value;
            _settings.DotSize = DotSizeSlider.Value;
            _settings.DotOpacity = DotOpacitySlider.Value;
            if (double.TryParse(OffsetXBox.Text, out var ox)) _settings.OffsetX = ox;
            else _settings.OffsetX = OffsetXSlider.Value;
            if (double.TryParse(OffsetYBox.Text, out var oy)) _settings.OffsetY = oy;
            else _settings.OffsetY = OffsetYSlider.Value;
            _settings.Visible = VisibleCheck.IsChecked == true;
            DrawPreview();
        }

        private void PresetColor(string c) { ColorBox.Text = c; }
        private void PresetGreen_Click(object sender, RoutedEventArgs e) => PresetColor("#00FF00");
        private void PresetCyan_Click(object sender, RoutedEventArgs e) => PresetColor("#00FFFF");
        private void PresetRed_Click(object sender, RoutedEventArgs e) => PresetColor("#FF0000");
        private void PresetYellow_Click(object sender, RoutedEventArgs e) => PresetColor("#FFFF00");
        private void PresetWhite_Click(object sender, RoutedEventArgs e) => PresetColor("#FFFFFF");

        private void Nudge(int dx, int dy)
        {
            _settings.OffsetX += dx;
            _settings.OffsetY += dy;
            LoadValues();
            DrawPreview();
        }
        private void NudgeLeft_Click(object sender, RoutedEventArgs e) => Nudge(-1, 0);
        private void NudgeUp_Click(object sender, RoutedEventArgs e) => Nudge(0, -1);
        private void NudgeRight_Click(object sender, RoutedEventArgs e) => Nudge(1, 0);
        private void NudgeDown_Click(object sender, RoutedEventArgs e) => Nudge(0, 1);
        private void ResetOffset_Click(object sender, RoutedEventArgs e)
        {
            _settings.OffsetX = 0;
            _settings.OffsetY = 0;
            LoadValues();
            _main.ApplySettings();
            _main.SaveSettings();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Apply();
            _main.Settings = _settings;
            _main.ApplySettings();
            _main.SaveSettings();
            Close();
        }

        private void DrawPreview()
        {
            PreviewCanvas.Children.Clear();
            var s = _settings;
            double cx = PreviewCanvas.Width / 2 + s.OffsetX * 0.5; // scale offset lightly
            double cy = PreviewCanvas.Height / 2 + s.OffsetY * 0.5;
            // not too large; keep
            cx = PreviewCanvas.Width / 2;
            cy = PreviewCanvas.Height / 2;

            System.Windows.Media.Color color, outlineColor;
            try { color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.Color); }
            catch { color = System.Windows.Media.Colors.White; }
            try { outlineColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.OutlineColor); }
            catch { outlineColor = System.Windows.Media.Colors.Black; }
            var brush = new System.Windows.Media.SolidColorBrush(color);
            var outlineBrush = new System.Windows.Media.SolidColorBrush(outlineColor);

            double L = s.Length;
            double T = s.Thickness;
            double G = s.Gap;

            // scale down if needed
            double scale = 1.0;
            if (L * 2 + G > 70) scale = 70.0 / (L * 2 + G);
            L *= scale; G *= scale; T = Math.Max(1, T * scale);

            switch (s.Shape)
            {
                case "Dot":
                    {
                        double r = Math.Max(1, L / 2);
                        var blackDot = new System.Windows.Shapes.Ellipse { Width = (r + 1.5) * 2, Height = (r + 1.5) * 2, Fill = System.Windows.Media.Brushes.Black };
                        System.Windows.Controls.Canvas.SetLeft(blackDot, cx - (r + 1.5));
                        System.Windows.Controls.Canvas.SetTop(blackDot, cy - (r + 1.5));
                        PreviewCanvas.Children.Add(blackDot);
                        var el = new System.Windows.Shapes.Ellipse { Width = r * 2, Height = r * 2, Fill = brush };
                        System.Windows.Controls.Canvas.SetLeft(el, cx - r);
                        System.Windows.Controls.Canvas.SetTop(el, cy - r);
                        PreviewCanvas.Children.Add(el);
                    }
                    break;
                case "Circle":
                    {
                        double r = Math.Max(4, L);
                        var el = new System.Windows.Shapes.Ellipse { Width = r * 2, Height = r * 2, Stroke = brush, StrokeThickness = T, Fill = System.Windows.Media.Brushes.Transparent };
                        System.Windows.Controls.Canvas.SetLeft(el, cx - r);
                        System.Windows.Controls.Canvas.SetTop(el, cy - r);
                        PreviewCanvas.Children.Add(el);
                    }
                    break;
                case "T-Bar":
                    {
                        var h = new System.Windows.Shapes.Line { X1 = cx - L, Y1 = cy, X2 = cx + L, Y2 = cy, Stroke = brush, StrokeThickness = T };
                        var v = new System.Windows.Shapes.Line { X1 = cx, Y1 = cy + G / 2, X2 = cx, Y2 = cy + G / 2 + L, Stroke = brush, StrokeThickness = T };
                        PreviewCanvas.Children.Add(h);
                        PreviewCanvas.Children.Add(v);
                    }
                    break;
                default: // Crosshair
                    {
                        AddPreviewLine(cx - G / 2 - L, cy, cx - G / 2, cy, brush, T, outlineBrush);
                        AddPreviewLine(cx + G / 2, cy, cx + G / 2 + L, cy, brush, T, outlineBrush);
                        AddPreviewLine(cx, cy - G / 2 - L, cx, cy - G / 2, brush, T, outlineBrush);
                        AddPreviewLine(cx, cy + G / 2, cx, cy + G / 2 + L, brush, T, outlineBrush);
                        double r = Math.Max(1, s.DotSize * scale);
                        var dotBrush = new System.Windows.Media.SolidColorBrush(color) { Opacity = s.DotOpacity };
                        var blackDot = new System.Windows.Shapes.Ellipse { Width = (r + 1) * 2, Height = (r + 1) * 2, Fill = System.Windows.Media.Brushes.Black };
                        System.Windows.Controls.Canvas.SetLeft(blackDot, cx - (r + 1));
                        System.Windows.Controls.Canvas.SetTop(blackDot, cy - (r + 1));
                        PreviewCanvas.Children.Add(blackDot);
                        var dot = new System.Windows.Shapes.Ellipse { Width = r * 2, Height = r * 2, Fill = dotBrush };
                        System.Windows.Controls.Canvas.SetLeft(dot, cx - r);
                        System.Windows.Controls.Canvas.SetTop(dot, cy - r);
                        PreviewCanvas.Children.Add(dot);
                    }
                    break;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private static Settings CloneSettings(Settings src)
        {
            return new Settings
            {
                Shape = src.Shape,
                Color = src.Color,
                OutlineColor = src.OutlineColor,
                Length = src.Length,
                Thickness = src.Thickness,
                Gap = src.Gap,
                Opacity = src.Opacity,
                Visible = src.Visible,
                OffsetX = src.OffsetX,
                OffsetY = src.OffsetY,
                DotSize = src.DotSize,
                DotOpacity = src.DotOpacity
            };
        }

        private void AddPreviewLine(double x1, double y1, double x2, double y2, System.Windows.Media.Brush brush, double thickness, System.Windows.Media.Brush outlineBrush)
        {
            var ol = new System.Windows.Shapes.Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = outlineBrush, StrokeThickness = thickness + 2, StrokeStartLineCap = System.Windows.Media.PenLineCap.Flat, StrokeEndLineCap = System.Windows.Media.PenLineCap.Flat };
            var ln = new System.Windows.Shapes.Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = brush, StrokeThickness = thickness, StrokeStartLineCap = System.Windows.Media.PenLineCap.Round, StrokeEndLineCap = System.Windows.Media.PenLineCap.Round };
            PreviewCanvas.Children.Add(ol);
            PreviewCanvas.Children.Add(ln);
        }
    }
}
