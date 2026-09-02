using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TripleSlashSkill : NormalAttackOverrideSkill
{
    [Header("Triple Slash")]
    [SerializeField] private int hitCount = 3;
    [SerializeField] private float damageMultiplierPerHit = 0.6f;
    [SerializeField] private float hitInterval = 0.1f;
    [SerializeField] private List<SimpleSpriteAnimatorVFX> slashVFXPrefabs = new List<SimpleSpriteAnimatorVFX>();
    [SerializeField] private SimpleSpriteAnimatorVFX hitVFXPrefab;

    [NonSerialized] private Hurtbox sequenceTarget;
    [NonSerialized] private CountdownTimer hitTimer;
    [NonSerialized] private int remainingHitCount;
    [NonSerialized] private float rawDamagePerHit;

    public override void Tick(float deltaTime)
    {
        base.Tick(deltaTime);

        if (remainingHitCount <= 0 || hitTimer == null || !hitTimer.IsRunning)
        {
            return;
        }

        hitTimer.Tick(deltaTime);
        if (hitTimer.IsFinished)
        {
            ProcessNextHit();
        }
    }

    protected override bool ExecuteOverrideAttack()
    {
        Hurtbox target = OverrideTargets[0];
        sequenceTarget = target;
        remainingHitCount = Mathf.Max(1, hitCount);
        rawDamagePerHit = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplierPerHit));
        hitTimer = new CountdownTimer(Mathf.Max(0f, hitInterval));

        ProcessNextHit();
        return true;
    }

    public override void ClearData()
    {
        ClearSequence();
        base.ClearData();
    }

    private void ProcessNextHit()
    {
        if (remainingHitCount <= 0)
        {
            return;
        }

        if (Owner != null && sequenceTarget != null)
        {
            HitData hitData = new HitData
            (
                Owner.gameObject,
                sequenceTarget,
                Owner.BattleTeam,
                TargetSide.Enemy,
                AttackEffect.Damage,
                Owner.NormalAttackDefinition.AttackType,
                rawDamagePerHit,
                AttackDamageType.PhysicalDamage,
                sequenceTarget.AimPosition
            );

            if (Owner.SkillAttackController.TryProcessHit(hitData, out _))
            {
                int hitIndex = Mathf.Max(1, hitCount) - remainingHitCount;
                if (hitIndex < slashVFXPrefabs.Count && slashVFXPrefabs[hitIndex] != null)
                {
                    SimpleSpriteAnimatorVFX slashVFXPrefab = slashVFXPrefabs[hitIndex];
                    Vector2Int facingDirection = Owner.FacingDirection;
                    float facingAngle = Mathf.Atan2(facingDirection.y, facingDirection.x) * Mathf.Rad2Deg;
                    float rotationAngle = slashVFXPrefab.transform.eulerAngles.z + facingAngle;
                    Quaternion rotation = Quaternion.Euler(0f, 0f, rotationAngle);
                    CombatVFXSpawner.SpawnSimpleSpriteVFX(slashVFXPrefab, Owner.NormalAttackController.AttackOrigin, rotation, Owner.CombatTime);
                }

                if (hitVFXPrefab != null)
                {
                    CombatVFXSpawner.SpawnSimpleSpriteVFX(hitVFXPrefab, sequenceTarget, Owner.CombatTime);
                }
            }
        }

        remainingHitCount--;
        if (remainingHitCount <= 0)
        {
            ClearSequence();
            FinishOverrideAttack();
            return;
        }

        if (hitTimer.TotalTime <= 0f)
        {
            ProcessNextHit();
            return;
        }

        hitTimer.Reset();
        hitTimer.StartTimer();
    }

    private void ClearSequence()
    {
        if (hitTimer != null)
        {
            hitTimer.StopTimer();
            hitTimer = null;
        }

        sequenceTarget = null;
        remainingHitCount = 0;
        rawDamagePerHit = 0f;
    }
}
