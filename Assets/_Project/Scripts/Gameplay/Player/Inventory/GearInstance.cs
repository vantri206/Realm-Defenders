using System;
using UnityEngine;

[Serializable]
public class GearInstance
{
    [SerializeField] private GearDefinition definition;

    private HeroInstance equippedHero;

    public GearDefinition Definition => definition;
    public HeroInstance EquippedHero => equippedHero;

    public bool IsValid => definition != null;

    public GearInstance() { }

    public GearInstance(GearDefinition definition)
    {
        Initialize(definition);
    }

    private void Initialize(GearDefinition definition)
    {
        if (definition == null)
        {
            Debug.LogError("[GearInstance] GearDefinition cannot be null.");
            return;
        }

        this.definition = definition;
    }

    public void OnEquip(HeroInstance hero)
    {
        if (hero == null || !hero.IsValid)
        {
            Debug.LogError("[GearInstance] A valid HeroInstance is required to equip gear.");
            return;
        }

        equippedHero = hero;
    }

    public void OnUnequip()
    {
        if (equippedHero == null)
        {
            Debug.LogWarning("[GearInstance] No hero is currently equipped with this gear.");
            return;
        }

        equippedHero = null;
    }
}