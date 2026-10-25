using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MDPro3
{
    public sealed class UltraSaveSafety : MonoBehaviour
    {
        UltraUserDataBackup backup;
        FileSystemWatcher[] watchers;
        volatile bool dirty;
        float nextCheck;
        public static string Location {get;private set;}
        public static void Initialize()
        {
            // Automated players must never read or overwrite a person's backup.
            if(Application.isEditor || Environment.GetCommandLineArgs().Contains("--format-smoke") || Environment.GetCommandLineArgs().Contains("--mac-platform-probe"))return;
            try
            {
                string root=Directory.GetCurrentDirectory();
                string parent=Application.platform==RuntimePlatform.OSXPlayer?Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Library","Application Support"):Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                Location=Path.Combine(parent,"Ultra Duel","UserData");
                var service=new GameObject("Save safety").AddComponent<UltraSaveSafety>();DontDestroyOnLoad(service.gameObject);
                service.backup=new UltraUserDataBackup(root,Location);
                string marker=Path.Combine(root,"Data","user-data-store.txt");
                if(!File.Exists(marker) || !Directory.Exists(Path.Combine(root,"Deck")))service.backup.Restore();
                Directory.CreateDirectory(Path.Combine(root,"Data"));Directory.CreateDirectory(Path.Combine(root,"Deck"));
                File.WriteAllText(marker,"Personal backups are stored outside the installation: "+Location);
                service.Save();
                service.watchers=new FileSystemWatcher[4];int i=0;
                foreach(string folder in new[]{"Deck","Replay","Progress","Data"})
                {
                    string path=Path.Combine(root,folder);Directory.CreateDirectory(path);
                    var watcher=new FileSystemWatcher(path){IncludeSubdirectories=true,NotifyFilter=NotifyFilters.LastWrite|NotifyFilters.FileName|NotifyFilters.DirectoryName};
                    FileSystemEventHandler changed=(s,e)=>{string relative=e.FullPath.Substring(root.Length+1);if(UltraUserDataBackup.IsPersonal(relative))service.dirty=true;};
                    watcher.Changed+=changed;watcher.Created+=changed;watcher.Deleted+=changed;
                    watcher.Renamed+=(s,e)=>service.dirty=true;watcher.Error+=(s,e)=>service.dirty=true;
                    watcher.EnableRaisingEvents=true;service.watchers[i++]=watcher;
                }
            }
            catch(Exception e){Debug.LogWarning("Save backup could not start: "+e.Message);}
        }
        void Update(){if(dirty && Time.unscaledTime>=nextCheck){dirty=false;nextCheck=Time.unscaledTime+3;Save();}}
        void Save(){try{backup?.Capture();}catch(IOException){dirty=true;nextCheck=Time.unscaledTime+3;}catch(Exception e){Debug.LogWarning("Save backup: "+e.Message);}}
        void OnApplicationFocus(bool focused){if(!focused)Save();}
        void OnApplicationQuit(){Save();}
        void OnDestroy(){if(watchers!=null)foreach(var watcher in watchers)watcher?.Dispose();}
    }
}
