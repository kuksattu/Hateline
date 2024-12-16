using System;
using System.IO;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.Hateline.CelesteNet;

public class CelesteNetHatComponent : GameComponent
{
    protected readonly CelesteNetClientModule _clientModule;
    
    public CelesteNetClient Client => _clientModule.Context?.Client;

    public CelesteNetHatComponent(Game game) : base(game)
    {
        if (!Everest.Loader.TryGetDependency(HatelineModule.CelesteNetMetadata, out EverestModule cnet))
            throw new InvalidOperationException($"Attempted to instantiate {nameof(CelesteNetHatComponent)} while Celeste is not loaded.");
        _clientModule = (CelesteNetClientModule)cnet;

        CelesteNetClientContext.OnStart += ClientStart;
    }

    private void ClientStart(CelesteNetClientContext _)
    {
        try
        {
            SendPlayerHat();
            Logger.Log(LogLevel.Verbose, "Hateline", $"clientStart: Called SendPlayerHat at CelesteNetClientContext.OnStart with {Client}");
        } catch (Exception e)
        {
            // if this threw an exception, CelesteNetClient.Start would actually fail
            Logger.Log(LogLevel.Warn, "Hateline", "Something went wrong while trying to SendPlayerHat at CelesteNetClientContext.OnStart");
            Logger.Log(LogLevel.Warn, "Hateline", $"{e}");
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
            
            string selHat = hatData.SelectedHat ?? HatelineModule.HAT_NONE;
            HatComponent hatComp = ghost.Get<HatComponent>();
            
            if (hatComp == null)
            { // Check if ghost has acquired a hat, otherwise continue early.
                if (selHat != HatelineModule.HAT_NONE)
                {
                    ghost.Add(hatComp = new HatComponent(selHat, hatData.CrownX, hatData.CrownY));
                    HandleCnetHat(hatData, hatComp, ghost.PlayerInfo.ID);
                }

                continue;
            }
            
            if (selHat == HatelineModule.HAT_NONE)
            { // Remove ghost's hat if it's none
                ghost.Remove(hatComp);
                continue;
            }
            
            if (hatData.CrownX != hatComp.CrownX || hatData.CrownY != hatComp.CrownY || hatData.SelectedHat != hatComp.CrownSprite)
            { // Update ghost's hat if it changes
                hatComp.RemoveSelf();
                ghost.Add(hatComp = new HatComponent(selHat, hatData.CrownX, hatData.CrownY));
                HandleCnetHat(hatData, hatComp, ghost.PlayerInfo.ID);
            }
        }
    }

    private static void HandleCnetHat(DataPlayerHat hatData, HatComponent hatComp, uint playerID)
    {
        if (hatData.SelectedHat == "cnet_hat"  && hatData.CnetTexture != null)
        {
            Texture2D newTexture = Texture2D.FromStream(Engine.Instance.GraphicsDevice, new MemoryStream(hatData.CnetTexture));
            VirtualTexture vtex = VirtualContent.CreateTexture($"{playerID}/hatelinehat", newTexture.Width, newTexture.Height, Color.Red);
            vtex.Texture = newTexture;
            var mtex = new MTexture(vtex);
            hatComp.animations[$"{playerID}/hatelinehat"] = new Sprite.Animation()
            {
                Delay = 0,
                Frames = new []{mtex},
                Goto = new Chooser<string>($"{playerID}/hatelinehat", 1f)
            };
            hatComp.Play($"{playerID}/hatelinehat");
        }
    }

    public void SendPlayerHat(string forceSend = null)
    {
        if (Client == null) return;
        
        string hatToSend = HatelineModule.Instance.CurrentHat;
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