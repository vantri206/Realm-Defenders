using UnityEngine;

public class CombatBootstrapper : MonoBehaviour
{
    [Header("Direct Test")]
    [SerializeField] private CombatStageTestSetup stageTestSetup;

    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CombatGrid combatGrid;
    [SerializeField] private HeroSquad heroSquad;
    [SerializeField] private HeroSquadView heroSquadView;
    [SerializeField] private HeroPlacement heroPlacement;
    [SerializeField] private HeroDeploymentSystem heroDeploymentSystem;
    [SerializeField] private HeroDetailView heroDetailView;
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
    private CombatMapView runtimeMapView;
    private bool isInitialized;

    public bool IsInitialized => isInitialized;

    private void Start()
    {
        if (isInitialized)
        {
            return;
        }

        PlayerSession playerSession = PlayerSession.Current;
        if (playerSession != null && playerSession.SelectedStage != null)
        {
            InitializeSessionCombat(playerSession);
            return;
        }

        if (stageTestSetup != null && stageTestSetup.TryCreateBootstrapData(out CombatBootstrapData bootstrapData))
        {
            InitializeCombat(bootstrapData);
            return;
        }

        Debug.LogError("[CombatBootstrapper] No selected runtime stage or direct test setup was provided.", this);
    }

    private void OnDestroy()
    {
        if (enemyWaveController != null)
        {
            enemyWaveController.OnWaveResolved -= HandleWaveResolved;
        }

        if (stageSystem != null)
        {
            stageSystem.OnStageEnded -= HandleStageEnded;
        }
    }

    public bool InitializeCombat(CombatBootstrapData bootstrapData)
    {
        if (isInitialized)
        {
            Debug.LogWarning("[CombatBootstrapper] Combat is already initialized.", this);
            return false;
        }

        if (bootstrapData == null || !bootstrapData.IsValid)
        {
            Debug.LogError("[CombatBootstrapper] Valid CombatBootstrapData is required.", this);
            return false;
        }

        if (!CheckReferences())
        {
            return false;
        }

        ghostHero = CreateGhostHeroView();
        if (ghostHero == null)
        {
            return false;
        }

        if (!combatGrid.BuildGridMap(bootstrapData.MapView.Grid, bootstrapData.MapData.GridCells))
        {
            return false;
        }

        pathfindingSystem.BuildCostGrid(combatGrid.Cells);
        if (!enemyRouteGraph.InitializeRoutes(combatGrid, bootstrapData.MapData.Routes))
        {
            return false;
        }

        UnitCombatContext combatContext = new UnitCombatContext(combatGrid, pathfindingSystem, combatTime);
        stageSystem.Initialize(bootstrapData.StartConfig, GetTotalSpawnCount(bootstrapData), combatTime);
        if (!stageSystem.IsInitialized)
        {
            return false;
        }
        stageSystem.OnStageEnded += HandleStageEnded;

        heroSquad.Initialize(heroSquadView, stageSystem);
        AddSquadHeroes(bootstrapData);
        heroPlacement.Initialize(combatContext);
        heroDeploymentSystem.Initialize(heroSquad, heroPlacement, combatTime);

        enemyWaveController.Initialize(
            combatContext,
            enemyRouteGraph,
            stageSystem,
            bootstrapData.MapData,
            bootstrapData.SpawnEvents);
        if (!enemyWaveController.IsInitialized)
        {
            return false;
        }

        enemyWaveController.OnWaveResolved += HandleWaveResolved;

        TileOverlayRenderer tileOverlayRenderer = bootstrapData.MapView.TileOverlayRenderer;
        playerCombatAction.Initialize(
            mainCamera,
            combatGrid,
            heroDeploymentSystem,
            heroDetailView,
            tileOverlayRenderer,
            ghostHero,
            stageSystem,
            combatTime);
        playerCombatAction.ChangeMode(PlayerCombatActionMode.None);
        combatUIController.Initialize(playerCombatAction, heroSquadView);
        stageHUDController.Initialize(bootstrapData.StageId, stageSystem, playerCombatAction, combatUIController, combatTime);

        isInitialized = true;
        enemyWaveController.StartWave();
        return true;
    }

    private void InitializeSessionCombat(PlayerSession playerSession)
    {
        CombatStageDefinition stageDefinition = playerSession.SelectedStage;
        if (stageDefinition == null || stageDefinition.MapPrefab == null || playerSession.HeroRoster == null)
        {
            Debug.LogError("[CombatBootstrapper] Selected stage, stage map prefab, and player roster are required for runtime combat.", this);
            return;
        }

        DisableAuthoringMapViews();

        runtimeMapView = Instantiate(stageDefinition.MapPrefab);
        CombatBootstrapData bootstrapData = new CombatBootstrapData(stageDefinition, runtimeMapView, playerSession.HeroRoster.Heroes);
        if (!InitializeCombat(bootstrapData))
        {
            Destroy(runtimeMapView.gameObject);
            runtimeMapView = null;
        }
    }

    private void DisableAuthoringMapViews()
    {
        CombatMapView[] sceneMapViews = FindObjectsByType<CombatMapView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sceneMapViews.Length; i++)
        {
            CombatMapView mapView = sceneMapViews[i];
            if (mapView != null && mapView.gameObject.scene == gameObject.scene)
            {
                mapView.gameObject.SetActive(false);
            }
        }
    }

    private void AddSquadHeroes(CombatBootstrapData bootstrapData)
    {
        if (bootstrapData.PlayerSquad == null)
        {
            return;
        }

        for (int i = 0; i < bootstrapData.PlayerSquad.Count; i++)
        {
            heroSquad.AddHeroInstance(bootstrapData.PlayerSquad[i]);
        }
    }

    private int GetTotalSpawnCount(CombatBootstrapData bootstrapData)
    {
        int totalSpawnCount = 0;
        for (int i = 0; i < bootstrapData.SpawnEvents.Count; i++)
        {
            EnemySpawnEventDefinition spawnEvent = bootstrapData.SpawnEvents[i];
            if (spawnEvent != null)
            {
                totalSpawnCount += Mathf.Max(0, spawnEvent.GroupCount) * Mathf.Max(0, spawnEvent.EnemiesPerGroup);
            }
        }

        return totalSpawnCount;
    }

    private void HandleWaveResolved()
    {
        stageSystem.NotifyWaveResolved();
    }

    private void HandleStageEnded(CombatStageResult result)
    {
        enemyWaveController.StopWave();
        combatTime.PauseCombat();
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

        if (heroSquad == null || heroSquadView == null || heroPlacement == null || heroDeploymentSystem == null || heroDetailView == null)
        {
            Debug.LogWarning("[CombatBootstrapper] Hero system references are not fully assigned.", this);
            hasReferences = false;
        }

        if (playerCombatAction == null || combatUIController == null || stageHUDController == null)
        {
            Debug.LogWarning("[CombatBootstrapper] Combat UI references are not fully assigned.", this);
            hasReferences = false;
        }

        if (ghostHeroPrefab == null)
        {
            Debug.LogWarning("[CombatBootstrapper] ghostHeroPrefab is not assigned.", this);
            hasReferences = false;
        }

        if (enemyRouteGraph == null || pathfindingSystem == null || enemyWaveController == null)
        {
            Debug.LogWarning("[CombatBootstrapper] Enemy system references are not fully assigned.", this);
            hasReferences = false;
        }

        if (stageSystem == null || combatTime == null)
        {
            Debug.LogWarning("[CombatBootstrapper] Stage system references are not fully assigned.", this);
            hasReferences = false;
        }

        return hasReferences;
    }
}
