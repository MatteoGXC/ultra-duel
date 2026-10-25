using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MDPro3.Net
{
    public static class UltraDeckCloud
    {
        static string Metadata => Environment.GetCommandLineArgs().Contains("--cloud-backup-check")?"Progress/deck-cloud-diagnostic.json":"Progress/deck-cloud.json";
        static bool busy;
        static float nextSync;
        public static string Status {get;private set;} = "Sign in to back up and restore your decks.";
        static string[] DeckFiles() => Directory.Exists("Deck/CloudDiagnostic") && Environment.GetCommandLineArgs().Contains("--cloud-backup-check") ? Directory.GetFiles("Deck/CloudDiagnostic","*.ydk",SearchOption.AllDirectories) : Environment.GetCommandLineArgs().Contains("--cloud-backup-check") ? new string[0] : Directory.GetFiles("Deck","*.ydk",SearchOption.AllDirectories);
        static bool SafeMenu()
        {
            var p=Program.instance;
            return p!=null && p.currentServant!=p.ocgcore && p.currentServant!=p.room && p.currentServant!=p.deckEditor;
        }
        static string Hash(string text)
        {
            using var sha=SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","").ToLowerInvariant();
        }
        static string Target(string name)
        {
            if(string.IsNullOrEmpty(name) || name.Length>180 || !name.EndsWith(".ydk",StringComparison.OrdinalIgnoreCase) ||
                name.Split('/').Any(s=>string.IsNullOrEmpty(s) || s=="." || s==".." || s.IndexOfAny(new[]{'\\',':','\0','<','>','"','|','?','*'})>=0))throw new IOException("Invalid cloud deck filename.");
            string root=Path.GetFullPath("Deck")+Path.DirectorySeparatorChar;
            string result=Path.GetFullPath(Path.Combine(root,name));
            if(!result.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Invalid cloud deck path.");
            return result;
        }
        static void Write(string path,string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary=path+".cloud-partial";
            File.WriteAllText(temporary,content,new UTF8Encoding(false));
            if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
        }
        public static async UniTask Sync(bool force=false)
        {
            if(Application.isEditor || (Environment.GetCommandLineArgs().Contains("--format-smoke") && !Environment.GetCommandLineArgs().Contains("--cloud-backup-check")) ||
                Environment.GetCommandLineArgs().Contains("--mac-platform-probe") || busy || !SafeMenu() || string.IsNullOrEmpty(UltraServices.Token))return;
            if(!force && Time.realtimeSinceStartup<nextSync)return;
            busy=true;nextSync=Time.realtimeSinceStartup+60;
            string token=UltraServices.Token,account=UltraServices.PlayerName.ToLowerInvariant();
            bool SameSession()=>token==UltraServices.Token && SafeMenu();
            try
            {
                Status="Syncing deck backups...";
                var metadata=File.Exists(Metadata)?JObject.Parse(File.ReadAllText(Metadata)):new JObject();
                var owners=metadata["owners"] as JObject ?? new JObject();metadata["owners"]=owners;
                var accounts=metadata["accounts"] as JObject ?? new JObject();metadata["accounts"]=accounts;
                var baseline=accounts[account] as JObject ?? new JObject();accounts[account]=baseline;
                // Claim unassigned files before restoring. Decks from another signed-in account stay local.
                Directory.CreateDirectory("Deck");
                foreach(string file in DeckFiles())
                {
                    string name=Path.GetFullPath(file).Substring(Path.GetFullPath("Deck").Length+1).Replace('\\','/').ToLowerInvariant();
                    if(owners[name]==null)owners[name]=account;
                }
                Write(Metadata,metadata.ToString());
                var remote=await UltraServices.Request("/decks");
                if(!SameSession())return;
                int restored=0,uploaded=0;
                foreach(var row in (JArray)remote["decks"])
                {
                    string name=(string)row["name"],key=name.ToLowerInvariant(),text=(string)row["content"],sha=(string)row["sha256"];
                    string path=Target(name);
                    if(Hash(text)!=sha)throw new IOException("Deck backup verification failed.");
                    string previous=(string)baseline[key],owner=(string)owners[key];
                    if(owner!=null && owner!=account)continue;
                    if(!File.Exists(path))
                    {
                        // A known missing file was deliberately deleted. A fresh install has no baseline.
                        if(previous==null){Write(path,text);restored++;owners[key]=account;}
                    }
                    else
                    {
                        string local=Hash(File.ReadAllText(path));
                        if(local!=sha && local==previous){Write(path,text);restored++;}
                        else if(local!=sha && sha!=previous)
                        {
                            string copy=Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+" (Cloud "+sha.Substring(0,8)+").ydk");
                            if(!File.Exists(copy)){Write(copy,text);restored++;}
                            owners[Path.GetFullPath(copy).Substring(Path.GetFullPath("Deck").Length+1).Replace('\\','/').ToLowerInvariant()]=account;
                        }
                    }
                    baseline[key]=sha;
                }
                Write(Metadata,metadata.ToString());
                foreach(string file in DeckFiles())
                {
                    if(!SameSession())break;
                    string name=Path.GetFullPath(file).Substring(Path.GetFullPath("Deck").Length+1).Replace('\\','/'),key=name.ToLowerInvariant();
                    if((string)owners[key]!=account || new FileInfo(file).Length>65536)continue;
                    string content=File.ReadAllText(file),sha=Hash(content);
                    if((string)baseline[key]==sha || !content.Contains("#main"))continue;
                    var saved=await UltraServices.Request("/decks","POST",new{name,content,baseSha=(string)baseline[key]??""});
                    if(token!=UltraServices.Token)return;
                    baseline[key]=(string)saved["sha256"];
                    Write(Metadata,metadata.ToString());
                    if(++uploaded>=8){nextSync=Time.realtimeSinceStartup+15;break;}
                }
                Write(Metadata,metadata.ToString());
                Status="Decks backed up to your account."+(restored>0?" Restored "+restored+" deck(s).":"");
            }
            catch(Exception e){Status="Deck backup will retry: "+e.Message;}
            finally{busy=false;}
        }
    }
}
