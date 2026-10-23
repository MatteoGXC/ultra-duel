using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MDPro3.Net;
using MDPro3.Duel.YGOSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MDPro3
{
    public static class SoloAiRuntimeAudit
    {
        // Opt-in packaged-player regression: every shipped list plays two
        // mirrored opening turns through the real TCP server and card scripts.
        public static IEnumerator Run(string output)
        {
            WindBot.Program.InitDatas("cards.cdb");
            var results=new List<object>();int failures=0;
            var rows=JObject.Parse(File.ReadAllText("Data/solo-published-decks.json"))["decks"] as JArray;
            int index=0;
            int target=Array.IndexOf(Environment.GetCommandLineArgs(),"--solo-ai-long")>=0?7:3;
            foreach(var row in rows)
            {
                index++;bool genesys=(string)row["mode"]=="Genesys";
                DuelFormat.Set(genesys,(int?)row["cap"]??100);
                int list=genesys?-1:BanlistManager.GetIndexByName(DuelFormat.TcgBanlist.Name);
                int port=17911;
                YgoServer.StartServer(port+" "+list+" 1 0 "+DuelFormat.MasterRule+" F F 8000 5 1 0 0");
                yield return new WaitForSecondsRealtime(.3f);
                var clients=new WindBot.Game.GameClient[2];Exception error=null;
                try
                {
                    for(int seat=0;seat<2;seat++)
                    {
                        var info=new WindBot.WindBotInfo{Name="Audit "+seat,Deck=(string)row["executor"],
                            DeckFile=Path.GetFullPath(Path.Combine("Data",(string)row["file"])),
                            Dialog="Universal.EN",Host="127.0.0.1",Port=port,Chat=false,NativeChainProtocol=true};
                        clients[seat]=new WindBot.Game.GameClient(info);clients[seat].Start();
                    }
                }catch(Exception e){error=e;}
                float until=Time.realtimeSinceStartup+180;
                while(error==null && Time.realtimeSinceStartup<until)
                {
                    try {foreach(var client in clients)client.Tick();}
                    catch(Exception e){error=e;}
                    var behavior=clients[0]?.Behavior;
                    if(behavior!=null && (behavior.AuditTurns>=target || (behavior.AuditTurns>0 && !clients[0].Connection.IsConnected)))break;
                    yield return null;
                }
                var b=clients[0]?.Behavior;
                int turns=b?.AuditTurns??0, summons=b?.AuditSummons??0,activations=b?.AuditActivations??0;
                bool passed=error==null && turns>=2 && (summons>0 || activations>0);
                if(!passed)failures++;
                results.Add(new{name=(string)row["name"],mode=(string)row["mode"],file=(string)row["file"],turns,summons,activations,passed,error=error?.ToString()});
                Debug.Log("SOLO_AI_AUDIT "+index+"/"+rows.Count+" "+(string)row["name"]+" "+(string)row["mode"]+" turns="+turns+" summons="+summons+" effects="+activations+" "+(passed?"PASS":"FAIL "+error));
                foreach(var client in clients)client?.Connection?.Close();
                yield return new WaitForSecondsRealtime(.1f);
                YgoServer.StopServer();
                yield return new WaitForSecondsRealtime(.2f);
                File.WriteAllText(Path.Combine(output,"solo-ai-audit.json"),JsonConvert.SerializeObject(results,Formatting.Indented));
            }
            File.WriteAllText(Path.Combine(output,failures==0?"passed.txt":"failed.txt"),rows.Count+" published opponents exercised; "+failures+" failures.");
            Application.Quit(failures==0?0:2);
        }
    }
}
