using System;
using UnityEngine;
using UnityEngine.UI;

public enum HubScreenId
{
    None,
    RosterHero
}

[RequireComponent(typeof(Button))]
public class HubNavigationButton : MonoBehaviour
{
    [SerializeField] private HubScreenId targetScreenId = HubScreenId.None;
    [SerializeField] private UIButtonFeedback button;

    public HubScreenId TargetScreenId => targetScreenId;
    public event Action<HubNavigationButton> OnNavigationRequested;

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
        if (button != null)
        {
            button.SetActive(value);
        }
    }

    public void SetInteractable(bool value)
    {
        if (button != null)
        {
            button.SetInteractable(value);
        }
    }

    private void HandleClicked()
    {
        if (button == null || !button.IsInteractable)
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

        button.OnClicked -= HandleClicked;
        button.OnClicked += HandleClicked;
    }

    private void UnregisterButtonEvent()
    {
        if (button != null)
        {
            button.OnClicked -= HandleClicked;
        }
    }

    private void CacheReferences()
    {
        if (button == null)
        {
            button = GetComponent<UIButtonFeedback>();
        }
    }
}
