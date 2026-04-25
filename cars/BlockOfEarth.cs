using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// A 2×2×2 cartoony earth/grass cube. Top face shows grass; all other faces show earth.
/// Mesh and textures are generated once and shared across all instances.
/// Includes a StaticBody3D collision shape so the car can drive on it.
/// </summary>
public partial class BlockOfEarth : MeshInstance3D
{
    /// <summary>Side length — matches tile XZ footprint (CellWidth = CellDepth = 2).</summary>
    public const float Side = TileGeometry.CellWidth;

    private static ArrayMesh      _sharedMesh;
    private static ImageTexture   _grassTexture;
    private static ImageTexture   _earthTexture;

    public BlockOfEarth()
    {
        Mesh = GetSharedMesh();

        var body     = new StaticBody3D();
        var collider = new CollisionShape3D();
        var shape    = new BoxShape3D { Size = new Vector3(Side, Side, Side) };
        collider.Shape = shape;
        body.AddChild(collider);
        AddChild(body);
    }

    /// <summary>
    /// World-space position for a block at grid column (gx, gz).
    /// The top surface sits exactly at y = 0, matching the bottom of GridY = 0 track tiles.
    /// </summary>
    public static Vector3 GridToWorld(int gx, int gz) =>
        new(gx * TileGeometry.CellWidth  + TileGeometry.CellWidth  / 2f,
            -Side / 2f,
            gz * TileGeometry.CellDepth  + TileGeometry.CellDepth  / 2f);

    // ─── Shared mesh ──────────────────────────────────────────────────────────

    private static ArrayMesh GetSharedMesh()
    {
        if (_sharedMesh != null) return _sharedMesh;
        _sharedMesh = BuildMesh();
        return _sharedMesh;
    }

