using UnityEngine;
using UnityEngine.Serialization;

public class CombatBootstrapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CombatGrid combatGrid;
    [SerializeField] private HeroSquad heroSquad;
    [SerializeField] private HeroSquadView heroSquadView;
    [SerializeField] private HeroPlacement heroPlacement;
    [SerializeField] private HeroDeploymentSystem heroDeploymentSystem;
    [SerializeField] private HeroDetailView heroDetailView;
    [SerializeField] private TileOverlayRenderer tileOverlayRenderer;
    [SerializeField] private PlayerCombatAction playerCombatAction;
    [SerializeField] private CombatUIController combatUIController;
    [SerializeField] private CombatStageHUDController stageHUDController;
    [SerializeField] private GhostHeroView ghostHeroPrefab;
    [SerializeField] private EnemyRouteGraph enemyRouteGraph;
    [SerializeField] private UnitPathfindingSystem pathfindingSystem;
    [SerializeField] private EnemyWaveController enemyWaveController;
    [SerializeField] private StageSystem stageSystem;
    [SerializeField] private CombatTimeController combatTime;

    private GhostHeroView ghostHero;
    private UnitCombatContext combatContext;

    private void Awake()
    {
        InitializeCombatBootstrapper();
    }

    public void InitializeCombatBootstrapper()
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

        combatContext = new UnitCombatContext(combatGrid, pathfindingSystem, combatTime);

        // Initialize grid map and pathfinding systems
        combatGrid.BuildGridMap();
        pathfindingSystem.BuildCostGrid(combatGrid.Cells);
        enemyRouteGraph.InitializeRoutes(combatGrid);

        // Initialize hero systems
        heroPlacement.Initialize(combatContext);
        heroDeploymentSystem.Initialize(heroSquad, heroPlacement, combatTime);
        heroSquad.Initialize(heroSquadView, stageSystem);

        // Initialize enemy systems
        enemyWaveController.Initialize(combatContext, enemyRouteGraph, stageSystem);

        // Initialize player input action and UI controller
        playerCombatAction.Initialize(mainCamera, combatGrid, heroDeploymentSystem, heroDetailView, tileOverlayRenderer, ghostHero, stageSystem, combatTime);
        playerCombatAction.ChangeMode(PlayerCombatActionMode.None);
        combatUIController.Initialize(playerCombatAction, heroSquadView);
        stageHUDController.Initialize(stageSystem, playerCombatAction, combatTime);

        // Initialize stage system
        stageSystem.Initialize(stageSystem.StartingMeat, stageSystem.StartingLives, enemyWaveController.TotalSpawnCount, combatTime);


        StartCombat();
    }

    private void StartCombat()
    {
        enemyWaveController.StartWave();
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
            Debug.LogWarning("[CombatBootstrapper] mainCamera is not assigned.", this);
            hasReferences = false;
        }

        if (combatGrid == null)
        {
            Debug.LogWarning("[CombatBootstrapper] combatGrid is not assigned.", this);
            hasReferences = false;
        }

        if (heroSquad == null)
        {
            Debug.LogWarning("[CombatBootstrapper] heroSquad is not assigned.", this);
            hasReferences = false;
        }

        if (heroSquadView == null)
        {
            Debug.LogWarning("[CombatBootstrapper] heroSquadView is not assigned.", this);
            hasReferences = false;
        }

        if (heroPlacement == null)
        {
            Debug.LogWarning("[CombatBootstrapper] heroPlacement is not assigned.", this);
            hasReferences = false;
        }

        if (heroDeploymentSystem == null)
        {
            Debug.LogWarning("[CombatBootstrapper] heroDeploymentSystem is not assigned.", this);
            hasReferences = false;
        }

        if (heroDetailView == null)
        {
            Debug.LogWarning("[CombatBootstrapper] heroDetailView is not assigned.", this);
            hasReferences = false;
        }

        if (tileOverlayRenderer == null)
        {
            Debug.LogWarning("[CombatBootstrapper] tileOverlayRenderer is not assigned.", this);
            hasReferences = false;
        }

        if (playerCombatAction == null)
        {
            Debug.LogWarning("[CombatBootstrapper] playerCombatAction is not assigned.", this);
            hasReferences = false;
        }

        if (combatUIController == null)
        {
            Debug.LogWarning("[CombatBootstrapper] combatUIController is not assigned.", this);
            hasReferences = false;
        }

        if (stageHUDController == null)
        {
            Debug.LogWarning("[CombatBootstrapper] StageHUDController is not assigned.", this);
            hasReferences = false;
        }

        if (ghostHeroPrefab == null)
        {
            Debug.LogWarning("[CombatBootstrapper] ghostHeroPrefab is not assigned.", this);
            hasReferences = false;
        }

        if (enemyRouteGraph == null)
        {
            Debug.LogWarning("[CombatBootstrapper] enemyRouteGraph is not assigned.", this);
            hasReferences = false;
        }

        if (pathfindingSystem == null)
        {
            Debug.LogWarning("[CombatBootstrapper] pathfindingSystem is not assigned.", this);
            hasReferences = false;
        }

        if (enemyWaveController == null)
        {
            Debug.LogWarning("[CombatBootstrapper] enemyWaveController is not assigned.", this);
            hasReferences = false;
        }

        if (stageSystem == null)
        {
            Debug.LogWarning("[CombatBootstrapper] StageSystem is not assigned.", this);
            hasReferences = false;
        }

        if (combatTime == null)
        {
            Debug.LogWarning("[CombatBootstrapper] combatTime is not assigned.", this);
            hasReferences = false;
        }

        return hasReferences;
    }
}
