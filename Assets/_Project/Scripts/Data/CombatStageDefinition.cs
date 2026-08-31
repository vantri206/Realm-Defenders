using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CombatStageDefinition", menuName = "Scriptable Objects/Stage/CombatStageDefinition")]
public class CombatStageDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string stageId;
    [SerializeField] private string stageName;

    [Header("Map")]
    [SerializeField] private CombatMapView mapPrefab;
    [SerializeField] private CombatMapData mapData = new CombatMapData();

    [Header("Stage Config")]
    [SerializeField] private CombatStageStartConfig startConfig = new CombatStageStartConfig();

    [Header("Spawn Events")]
    [SerializeField] private List<EnemySpawnEventDefinition> spawnEvents = new List<EnemySpawnEventDefinition>();

    public string StageId => stageId;
    public string StageName => stageName;
    public CombatMapView MapPrefab => mapPrefab;
    public CombatMapData MapData => mapData;
    public CombatStageStartConfig StartConfig => startConfig;
    public IReadOnlyList<EnemySpawnEventDefinition> SpawnEvents => spawnEvents;

#if UNITY_EDITOR
    public void SetData(string stageId, string stageName, CombatMapView mapPrefab, CombatMapData mapData, CombatStageStartConfig startConfig, List<EnemySpawnEventDefinition> spawnEvents)
    {
        this.stageId = stageId;
        this.stageName = stageName;
        this.mapPrefab = mapPrefab;

        if (mapData != null)
        {
            this.mapData = mapData;
        }
        else
        {
            this.mapData = new CombatMapData();
        }

        if (startConfig != null)
        {
            this.startConfig = new CombatStageStartConfig(startConfig);
        }
        else
        {
            this.startConfig = new CombatStageStartConfig();
        }

        if (spawnEvents != null)
        {
            this.spawnEvents = spawnEvents;
        }
        else
        {
            this.spawnEvents = new List<EnemySpawnEventDefinition>();
        }
    }
#endif
}
