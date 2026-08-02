using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HeroDefinition", menuName = "Scriptable Objects/HeroDefinition")]
public class HeroDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string heroId;
    [SerializeField] private string heroName;
    [SerializeField] private Sprite icon;
    [SerializeField] private HeroRuntime prefab;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float attack = 10f;
    [SerializeField] private float attackInterval = 1f;

    [Header("Attack")]
    [SerializeField] private TargetPriorityMode targetPriorityMode = TargetPriorityMode.Nearest;
    [SerializeField] private List<Vector2Int> attackPattern = new List<Vector2Int>
    {
        Vector2Int.zero,
        Vector2Int.left,
    };

    public string HeroId => heroId;
    public string HeroName => heroName;
    public Sprite Icon => icon;
    public HeroRuntime Prefab => prefab;
    public float MaxHealth => maxHealth;
    public float Attack => attack;
    public float AttackInterval => attackInterval;
    public TargetPriorityMode TargetPriorityMode => targetPriorityMode;
    public IReadOnlyList<Vector2Int> AttackPattern => attackPattern;
}
