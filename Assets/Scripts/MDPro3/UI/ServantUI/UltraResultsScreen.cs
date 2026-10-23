using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public static class UltraResultsScreen
    {
        static RectTransform R(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=parent.gameObject.layer;
            r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        static void Panel(RectTransform r,Color color){var i=r.gameObject.AddComponent<UltraChrome>();i.color=color;i.rimColor=new Color(.45f,.5f,.6f,.35f);i.raycastTarget=false;}
        static TextMeshProUGUI Text(Transform parent,string value,float size,float x,float y,float w,float h)
        {
            var t=R(value,parent,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.font=TMP_Settings.defaultFontAsset;t.text=value;t.fontSize=size;
            t.enableAutoSizing=true;t.fontSizeMax=size;t.fontSizeMin=size*.7f;t.color=Color.white;t.raycastTarget=false;return t;
        }
        static void Heading(Transform parent,string text)
        {
            var r=R(text,parent,.035f,.84f,.93f,.13f);Panel(r,new Color(.005f,.008f,.015f,.96f));Text(r,text,25,.025f,.1f,.95f,.8f);
        }
        public static GameObject Show(Transform parent,UltraProgress.Match game,Action next)
        {
            var root=R("Ultra Duel Results",parent,0,0,1,1);root.gameObject.AddComponent<UltraScreenBackdrop>().raycastTarget=true;root.SetAsLastSibling();
            Text(root,"DUEL RESULTS",31,.035f,.91f,.70f,.06f);
            var score=R("Duel Score",root,.055f,.18f,.39f,.65f);Panel(score,new Color(.34f,.35f,.39f,.58f));Heading(score,"Duel Score");
            Text(score,game.Score.ToString(),47,.08f,.65f,.66f,.12f);
            Panel(R("Score track",score,.08f,.61f,.80f,.01f),new Color(.08f,.1f,.15f));
            Panel(R("Score fill",score,.08f,.61f,.80f*Mathf.Clamp01(game.Score/5000f),.01f),new Color(.02f,.70f,.9f));
            string left="",right="";
            void Line(string label,int points){left+=label+"\n\n";right+=points+"\n\n";}
            if(game.result=="WIN")Line("Duel Victory",1500);
            if(game.result=="WIN" && game.damageTaken==0)Line("No Damage Taken",1000);
            if(game.damageDealt>=3000)Line("Over 3000 Damage",500);
            if(game.destroyed>0)Line("Destroyed "+game.destroyed+" Monsters",200);
            if(game.spells>0)Line("Activated Spell",100);
            if(game.summons>0)Line("Performed Special Summon",100);
            if(left.Length==0)left="Duel "+game.result+"\n\nTotal turns: "+game.turns;
            Text(score,left,21,.08f,.05f,.69f,.50f);
            Text(score,right,21,.77f,.05f,.15f,.50f).alignment=TextAlignmentOptions.TopRight;
            var rewards=R("Rewards",root,.465f,.40f,.29f,.43f);Panel(rewards,new Color(.34f,.35f,.39f,.58f));Heading(rewards,"Rewards");
            var icon=R("Gem reward",rewards,.08f,.42f,.23f,.30f);Panel(icon,new Color(.015f,.02f,.03f));R("Gem",icon,.18f,.14f,.64f,.70f).gameObject.AddComponent<UltraGem>().raycastTarget=false;
            Text(rewards,"×"+game.gemsEarned+" GEMS",22,.065f,.30f,.40f,.1f);
            Text(rewards,"+"+game.xpEarned+" XP",34,.46f,.49f,.47f,.17f);
            Text(rewards,"Free Duel Pass progress",18,.08f,.13f,.84f,.10f);
            var level=R("Level and XP",root,.465f,.18f,.29f,.195f);Panel(level,new Color(.34f,.35f,.39f,.58f));Heading(level,"LEVEL & EXP");
            int xp=game.xpBefore+game.xpEarned;
            Text(level,"Lv. "+(1+xp/500),28,.08f,.38f,.84f,.23f);
            Panel(R("Level track",level,.08f,.24f,.84f,.025f),new Color(.18f,.18f,.22f));
            Panel(R("Level progress",level,.08f,.24f,.84f*(xp%500)/500f,.025f),Color.white);
            R("Results mate",root,.765f,.18f,.22f,.62f).gameObject.AddComponent<UltraMatePreview>().Load(UltraAccount.ProfileMate).Forget();
            var button=R("Continue",root,.70f,.055f,.25f,.075f);Panel(button,Color.black);button.GetComponent<Image>().raycastTarget=true;
            var b=button.gameObject.AddComponent<Button>();b.targetGraphic=button.GetComponent<Image>();b.onClick.AddListener(()=>{UnityEngine.Object.Destroy(root.gameObject);next?.Invoke();});
            Text(button,"CONTINUE  >",27,.04f,.12f,.92f,.76f).color=new Color(.75f,1,.1f);
            return root.gameObject;
        }
    }
}
