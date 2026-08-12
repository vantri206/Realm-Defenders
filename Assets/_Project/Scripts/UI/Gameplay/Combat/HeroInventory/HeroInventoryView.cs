using System.Collections.Generic;
using UnityEngine;
using System;

public class HeroInventoryView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroInventory heroInventory;
    [SerializeField] private HeroCard heroCardPrefab;
    [SerializeField] private Transform heroCardContainer;

    private readonly List<HeroCard> heroCards = new List<HeroCard>();

    public IReadOnlyList<HeroCard> HeroCards => heroCards;

    public event Action<HeroCard> OnCardAdded;
    public event Action<HeroCard> OnCardRemoved;

    public void Initialize()
    {
        ClearAllCards();
    }

    private void ClearAllCards()
    {
        for (int i = heroCards.Count - 1; i >= 0; i--)
        {
            HeroCard card = heroCards[i];
            if (card == null)
            {
                continue;
            }

            OnCardRemoved?.Invoke(card);
            Destroy(card.gameObject);
        }

        heroCards.Clear();
    }

    public HeroCard AddHeroCard(HeroInstance heroInstance)
    {
        if (heroInventory == null || heroCardPrefab == null || heroCardContainer == null)
        {
            Debug.LogWarning("[HeroInventoryView] Missing references. Cannot add hero card.");
            return null;
        }

        if (heroInstance != null && heroInstance.IsValid)
        {
            HeroCard heroCard = Instantiate(heroCardPrefab, heroCardContainer);

            int index = heroCards.Count;
            SortCardsByHeroCost();
            for (int i = 0; i < heroCards.Count; i++)
            {
                if (heroCards[i] != null && heroCards[i].HeroInstance != null && heroCards[i].HeroInstance.DeployCost > heroInstance.DeployCost)
                {
                    index = i;
                    break;
                }
            }

            if (heroCard != null)
            {
                heroCard.Initialize(heroInstance);
                heroCards.Insert(index, heroCard);
                heroCard.transform.SetSiblingIndex(index);

                OnCardAdded?.Invoke(heroCard);
                return heroCard;
            }
        }
        Debug.LogWarning("[HeroInventoryView] Failed to instantiate hero card prefab.");
        return null;
    }

    public bool RemoveHeroCard(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogWarning("[HeroInventoryView] A valid HeroInstance is required to remove a hero card.", this);
            return false;
        }

        for (int i = 0; i < heroCards.Count; i++)
        {
            HeroCard card = heroCards[i];
            if (card != null && card.HeroInstance == heroInstance)
            {
                heroCards.RemoveAt(i);
                OnCardRemoved?.Invoke(card);
                Destroy(card.gameObject);
                return true;
            }
        }

        return false;
    }

    private void SortCardsByHeroCost()
    {
        heroCards.Sort((cardA, cardB) =>
        {
            if (cardA == null || cardB == null)
            {
                return 0;
            }

            HeroInstance heroA = cardA.HeroInstance;
            HeroInstance heroB = cardB.HeroInstance;

            if (heroA == null || heroB == null)
            {
                return 0;
            }

            int costA = heroA.DeployCost;
            int costB = heroB.DeployCost;

            return costA.CompareTo(costB);
        });
    }
}
