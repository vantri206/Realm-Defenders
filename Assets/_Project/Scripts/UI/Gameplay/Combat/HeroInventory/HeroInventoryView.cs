using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
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
        CreateHeroCards();
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

    private void CreateHeroCards()
    {
        if (heroInventory == null || heroCardPrefab == null || heroCardContainer == null)
        {
            Debug.LogWarning("[HeroInventoryView] Missing references. Cannot create hero cards.");
            return;
        }

        foreach (HeroInstance heroInstance in heroInventory.HeroInstances)
        {
            if (heroInstance != null && heroInstance.IsValid)
            {
                HeroCard heroCard = Instantiate(heroCardPrefab, heroCardContainer);
                if (heroCard != null)
                {
                    heroCard.Initialize(heroInstance);
                    heroCards.Add(heroCard);
                }
                else 
                {
                    Debug.LogWarning("[HeroInventoryView] Failed to instantiate hero card prefab.");
                }
            }
        }
    }
}
