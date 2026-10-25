using System;
using System.IO;
using Cysharp.Threading.Tasks;

namespace MDPro3.Net
{
    public static class UltraDeckCloudVerification
    {
        public static async UniTask Run()
        {
            const string session="Data/friends-session.json", metadata="Progress/deck-cloud-diagnostic.json";
            byte[] previous=File.Exists(session)?File.ReadAllBytes(session):null;
            string name="UT_SAVE_"+Guid.NewGuid().ToString("N").Substring(0,8);
            string path="Deck/CloudDiagnostic/"+name+".ydk";
            string content="#created by deck restore test\n#main\n89631139\n#extra\n!side\n";
            try
            {
                await UltraServices.SignIn(name,Guid.NewGuid().ToString("N"),true);
                Directory.CreateDirectory("Deck/CloudDiagnostic");File.WriteAllText(path,content);
                await UltraDeckCloud.Sync(true);
                var stored=await UltraServices.Request("/decks");
                if(!stored["decks"].ToString().Contains(name))throw new Exception("Client did not upload the fixture deck: "+UltraDeckCloud.Status);
                File.Delete(path);if(File.Exists(metadata))File.Delete(metadata);
                await UltraDeckCloud.Sync(true);
                if(!File.Exists(path) || File.ReadAllText(path)!=content)throw new Exception("Reinstalled client failed to restore its VPS deck: "+UltraDeckCloud.Status);
                UnityEngine.Debug.Log("CLOUD_DECK_REINSTALL_PASSED authenticated upload and fresh-install restoration through HTTPS");
            }
            finally
            {
                await UltraServices.SignOut();
                if(previous!=null)File.WriteAllBytes(session,previous);
                if(File.Exists(path))File.Delete(path);
                if(File.Exists(metadata))File.Delete(metadata);
            }
        }
    }
}
