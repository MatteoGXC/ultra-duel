using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.Utility;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UI;
using YgomSystem.ElementSystem;

namespace MDPro3
{
    public class TextureManager : Manager
    {
        public static TextureManager instance;
        public static TextureContainer container;

        private Material commonShopButtonMat;
        private Material commonShopButtonOverMat;

        public override void Initialize()
        {
            instance = this;
            base.Initialize();
            var handle = Addressables.LoadAssetAsync<TextureContainer>("ScriptableObjects/TextureContainer.asset");
            handle.Completed += (result) =>
            {
                container = result.Result;
            };
            _ = LoadMaterials();
        }

        public static bool loaded;
        private async UniTask LoadMaterials()
        {
            await UniTask.WaitUntil(() => container != null);

            commonShopButtonMat = await ABLoader.LoadMaterialAsync("MasterDuel/Material/GUI_CommonShopButton_N", default);
            SetCommonShopButtonMaterial(commonShopButtonMat);

            commonShopButtonOverMat = await ABLoader.LoadMaterialAsync("MasterDuel/Material/GUI_CommonShopButton_N_Over", default);
            SetCommonShopButtonMaterial(commonShopButtonOverMat);

#if UNITY_ANDROID || UNITY_STANDALONE_LINUX
            var dependencyDirectory = Program.root + "CrossDuel/Dependency";
            var depens = Directory.Exists(dependencyDirectory)
                ? Directory.GetFiles(dependencyDirectory, "*.bundle") : Array.Empty<string>();
            foreach (var depen in depens)
                await ABLoader.CacheFromFileAsync(Program.root + "CrossDuel/Dependency/" + Path.GetFileName(depen));
#endif
            loaded = true;
        }

        public static async UniTask<Texture2D> LoadPicFromFileAsync(string path)
        {
            if (!File.Exists(path))
                return null;
            string fullPath;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
            fullPath = Path.Combine("file://" + Application.persistentDataPath, path);
#elif UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX
            fullPath = Path.Combine("file://" + Environment.CurrentDirectory, path);
#else
            fullPath = Environment.CurrentDirectory + Program.STRING_SLASH + path;
#endif
            using var request = UnityWebRequestTexture.GetTexture(fullPath);
            await request.SendWebRequest().WithCancellation(Application.exitCancellationToken);

            if (request.result == UnityWebRequest.Result.Success)
                return DownloadHandlerTexture.GetContent(request);
            else
            {
                Debug.LogWarningFormat("Pic File [{0}] not fount.", path);
                return null;
            }
        }

        public async UniTask LoadCardToRawImageWithoutMaterialAsync(RawImage rawImage, int code, bool cache = true)
        {
            rawImage.texture =await CardImageLoader.LoadCardAsync(code, cache, rawImage.destroyCancellationToken);
        }

        public async UniTask LoadCardToRendererWithMaterialAsync(Renderer renderer, int code, bool cache = true)
        {
            var mat = MaterialLoader.GetCardMaterial(code, true);
            mat.mainTexture = await CardImageLoader.LoadCardAsync(code, cache, renderer.GetCancellationTokenOnDestroy());
            if(renderer != null)
                renderer.material = mat;
        }

        public async UniTask LoadDummyCard(ElementObjectManager manager, int code, uint player, bool active = false, 
            Renderer attachRenderer = null, Renderer attachRenderer2 = null)
        {
            if (active)
                manager.gameObject.SetActive(false);
            manager.GetElement<Renderer>("DummyCardModel_back").material = player == 0 ? OcgCore.myProtector : OcgCore.opProtector;

            var renderer = manager.GetElement<Renderer>("DummyCardModel_front");
            renderer.material = MaterialLoader.GetCardMaterial(code, true);
            renderer.material.mainTexture = await CardImageLoader.LoadCardAsync(code, false, manager.destroyCancellationToken);
            if(attachRenderer != null)
                attachRenderer.material.mainTexture = renderer.material.mainTexture;
            if (attachRenderer2 != null)
                attachRenderer2.material.mainTexture = renderer.material.mainTexture;
            if (active)
                manager.gameObject.SetActive(true);
        }

