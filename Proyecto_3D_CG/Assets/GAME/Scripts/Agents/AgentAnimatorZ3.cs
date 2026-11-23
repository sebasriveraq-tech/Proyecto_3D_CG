
using UnityEngine;
using UnityEngine.AI;

public class AgentAnimatorZ3 : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navigation;

    [Header("Configuración")]
    [SerializeField] private float velocidadMinima = 0.1f;
    [SerializeField] private float velocidadCaminar = 3.5f;

    private void Update()
    {
        if (animator == null || navigation == null)
            return;

        float velocidad = navigation.velocity.magnitude;

        if (velocidad < velocidadMinima)
        {
            animator.SetInteger("Locomotion", 0);
        }
        else if (velocidad < velocidadCaminar)
        {
            animator.SetInteger("Locomotion", 1);
        }
        else
        {
            animator.SetInteger("Locomotion", 2);
        }
    }

    public void Atacar()
    {
        if (animator != null)
            animator.SetTrigger("Attack");
    }
}
