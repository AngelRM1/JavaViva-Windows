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
        // v1.1.6: el java.exe que se lanza (suele salir enseguida: el motor
        // se relanza en otro JVM) y el nombre del juego que corre.
        private Process? launcherProcess;
        private string runningGameName = "";
        private string? javaExe;
        private string javaHow = "";
        private string customJava = "";

        // Biblioteca
        private ListView lvGames = null!;
        private Label lblDetail = null!;
        private Label lblStatus = null!;
        private Label lblJava = null!;
        // v1.1.5: favoritos + buscar en la biblioteca, captura de pantalla
        // y respaldo de la biblioteca.
        private TextBox txtSearch = null!;
        private CheckBox chkFavOnly = null!;
        private Button btnFav = null!;
        private Button btnPlay = null!;
        private Button btnStop = null!;
        private Button btnPlayLib = null!;
        // v1.1.4: aviso de nueva version (boton en la barra inferior).
        private Button btnUpdate = null!;
        private string? latestVersion;
        private string? latestUrl;
        private bool checkingUpdate = false;
        // v1.1.3: selector de resolucion en Biblioteca (el paso 2 del flujo:
        // elegir juego -> elegir resolucion -> Jugar/Iniciar).
        private ComboBox cmbResLib = null!;
        private bool syncingRes = false;
        private TextBox logBox = null!;

        // Configuracion
        private RadioButton rbAuto = null!, rbPreset = null!, rbCustom = null!;
        private ComboBox cmbPreset = null!;
        private DarkNumberBox numW = null!, numH = null!;
        private ComboBox cmbScale = null!, cmbPhone = null!, cmbRotate = null!, cmbFps = null!, cmbBacklight = null!, cmbM3gAa = null!;
        private CheckBox chkSound = null!, chkRepaint = null!, chkImmediate = null!, chkOverride = null!, chkM3gHalf = null!;
        private TextBox txtJava = null!;
        private Label lblConfigFor = null!;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // v1.1.5: captura de pantalla de la ventana del juego.
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        public MainForm()
        {
            Text = "JavaViva v1.1.7  -  Juegos Java .jar como en tu celular de los 2000  |  Nous Logic";
            Size = new Size(1020, 860);
            MinimumSize = new Size(900, 700);
            // v1.1.6: si la pantalla es mas baja que la ventana (p. ej. 768p),
            // la ventana no cabe y lo de abajo queda cortado. Ajustarla al area de trabajo.
            var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1040);
            if (Size.Height > wa.Height) Size = new Size(Size.Width, Math.Max(MinimumSize.Height, wa.Height));
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Bg; ForeColor = TextLight;
            Font = new Font("Segoe UI", 9.5f);
            AutoScroll = true;

            BuildHeader();
            BuildTabs();
            // v1.1.7: la seccion de terminal de abajo se elimino. El
            // registro (Log) queda sin salida visible y el boton de
            // actualizaciones se movio al encabezado (BuildHeader).

            Load += (_, _) =>
            {
                // v1.1.0: completar el modo oscuro en las piezas nativas
                // (barra de titulo y barras de scroll) y en los controles
                // que se crean despues de esta llamada.
                Theme.ApplyDarkTitleBar(this);
                Theme.ApplyDarkScrollbars(this);
                Theme.ApplyDarkControls(this);
                tabs.SelectedIndexChanged += (_, _) => Theme.ApplyDarkScrollbars(this);
                LoadSettings();
                CheckJava();
                games = Library.Load();
                // v1.1.3: limpiar la lista guardada (entradas de archivos
                // que ya no existen y duplicados de otras carpetas).
                var prunedGames = Library.PruneStale(games, baseDir);
                int pruned = games.Count - prunedGames.Count;
                games = prunedGames;
                if (pruned > 0) Log("Lista limpiada: " + pruned + " entrada(s) sin archivo o duplicadas.");
                ScanGamesFolder();
                RefreshList();
                Log("JavaViva v1.1.7 listo. Motor: " + EmulatorConfig.EngineVersion + " (GPLv3).");
                Log("Java: " + (javaExe ?? "NO ENCONTRADO") + " — " + javaHow);
                if (javaExe == null)
                    Log("Sin Java no se puede jugar. El paquete incluye un runtime en runtime\\windows-x64; si falta, instala Adoptium Temurin JRE desde Configuracion.");
                if (games.Count == 0)
                    Log("No hay juegos todavia. Pulsa 'Agregar .jar' o copia tus .jar en la carpeta Juegos y pulsa Refrescar.");
                // v1.1.4: comprobar actualizaciones al arrancar (en silencio
                // si no hay internet: solo queda en el registro).
                _ = CheckForUpdateAsync(manual: false);
            };
            FormClosing += (_, _) => { try { if (gameProcess is { HasExited: false }) gameProcess.Kill(entireProcessTree: true); } catch { } try { if (launcherProcess is { HasExited: false }) launcherProcess.Kill(entireProcessTree: true); } catch { } };
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
            // v1.1.3: el subtitulo largo y el texto de Java se cortaban por
            // la derecha (se salian de la ventana). Subtitulo corto y las
            // dos etiquetas de la derecha ancladas al borde con ancho fijo
            // y puntos suspensivos si el texto no cabe.
            var sub = new Label { Text = "Tus juegos .jar de Java, como en tu celular de los 2000", ForeColor = TextMuted, Location = new Point(86, 48), AutoSize = true };
            lblJava = new Label { Text = "Buscando Java...", ForeColor = TextMuted, Location = new Point(ClientSize.Width - 520, 12), Size = new Size(260, 20), TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            var ver = new Label { Text = "v1.1.7  •  Nous Logic  •  FreeJ2ME-Plus v1.53", ForeColor = TextMuted, Location = new Point(ClientSize.Width - 520, 38), Size = new Size(260, 20), TextAlign = ContentAlignment.MiddleRight, AutoEllipsis = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            header.Controls.AddRange(new Control[] { logoBox, title, sub, lblJava, ver });
            // v1.1.7: el boton de actualizaciones vivia en la barra inferior
            // y la terminal lo dejaba cortado. Ahora esta en el encabezado,
            // a la derecha de las etiquetas, centrado en vertical (y=21,
            // alto 40, igual que todos los botones de accion) y anclado al
            // borde derecho. No usa Btn(): su color cambia a verde cuando
            // hay una version nueva, y el Btn restaura su color original
            // al salir el raton. Aqui el color al salir depende de si hay aviso.
            btnUpdate = new Button { Text = "Buscar actualizaciones", Location = new Point(ClientSize.Width - 260, 21), Size = new Size(240, 40), BackColor = Color.FromArgb(100, 116, 139), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.MouseEnter += (_, _) => { if (btnUpdate.Enabled) btnUpdate.BackColor = ControlPaint.Light(btnUpdate.BackColor, 0.15f); };
            btnUpdate.MouseLeave += (_, _) => { btnUpdate.BackColor = latestVersion != null ? Accent : Color.FromArgb(100, 116, 139); };
            btnUpdate.Click += (_, _) => OnUpdateButtonClick();
            header.Controls.Add(btnUpdate);
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
            tabs = new TabControl { Location = new Point(0, 112), Size = new Size(ClientSize.Width, ClientSize.Height - 112), BackColor = Bg, ForeColor = TextLight, Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right };
            Theme.StyleTabs(tabs);
            var tabLib = new TabPage("📚  Biblioteca") { BackColor = Bg, UseVisualStyleBackColor = false };
            var tabCfg = new TabPage("⚙️  Configuración") { BackColor = Bg, UseVisualStyleBackColor = false };
            var tabKeys = new TabPage("📱  Teclas") { BackColor = Bg, UseVisualStyleBackColor = false };
            var tabDon = new TabPage("❤  Donaciones") { BackColor = Bg, UseVisualStyleBackColor = false };
            var tabAbout = new TabPage("ℹ️  Acerca de") { BackColor = Bg, UseVisualStyleBackColor = false };
            tabs.TabPages.AddRange(new[] { tabLib, tabCfg, tabKeys, tabDon, tabAbout });
            BuildLibraryTab(tabLib);
            BuildConfigTab(tabCfg);
            BuildKeysTab(tabKeys);
            BuildDonationsTab(tabDon);
            BuildAboutTab(tabAbout);
            Controls.Add(tabs);
        }

        // v1.1.7: la barra inferior con la terminal (registro) se elimino:
        // el registro queda sin salida visible y el boton de
        // actualizaciones se movio al encabezado. Log() y Status() ya
        // toleran que logBox y lblStatus sean nulos (no hacen nada).

        private void OnUpdateButtonClick()
        {
            if (checkingUpdate) return;
            if (latestVersion != null && latestUrl != null)
            {
                if (MessageBox.Show("Hay una versión nueva de JavaViva (" + latestVersion + ").\n\n¿Abrir la página de descarga?",
                    "JavaViva - Nueva versión", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    OpenUrl(latestUrl);
                return;
            }
            _ = CheckForUpdateAsync(manual: true);
        }

        // v1.1.4: aviso de nueva version al arrancar (como GameBoost v1.8.0).
        // Lee el ultimo release de AngelRM1/JavaViva-Windows en GitHub. Si
        // no hay internet o GitHub no responde, no molesta: solo queda en
        // el registro. Con manual=true (pulsar el boton) si avisa con un
        // mensaje, haya update o no.
        private async Task CheckForUpdateAsync(bool manual)
        {
            if (checkingUpdate) return;
            checkingUpdate = true;
            if (btnUpdate != null) btnUpdate.Enabled = false;
            try
            {
                using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(8) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("JavaViva");
                var json = await http.GetStringAsync("https://api.github.com/repos/AngelRM1/JavaViva-Windows/releases/latest");
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";
                var url = doc.RootElement.TryGetProperty("html_url", out var u) ? u.GetString() ?? "" : "";
                var verText = tag.TrimStart('v', 'V');
                if (!Version.TryParse(verText, out var latest))
                {
                    if (manual) MessageBox.Show("GitHub respondió con una versión que no entiendo: " + tag, "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    Log("No se entendió la versión del release: " + tag);
                    return;
                }
                var current = typeof(MainForm).Assembly.GetName().Version ?? new Version(1, 1, 4);
                string curText = "v" + current.Major + "." + current.Minor + "." + current.Build;
                if (latest > current)
                {
                    latestVersion = "v" + verText;
                    latestUrl = string.IsNullOrEmpty(url) ? "https://github.com/AngelRM1/JavaViva-Windows/releases/latest" : url;
                    if (btnUpdate != null)
                    {
                        btnUpdate.Text = "¡Nueva versión: " + latestVersion + "!";
                        btnUpdate.BackColor = Accent;
                    }
                    Log("Hay una versión nueva: " + latestVersion + " (tienes " + curText + "). Pulsa el botón verde para descargarla.");
                    if (manual) MessageBox.Show("Hay una versión nueva: " + latestVersion + " (tienes " + curText + ").\n\nPulsa el botón verde para abrir la descarga.", "JavaViva - Nueva versión", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Log("Sin actualizaciones: tienes la versión más reciente (" + curText + ").");
                    if (manual) MessageBox.Show("Ya tienes la versión más reciente (" + curText + ").", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log("No se pudo comprobar actualizaciones: " + ex.Message);
                if (manual) MessageBox.Show("No se pudo comprobar si hay actualizaciones.\n\nRevisa tu conexión a internet e inténtalo de nuevo.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                checkingUpdate = false;
                if (btnUpdate != null) btnUpdate.Enabled = true;
            }
        }

        // ---------------- Biblioteca ----------------
        private void BuildLibraryTab(TabPage tab)
        {
            tab.AutoScroll = true;
            // v1.1.5: buscar y solo favoritos, encima del listado.
            txtSearch = new TextBox { Location = new Point(14, 14), Size = new Size(500, 26), BackColor = CardBg, ForeColor = TextLight, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.5f), PlaceholderText = "🔍  Buscar por nombre, fabricante o archivo..." };
            txtSearch.TextChanged += (_, _) => RefreshList();
            chkFavOnly = new CheckBox { Text = "★  Solo favoritos", Location = new Point(524, 16), Size = new Size(170, 22), BackColor = Bg, ForeColor = TextLight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            chkFavOnly.CheckedChanged += (_, _) => RefreshList();
            tab.Controls.AddRange(new Control[] { txtSearch, chkFavOnly });
            lvGames = new ListView
            {
                Location = new Point(14, 48), Size = new Size(700, 346), View = View.Details,
                FullRowSelect = true, GridLines = true, BackColor = CardBg, ForeColor = TextLight,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f), Anchor = AnchorStyles.Top | AnchorStyles.Left,
            };
            lvGames.Columns.Add("★", 30);
            lvGames.Columns.Add("Juego", 210);
            lvGames.Columns.Add("Fabricante", 130);
            lvGames.Columns.Add("Tamaño", 80);
            lvGames.Columns.Add("Resolución auto", 130);
            lvGames.Columns.Add("Última vez", 110);
            // v1.1.0: encabezados y filas en oscuro (sin esto el encabezado
            // de columnas quedaba gris claro).
            Theme.StyleDarkListView(lvGames);
            lvGames.SelectedIndexChanged += (_, _) => { SelectFromList(); };
            lvGames.DoubleClick += (_, _) => PlaySelected();
            tab.Controls.Add(lvGames);

            int x = 730, y = 14;
            // v1.1.7: todos los botones de accion miden 240x40. Antes la
            // columna mezclaba 40, 32 y 42 de alto; ahora todos iguales,
            // con 8 px entre uno y otro (paso 48).
            btnPlay = Btn("▶  Jugar", x, y, 240, 40, Accent); y += 48;
            btnPlay.Click += (_, _) => PlaySelected();
            btnStop = Btn("⏹  Detener juego", x, y, 240, 40, Color.FromArgb(100, 116, 139)); y += 48;
            btnStop.Enabled = false;
            btnStop.Click += (_, _) => StopGame();
            var bAdd = Btn("📁  Agregar .jar / .jad...", x, y, 240, 40, null); y += 48;
            bAdd.Click += (_, _) => AddGameDialog();
            var bScan = Btn("🔄  Refrescar carpeta Juegos", x, y, 240, 40, null); y += 48;
            bScan.Click += (_, _) => { ScanGamesFolder(); RefreshList(); Log("Carpeta Juegos actualizada: " + games.Count + " juego(s)."); };
            var bOpen = Btn("📂  Abrir carpeta Juegos", x, y, 240, 40, null); y += 48;
            bOpen.Click += (_, _) => { var d = Library.GamesDir(baseDir); Directory.CreateDirectory(d); Process.Start(new ProcessStartInfo { FileName = d, UseShellExecute = true }); };
            var bDel = Btn("🗑  Eliminar juego", x, y, 240, 40, Color.FromArgb(239, 68, 68)); y += 48;
            bDel.Click += (_, _) => RemoveSelected();
            var bClear = Btn("🧹  Limpiar lista", x, y, 240, 40, Color.FromArgb(100, 116, 139)); y += 48;
            bClear.Click += (_, _) => ClearLibrary();
            // v1.1.5: favorito y captura de pantalla del juego en marcha.
            btnFav = Btn("★  Marcar favorito", x, y, 240, 40, null); y += 48;
            btnFav.Click += (_, _) => ToggleFavorite();
            var bShot = Btn("📷  Captura de pantalla", x, y, 240, 40, null); y += 48;
            bShot.Click += (_, _) => CaptureGameScreenshot();
            tab.Controls.AddRange(new Control[] { btnPlay, btnStop, bAdd, bScan, bOpen, bDel, bClear, btnFav, bShot });

            lblDetail = new Label { Location = new Point(14, 406), Size = new Size(700, 90), ForeColor = TextLight, Font = new Font("Segoe UI", 9.5f) };
            tab.Controls.Add(lblDetail);
            // v1.1.2: boton para arrancar el juego seleccionado junto a su
            // detalle (nombre, resolucion, perfil): lo mismo que ▶ Jugar,
            // pero aqui donde se ve con que resolucion va a arrancar.
            // v1.1.7: 240x40 como todos los botones de accion; baja a y=446
            // porque la columna derecha ahora termina en 438.
            btnPlayLib = Btn("▶  Iniciar juego", 730, 446, 240, 40, Accent);
            btnPlayLib.Click += (_, _) => PlaySelected();
            tab.Controls.Add(btnPlayLib);
            // v1.1.3: paso 2 del flujo en Biblioteca (elegir juego ->
            // elegir resolucion -> Jugar/Iniciar): selector de resolucion
            // para el juego seleccionado, sin ir a Configuracion.
            var resLbl = new Label { Text = "Resolución para este juego:", Location = new Point(14, 506), Size = new Size(210, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            cmbResLib = new ComboBox { Location = new Point(230, 502), Width = 210, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            Theme.StyleDarkComboBox(cmbResLib);
            cmbResLib.SelectedIndexChanged += (_, _) =>
            {
                if (syncingRes || selected == null) return;
                var item = cmbResLib.SelectedItem?.ToString() ?? "";
                if (cmbResLib.SelectedIndex == 0) selected.ResolutionMode = "Auto";
                else if (item.EndsWith(" (personalizada)")) { /* se queda la personalizada */ }
                else { selected.ResolutionMode = "Preset"; selected.Preset = item; }
                Library.Save(games);
                RefreshList();
            };
            var resNote = new Label { Text = "Automática detecta la del .jar. Si se ve cortado o en blanco, elige otra y pulsa Iniciar juego.", Location = new Point(452, 506), Size = new Size(520, 22), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            tab.Controls.AddRange(new Control[] { resLbl, cmbResLib, resNote });

            var quick = new RoundedPanel { Location = new Point(14, 540), Size = new Size(960, 78), BackColor = CardBg };
            var qTitle = new Label { Text = "Probar rápido con otra resolución (sin guardar): si se ve cortado o en blanco, prueba otra.", Location = new Point(14, 8), Size = new Size(920, 20), ForeColor = TextLight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            quick.Controls.Add(qTitle);
            // v1.1.7: los 5 botones de resolucion rapida miden lo mismo
            // entre si (170x40): 240 de ancho no caben 5 en una fila.
            var q1 = Btn("240x320", 14, 34, 170, 40, null); q1.Click += (_, _) => PlayWithResolution(240, 320);
            var q2 = Btn("176x208", 194, 34, 170, 40, null); q2.Click += (_, _) => PlayWithResolution(176, 208);
            var q3 = Btn("320x240 ◐", 374, 34, 170, 40, null); q3.Click += (_, _) => PlayWithResolution(320, 240);
            var q4 = Btn("128x160", 554, 34, 170, 40, null); q4.Click += (_, _) => PlayWithResolution(128, 160);
            var q5 = Btn("352x416", 734, 34, 170, 40, null); q5.Click += (_, _) => PlayWithResolution(352, 416);
            quick.Controls.AddRange(new Control[] { q1, q2, q3, q4, q5 });
            tab.Controls.Add(quick);
            // v1.1.5: respaldo de la biblioteca: exporta la lista a un .json
            // para recuperarla si se borra o se cambia de PC.
            var backup = new RoundedPanel { Location = new Point(14, 628), Size = new Size(960, 84), BackColor = CardBg };
            var bkTitle = new Label { Text = "💾  Respaldo de la biblioteca  (por si se borra la lista o cambias de PC)", Location = new Point(14, 10), Size = new Size(930, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };
            var bExport = Btn("⬇  Exportar respaldo...", 14, 40, 240, 40, null);
            bExport.Click += (_, _) => ExportLibrary();
            var bImport = Btn("⬆  Importar respaldo...", 266, 40, 240, 40, null);
            bImport.Click += (_, _) => ImportLibrary();
            var bkNote = new Label { Text = "Exporta la lista a un .json. Al importar se agregan los juegos que falten, sin borrar los que ya tienes.", Location = new Point(520, 42), Size = new Size(426, 34), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            backup.Controls.AddRange(new Control[] { bkTitle, bExport, bImport, bkNote });
            tab.Controls.Add(backup);
            AddBottomSpacer(tab, 722);
        }

        // v1.1.3: mantiene el selector de resolucion de Biblioteca al tanto
        // del juego seleccionado (y de lo que se cambie en Configuracion).
        private void SyncLibResCombo()
        {
            if (cmbResLib == null) return;
            syncingRes = true;
            try
            {
                cmbResLib.Items.Clear();
                cmbResLib.Items.Add("Automática (recomendada)");
                cmbResLib.Items.AddRange(EmulatorConfig.Resolutions);
                if (selected == null) { cmbResLib.SelectedIndex = 0; return; }
                if (selected.ResolutionMode == "Auto") cmbResLib.SelectedIndex = 0;
                else if (selected.ResolutionMode == "Preset") cmbResLib.SelectedItem = selected.Preset;
                else
                {
                    string custom = selected.CustomWidth + "x" + selected.CustomHeight + " (personalizada)";
                    cmbResLib.Items.Add(custom);
                    cmbResLib.SelectedItem = custom;
                }
                if (cmbResLib.SelectedIndex < 0) cmbResLib.SelectedIndex = 0;
            }
            finally { syncingRes = false; }
        }

        // ---------------- Configuracion ----------------
        private void BuildConfigTab(TabPage tab)
        {
            tab.AutoScroll = true;
            lblConfigFor = new Label { Text = "Configuración para: (elige un juego en Biblioteca)", Location = new Point(14, 12), Size = new Size(940, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            tab.Controls.Add(lblConfigFor);

            var card = new RoundedPanel { Location = new Point(14, 40), Size = new Size(960, 252), BackColor = CardBg };
            var t1 = new Label { Text = "📐  Pantalla del teléfono (resolución)", Location = new Point(14, 10), Size = new Size(400, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            card.Controls.Add(t1);
            rbAuto = new RadioButton { Text = "Automática (recomendado): JavaViva lee el .jar y pone la resolución para la que se hizo el juego", Location = new Point(16, 38), Size = new Size(920, 24), ForeColor = TextLight, Checked = true };
            rbPreset = new RadioButton { Text = "Elegir de la lista:", Location = new Point(16, 66), Size = new Size(160, 24), ForeColor = TextLight };
            cmbPreset = new ComboBox { Location = new Point(180, 66), Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbPreset.Items.AddRange(EmulatorConfig.Resolutions);
            cmbPreset.SelectedItem = "240x320";
            Theme.StyleDarkComboBox(cmbPreset);
            rbCustom = new RadioButton { Text = "Personalizada:", Location = new Point(340, 66), Size = new Size(130, 24), ForeColor = TextLight };
            numW = new DarkNumberBox { Location = new Point(480, 66), Width = 70, Minimum = 96, Maximum = 854, Value = 240 };
            var xl = new Label { Text = "x", Location = new Point(554, 68), Size = new Size(16, 22), ForeColor = TextLight };
            numH = new DarkNumberBox { Location = new Point(574, 66), Width = 70, Minimum = 65, Maximum = 854, Value = 320 };
            var autoNote = new Label { Text = "Cómo detecta la automática: busca ifone.png / glu.png en el .jar (su tamaño ES la resolución del juego) y si no, usa la imagen más grande del .jar. NFS Most Wanted: 240x320.", Location = new Point(16, 96), Size = new Size(920, 36), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            var sLbl = new Label { Text = "Escala de la ventana:", Location = new Point(16, 138), Size = new Size(150, 22), ForeColor = TextLight };
            cmbScale = new ComboBox { Location = new Point(170, 136), Width = 80, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbScale.Items.AddRange(new object[] { "1x", "2x", "3x" }); cmbScale.SelectedItem = "2x";
            Theme.StyleDarkComboBox(cmbScale);
            var rotLbl = new Label { Text = "Girar pantalla:", Location = new Point(280, 138), Size = new Size(110, 22), ForeColor = TextLight };
            cmbRotate = new ComboBox { Location = new Point(392, 136), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbRotate.Items.AddRange(new object[] { "0°", "90°", "180°", "270°" }); cmbRotate.SelectedItem = "0°";
            Theme.StyleDarkComboBox(cmbRotate);
            var fpsLbl = new Label { Text = "Límite FPS:", Location = new Point(510, 138), Size = new Size(90, 22), ForeColor = TextLight };
            cmbFps = new ComboBox { Location = new Point(600, 136), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbFps.Items.AddRange(new object[] { "Auto (60)", "60", "50", "30" }); cmbFps.SelectedItem = "Auto (60)";
            Theme.StyleDarkComboBox(cmbFps);
            var sndLbl = new Label { Text = "Sonido:", Location = new Point(720, 138), Size = new Size(60, 22), ForeColor = TextLight };
            chkSound = new CheckBox { Text = "Activado", Location = new Point(784, 138), Size = new Size(110, 22), ForeColor = TextLight, Checked = true };
            var blLbl = new Label { Text = "Luz de fondo:", Location = new Point(16, 170), Size = new Size(110, 22), ForeColor = TextLight };
            cmbBacklight = new ComboBox { Location = new Point(128, 168), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbBacklight.Items.AddRange(EmulatorConfig.Backlights); cmbBacklight.SelectedItem = "Disabled";
            Theme.StyleDarkComboBox(cmbBacklight);
            var blNote = new Label { Text = "Verde = Nokia clásico. Solo cambia el tinte de la pantalla.", Location = new Point(260, 170), Size = new Size(500, 22), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            // v1.1.1: boton para arrancar el juego justo despues de elegir
            // la resolucion (o dejar la automatica). Guarda la configuracion
            // y juega con ella; es lo mismo que "Probar con esta
            // configuracion" de abajo, pero aqui donde se elige.
            var bPlayCfg = Btn("▶  Iniciar juego", 16, 208, 240, 40, Accent);
            bPlayCfg.Click += (_, _) => { if (SaveConfigFromUI()) { Library.Save(games); PlaySelected(); } };
            var playNote = new Label { Text = "Arranca el juego con la resolución de arriba: la automática si no cambiaste nada, o la que elegiste.", Location = new Point(290, 214), Size = new Size(650, 26), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            card.Controls.AddRange(new Control[] { rbAuto, rbPreset, cmbPreset, rbCustom, numW, xl, numH, autoNote, sLbl, cmbScale, rotLbl, cmbRotate, fpsLbl, cmbFps, sndLbl, chkSound, blLbl, cmbBacklight, blNote, bPlayCfg, playNote });
            tab.Controls.Add(card);

            var card2 = new RoundedPanel { Location = new Point(14, 304), Size = new Size(960, 150), BackColor = CardBg };
            var t2 = new Label { Text = "📱  Interfaz / perfil de teléfono (teclas y controles)", Location = new Point(14, 10), Size = new Size(500, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            card2.Controls.Add(t2);
            var pLbl = new Label { Text = "Perfil:", Location = new Point(16, 42), Size = new Size(60, 22), ForeColor = TextLight };
            cmbPhone = new ComboBox { Location = new Point(76, 40), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            foreach (var p in EmulatorConfig.Phones) cmbPhone.Items.Add(p.Display);
            cmbPhone.SelectedItem = "Standard";
            Theme.StyleDarkComboBox(cmbPhone);
            var pHint = new Label { Text = "Standard vale para casi todos. Cambia a Nokia / Siemens / Motorola solo si el juego fue hecho para esa marca y las teclas no responden.", Location = new Point(310, 42), Size = new Size(630, 36), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            var m3gLbl = new Label { Text = "Gráficos 3D (M3G):", Location = new Point(16, 86), Size = new Size(130, 22), ForeColor = TextLight };
            chkM3gHalf = new CheckBox { Text = "Media resolución 3D (si va lento)", Location = new Point(150, 86), Size = new Size(240, 22), ForeColor = TextLight };
            var aaLbl = new Label { Text = "Antialiasing:", Location = new Point(410, 86), Size = new Size(90, 22), ForeColor = TextLight };
            cmbM3gAa = new ComboBox { Location = new Point(502, 84), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = CardBg, ForeColor = TextLight };
            cmbM3gAa.Items.AddRange(new object[] { "app", "on", "off" }); cmbM3gAa.SelectedItem = "app";
            Theme.StyleDarkComboBox(cmbM3gAa);
            var phNote = new Label { Text = "NFS Most Wanted usa 3D (M3G). Si se ve lento en tu PC, activa la media resolución 3D.", Location = new Point(16, 114), Size = new Size(920, 22), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            card2.Controls.AddRange(new Control[] { pLbl, cmbPhone, pHint, m3gLbl, chkM3gHalf, aaLbl, cmbM3gAa, phNote });
            tab.Controls.Add(card2);

            var card3 = new RoundedPanel { Location = new Point(14, 466), Size = new Size(960, 130), BackColor = CardBg };
            var t3 = new Label { Text = "🛠️  Compatibilidad (cuando un juego no arranca o se queda en blanco)", Location = new Point(14, 10), Size = new Size(700, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            card3.Controls.Add(t3);
            chkRepaint = new CheckBox { Text = "Repaint on setCurrent (NFS Most Wanted lo necesita, si no se queda en pantalla blanca)", Location = new Point(16, 40), Size = new Size(920, 22), ForeColor = TextLight };
            chkImmediate = new CheckBox { Text = "Immediate repaints (pruébalo si la pantalla parpadea o no se redibuja)", Location = new Point(16, 66), Size = new Size(920, 22), ForeColor = TextLight };
            chkOverride = new CheckBox { Text = "Override platform checks (activado por defecto, deja que el juego corra aunque pida otro teléfono)", Location = new Point(16, 92), Size = new Size(920, 22), ForeColor = TextLight, Checked = true };
            card3.Controls.AddRange(new Control[] { chkRepaint, chkImmediate, chkOverride });
            tab.Controls.Add(card3);

            var card4 = new RoundedPanel { Location = new Point(14, 608), Size = new Size(960, 96), BackColor = CardBg };
            var t4 = new Label { Text = "☕  Java (el motor que ejecuta el .jar)", Location = new Point(14, 10), Size = new Size(400, 22), ForeColor = TextLight, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            card4.Controls.Add(t4);
            txtJava = new TextBox { Location = new Point(16, 40), Width = 700, BackColor = CardBg, ForeColor = TextLight, ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
            var bJava = Btn("Examinar java.exe...", 730, 38, 240, 40, null);
            bJava.Click += (_, _) =>
            {
                using var d = new OpenFileDialog { Filter = "java.exe|java.exe|Todos|*.*", Title = "Elige java.exe" };
                if (d.ShowDialog() == DialogResult.OK) { customJava = d.FileName; txtJava.Text = customJava; SaveSettings(); CheckJava(); Log("Java personalizado: " + customJava); }
            };
            var jNote = new Label { Text = "Vacío = automático: primero el runtime incluido con JavaViva, si no el Java de tu PC. Si no tienes Java, descarga Adoptium Temurin JRE (gratis).", Location = new Point(16, 72), Size = new Size(920, 20), ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5f, FontStyle.Italic) };
            card4.Controls.AddRange(new Control[] { txtJava, bJava, jNote });
            tab.Controls.Add(card4);

            int by = 716;
            // v1.1.7: los 3 botones de abajo miden 240x40, repartidos en
            // el ancho de la tarjeta (14, 370, 726), igual que Donaciones.
            var bSave = Btn("💾  Guardar para este juego", 14, by, 240, 40, Accent);
            bSave.Click += (_, _) => { if (SaveConfigFromUI()) { Library.Save(games); RefreshList(); Log("Configuración guardada para " + (selected?.DisplayName ?? "")); Status("Configuración guardada."); } };
            var bTry = Btn("▶  Probar con esta configuración", 370, by, 240, 40, null);
            bTry.Click += (_, _) => { if (SaveConfigFromUI()) { Library.Save(games); PlaySelected(); } };
            var bReset = Btn("↺  Restablecer", 726, by, 240, 40, Color.FromArgb(100, 116, 139));
            bReset.Click += (_, _) => { if (selected != null) { var fresh = JarInspector.Inspect(selected.FilePath); EmulatorConfig.ApplyKnownFixes(fresh); selected.ResolutionMode = "Auto"; selected.Preset = "240x320"; selected.Phone = "Standard"; selected.Scale = 2; selected.Rotate = 0; selected.Fps = 60; selected.Sound = true; selected.Backlight = "Disabled"; selected.CompatRepaintOnSetCurrent = fresh.CompatRepaintOnSetCurrent; selected.CompatImmediateRepaints = false; selected.M3gHalfRes = false; selected.M3gAntialias = "app"; LoadGameIntoConfig(); Library.Save(games); Log("Configuración restablecida para " + selected.DisplayName); } };
            tab.Controls.AddRange(new Control[] { bSave, bTry, bReset });
            AddBottomSpacer(tab, 766);
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
                    // v1.1.7: todas las teclas del pad miden 120x40 (antes
                    // 120x46) para que el alto sea igual que el resto.
                    var b = Btn(k, 20 + c * 130, 16 + r * 56, 120, 40, null);
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
                // v1.1.7: 130x40, mismo alto que el resto de botones.
                var b = Btn(t, bx, by, 130, 40, null);
                b.Click += (_, _) => SendKeyToGame(s);
                nav.Controls.Add(b);
            }
            tab.Controls.Add(nav);
            AddBottomSpacer(tab, 766);
        }

        // v1.1.3: apartado de donaciones (igual que en GameBoost: PayPal y
        // Buy Me a Coffee de Nous Logic). Voluntarias, no desbloquean nada.
        private void BuildDonationsTab(TabPage tab)
        {
            tab.AutoScroll = true;
            var info = new InfoBox("JavaViva es gratis y lo seguirá siendo.\n\nSi te sirve y quieres apoyar su desarrollo y el de las demás apps de Nous Logic, puedes donar lo que quieras. Las donaciones son voluntarias y no desbloquean funciones: todo JavaViva es gratis.");
            info.Location = new Point(14, 12); info.Size = new Size(960, 110);
            tab.Controls.Add(info);
            var bPaypal = Btn("❤  Donar con PayPal", 14, 140, 240, 40, Color.FromArgb(0, 112, 186));
            bPaypal.Click += (_, _) => OpenUrl("https://paypal.me/engelarm");
            var bBmc = Btn("☕  Donar en Buy Me a Coffee", 370, 140, 240, 40, Color.FromArgb(255, 221, 0), Color.Black);
            bBmc.Click += (_, _) => OpenUrl("https://buymeacoffee.com/engelarm");
            var bWeb = Btn("🌐  nouslogic.xyz/dona-un-cafe", 726, 140, 240, 40, Accent);
            bWeb.Click += (_, _) => OpenUrl("https://nouslogic.xyz/dona-un-cafe");
            tab.Controls.AddRange(new Control[] { bPaypal, bBmc, bWeb });
            var note = new Label { Text = "Se abre tu navegador. Gracias por apoyar a Nous Logic.", Location = new Point(14, 196), Size = new Size(960, 22), ForeColor = TextMuted, Font = new Font("Segoe UI", 9, FontStyle.Italic) };
            tab.Controls.Add(note);
            AddBottomSpacer(tab, 240);
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                Log("Abriendo: " + url);
            }
            catch (Exception ex)
            {
                Log("No se pudo abrir el enlace: " + ex.Message);
                MessageBox.Show("No se pudo abrir el enlace:\n" + url, "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // v1.1.3: espacio vacio al final de cada pantalla para que el
        // contenido no quede pegado al borde inferior de la ventana.
        private void AddBottomSpacer(TabPage tab, int y)
        {
            tab.Controls.Add(new Panel { Location = new Point(14, y), Size = new Size(960, 40), BackColor = Bg });
        }

        private void BuildAboutTab(TabPage tab)
        {
            tab.AutoScroll = true;
            var info = new InfoBox("JavaViva v1.1.7 — Tus juegos Java .jar como en tu celular de los 2000.\n\nMotor: FreeJ2ME-Plus v1.53 (GPLv3), de TASEmulators, David Richardson y Saket Dandawate. JavaViva es solo el launcher con biblioteca y configuracion por juego; el emulador hace el trabajo pesado. Sin JavaViva, el mismo juego se puede abrir con: java -jar engine\\freej2me_plus.jar \"file:///C:\\ruta\\juego.jar\" 0 240 320 2\n\nQue NO hace: no convierte .jar a .apk ni a .exe, no descarga juegos y no garantiza que todos los juegos funcionen. Cada juego fue hecho para un telefono concreto; si uno no arranca, prueba otra resolucion o perfil en Configuracion. Usa solo .jar que tengas legalmente.");
            info.Location = new Point(14, 12); info.Size = new Size(960, 170);
            tab.Controls.Add(info);
            var cred = new Label { Text = "Creado por Nous Logic  •  nouslogic.xyz\nSi te sirve, comparte JavaViva con quien jugaba en Nokia o Sony Ericsson.", Location = new Point(14, 196), Size = new Size(960, 60), ForeColor = TextMuted };
            tab.Controls.Add(cred);
            AddBottomSpacer(tab, 266);
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
                // v1.1.3: texto corto para el encabezado (el largo se cortaba
                // por la derecha). El detalle completo sigue en el registro.
                string shortHow = javaHow.Contains("incluido") ? "Runtime incluido"
                    : javaHow.Contains("Configuracion") ? "Java personalizado"
                    : javaHow.Contains("PATH") ? "Java del sistema"
                    : javaHow.Contains("instalado") ? "Java instalado" : javaHow;
                lblJava.Text = javaExe != null ? "✓ Java listo: " + shortHow : "⚠ Sin Java: instala Adoptium Temurin JRE";
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
            Theme.ApplyDarkScrollbars(lvGames);
            lvGames.Items.Clear();
            // v1.1.5: buscar + solo favoritos; los favoritos van primero.
            var query = txtSearch?.Text?.Trim() ?? "";
            bool favOnly = chkFavOnly?.Checked == true;
            var shown = games
                .Where(g => (!favOnly || g.IsFavorite)
                    && (query.Length == 0
                        || g.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || g.Vendor.Contains(query, StringComparison.OrdinalIgnoreCase)
                        || Path.GetFileName(g.FilePath).Contains(query, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(g => g.IsFavorite)
                .ThenBy(g => g.DisplayName);
            foreach (var g in shown)
            {
                var (w, h) = g.EffectiveResolution();
                var item = new ListViewItem(new[]
                {
                    g.IsFavorite ? "★" : "",
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
            // v1.1.5: si hay filtro activo y no hay seleccion, se toma el
            // primer juego VISIBLE, no el primero de la lista completa.
            selected = lvGames.SelectedItems.Count > 0 ? lvGames.SelectedItems[0].Tag as GameEntry
                : lvGames.Items.Count > 0 ? lvGames.Items[0].Tag as GameEntry : null;
            if (btnPlayLib != null) btnPlayLib.Enabled = selected != null;
            if (btnFav != null) btnFav.Text = selected != null && selected.IsFavorite ? "☆  Quitar favorito" : "★  Marcar favorito";
            SyncLibResCombo();
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

        // v1.1.2: antes "Quitar de la lista" solo borraba la entrada y el
        // juego volvia a aparecer al refrescar (la carpeta Juegos se
        // re-escanea). Ahora pregunta: Si = quitar de la lista Y borrar
        // el archivo .jar y sus datos/guardados; No = solo de la lista
        // (el archivo se queda y al refrescar vuelve a aparecer).
        private void RemoveSelected()
        {
            if (selected == null) return;
            var name = selected.DisplayName;
            var res = MessageBox.Show("¿Qué quieres hacer con " + name + "?\n\n" +
                "Sí = quitar de la lista Y borrar el archivo de la carpeta Juegos (también sus datos y guardados).\n" +
                "No = quitar solo de la lista (el archivo se queda; al refrescar volverá a aparecer).",
                "JavaViva - Eliminar juego", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res == DialogResult.Cancel) return;
            bool deleteFile = res == DialogResult.Yes;
            if (deleteFile)
            {
                try { if (File.Exists(selected.FilePath)) File.Delete(selected.FilePath); }
                catch (Exception ex) { Log("No se pudo borrar el archivo: " + ex.Message); }
                try
                {
                    var dd = Library.DataDir(selected.SafeName);
                    if (Directory.Exists(dd)) Directory.Delete(dd, true);
                }
                catch { }
            }
            games.Remove(selected);
            Library.Save(games);
            RefreshList();
            Log(deleteFile ? "Eliminado (lista y archivo): " + name : "Quitado de la lista: " + name);
        }

        // v1.1.2: vacia el listado guardado (library.json). Los archivos
        // .jar se quedan en la carpeta Juegos: al refrescar volveran a
        // aparecer. Sirve para empezar la lista de cero y que todo se
        // vuelva a detectar.
        private void ClearLibrary()
        {
            if (games.Count == 0)
            {
                MessageBox.Show("La lista ya está vacía.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show("¿Borrar TODOS los juegos de la lista?\n\nLos archivos .jar se quedan en la carpeta Juegos: al pulsar Refrescar volverán a aparecer. Esto solo vacía el listado guardado.",
                "JavaViva - Limpiar lista", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            int n = games.Count;
            games.Clear();
            Library.Save(games);
            RefreshList();
            Log("Lista vaciada (" + n + " juegos). Los .jar siguen en la carpeta Juegos.");
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
            SyncLibResCombo();
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
            SyncLibResCombo();
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
            // v1.1.6: no lanzar otro juego mientras uno corre. Antes se podia:
            // el proceso viejo quedaba vivo y habia dos juegos a la vez.
            try { gameProcess?.Refresh(); } catch { }
            if (gameProcess is { HasExited: false })
            {
                if (MessageBox.Show("Ya hay un juego corriendo" + (string.IsNullOrWhiteSpace(runningGameName) ? "" : " (" + runningGameName + ")") + ".\n\n¿Detenerlo y lanzar este juego?",
                    "JavaViva", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                StopGame(silent: true);
            }

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
                // v1.1.6: el proceso que se lanza NO es el juego: FreeJ2ME-Plus
                // se relanza en otro JVM y este sale enseguida (codigo 0). Si
                // se siguiera a este, el registro diria "El juego se cerro"
                // con el juego en pantalla. Se sigue al hijo (TrackGameChildAsync).
                var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
                proc.OutputDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log("[juego] " + e.Data); };
                proc.ErrorDataReceived += (_, e) => { if (!string.IsNullOrEmpty(e.Data)) Log("[juego] " + e.Data); };
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                launcherProcess = proc;
                gameProcess = proc; // provisional: se corrige al encontrar al hijo
                runningGameName = g.DisplayName;
                g.LastPlayed = DateTime.Now;
                Library.Save(games);
                RefreshList();
                btnStop.Enabled = true;
                Status("Jugando: " + g.DisplayName + " (" + w + "x" + h + "). La ventana del juego se abre aparte.");
                _ = TrackGameChildAsync(proc, g);
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo lanzar el juego:\n" + ex.Message, "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Log("Error al lanzar: " + ex.Message);
            }
        }

        // v1.1.5: marcar/quitar favorito del juego seleccionado. Los
        // favoritos se guardan en library.json y van primeros en la lista.
        private void ToggleFavorite()
        {
            if (selected == null) return;
            var g = selected;
            g.IsFavorite = !g.IsFavorite;
            Library.Save(games);
            RefreshList();
            Log((g.IsFavorite ? "★ Favorito: " : "☆ Ya no es favorito: ") + g.DisplayName);
        }

        // v1.1.5: captura de pantalla de la ventana del juego en marcha.
        // Copia lo que se ve en pantalla (la ventana se trae al frente
        // primero), asi que el juego debe estar visible, no minimizado ni
        // tapado por otra ventana. Se guarda en Capturas\ junto al exe.
        private void CaptureGameScreenshot()
        {
            try { gameProcess?.Refresh(); } catch { }
            if (gameProcess is null or { HasExited: true })
            {
                MessageBox.Show("Primero pulsa ▶ Jugar para abrir el juego.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                gameProcess.Refresh();
                // v1.1.6: esperar a que la ventana exista (como en Teclas),
                // en vez de fallar a la primera si aun no se creo.
                IntPtr h = IntPtr.Zero;
                for (int i = 0; i < 10 && h == IntPtr.Zero; i++)
                {
                    h = gameProcess.MainWindowHandle;
                    if (h == IntPtr.Zero) System.Threading.Thread.Sleep(100);
                }
                if (h == IntPtr.Zero)
                {
                    MessageBox.Show("La ventana del juego aún no está lista. Espera un momento e inténtalo de nuevo.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (IsIconic(h)) ShowWindow(h, 9); // 9 = SW_RESTORE
                SetForegroundWindow(h);
                System.Threading.Thread.Sleep(200);
                if (!GetWindowRect(h, out var r)) throw new Exception("No se pudo leer la ventana del juego.");
                int w = r.Right - r.Left, ht = r.Bottom - r.Top;
                if (w <= 0 || ht <= 0) throw new Exception("La ventana del juego no tiene tamaño visible.");
                var dir = Path.Combine(baseDir, "Capturas");
                Directory.CreateDirectory(dir);
                string name = (selected?.SafeName ?? "juego") + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png";
                string path = Path.Combine(dir, name);
                using (var bmp = new Bitmap(w, ht))
                {
                    using (var g = Graphics.FromImage(bmp))
                        g.CopyFromScreen(r.Left, r.Top, 0, 0, new Size(w, ht));
                    bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                }
                Log("Captura guardada: " + path);
                Status("Captura guardada en Capturas.");
                if (MessageBox.Show("Captura guardada en:\n" + path + "\n\n¿Abrir la carpeta Capturas?", "JavaViva - Captura", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log("No se pudo tomar la captura: " + ex.Message);
                MessageBox.Show("No se pudo tomar la captura:\n" + ex.Message, "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // v1.1.5: respaldo de la biblioteca a un .json (la misma forma que
        // library.json, con favoritos y configuracion por juego incluidos).
        private void ExportLibrary()
        {
            using var d = new SaveFileDialog { Filter = "Respaldo de JavaViva|*.json|Todos los archivos|*.*", Title = "Exportar respaldo de la biblioteca", FileName = "JavaViva-respaldo-" + DateTime.Now.ToString("yyyyMMdd") + ".json" };
            if (d.ShowDialog() != DialogResult.OK) return;
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(games, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(d.FileName, json);
                Log("Respaldo exportado: " + d.FileName + " (" + games.Count + " juego(s)).");
                Status("Respaldo exportado.");
            }
            catch (Exception ex)
            {
                Log("No se pudo exportar el respaldo: " + ex.Message);
                MessageBox.Show("No se pudo exportar el respaldo:\n" + ex.Message, "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportLibrary()
        {
            using var d = new OpenFileDialog { Filter = "Respaldo de JavaViva|*.json|JSON|*.json", Title = "Importar respaldo de la biblioteca" };
            if (d.ShowDialog() != DialogResult.OK) return;
            try
            {
                var json = File.ReadAllText(d.FileName);
                var imported = System.Text.Json.JsonSerializer.Deserialize<List<GameEntry>>(json);
                if (imported == null || imported.Count == 0)
                {
                    MessageBox.Show("El archivo no tiene juegos para importar.", "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                var gamesDir = Library.GamesDir(baseDir);
                int added = 0, updated = 0, skipped = 0;
                foreach (var g in imported)
                {
                    if (string.IsNullOrWhiteSpace(g.FilePath)) { skipped++; continue; }
                    if (!File.Exists(g.FilePath))
                    {
                        // Al cambiar de PC la ruta cambia: si el .jar esta
                        // en la carpeta Juegos actual, se vuelve a encontrar
                        // por su nombre. Si no, se omite la entrada.
                        var local = Path.Combine(gamesDir, Path.GetFileName(g.FilePath));
                        if (File.Exists(local)) g.FilePath = local;
                        else { skipped++; continue; }
                    }
                    bool exists = games.Any(x => string.Equals(x.FilePath, g.FilePath, StringComparison.OrdinalIgnoreCase));
                    Library.Upsert(games, g);
                    if (exists) updated++; else added++;
                }
                Library.Save(games);
                RefreshList();
                Log($"Respaldo importado: {added} juego(s) nuevo(s), {updated} actualizado(s)" + (skipped > 0 ? $", {skipped} omitido(s) (archivo no encontrado)." : "."));
                Status("Respaldo importado.");
            }
            catch (Exception ex)
            {
                Log("No se pudo importar el respaldo: " + ex.Message);
                MessageBox.Show("No se pudo importar el respaldo:\n" + ex.Message, "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // v1.1.6: seguir al proceso real del juego (el hijo del lanzador).
        // Se busca hasta 10 s: si el lanzador sigue vivo 5 s sin hijo, el
        // motor no se relanzo y se queda con el lanzador.
        private async Task TrackGameChildAsync(Process launcher, GameEntry g)
        {
            Process? child = null;
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (DateTime.UtcNow < deadline)
                {
                    child = GameProcessFinder.FindGameChild(launcher, g.DisplayName);
                    if (child != null) break;
                    bool launcherAlive;
                    try { launcher.Refresh(); launcherAlive = !launcher.HasExited; } catch { launcherAlive = false; }
                    if (launcherAlive)
                    {
                        try { if (DateTime.UtcNow > launcher.StartTime.ToUniversalTime().AddSeconds(5)) break; } catch { break; }
                    }
                    await Task.Delay(300);
                }
                if (child == null) child = GameProcessFinder.FindGameChild(launcher, g.DisplayName);
            }
            catch (Exception ex)
            {
                Log("No se pudo seguir al proceso del juego: " + ex.Message);
            }
            var tracked = child ?? launcher;
            BeginInvoke(() =>
            {
                // Si el usuario ya lanzo otro juego o lo detuvo, no tocar nada.
                if (!ReferenceEquals(launcherProcess, launcher)) { try { child?.Dispose(); } catch { } return; }
                gameProcess = tracked;
                AttachExitHandler(tracked);
                btnStop.Enabled = true;
                if (child != null) Log("Juego en marcha (proceso " + child.Id + ").");
                else if (tracked.HasExited) Log("El lanzador salió y no se encontró el proceso del juego; puede que no haya arrancado.");
            });
        }

        private void AttachExitHandler(Process proc)
        {
            try { proc.EnableRaisingEvents = true; } catch { }
            var tracked = proc;
            proc.Exited += (_, _) => BeginInvoke(() =>
            {
                // Ignorar salidas viejas: si ya se sigue otro proceso, este
                // no es el juego actual (antes el manejador leia el campo y
                // decia "se cerro" del juego nuevo al morir el viejo).
                if (!ReferenceEquals(gameProcess, tracked)) return;
                gameProcess = null;
                launcherProcess = null;
                runningGameName = "";
                btnStop.Enabled = false;
                btnPlay.Enabled = true;
                Status("El juego se cerró.");
                Log("El juego se cerró (código " + SafeExitCode(tracked) + ").");
            });
        }

        private string SafeExitCode(Process p)
        {
            try { return p.ExitCode.ToString(); } catch { return "?"; }
        }

        private void StopGame(bool silent = false)
        {
            var proc = gameProcess;
            var launcher = launcherProcess;
            var name = runningGameName;
            gameProcess = null;
            launcherProcess = null;
            runningGameName = "";
            try
            {
                if (proc is { HasExited: false })
                {
                    proc.Kill(entireProcessTree: true);
                    if (!silent) Log("Juego detenido.");
                }
            }
            catch { }
            try
            {
                if (launcher != null && !ReferenceEquals(launcher, proc))
                {
                    // Si el lanzador ya salio y el hijo aun no estaba seguido,
                    // buscarlo por su ventana para matarlo tambien (si no
                    // quedaria huerfano corriendo).
                    var orphan = GameProcessFinder.FindGameChild(launcher, name, requireNamedWindow: true);
                    if (orphan is { HasExited: false }) orphan.Kill(entireProcessTree: true);
                    if (launcher is { HasExited: false }) launcher.Kill(entireProcessTree: true);
                }
            }
            catch { }
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
        private Button Btn(string text, int x, int y, int w, int h, Color? bg = null, Color? fg = null)
        {
            var b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = bg ?? Accent, ForeColor = fg ?? Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold), Cursor = Cursors.Hand };
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
