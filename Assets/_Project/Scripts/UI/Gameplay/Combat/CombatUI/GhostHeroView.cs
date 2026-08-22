using UnityEngine;

[DisallowMultipleComponent]
public class GhostHeroView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer leftArrowRenderer;
    [SerializeField] private SpriteRenderer rightArrowRenderer;
    [SerializeField] private SpriteRenderer upArrowRenderer;
    [SerializeField] private SpriteRenderer downArrowRenderer;

    [Header("Animator Parameters")]
    [SerializeField] private string dirXParameterName = "DirX";
    [SerializeField] private string dirYParameterName = "DirY";

    private float selectedArrowAlpha = 1.0f;
    private float unselectedArrowAlpha = 0.5f;

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

    public void SetAnimatorController(RuntimeAnimatorController animatorController)
    {
        if (animator != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }
    }

    public void SetFacingDirection(Vector2Int direction)
    {
        if (direction == Vector2Int.zero)
        {
            return;
        }

        SetAnimatorDirection(new Vector2(direction.x, direction.y));
        SetArrowDirection(direction);
    }

    private void SetAnimatorDirection(Vector2 direction)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        animator.SetFloat(dirXParameterName, direction.x);
        animator.SetFloat(dirYParameterName, direction.y);
    }

    public void Show(HeroCombatState combatState)
    {
        if (combatState == null || !combatState.IsValid)
        {
            Debug.LogWarning("[GhostHeroView] Invalid hero combat state. Cannot show ghost hero.");
            return;
        }

        SetHeroSprite(combatState.Definition.HeroSprite);
        SetAnimatorController(combatState.Definition.AnimatorController);
        gameObject.SetActive(true);
        SetFacingDirection(Vector2Int.left);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void UpdateWorldPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
    }

    private void SetArrowDirection(Vector2Int direction)
    {
        SetArrowAlpha(leftArrowRenderer, direction == Vector2Int.left ? selectedArrowAlpha : unselectedArrowAlpha);
        SetArrowAlpha(rightArrowRenderer, direction == Vector2Int.right ? selectedArrowAlpha : unselectedArrowAlpha);
        SetArrowAlpha(upArrowRenderer, direction == Vector2Int.up ? selectedArrowAlpha : unselectedArrowAlpha);
        SetArrowAlpha(downArrowRenderer, direction == Vector2Int.down ? selectedArrowAlpha : unselectedArrowAlpha);
    }

    private void SetArrowAlpha(SpriteRenderer arrowRenderer, float alpha)
    {
        if (arrowRenderer == null)
        {
            return;
        }

        Color color = arrowRenderer.color;
        color.a = Mathf.Clamp01(alpha);
        arrowRenderer.color = color;
    }
}
