using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using Dalamud.Interface;

namespace PalaceBuddy.Ui;

public partial class ConfigWindow : Window, IDisposable
{
    public enum Tabs
    {
        Config,
        Debug
    }

    private static Vector4 RGB(float r, float g, float b)
    {
        return new Vector4(r / 255.0f, g / 255.0f, b / 255.0f, 255.0f);
    }

    private Vector4 _colorGrey = RGB(160, 160, 160);

    private Tabs? _forceOpenTab;

    // Set to true when closed automatically
    // If set, then the window is re-opened automatically too
    public bool AutoClosed = false;

    public ConfigWindow() : base("PalaceBuddy Configuration##PalaceBuddyConfig")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 300),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose()
    {
        _dbResultTask?.Dispose();
    }

    public void OpenTab(Tabs? tab)
    {
        if (IsOpen)
            BringToFront();
        else
            IsOpen = true;

        _forceOpenTab = tab;
    }

    public void DrawConfigWindow()
    {
        var doTab = (string name, Tabs tabId, Action drawfn) => {
            bool forceOpenFlag = (_forceOpenTab == tabId);

            using var tabItem = forceOpenFlag ? ImRaii.TabItem(name, ref forceOpenFlag, ImGuiTabItemFlags.SetSelected) : ImRaii.TabItem(name);

            if (tabItem.Success)
                drawfn();

            if (forceOpenFlag)
                _forceOpenTab = null;
        };

        using var tabs = ImRaii.TabBar("ConfigWindowTabs");
        doTab("Config", Tabs.Config, DrawConfig);
        doTab("Debug", Tabs.Debug, DrawDebug);
    }

    public override void Draw()
    {
        try
        {
            DrawConfigWindow();
        }
        catch (Exception ex)
        {
            DalamudService.Log.Error(ex, "Draw");
        }
    }

    private void DrawHelpText(string helpText)
    {
        ImGui.SameLine();
        using (DalamudService.PluginInterface.UiBuilder.IconFontFixedWidthHandle.Push())
            ImGui.TextColored(ImGui.GetColorU32(ImGuiCol.TextDisabled), FontAwesomeIcon.QuestionCircle.ToIconString());
        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                using (ImRaii.TextWrapPos(ImGui.GetFontSize() * 35.0f))
                    ImGui.TextWrapped(helpText);
            }
        }
    }
}
