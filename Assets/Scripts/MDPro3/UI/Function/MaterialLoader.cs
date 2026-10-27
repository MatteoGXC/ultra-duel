using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Utility;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static MDPro3.CardRenderer;

namespace MDPro3
{
    public static class MaterialLoader
    {

        #region Card Materials

        private static Material cardMatNormalUI;
        private static Material cardMatShineUI;
        private static Material cardMatShineRDUI;
        private static Material cardMatRoyalUI;
        private static Material cardMatRoyalRDUI;
        private static Material cardMatGoldUI;
        private static Material cardMatGoldRDUI;
        private static Material cardMatMillenniumUI;
        private static Material cardMatMillenniumRDUI;

        private static Material cardMatNormal3D;
        private static Material cardMatShine3D;
        private static Material cardMatShineRD3D;
        private static Material cardMatRoyal3D;
        private static Material cardMatRoyalRD3D;
        private static Material cardMatGold3D;
        private static Material cardMatGoldRD3D;
        private static Material cardMatMillennium3D;
        private static Material cardMatMillenniumRD3D;

        private static Material cardMatShineOFUI;
        private static Material cardMatShineOFRDUI;
        private static Material cardMatRoyalOFUI;
        private static Material cardMatRoyalOFRDUI;
        private static Material cardMatGoldOFUI;
        private static Material cardMatGoldOFRDUI;
        private static Material cardMatMillenniumOFUI;
        private static Material cardMatMillenniumOFRDUI;

        private static Material cardMatShineOF3D;
        private static Material cardMatShineOFRD3D;
        private static Material cardMatRoyalOF3D;
        private static Material cardMatRoyalOFRD3D;
        private static Material cardMatGoldOF3D;
        private static Material cardMatGoldOFRD3D;
        private static Material cardMatMillenniumOF3D;
        private static Material cardMatMillenniumOFRD3D;

        private static Dictionary<int, Texture2D> cachedOverFrameMasks = new();

