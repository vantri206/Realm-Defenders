using UnityEngine;

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
    [SerializeField] private TileOverlayRenderer tileOverlayRenderer;
    [SerializeField] private PlayerCombatAction playerCombatAction;
    [SerializeField] private CombatUIController combatUIController;
    [SerializeField] private GhostHeroView ghostHeroView;

    private void Awake()
    {
        InitializeCombatSystem();
    }

    public void InitializeCombatSystem()
    {
        combatGrid?.BuildGridMap();
        heroPlacement?.Initialize(combatGrid);
        heroDeploymentSystem?.Initialize(heroInventory, heroPlacement);
        playerCombatAction?.Initialize(mainCamera, combatGrid, heroDeploymentSystem, tileOverlayRenderer, ghostHeroView);
        playerCombatAction?.ChangeMode(PlayerCombatActionMode.None);

        heroInventoryView?.Initialize();

        combatUIController?.Initialize(playerCombatAction, heroInventoryView);
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
