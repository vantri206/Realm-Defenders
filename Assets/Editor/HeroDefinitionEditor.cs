using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HeroDefinition))]
public sealed class HeroDefinitionEditor : Editor
{
    private SerializedProperty heroIdProperty;
    private SerializedProperty heroNameProperty;
    private SerializedProperty defaultSpriteProperty;
    private SerializedProperty iconProperty;
    private SerializedProperty displaySpriteProperty;
    private SerializedProperty heroClassProperty;
    private SerializedProperty descriptionProperty;
    private SerializedProperty animatorProperty;
    private SerializedProperty prefabProperty;
    private SerializedProperty maxHealthProperty;
    private SerializedProperty attackProperty;
    private SerializedProperty attackIntervalProperty;
    private SerializedProperty defenseProperty;
    private SerializedProperty specialDefenseProperty;
    private SerializedProperty blockProperty;
    private SerializedProperty movementTypeProperty;
    private SerializedProperty moveSpeedProperty;
    private SerializedProperty statProgressionTableProperty;
    private SerializedProperty normalAttackDefinitionProperty;
    private SerializedProperty passiveSkillProperty;
    private SerializedProperty activeSkillProperty;
    private SerializedProperty baseDeployCostProperty;
    private SerializedProperty baseRedeployTimeProperty;

    private string prefabValidationMessage;

