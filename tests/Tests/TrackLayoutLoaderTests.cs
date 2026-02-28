using System;
using System.Text.Json;

namespace Tests;

public class TrackLayoutLoaderTests
{
    [Fact]
    public void LoadFromJson_ParsesTilePlacements()
    {
        var json = """
            {
              "TrackLayout": [
                { "type": "Flat", "x": 1, "y": 2, "z": 3, "facing": "North" },
                { "type": "RampEntry", "x": 4, "y": 0, "z": 0, "facing": "East" }
              ]
            }
            """;

        var tiles = TrackLayoutLoader.LoadFromJson(json);

        Assert.Equal(2, tiles.Length);

        Assert.Equal(TileType.Flat, tiles[0].Type);
        Assert.Equal(1, tiles[0].GridX);
        Assert.Equal(2, tiles[0].GridY);
        Assert.Equal(3, tiles[0].GridZ);
        Assert.Equal(CardinalDirection.North, tiles[0].Facing);

        Assert.Equal(TileType.RampEntry, tiles[1].Type);
        Assert.Equal(4, tiles[1].GridX);
        Assert.Equal(CardinalDirection.East, tiles[1].Facing);
    }

    [Fact]
    public void LoadFromJson_EmptyArray_ReturnsEmpty()
    {
        var json = """{ "TrackLayout": [] }""";
        var tiles = TrackLayoutLoader.LoadFromJson(json);
        Assert.Empty(tiles);
    }

    [Theory]
    [InlineData("Flat",           TileType.Flat)]
    [InlineData("Ramp",           TileType.Ramp)]
    [InlineData("Gravel",         TileType.Gravel)]
    [InlineData("Fence",          TileType.Fence)]
    [InlineData("RampEntry",      TileType.RampEntry)]
    [InlineData("RampExit",       TileType.RampExit)]
    [InlineData("SlopeEdge",      TileType.SlopeEdge)]
    [InlineData("SlopeCorner",    TileType.SlopeCorner)]
    [InlineData("RampEntryCorner",TileType.RampEntryCorner)]
    [InlineData("SolidRampEntry", TileType.SolidRampEntry)]
    public void LoadFromJson_AllTileTypes_ParseCorrectly(string typeString, TileType expected)
    {
        var json = $$"""{ "TrackLayout": [{ "type": "{{typeString}}", "x": 0, "y": 0, "z": 0, "facing": "North" }] }""";
        var tiles = TrackLayoutLoader.LoadFromJson(json);
        Assert.Equal(expected, tiles[0].Type);
    }

    [Theory]
    [InlineData("North", CardinalDirection.North)]
    [InlineData("East",  CardinalDirection.East)]
    [InlineData("South", CardinalDirection.South)]
    [InlineData("West",  CardinalDirection.West)]
    public void LoadFromJson_AllFacings_ParseCorrectly(string facingString, CardinalDirection expected)
    {
        var json = $$"""{ "TrackLayout": [{ "type": "Flat", "x": 0, "y": 0, "z": 0, "facing": "{{facingString}}" }] }""";
        var tiles = TrackLayoutLoader.LoadFromJson(json);
        Assert.Equal(expected, tiles[0].Facing);
    }

    [Theory]
    [InlineData("flat")]
    [InlineData("FLAT")]
    [InlineData("rampentry")]
    [InlineData("RAMPENTRY")]
    public void LoadFromJson_TypeIsCaseInsensitive(string typeString)
    {
        var json = $$"""{ "TrackLayout": [{ "type": "{{typeString}}", "x": 0, "y": 0, "z": 0, "facing": "North" }] }""";
        var tiles = TrackLayoutLoader.LoadFromJson(json);
        Assert.Single(tiles);
    }

    [Theory]
    [InlineData("north")]
    [InlineData("EAST")]
    [InlineData("south")]
    public void LoadFromJson_FacingIsCaseInsensitive(string facingString)
    {
        var json = $$"""{ "TrackLayout": [{ "type": "Flat", "x": 0, "y": 0, "z": 0, "facing": "{{facingString}}" }] }""";
        var tiles = TrackLayoutLoader.LoadFromJson(json);
        Assert.Single(tiles);
    }

