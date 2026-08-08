using UnityEngine;

public class UnitGridPosition : MonoBehaviour
{
    private Vector3Int currentCell;
    private bool hasCell;

    public Vector3Int CurrentCell => currentCell;
    public bool HasCell => hasCell;

    public void Initialize(Vector3Int cellPosition)
    {
        SetCell(cellPosition);
    }

    public void SetCell(Vector3Int cellPosition)
    {
        currentCell = cellPosition;
        hasCell = true;
    }

    public void Clear()
    {
        currentCell = Vector3Int.zero;
        hasCell = false;
    }
}
