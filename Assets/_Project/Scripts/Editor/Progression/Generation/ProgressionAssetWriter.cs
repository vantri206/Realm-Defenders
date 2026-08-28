using System.IO;
using UnityEditor;
using UnityEngine;

internal static class ProgressionAssetWriter
{
    public static bool TryWriteExperienceTable(
        DefaultAsset outputFolder,
        string outputName,
        int[] experienceThresholds,
        out ExperienceProgressionTable table,
        out string error)
    {
        if (!TryLoadOrCreateAsset(outputFolder, outputName, out table, out string assetPath, out error))
        {
            return false;
        }

        Undo.RecordObject(table, "Generate Experience Progression Table");
        var serializedTable = new SerializedObject(table);
        SerializedProperty thresholdsProperty = serializedTable.FindProperty("experienceThresholds");
        thresholdsProperty.arraySize = experienceThresholds.Length;

        for (int index = 0; index < experienceThresholds.Length; index++)
        {
            thresholdsProperty.GetArrayElementAtIndex(index).intValue = experienceThresholds[index];
        }

        serializedTable.ApplyModifiedPropertiesWithoutUndo();
        SaveGeneratedAsset(table, assetPath);
        error = null;
        return true;
    }

    public static bool TryWriteUnitStatTable(
        DefaultAsset outputFolder,
        string outputName,
        UnitBaseStats[] statsByLevel,
        ScriptableObject definition,
        out UnitStatProgressionTable table,
        out string error)
    {
        if (!TryLoadOrCreateAsset(outputFolder, outputName, out table, out string assetPath, out error))
        {
            return false;
        }

        Undo.RecordObject(table, "Generate Unit Stat Progression Table");
        var serializedTable = new SerializedObject(table);
        SerializedProperty statsProperty = serializedTable.FindProperty("statsByLevel");
        statsProperty.arraySize = statsByLevel.Length;

        for (int index = 0; index < statsByLevel.Length; index++)
        {
            SerializedProperty row = statsProperty.GetArrayElementAtIndex(index);
            UnitBaseStats stats = statsByLevel[index];
            row.FindPropertyRelative("maxHealth").floatValue = stats.MaxHealth;
            row.FindPropertyRelative("attack").floatValue = stats.Attack;
            row.FindPropertyRelative("attackInterval").floatValue = stats.AttackInterval;
            row.FindPropertyRelative("defense").floatValue = stats.Defense;
            row.FindPropertyRelative("specialDefense").floatValue = stats.SpecialDefense;
            row.FindPropertyRelative("moveSpeed").floatValue = stats.MoveSpeed;
            row.FindPropertyRelative("blockCount").intValue = stats.BlockCount;
        }

        serializedTable.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);

        var serializedDefinition = new SerializedObject(definition);
        SerializedProperty tableProperty = serializedDefinition.FindProperty("statProgressionTable");
        if (tableProperty == null)
        {
            error = $"{definition.GetType().Name} does not contain a statProgressionTable field.";
            return false;
        }

        Undo.RecordObject(definition, "Assign Unit Stat Progression Table");
        tableProperty.objectReferenceValue = table;
        serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);

        SaveGeneratedAsset(table, assetPath);
        error = null;
        return true;
    }

    private static bool TryLoadOrCreateAsset<T>(
        DefaultAsset outputFolder,
        string outputName,
        out T asset,
        out string assetPath,
        out string error)
        where T : ScriptableObject
    {
        asset = null;
        assetPath = null;

        string folderPath = outputFolder != null ? AssetDatabase.GetAssetPath(outputFolder) : null;
        if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            error = "Drag a valid project folder into Output Folder.";
            return false;
        }

        string sanitizedName = SanitizeFileName(outputName);
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            error = "Output Name is required.";
            return false;
        }

        assetPath = $"{folderPath}/{sanitizedName}.asset";
        Object existingAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
        if (existingAsset != null && !(existingAsset is T))
        {
            error = $"An asset with a different type already exists at {assetPath}.";
            return false;
        }

        asset = existingAsset as T;
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            Undo.RegisterCreatedObjectUndo(asset, $"Create {typeof(T).Name}");
        }

        error = null;
        return true;
    }

    private static string SanitizeFileName(string outputName)
    {
        string fileName;
        if (outputName != null)
        {
            fileName = outputName.Trim();
        }
        else
        {
            fileName = string.Empty;
        }
        if (fileName.EndsWith(".asset"))
        {
            fileName = fileName.Substring(0, fileName.Length - ".asset".Length);
        }

        char[] invalidCharacters = Path.GetInvalidFileNameChars();
        for (int index = 0; index < invalidCharacters.Length; index++)
        {
            fileName = fileName.Replace(invalidCharacters[index], '_');
        }

        return fileName;
    }

    private static void SaveGeneratedAsset(Object asset, string assetPath)
    {
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(assetPath);
        Selection.activeObject = asset;
        EditorGUIUtility.PingObject(asset);
    }
}
