using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroDefinition", menuName = "Scriptable Objects/HeroDefinition")]
public class HeroDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string heroId;
    [SerializeField] private string heroName;
    [SerializeField] private Sprite heroSprite;
    [SerializeField] private Sprite heroIcon;
    [SerializeField] private ClassDefinition heroClass;
    [SerializeField] private string heroDescription;
    [SerializeField] private AnimatorOverrideController heroAnimator;
    [SerializeField] private HeroRuntime heroPrefab;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attack = 10f;
    [SerializeField] private float attackInterval = 1f;
    [SerializeField] private float defense = 0f;
    [SerializeField] private float specialDefense = 0f;
    [SerializeField] private int blockCount = 1;

    [Header("Deploy Stats")]
    [SerializeField] private int baseDeployCost = 15;
    [SerializeField] private float baseRedeployTime = 20f;

    [Header("Attack")]
    [SerializeField] private HeroAttackType attackType = HeroAttackType.Melee;
    [SerializeField] private TargetPriorityMode targetPriorityMode = TargetPriorityMode.Nearest;
    [SerializeField] private List<Vector2Int> attackPattern = new List<Vector2Int>
    {
        Vector2Int.zero,
    };

    public string HeroId => heroId;
    public string HeroName => heroName;
    public Sprite HeroSprite => heroSprite;
    public Sprite HeroIcon => heroIcon;
    public string HeroDescription => heroDescription;
    public ClassDefinition HeroClass => heroClass;
    public AnimatorOverrideController AnimatorController => heroAnimator;
    public HeroRuntime Prefab => heroPrefab;
    public float MaxHealth => maxHealth;
    public float Attack => attack;
    public float AttackInterval => attackInterval;
    public float Defense => defense;
    public float SpecialDefense => specialDefense;
    public int BlockCount => blockCount;
    public int BaseDeployCost => baseDeployCost;
    public float BaseRedeployTime => baseRedeployTime;
    public HeroAttackType AttackType => attackType;
    public TargetPriorityMode TargetPriorityMode => targetPriorityMode;
    public IReadOnlyList<Vector2Int> AttackPattern => attackPattern;

    public bool IsValid => heroPrefab != null;

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        attack = Mathf.Max(0f, attack);
        attackInterval = Mathf.Max(0f, attackInterval);
        baseDeployCost = Mathf.Max(0, baseDeployCost);
        baseRedeployTime = Mathf.Max(0f, baseRedeployTime);
    }
#endif
}
