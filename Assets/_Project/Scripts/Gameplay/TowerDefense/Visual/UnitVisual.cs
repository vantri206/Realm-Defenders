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
        animator.SetFloat(dirXParameterName, direction.x);
        animator.SetFloat(dirYParameterName, direction.y);
    }

    public void SetIsMoving(bool isMoving)
    {
        animator.SetBool(isMovingParameterName, isMoving);
    }

    public void TriggerAttack()
    {
        animator.SetTrigger(attackTriggerName);
    }

    public void TriggerDie()
    {
        animator.SetTrigger(dieTriggerName);
    }

    public void TriggerHurt()
    {
        animator.SetTrigger(hurtTriggerName);
    }
}
