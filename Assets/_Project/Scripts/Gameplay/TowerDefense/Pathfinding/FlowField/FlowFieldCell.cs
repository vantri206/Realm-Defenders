using UnityEngine;

public class FlowFieldCell
{
    private Vector3Int cellPosition;
    private byte cost;
    private int integrationCost;
    private Vector2Int bestDirection;
    private SearchNodeState nodeState;

    public Vector3Int CellPosition => cellPosition;
    public byte Cost => cost;
    public int IntegrationCost => integrationCost;
    public Vector2Int BestDirection => bestDirection;
    public SearchNodeState NodeState => nodeState;

    public FlowFieldCell(Vector3Int cellPosition, byte cost)
    {
        this.cellPosition = cellPosition;
        this.cost = cost;
        ResetValue();
    }

    public bool SetIntegrationCost(int newCost)
    {
        if (newCost < integrationCost)
        {
            integrationCost = newCost;
            return true;
        }
        return false;
    }

    public void ResetValue()
    {
        integrationCost = int.MaxValue; // Reset to a high value
        bestDirection = Vector2Int.zero; // Reset best direction
        nodeState = SearchNodeState.Unvisited; // Reset node state
    }
    
    public void SetBestDirection(Vector2Int direction)
    {
        bestDirection = direction;
    }

    public void SetNodeState(SearchNodeState state)
    {
        nodeState = state;
    }
}