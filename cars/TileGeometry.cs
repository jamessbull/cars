public enum TileType
{
    Flat,
    Ramp
}

public struct TileMeshData
{
    public float[] Vertices;
    public float[] Normals;
    public int[] Indices;
}

public static class TileGeometry
{
    public const float CellWidth = 2f;
    public const float CellHeight = 1f;
    public const float CellDepth = 2f;

    public static TileMeshData GenerateTile(TileType type)
    {
        return type switch
        {
            TileType.Flat => GenerateFlat(),
            TileType.Ramp => GenerateRamp(),
            _ => throw new System.ArgumentOutOfRangeException(nameof(type))
        };
    }

    public const float FlatThickness = 0.05f;

    private static TileMeshData GenerateFlat()
    {
        // Thin slab centered on origin: X=[-w/2, w/2], Z=[-d/2, d/2]
        // Top face at Y=0, bottom face at Y=-FlatThickness
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float t = FlatThickness;

        var vertices = new System.Collections.Generic.List<float>();
        var normals = new System.Collections.Generic.List<float>();
        var indices = new System.Collections.Generic.List<int>();

        int vi = 0;

        // Top face (Y=0, facing up)
        AddVertex(vertices, normals, -hw, 0,  hd, 0, 1, 0);
        AddVertex(vertices, normals,  hw, 0,  hd, 0, 1, 0);
        AddVertex(vertices, normals,  hw, 0, -hd, 0, 1, 0);
        AddVertex(vertices, normals, -hw, 0, -hd, 0, 1, 0);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);
        vi += 4;

