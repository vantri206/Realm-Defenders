using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyDefinition", menuName = "Scriptable Objects/EnemyDefinition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string enemyId;
    [SerializeField] private string enemyName;
    [SerializeField] private Sprite enemySprite;
    [SerializeField] private Sprite enemyIcon;
    [SerializeField] private string enemyDescription;
    [SerializeField] private AnimatorOverrideController enemyAnimator;
    [SerializeField] private EnemyRuntime enemyPrefab;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attack = 10f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float defense = 0f;
    [SerializeField] private float specialDefense = 0f;
    [SerializeField] private float moveSpeed = 1f;

    [Header("Attack")]
    [SerializeField] private UnitAttackType attackType = UnitAttackType.Melee;
    [SerializeField] private TargetPriorityMode targetPriorityMode = TargetPriorityMode.Nearest;
    [SerializeField] private List<Vector2Int> attackPattern = new List<Vector2Int>
    {
        Vector2Int.zero,
    };

    [SerializeField] private AttackMethod attackMethod;
    [SerializeField] private AttackDamageType attackDamageType;
    [SerializeField] private float normalAttackDamageMultiplier = 1f;

    [Header("Customization")]
    [SerializeField] private Vector2 centerOffset = new Vector2(0f, 0.5f);

    public string EnemyId => enemyId;
    public string EnemyName => enemyName;
    public Sprite EnemySprite => enemySprite;
    public Sprite EnemyIcon => enemyIcon;
    public string EnemyDescription => enemyDescription;
    public AnimatorOverrideController AnimatorController => enemyAnimator;
    public EnemyRuntime Prefab => enemyPrefab;
    public float MaxHealth => maxHealth;
    public float Attack => attack;
    public float AttackInterval => attackInterval;
    public float Defense => defense;
    public float SpecialDefense => specialDefense;
    public float MoveSpeed => moveSpeed;
    public UnitAttackType AttackType => attackType;
    public TargetPriorityMode TargetPriorityMode => targetPriorityMode;
    public IReadOnlyList<Vector2Int> AttackPattern => attackPattern;
    public AttackMethod AttackMethod => attackMethod;
    public AttackDamageType AttackDamageType => attackDamageType;
    public float NormalAttackDamageMultiplier => normalAttackDamageMultiplier;
    public Vector2 CenterOffset => centerOffset;

    public bool IsValid => enemyPrefab != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        attack = Mathf.Max(0f, attack);
        attackInterval = Mathf.Max(0f, attackInterval);
        normalAttackDamageMultiplier = Mathf.Max(0f, normalAttackDamageMultiplier);
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
#endif
}
