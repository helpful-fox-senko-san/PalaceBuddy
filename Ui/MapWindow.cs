using Dalamud.Interface.Windowing;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using System;
using System.Numerics;
using Dalamud.Interface.Textures;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using RoomFlags = FFXIVClientStructs.FFXIV.Client.Game.InstanceContent.InstanceContentDeepDungeon.RoomFlags;
using Dalamud.Interface.Textures.TextureWraps;

namespace PalaceBuddy.Ui;

// This draws a ghost version of unrevealed rooms over the deep dungeon map
public partial class MapWindow : Window
{
    private bool _initialized = false;

    private ISharedImmediateTexture _roomTex;
    private ISharedImmediateTexture _passageTex;
    private ISharedImmediateTexture _passageOpenTex;
    private ISharedImmediateTexture _returnTex;
    private ISharedImmediateTexture _returnOpenTex;
    private ISharedImmediateTexture _candleTex;

    public MapWindow() : base("##PalaceBuddyMap")
    {
        Flags |= ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoMove
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoInputs;

        _roomTex = DalamudService.TextureProvider.GetFromGame("ui/uld/DeepDungeonNaviMap_Rooms_hr1.tex");
        _passageTex = DalamudService.TextureProvider.GetFromGameIcon(60907);
        _passageOpenTex = DalamudService.TextureProvider.GetFromGameIcon(60908);
        _returnTex = DalamudService.TextureProvider.GetFromGameIcon(60905);
        _returnOpenTex = DalamudService.TextureProvider.GetFromGameIcon(60906);
        _candleTex = DalamudService.TextureProvider.GetFromGameIcon(63988);
    }

