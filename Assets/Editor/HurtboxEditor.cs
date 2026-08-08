using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Hurtbox))]
public class HurtboxEditor : Editor
{
    private SerializedProperty damageableMBProperty;
    private SerializedProperty scriptProperty;

    private void OnEnable()
    {
        scriptProperty = serializedObject.FindProperty("m_Script");
        damageableMBProperty = serializedObject.FindProperty("damageableMB");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(scriptProperty);
        }

        DrawPropertiesExcluding(serializedObject, "m_Script", "damageableMB");
        DrawDamageableField();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDamageableField()
    {
        EditorGUILayout.Space();

        MonoBehaviour currentDamageable = damageableMBProperty.objectReferenceValue as MonoBehaviour;
        Object pickedObject = EditorGUILayout.ObjectField(
            new GUIContent("Damageable"),
            currentDamageable,
            typeof(Object),
            true);

        if (pickedObject != currentDamageable)
        {
            AssignDamageable(pickedObject);
            currentDamageable = damageableMBProperty.objectReferenceValue as MonoBehaviour;
        }

        if (currentDamageable == null)
        {
            EditorGUILayout.HelpBox("Drop a GameObject or component that contains an IDamageable component.", MessageType.Info);
            return;
        }

        if (currentDamageable is not IDamageable)
        {
            EditorGUILayout.HelpBox($"{currentDamageable.name} does not implement IDamageable.", MessageType.Error);
            return;
        }

        DrawDamageablePicker(currentDamageable.gameObject, currentDamageable);
    }

    private void AssignDamageable(Object pickedObject)
    {
        if (pickedObject == null)
        {
            damageableMBProperty.objectReferenceValue = null;
            return;
        }

        if (pickedObject is MonoBehaviour monoBehaviour && monoBehaviour is IDamageable)
        {
            damageableMBProperty.objectReferenceValue = monoBehaviour;
            return;
        }

        GameObject pickedGameObject = null;

        if (pickedObject is GameObject gameObject)
        {
            pickedGameObject = gameObject;
        }
        else if (pickedObject is Component component)
        {
            pickedGameObject = component.gameObject;
        }

        if (pickedGameObject == null)
        {
            damageableMBProperty.objectReferenceValue = null;
            return;
        }

        List<MonoBehaviour> candidates = GetDamageableComponents(pickedGameObject);
        damageableMBProperty.objectReferenceValue = candidates.Count > 0 ? candidates[0] : null;
    }

    private void DrawDamageablePicker(GameObject sourceObject, MonoBehaviour currentDamageable)
    {
        List<MonoBehaviour> candidates = GetDamageableComponents(sourceObject);

        if (candidates.Count <= 1)
        {
            return;
        }

        string[] labels = new string[candidates.Count];
        int currentIndex = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            labels[i] = $"{candidates[i].gameObject.name} / {candidates[i].GetType().Name}";

            if (candidates[i] == currentDamageable)
            {
                currentIndex = i;
            }
        }

        int selectedIndex = EditorGUILayout.Popup("Damageable Component", currentIndex, labels);

        if (selectedIndex != currentIndex)
        {
            damageableMBProperty.objectReferenceValue = candidates[selectedIndex];
        }
    }

    private static List<MonoBehaviour> GetDamageableComponents(GameObject sourceObject)
    {
        List<MonoBehaviour> candidates = new();

        if (sourceObject == null)
        {
            return candidates;
        }

        for (Transform current = sourceObject.transform; current != null; current = current.parent)
        {
            MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];

                if (behaviour != null && behaviour is IDamageable)
                {
                    candidates.Add(behaviour);
                }
            }
        }

        return candidates;
    }
}
