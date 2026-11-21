using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace JavaViva
{
    /// <summary>Todo lo que FreeJ2ME-Plus v1.53 entiende, verificado contra su
    /// codigo fuente (Config.java) y su documentacion libretro.
    /// - game.conf por juego: &lt;datos&gt;/config/&lt;nombre-app&gt;/game.conf
    ///   con claves scrwidth, scrheight, phone, rotate, fps, sound... y las
    ///   de compatibilidad compatrepaintonsetcurrent / compatimmediaterepaints.
    ///   Por defecto el emulador las pone en "off": por eso NFS Most Wanted se
    ///   queda en pantalla blanca si no se activa compatrepaintonsetcurrent.
    /// - La resolucion del game.conf manda sobre la de la linea de comandos.
    /// - Resoluciones validas (libretro): 96x65 ... 240x320 (defecto) ... 854x480.</summary>
    public static class EmulatorConfig
    {
        public static readonly string EngineVersion = "FreeJ2ME-Plus v1.53";

        public static readonly string[] Resolutions = new[]
        {
            "96x65", "128x128", "128x160", "160x128", "176x208", "176x220",
            "208x208", "220x176", "240x240", "240x320", "320x240", "240x400",
            "320x320", "352x416", "360x360", "360x640", "640x360", "480x640",
            "640x480", "480x800", "800x480", "480x854", "854x480",
        };

        /// <summary>Perfiles de telefono. Display es lo que ve el usuario;
        /// Value es lo que se escribe en game.conf (phone=...). El motor solo
        /// reconoce los valores internos de FreeJ2ME-Plus v1.53 (verificado
        /// en el .jar: FreeJ2ME.class contiene Standard, LG, Motorola,
        /// MotoTriplets, MotoV8, MotoA1000, NokiaKeyboard, Sagem, Siemens,
        /// SKT y KDDI). Los nombres "Nokia", "Motorola V8", "Motorola A1000"
        /// y "Nokia Full Keyboard" son solo etiquetas de su GUI: si se
        /// escriben tal cual en game.conf, el motor no los reconoce y se
        /// comporta como Standard sin avisar. Por eso aqui van separados.</summary>
        public static readonly (string Display, string Value, string Hint)[] Phones = new[]
        {
            ("Standard", "Standard", "El de siempre. Vale para casi todos, incluido NFS Most Wanted."),
            ("Nokia (S40/S60)", "NokiaKeyboard", "Teclas Nokia S40/S60. Para juegos hechos para Nokia."),
            ("Nokia Full Keyboard", "NokiaKeyboard", "Nokia con teclado completo."),
            ("Siemens", "Siemens", "Para juegos hechos para Siemens."),
            ("MotoTriplets", "MotoTriplets", "Motorola triplets (antes Sharp)."),
            ("Motorola", "Motorola", "Motorola generico."),
            ("Motorola V8", "MotoV8", "Motorola V8."),
            ("Motorola A1000", "MotoA1000", "Motorola A1000."),
            ("LG", "LG", "LG."),
            ("Sagem", "Sagem", "Sagem."),
            ("SKT", "SKT", "SKT (Corea)."),
            ("KDDI", "KDDI", "KDDI (Japon)."),
        };

        /// <summary>Convierte un valor guardado (etiqueta vieja o valor
        /// interno) al valor interno que el motor reconoce. Las bibliotecas
        /// guardadas en v1.0/v1.0.1 tienen "Nokia", "Motorola V8", etc.</summary>
        public static string NormalizePhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "Standard";
            var p = phone.Trim();
            // Etiqueta vieja de v1.0/v1.0.1: "Nokia" a secas no esta en la
            // lista nueva (ahora es "Nokia (S40/S60)"), pero las bibliotecas
            // guardadas la tienen; sin este mapa caeria a Standard.
            if (string.Equals(p, "Nokia", StringComparison.OrdinalIgnoreCase))
                return "NokiaKeyboard";
            foreach (var (display, value, _) in Phones)
                if (string.Equals(p, display, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(p, value, StringComparison.OrdinalIgnoreCase))
                    return value;
            return "Standard";
        }

        public static string DisplayForPhone(string? phone)
        {
            var v = NormalizePhone(phone);
            // "Nokia Full Keyboard" y "Nokia (S40/S60)" comparten valor;
            // se muestra la primera etiqueta con ese valor.
            foreach (var (display, value, _) in Phones)
                if (string.Equals(v, value, StringComparison.OrdinalIgnoreCase))
                    return display;
            return "Standard";
        }

        public static string ValueForPhone(string? display) => NormalizePhone(display);

        public static string HintForPhone(string? phone)
        {
            var v = NormalizePhone(phone);
            foreach (var (_, value, hint) in Phones)
                if (string.Equals(v, value, StringComparison.OrdinalIgnoreCase))
                    return hint;
            return "";
        }

        public static readonly string[] Backlights = new[] { "Disabled", "Green", "Cyan", "Orange", "Violet", "Red" };

        public static (int w, int h) ParseResolution(string s)
        {
            var p = (s ?? "240x320").ToLowerInvariant().Split('x');
            if (p.Length == 2 && int.TryParse(p[0], out int w) && int.TryParse(p[1], out int h) && w > 0 && h > 0)
                return (w, h);
            return (240, 320);
        }

        /// <summary>La resolucion de preset mas cercana a un tamano de imagen.</summary>
        public static string NearestPreset(int w, int h)
        {
            string best = "240x320";
            double bestScore = double.MaxValue;
            foreach (var r in Resolutions)
            {
                var (rw, rh) = ParseResolution(r);
                double score = Math.Abs(rw - w) + Math.Abs(rh - h);
                // Un 240x323 es un 240x320 con un pixel de mas: no es otra resolucion.
                if (score < bestScore) { bestScore = score; best = r; }
            }
            return best;
        }

        /// <summary>Arreglos conocidos por juego. NFS Most Wanted necesita
        /// compatrepaintonsetcurrent=on o se queda en pantalla blanca.</summary>
        public static void ApplyKnownFixes(GameEntry g)
        {
            var n = (g.Name + " " + Path.GetFileName(g.FilePath)).ToLowerInvariant();
            if (n.Contains("nfsmw") || n.Contains("most wanted") || n.Contains("mostwanted"))
                g.CompatRepaintOnSetCurrent = true;
        }

        /// <summary>Escribe el game.conf que FreeJ2ME-Plus lee al arrancar.
        /// Se escribe en la carpeta de datos del juego y tambien para el nombre
        /// del archivo .jar, porque el emulador usa el nombre de la app (MIDlet-Name)
        /// y no siempre coincide con el del archivo.</summary>
        public static void WriteGameConf(string dataDir, GameEntry g)
        {
            var (w, h) = g.EffectiveResolution();
            var lines = new List<string>
            {
                "scrwidth=" + w,
                "scrheight=" + h,
                "phone=" + NormalizePhone(g.Phone),
                "backlightcolor=" + g.Backlight,
                "rotate=" + g.Rotate,
                "fps=" + (g.Fps <= 0 ? "60" : g.Fps.ToString()),
                "fontoffset=0",
                "spdhacknoalpha=off",
                "compatfantasyzonefix=off",
                "compattranstooriginonreset=off",
                "compatimmediaterepaints=" + OnOff(g.CompatImmediateRepaints),
                "compatrepaintonsetcurrent=" + OnOff(g.CompatRepaintOnSetCurrent),
                "compatoverrideplatchecks=" + OnOff(g.CompatOverridePlatChecks),
                "compatsiemensfriendlydrawing=off",
                "compatignorevolumechanges=off",
                "compatnotranslatedrawrgb=off",
                "compatmcv3horizfovfix=off",
                "fpshack=Disabled",
                "spdhackm3ghalfres=" + OnOff(g.M3gHalfRes),
                "m3gantialiasmode=" + g.M3gAntialias,
                "m3gbilinearmode=app",
                "m3gditheringmode=app",
                "m3gperspcorrmode=app",
                "m3gmipmapmode=app",
                "m3gperspcorrsubfactor=high",
                "m3gdisablefog=off",
                "spdhackmcv3halfres=off",
                "spdhackmcv3nolighting=off",
                "dojaversion=200",
            };
            // v1.0.2: el motor busca el game.conf en
            //   <datos>/config/<MIDlet-Name tal cual>/game.conf
            // (Config.java: "./config/" + nombre + "/game.conf"). Antes solo
            // se escribia en SafeName (espacios -> '_') y en el nombre del
            // archivo, asi que para "Age Of Empires III Mobile" o el Carbon
            // el motor ignoraba TODO el game.conf en silencio (perfil,
            // rotacion, FPS, compatibilidad y M3G). Ahora se escribe el
            // mismo archivo en todas las carpetas plausibles: el MIDlet-Name
            // crudo, el nombre visible, el SafeName y el nombre del .jar.
            // Escribir copias de mas no hace dano; que falte la buena, si.
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in new[] { g.Name, g.DisplayName, g.SafeName, Path.GetFileNameWithoutExtension(g.FilePath) })
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var clean = new string(raw.Trim().Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();
                if (!string.IsNullOrWhiteSpace(clean)) names.Add(clean);
            }
            foreach (var name in names)
            {
                var dir = Path.Combine(dataDir, "config", name);
                Directory.CreateDirectory(dir);
                File.WriteAllLines(Path.Combine(dir, "game.conf"), lines, new UTF8Encoding(false));
            }
            // Ajustes de sistema (sonido): freej2me_system/freej2me.conf.
            // v1.0.2: antes solo se escribia la primera vez (if !Exists), asi
            // que cambiar Sonido despues no tenia efecto. Ahora se actualiza
            // la linea sound= en cada lanzamiento, sin tocar los demas ajustes
            // del sistema que el usuario haya cambiado desde el emulador.
            var sysDir = Path.Combine(dataDir, "freej2me_system");
            Directory.CreateDirectory(sysDir);
            var sysPath = Path.Combine(sysDir, "freej2me.conf");
            var sysDefaults = new[]
            {
                "fpsCounterPosition=Off",
                "logLevel=2",
                "numsynths=4",
                "M3GWireframe=off",
                "M3GUntextured=off",
                "MCV3ShowTimeMetrics=off",
                "MCV3ShowHeapUsage=off",
                "deleteTempKJXFiles=on",
                "dumpAudioStreams=off",
                "dumpGraphicsObjects=off",
                "sound=" + OnOff(g.Sound),
                "soundfont=Default",
                "textfont=Default",
            };
            if (!File.Exists(sysPath))
            {
                File.WriteAllLines(sysPath, sysDefaults, new UTF8Encoding(false));
            }
            else
            {
                var sysLines = File.ReadAllLines(sysPath).ToList();
                var idx = sysLines.FindIndex(l => l.TrimStart().StartsWith("sound=", StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) sysLines[idx] = "sound=" + OnOff(g.Sound);
                else sysLines.Add("sound=" + OnOff(g.Sound));
                File.WriteAllLines(sysPath, sysLines, new UTF8Encoding(false));
            }
        }

        private static string OnOff(bool b) => b ? "on" : "off";

        /// <summary>Busca un Java para lanzar el emulador, en este orden:
        /// 1) El runtime incluido en la carpeta del programa (no pide instalar nada).
        /// 2) El Java del sistema (PATH).
        /// 3) Adoptium Temurin en Program Files.
        /// 4) Uno elegido a mano en Configuracion.</summary>
        public static (string? javaExe, string how) FindJava(string baseDir, string customJava)
        {
            var bundled = Path.Combine(baseDir, "runtime", "windows-x64", "bin", "java.exe");
            if (File.Exists(bundled)) return (bundled, "Runtime incluido con JavaViva (FreeJ2ME-Plus v1.53)");
            var bundledW = Path.Combine(baseDir, "runtime", "windows-x64", "bin", "javaw.exe");
            if (File.Exists(bundledW)) return (bundledW, "Runtime incluido con JavaViva (javaw)");
            if (!string.IsNullOrWhiteSpace(customJava) && File.Exists(customJava))
                return (customJava, "Java elegido en Configuracion");
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                var cand = Path.Combine(dir.Trim('"'), "java.exe");
                if (File.Exists(cand)) return (cand, "Java del sistema (PATH)");
            }
            foreach (var root in new[]
            {
                @"C:\Program Files\Eclipse Adoptium",
                @"C:\Program Files (x86)\Eclipse Adoptium",
                @"C:\Program Files\Java",
                @"C:\Program Files (x86)\Java",
            })
            {
                try
                {
                    if (!Directory.Exists(root)) continue;
                    var hit = Directory.GetFiles(root, "java.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (hit != null) return (hit, "Java instalado (" + hit + ")");
                }
                catch { }
            }
            return (null, "No se encontro Java");
        }

        public static string EngineJar(string baseDir) =>
            Path.Combine(baseDir, "engine", "freej2me_plus.jar");

        /// <summary>Linea de comandos del emulador. En Windows la ruta del .jar
        /// va como file:///C:\ruta\juego.jar (con la barra extra), segun el README
        /// de FreeJ2ME-Plus. La resolucion tambien va aqui, aunque el game.conf
        /// manda si existe.
        /// v1.0.1: el formato correcto de FreeJ2ME-Plus v1.53 es
        ///   java -jar freej2me_plus.jar &lt;juego&gt; &lt;fullscreen 0/1&gt; &lt;ancho&gt; &lt;alto&gt; &lt;escala&gt;
        /// (constructor de FreeJ2ME.java: args[0]=jar, args[1]=fullscreen,
        /// args[2]=ancho, args[3]=alto, args[4]=escala; tras cargar,
        /// scrwidth=args[argLen-2], scrheight=args[argLen-1]).
        /// Antes faltaba el "0" de fullscreen y la LCD quedaba en 220x2
        /// (pantalla negra, ventana enana) — el mismo bug que en la web.</summary>
        public static string BuildArguments(string engineJar, string gamePath, int w, int h, int scale)
        {
            var fileUrl = "file:///" + gamePath.Replace('/', '\\');
            return "-jar \"" + engineJar + "\" \"" + fileUrl + "\" 0 " + w + " " + h + " " + scale;
        }

        public static ProcessStartInfo BuildStart(string javaExe, string baseDir, GameEntry g, string dataDir)
        {
            var (w, h) = g.EffectiveResolution();
            return new ProcessStartInfo
            {
                FileName = javaExe,
                Arguments = BuildArguments(EngineJar(baseDir), g.FilePath, w, h, g.Scale),
                WorkingDirectory = dataDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
        }
    }
}
