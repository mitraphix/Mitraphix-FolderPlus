using System;
using System.IO;
using System.Windows.Forms;

namespace FolderPlus
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // Force working directory lock for stability
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                
                // Route to appropriate UI based on context menu clicked
                bool isNewPlus = args.Length > 0 && args[0].ToLower() == "-newplus";
                Application.Run(new Form1(isNewPlus));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Core System Error:\n" + ex.Message, "Mitraphix Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}