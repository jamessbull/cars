using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Loads/saves track layout data from JSON. Pure C# — no Godot dependency.
///
/// JSON format:
/// {
///   "TrackLayout": [
///     { "type": "Flat", "x": 0, "y": 0, "z": 0, "facing": "North" }
///   ],
///   "EarthBlocks": [
///     { "x": 0, "z": 0 }
///   ]
/// }
/// </summary>
public static class TrackLayoutLoader
{
    private class TrackLayoutFile
    {
        [JsonPropertyName("TrackLayout")]
        public TilePlacementDto[] TrackLayout { get; set; } = Array.Empty<TilePlacementDto>();

        [JsonPropertyName("EarthBlocks")]
        public EarthBlockDto[] EarthBlocks { get; set; } = Array.Empty<EarthBlockDto>();
    }

    private class TilePlacementDto
    {
        [JsonPropertyName("type")]   public string Type   { get; set; } = "";
        [JsonPropertyName("x")]      public int    X      { get; set; }
        [JsonPropertyName("y")]      public int    Y      { get; set; }
        [JsonPropertyName("z")]      public int    Z      { get; set; }
        [JsonPropertyName("facing")] public string Facing { get; set; } = "North";
    }

    private class EarthBlockDto
    {
        [JsonPropertyName("x")] public int X { get; set; }
        [JsonPropertyName("z")] public int Z { get; set; }
    }

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serializes tile placements and earth blocks to JSON.
    /// </summary>
    public static string SaveToJson(
        IEnumerable<TilePlacement> placements,
        IEnumerable<(int x, int z)> earthBlocks = null)
    {
        var tileDtos = new List<TilePlacementDto>();
        foreach (var p in placements)
        {
            tileDtos.Add(new TilePlacementDto
            {
                Type   = p.Type.ToString(),
                X      = p.GridX,
                Y      = p.GridY,
                Z      = p.GridZ,
                Facing = p.Facing.ToString()
            });
        }

        var earthDtos = new List<EarthBlockDto>();
        if (earthBlocks != null)
        {
            foreach (var (x, z) in earthBlocks)
                earthDtos.Add(new EarthBlockDto { X = x, Z = z });
        }

        var file = new TrackLayoutFile
        {
            TrackLayout = tileDtos.ToArray(),
            EarthBlocks = earthDtos.ToArray()
        };
        return JsonSerializer.Serialize(file, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Parses a JSON string and returns the tile placements it describes.
    /// </summary>
    public static TilePlacement[] LoadFromJson(string json)
    {
        var file = JsonSerializer.Deserialize<TrackLayoutFile>(json, Options)
            ?? throw new JsonException("JSON root was null");

        var result = new TilePlacement[file.TrackLayout.Length];
        for (int i = 0; i < file.TrackLayout.Length; i++)
        {
            var dto = file.TrackLayout[i];

            if (!Enum.TryParse<TileType>(dto.Type, ignoreCase: true, out var tileType))
                throw new ArgumentException($"Unknown tile type '{dto.Type}' at index {i}");

            if (!Enum.TryParse<CardinalDirection>(dto.Facing, ignoreCase: true, out var facing))
                throw new ArgumentException($"Unknown facing '{dto.Facing}' at index {i}");

            result[i] = new TilePlacement
            {
                Type   = tileType,
                GridX  = dto.X,
                GridY  = dto.Y,
                GridZ  = dto.Z,
                Facing = facing
            };
        }

        return result;
    }

    /// <summary>
    /// Parses a JSON string and returns the earth block positions it describes.
    /// Returns an empty array if no EarthBlocks key is present.
    /// </summary>
    public static EarthBlockPlacement[] LoadEarthBlocksFromJson(string json)
    {
        var file = JsonSerializer.Deserialize<TrackLayoutFile>(json, Options)
            ?? throw new JsonException("JSON root was null");

        var result = new EarthBlockPlacement[file.EarthBlocks.Length];
        for (int i = 0; i < file.EarthBlocks.Length; i++)
            result[i] = new EarthBlockPlacement { GridX = file.EarthBlocks[i].X, GridZ = file.EarthBlocks[i].Z };

        return result;
    }
}
