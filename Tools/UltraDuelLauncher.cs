using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

// Keep the existing Unity runtime path so older installations and shortcuts work.
internal static class UltraDuelLauncher
{
    [STAThread]
    static void Main()
    {
        string folder = AppDomain.CurrentDomain.BaseDirectory;
        string game = Path.Combine(folder, "Matteo-Duel.exe");
        if (!File.Exists(game))
        {
            MessageBox.Show("Keep Ultra-Duel.exe in the installed game folder.", "Ultra Duel");
            return;
        }
        string command = Environment.CommandLine.TrimStart();
        int end = command.StartsWith("\"") ? command.IndexOf('"', 1) + 1 : command.IndexOf(' ');
        string arguments = end > 0 ? command.Substring(end).TrimStart() : "";
        Process.Start(new ProcessStartInfo(game, arguments) { WorkingDirectory = folder });
    }
}
