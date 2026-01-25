using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace PalaceBuddy.Ui;

// Tracks the position of the DeepDungeonMap addon
public partial class MapAddonHelper
{
    public bool Visible { get; private set; }
    public Vector2 Position { get; private set; }
    public Vector2 Size { get; private set; }

    // Absolute screen position of the minimap area inside of the addon
    public Vector2 MapPosition { get; private set; }
    public Vector2 MapSize { get; private set; }

    // Each frame this is written to, the map positioning will be adjusted to this offset
    // This is so we can re-center the map and ensure ghost rooms will fit
    public Vector2 MapShift { get; set; } = Vector2.Zero;

    public unsafe void Update()
    {
        try
        {
            var safeAddon = DalamudService.GameGui.GetAddonByName("DeepDungeonMap");
            
            if (safeAddon.IsNull)
                return;

            AtkUnitBase* addon = (AtkUnitBase*)safeAddon.Address;

            Visible = addon->IsVisible;
            
            if (!Visible)
                return;

            var windowScale = addon->Scale;
            Position = new(addon->X, addon->Y);
            Size = new(addon->GetScaledWidth(true), addon->GetScaledHeight(true));

            Vector2 mapShiftToApply = Vector2.Zero;

            // Only keep map shifting applied for one frame
            mapShiftToApply = MapShift;
            MapShift = Vector2.Zero;

            // We assume the map is 220x220 at coordinates 20,72
            // (We can't read the coordinates from memory since we mutate them)
            // TODO: Read the layout file?
            var mapNode = addon->GetNodeById(29);
            if (mapNode != null)
            {
                var oldPos = new Vector2(mapNode->X, mapNode->Y);
                var newPos = new Vector2(
                    20f + mapShiftToApply.X,
                    72f + mapShiftToApply.Y
                );

                if (oldPos != newPos)
                {
                    mapNode->X = newPos.X;
                    mapNode->Y = newPos.Y;
                    mapNode->DrawFlags |= 0x01;
                }
            }

            MapPosition = new(
                Position.X + 20f * windowScale,
                Position.Y + 72f * windowScale
            );

            MapSize = new(
                220f * windowScale,
                220f * windowScale
            );
        }
        catch (Exception ex)
        {
            DalamudService.Log.Error(ex, "MapAddonHelper.Update");
        }
    }
}
