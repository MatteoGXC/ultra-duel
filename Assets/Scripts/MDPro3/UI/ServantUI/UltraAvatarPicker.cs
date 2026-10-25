using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public partial class MainMenuUI
    {
        void OpenAvatarPicker()
        {
            OpenModal("PROFILE ICON","Choose your icon. Its colors set the profile frame accent.","avatar");WideModal();
            var content=ScrollArea();int index=0;
            foreach(var icon in Program.items.faces.Where(i=>!i.diy && !i.notReady && i.id>=1010000))
            {
                int id=icon.id;
                var row=Row(content,index++,icon.name,"Profile and duel icon");
                var image=HomeRect("Icon preview",row,.64f,.1f,.1f,.8f).gameObject.AddComponent<Image>();image.preserveAspect=true;
                LoadProfileIcon(image,id).Forget();
                SmallButton(row,"SELECT",()=>{Config.Set("DuelFace0",id.ToString());Config.Save();RefreshUltraHome();OpenProfile();},.76f,.2f,.21f,.6f,18);
            }
        }
        async UniTask LoadProfileIcon(Image image,int id)
        {
            var sprite=await Program.items.TryLoadItemIconAsync(id.ToString(),Items.ItemType.Face);
            if(image!=null)image.sprite=sprite;
        }
    }
}
