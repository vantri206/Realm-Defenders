using System.Collections.Generic;

public class CombatBootstrapData
{
    public string StageId { get; }
    public string StageName { get; }
    public CombatMapView MapView { get; }
    public CombatMapData MapData { get; }
    public CombatStageStartConfig StartConfig { get; }
    public IReadOnlyList<EnemySpawnEventDefinition> SpawnEvents { get; }
    public IReadOnlyList<HeroInstance> PlayerSquad { get; }

    public bool IsValid => MapView != null && MapView.Grid != null && MapView.TileOverlayRenderer != null && 
                        MapData != null && StartConfig != null && SpawnEvents != null;

    public CombatBootstrapData(string stageId, string stageName, CombatMapView mapView, CombatMapData mapData,
                            CombatStageStartConfig startConfig, IReadOnlyList<EnemySpawnEventDefinition> spawnEvents, IReadOnlyList<HeroInstance> playerSquad)
    {
        StageId = stageId;
        StageName = stageName;
        MapView = mapView;
        MapData = mapData;
        StartConfig = startConfig;
        SpawnEvents = spawnEvents;
        PlayerSquad = playerSquad;
    }

    public CombatBootstrapData(CombatStageDefinition stageDefinition, CombatMapView mapView, IReadOnlyList<HeroInstance> playerSquad)
        : this(stageDefinition.StageId, stageDefinition.StageName, mapView, stageDefinition.MapData, stageDefinition.StartConfig, stageDefinition.SpawnEvents, playerSquad)
    {
    }
}
