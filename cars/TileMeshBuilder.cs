using Godot;

/// <summary>
/// Shared mesh/material/library builder for tile types.
/// Used by both TrackGridMap (game) and MapEditorNode (editor).
/// </summary>
public static class TileMeshBuilder
{
    /// <summary>Convert flat TileMeshData arrays into a Godot ArrayMesh with the given material.</summary>
    public static ArrayMesh BuildMesh(TileMeshData data, Material material)
    {
        int vertexCount = data.Vertices.Length / 3;

        var vertices = new Vector3[vertexCount];
        var normals  = new Vector3[vertexCount];
        for (int i = 0; i < vertexCount; i++)
        {
            vertices[i] = new Vector3(data.Vertices[i * 3], data.Vertices[i * 3 + 1], data.Vertices[i * 3 + 2]);
            normals[i]  = new Vector3(data.Normals[i * 3],  data.Normals[i * 3 + 1],  data.Normals[i * 3 + 2]);
        }

        var indices = new int[data.Indices.Length];
        System.Array.Copy(data.Indices, indices, data.Indices.Length);

        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        arrays[(int)Mesh.ArrayType.Normal] = normals;
        arrays[(int)Mesh.ArrayType.Index]  = indices;

        var mesh = new ArrayMesh();
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
        mesh.SurfaceSetMaterial(0, material);
        return mesh;
    }

    /// <summary>Return the standard opaque material for a given tile type.</summary>
    public static Material MaterialForType(TileType type)
    {
        return type switch
        {
            TileType.Flat           => FlatMaterial(),
            TileType.Ramp           => RampMaterial(),
            TileType.Gravel         => GravelMaterial(),
            TileType.Fence          => FenceMaterial(),
            TileType.RampEntry      => RampMaterial(),
            TileType.RampExit       => RampMaterial(),
            TileType.SlopeEdge      => GravelMaterial(),
            TileType.SlopeCorner    => GravelMaterial(),
            TileType.RampEntryCorner => RampMaterial(),
            TileType.SolidRampEntry => RampMaterial(),
            _                       => FlatMaterial()
        };
    }

    /// <summary>
    /// Build a complete MeshLibrary for all TileType values, including collision shapes.
    /// </summary>
    public static MeshLibrary BuildMeshLibrary()
    {
        var library = new MeshLibrary();

        foreach (TileType type in System.Enum.GetValues<TileType>())
        {
            int id = (int)type;
            var meshData      = TileGeometry.GenerateTile(type);
            var mat           = MaterialForType(type);
            var arrayMesh     = BuildMesh(meshData, mat);
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

    private static StandardMaterial3D FlatMaterial()
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.3f, 0.8f, 0.3f);
        mat.Roughness   = 0.8f;
        mat.CullMode    = BaseMaterial3D.CullModeEnum.Disabled;
        return mat;
    }

    private static StandardMaterial3D RampMaterial()
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.7f, 0.7f, 0.7f);
        mat.Roughness   = 0.8f;
        mat.CullMode    = BaseMaterial3D.CullModeEnum.Disabled;
        return mat;
    }

    private static StandardMaterial3D GravelMaterial()
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.6f, 0.45f, 0.25f);
        mat.Roughness   = 1.0f;
        mat.CullMode    = BaseMaterial3D.CullModeEnum.Disabled;
        return mat;
    }

    private static StandardMaterial3D FenceMaterial()
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoColor = new Color(0.4f, 0.4f, 0.4f);
        mat.Roughness   = 0.9f;
        mat.CullMode    = BaseMaterial3D.CullModeEnum.Disabled;
        return mat;
    }
}