        [RuntimeInitializeOnLoadMethod]
        public static async UniTask LoadCardMaterials()
        {
            await UniTask.WaitUntil(() => TextureManager.loaded && TextureManager.container != null);

            cardMatNormalUI = ABLoader.LoadMasterDuelMaterial("NormalStyleUI");
            cardMatNormal3D = ABLoader.LoadMasterDuelMaterial("NormalStyle3D");


            cardMatShineUI = ABLoader.LoadMasterDuelMaterial("ShineStyleUI");
            cardMatShine3D = ABLoader.LoadMasterDuelMaterial("ShineStyle3D");
            cardMatShineRDUI = UnityEngine.Object.Instantiate(cardMatShineUI);
            SetMaterialToRD(cardMatShineRDUI);
            cardMatShineRD3D = UnityEngine.Object.Instantiate(cardMatShine3D);
            SetMaterialToRD(cardMatShineRD3D);

            cardMatShineOFUI = ABLoader.LoadMasterDuelMaterial("ShineStyleOFUI") ?? cardMatShineUI;
            cardMatShineOF3D = ABLoader.LoadMasterDuelMaterial("ShineStyleOF3D") ?? cardMatShine3D;
            cardMatShineOFRDUI = UnityEngine.Object.Instantiate(cardMatShineOFUI);
            SetMaterialToRD(cardMatShineOFRDUI);
            cardMatShineOFRD3D = UnityEngine.Object.Instantiate(cardMatShineOF3D);
            SetMaterialToRD(cardMatShineOFRD3D);


            cardMatRoyalUI = ABLoader.LoadMasterDuelMaterial("RoyalStyleUI");
            cardMatGoldUI = UnityEngine.Object.Instantiate(cardMatRoyalUI);
            SetMaterialToGold(cardMatGoldUI);
            cardMatMillenniumUI = UnityEngine.Object.Instantiate(cardMatRoyalUI);
            SetMaterialToMillennium(cardMatMillenniumUI);
            cardMatRoyalRDUI = UnityEngine.Object.Instantiate(cardMatRoyalUI);
            SetMaterialToRD(cardMatRoyalRDUI);
            cardMatGoldRDUI = UnityEngine.Object.Instantiate(cardMatGoldUI);
            SetMaterialToRD(cardMatGoldRDUI);
            cardMatMillenniumRDUI = UnityEngine.Object.Instantiate(cardMatMillenniumUI);
            SetMaterialToRD(cardMatMillenniumRDUI);
            cardMatRoyal3D = ABLoader.LoadMasterDuelMaterial("RoyalStyle3D");
            cardMatGold3D = UnityEngine.Object.Instantiate(cardMatRoyal3D);
            SetMaterialToGold(cardMatGold3D);
            cardMatMillennium3D = UnityEngine.Object.Instantiate(cardMatRoyal3D);
            SetMaterialToMillennium(cardMatMillennium3D);
            cardMatRoyalRD3D = UnityEngine.Object.Instantiate(cardMatRoyal3D);
            SetMaterialToRD(cardMatRoyalRD3D);
            cardMatGoldRD3D = UnityEngine.Object.Instantiate(cardMatGold3D);
            SetMaterialToRD(cardMatGoldRD3D);
            cardMatMillenniumRD3D = UnityEngine.Object.Instantiate(cardMatMillennium3D);
            SetMaterialToRD(cardMatMillenniumRD3D);

            cardMatRoyalOFUI = ABLoader.LoadMasterDuelMaterial("RoyalStyleOFUI") ?? cardMatRoyalUI;
            cardMatGoldOFUI = UnityEngine.Object.Instantiate(cardMatRoyalOFUI);
            SetMaterialToGold(cardMatGoldOFUI);
            cardMatMillenniumOFUI = UnityEngine.Object.Instantiate(cardMatRoyalOFUI);
            SetMaterialToMillennium(cardMatMillenniumOFUI);
            cardMatRoyalOFRDUI = UnityEngine.Object.Instantiate(cardMatRoyalOFUI);
            SetMaterialToRD(cardMatRoyalOFRDUI);
            cardMatGoldOFRDUI = UnityEngine.Object.Instantiate(cardMatGoldOFUI);
            SetMaterialToRD(cardMatGoldOFRDUI);
            cardMatMillenniumOFRDUI = UnityEngine.Object.Instantiate(cardMatMillenniumOFUI);
            SetMaterialToRD(cardMatMillenniumOFRDUI);
            cardMatRoyalOF3D = ABLoader.LoadMasterDuelMaterial("RoyalStyleOF3D") ?? cardMatRoyal3D;
            cardMatGoldOF3D = UnityEngine.Object.Instantiate(cardMatRoyalOF3D);
            SetMaterialToGold(cardMatGoldOF3D);
            cardMatMillenniumOF3D = UnityEngine.Object.Instantiate(cardMatRoyalOF3D);
            SetMaterialToMillennium(cardMatMillenniumOF3D);
            cardMatRoyalOFRD3D = UnityEngine.Object.Instantiate(cardMatRoyalOF3D);
            SetMaterialToRD(cardMatRoyalOFRD3D);
            cardMatGoldOFRD3D = UnityEngine.Object.Instantiate(cardMatGoldOF3D);
            SetMaterialToRD(cardMatGoldOFRD3D);
            cardMatMillenniumOFRD3D = UnityEngine.Object.Instantiate(cardMatMillenniumOF3D);
            SetMaterialToRD(cardMatMillenniumOFRD3D);
        }

        private static void SetMaterialToRD(Material material)
        {
            material.SetTexture("_CardMask", TextureManager.container.rd_Mask);
            material.SetTexture("_KiraMask", TextureManager.container.rd_KiraMask);
            material.SetTexture("_MainNormal", TextureManager.container.rd_CardNormal);
            material.SetTexture("_AttributeTex", TextureManager.container.rd_CardAttributeSet);
            material.SetVector("_AttributeSize_Pos", new Vector4(8.31f, 12.26f, -3.19f, -5.13f));
        }

        private static void SetMaterialToGold(Material material)
        {
            material.SetFloat("_CardDistortion01", 1.2f);
            material.SetFloat("_Kira01_01Tile", 0.25f);
            material.SetFloat("_Kira01_01Power", 3f);
            material.SetColor("_KiraColor02", new Color(0.5f, 0.5f, 0f, 0f));
            material.SetColor("_CubemapColor", new Color(0.7f, 0.7f, 0f, 0f));
        }

