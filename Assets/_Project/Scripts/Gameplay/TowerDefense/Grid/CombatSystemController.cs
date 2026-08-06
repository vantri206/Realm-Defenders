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
        ghostHero = CreateGhostHeroView();

        combatGrid?.BuildGridMap();
        heroPlacement?.Initialize(combatGrid);
        heroDeploymentSystem?.Initialize(heroInventory, heroPlacement);
        playerCombatAction?.Initialize(mainCamera, combatGrid, heroDeploymentSystem, heroDetailView, tileOverlayRenderer, ghostHero);
        playerCombatAction?.ChangeMode(PlayerCombatActionMode.None);

        heroInventoryView?.Initialize();

        combatUIController?.Initialize(playerCombatAction, heroInventoryView);
    }

    private GhostHeroView CreateGhostHeroView()
    {
        if (ghostHero != null)
        {
            return ghostHero;
        }

        if (ghostHeroPrefab == null)
        {
            Debug.LogWarning("[CombatSystemController] Ghost hero prefab is not assigned.");
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
}
