using MDPro3.Utility;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Playables;
using YgomSystem.ElementSystem;

namespace MDPro3
{
    public static class Tools
    {
        public static void ChangeLayer(GameObject go, string layer, bool setAllChildrenActivate = false)
        {
            //Debug.Log("Change Layer: " + go.name + "-> " + layer);
            foreach (Transform t in go.transform.GetComponentsInChildren<Transform>(true))
            {
                if (setAllChildrenActivate) t.gameObject.SetActive(true);
                t.gameObject.layer = LayerMask.NameToLayer(layer);
            }
        }

        public static void ChangeLayer(GameObject go, int layerMask, bool setAllChildrenActivate = false)
        {
            //Debug.Log("Change Layer: " + go.name + "-> " + layer);
            foreach (Transform t in go.transform.GetComponentsInChildren<Transform>(true))
            {
                if (setAllChildrenActivate) t.gameObject.SetActive(true);
                t.gameObject.layer = layerMask;
            }
        }

        public static void ChangeSortingLayer(GameObject go, string sortingLayer)
        {
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                renderer.sortingLayerName = sortingLayer;
        }

        public static void ChangeMaterialRenderQueue(GameObject root, int queue)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.material.renderQueue = queue;
        }

        public static void PlayAnimation(Transform animationContainer, string animationName)
        {
            if (animationContainer == null) return;
            Animator[] animators = animationContainer.GetComponentsInChildren<Animator>();
            foreach (Animator animator in animators)
            {
                if (animator == null || string.IsNullOrEmpty(animationName))
                    continue;

                var hasTrigger = false;
                var parameters = animator.parameters;
                for (var i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    if (param.type == AnimatorControllerParameterType.Trigger && param.name == animationName)
                    {
                        hasTrigger = true;
                        break;
                    }
                }

                if (hasTrigger)
                    animator.SetTrigger(animationName);
            }
        }

        public static void PlayParticle(Transform particleContainer, string particleName)
        {
            if (particleContainer == null) return;
            foreach (var child in particleContainer.GetComponentsInChildren<Transform>(true))
                if (child.name.ToLower().Contains(particleName.ToLower()))
                    foreach (var p in child.GetComponentsInChildren<ParticleSystem>(true))
                        p.Play();
        }

        public static void SetAnimatorTimescale(Transform container, float timeScale)
        {
            foreach (var animator in container.GetComponentsInChildren<Animator>(true))
                animator.speed = timeScale;
        }

        public static void SetParticleSystemSimulationSpeed(Transform container, float timeScale)
        {
            foreach(var particle in container.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = particle.main;
                main.simulationSpeed = timeScale;
            }
        }

        public static void SetPlayableDirectorUnscaledGameTime(Transform container)
        {
            foreach (var director in container.GetComponentsInChildren<PlayableDirector>(true))
                director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        }

        public static PlayableDirector GetPlayableDirectorInChildren(Transform container)
        {
            PlayableDirector returnValue = null;
            for (int i = 0 ; i < container.childCount; i++)
            {
                if (container.GetChild(i).GetComponent<PlayableDirector>() != null)
                    returnValue = container.GetChild(i).GetComponent<PlayableDirector>();
                else
                    UnityEngine.Object.Destroy(container.GetChild(i).gameObject);
            }
            return returnValue;
        }

        public static int CompareTime(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var xInfo = (FileInfo)x;
            var yInfo = (FileInfo)y;
            return yInfo.LastWriteTime.CompareTo(xInfo.LastWriteTime);
        }

        public static int CompareName(object x, object y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var xInfo = (FileInfo)x;
            var yInfo = (FileInfo)y;
            return xInfo.FullName.CompareTo(yInfo.FullName);
        }

        public static string GetTimeString()
        {
            return DateTime.Now.ToString("MM-dd「HH：mm：ss」");
        }

