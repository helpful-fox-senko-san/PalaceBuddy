using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using PalaceBuddy.Ui;
using ECommons;
using System;
using System.Numerics;

namespace PalaceBuddy;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/pbud";

    public static Configuration Configuration { get; private set; } = null!;
    public static WindowSystem WindowSystem = new("PalaceBuddy");

    public static Buddy Buddy { get; private set; } = null!;
    public static LocationLoader LocationLoader { get; private set; } = null!;
    public static CircleRenderer CircleRenderer { get; private set; } = null!;
    public static GameScanner GameScanner { get; private set; } = null!;

    public static ConfigWindow ConfigWindow { get; private set; } = null!;

    public static ushort OverrideTerritory { get; set; }
    public static bool IsOverrideTerritory => OverrideTerritory != 0;
    public static ushort TerritoryType =>
        IsOverrideTerritory ? OverrideTerritory : DalamudService.ClientState.TerritoryType;

    public static Vector3 RoundPos(Vector3 pos)
    {
        return new Vector3(
            float.Round(pos.X * 100.0f, MidpointRounding.AwayFromZero) / 100.0f,
            float.Round(pos.Y * 100.0f, MidpointRounding.AwayFromZero) / 100.0f,
            float.Round(pos.Z * 100.0f, MidpointRounding.AwayFromZero) / 100.0f
        );
    }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        DalamudService.Initialize(pluginInterface);
        ECommonsMain.Init(pluginInterface, this, Module.SplatoonAPI);

        try
        {
            Configuration = DalamudService.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        }
        catch (Exception ex)
        {
            DalamudService.Log.Error(ex, "Loading configuration failed, resetting");
            Configuration = new Configuration();
        }

        Buddy = new Buddy();
        LocationLoader = new LocationLoader();
        CircleRenderer = new CircleRenderer();
        GameScanner = new GameScanner();

        ConfigWindow = new ConfigWindow();
        WindowSystem.AddWindow(ConfigWindow);

        DalamudService.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            ShowInHelp = false
        });

        DalamudService.PluginInterface.UiBuilder.Draw += Draw;
        DalamudService.PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;

        if (DalamudService.PluginInterface.Reason == PluginLoadReason.Reload)
            ConfigWindow.OpenTab(ConfigWindow.Tabs.Debug);

        DalamudService.Framework.RunOnFrameworkThread(() =>
        {
            Buddy.Initialize();
        });
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();

        CircleRenderer.Dispose();
        Buddy.Dispose();

        DalamudService.CommandManager.RemoveHandler(CommandName);
        ECommonsMain.Dispose();
    }

    private static void Draw()
    {
        WindowSystem.Draw();
    }

    public static void OpenMainUi()
    {
        ConfigWindow.IsOpen = true;
    }

    private void OnCommand(string command, string args)
    {
        ConfigWindow.Toggle();
    }
}
