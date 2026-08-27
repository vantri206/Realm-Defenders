using System;
using System.Collections.Generic;
using UnityEngine;

public class HeroRosterView : MonoBehaviour
{
    [Header("Player Session")]
    [SerializeField] private PlayerSession playerSession;

    [Header("Screen")]
    [SerializeField] private GameObject viewRoot;
    [SerializeField] private UIButtonFeedback closeButton;

    [Header("Hero Cards")]
    [SerializeField] private Transform heroCardContainer;
    [SerializeField] private RosterHeroCard heroCardPrefab;

    [Header("Hero Views")]
    [SerializeField] private RosterHeroDetailView heroDetailView;
    [SerializeField] private HeroProgressionView heroProgressionView;
    [SerializeField] private RosterGearPickerView rosterGearPickerView;

    private readonly List<RosterHeroCard> spawnedCards = new List<RosterHeroCard>();

    private RosterHeroCard selectedCard;
    private HeroInstance selectedHero;

    public event Action OnCloseRequested;

    private void Awake()
    {
        CacheReferences();
        HideDetail();
    }

    private void OnEnable()
    {
        RegisterCloseButtonEvent();
        RegisterPlayerSessionEvents();
        RegisterGearViewEvents();
    }

    private void OnDisable()
    {
        UnregisterCloseButtonEvent();
        UnregisterPlayerSessionEvents();
        UnregisterGearViewEvents();
    }

    private void OnDestroy()
    {
        UnregisterCloseButtonEvent();
        UnregisterCardEvents();
        UnregisterPlayerSessionEvents();
        UnregisterGearViewEvents();
    }

    public void Show(HeroRoster heroRoster)
    {
        viewRoot.SetActive(true);
        ResetHeroCards(heroRoster);
    }

    public void Hide()
    {
        HideGearPicker();
        viewRoot.SetActive(false);
    }

    private void ResetHeroCards(HeroRoster heroRoster)
    {
        ClearSpawnedCards();
        HideDetail();

        if (heroRoster == null || !heroRoster.HasHeroes)
        {
            return;
        }

        if (heroCardContainer == null || heroCardPrefab == null)
        {
            Debug.LogError("[HeroRosterView] Hero card container and prefab are required to display the team.", this);
            return;
        }

        IReadOnlyList<HeroInstance> heroes = heroRoster.Heroes;

        for (int i = 0; i < heroes.Count; i++)
        {
            HeroInstance hero = heroes[i];
            if (hero == null || !hero.IsValid)
            {
                continue;
            }

            RosterHeroCard card = Instantiate(heroCardPrefab, heroCardContainer);
            card.BindHeroData(hero);
            card.OnCardClicked += HandleHeroCardClicked;
            spawnedCards.Add(card);
        }
    }

    private void HandleHeroCardClicked(RosterHeroCard card, HeroInstance hero)
    {
        if (hero == null || !hero.IsValid || hero.Definition == null)
        {
            return;
        }

        HideGearPicker();

        if (selectedCard != null && selectedCard != card)
        {
            selectedCard.SetActive(true);
        }

        selectedCard = card;
        selectedHero = hero;
        selectedCard.SetActive(false);

        ShowDetail(selectedHero);
    }

    private void ShowDetail(HeroInstance hero)
    {
        heroDetailView.Show(hero);
        heroProgressionView.Show(hero, playerSession);
    }

    private void HideDetail()
    {
        selectedCard = null;
        selectedHero = null;
        HideGearPicker();
        heroDetailView.Hide();
        heroProgressionView.Hide();
    }

