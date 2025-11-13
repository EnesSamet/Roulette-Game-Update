using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GridInput : MonoBehaviour
{
    [System.Serializable]
    public class GridArea
    {
        public int cols;
        public int rows;
        public Vector2 size;     // world size (x,z)
        public Vector2 offset;   // local start (x,z)
        public int[] preset;     // length = cols * rows
    }

    [Header("Grids")]
    public GridArea grid1;
    public GridArea grid2;
    public GridArea grid3;

    [Header("Refs")]
    public GridBets bets;       // talk to money/chips/history
    public GameManager manager; // for chipValue and yLocal
    public float yLocal = 0.5f; // plane height for ray

    [Header("Click Settings")]
    public float edgeThreshold = 0.22f;
    public float cornerThreshold = 0.18f;

    [SerializeField] BetHistory betHistory;

    void Update()
    {
        if (manager.canPlayBet)
        {
            bool left = Input.GetMouseButtonDown(0);
            bool right = Input.GetMouseButtonDown(1);
            if (!left && !right) return;

            // plane of the board
            Vector3 planePointWorld = transform.TransformPoint(new Vector3(0f, yLocal, 0f));
            Plane plane = new Plane(transform.up, planePointWorld);

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!plane.Raycast(ray, out float t)) return;

            Vector3 hit = ray.GetPoint(t);
            bool removeMode = right;

            // try grids in order: first one that contains the click wins
            if (HandleGridClick(grid1, hit, removeMode)) return;
            if (HandleGridClick(grid2, hit, removeMode)) return;
            HandleGridClick(grid3, hit, removeMode);
        }
    }

    bool HandleGridClick(GridArea g, Vector3 hit, bool removeMode)
    {
        if (g == null || g.cols <= 0 || g.rows <= 0 || g.preset == null || g.preset.Length != g.cols * g.rows)
            return false;

        // which cell?
        int cx, cz;
        if (!GridGeometry.WorldToCell(transform, g.size, g.offset, g.cols, g.rows, hit, out cx, out cz))
            return false;

        int code = g.preset[cz * g.cols + cx];

        // OUTSIDE BET
        if (code <= 0)
        {
            if (removeMode)
            {
                // remove by chip name (must click chip collider)
                string name = BetBuilder.OutsideName(code);
                if (!string.IsNullOrEmpty(name))
                    bets.TryRemoveBet(name, Input.mousePosition);
                return true;
            }
            else
            {
                Bet b = BetBuilder.Outside(code, manager.chipValue);
                Vector3 chipPos = GridGeometry.CellCenterWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, cx, cz);
                if (b != null) bets.PlaceBet(b, chipPos);
                return true;
            }
        }

        // INSIDE BETS
        float u, v;
        if (!GridGeometry.LocalUVInCell(transform, g.size, g.offset, g.cols, g.rows, cx, cz, hit, out u, out v))
            return false;

        bool holdShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool holdCtrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        // STREET (lane) with Shift (centered on bottom edge of row)
        if (holdShift)
        {
            var nums = RowNumbers(g.preset, g.cols, g.rows, cz);
            if (nums != null)
            {
                string label = $"Street {string.Join("-", nums)}";
                if (removeMode)
                {
                    bets.TryRemoveBet(label, Input.mousePosition);
                }
                else
                {
                    Bet b = BetBuilder.Street(nums, manager.chipValue);
                    Vector3 chipPos = GridGeometry.EdgeMidpointWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, -1, cz, 0, cz);
                    bets.PlaceBet(b, chipPos);
                }
                return true;
            }
        }

        // SIX-LINE with Ctrl (bottom-left corner of 2-row block)
        if (holdCtrl)
        {
            int startRow = cz;
            var nums = SixLineNumbers(g.preset, g.cols, g.rows, startRow);
            if (nums == null && cz > 0) { startRow = cz - 1; nums = SixLineNumbers(g.preset, g.cols, g.rows, startRow); }

            if (nums != null)
            {
                string label = $"Six Line {string.Join("-", nums)}";
                if (removeMode)
                {
                    bets.TryRemoveBet(label, Input.mousePosition);
                }
                else
                {
                    Bet b = BetBuilder.SixLine(nums, manager.chipValue);
                    Vector3 chipPos = GridGeometry.SixLineCornerWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, startRow);
                    bets.PlaceBet(b, chipPos);
                }
                return true;
            }
        }

        // CORNER (near any of 4 corners inside the cell)
        bool clickedCorner =
            (u <= cornerThreshold && v <= cornerThreshold) ||
            (u >= 1f - cornerThreshold && v <= cornerThreshold) ||
            (u <= cornerThreshold && v >= 1f - cornerThreshold) ||
            (u >= 1f - cornerThreshold && v >= 1f - cornerThreshold);

        if (clickedCorner)
        {
            int ax, az;
            GridGeometry.NearestCorner(transform, g.size, g.offset, g.cols, g.rows, hit, out ax, out az);
            var nums = CornerNumbers(g.preset, g.cols, g.rows, ax - 1, az - 1);
            if (nums != null)
            {
                string label = $"Corner {string.Join("-", nums)}";
                if (removeMode)
                {
                    bets.TryRemoveBet(label, Input.mousePosition);
                }
                else
                {
                    Bet b = BetBuilder.Corner(nums, manager.chipValue);
                    Vector3 chipPos = GridGeometry.CornerWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, ax, az);
                    bets.PlaceBet(b, chipPos);
                }
                return true;
            }
        }

        // SPLITS on edges, else STRAIGHT
        if (u <= edgeThreshold) // left
        {
            var pair = SplitPair(g.preset, g.cols, g.rows, cx, cz, -1, 0);
            if (pair.HasValue)
            {
                string label = BetBuilder.SplitName(pair.Value.a, pair.Value.b);
                if (removeMode)
                {
                    bets.TryRemoveBet(label, Input.mousePosition);
                }
                else
                {
                    Bet b = BetBuilder.Split(pair.Value.a, pair.Value.b, manager.chipValue);
                    Vector3 chipPos = GridGeometry.EdgeMidpointWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, cx, cz, cx - 1, cz);
                    bets.PlaceBet(b, chipPos);
                }
                return true;
            }
        }
        else if (u >= 1f - edgeThreshold) // right
        {
            var pair = SplitPair(g.preset, g.cols, g.rows, cx, cz, +1, 0);
            if (pair.HasValue)
            {
                string label = BetBuilder.SplitName(pair.Value.a, pair.Value.b);
                if (removeMode)
                {
                    bets.TryRemoveBet(label, Input.mousePosition);
                }
                else
                {
                    Bet b = BetBuilder.Split(pair.Value.a, pair.Value.b, manager.chipValue);
                    Vector3 chipPos = GridGeometry.EdgeMidpointWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, cx, cz, cx + 1, cz);
                    bets.PlaceBet(b, chipPos);
                }
                return true;
            }
        }
        else if (v <= edgeThreshold) // down
        {
            var pair = SplitPair(g.preset, g.cols, g.rows, cx, cz, 0, -1);
            if (pair.HasValue)
            {
                string label = BetBuilder.SplitName(pair.Value.a, pair.Value.b);
                if (removeMode)
                {
                    bets.TryRemoveBet(label, Input.mousePosition);
                }
                else
                {
                    Bet b = BetBuilder.Split(pair.Value.a, pair.Value.b, manager.chipValue);
                    Vector3 chipPos = GridGeometry.EdgeMidpointWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, cx, cz, cx, cz - 1);
                    bets.PlaceBet(b, chipPos);
                }
                return true;
            }
        }
        else if (v >= 1f - edgeThreshold) // up
        {
            var pair = SplitPair(g.preset, g.cols, g.rows, cx, cz, 0, +1);
            if (pair.HasValue)
            {
                string label = BetBuilder.SplitName(pair.Value.a, pair.Value.b);
                if (removeMode)
                {
                    bets.TryRemoveBet(label, Input.mousePosition);
                }
                else
                {
                    Bet b = BetBuilder.Split(pair.Value.a, pair.Value.b, manager.chipValue);
                    Vector3 chipPos = GridGeometry.EdgeMidpointWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, cx, cz, cx, cz + 1);
                    bets.PlaceBet(b, chipPos);
                }
                return true;
            }
        }

        // STRAIGHT (fallback)
        int n = CodeAt(g.preset, g.cols, cx, cz);
        if (n >= 0 && n <= 36)
        {
            string label = BetBuilder.StraightName(n);
            if (removeMode)
            {
                bets.TryRemoveBet(label, Input.mousePosition);
            }
            else
            {
                Bet b = BetBuilder.Straight(n, manager.chipValue); // supports 0 now
                Vector3 chipPos = GridGeometry.CellCenterWorld(transform, g.size, g.offset, g.cols, g.rows, yLocal + bets.chipYOffset, cx, cz);
                bets.PlaceBet(b, chipPos);
            }
            return true;
        }
        return false;
    }

    // ----- helpers -----
    int CodeAt(int[] preset, int cols, int cx, int cz)
    {
        return preset[cz * cols + cx];
    }

    void AddNumberIfValid(List<int> list, int[] preset, int cols, int cx, int cz)
    {
        int code = CodeAt(preset, cols, cx, cz);
        if (code >= 0 && code <= 36) list.Add(code);
    }

    // Row: collect 3 numbers (assuming 3 columns)
    List<int> RowNumbers(int[] preset, int cols, int rows, int cz)
    {
        if (cols != 3) return null;
        var nums = new List<int>();
        for (int x = 0; x < cols; x++) AddNumberIfValid(nums, preset, cols, x, cz);
        return nums.Count == 3 ? nums : null;
    }

    // Six-line (two rows of 3) from startRow
    List<int> SixLineNumbers(int[] preset, int cols, int rows, int startRow)
    {
        if (cols != 3 || startRow + 1 >= rows) return null;
        var nums = new List<int>();
        for (int x = 0; x < cols; x++)
        {
            AddNumberIfValid(nums, preset, cols, x, startRow);
            AddNumberIfValid(nums, preset, cols, x, startRow + 1);
        }
        return nums.Count == 6 ? nums : null;
    }

    // Corner: numbers at (ax,az) 2x2 block
    List<int> CornerNumbers(int[] preset, int cols, int rows, int ax, int az)
    {
        if (ax < 0 || az < 0 || ax + 1 >= cols || az + 1 >= rows) return null;
        var nums = new List<int>();
        AddNumberIfValid(nums, preset, cols, ax, az);
        AddNumberIfValid(nums, preset, cols, ax + 1, az);
        AddNumberIfValid(nums, preset, cols, ax, az + 1);
        AddNumberIfValid(nums, preset, cols, ax + 1, az + 1);
        return nums.Count == 4 ? nums : null;
    }

    // Split with neighbor (dx,dz)
    (int a, int b)? SplitPair(int[] preset, int cols, int rows, int cx, int cz, int dx, int dz)
    {
        int nx = cx + dx, nz = cz + dz;
        if (nx < 0 || nx >= cols || nz < 0 || nz >= rows) return null;

        int A = CodeAt(preset, cols, cx, cz);
        int B = CodeAt(preset, cols, nx, nz);
        if (A >= 0 && A <= 36 && B >= 0 && B <= 36)
        {
            if (A > B) { int tmp = A; A = B; B = tmp; }
            return (A, B);
        }
        return null;
    }

    // simple gizmos drawing (optional)
    void OnDrawGizmos()
    {
        if (grid1 != null) GridGeometry.DrawGridGizmos(transform, grid1.size, grid1.offset, grid1.cols, grid1.rows, yLocal, Color.black);
        if (grid2 != null) GridGeometry.DrawGridGizmos(transform, grid2.size, grid2.offset, grid2.cols, grid2.rows, yLocal, Color.cyan);
        if (grid3 != null) GridGeometry.DrawGridGizmos(transform, grid3.size, grid3.offset, grid3.cols, grid3.rows, yLocal, Color.yellow);
    }
}