        private void SetCommonShopButtonMaterial(Material mat)
        {
            mat.SetFloat("_NoiseSize", 500f);
            mat.SetFloat("_NoiseSpeed", 0.5f);
            mat.SetVector("_TilingOffset", new Vector4(1f, 1f, 0f, 0f));
            mat.SetVector("_MainTexMinMax", new Vector4(-0.5f, 1f, -0.5f, 1f));
        }

        public async UniTask SetCommonShopButtonMaterial(Image image, bool hover)
        {
            if (hover)
            {
                await UniTask.WaitUntil(() => commonShopButtonOverMat != null, cancellationToken : image.destroyCancellationToken);
                image.material = commonShopButtonOverMat;
            }
            else
            {
                await UniTask.WaitUntil(() => commonShopButtonMat != null, cancellationToken: image.destroyCancellationToken);
                image.material = commonShopButtonMat;
            }
        }

        #region Closeup

        static Dictionary<int, Texture2D> cachedCloseups = new Dictionary<int, Texture2D>();

        public async UniTask<Texture2D> LoadCloseupAsync(int code, MeshRenderer renderer = null)
        {
            if(renderer != null)
                renderer.gameObject.SetActive(false);
            if (cachedCloseups.TryGetValue(code, out var returenValue))
            {
                if (renderer != null)
                    ResizeCloseup(renderer, returenValue);
                return returenValue;
            }
            if (!Directory.Exists(Program.PATH_CLOSEUP))
                Directory.CreateDirectory(Program.PATH_CLOSEUP);
            var path = Program.PATH_CLOSEUP + code + Program.EXPANSION_PNG;
            if (!File.Exists(path))
                return null;

            returenValue = await LoadPicFromFileAsync(path);

            returenValue.name = "Closeup_" + code;
            if (cachedCloseups.ContainsKey(code))
            {
                Destroy(returenValue);
                returenValue = cachedCloseups[code];
            }
            else
                cachedCloseups.Add(code, returenValue);
            if (renderer != null)
                ResizeCloseup(renderer, returenValue);
            return returenValue;
        }

        void ResizeCloseup(MeshRenderer renderer, Texture2D tex)
        {
            renderer.material.mainTexture = tex;
            var aspect = (float)tex.width / tex.height;
            renderer.transform.localScale = new Vector3 (8f * aspect, 8f, 1f);
            renderer.gameObject.SetActive(true);
            DOTween.To(() => 0f, x =>
            {
                renderer.transform.localScale = new Vector3(x * aspect, x, 1f);
            }, 8f, 0.3f);
        }

        #endregion

        #region Card UI

        public static Sprite GetCardLocationIcon(GPS p)
        {
            if ((p.location & (uint)CardLocation.Hand) > 0)
                return container.locationHand;
            else if ((p.location & (uint)CardLocation.Deck) > 0)
                return container.locationDeck;
            else if ((p.location & (uint)CardLocation.Extra) > 0)
                return container.locationExtra;
            else if ((p.location & (uint)CardLocation.Grave) > 0)
                return container.locationGrave;
            else if ((p.location & (uint)CardLocation.Removed) > 0)
                return container.locationRemoved;
            else if ((p.location & (uint)CardLocation.Overlay) > 0)
                return container.locationOverlay;
            else if ((p.location & (uint)CardLocation.Onfield) > 0)
            {
                if(p.controller == 0)
                    return container.locationMyField;
                else
                    return container.locationOpField;
            }
            else if ((p.location & (uint)CardLocation.Search) > 0)
                return container.locationSearch;
            else
                return container.typeNone;
        }

