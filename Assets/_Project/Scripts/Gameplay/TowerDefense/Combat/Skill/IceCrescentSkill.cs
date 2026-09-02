using System;
using UnityEngine;

[Serializable]
public class IceCrescentSkill : BaseSkill
{
    [Header("Ice Crescent")]
    [SerializeField] private int meleeRange = 1;

    public override void Initialize(HeroRuntime owner, SkillDefinition definition)
    {
        base.Initialize(owner, definition);
        Owner.NormalAttackController.OnNormalAttackFired += HandleNormalAttackFired;
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
        }

        base.ClearData();
    }

    private void HandleNormalAttackFired(NormalAttackFiredData firedData)
    {
        if (Owner == null || firedData == null || firedData.Target == null || firedData.Target.OwnerRuntime == null)
        {
            return;
        }

        Vector3Int ownerCell = Owner.ActiveCellPosition;
        Vector3Int targetCell = firedData.Target.OwnerRuntime.ActiveCellPosition;
        int cellDistance = Mathf.Max(Mathf.Abs(ownerCell.x - targetCell.x), Mathf.Abs(ownerCell.y - targetCell.y));
        firedData.AttackMethod = cellDistance <= Mathf.Max(0, meleeRange) ? AttackMethod.DirectTarget : AttackMethod.Projectile;
    }
}
