using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Hateline.CelesteNet;

public class DataCNetHat : DataType<DataCNetHat>
{
    static DataCNetHat() { DataID = $"Hateline_CNetHat"; }
    
    public DataPlayerInfo Player;

    // Possibly add the attributes from Sprites.xml to here so they can be synced.
    public byte[] CnetHatBytes;
    
    public override DataFlags DataFlags => DataFlags.CoreType;

    public override bool FilterHandle(DataContext ctx) => Player != null;
    
    public override MetaType[] GenerateMeta(DataContext ctx) => new MetaType[]
    {
        new MetaPlayerPrivateState(Player),
        new MetaBoundRef(DataType<DataPlayerInfo>.DataID, Player?.ID ?? uint.MaxValue, true),
    };

    public override void FixupMeta(DataContext ctx)
    {
        Player = Get<MetaPlayerPrivateState>(ctx);
        Get<MetaBoundRef>(ctx).ID = Player?.ID ?? uint.MaxValue;
    }

    protected override void Read(CelesteNetBinaryReader reader)
    {
        var protocolVersion = reader.ReadInt32();
        
        int len = reader.ReadInt32();
        byte[] bytes = reader.ReadBytes(len);
        CnetHatBytes = len==0 ? null : bytes;
        Logger.Log(nameof(HatelineModule), $"Read bytelength {bytes.Length}");
        Logger.Log(nameof(HatelineModule), $"Read bytes {string.Join(',', bytes)}");
    }

    protected override void Write(CelesteNetBinaryWriter writer)
    {
        writer.Write(CelesteNetSupport.PROTOCOL_VERSION);
        writer.Write(CnetHatBytes.Length);
        writer.Write(CnetHatBytes);
        
        Logger.Log(nameof(HatelineModule), $"Wrote bytelength {CnetHatBytes.Length}");
        Logger.Log(nameof(HatelineModule), $"Wrote bytes {string.Join(',', CnetHatBytes)}");
    }
    
    
    // Tucked away most of the cnet related code here so it's all easily contained in one file.
    public void CreateCnetHat(HatComponent hatComp, uint playerID, string selectedHat)
    { // This could possibly be included into the HatComponents createhat method when the try clause fails.
        
        // We update the crownsprite here again, since it got set to none during the initial creation since the try clause threw an error.
        // Otherwise the cnet code will create a new hat every frame since the received hat won't be none.
        // Maybe the cnet hat should be a separate component. I'm not good enough at coding to figure these out.
        hatComp.CrownSprite = selectedHat;
        
        if (CnetHatBytes == null) return;
        string id = $"{playerID}/hatelinehat";
        var mtex = CreateCnetHatTexture(CnetHatBytes);
        if (IsInvalidCNetHat(CnetHatBytes)) return;
        
        hatComp.animations[id] = new Sprite.Animation
        {
            Delay = 0,
            Frames = new []{mtex},
            Goto = new Chooser<string>(id, 1f)
        };
        hatComp.Play(id);
    }
    
    public static MTexture CreateCnetHatTexture(byte[] bytes)
    { // We create a new MTexture from an array of bytes that represent the image data.
        var memStream = new MemoryStream(bytes);
        Texture2D newTexture = Texture2D.FromStream(Engine.Instance.GraphicsDevice, memStream);
        VirtualTexture vtex = VirtualContent.CreateTexture("hatelinerandompaththatprobablyneedstobeuniquebutidontknow", newTexture.Width, newTexture.Height, Color.Red);
        vtex.Texture = newTexture;
        return new MTexture(vtex);
    }

    public static bool IsInvalidCNetHat(byte[] bytes)
    {
        MTexture mtex = CreateCnetHatTexture(bytes);
        bool isInvalid = mtex.Width > 32 || mtex.Height > 32 || bytes.Length > 1024 || !HatelineModule.Settings.ReceiveCnetHats;
        mtex.Unload();
        return isInvalid;
    }
}