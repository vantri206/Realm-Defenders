using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroDefinition", menuName = "Scriptable Objects/HeroDefinition")]
public class HeroDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string heroId;
    [SerializeField] private string heroName;
    [SerializeField] private Sprite heroDefaultSprite;
    [SerializeField] private Sprite heroIcon;
    [SerializeField] private Sprite heroDisplaySprite;
    [SerializeField] private ClassDefinition heroClass;
    [SerializeField] private HeroRarity heroRarity;
    [SerializeField, TextArea(3, 10)] private string heroDescription;
    [SerializeField] private AnimatorOverrideController heroAnimator;
    [SerializeField] private HeroRuntime heroPrefab;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attack = 10f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float defense = 0f;
    [SerializeField] private float specialDefense = 0f;
    [SerializeField] private int blockCount = 1;
    [SerializeField] private UnitMovementType movementType = UnitMovementType.Ground;
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private UnitStatProgressionTable statProgressionTable;

    [Header("Attack")]
    [SerializeField] private NormalAttackDefinition normalAttackDefinition;

    [Header("Abilities")]
    [SerializeField] private SkillDefinition passiveSkill;
    [SerializeField] private SkillDefinition activeSkill;

    [Header("Deploy Stats")]
    [SerializeField] private int baseDeployCost = 15;
    [SerializeField] private float baseRedeployTime = 20f;

    public string HeroId => heroId;
    public string HeroName => heroName;
    public Sprite HeroDisplaySprite => heroDisplaySprite;
    public Sprite HeroDefaultSprite => heroDefaultSprite;
    public Sprite HeroIcon => heroIcon;
    public string HeroDescription => heroDescription;
    public ClassDefinition HeroClass => heroClass;
    public HeroRarity HeroRarity => heroRarity;
    public AnimatorOverrideController AnimatorController => heroAnimator;
    public HeroRuntime Prefab => heroPrefab;
    public float MaxHealth => maxHealth;
    public float Attack => attack;
    public float AttackInterval => attackInterval;
    public float Defense => defense;
    public float SpecialDefense => specialDefense;
    public UnitMovementType MovementType => movementType;
    public NormalAttackDefinition NormalAttackDefinition => normalAttackDefinition;
    public SkillDefinition PassiveSkill => passiveSkill;
    public SkillDefinition ActiveSkill => activeSkill;
    public int BlockCount => blockCount;
    public float MoveSpeed => moveSpeed;
    public UnitStatProgressionTable StatProgressionTable => statProgressionTable;
    public int BaseDeployCost => baseDeployCost;
    public float BaseRedeployTime => baseRedeployTime;

    public bool IsValid => heroPrefab != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        attack = Mathf.Max(0f, attack);
        attackInterval = Mathf.Max(0f, attackInterval);
        moveSpeed = Mathf.Max(0f, moveSpeed);
        baseDeployCost = Mathf.Max(0, baseDeployCost);
        baseRedeployTime = Mathf.Max(0f, baseRedeployTime);
    }
#endif
}
