using UnityEngine;
using UnityEngine.UI;
namespace MDPro3.UI.ServantUI
{
    public sealed class UltraPassIcon : MaskableGraphic
    {
        static Texture2D supplied;
        public override Texture mainTexture=>supplied!=null?supplied:(supplied=Resources.Load<Texture2D>("Ultra/UserPassIcon"));
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture!=null)UltraReferenceArt.Quad(vh,rectTransform.rect,new Rect(0,0,1,1),color);
        }
    }
}
