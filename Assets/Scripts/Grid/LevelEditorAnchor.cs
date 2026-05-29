using UnityEngine;

[ExecuteAlways]
public class LevelEditorAnchor : MonoBehaviour
{
    [SerializeField] private float cellSize = 1.1f;

    public float CellSize => cellSize;

    public Vector3 GridToWorldPosition(int x, int y)
    {
        return transform.position + new Vector3(x * cellSize, 0f, y * cellSize);
    }

    public bool TryWorldToGrid(Vector3 worldPosition, out int x, out int y)
    {
        Vector3 local = worldPosition - transform.position;
        x = Mathf.FloorToInt(local.x / cellSize);
        y = Mathf.FloorToInt(local.z / cellSize);
        return true;
    }
}
