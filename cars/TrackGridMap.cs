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

        PlaceDemoTrack();
        PositionCamera();
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

    private MeshLibrary BuildMeshLibrary()
    {
        var library = new MeshLibrary();

        var flatMaterial = new StandardMaterial3D();
        flatMaterial.AlbedoColor = new Color(0.3f, 0.8f, 0.3f);
        flatMaterial.Roughness = 0.8f;
        flatMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

        var rampMaterial = new StandardMaterial3D();
        rampMaterial.AlbedoColor = new Color(0.7f, 0.7f, 0.7f);
        rampMaterial.Roughness = 0.8f;
        rampMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

        var gravelMaterial = new StandardMaterial3D();
        gravelMaterial.AlbedoColor = new Color(0.6f, 0.45f, 0.25f);
        gravelMaterial.Roughness = 1.0f;
        gravelMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

        var fenceMaterial = new StandardMaterial3D();
        fenceMaterial.AlbedoColor = new Color(0.4f, 0.4f, 0.4f);
        fenceMaterial.Roughness = 0.9f;
        fenceMaterial.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

        foreach (TileType type in System.Enum.GetValues<TileType>())
        {
            int id = (int)type;
            var meshData = TileGeometry.GenerateTile(type);
            var mat = type switch
            {
                TileType.Flat => flatMaterial,
                TileType.Ramp => rampMaterial,
                TileType.Gravel => gravelMaterial,
                TileType.Fence => fenceMaterial,
                TileType.RampEntry => rampMaterial,
                TileType.RampExit => rampMaterial,
                TileType.SlopeEdge => gravelMaterial,
                TileType.SlopeCorner => gravelMaterial,
                TileType.RampEntryCorner => rampMaterial,
                TileType.SolidRampEntry => rampMaterial,
                _ => flatMaterial
            };

            var arrayMesh = ConvertToArrayMesh(meshData, mat);
            var collisionShape = CreateCollisionShape(meshData);

            library.CreateItem(id);
            library.SetItemName(id, type.ToString());
            library.SetItemMesh(id, arrayMesh);

            var shapes = new Godot.Collections.Array();
            shapes.Add(collisionShape);
            shapes.Add(Transform3D.Identity);
            library.SetItemShapes(id, shapes);

            GD.Print($"MeshLibrary item {id} ({type}): {meshData.Vertices.Length / 3} verts, {meshData.Indices.Length / 3} tris");
        }

        return library;
    }

    private static ArrayMesh ConvertToArrayMesh(TileMeshData data, Material material)
    {
        int vertexCount = data.Vertices.Length / 3;

        var vertices = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = new Vector3(data.Vertices[i * 3], data.Vertices[i * 3 + 1], data.Vertices[i * 3 + 2]);
            normals[i] = new Vector3(data.Normals[i * 3], data.Normals[i * 3 + 1], data.Normals[i * 3 + 2]);
        }

        var indices = new int[data.Indices.Length];
        System.Array.Copy(data.Indices, indices, data.Indices.Length);

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Index] = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, material);
        return mesh;
    }

    private static ConcavePolygonShape3D CreateCollisionShape(TileMeshData data)
    {
        var faces = new Vector3[data.Indices.Length];
        for (int i = 0; i < data.Indices.Length; i++)
        {
            int vi = data.Indices[i] * 3;
            faces[i] = new Vector3(data.Vertices[vi], data.Vertices[vi + 1], data.Vertices[vi + 2]);
        }

        var shape = new ConcavePolygonShape3D();
        shape.BackfaceCollision = true;
        shape.SetFaces(faces);
        return shape;
    }

    private TilePlacement[] LoadPlacements()
    {
        if (!string.IsNullOrEmpty(TrackLayoutFile) && FileAccess.FileExists(TrackLayoutFile))
        {
            var json = FileAccess.GetFileAsString(TrackLayoutFile);
            GD.Print($"Loading track from {TrackLayoutFile}");
            return TrackLayoutLoader.LoadFromJson(json);
        }

        GD.Print("No track file found, using demo track");
        return TrackLayout.GetDemoTrack();
    }

    private void PlaceDemoTrack()
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
