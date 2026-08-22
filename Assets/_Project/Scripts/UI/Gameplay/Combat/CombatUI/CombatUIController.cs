using System.Collections.Generic;
using UnityEngine;

public class CombatUIController : MonoBehaviour
{
    private readonly List<HeroCard> heroCards = new List<HeroCard>();

    private PlayerCombatAction combatAction;
    private HeroSquadView heroSquadView;
    private GameInput gameInput;

    private HeroCard activeCard = null;
    private HeroCard deployingCard = null;
    private HeroCombatState deployingHero = null;
    private float deployStartTime;

    private float deployHoldDuration = 0.2f;

    private bool isInitialized;

    public void Initialize(PlayerCombatAction playerCombatAction, HeroSquadView squadView)
    {
        if (isInitialized)
        {
            return;
        }

        if (playerCombatAction == null || squadView == null)
        {
            Debug.LogError("[CombatUIController] Required references are null.", this);
            return;
        }

        combatAction = playerCombatAction;
        heroSquadView = squadView;
        gameInput = GameInput.Instance;

        RegisterInputEvents();
        RegisterSquadEvents();

        for (int i = 0; i < heroSquadView.HeroCards.Count; i++)
        {
            AddCard(heroSquadView.HeroCards[i]);
        }

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
        UnregisterSquadEvents();
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

    private void RegisterSquadEvents()
    {
        if (heroSquadView == null)
        {
            Debug.LogError("[CombatUIController] HeroSquadView is required to register squad events.", this);
            return;
        }

        heroSquadView.OnCardAdded += AddCard;
        heroSquadView.OnCardRemoved += RemoveCard;
    }

    private void UnregisterSquadEvents()
    {
        if (heroSquadView == null)
        {
            return;
        }

        heroSquadView.OnCardAdded -= AddCard;
        heroSquadView.OnCardRemoved -= RemoveCard;
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

        HeroCombatState combatState = card.CombatState;
        if (combatState == null || !combatState.IsValid)
        {
            return;
        }

        StartDeploy(card, combatState);
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

    private void StartDeploy(HeroCard card, HeroCombatState combatState)
    {
        if (combatAction != null)
        {
            combatAction.ShowDetailHero(combatState);
        }

        deployingCard = card;
        deployingHero = combatState;
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
        HeroCombatState combatState = deployingHero;
        ClearDeploy();

        BeginDeployDrag(card, combatState, screenPosition);
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

    private void BeginDeployDrag(HeroCard card, HeroCombatState combatState, Vector2 screenPosition)
    {
        if (card == null || combatState == null || !combatState.IsValid || combatAction == null)
        {
            activeCard = null;
            return;
        }

        if (!combatAction.StartDeployHero(combatState, screenPosition))
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
