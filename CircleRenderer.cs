using System;
using System.Collections.Generic;
using System.Numerics;
using ECommons;
using ECommons.SplatoonAPI;

namespace PalaceBuddy;

public class CircleRenderer : IDisposable
{
    private struct TrapElement
    {
        public Vector3 Location;
        public uint Color;
        public float Thickness;
    }

    private readonly Dictionary<string, List<Element?>> _elements = new(4);
    private readonly List<(TrapElement Props, Element? Elem)> _dynamicTraps = new(4);

    private TrapElement[] _trapProperties = [];
    private Element?[] _trapElements = [];
    private readonly List<uint> _activeLabels = new();

    public int NumTrapElements => _trapElements.Length;
    public int NumActiveLabels => _activeLabels.Count;

    #region Element Styling
    private (TrapElement, Element?) CreateTrapElement(Vector3 location)
    {
        var trapElement = new TrapElement()
        {
            Location = location,
            Color = Plugin.Configuration.TrapColor.ToUint(),
            Thickness = 2f
        };

        Element? splatoonElement = null;

        try
        {
            splatoonElement = new Element(ElementType.CircleAtFixedCoordinates)
            {
                refX = location.X,
                refY = location.Z,
                refZ = location.Y,

                Filled = false,
                radius = 1.7f,
                color = trapElement.Color,
                thicc = trapElement.Thickness
            };
        }
        catch { }

        return (trapElement, splatoonElement);
    }

    private Element CreateChestLabelElement(uint objectId, string labelText)
    {
        return new Element(ElementType.CircleRelativeToActorPosition)
        {
            refActorType = RefActorType.IGameObjectWithSpecifiedAttribute,
            refActorComparisonType = RefActorComparisonType.ObjectID,
            refActorObjectID = objectId,
            onlyTargetable = true,

            overlayText = labelText,
            overlayTextColor = 0xFFAFEFFF,
            overlayBGColor = 0x80000000,
            overlayVOffset = 1.4f,

            Filled = false,
            radius = 0,
            color = 0,
            thicc = 0
        };
    }
    #endregion

    public bool IsConnected => Splatoon.IsConnected();

    public CircleRenderer()
    {
        Splatoon.SetOnConnect(OnConnect);
    }

    private void OnConnect()
    {
        DalamudService.Framework.RunOnTick(() =>
        {
            DalamudService.Log.Information("Re-building Splatoon elements");
            _elements.Clear();
            _dynamicTraps.Clear();

            for (int i = 0; i < _trapElements.Length; ++i)
                (_trapProperties[i], _trapElements[i]) = CreateTrapElement(_trapProperties[i].Location);

            Splatoon.AddDynamicElements("PalaceBuddy.Traps", _trapElements, -2);
        }, TimeSpan.FromSeconds(1));
    }

    // Create the trap elements after loading locations from the database
    public void SetLocations(List<Vector3> locationList, Vector3 playerPos)
    {
        if (!Splatoon.IsConnected())
            DalamudService.ChatGui.PrintError("[PalaceBuddy] Warning: Splatoon is not loaded!");

        if (_trapElements.Length > 0)
            Splatoon.RemoveDynamicElements("PalaceBuddy.Traps");

        DalamudService.Log.Verbose("CircleRenderer.SetLocations");

        _trapProperties = new TrapElement[locationList.Count];
        _trapElements = new Element[locationList.Count];
        for (int i = 0; i < locationList.Count; ++i)
            (_trapProperties[i], _trapElements[i]) = CreateTrapElement(locationList[i]);

        if (_trapElements.Length > 0)
            Splatoon.AddDynamicElements("PalaceBuddy.Traps", _trapElements, -2);

        UpdateLocations(playerPos);
    }

    // Create a new trap element dynamically, as visible ones are loaded in to view
    public void AddLocation(Vector3 location, Vector3 playerPos)
    {
        var (prop, elem) = CreateTrapElement(location);
        Splatoon.AddDynamicElement("PalaceBuddy.DynamicTraps", elem, -2);
        prop = UpdateSingleLocation(playerPos, prop, elem);
        _dynamicTraps.Add((prop, elem));
    }

    // Update the appearance of trap indicators based on the player position
    public void UpdateLocations(Vector3 playerPos)
    {
        if (!Splatoon.IsConnected())
            return;

        for (int i = 0; i < _trapElements.Length; ++i)
            _trapProperties[i] = UpdateSingleLocation(playerPos, _trapProperties[i], _trapElements[i]);

        for (int i = 0; i < _dynamicTraps.Count; ++i)
        {
            var trap = _dynamicTraps[i];
            _dynamicTraps[i] = trap with { Props = UpdateSingleLocation(playerPos, trap.Props, trap.Elem) };
        }
    }

    private static TrapElement UpdateSingleLocation(Vector3 playerPos, TrapElement props, Element? elem)
    {
        if (elem == null) return props;

        var elemPos = props.Location;
        var elemColor = props.Color;
        var elemThickness = props.Thickness;
        var dist = float.Abs(Vector3.Distance(elemPos, playerPos));

        var trapColor = Plugin.Configuration.TrapColor.ToUint();
        var trapThickness = Plugin.Configuration.TrapThickness;

        uint newColor = 0x000000FF;
        float newThickness = 1f;

        float drawDist = float.Max(Plugin.Configuration.TrapDrawDistance, 30.0f);
        float drawDistPlus = drawDist * 1.5f;

        if (dist <= drawDistPlus)
        {
            uint alpha = (trapColor & 0xFF000000U) >> 24;
            alpha = ((uint)(alpha * float.Sqrt(1.0f - dist / drawDist))) << 24;
            newColor = (trapColor & 0x00FFFFFF) | alpha;
            newThickness = float.Max(1.0f, (1.0f - dist / drawDistPlus) * trapThickness);
        }

        if (elemColor != newColor)
            elem.color = props.Color = newColor;

        if (elemThickness != newThickness)
            elem.thicc = props.Thickness = newThickness;

        return props;
    }

    public void ClearLocations()
    {
        if (_trapElements.Length > 0)
            Splatoon.RemoveDynamicElements("PalaceBuddy.Traps");
        _trapProperties = [];
        _trapElements = [];
    }

    public void AddChestLabel(uint objectId, string labelText)
    {
        if (_activeLabels.Contains(objectId)) return;

        if (!_elements.ContainsKey("ChestLabel"))
            _elements.Add("ChestLabel", new(16));

        var elem = CreateChestLabelElement(objectId, labelText);
        _elements["ChestLabel"].Add(elem);
        Splatoon.AddDynamicElement("PalaceBuddy.ChestLabel", elem, -2);
        _activeLabels.Add(objectId);
    }

    public void Dispose()
    {
        RemoveAllElements();
    }

    public void RemoveTemporaryElements(string? key = null)
    {
        if (key == null)
        {
            foreach (var elem in _elements)
                Splatoon.RemoveDynamicElements($"PalaceBuddy.{elem.Key}");
            _elements.Clear();
        }
        else
        {
            Splatoon.RemoveDynamicElements($"PalaceBuddy.{key}");
            if (_elements.ContainsKey(key))
                _elements[key].Clear();
        }

        if (key == null || key == "ChestLabel")
            _activeLabels.Clear();
    }

    public void RemoveTemporaryTraps()
    {
        Splatoon.RemoveDynamicElements($"PalaceBuddy.DynamicTraps");
        _dynamicTraps.Clear();
    }

    public void RemoveAllElements()
    {
        ClearLocations();
        RemoveTemporaryTraps();
        RemoveTemporaryElements();
    }
}