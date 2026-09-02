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
    [SerializeField, TextArea(3, 10)] private string enemyDescription;
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
    [SerializeField] private bool canBeBlocked = true;
    [SerializeField] private int meatReward = 2;

    [Header("Attack")]
    [SerializeField] private NormalAttackDefinition normalAttack;
    [SerializeField] private bool canAttackWhenNotBlocked = false;

    [Header("Passive")]
    [SerializeField] private float normalAttackDamageMultiplier = 1f;
    [SerializeField] private float normalAttackLifeSteal;
    [SerializeField] private EnemyDefinition deathReplacement;

    [Header("Customization")]
    [SerializeField] private Vector2 navigationOffset = new Vector2(0f, 0.5f);

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
    public NormalAttackDefinition NormalAttackDefinition => normalAttack;
    public bool CanAttackWhenNotBlocked => canAttackWhenNotBlocked;
    public UnitMovementType MovementType => movementType;
    public bool CanBeBlocked => canBeBlocked;
    public int MeatReward => meatReward;
    public float NormalAttackDamageMultiplier => normalAttackDamageMultiplier;
    public float NormalAttackLifeSteal => normalAttackLifeSteal;
    public EnemyDefinition DeathReplacement => deathReplacement;
    public Vector2 NavigationOffset => navigationOffset;

    public bool IsValid => enemyPrefab != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        attack = Mathf.Max(0f, attack);
        attackInterval = Mathf.Max(0f, attackInterval);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        normalAttackDamageMultiplier = Mathf.Max(0f, normalAttackDamageMultiplier);
        normalAttackLifeSteal = Mathf.Max(0f, normalAttackLifeSteal);
    }
#endif
}
