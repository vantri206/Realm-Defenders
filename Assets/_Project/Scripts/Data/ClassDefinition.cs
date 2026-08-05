using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ClassDefinition", menuName = "Scriptable Objects/ClassDefinition")]
public class ClassDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string classId;
    [SerializeField] private Sprite icon;
    [SerializeField] private String description;

    public string ClassId => classId;
    public Sprite Icon => icon;
    public string Description => description;
}
