using System;

namespace Tests;

public class TileGeometryTests
{
    [Fact]
    public void CellSize_HasExpectedValues()
    {
        Assert.Equal(2f, TileGeometry.CellWidth);
        Assert.Equal(2f, TileGeometry.CellDepth);
        // CellHeight = ArcRise / ArcGridSpan = R(1 - cos(θ)) / 3
        float expectedH = TileGeometry.ArcRadius * (1f - MathF.Cos(TileGeometry.ExitAngleRad)) / TileGeometry.ArcGridSpan;
        Assert.Equal(expectedH, TileGeometry.CellHeight, 0.001f);
    }

    [Fact]
    public void TileThickness_IsHalfMeter()
    {
        Assert.Equal(0.5f, TileGeometry.TileThickness);
    }

    // --- Flat tile ---

    [Fact]
    public void Flat_HasTopBottomAndSideWalls()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        // 6 faces (top + bottom + 4 sides) * 4 verts = 24 verts
        Assert.Equal(24, tile.Vertices.Length / 3);
        // 6 faces * 2 tris = 12 tris * 3 indices = 36
        Assert.Equal(36, tile.Indices.Length);
    }

    [Fact]
    public void Flat_TopFaceAtYZero()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        // First 4 vertices are the top face — all at Y=0
        for (int i = 0; i < 4; i++)
            Assert.Equal(0f, tile.Vertices[i * 3 + 1]);
    }

    [Fact]
    public void Flat_BottomFaceAtNegativeThickness()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        // Vertices 4-7 are the bottom face — all at Y=-TileThickness
        for (int i = 4; i < 8; i++)
            Assert.Equal(-TileGeometry.TileThickness, tile.Vertices[i * 3 + 1]);
    }

    [Fact]
    public void Flat_TopNormalsPointUp()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        for (int i = 0; i < 4; i++)
        {
            Assert.Equal(0f, tile.Normals[i * 3]);
            Assert.Equal(1f, tile.Normals[i * 3 + 1]);
            Assert.Equal(0f, tile.Normals[i * 3 + 2]);
        }
    }

    [Fact]
    public void Flat_CenteredOnOrigin()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        AssertCenteredX(tile);
        AssertCenteredZ(tile);
    }

    [Fact]
    public void Flat_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void Flat_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.Flat);
        VerifyAllWindings(tile);
    }

    // --- Ramp tile ---

    [Fact]
    public void Ramp_MaxHeightEqualsRampRise()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        float maxY = float.MinValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
            if (tile.Vertices[i] > maxY) maxY = tile.Vertices[i];
        Assert.Equal(TileGeometry.RampRise, maxY, 0.001f);
    }

    [Fact]
    public void Ramp_HasSixFaces()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        // 6 faces (top + bottom + 4 thin edge walls) * 4 verts = 24 verts
        Assert.Equal(24, tile.Vertices.Length / 3);
    }

    [Fact]
    public void Ramp_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void Ramp_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        VerifyAllWindings(tile);
    }

    [Fact]
    public void Ramp_CenteredOnOriginX()
    {
        var tile = TileGeometry.GenerateTile(TileType.Ramp);
        AssertCenteredX(tile);
    }

    // --- Gravel tile ---

    [Fact]
    public void Gravel_SameGeometryAsFlat()
    {
        var gravel = TileGeometry.GenerateTile(TileType.Gravel);
        var flat = TileGeometry.GenerateTile(TileType.Flat);
        Assert.Equal(flat.Vertices.Length, gravel.Vertices.Length);
        Assert.Equal(flat.Indices.Length, gravel.Indices.Length);
    }

    // --- Fence tile ---

    [Fact]
    public void Fence_MaxYEqualsFenceHeight()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        float maxY = float.MinValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
            if (tile.Vertices[i] > maxY) maxY = tile.Vertices[i];
        Assert.Equal(TileGeometry.FenceHeight, maxY);
    }

    [Fact]
    public void Fence_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void Fence_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.Fence);
        VerifyAllWindings(tile);
    }

    // --- RampEntry tile ---

    [Fact]
    public void RampEntry_StartsAtYZero()
    {
        // Back edge (Z=+hd) should be at Y=0 (flat)
        float y = TileGeometry.RampEntryY(0);
        Assert.Equal(0f, y, 0.001f);
    }

    [Fact]
    public void RampEntry_EndsAtArcRise()
    {
        // Front edge (Z=-hd) reaches Y=ArcRise (= 3 × CellHeight)
        float y = TileGeometry.RampEntryY(TileGeometry.CellDepth);
        Assert.Equal(TileGeometry.ArcRise, y, 0.001f);
    }

    [Fact]
    public void RampEntry_StartsFlat()
    {
        float slope = TileGeometry.RampEntrySlope(0);
        Assert.Equal(0f, slope, 0.001f);
    }

    [Fact]
    public void RampEntry_EndsAtExitAngle()
    {
        // slope = tan(30°) ≈ 0.5774
        float slope = TileGeometry.RampEntrySlope(TileGeometry.CellDepth);
        float expectedSlope = MathF.Tan(TileGeometry.ExitAngleRad);
        Assert.Equal(expectedSlope, slope, 0.001f);
    }

    [Fact]
    public void RampEntry_MonotonicallyIncreasing()
    {
        float prev = 0f;
        for (int i = 1; i <= 20; i++)
        {
            float z = TileGeometry.CellDepth * i / 20f;
            float y = TileGeometry.RampEntryY(z);
            Assert.True(y >= prev, $"RampEntry Y decreased at z={z}: {y} < {prev}");
            prev = y;
        }
    }

    [Fact]
    public void RampEntry_HasThickness()
    {
        var tile = TileGeometry.GenerateTile(TileType.RampEntry);
        float minY = float.MaxValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
            if (tile.Vertices[i] < minY) minY = tile.Vertices[i];
        // Minimum Y should be at least -TileThickness (back edge bottom)
        Assert.True(minY <= -TileGeometry.TileThickness + 0.01f);
    }

    [Fact]
    public void RampEntry_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.RampEntry);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void RampEntry_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.RampEntry);
        VerifyAllWindings(tile);
    }

    // --- RampExit tile ---

    [Fact]
    public void RampExit_StartsAtYZero()
    {
        float y = TileGeometry.RampExitY(0);
        Assert.Equal(0f, y, 0.001f);
    }

    [Fact]
    public void RampExit_EndsAtArcRise()
    {
        float y = TileGeometry.RampExitY(TileGeometry.CellDepth);
        Assert.Equal(TileGeometry.ArcRise, y, 0.001f);
    }

    [Fact]
    public void RampExit_StartsAtExitAngle()
    {
        // RampExit slope at z=0 should be tan(ExitAngle)
        float slope = TileGeometry.RampExitSlope(0);
        float expectedSlope = MathF.Tan(TileGeometry.ExitAngleRad);
        Assert.Equal(expectedSlope, slope, 0.001f);
    }

    [Fact]
    public void RampExit_EndsFlat()
    {
        // The slope at z=D should be 0; max Y on the tile equals ArcRise
        var tile = TileGeometry.GenerateTile(TileType.RampExit);
        float maxY = float.MinValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
            if (tile.Vertices[i] > maxY) maxY = tile.Vertices[i];
        Assert.Equal(TileGeometry.ArcRise, maxY, 0.001f);
    }

    [Fact]
    public void RampExit_MonotonicallyIncreasing()
    {
        float prev = 0f;
        for (int i = 1; i <= 20; i++)
        {
            float z = TileGeometry.CellDepth * i / 20f;
            float y = TileGeometry.RampExitY(z);
            Assert.True(y >= prev, $"RampExit Y decreased at z={z}: {y} < {prev}");
            prev = y;
        }
    }

    [Fact]
    public void RampExit_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.RampExit);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void RampExit_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.RampExit);
        VerifyAllWindings(tile);
    }

    // --- SlopeEdge tile ---

    [Fact]
    public void SlopeEdge_MaxHeightEqualsCellHeight()
    {
        var tile = TileGeometry.GenerateTile(TileType.SlopeEdge);
        float maxY = float.MinValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
            if (tile.Vertices[i] > maxY) maxY = tile.Vertices[i];
        Assert.Equal(TileGeometry.CellHeight, maxY, 0.01f);
    }

    [Fact]
    public void SlopeEdge_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.SlopeEdge);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void SlopeEdge_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.SlopeEdge);
        VerifyAllWindings(tile);
    }

    // --- SlopeCorner tile ---

    [Fact]
    public void SlopeCorner_HasRaisedCorner()
    {
        var tile = TileGeometry.GenerateTile(TileType.SlopeCorner);
        float maxY = float.MinValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
            if (tile.Vertices[i] > maxY) maxY = tile.Vertices[i];
        // Raised corner should be close to CellHeight (exact value depends on falloff)
        Assert.True(maxY > TileGeometry.CellHeight * 0.5f, $"Max Y ({maxY}) should be significantly raised");
    }

    [Fact]
    public void SlopeCorner_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.SlopeCorner);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void SlopeCorner_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.SlopeCorner);
        VerifyAllWindings(tile);
    }

    // --- SolidRampEntry tile ---

    [Fact]
    public void SolidRampEntry_NoNegativeY()
    {
        // Solid wedge sits on Y=0 floor — no vertex should go below Y=0
        var tile = TileGeometry.GenerateTile(TileType.SolidRampEntry);
        for (int i = 1; i < tile.Vertices.Length; i += 3)
            Assert.True(tile.Vertices[i] >= -0.001f, $"Vertex Y={tile.Vertices[i]} is below Y=0");
    }

    [Fact]
    public void SolidRampEntry_MaxHeightIsArcRise()
    {
        var tile = TileGeometry.GenerateTile(TileType.SolidRampEntry);
        float maxY = float.MinValue;
        for (int i = 1; i < tile.Vertices.Length; i += 3)
            if (tile.Vertices[i] > maxY) maxY = tile.Vertices[i];
        Assert.Equal(TileGeometry.ArcRise, maxY, 0.001f);
    }

    [Fact]
    public void SolidRampEntry_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.SolidRampEntry);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void SolidRampEntry_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.SolidRampEntry);
        VerifyAllWindings(tile);
    }

    // --- RampEntryCorner tile ---

    [Fact]
    public void RampEntryCorner_ThreeCornersAtZero()
    {
        float hw = TileGeometry.CellWidth / 2f;
        float cd = TileGeometry.CellDepth;
        Assert.Equal(0f, TileGeometry.RampEntryCornerY(-hw, 0f),    0.001f); // back-left
        Assert.Equal(0f, TileGeometry.RampEntryCornerY(+hw, 0f),    0.001f); // back-right
        Assert.Equal(0f, TileGeometry.RampEntryCornerY(-hw, cd),    0.001f); // front-left
    }

    [Fact]
    public void RampEntryCorner_RaisedCornerAtArcRise()
    {
        float hw = TileGeometry.CellWidth / 2f;
        float cd = TileGeometry.CellDepth;
        Assert.Equal(TileGeometry.ArcRise, TileGeometry.RampEntryCornerY(+hw, cd), 0.001f);
    }

    [Fact]
    public void RampEntryCorner_FrontEdgeMatchesRampEntryProfile()
    {
        // h(x, CellDepth) should equal RampEntryY(x + hw) for all x ∈ [-hw, +hw].
        float hw = TileGeometry.CellWidth / 2f;
        float cd = TileGeometry.CellDepth;
        for (int i = 0; i <= 10; i++)
        {
            float x = -hw + i * (TileGeometry.CellWidth / 10f);
            float expected = TileGeometry.RampEntryY(x + hw);
            float actual = TileGeometry.RampEntryCornerY(x, cd);
            Assert.Equal(expected, actual, 0.001f);
        }
    }

    [Fact]
    public void RampEntryCorner_RightEdgeMatchesRampEntryProfile()
    {
        // h(+hw, z) should equal RampEntryY(z) for all z ∈ [0, CellDepth].
        float hw = TileGeometry.CellWidth / 2f;
        float cd = TileGeometry.CellDepth;
        for (int i = 0; i <= 10; i++)
        {
            float z = i * (cd / 10f);
            float expected = TileGeometry.RampEntryY(z);
            float actual = TileGeometry.RampEntryCornerY(hw, z);
            Assert.Equal(expected, actual, 0.001f);
        }
    }

    [Fact]
    public void RampEntryCorner_IndicesInRange()
    {
        var tile = TileGeometry.GenerateTile(TileType.RampEntryCorner);
        AssertIndicesInRange(tile);
    }

    [Fact]
    public void RampEntryCorner_AllFacesHaveCorrectWinding()
    {
        var tile = TileGeometry.GenerateTile(TileType.RampEntryCorner);
        VerifyAllWindings(tile);
    }

    // --- All tile types generate without error ---

    [Theory]
    [InlineData(TileType.Flat)]
    [InlineData(TileType.Ramp)]
    [InlineData(TileType.Gravel)]
    [InlineData(TileType.Fence)]
    [InlineData(TileType.RampEntry)]
    [InlineData(TileType.RampExit)]
    [InlineData(TileType.SlopeEdge)]
    [InlineData(TileType.SlopeCorner)]
    [InlineData(TileType.RampEntryCorner)]
    [InlineData(TileType.SolidRampEntry)]
    public void AllTiles_VerticesAndNormalsMatch(TileType type)
    {
        var tile = TileGeometry.GenerateTile(type);
        Assert.Equal(tile.Vertices.Length, tile.Normals.Length);
    }

    [Theory]
    [InlineData(TileType.Flat)]
    [InlineData(TileType.Ramp)]
    [InlineData(TileType.Gravel)]
    [InlineData(TileType.Fence)]
    [InlineData(TileType.RampEntry)]
    [InlineData(TileType.RampExit)]
    [InlineData(TileType.SlopeEdge)]
    [InlineData(TileType.SlopeCorner)]
    [InlineData(TileType.RampEntryCorner)]
    [InlineData(TileType.SolidRampEntry)]
    public void AllTiles_HavePositiveTriangleCount(TileType type)
    {
        var tile = TileGeometry.GenerateTile(type);
        Assert.True(tile.Indices.Length >= 6, $"{type} should have at least 2 triangles");
    }

    // --- Helpers ---

    private static void AssertCenteredX(TileMeshData tile)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        for (int i = 0; i < tile.Vertices.Length; i += 3)
        {
            if (tile.Vertices[i] < minX) minX = tile.Vertices[i];
            if (tile.Vertices[i] > maxX) maxX = tile.Vertices[i];
        }
        Assert.Equal(-TileGeometry.CellWidth / 2f, minX);
        Assert.Equal(TileGeometry.CellWidth / 2f, maxX);
    }

    private static void AssertCenteredZ(TileMeshData tile)
    {
        float minZ = float.MaxValue, maxZ = float.MinValue;
        for (int i = 2; i < tile.Vertices.Length; i += 3)
        {
            if (tile.Vertices[i] < minZ) minZ = tile.Vertices[i];
            if (tile.Vertices[i] > maxZ) maxZ = tile.Vertices[i];
        }
        Assert.Equal(-TileGeometry.CellDepth / 2f, minZ);
        Assert.Equal(TileGeometry.CellDepth / 2f, maxZ);
    }

    private static void AssertIndicesInRange(TileMeshData tile)
    {
        int vertexCount = tile.Vertices.Length / 3;
        foreach (int idx in tile.Indices)
            Assert.InRange(idx, 0, vertexCount - 1);
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