        private static void SetMaterialToMillennium(Material material)
        {
            material.SetTexture("_HighlightNormal"
                , TextureManager.container.CardKiraNormal03_Millennium);
            material.SetColor("_CubemapColor", new Color(0.898f, 0.3245f, 0.7723f, 0f));
            material.SetColor("_KiraColor02", new Color(0.3099f, 0.1633f, 0.2753f, 0f));
            material.SetFloat("_Kira01_01Tile", 0.25f);
            material.SetFloat("_Kira01_02Tile", 0f);
            material.SetFloat("_RanbowPower", 0.5f);
        }

        private static Color GetMillenniumFrameColor(Card data)
        {
            Color color;
            if (data.HasType(CardType.Pendulum))
                color = new Color(0.3099f, 0.1633f, 0.2753f, 0f);
            else if (data.HasType(CardType.Spell))
                color = new Color(0f, 0.8867f, 1f, 0f);
            else if (data.HasType(CardType.Trap))
                color = new Color(1f, 0f, 1f, 0f);
            else if (data.HasType(CardType.Normal))
                color = new Color(1f, 0.6f, 0f, 0f);
            else if (data.HasType(CardType.Fusion))
                color = new Color(1f, 0f, 1f, 0f);
            else if (data.HasType(CardType.Ritual))
                color = new Color(0f, 0.2f, 1f, 0f);
            else if (data.HasType(CardType.Synchro))
                color = new Color(0.4f, 0.4f, 0.4f, 0f);
            else if (data.HasType(CardType.Xyz))
                color = new Color(0.1f, 0.1f, 0.1f, 0f);
            else if (data.HasType(CardType.Link))
                color = new Color(0f, 0.4f, 1f, 0f);
            else
                color = new Color(1f, 0.2357f, 0f, 0f);
            return color;
        }

        private static Color GetMillenniumNameColor(Card data)
        {
            if (data.HasType(CardType.Spell))
                return new Color(0f, 1f, 1f, 1f);
            else if (data.HasType(CardType.Trap))
                return new Color(1f, 0f, 0.5f, 1f);
            else if ((data.Attribute & (uint)CardAttribute.Light) > 0)
                return new Color(1f, 1f, 0f, 1f);
            else if ((data.Attribute & (uint)CardAttribute.Divine) > 0)
                return new Color(1f, 1f, 0f, 1f);
            else if ((data.Attribute & (uint)CardAttribute.Dark) > 0)
                return new Color(1f, 0f, 1f, 1f);
            else if ((data.Attribute & (uint)CardAttribute.Water) > 0)
                return new Color(0f, 1f, 1f, 1f);
            else if ((data.Attribute & (uint)CardAttribute.Fire) > 0)
                return new Color(1f, 0f, 0f, 1f);
            else if ((data.Attribute & (uint)CardAttribute.Earth) > 0)
                return new Color(0.8f, 0.8f, 0.8f, 1f);
            else if ((data.Attribute & (uint)CardAttribute.Wind) > 0)
                return new Color(0f, 1f, 0f, 1f);
            else
                return new Color(1f, 1f, 0f, 1f);
        }

