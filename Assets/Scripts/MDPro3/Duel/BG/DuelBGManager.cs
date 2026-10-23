using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using YgomGame.Bg;
using YgomSystem.Effect;
using YgomSystem.ElementSystem;
using YgomSystem.Timeline;
using static MDPro3.Servant.OcgCore;
using static YgomGame.Bg.BgEffectSettingInner;
using static YgomGame.Bg.BgUnit;

namespace MDPro3.Duel
{
    public class DuelBGManager
    {
        #region Parameters

        private OcgCore Core => Program.instance.ocgcore;
        private Deck deck;

        private bool mate0Random = true;
        private bool mate1Random = true;
        private Tween mate0RandomCooldownTween;
        private Tween mate1RandomCooldownTween;
        private int bgPhase0 = 0;
        private int bgPhase1 = 0;
        private bool backgroundFieldInitialize = false;
        public bool loaded;

        private float field0TapTime;
        private float field1TapTime;
        private float mate0TapTime;
        private float mate1TapTime;

        #endregion

        #region Objects

        public DuelMessage processor;

        public readonly List<GameObject> allGameObjects = new();
        private readonly List<GameObject> turnEndDeleteObjects = new();
        private static readonly List<int> cardEffectCodes = new();
        private static readonly List<GameObject> cardEffects = new();

        private GameObject attackLine;
        private GameObject targetLine;
        private readonly List<GameObject> targetLines = new();
        private GameObject equipLine;
        public GameObject fieldSummonRightInfo;

        public BgEffectManager field0Manager;
        public BgEffectManager field1Manager;
        private BgEffectManager grave0Manager;
        private BgEffectManager grave1Manager;
        private readonly List<GraveBehaviour> graves = new();
        private BgEffectManager stand0Manager;
        private BgEffectManager stand1Manager;
        private Mate mate0;
        private Mate mate1;
        private PremiumMateState premiumMate0;
        private PremiumMateState premiumMate1;

        private GameObject phaseButton;
        private TimerHandler timerHandler;
        private PlayableGuide playableGuide;
        public ElementObjectManager myDeck;
        public ElementObjectManager myExtra;
        public ElementObjectManager opDeck;
        public ElementObjectManager opExtra;
        public List<PlaceSelector> places = new();
        private DuelFinalBlow duelFinalBlow;

        #endregion

        private sealed class PremiumMateState
        {
            public int Side;
            public PremiumMateRule Rule;
            public PremiumMateSwapEffect SwapEffect;
            public Transform Anchor;
            public readonly Dictionary<int, Mate> Forms = new();
            public int ActiveMateId;
            public bool IsTransitioning;
            public int QueuedMateId;
            public bool IsPermanentSub;
            public bool InBattlePhaseSub;
            public bool PendingDirectAttack;
            public IReadOnlyList<string> OverrideTriggerPriority;
            public int PendingTurnSwapToken;
            public int RayeEngageMateId;
            public int RayeBattleMateId;
        }

        private readonly struct SwapEffectLabelPlaybackResult
        {
            public bool Consumed { get; }

            public SwapEffectLabelPlaybackResult(bool consumed)
            {
                Consumed = consumed;
            }

            public static SwapEffectLabelPlaybackResult None => new(false);
        }

        #region Public

        public async UniTask LoadAssetsAsync()
        {
            loaded = false;
            ResetPremiumMateStates();

            deck = null;
            var deckName = Config.GetConfigDeckName();
            if (condition == Condition.Duel
                && !inPuzzle
                && File.Exists(Program.PATH_DECK + deckName + Program.EXPANSION_YDK))
                deck = new Deck(Program.PATH_DECK + deckName + Program.EXPANSION_YDK);

            var hasSide0Appearance = RoomServant.TryGetOnlineAppearanceForSide(0, out var side0Appearance);
            var hasSide1Appearance = RoomServant.TryGetOnlineAppearanceForSide(1, out var side1Appearance);
            var overrideDeckAppearance = Config.GetBool("OverrideDeckAppearance", false);

            UIManager.UIBlackIn(Core.TransitionTime);
            await UniTask.WaitForSeconds(Core.TransitionTime);
            await UniTask.WaitUntil(() => Appearance.loaded);
            if(inPuzzle || condition != Condition.Duel)
                await Program.instance.appearance.LoadSettingAssets();
            await ABLoader.CacheMasterDuelBundles();
            Program.instance.ocgcore.LoadDuelButton();

            CameraManager.ShiftTo3D();
            UIManager.HideExitButton(0);
            UIManager.HideLine(0);
            AudioManager.StopBGM();

            if (attackLine == null)
            {
                attackLine = ABLoader.LoadMasterDuelGameObject("fxp_atk_select_arrow_001");
                attackLine.SetActive(false);
                await UniTask.Yield();
            }
            if (targetLine == null)
            {
                targetLine = ABLoader.LoadMasterDuelGameObject("fxp_target_arrow_001");
                targetLine.SetActive(false);
                await UniTask.Yield();
            }
            if (equipLine == null)
            {
                equipLine = ABLoader.LoadMasterDuelGameObject("fxp_equip_arrow_001");
                equipLine.SetActive(false);
                await UniTask.Yield();
            }
            if (fieldSummonRightInfo == null)
            {
                var handle = Addressables.InstantiateAsync("Prefab/FieldSummonRightInfo.prefab");
                await handle;
                fieldSummonRightInfo = handle.Result;
                fieldSummonRightInfo.SetActive(false);
                fieldSummonRightInfo.transform.SetParent(Program.instance.container_3D);
            }

            #region Field

            var path = Program.items.GetAssetPath(
                Config.Get(condition.ToString() + "Field0",
                Program.items.mats[0].id.ToString()), Items.ItemType.Mat);
            if (condition == Condition.Watch && hasSide0Appearance)
                path = Program.items.GetAssetPath(side0Appearance.Field.ToString(), Items.ItemType.Mat);
            else if (deck != null && !overrideDeckAppearance)
                path = Program.items.GetAssetPath(deck.Field.ToString(), Items.ItemType.Mat);
            path = "MasterDuel/" + path;
            var field0 = await ABLoader.LoadFromFileAsync(path + "_near", false, true);
            field0.transform.SetParent(Program.instance.container_3D, false);
            field0Manager = field0.GetComponent<BgEffectManager>();

            var field1Config = Config.Get(condition.ToString() + "Field1",
                Program.items.mats[0].id.ToString());
            if (hasSide1Appearance)
                field1Config = side1Appearance.Field.ToString();
            var field1 = await ABLoader.LoadFromFileAsync("MasterDuel/" +
                Program.items.GetAssetPath(field1Config, Items.ItemType.Mat, 1) + "_far"
                , false, true);
            field1.transform.SetParent(Program.instance.container_3D, false);
            field1Manager = field1.GetComponent<BgEffectManager>();

            var collider = field0.AddComponent<BoxCollider>();
            collider.center = new Vector3(38, 5, -10);
            collider.size = new Vector3(10, 10, 10);
            collider = field1.AddComponent<BoxCollider>();
            collider.center = new Vector3(-38, 5, 10);
            collider.size = new Vector3(10, 10, 10);

            Transform pos_Grave_near = field0.transform.GetChildByName("POS_Grave_near");
            Transform pos_Grave_far = field1.transform.GetChildByName("POS_Grave_far");
            Transform pos_AvatarStand_near = field0.transform.GetChildByName("POS_AvatarStand_near");
            Transform pos_AvatarStand_far = field1.transform.GetChildByName("POS_AvatarStand_far");
            Transform pos_Avatar_near = field0.transform.GetChildByName("POS_Avatar_near");
            Transform pos_Avatar_far = field1.transform.GetChildByName("POS_Avatar_far");

            allGameObjects.Add(field0);
            allGameObjects.Add(field1);

            #endregion

            #region Grave

            path = Program.items.GetAssetPath(
                Config.Get(condition.ToString() + "Grave0",
                Program.items.graves[0].id.ToString()), Items.ItemType.Grave);
            if (condition == Condition.Watch && hasSide0Appearance)
                path = Program.items.GetAssetPath(side0Appearance.Grave.ToString(), Items.ItemType.Grave);
            else if (deck != null && !overrideDeckAppearance)
                path = Program.items.GetAssetPath(deck.Grave.ToString(), Items.ItemType.Grave);
            path = "MasterDuel/" + path;

            var grave0 = await ABLoader.LoadFromFileAsync(path + "_near", false, true);
            grave0.transform.SetParent(pos_Grave_near, false);
            grave0Manager = grave0.GetComponent<BgEffectManager>();

            var grave1Config = Config.Get(condition.ToString() + "Grave1",
                Program.items.graves[0].id.ToString());
            if (hasSide1Appearance)
                grave1Config = side1Appearance.Grave.ToString();
            var grave1 = await ABLoader.LoadFromFileAsync("MasterDuel/" +
                Program.items.GetAssetPath(grave1Config, Items.ItemType.Grave, 1) + "_far"
                , false, true);
            grave1.transform.SetParent(pos_Grave_far, false);
            grave1Manager = grave1.GetComponent<BgEffectManager>();

            Tools.PlayAnimation(grave0.transform, "StartToPhase1");
            Tools.PlayAnimation(grave1.transform, "StartToPhase1");

            graves.Clear();
            var g0 = grave0.AddComponent<GraveBehaviour>();
            g0.controller = 0;
            graves.Add(g0);
            var g1 = grave1.AddComponent<GraveBehaviour>();
            g1.controller = 1;
            graves.Add(g1);

            #endregion

            #region Stand

            var standConfig = Config.Get(condition.ToString() + "Stand0", Program.items.stands[0].id.ToString());
            if (condition == Condition.Watch && hasSide0Appearance)
                standConfig = side0Appearance.Stand.ToString();
            if (standConfig != Items.CODE_NONE.ToString() || deck != null)
            {
                path = Program.items.GetAssetPath(standConfig, Items.ItemType.Stand);
                if (deck != null && !overrideDeckAppearance)
                    path = Program.items.GetAssetPath(deck.Stand.ToString(), Items.ItemType.Stand);
                path = "MasterDuel/" + path;
                var stand0 = await ABLoader.LoadFromFileAsync(path + "_near", false, true);
                stand0.transform.SetParent(pos_AvatarStand_near, false);

                pos_Avatar_near = stand0.transform.GetChildByName("POS_Avatar_near");
                Tools.PlayAnimation(stand0.transform, "StartToPhase1");
                stand0Manager = stand0.GetComponent<BgEffectManager>();
            }

            standConfig = Config.Get(condition.ToString() + "Stand1", Program.items.stands[0].id.ToString());
            if (hasSide1Appearance)
                standConfig = side1Appearance.Stand.ToString();
            if (standConfig != Items.CODE_NONE.ToString())
            {
                var stand1 = await ABLoader.LoadFromFileAsync("MasterDuel/" +
                Program.items.GetAssetPath(standConfig, Items.ItemType.Stand, 1) + "_far"
                , false, true);
                stand1.transform.SetParent(pos_AvatarStand_far, false);

                pos_Avatar_far = stand1.transform.GetChildByName("POS_Avatar_far");
                Tools.PlayAnimation(stand1.transform, "StartToPhase1");
                stand1Manager = stand1.GetComponent<BgEffectManager>();
            }

            #endregion

            #region Mate

            var mateConfig = Config.Get(condition.ToString() + "Mate0", Program.items.mates[0].id.ToString());
            if (condition == Condition.Watch && hasSide0Appearance)
                mateConfig = side0Appearance.Mate.ToString();
            int mateCode = int.Parse(mateConfig);
            if (deck != null && !overrideDeckAppearance)
                mateCode = deck.Mate;
            await LoadMateForSideAsync(0, mateCode, pos_Avatar_near);

            mateConfig = Config.Get(condition.ToString() + "Mate1", Program.items.mates[0].id.ToString());
            if (hasSide1Appearance)
                mateConfig = side1Appearance.Mate.ToString();
            await LoadMateForSideAsync(1, int.Parse(mateConfig), pos_Avatar_far);

            #endregion

            #region CelestialSphere

            var matBack = ABLoader.LoadMasterDuelGameObject("CelestialSphere_c001");
            matBack.transform.SetParent(Program.instance.container_3D, false);
            matBack.transform.localScale = Vector3.one * 2;

            allGameObjects.Add(matBack);

            #endregion

            #region PhaseButton

            if (MatIsSpecial(field1.name))
            {
                phaseButton = await ABLoader.LoadFromFileAsync("MasterDuel/BG/Timer/PhaseButton_013", true, true);
                phaseButton.GetComponent<Animator>().SetTrigger("Start");
                Tools.PlayAnimation(phaseButton.transform, "StartToPhase1");
            }
            else
            {
                phaseButton = ABLoader.LoadMasterDuelGameObject("PhaseButton_c001");
                await UniTask.Yield();
            }

            phaseButton.transform.SetParent(Program.instance.container_3D, false);
            allGameObjects.Add(phaseButton);
            phaseButton.AddComponent<PhaseButtonHandler>();

            #endregion

            #region Timer

            if (condition == OcgCore.Condition.Duel && !OcgCore.inputMode)
            {
                GameObject timer;
                if (MatIsSpecial(field1.name))
                    timer = await ABLoader.LoadFromFileAsync("MasterDuel/BG/Timer/Timer_013", true, true);
                else
                {
                    timer = ABLoader.LoadMasterDuelGameObject("Timer_c001");
                    await UniTask.Yield();
                }

                timerHandler = timer.AddComponent<TimerHandler>();
                timer.transform.SetParent(Program.instance.container_3D, false);
                timerHandler.timeLimit = timeLimit;
                timerHandler.time = timeLimit;
                allGameObjects.Add(timer);
            }

            #endregion

            #region Playable Guide

            playableGuide = Create<PlayableGuide>();
            playableGuide.Load(field0.name, field1.name);
            await UniTask.WaitUntil(() => playableGuide.loaded);

            if (DeviceInfo.OnAndroid())
                playableGuide.SetHeight(0.6f);

            #endregion

            #region Deck Model

            var deckMat = Appearance.duelProtector0;
            if (deck != null && !overrideDeckAppearance)
                deckMat = await ABLoader.LoadProtectorMaterial(deck.Protector.ToString(), Application.exitCancellationToken);

            if (condition == Condition.Duel)
                myProtector = deckMat;
            else if (condition == Condition.Watch)
            {
                myProtector = Appearance.watchProtector0;
                if (hasSide0Appearance)
                {
                    var mat = await ABLoader.LoadProtectorMaterial(side0Appearance.Protector.ToString(), Application.exitCancellationToken);
                    if (mat != null)
                        myProtector = mat;
                }
            }
            else if (condition == Condition.Replay)
                myProtector = Appearance.replayProtector0;

            if (condition == Condition.Duel)
            {
                opProtector = Appearance.duelProtector1;
                if (hasSide1Appearance)
                {
                    var mat = await ABLoader.LoadProtectorMaterial(side1Appearance.Protector.ToString(), Application.exitCancellationToken);
                    if (mat != null)
                        opProtector = mat;
                }
            }
            else if (condition == Condition.Watch)
            {
                opProtector = Appearance.watchProtector1;
                if (hasSide1Appearance)
                {
                    var mat = await ABLoader.LoadProtectorMaterial(side1Appearance.Protector.ToString(), Application.exitCancellationToken);
                    if (mat != null)
                        opProtector = mat;
                }
            }
            else if (condition == Condition.Replay)
                opProtector = Appearance.replayProtector1;

            var deckModel = ABLoader.LoadMasterDuelGameObject("DuelDeckAppearance");

            myDeck = deckModel.GetComponent<ElementObjectManager>();
            InitializeDeckModel(myDeck, 0, CardLocation.Deck);
            myExtra = UnityEngine.Object.Instantiate(deckModel).GetComponent<ElementObjectManager>();
            InitializeDeckModel(myExtra, 0, CardLocation.Extra);
            opDeck = UnityEngine.Object.Instantiate(deckModel).GetComponent<ElementObjectManager>();
            InitializeDeckModel(opDeck, 1, CardLocation.Deck);
            opExtra = UnityEngine.Object.Instantiate(deckModel).GetComponent<ElementObjectManager>();
            InitializeDeckModel(opExtra, 1, CardLocation.Extra);

            #endregion

            #region Places

            places.Clear();
            for (uint c = 0; c < 2; c++)
            {
                GPS gps = new()
                {
                    controller = c,
                    location = (uint)CardLocation.Deck
                };
                CreatePlaceSelector(gps);

                gps = new()
                {
                    controller = c,
                    location = (uint)CardLocation.Extra
                };
                CreatePlaceSelector(gps);

                for (uint s = 0; s < (c == 0 && MasterRule != 8 ? 7 : 5); s++)
                {
                    gps = new()
                    {
                        controller = c,
                        location = (uint)CardLocation.MonsterZone,
                        sequence = s
                    };
                    CreatePlaceSelector(gps);
                }
                for (uint s = 0; s < 6; s++)
                {
                    gps = new()
                    {
                        controller = c,
                        location = (uint)CardLocation.SpellZone,
                        sequence = s
                    };
                    CreatePlaceSelector(gps);
                }
            }

            #endregion

            #region Quit Load

            await processor.PreloadPlayerNames();
            if (Core.NeedVoice())
            {
                Core.GetUI<OcgCoreUI>().CG.alpha = 1;
                Core.GetUI<OcgCoreUI>().CG.blocksRaycasts = true;
            }
            Core.GetUI<OcgCoreUI>().Buttons.SetActive(false);

            UIManager.ShowFPSLeft();
            UIManager.HideBlackBack(0f);
            UIManager.UIBlackOut(Core.TransitionTime);
            await UniTask.WaitForSeconds(Core.TransitionTime);

            backgroundFieldInitialize = false;
            BackgroundFieldInitialize();

            if (condition == Condition.Duel && Config.GetBool("DuelAutoAcc", false)
                || condition == Condition.Watch && Config.GetBool("WatchAutoAcc", false)
                || condition == Condition.Replay && Config.GetBool("ReplayAutoAcc", false))
                Core.GetUI<OcgCoreUI>().OnAcc();

            #endregion

            loaded = true;
        }

        public async UniTask ExitDuelAsync()
        {
            ClearResponse();
            CameraManager.BlackOut(0f, 0.3f);

            UIManager.UIBlackIn(Core.TransitionTime);
            Core.GetUI<OcgCoreUI>().CloseHint();
            HideAttackLine();
            HideDuelFinalBlowText();
            await UniTask.WaitForSeconds(Core.TransitionTime);
            Core.servantUI.ShutDown();

            NoMoreWait = true;
            packages.Clear();
            allPackages.Clear();
            AudioManager.ResetSESource();
            mycardDuel = false;
            Core.CloseCharaFace();
            Dispose();

            foreach (var card in cards)
                card.Dispose();
            cards.Clear();
            pause = false;
            nextMoveAction = null;
            Core.cachedCharaFaces.Clear();
            CameraManager.ShiftTo2D();
            Core.GetUI<OcgCoreUI>().Buttons.SetActive(false);
            Core.GetUI<OcgCoreUI>().DuelLog.ClearLog();
            Program.instance.ui_.chatPanel.Hide();
            await UniTask.WaitForSeconds(0.3f);
            UIManager.UIBlackOut(Core.TransitionTime);
            await UniTask.WaitForSeconds(Core.TransitionTime);
            UIManager.ShowFPSRight();
            AudioManager.PlayBGM(AudioManager.BGM_MENU_MAIN);
        }

        private void InitializeDeckModel(ElementObjectManager deckManager, int player, CardLocation location)
        {
            deckManager.transform.SetParent(player == 0 ? field0Manager.transform : field1Manager.transform, false);
            deckManager.transform.localPosition = _positionMap[(player, location)];
            deckManager.transform.localEulerAngles = _angleMap[(player, location)];
            allGameObjects.Add(deckManager.gameObject);

            var mat = player == 0 ? myProtector : opProtector;
            deckManager.GetNestedElement<MeshRenderer>("DummyDeck/DummyCardModel_back").material = mat;
            deckManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel01_back").material = mat;
            deckManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel02_back").material = mat;
            deckManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel03_back").material = mat;
            deckManager.GetNestedElement<MeshRenderer>("CardShuffleTop/CardModel04_back").material = mat;

            deckManager.gameObject.SetActive(false);
        }

