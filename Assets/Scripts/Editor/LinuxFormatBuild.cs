using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class LinuxFormatBuild
{
    public static void Build() => Compile(true);
    public static void Recompile() => Compile(false);
    static void Compile(bool buildContent)
    {
        string root = Directory.GetCurrentDirectory();
        string destination = Environment.GetEnvironmentVariable("ULTRA_LINUX_OUTPUT") ?? Path.Combine(root, "release", "Linux-x86_64");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
        PlayerSettings.productName = "Ultra Duel";
        PlayerSettings.bundleVersion = MDPro3.Net.UltraServices.Version;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore });
        PlayerSettings.runInBackground = true;
        if (buildContent)
        {
        AddressableAssetSettings.BuildPlayerContent(out var content);
        if (!string.IsNullOrEmpty(content.Error)) throw new Exception(content.Error);
        string bundles = Path.Combine(root, "Platforms/StandaloneLinux64/MasterDuel/Built-in");
        Directory.CreateDirectory(bundles);
        var names = new[] { "shaders", "sprites", "materials", "outduel", "textures", "duel" };
        var builds = new AssetBundleBuild[names.Length];
        for (int i = 0; i < names.Length; i++)
            builds[i] = new AssetBundleBuild { assetBundleName = names[i], assetNames = AssetDatabase.GetAssetPathsFromAssetBundle(names[i]) };
        if (BuildPipeline.BuildAssetBundles(bundles, builds, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneLinux64) == null)
            throw new Exception("Linux duel assets did not build.");
        }
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { "Assets/Scenes/Boot.unity" },
            locationPathName = Path.Combine(destination, "Ultra-Duel.x86_64"),
            target = BuildTarget.StandaloneLinux64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Linux build failed: " + report.summary.result);
        Debug.Log("ULTRA_LINUX_PLAYER_BUILT " + destination);
    }
}
