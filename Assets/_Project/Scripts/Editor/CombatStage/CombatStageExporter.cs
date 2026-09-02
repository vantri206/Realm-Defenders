using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CombatStageExporter
{
    public static void Export(CombatStageAuthoring authoring)
    {
        if (authoring == null ||
            !authoring.TryCreateStageData(out CombatMapData mapData, out List<EnemySpawnEventDefinition> spawnEvents))
        {
            return;
        }

        CombatStageDefinition definition = authoring.OutputDefinition;
        if (definition == null)
        {
            string definitionPath = EditorUtility.SaveFilePanelInProject(
                "Create Combat Stage Definition",
                GetSafeFileName(authoring.StageId) + "_Stage",
                "asset",
                "Select where to save the combat stage definition.");

            if (string.IsNullOrEmpty(definitionPath))
            {
                return;
            }

            definition = ScriptableObject.CreateInstance<CombatStageDefinition>();
            AssetDatabase.CreateAsset(definition, definitionPath);
            Undo.RecordObject(authoring, "Assign Combat Stage Definition");
            authoring.SetOutputDefinition(definition);
            EditorUtility.SetDirty(authoring);
        }

        string mapPrefabPath = GetMapPrefabPath(definition, authoring.StageId);
        GameObject mapPrefabRoot = PrefabUtility.SaveAsPrefabAsset(authoring.MapView.gameObject, mapPrefabPath);
        if (mapPrefabRoot == null || !mapPrefabRoot.TryGetComponent(out CombatMapView mapPrefab))
        {
            Debug.LogError("[CombatStageExporter] Failed to export CombatMapView prefab.", authoring);
            return;
        }

        Undo.RecordObject(definition, "Export Combat Stage Definition");
        definition.SetData(
            authoring.StageId,
            authoring.StageName,
            mapPrefab,
            mapData,
            authoring.StartConfig,
            spawnEvents);
        CombatStagePreviewExporter.GenerateMapPreview(authoring, definition);

        EditorUtility.SetDirty(definition);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[CombatStageExporter] Exported stage '{authoring.StageId}' to '{AssetDatabase.GetAssetPath(definition)}'.", definition);
    }

    private static string GetMapPrefabPath(CombatStageDefinition definition, string stageId)
    {
        if (definition.MapPrefab != null)
        {
            string existingPath = AssetDatabase.GetAssetPath(definition.MapPrefab);
            if (!string.IsNullOrEmpty(existingPath))
            {
                return existingPath;
            }
        }

        string definitionPath = AssetDatabase.GetAssetPath(definition);
        string directory = Path.GetDirectoryName(definitionPath);
        if (!string.IsNullOrEmpty(directory))
        {
            directory = directory.Replace('\\', '/');
        }

        return $"{directory}/{GetSafeFileName(stageId)}_Map.prefab";
    }

    private static string GetSafeFileName(string value)
    {
        string fileName = string.IsNullOrWhiteSpace(value) ? "CombatStage" : value.Trim();
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '_');
        }

        return fileName;
    }
}
