using UnityEngine;

public class CombatTimeController : MonoBehaviour
{
    [Header("Time Settings")]
    [SerializeField] private float combatSpeedMultiplier = 1f;

    public float CombatSpeedMultiplier => combatSpeedMultiplier;
    public float CombatDeltaTime => Time.deltaTime * combatSpeedMultiplier;
    public float CombatFixedDeltaTime => Time.fixedDeltaTime * combatSpeedMultiplier;
    public bool IsCombatPaused => combatSpeedMultiplier <= 0f;

    public void SetSpeedMultiplier(float multiplier)
    {
        combatSpeedMultiplier = Mathf.Max(multiplier, 0f);
    }  

    public void PauseCombat()
    {
        combatSpeedMultiplier = 0f;
    }

    public void Resume()
{
        combatSpeedMultiplier = 1f;
    }
}
