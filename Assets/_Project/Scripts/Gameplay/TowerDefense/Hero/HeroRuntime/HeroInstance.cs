using System;
using UnityEngine;

[Serializable]
public class HeroInstance
{
    [SerializeField] private HeroDefinition definition;
    [SerializeField] private int level = 1;
    [SerializeField] private int star = 1;

    public HeroDefinition Definition => definition;
    public int Level => level;
    public int Star => star;
    public bool IsValid => definition != null;

    public void Initialize(HeroDefinition definition)
    {
        this.definition = definition;
        level = 1;
        star = 1;
    }

    public void SetLevel(int value)
    {
        level = Mathf.Max(1, value);
    }

    public void SetStar(int value)
    {
        star = Mathf.Max(1, value);
    }
}
