
using UnityEngine;

public class ActivadorEnemigoEtapa1 : MonoBehaviour
{
    [Header("Enemigo que se activara")]
    public AgentControllerEtapa1 enemigo;

    private bool activado = false;

    private void OnTriggerEnter(Collider other)
    {
        if (activado)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (enemigo == null)
        {
            Debug.LogWarning("No hay enemigo asignado al detector.", this);
            return;
        }

        enemigo.Activar();
        activado = true;

        Debug.Log("¡Enemigo activado!");
    }
}
