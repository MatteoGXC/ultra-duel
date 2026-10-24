using System;
using System.IO;
using Mono.Data.Sqlite;
using UnityEngine;

namespace MDPro3
{
    // Runs the packaged Apple Silicon player itself, including Mono's plugin loading.
    public static class MacPlatformProbe
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Run()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--mac-platform-probe");
            if(i<0 || i+1>=args.Length)return;
            string output=Path.GetFullPath(args[i+1]);
            try
            {
                string root=Environment.GetEnvironmentVariable("ULTRA_DUEL_DATA");
                if(string.IsNullOrEmpty(root))throw new Exception("Missing writable runtime directory.");
                Directory.SetCurrentDirectory(root);Program.SetRoot();
                if(Program.root!=Program.PATH_ROOT_MAC)throw new Exception("Player was not built for macOS.");
                if(!Net.YgoServer.SupportsTcgAndGenesys())throw new Exception("Native formats failed to load.");
                using(var db=new SqliteConnection("URI=file:"+Path.GetFullPath("Data/locales/en-US/cards.cdb")))
                {
                    db.Open();using(var command=db.CreateCommand())
                    {
                        command.CommandText="SELECT COUNT(*) FROM datas";
                        if(Convert.ToInt32(command.ExecuteScalar())<14000)throw new Exception("Card database is incomplete.");
                        command.CommandText="SELECT name FROM texts WHERE id=37818794";
                        if((string)command.ExecuteScalar()!="Red-Eyes Dark Dragoon")throw new Exception("Incorrect English card database.");
                    }
                }
                if(!File.Exists("Data/script.zip") || Directory.GetFiles("Picture/Art").Length<10000)throw new Exception("Card content is missing.");
                string bundle=Path.Combine(Program.root,"MasterDuel/Built-in/sprites");
                var sprites=AssetBundle.LoadFromFile(bundle);
                if(sprites==null)throw new Exception("The native macOS asset bundle did not load.");
                sprites.Unload(true);
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output,"PASS Apple Silicon Unity player: writable runtime, ARM native TCG/Genesys engine, SQLite, English card database, scripts, artwork and native macOS asset bundle.");
                Debug.Log("ULTRA_MAC_PLATFORM_PROBE_PASSED");Application.Quit(0);
            }
            catch(Exception error){Debug.LogException(error);File.WriteAllText(output+".failed",error.ToString());Application.Quit(3);}
        }
    }
}
