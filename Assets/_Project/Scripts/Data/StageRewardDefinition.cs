using UnityEngine;

[CreateAssetMenu(fileName = "StageRewardDefinition", menuName = "Scriptable Objects/Stage/StageRewardDefinition")]
public class StageRewardDefinition : ScriptableObject
{
    [SerializeField] private int experienceAmount;
    [SerializeField] private GearDefinition gearReward;
    [SerializeField] private HeroDefinition heroReward;
    [SerializeField] private int heroRewardLevel = 1;

    public int ExperienceAmount => experienceAmount;
    public GearDefinition GearReward => gearReward;
    public HeroDefinition HeroReward => heroReward;
    public int HeroRewardLevel => heroRewardLevel;

#if UNITY_EDITOR
    private void OnValidate()
    {
        experienceAmount = Mathf.Max(0, experienceAmount);
        heroRewardLevel = Mathf.Max(1, heroRewardLevel);
    }
#endif
}