    private void OnEnable()
    {
        heroIdProperty = serializedObject.FindProperty("heroId");
        heroNameProperty = serializedObject.FindProperty("heroName");
        defaultSpriteProperty = serializedObject.FindProperty("heroDefaultSprite");
        iconProperty = serializedObject.FindProperty("heroIcon");
        displaySpriteProperty = serializedObject.FindProperty("heroDisplaySprite");
        heroClassProperty = serializedObject.FindProperty("heroClass");
        descriptionProperty = serializedObject.FindProperty("heroDescription");
        animatorProperty = serializedObject.FindProperty("heroAnimator");
        prefabProperty = serializedObject.FindProperty("heroPrefab");
        maxHealthProperty = serializedObject.FindProperty("maxHealth");
        attackProperty = serializedObject.FindProperty("attack");
        attackIntervalProperty = serializedObject.FindProperty("attackInterval");
        defenseProperty = serializedObject.FindProperty("defense");
        specialDefenseProperty = serializedObject.FindProperty("specialDefense");
        blockProperty = serializedObject.FindProperty("blockCount");
        movementTypeProperty = serializedObject.FindProperty("movementType");
        moveSpeedProperty = serializedObject.FindProperty("moveSpeed");
        statProgressionTableProperty = serializedObject.FindProperty("statProgressionTable");
        normalAttackDefinitionProperty = serializedObject.FindProperty("normalAttackDefinition");
        passiveSkillProperty = serializedObject.FindProperty("passiveSkill");
        activeSkillProperty = serializedObject.FindProperty("activeSkill");
        baseDeployCostProperty = serializedObject.FindProperty("baseDeployCost");
        baseRedeployTimeProperty = serializedObject.FindProperty("baseRedeployTime");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawIdentitySection();
        EditorGUILayout.Space(8f);
        DrawStatsSection();
        EditorGUILayout.Space(8f);
        DrawAttackSection();
        EditorGUILayout.Space(8f);
        DrawSkillsSection();
        EditorGUILayout.Space(8f);
        DrawDeployStatsSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentitySection()
    {
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(heroIdProperty, new GUIContent("Hero ID"));
        EditorGUILayout.PropertyField(heroNameProperty, new GUIContent("Hero Name"));
        EditorGUILayout.PropertyField(defaultSpriteProperty, new GUIContent("Hero Default Sprite"));
        EditorGUILayout.PropertyField(iconProperty, new GUIContent("Hero Icon"));
        EditorGUILayout.PropertyField(displaySpriteProperty, new GUIContent("Hero Display Sprite"));
        EditorGUILayout.PropertyField(heroClassProperty, new GUIContent("Hero Class"));
        EditorGUILayout.PropertyField(descriptionProperty, new GUIContent("Description"));
        EditorGUILayout.PropertyField(animatorProperty, new GUIContent("Animator Override"));
        DrawHeroPrefabField();
        EditorGUILayout.EndVertical();
    }

    private void DrawHeroPrefabField()
    {
        HeroRuntime currentPrefab = prefabProperty.objectReferenceValue as HeroRuntime;
        GameObject currentPrefabObject = currentPrefab != null ? currentPrefab.gameObject : null;

        EditorGUI.BeginChangeCheck();
        GameObject selectedPrefabObject = EditorGUILayout.ObjectField(
            new GUIContent("Hero Prefab"),
            currentPrefabObject,
            typeof(GameObject),
            false) as GameObject;

        if (EditorGUI.EndChangeCheck())
        {
            prefabValidationMessage = null;

            if (selectedPrefabObject == null)
            {
                prefabProperty.objectReferenceValue = null;
                return;
            }

            HeroRuntime selectedHeroRuntime = selectedPrefabObject.GetComponent<HeroRuntime>();
            if (selectedHeroRuntime == null)
            {
                prefabValidationMessage = "Selected prefab must have a HeroRuntime component.";
                return;
            }

            prefabProperty.objectReferenceValue = selectedHeroRuntime;
        }

        if (!string.IsNullOrEmpty(prefabValidationMessage))
        {
            EditorGUILayout.HelpBox(prefabValidationMessage, MessageType.Error);
        }
    }

    private void DrawStatsSection()
    {
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(maxHealthProperty, new GUIContent("Max Health"));
        EditorGUILayout.PropertyField(attackProperty);
        EditorGUILayout.PropertyField(attackIntervalProperty, new GUIContent("Attack Interval"));
        EditorGUILayout.PropertyField(defenseProperty);
        EditorGUILayout.PropertyField(specialDefenseProperty, new GUIContent("Special Defense"));
        EditorGUILayout.PropertyField(blockProperty, new GUIContent("Block Count"));
        EditorGUILayout.PropertyField(movementTypeProperty, new GUIContent("Movement Type"));
        EditorGUILayout.PropertyField(moveSpeedProperty, new GUIContent("Move Speed"));
        EditorGUILayout.PropertyField(statProgressionTableProperty, new GUIContent("Stat Progression"));
        EditorGUILayout.EndVertical();
    }

    private void DrawAttackSection()
    {
        EditorGUILayout.LabelField("Attack", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(normalAttackDefinitionProperty, new GUIContent("Normal Attack"));

        if (normalAttackDefinitionProperty.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("A NormalAttackDefinition is required.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDeployStatsSection()
    {
        EditorGUILayout.LabelField("Deploy Stats", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(baseDeployCostProperty, new GUIContent("Base Deploy Cost"));
        EditorGUILayout.PropertyField(baseRedeployTimeProperty, new GUIContent("Base Redeploy Time"));
        EditorGUILayout.EndVertical();
    }

    private void DrawSkillsSection()
    {
        EditorGUILayout.LabelField("Skills", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        DrawSkillField(passiveSkillProperty, "Passive Skill", SkillType.Passive);
        DrawSkillField(activeSkillProperty, "Active Skill", SkillType.Active);

        EditorGUILayout.EndVertical();
    }

    private static void DrawSkillField(SerializedProperty skillProperty, string label, SkillType expectedType)
    {
        EditorGUILayout.PropertyField(skillProperty, new GUIContent(label));

        SkillDefinition skillDefinition = skillProperty.objectReferenceValue as SkillDefinition;
        if (skillDefinition == null)
        {
            EditorGUILayout.HelpBox($"A {expectedType} SkillDefinition is required.", MessageType.Warning);
            return;
        }

        if (skillDefinition.SkillType != expectedType)
        {
            EditorGUILayout.HelpBox(
                $"{skillDefinition.name} is {skillDefinition.SkillType}. This slot requires a {expectedType} Skill.",
                MessageType.Error);
        }
    }
}
