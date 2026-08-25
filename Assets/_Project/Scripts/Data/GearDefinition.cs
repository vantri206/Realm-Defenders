using UnityEngine;

[CreateAssetMenu(fileName = "GearDefinition", menuName = "Scriptable Objects/GearDefinition")]
public class GearDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string gearId;
    [SerializeField] private string gearName;
    [SerializeField] private Sprite gearIcon;
    [SerializeField] private GearType gearType;
    [SerializeField] private GearRarity gearRarity;

    [Header("Stats")]
    [SerializeField] private UnitStatModifier[] statModifiers;

    [Header("Description")]
    [SerializeField, TextArea] private string passiveDescription;

    public string GearId => gearId;
    public string GearName => gearName;
    public Sprite GearIcon => gearIcon;
    public GearType GearType => gearType;
    public GearRarity GearRarity => gearRarity;
    public UnitStatModifier[] StatModifiers => statModifiers;
    public string PassiveDescription => passiveDescription;
}
