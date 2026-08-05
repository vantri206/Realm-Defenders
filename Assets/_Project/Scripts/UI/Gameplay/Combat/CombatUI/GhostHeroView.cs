using UnityEngine;

[DisallowMultipleComponent]
public class GhostHeroView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animator Parameters")]
    [SerializeField] private string dirXParameterName = "DirX";
    [SerializeField] private string dirYParameterName = "DirY";

    [Header("Settings")]
    [SerializeField] private Color invalidColor = Color.red;

    private Color initColor = Color.white;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            initColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        SetFacingDirection(Vector2Int.left);
    }

    public void SetHeroSprite(Sprite heroSprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = heroSprite;
        }
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        SetAnimatorDirection(new Vector2(direction.x, direction.y));
    }

    private void SetAnimatorDirection(Vector2 direction)
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat(dirXParameterName, direction.x);
        animator.SetFloat(dirYParameterName, direction.y);
    }

    public void Show(HeroInstance heroInstance)
    {
        if (heroInstance == null || !heroInstance.IsValid)
        {
            Debug.LogWarning("[GhostHeroView] Invalid hero instance. Cannot show ghost hero.");
            return;
        }

        SetHeroSprite(heroInstance.Definition.HeroSprite);
        if (animator != null)
        {
            animator.runtimeAnimatorController = heroInstance.Definition.AnimatorController;
        }

        SetInvalidState(false);
        SetFacingDirection(Vector2Int.left);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdateWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    public void SetInvalidState(bool isInvalid)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isInvalid ? invalidColor : initColor;
        }
    }
}
