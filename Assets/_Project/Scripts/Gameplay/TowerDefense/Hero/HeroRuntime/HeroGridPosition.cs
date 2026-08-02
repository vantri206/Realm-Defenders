using UnityEngine;

[DisallowMultipleComponent]
public class HeroGridPosition : MonoBehaviour
{
    private CombatGrid combatGrid;
    private Vector3Int currentCell;
    private bool hasCell;

    public CombatGrid CombatGrid => combatGrid;
    public Vector3Int CurrentCell => currentCell;
    public bool HasCell => hasCell;

    public void Initialize(CombatGrid combatGrid, Vector3Int cellPosition)
    {
        this.combatGrid = combatGrid;
        SetCell(cellPosition);
    }

    public void SetCell(Vector3Int cellPosition)
    {
        currentCell = cellPosition;
        hasCell = true;
    }

    public void Clear()
    {
        combatGrid = null;
        currentCell = Vector3Int.zero;
        hasCell = false;
    }
}
