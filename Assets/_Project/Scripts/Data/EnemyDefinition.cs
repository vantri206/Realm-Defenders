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
    [SerializeField] private UnitMovementType movementType = UnitMovementType.Ground;
    [SerializeField] private int foodReward = 2;

    [Header("Attack")]
    [SerializeField] private UnitAttackType attackType = UnitAttackType.Melee;
    [SerializeField] private TargetPriorityMode targetPriorityMode = TargetPriorityMode.Nearest;
    [SerializeField] private List<Vector2Int> attackPattern = new List<Vector2Int>
    {
        Vector2Int.zero,
    };

    [SerializeField] private TargetSide targetSide = TargetSide.Enemy;
    [SerializeField] private AttackEffect attackEffect = AttackEffect.Damage;
    [SerializeField] private AttackMethod attackMethod;
    [SerializeField] private AttackDamageType attackDamageType;
    [SerializeField] private float normalAttackEffectMultiplier = 1f;
    [SerializeField] private AttackProjectile normalAttackProjectilePrefab;
    [SerializeField] private AttackAOEHit normalAttackAOEHitPrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX normalAttackHitVFXPrefab;

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
    public UnitMovementType MovementType => movementType;
    public int FoodReward => foodReward;
    public UnitAttackType AttackType => attackType;
    public TargetPriorityMode TargetPriorityMode => targetPriorityMode;
    public IReadOnlyList<Vector2Int> AttackPattern => attackPattern;
    public TargetSide TargetSide => targetSide;
    public AttackEffect AttackEffect => attackEffect;
    public AttackMethod AttackMethod => attackMethod;
    public AttackDamageType AttackDamageType => attackDamageType;
    public float NormalAttackEffectMultiplier => normalAttackEffectMultiplier;
    public AttackProjectile NormalAttackProjectilePrefab => normalAttackProjectilePrefab;
    public AttackAOEHit NormalAttackAOEHitPrefab => normalAttackAOEHitPrefab;
    public SimpleSpriteAnimatorVFX NormalAttackHitVFXPrefab => normalAttackHitVFXPrefab;
    public Vector2 CenterOffset => centerOffset;

    public bool IsValid => enemyPrefab != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        attack = Mathf.Max(0f, attack);
        attackInterval = Mathf.Max(0f, attackInterval);
        normalAttackEffectMultiplier = Mathf.Max(0f, normalAttackEffectMultiplier);
        moveSpeed = Mathf.Max(0f, moveSpeed);
    }
#endif
}
