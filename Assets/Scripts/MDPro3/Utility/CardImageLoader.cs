using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace MDPro3.Utility
{
    public static class CardImageLoader
    {

        #region 字段和属性

        private class VideoCacheEntry
        {
            public Texture Texture;
            public CardRenderer Renderer;
            public int ReferenceCount;
            public bool IsPersistent;
            public Task<(Texture Texture, CardRenderer Renderer)> LoadingTask;
        }

        public const float DEFAULT_ASPECT = 704 / 1024f;

        private static readonly ConcurrentDictionary<int, CacheEntry> cachedArts = new();
        private static readonly ConcurrentDictionary<int, CacheEntry> cachedCards = new();
        private static readonly ConcurrentDictionary<int, Texture2D> cachedCardNames = new();
        private static readonly ConcurrentDictionary<int, Texture2D> cachedOverFrames = new();
        private static readonly ConcurrentDictionary<int, VideoCacheEntry> cachedVideoCards = new();

        private static readonly ConcurrentDictionary<int, SemaphoreSlim> artLoadingLocks = new();
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> overFrameLoadingLocks = new();
        private static readonly ConcurrentDictionary<int, SemaphoreSlim> cardLoadingLocks = new();

        private static readonly List<CardRenderer> videos = new();

        public static bool lastCardFoundArt;
        public static bool lastCardRenderSucceed;

        private static SemaphoreSlim artSemaphore;
        private static SemaphoreSlim overFrameSemaphore;
        private static SemaphoreSlim cardSemaphore;
        private static int maxLoads;

        #endregion

        #region 初始化

        public static void Initialize()
        {
            maxLoads = GetOptimalConcurrency();

            artSemaphore = new SemaphoreSlim(maxLoads, maxLoads);
            overFrameSemaphore = new SemaphoreSlim(maxLoads, maxLoads);
            cardSemaphore = new SemaphoreSlim(maxLoads, maxLoads);

            _ = InitializeArtFileListAsync();
            _ = InitializeVideoArtFileListAsync();
            _ = InitializeOverFrameFileListAsync();

            SystemEvent.OnVideoCardConfigChange += CheckArtVideoConfig;
        }

        private static int GetOptimalConcurrency()
        {
            if (DeviceInfo.OnMobile())
                return 1;
            else
                return 2;
        }

        #endregion

        #region API

        public static async UniTask<Texture2D> LoadArtAsync(
            int code,
            bool persistent = false,
            CancellationToken token = default)
        {
            var lockObj = artLoadingLocks.GetOrAdd(code, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(token);

            try
            {
                if (cachedArts.TryGetValue(code, out var entry) && entry.Texture == null && entry.LoadingTask == null)
                    cachedArts.TryRemove(code, out _);
                if (cachedArts.TryGetValue(code, out entry))
                {

                    if (entry.LoadingTask != null)
                        await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                    if (entry.Texture != null)
                    {

                        Interlocked.Increment(ref entry.ReferenceCount);
                        entry.IsPersistent |= persistent;
                    }
                    else
                        Debug.LogError($"Art texture is null for code {code}");

                    return entry.Texture;
                }

                CacheEntry newEntry = new()
                {
                    LoadingTask = InternalLoadArtAsync(code, token)
                };

                if (cachedArts.TryAdd(code, newEntry))
                {

                    newEntry.ReferenceCount = 1;
                    newEntry.IsPersistent = persistent;
                    try
                    {
                        newEntry.Texture = await newEntry.LoadingTask;
                    }
                    catch(OperationCanceledException ex)
                    {
                        cachedArts.TryRemove(code, out _);
                        throw ex;
                    }
                    if (newEntry.Texture == null)
                        cachedArts.TryRemove(code, out _);

                    newEntry.LoadingTask = null;
                    return newEntry.Texture;
                }
                else
                {
                    Debug.LogError("CardImageLoader: Unexpected Errror.");
                    return null;
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                lockObj.Release();

            }
        }

        public static void ReleaseArt(int code)
        {
            if (!cachedArts.TryGetValue(code, out var entry)) return;

            var newCount = Interlocked.Decrement(ref entry.ReferenceCount);

            if(newCount < 0)
                Debug.LogError($"Art reference count for code {code} is less than zero.");

            if (newCount == 0 && !entry.IsPersistent)
                if (cachedArts.TryRemove(code, out _))
                    UnityEngine.Object.Destroy(entry.Texture);
        }

        public static async UniTask<Texture2D> LoadOverFrameAsync(
            int code,
            CancellationToken token = default)
        {
            var lockObj = overFrameLoadingLocks.GetOrAdd(code, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(token);

            try
            {
                if (cachedOverFrames.TryGetValue(code, out var tex))
                    return tex;

                var of = await InternalLoadOverFrameAsync(code, token);
                if(of != null)
                    cachedOverFrames[code] = of;
                return of;
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                lockObj.Release();
                overFrameLoadingLocks.TryRemove(code, out _);
            }
        }


        public static async UniTask<Texture> LoadCardAsync(
            int code,
            bool persistent = false,
            CancellationToken token = default,
            bool forceTexture = false)
        {
            var lockObj = cardLoadingLocks.GetOrAdd(code, _ => new SemaphoreSlim(1, 1));
            await lockObj.WaitAsync(token);
            try
            {
                if (!CardRenderer.CardHasVideoArt(code) || forceTexture)
                {
                    if (cachedCards.TryGetValue(code, out var entry) && entry.Texture == null && entry.LoadingTask == null)
                        cachedCards.TryRemove(code, out _);
                    if (cachedCards.TryGetValue(code, out entry))
                    {
                        if (entry.LoadingTask != null)
                            await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                        if (entry.Texture != null)
                        {
                            Interlocked.Increment(ref entry.ReferenceCount);
                            entry.IsPersistent |= persistent;
                        }
                        else
                            Debug.LogError($"Card texture is null for code {code}");
                        return entry.Texture;
                    }

                    CacheEntry newEntry = new()
                    {
                        LoadingTask = InternalLoadCardAsync(code, token)
                    };

                    if (cachedCards.TryAdd(code, newEntry))
                    {
                        newEntry.ReferenceCount = 1;
                        newEntry.IsPersistent = persistent;
                        try
                        {
                            newEntry.Texture = await newEntry.LoadingTask;
                        }
                        catch (OperationCanceledException ex)
                        {
                            cachedCards.TryRemove(code, out _);
                            throw ex;
                        }
                        newEntry.LoadingTask = null;
                        if (newEntry.Texture == null) cachedCards.TryRemove(code, out _);
                        return newEntry.Texture;
                    }
                    else
                    {
                        Debug.LogError("CardImageLoader: Unexpected Error.");
                        return null;
                    }
                }
                else
                {
                    try
                    {
                        if (cachedVideoCards.TryGetValue(code, out var entry))
                        {
                            if (entry.LoadingTask != null)
                                await entry.LoadingTask.AsUniTask().AttachExternalCancellation(token);
                            if (entry.Texture != null)
                            {
                                Interlocked.Increment(ref entry.ReferenceCount);
                                entry.IsPersistent |= persistent;
                            }
                            else
                                Debug.LogError($"Video card texture is null for code {code}");
                            return entry.Texture;
                        }

                        VideoCacheEntry newEntry = new()
                        {
                            LoadingTask = InternalLoadVideoCardAsync(code, token).AsTask()
                        };

                        if (cachedVideoCards.TryAdd(code, newEntry))
                        {
                            newEntry.ReferenceCount = 1;
                            newEntry.IsPersistent = persistent;
                            try
                            {
                                var result = await newEntry.LoadingTask;
                                newEntry.Texture = result.Texture;
                                newEntry.Renderer = result.Renderer;
                            }
                            catch (OperationCanceledException ex)
                            {
                                cachedVideoCards.TryRemove(code, out _);
                                throw ex;
                            }
                            if (newEntry.Texture == null)
                                Debug.LogError($"{code} video card is null");

                            newEntry.LoadingTask = null;
                            return newEntry.Texture;
                        }
                        else
                        {
                            Debug.LogError("CardImageLoader: Unexpected Error.");
                            return null;
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                lockObj.Release();

            }
        }

        public static void ReleaseCard(int code)
        {
            // 普通卡片释放
            if (cachedCards.TryGetValue(code, out var entry))
            {
                var newCount = Interlocked.Decrement(ref entry.ReferenceCount);
                if (newCount == 0 && !entry.IsPersistent)
                    if (cachedCards.TryRemove(code, out _))
                        UnityEngine.Object.Destroy(entry.Texture);
                return;
            }

            // 视频卡释放
            if (cachedVideoCards.TryGetValue(code, out var videoEntry))
            {
                var newCount = Interlocked.Decrement(ref videoEntry.ReferenceCount);
                if (newCount == 0 && !videoEntry.IsPersistent)
                {
                    if (cachedVideoCards.TryRemove(code, out _))
                    {
                        if (videoEntry.Renderer != null)
                        {
                            videoEntry.Renderer.Dispose();
                            videos.Remove(videoEntry.Renderer);
                        }
                    }
                }
            }
        }

        public static Texture2D LoadCardName(int code)
        {
            if (cachedCardNames.TryGetValue(code, out var tex))
                return tex;

            tex = InternalLoadCardName(code);
            cachedCardNames.TryAdd(code, tex);

            return tex;
        }

        public static void ClearCache()
        {
            // 普通卡片
            foreach (var card in cachedCards.Values)
                UnityEngine.Object.Destroy(card.Texture);
            cachedCards.Clear();

            foreach (var cardNameTex in cachedCardNames.Values)
                UnityEngine.Object.Destroy(cardNameTex);
            cachedCardNames.Clear();

            // 视频卡
            foreach (var video in cachedVideoCards.Values)
            {
                if (video.Renderer != null)
                    video.Renderer.Dispose();
            }
            cachedVideoCards.Clear();
            videos.Clear();
        }

        public static int GetCardReferenceCount(int code)
        {
            if (cachedCards.TryGetValue(code, out var entry))
                return entry.ReferenceCount;
            if (cachedVideoCards.TryGetValue(code, out var videoEntry))
                return videoEntry.ReferenceCount;
            return 0;
        }

        #endregion

        #region Private

        private class CacheEntry
        {
            public Texture2D Texture;
            public int ReferenceCount;
            public bool IsPersistent;
            public Task<Texture2D> LoadingTask;
        }

        private static async Task<Texture2D> InternalLoadArtAsync(
            int code,
            CancellationToken token)
        {
            await artSemaphore.WaitAsync(token);
            lastCardFoundArt = true;

            try
            {
                await InitializeArtFileListAsync();
                var path = GetArtFilePath(code);
                if (string.IsNullOrEmpty(path))
                {
                    bool needCrop = false;
                    var art = await LoadArtFromZipAsync("art", code, token);
                    if (art == null)
                    {
                        needCrop = true;
                        art = await  LoadArtFromZipAsync("pics", code, token);
                    }
                    if (art == null)
                    {
                        lastCardFoundArt = false;
                        return null;
                    }

                    if (needCrop)
                        art = CropCardToArt(art, code);
                    return art;
                }

                using var request = UnityWebRequestTexture.GetTexture(path);
                try
                {
                    await request.SendWebRequest().ToUniTask(cancellationToken: token);
                }
                catch (OperationCanceledException ex) when (token.IsCancellationRequested)
                {
                    request.Abort();
                    throw ex;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"加载失败: {ex.Message}");
                    lastCardFoundArt = false;
                    throw ex;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"加载失败: {request.error}");
                    lastCardFoundArt = false;
                    return null;
                }
                return DownloadHandlerTexture.GetContent(request);
            }
            finally 
            {
                artSemaphore.Release(); 
            }
        }

        private static async UniTask<Texture2D> InternalLoadOverFrameAsync(int code, CancellationToken token)
        {
            await overFrameSemaphore.WaitAsync(token);

            try
            {
                if (!CardHasOverFrame(code))
                    return null;
                var path = Tools.GetPlatformFilePath(Program.PATH_OVER_FRAME + code + ".png");
                path=new Uri(Path.GetFullPath(path)).AbsoluteUri;
                using var request = UnityWebRequestTexture.GetTexture(path);
                try
                {
                    await request.SendWebRequest().ToUniTask(cancellationToken: token);
                }
                catch (OperationCanceledException ex) when (token.IsCancellationRequested)
                {
                    request.Abort();
                    throw ex;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"加载失败: {ex.Message}");
                    throw ex;
                }

                if(request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"加载失败: {request.error}");
                    return null;
                }
                var tex = DownloadHandlerTexture.GetContent(request);
                var aspect = tex.width / (float)tex.height;
                if(aspect > DEFAULT_ASPECT)
                    tex = TextureProcessor.PadBottomToMatchAspect(tex, DEFAULT_ASPECT);
                return tex;
            }
            finally
            {
                overFrameSemaphore.Release();
            }
        }

        private static async Task<Texture2D> InternalLoadCardAsync(
            int code,
            CancellationToken token)
        {
            await UniTask.WaitUntil(() => TextureManager.container != null, cancellationToken: token);
            await cardSemaphore.WaitAsync(token);
            lastCardRenderSucceed = true;

            try
            {

                var data = CardsManager.Get(code, true);
                if (data.Id == 0)
                {
                    lastCardRenderSucceed = false;
                    return TextureManager.container.unknownCard.texture;
                }


                var art = await LoadArtAsync(code, false, token).AttachExternalCancellation(token);
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (art == null)
                {
                    Debug.LogError($"Get null from ArtLoad for Card {data.Id}:");
                    art = TextureManager.container.unknownArt.texture;
                }

                Texture2D overFrame = await LoadOverFrameAsync(code, token).AttachExternalCancellation(token);

                if (!Program.instance.cardRenderer.RenderCard(code, art, overFrame))
                {
                    lastCardRenderSucceed = false;
                    return TextureManager.container.unknownCard.texture;
                }

                RenderTexture.active = Program.instance.cardRenderer.renderTexture;
                var returnValue = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGB24, true);
                returnValue.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);
                returnValue.Apply();
                returnValue.name = "Card_" + code;
                ReleaseArt(code);

                return returnValue;
            }
            finally { cardSemaphore.Release(); }
        }

        private static async UniTask<(Texture Texture, CardRenderer Renderer)> InternalLoadVideoCardAsync(
            int code,
            CancellationToken token)
        {
            await UniTask.WaitUntil(() => TextureManager.container != null, cancellationToken: token);
            await cardSemaphore.WaitAsync(token);
            lastCardRenderSucceed = true;

            try
            {
                var data = CardsManager.Get(code, true);
                if (data.Id == 0)
                {
                    lastCardRenderSucceed = false;
                    return (TextureManager.container.unknownCard.texture, null);
                }
                var art = await LoadArtAsync(code, false, token).AttachExternalCancellation(token);
                if (token.IsCancellationRequested)
                    throw new OperationCanceledException(token);

                if (art == null)
                {
                    Debug.LogError($"Get null from ArtLoad for Card {data.Id}:");
                    art = TextureManager.container.unknownArt.texture;
                }

                var cardRenderer = (await Addressables.InstantiateAsync("Prefab/CardRenderer.prefab")).GetComponent<CardRenderer>();
                videos.Add(cardRenderer);
                var texture = await cardRenderer.GetVideoCardAsync(code);
                return (texture, cardRenderer);
            }
            finally { cardSemaphore.Release(); }
        }

        private static Texture2D InternalLoadCardName(int code)
        {
            Program.instance.cardRenderer.RenderName(code);
            RenderTexture.active = Program.instance.cardRenderer.renderTexture;
            var result = new Texture2D(RenderTexture.active.width, 203, TextureFormat.RGBA32, false);
            var rect = new Rect(0, Program.instance.cardRenderer.renderTexture.height - 203
                , Program.instance.cardRenderer.renderTexture.width, 203);
            result.ReadPixels(rect, 0, 0);
            result.Apply();
            result.wrapMode = TextureWrapMode.Clamp;
            return result;
        }

        private static async UniTask<Texture2D> LoadArtFromZipAsync(
            string folder,
            int code,
            CancellationToken token)
        {
            MemoryStream stream = null;
            var targetPNG = $"{folder.ToLower()}/{code}{Program.EXPANSION_PNG.ToLower()}";
            var targetJPG = $"{folder.ToLower()}/{code}{Program.EXPANSION_JPG.ToLower()}";

            try
            {
                foreach (var zip in ZipHelper.zips)
                {
                    if (zip.Name.ToLower().EndsWith(Program.SCRIPT_ZIP))
                        continue;
                    foreach (var file in zip.EntryFileNames)
                    {
                        if (file.ToLower().Replace("\\", "/") == targetPNG || file.ToLower().Replace("\\", "/") == targetJPG)
                        {
                            stream = Tools.GetStream();
                            var entry = zip[file];
                            entry.Extract(stream);
                            break;
                        }
                    }
                }

                await UniTask.Yield(cancellationToken: token);

                if (stream != null)
                {
                    Texture2D returnValue = new(0, 0);
                    returnValue.LoadImage(stream.ToArray());
                    return returnValue;
                }
                else
                    return null;
            }
            catch (Exception e)
            {
                throw e;
            }            
            finally
            {
                if(stream != null)
                    Tools.ReturnStream(stream);
            }
        }

        private static Texture2D CropCardToArt(Texture2D pic, int code)
        {
            var data = CardsManager.Get(code);
            if (code >= 120000000 && code < 130000000)
            {
                if (data.HasType(CardType.Monster))
                    return GetArtFromRushDuelMonsterCard(pic);
                else
                    return GetArtFromRushDuelSpellCard(pic);
            }
            else if (data.HasType(CardType.Pendulum))
                return GetArtFromPendulumCard(pic);
            else
                return GetArtFromCard(pic);
        }

        private static void CheckArtVideoConfig()
        {
            var config = Config.GetBool("VideoCard", true);
            if (!config)
                ClearArtVideos();
        }

        private static void ClearArtVideos()
        {
            foreach (var video in cachedVideoCards.Values)
            {
                if (video.Texture != null)
                    UnityEngine.Object.Destroy(video.Texture);
                if (video.Renderer != null)
                    video.Renderer.Dispose();
            }
            cachedVideoCards.Clear();
            videos.Clear();
        }

        #endregion

        #region Crop Texture

        private static Texture2D GetArtFromCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.13f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.3f);
            var width = Mathf.CeilToInt(cardPic.width * 0.87f);
            var height = Mathf.CeilToInt(cardPic.height * 0.81f);
            return TextureProcessor.GetCroppingTex(cardPic, startX, startY, width, height);
        }

        private static Texture2D GetArtFromPendulumCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.067f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.38f);
            var width = Mathf.CeilToInt(cardPic.width * 0.933f);
            var height = Mathf.CeilToInt(cardPic.height * 0.81f);
            return TextureProcessor.GetCroppingTex(cardPic, startX, startY, width, height);
        }

        private static Texture2D GetArtFromRushDuelMonsterCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.067f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.29f);
            var width = Mathf.CeilToInt(cardPic.width * 0.933f);
            var height = Mathf.CeilToInt(cardPic.height * 0.90f);
            return TextureProcessor.GetCroppingTex(cardPic, startX, startY, width, height);
        }

        private static Texture2D GetArtFromRushDuelSpellCard(Texture2D cardPic)
        {
            var startX = Mathf.CeilToInt(cardPic.width * 0.067f);
            var startY = Mathf.CeilToInt(cardPic.height * 0.29f);
            var width = Mathf.CeilToInt(cardPic.width * 0.933f);
            var height = Mathf.CeilToInt(cardPic.height * 0.90f);
            return TextureProcessor.GetCroppingTex(cardPic, startX, startY, width, height);
        }

        #endregion

        #region Art File List Cache

        private static readonly Dictionary<int, string> artFileList = new();
        private static readonly Dictionary<int, string> artAltFileList = new();
        private static bool artFileListInitialized;
        private static bool artFileListInitializing;

        private static async UniTask InitializeArtFileListAsync()
        {
            string artDir = Tools.GetPlatformPath(Program.PATH_ART);
            bool artDirExists = Directory.Exists(artDir);
            // A scan that ran before Picture was beside the build must not stick.
            if (artFileListInitialized && artFileList.Count == 0 && artDirExists)
            {
                artFileListInitialized = false;
                artFileList.Clear();
                artAltFileList.Clear();
            }
            if (artFileListInitialized) return;
            if (artFileListInitializing)
            {
                await UniTask.WaitUntil(() => !artFileListInitializing);
                if (artFileListInitialized) return;
            }
            artFileListInitializing = true;
            try
            {
                await UniTask.Yield();
                if (!artDirExists)
                {
                    // Leave the list uninitialized so the next load tries the build folder again.
                    return;
                }

                foreach (var file in Directory.GetFiles(artDir))
                {
                    var extension = Path.GetExtension(file).ToLowerInvariant();
                    if (extension != ".jpg" && extension != ".png" && extension != ".jpeg") continue;
                    if (int.TryParse(Path.GetFileNameWithoutExtension(file), out var code))
                        artFileList[code] = extension;
                }

                var alt = Tools.GetPlatformPath(Program.PATH_ALT_ART);
                if (Directory.Exists(alt))
                {
                    foreach (var file in Directory.GetFiles(alt, "*.jpg"))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (int.TryParse(fileName, out var code) && !artAltFileList.ContainsKey(code))
                            artAltFileList.Add(code, Program.EXPANSION_JPG);
                    }
                    foreach (var file in Directory.GetFiles(alt, "*.png"))
                    {
                        var fileName = Path.GetFileNameWithoutExtension(file);
                        if (int.TryParse(fileName, out var code))
                            if (!artAltFileList.ContainsKey(code))
                                artAltFileList.Add(code, Program.EXPANSION_PNG);
                    }
                }

                artFileListInitialized = true;
            }
            finally
            {
                artFileListInitializing = false;
            }
        }

        private static string GetArtFilePath(int code)
        {
            string path = string.Empty;
            if (artAltFileList.ContainsKey(code))
                path = Program.PATH_ALT_ART + code + artAltFileList[code];
            else if (artFileList.TryGetValue(code, out var extension))
                path = Program.PATH_ART + code + extension;

            if (!string.IsNullOrEmpty(path))
                path = Tools.GetPlatformFilePath(path);

            return path;
        }

        #endregion

        #region Video Art File List Cache

        private static readonly List<int> videoArtFileList = new();
        private static bool videoArtFileListInitialized;
        private static async UniTask InitializeVideoArtFileListAsync()
        {
            if (videoArtFileListInitialized) return;

            var path = Tools.GetPlatformPath(Program.PATH_VIDEO_ART);

            if (Directory.Exists(path))
            {
                //await UniTask.SwitchToThreadPool();
                await UniTask.Yield();
                foreach (var file in Directory.GetFiles(path, "*.mp4"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (int.TryParse(fileName, out var code))
                        videoArtFileList.Add(code);
                }
            }

            videoArtFileListInitialized = true;
        }

        public static bool CardHasVideoArt(int code)
        {
            return videoArtFileList.Contains(code);
        }

        public static void ReloadArtVideos()
        {
            videoArtFileListInitialized = false;
            _ = InitializeVideoArtFileListAsync();
            ClearArtVideos();
        }

        #endregion

        #region Over Frame Cache

        private static readonly List<int> overFrameFileList = new();
        private static bool overFrameFileListInitialized;
        private static async UniTask InitializeOverFrameFileListAsync()
        {
            if (overFrameFileListInitialized) return;
            var path = Tools.GetPlatformPath(Program.PATH_OVER_FRAME);
            if (Directory.Exists(path))
            {
                //await UniTask.SwitchToThreadPool();
                await UniTask.Yield();
                foreach (var file in Directory.GetFiles(path, "*.png"))
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    if (int.TryParse(fileName, out var code))
                        overFrameFileList.Add(code);
                }
            }
            overFrameFileListInitialized = true;
        }

        public static bool CardHasOverFrame(int code)
        {
            return overFrameFileList.Contains(code);
        }

        public static Texture2D GetOverFrame(int code)
        {
            if(cachedOverFrames.ContainsKey(code))
                return cachedOverFrames[code];
            return null;
        }

        #endregion

    }
}
