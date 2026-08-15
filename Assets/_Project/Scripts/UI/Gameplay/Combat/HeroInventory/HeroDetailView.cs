using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class HeroDetailView : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject viewRoot;

    [Header("Identity")]
    [SerializeField] private Image avatarImage;
    [SerializeField] private UIValueTextBinding heroName = new UIValueTextBinding();

    [Header("Trait")]
    [SerializeField] private Image classIcon;
    [SerializeField] private UIValueTextBinding className = new UIValueTextBinding();
    [SerializeField] private UIValueTextBinding attackTypeText;

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

        Refresh();
        SetData(heroInstance);

        Debug.Log($"Showing hero detail view for {heroInstance.Definition.HeroName}");
    }

    public void Show(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null || !heroRuntime.IsInitialized)
        {
            return;
        }

        Refresh();
        SetData(heroRuntime);
    }

    public void Refresh()
    {
        SetAvatar(null);
        heroName.Refresh();
        SetClassIcon(null);
        className.Refresh();
        attackTypeText.Refresh();
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

        gameObject.SetActive(false);
    }

    public void SetData(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Refresh();
            return;
        }

        currentHero = heroInstance;

        HeroDefinition definition = heroInstance.Definition;
        UnitStats stats = heroInstance.Stats;
        UnitBlock blocker = heroInstance.Block;

        // Identity
        SetAvatar(definition.HeroIcon);
        SetClassIcon(definition.HeroClass.Icon);
        heroName.SetText(definition.HeroName.ToString().ToUpper());
        className.SetText(definition.HeroClass.ClassId.ToString().ToUpper());
        attackTypeText.SetText(definition.AttackType.ToString().ToUpper());

        // Stats
        maxHealth.SetInt(stats.MaxHealth);
        currentHealth.SetInt(stats.MaxHealth);
        attack.SetInt(stats.Attack);
        defense.SetInt(stats.Defense);
        specialDefense.SetInt(stats.SpecialDefense);
        attackSpeed.SetSeconds(stats.AttackInterval);
        block.SetInt(blocker.BlockCount);
        deployCost.SetInt(heroInstance.DeployCost);
        redeployTime.SetSeconds(heroInstance.RedeployTime);
    }

    public void SetData(HeroRuntime heroRuntime)
    {
        if (heroRuntime == null || !heroRuntime.IsInitialized)
        {
            Refresh();
            return;
        }

        currentHero = heroRuntime.Instance;

        SetData(currentHero);
        currentHealth.SetInt(heroRuntime.CurrentHealth);
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

    private void SetClassIcon(Sprite sprite)
    {
        if (classIcon == null)
        {
            return;
        }

        if (sprite == null)
        {
            classIcon.sprite = null;
            classIcon.enabled = false;
            return;
        }

        classIcon.sprite = sprite;
        classIcon.enabled = true;
    }
}