        // Bottom face (Y=-t, facing down)
        AddVertex(vertices, normals, -hw, -t,  hd, 0, -1, 0);
        AddVertex(vertices, normals, -hw, -t, -hd, 0, -1, 0);
        AddVertex(vertices, normals,  hw, -t, -hd, 0, -1, 0);
        AddVertex(vertices, normals,  hw, -t,  hd, 0, -1, 0);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);
        vi += 4;

        // Front wall (Z=-hd, facing -Z)
        AddVertex(vertices, normals, -hw,  0, -hd, 0, 0, -1);
        AddVertex(vertices, normals,  hw,  0, -hd, 0, 0, -1);
        AddVertex(vertices, normals,  hw, -t, -hd, 0, 0, -1);
        AddVertex(vertices, normals, -hw, -t, -hd, 0, 0, -1);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);
        vi += 4;

        // Back wall (Z=+hd, facing +Z)
        AddVertex(vertices, normals,  hw,  0, hd, 0, 0, 1);
        AddVertex(vertices, normals, -hw,  0, hd, 0, 0, 1);
        AddVertex(vertices, normals, -hw, -t, hd, 0, 0, 1);
        AddVertex(vertices, normals,  hw, -t, hd, 0, 0, 1);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);
        vi += 4;

        // Left wall (X=-hw, facing -X)
        AddVertex(vertices, normals, -hw,  0,  hd, -1, 0, 0);
        AddVertex(vertices, normals, -hw,  0, -hd, -1, 0, 0);
        AddVertex(vertices, normals, -hw, -t, -hd, -1, 0, 0);
        AddVertex(vertices, normals, -hw, -t,  hd, -1, 0, 0);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);
        vi += 4;

        // Right wall (X=+hw, facing +X)
        AddVertex(vertices, normals, hw,  0, -hd, 1, 0, 0);
        AddVertex(vertices, normals, hw,  0,  hd, 1, 0, 0);
        AddVertex(vertices, normals, hw, -t,  hd, 1, 0, 0);
        AddVertex(vertices, normals, hw, -t, -hd, 1, 0, 0);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);

        return new TileMeshData
        {
            Vertices = vertices.ToArray(),
            Normals = normals.ToArray(),
            Indices = indices.ToArray()
        };
    }

    private static TileMeshData GenerateRamp()
    {
        // Canonical ramp rises toward -Z (North), centered on origin
        // Back edge (Z=+hd) at Y=0, front edge (Z=-hd) at Y=CellHeight
        // Solid wedge: top surface + bottom + front wall + 2 side walls
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float h = CellHeight;

        // Ramp surface normal: slope from (0,0,hd) to (0,h,-hd)
        // Normal = (0, d, h) / sqrt(d*d + h*h) where d = CellDepth
        float d = CellDepth;
        float slopeLen = System.MathF.Sqrt(d * d + h * h);
        float ny = d / slopeLen;
        float nz = h / slopeLen;

        var vertices = new System.Collections.Generic.List<float>();
        var normals = new System.Collections.Generic.List<float>();
        var indices = new System.Collections.Generic.List<int>();

        int vi = 0;

        // Face 1: Top (ramp surface) - 4 verts, 2 tris
        AddVertex(vertices, normals, -hw, 0, hd, 0, ny, nz);
        AddVertex(vertices, normals,  hw, 0, hd, 0, ny, nz);
        AddVertex(vertices, normals,  hw, h, -hd, 0, ny, nz);
        AddVertex(vertices, normals, -hw, h, -hd, 0, ny, nz);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);
        vi += 4;

        // Face 2: Bottom - quad at Y=0, facing down
        AddVertex(vertices, normals, -hw, 0,  hd, 0, -1, 0);
        AddVertex(vertices, normals, -hw, 0, -hd, 0, -1, 0);
        AddVertex(vertices, normals,  hw, 0, -hd, 0, -1, 0);
        AddVertex(vertices, normals,  hw, 0,  hd, 0, -1, 0);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);
        vi += 4;

        // Face 3: Front wall (Z=-hd) - quad, facing -Z
        // Vertex order chosen so geometric normal points -Z (outward)
        AddVertex(vertices, normals, -hw, 0, -hd, 0, 0, -1);
        AddVertex(vertices, normals, -hw, h, -hd, 0, 0, -1);
        AddVertex(vertices, normals,  hw, h, -hd, 0, 0, -1);
        AddVertex(vertices, normals,  hw, 0, -hd, 0, 0, -1);
        AddTriangle(indices, vi, 0, 1, 2);
        AddTriangle(indices, vi, 0, 2, 3);
        vi += 4;

        // Face 4: Left wall (X=-hw) - triangle, facing -X
        // Vertex order: geometric normal points -X (outward)
        AddVertex(vertices, normals, -hw, 0,  hd, -1, 0, 0);
        AddVertex(vertices, normals, -hw, h, -hd, -1, 0, 0);
        AddVertex(vertices, normals, -hw, 0, -hd, -1, 0, 0);
        AddTriangle(indices, vi, 0, 1, 2);
        vi += 3;

        // Face 5: Right wall (X=+hw) - triangle, facing +X
        // Vertex order: geometric normal points +X (outward)
        AddVertex(vertices, normals, hw, 0,  hd, 1, 0, 0);
        AddVertex(vertices, normals, hw, 0, -hd, 1, 0, 0);
        AddVertex(vertices, normals, hw, h, -hd, 1, 0, 0);
        AddTriangle(indices, vi, 0, 1, 2);

        return new TileMeshData
        {
            Vertices = vertices.ToArray(),
            Normals = normals.ToArray(),
            Indices = indices.ToArray()
        };
    }

    private static void AddVertex(System.Collections.Generic.List<float> vertices,
        System.Collections.Generic.List<float> normals,
        float x, float y, float z, float nx, float ny, float nz)
    {
        vertices.Add(x);
        vertices.Add(y);
        vertices.Add(z);
        normals.Add(nx);
        normals.Add(ny);
        normals.Add(nz);
    }

    private static void AddTriangle(System.Collections.Generic.List<int> indices,
        int baseIndex, int a, int b, int c)
    {
        indices.Add(baseIndex + a);
        indices.Add(baseIndex + b);
        indices.Add(baseIndex + c);
    }
}