        public static List<string> GetLocalIPv4()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var returnValue = new List<string >();
            foreach(var address in  host.AddressList)
                if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    returnValue.Add(address.ToString() ?? "127.0.0.1");
            return returnValue;
        }

        public static string[] SplitWithPreservedQuotes(string input)
        {
            List<string> result = new List<string>();
            int start = 0;
            bool inQuotes = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (input[i] == ' ' && !inQuotes)
                {
                    result.Add(input.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(input.Substring(start));
            for (int i = 0; i < result.Count; i++)
                result[i] = result[i].Replace("\"", "");
            return result.ToArray();
        }

        public static KeyValuePair<TKey, TValue> GetNthDictionaryElement<TKey, TValue>(Dictionary<TKey, TValue> dic, int n)
        {
            if (n < 0)
                n = 0;
            if (n >= dic.Count)
                n = dic.Count - 1;
            var enumerator = dic.GetEnumerator();
            for (int i = 0; i < n + 1; i++)
                enumerator.MoveNext();
            return enumerator.Current;
        }

        public static KeyValuePair<TKey, TValue> GetRandomDictionaryElement<TKey, TValue>(Dictionary<TKey, TValue> dic)
        {
            return GetNthDictionaryElement(dic, UnityEngine.Random.Range(0, dic.Count));
        }

        public static bool StringIsAlphaNumeric(string input)
        {
            Regex regex = new Regex("^[A-Za-z0-9]+$");
            return regex.IsMatch(input);
        }
        public static bool StringIsLowerAlphaNumeric(string input)
        {
            Regex regex = new Regex("^[a-z0-9]+$");
            return regex.IsMatch(input);
        }

        public static int GetLocalDeckCount()
        {
            return Directory.GetFiles(Program.PATH_DECK, "*.ydk").Length;
        }

        public static DateTime GetLocalDeckLastEditTime()
        {
            DateTime dateTime = DateTime.MinValue;
            foreach(var file in Directory.GetFiles(Program.PATH_DECK, "*.ydk"))
            {
                var fileInfo = new FileInfo(file);
                if(fileInfo.LastWriteTime > dateTime)
                    dateTime = fileInfo.LastWriteTime;
            }
            return dateTime;
        }

        #region Online

        public static async Task<Texture2D> DownloadImageAsync(string url, CancellationToken cancellationToken = default)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            request.SetRequestHeader("User-Agent", "MDPro3/" + Application.version + " (" + System.Environment.OSVersion.ToString() + "); Unity/" + Application.unityVersion);

            var send = request.SendWebRequest();
            while (!send.isDone)
                await TaskUtility.WaitOneFrame(cancellationToken);
            if (!Application.isPlaying)
                return null;

            cancellationToken.ThrowIfCancellationRequested();

            if (request.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerTexture.GetContent(request);
            }
            else
            {
                UnityEngine.Debug.LogErrorFormat("Image [{0}]: {1}", url, request.error);
                return null;
            }
        }

        #endregion

        public static Vector3 GetDeckModelTopPosition(ElementObjectManager manager)
        {
            var subManager = manager.GetElement<ElementObjectManager>("CardShuffleTop");
            var returnValue = subManager.GetElement<Transform>("CardModel01_back").position;
            var position = subManager.GetElement<Transform>("CardModel02_back").position;
            if (position.y > returnValue.y)
                returnValue = position;
            position = subManager.GetElement<Transform>("CardModel03_back").position;
            if (position.y > returnValue.y)
                returnValue = position;
            position = subManager.GetElement<Transform>("CardModel04_back").position;
            if (position.y > returnValue.y)
                returnValue = position;
            return returnValue;
        }

        public static void ClearDirectoryRecursively(DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
                file.Delete();
            foreach (var subDir in directory.GetDirectories())
            {
                ClearDirectoryRecursively(subDir);
                subDir.Delete();
            }
        }

        public static float CalculateWeightedDistance(Vector3 a, Vector3 b, char priorityAxis)
        {
            bool isPriorityX = Char.ToLower(priorityAxis) == 'x';
            bool isPriorityY = Char.ToLower(priorityAxis) == 'y';
            bool isPriorityZ = Char.ToLower(priorityAxis) == 'z';

            float deltaX = Mathf.Abs(a.x - b.x);
            float deltaY = Mathf.Abs(a.y - b.y);
            float deltaZ = Mathf.Abs(a.z - b.z);

            if (isPriorityX)
            {
                deltaY *= 10;
                deltaZ *= 10;
            }
            else if (isPriorityY)
            {
                deltaX *= 10;
                deltaZ *= 10;
            }
            else if (isPriorityZ)
            {
                deltaX *= 10;
                deltaY *= 10;
            }

            return Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        public static bool InLastRow(int index, int  counts, int columes)
        {
            if (columes <= 0)
            {
                throw new ArgumentException("columes must be greater than 0");
            }

            int lastRowStartIndex = counts - (counts % columes);
            if (counts % columes == 0)
            {
                lastRowStartIndex = counts - columes;
            }

            return index >= lastRowStartIndex;
        }

        #region Pools

        private static readonly ConcurrentBag<MemoryStream> streamPool = new();

        public static MemoryStream GetStream()
        {
            if (streamPool.TryTake(out var stream))
            {
                stream.SetLength(0);
                return stream;
            }
            return new MemoryStream();
        }

        public static void ReturnStream(MemoryStream stream)
        {
            streamPool.Add(stream);
        }

        #endregion

        #region Path

        public static string FormatPlatformUrl(string filePath)
        {
            filePath = GetPlatformFilePath(filePath);
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            return "file://" + filePath.Replace("\\", "/");
#elif UNITY_ANDROID || UNITY_IOS
            return filePath.Replace("\\", "/");
#else
            return filePath;
#endif
        }

        public static string GetPlatformFilePath(string path)
        {
#if !UNITY_EDITOR && (UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX)
            // A bare Unix path is treated as an HTTPS localhost URL by UnityWebRequest.
            return new Uri(ResolvePlatformPath(path, false)).AbsoluteUri;
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN
            return ResolvePlatformPath(path, false);
#elif UNITY_ANDROID || UNITY_IOS
            return Path.Combine("file://" + Application.persistentDataPath, path);
#else
            return path;
#endif
        }

        public static string GetPlatformPath(string path)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            return ResolvePlatformPath(path, true);
#elif UNITY_ANDROID || UNITY_IOS
            return Path.Combine(Application.persistentDataPath, path);
#else
            return path;
#endif
        }

        // Picture lives beside the player (parent of Ultra-Duel_Data), even when the
        // process current directory is not the build folder.
        static string ResolvePlatformPath(string path, bool directory)
        {
            string relative = path ?? "";
            string cwd = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, relative));
            if (directory ? Directory.Exists(cwd) : File.Exists(cwd)) return cwd;
            if (!string.IsNullOrEmpty(Application.dataPath))
            {
                string beside = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
                if (directory ? Directory.Exists(beside) : File.Exists(beside)) return beside;
            }
            return cwd;
        }

        #endregion

        #region Json

        public static string FormatJsonString(string jsonString)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(jsonString);
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch
            {
                return jsonString;
            }
        }

        #endregion

        #region Screen

        public static float GetScreenAspectRatio()
        {
            return (float)Screen.width / Screen.height;
        }

        public static bool IsAspectRatioWidescreen()
        {
            return GetScreenAspectRatio() >= 1.77f; // 16:9
        }

        #endregion

        #region Text

        public static string RemoveFirstLines(string text, int linesToRemove)
        {
            if (string.IsNullOrEmpty(text) || linesToRemove <= 0)
                return text;

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            if (lines.Length <= linesToRemove)
                return text;

            var remainingLines = lines.Skip(linesToRemove).ToArray();
            string newLine = text.Contains("\r\n") ? "\r\n" : "\n";
            return string.Join(newLine, remainingLines);
        }

        public static string MergeWithNewLine(string str1, string str2)
        {
            var nonEmpty = new[] { str1, str2 }
                .Where(s => !string.IsNullOrWhiteSpace(s));
            return string.Join(Environment.NewLine, nonEmpty);
        }

        #endregion
    }
}
