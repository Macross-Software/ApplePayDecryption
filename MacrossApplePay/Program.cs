using System;
using System.Windows.Forms;

namespace Macross
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using MainForm MainForm = new MainForm();
            Application.Run(MainForm);
        }
    }
}
