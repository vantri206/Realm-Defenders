using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class RosterHeroCard : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image heroImage;
    [SerializeField] private UIValueTextBinding levelText = new UIValueTextBinding();

    private HeroInstance heroInstance;

    public HeroInstance HeroInstance => heroInstance;
    public Image Background => background;
    public event Action<RosterHeroCard, HeroInstance> OnCardClicked;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();

        RegisterButtonEvent();
    }

    private void OnDisable()
    {
        UnregisterButtonEvent();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvent();
    }

    public void BindHeroData(HeroInstance hero)
    {
        heroInstance = hero;

        if (hero == null || !hero.IsValid || hero.Definition == null)
        {
            Clear();
            return;
        }

        if (heroImage != null)
        {
            heroImage.sprite = hero.Definition.HeroDisplaySprite;
            heroImage.enabled = hero.Definition.HeroDisplaySprite != null;
        }

        if (levelText != null)
        {
            Debug.Log($"Setting level text for hero {hero.Definition.HeroName} to LV.{hero.Level}");
            levelText.SetText($"LV.{hero.Level}");
        }
    }

    public void Clear()
    {
        heroInstance = null;

        if (heroImage != null)
        {
            heroImage.sprite = null;
            heroImage.enabled = false;
        }

        if (levelText != null)
        {
            levelText.Refresh();
        }   
    }

    public void SetActive(bool isActive)
    {
        button.interactable = isActive;
    }

    private void HandleClicked()
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            return;
        }

        OnCardClicked?.Invoke(this, heroInstance);
    }

    private void RegisterButtonEvent()
    {
        if (button == null)
        {
            Debug.LogError("[RosterHeroCard] Button reference is required.", this);
            return;
        }

        button.onClick.RemoveListener(HandleClicked);
        button.onClick.AddListener(HandleClicked);
    }

    private void UnregisterButtonEvent()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClicked);
        }
    }

    private void CacheReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
        
        if (background == null)
        {
            background = GetComponent<Image>();
        }
    }
}
