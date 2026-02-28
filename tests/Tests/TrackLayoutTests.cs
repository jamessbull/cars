using System.Linq;

namespace Tests;

public class TrackLayoutTests
{
    private readonly TilePlacement[] _track = TrackLayout.GetDemoTrack();

    [Fact]
    public void DemoTrack_HasExpectedTileCount()
    {
        // 11 tiles per lane * 3 lanes = 33 track
        // + gravel run-off and ends + fence border
        Assert.True(_track.Length > 33, $"Track should have more than 33 tiles, got {_track.Length}");
    }

    [Fact]
    public void DemoTrack_FiveLanes()
    {
        // 3 main lanes (Z=-1..+1) plus bridge approach lanes at Z=-2 and Z=+2
        var trackTiles = _track.Where(t => t.Type != TileType.Gravel && t.Type != TileType.Fence);
        var zValues = trackTiles.Select(t => t.GridZ).Distinct().OrderBy(z => z).ToArray();
        Assert.Equal(new[] { -2, -1, 0, 1, 2 }, zValues);
    }

    [Fact]
    public void DemoTrack_EachMainLaneHas17Tiles()
    {
        // Each main lane (Z=-1, 0, +1) carries 17 tiles: 11 road tiles
        // plus 6 bridge-overhead tiles placed at elevated GridY positions.
        var trackTiles = _track.Where(t => t.Type != TileType.Gravel && t.Type != TileType.Fence).ToArray();
        for (int lane = -1; lane <= 1; lane++)
        {
            var laneTiles = trackTiles.Where(t => t.GridZ == lane).ToArray();
            Assert.Equal(17, laneTiles.Length);
        }
    }

    [Fact]
    public void DemoTrack_CenterLaneProfile()
    {
        var center = _track
            .Where(t => t.GridZ == 0 && t.Type != TileType.Gravel && t.Type != TileType.Fence)
            .ToArray();

        // Elevated track: RampEntry rises at X=2, Ramp spans X=3, RampExit lands at X=4
        Assert.Contains(center, t => t.GridX == 2 && t.Type == TileType.RampEntry);
        Assert.Contains(center, t => t.GridX == 3 && t.Type == TileType.Ramp && t.GridY == 3);
        Assert.Contains(center, t => t.GridX == 4 && t.Type == TileType.RampExit && t.GridY == 10);
        // Flat tiles at both ends
        Assert.Contains(center, t => t.GridX == 0 && t.Type == TileType.Flat);
        Assert.Contains(center, t => t.GridX == 10 && t.Type == TileType.Flat);
    }

    [Fact]
    public void DemoTrack_RampAtGridY3()
    {
        var ramp = _track.First(t => t.Type == TileType.Ramp && t.GridZ == 0 && t.Facing == CardinalDirection.East);
        Assert.Equal(3, ramp.GridY);
    }

    [Fact]
    public void DemoTrack_RampExitAtGridY10()
    {
        var exit = _track.First(t => t.Type == TileType.RampExit && t.GridZ == 0 && t.Facing == CardinalDirection.East);
        Assert.Equal(10, exit.GridY);
    }

    [Fact]
    public void DemoTrack_HasGravelTiles()
    {
        var gravel = _track.Where(t => t.Type == TileType.Gravel).ToArray();
        Assert.True(gravel.Length > 0);
    }

    [Fact]
    public void DemoTrack_HasFenceTiles()
    {
        var fence = _track.Where(t => t.Type == TileType.Fence).ToArray();
        Assert.True(fence.Length > 0);
    }

    [Fact]
    public void DemoTrack_FenceAtPerimeter()
    {
        Assert.Contains(_track, t => t.Type == TileType.Fence && t.GridX == -2);
        Assert.Contains(_track, t => t.Type == TileType.Fence && t.GridX == 12);
        Assert.Contains(_track, t => t.Type == TileType.Fence && t.GridZ == -4);
        Assert.Contains(_track, t => t.Type == TileType.Fence && t.GridZ == 4);
    }

    [Fact]
    public void OrientationIndex_NorthIsZero()
    {
        Assert.Equal(0, TrackLayout.GetOrientationIndex(CardinalDirection.North));
    }

    [Fact]
    public void OrientationIndex_AllDirectionsUnique()
    {
        var indices = System.Enum.GetValues<CardinalDirection>()
            .Select(d => TrackLayout.GetOrientationIndex(d))
            .ToArray();
        Assert.Equal(indices.Length, indices.Distinct().Count());
    }
}
