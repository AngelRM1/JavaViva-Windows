using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace JavaViva
{
    /// <summary>v1.1.6: buscar el proceso real del juego. FreeJ2ME-Plus se
    /// relanza en otro JVM al arrancar: el java.exe que lanzamos sale
    /// enseguida (codigo 0) y el juego corre en un java.exe hijo. El hijo
    /// se busca por fecha de arranque (nacio con el lanzador) y por tener
    /// ventana; si no hay ventana todavia, el mas reciente. Con
    /// requireNamedWindow solo vale un proceso cuya ventana mencione el
    /// juego o FreeJ2ME (para no matar un java ajeno al detener).</summary>
    public static class GameProcessFinder
    {
        public static Process? FindGameChild(Process launcher, string gameName, bool requireNamedWindow = false)
        {
            DateTime since;
            try { since = launcher.StartTime.AddSeconds(-2); } catch { since = DateTime.Now.AddSeconds(-15); }
            var cands = new List<Process>();
            foreach (var name in new[] { "java", "javaw" })
            {
                Process[] list;
                try { list = Process.GetProcessesByName(name); } catch { continue; }
                foreach (var p in list)
                {
                    try
                    {
                        if (p.Id == launcher.Id) { p.Dispose(); continue; }
                        if (p.StartTime < since) { p.Dispose(); continue; }
                        cands.Add(p);
                    }
                    catch { try { p.Dispose(); } catch { } }
                }
            }
            if (cands.Count == 0) return null;
            Process? namedWindow = null, anyWindow = null, newest = null;
            foreach (var p in cands)
            {
                try
                {
                    if (newest == null || p.StartTime > newest.StartTime) newest = p;
                    if (p.MainWindowHandle != IntPtr.Zero)
                    {
                        string title = "";
                        try { title = p.MainWindowTitle ?? ""; } catch { }
                        bool named = !string.IsNullOrWhiteSpace(gameName)
                            && (title.Contains(gameName, StringComparison.OrdinalIgnoreCase)
                                || title.Contains("FreeJ2ME", StringComparison.OrdinalIgnoreCase));
                        if (named) { if (namedWindow == null) namedWindow = p; }
                        else if (anyWindow == null) anyWindow = p;
                    }
                }
                catch { }
            }
            var best = namedWindow ?? anyWindow ?? (requireNamedWindow ? null : newest);
            foreach (var p in cands) if (!ReferenceEquals(p, best)) p.Dispose();
            return best;
        }
    }
}
