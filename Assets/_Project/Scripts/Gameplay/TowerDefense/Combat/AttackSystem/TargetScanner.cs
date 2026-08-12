using System.Collections.Generic;
using UnityEngine;

public class TargetScanner : MonoBehaviour
{
    private CombatGrid combatGrid;
    private TeamIdentity ownerTeam;
    private LayerMask targetMask;
    private int maxTargets = 128;

    private readonly HashSet<Hurtbox> uniqueTargets = new HashSet<Hurtbox>();
    private Collider2D[] scanBuffer;
    private ContactFilter2D scanFilter;

    public CombatGrid CombatGrid => combatGrid;

    private void Awake()
    {
        targetMask = GameLayer.HurtboxMask;
    }

    public void Initialize(CombatGrid combatGrid, TeamIdentity ownerTeam)
    {
        this.combatGrid = combatGrid;
        this.ownerTeam = ownerTeam;
        InitializeBuffer();
    }

    public void Scan(Vector2 originPosition, IReadOnlyList<Vector2Int> pattern, List<Hurtbox> results)
    {
        if (results == null)
        {
            Debug.LogError("[TargetScanner] Results list is required for scanning.", this);
            return;
        }

        results.Clear();
        uniqueTargets.Clear();

        if (combatGrid == null || combatGrid.Grid == null)
        {
            Debug.LogError("[TargetScanner] CombatGrid is required before scanning targets.", this);
            return;
        }

        if (pattern == null)
        {
            Debug.LogError("[TargetScanner] Attack pattern is required before scanning targets.", this);
            return;
        }

        InitializeBuffer();
        ConfigureScanFilter();

        for (int i = 0; i < pattern.Count; i++)
        {
            Vector2Int offset = pattern[i];
            Vector2 scanPosition = GetPatternScanPosition(originPosition, offset);
            ScanAtPosition(scanPosition, results);
        }
    }

    public bool IsValidTarget(Hurtbox hurtbox)
    {
        if (hurtbox == null)
        {
            return false;
        }

        IDamageable damageable = hurtbox.GetDamageable();
        if (damageable == null || damageable.IsDead)
        {
            return false;
        }

        TeamIdentity targetTeam = hurtbox.GetTargetTeam();
        if (ownerTeam != null && !ownerTeam.IsEnemy(targetTeam))
        {
            return false;
        }

        return true;
    }

    private void ScanAtPosition(Vector2 centerPosition, List<Hurtbox> results)
    {
        Vector2 cellSize = GetCellScanSize();
        int hitCount = Physics2D.OverlapBox(centerPosition, cellSize, 0f, scanFilter, scanBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            if (!scanBuffer[i].TryGetComponent(out Hurtbox hurtbox))
            {
                continue;
            }

            if (!IsValidTarget(hurtbox) || !uniqueTargets.Add(hurtbox))
            {
                continue;
            }

            results.Add(hurtbox);
        }
    }

    private Vector2 GetPatternScanPosition(Vector2 originPosition, Vector2Int offset)
    {
        Vector3 gridCellSize = combatGrid.Grid.cellSize;
        return originPosition + new Vector2(offset.x * gridCellSize.x, offset.y * gridCellSize.y);
    }

    private Vector2 GetCellScanSize()
    {
        Vector3 gridCellSize = combatGrid.Grid.cellSize;
        return new Vector2(Mathf.Max(0f, gridCellSize.x), Mathf.Max(0f, gridCellSize.y));
    }

    private void InitializeBuffer()
    {
        int capacity = Mathf.Max(1, maxTargets);

        if (scanBuffer == null || scanBuffer.Length != capacity)
        {
            scanBuffer = new Collider2D[capacity];
        }
    }

    private void ConfigureScanFilter()
    {
        scanFilter = new ContactFilter2D();
        scanFilter.SetLayerMask(targetMask);
        scanFilter.useTriggers = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maxTargets = Mathf.Max(1, maxTargets);
    }
#endif
}
