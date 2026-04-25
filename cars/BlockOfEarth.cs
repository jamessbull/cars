using Godot;
using System.Collections.Generic;

/// <summary>
/// A 2×2×2 earth cube. All faces use the same earth texture.
/// Mesh and material are shared across all instances.
/// Includes a StaticBody3D collision shape so the car can drive on it.
/// </summary>
public partial class BlockOfEarth : MeshInstance3D
{
    /// <summary>Side length — matches tile XZ footprint (CellWidth = CellDepth = 2).</summary>
    public const float Side = TileGeometry.CellWidth;

    private static ArrayMesh _sharedMesh;

    public BlockOfEarth()
    {
        Mesh = GetSharedMesh();

        var body     = new StaticBody3D();
        var collider = new CollisionShape3D();
        collider.Shape = new BoxShape3D { Size = new Vector3(Side, Side, Side) };
        body.AddChild(collider);
        AddChild(body);
    }

    /// <summary>
    /// World-space position for a block at grid column (gx, gz).
    /// Top surface sits at y = 0, matching the bottom of GridY = 0 track tiles.
    /// </summary>
    public static Vector3 GridToWorld(int gx, int gz) =>
        new(gx * TileGeometry.CellWidth  + TileGeometry.CellWidth  / 2f,
            -Side / 2f,
            gz * TileGeometry.CellDepth  + TileGeometry.CellDepth  / 2f);

    // ─── Shared mesh (earth texture on all 6 faces) ───────────────────────────

    private static ArrayMesh GetSharedMesh()
    {
        if (_sharedMesh != null) return _sharedMesh;

        float h = Side / 2f;
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var idx   = new List<int>();

        AppendQuad(verts, norms, uvs, idx, new(-h,h,-h), new(h,h,-h), new(h,h,h),  new(-h,h,h),  new(0,1,0));   // top
        AppendQuad(verts, norms, uvs, idx, new(-h,h,h),  new(h,h,h),  new(h,-h,h), new(-h,-h,h), new(0,0,1));   // +Z
        AppendQuad(verts, norms, uvs, idx, new(h,h,-h),  new(-h,h,-h),new(-h,-h,-h),new(h,-h,-h),new(0,0,-1));  // -Z
        AppendQuad(verts, norms, uvs, idx, new(-h,h,-h), new(-h,h,h), new(-h,-h,h),new(-h,-h,-h),new(-1,0,0));  // -X
        AppendQuad(verts, norms, uvs, idx, new(h,h,h),   new(h,h,-h), new(h,-h,-h),new(h,-h,h),  new(1,0,0));   // +X
        AppendQuad(verts, norms, uvs, idx, new(-h,-h,h), new(h,-h,h), new(h,-h,-h),new(-h,-h,-h),new(0,-1,0));  // bottom

        var arr = new Godot.Collections.Array();
        arr.Resize((int)Mesh.ArrayType.Max);
        arr[(int)Mesh.ArrayType.Vertex] = verts.ToArray();
        arr[(int)Mesh.ArrayType.Normal] = norms.ToArray();
        arr[(int)Mesh.ArrayType.TexUV]  = uvs.ToArray();
        arr[(int)Mesh.ArrayType.Index]  = idx.ToArray();

        _sharedMesh = new ArrayMesh();
        _sharedMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arr);
        _sharedMesh.SurfaceSetMaterial(0, TileMeshBuilder.EarthMaterial());
        return _sharedMesh;
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
}
