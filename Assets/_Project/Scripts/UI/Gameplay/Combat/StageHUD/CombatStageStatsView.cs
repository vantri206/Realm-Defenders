using UnityEngine;
using UnityEngine.UI;

public class CombatStageStatsView : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image meatIcon;
    [SerializeField] private UIValueTextBinding currentMeatText;
    [SerializeField] private UIValueTextBinding currentLivesText;
    [SerializeField] private UIValueTextBinding resolvedObjectiveEnemiesText;
    [SerializeField] private UIValueTextBinding totalObjectiveEnemiesText;

    [Header("Color Settings")]
    [SerializeField] private Color healthTextNormalColor = Color.green;
    [SerializeField] private Color healthTextLowColor = Color.red;

    // References
    private StageSystem stageSystem;


    public void Initialize(StageSystem stageSystem)
    {
        if (stageSystem == null)
        {
            Debug.LogError("[CombatStageStatsView] StageSystem reference is null.");
            return;
        }

        if (this.stageSystem != null)
        {
            this.stageSystem.OnStageStatsChanged -= UpdateUI;
        }

        this.stageSystem = stageSystem;

        if (isActiveAndEnabled)
        {
            this.stageSystem.OnStageStatsChanged += UpdateUI;
        }

        UpdateUI();
    }

    private void OnEnable()
    {
        if (stageSystem != null)
        {
            stageSystem.OnStageStatsChanged -= UpdateUI;
            stageSystem.OnStageStatsChanged += UpdateUI;
        }
    }

    private void OnDisable()
    {
        if (stageSystem != null)
        {
            stageSystem.OnStageStatsChanged -= UpdateUI;
        }
    }

    private void Update()
    {
        if (stageSystem.IsInitialized)
        {
            float remainingMeatTimerTime = stageSystem.MeatNaturalTimer.RemainingTime;
            float totalMeatTimerTime = stageSystem.MeatNaturalTimer.TotalTime;
            float fillAmount = Mathf.Clamp01(remainingMeatTimerTime / Mathf.Max(totalMeatTimerTime, 0.0001f));
            if (meatIcon != null)
            {
                meatIcon.fillAmount = fillAmount;
            }
        }
    }

    private void UpdateUI()
    {
        currentMeatText.SetInt(stageSystem.CurrentMeat);
        currentLivesText.SetInt(stageSystem.CurrentLives);
        resolvedObjectiveEnemiesText.SetInt(stageSystem.ResolvedObjectiveEnemies);
        totalObjectiveEnemiesText.SetInt(stageSystem.StageEnemyCount);

        if (stageSystem.CurrentLives < stageSystem.StartingLives / 2)
        {
            currentLivesText.SetTextColor(healthTextLowColor);
        }
        else
        {
            currentLivesText.SetTextColor(healthTextNormalColor);
        }
    }
}
