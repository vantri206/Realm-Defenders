using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnemyDictonaryView : MonoBehaviour
{
    [Header("Screen")]
    [SerializeField] private GameObject viewRoot;
    [SerializeField] private UIButtonFeedback closeButton;

    [Header("Enemy Data")]
    [SerializeField] private List<EnemyDefinition> enemyDefinitions = new List<EnemyDefinition>();

    [Header("Enemy Cards")]
    [SerializeField] private ScrollRect enemyScrollRect;
    [SerializeField] private Transform enemyCardContainer;
    [SerializeField] private GameObject enemyCardPrefab;
    [SerializeField] private float scrollEndPadding = 160f;

    private readonly List<EnemyDictionaryCard> spawnedCards = new List<EnemyDictionaryCard>();

    public event Action OnCloseRequested;

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
    }

    public void Show()
    {
        if (viewRoot != null)
        {
            viewRoot.SetActive(true);
        }

        ResetEnemyCards();
    }

    public void Hide()
    {
        if (viewRoot != null)
        {
            viewRoot.SetActive(false);
        }
    }

    private void ResetEnemyCards()
    {
        ClearSpawnedCards();

        if (enemyDefinitions == null || enemyDefinitions.Count == 0)
        {
            return;
        }

        if (enemyCardContainer == null || enemyCardPrefab == null)
        {
            Debug.LogError("[EnemyDictonaryView] Enemy card container and prefab are required to display enemies.", this);
            return;
        }

        for (int i = 0; i < enemyDefinitions.Count; i++)
        {
            EnemyDefinition definition = enemyDefinitions[i];
            if (definition == null)
            {
                continue;
            }

            GameObject cardObject = Instantiate(enemyCardPrefab, enemyCardContainer);
            EnemyDictionaryCard card = cardObject.GetComponent<EnemyDictionaryCard>();
            if (card == null)
            {
                card = cardObject.AddComponent<EnemyDictionaryCard>();
            }

            card.BindEnemyData(definition);
            spawnedCards.Add(card);
        }

        RefreshScrollLayout();
    }

    private void ClearSpawnedCards()
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            EnemyDictionaryCard card = spawnedCards[i];
            if (card == null)
            {
                continue;
            }

            card.gameObject.SetActive(false);
            Destroy(card.gameObject);
        }

        spawnedCards.Clear();
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

    private void CacheReferences()
    {
        if (viewRoot == null)
        {
            viewRoot = gameObject;
        }

        if (enemyScrollRect == null)
        {
            enemyScrollRect = GetComponentInChildren<ScrollRect>(true);
        }
    }

    private void RefreshScrollLayout()
    {
        RectTransform contentRect = enemyCardContainer as RectTransform;
        if (contentRect == null)
        {
            return;
        }

        ApplyRuntimeEndPadding(contentRect);
        ApplyContentHeight(contentRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();
        ApplyContentHeight(contentRect);

        if (enemyScrollRect != null)
        {
            enemyScrollRect.content = contentRect;
            enemyScrollRect.StopMovement();
            enemyScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ApplyRuntimeEndPadding(RectTransform contentRect)
    {
        int endPadding = Mathf.CeilToInt(Mathf.Max(0f, scrollEndPadding));
        if (endPadding <= 0)
        {
            return;
        }

        VerticalLayoutGroup verticalLayoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
        {
            RectOffset padding = verticalLayoutGroup.padding;
            if (padding.bottom < endPadding)
            {
                padding.bottom = endPadding;
                verticalLayoutGroup.padding = padding;
            }

            return;
        }

        GridLayoutGroup gridLayoutGroup = contentRect.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup != null)
        {
            RectOffset padding = gridLayoutGroup.padding;
            if (padding.bottom < endPadding)
            {
                padding.bottom = endPadding;
                gridLayoutGroup.padding = padding;
            }
        }
    }

    private void ApplyContentHeight(RectTransform contentRect)
    {
        float preferredHeight = CalculatePreferredContentHeight(contentRect);
        if (preferredHeight <= 0f)
        {
            return;
        }

        if (enemyScrollRect != null && enemyScrollRect.viewport != null)
        {
            preferredHeight = Mathf.Max(preferredHeight, enemyScrollRect.viewport.rect.height);
        }

        contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
    }

    private float CalculatePreferredContentHeight(RectTransform contentRect)
    {
        VerticalLayoutGroup verticalLayoutGroup = contentRect.GetComponent<VerticalLayoutGroup>();
        if (verticalLayoutGroup != null)
        {
            return CalculateVerticalLayoutHeight(contentRect, verticalLayoutGroup);
        }

        GridLayoutGroup gridLayoutGroup = contentRect.GetComponent<GridLayoutGroup>();
        if (gridLayoutGroup != null)
        {
            return CalculateGridLayoutHeight(contentRect, gridLayoutGroup);
        }

        return LayoutUtility.GetPreferredHeight(contentRect);
    }

    private float CalculateVerticalLayoutHeight(RectTransform contentRect, VerticalLayoutGroup verticalLayoutGroup)
    {
        float height = verticalLayoutGroup.padding.top + verticalLayoutGroup.padding.bottom;
        int activeChildCount = 0;

        for (int i = 0; i < contentRect.childCount; i++)
        {
            RectTransform childRect = contentRect.GetChild(i) as RectTransform;
            if (childRect == null || !childRect.gameObject.activeSelf)
            {
                continue;
            }

            if (activeChildCount > 0)
            {
                height += verticalLayoutGroup.spacing;
            }

            float childHeight = LayoutUtility.GetPreferredHeight(childRect);
            if (childHeight <= 0f)
            {
                childHeight = childRect.rect.height;
            }

            height += childHeight;
            activeChildCount++;
        }

        return height;
    }

    private float CalculateGridLayoutHeight(RectTransform contentRect, GridLayoutGroup gridLayoutGroup)
    {
        int activeChildCount = 0;

        for (int i = 0; i < contentRect.childCount; i++)
        {
            Transform child = contentRect.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                activeChildCount++;
            }
        }

        if (activeChildCount == 0)
        {
            return gridLayoutGroup.padding.top + gridLayoutGroup.padding.bottom;
        }

        int columnCount = Mathf.Max(1, gridLayoutGroup.constraintCount);
        int rowCount = Mathf.CeilToInt((float)activeChildCount / columnCount);

        return gridLayoutGroup.padding.top
               + gridLayoutGroup.padding.bottom
               + rowCount * gridLayoutGroup.cellSize.y
               + Mathf.Max(0, rowCount - 1) * gridLayoutGroup.spacing.y;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        scrollEndPadding = Mathf.Max(0f, scrollEndPadding);
        CacheReferences();
    }
#endif
}
