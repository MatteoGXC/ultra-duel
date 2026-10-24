using Cysharp.Threading.Tasks;
using Spine.Unity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using Willow;
using Willow.InGameField;
using YgomGame;

namespace MDPro3
{
    public class ABLoader
    {
        public static Dictionary<string, GameObject> cachedAB = new();
        public static Dictionary<string, GameObject> cachedABFolder = new();
        public static Dictionary<string, Material> cachedPMat = new();
        public static Dictionary<string, Material> cachedFrameMat = new();
        private static readonly SemaphoreSlim protectorSemaphoreSlim = new(1, 1);
        private static readonly SemaphoreSlim frameSemaphoreSlim = new(1, 1);

        public static async UniTask<AssetBundle> CacheFromFileAsync(string path)
        {
            return await AssetBundle.LoadFromFileAsync(path);
        }

        public static GameObject LoadFromFile(string path, bool cache, bool instantiate)
        {
            if (cachedAB.TryGetValue(path, out var returnValue))
            {
                if (instantiate)
                    return UnityEngine.Object.Instantiate(returnValue);
                else
                    return returnValue;
            }

            AssetBundle ab;
            ab = AssetBundle.LoadFromFile(Program.root + path);
            var assets = ab.LoadAllAssets();
            foreach (UnityEngine.Object asset in assets)
            {
                if (typeof(GameObject).IsInstanceOfType(asset))
                {
                    if (cache)
                        if (!cachedAB.TryAdd(path, asset as GameObject))
                            Debug.LogError($"Failed to cache {path}");
                    returnValue = asset as GameObject;
                    break;
                }
            }

            ab.Unload(false);
            if (instantiate && returnValue != null)
                return UnityEngine.Object.Instantiate(returnValue);
            else
                return returnValue;
        }

        public static async UniTask<GameObject> LoadFromFileAsync(string path, bool cache, bool instantiate)
        {
            if (cachedAB.TryGetValue(path, out GameObject returnValue))
            {
                if (instantiate)
                    return UnityEngine.Object.Instantiate(returnValue);
                else
                    return returnValue;
            }

            AssetBundle ab = await AssetBundle.LoadFromFileAsync(Program.root + path);
            var expectedName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(expectedName))
            {
                var assetRequest = ab.LoadAssetAsync<GameObject>(expectedName);
                await assetRequest;
                returnValue = assetRequest.asset as GameObject;
            }

            if (returnValue == null)
            {
                var assets = ab.LoadAllAssets();
                foreach (UnityEngine.Object asset in assets)
                {
                    if (!typeof(GameObject).IsInstanceOfType(asset))
                        continue;

                    var candidate = asset as GameObject;
                    returnValue = candidate;
                    if (candidate != null && candidate.name == expectedName)
                        break;
                }
            }
            ab.Unload(false);

            if (cache && returnValue != null)
            {
                if (!cachedAB.TryAdd(path, returnValue))
                    Debug.LogWarning($"Failed to cache {path}");
            }

            if (instantiate && returnValue != null)
                return UnityEngine.Object.Instantiate(returnValue);
            else
                return returnValue;
        }

        public static GameObject LoadFromFolder<T>(string path, bool cache, bool instantiate) where T : Component
        {
            if (cachedABFolder.TryGetValue(path, out var returnValue))
            {
                if (instantiate)
                    return UnityEngine.Object.Instantiate(returnValue);
                else
                    return returnValue;
            }

            DirectoryInfo dir = new(Program.root + path);
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            dir = new DirectoryInfo(Path.Combine(Application.dataPath, Program.root + path));
#endif
            FileInfo[] files = dir.GetFiles("*");
            List<AssetBundle> bundles = new();
            for (int i = 0; i < files.Length; i++)
                bundles.Add(AssetBundle.LoadFromFile(files[i].FullName));
            List<GameObject> loadedPrefabs = new();
            foreach (AssetBundle bundle in bundles)
            {
                var prefabs = bundle.LoadAllAssets();
                for (int j = 0; j < prefabs.Length; j++)
                    if (typeof(GameObject).IsInstanceOfType(prefabs[j]))
                        loadedPrefabs.Add(prefabs[j] as GameObject);
            }

            foreach (var prefab in loadedPrefabs)
                if (prefab.TryGetComponent<T>(out _))
                {
                    returnValue = prefab;
                    break;
                }
            foreach (AssetBundle bundle in bundles)
                bundle.Unload(false);
            if(cache && returnValue != null)
                cachedABFolder.TryAdd(path, returnValue);

            if (returnValue == null)
                Debug.Log($"LoadFromFolderAsync get null: {path}");

            if (instantiate)
            {
                if (returnValue != null)
                    return UnityEngine.Object.Instantiate(returnValue);
                else
                    return UnityEngine.Object.Instantiate(loadedPrefabs[0]);
            }
            else
            {
                if (returnValue != null)
                    return returnValue;
                else
                    return loadedPrefabs[0];
            }
        }

