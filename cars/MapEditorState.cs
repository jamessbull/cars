using System;
using System.Collections.Generic;

/// <summary>
/// All editor state and non-Godot logic. Testable without the engine.
/// </summary>
public class MapEditorState
{
    private static readonly TileType[] TileTypes = (TileType[])Enum.GetValues(typeof(TileType));
    private static readonly CardinalDirection[] Facings = (CardinalDirection[])Enum.GetValues(typeof(CardinalDirection));

    public const int MaxGridY = 30;

    /// <summary>
    /// Number of raw grid cells per one tile height (TileThickness / CellHeight, rounded).
    /// Scroll moves the grid by this many cells so each step = exactly one tile thickness.
    /// </summary>
    public static readonly int HeightStep = (int)System.MathF.Round(TileGeometry.TileThickness / TileGeometry.CellHeight);

    public Dictionary<(int x, int y, int z), TilePlacement> PlacedTiles { get; } = new();
    public TileType SelectedType { get; private set; } = TileType.Flat;
    public CardinalDirection SelectedFacing { get; private set; } = CardinalDirection.North;
    public int GridY { get; private set; } = 0;

    /// <summary>Upsert the current tile type/facing at (gx, gy, gz).</summary>
    public void PlaceTile(int gx, int gy, int gz)
    {
        var key = (gx, gy, gz);
        PlacedTiles[key] = new TilePlacement
        {
            Type   = SelectedType,
            GridX  = gx,
            GridY  = gy,
            GridZ  = gz,
            Facing = SelectedFacing
        };
    }

    /// <summary>Remove the tile at (gx, gy, gz) if it exists.</summary>
    public void DeleteTile(int gx, int gy, int gz)
    {
        PlacedTiles.Remove((gx, gy, gz));
    }

    /// <summary>Cycle tile type by delta (+1/-1), wrapping around.</summary>
    public void CycleTileType(int delta)
    {
        int idx = Array.IndexOf(TileTypes, SelectedType);
        idx = ((idx + delta) % TileTypes.Length + TileTypes.Length) % TileTypes.Length;
        SelectedType = TileTypes[idx];
    }

    /// <summary>Directly select a specific tile type.</summary>
    public void SelectTileType(TileType type)
    {
        SelectedType = type;
    }

    /// <summary>Cycle facing by delta (+1/-1) through N→E→S→W, wrapping around.</summary>
    public void CycleFacing(int delta)
    {
        int idx = Array.IndexOf(Facings, SelectedFacing);
        idx = ((idx + delta) % Facings.Length + Facings.Length) % Facings.Length;
        SelectedFacing = Facings[idx];
    }

    /// <summary>Move the active grid Y level by delta, clamped to [0, MaxGridY].</summary>
    public void ChangeGridY(int delta)
    {
        GridY = Math.Clamp(GridY + delta, 0, MaxGridY);
    }

    /// <summary>
    /// Convert a world-space XZ position to grid cell coordinates.
    /// Uses Floor because Godot's GridMap (cell_center_x/z = true) places each tile
    /// at gx*CellWidth + CellWidth/2, so cell gx occupies [gx*CW, (gx+1)*CW).
    /// </summary>
    public static (int gx, int gz) WorldToGridXZ(float wx, float wz)
    {
        int gx = (int)MathF.Floor(wx / TileGeometry.CellWidth);
        int gz = (int)MathF.Floor(wz / TileGeometry.CellDepth);
        return (gx, gz);
    }

    /// <summary>Serialize all placed tiles to JSON.</summary>
    public string ToJson() => TrackLayoutLoader.SaveToJson(PlacedTiles.Values);
}
