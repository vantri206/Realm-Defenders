using UnityEngine;

[DisallowMultipleComponent]
public sealed class StatBinding : MonoBehaviour
{
    [SerializeField] private UIValueTextBinding totalText = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding detailText = new UIValueTextBinding();

    public UIValueTextBinding TotalText => totalText;
    public UIValueTextBinding DetailText => detailText;
}
