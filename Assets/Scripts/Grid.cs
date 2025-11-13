using UnityEngine;

public class Grid
{
    // public for easy debugging / reads
    public int width, height;      // number of cells in X and Z
    public float cellSizeX, cellSizeZ;
    public Vector3 origin;         // world-space origin (bottom-left corner)

    public Grid(int width, int height, Vector3 origin, float sizeX, float sizeZ)
    {
        this.width = width;
        this.height = height;
        this.origin = origin;

        // per-cell size
        cellSizeX = sizeX / width;
        cellSizeZ = sizeZ / height;

        // draw the grid once
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), Color.white, 10000000f);
                Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), Color.white, 10000000f);
            }
        }
        Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), Color.white, 10000000f);
        Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), Color.white, 10000000f);
    }

    // convert cell index -> world position (corner at (x,z))
    public Vector3 GetWorldPosition(int x, int z)
    {
        // IMPORTANT: add origin, don't scale origin by cell size
        // world = origin + (x * cellSizeX, 0, z * cellSizeZ)
        return new Vector3(
            origin.x + x * cellSizeX,
            origin.y,
            origin.z + z * cellSizeZ
        );
    }

    // convert world position -> cell index
    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        // invert GetWorldPosition
        x = Mathf.FloorToInt((worldPosition.x - origin.x) / cellSizeX);
        z = Mathf.FloorToInt((worldPosition.z - origin.z) / cellSizeZ);
    }
}
