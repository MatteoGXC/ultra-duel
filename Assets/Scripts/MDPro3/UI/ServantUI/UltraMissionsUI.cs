using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public partial class MainMenuUI
    {
        string missionCategory="All Missions";
        void OpenMissions()
        {
            OpenModal("MISSIONS","","missions");WideModal();
            string[] categories={"All Missions","Daily","Lifetime","Duel Pass"};
            for(int i=0;i<categories.Length;i++)
            {
                string category=categories[i];float y=.70f-i*.115f;
                var button=SmallButton(modal,category.ToUpperInvariant(),()=>{missionCategory=category;OpenMissions();},.045f,y,.225f,.10f,25,category==missionCategory?UltraTier.Primary:UltraTier.Secondary);
                button.GetComponent<UltraThemedButton>().SetSelected(category==missionCategory);
            }
            HomeText(modal,"Daily missions reset at 00:00 UTC",17,Muted,.045f,.13f,.225f,.10f);
            HomeText(modal,missionCategory,25,Color.white,.31f,.79f,.63f,.055f);
            var viewport=HomeRect("Mission list",modal,.31f,.14f,.64f,.64f);var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var content=HomeRect("Mission content",viewport,0,1,1,0);content.pivot=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=40;scroll.movementType=ScrollRect.MovementType.Clamped;
            var rows=UltraMissions.Rows().Where(r=>missionCategory=="All Missions" || r.category==missionCategory).ToArray();
            for(int i=0;i<rows.Length;i++)
            {
                var mission=rows[i];var row=HomeRect(mission.id,content,0,1,1,0);row.pivot=new Vector2(.5f,1);row.sizeDelta=new Vector2(0,238);row.anchoredPosition=new Vector2(0,-i*254);
                var panel=row.gameObject.AddComponent<UltraChrome>();panel.cut=UltraCut.Panel;panel.rim=1.5f;panel.color=UltraTheme.Panel;panel.raycastTarget=false;
                var heading=HomeRect("Mission heading",row,.002f,.73f,.996f,.265f);HomePanel(heading,UltraTheme.PanelSolid);
                HomeText(row,mission.title+"  ("+mission.progress+"/"+mission.target+")",26,Color.white,.02f,.76f,.96f,.21f);
                HomePanel(HomeRect("Progress track",row,.05f,.41f,.64f,.015f),new Color(.34f,.37f,.40f));
                var progress=HomeRect("Progress",row,.05f,.41f,.64f*Mathf.Clamp01((float)mission.progress/mission.target),.015f);HomePanel(progress,UltraTheme.Accent);progress.gameObject.AddComponent<UltraAccentFill>();
                HomeText(row,mission.category+"  /  "+(mission.claimed?"RECEIVED":mission.progress>=mission.target?"COMPLETE":"IN PROGRESS"),17,mission.claimed?Muted:Lime,.05f,.12f,.60f,.12f);
                var reward=HomeRect("Reward",row,.76f,.09f,.19f,.57f);var rim=reward.gameObject.AddComponent<UltraChrome>();rim.color=UltraTheme.PanelSolid;rim.cut=UltraCut.Small;rim.rim=2;rim.followThemeRim=false;rim.rimColor=UltraTheme.Accent;rim.raycastTarget=false;
                HomePanel(HomeRect("Reward target",reward,.02f,.76f,.96f,.22f),Color.black);
                HomeText(reward,mission.claimed?"✓":mission.target.ToString(),21,Color.white,.05f,.77f,.90f,.20f).alignment=TextAlignmentOptions.Center;
                HomeRect("Gem",reward,.32f,.27f,.36f,.43f).gameObject.AddComponent<UltraGem>().raycastTarget=false;
                HomeText(reward,"×"+mission.gems,20,Color.white,.05f,.03f,.90f,.21f).alignment=TextAlignmentOptions.Center;
                if(!mission.claimed && mission.progress>=mission.target){var button=reward.gameObject.AddComponent<Button>();button.targetGraphic=rim;rim.raycastTarget=true;button.onClick.AddListener(()=>Run(async()=>{await UltraMissions.Claim(mission.id);OpenMissions();}));}
            }
            content.sizeDelta=new Vector2(0,rows.Length*254);
            var receive=SmallButton(modal,"RECEIVE ALL",()=>Run(async()=>{await UltraMissions.Claim();OpenMissions();}),.70f,.025f,.25f,.085f,25,UltraTier.Hero);
            receive.interactable=UltraMissions.Rows().Any(r=>!r.claimed && r.progress>=r.target);
            SmallButton(modal,"DUEL PASS",OpenPass,.31f,.025f,.24f,.085f,21);
            if(rows.Length==0)HomeText(modal,"Connect to load your missions.",24,Muted,.33f,.42f,.58f,.10f);
        }
    }
}
