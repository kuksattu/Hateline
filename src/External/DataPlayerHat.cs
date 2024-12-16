using System;
using System.IO;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.DataTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Hateline.CelesteNet;

public class DataPlayerHat : DataType<DataPlayerHat>
{
    static DataPlayerHat() { DataID = $"Hateline_PlayerHat1"; }
    
    public DataPlayerInfo Player;

    public int CrownX = HatelineModule.Settings.CrownX;
    public int CrownY = HatelineModule.Settings.CrownY;
    
    public string SelectedHat = HatelineModule.Settings.SelectedHat;

    public byte[] CnetTexture;
    
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
        //Console.WriteLine("Read ProtocolVersion");
        CrownX = reader.ReadInt32();
        //Console.WriteLine("Read CrownX");
        CrownY = reader.ReadInt32();
        //Console.WriteLine("Read CrownY");
        SelectedHat = reader.ReadString();
        //Console.WriteLine("Read SelectedHat");
        
        
        int len = reader.ReadInt32();
        
        byte[] bytes = reader.ReadBytes(len);
        Logger.Log(nameof(HatelineModule), $"Read bytes {string.Join(',', bytes)}");
        CnetTexture = len==0 ? null : bytes;
        
        // Texture2D newTexture = Texture2D.FromStream(Engine.Instance.GraphicsDevice, new MemoryStream(bytes));
        // VirtualTexture vtex = VirtualContent.CreateTexture("randomPath2", newTexture.Width, newTexture.Height, Color.Red);
        // vtex.Texture = newTexture;
        // CnetTexture = new MTexture(vtex);

    }

    protected override void Write(CelesteNetBinaryWriter writer)
    {
        writer.Write(CelesteNetSupport.PROTOCOL_VERSION);
        //Console.WriteLine("Wrote ProtocolVersion");
        writer.Write(CrownX);
        //Console.WriteLine("Wrote CrownX");
        writer.Write(CrownY);
        //Console.WriteLine("Wrote CrownY");
        writer.Write(SelectedHat);
        //Console.WriteLine("Wrote SelectedHat");

        byte[] bytes = Array.Empty<byte>();
        try
        {
            bytes = File.ReadAllBytes(HatelineModule.Settings.CnetHatPath + ".png");
        }
        catch { }
        
        writer.Write(bytes.Length);
        Logger.Log(nameof(HatelineModule), $"Wrote bytelength {bytes.Length}");
        writer.Write(bytes);
        Logger.Log(nameof(HatelineModule), $"Wrote bytes {string.Join(',', bytes)}");
    }
    
}