        public static Sprite GetCardRaceIcon(int race)
        {
            if ((race & (uint)CardRace.Warrior) > 0)
                return container.raceWarrior;
            else if ((race & (uint)CardRace.SpellCaster) > 0)
                return container.raceSpellCaster;
            else if ((race & (uint)CardRace.Fairy) > 0)
                return container.raceFairy;
            else if ((race & (uint)CardRace.Fiend) > 0)
                return container.raceFiend;
            else if ((race & (uint)CardRace.Zombie) > 0)
                return container.raceZombie;
            else if ((race & (uint)CardRace.Machine) > 0)
                return container.raceMachine;
            else if ((race & (uint)CardRace.Aqua) > 0)
                return container.raceAqua;
            else if ((race & (uint)CardRace.Pyro) > 0)
                return container.racePyro;
            else if ((race & (uint)CardRace.Rock) > 0)
                return container.raceRock;
            else if ((race & (uint)CardRace.WindBeast) > 0)
                return container.raceWindBeast;
            else if ((race & (uint)CardRace.Plant) > 0)
                return container.racePlant;
            else if ((race & (uint)CardRace.Insect) > 0)
                return container.raceInsect;
            else if ((race & (uint)CardRace.Thunder) > 0)
                return container.raceThunder;
            else if ((race & (uint)CardRace.Dragon) > 0)
                return container.raceDragon;
            else if ((race & (uint)CardRace.Beast) > 0)
                return container.raceBeast;
            else if ((race & (uint)CardRace.BeastWarrior) > 0)
                return container.raceBeastWarrior;
            else if ((race & (uint)CardRace.Dinosaur) > 0)
                return container.raceDinosaur;
            else if ((race & (uint)CardRace.Fish) > 0)
                return container.raceFish;
            else if ((race & (uint)CardRace.SeaSerpent) > 0)
                return container.raceSeaSerpent;
            else if ((race & (uint)CardRace.Reptile) > 0)
                return container.raceReptile;
            else if ((race & (uint)CardRace.Psycho) > 0)
                return container.racePsycho;
            else if ((race & (uint)CardRace.DivineBeast) > 0)
                return container.raceDivineBeast;
            else if ((race & (uint)CardRace.CreatorGod) > 0)
                return container.raceCreatorGod;
            else if ((race & (uint)CardRace.Wyrm) > 0)
                return container.raceWyrm;
            else if ((race & (uint)CardRace.Cyberse) > 0)
                return container.raceCyberse;
            else if ((race & (uint)CardRace.Illustion) > 0)
                return container.raceIllustion;
            else
                return container.typeNone;
        }

        public static Sprite GetSpellTrapTypeIcon(Card data)
        {
            if (data.HasType(CardType.Counter))
                return container.typeCounter;
            else if (data.HasType(CardType.Field))
                return container.typeField;
            else if (data.HasType(CardType.Equip))
                return container.typeEquip;
            else if (data.HasType(CardType.Continuous))
                return container.typeContinuous;
            else if (data.HasType(CardType.QuickPlay))
                return container.typeQuickPlay;
            else if (data.HasType(CardType.Ritual))
                return container.typeRitual;
            else
                return container.typeNone;
        }

        public static Sprite GetCardLevelIcon(Card data)
        {
            if (data.HasType(CardType.Link))
                return container.typeLink;
            else if (data.HasType(CardType.Xyz))
                return container.typeRank;
            else
                return container.typeLevel;
        }

