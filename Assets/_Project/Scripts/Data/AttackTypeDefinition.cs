using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackTypeDefinition", menuName = "Scriptable Objects/AttackTypeDefinition")]
public class AttackTypeDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private UnitAttackType attackType;
    [SerializeField] private Sprite icon;
    [SerializeField] private String description;

    public UnitAttackType AttackType => attackType;
    public Sprite Icon => icon;
    public string Description => description;
}