        public static GameObject LoadFromFolder(string path)
        {
            DirectoryInfo dir = new(Program.root + path);
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            dir = new DirectoryInfo(Path.Combine(Application.dataPath, Program.root + path));
#endif
            FileInfo[] files = dir.GetFiles("*");
            List<AssetBundle> bundles = new();
            for (int i = 0; i < files.Length; i++)
                bundles.Add(AssetBundle.LoadFromFile(files[i].FullName));

            var go = new GameObject(Path.GetFileName(path));
            foreach(var bundle in bundles)
            {
                var prefabs = bundle.LoadAllAssets();
                for (int i = 0; i < prefabs.Length; i++)
                {
                    if (typeof(GameObject).IsInstanceOfType(prefabs[i]))
                    {
                        var p =UnityEngine.Object.Instantiate((GameObject)prefabs[i]);
                        p.transform.SetParent(go.transform);
                    }
                }
            }

            foreach (AssetBundle bundle in bundles)
                bundle.Unload(false);

            return go;
        }

        public static async UniTask<GameObject> LoadFromFolderAsync<T>(string path, bool cache, bool instantiate
            , Action<GameObject , List<UnityEngine.Object>> processEvent = null ) where T : Component
        {
            if (cachedABFolder.TryGetValue(path, out var returnValue))
            {
                if (instantiate)
                    return UnityEngine.Object.Instantiate(returnValue);
                else
                    return returnValue;
            }

            DirectoryInfo dir = new(Program.root + path);
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            dir = new DirectoryInfo(Path.Combine(Application.dataPath, Program.root + path));
#endif
            if (!dir.Exists)
            {
                Debug.LogError($"[LoadFromFolderAsync]: Path [{dir.FullName}] does not exist.");
                return null;
            }
            FileInfo[] files = dir.GetFiles("*");
            List<AssetBundle> bundles = new();
            for (int i = 0; i < files.Length; i++)
                bundles.Add(await AssetBundle.LoadFromFileAsync(files[i].FullName));

            var allAssets = new List<UnityEngine.Object>();
            foreach (AssetBundle bundle in bundles)
            {
                var assets = bundle.LoadAllAssets();
                allAssets.AddRange(assets.ToList());
            }

            var allPrefabs = new List<GameObject>();
            foreach (var asset in allAssets)
                if (typeof(GameObject).IsInstanceOfType(asset))
                    allPrefabs.Add(asset as GameObject);

            foreach (var prefab in allPrefabs)
                if (prefab.TryGetComponent<T>(out _))
                {
                    returnValue = prefab;
                    break;
                }

            foreach (AssetBundle bundle in bundles)
                bundle.Unload(false);

            if (returnValue != null)
                processEvent?.Invoke(returnValue, allAssets);

            if (cache && returnValue != null)
                cachedABFolder.TryAdd(path, returnValue);

            if (returnValue == null)
                Debug.Log($"[ABLoader]: LoadFromFolderAsync get no GameObject: {path}");

            if (instantiate)
            {
                if(returnValue != null)
                    return UnityEngine.Object.Instantiate(returnValue);
                else
                    return UnityEngine.Object.Instantiate(allPrefabs[0]);
            }
            else
            {
                if (returnValue != null)
                    return returnValue;
                else
                    return allPrefabs[0];
            }
        }

        public static async UniTask<List<GameObject>> LoadsFromFolderAsync<T>(string path)
        {
            var returnValue = new List<GameObject>();

            DirectoryInfo dir = new(Program.root + path);
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            dir = new DirectoryInfo(Path.Combine(Application.dataPath, Program.root + path));
#endif

            FileInfo[] files = dir.GetFiles("*");
            List<AssetBundle> bundles = new();
            for (int i = 0; i < files.Length; i++)
                bundles.Add(await AssetBundle.LoadFromFileAsync(files[i].FullName));

            var loadedPrefabs = new List<GameObject>();
            foreach (AssetBundle bundle in bundles)
            {
                var prefabs = bundle.LoadAllAssets();
                foreach(var prefab in prefabs)
                    if (typeof(GameObject).IsInstanceOfType(prefab))
                        loadedPrefabs.Add(prefab as GameObject);
            }

            foreach (var prefab in loadedPrefabs)
                if (prefab.TryGetComponent<T>(out _))
                    returnValue.Add(prefab);

            foreach (AssetBundle bundle in bundles)
                bundle.Unload(false);

            return returnValue;
        }

