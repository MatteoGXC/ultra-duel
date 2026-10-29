using System;
using System.IO;
using UnityEngine;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.Networking;

namespace MDPro3
{
    public class Boot : MonoBehaviour
    {
        public Slider progressBar;
        public Text text;

        private string title;
        private string dots;
        private float time;
        private bool extracting;
        private int totalNum;
        private int nowNum;

        private readonly List<string> zips = new();

        private void Start()
        {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            Directory.SetCurrentDirectory(Path.GetDirectoryName(Application.dataPath));
#elif !UNITY_EDITOR && UNITY_STANDALONE_OSX
            Directory.SetCurrentDirectory(Environment.GetEnvironmentVariable("ULTRA_DUEL_DATA")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath,"../..")));
#elif !UNITY_EDITOR && UNITY_STANDALONE_LINUX
            Directory.SetCurrentDirectory(Environment.GetEnvironmentVariable("ULTRA_DUEL_DATA")
                ?? Path.GetDirectoryName(Application.dataPath));
            // Reuse the validated desktop font assets with Unix paths. The Linux
            // player, graphics resources, native engine, and duel bundles stay native.
            Addressables.InternalIdTransformFunc = location => location.InternalId
                .Replace('\\', '/').Replace("StandaloneWindows64/", "StandaloneLinux64/");
#endif
            UltraSaveSafety.Initialize();
            Application.targetFrameRate = 0;

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            Environment.CurrentDirectory = Application.persistentDataPath;
            Directory.SetCurrentDirectory(Application.persistentDataPath);
            if (VersionCheck())
            {
                BetterStreamingAssets.Initialize();
                var paths = BetterStreamingAssets.GetFiles("\\", "*.zip");
                foreach (var zip in paths)
                    zips.Add(Path.GetFileName(zip).Replace(".zip", ""));
                StartCoroutine(CheckFile());
            }
#elif !UNITY_EDITOR && UNITY_STANDALONE_OSX
            InitializeLanguage();
            StartCoroutine(LoadMainSceneAsync());
#else
            InitializeLanguage();
            StartCoroutine(LoadMainSceneAsync());
#endif
        }

        private void Update()
        {
            time += Time.deltaTime;
            if (time > 0.33f)
            {
                time = 0;
                dots += ".";
                if (dots == "....")
                    dots = "";
            }
            if (extracting && totalNum != 0)
            {
                float progress = (float)nowNum / totalNum;
                progressBar.value = progress;
            }
            if (totalNum == 0)
                text.text = title + dots;
            else
                text.text = title + "(" + nowNum + Program.STRING_SLASH + totalNum + ")";
        }

        private bool InitializeLanguage()
        {
            if (!Directory.Exists(Program.PATH_DATA))
            {
                Directory.CreateDirectory(Program.PATH_DATA);
                Config.Initialize(Program.PATH_CONFIG);
                return true;
            }
            else
            {
                Config.Initialize(Program.PATH_CONFIG);
                if(Config.Get("Version", "Version") == "Version")
                    return true;
                InterString.Initialize();
                return false;
            }
        }

        private IEnumerator CheckFile()
        {
            IEnumerator enumerator;
            foreach (string zip in zips)
            {
                if (!Config.GetBool(zip + "_install", false))
                {
                    enumerator = Check(zip);
                    StartCoroutine(enumerator);
                    while (enumerator.MoveNext())
                        yield return enumerator.Current;
                    Config.Set(zip + "_install", "1");
                    Config.Save();
                    GC.Collect();
                }
            }
            yield return null;
            StartCoroutine(LoadMainSceneAsync());
        }

        private IEnumerator Check(string type)
        {
            title = InterString.Get("正在读取[?]", type + ".zip");
            nowNum = 0;
            totalNum = 0;

            string tempZipPath = Path.Combine(Application.temporaryCachePath, type + "_temp.zip");

            string filePath = Path.Combine(Application.streamingAssetsPath, type + ".zip");
            using UnityWebRequest request = UnityWebRequest.Get(filePath);
            request.downloadHandler = new DownloadHandlerFile(tempZipPath);
            request.SendWebRequest();
            while (!request.isDone)
            {
                float progress = Mathf.Clamp01(request.downloadProgress / 0.9f);
                progressBar.value = progress;
                yield return null;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                title = InterString.Get("正在解压[?]", type + ".zip");
                string outPath = "";
                if (type.Contains("_"))
                    outPath = type.Split('_')[0];
                if (outPath.Length > 0 && !Directory.Exists(outPath))
                    Directory.CreateDirectory(outPath);

                IEnumerator extractEnumerator = ExtractZipFileFromFile(tempZipPath, outPath);
                while (extractEnumerator.MoveNext())
                    yield return extractEnumerator.Current;
            }
            else
                title = $"Decompression failed: {request.error}";
        }

        private IEnumerator LoadMainSceneAsync()
        {
            nowNum = 0;
            totalNum = 0;
            progressBar.value = 0;

            title = InterString.Get("正在初始化");

            Program.SetRoot();
            _ = ABLoader.CacheMasterDuelOutDuelBundles();
            while (!ABLoader.mdCached)
            {
                progressBar.value = ABLoader.mdCachedProgress;
                yield return null;
            }

            Config.Initialize(Program.PATH_CONFIG);
            Config.Set("Version", Application.version[..5]);
            Config.Save();

            var ini = Addressables.InitializeAsync();
            while (!ini.IsDone)
            {
                progressBar.value = ini.PercentComplete;
                yield return null;
            }

            title = InterString.Get("正在读取数据");
            var handle = Addressables.LoadAssetAsync<Items>("ScriptableObjects/Items.asset");
            while (!handle.IsDone)
            {
                progressBar.value = handle.PercentComplete;
                yield return null;
            }
            Program.items = handle.Result;

            title = InterString.Get("正在进入游戏");
            var load = Addressables.LoadSceneAsync("SceneMain");
            while (!load.IsDone)
            {
                yield return null;
                progressBar.value = load.PercentComplete;
            }
        }

        public static void FastExtractZipFile(string file, string dir, string password = "")
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            FastZip zip = new FastZip();
            zip.Password = password;
            zip.ExtractZip(file, dir, "");
        }

        private IEnumerator ExtractZipFile(byte[] data, string outFolder)
        {
            ZipFile zf = null;
            using MemoryStream mstrm = new(data);
            zf = new ZipFile(mstrm);
            int count = 0;
            foreach (ZipEntry zipEntry in zf)
                count++;
            totalNum = count;
            nowNum = 0;
            extracting = true;
            foreach (ZipEntry zipEntry in zf)
            {
                nowNum++;
                if (!zipEntry.IsFile)
                {
                    continue;
                }
                string entryFileName = zipEntry.Name;
                byte[] buffer = new byte[4096];
                Stream zipStream = zf.GetInputStream(zipEntry);
                string fullZipToPath = Path.Combine(outFolder, entryFileName);
                string directoryName = Path.GetDirectoryName(fullZipToPath);
                if (directoryName.Length > 0)
                    Directory.CreateDirectory(directoryName);
                using (FileStream streamWriter = File.Create(fullZipToPath))
                {
                    StreamUtils.Copy(zipStream, streamWriter, buffer);
                }
                yield return null;
            }
            if (zf != null)
            {
                zf.IsStreamOwner = true;
                zf.Close();
            }
        }

        private bool VersionCheck()
        {
            var firstInstall = InitializeLanguage();

            var installVersion = Application.version;
            var installedVersion = Config.Get("Version", "Version");
            if (installedVersion == "Version")
                firstInstall = true;

            if(firstInstall)
            {
                if(installVersion.Length > 5 || !installVersion.EndsWith("0"))
                {
                    title = "不能直接安装更新包。Can not install update apk directly.";
                    Directory.Delete(Program.PATH_DATA);
                    return false;
                }
                else
                    return true;
            }
            else //firstInstall
            {
                if(installVersion == installedVersion)
                    return true;

                if (installVersion.Length > 5)
                {
                    if(installVersion.EndsWith("0"))
                    {
                        if (InstallNext(installedVersion, installVersion))
                            return true;
                        else if (installVersion[..5] == installedVersion)
                            return true;
                        else
                        {
                            title = InterString.Get("当前更新包需要的版本：「[?]」。", VersionPre(installVersion));
                            title += InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                    else
                    {
                        if (installVersion.Substring(0, 5) == installedVersion)
                            return true;
                        else
                        {
                            title = InterString.Get("当前更新包需要的版本：「[?]」。", installVersion.Substring(0, 5));
                            title += InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                }
                else
                {
                    if (installVersion.EndsWith("0"))
                        return true;
                    else
                    {
                        if(VersionPre(installVersion) == installedVersion.Substring(0, 5))
                            return true;
                        else
                        {
                            title = InterString.Get("当前更新包需要的版本：「[?]」。", VersionPre(installVersion));
                            title += InterString.Get("已安装版本：「[?]」。", installedVersion);
                            return false;
                        }
                    }
                }
            }
        }

        private bool InstallNext(string installedVersion, string installVersion)
        {
            var installedInt = GetVersionInt(installedVersion);
            var installInt = GetVersionInt(installVersion);
            if(installInt -  installedInt == 1)
                return true;
            else
                return false;
        }

        private string VersionPre(string version)
        {
            var versionInt = GetVersionInt(version);
            string returnValue = (versionInt - 1).ToString("D3");
            return returnValue.Substring(0, 1) + "." + returnValue.Substring(1, 1) + "." + returnValue.Substring(2, 1);
        }

        private int GetVersionInt(string version)
        {
            string textInt = version.Substring(0, 1) + version.Substring(2, 1) + version.Substring(4, 1);
            return int.Parse(textInt);
        }

        private IEnumerator ExtractZipFileFromFile(string zipPath, string outFolder)
        {
            ZipFile zf = null;
            try
            {
                zf = new ZipFile(zipPath);
                int count = 0;
                foreach (ZipEntry zipEntry in zf)
                    count++;
                totalNum = count;
                nowNum = 0;
                extracting = true;

                foreach (ZipEntry zipEntry in zf)
                {
                    nowNum++;
                    if (!zipEntry.IsFile)
                        continue;

                    string entryFileName = zipEntry.Name;
                    byte[] buffer = new byte[4096];
                    Stream zipStream = zf.GetInputStream(zipEntry);
                    string fullZipToPath = Path.Combine(outFolder, entryFileName);
                    string directoryName = Path.GetDirectoryName(fullZipToPath);
                    if (directoryName.Length > 0)
                        Directory.CreateDirectory(directoryName);

                    using (FileStream streamWriter = File.Create(fullZipToPath))
                    {
                        StreamUtils.Copy(zipStream, streamWriter, buffer);
                    }

                    yield return null;
                }
            }
            finally
            {
                if (zf != null)
                {
                    zf.IsStreamOwner = true;
                    zf.Close();
                }
                extracting = false;
            }
        }

    }
}
