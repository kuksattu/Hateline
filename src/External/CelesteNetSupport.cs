using Monocle;

namespace Celeste.Mod.Hateline.CelesteNet;

public static class CelesteNetSupport
{
    public const int PROTOCOL_VERSION = 1;

    public static bool Loaded;

    public static CelesteNetHatComponent CNetComponent { get; private set; }

    public static void Load()
    {
        if (Loaded) return;
        Celeste.Instance.Components.Add(CNetComponent = new CelesteNetHatComponent(Celeste.Instance));
        Loaded = true;
        On.Celeste.Player.Added += OnPlayerAdded;
    }

    public static void Unload()
    {
        if (!Loaded) return;
        Celeste.Instance.Components.Remove(CNetComponent);
        CNetComponent.Dispose();
        CNetComponent = null;
        Loaded = false;
        On.Celeste.Player.Added -= OnPlayerAdded;
    }

    private static void OnPlayerAdded(On.Celeste.Player.orig_Added orig, Player self, Scene scene)
    {
        orig(self, scene);

        Logger.Log(LogLevel.Verbose, "Hateline", $"OnPlayerAdded: Calling SendPlayerHat of {CNetComponent}");

        CNetComponent?.SendPlayerHat();
    }
}