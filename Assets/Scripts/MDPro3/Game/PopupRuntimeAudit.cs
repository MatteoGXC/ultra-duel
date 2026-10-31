using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace MDPro3
{
    // Runs only in the isolated --format-smoke local duel. Every interactive
    // response is intercepted, so fixtures never submit choices to a server.
    internal static class PopupRuntimeAudit
    {
        static readonly string[] Prefabs={
            "UI/PopupDuelYesOrNo.prefab","UI/PopupDuelPhase.prefab","UI/PopupDuelSelectCard.prefab",
            "UI/PopupDuelPosition.prefab","UI/PopupDuelSelection.prefab","UI/PopupDuelInput.prefab",
            "Popup/PopupConfirm.prefab","Popup/PopupInput.prefab","Popup/PopupProgress.prefab",
            "Popup/PopupRockPaperScissors.prefab","Popup/PopupSearchFilter.prefab","Popup/PopupSearchOrder.prefab",
            "Popup/PopupSelection.prefab","Popup/PopupText.prefab","Popup/PopupYesOrNo.prefab"};

        public static IEnumerator Run(string output,Func<string,IEnumerator> capture)
        {
            if(RoomServant.FromUltraHost)throw new Exception("Popup fixtures require the isolated local duel.");
            var core=Program.instance.ocgcore;
            var oldHandler=OcgCore.handler;bool oldPause=OcgCore.pause;
            var oldMessage=OcgCore.currentMessage;
            bool oldResponded=core.messageDispatcher.playerResponed;
            string oldScale=Config.Get("UltraGuiScale","100"),oldLegacyScale=Config.Get("UIScale","1000");
            var results=new List<object>();var failures=new List<string>();var errors=new List<string>();
            var oldTweenLogging=DOTween.logBehaviour;
            DOTween.logBehaviour=LogBehaviour.Default;
            Application.LogCallback record=(message,stack,type)=>{
                if(type==LogType.Exception || type==LogType.Error
                    || (type==LogType.Warning && message.Contains("DOTWEEN")
                        && (message.IndexOf("callback",StringComparison.OrdinalIgnoreCase)>=0 || message.IndexOf("exception",StringComparison.OrdinalIgnoreCase)>=0)))errors.Add(message+"\n"+stack);
            };
            var responses=new List<int>();
            OcgCore.pause=true;OcgCore.handler=bytes=>{if(bytes.Length>=4)responses.Add(BitConverter.ToInt32(bytes,0));};
            Application.logMessageReceived+=record;
            try
            {
                Program.instance.ui_.chatPanel.Hide();
                var labels=JObject.Parse(File.ReadAllText("Data/popup-label-audit.json"));
                foreach(var entry in labels["choices"])
                {
                    int code=(int)entry["card"],index=(int)entry["index"];
                    string actual=StringHelper.Get(unchecked((int)((uint)code*16+(uint)index)));
                    if(actual!=(string)entry["english"])throw new Exception("Loaded popup label does not match its script: "+code+" / "+index+" / "+actual);
                }
                const int customCode=200000000;
                bool existed=CardsManager._cards.TryGetValue(customCode,out var previous);
                var fixture=new Card{Id=customCode,Name="Custom effect audit",Str=new string[16]};fixture.Str[3]="Unsigned effect description";
                try
                {
                    CardsManager._cards[customCode]=fixture;
                    if(StringHelper.Get(unchecked((int)((uint)customCode*16+3)))!=fixture.Str[3])throw new Exception("High card IDs were decoded as negative system strings.");
                    if(string.IsNullOrWhiteSpace(StringHelper.Get(-1)) || StringHelper.Get(-1)=="???")throw new Exception("Unknown effect text raised an unusable popup label.");
                }
                finally{if(existed)CardsManager._cards[customCode]=previous;else CardsManager._cards.Remove(customCode);}
                foreach(int scale in new[]{70,100,125})
                {
                    Config.Set("UltraGuiScale",scale.ToString());Config.Set("UIScale","1000");
                    foreach(string address in Prefabs)
                    {
                        int errorStart=errors.Count;string issue=null;
                        var load=Addressables.InstantiateAsync(address);
                        yield return load;
                        if(load.Status!=AsyncOperationStatus.Succeeded || load.Result==null)
                        {failures.Add(address+" failed to load");continue;}
                        var go=load.Result;
                        var legacy=go.GetComponent<PopupBase>();var modern=go.GetComponent<UI.Popup.Popup>();
                        go.transform.SetParent(legacy!=null?core.GetUI<OcgCoreUI>().RectPopup:Program.instance.ui_.popup,false);
                        try{Configure(go);if(legacy!=null)legacy.Show();else modern.Show();}
                        catch(Exception e){issue=e.Message;}
                        yield return new WaitForSecondsRealtime(.6f);
                        Canvas.ForceUpdateCanvases();
                        try
                        {
                            if(issue==null)Check(go,legacy,modern);
                            if(issue==null && modern!=null)modern.SendMessage("SelectLastSelected");
                            if(issue==null && go.GetComponent<PopupDuelSelection>()!=null)
                            {
                                var popup=go.GetComponent<PopupDuelSelection>();
                                EventSystem.current.SetSelectedGameObject(null);
                                int before=responses.Count;
                                popup.scrollRect.content.GetComponentsInChildren<Button>().First(b=>b.name=="42").onClick.Invoke();
                                if(responses.Count!=before+1 || responses.Last()!=42)throw new Exception("Choice response depended on keyboard focus instead of the clicked button.");
                            }
                        }
                        catch(Exception e){issue=e.Message;}
                        if(scale==100 || issue!=null)yield return capture("popup-"+Path.GetFileNameWithoutExtension(address)+"-"+scale);
                        if(go!=null)
                        {
                            try{if(legacy!=null)legacy.Hide();else modern.Hide();}
                            catch(Exception e){issue=issue??e.Message;UnityEngine.Object.Destroy(go);}
                        }
                        yield return new WaitForSecondsRealtime(.4f);
                        if(errors.Count>errorStart)issue=issue??string.Join("; ",errors.Skip(errorStart));
                        if(issue!=null)failures.Add(address+" @ "+scale+"%: "+issue);
                        results.Add(new{popup=address,scale,passed=issue==null,error=issue});
                    }
                }
                Config.Set("UltraGuiScale","100");
                // A closing dialog must not clear the newer dialog's focus,
                // current-popup pointer, return action, or input mode.
                foreach(string address in new[]{"UI/PopupDuelInput.prefab","Popup/PopupInput.prefab"})
                {
                    var firstLoad=Addressables.InstantiateAsync(address);yield return firstLoad;
                    var secondLoad=Addressables.InstantiateAsync(address);yield return secondLoad;
                    var first=firstLoad.Result;var second=secondLoad.Result;
                    bool legacy=address.StartsWith("UI/");
                    first.transform.SetParent(legacy?core.GetUI<OcgCoreUI>().RectPopup:Program.instance.ui_.popup,false);
                    second.transform.SetParent(first.transform.parent,false);Configure(first);Configure(second);
                    if(legacy)
                    {
                        first.GetComponent<PopupBase>().Show();first.GetComponent<PopupBase>().Hide();second.GetComponent<PopupBase>().Show();
                        yield return new WaitForSecondsRealtime(.5f);
                        if(core.currentPopup!=second.GetComponent<PopupDuel>() || core.returnAction==null || !OcgCore.inputMode)failures.Add("Closing input popup reset the next duel popup.");
                        second.GetComponent<PopupBase>().Hide();
                    }
                    else
                    {
                        first.GetComponent<UI.Popup.Popup>().Show();first.GetComponent<UI.Popup.Popup>().Hide();second.GetComponent<UI.Popup.Popup>().Show();
                        yield return new WaitForSecondsRealtime(.5f);
                        if(Program.instance.ui_.currentPopupB!=second.GetComponent<UI.Popup.Popup>())failures.Add("Closing input popup reset the next menu popup.");
                        second.GetComponent<UI.Popup.Popup>().Hide();
                    }
                    yield return new WaitForSecondsRealtime(.5f);
                    if(OcgCore.inputMode)failures.Add("Input mode remained active after the input popup closed.");
                }
                var emptyLoad=Addressables.InstantiateAsync("Popup/PopupSelection.prefab");yield return emptyLoad;
                emptyLoad.Result.transform.SetParent(Program.instance.ui_.popup,false);
                var empty=emptyLoad.Result.GetComponent<UI.Popup.PopupSelection>();empty.args=new List<string>{"No available options",""};empty.Show();
                yield return new WaitForSecondsRealtime(.5f);empty.SendMessage("SelectLastSelected");empty.Hide();yield return new WaitForSecondsRealtime(.4f);
                // Reproduce the reported menus and a long scrollable choice list.
                foreach(int code in new[]{4891376,16387555})
                {
                    core.GetUI<OcgCoreUI>().ShowPopupSelection(new List<string>{"Select Effect",StringHelper.Get(code*16+2),StringHelper.Get(code*16+3)},new List<int>{0,1});
                    yield return new WaitForSecondsRealtime(.5f);
                    yield return capture("popup-reported-"+code);
                    core.currentPopup.Hide();yield return new WaitForSecondsRealtime(.4f);
                }
                var selections=new List<string>{"Long effect choices"};
                var ids=new List<int>();
                for(int n=0;n<24;n++){selections.Add("Option "+(n+1)+": "+string.Join(" ",Enumerable.Repeat("Choose a card from your hand, Deck or GY, then resolve its effect.",4)));ids.Add(n);}
                core.GetUI<OcgCoreUI>().ShowPopupSelection(selections,ids);
                yield return new WaitForSecondsRealtime(.5f);
                var many=(PopupDuelSelection)core.currentPopup;
                var rows=many.scrollRect.content.GetComponentsInChildren<Text>();
                if(rows.Length!=24)failures.Add("Long choice list dropped rows");
                foreach(var text in rows)
                    if(text.preferredHeight>text.rectTransform.rect.height+2)failures.Add("Long choice text clipped");
                yield return capture("popup-long-effect-choices");
                many.scrollRect.verticalNormalizedPosition=0;
                yield return capture("popup-long-effect-choices-bottom");
                many.Hide();yield return new WaitForSecondsRealtime(.4f);
                File.WriteAllText(Path.Combine(output,"popup-audit.json"),JsonConvert.SerializeObject(new{popupTypes=Prefabs.Length,choiceDescriptions=(int)labels["selectableDescriptions"],results,failures},Formatting.Indented));
                if(failures.Count>0)throw new Exception("Popup audit failed: "+string.Join(" | ",failures));
                Debug.Log("POPUP_AUDIT_PASSED "+Prefabs.Length+" popup types at 70/100/125%, all loaded effect choices, colon labels, response IDs, scrolling and reported menus");
            }
            finally
            {
                Application.logMessageReceived-=record;
                DOTween.logBehaviour=oldTweenLogging;
                Config.Set("UltraGuiScale",oldScale);Config.Set("UIScale",oldLegacyScale);
                OcgCore.handler=oldHandler;OcgCore.pause=oldPause;OcgCore.currentMessage=oldMessage;
                core.messageDispatcher.playerResponed=oldResponded;
            }
        }

        static void Configure(GameObject go)
        {
            var old=go.GetComponent<PopupBase>();
            if(old!=null)old.selections=new List<string>{"Popup audit","Choose this effect?","Confirm","Cancel"};
            if(go.TryGetComponent<PopupDuelYesOrNo>(out var yes))yes.selections=new List<string>{"Select Effect","Activate Kewl Tune Cue?","Yes","No"};
            if(go.TryGetComponent<PopupDuelPhase>(out var phase))phase.selections=new List<string>{DuelPhase.Main1.ToString(),DuelPhase.BattleStart.ToString(),DuelPhase.End.ToString()};
            if(go.TryGetComponent<PopupDuelSelectCard>(out var cards))
            {cards.cards=OcgCore.cards.Where(c=>c.p.controller==0 && c.p.InLocation(CardLocation.Hand)).Take(8).ToList();cards.min=0;cards.max=1;cards.exitable=true;cards.sendable=false;cards.hint="Choose a card";}
            if(go.TryGetComponent<PopupDuelPosition>(out var position)){position.code=89631139;position.count=3;position.option1=1;}
            if(go.TryGetComponent<PopupDuelSelection>(out var choice))
            {choice.selections=new List<string>{"Select Effect",StringHelper.Get(16387555*16+2),StringHelper.Get(16387555*16+3)};choice.responses=new List<int>{17,42};}
            if(go.TryGetComponent<PopupDuelInput>(out var input))input.selections=new List<string>{"Save replay","Save","Cancel","Popup audit"};
            var modern=go.GetComponent<UI.Popup.Popup>();
            if(modern!=null)modern.args=new List<string>{"Popup audit","Choose this action?","Confirm","Cancel"};
            if(go.TryGetComponent<UI.Popup.PopupInput>(out var textInput))textInput.args=new List<string>{"Input","Popup audit"};
            if(go.TryGetComponent<UI.Popup.PopupText>(out var text))text.args=new List<string>{"Card text","DARK: Draw 1 card.\nChoose: top of the Deck or bottom of the Deck."};
            if(go.TryGetComponent<UI.Popup.PopupSelection>(out var selection))selection.args=new List<string>{"Select Effect","","DARK: Draw 1 card","Choose: top: bottom","g:Genesys: 100 points"};
        }

        static void Check(GameObject go,PopupBase legacy,UI.Popup.Popup modern)
        {
            RectTransform frame=legacy!=null?legacy.window:go.GetComponent<YgomSystem.ElementSystem.ElementObjectManager>().GetElement<RectTransform>("Content");
            var canvas=go.GetComponentInParent<Canvas>();Camera camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
            var corners=new Vector3[4];frame.GetWorldCorners(corners);
            foreach(var corner in corners)
            {
                var point=RectTransformUtility.WorldToScreenPoint(camera,corner);
                if(point.x < -3 || point.x>Screen.width+3 || point.y < -3 || point.y>Screen.height+3)throw new Exception("Popup frame extends outside the screen.");
            }
            foreach(var label in go.GetComponentsInChildren<TMP_Text>())
                if(label.text.Trim()=="???" || label.font==null)throw new Exception("Missing popup text or font: "+label.text);
            foreach(var label in go.GetComponentsInChildren<Text>())
                if(label.text.Trim()=="???" || label.font==null)throw new Exception("Missing legacy popup text or font: "+label.text);
            if(go.GetComponent<UI.Popup.PopupSelection>()!=null)
            {
                var labels=go.GetComponentsInChildren<UI.Popup.SelectionToggle_PopupSelectionItem>().Select(i=>i.selection).ToArray();
                foreach(string expected in new[]{"DARK: Draw 1 card","Choose: top: bottom","Genesys: 100 points"})
                    if(!labels.Contains(expected))throw new Exception("Selection lost text around a colon: "+expected);
            }
            IEnumerable<RectTransform> rows=null;
            if(go.TryGetComponent<PopupDuelSelection>(out var choices))
                rows=choices.scrollRect.content.GetComponentsInChildren<Button>().Select(b=>b.transform as RectTransform);
            else if(go.GetComponent<UI.Popup.PopupSelection>()!=null)
                rows=go.GetComponentsInChildren<UI.Popup.SelectionToggle_PopupSelectionItem>().Select(i=>i.transform as RectTransform);
            if(rows!=null)
            {
                var bounds=rows.Select(row=>{var c=new Vector3[4];row.GetWorldCorners(c);var pixels=c.Select(p=>RectTransformUtility.WorldToScreenPoint(camera,p)).ToArray();return new{top=pixels.Max(p=>p.y),bottom=pixels.Min(p=>p.y)};}).OrderByDescending(r=>r.top).ToArray();
                for(int n=1;n<bounds.Length;n++)if(bounds[n-1].bottom+1<bounds[n].top)throw new Exception("Popup choice buttons overlap at the selected GUI scale.");
            }
        }
    }
}
