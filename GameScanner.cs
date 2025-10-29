using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace PalaceBuddy;

public class GameTrap
{
    public uint ObjectId;
    public uint DataId;
    public Vector3 Position;
}

public class GameScanner
{
    private readonly HashSet<uint> _seenTraps = new();

    public GameScanner()
    {
    }

    public IEnumerable<GameTrap> FindNewTraps(IFramework framework, uint[] trapIds)
    {
        foreach (var obj in DalamudService.ObjectTable.EventObjects)
        {
            var eobj = obj as IEventObj;
            if (eobj == null)
                continue;

            var id = eobj.EntityId;
            if (_seenTraps.Contains(id))
                continue;
            _seenTraps.Add(id);

            var dataId = eobj.BaseId;

            if (!trapIds.Contains(dataId))
                continue;

            var trap = new GameTrap()
            {
                DataId = eobj.BaseId,
                ObjectId = id,
                Position = Plugin.RoundPos(eobj.Position)
            };

            yield return trap;
        }
    }

    public void ClearSeenTraps()
    {
        _seenTraps.Clear();
    }
}
