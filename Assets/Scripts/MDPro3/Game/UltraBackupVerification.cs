using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MDPro3
{
    public static class UltraBackupVerification
    {
        static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
        public static string Run()
        {
            string temp=Path.GetFullPath(Path.GetTempPath());string root=Path.Combine(temp,"UltraDataTest-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                string game=Path.Combine(root,"Install"),store=Path.Combine(root,"UserData");
                Directory.CreateDirectory(Path.Combine(game,"Deck"));Directory.CreateDirectory(Path.Combine(game,"Data"));
                File.WriteAllText(Path.Combine(game,"Deck","My deck.ydk"),"#main\n123\n#extra\n!side\n");
                File.WriteAllText(Path.Combine(game,"Data","config.conf"),"UltraGuiScale->85\n");
                File.WriteAllText(Path.Combine(game,"Data","friends-session.json"),"not-a-backup-input");
                var original=new UltraUserDataBackup(game,store);Require(original.Capture(),"Initial backup was not created.");Require(!original.Capture(),"Unchanged saves created duplicate snapshots.");
                // Simulate removing the old installation and selecting a new one.
                string old=Path.Combine(root,"OldInstall");
                Require(Path.GetFullPath(game).StartsWith(root+Path.DirectorySeparatorChar) && Path.GetFullPath(old).StartsWith(root+Path.DirectorySeparatorChar),"Unsafe test move.");
                Directory.Move(game,old);Directory.CreateDirectory(Path.Combine(game,"Deck"));
                var fresh=new UltraUserDataBackup(game,store);Require(fresh.Restore()==2,"Reinstall did not restore the deck and preferences.");
                Require(File.ReadAllText(Path.Combine(game,"Deck","My deck.ydk")).Contains("123"),"Restored deck contents changed.");
                Require(!File.Exists(Path.Combine(game,"Data","friends-session.json")),"Login tokens entered the save backup.");
                File.WriteAllText(Path.Combine(game,"Deck","My deck.ydk"),"new personal deck");fresh.Restore();
                Require(File.ReadAllText(Path.Combine(game,"Deck","My deck.ydk"))=="new personal deck","Restore overwrote an existing deck.");
                Require(File.Exists(Path.Combine(game,"Deck","My deck (Recovered).ydk")),"Conflicting deck was not preserved separately.");
                fresh.Capture();File.Delete(Path.Combine(game,"Deck","My deck (Recovered).ydk"));fresh.Capture();
                Require(fresh.Restore()==0,"An intentionally deleted deck was resurrected from an older snapshot.");
                using(var zip=ZipFile.OpenRead(Path.Combine(store,"Decks-Latest.zip")))Require(zip.Entries.Count==1 && zip.Entries[0].FullName=="Deck/My deck.ydk","Readable deck export contains incorrect files.");
                File.AppendAllText(Path.Combine(store,"latest.txt"),"\n"+new string('a',64)+"|0|"+Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("../outside.ydk")));
                Require(fresh.Restore()==0 && !File.Exists(Path.Combine(root,"outside.ydk")),"Backup path traversal was accepted.");
                return "REINSTALL_BACKUP_PASSED deletion/reinstall, preferences, deck conflicts, intentional deletions, token exclusion and safe export";
            }
            finally
            {
                string resolved=Path.GetFullPath(root);
                if(resolved.StartsWith(Path.Combine(temp,"UltraDataTest-"),StringComparison.OrdinalIgnoreCase) && Directory.Exists(resolved))Directory.Delete(resolved,true);
            }
        }
#if ULTRA_BACKUP_TEST_PROGRAM
        public static void Main(){Console.WriteLine(Run());}
#endif
    }
}
