using UnityEngine;

[DisallowMultipleComponent]
public class TooltipTrigger : MonoBehaviour
{
    [SerializeField, TextArea] private string tooltipText;

    public string TooltipText => tooltipText;
    public bool HasText => !string.IsNullOrWhiteSpace(tooltipText);

    public void SetText(string text)
    {
        tooltipText = text;
    }
}
