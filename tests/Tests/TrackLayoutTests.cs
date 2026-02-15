using System.Linq;

namespace Tests;

public class TrackLayoutTests
{
    private readonly TilePlacement[] _track = TrackLayout.GetDemoTrack();

    [Fact]
    public void DemoTrack_Has21Tiles()
    {
        // 7 tiles per lane x 3 lanes
        Assert.Equal(21, _track.Length);
    }

    [Fact]
    public void DemoTrack_ThreeLanes()
    {
        var zValues = _track.Select(t => t.GridZ).Distinct().OrderBy(z => z).ToArray();
        Assert.Equal(new[] { -1, 0, 1 }, zValues);
    }

    [Fact]
    public void DemoTrack_EachLaneHas7Tiles()
    {
        for (int lane = -1; lane <= 1; lane++)
        {
            var laneTiles = _track.Where(t => t.GridZ == lane).ToArray();
            Assert.Equal(7, laneTiles.Length);
        }
    }

    [Fact]
    public void DemoTrack_CenterLaneMatchesOriginalPattern()
    {
        var center = _track.Where(t => t.GridZ == 0).OrderBy(t => t.GridX).ToArray();

        Assert.Equal(TileType.Flat, center[0].Type);
        Assert.Equal(TileType.Flat, center[1].Type);
        Assert.Equal(TileType.Ramp, center[2].Type);
        Assert.Equal(CardinalDirection.East, center[2].Facing);
        Assert.Equal(TileType.Flat, center[3].Type);
        Assert.Equal(1, center[3].GridY);
        Assert.Equal(TileType.Ramp, center[4].Type);
        Assert.Equal(CardinalDirection.West, center[4].Facing);
        Assert.Equal(TileType.Flat, center[5].Type);
        Assert.Equal(TileType.Flat, center[6].Type);
    }

    [Fact]
    public void DemoTrack_AllLanesHaveSameXPattern()
    {
        var center = _track.Where(t => t.GridZ == 0).OrderBy(t => t.GridX).ToArray();
        for (int lane = -1; lane <= 1; lane++)
        {
            var laneTiles = _track.Where(t => t.GridZ == lane).OrderBy(t => t.GridX).ToArray();
            for (int i = 0; i < 7; i++)
            {
                Assert.Equal(center[i].Type, laneTiles[i].Type);
                Assert.Equal(center[i].GridX, laneTiles[i].GridX);
                Assert.Equal(center[i].GridY, laneTiles[i].GridY);
                Assert.Equal(center[i].Facing, laneTiles[i].Facing);
            }
        }
    }

    [Fact]
    public void DemoTrack_ApproachAndExitAtY0()
    {
        var center = _track.Where(t => t.GridZ == 0).OrderBy(t => t.GridX).ToArray();
        Assert.Equal(0, center[0].GridY);
        Assert.Equal(0, center[1].GridY);
        Assert.Equal(0, center[5].GridY);
        Assert.Equal(0, center[6].GridY);
    }

    [Fact]
    public void DemoTrack_BothRampsAtY0()
    {
        var center = _track.Where(t => t.GridZ == 0).OrderBy(t => t.GridX).ToArray();
        Assert.Equal(0, center[2].GridY);
        Assert.Equal(0, center[4].GridY);
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
