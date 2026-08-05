using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HeroDetailView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject viewRoot;

    [Header("Identity")]
    [SerializeField] private GameObject avatarRoot;
    [SerializeField] private Image avatarImage;
    [SerializeField] private UIValueTextBinding heroName = new UIValueTextBinding();

    [Header("Health")]
    [SerializeField] private UIValueTextBinding currentHealth = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding maxHealth = new UIValueTextBinding();

    [Header("Stats")]
    [SerializeField] private UIValueTextBinding attack = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding defense = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding specialDefense = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding attackSpeed = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding block = new UIValueTextBinding();

    [Header("Deployment")]
    [SerializeField] private UIValueTextBinding deployCost = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding redeployTime = new UIValueTextBinding();

    private HeroInstance currentHero;

    private void Awake()
    {
        Refresh();
    }

    public void Show(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return;
        }

        currentHero = heroInstance;
        Refresh();
        SetData(heroInstance);
    }

    public void Refresh()
    {
        SetAvatar(null);
        heroName.Refresh();
        currentHealth.Refresh();
        maxHealth.Refresh();
        attack.Refresh();
        defense.Refresh();
        specialDefense.Refresh();
        attackSpeed.Refresh();
        block.Refresh();
        deployCost.Refresh();
        redeployTime.Refresh();
    }

    public void Hide()
    {
        currentHero = null;
        Refresh();

        if (viewRoot != null)
        {
            viewRoot.SetActive(false);
            return;
        }

        Debug.LogWarning("View root is not assigned in HeroDetailView.");
        gameObject.SetActive(false);
    }

    public void SetData(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Refresh();
            return;
        }

        HeroDefinition definition = heroInstance.Definition;

        SetAvatar(definition.HeroIcon);
        heroName.SetText(definition.HeroName);
        currentHealth.SetInt(definition.MaxHealth);
        maxHealth.SetInt(definition.MaxHealth);
        attack.SetInt(definition.Attack);
        defense.SetInt(definition.Defense);
        specialDefense.SetInt(definition.SpecialDefense);
        attackSpeed.SetSeconds(definition.AttackInterval);
        block.SetInt(definition.Block);
        deployCost.SetInt(heroInstance.DeployCost);
        redeployTime.SetSeconds(heroInstance.RedeployTime);
    }

    private void SetAvatar(Sprite sprite)
    {
        if (avatarImage == null)
        {
            return;
        }

        if (sprite == null)
        {
            avatarImage.sprite = null;
            avatarImage.enabled = false;
            return;
        }

        avatarImage.sprite = sprite;
        avatarImage.enabled = true;
    }
}
