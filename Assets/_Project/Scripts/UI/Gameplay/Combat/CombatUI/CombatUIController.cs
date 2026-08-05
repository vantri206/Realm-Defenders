using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatUIController : MonoBehaviour
{
    private readonly List<HeroCard> heroCards = new List<HeroCard>();

    private PlayerCombatAction combatAction;
    private GameInput gameInput;

    private HeroCard activeCard = null;

    private bool isInitialized;

    public void Initialize(PlayerCombatAction playerCombatAction, HeroInventoryView heroInventoryView)
    {
        if (isInitialized)
        {
            return;
        }

        combatAction = playerCombatAction;

        gameInput = GameInput.Instance;

        RegisterInputEvents();
        RegisterHeroCardInputs(heroInventoryView.HeroCards);

        isInitialized = true;
    }

    private void OnDisable()
    {
        UnregisterInputEvents();
        UnregisterHeroCardInputs();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        Vector2 screenPosition = gameInput.MouseScreenPosition;

        if (activeCard != null)
        {
            UpdateDeployDrag(screenPosition);
            return;
        }

        combatAction.UpdateHover(screenPosition);
    }

    private void RegisterInputEvents()
    {
        if (gameInput == null)
        {
            gameInput = GameInput.Instance;
        }

        if (gameInput == null)
        {
            return;
        }

        gameInput.OnPrimaryPerformed -= HandlePrimaryPerformed;
        gameInput.OnSecondaryPerformed -= HandleSecondaryPerformed;

        gameInput.OnPrimaryPerformed += HandlePrimaryPerformed;
        gameInput.OnSecondaryPerformed += HandleSecondaryPerformed;
    }

    private void UnregisterInputEvents()
    {
        if (gameInput == null)
        {
            return;
        }

        gameInput.OnPrimaryPerformed -= HandlePrimaryPerformed;
        gameInput.OnSecondaryPerformed -= HandleSecondaryPerformed;
    }

    private void RegisterHeroCardInputs(IReadOnlyList<HeroCard> cards)
    {
        if (cards == null)
        {
            return;
        }

        heroCards.Clear();
        heroCards.AddRange(cards);

        for (int i = 0; i < cards.Count; i++)
        {
            HeroCard card = cards[i];
            if (card != null && card.CardInput != null)
            {
                card.CardInput.OnHoverEntered += HandleHeroCardHoverEntered;
                card.CardInput.OnHoverExited += HandleHeroCardHoverExited;
                card.CardInput.OnPrimaryPerformed += HandleHeroCardPrimaryPerformed;
                card.CardInput.OnPrimaryCanceled += HandleHeroCardPrimaryCanceled;
            }
        }
    }

    private void UnregisterHeroCardInputs()
    {
        for (int i = 0; i < heroCards.Count; i++)
        {
            HeroCard card = heroCards[i];
            if (card != null && card.CardInput != null)
            {
                card.CardInput.OnHoverEntered -= HandleHeroCardHoverEntered;
                card.CardInput.OnHoverExited -= HandleHeroCardHoverExited;
                card.CardInput.OnPrimaryPerformed -= HandleHeroCardPrimaryPerformed;
                card.CardInput.OnPrimaryCanceled -= HandleHeroCardPrimaryCanceled;
            }
        }
    }

    private void HandleHeroCardHoverEntered(HeroCardInput cardInput, Vector2 screenPosition)
    {
        SetHeroCardHoverSelected(cardInput, true);
    }

    private void HandleHeroCardHoverExited(HeroCardInput cardInput, Vector2 screenPosition)
    {
        SetHeroCardHoverSelected(cardInput, false);
    }

    private void HandleHeroCardPrimaryPerformed(HeroCardInput cardInput, Vector2 screenPosition)
    {
        if (!isInitialized || cardInput == null || activeCard != null)
        {
            return;
        }

        for (int i = 0; i < heroCards.Count; i++)
        {
            HeroCard card = heroCards[i];
            if (card == null || card.CardInput != cardInput)
            {
                continue;
            }

            HeroInstance heroInstance = cardInput.HeroInstance;
            if (heroInstance == null || !heroInstance.IsValid)
            {
                return;
            }

            activeCard = card;
            BeginDeployDrag(card, heroInstance, screenPosition);
            return;
        }
    }

    private void HandleHeroCardPrimaryCanceled(HeroCardInput cardInput, Vector2 screenPosition)
    {
        if (!isInitialized || cardInput == null)
        {
            return;
        }

        if (activeCard == null || activeCard.CardInput != cardInput)
        {
            return;
        }

        EndDeployDrag(screenPosition);
        activeCard = null;
    }

    private void HandlePrimaryPerformed(Vector2 screenPosition)
    {
        if (!isInitialized || activeCard != null || combatAction == null)
        {
            return;
        }

        combatAction.UpdateHover(screenPosition);
    }

    private void HandleSecondaryPerformed(Vector2 screenPosition)
    {
        if (!isInitialized)
        {
            return;
        }

        CancelDeployDrag();
        combatAction.RefreshMode();
    }

    private void BeginDeployDrag(HeroCard card, HeroInstance heroInstance, Vector2 screenPosition)
    {
        if (card == null || heroInstance == null || !heroInstance.IsValid || combatAction == null)
        {
            activeCard = null;
            return;
        }

        activeCard = card;

        combatAction.ChangeMode(PlayerCombatActionMode.DeployingHero);
        combatAction.DeployingHero(heroInstance);
        combatAction.ShowDeployGhost(heroInstance);
        UpdateDeployDrag(screenPosition);
    }

    private void UpdateDeployDrag(Vector2 screenPosition)
    {
        if (activeCard == null || combatAction == null)
        {
            return;
        }

        combatAction.UpdateHover(screenPosition);
        combatAction.UpdateDeployGhost(screenPosition);
    }

    private void EndDeployDrag(Vector2 screenPosition)
    {
        if (activeCard == null)
        {
            return;
        }

        UpdateDeployDrag(screenPosition);

        activeCard = null;

        combatAction.HideDeployGhost();
        combatAction.RefreshMode();

        combatAction.CancelDeployHero();
    }

    private void CancelDeployDrag()
    {
        if (activeCard != null)
        {
            activeCard = null;
        }

        combatAction.HideDeployGhost();
        combatAction.RefreshMode();
    }

    private void SetHeroCardHoverSelected(HeroCardInput cardInput, bool isSelected)
    {
        if (cardInput == null)
        {
            return;
        }

        for (int i = 0; i < heroCards.Count; i++)
        {
            HeroCard card = heroCards[i];
            if (card == null || card.CardInput != cardInput)
            {
                continue;
            }

            card.CardView.SetSelected(isSelected);
            return;
        }
    }
}
