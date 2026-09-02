using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestStageView : MonoBehaviour
{
    [Header("Screen")]
    [SerializeField] private GameObject viewRoot;
    [SerializeField] private UIButtonFeedback closeButton;

    [Header("Stage Data")]
    [SerializeField] private List<CombatStageDefinition> stageDefinitions = new List<CombatStageDefinition>();

    [Header("Stage Buttons")]
    [SerializeField] private Transform stageButtonContainer;
    [SerializeField] private GameObject stageButtonPrefab;

    [Header("Detail")]
    [SerializeField] private TMP_Text stageNameText;
    [SerializeField] private Image mapPreviewImage;
    [SerializeField] private TMP_Text experienceRewardText;
    [SerializeField] private Image gearRewardIcon;
    [SerializeField] private Image heroRewardIcon;

    private readonly List<QuestStageButton> spawnedButtons = new List<QuestStageButton>();
    private QuestStageButton selectedButton;
    private CombatStageDefinition selectedStage;

    public CombatStageDefinition SelectedStage => selectedStage;
    public event Action OnCloseRequested;
    public event Action<CombatStageDefinition> OnStageSelected;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        RegisterCloseButtonEvent();
    }

    private void OnDisable()
    {
        UnregisterCloseButtonEvent();
    }

    private void OnDestroy()
    {
        UnregisterCloseButtonEvent();
        UnregisterStageButtonEvents();
    }

    public void Show()
    {
        if (viewRoot != null)
        {
            viewRoot.SetActive(true);
        }

        ResetStageButtons();
    }

    public void Hide()
    {
        if (viewRoot != null)
        {
            viewRoot.SetActive(false);
        }
    }

    private void ResetStageButtons()
    {
        ClearSpawnedButtons();

        if (stageDefinitions == null || stageDefinitions.Count == 0)
        {
            BindStageDetail(null);
            return;
        }

        if (stageButtonContainer == null || stageButtonPrefab == null)
        {
            Debug.LogError("[QuestStageView] Stage button container and prefab are required to display stages.", this);
            BindStageDetail(null);
            return;
        }

        CombatStageDefinition firstStage = null;
        QuestStageButton firstButton = null;

        for (int i = 0; i < stageDefinitions.Count; i++)
        {
            CombatStageDefinition stageDefinition = stageDefinitions[i];
            if (stageDefinition == null)
            {
                continue;
            }

            GameObject buttonObject = Instantiate(stageButtonPrefab, stageButtonContainer);
            QuestStageButton button = buttonObject.GetComponent<QuestStageButton>();
            if (button == null)
            {
                button = buttonObject.AddComponent<QuestStageButton>();
            }

            button.BindStageData(stageDefinition, i);
            button.OnStageClicked += HandleStageButtonClicked;
            spawnedButtons.Add(button);

            if (firstStage == null)
            {
                firstStage = stageDefinition;
                firstButton = button;
            }
        }

        RefreshButtonLayout();
        SelectStage(firstButton, firstStage);
    }

    private void HandleStageButtonClicked(QuestStageButton button, CombatStageDefinition stageDefinition)
    {
        SelectStage(button, stageDefinition);
    }

    private void SelectStage(QuestStageButton button, CombatStageDefinition stageDefinition)
    {
        selectedButton = button;
        selectedStage = stageDefinition;

        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            QuestStageButton spawnedButton = spawnedButtons[i];
            if (spawnedButton != null)
            {
                spawnedButton.SetSelected(spawnedButton == selectedButton);
            }
        }

        BindStageDetail(selectedStage);
        OnStageSelected?.Invoke(selectedStage);
    }

    private void BindStageDetail(CombatStageDefinition stageDefinition)
    {
        if (stageNameText != null)
        {
            stageNameText.text = GetStageDisplayName(stageDefinition);
        }

        Sprite mapPreview = null;
        StageRewardDefinition rewardDefinition = null;
        if (stageDefinition != null)
        {
            mapPreview = stageDefinition.MapPreview;
            rewardDefinition = stageDefinition.RewardDefinition;
        }

        SetImage(mapPreviewImage, mapPreview);

        int experienceAmount = 0;
        Sprite gearIcon = null;
        Sprite heroIcon = null;

        if (rewardDefinition != null)
        {
            experienceAmount = rewardDefinition.ExperienceAmount;
            if (rewardDefinition.GearReward != null)
            {
                gearIcon = rewardDefinition.GearReward.GearIcon;
            }

            if (rewardDefinition.HeroReward != null)
            {
                heroIcon = rewardDefinition.HeroReward.HeroIcon;
            }
        }

        if (experienceRewardText != null)
        {
            experienceRewardText.text = experienceAmount.ToString("N0", CultureInfo.InvariantCulture);
        }

        SetImage(gearRewardIcon, gearIcon);
        SetImage(heroRewardIcon, heroIcon);
    }

    private string GetStageDisplayName(CombatStageDefinition stageDefinition)
    {
        if (stageDefinition == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(stageDefinition.StageName))
        {
            return stageDefinition.StageName.ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(stageDefinition.StageId))
        {
            return stageDefinition.StageId.ToUpperInvariant();
        }

        return string.Empty;
    }

    private void ClearSpawnedButtons()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            QuestStageButton button = spawnedButtons[i];
            if (button == null)
            {
                continue;
            }

            button.OnStageClicked -= HandleStageButtonClicked;
            button.gameObject.SetActive(false);
            Destroy(button.gameObject);
        }

        spawnedButtons.Clear();
        selectedButton = null;
        selectedStage = null;
    }

    private void RefreshButtonLayout()
    {
        RectTransform containerRect = stageButtonContainer as RectTransform;
        if (containerRect == null)
        {
            return;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        Canvas.ForceUpdateCanvases();
    }

    private void HandleCloseButtonClicked()
    {
        OnCloseRequested?.Invoke();
    }

    private void RegisterCloseButtonEvent()
    {
        if (closeButton == null)
        {
            return;
        }

        closeButton.OnClicked -= HandleCloseButtonClicked;
        closeButton.OnClicked += HandleCloseButtonClicked;
    }

    private void UnregisterCloseButtonEvent()
    {
        if (closeButton != null)
        {
            closeButton.OnClicked -= HandleCloseButtonClicked;
        }
    }

    private void UnregisterStageButtonEvents()
    {
        for (int i = 0; i < spawnedButtons.Count; i++)
        {
            if (spawnedButtons[i] != null)
            {
                spawnedButtons[i].OnStageClicked -= HandleStageButtonClicked;
            }
        }
    }

    private void CacheReferences()
    {
        if (viewRoot == null)
        {
            viewRoot = gameObject;
        }

        if (closeButton == null)
        {
            Transform closeButtonTransform = FindChildRecursive(transform, "Close_Button");
            if (closeButtonTransform != null)
            {
                closeButton = closeButtonTransform.GetComponent<UIButtonFeedback>();
            }

            if (closeButton == null)
            {
                closeButton = GetComponentInChildren<UIButtonFeedback>(true);
            }
        }

        if (stageButtonContainer == null)
        {
            Transform container = FindChildRecursive(transform, "LevelButton_Container");
            if (container != null)
            {
                stageButtonContainer = container;
            }
        }

        if (mapPreviewImage == null)
        {
            mapPreviewImage = FindImage("Map_View");
        }

        if (experienceRewardText == null)
        {
            experienceRewardText = FindText("Amount_Text");
        }

        if (gearRewardIcon == null)
        {
            gearRewardIcon = FindImage("Gear_Icon");
        }

        if (heroRewardIcon == null)
        {
            heroRewardIcon = FindImage("Hero_Icon");
        }

        if (stageNameText == null)
        {
            stageNameText = FindText("StageName_Text");
            if (stageNameText == null)
            {
                stageNameText = FindText("StageNameText");
            }
        }
    }

    private Image FindImage(string childName)
    {
        Transform child = FindChildRecursive(transform, childName);
        if (child != null)
        {
            return child.GetComponent<Image>();
        }

        return null;
    }

    private TMP_Text FindText(string childName)
    {
        Transform child = FindChildRecursive(transform, childName);
        if (child != null)
        {
            return child.GetComponent<TMP_Text>();
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void SetImage(Image image, Sprite sprite)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
    }
#endif
}
