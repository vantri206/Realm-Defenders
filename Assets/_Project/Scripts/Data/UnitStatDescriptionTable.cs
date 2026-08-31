using UnityEngine;

[CreateAssetMenu(fileName = "UnitStatDescriptionTable", menuName = "Scriptable Objects/UnitStatDescriptionTable")]
public class UnitStatDescriptionTable : ScriptableObject
{
    [SerializeField, TextArea] private string healthDescription;
    [SerializeField, TextArea] private string attackDescription;
    [SerializeField, TextArea] private string defenseDescription;
    [SerializeField, TextArea] private string specialDefenseDescription;
    [SerializeField, TextArea] private string attackSpeedDescription;
    [SerializeField, TextArea] private string blockDescription;
    [SerializeField, TextArea] private string deployCostDescription;
    [SerializeField, TextArea] private string redeployTimeDescription;

    public string HealthDescription => healthDescription;
    public string AttackDescription => attackDescription;
    public string DefenseDescription => defenseDescription;
    public string SpecialDefenseDescription => specialDefenseDescription;
    public string AttackSpeedDescription => attackSpeedDescription;
    public string BlockDescription => blockDescription;
    public string DeployCostDescription => deployCostDescription;
    public string RedeployTimeDescription => redeployTimeDescription;
}
