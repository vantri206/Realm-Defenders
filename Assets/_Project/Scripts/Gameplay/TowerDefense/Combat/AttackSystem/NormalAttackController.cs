using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NormalAttackController : MonoBehaviour
{
    private readonly List<Vector2Int> defaultPatternOffsets = new List<Vector2Int>
    {
        Vector2Int.zero,
        Vector2Int.left,
    };

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

    public void Initialize(float attack, float attackInterval)
    {
        this.attack = Mathf.Max(0f, attack);
        this.attackInterval = Mathf.Max(0f, attackInterval);
        attackTimer = new CountdownTimer(this.attackInterval);
        isInitialized = true;
    }

    public void Tick(float deltaTime, Vector3Int originCell, IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (!isInitialized)
        {
            return;
        }

        attackTimer.Tick(deltaTime);

        if (!IsReadyAttack)
        {
            return;
        }

        TryAttack(originCell, patternOffsets);
    }

    public bool TryAttack(Vector3Int originCell, IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (!isInitialized)
        {
            return false;
        }

        Hurtbox target = SelectTarget(originCell, patternOffsets);
        if (target == null)
        {
            currentTarget = null;
            return false;
        }

        Attack(target);
        attackTimer.Reset(attackInterval);
        attackTimer.StartTimer();
        return true;
    }

    public Hurtbox SelectTarget(Vector3Int originCell, IReadOnlyList<Vector2Int> patternOffsets)
    {
        Hurtbox target = null;

        if (targetScanner == null || targetSelector == null || targetScanner.CombatGrid == null || patternOffsets == null)
        {
            return null;
        }

        targetScanner.Scan(originCell, patternOffsets, validTargets);

        target = targetSelector.SelectTarget(validTargets, targetScanner.CombatGrid.CellToWorldCenter(originCell));
        return target;
    }

    private void Attack(Hurtbox target)
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
