using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class MacFormatBuild
{
    public static void Build()=>Compile(true);
    public static void Recompile()=>Compile(false);
    static void Compile(bool buildContent)
    {
        string target=Environment.GetEnvironmentVariable("ULTRA_MAC_OUTPUT")??Path.Combine(Directory.GetCurrentDirectory(),"release","Mac-arm64","Ultra-Duel-Player.app");
        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone,BuildTarget.StandaloneOSX);
        PlayerSettings.productName="Ultra Duel";PlayerSettings.bundleVersion="0.1.7";
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone,1);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneOSX,new[]{UnityEngine.Rendering.GraphicsDeviceType.Metal});
        PlayerSettings.runInBackground=true;
        if(buildContent)
        {
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent(out var content);
            if(!string.IsNullOrEmpty(content.Error))throw new Exception(content.Error);
            string bundles=Path.Combine(Directory.GetCurrentDirectory(),"Platforms/StandaloneOSX/MasterDuel/Built-in");
            Directory.CreateDirectory(bundles);
            var names=new[]{"shaders","sprites","materials","outduel","textures","duel"};
            var builds=new AssetBundleBuild[names.Length];
            for(int i=0;i<names.Length;i++)
                builds[i]=new AssetBundleBuild{assetBundleName=names[i],assetNames=AssetDatabase.GetAssetPathsFromAssetBundle(names[i])};
            if(BuildPipeline.BuildAssetBundles(bundles,builds,BuildAssetBundleOptions.ChunkBasedCompression,BuildTarget.StandaloneOSX)==null)
                throw new Exception("macOS built-in duel bundles did not build.");
        }
        UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings.BuildAddressablesWithPlayerBuild=AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Boot.unity"},locationPathName=target,target=BuildTarget.StandaloneOSX,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Apple Silicon player build failed: "+report.summary.result);
        Debug.Log("ULTRA_MAC_ARM64_PLAYER_BUILT "+target);
    }
}