        public static Sprite GetCardCounterIcon(int counter)
        {
            return counter switch
            {
                0x1 => container.counterMagic,
                0x1002 => container.counterWedge,
                0x3 => container.counterBushido,
                0x4 => container.counterPsycho,
                0x5 => container.counterShine,
                0x6 => container.counterGem,
                0x8 => container.counterDeformer,
                0x1009 => container.counterVenom,
                0xA => container.counterGenex,
                //0xB => , //指示物（古代的机械城）
                0xC => container.counterThunder,
                0xD => container.counterGreed,
                0x100E => container.counterAlien,
                0xF => container.counterWorm,
                0x10 => container.counterBF,
                0x11 => container.counterHyper,
                0x12 => container.counterKarakuri,
                0x13 => container.counterChaos,
                //0x14 => , //指示物（奇迹之侏罗纪蛋）
                0x1015 => container.counterIce,
                0x16 => container.counterStone,
                0x17 => container.counterDonguri,
                0x18 => container.counterFlower,
                0x1019 => container.counterFog,
                0x1A => container.counterDouble,
                0x1B => container.counterClock,
                0x1C => container.counterD,
                0x1D => container.counterJunk,
                0x1E => container.counterGate,
                //0x1F => , //指示物（巨大战舰）
                0x20 => container.counterPlant,
                0x1021 => container.counterGuard2,
                0x22 => container.counterDragonic,
                0x23 => container.counterOcean,
                0x1024 => container.counterString,
                0x25 => container.counterChronicle,
                //0x26 =>, //指示物（金属射手）
                //0x27 =>, //指示物（死亡蚊）
                //0x28 =>, //指示物（暗黑弹射手）
                //0x29 =>, //指示物（气球蜥蜴）
                //0x102A =>, //指示物（魔法防护器）
                0x2B => container.counterDestiny,
                0x2C => container.counterOrbital,
                //0x2D =>, //指示物（踢火）
                0x2E => container.counterShark,
                0x2F => container.counterPumpkin,
                0x30 => container.counterKattobing,
                0x31 => container.counterHopeSlash,
                0x32 => container.counterBalloon,
                0x33 => container.counterYosen,
                //0x34 => , //指示物（纸箱拳击手）
                0x35 => container.counterSound,
                0x36 => container.counterEM,
                0x37 => container.counterKaiju,
                0x1038 => container.counterHoukai,
                0x1039 => container.counterZushin,
                //0x40 => , //指示物（No.51 怪腕之必杀摔角手）
                0x1041 => container.counterPredator,
                //0x42 => , //指示物（爆竹鬼）
                0x43 => container.counterDefect,
                //0x44 => , //指示物（弹带城壁龙）
                0x1045 => container.counterScales,
                //0x46 => , //指示物（刚鬼死斗）
                //0x47 => , //指示物（限制代码）
                //0x48 => , //指示物（连接死亡炮塔）
                0x1049 => container.counterPolice,
                0x4A => container.counterAthlete,
                0x4B => container.counterBarrel,
                0x4C => container.counterSummon,
                0x104D => container.counterSignal,
                //0x4E => , //指示物（魂之灵摆）
                0x104F => container.counterVenemy,
                //0x50 => , //指示物（娱乐伙伴 掉头跑骑兵）
                //0x51 => , //指示物（蜂军巢）
                //0x52 => , //指示物（防火龙·暗流体）
                //0x53 => , //指示物（炽天蝶）
                //0x54 => , //指示物（星遗物引导的前路）
                //0x55 => , //指示物（隐居者的大釜）
                0x56 => container.counterFireStar,
                0x57 => container.counterPhantasm,
                //0x58 => , //指示物（祢须三破鸣比）
                0x59 => container.counterOtoshidama,
                //0x5A => , //指示物（战吼试炼）
                //0x5B => , //指示物（北极天熊北斗星）
                0x105C => container.counterBurn,
                //0x5D => , //指示物（机巧传-神使记纪图）
                0x5E => container.counterOunokagi,
                0x5F => container.counterPiece,
                //0x60 => , //指示物（北极天熊辐射）
                //0x61 => , //指示物（命运的囚人）
                //0x62 => , //指示物（逐渐削减的生命）
                0x1063 => container.counterIllusion,
                0x64 => container.counterGG,
                0x1065 => container.counterRabbit,
                //0x66 => , //指示物（推荐捏军贯）
                //0x67 => , //指示物（战斗车轮）
                //0x68 => , //指示物（图腾柱）
                //0x69 => , //指示物（吠陀-优婆尼沙昙）
                0x6A => container.counterKyoumei,
                0x106B => container.counterKyouai,
                0x6C => container.counterAccess,
                0x6D => container.counterShukudai,
                0x6E => container.counterShiki,
                0x6F => container.counterC,
                0x70 => container.counterDish,
                0x71 => container.counterKyuzai,
                0x1072 => container.counterGirl,
                0x73 => container.counterT,

                0x102A => container.counterGardna,
                _ => container.counterNormal,
            };

            /*
            Currently not used:
            counterDeath
            counterEarthBind
            counterGardna
            counterGuard

            */
        }

