using UnityEngine;
using UnityEngine.AI;

/// AgentController: enemigo simple con NavMesh. Persigue al jugador cuando entra en su radio
/// de deteccion, lo ataca al alcanzarlo y vuelve a su punto de origen si lo pierde.

[RequireComponent(typeof(NavMeshAgent))]
public class AgentController : MonoBehaviour
{
    [Header("Deteccion")]
    public float distanciaDeteccion = 10f;   // metros a los que empieza a perseguir

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
        float distancia = Vector3.Distance(transform.position, posicionJugador);

        // El NavMeshAgent calcula la ruta solo: rodea obstaculos y no atraviesa paredes.
        if (distancia <= distanciaDeteccion)
            agente.SetDestination(posicionJugador);
        else
            agente.SetDestination(puntoInicial);

        // Ataca al alcance con cooldown, igual que el dano de PlayerHealth.
        if (distancia <= distanciaAtaque && Time.time >= proximoDano)
        {
            proximoDano = Time.time + cooldownDano;
            saludJugador.RecibirDano(dano);
        }
    }
}
