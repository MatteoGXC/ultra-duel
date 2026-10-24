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
        string destination = Environment.GetEnvironmentVariable("MATTEO_BUILD_OUTPUT") ?? Path.Combine(root, "release", "MDPro3-TCG-Genesys");
        Directory.CreateDirectory(destination);
        if (!File.Exists("Data/lflist_genesys.conf") || !File.Exists("Data/format-data-sources.json"))
            throw new Exception("Run python Tools/refresh-format-data.py before building.");

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
        PlayerSettings.productName = "Ultra Duel";
        PlayerSettings.bundleVersion = "0.1.5";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });
        AssetDatabase.SaveAssets();
        if (buildAddressables)
        {
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult addressables);
            if (!string.IsNullOrEmpty(addressables.Error)) throw new Exception(addressables.Error);
        }
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.BuildAddressablesWithPlayerBuild =
            AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Boot.unity" },
            locationPathName = Path.Combine(destination, "Matteo-Duel.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows build failed: " + report.summary.result);
        Debug.Log("WINDOWS_PLAYER_COMPILED_RUNTIME_ASSETS_PENDING " + destination);
    }

    public static void AssembleRuntime()
    {
        string root = Directory.GetCurrentDirectory();
        string destination = Environment.GetEnvironmentVariable("MATTEO_BUILD_OUTPUT") ?? Path.Combine(root, "release", "MDPro3-TCG-Genesys");
        if (!Directory.Exists("Platforms/StandaloneWindows64/MasterDuel"))
            throw new Exception("The original Windows runtime asset bundles have not finished downloading.");
        if (!MDPro3.Net.YgoServer.SupportsTcgAndGenesys())
            throw new Exception("Build both native rules-engine DLLs before assembling the game.");
        // Core Master Duel music and effects are Addressables built into the player.
        // The separate, unavailable Sound repository only provides optional character voices.
        foreach (string pack in new[] { "windows", "art", "closeup" })
            if (!File.Exists(Path.Combine(root,"..","downloads",pack+".files-complete")))
                throw new Exception("Runtime pack has not finished verification: " + pack);
        CopyDirectory(Path.Combine(root,"Data"), Path.Combine(destination,"Data"));
        string config = Path.Combine(destination,"Data","config.conf");
        if (!File.Exists(config))
            File.WriteAllText(config,
                "Language->en-US\nCardLanguage->en-US\nPrereleaseLanguage->en-US\n" +
                "DuelFormat->TCG\nGenesysPointCap->100\nDeckInUse->Starter - TCG\n");
        CopyDirectory(Path.Combine(root,"Platforms","StandaloneWindows64"),
            Path.Combine(destination,"Matteo-Duel_Data","StandaloneWindows64"));
        foreach (string folder in new[] { "Sound", "Picture", "Deck", "Puzzle", "Expansions" })
            if (Directory.Exists(folder)) CopyDirectory(Path.Combine(root,folder), Path.Combine(destination,folder));
        foreach (string folder in new[] { "Deck", "Replay", "Expansions" })
            Directory.CreateDirectory(Path.Combine(destination,folder));
        foreach (string library in new[] { "ocgcore.dll", "ygoserver.dll" })
        {
            string nativeTarget = Path.Combine(destination,"Matteo-Duel_Data","Plugins","x86_64",library);
            if (!File.Exists(nativeTarget))
                throw new Exception("Expected Unity native-plugin layout is missing: " + nativeTarget);
            File.Copy(Path.Combine(root,"Assets","Plugins","Windows",library),nativeTarget,true);
        }
        if (File.Exists("LICENSE")) File.Copy("LICENSE", Path.Combine(destination,"LICENSE"), true);
        File.Copy("FORMAT-BUILD.md", Path.Combine(destination,"README.txt"), true);
        Debug.Log("WINDOWS_FORMAT_BUILD_SUCCEEDED " + destination);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (string file in Directory.GetFiles(source))
        {
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
