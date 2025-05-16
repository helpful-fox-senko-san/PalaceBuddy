using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace PalaceBuddy.Ui;

public class DebugWindow : Window, IDisposable
{
    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public DebugWindow()
        : base("PalaceBuddy###PalaceBuddy", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(300, 100),
        };
    }

    public void Dispose()
    {
        _dbResultTask?.Dispose();
    }

    private string? _dbResult = null;
    private Task<string>? _dbResultTask = null;

    public override void Draw()
    {
        // Normally a BeginChild() would have to be followed by an unconditional EndChild(),
        // ImRaii takes care of this after the scope ends.
        // This works for all ImGui functions that require specific handling, examples are BeginTable() or Indent().
        using (var child = ImRaii.Child("SomeChildWithAScrollbar", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                if (DalamudService.ClientState.LocalPlayer != null)
                {
                    var pos = DalamudService.ClientState.LocalPlayer.Position;
                    ImGui.TextUnformatted($"position {pos.X:0.0000},{pos.Y:0.0000},{pos.Z:0.0000}");
                }

                // Example for quarrying Lumina directly, getting the name of our current area.
                var territoryId = DalamudService.ClientState.TerritoryType;
                if (DalamudService.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(territoryId, out var territoryRow))
                {
                    ImGui.TextUnformatted($"We are currently in ({territoryId}) \"{territoryRow.PlaceName.Value.Name.ExtractText()}\"");
                }
                else
                {
                    ImGui.TextUnformatted("Invalid territory.");
                }

                ImGui.Separator();

                ImGui.TextUnformatted($"Buddy.Enabled: {Plugin.Buddy.Enabled}");
                ImGui.TextUnformatted($"Buddy.FloorNumber: {Plugin.Buddy.FloorNumber}");
                ImGui.TextUnformatted($"Buddy.TransferActive: {Plugin.Buddy.TransferActive}");
                ImGui.TextUnformatted($"Buddy.SafetyActive: {Plugin.Buddy.SafetyActive}");
                ImGui.TextUnformatted($"Buddy.PassageActive: {Plugin.Buddy.PassageActive}");

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
    }
}
