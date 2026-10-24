using System;
using DG.Tweening;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using MDPro3.Servant;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public class UltraHomeKeyboard : MonoBehaviour
    {
        public Action open;
        string typed="";
        Keyboard keyboard;
        void OnEnable() {keyboard=Keyboard.current;if(keyboard!=null)keyboard.onTextInput+=Text;}
        void OnDisable() {if(keyboard!=null)keyboard.onTextInput-=Text;typed="";}
        void Text(char c)
        {
            if(EventSystem.current?.currentSelectedGameObject?.GetComponent<TMP_InputField>()!=null)return;
            typed+=char.ToLowerInvariant(c);
            if(typed.Length>10)typed=typed.Substring(typed.Length-10);
            if(typed=="ultraadmin"){typed="";open?.Invoke();}
        }
    }

    public partial class MainMenuUI
    {
        RawImage NativeArt(Transform parent,string asset,float x,float y,float w,float h)
        {
            var rect=HomeRect(asset,parent,x,y,w,h);
            var texture=Resources.Load<Texture2D>("Ultra/"+asset);
            if(asset=="ProfileFrame")
            {
                var sliced=rect.gameObject.AddComponent<Image>();
                if(texture!=null)sliced.sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(12,12,12,12));
                sliced.type=Image.Type.Sliced;sliced.raycastTarget=false;return null;
            }
            var image=rect.gameObject.AddComponent<RawImage>();image.texture=texture;image.raycastTarget=false;return image;
        }
        int passPage;
        void WideModal()
        {
            modal.anchorMin=new Vector2(.04f,.055f);modal.anchorMax=new Vector2(.96f,.945f);
            modal.offsetMin=modal.offsetMax=Vector2.zero;
            overlay.GetComponent<Image>().color=Color.clear;
            var panel=modal.GetComponent<Image>();panel.color=Color.clear;
            if(panel is UltraChrome chrome){chrome.rim=0;chrome.rimColor=Color.clear;}
        }
        void StatPanel(Transform parent,string heading,string value,float x,float y,float w,float h)
        {
            var panel=HomeRect(heading,parent,x,y,w,h);HomePanel(panel,new Color(.12f,.15f,.21f,.96f));
            HomePanel(HomeRect("Heading strip",panel,0,.65f,1,.35f),new Color(.68f,.72f,.77f));
            HomeText(panel,heading,17,new Color(.04f,.06f,.08f),.04f,.68f,.92f,.28f);
            HomeText(panel,value,27,Color.white,.04f,.04f,.92f,.54f);
        }
        async UniTask LoadMatePortrait(Image image,int id=0)
        {
            try
            {
                if(id==0)id=UltraAccount.ProfileMate;
                var sprite=await Program.items.TryLoadItemIconAsync(id.ToString(),Items.ItemType.Mate);
                if(image!=null && sprite!=null)image.sprite=sprite;
            }
            catch(Exception e){Debug.LogWarning("Mate portrait: "+e.Message);}
        }
        void OpenUpdates()
        {
            OpenModal("UPDATES","Ultra Duel v"+UltraServices.Version+" / What's new","updates");
            HomeText(modal,"PLAYER EXPERIENCE\nNew Duel Pass reward grid, accessory shop, Game Start screen and player profiles.\n\nPLAY WITH FRIENDS\nSide panel, duel invitations, room tables, account gems and online / in-duel status.\n\nSOLO & CARDS\nChain-prompt compatibility fixes and published-deck AI support.\nIncludes the previous 300 card entries, artwork fixes and Burst Protocol index.",23,Color.white,.055f,.23f,.89f,.52f);
            SmallButton(modal,"CHECK FOR UPDATES",()=>UltraServices.CheckUpdates(true),.055f,.12f,.89f,.08f);
        }
        void ShowGameStart()
        {
            OpenModal("", "", "start");
            overlay.GetComponent<Image>().color=new Color(0,0,0,.08f);
            modal.anchorMin=Vector2.zero;modal.anchorMax=new Vector2(.52f,1);modal.offsetMin=modal.offsetMax=Vector2.zero;
            // The existing selected wallpaper stays visible on the right.
            foreach(Transform child in modal)child.gameObject.SetActive(false);
            modal.GetComponent<Image>().color=Color.clear;
            var wordmark=HomeText(modal,"ULTRA DUEL",67,new Color(.91f,.94f,1),.055f,.45f,.89f,.15f);
            wordmark.fontStyle=FontStyles.Bold;wordmark.alignment=TextAlignmentOptions.Center;
            wordmark.outlineWidth=.18f;wordmark.outlineColor=new Color32(84,80,183,255);
            var logoGroup=wordmark.gameObject.AddComponent<CanvasGroup>();logoGroup.alpha=0;
            var logoRect=wordmark.rectTransform;var logoEnd=logoRect.anchoredPosition;logoRect.anchoredPosition+=Vector2.left*45;
            logoGroup.DOFade(1,.8f).SetUpdate(true).SetLink(wordmark.gameObject);
            logoRect.DOAnchorPos(logoEnd,1).SetEase(Ease.OutQuart).SetUpdate(true).SetLink(wordmark.gameObject);
            var titleTriangle=HomeRect("Original title artwork",modal,.055f,.32f,.89f,.40f).gameObject.AddComponent<UltraNativeTitle>();titleTriangle.raycastTarget=false;
            wordmark.transform.SetAsLastSibling();
            var homeDefault=defaultSelectable;
            defaultSelectable=SmallButton(modal,"GAME START",()=>{CloseModal();defaultSelectable=homeDefault;homeDefault?.Select();},.10f,.23f,.76f,.075f,25);
            var startFrame=defaultSelectable.GetComponent<UltraChrome>();startFrame.color=startFrame.rimColor=Color.clear;startFrame.rim=0;
            defaultSelectable.transition=Selectable.Transition.None;
            var pulse=defaultSelectable.gameObject.AddComponent<CanvasGroup>();pulse.DOFade(.38f,1.4f).SetLoops(-1,LoopType.Yoyo).SetEase(Ease.InOutSine).SetUpdate(true).SetLink(defaultSelectable.gameObject);
            HomeText(modal,"Ver. "+UltraServices.Version+"\n"+(string.IsNullOrEmpty(UltraServices.PlayerName)?"Welcome, Duelist":UltraServices.PlayerName),18,Muted,.10f,.84f,.80f,.09f);
        }
        void OpenProfile()
        {
            string name=string.IsNullOrEmpty(UltraServices.PlayerName)?Config.Get("DuelPlayerName0","Player"):UltraServices.PlayerName;
            OpenModal("PROFILE","", "profile");WideModal();
            var card=HomeRect("Profile card",modal,.27f,.20f,.29f,.62f);HomePanel(card,new Color(.02f,.025f,.035f,.96f));
            var frame=card.GetComponent<UltraChrome>();frame.rim=2;frame.rimColor=new Color(.72f,.74f,.78f);
            var bannerClip=HomeRect("Profile banner",card,.015f,.72f,.97f,.265f);bannerClip.gameObject.AddComponent<RectMask2D>();
            var banner=HomeRect("Banner art",bannerClip,0,0,1,1).gameObject.AddComponent<Image>();
            LoadBanner(banner).Forget();
            HomePanel(HomeRect("Name band",card,.015f,.49f,.97f,.225f),new Color(.25f,.26f,.28f,.70f));
            var avatar=HomeRect("Profile mate icon",card,.035f,.56f,.23f,.19f).gameObject.AddComponent<Image>();avatar.preserveAspect=true;LoadMatePortrait(avatar).Forget();
            HomeText(card,name,28,Color.white,.30f,.60f,.65f,.105f);
            int uid=(int?)UltraServices.State?["id"]??0;
            string identity=uid==0?"OFFLINE":uid.ToString("D9");
            if(uid!=0)identity=identity.Substring(0,3)+"-"+identity.Substring(3,3)+"-"+identity.Substring(6,3);
            HomeText(card,"LV "+UltraAccount.Level+"     ID: "+identity,20,Color.white,.05f,.48f,.90f,.08f);
            HomePanel(HomeRect("Divider",card,.035f,.47f,.93f,.002f),Muted);
            int friends=(UltraServices.State?["friends"] as JArray)?.Count??0;
            HomeText(card,friends+" Friends     |     "+UltraAccount.Rank,18,Muted,.05f,.385f,.90f,.08f);
            HomePanel(HomeRect("Divider",card,.035f,.375f,.93f,.002f),Muted);
            SmallButton(card,UltraAccount.Rank,OpenPass,.055f,.25f,.89f,.09f,18);
            HomeText(card,"PASS GRADE "+UltraAccount.Tier+" / 20",21,Color.white,.055f,.14f,.89f,.08f);
            HomeText(card,"DUELIST PROFILE",15,Muted,.055f,.04f,.89f,.06f);
            HomeRect("Animated mate",modal,.61f,.27f,.23f,.44f).gameObject.AddComponent<UltraMatePreview>().Load(UltraAccount.ProfileMate).Forget();
            SmallButton(modal,"EDIT",OpenProfileEdit,.10f,.055f,.18f,.08f,23);
            SmallButton(modal,"REPLAY",OpenHistory,.305f,.055f,.18f,.08f,23);
            SmallButton(modal,"DATA",OpenStats,.51f,.055f,.18f,.08f,23);
            SmallButton(modal,"COLLECTION",OpenCollection,.715f,.055f,.18f,.08f,23);
        }
        async UniTask LoadBanner(Image image)
        {
            try {var sprite=await Program.items.TryLoadItemIconAsync(Config.Get("Wallpaper",UIManager.DefaultWallpaper),Items.ItemType.Wallpaper);if(image!=null && sprite!=null){image.sprite=sprite;var fit=image.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=(float)sprite.texture.width/sprite.texture.height;}}
            catch(Exception e){Debug.LogWarning("Profile banner: "+e.Message);}
        }
        void OpenProfileEdit()
        {
            OpenModal("EDIT PROFILE","Choose an owned mate for your profile","edit-profile");WideModal();
            var content=ScrollArea();int index=0;
            var row=Row(content,index++,"Starter mate","Free starter");
            SmallButton(row,"EQUIP",()=>EquipProfileMate(1000001),.75f,.2f,.22f,.6f,18);
            foreach(var item in UltraAccount.Catalog.Where(p=>p.kind=="Mate" && UltraAccount.Owns(p)))
            {
                int id=item.id;row=Row(content,index++,item.Name,"Owned mate");
                SmallButton(row,"EQUIP",()=>EquipProfileMate(id),.75f,.2f,.22f,.6f,18);
            }
            SmallButton(modal,"SHOP",OpenShop,.045f,.025f,.23f,.07f,18);
            SmallButton(modal,"PROFILE ICON",OpenAvatarPicker,.70f,.025f,.25f,.07f,18);
        }
        void EquipProfileMate(int id){Config.Set("ProfileMate",id.ToString());Config.Save();OpenProfile();RefreshUltraHome();}
        void OpenCollection()
        {
            OpenModal("COLLECTION","Your owned accessories / Equip deck items from the deck appearance menu","collection");WideModal();
            var content=ScrollArea();int index=0;
            foreach(var item in UltraAccount.Catalog.Where(UltraAccount.Owns))Row(content,index++,item.Name,item.kind+" / Owned");
            if(index==0)Row(content,0,"Your collection starts here","Starter accessories are always available. Earn gems in duels and the free pass.");
            SmallButton(modal,"SHOP",OpenShop,.045f,.025f,.23f,.07f,18);
        }
        void OpenStats()
        {
            var p=UltraAccount.Profile;
            string name=string.IsNullOrEmpty(UltraServices.PlayerName)?Config.Get("DuelPlayerName0","Player"):UltraServices.PlayerName;
            OpenModal("PLAYER PROFILE",name+" / "+UltraAccount.Rank,"profile");
            var avatar=HomeRect("Mate",modal,.055f,.49f,.20f,.26f).gameObject.AddComponent<Image>();avatar.preserveAspect=true;LoadMatePortrait(avatar).Forget();
            StatPanel(modal,"LEVEL",UltraAccount.Level.ToString(),.29f,.59f,.29f,.15f);
            StatPanel(modal,"PASS GRADE",UltraAccount.Tier+" / 20",.62f,.59f,.32f,.15f);
            int wins=p==null?UltraProgress.Data.wins:(int)p["wins"];
            int games=p==null?UltraProgress.Data.matches.Count:(int)p["played"];
            string favorite=p==null?UltraProgress.Data.matches.GroupBy(x=>x.deck).OrderByDescending(x=>x.Count()).FirstOrDefault()?.Key:(string)p["favoriteDeck"];
            StatPanel(modal,"WINS",wins.ToString(),.29f,.38f,.29f,.15f);
            StatPanel(modal,"GAMES PLAYED",games.ToString(),.62f,.38f,.32f,.15f);
            HomeText(modal,"MOST PLAYED DECK\n"+(favorite??"No games yet"),23,Color.white,.055f,.22f,.89f,.12f);
            SmallButton(modal,"MATCH HISTORY",OpenHistory,.055f,.12f,.42f,.08f);
            SmallButton(modal,"DUEL PASS",OpenPass,.525f,.12f,.42f,.08f);
            modalStatus.text=p==null?"Offline profile / This PC":"Account profile / Personal play statistics";
        }
        void ShowShop(string category,int page)
        {
            OpenModal("SHOP","Accessories", "shop");WideModal();
            HomeText(modal,"◆ "+UltraAccount.Gems+" GEMS",22,Color.white,.74f,.89f,.21f,.06f);
            SmallButton(modal,"ACCESSORIES",()=>ShowShop(category,page),.045f,.71f,.42f,.065f,21);
            SmallButton(modal,"DUEL PASS",OpenPass,.49f,.71f,.46f,.065f,21);
            HomePanel(HomeRect("Category underline",modal,.045f,.70f,.42f,.003f),Lime);
            string[] categories={"Mate","Case","Protector","Field","Frame"};
            for(int i=0;i<categories.Length;i++)
            {
                string type=categories[i];
                var b=SmallButton(modal,type=="Field"?"FIELD SETS":type.ToUpperInvariant(),()=>ShowShop(type,0),.045f,.59f-i*.082f,.17f,.068f,19);
                b.GetComponent<Image>().color=type==category?new Color(.5f,.7f,.02f):new Color(.06f,.13f,.18f);
                if(type==category)b.GetComponentInChildren<TextMeshProUGUI>().color=Color.black;
            }
            var list=UltraAccount.Catalog.Where(p=>p.kind==category).ToList();int pages=Math.Max(1,(list.Count+4)/5);page=Mathf.Clamp(page,0,pages-1);
            HomeText(modal,category=="Field"?"DUEL FIELD SETS":category.ToUpperInvariant()+" COLLECTION",22,Color.white,.25f,.61f,.68f,.055f);
            HomePanel(HomeRect("Section rule",modal,.25f,.60f,.70f,.0015f),Muted);
            for(int i=0;i<5 && page*5+i<list.Count;i++)
            {
                var item=list[page*5+i];bool owned=UltraAccount.Owns(item);
                var card=HomeRect(item.Name,modal,.25f+i*.142f,.19f,.132f,.385f);
                HomePanel(card,new Color(.055f,.075f,.13f,.94f));
                var trim=card.GetComponent<UltraChrome>();trim.rim=2;trim.rimColor=owned?Lime:new Color(.38f,.36f,.65f);
                HomePanel(HomeRect("Banner",card,.02f,.90f,.96f,.09f),owned?new Color(.2f,.42f,.1f):new Color(.22f,.12f,.37f));
                HomeText(card,owned?"OWNED":category=="Field"?"COMPLETE SET":"ACCESSORY",12,Color.white,.06f,.90f,.88f,.08f).alignment=TextAlignmentOptions.Center;
                var preview=HomeRect("Preview",card,.055f,.35f,.89f,.53f).gameObject.AddComponent<Image>();preview.preserveAspect=true;preview.raycastTarget=false;
                LoadProduct(preview,item).Forget();
                HomeText(card,item.Name,17,Color.white,.055f,.17f,.89f,.17f).alignment=TextAlignmentOptions.Center;
                SmallButton(card,owned?(item.kind=="Frame"?"EQUIP":"OWNED"):"◆ "+item.price,()=>Run(async()=>{await UltraAccount.Buy(item);ShowShop(category,page);modalStatus.text=item.Name+" unlocked.";}),.07f,.035f,.86f,.12f,18).interactable=!owned || item.kind=="Frame";
            }
            if(category=="Field")HomeText(modal,"Each set includes the duel field, mate base and field parts.",17,Muted,.25f,.125f,.70f,.04f);
            SmallButton(modal,"<",()=>ShowShop(category,page-1),.25f,.025f,.10f,.07f,24).interactable=page>0;
            HomeText(modal,(page+1)+" / "+pages,18,Muted,.39f,.025f,.14f,.07f);
            SmallButton(modal,">",()=>ShowShop(category,page+1),.55f,.025f,.10f,.07f,24).interactable=page+1<pages;
            modalStatus.text="Earn gems in duels and the free pass. Starter accessories stay available.";
        }
        async UniTask LoadProduct(Image image,UltraAccount.Product item)
        {
            try {
                if(item.kind=="Frame"){image.color=item.name=="Violet"?new Color(.65f,.3f,1):new Color(.1f,.85f,.5f);return;}
                if(item.kind=="Protector")
                {
                    var mat=await ABLoader.LoadProtectorMaterial(item.id.ToString(),image.GetCancellationTokenOnDestroy());
                    if(image==null)return;
                    var raw=image.gameObject.AddComponent<RawImage>();raw.texture=mat.mainTexture;raw.raycastTarget=false;image.enabled=false;return;
                }
                var type=item.kind=="Mate"?Items.ItemType.Mate:item.kind=="Case"?Items.ItemType.Case:Items.ItemType.Mat;
                var sprite=await Program.items.TryLoadItemIconAsync(item.id.ToString(),type);
                if(image!=null && sprite!=null)image.sprite=sprite;
            }catch(Exception e){if(image!=null)image.color=new Color(.15f,.23f,.32f);Debug.LogWarning("Shop preview: "+e.Message);}
        }
        void OpenFriend(JToken person)
        {
            int id=(int)person["id"];string name=(string)person["name"];
            OpenModal(name,(string)person["activity"]??"Friend","friend");
            HomeText(modal,"CHALLENGE A FRIEND",32,Color.white,.055f,.57f,.89f,.15f);
            SmallButton(modal,"INVITE TO DUEL",()=>InviteFriend(id),.055f,.40f,.89f,.11f);
            SmallButton(modal,"REMOVE FRIEND",()=>UIManager.ShowPopupYesOrNo(new System.Collections.Generic.List<string>{"Remove friend","Remove "+name+"?","Remove","Cancel",Config.STRING_YES},()=>RemoveFriend(id),null),.055f,.23f,.42f,.09f);
            SmallButton(modal,"BACK",ShowFriends,.525f,.23f,.42f,.09f);
        }
        void InviteFriend(int id)
        {
            DuelFormat.Select(()=>Run(async()=>{
                await UltraServices.Refresh();
                var room=(UltraServices.State?["rooms"] as JArray)?.FirstOrDefault(r=>(int)r["hostId"]==(int)UltraServices.State["id"]);
                JObject hosted;
                if(room==null)hosted=await UltraServices.Request("/rooms","POST",new{format=DuelFormat.Genesys?"Genesys":"TCG",cap=DuelFormat.PointCap});
                else hosted=await UltraServices.Request("/rooms/"+(string)room["id"]+"/join","POST",new{});
                await UltraServices.Request("/invites","POST",new{id,room=(string)hosted["id"]});
                CloseModal();await UltraServices.JoinRoom(hosted);
                MessageManager.Cast("Duel invitation sent. Your friend can join from Friends.");
            }));
        }
        void JoinFriendRoom(string id)
        {
            Run(async()=>{var room=await UltraServices.Request("/rooms/"+id+"/join","POST",new{});CloseModal();await UltraServices.JoinRoom(room);});
        }
        void OpenHistory()
        {
            OpenModal("MATCH HISTORY","Your recent matches / Select a saved replay to watch","history");WideModal();
            var content=ScrollArea();int index=0;
            foreach(var game in UltraProgress.Data.matches)
            {
                string date=DateTime.TryParse(game.date,out var dt)?dt.ToLocalTime().ToString("yyyy MMM dd HH:mm"):game.date;
                var row=Row(content,index++,game.result+"    VS "+game.opponent,game.mode+"  /  "+game.deck+"  /  "+date+"  /  Turns: "+game.turns);
                HomePanel(HomeRect("Result edge",row,0,0,.005f,1),game.result=="WIN"?Lime:new Color(.65f,.3f,.35f));
                bool replay=!string.IsNullOrEmpty(game.replay) && (File.Exists(Program.PATH_REPLAY+game.replay+Program.EXPANSION_YRP3D) || File.Exists(Program.PATH_REPLAY+game.replay));
                SmallButton(row,replay?"WATCH  >":"NO REPLAY",()=>{CloseModal();Program.instance.ShiftToServant(Program.instance.replay);PlayHistoryReplay(game.replay).Forget();},.75f,.2f,.22f,.60f,16).interactable=replay;
            }
            if(Directory.Exists(Program.PATH_REPLAY))
            foreach(var path in Directory.GetFiles(Program.PATH_REPLAY).OrderByDescending(File.GetLastWriteTime))
            {
                if(!path.EndsWith(Program.EXPANSION_YRP3D) && !path.EndsWith(Program.EXPANSION_YRP))continue;
                string file=Path.GetFileName(path),name=file.EndsWith(Program.EXPANSION_YRP3D)?Path.GetFileNameWithoutExtension(path):file;
                if(UltraProgress.Data.matches.Any(g=>g.replay==name || g.replay==file))continue;
                var row=Row(content,index++,"REPLAY    "+Path.GetFileNameWithoutExtension(path),File.GetLastWriteTime(path).ToString("yyyy MMM dd HH:mm"));
                SmallButton(row,"WATCH  >",()=>{CloseModal();Program.instance.ShiftToServant(Program.instance.replay);PlayHistoryReplay(name).Forget();},.75f,.2f,.22f,.60f,16);
            }
            if(index==0)Row(content,0,"Your next duel starts your history","Finished duels save a replay automatically.");
            SmallButton(modal,"ALL REPLAYS",()=>{CloseModal();OnReplay();},.045f,.025f,.24f,.07f,18);
        }
        async UniTask PlayHistoryReplay(string replay)
        {
            await UniTask.WaitUntil(()=>Program.instance.replay.servantUI!=null && Program.instance.replay.servantUI.gameObject.activeInHierarchy);
            Program.instance.replay.GetUI<ReplaySelectorUI>().KF_Replay(replay);
        }
        void OpenAdmin()
        {
            if((bool?)UltraServices.State?["admin"]!=true){MessageManager.Cast("Administrator account required.");return;}
            Run(async()=>{
                var players=await UltraServices.Request("/admin/players");
                OpenModal("ADMINISTRATION","Registered players / Account wallet grants are recorded on the VPS","admin");WideModal();
                var content=ScrollArea();int index=0;
                foreach(var player in players["players"] as JArray ?? new JArray())
                {
                    int id=(int)player["id"];
                    var row=Row(content,index++,(string)player["name"],(string)player["activity"]+" / "+(int)player["gems"]+" gems");
                    SmallButton(row,"GRANT",()=>GrantPlayer(id,(string)player["name"]),.75f,.2f,.22f,.60f,18);
                }
                SmallButton(modal,"REFRESH",OpenAdmin,.045f,.025f,.23f,.07f,18);
            });
        }
        void GrantPlayer(int id,string name)
        {
            OpenModal("GRANT GEMS",name+" / Added to their account wallet","grant");
            var amount=Input(modal,"Gem amount (1–100000)",.57f);amount.contentType=TMP_InputField.ContentType.IntegerNumber;
            var reason=Input(modal,"Reason for reward",.42f);reason.characterLimit=200;
            SmallButton(modal,"CONFIRM GRANT",()=>Run(async()=>{
                if(!int.TryParse(amount.text,out int value) || value<1 || value>100000)throw new Exception("Enter 1–100,000 gems.");
                await UltraServices.Request("/admin/grant","POST",new{id,amount=value,reason=reason.text});
                modalStatus.text="Granted "+value+" gems to "+name+".";
                await UltraServices.Refresh();
            }),.055f,.24f,.89f,.10f);
        }
    }
}
