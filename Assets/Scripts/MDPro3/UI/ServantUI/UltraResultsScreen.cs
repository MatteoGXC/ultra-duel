using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public sealed class UltraResultsAnimation : MonoBehaviour
    {
        public Sequence sequence;
        void OnDisable(){sequence?.Kill();}
    }
    public static class UltraResultsScreen
    {
        static TMP_FontAsset resultsFont;
        static TMP_FontAsset ResolveFont(Transform parent)
        {
            // Packaged builds may have no TMP default font. Use the same loaded
            // alphabetic font that already renders the home UI and duel labels.
            var menu=Program.instance?.menu?.servantUI;
            var labels=menu!=null?menu.GetComponentsInChildren<TextMeshProUGUI>(true):parent.GetComponentsInChildren<TextMeshProUGUI>(true);
            var font=labels.Select(t=>t.font).FirstOrDefault(f=>f!=null && f.atlasTexture!=null && f.HasCharacter('A') && f.HasCharacter('0'));
            return font??TMP_Settings.defaultFontAsset;
        }
        static RectTransform R(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=parent.gameObject.layer;
            r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        static void Panel(RectTransform r,Color color){var i=r.gameObject.AddComponent<UltraChrome>();i.color=color;i.rimColor=new Color(.45f,.5f,.6f,.35f);i.raycastTarget=false;}
        static TextMeshProUGUI Text(Transform parent,string value,float size,float x,float y,float w,float h)
        {
            var t=R(value,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=resultsFont;t.text=value;t.fontSize=size;
            t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=size*.7f;t.color=UltraTheme.Fg;t.raycastTarget=false;
            UltraTheme.Style(t,size>=26f,size>=26f?4f:0f);return t;
        }
        static void Heading(Transform parent,string text)
        {
            var r=R(text,parent,.035f,.84f,.93f,.13f);Panel(r,new Color(.005f,.008f,.015f,.96f));Text(r,text,25,.025f,.1f,.95f,.8f);
        }
        public static GameObject Show(Transform parent,UltraProgress.Match game,Action next)
        {
            resultsFont=ResolveFont(parent);
            Program.instance?.ui_?.chatPanel?.Hide();
            var root=R("Ultra Duel Results",parent,0,0,1,1);root.gameObject.AddComponent<UltraNativeBackdrop>();root.SetAsLastSibling();
            Text(root,"DUEL RESULTS",31,.035f,.91f,.70f,.06f);
            var score=R("Duel Score",root,.055f,.18f,.39f,.65f);Panel(score,UltraTheme.Panel);Heading(score,"Duel Score");
            var totalLabel=Text(score,"0",47,.08f,.65f,.66f,.12f);
            var animation=DOTween.Sequence().SetUpdate(true).SetLink(root.gameObject);int total=0,displayed=0,rowIndex=0;
            root.gameObject.AddComponent<UltraResultsAnimation>().sequence=animation;
            Panel(R("Score track",score,.08f,.61f,.80f,.01f),new Color(.08f,.1f,.15f));
            var scoreFill=R("Score fill",score,.08f,.61f,0,.01f);Panel(scoreFill,UltraTheme.Accent);scoreFill.gameObject.AddComponent<UltraAccentFill>();
            void Line(string label,int points)
            {
                var row=R(label,score,.08f,.51f-rowIndex++*.083f,.84f,.076f);var group=row.gameObject.AddComponent<CanvasGroup>();group.alpha=0;
                Text(row,label,21,0,.05f,.81f,.90f);Text(row,points.ToString(),21,.81f,.05f,.19f,.90f).alignment=TextAlignmentOptions.MidlineRight;
                Panel(R("Rule",row,0,0,1,.012f),new Color(.60f,.63f,.68f,.4f));
                var end=row.anchoredPosition;row.anchoredPosition=end+Vector2.right*25;total+=points;int target=total;
                animation.Append(group.DOFade(1,.20f)).Join(row.DOAnchorPos(end,.25f).SetEase(Ease.OutQuart));
                animation.Append(DOTween.To(()=>displayed,v=>{displayed=v;totalLabel.text=v.ToString();},target,.34f).SetEase(Ease.OutCubic));
                animation.Join(scoreFill.DOAnchorMax(new Vector2(.08f+.80f*Mathf.Clamp01(target/5000f),.62f),.34f));
            }
            if(game.result=="WIN")Line("Duel Victory",1500);
            if(game.result=="WIN" && game.damageTaken==0)Line("No Damage Taken",1000);
            if(game.damageDealt>=3000)Line("Over 3000 Damage",500);
            if(game.destroyed>0)Line("Destroyed "+game.destroyed+" Monsters",200);
            if(game.spells>0)Line("Activated Spell",100);
            if(game.summons>0)Line("Performed Special Summon",100);
            if(rowIndex==0)Line("Duel "+game.result+" / "+game.turns+" turns",0);
            var rewards=R("Rewards",root,.465f,.40f,.29f,.43f);Panel(rewards,UltraTheme.Panel);Heading(rewards,"Rewards");
            var icon=R("Gem reward",rewards,.08f,.42f,.23f,.30f);Panel(icon,new Color(.015f,.02f,.03f));R("Gem",icon,.18f,.14f,.64f,.70f).gameObject.AddComponent<UltraGem>().raycastTarget=false;
            Text(rewards,"×"+game.gemsEarned+" GEMS",22,.065f,.30f,.40f,.1f);
            Text(rewards,"+"+game.xpEarned+" XP",34,.46f,.49f,.47f,.17f);
            Text(rewards,"Free Duel Pass progress",18,.08f,.13f,.84f,.10f);
            var level=R("Level and XP",root,.465f,.18f,.29f,.195f);Panel(level,UltraTheme.Panel);Heading(level,"LEVEL & EXP");
            int xp=game.xpBefore;
            var levelLabel=Text(level,"Lv. "+(1+xp/500),28,.08f,.38f,.84f,.23f);
            Panel(R("Level track",level,.08f,.24f,.84f,.025f),new Color(.18f,.18f,.22f));
            var levelFill=R("Level progress",level,.08f,.24f,.84f*(xp%500)/500f,.025f);Panel(levelFill,UltraTheme.Accent);levelFill.gameObject.AddComponent<UltraAccentFill>();
            var rewardReveal=rewards.gameObject.AddComponent<CanvasGroup>();rewardReveal.alpha=0;
            animation.Append(rewardReveal.DOFade(1,.35f));
            animation.Append(DOTween.To(()=>xp,v=>{xp=v;levelLabel.text="Lv. "+(1+v/500);levelFill.anchorMax=new Vector2(.08f+.84f*(v%500)/500f,.265f);},game.xpBefore+game.xpEarned,.7f).SetEase(Ease.OutCubic));
            R("Results mate",root,.765f,.18f,.22f,.62f).gameObject.AddComponent<UltraMatePreview>().Load(UltraAccount.ProfileMate).Forget();
            var button=R("Continue",root,.70f,.055f,.25f,.075f);Panel(button,UltraTheme.Accent);button.GetComponent<Image>().raycastTarget=true;
            var b=button.gameObject.AddComponent<Button>();b.targetGraphic=button.GetComponent<Image>();b.onClick.AddListener(()=>{if(animation.IsActive() && !animation.IsComplete()){animation.Complete(true);return;}UnityEngine.Object.Destroy(root.gameObject);next?.Invoke();});
            Text(button,"CONTINUE",27,.04f,.12f,.92f,.76f).alignment=TextAlignmentOptions.Center;
            button.gameObject.AddComponent<UltraThemedButton>().Configure(UltraTier.Hero);
            return root.gameObject;
        }
    }
}
