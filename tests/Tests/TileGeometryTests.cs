namespace Tests;

public class TileGeometryTests
{
    [Fact]
    public void Flat_IsThinSlab()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        // 2 faces (top + bottom) * 4 verts = 8 verts, 3 floats each
        Assert.Equal(24, tile.Vertices.Length);
        Assert.Equal(24, tile.Normals.Length);
        // 2 faces * 2 tris * 3 indices = 12
        Assert.Equal(12, tile.Indices.Length);
    }

    [Fact]
    public void Flat_TopFaceAtYZero()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        // First 4 vertices are the top face — all at Y=0
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(0f, tile.Vertices[i * 3 + 1]); // Y component
        }
    }

    [Fact]
    public void Flat_BottomFaceAtNegativeThickness()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        // Vertices 4-7 are the bottom face — all at Y=-FlatThickness
        for (int i = 4; i < 8; i++)
        {
            Assert.Equal(-TileGeometry.FlatThickness, tile.Vertices[i * 3 + 1]);
        }
    }

    [Fact]
    public void Flat_TopNormalsPointUp()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        // First 4 normals (top face) point up
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(0f, tile.Normals[i * 3]);
            Assert.Equal(1f, tile.Normals[i * 3 + 1]);
            Assert.Equal(0f, tile.Normals[i * 3 + 2]);
        }
    }

    [Fact]
    public void Flat_CenteredOnOrigin_X()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < tile.Vertices.Length; i += 3)
        {
            if (tile.Vertices[i] < minX) minX = tile.Vertices[i];
            if (tile.Vertices[i] > maxX) maxX = tile.Vertices[i];
        }
        Assert.Equal(-TileGeometry.CellWidth / 2f, minX);
        Assert.Equal(TileGeometry.CellWidth / 2f, maxX);
    }

    [Fact]
    public void Flat_CenteredOnOrigin_Z()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        float minZ = float.MaxValue, maxZ = float.MinValue;
        for (int i = 2; i < tile.Vertices.Length; i += 3)
        {
            if (tile.Vertices[i] < minZ) minZ = tile.Vertices[i];
            if (tile.Vertices[i] > maxZ) maxZ = tile.Vertices[i];
        }
        Assert.Equal(-TileGeometry.CellDepth / 2f, minZ);
        Assert.Equal(TileGeometry.CellDepth / 2f, maxZ);
    }

    [Fact]
    public void Flat_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        int vertexCount = tile.Vertices.Length / 3;
        foreach (int idx in tile.Indices)
        {
            Assert.InRange(idx, 0, vertexCount - 1);
        }
    }

    [Fact]
    public void Flat_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        VerifyAllWindings(tile);
    }

    [Fact]
    public void Ramp_VerticesAndNormalsMatch()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        Assert.Equal(tile.Vertices.Length, tile.Normals.Length);
    }

    [Fact]
    public void Ramp_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        int vertexCount = tile.Vertices.Length / 3;
        foreach (int idx in tile.Indices)
        {
            Assert.InRange(idx, 0, vertexCount - 1);
        }
    }

    [Fact]
    public void Ramp_MaxHeightEqualsCellHeight()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        float maxY = float.MinValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
        {
            if (tile.Vertices[i] > maxY) maxY = tile.Vertices[i];
        }
        Assert.Equal(TileGeometry.CellHeight, maxY);
    }

    [Fact]
    public void Ramp_CenteredOnOrigin_X()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < tile.Vertices.Length; i += 3)
        {
            if (tile.Vertices[i] < minX) minX = tile.Vertices[i];
            if (tile.Vertices[i] > maxX) maxX = tile.Vertices[i];
        }
        Assert.Equal(-TileGeometry.CellWidth / 2f, minX);
        Assert.Equal(TileGeometry.CellWidth / 2f, maxX);
    }

    [Fact]
    public void Ramp_HasMultipleFaces()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        // Ramp surface only = 2 triangles
        Assert.Equal(6, tile.Indices.Length);
    }

    [Fact]
    public void Ramp_TopSurfaceNormalPointsDownSlope()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        // Normal points toward the high end of the ramp (-Z in canonical orientation)
        // so the front face is visible from the approach (low) side
        float ny = tile.Normals[1];
        float nz = tile.Normals[2];
        Assert.True(ny < 0, "Ramp surface normal Y should be negative (front face points down-slope)");
        Assert.True(nz < 0, "Ramp surface normal Z should be negative (toward high end)");
    }

    [Fact]
    public void Ramp_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        VerifyAllWindings(tile);
    }

    [Fact]
    public void Gravel_SameGeometryAsFlat()
    {
        var gravel = TileGeometry.GenerateTile(TileType.Gravel);
        var flat = TileGeometry.GenerateTile(TileType.Flat);
        Assert.Equal(flat.Vertices.Length, gravel.Vertices.Length);
        Assert.Equal(flat.Indices.Length, gravel.Indices.Length);
    }

    [Fact]
    public void Gravel_TopFaceAtYZero()
    {
        var tile = TileGeometry.GenerateTile(TileType.Gravel);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(0f, tile.Vertices[i * 3 + 1]);
        }
    }

    [Fact]
    public void Fence_Has28Verts84Floats()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        // 7 faces (ground top/bottom + wall front/back/top/left/right) * 4 verts = 28 verts, 84 floats
        Assert.Equal(84, tile.Vertices.Length);
        Assert.Equal(84, tile.Normals.Length);
    }

    [Fact]
    public void Fence_Has42Indices()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        // 7 faces * 2 tris * 3 indices = 42
        Assert.Equal(42, tile.Indices.Length);
    }

    [Fact]
    public void Fence_MaxYEqualsFenceHeight()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        float maxY = float.MinValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
        {
            if (tile.Vertices[i] > maxY) maxY = tile.Vertices[i];
        }
        Assert.Equal(TileGeometry.FenceHeight, maxY);
    }

    [Fact]
    public void Fence_WallNormalFacesPositiveZ()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        // Wall verts are indices 8-11 (3rd face), check their normals
        for (int i = 8; i < 12; i++)
        {
            Assert.Equal(0f, tile.Normals[i * 3]);     // nx
            Assert.Equal(0f, tile.Normals[i * 3 + 1]); // ny
            Assert.Equal(1f, tile.Normals[i * 3 + 2]); // nz
        }
    }

    [Fact]
    public void Fence_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        int vertexCount = tile.Vertices.Length / 3;
        foreach (int idx in tile.Indices)
        {
            Assert.InRange(idx, 0, vertexCount - 1);
        }
    }

    [Fact]
    public void Fence_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        VerifyAllWindings(tile);
    }

    [Fact]
    public void CellSize_HasExpectedValues()
    {
        Assert.Equal(2f, TileGeometry.CellWidth);
        Assert.Equal(1f, TileGeometry.CellHeight);
        Assert.Equal(2f, TileGeometry.CellDepth);
    }

    private static void VerifyAllWindings(TileMeshData tile)
    {
        for (int t = 0; t < tile.Indices.Length; t += 3)
        {
            int i0 = tile.Indices[t] * 3;
            int i1 = tile.Indices[t + 1] * 3;
            int i2 = tile.Indices[t + 2] * 3;

            float v0x = tile.Vertices[i0], v0y = tile.Vertices[i0 + 1], v0z = tile.Vertices[i0 + 2];
            float v1x = tile.Vertices[i1], v1y = tile.Vertices[i1 + 1], v1z = tile.Vertices[i1 + 2];
            float v2x = tile.Vertices[i2], v2y = tile.Vertices[i2 + 1], v2z = tile.Vertices[i2 + 2];

            float e1x = v1x - v0x, e1y = v1y - v0y, e1z = v1z - v0z;
            float e2x = v2x - v0x, e2y = v2y - v0y, e2z = v2z - v0z;

            float gx = e1y * e2z - e1z * e2y;
            float gy = e1z * e2x - e1x * e2z;
            float gz = e1x * e2y - e1y * e2x;

            float nx = tile.Normals[i0], ny = tile.Normals[i0 + 1], nz = tile.Normals[i0 + 2];
            float dot = gx * nx + gy * ny + gz * nz;
            Assert.True(dot > 0, $"Triangle {t / 3} has inverted winding (dot={dot})");
        }
    }
}
