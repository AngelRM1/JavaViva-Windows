using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace JavaViva
{
    /// <summary>Un juego .jar/.jad de la biblioteca, con su configuracion por juego.</summary>
    public class GameEntry
    {
        public string FilePath { get; set; } = "";
        public string Name { get; set; } = "";
        public string Vendor { get; set; } = "";
        public string Version { get; set; } = "";
        public string Profile { get; set; } = "";      // MIDP-2.0, CLDC-1.1...
        public long SizeBytes { get; set; }
        public string AutoResolution { get; set; } = ""; // detectada del .jar, ej. "240x320"
        public string AutoHow { get; set; } = "";        // como se detecto
        public DateTime? LastPlayed { get; set; }

        // Configuracion por juego (la del panel Configuracion)
        public string ResolutionMode { get; set; } = "Auto"; // Auto | Preset | Custom
        public string Preset { get; set; } = "240x320";
        public int CustomWidth { get; set; } = 240;
        public int CustomHeight { get; set; } = 320;
        public int Scale { get; set; } = 2;
        public string Phone { get; set; } = "Standard";
        public int Rotate { get; set; } = 0;
        public int Fps { get; set; } = 60;               // 0 = Auto
        public bool Sound { get; set; } = true;
        public string Backlight { get; set; } = "Disabled";
        public bool CompatRepaintOnSetCurrent { get; set; } = false;
        public bool CompatImmediateRepaints { get; set; } = false;
        public bool CompatOverridePlatChecks { get; set; } = true;
        public bool M3gHalfRes { get; set; } = false;
        public string M3gAntialias { get; set; } = "app";

        public string DisplayName => string.IsNullOrWhiteSpace(Name)
            ? Path.GetFileNameWithoutExtension(FilePath) : Name;

        // Validacion del .jar (v1.0.2): un .jar corrupto o sin manifiesto se
        // agregaba en silencio y al lanzar solo salia "El juego se cerro".
        // IsValid=false con el motivo en ValidationError para avisar al
        // agregar y al lanzar, en vez de dejarlo morir en silencio.
        public bool IsValid { get; set; } = true;
        public string ValidationError { get; set; } = "";

        public string SafeName
        {
            get
            {
                var s = new string(DisplayName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ').ToArray());
                s = s.Trim().Replace(' ', '_');
                return string.IsNullOrWhiteSpace(s) ? "juego" : s;
            }
        }

        public (int w, int h) EffectiveResolution()
        {
            if (ResolutionMode == "Custom") return (CustomWidth, CustomHeight);
            if (ResolutionMode == "Preset") return EmulatorConfig.ParseResolution(Preset);
            // Auto: lo detectado del .jar, si no 240x320 (el mas comun)
            if (!string.IsNullOrWhiteSpace(AutoResolution))
                return EmulatorConfig.ParseResolution(AutoResolution);
            return (240, 320);
        }
    }

    /// <summary>Lee el manifiesto y los PNG de un .jar para saber que juego es
    /// y a que resolucion fue hecho. Sin el manifiesto no hay instalacion real
    /// en un telefono Java: el telefono leia justo estos datos.</summary>
    public static class JarInspector
    {
        public static GameEntry Inspect(string path)
        {
            var g = new GameEntry { FilePath = Path.GetFullPath(path) };
            var fi = new FileInfo(path);
            g.SizeBytes = fi.Exists ? fi.Length : 0;
            if (string.IsNullOrWhiteSpace(g.Name))
                g.Name = Path.GetFileNameWithoutExtension(path);

            try
            {
                if (path.EndsWith(".jad", StringComparison.OrdinalIgnoreCase))
                {
                    ReadJad(path, g);
                    // Si el .jad apunta a un .jar hermano, inspecciona ese tambien.
                    var jar = FindJarForJad(path);
                    if (jar != null) ReadJar(jar, g);
                    else { g.IsValid = false; g.ValidationError = "El .jad no tiene su .jar hermano en la misma carpeta, asi que no se puede lanzar."; }
                    return g;
                }
                ReadJar(path, g);
            }
            catch (Exception ex)
            {
                // .jar danado o sin manifiesto: se queda el nombre del archivo,
                // pero marcado como invalido para avisar en vez de agregarlo en silencio.
                g.IsValid = false;
                g.ValidationError = "No se pudo leer como .jar valido: " + ex.Message;
            }
            return g;
        }

        private static void ReadJad(string path, GameEntry g)
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var kv = SplitAttr(line);
                if (kv == null) continue;
                ApplyAttr(g, kv.Value.k, kv.Value.v);
            }
        }

        private static string? FindJarForJad(string jadPath)
        {
            var dir = Path.GetDirectoryName(jadPath) ?? "";
            var baseName = Path.GetFileNameWithoutExtension(jadPath);
            var cand = Path.Combine(dir, baseName + ".jar");
            if (File.Exists(cand)) return cand;
            var any = Directory.GetFiles(dir, "*.jar");
            return any.Length == 1 ? any[0] : null;
        }

        private static void ReadJar(string path, GameEntry g)
        {
            using var zip = ZipFile.OpenRead(path);
            var manifest = zip.GetEntry("META-INF/MANIFEST.MF");
            if (manifest == null)
            {
                g.IsValid = false;
                g.ValidationError = "El .jar no tiene META-INF/MANIFEST.MF: no es un juego J2ME valido.";
                return;
            }
            bool hasMidlet = false;
            using (var r = new StreamReader(manifest.Open(), Encoding.UTF8))
            {
                // El manifiesto puede partir lineas largas con un espacio al inicio.
                var sb = new StringBuilder();
                string? line;
                while ((line = r.ReadLine()) != null)
                {
                    if (line.StartsWith(" ") && sb.Length > 0) { sb.Append(line.Substring(1)); continue; }
                    if (sb.Length > 0 && ApplyManifestLine(g, sb.ToString())) hasMidlet = true;
                    sb.Clear(); sb.Append(line);
                }
                if (sb.Length > 0 && ApplyManifestLine(g, sb.ToString())) hasMidlet = true;
            }
            if (!hasMidlet)
            {
                g.IsValid = false;
                g.ValidationError = "El manifiesto no declara ningun MIDlet (falta MIDlet-Name o MIDlet-1): no es un juego J2ME valido.";
                return;
            }
            DetectResolution(zip, g);
        }

        /// <summary>Devuelve true si la linea declara un MIDlet.</summary>
        private static bool ApplyManifestLine(GameEntry g, string line)
        {
            var kv = SplitAttr(line);
            if (kv == null) return false;
            return ApplyAttr(g, kv.Value.k, kv.Value.v);
        }

        private static (string k, string v)? SplitAttr(string line)
        {
            var i = line.IndexOf(':');
            if (i <= 0) return null;
            return (line.Substring(0, i).Trim(), line.Substring(i + 1).Trim());
        }

        private static bool ApplyAttr(GameEntry g, string k, string v)
        {
            switch (k)
            {
                case "MIDlet-Name": if (!string.IsNullOrWhiteSpace(v)) g.Name = v; return true;
                case "MIDlet-1":
                    // "Nombre, icono, clase": el nombre es el primer campo.
                    var first = v.Split(',')[0].Trim();
                    if (!string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(g.Name)) g.Name = first;
                    return true;
                case "MIDlet-Vendor": g.Vendor = v; break;
                case "MIDlet-Version": g.Version = v; break;
                case "MicroEdition-Profile": g.Profile = v; break;
                case "MicroEdition-Configuration":
                    g.Profile = string.IsNullOrWhiteSpace(g.Profile) ? v : g.Profile + " / " + v; break;
            }
            return false;
        }

        /// <summary>Detecta la resolucion para la que se hizo el juego.
        /// 1) ifone.png / glu.png: su tamano ES la resolucion del juego.
        /// 2) Si no, el PNG mas grande del .jar (fondo o pantalla de titulo).
        /// Es el mismo truco que usan las guias de preservacion J2ME.</summary>
        private static void DetectResolution(ZipArchive zip, GameEntry g)
        {
            var pngs = zip.Entries.Where(e => e.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)).ToList();
            if (pngs.Count == 0) return;

            foreach (var probe in new[] { "ifone.png", "glu.png" })
            {
                var hit = pngs.FirstOrDefault(e => e.Name.Equals(probe, StringComparison.OrdinalIgnoreCase));
                if (hit != null)
                {
                    var s = ReadPngSize(hit);
                    if (s != null)
                    {
                        g.AutoResolution = $"{s.Value.w}x{s.Value.h}";
                        g.AutoHow = $"Detectada por {probe} ({g.AutoResolution})";
                        return;
                    }
                }
            }

            (int w, int h, string name)? best = null;
            foreach (var e in pngs)
            {
                var s = ReadPngSize(e);
                if (s == null) continue;
                if (best == null || s.Value.w * s.Value.h > best.Value.w * best.Value.h)
                    best = (s.Value.w, s.Value.h, e.FullName);
            }
            if (best != null)
            {
                var mapped = EmulatorConfig.NearestPreset(best.Value.w, best.Value.h);
                g.AutoResolution = mapped;
                g.AutoHow = $"Imagen mas grande del .jar: {best.Value.name} ({best.Value.w}x{best.Value.h}) -> {mapped}";
            }
        }

        /// <summary>Lee ancho/alto de un PNG por su cabecera IHDR, sin decodificar la imagen.</summary>
        private static (int w, int h)? ReadPngSize(ZipArchiveEntry e)
        {
            try
            {
                using var s = e.Open();
                var buf = new byte[33];
                int read = 0;
                while (read < buf.Length)
                {
                    int n = s.Read(buf, read, buf.Length - read);
                    if (n <= 0) break;
                    read += n;
                }
                if (read < 24) return null;
                if (buf[0] != 0x89 || buf[1] != (byte)'P' || buf[2] != (byte)'N' || buf[3] != (byte)'G') return null;
                int w = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
                int h = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
                if (w <= 0 || h <= 0 || w > 2000 || h > 2000) return null;
                return (w, h);
            }
            catch { return null; }
        }
    }

    /// <summary>Biblioteca persistente en %AppData%\JavaViva\library.json.</summary>
    public static class Library
    {
        public static string AppDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JavaViva");
        public static string LibraryPath => Path.Combine(AppDir, "library.json");
        public static string GamesDir(string baseDir) => Path.Combine(baseDir, "Juegos");
        public static string DataDir(string gameSafeName) => Path.Combine(AppDir, "datos", gameSafeName);

        public static List<GameEntry> Load()
        {
            try
            {
                if (!File.Exists(LibraryPath)) return new List<GameEntry>();
                var json = File.ReadAllText(LibraryPath);
                return JsonSerializer.Deserialize<List<GameEntry>>(json) ?? new List<GameEntry>();
            }
            catch
            {
                // v1.0.2: antes un library.json corrupto se vaciaba en silencio
                // y se perdia la biblioteca. Ahora se guarda una copia para
                // no perderla y se empieza con lista vacia.
                try
                {
                    if (File.Exists(LibraryPath))
                        File.Copy(LibraryPath, Path.Combine(AppDir,
                            "library.corrupto-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json"), true);
                }
                catch { }
                return new List<GameEntry>();
            }
        }

        public static void Save(List<GameEntry> games)
        {
            Directory.CreateDirectory(AppDir);
            var json = JsonSerializer.Serialize(games, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(LibraryPath, json);
        }

        public static GameEntry Upsert(List<GameEntry> games, GameEntry g)
        {
            var ex = games.FirstOrDefault(x =>
                string.Equals(x.FilePath, g.FilePath, StringComparison.OrdinalIgnoreCase));
            if (ex != null)
            {
                // Re-inspeccion: actualiza datos del .jar, conserva la config del usuario.
                ex.Name = g.Name; ex.Vendor = g.Vendor; ex.Version = g.Version;
                ex.Profile = g.Profile; ex.SizeBytes = g.SizeBytes;
                ex.AutoResolution = g.AutoResolution; ex.AutoHow = g.AutoHow;
                ex.IsValid = g.IsValid; ex.ValidationError = g.ValidationError;
                return ex;
            }
            games.Add(g);
            return g;
        }
    }
}
