using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class EnemyDictionaryCard : MonoBehaviour
{
    [Header("Identity")]
    [SerializeField] private Image enemyImage;
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text enemyDescriptionText;

    [Header("Stats")]
    [SerializeField] private StatBinding maxHealthStat;
    [SerializeField] private StatBinding defenseStat;
    [SerializeField] private StatBinding specialDefenseStat;
    [SerializeField] private StatBinding attackStat;
    [SerializeField] private StatBinding attackIntervalStat;
    [SerializeField] private StatBinding moveSpeedStat;
    [SerializeField] private StatBinding blockStat;

    private EnemyDefinition enemyDefinition;

    public EnemyDefinition EnemyDefinition => enemyDefinition;

    private void Awake()
    {
        CacheReferences();
    }

    public void BindEnemyData(EnemyDefinition definition)
    {
        CacheReferences();

        enemyDefinition = definition;

        if (definition == null)
        {
            Clear();
            return;
        }

        SetImage(enemyImage, definition.EnemySprite);
        if (string.IsNullOrEmpty(definition.EnemyName))
        {
            SetText(enemyNameText, string.Empty);
        }
        else
        {
            SetText(enemyNameText, definition.EnemyName.ToUpper());
        }
        SetText(enemyDescriptionText, definition.EnemyDescription);

        SetStat(maxHealthStat, definition.MaxHealth);
        SetStat(defenseStat, definition.Defense);
        SetStat(specialDefenseStat, definition.SpecialDefense);
        SetStat(attackStat, definition.Attack);
        SetStat(attackIntervalStat, definition.AttackInterval, "0.##");
        SetStat(moveSpeedStat, definition.MoveSpeed, "0.##");

        if (blockStat != null && blockStat != maxHealthStat)
        {
            blockStat.gameObject.SetActive(false);
            RefreshStat(blockStat);
        }
    }

    public void Clear()
    {
        enemyDefinition = null;

        SetImage(enemyImage, null);
        SetText(enemyNameText, string.Empty);
        SetText(enemyDescriptionText, string.Empty);

        RefreshStat(maxHealthStat);
        RefreshStat(defenseStat);
        RefreshStat(specialDefenseStat);
        RefreshStat(attackStat);
        RefreshStat(attackIntervalStat);
        RefreshStat(moveSpeedStat);

        if (blockStat != null && blockStat != maxHealthStat)
        {
            blockStat.gameObject.SetActive(false);
            RefreshStat(blockStat);
        }
    }

    private void CacheReferences()
    {
        if (enemyImage == null)
        {
            enemyImage = FindImage("EnemySprite");
        }

        if (enemyNameText == null)
        {
            enemyNameText = FindText("NameText");
        }

        if (enemyDescriptionText == null)
        {
            enemyDescriptionText = FindText("DescriptionText");
        }

        if (maxHealthStat == null)
        {
            maxHealthStat = FindStatBinding("Health");
            if (maxHealthStat == null)
            {
                maxHealthStat = FindStatBinding("Block");
            }
        }

        if (defenseStat == null)
        {
            defenseStat = FindStatBinding("Defense");
        }

        if (specialDefenseStat == null)
        {
            specialDefenseStat = FindStatBinding("SpecialDefense");
        }

        if (attackStat == null)
        {
            attackStat = FindStatBinding("Attack");
        }

        if (attackIntervalStat == null)
        {
            attackIntervalStat = FindStatBinding("AttackInterval");
        }

        if (moveSpeedStat == null)
        {
            moveSpeedStat = FindStatBinding("MoveSpeed");
        }

        if (blockStat == null)
        {
            blockStat = FindStatBinding("Block");
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

    private StatBinding FindStatBinding(string childName)
    {
        Transform child = FindChildRecursive(transform, childName);
        if (child != null)
        {
            return child.GetComponent<StatBinding>();
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
    }

    private static void SetStat(StatBinding statBinding, float value, string floatFormat = null)
    {
        if (statBinding == null || statBinding.TotalText == null || statBinding.TotalText.Text == null)
        {
            return;
        }

        statBinding.gameObject.SetActive(true);

        if (string.IsNullOrEmpty(floatFormat))
        {
            statBinding.TotalText.SetInt(value);
        }
        else
        {
            statBinding.TotalText.SetFloat(value, floatFormat);
        }

        if (statBinding.DetailText != null && statBinding.DetailText.Text != null)
        {
            statBinding.DetailText.Hide();
        }
    }

    private static void RefreshStat(StatBinding statBinding)
    {
        if (statBinding == null)
        {
            return;
        }

        RefreshBinding(statBinding.TotalText);

        if (statBinding.DetailText != null && statBinding.DetailText.Text != null)
        {
            statBinding.DetailText.Hide();
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(value))
        {
            text.text = string.Empty;
        }
        else
        {
            text.text = value;
        }
    }

    private static void RefreshBinding(UIValueTextBinding binding)
    {
        if (binding != null && binding.Text != null)
        {
            binding.Refresh();
        }
    }
}
