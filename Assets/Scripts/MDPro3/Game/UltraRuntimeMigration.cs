using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace MDPro3
{
    public static class UltraRuntimeMigration
    {
        // Older update scripts cannot invoke the new migration DLL. The cumulative
        // patch includes the complete Unity boot runtime, so it can finish importing
        // unchanged content here, before Boot loads any Addressables or user data.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void ImportLegacyRuntime()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            string root = Path.GetDirectoryName(Application.dataPath);
            if (!UltraInstallMigration.LegacyDataDirectories.Any(n => Directory.Exists(Path.Combine(root, n)))) return;
            var migration = new UltraInstallMigration(root);
            try
            {
                new UltraUserDataBackup(root, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ultra Duel", "UserData")).Capture();
                migration.Prepare();
                migration.Complete();
                Debug.Log("ULTRA_RUNTIME_MIGRATED backup=" + migration.BackupDirectory);
            }
            catch (Exception error)
            {
                migration.Rollback();
                Debug.LogError("Ultra Duel runtime migration failed; previous files were preserved: " + error);
                Application.Quit(1);
            }
#endif
        }
    }
}
