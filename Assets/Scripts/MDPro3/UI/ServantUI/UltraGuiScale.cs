using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    // Scale interactive UI only. Wallpaper, background and 3D field never enter
    // this list; authored layouts and button hover animations remain intact.
    [DefaultExecutionOrder(10000)]
    public sealed class UltraGuiScale : MonoBehaviour
    {
        sealed class Entry { public Transform transform; public Vector3 authored, applied; }
        readonly Dictionary<int,Entry> entries=new();
        float nextScan;
        public static int Percent => Mathf.Clamp(int.TryParse(Config.Get("UltraGuiScale","100"),out int value)?value:100,70,125);
        public static void Set(int value){Config.Set("UltraGuiScale",Mathf.Clamp(value,70,125).ToString());Config.Save();}
        public static float FitPopup(RectTransform frame,float requested)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(frame);
            var canvas=frame.GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
            float scale=requested;
            if(canvas!=null)
            {
                if(frame.rect.width>0)scale=Mathf.Min(scale,canvas.rect.width*.94f/frame.rect.width);
                if(frame.rect.height>0)scale=Mathf.Min(scale,canvas.rect.height*.94f/frame.rect.height);
            }
            frame.localScale=Vector3.one*scale;
            return scale;
        }
        void LateUpdate()
        {
            if(Time.unscaledTime>=nextScan)
            {
                nextScan=Time.unscaledTime+.25f;
                foreach(var item in Resources.FindObjectsOfTypeAll<Selectable>())
                {
                    if(item==null || !item.gameObject.scene.IsValid() || item.GetComponentInParent<Canvas>()==null)continue;
                    // Popup layouts scale as a group. Enlarging each row button
                    // independently makes neighboring choices overlap or clip.
                    if(item.GetComponentInParent<PopupBase>(true)!=null
                        || item.GetComponentInParent<MDPro3.UI.Popup.Popup>(true)!=null
                        || item.GetComponentInParent<UltraPopupFrame>(true)!=null)continue;
                    var ancestor=item.transform.parent;
                    bool nested=false;
                    while(ancestor!=null){if(ancestor.GetComponent<Selectable>()!=null){nested=true;break;}ancestor=ancestor.parent;}
                    if(nested || entries.ContainsKey(item.GetInstanceID()))continue;
                    entries[item.GetInstanceID()]=new Entry{transform=item.transform,authored=item.transform.localScale,applied=item.transform.localScale};
                }
            }
            float scale=Percent/100f;
            foreach(var entry in entries.Values)
            {
                if(entry.transform==null || !entry.transform.gameObject.activeInHierarchy)continue;
                var current=entry.transform.localScale;
                if((current-entry.applied).sqrMagnitude>.000001f)entry.authored=current;
                entry.applied=entry.authored*scale;entry.transform.localScale=entry.applied;
            }
            // Destroyed modal buttons must not accumulate during a long session.
            if(entries.Count>500){var dead=new List<int>();foreach(var pair in entries)if(pair.Value.transform==null)dead.Add(pair.Key);foreach(int key in dead)entries.Remove(key);}
        }
    }
}
