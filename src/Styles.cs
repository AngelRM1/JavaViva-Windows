using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JavaViva
{
    /// <summary>Tema oscuro con acento verde, igual que GameBoost, para que
    /// las apps de Nous Logic se vean como familia.
    /// v1.1.0: la ventana YA era oscura en sus propios controles, pero
    /// Windows dejaba claras la barra de titulo, las barras de scroll,
    /// los encabezados de la lista y los desplegables de los combos.
    /// Estos metodos completan el modo oscuro en esas piezas nativas.</summary>
    public static class Theme
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);

        /// <summary>Barra de titulo oscura (DWMWA_USE_IMMERSIVE_DARK_MODE).
        /// Sin esto la barra queda blanca/azul aunque todo lo demas sea oscuro.</summary>
        public static void ApplyDarkTitleBar(Form form)
        {
            try
            {
                int useDark = 1;
                DwmSetWindowAttribute(form.Handle, 20, ref useDark, sizeof(int));
            }
            catch { }
        }

        /// <summary>Barras de scroll oscuras en el control y en todos sus
        /// hijos (tema oscuro del Explorador, Windows 10 1809+). En los
        /// controles ocultos al arrancar hay que volver a aplicarlo al
        /// cambiar de pestana; MainForm lo llama en Load y al cambiar de tab.</summary>
        public static void ApplyDarkScrollbars(Control root)
        {
            try { SetWindowTheme(root.Handle, "DarkMode_Explorer", null); } catch { }
            foreach (Control c in root.Controls) ApplyDarkScrollbars(c);
        }

        /// <summary>ListView con encabezados y filas dibujados en oscuro.
        /// Sin OwnerDraw, el encabezado de columnas queda gris claro aunque
        /// el resto de la lista sea oscura.</summary>
        public static void StyleDarkListView(ListView lv)
        {
            // v1.1.3: el tema oscuro de los scrolls tambien aqui, no solo en
            // Load: la barra del listado se quedaba clara en algunos casos.
            ApplyDarkScrollbars(lv);
            lv.OwnerDraw = true;
            lv.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(CardBg), e.Bounds);
                using var pen = new Pen(CardBorder);
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
                TextRenderer.DrawText(e.Graphics, e.Header!.Text, new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height),
                    TextMuted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            };
            lv.DrawItem += (s, e) =>
            {
                bool sel = e.Item.Selected;
                var bg = sel ? Color.FromArgb(20, 83, 45)
                    : (e.ItemIndex % 2 == 0 ? CardBg : Color.FromArgb(36, 52, 71));
                e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);
                using var pen = new Pen(CardBorder);
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            };
            lv.DrawSubItem += (s, e) =>
            {
                bool sel = e.Item?.Selected == true;
                var fg = sel ? Color.White : (e.ColumnIndex == 0 ? TextLight : TextMuted);
                var font = e.ColumnIndex == 0
                    ? new Font("Segoe UI", 9.5f, FontStyle.Bold)
                    : new Font("Segoe UI", 9.5f);
                TextRenderer.DrawText(e.Graphics, e.SubItem?.Text ?? "", font,
                    new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 6, e.Bounds.Height),
                    fg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            };
        }

        /// <summary>Recorre todos los controles y quita los fondos claros
        /// que WinForms pone por defecto: las casillas y los circulos de
        /// CheckBox/RadioButton se dibujan con el tema claro del sistema
        /// aunque el formulario sea oscuro. Con FlatStyle.Flat y el fondo
        /// del padre, la casilla queda oscura y la marca en color claro.
        /// Llamar una vez en Load (los controles ya existen entonces).</summary>
        public static void ApplyDarkControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is CheckBox chk)
                {
                    chk.FlatStyle = FlatStyle.Flat;
                    chk.BackColor = root.BackColor;
                    chk.ForeColor = TextLight;
                    chk.FlatAppearance.BorderColor = CardBorder;
                    chk.FlatAppearance.CheckedBackColor = root.BackColor;
                    chk.FlatAppearance.MouseOverBackColor = root.BackColor;
                    chk.FlatAppearance.MouseDownBackColor = root.BackColor;
                }
                else if (c is RadioButton rb)
                {
                    rb.FlatStyle = FlatStyle.Flat;
                    rb.BackColor = root.BackColor;
                    rb.ForeColor = TextLight;
                    rb.FlatAppearance.BorderColor = CardBorder;
                    rb.FlatAppearance.CheckedBackColor = root.BackColor;
                    rb.FlatAppearance.MouseOverBackColor = root.BackColor;
                    rb.FlatAppearance.MouseDownBackColor = root.BackColor;
                }
                ApplyDarkControls(c);
            }
        }

        /// <summary>ComboBox con la lista desplegable en oscuro. Sin
        /// OwnerDraw, la lista se abre blanca aunque el combo sea oscuro.
        /// FlatStyle.Flat quita tambien el boton de la flecha claro.</summary>
        public static void StyleDarkComboBox(ComboBox cmb)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.DrawMode = DrawMode.OwnerDrawFixed;
            cmb.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                e.Graphics.FillRectangle(new SolidBrush(sel ? Color.FromArgb(20, 83, 45) : CardBg), e.Bounds);
                TextRenderer.DrawText(e.Graphics, cmb.Items[e.Index]?.ToString() ?? "", cmb.Font,
                    new Rectangle(e.Bounds.X + 4, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height),
                    sel ? Color.White : TextLight,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
            };
        }

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
            // v1.1.2: las pestañas se dibujan oscuras, pero la tira que queda
            // detras (a la derecha de la ultima pestana y la costura con el
            // contenido) la pinta Windows de blanco aunque el TabControl
            // tenga fondo oscuro. El evento Paint corre despues de que
            // Windows pinta, asi que se cubre con el color oscuro.
            tabs.Paint += (_, e) =>
            {
                if (tabs.TabCount == 0) return;
                var last = tabs.GetTabRect(tabs.TabCount - 1);
                using var bgBrush = new SolidBrush(Bg);
                if (last.Right < tabs.Width)
                    e.Graphics.FillRectangle(bgBrush, new Rectangle(last.Right, 0, tabs.Width - last.Right, last.Bottom + 2));
                e.Graphics.FillRectangle(bgBrush, new Rectangle(0, last.Bottom, tabs.Width, 2));
                using var pen = new Pen(Bg, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, tabs.Width - 1, tabs.Height - 1);
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

    /// <summary>Campo numerico con botones ▲▼ en oscuro. El NumericUpDown
    /// de Windows deja los botones de subir/bajar claros aunque el resto de
    /// la ventana sea oscura (son del sistema y no se pueden pintar). Este
    /// control los dibuja con el tema de JavaViva: caja de texto oscura +
    /// dos botones planos oscuros. API compatible con lo que usa MainForm:
    /// Minimum, Maximum, Value (int) y ValueChanged.</summary>
    public class DarkNumberBox : Panel
    {
        private readonly TextBox txt;
        private readonly Button btnUp, btnDown;
        private int val;

        public int Minimum { get; set; } = 0;
        public int Maximum { get; set; } = 100;

        public int Value
        {
            get => val;
            set
            {
                int nv = Math.Clamp(value, Minimum, Maximum);
                if (nv == val && txt.Text == nv.ToString()) return;
                val = nv;
                txt.Text = val.ToString();
            }
        }

        public event EventHandler? ValueChanged;

        public DarkNumberBox()
        {
            BackColor = Theme.CardBg;
            BorderStyle = BorderStyle.FixedSingle;
            Height = 26;
            txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Theme.CardBg,
                ForeColor = Theme.TextLight,
                Font = new Font("Segoe UI", 9.5f),
                Location = new Point(4, 4),
                Width = 40,
            };
            txt.Leave += (_, _) => CommitText();
            txt.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { CommitText(); e.SuppressKeyPress = true; } };
            btnUp = new Button { Text = "▲", Font = new Font("Segoe UI", 6.5f), FlatStyle = FlatStyle.Flat, BackColor = Theme.CardBg, ForeColor = Theme.TextMuted, Cursor = Cursors.Hand };
            btnUp.FlatAppearance.BorderSize = 0;
            btnUp.Click += (_, _) => { Value += 1; ValueChanged?.Invoke(this, EventArgs.Empty); };
            btnDown = new Button { Text = "▼", Font = new Font("Segoe UI", 6.5f), FlatStyle = FlatStyle.Flat, BackColor = Theme.CardBg, ForeColor = Theme.TextMuted, Cursor = Cursors.Hand };
            btnDown.FlatAppearance.BorderSize = 0;
            btnDown.Click += (_, _) => { Value -= 1; ValueChanged?.Invoke(this, EventArgs.Empty); };
            Controls.AddRange(new Control[] { txt, btnUp, btnDown });
            Resize += (_, _) => LayoutParts();
            LayoutParts();
        }

        private void LayoutParts()
        {
            int bw = 18;
            txt.Width = Math.Max(10, Width - bw - 8);
            btnUp.Location = new Point(Width - bw - 1, 0);
            btnUp.Size = new Size(bw, Height / 2);
            btnDown.Location = new Point(Width - bw - 1, Height / 2);
            btnDown.Size = new Size(bw, Height - Height / 2);
        }

        private void CommitText()
        {
            if (int.TryParse(txt.Text, out int v)) Value = v;
            else txt.Text = val.ToString();
            ValueChanged?.Invoke(this, EventArgs.Empty);
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
