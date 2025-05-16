using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace PalaceBuddy;

// Every function in this class, except the constructor and Dispose, is expected to be run on the Framework thread
public class Buddy : IDisposable
{
    private class BuddyFloorState
    {
        public bool PassageActive;
        public bool TransferActive;
        public bool SafetyActive;
        public int FloorNumber = -1;
    }

    private bool _disposed = false;
    private BuddyFloorState? FloorState = null;

    public bool Enabled => FloorState != null;
    public bool PassageActive => FloorState?.PassageActive ?? false;
    public bool TransferActive => FloorState?.TransferActive ?? false;
    public bool SafetyActive => FloorState?.SafetyActive ?? false;
    public int FloorNumber => FloorState?.FloorNumber ?? -1;

    private readonly Regex _passageRegex;
    private readonly Regex _floorRegex;
    private readonly string _introMessage;
    private readonly string _transferMessage;
    private readonly string _safetyMessage;
    private readonly string _sightMessage;

    private const ushort DeepDungeonChatTypeId = 2105; // XivChatType

    private const int PassageLogMessageId = 7245; // The ### is activated!
    private const int FloorLogMessageId = 7270; // Floor ##
    private const int TransferLogMessageId = 7248; // Transference initiated!
    private const int IntroLogMessageId = 7249; // The current duty uses an independent levelling system.
    private const int SafetyLogMessageId = 7255; // All the traps on this floor have disappeared!
    private const int SightLogMessageId = 7256; // The map for this floor has been revealed!

    // There's no need for this data since the log message is only used for passage objects anyway
    //private readonly static int[] PassageEObjIds = [2007188, 2009507, 2013287];
    // Tracking activation traps by their activation message won't work well in a party context + because of latency
    //private readonly static int[] TrapLogMessageIds = [7224, 7225, 7226, 7227, 7228, 9210, 10278];

    private readonly static ETerritoryType[] DDTerritoryTypes = Enum.GetValues<ETerritoryType>();

    // DeepDungeonTracker/Common/NodeUtility.cs
    private readonly static Regex NumberRegex = new Regex("\\d+");

    private unsafe static (bool, int) MapFloorNumber()
    {
        var addon = (AtkUnitBase*)DalamudService.GameGui?.GetAddonByName("DeepDungeonMap", 1)!;
        if (addon == null)
            return (false, -1);

        for (var i = 0; i < addon->UldManager.NodeListCount; i++)
        {
            var textNode = addon->UldManager.NodeList[i]->GetAsAtkTextNode();
            if (textNode == null)
                continue;

            if (int.TryParse(NumberRegex.Match(textNode->NodeText.ToString()).Value, out var number))
                return (true, number);
            break;
        }
        return (false, -1);
    }

    public Buddy()
    {
        // Chat messages
        var LogMessageSheet = DalamudService.DataManager.GetExcelSheet<LogMessage>();

        var passageSeString = LogMessageSheet.GetRow(PassageLogMessageId).Text.ToDalamudString();
        var passageRegex = "";
        foreach (var payload in passageSeString.Payloads)
        {
            passageRegex += payload switch
            {
                ITextProvider text => text.Text,
                _ => "(.+?)"
            };
        }
        _passageRegex = new($"^{passageRegex}$");

        var floorSeString = LogMessageSheet.GetRow(FloorLogMessageId).Text.ToDalamudString();
        var floorRegex = "";
        foreach (var payload in floorSeString.Payloads)
        {
            floorRegex += payload switch
            {
                ITextProvider text => text.Text,
                _ => "(\\d+?)"
            };
        }
        _floorRegex = new($"^{floorRegex}$");

        _transferMessage = LogMessageSheet.GetRow(TransferLogMessageId).Text.ExtractText();
        _introMessage = LogMessageSheet.GetRow(IntroLogMessageId).Text.ExtractText();
        _safetyMessage = LogMessageSheet.GetRow(SafetyLogMessageId).Text.ExtractText();
        _sightMessage = LogMessageSheet.GetRow(SightLogMessageId).Text.ExtractText();

        DalamudService.ClientState.TerritoryChanged += OnTerritoryChanged;
    }

