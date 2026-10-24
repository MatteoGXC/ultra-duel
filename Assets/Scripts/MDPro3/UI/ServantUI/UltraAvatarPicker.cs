using System.Linq;
using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Net;

namespace MDPro3.UI.ServantUI
{
    public partial class MainMenuUI
    {
        void OpenAvatarPicker()
        {
            OpenModal("PROFILE ICON","Search card names to choose your portrait and its accent color.","avatar");WideModal();
            var query=Input(modal,"Card name",.72f);
            SmallButton(modal,"SEARCH",()=>ShowAvatarResults(query.text),.74f,.69f,.20f,.09f,21);
            query.onSubmit.AddListener(ShowAvatarResults);
        }
        void ShowAvatarResults(string query)
        {
            OpenAvatarPicker();
            var content=ScrollArea();int index=0;
            foreach(var card in CardsManager.Search(query,new System.Collections.Generic.List<long>(),null,string.Empty,namesOnly:true).GroupBy(c=>c.Name).Select(g=>g.First()).Take(40))
            {
                int id=card.Id;var row=Row(content,index++,card.Name,"Use card artwork / automatic accent");
                SmallButton(row,"SELECT",()=>Run(async()=>{
                    if(!string.IsNullOrEmpty(UltraServices.Token))UltraServices.SetProfile(await UltraServices.Request("/profile/avatar","POST",new{card=id}));
                    UltraProgress.Data.avatarCard=id;UltraProgress.Save();RefreshUltraHome();OpenProfile();
                }),.76f,.2f,.21f,.6f,18);
            }
        }
    }
}
