using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons.SplatoonAPI;

namespace PalaceBuddy;

public class CircleRenderer : IDisposable
{
    private readonly Dictionary<string, List<Element>> _elements = new();
    private Element[] _trapElements = [];

#region Element Styling
    private Element CreateTrapElement(Vector3 Location)
    {
        return new Element(ElementType.CircleAtFixedCoordinates)
        {
            refX = Location.X,
            refY = Location.Z,
            refZ = Location.Y,

            Filled = false,
            radius = 1.7f,
            color = 0x9F0000FF,
            thicc = 2
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
        _elements.Clear();
    }

    // Create the trap elements after loading locations from the database
    public void SetLocations(List<Vector3> locationList, Vector3 playerPos)
    {
        if (_trapElements.Length > 0)
            Splatoon.RemoveDynamicElements("PalaceBuddy.Traps");

        _trapElements = new Element[locationList.Count];
        for (int i = 0; i < locationList.Count; ++i)
            _trapElements[i] = CreateTrapElement(locationList[i]);

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
            var elemPos = new Vector3(elem.refX, elem.refZ, elem.refY);
            var dist = float.Abs(Vector3.Distance(elemPos, playerPos));

            uint alpha = 0x9F;
            alpha = ((uint)(alpha * float.Sqrt(1.0f - dist / 40f))) << 24;
            if (elem.color != 0x00000000)
                elem.color = (elem.color & 0x00FFFFFF) | alpha;

            elem.thicc = float.Max(1.0f, (1.0f - dist / 60f) * 2.5f);
            if (elem.thicc < 0f)
                elem.thicc = 0f;
        }
    }

    public void Redraw()
    {
    }

    public void Dispose()
    {
        RemoveAllElements();
    }

    private void RemoveAllElements()
    {
        if (_trapElements.Length > 0)
            Splatoon.RemoveDynamicElements("PalaceBuddy.Traps");

        foreach (var elem in _elements)
            Splatoon.RemoveDynamicElements($"PalaceBuddy.{elem.Key}");
    }
}