    private static ArrayMesh BuildMesh()
    {
        float h = Side / 2f;
        var mesh = new ArrayMesh();

        // Surface 0: top face — grass
        {
            var verts = new Vector3[] { new(-h,h,-h), new(h,h,-h), new(h,h,h), new(-h,h,h) };
            var norms = new Vector3[] { Vector3.Up, Vector3.Up, Vector3.Up, Vector3.Up };
            var uvs   = new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) };
            var idx   = new int[]     { 0,1,2, 0,2,3 };
            AppendSurface(mesh, verts, norms, uvs, idx);
            mesh.SurfaceSetMaterial(0, MakeMaterial(GrassTexture()));
        }

        // Surface 1: 4 sides + bottom — earth
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs   = new List<Vector2>();
            var idx   = new List<int>();

            AppendQuad(verts, norms, uvs, idx,
                new(-h,h, h), new(h,h, h), new(h,-h, h), new(-h,-h, h), new(0,0,1));   // +Z
            AppendQuad(verts, norms, uvs, idx,
                new(h,h,-h), new(-h,h,-h), new(-h,-h,-h), new(h,-h,-h), new(0,0,-1));  // -Z
            AppendQuad(verts, norms, uvs, idx,
                new(-h,h,-h), new(-h,h,h), new(-h,-h,h), new(-h,-h,-h), new(-1,0,0));  // -X
            AppendQuad(verts, norms, uvs, idx,
                new(h,h,h), new(h,h,-h), new(h,-h,-h), new(h,-h,h), new(1,0,0));       // +X
            AppendQuad(verts, norms, uvs, idx,
                new(-h,-h,h), new(h,-h,h), new(h,-h,-h), new(-h,-h,-h), new(0,-1,0));  // bottom

            AppendSurface(mesh, verts.ToArray(), norms.ToArray(), uvs.ToArray(), idx.ToArray());
            mesh.SurfaceSetMaterial(1, MakeMaterial(EarthTexture()));
        }

        return mesh;
    }

    private static void AppendSurface(ArrayMesh mesh, Vector3[] verts, Vector3[] norms,
        Vector2[] uvs, int[] idx)
    {
        var arr = new Godot.Collections.Array();
        arr.Resize((int)Mesh.ArrayType.Max);
        arr[(int)Mesh.ArrayType.Vertex] = verts;
        arr[(int)Mesh.ArrayType.Normal] = norms;
        arr[(int)Mesh.ArrayType.TexUV]  = uvs;
        arr[(int)Mesh.ArrayType.Index]  = idx;
        mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr);
    }

    private static void AppendQuad(List<Vector3> verts, List<Vector3> norms, List<Vector2> uvs,
        List<int> idx, Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 normal)
    {
        int b = verts.Count;
        verts.AddRange(new[] { v0, v1, v2, v3 });
        norms.AddRange(new[] { normal, normal, normal, normal });
        uvs.AddRange(new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) });
        idx.AddRange(new[] { b,b+1,b+2, b,b+2,b+3 });
    }

    private static StandardMaterial3D MakeMaterial(ImageTexture tex)
    {
        var mat = new StandardMaterial3D();
        mat.AlbedoTexture  = tex;
        mat.TextureFilter  = BaseMaterial3D.TextureFilterEnum.Nearest; // crisp pixel look
        mat.Roughness      = 0.95f;
        mat.CullMode       = BaseMaterial3D.CullModeEnum.Disabled;
        return mat;
    }

    // ─── Procedural textures ──────────────────────────────────────────────────

    private static ImageTexture GrassTexture()
    {
        if (_grassTexture != null) return _grassTexture;

        const int Size = 64;
        var img = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        img.Fill(new Color(0.34f, 0.72f, 0.21f)); // bright cartoon green base

        var rng = new Random(42);

        // Darker clumps — grass shadow patches
        var darkGreen = new Color(0.19f, 0.51f, 0.11f);
        for (int i = 0; i < 20; i++)
            PaintBlob(img, rng, Size, darkGreen, minR: 3, maxR: 8);

        // Lighter highlights — grass tips catching light
        var lightGreen = new Color(0.50f, 0.87f, 0.28f);
        for (int i = 0; i < 12; i++)
            PaintBlob(img, rng, Size, lightGreen, minR: 2, maxR: 5);

        // A few bright yellow-green spots for a cartoonish pop
        var pop = new Color(0.62f, 0.90f, 0.18f);
        for (int i = 0; i < 6; i++)
            PaintBlob(img, rng, Size, pop, minR: 1, maxR: 3);

        _grassTexture = ImageTexture.CreateFromImage(img);
        return _grassTexture;
    }

    private static ImageTexture EarthTexture()
    {
        if (_earthTexture != null) return _earthTexture;

        const int Size = 64;
        var img = Image.CreateEmpty(Size, Size, false, Image.Format.Rgba8);
        img.Fill(new Color(0.62f, 0.37f, 0.17f)); // warm medium brown base

        var rng = new Random(99);

        // Darker clumps — dirt lumps and shadows
        var darkBrown = new Color(0.38f, 0.21f, 0.08f);
        for (int i = 0; i < 22; i++)
            PaintBlob(img, rng, Size, darkBrown, minR: 3, maxR: 8);

        // Lighter tan patches — dry surface highlights
        var lightTan = new Color(0.82f, 0.58f, 0.30f);
        for (int i = 0; i < 14; i++)
            PaintBlob(img, rng, Size, lightTan, minR: 2, maxR: 5);

        // Tiny stone-grey flecks for gritty texture
        var stone = new Color(0.54f, 0.51f, 0.47f);
        for (int i = 0; i < 10; i++)
            PaintBlob(img, rng, Size, stone, minR: 1, maxR: 2);

        _earthTexture = ImageTexture.CreateFromImage(img);
        return _earthTexture;
    }

    private static void PaintBlob(Image img, Random rng, int size, Color color, int minR, int maxR)
    {
        int cx = rng.Next(size);
        int cy = rng.Next(size);
        int r  = rng.Next(minR, maxR + 1);
        for (int dy = -r; dy <= r; dy++)
        for (int dx = -r; dx <= r; dx++)
        {
            if (dx * dx + dy * dy > r * r) continue;
            int px = ((cx + dx) % size + size) % size;
            int py = ((cy + dy) % size + size) % size;
            img.SetPixel(px, py, color);
        }
    }
}
