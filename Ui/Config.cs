using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using Dalamud.Interface.Colors;

namespace PalaceBuddy.Ui;

public partial class ConfigWindow : Window, IDisposable
{
    private string? _assemblyVersion = null;

    private void DrawConfig()
    {
        using var tabId = ImRaii.PushId("Config");

        bool b;
        float f;
        Vector4 vec4;

        if (!Plugin.CircleRenderer.IsConnected)
        {
            ImGui.TextColoredWrapped(ImGuiColors.DalamudYellow, "Warning: Splatoon must be installed and enabled for the plugin to function!");
            ImGui.Separator();
        }

        b = Plugin.Configuration.ShowTrapLocations;
        if (ImGui.Checkbox("Show Possible Trap Locations", ref b))
        {
            Plugin.Configuration.ShowTrapLocations = b;
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        DrawHelpText("Display known trap locations inside of deep dungeons.");

        b = Plugin.Configuration.ShowUnrevealedMap;
        if (ImGui.Checkbox("Show All Rooms On Map", ref b))
        {
            Plugin.Configuration.ShowUnrevealedMap = b;
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        DrawHelpText("Display a dimmed version of the full deep dungeon map.\nNote: Chests will not be displayed until a room is revealed.");

        b = Plugin.Configuration.MarkChestContents;
        if (ImGui.Checkbox("Mark Chest Contents", ref b))
        {
            Plugin.Configuration.MarkChestContents = b;
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        DrawHelpText("Mark the contents of chests if your inventory is full. (SOLO ONLY)");

        ImGui.Separator();

        ImGui.Text("Trap Location Appearance");

        vec4 = Plugin.Configuration.TrapColor;
        if (ImGui.ColorEdit4("##Trap Color", ref vec4,
            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.DisplayRgb | ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.DisplayMask))
        {
            Plugin.Configuration.TrapColor = vec4;
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        ImGui.SameLine(102.0f * ImGuiHelpers.GlobalScale);
        ImGui.Text("Color");

        f = Plugin.Configuration.TrapThickness;
        ImGui.SetNextItemWidth(90.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("##Trap Thickness", ref f, 1.0f, 5.0f, "%.1f"))
        {
            Plugin.Configuration.TrapThickness = f;
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        ImGui.SameLine(102.0f * ImGuiHelpers.GlobalScale);
        ImGui.Text("Thickness");

        f = Plugin.Configuration.TrapDrawDistance;
        ImGui.SetNextItemWidth(90.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("##Trap Draw Distance", ref f, 20.0f, 80.0f, "%.0f"))
        {
            Plugin.Configuration.TrapDrawDistance = f;
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        ImGui.SameLine(102.0f * ImGuiHelpers.GlobalScale);
        ImGui.Text("Draw Distance");

        b = Plugin.Configuration.ShowSightedTraps;
        if (ImGui.Checkbox("Show Sighted Traps", ref b))
        {
            Plugin.Configuration.ShowSightedTraps = b;
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        DrawHelpText("Continue to indicate traps revealed by a pomander of sight.");

        ImGui.Separator();

        ImGui.Text("Minimap Appearance");

        f = Plugin.Configuration.MapRoomTint.W;
        ImGui.SetNextItemWidth(90.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.SliderFloat("##Map Opacity", ref f, 0.4f, 0.8f, "%.2f"))
        {
            Plugin.Configuration.MapRoomTint = Plugin.Configuration.MapRoomTint with { W = f };
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        ImGui.SameLine(102.0f * ImGuiHelpers.GlobalScale);
        ImGui.Text("Room Opacity");

        vec4 = Plugin.Configuration.MapRoomTint;
        if (ImGui.ColorEdit4("##Room Color", ref vec4,
            ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.DisplayRgb | ImGuiColorEditFlags.DisplayHex | ImGuiColorEditFlags.DisplayMask | ImGuiColorEditFlags.NoAlpha))
        {
            Plugin.Configuration.MapRoomTint = vec4;
            Plugin.Configuration.Save();
            Plugin.Buddy.ForceUpdate();
        }
        ImGui.SameLine(102.0f * ImGuiHelpers.GlobalScale);
        ImGui.Text("Room Color Tint");

        ImGui.Separator();

        if (_assemblyVersion == null)
            _assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!.ToString();

        using var pushColor = ImRaii.PushColor(ImGuiCol.Text, _colorGrey);
        ImGuiHelpers.CenteredText($"Version {_assemblyVersion}");
    }
}