        public static async UniTask<GameObject> LoadMonsterCutinAsync(int code, bool cache)
        {
            var path = $"MonsterCutin/{code}";
            return await LoadFromFolderAsync<PlayableDirector>(path, cache, true, FindAndSetSkeletonDataAsset);
        }

        private static void FindAndSetSkeletonDataAsset(GameObject prefab, List<UnityEngine.Object> assets)
        {
            //召唤兽 梅尔卡巴[75286622]在安卓端和iOS端的Spine动画资源丢失，
            //临时修复方案为从已加载的AssetBundle中寻找SkeletonDataAsset并赋值。

            //傻缺科乐美在 混沌幻魔[60110982]的Root节点留了个没删的SkeletonAnimation。

            var sas = prefab.GetComponentsInChildren<SkeletonAnimation>();
            if(sas == null || sas.Length == 0 || sas.Length > 1)
                return;

            var sa = sas[0];
            if (sa == null)
                return;
            if (sa.skeletonDataAsset == null)
            {
                foreach (var asset in assets)
                    if (asset is SkeletonDataAsset sda)
                    {
                        sa.skeletonDataAsset = sda;
                        break;
                    }
            }
        }

        private static Shader defaultProtectorShader;
        private const string defaultProtectorCode = "1070002";
        public static async UniTask<Material> LoadProtectorMaterial(string code, CancellationToken token)
        {
            if(defaultProtectorShader == null)
                await InternalLoadProtectorMaterialAsync(defaultProtectorCode, token);
            return await InternalLoadProtectorMaterialAsync(code, token);
        }