        private static readonly Dictionary<(int, CardLocation), Vector3> _positionMap = new()
        {
            [(0, CardLocation.Deck)] = new(26.6f, 1.5f, -23.5f),
            [(0, CardLocation.Extra)] = new(-26.6f, 1.5f, -23.5f),
            [(1, CardLocation.Deck)] = new(-26.6f, 1.5f, 23.5f),
            [(1, CardLocation.Extra)] = new(26.6f, 1.5f, 23.5f)
        };

        private static readonly Dictionary<(int, CardLocation), Vector3> _angleMap = new()
        {
            [(0, CardLocation.Deck)] = new(0f, -20f, 0f),
            [(0, CardLocation.Extra)] = new(0f, 20f, 0f),
            [(1, CardLocation.Deck)] = new(0f, 160f, 0f),
            [(1, CardLocation.Extra)] = new(0f, -160f, 0f)
        };

        public async UniTask ShowDecksAsync()
        {
            if (myDeck == null || myExtra == null
                || opDeck == null || opExtra == null)
                return;

            await ShowAllDeckModelsAsync();

            Core.GetUI<OcgCoreUI>().CG.alpha = 1;
            Core.GetUI<OcgCoreUI>().CG.blocksRaycasts = true;
            Core.GetUI<OcgCoreUI>().Buttons.SetActive(true);
            AudioManager.PlayBgmNormal(Config.GetBool("BGMbyMySide", true) ? field0Manager.name : field1Manager.name);
        }

        public async UniTask ShowDecksWithDuelStartTextAsync()
        {
            if (myDeck == null || myExtra == null
                || opDeck == null || opExtra == null)
                return;

            await ShowAllDeckModelsAsync();

            var effect = ABLoader.LoadMasterDuelGameObject("DuelTextStart");
            var director = effect.GetComponent<PlayableDirector>();
            await director.WaitAsync();
            UnityEngine.Object.Destroy(effect);

            Core.GetUI<OcgCoreUI>().CG.alpha = 1;
            Core.GetUI<OcgCoreUI>().CG.blocksRaycasts = true;
            Core.GetUI<OcgCoreUI>().Buttons.SetActive(true);
            AudioManager.PlayBgmNormal(Config.GetBool("BGMbyMySide", true) ? field0Manager.name : field1Manager.name);
        }

        public void BackgroundFieldInitialize()
        {
            if (field0Manager == null || field1Manager == null)
                return;
            if (bgPhase0 == 1 && bgPhase1 == 1) return;

            if (backgroundFieldInitialize)
            {
                field0Manager.gameObject.SetActive(false);
                field1Manager.gameObject.SetActive(false);
                field0Manager.gameObject.SetActive(true);
                field1Manager.gameObject.SetActive(true);
            }

            field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.StartToPhase1);
            grave0Manager.PlayAnimatorTrigger(TriggerLabelDefine.StartToPhase1);
            bgPhase0 = 1;
            field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.StartToPhase1);
            grave1Manager.PlayAnimatorTrigger(TriggerLabelDefine.StartToPhase1);
            bgPhase1 = 1;

            InitializeMateEntry(0);
            InitializeMateEntry(1);

            if (timerHandler != null)
                timerHandler.DuelStart();

            RestartMateRandomCooldown(0);
            RestartMateRandomCooldown(1);

