using System;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;

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
                ImGui.TextUnformatted($"Buddy.Enabled: {Plugin.Buddy.Enabled}");
                if (Plugin.Buddy.Enabled)
                {
                    var pos = Plugin.Buddy.PlayerPosition;
                    ImGui.TextUnformatted($"PlayerPosition {pos.X:0.0000},{pos.Y:0.0000},{pos.Z:0.0000}");
                    ImGui.TextUnformatted($"FloorNumber: {Plugin.Buddy.FloorNumber}");

                    ImGui.TextUnformatted($"SafetyActive: {Plugin.Buddy.SafetyActive}");

                    ImGui.TextUnformatted($"LastGoldCofferTargetId: {Plugin.Buddy.LastGoldCofferTargetId:X}");
                    ImGui.TextUnformatted($"LastSilverCofferTargetId: {Plugin.Buddy.LastSilverCofferTargetId:X}");

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
    }
}
