using UnityEngine;
using UnityEngine.AI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class AgentControllerEtapa1 : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;
    public AgentAnimatorZ3 animaciones;
    public PlayerHealth saludJugador;

    [Header("Movimiento")]
    public float velocidadPersecucion = 5f;

    [Header("Deteccion y ataque")]
    public float distanciaAtaque = 1.8f;
    public int dano = 10;
    public float cooldownDano = 1.5f;

    [Header("Reaccion")]
    public Renderer[] indicadoresVisuales;
    public AudioSource audioEstado;

    public Color colorPatrulla = new Color(0.25f, 0.72f, 0.78f);
    public Color colorAlerta = new Color(1f, 0.72f, 0.12f);
    public Color colorPersecucion = new Color(1f, 0.12f, 0.12f);
    public Color colorAtaque = new Color(1f, 0.28f, 0.04f);

    private NavMeshAgent agente;
    private bool activado = false;
    public bool EstaActivado => activado;

    private float proximoDano;
    private bool enModoAtaque;
    private MaterialPropertyBlock bloqueMaterial;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();

        if (animaciones == null)
            animaciones = GetComponentInChildren<AgentAnimatorZ3>();

        audioEstado = audioEstado != null
            ? audioEstado
            : GetComponent<AudioSource>();

        if (audioEstado == null)
            audioEstado = gameObject.AddComponent<AudioSource>();

        audioEstado.playOnAwake = false;
        audioEstado.spatialBlend = 1f;
        audioEstado.volume = 0.45f;

        if (indicadoresVisuales == null ||
            indicadoresVisuales.Length == 0)
        {
            indicadoresVisuales = GetComponentsInChildren<Renderer>();
        }

        bloqueMaterial = new MaterialPropertyBlock();
    }

    private void Start()
    {
        if (jugador == null)
        {
            GameObject objetoJugador =
                GameObject.FindGameObjectWithTag("Player");

            if (objetoJugador != null)
                jugador = objetoJugador.transform;
        }

        if (jugador != null && saludJugador == null)
        {
            saludJugador = jugador.GetComponent<PlayerHealth>();

            if (saludJugador == null)
                saludJugador = jugador.GetComponentInParent<PlayerHealth>();
        }

        agente.speed = velocidadPersecucion;
        agente.stoppingDistance = Mathf.Max(0.1f, distanciaAtaque * 0.8f);

        agente.isStopped = true;

        ActualizarColor(colorPatrulla);

        if (!agente.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                2f,
                NavMesh.AllAreas))
            {
                agente.Warp(hit.position);
            }
        }
    }

    private void Update()
    {
        if (!activado ||
            jugador == null ||
            saludJugador == null ||
            saludJugador.vidaActual <= 0)
            return;

        if (!agente.enabled || !agente.isOnNavMesh)
            return;

        float distancia = Vector3.Distance(
            transform.position,
            jugador.position
        );

        if (distancia > distanciaAtaque)
        {
            agente.isStopped = false;
            agente.SetDestination(jugador.position);

            if (enModoAtaque)
            {
                enModoAtaque = false;
                ActualizarColor(colorPersecucion);
            }
        }
        else
        {
            agente.isStopped = true;
            agente.ResetPath();

            MirarJugador();

            if (!enModoAtaque)
            {
                enModoAtaque = true;
                ActualizarColor(colorAtaque);
            }

            if (Time.time >= proximoDano)
            {
                proximoDano = Time.time + cooldownDano;

                if (animaciones != null)
                    animaciones.Atacar();

                saludJugador.RecibirDano(dano);

                LabAudioFeedback.ReproducirAtaqueAgente(
                    transform.position
                );

                StartCoroutine(PulsoAtaque());
            }
        }
    }

    public void Activar()
    {
        if (activado)
            return;

        activado = true;

        if (agente.isOnNavMesh)
            agente.isStopped = false;

        enModoAtaque = false;

        // Sonido de alerta del sistema original.
        LabAudioFeedback.ReproducirAlertaAgente(
            transform.position
        );

        // Color de alerta.
        ActualizarColor(colorAlerta);

        StartCoroutine(TransicionPersecucion());
    }

    private IEnumerator TransicionPersecucion()
    {
        yield return new WaitForSeconds(0.4f);

        if (activado && !enModoAtaque)
            ActualizarColor(colorPersecucion);
    }

    private void ActualizarColor(Color color)
    {
        if (indicadoresVisuales == null)
            return;

        foreach (Renderer renderizador in indicadoresVisuales)
        {
            if (renderizador == null)
                continue;

            renderizador.GetPropertyBlock(bloqueMaterial);

            bloqueMaterial.SetColor("_BaseColor", color);
            bloqueMaterial.SetColor("_Color", color);
            bloqueMaterial.SetColor("_EmissionColor", color * 0.35f);

            renderizador.SetPropertyBlock(bloqueMaterial);
        }
    }

    private void MirarJugador()
    {
        Vector3 direccion = jugador.position - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.LookRotation(direccion),
                agente.angularSpeed * Time.deltaTime
            );
        }
    }

    private IEnumerator PulsoAtaque()
    {
        Vector3 escalaOriginal = transform.localScale;

        transform.localScale = escalaOriginal * 1.08f;

        yield return new WaitForSeconds(0.12f);

        if (this != null)
            transform.localScale = escalaOriginal;
    }
}
