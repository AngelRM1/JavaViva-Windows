using System;
using System.Threading;
using System.Windows.Forms;

namespace JavaViva
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) =>
            {
                MessageBox.Show("Error no controlado:\n" + e.Exception.Message,
                    "JavaViva", MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            Application.Run(new MainForm());
        }
    }
}
