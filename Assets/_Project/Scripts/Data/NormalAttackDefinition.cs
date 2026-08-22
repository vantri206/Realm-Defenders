using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NormalAttackDefinition", menuName = "Scriptable Objects/NormalAttackDefinition")]
public class NormalAttackDefinition : ScriptableObject
{
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

    [Header("Normal Attack VFX")]
    [SerializeField] private SimpleSpriteAnimatorVFX normalAttackHitVFXPrefab;
    [SerializeField, Tooltip("Particle VFX spawned at each target after a normal attack successfully restores health.")]
    private ParticleVFX normalAttackHealVFXPrefab;

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
    public ParticleVFX NormalAttackHealVFXPrefab => normalAttackHealVFXPrefab;

#if UNITY_EDITOR
    private void OnValidate()
    {
        normalAttackEffectMultiplier = Mathf.Max(0f, normalAttackEffectMultiplier);
    }
#endif
}
