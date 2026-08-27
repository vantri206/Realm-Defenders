using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class GearSlotBinding : MonoBehaviour
{
    [SerializeField] private Button gearSlotButton;
    [SerializeField] private Image unequippedGearFrame;
    [SerializeField] private Image equippedGearFrame;
    [SerializeField] private Image gearIcon;
    [SerializeField] private TooltipTrigger tooltipTrigger;

    public Button GearSlotButton => gearSlotButton;
    public Image UnequippedGearFrame => unequippedGearFrame;
    public Image EquippedGearFrame => equippedGearFrame;
    public Image GearIcon => gearIcon;

    public event Action OnClicked;

    public void SetGearTooltip(GearInstance gear)
    {
        CacheReferences();

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetText(TooltipHelper.GetGearTooltipText(gear));
        }
    }

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        RegisterButtonEvent();
    }

    private void OnDisable()
    {
        UnregisterButtonEvent();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvent();
    }

    private void HandleButtonClicked()
    {
        OnClicked?.Invoke();
    }

    private void RegisterButtonEvent()
    {
        if (gearSlotButton == null)
        {
            return;
        }

        gearSlotButton.onClick.RemoveListener(HandleButtonClicked);
        gearSlotButton.onClick.AddListener(HandleButtonClicked);
    }

    private void UnregisterButtonEvent()
    {
        if (gearSlotButton != null)
        {
            gearSlotButton.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    private void CacheReferences()
    {
        if (gearSlotButton == null)
        {
            gearSlotButton = GetComponent<Button>();
        }

        if (tooltipTrigger == null)
        {
            tooltipTrigger = GetComponent<TooltipTrigger>();
        }
    }
}
