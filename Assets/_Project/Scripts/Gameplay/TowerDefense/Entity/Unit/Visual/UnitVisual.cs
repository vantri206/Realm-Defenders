using UnityEngine;

public class UnitVisual : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private string dirXParameterName = "DirX";
    [SerializeField] private string dirYParameterName = "DirY";
    [SerializeField] private string isMovingParameterName = "IsMoving";
    [SerializeField] private string attackTriggerName = "AttackTrigger";
    [SerializeField] private string dieTriggerName = "DieTrigger";
    [SerializeField] private string hurtTriggerName = "HurtTrigger";

    public Animator Animator => animator;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    public void Initialize(Sprite sprite, RuntimeAnimatorController animatorController)
    {
        if (animator != null && animatorController != null)
        {
            animator.runtimeAnimatorController = animatorController;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
    }
    
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

    public void SetDirection(Vector2 direction)
    {
        if (!HasAnimatorReference("set direction"))
        {
            return;
        }

        animator.SetFloat(dirXParameterName, direction.x);
        animator.SetFloat(dirYParameterName, direction.y);
    }

    public void SetIsMoving(bool isMoving)
    {
        if (!HasAnimatorReference("set movement state"))
        {
            return;
        }

        animator.SetBool(isMovingParameterName, isMoving);
    }

    public void TriggerAttack()
    {
        if (!HasAnimatorReference("trigger attack animation"))
        {
            return;
        }

        animator.SetTrigger(attackTriggerName);
    }

    public void TriggerDie()
    {
        if (!HasAnimatorReference("trigger die animation"))
        {
            return;
        }

        animator.SetTrigger(dieTriggerName);
    }

    public void TriggerHurt()
    {
        if (!HasAnimatorReference("trigger hurt animation"))
        {
            return;
        }

        animator.SetTrigger(hurtTriggerName);
    }

    private bool HasAnimatorReference(string actionName)
    {
        if (animator != null)
        {
            return true;
        }

        Debug.LogError($"[UnitVisual] Animator is required to {actionName}.", this);
        return false;
    }
}
