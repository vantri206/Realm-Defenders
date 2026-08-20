using UnityEngine;
using UnityEngine.EventSystems;

public class HeroInteractionInput : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private HeroRuntime heroRuntime;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        heroRuntime.HandleSelection();
    }
}
