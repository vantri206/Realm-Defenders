using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NormalAttackController : MonoBehaviour
{
    // References
    private readonly List<Hurtbox> validTargets = new List<Hurtbox>();
    private TargetScanner targetScanner;
    private TargetSelector targetSelector;
    private UnitVisual unitVisual;
    private CountdownTimer attackTimer;
    private Hurtbox currentTarget;

    // Attack properties
    private float attack;
    private float attackInterval;

    private bool isInitialized;

    public Hurtbox CurrentTarget => currentTarget;
    public bool IsReadyAttack => isInitialized && attackTimer.IsFinished;

    private void Awake()
    {
        CacheReferences();
        attackTimer = new CountdownTimer(0f);
    }

    public void Initialize(float attack, float attackInterval, TargetScanner targetScanner, TargetSelector targetSelector, UnitVisual unitVisual)
    {
        this.attack = Mathf.Max(0f, attack);
        this.attackInterval = Mathf.Max(0f, attackInterval);

        this.targetScanner = targetScanner;
        this.targetSelector = targetSelector;
        this.unitVisual = unitVisual;

        isInitialized = true;
    }

    public void Tick(float deltaTime, Vector3Int attackerCell, IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (!isInitialized)
        {
            return;
        }

        if (attackTimer.IsRunning)
        {
            attackTimer.Tick(deltaTime);
        }

        if (!IsReadyAttack)
        {
            return;
        }

        TriggerAttack(attackerCell, patternOffsets);
    }

    public void TriggerAttack(Vector3Int attackerCell, IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (!isInitialized)
        {
            return;
        }

        Hurtbox target = SelectTarget(attackerCell, patternOffsets);
        if (target == null)
        {
            currentTarget = null;
            return;
        }

        NormalAttack(target);
        attackTimer.Reset(attackInterval);
        attackTimer.StartTimer();
    }

    public Hurtbox SelectTarget(Vector3Int attackerCell, IReadOnlyList<Vector2Int> patternOffsets)
    {
        Hurtbox target = null;

        if (targetScanner == null || targetSelector == null || targetScanner.CombatGrid == null || patternOffsets == null)
        {
            return null;
        }

        targetScanner.Scan(attackerCell, patternOffsets, validTargets);

        target = targetSelector.SelectTarget(validTargets, targetScanner.CombatGrid.CellToWorldCenter(attackerCell));
        return target;
    }

    private void NormalAttack(Hurtbox target)
    {
        currentTarget = target;

        IDamageable damageable = target.GetDamageable();
        if (damageable == null)
        {
            return;
        }

        unitVisual?.TriggerAttack();

        DamageSystem.ApplyDamage(new DamageRequest(gameObject, damageable, attack, target.Position));
    }

    private void CacheReferences()
    {
        if (targetScanner == null)
        {
            targetScanner = GetComponent<TargetScanner>();
        }

        if (targetSelector == null)
        {
            targetSelector = GetComponent<TargetSelector>();
        }

        if (unitVisual == null)
        {
            unitVisual = GetComponentInChildren<UnitVisual>();
        }
    }
}
