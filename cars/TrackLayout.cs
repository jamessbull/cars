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
    /// Returns the demo track: flat road with RampEntry + Ramp for testing.
    /// </summary>
    public static TilePlacement[] GetDemoTrack()
    {
        // Test track: flat + RampEntry(3) + Ramp(7) + RampExit(3) + Flat at top
        // gridY: Entry=0 rises 3, Ramp=3 rises 7, Exit=10 rises 3, Flat=13
        var baseTiles = new TilePlacement[]
        {
            new TilePlacement { Type = TileType.Flat,      GridX = 0, GridY = 0,  GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Flat,      GridX = 1, GridY = 0,  GridZ = 0, Facing = CardinalDirection.North },
            
            new TilePlacement { Type = TileType.RampEntry, GridX = 2, GridY = 0,  GridZ = 0, Facing = CardinalDirection.East },
            //new TilePlacement { Type = TileType.Flat, GridX = 2, GridY = 0,  GridZ = 0, Facing = CardinalDirection.East },
            
            new TilePlacement { Type = TileType.Ramp,      GridX = 3, GridY = 3,  GridZ = 0, Facing = CardinalDirection.East },
            new TilePlacement { Type = TileType.Flat,      GridX = 3, GridY = 0,  GridZ = 0, Facing = CardinalDirection.East },
            
            new TilePlacement { Type = TileType.RampExit,  GridX = 4, GridY = 10, GridZ = 0, Facing = CardinalDirection.East },
            new TilePlacement { Type = TileType.Flat,  GridX = 4, GridY = 0, GridZ = 0, Facing = CardinalDirection.East },
            
            new TilePlacement { Type = TileType.Flat,      GridX = 5, GridY = 13, GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Flat,      GridX = 5, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
            
            
            
            new TilePlacement { Type = TileType.Flat,      GridX = 6, GridY = 0,  GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.RampExit,  GridX = 6, GridY = 10,  GridZ = 0, Facing = CardinalDirection.West },
            
            new TilePlacement { Type = TileType.Flat,      GridX = 7, GridY = 0,  GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Ramp,      GridX = 7, GridY = 3,  GridZ = 0, Facing = CardinalDirection.West },
            
            new TilePlacement { Type = TileType.Flat,      GridX = 8, GridY = 0,  GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.RampEntry,      GridX = 8, GridY = 0,  GridZ = 0, Facing = CardinalDirection.West },
            
            new TilePlacement { Type = TileType.Flat,      GridX = 9, GridY = 0,  GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Flat,      GridX = 10, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
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

        // Gravel run-off: 2 rows each side (Z=-3..-2, Z=+2..+3) across X=-1..11
        for (int x = -1; x <= 11; x++)
        {
            for (int z = -3; z <= -3; z++)
                result.Add(new TilePlacement { Type = TileType.Gravel, GridX = x, GridY = 0, GridZ = z, Facing = CardinalDirection.North });
            for (int z = 3; z <= 3; z++)
                result.Add(new TilePlacement { Type = TileType.Gravel, GridX = x, GridY = 0, GridZ = z, Facing = CardinalDirection.North });
        }
        // left hand side
        
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = -1, GridY = 0, GridZ = 2, Facing = CardinalDirection.North });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 0, GridY = 0, GridZ = 2, Facing = CardinalDirection.North });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 1, GridY = 0, GridZ = 2, Facing = CardinalDirection.North });
        result.Add(new TilePlacement() {Type = TileType.RampEntryCorner, GridX = 2, GridY = 0, GridZ = 2, Facing = CardinalDirection.North });
        result.Add(new TilePlacement() {Type = TileType.RampEntryCorner, GridX = 3, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 4, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 5, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 6, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 7, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 8, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 9, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 10, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 11, GridY = 0, GridZ = 2, Facing = CardinalDirection.West });
        
        //right side
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = -1, GridY = 0, GridZ = -2, Facing = CardinalDirection.North });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 0, GridY = 0, GridZ = -2, Facing = CardinalDirection.North });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 1, GridY = 0, GridZ = -2, Facing = CardinalDirection.North });
        result.Add(new TilePlacement() {Type = TileType.RampEntryCorner, GridX = 2, GridY = 0, GridZ = -2, Facing = CardinalDirection.East });
        result.Add(new TilePlacement() {Type = TileType.RampEntryCorner, GridX = 3, GridY = 0, GridZ = -2, Facing = CardinalDirection.South });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 4, GridY = 0, GridZ = -2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 5, GridY = 0, GridZ = -2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 6, GridY = 0, GridZ = -2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 7, GridY = 0, GridZ = -2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 8, GridY = 0, GridZ = -2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 9, GridY = 0, GridZ = -2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 10, GridY = 0, GridZ = -2, Facing = CardinalDirection.West });
        result.Add(new TilePlacement() {Type = TileType.Flat, GridX = 11, GridY = 0, GridZ = -2, Facing = CardinalDirection.West });
        
        // Gravel at track ends: X=-1 and X=11 for Z=-1..+1
        for (int z = -1; z <= 1; z++)
        {
            result.Add(new TilePlacement { Type = TileType.Gravel, GridX = -1, GridY = 0, GridZ = z, Facing = CardinalDirection.North });
            result.Add(new TilePlacement { Type = TileType.Gravel, GridX = 11, GridY = 0, GridZ = z, Facing = CardinalDirection.North });
        }

        // Fence border ring
        // West wall (X=-2): Z=-4..+4
        for (int z = -4; z <= 4; z++)
            result.Add(new TilePlacement { Type = TileType.Fence, GridX = -2, GridY = 0, GridZ = z, Facing = CardinalDirection.East });

        // East wall (X=12): Z=-4..+4
        for (int z = -4; z <= 4; z++)
            result.Add(new TilePlacement { Type = TileType.Fence, GridX = 12, GridY = 0, GridZ = z, Facing = CardinalDirection.West });

        // North wall (Z=-4): X=-1..11
        for (int x = -1; x <= 11; x++)
            result.Add(new TilePlacement { Type = TileType.Fence, GridX = x, GridY = 0, GridZ = -4, Facing = CardinalDirection.South });

        // South wall (Z=+4): X=-1..11
        for (int x = -1; x <= 11; x++)
            result.Add(new TilePlacement { Type = TileType.Fence, GridX = x, GridY = 0, GridZ = 4, Facing = CardinalDirection.North });

        return result.ToArray();
    }
}
