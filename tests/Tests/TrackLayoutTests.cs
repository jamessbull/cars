namespace Tests;

public class TrackLayoutTests
{
    private readonly TilePlacement[] _track = TrackLayout.GetDemoTrack();

    [Fact]
    public void DemoTrack_Has7Tiles()
    {
        Assert.Equal(7, _track.Length);
    }

    [Fact]
    public void DemoTrack_FirstTwoAreFlat()
    {
        Assert.Equal(TileType.Flat, _track[0].Type);
        Assert.Equal(TileType.Flat, _track[1].Type);
    }

    [Fact]
    public void DemoTrack_ThirdIsRampFacingEast()
    {
        Assert.Equal(TileType.Ramp, _track[2].Type);
        Assert.Equal(CardinalDirection.East, _track[2].Facing);
    }

    [Fact]
    public void DemoTrack_FourthIsFlatAtY1()
    {
        Assert.Equal(TileType.Flat, _track[3].Type);
        Assert.Equal(1, _track[3].GridY);
    }

    [Fact]
    public void DemoTrack_FifthIsRampFacingWest()
    {
        Assert.Equal(TileType.Ramp, _track[4].Type);
        Assert.Equal(CardinalDirection.West, _track[4].Facing);
    }

    [Fact]
    public void DemoTrack_LastTwoAreFlat()
    {
        Assert.Equal(TileType.Flat, _track[5].Type);
        Assert.Equal(TileType.Flat, _track[6].Type);
    }

    [Fact]
    public void DemoTrack_XPositionsAreSequential()
    {
        for (int i = 0; i < _track.Length; i++)
        {
            Assert.Equal(i, _track[i].GridX);
        }
    }

    [Fact]
    public void DemoTrack_AllOnSameZPlane()
    {
        foreach (var tile in _track)
        {
            Assert.Equal(0, tile.GridZ);
        }
    }

    [Fact]
    public void DemoTrack_ApproachAndExitAtY0()
    {
        Assert.Equal(0, _track[0].GridY);
        Assert.Equal(0, _track[1].GridY);
        Assert.Equal(0, _track[5].GridY);
        Assert.Equal(0, _track[6].GridY);
    }

    [Fact]
    public void DemoTrack_BothRampsAtY0()
    {
        // Both ramps placed at grid Y=0; the mesh itself spans Y=0 to Y=CellHeight
        Assert.Equal(0, _track[2].GridY);
        Assert.Equal(0, _track[4].GridY);
    }

    [Fact]
    public void OrientationIndex_NorthIsZero()
    {
        Assert.Equal(0, TrackLayout.GetOrientationIndex(CardinalDirection.North));
    }

    [Fact]
    public void OrientationIndex_EastIs22()
    {
        Assert.Equal(22, TrackLayout.GetOrientationIndex(CardinalDirection.East));
    }

    [Fact]
    public void OrientationIndex_SouthIs10()
    {
        Assert.Equal(10, TrackLayout.GetOrientationIndex(CardinalDirection.South));
    }

    [Fact]
    public void OrientationIndex_WestIs16()
    {
        Assert.Equal(16, TrackLayout.GetOrientationIndex(CardinalDirection.West));
    }
}
