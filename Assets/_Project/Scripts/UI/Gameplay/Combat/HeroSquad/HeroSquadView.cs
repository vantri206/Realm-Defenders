using System.Collections.Generic;
using UnityEngine;
using System;

public class HeroSquadView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroSquad heroSquad;
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

    public void OnDisable()
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

    public HeroCard AddHeroCard(HeroCombatState combatState)
    {
        if (heroSquad == null || heroCardPrefab == null || heroCardContainer == null)
        {
            Debug.LogWarning("[HeroSquadView] Missing references. Cannot add hero card.");
            return null;
        }

        if (combatState != null && combatState.IsValid)
        {
            HeroCard heroCard = Instantiate(heroCardPrefab, heroCardContainer);

            int index = heroCards.Count;
            SortCardsByHeroCost();
            for (int i = 0; i < heroCards.Count; i++)
            {
                if (heroCards[i] != null && heroCards[i].CombatState != null && heroCards[i].CombatState.DeployCost > combatState.DeployCost)
                {
                    index = i;
                    break;
                }
            }

            if (heroCard != null)
            {
                heroCard.Initialize(combatState);
                heroCards.Insert(index, heroCard);
                heroCard.transform.SetSiblingIndex(index);

                OnCardAdded?.Invoke(heroCard);
                return heroCard;
            }
        }
        Debug.LogWarning("[HeroSquadView] Failed to instantiate hero card prefab.");
        return null;
    }

    public bool RemoveHeroCard(HeroCombatState combatState)
    {
        if (combatState == null || !combatState.IsValid)
        {
            Debug.LogWarning("[HeroSquadView] A valid HeroCombatState is required to remove a hero card.", this);
            return false;
        }

        for (int i = 0; i < heroCards.Count; i++)
        {
            HeroCard card = heroCards[i];
            if (card != null && card.CombatState == combatState)
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

            HeroCombatState heroA = cardA.CombatState;
            HeroCombatState heroB = cardB.CombatState;

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