        public static async UniTask<Material> InternalLoadProtectorMaterialAsync(string code, CancellationToken token)
        {
            await protectorSemaphoreSlim.WaitAsync(token);

            try
            {
                if (code == Items.CODE_RANDOM.ToString())
                    code = Program.items.GetRandomItem(Items.ItemType.Protector).id.ToString();

                if (cachedPMat.TryGetValue(code, out var material))
                    if (material != null)
                        return material;

                var folder = Program.root + "MasterDuel/Protector/" + code;
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            folder = Path.Combine(Application.dataPath, folder);
#endif
                if (!Directory.Exists(folder))
                    return null;

                var files = Directory.GetFiles(folder)
                    .OrderBy(path => Path.GetFileName(path).Equals(code, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (files.Length == 0)
                    return null;

                var bundles = new List<AssetBundle>();
                AssetBundle matAB = null;
                foreach (var path in files)
                {
                    var bundle = await AssetBundle.LoadFromFileAsync(path).WithCancellation(token);
                    if (bundle == null)
                        continue;

                    bundles.Add(bundle);
                    if (Path.GetFileName(path).Equals(code, StringComparison.OrdinalIgnoreCase))
                        matAB = bundle;
                }
                if (matAB == null)
                    return null;

                material = matAB.LoadAsset<Material>("PMat");
                if (material == null)
                {
                    foreach (var bundle in bundles)
                        bundle.Unload(false);
                    return null;
                }

                if (defaultProtectorShader == null &&
                    code == defaultProtectorCode)
                    defaultProtectorShader = material.shader;
                if (material.shader.name.Contains("3D"))
                    material.shader = defaultProtectorShader;
                material.renderQueue = 3000;
                foreach (var bundle in bundles)
                    bundle.Unload(false);
                cachedPMat.TryAdd(code, material);

                return material;
            }
            finally
            {
                protectorSemaphoreSlim.Release();
            }
        }

        public static async UniTask<Material> LoadFrameMaterial(string code)
        {
            if (code == Items.CODE_RANDOM.ToString())
                code = Items.lastRandomFrameID;

            await frameSemaphoreSlim.WaitAsync();

            try
            {
                if (cachedFrameMat.TryGetValue(code, out var cachedMaterial))
                    if (cachedMaterial != null)
                        return new Material(cachedMaterial);

                var ab = await AssetBundle.LoadFromFileAsync(Program.root + "MasterDuel/Frame/ProfileFrameMat" + code);
                if (ab == null)
                    return null;

                try
                {
                    var material = ab.LoadAsset<Material>("ProfileFrameMat" + code);
                    if (material == null)
                        return null;

                    TextureManager.ChangeProfileFrameMaterialWrapMode(material);

                    if (cachedFrameMat.TryGetValue(code, out cachedMaterial) && cachedMaterial != null)
                        return new Material(cachedMaterial);

                    cachedFrameMat[code] = material;
                    return new Material(material);
                }
                finally
                {
                    ab.Unload(false);
                }
            }
            finally
            {
                frameSemaphoreSlim.Release();
            }
        }

        public static async UniTask<Material> LoadMaterialAsync(string path, CancellationToken token)
        {
            var ab = await AssetBundle.LoadFromFileAsync(Program.root + path).WithCancellation(token);
            var matetial = ab.LoadAsset<Material>(Path.GetFileName(path));
            ab.Unload(false);
            return matetial;
        }

        public static async UniTask<Shader> LoadShaderAsync(string path, CancellationToken token)
        {
            var ab = await AssetBundle.LoadFromFileAsync(Path.Combine(Program.root, path)).WithCancellation(token);
            var shader = ab.LoadAsset<Shader>(Path.GetFileNameWithoutExtension(path));
            ab.Unload(false);
            return shader;
        }

        public static async UniTask<Mate> LoadMateAsync(int code)
        {
            Items.Item item = new();
            foreach (var mate in Program.items.mates)
            {
                if (mate.id == code)
                {
                    item = mate;
                    break;
                }
            }
            Mate.MateType type = Mate.MateType.MasterDuel;
            if (item.id == 0 && File.Exists(Program.root + "CrossDuel/" + code + ".bundle"))
                type = Mate.MateType.CrossDuel;
            Mate returnValue = null;
            if (type == Mate.MateType.CrossDuel)
            {
                var ab = await AssetBundle.LoadFromFileAsync(Program.root + "CrossDuel/" + code + ".bundle");
                var all = ab.LoadAllAssets();
                ab.Unload(false);
                foreach (var asset in all)
                {
                    if (asset is NamedAssetContainer container)
                    {
                        container.TryGet<GameObject>("prefab", out var prefab);
                        container.TryGet<NamedAssetContainer>("Timelines", out var timelines);
                        container.TryGet<ParameterContainer>("Settings", out var settings);
                        var mateGo = UnityEngine.Object.Instantiate(prefab);
                        mateGo.AddComponent<FieldParamEventController_AnimationEventReceiver>();
                        foreach (var s in timelines.AllNamedAssetNames())
                        {
                            timelines.TryGet<GameObject>(s, out var timeline);
                            var newT = UnityEngine.Object.Instantiate(timeline);
                            newT.transform.SetParent(mateGo.transform, false);
                            newT.SetActive(true);
                            for (int i = 0; i < newT.transform.childCount; i++)
                            {
                                if (newT.transform.GetChild(i).GetComponent<Volume>() != null)
                                    UnityEngine.Object.Destroy(newT.transform.GetChild(i).gameObject);
                                if (newT.transform.GetChild(i).name == "UIBattleDownAni")
                                    UnityEngine.Object.Destroy(newT.transform.GetChild(i).gameObject);
                            }
                            var controller = newT.GetComponent<CustomTimelineController>();
                            var bindTrackInfo = controller.checkReplacer.m_bindTrackInfo;
                            var director = newT.transform.GetChild(0).GetComponent<PlayableDirector>();

                            if (director == null)
                                continue;
                            Dictionary<string, PlayableBinding> bindingDict = new Dictionary<string, PlayableBinding>();
                            foreach (PlayableBinding pb in director.playableAsset.outputs)
                                foreach (var bind in bindTrackInfo)
                                    if (pb.streamName == bind.m_name
                                        && director.GetGenericBinding(pb.sourceObject) == null)
                                        director.SetGenericBinding(pb.sourceObject, mateGo.GetComponent<Animator>());
                        }
                        returnValue = mateGo.AddComponent<Mate>();
                    }
                }
            }
            else
            {
                var matePath = Program.items.GetAssetPath(code.ToString(), Items.ItemType.Mate);

                GameObject mateGo;
                if (matePath.EndsWith("_Folder"))
                    mateGo = await LoadFromFolderAsync<CharacterCollision>("MasterDuel/" + matePath.Replace("_Folder", string.Empty), false, true);
                else
                    mateGo = await LoadFromFileAsync("MasterDuel/" + matePath, false, true);
                returnValue = mateGo.AddComponent<Mate>();
            }
            returnValue.type = type;
            returnValue.code = code;
            return returnValue;
        }

        #region Built-in MasterDuel Assets

        public static bool mdCached;
        public static float mdCachedProgress;
        private static bool mdDuelCached;

        private static AssetBundle mdBundleDuel;
        private static AssetBundle mdBundleOutDuel;
        private static AssetBundle mdBundleMaterials;
        private static AssetBundle mdBundleSprites;
        private static AssetBundle mdBundleTextures;

        public static async UniTask CacheMasterDuelOutDuelBundles()
        {
            mdCachedProgress = 0f;
            await CacheFromFileAsync(Program.root + "MasterDuel/Built-in/shaders");
            mdCachedProgress = 0.3f;
            mdBundleSprites = await CacheFromFileAsync(Program.root + "MasterDuel/Built-in/sprites");
            mdCachedProgress = 0.6f;
            mdBundleMaterials = await CacheFromFileAsync(Program.root + "MasterDuel/Built-in/materials");
            mdCachedProgress = 0.9f;
            mdBundleOutDuel = await CacheFromFileAsync(Program.root + "MasterDuel/Built-in/outduel");
            mdCachedProgress = 1f;
            mdCached = true;
        }

        public static async UniTask CacheMasterDuelBundles()
        {
            if (mdDuelCached)
                return;
            mdBundleTextures = await CacheFromFileAsync(Program.root + "MasterDuel/Built-in/textures");
            mdBundleDuel = await CacheFromFileAsync(Program.root + "MasterDuel/Built-in/duel");
            mdDuelCached = true;
        }

        public static GameObject LoadMasterDuelGameObject(string oName)
        {
            if(mdBundleDuel == null)
            {
                Debug.LogError("MasterDuel AssetBundle [Duel] is not cached!");
                return null;
            }

            var prefab = mdBundleDuel.LoadAsset<GameObject>(oName);
            if(prefab == null)
            {
                Debug.LogError($"MasterDuel AssetBundle [Duel] does not contain [{oName}]!");
                return null;
            }
            return UnityEngine.Object.Instantiate(prefab);
        }

        public static GameObject LoadMasterDuelOutDuelObject(string oName)
        {
            if (mdBundleOutDuel == null)
            {
                Debug.LogError("MasterDuel AssetBundle [OutDuel] is not cached!");
                return null;
            }

            var prefab = mdBundleOutDuel.LoadAsset<GameObject>(oName);
            if (prefab == null)
            {
                Debug.LogError($"MasterDuel AssetBundle [OutDuel] does not contain [{oName}]!");
                return null;
            }
            return UnityEngine.Object.Instantiate(prefab);
        }

        public static Material LoadMasterDuelMaterial(string mName)
        {
            if (mdBundleMaterials == null)
            {
                Debug.LogError("MasterDuel AssetBundle [Materials] is not cached!");
                return null;
            }
            var mat = mdBundleMaterials.LoadAsset<Material>(mName);
            if (mat == null)
            {
                Debug.LogError($"MasterDuel AssetBundle [Materials] does not contain material [{mName}]!");
                return null;
            }
            return UnityEngine.Object.Instantiate(mat);
        }

        public static Sprite LoadMasterDuelSprite(string sName)
        {
            if (mdBundleSprites == null)
            {
                Debug.LogError("MasterDuel AssetBundle [Sprites] is not cached!");
                return null;
            }
            var sprite = mdBundleSprites.LoadAsset<Sprite>(sName);
            if (sprite == null)
            {
                Debug.LogError($"MasterDuel AssetBundle [Sprites] does not contain sprite [{sName}]!");
                return null;
            }
            return sprite;
        }

        public static Texture2D LoadMasterDuelTexture(string tName)
        {
            if (mdBundleTextures == null)
            {
                Debug.LogError("MasterDuel AssetBundle [Textures] is not cached!");
                return null;
            }
            var tex = mdBundleTextures.LoadAsset<Texture2D>(tName);
            if (tex == null)
            {
                Debug.LogError($"MasterDuel AssetBundle [Textures] does not contain texture [{tName}]!");
                return null;
            }
            return tex;
        }

        #endregion
    }
}
