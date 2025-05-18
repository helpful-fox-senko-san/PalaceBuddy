using System;
using System.Collections.Generic;
using System.Numerics;
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

    private readonly Dictionary<string, List<Element>> _elements = new();
    private TrapElement[] _trapProperties = [];
    private Element[] _trapElements = [];
    private readonly List<uint> _activeLabels = new();

    public int NumTrapElements => _trapElements.Length;
    public int NumActiveLabels => _activeLabels.Count;

#region Element Styling
    private (TrapElement, Element) CreateTrapElement(Vector3 location)
    {
        var trapElement = new TrapElement()
        {
            Location = location,
            Color = 0x9F0000FF,
            Thickness = 2f
        };

        var splatoonElement = new Element(ElementType.CircleAtFixedCoordinates)
        {
            refX = location.X,
            refY = location.Z,
            refZ = location.Y,

            Filled = false,
            radius = 1.7f,
            color = trapElement.Color,
            thicc = trapElement.Thickness
        };

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

    public void EnableRender()
    {
    }

    // Clean up and remove all elements
    public void DisableRender()
    {
        RemoveAllElements();
    }

    // Create the trap elements after loading locations from the database
    public void SetLocations(List<Vector3> locationList, Vector3 playerPos)
    {
        if (_trapElements.Length > 0)
            Splatoon.RemoveDynamicElements("PalaceBuddy.Traps");

        _trapProperties = new TrapElement[locationList.Count];
        _trapElements = new Element[locationList.Count];
        for (int i = 0; i < locationList.Count; ++i)
        {
            (_trapProperties[i], _trapElements[i]) = CreateTrapElement(locationList[i]);
        }

        if (_trapElements.Length > 0)
            Splatoon.AddDynamicElements("PalaceBuddy.Traps", _trapElements, -2);

        UpdateLocations(playerPos);
    }

    // Update the appearance of trap indicators based on the player position
    public void UpdateLocations(Vector3 playerPos)
    {
        for (int i = 0; i < _trapElements.Length; ++i)
        {
            var elem = _trapElements[i];
            var elemPos = _trapProperties[i].Location;
            var elemColor = _trapProperties[i].Color;
            var elemThickness = _trapProperties[i].Thickness;
            var dist = float.Abs(Vector3.Distance(elemPos, playerPos));

            uint newColor = 0x000000FF;
            float newThickness = 1f;

            if (dist <= 60f)
            {
                uint alpha = 0x9F;
                alpha = ((uint)(alpha * float.Sqrt(1.0f - dist / 40f))) << 24;
                newColor = (elemColor & 0x00FFFFFF) | alpha;
                newThickness = float.Max(1.0f, (1.0f - dist / 60f) * 2.5f);
            }

            if (elemColor != newColor)
                elem.color = _trapProperties[i].Color = newColor;

            if (elemThickness != newThickness)
                elem.thicc = _trapProperties[i].Thickness = newThickness;
        }
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
            _elements.Add("ChestLabel", new());

        DalamudService.Log.Debug("adding element");
        var elem = CreateChestLabelElement(objectId, labelText);
        _elements["ChestLabel"].Add(elem);
        Splatoon.AddDynamicElement("PalaceBuddy.ChestLabel", elem, -2);
        _activeLabels.Add(objectId);
    }

    public void Redraw()
    {
    }

    public void Dispose()
    {
        RemoveAllElements();
    }

    public void RemoveTemporaryElements()
    {
        foreach (var elem in _elements)
            Splatoon.RemoveDynamicElements($"PalaceBuddy.{elem.Key}");

        _elements.Clear();
        _activeLabels.Clear();
    }

    private void RemoveAllElements()
    {
        if (_trapElements.Length > 0)
            Splatoon.RemoveDynamicElements("PalaceBuddy.Traps");

        RemoveTemporaryElements();
    }
}