        #endregion

        #region Public Static Functions

        public static Sprite Texture2Sprite(Texture2D texture)
        {
            if (texture == null)
                return null;
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            return sprite;
        }

        public static void ReplaceTransparentPixelsWithColor(Texture2D texture, Color replacementColor)
        {
            var pixels = texture.GetPixels32();

            for(int i = 0; i < pixels.Length; i++)
                if (pixels[i].a == 0)
                    pixels[i] = replacementColor;

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        /// <summary>
        /// Creates a new Texture2D with the original texture centered and allows for vertical and horizontal offsets.
        /// </summary>
        /// <param name="originalTexture">The original Texture2D.</param>
        /// <param name="newSize">The size of the new Texture2D.</param>
        /// <param name="offsetX">Horizontal offset in pixels.</param>
        /// <param name="offsetY">Vertical offset in pixels.</param>
        /// <returns>A new Texture2D with the original texture centered and offset.</returns>
        public static Texture2D CreateCenteredTexture(Texture2D originalTexture, int newSize, int offsetX, int offsetY)
        {
            if (originalTexture == null)
                throw new System.ArgumentNullException("originalTexture", "Original texture cannot be null.");

            Texture2D newTexture = new(newSize, newSize, originalTexture.format, false);

            for (int y = 0; y < newSize; y++)
                for (int x = 0; x < newSize; x++)
                    newTexture.SetPixel(x, y, Color.clear);
            newTexture.Apply();

            int centerX = newSize / 2;
            int centerY = newSize / 2;

            int startX = centerX - originalTexture.width / 2 + offsetX;
            int startY = centerY - originalTexture.height / 2 + offsetY;

            for (int y = 0; y < originalTexture.height; y++)
            {
                for (int x = 0; x < originalTexture.width; x++)
                {
                    Color pixelColor = originalTexture.GetPixel(x, y);
                    int newX = startX + x;
                    int newY = startY + y;

                    if (newX >= 0 && newX < newSize && newY >= 0 && newY < newSize)
                        newTexture.SetPixel(newX, newY, pixelColor);
                }
            }

            newTexture.Apply();

            return newTexture;
        }

        public static void ChangeProfileFrameMaterialWrapMode(Material mat)
        {
#if !UNITY_ANDROID
            return;
#endif

            if (mat == null)
                return;

            for(int i = 0; i < mat.shader.GetPropertyCount(); i++)
            {
                if(mat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    var propName = mat.shader.GetPropertyName(i);
                    if (propName != "_ProfileFrameTex" && propName != "_MainTex")
                    {
                        var tex = mat.GetTexture(propName);
                        if (tex != null)
                            tex.wrapMode = TextureWrapMode.Repeat;
                    }
                }
            }
        }

        public static Texture2D GetCroppingTex(Texture2D texture, int startX, int startY, int width, int height)
        {
            var returnValue = new Texture2D(width - startX, height - startY);
            var pix = new Color[returnValue.width * returnValue.height];
            var index = 0;
            for (var y = startY; y < height; y++)
                for (var x = startX; x < width; x++)
                    pix[index++] = texture.GetPixel(x, y);
            returnValue.SetPixels(pix);
            returnValue.Apply();
            return returnValue;
        }

        #endregion
    }
}