    public void Initialize()
    {
        // Check initial state
        OnTerritoryChanged(DalamudService.ClientState.TerritoryType);
    }

    public void Enable()
    {
        if (_disposed || FloorState != null) return;
        DalamudService.Log.Debug("Buddy.Enable");
        DalamudService.ChatGui.ChatMessage += OnChatMessage;
        FloorState = new();
        CheckMapFloorNow();
        // XXX: After checking the map floor, it may result in Disable() being called immediately
    }

    public void Disable()
    {
        if (FloorState == null) return;
        DalamudService.Log.Debug("Buddy.Disable");
        DalamudService.ChatGui.ChatMessage -= OnChatMessage;
        FloorState = null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DalamudService.Log.Debug("Buddy.Dispose");
            Disable();
            DalamudService.ClientState.TerritoryChanged -= OnTerritoryChanged;
            _disposed = true;
        }
    }

    private void CheckMapFloorNow()
    {
        var (mapFloorResult, mapFloorNum) = MapFloorNumber();
        if (mapFloorResult)
        {
            DalamudService.Log.Debug($"Retrieved map floor number: {mapFloorNum}");
            OnFloorChangeMessage(mapFloorNum);
        }
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if ((ushort)type != DeepDungeonChatTypeId) return;

        var msg = message.TextValue;

        if (msg == _transferMessage) OnTransferMessage();
        else if (msg == _introMessage) OnIntroMessage();
        else if (msg == _safetyMessage) OnSafetyMessage();
        else if (msg == _sightMessage) OnSightMessage();
        else
        {
            var floorMatch = _floorRegex.Match(msg);
            if (floorMatch.Success && floorMatch.Groups.Count >= 2) OnFloorChangeMessage(int.Parse(floorMatch.Groups[1].ValueSpan));

            var passageMatch = _passageRegex.Match(msg);
            if (passageMatch.Success) OnPassageMessage();
        }
    }

    // Transference initiated!
    private void OnTransferMessage()
    {
        if (FloorState == null) return;
        DalamudService.Log.Debug("Buddy.OnTransferMessage");
        FloorState.TransferActive = true;
    }

    // The current duty uses an independent levelling system.
    private void OnIntroMessage()
    {
        if (FloorState == null) return;
        DalamudService.Log.Debug("Buddy.OnIntroMessage");
        CheckMapFloorNow();
        // Last chance fallback -- we don't really need the exact floor number anyway
        if (FloorNumber == -1)
            OnFloorChangeMessage(1);
    }

    // All the traps on this floor have disappeared!
    private void OnSafetyMessage()
    {
        if (FloorState == null) return;
        DalamudService.Log.Debug("Buddy.OnSafetyMessage");
        // Safety should hide all trap markers
        FloorState.SafetyActive = true;
    }

    // The map for this floor has been revealed!
    private void OnSightMessage()
    {
        DalamudService.Log.Debug("Buddy.OnSightMessage");
        // Sight is effectively the same as Safety. Visible traps will be marked by general rules anyway
        OnSafetyMessage();
    }

    // Floor ##
    private void OnFloorChangeMessage(int floor)
    {
        DalamudService.Log.Debug($"Buddy.OnFloorChangeMessage({floor})");
        if (floor == FloorNumber) return;
        FloorState = new() { FloorNumber = floor };

        // TODO: Eliminate nearby traps in the home room if possible

        // No traps on boss floors
        bool isBossFloor = (floor % 10 == 0) || (DalamudService.ClientState.TerritoryType == (ushort)ETerritoryType.EurekaOrthos_91_100 && floor == 99);
        if (isBossFloor)
            Disable();
    }

    // The ## of Passage is activated!
    private void OnPassageMessage()
    {
        if (FloorState == null) return;
        DalamudService.Log.Debug("Buddy.OnPassageMessage");
        FloorState.PassageActive = true;
    }

    // TerritoryKind=31 for deep dungeon
    private void OnTerritoryChanged(ushort territoryType)
    {
        DalamudService.Log.Debug("Buddy.OnTerritoryChanged");
        if (DDTerritoryTypes.Contains((ETerritoryType)territoryType))
            Enable();
        else
            Disable();
    }
}
