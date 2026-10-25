using System;
using System.IO;
using MDPro3;

public static class UltraBrandingTests
{
    static void Check(bool value,string message) { if(!value)throw new Exception(message); }
    static void Write(string root,string name,string value)
    {
        string path=Path.Combine(root,name.Replace('/',Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path,value);
    }
    public static int Main(string[] args)
    {
        if(args.Length>0 && args[0]=="--hold") { System.Threading.Thread.Sleep(30000);return 0; }
        string root=Path.Combine(Path.GetFullPath(args[0]),"migration-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        string legacy=UltraInstallMigration.LegacyDataDirectories[0];
        foreach(string name in new[]{"Managed/Assembly-CSharp.dll","globalgamemanagers","Plugins/x86_64/ocgcore.dll","Plugins/x86_64/ygoserver.dll","StandaloneWindows64/MasterDuel/field.bundle"})
            Write(root,legacy+"/"+name,"old "+name);
        Write(root,UltraInstallMigration.LegacyExecutables[0],"old player");
        Write(root,UltraInstallMigration.ObsoleteTools[0],"old updater");
        Write(root,"UnityPlayer.dll","engine");
        Write(root,"Data/config.conf","personal settings");
        Write(root,"Deck/Mine.ydk",DeckFormatMetadata.LegacyPrefix+"Genesys:47\n#main\n89631139\n!side\n");
        Write(root,"Progress/profile.json","earned progress");
        string deck=File.ReadAllText(Path.Combine(root,"Deck/Mine.ydk"));
        var migration=new UltraInstallMigration(root);
        migration.Prepare();
        Check(!Directory.Exists(Path.Combine(root,legacy)),"Legacy runtime was not moved.");
        File.WriteAllText(migration.PrepareReplacement(UltraInstallMigration.Executable),"new Unity player UnityPlayer.dll");
        File.WriteAllText(migration.PrepareReplacement(UltraInstallMigration.DataDirectory+"/Managed/Assembly-CSharp.dll"),"new code");
        migration.Complete();
        UltraInstallMigration.ValidateRuntime(root);
        Check(File.Exists(Path.Combine(migration.BackupDirectory,UltraInstallMigration.LegacyExecutables[0])),"Old player was not archived.");
        Check(!File.Exists(Path.Combine(root,UltraInstallMigration.LegacyExecutables[0])),"Old player remains in the application.");
        Check(File.ReadAllText(Path.Combine(root,"Deck/Mine.ydk"))==deck,"Deck changed during runtime migration.");
        Check(File.ReadAllText(Path.Combine(root,"Data/config.conf"))=="personal settings","Settings changed.");
        Check(File.ReadAllText(Path.Combine(root,"Progress/profile.json"))=="earned progress","Progress changed.");
        migration.Rollback();
        Check(File.ReadAllText(Path.Combine(root,legacy+"/Managed/Assembly-CSharp.dll"))=="old Managed/Assembly-CSharp.dll","Rollback did not restore original code.");
        Check(File.ReadAllText(Path.Combine(root,UltraInstallMigration.LegacyExecutables[0]))=="old player","Rollback did not restore original executable.");
        Check(!File.Exists(Path.Combine(root,UltraInstallMigration.Executable)),"Rollback retained the new player.");

        // Updates applied by old scripts already contain the new boot runtime.
        Write(root,UltraInstallMigration.DataDirectory+"/Managed/Assembly-CSharp.dll","newer code");
        Write(root,UltraInstallMigration.Executable,"new Unity player UnityPlayer.dll");
        migration=new UltraInstallMigration(root);migration.Prepare();migration.Complete();
        Check(File.ReadAllText(Path.Combine(root,UltraInstallMigration.DataDirectory+"/Managed/Assembly-CSharp.dll"))=="newer code","Old runtime replaced updated code.");
        Check(File.Exists(Path.Combine(root,UltraInstallMigration.DataDirectory+"/StandaloneWindows64/MasterDuel/field.bundle")),"Unchanged assets were not imported.");
        Check(File.ReadAllText(Path.Combine(root,"Deck/Mine.ydk"))==deck,"Old-updater recovery changed a deck.");
        Check(UltraInstallMigration.CanonicalArchivePath(legacy+"/asset")==UltraInstallMigration.DataDirectory+"/asset","Historical archive mapping failed.");
        Check(UltraInstallMigration.CanonicalArchivePath(UltraInstallMigration.ObsoleteTools[0])==null,"Obsolete updater would be reinstalled.");
        bool rejected=false;try{migration.PrepareReplacement("../outside.txt");}catch(IOException){rejected=true;}
        Check(rejected,"Migration allowed a path outside the installation.");
        Check(DeckFormatMetadata.IsMetadata(DeckFormatMetadata.LegacyPrefix+"TCG:100"),"Legacy deck reading was removed.");
        Check(DeckFormatMetadata.Write("Genesys",47)=="#ultra-format:Genesys:47","New decks do not use canonical metadata.");
        string active=Path.Combine(root,"running-fixture");Directory.CreateDirectory(active);
        File.Copy(System.Reflection.Assembly.GetExecutingAssembly().Location,Path.Combine(active,UltraInstallMigration.Executable));
        using(var child=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Path.Combine(active,UltraInstallMigration.Executable),"--hold") { UseShellExecute=false,CreateNoWindow=true }))
        {
            try
            {
                System.Threading.Thread.Sleep(200);
                rejected=false;try{UltraInstallMigration.AssertNotRunning(active);}catch(IOException){rejected=true;}
                Check(rejected,"A running game was not protected.");
            }
            finally { if(!child.HasExited)child.Kill();child.WaitForExit(); }
        }
        Console.WriteLine("PASS runtime migration, old-updater recovery, rollback, archive mapping, personal-data preservation and metadata compatibility.");
        return 0;
    }
}
