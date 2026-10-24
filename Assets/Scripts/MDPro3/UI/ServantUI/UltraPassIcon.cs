using UnityEngine;
using UnityEngine.UI;
namespace MDPro3.UI.ServantUI
{
    public sealed class UltraPassIcon : MaskableGraphic
    {
        static Texture2D supplied;
        static Material transparentArtwork;
        protected override void Awake()
        {
            base.Awake();
            if(transparentArtwork==null)
            {
                transparentArtwork=new Material(Resources.Load<Shader>("Ultra/ReferenceTransparency"));
                Color key=new Color32(31,31,31,255);if(QualitySettings.activeColorSpace==ColorSpace.Linear)key=key.linear;
                transparentArtwork.SetVector("_Background",new Vector4(key.r,key.g,key.b,0));
                transparentArtwork.SetFloat("_Tolerance",QualitySettings.activeColorSpace==ColorSpace.Linear?.006f:.025f);
            }
            material=transparentArtwork;
        }
        public override Texture mainTexture=>supplied!=null?supplied:(supplied=Resources.Load<Texture2D>("Ultra/UserPassIcon"));
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(mainTexture!=null)UltraReferenceArt.Quad(vh,rectTransform.rect,new Rect(0,0,1,1),color);
        }
    }
}