            backgroundFieldInitialize = true;
        }

        public void OnNewTurn(bool myTurn, int turn)
        {
            ApplyPremiumNewTurn(GetPremiumMateState(0), myTurn, turn);
            ApplyPremiumNewTurn(GetPremiumMateState(1), !myTurn, turn);
        }

        public void OnNewPhase(int turnPlayer, DuelPhase phase)
        {
            ApplyPremiumPhase(GetPremiumMateState(0), turnPlayer == 0, phase);
            ApplyPremiumPhase(GetPremiumMateState(1), turnPlayer == 1, phase);
        }

        public void OnSpecialSummonFromExtra(int ownerPlayer)
        {
            var state = GetPremiumMateState(ownerPlayer);
            if (state == null)
                return;

            if (state.Rule.Behavior == PremiumMateBehavior.GaiaExtraDeckPermanent
                || state.Rule.Behavior == PremiumMateBehavior.FiendsmithExtraDeckOrEquipPermanent)
            {
                if (state.IsPermanentSub)
                    return;
                state.IsPermanentSub = true;
                RequestPremiumMateForm(state, state.Rule.SubId);
            }
        }

        public void OnEquipApplied(int ownerPlayer)
        {
            var state = GetPremiumMateState(ownerPlayer);
            if (state == null)
                return;
            if (state.Rule.Behavior != PremiumMateBehavior.FiendsmithExtraDeckOrEquipPermanent)
                return;
            if (state.IsPermanentSub)
                return;

            state.IsPermanentSub = true;
            RequestPremiumMateForm(state, state.Rule.SubId);
        }

        public void OnLifePointsChanged(int player, int lifePoint)
        {
            var state = GetPremiumMateState(player);
            if (state == null)
                return;
            if (state.Rule.Behavior != PremiumMateBehavior.ShuraigLpThreshold)
                return;
            if (state.IsPermanentSub)
                return;
            if (lifePoint > state.Rule.LpThreshold)
                return;

            state.IsPermanentSub = true;
            RequestPremiumMateForm(state, state.Rule.SubId);
        }

        public void OnDirectAttack(int attackerPlayer)
        {
            var state = GetPremiumMateState(attackerPlayer);
            if (state == null)
                return;
            if (state.Rule.Behavior != PremiumMateBehavior.RayeBattlePhaseAndDirectAttack)
                return;
            var battleMateId = GetRayeBattleMateId(state);
            if (state.ActiveMateId != battleMateId)
                return;

            state.PendingDirectAttack = true;
        }

        public void OnPlayerDamaged(int defenderPlayer, int amount)
        {
            if (amount <= 0)
                return;

            var state0 = GetPremiumMateState(0);
            var state1 = GetPremiumMateState(1);
            TryResolveRayeDirectAttack(state0, defenderPlayer);
            TryResolveRayeDirectAttack(state1, defenderPlayer);
        }

        private void TryResolveRayeDirectAttack(PremiumMateState state, int defenderPlayer)
        {
            if (state == null)
                return;
            if (state.Rule.Behavior != PremiumMateBehavior.RayeBattlePhaseAndDirectAttack)
                return;
            if (!state.PendingDirectAttack)
                return;
            if (state.Side == defenderPlayer)
                return;

            state.PendingDirectAttack = false;
            state.InBattlePhaseSub = false;
            RequestPremiumMateForm(state, GetRayeEngageMateId(state));
        }

        private void ApplyPremiumNewTurn(PremiumMateState state, bool ownerTurn, int turn)
        {
            if (state == null)
                return;
            _ = ownerTurn;

            state.PendingDirectAttack = false;

            switch (state.Rule.Behavior)
            {
                case PremiumMateBehavior.LaundryBattlePhaseRoundTrip:
                    state.InBattlePhaseSub = false;
                    if (!state.IsPermanentSub)
                        RequestPremiumMateForm(state, state.Rule.BaseId);
                    break;
                case PremiumMateBehavior.RayeBattlePhaseAndDirectAttack:
                    state.InBattlePhaseSub = false;
                    break;
                case PremiumMateBehavior.IpSpTurnParity:
                    var target = (turn % 2 != 0) ? state.Rule.BaseId : state.Rule.SubId;
                    const float ipSpPreSwapDelaySeconds = 0.80f;
                    var turnSwapToken = ++state.PendingTurnSwapToken;
                    _ = RequestPremiumMateFormDelayedAsync(state, target, ipSpPreSwapDelaySeconds, turnSwapToken);
                    break;
            }
        }

        private async UniTask RequestPremiumMateFormDelayedAsync(PremiumMateState state, int targetMateId, float delaySeconds, int token)
        {
            if (state == null)
                return;
            if (delaySeconds > 0f)
                await UniTask.WaitForSeconds(delaySeconds);
            if (state.PendingTurnSwapToken != token)
                return;
            if (GetPremiumMateState(state.Side) != state)
                return;

            RequestPremiumMateForm(state, targetMateId);
        }

        private void ApplyPremiumPhase(PremiumMateState state, bool ownerTurn, DuelPhase phase)
        {
            if (state == null)
                return;

            switch (state.Rule.Behavior)
            {
                case PremiumMateBehavior.LaundryBattlePhaseRoundTrip:
                    if (ownerTurn && phase == DuelPhase.BattleStart)
                    {
                        state.InBattlePhaseSub = true;
                        RequestPremiumMateForm(state, state.Rule.SubId);
                    }
                    else if ((!ownerTurn || phase == DuelPhase.Main2 || phase == DuelPhase.End) && state.InBattlePhaseSub)
                    {
                        state.InBattlePhaseSub = false;
                        RequestPremiumMateForm(state, state.Rule.BaseId);
                    }
                    break;
                case PremiumMateBehavior.RayeBattlePhaseAndDirectAttack:
                    var battleMateId = GetRayeBattleMateId(state);
                    if (ownerTurn && phase == DuelPhase.BattleStart)
                    {
                        state.PendingDirectAttack = false;
                        state.InBattlePhaseSub = true;
                        if (state.ActiveMateId != battleMateId)
                            RequestPremiumMateForm(state, battleMateId);
                    }
                    else if ((phase == DuelPhase.Main2 || phase == DuelPhase.End) && state.InBattlePhaseSub)
                    {
                        state.PendingDirectAttack = false;
                        state.InBattlePhaseSub = false;
                    }
                    break;
            }
        }

        private void InitializeMateEntry(int side)
        {
            var state = GetPremiumMateState(side);
            if (state != null)
            {
                foreach (var pair in state.Forms)
                    if (pair.Value != null)
                        pair.Value.gameObject.SetActive(false);

                if (!state.Forms.TryGetValue(state.ActiveMateId, out var activeMate) || activeMate == null)
                    activeMate = state.Forms.Values.FirstOrDefault(m => m != null);

                if (activeMate == null)
                {
                    SetMateForSide(side, null);
                    return;
                }

                state.ActiveMateId = activeMate.code;
                ResetMateTransform(activeMate, state.Anchor);
                SetExclusivePremiumMateActiveForm(state, activeMate);
                activeMate.PrepareForPremiumSwapActivation();
                activeMate.Play(Mate.MateAction.Entry);
                SetMateForSide(side, activeMate);
                return;
            }

            var mate = GetMateForSide(side);
            if (mate != null)
            {
                mate.gameObject.SetActive(true);
                mate.Play(Mate.MateAction.Entry);
            }
        }

        private async UniTask LoadMateForSideAsync(int side, int mateCode, Transform anchor)
        {
            SetPremiumMateState(side, null);
            SetMateForSide(side, null);

            if (mateCode == Items.CODE_NONE)
                return;

            var normalizedMateCode = PremiumMateRules.GetBaseMateId(mateCode);
            if (PremiumMateRules.TryGetRuleByBaseId(normalizedMateCode, out var rule))
            {
                var formIds = new List<int> { rule.BaseId };
                foreach (var variantId in rule.VariantIds)
                    if (!formIds.Contains(variantId))
                        formIds.Add(variantId);

                var loadedForms = new Dictionary<int, Mate>();
                foreach (var formId in formIds)
                {
                    var formMate = await ABLoader.LoadMateAsync(formId);
                    if (formMate == null)
                        continue;

                    ConfigureLoadedMate(formMate, anchor);
                    loadedForms[formId] = formMate;
                }

                if (loadedForms.TryGetValue(rule.BaseId, out var baseMate)
                    && loadedForms.TryGetValue(rule.SubId, out _))
                {
                    var state = new PremiumMateState
                    {
                        Side = side,
                        Rule = rule,
                        SwapEffect = PremiumMateSwapEffects.GetOrDefault(rule.BaseId),
                        Anchor = anchor,
                        ActiveMateId = rule.BaseId
                    };
                    foreach (var pair in loadedForms)
                        state.Forms[pair.Key] = pair.Value;

                    if (rule.Behavior == PremiumMateBehavior.RayeBattlePhaseAndDirectAttack)
                    {
                        // The premium Raye mate's item IDs do not match the visual form order:
                        // 1003003 = Engage, 1003103 = Kagari, 1003203 = Raye.
                        // The opening Raye-only startup is disabled; start directly on Engage.
                        state.RayeEngageMateId = rule.BaseId;
                        state.RayeBattleMateId = loadedForms.ContainsKey(1003103) ? 1003103 : rule.SubId;
                        state.ActiveMateId = state.RayeEngageMateId;
                    }

                    SetPremiumMateState(side, state);
                    if (!loadedForms.TryGetValue(state.ActiveMateId, out var initialMate) || initialMate == null)
                        initialMate = baseMate;
                    SetMateForSide(side, initialMate);
                    return;
                }

                var fallbackMate = loadedForms.Values.FirstOrDefault();
                if (fallbackMate != null)
                {
                    SetMateForSide(side, fallbackMate);
                    return;
                }
            }

            var mate = await ABLoader.LoadMateAsync(normalizedMateCode);
            if (mate == null)
                return;

            ConfigureLoadedMate(mate, anchor);
            SetMateForSide(side, mate);
        }

        private static void ConfigureLoadedMate(Mate mate, Transform anchor)
        {
            if (mate == null)
                return;

            ResetMateTransform(mate, anchor);
            mate.gameObject.SetActive(false);
        }

        private static void ResetMateTransform(Mate mate, Transform anchor)
        {
            if (mate == null || anchor == null)
                return;

            mate.parent = anchor;
            mate.transform.SetParent(anchor, false);
            mate.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private static int GetRayeEngageMateId(PremiumMateState state)
        {
            if (state == null)
                return 0;
            if (state.RayeEngageMateId > 0)
                return state.RayeEngageMateId;
            return state.Rule.BaseId;
        }

        private static int GetRayeBattleMateId(PremiumMateState state)
        {
            if (state == null)
                return 0;
            return state.RayeBattleMateId > 0 ? state.RayeBattleMateId : state.Rule.SubId;
        }

        private static int ResolvePremiumMateTargetId(PremiumMateState state, int targetMateId)
        {
            if (state == null)
                return targetMateId;

            if (state.Rule.Behavior == PremiumMateBehavior.RayeBattlePhaseAndDirectAttack
                && targetMateId == GetRayeBattleMateId(state)
                && !state.InBattlePhaseSub)
            {
                return GetRayeEngageMateId(state);
            }

            return targetMateId;
        }

        private void ActivatePremiumMateFormImmediate(PremiumMateState state, int targetMateId)
        {
            if (state == null)
                return;
            if (!state.Forms.TryGetValue(targetMateId, out var targetMate) || targetMate == null)
                return;

            ResetMateTransform(targetMate, state.Anchor);
            SetExclusivePremiumMateActiveForm(state, targetMate);
            targetMate.PrepareForPremiumSwapActivation();
            state.ActiveMateId = targetMateId;
            SetMateForSide(state.Side, targetMate);
            RestartMateRandomCooldown(state.Side);
        }

        private static void SetExclusivePremiumMateActiveForm(PremiumMateState state, Mate activeMate, Mate additionalActiveMate = null)
        {
            if (state == null)
                return;
            foreach (var pair in state.Forms)
            {
                var mate = pair.Value;
                if (mate == null)
                    continue;
                mate.gameObject.SetActive(mate == activeMate || mate == additionalActiveMate);
            }
        }

        private static bool ShouldSuppressAmbientPremiumMateActions(PremiumMateState state)
        {
            return state != null && state.Rule.Behavior == PremiumMateBehavior.RayeBattlePhaseAndDirectAttack;
        }

        private static bool HasNonChangeTrigger(IReadOnlyList<string> triggerPriority)
        {
            if (triggerPriority == null || triggerPriority.Count == 0)
                return false;

            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (string.IsNullOrEmpty(trigger))
                    continue;
                if (!trigger.StartsWith("Change", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsChangeTrigger(string triggerName)
        {
            return !string.IsNullOrEmpty(triggerName) && triggerName.StartsWith("Change", StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasConfiguredChangeTrigger(IReadOnlyList<string> triggerPriority)
        {
            if (triggerPriority == null || triggerPriority.Count == 0)
                return false;

            for (var i = 0; i < triggerPriority.Count; i++)
            {
                if (IsChangeTrigger(triggerPriority[i]))
                    return true;
            }

            return false;
        }

        private static IReadOnlyList<string> BuildNonChangeFirstPriority(IReadOnlyList<string> triggerPriority)
        {
            if (triggerPriority == null || triggerPriority.Count <= 1)
                return triggerPriority;

            var hasLeadingChange = false;
            var hasFollowingNonChange = false;
            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (string.IsNullOrEmpty(trigger))
                    continue;
                if (IsChangeTrigger(trigger))
                {
                    hasLeadingChange = true;
                    continue;
                }

                if (hasLeadingChange)
                {
                    hasFollowingNonChange = true;
                    break;
                }
            }

            if (!hasFollowingNonChange)
                return triggerPriority;

            var reordered = new List<string>(triggerPriority.Count);
            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (string.IsNullOrEmpty(trigger) || IsChangeTrigger(trigger))
                    continue;
                reordered.Add(trigger);
            }

            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (string.IsNullOrEmpty(trigger) || !IsChangeTrigger(trigger))
                    continue;
                reordered.Add(trigger);
            }

            return reordered.Count > 0 ? reordered : triggerPriority;
        }

        private static bool MateHasAnyChangeTriggerParameter(Mate mate, IReadOnlyList<string> triggerPriority)
        {
            if (mate == null || triggerPriority == null || triggerPriority.Count == 0)
                return false;
            var checkedSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < triggerPriority.Count; i++)
            {
                var trigger = triggerPriority[i];
                if (!IsChangeTrigger(trigger))
                    continue;
                if (!checkedSet.Add(trigger))
                    continue;
                if (mate.HasMasterDuelTriggerParameter(trigger))
                    return true;
            }

            return false;
        }

        private static bool MateHasAnyTransitionCandidate(Mate mate, IReadOnlyList<string> triggerPriority)
        {
            if (mate == null || triggerPriority == null || triggerPriority.Count == 0)
                return false;

            return mate.TryDescribeMasterDuelTransitionCandidates(triggerPriority, out _);
        }

        private static UniTask WaitForSecondsUnscaledAsync(float seconds)
        {
            if (seconds <= 0f)
                return UniTask.CompletedTask;

            return UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.UnscaledDeltaTime);
        }

        private static UniTask WaitForSecondsSwapTimingAsync(float seconds, bool useUnscaledTiming)
        {
            if (seconds <= 0f)
                return UniTask.CompletedTask;

            return useUnscaledTiming
                ? WaitForSecondsUnscaledAsync(seconds)
                : UniTask.WaitForSeconds(seconds);
        }

        private static async UniTask<float> MeasureCurrentTransitionQueueDelayAsync(
            Mate mate,
            int beforeStateHash,
            bool beforeInTransition,
            float maxWaitSeconds)
        {
            if (mate == null || maxWaitSeconds <= 0f)
                return 0f;

            var startRealtime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startRealtime < maxWaitSeconds)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                if (!mate.TryGetMasterDuelPrimaryAnimatorSnapshot(
                        out var stateHash,
                        out _,
                        out _,
                        out var inTransition,
                        out var activeInHierarchy)
                    || !activeInHierarchy)
                {
                    return 0f;
                }

                if ((!beforeInTransition && inTransition) || stateHash != beforeStateHash)
                    return Time.realtimeSinceStartup - startRealtime;
            }

            return 0f;
        }

        private void RequestPremiumMateForm(PremiumMateState state, int targetMateId)
        {
            if (state == null)
                return;
            targetMateId = ResolvePremiumMateTargetId(state, targetMateId);
            if (!state.Forms.ContainsKey(targetMateId))
                return;

            if (state.IsTransitioning && state.QueuedMateId == targetMateId)
                return;
            if (!state.IsTransitioning && state.ActiveMateId == targetMateId)
                return;
            state.QueuedMateId = targetMateId;
            if (state.IsTransitioning)
                return;

            _ = ProcessPremiumMateSwapQueueAsync(state);
        }

        private async UniTask ProcessPremiumMateSwapQueueAsync(PremiumMateState state)
        {
            if (state == null)
                return;

            state.IsTransitioning = true;
            try
            {
                while (state.QueuedMateId != 0)
                {
                    if (state.QueuedMateId == state.ActiveMateId)
                    {
                        state.QueuedMateId = 0;
                        break;
                    }

                    var nextMateId = state.QueuedMateId;
                    state.QueuedMateId = 0;
                    await SwapPremiumMateAsync(state, nextMateId);
                }
            }
            finally
            {
                state.IsTransitioning = false;
            }
        }

        private async UniTask SwapPremiumMateAsync(PremiumMateState state, int targetMateId)
        {
            if (state == null)
                return;
            targetMateId = ResolvePremiumMateTargetId(state, targetMateId);
            if (!state.Forms.TryGetValue(targetMateId, out var nextMate) || nextMate == null)
                return;

            var currentMate = GetMateForSide(state.Side);
            if (currentMate == null && state.Forms.TryGetValue(state.ActiveMateId, out var fromState))
                currentMate = fromState;

            if (currentMate == nextMate)
            {
                SetExclusivePremiumMateActiveForm(state, nextMate);
                state.ActiveMateId = targetMateId;
                SetMateForSide(state.Side, nextMate);
                return;
            }

            var toSub = state.Rule.Behavior == PremiumMateBehavior.RayeBattlePhaseAndDirectAttack
                ? targetMateId == GetRayeBattleMateId(state)
                : targetMateId == state.Rule.SubId;
            var swapDelay = toSub ? state.SwapEffect.ToSubDelaySeconds : state.SwapEffect.ToBaseDelaySeconds;
            var effectAssetPath = state.SwapEffect.GetEffectAssetPath(toSub);
            var useUnscaledSwapTiming = state.SwapEffect.UseUnscaledSwapTiming;
            var changeOnTargetMate = state.SwapEffect.PlayChangeOnTargetMate;
            var changeOnBothMates = state.SwapEffect.PlayChangeOnBothMates;
            var playChangeOnCurrentMate = !changeOnTargetMate || changeOnBothMates;
            var playChangeOnNextMate = changeOnTargetMate || changeOnBothMates;
            var triggerPriorityOverride = state.OverrideTriggerPriority;
            var currentTriggerPriority = triggerPriorityOverride ?? (toSub
                ? state.SwapEffect.ToSubCurrentTriggerPriority
                : state.SwapEffect.ToBaseCurrentTriggerPriority);
            var nextTriggerPriority = triggerPriorityOverride ?? (toSub
                ? state.SwapEffect.ToSubNextTriggerPriority
                : state.SwapEffect.ToBaseNextTriggerPriority);
            IReadOnlyList<string> effectiveNextTriggerPriority = nextTriggerPriority;

            if (state.SwapEffect.UseChangeMotion && currentMate != null && playChangeOnCurrentMate
                && !MateHasAnyTransitionCandidate(currentMate, currentTriggerPriority))
                playChangeOnCurrentMate = false;
            state.OverrideTriggerPriority = null;
            var playedChangeOnCurrent = false;
            var playedChangeOnNext = false;
            var currentTransitionDelay = 0f;
            var nextTransitionDelay = 0f;
            var currentTriggerQueueDelay = 0f;
            var currentTriggerQueueProbeStarted = false;
            var currentTriggerQueueDelayTask = UniTask.FromResult(0f);
            var currentBeforeStateHash = 0;
            var currentBeforeInTransition = false;
            var hasCurrentSnapshot = false;
            if (state.SwapEffect.CompensateCurrentTriggerQueueDelay && currentMate != null)
            {
                hasCurrentSnapshot = currentMate.TryGetMasterDuelPrimaryAnimatorSnapshot(
                    out currentBeforeStateHash,
                    out _,
                    out _,
                    out currentBeforeInTransition,
                    out _);
            }

            void StartCurrentTriggerQueueProbeIfNeeded()
            {
                if (currentTriggerQueueProbeStarted || !playedChangeOnCurrent || !hasCurrentSnapshot)
                    return;
                if (state.SwapEffect.MaxCurrentTriggerQueueDelaySeconds <= 0f)
                    return;

                currentTriggerQueueProbeStarted = true;
                currentTriggerQueueDelayTask = MeasureCurrentTransitionQueueDelayAsync(
                    currentMate,
                    currentBeforeStateHash,
                    currentBeforeInTransition,
                    state.SwapEffect.MaxCurrentTriggerQueueDelaySeconds);
            }

            var effectPosition = currentMate != null ? currentMate.transform.position : state.Anchor.position;
            var effectLabel = toSub ? state.SwapEffect.ToSubEffectLabel : state.SwapEffect.ToBaseEffectLabel;
            var preferEffectLabelPlayback = state.SwapEffect.PreferEffectLabelPlayback && !string.IsNullOrEmpty(effectLabel);
            if (!preferEffectLabelPlayback && state.SwapEffect.UseChangeMotion && currentMate != null && playChangeOnCurrentMate)
            {
                playedChangeOnCurrent = currentMate.PlayChangeTransition(currentTriggerPriority, out currentTransitionDelay);
                StartCurrentTriggerQueueProbeIfNeeded();
            }

            if (state.SwapEffect.SourceToEffectDelaySeconds > 0f)
                await WaitForSecondsSwapTimingAsync(state.SwapEffect.SourceToEffectDelaySeconds, useUnscaledSwapTiming);

            var effectPlayback = await SpawnPremiumSwapEffectAsync(state, effectAssetPath, effectPosition, effectLabel, toSub);
            if (preferEffectLabelPlayback && state.SwapEffect.UseChangeMotion && currentMate != null && playChangeOnCurrentMate)
            {
                if (!effectPlayback.Consumed)
                {
                    playedChangeOnCurrent = currentMate.PlayChangeTransition(currentTriggerPriority, out currentTransitionDelay);
                    StartCurrentTriggerQueueProbeIfNeeded();
                }
            }

            var delay = swapDelay;
            if (currentTriggerQueueProbeStarted)
            {
                currentTriggerQueueDelay = await currentTriggerQueueDelayTask;
                if (currentTriggerQueueDelay > 0f)
                    delay += currentTriggerQueueDelay;
            }

            if (playedChangeOnCurrent)
            {
                if (state.SwapEffect.UseTransitionDelayAsMinimum)
                    delay = Mathf.Max(delay, currentTransitionDelay);
                else if (delay <= 0f)
                    delay = currentTransitionDelay;
            }

            var keepCurrentAliveForOverlap = currentMate != null
                && playedChangeOnCurrent
                && changeOnBothMates
                && currentTransitionDelay > delay;
            if (delay > 0f)
                await WaitForSecondsSwapTimingAsync(delay, useUnscaledSwapTiming);

            targetMateId = ResolvePremiumMateTargetId(state, targetMateId);
            if (!state.Forms.TryGetValue(targetMateId, out nextMate) || nextMate == null)
                return;

            ResetMateTransform(nextMate, state.Anchor);
            if (currentMate != null && !keepCurrentAliveForOverlap)
                currentMate.gameObject.SetActive(false);
            SetExclusivePremiumMateActiveForm(state, nextMate, keepCurrentAliveForOverlap ? currentMate : null);
            nextMate.PrepareForPremiumSwapActivation();
            state.ActiveMateId = targetMateId;
            SetMateForSide(state.Side, nextMate);

            if (state.SwapEffect.PreferNonChangeNextWhenChangeTriggerMissing
                && triggerPriorityOverride == null
                && HasConfiguredChangeTrigger(nextTriggerPriority)
                && !MateHasAnyChangeTriggerParameter(nextMate, nextTriggerPriority))
            {
                var reorderedPriority = BuildNonChangeFirstPriority(nextTriggerPriority);
                if (!ReferenceEquals(reorderedPriority, nextTriggerPriority))
                    effectiveNextTriggerPriority = reorderedPriority;
            }

            if (state.SwapEffect.NextMotionLeadInSeconds > 0f)
                await WaitForSecondsUnscaledAsync(state.SwapEffect.NextMotionLeadInSeconds);

            var hasNextActivationTrigger = HasNonChangeTrigger(effectiveNextTriggerPriority);
            var shouldPlayNextMotion = state.SwapEffect.UseChangeMotion
                && (playChangeOnNextMate || !playedChangeOnCurrent || hasNextActivationTrigger);
            if (shouldPlayNextMotion)
            {
                playedChangeOnNext = nextMate.PlayChangeTransition(effectiveNextTriggerPriority, out nextTransitionDelay);
                var nextStageWait = nextTransitionDelay;
                if (state.SwapEffect.NextMotionMinDurationSeconds > 0f)
                    nextStageWait = Mathf.Max(nextStageWait, state.SwapEffect.NextMotionMinDurationSeconds);
                if (nextStageWait > 0f)
                    await WaitForSecondsUnscaledAsync(nextStageWait);
                if (state.SwapEffect.NextMotionDelaySeconds > 0f)
                    await WaitForSecondsUnscaledAsync(state.SwapEffect.NextMotionDelaySeconds);
            }

            if (keepCurrentAliveForOverlap && currentMate != null)
                currentMate.gameObject.SetActive(false);

            RestartMateRandomCooldown(state.Side);
        }

        private async UniTask<SwapEffectLabelPlaybackResult> SpawnPremiumSwapEffectAsync(PremiumMateState state, string effectPath, Vector3 position, string effectLabel = null, bool toSub = true)
        {
            if (state == null || state.SwapEffect == null)
                return SwapEffectLabelPlaybackResult.None;
            if (!state.SwapEffect.HasChangeEffectAsset(toSub))
                return SwapEffectLabelPlaybackResult.None;

            var fullPath = Path.Combine(Program.root, effectPath);
            GameObject effect = null;
            var selectedScore = int.MinValue;
            void ConsiderEffectCandidate(GameObject candidate)
            {
                if (candidate == null)
                    return;

                var score = GetSwapEffectCandidateScore(candidate);
                if (effect == null || score > selectedScore)
                {
                    if (effect != null && effect != candidate)
                        UnityEngine.Object.Destroy(effect);

                    effect = candidate;
                    selectedScore = score;
                }
                else
                {
                    UnityEngine.Object.Destroy(candidate);
                }
            }

            if (Directory.Exists(fullPath))
            {
                var leafName = Path.GetFileName(effectPath);
                var directBundlePath = Path.Combine(fullPath, leafName);
                if (!string.IsNullOrEmpty(leafName) && File.Exists(directBundlePath))
                {
                    var loadPath = effectPath + "/" + leafName;
                    ConsiderEffectCandidate(await ABLoader.LoadFromFileAsync(loadPath, false, true));
                }

                ConsiderEffectCandidate(await ABLoader.LoadFromFolderAsync<PlayableDirector>(effectPath, false, true));
                ConsiderEffectCandidate(await ABLoader.LoadFromFolderAsync<ParticleSystem>(effectPath, false, true));
            }
            else
            {
                ConsiderEffectCandidate(await ABLoader.LoadFromFileAsync(effectPath, false, true));
            }

            if (effect == null)
                return SwapEffectLabelPlaybackResult.None;

            effect.transform.SetParent(Program.instance.container_3D, false);
            effect.transform.position = position;
            var labelResult = await PlayPremiumSwapEffectLabelAsync(effect, effectLabel);
            UnityEngine.Object.Destroy(effect, 5f);
            return labelResult;
        }

        private async UniTask<SwapEffectLabelPlaybackResult> PlayPremiumSwapEffectLabelAsync(GameObject effect, string effectLabel)
        {
            if (effect == null)
                return SwapEffectLabelPlaybackResult.None;

            var labelPlayed = false;
            var fallbackPlayed = false;

            var animators = effect.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null)
                    continue;

                if (TryPlayAnimatorEffectLabel(animator, effectLabel))
                    labelPlayed = true;
            }

            var directors = effect.GetComponentsInChildren<PlayableDirector>(true);
            for (var i = 0; i < directors.Length; i++)
            {
                var director = directors[i];
                if (director == null)
                    continue;

                if (TryPlayDirectorEffectLabel(director, effectLabel, out var startTime))
                {
                    director.time = startTime;
                    director.Play();
                    labelPlayed = true;
                }
            }

            var changeEffects = effect.GetComponentsInChildren<BgAvatarChangeEffect>(true);
            if (!labelPlayed && !string.IsNullOrEmpty(effectLabel))
            {
                for (var i = 0; i < changeEffects.Length; i++)
                {
                    if (!TryPlayBgAvatarChangeEffectLabel(changeEffects[i], effectLabel))
                        continue;

                    labelPlayed = true;
                    break;
                }
            }

            if (!labelPlayed && !string.IsNullOrEmpty(effectLabel))
            {
                if (TryPlayElementObjectLabel(effect, effectLabel))
                    labelPlayed = true;
                else if (TryPlayAnyComponentLabel(effect, effectLabel))
                    labelPlayed = true;
            }

            var labeledControllers = effect.GetComponentsInChildren<LabeledPlayableController>(true);
            if (!labelPlayed && !string.IsNullOrEmpty(effectLabel) && labeledControllers.Length > 0)
            {
                for (var i = 0; i < directors.Length; i++)
                {
                    var director = directors[i];
                    if (director == null || director.state == PlayState.Playing)
                        continue;
                    director.Play();
                }

                const int maxFramesToWait = 3;
                for (var frame = 0; frame <= maxFramesToWait && !labelPlayed; frame++)
                {
                    for (var i = 0; i < labeledControllers.Length; i++)
                    {
                        var controller = labeledControllers[i];
                        if (controller == null || controller.loopMixerBehaviour == null)
                            continue;

                        controller.PlayLabel(effectLabel, (TimelineClip)null);
                        labelPlayed = true;
                        break;
                    }

                    if (!labelPlayed && frame < maxFramesToWait)
                        await UniTask.Yield();
                }
            }

            if (!labelPlayed && !string.IsNullOrEmpty(effectLabel) && directors.Length == 1)
            {
                var director = directors[0];
                if (director != null)
                {
                    director.time = 0d;
                    director.Play();
                    fallbackPlayed = true;
                }
            }

            return new SwapEffectLabelPlaybackResult(labelPlayed || fallbackPlayed);
        }

        private static bool TryPlayBgAvatarChangeEffectLabel(BgAvatarChangeEffect changeEffect, string effectLabel)
        {
            if (changeEffect == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var toMain = LabelEquals(changeEffect.toMainLabel, effectLabel);
            var toSub = LabelEquals(changeEffect.toSubLabel, effectLabel);
            if (!toMain && !toSub)
                return false;

            var fieldName = toMain ? "toMainObj" : "toSubObj";
            var field = typeof(BgAvatarChangeEffect).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var particle = field != null ? field.GetValue(changeEffect) as ParticleSystem : null;
            if (particle != null)
            {
                PlayTargetObject(particle.gameObject);
                return true;
            }

            if (TryPlayElementObjectLabel(changeEffect.gameObject, effectLabel))
                return true;

            return TryPlayAnyComponentLabel(changeEffect.gameObject, effectLabel);
        }

        private static bool TryPlayElementObjectLabel(GameObject root, string effectLabel)
        {
            if (root == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var elements = root.GetComponentsInChildren<ElementObject>(true);
            var played = false;
            for (var i = 0; i < elements.Length; i++)
            {
                var element = elements[i];
                if (element == null || !LabelEquals(element.label, effectLabel))
                    continue;

                PlayTargetObject(element.gameObject);
                played = true;
            }

            return played;
        }

        private static bool TryPlayAnyComponentLabel(GameObject root, string effectLabel)
        {
            if (root == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            var played = false;
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                var field = behaviour.GetType().GetField("label", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field == null || field.FieldType != typeof(string))
                    continue;

                var value = field.GetValue(behaviour) as string;
                if (!LabelEquals(value, effectLabel))
                    continue;

                PlayTargetObject(behaviour.gameObject);
                played = true;
            }

            return played;
        }

        private static void PlayTargetObject(GameObject target)
        {
            if (target == null)
                return;

            target.SetActive(true);

            var particles = target.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                if (particle == null)
                    continue;

                particle.gameObject.SetActive(true);
                particle.Clear(true);
                particle.Play(true);
            }

            var animators = target.GetComponentsInChildren<Animator>(true);
            for (var i = 0; i < animators.Length; i++)
            {
                var animator = animators[i];
                if (animator == null)
                    continue;
                animator.gameObject.SetActive(true);
                animator.Rebind();
                animator.Update(0f);
                animator.Play(0, 0, 0f);
            }

            var directors = target.GetComponentsInChildren<PlayableDirector>(true);
            for (var i = 0; i < directors.Length; i++)
            {
                var director = directors[i];
                if (director == null)
                    continue;
                director.gameObject.SetActive(true);
                director.time = 0d;
                director.Play();
            }
        }

        private static bool LabelEquals(string a, string b)
        {
            return !string.IsNullOrEmpty(a)
                && !string.IsNullOrEmpty(b)
                && a.Equals(b, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryPlayDirectorEffectLabel(PlayableDirector director, string effectLabel, out double startTime)
        {
            startTime = 0d;
            if (director == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var playableName = director.playableAsset != null ? director.playableAsset.name : string.Empty;
            var nameMatch =
                (!string.IsNullOrEmpty(playableName) && playableName.IndexOf(effectLabel, StringComparison.OrdinalIgnoreCase) >= 0)
                || director.name.IndexOf(effectLabel, StringComparison.OrdinalIgnoreCase) >= 0
                || director.gameObject.name.IndexOf(effectLabel, StringComparison.OrdinalIgnoreCase) >= 0;
            if (nameMatch)
                return true;

            return TryGetDirectorLabelStartTime(director, effectLabel, out startTime);
        }

        private static bool TryGetDirectorLabelStartTime(PlayableDirector director, string effectLabel, out double startTime)
        {
            startTime = 0d;
            if (director == null || string.IsNullOrEmpty(effectLabel) || director.playableAsset == null)
                return false;

            var outputs = director.playableAsset.outputs;
            foreach (var output in outputs)
            {
                var track = output.sourceObject as TrackAsset;
                if (track == null)
                    continue;

                foreach (var clip in track.GetClips())
                {
                    if (clip == null)
                        continue;

                    var displayName = clip.displayName;
                    if (!string.IsNullOrEmpty(displayName)
                        && displayName.Equals(effectLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        startTime = clip.start;
                        return true;
                    }
                }
            }

            return false;
        }

        private static int GetSwapEffectCandidateScore(GameObject candidate)
        {
            if (candidate == null)
                return int.MinValue;

            var labeledControllers = candidate.GetComponentsInChildren<LabeledPlayableController>(true).Length;
            var directors = candidate.GetComponentsInChildren<PlayableDirector>(true).Length;
            var animators = candidate.GetComponentsInChildren<Animator>(true).Length;
            var particles = candidate.GetComponentsInChildren<ParticleSystem>(true).Length;
            var changeEffects = candidate.GetComponentsInChildren<BgAvatarChangeEffect>(true).Length;
            var elementLabels = candidate.GetComponentsInChildren<ElementObject>(true).Length;
            return labeledControllers * 100 + changeEffects * 60 + directors * 20 + animators * 8 + elementLabels * 4 + particles * 2;
        }

        private static bool TryPlayAnimatorEffectLabel(Animator animator, string effectLabel)
        {
            if (animator == null || string.IsNullOrEmpty(effectLabel))
                return false;

            var played = false;
            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.type != AnimatorControllerParameterType.Trigger)
                    continue;
                if (!parameter.name.Equals(effectLabel, StringComparison.OrdinalIgnoreCase))
                    continue;

                animator.SetTrigger(parameter.name);
                played = true;
            }
            if (played)
                return true;

            for (var layer = 0; layer < animator.layerCount; layer++)
            {
                var layerName = animator.GetLayerName(layer);
                var shortHash = Animator.StringToHash(effectLabel);
                var fullHash = Animator.StringToHash($"{layerName}.{effectLabel}");
                if (animator.HasState(layer, fullHash))
                {
                    animator.Play(fullHash, layer, 0f);
                    played = true;
                    continue;
                }
                if (animator.HasState(layer, shortHash))
                {
                    animator.Play(shortHash, layer, 0f);
                    played = true;
                }
            }
            if (played)
                return true;

            var controller = animator.runtimeAnimatorController;
            if (controller == null)
                return false;

            var clips = controller.animationClips;
            for (var i = 0; i < clips.Length; i++)
            {
                var clip = clips[i];
                if (clip == null)
                    continue;
                if (!clip.name.Equals(effectLabel, StringComparison.OrdinalIgnoreCase))
                    continue;

                animator.Play(clip.name, 0, 0f);
                return true;
            }

            return false;
        }

        private Mate GetMateForSide(int side)
        {
            return side == 0 ? mate0 : mate1;
        }

        private void SetMateForSide(int side, Mate mate)
        {
            if (side == 0)
                mate0 = mate;
            else
                mate1 = mate;
        }

        private PremiumMateState GetPremiumMateState(int side)
        {
            return side == 0 ? premiumMate0 : premiumMate1;
        }

        private void SetPremiumMateState(int side, PremiumMateState state)
        {
            if (side == 0)
                premiumMate0 = state;
            else
                premiumMate1 = state;
        }

        private void ResetPremiumMateStates()
        {
            premiumMate0 = null;
            premiumMate1 = null;
            mate0RandomCooldownTween?.Kill();
            mate0RandomCooldownTween = null;
            mate1RandomCooldownTween?.Kill();
            mate1RandomCooldownTween = null;
        }

        public void RefreshBgState()
        {
            ResizeDecks();
            RefreshGravesState();
        }

        public void ResizeDecks()
        {
            if (myDeck == null || myExtra == null
                || opDeck == null || opExtra == null)
                return;

            ResizeDeckModel(myDeck, Core.GetLocationCardCount(CardLocation.Deck, 0));
            ResizeDeckModel(myExtra, Core.GetLocationCardCount(CardLocation.Extra, 0));
            ResizeDeckModel(opDeck, Core.GetLocationCardCount(CardLocation.Deck, 1));
            ResizeDeckModel(opExtra, Core.GetLocationCardCount(CardLocation.Extra, 1));
        }

        public void RefreshGravesState()
        {
            if (grave0Manager == null
                || grave1Manager == null)
                return;

            RefreshGraveState(grave0Manager, Core.GetLocationCardCount(CardLocation.Grave, 0));
            RefreshExcludeState(grave0Manager, Core.GetLocationCardCount(CardLocation.Removed, 0));
            RefreshGraveState(grave1Manager, Core.GetLocationCardCount(CardLocation.Grave, 1));
            RefreshExcludeState(grave1Manager, Core.GetLocationCardCount(CardLocation.Removed, 1));
        }

        private void ResizeDeckModel(ElementObjectManager deck, int count)
        {
            var deckSetOffset = deck.GetElement<Transform>("DeckSetOffset");
            if (count == 0)
                deckSetOffset.transform.localScale = Vector3.zero;
            else
                deckSetOffset.transform.localScale = new Vector3(0.9f, count / 40f, 0.9f);

            var cardShuffle = deck.GetElement<Transform>("CardShuffleTop");
            if (count == 0)
                cardShuffle.transform.localScale = Vector3.zero;
            else
                cardShuffle.transform.localScale = new Vector3(0.9f, count / 40f, 0.9f);
        }

        private void RefreshGraveState(ElementObjectManager grave, int count)
        {
            var stateMap = new Dictionary<Func<int, bool>, string>
            {
                { c => c >= 20, "GraveIdleS3" },
                { c => c >= 10, "GraveIdleS2" },
                { c => c > 0,  "GraveIdleS1" }
            };

            var particleSystems = new[] { "GraveIdleS1", "GraveIdleS2", "GraveIdleS3" }
                .ToDictionary(name => name, grave.GetElement<ParticleSystem>);

            foreach (var ps in particleSystems.Values)
                ps.Stop();

            var activeSystem = stateMap.FirstOrDefault(kv => kv.Key(count)).Value;
            if (activeSystem != null && particleSystems.TryGetValue(activeSystem, out var systemToPlay))
                systemToPlay.Play();

            grave.GetElement<Renderer>("Material01").material.SetFloat(
                "_GraveCardExist",
                count > 0 ? 1 : 0
            );
        }

        private void RefreshExcludeState(ElementObjectManager exclude, int count)
        {
            var stateMap = new Dictionary<Func<int, bool>, string>
            {
                { c => c >= 20, "ExcludeIdleS3" },
                { c => c >= 10, "ExcludeIdleS2" },
                { c => c > 0,  "ExcludeIdleS1" }
            };

            var particleSystems = new[] { "ExcludeIdleS1", "ExcludeIdleS2", "ExcludeIdleS3" }
                .ToDictionary(name => name, exclude.GetElement<ParticleSystem>);

            foreach (var ps in particleSystems.Values)
                ps.Stop();

            var activeSystem = stateMap.FirstOrDefault(kv => kv.Key(count)).Value;
            if (activeSystem != null && particleSystems.TryGetValue(activeSystem, out var systemToPlay))
                systemToPlay.Play();

            exclude.GetElement<Renderer>("Material01").material.SetFloat(
                "_ExcludeCardExist",
                count > 0 ? 1 : 0
            );
        }

        public void UpdateBgEffects(int player, bool first = false)
        {
            if (myDeck == null || myExtra == null
                || opDeck == null || opExtra == null
                || grave0Manager == null
                || grave1Manager == null)
                return;

            if (player == 0)
            {
                field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.PhaseToDamagePhaseAll);
                //field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhaseToNextPhaseAll);
                if (!first && CanPlayMateAction(0, mate0))
                    mate0.Play(Mate.MateAction.GetDamage);
                if (bgPhase0 == 1 && life0 < (lpLimit * 0.75f))
                {
                    bgPhase0++;
                    var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE1_P";
                    field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase1ToPhase2, seLabel);
                    field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.OtherSideDamagePhase1ToPhase2);
                    grave0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase1ToPhase2);
                    if (stand0Manager != null)
                        stand0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase1ToPhase2);
                }
                if (bgPhase0 == 2 && life0 < (lpLimit * 0.5f))
                {
                    bgPhase0++;
                    var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE2_P";
                    field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase2ToPhase3, seLabel);
                    field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.OtherSideDamagePhase2ToPhase3);
                    grave0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase2ToPhase3);
                    if (stand0Manager != null)
                        stand0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase2ToPhase3);
                }
                if (bgPhase0 == 3 && life0 < (lpLimit * 0.25f))
                {
                    bgPhase0++;
                    var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE3_P";
                    field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase3ToPhase4, seLabel);
                    field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.OtherSideDamagePhase3ToPhase4);
                    grave0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase3ToPhase4);
                    if (stand0Manager != null)
                        stand0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase3ToPhase4);
                    AudioManager.PlayBgmClimax();
                }
                if (bgPhase0 == 4 && life0 <= 0)
                {
                    //bgPhase0++;
                    //var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE4_P";
                    //field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd, seLabel);
                    //field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndLose);
                    //field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndWin);
                    //grave0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                    //if (stand0Manager != null)
                    //    stand0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                }
            }
            else
            {
                field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.PhaseToDamagePhaseAll);
                //field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhaseToNextPhaseAll);
                if (!first && CanPlayMateAction(1, mate1))
                    mate1.Play(Mate.MateAction.GetDamage);
                if (bgPhase1 == 1 && life1 < (lpLimit * 0.75f))
                {
                    bgPhase1++;
                    var seLabel = "SE_FIELD_MAT" + field1Manager.name.Substring(4, 3) + "_PHASE1_P";
                    field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase1ToPhase2, seLabel);
                    field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.OtherSideDamagePhase1ToPhase2);
                    grave1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase1ToPhase2);
                    if (stand1Manager != null)
                        stand1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase1ToPhase2);
                }
                if (bgPhase1 == 2 && life1 < (lpLimit * 0.5f))
                {
                    bgPhase1++;
                    var seLabel = "SE_FIELD_MAT" + field1Manager.name.Substring(4, 3) + "_PHASE2_P";
                    field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase2ToPhase3, seLabel);
                    field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.OtherSideDamagePhase2ToPhase3);
                    grave1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase2ToPhase3);
                    if (stand1Manager != null)
                        stand1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase2ToPhase3);
                }
                if (bgPhase1 == 3 && life1 < (lpLimit * 0.25f))
                {
                    bgPhase1++;
                    var seLabel = "SE_FIELD_MAT" + field1Manager.name.Substring(4, 3) + "_PHASE3_P";
                    field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase3ToPhase4, seLabel);
                    field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.OtherSideDamagePhase3ToPhase4);
                    grave1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase3ToPhase4);
                    if (stand1Manager != null)
                        stand1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase3ToPhase4);
                    AudioManager.PlayBgmClimax();
                }
                if (bgPhase1 == 4 && life1 <= 0)
                {
                    //bgPhase1++;
                    //var seLabel = "SE_FIELD_MAT" + field1Manager.name.Substring(4, 3) + "_PHASE4_P";
                    //field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd, seLabel);
                    //field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndWin);
                    //field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndLose);
                    //grave1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                    //if (stand1Manager != null)
                    //    stand1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                }
            }
        }

        public void PlayGraveEffect(GPS p, bool isIn)
        {
            if(grave0Manager == null || grave1Manager == null)
                return;

            ElementObjectManager manager;
            if (p.InMyControl())
                manager = grave0Manager;
            else
                manager = grave1Manager;
            if (manager == null)
                return;

            BgEffectSetting effect;
            BgEffectSetting effectEnd;
            string audio = string.Empty;
            if ((p.location & (uint)CardLocation.Grave) > 0)
            {
                if (isIn)
                {
                    effect = manager.GetElement<BgEffectSetting>("GraveIn");
                    effectEnd = manager.GetElement<BgEffectSetting>("GraveInend");
                    audio = "SE_CEMETARY_ABSORB";
                }
                else
                {
                    effect = manager.GetElement<BgEffectSetting>("GraveOut");
                    effectEnd = manager.GetElement<BgEffectSetting>("GraveOutend");
                    audio = "SE_CEMETARY_GOOUT";
                }
            }
            else
            {
                if (isIn)
                {
                    effect = manager.GetElement<BgEffectSetting>("ExcludeIn");
                    effectEnd = manager.GetElement<BgEffectSetting>("ExcludeInend");
                    audio = "SE_EXCLUSION_ABSORB";
                }
                else
                {
                    effect = manager.GetElement<BgEffectSetting>("ExcludeOut");
                    effectEnd = manager.GetElement<BgEffectSetting>("ExcludeOutend");
                    audio = "SE_EXCLUSION_GOOUT";
                }
            }
            DOTween.To(v => { }, 0, 0, effect.delay).OnComplete(() =>
            {
                effect.particle.Play();
                AudioManager.PlaySE(audio);
            });
            DOTween.To(v => { }, 0, 0, effectEnd.delay).OnComplete(() =>
            {
                effectEnd.particle.Play();
            });
        }

        public void ShowBgHint()
        {
            if (myDeck == null || myExtra == null
                || opDeck == null || opExtra == null
                || grave0Manager == null
                || grave1Manager == null)
                return;

            bool haveHint = false;
            var cardsActivated = cards.Where(c => c.buttons.Count > 0);

            foreach (var card in cardsActivated)
                if (card.p.InLocation(CardLocation.Grave))
                    if (card.p.InMyControl())
                    {
                        var effect = ABLoader.LoadMasterDuelGameObject("fxp_HL_active_grave_001");
                        effect.transform.SetParent(grave0Manager.GetElement<Transform>("GraveHighlightNear"), false);
                        UnityEngine.Object.Destroy(effect, 3f);
                        grave0Manager.GetElement<Animator>("GraveHighlightNear").SetBool("On", true);
                        haveHint = true;
                        break;
                    }
            foreach (var card in cardsActivated)
                if (card.p.InLocation(CardLocation.Grave))
                    if (!card.p.InMyControl())
                    {
                        var effect = ABLoader.LoadMasterDuelGameObject("fxp_HL_active_grave_001");
                        effect.transform.SetParent(grave1Manager.GetElement<Transform>("GraveHighlightFar"), false);
                        UnityEngine.Object.Destroy(effect, 3f);
                        grave1Manager.GetElement<Animator>("GraveHighlightFar").SetBool("On", true);
                        haveHint = true;
                        break;
                    }
            foreach (var card in cardsActivated)
                if (card.p.InLocation(CardLocation.Removed))
                    if (card.p.InMyControl())
                    {
                        var effect = ABLoader.LoadMasterDuelGameObject("fxp_HL_active_exclude_001");
                        effect.transform.SetParent(grave0Manager.GetElement<Transform>("ExcludeHighlightNear"), false);
                        UnityEngine.Object.Destroy(effect, 3f);
                        grave0Manager.GetElement<Animator>("ExcludeHighlightNear").SetBool("On", true);
                        haveHint = true;
                        break;
                    }
            foreach (var card in cardsActivated)
                if (card.p.InLocation(CardLocation.Removed))
                    if (!card.p.InMyControl())
                    {
                        var effect = ABLoader.LoadMasterDuelGameObject("fxp_HL_active_exclude_001");
                        effect.transform.SetParent(grave1Manager.GetElement<Transform>("ExcludeHighlightFar"), false);
                        UnityEngine.Object.Destroy(effect, 3f);
                        grave1Manager.GetElement<Animator>("ExcludeHighlightFar").SetBool("On", true);
                        haveHint = true;
                        break;
                    }
            foreach (var card in cardsActivated)
                if ((card.p.location & (uint)CardLocation.Extra) > 0)
                    if (card.p.InMyControl())
                    {
                        var effect = ABLoader.LoadMasterDuelGameObject("fxp_HL_active_Exdeck_001");
                        effect.transform.SetParent(myExtra.transform, false);
                        effect.transform.position = Tools.GetDeckModelTopPosition(myExtra);
                        foreach (var place in places)
                            place.ShowHint((uint)CardLocation.Extra, 0u);
                        UnityEngine.Object.Destroy(effect, 3f);
                        haveHint = true;
                        break;
                    }
            foreach (var card in cardsActivated)
                if ((card.p.location & (uint)CardLocation.Extra) > 0)
                    if (!card.p.InMyControl())
                    {
                        var effect = ABLoader.LoadMasterDuelGameObject("fxp_HL_active_Exdeck_001");
                        effect.transform.SetParent(opExtra.transform, false);
                        effect.transform.position = Tools.GetDeckModelTopPosition(opExtra);
                        foreach (var place in places)
                            place.ShowHint((uint)CardLocation.Extra, 1u);
                        UnityEngine.Object.Destroy(effect, 3f);
                        haveHint = true;
                        break;
                    }
            foreach (var card in cardsActivated)
                if ((card.p.location & (uint)CardLocation.Deck) > 0)
                    if (card.p.controller == 0)
                    {
                        var effect = ABLoader.LoadMasterDuelGameObject("fxp_HL_active_Exdeck_001");
                        effect.transform.SetParent(myDeck.transform, false);
                        effect.transform.position = Tools.GetDeckModelTopPosition(myDeck);
                        foreach (var place in places)
                            place.ShowHint((uint)CardLocation.Deck, 0u);
                        UnityEngine.Object.Destroy(effect, 3f);
                        haveHint = true;
                        break;
                    }
            foreach (var card in cardsActivated)
                if ((card.p.location & (uint)CardLocation.Deck) > 0)
                    if (!card.p.InMyControl())
                    {
                        var effect = ABLoader.LoadMasterDuelGameObject("fxp_HL_active_Exdeck_001");
                        effect.transform.SetParent(opDeck.transform, false);
                        effect.transform.position = Tools.GetDeckModelTopPosition(opDeck);
                        foreach (var place in places)
                            place.ShowHint((uint)CardLocation.Deck, 1u);
                        UnityEngine.Object.Destroy(effect, 3f);
                        haveHint = true;
                        break;
                    }
            if (haveHint)
                AudioManager.PlaySE("SE_DUEL_ACTIVE_POSSIBLE");
        }

        public void ClearResponse()
        {

            var myMaxDeck = Core.GetLocationCardCount(CardLocation.Deck, 0);
            var opMaxDeck = Core.GetLocationCardCount(CardLocation.Deck, 1);
            foreach (var card in cards)
            {
                card.effects.Clear();
                card.ClearButtons();
                if (card.forSelect)
                {
                    card.forSelect = false;
                    if (card.p.InLocation(CardLocation.Deck))
                    {
                        if (deckReserved)
                        {
                            if (card.p.controller == 0 && card.p.sequence != myMaxDeck - 1)
                                card.EraseData();
                            if (card.p.controller == 1 && card.p.sequence != opMaxDeck - 1)
                                card.EraseData();
                        }
                        else
                        {
                            card.EraseData();
                        }
                    }
                }
            }

            foreach (var place in places)
            {
                place.StopResponse();
                place.HideHint();
                place.ClearButtons();
            }
            foreach (var grave in graves)
            {
                grave.ClearGraveButtons();
                grave.ClearExcludeButtons();
            }

            PhaseButtonHandler.battlePhase = false;
            PhaseButtonHandler.main2Phase = false;
            PhaseButtonHandler.endPhase = false;
            PhaseButtonHandler.CloseHint();

            CloseBgHint();
            FieldSelectReset();
            ES_selectHint = string.Empty;

        }

        private void CloseBgHint()
        {
            if (grave0Manager == null || grave1Manager == null)
                return;

            grave0Manager.GetElement<Animator>("GraveHighlightNear").SetBool("On", false);
            grave0Manager.GetElement<Animator>("ExcludeHighlightNear").SetBool("On", false);
            grave1Manager.GetElement<Animator>("GraveHighlightFar").SetBool("On", false);
            grave1Manager.GetElement<Animator>("ExcludeHighlightFar").SetBool("On", false);
        }

        private void FieldSelectReset()
        {
            foreach (var place in places)
                place.StopResponse();
            btnConfirm.Hide();
            btnCancel.Hide();
            Core.GetUI<OcgCoreUI>().CloseHint();
        }

        public void Dispose()
        {
            foreach(var go in turnEndDeleteObjects)
                UnityEngine.Object.Destroy(go);
            turnEndDeleteObjects.Clear();
            foreach (var go in allGameObjects)
                UnityEngine.Object.Destroy(go);
            allGameObjects.Clear();
            bgPhase0 = 0;
            bgPhase1 = 0;
            ResetPremiumMateStates();
            mate0 = null;
            mate1 = null;
        }

        public async UniTask ShowDuelResultText(string text)
        {
            var go = ABLoader.LoadMasterDuelGameObject(text);
            allGameObjects.Add(go);
            var director = go.GetComponent<PlayableDirector>();
            await director.WaitAsync();
            UnityEngine.Object.Destroy(go);
        }

        public void DuelEndEvent()
        {
            if(timerHandler != null)
                timerHandler.DuelEnd();
            if(playableGuide != null)
                playableGuide.End();
        }

        public async UniTask PlayCommonSpecialWin(int[] codes)
        {
            var count = codes.Length;
            var go = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/SpecialWin/SpecialWinCommonCard0" + count, false, true);
            allGameObjects.Add(go);
            var mner = go.GetComponent<ElementObjectManager>();
            foreach (var child in mner.transform.GetComponentsInChildren<Transform>(true))
                if (child.name == "White")
                    child.gameObject.SetActive(false);

            _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard01"), codes[0], 0, true);
            mner.GetElement<ElementObjectManager>("DummyCard01").GetElement<Renderer>("DummyCardModel_front").material.renderQueue = 4000;
            if (count > 1)
                _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard02"), codes[1], 0, true);
            if (count > 2)
                _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard03"), codes[2], 0, true);
            if (count > 3)
                _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard04"), codes[3], 0, true);
            if (count > 4)
                _ = Program.instance.texture_.LoadDummyCard(mner.GetElement<ElementObjectManager>("DummyCard05"), codes[4], 0, true);

            var director = mner.GetComponent<PlayableDirector>();
            director.Play();
            await director.WaitAsync();
            UnityEngine.Object.Destroy(go);
        }

        public async UniTask PlaySpecialWin(string path, Action<ElementObjectManager> action = null)
        {
            var go = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/SpecialWin/" + path, false, true);
            allGameObjects.Add(go);
            var manager = go.GetComponent<ElementObjectManager>();
            action?.Invoke(manager);
            var director = go.GetComponent<PlayableDirector>();
            director.Play();
            await director.WaitAsync();
            UnityEngine.Object.Destroy(go);
        }

        public void ShowBGEnd(DuelResult result)
        {
            void HeroWin()
            {
                field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndWin);
                if (CanPlayMateAction(0, mate0))
                    mate0.Play(Mate.MateAction.Victory);
            }

            void HeroLose()
            {
                bgPhase0 = 4;
                var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE4_P";
                field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase1ToPhase2);
                field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase2ToPhase3);
                field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase3ToPhase4);
                field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd, seLabel);
                field0Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndLose);

                if (stand0Manager != null)
                    stand0Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                if (CanPlayMateAction(0, mate0))
                    mate0.Play(Mate.MateAction.Defeat);
            }

            void RivalWin()
            {
                field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndWin);
                if (CanPlayMateAction(1, mate1))
                    mate1.Play(Mate.MateAction.Victory);
            }

            void RivalLose()
            {
                bgPhase1 = 4;
                var seLabel = "SE_FIELD_MAT" + field0Manager.name.Substring(4, 3) + "_PHASE4_R";
                field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase1ToPhase2);
                field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase2ToPhase3);
                field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase3ToPhase4);
                field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd, seLabel);
                field1Manager.PlayAnimatorTrigger(TriggerLabelDefine.EndLose);

                if (stand1Manager != null)
                    stand1Manager.PlayAnimatorTrigger(TriggerLabelDefine.DamagePhase4ToEnd);
                if (CanPlayMateAction(1, mate1))
                    mate1.Play(Mate.MateAction.Defeat);
            }

            if (result == DuelResult.Win)
            {
                HeroWin();
                RivalLose();
            }
            else if (result == DuelResult.Lose)
            {
                HeroLose();
                RivalWin();
            }
            else if(result == DuelResult.Draw)
            {
                HeroLose();
                RivalLose();
            }
        }

        public async UniTask ShowChainStack()
        {
            int chain = cardsInChain.Count;
            if (chain == 1)
                return;
            if (!NeedChainAnimation())
                return;

            GameObject animation;
            if (chain < 3)
                animation = ABLoader.LoadMasterDuelGameObject("DuelChainStack01");
            else
            {
                animation = ABLoader.LoadMasterDuelGameObject("DuelChainStack02");
                DOTween.To(v => { }, 0, 0, 0.0166f).OnComplete(() =>
                {
                    AudioManager.PlaySE("SE_DUELCHAIN_STACK02");
                });
                DOTween.To(v => { }, 0, 0, 0.767f).OnComplete(() =>
                {
                    if (chain == 3)
                        AudioManager.PlaySE("SE_DUEL_CHAIN_NUMEFF_01");
                    else if (chain == 4)
                        AudioManager.PlaySE("SE_DUEL_CHAIN_NUMEFF_02");
                    else
                        AudioManager.PlaySE("SE_DUEL_CHAIN_NUMEFF_03");
                });
            }
            var director = animation.GetComponent<PlayableDirector>();
            var manager = animation.GetComponent<ElementObjectManager>();

            if(chain >= 4)
            {
                director.GetTrackAsset("LCardLightSetScaleC03").muted = true;
                director.GetTrackAsset("LCardLightSetScaleC04").muted = false;
                director.GetTrackAsset("RCardLightSetScaleC03").muted = true;
                director.GetTrackAsset("RCardLightSetScaleC04").muted = false;
            }

            ElementObjectManager targetCardD;
            if (controllerInChain[chain - 1] == 0)
            {
                targetCardD = manager.GetElement<ElementObjectManager>("DummyChainCardDL");
                manager.GetElement("ChainCardSetDROffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Tens"),
                    chain);
            }
            else
            {
                targetCardD = manager.GetElement<ElementObjectManager>("DummyChainCardDR");
                manager.GetElement("ChainCardSetDLOffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Tens"),
                    chain);
            }
            _ = Program.instance.texture_.LoadDummyCard(targetCardD, codesInChain[chain - 1], 0, true);

            if (controllerInChain[chain - 1] == controllerInChain[chain - 2])
            {
                manager.GetElement("ChainStraightCLtoDR").SetActive(false);
                manager.GetElement("ChainStraightCRtoDL").SetActive(false);
            }
            else
            {
                if (controllerInChain[chain - 1] == 0)
                    manager.GetElement("ChainStraightCLtoDR").SetActive(false);
                else
                    manager.GetElement("ChainStraightCRtoDL").SetActive(false);
            }
            ElementObjectManager targetCardC;
            if (controllerInChain[chain - 2] == 0)
            {
                targetCardC = manager.GetElement<ElementObjectManager>("DummyChainCardCL");
                manager.GetElement("ChainCardSetCROffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumCL_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumCL_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumCL_Tens"),
                    chain - 1);
            }
            else
            {
                targetCardC = manager.GetElement<ElementObjectManager>("DummyChainCardCR");
                manager.GetElement("ChainCardSetCLOffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumCR_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumCR_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumCR_Tens"),
                    chain - 1);
            }
            _ = Program.instance.texture_.LoadDummyCard(targetCardC, codesInChain[chain - 2], 0, true);

            if (chain > 2)
            {
                if (controllerInChain[chain - 2] == controllerInChain[chain - 3])
                {
                    manager.GetElement("ChainStraightBLtoCR").SetActive(false);
                    manager.GetElement("ChainStraightBRtoCL").SetActive(false);
                }
                else
                {
                    if (controllerInChain[chain - 2] == 0)
                        manager.GetElement("ChainStraightBLtoCR").SetActive(false);
                    else
                        manager.GetElement("ChainStraightBRtoCL").SetActive(false);
                }
                ElementObjectManager targetCardB;
                if (controllerInChain[chain - 3] == 0)
                {
                    targetCardB = manager.GetElement<ElementObjectManager>("DummyChainCardBL");
                    manager.GetElement("ChainCardSetBROffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Tens"),
                        chain - 2);
                }
                else
                {
                    targetCardB = manager.GetElement<ElementObjectManager>("DummyChainCardBR");
                    manager.GetElement("ChainCardSetBLOffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Tens"),
                        chain - 2);
                }
                _ = Program.instance.texture_.LoadDummyCard(targetCardB, codesInChain[chain - 3], 0, true);

                if (chain > 3)
                {
                    if (controllerInChain[chain - 3] == controllerInChain[chain - 4])
                    {
                        manager.GetElement("ChainStraightALtoBR").SetActive(false);
                        manager.GetElement("ChainStraightARtoBL").SetActive(false);
                    }
                    else
                    {
                        if (controllerInChain[chain - 3] == 0)
                            manager.GetElement("ChainStraightALtoBR").SetActive(false);
                        else
                            manager.GetElement("ChainStraightARtoBL").SetActive(false);
                    }
                    ElementObjectManager targetCardA;
                    if (controllerInChain[chain - 4] == 0)
                    {
                        targetCardA = manager.GetElement<ElementObjectManager>("DummyChainCardAL");
                        manager.GetElement("ChainCardSetAROffset").SetActive(false);
                        ChangeChainNumber(
                            manager.GetElement<SpriteRenderer>("ChainNumAL_Digit"),
                            manager.GetElement<SpriteRenderer>("ChainNumAL_Ones"),
                            manager.GetElement<SpriteRenderer>("ChainNumAL_Tens"),
                            chain - 3);
                    }
                    else
                    {
                        targetCardA = manager.GetElement<ElementObjectManager>("DummyChainCardAR");
                        manager.GetElement("ChainCardSetALOffset").SetActive(false);
                        ChangeChainNumber(
                            manager.GetElement<SpriteRenderer>("ChainNumAR_Digit"),
                            manager.GetElement<SpriteRenderer>("ChainNumAR_Ones"),
                            manager.GetElement<SpriteRenderer>("ChainNumAR_Tens"),
                            chain - 3);
                    }
                    _ = Program.instance.texture_.LoadDummyCard(targetCardA, codesInChain[chain - 4], 0, true);
                }
                else
                {
                    manager.GetElement("ChainStraightALtoBR").SetActive(false);
                    manager.GetElement("ChainStraightARtoBL").SetActive(false);
                    manager.GetElement("ChainCardSetALOffset").SetActive(false);
                    manager.GetElement("ChainCardSetAROffset").SetActive(false);
                }
            }

            await director.WaitAsync();
            UnityEngine.Object.Destroy(animation);
        }

        public async UniTask ShowChainResolve()
        {
            var chain = chainSolvingIndex;
            if (cardsInChain.Count == 1)
                return;
            if (!NeedChainAnimation())
                return;

            GameObject animation;
            if (chain == 1)
                animation = ABLoader.LoadMasterDuelGameObject("DuelChainResolve01");
            else if (chain == 2)
                animation = ABLoader.LoadMasterDuelGameObject("DuelChainResolve02");
            else
                animation = ABLoader.LoadMasterDuelGameObject("DuelChainResolve03");

            var director = animation.GetComponent<PlayableDirector>();
            var manager = animation.GetComponent<ElementObjectManager>();

            ElementObjectManager targetCardD;
            if (controllerInChain[chain - 1] == 0)
            {
                targetCardD = manager.GetElement<ElementObjectManager>("DummyChainCardDL");
                manager.GetElement("ChainCardSetDROffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumDL_Tens"),
                    chain);
            }
            else
            {
                targetCardD = manager.GetElement<ElementObjectManager>("DummyChainCardDR");
                manager.GetElement("ChainCardSetDLOffset").SetActive(false);
                ChangeChainNumber(
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Digit"),
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Ones"),
                    manager.GetElement<SpriteRenderer>("ChainNumDR_Tens"),
                    chain);
            }
            _ = Program.instance.texture_.LoadDummyCard(targetCardD, codesInChain[chain - 1], 0, true);

            if (chain > 1)
            {
                if (chain != cardsInChain.Count)
                {
                    manager.GetComponent<PlayableDirector>().time = 0.83f;
                    manager.GetElement("ResolveTextSet").SetActive(false);
                }
                if (controllerInChain[chain - 1] == controllerInChain[chain - 2])
                {
                    manager.GetElement("ChainStraightCLtoDR").SetActive(false);
                    manager.GetElement("ChainStraightCRtoDL").SetActive(false);
                }
                else
                {
                    if (controllerInChain[chain - 1] == 0)
                        manager.GetElement("ChainStraightCLtoDR").SetActive(false);
                    else
                        manager.GetElement("ChainStraightCRtoDL").SetActive(false);
                }

                ElementObjectManager targetCardC;
                if (controllerInChain[chain - 2] == 0)
                {
                    targetCardC = manager.GetElement<ElementObjectManager>("DummyChainCardCL");
                    manager.GetElement("ChainCardSetCROffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumCL_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumCL_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumCL_Tens"),
                        chain - 1);
                }
                else
                {
                    targetCardC = manager.GetElement<ElementObjectManager>("DummyChainCardCR");
                    manager.GetElement("ChainCardSetCLOffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumCR_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumCR_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumCR_Tens"),
                        chain - 1);
                }
                _ = Program.instance.texture_.LoadDummyCard(targetCardC, codesInChain[chain - 2], 0, true);
            }

            if (chain > 2)
            {
                if (controllerInChain[chain - 2] == controllerInChain[chain - 3])
                {
                    manager.GetElement("ChainStraightBLtoCR").SetActive(false);
                    manager.GetElement("ChainStraightBRtoCL").SetActive(false);
                }
                else
                {
                    if (controllerInChain[chain - 2] == 0)
                        manager.GetElement("ChainStraightBLtoCR").SetActive(false);
                    else
                        manager.GetElement("ChainStraightBRtoCL").SetActive(false);
                }

                ElementObjectManager targetCardB;
                if (controllerInChain[chain - 3] == 0)
                {
                    targetCardB = manager.GetElement<ElementObjectManager>("DummyChainCardBL");
                    manager.GetElement("ChainCardSetBROffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumBL_Tens"),
                        chain - 2);
                }
                else
                {
                    targetCardB = manager.GetElement<ElementObjectManager>("DummyChainCardBR");
                    manager.GetElement("ChainCardSetBLOffset").SetActive(false);
                    ChangeChainNumber(
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Digit"),
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Ones"),
                        manager.GetElement<SpriteRenderer>("ChainNumBR_Tens"),
                        chain - 2);
                }
                _ = Program.instance.texture_.LoadDummyCard(targetCardB, codesInChain[chain - 3], 0, true);
            }

            await director.WaitAsync();
            UnityEngine.Object.Destroy(animation);
        }

        public async UniTask ShowCardEffectAnimation()
        {
            var card = cardsInChain[chainSolvingIndex - 1];
            card.ResolveChain(chainSolvingIndex);

            if (card.GetData().Id != codesInChain[chainSolvingIndex - 1])
                return;
            if (negatedInChain.Contains(chainSolvingIndex)
                || card.disabledInChain)
                return;
            if (negatedInChain.Contains(chainSolvingIndex) 
                || Core.CurrentChainDisabled(chainSolvingIndex)
                || card.negated 
                || card.Disabled)
            {
                card.disabledInChain = true;
                await card.AnimationNegate().WaitAsync();
                return;
            }
            
            if (condition == Condition.Duel
                && !Config.GetBool("DuelEffect",true))
                return;
            if (condition == Condition.Watch
                && !Config.GetBool("WatchEffect", true))
                return;
            if (condition == Condition.Replay
                && !Config.GetBool("ReplayEffect", true))
                return;
            
            var code = card.GetData().GetOriginalID();
            if (card.GetData().Id == 83764719)//死者苏生 异画
                code = 83764719;
            if (card.GetData().Id == 63166096)//闪刀起动-交闪 异画
                code = 63166096;
            if (card.GetData().Id == 32807848)//增援 异画
                code = 32807848;
            if (card.GetData().Id == 49238329)//强欲而金满之壶 异画
                code = 49238329;
            if (card.GetData().Id == 24224831)//墓穴的指名者 异画
                code = 24224831;

            var targetFolder = Program.root + "MasterDuel/Card/" + code.ToString();
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            targetFolder = Path.Combine(Application.dataPath, targetFolder);
#endif

            if (Directory.Exists(targetFolder))
            {
                if (!cardEffectCodes.Contains(code))
                {
                    var prefabs = await ABLoader.LoadsFromFolderAsync<PlayableDirector>("MasterDuel/Card/" + code.ToString());
                    if (code == 83764718)
                        prefabs[0].name = "Ef83764718";
                    else if(code == 83764719)
                        prefabs[0].name = "Ef83764719";
                    cardEffects.AddRange(prefabs);
                    cardEffectCodes.Add(code);
                }

                GameObject effect = null;

                // 旋风
                if (code == 5318639)
                {
                    if (card.effectTargets.Count > 0 && card.effectTargets[0].model != null)
                    {
                        AudioManager.PlaySE("SE_EV_CYCLONE");
                        effect = GetCardEffectPrefab("Ef04909_OP");
                        effect.transform.position = card.effectTargets[0].model.transform.position;
                        if (card.p.controller != 0)
                            effect.transform.localEulerAngles = new Vector3(0, 180, 0);
                    }
                    else
                        return;
                }
                // 月女神之镞
                else if (code == 2263869)
                {
                    if (card.effectTargets.Count > 0 && card.effectTargets[0].model != null)
                    {
                        AudioManager.PlaySE("SE_EV_ULTIMATE_SLAYER");
                        effect = GetCardEffectPrefab("Ef17469");
                    }
                    else
                        return;
                }
                // 雷击
                else if (code == 12580477)
                {
                    AudioManager.PlaySE("SE_EV_RAIGEKI");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef04343_Far" : "Ef04343_Near");
                    DOTween.To(v => { }, 0, 0, 0.4f).OnComplete(() =>
                    {
                        CameraManager.ShakeCamera(true);
                    });
                }
                // 灰流丽
                else if (code == 14558127)
                {
                    if (chainSolvingIndex > 1)
                    {
                        AudioManager.PlaySE("SE_EV_ASH_BLOSSOM_v2");
                        effect = GetCardEffectPrefab("Ef03891");
                        effect.transform.localPosition = GameCard.GetCardPosition(cardsInChain[chainSolvingIndex - 2].p);
                    }
                    else
                        return;
                }
                // 鹰身女妖的羽毛扫
                else if (code == 18144506)
                {
                    AudioManager.PlaySE("SE_EV_HARPIESFEATHER_DUSTER_3D");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef04678" : "Ef04678Op");
                    effect.transform.DestroyChildrenByName("DistPlane");
                }
                // 红色重启
                else if (code == 23002292)
                {
                    AudioManager.PlaySE("SE_EV_REDREBOOT");
                    effect = GetCardEffectPrefab("Ef13622");
                }
                // 墓穴的指名者
                else if (code == 24224830)
                {
                    AudioManager.PlaySE("SE_EV_CALLED_GRAVE");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef13619" : "Ef13619Op");
                }
                // 墓穴的指名者 异画
                else if (code == 24224831)
                {
                    AudioManager.PlaySE(card.p.InMyControl() ? "SE_EV_EF21233_R" : "SE_EV_EF21233_P");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? Tools.IsAspectRatioWidescreen() ? "Ef21233_Op" : "Ef21233_Op_4x3" : "Ef21233");
                }
                // 禁忌的一滴
                else if (code == 24299458)
                {
                    AudioManager.PlaySE("SE_EV_FORBIDDEN_DROPLET");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef15299_Far" : "Ef15299_Near");
                }
                // 三战之才
                else if (code == 25311006)
                {
                    AudioManager.PlaySE("SE_EV_TRIPLETACTICS_TALENT");
                    effect = GetCardEffectPrefab("Ef15296");
                }
                // 神之宣告
                else if (code == 41420027)
                {
                    AudioManager.PlaySE("SE_EV_SOLEMNJUDGMENT");
                    effect = GetCardEffectPrefab("Ef04861");
                }
                // 神圣防护罩 -反射镜力-
                else if (code == 44095762)
                {
                    AudioManager.PlaySE("SE_EV_MIRRORFORCE");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef04887" : "Ef04887Op");
                }
                // 黑洞
                else if (code == 53129443)
                {
                    AudioManager.PlaySE("SE_EV_BLACKHOLE");
                    effect = GetCardEffectPrefab("Ef04342");
                }
                // 冥王结界波
                else if (code == 54693926)
                {
                    AudioManager.PlaySE("SE_EV_DARKRULER_NOMORE");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef14742" : "Ef14742Op");
                }
                // 王宫的敕命
                else if (code == 61740673)
                {
                    AudioManager.PlaySE("SE_EV_IMPERIAL_ORDER");
                    effect = GetCardEffectPrefab("Ef04960");
                }
                // 魔法筒
                else if (code == 62279055)
                {
                    AudioManager.PlaySE("SE_EV_MAGIC_CYLINDER");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef05124_near" : "Ef05124_far");
                    var se = effect.AddComponent<ScreenEffect>();
                    se.cameraViewType = ScreenEffect.ViewType.View3D;
                    se.useMainCameraSetting = true;

                    //Tools.ChangeLayer(effect, "Default");
                }
                // 千把刀
                else if (code == 63391643)
                {
                    if (card.effectTargets.Count > 0 && card.effectTargets[0].model != null)
                    {
                        AudioManager.PlaySE("SE_EV_THOUSANDKNIVES");
                        effect = GetCardEffectPrefab("Ef05166");
                        effect.transform.position = card.effectTargets[0].model.transform.position;
                        if (!card.p.InMyControl())
                            effect.transform.localEulerAngles = new Vector3(0, 180, 0);
                    }
                    else
                        return;
                }
                // 抹杀之指名者
                else if (code == 65681983)
                {
                    AudioManager.PlaySE("SE_EV_CROSSOUT_DESIGNATOR");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef14627_Far" : "Ef14627_Near");
                }
                // 光之护封剑
                else if (code == 72302403)
                {
                    AudioManager.PlaySE("SE_EV_GOFUKEN");
                    effect = GetCardEffectPrefab(card.p.InMyControl() ? "Ef04354" : "Ef04354Op");
                }
                // 封印之黄金柜
                else if (code == 75500286)
                {
                    AudioManager.PlaySE("SE_EV_GOLD_SARCOPHAGUS");
                    effect = GetCardEffectPrefab("Ef06161");
                }
                // 死者苏生
                else if (code == 83764718 || code == 83764719)
                {
                    AudioManager.PlaySE("SE_EV_MONSTER_REBORN");
                    effect = GetCardEffectPrefab("Ef" + code);
                }
                // 闪刀起动-交闪
                else if (code == 63166095 || code == 63166096)
                {
                    nextMoveActionDuration = 2.2f + 5f;
                    nextMoveNeedCode = true;
                    nextMoveAction = (code2) =>
                    {
                        effect = GetCardEffectPrefab(code == 63166095 ? "Ef13671" : "Ef03434");
                        var manager = effect.GetComponent<ElementObjectManager>();
                        nextMoveManager = manager;
                        var renderer = manager.GetElement<Renderer>("SummonPosDummy");
                        _ = Program.instance.texture_.LoadCardToRendererWithMaterialAsync(renderer, code2, true);
                        UnityEngine.Object.Destroy(effect, 2.2f);
                    };

                    nextEventAction = () =>
                    {
                        if (nextMoveManager == null)
                            return;

                        var target = nextMoveManager.GetElement<Transform>("DummyCard01");
                        var card = lastMoveCard;
                        card.model.SetActive(true);
                        card.ResetModelRotation();
                        card.model.transform.position = target.position;
                        card.model.transform.eulerAngles = new Vector3(-target.eulerAngles.x, 0f, 0f);

                        nextMoveAction = null;
                        nextEventAction = null;
                        nextMoveManager = null;
                        _ = card.MoveAsync(card.p, false, 0f, 0.7f).ContinueWith(() => NoMoreWait = true);
                    };

                    return;
                }
                // 大风暴
                else if (code == 19613556)
                {
                    AudioManager.PlaySE("SE_EV_HEAVY_STORM");
                    effect = GetCardEffectPrefab("Ef04891");
                }
                // 天霆号 阿宙斯
                else if (code == 90448279)
                {
                    AudioManager.PlaySE("SE_EV_AZEUS");
                    effect = GetCardEffectPrefab("Ef15524");
                }
                // 增援
                else if (code == 32807846)
                {
                    AudioManager.PlaySE("SE_EV_039_NORMAL");
                    effect = GetCardEffectPrefab("Ef05328");
                }
                // 增援 异画
                else if (code == 32807848)
                {
                    AudioManager.PlaySE("SE_EV_039_SPECIAL");
                    effect = GetCardEffectPrefab("Ef20040");
                }
                // 幽鬼兔
                else if (code == 59438930)
                {
                    if (chainSolvingIndex > 1)
                    {
                        var targetCard = cardsInChain[chainSolvingIndex - 2];
                        if (!targetCard.p.InLocation(CardLocation.Onfield))
                            return;
                        AudioManager.PlaySE("SE_EV_038_NORMAL");
                        effect = GetCardEffectPrefab("Ef11708");
                        var manager = effect.GetComponent<ElementObjectManager>();
                        var cardEffect = manager.GetElement<Transform>("EffectOffset");
                        cardEffect.localPosition = GameCard.GetCardPosition(targetCard.p);
                        cardEffect.localEulerAngles = GameCard.GetCardRotation(targetCard.p);
                        cardEffect.localScale = GameCard.GetCardScale(targetCard.p);
                        manager.GetNestedElement<MeshRenderer>("CardOffset/DummyCard/DummyCardModel_front")
                            .material = targetCard.GetMaterial();
                    }
                    else
                        return;
                }
                // 欢聚友伴·抖抖海月水母
                else if (code == 84192580)
                {
                    AudioManager.PlaySE("SE_EV_040_NORMAL");
                    effect = GetCardEffectPrefab("Ef20206_Act01");
                    var manager = effect.GetComponent<ElementObjectManager>();
                    UnityEngine.Object.Destroy(manager.GetElement(card.p.InMyControl() ? "Hand01" : "EnHand01"));
                }
                // 欢聚友伴·茸茸长尾山雀
                else if (code == 42141493)
                {
                    AudioManager.PlaySE("SE_EV_041_NORMAL");
                    effect = GetCardEffectPrefab("Ef20500_Act01");
                    var manager = effect.GetComponent<ElementObjectManager>();

                    if (card.p.InMyControl())
                    {
                        UnityEngine.Object.Destroy(manager.GetElement("MainDeck"));
                        UnityEngine.Object.Destroy(manager.GetElement("ExDeck"));
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<ElementObjectManager>("EnMainDeck")
                            , Core.GetLocationCardCount(CardLocation.Deck, 1), opDeck);
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<ElementObjectManager>("EnExDeck")
                            , Core.GetLocationCardCount(CardLocation.Extra, 1), opExtra);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(manager.GetElement("EnMainDeck"));
                        UnityEngine.Object.Destroy(manager.GetElement("EnExDeck"));
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<ElementObjectManager>("MainDeck")
                            , Core.GetLocationCardCount(CardLocation.Deck, 0), myDeck);
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<ElementObjectManager>("ExDeck")
                            , Core.GetLocationCardCount(CardLocation.Extra, 0), myExtra);
                    }
                }
                // 欢聚友伴·喵喵豹猫
                else if (code == 87126721)
                {
                    AudioManager.PlaySE("SE_EV_042_NORMAL");
                    effect = GetCardEffectPrefab("Ef20764_Act01");

                    foreach (var sr in effect.transform.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.maskInteraction = SpriteMaskInteraction.None;

                    if (card.p.InMyControl())
                        UnityEngine.Object.Destroy(effect.transform.GetChildByName("GraveSet").gameObject);
                    else
                        UnityEngine.Object.Destroy(effect.transform.GetChildByName("EnGraveSet").gameObject);
                }
                // 灵王的波动
                else if (code == 40366667)
                {
                    if (chainSolvingIndex > 1)
                    {
                        nextNegateAction_Additional = () =>
                        {
                            AudioManager.PlaySE("SE_EV_044_NORMAL");
                            effect = GetCardEffectPrefab("Ef20555");
                            effect.transform.DestroyChildByName("BG");
                            var manager = effect.GetComponent<ElementObjectManager>();
                            manager.GetNestedElement("CardOffset/DummyCard").SetActive(false);
                            var targetEff = card.setOverTurn ? "CardOffset/nomalEf" : "CardOffset/handEf";
                            manager.GetNestedElement(targetEff).SetActive(true);
                            nextNegateAction_AdditionalManager = manager;
                            nextNegateAction_AdditionalTime = 0.5f;
                            UnityEngine.Object.Destroy(effect, 2f);
                        };
                    }
                    return;
                }
                // 圣王的粉碎
                else if (code == 97045737)
                {
                    if (chainSolvingIndex > 1)
                    {
                        nextNegateAction_Additional = () =>
                        {
                            AudioManager.PlaySE("SE_EV_043_NORMAL");
                            effect = GetCardEffectPrefab("Ef20257");
                            effect.transform.DestroyChildByName("BG");
                            var manager = effect.GetComponent<ElementObjectManager>();
                            manager.GetElement("CardOffset").SetActive(false);
                            nextNegateAction_AdditionalManager = manager;
                            nextNegateAction_AdditionalTime = 0.8f;
                            UnityEngine.Object.Destroy(effect, 2f);
                        };
                    }
                    return;
                }
                // 小丑与锁鸟
                else if (code == 94145021)
                {
                    AudioManager.PlaySE("SE_EV_EF09279_v1");
                    effect = GetCardEffectPrefab("Ef09279_Act01");
                    var manager = effect.GetComponent<ElementObjectManager>();

                    if (card.p.InMyControl())
                    {
                        UnityEngine.Object.Destroy(manager.GetElement("MainDeck"));
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<ElementObjectManager>("EnMainDeck")
                            , Core.GetLocationCardCount(CardLocation.Deck, 1), opDeck);
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(manager.GetElement("EnMainDeck"));
                        DuelEffectUtil.SetDeckModelAppearance(manager.GetElement<ElementObjectManager>("MainDeck")
                            , Core.GetLocationCardCount(CardLocation.Deck, 0), myDeck);
                    }
                }
                // 强欲而金满之壶 异画
                else if (code == 49238329)
                {
                    AudioManager.PlaySE("SE_EV_EF14144_v2");
                    effect = GetCardEffectPrefab("Ef21234");
                }

                var director = effect.GetComponent<PlayableDirector>();
                await director.AutoDestroy();
            }
            else
            {
                // 技能抽取
                if (code == 82732705)
                {
                    if (card.model == null)
                        return;

                    AudioManager.PlaySE("SE_EV_SKILLDRAIN");
                    var effArea = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_05740_Area", true, true);
                    var effCard = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_05740_Card", true, true);
                    effCard.SetActive(false);
                    effCard.transform.position = card.model.transform.position;
                    await UniTask.WaitForSeconds(0.5f);
                    effCard.SetActive(true);
                    await UniTask.WaitForSeconds(1f);
                    UnityEngine.Object.Destroy(effArea);
                    UnityEngine.Object.Destroy(effCard);
                }
                // 无限泡影
                else if (code == 10045474)
                {
                    if (card.effectTargets.Count == 0 || card.effectTargets[0].model == null)
                        return;

                    var time = 1.5f;

                    AudioManager.PlaySE("SE_EV_INFINITE_IMPERMANENCE");
                    var effCard = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_13631_Card", true, true);
                    effCard.transform.position = card.effectTargets[0].model.transform.position;
                    if (card.effectTargets[0].p.InPosition(CardPosition.Attack))
                        UnityEngine.Object.Destroy(effCard.transform.GetChild(0).GetChild(1).gameObject);
                    else
                        UnityEngine.Object.Destroy(effCard.transform.GetChild(0).GetChild(0).gameObject);
                    if (card.setOverTurn)
                    {
                        GameObject effArea = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_13631_Area", true, false);
                        GameObject effAreaLoop = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_13631_Area_Loop", true, false);
                        foreach (var place in places)
                        {
                            if (place.InTheSameLine(card.p))
                            {
                                var area = UnityEngine.Object.Instantiate(effArea);
                                area.transform.position = place.transform.position;
                                UnityEngine.Object.Destroy(area, time);
                                var loop = UnityEngine.Object.Instantiate(effAreaLoop);
                                loop.transform.SetParent(place.transform, false);
                                if ((place.p.location & (uint)CardLocation.MonsterZone) > 0)
                                    loop.transform.localScale = new Vector3(1f, 1f, 1.1f);
                                allGameObjects.Add(loop);
                                turnEndDeleteObjects.Add(loop);
                            }
                        }
                    }
                    await UniTask.WaitForSeconds(time);
                    UnityEngine.Object.Destroy(effCard);
                }
                // 闪电风暴
                else if (code == 14532163)
                {
                    var eff = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MagicTrapEffects/fxp_14876", true, true);
                    var manager = eff.GetComponent<ElementObjectManager>();
                    if (card.p.InMyControl())
                    {
                        AudioManager.PlaySE("SE_EV_LIGHTNINGSTORM_P");
                        manager.GetElement("NearMonster").SetActive(false);
                    }
                    else
                    {
                        AudioManager.PlaySE("SE_EV_LIGHTNINGSTORM_R");
                        manager.GetElement("FarMonster").SetActive(false);
                    }
                    await UniTask.WaitForSeconds(1f);
                    UnityEngine.Object.Destroy(eff);
                }
                // 效果遮蒙者
                else if (code == 97268402)
                {
                    if (card.effectTargets.Count == 0 || card.effectTargets[0].model == null)
                        return;
                    AudioManager.PlaySE("SE_EV_EFFECT_VEILER");
                    var eff = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MonsterEffectProcess/fxp_mep08933_01", true, true);
                    eff.transform.position = card.effectTargets[0].model.transform.position;
                    await UniTask.WaitForSeconds(1f);
                    UnityEngine.Object.Destroy(eff);
                }
                // 屋敷童
                else if (code == 73642296)
                {
                    if (chainSolvingIndex < 2)
                        return;

                    await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Effects/MonsterEffectProcess/ef13587", true, false);
                    if (!cardEffectCodes.Contains(73642297))
                    {
                        var prefabs = await ABLoader.LoadsFromFolderAsync<PlayableDirector>("MasterDuel/Card/73642297");
                        cardEffects.AddRange(prefabs);
                        cardEffectCodes.Add(73642297);
                    }

                    nextNegateAction = () =>
                    {
                        AudioManager.PlaySE("SE_EV_GHOSTBELLE");

                        var targetCard = cardsInChain[chainSolvingIndex - 2];
                        var eff = ABLoader.LoadFromFolder<ParticleSystem>("MasterDuel/Effects/MonsterEffectProcess/ef13587", true, true);
                        eff.transform.localPosition = GameCard.GetCardPosition(targetCard.p);
                        Tools.ChangeLayer(eff, "DuelOverlay3D");
                        CameraManager.DuelOverlay3DPlus();

                        if (card.GetData().Id == 73642297)
                        {
                            AudioManager.PlaySE("SE_EV_037_SPECIAL");
                            var model = GetCardEffectPrefab(card.p.InMyControl() ? "Ef03892" : "Ef03892_Op");
                            model.transform.localPosition = GameCard.GetCardPosition(targetCard.p);
                            Tools.ChangeLayer(model, "DuelOverlay3D");
                            UnityEngine.Object.Destroy(model, 2f);
                        }
                        DOTween.To(v => { }, 0, 0, 2f).OnComplete(() =>
                        {
                            UnityEngine.Object.Destroy(eff);
                            CameraManager.DuelOverlay3DMinus();
                        });
                    };
                }
            }
        }

        public async UniTask ShowAllDeckModelsAsync()
        {
            if(myDeck == null || myExtra == null || opDeck == null || opExtra == null)
                return;

            myDeck.gameObject.SetActive(true);
            myExtra.gameObject.SetActive(true);
            opDeck.gameObject.SetActive(true);
            opExtra.gameObject.SetActive(true);

            var director = myDeck.GetComponent<PlayableDirector>();
            await director.WaitAsync();
            myDeck.GetElement("DeckSetOffset").SetActive(false);
            myDeck.GetElement("CardShuffleTop").SetActive(true);
            myExtra.GetElement("DeckSetOffset").SetActive(false);
            myExtra.GetElement("CardShuffleTop").SetActive(true);
            opDeck.GetElement("DeckSetOffset").SetActive(false);
            opDeck.GetElement("CardShuffleTop").SetActive(true);
            opExtra.GetElement("DeckSetOffset").SetActive(false);
            opExtra.GetElement("CardShuffleTop").SetActive(true);
        }

        public void ShowTargetLines(Vector3 start, List<GameCard> targets)
        {
            if (targetLine == null)
                return;

            foreach (var card in targets)
            {
                if ((card.p.location & (uint)CardLocation.Onfield) > 0)
                {
                    var newLine = UnityEngine.Object.Instantiate(targetLine);
                    newLine.SetActive(true);
                    var line = newLine.transform.GetChild(0).GetComponent<LineRenderer>();
                    var end = card.model.transform.position;
                    var posArr = new Vector3[9]
                    {
                        new(start.x, 1f, start.z),
                        new(start.x + (end.x - start.x) * 0.125f, 5f, start.z + (end.z - start.z) * 0.125f),
                        new(start.x + (end.x - start.x) * 0.25f, 9f, start.z + (end.z - start.z) * 0.25f),
                        new(start.x + (end.x - start.x) * 0.375f, 11f, start.z + (end.z - start.z) * 0.375f),
                        new(start.x + (end.x - start.x) * 0.5f, 12f, start.z + (end.z - start.z) * 0.5f),
                        new(start.x + (end.x - start.x) * 0.625f, 11f, start.z + (end.z - start.z) * 0.625f),
                        new(start.x + (end.x - start.x) * 0.75f, 9f, start.z + (end.z - start.z) * 0.75f),
                        new(start.x + (end.x - start.x) * 0.875f, 5f, start.z + (end.z - start.z) * 0.875f),
                        new(end.x, 1f, end.z),
                    };
                    line.SetPositions(posArr);
                    targetLines.Add(newLine);
                }
            }
        }

        public void HideTargetLines()
        {
            foreach (var line in targetLines)
                UnityEngine.Object.Destroy(line);
            targetLines.Clear();
        }

        public void ShowEquipLine(Vector3 start, Vector3 end)
        {
            if (equipLine == null)
                return;

            var line = equipLine.transform.GetChild(0).GetComponent<LineRenderer>();
            var posArr = new Vector3[9]
            {
                new(start.x, 1f, start.z),
                new(start.x + (end.x - start.x) * 0.125f, 1.5f, start.z + (end.z - start.z) * 0.125f),
                new(start.x + (end.x - start.x) * 0.25f, 2f, start.z + (end.z - start.z) * 0.25f),
                new(start.x + (end.x - start.x) * 0.375f, 2.5f, start.z + (end.z - start.z) * 0.375f),
                new(start.x + (end.x - start.x) * 0.5f, 2.8f, start.z + (end.z - start.z) * 0.5f),
                new(start.x + (end.x - start.x) * 0.625f, 2.5f, start.z + (end.z - start.z) * 0.625f),
                new(start.x + (end.x - start.x) * 0.75f, 2f, start.z + (end.z - start.z) * 0.75f),
                new(start.x + (end.x - start.x) * 0.875f, 1.5f, start.z + (end.z - start.z) * 0.875f),
                new(end.x, 1f, end.z),
            };
            line.SetPositions(posArr);
            equipLine.SetActive(true);
        }

        public void HideEquipLine()
        {
            if (equipLine == null)
                return;
            equipLine.SetActive(false);
        }

        public void ShowAttackLine(Vector3 end, Vector3 start)
        {
            if (attackLine == null)
                return;

            var lineManager = attackLine.GetComponent<ElementObjectManager>();
            var line1 = lineManager.GetElement<LineRenderer>("arrowlimeRollover");
            var line2 = lineManager.GetElement<LineRenderer>("arrowRollover");
            var posArr = new Vector3[9]
            {
                new(start.x, 5, start.z),
                new(start.x + (end.x - start.x) * 0.125f, 5.8f, start.z + (end.z - start.z) * 0.125f),
                new(start.x + (end.x - start.x) * 0.25f, 6.3f, start.z + (end.z - start.z) * 0.25f),
                new(start.x + (end.x - start.x) * 0.375f, 6.5f, start.z + (end.z - start.z) * 0.375f),
                new(start.x + (end.x - start.x) * 0.5f, 6.5f, start.z + (end.z - start.z) * 0.5f),
                new(start.x + (end.x - start.x) * 0.625f, 6.5f, start.z + (end.z - start.z) * 0.625f),
                new(start.x + (end.x - start.x) * 0.75f, 6.3f, start.z + (end.z - start.z) * 0.75f),
                new(start.x + (end.x - start.x) * 0.875f, 5.8f, start.z + (end.z - start.z) * 0.875f),
                new(end.x, 5, end.z),
            };
            line1.SetPositions(posArr);
            line2.SetPositions(posArr);
            attackLine.SetActive(true);
        }

        public void HideAttackLine()
        {
            
            if (attackLine == null)
                return;
            attackLine.SetActive(false);
        }

        public void ShowDuelFinalBlowText()
        {
            if(duelFinalBlow != null)
                UnityEngine.Object.Destroy(duelFinalBlow.gameObject);
            duelFinalBlow = ABLoader.LoadMasterDuelGameObject("DuelFinalBlow").GetComponent<DuelFinalBlow>();
        }

        public void HideDuelFinalBlowText()
        {
            if(duelFinalBlow == null) return;
            duelFinalBlow.Destroy();
        }

        public bool IsFinalBlow()
        {
            return duelFinalBlow != null;
        }

        public async UniTask<bool> NeedSpecialFinalAttackAsync(GameCard attackCard, Vector3 attackedPosition)
        {
            var data = attackCard.GetData();
            var returnValue = FinalAttackType.Normal;
            if (Settings.Data.FinalAttackBlueEyes.Contains(data.Id) || Settings.Data.FinalAttackBlueEyes.Contains(data.Alias))
                returnValue = FinalAttackType.BlueEyes;
            if (Settings.Data.FinalAttackDarkM.Contains(data.Id) || Settings.Data.FinalAttackDarkM.Contains(data.Alias))
                returnValue = FinalAttackType.DarkM;
            if (Settings.Data.FinalAttackRedEyes.Contains(data.Id) || Settings.Data.FinalAttackRedEyes.Contains(data.Alias))
                returnValue = FinalAttackType.RedEyes;
            if (Settings.Data.FinalAttackObelisk.Contains(data.Id) || Settings.Data.FinalAttackObelisk.Contains(data.Alias))
                returnValue = FinalAttackType.Obelisk;
            if (Settings.Data.FinalAttackRa.Contains(data.Id) || Settings.Data.FinalAttackRa.Contains(data.Alias))
                returnValue = FinalAttackType.Ra;
            if (Settings.Data.FinalAttackSlifer.Contains(data.Id) || Settings.Data.FinalAttackSlifer.Contains(data.Alias))
                returnValue = FinalAttackType.Slifer;

            if (returnValue == FinalAttackType.Normal)
                return false;
            await AnimationFinalAttackAsync(returnValue, attackCard, attackedPosition);
            return true;
        }

        public void FinishDamageEffect()
        {
            AudioManager.StopBGM();
            Core.GetUI<OcgCoreUI>().OnNor();

#if UNITY_EDITOR
            Program.instance.timeScaleForEditor = 0.1f;
            DOTween.To(() => Program.instance.timeScaleForEditor, x => Program.instance.timeScaleForEditor = x, 1f, 0.85f).SetEase(Ease.InQuad);
#else
            Program.instance.TimeScale = 0.1f;
            DOTween.To(() => Program.instance.TimeScale, x => Program.instance.TimeScale = x, 1f, 0.85f).SetEase(Ease.InQuad);
#endif

            if (life0 <= 0)
            {
                var hitObj = ABLoader.LoadMasterDuelGameObject("fxp_dithit_fin_near_001");
                hitObj.transform.position = new Vector3(0, 15, -25);
                UnityEngine.Object.Destroy(hitObj, 10);
            }
            if (life1 <= 0)
            {
                var hitObj = ABLoader.LoadMasterDuelGameObject("fxp_dithit_fin_far_001");
                hitObj.transform.position = new Vector3(0, 15, 25);
                UnityEngine.Object.Destroy(hitObj, 10);
            }
        }

        public void ReleaseTurnObjects()
        {
            foreach(var go in turnEndDeleteObjects)
                UnityEngine.Object.Destroy(go);
            turnEndDeleteObjects.Clear();
        }

        public async UniTask ShowTurnChangeBanner(int player)
        {
            if (turns < 2)
                return;

            var banner = ABLoader.LoadMasterDuelGameObject("DuelTurnChange0" + player);
            var director = banner.GetComponent<PlayableDirector>();
            await director.WaitAsync();
            UnityEngine.Object.Destroy(banner);
        }

        public async UniTask ShowPhaseBanner(int player, DuelPhase phase)
        {
            if (phase == DuelPhase.BattleStep
                || phase == DuelPhase.Damage
                || phase == DuelPhase.DamageCal
                || phase == DuelPhase.Battle
                || phase == DuelPhase.Damage)
                return;

            var tail = player == 0 ? "_near" : "_far";
            GameObject banner = phase switch
            {
                DuelPhase.Draw => ABLoader.LoadMasterDuelGameObject("DuelDrawPhase" + tail),
                DuelPhase.Standby => ABLoader.LoadMasterDuelGameObject("DuelStanbyPhase" + tail),
                DuelPhase.Main1 => ABLoader.LoadMasterDuelGameObject("DuelMain01Phase" + tail),
                DuelPhase.Main2 => ABLoader.LoadMasterDuelGameObject("DuelMain02Phase" + tail),
                DuelPhase.End => ABLoader.LoadMasterDuelGameObject("DuelEndPhase" + tail),
                _ => ABLoader.LoadMasterDuelGameObject("DuelBattlePhase" + tail),
            };

            string se = phase switch
            {
                DuelPhase.Draw => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_DRAW",
                DuelPhase.Standby => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_STANDBY",
                DuelPhase.Main1 => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_MAIN1",
                DuelPhase.Main2 => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_MAIN2",
                DuelPhase.End => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_END",
                _ => $"SE_PHASE{(player == 0 ? string.Empty : "_OPP")}_BATTLE",
            };

            AudioManager.PlaySE(se);
            var director = banner.GetComponent<PlayableDirector>();
            await director.WaitAsync();
            UnityEngine.Object.Destroy(banner);
        }

        public void SetPlayableGuide(bool isMe)
        {
            if (playableGuide == null)
                return;

            playableGuide.Set(isMe);
        }

        public async UniTask PlayShuffleDeckAsync(int player)
        {
            var animator = (player == 0 ? myDeck : opDeck).GetElement<Animator>("CardShuffleTop");
            animator.speed = 2;
            animator.SetTrigger("Shuffle");
            CameraManager.Overlay3DReset();
            CameraManager.DuelOverlay3DPlus();
            Tools.ChangeLayer(animator.gameObject, "DuelOverlay3D");
            Program.instance.audio_.PlayShuffleSE();
            await UniTask.WaitForSeconds(0.5f);
            animator.SetTrigger("Idle");
            await UniTask.Yield();
            CameraManager.DuelOverlay3DMinus();
            Tools.ChangeLayer(animator.gameObject, "Default");
        }

        public async UniTask PlaySummonPendulum()
        {
            Core.GetUI<OcgCoreUI>().CardDescription.Hide();

            var pendulum = ABLoader.LoadMasterDuelGameObject("SummonPendulum01");
            var manager = pendulum.GetComponent<ElementObjectManager>();
            manager = manager.GetElement<ElementObjectManager>("SummonPendulumShowCard");
            pendulum.transform.SetParent(Program.instance.container_3D, false);

            var card1 = manager.GetElement<ElementObjectManager>("DummyCard01");
            var card2 = manager.GetElement<ElementObjectManager>("DummyCard02");
            _ = Program.instance.texture_.LoadDummyCard(card1, cardsBeTarget[0].GetData().Id, cardsBeTarget[0].p.controller);
            _ = Program.instance.texture_.LoadDummyCard(card2, cardsBeTarget[1].GetData().Id, cardsBeTarget[1].p.controller);
            var scale1 = cardsBeTarget[0].GetData().LScale;
            var scale2 = cardsBeTarget[1].GetData().RScale;

            if (scale1 < 10)
            {
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00Ones"));
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00Tens"));
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00OnesA"));
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00TensA"));

                manager.GetElement<MeshRenderer>("LPendulumNum00Digit").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("LPendulumNum0" + scale1);
                manager.GetElement<MeshRenderer>("LPendulumNum00DigitA").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("LPendulumNum0" + scale1);
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00Digit"));
                UnityEngine.Object.Destroy(manager.GetElement("LPendulumNum00DigitA"));

                manager.GetElement<MeshRenderer>("LPendulumNum00Tens").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("LPendulumNum01");
                manager.GetElement<MeshRenderer>("LPendulumNum00TensA").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("LPendulumNum01");
                manager.GetElement<MeshRenderer>("LPendulumNum00Ones").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("LPendulumNum0" + (scale1 - 10));
                manager.GetElement<MeshRenderer>("LPendulumNum00OnesA").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("LPendulumNum0" + (scale1 - 10));
            }

            if (scale2 < 10)
            {
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00Ones"));
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00Tens"));
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00OnesA"));
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00TensA"));

                manager.GetElement<MeshRenderer>("RPendulumNum00Digit").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("RPendulumNum0" + scale2);
                manager.GetElement<MeshRenderer>("RPendulumNum00DigitA").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("RPendulumNum0" + scale2);
            }
            else
            {
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00Digit"));
                UnityEngine.Object.Destroy(manager.GetElement("RPendulumNum00DigitA"));

                manager.GetElement<MeshRenderer>("RPendulumNum00Tens").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("RPendulumNum01");
                manager.GetElement<MeshRenderer>("RPendulumNum00TensA").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("RPendulumNum01");
                manager.GetElement<MeshRenderer>("RPendulumNum00Ones").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("RPendulumNum0" + (scale2 - 10));
                manager.GetElement<MeshRenderer>("RPendulumNum00OnesA").material.mainTexture
                    = ABLoader.LoadMasterDuelTexture("RPendulumNum0" + (scale2 - 10));
            }

            if(MasterRule >= 4)
            {
                var scaleSet = ABLoader.LoadMasterDuelGameObject("SummonPendulumScaleSet");
                scaleSet.transform.SetParent(Program.instance.container_3D);
                if (!cardsBeTarget[0].p.InMyControl())
                    scaleSet.transform.localEulerAngles = new Vector3(0, 180, 0);
                var ltm = scaleSet.GetComponent<LoopTrackManager>();
                await UniTask.WaitForSeconds(3.5f);
                ltm.StopLoop();
                await UniTask.WaitForSeconds(0.5f);
                UnityEngine.Object.Destroy(scaleSet);
                UnityEngine.Object.Destroy(pendulum);
            }
            else
            {
                await UniTask.WaitForSeconds(4f);
                UnityEngine.Object.Destroy(pendulum);
            }
        }

        public bool HoveringField0()
        {
            if (field0Manager == null)
                return false;
            if (UserInput.HoverObject == field0Manager.gameObject)
                return true;
            return false;
        }

        public bool HoveringField1()
        {
            if (field1Manager == null)
                return false;
            if (UserInput.HoverObject == field1Manager.gameObject)
                return true;
            return false;
        }

        public void TapField0()
        {
            if (field0Manager == null)
                return;
            if (Time.time - field0TapTime < 1f)
                return;
            field0TapTime = Time.time;
            field0Manager.PlayTapAnimation();
        }

        public void TapField1()
        {
            if (field1Manager == null)
                return;
            if (Time.time - field1TapTime < 1f)
                return;
            field1TapTime = Time.time;
            field1Manager.PlayTapAnimation();
        }

        public bool HoveringMate0()
        {
            if (mate0 == null)
                return false;
            if (UserInput.HoverObject == mate0.gameObject)
                return true;
            if (UserInput.HoverObject != null && UserInput.HoverObject.transform.IsChildOf(mate0.transform))
                return true;
            return false;
        }

        public bool HoveringMate1()
        {
            if (mate1 == null)
                return false;
            if (UserInput.HoverObject == mate1.gameObject)
                return true;
            if (UserInput.HoverObject != null && UserInput.HoverObject.transform.IsChildOf(mate1.transform))
                return true;
            return false;
        }

        public void TapMate0()
        {
            if (!CanPlayMateAction(0, mate0, false))
                return;
            if (Time.time - mate0TapTime < 1f)
                return;
            mate0TapTime = Time.time;
            mate0.Play(Mate.MateAction.Tap);
        }

        public void TapMate1()
        {
            if (!CanPlayMateAction(1, mate1, false))
                return;
            if (Time.time - mate1TapTime < 1f)
                return;
            mate1TapTime = Time.time;
            mate1.Play(Mate.MateAction.Tap);
        }

        public void PlayMate0Random()
        {
            if (!CanPlayMateAction(0, mate0))
                return;
            if (ShouldSuppressAmbientPremiumMateActions(GetPremiumMateState(0)))
                return;
            if (!mate0Random)
                return;

            mate0.Play(Mate.MateAction.Random);
            RestartMateRandomCooldown(0);
        }

        public void PlayMate1Random()
        {
            if (!CanPlayMateAction(1, mate1))
                return;
            if (ShouldSuppressAmbientPremiumMateActions(GetPremiumMateState(1)))
                return;
            if (!mate1Random)
                return;

            mate1.Play(Mate.MateAction.Random);
            RestartMateRandomCooldown(1);
        }

        private bool CanPlayMateAction(int side, Mate mate, bool skipWhileTransitioning = true)
        {
            if (mate == null)
                return false;
            if (skipWhileTransitioning && IsPremiumMateTransitioning(side))
                return false;
            return true;
        }

        private bool IsPremiumMateTransitioning(int side)
        {
            var state = GetPremiumMateState(side);
            return state != null && state.IsTransitioning;
        }

        private void RestartMateRandomCooldown(int side)
        {
            var premiumState = GetPremiumMateState(side);
            if (ShouldSuppressAmbientPremiumMateActions(premiumState))
            {
                if (side == 0)
                {
                    mate0Random = false;
                    mate0RandomCooldownTween?.Kill();
                    mate0RandomCooldownTween = null;
                }
                else
                {
                    mate1Random = false;
                    mate1RandomCooldownTween?.Kill();
                    mate1RandomCooldownTween = null;
                }
                return;
            }

            var mate = side == 0 ? mate0 : mate1;
            var delay = mate != null && mate.type == Mate.MateType.MasterDuel
                ? UnityEngine.Random.Range(20f, 70f)
                : UnityEngine.Random.Range(8f, 16f);

            if (side == 0)
            {
                mate0Random = false;
                mate0RandomCooldownTween?.Kill();
                mate0RandomCooldownTween = DOTween.To(v => { }, 0, 0, delay).OnComplete(() =>
                {
                    mate0Random = true;
                });
            }
            else
            {
                mate1Random = false;
                mate1RandomCooldownTween?.Kill();
                mate1RandomCooldownTween = DOTween.To(v => { }, 0, 0, delay).OnComplete(() =>
                {
                    mate1Random = true;
                });
            }
        }

        public void SetBgTimeScale(float timeScale)
        {
            Tools.SetAnimatorTimescale(field0Manager.transform, timeScale);
            Tools.SetAnimatorTimescale(field1Manager.transform, timeScale);
            Tools.SetAnimatorTimescale(phaseButton.transform, timeScale);
            if (timerHandler != null)
                Tools.SetAnimatorTimescale(timerHandler.transform, timeScale);
            Tools.SetParticleSystemSimulationSpeed(field0Manager.transform, timeScale);
            Tools.SetParticleSystemSimulationSpeed(field1Manager.transform, timeScale);
        }

        public void SetTimeLimit(int player, int time)
        {
            if (timerHandler == null)
                return;
            timerHandler.time = time;
            timerHandler.player = player;
        }

        public void ResetFields()
        {
            field0Manager.gameObject.SetActive(false);
            field1Manager.gameObject.SetActive(false);
            field0Manager.gameObject.SetActive(true);
            field1Manager.gameObject.SetActive(true);
        }

        private CancellationTokenSource exTopCts;

        public void SetExDeckTop(GameCard card)
        {
            if(exTopCts != null)
            {
                exTopCts.Cancel();
                exTopCts.Dispose();
            }

            exTopCts = new CancellationTokenSource();

            var deck = card.p.InMyControl() ? myExtra : opExtra;
            var code = card.GetData().Id;
            var targetMat = MaterialLoader.GetCardMaterial(code, true);
            foreach (var r in deck.transform.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("back"))
                    r.material = targetMat;
            _ = SetCardToMaterialAsync(targetMat, code, exTopCts.Token);
        }

        private async UniTask SetCardToMaterialAsync(Material mat, int code, CancellationToken token)
        {
            var tex = await CardImageLoader.LoadCardAsync(code, true, token);
            if(mat == null)
                return;
            mat.mainTexture = tex;
        }

        public void UpdateExDeckTop(uint controller)
        {
            GameCard topCard = null;
            var extraCount = Core.GetLocationCardCount(CardLocation.Extra, controller);
            foreach (var c in cards)
                if (c.p.InSequence(controller, CardLocation.Extra, extraCount - 1))
                {
                    topCard = c;
                    break;
                }            

            if (topCard != null && topCard.p.InPosition(CardPosition.FaceUp) && topCard.GetData().Id != 0)
            {
                SetExDeckTop(topCard);
                return;
            }

            var deck = controller == 0 ? myExtra : opExtra;
            var targetMat = controller == 0 ? myProtector : opProtector;
            foreach (var r in deck.transform.GetComponentsInChildren<Renderer>(true))
                if (r.name.Contains("back"))
                    r.material = targetMat;
        }

        public static bool MatIsSpecial(string matName)
        {
            if (matName.StartsWith("Mat_013") || matName.StartsWith("Mat_045"))
                return true;
            return false;
        }

        #endregion

        #region Private

        private T Create<T>(bool addToList = true) where T : MonoBehaviour
        {
            GameObject obj = new(typeof(T).Name);
            T component = obj.AddComponent<T>();
            if (addToList)
                allGameObjects.Add(obj);
            return component;
        }

        private void CreatePlaceSelector(GPS p)
        {
            var go = new GameObject("PlaceSelector");
            var mono = go.AddComponent<PlaceSelector>();
            mono.p = p;
            go.transform.SetParent(Program.instance.container_3D);
            allGameObjects.Add(go);
            places.Add(mono);
        }

        private bool NeedChainAnimation()
        {
            bool config = true;
            if (condition == Condition.Duel && Config.Get("DuelChain", "1") == "0")
                config = false;
            else if (condition == Condition.Watch && Config.Get("WatchChain", "1") == "0")
                config = false;
            else if (condition == Condition.Replay && Config.Get("ReplayChain", "1") == "0")
                config = false;
            return config;
        }

        private void ChangeChainNumber(SpriteRenderer digit, SpriteRenderer one, SpriteRenderer ten, int number)
        {
            if (number < 10)
            {
                one.gameObject.SetActive(false);
                ten.gameObject.SetActive(false);
                digit.sprite = TextureManager.container.GetChainNumSprite(number);
            }
            else
            {
                digit.gameObject.SetActive(false);
                one.sprite = TextureManager.container.GetChainNumSprite(number % 10);
                ten.sprite = TextureManager.container.GetChainNumSprite((number / 10) % 10);
            }
        }

        private GameObject GetCardEffectPrefab(string name)
        {
            foreach(var go in cardEffects)
                if (go.name == name)
                    return UnityEngine.Object.Instantiate(go);
            return null;
        }

        private async UniTask AnimationFinalAttackAsync(FinalAttackType type, GameCard attackCard, Vector3 attackedPosition)
        {
            if (type == FinalAttackType.Normal)
                return;

            Sequence sequence;
            if (type == FinalAttackType.BlueEyes)
                sequence = await AnimationFinalAttack_BlueEyes(attackCard, attackedPosition);
            else if (type == FinalAttackType.DarkM)
                sequence = await AnimationFinalAttack_DarkM(attackCard, attackedPosition);
            else if (type == FinalAttackType.RedEyes)
                sequence = await AnimationFinalAttack_RedEyes(attackCard, attackedPosition);
            else if (type == FinalAttackType.Ra)
                sequence = await AnimationFinalAttack_Ra(attackCard, attackedPosition);
            else if (type == FinalAttackType.Slifer)
                sequence = await AnimationFinalAttack_Slifer(attackCard, attackedPosition);
            else// if (type == FinalAttackType.Obelisk)
                sequence = await AnimationFinalAttack_Obelisk(attackCard, attackedPosition);

            await sequence.WaitAsync();
        }

        private async UniTask<Sequence> AnimationFinalAttack_BlueEyes(GameCard attackCard, Vector3 attackedPosition)
        {
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/BlueEyes/CardSet", true, false);
            await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/BlueEyes/ScreenEffect", true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/BlueEyes/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, false);
            await ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/FinalAttack/BlueEyes/Beam", true, false);

            var attackRotation = attackCard.p.InMyControl() ? Vector3.zero : new Vector3(0f, 180f, 0f);
            CameraManager.Duel3DOverlayStickWithMain(true);

            var cardSet = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/BlueEyes/CardSet", true, true);
            var attackTransform = cardSet.transform;
            var cardSetManager = attackTransform.GetComponent<ElementObjectManager>();
            var subManager = cardSetManager.GetElement<ElementObjectManager>("Card");
            _ = Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            attackCard.model.SetActive(false);
            Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            var screenEffect = await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/BlueEyes/ScreenEffect", true, true);
            screenEffect.transform.SetParent(Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            var hit = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/BlueEyes/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, true);
            hit.transform.position = attackedPosition;
            hit.SetActive(false);

            var beam = await ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/FinalAttack/BlueEyes/Beam", true, true);
            beam.transform.SetParent(cardSetManager.transform, false);
            beam.transform.localPosition = new Vector3(0f, 1f, 0f);
            beam.GetComponent<PlayableDirector>().enabled = true;
            beam.GetComponent<PlayableDirector>().playOnAwake = true;
            beam.SetActive(false);

            var offset = new Vector3(0f, 5f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;
            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            AudioManager.PlaySE("SE_MONSTERATTACK_BE_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = faceAngle.y > 0 && faceAngle.y < 180 ? -60f : 60f;
            offset = new Vector3(0f, 5f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.6f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(45f, 0f, 0f), 0.6f).SetEase(Ease.InOutCubic));

            offset = new Vector3(0f, 5f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            sequence.Join(attackTransform.DORotate(faceAngle, 0.3f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.1f).OnComplete(() => 
            {
                beam.SetActive(true);
            }));

            offset = new Vector3(0f, 3f, 8f);
            if (!attackCard.p.InMyControl())
                offset.z = -8f;
            sequence.Append(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition + offset, 0.1f));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.15f).OnComplete(() =>
            {
                hit.SetActive(true);
                AudioManager.PlaySE("SE_MONSTERATTACK_BE_02");
                if (NextMessageIs(GameMessage.Damage))
                    NoMoreWait = true;
                CameraManager.ShakeCamera(true);
            }));
            sequence.AppendInterval(1f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                attackCard.model.SetActive(true);
                CameraManager.Duel3DOverlayStickWithMain(false);
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_DarkM(GameCard attackCard, Vector3 attackedPosition)
        {
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/CardSet", true, false);
            await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/DarkM/ScreenEffect", true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/LineRendererDA", true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/TargetPoint", true, false);

            var attackRotation = attackCard.p.InMyControl() ? Vector3.zero : new Vector3(0f, 180f, 0f);
            CameraManager.Duel3DOverlayStickWithMain(true);
            CameraManager.DuelOverlay3DPlus();

            var cardSet = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/CardSet", true, true);
            var screenEffect = await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/DarkM/ScreenEffect", true, true);
            var hit = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, true);
            var lineRendererDA = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/LineRendererDA", true, true);
            var targetPoint = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/DarkM/TargetPoint", true, true);

            Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            Tools.ChangeLayer(screenEffect, "DuelOverlay3D");
            Tools.ChangeLayer(hit, "DuelOverlay3D");
            Tools.ChangeLayer(lineRendererDA, "DuelOverlay3D");
            Tools.ChangeLayer(targetPoint, "DuelOverlay3D");

            var attackTransform = cardSet.transform;
            var cardSetManager = attackTransform.GetComponent<ElementObjectManager>();
            var subManager = cardSetManager.GetElement<ElementObjectManager>("Card");
            _ = Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            attackCard.model.SetActive(false);
            screenEffect.transform.SetParent(Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            hit.transform.position = attackedPosition;
            hit.SetActive(false);
            targetPoint.transform.position = attackedPosition;
            targetPoint.GetComponent<PlayableDirector>().playOnAwake = true;
            targetPoint.transform.LookAt(attackCard.model.transform);
            targetPoint.transform.GetChild(0).transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            targetPoint.SetActive(false);

            var lineManager = lineRendererDA.GetComponent<ElementObjectManager>();
            var line1 = lineManager.GetElement<LineRenderer>("Line01");
            var line2 = lineManager.GetElement<LineRenderer>("Line02");
            lineRendererDA.SetActive(false);

            var offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;

            var positions = new Vector3[2]
            {
                attackTransform.position + new Vector3(0f, 1f, 0f),
                attackedPosition
            };
            line1.SetPositions(positions);
            line2.SetPositions(positions);

            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            AudioManager.PlaySE("SE_MONSTERATTACK_BM_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = faceAngle.y > 0 && faceAngle.y < 180 ? -60f : 60f;
            offset = new Vector3(0f, 40f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.8f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(45f, 0f, 0f), 0.8f).SetEase(Ease.InOutCubic));

            offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(30f, 0f, 0f), 0.3f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.1f).OnComplete(() =>
            {
                lineRendererDA.SetActive(true);
                UnityEngine.Object.Destroy(lineRendererDA, 0.58f);
            }));

            offset = new Vector3(0f, 3f, 8f);
            if (attackCard.p.controller != 0)
                offset.z = -8f;
            sequence.Append(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition + offset, 0.1f));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.15f).OnComplete(() =>
            {
                hit.SetActive(true);
                targetPoint.SetActive(true);
                AudioManager.PlaySE("SE_MONSTERATTACK_BM_02");
                if (NextMessageIs(GameMessage.Damage))
                    NoMoreWait = true;
                CameraManager.ShakeCamera(true);
            }));

            sequence.AppendInterval(0.6f);

            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                UnityEngine.Object.Destroy(targetPoint);
                attackCard.model.SetActive(true);
                CameraManager.Duel3DOverlayStickWithMain(false);
                CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_RedEyes(GameCard attackCard, Vector3 attackedPosition)
        {
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/RedEyes/CardSet", true, false);
            await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/RedEyes/ScreenEffect", true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/RedEyes/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, false);
            await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Timeline/FinalAttack/RedEyes/Bless", true, false);

            var attackRotation = attackCard.p.controller == 0 ? Vector3.zero : new Vector3(0f, 180f, 0f);
            CameraManager.Duel3DOverlayStickWithMain(true);
            CameraManager.DuelOverlay3DPlus();

            var cardSet = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/RedEyes/CardSet", true, true);
            var screenEffect = await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/RedEyes/ScreenEffect", true, true);
            var hit = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/RedEyes/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, true);
            var bless = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Timeline/FinalAttack/RedEyes/Bless", true, true);
            bless.SetActive(false);

            Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            var attackTransform = cardSet.transform;
            var cardSetManager = attackTransform.GetComponent<ElementObjectManager>();
            var subManager = cardSetManager.GetElement<ElementObjectManager>("Card");
            _ = Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            cardSetManager.GetComponent<PlayableDirector>().Play();
            attackCard.model.SetActive(false);
            screenEffect.transform.SetParent(Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            hit.transform.position = attackedPosition;
            hit.SetActive(false);

            var offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;

            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            AudioManager.PlaySE("SE_MONSTERATTACK_RE_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = faceAngle.y > 0 && faceAngle.y < 180 ? -60f : 60f;
            offset = new Vector3(0f, 40f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.6f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(45f, 0f, 0f), 0.6f).SetEase(Ease.InOutCubic));

            offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(30f, 0f, 0f), 0.3f).SetEase(Ease.InOutCubic));

            offset = new Vector3(0f, 3f, 8f);
            if (!attackCard.p.InMyControl())
                offset.z = -8f;
            sequence.Append(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition + offset, 0.3f));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.05f).OnComplete(() =>
            {
                bless.SetActive(true);
                bless.transform.position = attackTransform.position;
                bless.transform.LookAt(attackedPosition);
                bless.transform.DOMove(attackedPosition, 0.3f);
            }));

            sequence.AppendCallback(() =>
            {
                hit.SetActive(true);
                AudioManager.PlaySE("SE_MONSTERATTACK_RE_02");
                if (NextMessageIs(GameMessage.Damage))
                    NoMoreWait = true;
                CameraManager.ShakeCamera(true);
            });
            sequence.AppendInterval(0.6f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                UnityEngine.Object.Destroy(bless);
                attackCard.model.SetActive(true);
                CameraManager.Duel3DOverlayStickWithMain(false);
                CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_Ra(GameCard attackCard, Vector3 attackedPosition)
        {
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Ra/CardSet", true, false);
            await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/Ra/ScreenEffect", true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Ra/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, false);
            await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Timeline/FinalAttack/Ra/Bless", true, false);

            var attackRotation = attackCard.p.controller == 0 ? Vector3.zero : new Vector3(0f, 180f, 0f);
            CameraManager.Duel3DOverlayStickWithMain(true);
            CameraManager.DuelOverlay3DPlus();

            var cardSet = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Ra/CardSet", true, true);
            var screenEffect = await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/Ra/ScreenEffect", true, true);
            var hit = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Ra/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, true);
            var bless = await ABLoader.LoadFromFolderAsync<ParticleSystem>("MasterDuel/Timeline/FinalAttack/Ra/Bless", true, true);
            Tools.SetParticleSystemSimulationSpeed(bless.transform, 0.5f);
            bless.SetActive(false);

            Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            var attackTransform = cardSet.transform;
            var cardSetManager = attackTransform.GetComponent<ElementObjectManager>();
            var subManager = cardSetManager.GetElement<ElementObjectManager>("Card");
            _ = Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            cardSetManager.GetComponent<PlayableDirector>().Play();
            attackCard.model.SetActive(false);
            screenEffect.transform.SetParent(Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            hit.transform.position = attackedPosition;
            hit.SetActive(false);

            var offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;

            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            AudioManager.PlaySE("SE_MONSTERATTACK_RA_01");
            Sequence sequence = DOTween.Sequence();
            faceAngle.z = 0f;
            offset = new Vector3(0f, 40f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 1f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(-30f, 0f, 0f), 1f).SetEase(Ease.InOutCubic));

            sequence.AppendCallback(() =>
            {
                bless.SetActive(true);
                bless.transform.position = attackTransform.position;
                bless.transform.LookAt(attackedPosition);

                bless.transform.DOMove(attackedPosition, 0.3f);

                offset = new Vector3(0f, 3f, 8f);
                if (attackCard.p.controller != 0)
                    offset.z = -8f;
                Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition + offset, 0.3f);
            });

            offset = new Vector3(0f, 20f, 0f);
            if (!attackCard.p.InMyControl())
                offset.z = 0f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.OutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(30f, 0f, 0f), 0.3f).SetEase(Ease.OutCubic));


            sequence.AppendCallback(() =>
            {
                hit.SetActive(true);
                AudioManager.PlaySE("SE_MONSTERATTACK_RA_02");
                if (NextMessageIs(GameMessage.Damage))
                    NoMoreWait = true;
                CameraManager.ShakeCamera(true);
            });
            sequence.AppendInterval(0.6f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                UnityEngine.Object.Destroy(bless);
                attackCard.model.SetActive(true);
                CameraManager.Duel3DOverlayStickWithMain(false);
                CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_Slifer(GameCard attackCard, Vector3 attackedPosition)
        {
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Slifer/CardSet", true, false);
            await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/Slifer/ScreenEffect", true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Slifer/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, false);
            await ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/FinalAttack/Slifer/Beam", true, false);

            var attackRotation = attackCard.p.InMyControl() ? Vector3.zero : new Vector3(0f, 180f, 0f);
            CameraManager.Duel3DOverlayStickWithMain(true);
            CameraManager.DuelOverlay3DPlus();

            var cardSet = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Slifer/CardSet", true, true);
            Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            var attackTransform = cardSet.transform;
            var cardSetManager = attackTransform.GetComponent<ElementObjectManager>();
            var subManager = cardSetManager.GetElement<ElementObjectManager>("Card");
            _ = Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            attackCard.model.SetActive(false);

            var screenEffect = await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/Slifer/ScreenEffect", true, true);
            screenEffect.transform.SetParent(Program.instance.camera_.cameraDuelOverlay3D.transform, true);
            screenEffect.SetActive(false);

            var hit = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Slifer/Hit" + (attackCard.p.InMyControl() ? "Far" : "Near"), true, true);
            hit.transform.position = attackedPosition;
            hit.SetActive(false);

            var beam = await ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/FinalAttack/Slifer/Beam", true, true);
            beam.transform.SetParent(cardSetManager.transform, false);
            beam.transform.localPosition = new Vector3(0f, 1f, 0f);
            beam.GetComponent<PlayableDirector>().enabled = true;
            beam.GetComponent<PlayableDirector>().playOnAwake = true;
            beam.SetActive(false);

            var offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;
            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            AudioManager.PlaySE("SE_MONSTERATTACK_SLIFER_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = 0;
            offset = new Vector3(0f, 40f, -15f);
            if (!attackCard.p.InMyControl())
                offset.z = 15f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.8f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(faceAngle + new Vector3(30f, 0f, 0f), 0.8f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.4f).OnComplete(() =>
            {
                screenEffect.SetActive(true);
            }));

            offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.3f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            sequence.Join(attackTransform.DORotate(faceAngle, 0.3f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 0.1f).OnComplete(() =>
            {
                beam.SetActive(true);
            }));
            sequence.AppendCallback(() =>
            {
                offset = new Vector3(0f, 3f, 8f);
                if (attackCard.p.controller != 0)
                    offset.z = -8f;
                Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition + offset, 0.1f);
            });
            sequence.AppendInterval(0.1f);
            sequence.AppendCallback(() =>
            {
                hit.SetActive(true);
                AudioManager.PlaySE("SE_MONSTERATTACK_SLIFER_02");
                if (NextMessageIs(GameMessage.Damage))
                    NoMoreWait = true;
                CameraManager.ShakeCamera(true);
            });
            sequence.AppendInterval(0.6f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                attackCard.model.SetActive(true);
                CameraManager.Duel3DOverlayStickWithMain(false);
                CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        private async UniTask<Sequence> AnimationFinalAttack_Obelisk(GameCard attackCard, Vector3 attackedPosition)
        {
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/CardSet", true, false);
            await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/Obelisk/ScreenEffect", true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, false);
            await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/Punch", true, false);

            var attackRotation = attackCard.p.controller == 0 ? Vector3.zero : new Vector3(0f, 180f, 0f);
            CameraManager.Duel3DOverlayStickWithMain(true);
            CameraManager.DuelOverlay3DPlus();

            var cardSet = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/CardSet", true, true);
            var screenEffect = await ABLoader.LoadFromFolderAsync<ScreenEffect>("MasterDuel/Timeline/FinalAttack/Obelisk/ScreenEffect", true, true);
            var hit = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/Hit" + (attackCard.p.controller == 0 ? "Far" : "Near"), true, true);
            var punch = await ABLoader.LoadFromFolderAsync<ElementObjectManager>("MasterDuel/Timeline/FinalAttack/Obelisk/Punch", true, true);

            punch.GetComponent<PlayableDirector>().enabled = true;
            if (!attackCard.p.InMyControl())
                punch.transform.eulerAngles = new Vector3(0f, 180f, 0f);

            Tools.ChangeLayer(cardSet, "DuelOverlay3D");
            Tools.ChangeLayer(punch, "DuelOverlay3D");

            var attackTransform = cardSet.transform;
            var cardSetManager = attackTransform.GetComponent<ElementObjectManager>();
            var subManager = cardSetManager.GetElement<ElementObjectManager>("Card");
            _ = Program.instance.texture_.LoadDummyCard(subManager, attackCard.GetData().Id, attackCard.p.controller);
            cardSetManager.GetComponent<PlayableDirector>().Play();
            attackCard.model.SetActive(false);
            screenEffect.transform.SetParent(Program.instance.camera_.cameraDuelOverlay3D.transform, true);

            hit.transform.position = attackedPosition;
            hit.SetActive(false);
            punch.transform.position = attackedPosition;
            punch.SetActive(false);

            var offset = new Vector3(0f, 5f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            var attackPosition = attackCard.model.transform.position;
            attackTransform.position = attackPosition + offset;

            attackTransform.LookAt(attackedPosition);
            var faceAngle = attackTransform.eulerAngles;
            faceAngle.x = 0;
            attackTransform.eulerAngles = attackRotation;
            attackTransform.position = attackPosition;

            AudioManager.PlaySE("SE_MONSTERATTACK_OBELISK_01");
            Sequence sequence = DOTween.Sequence();

            faceAngle.z = 0f;
            offset = new Vector3(5f, 40f, -15f);
            if (!attackCard.p.InMyControl())
            {
                offset.x = -5f;
                offset.z = 15f;
            }
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 1.5f).SetEase(Ease.InOutCubic));
            offset = new Vector3(-30f, 35f, 0f);
            sequence.Join(attackTransform.DORotate(faceAngle + offset, 1.5f).SetEase(Ease.InOutCubic));
            sequence.Join(DOTween.To(v => { }, 0f, 0f, 1f).OnComplete(() =>
            {
                punch.SetActive(true);
            }));

            offset = new Vector3(0f, 20f, -8f);
            if (!attackCard.p.InMyControl())
                offset.z = 8f;
            sequence.Append(attackTransform.DOMove(attackPosition + offset, 0.4f).SetEase(Ease.InOutCubic));
            faceAngle.z = 0f;
            offset = new Vector3(20f, 0f, 0f);
            sequence.Join(attackTransform.DORotate(faceAngle + offset, 0.4f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.GetChild(0).DOLocalMoveZ(10f, 0.4f));
            sequence.Join(attackTransform.GetChild(0).DOLocalRotate(new Vector3(0f, -30f, 0f), 0.4f));
            offset = new Vector3(0f, 3f, 8f);
            if (!attackCard.p.InMyControl())
                offset.z = -8f;
            sequence.Join(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition + offset, 0.4f));
            sequence.AppendInterval(0.1f);
            sequence.AppendCallback(() =>
            {
                hit.SetActive(true);
                AudioManager.PlaySE("SE_MONSTERATTACK_OBELISK_02");
                if (NextMessageIs(GameMessage.Damage))
                    NoMoreWait = true;
                CameraManager.ShakeCamera(true);
            });

            sequence.AppendInterval(0.6f);
            sequence.Append(attackTransform.DOMove(attackPosition, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.DORotate(attackRotation, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.GetChild(0).DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.InOutCubic));
            sequence.Join(attackTransform.GetChild(0).DOLocalRotate(Vector3.zero, 0.5f).SetEase(Ease.InOutCubic));

            sequence.Join(Program.instance.camera_.cameraMain.transform.DOLocalMove(CameraManager.mainCameraDefaultPosition, 0.2f));

            sequence.OnComplete(() =>
            {
                UnityEngine.Object.Destroy(cardSet);
                UnityEngine.Object.Destroy(hit);
                UnityEngine.Object.Destroy(screenEffect);
                UnityEngine.Object.Destroy(punch);
                attackCard.model.SetActive(true);
                CameraManager.Duel3DOverlayStickWithMain(false);
                CameraManager.DuelOverlay3DMinus();
            });

            return sequence;
        }

        #endregion

        #region Enum

        private enum FinalAttackType
        {
            BlueEyes,
            DarkM,
            RedEyes,
            Slifer,
            Obelisk,
            Ra,
            Normal
        }

        #endregion

    }
}
