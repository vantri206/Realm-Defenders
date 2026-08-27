using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class RosterGearPickerView : MonoBehaviour
{
    [Header("Screen")]
    [SerializeField] private GameObject viewRoot;
    [SerializeField] private UIButtonFeedback closeButton;

    [Header("Gear Cards")]
    [SerializeField] private Transform gearCardContainer;
    [SerializeField] private RosterGearCard gearCardPrefab;

    private readonly List<RosterGearCard> spawnedCards = new List<RosterGearCard>();

    public event Action<GearInstance> OnGearSelected;
    public event Action OnCloseRequested;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        RegisterCloseButtonEvent();
    }

    private void OnDisable()
    {
        UnregisterCloseButtonEvent();
    }

    private void OnDestroy()
    {
        UnregisterCloseButtonEvent();
        UnregisterCardEvents();
    }

    public void Show(IReadOnlyList<GearInstance> gears, HeroInstance selectedHero)
    {
        ClearSpawnedCards();

        if (viewRoot != null)
        {
            viewRoot.SetActive(true);
        }

        if (gearCardContainer == null || gearCardPrefab == null)
        {
            Debug.LogError("[RosterGearPickerView] Gear card container and prefab are required to display gears.", this);
            return;
        }

        if (gears == null)
        {
            return;
        }

        for (int i = 0; i < gears.Count; i++)
        {
            GearInstance gear = gears[i];
            if (gear == null || !gear.IsValid)
            {
                continue;
            }

            RosterGearCard card = Instantiate(gearCardPrefab, gearCardContainer);
            card.BindGearData(gear, selectedHero);

            card.OnCardClicked += HandleGearCardClicked;

            spawnedCards.Add(card);
        }
    }

    public void Hide()
    {
        ClearSpawnedCards();

        if (viewRoot != null)
        {
            viewRoot.SetActive(false);
        }
    }

    private void HandleGearCardClicked(RosterGearCard card, GearInstance gear)
    {
        if (card == null || gear == null || !gear.IsValid)
        {
            return;
        }

        OnGearSelected?.Invoke(gear);
    }

    private void HandleCloseButtonClicked()
    {
        OnCloseRequested?.Invoke();
    }

    private void RegisterCloseButtonEvent()
    {
        if (closeButton == null)
        {
            return;
        }

        closeButton.OnClicked -= HandleCloseButtonClicked;
        closeButton.OnClicked += HandleCloseButtonClicked;
    }

    private void UnregisterCloseButtonEvent()
    {
        if (closeButton != null)
        {
            closeButton.OnClicked -= HandleCloseButtonClicked;
        }
    }

    private void ClearSpawnedCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            RosterGearCard card = spawnedCards[i];
            if (card == null)
            {
                continue;
            }

            card.OnCardClicked -= HandleGearCardClicked;
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }

        spawnedCards.Clear();
    }

    private void UnregisterCardEvents()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
            {
                spawnedCards[i].OnCardClicked -= HandleGearCardClicked;
            }
        }
    }

    private void CacheReferences()
    {
        if (viewRoot == null)
        {
            viewRoot = gameObject;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif
}