    public unsafe void DrawMap()
    {
        // Position ourselves over the deep dungeon map
        Position = Plugin.MapAddonHelper.MapPosition;
        Size = Plugin.MapAddonHelper.MapSize;

        if (!Plugin.Buddy.Enabled)
            return;

        // Won't work if not in deep dungeon
        var eventFramework = EventFramework.Instance();
        if (eventFramework == null)
            return;
        var instanceContent = eventFramework->GetInstanceContentDeepDungeon();
        if (instanceContent == null)
            return;

        // 12 steps on each meter
        bool passageOpen = instanceContent->PassageProgress >= 11;
        bool returnOpen = instanceContent->ReturnProgress >= 11;

        // This array contains the floor layout
        // (chest data is not available until the room is revealed :c)
        var mapData = instanceContent->MapData;

        // Only handle standard deep dungeon floor types
        // Anything else should be a big room or a boss
        if (instanceContent->LayoutInitializationType > 2)
            return;

        // Calculate true map bounds and revealed map bounds (for re-centering logic)
        int mapMinX = 4;
        int mapMaxX = 0;
        int mapMinY = 4;
        int mapMaxY = 0;
        int revealedMinX = 4;
        int revealedMaxX = 0;
        int revealedMinY = 4;
        int revealedMaxY = 0;
        for (int i = 0; i < 25; ++i)
        {
            int x = i % 5;
            int y = i / 5;
            byte roomByte = (byte)mapData[i];
            if (((ushort)roomByte & 0x0F) != 0)
            {
                mapMinX = int.Min(x, mapMinX);
                mapMaxX = int.Max(x, mapMaxX);
                mapMinY = int.Min(y, mapMinY);
                mapMaxY = int.Max(y, mapMaxY);
            }
            if (mapData[i].HasFlag(RoomFlags.Revealed))
            {
                revealedMinX = int.Min(x, revealedMinX);
                revealedMaxX = int.Max(x, revealedMaxX);
                revealedMinY = int.Min(y, revealedMinY);
                revealedMaxY = int.Max(y, revealedMaxY);
            }
        }

        // Panic -- one of the dimensions is zero
        if (mapMaxX < mapMinX || mapMaxY < mapMinY)
            return;
        if (revealedMaxX < revealedMinX || revealedMaxY < revealedMinY)
            return;

        // Use the difference between the real map and the revealed map
        // to re-center the revealed map, so our ghost nodes can fit corectly
        // (This has to be in unscaled pixels)
        Plugin.MapAddonHelper.MapShift = new Vector2(
            (mapMaxX + mapMinX - revealedMaxX - revealedMinX) * -44f / 2f,
            (mapMaxY + mapMinY - revealedMaxY - revealedMinY) * -44f / 2f
        );

        // Size of a room, as drawn with scaling
        float roomSizeX = Plugin.MapAddonHelper.MapSize.X / 5f;
        float roomSizeY = Plugin.MapAddonHelper.MapSize.Y / 5f;

        // This should calculate the scale value of the map addon
        var scale = new Vector2(roomSizeX / 44f, roomSizeY / 44f);

        // Size (in tiles) of the revealed map vs the full unrevealed map
        var mapCols = 1 + mapMaxX - mapMinX;
        var mapRows = 1 + mapMaxY - mapMinY;

        // Top-left corner of our re-centered map
        float mapOffX = (Plugin.MapAddonHelper.MapSize.X - mapCols * roomSizeX) / 2;
        float mapOffY = (Plugin.MapAddonHelper.MapSize.Y - mapRows * roomSizeY) / 2;
        
        // Always request all of these textures to avoid flicker
        var roomTexWrap = _roomTex.GetWrapOrEmpty();
        var passageWrap = _passageTex.GetWrapOrEmpty();
        var passageOpenWrap = _passageOpenTex.GetWrapOrEmpty();
        var returnWrap = _returnTex.GetWrapOrEmpty();
        var returnOpenWrap = _returnOpenTex.GetWrapOrEmpty();
        
        // Actually draw the unrevealed tiles, mimicing the game's appearance
        for (int i = 0; i < 25; ++i)
        {
            int roomX = i % 5;
            int roomY = i / 5;
            ushort roomData = (ushort)mapData[i];
            int tileNum = roomData & 0x0F;
            if (tileNum == 0 || mapData[i].HasFlag(RoomFlags.Revealed))
                continue;

            int tilesetCol = tileNum % 4;
            int tilesetRow = tileNum / 4;

            // Tileset coordinates are all 2x scaled due to being a high res texture
            float tilesetX = 2f + tilesetCol * 48f;
            float tilesetY = 2f + tilesetRow * 48f;
            var tileSize = new Vector2(roomSizeX, roomSizeY);
            var tileUv = new Vector2(tilesetX / 192f, tilesetY / 192f);
            var tileUv2 = new Vector2((tilesetX + 44f) / 192f, (tilesetY + 44f) / 192f);

            var tint = new Vector4(Plugin.Configuration.MapRoomTint.W);
            tint *= Plugin.Configuration.MapRoomTint;

            var tilePos = new Vector2(
                mapOffX - (mapMinX - roomX) * roomSizeX,
                mapOffY - (mapMinY - roomY) * roomSizeY
            );
            ImGui.SetCursorPos(tilePos);
            ImGui.Image(roomTexWrap.Handle, tileSize, tileUv, tileUv2, tint);

            IDalamudTextureWrap? img = null;

            if (mapData[i].HasFlag(RoomFlags.Passage))
                img = (passageOpen ? passageOpenWrap : passageWrap);
            else if (mapData[i].HasFlag(RoomFlags.Return))
                img = (returnOpen ? returnOpenWrap : returnWrap);
            else if (((ushort)mapData[i] & 0x100) == 0x100) // Candle
                img = _candleTex.GetWrapOrEmpty();

            // This icon is scaled down to 65% size
            if (img != null)
            {
                ImGui.SetCursorPos(new(
                    tilePos.X + (6f + 16f * 0.35f) * scale.X,
                    tilePos.Y + (12f + 16f * 0.35f) * scale.Y
                ));
                ImGui.Image(img.Handle, new Vector2(32f * 0.65f, 32f * 0.65f) * scale);
            }
        }
    }

    public override void PreOpenCheck()
    {
        base.PreOpenCheck();

        if (Plugin.Buddy.Enabled || IsOpen)
            Plugin.MapAddonHelper.Update();

        bool shouldOpen = Plugin.Buddy.Enabled
            && Plugin.Configuration.ShowUnrevealedMap
            && Plugin.MapAddonHelper.Visible;
        
        if (!IsOpen && shouldOpen)
            IsOpen = true;
        else if (IsOpen && !shouldOpen)
            IsOpen = false;
    }

    public override void Draw()
    {
        try
        {
            DrawMap();
        }
        catch (Exception ex)
        {
            DalamudService.Log.Error(ex, "MapWindow.Draw");
        }
    }
}
