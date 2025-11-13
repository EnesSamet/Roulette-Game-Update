using UnityEngine;

public static class GridGeometry
{
    public static bool WorldToCell(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, Vector3 worldPos, out int x, out int z)
    {
        x = z = 0;
        Vector3 local = tr.InverseTransformPoint(worldPos);
        local.x -= offset.x;
        local.z -= offset.y;

        float cellX = size.x / Mathf.Max(1, cols);
        float cellZ = size.y / Mathf.Max(1, rows);

        x = Mathf.FloorToInt(local.x / cellX);
        z = Mathf.FloorToInt(local.z / cellZ);

        return x >= 0 && z >= 0 && x < cols && z < rows;
    }

    public static bool LocalUVInCell(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, int cx, int cz, Vector3 hit, out float u, out float v)
    {
        u = v = 0f;
        Vector3 local = tr.InverseTransformPoint(hit);
        local.x -= offset.x;
        local.z -= offset.y;

        float cellX = size.x / cols;
        float cellZ = size.y / rows;

        float xInCell = local.x - cx * cellX;
        float zInCell = local.z - cz * cellZ;

        if (xInCell < 0 || xInCell > cellX || zInCell < 0 || zInCell > cellZ) return false;

        u = xInCell / cellX; // 0..1 left->right
        v = zInCell / cellZ; // 0..1 bottom->top
        return true;
    }

    public static bool NearestCorner(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, Vector3 hit, out int ax, out int az)
    {
        ax = az = 0;
        Vector3 local = tr.InverseTransformPoint(hit);
        local.x -= offset.x;
        local.z -= offset.y;

        float cellX = size.x / cols;
        float cellZ = size.y / rows;

        float gx = local.x / cellX;
        float gz = local.z / cellZ;

        ax = Mathf.Clamp(Mathf.RoundToInt(gx), 0, cols);
        az = Mathf.Clamp(Mathf.RoundToInt(gz), 0, rows);
        return true;
    }

    public static Vector3 CellCenterWorld(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, float y, int x, int z)
    {
        float cellX = size.x / cols;
        float cellZ = size.y / rows;

        Vector3 local = new Vector3(
            offset.x + (x + 0.5f) * cellX,
            y,
            offset.y + (z + 0.5f) * cellZ
        );
        return tr.TransformPoint(local);
    }

    public static Vector3 CornerWorld(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, float y, int ax, int az)
    {
        float cellX = size.x / cols;
        float cellZ = size.y / rows;

        Vector3 local = new Vector3(
            offset.x + ax * cellX,
            y,
            offset.y + az * cellZ
        );
        return tr.TransformPoint(local);
    }

    public static Vector3 EdgeMidpointWorld(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, float y, int x1, int z1, int x2, int z2)
    {
        Vector3 c1 = CellCenterWorld(tr, size, offset, cols, rows, y, x1, z1);
        Vector3 c2 = CellCenterWorld(tr, size, offset, cols, rows, y, x2, z2);
        return (c1 + c2) * 0.5f;
    }

    // Street chip at bottom edge under where clicked (uses cx + u)
    public static Vector3 StreetEdgeWorld(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, float y, int rowZ, int cx, float u)
    {
        Vector3 leftBottom = CornerWorld(tr, size, offset, cols, rows, y, 0, rowZ);
        Vector3 rightBottom = CornerWorld(tr, size, offset, cols, rows, y, cols, rowZ);

        //float t = Mathf.Clamp01((cx + Mathf.Clamp01(u)) / Mathf.Max(1, cols));
        float t = (cx + 0.5f) / cols;
        return Vector3.Lerp(leftBottom, rightBottom, t);
    }

    // Six-line chip at bottom-left corner of the 2-row block
    public static Vector3 SixLineCornerWorld(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, float y, int startRow)
    {
        return CornerWorld(tr, size, offset, cols, rows, y, 0, startRow + 1);
    }
    // Draw grid lines in Gizmos (editor preview)
    public static void DrawGridGizmos(Transform tr, Vector2 size, Vector2 offset, int cols, int rows, float y, Color color)
    {
        if (cols <= 0 || rows <= 0) return;

        float cellX = size.x / cols;
        float cellZ = size.y / rows;

        Gizmos.color = color;

        // vertical lines
        for (int x = 0; x <= cols; x++)
        {
            Vector3 a = new Vector3(offset.x + x * cellX, y, offset.y + 0f);
            Vector3 b = new Vector3(offset.x + x * cellX, y, offset.y + rows * cellZ);
            Gizmos.DrawLine(tr.TransformPoint(a), tr.TransformPoint(b));
        }

        // horizontal lines
        for (int z = 0; z <= rows; z++)
        {
            Vector3 a = new Vector3(offset.x + 0f, y, offset.y + z * cellZ);
            Vector3 b = new Vector3(offset.x + cols * cellX, y, offset.y + z * cellZ);
            Gizmos.DrawLine(tr.TransformPoint(a), tr.TransformPoint(b));
        }
    }
}
