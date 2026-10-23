using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

public static class WindowsInstallerSmoke
{
    public static int Main(string[] args)
    {
        string work = Path.GetFullPath(args[1]);
        Directory.CreateDirectory(work);
        var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
        var extract = assembly.GetType("WindowsInstaller").GetMethod("Extract", BindingFlags.NonPublic | BindingFlags.Static);
        string target = Path.Combine(work, "preserve-check");
        Directory.CreateDirectory(Path.Combine(target, "Data"));
        Directory.CreateDirectory(Path.Combine(target, "Deck"));
        Directory.CreateDirectory(Path.Combine(target, "Progress"));
        File.WriteAllText(Path.Combine(target, "Progress/profile.json"), "earned rewards");
        File.WriteAllText(Path.Combine(target, "Data/friends-session.json"), "existing session");
        File.WriteAllText(Path.Combine(target, "Data/Settings.json"), "custom settings");
        File.WriteAllText(Path.Combine(target, "Data/config.conf"), "existing user settings");
        File.WriteAllText(Path.Combine(target, "Deck/Starter.ydk"), "existing user deck");
        string fixture = Path.Combine(work, "fixture.zip");
        using (var zip = ZipFile.Open(fixture, ZipArchiveMode.Create))
        {
            Write(zip, "Data/config.conf", "default settings");
            Write(zip, "Deck/Starter.ydk", "default starter");
            Write(zip, "Game/data.txt", "runtime asset");
            Write(zip, "Progress/profile.json", "empty profile");
            Write(zip, "Data/friends-session.json", "empty session");
            Write(zip, "Data/Settings.json", "default settings");
        }
        extract.Invoke(null, new object[] { fixture, target });
        if (File.ReadAllText(Path.Combine(target, "Data/config.conf")) != "existing user settings"
            || File.ReadAllText(Path.Combine(target, "Deck/Starter.ydk")) != "existing user deck"
            || File.ReadAllText(Path.Combine(target, "Progress/profile.json")) != "earned rewards"
            || File.ReadAllText(Path.Combine(target, "Data/friends-session.json")) != "existing session"
            || File.ReadAllText(Path.Combine(target, "Data/Settings.json")) != "custom settings"
            || File.ReadAllText(Path.Combine(target, "Game/data.txt")) != "runtime asset")
            throw new Exception("Existing settings/decks were overwritten or runtime data was not installed.");
        string traversal = Path.Combine(work, "traversal.zip");
        using (var zip = ZipFile.Open(traversal, ZipArchiveMode.Create)) Write(zip, "../outside.txt", "invalid");
        bool rejected = false;
        try { extract.Invoke(null, new object[] { traversal, target }); }
        catch (TargetInvocationException error) { rejected = error.InnerException is IOException; }
        if (!rejected || File.Exists(Path.Combine(work, "outside.txt"))) throw new Exception("Archive path traversal was not rejected.");
        Console.WriteLine("PASS installer preserves existing decks/settings and rejects paths outside its installation folder.");
        if (args.Length > 2)
        {
            string fresh = Path.Combine(work, "fresh-game");
            foreach (string part in Directory.GetFiles(args[2], "Matteo-Duel-Windows-*.zip"))
            {
                extract.Invoke(null, new object[] { part, fresh });
                Console.WriteLine("Extracted " + Path.GetFileName(part));
            }
            if (!File.Exists(Path.Combine(fresh, "Matteo-Duel.exe"))) throw new Exception("Packaged executable missing.");
            Console.WriteLine("PASS full package extracted using the installer's own extraction code.");
        }
        return 0;
    }
    static void Write(ZipArchive zip, string path, string value)
    {
        using (var writer = new StreamWriter(zip.CreateEntry(path).Open())) writer.Write(value);
    }
}
