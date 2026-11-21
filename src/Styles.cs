using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace JavaViva
{
    /// <summary>Tema oscuro con acento verde, igual que GameBoost, para que
    /// las apps de Nous Logic se vean como familia.</summary>
    public static class Theme
    {
        public static readonly Color Bg = Color.FromArgb(15, 23, 42);
        public static readonly Color PanelBg = Color.FromArgb(30, 41, 59);
        public static readonly Color CardBg = Color.FromArgb(30, 41, 59);
        public static readonly Color CardBorder = Color.FromArgb(51, 65, 85);
        public static readonly Color Accent = Color.FromArgb(34, 197, 94);
        public static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
        public static readonly Color Warn = Color.FromArgb(245, 158, 11);
        public static readonly Color Danger = Color.FromArgb(239, 68, 68);
        public static readonly Color TextLight = Color.FromArgb(226, 232, 240);
        public static readonly Color TextMuted = Color.FromArgb(148, 163, 184);
        public static readonly Color InfoBg = Color.FromArgb(30, 58, 95);
        public static readonly Color InfoBorder = Color.FromArgb(59, 130, 246);

        public static void StyleTabs(TabControl tabs)
        {
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize = new Size(170, 38);
            tabs.SizeMode = TabSizeMode.Fixed;
            tabs.DrawItem += (sender, e) =>
            {
                var tc = (TabControl)sender!;
                var page = tc.TabPages[e.Index];
                bool selected = e.Index == tc.SelectedIndex;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = e.Bounds;
                rect.Inflate(-2, -2);
                using var bgBrush = new SolidBrush(selected ? Color.FromArgb(34, 197, 94) : Color.FromArgb(30, 41, 59));
                using var path = RoundedRect(rect, 8);
                g.FillPath(bgBrush, path);
                if (selected)
                {
                    using var pen = new Pen(Color.FromArgb(74, 222, 128), 1.5f);
                    g.DrawPath(pen, path);
                }
                var textColor = selected ? Color.White : Color.FromArgb(203, 213, 225);
                var font = new Font("Segoe UI", 9.5f, selected ? FontStyle.Bold : FontStyle.Regular);
                TextRenderer.DrawText(g, page.Text, font, rect, textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            };
        }

        public static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 12;
        public Color BorderColor { get; set; } = Theme.CardBorder;
        public int BorderWidth { get; set; } = 1;

        public RoundedPanel()
        {
            BackColor = Theme.CardBg;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = Theme.RoundedRect(rect, CornerRadius);
            using var brush = new SolidBrush(BackColor);
            g.FillPath(brush, path);
            if (BorderWidth > 0)
            {
                using var pen = new Pen(BorderColor, BorderWidth);
                g.DrawPath(pen, path);
            }
            base.OnPaint(e);
        }
    }

    public class InfoBox : RoundedPanel
    {
        public InfoBox(string text, Color? border = null)
        {
            BackColor = Theme.InfoBg;
            BorderColor = border ?? Theme.InfoBorder;
            CornerRadius = 10;
            var lbl = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(219, 234, 254),
                Font = new Font("Segoe UI", 9.5f),
                Padding = new Padding(12, 8, 12, 8),
            };
            Controls.Add(lbl);
        }
    }
}
