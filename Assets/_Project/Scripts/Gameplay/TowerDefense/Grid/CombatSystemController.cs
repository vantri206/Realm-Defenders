using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public class CombatSystemController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CombatGrid combatGrid;
    [SerializeField] private HeroInventory heroInventory;
    [SerializeField] private HeroInventoryView heroInventoryView;
    [SerializeField] private HeroPlacement heroPlacement;
    [SerializeField] private HeroDeploymentSystem heroDeploymentSystem;
    [SerializeField] private HeroDetailView heroDetailView;
    [SerializeField] private TileOverlayRenderer tileOverlayRenderer;
    [SerializeField] private PlayerCombatAction playerCombatAction;
    [SerializeField] private CombatUIController combatUIController;
    [SerializeField] private GhostHeroView ghostHeroPrefab;

    private GhostHeroView ghostHero;

    private void Awake()
    {
        InitializeCombatSystem();
    }

    public void InitializeCombatSystem()
    {
        if (!CheckReferences())
        {
            return;
        }

        ghostHero = CreateGhostHeroView();
        if (ghostHero == null)
        {
            return;
        }

        combatGrid.BuildGridMap();
        heroPlacement.Initialize(combatGrid);
        heroDeploymentSystem.Initialize(heroInventory, heroPlacement);
        playerCombatAction.Initialize(mainCamera, combatGrid, heroDeploymentSystem, heroDetailView, tileOverlayRenderer, ghostHero);
        playerCombatAction.ChangeMode(PlayerCombatActionMode.None);

        heroInventory.Initialize(heroInventoryView);

        combatUIController.Initialize(playerCombatAction, heroInventoryView);
    }

    private GhostHeroView CreateGhostHeroView()
    {
        if (ghostHero != null)
        {
            return ghostHero;
        }

        if (ghostHeroPrefab == null)
        {
            return null;
        }

        GhostHeroView instance = Instantiate(ghostHeroPrefab);
        instance.Hide();
        return instance;
    }

    [ContextMenu("Debug/Change Mode None")]
    private void ChangeModeNone()
    {
        playerCombatAction.RefreshMode();
    }

    [ContextMenu("Debug/Change Mode Deploying Hero")]
    private void ChangeModeDeployingHero()
    {
        playerCombatAction.ChangeMode(PlayerCombatActionMode.DeployingHero);
    }
    
    private bool CheckReferences()
    {
        bool hasReferences = true;

        if (mainCamera == null)
        {
            Debug.LogWarning("[CombatSystemController] mainCamera is not assigned.", this);
            hasReferences = false;
        }

        if (combatGrid == null)
        {
            Debug.LogWarning("[CombatSystemController] combatGrid is not assigned.", this);
            hasReferences = false;
        }

        if (heroInventory == null)
        {
            Debug.LogWarning("[CombatSystemController] heroInventory is not assigned.", this);
            hasReferences = false;
        }

        if (heroInventoryView == null)
        {
            Debug.LogWarning("[CombatSystemController] heroInventoryView is not assigned.", this);
            hasReferences = false;
        }

        if (heroPlacement == null)
        {
            Debug.LogWarning("[CombatSystemController] heroPlacement is not assigned.", this);
            hasReferences = false;
        }

        if (heroDeploymentSystem == null)
        {
            Debug.LogWarning("[CombatSystemController] heroDeploymentSystem is not assigned.", this);
            hasReferences = false;
        }

        if (heroDetailView == null)
        {
            Debug.LogWarning("[CombatSystemController] heroDetailView is not assigned.", this);
            hasReferences = false;
        }

        if (tileOverlayRenderer == null)
        {
            Debug.LogWarning("[CombatSystemController] tileOverlayRenderer is not assigned.", this);
            hasReferences = false;
        }

        if (playerCombatAction == null)
        {
            Debug.LogWarning("[CombatSystemController] playerCombatAction is not assigned.", this);
            hasReferences = false;
        }

        if (combatUIController == null)
        {
            Debug.LogWarning("[CombatSystemController] combatUIController is not assigned.", this);
            hasReferences = false;
        }

        if (ghostHeroPrefab == null)
        {
            Debug.LogWarning("[CombatSystemController] ghostHeroPrefab is not assigned.", this);
            hasReferences = false;
        }

        return hasReferences;
    }
}
