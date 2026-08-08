using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class CombatUIController : MonoBehaviour
{
    private readonly List<HeroCard> heroCards = new List<HeroCard>();

    private PlayerCombatAction combatAction;
    private GameInput gameInput;

    private HeroCard activeCard = null;
    private HeroCard deployingCard = null;
    private HeroInstance deployingHero = null;
    private float deployStartTime;

    private float deployHoldDuration = 0.2f;

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
        if (heroInventoryView != null)
        {
            RegisterHeroCardInputs(heroInventoryView.HeroCards);
        }

        isInitialized = true;
    }

    private void OnDisable()
    {
        ClearDeploy();
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

        if (deployingCard != null)
        {
            UpdateDeploy(screenPosition);
            return;
        }

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
        gameInput.OnDirectionPerformed -= HandleDirectionPerformed;
        gameInput.OnActionPerformed -= HandleActionPerformed;

        gameInput.OnPrimaryPerformed += HandlePrimaryPerformed;
        gameInput.OnSecondaryPerformed += HandleSecondaryPerformed;
        gameInput.OnDirectionPerformed += HandleDirectionPerformed;
        gameInput.OnActionPerformed += HandleActionPerformed;
    }

    private void UnregisterInputEvents()
    {
        if (gameInput == null)
        {
            return;
        }

        gameInput.OnPrimaryPerformed -= HandlePrimaryPerformed;
        gameInput.OnSecondaryPerformed -= HandleSecondaryPerformed;
        gameInput.OnDirectionPerformed -= HandleDirectionPerformed;
        gameInput.OnActionPerformed -= HandleActionPerformed;
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

            StartDeploy(card, heroInstance);
            return;
        }
    }

    private void HandleHeroCardPrimaryCanceled(HeroCardInput cardInput, Vector2 screenPosition)
    {
        if (!isInitialized || cardInput == null)
        {
            return;
        }

        if (deployingCard != null && deployingCard.CardInput == cardInput)
        {
            ClearDeploy();
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
        if (!isInitialized || deployingCard != null || activeCard != null || combatAction == null)
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
    }

    private void HandleDirectionPerformed(Vector2Int direction)
    {
        if (!isInitialized || combatAction == null)
        {
            return;
        }

        combatAction.UpdateDeployDirection(direction);
    }

    private void HandleActionPerformed()
    {
        if (!isInitialized || combatAction == null)
        {
            return;
        }

        combatAction.PerformAction();
    }

    private void StartDeploy(HeroCard card, HeroInstance heroInstance)
    {
        if (combatAction != null)
        {
            combatAction.ShowDetailHero(heroInstance);
        }

        deployingCard = card;
        deployingHero = heroInstance;
        deployStartTime = Time.unscaledTime;
    }

    private void UpdateDeploy(Vector2 screenPosition)
    {
        if (deployingCard == null || deployingHero == null || !deployingHero.IsValid)
        {
            ClearDeploy();
            return;
        }

        if (Time.unscaledTime - deployStartTime < deployHoldDuration)
        {
            return;
        }

        HeroCard card = deployingCard;
        HeroInstance heroInstance = deployingHero;
        ClearDeploy();

        BeginDeployDrag(card, heroInstance, screenPosition);
    }

    private void ClearDeploy()
    {
        deployingCard = null;
        deployingHero = null;
        deployStartTime = 0f;
    }

    private void BeginDeployDrag(HeroCard card, HeroInstance heroInstance, Vector2 screenPosition)
    {
        if (card == null || heroInstance == null || !heroInstance.IsValid || combatAction == null)
        {
            activeCard = null;
            return;
        }

        if (!combatAction.StartDeployHero(heroInstance, screenPosition))
        {
            activeCard = null;
            return;
        }

        activeCard = card;
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

        if (!combatAction.SaveDeployCellPosition())
        {
            CancelDeployDrag();
            return;
        }

        combatAction.ChangeMode(PlayerCombatActionMode.SelectingDeployDirection);
    }

    private void CancelDeployDrag()
    {
        ClearDeploy();

        if (activeCard != null)
        {
            activeCard = null;
        }

        combatAction.FinishDeployHero();
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
