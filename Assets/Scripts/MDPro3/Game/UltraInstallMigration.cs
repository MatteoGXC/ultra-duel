using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace MDPro3
{
    // Shared by the game, installer and updater; deliberately compatible with .NET Framework/C# 5.
    public sealed class UltraInstallMigration
    {
        public const string Executable = "Ultra-Duel.exe";
        public const string DataDirectory = "Ultra-Duel_Data";
        // Historical names are migration inputs only, never new output names.
        public static readonly string[] LegacyExecutables = { "Matteo-Duel.exe", "MDPro3-TCG-Genesys.exe" };
        public static readonly string[] LegacyDataDirectories = { "Matteo-Duel_Data", "MDPro3-TCG-Genesys_Data" };
        public static readonly string[] ObsoleteTools = { "Update-Matteo-Duel.cmd", "Update-Matteo-Duel.ps1" };
        readonly string root;
        readonly string backup;
        readonly List<string[]> moves = new List<string[]>();
        readonly List<string> created = new List<string>();
        readonly Dictionary<string, string> replaced = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string BackupDirectory { get { return backup; } }

        public UltraInstallMigration(string directory)
        {
            root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            backup = Inside(".ultra-migration-backups/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N"));
        }

        string Inside(string relative)
        {
            string path = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Migration path escapes the game installation.");
            for (string parent = Path.GetDirectoryName(path); parent != root && parent != null; parent = Path.GetDirectoryName(parent))
                if (Directory.Exists(parent)) NoLink(parent);
            return path;
        }

        static void NoLink(string path)
        {
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Migration will not follow a linked runtime path: " + path);
        }

        public static bool IsInstallation(string directory)
        {
            return File.Exists(Path.Combine(directory, Executable)) || LegacyExecutables.Any(n => File.Exists(Path.Combine(directory, n)));
        }

        public static string DefaultInstallationDirectory(string applicationData)
        {
            string current = Path.Combine(applicationData, "UltraDuel");
            if (IsInstallation(current)) return current;
            foreach (string old in new[] { "MatteoDuel", "Matteo-Duel", "Matteo Duel" })
            {
                string candidate = Path.Combine(applicationData, old);
                if (IsInstallation(candidate)) return candidate;
            }
            return current;
        }

        public static void AssertNotRunning(string directory)
        {
            string prefix = Path.GetFullPath(directory).TrimEnd('\\', '/') + Path.DirectorySeparatorChar;
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    string path = null;
                    try { path = process.MainModule.FileName; } catch { }
                    if (path != null && path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        throw new IOException("Close Ultra Duel before updating. Your running game has not been changed.");
                }
            }
        }

        public static string CanonicalArchivePath(string name)
        {
            name = name.Replace('\\', '/');
            if (ObsoleteTools.Any(n => string.Equals(name, n, StringComparison.OrdinalIgnoreCase))) return null;
            foreach (string old in LegacyExecutables)
                if (string.Equals(name, old, StringComparison.OrdinalIgnoreCase)) return Executable;
            foreach (string old in LegacyDataDirectories)
            {
                if (string.Equals(name, old, StringComparison.OrdinalIgnoreCase)) return DataDirectory;
                if (name.StartsWith(old + "/", StringComparison.OrdinalIgnoreCase)) return DataDirectory + name.Substring(old.Length);
            }
            return name;
        }

        void Journal(string operation, string from, string to)
        {
            Directory.CreateDirectory(backup);
            File.AppendAllText(Path.Combine(backup, "migration.log"), operation + "\t" +
                Convert.ToBase64String(Encoding.UTF8.GetBytes(from)) + "\t" +
                Convert.ToBase64String(Encoding.UTF8.GetBytes(to)) + Environment.NewLine);
        }

        void Move(string from, string to)
        {
            // Both absolute targets must remain inside this explicitly selected installation.
            from = Inside(from.Substring(root.Length + 1));
            to = Inside(to.Substring(root.Length + 1));
            NoLink(from);
            Directory.CreateDirectory(Path.GetDirectoryName(to));
            Journal("MOVE", from, to);
            if (Directory.Exists(from)) Directory.Move(from, to); else File.Move(from, to);
            moves.Add(new[] { from, to });
        }

        void MergeMissing(string source, string destination)
        {
            NoLink(source);
            if (Directory.Exists(destination)) NoLink(destination);
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                NoLink(file);
                string target = Path.Combine(destination, Path.GetFileName(file));
                if (File.Exists(target)) continue; // Updated game files always win.
                string temporary=target+".ultra-copy-"+Guid.NewGuid().ToString("N");
                try { File.Copy(file,temporary);File.Move(temporary,target);created.Add(target); }
                finally { if(File.Exists(temporary))File.Delete(temporary); }
            }
            foreach (string child in Directory.GetDirectories(source))
                MergeMissing(child, Path.Combine(destination, Path.GetFileName(child)));
        }

        public void Prepare()
        {
            foreach (string old in LegacyDataDirectories)
            {
                string source = Inside(old), destination = Inside(DataDirectory);
                if (!Directory.Exists(source)) continue;
                if (!Directory.Exists(destination)) Move(source, destination);
                else MergeMissing(source, destination);
            }
        }

        public string PrepareReplacement(string relative)
        {
            string target = Inside(relative);
            if (!replaced.ContainsKey(target))
            {
                string original = null;
                if (File.Exists(target))
                {
                    NoLink(target);
                    original = Path.Combine(backup, "replaced", target.Substring(root.Length + 1));
                    Directory.CreateDirectory(Path.GetDirectoryName(original));
                    File.Copy(target, original);
                }
                else created.Add(target);
                replaced.Add(target, original);
            }
            return target;
        }

        public static void ValidateRuntime(string directory)
        {
            if(!IsCompiledUnityPlayer(directory))throw new IOException("Ultra-Duel.exe must be the compiled Unity player.");
            foreach (string relative in new[] { Executable, "UnityPlayer.dll",
                DataDirectory + "/Managed/Assembly-CSharp.dll", DataDirectory + "/globalgamemanagers",
                DataDirectory + "/Plugins/x86_64/ocgcore.dll", DataDirectory + "/Plugins/x86_64/ygoserver.dll" })
                if (!File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))))
                    throw new IOException("Ultra Duel runtime is incomplete: " + relative);
        }

        public static bool IsCompiledUnityPlayer(string directory)
        {
            string player=Path.Combine(directory,Executable);
            return File.Exists(player) && Encoding.ASCII.GetString(File.ReadAllBytes(player)).IndexOf("UnityPlayer.dll",StringComparison.OrdinalIgnoreCase)>=0;
        }

        public void Complete()
        {
            ValidateRuntime(root);
            foreach (string name in LegacyExecutables.Concat(LegacyDataDirectories).Concat(ObsoleteTools))
            {
                string source = Inside(name);
                if (File.Exists(source) || Directory.Exists(source)) Move(source, Path.Combine(backup, name));
            }
            Journal("COMPLETE", root, DataDirectory);
        }

        public void Rollback()
        {
            foreach (var file in replaced)
                if (file.Value != null)
                {
                    string temporary=file.Key+".ultra-restore-"+Guid.NewGuid().ToString("N");
                    try { File.Copy(file.Value,temporary);ReplaceFile(temporary,file.Key); }
                    finally { if(File.Exists(temporary))File.Delete(temporary); }
                }
            foreach (string file in created) if (File.Exists(file)) File.Delete(file);
            for (int i = moves.Count - 1; i >= 0; i--)
            {
                var move = moves[i];
                if (Directory.Exists(move[1])) Directory.Move(move[1], move[0]);
                else if (File.Exists(move[1])) File.Move(move[1], move[0]);
            }
            Journal("ROLLED_BACK", root, root);
        }

        public static void ReplaceFile(string temporary,string destination)
        {
            if(File.Exists(destination))File.Replace(temporary,destination,null);
            else File.Move(temporary,destination);
        }
    }
}
