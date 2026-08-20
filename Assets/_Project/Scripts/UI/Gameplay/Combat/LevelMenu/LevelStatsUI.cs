using UnityEngine;
using UnityEngine.UI;

public class LevelStatsUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Image foodIcon;
    [SerializeField] private UIValueTextBinding currentFoodText;
    [SerializeField] private UIValueTextBinding currentLivesText;
    [SerializeField] private UIValueTextBinding resolvedObjectiveEnemiesText;
    [SerializeField] private UIValueTextBinding totalObjectiveEnemiesText;

    [Header("Color Settings")]
    [SerializeField] private Color healthTextNormalColor = Color.green;
    [SerializeField] private Color healthTextLowColor = Color.red;

    // References
    private LevelSystem levelSystem;


    public void Initialize(LevelSystem levelSystem)
    {
        if (levelSystem == null)
        {
            Debug.LogError("LevelStatsUI: LevelSystem reference is null.");
            return;
        }

        if (this.levelSystem != null)
        {
            this.levelSystem.OnLevelStatsChanged -= UpdateUI;
        }

        this.levelSystem = levelSystem;

        if (isActiveAndEnabled)
        {
            this.levelSystem.OnLevelStatsChanged += UpdateUI;
        }

        UpdateUI();
    }

    private void OnEnable()
    {
        if (levelSystem != null)
        {
            levelSystem.OnLevelStatsChanged -= UpdateUI;
            levelSystem.OnLevelStatsChanged += UpdateUI;
        }
    }

    private void OnDisable()
    {
        if (levelSystem != null)
        {
            levelSystem.OnLevelStatsChanged -= UpdateUI;
        }
    }

    private void Update()
    {
        if (levelSystem.IsInitialized)
        {
            float remainingFoodTimerTime = levelSystem.FoodNaturalTimer.RemainingTime;
            float totalFoodTimerTime = levelSystem.FoodNaturalTimer.TotalTime;
            float fillAmount = Mathf.Clamp01(remainingFoodTimerTime / Mathf.Max(totalFoodTimerTime, 0.0001f));
            if (foodIcon != null)
            {
                foodIcon.fillAmount = fillAmount;
            }
        }
    }

    private void UpdateUI()
    {
        currentFoodText.SetInt(levelSystem.CurrentFood);
        currentLivesText.SetInt(levelSystem.CurrentLives);
        resolvedObjectiveEnemiesText.SetInt(levelSystem.ResolvedObjectiveEnemies);
        totalObjectiveEnemiesText.SetInt(levelSystem.LevelEnemyCount);

        if (levelSystem.CurrentLives < levelSystem.StartingLives / 2)
        {
            currentLivesText.SetTextColor(healthTextLowColor);
        }
        else
        {
            currentLivesText.SetTextColor(healthTextNormalColor);
        }
    }
}
