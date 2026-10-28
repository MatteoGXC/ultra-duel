using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WindowsFormatBuild
{
    public static void Build()
    {
        CompilePlayer();
        AssembleRuntime();
    }

    public static void CompilePlayer()
    {
        CompilePlayer(true);
    }

    public static void RecompilePlayer()
    {
        CompilePlayer(false);
    }

    private static void CompilePlayer(bool buildAddressables)
    {
        string root = Directory.GetCurrentDirectory();
        string destination = Environment.GetEnvironmentVariable("ULTRA_BUILD_OUTPUT") ?? Path.Combine(root, "release", "Ultra-Duel");
        Directory.CreateDirectory(destination);
        if (!File.Exists("Data/lflist_genesys.conf") || !File.Exists("Data/format-data-sources.json"))
            throw new Exception("Run python Tools/refresh-format-data.py before building.");

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        PlayerSettings.productName = "Ultra Duel";
        PlayerSettings.bundleVersion = "0.1.18";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
        AssetDatabase.SaveAssets();
        string verifiedContent=Environment.GetEnvironmentVariable("ULTRA_WINDOWS_CONTENT");
        if (buildAddressables && string.IsNullOrEmpty(verifiedContent))
        {
            if(AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/RenderFontEnglish.asset")==null)
                throw new Exception("Restore the original TMP font sources, or set ULTRA_WINDOWS_CONTENT to a verified Windows runtime data directory before building.");
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult addressables);
            if (!string.IsNullOrEmpty(addressables.Error)) throw new Exception(addressables.Error);
        }
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.BuildAddressablesWithPlayerBuild =
            AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Boot.unity" },
            locationPathName = Path.Combine(destination, "Ultra-Duel.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows build failed: " + report.summary.result);
        UseVerifiedMenuContent(destination);
        Debug.Log("WINDOWS_PLAYER_COMPILED_RUNTIME_ASSETS_PENDING " + destination);
    }

    public static void AssembleRuntime()
    {
        string root = Directory.GetCurrentDirectory();
        string destination = Environment.GetEnvironmentVariable("ULTRA_BUILD_OUTPUT") ?? Path.Combine(root, "release", "Ultra-Duel");
        if (!Directory.Exists("Platforms/StandaloneWindows64/MasterDuel"))
            throw new Exception("The original Windows runtime asset bundles have not finished downloading.");
        if (!MDPro3.Net.YgoServer.SupportsTcgAndGenesys())
            throw new Exception("Build both native rules-engine DLLs before assembling the game.");
        // Core Master Duel music and effects are Addressables built into the player.
        // The separate, unavailable Sound repository only provides optional character voices.
        foreach (string pack in new[] { "windows", "art", "closeup" })
            if (!File.Exists(Path.Combine(root,"..","downloads",pack+".files-complete")))
                throw new Exception("Runtime pack has not finished verification: " + pack);
        CopyDirectory(Path.Combine(root,"Data"), Path.Combine(destination,"Data"), true);
        string config = Path.Combine(destination,"Data","config.conf");
        if (!File.Exists(config))
            File.WriteAllText(config,
                "Language->en-US\nCardLanguage->en-US\nPrereleaseLanguage->en-US\n" +
                "DuelFormat->TCG\nGenesysPointCap->100\nDeckInUse->Starter - TCG\n");
        CopyDirectory(Path.Combine(root,"Platforms","StandaloneWindows64"),
            Path.Combine(destination,"Ultra-Duel_Data","StandaloneWindows64"));
        foreach (string folder in new[] { "Sound", "Picture", "Puzzle", "Expansions" })
            if (Directory.Exists(folder)) CopyDirectory(Path.Combine(root,folder), Path.Combine(destination,folder));
        foreach (string folder in new[] { "Deck", "Replay", "Expansions" })
            Directory.CreateDirectory(Path.Combine(destination,folder));
        foreach (string deck in Directory.GetFiles(Path.Combine(root,"Deck"),"Starter - *.ydk"))
        {
            string targetDeck=Path.Combine(destination,"Deck",Path.GetFileName(deck));
            if(!File.Exists(targetDeck))File.Copy(deck,targetDeck);
        }
        foreach (string library in new[] { "ocgcore.dll", "ygoserver.dll" })
        {
            string nativeTarget = Path.Combine(destination,"Ultra-Duel_Data","Plugins","x86_64",library);
            if (!File.Exists(nativeTarget))
                throw new Exception("Expected Unity native-plugin layout is missing: " + nativeTarget);
            File.Copy(Path.Combine(root,"Assets","Plugins","Windows",library),nativeTarget,true);
        }
        if (File.Exists("LICENSE")) File.Copy("LICENSE", Path.Combine(destination,"LICENSE"), true);
        File.Copy("FORMAT-BUILD.md", Path.Combine(destination,"README.txt"), true);
        UseVerifiedMenuContent(destination);
        var tools=new System.Diagnostics.ProcessStartInfo("powershell.exe",
            "-NoProfile -ExecutionPolicy Bypass -File \""+Path.Combine(root,"Tools","Build-Ultra-Tools.ps1")+
            "\" -OutputDirectory \""+destination+"\"") { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
        using(var process=System.Diagnostics.Process.Start(tools))
        {
            string text=process.StandardOutput.ReadToEnd()+process.StandardError.ReadToEnd();process.WaitForExit();
            if(process.ExitCode!=0)throw new Exception("Ultra Duel updater tools failed: "+text);
        }
        string commit;
        using(var git=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git","rev-parse HEAD") {
            WorkingDirectory=root,UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true }))
        { commit=git.StandardOutput.ReadToEnd().Trim();git.WaitForExit();if(git.ExitCode!=0)throw new Exception("Cannot identify build source."); }
        File.WriteAllText(Path.Combine(destination,"build-info.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {
            version=PlayerSettings.bundleVersion,commit=commit,application="Ultra Duel",executable="Ultra-Duel.exe",
            runtimeData="Ultra-Duel_Data",checks=new { nativeRulesCapabilities=true }
        },Newtonsoft.Json.Formatting.Indented));
        File.WriteAllText(Path.Combine(destination,"Data","installed-version.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {
            version=PlayerSettings.bundleVersion,sourceCommit=commit,runtimeLayout="ultra-v1"
        }));
        Debug.Log("WINDOWS_FORMAT_BUILD_SUCCEEDED " + destination);
    }

    private static void UseVerifiedMenuContent(string destination)
    {
        string source=Environment.GetEnvironmentVariable("ULTRA_WINDOWS_CONTENT");
        if(string.IsNullOrEmpty(source))return;
        source=Path.GetFullPath(source);
        string target=Path.GetFullPath(Path.Combine(destination,"Ultra-Duel_Data"));
        if(source==target || !Directory.Exists(Path.Combine(source,"StandaloneWindows64","MDPro3")) ||
            !Directory.Exists(Path.Combine(source,"StreamingAssets","aa")))
            throw new Exception("ULTRA_WINDOWS_CONTENT must point to a separate, complete verified Windows runtime data directory.");
        // Runtime art and fonts are upstream binary assets. Reusing validated menu
        // bundles does not replace the newly compiled player or managed game code.
        CopyDirectory(Path.Combine(source,"StandaloneWindows64","MDPro3"),Path.Combine(target,"StandaloneWindows64","MDPro3"));
        CopyDirectory(Path.Combine(source,"StreamingAssets","aa"),Path.Combine(target,"StreamingAssets","aa"));
    }

    private static void CopyDirectory(string source, string target, bool dataRoot=false)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
        {
            if(dataRoot && Array.IndexOf(new[]{"config.conf","Settings.json","mycard_apps.json","profile.json","friends-session.json"},Path.GetFileName(file))>=0)continue;
            string destination = Path.Combine(target,Path.GetFileName(file));
            var input = new FileInfo(file);
            var output = new FileInfo(destination);
            if (!output.Exists || input.Length != output.Length || input.LastWriteTimeUtc != output.LastWriteTimeUtc)
                File.Copy(file,destination,true);
        }
        foreach (string folder in Directory.GetDirectories(source))
            CopyDirectory(folder,Path.Combine(target,Path.GetFileName(folder)));
    }
}
