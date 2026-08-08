using UnityEngine;
using UnityEngine.EventSystems;

public class GhostDirectionInput : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GridDirection direction;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        GameInput.Instance.RaiseDirectionPerformed(direction.ToVector2Int());
    }
}
