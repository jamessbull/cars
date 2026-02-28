namespace Tests;

public class MapEditorStateTests
{
    // ── PlaceTile ─────────────────────────────────────────────────────────────

    [Fact]
    public void PlaceTile_AddsEntry()
    {
        var state = new MapEditorState();
        state.PlaceTile(1, 0, 2);

        Assert.True(state.PlacedTiles.ContainsKey((1, 0, 2)));
        var tile = state.PlacedTiles[(1, 0, 2)];
        Assert.Equal(TileType.Flat, tile.Type);
        Assert.Equal(1, tile.GridX);
        Assert.Equal(0, tile.GridY);
        Assert.Equal(2, tile.GridZ);
        Assert.Equal(CardinalDirection.North, tile.Facing);
    }

    [Fact]
    public void PlaceTile_ReplacesExistingAtSameCell()
    {
        var state = new MapEditorState();
        state.PlaceTile(0, 0, 0);

        state.SelectTileType(TileType.Fence);
        state.CycleFacing(1); // North → East
        state.PlaceTile(0, 0, 0);

        Assert.Single(state.PlacedTiles);
        var tile = state.PlacedTiles[(0, 0, 0)];
        Assert.Equal(TileType.Fence,           tile.Type);
        Assert.Equal(CardinalDirection.East,   tile.Facing);
    }

    // ── DeleteTile ────────────────────────────────────────────────────────────

    [Fact]
    public void DeleteTile_RemovesEntry()
    {
        var state = new MapEditorState();
        state.PlaceTile(3, 1, 4);
        state.DeleteTile(3, 1, 4);

        Assert.False(state.PlacedTiles.ContainsKey((3, 1, 4)));
    }

    [Fact]
    public void DeleteTile_NonexistentCell_DoesNotThrow()
    {
        var state = new MapEditorState();
        var ex = Record.Exception(() => state.DeleteTile(99, 99, 99));
        Assert.Null(ex);
    }

    // ── CycleTileType ─────────────────────────────────────────────────────────

    [Fact]
    public void CycleTileType_WrapsForwardAtEnd()
    {
        var state = new MapEditorState();
        var types = System.Enum.GetValues<TileType>();

        // Advance to the last type
        state.CycleTileType(types.Length - 1);
        Assert.Equal(types[^1], state.SelectedType);

        // One more step should wrap to the first
        state.CycleTileType(1);
        Assert.Equal(types[0], state.SelectedType);
    }

    [Fact]
    public void CycleTileType_WrapsBackwardAtStart()
    {
        var state = new MapEditorState();
        // Default is first type; stepping back should wrap to the last
        state.CycleTileType(-1);
        var types = System.Enum.GetValues<TileType>();
        Assert.Equal(types[^1], state.SelectedType);
    }

    // ── CycleFacing ──────────────────────────────────────────────────────────

    [Fact]
    public void CycleFacing_RotatesThroughAllFour()
    {
        var state = new MapEditorState();
        Assert.Equal(CardinalDirection.North, state.SelectedFacing);

        state.CycleFacing(1);
        Assert.Equal(CardinalDirection.East, state.SelectedFacing);

        state.CycleFacing(1);
        Assert.Equal(CardinalDirection.South, state.SelectedFacing);

        state.CycleFacing(1);
        Assert.Equal(CardinalDirection.West, state.SelectedFacing);

        state.CycleFacing(1); // wraps back
        Assert.Equal(CardinalDirection.North, state.SelectedFacing);
    }

    // ── ChangeGridY ──────────────────────────────────────────────────────────

    [Fact]
    public void ChangeGridY_ClampsAtZero()
    {
        var state = new MapEditorState();
        state.ChangeGridY(-5);
        Assert.Equal(0, state.GridY);
    }

    [Fact]
    public void ChangeGridY_ClampsAtMax()
    {
        var state = new MapEditorState();
        state.ChangeGridY(MapEditorState.MaxGridY + 100);
        Assert.Equal(MapEditorState.MaxGridY, state.GridY);
    }

    [Fact]
    public void ChangeGridY_MovesInRange()
    {
        var state = new MapEditorState();
        state.ChangeGridY(5);
        Assert.Equal(5, state.GridY);
        state.ChangeGridY(-2);
        Assert.Equal(3, state.GridY);
    }

    // ── WorldToGridXZ ─────────────────────────────────────────────────────────

    [Fact]
    public void WorldToGridXZ_SnapsToContainingCell()
    {
        // CellWidth = CellDepth = 2.0.
        // GridMap with cell_center_x/z=true places cell gx at world X = gx*2+1,
        // so cell gx occupies [gx*2, (gx+1)*2).  We use Floor to identify the cell.
        var (gx, gz) = MapEditorState.WorldToGridXZ(4.0f, 6.0f);
        Assert.Equal(2, gx); // floor(4/2) = 2  (cell 2 covers [4,6))
        Assert.Equal(3, gz); // floor(6/2) = 3  (cell 3 covers [6,8))

        var (gx2, gz2) = MapEditorState.WorldToGridXZ(4.6f, 7.4f);
        Assert.Equal(2, gx2); // floor(4.6/2) = floor(2.3) = 2
        Assert.Equal(3, gz2); // floor(7.4/2) = floor(3.7) = 3  (7.4 is inside cell 3, not 4)
    }

    [Fact]
    public void WorldToGridXZ_NegativeCoordinates()
    {
        var (gx, gz) = MapEditorState.WorldToGridXZ(-4.0f, -6.0f);
        Assert.Equal(-2, gx);
        Assert.Equal(-3, gz);
    }

    // ── ToJson / round-trip ──────────────────────────────────────────────────

    [Fact]
    public void ToJson_RoundTrip_LoadFromJsonReturnsOriginalTiles()
    {
        var state = new MapEditorState();

        state.SelectTileType(TileType.Gravel);
        state.CycleFacing(2); // → South
        state.PlaceTile(0, 0, 0);

        state.SelectTileType(TileType.Fence);
        state.CycleFacing(-1); // → East (going back from South)
        state.PlaceTile(5, 2, -1);

        var json  = state.ToJson();
        var tiles = TrackLayoutLoader.LoadFromJson(json);

        Assert.Equal(2, tiles.Length);

        // Order may vary (Dictionary), so search by position
        bool found0 = false, found1 = false;
        foreach (var t in tiles)
        {
            if (t.GridX == 0 && t.GridY == 0 && t.GridZ == 0)
            {
                Assert.Equal(TileType.Gravel,          t.Type);
                Assert.Equal(CardinalDirection.South,  t.Facing);
                found0 = true;
            }
            else if (t.GridX == 5 && t.GridY == 2 && t.GridZ == -1)
            {
                Assert.Equal(TileType.Fence,           t.Type);
                found1 = true;
            }
        }
        Assert.True(found0, "Tile at (0,0,0) not found");
        Assert.True(found1, "Tile at (5,2,-1) not found");
    }
}
