using System.Collections.Generic;
using UnityEngine;

public class HeroInventoryView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroInventory heroInventory;
    [SerializeField] private HeroCard heroCardPrefab;
    [SerializeField] private Transform heroCardContainer;

    private readonly List<HeroCard> heroCards = new List<HeroCard>();

    public IReadOnlyList<HeroCard> HeroCards => heroCards;

    public void Initialize()
    {
        ClearAllCards();
    }

    private void ClearAllCards()
    {
        heroCards.Clear();

        if (heroCardContainer == null)
        {
            return;
        }

        foreach (Transform child in heroCardContainer)
        {
            Destroy(child.gameObject);
        }
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
            }
            else 
            {
                Debug.LogWarning("[HeroInventoryView] Failed to instantiate hero card prefab.");
            }
            return heroCard;
        }
        return null;
    }

    public bool RemoveHeroCard(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return false;
        }

        for (int i = 0; i < heroCards.Count; i++)
        {
            HeroCard card = heroCards[i];
            if (card != null && card.HeroInstance == heroInstance)
            {
                UnregisterHeroEvents(card, heroInstance);
                heroCards.RemoveAt(i);
                Destroy(card.gameObject);
                return true;
            }
        }

        return false;
    }

    private void UnregisterHeroEvents(HeroCard card, HeroInstance heroInstance)
    {
        if (card != null && heroInstance != null)
        {
            heroInstance.OnDeployStateChanged -= card.OnHeroDeployStateChanged;
        }
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
