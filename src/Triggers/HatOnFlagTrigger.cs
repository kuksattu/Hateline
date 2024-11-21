using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Hateline.Triggers;

[CustomEntity("Hateline/HatOnFlagTrigger")]
public class HatOnFlagTrigger : Trigger
{
    public string hat;
    public string flag;
    
    public bool inverted;
    
    public int hatX;
    public int hatY;

    public HatOnFlagTrigger(EntityData data, Vector2 offset)
        : base(data, offset)
    {
        flag = data.Attr("flag", "");
        inverted = data.Bool("inverted", false);
        hat = data.Attr("hat", "flower");
        hatX = data.Int("hatX", 0);
        hatY = data.Int("hatY", 0);
    }

    public override void OnEnter(Player player)
    {
        base.OnEnter(player);
        
        bool flagVal = SceneAs<Level>().Session.GetFlag(flag);
        if (inverted) flagVal = !flagVal;

        if (!flagVal || !HatelineModule.Settings.AllowMapChanges) return;

        HatelineModule.Session.MapForcedHat = hat;
        HatelineModule.Session.mapsetX = hatX;
        HatelineModule.Session.mapsetY = hatY;
        HatelineModule.ReloadHat();
    }
}