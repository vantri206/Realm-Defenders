using System;
using UnityEngine;

[Serializable]
public class TripleSlashSkill : NormalAttackOverrideSkill
{
    [Header("Triple Slash")]
    [SerializeField] private int hitCount = 3;
    [SerializeField] private float damageMultiplierPerHit = 0.6f;
    [SerializeField] private float hitInterval = 0.1f;
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

    protected override bool ExecuteOverrideAttack(Hurtbox target)
    {
        sequenceTarget = target;
        remainingHitCount = Mathf.Max(1, hitCount);
        rawDamagePerHit = DamageCalculator.CalculateBaseDamage(Owner.Attack, Mathf.Max(0f, damageMultiplierPerHit));
        hitTimer = new CountdownTimer(Mathf.Max(0f, hitInterval));

        Owner.FacePosition(target.AimPosition);
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

            if (Owner.SkillAttackController.TryProcessHit(hitData, out HitResult hitResult))
            {
                if (hitVFXPrefab != null)
                {
                    CombatVFXSpawner.SpawnSimpleSpriteVFX(hitVFXPrefab, hitData.HitPosition);
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
