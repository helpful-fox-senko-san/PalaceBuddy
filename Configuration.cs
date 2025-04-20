using Dalamud.Configuration;
using System;

namespace PalaceBuddy;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        DalamudService.PluginInterface.SavePluginConfig(this);
    }
}
