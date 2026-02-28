using System;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Loads track layout data from JSON. Pure C# — no Godot dependency.
///
/// Expected JSON format:
/// {
///   "TrackLayout": [
///     { "type": "Flat",      "x": 0, "y": 0, "z": 0, "facing": "North" },
///     { "type": "RampEntry", "x": 1, "y": 0, "z": 0, "facing": "East"  }
///   ]
/// }
///
/// Property names and enum values are matched case-insensitively.
/// </summary>
public static class TrackLayoutLoader
{
    private class TrackLayoutFile
    {
        [JsonPropertyName("TrackLayout")]
        public TilePlacementDto[] TrackLayout { get; set; } = Array.Empty<TilePlacementDto>();
    }

    private class TilePlacementDto
    {
        [JsonPropertyName("type")]   public string Type   { get; set; } = "";
        [JsonPropertyName("x")]      public int    X      { get; set; }
        [JsonPropertyName("y")]      public int    Y      { get; set; }
        [JsonPropertyName("z")]      public int    Z      { get; set; }
        [JsonPropertyName("facing")] public string Facing { get; set; } = "North";
    }

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Parses a JSON string and returns the tile placements it describes.
    /// Throws <see cref="JsonException"/> for malformed JSON.
    /// Throws <see cref="ArgumentException"/> for unknown tile type or facing values.
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
}