    [Fact]
    public void LoadFromJson_UnknownTileType_ThrowsArgumentException()
    {
        var json = """{ "TrackLayout": [{ "type": "Banana", "x": 0, "y": 0, "z": 0, "facing": "North" }] }""";
        Assert.Throws<ArgumentException>(() => TrackLayoutLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_UnknownFacing_ThrowsArgumentException()
    {
        var json = """{ "TrackLayout": [{ "type": "Flat", "x": 0, "y": 0, "z": 0, "facing": "Up" }] }""";
        Assert.Throws<ArgumentException>(() => TrackLayoutLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_MalformedJson_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => TrackLayoutLoader.LoadFromJson("not json"));
    }

    [Fact]
    public void LoadFromJson_GridCoordinatesAreCorrect()
    {
        var json = """{ "TrackLayout": [{ "type": "Flat", "x": -5, "y": 13, "z": 2, "facing": "West" }] }""";
        var tiles = TrackLayoutLoader.LoadFromJson(json);
        Assert.Equal(-5, tiles[0].GridX);
        Assert.Equal(13, tiles[0].GridY);
        Assert.Equal(2,  tiles[0].GridZ);
    }

    // ── SaveToJson tests ─────────────────────────────────────────────────────

    [Fact]
    public void SaveToJson_EmptyCollection_WritesEmptyArray()
    {
        var json = TrackLayoutLoader.SaveToJson(System.Array.Empty<TilePlacement>());
        var tiles = TrackLayoutLoader.LoadFromJson(json);
        Assert.Empty(tiles);
    }

    [Fact]
    public void SaveToJson_SingleTile_RoundTrip()
    {
        var original = new TilePlacement
        {
            Type   = TileType.RampEntry,
            GridX  = 3,
            GridY  = 7,
            GridZ  = -2,
            Facing = CardinalDirection.East
        };

        var json   = TrackLayoutLoader.SaveToJson(new[] { original });
        var tiles  = TrackLayoutLoader.LoadFromJson(json);

        Assert.Single(tiles);
        Assert.Equal(TileType.RampEntry,       tiles[0].Type);
        Assert.Equal(3,                         tiles[0].GridX);
        Assert.Equal(7,                         tiles[0].GridY);
        Assert.Equal(-2,                        tiles[0].GridZ);
        Assert.Equal(CardinalDirection.East,    tiles[0].Facing);
    }

    [Fact]
    public void SaveToJson_MultipleTiles_RoundTrip()
    {
        var originals = new[]
        {
            new TilePlacement { Type = TileType.Flat,  GridX = 0, GridY = 0, GridZ = 0, Facing = CardinalDirection.North },
            new TilePlacement { Type = TileType.Fence, GridX = 5, GridY = 2, GridZ = 3, Facing = CardinalDirection.West  },
        };

        var json  = TrackLayoutLoader.SaveToJson(originals);
        var tiles = TrackLayoutLoader.LoadFromJson(json);

        Assert.Equal(2, tiles.Length);
        Assert.Equal(TileType.Flat,          tiles[0].Type);
        Assert.Equal(TileType.Fence,         tiles[1].Type);
        Assert.Equal(CardinalDirection.West, tiles[1].Facing);
        Assert.Equal(5, tiles[1].GridX);
        Assert.Equal(2, tiles[1].GridY);
        Assert.Equal(3, tiles[1].GridZ);
    }

    [Fact]
    public void SaveToJson_ProducesValidJson()
    {
        var placement = new TilePlacement { Type = TileType.Gravel, GridX = 1, GridY = 0, GridZ = 1, Facing = CardinalDirection.South };
        var json = TrackLayoutLoader.SaveToJson(new[] { placement });

        // Must parse without throwing
        var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("TrackLayout", out _));
    }
}
