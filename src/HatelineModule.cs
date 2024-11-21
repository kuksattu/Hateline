using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Celeste.Mod.Hateline.CelesteNet;
using FMOD.Studio;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.Hateline;

public class HatelineModule : EverestModule
{
    public static HatelineModule Instance { get; private set; }
    
    public override Type SettingsType => typeof(HatelineModuleSettings);
    
    public static HatelineModuleSettings Settings => (HatelineModuleSettings)Instance._Settings;
    
    public override Type SessionType => typeof(HatelineModuleSession);
    
    public static HatelineModuleSession Session => (HatelineModuleSession)Instance._Session;
    
    public bool HasForcedHat => Settings.AllowMapChanges && Session?.MapForcedHat != null;
    public bool ShouldShowHat => HasForcedHat || Settings.Enabled;
    
    public string? VisibleHat => HasForcedHat ? Session.MapForcedHat : Settings.SelectedHat;
    public string? CurrentHat => ShouldShowHat ? VisibleHat : HAT_NONE;
    
    public const string HAT_NONE = "none";
    
    public int CurrentX => HasForcedHat ? Session.mapsetX : Settings.CrownX;
    public int CurrentY => HasForcedHat ? Session.mapsetY : Settings.CrownY;
    
    public static HashSet<string> hats = new();
    
    public Dictionary<string, Dictionary<string, string>> HatAttributes = new();
    
    // Define custom hat attributes here
    private readonly List<(string, string)> _hatAttributeDefinitions = new()
    { // The name of the attribute and its default value
        ("scaling", "true"),
        ("flip", "true"),
    };

    public HatelineModule() { Instance = this; }

    public override void Load()
    {
        typeof(GravityHelperImports).ModInterop();
        On.Celeste.Player.Added += HookPlayerAdded;
        On.Celeste.Level.LoadLevel += HookLoadLevel;
        On.Celeste.Player.ResetSprite += HookPlayerResetSprite;
        
        if (Everest.Modules.Any(m => m.Metadata.Name == "CelesteNet.Client")) 
            CelesteNetSupport.Load();
    }

    public override void Unload()
    {
        On.Celeste.Player.Added -= HookPlayerAdded;
        On.Celeste.Level.LoadLevel -= HookLoadLevel;
        On.Celeste.Player.ResetSprite -= HookPlayerResetSprite;

        if (Everest.Modules.Any(m => m.Metadata.Name == "CelesteNet.Client")) 
            CelesteNetSupport.Unload();
    }
    
    private static void HookLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerintro, bool isfromloader)
    {
        RegisterHats();
        orig(self, playerintro, isfromloader);
    }
    
    private static void HookPlayerResetSprite(On.Celeste.Player.orig_ResetSprite orig, Player self, PlayerSpriteMode mode)
    {
        orig(self, mode);
        self.Get<HatComponent>()?.RemoveSelf();
        self.Add(new HatComponent(Instance.CurrentHat, Instance.CurrentX, Instance.CurrentY));
    }
    
    private static void HookPlayerAdded(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
    {
        orig(self, scene);
        self.Get<HatComponent>()?.RemoveSelf();
        self.Add(new HatComponent(Instance.CurrentHat, Instance.CurrentX, Instance.CurrentY));
    }
    
    
    public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot)
    {
        base.CreateModMenuSection(menu, inGame, snapshot);
        HatelineSettingsUI.CreateMenu(menu, inGame);
    }
    
    public static void ReloadHat()
    {
        HatComponent playerHatComponent = Engine.Scene?.Tracker?.GetEntity<Player>()?.Get<HatComponent>();
        if (playerHatComponent is null) return;
        
        playerHatComponent.CreateHat(Instance.CurrentHat, true);
        playerHatComponent.SetPosition(Instance.CurrentX, Instance.CurrentY);
        
        CelesteNetSupport.CNetComponent?.SendPlayerHat();
    }

    public static void RegisterHats()
    {
        Instance.HatAttributes.Clear();
        hats.Clear();
        foreach (KeyValuePair<string, SpriteData> data in GFX.SpriteBank.SpriteData)
        {
            if (!data.Key.StartsWith("hateline_")) continue;
            
            // [9..] removes the hateline_ prefix
            string hatName = data.Key[9..];
            XmlNode node = data.Value.Sources[0].XML;
            Dictionary<string, string> attributesToAdd = new();
            
            foreach ((string name, string defaultValue) in Instance._hatAttributeDefinitions)
                attributesToAdd[name] = node.Attributes?[name]?.Value ?? defaultValue;
            
            Instance.HatAttributes[hatName] = attributesToAdd;
            
            if (hatName != HAT_NONE)
                hats.Add(hatName);
        }
    }
}