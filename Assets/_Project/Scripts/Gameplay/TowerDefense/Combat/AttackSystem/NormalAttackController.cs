using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NormalAttackController : MonoBehaviour
{
    [SerializeField] private Transform attackPoint;

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
        if (targetScanner == null)
        {
            Debug.LogError("[NormalAttackController] TargetScanner is required to initialize attacks.", this);
            return;
        }

        if (targetSelector == null)
        {
            Debug.LogError("[NormalAttackController] TargetSelector is required to initialize attacks.", this);
            return;
        }

        this.attack = Mathf.Max(0f, attack);
        this.attackInterval = Mathf.Max(0f, attackInterval);

        this.targetScanner = targetScanner;
        this.targetSelector = targetSelector;
        this.unitVisual = unitVisual;

        isInitialized = true;
    }

    public void Tick(float deltaTime, IReadOnlyList<Vector2Int> patternOffsets)
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

        TriggerAttack(patternOffsets);
    }

    public void TriggerAttack(IReadOnlyList<Vector2Int> patternOffsets)
    {
        if (!isInitialized)
        {
            return;
        }

        Hurtbox target = SelectTarget(patternOffsets);
        if (target == null)
        {
            currentTarget = null;
            return;
        }

        NormalAttack(target);
        attackTimer.Reset(attackInterval);
        attackTimer.StartTimer();
    }

    public Hurtbox SelectTarget(IReadOnlyList<Vector2Int> patternOffsets)
    {
        Hurtbox target = null;

        if (targetScanner == null || targetSelector == null || targetScanner.CombatGrid == null || patternOffsets == null)
        {
            Debug.LogError("[NormalAttackController] Cannot select target because required attack dependencies are missing.", this);
            return null;
        }

        Vector2 originPosition = GetAttackOriginPosition();
        targetScanner.Scan(originPosition, patternOffsets, validTargets);

        target = targetSelector.SelectTarget(validTargets, originPosition);
        return target;
    }

    private void NormalAttack(Hurtbox target)
    {
        currentTarget = target;

        IDamageable damageable = target.GetDamageable();
        if (damageable == null)
        {
            Debug.LogWarning("[NormalAttackController] Selected Hurtbox does not resolve to an IDamageable target.", this);
            return;
        }

        if (unitVisual == null)
        {
            Debug.LogError("[NormalAttackController] UnitVisual is required to trigger attack animation.", this);
        }
        else
        {
            unitVisual.TriggerAttack();
        }

        DamageSystem.ApplyDamage(new DamageRequest(gameObject, damageable, attack, target.Position));
    }

    private Vector2 GetAttackOriginPosition()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
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
