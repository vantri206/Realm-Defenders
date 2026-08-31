using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FourthStrikeSkill : BaseSkill
{
    [Header("Fourth Strike")]
    [SerializeField] private int requiredNormalAttackCount = 4;
    [SerializeField] private float healMultiplier = 0.6f;
    [SerializeField] private ParticleVFX healVFXPrefab;

    [NonSerialized] private int currentNormalAttackCount;
    [NonSerialized] private HashSet<int> healingAttackIds;
    [NonSerialized] private Dictionary<int, float> physicalDamageByAttackId;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);

        currentNormalAttackCount = 0;
        healingAttackIds = new HashSet<int>();
        physicalDamageByAttackId = new Dictionary<int, float>();

        Owner.NormalAttackController.OnNormalAttackFired += HandleNormalAttackFired;
        Owner.NormalAttackController.OnNormalAttackHitResolved += HandleNormalAttackHitResolved;
        Owner.NormalAttackController.OnNormalAttackFinished += HandleNormalAttackFinished;
    }

    public override bool CanActivate()
    {
        return false;
    }

    public override void Activate()
    {
    }

    public override void ClearData()
    {
        if (Owner != null && Owner.NormalAttackController != null)
        {
            Owner.NormalAttackController.OnNormalAttackFired -= HandleNormalAttackFired;
            Owner.NormalAttackController.OnNormalAttackHitResolved -= HandleNormalAttackHitResolved;
            Owner.NormalAttackController.OnNormalAttackFinished -= HandleNormalAttackFinished;
        }

        healingAttackIds?.Clear();
        physicalDamageByAttackId?.Clear();
        currentNormalAttackCount = 0;
        base.ClearData();
    }

    private void HandleNormalAttackFired(NormalAttackFiredData firedData)
    {
        currentNormalAttackCount++;
        if (currentNormalAttackCount < Mathf.Max(1, requiredNormalAttackCount))
        {
            return;
        }

        currentNormalAttackCount = 0;
        healingAttackIds.Add(firedData.AttackId);
        physicalDamageByAttackId[firedData.AttackId] = 0f;
    }

    private void HandleNormalAttackHitResolved(int attackId, HitData hitData, HitResult hitResult)
    {
        if (!healingAttackIds.Contains(attackId) || hitData.Effect != AttackEffect.Damage ||
            hitData.DamageType != AttackDamageType.PhysicalDamage)
        {
            return;
        }

        physicalDamageByAttackId[attackId] += hitResult.DamageTaken;
    }

    private void HandleNormalAttackFinished(int attackId)
    {
        if (!healingAttackIds.Remove(attackId))
        {
            return;
        }

        physicalDamageByAttackId.TryGetValue(attackId, out float physicalDamage);
        physicalDamageByAttackId.Remove(attackId);

        if (Owner == null || Owner.IsDead || physicalDamage <= 0f)
        {
            return;
        }

        float healValue = DamageCalculator.CalculateBaseEffectValue(physicalDamage, Mathf.Max(0f, healMultiplier));
        HitResult healResult = DamageSystem.ApplyHeal(new HealRequest(Owner.gameObject, Owner.Health, healValue, Owner.CenterPosition));
        if (healResult.HealthRestored > 0f && healVFXPrefab != null)
        {
            CombatVFXSpawner.SpawnParticleVFX(healVFXPrefab, Owner.Hurtbox);
        }
    }
}
