using System.Collections.Generic;
using UnityEngine;
using MDPro3.UI;
using System.Collections;
using System.IO;
using static UnityEngine.UI.Image;
using YgomSystem.Effect;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

namespace MDPro3
{
    public class BackgroundManager : Manager
    {
        GameObject back;
        int cid;
        public static readonly Dictionary<int, string> backgrounds = new Dictionary<int, string>()
        {
            {1, "Classic" },
            {2, "Classic2" },
            {11, "ClassicRed" },
            {5, "ClassicPink" },
            {4, "ClassicPinkShine" },
            //{6, "ClassicWhite" },
            {7, "WCS" },
            {8, "Shop" },
            {9, "Knowledge" },
            {3, "PurpleDarkFantasy" },
            {10, "DeepDarkFantasy" },
            //{13, "New" },
            {12, "DIY Green" },
            {14, "Neos" },
            {16, "Classic Grid" },
            {17, "4th Anniversary" },

            {50, "DIY Classic" },
            {51, "DIY Classic2" },
            {52, "DIY Red" },
            {53, "DIY Pink" },
            {54, "DIY PinkShine" },
            //{55, "DIY Purple" },
            {56, "DIY WCS" },
            {57, "DIY SHOP" },
            {58, "DIY Knowledge" },
            {59, "DIY DeepDarkFantasy" },
        };

        public void Refresh()
        {
            Change(cid);
        }

        public void Change(int id)
        {
            if(GetComponent<UltraWallpaperTheme>()==null)gameObject.AddComponent<UltraWallpaperTheme>();
            if(id==0)
            {
                var available=new List<int>();
                foreach(int candidate in backgrounds.Keys)
                    if(BackgroundInstalled(BackgroundAssetID(candidate)))available.Add(candidate);
                id=available.Count>0?available[Random.Range(0,available.Count)]:2;
            }
            cid=id;
            id=BackgroundAssetID(id);
            if(!BackgroundInstalled(id))
            {
                id=2;cid=2;
                if(!BackgroundInstalled(id))
                {
                    Debug.LogWarning("No installed menu background is available.");
                    return;
                }
            }
            var loaded=ABLoader.LoadFromFolder<SpriteRenderer>("MasterDuel/Background/Back"+id.ToString("D4"),true,true);
            if(loaded==null){Debug.LogWarning("The selected background has no sprite prefab.");return;}
            Destroy(back);back=loaded;
            if (back.TryGetComponent<SpriteScaler>(out var spriteScaler))
            {
                spriteScaler.isApplyOnUpdate = true;
                spriteScaler.SetFitMode(SpriteScaler.FitMode.FitHighestResolutionMaintainAspectRatio);
            }
            Tools.ChangeLayer(back, "2D");
            back.transform.SetParent(transform, false);

            if(id == 12 || cid >= 50)
            {
                _ = SetDIYBGAsync(back.GetComponent<SpriteRenderer>(), id);
            }
        }

        static int BackgroundAssetID(int id)=>id switch
        {
            50=>1,51=>2,52=>11,53=>5,54=>4,55=>6,56=>7,57=>8,58=>9,59=>10,_=>id
        };
        static bool BackgroundInstalled(int id)
        {
            string folder=Path.Combine(Program.root,"MasterDuel","Background","Back"+id.ToString("D4"));
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
            folder=Path.Combine(Application.dataPath,folder);
#endif
            return Directory.Exists(folder);
        }

        private async UniTask SetDIYBGAsync(SpriteRenderer renderer, int id)
        {
            var bg = Program.PATH_DIY + "Background";
            if (File.Exists(bg + Program.EXPANSION_PNG))
                bg += Program.EXPANSION_PNG;
            else if (File.Exists(bg + Program.EXPANSION_JPG))
                bg += Program.EXPANSION_JPG;
            else
                return;

            var pic = await TextureManager.LoadPicFromFileAsync(bg);
            int targetHeight = 1080;
            Texture2D scaledTexture;
            if(pic.height != targetHeight)
            {
                int newWidth = Mathf.RoundToInt(pic.width * (targetHeight / (float)pic.height));
                scaledTexture = TextureProcessor.ResizeTexture2D(pic, newWidth, targetHeight);
            }
            else
                scaledTexture = pic;

            if (renderer != null)
                renderer.sprite = TextureManager.Texture2Sprite(scaledTexture);
            if(id == 9)
                renderer.material.SetTexture("_MainTex01", scaledTexture);
        }

        public int GetIDByName(string bgName)
        {
            var id = 0;
            if(bgName == InterString.Get("随机"))
                return 0;
            foreach (var background in backgrounds)
            {
                if(bgName == background.Value)
                {
                    id = background.Key;
                    break;
                }
            }
            return id;
        }
    }
}
