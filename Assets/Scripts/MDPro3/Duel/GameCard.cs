using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using YgomGame.Duel;
using YgomSystem.ElementSystem;
using static MDPro3.Servant.OcgCore;

namespace MDPro3
{
    public class GPS
    {
        public uint controller;
        public uint location;
        public uint sequence;
        public int position;
        public uint reason;

        public GPS Copy()
        {
            return (GPS)MemberwiseClone();
        }

        public bool InMyControl()
        {
            return controller == 0;
        }

        public bool InLocation(CardLocation location)
        {
            return (this.location & (uint)location) > 0;
        }

        public bool InLocation(CardLocation location1, CardLocation location2)
        {
            return InLocation(location1) || InLocation(location2);
        }

        public bool InLocation(CardLocation location1, CardLocation location2, CardLocation location3)
        {
            return InLocation(location1) || InLocation(location2) || InLocation(location3);
        }

        public bool InPosition(CardPosition position)
        {
            return (this.position & (uint)position) > 0;
        }

        public bool InPendulumSequence() //TODO
        {
            return sequence == 0 || sequence == 4 || sequence == 6 || sequence == 7;
        }

        public bool IsReason(CardReason reason)
        {
            return IsReason(this.reason, reason);
        }

        public bool InSequence(uint player, CardLocation location, int targetSequence)
        {
            return player == controller && InLocation(location) && sequence == targetSequence;
        }

        public static bool IsReason(uint reason, CardReason cardReason)
        {
            return (reason & (uint)cardReason) > 0;
        }

    }

    public class Effect
    {
        public string desc;
        public int flag;
        public int ptr;
        public bool forced;
    }

    public class GameCard : MonoBehaviour
    {
        private Card data = new ();
        private readonly Card lastValidData = new();
        public GPS p;
        private bool m_disabled;
        public bool Disabled
        {
            get
            {
                return m_disabled;
            }
            set
            {
                m_disabled = value;
                SetDisabled();
            }
        }
        public bool negated;
        public bool disabledInChain;
        public bool SemiNomiSummoned = false;
        public int selectPtr = 0;
        public int levelForSelect_1 = 0;
        public int levelForSelect_2 = 0;
        public int counterCanCount = 0;
        public int counterSelected = 0;
        public List<GameCard> targets = new();
        public List<GameCard> effectTargets = new();
        public List<GameCard> overlays = new();
        public GameCard overlayParent;
        public GameCard equipedCard;
        public List<Effect> effects = new();
        public bool forSelect;

        public int overFatherCount;
        private const float closeupLineColorIntensity = 1.5f;

        public GameObject model;
        public ElementObjectManager manager;
        public GPS cacheP;

        private bool inAnimation;
        private bool clicked;
        private bool hover;
        private bool hoving;

        private int currentCardCode = 0;
        private Texture currentCardTexture;

        private void Awake()
        {
            SystemEvent.OnVideoCardConfigChange += ReloadFaceWhenConfigChange;
        }

        public void Dispose()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }

            lock (this)
            {
                if (currentCardTexture != null && currentCardCode != 0)
                {
                    CardImageLoader.ReleaseCard(currentCardCode);
                    currentCardTexture = null;
                    currentCardCode = 0;
                }
            }

