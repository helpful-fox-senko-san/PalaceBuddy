using Dalamud.Configuration;
using System;
using System.Numerics;

namespace PalaceBuddy;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ShowTrapLocations { get; set; } = true;
    public bool ShowSightedTraps { get; set; } = true;
    public bool MarkChestContents { get; set; } = true;

    public float TrapDrawDistance { get; set; } = 40.0f;
    public Vector4 TrapColor { get; set; } = new(0.8f, 0.0f, 0.0f, 0.667f);
    public float TrapThickness { get; set; } = 2.5f;

    // the below exist just to make saving less cumbersome
    public void Save()
    {
        DalamudService.PluginInterface.SavePluginConfig(this);
    }
}