        public static Material GetCardMaterial(int code, bool use3D = false)
        {
            Material normalSource = use3D ? cardMatNormal3D : cardMatNormalUI;
            if (code < 0)
                return normalSource == null ? null : UnityEngine.Object.Instantiate(normalSource);

            bool rushDuel = CardRenderer.NeedRushDuelStyle(code);
            var rarity = CardRarity.GetRarity(code);
            var overFrame = CardImageLoader.GetOverFrame(code);

            Material source = null;
            switch (rarity)
            {
                case CardRarity.Rarity.Normal:
                    source = normalSource;
                    break;
                case CardRarity.Rarity.Shine:
                    source = overFrame == null ? rushDuel ? use3D ? cardMatShineOFRD3D : cardMatShineOFRDUI : use3D ? cardMatShineOF3D : cardMatShineOFUI
                        : rushDuel ? use3D ? cardMatShineOFRD3D : cardMatShineOFRDUI : use3D ? cardMatShineOF3D : cardMatShineOFUI;
                    break;
                case CardRarity.Rarity.Royal:
                    source = overFrame == null ? rushDuel ? use3D ? cardMatRoyalRD3D : cardMatRoyalRDUI : use3D ? cardMatRoyal3D : cardMatRoyalUI
                        : rushDuel ? use3D ? cardMatRoyalOFRD3D : cardMatRoyalOFRDUI : use3D ? cardMatRoyalOF3D : cardMatRoyalOFUI;
                    break;
                case CardRarity.Rarity.Gold:
                    source = overFrame == null ? rushDuel ? use3D ? cardMatGoldRD3D : cardMatGoldRDUI : use3D ? cardMatGold3D : cardMatGoldUI
                        : rushDuel ? use3D ? cardMatGoldOFRD3D : cardMatGoldOFRDUI : use3D ? cardMatGoldOF3D : cardMatGoldOFUI;
                    break;
                case CardRarity.Rarity.Millennium:
                    source = overFrame == null ? rushDuel ? use3D ? cardMatMillenniumRD3D : cardMatMillenniumRDUI : use3D ? cardMatMillennium3D : cardMatMillenniumUI
                        : rushDuel ? use3D ? cardMatMillenniumOFRD3D : cardMatMillenniumOFRDUI : use3D ? cardMatMillenniumOF3D : cardMatMillenniumOFUI;
                    break;
            }

            // Shine and royal sources are often missing. Instantiating a null material left the art window on the loading texture.
            if (source == null) source = normalSource;
            if (source == null) return null;
            Material mat = UnityEngine.Object.Instantiate(source);
            if (rarity != CardRarity.Rarity.Normal && source == normalSource)
                return mat;

            if (rarity != CardRarity.Rarity.Normal)
            {
                var data = CardsManager.Get(code);
                if (data.HasType(CardType.Spell))
                    mat.SetFloat("_AttributeTile", 7);
                else if (data.HasType(CardType.Trap))
                    mat.SetFloat("_AttributeTile", 8);
                else if ((data.Attribute & (uint)CardAttribute.Light) > 0)
                    mat.SetFloat("_AttributeTile", 0);
                else if ((data.Attribute & (uint)CardAttribute.Dark) > 0)
                    mat.SetFloat("_AttributeTile", 1);
                else if ((data.Attribute & (uint)CardAttribute.Water) > 0)
                    mat.SetFloat("_AttributeTile", 2);
                else if ((data.Attribute & (uint)CardAttribute.Fire) > 0)
                    mat.SetFloat("_AttributeTile", 3);
                else if ((data.Attribute & (uint)CardAttribute.Earth) > 0)
                    mat.SetFloat("_AttributeTile", 4);
                else if ((data.Attribute & (uint)CardAttribute.Wind) > 0)
                    mat.SetFloat("_AttributeTile", 5);
                else if ((data.Attribute & (uint)CardAttribute.Divine) > 0)
                    mat.SetFloat("_AttributeTile", 6);

                var nameTask = CardImageLoader.LoadCardName(code);
                mat.SetTexture("_MonsterNameTex", nameTask);

                if (rushDuel)
                {
                    if (data.HasType(CardType.Pendulum))
                        mat.SetTexture("_KiraMask", TextureManager.container.rd_KiraMaskPendulum);
                }
                else
                {
                    if (data.HasType(CardType.Link))
                    {
                        mat.SetTexture("_CardMask", TextureManager.container.cardFrameMaskLink);
                        mat.SetTexture("_KiraMask", TextureManager.container.cardKiraMaskLink);
                        mat.SetTexture("_MainNormal", TextureManager.container.cardNormalLink);
                        if (rarity == CardRarity.Rarity.Shine)
                            mat.SetFloat("_LinkOn_Off", 1f);
                    }
                    else if (data.HasType(CardType.Pendulum))
                    {
                        mat.SetTexture("_CardMask", TextureManager.container.cardFrameMaskPendulum);
                        mat.SetTexture("_KiraMask", TextureManager.container.cardKiraMaskPendulum);
                        mat.SetTexture("_MainNormal", TextureManager.container.cardNormalPendulum);
                    }
                    else
                    {
                        mat.SetTexture("_CardMask", TextureManager.container.cardFrameMask);
                        mat.SetTexture("_KiraMask", TextureManager.container.cardKiraMask);
                        mat.SetTexture("_MainNormal", TextureManager.container.cardNormal);
                    }

                    if (Language.AttributeNeedRuby())
                        mat.SetVector("_AttributeSize_Pos", new Vector4(9.85f, 13.96f, -3.7f, -5.81f));
                }

                if (rarity == CardRarity.Rarity.Millennium)
                {
                    mat.SetColor("_KiraColor02", GetMillenniumFrameColor(data));
                    mat.SetColor("_CubemapColor", GetMillenniumNameColor(data));
                }

                if(overFrame != null)
                {
                    if(!cachedOverFrameMasks.TryGetValue(code, out var mask))
                    {
                        var descMask = TextureManager.container.GetDescMask(GetCardStyleByCode(code), data.HasType(CardType.Pendulum));
                        mask = TextureProcessor.ApplyMaskToAlpha(overFrame, descMask, invertMask: true);
                        mask = TextureProcessor.InvertAlpha(mask);
#if UNITY_EDITOR
                        mask.alphaIsTransparency = true;
#endif
                        cachedOverFrameMasks[code] = mask;
                    }
                    mat.SetTexture("_OverFrameMask", mask);
                }
            }

            return mat;
        }

