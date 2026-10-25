using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace MDPro3
{
    // Independent of Unity so reinstall/restore behavior can be tested with real
    // temporary installations. Nothing in the game installer owns this folder.
    public sealed class UltraUserDataBackup
    {
        readonly string runtime, store;
        sealed class Cached { public long length,ticks; public string hash; }
        readonly Dictionary<string,Cached> cache=new Dictionary<string,Cached>(StringComparer.OrdinalIgnoreCase);
        static readonly string[] Personal={"Data/config.conf","Data/Settings.json","Data/profile.json","Data/mycard_apps.json"};
        public string Location{get{return store;}}
        public UltraUserDataBackup(string runtime,string store){this.runtime=Path.GetFullPath(runtime);this.store=Path.GetFullPath(store);}
        public static bool IsPersonal(string relative)
        {
            relative=relative.Replace('\\','/');
            return relative.StartsWith("Deck/",StringComparison.OrdinalIgnoreCase) || relative.StartsWith("Replay/",StringComparison.OrdinalIgnoreCase) || relative.StartsWith("Progress/",StringComparison.OrdinalIgnoreCase) || Personal.Any(p=>p.Equals(relative,StringComparison.OrdinalIgnoreCase));
        }
        static string Hash(string path)
        {
            using(var stream=File.OpenRead(path))using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        string Target(string relative)
        {
            if(!IsPersonal(relative) || Path.IsPathRooted(relative) || relative.Contains(":") || relative.Split('/','\\').Contains(".."))throw new IOException("Invalid backup path.");
            string result=Path.GetFullPath(Path.Combine(runtime,relative));
            if(!result.StartsWith(runtime+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Backup path escaped installation.");
            return result;
        }
        static void AtomicWrite(string target,string temporary)
        {
            if(File.Exists(target))File.Replace(temporary,target,null);
            else File.Move(temporary,target);
        }
        public int Restore()
        {
            string latest=Path.Combine(store,"latest.txt");if(!File.Exists(latest))return 0;
            if(File.ReadLines(latest).FirstOrDefault()!="ULTRA_USER_DATA_1")return 0;
            int count=0;
            foreach(string line in File.ReadAllLines(latest).Skip(1))
            {
                try
                {
                    string[] fields=line.Split('|');if(fields.Length!=3 || fields[0].Length!=64 || fields[0].Any(c=>!Uri.IsHexDigit(c)))continue;
                    string relative=Encoding.UTF8.GetString(Convert.FromBase64String(fields[2]));string target=Target(relative);
                    string blob=Path.Combine(store,"Files",fields[0]);if(!File.Exists(blob) || Hash(blob)!=fields[0])continue;
                    if(File.Exists(target))
                    {
                        if(Hash(target)==fields[0] || !relative.StartsWith("Deck/",StringComparison.OrdinalIgnoreCase))continue;
                        // Keep both versions of a deck with the same filename.
                        string stem=Path.Combine(Path.GetDirectoryName(target),Path.GetFileNameWithoutExtension(target)+" (Recovered)");string extension=Path.GetExtension(target);target=stem+extension;
                        int suffix=2;
                        while(File.Exists(target) && Hash(target)!=fields[0])target=stem+" "+(suffix++)+extension;
                        if(File.Exists(target))continue;
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(blob,target,false);
                    long ticks;
                    if(long.TryParse(fields[1],out ticks) && ticks>=DateTime.MinValue.Ticks && ticks<=DateTime.MaxValue.Ticks)File.SetLastWriteTimeUtc(target,new DateTime(ticks,DateTimeKind.Utc));
                    count++;
                }
                catch(IOException){/* A locked destination stays untouched. */}
                catch(UnauthorizedAccessException){}
                catch(FormatException){}
            }
            return count;
        }
        public bool Capture()
        {
            // A deleted installation must never replace a good backup with an
            // empty library while an old game process is still shutting down.
            if(!Directory.Exists(Path.Combine(runtime,"Deck")))return false;
            Directory.CreateDirectory(Path.Combine(store,"Files"));Directory.CreateDirectory(Path.Combine(store,"Snapshots"));
            var files=new List<string>();
            foreach(string folder in new[]{"Deck","Replay","Progress"})
            {
                string path=Path.Combine(runtime,folder);if(Directory.Exists(path))files.AddRange(Directory.GetFiles(path,"*",SearchOption.AllDirectories));
            }
            files.AddRange(Personal.Select(p=>Path.Combine(runtime,p)).Where(File.Exists));
            var rows=new List<string>{"ULTRA_USER_DATA_1"};var decks=new List<Tuple<string,string>>();
            foreach(string source in files.OrderBy(p=>p,StringComparer.OrdinalIgnoreCase))
            {
                string relative=source.Substring(runtime.Length+1).Replace('\\','/');Target(relative);
                var info=new FileInfo(source);Cached previous;
                if(cache.TryGetValue(relative,out previous) && previous.length==info.Length && previous.ticks==info.LastWriteTimeUtc.Ticks && File.Exists(Path.Combine(store,"Files",previous.hash)))
                {
                    rows.Add(previous.hash+"|"+previous.ticks+"|"+Convert.ToBase64String(Encoding.UTF8.GetBytes(relative)));
                    if(relative.StartsWith("Deck/",StringComparison.OrdinalIgnoreCase))decks.Add(Tuple.Create(relative,Path.Combine(store,"Files",previous.hash)));
                    continue;
                }
                string temporary=Path.Combine(store,"Files",Guid.NewGuid().ToString("N")+".partial");
                try
                {
                    long before=File.GetLastWriteTimeUtc(source).Ticks;File.Copy(source,temporary,false);
                    if(before!=File.GetLastWriteTimeUtc(source).Ticks)throw new IOException("Save is still being written.");
                    string hash=Hash(temporary),blob=Path.Combine(store,"Files",hash);
                    if(!File.Exists(blob))File.Move(temporary,blob);
                    else if(Hash(blob)!=hash)AtomicWrite(blob,temporary);
                    cache[relative]=new Cached{length=new FileInfo(blob).Length,ticks=before,hash=hash};
                    rows.Add(hash+"|"+before+"|"+Convert.ToBase64String(Encoding.UTF8.GetBytes(relative)));
                    if(relative.StartsWith("Deck/",StringComparison.OrdinalIgnoreCase))decks.Add(Tuple.Create(relative,blob));
                }
                finally{if(File.Exists(temporary))File.Delete(temporary);}
            }
            string latest=Path.Combine(store,"latest.txt"),contents=string.Join("\n",rows);
            if(File.Exists(latest) && File.ReadAllText(latest)==contents)return false;
            string snapshot=Path.Combine(store,"Snapshots",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+Guid.NewGuid().ToString("N")+".txt");
            File.WriteAllText(snapshot,contents,Encoding.UTF8);
            string pending=latest+".partial";File.WriteAllText(pending,contents,Encoding.UTF8);AtomicWrite(latest,pending);
            string archive=Path.Combine(store,"Decks-Latest.zip"),zipTemp=archive+".partial";
            using(var stream=new FileStream(zipTemp,FileMode.Create))using(var zip=new ZipArchive(stream,ZipArchiveMode.Create))
                foreach(var deck in decks){var entry=zip.CreateEntry(deck.Item1,CompressionLevel.Optimal);using(var output=entry.Open())using(var input=File.OpenRead(deck.Item2))input.CopyTo(output);}
            AtomicWrite(archive,zipTemp);
            return true;
        }
    }
}
