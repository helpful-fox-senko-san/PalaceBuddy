using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using PalaceBuddy.Ui;

namespace PalaceBuddy;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/pbud";

    public static Configuration Configuration { get; private set; } = null!;
    public static WindowSystem WindowSystem = new("PalaceBuddy");

    public static Buddy Buddy { get; private set; } = null!;
    public static LocationLoader LocationLoader { get; private set; } = null!;

    public static DebugWindow DebugWindow { get; private set; } = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        DalamudService.Initialize(pluginInterface);

        Configuration = DalamudService.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Buddy = new Buddy();
        LocationLoader = new LocationLoader();

        DebugWindow = new DebugWindow();
        WindowSystem.AddWindow(DebugWindow);

        DalamudService.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            ShowInHelp = false
        });

        DalamudService.PluginInterface.UiBuilder.Draw += Draw;
        DalamudService.PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;

        if (DalamudService.PluginInterface.Reason == PluginLoadReason.Reload)
            DebugWindow.IsOpen = true;

        DalamudService.Framework.RunOnFrameworkThread(() => {
            Buddy.Initialize();
        });
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();

        DebugWindow.Dispose();

        Buddy.Dispose();

        DalamudService.CommandManager.RemoveHandler(CommandName);
    }

    private static void Draw()
    {
        WindowSystem.Draw();
    }

    public static void OpenMainUi()
    {
        DebugWindow.IsOpen = true;
    }

    private void OnCommand(string command, string args)
    {
        DebugWindow.Toggle();
    }
}
