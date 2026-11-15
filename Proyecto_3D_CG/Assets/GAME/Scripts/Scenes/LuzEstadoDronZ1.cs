using UnityEngine;

// Luz propia del dron de la Zona 1; no modifica el comportamiento del agente.
public class LuzEstadoDronZ1 : MonoBehaviour
{
    public AgentController agente;
    public Light luzEstado;

    private void LateUpdate()
    {
        if (agente == null || luzEstado == null) return;

        switch (agente.EstadoActual)
        {
            case AgentState.Persecucion:
            case AgentState.Ataque:
                luzEstado.color = new Color(1f, 0.08f, 0.04f);
                break;
            case AgentState.AlertaInvestigacion:
            case AgentState.Retorno:
                luzEstado.color = new Color(1f, 0.42f, 0.05f);
                break;
            default:
                luzEstado.color = new Color(0.12f, 1f, 0.25f);
                break;
        }
    }
}
