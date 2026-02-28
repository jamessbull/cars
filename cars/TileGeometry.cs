using System;
using System.Collections.Generic;

public enum TileType
{
    Flat,
    Ramp,
    Gravel,
    Fence,
    RampEntry,
    RampExit,
    SlopeEdge,
    SlopeCorner,
    RampEntryCorner,
    SolidRampEntry
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
    public const float CellDepth = 2f;

    /// <summary>
    /// Exit angle chosen so that ramp/arc rise ratio is exactly 7/3,
    /// giving perfect tessellation. cos(θ) = 3/4, θ ≈ 41.4°.
    /// </summary>
    public static readonly float ExitAngleRad = MathF.Acos(0.75f);
    public static readonly float ExitAngleDeg = ExitAngleRad * 180f / MathF.PI;

    /// <summary>Radius of the circular arc used for RampEntry/RampExit tiles.</summary>
    public static readonly float ArcRadius = CellDepth / MathF.Sin(ExitAngleRad);

    /// <summary>Total rise of one circular arc tile = R(1 - cos(θ)) = D × tan(θ/2).</summary>
    public static readonly float ArcRise = ArcRadius * (1f - MathF.Cos(ExitAngleRad));

    /// <summary>Number of grid cells (CellHeights) one arc tile spans vertically.</summary>
    public const int ArcGridSpan = 3;

    /// <summary>Number of grid cells (CellHeights) one ramp tile spans vertically.</summary>
    public const int RampGridSpan = 7;

    /// <summary>
    /// Grid cell height = ArcRise / ArcGridSpan.
    /// Both arc and ramp tiles rise exact integer multiples of this.
    /// </summary>
    public static readonly float CellHeight = ArcRise / ArcGridSpan;

    /// <summary>
    /// Rise of the straight ramp tile = CellDepth × tan(ExitAngle) = RampGridSpan × CellHeight.
    /// </summary>
    public static readonly float RampRise = CellDepth * MathF.Tan(ExitAngleRad);

    public const float TileThickness = 0.5f;
    public const float FenceHeight = 1.5f;
    public const float FenceThickness = 0.3f;

    public const int CurveSegments = 8;