            Destroy(model);
            Destroy(this);
            SystemEvent.OnVideoCardConfigChange -= ReloadFaceWhenConfigChange;
        }

        private void LateUpdate()
        {
            if (model == null)
                return;

            hover = UserInput.HoverObject == manager.GetElement("CardModel");
            if (hover)
            {
                if(p.InMyControl() && HideMyHandCard)
                    HideMyHandCard = false;
                else if(!p.InMyControl() && HideOpHandCard)
                    HideOpHandCard = false;
            }
            if (!hover) hoving = false;

            if (hover && UserInput.MouseLeftUp && !handCardDraged)
                OnClick();
            else if (!hover && UserInput.MouseLeftUp)
            {
                if (UserInput.HoverObject == null)
                    NotClickThis();
                else if (UserInput.HoverObject.name != "PlaceSelector")
                    NotClickThis();
            }

            if (Math.Abs(handOffset - lastHandOffset) > 10)
                NotClickThis();

            if (p.InLocation(CardLocation.Hand))
            {
                if (hover && hoving == false && clicked == false)
                {
                    hoving = true;
                    handDefault = false;
                    AnimationHandHover();
                    MoveToHandDefault(0.1f);
                }
                if (hover && UserInput.MouseLeftUp && !handCardDraged)
                {
                    clicked = true;
                    handDefault = false;
                    AnimationHandAppeal();
                }
                if (!hover && UserInput.MouseLeftDown)
                {
                    clicked = false;
                }
                if (!hover && !clicked && !handDefault)
                {
                    AnimationHandDefault(0.1f);
                }
                if (Math.Abs(handOffset - lastHandOffset) > 10)
                    SetHandDefault();
            }
        }

        public Material GetMaterial()
        {
            if (model == null)
                return null;
            return manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>().material;
        }

        public void OnClick()
        {
            if (model == null)
                return;
            if ((p.location & (uint)CardLocation.Hand) == 0)
                AudioManager.PlaySE("SE_DUEL_SELECT");
            Program.instance.ocgcore.GetUI<OcgCoreUI>().CardDescription.Show(this, GetMaterial());
            if (data.HasType(CardType.Xyz) && (p.location & (uint)CardLocation.MonsterZone) > 0)
                Program.instance.ocgcore.GetUI<OcgCoreUI>().CardList.Show(Program.instance.ocgcore.GCS_GetOverlays(this), CardLocation.Overlay, (int)p.controller);
            else
                Program.instance.ocgcore.GetUI<OcgCoreUI>().CardList.Hide();

            if (equipedCard != null)
                Program.instance.ocgcore.ShowEquipLine(model.transform.position, equipedCard.model.transform.position);
            if (targets != null)
                Program.instance.ocgcore.ShowTargetLines(model.transform.position, targets);

            if (buttons.Count == 0 || Program.instance.ocgcore.currentPopup != null)
                return;
            foreach (var button in buttonObjs)
                button.Show();
            if (hightYellow)
                manager.GetElement("EffectHighlightYellowSelect").SetActive(true);
            else
                manager.GetElement("EffectHighlightBlueSelect").SetActive(true);
        }

        public void NotClickThis()
        {
            if (model == null || buttons.Count == 0)
                return;
            foreach (var button in buttonObjs)
                button.Hide();
            if (hightYellow)
                manager.GetElement("EffectHighlightYellowSelect").SetActive(false);
            else
                manager.GetElement("EffectHighlightBlueSelect").SetActive(false);
        }

        private GameObject CreateModel(bool real = true)
        {
            //model = Instantiate(Program.instance.ocgcore.container.cardModel);
            model = ABLoader.LoadMasterDuelGameObject("DuelCardModel");
            manager = model.GetComponent<ElementObjectManager>();

            manager.GetElement("StatusLabelRoot").SetActive(false);

            var cardMono = manager.GetElement<GameCardMono>("CardModel");
            cardMono.cookieCard = this;

            var cardParmUp = ABLoader.LoadMasterDuelGameObject("fxp_cardparm_up_001");
            var cardParmDown = ABLoader.LoadMasterDuelGameObject("fxp_cardparm_down_001");
            var cardParmChange = ABLoader.LoadMasterDuelGameObject("fxp_cardparm_change_001");

            var cardBuffActive = ABLoader.LoadMasterDuelGameObject("fxp_bff_active_001");
            var cardNegate = ABLoader.LoadMasterDuelGameObject("fxp_bff_disable_001");
            var cardDisquiet = ABLoader.LoadMasterDuelGameObject("fxp_bff_disquiet_001");
            var cardBlueHighlight = ABLoader.LoadMasterDuelGameObject("fxp_HL_set_001");
            var cardBlueHighlightSelect = ABLoader.LoadMasterDuelGameObject("fxp_HL_set_sct_001");
            var cardYellowHighlight = ABLoader.LoadMasterDuelGameObject("fxp_HL_SPsom_001");
            var cardYellowHighlightSelect = ABLoader.LoadMasterDuelGameObject("fxp_HL_SPsom_sct_001");

            cardParmUp.transform.SetParent(manager.GetElement<Transform>("Turn").GetChild(1), false);
            cardParmDown.transform.SetParent(manager.GetElement<Transform>("Turn").GetChild(1), false);
            cardParmChange.transform.SetParent(manager.GetElement<Transform>("Turn").GetChild(1), false);
            cardBuffActive.transform.SetParent(manager.GetElement<Transform>("Turn").GetChild(1), false);
            cardBuffActive.transform.localPosition = new Vector3(0, 0.1f, 0);
            cardNegate.transform.SetParent(manager.GetElement<Transform>("Turn").GetChild(1), false);
            cardNegate.transform.localPosition = new Vector3(0, 0.1f, 0);
            cardDisquiet.transform.SetParent(manager.GetElement<Transform>("Turn").GetChild(1), false);
            cardDisquiet.transform.localPosition = new Vector3(0, 0f, 0);
            cardDisquiet.transform.localEulerAngles = new Vector3(0f, 0f, 180f);

            var highlight = new GameObject("Highlight");
            cardBlueHighlight.transform.SetParent(highlight.transform, false);
            cardBlueHighlightSelect.transform.SetParent(highlight.transform, false);
            cardYellowHighlight.transform.SetParent(highlight.transform, false);
            cardYellowHighlightSelect.transform.SetParent(highlight.transform, false);
            highlight.transform.SetParent(manager.GetElement<Transform>("Turn").GetChild(1), false);
            //Tools.ChangeSortingLayer(highlight, "DuelEffect_Low");
            Tools.ChangeMaterialRenderQueue(highlight, 3001);

            var e1 = cardParmUp.AddComponent<ElementObject>();
            e1.label = "EffectBuff";
            var e2 = cardParmDown.AddComponent<ElementObject>();
            e2.label = "EffectDebuff";
            var e3 = cardParmChange.AddComponent<ElementObject>();
            e3.label = "EffectChange";
            var e4 = cardBuffActive.AddComponent<ElementObject>();
            e4.label = "EffectBuffActive";
            var e5 = cardNegate.AddComponent<ElementObject>();
            e5.label = "EffectNegate";
            var e6 = cardDisquiet.AddComponent<ElementObject>();
            e6.label = "EffectDisquiet";

            var e7 = cardBlueHighlight.AddComponent<ElementObject>();
            e7.label = "EffectHighlightBlue";
            var e8 = cardBlueHighlightSelect.AddComponent<ElementObject>();
            e8.label = "EffectHighlightBlueSelect";
            var e9 = cardYellowHighlight.AddComponent<ElementObject>();
            e9.label = "EffectHighlightYellow";
            var e10 = cardYellowHighlightSelect.AddComponent<ElementObject>();
            e10.label = "EffectHighlightYellowSelect";

            e7.transform.localScale = new Vector3(1.03f, 1f, 1.025f);
            e9.transform.localScale = new Vector3(1.02f, 1f, 1f);

            var list = manager.serializedElements.ToList();
            list.Add(e1);
            list.Add(e2);
            list.Add(e3);
            list.Add(e4);
            list.Add(e5);
            list.Add(e6);

            list.Add(e7);
            list.Add(e8);
            list.Add(e9);
            list.Add(e10);

            manager.serializedElements = list.ToArray();

            cardParmUp.SetActive(false);
            cardParmDown.SetActive(false);
            cardParmChange.SetActive(false);
            cardBuffActive.SetActive(false);
            cardNegate.SetActive(false);
            cardDisquiet.SetActive(false);

            cardBlueHighlight.SetActive(false);
            cardBlueHighlightSelect.SetActive(false);
            cardYellowHighlight.SetActive(false);
            cardYellowHighlightSelect.SetActive(false);

            var back = manager.GetElement<Transform>("CardModel").GetChild(0).GetComponent<Renderer>();

            back.material = p.controller ==0 ? myProtector : opProtector;
            back.material.renderQueue = 3000;
            back.material.SetFloat("_ZWrite", 1f);
            SetFace();

            model.transform.SetParent(Program.instance.ocgcore.GetFieldTransform(p.controller));
            if (real)
                return model;
            else
            {
                var go = model;
                model = null;
                return go;
            }
        }

        private CancellationTokenSource cts;

        private void SetFace()
        {
            if(cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            cts = new CancellationTokenSource();
            _ = SetFaceAsync(cts.Token);
        }

        private bool isRenderTexture;

        private void ReloadFaceWhenConfigChange()
        {
            var config = Config.GetBool("VideoCard", true);
            if (config && CardImageLoader.CardHasVideoArt(data.Id))
                SetFace();
            else if(!config && isRenderTexture)
                SetFace();
        }

        private async UniTask SetFaceAsync(CancellationToken cancellationToken)
        {
            Texture oldTexture = null;
            int oldCode = 0;
            lock (this)
            {
                oldTexture = currentCardTexture;
                oldCode = currentCardCode;
                currentCardTexture = null;
                currentCardCode = 0;
            }
            if (oldTexture != null && oldCode != 0)
            {
                CardImageLoader.ReleaseCard(oldCode);
            }

            var cardCode = data.Id;
            Renderer cardFace = manager.GetElement<Transform>("CardModel").
                    GetChild(1).GetComponent<Renderer>();

            var mat = MaterialLoader.GetCardMaterial(data.Id, true);
            if (model == null)
                return;
            cardFace.material = mat;
            cardFace.material.renderQueue = 2999;

            Texture texture = null;
            try
            {
                texture = await CardImageLoader.LoadCardAsync(cardCode, true, cancellationToken);
                isRenderTexture = texture is RenderTexture;

                if (this == null || model == null || data.Id != cardCode)
                {
                    if (texture != null)
                        CardImageLoader.ReleaseCard(cardCode);
                    return;
                }

                cardFace.material.mainTexture = texture;
                SetDisabled();

                lock (this)
                {
                    currentCardTexture = texture;
                    currentCardCode = cardCode;
                }
            }
            catch (OperationCanceledException)
            {
                if (texture != null)
                    CardImageLoader.ReleaseCard(cardCode);
                throw;
            }
            catch (Exception)
            {
                if (texture != null)
                    CardImageLoader.ReleaseCard(cardCode);
                throw;
            }
        }

        public Card GetData()
        {
            return data;
        }

        public Card GetValidData()
        {
            if (data.Id > 0)
                return data;
            else
                return lastValidData;
        }

        public void SetData(Card d)
        {
            if (d.Id > 0)
                d.CloneTo(lastValidData);
            else if (data.Id > 0)
                data.CloneTo(lastValidData);

            d.Attack = Card.NormalizeBattleValue(d.Attack, false);
            d.Defense = Card.NormalizeBattleValue(d.Defense, false);
            d.rAttack = Card.NormalizeBattleValue(d.rAttack, false);
            d.rDefense = Card.NormalizeBattleValue(d.rDefense, false);

            if (d.Id != data.Id)
            {
                data = d;
                if (model != null)
                {
                    if (data.Id > 0)
                    {
                        SetFace();
                        ShowFaceDownCardOrNot(NeedShowFaceDownCard());
                        RefreshHandShadowState();
                    }
                }
            }
            data = d;
            if (model != null && p.InLocation(CardLocation.Hand) && !inAnimation)
            {
                RefreshHandTurnByCode();
                RefreshHandShadowState();
            }
            RefreshLabel();
            UpdateExDeckTop();
        }

        public void SetCode(int code)
        {
            if (code > 0)
            {
                if (data.Id != code)
                {
                    SetData(CardsManager.Get(code));
                    data.Id = code;
                    if (p.controller == 1)
                        if (condition == OcgCore.Condition.Duel)
                            if (!sideReference.Main.Contains(code))
                                sideReference.Main.Add(code);
                }
            }
        }

        public void RefreshData()
        {
            CardsManager.Get(data.Id).CloneTo(data);
            SetData(data);
            ClearAllTails();
        }

        public void EraseData()
        {
            SetData(CardsManager.Get(0));
            Disabled = false;
            ClearAllTails();
        }

        public void AddTarget(GameCard card)
        {
            if (!targets.Contains(card))
                targets.Add(card);
        }

        public void RemoveTarget(GameCard card)
        {
            targets.Remove(card);
        }

        public void AddEffectTarget(GameCard card)
        {
            if (!effectTargets.Contains(card))
                effectTargets.Add(card);
        }

        public void RemoveEffectTarget(GameCard card)
        {
            effectTargets.Remove(card);
        }

        public static Vector3 GetCardPosition(GPS p)
        {
            var returnValue = Vector3.zero;

            if (p.InLocation(CardLocation.Search))
            {
                return new Vector3(0, -50, 0);
            }
            else if (p.InLocation(CardLocation.Unknown))
            {
                return new Vector3(0, 10, 0);
            }
            else if (p.InLocation(CardLocation.Hand))
            {
                int handsCount;

                if (p.InMyControl())
                    handsCount = Program.instance.ocgcore.GetMyHandCount();
                else
                    handsCount = Program.instance.ocgcore.GetOpHandCount();

                float x = p.sequence * 4 - (handsCount - 1) * 2;
                if (p.controller == 0)
                {
                    var z = GetMyHandBaseZ();
                    return new Vector3(x + handOffset * UIManager.ScreenLengthWithoutScalerX(0.038f), 15, z);
                }
                else
                {
                    var z = GetOpHandBaseZ();
                    return new Vector3(-x, 15, z);
                }
            }
            else if (p.InLocation(CardLocation.Deck))
            {
                if (p.controller == 0)
                    returnValue = new Vector3(26.6f, 1.5f, -23.5f);
                else
                    returnValue = new Vector3(-26.6f, 1.5f, 23.5f);
                returnValue.y += p.sequence * 0.11f;
            }
            else if (p.InLocation(CardLocation.Extra))
            {
                if (p.InMyControl())
                    returnValue = new Vector3(-26.6f, 1.5f, -23.5f);
                else
                    returnValue = new Vector3(26.6f, 1.5f, 23.5f);
                returnValue.y += p.sequence * 0.11f;
            }
            else if (p.InLocation(CardLocation.Grave))
            {
                var offset = (p.InMyControl() ? movingToMyGrave : movingToOpGrave) - 1;
                if (offset < 0)
                    offset = 0;
                if (p.InMyControl())
                    returnValue = new Vector3(25.74f - 3.75f * offset, 5f + 0.1f * offset, -14.26f);
                else
                    returnValue = new Vector3(-25.74f + 3.75f * offset, 5f + 0.1f * offset, 14.26f);
            }
            else if (p.InLocation(CardLocation.Removed))
            {
                var offset = (p.InMyControl() ? movingToMyExclude : movingToOpExclude) - 1;
                if (offset < 0)
                    offset = 0;
                if (p.controller == 0)
                    returnValue = new Vector3(25.74f + 1.842971f - 3.75f * offset, 5f + 0.1f * offset, -14.26f + 6.236011f);
                else
                    returnValue = new Vector3(-25.74f - 1.842971f + 3.75f * offset, 5f + 0.1f * offset, 14.26f - 6.236011f);
            }

            else if (p.InLocation(CardLocation.MonsterZone))
            {
                var realIndex = p.sequence;
                if (p.controller == 0)
                {
                    realIndex = p.sequence;
                    returnValue.y = 0.2f;
                    returnValue.z = -9.48f;
                }
                else
                {
                    if (realIndex <= 4)
                        realIndex = 4 - p.sequence;
                    else if (realIndex == 5)
                        realIndex = 6;
                    else if (realIndex == 6) realIndex = 5;
                    returnValue.y = 0.2f;
                    returnValue.z = 9.51f;
                }

                switch (realIndex)
                {
                    case 0:
                        returnValue.x = -17.2f;
                        break;
                    case 1:
                        returnValue.x = -8.6f;
                        break;
                    case 2:
                        returnValue.x = 0f;
                        break;
                    case 3:
                        returnValue.x = 8.6f;
                        break;
                    case 4:
                        returnValue.x = 17.2f;
                        break;
                    case 5:
                        returnValue.x = -8.6f;
                        returnValue.z = 0;
                        break;
                    case 6:
                        returnValue.x = 8.6f;
                        returnValue.z = 0;
                        break;
                }
            }
            else if (p.InLocation(CardLocation.SpellZone))
            {
                if (p.sequence < 5 || (p.sequence == 6 || p.sequence == 7) && OcgCore.MasterRule >= 4)
                {
                    var realIndex = p.sequence;
                    if (p.controller == 0)
                    {
                        realIndex = p.sequence;
                        returnValue.y = 0.2f;
                        returnValue.z = -18f;
                    }
                    else
                    {
                        if (realIndex <= 4)
                            realIndex = 4 - p.sequence;
                        else if (realIndex == 7)
                            realIndex = 6;
                        else if (realIndex == 6) realIndex = 7;
                        returnValue.y = 0.2f;
                        returnValue.z = 18f;
                    }

                    switch (realIndex)
                    {
                        case 0:
                            returnValue.x = -17.2f;
                            break;
                        case 1:
                            returnValue.x = -8.6f;
                            break;
                        case 2:
                            returnValue.x = 0f;
                            break;
                        case 3:
                            returnValue.x = 8.6f;
                            break;
                        case 4:
                            returnValue.x = 17.2f;
                            break;
                        case 6:
                            returnValue.x = -8.6f;
                            break;
                        case 7:
                            returnValue.x = 8.6f;
                            break;
                    }
                }

                if (p.sequence == 5)
                {
                    if (p.controller == 0)
                        returnValue = new Vector3(-25f, 0.1f, -10f);
                    else
                        returnValue = new Vector3(25f, 0.1f, 10f);
                }

                if (MasterRule <= 3)
                {
                    if (p.sequence == 6)
                    {
                        if (p.controller == 0)
                            returnValue = new Vector3(-30f, 10, -15f);
                        else
                            returnValue = new Vector3(30f, 10, 10f);
                    }

                    if (p.sequence == 7)
                    {
                        if (p.controller == 0)
                            returnValue = new Vector3(30f, 10, -15f);
                        else
                            returnValue = new Vector3(-30f, 10, 10f);
                    }
                }
            }

            if (p.InLocation(CardLocation.Overlay))
            {
                returnValue.y -= (p.position + 2) * 0.02f;
                returnValue.x += (p.position + 1) * 0.2f;
            }
            return returnValue;
        }

        private static float GetMyHandBaseZ()
        {
            return -28f + GetHandDepthOffsetByFov();
        }

        private static float GetOpHandBaseZ()
        {
            var baseZ = 17f;
            if (CurrentReplayGodView)
                baseZ += 2f;
            return baseZ - GetHandDepthOffsetByFov();
        }

        private static float GetHandDepthOffsetByFov()
        {
            var camera = Program.instance?.camera_?.cameraMain;
            if (camera == null)
                return 0f;

            var fovDelta = Mathf.Clamp(30f - camera.fieldOfView, 0f, 6f);
            var fovCompensation = fovDelta * 0.7f;

            // After widening duel camera framing, retract hand outward on ultrawide displays.
            var aspect = Tools.GetScreenAspectRatio();
            var ultraWide21x9T = Mathf.InverseLerp(16f / 9f, 21f / 9f, aspect);
            var ultraWide32x9T = Mathf.InverseLerp(21f / 9f, 32f / 9f, aspect);
            // Strong ultrawide push so hand cards sit lower while keeping a little more text visible.
            var ultraWideOutward = Mathf.Lerp(0f, 1.60f, ultraWide21x9T) + Mathf.Lerp(0f, 1.10f, ultraWide32x9T);

            return fovCompensation - ultraWideOutward;
        }

        public static Vector3 GetCardRotation(GPS p, int code = 0)
        {
            var condition = CardRuleCondition.MeUpAtk;
            if (p.InLocation(CardLocation.Deck))
            {
                if ((p.position & (uint)CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpDeck;
                else
                    condition = CardRuleCondition.MeDownDeck;
            }
            else if (p.InLocation(CardLocation.Extra))
            {
                if ((p.position & (uint)CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpExDeck;
                else
                    condition = CardRuleCondition.MeDownExDeck;
            }
            else if (p.InLocation(CardLocation.Grave))
            {
                if ((p.position & (uint)CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpGrave;
                else
                    condition = CardRuleCondition.MeDownGrave;
            }
            else if (p.InLocation(CardLocation.Removed))
            {
                if ((p.position & (uint)CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpRemoved;
                else
                    condition = CardRuleCondition.MeDownRemoved;
            }
            else if (p.InLocation(CardLocation.MonsterZone))
            {
                if ((p.position & (uint)CardPosition.FaceUp) > 0)
                {
                    if ((p.position & (uint)CardPosition.Attack) > 0)
                        condition = CardRuleCondition.MeUpAtk;
                    else
                        condition = CardRuleCondition.MeUpDef;
                }
                else
                {
                    if ((p.position & (uint)CardPosition.Attack) > 0)
                        condition = CardRuleCondition.MeDownAtk;
                    else
                        condition = CardRuleCondition.MeDownDef;

                }
            }
            else if (p.InLocation(CardLocation.SpellZone))
            {
                if ((p.position & (uint)CardPosition.FaceUp) > 0)
                    condition = CardRuleCondition.MeUpAtk;
                else
                    condition = CardRuleCondition.MeDownAtk;
            }
            else if (p.InLocation(CardLocation.Hand))
            {
                if (code != 0)
                    condition = CardRuleCondition.MeUpHand;
                else
                    condition = CardRuleCondition.MeDownHand;
            }

            if (p.InLocation(CardLocation.Overlay))
                condition = CardRuleCondition.MeUpAtk;

            if (p.controller != 0)
            {
                switch (condition)
                {
                    case CardRuleCondition.MeUpAtk:
                        condition = CardRuleCondition.OpUpAtk;
                        break;
                    case CardRuleCondition.MeDownAtk:
                        condition = CardRuleCondition.OpDownAtk;
                        break;
                    case CardRuleCondition.MeUpDef:
                        condition = CardRuleCondition.OpUpDef;
                        break;
                    case CardRuleCondition.MeDownDef:
                        condition = CardRuleCondition.OpDownDef;
                        break;
                    case CardRuleCondition.MeUpDeck:
                        condition = CardRuleCondition.OpUpDeck;
                        break;
                    case CardRuleCondition.MeDownDeck:
                        condition = CardRuleCondition.OpDownDeck;
                        break;
                    case CardRuleCondition.MeUpExDeck:
                        condition = CardRuleCondition.OpUpExDeck;
                        break;
                    case CardRuleCondition.MeDownExDeck:
                        condition = CardRuleCondition.OpDownExDeck;
                        break;
                    case CardRuleCondition.MeUpGrave:
                        condition = CardRuleCondition.OpUpGrave;
                        break;
                    case CardRuleCondition.MeDownGrave:
                        condition = CardRuleCondition.OpDownGrave;
                        break;
                    case CardRuleCondition.MeUpRemoved:
                        condition = CardRuleCondition.OpUpRemoved;
                        break;
                    case CardRuleCondition.MeDownRemoved:
                        condition = CardRuleCondition.OpDownRemoved;
                        break;
                    case CardRuleCondition.MeUpHand:
                        condition = CardRuleCondition.OpUpHand;
                        break;
                    case CardRuleCondition.MeDownHand:
                        condition = CardRuleCondition.OpDownHand;
                        break;
                }
            }

            return condition switch
            {
                CardRuleCondition.MeUpAtk => new Vector3(0, 0, 0),
                CardRuleCondition.MeUpDef => new Vector3(0, 270, 0),
                CardRuleCondition.MeDownAtk => new Vector3(0, 0, 180),
                CardRuleCondition.MeDownDef => new Vector3(0, 270, 180),
                CardRuleCondition.MeUpDeck => new Vector3(0, -19.5f, 0),
                CardRuleCondition.MeDownDeck => new Vector3(0, -19.5f, 180),
                CardRuleCondition.MeUpExDeck => new Vector3(0, 19.5f, 0),
                CardRuleCondition.MeDownExDeck => new Vector3(0, 19.5f, 180),
                CardRuleCondition.MeUpGrave => new Vector3(0, 0, 0),
                CardRuleCondition.MeDownGrave => new Vector3(0, 270, 0),
                CardRuleCondition.MeUpRemoved => new Vector3(0, 90, 0),
                CardRuleCondition.MeDownRemoved => new Vector3(0, 90, 180),
                CardRuleCondition.MeUpHand => new Vector3(-20, 0, 0),
                CardRuleCondition.MeDownHand => new Vector3(-20, 0, 180),
                CardRuleCondition.OpUpAtk => new Vector3(0, 180, 0),
                CardRuleCondition.OpUpDef => new Vector3(0, 90, 0),
                CardRuleCondition.OpDownAtk => new Vector3(0, 180, 180),
                CardRuleCondition.OpDownDef => new Vector3(0, 90, 180),
                CardRuleCondition.OpUpDeck => new Vector3(0, 160.5f, 0),
                CardRuleCondition.OpDownDeck => new Vector3(0, 160.5f, 180),
                CardRuleCondition.OpUpExDeck => new Vector3(0, 199.5f, 0),
                CardRuleCondition.OpDownExDeck => new Vector3(0, 199.5f, 180),
                CardRuleCondition.OpUpGrave => new Vector3(0, 180, 0),
                CardRuleCondition.OpDownGrave => new Vector3(0, 180, 180),
                CardRuleCondition.OpUpRemoved => new Vector3(0, 270, 0),
                CardRuleCondition.OpDownRemoved => new Vector3(0, 270, 180),
                CardRuleCondition.OpUpHand => new Vector3(20, 180, 0),
                CardRuleCondition.OpDownHand => new Vector3(20, 180, 180),
                _ => Vector3.zero,
            };
        }

        public static Vector3 GetEffectRotaion(GPS p)
        {
            if (p.InMyControl())
            {
                if (p.InPosition(CardPosition.Attack))
                    return new Vector3(0, 0, 0);
                else
                    return new Vector3(0, 270, 0);
            }
            else
            {
                if (p.InPosition(CardPosition.Attack))
                    return new Vector3(0, 180, 0);
                else
                    return new Vector3(0, 90, 0);
            }
        }

        public static Vector3 GetCardScale(GPS p)
        {
            if (p.InLocation(CardLocation.SpellZone))
                return new Vector3(0.8f, 1f, 0.8f);
            else if (p.InLocation(CardLocation.Deck, CardLocation.Extra))
                return new Vector3(0.9f, 1f, 0.9f);
            return Vector3.one;
        }

        private bool ThisLocationShouldHaveModel(GPS p)
        {
            if (p.InLocation(CardLocation.Hand))
                return true;
            else if (p.InLocation(CardLocation.Overlay))
                return false;
            else if (p.InLocation(CardLocation.Onfield))
                return true;
            else
                return false;
        }

        public bool InPendulumZone()
        {
            return InPendulumZoneIf(p, data.Id);
        }

        public static bool InPendulumZoneIf(GPS p, int code)
        {
            if (MasterRule == 8) return false;
            var data = CardsManager.Get(code);
            if ((p.location & (uint)CardLocation.SpellZone) == 0)
                return false;
            if (MasterRule > 3)
            {
                if (p.sequence != 0 && p.sequence != 4)
                    return false;
            }
            else if (p.sequence != 6 && p.sequence != 7)
                return false;
            if (!data.HasType(CardType.Pendulum))
                return false;
            if ((p.position & (uint)CardPosition.FaceDown) > 0)
                return false;
            return true;
        }

        public void UpdateExDeckTop()
        {
            var player = p.controller;
            if(cacheP != null)
                player = cacheP.controller;

            if (p.InLocation(CardLocation.Extra) ||
                (cacheP != null && cacheP.InLocation(CardLocation.Extra)))
                Program.instance.ocgcore.UpdateExDeckTop(player);
        }

        public async UniTask MoveAsync(GPS gps, bool rush = false, float wait = 0f, float overrideMoveTime = 0f)
        {

            #region Analyze

            lastMoveCard = this;

            //Reset State
            if (p.location != gps.location || gps.InPosition(CardPosition.FaceDown))
            {
                targets.Clear();
                equipedCard = null;
                foreach (var card in cards)
                    card.RemoveTarget(this);
                Disabled = false;
                setOverTurn = false;
                RefreshData();
            }

            overlays = Program.instance.ocgcore.GCS_GetOverlays(this);
            cacheP = p;
            p = gps;

            if (p.InLocation(CardLocation.Hand)
                && p.InMyControl())
                p.position = (int)CardPosition.FaceUpAttack;

            if (!SemiNomiSummoned
                && CardsManager.Get(data.Id).HasType(CardType.Monster)
                && (CardsManager.Get(data.Id).Type & 0x68020C0) > 0
                && p.InLocation(CardLocation.Grave, CardLocation.Removed))
                AddStringTail(InterString.Get("未正规登场"));
            else
                RemoveStringTail(InterString.Get("未正规登场"), true);
            //Debug.LogFormat("{0}: reason: {1:X} location: {2:X}", data.Name, p.reason, p.location);

            for (int i = 0; i < overlays.Count; i++)
            {
                overlays[i].overlayParent = this;
                overlays[i].p.controller = gps.controller;
                overlays[i].p.location = gps.location | (uint)CardLocation.Overlay;
                overlays[i].p.sequence = gps.sequence;
                overlays[i].p.position = i;
            }
            Program.instance.ocgcore.ArrangeCards();

            if (currentMessage == GameMessage.Move
                && cacheP.location != p.location
                && p.IsReason(CardReason.MATERIAL)
                && !cacheP.InLocation(CardLocation.Overlay))
                materialCards.Add(this);

            if (!ThisLocationShouldHaveModel(p) && cacheP.location == p.location)
                return;
            if (cacheP.InLocation(CardLocation.Overlay)
                && (p.IsReason(CardReason.RULE) || p.InLocation(CardLocation.Extra)))
                return;

            #endregion

            #region Pre

            float moveTime = 0.3f;

            if (rush)
            {
                if (ThisLocationShouldHaveModel(p) && model == null)
                {
                    CreateModel();
                    ModelAt(p);
                    ShowFaceDownCardOrNot(NeedShowFaceDownCard());
                    RefreshHandShadowState();
                    if (IsFaceDownOnSpellZone())
                        setOverTurn = true;
                    if (p.InLocation(CardLocation.Hand))
                    {
                        if (p.InMyControl())
                            needRefreshMyHand = true;
                        else
                            needRefreshOpHand = true;
                    }
                }
                return;
            }

            if (!ThisLocationShouldHaveModel(cacheP) && model != null)
            {
                Destroy(model, 1f);
                model = null;
            }
            if (model == null)
            {
                CreateModel();
                if (cacheP.InLocation(CardLocation.Deck))
                    cacheP.position = (int)CardPosition.FaceDownAttack;
                model.transform.SetParent(Program.instance.ocgcore.GetFieldTransform(p.controller));
                ModelAt(cacheP);
            }
            if ( nextMoveAction != null)
            {
                model.SetActive(false);
                var code = data.Id;
                if (code == 0)
                {
                    code = Program.instance.ocgcore.GetNextConfirmedCardCode();
                    SetCode(code);
                }
                nextMoveAction.Invoke(code);
                await UniTask.WaitForSeconds(nextMoveActionDuration);
                return;
            }

            inAnimation = true;
            needRefreshMyHand = true;
            needRefreshOpHand = true;

            string se = string.Empty;
            var sequence = DOTween.Sequence();
            float timePassed = 0f;

            if (p.InLocation(CardLocation.Grave, CardLocation.Removed))
            {
                if (p.InLocation(CardLocation.Grave))
                {
                    if (p.InMyControl())
                        movingToMyGrave++;
                    else
                        movingToOpGrave++;
                }
                else
                {
                    if (p.InMyControl())
                        movingToMyExclude++;
                    else
                        movingToOpExclude++;
                }
            }

            var position = GetCardPosition(p);
            var rotation = GetCardRotation(p, data.Id);

            bool handAppeal = false;
            float curveHeight = 20;
            var ease = Ease.Unset;
            if (overrideMoveTime > 0f)
            {
                moveTime = overrideMoveTime;
            }
            else
            {
                switch (currentMessage)
                {
                    case GameMessage.Draw:
                        curveHeight = 5f;
                        if (p.controller == 0)
                        {
                            moveTime = 0.5f;
                            handAppeal = true;
                        }
                        else
                            moveTime = 0.25f;
                        break;
                    case GameMessage.Move:
                        moveTime = 0.4f;
                        if (p.InLocation(CardLocation.Hand)
                            && p.InMyControl())
                        {
                            moveTime = 0.5f;
                            handAppeal = true;
                        }
                        if(cacheP != null 
                            && !cacheP.InLocation(CardLocation.Onfield)
                            && p.InLocation(CardLocation.Onfield))
                        {
                            curveHeight = 40f;
                        }
                        if (p.InLocation(CardLocation.Overlay))
                            curveHeight = 20f;
                        break;
                    case GameMessage.FlipSummoning:
                    case GameMessage.PosChange:
                    case GameMessage.ShuffleSetCard:
                    case GameMessage.Swap:
                        curveHeight = 10f;
                        moveTime = 0.2f;
                        break;
                }
            }

            #endregion

            #region Special Move SE and Effect

            //From Grave or Exclude
            if (cacheP.InLocation(CardLocation.Grave, CardLocation.Removed))
                timePassed += SequenceFromGrave(sequence, cacheP);
            //From Deck to Hand
            if (cacheP.InLocation(CardLocation.Deck)
                && p.InLocation(CardLocation.Hand))
                se = "SE_CARD_MOVE_0" + UnityEngine.Random.Range(1, 5);
            //Destroy
            if(p.IsReason(CardReason.DESTROY)
                && model != null
                && cacheP.InLocation(CardLocation.Onfield, CardLocation.Hand))
            {
                moveTime = 0.4f;
                se = "SE_CARDBREAK_01";
                if (!data.HasType(CardType.Token))
                {
                    var breakEffectPath = "fxp_cardbrk_bff_001";
                    var trail1Path = "fxp_grave_brksol_trail_001";
                    var trail2Path = "fxp_grave_ReCard_move_001";
                    if (p.InLocation(CardLocation.Removed))
                    {
                        breakEffectPath = "fxp_exclude_001";
                        trail1Path = "fxp_exclude_brksol_trail_001";
                        trail2Path = "fxp_exclude_ReCard_move_001";
                    }
                    var fx = ABLoader.LoadMasterDuelGameObject(breakEffectPath);
                    fx.transform.position = model.transform.position;
                    Destroy(fx, 3f);

                    manager.GetElement<Transform>("CardPlane").localScale = Vector3.zero;
                    var trail1 = ABLoader.LoadMasterDuelGameObject(trail1Path);
                    var trail2 = ABLoader.LoadMasterDuelGameObject(trail2Path);
                    trail1.transform.SetParent(model.transform, false);
                    trail2.transform.SetParent(model.transform, false);
                    Destroy(trail1, 3f);
                    Destroy(trail2, 3f);
                }
            }
            //Xyz Material
            if (cacheP.InLocation(CardLocation.MonsterZone)
                && p.InLocation(CardLocation.Overlay)
                && p.InLocation(CardLocation.Extra))
            {
                AudioManager.PlaySE(se);
                AudioManager.PlaySE("SE_SUMMON_XYZ_MATERIAL");
                var fx = ABLoader.LoadMasterDuelGameObject("XYZTrailFieldCard01");
                fx.transform.localPosition = model.transform.position;
                fx.transform.localEulerAngles = GetEffectRotaion(cacheP);
                var manager = fx.GetComponent<ElementObjectManager>();
                var dummyFace = manager.GetNestedElement<MeshRenderer>("DummyCard01/DummyCardModel_front");
                dummyFace.material = GetMaterial();
                if (cacheP.InPosition(CardPosition.Defence))
                    fx.transform.eulerAngles = new Vector3(0, 90, 0);
                Destroy(model);
                var director = fx.GetComponent<PlayableDirector>();
                _ = director.AutoDestroy();
                //TODO 送去墓地的特效
                await UniTask.WaitForSeconds(0.2f);
                return;
            }
            //Material
            if (ThisLocationShouldHaveModel(cacheP)
                && p.IsReason(CardReason.MATERIAL)
                && !p.InLocation(CardLocation.Onfield)
                && ((p.reason & ((uint)CardReason.Ritual) + (uint)CardReason.Fusion + (uint)CardReason.Synchro + (uint)CardReason.Link) > 0))
            {
                AudioManager.PlaySE(se);
                var se2 = string.Empty;
                var trail = string.Empty;
                if (p.IsReason(CardReason.Ritual))
                {
                    se2 = "SE_SUMMON_RITUAL_MATERIAL";
                    trail = "RitualTrailFieldCard01";
                }
                else if (p.IsReason(CardReason.Fusion))
                {
                    se2 = "SE_SUMMON_FUS_MATERIAL";
                    trail = "FusionTrailFieldCard01";
                }
                else if (p.IsReason(CardReason.Synchro))
                {
                    se2 = "SE_SUMMON_SYNC_MATERIAL";
                    if(GetData().HasType(CardType.Tuner))
                        trail = "Synchro00TrailFieldCard01";
                    else
                        trail = "Synchro01TrailFieldCard01";
                }
                else if (p.IsReason(CardReason.Link))
                {
                    se2 = "SE_SUMMON_LINK_MATERIAL";
                    trail = "LinkTrailFieldCard01";
                }

                AudioManager.PlaySE(se2);
                var fx = ABLoader.LoadMasterDuelGameObject(trail);
                fx.transform.localPosition = model.transform.position;
                fx.transform.localEulerAngles = GetEffectRotaion(cacheP);
                var manager = fx.GetComponent<ElementObjectManager>();
                var dummyFace = manager.GetNestedElement<MeshRenderer>("DummyCard01/DummyCardModel_front");
                dummyFace.material = GetMaterial();

                if (p.InLocation(CardLocation.Extra)
                    && !p.InLocation(CardLocation.Overlay)
                    && p.InPosition(CardPosition.FaceUp))
                    Program.instance.ocgcore.SetExDeckTop(this);
                var director = fx.GetComponent<PlayableDirector>();
                _ = director.AutoDestroy();
            }
            // Owner hand additions use normal hand movement. The legacy
            // CardCheatAppear cinematic assumes a scripted cutscene and can
            // leave a full-screen effect covering an ordinary network duel.
            // Query updates can replace the move reason before the animation
            // finishes. Network hand additions never need a cutscene, regardless
            // of that mutable reason (including repeated additions and replays).
            bool ownerHandAddition = cacheP.location == 0
                && p.InLocation(CardLocation.Hand) && !data.HasType(CardType.Token);
            if(ownerHandAddition)
                ModelAt(p);
            //from unknown
            if (cacheP.location == 0 && !ownerHandAddition)
            {
                //Token In
                if (data.HasType(CardType.Token))
                {
                    AudioManager.PlaySE("SE_CARD_TOKEN_SUMMON");
                    var fx = ABLoader.LoadMasterDuelGameObject("SummonToken01");
                    fx.transform.position = GetCardPosition(p);
                    fx.transform.localEulerAngles = GetEffectRotaion(p);
                    ModelAt(p);
                    model.SetActive(false);
                    var tokenManager = fx.GetComponent<ElementObjectManager>();
                    _ = Program.instance.texture_.LoadDummyCard(tokenManager.GetElement<ElementObjectManager>("DummyCard01"), data.Id, cacheP.controller);
                    _ = UniTask.WaitForSeconds(1.25f).ContinueWith(() => { model.SetActive(true); });
                    Destroy(fx, 2f);
                    await UniTask.WaitForSeconds(0.5f);
                    return;
                }
                //Card Cheat
                else
                {
                    model.SetActive(false);
                    var fx = await ABLoader.LoadFromFolderAsync<PlayableDirector>("MasterDuel/Timeline/Summon/CardCheat/CardCheatAppear", true, true);
                    fx.transform.position = new Vector3(0f, 48.6f, -17.7f);
                    fx.transform.localEulerAngles = new Vector3(-20f, 0f, 0f);
                    AudioManager.PlaySE("SE_CARDCHEAT_APPEAR");
                    model.transform.position = fx.transform.position;
                    model.transform.localEulerAngles = fx.transform.localEulerAngles;
                    sequence.Pause();
                    if (data.Id == 0)
                    {
                        var code = Program.instance.ocgcore.GetUpdateDataIdByGameCard(this);
                        SetCode(code);
                    }
                    var fxManager = fx.GetComponent<ElementObjectManager>();
                    _ = Program.instance.texture_.LoadDummyCard(
                        fxManager.GetElement<ElementObjectManager>("DummyCard01"), 
                        data.Id, cacheP.controller, false, 
                        fxManager.GetElement<Renderer>("DummyCardModel_front02"),
                        fxManager.GetElement<Renderer>("DummyCardModel_front03"));

                    overrideMoveTime = 0.4f;
                    await UniTask.WaitForSeconds(1.25f);

                    Destroy(fx);
                    model.SetActive(true);
                }
            }
            //Token Out (to unknow)
            if (p.location == 0)
            {
                AudioManager.PlaySE("SE_CARD_TOKEN_BREAK");
                var fx = ABLoader.LoadMasterDuelGameObject("fxp_bff_tokese_001");
                fx.transform.position = model.transform.position;
                fx.transform.localEulerAngles = GetEffectRotaion(p);
                Destroy(model);
                Destroy(fx, 3f);
                await UniTask.WaitForSeconds(0.2f);
                return;
            }
            //Release
            if ((p.reason & (uint)CardReason.RELEASE) > 0 && model != null)
            {
                se = "SE_SUMMON_ADVANCE";
                var fx = ABLoader.LoadMasterDuelGameObject("fxp_sacrifice_rls_001");
                fx.transform.position = model.transform.position;
                Destroy(fx, 5f);
            }
            //SpSummon
            if (p.IsReason(CardReason.SPSUMMON)
                && p.InLocation(CardLocation.MonsterZone)
                && !cacheP.InLocation(CardLocation.MonsterZone)
                && !p.InLocation(CardLocation.Overlay))
            {
                bool needSummonEffect = true;
                if (condition == OcgCore.Condition.Duel
                    && !Config.GetBool("DuelSummon", true))
                    needSummonEffect = false;
                if (condition == OcgCore.Condition.Watch
                    && !Config.GetBool("WatchSummon", true))
                    needSummonEffect = false;
                if (condition == OcgCore.Condition.Replay
                    && !Config.GetBool("ReplaySummon", true))
                    needSummonEffect = false;

                if (needSummonEffect
                    && materialCards.Count > 0
                    && (TypeMatchReason(data.Type, (int)materialCards[0].p.reason)
                    || TypeMatchReason(data.Type, materialCards[0].GetData().Reason)))
                {
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().CardDescription.Hide();
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().CardList.Hide();

                    AudioManager.PlaySE(se);
                    summonCard = this;
                    await TimelineHelper.PlaySummonTimelineAsync();
                    return;
                }
                else
                {
                    AudioManager.PlaySE(se);

                    bool cutin = CutinViewer.CutinExist(data.Id);
                    sequence.Pause();
                    if (cutin)
                    {
                        if (cacheP.InLocation(CardLocation.Grave, CardLocation.Removed))
                            model?.SetActive(false);
                        await CutinViewer.Play(data.Id, (int)p.controller);
                    }
                    if (data.IsHighLevel())
                        await SequenceStrongSummon(sequence, position, rotation, 0, timePassed).WaitAsync();
                    else
                        await SequenceNormalSummon(sequence, position, rotation, 0, timePassed).WaitAsync();
                    return;
                }
            }
            //Summon
            else if (p.InPosition(CardPosition.FaceUp)
                && p.InLocation(CardLocation.MonsterZone)
                && cacheP.InLocation(CardLocation.Hand)
                && !p.InLocation(CardLocation.Overlay))
            {
                AudioManager.PlaySE(se);
                sequence.Pause();
                bool cutin = CutinViewer.CutinExist(data.Id);
                if (cutin)
                    await CutinViewer.Play(data.Id, (int)p.controller);
                if (data.IsHighLevel())
                    await SequenceStrongSummon(sequence, position, rotation, 0, timePassed).WaitAsync();
                else
                    await SequenceNormalSummon(sequence, position, rotation, 0, timePassed).WaitAsync();
                return;
            }

            #endregion

            #region Normal Move

            var cardPlane = manager.GetElement<Transform>("CardPlane");
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var turn = manager.GetElement<Transform>("Turn");

            sequence.AppendInterval(wait);
            if (handAppeal)
            {
                ease = Ease.OutCubic;
            }

            sequence.Append(model.transform.DOPath(GenerateCurvePath(model.transform.position, position, curveHeight),
                moveTime, PathType.Linear, PathMode.Full3D, 10).OnStart(() =>
            {
                MoveStartAction();
            }));
            sequence.Join(model.transform.DOLocalRotate(Vector3.zero, moveTime));

            sequence.Join(pivot.DOScale(GetCardScale(p), moveTime * 0.95f));
            //Turn
            if (p.InLocation(CardLocation.Removed)
                || p.InLocation(CardLocation.Deck)
                || p.InLocation(CardLocation.Extra))
                sequence.Join(turn.DOLocalRotate(new Vector3(0, 0, rotation.z), moveTime * 0.6f));
            else
                sequence.Join(turn.DOLocalRotate(new Vector3(0, (rotation.y == 0) || (rotation.y == 180) ? 0 : 270, rotation.z), moveTime * 0.6f).SetEase(ease));
            if (handAppeal && overrideMoveTime == 0)
            {
                sequence.Join(turn.DOLocalMove(new Vector3(0, 0, 10), moveTime).SetEase(Ease.OutCubic).OnComplete(() =>
                {
                    turn.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.InCubic); // TODO: tween in tween
                }));
            }

            //CardPlane
            if (p.InLocation(CardLocation.Deck)
                || p.InLocation(CardLocation.Extra)
                || p.InLocation(CardLocation.Removed))
                sequence.Join(cardPlane.DOLocalRotate(new Vector3(rotation.x, rotation.y, 0), moveTime * 0.5f));
            else
                sequence.Join(cardPlane.DOLocalRotate(new Vector3(rotation.x, (rotation.y == 0 || rotation.y == 270) ? 0 : 180, 0), moveTime * 0.5f));

            //Pivot && Offset
            if (p.InLocation(CardLocation.Hand))
            {
                sequence.Join(pivot.DOLocalMove(new Vector3(0, 0, HandOffsetPositionByX(position.x)), moveTime / 4));
                sequence.Join(offset.DOLocalRotate(new Vector3(0, HandOffsetRotationByX(position.x), handAngle), moveTime / 4));
                handDefault = true;
            }
            else
            {
                sequence.Join(offset.DOLocalMove(Vector3.zero, moveTime / 4));
                sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.21f));
                sequence.Join(pivot.DOLocalMove(Vector3.zero, moveTime / 4));
                sequence.Join(pivot.DOLocalRotate(Vector3.zero, moveTime / 4));
            }

            //To Grave or Exclude
            if (p.InLocation(CardLocation.Grave, CardLocation.Removed))
            {
                if (p.InLocation(CardLocation.Grave))
                    timePassed += SequenceToGrave(sequence, p);
                else
                    timePassed += SequenceToExclude(sequence, p);
            }

            //Overlay Out
            if (cacheP.InLocation(CardLocation.Overlay)
                && (p.IsReason(CardReason.EFFECT) || !p.InLocation(CardLocation.Overlay))
                && !p.IsReason(CardReason.RULE))
            {
                se = "SE_CARD_XYZ_OUT";
                var fx = ABLoader.LoadMasterDuelGameObject("fxp_bff_overlay_out_001");
                fx.transform.position = GetCardPosition(cacheP);
                Destroy(fx, 3f);

                var trail = ABLoader.LoadMasterDuelGameObject("fxp_bff_overlay_trail_001");
                trail.transform.SetParent(model.transform, false);
            }

            //Overlay In
            if (!cacheP.InLocation(CardLocation.Overlay)
                && !p.InLocation(CardLocation.Extra)
                && p.InLocation(CardLocation.Overlay)
                && p.IsReason(CardReason.Xyz))
            {
                se = string.Empty;
                DOTween.To(v => { }, 0, 0, moveTime + timePassed).OnComplete(() =>
                {
                    AudioManager.PlaySE("SE_CARD_XYZ_IN");
                    var fx = ABLoader.LoadMasterDuelGameObject("fxp_bff_overlay_in_001");
                    fx.transform.position = GetCardPosition(p);
                    Destroy(fx, 3f);
                });

                var trail = ABLoader.LoadMasterDuelGameObject("fxp_bff_overlay_trail_001");
                trail.transform.SetParent(model.transform, false);
            }

            if (handAppeal)
            {
                sequence.AppendInterval(0.2f);
            }

            sequence.OnComplete(() =>
            {
                MoveEndAction();
            });

            AudioManager.PlaySE(se);

            sequence.Play();
            await sequence.WaitAsync();

            #endregion

        }

        private Vector3[] GenerateCurvePath(Vector3 start, Vector3 end, float curveHeight = 40f)
        {
            //curveHeight = Program.instance.testFloat;
            var endBias = 0.9f;
            Vector3 midPoint = start * (1 - endBias) + end * endBias;
            Vector3 controlPoint = midPoint + Vector3.up * curveHeight;

            Vector3[] path = new Vector3[10];
            for (int i = 0; i < 10; i++)
            {
                float rawT = i / 9.0f;
                float mappedT = Mathf.Pow(rawT, 0.5f);
                path[i] = CalculateBezierPoint(start, controlPoint, end, mappedT);
            }
            return path;
        }

        private Vector3 CalculateBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        private void MoveStartAction()
        {
            if (cacheP != null && cacheP.InLocation(CardLocation.Extra))
                UpdateExDeckTop();
            if ((cacheP != null && cacheP.InLocation(CardLocation.Hand))
                || p.InLocation(CardLocation.Hand))
                Program.instance.ocgcore.RefreshHandCardPosition();
            if (cacheP != null && cacheP.InLocation(CardLocation.Deck, CardLocation.Extra))
                Program.instance.ocgcore.DuelBGManager.ResizeDecks();
            if (cacheP != null && cacheP.InLocation(CardLocation.Grave, CardLocation.Removed))
                Program.instance.ocgcore.DuelBGManager.RefreshGravesState();
            if ((p.position & (uint)CardPosition.FaceDown) > 0
                || (p.location & (uint)CardLocation.MonsterZone) == 0)
                HideLabel();
        }

        private void MoveEndAction()
        {
            inAnimation = false;

            if (!ThisLocationShouldHaveModel(p) && model != null)
                Destroy(model);
            else
                manager.GetElement<Transform>("CardPlane").localScale = Vector3.one;

            if (p.InLocation(CardLocation.Extra)
                && !p.InLocation(CardLocation.Overlay)
                && p.InPosition(CardPosition.FaceUp))
                Program.instance.ocgcore.SetExDeckTop(this);

            ShowFaceDownCardOrNot(NeedShowFaceDownCard());
            RefreshHandTurnByCode();
            RefreshHandShadowState();

            if (p.InLocation(CardLocation.Deck, CardLocation.Extra))
                Program.instance.ocgcore.DuelBGManager.ResizeDecks();
            if (p.InLocation(CardLocation.Grave, CardLocation.Removed))
                Program.instance.ocgcore.DuelBGManager.RefreshGravesState();
        }

        private void RefreshHandTurnByCode()
        {
            if (model == null || !p.InLocation(CardLocation.Hand))
                return;
            manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 0, data.Id == 0 ? 180 : 0);
        }

        private void RefreshHandShadowState()
        {
            if (model == null)
                return;

            var mode = p.InLocation(CardLocation.Hand) && !p.InMyControl()
                ? ShadowCastingMode.Off
                : ShadowCastingMode.On;
            var cardModel = manager.GetElement<Transform>("CardModel");
            for (int i = 0; i < cardModel.childCount; i++)
            {
                var renderer = cardModel.GetChild(i).GetComponent<Renderer>();
                if (renderer != null)
                    renderer.shadowCastingMode = mode;
            }
        }

        public Sequence StartCardSequence(Vector3 fromPosition, Vector3 fromRotation, float interval = 0f)
        {
            if (model == null)
                return null;

            ResetModelPositon();
            model.transform.localPosition = fromPosition;
            model.transform.eulerAngles = fromRotation;
            var position = GetCardPosition(p);
            var rotaion = GetCardRotation(p);
            var sequence = DOTween.Sequence();
            if (data.IsHighLevel())
                return SequenceStrongSummon(sequence, position, rotaion, interval, 0f);
            else
                return SequenceNormalSummon(sequence, position, rotaion, interval, 0f);
        }

        private Sequence SequenceStrongSummon(Sequence sequence, Vector3 position, Vector3 angle, float interval, float timePassed)
        {
            var cardPlane = manager.GetElement<Transform>("CardPlane");
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var turn = manager.GetElement<Transform>("Turn");

            sequence.AppendInterval(interval);
            sequence.AppendCallback(() =>
            {
                UpdateExDeckTop();
            });

            var midP = position;
            midP.y = 15;

            sequence.Append(manager.transform.DOMove(midP, 0.4f).SetEase(Ease.InOutSine).OnStart(() =>
            {
                MoveStartAction();
            }));
            sequence.Join(manager.transform.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(cardPlane.DOLocalRotate(new Vector3(0, (angle.y == 0) || (angle.y == 270) ? 0 : 180, 0), 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOScale(GetCardScale(p), 0.16f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOLocalMove(Vector3.zero, 0.16f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOLocalRotate(Vector3.zero, 0.16f).SetEase(Ease.InOutSine));

            sequence.Join(offset.DOLocalMove(Vector3.zero, 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.4f).SetEase(Ease.InOutSine));
            sequence.Join(turn.DOLocalRotate(new Vector3(0, (angle.y == 0) || (angle.y == 180) ? 0 : 270, angle.z), 0.2f).SetEase(Ease.InOutSine));

            sequence.AppendInterval(0.26f);
            sequence.Insert(timePassed + interval + 0.26f, pivot.DOLocalMoveY(10, 0.4f).SetEase(Ease.InOutQuart));
            sequence.Insert(timePassed + interval + 0.26f, pivot.DOLocalRotate(new Vector3(-35, 0, 0), 0.4f).SetEase(Ease.InOutQuart));

            sequence.Append(pivot.DOLocalRotate(Vector3.zero, 0.14f).SetEase(Ease.InQuart));
            sequence.Join(pivot.DOLocalMoveY(0, 0.14f).SetEase(Ease.InQuart));
            sequence.Join(manager.transform.DOMove(position, 0.14f).SetEase(Ease.InQuart));

            sequence.OnComplete(() =>
            {
                MoveEndAction();
            });
            return sequence;
        }

        private Sequence SequenceNormalSummon(Sequence sequence, Vector3 position, Vector3 angle, float interval, float timePassed)
        {
            var cardPlane = manager.GetElement<Transform>("CardPlane");
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var turn = manager.GetElement<Transform>("Turn");

            var midP = position;
            midP.y = 10;

            sequence.AppendInterval(interval);
            sequence.Append(manager.transform.DOMove(midP, 0.3f).SetEase(Ease.InOutSine).OnStart(() =>
            {
                MoveStartAction();
            }));
            sequence.Join(manager.transform.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(cardPlane.DOLocalRotate(new Vector3(0, (angle.y == 0) || (angle.y == 270) ? 0 : 180, 0), 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOScale(GetCardScale(p), 0.16f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOLocalMove(Vector3.zero, 0.16f).SetEase(Ease.InOutSine));
            sequence.Join(pivot.DOLocalRotate(Vector3.zero, 0.16f).SetEase(Ease.InOutSine));

            sequence.Join(offset.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.3f).SetEase(Ease.InOutSine));
            sequence.Join(turn.DOLocalRotate(new Vector3(0, (angle.y == 0) || (angle.y == 180) ? 0 : 270, angle.z), 0.1f).SetEase(Ease.InOutSine));

            sequence.AppendInterval(0.16f);
            sequence.Insert(timePassed + interval + 0.16f, pivot.DOLocalMoveY(5, 0.3f).SetEase(Ease.InOutQuart));
            sequence.Insert(timePassed + interval + 0.16f, pivot.DOLocalRotate(new Vector3(-15, 0, 0), 0.3f).SetEase(Ease.InOutQuart));

            sequence.Append(pivot.DOLocalRotate(Vector3.zero, 0.14f).SetEase(Ease.InQuart));
            sequence.Join(pivot.DOLocalMoveY(0, 0.14f).SetEase(Ease.InQuart));
            sequence.Join(manager.transform.DOMove(position, 0.14f).SetEase(Ease.InQuart));

            sequence.OnComplete(() =>
            {
                MoveEndAction();
            });
            return sequence;
        }

        private float SequenceFromGrave(Sequence sequence, GPS p)
        {
            var dummy = ABLoader.LoadMasterDuelGameObject(p.InLocation(CardLocation.Grave) ? "DuelFromGrave01" : "DuelFromExclude01");
            dummy.transform.position = GetCardPosition(p);
            dummy.transform.eulerAngles = GetCardRotation(p);
            dummy.SetActive(false);
            var time = (35f / 60f);
            var dummyManager = dummy.GetComponent<ElementObjectManager>();
            _ = Program.instance.texture_.LoadDummyCard(dummyManager.GetElement<ElementObjectManager>("DummyCard01"), data.Id, p.controller);
            sequence.AppendCallback(() =>
            {
                model?.SetActive(false);
                dummy.SetActive(true);
                Program.instance.ocgcore.PlayGraveEffect(p, false);
            });
            sequence.AppendInterval(time);
            sequence.AppendCallback(() =>
            {
                model?.SetActive(true);
                Destroy(dummy);
            });

            return time;
        }

        private float SequenceToGrave(Sequence sequence, GPS p)
        {
            var count = p.InMyControl() ? movingToMyGrave : movingToOpGrave;
            if (count > 5)
            {
                Debug.Log("movingToGrave Over 5");
                count = 5;
            }
            if (count < 1)
            {
                Debug.Log("movingToGrave Less than 1");
                count = 1;
            }
            var dummy = ABLoader.LoadMasterDuelGameObject("DuelToGrave0" + count);
            dummy.transform.position = GetCardPosition(p);
            dummy.transform.eulerAngles = GetCardRotation(p);
            var time = (30f / 60f);
            dummy.SetActive(false);
            var dummyManager = dummy.GetComponent<ElementObjectManager>();
            _ = Program.instance.texture_.LoadDummyCard(dummyManager.GetElement<ElementObjectManager>("DummyCard01"), data.Id, p.controller);
            sequence.AppendCallback(() =>
            {
                Destroy(model);
                dummy.SetActive(true);
                if(!NextMessageIsMovingToGrave(p.controller))
                    Program.instance.ocgcore.PlayGraveEffect(p, false);
                if (p.InMyControl())
                    movingToMyGrave--;
                else
                    movingToOpGrave--;
            });
            sequence.AppendInterval(time);
            sequence.AppendCallback(() =>
            {
                Destroy(dummy);
            });

            return time;
        }

        private float SequenceToExclude(Sequence sequence, GPS p)
        {
            var count = p.InMyControl() ? movingToMyExclude : movingToOpExclude;
            if (count > 5)
            {
                Debug.Log("movingToExclude Over 5");
                count = 5;
            }
            if (count < 1)
            {
                Debug.Log("movingToExclude Less than 1");
                count = 1;
            }

            var dummy = ABLoader.LoadMasterDuelGameObject("DuelToExclude0" + count);
            dummy.transform.position = GetCardPosition(p);
            dummy.transform.eulerAngles = GetCardRotation(p);
            var time = (30f / 60f);
            dummy.SetActive(false);
            var dummyManager = dummy.GetComponent<ElementObjectManager>();
            _ = Program.instance.texture_.LoadDummyCard(dummyManager.GetElement<ElementObjectManager>("DummyCard01"), data.Id, p.controller);
            sequence.AppendCallback(() =>
            {
                Destroy(model);
                dummy.SetActive(true);
                if(!NextMessageIsMovingToExclude(p.controller))
                    Program.instance.ocgcore.PlayGraveEffect(p, false);
                if (p.InMyControl())
                    movingToMyExclude--;
                else
                    movingToOpExclude--;
            });
            sequence.AppendInterval(time);
            sequence.AppendCallback(() =>
            {
                Destroy(dummy);
            });

            return time;
        }

        private void ResetModelPositon()
        {
            if (model == null)
                return;
            model.transform.localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("CardPlane").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("CardPlane").localPosition = Vector3.zero;
            manager.GetElement<Transform>("Pivot").localScale = GetCardScale(p);
            manager.GetElement<Transform>("Pivot").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Pivot").localPosition = Vector3.zero;
            manager.GetElement<Transform>("Offset").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Offset").localPosition = Vector3.zero;
            manager.GetElement<Transform>("Turn").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Turn").localPosition = Vector3.zero;
        }

        public void ResetModelRotation()
        {
            if (model == null)
                return;
            manager.transform.localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("CardPlane").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Pivot").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Offset").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("Turn").localEulerAngles = Vector3.zero;
            manager.GetElement<Transform>("CardModel").localEulerAngles = Vector3.zero;
        }

        #region Animation

        public static float handAngle = -10f;
        private bool handDefault;
        private bool appealed = false;

        private void ModelAt(GPS gps, GameObject model = null)
        {
            ElementObjectManager manager;
            if (model == null)
            {
                model = this.model;
                manager = model.GetComponent<ElementObjectManager>();
            }
            else
                manager = this.manager;
            model.transform.localPosition = GetCardPosition(gps);
            Vector3 rotation = GetCardRotation(gps, data.Id);

            var cardPlane = manager.GetElement<Transform>("CardPlane");
            if ((gps.location & (uint)CardLocation.Deck) > 0
                || (gps.location & (uint)CardLocation.Extra) > 0
                || (gps.location & (uint)CardLocation.Removed) > 0)
                cardPlane.localEulerAngles = new Vector3(rotation.x, rotation.y, 0);
            else
                cardPlane.localEulerAngles = new Vector3(rotation.x, (rotation.y == 0 || rotation.y == 270) ? 0 : 180, 0);

            manager.GetElement<Transform>("Pivot").localScale = GetCardScale(p);

            if ((rotation.y == 90 || rotation.y == 270) && (gps.location & (uint)CardLocation.Removed) == 0)
                manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 270, rotation.z);
            else
                manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 0, rotation.z);
        }

        public void AnimationShuffle(float shuffleTime)
        {
            inAnimation = true;
            if (!p.InLocation(CardLocation.Hand))
                return;
            if (model != null)
            {
                manager.GetElement<Transform>("Pivot").DOLocalMoveZ(0, shuffleTime);
                manager.GetElement<Transform>("Offset").DOLocalRotate(Vector3.zero, shuffleTime);
                manager.GetElement<Transform>("Turn").DOLocalRotate(new Vector3(0, 0, 180), shuffleTime);

                model.transform.DOLocalMoveX(0, shuffleTime).OnComplete(() =>
                {
                    if (cards.Contains(this))
                        AnimationHandDefault(shuffleTime, true);
                    else
                        Dispose();// null for TagSwap
                });
            }
            else// null for TagSwap
            {
                DOTween.To(v => { }, 0, 0, shuffleTime).OnComplete(() =>
                {
                    CreateModel();
                    ModelAt(p);
                    manager.GetElement<Transform>("Pivot").localPosition = Vector3.zero;
                    manager.GetElement<Transform>("Offset").localEulerAngles = Vector3.zero;
                    manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 0, 180);
                    var position = model.transform.localPosition;
                    model.transform.localPosition = new Vector3(0, position.y, position.z);
                    AnimationHandDefault(shuffleTime, true);
                });
            }
        }

        public float HandOffsetRotationByX(float x)
        {
            var abs = x > 0 ? x : -x;
            return x * (abs * -0.006f + 1.2f) * ((p.controller == 0) ? 1 : -1);
        }

        public float HandOffsetPositionByX(float x)
        {
            var abs = x > 0 ? x : -x;
            return -abs * (abs * 0.0055f + 0.08f);
        }

        public void AnimationHandDefault(float time, bool ignore = false)
        {
            if (model == null || !p.InLocation(CardLocation.Hand) || (inAnimation && !ignore))
                return;
            model.transform.SetParent(null, true);
            handDefault = true;
            appealed = false;
            MoveToHandDefault(time);

            var targetPosition = GetCardPosition(p);
            var x = targetPosition.x;

            Transform pivot = manager.GetElement<Transform>("Pivot");
            Transform offset = manager.GetElement<Transform>("Offset");
            Transform turn = manager.GetElement<Transform>("Turn");
            pivot.DOLocalMove(new Vector3(0, 0, HandOffsetPositionByX(x)), time).OnComplete(() =>
            {
                if (ignore)
                    inAnimation = false;
            });
            offset.DOLocalMove(Vector3.zero, time);
            offset.DOLocalRotate(new Vector3(0, HandOffsetRotationByX(x), handAngle), time);
            if (data.Id == 0)
                turn.DOLocalRotate(new Vector3(0, 0, 180), time);
            else
                turn.DOLocalRotate(Vector3.zero, time);
        }

        private void MoveToHandDefault(float time)
        {
            var targetPosition = GetCardPosition(p);
            var x = targetPosition.x;
            if (HideMyHandCard && p.InMyControl())
                targetPosition.z = GetMyHandBaseZ();
            if (HideOpHandCard && !p.InMyControl())
                targetPosition.z = GetOpHandBaseZ();
            model.transform.DOLocalMove(targetPosition, time);
        }

        public void SetHandToDefault()
        {
            if (model == null || !p.InLocation(CardLocation.Hand) || inAnimation)
                return;

            clicked = false;
            handDefault = false;
        }

        public void SetHandDefault()
        {
            if (model == null || !p.InLocation(CardLocation.Hand))
                return;            
            appealed = false;
            model.transform.localPosition = GetCardPosition(p);
            float x = model.transform.localPosition.x;
            manager.GetElement<Transform>("Pivot").localPosition = new Vector3(0, 0, HandOffsetPositionByX(x));
            manager.GetElement<Transform>("Offset").localPosition = Vector3.zero;
            manager.GetElement<Transform>("Offset").localEulerAngles = new Vector3(0, HandOffsetRotationByX(x), handAngle);
            manager.GetElement<Transform>("Turn").localEulerAngles = new Vector3(0, 0, (data.Id == 0) ? 180 : 0);
        }

        private void AnimationHandHover()
        {
            if (inAnimation)
                return;
            var offset = manager.GetElement<Transform>("Offset");
            offset.DOLocalMove(new Vector3(0, 2, 1), 0.1f);
        }

        public void AnimationHandAppeal()
        {
            if (appealed || inAnimation)
                return;
            appealed = true;
            manager.GetElement<Transform>("Pivot").DOLocalMove(new Vector3(0, 2, 3), 0.1f);
            manager.GetElement<Transform>("Offset").DOLocalRotate(Vector3.zero, 0.1f);
            manager.GetElement<Transform>("Offset").DOLocalMove(Vector3.zero, 0.1f);
            AudioManager.PlaySE("SE_CARD_MOVE_0" + UnityEngine.Random.Range(1, 5));
        }

        public Sequence AnimationNegate()
        {
            nextNegateAction?.Invoke();
            nextNegateAction = null;
            AudioManager.PlaySE("SE_EFFECT_INVALID");

            CameraManager.BlackInOut(0f, 0.1f, 0.7f, 0.2f);
            ElementObjectManager manager;
            GameObject model;
            if (ThisLocationShouldHaveModel(p))
            {
                model = this.model;
                manager = this.manager;
            }
            else
            {
                model = CreateModel(false);
                ModelAt(p, model);
                manager = model.GetComponent<ElementObjectManager>();
            }

            var cardFace = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>();
            var originMono = cardFace.material.GetFloat("_Monochrome");
            Tools.ChangeLayer(model, "DuelOverlay3D");
            CameraManager.DuelOverlay3DPlus();
            manager.GetElement("EffectNegate").SetActive(false);
            manager.GetElement("EffectNegate").SetActive(true);
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var scale = pivot.localScale;

            bool additional = false;
            if (nextNegateAction_Additional != null)
            {
                nextNegateAction_Additional?.Invoke();
                nextNegateAction_AdditionalManager.transform.SetParent(offset, false);
                nextNegateAction_Additional = null;
                additional = true;
            }

            var showTime = 0.7f;
            if (additional)
                showTime += nextNegateAction_AdditionalTime;

            var sequence = DOTween.Sequence();
            if (p.InLocation(CardLocation.Deck, CardLocation.Extra, CardLocation.Onfield))
            {
                HideLabel();
                var height = 10f;
                if (p.InLocation(CardLocation.Deck, CardLocation.Extra))
                    height = 5f;
                sequence.Append(offset.DOLocalMoveY(height, 0.1f));
                sequence.Join(DOTween.To(() => originMono, x => cardFace.material.SetFloat("_Monochrome", x), 1, 0.1f));
                sequence.Join(pivot.DOScale(1f, 0.1f));
                sequence.AppendInterval(showTime);
                sequence.Append(offset.DOLocalMoveY(0f, 0.2f));
                if (Disabled)
                    originMono = 1f;
                if (originMono != 1f)
                    sequence.Join(DOTween.To(() => 1, x => cardFace.material.SetFloat("_Monochrome", x), originMono, 0.2f));
                sequence.Join(pivot.DOScale(scale, 0.2f));
                sequence.OnComplete(() =>
                {
                    Tools.ChangeLayer(model, "Default");
                    CameraManager.DuelOverlay3DMinus();
                    RefreshLabel();
                    if (!ThisLocationShouldHaveModel(p))
                        Destroy(model);
                });
            }
            else if ((p.location & (uint)CardLocation.Hand) > 0)
            {
                inAnimation = true;
                if (p.controller != 0)
                    manager.GetElement<Transform>("Turn").DOLocalRotate(Vector3.zero, 0.1f);
                var originRotaion = pivot.localEulerAngles;

                sequence.Append(offset.DOLocalMoveY(1, 0.1f));
                sequence.Join(offset.DOLocalMoveZ(5, 0.1f));
                sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.1f));
                sequence.Join(pivot.DOLocalRotate(Vector3.zero, 0.1f));
                sequence.Join(manager.GetElement<Transform>("Turn").DOLocalRotate(Vector3.zero, 0.1f));
                sequence.Join(DOTween.To(() => originMono, x => cardFace.material.SetFloat("_Monochrome", x), 1, 0.1f));
                sequence.Append(offset.DOLocalMoveY(1.2f, showTime));
                sequence.Join(offset.DOLocalMoveZ(5.5f, showTime));
                sequence.Append(offset.DOLocalMoveY(0f, 0.2f));
                sequence.Join(offset.DOLocalMoveZ(0f, 0.2f));
                sequence.Join(pivot.DOLocalRotate(originRotaion, 0.15f));
                sequence.Join(DOTween.To(() => 1, x => cardFace.material.SetFloat("_Monochrome", x), 0, 0.2f));
                sequence.Insert(0, pivot.DOScale(1.2f, 0.2f));
                sequence.Insert(showTime + 0.1f, pivot.DOScale(scale, 0.2f));
                sequence.OnComplete(() =>
                {
                    Tools.ChangeLayer(model, "Default");
                    CameraManager.DuelOverlay3DMinus();
                    inAnimation = false;
                    cardFace.material.SetFloat("_Monochrome", originMono);
                });
            }
            else if ((p.location & (uint)CardLocation.Grave) > 0
                || (p.location & (uint)CardLocation.Removed) > 0)
            {
                offset.localPosition = new Vector3(0, -2, 0);
                sequence.Append(offset.DOLocalMoveY(0, 0.1f));
                sequence.Join(DOTween.To(() => originMono, x => cardFace.material.SetFloat("_Monochrome", x), 1, 0.1f));
                sequence.Join(offset.DOScale(1f, 0.1f));
                sequence.AppendInterval(showTime);
                sequence.Append(offset.DOLocalMoveY(-2f, 0.2f));
                sequence.Join(DOTween.To(() => 1, x => cardFace.material.SetFloat("_Monochrome", x), 0, 0.2f));
                sequence.Join(offset.DOScale(Vector3.one * 0.5f, 0.2f));
                sequence.OnComplete(() =>
                {
                    Destroy(model);
                    CameraManager.DuelOverlay3DMinus();
                    cardFace.material.SetFloat("_Monochrome", originMono);
                });
            }

            return sequence;
        }

        public Sequence AnimationActivate()
        {
            AudioManager.PlaySE("SE_CARDVIEW_01");
            CameraManager.BlackInOut(0f, 0.3f, 0.4f, 0.3f);
            ElementObjectManager manager;
            GameObject model;
            if (ThisLocationShouldHaveModel(p))
            {
                model = this.model;
                manager = this.manager;
            }
            else
            {
                model = CreateModel(false);
                ModelAt(p, model);
                manager = model.GetComponent<ElementObjectManager>();
            }
            Tools.ChangeLayer(model, "DuelOverlay3D");
            CameraManager.DuelOverlay3DPlus();
            manager.GetElement("EffectBuffActive").SetActive(false);
            manager.GetElement("EffectBuffActive").SetActive(true);
            var pivot = manager.GetElement<Transform>("Pivot");
            var offset = manager.GetElement<Transform>("Offset");
            var scale = pivot.localScale;
            var sequence = DOTween.Sequence();

            var ease = Ease.OutCubic;

            if (p.InLocation(CardLocation.Deck, CardLocation.Extra, CardLocation.Onfield))
            {
                HideLabel();
                var height = 10f;
                if (p.InLocation(CardLocation.Deck, CardLocation.Extra))
                    height = 5f;
                sequence.Append(offset.DOLocalMoveY(height, 0.2f).SetEase(ease));
                sequence.Join(pivot.DOScale(1f, 0.2f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(height * 1.1f, 0.7f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(0f, 0.2f));
                sequence.Join(pivot.DOScale(scale, 0.2f));
                sequence.OnComplete(() =>
                {
                    Tools.ChangeLayer(model, "Default");
                    CameraManager.DuelOverlay3DMinus();
                    RefreshLabel();
                    if (!ThisLocationShouldHaveModel(p))
                        Destroy(model);
                });
            }
            else if ((p.location & (uint)CardLocation.Hand) > 0)
            {
                inAnimation = true;
                if (p.controller != 0)
                    manager.GetElement<Transform>("Turn").DOLocalRotate(Vector3.zero, 0.1f);
                var originRotaion = pivot.localEulerAngles;

                sequence.Append(offset.DOLocalMoveY(1, 0.2f).SetEase(ease));
                sequence.Join(offset.DOLocalMoveZ(5, 0.2f).SetEase(ease));
                sequence.Join(offset.DOLocalRotate(Vector3.zero, 0.2f).SetEase(ease));
                sequence.Join(pivot.DOLocalRotate(Vector3.zero, 0.2f).SetEase(ease));
                sequence.Join(manager.GetElement<Transform>("Turn").DOLocalRotate(Vector3.zero, 0.2f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(1.1f, 0.7f));
                sequence.Join(offset.DOLocalMoveZ(5.1f, 0.7f));
                sequence.Append(offset.DOLocalMoveY(0f, 0.2f));
                sequence.Join(offset.DOLocalMoveZ(0f, 0.2f));
                sequence.Join(pivot.DOLocalRotate(originRotaion, 0.2f));
                sequence.Insert(0, pivot.DOScale(1.1f, 0.15f).SetEase(ease));
                sequence.Insert(0.9f, pivot.DOScale(scale, 0.2f));
                sequence.OnComplete(() =>
                {
                    Tools.ChangeLayer(model, "Default");
                    CameraManager.DuelOverlay3DMinus();
                    inAnimation = false;
                });
            }
            else if ((p.location & (uint)CardLocation.Grave) > 0
                || (p.location & (uint)CardLocation.Removed) > 0)
            {
                offset.localPosition = new Vector3(0, -1, 0);
                offset.localScale = Vector3.one * 0.5f;
                sequence.Append(offset.DOLocalMoveY(5f, 0.2f).SetEase(ease));
                sequence.Join(offset.DOScale(1f, 0.2f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(5.5f, 0.7f).SetEase(ease));
                sequence.Append(offset.DOLocalMoveY(-1f, 0.2f));
                sequence.Join(offset.DOScale(0.5f, 0.2f));
                sequence.OnComplete(() =>
                {
                    Destroy(model);
                    CameraManager.DuelOverlay3DMinus();
                });
            }

            return sequence;
        }

        public Sequence AnimationConfirm(int id)
        {
            if (!ThisLocationShouldHaveModel(p))
            {
                CreateModel();
                ModelAt(p);
                model.SetActive(false);
            }
            inAnimation = true;
            var offset = manager.GetElement<Transform>("Offset");
            var offsetPosition = offset.localPosition;
            var turn = manager.GetElement<Transform>("Turn");
            var turnEulerAngles = turn.localEulerAngles;

            var sequence = DOTween.Sequence();
            if(id > 0)
                sequence.AppendInterval(id);

            var endPosition = new Vector3(0f, 2f, 3f);
            if (p.InLocation(CardLocation.Grave, CardLocation.Removed))
                endPosition.z = 0f;

            sequence.Append(offset.DOLocalMove(endPosition, 0.1f).OnStart(() => 
            {
                model.SetActive(true);
                ShowFaceDownCardOrNot(false);
                AudioManager.PlaySE("SE_CARDVIEW_02");
                if(Program.instance.ocgcore.GetAutoInfo())
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().CardDescription.Show(this, null);
            }));
            sequence.Join(turn.DOLocalRotate(Vector3.zero, 0.1f).OnComplete(() =>
            {
                var highlight = ABLoader.LoadMasterDuelGameObject("fxp_card_decide_001");
                highlight.transform.SetPositionAndRotation(offset.position, offset.rotation);
                highlight.transform.localScale = GetCardScale(p);
                Destroy(highlight, 1f);
            }));
            sequence.AppendInterval(0.8f);
            sequence.AppendCallback(() =>
            {
                inAnimation = false;
                if ((p.location & (uint)CardLocation.Hand) > 0)
                    SetHandToDefault();
                else
                {
                    offset.DOLocalMove(offsetPosition, 0.1f);
                    turn.DOLocalRotate(turnEulerAngles, 0.1f).OnComplete(() =>
                    {
                        if (!ThisLocationShouldHaveModel(p) && model != null)
                            Destroy(model);
                    });
                }
                ShowFaceDownCardOrNot(NeedShowFaceDownCard());
            });
            return sequence;
        }

        public void AnimationPositon(float delay = 0)
        {
            if (model == null)
                return;

            var positionManager = manager.GetElement<ElementObjectManager>("FieldCardChangeIcon");
            var sequence = DOTween.Sequence();
            sequence.AppendInterval(delay);
            if ((p.position & (uint)CardPosition.Attack) > 0)
            {
                AudioManager.PlaySE("SE_DISP_ATTACK", 0.6f);
                positionManager.GetElement<Transform>("Sword1").localEulerAngles = new Vector3(90, 0, 0);
                positionManager.GetElement<Transform>("Sword2").localEulerAngles = new Vector3(90, 0, 0);
                sequence.Append(positionManager.GetElement<Transform>("Sword1").DOLocalRotate(new Vector3(90, 0, 60), 0.2f).SetEase(Ease.OutCubic));
                sequence.Join(positionManager.GetElement<Transform>("Sword2").DOLocalRotate(new Vector3(90, 0, -60), 0.2f).SetEase(Ease.OutCubic));
                sequence.Join(positionManager.GetElement<SpriteRenderer>("Sword1").DOFade(1, 0.35f).SetEase(Ease.OutCubic));
                sequence.Join(positionManager.GetElement<SpriteRenderer>("Sword2").DOFade(1, 0.35f).SetEase(Ease.OutCubic));
                sequence.Insert(0.2f, positionManager.GetElement<Transform>("Sword1").DOLocalRotate(new Vector3(90, 0, 45), 0.15f).SetEase(Ease.OutCubic));
                sequence.Insert(0.2f, positionManager.GetElement<Transform>("Sword2").DOLocalRotate(new Vector3(90, 0, -45), 0.15f).SetEase(Ease.OutCubic));
                sequence.AppendInterval(0.2f);
                sequence.Append(positionManager.GetElement<SpriteRenderer>("Sword1").DOFade(0, 0.3f).SetEase(Ease.InQuad));
                sequence.Join(positionManager.GetElement<SpriteRenderer>("Sword2").DOFade(0, 0.3f).SetEase(Ease.InQuad));
            }
            else
            {
                AudioManager.PlaySE("SE_DISP_DEFENS", 0.6f);
                positionManager.GetElement<Transform>("Defense").localScale = new Vector3(3, 3, 3);
                sequence.Append(positionManager.GetElement<Transform>("Defense").DOScale(new Vector3(3.8f, 3.8f, 3.8f), 0.35f));
                sequence.Join(positionManager.GetElement<SpriteRenderer>("Defense").DOFade(1, 0.3f).SetEase(Ease.OutCubic));
                sequence.AppendInterval(0.2f);
                sequence.Append(positionManager.GetElement<SpriteRenderer>("Defense").DOFade(0, 0.3f).SetEase(Ease.InCubic));
            }
        }

        public void AnimationTarget()
        {
            AudioManager.PlaySE("SE_CEMETERY_CARD");

            GameObject model;
            if (ThisLocationShouldHaveModel(p))
                model = this.model;
            else
            {
                model = CreateModel(false);
                ModelAt(p, model);
                Destroy(model, 0.49f);
            }

            var fx = ABLoader.LoadMasterDuelGameObject("fxp_card_decide_001");
            fx.transform.position = model.transform.position;
            if (p.InLocation(CardLocation.MonsterZone) && p.InPosition(CardPosition.Defence))
                fx.transform.localEulerAngles = new Vector3(0, 90, 0);
            if (p.InLocation(CardLocation.Removed))
                fx.transform.localEulerAngles = new Vector3(0, 90, 0);
            if (p.InLocation(CardLocation.SpellZone))
                fx.transform.localScale = Vector3.one * 0.8f;
            if (p.InLocation(CardLocation.Deck, CardLocation.Extra))
                fx.transform.localEulerAngles = new Vector3(0, GetCardRotation(p).y, 0);
            Destroy(fx, 1f);
        }

        private readonly float[] bounceDutationsHuge = new float[] { 0.4f, 0.2f, 0.133f, 0.133f, 0.067f, 0.067f };
        private readonly float[] bounceDutationsSlight = new float[] { 0.2f, 0.133f, 0.133f, 0.067f };

        public void AnimationLandShake(GameCard card, int shakeLevel)
        {
            if(shakeLevel == 0)
                return;
            if (card == this)
                return;
            if (!p.InLocation(CardLocation.Onfield))
                return;
            if (model == null) // Overlays
                return;

            const float minCardsDistance = 8.6f;
            const float delayOffset = 0.01f;
            float distance = Vector3.Distance(card.model.transform.position, model.transform.position);
            float delay = Math.Max(0, distance - minCardsDistance) * delayOffset;
            Vector3 direction = (card.model.transform.position - model.transform.position).normalized;
            direction.y = 0;

            if (shakeLevel == 2)
            {
                int bounceCount = 6;
                float height = 5f;
                float angle = 50f;

                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(delay);
                model.transform.GetPositionAndRotation(out Vector3 originalPosition, out Quaternion originalRotation);

                for (int i = 0; i < bounceCount; i++)
                {
                    float decay = Mathf.Pow(0.7f, i);

                    float duration = bounceDutationsHuge[i];
                    float currentHeight = height * decay;
                    float currentAngle = angle * decay;

                    Vector3 bounceDirection = (i % 2 == 0) ? direction : -direction;
                    Vector3 axis = Vector3.Cross(bounceDirection, Vector3.up).normalized;
                    Quaternion rotation = Quaternion.AngleAxis(currentAngle, axis);

                    seq.Append(model.transform.DOMoveY(originalPosition.y + currentHeight, duration / 2)
                        .SetEase(Ease.OutQuad))
                        .Join(model.transform.DORotateQuaternion(rotation, duration / 2).SetEase(Ease.OutQuad));

                    seq.Append(model.transform.DOMoveY(originalPosition.y, duration / 2).SetEase(Ease.InQuad))
                        .Join(model.transform.DORotateQuaternion(originalRotation, duration / 2).SetEase(Ease.InQuad));
                }
            }
            else if(shakeLevel == 1)
            {
                int bounceCount = 4;
                float height = 3f;
                float angle = 10f;

                Sequence seq = DOTween.Sequence();
                seq.AppendInterval(delay);
                model.transform.GetPositionAndRotation(out Vector3 originalPosition, out Quaternion originalRotation);

                for (int i = 0; i < bounceCount; i++)
                {
                    float decay = Mathf.Pow(0.7f, i);

                    float duration = bounceDutationsSlight[i];
                    float currentHeight = height * decay;
                    float currentAngle = angle * decay;

                    Vector3 bounceDirection = (i % 2 == 0) ? direction : -direction;
                    Vector3 axis = Vector3.Cross(bounceDirection, Vector3.up).normalized;
                    Quaternion rotation = Quaternion.AngleAxis(currentAngle, axis);

                    seq.Append(model.transform.DOMoveY(originalPosition.y + currentHeight, duration / 2)
                        .SetEase(Ease.OutQuad))
                        .Join(model.transform.DORotateQuaternion(rotation, duration / 2).SetEase(Ease.OutQuad));

                    seq.Append(model.transform.DOMoveY(originalPosition.y, duration / 2).SetEase(Ease.InQuad))
                        .Join(model.transform.DORotateQuaternion(originalRotation, duration / 2).SetEase(Ease.InQuad));
                }
            }
        }

        public Sequence AnimationConfirmDeckTop(int id)
        {
            CreateModel();
            var pTop = p.Copy();
            pTop.sequence = (uint)Program.instance.ocgcore.GetLocationCardCount((CardLocation)p.location, p.controller) - 1;

            ModelAt(pTop);
            model.SetActive(false);
            var turn = manager.GetElement<Transform>("Turn");
            var sequence = DOTween.Sequence();
            sequence.AppendInterval(1f * id);
            sequence.Append(turn.DOLocalMoveY(2, 0.1f).OnStart(() =>
            {
                if (Program.instance.ocgcore.GetAutoInfo())
                    Program.instance.ocgcore.GetUI<OcgCoreUI>().CardDescription.Show(this, null);

                model.SetActive(true);
                if (Program.instance.ocgcore.GetLocationCardCount(CardLocation.Deck, p.controller) == 1)
                {
                    var deckModel = Program.instance.ocgcore.GetDeckModel(p.controller, CardLocation.Deck);
                    Program.instance.ocgcore.SetDeckModelActive(deckModel, false);
                }
            }));
            sequence.Join(turn.DOLocalRotate(Vector3.zero, 0.1f));
            sequence.Append(turn.DOLocalMoveY(0.1f * (id + 1), 0.1f).OnComplete(() =>
            {
                AudioManager.PlaySE("SE_CARDVIEW_02");
                var effect = ABLoader.LoadMasterDuelGameObject("fxp_card_decide_deck_001");
                effect.transform.SetPositionAndRotation(turn.position, turn.rotation);
                effect.transform.localScale = GetCardScale(p);
                Destroy(effect, 1f);
            }));
            sequence.AppendInterval(0.6f);
            sequence.Append(turn.DOLocalMoveY(2, 0.1f));
            sequence.Join(turn.DOLocalRotate(new Vector3(0, 0, 180), 0.1f));
            sequence.Append(turn.DOLocalMoveY(0f, 0.1f));
            sequence.OnComplete(() =>
            {
                Destroy(model);
                var deckModel = Program.instance.ocgcore.GetDeckModel(p.controller, CardLocation.Deck);
                Program.instance.ocgcore.SetDeckModelActive(deckModel, true);
            });

            return sequence;
        }

        #endregion

        #region Button

        public struct DuelButtonInfo
        {
            public List<int> response;
            public string hint;
            public ButtonType type;
        }

        private bool hightYellow = false;

        public List<DuelButtonInfo> buttons = new();
        private readonly List<DuelButton> buttonObjs = new();

        public void AddButton(int response, string hint, ButtonType type)
        {
            bool exist = false;
            foreach (var button in buttons)
                if (button.type == type)
                {
                    exist = true;
                    button.response.Add(response);
                }
            if (!exist)
                buttons.Add(new DuelButtonInfo() { response = new List<int>() { response }, hint = hint, type = type });
        }
        public void CreateButtons()
        {
            if (model == null || buttons.Count == 0)
                return;
            buttons.Sort((x, y) => x.type.CompareTo(y.type));
            for (int i = 0; i < buttons.Count; i++)
            {
                var obj = ABLoader.LoadMasterDuelGameObject("DuelButton");
                var mono = obj.GetComponent<DuelButton>();
                buttonObjs.Add(mono);
                mono.response = buttons[i].response;
                mono.hint = buttons[i].hint;
                mono.type = buttons[i].type;
                mono.id = i;
                mono.buttonsCount = buttons.Count;
                mono.cookieCard = this;
            }
            hightYellow = false;
            foreach (var button in buttons)
            {
                if (button.type == ButtonType.Activate
                    || button.type == ButtonType.PenSummon
                    || button.type == ButtonType.SetPendulum
                    || button.type == ButtonType.SpSummon
                    )
                { hightYellow = true; break; }
            }
            if (hightYellow)
                manager.GetElement("EffectHighlightYellow").SetActive(true);
            else
                manager.GetElement("EffectHighlightBlue").SetActive(true);

            var highlightParent = manager.GetElement("EffectHighlightBlue").transform.parent.gameObject;
            var t1 = manager.GetElement<Transform>("EffectHighlightYellow").GetChild(0);
            var t2 = manager.GetElement<Transform>("EffectHighlightBlue").GetChild(0);
            var t3 = manager.GetElement<Transform>("EffectHighlightYellowSelect").GetChild(0);
            var t4 = manager.GetElement<Transform>("EffectHighlightBlueSelect").GetChild(0);
            var needDown = p.InPosition(CardPosition.FaceUp);
            if(p.InLocation(CardLocation.Hand))
                needDown  = true;
            var position = new Vector3(0f, needDown ? -0.05f : 0.05f, 0f);
            t1.localPosition = position;
            t2.localPosition = position;
            t3.localPosition = position;
            t4.localPosition = position;
        }

        public void ClearButtons()
        {
            buttons.Clear();
            if (model == null)
                return;
            foreach (var button in buttonObjs)
                Destroy(button.gameObject);
            buttonObjs.Clear();
            manager.GetElement("EffectHighlightBlue").SetActive(false);
            manager.GetElement("EffectHighlightYellow").SetActive(false);
            manager.GetElement("EffectHighlightBlueSelect").SetActive(false);
            manager.GetElement("EffectHighlightYellowSelect").SetActive(false);
        }

        #endregion

        #region Label

        public bool labelShowing = false;
        private const string upColor = "<color=#00FFFF>";
        private const string upGrayColor = "<color=#009999>";
        private const string normalColor = "<color=#FFFFFF>";
        private const string normalGrayColor = "<color=#999999>";
        private const string downColor = "<color=#FF0000>";
        private const string downGrayColor = "<color=#990000>";
        private const string colorEndLabel = "</color>";
        private const string smallSize = "<size=75%>";
        private const string sizeEndLabel = "</size>";

        private int attack = 0;
        private int defense = 0;
        private float changeTime = 0.6f;
        private int lastAttribute;
        private int lastRace;
        private bool closeupShowing;
        private int setTurn = 0;
        public bool setOverTurn;

        public void RefreshLabel()
        {
            if(model == null) 
                return;
            if ((p.location & (uint)CardLocation.Onfield) == 0 || (p.position & (uint)CardPosition.FaceUp) == 0)
            {
                HideLabel();
                return;
            }

            Card origin = CardsManager.Get(data.Id);

            if ((p.location & (uint)CardLocation.MonsterZone) > 0)
            {
                //LinkMarker
                if (data.HasType(CardType.Link))
                {
                    manager.GetElement("LinkMarker0").SetActive((data.LinkMarker & (uint)CardLinkMarker.TopLeft) > 0);
                    manager.GetElement("LinkMarker1").SetActive((data.LinkMarker & (uint)CardLinkMarker.Top) > 0);
                    manager.GetElement("LinkMarker2").SetActive((data.LinkMarker & (uint)CardLinkMarker.TopRight) > 0);
                    manager.GetElement("LinkMarker3").SetActive((data.LinkMarker & (uint)CardLinkMarker.Left) > 0);
                    manager.GetElement("LinkMarker4").SetActive((data.LinkMarker & (uint)CardLinkMarker.Right) > 0);
                    manager.GetElement("LinkMarker5").SetActive((data.LinkMarker & (uint)CardLinkMarker.BottomLeft) > 0);
                    manager.GetElement("LinkMarker6").SetActive((data.LinkMarker & (uint)CardLinkMarker.Bottom) > 0);
                    manager.GetElement("LinkMarker7").SetActive((data.LinkMarker & (uint)CardLinkMarker.BottomRight) > 0);
                }
                else
                    for (int i = 0; i < 8; i++)
                        manager.GetElement("LinkMarker" + i).SetActive(false);

                //Overlay Material
                if (data.HasType(CardType.Xyz))
                {
                    manager.GetElement("MonsterMaterialsRoot").SetActive(true);
                    int overlayCounts = Program.instance.ocgcore.GCS_GetOverlays(this).Count;
                    manager.GetElement<TextMeshPro>("TextMonsterMaterials").text = overlayCounts.ToString();
                }
                else
                    manager.GetElement("MonsterMaterialsRoot").SetActive(false);
                //Attack/Defence
                manager.GetElement("CardAttackBody").SetActive(true);
                var text = manager.GetElement<TextMeshPro>("TextPowerPoint");
                if (!labelShowing)
                {
                    string atkDef = string.Empty;

                    if (data.HasType(CardType.Link))
                    {
                        if (data.Attack > data.rAttack)
                            atkDef = upColor + data.Attack.ToString() + colorEndLabel;
                        else if (data.Attack < data.rAttack)
                            atkDef = downColor + data.Attack.ToString() + colorEndLabel;
                        else
                            atkDef = normalColor + data.Attack.ToString() + colorEndLabel;
                    }
                    else
                    {
                        int rAtk = data.rAttack;
                        int rDef = data.rDefense;
                        if (rAtk < 0) rAtk = 0;
                        if (rDef < 0) rDef = 0;
                        if (data.Attack > rAtk)
                        {
                            if (p.InPosition(CardPosition.Attack))
                                atkDef += upColor;
                            else
                                atkDef += upGrayColor + smallSize;
                        }
                        else if (data.Attack < rAtk)
                        {
                            if (p.InPosition(CardPosition.Attack))
                                atkDef += downColor;
                            else
                                atkDef += downGrayColor + smallSize;
                        }
                        else
                        {
                            if (p.InPosition(CardPosition.Attack))
                                atkDef += normalColor;
                            else
                                atkDef += normalGrayColor + smallSize;
                        }
                        atkDef += data.Attack.ToString() + colorEndLabel;
                        if (!p.InPosition(CardPosition.Attack))
                            atkDef += sizeEndLabel;
                        atkDef += "/";
                        if (data.Defense > rDef)
                        {
                            if ((p.position & (uint)CardPosition.Attack) > 0)
                                atkDef += upGrayColor + smallSize;
                            else
                                atkDef += upColor;
                        }
                        else if (data.Defense < rDef)
                        {
                            if ((p.position & (uint)CardPosition.Attack) > 0)
                                atkDef += downGrayColor + smallSize;
                            else
                                atkDef += downColor;
                        }
                        else
                        {
                            if ((p.position & (uint)CardPosition.Attack) > 0)
                                atkDef += normalGrayColor + smallSize;
                            else
                                atkDef += normalColor;
                        }
                        atkDef += data.Defense.ToString() + colorEndLabel;
                    }
                    text.text = atkDef;
                }
                else
                {
                    if (data.Attack > attack)
                    {
                        AudioManager.PlaySE("SE_BUFF_ATTACK");
                        var buff = manager.GetElement("EffectBuff");
                        buff.SetActive(false);
                        buff.SetActive(true);
                    }
                    else if (data.Attack < attack)
                    {
                        AudioManager.PlaySE("SE_DEBUFF_ATTACK");
                        var buff = manager.GetElement("EffectDebuff");
                        buff.SetActive(false);
                        buff.SetActive(true);
                    }

                    if (data.HasType(CardType.Link))
                    {
                        var s1 = string.Empty;
                        if (data.Attack > data.rAttack)
                            s1 = upColor;
                        else if (data.Attack < data.rAttack)
                            s1 = downColor;
                        else
                            s1 = normalColor;

                        if (attack != data.Attack)
                        {
                            var originAttack = attack;
                            DOTween.To(() => originAttack, x =>
                            {
                                text.text = s1 + x + colorEndLabel;
                            }, data.Attack, changeTime);
                        }
                        else
                            text.text = s1 + data.Attack.ToString() + colorEndLabel;
                    }
                    else
                    {
                        int rAtk = data.rAttack;
                        int rDef = data.rDefense;
                        if (rAtk < 0) rAtk = 0;
                        if (rDef < 0) rDef = 0;
                        string s1 = string.Empty;

                        if (data.Attack > rAtk)
                        {
                            if (p.InPosition(CardPosition.Attack))
                                s1 += upColor;
                            else
                                s1 += upGrayColor + smallSize;
                        }
                        else if (data.Attack < rAtk)
                        {
                            if (p.InPosition(CardPosition.Attack))
                                s1 += downColor;
                            else
                                s1 += downGrayColor + smallSize;
                        }
                        else
                        {
                            if (p.InPosition(CardPosition.Attack))
                                s1 += normalColor;
                            else
                                s1 += normalGrayColor + smallSize;
                        }
                        string s2 = colorEndLabel;
                        if (!p.InPosition(CardPosition.Attack))
                            s2 += sizeEndLabel;
                        s2 += "/";
                        string s3 = string.Empty;
                        if (data.Defense > rDef)
                        {
                            if (p.InPosition(CardPosition.Attack))
                                s3 += upGrayColor + smallSize;
                            else
                                s3 += upColor;
                        }
                        else if (data.Defense < rDef)
                        {
                            if (p.InPosition(CardPosition.Attack))
                                s3 += downGrayColor + smallSize;
                            else
                                s3 += downColor;
                        }
                        else
                        {
                            if (p.InPosition(CardPosition.Attack))
                                s3 += normalGrayColor + smallSize;
                            else
                                s3 += normalColor;
                        }
                        string s4 = colorEndLabel;
                        if (p.InPosition(CardPosition.Attack))
                            s4 += sizeEndLabel;
                        var originAttack = attack;
                        var originDefense = defense;
                        if (data.Attack != attack && data.Defense == defense)
                        {
                            DOTween.To(() => originAttack, x =>
                            {
                                text.text = s1 + x + s2 + s3 + data.Defense + s4;
                            }, data.Attack, changeTime);
                        }
                        else if (data.Attack == attack && data.Defense != defense)
                        {
                            DOTween.To(() => originDefense, x =>
                            {
                                text.text = s1 + data.Attack + s2 + s3 + x + s4;
                            }, data.Defense, changeTime);
                        }
                        else if (data.Attack != attack && data.Defense != defense)
                        {
                            DOTween.To(() => originAttack, x =>
                            {
                                text.text = s1 + x + s2;
                            }, data.Attack, changeTime);
                            DOTween.To(() => originDefense, x =>
                            {
                                text.text += s3 + x + s4;
                            }, data.Defense, changeTime);
                        }
                        else
                            text.text = s1 + data.Attack + s2 + s3 + data.Defense + s4;
                    }
                }
                attack = data.Attack;
                defense = data.Defense;
                manager.GetElement("CardPendulumBody").SetActive(false);
                //Link Count & Level Count
                if (data.HasType(CardType.Link))
                {
                    manager.GetElement("LinkCount").SetActive(true);
                    manager.GetElement<TextMeshPro>("TextLinkCount").text = data.GetLinkCount().ToString();
                    manager.GetElement("CardLevel").SetActive(false);
                }
                else
                {
                    manager.GetElement("LinkCount").SetActive(false);
                    manager.GetElement("CardLevel").SetActive(true);
                    if (data.HasType(CardType.Xyz))
                        manager.GetElement<SpriteRenderer>("IconLevel").sprite = TextureManager.container.typeRank;
                    else
                        manager.GetElement<SpriteRenderer>("IconLevel").sprite = TextureManager.container.typeLevel;
                    string lv = "";
                    if (data.Level > origin.Level)
                        lv += upColor;
                    else if (data.Level < origin.Level)
                        lv += downColor;
                    else
                        lv += normalColor;
                    lv += data.Level.ToString() + "</color>";
                    manager.GetElement<TextMeshPro>("TextLevel").text = lv;
                }
                //Tuner
                if (data.HasType(CardType.Tuner))
                {
                    manager.GetElement("TunerIconRoot").SetActive(true);
                    if (origin.HasType(CardType.Tuner))
                        manager.GetElement("TunerIconOutline").SetActive(false);
                    else
                        manager.GetElement("TunerIconOutline").SetActive(true);
                }
                else
                    manager.GetElement("TunerIconRoot").SetActive(false);
                //Attribute
                if ((data.Attribute & (uint)CardAttribute.Light) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeLight;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 1, 0, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)CardAttribute.Dark) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeDark;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 0, 1, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)CardAttribute.Water) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeWater;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(0, 1, 1, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)CardAttribute.Fire) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeFire;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 0, 0, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)CardAttribute.Earth) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeEarth;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 0.2f, 0, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)CardAttribute.Wind) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeWind;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(0, 1, 0, 1) * closeupLineColorIntensity);
                }
                else if ((data.Attribute & (uint)CardAttribute.Divine) > 0)
                {
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeDivine;
                    manager.GetElement<MeshRenderer>("Closeup").material.SetColor("_Color", new Color(1, 1, 0, 1) * closeupLineColorIntensity);
                }
                if (data.Id > 0 && data.Attribute != origin.Attribute)
                {
                    manager.GetElement("IconAttributeChange").SetActive(true);
                    if (lastAttribute != data.Attribute)
                    {
                        lastAttribute = data.Attribute;
                        AudioManager.PlaySE("SE_BUFF_CHANGE");
                        manager.GetElement("EffectChange").SetActive(false);
                        manager.GetElement("EffectChange").SetActive(true);
                    }
                }
                else
                    manager.GetElement("IconAttributeChange").SetActive(false);
                manager.GetElement<SpriteRenderer>("MagicType").sprite = TextureManager.container.typeNone;
                manager.GetElement("MagicTypeChange").SetActive(false);
                //Race
                if ((data.Race & (uint)CardRace.Warrior) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceWarrior;
                else if ((data.Race & (uint)CardRace.SpellCaster) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceSpellCaster;
                else if ((data.Race & (uint)CardRace.Fairy) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceFairy;
                else if ((data.Race & (uint)CardRace.Fiend) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceFiend;
                else if ((data.Race & (uint)CardRace.Zombie) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceZombie;
                else if ((data.Race & (uint)CardRace.Machine) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceMachine;
                else if ((data.Race & (uint)CardRace.Aqua) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceAqua;
                else if ((data.Race & (uint)CardRace.Pyro) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.racePyro;
                else if ((data.Race & (uint)CardRace.Rock) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceRock;
                else if ((data.Race & (uint)CardRace.WindBeast) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceWindBeast;
                else if ((data.Race & (uint)CardRace.Plant) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.racePlant;
                else if ((data.Race & (uint)CardRace.Insect) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceInsect;
                else if ((data.Race & (uint)CardRace.Thunder) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceThunder;
                else if ((data.Race & (uint)CardRace.Dragon) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceDragon;
                else if ((data.Race & (uint)CardRace.Beast) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceBeast;
                else if ((data.Race & (uint)CardRace.BeastWarrior) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceBeastWarrior;
                else if ((data.Race & (uint)CardRace.Dinosaur) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceDinosaur;
                else if ((data.Race & (uint)CardRace.Fish) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceFish;
                else if ((data.Race & (uint)CardRace.SeaSerpent) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceSeaSerpent;
                else if ((data.Race & (uint)CardRace.Reptile) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceReptile;
                else if ((data.Race & (uint)CardRace.Psycho) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.racePsycho;
                else if ((data.Race & (uint)CardRace.DivineBeast) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceDivineBeast;
                else if ((data.Race & (uint)CardRace.CreatorGod) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceCreatorGod;
                else if ((data.Race & (uint)CardRace.Wyrm) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceWyrm;
                else if ((data.Race & (uint)CardRace.Cyberse) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceCyberse;
                else if ((data.Race & (uint)CardRace.Illustion) > 0)
                    manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.raceIllustion;
                if (data.Id > 0 && data.Race != origin.Race)
                {
                    manager.GetElement("IconTypeChange").SetActive(true);
                    if (lastRace != data.Race)
                    {
                        lastRace = data.Race;
                        AudioManager.PlaySE("SE_BUFF_CHANGE");
                        manager.GetElement("EffectChange").SetActive(false);
                        manager.GetElement("EffectChange").SetActive(true);
                    }
                }
                else
                    manager.GetElement("IconTypeChange").SetActive(false);
            }
            else
            {
                for (int i = 0; i < 8; i++)
                    manager.GetElement("LinkMarker" + i).SetActive(false);
                manager.GetElement("MonsterMaterialsRoot").SetActive(false);
                manager.GetElement("CardAttackBody").SetActive(false);
                int p1 = 0;
                int p2 = 4;
                if (MasterRule <= 3)
                {
                    p1 = 6;
                    p2 = 7;
                }

                //Pendulum Scale
                if ((p.location & (uint)CardLocation.PendulumZone) > 0 ||
                    (data.HasType(CardType.Pendulum)
                    && (p.location & (uint)CardLocation.SpellZone) > 0
                    && !data.HasType(CardType.Equip)
                    && !data.HasType(CardType.Continuous)
                    && !data.HasType(CardType.Trap))
                    && (p.sequence == p1 || p.sequence == p2))
                {
                    manager.GetElement("CardPendulumBody").SetActive(true);
                    string pendulum = "";
                    if (p.sequence == p1)
                    {
                        manager.GetElement("PendulumLeft").SetActive(true);
                        manager.GetElement("PendulumRight").SetActive(false);
                        if (data.LScale > origin.LScale)
                            pendulum += upColor;
                        else if (data.LScale < origin.LScale)
                            pendulum += downColor;
                        else
                            pendulum += normalColor;
                        pendulum += data.LScale.ToString() + "</color>";
                        manager.GetElement<TextMeshPro>("TextPendulumLeft").text = pendulum;
                    }
                    else
                    {
                        manager.GetElement("PendulumLeft").SetActive(false);
                        manager.GetElement("PendulumRight").SetActive(true);
                        if (data.RScale > origin.RScale)
                            pendulum += upColor;
                        else if (data.RScale < origin.RScale)
                            pendulum += downColor;
                        else
                            pendulum += normalColor;
                        pendulum += data.RScale.ToString() + "</color>";
                        manager.GetElement<TextMeshPro>("TextPendulumRight").text = pendulum;
                    }
                }
                else
                    manager.GetElement("CardPendulumBody").SetActive(false);
                manager.GetElement("LinkCount").SetActive(false);
                manager.GetElement("CardLevel").SetActive(false);
                manager.GetElement("TunerIconRoot").SetActive(false);
                //Attribute
                if (data.HasType(CardType.Spell))
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeSpell;
                else
                    manager.GetElement<SpriteRenderer>("IconAttribute").sprite = TextureManager.container.attributeTrap;
                manager.GetElement("IconAttributeChange").SetActive(false);

                //Magic Trap Type
                if (data.HasType(CardType.Counter))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = TextureManager.container.typeCounter;
                else if (data.HasType(CardType.Field))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = TextureManager.container.typeField;
                else if (data.HasType(CardType.Equip))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = TextureManager.container.typeEquip;
                else if (data.HasType(CardType.Continuous))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = TextureManager.container.typeContinuous;
                else if (data.HasType(CardType.QuickPlay))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = TextureManager.container.typeQuickPlay;
                else if (data.HasType(CardType.Ritual) && !origin.HasType(CardType.Monster))
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = TextureManager.container.typeRitual;
                else
                    manager.GetElement<SpriteRenderer>("MagicType").sprite = TextureManager.container.typeNone;
                manager.GetElement("MagicTypeChange").SetActive(false);
                manager.GetElement<SpriteRenderer>("IconType").sprite = TextureManager.container.typeNone;
                manager.GetElement("IconTypeChange").SetActive(false);
            }

            if (cardCounters.Count > 0)
            {
                int counter = 0;
                int count = 0;
                foreach (var cc in cardCounters)
                {
                    counter = cc.Key;
                    count = cc.Value;
                    break;
                }
                manager.GetElement<SpriteRenderer>("IconCounter").sprite = TextureManager.GetCardCounterIcon(counter);
                manager.GetElement<TextMeshPro>("TextCounter").text = count.ToString();
            }
            else
            {
                manager.GetElement<SpriteRenderer>("IconCounter").sprite = TextureManager.container.typeNone;
                manager.GetElement<TextMeshPro>("TextCounter").text = string.Empty;
            }

            manager.GetElement<Transform>("DefaultShow").localEulerAngles = new Vector3(0f, 0f, 0f);
            manager.GetElement<Transform>("DefaultHide").localEulerAngles = new Vector3(0f, 0f, 0f);
            manager.GetElement<Transform>("CloseupOffset").localEulerAngles = new Vector3(0f, 0f, 0f);

            if (p.controller == 0)
            {
                manager.GetElement<Transform>("MonsterMaterialsRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardAttackBody").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardPendulumBody").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("LinkCount").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardLevel").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("TunerIconRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardAttribute").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("MagicTypeBase").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardType").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("CardCounter").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("StatusIcon").localEulerAngles = new Vector3(0f, 0f, 0f);
                manager.GetElement<Transform>("LinkMarkerRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
            }
            else
            {
                if (CloseupConfig() && (p.location & (uint)CardLocation.MonsterZone) > 0)
                {
                    manager.GetElement<Transform>("DefaultShow").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("DefaultHide").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CloseupOffset").localEulerAngles = new Vector3(0f, 180f, 0f);

                    manager.GetElement<Transform>("MonsterMaterialsRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardAttackBody").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardPendulumBody").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("LinkCount").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardLevel").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("TunerIconRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardAttribute").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("MagicTypeBase").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardType").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("CardCounter").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("StatusIcon").localEulerAngles = new Vector3(0f, 0f, 0f);
                    manager.GetElement<Transform>("LinkMarkerRoot").localEulerAngles = new Vector3(0f, 180f, 0f);
                }
                else
                {
                    manager.GetElement<Transform>("MonsterMaterialsRoot").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardAttackBody").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardPendulumBody").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("LinkCount").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardLevel").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("TunerIconRoot").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardAttribute").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("MagicTypeBase").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardType").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("CardCounter").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("StatusIcon").localEulerAngles = new Vector3(0f, 180f, 0f);
                    manager.GetElement<Transform>("LinkMarkerRoot").localEulerAngles = new Vector3(0f, 0f, 0f);
                }
            }

            ShowLabel();
        }

        public void ShowLabel()
        {
            labelShowing = true;
            Transform labelRoot = manager.GetElement<Transform>("StatusLabelRoot");
            labelRoot.gameObject.SetActive(true);
            labelRoot.DOScale(1, 0.2f).SetEase(Ease.InCubic);
            if (NeedShowCloseup())
            {
                var renderer = manager.GetElement<MeshRenderer>("Closeup");
                if(renderer.material.mainTexture == null)
                    closeupShowing = false;
                if (!closeupShowing)
                {
                    closeupShowing = true;
                    _ = Program.instance.texture_.LoadCloseupAsync(data.Id, renderer);
                }
            }
            else
            {
                closeupShowing = false;
                manager.GetElement("Closeup").SetActive(false);
            }

            HideHiddenLabel();
        }

        public void HideLabel()
        {
            labelShowing = false;
            Transform labelRoot = manager.GetElement<Transform>("StatusLabelRoot");
            labelRoot.DOScale(0, 0.2f).SetEase(Ease.OutCubic).OnComplete(() => labelRoot.gameObject.SetActive(false));
            manager.GetElement("Closeup").SetActive(false);
            closeupShowing = false;
        }

        public void ShowHiddenLabel()
        {
            if (model == null)
                return;
            manager.GetElement("CardAttribute").SetActive(true);
            manager.GetElement("MagicTypeBase").SetActive(true);
            manager.GetElement("CardType").SetActive(true);

            Transform linkMarker = manager.GetElement<Transform>("LinkMarkerRoot");
            linkMarker.DOScale(1.7f, 0.2f).SetEase(Ease.InOutCubic);
            foreach (var sr in linkMarker.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingLayerName = "CardStatus";
        }

        public void HideHiddenLabel()
        {
            if (model == null || !labelShowing)
                return;
            if (!manager.GetElement("IconAttributeChange").activeSelf)
                manager.GetElement("CardAttribute").SetActive(false);
            else
                manager.GetElement("CardAttribute").SetActive(true);
            if ((p.location & (uint)CardLocation.SpellZone) == 0
                || !manager.GetElement("MagicTypeChange").activeSelf)
                manager.GetElement("MagicTypeBase").SetActive(false);
            else
                manager.GetElement("MagicTypeBase").SetActive(true);
            if ((p.location & (uint)CardLocation.MonsterZone) == 0
                || !manager.GetElement("IconTypeChange").activeSelf)
                manager.GetElement("CardType").SetActive(false);
            else
                manager.GetElement("CardType").SetActive(true);

            Transform linkMarker = manager.GetElement<Transform>("LinkMarkerRoot");
            linkMarker.DOScale(1f, 0.2f).SetEase(Ease.InOutCubic);
            foreach (var sr in linkMarker.GetComponentsInChildren<SpriteRenderer>(true))
                sr.sortingLayerName = "Default";
        }

        private void SetDisabled()
        {
            if (model == null)
                return;
            var cardFace = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>();
            if ((p.location & (uint)CardLocation.Onfield) == 0)
                m_disabled = false;
            if ((p.position & (uint)CardPosition.FaceDown) > 0)
                m_disabled = false;

            if (Disabled)
            {
                cardFace.material.SetFloat("_Monochrome", 1);
                manager.GetElement<Renderer>("Closeup").material.SetVector("RGBA", Vector4.zero);
                manager.GetElement<Renderer>("Closeup").material.SetFloat("Mono", 1);
            }
            else
            {
                cardFace.material.SetFloat("_Monochrome", 0);
                manager.GetElement<Renderer>("Closeup").material.SetVector("RGBA", Vector4.one);
                manager.GetElement<Renderer>("Closeup").material.SetFloat("Mono", 0);
            }
        }

        private bool NeedShowCloseup()
        {
            if (model == null)
                return false;
            if(!CloseupConfig())
                return false;
            if((p.location & (uint)CardLocation.MonsterZone) == 0)
                return false;
            if ((p.position & (uint)CardPosition.FaceDown) > 0)
                return false;
            if(!File.Exists(Program.PATH_CLOSEUP + data.Id + Program.EXPANSION_PNG))
                return false;
            return true;
        }

        private bool CloseupConfig()
        {
            if (condition == OcgCore.Condition.Duel && !Config.GetBool("DuelCloseup", true))
                return false;
            if (condition == OcgCore.Condition.Watch && !Config.GetBool("WatchCloseup", true))
                return false;
            if (condition == OcgCore.Condition.Replay && !Config.GetBool("ReplayCloseup", true))
                return false;
            return true;
        }

        public bool NeedShowFaceDownCard()
        {
            if(data.Id == 0)
                return false;
            if((p.position & (uint)CardPosition.FaceUp) > 0)
                return false;
            if((p.location & (uint)CardLocation.Onfield) == 0)
                return false;

            return true;
        }

        public bool IsFaceDownOnSpellZone()
        {
            if ((p.position & (uint)CardPosition.FaceUp) > 0)
                return false;
            if ((p.location & (uint)CardLocation.SpellZone) == 0)
                return false;

            return true;
        }

        public void ShowFaceDownCardOrNot(bool show)
        {
            if (model == null)
                return;
            if (IsFaceDownOnSpellZone())
                setTurn = OcgCore.turns;
            else
                setTurn = 0;

            var back = manager.GetElement<Transform>("CardModel").GetChild(0).GetComponent<Renderer>();
            var face = manager.GetElement<Transform>("CardModel").GetChild(1).GetComponent<Renderer>();

            if (condition == OcgCore.Condition.Duel && !Config.GetBool("DuelFaceDown", true))
                show = false;
            if (condition == OcgCore.Condition.Watch && !Config.GetBool("WatchFaceDown", true))
                show = false;
            if (condition == OcgCore.Condition.Replay && !Config.GetBool("ReplayFaceDown", true))
                show = false;

            if (show)
            {
                // Some bundled card models have no face-preview animator. This
                // cosmetic feature must not abort the authoritative card move.
                face.GetComponent<Animator>()?.SetBool("Show", true);
                face.transform.localEulerAngles = new Vector3(180f, 0f, 0f);
                face.material.SetTexture("_LoadingTex", back.material.mainTexture);
                back.gameObject.SetActive(false);
            }
            else
            {
                back.gameObject.SetActive(true);
                face.GetComponent<Animator>()?.SetBool("Show", false);
                face.transform.localEulerAngles = new Vector3(180f, 0f, -180f);
                manager.GetElement("EffectDisquiet")?.SetActive(false);
            }
        }

        public void ShowDisquiet()
        {
            if (model == null)
                return;
            if (setTurn == 0)
                return;
            if (setTurn >= turns)
                return;
            if ((p.location & (uint)CardLocation.SpellZone) == 0)
                return;
            if ((p.position & (uint)CardPosition.FaceDown) == 0)
                return;

            manager.GetElement("EffectDisquiet").SetActive(true);
            setOverTurn = true;
        }

        #endregion

        #region CardCounter

        Dictionary<int, int> cardCounters = new Dictionary<int, int>();

        public void AddCounter(int counter, int count)
        {
            AudioManager.PlaySE("SE_CARD_COUNTER");

            int fullCount = count;

            bool haveCounter = cardCounters.Any(cc => cc.Key == counter);
            if (haveCounter)
            {
                fullCount += cardCounters[counter];
                cardCounters.Remove(counter);
            }
            cardCounters.Add(counter, fullCount);

            var counterName = StringHelper.Get("counter", counter);
            for (int i = 0; i < count; i++)
                AddStringTail(counterName);
            RefreshLabel();
        }

        public void RemoveCounter(int counter, int count)
        {
            AudioManager.PlaySE("SE_CARD_COUNTER");
            var fullCount = cardCounters[counter] - count;
            cardCounters.Remove(counter);
            if (fullCount > 0)
                cardCounters.Add(counter, fullCount);

            var counterName = StringHelper.Get("counter", counter);
            for (int i = 0; i < count; i++)
                RemoveStringTail(counterName);
            RefreshLabel();
        }

        public void ClearCounter()
        {
            cardCounters.Clear();
        }

        public int GetCounterCount(int type)
        {
            cardCounters.TryGetValue(type, out var count);
            return count;
        }

        #endregion

        #region String Tail

        public MultiStringMaster tails = new();

        public void AddStringTail(string tail)
        {
            tails.Add(tail);
        }

        public void RemoveStringTail(string tail, bool all = false)
        {
            tails.Remove(tail, all);
        }

        public void ClearAllTails()
        {
            ClearCounter();
            tails.Clear();
        }

        #endregion

        #region Chain

        public class Chain
        {
            public int i;
            public DuelChainSpot chainSpot;
        }

        public List<Chain> chains = new();

        public void AddChain(int i)
        {
            var obj = ABLoader.LoadMasterDuelGameObject("ChainSpot");
            Program.instance.ocgcore.allGameObjects.Add(obj);
            chains.Add(new Chain() { i = i, chainSpot = obj.GetComponent<DuelChainSpot>() });
            chains[^1].chainSpot.Play(i, p, model != null);
        }

        public void ResolveChain(int i)
        {
            foreach (var chain in chains)
            {
                if (chain.i == i)
                {
                    chain.chainSpot.OnChainResolveBegin();
                    break;
                }
            }
        }

        public void RemoveChain(int i)
        {
            foreach (var chain in chains)
            {
                if (chain.i == i)
                {
                    chain.chainSpot.OnChainResolveEnd();
                    Destroy(chain.chainSpot.gameObject, 1f);
                    chains.Remove(chain);
                    break;
                }
            }
        }

        public void RemoveAllChain()
        {
            foreach (var chain in chains)
                Destroy(chain.chainSpot.gameObject, 1f);
            chains.Clear();
        }

        #endregion

        #region enum

        public enum Condition
        {
            None,
            Chaining,
            Selected
        }

        private enum CardRuleCondition
        {
            MeUpAtk,
            MeUpDef,
            MeDownAtk,
            MeDownDef,
            OpUpAtk,
            OpUpDef,
            OpDownAtk,
            OpDownDef,
            MeUpDeck,
            MeDownDeck,
            OpUpDeck,
            OpDownDeck,
            MeUpExDeck,
            MeDownExDeck,
            OpUpExDeck,
            OpDownExDeck,
            MeUpGrave,
            MeDownGrave,
            OpUpGrave,
            OpDownGrave,
            MeUpRemoved,
            MeDownRemoved,
            OpUpRemoved,
            OpDownRemoved,
            MeUpHand,
            MeDownHand,
            OpUpHand,
            OpDownHand
        }

        #endregion

    }

}