        public static void ClearCachedOverFrameMasks()
        {
            foreach (var mask in cachedOverFrameMasks.Values)
                UnityEngine.Object.Destroy(mask);
            cachedOverFrameMasks.Clear();
        }

        #endregion

        #region Load Material

        private static readonly ConcurrentDictionary<string, Material> _loadedMaterials = new();
        private static readonly ConcurrentDictionary<string, Task<Material>> _loadMaterialTasks = new();

        private static async Task<Material> LoadMaterialAsync(string materialName, CancellationToken token)
        {
            var mat = await ABLoader.LoadMaterialAsync("MasterDuel/Material/" + materialName, token);
            _loadedMaterials.TryAdd(materialName, mat);
            _loadMaterialTasks.TryRemove(materialName, out _);
            return mat;
        }

        public static async UniTask<Material> LoadMaterialByNameAsync(string materialName)
        {
            if (_loadedMaterials.TryGetValue(materialName, out var material))
            {
                return material;
            }

            if (_loadMaterialTasks.TryGetValue(materialName, out var task))
            {
                return await task;
            }
            else
            {
                using var cts = new CancellationTokenSource();
                task = LoadMaterialAsync(materialName, cts.Token);
                if (_loadMaterialTasks.TryAdd(materialName, task))
                {
                    return await task;
                }
                else
                {
                    cts.Cancel();
                    return await _loadMaterialTasks[materialName];
                }
            }
        }

        #endregion

        #region Load Shader

        private static readonly ConcurrentDictionary<string, Shader> _loadedShaders = new();
        private static readonly ConcurrentDictionary<string, UniTask<Shader>> _loadShaderTasks = new();

        private static async UniTask<Shader> LoadShaderAsync(string shaderName, CancellationToken token)
        {
            var shader = await ABLoader.LoadShaderAsync("MasterDuel/Shader/" + shaderName, token);
            _loadedShaders.TryAdd(shaderName, shader);
            _loadShaderTasks.TryRemove(shaderName, out _);
            return shader;
        }

        public static async UniTask<Shader> LoadShaderByNameAsync(string shaderName)
        {
            if (_loadedShaders.TryGetValue(shaderName, out var shader))
                return shader;

            if (_loadShaderTasks.TryGetValue(shaderName, out var task))
            {
                return await task;
            }
            else
            {
                using var cts = new CancellationTokenSource();
                task = LoadShaderAsync(shaderName, cts.Token);
                if (_loadShaderTasks.TryAdd(shaderName, task))
                {
                    return await task;
                }
                else
                {
                    cts.Cancel();
                    return await _loadShaderTasks[shaderName];
                }
            }
        }

        #endregion

    }
}