    public static TileMeshData GenerateTile(TileType type)
    {
        return type switch
        {
            TileType.Flat => GenerateFlat(),
            TileType.Ramp => GenerateRamp(),
            TileType.Gravel => GenerateGravel(),
            TileType.Fence => GenerateFence(),
            TileType.RampEntry => GenerateRampEntry(),
            TileType.RampExit => GenerateRampExit(),
            TileType.SlopeEdge => GenerateSlopeEdge(),
            TileType.SlopeCorner => GenerateSlopeCorner(),
            TileType.RampEntryCorner => GenerateRampEntryCorner(),
            TileType.SolidRampEntry => GenerateSolidRampEntry(),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    // --- RampEntry circular arc profile ---
    // Circle of radius R centered at (0, R). Arc sweeps from 0° to ExitAngle.
    // y(z) = R - sqrt(R² - z²)
    // Starts flat (tangent horizontal), exits at ExitAngleDeg.
    // Rises ArcRise (= 3 CellHeights) over CellDepth.
    public static float RampEntryY(float z)
    {
        float R = ArcRadius;
        return R - MathF.Sqrt(R * R - z * z);
    }

    public static float RampEntrySlope(float z)
    {
        float R = ArcRadius;
        return z / MathF.Sqrt(R * R - z * z);
    }

    /// <summary>
    /// Height of the RampEntryCorner tile surface.
    /// Three corners at Y=0, one corner (+hw, CellDepth) raised to ArcRise.
    /// h(x, zParam) = RampEntryY(x + hw) * RampEntryY(zParam) / ArcRise
    /// where x ∈ [-hw, +hw], zParam ∈ [0, CellDepth] (0=back, CellDepth=front).
    /// </summary>
    public static float RampEntryCornerY(float x, float zParam)
    {
        float hw = CellWidth / 2f;
        return RampEntryY(x + hw) * RampEntryY(zParam) / ArcRise;
    }

    // --- RampExit circular arc profile (mirror of RampEntry) ---
    // Starts at ExitAngleDeg, curves to flat.
    // y(z) = ArcRise - R + sqrt(R² - (D-z)²)
    // Rises ArcRise (= 3 CellHeights) over CellDepth.
    public static float RampExitY(float z)
    {
        float R = ArcRadius;
        float D = CellDepth;
        float u = D - z;
        return ArcRise - R + MathF.Sqrt(R * R - u * u);
    }

    public static float RampExitSlope(float z)
    {
        float R = ArcRadius;
        float D = CellDepth;
        float u = D - z;
        return u / MathF.Sqrt(R * R - u * u);
    }

    private static TileMeshData GenerateFlat()
    {
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float t = TileThickness;

        var verts = new List<float>();
        var norms = new List<float>();
        var inds = new List<int>();
        int vi = 0;

        // Top face
        AddVertex(verts, norms, -hw, 0, hd, 0, 1, 0);
        AddVertex(verts, norms, hw, 0, hd, 0, 1, 0);
        AddVertex(verts, norms, hw, 0, -hd, 0, 1, 0);
        AddVertex(verts, norms, -hw, 0, -hd, 0, 1, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Bottom face
        AddVertex(verts, norms, -hw, -t, hd, 0, -1, 0);
        AddVertex(verts, norms, -hw, -t, -hd, 0, -1, 0);
        AddVertex(verts, norms, hw, -t, -hd, 0, -1, 0);
        AddVertex(verts, norms, hw, -t, hd, 0, -1, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Side walls — vertex order must produce CCW winding when viewed from normal direction
        vi = AddSideWall(verts, norms, inds, vi, -hw, 0, -hw, -t, -hd, hd, -1, 0, 0); // left
        vi = AddSideWall(verts, norms, inds, vi, hw, 0, hw, -t, hd, -hd, 1, 0, 0);    // right
        vi = AddSideWall(verts, norms, inds, vi, -hw, 0, hw, -t, hd, hd, 0, 0, 1);    // back (+Z)
        vi = AddSideWall(verts, norms, inds, vi, hw, 0, -hw, -t, -hd, -hd, 0, 0, -1); // front (-Z)

        return new TileMeshData { Vertices = verts.ToArray(), Normals = norms.ToArray(), Indices = inds.ToArray() };
    }

    // Adds a rectangular side wall quad (2 triangles)
    // x0,yTop,x1,yBot define the X range; z0,z1 define Z range
    private static int AddSideWall(List<float> verts, List<float> norms, List<int> inds,
        int vi, float x0, float yTop, float x1, float yBot, float z0, float z1,
        float nx, float ny, float nz)
    {
        AddVertex(verts, norms, x0, yBot, z0, nx, ny, nz);
        AddVertex(verts, norms, x1, yBot, z1, nx, ny, nz);
        AddVertex(verts, norms, x1, yTop, z1, nx, ny, nz);
        AddVertex(verts, norms, x0, yTop, z0, nx, ny, nz);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        return vi + 4;
    }

    private static TileMeshData GenerateRamp()
    {
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float h = RampRise;
        float t = TileThickness;

        float d = CellDepth;
        float slopeLen = MathF.Sqrt(d * d + h * h);
        float ny = d / slopeLen;
        float nz = h / slopeLen;

        var verts = new List<float>();
        var norms = new List<float>();
        var inds = new List<int>();
        int vi = 0;

        // Top surface (ramp face) — normal points up-slope
        AddVertex(verts, norms, -hw, 0, hd, 0, ny, nz);
        AddVertex(verts, norms, hw, 0, hd, 0, ny, nz);
        AddVertex(verts, norms, hw, h, -hd, 0, ny, nz);
        AddVertex(verts, norms, -hw, h, -hd, 0, ny, nz);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Bottom surface — parallel to top, offset down by thickness
        AddVertex(verts, norms, -hw, -t, hd, 0, -ny, -nz);
        AddVertex(verts, norms, -hw, h - t, -hd, 0, -ny, -nz);
        AddVertex(verts, norms, hw, h - t, -hd, 0, -ny, -nz);
        AddVertex(verts, norms, hw, -t, hd, 0, -ny, -nz);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Left edge wall (X=-hw) — thin parallelogram spanning thickness
        AddVertex(verts, norms, -hw, h - t, -hd, -1, 0, 0);
        AddVertex(verts, norms, -hw, -t, hd, -1, 0, 0);
        AddVertex(verts, norms, -hw, 0, hd, -1, 0, 0);
        AddVertex(verts, norms, -hw, h, -hd, -1, 0, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Right edge wall (X=+hw) — thin parallelogram spanning thickness
        AddVertex(verts, norms, hw, 0, hd, 1, 0, 0);
        AddVertex(verts, norms, hw, -t, hd, 1, 0, 0);
        AddVertex(verts, norms, hw, h - t, -hd, 1, 0, 0);
        AddVertex(verts, norms, hw, h, -hd, 1, 0, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Back edge wall (Z=+hd) — thin rect, height = thickness
        AddVertex(verts, norms, -hw, -t, hd, 0, 0, 1);
        AddVertex(verts, norms, hw, -t, hd, 0, 0, 1);
        AddVertex(verts, norms, hw, 0, hd, 0, 0, 1);
        AddVertex(verts, norms, -hw, 0, hd, 0, 0, 1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Front edge wall (Z=-hd) — thin rect, height = thickness
        AddVertex(verts, norms, hw, h - t, -hd, 0, 0, -1);
        AddVertex(verts, norms, -hw, h - t, -hd, 0, 0, -1);
        AddVertex(verts, norms, -hw, h, -hd, 0, 0, -1);
        AddVertex(verts, norms, hw, h, -hd, 0, 0, -1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);

        return new TileMeshData { Vertices = verts.ToArray(), Normals = norms.ToArray(), Indices = inds.ToArray() };
    }

    private static TileMeshData GenerateGravel()
    {
        return GenerateFlat();
    }

    private static TileMeshData GenerateFence()
    {
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float t = TileThickness;
        float fh = FenceHeight;
        float wt = FenceThickness;
        float wz0 = -hd - wt / 2f;
        float wz1 = -hd + wt / 2f;

        var verts = new List<float>();
        var norms = new List<float>();
        var inds = new List<int>();
        int vi = 0;

        // Ground top face
        AddVertex(verts, norms, -hw, 0, hd, 0, 1, 0);
        AddVertex(verts, norms, hw, 0, hd, 0, 1, 0);
        AddVertex(verts, norms, hw, 0, -hd, 0, 1, 0);
        AddVertex(verts, norms, -hw, 0, -hd, 0, 1, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Ground bottom face
        AddVertex(verts, norms, -hw, -t, hd, 0, -1, 0);
        AddVertex(verts, norms, -hw, -t, -hd, 0, -1, 0);
        AddVertex(verts, norms, hw, -t, -hd, 0, -1, 0);
        AddVertex(verts, norms, hw, -t, hd, 0, -1, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Wall front face (+Z side, facing inward)
        AddVertex(verts, norms, -hw, 0, wz1, 0, 0, 1);
        AddVertex(verts, norms, hw, 0, wz1, 0, 0, 1);
        AddVertex(verts, norms, hw, fh, wz1, 0, 0, 1);
        AddVertex(verts, norms, -hw, fh, wz1, 0, 0, 1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Wall back face (-Z side, facing outward)
        AddVertex(verts, norms, hw, 0, wz0, 0, 0, -1);
        AddVertex(verts, norms, -hw, 0, wz0, 0, 0, -1);
        AddVertex(verts, norms, -hw, fh, wz0, 0, 0, -1);
        AddVertex(verts, norms, hw, fh, wz0, 0, 0, -1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Wall top face
        AddVertex(verts, norms, -hw, fh, wz0, 0, 1, 0);
        AddVertex(verts, norms, -hw, fh, wz1, 0, 1, 0);
        AddVertex(verts, norms, hw, fh, wz1, 0, 1, 0);
        AddVertex(verts, norms, hw, fh, wz0, 0, 1, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Wall left face
        AddVertex(verts, norms, -hw, 0, wz0, -1, 0, 0);
        AddVertex(verts, norms, -hw, 0, wz1, -1, 0, 0);
        AddVertex(verts, norms, -hw, fh, wz1, -1, 0, 0);
        AddVertex(verts, norms, -hw, fh, wz0, -1, 0, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Wall right face
        AddVertex(verts, norms, hw, 0, wz1, 1, 0, 0);
        AddVertex(verts, norms, hw, 0, wz0, 1, 0, 0);
        AddVertex(verts, norms, hw, fh, wz0, 1, 0, 0);
        AddVertex(verts, norms, hw, fh, wz1, 1, 0, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);

        return new TileMeshData { Vertices = verts.ToArray(), Normals = norms.ToArray(), Indices = inds.ToArray() };
    }

    private static TileMeshData GenerateCurvedTile(Func<float, float> profileY, Func<float, float> profileSlope)
    {
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float t = TileThickness;
        int segs = CurveSegments;
        float dz = CellDepth / segs;

        var verts = new List<float>();
        var norms = new List<float>();
        var inds = new List<int>();
        int vi = 0;

        // Build height array (z goes from 0 to CellDepth, mapped to world Z from +hd to -hd)
        var heights = new float[segs + 1];
        var slopes = new float[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float z = i * dz;
            heights[i] = profileY(z);
            slopes[i] = profileSlope(z);
        }

        // Top surface — segs strips, each with 2 triangles
        for (int i = 0; i < segs; i++)
        {
            float z0 = hd - i * dz;       // world Z (back to front)
            float z1 = hd - (i + 1) * dz;
            float y0 = heights[i];
            float y1 = heights[i + 1];

            // Normal from slope: if dy/dz_param = s, world normal points (0, 1, s) normalized
            // (since increasing z_param = decreasing world Z)
            float s0 = slopes[i];
            float len0 = MathF.Sqrt(1f + s0 * s0);
            float ny0 = 1f / len0, nz0 = s0 / len0;
            float s1 = slopes[i + 1];
            float len1 = MathF.Sqrt(1f + s1 * s1);
            float ny1 = 1f / len1, nz1 = s1 / len1;

            AddVertex(verts, norms, -hw, y0, z0, 0, ny0, nz0);
            AddVertex(verts, norms, hw, y0, z0, 0, ny0, nz0);
            AddVertex(verts, norms, hw, y1, z1, 0, ny1, nz1);
            AddVertex(verts, norms, -hw, y1, z1, 0, ny1, nz1);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Bottom surface — same curve offset down by thickness
        for (int i = 0; i < segs; i++)
        {
            float z0 = hd - i * dz;
            float z1 = hd - (i + 1) * dz;
            float y0 = heights[i] - t;
            float y1 = heights[i + 1] - t;

            float s0 = slopes[i];
            float len0 = MathF.Sqrt(1f + s0 * s0);
            float ny0 = -1f / len0, nz0 = -s0 / len0;
            float s1 = slopes[i + 1];
            float len1 = MathF.Sqrt(1f + s1 * s1);
            float ny1 = -1f / len1, nz1 = -s1 / len1;

            AddVertex(verts, norms, -hw, y0, z0, 0, ny0, nz0);
            AddVertex(verts, norms, -hw, y1, z1, 0, ny1, nz1);
            AddVertex(verts, norms, hw, y1, z1, 0, ny1, nz1);
            AddVertex(verts, norms, hw, y0, z0, 0, ny0, nz0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Left wall (X=-hw) — CCW when viewed from -X
        for (int i = 0; i < segs; i++)
        {
            float z0 = hd - i * dz;
            float z1 = hd - (i + 1) * dz;
            float yTop0 = heights[i], yBot0 = heights[i] - t;
            float yTop1 = heights[i + 1], yBot1 = heights[i + 1] - t;

            AddVertex(verts, norms, -hw, yBot1, z1, -1, 0, 0);
            AddVertex(verts, norms, -hw, yBot0, z0, -1, 0, 0);
            AddVertex(verts, norms, -hw, yTop0, z0, -1, 0, 0);
            AddVertex(verts, norms, -hw, yTop1, z1, -1, 0, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Right wall (X=+hw) — CCW when viewed from +X
        for (int i = 0; i < segs; i++)
        {
            float z0 = hd - i * dz;
            float z1 = hd - (i + 1) * dz;
            float yTop0 = heights[i], yBot0 = heights[i] - t;
            float yTop1 = heights[i + 1], yBot1 = heights[i + 1] - t;

            AddVertex(verts, norms, hw, yBot0, z0, 1, 0, 0);
            AddVertex(verts, norms, hw, yBot1, z1, 1, 0, 0);
            AddVertex(verts, norms, hw, yTop1, z1, 1, 0, 0);
            AddVertex(verts, norms, hw, yTop0, z0, 1, 0, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Back wall (Z=+hd, first edge)
        float backY = heights[0];
        AddVertex(verts, norms, -hw, backY - t, hd, 0, 0, 1);
        AddVertex(verts, norms, hw, backY - t, hd, 0, 0, 1);
        AddVertex(verts, norms, hw, backY, hd, 0, 0, 1);
        AddVertex(verts, norms, -hw, backY, hd, 0, 0, 1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Front wall (Z=-hd, last edge)
        float frontY = heights[segs];
        AddVertex(verts, norms, hw, frontY - t, -hd, 0, 0, -1);
        AddVertex(verts, norms, -hw, frontY - t, -hd, 0, 0, -1);
        AddVertex(verts, norms, -hw, frontY, -hd, 0, 0, -1);
        AddVertex(verts, norms, hw, frontY, -hd, 0, 0, -1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);

        return new TileMeshData { Vertices = verts.ToArray(), Normals = norms.ToArray(), Indices = inds.ToArray() };
    }

    private static TileMeshData GenerateRampEntry()
    {
        return GenerateCurvedTile(RampEntryY, RampEntrySlope);
    }

    private static TileMeshData GenerateRampExit()
    {
        return GenerateCurvedTile(RampExitY, RampExitSlope);
    }

    private static TileMeshData GenerateSlopeEdge()
    {
        // One edge (X=-hw) slopes from Y=0 to Y=CellHeight along Z.
        // Opposite edge (X=+hw) stays flat at Y=0.
        // Bilinear interpolation between them. Subdivided along Z.
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float t = TileThickness;
        float h = CellHeight;
        int segs = CurveSegments;
        float dz = CellDepth / segs;

        var verts = new List<float>();
        var norms = new List<float>();
        var inds = new List<int>();
        int vi = 0;

        // Top surface — grid of quads
        // Height at any point: y(x, z_param) = h * (z_param/D) * ((hw - x) / CellWidth)
        // where z_param goes 0..D (back to front), x goes -hw..+hw
        for (int i = 0; i < segs; i++)
        {
            float zp0 = i * dz;
            float zp1 = (i + 1) * dz;
            float wz0 = hd - zp0;
            float wz1 = hd - zp1;

            float yLeftBack = h * (zp0 / CellDepth);
            float yLeftFront = h * (zp1 / CellDepth);
            float yRightBack = 0f;
            float yRightFront = 0f;

            // Approximate normal from cross product of surface edges
            float slopeZ = h / CellDepth; // slope along Z at left edge
            // Normal is roughly (slopeZ * hw/CellWidth, 1, slopeZ * something) — use face normal
            float e1x = CellWidth, e1y = yRightBack - yLeftBack, e1z = 0;
            float e2x = 0, e2y = yLeftFront - yLeftBack, e2z = wz1 - wz0;
            float nx = e1y * e2z - e1z * e2y;
            float ny = e1z * e2x - e1x * e2z;
            float nz = e1x * e2y - e1y * e2x;
            float nl = MathF.Sqrt(nx * nx + ny * ny + nz * nz);
            if (nl > 0) { nx /= nl; ny /= nl; nz /= nl; }

            AddVertex(verts, norms, -hw, yLeftBack, wz0, nx, ny, nz);
            AddVertex(verts, norms, hw, yRightBack, wz0, nx, ny, nz);
            AddVertex(verts, norms, hw, yRightFront, wz1, nx, ny, nz);
            AddVertex(verts, norms, -hw, yLeftFront, wz1, nx, ny, nz);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Bottom surface
        for (int i = 0; i < segs; i++)
        {
            float zp0 = i * dz;
            float zp1 = (i + 1) * dz;
            float wz0 = hd - zp0;
            float wz1 = hd - zp1;

            float yLeftBack = h * (zp0 / CellDepth) - t;
            float yLeftFront = h * (zp1 / CellDepth) - t;
            float yRightBack = -t;
            float yRightFront = -t;

            AddVertex(verts, norms, -hw, yLeftBack, wz0, 0, -1, 0);
            AddVertex(verts, norms, -hw, yLeftFront, wz1, 0, -1, 0);
            AddVertex(verts, norms, hw, yRightFront, wz1, 0, -1, 0);
            AddVertex(verts, norms, hw, yRightBack, wz0, 0, -1, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Left wall (X=-hw, slopes from 0 to h) — CCW from -X
        for (int i = 0; i < segs; i++)
        {
            float wz0 = hd - i * dz;
            float wz1 = hd - (i + 1) * dz;
            float yT0 = h * (i * dz / CellDepth);
            float yT1 = h * ((i + 1) * dz / CellDepth);

            AddVertex(verts, norms, -hw, yT1 - t, wz1, -1, 0, 0);
            AddVertex(verts, norms, -hw, yT0 - t, wz0, -1, 0, 0);
            AddVertex(verts, norms, -hw, yT0, wz0, -1, 0, 0);
            AddVertex(verts, norms, -hw, yT1, wz1, -1, 0, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Right wall (X=+hw, flat at Y=0) — CCW from +X
        AddVertex(verts, norms, hw, -t, hd, 1, 0, 0);
        AddVertex(verts, norms, hw, -t, -hd, 1, 0, 0);
        AddVertex(verts, norms, hw, 0, -hd, 1, 0, 0);
        AddVertex(verts, norms, hw, 0, hd, 1, 0, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Back wall (Z=+hd, both sides at Y=0)
        AddVertex(verts, norms, -hw, -t, hd, 0, 0, 1);
        AddVertex(verts, norms, hw, -t, hd, 0, 0, 1);
        AddVertex(verts, norms, hw, 0, hd, 0, 0, 1);
        AddVertex(verts, norms, -hw, 0, hd, 0, 0, 1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Front wall (Z=-hd, left at h, right at 0)
        AddVertex(verts, norms, hw, -t, -hd, 0, 0, -1);
        AddVertex(verts, norms, -hw, h - t, -hd, 0, 0, -1);
        AddVertex(verts, norms, -hw, h, -hd, 0, 0, -1);
        AddVertex(verts, norms, hw, 0, -hd, 0, 0, -1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);

        return new TileMeshData { Vertices = verts.ToArray(), Normals = norms.ToArray(), Indices = inds.ToArray() };
    }

    private static TileMeshData GenerateSlopeCorner()
    {
        // One corner (-hw, -hd) raised to CellHeight. Other 3 corners at Y=0.
        // Height falls off quadratically from the raised corner.
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float t = TileThickness;
        float h = CellHeight;
        int gridN = 4; // 4x4 grid
        float dx = CellWidth / gridN;
        float dz = CellDepth / gridN;

        var verts = new List<float>();
        var norms = new List<float>();
        var inds = new List<int>();
        int vi = 0;

        // Height function: raised corner is at (-hw, -hd) in world = (0, D) in param space
        // height = H * max(0, 1 - dist/maxDist)^2 where dist from raised corner
        float maxDist = MathF.Sqrt(CellWidth * CellWidth + CellDepth * CellDepth);
        Func<float, float, float> heightAt = (wx, wz) =>
        {
            float dx2 = wx - (-hw);
            float dz2 = wz - (-hd);
            float dist = MathF.Sqrt(dx2 * dx2 + dz2 * dz2);
            float ratio = Math.Clamp(1f - dist / maxDist, 0f, 1f);
            return h * ratio * ratio;
        };

        // Build height grid
        var grid = new float[gridN + 1, gridN + 1];
        for (int ix = 0; ix <= gridN; ix++)
            for (int iz = 0; iz <= gridN; iz++)
            {
                float wx = -hw + ix * dx;
                float wz = hd - iz * dz; // Z goes from +hd (back) to -hd (front)
                grid[ix, iz] = heightAt(wx, wz);
            }

        // Top surface
        for (int ix = 0; ix < gridN; ix++)
        for (int iz = 0; iz < gridN; iz++)
        {
            float x0 = -hw + ix * dx, x1 = x0 + dx;
            float z0 = hd - iz * dz, z1 = z0 - dz;
            float y00 = grid[ix, iz], y10 = grid[ix + 1, iz];
            float y01 = grid[ix, iz + 1], y11 = grid[ix + 1, iz + 1];

            // Face normal from cross product
            float e1x2 = dx, e1y = y10 - y00;
            float e2y = y01 - y00, e2z = -dz;
            float nx = e1y * e2z; float ny2 = -e1x2 * e2z; float nz = e1x2 * e2y;
            float nl = MathF.Sqrt(nx * nx + ny2 * ny2 + nz * nz);
            if (nl > 0) { nx /= nl; ny2 /= nl; nz /= nl; }

            AddVertex(verts, norms, x0, y00, z0, nx, ny2, nz);
            AddVertex(verts, norms, x1, y10, z0, nx, ny2, nz);
            AddVertex(verts, norms, x1, y11, z1, nx, ny2, nz);
            AddVertex(verts, norms, x0, y01, z1, nx, ny2, nz);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Bottom surface (flat at -t for simplicity)
        for (int ix = 0; ix < gridN; ix++)
        for (int iz = 0; iz < gridN; iz++)
        {
            float x0 = -hw + ix * dx, x1 = x0 + dx;
            float z0 = hd - iz * dz, z1 = z0 - dz;
            float y00 = grid[ix, iz] - t, y10 = grid[ix + 1, iz] - t;
            float y01 = grid[ix, iz + 1] - t, y11 = grid[ix + 1, iz + 1] - t;

            AddVertex(verts, norms, x0, y00, z0, 0, -1, 0);
            AddVertex(verts, norms, x0, y01, z1, 0, -1, 0);
            AddVertex(verts, norms, x1, y11, z1, 0, -1, 0);
            AddVertex(verts, norms, x1, y10, z0, 0, -1, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Edge walls — 4 edges, each with gridN segments
        // Back edge (Z=+hd, iz=0)
        for (int ix = 0; ix < gridN; ix++)
        {
            float x0 = -hw + ix * dx, x1 = x0 + dx;
            float y0 = grid[ix, 0], y1 = grid[ix + 1, 0];
            AddVertex(verts, norms, x0, y0 - t, hd, 0, 0, 1);
            AddVertex(verts, norms, x1, y1 - t, hd, 0, 0, 1);
            AddVertex(verts, norms, x1, y1, hd, 0, 0, 1);
            AddVertex(verts, norms, x0, y0, hd, 0, 0, 1);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Front edge (Z=-hd, iz=gridN)
        for (int ix = 0; ix < gridN; ix++)
        {
            float x0 = -hw + ix * dx, x1 = x0 + dx;
            float y0 = grid[ix, gridN], y1 = grid[ix + 1, gridN];
            AddVertex(verts, norms, x1, y1 - t, -hd, 0, 0, -1);
            AddVertex(verts, norms, x0, y0 - t, -hd, 0, 0, -1);
            AddVertex(verts, norms, x0, y0, -hd, 0, 0, -1);
            AddVertex(verts, norms, x1, y1, -hd, 0, 0, -1);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Left edge (X=-hw, ix=0) — CCW from -X
        for (int iz = 0; iz < gridN; iz++)
        {
            float z0 = hd - iz * dz, z1 = z0 - dz;
            float y0 = grid[0, iz], y1 = grid[0, iz + 1];
            AddVertex(verts, norms, -hw, y1 - t, z1, -1, 0, 0);
            AddVertex(verts, norms, -hw, y0 - t, z0, -1, 0, 0);
            AddVertex(verts, norms, -hw, y0, z0, -1, 0, 0);
            AddVertex(verts, norms, -hw, y1, z1, -1, 0, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Right edge (X=+hw, ix=gridN) — CCW from +X
        for (int iz = 0; iz < gridN; iz++)
        {
            float z0 = hd - iz * dz, z1 = z0 - dz;
            float y0 = grid[gridN, iz], y1 = grid[gridN, iz + 1];
            AddVertex(verts, norms, hw, y0 - t, z0, 1, 0, 0);
            AddVertex(verts, norms, hw, y1 - t, z1, 1, 0, 0);
            AddVertex(verts, norms, hw, y1, z1, 1, 0, 0);
            AddVertex(verts, norms, hw, y0, z0, 1, 0, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        return new TileMeshData { Vertices = verts.ToArray(), Normals = norms.ToArray(), Indices = inds.ToArray() };
    }

    private static TileMeshData GenerateSolidRampEntry()
    {
        // Same curved top surface as RampEntry, but solid:
        //   - Bottom face is flat at Y=0 (not offset by TileThickness)
        //   - Left/right walls fill the full height from Y=0 to the curved profile
        //   - Front wall spans Y=0 to Y=ArcRise
        //   - No back wall (arc starts at Y=0 = base level, so nothing to close)
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        int segs = CurveSegments;
        float dz = CellDepth / segs;

        var verts = new List<float>();
        var norms = new List<float>();
        var inds = new List<int>();
        int vi = 0;

        var heights = new float[segs + 1];
        var slopes = new float[segs + 1];
        for (int i = 0; i <= segs; i++)
        {
            float z = i * dz;
            heights[i] = RampEntryY(z);
            slopes[i] = RampEntrySlope(z);
        }

        // Top surface — identical to RampEntry
        for (int i = 0; i < segs; i++)
        {
            float wz0 = hd - i * dz, wz1 = hd - (i + 1) * dz;
            float y0 = heights[i], y1 = heights[i + 1];
            float s0 = slopes[i], s1 = slopes[i + 1];
            float len0 = MathF.Sqrt(1f + s0 * s0), len1 = MathF.Sqrt(1f + s1 * s1);
            float ny0 = 1f / len0, nz0 = s0 / len0;
            float ny1 = 1f / len1, nz1 = s1 / len1;

            AddVertex(verts, norms, -hw, y0, wz0, 0, ny0, nz0);
            AddVertex(verts, norms,  hw, y0, wz0, 0, ny0, nz0);
            AddVertex(verts, norms,  hw, y1, wz1, 0, ny1, nz1);
            AddVertex(verts, norms, -hw, y1, wz1, 0, ny1, nz1);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Bottom face — flat at Y=0, normal pointing down
        AddVertex(verts, norms, -hw, 0,  hd, 0, -1, 0);
        AddVertex(verts, norms, -hw, 0, -hd, 0, -1, 0);
        AddVertex(verts, norms,  hw, 0, -hd, 0, -1, 0);
        AddVertex(verts, norms,  hw, 0,  hd, 0, -1, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Left wall (X=-hw) — filled from Y=0 to curved height, CCW from -X.
        // At i=0 the back height is 0 so the first triangle is degenerate; skip it.
        for (int i = 0; i < segs; i++)
        {
            float wz0 = hd - i * dz, wz1 = wz0 - dz;
            float h0 = heights[i], h1 = heights[i + 1];

            AddVertex(verts, norms, -hw, 0,  wz0, -1, 0, 0); // bottom-back
            AddVertex(verts, norms, -hw, h0, wz0, -1, 0, 0); // top-back
            AddVertex(verts, norms, -hw, h1, wz1, -1, 0, 0); // top-front
            AddVertex(verts, norms, -hw, 0,  wz1, -1, 0, 0); // bottom-front
            if (h0 > 0) AddTriangle(inds, vi, 0, 1, 2);      // skip when top-back == bottom-back
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Right wall (X=+hw) — filled from Y=0 to curved height, CCW from +X.
        for (int i = 0; i < segs; i++)
        {
            float wz0 = hd - i * dz, wz1 = wz0 - dz;
            float h0 = heights[i], h1 = heights[i + 1];

            AddVertex(verts, norms,  hw, 0,  wz1, 1, 0, 0); // bottom-front
            AddVertex(verts, norms,  hw, h1, wz1, 1, 0, 0); // top-front
            AddVertex(verts, norms,  hw, h0, wz0, 1, 0, 0); // top-back
            AddVertex(verts, norms,  hw, 0,  wz0, 1, 0, 0); // bottom-back
            AddTriangle(inds, vi, 0, 1, 2);
            if (h0 > 0) AddTriangle(inds, vi, 0, 2, 3);     // skip when top-back == bottom-back
            vi += 4;
        }

        // Front wall (Z=-hd) — full height Y=0 to ArcRise, CCW from -Z
        float frontY = heights[segs];
        AddVertex(verts, norms,  hw, 0,      -hd, 0, 0, -1);
        AddVertex(verts, norms, -hw, 0,      -hd, 0, 0, -1);
        AddVertex(verts, norms, -hw, frontY, -hd, 0, 0, -1);
        AddVertex(verts, norms,  hw, frontY, -hd, 0, 0, -1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);

        return new TileMeshData { Vertices = verts.ToArray(), Normals = norms.ToArray(), Indices = inds.ToArray() };
    }

    private static TileMeshData GenerateRampEntryCorner()
    {
        // Doubly-curved corner: three corners at Y=0, one corner (+hw, front) raised to ArcRise.
        // h(x, zParam) = RampEntryY(x + hw) * RampEntryY(zParam) / ArcRise
        // Normal: n_raw = (-∂h/∂x, 1, ∂h/∂zParam), then normalized.
        float hw = CellWidth / 2f;
        float hd = CellDepth / 2f;
        float t = TileThickness;
        int segs = CurveSegments;
        float dx = CellWidth / segs;
        float dz = CellDepth / segs;

        var verts = new List<float>();
        var norms = new List<float>();
        var inds = new List<int>();
        int vi = 0;

        // Precompute (segs+1)×(segs+1) height and normal grids.
        // ix=0 → x=-hw, ix=segs → x=+hw; iz=0 → zParam=0 (back), iz=segs → zParam=CellDepth (front).
        var gy = new float[segs + 1, segs + 1];
        var gnx = new float[segs + 1, segs + 1];
        var gny = new float[segs + 1, segs + 1];
        var gnz = new float[segs + 1, segs + 1];

        for (int ix = 0; ix <= segs; ix++)
        for (int iz = 0; iz <= segs; iz++)
        {
            float x = -hw + ix * dx;
            float zp = iz * dz;
            float xArg = x + hw; // ∈ [0, CellWidth]
            gy[ix, iz] = RampEntryY(xArg) * RampEntryY(zp) / ArcRise;
            float dhdx = RampEntrySlope(xArg) * RampEntryY(zp) / ArcRise;
            float dhdzp = RampEntryY(xArg) * RampEntrySlope(zp) / ArcRise;
            float nx = -dhdx, ny = 1f, nz = dhdzp;
            float len = MathF.Sqrt(nx * nx + ny * ny + nz * nz);
            gnx[ix, iz] = nx / len;
            gny[ix, iz] = ny / len;
            gnz[ix, iz] = nz / len;
        }

        // Top surface: segs×segs quads, CCW from +Y.
        for (int ix = 0; ix < segs; ix++)
        for (int iz = 0; iz < segs; iz++)
        {
            float x0 = -hw + ix * dx, x1 = x0 + dx;
            float wz0 = hd - iz * dz, wz1 = wz0 - dz;
            AddVertex(verts, norms, x0, gy[ix, iz],     wz0, gnx[ix, iz],     gny[ix, iz],     gnz[ix, iz]);
            AddVertex(verts, norms, x1, gy[ix+1, iz],   wz0, gnx[ix+1, iz],   gny[ix+1, iz],   gnz[ix+1, iz]);
            AddVertex(verts, norms, x1, gy[ix+1, iz+1], wz1, gnx[ix+1, iz+1], gny[ix+1, iz+1], gnz[ix+1, iz+1]);
            AddVertex(verts, norms, x0, gy[ix, iz+1],   wz1, gnx[ix, iz+1],   gny[ix, iz+1],   gnz[ix, iz+1]);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Bottom surface: same grid offset by -t, CCW from -Y.
        for (int ix = 0; ix < segs; ix++)
        for (int iz = 0; iz < segs; iz++)
        {
            float x0 = -hw + ix * dx, x1 = x0 + dx;
            float wz0 = hd - iz * dz, wz1 = wz0 - dz;
            AddVertex(verts, norms, x0, gy[ix, iz]     - t, wz0, 0, -1, 0);
            AddVertex(verts, norms, x0, gy[ix, iz+1]   - t, wz1, 0, -1, 0);
            AddVertex(verts, norms, x1, gy[ix+1, iz+1] - t, wz1, 0, -1, 0);
            AddVertex(verts, norms, x1, gy[ix+1, iz]   - t, wz0, 0, -1, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Back wall (Z=+hd, iz=0): all heights are 0 (RampEntryY(0)=0), flat rect, normal (0,0,+1).
        AddVertex(verts, norms, -hw, -t, hd, 0, 0, 1);
        AddVertex(verts, norms, +hw, -t, hd, 0, 0, 1);
        AddVertex(verts, norms, +hw,  0, hd, 0, 0, 1);
        AddVertex(verts, norms, -hw,  0, hd, 0, 0, 1);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Left wall (X=-hw, ix=0): all heights are 0 (RampEntryY(0)=0), flat rect, normal (-1,0,0).
        AddVertex(verts, norms, -hw, -t, -hd, -1, 0, 0);
        AddVertex(verts, norms, -hw, -t, +hd, -1, 0, 0);
        AddVertex(verts, norms, -hw,  0, +hd, -1, 0, 0);
        AddVertex(verts, norms, -hw,  0, -hd, -1, 0, 0);
        AddTriangle(inds, vi, 0, 1, 2);
        AddTriangle(inds, vi, 0, 2, 3);
        vi += 4;

        // Front wall (Z=-hd, iz=segs): curved in X, normal (0,0,-1).
        for (int ix = 0; ix < segs; ix++)
        {
            float x0 = -hw + ix * dx, x1 = x0 + dx;
            float y0 = gy[ix, segs], y1 = gy[ix+1, segs];
            AddVertex(verts, norms, x1, y1 - t, -hd, 0, 0, -1);
            AddVertex(verts, norms, x0, y0 - t, -hd, 0, 0, -1);
            AddVertex(verts, norms, x0, y0,     -hd, 0, 0, -1);
            AddVertex(verts, norms, x1, y1,     -hd, 0, 0, -1);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        // Right wall (X=+hw, ix=segs): curved in Z, normal (+1,0,0).
        for (int iz = 0; iz < segs; iz++)
        {
            float wz0 = hd - iz * dz, wz1 = wz0 - dz;
            float y0 = gy[segs, iz], y1 = gy[segs, iz+1];
            AddVertex(verts, norms, hw, y0 - t, wz0, 1, 0, 0);
            AddVertex(verts, norms, hw, y1 - t, wz1, 1, 0, 0);
            AddVertex(verts, norms, hw, y1,     wz1, 1, 0, 0);
            AddVertex(verts, norms, hw, y0,     wz0, 1, 0, 0);
            AddTriangle(inds, vi, 0, 1, 2);
            AddTriangle(inds, vi, 0, 2, 3);
            vi += 4;
        }

        return new TileMeshData { Vertices = verts.ToArray(), Normals = norms.ToArray(), Indices = inds.ToArray() };
    }

    private static void AddVertex(List<float> vertices, List<float> normals,
        float x, float y, float z, float nx, float ny, float nz)
    {
        vertices.Add(x); vertices.Add(y); vertices.Add(z);
        normals.Add(nx); normals.Add(ny); normals.Add(nz);
    }

    private static void AddTriangle(List<int> indices, int baseIndex, int a, int b, int c)
    {
        indices.Add(baseIndex + a);
        indices.Add(baseIndex + b);
        indices.Add(baseIndex + c);
    }
}
