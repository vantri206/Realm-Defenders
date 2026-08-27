using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SkillSlotBinding : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private GameObject cooldownIcon;
    [SerializeField] private UIValueTextBinding cooldownText = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding skillName = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding description = new UIValueTextBinding();

    public Image Icon => icon;
    public GameObject CooldownIcon => cooldownIcon;
    public UIValueTextBinding CooldownText => cooldownText;
    public UIValueTextBinding SkillName => skillName;
    public UIValueTextBinding Description => description;
}
