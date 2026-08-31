using UnityEngine;

[CreateAssetMenu(fileName = "SkillDefinition", menuName = "Scriptable Objects/SkillDefinition")]
public class SkillDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string skillId;
    [SerializeField] private string skillName;
    [SerializeField] private Sprite skillIcon;
    [SerializeField, TextArea] private string skillDescription;

    [Header("Skill")]
    [SerializeField] private SkillType skillType = SkillType.Active;
    [SerializeField] private SkillTargetType targetType = SkillTargetType.Enemy;
    [SerializeField] private float cooldown;
    [SerializeReference] private BaseSkill skill;

    public string SkillId => skillId;
    public string SkillName => skillName;
    public Sprite SkillIcon => skillIcon;
    public string SkillDescription => skillDescription;
    public SkillType SkillType => skillType;
    public SkillTargetType TargetType => targetType;
    public float Cooldown => cooldown;
    public BaseSkill Skill => skill;

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        cooldown = Mathf.Max(0f, cooldown);

        if (skill != null)
        {
            if (skill is AutoActiveSkill autoActiveSkill)
            {
                skillType = SkillType.Active;
            }
            else
            {
                skillType = SkillType.Passive;
            }
        }
    }
#endif
}
