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
        // Base track pattern (single lane along X axis)
        var baseTiles = new TilePlacement[]
        {
            new TilePlacement { Type = TileType.Flat, GridX = 0, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Flat, GridX = 1, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Ramp, GridX = 2, GridY = 0, GridZ = 0, Facing = CardinalDirection.East },
            new TilePlacement { Type = TileType.Flat, GridX = 3, GridY = 1, GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Ramp, GridX = 4, GridY = 0, GridZ = 0, Facing = CardinalDirection.West },
            new TilePlacement { Type = TileType.Flat, GridX = 5, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Flat, GridX = 6, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
        };

        // Replicate across 3 lanes: Z = -1, 0, +1
        var result = new System.Collections.Generic.List<TilePlacement>();
        for (int lane = -1; lane <= 1; lane++)
        {
            foreach (var tile in baseTiles)
            {
                result.Add(new TilePlacement
                {
                    Type = tile.Type,
                    GridX = tile.GridX,
                    GridY = tile.GridY,
                    GridZ = tile.GridZ + lane,
                    Facing = tile.Facing
                });
            }
        }

        // Gravel run-off: 2 rows each side (Z=-3..-2, Z=+2..+3) across X=-1..7
        for (int x = -1; x <= 7; x++)
        {
            for (int z = -3; z <= -2; z++)
                result.Add(new TilePlacement { Type = TileType.Gravel, GridX = x, GridY = 0, GridZ = z, Facing = CardinalDirection.North });
            for (int z = 2; z <= 3; z++)
                result.Add(new TilePlacement { Type = TileType.Gravel, GridX = x, GridY = 0, GridZ = z, Facing = CardinalDirection.North });
        }

        // Gravel at track ends: X=-1 and X=7 for Z=-1..+1
        for (int z = -1; z <= 1; z++)
        {
            result.Add(new TilePlacement { Type = TileType.Gravel, GridX = -1, GridY = 0, GridZ = z, Facing = CardinalDirection.North });
            result.Add(new TilePlacement { Type = TileType.Gravel, GridX = 7, GridY = 0, GridZ = z, Facing = CardinalDirection.North });
        }

        // Fence border ring
        // West wall (X=-2): Z=-4..+4, wall faces East (+X) → canonical wall on -Z rotated to face +X = East
        for (int z = -4; z <= 4; z++)
            result.Add(new TilePlacement { Type = TileType.Fence, GridX = -2, GridY = 0, GridZ = z, Facing = CardinalDirection.East });

        // East wall (X=8): Z=-4..+4, wall faces West (-X)
        for (int z = -4; z <= 4; z++)
            result.Add(new TilePlacement { Type = TileType.Fence, GridX = 8, GridY = 0, GridZ = z, Facing = CardinalDirection.West });

        // North wall (Z=-4): X=-1..7, wall faces South (+Z) → canonical wall on -Z rotated 180° = South
        for (int x = -1; x <= 7; x++)
            result.Add(new TilePlacement { Type = TileType.Fence, GridX = x, GridY = 0, GridZ = -4, Facing = CardinalDirection.South });

        // South wall (Z=+4): X=-1..7, wall faces North (-Z) → canonical wall on -Z, no rotation = North
        for (int x = -1; x <= 7; x++)
            result.Add(new TilePlacement { Type = TileType.Fence, GridX = x, GridY = 0, GridZ = 4, Facing = CardinalDirection.North });

        return result.ToArray();
    }
}