    private void HandleGearPickerRequested(GearType gearType)
    {
        if (selectedHero == null || !selectedHero.IsValid || playerSession == null || rosterGearPickerView == null)
        {
            return;
        }

        IReadOnlyList<GearInstance> allGears = playerSession.GetAllGears();
        List<GearInstance> filteredGears = new List<GearInstance>();
        GearInstance selectedGear = null;

        if (gearType == GearType.Weapon && selectedHero.EquippedWeapon != null && selectedHero.EquippedWeapon.IsValid)
        {
            selectedGear = selectedHero.EquippedWeapon;
        }
        else if (gearType == GearType.Armor && selectedHero.EquippedArmor != null && selectedHero.EquippedArmor.IsValid)
        {
            selectedGear = selectedHero.EquippedArmor;
        }

        if (allGears != null)
        {
            for (int i = 0; i < allGears.Count; i++)
            {
                GearInstance gear = allGears[i];
                if (gear == null || !gear.IsValid || gear.Definition.GearType != gearType)
                {
                    continue;
                }

                if (gear == selectedGear)
                {
                    filteredGears.Insert(0, gear);  // first show the currently equipped gear
                }
                else
                {
                    filteredGears.Add(gear);
                }
            }
        }

        rosterGearPickerView.Show(filteredGears, selectedHero);
    }

    private void HandleGearSelected(GearInstance gear)
    {
        if (selectedHero == null || !selectedHero.IsValid || gear == null || !gear.IsValid || playerSession == null)
        {
            return;
        }

        switch (gear.Definition.GearType)
        {
            case GearType.Weapon:
                playerSession.EquipWeapon(selectedHero, gear);
                break;
            case GearType.Armor:
                playerSession.EquipArmor(selectedHero, gear);
                break;
            default:
                return;
        }

        HideGearPicker();
    }

    private void HideGearPicker()
    {
        if (rosterGearPickerView != null)
        {
            rosterGearPickerView.Hide();
        }
    }

    private void HandleHeroChanged(HeroInstance hero, HeroChangeType changeType)
    {
        if (hero == null || !hero.IsValid)
        {
            return;
        }

        if (changeType == HeroChangeType.Progression)
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                RosterHeroCard card = spawnedCards[i];
                if (card == null)
                {
                    continue;
                }

                HeroInstance cardHero = card.HeroInstance;
                if (cardHero != null && cardHero.Equals(hero))
                {
                    card.BindHeroData(hero);
                    break;
                }
            }
        }

        if (selectedHero != null && selectedHero.Equals(hero) &&
            (changeType == HeroChangeType.Stats || changeType == HeroChangeType.Skills))
        {
            ShowDetail(selectedHero);
        }
    }

    private void ClearSpawnedCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            RosterHeroCard card = spawnedCards[i];
            if (card == null)
            {
                continue;
            }

            card.OnCardClicked -= HandleHeroCardClicked;
            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }

        spawnedCards.Clear();
        selectedCard = null;
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

    private void UnregisterCardEvents()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] != null)
            {
                spawnedCards[i].OnCardClicked -= HandleHeroCardClicked;
            }
        }
    }

    private void RegisterPlayerSessionEvents()
    {
        if (playerSession != null)
        {
            playerSession.OnHeroChanged += HandleHeroChanged;
        }
    }

    private void UnregisterPlayerSessionEvents()
    {
        if (playerSession != null)
        {
            playerSession.OnHeroChanged -= HandleHeroChanged;
        }
    }

    private void RegisterGearViewEvents()
    {
        if (heroDetailView != null)
        {
            heroDetailView.OnGearPickerRequested -= HandleGearPickerRequested;
            heroDetailView.OnGearPickerRequested += HandleGearPickerRequested;
        }

        if (rosterGearPickerView != null)
        {
            rosterGearPickerView.OnGearSelected -= HandleGearSelected;
            rosterGearPickerView.OnGearSelected += HandleGearSelected;
            rosterGearPickerView.OnCloseRequested -= HandleGearPickerCloseRequested;
            rosterGearPickerView.OnCloseRequested += HandleGearPickerCloseRequested;
        }
    }

    private void UnregisterGearViewEvents()
    {
        if (heroDetailView != null)
        {
            heroDetailView.OnGearPickerRequested -= HandleGearPickerRequested;
        }

        if (rosterGearPickerView != null)
        {
            rosterGearPickerView.OnGearSelected -= HandleGearSelected;
            rosterGearPickerView.OnCloseRequested -= HandleGearPickerCloseRequested;
        }
    }

    private void HandleGearPickerCloseRequested()
    {
        HideGearPicker();
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
