public enum CardinalDirection
{
    North,
    East,
    South,
    West
}

public struct TilePlacement
{
    public TileType Type;
    public int GridX;
    public int GridY;
    public int GridZ;
    public CardinalDirection Facing;
}

public static class TrackLayout
{
    /// <summary>
    /// Returns the GridMap orientation index for a cardinal direction.
    /// These correspond to Godot's orthogonal basis indices (_ortho_bases table).
    /// Only pure Y-axis rotations are used (Y axis stays up).
    /// </summary>
    public static int GetOrientationIndex(CardinalDirection direction)
    {
        return direction switch
        {
            CardinalDirection.North => 0,   // Identity
            CardinalDirection.East => 22,   // -90° around Y: maps -Z to +X
            CardinalDirection.South => 10,  // 180° around Y: maps -Z to +Z
            CardinalDirection.West => 16,   // +90° around Y: maps -Z to -X
            _ => 0
        };
    }

    /// <summary>
    /// Returns the "The Hump" demo track: 7 tiles along X axis with a ramp up, flat top, and ramp down.
    /// </summary>
    public static TilePlacement[] GetDemoTrack()
    {
        return new TilePlacement[]
        {
            // X=0: Flat approach
            new TilePlacement { Type = TileType.Flat, GridX = 0, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
            // X=1: Flat approach
            new TilePlacement { Type = TileType.Flat, GridX = 1, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
            // X=2: Ramp facing East (rises from Y=0 toward +X direction)
            new TilePlacement { Type = TileType.Ramp, GridX = 2, GridY = 0, GridZ = 0, Facing = CardinalDirection.East },
            // X=3: Flat at top of hump
            new TilePlacement { Type = TileType.Flat, GridX = 3, GridY = 1, GridZ = 0, Facing = CardinalDirection.North },
            // X=4: Ramp facing West at Y=0 (high end at Y=1 on -X side, low end at Y=0 on +X side)
            new TilePlacement { Type = TileType.Ramp, GridX = 4, GridY = 0, GridZ = 0, Facing = CardinalDirection.West },
            // X=5: Flat exit
            new TilePlacement { Type = TileType.Flat, GridX = 5, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
            // X=6: Flat exit
            new TilePlacement { Type = TileType.Flat, GridX = 6, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
        };
    }
}
