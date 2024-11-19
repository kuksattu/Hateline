using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Hateline.CelesteNet
{
    public class CelesteNetHatComponent : GameComponent
    {
        protected readonly CelesteNetClientModule _clientModule;
        private Delegate _initHook;
        private Delegate _disposeHook;

        public CelesteNetClient Client => _clientModule.Context?.Client;

        public CelesteNetHatComponent(Game game) : base(game)
        {
            _clientModule = (CelesteNetClientModule)Everest.Modules.FirstOrDefault(m => m is CelesteNetClientModule);
            if (_clientModule == null) throw new Exception("CelesteNet not loaded???");
            
            EventInfo startEvent = typeof(CelesteNetClientContext).GetEvent("OnStart");
            if (startEvent.EventHandlerType.GenericTypeArguments.FirstOrDefault() == typeof(CelesteNetClientContext))
                startEvent.AddEventHandler(null, _initHook = (Action<CelesteNetClientContext>)(_ => clientStart()));
            else
                startEvent.AddEventHandler(null, _initHook = (Action<object>)(_ => clientStart()));

            EventInfo disposeEvent = typeof(CelesteNetClientContext).GetEvent("OnDispose");
            if (disposeEvent.EventHandlerType.GenericTypeArguments.FirstOrDefault() == typeof(CelesteNetClientContext))
                disposeEvent.AddEventHandler(null, _disposeHook = (Action<CelesteNetClientContext>)(_ => clientDisposed()));
            else
                disposeEvent.AddEventHandler(null, _disposeHook = (Action<object>)(_ => clientDisposed()));
            
        }
        
        private void clientDisposed()
        {
        }

        private void clientStart()
        {
            try
            {
                SendPlayerHat();
                Logger.Log(LogLevel.Verbose, "Hateline", $"clientStart: Called SendPlayerHat at CelesteNetClientContext.OnStart with {Client}");
            } catch
            {
                // if this threw an exception, CelesteNetClient.Start would actually fail
                Logger.Log(LogLevel.Warn, "Hateline", $"clientStart: Something went wrong while trying to SendPlayerHat at CelesteNetClientContext.OnStart");
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Engine.Scene == null)
                return;

            foreach (Ghost ghost in Engine.Scene.Tracker.GetEntities<Ghost>())
            {
                DataPlayerHat hatData = null;
                CelesteNetSupport.CNetComponent?.Client?.Data?.TryGetBoundRef(ghost.PlayerInfo, out hatData);
                if (hatData == null) continue;
                
                string selHat = hatData?.SelectedHat ?? HatelineModule.HAT_NONE;
                HatComponent hatComp = ghost.Get<HatComponent>();
                
                switch (hatComp)
                {
                    case null when hatData.SelectedHat != HatelineModule.HAT_NONE:
                        ghost.Add(new HatComponent(selHat, hatData?.CrownX, hatData?.CrownY));
                        continue;
                    case null:
                        continue;
                }
                
                if (hatData.CrownX != hatComp.CrownX || hatData.CrownY != hatComp.CrownY || hatData.SelectedHat != hatComp.CrownSprite)
                {
                    hatComp.RemoveSelf();
                    hatComp = new HatComponent(selHat, hatData?.CrownX, hatData?.CrownY);
                    ghost.Add(hatComp);
                }
                
                if (selHat == HatelineModule.HAT_NONE)
                {
                    ghost.Remove(hatComp);
                }
            }
        }

        public void SendPlayerHat(string forceSend = null)
        {
            if (Client == null) return;
            
            if(!HatelineModule.Instance.ShouldShowHat && string.IsNullOrEmpty(forceSend)) return;

            string hatToSend = HatelineModule.Instance.VisibleHat;
            if (!string.IsNullOrEmpty(forceSend))
                hatToSend = forceSend;

            Client.SendAndHandle(new DataPlayerHat
            {
                CrownX = HatelineModule.Instance.CurrentX,
                CrownY = HatelineModule.Instance.CurrentY,
                SelectedHat = hatToSend,
                Player = Client.PlayerInfo,
            });
        }
    }
}