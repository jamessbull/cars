using Godot;
using System;

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
            TileType.Flat            => FlatMaterial(),
            TileType.Ramp            => RampMaterial(),
            TileType.Gravel          => GravelMaterial(),
            TileType.Fence           => FenceMaterial(),
            TileType.RampEntry       => RampMaterial(),
            TileType.RampExit        => RampMaterial(),
            TileType.SlopeEdge       => GravelMaterial(),
            TileType.SlopeCorner     => GravelMaterial(),
            TileType.RampEntryCorner => RampMaterial(),
            TileType.SolidRampEntry  => RampMaterial(),
            TileType.Grass           => GrassMaterial(),
            _                        => FlatMaterial()
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
        mat.AlbedoTexture = FlatTexture();
        mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        mat.Roughness     = 0.85f;
        mat.CullMode      = BaseMaterial3D.CullModeEnum.Disabled;
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

    private static StandardMaterial3D GrassMaterial()
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoTexture = GrassTexture();
        mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        mat.Roughness     = 0.95f;
        mat.CullMode      = BaseMaterial3D.CullModeEnum.Disabled;
        return mat;
    }

    public static StandardMaterial3D EarthMaterial()
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoTexture = EarthTexture();
        mat.TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest;
        mat.Roughness     = 1.0f;
        mat.CullMode      = BaseMaterial3D.CullModeEnum.Disabled;
        return mat;
    }

    // ─── Procedural textures ──────────────────────────────────────────────────

    private static ImageTexture _flatTexture;
    private static ImageTexture _grassTexture;
    private static ImageTexture _earthTexture;

    private static ImageTexture FlatTexture()
    {
        if (_flatTexture != null) return _flatTexture;
        const int Size = 64;
        var img = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        img.Fill(new Color(0.54f, 0.54f, 0.54f)); // mid-grey asphalt base
        var rng = new Random(7);
        // Fine dark speckles — aggregate and tar variations
        for (int i = 0; i < 45; i++) PaintBlob(img, rng, Size, new Color(0.38f, 0.38f, 0.38f), 1, 3);
        // Light highlights — pale stones / worn surface
        for (int i = 0; i < 28; i++) PaintBlob(img, rng, Size, new Color(0.67f, 0.67f, 0.67f), 1, 2);
        // Very fine dark flecks for grit
        for (int i = 0; i < 20; i++) PaintBlob(img, rng, Size, new Color(0.30f, 0.30f, 0.30f), 1, 1);
        _flatTexture = ImageTexture.CreateFromImage(img);
        return _flatTexture;
    }

    private static ImageTexture GrassTexture()
    {
        if (_grassTexture != null) return _grassTexture;
        const int Size = 64;
        var img = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        img.Fill(new Color(0.34f, 0.72f, 0.21f));
        var rng = new Random(42);
        for (int i = 0; i < 20; i++) PaintBlob(img, rng, Size, new Color(0.19f, 0.51f, 0.11f), 3, 8);
        for (int i = 0; i < 12; i++) PaintBlob(img, rng, Size, new Color(0.50f, 0.87f, 0.28f), 2, 5);
        for (int i = 0; i <  6; i++) PaintBlob(img, rng, Size, new Color(0.62f, 0.90f, 0.18f), 1, 3);
        _grassTexture = ImageTexture.CreateFromImage(img);
        return _grassTexture;
    }

    public static ImageTexture EarthTexture()
    {
        if (_earthTexture != null) return _earthTexture;
        const int Size = 64;
        var img = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        img.Fill(new Color(0.62f, 0.37f, 0.17f));
        var rng = new Random(99);
        for (int i = 0; i < 22; i++) PaintBlob(img, rng, Size, new Color(0.38f, 0.21f, 0.08f), 3, 8);
        for (int i = 0; i < 14; i++) PaintBlob(img, rng, Size, new Color(0.82f, 0.58f, 0.30f), 2, 5);
        for (int i = 0; i < 10; i++) PaintBlob(img, rng, Size, new Color(0.54f, 0.51f, 0.47f), 1, 2);
        _earthTexture = ImageTexture.CreateFromImage(img);
        return _earthTexture;
    }

    private static void PaintBlob(Image img, Random rng, int size, Color color, int minR, int maxR)
    {
        int cx = rng.Next(size), cy = rng.Next(size), r = rng.Next(minR, maxR + 1);
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (dx * dx + dy * dy > r * r) continue;
            img.SetPixel(((cx + dx) % size + size) % size, ((cy + dy) % size + size) % size, color);
        }
    }
}
