using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MessageBox : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private GameObject viewRoot;

    [Header("Content")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text messageText;

    [Header("Action")]
    [SerializeField] private UIButtonFeedback confirmButton;

    private bool isOpening;

    public bool IsOpening => isOpening;

    public event Action OnClosed;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        RegisterConfirmButtonEvent();
    }

    private void OnDisable()
    {
        UnregisterConfirmButtonEvent();

        if (!isOpening)
        {
            return;
        }

        isOpening = false;
        OnClosed?.Invoke();
    }

    private void OnDestroy()
    {
        UnregisterConfirmButtonEvent();
    }

    public bool Show(string title, Sprite icon, string text)
    {
        if (isOpening || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        CacheReferences();

        if (!HasRequiredReferences())
        {
            return false;
        }

        isOpening = true;
        SetText(titleText, title);
        SetIcon(icon);
        messageText.text = text;

        if (!viewRoot.activeSelf)
        {
            viewRoot.SetActive(true);
        }

        return true;
    }

    public void HideImmediate()
    {
        isOpening = false;

        if (viewRoot != null)
        {
            viewRoot.SetActive(false);
        }
    }

    private void HandleConfirmRequested()
    {
        Close();
    }

    private void Close()
    {
        if (!isOpening)
        {
            return;
        }

        isOpening = false;

        if (viewRoot != null)
        {
            viewRoot.SetActive(false);
        }

        OnClosed?.Invoke();
    }

    private void HandleConfirmButtonClicked()
    {
        HandleConfirmRequested();
    }

    private void SetIcon(Sprite icon)
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.gameObject.SetActive(icon != null);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target == null)
        {
            return;
        }

        bool hasValue = !string.IsNullOrWhiteSpace(value);
        target.text = hasValue ? value : string.Empty;
        target.gameObject.SetActive(hasValue);
    }

    private bool HasRequiredReferences()
    {
        bool hasReferences = true;

        if (viewRoot == null || messageText == null)
        {
            Debug.LogError("[MessageBox] ViewRoot and MessageText references are required.", this);
            hasReferences = false;
        }

        if (confirmButton == null)
        {
            Debug.LogError("[MessageBox] ConfirmButton reference is required.", this);
            hasReferences = false;
        }

        return hasReferences;
    }

    private void RegisterConfirmButtonEvent()
    {
        if (confirmButton == null)
        {
            return;
        }

        confirmButton.OnClicked -= HandleConfirmButtonClicked;
        confirmButton.OnClicked += HandleConfirmButtonClicked;
    }

    private void UnregisterConfirmButtonEvent()
    {
        if (confirmButton != null)
        {
            confirmButton.OnClicked -= HandleConfirmButtonClicked;
        }
    }

    private void CacheReferences()
    {
        if (viewRoot == null)
        {
            viewRoot = gameObject;
        }

        if (confirmButton == null)
        {
            confirmButton = GetComponentInChildren<UIButtonFeedback>(true);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif
}
