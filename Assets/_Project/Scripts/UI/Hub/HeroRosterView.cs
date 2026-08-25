using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeroRosterView : MonoBehaviour
{
    [Header("Screen")]
    [SerializeField] private GameObject viewRoot;
    [SerializeField] private Button closeButton;

    [Header("Hero Cards")]
    [SerializeField] private Transform heroCardContainer;
    [SerializeField] private RosterHeroCard heroCardPrefab;

    [Header("Hero Detail")]
    [SerializeField] private GameObject heroDetailRoot;
    [SerializeField] private Image detailHeroImage;
    [SerializeField] private UIValueTextBinding detailHeroName = new UIValueTextBinding();

    private readonly List<RosterHeroCard> spawnedCards = new List<RosterHeroCard>();
    private RosterHeroCard selectedCard;

    public event Action OnCloseRequested;

    private void Awake()
    {
        CacheReferences();
        HideDetail();
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

    public void Show(HeroRoster heroRoster)
    {
        viewRoot.SetActive(true);
        ResetHeroCards(heroRoster);
    }

    public void Hide()
    {
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
            Debug.LogError("[TeamView] Hero card container and prefab are required to display the team.", this);
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

        if (selectedCard != null && selectedCard != card)
        {
            selectedCard.SetActive(true);
        }

        selectedCard = card;
        selectedCard.SetActive(false);
        ShowDetail(hero);
    }

    private void ShowDetail(HeroInstance hero)
    {
        if (heroDetailRoot != null)
        {
            heroDetailRoot.SetActive(true);
        }

        if (detailHeroImage != null)
        {
            if (hero.Definition.HeroDisplaySprite != null)
            {
                detailHeroImage.sprite = hero.Definition.HeroDisplaySprite;
                detailHeroImage.enabled = true;
            }
            else
            {
                detailHeroImage.sprite = null;
                detailHeroImage.enabled = false;
            }
        }

        if (detailHeroName != null)
        {
            detailHeroName.SetText(hero.Definition.HeroName.ToString().ToUpper());
        }
    }

    private void HideDetail()
    {
        selectedCard = null;

        if (heroDetailRoot != null)
        {
            heroDetailRoot.SetActive(false);
        }

        if (detailHeroImage != null)
        {
            detailHeroImage.sprite = null;
            detailHeroImage.enabled = false;
        }

        if (detailHeroName != null)
        {
            detailHeroName.Refresh();
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

        closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
        closeButton.onClick.AddListener(HandleCloseButtonClicked);
    }

    private void UnregisterCloseButtonEvent()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
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

    private void CacheReferences()
    {
        if (viewRoot == null)
        {
            viewRoot = gameObject;
        }
    }
}
