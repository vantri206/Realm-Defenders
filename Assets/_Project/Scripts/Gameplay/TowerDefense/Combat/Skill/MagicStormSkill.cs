using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MagicStormSkill : AutoActiveSkill
{
    [Header("Magic Storm")]
    [SerializeField] private float duration = 4f;
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private float damageMultiplierPerTick = 0.5f;
    [SerializeField] private UnitAttackType attackType = UnitAttackType.Ranged;
    [SerializeField] private List<Vector2Int> areaPattern = new List<Vector2Int> { Vector2Int.zero };
    [SerializeField] private AttackAOEHit aoeHitPrefab;
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    [NonSerialized] private Hurtbox activationTarget;
    [NonSerialized] private Vector3 stormPosition;
    [NonSerialized] private float rawDamagePerTick;
    [NonSerialized] private float elapsedTime;
    [NonSerialized] private float nextTickTime;
    [NonSerialized] private int activeAOECount;
    [NonSerialized] private bool allTicksSpawned;

    public override bool CanActivate()
    {
        if (!CanCastSkill || aoeHitPrefab == null)
        {
            return false;
        }

        IReadOnlyList<Vector2Int> resolvedPattern = AttackPatternResolver.RefreshAttackPattern(areaPattern, Owner.FacingDirection);
        return Owner.TrySelectSkillTarget(resolvedPattern, TargetSide.Enemy, AttackEffect.Damage, attackType, out activationTarget);
    }

    public override void Activate()
    {
        if (activationTarget == null)
        {
            FinishSkill();
            return;
        }

        stormPosition = activationTarget.AimPosition;
        rawDamagePerTick = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplierPerTick));
        elapsedTime = 0f;
        nextTickTime = Mathf.Max(0.01f, tickInterval);
        activeAOECount = 0;
        allTicksSpawned = false;

        Owner.FacePosition(stormPosition);
        Owner.TriggerSkillAttackAnimation();
        activationTarget = null;
    }

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (!IsActiving || Owner == null || Owner.IsDead)
        {
            if (IsActiving && Owner != null && Owner.IsDead)
            {
                ResetStorm();
                FinishSkill();
            }
            return;
        }

        float resolvedDuration = Mathf.Max(0f, duration);
        elapsedTime += Mathf.Max(0f, deltaTime);

        while (nextTickTime <= resolvedDuration && elapsedTime >= nextTickTime)
        {
            SpawnStormTick();
            nextTickTime += Mathf.Max(0.01f, tickInterval);
        }

        if (nextTickTime > resolvedDuration)
        {
            allTicksSpawned = true;
            TryFinishStorm();
        }
    }

    public override void ClearData()
    {
        ResetStorm();
        base.ClearData();
    }

    private void SpawnStormTick()
    {
        AttackExecutionData executionData = new AttackExecutionData
        (
            Owner.gameObject,
            Owner.BattleTeam,
            TargetSide.Enemy,
            AttackEffect.Damage,
            attackType,
            rawDamagePerTick,
            AttackDamageType.MagicalDamage
        );
        AttackVFXData vfxData = new AttackVFXData(hitVFXPrefab);

        activeAOECount++;
        AttackAOEHit aoeHit = ObjectPoolingHelper.Spawn
        (
            aoeHitPrefab,
            stormPosition,
            Quaternion.identity,
            spawnedAOEHit => spawnedAOEHit.Initialize
            (
                executionData,
                vfxData,
                Owner.CombatTime,
                HandleStormTickFinished,
                HandleStormHitResolved
            )
        );

        if (aoeHit == null)
        {
            HandleStormTickFinished();
        }

        Owner.TriggerSkillAttackAnimation();
    }

    private void HandleStormHitResolved(HitData hitData, HitResult hitResult)
    {
        if (Owner != null)
        {
            Owner.SkillAttackController.NotifySkillAttackHitResolved(hitData, hitResult);
        }
    }

    private void HandleStormTickFinished()
    {
        activeAOECount = Mathf.Max(0, activeAOECount - 1);
        TryFinishStorm();
    }

    private void TryFinishStorm()
    {
        if (!allTicksSpawned || activeAOECount > 0)
        {
            return;
        }

        ResetStorm();
        FinishSkill();
    }

    private void ResetStorm()
    {
        activationTarget = null;
        stormPosition = Vector3.zero;
        rawDamagePerTick = 0f;
        elapsedTime = 0f;
        nextTickTime = 0f;
        activeAOECount = 0;
        allTicksSpawned = false;
    }
}
