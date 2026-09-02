using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class QuestStageButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text stageText;

    private CombatStageDefinition stageDefinition;

    public CombatStageDefinition StageDefinition => stageDefinition;
    public event Action<QuestStageButton, CombatStageDefinition> OnStageClicked;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
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

    public void BindStageData(CombatStageDefinition definition, int displayIndex)
    {
        CacheReferences();
        stageDefinition = definition;

        string stageLabel = $"ST-{displayIndex + 1}";
        if (definition != null)
        {
            if (!string.IsNullOrWhiteSpace(definition.StageId))
            {
                stageLabel = definition.StageId;
            }
            else if (!string.IsNullOrWhiteSpace(definition.StageName))
            {
                stageLabel = definition.StageName;
            }
        }

        if (stageText != null)
        {
            stageText.text = stageLabel;
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (button != null)
        {
            button.interactable = !isSelected;
        }
    }

    private void HandleButtonClicked()
    {
        OnStageClicked?.Invoke(this, stageDefinition);
    }

    private void RegisterButtonEvent()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(HandleButtonClicked);
        button.onClick.AddListener(HandleButtonClicked);
    }

    private void UnregisterButtonEvent()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClicked);
        }
    }

    private void CacheReferences()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (stageText == null)
        {
            stageText = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
