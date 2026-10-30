using UnityEngine;
using UnityEngine.AI;

/// AgentController: enemigo simple con NavMesh. Persigue al jugador cuando entra en su radio
/// de deteccion, lo ataca al alcanzarlo y vuelve a su punto de origen si lo pierde.

[RequireComponent(typeof(NavMeshAgent))]
public class AgentController : MonoBehaviour
{
    [Header("Deteccion")]
    public float distanciaDeteccion = 10f;
    [Range(1f, 360f)]
    public float campoVision = 110f;
    public Transform puntoVision;
    public LayerMask capasVision = ~0;

    [Header("Ataque")]
    public float distanciaAtaque = 1.8f;     // metros a los que puede golpear
    public int dano = 10;                    // vida que quita cada golpe
    public float cooldownDano = 1.5f;        // segundos entre golpes

    private NavMeshAgent agente;
    private PlayerHealth saludJugador;
    private Vector3 puntoInicial;            // donde aparecio: ahi vuelve si pierde al jugador
    private float proximoDano = 0f;

    private void Start()
    {
        agente = GetComponent<NavMeshAgent>();
        puntoInicial = transform.position;

        // El Player no usa tag especial: se busca directamente por su componente de vida.
        saludJugador = FindFirstObjectByType<PlayerHealth>();

        // Se detiene un poco antes del alcance para no empujar al jugador.
        agente.stoppingDistance = distanciaAtaque * 0.8f;
    }

    private void Update()
    {
        // Con el juego congelado (pausa o panel final) el agente no decide nada.
        if (Time.timeScale == 0f || saludJugador == null) return;

        Vector3 posicionJugador = saludJugador.transform.position;
        Vector3 origenVision = puntoVision != null
            ? puntoVision.position
            : transform.position + Vector3.up * 1.5f;
        Vector3 destinoJugador = posicionJugador + Vector3.up;
        Vector3 direccionJugador = destinoJugador - origenVision;
        float distancia = Vector3.Distance(transform.position, posicionJugador);
        bool jugadorVisible = PuedeVerJugador(origenVision, destinoJugador, direccionJugador, distancia);

        // El NavMeshAgent calcula la ruta solo: rodea obstaculos y no atraviesa paredes.
        if (jugadorVisible)
            agente.SetDestination(posicionJugador);
        else
            agente.SetDestination(puntoInicial);

        // Ataca al alcance con cooldown, igual que el dano de PlayerHealth.
        if (jugadorVisible && distancia <= distanciaAtaque && Time.time >= proximoDano)
        {
            proximoDano = Time.time + cooldownDano;
            saludJugador.RecibirDano(dano);
        }
    }

    private bool PuedeVerJugador(Vector3 origen, Vector3 destino, Vector3 direccion, float distancia)
    {
        if (distancia > distanciaDeteccion || direccion.sqrMagnitude <= 0.0001f)
            return false;

        float angulo = Vector3.Angle(transform.forward, direccion);
        if (angulo > campoVision * 0.5f)
            return false;

        RaycastHit[] impactos = Physics.RaycastAll(
            origen,
            direccion.normalized,
            direccion.magnitude,
            capasVision,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(impactos, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit impacto in impactos)
        {
            Transform golpeado = impacto.collider.transform;
            if (golpeado == transform || golpeado.IsChildOf(transform))
                continue;

            return golpeado == saludJugador.transform ||
                   golpeado.IsChildOf(saludJugador.transform);
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origen = puntoVision != null
            ? puntoVision.position
            : transform.position + Vector3.up * 1.5f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origen, distanciaDeteccion);
        Gizmos.color = Color.cyan;
        Vector3 izquierda = Quaternion.Euler(0f, -campoVision * 0.5f, 0f) * transform.forward;
        Vector3 derecha = Quaternion.Euler(0f, campoVision * 0.5f, 0f) * transform.forward;
        Gizmos.DrawRay(origen, izquierda * distanciaDeteccion);
        Gizmos.DrawRay(origen, derecha * distanciaDeteccion);
    }
}
