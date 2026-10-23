using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using System;
using System.Diagnostics;

namespace MDPro3.Duel
{
    public class MessageDispatcher
    {
        public readonly LogMessage log;
        public readonly VoiceMessage voice;
        public readonly DuelMessage duel;

        public bool playerResponed;

        public MessageDispatcher()
        {
            log = new(this);
            voice = new(this);
            duel = new(this);
        }

        private Package lastPackage;

        public async UniTask Process(Package p)
        {
            if (p.Function != (int)GameMessage.Retry)
                lastPackage = p;

            playerResponed = false;

            try
            {
                await log.Process(p);
            } 
            catch(Exception e) { UnityEngine.Debug.LogException(e); }
            try
            {
                await voice.Process(p);
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
            try
            {
                await duel.Process(p);
            }
            catch (Exception e) { UnityEngine.Debug.LogException(e); }
        }

        public async UniTask RetryMessage()
        {
            if (lastPackage == null)
                return;
            await Process(lastPackage);
        }

        public void Dispose()
        {
            log.Dispose();
            voice.Dispose();
            duel.Dispose();
        }
    }
}
