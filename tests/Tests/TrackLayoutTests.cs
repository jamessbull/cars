using System.Linq;

namespace Tests;

public class TrackLayoutTests
{
    private readonly TilePlacement[] _track = TrackLayout.GetDemoTrack();

    [Fact]
    public void DemoTrack_Has99Tiles()
    {
        // 21 track + 42 gravel + 36 fence = 99
        Assert.Equal(99, _track.Length);
    }

    [Fact]
    public void DemoTrack_ThreeLanes()
    {
        var trackTiles = _track.Where(t => t.Type == TileType.Flat || t.Type == TileType.Ramp);
        var zValues = trackTiles.Select(t => t.GridZ).Distinct().OrderBy(z => z).ToArray();
        Assert.Equal(new[] { -1, 0, 1 }, zValues);
    }

    [Fact]
    public void DemoTrack_EachLaneHas7Tiles()
    {
        var trackTiles = _track.Where(t => t.Type == TileType.Flat || t.Type == TileType.Ramp).ToArray();
        for (int lane = -1; lane <= 1; lane++)
        {
            var laneTiles = trackTiles.Where(t => t.GridZ == lane).ToArray();
            Assert.Equal(7, laneTiles.Length);
        }
    }

    [Fact]
    public void DemoTrack_CenterLaneMatchesOriginalPattern()
    {
        var center = _track.Where(t => t.GridZ == 0 && (t.Type == TileType.Flat || t.Type == TileType.Ramp)).OrderBy(t => t.GridX).ToArray();

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
        var center = _track.Where(t => t.GridZ == 0 && (t.Type == TileType.Flat || t.Type == TileType.Ramp)).OrderBy(t => t.GridX).ToArray();
        for (int lane = -1; lane <= 1; lane++)
        {
            var laneTiles = _track.Where(t => t.GridZ == lane && (t.Type == TileType.Flat || t.Type == TileType.Ramp)).OrderBy(t => t.GridX).ToArray();
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
        var center = _track.Where(t => t.GridZ == 0 && (t.Type == TileType.Flat || t.Type == TileType.Ramp)).OrderBy(t => t.GridX).ToArray();
        Assert.Equal(0, center[0].GridY);
        Assert.Equal(0, center[1].GridY);
        Assert.Equal(0, center[5].GridY);
        Assert.Equal(0, center[6].GridY);
    }

    [Fact]
    public void DemoTrack_BothRampsAtY0()
    {
        var center = _track.Where(t => t.GridZ == 0 && (t.Type == TileType.Flat || t.Type == TileType.Ramp)).OrderBy(t => t.GridX).ToArray();
        Assert.Equal(0, center[2].GridY);
        Assert.Equal(0, center[4].GridY);
    }

    [Fact]
    public void DemoTrack_Has42GravelTiles()
    {
        var gravel = _track.Where(t => t.Type == TileType.Gravel).ToArray();
        Assert.Equal(42, gravel.Length);
    }

    [Fact]
    public void DemoTrack_Has36FenceTiles()
    {
        var fence = _track.Where(t => t.Type == TileType.Fence).ToArray();
        Assert.Equal(36, fence.Length);
    }

    [Fact]
    public void DemoTrack_GravelAtTrackEnds()
    {
        // Gravel should exist at X=-1 and X=7 for Z=-1..+1
        for (int z = -1; z <= 1; z++)
        {
            Assert.Contains(_track, t => t.Type == TileType.Gravel && t.GridX == -1 && t.GridZ == z);
            Assert.Contains(_track, t => t.Type == TileType.Gravel && t.GridX == 7 && t.GridZ == z);
        }
    }

    [Fact]
    public void DemoTrack_FenceAtPerimeter()
    {
        // West fence at X=-2
        Assert.Contains(_track, t => t.Type == TileType.Fence && t.GridX == -2 && t.GridZ == 0);
        // East fence at X=8
        Assert.Contains(_track, t => t.Type == TileType.Fence && t.GridX == 8 && t.GridZ == 0);
        // North fence at Z=-4
        Assert.Contains(_track, t => t.Type == TileType.Fence && t.GridZ == -4 && t.GridX == 3);
        // South fence at Z=4
        Assert.Contains(_track, t => t.Type == TileType.Fence && t.GridZ == 4 && t.GridX == 3);
    }

    [Fact]
    public void DemoTrack_FenceOrientations()
    {
        // West wall faces East
        var westFence = _track.First(t => t.Type == TileType.Fence && t.GridX == -2 && t.GridZ == 0);
        Assert.Equal(CardinalDirection.East, westFence.Facing);

        // East wall faces West
        var eastFence = _track.First(t => t.Type == TileType.Fence && t.GridX == 8 && t.GridZ == 0);
        Assert.Equal(CardinalDirection.West, eastFence.Facing);

        // North wall faces South
        var northFence = _track.First(t => t.Type == TileType.Fence && t.GridZ == -4 && t.GridX == 3);
        Assert.Equal(CardinalDirection.South, northFence.Facing);

        // South wall faces North
        var southFence = _track.First(t => t.Type == TileType.Fence && t.GridZ == 4 && t.GridX == 3);
        Assert.Equal(CardinalDirection.North, southFence.Facing);
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
