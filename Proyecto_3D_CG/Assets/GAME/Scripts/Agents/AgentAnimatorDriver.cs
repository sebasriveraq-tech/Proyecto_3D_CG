using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class AgentAnimatorDriver : MonoBehaviour
{
    private static readonly int Locomotion = Animator.StringToHash("Locomotion");
    private static readonly int Attack = Animator.StringToHash("AgentAttack");
    private static readonly int AttackState = Animator.StringToHash("Attack");

    [SerializeField] private Animator animator;
    [SerializeField] private AgentController controller;
    [SerializeField] private NavMeshAgent navigation;
    [SerializeField, Min(0.5f)] private float attackAnimationInterval = 1.2f;

    private float nextAttackAnimation;

    private void Awake()
    {
        if (controller == null) controller = GetComponent<AgentController>();
        if (navigation == null) navigation = GetComponent<NavMeshAgent>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator != null) animator.applyRootMotion = false;
    }

    private void Update()
    {
        if (animator == null || controller == null) return;

        bool moving = navigation != null && navigation.enabled &&
            navigation.velocity.sqrMagnitude > 0.04f;
        int locomotion = !moving ? 0 :
            controller.EstadoActual == AgentState.Persecucion ? 2 : 1;
        animator.SetInteger(Locomotion, locomotion);

        if (controller.EstadoActual != AgentState.Ataque)
        {
            nextAttackAnimation = 0f;
            return;
        }

        if (Time.time < nextAttackAnimation) return;
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool alreadyAttacking = state.shortNameHash == AttackState ||
            animator.IsInTransition(0) &&
            animator.GetNextAnimatorStateInfo(0).shortNameHash == AttackState;
        if (!alreadyAttacking) animator.SetTrigger(Attack);
        nextAttackAnimation = Time.time + attackAnimationInterval;
    }
}
