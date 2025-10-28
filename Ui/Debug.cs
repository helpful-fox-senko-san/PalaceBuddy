using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System;
using System.Threading.Tasks;

namespace PalaceBuddy.Ui;

public partial class ConfigWindow : Window, IDisposable
{
    private string? _dbResult = null;
    private Task<string>? _dbResultTask = null;

    private void DrawDebug()
    {
        using var tabId = ImRaii.PushId("Debug");

        ImGui.TextUnformatted($"Buddy.Enabled: {Plugin.Buddy.Enabled}");
        if (Plugin.Buddy.Enabled)
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("Test Disable"))
                Plugin.Buddy.Disable();
            var pos = Plugin.Buddy.PlayerPosition;
            ImGui.TextUnformatted($"PlayerPosition {pos.X:0.0000},{pos.Y:0.0000},{pos.Z:0.0000}");
            ImGui.TextUnformatted($"FloorNumber: {Plugin.Buddy.FloorNumber}");

            ImGui.Text("  Test");
            foreach (int i in (int[])[1, 10, 41, 81, 99, 191, 200])
            {
                ImGui.SameLine();
                if (ImGui.SmallButton($"{i}"))
                    Plugin.Buddy.OnFloorChangeMessage(i);
            }

            ImGui.TextUnformatted($"SightActive: {Plugin.Buddy.SightActive}");
            ImGui.SameLine();
            using (ImRaii.Disabled(Plugin.Buddy.SightActive))
                if (ImGui.SmallButton("Test Sight"))
                    Plugin.Buddy.OnSightMessage();
            ImGui.TextUnformatted($"SafetyActive: {Plugin.Buddy.SafetyActive}");
            ImGui.SameLine();
            using (ImRaii.Disabled(Plugin.Buddy.SafetyActive))
                if (ImGui.SmallButton("Test Safety"))
                    Plugin.Buddy.OnSafetyMessage();

            ImGui.TextUnformatted($"LastGoldCofferTargetId: {Plugin.Buddy.LastGoldCofferTargetId:X}");
            ImGui.TextUnformatted($"LastSilverCofferTargetId: {Plugin.Buddy.LastSilverCofferTargetId:X}");

            using (ImRaii.Disabled(!Plugin.Configuration.MarkChestContents))
                if (ImGui.SmallButton("Test Full Inventory"))
                {
                    Plugin.Buddy.OnFullMessage("pomander of strength");
                    Plugin.Buddy.OnFullMessage("piece of mazeroot incense");
                }
        }
        else
        {
            ImGui.SameLine();
            if (ImGui.SmallButton("Test Enable"))
                Plugin.Buddy.Enable();
        }

        ImGui.Separator();

        ImGui.TextUnformatted($"# Trap Elements: {Plugin.CircleRenderer.NumTrapElements}");
        ImGui.TextUnformatted($"# Active Labels: {Plugin.CircleRenderer.NumActiveLabels}");

        ImGui.Separator();

        if (_dbResult == null)
        {
            if (_dbResultTask == null)
                _dbResultTask = Plugin.LocationLoader.CheckDB();
            if (_dbResultTask.IsCompleted)
            {
                _dbResult = _dbResultTask.Result;
                _dbResultTask.Dispose();
                _dbResultTask = null;
            }
            ImGui.TextUnformatted($"DB check in progress...");
        }
        else
        {
            ImGui.TextUnformatted($"DB: {_dbResult}");
        }

        using (ImRaii.Disabled(_dbResultTask != null))
        {
            if (ImGui.Button("Check Again"))
                _dbResult = null;
        }
    }
}
