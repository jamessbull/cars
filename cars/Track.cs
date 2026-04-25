using System;
using System.Collections.Generic;

/// <summary>
/// Pure C# representation of the track layout. Answers terrain queries from the car.
/// No Godot dependency — fully unit-testable.
/// </summary>
public class Track
{
    private readonly Dictionary<(int gx, int gy, int gz), TileType> _tiles;

    public Track(IEnumerable<TilePlacement> placements)
    {
        _tiles = new Dictionary<(int, int, int), TileType>();
        foreach (var p in placements)
            _tiles[(p.GridX, p.GridY, p.GridZ)] = p.Type;
    }

    /// <summary>
    /// Rolling drag multiplier to apply each physics frame at the given world position.
    /// Values below 1.0 bleed off speed; lower = more resistance.
    /// </summary>
    public float DragAt(float x, float y, float z)
    {
        return TileAt(x, y, z) switch
        {
            TileType.Gravel => CarConstants.GravelDragFactor,
            TileType.Grass  => CarConstants.GrassDragFactor,
            _               => CarConstants.RollingDragFactor
        };
    }

    /// <summary>
    /// Lateral grip correction factor at the given world position.
    /// 1.0 = full grip (all sideways velocity cancelled), 0.0 = no correction.
    /// </summary>
    public float GripAt(float x, float y, float z)
    {
        return TileAt(x, y, z) switch
        {
            TileType.Grass => CarConstants.GrassGripFactor,
            _              => 1.0f
        };
    }

    /// <summary>
    /// Find the tile the car at (x, y, z) is resting on.
    /// Searches downward from the car's approximate grid height so that bridge
    /// tiles are found before any grass or road tiles directly beneath them.
    /// </summary>
    private TileType? TileAt(float x, float y, float z)
    {
        int gx = (int)MathF.Floor(x / TileGeometry.CellWidth);
        int gz = (int)MathF.Floor(z / TileGeometry.CellDepth);
        int carGridY = (int)MathF.Floor(y / TileGeometry.CellHeight);

        for (int gy = carGridY; gy >= 0; gy--)
        {
            if (_tiles.TryGetValue((gx, gy, gz), out var type))
                return type;
        }
        return null;
    }
}
