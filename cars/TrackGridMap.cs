using Godot;

public partial class TrackGridMap : Node3D
{
    /// <summary>
    /// Path to a JSON track layout file (Godot res:// or absolute path).
    /// When set and the file exists, it overrides the built-in demo track.
    /// Leave empty to always use the demo track.
    /// </summary>
    [Export] public string TrackLayoutFile = "res://track_layout.json";

    private GridMap _gridMap;

    public override void _Ready()
    {
        SetupEnvironment();

        var library = BuildMeshLibrary();

        _gridMap = new GridMap();
        _gridMap.MeshLibrary = library;
        _gridMap.CellSize = new Vector3(TileGeometry.CellWidth, TileGeometry.CellHeight, TileGeometry.CellDepth);
        AddChild(_gridMap);

        var placements = PlaceDemoTrack();
        SpawnEarthBlocks();
        PositionCamera();
        PositionCar(placements);
    }

    private void SetupEnvironment()
    {
        var env = new Environment();
        env.AmbientLightSource = Environment.AmbientSource.Color;
        env.AmbientLightColor = new Color(0.3f, 0.3f, 0.3f);
        env.AmbientLightEnergy = 1.0f;
        env.BackgroundMode = Environment.BGMode.Color;
        env.BackgroundColor = new Color(0.15f, 0.15f, 0.2f);

        var worldEnv = new WorldEnvironment();
        worldEnv.Environment = env;
        AddChild(worldEnv);
    }

    private static MeshLibrary BuildMeshLibrary() => TileMeshBuilder.BuildMeshLibrary();

    private TilePlacement[] LoadPlacements()
    {
        if (!string.IsNullOrEmpty(TrackLayoutFile) && FileAccess.FileExists(TrackLayoutFile))
        {
            var json = FileAccess.GetFileAsString(TrackLayoutFile);
            if (string.IsNullOrWhiteSpace(json))
            {
                GD.PrintErr($"TrackGridMap: {TrackLayoutFile} exists but is empty — using demo track");
                return TrackLayout.GetDemoTrack();
            }

            GD.Print($"TrackGridMap: loading {TrackLayoutFile} ({json.Length} chars)");
            try
            {
                var placements = TrackLayoutLoader.LoadFromJson(json);
                GD.Print($"TrackGridMap: loaded {placements.Length} tiles");
                return placements;
            }
            catch (System.Exception e)
            {
                GD.PrintErr($"TrackGridMap: failed to parse {TrackLayoutFile}: {e.Message} — using demo track");
                return TrackLayout.GetDemoTrack();
            }
        }

        GD.Print("TrackGridMap: no track file found, using demo track");
        return TrackLayout.GetDemoTrack();
    }

    private TilePlacement[] PlaceDemoTrack()
    {
        var placements = LoadPlacements();
        foreach (var placement in placements)
        {
            int tileId = (int)placement.Type;
            int orientation = TrackLayout.GetOrientationIndex(placement.Facing);
            var cellPos = new Vector3I(placement.GridX, placement.GridY, placement.GridZ);
            _gridMap.SetCellItem(cellPos, tileId, orientation);
            GD.Print($"Placed {placement.Type} (id={tileId}) at grid ({placement.GridX},{placement.GridY},{placement.GridZ}) facing {placement.Facing} orientation={orientation}");
        }
        GD.Print($"GridMap cell_size={_gridMap.CellSize} total cells={_gridMap.GetUsedCells().Count}");
        return placements;
    }

    private void SpawnEarthBlocks()
    {
        if (string.IsNullOrEmpty(TrackLayoutFile) || !FileAccess.FileExists(TrackLayoutFile))
            return;

        var json = FileAccess.GetFileAsString(TrackLayoutFile);
        if (string.IsNullOrWhiteSpace(json)) return;

        try
        {
            var blocks = TrackLayoutLoader.LoadEarthBlocksFromJson(json);
            foreach (var eb in blocks)
            {
                var block = new BlockOfEarth();
                block.Position = BlockOfEarth.GridToWorld(eb.GridX, eb.GridZ);
                AddChild(block);
            }
            if (blocks.Length > 0)
                GD.Print($"TrackGridMap: spawned {blocks.Length} earth blocks");
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"TrackGridMap: failed to spawn earth blocks: {e.Message}");
        }
    }

    private void PositionCar(TilePlacement[] placements)
    {
        var car = GetParent().GetNodeOrNull<Node3D>("Car");
        if (car == null)
        {
            GD.PrintErr("TrackGridMap: Car node not found under parent — skipping spawn");
            return;
        }

        // Spawn at the centre of grid cell (0, 0, 0) — the conventional track start.
        // CellWidth = CellDepth = 2.0, so the cell centre is at world X/Z = 1.0.
        // The +1.0 on Y lifts the car clear of the tile surface.
        float worldX = TileGeometry.CellWidth  / 2f;   // 1.0
        float worldY = TileGeometry.CellHeight / 2f + 1.0f;
        float worldZ = TileGeometry.CellDepth  / 2f;   // 1.0

        car.Position = new Vector3(worldX, worldY, worldZ);
        // -PI/2 rotation around Y makes the car face +X (East), matching the default track direction.
        car.Rotation = new Vector3(0f, -System.MathF.PI / 2f, 0f);
        GD.Print($"TrackGridMap: car spawned at ({worldX:F3}, {worldY:F3}, {worldZ:F3}), {placements.Length} tiles placed");
    }

    private void PositionCamera()
    {
        var camera = GetParent().GetNodeOrNull<Camera3D>("Camera3D");
        if (camera != null)
        {
            // GridMap with center_x/z offsets cells by half cell_size, so track center shifts
            // Cell centers: X = gridX*2+1, Z = gridZ*2+1. Track center ≈ (7, 0.5, 1)
            camera.Position = new Vector3(7, 5, 10);
            camera.LookAt(new Vector3(7, 0, 1), Vector3.Up);
        }
    }
}
