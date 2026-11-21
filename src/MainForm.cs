using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace JavaViva
{
    public partial class MainForm : Form
    {
        private static readonly Color Bg = Theme.Bg;
        private static readonly Color PanelBg = Theme.PanelBg;
        private static readonly Color CardBg = Theme.CardBg;
        private static readonly Color Accent = Theme.Accent;
        private static readonly Color TextLight = Theme.TextLight;
        private static readonly Color TextMuted = Theme.TextMuted;
        private static readonly Color Warn = Theme.Warn;
        private static readonly Color Danger = Theme.Danger;

        private readonly string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        private List<GameEntry> games = new();
        private GameEntry? selected;
        private Process? gameProcess;
        private string? javaExe;
        private string javaHow = "";
        private string customJava = "";

        // Biblioteca
        private ListView lvGames = null!;
        private Label lblDetail = null!;
        private Label lblStatus = null!;
        private Label lblJava = null!;
        private Button btnPlay = null!;
        private Button btnStop = null!;
        private TextBox logBox = null!;

        // Configuracion
        private RadioButton rbAuto = null!, rbPreset = null!, rbCustom = null!;
        private ComboBox cmbPreset = null!;
        private NumericUpDown numW = null!, numH = null!;
        private ComboBox cmbScale = null!, cmbPhone = null!, cmbRotate = null!, cmbFps = null!, cmbBacklight = null!, cmbM3gAa = null!;
        private CheckBox chkSound = null!, chkRepaint = null!, chkImmediate = null!, chkOverride = null!, chkM3gHalf = null!;
        private TextBox txtJava = null!;
        private Label lblConfigFor = null!;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public MainForm()
        {
            Text = "JavaViva v1.0.2  -  Juegos Java .jar como en tu celular de los 2000  |  Nous Logic";
            Size = new Size(1020, 860);
            MinimumSize = new Size(900, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg; ForeColor = TextLight;
            Font = new Font("Segoe UI", 9.5f);
            AutoScroll = true;

            BuildHeader();
            BuildTabs();
            BuildBottom();

            Load += (_, _) =>
            {
                LoadSettings();
                CheckJava();
                games = Library.Load();
                ScanGamesFolder();
                RefreshList();
                Log("JavaViva v1.0.2 listo. Motor: " + EmulatorConfig.EngineVersion + " (GPLv3).");
                Log("Java: " + (javaExe ?? "NO ENCONTRADO") + " — " + javaHow);
                if (javaExe == null)
                    Log("Sin Java no se puede jugar. El paquete incluye un runtime en runtime\\windows-x64; si falta, instala Adoptium Temurin JRE desde Configuracion.");
                if (games.Count == 0)
                    Log("No hay juegos todavia. Pulsa 'Agregar .jar' o copia tus .jar en la carpeta Juegos y pulsa Refrescar.");
            };
            FormClosing += (_, _) => { try { if (gameProcess is { HasExited: false }) gameProcess.Kill(); } catch { } };
        }

        // ---------------- Header ----------------
        private void BuildHeader()
        {
            var header = new Panel { Location = new Point(0, 0), Size = new Size(ClientSize.Width, 82), BackColor = PanelBg, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            var logoBox = new PictureBox { Location = new Point(14, 11), Size = new Size(60, 60), SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent };
            try
            {
                var asm = typeof(MainForm).Assembly;
                using var s = asm.GetManifestResourceStream("JavaViva.logo.png");
                if (s != null) logoBox.Image = Image.FromStream(s);
            }
            catch { }
            var title = new Label { Text = "JavaViva", Font = new Font("Segoe UI", 22, FontStyle.Bold), ForeColor = Color.White, Location = new Point(84, 8), AutoSize = true };
            var sub = new Label { Text = "Tus juegos .jar de Java, como en tu celular de los 2000  •  Biblioteca, resolucion y perfiles por juego", ForeColor = TextMuted, Location = new Point(86, 48), AutoSize = true };
            lblJava = new Label { Text = "Buscando Java...", ForeColor = TextMuted, Location = new Point(640, 14), AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            var ver = new Label { Text = "v1.0.2  •  Nous Logic  •  FreeJ2ME-Plus v1.53", ForeColor = TextMuted, Location = new Point(640, 38), AutoSize = true };
            header.Controls.AddRange(new Control[] { logoBox, title, sub, lblJava, ver });
            var line = new Panel { Location = new Point(0, 79), Size = new Size(ClientSize.Width, 3), BackColor = Accent, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            header.Controls.Add(line);
            Controls.Add(header);

            var tip = new Label
            {
                Location = new Point(0, 82), Size = new Size(ClientSize.Width, 30), BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(253, 224, 71), Text = "   💡  Si un juego se ve cortado, chico o en blanco: cambia la resolucion en Configuracion y vuelve a jugar. NFS Most Wanted ya trae su arreglo activado.",
                TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(tip);
        }

        // ---------------- Tabs ----------------
        private TabControl tabs = null!;
        private void BuildTabs()
        {
            tabs = new TabControl { Location = new Point(0, 112), Size = new Size(ClientSize.Width, ClientSize.Height - 112 - 150), BackColor = Bg, ForeColor = TextLight, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            Theme.StyleTabs(tabs);
            var tabLib = new TabPage("📚  Biblioteca") { BackColor = Bg };
            var tabCfg = new TabPage("⚙️  Configuración") { BackColor = Bg };
            var tabKeys = new TabPage("📱  Teclas") { BackColor = Bg };
            var tabAbout = new TabPage("ℹ️  Acerca de") { BackColor = Bg };
            tabs.TabPages.AddRange(new[] { tabLib, tabCfg, tabKeys, tabAbout });
            BuildLibraryTab(tabLib);
            BuildConfigTab(tabCfg);
            BuildKeysTab(tabKeys);
            BuildAboutTab(tabAbout);
            Controls.Add(tabs);
        }

        private void BuildBottom()
        {
            var bottom = new Panel { Location = new Point(0, ClientSize.Height - 150), Size = new Size(ClientSize.Width, 150), BackColor = PanelBg, Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            lblStatus = new Label { Text = "Listo.", Location = new Point(10, 4), Size = new Size(ClientSize.Width - 20, 22), ForeColor = TextMuted };
            logBox = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Location = new Point(8, 26), Size = new Size(ClientSize.Width - 16, 116), BackColor = Color.FromArgb(15, 20, 32), ForeColor = Color.FromArgb(203, 213, 225), Font = new Font("Consolas", 9), BorderStyle = BorderStyle.None, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            bottom.Controls.AddRange(new Control[] { lblStatus, logBox });
            Controls.Add(bottom);
        }

        // ---------------- Biblioteca ----------------
        private void BuildLibraryTab(TabPage tab)
        {
            tab.AutoScroll = true;
            lvGames = new ListView
            {
                Location = new Point(14, 14), Size = new Size(700, 380), View = View.Details,
                FullRowSelect = true, GridLines = true, BackColor = CardBg, ForeColor = TextLight,
                Font = new Font("Segoe UI", 9.5f), Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            lvGames.Columns.Add("Juego", 240);
            lvGames.Columns.Add("Fabricante", 130);
            lvGames.Columns.Add("Tamaño", 80);
            lvGames.Columns.Add("Resolución auto", 130);
            lvGames.Columns.Add("Última vez", 110);
            lvGames.SelectedIndexChanged += (_, _) => { SelectFromList(); };
            lvGames.DoubleClick += (_, _) => PlaySelected();
            tab.Controls.Add(lvGames);

            int x = 730, y = 14;
            btnPlay = Btn("▶  Jugar", x, y, 240, 42, Accent); y += 52;
            btnPlay.Click += (_, _) => PlaySelected();
            btnStop = Btn("⏹  Detener juego", x, y, 240, 36, Color.FromArgb(100, 116, 139)); y += 46;
            btnStop.Enabled = false;
            btnStop.Click += (_, _) => StopGame();
            var bAdd = Btn("📁  Agregar .jar / .jad...", x, y, 240, 36, null); y += 46;
            bAdd.Click += (_, _) => AddGameDialog();
            var bScan = Btn("🔄  Refrescar carpeta Juegos", x, y, 240, 36, null); y += 46;
            bScan.Click += (_, _) => { ScanGamesFolder(); RefreshList(); Log("Carpeta Juegos actualizada: " + games.Count + " juego(s)."); };
            var bOpen = Btn("📂  Abrir carpeta Juegos", x, y, 240, 36, null); y += 46;
            bOpen.Click += (_, _) => { var d = Library.GamesDir(baseDir); Directory.CreateDirectory(d); Process.Start(new ProcessStartInfo { FileName = d, UseShellExecute = true }); };
            var bDel = Btn("🗑  Quitar de la lista", x, y, 240, 36, Color.FromArgb(239, 68, 68)); y += 46;
            bDel.Click += (_, _) => RemoveSelected();

            lblDetail = new Label { Location = new Point(14, 406), Size = new Size(960, 90), ForeColor = TextLight, Font = new Font("Segoe UI", 9.5f) };
            tab.Controls.Add(lblDetail);

            var quick = new RoundedPanel { Location = new Point(14, 500), Size = new Size(960, 78), BackColor = CardBg };
            var qTitle = new Label { Text = "Probar rápido con otra resolución (sin guardar): si se ve cortado o en blanco, prueba otra.", Location = new Point(14, 8), Size = new Size(920, 20), ForeColor = TextLight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            quick.Controls.Add(qTitle);
            var q1 = Btn("240x320", 14, 34, 130, 32, null); q1.Click += (_, _) => PlayWithResolution(240, 320);
            var q2 = Btn("176x208", 154, 34, 130, 32, null); q2.Click += (_, _) => PlayWithResolution(176, 208);
            var q3 = Btn("320x240 ◐", 294, 34, 130, 32, null); q3.Click += (_, _) => PlayWithResolution(320, 240);
            var q4 = Btn("128x160", 434, 34, 130, 32, null); q4.Click += (_, _) => PlayWithResolution(128, 160);
            var q5 = Btn("352x416", 574, 34, 130, 32, null); q5.Click += (_, _) => PlayWithResolution(352, 416);
            quick.Controls.AddRange(new Control[] { q1, q2, q3, q4, q5 });
            tab.Controls.Add(quick);
        }

        // ---------------- Configuracion ----------------
        private void BuildConfigTab(TabPage tab)
        {
            tab.AutoScroll = true;
            lblConfigFor = new Label { Text = "Configuración para: (elige un juego en Biblioteca)", Location = new Point(14, 12), Size = new Size(940, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            tab.Controls.Add(lblConfigFor);

            var card = new RoundedPanel { Location = new Point(14, 40), Size = new Size(960, 210), BackColor = CardBg };
            var t1 = new Label { Text = "📐  Pantalla del teléfono (resolución)", Location = new Point(14, 10), Size = new Size(400, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            card.Controls.Add(t1);
            rbAuto = new RadioButton { Text = "Automática (recomendado): JavaViva lee el .jar y pone la resolución para la que se hizo el juego", Location = new Point(16, 38), Size = new Size(920, 24), ForeColor = TextLight, Checked = true };
            rbPreset = new RadioButton { Text = "Elegir de la lista:", Location = new Point(16, 66), Size = new Size(160, 24), ForeColor = TextLight };
            cmbPreset = new ComboBox { Location = new Point(180, 66), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbPreset.Items.AddRange(EmulatorConfig.Resolutions);
            cmbPreset.SelectedItem = "240x320";
            rbCustom = new RadioButton { Text = "Personalizada:", Location = new Point(340, 66), Size = new Size(130, 24), ForeColor = TextLight };
            numW = new NumericUpDown { Location = new Point(480, 66), Width = 70, Minimum = 96, Maximum = 854, Value = 240, BackColor = CardBg, ForeColor = TextLight };
            var xl = new Label { Text = "x", Location = new Point(554, 68), Size = new Size(16, 22), ForeColor = TextLight };
            numH = new NumericUpDown { Location = new Point(574, 66), Width = 70, Minimum = 65, Maximum = 854, Value = 320, BackColor = CardBg, ForeColor = TextLight };
            var autoNote = new Label { Text = "Cómo detecta la automática: busca ifone.png / glu.png en el .jar (su tamaño ES la resolución del juego) y si no, usa la imagen más grande del .jar. NFS Most Wanted: 240x320.", Location = new Point(16, 96), Size = new Size(920, 36), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            var sLbl = new Label { Text = "Escala de la ventana:", Location = new Point(16, 138), Size = new Size(150, 22), ForeColor = TextLight };
            cmbScale = new ComboBox { Location = new Point(170, 136), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbScale.Items.AddRange(new object[] { "1x", "2x", "3x" }); cmbScale.SelectedItem = "2x";
            var rotLbl = new Label { Text = "Girar pantalla:", Location = new Point(280, 138), Size = new Size(110, 22), ForeColor = TextLight };
            cmbRotate = new ComboBox { Location = new Point(392, 136), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbRotate.Items.AddRange(new object[] { "0°", "90°", "180°", "270°" }); cmbRotate.SelectedItem = "0°";
            var fpsLbl = new Label { Text = "Límite FPS:", Location = new Point(510, 138), Size = new Size(90, 22), ForeColor = TextLight };
            cmbFps = new ComboBox { Location = new Point(600, 136), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbFps.Items.AddRange(new object[] { "Auto (60)", "60", "50", "30" }); cmbFps.SelectedItem = "Auto (60)";
            var sndLbl = new Label { Text = "Sonido:", Location = new Point(720, 138), Size = new Size(60, 22), ForeColor = TextLight };
            chkSound = new CheckBox { Text = "Activado", Location = new Point(784, 138), Size = new Size(110, 22), ForeColor = TextLight, Checked = true };
            var blLbl = new Label { Text = "Luz de fondo:", Location = new Point(16, 170), Size = new Size(110, 22), ForeColor = TextLight };
            cmbBacklight = new ComboBox { Location = new Point(128, 168), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbBacklight.Items.AddRange(EmulatorConfig.Backlights); cmbBacklight.SelectedItem = "Disabled";
            var blNote = new Label { Text = "Verde = Nokia clásico. Solo cambia el tinte de la pantalla.", Location = new Point(260, 170), Size = new Size(500, 22), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            card.Controls.AddRange(new Control[] { rbAuto, rbPreset, cmbPreset, rbCustom, numW, xl, numH, autoNote, sLbl, cmbScale, rotLbl, cmbRotate, fpsLbl, cmbFps, sndLbl, chkSound, blLbl, cmbBacklight, blNote });
            tab.Controls.Add(card);

            var card2 = new RoundedPanel { Location = new Point(14, 262), Size = new Size(960, 150), BackColor = CardBg };
            var t2 = new Label { Text = "📱  Interfaz / perfil de teléfono (teclas y controles)", Location = new Point(14, 10), Size = new Size(500, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            card2.Controls.Add(t2);
            var pLbl = new Label { Text = "Perfil:", Location = new Point(16, 42), Size = new Size(60, 22), ForeColor = TextLight };
            cmbPhone = new ComboBox { Location = new Point(76, 40), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            foreach (var p in EmulatorConfig.Phones) cmbPhone.Items.Add(p.Display);
            cmbPhone.SelectedItem = "Standard";
            var pHint = new Label { Text = "Standard vale para casi todos. Cambia a Nokia / Siemens / Motorola solo si el juego fue hecho para esa marca y las teclas no responden.", Location = new Point(310, 42), Size = new Size(630, 36), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            var m3gLbl = new Label { Text = "Gráficos 3D (M3G):", Location = new Point(16, 86), Size = new Size(130, 22), ForeColor = TextLight };
            chkM3gHalf = new CheckBox { Text = "Media resolución 3D (si va lento)", Location = new Point(150, 86), Size = new Size(240, 22), ForeColor = TextLight };
            var aaLbl = new Label { Text = "Antialiasing:", Location = new Point(410, 86), Size = new Size(90, 22), ForeColor = TextLight };
            cmbM3gAa = new ComboBox { Location = new Point(502, 84), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbM3gAa.Items.AddRange(new object[] { "app", "on", "off" }); cmbM3gAa.SelectedItem = "app";
            var phNote = new Label { Text = "NFS Most Wanted usa 3D (M3G). Si se ve lento en tu PC, activa la media resolución 3D.", Location = new Point(16, 114), Size = new Size(920, 22), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            card2.Controls.AddRange(new Control[] { pLbl, cmbPhone, pHint, m3gLbl, chkM3gHalf, aaLbl, cmbM3gAa, phNote });
            tab.Controls.Add(card2);

            var card3 = new RoundedPanel { Location = new Point(14, 424), Size = new Size(960, 130), BackColor = CardBg };
            var t3 = new Label { Text = "🛠️  Compatibilidad (cuando un juego no arranca o se queda en blanco)", Location = new Point(14, 10), Size = new Size(700, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            card3.Controls.Add(t3);
            chkRepaint = new CheckBox { Text = "Repaint on setCurrent (NFS Most Wanted lo necesita, si no se queda en pantalla blanca)", Location = new Point(16, 40), Size = new Size(920, 22), ForeColor = TextLight };
            chkImmediate = new CheckBox { Text = "Immediate repaints (pruébalo si la pantalla parpadea o no se redibuja)", Location = new Point(16, 66), Size = new Size(920, 22), ForeColor = TextLight };
            chkOverride = new CheckBox { Text = "Override platform checks (activado por defecto, deja que el juego corra aunque pida otro teléfono)", Location = new Point(16, 92), Size = new Size(920, 22), ForeColor = TextLight, Checked = true };
            card3.Controls.AddRange(new Control[] { chkRepaint, chkImmediate, chkOverride });
            tab.Controls.Add(card3);

            var card4 = new RoundedPanel { Location = new Point(14, 566), Size = new Size(960, 96), BackColor = CardBg };
            var t4 = new Label { Text = "☕  Java (el motor que ejecuta el .jar)", Location = new Point(14, 10), Size = new Size(400, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            card4.Controls.Add(t4);
            txtJava = new TextBox { Location = new Point(16, 40), Width = 700, BackColor = CardBg, ForeColor = TextLight, ReadOnly = true };
            var bJava = Btn("Examinar java.exe...", 730, 38, 200, 30, null);
            bJava.Click += (_, _) =>
            {
                using var d = new OpenFileDialog { Filter = "java.exe|java.exe|Todos|*.*", Title = "Elige java.exe" };
                if (d.ShowDialog() == DialogResult.OK) { customJava = d.FileName; txtJava.Text = customJava; SaveSettings(); CheckJava(); Log("Java personalizado: " + customJava); }
            };
            var jNote = new Label { Text = "Vacío = automático: primero el runtime incluido con JavaViva, si no el Java de tu PC. Si no tienes Java, descarga Adoptium Temurin JRE (gratis).", Location = new Point(16, 72), Size = new Size(920, 20), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            card4.Controls.AddRange(new Control[] { txtJava, bJava, jNote });
            tab.Controls.Add(card4);

            int by = 674;
            var bSave = Btn("💾  Guardar para este juego", 14, by, 260, 40, Accent);
            bSave.Click += (_, _) => { if (SaveConfigFromUI()) { Library.Save(games); RefreshList(); Log("Configuración guardada para " + (selected?.DisplayName ?? "")); Status("Configuración guardada."); } };
            var bTry = Btn("▶  Probar con esta configuración", 288, by, 280, 40, null);
            bTry.Click += (_, _) => { if (SaveConfigFromUI()) { Library.Save(games); PlaySelected(); } };
            var bReset = Btn("↺  Restablecer", 582, by, 160, 40, Color.FromArgb(100, 116, 139));
            bReset.Click += (_, _) => { if (selected != null) { var fresh = JarInspector.Inspect(selected.FilePath); EmulatorConfig.ApplyKnownFixes(fresh); selected.ResolutionMode = "Auto"; selected.Preset = "240x320"; selected.Phone = "Standard"; selected.Scale = 2; selected.Rotate = 0; selected.Fps = 60; selected.Sound = true; selected.Backlight = "Disabled"; selected.CompatRepaintOnSetCurrent = fresh.CompatRepaintOnSetCurrent; selected.CompatImmediateRepaints = false; selected.M3gHalfRes = false; selected.M3gAntialias = "app"; LoadGameIntoConfig(); Library.Save(games); Log("Configuración restablecida para " + selected.DisplayName); } };
            tab.Controls.AddRange(new Control[] { bSave, bTry, bReset });
        }

        // ---------------- Teclas ----------------
        private void BuildKeysTab(TabPage tab)
        {
            tab.AutoScroll = true;
            var info = new InfoBox("El juego se controla con el teclado de tu PC cuando su ventana esta al frente. Pulsa un boton de este teclado y JavaViva lleva la ventana del juego al frente y pulsa esa tecla por ti. Tambien puedes pulsar directamente las teclas. Los digitos 0-9 van por el teclado numerico de tu PC (o los botones de abajo): la fila superior de digitos no la acepta el motor.");
            info.Location = new Point(14, 12); info.Size = new Size(960, 56);
            tab.Controls.Add(info);

            var rows = new (string pc, string phone, string send)[]
            {
                ("Q", "Tecla suave izquierda", "q"),
                ("W", "Tecla suave derecha", "w"),
                ("↑ ↓ ← →", "Moverte / acelerar / frenar", "{UP}"),
                ("Enter  /  5", "Disparar / Aceptar / Acelerar (en NFS)", "{ENTER}"),
                // v1.0.2: el motor solo acepta los digitos del teclado numerico
                // (VK_NUMPAD0-9); las teclas 1-9 de la fila superior no llegan
                // al juego. Por eso los botones de abajo envian {NUMPADn}.
                ("Teclado numerico 1-9, 0", "Teclas del telefono (usa el teclado numerico de tu PC o los botones de abajo; la fila superior de digitos no la acepta el motor)", "5"),
                ("E", "Tecla *", "e"),
                ("R", "Tecla #", "r"),
                ("A", "Borrar (CLR)", "a"),
                ("Esc", "Menu del emulador (resolucion, sonido, salir)", "{ESC}"),
                ("Ctrl + C", "Sin funcion directa en el juego (el motor no responde a Ctrl+C con una pulsacion simple)", "^c"),
                ("+  /  -", "Hacer la ventana mas grande / mas chica", "{ADD}"),
                // v1.0.2: Espacio y X no hacen nada en el juego con una
                // pulsacion simple (el motor no las usa para avance ni pausa);
                // antes la tabla prometia que si.
                ("Espacio", "Sin funcion en el juego (el motor no la usa para avance rapido)", " "),
                ("X", "Sin funcion en el juego (el motor no la usa para pausa)", "x"),
            };
            int y = 82;
            foreach (var (pc, phone, _) in rows)
            {
                var a = new Label { Text = pc, Location = new Point(16, y), Size = new Size(130, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
                var b = new Label { Text = phone, Location = new Point(150, y), Size = new Size(600, 22), ForeColor = TextMuted };
                tab.Controls.AddRange(new Control[] { a, b });
                y += 26;
            }

            var padTitle = new Label { Text = "Teclado del teléfono (pulsa para enviar la tecla al juego):", Location = new Point(14, y + 8), Size = new Size(500, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            tab.Controls.Add(padTitle);
            y += 36;
            var pad = new RoundedPanel { Location = new Point(14, y), Size = new Size(420, 300), BackColor = CardBg };
            string[,] keys = { { "1", "2", "3" }, { "4", "5", "6" }, { "7", "8", "9" }, { "*", "0", "#" } };
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 3; c++)
                {
                    var k = keys[r, c];
                    var b = Btn(k, 20 + c * 130, 16 + r * 56, 120, 46, null);
                    // v1.0.2: los digitos van como {NUMPADn}. Antes se enviaba
                    // "5" (fila superior) y el motor lo ignoraba: acelerar con
                    // el boton 5 en NFS no funcionaba. "*" y "#" van como E/R.
                    string send = k == "*" ? "e" : k == "#" ? "r" : "{NUMPAD" + k + "}";
                    b.Click += (_, _) => SendKeyToGame(send);
                    pad.Controls.Add(b);
                }
            var bUp = Btn("↑", 150, 244, 120, 40, null); bUp.Click += (_, _) => SendKeyToGame("{UP}");
            pad.Controls.Add(bUp);
            tab.Controls.Add(pad);

            var nav = new RoundedPanel { Location = new Point(450, y), Size = new Size(520, 300), BackColor = CardBg };
            var nTitle = new Label { Text = "Dirección y acciones", Location = new Point(14, 12), Size = new Size(400, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            nav.Controls.Add(nTitle);
            var navBtns = new (string t, string s, int x, int yy)[]
            {
                ("◀", "{LEFT}", 20, 50), ("▲", "{UP}", 150, 50), ("▶", "{RIGHT}", 280, 50),
                ("▼", "{DOWN}", 150, 110),
                ("Suave ◀ (Q)", "q", 20, 180), ("Fuego (Enter)", "{ENTER}", 150, 180), ("Suave ▶ (W)", "w", 280, 180),
                ("Menú (Esc)", "{ESC}", 150, 240),
            };
            foreach (var (t, s, bx, by) in navBtns)
            {
                var b = Btn(t, bx, by, 130, 44, null);
                b.Click += (_, _) => SendKeyToGame(s);
                nav.Controls.Add(b);
            }
            tab.Controls.Add(nav);
        }

        private void BuildAboutTab(TabPage tab)
        {
            tab.AutoScroll = true;
            var info = new InfoBox("JavaViva v1.0.2 — Tus juegos Java .jar como en tu celular de los 2000.\n\nMotor: FreeJ2ME-Plus v1.53 (GPLv3), de TASEmulators, David Richardson y Saket Dandawate. JavaViva es solo el launcher con biblioteca y configuracion por juego; el emulador hace el trabajo pesado. Sin JavaViva, el mismo juego se puede abrir con: java -jar engine\\freej2me_plus.jar \"file:///C:\\ruta\\juego.jar\" 0 240 320 2\n\nQue NO hace: no convierte .jar a .apk ni a .exe, no descarga juegos y no garantiza que todos los juegos funcionen. Cada juego fue hecho para un telefono concreto; si uno no arranca, prueba otra resolucion o perfil en Configuracion. Usa solo .jar que tengas legalmente.");
            info.Location = new Point(14, 12); info.Size = new Size(960, 170);
            tab.Controls.Add(info);
            var cred = new Label { Text = "Creado por Nous Logic  •  nouslogic.xyz\nSi te sirve, comparte JavaViva con quien jugaba en Nokia o Sony Ericsson.", Location = new Point(14, 196), Size = new Size(960, 60), ForeColor = TextMuted };
            tab.Controls.Add(cred);
        }

        // ---------------- Biblioteca: datos ----------------
        // v1.0.2: el java.exe elegido a mano se guarda en
        // %AppData%\JavaViva\settings.json. Antes se perdia al cerrar.
        private string SettingsPath => Path.Combine(Library.AppDir, "settings.json");

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var json = File.ReadAllText(SettingsPath);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("customJava", out var v))
                    customJava = v.GetString() ?? "";
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                Directory.CreateDirectory(Library.AppDir);
                File.WriteAllText(SettingsPath,
                    System.Text.Json.JsonSerializer.Serialize(new { customJava }));
            }
            catch { }
        }

        private void CheckJava()
        {
            (javaExe, javaHow) = EmulatorConfig.FindJava(baseDir, customJava);
            if (lblJava != null)
            {
                lblJava.Text = javaExe != null ? "✓ Java listo: " + javaHow : "⚠ Sin Java: instala Adoptium Temurin JRE";
                lblJava.ForeColor = javaExe != null ? Accent : Warn;
            }
            if (txtJava != null) txtJava.Text = customJava;
        }

        private void ScanGamesFolder()
        {
            var dir = Library.GamesDir(baseDir);
            Directory.CreateDirectory(dir);
            foreach (var f in Directory.GetFiles(dir, "*.jar").Concat(Directory.GetFiles(dir, "*.jad")))
            {
                var g = JarInspector.Inspect(f);
                if (!g.IsValid)
                {
                    // v1.0.2: no se agrega en silencio; se avisa en el registro.
                    Log("Ignorado (no es un juego valido): " + Path.GetFileName(f) + " — " + g.ValidationError);
                    continue;
                }
                EmulatorConfig.ApplyKnownFixes(g);
                Library.Upsert(games, g);
            }
            Library.Save(games);
        }

        private void RefreshList()
        {
            lvGames.Items.Clear();
            foreach (var g in games.OrderBy(x => x.DisplayName))
            {
                var (w, h) = g.EffectiveResolution();
                var item = new ListViewItem(new[]
                {
                    g.DisplayName,
                    g.Vendor,
                    g.SizeBytes >= 1048576 ? $"{g.SizeBytes / 1048576.0:F1} MB" : $"{g.SizeBytes / 1024} KB",
                    g.ResolutionMode == "Auto" ? (string.IsNullOrWhiteSpace(g.AutoResolution) ? "Auto (240x320)" : "Auto: " + g.AutoResolution) : $"{w}x{h}",
                    g.LastPlayed?.ToString("dd/MM HH:mm") ?? "—",
                }) { Tag = g };
                lvGames.Items.Add(item);
            }
            SelectFromList();
        }

        private void SelectFromList()
        {
            selected = lvGames.SelectedItems.Count > 0 ? lvGames.SelectedItems[0].Tag as GameEntry : games.FirstOrDefault();
            if (selected == null) { lblDetail.Text = "No hay juegos. Agrega un .jar."; return; }
            var (w, h) = selected.EffectiveResolution();
            lblDetail.Text = $"🎮 {selected.DisplayName}  •  {selected.Vendor} {selected.Version}  •  {selected.Profile}\n" +
                $"📄 {selected.FilePath}  ({selected.SizeBytes / 1024} KB)\n" +
                $"📐 Resolución: {w}x{h} ({selected.ResolutionMode}) — {selected.AutoHow}\n" +
                $"📱 Perfil: {selected.Phone}  •  Escala: {selected.Scale}x  •  Sonido: {(selected.Sound ? "sí" : "no")}  •  Repaint on setCurrent: {(selected.CompatRepaintOnSetCurrent ? "sí" : "no")}";
            LoadGameIntoConfig();
        }

        private void AddGameDialog()
        {
            using var d = new OpenFileDialog { Filter = "Juegos Java|*.jar;*.jad|JAR|*.jar|JAD|*.jad", Title = "Elige un juego .jar o .jad", Multiselect = true };
            if (d.ShowDialog() != DialogResult.OK) return;
            var dir = Library.GamesDir(baseDir);
            Directory.CreateDirectory(dir);
            var rejected = new List<string>();
            foreach (var src in d.FileNames)
            {
                var dest = Path.Combine(dir, Path.GetFileName(src));
                try
                {
                    if (!string.Equals(Path.GetFullPath(src), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
                        File.Copy(src, dest, true);
                    var g = JarInspector.Inspect(dest);
                    if (!g.IsValid)
                    {
                        // v1.0.2: se rechaza con el motivo, no se agrega en silencio.
                        rejected.Add(Path.GetFileName(src) + ": " + g.ValidationError);
                        Log("Rechazado: " + Path.GetFileName(src) + " — " + g.ValidationError);
                        continue;
                    }
                    EmulatorConfig.ApplyKnownFixes(g);
                    Library.Upsert(games, g);
                    Log("Agregado: " + g.DisplayName + " — " + g.AutoHow);
                }
                catch (Exception ex) { Log("No se pudo agregar " + src + ": " + ex.Message); }
            }
            if (rejected.Count > 0)
                MessageBox.Show("Estos archivos no son juegos J2ME validos y no se agregaron:\n\n• " + string.Join("\n• ", rejected),
                    "JavaViva - Archivos rechazados", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Library.Save(games);
            RefreshList();
        }

        private void RemoveSelected()
        {
            if (selected == null) return;
            if (MessageBox.Show("Quitar " + selected.DisplayName + " de la lista? (El archivo .jar se queda en la carpeta Juegos.)",
                "JavaViva", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            games.Remove(selected);
            Library.Save(games);
            RefreshList();
            Log("Quitado de la lista: " + selected.DisplayName);
        }

        // ---------------- Configuracion: cargar / guardar ----------------
        private void LoadGameIntoConfig()
        {
            if (selected == null) return;
            lblConfigFor.Text = "Configuración para: " + selected.DisplayName + (string.IsNullOrWhiteSpace(selected.AutoHow) ? "" : "  —  " + selected.AutoHow);
            rbAuto.Checked = selected.ResolutionMode == "Auto";
            rbPreset.Checked = selected.ResolutionMode == "Preset";
            rbCustom.Checked = selected.ResolutionMode == "Custom";
            cmbPreset.SelectedItem = selected.Preset;
            numW.Value = Math.Clamp(selected.CustomWidth, 96, 854);
            numH.Value = Math.Clamp(selected.CustomHeight, 65, 854);
            cmbScale.SelectedItem = selected.Scale + "x";
            // v1.0.2: lo guardado es el valor interno del motor (MotoV8...);
            // en el combo se muestra su etiqueta (Motorola V8...).
            cmbPhone.SelectedItem = EmulatorConfig.DisplayForPhone(selected.Phone);
            cmbRotate.SelectedItem = selected.Rotate + "°";
            cmbFps.SelectedItem = selected.Fps <= 0 ? "Auto (60)" : selected.Fps.ToString();
            chkSound.Checked = selected.Sound;
            cmbBacklight.SelectedItem = selected.Backlight;
            chkRepaint.Checked = selected.CompatRepaintOnSetCurrent;
            chkImmediate.Checked = selected.CompatImmediateRepaints;
            chkOverride.Checked = selected.CompatOverridePlatChecks;
            chkM3gHalf.Checked = selected.M3gHalfRes;
            cmbM3gAa.SelectedItem = selected.M3gAntialias;
        }

        private bool SaveConfigFromUI(bool silent = false)
        {
            if (selected == null)
            {
                if (!silent) MessageBox.Show("Elige un juego en Biblioteca primero.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            selected.ResolutionMode = rbAuto.Checked ? "Auto" : rbPreset.Checked ? "Preset" : "Custom";
            selected.Preset = cmbPreset.SelectedItem?.ToString() ?? "240x320";
            selected.CustomWidth = (int)numW.Value;
            selected.CustomHeight = (int)numH.Value;
            selected.Scale = int.TryParse((cmbScale.SelectedItem?.ToString() ?? "2x").TrimEnd('x'), out int sc) ? sc : 2;
            // v1.0.2: se guarda el valor interno del motor, no la etiqueta.
            selected.Phone = EmulatorConfig.ValueForPhone(cmbPhone.SelectedItem?.ToString());
            selected.Rotate = int.TryParse((cmbRotate.SelectedItem?.ToString() ?? "0°").TrimEnd('°'), out int rot) ? rot : 0;
            var fps = cmbFps.SelectedItem?.ToString() ?? "Auto (60)";
            selected.Fps = fps.StartsWith("Auto") ? 60 : int.TryParse(fps, out int f) ? f : 60;
            selected.Sound = chkSound.Checked;
            selected.Backlight = cmbBacklight.SelectedItem?.ToString() ?? "Disabled";
            selected.CompatRepaintOnSetCurrent = chkRepaint.Checked;
            selected.CompatImmediateRepaints = chkImmediate.Checked;
            selected.CompatOverridePlatChecks = chkOverride.Checked;
            selected.M3gHalfRes = chkM3gHalf.Checked;
            selected.M3gAntialias = cmbM3gAa.SelectedItem?.ToString() ?? "app";
            return true;
        }

        // ---------------- Jugar ----------------
        private void PlaySelected() => Launch(selected, null);

        private void PlayWithResolution(int w, int h)
        {
            if (selected == null) { MessageBox.Show("Elige un juego en Biblioteca primero.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            Launch(selected, (w, h));
        }

        private void Launch(GameEntry? g, (int w, int h)? quickRes)
        {
            if (g == null) { MessageBox.Show("Elige un juego en Biblioteca primero.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!File.Exists(g.FilePath)) { MessageBox.Show("No encuentro el archivo:\n" + g.FilePath, "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
            // v1.0.2: un .jar invalido ya no llega aqui (se rechaza al agregar),
            // pero si quedo en la biblioteca de una version vieja, se avisa con
            // el motivo en vez del generico "El juego se cerro".
            if (!g.IsValid)
            {
                MessageBox.Show("Este archivo no es un juego J2ME valido:\n\n" + g.ValidationError +
                    "\n\nQuitalo de la lista y agrega el .jar correcto.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // v1.0.2: antes Jugar usaba la configuracion guardada y los cambios
            // sin pulsar Guardar se perdian. Ahora se aplican al lanzar.
            if (ReferenceEquals(g, selected) && SaveConfigFromUI(silent: true)) Library.Save(games);
            CheckJava();
            if (javaExe == null)
            {
                MessageBox.Show("No se encontró Java.\n\nEl paquete incluye un runtime en runtime\\windows-x64. Si esa carpeta falta, instala gratis Adoptium Temurin JRE (Java 8 o mas) y vuelve a abrir JavaViva.",
                    "JavaViva - Falta Java", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var engine = EmulatorConfig.EngineJar(baseDir);
            if (!File.Exists(engine))
            {
                MessageBox.Show("Falta el motor en:\n" + engine + "\n\nVuelve a descomprimir el paquete completo de JavaViva (el .exe solo no basta).",
                    "JavaViva - Falta el motor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            StopGame(silent: true);

            // Resolucion rapida: no se guarda, solo se prueba.
            var savedMode = g.ResolutionMode; var savedW = g.CustomWidth; var savedH = g.CustomHeight;
            if (quickRes != null) { g.ResolutionMode = "Custom"; g.CustomWidth = quickRes.Value.w; g.CustomHeight = quickRes.Value.h; }

            var dataDir = Library.DataDir(g.SafeName);
            Directory.CreateDirectory(dataDir);
            EmulatorConfig.WriteGameConf(dataDir, g);
            var (w, h) = g.EffectiveResolution();
            var psi = EmulatorConfig.BuildStart(javaExe, baseDir, g, dataDir);

            if (quickRes != null) { g.ResolutionMode = savedMode; g.CustomWidth = savedW; g.CustomHeight = savedH; }

            Log($"▶ Jugando {g.DisplayName} — {w}x{h} escala {g.Scale}x, perfil {g.Phone}, repaint-on-setCurrent={(g.CompatRepaintOnSetCurrent ? "on" : "off")}");
            Log("  Comando: \"" + psi.FileName + "\" " + psi.Arguments);
            Log("  Datos/guardados en: " + dataDir);
            try
            {
                gameProcess = new Process { StartInfo = psi, EnableRaisingEvents = true };
                gameProcess.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log("[juego] " + e.Data); };
                gameProcess.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log("[juego] " + e.Data); };
                gameProcess.Exited += (_, _) => BeginInvoke(() => { btnStop.Enabled = false; btnPlay.Enabled = true; Status("El juego se cerró."); Log("El juego se cerró (código " + (gameProcess?.ExitCode.ToString() ?? "?") + ")."); });
                gameProcess.Start();
                gameProcess.BeginOutputReadLine();
                gameProcess.BeginErrorReadLine();
                g.LastPlayed = DateTime.Now;
                Library.Save(games);
                RefreshList();
                btnStop.Enabled = true;
                Status("Jugando: " + g.DisplayName + " (" + w + "x" + h + "). La ventana del juego se abre aparte.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo lanzar el juego:\n" + ex.Message, "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("Error al lanzar: " + ex.Message);
            }
        }

        private void StopGame(bool silent = false)
        {
            try
            {
                if (gameProcess is { HasExited: false })
                {
                    gameProcess.Kill(entireProcessTree: true);
                    if (!silent) Log("Juego detenido.");
                }
            }
            catch { }
            gameProcess = null;
            btnStop.Enabled = false;
            btnPlay.Enabled = true;
        }

        private void SendKeyToGame(string keys)
        {
            if (gameProcess is null or { HasExited: true })
            {
                MessageBox.Show("Primero pulsa ▶ Jugar para abrir el juego.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                // v1.0.2: antes se hacia SetForegroundWindow y se enviaba la
                // tecla enseguida; si la ventana del juego aun no estaba lista,
                // la tecla caia en JavaViva. Ahora se espera a que la ventana
                // exista y se le da un momento al frente antes de enviar.
                gameProcess.Refresh();
                IntPtr h = IntPtr.Zero;
                for (int i = 0; i < 10 && h == IntPtr.Zero; i++)
                {
                    h = gameProcess.MainWindowHandle;
                    if (h == IntPtr.Zero) System.Threading.Thread.Sleep(100);
                }
                if (h != IntPtr.Zero) SetForegroundWindow(h);
                System.Threading.Thread.Sleep(120);
                SendKeys.SendWait(keys);
            }
            catch (Exception ex) { Log("No se pudo enviar la tecla: " + ex.Message); }
        }

        // ---------------- Helpers ----------------
        private Button Btn(string text, int x, int y, int w, int h, Color? bg = null)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = bg ?? Accent, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            var baseColor = b.BackColor;
            b.MouseEnter += (_, _) => { if (b.Enabled) b.BackColor = ControlPaint.Light(baseColor, 0.15f); };
            b.MouseLeave += (_, _) => { b.BackColor = baseColor; };
            return b;
        }

        private void Status(string s) { if (lblStatus != null) lblStatus.Text = s; }

        private void Log(string s)
        {
            if (logBox == null) return;
            if (logBox.InvokeRequired) { logBox.BeginInvoke(() => Log(s)); return; }
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + s + Environment.NewLine);
        }
    }
}
