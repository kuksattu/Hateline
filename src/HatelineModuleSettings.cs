namespace Celeste.Mod.Hateline;

[SettingName("Hateline Settings")]
public class HatelineModuleSettings : EverestModuleSettings
{
    private bool _enabled = true;
    public bool Enabled {
        get => _enabled;
        set { _enabled = value;
            HatelineModule.ReloadHat();
        }
    }

    [SettingIgnore]
    public string SelectedHat { get; set; } = HatelineModule.HAT_NONE;
    
    private bool _allowMapChanges = true;
    public bool AllowMapChanges
    {
        get => _allowMapChanges;
        set { _allowMapChanges = value;
            HatelineModule.ReloadHat();
        }
    }

    public bool HatScaling { get; set; } = true;

    private int _crownX = 0;
    [SettingRange(-100, 100, false)]
    public int CrownX
    {
        get => _crownX;
        set { _crownX = value; 
            HatelineModule.ReloadHat();
        }
    }
    
    private int _crownY = 0;
    [SettingRange(-100, 100, false)]
    public int CrownY
    {
        get => _crownY;
        set { _crownY = value;
            HatelineModule.ReloadHat();
        }
    }
}