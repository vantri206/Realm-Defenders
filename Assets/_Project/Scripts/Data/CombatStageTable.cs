using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CombatStageTable", menuName = "Scriptable Objects/Stage/CombatStageTable")]
public class CombatStageTable : ScriptableObject
{
    [SerializeField] private List<CombatStageDefinition> stages = new List<CombatStageDefinition>();

    public IReadOnlyList<CombatStageDefinition> Stages => stages;
    public int StageCount => stages != null ? stages.Count : 0;

    public bool TryGetStage(string stageId, out CombatStageDefinition stage)
    {
        stage = null;

        if (string.IsNullOrEmpty(stageId) || stages == null)
        {
            return false;
        }

        for (int i = 0; i < stages.Count; i++)
        {
            CombatStageDefinition currentStage = stages[i];
            if (currentStage != null && currentStage.StageId == stageId)
            {
                stage = currentStage;
                return true;
            }
        }

        return false;
    }
}
