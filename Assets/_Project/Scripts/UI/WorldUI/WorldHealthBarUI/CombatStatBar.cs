using UnityEngine;

public class CombatStatBar : WorldHealthBar
{
    [Header("Combat Stat References")]
    [SerializeField] private SpriteRenderer shieldFillRenderer;
    [SerializeField] private SpriteRenderer skillChargeFillRenderer;
    [SerializeField] private SpriteRenderer skillChargeBackgroundRenderer;

    private Transform shieldFillTransform;
    private Transform skillChargeFillTransform;
    private Vector3 fullShieldFillScale;
    private Vector3 fullSkillChargeFillScale;

    private float shieldPercent;
    private float skillChargePercent = 1f;

    protected override void OnEnable()
    {
        base.OnEnable();
        ApplyShieldFill();
        ApplySkillChargeFill();
    }

    public void SetShieldValue(float currentShield, float maxHealth)
    {
        if (maxHealth > 0f)
        {
            shieldPercent = Mathf.Clamp01(currentShield / maxHealth);
        }
        else
        {
            shieldPercent = 0f;
        }

        ApplyShieldFill();
    }

    public void SetSkillCharge(float cooldownRemaining, float cooldownTime)
    {
        if (cooldownTime > 0f)
        {
            skillChargePercent = Mathf.Clamp01(1f - cooldownRemaining / cooldownTime);
        }
        else
        {
            skillChargePercent = 1f;
        }

        ApplySkillChargeFill();
    }

    protected override void SetVisible(bool isVisible)
    {
        base.SetVisible(isVisible);

        if (shieldFillRenderer != null)
        {
            shieldFillRenderer.enabled = isVisible;
        }

        if (skillChargeFillRenderer != null)
        {
            skillChargeFillRenderer.enabled = isVisible;
        }

        if (skillChargeBackgroundRenderer != null)
        {
            skillChargeBackgroundRenderer.enabled = isVisible;
        }
    }

    private void ApplyShieldFill()
    {
        ApplyFill(shieldFillTransform, fullShieldFillScale, shieldPercent);
    }

    private void ApplySkillChargeFill()
    {
        ApplyFill(skillChargeFillTransform, fullSkillChargeFillScale, skillChargePercent);
    }

    private static void ApplyFill(Transform fillTransform, Vector3 fullFillScale, float percent)
    {
        if (fillTransform == null)
        {
            return;
        }

        Vector3 fillScale = fullFillScale;
        fillScale.x = Mathf.Clamp01(percent);
        fillTransform.localScale = fillScale;
    }

    [ContextMenu("Cache References")]
    protected override void CacheReferences()
    {
        base.CacheReferences();

        if (shieldFillRenderer != null)
        {
            shieldFillTransform = shieldFillRenderer.transform;
            fullShieldFillScale = shieldFillTransform.localScale;
            fullShieldFillScale.x = 1f;
        }

        if (skillChargeFillRenderer != null)
        {
            skillChargeFillTransform = skillChargeFillRenderer.transform;
            fullSkillChargeFillScale = skillChargeFillTransform.localScale;
            fullSkillChargeFillScale.x = 1f;
        }
    }
}
