using System.Collections.Generic;
using UnityEngine;

public class CombatUIController : MonoBehaviour
{
    private readonly List<HeroCard> heroCards = new List<HeroCard>();

    private PlayerCombatAction combatAction;
    private HeroInventoryView heroInventoryView;
    private GameInput gameInput;

    private HeroCard activeCard = null;
    private HeroCard deployingCard = null;
    private HeroInstance deployingHero = null;
    private float deployStartTime;

    private float deployHoldDuration = 0.2f;

    private bool isInitialized;

    public void Initialize(PlayerCombatAction playerCombatAction, HeroInventoryView inventoryView)
    {
        if (isInitialized)
        {
            return;
        }

        if (playerCombatAction == null || inventoryView == null)
        {
            Debug.LogError("[CombatUIController] Required references are null.", this);
            return;
        }

        combatAction = playerCombatAction;
        heroInventoryView = inventoryView;
        gameInput = GameInput.Instance;

        RegisterInputEvents();
        RegisterInventoryEvents();

        isInitialized = true;
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            return;
        }

        gameInput = GameInput.Instance;
        RegisterInputEvents();
        RegisterAllHeroCardInputs();
    }

    private void OnDisable()
    {
        ClearDeploy();
        activeCard = null;

        UnregisterInputEvents();
        UnregisterAllHeroCardInputs();
    }

    private void OnDestroy()
    {
        UnregisterInventoryEvents();
        UnregisterInputEvents();
        UnregisterAllHeroCardInputs();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        if (gameInput == null)
        {
            Debug.LogError("[CombatUIController] GameInput is required to update combat UI input.", this);
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

    public void AddCard(HeroCard card)
    {
        if (card == null)
        {
            return;
        }

        if (!heroCards.Contains(card))
        {
            heroCards.Add(card);

            if (isActiveAndEnabled)
            {
                RegisterHeroCardInputs(card);
            }
        }
    }

    public void RemoveCard(HeroCard card)
    {
        if (card == null)
        {
            return;
        }

        UnregisterHeroCardInputs(card);
        heroCards.Remove(card);

        if (activeCard == card || deployingCard == card)
        {
            CancelDeployDrag();
            return;
        }
    }

    private void RegisterAllHeroCardInputs()
    {
        for (int i = 0; i < heroCards.Count; i++)
        {
            RegisterHeroCardInputs(heroCards[i]);
        }
    }

    private void UnregisterAllHeroCardInputs()
    {
        for (int i = 0; i < heroCards.Count; i++)
        {
            UnregisterHeroCardInputs(heroCards[i]);
        }
    }

    private void RegisterInventoryEvents()
    {
        if (heroInventoryView == null)
        {
            Debug.LogError("[CombatUIController] HeroInventoryView is required to register inventory events.", this);
            return;
        }

        heroInventoryView.OnCardAdded += AddCard;
        heroInventoryView.OnCardRemoved += RemoveCard;
    }

    private void UnregisterInventoryEvents()
    {
        if (heroInventoryView == null)
        {
            return;
        }

        heroInventoryView.OnCardAdded -= AddCard;
        heroInventoryView.OnCardRemoved -= RemoveCard;
    }

    private void RegisterInputEvents()
    {
        if (gameInput == null)
        {
            gameInput = GameInput.Instance;
        }

        if (gameInput == null)
        {
            Debug.LogError("[CombatUIController] GameInput is required to register combat UI input events.", this);
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

    public void RegisterHeroCardInputs(HeroCard card)
    {
        if (card == null || card.CardInput == null)
        {
            Debug.LogError("[CombatUIController] HeroCard and HeroCardInput are required to register card input events.", this);
            return;
        }

        card.CardInput.OnHoverEntered += HandleHeroCardHoverEntered;
        card.CardInput.OnHoverExited += HandleHeroCardHoverExited;
        card.CardInput.OnPrimaryPerformed += HandleHeroCardPrimaryPerformed;
        card.CardInput.OnPrimaryCanceled += HandleHeroCardPrimaryCanceled;
    }

    public void UnregisterHeroCardInputs(HeroCard card)
    {
        if (card != null && card.CardInput != null)
        {
            card.CardInput.OnHoverEntered -= HandleHeroCardHoverEntered;
            card.CardInput.OnHoverExited -= HandleHeroCardHoverExited;
            card.CardInput.OnPrimaryPerformed -= HandleHeroCardPrimaryPerformed;
            card.CardInput.OnPrimaryCanceled -= HandleHeroCardPrimaryCanceled;
        }
    }

    private void HandleHeroCardHoverEntered(HeroCard card)
    {
        SetHeroCardHoverSelected(card, true);
    }

    private void HandleHeroCardHoverExited(HeroCard card)
    {
        SetHeroCardHoverSelected(card, false);
    }

    private void HandleHeroCardPrimaryPerformed(HeroCard card, Vector2 screenPosition)
    {
        if (!isInitialized || card == null || activeCard != null)
        {
            return;
        }

        HeroInstance heroInstance = card.HeroInstance;
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return;
        }

        StartDeploy(card, heroInstance);
        return;
    }

    private void HandleHeroCardPrimaryCanceled(HeroCard card, Vector2 screenPosition)
    {
        if (!isInitialized || card == null)
        {
            return;
        }

        if (activeCard != null && activeCard == card)
        {
            EndDeployDrag(screenPosition);
            ClearActiveCard();
            return;
        }

        if (deployingCard != null && deployingCard == card)
        {
            ClearDeploy();
            return;
        }
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

    private void ClearActiveCard()
    {
        activeCard = null;
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
        activeCard = null;

        if (combatAction != null)
        {
            combatAction.FinishDeployHero();
        }
    }

    private void SetHeroCardHoverSelected(HeroCard card, bool isSelected)
    {
        if (card == null)
        {
            return;
        }

        if (card.CardView == null)
        {
            Debug.LogError("[CombatUIController] HeroCardView is required to update hover selection.", this);
            return;
        }

        card.CardView.SetSelected(isSelected);
    }
}
