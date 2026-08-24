using System;
using UnityEngine;
using UnityEngine.UI;

public enum HeadquartersScreenId
{
    None,
    Team
}

[RequireComponent(typeof(Button))]
public class NavigationButton : MonoBehaviour
{
    [SerializeField] private HeadquartersScreenId targetScreenId = HeadquartersScreenId.None;
    [SerializeField] private Button button;
    [SerializeField] private UIButtonFeeling buttonFeeling;

    public HeadquartersScreenId TargetScreenId => targetScreenId;
    public event Action<NavigationButton> OnNavigationRequested;

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

    public void SetActive(bool value)
    {
        if (buttonFeeling != null)
        {
            buttonFeeling.SetActive(value);
        }
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
        {
            button.interactable = value;
        }

        if (buttonFeeling != null)
        {
            buttonFeeling.SetInteractable(value);
        }
    }

    private void HandleClicked()
    {
        if (button == null || !button.interactable)
        {
            return;
        }

        if (buttonFeeling != null && !buttonFeeling.TryPlayClickFeedback())
        {
            return;
        }

        OnNavigationRequested?.Invoke(this);
    }

    private void RegisterButtonEvent()
    {
        if (button == null)
        {
            Debug.LogError("[NavigationButton] Button reference is required.", this);
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

        if (buttonFeeling == null)
        {
            buttonFeeling = GetComponent<UIButtonFeeling>();
        }
    }
}
