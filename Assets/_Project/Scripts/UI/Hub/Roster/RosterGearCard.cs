using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RosterGearCard : MonoBehaviour
{
    [Header("Gear Card")]
    [SerializeField] private Button cardButton;
    [SerializeField] private Image background;
    [SerializeField] private Image selectedFrame;
    [SerializeField] private Image gearIcon;
    [SerializeField] private Image equippedBadge;
    [SerializeField] private TooltipTrigger tooltipTrigger;

    private GearInstance gearInstance;

    public GearInstance GearInstance => gearInstance;

    public event Action<RosterGearCard, GearInstance> OnCardClicked;

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

    public void BindGearData(GearInstance gear, HeroInstance currentSelectedHero)
    {
        CacheReferences();

        if (gear == null || !gear.IsValid)
        {
            Clear();
            return;
        }

        gearInstance = gear;

        if (cardButton != null)
        {
            cardButton.interactable = true;
        }

        if (background != null)
        {
            background.enabled = true;
        }

        SetImage(gearIcon, gear.Definition.GearIcon);
        SetTooltipText(TooltipHelper.GetGearTooltipText(gear));

        bool isSelected = currentSelectedHero != null && gear.EquippedHero == currentSelectedHero;
        SetActive(selectedFrame, isSelected);

        HeroInstance equippedHero = gear.EquippedHero;

        if (equippedHero != null && equippedHero.Definition != null)
        {
            SetImage(equippedBadge, equippedHero.Definition.HeroIcon);
        }
        else
        {
            SetImage(equippedBadge, null);
        }
    }

    public void Clear()
    {
        gearInstance = null;

        if (cardButton != null)
        {
            cardButton.interactable = false;
        }

        SetImage(gearIcon, null);
        SetActive(selectedFrame, false);
        SetImage(equippedBadge, null);
        SetTooltipText(string.Empty);
    }

    private void HandleButtonClicked()
    {
        if (gearInstance == null || !gearInstance.IsValid)
        {
            return;
        }

        OnCardClicked?.Invoke(this, gearInstance);
    }

    private void RegisterButtonEvent()
    {
        if (cardButton == null)
        {
            return;
        }

        cardButton.onClick.RemoveListener(HandleButtonClicked);
        cardButton.onClick.AddListener(HandleButtonClicked);
    }

    private void UnregisterButtonEvent()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    private void CacheReferences()
    {
        if (cardButton == null)
        {
            cardButton = GetComponent<Button>();
        }

        if (tooltipTrigger == null)
        {
            tooltipTrigger = GetComponent<TooltipTrigger>();
        }
    }

    private void SetTooltipText(string text)
    {
        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetText(text);
        }
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
    }

    private static void SetActive(Image image, bool isActive)
    {
        if (image != null)
        {
            image.gameObject.SetActive(isActive);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif
}
